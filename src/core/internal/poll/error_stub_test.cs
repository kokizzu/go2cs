// Copyright 2019 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build !linux
namespace go.@internal;

using errors = errors_package;
using os = os_package;
using Δruntime = runtime_package;
using static go.@internal.poll_internal_test_package;

partial class poll_test_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸerrors() {
    builtin.initPackage(typeof(errors_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸos() {
    builtin.initPackage(typeof(os_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸruntime() {
    builtin.initPackage(typeof(runtime_package));
}

internal static (ж<os.File>, error) badStateFile() {
    return (default!, errors.New("not supported on "u8 + Δruntime.GOOS));
}

internal static (@string, bool) isBadStateFileError(error err) {
    return ("", false);
}

} // end poll_test_package
