// Goroutine.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;
using System.Threading;

namespace go.golib;

/// <summary>
/// The goroutine ROOT — where the body of every Go <c>go</c> statement begins and ends.
/// </summary>
/// <remarks>
/// Every <c>builtin.goǃ</c> overload dispatches through <see cref="Start"/>, so the root's policy is
/// stated exactly once instead of being duplicated across the arity overloads.
/// </remarks>
public static class Goroutine
{
    // True only while this thread is executing a goroutine body. Goroutines run on POOL threads, so
    // the flag MUST be restored on the way out: a thread that finished one goroutine returns to the
    // pool and must no longer look like one.
    [ThreadStatic]
    private static bool t_onGoroutine;

    // The containment policy, when a HOST installed one. Only ever assigned non-null, so a root
    // whose filter observed a policy can never find it gone by the time the handler runs.
    private static Action<Exception>? s_containment;

    // The panic observer, when a HOST installed one. Read once per escaping panic and never cleared.
    private static Action<PanicException>? s_panicObserver;

    /// <summary>
    /// Indicates whether the calling thread is running a goroutine body rather than the main
    /// goroutine.
    /// </summary>
    /// <remarks>
    /// <c>runtime.Goexit</c> is the one Go primitive whose contract differs between the two: from a
    /// goroutine it ends that goroutine, while from the MAIN goroutine it ends <c>main</c> without
    /// returning and leaves the program running its other goroutines — a shape with no managed
    /// counterpart today, so the main-goroutine case stays gated (see the runtime package's
    /// <c>managed_impl.cs</c>).
    /// </remarks>
    public static bool OnGoroutine => t_onGoroutine;

    /// <summary>
    /// Marks the calling thread as running a goroutine body for the lifetime of the returned scope.
    /// </summary>
    /// <remarks>
    /// Used by hosts that run Go code on a thread they created themselves rather than through
    /// <see cref="Start"/> — the converted-test host runs each test on a dedicated thread, and that
    /// thread IS the test's goroutine in Go terms (Go's <c>tRunner</c> runs every test in one).
    /// </remarks>
    public static Scope Enter()
    {
        Scope scope = new(t_onGoroutine);
        t_onGoroutine = true;
        return scope;
    }

    /// <summary>
    /// Queues <paramref name="body"/> to run as a goroutine.
    /// </summary>
    public static void Start(Action body)
    {
        ThreadPool.QueueUserWorkItem(_ => Run(body));
    }

    /// <summary>
    /// Installs a HOST policy that contains a non-panic exception escaping a goroutine instead of
    /// letting it terminate the process.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A converted program must NOT call this: Go's behavior for an unhandled failure in a goroutine
    /// IS process death, and that fidelity is the default here (the exception reaches golib's
    /// AppDomain backstop, which reports it and exits 2). The policy exists for a host that runs many
    /// independent Go programs in one process — the converted-test host — where killing the process
    /// discards every result it had not yet written and blanks the tail of the package's run. There,
    /// one goroutine's infrastructure failure belongs to ONE test.
    /// </para>
    /// <para>
    /// A <c>PanicException</c> (or any exception that maps to a Go runtime panic) is NEVER offered to
    /// the policy: a panic crossing a goroutine root keeps its Go-faithful fatal path even under a
    /// host, because that is what Go does and what the differential oracle must observe.
    /// </para>
    /// </remarks>
    public static void ContainUnhandledExceptions(Action<Exception> policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        Volatile.Write(ref s_containment, policy);
    }

