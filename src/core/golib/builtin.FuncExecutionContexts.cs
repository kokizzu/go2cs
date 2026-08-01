// builtin.FuncExecutionContexts.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// ReSharper disable InconsistentNaming

namespace go;

// ---------------------------------------------------------------------------------------------
// FUNCTION EXECUTION CONTEXTS — every `func` overload, i.e. the scope that gives a converted Go
// function its `defer`, `panic` and `recover`.
//
// WHAT LIVES HERE
//   `func(…)` overloads that wrap a converted body in a `GoFunc<…>` and run it. The converter
//   emits one of these around ANY Go function that defers or recovers, so the body arrives as a
//   lambda receiving the two delegates it needs:
//
//       // Go:  func f() { defer g(); … }
//       func((defer, _) => { deferǃ(g, defer); … });
//
//   `GoFunc.Execute` then does what Go's runtime does for free and C# has no equivalent for: it
//   catches a panic (an explicit `panic()` or a .NET exception that maps to a Go runtime panic)
//   into a slot `recover()` can read, and drains the deferred stack in a `finally` so the defers
//   run on every exit path.
//
// WHY AN ARITY LADDER OF `ref` PARAMETERS
//   A C# lambda cannot capture a `ref` local or a `ref struct`, but Go closures routinely mutate
//   the enclosing function's variables — including named results, which `defer` exists to modify.
//   So each variable the body must mutate BY REFERENCE is threaded through as an explicit
//   `ref TRefN` parameter rather than captured, and `GoFunc.Execute(ref …)` hands it back to the
//   body. Sixteen rungs cover the observed corpus. Every rung is generic, so nothing is boxed on
//   the way through.
//
//   Only the ONE-ref rung carries `allows ref struct`, so a `Span`-shaped local can be threaded
//   through a function that ref-threads exactly one variable and not through one that threads two.
//   Read that as a real limit rather than an accident to quietly widen: adding the anti-constraint
//   means adding it to the `func` overload AND to its `GoFunc<…>` rung, never to one of the pair.
//
// EXTENDING IT
//   Add a rung only when a corpus function needs that many ref-threaded variables, and add both
//   the void (`GoRefAction`) and value-returning (`GoRefFunction`) forms. A matching `GoFunc<…>`
//   type must exist first — that is the ladder's other half, and it lives in GoFunc.cs for the
//   zero- and one-ref rungs and in GoFunc.RefParameterArities.cs for rungs 2 through 16.
// ---------------------------------------------------------------------------------------------
public static partial class builtin
{
    // ** Go Function Execution Context Handlers **/

    /// <summary>
    /// Executes a Go function with no return value.
    /// </summary>
    /// <param name="action">Go function to execute called with defer and recover function references.</param>
    public static void func(GoFunc<object>.GoAction action)
    {
        new GoFunc<object>(action).Execute();
    }

    /// <summary>
    /// Executes a Go function with a return value.
    /// </summary>
    /// <param name="function">Go function to execute called with defer and recover function references.</param>
    public static T func<T>(GoFunc<T>.GoFunction function)
    {
        return new GoFunc<T>(function).Execute();
    }

    /// <summary>
    /// Executes a Go function with 1 reference parameter and no return value.
    /// </summary>
    /// <param name="ref1">Reference parameter 1.</param>
    /// <param name="action">Go function to execute called with defer and recover function references.</param>
    public static void func<TRef1>(ref TRef1 ref1, GoFunc<TRef1, object>.GoRefAction action)
        where TRef1 : allows ref struct
    {
        new GoFunc<TRef1, object>(action).Execute(ref ref1);
    }

    /// <summary>
    /// Executes a Go function with 1 reference parameter and a return value.
    /// </summary>
    /// <param name="ref1">Reference parameter 1.</param>
    /// <param name="function">Go function to execute called with defer and recover function references.</param>
    public static T func<TRef1, T>(ref TRef1 ref1, GoFunc<TRef1, T>.GoRefFunction function)
        where TRef1 : allows ref struct
    {
        return new GoFunc<TRef1, T>(function).Execute(ref ref1);
    }

    #region [ func<TRef1, TRef2, ... TRef16> Implementations ]

    /*  The following code was generated using the "GenGoFuncRefInstances" utility: */

    /// <summary>
    /// Executes a Go function with 2 reference parameters and no return value.
    /// </summary>
    /// <param name="ref1">Reference parameter 1.</param>
    /// <param name="ref2">Reference parameter 2.</param>
    /// <param name="action">Go function to execute called with defer and recover function references.</param>
    public static void func<TRef1, TRef2>(ref TRef1 ref1, ref TRef2 ref2, GoFunc<TRef1, TRef2, object>.GoRefAction action)
    {
        new GoFunc<TRef1, TRef2, object>(action).Execute(ref ref1, ref ref2);
    }

