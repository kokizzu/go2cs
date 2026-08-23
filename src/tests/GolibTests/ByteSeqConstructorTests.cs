using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

[TestClass]
public class ByteSeqConstructorTests
{
    // C3 (span-unification census, tranche 1): the two BOXED-interface copy constructors --
    // slice<T>(IByteSeq<T>) and @string(IByteSeq<byte>) -- walked their source through the
    // interface indexer, paying a dispatch and a bounds check per element. They now take the
    // sequence's own window (`ꓸꓸꓸ`) as a span and copy it once, which is the shape
    // ByteSeqExtensions.ToSlice already used for the constrained-generic route to the same
    // conversion.
    //
    // The property that matters is WINDOW correctness: a sub-slice or sub-string source must
    // contribute only its own window. The element loop got that right by construction (the
    // indexer is window-relative); the span form depends on `ꓸꓸꓸ` being window-correct on both
    // implementers, so these pin it. Allocation behavior is unchanged -- still exactly one
    // charged copy -- and that is asserted rather than assumed.

    // Boxing to the interface is what selects the constructors under test; a concrete slice<byte>
    // or @string argument binds its own more specific overload.
    private static IByteSeq<byte> Boxed(slice<byte> value) => value;

    private static IByteSeq<byte> Boxed(@string value) => value;

    [TestMethod]
    public void SliceFromAWindowedSequenceCopiesOnlyThatWindow()
    {
        slice<byte> source = new slice<byte>(new byte[] { 1, 2, 3, 4, 5, 6 })[2..5];   // {3, 4, 5}

        slice<byte> copy = new slice<byte>(Boxed(source));

        Assert.AreEqual((nint)3, copy.Length, "the copy took more than the source's window");
        Assert.AreEqual(3, copy[0]);
        Assert.AreEqual(4, copy[1]);
        Assert.AreEqual(5, copy[2]);
    }

    // The copying path is the one a NON-slice source takes (a slice<T> is adopted instead, which
    // the fast-path guard below pins). An @string is immutable, so independence is asserted from
    // the other side: writing through the result must not reach the string's backing.
    [TestMethod]
    public void SliceFromANonSliceSequenceIsACopyNotAnAlias()
    {
        @string source = "abc";

        slice<byte> copy = new slice<byte>(Boxed(source));
        copy[0] = 99;

        Assert.AreEqual("abc", source.ToString(),
            "the constructor aliased the string's backing instead of copying it — @string is " +
            "immutable, so a writable window over its storage would break that");
        Assert.AreEqual(99, copy[0]);
    }

    [TestMethod]
    public void StringFromAWindowedSliceCopiesOnlyThatWindow()
    {
        slice<byte> source = new slice<byte>(System.Text.Encoding.ASCII.GetBytes("abcdefgh"))[2..5];

        @string copy = new @string(Boxed(source));

        Assert.AreEqual(3, copy.Length);
        Assert.AreEqual("cde", copy.ToString());
    }

    [TestMethod]
    public void StringFromAWindowedStringCopiesOnlyThatWindow()
    {
        @string whole = "abcdefgh";
        @string source = whole[2..5];   // "cde"

        @string copy = new @string(Boxed(source));

        Assert.AreEqual(3, copy.Length);
        Assert.AreEqual("cde", copy.ToString());
    }

    [TestMethod]
    public void EmptySequencesRoundTrip()
    {
        slice<byte> emptySlice = new slice<byte>(Array.Empty<byte>());

        Assert.AreEqual((nint)0, new slice<byte>(Boxed(emptySlice)).Length);
        Assert.AreEqual(0, new @string(Boxed(emptySlice)).Length);
        Assert.AreEqual(0, new @string(Boxed((@string)"")).Length);
    }

    // The identity fast path must survive: a slice<T> source is adopted, not re-copied.
    [TestMethod]
    public void SliceFromASliceAdoptsItWithoutCopying()
    {
        byte[] backing = { 1, 2, 3 };
        slice<byte> source = new slice<byte>(backing);

        slice<byte> adopted = new slice<byte>(Boxed(source));
        backing[1] = 42;

        Assert.AreEqual(42, adopted[1],
            "the slice<T> fast path stopped adopting its source -- it is meant to be the identity, " +
            "so a later write through the shared backing must be visible");
    }

    [TestMethod]
    public void ConversionCostsExactlyOneChargedAllocation()
    {
        AllocationCounter.Enable();

        // A @string source takes the copying path (only slice<T> is adopted), and boxing it once
        // outside the measurement keeps the interface cast out of the count.
        IByteSeq<byte> source = Boxed((@string)"abcdefgh");

        for (int warm = 0; warm < 32; warm++)
            _ = new slice<byte>(source);

        long before = AllocationCounter.CurrentThreadCount;

        for (int run = 0; run < 100; run++)
            _ = new slice<byte>(source);

        long objects = AllocationCounter.CurrentThreadCount - before;

        Assert.AreEqual(100L, objects,
            $"expected exactly one charged allocation per conversion (100 total), got {objects} — " +
            "the span copy must not have added or removed a charged object");
    }
}
