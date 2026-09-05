// DarwinSigactionContractTests.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GolibTests;

/// <summary>
/// Pins the four POSIX contracts the DARWIN <c>runtime.sigaction</c> body
/// (runtime/darwin/sigaction_impl.cs, darwin run-layer increment 6) is written against, measured
/// on the fleet's own hardware against glibc — the arrangement <c>DarwinSigmaskContractTests</c>
/// and <c>LibcCallDispatchTests</c> already use to drive darwin seams on Linux.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this shape and not a test of the body.</b> The body compiles only under
/// <c>$(GoTargetOS)=darwin</c>, and nothing on the fleet or in CI runs a darwin GolibTests
/// assembly, so a darwin-conditional guard here would execute nowhere — an unexercisable branch,
/// a false-green seed. The body's own guard is the hosted <c>behavioral-stderr</c> row
/// <c>SignalPrimitives</c> on osx-arm64, whose negative arm is the train-26 crash report it was cut
/// against (exit 138, zero stderr, the CLR's stack walk dereferencing a sigaction read-back).
/// </para>
/// <para>
/// <b>What a green here proves.</b> That <c>sigaction(2)</c> reports failure as <c>-1</c> with
/// errno SET (the increment-5 sibling, <c>pthread_sigmask</c>, RETURNS its errno — a body borrowing
/// that clause would read a stale value here); that a NULL <c>act</c> is a pure query, reporting the
/// current action and changing nothing; that <c>act</c> and <c>oact</c> may be two non-overlapping
/// regions of ONE allocation, which is how the body passes them; and that an installed action
/// reads back as installed — the handler word at offset 0 — which is what the body's <c>new</c>
/// arm relies on and what its <c>old</c> arm decodes.
/// </para>
/// <para>
/// <b>What it does not prove</b>, stated so a green is not over-read: darwin's user-visible
/// <c>struct sigaction</c> is 16 bytes <c>{ handler (8) | sa_mask (4) | sa_flags (4) }</c> on amd64
/// and arm64 alike, where glibc's is 152 bytes with a 128-byte mask; the WIDTH and the field
/// OFFSETS the darwin body encodes are read from the pinned go1.23.12 <c>defs_darwin_*.go</c> in
/// the body's own header, and stay a mac dispatch's to confirm. Only the handler word at offset 0
/// is common to both layouts, and it is the only field asserted by value here.
/// </para>
/// <para>
/// Linux-only by construction (glibc's layout and signal numbers), compile-removed under any
/// other <c>$(GoTargetOS)</c> exactly as <c>DarwinSigmaskContractTests</c> is.
/// </para>
/// </remarks>
[TestClass]
public class DarwinSigactionContractTests
{
    private const int SIGUSR1 = 10;         // linux/amd64; neither the CLR nor the host uses it
    private const int EINVAL = 22;
    private const long SIG_IGN = 1;

    // glibc's struct sigaction on x86-64 is 152 bytes (sa_handler 8, sa_mask 128, sa_flags 4, pad 4,
    // sa_restorer 8); each region is 256 bytes so the image fits with zeroed headroom, and two
    // regions live in ONE allocation exactly as the darwin body arranges its two 16-byte images.
    private const int RegionBytes = 256;

    // Declared WITH SetLastError, exactly as the darwin body declares it: sigaction returns -1
    // and sets errno, which is the clause that differs from pthread_sigmask.
    [DllImport("libc", EntryPoint = "sigaction", SetLastError = true)]
    private static extern int sigaction(int sig, nint act, nint oact);

    private static void Zero(nint buffer, int offset)
    {
        for (int i = 0; i < RegionBytes; i += 8)
            Marshal.WriteInt64(buffer, offset + i, 0L);
    }

    private static byte[] Image(nint buffer, int offset)
    {
        byte[] image = new byte[RegionBytes];
        Marshal.Copy(buffer + offset, image, 0, RegionBytes);
        return image;
    }

    // MEASURED on the fleet's glibc by this guard's first run: two consecutive queries of SIGUSR1
    // differed from byte 16 onward. glibc defines only the kernel-visible fields of a read-back --
    // the handler word, the kernel's eight mask bytes, sa_flags and sa_restorer -- and leaves the
    // rest of the 128-byte user mask as whatever its stack held, so whole-image equality is not a
    // contract glibc offers. Normalize keeps the defined fields and zeroes the rest; every
    // comparison below is over defined bytes only. (darwin's 16-byte user struct has no undefined
    // region: its mask IS the kernel's four bytes, which is what the body decodes.)
    private static byte[] Normalize(byte[] image)
    {
        byte[] defined = new byte[RegionBytes];
        Array.Copy(image, 0, defined, 0, 16);       // sa_handler (8) + the kernel's sa_mask (8)
        Array.Copy(image, 136, defined, 136, 4);    // sa_flags
        Array.Copy(image, 144, defined, 144, 8);    // sa_restorer
        return defined;
    }

    // The handler word and the kernel's mask -- the two fields an installed action keeps verbatim,
    // and the two the darwin body encodes and decodes (sa_flags is the third, but glibc edits it on
    // the way in with SA_RESTORER, see contract 3).
    private static byte[] HandlerAndMask(byte[] image)
    {
        byte[] head = new byte[16];
        Array.Copy(image, 0, head, 0, 16);
        return head;
    }

