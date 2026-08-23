using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using static go.builtin;

namespace GolibTests;

[TestClass]
public class StringCopyTests
{
    // C1 (span-unification census, tranche 1): `copy(dst, someString)` used to bind the implicit
    // slice<byte>(@string) operator, allocating a charged full-length copy of the string and then
    // copying a SECOND time into dst. These lock the Go semantics the direct span copy has to keep:
    // the count is min(len(dst), len(src)), nothing outside the destination window is touched, and
    // a short destination truncates rather than over-reading the string.

    [TestMethod]
    public void CopyFromStringReturnsMinimumAndWritesThoseBytes()
    {
        slice<byte> destination = new slice<byte>(new byte[5]);

        nint copied = copy(destination, (@string)"hello");

        Assert.AreEqual((nint)5, copied);
        Assert.AreEqual("hello", System.Text.Encoding.UTF8.GetString(destination.ToSpan()));
    }

    [TestMethod]
    public void CopyFromStringTruncatesToAShortDestination()
    {
        slice<byte> destination = new slice<byte>(new byte[3]);

        nint copied = copy(destination, (@string)"hello");

        Assert.AreEqual((nint)3, copied, "copy must report min(len(dst), len(src))");
        Assert.AreEqual("hel", System.Text.Encoding.UTF8.GetString(destination.ToSpan()));
    }

    [TestMethod]
    public void CopyFromStringStopsAtTheEndOfAShortSource()
    {
        byte[] backing = { 9, 9, 9, 9, 9 };
        slice<byte> destination = new slice<byte>(backing);

        nint copied = copy(destination, (@string)"ab");

        Assert.AreEqual((nint)2, copied);
        Assert.AreEqual((byte)'a', backing[0]);
        Assert.AreEqual((byte)'b', backing[1]);
        Assert.AreEqual(9, backing[2], "copy wrote past the end of the source string");
    }

    [TestMethod]
    public void CopyFromStringRespectsTheDestinationWindow()
    {
        byte[] backing = { 9, 9, 9, 9, 9, 9 };
        slice<byte> window = new slice<byte>(backing)[2..5];

        nint copied = copy(window, (@string)"xyz");

        Assert.AreEqual((nint)3, copied);
        Assert.AreEqual(9, backing[0], "wrote before the destination window");
        Assert.AreEqual(9, backing[1], "wrote before the destination window");
        Assert.AreEqual((byte)'x', backing[2]);
        Assert.AreEqual((byte)'y', backing[3]);
        Assert.AreEqual((byte)'z', backing[4]);
        Assert.AreEqual(9, backing[5], "wrote after the destination window");
    }

    // A string carrying a window of its own (a slice of a larger string) must contribute only its
    // own bytes -- the direct path reads @string.Bytes, which is window-relative.
    [TestMethod]
    public void CopyFromASlicedStringUsesOnlyThatWindow()
    {
        @string whole = "abcdefgh";
        @string middle = whole[2..5];   // "cde"
        slice<byte> destination = new slice<byte>(new byte[3]);

        nint copied = copy(destination, middle);

        Assert.AreEqual((nint)3, copied);
        Assert.AreEqual("cde", System.Text.Encoding.UTF8.GetString(destination.ToSpan()));
    }

    [TestMethod]
    public void CopyFromEmptyStringCopiesNothing()
    {
        byte[] backing = { 4, 4, 4 };
        slice<byte> destination = new slice<byte>(backing);

        nint copied = copy(destination, (@string)"");

        Assert.AreEqual((nint)0, copied);
        Assert.AreEqual(4, backing[0]);
    }

    [TestMethod]
    public void CopyIntoEmptyDestinationCopiesNothing()
    {
        slice<byte> destination = new slice<byte>(Array.Empty<byte>());

        nint copied = copy(destination, (@string)"hello");

        Assert.AreEqual((nint)0, copied);
    }

    // Multi-byte UTF-8 truncated mid-rune: Go's copy is byte-wise and splits the rune without
    // complaint, so the direct span copy must not "helpfully" stop at a rune boundary.
    [TestMethod]
    public void CopyFromStringIsByteWiseAcrossRuneBoundaries()
    {
        slice<byte> destination = new slice<byte>(new byte[2]);

        nint copied = copy(destination, (@string)"éx");   // 0xC3 0xA9 0x78

        Assert.AreEqual((nint)2, copied);
        Assert.AreEqual(0xC3, destination[0]);
        Assert.AreEqual(0xA9, destination[1]);
    }
}
