using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

[TestClass]
public class RuneConversionTests
{
    // C5 (span-unification census, tranche 1). `[]rune(s)` went through
    // `((IEnumerable<rune>)s).ToArray()`: the rune enumerator calls ToRunes (a full decode into an
    // array), yields that array back one element at a time through an iterator, and LINQ
    // re-materializes it through its growth buffers -- three passes for a conversion Go performs
    // with one. `rune(s)` was worse in shape: it decoded the WHOLE string to take element zero.
    //
    // Both now decode directly. The decode SEMANTICS are the delicate part, because Go's are not
    // .NET's: an invalid sequence yields one U+FFFD per invalid BYTE, never .NET's maximal-subpart
    // consumption that can swallow several bytes as a single replacement. These pin that on both
    // paths, and pin the two against each other.

    private const int ReplacementChar = 0xFFFD;

    [TestMethod]
    public void RuneSliceDecodesMultiByteRunes()
    {
        slice<rune> runes = (@string)"aé☺𝄞";

        Assert.AreEqual((nint)4, runes.Length, "one element per RUNE, not per byte");
        Assert.AreEqual('a', runes[0]);
        Assert.AreEqual('é', runes[1]);
        Assert.AreEqual('☺', runes[2]);
        Assert.AreEqual(0x1D11E, runes[3], "the astral rune must decode to one code point, not a surrogate pair");
    }

    [TestMethod]
    public void RuneSliceOfAnEmptyStringIsEmpty()
    {
        slice<rune> runes = (@string)"";

        Assert.AreEqual((nint)0, runes.Length);
    }

    // Go yields ONE U+FFFD per invalid byte. 0xFF 0xFE is two invalid bytes, so two replacements --
    // .NET's maximal-subpart rule would be entitled to report fewer.
    [TestMethod]
    public void RuneSliceYieldsOneReplacementPerInvalidByte()
    {
        slice<rune> runes = new @string(new byte[] { (byte)'a', 0xFF, 0xFE, (byte)'b' });

        Assert.AreEqual((nint)4, runes.Length,
            "an invalid sequence must advance a SINGLE byte per replacement, as Go's []rune(string) does");
        Assert.AreEqual('a', runes[0]);
        Assert.AreEqual(ReplacementChar, runes[1]);
        Assert.AreEqual(ReplacementChar, runes[2]);
        Assert.AreEqual('b', runes[3]);
    }

    [TestMethod]
    public void RuneSliceRespectsAStringWindow()
    {
        @string whole = "abcdef";

        slice<rune> runes = whole[2..4];

        Assert.AreEqual((nint)2, runes.Length, "the conversion read outside the string's own window");
        Assert.AreEqual('c', runes[0]);
        Assert.AreEqual('d', runes[1]);
    }

    // The resulting slice owns its array: the conversion is a decode, so nothing may alias the
    // string's immutable backing.
    [TestMethod]
    public void RuneSliceIsWritableWithoutDisturbingTheString()
    {
        @string source = "abc";

        slice<rune> runes = source;
        runes[0] = 'z';

        Assert.AreEqual("abc", source.ToString());
        Assert.AreEqual('z', runes[0]);
    }

    [TestMethod]
    public void RuneCastTakesTheFirstRune()
    {
        Assert.AreEqual('a', (rune)(@string)"abc");
        Assert.AreEqual('é', (rune)(@string)"éa", "a multi-byte leading rune must decode whole");
        Assert.AreEqual(0x1D11E, (rune)(@string)"𝄞x", "an astral leading rune must decode whole");
    }

    [TestMethod]
    public void RuneCastOfAnEmptyStringIsZero()
    {
        Assert.AreEqual(0, (rune)(@string)"");
    }

    [TestMethod]
    public void RuneCastOfAnInvalidLeadingByteIsTheReplacementChar()
    {
        Assert.AreEqual(ReplacementChar, (rune)new @string(new byte[] { 0xFF, (byte)'a' }));
    }

    [TestMethod]
    public void RuneCastRespectsAStringWindow()
    {
        @string whole = "abcdef";

        Assert.AreEqual('c', (rune)whole[2..4]);
    }

    // The cheap single-rune path and the full decode must not be able to disagree: whatever
    // []rune(s) puts first is what rune(s) answers.
    [TestMethod]
    public void TheTwoConversionsAgreeOnTheFirstRune()
    {
        @string[] cases =
        {
            "abc",
            "éa",
            "☺x",
            "𝄞x",
            new @string(new byte[] { 0xFF, (byte)'a' }),
            new @string(new byte[] { 0xC3 }),
        };

        foreach (@string value in cases)
        {
            slice<rune> runes = value;

            Assert.AreEqual(runes[0], (rune)value,
                $"rune(s) and []rune(s)[0] disagree for {value.Length} byte(s)");
        }
    }
}