    /// <summary>
    /// Executes a Go function with 2 reference parameters and a return value.
    /// </summary>
    /// <param name="ref1">Reference parameter 1.</param>
    /// <param name="ref2">Reference parameter 2.</param>
    /// <param name="function">Go function to execute called with defer and recover function references.</param>
    public static T func<TRef1, TRef2, T>(ref TRef1 ref1, ref TRef2 ref2, GoFunc<TRef1, TRef2, T>.GoRefFunction function)
    {
        return new GoFunc<TRef1, TRef2, T>(function).Execute(ref ref1, ref ref2);
    }

    /// <summary>
    /// Executes a Go function with 3 reference parameters and no return value.
    /// </summary>
    /// <param name="ref1">Reference parameter 1.</param>
    /// <param name="ref2">Reference parameter 2.</param>
    /// <param name="ref3">Reference parameter 3.</param>
    /// <param name="action">Go function to execute called with defer and recover function references.</param>
    public static void func<TRef1, TRef2, TRef3>(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, GoFunc<TRef1, TRef2, TRef3, object>.GoRefAction action)
    {
        new GoFunc<TRef1, TRef2, TRef3, object>(action).Execute(ref ref1, ref ref2, ref ref3);
    }

    /// <summary>
    /// Executes a Go function with 3 reference parameters and a return value.
    /// </summary>
    /// <param name="ref1">Reference parameter 1.</param>
    /// <param name="ref2">Reference parameter 2.</param>
    /// <param name="ref3">Reference parameter 3.</param>
    /// <param name="function">Go function to execute called with defer and recover function references.</param>
    public static T func<TRef1, TRef2, TRef3, T>(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, GoFunc<TRef1, TRef2, TRef3, T>.GoRefFunction function)
    {
        return new GoFunc<TRef1, TRef2, TRef3, T>(function).Execute(ref ref1, ref ref2, ref ref3);
    }

    /// <summary>
    /// Executes a Go function with 4 reference parameters and no return value.
    /// </summary>
    /// <param name="ref1">Reference parameter 1.</param>
    /// <param name="ref2">Reference parameter 2.</param>
    /// <param name="ref3">Reference parameter 3.</param>
    /// <param name="ref4">Reference parameter 4.</param>
    /// <param name="action">Go function to execute called with defer and recover function references.</param>
    public static void func<TRef1, TRef2, TRef3, TRef4>(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, GoFunc<TRef1, TRef2, TRef3, TRef4, object>.GoRefAction action)
    {
        new GoFunc<TRef1, TRef2, TRef3, TRef4, object>(action).Execute(ref ref1, ref ref2, ref ref3, ref ref4);
    }

    /// <summary>
    /// Executes a Go function with 4 reference parameters and a return value.
    /// </summary>
    /// <param name="ref1">Reference parameter 1.</param>
    /// <param name="ref2">Reference parameter 2.</param>
    /// <param name="ref3">Reference parameter 3.</param>
    /// <param name="ref4">Reference parameter 4.</param>
    /// <param name="function">Go function to execute called with defer and recover function references.</param>
    public static T func<TRef1, TRef2, TRef3, TRef4, T>(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, GoFunc<TRef1, TRef2, TRef3, TRef4, T>.GoRefFunction function)
    {
        return new GoFunc<TRef1, TRef2, TRef3, TRef4, T>(function).Execute(ref ref1, ref ref2, ref ref3, ref ref4);
    }

    /// <summary>
    /// Executes a Go function with 5 reference parameters and no return value.
    /// </summary>
    /// <param name="ref1">Reference parameter 1.</param>
    /// <param name="ref2">Reference parameter 2.</param>
    /// <param name="ref3">Reference parameter 3.</param>
    /// <param name="ref4">Reference parameter 4.</param>
    /// <param name="ref5">Reference parameter 5.</param>
    /// <param name="action">Go function to execute called with defer and recover function references.</param>
    public static void func<TRef1, TRef2, TRef3, TRef4, TRef5>(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, object>.GoRefAction action)
    {
        new GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, object>(action).Execute(ref ref1, ref ref2, ref ref3, ref ref4,
            ref ref5);
    }

    /// <summary>
    /// Executes a Go function with 5 reference parameters and a return value.
    /// </summary>
    /// <param name="ref1">Reference parameter 1.</param>
    /// <param name="ref2">Reference parameter 2.</param>
    /// <param name="ref3">Reference parameter 3.</param>
    /// <param name="ref4">Reference parameter 4.</param>
    /// <param name="ref5">Reference parameter 5.</param>
    /// <param name="function">Go function to execute called with defer and recover function references.</param>
    public static T func<TRef1, TRef2, TRef3, TRef4, TRef5, T>(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, T>.GoRefFunction function)
    {
        return new GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, T>(function).Execute(ref ref1, ref ref2, ref ref3,
            ref ref4, ref ref5);
    }