    /// <summary>
    /// Installs a HOST observer that is handed a PANIC escaping a goroutine root, immediately before
    /// the fatal path it is entitled to takes the process down.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This contains nothing and cannot: an unrecovered panic in a goroutine kills a Go program, so
    /// it kills a converted one (see <see cref="ContainUnhandledExceptions"/> for why a host may not
    /// opt out of that). The observer runs from an exception FILTER — first pass, before any unwind —
    /// and the filter always declines, so the panic still reaches golib's AppDomain backstop with its
    /// stack intact and the intervening finally blocks unrun, byte-identical to an unobserved run.
    /// </para>
    /// <para>
    /// It exists because that fatal path is FRAMELESS by design: the backstop prints the panic VALUE
    /// and exits 2, which is Go's own report and exactly what a converted program should print. A
    /// host running many independent Go programs in one process needs more than the value — WHICH
    /// program died, WHERE it faulted, and a last chance to flush the results it has already gathered
    /// but not yet written. Without this, a goroutine panic anywhere in a converted package's test
    /// suite erased every verdict in the run and reported one line with no frame in their place.
    /// </para>
    /// </remarks>
    public static void ObserveUnhandledPanic(Action<PanicException> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        Volatile.Write(ref s_panicObserver, observer);
    }

    // The goroutine ROOT itself, on the CALLING thread. Start queues it; GolibTests calls it directly,
    // because the root's policy can only be observed from a thread whose exception the caller can
    // still catch — the real path ends in an unhandled exception and a dead process by design.
    internal static void Run(Action body)
    {
        using Scope scope = Enter();

        try
        {
            body();
        }
        // Clause order is load-bearing: a GoexitException is not a panic either, so it would satisfy
        // the containment filter below and be reported as a host failure. It is handled here first.
        catch (GoexitException)
        {
            // runtime.Goexit: THIS goroutine ends here. Its deferred calls have already run on the
            // way out (GoFunc.HandleFinally), recover() never saw the unwind (GoexitException is not
            // a PanicException), and no other goroutine is affected — Go's three Goexit properties,
            // all of them falling out of machinery that already existed.
        }
        catch (Exception ex) when (CanContain(ex))
        {
            // A host installed a containment policy and this is not a panic — hand it over instead
            // of letting it reach the process-killing backstop. The filter (rather than a catch and
            // rethrow) is deliberate: when nothing contains the exception, NO handler matches and the
            // runtime's unhandled path is reached with the stack intact and the intervening finally
            // blocks unrun — byte-identical to the behavior before any of this existed. A
            // PanicException never reaches a policy at all: it stays unhandled and hits golib's
            // AppDomain backstop, which reports it Go-style and exits 2, exactly as Go crashes on an
            // unrecovered panic in any goroutine.
            Volatile.Read(ref s_containment)!(ex);
        }
        catch (Exception ex) when (Observed(ex))
        {
            // Unreachable by construction: Observed always returns false, so this root catches a
            // panic no more than it did before an observer could exist. The clause is here only to
            // give the filter — which runs while the panicking stack is still standing — a place to
            // be attached.
        }
    }

    private static bool CanContain(Exception ex) =>
        Volatile.Read(ref s_containment) is not null && !RuntimeErrorPanic.TryAsPanic(ex, out _);

    // Hands an escaping panic to the host observer and ALWAYS declines it. Reached only after
    // CanContain has said no, which for a panic is unconditional.
    private static bool Observed(Exception ex)
    {
        if (Volatile.Read(ref s_panicObserver) is not { } observer)
            return false;

        // Adoption also snapshots the panic's ORIGIN (RuntimeErrorPanic's documented side effect at
        // every adoption point), so a panic that passed through no defer frame at all — nothing to
        // catch it between the fault and here — still reports where it faulted rather than naming
        // this root.
        if (!RuntimeErrorPanic.TryAsPanic(ex, out PanicException? panic))
            return false;

        try
        {
            observer(panic);
        }
        catch
        {
            // An observer is a diagnostic, never a participant: whatever it does, or fails to do,
            // the panic reaches the fatal path unchanged.
        }

        return false;
    }

    /// <summary>
    /// Restores the goroutine marking a matching <see cref="Enter"/> replaced.
    /// </summary>
    public readonly struct Scope : IDisposable
    {
        private readonly bool m_previous;

        internal Scope(bool previous)
        {
            m_previous = previous;
        }

        public void Dispose()
        {
            t_onGoroutine = m_previous;
        }
    }
}
