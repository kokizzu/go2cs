// Copyright 2010 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go;

using Δio = io_package;
using Δos = os_package;
using static go.flag_package;

partial class flag_internal_test_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸio() {
    builtin.initPackage(typeof(io_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸos() {
    builtin.initPackage(typeof(os_package));
}

// Additional routines compiled into the package only during testing.
public static Action DefaultUsage;
internal static void initᴛDefaultUsage() { DefaultUsage = Usage; }

// ResetForTesting clears all flag state and sets the usage function as directed.
// After calling ResetForTesting, parse errors in flag handling will not
// exit the program.
public static void ResetForTesting(Action usage) {
    CommandLine = NewFlagSet(Δos.Args[0], ContinueOnError);
    CommandLine.SetOutput(Δio.Discard);
    CommandLine.Value.Usage = commandLineUsage;
    Usage = usage;
}

} // end flag_internal_test_package