    /// <summary>
    /// Executes a Go function with 6 reference parameters and no return value.
    /// </summary>
    /// <param name="ref1">Reference parameter 1.</param>
    /// <param name="ref2">Reference parameter 2.</param>
    /// <param name="ref3">Reference parameter 3.</param>
    /// <param name="ref4">Reference parameter 4.</param>
    /// <param name="ref5">Reference parameter 5.</param>
    /// <param name="ref6">Reference parameter 6.</param>
    /// <param name="action">Go function to execute called with defer and recover function references.</param>
    public static void func<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6>(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, object>.GoRefAction action)
    {
        new GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, object>(action).Execute(ref ref1, ref ref2, ref ref3,
            ref ref4, ref ref5, ref ref6);
    }

    /// <summary>
    /// Executes a Go function with 6 reference parameters and a return value.
    /// </summary>
    /// <param name="ref1">Reference parameter 1.</param>
    /// <param name="ref2">Reference parameter 2.</param>
    /// <param name="ref3">Reference parameter 3.</param>
    /// <param name="ref4">Reference parameter 4.</param>
    /// <param name="ref5">Reference parameter 5.</param>
    /// <param name="ref6">Reference parameter 6.</param>
    /// <param name="function">Go function to execute called with defer and recover function references.</param>
    public static T func<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, T>(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, T>.GoRefFunction function)
    {
        return new GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, T>(function).Execute(ref ref1, ref ref2, ref ref3,
            ref ref4, ref ref5, ref ref6);
    }

    /// <summary>
    /// Executes a Go function with 7 reference parameters and no return value.
    /// </summary>
    /// <param name="ref1">Reference parameter 1.</param>
    /// <param name="ref2">Reference parameter 2.</param>
    /// <param name="ref3">Reference parameter 3.</param>
    /// <param name="ref4">Reference parameter 4.</param>
    /// <param name="ref5">Reference parameter 5.</param>
    /// <param name="ref6">Reference parameter 6.</param>
    /// <param name="ref7">Reference parameter 7.</param>
    /// <param name="action">Go function to execute called with defer and recover function references.</param>
    public static void func<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7>(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, object>.GoRefAction action)
    {
        new GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, object>(action).Execute(ref ref1, ref ref2,
            ref ref3, ref ref4, ref ref5, ref ref6, ref ref7);
    }

    /// <summary>
    /// Executes a Go function with 7 reference parameters and a return value.
    /// </summary>
    /// <param name="ref1">Reference parameter 1.</param>
    /// <param name="ref2">Reference parameter 2.</param>
    /// <param name="ref3">Reference parameter 3.</param>
    /// <param name="ref4">Reference parameter 4.</param>
    /// <param name="ref5">Reference parameter 5.</param>
    /// <param name="ref6">Reference parameter 6.</param>
    /// <param name="ref7">Reference parameter 7.</param>
    /// <param name="function">Go function to execute called with defer and recover function references.</param>
    public static T func<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, T>(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, T>.GoRefFunction function)
    {
        return new GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, T>(function).Execute(ref ref1, ref ref2,
            ref ref3, ref ref4, ref ref5, ref ref6, ref ref7);
    }

    /// <summary>
    /// Executes a Go function with 8 reference parameters and no return value.
    /// </summary>
    /// <param name="ref1">Reference parameter 1.</param>
    /// <param name="ref2">Reference parameter 2.</param>
    /// <param name="ref3">Reference parameter 3.</param>
    /// <param name="ref4">Reference parameter 4.</param>
    /// <param name="ref5">Reference parameter 5.</param>
    /// <param name="ref6">Reference parameter 6.</param>
    /// <param name="ref7">Reference parameter 7.</param>
    /// <param name="ref8">Reference parameter 8.</param>
    /// <param name="action">Go function to execute called with defer and recover function references.</param>
    public static void func<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8>(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, object>.GoRefAction action)
    {
        new GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, object>(action).Execute(ref ref1, ref ref2,
            ref ref3, ref ref4, ref ref5, ref ref6, ref ref7, ref ref8);
    }

