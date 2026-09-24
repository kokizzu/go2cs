// ValuePunBitcastTests.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using static go.builtin;

namespace GolibTests;

/// <summary>
/// Guards golib's <c>bitcast</c>, the rendering of Go's value pun READ
/// <c>*(*U)(unsafe.Pointer(&amp;x))</c> (math.Float64bits and its siblings; the Float*bits seat): it must
/// reproduce the bits exactly, including the patterns a float round trip would disturb, and allocate
/// nothing, where the reinterpret it replaces cost three counted objects per call.
/// </summary>
[TestClass]
public class ValuePunBitcastTests
{
    [TestMethod]
    public void Float64BitsMatchTheIEEEPatterns()
    {
        Assert.AreEqual(0x3FF8000000000000UL, bitcast<double, ulong>(1.5));
        Assert.AreEqual(0x8000000000000000UL, bitcast<double, ulong>(-0.0), "negative zero keeps its sign bit");
        Assert.AreEqual(0x7FF0000000000000UL, bitcast<double, ulong>(double.PositiveInfinity));
        Assert.AreEqual(double.NegativeInfinity, bitcast<ulong, double>(0xFFF0000000000000UL));
    }

    [TestMethod]
    public void NaNPayloadsSurviveTheRoundTripInBothWidths()
    {
        const ulong payload64 = 0x7FF0000000000123UL; // a signalling NaN with a payload
        Assert.AreEqual(payload64, bitcast<double, ulong>(bitcast<ulong, double>(payload64)));

        const uint payload32 = 0x7F800123U;
        Assert.AreEqual(payload32, bitcast<float, uint>(bitcast<uint, float>(payload32)));
        Assert.AreEqual(0x3FC00000U, bitcast<float, uint>(1.5f));
    }

    // The emission bitcast replaces, verbatim (math/unsafe.cs at fa18863b94): the parameter heap-boxed
    // because its address is taken, then read back through a reinterpreting field reference.
    private static ulong ReinterpretFloat64bits(double fʗp)
    {
        ref var f = ref heap(fʗp, out var Ꮡf);

        return ~Ꮡf.Reinterpret<double, ulong>();
    }

    [TestMethod]
    public void TheReinterpretItReplacesCostsThreeCountedObjectsPerCall()
    {
        AllocationCounter.Enable();

        long counted0 = AllocationCounter.CurrentThreadCount;
        ulong viaReinterpret = ReinterpretFloat64bits(1.5);
        long reinterpretCount = AllocationCounter.CurrentThreadCount - counted0;

        counted0 = AllocationCounter.CurrentThreadCount;
        ulong viaBitcast = bitcast<double, ulong>(1.5);
        long bitcastCount = AllocationCounter.CurrentThreadCount - counted0;

        Assert.AreEqual(viaReinterpret, viaBitcast, "the two emissions agree on the bits");
        Assert.AreEqual(3L, reinterpretCount, "the heap box, its pinnable slot, and the reinterpreting reference");
        Assert.AreEqual(0L, bitcastCount);
    }

    [TestMethod]
    public void BitcastAllocatesNothingPerCall()
    {
        AllocationCounter.Enable();

        // Warm both instantiations first: a generic method's first call can allocate runtime type data,
        // which is not the call's own cost.
        ulong sink = bitcast<double, ulong>(0.25) + bitcast<float, uint>(0.25f);

        const int calls = 100_000;
        long bytes0 = GC.GetAllocatedBytesForCurrentThread();
        long counted0 = AllocationCounter.CurrentThreadCount;

        for (int i = 0; i < calls; i++)
            sink += bitcast<double, ulong>(i * 0.5) + bitcast<float, uint>(i);

        // Per call, integer-divided as AllocsPerRun divides: a one-off runtime allocation while the loop
        // runs (tier-up) is not a per-call cost; any per-call allocation would read at least 1 here.
        Assert.AreEqual(0L, (AllocationCounter.CurrentThreadCount - counted0) / calls);
        Assert.AreEqual(0L, (GC.GetAllocatedBytesForCurrentThread() - bytes0) / calls);
        Assert.AreNotEqual(0UL, sink);
    }
}
