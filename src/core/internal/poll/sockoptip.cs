// Copyright 2011 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build unix || windows
[assembly: go.GoPositionMap("internal/poll/sockoptip.go", "sockoptip.cs", "AAwY0oCCpILY0oCCpII=")]

namespace go.@internal;

using Δsyscall = syscall_package;

partial class poll_package {

// SetsockoptIPMreq wraps the setsockopt network call with an IPMreq argument.
public static error SetsockoptIPMreq(this ж<FD> Ꮡfd, nint level, nint name, ж<Δsyscall.IPMreq> Ꮡmreq) {
    GoFrame ᒐ = default;
    try {
        ref var fd = ref Ꮡfd.DerefOrNull();

        {
            var err = Ꮡfd.incref(); if (err != default!) {
                return err;
            }
        }
        defer(() => Ꮡfd.decref(), ref ᒐ);
        return Δsyscall.SetsockoptIPMreq(fd.Sysfd, level, name, Ꮡmreq);
    }
    catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); return default!; }
    finally { ᒐ.Run(); }
}

// SetsockoptIPv6Mreq wraps the setsockopt network call with an IPv6Mreq argument.
public static error SetsockoptIPv6Mreq(this ж<FD> Ꮡfd, nint level, nint name, ж<Δsyscall.IPv6Mreq> Ꮡmreq) {
    GoFrame ᒐ = default;
    try {
        ref var fd = ref Ꮡfd.DerefOrNull();

        {
            var err = Ꮡfd.incref(); if (err != default!) {
                return err;
            }
        }
        defer(() => Ꮡfd.decref(), ref ᒐ);
        return Δsyscall.SetsockoptIPv6Mreq(fd.Sysfd, level, name, Ꮡmreq);
    }
    catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); return default!; }
    finally { ᒐ.Run(); }
}

} // end poll_package
