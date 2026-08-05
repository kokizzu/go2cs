// GoFuncRoot.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// ReSharper disable CheckNamespace

using System;
using System.Threading;

namespace go;

/// <summary>
/// Represents the root execution context for all Go functions.
/// </summary>
public class GoFuncRoot
{
    // Static thread local storage for captured panic exception shared between all GoFunc instances
    protected static readonly ThreadLocal<PanicException> CapturedPanic = new();

    // The panic whose deferred calls are RUNNING on this thread. CapturedPanic is cleared by
    // recover(), but Go's traceback keeps showing the panicking frames for the rest of the deferred
    // sequence — so the panic being handled is tracked separately, strictly scoped to GoFrame.Run
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

    // The two slots, reachable by the golib members that read and write them: GoFrame's catch/
    // finally pair and builtin.recover().
    internal static PanicException? CapturedPanicValue
    {
        get => CapturedPanic.Value;
        set => CapturedPanic.Value = value!;
    }

    internal static PanicException? HandledPanicValue
    {
        get => HandledPanic.Value;
        set => HandledPanic.Value = value;
    }
}
