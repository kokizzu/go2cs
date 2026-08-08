// Copyright 2023 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
// Support for pidfd was added during the course of a few Linux releases:
//  v5.1: pidfd_send_signal syscall;
//  v5.2: CLONE_PIDFD flag for clone syscall;
//  v5.3: pidfd_open syscall, clone3 syscall;
//  v5.4: P_PIDFD idtype support for waitid syscall;
//  v5.6: pidfd_getfd syscall.
namespace go;

using errors = errors_package;
using unix = @internal.syscall.unix_package;
using Δsync = sync_package;
using syscall = syscall_package;
using @unsafe = unsafe_package;
using @internal.syscall;

partial class os_package {

internal static ж<syscall.SysProcAttr> ensurePidfd(ж<syscall.SysProcAttr> ᏑsysAttr) {
    ref var sysAttr = ref ᏑsysAttr.DerefOrNull();

    if (!pidfdWorks()) {
        return ᏑsysAttr;
    }
    ref var pidfd = ref heap(new nint(), out var Ꮡpidfd);
    if (ᏑsysAttr == nil) {
        return Ꮡ(new syscall.SysProcAttr(
            PidFD: Ꮡpidfd
        ));
    }
    if (sysAttr.PidFD == nil) {
        ref var newSys = ref heap<syscall.SysProcAttr>(out var ᏑnewSys);
        newSys = sysAttr; // copy
        newSys.PidFD = Ꮡpidfd;
        return ᏑnewSys;
    }
    return ᏑsysAttr;
}

internal static (uintptr, bool) getPidfd(ж<syscall.SysProcAttr> ᏑsysAttr) {
    ref var sysAttr = ref ᏑsysAttr.DerefOrNull();

    if (!pidfdWorks()) {
        return (0, false);
    }
    return ((uintptr)(sysAttr.PidFD.Value), true);
}

internal static (uintptr, error) pidfdFind(nint pid) {
    if (!pidfdWorks()) {
        return (0, syscall.ENOSYS);
    }
    var (h, err) = unix.PidFDOpen(pid, 0);
    if (err != default!) {
        return (0, convertESRCH(err));
    }
    return (h, default!);
}

// _P_PIDFD is used as idtype argument to waitid syscall.
internal static UntypedInt _P_PIDFD => 3;

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string waitidˢ = "waitid"u8;

internal static (ж<ProcessState>, error) pidfdWait(this ж<Process> Ꮡp) {
    GoFrame ᒐ = default;
    try {
        // When pidfd is used, there is no wait/kill race (described in CL 23967)
        // because the PID recycle issue doesn't exist (IOW, pidfd, unlike PID,
        // is guaranteed to refer to one particular process). Thus, there is no
        // need for the workaround (blockUntilWaitable + sigMu) from pidWait.
        //
        // We _do_ need to be careful about reuse of the pidfd FD number when
        // closing the pidfd. See handle for more details.
        var (handle, status) = Ꮡp.handleTransientAcquire();
        var exprᴛ1 = status;
        if (exprᴛ1 == statusDone) {
            return (default!, NewSyscallError(waitˢ, // Process already completed Wait, or was not found by
 // pidfdFind. Return ECHILD for consistency with what the wait
 // syscall would return.
 syscall.ECHILD));
        }
        if (exprᴛ1 == statusReleased) {
            return (default!, syscall.EINVAL);
        }

        defer(Ꮡp.handleTransientRelease, ref ᒐ);
        ref var info = ref heap(new unix.SiginfoChild(), out var Ꮡinfo);
        ref var rusage = ref heap(new syscall.Rusage(), out var Ꮡrusage);
        syscall.Errno e = default!;
        while (ᐧ) {
            (_, _, e) = syscall.Syscall6(syscall.SYS_WAITID, _P_PIDFD, handle, (uintptr)Ꮡinfo, syscall.WEXITED, (uintptr)Ꮡrusage, 0);
            if (e != syscall.EINTR) {
                break;
            }
        }
        if (e != 0) {
            return (default!, NewSyscallError(waitidˢ, e));
        }
        // Release the Process' handle reference, in addition to the reference
        // we took above.
        Ꮡp.handlePersistentRelease(statusDone);
        return (Ꮡ(new ProcessState(
            pid: (nint)info.Pid,
            status: info.WaitStatus(),
            rusage: Ꮡrusage
        )), default!);
    }
    catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); return default!; }
    finally { ᒐ.Run(); }
}

internal static error pidfdSendSignal(this ж<Process> Ꮡp, syscallꓸSignal s) {
    GoFrame ᒐ = default;
    try {
        var (handle, status) = Ꮡp.handleTransientAcquire();
        var exprᴛ1 = status;
        if (exprᴛ1 == statusDone) {
            return ErrProcessDone;
        }
        if (exprᴛ1 == statusReleased) {
            return errors.New(osProcessAlreadyReleasedˢ);
        }

        defer(Ꮡp.handleTransientRelease, ref ᒐ);
        return convertESRCH(unix.PidFDSendSignal(handle, s));
    }
    catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); return default!; }
    finally { ᒐ.Run(); }
}

internal static bool pidfdWorks() {
    return checkPidfdOnce() == default!;
}

internal static Func<error> checkPidfdOnce;
internal static void initᴛcheckPidfdOnce() { checkPidfdOnce = Δsync.OnceValue<error>(checkPidfd); }

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string pidfdOpenˢ = "pidfd_open"u8;
internal static readonly @string pidfdWaitˢ = "pidfd_wait"u8;
internal static readonly @string pidfdSendSignalˢ = "pidfd_send_signal"u8;

// checkPidfd checks whether all required pidfd-related syscalls work.
// This consists of pidfd_open and pidfd_send_signal syscalls, and waitid
// syscall with idtype of P_PIDFD.
//
// Reasons for non-working pidfd syscalls include an older kernel and an
// execution environment in which the above system calls are restricted by
// seccomp or a similar technology.
internal static error checkPidfd() {
    GoFrame ᒐ = default;
    try {
        // Get a pidfd of the current process (opening of "/proc/self" won't
        // work for waitid).
        var (fd, err) = unix.PidFDOpen(syscall.Getpid(), 0);
        if (err != default!) {
            return NewSyscallError(pidfdOpenˢ, err);
        }
        defer(syscall.Close, (nint)fd, ref ᒐ);
        // Check waitid(P_PIDFD) works.
        while (ᐧ) {
            var (ᴛ1, ᴛ2, ᴛ3) = syscall.Syscall6(syscall.SYS_WAITID, _P_PIDFD, fd, 0, syscall.WEXITED, 0, 0);
            (_, _, err) = (ᴛ1, ᴛ2, ᴛ3);
            if (!AreEqual(err, syscall.EINTR)) {
                break;
            }
        }
        // Expect ECHILD from waitid since we're not our own parent.
        if (!AreEqual(err, syscall.ECHILD)) {
            return NewSyscallError(pidfdWaitˢ, err);
        }
        // Check pidfd_send_signal works (should be able to send 0 to itself).
        {
            var errΔ1 = unix.PidFDSendSignal(fd, 0); if (errΔ1 != default!) {
                return NewSyscallError(pidfdSendSignalˢ, errΔ1);
            }
        }
        return default!;
    }
    catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); return default!; }
    finally { ᒐ.Run(); }
}

} // end os_package
