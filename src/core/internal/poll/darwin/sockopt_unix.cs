// Copyright 2017 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build unix
namespace go.@internal;

using Δsyscall = syscall_package;

partial class poll_package {

// SetsockoptByte wraps the setsockopt network call with a byte argument.
public static error SetsockoptByte(this ж<FD> Ꮡfd, nint level, nint name, byte arg) {
    GoFrame ᒐ = default;
    try {
        ref var fd = ref Ꮡfd.DerefOrNull();

        {
            var err = Ꮡfd.incref(); if (err != default!) {
                return err;
            }
        }
        defer(() => Ꮡfd.decref(), ref ᒐ);
        return Δsyscall.SetsockoptByte(fd.Sysfd, level, name, arg);
    }
    catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); return default!; }
    finally { ᒐ.Run(); }
}

} // end poll_package
