using System;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using static go.runtime_package;

namespace GolibTests;

// runtime.efaceHash and ifaceHash (src/core/runtime/managed_impl.cs), the hash Go gives an interface
// value. The converted bodies read an eface through a pointer to the interface variable, which here is
// a managed reference with no address, so they died in the arm-2a refusal ("*eface over 0x...") and
// runtime's TestSmhasherAvalanche died with them after its Bytes/Int32/Int64 rows had passed.
// What the test asks is the property of OUR hash: hashing the interface's dynamic value, a
// single-bit change in it flips about half of the output bits.
[TestClass]
public class RuntimeEfaceHashTests
{
    [TestMethod]
    public void EqualValuesHashEqual_AndDifferentValuesDiffer()
    {
        Assert.AreEqual(GoEfaceHashProbe((ulong)5, new uintptr(0)), GoEfaceHashProbe((ulong)5, new uintptr(0)));
        Assert.AreNotEqual(GoEfaceHashProbe((ulong)5, new uintptr(0)), GoEfaceHashProbe((ulong)6, new uintptr(0)));
        Assert.AreNotEqual(GoEfaceHashProbe((ulong)5, new uintptr(0)), GoEfaceHashProbe((ulong)5, new uintptr(1)));
    }

    [TestMethod]
    public void ANilInterfaceHashesToItsSeed()
    {
        Assert.AreEqual(new uintptr(7), GoEfaceHashProbe(default!, new uintptr(7)));
    }

    [TestMethod]
    public void EqualStringsHashEqual_AndDifferentStringsDiffer()
    {
        Assert.AreEqual(GoEfaceHashProbe((@string)"hello"u8, new uintptr(0)), GoEfaceHashProbe((@string)"hello"u8, new uintptr(0)));
        Assert.AreNotEqual(GoEfaceHashProbe((@string)"hello"u8, new uintptr(0)), GoEfaceHashProbe((@string)"hellp"u8, new uintptr(0)));
    }

    [TestMethod]
    public void AnUnhashableDynamicTypeIsRefusedWithGosText()
    {
        PanicException ex = Assert.ThrowsException<PanicException>(() => GoEfaceHashProbe(new slice<nint>(new nint[] { 1 }), new uintptr(0)));
        StringAssert.Contains(ex.Message, "hash of unhashable type");
    }

    [TestMethod]
    public void OneFlippedBitFlipsAboutHalfTheOutputBits()
    {
        // TestSmhasherAvalanche's measurement over the same 64-bit key: the mean flip count per (input bit,
        // output bit) sits near 0.5, so the mean over all 64 input bits of the differing output bits is near 32.
        var rng = new Random(12345);
        long total = 0, samples = 0;

        for (int n = 0; n < 200; n++)
        {
            ulong key = (ulong)rng.NextInt64();
            ulong h0 = (ulong)GoEfaceHashProbe(key, new uintptr(0));

            for (int bit = 0; bit < 64; bit++)
            {
                ulong h1 = (ulong)GoEfaceHashProbe(key ^ (1UL << bit), new uintptr(0));
                total += BitOperations.PopCount(h0 ^ h1);
                samples++;
            }
        }

        double mean = (double)total / samples;
        Assert.IsTrue(mean > 28 && mean < 36, $"mean output bits flipped per input bit was {mean}, want near 32");
    }
}
