// Copyright 2022 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

// Package syscall provides the syscall primitives required for the runtime.
namespace go.@internal.runtime;

using @unsafe = unsafe_package;

partial class syscall_package {

// TODO(https://go.dev/issue/51087): This package is incomplete and currently
// only contains very minimal support for Linux.

// Syscall6 calls system call number 'num' with arguments a1-6.
public static partial (uintptr r1, uintptr r2, uintptr errno) Syscall6(uintptr num, uintptr a1, uintptr a2, uintptr a3, uintptr a4, uintptr a5, uintptr a6);

public static (int32 fd, uintptr errno) EpollCreate1(int32 flags) {
    var (r1, _, e) = Syscall6(SYS_EPOLL_CREATE1, (uintptr)flags, 0, 0, 0, 0, 0);
    return ((int32)r1, e);
}

internal static ж<uintptr> Ꮡ_zero = new StandardBox<uintptr>(default(uintptr));
internal static ref uintptr _zero => ref Ꮡ_zero.Value;

public static (int32 n, uintptr errno) EpollWait(int32 epfd, slice<EpollEvent> events, int32 maxev, int32 waitms) {
    @unsafe.Pointer ev = default!;
    if (len(events) > 0){
        ev = @unsafe.Pointer.FromPinnedBox(Ꮡ(events, 0));
    } else {
        ev = @unsafe.Pointer.FromBox(Ꮡ_zero);
    }
    var ᴋ0 = ev;
        var (r1, _, e) = Syscall6(SYS_EPOLL_PWAIT, (uintptr)epfd, (uintptr)ᴋ0, (uintptr)maxev, (uintptr)waitms, 0, 0);
    System.GC.KeepAlive(ᴋ0);
    return ((int32)r1, e);
}

public static uintptr /*errno*/ EpollCtl(int32 epfd, int32 op, int32 fd, ж<EpollEvent> Ꮡevent) {
    var ᴋ1 = Ꮡevent;
        var (_, _, e) = Syscall6(SYS_EPOLL_CTL, (uintptr)epfd, (uintptr)op, (uintptr)fd, (uintptr)ᴋ1, 0, 0);
    System.GC.KeepAlive(ᴋ1);
    return e;
}

public static (int32 fd, uintptr errno) Eventfd(int32 initval, int32 flags) {
    var (r1, _, e) = Syscall6(SYS_EVENTFD2, (uintptr)initval, (uintptr)flags, 0, 0, 0, 0);
    return ((int32)r1, e);
}

} // end syscall_package
