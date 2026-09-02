// GoSizeWidthTests.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

/// <summary>
/// Guards <see cref="GoReflect.TryGoSizeOf"/> — the Go size as Go's own UNSIGNED width, with
/// derivability answered separately from the size.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="GoReflect.GoSizeOf"/> answers <c>-1</c> for "cannot be known", and that was the same
/// answer a size of 2^63 and up produced, because <c>nint</c> is signed. The two are different
/// questions and a legal Go type really does reach the far half of the address space:
/// <c>reflect.TestStructOfTooLarge</c> builds a 2^64-3 struct out of two half-address-space arrays
/// specifically to exercise Go's four overflow panics.
/// </para>
/// <para>
/// The discriminating pair below is the point. <c>[2^63-1]byte</c> and <c>[2^62]int16</c> differ by
/// ONE byte of Go size and land on opposite sides of <c>nint</c>: the first is the largest size the
/// signed form could report, the second is the smallest it could not. A width fix that did not
/// actually widen would pass the first and fail the second.
/// </para>
/// </remarks>
[TestClass]
public class GoSizeWidthTests
{
    private static readonly nint HalfAddressSpace = nint.MaxValue;   // 2^63-1

    [TestMethod]
    public void TheLargestSizeThatFitsSignedIsReportedExactly()
    {
        Assert.IsTrue(GoReflect.TryGoSizeOf(typeof(array<byte>), [HalfAddressSpace], out nuint size), "[2^63-1]byte derivable");
        Assert.AreEqual((nuint)nint.MaxValue, size, "[2^63-1]byte size");
        Assert.AreEqual(nint.MaxValue, GoReflect.GoSizeOf(typeof(array<byte>), [HalfAddressSpace]), "the signed form still answers it");
    }

    [TestMethod]
    public void TheSmallestSizeThatDoesNotFitSignedIsStillASize()
    {
        // 2^62 int16s = 2^63 bytes: one more than the row above, and negative as nint.
        Assert.IsTrue(GoReflect.TryGoSizeOf(typeof(array<int16>), [(nint)1 << 62], out nuint size), "[2^62]int16 derivable");
        Assert.AreEqual((nuint)1 << 63, size, "[2^62]int16 size");

        // And the signed form reports it as unknowable rather than negative — the documented
        // narrowing, asserted so the contract cannot drift back to a wrong number.
        Assert.AreEqual(-1, GoReflect.GoSizeOf(typeof(array<int16>), [(nint)1 << 62]), "the signed form refuses it");
    }

    [TestMethod]
    public void ASizePastTheAddressSpaceIsNotDerivableRatherThanWrapped()
    {
        // 2^63-1 int64s is 8x the address space. Go's ArrayOf panics before such a type exists, so
        // the honest answer from a QUERY is "cannot describe it" — never a wrapped-around number,
        // which is what a saturating or unchecked multiply would produce.
        Assert.IsFalse(GoReflect.TryGoSizeOf(typeof(array<int64>), [HalfAddressSpace], out nuint size), "[2^63-1]int64 must not be derivable");
        Assert.AreEqual((nuint)0, size, "an underivable size is zero, never a wrapped value");
    }

    [TestMethod]
    public void DerivabilityIsStillAnsweredForTheOrdinaryUnknowableCase()
    {
        // The question -1 was ALWAYS asking: an array whose length the managed type does not carry.
        // Widening must not have turned "unknown" into "zero-sized".
        Assert.IsFalse(GoReflect.TryGoSizeOf(typeof(array<byte>), null, out _), "a dimension-less array is not derivable");
        Assert.AreEqual(-1, GoReflect.GoSizeOf(typeof(array<byte>)), "and the signed form agrees");
    }

    [TestMethod]
    public void OrdinarySizesAreUnchanged()
    {
        Assert.IsTrue(GoReflect.TryGoSizeOf(typeof(@string), null, out nuint str), "string");
        Assert.AreEqual((nuint)16, str, "string is {ptr,len}");

        Assert.IsTrue(GoReflect.TryGoSizeOf(typeof(slice<byte>), null, out nuint sl), "slice");
        Assert.AreEqual((nuint)24, sl, "slice is {ptr,len,cap}");

        Assert.IsTrue(GoReflect.TryGoSizeOf(typeof(array<int32>), [4], out nuint arr), "[4]int32");
        Assert.AreEqual((nuint)16, arr, "[4]int32");
    }
}
