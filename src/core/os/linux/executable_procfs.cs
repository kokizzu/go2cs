// Copyright 2016 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build linux || netbsd
namespace go;

using errors = errors_package;
using stringslite = @internal.stringslite_package;
using Δruntime = runtime_package;
using @internal;

partial class os_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸinternalꓸstringslite() {
    builtin.initPackage(typeof(@internal.stringslite_package));
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string procSelfExeˢ = "/proc/self/exe"u8;
internal static readonly @string procCurprocExeˢ = "/proc/curproc/exe"u8;
internal static readonly @string deletedˢ = " (deleted)"u8;

internal static (@string, error) executable() {
    @string procfn = default!;
    var exprᴛ1 = Δruntime.GOOS;
    if (exprᴛ1 == "linux"u8 || exprᴛ1 == "android"u8) {
        procfn = procSelfExeˢ;
    }
    else if (exprᴛ1 == "netbsd"u8) {
        procfn = procCurprocExeˢ;
    }
    else { /* default: */
        return ("", errors.New("Executable not implemented for "u8 + Δruntime.GOOS));
    }

    var (path, err) = Readlink(procfn);
    // When the executable has been deleted then Readlink returns a
    // path appended with " (deleted)".
    return (stringslite.TrimSuffix(path, deletedˢ), err);
}

} // end os_package
