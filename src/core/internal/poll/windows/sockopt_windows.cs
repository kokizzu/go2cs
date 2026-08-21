// Copyright 2009 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
[assembly: go.GoPositionMap("internal/poll/sockopt_windows.go", "sockopt_windows.cs", "AAsU0oCCpII=")]

namespace go.@internal;

using Δsyscall = syscall_package;

partial class poll_package {

// WSAIoctl wraps the WSAIoctl network call.
public static error WSAIoctl(this ж<FD> Ꮡfd, uint32 iocc, ж<byte> Ꮡinbuf, uint32 cbif, ж<byte> Ꮡoutbuf, uint32 cbob, ж<uint32> Ꮡcbbr, ж<Δsyscall.Overlapped> Ꮡoverlapped, uintptr completionRoutine) {
    GoFrame ᒐ = default;
    try {
        ref var fd = ref Ꮡfd.DerefOrNull();

        {
            var err = Ꮡfd.incref(); if (err != default!) {
                return err;
            }
        }
        defer(() => Ꮡfd.decref(), ref ᒐ);
        return Δsyscall.WSAIoctl(fd.Sysfd, iocc, Ꮡinbuf, cbif, Ꮡoutbuf, cbob, Ꮡcbbr, Ꮡoverlapped, completionRoutine);
    }
    catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); return default!; }
    finally { ᒐ.Run(); }
}

} // end poll_package
