// Copyright 2024 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.@internal.syscall;

using syscall = syscall_package;
using @unsafe = unsafe_package;

partial class unix_package {

// Note that pgid should really be pid_t, however _C_int (aka int32) is
// generally equivalent.
public static error /*err*/ Tcsetpgrp(nint fd, int32 pgidʗp) {
    ref var pgid = ref heap(pgidʗp, out var Ꮡpgid);

    var (_, _, errno) = syscall.Syscall6(syscall.SYS_IOCTL, (uintptr)fd, (uintptr)syscall.TIOCSPGRP, (uintptr)Ꮡpgid, 0, 0, 0);
    if (errno != 0) {
        return errno;
    }
    return default!;
}

} // end unix_package
