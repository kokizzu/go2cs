// Copyright 2021 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build dragonfly || freebsd || linux
namespace go.@internal.syscall;

using atomic = sync.atomic_package;
using syscall = syscall_package;
using @unsafe = unsafe_package;
using sync;

partial class unix_package {

internal static ж<atomic.Bool> ᏑgetrandomUnsupported = new(default(atomic.Bool));
internal static ref atomic.Bool getrandomUnsupported => ref ᏑgetrandomUnsupported.Value;

[GoType("num:uintptr")] partial struct GetRandomFlag;

// GetRandom calls the getrandom system call.
public static (nint n, error err) GetRandom(slice<byte> p, GetRandomFlag flags) {
    if (len(p) == 0) {
        return (0, default!);
    }
    if (ᏑgetrandomUnsupported.Load()) {
        return (0, syscall.ENOSYS);
    }
    var (r1, _, errno) = syscall.Syscall(getrandomTrap,
        (uintptr)Ꮡ(p, 0),
        (uintptr)len(p),
        (uintptr)flags);
    if (errno != 0) {
        if (errno == syscall.ENOSYS) {
            ᏑgetrandomUnsupported.Store(true);
        }
        return (0, errno);
    }
    return ((nint)r1, default!);
}

} // end unix_package
