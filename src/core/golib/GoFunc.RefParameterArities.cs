// GoFunc.RefParameterArities.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// ReSharper disable CheckNamespace

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using go.golib;

namespace go;

// ---------------------------------------------------------------------------------------------
// REF-PARAMETER ARITIES — GoFunc<TRef1 … TRefN, T> for N = 2 through 16.
//
// WHAT LIVES HERE
//   The upper rungs of the execution-context ladder: one sealed class per count of variables a
//   converted Go function threads through BY REFERENCE. Every rung is the same six members —
//   two delegates, a field, two constructors, an Execute — with one more `ref TRefN refN` woven
//   through each of them, so this file is repetition rather than content. The logic a reader
//   comes looking for (capturing a panic so `recover()` can see it, draining the deferred stack
//   on every exit path) lives once in `GoFunc<T>` over in GoFunc.cs, which every rung here
//   derives from.
//
// WHY ONE CLASS PER ARITY RATHER THAN ONE VARIADIC FORM
//   A `ref` parameter list is part of a delegate's TYPE, and C# has no variadic `ref` — there is
//   no way to spell "some number of by-reference parameters" in a signature. A `params object[]`
//   form would compile and would box every argument and hand the body a COPY, which is exactly
//   the bug the ladder exists to prevent: a C# lambda cannot capture a `ref` local at all, yet
//   Go closures routinely mutate their enclosing function's variables. Named results are the
//   case `defer` exists to modify:
//
//       // Go:  func f() (err error) { defer func() { if err != nil { … } }(); … }
//
//   The deferred body has to see and write the SAME `err` the function returns, so `err` is
//   threaded as a `ref` parameter and `Execute(ref err)` hands it back to the body. Sixteen
//   rungs cover the observed corpus; the ceiling is arbitrary only in that a seventeenth would
//   be more of the same.
//
// ONLY THE ONE-REF RUNG ADMITS A `ref struct`
//   `GoFunc<TRef1, T>` in GoFunc.cs carries `where TRef1 : allows ref struct`; no rung here does.
//   So a `Span<T>`-shaped local can be ref-threaded through a function that threads exactly ONE
//   variable, and not through one that threads two. That asymmetry is a real limit, not an
//   oversight to paper over silently: widening it means adding the anti-constraint to every type
//   parameter of a rung AND to the matching `func` overload, never to one of the pair.
//
// THE OTHER HALF OF THE LADDER
//   builtin.FuncExecutionContexts.cs holds the `func(ref …, …)` overloads that converted code
//   actually calls; each one constructs the rung of matching arity from this file. A rung with no
//   `func` overload is unreachable, and a `func` overload with no rung does not compile — add or
//   change both together.
//
// REGENERATING IT
//   The rungs were first emitted by the `GenGoFuncRefInstances` utility (src/Utilities), which
//   still writes exactly this two-file shape: `GoFuncRef.cs` for the types, `BuiltInGoFuncRef.cs`
//   for the `func` overloads. Its templates have since DRIFTED from the live code — they emit a
//   `panic` delegate parameter, a `HandlePanic` handler, and a bare `catch (PanicException ex)`.
//   The live rungs instead filter with `RuntimeErrorPanic.TryAsPanic`, which is what makes a .NET
//   exception that maps to a Go runtime panic (integer divide by zero, nil dereference)
//   RECOVERABLE rather than fatal, and then call `CaptureThrowSite` so the traceback names the
//   throw site. Pasting a fresh generator run over this file compiles cleanly and quietly hands
//   all sixteen rungs back the older, narrower panic behavior. Extend by hand, or fix the
//   templates first.
//
// PERFORMANCE AND DEBUGGING SHAPE — KEEP IT
//   Every `Execute` carries `[MethodImpl(MethodImplOptions.AggressiveInlining),
//   DebuggerStepperBoundary]`, and both belong on any rung added. The inlining hint asks the JIT
//   to fold away the forward to the body delegate wherever it can; the stepper boundary is the
//   load-bearing one, because without it a step-into from converted Go code lands in this runtime
//   plumbing instead of in the Go function body the developer was actually stepping into. These
//   rungs sit on the call path of every converted function that defers or recovers, which in the
//   stdlib corpus includes hot loops — so this file is not a candidate for reflection, dynamic
//   dispatch or any other "one clever method instead of sixteen dull ones" rewrite.
// ---------------------------------------------------------------------------------------------