    /// <summary>
    /// Executes a Go function with 8 reference parameters and a return value.
    /// </summary>
    /// <param name="ref1">Reference parameter 1.</param>
    /// <param name="ref2">Reference parameter 2.</param>
    /// <param name="ref3">Reference parameter 3.</param>
    /// <param name="ref4">Reference parameter 4.</param>
    /// <param name="ref5">Reference parameter 5.</param>
    /// <param name="ref6">Reference parameter 6.</param>
    /// <param name="ref7">Reference parameter 7.</param>
    /// <param name="ref8">Reference parameter 8.</param>
    /// <param name="function">Go function to execute called with defer and recover function references.</param>
    public static T func<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, T>(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, T>.GoRefFunction function)
    {
        return new GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, T>(function).Execute(ref ref1,
            ref ref2, ref ref3, ref ref4, ref ref5, ref ref6, ref ref7, ref ref8);
    }

    /// <summary>
    /// Executes a Go function with 9 reference parameters and no return value.
    /// </summary>
    /// <param name="ref1">Reference parameter 1.</param>
    /// <param name="ref2">Reference parameter 2.</param>
    /// <param name="ref3">Reference parameter 3.</param>
    /// <param name="ref4">Reference parameter 4.</param>
    /// <param name="ref5">Reference parameter 5.</param>
    /// <param name="ref6">Reference parameter 6.</param>
    /// <param name="ref7">Reference parameter 7.</param>
    /// <param name="ref8">Reference parameter 8.</param>
    /// <param name="ref9">Reference parameter 9.</param>
    /// <param name="action">Go function to execute called with defer and recover function references.</param>
    public static void func<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9>(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, object>.GoRefAction action)
    {
        new GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, object>(action).Execute(ref ref1,
            ref ref2, ref ref3, ref ref4, ref ref5, ref ref6, ref ref7, ref ref8, ref ref9);
    }

    /// <summary>
    /// Executes a Go function with 9 reference parameters and a return value.
    /// </summary>
    /// <param name="ref1">Reference parameter 1.</param>
    /// <param name="ref2">Reference parameter 2.</param>
    /// <param name="ref3">Reference parameter 3.</param>
    /// <param name="ref4">Reference parameter 4.</param>
    /// <param name="ref5">Reference parameter 5.</param>
    /// <param name="ref6">Reference parameter 6.</param>
    /// <param name="ref7">Reference parameter 7.</param>
    /// <param name="ref8">Reference parameter 8.</param>
    /// <param name="ref9">Reference parameter 9.</param>
    /// <param name="function">Go function to execute called with defer and recover function references.</param>
    public static T func<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, T>(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, T>.GoRefFunction function)
    {
        return new GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, T>(function).Execute(ref ref1,
            ref ref2, ref ref3, ref ref4, ref ref5, ref ref6, ref ref7, ref ref8, ref ref9);
    }

    /// <summary>
    /// Executes a Go function with 10 reference parameters and no return value.
    /// </summary>
    /// <param name="ref1">Reference parameter 1.</param>
    /// <param name="ref2">Reference parameter 2.</param>
    /// <param name="ref3">Reference parameter 3.</param>
    /// <param name="ref4">Reference parameter 4.</param>
    /// <param name="ref5">Reference parameter 5.</param>
    /// <param name="ref6">Reference parameter 6.</param>
    /// <param name="ref7">Reference parameter 7.</param>
    /// <param name="ref8">Reference parameter 8.</param>
    /// <param name="ref9">Reference parameter 9.</param>
    /// <param name="ref10">Reference parameter 10.</param>
    /// <param name="action">Go function to execute called with defer and recover function references.</param>
    public static void func<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10>(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10, GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, object>.GoRefAction action)
    {
        new GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, object>(action).Execute(
            ref ref1, ref ref2, ref ref3, ref ref4, ref ref5, ref ref6, ref ref7, ref ref8, ref ref9, ref ref10);
    }

    /// <summary>
    /// Executes a Go function with 10 reference parameters and a return value.
    /// </summary>
    /// <param name="ref1">Reference parameter 1.</param>
    /// <param name="ref2">Reference parameter 2.</param>
    /// <param name="ref3">Reference parameter 3.</param>
    /// <param name="ref4">Reference parameter 4.</param>
    /// <param name="ref5">Reference parameter 5.</param>
    /// <param name="ref6">Reference parameter 6.</param>
    /// <param name="ref7">Reference parameter 7.</param>
    /// <param name="ref8">Reference parameter 8.</param>
    /// <param name="ref9">Reference parameter 9.</param>
    /// <param name="ref10">Reference parameter 10.</param>
    /// <param name="function">Go function to execute called with defer and recover function references.</param>
    public static T func<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, T>(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10, GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, T>.GoRefFunction function)
    {
        return new GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, T>(function).Execute(
            ref ref1, ref ref2, ref ref3, ref ref4, ref ref5, ref ref6, ref ref7, ref ref8, ref ref9, ref ref10);
    }

