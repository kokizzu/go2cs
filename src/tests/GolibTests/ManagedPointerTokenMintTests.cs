using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

[TestClass]
public class ManagedPointerTokenMintTests
{
    // ManagedPointerTokens.MintOpaque is the converter's emission for `T(unsafe.Pointer(p))` where
    // T's underlying type is `*struct{}` (syscall.Pointer): an OPAQUE pointer field handed to a
    // syscall boundary. For a pointee free of managed references the numeric route already works —
    // the box pins and reports a stable address — but a REFERENCE-BEARING pointee has no address to
    // give, and the old emission handed the kernel a transient GC-heap address (crypto/x509's
    // SSL_EXTRA_CERT_CHAIN_POLICY_PARA, measured as an ACCESS_VIOLATION inside
    // CertVerifyCertificateChainPolicy). The invariants below are not expressible as Go-parity
    // behavioral tests — they are the golib-level contract between the mint and the boundary
    // wrapper that resolves it — so they are guarded here.

    private struct ReferenceBearing
    {
        internal ж<ushort> Name;
        internal uint Auth;
    }

    private struct ReferenceFree
    {
        internal uint A;
        internal uint B;
    }

    [TestMethod]
    public void ReferenceBearingPointeeMintsItsOrderTokenAndResolvesToItsBox()
    {
        ref ReferenceBearing value = ref heap(new ReferenceBearing { Auth = 2 }, out ж<ReferenceBearing> Ꮡvalue);
        value.Name = Ꮡ((ushort)65);

        ж<EmptyStruct> minted = ManagedPointerTokens.MintOpaque(Ꮡvalue);

        // The scalar the boundary wrapper receives is the box's own order token — the value
        // Resolve verifies against — not a heap address.
        Assert.AreEqual(Ꮡvalue.PointerOrderToken, (nuint)(uintptr)minted);

        // And the wrapper's recovery: the token resolves to the very box that was minted, so the
        // wrapper reads the pointee's fields from the original storage.
        Assert.AreSame(Ꮡvalue, ManagedPointerTokens.Resolve((nuint)(uintptr)minted));
        GC.KeepAlive(value);
    }

    [TestMethod]
    public void ReferenceFreePointeeKeepsTheNumericRouteByteForByte()
    {
        ref ReferenceFree value = ref heap(new ReferenceFree { A = 1, B = 2 }, out ж<ReferenceFree> Ꮡvalue);

        ж<EmptyStruct> minted = ManagedPointerTokens.MintOpaque(Ꮡvalue);

        // A blittable pointee pins and reports stable storage — the address the kernel can really
        // read — exactly as the ж→uintptr operator answers on its own.
        Assert.AreEqual((nuint)(uintptr)Ꮡvalue, (nuint)(uintptr)minted);
        GC.KeepAlive(value);
    }

    [TestMethod]
    public void NativeBoxAndNilKeepTheirAddresses()
    {
        ж<ReferenceBearing> native = (ж<ReferenceBearing>)(uintptr)(nuint)0x2000;

        Assert.AreEqual((nuint)0x2000, (nuint)(uintptr)ManagedPointerTokens.MintOpaque(native));
        Assert.AreEqual((nuint)0, (nuint)(uintptr)ManagedPointerTokens.MintOpaque<ReferenceBearing>(null));
        Assert.AreEqual((nuint)0, (nuint)(uintptr)ManagedPointerTokens.MintOpaque(new StandardBox<ReferenceBearing>(nil)));
    }

    [TestMethod]
    public void MintedPointerKeepsItsReferentAliveAcrossACollection()
    {
        // The emitted mint's referent is otherwise reachable only through a local the JIT may
        // retire before the syscall that consumes the token — the boundary wrapper resolves
        // MID-CALL, so the mint itself must hold the referent, exactly as the Go pointer it
        // stands for would.
        (ж<EmptyStruct> minted, nuint token) = mintWithoutKeepingTheReferent();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        object? resolved = ManagedPointerTokens.Resolve(token);

        Assert.IsNotNull(resolved);
        Assert.IsInstanceOfType(resolved, typeof(ж<ReferenceBearing>));
        GC.KeepAlive(minted);
    }

    private static (ж<EmptyStruct>, nuint) mintWithoutKeepingTheReferent()
    {
        ж<ReferenceBearing> Ꮡvalue = Ꮡ(new ReferenceBearing { Auth = 2 });

        ж<EmptyStruct> minted = ManagedPointerTokens.MintOpaque(Ꮡvalue);

        return (minted, (nuint)(uintptr)minted);
    }
}
