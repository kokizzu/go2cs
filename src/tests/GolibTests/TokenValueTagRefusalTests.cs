using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

// THE TOKEN DOOR'S GUARD -- DESIGN-token-value-tag-refusal.md, outcome B.
//
// A reference-bearing pointee has no address to give, so `(uintptr)` over its box answers an order
// TOKEN. Handing that number to native code writes the kernel's output somewhere that is not the
// caller's struct, or faults inside a system DLL. The remedy landed for ONE reached member as a
// hand-own (`rtlGetVersion`, whose token reached `ntdll!RtlGetVersion` as an access violation);
// this is the GENERAL door, so the next member is a caught panic naming itself instead.
//
// THE NEGATIVE ARM IS THE ONE THAT EARNS THE INCREMENT, and it is why this class is not three
// asserts around a happy path: a door that refuses a REAL address is worse than no door at all,
// because it converts working code into a panic. So the honest argument shapes -- a real native
// address, a pinned managed address, a HANDLE, a length, a flag word, zero, and -1 -- are each
// pinned by name, and -1 twice over, since `INVALID_HANDLE_VALUE` has bit 63 set and is exactly
// what a predicate that tested only the sign bit would wrongly reject.
//
// SPLIT DELIBERATELY FROM THE WIRING. These arms measure whether the PREDICATE is right, and they
// run on every host. Whether the trampoline actually CONSULTS it is a different question, measured
// by the windows-only arm in TokenDoorWiredTests -- one question per arm, because an arm that
// conflates them passes when either half is true.
[TestClass]
public class TokenValueTagRefusalTests
{
    // The shape that mints a token: a struct holding a REFERENCE, so its box gets no pinnable slot
    // and has no address to answer with. Same construction as PointerStorageKindTests.Descriptor.
    private struct ReferenceBearing
    {
        internal string name;
        internal nint scalar;
    }

    private struct ReferenceFree
    {
        internal long a;
        internal long b;
    }

    // ---- the mint's own contract ----

    [TestMethod]
    public void MintSetsBit63AndClearsBit47_LeavingTheLow32BitsForDisplacement()
    {
        ulong token = new StandardBox<ReferenceBearing>(new ReferenceBearing { name = "x" }).PointerOrderToken;

        Assert.AreNotEqual(0UL, token & (1UL << 63), "bit 63 must be SET -- half of what makes the value non-canonical");
        Assert.AreEqual(0UL, token & (1UL << 47), "bit 47 must be CLEAR -- the other half; both together are the tag");
        Assert.AreEqual(0UL, token & 0xFFFFFFFFUL,
            "an allocation base leaves the whole low 32 bits to the within-allocation displacement, " +
            "which is what keeps ElemRefBox's absolute index and IsTokenArithmetic's mask untouched");
        Assert.AreEqual(0UL, token & 7UL, "and it stays 8-aligned, which sync/atomic's TestAutoAligned64 reads through");
    }

    [TestMethod]
    public void ANonCanonicalTokenCanNeverBeAValidX86_64Address()
    {
        // The soundness argument in one assert: x86-64 requires bits 63..47 to be ALL EQUAL in a
        // valid user-mode address. The tag forces them unequal, so the token and address sets are
        // disjoint BY CONSTRUCTION rather than by table -- which is the whole reason the trampoline
        // may test a bare number at all.
        ulong token = new StandardBox<ReferenceBearing>(new ReferenceBearing { name = "y" }).PointerOrderToken;
        ulong top17 = token >> 47;

        Assert.AreNotEqual(0UL, top17, "bits 63..47 are not all zero");
        Assert.AreNotEqual((1UL << 17) - 1, top17, "and not all one -- so the value is non-canonical either way");
    }

    // ---- POSITIVE: the class the door exists for ----

    [TestMethod]
    public void Positive_AReferenceBearingBoxTokenIsRefused()
    {
        nuint token = new StandardBox<ReferenceBearing>(new ReferenceBearing { name = "tcp", scalar = 0x5A5A }).PointerOrderToken;

        Assert.IsTrue(ManagedPointerTokens.IsTaggedToken(token),
            "this is the value that reached ntdll!RtlGetVersion as an access violation; the door's " +
            "entire purpose is that it is refusable from the number alone");
    }

    [TestMethod]
    public void Positive_ADisplacedTokenIsStillRefused()
    {
        // The shape that actually reached a banked row is a token PLUS an offset, not a bare base:
        // a field or element reference derived from a reference-bearing allocation. The displacement
        // occupies bits 31..0 and the tag lives at 63 and 47, so a derived token is still refused --
        // asserted rather than reasoned, because a door that catches only bases catches almost none
        // of the real class.
        nuint token = new StandardBox<ReferenceBearing>(new ReferenceBearing { name = "z" }).PointerOrderToken;

        foreach (nuint displacement in new nuint[] { 1, 8, 4096, 0x7FFFFFFF, 0xFFFFFFFF })
        {
            Assert.IsTrue(ManagedPointerTokens.IsTaggedToken(token + displacement),
                $"a token displaced by {displacement} is still a token");
        }
    }

