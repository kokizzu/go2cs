// GoFunc.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// ReSharper disable CheckNamespace
// ReSharper disable ConditionIsAlwaysTrueOrFalse

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using go.golib;

namespace go;

/// <summary>
/// Delegate for the Go "defer" function.
/// </summary>
/// <param name="deferredAction"></param>
public delegate void Defer(Action deferredAction);

/// <summary>
/// Delegate for the Go "recover" function.
/// </summary>
/// <returns>Recovered panic state, if any.</returns>
public delegate object? Recover();

/// <summary>
/// Represents the root execution context for all Go functions.
/// </summary>
public class GoFuncRoot
{
    // Static thread local storage for captured panic exception shared between all GoFunc instances
    protected static readonly ThreadLocal<PanicException> CapturedPanic = new();

    // The panic whose deferred calls are RUNNING on this thread. CapturedPanic is cleared by
    // recover(), but Go's traceback keeps showing the panicking frames for the rest of the deferred
    // sequence — so the panic being handled is tracked separately, strictly scoped to HandleFinally
    // (saved and restored), which is what keeps it from ever going stale.
    protected static readonly ThreadLocal<PanicException?> HandledPanic = new();

    /// <summary>
    /// Gets the panic whose traceback a <c>runtime.Stack</c>/<c>debug.Stack</c> call on this thread
    /// should report — the one being handled by an enclosing deferred sequence, else one caught and
    /// not yet recovered. Null when no panic is in flight.
    /// </summary>
    /// <remarks>
    /// Go keeps a panicking goroutine's frames on the stack until the panic completes, so a
    /// traceback taken from a deferred function shows the panic site; the CLR has already unwound
    /// them. Consumers append <see cref="PanicException.PanicTrace"/> to the live managed trace to
    /// recover Go's observable output — see runtime's Stack.
    /// </remarks>
    public static PanicException? InFlightPanic => HandledPanic.Value ?? CapturedPanic.Value;
}

/// <summary>
/// Represents a Go function execution context for handling "defer" and "recover" keywords.
/// </summary>
/// <remarks>
/// <para>
/// This is where the whole ladder's actual behavior lives; every other <c>GoFunc&lt;…&gt;</c> in this
/// file and in <c>GoFunc.RefParameterArities.cs</c> derives from it and adds nothing but by-reference
/// parameters. The converter wraps any Go function that defers or recovers in one of these, because
/// Go's runtime does two things for free that C# has no direct equivalent for: it lets a deferred
/// call observe an in-flight panic through <c>recover()</c>, and it guarantees the deferred calls run
/// on EVERY exit path — normal return, panic, or a runtime fault.
/// </para>
/// <para>
/// <see cref="Execute"/> supplies both: a <c>catch</c> that parks the panic where
/// <see cref="HandleRecover"/> can hand it to <c>recover()</c>, and a <c>finally</c> that drains the
/// deferred stack. Note the ORDER that produces — the deferred calls run inside the <c>finally</c>,
/// i.e. after the panic has been captured, which is what makes a <c>defer</c> able to recover the
/// panic raised by the body it was registered in.
/// </para>
/// </remarks>
public class GoFunc<T> : GoFuncRoot
{
    public delegate void GoAction(Defer defer, Recover recover);
    public delegate T GoFunction(Defer defer, Recover recover);

    protected Stack<Action>? Defers;

    private readonly GoFunction? m_function;

    protected GoFunc()
    {
        m_function = null;
    }

    public GoFunc(GoAction action)
    {
        m_function = (defer, recover) =>
        {
            action(defer, recover);
            return default!;
        };
    }

    public GoFunc(GoFunction function)
    {
        m_function = function;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepperBoundary]
    public T Execute()
    {
        T result = default!;

        try
        {
            if (m_function is not null)
                result = m_function(HandleDefer, HandleRecover);
        }
        catch (Exception ex) when (RuntimeErrorPanic.TryAsPanic(ex, out PanicException? panic))
        {
            // Captures Go panics — explicit panic() calls and .NET exceptions that map to Go
            // runtime panics (e.g. integer divide by zero) — so recover() can observe them.
            // Non-panic exceptions fail the filter and propagate unchanged.
            // The throw site is snapshotted HERE, at the first (innermost, deepest) catch, before
            // any re-raise can reset it — see PanicException.PanicTrace.
            panic.CaptureThrowSite(ex);
            CapturedPanic.Value = panic;
        }
        finally
        {
            HandleFinally();
        }

        return result!;
    }

