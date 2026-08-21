using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

[TestClass]
public class PointerTokenLayoutTruthTests
{
    // Ruling A (board, 2026-08-20): `Value.Pointer()` is an identity TOKEN, not an address, but the
    // assertion sync/atomic's TestAutoAligned64 makes of it is Go's align64 GUARANTEE -- a layout
    // invariant the model genuinely honors. So the token's low bits must mirror the Go-computed
    // layout: allocation bases are minted 8-aligned and a field's token is base + its Go offset,
    // which makes `p & 7` answer from the SAME GoFieldOffsets walk that answers StructField.Offset.
    //
    // The ruling also requires that token DISTINCTNESS be measured, not assumed -- the whole reason
    // the old construction packed a field-identity hash into the low bits. These guards are that
    // measurement, and they are the golib-level contract: the Go-parity half is TestAutoAligned64
    // itself, running through the converted sync/atomic suite.

    // `struct { _ uint32; i int64 }` -- TestAutoAligned64's shape exactly: a 4-byte field forcing
    // the 8-byte field to offset 8 under Go's amd64 rules.
    private struct Aligned64Probe
    {
        internal uint pad;
        internal long i;
    }

    private struct MixedWidths
    {
        internal byte a;
        internal short b;
        internal int c;
        internal long d;
    }

    [TestMethod]
    public void AllocationBaseIs8Aligned()
    {
        // A heap box IS its storage, so its token is the bare allocation base.
        for (int i = 0; i < 64; i++)
        {
            ref MixedWidths value = ref heap(new MixedWidths { d = i }, out ж<MixedWidths> boxed);
            _ = value;

            Assert.AreEqual(0UL, (ulong)boxed.PointerOrderToken & 7UL,
                "every allocation base must be 8-aligned, so a pointer to a struct never contradicts Go's alignment guarantee");
        }
    }

    [TestMethod]
    public void FieldTokenIsBasePlusGoOffset()
    {
        ref Aligned64Probe probe = ref heap(new Aligned64Probe(), out ж<Aligned64Probe> Ꮡprobe);
        _ = probe;

        nuint expectedBase = Ꮡprobe.PointerOrderToken;

        // GoFieldOffsets is the authority the ruling names; the token must agree with it field for
        // field, which is what makes `Offset` and `p & 7` one answer instead of two.
        nint[] offsets = GoReflect.GoFieldOffsets(typeof(Aligned64Probe))!;

        Assert.AreEqual(0, (int)offsets[0], "fixture is inert: pad must sit at offset 0");
        Assert.AreEqual(8, (int)offsets[1], "fixture is inert: Go's amd64 rules must put the 8-byte field at offset 8");

        nuint padToken = Ꮡprobe.of<uint>(Aligned64ProbeAccess.Ꮡpad).PointerOrderToken;
        nuint iToken = Ꮡprobe.of<long>(Aligned64ProbeAccess.Ꮡi).PointerOrderToken;

        Assert.AreEqual(expectedBase + (nuint)offsets[0], padToken, "a field's token is its allocation base plus its GO offset");
        Assert.AreEqual(expectedBase + (nuint)offsets[1], iToken, "a field's token is its allocation base plus its GO offset");

        // The assertion TestAutoAligned64 actually makes.
        Assert.AreEqual(0UL, (ulong)iToken & 7UL, "an 8-byte field at Go offset 8 must token 8-aligned");
    }

    [TestMethod]
    public void FieldTokensStayDISTINCTAcrossFieldsAndAllocations()
    {
        // The distinctness measurement the ruling requires. Two things must hold: fields at
        // DIFFERENT Go offsets never share a token within one allocation, and no token is reused
        // across allocations.
        //
        // What must NOT be asserted is that a struct and its offset-0 field differ. They are ONE
        // address in Go -- `unsafe.Pointer(&s) == unsafe.Pointer(&s.a)` -- and under this
        // construction they token alike, which is the layout truth arriving rather than a collision.
        // (Two zero-size fields sharing an offset are the same case.) The first draft of this guard
        // asserted 5 distinct tokens per allocation and failed on exactly that row; the assertion was
        // wrong, not the construction, and it is written out here so the next reader does not
        // "restore" it.
        HashSet<nuint> all = new();
        int allocations = 128;

        for (int i = 0; i < allocations; i++)
        {
            ref MixedWidths value = ref heap(new MixedWidths(), out ж<MixedWidths> box);
            _ = value;

            nuint whole = box.PointerOrderToken;
            nuint a = box.of<byte>(MixedWidthsAccess.Ꮡa).PointerOrderToken;
            nuint b = box.of<short>(MixedWidthsAccess.Ꮡb).PointerOrderToken;
            nuint c = box.of<int>(MixedWidthsAccess.Ꮡc).PointerOrderToken;
            nuint d = box.of<long>(MixedWidthsAccess.Ꮡd).PointerOrderToken;

            Assert.AreEqual(whole, a, "a struct and its offset-0 field are one Go address and must token alike");

            Assert.AreEqual(4, new HashSet<nuint> { a, b, c, d }.Count,
                "four fields at four distinct Go offsets must produce four distinct tokens");

            foreach (nuint token in new[] { a, b, c, d })
            {
                Assert.IsTrue(all.Add(token), "a token minted for one allocation must not repeat for another");
            }
        }

        Assert.AreEqual(allocations * 4, all.Count, "every (allocation, distinct-offset field) pair must hold its own token");
    }

    [TestMethod]
    public void EqualPointersStillTokenEqually()
    {
        // The documented invariant the construction may not break: two boxes that ARE the same Go
        // pointer token alike. Taking the same field twice is exactly that case.
        ref MixedWidths value = ref heap(new MixedWidths(), out ж<MixedWidths> box);
        _ = value;

        ж<long> first = box.of<long>(MixedWidthsAccess.Ꮡd);
        ж<long> second = box.of<long>(MixedWidthsAccess.Ꮡd);

        Assert.AreEqual(first, second, "fixture is inert: two references to one field must be equal pointers");
        Assert.AreEqual(first.PointerOrderToken, second.PointerOrderToken, "equal pointers must always token equally");
    }

    private static class Aligned64ProbeAccess
    {
        internal static ref uint Ꮡpad(ref Aligned64Probe instance) => ref instance.pad;
        internal static ref long Ꮡi(ref Aligned64Probe instance) => ref instance.i;
    }

    private static class MixedWidthsAccess
    {
        internal static ref byte Ꮡa(ref MixedWidths instance) => ref instance.a;
        internal static ref short Ꮡb(ref MixedWidths instance) => ref instance.b;
        internal static ref int Ꮡc(ref MixedWidths instance) => ref instance.c;
        internal static ref long Ꮡd(ref MixedWidths instance) => ref instance.d;
    }
}
