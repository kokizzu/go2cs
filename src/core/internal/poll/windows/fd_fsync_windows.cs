// Copyright 2018 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
[assembly: go.GoPositionMap("internal/poll/fd_fsync_windows.go", "fd_fsync_windows.cs", "AAsU0oCCpII=")]

namespace go.@internal;

using Δsyscall = syscall_package;

partial class poll_package {

// Fsync wraps syscall.Fsync.
public static error Fsync(this ж<FD> Ꮡfd) {
    GoFrame ᒐ = default;
    try {
        ref var fd = ref Ꮡfd.DerefOrNull();

        {
            var err = Ꮡfd.incref(); if (err != default!) {
                return err;
            }
        }
        defer(() => Ꮡfd.decref(), ref ᒐ);
        return Δsyscall.Fsync(fd.Sysfd);
    }
    catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); return default!; }
    finally { ᒐ.Run(); }
}

} // end poll_package
