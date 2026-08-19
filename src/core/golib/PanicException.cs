// PanicException.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;
using System.Diagnostics;
using System.Reflection;
using go.golib;

namespace go;

/// <summary>
/// Represents an exception for the "panic" keyword.
/// </summary>
[DebuggerNonUserCode]
public class PanicException(object? state, Exception? innerException = null) :
    Exception(null, innerException)
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public object? State { get; } = state;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string? m_message;

    /// <summary>
    /// The panic value as Go's runtime reports it — <see cref="PanicText"/> of <see cref="State"/>.
    /// </summary>
    /// <remarks>
    /// Computed on FIRST READ, not at construction, because that is when Go computes it: Go runs
    /// <c>preprintpanics</c> only once a panic has gone unrecovered and is about to be printed. A
    /// recovered panic — <c>fmt</c>'s catchPanic, <c>text/template</c>'s errRecover, and every
    /// <c>defer func(){ recover() }()</c> in the corpus — therefore never calls a user
    /// <c>Error()</c>/<c>String()</c> at all, exactly as in Go, and pays nothing for the rule.
    /// </remarks>
    public override string Message => m_message ??= PanicText(State);

    /// <summary>
    /// Renders a panic VALUE the way Go's runtime prints it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Go's <c>preprintpanics</c> (runtime/panic.go) substitutes before anything is printed: an
    /// <c>error</c> panic value becomes its <c>Error()</c>, a <c>Stringer</c> its <c>String()</c>.
    /// Without that rule a converted <c>panic(err)</c> renders the managed value itself — for a
    /// pointer-held error, its ADDRESS (<c>panic: 0x211163e3340</c>) — which is not merely
    /// unlike Go, it destroys the one piece of information a panic report exists to carry. It cost
    /// the row-harvest-2 lane a diagnostic round-trip on the only defect it was chasing.
    /// </para>
    /// <para>
    /// The <c>Stringer</c> arm is not redundant with <c>ToString()</c>. A Go named type's generated
    /// <c>ToString()</c> forwards to its UNDERLYING value (go2cs-gen's InheritedTypeTemplate), so
    /// <c>panic(2 * time.Second)</c> would print <c>2000000000</c> where Go prints <c>2s</c>. The
    /// method is found the way golib's <c>error&lt;T&gt;</c> finds <c>Error</c> — through the
    /// extension-method registry, which is where a converted Go method lives.
    /// </para>
    /// <para>
    /// Go throws a fatal <c>"panic while printing panic value"</c> if that substitution itself
    /// panics. Reproducing the FATALITY from a <see cref="Exception.Message"/> getter would be
    /// worse than the divergence it reports, so the text is returned instead of thrown.
    /// </para>
    /// </remarks>
    public static string PanicText(object? state)
    {
        switch (state)
        {
            case null:
                return "nil";
            // Ahead of the method probes: this is the commonest panic value by far, and its own
            // ToString is already the Go rendering.
            case @string text:
                return text.ToString();
        }

        try
        {
            if (state is error goError)
                return goError.Error().ToString();

            if (TryGoStringMethod(state, out string stringer))
                return stringer;
        }
        catch (Exception ex) when (ex is not GoexitException)
        {
            return "panic while printing panic value";
        }

        return state.ToString() ?? "nil";
    }

    // Go's `stringer` is the structural `interface{ String() string }`, so this asks the same
    // question of the managed value: does its Go method set carry `String`? A converted Go method is
    // an extension method over its receiver, and the registry's precedence comparer can hand back
    // one declared for a DIFFERENT receiver shape (the ж<T> form for a value, or the reverse), so
    // the receiver is re-checked before the call rather than trusted.
    private static bool TryGoStringMethod(object state, out string text)
    {
        text = "";

        if (state.GetType().GetExtensionMethod("String") is not { } method ||
            method.ReturnType != typeof(@string) ||
            method.GetParameters() is not { Length: 1 } parameters ||
            !parameters[0].ParameterType.IsInstanceOfType(state))
        {
            return false;
        }

        text = ((@string)method.Invoke(null, [state])!).ToString();
        return true;
    }

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

    /// <summary>
    /// Reports the panic SITE first, then the frames the panic unwound through to reach the reader —
    /// the closest a CLR exception can come to Go's single, uninterrupted panic traceback.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The base property cannot answer this on its own. A panic reaches its reporter by being
    /// re-raised from <c>GoFunc.HandleFinally</c> (<c>throw CapturedPanic.Value</c>, after the
    /// deferred sequence declined to recover it), and re-raising a stored instance RESETS
    /// <see cref="Exception.StackTrace"/> to the re-raise point. Worse, a runtime-error panic — a nil
    /// dereference, a divide by zero — is SYNTHESIZED by <see cref="golib.RuntimeErrorPanic"/> from
    /// the .NET exception and was never thrown at the fault site at all, so its base trace can only
    /// ever name the re-raise. Both cases print the same useless frame: every panic in the corpus
    /// appears to originate inside the defer machinery, and the real fault site — the whole reason a
    /// traceback is read — is gone.
    /// </para>
    /// <para>
    /// <see cref="PanicTrace"/> already holds the origin, snapshotted once at the first catch, and is
    /// how <c>runtime.Stack</c> reproduces Go's traceback. This makes every OTHER reader — the
    /// Phase-4 test host, an unhandled-exception dump, a debugger — see it too, without any of them
    /// having to know the panic machinery exists.
    /// </para>
    /// </remarks>
    public override string? StackTrace
    {
        get
        {
            string? unwound = base.StackTrace;

            if (PanicTrace is null || PanicTrace.FrameCount == 0)
                return unwound;

            // StackTrace.ToString() terminates its final frame with a newline; Exception.StackTrace
            // does not — so the origin concatenates onto the unwind cleanly, and trims to match the
            // base property's shape when there is no unwind to append.
            string origin = PanicTrace.ToString();

            return unwound is null ? origin.TrimEnd() : origin + unwound;
        }
    }

    // Snapshot the throw site the first time this panic is caught. `thrown` is the exception that
    // actually travelled: for a mapped .NET runtime error (nil deref, divide by zero) THIS instance
    // was synthesized by RuntimeErrorPanic and was never thrown, so only the original carries frames.
    internal void CaptureThrowSite(Exception thrown)
    {
        PanicTrace ??= new StackTrace(thrown, fNeedFileInfo: true);
    }

    // Adopt the origin of the panic being handled when this one is raised from a deferred call —
    // Go's traceback there still shows the original panic's frames. Deliberately UNCONDITIONAL,
    // overriding CaptureThrowSite's first-adoption snapshot: the drain's own IsPanic filter adopts
    // the new panic (at the deferred call's site) before the drain can express the inheritance, and
    // an explicit re-panic inheritance is the stronger semantic at this method's one call site.
    internal void InheritThrowSite(PanicException origin)
    {
        PanicTrace = origin.PanicTrace ?? PanicTrace;
    }
}
