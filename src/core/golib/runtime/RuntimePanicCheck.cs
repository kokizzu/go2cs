// RuntimePanicCheck.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;
using System.Diagnostics;
using System.Reflection;

namespace go.golib;

/// <summary>
/// Go's <c>panicCheck1</c>: a bounds or shift panic raised by code IN package <c>runtime</c> is a fatal
/// error, not a panic. <c>recover()</c> never sees it, and the process exits 2.
/// </summary>
/// <remarks>
/// <para>
/// Go's index, slice, slice3 and slice-convert bounds checks and its negative-shift check reach
/// <c>panicCheck1(callerpc, msg)</c>, which throws when the faulting function's name has the
/// <c>"runtime."</c> prefix: the runtime faulting on its own data is a runtime bug, never a value a
/// program should catch. The divide and nil-dereference checks are <c>panicCheck2</c>, which throws only
/// while mallocing, so they stay recoverable here and are not routed through this class.
/// </para>
/// <para>
/// The marker is STRUCTURAL. The converter declares every member of package <c>runtime</c> on
/// <c>go.runtime_package</c>, and a <c>-tests</c> build puts package runtime's own <c>_test.go</c> files
/// (Go names them <c>runtime.*</c> too) on the internal-test bridge <c>go.runtime_internal_test_package</c>,
/// in both cases directly or on a type nested in the class. <c>runtime_test_package</c> (package
/// <c>runtime_test</c>) and <c>internal/runtime/*</c> are other classes, which matches Go's prefix
/// exactly. No message text is read and no assembly name is guessed.
/// </para>
/// <para>
/// The raising frame must still EXIST for the check to see it. A method the JIT inlines into a
/// non-runtime caller hides, and the panic then stays recoverable, which is the behavior before this
/// check and never a false fatal. Go's <c>//go:noinline</c> is not carried to the emission as
/// <c>NoInlining</c>, so under full optimization a tiny runtime helper can inline away.
/// </para>
/// <para>
/// COST: one <see cref="StackTrace"/> capture per bounds or shift panic, and nothing on the non-panic
/// path. The capture is of the WHOLE stack, so the cost grows with depth: measured on linux Release,
/// about 8 µs at 5 frames and 22 to 25 µs at 50, against about 3 µs for the throw and catch alone.
/// </para>
/// </remarks>
internal static class RuntimePanicCheck
{
    /// <summary>
    /// Terminates the process with Go's fatal report when the bounds or shift panic being raised
    /// comes from package <c>runtime</c>, and returns otherwise.
    /// </summary>
    /// <param name="throwText">Go's throw text for the check, such as <c>"index out of range"</c>.</param>
    internal static void Check(string throwText)
    {
        if (RaisedInRuntimePackage(out _))
            FatalReport.Fatal(throwText, userFault: false);
    }

    /// <summary>
    /// Reports whether the first frame outside golib is declared on package <c>runtime</c>'s class,
    /// its internal-test bridge, or a type nested in either, and names that frame.
    /// </summary>
    internal static bool RaisedInRuntimePackage(out string frameName)
    {
        frameName = "";
        StackTrace trace = new(1, false);
        Assembly golib = typeof(RuntimePanicCheck).Assembly;

        for (int i = 0; i < trace.FrameCount; i++)
        {
            MethodBase? method = trace.GetFrame(i)?.GetMethod();
            Type? type = method?.DeclaringType;

            if (type is null || type.Assembly == golib)
                continue;

            frameName = $"{type.FullName}.{method!.Name}";
            return IsRuntimePackageType(type);
        }

        return false;
    }

    /// <summary>
    /// Reports whether <paramref name="type"/> is package <c>runtime</c>'s class or its internal-test
    /// bridge, or is nested in either.
    /// </summary>
    internal static bool IsRuntimePackageType(Type type)
    {
        for (Type? outer = type; outer is not null; outer = outer.DeclaringType)
        {
            if (outer.FullName is "go.runtime_package" or "go.runtime_internal_test_package")
                return true;
        }

        return false;
    }
}