    protected void HandleDefer(Action? deferredAction)
    {
        if (deferredAction is null)
            return;

        Defers ??= new Stack<Action>();
        Defers.Push(deferredAction);
    }

    protected object? HandleRecover()
    {
        object? result = CapturedPanic.Value?.State;
        CapturedPanic.Value = null!;
        return result;
    }

    protected void HandleFinally()
    {
        if (Defers is not null)
        {
            // The panic this deferred sequence is handling, if any. It stays observable through
            // InFlightPanic for the whole sequence — recover() clears CapturedPanic, but Go's
            // traceback keeps showing the panicking frames until the panic completes. Strictly
            // save/restore scoped, so it cannot outlive the sequence.
            PanicException? handling = CapturedPanic.Value;
            PanicException? outer = HandledPanic.Value;

            HandledPanic.Value = handling ?? outer;

            try
            {
                while (Defers.Count > 0)
                {
                    try
                    {
                        Defers.Pop()();
                    }
                    catch (PanicException rePanic) when (handling is not null)
                    {
                        // Go's re-panic idiom (`defer func(){ panic(recover()) }()`, which is how
                        // sync.OnceFunc replays a panic on every call) raises a NEW panic from the
                        // deferred frame. Go's traceback still shows the original panic's frames, so
                        // the new panic adopts the origin rather than starting a fresh, shallower one.
                        rePanic.InheritThrowSite(handling);
                        throw;
                    }
                }
            }
            finally
            {
                HandledPanic.Value = outer;
            }
        }

        if (CapturedPanic.Value is not null)
            throw CapturedPanic.Value;
    }
}

/// <summary>
/// Represents a Go function execution context with 1 reference parameter for handling "defer" and "recover" keywords.
/// </summary>
/// <remarks>
/// <para>
/// The first rung of the ref-parameter ladder, and the only one kept here: rungs 2 through 16 are
/// mechanical repetition of this exact shape and live in <c>GoFunc.RefParameterArities.cs</c>, whose
/// banner explains why the ladder exists at all. This rung stays beside <see cref="GoFunc{T}"/> as the
/// one worked example, so a reader can see how a by-reference variable is threaded without opening
/// eight hundred lines of near-identical siblings.
/// </para>
/// <para>
/// It is also the ONLY rung that admits a <c>ref struct</c>. <c>allows ref struct</c> here means a
/// converted function may ref-thread a <c>Span</c>-shaped local through this rung; a function that
/// ref-threads two variables cannot, because no rung in the sibling file carries the anti-constraint.
/// </para>
/// </remarks>
public sealed class GoFunc<TRef1, T> : GoFunc<T>
    where TRef1 : allows ref struct
{
    public delegate void GoRefAction(ref TRef1 ref1, Defer defer, Recover recover);
    public delegate T GoRefFunction(ref TRef1 ref1, Defer defer, Recover recover);

    private readonly GoRefFunction m_function;

    public GoFunc(GoRefAction action)
    {
        m_function = (ref TRef1 ref1, Defer defer, Recover recover) =>
        {
            action(ref ref1, defer, recover);
            return default!;
        };
    }

    public GoFunc(GoRefFunction function)
    {
        m_function = function;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepperBoundary]
    public T Execute(ref TRef1 ref1)
    {
        T result = default!;

        try
        {
            result = m_function(ref ref1, HandleDefer, HandleRecover);
        }
        catch (Exception ex) when (RuntimeErrorPanic.TryAsPanic(ex, out PanicException? panic))
        {
            // Captures Go panics — explicit panic() calls and .NET exceptions that map to Go
            // runtime panics (e.g. integer divide by zero) — so recover() can observe them.
            // Non-panic exceptions fail the filter and propagate unchanged.
            // The throw site is snapshotted HERE, at the first (innermost, deepest) catch, before
            // any re-raise can reset it — see PanicException.PanicTrace.
            panic.CaptureThrowSite(ex);
            CapturedPanic.Value = panic;
        }
        finally
        {
            HandleFinally();
        }

        return result!;
    }
}
