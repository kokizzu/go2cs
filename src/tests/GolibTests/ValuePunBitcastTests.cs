// ValuePunBitcastTests.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;
using System.Runtime.CompilerServices;
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

    // A signalling NaN (quiet bit clear) passed as a CONSTANT: math.Float32frombits(0x7f800001), the shape
    // reflect's TestConvertNaNs, TestSignalingNaNArgument and TestSignalingNaNReturn take. Only an optimizing
    // JIT inlines bitcast into a caller and constant-folds it, and RyuJIT holds a folded float32 constant as
    // a double, which QUIETS a signalling NaN: 0x7f800001 reads back 0x7fc00001. Validation rows run Release
    // with tiered compilation off, so every method there is compiled fully optimized on its first call; the
    // helpers below are AggressiveOptimization to be compiled that way under any tiering setting. The test
    // above cannot see it (its method runs at tier 0, which does not inline), and neither can a Debug build,
    // which does not optimize: these arms can go red only at Release. The float64 arm is not red at the seat
    // that introduced bitcast (9ac6051e46; x64, net10): the JIT's double constant keeps its bits.
    private const uint SignalingNaN32 = 0x7F800001U;
    private const ulong SignalingNaN64 = 0x7FF0000000000001UL;

    private static readonly float[] s_float32Slot = new float[1];
    private static readonly double[] s_float64Slot = new double[1];

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static uint SignalingNaN32ReadDirectly() => bitcast<float, uint>(bitcast<uint, float>(SignalingNaN32));

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static uint SignalingNaN32ReadAfterAStore()
    {
        s_float32Slot[0] = bitcast<uint, float>(SignalingNaN32);
        return bitcast<float, uint>(s_float32Slot[0]);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static ulong SignalingNaN64ReadDirectly() => bitcast<double, ulong>(bitcast<ulong, double>(SignalingNaN64));

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static ulong SignalingNaN64ReadAfterAStore()
    {
        s_float64Slot[0] = bitcast<ulong, double>(SignalingNaN64);
        return bitcast<double, ulong>(s_float64Slot[0]);
    }

    [TestMethod]
    public void AConstantSignalingNaN32KeepsItsBitsWhenTheJitFoldsTheCast()
    {
        Assert.AreEqual($"{SignalingNaN32:x8} direct, {SignalingNaN32:x8} stored",
            $"{SignalingNaN32ReadDirectly():x8} direct, {SignalingNaN32ReadAfterAStore():x8} stored");
    }

    [TestMethod]
    public void AConstantSignalingNaN64KeepsItsBitsWhenTheJitFoldsTheCast()
    {
        Assert.AreEqual($"{SignalingNaN64:x16} direct, {SignalingNaN64:x16} stored",
            $"{SignalingNaN64ReadDirectly():x16} direct, {SignalingNaN64ReadAfterAStore():x16} stored");
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
