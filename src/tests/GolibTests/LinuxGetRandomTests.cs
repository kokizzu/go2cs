using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using static go.builtin;
using rand = go.crypto.rand_package;
using unix = go.@internal.syscall.unix_package;

namespace GolibTests;

[TestClass]
public class LinuxGetRandomTests
{
    // Go 1.24 routes internal/syscall/unix.GetRandom through runtime's vDSO getrandom first:
    // getrandom.go:15 pulls `//go:linkname vgetrandom runtime.vgetrandom` over a bodyless
    // declaration, and only when the runtime answers (_, false) does it fall back to the getrandom
    // SYSCALL. The converter emitted that pull as a bodyless partial until runtime.vgetrandom joined
    // linknameForwardTargets, so PartialStubGenerator filled it with a throwing stub, and on the linux
    // flavour every crypto/rand read died with `vgetrandom: no implementation reached this
    // compilation` — 25 rows of the go1.24.13 Linux leg, crypto/x509 and hash/maphash at static init.
    //
    // The forwarder calls the CONVERTED runtime.vgetrandom, whose answer is Go's own no-vDSO answer:
    // vgetrandom_linux.go:93-95 returns (-1, false) while vgetrandomAlloc.stateSize is 0, and nothing
    // in the converted runtime runs osinit's vgetrandomInit, the only writer of stateSize. So
    // GetRandom takes the syscall, exactly as Go does on a host without vDSO getrandom.
    //
    // Linux-only by construction: getrandom.go's build tags select the linux flavour, and on any
    // other $(GoTargetOS) the csproj removes this file. On a non-linux host it reports Inconclusive.

    private static void RequireLinux()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            Assert.Inconclusive("GetRandom's vgetrandom pull is the linux flavour's declaration");
    }

    // The PREMISE, read by reflection so this arm compiles on either side of the seat (the method is
    // internal until the forward row widens it): the runtime body the forwarder reaches answers
    // "unsupported". It holds before the fix too; the fix is that GetRandom now REACHES it.
    [TestMethod]
    public void RuntimeVgetrandomAnswersUnsupported()
    {
        RequireLinux();

        MethodInfo? method = typeof(go.runtime_package).GetMethod("vgetrandom", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.IsNotNull(method, "runtime.vgetrandom must exist in the linux flavour of the converted runtime");

        var (ret, supported) = ((nint, bool))method!.Invoke(null, [new slice<byte>(16), (uint32)0])!;

        Assert.AreEqual((nint)(-1), ret, "no vDSO state exists, so vgetrandom_linux.go:93-95 answers ret -1");
        Assert.IsFalse(supported, "no vDSO state exists, so the answer is 'unsupported' and GetRandom must take the syscall");
    }

    // THE GATE: the stub threw NotImplementedException out of this call before the forward row.
    [TestMethod]
    public void GetRandomFillsTheBufferThroughTheSyscallFallback()
    {
        RequireLinux();

        var buffer = new slice<byte>(64);
        var (n, err) = unix.GetRandom(buffer, default);

        Assert.IsNull(err, $"GetRandom must succeed, got: {err?.Error().ToString()}");
        Assert.AreEqual((nint)64, n, "GetRandom must fill the whole buffer");

        // 64 random bytes all zero has probability 2^-512; a zero buffer means nothing was written.
        Assert.IsTrue(Enumerable.Range(0, 64).Any(i => buffer[i] != 0), "GetRandom returned success but wrote nothing");
    }

    // What a user reaches: crypto/rand.Read, the call every one of the 25 rows died under.
    [TestMethod]
    public void CryptoRandReadWorks()
    {
        RequireLinux();

        var first = new slice<byte>(32);
        var second = new slice<byte>(32);

        var (n1, err1) = rand.Read(first);
        var (n2, err2) = rand.Read(second);

        Assert.IsNull(err1, $"crypto/rand.Read must succeed, got: {err1?.Error().ToString()}");
        Assert.IsNull(err2, $"crypto/rand.Read must succeed, got: {err2?.Error().ToString()}");
        Assert.AreEqual((nint)32, n1);
        Assert.AreEqual((nint)32, n2);

        // Two independent 32-byte reads are equal with probability 2^-256.
        Assert.IsFalse(Enumerable.Range(0, 32).All(i => first[i] == second[i]), "two crypto/rand reads returned identical bytes");
    }
}