    private static byte[] Query(int sig)
    {
        nint buffer = Marshal.AllocHGlobal(RegionBytes);

        try
        {
            Zero(buffer, 0);
            Assert.AreEqual(0, sigaction(sig, 0, buffer), "a pure query must succeed");
            return Normalize(Image(buffer, 0));
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static void Install(int sig, byte[] image)
    {
        nint buffer = Marshal.AllocHGlobal(RegionBytes);

        try
        {
            Marshal.Copy(image, 0, buffer, RegionBytes);
            Assert.AreEqual(0, sigaction(sig, buffer, 0), "restoring an action must succeed");
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    /// <summary>
    /// Contract 1 — failure is <c>-1</c> with errno SET. A body reading the return value as the
    /// errno (the increment-5 clause, correct for <c>pthread_sigmask</c>) would report -1 as the
    /// reason here, which is why the darwin body declares SetLastError and reads
    /// <c>Marshal.GetLastPInvokeError()</c>.
    /// </summary>
    /// <remarks>
    /// Both halves are asserted, following the increment-5 lesson that a NULL-argument call can
    /// be a no-op rather than an error: for sigaction the signal number is validated whatever the
    /// pointers are, so the NULL/NULL query of an invalid signal fails too, and the body's
    /// nil-box arms cannot mask a bad signal number.
    /// </remarks>
    [TestMethod]
    public void FailureIsMinusOneWithErrnoSet()
    {
        int rc = sigaction(12345, 0, 0);
        int errno = Marshal.GetLastPInvokeError();

        Assert.AreEqual(-1, rc, "an invalid signal number must fail even with nothing to apply or report");
        Assert.AreEqual(EINVAL, errno, "and the reason must be in errno, not in the return value");

        nint buffer = Marshal.AllocHGlobal(RegionBytes);

        try
        {
            Zero(buffer, 0);
            rc = sigaction(12345, 0, buffer);
            errno = Marshal.GetLastPInvokeError();

            Assert.AreEqual(-1, rc, "an invalid signal number must fail on a query too");
            Assert.AreEqual(EINVAL, errno, "with the same errno");
            CollectionAssert.AreEqual(new byte[RegionBytes], Image(buffer, 0), "and a rejected query must not have written the old-action region");
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    /// <summary>
    /// Contract 2 — a NULL <c>act</c> is a pure query: the current action is reported and left
    /// alone. This is what the darwin body's nil-<c>new</c> arm (getsig) relies on.
    /// </summary>
    [TestMethod]
    public void NullActIsAPureQuery()
    {
        byte[] first = Query(SIGUSR1);
        byte[] second = Query(SIGUSR1);

        CollectionAssert.AreEqual(first, second, "two queries in a row must report the same defined fields — querying did not write");
    }

    /// <summary>
    /// Contract 3 — <c>act</c> and <c>oact</c> may be two non-overlapping regions of ONE
    /// allocation. The darwin body allocates 32 bytes and passes offsets 0 and 16; this pins the
    /// shape (at glibc's width) rather than the width: reinstalling the queried action through
    /// region A while reporting into region B must succeed, report the action just queried, and
    /// leave the handler and mask as they were.
    /// </summary>
    [TestMethod]
    public void ActAndOactMayBeOneAllocationTwoRegions()
    {
        byte[] before = Query(SIGUSR1);
        nint buffer = Marshal.AllocHGlobal(RegionBytes * 2);

        try
        {
            Marshal.Copy(before, 0, buffer, RegionBytes);
            Zero(buffer, RegionBytes);

            int rc = sigaction(SIGUSR1, buffer, buffer + RegionBytes);
            Assert.AreEqual(0, rc, "set-and-report through one allocation must succeed");

            CollectionAssert.AreEqual(before, Normalize(Image(buffer, RegionBytes)), "the old-action region must carry the action held BEFORE the call");
            // MEASURED (ctypes and this guard's second run agree): reinstalling a queried SIG_DFL action
            // through glibc reads back with SA_RESTORER (0x04000000) added to sa_flags and glibc's own
            // restorer address filled in -- the C library's business, not the caller's -- so the
            // "unchanged" half of this contract is the handler word and the mask, the two fields the
            // darwin body encodes, never the flags word or the restorer.
            CollectionAssert.AreEqual(HandlerAndMask(before), HandlerAndMask(Query(SIGUSR1)), "and reinstalling the queried action must leave its handler and mask unchanged");
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
            Install(SIGUSR1, before);
        }
    }

    /// <summary>
    /// Contract 4 — an installed action reads back as installed: the handler word at offset 0,
    /// the one field common to darwin's 16-byte layout and glibc's 152-byte one. The darwin
    /// body's <c>new</c> arm encodes that word from <c>__sigaction_u</c> and its <c>old</c> arm
    /// decodes it back; <c>SIG_IGN</c> is what <c>signal.Ignore</c> installs, the row's second
    /// statement.
    /// </summary>
    [TestMethod]
    public void AnInstalledActionReadsBackAsInstalled()
    {
        byte[] before = Query(SIGUSR1);
        long originalHandler = BitConverter.ToInt64(before, 0);

        nint buffer = Marshal.AllocHGlobal(RegionBytes);

        try
        {
            Zero(buffer, 0);
            Marshal.WriteInt64(buffer, 0, SIG_IGN);

            Assert.AreEqual(0, sigaction(SIGUSR1, buffer, 0), "installing SIG_IGN must succeed");

            byte[] after = Query(SIGUSR1);
            Assert.AreEqual(SIG_IGN, BitConverter.ToInt64(after, 0), "the handler word must read back as the SIG_IGN just installed");

            Install(SIGUSR1, before);
            Assert.AreEqual(originalHandler, BitConverter.ToInt64(Query(SIGUSR1), 0), "and restoring the original image must read back as the original handler");
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
            Install(SIGUSR1, before);
        }
    }
}
