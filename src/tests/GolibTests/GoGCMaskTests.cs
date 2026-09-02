// GoGCMaskTests.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

/// <summary>
/// Guards <see cref="GoReflect.GoGCMaskOf"/> — Go's GC pointer bitmap for a type, one byte per
/// POINTER WORD, <c>1</c> where the collector must scan that word.
/// </summary>
/// <remarks>
/// <para>
/// The expectations below are Go's own, taken from <c>reflect.TestGCBits</c>'s building blocks:
/// <c>*byte</c> is <c>{1}</c>, <c>Xscalar{uintptr}</c> holds no pointer, <c>Xptr{*byte}</c> is
/// <c>{1}</c>, and the two-word mixes <c>Xptrscalar{*byte; uintptr}</c> and
/// <c>Xscalarptr{uintptr; *byte}</c> are <c>{1,0}</c> and <c>{0,1}</c> — which is the pair that
/// makes this a real test rather than a smoke check, because a walk that merely reported "this type
/// has a pointer somewhere" would pass both with the same answer.
/// </para>
/// <para>
/// The GRANULARITY is the thing most easily got wrong, and it is asserted rather than assumed:
/// <c>runtime.getgcmask</c> builds <c>make([]byte, n/goarch.PtrSize)</c> and indexes
/// <c>[i/goarch.PtrSize]</c>, so there is one entry per WORD. reflect's own doc comment says "one
/// entry per byte", which describes the bitmap's storage and would transpose the answer.
/// <c>verifyGCBits</c> compares by PREFIX — it forgives a mask longer than expected, because Go's
/// iterator runs out to the size class — and forgives nothing that is shifted, so a byte-vs-word
/// mistake fails everywhere while an over-long answer passes.
/// </para>
/// </remarks>
[TestClass]
public class GoGCMaskTests
{
    [GoType] private partial struct Xscalar { public nuint x; }
    [GoType] private partial struct Xptr { public ж<byte> x; }
    [GoType] private partial struct Xptrscalar { public ж<byte> p; public nuint s; }
    [GoType] private partial struct Xscalarptr { public nuint s; public ж<byte> p; }

    private static void AssertMask(string what, byte[] expected, byte[]? actual)
    {
        Assert.IsNotNull(actual, $"{what}: GoGCMaskOf answered null — the layout was not derivable");
        Assert.AreEqual(expected.Length, actual!.Length, $"{what}: mask LENGTH (one entry per pointer word)");

        for (int i = 0; i < expected.Length; i++)
            Assert.AreEqual(expected[i], actual[i], $"{what}: word {i}");
    }

    [TestMethod]
    public void PointerIsOneScannedWord()
    {
        AssertMask("*byte", [1], GoReflect.GoGCMaskOf(typeof(ж<byte>)));
    }

    [TestMethod]
    public void ScalarStructHoldsNoPointer()
    {
        AssertMask("struct{ x uintptr }", [0], GoReflect.GoGCMaskOf(typeof(Xscalar)));
    }

    [TestMethod]
    public void PointerStructIsOneScannedWord()
    {
        AssertMask("struct{ x *byte }", [1], GoReflect.GoGCMaskOf(typeof(Xptr)));
    }

    // The discriminating pair: same size, same pointer COUNT, different pointer POSITION.
    [TestMethod]
    public void PointerThenScalarScansTheFirstWordOnly()
    {
        AssertMask("struct{ *byte; uintptr }", [1, 0], GoReflect.GoGCMaskOf(typeof(Xptrscalar)));
    }

    [TestMethod]
    public void ScalarThenPointerScansTheSecondWordOnly()
    {
        AssertMask("struct{ uintptr; *byte }", [0, 1], GoReflect.GoGCMaskOf(typeof(Xscalarptr)));
    }

    // Go's own shapes, asserted because the per-kind table is where a walk silently goes wrong:
    // a string is {ptr,len} so ONE of its two words is scanned, and an interface is {type,data}
    // with BOTH scanned. A walk that answered "16 bytes of pointer" for both would pass PtrBytes
    // and fail here.
    [TestMethod]
    public void StringScansItsPointerWordOnly()
    {
        AssertMask("string", [1, 0], GoReflect.GoGCMaskOf(typeof(@string)));
    }

    [TestMethod]
    public void MaskAgreesWithPtrBytesOnEveryShape()
    {
        // The two answers are the same truth at different resolutions, so the LAST set word must be
        // exactly PtrBytes/8 - 1. Asserted across the shapes above so the pair can never drift.
        foreach (Type t in new[] { typeof(ж<byte>), typeof(Xscalar), typeof(Xptr), typeof(Xptrscalar), typeof(Xscalarptr), typeof(@string) })
        {
            byte[]? mask = GoReflect.GoGCMaskOf(t);
            nint ptrBytes = GoReflect.GoPtrBytesOf(t);

            Assert.IsNotNull(mask, $"{t.Name}: mask");

            int last = -1;

            for (int i = 0; i < mask!.Length; i++)
            {
                if (mask[i] != 0)
                    last = i;
            }

            Assert.AreEqual(ptrBytes == 0 ? -1 : (int)(ptrBytes / 8) - 1, last, $"{t.Name}: last scanned word vs PtrBytes");
        }
    }
    // -------- the seam's own primitive: runtime.getgcmask reaches the pointee TYPE through
    // PointeeTypeOfValue, so the boxed-pointer contract is guarded here rather than only through
    // reflect's suite. The negative arm is the one that matters: getgcmask THROWS Go's
    // "bad argument" text on a null answer, so a helper that answered a type for a non-pointer
    // would silently turn a Go panic into a wrong mask.
    [TestMethod]
    public void PointeeTypeOfValueReadsThroughAPointerBox()
    {
        Assert.AreEqual(typeof(Xptrscalar), GoReflect.PointeeTypeOfValue(Ꮡ(new Xptrscalar())), "*Xptrscalar");
        Assert.AreEqual(typeof(byte), GoReflect.PointeeTypeOfValue(Ꮡ((byte)7)), "*byte");
    }

    [TestMethod]
    public void PointeeTypeOfValueRefusesWhatIsNotAPointer()
    {
        Assert.IsNull(GoReflect.PointeeTypeOfValue(null), "null");
        Assert.IsNull(GoReflect.PointeeTypeOfValue(new Xptrscalar()), "a value, not a pointer to one");
        Assert.IsNull(GoReflect.PointeeTypeOfValue((@string)"s"), "a string is not a pointer");
    }
}
