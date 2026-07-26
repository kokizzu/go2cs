// PanicException.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;
using System.Diagnostics;

namespace go;

/// <summary>
/// Represents an exception for the "panic" keyword.
/// </summary>
[DebuggerNonUserCode]
public class PanicException(object? state, Exception? innerException = null) :
    Exception(state?.ToString() ?? "nil", innerException)
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public object? State { get; } = state;

    /// <summary>
    /// Gets the stack trace of the site where this panic ORIGINALLY started, as
    /// <c>runtime.Stack</c>/<c>debug.Stack</c> must report it while the panic is being handled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Go keeps the panicking frames physically on the stack until the panic completes, so a
    /// traceback taken inside a deferred function shows the panic site. A CLR exception has already
    /// unwound those frames by the time a <c>finally</c>-based defer runs, and worse, both of the
    /// ways a panic travels DESTROY the trace: re-raising the same instance (<c>throw ex</c>) resets
    /// <see cref="Exception.StackTrace"/> to the re-raise point, and Go's own re-panic idiom
    /// (<c>defer func(){ p := recover(); panic(p) }()</c> — exactly what <c>sync.OnceFunc</c> does)
    /// creates a brand new panic in the deferred frame. This property is snapshotted ONCE, at the
    /// first catch, and inherited by any panic raised while handling this one, so the origin
    /// survives both.
    /// </para>
    /// <para>
    /// Cost is zero on the non-panicking path: the CLR fills <see cref="Exception.StackTrace"/> at
    /// throw time regardless, and nothing here is computed unless a panic is actually caught.
    /// </para>
    /// </remarks>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public StackTrace? PanicTrace { get; private set; }

    // Snapshot the throw site the first time this panic is caught. `thrown` is the exception that
    // actually travelled: for a mapped .NET runtime error (nil deref, divide by zero) THIS instance
    // was synthesized by RuntimeErrorPanic and was never thrown, so only the original carries frames.
    internal void CaptureThrowSite(Exception thrown)
    {
        PanicTrace ??= new StackTrace(thrown, fNeedFileInfo: true);
    }

    // Adopt the origin of the panic being handled when this one is raised from a deferred call —
    // Go's traceback there still shows the original panic's frames.
    internal void InheritThrowSite(PanicException origin)
    {
        PanicTrace ??= origin.PanicTrace;
    }
}
