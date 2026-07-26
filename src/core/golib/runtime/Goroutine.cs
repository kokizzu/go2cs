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

    private static void Run(Action body)
    {
        using Scope scope = Enter();

        try
        {
            body();
        }
        catch (GoexitException)
        {
            // runtime.Goexit: THIS goroutine ends here. Its deferred calls have already run on the
            // way out (GoFunc.HandleFinally), recover() never saw the unwind (GoexitException is not
            // a PanicException), and no other goroutine is affected — Go's three Goexit properties,
            // all of them falling out of machinery that already existed. A PanicException reaching
            // this point is NOT caught: it stays unhandled and reaches golib's AppDomain backstop,
            // which reports it Go-style and exits 2, exactly as Go crashes on an unrecovered panic
            // in any goroutine.
        }
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
