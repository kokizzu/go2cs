// Copyright 2014 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go;

using @unsafe = unsafe_package;

partial class syscall_package {

// fcntl64Syscall is usually SYS_FCNTL, but is overridden on 32-bit Linux
// systems by flock_linux_32bit.go to be SYS_FCNTL64.
internal static uintptr fcntl64Syscall = SYS_FCNTL;

// FcntlFlock performs a fcntl syscall for the [F_GETLK], [F_SETLK] or [F_SETLKW] command.
public static error FcntlFlock(uintptr fd, nint cmd, ж<Flock_t> Ꮡlk) {
    var (_, _, errno) = Syscall(fcntl64Syscall, fd, (uintptr)cmd, (uintptr)Ꮡlk);
    if (errno == 0) {
        return default!;
    }
    return errno;
}

} // end syscall_package