    // ---- NEGATIVE, AND LOAD-BEARING: a door that refuses an honest argument is worse than none ----

    [TestMethod]
    public void Negative_MinusOneIsNeverRefused_INVALID_HANDLE_VALUE()
    {
        // Pinned alone and first, because it is the single most likely false positive: -1 widened to
        // uintptr is 0xFFFF_FFFF_FFFF_FFFF, which HAS bit 63 set. A predicate that tested only the
        // sign bit would reject every failed handle in the corpus, and would do it in code paths
        // that are already error paths -- the hardest place to notice.
        Assert.IsFalse(ManagedPointerTokens.IsTaggedToken(unchecked((nuint)(nint)(-1))),
            "INVALID_HANDLE_VALUE has bit 63 SET and bit 47 SET, so it is canonical-shaped and passes");
    }

    [TestMethod]
    public void Negative_EveryHonestArgumentShapeIsRefusedByNothing()
    {
        foreach ((nuint value, string what) in new (nuint, string)[]
        {
            (0, "a null pointer / zero flag word"),
            (1, "a small count"),
            (3, "STD_ERROR_HANDLE-shaped small handle"),
            (0x1000, "a page-sized length"),
            (0x0000_0400, "a flag word (FILE_FLAG_-shaped)"),
            (uint.MaxValue, "a 32-bit -1 that was NOT sign-extended"),
            ((nuint)0x7FFF_FFFF_FFFF_FFFFUL, "the largest positive intptr"),
            (unchecked((nuint)(nint)(-1)), "INVALID_HANDLE_VALUE"),
            (unchecked((nuint)(nint)(-2)), "a negative errno-shaped value"),
        })
        {
            Assert.IsFalse(ManagedPointerTokens.IsTaggedToken(value), $"{what} must pass the door untouched");
        }
    }

    [TestMethod]
    public unsafe void Negative_RealAddressesAreRefusedByNothing()
    {
        // Two real addresses of different provenance, because "canonical" is a property of the
        // ADDRESS SPACE and one allocator's range is not evidence about another's.
        IntPtr native = Marshal.AllocHGlobal(64);

        try
        {
            Assert.IsFalse(ManagedPointerTokens.IsTaggedToken((nuint)(nint)native),
                "a native heap address is canonical and must pass");

            byte[] managed = new byte[64];

            fixed (byte* pinned = managed)
            {
                Assert.IsFalse(ManagedPointerTokens.IsTaggedToken((nuint)pinned),
                    "and so is a pinned managed buffer's address -- the shape readFile hands the kernel");
            }

            // The stack too: a third range, and the one an out-parameter most often names.
            long local = 0;

            Assert.IsFalse(ManagedPointerTokens.IsTaggedToken((nuint)(&local)), "a stack address is canonical and must pass");
        }
        finally
        {
            Marshal.FreeHGlobal(native);
        }
    }

    // ---- PROVENANCE: the box whose token IS an address must never be tagged ----

    [TestMethod]
    public unsafe void Provenance_ANativeBackedBoxTokenIsARealAddress_AndIsNotRefused()
    {
        IntPtr native = Marshal.AllocHGlobal(sizeof(ReferenceFree));

        try
        {
            ж<ReferenceFree> box = new NativeBox<ReferenceFree>((nuint)(nint)native);
            nuint token = box.PointerOrderToken;

            Assert.AreEqual((nuint)(nint)native, token,
                "a native alias's token IS its address -- it is not minted and must not be tagged");
            Assert.IsFalse(ManagedPointerTokens.IsTaggedToken(token),
                "so the door must let it through; refusing it would break every hand-owned native seam");
        }
        finally
        {
            Marshal.FreeHGlobal(native);
        }
    }

    // ---- the predicate must be able to say NO: a green that cannot go red is not a measurement ----

    [TestMethod]
    public void ThePredicateDiscriminates_ItDoesNotAnswerTrueForEverythingWithBit63()
    {
        // Same top bit, opposite verdicts, decided only by bit 47. If this ever passes with both
        // values true, the predicate has degenerated into a sign test and the negative arm above is
        // green for the wrong reason.
        const ulong tagged = (1UL << 63);
        const ulong canonicalShaped = (1UL << 63) | (1UL << 47);

        Assert.IsTrue(ManagedPointerTokens.IsTaggedToken((nuint)tagged), "bit 63 set, bit 47 clear -> a token");
        Assert.IsFalse(ManagedPointerTokens.IsTaggedToken((nuint)canonicalShaped), "bit 63 set, bit 47 SET -> not a token");
    }
}
