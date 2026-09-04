// DarwinSigmaskContractTests.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GolibTests;

/// <summary>
/// Pins the three POSIX contracts the DARWIN <c>runtime.sigprocmask</c> body
/// (runtime/darwin/sigprocmask_impl.cs, darwin run-layer increment 5) is written against, measured
/// on the fleet's own hardware against glibc — the same arrangement
/// <c>LibcCallDispatchTests</c> uses to drive the darwin keystone's dispatch on Linux.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this shape and not a test of the body.</b> The body compiles only under
/// <c>$(GoTargetOS)=darwin</c>, and NOTHING on the fleet or in CI runs a darwin assembly —
/// GolibTests is not referenced by any workflow, so a darwin-conditional guard here would be
/// compiled out on every host that runs it and would never execute anywhere. That is an
/// unexercisable branch, which is a false-green seed rather than a guard. The body's own guard is
/// the hosted <c>behavioral-full</c> census row <c>SignalPrimitives</c>, whose negative arm is the
/// train-24 measurement it was cut against (osx-x64 `panic: FuncPCABI0 …
/// runtime.sigprocmask_trampoline`, osx-arm64 mute exit 138, both after 2 of main.go's 6 lines).
/// </para>
/// <para>
/// <b>What a green here proves.</b> The three contracts the body leans on, each a clause where the
/// Linux arm's body (runtime/linux/sigprocmask_impl.cs, C1's runtime increment 1) would have been
/// wrong if borrowed: that <c>pthread_sigmask</c> reports failure through its RETURN value and not
/// through errno; that a NULL new-set is a pure read; and that the new-set and old-set pointers may
/// be two non-overlapping regions of ONE allocation, which is how the darwin body passes them.
/// </para>
/// <para>
/// <b>What it does not prove</b>, stated so a green is not over-read: darwin's set is 32 bits
/// (<c>type sigset uint32</c>) where glibc's is 128 bytes, and darwin's <c>how</c> numbering is
/// 1/2/3 where Linux's is 0/1/2. Neither width nor numbering can be checked here; both are read
/// from the pinned go1.23.12 source in the body's own header, and both stay a mac dispatch's to
/// confirm.
/// </para>
/// <para>
/// Linux-only by construction (glibc's sizes and signal numbers), compile-removed under any other
/// <c>$(GoTargetOS)</c> exactly as <c>LibcCallDispatchTests</c> is.
/// </para>
/// </remarks>
[TestClass]
public class DarwinSigmaskContractTests
{
    private const int SIG_BLOCK = 0;        // LINUX numbering — this file drives glibc, not darwin
    private const int SIG_SETMASK = 2;
    private const int SIGUSR1 = 10;         // linux/amd64; neither the CLR nor the host uses it
    private const int EINVAL = 22;
    private const int SigsetBytes = 128;    // glibc's sigset_t; darwin's is 4

    // Declared WITHOUT SetLastError, exactly as the darwin body declares it: pthread_sigmask
    // returns its error number and does not set errno, so there is no errno to marshal back.
    [DllImport("libc", EntryPoint = "pthread_sigmask")]
    private static extern int pthread_sigmask(int how, nint set, nint oset);

    private static void Zero(nint buffer, int offset)
    {
        for (int i = 0; i < SigsetBytes; i += 8)
            Marshal.WriteInt64(buffer, offset + i, 0L);
    }