    /// <summary>
    /// Executes a Go function with 11 reference parameters and no return value.
    /// </summary>
    /// <param name="ref1">Reference parameter 1.</param>
    /// <param name="ref2">Reference parameter 2.</param>
    /// <param name="ref3">Reference parameter 3.</param>
    /// <param name="ref4">Reference parameter 4.</param>
    /// <param name="ref5">Reference parameter 5.</param>
    /// <param name="ref6">Reference parameter 6.</param>
    /// <param name="ref7">Reference parameter 7.</param>
    /// <param name="ref8">Reference parameter 8.</param>
    /// <param name="ref9">Reference parameter 9.</param>
    /// <param name="ref10">Reference parameter 10.</param>
    /// <param name="ref11">Reference parameter 11.</param>
    /// <param name="action">Go function to execute called with defer and recover function references.</param>
    public static void func<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11>(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10, ref TRef11 ref11, GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11, object>.GoRefAction action)
    {
        new GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11, object>(action)
            .Execute(ref ref1, ref ref2, ref ref3, ref ref4, ref ref5, ref ref6, ref ref7, ref ref8, ref ref9,
                ref ref10, ref ref11);
    }

    /// <summary>
    /// Executes a Go function with 11 reference parameters and a return value.
    /// </summary>
    /// <param name="ref1">Reference parameter 1.</param>
    /// <param name="ref2">Reference parameter 2.</param>
    /// <param name="ref3">Reference parameter 3.</param>
    /// <param name="ref4">Reference parameter 4.</param>
    /// <param name="ref5">Reference parameter 5.</param>
    /// <param name="ref6">Reference parameter 6.</param>
    /// <param name="ref7">Reference parameter 7.</param>
    /// <param name="ref8">Reference parameter 8.</param>
    /// <param name="ref9">Reference parameter 9.</param>
    /// <param name="ref10">Reference parameter 10.</param>
    /// <param name="ref11">Reference parameter 11.</param>
    /// <param name="function">Go function to execute called with defer and recover function references.</param>
    public static T func<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11, T>(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10, ref TRef11 ref11, GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11, T>.GoRefFunction function)
    {
        return new GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11, T>(function)
            .Execute(ref ref1, ref ref2, ref ref3, ref ref4, ref ref5, ref ref6, ref ref7, ref ref8, ref ref9,
                ref ref10, ref ref11);
    }

    /// <summary>
    /// Executes a Go function with 12 reference parameters and no return value.
    /// </summary>
    /// <param name="ref1">Reference parameter 1.</param>
    /// <param name="ref2">Reference parameter 2.</param>
    /// <param name="ref3">Reference parameter 3.</param>
    /// <param name="ref4">Reference parameter 4.</param>
    /// <param name="ref5">Reference parameter 5.</param>
    /// <param name="ref6">Reference parameter 6.</param>
    /// <param name="ref7">Reference parameter 7.</param>
    /// <param name="ref8">Reference parameter 8.</param>
    /// <param name="ref9">Reference parameter 9.</param>
    /// <param name="ref10">Reference parameter 10.</param>
    /// <param name="ref11">Reference parameter 11.</param>
    /// <param name="ref12">Reference parameter 12.</param>
    /// <param name="action">Go function to execute called with defer and recover function references.</param>
    public static void func<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11, TRef12>(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10, ref TRef11 ref11, ref TRef12 ref12, GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11, TRef12, object>.GoRefAction action)
    {
        new GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11, TRef12, object>(
            action).Execute(ref ref1, ref ref2, ref ref3, ref ref4, ref ref5, ref ref6, ref ref7, ref ref8, ref ref9,
            ref ref10, ref ref11, ref ref12);
    }

    /// <summary>
    /// Executes a Go function with 12 reference parameters and a return value.
    /// </summary>
    /// <param name="ref1">Reference parameter 1.</param>
    /// <param name="ref2">Reference parameter 2.</param>
    /// <param name="ref3">Reference parameter 3.</param>
    /// <param name="ref4">Reference parameter 4.</param>
    /// <param name="ref5">Reference parameter 5.</param>
    /// <param name="ref6">Reference parameter 6.</param>
    /// <param name="ref7">Reference parameter 7.</param>
    /// <param name="ref8">Reference parameter 8.</param>
    /// <param name="ref9">Reference parameter 9.</param>
    /// <param name="ref10">Reference parameter 10.</param>
    /// <param name="ref11">Reference parameter 11.</param>
    /// <param name="ref12">Reference parameter 12.</param>
    /// <param name="function">Go function to execute called with defer and recover function references.</param>
    public static T func<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11, TRef12, T>(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10, ref TRef11 ref11, ref TRef12 ref12, GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11, TRef12, T>.GoRefFunction function)
    {
        return
            new GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11, TRef12, T>(
                function).Execute(ref ref1, ref ref2, ref ref3, ref ref4, ref ref5, ref ref6, ref ref7, ref ref8,
                ref ref9, ref ref10, ref ref11, ref ref12);
    }

