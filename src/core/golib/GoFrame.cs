// GoFrame.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using go.golib;

namespace go;

/// <summary>
/// The per-call defer list of a converted Go function — a Go stack frame's deferred-call
/// records, and nothing else.
/// </summary>
/// <remarks>
/// <para>
/// A Go function that defers or recovers is emitted with its body INLINE inside
/// <c>try</c>/<c>catch</c>/<c>finally</c> and one of these declared beside it:
/// </para>
/// <code>
/// //  Go:   func f() { defer g(); … }
/// GoFrame ᒐ = default;
/// try
/// {
///     deferǃ(g, ref ᒐ);
///     …
/// }
/// catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
/// finally { ᒐ.Run(); }
/// </code>
/// <para>
/// That replaces the <c>func&lt;T&gt;((defer, recover) =&gt; …)</c> execution context, which
/// modelled the same three things — a catch, a finally, and a defer list — as an OBJECT that owned
/// the body. Owning the body forced the body to be a delegate, which forced a display class for
/// everything it touched, which forced the <c>GoFunc&lt;TRef1…TRef16&gt;</c> ladder for everything
/// a delegate cannot capture. None of that is needed: <c>try</c>/<c>catch</c>/<c>finally</c> are
/// STATEMENTS, and <c>recover()</c> reads a static thread-local slot rather than a handle on the
/// frame — so a deferred closure never needs to reach the frame, and only the defer LIST is
/// genuinely per-call. A <c>ref struct</c> can hold it: it lives in the caller's own stack frame,
/// the JIT can enregister the slots, and it allocates nothing.
/// </para>
/// <para>
/// Design: <c>docs/Phase4/DESIGN-closure-emission.md</c> §4.
/// </para>
/// </remarks>
public ref struct GoFrame
{
    // Inline slots for the common defer arities; m_overflow is the correctness tail past them.
    // FOUR comes from a census of the Go standard library, not from a guess: of its 1,454 deferring
    // scopes (defer statements counted per function or literal, the way Go scopes them), 85.7%
    // register one, 96.3% two or fewer, and 99.2% four or fewer — one scope reaches sixteen. So the
    // overflow list exists for correctness and is allocated by a vanishing fraction of frames.
    // A defer inside a LOOP registers once per iteration and is what actually reaches it, which is
    // also why the count cannot be a purely syntactic property (see docs/Phase4/DESIGN-closure-emission.md §4.2).
    private Action? m_d0, m_d1, m_d2, m_d3;
    private List<Action>? m_overflow;
    private int m_count;

    /// <summary>
    /// Gets the number of deferred calls registered in this frame and not yet run.
    /// </summary>
    public readonly int Count => m_count;

    /// <summary>
    /// Registers a deferred call, i.e. Go's <c>defer</c> statement.
    /// </summary>
    /// <param name="deferred">Deferred call to register; a null registration is ignored.</param>
    /// <remarks>
    /// Go evaluates a deferred call's ARGUMENTS at the <c>defer</c> statement and runs the call on
    /// function exit, so the emission captures the arguments here — see the <c>deferǃ</c> arity
    /// ladder in <c>builtin.DeferRegistrations.cs</c>, which is what closes over them.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Push(Action? deferred)
    {
        if (deferred is null)
            return;

        switch (m_count)
        {
            case 0:
                m_d0 = deferred;
                break;
            case 1:
                m_d1 = deferred;
                break;
            case 2:
                m_d2 = deferred;
                break;
            case 3:
                m_d3 = deferred;
                break;
            default:
                (m_overflow ??= new List<Action>()).Add(deferred);
                break;
        }

        m_count++;
    }

    /// <summary>
    /// Runs this frame's deferred calls, last registered first, then re-raises an unrecovered panic.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Called from the emitted <c>finally</c>, so it runs on EVERY exit path — normal return,
    /// panic, <c>runtime.Goexit</c>, or a mapped runtime fault — which is Go's whole guarantee for
    /// <c>defer</c>. It runs AFTER the panic has been parked by <see cref="Capture"/>, which is
    /// what lets a deferred call recover the panic raised by the body it was registered in.
    /// </para>
    /// <para>
    /// This is <c>GoFunc.HandleFinally</c>'s logic verbatim — the <c>HandledPanic</c>
    /// save/restore, the re-panic <c>InheritThrowSite</c> rule, and the final re-throw of an
    /// unrecovered panic all behave exactly as they did. It moved; it did not change.
    /// </para>
    /// </remarks>
    public void Run()
    {
        if (m_count > 0)
        {
            // The panic this deferred sequence is handling, if any. It stays observable through
            // InFlightPanic for the whole sequence — recover() clears the captured panic, but Go's
            // traceback keeps showing the panicking frames until the panic completes. Strictly
            // save/restore scoped, so it cannot outlive the sequence.
            PanicException? handling = GoFuncRoot.CapturedPanicValue;
            PanicException? outer = GoFuncRoot.HandledPanicValue;

            GoFuncRoot.HandledPanicValue = handling ?? outer;

            try
            {
                while (m_count > 0)
                {
                    try
                    {
                        Pop()();
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
                GoFuncRoot.HandledPanicValue = outer;
            }
        }

        if (GoFuncRoot.CapturedPanicValue is not null)
            throw GoFuncRoot.CapturedPanicValue;
    }

    // LIFO removal. The slot is cleared on the way out so a frame that outlives its drain (it
    // cannot, but the JIT does not know that) holds no reference to a run delegate.
    private Action Pop()
    {
        m_count--;

        if (m_count > 3)
        {
            int index = m_count - 4;
            Action overflowed = m_overflow![index];
            m_overflow.RemoveAt(index);
            return overflowed;
        }

        Action deferred;

        switch (m_count)
        {
            case 0:
                deferred = m_d0!;
                m_d0 = null;
                break;
            case 1:
                deferred = m_d1!;
                m_d1 = null;
                break;
            case 2:
                deferred = m_d2!;
                m_d2 = null;
                break;
            default:
                deferred = m_d3!;
                m_d3 = null;
                break;
        }

        return deferred;
    }

    /// <summary>
    /// The emitted <c>catch</c> FILTER: reports whether an exception is (or maps to) a Go panic.
    /// </summary>
    /// <param name="ex">Exception to inspect.</param>
    /// <param name="panic">Resulting panic when the exception maps to a Go panic.</param>
    /// <returns><c>true</c> if <paramref name="ex"/> is (or maps to) a Go panic; otherwise <c>false</c>.</returns>
    /// <remarks>
    /// A pure forward to <see cref="RuntimeErrorPanic.TryAsPanic"/> — which is the ONE adoption
    /// point where a .NET exception becomes a Go panic and where the panic's origin is snapshotted.
    /// It exists so an emitted <c>catch</c> filter can name it without the converted file having to
    /// import <c>go.golib</c>; a non-panic exception fails the filter and propagates unchanged,
    /// exactly as it does past <c>GoFunc.Execute</c>. <see cref="GoexitException"/> fails it by
    /// design, so a <c>runtime.Goexit</c> unwinds through the frame while the <c>finally</c> still
    /// runs the defers.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPanic(Exception ex, [NotNullWhen(true)] out PanicException? panic)
    {
        return RuntimeErrorPanic.TryAsPanic(ex, out panic);
    }

    /// <summary>
    /// The emitted <c>catch</c> BODY: parks a caught panic where <c>recover()</c> can read it.
    /// </summary>
    /// <param name="panic">Panic caught by the emitted frame.</param>
    /// <remarks>
    /// Deliberately separate from <see cref="IsPanic"/> rather than folded into the filter: an
    /// exception filter runs during the FIRST pass of managed exception handling, before any
    /// intervening <c>finally</c>, and parking the panic is what the <c>finally</c> then observes.
    /// Keeping it in the catch BODY preserves <c>GoFunc.Execute</c>'s ordering exactly. The origin
    /// snapshot is not repeated here — <see cref="RuntimeErrorPanic.TryAsPanic"/> already took it
    /// at the adoption point, and it is once-only.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepperBoundary]
    public static void Capture(PanicException panic)
    {
        GoFuncRoot.CapturedPanicValue = panic;
    }
}
