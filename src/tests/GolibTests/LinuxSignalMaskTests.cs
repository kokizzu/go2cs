using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GolibTests;

/// <summary>
/// Guards the linux flavour's <c>runtime.rtsigprocmask</c> body (sigprocmask_impl.cs): the mask the
/// converted seam reads, blocks and unblocks is the KERNEL's mask for the calling thread, checked
/// against libc's own <c>pthread_sigmask</c> read so a body that faked a mask without touching the
/// kernel cannot pass. The generated stub throws, so every arm goes RED against it (the negative
/// control the cut was measured with). Linux-only by construction: the declaration lives under
/// runtime/linux, and the file is compile-removed on any other GoTargetOS; on another host it
/// reports Inconclusive rather than a vacuous green.
/// </summary>
[TestClass]
public class LinuxSignalMaskTests
{
    private const int SIG_BLOCK = 0;    // Go's _SIG_BLOCK, the kernel's numbering
    private const int SIG_UNBLOCK = 1;
    private const int SIG_SETMASK = 2;
    private const int SIGUSR1 = 10;     // linux/amd64; a signal neither the CLR nor the test host uses for itself

    // The INDEPENDENT instrument: glibc's own read of the calling thread's mask (a 128-byte
    // sigset_t whose first 8 bytes are the kernel's set), never the seam under test.
    [DllImport("libc", SetLastError = true)]
    private static extern int pthread_sigmask(int how, IntPtr set, IntPtr oldset);

    private static ulong KernelMask()
    {
        IntPtr buffer = Marshal.AllocHGlobal(128);
        try
        {
            for (int i = 0; i < 128; i += 8)
                Marshal.WriteInt64(buffer, i, 0L);

            int rc = pthread_sigmask(SIG_BLOCK, IntPtr.Zero, buffer);
            Assert.AreEqual(0, rc, "pthread_sigmask read must succeed");

            return unchecked((ulong)Marshal.ReadInt64(buffer, 0));
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static bool NotLinux()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return false;

        Assert.Inconclusive("rtsigprocmask is the linux flavour's declaration");
        return true;
    }

    [TestMethod]
    public void ReadsTheCallingThreadsMaskAsTheKernelHoldsIt()
    {
        if (NotLinux())
            return;

        // _SIG_SETMASK with a nil new set is Go's pure read (Sigisblocked's shape).
        ulong seam = go.runtime_package.GoSigprocmask(SIG_SETMASK, null);
        ulong kernel = KernelMask();

        Assert.AreEqual(kernel, seam, $"the seam's mask 0x{seam:x16} must be the kernel's 0x{kernel:x16}");
    }

    [TestMethod]
    public void BlocksAndUnblocksASignalAndTheKernelAgrees()
    {
        if (NotLinux())
            return;

        ulong bit = 1UL << (SIGUSR1 - 1);
        ulong original = go.runtime_package.GoSigprocmask(SIG_SETMASK, null);
        Assert.AreEqual(0UL, original & bit, "SIGUSR1 must start unblocked on the test thread, or the arms below cannot discriminate");

        try
        {
            // BLOCK returns the mask BEFORE the change, as Go's `old` reports it.
            ulong before = go.runtime_package.GoSigprocmask(SIG_BLOCK, bit);
            Assert.AreEqual(original, before, "the old-mask out-parameter must report the mask before the block");

            Assert.AreNotEqual(0UL, go.runtime_package.GoSigprocmask(SIG_SETMASK, null) & bit, "the seam must read SIGUSR1 back as blocked");
            Assert.AreNotEqual(0UL, KernelMask() & bit, "the KERNEL must hold SIGUSR1 blocked -- a mask faked in managed memory fails here");

            ulong beforeUnblock = go.runtime_package.GoSigprocmask(SIG_UNBLOCK, bit);
            Assert.AreNotEqual(0UL, beforeUnblock & bit, "the old mask reported by UNBLOCK must still carry the bit");

            Assert.AreEqual(0UL, go.runtime_package.GoSigprocmask(SIG_SETMASK, null) & bit, "the seam must read SIGUSR1 back as unblocked");
            Assert.AreEqual(0UL, KernelMask() & bit, "the KERNEL must hold SIGUSR1 unblocked again");
        }
        finally
        {
            go.runtime_package.GoSigprocmask(SIG_SETMASK, original);
        }
    }

    [TestMethod]
    public void SetmaskReplacesTheWholeMaskAndRestoresIt()
    {
        if (NotLinux())
            return;

        ulong original = go.runtime_package.GoSigprocmask(SIG_SETMASK, null);
        ulong usr1 = 1UL << (SIGUSR1 - 1);
        ulong usr2 = 1UL << (12 - 1);   // SIGUSR2

        try
        {
            go.runtime_package.GoSigprocmask(SIG_SETMASK, original | usr1 | usr2);
            ulong kernel = KernelMask();
            Assert.AreEqual(original | usr1 | usr2, kernel, "SETMASK must replace the whole mask as the kernel sees it");
        }
        finally
        {
            go.runtime_package.GoSigprocmask(SIG_SETMASK, original);
        }

        Assert.AreEqual(original, KernelMask(), "the restore must leave the kernel's mask exactly as it was");
    }
}