    /// <summary>
    /// Executes a Go function with 13 reference parameters and no return value.
    /// </summary>
    /// <param name="ref1">Reference parameter 1.</param>
    /// <param name="ref2">Reference parameter 2.</param>
    /// <param name="ref3">Reference parameter 3.</param>
    /// <param name="ref4">Reference parameter 4.</param>
    /// <param name="ref5">Reference parameter 5.</param>
    /// <param name="ref6">Reference parameter 6.</param>
    /// <param name="ref7">Reference parameter 7.</param>
    /// <param name="ref8">Reference parameter 8.</param>
    /// <param name="ref9">Reference parameter 9.</param>
    /// <param name="ref10">Reference parameter 10.</param>
    /// <param name="ref11">Reference parameter 11.</param>
    /// <param name="ref12">Reference parameter 12.</param>
    /// <param name="ref13">Reference parameter 13.</param>
    /// <param name="action">Go function to execute called with defer and recover function references.</param>
    public static void func<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11, TRef12, TRef13>(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10, ref TRef11 ref11, ref TRef12 ref12, ref TRef13 ref13, GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11, TRef12, TRef13, object>.GoRefAction action)
    {
        new GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11, TRef12, TRef13,
            object>(action).Execute(ref ref1, ref ref2, ref ref3, ref ref4, ref ref5, ref ref6, ref ref7, ref ref8,
            ref ref9, ref ref10, ref ref11, ref ref12, ref ref13);
    }

    /// <summary>
    /// Executes a Go function with 13 reference parameters and a return value.
    /// </summary>
    /// <param name="ref1">Reference parameter 1.</param>
    /// <param name="ref2">Reference parameter 2.</param>
    /// <param name="ref3">Reference parameter 3.</param>
    /// <param name="ref4">Reference parameter 4.</param>
    /// <param name="ref5">Reference parameter 5.</param>
    /// <param name="ref6">Reference parameter 6.</param>
    /// <param name="ref7">Reference parameter 7.</param>
    /// <param name="ref8">Reference parameter 8.</param>
    /// <param name="ref9">Reference parameter 9.</param>
    /// <param name="ref10">Reference parameter 10.</param>
    /// <param name="ref11">Reference parameter 11.</param>
    /// <param name="ref12">Reference parameter 12.</param>
    /// <param name="ref13">Reference parameter 13.</param>
    /// <param name="function">Go function to execute called with defer and recover function references.</param>
    public static T func<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11, TRef12, TRef13, T>(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10, ref TRef11 ref11, ref TRef12 ref12, ref TRef13 ref13, GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11, TRef12, TRef13, T>.GoRefFunction function)
    {
        return
            new GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11, TRef12, TRef13,
                T>(function).Execute(ref ref1, ref ref2, ref ref3, ref ref4, ref ref5, ref ref6, ref ref7, ref ref8,
                ref ref9, ref ref10, ref ref11, ref ref12, ref ref13);
    }

    /// <summary>
    /// Executes a Go function with 14 reference parameters and no return value.
    /// </summary>
    /// <param name="ref1">Reference parameter 1.</param>
    /// <param name="ref2">Reference parameter 2.</param>
    /// <param name="ref3">Reference parameter 3.</param>
    /// <param name="ref4">Reference parameter 4.</param>
    /// <param name="ref5">Reference parameter 5.</param>
    /// <param name="ref6">Reference parameter 6.</param>
    /// <param name="ref7">Reference parameter 7.</param>
    /// <param name="ref8">Reference parameter 8.</param>
    /// <param name="ref9">Reference parameter 9.</param>
    /// <param name="ref10">Reference parameter 10.</param>
    /// <param name="ref11">Reference parameter 11.</param>
    /// <param name="ref12">Reference parameter 12.</param>
    /// <param name="ref13">Reference parameter 13.</param>
    /// <param name="ref14">Reference parameter 14.</param>
    /// <param name="action">Go function to execute called with defer and recover function references.</param>
    public static void func<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11, TRef12, TRef13, TRef14>(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10, ref TRef11 ref11, ref TRef12 ref12, ref TRef13 ref13, ref TRef14 ref14, GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11, TRef12, TRef13, TRef14, object>.GoRefAction action)
    {
        new GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11, TRef12, TRef13, TRef14
            , object>(action).Execute(ref ref1, ref ref2, ref ref3, ref ref4, ref ref5, ref ref6, ref ref7, ref ref8,
            ref ref9, ref ref10, ref ref11, ref ref12, ref ref13, ref ref14);
    }