    private static ulong ReadMask()
    {
        nint buffer = Marshal.AllocHGlobal(SigsetBytes);

        try
        {
            Zero(buffer, 0);
            Assert.AreEqual(0, pthread_sigmask(SIG_BLOCK, 0, buffer), "a pure read must succeed");
            return unchecked((ulong)Marshal.ReadInt64(buffer, 0));
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static void WriteMask(ulong mask)
    {
        nint buffer = Marshal.AllocHGlobal(SigsetBytes);

        try
        {
            Zero(buffer, 0);
            Marshal.WriteInt64(buffer, 0, unchecked((long)mask));
            Assert.AreEqual(0, pthread_sigmask(SIG_SETMASK, buffer, 0), "restoring the mask must succeed");
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    /// <summary>
    /// Contract 1 — the failure is the RETURN value. A body reading errno instead (the shape the
    /// linux arm's raw <c>syscall(2)</c> form correctly uses) would report a stale, unrelated
    /// number here, which is why the darwin body reads <c>rc</c> and declares no SetLastError.
    /// </summary>
    /// <remarks>
    /// The bogus <c>how</c> must be paired with a NON-NULL set, and that is a measurement rather
    /// than a style choice: this guard's first form passed <c>(12345, NULL, NULL)</c> and glibc
    /// answered <b>0</b> — with nothing to apply, <c>how</c> is never inspected. POSIX only gives
    /// <c>how</c> meaning when <c>set</c> is non-NULL, so the NULL-set call is a no-op rather than
    /// an error, and a guard written the first way could never reach the failure path it names.
    /// Both halves are asserted below, because the no-op half is exactly what the body's nil-box
    /// arm relies on.
    /// </remarks>
    [TestMethod]
    public void FailureIsReportedThroughTheReturnValue()
    {
        Assert.AreEqual(0, pthread_sigmask(12345, 0, 0),
            "with nothing to apply, 'how' is not inspected — the NULL-set call is a no-op");

        ulong before = ReadMask();
        nint buffer = Marshal.AllocHGlobal(SigsetBytes);

        try
        {
            Zero(buffer, 0);
            int rc = pthread_sigmask(12345, buffer, 0);

            Assert.AreNotEqual(0, rc, "an invalid 'how' WITH a set to apply must fail");
            Assert.AreEqual(EINVAL, rc, "and the errno must be the RETURN value, not something errno holds");
            Assert.AreEqual(before, ReadMask(), "a rejected call must not have changed the mask");
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    /// <summary>
    /// Contract 2 — a NULL new-set is a pure read: the mask is reported and left alone. This is what
    /// the darwin body's nil-box arm relies on (`hasNew == false` passes a null pointer, as in Go).
    /// </summary>
    [TestMethod]
    public void NullNewSetIsAPureRead()
    {
        ulong before = ReadMask();

        try
        {
            WriteMask(before | (1UL << (SIGUSR1 - 1)));

            ulong blocked = ReadMask();
            Assert.AreNotEqual(before, blocked, "the control must actually change the mask");

            // Two pure reads in a row: the second must agree with the first, i.e. reading did not write.
            Assert.AreEqual(blocked, ReadMask(), "a NULL new-set must not modify the mask");
        }
        finally
        {
            WriteMask(before);
        }
    }

    /// <summary>
    /// Contract 3 — the new-set and old-set pointers may be two non-overlapping regions of ONE
    /// allocation. The darwin body allocates 8 bytes and passes offsets 0 and 4; this pins the shape
    /// (at glibc's width) rather than the width.
    /// </summary>
    [TestMethod]
    public void NewAndOldMayBeOneAllocationTwoRegions()
    {
        ulong before = ReadMask();
        nint buffer = Marshal.AllocHGlobal(SigsetBytes * 2);

        try
        {
            Zero(buffer, 0);
            Zero(buffer, SigsetBytes);
            Marshal.WriteInt64(buffer, 0, unchecked((long)(before | (1UL << (SIGUSR1 - 1)))));

            int rc = pthread_sigmask(SIG_SETMASK, buffer, buffer + SigsetBytes);
            Assert.AreEqual(0, rc, "set-and-report through one allocation must succeed");

            ulong reported = unchecked((ulong)Marshal.ReadInt64(buffer, SigsetBytes));
            Assert.AreEqual(before, reported, "the old-set region must carry the mask held BEFORE the call");
            Assert.AreEqual(before | (1UL << (SIGUSR1 - 1)), ReadMask(), "and the new set must have been applied");
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
            WriteMask(before);
        }
    }
}
