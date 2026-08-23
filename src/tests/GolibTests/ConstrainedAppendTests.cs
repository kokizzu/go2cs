using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using static go.builtin;

namespace GolibTests;

[TestClass]
public class ConstrainedAppendTests
{
    // C4 (span-unification census, tranche 1): the constrained `append` over a ReadOnlySpan
    // (`S ~[]E` generic bodies) called items.ToArray() before handing the elements on -- one array
    // allocation and copy per call, purely because slice<T>.Append took Span. Append's body only
    // ever READ its elements, so the parameter widened to ReadOnlySpan and the array is gone.
    //
    // These pin the semantics Append owes Go, since widening a parameter must not disturb them:
    // the empty append returns the SAME header (identity, not a fresh allocation), a within-
    // capacity append writes IN PLACE and is visible through every sharer of the backing, and
    // growing past capacity DETACHES.

    // slice<T> satisfies `S : ISlice<T>, ISliceWrap<S, T>` itself -- that is exactly why the
    // constrained overload routes to slice<T>.Append directly rather than recursing through
    // append(...) -- so it stands in for a converter-emitted named slice type here.
    [TestMethod]
    public void ConstrainedAppendOverAReadOnlySpanAppendsTheElements()
    {
        slice<byte> seq = new slice<byte>(new byte[] { 1, 2 });
        ReadOnlySpan<byte> items = stackalloc byte[] { 3, 4, 5 };

        slice<byte> result = append<slice<byte>, byte>(seq, items);

        Assert.AreEqual((nint)5, result.Length);
        Assert.AreEqual(1, result[0]);
        Assert.AreEqual(5, result[4]);
    }

    // Go's append with nothing to add returns s ITSELF: nil stays nil, and a non-nil empty stays
    // that same non-nil empty. bytes.Clone depends on the second half.
    [TestMethod]
    public void EmptyAppendReturnsTheSameHeader()
    {
        byte[] backing = { 7, 8, 9 };
        slice<byte> source = new slice<byte>(backing);

        slice<byte> result = go.slice<byte>.Append(source, ReadOnlySpan<byte>.Empty);

        Assert.AreEqual(source.Length, result.Length);

        // Backing identity is asserted BEHAVIORALLY: `Source` materializes a fresh copy (that is
        // C7's subject), so comparing it would pass no matter what the append did.
        result[0] = 55;
        Assert.AreEqual(55, backing[0], "the empty append laundered identity through a new allocation");
    }

    [TestMethod]
    public void WithinCapacityAppendWritesInPlaceAndIsSharedWithTheOriginal()
    {
        // Length 2, capacity 4 -- room to append without reallocating.
        byte[] backing = new byte[4];
        slice<byte> source = new slice<byte>(backing).slice(0, 2, 4);
        ReadOnlySpan<byte> items = stackalloc byte[] { 42 };

        slice<byte> grown = go.slice<byte>.Append(source, items);

        Assert.AreEqual((nint)3, grown.Length);
        Assert.AreEqual(42, grown[2]);
        Assert.AreEqual(42, backing[2],
            "a within-capacity append reallocated; Go appends IN PLACE, so the element must be " +
            "visible through the original backing every sharer sees");
    }

    [TestMethod]
    public void AppendPastCapacityDetachesFromTheOriginalBacking()
    {
        byte[] backing = { 1, 2 };
        slice<byte> source = new slice<byte>(backing);
        ReadOnlySpan<byte> items = stackalloc byte[] { 3 };

        slice<byte> grown = go.slice<byte>.Append(source, items);

        Assert.AreEqual((nint)3, grown.Length);

        grown[0] = 99;
        Assert.AreEqual(1, backing[0],
            "growing past capacity must DETACH, as Go's append does -- the grown slice still " +
            "writes through to the old backing");
    }

    // The census flagged one risk in widening the parameter: the corpus must not grow a CS0121.
    // Widening the EXISTING overload rather than adding a second params-span one is what avoids it,
    // and these call shapes are the ones that would have become ambiguous -- they are compile-time
    // assertions as much as runtime ones.
    [TestMethod]
    public void EveryArgumentShapeStillBindsUnambiguously()
    {
        slice<byte> source = new slice<byte>(new byte[] { 1 });

        byte[] asArray = { 2, 3 };
        Span<byte> asSpan = stackalloc byte[] { 4 };
        ReadOnlySpan<byte> asReadOnlySpan = stackalloc byte[] { 5 };

        Assert.AreEqual((nint)3, go.slice<byte>.Append(source, asArray).Length);
        Assert.AreEqual((nint)2, go.slice<byte>.Append(source, asSpan).Length);
        Assert.AreEqual((nint)2, go.slice<byte>.Append(source, asReadOnlySpan).Length);
        Assert.AreEqual((nint)3, go.slice<byte>.Append(source, 6, 7).Length);
    }
}
