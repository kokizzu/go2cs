// Copyright 2019 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build aix || darwin || freebsd || linux || netbsd || openbsd || solaris
namespace go;

using Δruntime = runtime_package;

partial class syscall_package {

// Round the length of a raw sockaddr up to align it properly.
internal static nint cmsgAlignOf(nint salen) {
    nint salign = sizeofPtr;
    // dragonfly needs to check ABI version at runtime, see cmsgAlignOf in
    // sockcmsg_dragonfly.go
    var exprᴛ1 = Δruntime.GOOS;
    if (exprᴛ1 == "aix"u8) {
        salign = 1;
    }
    else if (exprᴛ1 == "darwin"u8 || exprᴛ1 == "ios"u8 || exprᴛ1 == "illumos"u8 || exprᴛ1 == "solaris"u8) {
        if (sizeofPtr == 8) {
            // There is no alignment on AIX.
            // NOTE: It seems like 64-bit Darwin, Illumos and Solaris
            // kernels still require 32-bit aligned access to network
            // subsystem.
            salign = 4;
        }
    }
    else if (exprᴛ1 == "netbsd"u8 || exprᴛ1 == "openbsd"u8) {
        if (Δruntime.GOARCH == "arm"u8) {
            // NetBSD and OpenBSD armv7 require 64-bit alignment.
            salign = 8;
        }
        if (Δruntime.GOOS == "netbsd"u8 && Δruntime.GOARCH == "arm64"u8) {
            // NetBSD aarch64 requires 128-bit alignment.
            salign = 16;
        }
    }

    return (nint)((salen + salign - 1) & ~(salign - 1));
}

} // end syscall_package
