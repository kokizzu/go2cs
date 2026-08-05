// builtin.DeferRegistrations.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// ReSharper disable InconsistentNaming

using System;

namespace go;

// ---------------------------------------------------------------------------------------------
// DEFER REGISTRATIONS — every `deferǃ` overload, i.e. the C# spelling of Go's `defer f(a, b, …)`.
//
// WHAT LIVES HERE
//   An ARITY LADDER of `deferǃ<T1…Tn>(Action<T1…Tn>, T1 arg1, …, Defer defer)` rungs from 1 to 16
//   parameters, each with a `Func<…, TResult>` twin that discards the result. Every rung has the
//   same one-line body: hand a closure to the `defer` delegate.
//
// WHY THE TRAILING `Defer defer` PARAMETER
//   Go's `defer` is scoped to the ENCLOSING FUNCTION, so something has to know which function is
//   registering. The converter wraps any Go function that defers or recovers in a
//   `func((defer, panic, recover) => { … })` execution context (see
//   builtin.FuncExecutionContexts.cs) and threads that context's `defer` delegate through as the
//   last argument. That is also why the name is `deferǃ` and not `defer`: the plain name is
//   already taken by the delegate parameter in scope, and `ǃ` (U+01C3, LATIN LETTER RETROFLEX
//   CLICK) is a legal C# identifier character where `!` is not.
//
// WHY A LADDER AND NOT ONE VARIADIC METHOD
//   Same reason as the goroutine ladder: Go evaluates a deferred call's arguments AT THE `defer`
//   STATEMENT and runs the call later, so every argument must be captured at registration time. A
//   `params object[]` form would do it while boxing every value argument — on a path that runs on
//   function exit throughout the corpus. The generic rungs capture without boxing and let the JIT
//   see the concrete delegate, so the ladder is a deliberate performance shape, not clutter.
//
// EXTENDING IT
//   Add a rung only when a corpus package actually needs that arity, and add BOTH forms — the
//   `Action` rung and the result-discarding `Func` twin — or a value-returning method group will
//   fail to bind at exactly that arity (CS0407). Keep the bodies mechanical.
// ---------------------------------------------------------------------------------------------
public static partial class builtin
{
    // ** Go defer Handlers with Parameters **

    /// <summary>
    /// Executes a deferred action with one parameter.
    /// </summary>
    /// <typeparam name="T">First parameter type.</typeparam>
    /// <param name="action">Target defer action.</param>
    /// <param name="arg">First parameter.</param>
    /// <param name="defer">Source defer function.</param>
    // The following is basically "defer!". We use the Unicode bang-type character
    // as a valid C# identifier symbol, where the standard "!" is not. This is
    // to disambiguate the method name with parameters from the Defer delegate
    // passed into the function execution context with a fixed name of 'defer'.
    public static void deferǃ<T>(Action<T> action, T arg, Defer defer)
    {
        defer(() => action(arg));
    }


    /// <summary>
    /// Executes a deferred call to a VALUE-RETURNING function, discarding the result - Go
    /// discards the return values of a deferred call (`defer CloseHandle(h)`, where CloseHandle
    /// returns error). Without these Func twins a value-returning method group cannot bind the
    /// Action overloads (CS0407, syscall's windows defers).
    /// </summary>
    public static void deferǃ<T, TResult>(Func<T, TResult> action, T arg, Defer defer)
    {
        defer(() => { action(arg); });
    }

    /// <summary>Executes a deferred call to a value-returning function, discarding the result.</summary>
    public static void deferǃ<T1, T2, TResult>(Func<T1, T2, TResult> action, T1 arg1, T2 arg2, Defer defer)
    {
        defer(() => { action(arg1, arg2); });
    }

