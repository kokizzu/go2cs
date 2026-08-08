// Copyright 2011 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.@internal;

using Δsyscall = syscall_package;

partial class poll_package {

// maxSendfileSize is the largest chunk size we ask the kernel to copy
// at a time.
internal const nint maxSendfileSize = /* 4 << 20 */ 4194304;

// SendFile wraps the sendfile system call.
public static (int64 written, error err, bool handled) SendFile(ж<FD> ᏑdstFD, nint src, int64 remain) {
    int64 written = default!;
    error err = default!;
    bool handled = default!;
    GoFrame ᒐ = default;
    try {
        ref var dstFD = ref ᏑdstFD.DerefOrNull();

        defer(() => {
            TestHookDidSendFile(ᏑdstFD, src, written, err, handled);
        }, ref ᒐ);
        {
            var errΔ1 = ᏑdstFD.writeLock(); if (errΔ1 != default!) {
                (written, err, handled) = (0, errΔ1, false); goto ᒐdone;
            }
        }
        defer(ᏑdstFD.writeUnlock, ref ᒐ);
        {
            var errΔ2 = dstFD.pd.prepareWrite(dstFD.isFile); if (errΔ2 != default!) {
                (written, err, handled) = (0, errΔ2, false); goto ᒐdone;
            }
        }
        nint dst = dstFD.Sysfd;
        while (remain > 0) {
            nint n = maxSendfileSize;
            if ((int64)n > remain) {
                n = (nint)remain;
            }
            (n, err) = Δsyscall.Sendfile(dst, src, nil, n);
            if (n > 0){
                written += (int64)n;
                remain -= (int64)n;
                continue;
            } else 
            if (!AreEqual(err, Δsyscall.EAGAIN) && !AreEqual(err, Δsyscall.EINTR)) {
                // This includes syscall.ENOSYS (no kernel
                // support) and syscall.EINVAL (fd types which
                // don't implement sendfile), and other errors.
                // We should end the loop when there is no error
                // returned from sendfile(2) or it is not a retryable error.
                break;
            }
            if (AreEqual(err, Δsyscall.EINTR)) {
                continue;
            }
            {
                err = dstFD.pd.waitWrite(dstFD.isFile); if (err != default!) {
                    break;
                }
            }
        }
        handled = written != 0 || (!AreEqual(err, Δsyscall.ENOSYS) && !AreEqual(err, Δsyscall.EINVAL));
    }
    catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
    finally { ᒐ.Run(); }
    ᒐdone: return (written, err, handled);
}

} // end poll_package
