// Copyright 2018 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build dragonfly || freebsd || linux || netbsd || (openbsd && mips64)
namespace go.@internal;

using Δsyscall = syscall_package;
using @unsafe = unsafe_package;

partial class poll_package {

internal static (uintptr, error) writev(nint fd, slice<Δsyscall.Iovec> iovecs) {
    uintptr r = default!;
    Δsyscall.Errno e = default!;
    while (ᐧ) {
        (r, _, e) = Δsyscall.Syscall(Δsyscall.SYS_WRITEV, (uintptr)fd, (uintptr)Ꮡ(iovecs, 0), (uintptr)len(iovecs));
        if (e != Δsyscall.EINTR) {
            break;
        }
    }
    if (e != 0) {
        return (r, e);
    }
    return (r, default!);
}

} // end poll_package
