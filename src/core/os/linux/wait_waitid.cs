// Copyright 2016 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
// We used to use this code for Darwin, but according to issue #19314
// waitid returns if the process is stopped, even when using WEXITED.
//go:build linux
namespace go;

using Δruntime = runtime_package;
using syscall = syscall_package;
using @unsafe = unsafe_package;

partial class os_package {

internal static UntypedInt _P_PID => 1;

// blockUntilWaitable attempts to block until a call to p.Wait will
// succeed immediately, and reports whether it has done so.
// It does not actually call p.Wait.
internal static (bool, error) blockUntilWaitable(this ж<Process> Ꮡp) {
    ref var p = ref Ꮡp.DerefOrNull();

    // The waitid system call expects a pointer to a siginfo_t,
    // which is 128 bytes on all Linux systems.
    // On darwin/amd64, it requires 104 bytes.
    // We don't care about the values it returns.
    ref var siginfo = ref heap(new array<uint64>(16), out var Ꮡsiginfo);
    var psig = Ꮡsiginfo.at<uint64>(0);
    syscall.Errno e = default!;
    while (ᐧ) {
        (_, _, e) = syscall.Syscall6(syscall.SYS_WAITID, _P_PID, (uintptr)p.Pid, (uintptr)psig, (uintptr)((uintptr)syscall.WEXITED | (uintptr)syscall.WNOWAIT), 0, 0);
        if (e != syscall.EINTR) {
            break;
        }
    }
    Δruntime.KeepAlive(Ꮡp.OrTypedNil());
    if (e != 0) {
        // waitid has been available since Linux 2.6.9, but
        // reportedly is not available in Ubuntu on Windows.
        // See issue 16610.
        if (e == syscall.ENOSYS) {
            return (false, default!);
        }
        return (false, NewSyscallError(waitidˢ, e));
    }
    return (true, default!);
}

} // end os_package