    /// <summary>
    /// Executes a Go function with 14 reference parameters and a return value.
    /// </summary>
    /// <param name="ref1">Reference parameter 1.</param>
    /// <param name="ref2">Reference parameter 2.</param>
    /// <param name="ref3">Reference parameter 3.</param>
    /// <param name="ref4">Reference parameter 4.</param>
    /// <param name="ref5">Reference parameter 5.</param>
    /// <param name="ref6">Reference parameter 6.</param>
    /// <param name="ref7">Reference parameter 7.</param>
    /// <param name="ref8">Reference parameter 8.</param>
    /// <param name="ref9">Reference parameter 9.</param>
    /// <param name="ref10">Reference parameter 10.</param>
    /// <param name="ref11">Reference parameter 11.</param>
    /// <param name="ref12">Reference parameter 12.</param>
    /// <param name="ref13">Reference parameter 13.</param>
    /// <param name="ref14">Reference parameter 14.</param>
    /// <param name="function">Go function to execute called with defer and recover function references.</param>
    public static T func<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11, TRef12, TRef13, TRef14, T>(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10, ref TRef11 ref11, ref TRef12 ref12, ref TRef13 ref13, ref TRef14 ref14, GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11, TRef12, TRef13, TRef14, T>.GoRefFunction function)
    {
        return
            new GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11, TRef12, TRef13,
                TRef14, T>(function).Execute(ref ref1, ref ref2, ref ref3, ref ref4, ref ref5, ref ref6, ref ref7,
                ref ref8, ref ref9, ref ref10, ref ref11, ref ref12, ref ref13, ref ref14);
    }

    /// <summary>
    /// Executes a Go function with 15 reference parameters and no return value.
    /// </summary>
    /// <param name="ref1">Reference parameter 1.</param>
    /// <param name="ref2">Reference parameter 2.</param>
    /// <param name="ref3">Reference parameter 3.</param>
    /// <param name="ref4">Reference parameter 4.</param>
    /// <param name="ref5">Reference parameter 5.</param>
    /// <param name="ref6">Reference parameter 6.</param>
    /// <param name="ref7">Reference parameter 7.</param>
    /// <param name="ref8">Reference parameter 8.</param>
    /// <param name="ref9">Reference parameter 9.</param>
    /// <param name="ref10">Reference parameter 10.</param>
    /// <param name="ref11">Reference parameter 11.</param>
    /// <param name="ref12">Reference parameter 12.</param>
    /// <param name="ref13">Reference parameter 13.</param>
    /// <param name="ref14">Reference parameter 14.</param>
    /// <param name="ref15">Reference parameter 15.</param>
    /// <param name="action">Go function to execute called with defer and recover function references.</param>
    public static void func<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11, TRef12, TRef13, TRef14, TRef15>(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10, ref TRef11 ref11, ref TRef12 ref12, ref TRef13 ref13, ref TRef14 ref14, ref TRef15 ref15, GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11, TRef12, TRef13, TRef14, TRef15, object>.GoRefAction action)
    {
        new GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11, TRef12, TRef13, TRef14
            , TRef15, object>(action).Execute(ref ref1, ref ref2, ref ref3, ref ref4, ref ref5, ref ref6, ref ref7,
            ref ref8, ref ref9, ref ref10, ref ref11, ref ref12, ref ref13, ref ref14, ref ref15);
    }

    /// <summary>
    /// Executes a Go function with 15 reference parameters and a return value.
    /// </summary>
    /// <param name="ref1">Reference parameter 1.</param>
    /// <param name="ref2">Reference parameter 2.</param>
    /// <param name="ref3">Reference parameter 3.</param>
    /// <param name="ref4">Reference parameter 4.</param>
    /// <param name="ref5">Reference parameter 5.</param>
    /// <param name="ref6">Reference parameter 6.</param>
    /// <param name="ref7">Reference parameter 7.</param>
    /// <param name="ref8">Reference parameter 8.</param>
    /// <param name="ref9">Reference parameter 9.</param>
    /// <param name="ref10">Reference parameter 10.</param>
    /// <param name="ref11">Reference parameter 11.</param>
    /// <param name="ref12">Reference parameter 12.</param>
    /// <param name="ref13">Reference parameter 13.</param>
    /// <param name="ref14">Reference parameter 14.</param>
    /// <param name="ref15">Reference parameter 15.</param>
    /// <param name="function">Go function to execute called with defer and recover function references.</param>
    public static T func<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11, TRef12, TRef13, TRef14, TRef15, T>(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10, ref TRef11 ref11, ref TRef12 ref12, ref TRef13 ref13, ref TRef14 ref14, ref TRef15 ref15, GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11, TRef12, TRef13, TRef14, TRef15, T>.GoRefFunction function)
    {
        return
            new GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11, TRef12, TRef13,
                TRef14, TRef15, T>(function).Execute(ref ref1, ref ref2, ref ref3, ref ref4, ref ref5, ref ref6,
                ref ref7, ref ref8, ref ref9, ref ref10, ref ref11, ref ref12, ref ref13, ref ref14, ref ref15);
    }

