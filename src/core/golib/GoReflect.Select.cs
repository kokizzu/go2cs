// GoReflect.Select.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// ReSharper disable InconsistentNaming

using System;

namespace go;

// ---------------------------------------------------------------------------------------------
// reflect.Select over golib's OWN select engine.
//
// reflect.Select's auto conversion could not run: the direction check reinterpreted a synthesized
// descriptor onto the linker's chanType record (non-deterministic, see the Value.Close hand-own for
// the same root), and the select itself was a `rselect` runtime stub. But the select ALGORITHM is
// not the missing piece — golib already runs it for the converter's own `select` statements
// (builtin.select / SelectRuntime). So this is a BRIDGE, not a reimplementation: reflect hands each
// case's live channel (IChannel) and boxed send value here, this builds the SAME SelectOp the
// typed Sending/Receiving mint (through ISelectableChannel), runs SelectRuntime, and — for a
// receive win — consumes the delivered value the engine pushed to SelectPending keyed by that
// case's core. Concurrency semantics (closed-ready, nil-never-ready, default, blocking, fairness)
// are the engine's contract, unchanged; nothing about them lives here.
//
// This is the ONLY public surface the reflect.Select hand-own adds: SelectOp, ChanCore,
// SelectRuntime and SelectPending all stay internal to golib.
// ---------------------------------------------------------------------------------------------
public static partial class GoReflect
{
    /// <summary>
    /// Runs a <c>reflect.Select</c> over the NON-DEFAULT cases <paramref name="channels"/> /
    /// <paramref name="isSend"/> / <paramref name="sendValues"/> (parallel arrays, in the caller's
    /// case order with default cases omitted), through golib's own select engine.
    /// </summary>
    /// <param name="channels">Each case's live channel as <see cref="IChannel"/>; <c>null</c> or a
    /// nil-core channel registers a never-ready case (Go: a nil channel's case is never chosen).</param>
    /// <param name="isSend"><c>true</c> for a send case, <c>false</c> for a receive case.</param>
    /// <param name="sendValues">The boxed value to send for a send case (already marshalled by the
    /// caller to the channel's element type); ignored for a receive case.</param>
    /// <param name="hasDefault">Whether the select carried a <c>default:</c> case — then the run is
    /// non-blocking and returns <c>-1</c> when no case is ready.</param>
    /// <returns>
    /// <c>opWinner</c>: the index (into these arrays) of the case that fired, or <c>-1</c> when a
    /// default fired (only possible when <paramref name="hasDefault"/>). For a receive win,
    /// <c>recvValue</c> is the delivered value (boxed; <c>null</c> = zero value) and <c>recvOk</c> is
    /// Go's comma-ok bit (<c>false</c> = closed-and-drained). A send win yields <c>(idx, null, false)</c>.
    /// </returns>
    public static (nint opWinner, object? recvValue, bool recvOk) RunSelect(
        IChannel?[] channels, bool[] isSend, object?[] sendValues, bool hasDefault)
    {
        SelectOp[] ops = new SelectOp[channels.Length];

        for (int i = 0; i < channels.Length; i++)
        {
            // A nil channel (null reference, or a channel<T> struct with a null core) mints a
            // never-ready descriptor exactly as the typed Sending/Receiving do — the engine
            // partitions it out. ISelectableChannel is the one non-generic seam that reaches the
            // same SelectOp construction without T in hand.
            ops[i] = channels[i] is ISelectableChannel selectable
                ? selectable.SelectOpFor(isSend[i], isSend[i] ? sendValues[i] : null)
                : new SelectOp(null, isSend[i], null);
        }

        int winner = hasDefault ? SelectRuntime.TryRun(ops) : SelectRuntime.Run(ops);

        if (winner < 0)
        {
            // Non-blocking run with nothing ready: the default case fired.
            return (-1, null, false);
        }

        if (isSend[winner])
        {
            // A send win carries no delivered value (and pushed no pending frame).
            return (winner, null, false);
        }

        // A receive win: the engine pushed the delivered value to SelectPending keyed by the
        // winning case's core — consume it here (the reflect analog of the emitted guard's
        // `case N when ch.ꟷᐳ(out v):` pop), so nothing is left stranded on the per-thread stack.
        SelectPending.TryConsume(ops[winner].Core, out object? value, out bool ok);
        return (winner, value, ok);
    }
}
