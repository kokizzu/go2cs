// Copyright 2018 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build dragonfly || freebsd || linux || netbsd || (openbsd && mips64)
namespace go.@internal.syscall;

using syscall = syscall_package;
using @unsafe = unsafe_package;

partial class unix_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸsyscall() {
    builtin.initPackage(typeof(syscall_package));
}

public static error Unlinkat(nint dirfd, @string path, nint flags) {
    var (p, err) = syscall.BytePtrFromString(path);
    if (err != default!) {
        return err;
    }
    var ᴋ0 = p;
        var (_, _, errno) = syscall.Syscall(unlinkatTrap, (uintptr)dirfd, (uintptr)ᴋ0, (uintptr)flags);
    System.GC.KeepAlive(ᴋ0);
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
    var ᴋ1 = p;
        var (fd, _, errno) = syscall.Syscall6(openatTrap, (uintptr)dirfd, (uintptr)ᴋ1, (uintptr)flags, (uintptr)perm, 0, 0);
    System.GC.KeepAlive(ᴋ1);
    if (errno != 0) {
        return (0, errno);
    }
    return ((nint)fd, default!);
}

} // end unix_package
