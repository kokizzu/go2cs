// GoexitException.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;
using System.Diagnostics;

namespace go;

/// <summary>
/// Unwinds the calling goroutine for <c>runtime.Goexit</c>.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately NOT a <see cref="PanicException"/>. Go specifies that <c>recover()</c> returns
/// <c>nil</c> inside the deferred calls a Goexit runs — a defer cannot cancel a Goexit the way it
/// can cancel a panic — and golib's recover path captures only panic-convertible exceptions
/// (<c>GoFunc.Execute</c>'s filter, <c>RuntimeErrorPanic.TryAsPanic</c>). Because this type fails
/// that filter, it is invisible to <c>recover()</c> BY CONSTRUCTION: the recover path needs no
/// knowledge of Goexit at all.
/// </para>
/// <para>
/// The deferred calls still run. <c>GoFunc.HandleFinally</c> sits in a <c>finally</c>, so an
/// unwinding <see cref="GoexitException"/> pops the defer stack in the usual order exactly as a
/// panic does.
/// </para>
/// <para>
/// The goroutine root (<c>golib.Goroutine.Run</c>) swallows it, ending THAT goroutine and no other;
/// a <see cref="PanicException"/> reaching the same point keeps its fatal-crash path unchanged.
/// </para>
/// </remarks>
[DebuggerNonUserCode]
public class GoexitException : Exception
{
    public GoexitException() : base("runtime.Goexit")
    {
    }
}
