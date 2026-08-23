using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using static go.builtin;

namespace GolibTests;

[TestClass]
public class SliceCopyWindowTests
{
    // C2b (span-unification census, tranche 1): the heterogeneous `copy` fallback double-offsets.
    // `slice<T>`'s indexers are WINDOW-RELATIVE — they add m_low internally and bounds-check against
    // m_length — so `dst[dst.Low + i]` adds the low bound a second time. With any nonzero-Low
    // operand the copy reads and writes the wrong elements, or panics when the doubled index leaves
    // the window. The sibling overload's own comment states the rule this line breaks.
    //
    // Converted Go cannot reach it (Go's `copy` requires identical element types, which takes the
    // same-type arm), so no corpus gate has ever exercised it — hand-written and interop callers
    // only. That is exactly why it needs a unit guard rather than a behavioral one.

    [TestMethod]
    public void HeterogeneousCopyRespectsSourceAndDestinationWindows()
    {
        // Two DIFFERENT element types, so the copy takes the IConvertible fallback, and both
        // operands are offset views rather than whole arrays.
        slice<int> source = new slice<int>(new[] { 90, 91, 92, 93, 94, 95 })[2..5];   // {92, 93, 94}
        slice<long> destination = new slice<long>(new long[] { 0, 0, 0, 0, 0, 0 })[3..6];

        nint copied = copy(destination, source);

        Assert.AreEqual((nint)3, copied, "copy must report min(len(dst), len(src))");
        Assert.AreEqual(92L, destination[0], "the window's first element was not written");
        Assert.AreEqual(93L, destination[1]);
        Assert.AreEqual(94L, destination[2], "the window's last element was not written");
    }

    [TestMethod]
    public void HeterogeneousCopyLeavesElementsOutsideTheWindowUntouched()
    {
        long[] backing = { 7, 7, 7, 7, 7, 7 };
        slice<long> whole = new slice<long>(backing);
        slice<long> window = whole[2..4];
        slice<int> source = new slice<int>(new[] { 1, 2 });

        copy(window, source);

        Assert.AreEqual(7L, backing[0], "an element BEFORE the destination window was overwritten");
        Assert.AreEqual(7L, backing[1], "an element BEFORE the destination window was overwritten");
        Assert.AreEqual(1L, backing[2]);
        Assert.AreEqual(2L, backing[3]);
        Assert.AreEqual(7L, backing[4], "an element AFTER the destination window was overwritten");
        Assert.AreEqual(7L, backing[5], "an element AFTER the destination window was overwritten");
    }

    // The same-type path is the one converted Go actually emits; it is correct today and must stay
    // correct through C2's span unification, offsets included.
    [TestMethod]
    public void SameTypeCopyRespectsWindowsToo()
    {
        byte[] backing = { 0, 0, 0, 0, 0, 0 };
        slice<byte> destination = new slice<byte>(backing)[1..4];
        slice<byte> source = new slice<byte>(new byte[] { 10, 20, 30, 40, 50 })[2..5];

        nint copied = copy(destination, source);

        Assert.AreEqual((nint)3, copied);
        Assert.AreEqual(0, backing[0], "wrote before the destination window");
        Assert.AreEqual(30, backing[1]);
        Assert.AreEqual(40, backing[2]);
        Assert.AreEqual(50, backing[3]);
        Assert.AreEqual(0, backing[4], "wrote after the destination window");
    }
}
