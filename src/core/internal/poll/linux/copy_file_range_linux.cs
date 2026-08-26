// Copyright 2020 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.@internal;

using unix = go.@internal.syscall.unix_package;
using sync = sync_package;
using Δsyscall = syscall_package;
using go.@internal.syscall;

partial class poll_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸinternalꓸsyscallꓸunix() {
    builtin.initPackage(typeof(go.@internal.syscall.unix_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸsync() {
    builtin.initPackage(typeof(sync_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸsyscall() {
    builtin.initPackage(typeof(syscall_package));
}

// copy_file_range(2) is broken in various ways on kernels older than 5.3,
// see https://go.dev/issue/42400 and
// https://man7.org/linux/man-pages/man2/copy_file_range.2.html#VERSIONS
internal static Func<bool> isKernelVersionGE53 = sync.OnceValue(bool () => {
    var (major, minor) = unix.KernelVersion();
    return major > 5 || (major == 5 && minor >= 3);
});

internal static UntypedInt maxCopyFileRangeRound => /* 1 << 30 */ 1073741824;

// CopyFileRange copies at most remain bytes of data from src to dst, using
// the copy_file_range system call. dst and src must refer to regular files.
public static (int64 written, bool handled, error err) CopyFileRange(ж<FD> Ꮡdst, ж<FD> Ꮡsrc, int64 remain) {
    int64 written = default!;

    ref var dst = ref Ꮡdst.DerefOrNull();
    ref var src = ref Ꮡsrc.DerefOrNull();
    if (!isKernelVersionGE53()) {
        return (0, false, default!);
    }
    while (remain > 0) {
        var max = remain;
        if (max > maxCopyFileRangeRound) {
            max = maxCopyFileRangeRound;
        }
        var (n, errΔ1) = copyFileRange(Ꮡdst, Ꮡsrc, (nint)max);
        var exprᴛ1 = errΔ1;
        if (AreEqual(exprᴛ1, Δsyscall.ENOSYS)) {
            return (0, false, default!);
        }
        if (AreEqual(exprᴛ1, Δsyscall.EXDEV) || AreEqual(exprᴛ1, Δsyscall.EINVAL) || AreEqual(exprᴛ1, Δsyscall.EIO) || AreEqual(exprᴛ1, Δsyscall.EOPNOTSUPP) || AreEqual(exprᴛ1, Δsyscall.EPERM)) {
            return (0, false, default!);
        }
        if (AreEqual(exprᴛ1, default!)) {
            if (n == 0) {
                // copy_file_range(2) was introduced in Linux 4.5.
                // Go supports Linux >= 2.6.33, so the system call
                // may not be present.
                //
                // If we see ENOSYS, we have certainly not transferred
                // any data, so we can tell the caller that we
                // couldn't handle the transfer and let them fall
                // back to more generic code.
                // Prior to Linux 5.3, it was not possible to
                // copy_file_range across file systems. Similarly to
                // the ENOSYS case above, if we see EXDEV, we have
                // not transferred any data, and we can let the caller
                // fall back to generic code.
                //
                // As for EINVAL, that is what we see if, for example,
                // dst or src refer to a pipe rather than a regular
                // file. This is another case where no data has been
                // transferred, so we consider it unhandled.
                //
                // If src and dst are on CIFS, we can see EIO.
                // See issue #42334.
                //
                // If the file is on NFS, we can see EOPNOTSUPP.
                // See issue #40731.
                //
                // If the process is running inside a Docker container,
                // we might see EPERM instead of ENOSYS. See issue
                // #40893. Since EPERM might also be a legitimate error,
                // don't mark copy_file_range(2) as unsupported.
                // If we did not read any bytes at all,
                // then this file may be in a file system
                // where copy_file_range silently fails.
                // https://lore.kernel.org/linux-fsdevel/20210126233840.GG4626@dread.disaster.area/T/#m05753578c7f7882f6e9ffe01f981bc223edef2b0
                if (written == 0) {
                    return (0, false, default!);
                }
                // Otherwise src is at EOF, which means
                // we are done.
                return (written, true, default!);
            }
            remain -= n;
            written += n;
        }
        else { /* default: */
            return (written, true, errΔ1);
        }

    }
    return (written, true, default!);
}

// copyFileRange performs one round of copy_file_range(2).
internal static (int64 written, error err) copyFileRange(ж<FD> Ꮡdst, ж<FD> Ꮡsrc, nint max) {
    int64 written = default!;
    error err = default!;
    GoFrame ᒐ = default;
    try {
        ref var dst = ref Ꮡdst.DerefOrNull();
        ref var src = ref Ꮡsrc.DerefOrNull();

        // The signature of copy_file_range(2) is:
        //
        // ssize_t copy_file_range(int fd_in, loff_t *off_in,
        //                         int fd_out, loff_t *off_out,
        //                         size_t len, unsigned int flags);
        //
        // Note that in the call to unix.CopyFileRange below, we use nil
        // values for off_in and off_out. For the system call, this means
        // "use and update the file offsets". That is why we must acquire
        // locks for both file descriptors (and why this whole machinery is
        // in the internal/poll package to begin with).
        {
            var errΔ1 = Ꮡdst.writeLock(); if (errΔ1 != default!) {
                (written, err) = (0, errΔ1); goto ᒐdone;
            }
        }
        defer(Ꮡdst.writeUnlock, ref ᒐ);
        {
            var errΔ2 = Ꮡsrc.readLock(); if (errΔ2 != default!) {
                (written, err) = (0, errΔ2); goto ᒐdone;
            }
        }
        defer(Ꮡsrc.readUnlock, ref ᒐ);
        nint n = default!;
        while (ᐧ) {
            (n, err) = unix.CopyFileRange(src.Sysfd, nil, dst.Sysfd, nil, max, 0);
            if (!AreEqual(err, Δsyscall.EINTR)) {
                break;
            }
        }
        (written, err) = ((int64)n, err);
    }
    catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
    finally { ᒐ.Run(); }
    ᒐdone: return (written, err);
}

} // end poll_package
