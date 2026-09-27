using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static go.runtime_package;

namespace GolibTests;

/// <summary>
/// The linux runtime's raw file-descriptor syscalls (runtime/linux/stubs2_impl.cs): read, write1, open,
/// closefd, pipe2 and mincore answer with Go's own conventions -- open and closefd -1 on failure, the
/// others the negative errno. Each arm is the sequence of a runtime test that reached a throwing stub on
/// the runtime row (TestBadOpen, TestNonblockingPipe, TestMincoreErrorSign). Linux only: the primitives
/// are bodied in the linux flavour.
/// </summary>
[TestClass]
public class RuntimeFdSyscallTests
{
    private const int EBADF = 9;
    private const int EINVAL = 22;

    private static bool OnLinux => OperatingSystem.IsLinux();

    [TestMethod]
    public void ABadOpenAndIoOnFdMinusOneAnswerGosConventions()
    {
        if (!OnLinux) Assert.Inconclusive("the primitives are the linux flavour's");
        (int open, int read, int write, int close, string? failure) = GoBadOpenProbe();
        Assert.IsNull(failure, $"a primitive failed: {failure}");
        Assert.AreEqual(-1, open, "open of a missing path answers -1");
        Assert.AreEqual(-EBADF, read, "read on fd -1 answers -EBADF");
        Assert.AreEqual(-EBADF, write, "write1 on fd ^0 answers -EBADF");
        Assert.AreEqual(-1, close, "closefd(-1) answers -1");
    }

    [TestMethod]
    public void APipeCarriesAByteAndClosesClean()
    {
        if (!OnLinux) Assert.Inconclusive("the primitives are the linux flavour's");
        (int errno, int wrote, int readCount, byte readByte, int closeR, int closeW, string? failure) = GoPipe2RoundTripProbe();
        Assert.IsNull(failure, $"a primitive failed: {failure}");
        Assert.AreEqual(0, errno, "pipe2 answered");
        Assert.AreEqual(1, wrote, "write1 wrote the byte");
        Assert.AreEqual(1, readCount, "read read it");
        Assert.AreEqual((byte)42, readByte, "the byte that went in came out");
        Assert.AreEqual(0, closeR, "closefd(reader) answers 0");
        Assert.AreEqual(0, closeW, "closefd(writer) answers 0");
    }

    [TestMethod]
    public void MincoreOnAMisalignedAddressAnswersNegativeEinval()
    {
        if (!OnLinux) Assert.Inconclusive("the primitives are the linux flavour's");
        (int result, string? failure) = GoMincoreErrorSignProbe();
        Assert.IsNull(failure, $"mincore failed: {failure}");
        Assert.AreEqual(-EINVAL, result, "the sign is the test: -EINVAL, not EINVAL");
    }

    // A real write1 reaches netpollBreak's write to netpollEventFd, which the managed host never
    // creates (netpollGenericInit is a no-op, netpoll_impl.cs): -EBADF, then Go's throw, which ends
    // the process. netpollBreak refuses by name instead, before the write.
    [TestMethod]
    public void NetpollBreakRefusesByNameBeforeItsWrite()
    {
        if (!OnLinux) Assert.Inconclusive("the primitives are the linux flavour's");
        string? failure = GoNetpollBreakProbe(30000);
        Assert.IsNotNull(failure, "netpollBreak returned: the managed host has no poller to break");
        StringAssert.StartsWith(failure, "PanicException: runtime: netpollBreak:", failure);
    }
}
