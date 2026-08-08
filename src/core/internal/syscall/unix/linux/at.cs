// Copyright 2018 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build dragonfly || freebsd || linux || netbsd || (openbsd && mips64)
namespace go.@internal.syscall;

using syscall = syscall_package;
using @unsafe = unsafe_package;

partial class unix_package {

public static error Unlinkat(nint dirfd, @string path, nint flags) {
    var (p, err) = syscall.BytePtrFromString(path);
    if (err != default!) {
        return err;
    }
    var (_, _, errno) = syscall.Syscall(unlinkatTrap, (uintptr)dirfd, (uintptr)p, (uintptr)flags);
    if (errno != 0) {
        return errno;
    }
    return default!;
}

public static (nint, error) Openat(nint dirfd, @string path, nint flags, uint32 perm) {
    var (p, err) = syscall.BytePtrFromString(path);
    if (err != default!) {
        return (0, err);
    }
    var (fd, _, errno) = syscall.Syscall6(openatTrap, (uintptr)dirfd, (uintptr)p, (uintptr)flags, (uintptr)perm, 0, 0);
    if (errno != 0) {
        return (0, errno);
    }
    return ((nint)fd, default!);
}

} // end unix_package
