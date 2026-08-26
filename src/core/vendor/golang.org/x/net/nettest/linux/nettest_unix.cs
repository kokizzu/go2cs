// Copyright 2019 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build aix || darwin || dragonfly || freebsd || linux || netbsd || openbsd || solaris || zos
namespace go.vendor.golang.org.x.net;

using syscall = syscall_package;

partial class nettest_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸsyscall() {
    builtin.initPackage(typeof(syscall_package));
}

internal static bool supportsRawSocket() {
    foreach (var (_, af) in new nint[]{syscall.AF_INET, syscall.AF_INET6}.slice()) {
        var (s, err) = syscall.Socket(af, syscall.SOCK_RAW, 0);
        if (err != default!) {
            continue;
        }
        syscall.Close(s);
        return true;
    }
    return false;
}

} // end nettest_package
