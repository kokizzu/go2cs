// Copyright 2018 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

//go:build aix || darwin || dragonfly || freebsd || js || linux || netbsd || openbsd || solaris
// +build aix darwin dragonfly freebsd js linux netbsd openbsd solaris

// package net -- go2cs converted at 2022 March 13 05:29:44 UTC
// import "net" ==> using net = go.net_package
// Original source: C:\Program Files\Go\src\net\error_unix.go
namespace go;

using syscall = syscall_package;

public static partial class net_package {

private static bool isConnError(error err) {
    {
        syscall.Errno (se, ok) = err._<syscall.Errno>();

        if (ok) {
            return se == syscall.ECONNRESET || se == syscall.ECONNABORTED;
        }
    }
    return false;
}

} // end net_package