    /// <summary>Executes a deferred call to a value-returning function, discarding the result.</summary>
    public static void deferǃ<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> action, T1 arg1, T2 arg2, T3 arg3, Defer defer)
    {
        defer(() => { action(arg1, arg2, arg3); });
    }

    /// <summary>Executes a deferred call to a value-returning function, discarding the result.</summary>
    public static void deferǃ<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, Defer defer)
    {
        defer(() => { action(arg1, arg2, arg3, arg4); });
    }

    /// <summary>Executes a deferred call to a value-returning function, discarding the result.</summary>
    public static void deferǃ<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, Defer defer)
    {
        defer(() => { action(arg1, arg2, arg3, arg4, arg5); });
    }

    /// <summary>Executes a deferred call to a value-returning function, discarding the result.</summary>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, TResult>(Func<T1, T2, T3, T4, T5, T6, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, Defer defer)
    {
        defer(() => { action(arg1, arg2, arg3, arg4, arg5, arg6); });
    }

    /// <summary>Executes a deferred call to a value-returning function, discarding the result.</summary>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, T7, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, Defer defer)
    {
        defer(() => { action(arg1, arg2, arg3, arg4, arg5, arg6, arg7); });
    }

    /// <summary>Executes a deferred call to a value-returning function, discarding the result.</summary>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, Defer defer)
    {
        defer(() => { action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8); });
    }

    /// <summary>Executes a deferred call to a value-returning function, discarding the result.</summary>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, Defer defer)
    {
        defer(() => { action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9); });
    }

    /// <summary>Executes a deferred call to a value-returning function, discarding the result.</summary>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, Defer defer)
    {
        defer(() => { action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10); });
    }

    /// <summary>Executes a deferred call to a value-returning function, discarding the result.</summary>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, Defer defer)
    {
        defer(() => { action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11); });
    }

    /// <summary>Executes a deferred call to a value-returning function, discarding the result.</summary>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, Defer defer)
    {
        defer(() => { action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12); });
    }

    /// <summary>Executes a deferred call to a value-returning function, discarding the result.</summary>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, Defer defer)
    {
        defer(() => { action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13); });
    }

    /// <summary>Executes a deferred call to a value-returning function, discarding the result.</summary>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, Defer defer)
    {
        defer(() => { action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14); });
    }

    /// <summary>Executes a deferred call to a value-returning function, discarding the result.</summary>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, Defer defer)
    {
        defer(() => { action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15); });
    }

    /// <summary>Executes a deferred call to a value-returning function, discarding the result.</summary>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16, Defer defer)
    {
        defer(() => { action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16); });
    }

    #region [ deferǃ<T1, T2, ... T16> Implementations ]

    /// <summary>
    /// Executes a deferred action with two parameters.
    /// </summary>
    /// <typeparam name="T1">First parameter type.</typeparam>
    /// <typeparam name="T2">First parameter type.</typeparam>
    /// <param name="action">Target defer action.</param>
    /// <param name="arg1">First parameter.</param>
    /// <param name="arg2">Second parameter.</param>
    /// <param name="defer">Source defer function.</param>
    public static void deferǃ<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2, Defer defer)
    {
        defer(() => action(arg1, arg2));
    }

    /// <summary>
    /// Executes a deferred action with three parameters.
    /// </summary>
    /// <typeparam name="T1">First parameter type.</typeparam>
    /// <typeparam name="T2">Second parameter type.</typeparam>
    /// <typeparam name="T3">Third parameter type.</typeparam>
    /// <param name="action">Target defer action.</param>
    /// <param name="arg1">First parameter.</param>
    /// <param name="arg2">Second parameter.</param>
    /// <param name="arg3">Third parameter.</param>
    /// <param name="defer">Source defer function.</param>
    public static void deferǃ<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3, Defer defer)
    {
        defer(() => action(arg1, arg2, arg3));
    }

    /// <summary>
    /// Executes a deferred action with four parameters.
    /// </summary>
    /// <typeparam name="T1">First parameter type.</typeparam>
    /// <typeparam name="T2">Second parameter type.</typeparam>
    /// <typeparam name="T3">Third parameter type.</typeparam>
    /// <typeparam name="T4">Fourth parameter type.</typeparam>
    /// <param name="action">Target defer action.</param>
    /// <param name="arg1">First parameter.</param>
    /// <param name="arg2">Second parameter.</param>
    /// <param name="arg3">Third parameter.</param>
    /// <param name="arg4">Fourth parameter.</param>
    /// <param name="defer">Source defer function.</param>
    public static void deferǃ<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, Defer defer)
    {
        defer(() => action(arg1, arg2, arg3, arg4));
    }

    /// <summary>
    /// Executes a deferred action with five parameters.
    /// </summary>
    /// <typeparam name="T1">First parameter type.</typeparam>
    /// <typeparam name="T2">Second parameter type.</typeparam>
    /// <typeparam name="T3">Third parameter type.</typeparam>
    /// <typeparam name="T4">Fourth parameter type.</typeparam>
    /// <typeparam name="T5">Fifth parameter type.</typeparam>
    /// <param name="action">Target defer action.</param>
    /// <param name="arg1">First parameter.</param>
    /// <param name="arg2">Second parameter.</param>
    /// <param name="arg3">Third parameter.</param>
    /// <param name="arg4">Fourth parameter.</param>
    /// <param name="arg5">Fifth parameter.</param>
    /// <param name="defer">Source defer function.</param>
    public static void deferǃ<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, Defer defer)
    {
        defer(() => action(arg1, arg2, arg3, arg4, arg5));
    }

    /// <summary>
    /// Executes a deferred action with six parameters.
    /// </summary>
    /// <typeparam name="T1">First parameter type.</typeparam>
    /// <typeparam name="T2">Second parameter type.</typeparam>
    /// <typeparam name="T3">Third parameter type.</typeparam>
    /// <typeparam name="T4">Fourth parameter type.</typeparam>
    /// <typeparam name="T5">Fifth parameter type.</typeparam>
    /// <typeparam name="T6">Sixth parameter type.</typeparam>
    /// <param name="action">Target defer action.</param>
    /// <param name="arg1">First parameter.</param>
    /// <param name="arg2">Second parameter.</param>
    /// <param name="arg3">Third parameter.</param>
    /// <param name="arg4">Fourth parameter.</param>
    /// <param name="arg5">Fifth parameter.</param>
    /// <param name="arg6">Sixth parameter.</param>
    /// <param name="defer">Source defer function.</param>
    public static void deferǃ<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, Defer defer)
    {
        defer(() => action(arg1, arg2, arg3, arg4, arg5, arg6));
    }

    /// <summary>
    /// Executes a deferred action with seven parameters.
    /// </summary>
    /// <typeparam name="T1">First parameter type.</typeparam>
    /// <typeparam name="T2">Second parameter type.</typeparam>
    /// <typeparam name="T3">Third parameter type.</typeparam>
    /// <typeparam name="T4">Fourth parameter type.</typeparam>
    /// <typeparam name="T5">Fifth parameter type.</typeparam>
    /// <typeparam name="T6">Sixth parameter type.</typeparam>
    /// <typeparam name="T7">Seventh parameter type.</typeparam>
    /// <param name="action">Target defer action.</param>
    /// <param name="arg1">First parameter.</param>
    /// <param name="arg2">Second parameter.</param>
    /// <param name="arg3">Third parameter.</param>
    /// <param name="arg4">Fourth parameter.</param>
    /// <param name="arg5">Fifth parameter.</param>
    /// <param name="arg6">Sixth parameter.</param>
    /// <param name="arg7">Seventh parameter.</param>
    /// <param name="defer">Source defer function.</param>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, T7>(Action<T1, T2, T3, T4, T5, T6, T7> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, Defer defer)
    {
        defer(() => action(arg1, arg2, arg3, arg4, arg5, arg6, arg7));
    }

    /// <summary>
    /// Executes a deferred action with eight parameters.
    /// </summary>
    /// <typeparam name="T1">First parameter type.</typeparam>
    /// <typeparam name="T2">Second parameter type.</typeparam>
    /// <typeparam name="T3">Third parameter type.</typeparam>
    /// <typeparam name="T4">Fourth parameter type.</typeparam>
    /// <typeparam name="T5">Fifth parameter type.</typeparam>
    /// <typeparam name="T6">Sixth parameter type.</typeparam>
    /// <typeparam name="T7">Seventh parameter type.</typeparam>
    /// <typeparam name="T8">Eighth parameter type.</typeparam>
    /// <param name="action">Target defer action.</param>
    /// <param name="arg1">First parameter.</param>
    /// <param name="arg2">Second parameter.</param>
    /// <param name="arg3">Third parameter.</param>
    /// <param name="arg4">Fourth parameter.</param>
    /// <param name="arg5">Fifth parameter.</param>
    /// <param name="arg6">Sixth parameter.</param>
    /// <param name="arg7">Seventh parameter.</param>
    /// <param name="arg8">Eighth parameter.</param>
    /// <param name="defer">Source defer function.</param>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, T7, T8>(Action<T1, T2, T3, T4, T5, T6, T7, T8> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, Defer defer)
    {
        defer(() => action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8));
    }

    /// <summary>
    /// Executes a deferred action with nine parameters.
    /// </summary>
    /// <typeparam name="T1">First parameter type.</typeparam>
    /// <typeparam name="T2">Second parameter type.</typeparam>
    /// <typeparam name="T3">Third parameter type.</typeparam>
    /// <typeparam name="T4">Fourth parameter type.</typeparam>
    /// <typeparam name="T5">Fifth parameter type.</typeparam>
    /// <typeparam name="T6">Sixth parameter type.</typeparam>
    /// <typeparam name="T7">Seventh parameter type.</typeparam>
    /// <typeparam name="T8">Eighth parameter type.</typeparam>
    /// <typeparam name="T9">Ninth parameter type.</typeparam>
    /// <param name="action">Target defer action.</param>
    /// <param name="arg1">First parameter.</param>
    /// <param name="arg2">Second parameter.</param>
    /// <param name="arg3">Third parameter.</param>
    /// <param name="arg4">Fourth parameter.</param>
    /// <param name="arg5">Fifth parameter.</param>
    /// <param name="arg6">Sixth parameter.</param>
    /// <param name="arg7">Seventh parameter.</param>
    /// <param name="arg8">Eighth parameter.</param>
    /// <param name="arg9">Ninth parameter.</param>
    /// <param name="defer">Source defer function.</param>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, Defer defer)
    {
        defer(() => action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9));
    }

    /// <summary>
    /// Executes a deferred action with ten parameters.
    /// </summary>
    /// <typeparam name="T1">First parameter type.</typeparam>
    /// <typeparam name="T2">Second parameter type.</typeparam>
    /// <typeparam name="T3">Third parameter type.</typeparam>
    /// <typeparam name="T4">Fourth parameter type.</typeparam>
    /// <typeparam name="T5">Fifth parameter type.</typeparam>
    /// <typeparam name="T6">Sixth parameter type.</typeparam>
    /// <typeparam name="T7">Seventh parameter type.</typeparam>
    /// <typeparam name="T8">Eighth parameter type.</typeparam>
    /// <typeparam name="T9">Ninth parameter type.</typeparam>
    /// <typeparam name="T10">Tenth parameter type.</typeparam>
    /// <param name="action">Target defer action.</param>
    /// <param name="arg1">First parameter.</param>
    /// <param name="arg2">Second parameter.</param>
    /// <param name="arg3">Third parameter.</param>
    /// <param name="arg4">Fourth parameter.</param>
    /// <param name="arg5">Fifth parameter.</param>
    /// <param name="arg6">Sixth parameter.</param>
    /// <param name="arg7">Seventh parameter.</param>
    /// <param name="arg8">Eighth parameter.</param>
    /// <param name="arg9">Ninth parameter.</param>
    /// <param name="arg10">Tenth parameter.</param>
    /// <param name="defer">Source defer function.</param>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, Defer defer)
    {
        defer(() => action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10));
    }

    /// <summary>
    /// Executes a deferred action with eleven parameters.
    /// </summary>
    /// <typeparam name="T1">First parameter type.</typeparam>
    /// <typeparam name="T2">Second parameter type.</typeparam>
    /// <typeparam name="T3">Third parameter type.</typeparam>
    /// <typeparam name="T4">Fourth parameter type.</typeparam>
    /// <typeparam name="T5">Fifth parameter type.</typeparam>
    /// <typeparam name="T6">Sixth parameter type.</typeparam>
    /// <typeparam name="T7">Seventh parameter type.</typeparam>
    /// <typeparam name="T8">Eighth parameter type.</typeparam>
    /// <typeparam name="T9">Ninth parameter type.</typeparam>
    /// <typeparam name="T10">Tenth parameter type.</typeparam>
    /// <typeparam name="T11">Eleventh parameter type.</typeparam>
    /// <param name="action">Target defer action.</param>
    /// <param name="arg1">First parameter.</param>
    /// <param name="arg2">Second parameter.</param>
    /// <param name="arg3">Third parameter.</param>
    /// <param name="arg4">Fourth parameter.</param>
    /// <param name="arg5">Fifth parameter.</param>
    /// <param name="arg6">Sixth parameter.</param>
    /// <param name="arg7">Seventh parameter.</param>
    /// <param name="arg8">Eighth parameter.</param>
    /// <param name="arg9">Ninth parameter.</param>
    /// <param name="arg10">Tenth parameter.</param>
    /// <param name="arg11">Eleventh parameter.</param>
    /// <param name="defer">Source defer function.</param>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, Defer defer)
    {
        defer(() => action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11));
    }

    /// <summary>
    /// Executes a deferred action with twelve parameters.
    /// </summary>
    /// <typeparam name="T1">First parameter type.</typeparam>
    /// <typeparam name="T2">Second parameter type.</typeparam>
    /// <typeparam name="T3">Third parameter type.</typeparam>
    /// <typeparam name="T4">Fourth parameter type.</typeparam>
    /// <typeparam name="T5">Fifth parameter type.</typeparam>
    /// <typeparam name="T6">Sixth parameter type.</typeparam>
    /// <typeparam name="T7">Seventh parameter type.</typeparam>
    /// <typeparam name="T8">Eighth parameter type.</typeparam>
    /// <typeparam name="T9">Ninth parameter type.</typeparam>
    /// <typeparam name="T10">Tenth parameter type.</typeparam>
    /// <typeparam name="T11">Eleventh parameter type.</typeparam>
    /// <typeparam name="T12">Twelfth parameter type.</typeparam>
    /// <param name="action">Target defer action.</param>
    /// <param name="arg1">First parameter.</param>
    /// <param name="arg2">Second parameter.</param>
    /// <param name="arg3">Third parameter.</param>
    /// <param name="arg4">Fourth parameter.</param>
    /// <param name="arg5">Fifth parameter.</param>
    /// <param name="arg6">Sixth parameter.</param>
    /// <param name="arg7">Seventh parameter.</param>
    /// <param name="arg8">Eighth parameter.</param>
    /// <param name="arg9">Ninth parameter.</param>
    /// <param name="arg10">Tenth parameter.</param>
    /// <param name="arg11">Eleventh parameter.</param>
    /// <param name="arg12">Twelfth parameter.</param>
    /// <param name="defer">Source defer function.</param>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, Defer defer)
    {
        defer(() => action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12));
    }

    /// <summary>
    /// Executes a deferred action with thirteen parameters.
    /// </summary>
    /// <typeparam name="T1">First parameter type.</typeparam>
    /// <typeparam name="T2">Second parameter type.</typeparam>
    /// <typeparam name="T3">Third parameter type.</typeparam>
    /// <typeparam name="T4">Fourth parameter type.</typeparam>
    /// <typeparam name="T5">Fifth parameter type.</typeparam>
    /// <typeparam name="T6">Sixth parameter type.</typeparam>
    /// <typeparam name="T7">Seventh parameter type.</typeparam>
    /// <typeparam name="T8">Eighth parameter type.</typeparam>
    /// <typeparam name="T9">Ninth parameter type.</typeparam>
    /// <typeparam name="T10">Tenth parameter type.</typeparam>
    /// <typeparam name="T11">Eleventh parameter type.</typeparam>
    /// <typeparam name="T12">Twelfth parameter type.</typeparam>
    /// <typeparam name="T13">Thirteenth parameter type.</typeparam>
    /// <param name="action">Target defer action.</param>
    /// <param name="arg1">First parameter.</param>
    /// <param name="arg2">Second parameter.</param>
    /// <param name="arg3">Third parameter.</param>
    /// <param name="arg4">Fourth parameter.</param>
    /// <param name="arg5">Fifth parameter.</param>
    /// <param name="arg6">Sixth parameter.</param>
    /// <param name="arg7">Seventh parameter.</param>
    /// <param name="arg8">Eighth parameter.</param>
    /// <param name="arg9">Ninth parameter.</param>
    /// <param name="arg10">Tenth parameter.</param>
    /// <param name="arg11">Eleventh parameter.</param>
    /// <param name="arg12">Twelfth parameter.</param>
    /// <param name="arg13">Thirteenth parameter.</param>
    /// <param name="defer">Source defer function.</param>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, Defer defer)
    {
        defer(() => action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13));
    }

    /// <summary>
    /// Executes a deferred action with fourteen parameters.
    /// </summary>
    /// <typeparam name="T1">First parameter type.</typeparam>
    /// <typeparam name="T2">Second parameter type.</typeparam>
    /// <typeparam name="T3">Third parameter type.</typeparam>
    /// <typeparam name="T4">Fourth parameter type.</typeparam>
    /// <typeparam name="T5">Fifth parameter type.</typeparam>
    /// <typeparam name="T6">Sixth parameter type.</typeparam>
    /// <typeparam name="T7">Seventh parameter type.</typeparam>
    /// <typeparam name="T8">Eighth parameter type.</typeparam>
    /// <typeparam name="T9">Ninth parameter type.</typeparam>
    /// <typeparam name="T10">Tenth parameter type.</typeparam>
    /// <typeparam name="T11">Eleventh parameter type.</typeparam>
    /// <typeparam name="T12">Twelfth parameter type.</typeparam>
    /// <typeparam name="T13">Thirteenth parameter type.</typeparam>
    /// <typeparam name="T14">Fourteenth parameter type.</typeparam>
    /// <param name="action">Target defer action.</param>
    /// <param name="arg1">First parameter.</param>
    /// <param name="arg2">Second parameter.</param>
    /// <param name="arg3">Third parameter.</param>
    /// <param name="arg4">Fourth parameter.</param>
    /// <param name="arg5">Fifth parameter.</param>
    /// <param name="arg6">Sixth parameter.</param>
    /// <param name="arg7">Seventh parameter.</param>
    /// <param name="arg8">Eighth parameter.</param>
    /// <param name="arg9">Ninth parameter.</param>
    /// <param name="arg10">Tenth parameter.</param>
    /// <param name="arg11">Eleventh parameter.</param>
    /// <param name="arg12">Twelfth parameter.</param>
    /// <param name="arg13">Thirteenth parameter.</param>
    /// <param name="arg14">Fourteenth parameter.</param>
    /// <param name="defer">Source defer function.</param>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, Defer defer)
    {
        defer(() => action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14));
    }

    /// <summary>
    /// Executes a deferred action with fifteen parameters.
    /// </summary>
    /// <typeparam name="T1">First parameter type.</typeparam>
    /// <typeparam name="T2">Second parameter type.</typeparam>
    /// <typeparam name="T3">Third parameter type.</typeparam>
    /// <typeparam name="T4">Fourth parameter type.</typeparam>
    /// <typeparam name="T5">Fifth parameter type.</typeparam>
    /// <typeparam name="T6">Sixth parameter type.</typeparam>
    /// <typeparam name="T7">Seventh parameter type.</typeparam>
    /// <typeparam name="T8">Eighth parameter type.</typeparam>
    /// <typeparam name="T9">Ninth parameter type.</typeparam>
    /// <typeparam name="T10">Tenth parameter type.</typeparam>
    /// <typeparam name="T11">Eleventh parameter type.</typeparam>
    /// <typeparam name="T12">Twelfth parameter type.</typeparam>
    /// <typeparam name="T13">Thirteenth parameter type.</typeparam>
    /// <typeparam name="T14">Fourteenth parameter type.</typeparam>
    /// <typeparam name="T15">Fifteenth parameter type.</typeparam>
    /// <param name="action">Target defer action.</param>
    /// <param name="arg1">First parameter.</param>
    /// <param name="arg2">Second parameter.</param>
    /// <param name="arg3">Third parameter.</param>
    /// <param name="arg4">Fourth parameter.</param>
    /// <param name="arg5">Fifth parameter.</param>
    /// <param name="arg6">Sixth parameter.</param>
    /// <param name="arg7">Seventh parameter.</param>
    /// <param name="arg8">Eighth parameter.</param>
    /// <param name="arg9">Ninth parameter.</param>
    /// <param name="arg10">Tenth parameter.</param>
    /// <param name="arg11">Eleventh parameter.</param>
    /// <param name="arg12">Twelfth parameter.</param>
    /// <param name="arg13">Thirteenth parameter.</param>
    /// <param name="arg14">Fourteenth parameter.</param>
    /// <param name="arg15">Fifteenth parameter.</param>
    /// <param name="defer">Source defer function.</param>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, Defer defer)
    {
        defer(() => action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15));
    }

    /// <summary>
    /// Executes a deferred action with sixteen parameters.
    /// </summary>
    /// <typeparam name="T1">First parameter type.</typeparam>
    /// <typeparam name="T2">Second parameter type.</typeparam>
    /// <typeparam name="T3">Third parameter type.</typeparam>
    /// <typeparam name="T4">Fourth parameter type.</typeparam>
    /// <typeparam name="T5">Fifth parameter type.</typeparam>
    /// <typeparam name="T6">Sixth parameter type.</typeparam>
    /// <typeparam name="T7">Seventh parameter type.</typeparam>
    /// <typeparam name="T8">Eighth parameter type.</typeparam>
    /// <typeparam name="T9">Ninth parameter type.</typeparam>
    /// <typeparam name="T10">Tenth parameter type.</typeparam>
    /// <typeparam name="T11">Eleventh parameter type.</typeparam>
    /// <typeparam name="T12">Twelfth parameter type.</typeparam>
    /// <typeparam name="T13">Thirteenth parameter type.</typeparam>
    /// <typeparam name="T14">Fourteenth parameter type.</typeparam>
    /// <typeparam name="T15">Fifteenth parameter type.</typeparam>
    /// <typeparam name="T16">Sixteenth parameter type.</typeparam>
    /// <param name="action">Target defer action.</param>
    /// <param name="arg1">First parameter.</param>
    /// <param name="arg2">Second parameter.</param>
    /// <param name="arg3">Third parameter.</param>
    /// <param name="arg4">Fourth parameter.</param>
    /// <param name="arg5">Fifth parameter.</param>
    /// <param name="arg6">Sixth parameter.</param>
    /// <param name="arg7">Seventh parameter.</param>
    /// <param name="arg8">Eighth parameter.</param>
    /// <param name="arg9">Ninth parameter.</param>
    /// <param name="arg10">Tenth parameter.</param>
    /// <param name="arg11">Eleventh parameter.</param>
    /// <param name="arg12">Twelfth parameter.</param>
    /// <param name="arg13">Thirteenth parameter.</param>
    /// <param name="arg14">Fourteenth parameter.</param>
    /// <param name="arg15">Fifteenth parameter.</param>
    /// <param name="arg16">Sixteenth parameter.</param>
    /// <param name="defer">Source defer function.</param>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16, Defer defer)
    {
        defer(() => action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16));
    }

    #endregion

    #region [ Frame registrations ]

    // -----------------------------------------------------------------------------------------
    // THE FRAME FORM of every rung above: the same registration, into a GoFrame passed by
    // reference instead of through a Defer delegate.
    //
    // The trailing `Defer defer` parameter of the rungs above exists because the retired
    // execution-context lambda OWNED the defer list, so the body had to be handed a delegate to
    // reach it. A frame is a local of the function that defers, so the registration simply names
    // it: `deferǃ(fd.writeUnlock, ref ᒐ)`. Nothing is captured, nothing is allocated for the
    // list itself, and the arity ladder keeps doing the one job it was always for — evaluating
    // the deferred call's ARGUMENTS at the `defer` statement, generically, without boxing.
    //
    // A NULLARY rung exists here and does not above, because the nullary lambda form called the
    // `defer` delegate directly (`defer(() => …)`); there is no delegate to call now.
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Registers a deferred call taking no arguments.
    /// </summary>
    /// <param name="action">Deferred call.</param>
    /// <param name="frame">Registering function's frame.</param>
    public static void deferǃ(Action action, ref GoFrame frame)
    {
        frame.Push(action);
    }

    /// <summary>
    /// Registers a deferred call with 1 parameter, evaluated now and passed on exit.
    /// </summary>
    /// <typeparam name="T">First parameter type.</typeparam>
    /// <param name="action">Deferred call.</param>
    /// <param name="arg">First parameter.</param>
    /// <param name="frame">Registering function's frame.</param>
    public static void deferǃ<T>(Action<T> action, T arg, ref GoFrame frame)
    {
        frame.Push(() => action(arg));
    }

    /// <summary>Registers a deferred call to a value-returning function, discarding the result.</summary>
    public static void deferǃ<T, TResult>(Func<T, TResult> action, T arg, ref GoFrame frame)
    {
        frame.Push(() => { action(arg); });
    }

    /// <summary>
    /// Registers a deferred call with 2 parameters, evaluated now and passed on exit.
    /// </summary>
    /// <typeparam name="T1">First parameter type.</typeparam>
    /// <typeparam name="T2">Second parameter type.</typeparam>
    /// <param name="action">Deferred call.</param>
    /// <param name="arg1">First parameter.</param>
    /// <param name="arg2">Second parameter.</param>
    /// <param name="frame">Registering function's frame.</param>
    public static void deferǃ<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2, ref GoFrame frame)
    {
        frame.Push(() => action(arg1, arg2));
    }

    /// <summary>Registers a deferred call to a value-returning function, discarding the result.</summary>
    public static void deferǃ<T1, T2, TResult>(Func<T1, T2, TResult> action, T1 arg1, T2 arg2, ref GoFrame frame)
    {
        frame.Push(() => { action(arg1, arg2); });
    }

    /// <summary>
    /// Registers a deferred call with 3 parameters, evaluated now and passed on exit.
    /// </summary>
    /// <typeparam name="T1">First parameter type.</typeparam>
    /// <typeparam name="T2">Second parameter type.</typeparam>
    /// <typeparam name="T3">Third parameter type.</typeparam>
    /// <param name="action">Deferred call.</param>
    /// <param name="arg1">First parameter.</param>
    /// <param name="arg2">Second parameter.</param>
    /// <param name="arg3">Third parameter.</param>
    /// <param name="frame">Registering function's frame.</param>
    public static void deferǃ<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3, ref GoFrame frame)
    {
        frame.Push(() => action(arg1, arg2, arg3));
    }

    /// <summary>Registers a deferred call to a value-returning function, discarding the result.</summary>
    public static void deferǃ<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> action, T1 arg1, T2 arg2, T3 arg3, ref GoFrame frame)
    {
        frame.Push(() => { action(arg1, arg2, arg3); });
    }

    /// <summary>
    /// Registers a deferred call with 4 parameters, evaluated now and passed on exit.
    /// </summary>
    /// <typeparam name="T1">First parameter type.</typeparam>
    /// <typeparam name="T2">Second parameter type.</typeparam>
    /// <typeparam name="T3">Third parameter type.</typeparam>
    /// <typeparam name="T4">Fourth parameter type.</typeparam>
    /// <param name="action">Deferred call.</param>
    /// <param name="arg1">First parameter.</param>
    /// <param name="arg2">Second parameter.</param>
    /// <param name="arg3">Third parameter.</param>
    /// <param name="arg4">Fourth parameter.</param>
    /// <param name="frame">Registering function's frame.</param>
    public static void deferǃ<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, ref GoFrame frame)
    {
        frame.Push(() => action(arg1, arg2, arg3, arg4));
    }

    /// <summary>Registers a deferred call to a value-returning function, discarding the result.</summary>
    public static void deferǃ<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, ref GoFrame frame)
    {
        frame.Push(() => { action(arg1, arg2, arg3, arg4); });
    }

    /// <summary>
    /// Registers a deferred call with 5 parameters, evaluated now and passed on exit.
    /// </summary>
    /// <typeparam name="T1">First parameter type.</typeparam>
    /// <typeparam name="T2">Second parameter type.</typeparam>
    /// <typeparam name="T3">Third parameter type.</typeparam>
    /// <typeparam name="T4">Fourth parameter type.</typeparam>
    /// <typeparam name="T5">Fifth parameter type.</typeparam>
    /// <param name="action">Deferred call.</param>
    /// <param name="arg1">First parameter.</param>
    /// <param name="arg2">Second parameter.</param>
    /// <param name="arg3">Third parameter.</param>
    /// <param name="arg4">Fourth parameter.</param>
    /// <param name="arg5">Fifth parameter.</param>
    /// <param name="frame">Registering function's frame.</param>
    public static void deferǃ<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, ref GoFrame frame)
    {
        frame.Push(() => action(arg1, arg2, arg3, arg4, arg5));
    }

    /// <summary>Registers a deferred call to a value-returning function, discarding the result.</summary>
    public static void deferǃ<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, ref GoFrame frame)
    {
        frame.Push(() => { action(arg1, arg2, arg3, arg4, arg5); });
    }

    /// <summary>
    /// Registers a deferred call with 6 parameters, evaluated now and passed on exit.
    /// </summary>
    /// <typeparam name="T1">First parameter type.</typeparam>
    /// <typeparam name="T2">Second parameter type.</typeparam>
    /// <typeparam name="T3">Third parameter type.</typeparam>
    /// <typeparam name="T4">Fourth parameter type.</typeparam>
    /// <typeparam name="T5">Fifth parameter type.</typeparam>
    /// <typeparam name="T6">Sixth parameter type.</typeparam>
    /// <param name="action">Deferred call.</param>
    /// <param name="arg1">First parameter.</param>
    /// <param name="arg2">Second parameter.</param>
    /// <param name="arg3">Third parameter.</param>
    /// <param name="arg4">Fourth parameter.</param>
    /// <param name="arg5">Fifth parameter.</param>
    /// <param name="arg6">Sixth parameter.</param>
    /// <param name="frame">Registering function's frame.</param>
    public static void deferǃ<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, ref GoFrame frame)
    {
        frame.Push(() => action(arg1, arg2, arg3, arg4, arg5, arg6));
    }

    /// <summary>Registers a deferred call to a value-returning function, discarding the result.</summary>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, TResult>(Func<T1, T2, T3, T4, T5, T6, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, ref GoFrame frame)
    {
        frame.Push(() => { action(arg1, arg2, arg3, arg4, arg5, arg6); });
    }

    /// <summary>
    /// Registers a deferred call with 7 parameters, evaluated now and passed on exit.
    /// </summary>
    /// <typeparam name="T1">First parameter type.</typeparam>
    /// <typeparam name="T2">Second parameter type.</typeparam>
    /// <typeparam name="T3">Third parameter type.</typeparam>
    /// <typeparam name="T4">Fourth parameter type.</typeparam>
    /// <typeparam name="T5">Fifth parameter type.</typeparam>
    /// <typeparam name="T6">Sixth parameter type.</typeparam>
    /// <typeparam name="T7">Seventh parameter type.</typeparam>
    /// <param name="action">Deferred call.</param>
    /// <param name="arg1">First parameter.</param>
    /// <param name="arg2">Second parameter.</param>
    /// <param name="arg3">Third parameter.</param>
    /// <param name="arg4">Fourth parameter.</param>
    /// <param name="arg5">Fifth parameter.</param>
    /// <param name="arg6">Sixth parameter.</param>
    /// <param name="arg7">Seventh parameter.</param>
    /// <param name="frame">Registering function's frame.</param>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, T7>(Action<T1, T2, T3, T4, T5, T6, T7> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, ref GoFrame frame)
    {
        frame.Push(() => action(arg1, arg2, arg3, arg4, arg5, arg6, arg7));
    }

    /// <summary>Registers a deferred call to a value-returning function, discarding the result.</summary>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, T7, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, ref GoFrame frame)
    {
        frame.Push(() => { action(arg1, arg2, arg3, arg4, arg5, arg6, arg7); });
    }

    /// <summary>
    /// Registers a deferred call with 8 parameters, evaluated now and passed on exit.
    /// </summary>
    /// <typeparam name="T1">First parameter type.</typeparam>
    /// <typeparam name="T2">Second parameter type.</typeparam>
    /// <typeparam name="T3">Third parameter type.</typeparam>
    /// <typeparam name="T4">Fourth parameter type.</typeparam>
    /// <typeparam name="T5">Fifth parameter type.</typeparam>
    /// <typeparam name="T6">Sixth parameter type.</typeparam>
    /// <typeparam name="T7">Seventh parameter type.</typeparam>
    /// <typeparam name="T8">Eighth parameter type.</typeparam>
    /// <param name="action">Deferred call.</param>
    /// <param name="arg1">First parameter.</param>
    /// <param name="arg2">Second parameter.</param>
    /// <param name="arg3">Third parameter.</param>
    /// <param name="arg4">Fourth parameter.</param>
    /// <param name="arg5">Fifth parameter.</param>
    /// <param name="arg6">Sixth parameter.</param>
    /// <param name="arg7">Seventh parameter.</param>
    /// <param name="arg8">Eighth parameter.</param>
    /// <param name="frame">Registering function's frame.</param>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, T7, T8>(Action<T1, T2, T3, T4, T5, T6, T7, T8> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, ref GoFrame frame)
    {
        frame.Push(() => action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8));
    }

    /// <summary>Registers a deferred call to a value-returning function, discarding the result.</summary>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, ref GoFrame frame)
    {
        frame.Push(() => { action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8); });
    }

    /// <summary>
    /// Registers a deferred call with 9 parameters, evaluated now and passed on exit.
    /// </summary>
    /// <typeparam name="T1">First parameter type.</typeparam>
    /// <typeparam name="T2">Second parameter type.</typeparam>
    /// <typeparam name="T3">Third parameter type.</typeparam>
    /// <typeparam name="T4">Fourth parameter type.</typeparam>
    /// <typeparam name="T5">Fifth parameter type.</typeparam>
    /// <typeparam name="T6">Sixth parameter type.</typeparam>
    /// <typeparam name="T7">Seventh parameter type.</typeparam>
    /// <typeparam name="T8">Eighth parameter type.</typeparam>
    /// <typeparam name="T9">Ninth parameter type.</typeparam>
    /// <param name="action">Deferred call.</param>
    /// <param name="arg1">First parameter.</param>
    /// <param name="arg2">Second parameter.</param>
    /// <param name="arg3">Third parameter.</param>
    /// <param name="arg4">Fourth parameter.</param>
    /// <param name="arg5">Fifth parameter.</param>
    /// <param name="arg6">Sixth parameter.</param>
    /// <param name="arg7">Seventh parameter.</param>
    /// <param name="arg8">Eighth parameter.</param>
    /// <param name="arg9">Ninth parameter.</param>
    /// <param name="frame">Registering function's frame.</param>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, ref GoFrame frame)
    {
        frame.Push(() => action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9));
    }

    /// <summary>Registers a deferred call to a value-returning function, discarding the result.</summary>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, ref GoFrame frame)
    {
        frame.Push(() => { action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9); });
    }

    /// <summary>
    /// Registers a deferred call with 10 parameters, evaluated now and passed on exit.
    /// </summary>
    /// <typeparam name="T1">First parameter type.</typeparam>
    /// <typeparam name="T2">Second parameter type.</typeparam>
    /// <typeparam name="T3">Third parameter type.</typeparam>
    /// <typeparam name="T4">Fourth parameter type.</typeparam>
    /// <typeparam name="T5">Fifth parameter type.</typeparam>
    /// <typeparam name="T6">Sixth parameter type.</typeparam>
    /// <typeparam name="T7">Seventh parameter type.</typeparam>
    /// <typeparam name="T8">Eighth parameter type.</typeparam>
    /// <typeparam name="T9">Ninth parameter type.</typeparam>
    /// <typeparam name="T10">Tenth parameter type.</typeparam>
    /// <param name="action">Deferred call.</param>
    /// <param name="arg1">First parameter.</param>
    /// <param name="arg2">Second parameter.</param>
    /// <param name="arg3">Third parameter.</param>
    /// <param name="arg4">Fourth parameter.</param>
    /// <param name="arg5">Fifth parameter.</param>
    /// <param name="arg6">Sixth parameter.</param>
    /// <param name="arg7">Seventh parameter.</param>
    /// <param name="arg8">Eighth parameter.</param>
    /// <param name="arg9">Ninth parameter.</param>
    /// <param name="arg10">Tenth parameter.</param>
    /// <param name="frame">Registering function's frame.</param>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, ref GoFrame frame)
    {
        frame.Push(() => action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10));
    }

    /// <summary>Registers a deferred call to a value-returning function, discarding the result.</summary>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, ref GoFrame frame)
    {
        frame.Push(() => { action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10); });
    }

    /// <summary>
    /// Registers a deferred call with 11 parameters, evaluated now and passed on exit.
    /// </summary>
    /// <typeparam name="T1">First parameter type.</typeparam>
    /// <typeparam name="T2">Second parameter type.</typeparam>
    /// <typeparam name="T3">Third parameter type.</typeparam>
    /// <typeparam name="T4">Fourth parameter type.</typeparam>
    /// <typeparam name="T5">Fifth parameter type.</typeparam>
    /// <typeparam name="T6">Sixth parameter type.</typeparam>
    /// <typeparam name="T7">Seventh parameter type.</typeparam>
    /// <typeparam name="T8">Eighth parameter type.</typeparam>
    /// <typeparam name="T9">Ninth parameter type.</typeparam>
    /// <typeparam name="T10">Tenth parameter type.</typeparam>
    /// <typeparam name="T11">Eleventh parameter type.</typeparam>
    /// <param name="action">Deferred call.</param>
    /// <param name="arg1">First parameter.</param>
    /// <param name="arg2">Second parameter.</param>
    /// <param name="arg3">Third parameter.</param>
    /// <param name="arg4">Fourth parameter.</param>
    /// <param name="arg5">Fifth parameter.</param>
    /// <param name="arg6">Sixth parameter.</param>
    /// <param name="arg7">Seventh parameter.</param>
    /// <param name="arg8">Eighth parameter.</param>
    /// <param name="arg9">Ninth parameter.</param>
    /// <param name="arg10">Tenth parameter.</param>
    /// <param name="arg11">Eleventh parameter.</param>
    /// <param name="frame">Registering function's frame.</param>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, ref GoFrame frame)
    {
        frame.Push(() => action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11));
    }

    /// <summary>Registers a deferred call to a value-returning function, discarding the result.</summary>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, ref GoFrame frame)
    {
        frame.Push(() => { action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11); });
    }

    /// <summary>
    /// Registers a deferred call with 12 parameters, evaluated now and passed on exit.
    /// </summary>
    /// <typeparam name="T1">First parameter type.</typeparam>
    /// <typeparam name="T2">Second parameter type.</typeparam>
    /// <typeparam name="T3">Third parameter type.</typeparam>
    /// <typeparam name="T4">Fourth parameter type.</typeparam>
    /// <typeparam name="T5">Fifth parameter type.</typeparam>
    /// <typeparam name="T6">Sixth parameter type.</typeparam>
    /// <typeparam name="T7">Seventh parameter type.</typeparam>
    /// <typeparam name="T8">Eighth parameter type.</typeparam>
    /// <typeparam name="T9">Ninth parameter type.</typeparam>
    /// <typeparam name="T10">Tenth parameter type.</typeparam>
    /// <typeparam name="T11">Eleventh parameter type.</typeparam>
    /// <typeparam name="T12">Twelfth parameter type.</typeparam>
    /// <param name="action">Deferred call.</param>
    /// <param name="arg1">First parameter.</param>
    /// <param name="arg2">Second parameter.</param>
    /// <param name="arg3">Third parameter.</param>
    /// <param name="arg4">Fourth parameter.</param>
    /// <param name="arg5">Fifth parameter.</param>
    /// <param name="arg6">Sixth parameter.</param>
    /// <param name="arg7">Seventh parameter.</param>
    /// <param name="arg8">Eighth parameter.</param>
    /// <param name="arg9">Ninth parameter.</param>
    /// <param name="arg10">Tenth parameter.</param>
    /// <param name="arg11">Eleventh parameter.</param>
    /// <param name="arg12">Twelfth parameter.</param>
    /// <param name="frame">Registering function's frame.</param>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, ref GoFrame frame)
    {
        frame.Push(() => action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12));
    }

    /// <summary>Registers a deferred call to a value-returning function, discarding the result.</summary>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, ref GoFrame frame)
    {
        frame.Push(() => { action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12); });
    }

    /// <summary>
    /// Registers a deferred call with 13 parameters, evaluated now and passed on exit.
    /// </summary>
    /// <typeparam name="T1">First parameter type.</typeparam>
    /// <typeparam name="T2">Second parameter type.</typeparam>
    /// <typeparam name="T3">Third parameter type.</typeparam>
    /// <typeparam name="T4">Fourth parameter type.</typeparam>
    /// <typeparam name="T5">Fifth parameter type.</typeparam>
    /// <typeparam name="T6">Sixth parameter type.</typeparam>
    /// <typeparam name="T7">Seventh parameter type.</typeparam>
    /// <typeparam name="T8">Eighth parameter type.</typeparam>
    /// <typeparam name="T9">Ninth parameter type.</typeparam>
    /// <typeparam name="T10">Tenth parameter type.</typeparam>
    /// <typeparam name="T11">Eleventh parameter type.</typeparam>
    /// <typeparam name="T12">Twelfth parameter type.</typeparam>
    /// <typeparam name="T13">Thirteenth parameter type.</typeparam>
    /// <param name="action">Deferred call.</param>
    /// <param name="arg1">First parameter.</param>
    /// <param name="arg2">Second parameter.</param>
    /// <param name="arg3">Third parameter.</param>
    /// <param name="arg4">Fourth parameter.</param>
    /// <param name="arg5">Fifth parameter.</param>
    /// <param name="arg6">Sixth parameter.</param>
    /// <param name="arg7">Seventh parameter.</param>
    /// <param name="arg8">Eighth parameter.</param>
    /// <param name="arg9">Ninth parameter.</param>
    /// <param name="arg10">Tenth parameter.</param>
    /// <param name="arg11">Eleventh parameter.</param>
    /// <param name="arg12">Twelfth parameter.</param>
    /// <param name="arg13">Thirteenth parameter.</param>
    /// <param name="frame">Registering function's frame.</param>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, ref GoFrame frame)
    {
        frame.Push(() => action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13));
    }

    /// <summary>Registers a deferred call to a value-returning function, discarding the result.</summary>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, ref GoFrame frame)
    {
        frame.Push(() => { action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13); });
    }

    /// <summary>
    /// Registers a deferred call with 14 parameters, evaluated now and passed on exit.
    /// </summary>
    /// <typeparam name="T1">First parameter type.</typeparam>
    /// <typeparam name="T2">Second parameter type.</typeparam>
    /// <typeparam name="T3">Third parameter type.</typeparam>
    /// <typeparam name="T4">Fourth parameter type.</typeparam>
    /// <typeparam name="T5">Fifth parameter type.</typeparam>
    /// <typeparam name="T6">Sixth parameter type.</typeparam>
    /// <typeparam name="T7">Seventh parameter type.</typeparam>
    /// <typeparam name="T8">Eighth parameter type.</typeparam>
    /// <typeparam name="T9">Ninth parameter type.</typeparam>
    /// <typeparam name="T10">Tenth parameter type.</typeparam>
    /// <typeparam name="T11">Eleventh parameter type.</typeparam>
    /// <typeparam name="T12">Twelfth parameter type.</typeparam>
    /// <typeparam name="T13">Thirteenth parameter type.</typeparam>
    /// <typeparam name="T14">Fourteenth parameter type.</typeparam>
    /// <param name="action">Deferred call.</param>
    /// <param name="arg1">First parameter.</param>
    /// <param name="arg2">Second parameter.</param>
    /// <param name="arg3">Third parameter.</param>
    /// <param name="arg4">Fourth parameter.</param>
    /// <param name="arg5">Fifth parameter.</param>
    /// <param name="arg6">Sixth parameter.</param>
    /// <param name="arg7">Seventh parameter.</param>
    /// <param name="arg8">Eighth parameter.</param>
    /// <param name="arg9">Ninth parameter.</param>
    /// <param name="arg10">Tenth parameter.</param>
    /// <param name="arg11">Eleventh parameter.</param>
    /// <param name="arg12">Twelfth parameter.</param>
    /// <param name="arg13">Thirteenth parameter.</param>
    /// <param name="arg14">Fourteenth parameter.</param>
    /// <param name="frame">Registering function's frame.</param>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, ref GoFrame frame)
    {
        frame.Push(() => action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14));
    }

    /// <summary>Registers a deferred call to a value-returning function, discarding the result.</summary>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, ref GoFrame frame)
    {
        frame.Push(() => { action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14); });
    }

    /// <summary>
    /// Registers a deferred call with 15 parameters, evaluated now and passed on exit.
    /// </summary>
    /// <typeparam name="T1">First parameter type.</typeparam>
    /// <typeparam name="T2">Second parameter type.</typeparam>
    /// <typeparam name="T3">Third parameter type.</typeparam>
    /// <typeparam name="T4">Fourth parameter type.</typeparam>
    /// <typeparam name="T5">Fifth parameter type.</typeparam>
    /// <typeparam name="T6">Sixth parameter type.</typeparam>
    /// <typeparam name="T7">Seventh parameter type.</typeparam>
    /// <typeparam name="T8">Eighth parameter type.</typeparam>
    /// <typeparam name="T9">Ninth parameter type.</typeparam>
    /// <typeparam name="T10">Tenth parameter type.</typeparam>
    /// <typeparam name="T11">Eleventh parameter type.</typeparam>
    /// <typeparam name="T12">Twelfth parameter type.</typeparam>
    /// <typeparam name="T13">Thirteenth parameter type.</typeparam>
    /// <typeparam name="T14">Fourteenth parameter type.</typeparam>
    /// <typeparam name="T15">Fifteenth parameter type.</typeparam>
    /// <param name="action">Deferred call.</param>
    /// <param name="arg1">First parameter.</param>
    /// <param name="arg2">Second parameter.</param>
    /// <param name="arg3">Third parameter.</param>
    /// <param name="arg4">Fourth parameter.</param>
    /// <param name="arg5">Fifth parameter.</param>
    /// <param name="arg6">Sixth parameter.</param>
    /// <param name="arg7">Seventh parameter.</param>
    /// <param name="arg8">Eighth parameter.</param>
    /// <param name="arg9">Ninth parameter.</param>
    /// <param name="arg10">Tenth parameter.</param>
    /// <param name="arg11">Eleventh parameter.</param>
    /// <param name="arg12">Twelfth parameter.</param>
    /// <param name="arg13">Thirteenth parameter.</param>
    /// <param name="arg14">Fourteenth parameter.</param>
    /// <param name="arg15">Fifteenth parameter.</param>
    /// <param name="frame">Registering function's frame.</param>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, ref GoFrame frame)
    {
        frame.Push(() => action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15));
    }

    /// <summary>Registers a deferred call to a value-returning function, discarding the result.</summary>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, ref GoFrame frame)
    {
        frame.Push(() => { action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15); });
    }

    /// <summary>
    /// Registers a deferred call with 16 parameters, evaluated now and passed on exit.
    /// </summary>
    /// <typeparam name="T1">First parameter type.</typeparam>
    /// <typeparam name="T2">Second parameter type.</typeparam>
    /// <typeparam name="T3">Third parameter type.</typeparam>
    /// <typeparam name="T4">Fourth parameter type.</typeparam>
    /// <typeparam name="T5">Fifth parameter type.</typeparam>
    /// <typeparam name="T6">Sixth parameter type.</typeparam>
    /// <typeparam name="T7">Seventh parameter type.</typeparam>
    /// <typeparam name="T8">Eighth parameter type.</typeparam>
    /// <typeparam name="T9">Ninth parameter type.</typeparam>
    /// <typeparam name="T10">Tenth parameter type.</typeparam>
    /// <typeparam name="T11">Eleventh parameter type.</typeparam>
    /// <typeparam name="T12">Twelfth parameter type.</typeparam>
    /// <typeparam name="T13">Thirteenth parameter type.</typeparam>
    /// <typeparam name="T14">Fourteenth parameter type.</typeparam>
    /// <typeparam name="T15">Fifteenth parameter type.</typeparam>
    /// <typeparam name="T16">Sixteenth parameter type.</typeparam>
    /// <param name="action">Deferred call.</param>
    /// <param name="arg1">First parameter.</param>
    /// <param name="arg2">Second parameter.</param>
    /// <param name="arg3">Third parameter.</param>
    /// <param name="arg4">Fourth parameter.</param>
    /// <param name="arg5">Fifth parameter.</param>
    /// <param name="arg6">Sixth parameter.</param>
    /// <param name="arg7">Seventh parameter.</param>
    /// <param name="arg8">Eighth parameter.</param>
    /// <param name="arg9">Ninth parameter.</param>
    /// <param name="arg10">Tenth parameter.</param>
    /// <param name="arg11">Eleventh parameter.</param>
    /// <param name="arg12">Twelfth parameter.</param>
    /// <param name="arg13">Thirteenth parameter.</param>
    /// <param name="arg14">Fourteenth parameter.</param>
    /// <param name="arg15">Fifteenth parameter.</param>
    /// <param name="arg16">Sixteenth parameter.</param>
    /// <param name="frame">Registering function's frame.</param>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16, ref GoFrame frame)
    {
        frame.Push(() => action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16));
    }

    /// <summary>Registers a deferred call to a value-returning function, discarding the result.</summary>
    public static void deferǃ<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16, ref GoFrame frame)
    {
        frame.Push(() => { action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16); });
    }

    #endregion
}