    /// <summary>
    /// Executes a Go function with 16 reference parameters and no return value.
    /// </summary>
    /// <param name="ref1">Reference parameter 1.</param>
    /// <param name="ref2">Reference parameter 2.</param>
    /// <param name="ref3">Reference parameter 3.</param>
    /// <param name="ref4">Reference parameter 4.</param>
    /// <param name="ref5">Reference parameter 5.</param>
    /// <param name="ref6">Reference parameter 6.</param>
    /// <param name="ref7">Reference parameter 7.</param>
    /// <param name="ref8">Reference parameter 8.</param>
    /// <param name="ref9">Reference parameter 9.</param>
    /// <param name="ref10">Reference parameter 10.</param>
    /// <param name="ref11">Reference parameter 11.</param>
    /// <param name="ref12">Reference parameter 12.</param>
    /// <param name="ref13">Reference parameter 13.</param>
    /// <param name="ref14">Reference parameter 14.</param>
    /// <param name="ref15">Reference parameter 15.</param>
    /// <param name="ref16">Reference parameter 16.</param>
    /// <param name="action">Go function to execute called with defer and recover function references.</param>
    public static void func<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11, TRef12, TRef13, TRef14, TRef15, TRef16>(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10, ref TRef11 ref11, ref TRef12 ref12, ref TRef13 ref13, ref TRef14 ref14, ref TRef15 ref15, ref TRef16 ref16, GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11, TRef12, TRef13, TRef14, TRef15, TRef16, object>.GoRefAction action)
    {
        new GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11, TRef12, TRef13, TRef14
            , TRef15, TRef16, object>(action).Execute(ref ref1, ref ref2, ref ref3, ref ref4, ref ref5, ref ref6,
            ref ref7, ref ref8, ref ref9, ref ref10, ref ref11, ref ref12, ref ref13, ref ref14, ref ref15, ref ref16);
    }

    /// <summary>
    /// Executes a Go function with 16 reference parameters and a return value.
    /// </summary>
    /// <param name="ref1">Reference parameter 1.</param>
    /// <param name="ref2">Reference parameter 2.</param>
    /// <param name="ref3">Reference parameter 3.</param>
    /// <param name="ref4">Reference parameter 4.</param>
    /// <param name="ref5">Reference parameter 5.</param>
    /// <param name="ref6">Reference parameter 6.</param>
    /// <param name="ref7">Reference parameter 7.</param>
    /// <param name="ref8">Reference parameter 8.</param>
    /// <param name="ref9">Reference parameter 9.</param>
    /// <param name="ref10">Reference parameter 10.</param>
    /// <param name="ref11">Reference parameter 11.</param>
    /// <param name="ref12">Reference parameter 12.</param>
    /// <param name="ref13">Reference parameter 13.</param>
    /// <param name="ref14">Reference parameter 14.</param>
    /// <param name="ref15">Reference parameter 15.</param>
    /// <param name="ref16">Reference parameter 16.</param>
    /// <param name="function">Go function to execute called with defer and recover function references.</param>
    public static T func<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11, TRef12, TRef13, TRef14, TRef15, TRef16, T>(ref TRef1 ref1, ref TRef2 ref2, ref TRef3 ref3, ref TRef4 ref4, ref TRef5 ref5, ref TRef6 ref6, ref TRef7 ref7, ref TRef8 ref8, ref TRef9 ref9, ref TRef10 ref10, ref TRef11 ref11, ref TRef12 ref12, ref TRef13 ref13, ref TRef14 ref14, ref TRef15 ref15, ref TRef16 ref16, GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11, TRef12, TRef13, TRef14, TRef15, TRef16, T>.GoRefFunction function)
    {
        return
            new GoFunc<TRef1, TRef2, TRef3, TRef4, TRef5, TRef6, TRef7, TRef8, TRef9, TRef10, TRef11, TRef12, TRef13,
                TRef14, TRef15, TRef16, T>(function).Execute(ref ref1, ref ref2, ref ref3, ref ref4, ref ref5, ref ref6,
                ref ref7, ref ref8, ref ref9, ref ref10, ref ref11, ref ref12, ref ref13, ref ref14, ref ref15,
                ref ref16);
    }

    #endregion
}