/// <summary>
/// Represents a Go function execution context with 2 reference parameters for handling "defer" and "recover" keywords.
/// </summary>
public sealed class GoFunc<TRef1, TRef2, T> : GoFunc<T>
{
    public delegate void GoRefAction(ref TRef1 ref1, ref TRef2 ref2, Defer defer, Recover recover);
    public delegate T GoRefFunction(ref TRef1 ref1, ref TRef2 ref2, Defer defer, Recover recover);

    private readonly GoRefFunction m_function;

    public GoFunc(GoRefAction action)
    {
        m_function = (ref TRef1 ref1, ref TRef2 ref2, Defer defer, Recover recover) =>
        {
            action(ref ref1, ref ref2, defer, recover);
            return default!;
        };
    }

    public GoFunc(GoRefFunction function)
    {
        m_function = function;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepperBoundary]
    public T Execute(ref TRef1 ref1, ref TRef2 ref2)
    {
        T result = default!;

        try
        {
            result = m_function(ref ref1, ref ref2, HandleDefer, HandleRecover);
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

/// <summary>
/// Represents a Go function execution context with 3 reference parameters for handling "defer" and "recover" keywords.
/// </summary>
public sealed class GoFunc<TRef1, TRef2, TRef3, T> : GoFunc<T>
{
    public delegate void GoRefAction(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, Defer defer, Recover recover);
    public delegate T GoRefFunction(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, Defer defer, Recover recover);

    private readonly GoRefFunction m_function;

    public GoFunc(GoRefAction action)
    {
        m_function = (ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, Defer defer, Recover recover) =>
        {
            action(ref ref1, ref ref2, ref ref3, defer, recover);
            return default!;
        };
    }

    public GoFunc(GoRefFunction function)
    {
        m_function = function;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepperBoundary]
    public T Execute(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3)
    {
        T result = default!;

        try
        {
            result = m_function(ref ref1, ref ref2, ref ref3, HandleDefer, HandleRecover);
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

/// <summary>
/// Represents a Go function execution context with 4 reference parameters for handling "defer" and "recover" keywords.
/// </summary>
public sealed class GoFunc<TRef1, TRef2, TRef3, TRef4, T> : GoFunc<T>
{
    public delegate void GoRefAction(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, Defer defer, Recover recover);
    public delegate T GoRefFunction(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, Defer defer, Recover recover);

    private readonly GoRefFunction m_function;

    public GoFunc(GoRefAction action)
    {
        m_function = (ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, Defer defer, Recover recover) =>
        {
            action(ref ref1, ref ref2, ref ref3, ref ref4, defer, recover);
            return default!;
        };
    }

    public GoFunc(GoRefFunction function)
    {
        m_function = function;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepperBoundary]
    public T Execute(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4)
    {
        T result = default!;

        try
        {
            result = m_function(ref ref1, ref ref2, ref ref3, ref ref4, HandleDefer, HandleRecover);
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

/// <summary>
/// Represents a Go function execution context with 5 reference parameters for handling "defer" and "recover" keywords.
/// </summary>
public sealed class GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, T> : GoFunc<T>
{
    public delegate void GoRefAction(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, Defer defer, Recover recover);
    public delegate T GoRefFunction(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, Defer defer, Recover recover);

    private readonly GoRefFunction m_function;

    public GoFunc(GoRefAction action)
    {
        m_function = (ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, Defer defer, Recover recover) =>
        {
            action(ref ref1, ref ref2, ref ref3, ref ref4, ref ref5, defer, recover);
            return default!;
        };
    }

    public GoFunc(GoRefFunction function)
    {
        m_function = function;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepperBoundary]
    public T Execute(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5)
    {
        T result = default!;

        try
        {
            result = m_function(ref ref1, ref ref2, ref ref3, ref ref4, ref ref5, HandleDefer, HandleRecover);
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

/// <summary>
/// Represents a Go function execution context with 6 reference parameters for handling "defer" and "recover" keywords.
/// </summary>
public sealed class GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, T> : GoFunc<T>
{
    public delegate void GoRefAction(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, Defer defer, Recover recover);
    public delegate T GoRefFunction(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, Defer defer, Recover recover);

    private readonly GoRefFunction m_function;

    public GoFunc(GoRefAction action)
    {
        m_function = (ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, Defer defer, Recover recover) =>
        {
            action(ref ref1, ref ref2, ref ref3, ref ref4, ref ref5, ref ref6, defer, recover);
            return default!;
        };
    }

    public GoFunc(GoRefFunction function)
    {
        m_function = function;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepperBoundary]
    public T Execute(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6)
    {
        T result = default!;

        try
        {
            result = m_function(ref ref1, ref ref2, ref ref3, ref ref4, ref ref5, ref ref6, HandleDefer, HandleRecover);
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

/// <summary>
/// Represents a Go function execution context with 7 reference parameters for handling "defer" and "recover" keywords.
/// </summary>
public sealed class GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, T> : GoFunc<T>
{
    public delegate void GoRefAction(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, Defer defer, Recover recover);
    public delegate T GoRefFunction(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, Defer defer, Recover recover);

    private readonly GoRefFunction m_function;

    public GoFunc(GoRefAction action)
    {
        m_function = (ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, Defer defer, Recover recover) =>
        {
            action(ref ref1, ref ref2, ref ref3, ref ref4, ref ref5, ref ref6, ref ref7, defer, recover);
            return default!;
        };
    }

    public GoFunc(GoRefFunction function)
    {
        m_function = function;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepperBoundary]
    public T Execute(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7)
    {
        T result = default!;

        try
        {
            result = m_function(ref ref1, ref ref2, ref ref3, ref ref4, ref ref5, ref ref6, ref ref7, HandleDefer, HandleRecover);
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

/// <summary>
/// Represents a Go function execution context with 8 reference parameters for handling "defer" and "recover" keywords.
/// </summary>
public sealed class GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, T> : GoFunc<T>
{
    public delegate void GoRefAction(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, Defer defer, Recover recover);
    public delegate T GoRefFunction(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, Defer defer, Recover recover);

    private readonly GoRefFunction m_function;

    public GoFunc(GoRefAction action)
    {
        m_function = (ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, Defer defer, Recover recover) =>
        {
            action(ref ref1, ref ref2, ref ref3, ref ref4, ref ref5, ref ref6, ref ref7, ref ref8, defer, recover);
            return default!;
        };
    }

    public GoFunc(GoRefFunction function)
    {
        m_function = function;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepperBoundary]
    public T Execute(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8)
    {
        T result = default!;

        try
        {
            result = m_function(ref ref1, ref ref2, ref ref3, ref ref4, ref ref5, ref ref6, ref ref7, ref ref8, HandleDefer, HandleRecover);
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

/// <summary>
/// Represents a Go function execution context with 9 reference parameters for handling "defer" and "recover" keywords.
/// </summary>
public sealed class GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, T> : GoFunc<T>
{
    public delegate void GoRefAction(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, Defer defer, Recover recover);
    public delegate T GoRefFunction(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, Defer defer, Recover recover);

    private readonly GoRefFunction m_function;

    public GoFunc(GoRefAction action)
    {
        m_function = (ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, Defer defer, Recover recover) =>
        {
            action(ref ref1, ref ref2, ref ref3, ref ref4, ref ref5, ref ref6, ref ref7, ref ref8, ref ref9, defer, recover);
            return default!;
        };
    }

    public GoFunc(GoRefFunction function)
    {
        m_function = function;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepperBoundary]
    public T Execute(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9)
    {
        T result = default!;

        try
        {
            result = m_function(ref ref1, ref ref2, ref ref3, ref ref4, ref ref5, ref ref6, ref ref7, ref ref8, ref ref9, HandleDefer, HandleRecover);
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

/// <summary>
/// Represents a Go function execution context with 10 reference parameters for handling "defer" and "recover" keywords.
/// </summary>
public sealed class GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, T> : GoFunc<T>
{
    public delegate void GoRefAction(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10, Defer defer, Recover recover);
    public delegate T GoRefFunction(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10, Defer defer, Recover recover);

    private readonly GoRefFunction m_function;

    public GoFunc(GoRefAction action)
    {
        m_function = (ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10, Defer defer, Recover recover) =>
        {
            action(ref ref1, ref ref2, ref ref3, ref ref4, ref ref5, ref ref6, ref ref7, ref ref8, ref ref9, ref ref10, defer, recover);
            return default!;
        };
    }

    public GoFunc(GoRefFunction function)
    {
        m_function = function;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepperBoundary]
    public T Execute(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10)
    {
        T result = default!;

        try
        {
            result = m_function(ref ref1, ref ref2, ref ref3, ref ref4, ref ref5, ref ref6, ref ref7, ref ref8, ref ref9, ref ref10, HandleDefer, HandleRecover);
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

/// <summary>
/// Represents a Go function execution context with 11 reference parameters for handling "defer" and "recover" keywords.
/// </summary>
public sealed class GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11, T> : GoFunc<T>
{
    public delegate void GoRefAction(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10, ref TRef11 ref11, Defer defer, Recover recover);
    public delegate T GoRefFunction(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10, ref TRef11 ref11, Defer defer, Recover recover);

    private readonly GoRefFunction m_function;

    public GoFunc(GoRefAction action)
    {
        m_function = (ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10, ref TRef11 ref11, Defer defer, Recover recover) =>
        {
            action(ref ref1, ref ref2, ref ref3, ref ref4, ref ref5, ref ref6, ref ref7, ref ref8, ref ref9, ref ref10, ref ref11, defer, recover);
            return default!;
        };
    }

    public GoFunc(GoRefFunction function)
    {
        m_function = function;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepperBoundary]
    public T Execute(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10, ref TRef11 ref11)
    {
        T result = default!;

        try
        {
            result = m_function(ref ref1, ref ref2, ref ref3, ref ref4, ref ref5, ref ref6, ref ref7, ref ref8, ref ref9, ref ref10, ref ref11, HandleDefer, HandleRecover);
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

/// <summary>
/// Represents a Go function execution context with 12 reference parameters for handling "defer" and "recover" keywords.
/// </summary>
public sealed class GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11, TRef12, T> : GoFunc<T>
{
    public delegate void GoRefAction(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10, ref TRef11 ref11, ref TRef12 ref12, Defer defer, Recover recover);
    public delegate T GoRefFunction(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10, ref TRef11 ref11, ref TRef12 ref12, Defer defer, Recover recover);

    private readonly GoRefFunction m_function;

    public GoFunc(GoRefAction action)
    {
        m_function = (ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10, ref TRef11 ref11, ref TRef12 ref12, Defer defer, Recover recover) =>
        {
            action(ref ref1, ref ref2, ref ref3, ref ref4, ref ref5, ref ref6, ref ref7, ref ref8, ref ref9, ref ref10, ref ref11, ref ref12, defer, recover);
            return default!;
        };
    }

    public GoFunc(GoRefFunction function)
    {
        m_function = function;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepperBoundary]
    public T Execute(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10, ref TRef11 ref11, ref TRef12 ref12)
    {
        T result = default!;

        try
        {
            result = m_function(ref ref1, ref ref2, ref ref3, ref ref4, ref ref5, ref ref6, ref ref7, ref ref8, ref ref9, ref ref10, ref ref11, ref ref12, HandleDefer, HandleRecover);
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

/// <summary>
/// Represents a Go function execution context with 13 reference parameters for handling "defer" and "recover" keywords.
/// </summary>
public sealed class GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11, TRef12, TRef13, T> : GoFunc<T>
{
    public delegate void GoRefAction(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10, ref TRef11 ref11, ref TRef12 ref12, ref TRef13 ref13, Defer defer, Recover recover);
    public delegate T GoRefFunction(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10, ref TRef11 ref11, ref TRef12 ref12, ref TRef13 ref13, Defer defer, Recover recover);

    private readonly GoRefFunction m_function;

    public GoFunc(GoRefAction action)
    {
        m_function = (ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10, ref TRef11 ref11, ref TRef12 ref12, ref TRef13 ref13, Defer defer, Recover recover) =>
        {
            action(ref ref1, ref ref2, ref ref3, ref ref4, ref ref5, ref ref6, ref ref7, ref ref8, ref ref9, ref ref10, ref ref11, ref ref12, ref ref13, defer, recover);
            return default!;
        };
    }

    public GoFunc(GoRefFunction function)
    {
        m_function = function;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepperBoundary]
    public T Execute(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10, ref TRef11 ref11, ref TRef12 ref12, ref TRef13 ref13)
    {
        T result = default!;

        try
        {
            result = m_function(ref ref1, ref ref2, ref ref3, ref ref4, ref ref5, ref ref6, ref ref7, ref ref8, ref ref9, ref ref10, ref ref11, ref ref12, ref ref13, HandleDefer, HandleRecover);
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

/// <summary>
/// Represents a Go function execution context with 14 reference parameters for handling "defer" and "recover" keywords.
/// </summary>
public sealed class GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11, TRef12, TRef13, TRef14, T> : GoFunc<T>
{
    public delegate void GoRefAction(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10, ref TRef11 ref11, ref TRef12 ref12, ref TRef13 ref13, ref TRef14 ref14, Defer defer, Recover recover);
    public delegate T GoRefFunction(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10, ref TRef11 ref11, ref TRef12 ref12, ref TRef13 ref13, ref TRef14 ref14, Defer defer, Recover recover);

    private readonly GoRefFunction m_function;

    public GoFunc(GoRefAction action)
    {
        m_function = (ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10, ref TRef11 ref11, ref TRef12 ref12, ref TRef13 ref13, ref TRef14 ref14, Defer defer, Recover recover) =>
        {
            action(ref ref1, ref ref2, ref ref3, ref ref4, ref ref5, ref ref6, ref ref7, ref ref8, ref ref9, ref ref10, ref ref11, ref ref12, ref ref13, ref ref14, defer, recover);
            return default!;
        };
    }

    public GoFunc(GoRefFunction function)
    {
        m_function = function;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepperBoundary]
    public T Execute(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10, ref TRef11 ref11, ref TRef12 ref12, ref TRef13 ref13, ref TRef14 ref14)
    {
        T result = default!;

        try
        {
            result = m_function(ref ref1, ref ref2, ref ref3, ref ref4, ref ref5, ref ref6, ref ref7, ref ref8, ref ref9, ref ref10, ref ref11, ref ref12, ref ref13, ref ref14, HandleDefer, HandleRecover);
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

/// <summary>
/// Represents a Go function execution context with 15 reference parameters for handling "defer" and "recover" keywords.
/// </summary>
public sealed class GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11, TRef12, TRef13, TRef14, TRef15, T> : GoFunc<T>
{
    public delegate void GoRefAction(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10, ref TRef11 ref11, ref TRef12 ref12, ref TRef13 ref13, ref TRef14 ref14, ref TRef15 ref15, Defer defer, Recover recover);
    public delegate T GoRefFunction(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10, ref TRef11 ref11, ref TRef12 ref12, ref TRef13 ref13, ref TRef14 ref14, ref TRef15 ref15, Defer defer, Recover recover);

    private readonly GoRefFunction m_function;

    public GoFunc(GoRefAction action)
    {
        m_function = (ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10, ref TRef11 ref11, ref TRef12 ref12, ref TRef13 ref13, ref TRef14 ref14, ref TRef15 ref15, Defer defer, Recover recover) =>
        {
            action(ref ref1, ref ref2, ref ref3, ref ref4, ref ref5, ref ref6, ref ref7, ref ref8, ref ref9, ref ref10, ref ref11, ref ref12, ref ref13, ref ref14, ref ref15, defer, recover);
            return default!;
        };
    }

    public GoFunc(GoRefFunction function)
    {
        m_function = function;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepperBoundary]
    public T Execute(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10, ref TRef11 ref11, ref TRef12 ref12, ref TRef13 ref13, ref TRef14 ref14, ref TRef15 ref15)
    {
        T result = default!;

        try
        {
            result = m_function(ref ref1, ref ref2, ref ref3, ref ref4, ref ref5, ref ref6, ref ref7, ref ref8, ref ref9, ref ref10, ref ref11, ref ref12, ref ref13, ref ref14, ref ref15, HandleDefer, HandleRecover);
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

/// <summary>
/// Represents a Go function execution context with 16 reference parameters for handling "defer" and "recover" keywords.
/// </summary>
public sealed class GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11, TRef12, TRef13, TRef14, TRef15, TRef16, T> : GoFunc<T>
{
    public delegate void GoRefAction(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10, ref TRef11 ref11, ref TRef12 ref12, ref TRef13 ref13, ref TRef14 ref14, ref TRef15 ref15, ref TRef16 ref16, Defer defer, Recover recover);
    public delegate T GoRefFunction(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10, ref TRef11 ref11, ref TRef12 ref12, ref TRef13 ref13, ref TRef14 ref14, ref TRef15 ref15, ref TRef16 ref16, Defer defer, Recover recover);

    private readonly GoRefFunction m_function;

    public GoFunc(GoRefAction action)
    {
        m_function = (ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10, ref TRef11 ref11, ref TRef12 ref12, ref TRef13 ref13, ref TRef14 ref14, ref TRef15 ref15, ref TRef16 ref16, Defer defer, Recover recover) =>
        {
            action(ref ref1, ref ref2, ref ref3, ref ref4, ref ref5, ref ref6, ref ref7, ref ref8, ref ref9, ref ref10, ref ref11, ref ref12, ref ref13, ref ref14, ref ref15, ref ref16, defer, recover);
            return default!;
        };
    }

    public GoFunc(GoRefFunction function)
    {
        m_function = function;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepperBoundary]
    public T Execute(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10, ref TRef11 ref11, ref TRef12 ref12, ref TRef13 ref13, ref TRef14 ref14, ref TRef15 ref15, ref TRef16 ref16)
    {
        T result = default!;

        try
        {
            result = m_function(ref ref1, ref ref2, ref ref3, ref ref4, ref ref5, ref ref6, ref ref7, ref ref8, ref ref9, ref ref10, ref ref11, ref ref12, ref ref13, ref ref14, ref ref15, ref ref16, HandleDefer, HandleRecover);
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
