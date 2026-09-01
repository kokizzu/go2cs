using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using go.testing_runtime;

namespace GolibTests;

[TestClass]
public class TestFormatIntegerVerbsTests
{
    // WHY THIS EXISTS, and why it is a GolibTests file.
    //
    // TestFormat is the Phase-4 test host's deliberately fmt-free formatter: it renders every
    // t.Log/t.Error/t.Errorf message for EVERY converted suite. Nothing gated it. Behavioral tests
    // never reach the host, and the differential oracle compares VERDICTS, not log text
    // (TestingInfrastructureRequirements S7) -- so a formatter defect is invisible to the whole
    // Phase-4 apparatus by construction. Two of them lived there because of it, and both were found
    // by reading one failure message rather than by any gate:
    //
    //   1. golib's `uintptr` is a STRUCT (it wraps an nuint so inference and boxing keep reporting
    //      the Go type), so a coercion switching over CLR primitives alone missed it and the shim
    //      answered `%!x(uintptr=...)`. R hit this in reflect's TestValuePointerAndUnsafePointer,
    //      where BOTH sides of a failing comparison print with %#x -- the disclosure hid the very
    //      values the assert existed to show.
    //
    //   2. The same case label listed `nint or nuint` and then called Convert.ToInt64. IntPtr and
    //      UIntPtr do NOT implement IConvertible, so `%x` on a Go `int` -- the commonest integer
    //      kind there is -- did not mis-render, it THREW InvalidCastException and failed the whole
    //      test as INFRASTRUCTURE-ERROR. That one can move a verdict, not just a message, and it
    //      was invisible in the reported output. It survived because no banked suite happens to
    //      hex-format an int in a log line; a sweep could not have caught it either.
    //
    // Same precedent as TestExecutionOutputCapTests one file over: the host has no _test.go
    // anywhere, and a converted suite exercises it only by being RUN -- precisely the path a guard
    // on the reporting machinery must not depend on.
    //
    // PROVENANCE OF THE EXPECTATIONS. Every string below is what `go test -v` printed for the same
    // t.Logf call on go1.23.12, captured from a throwaway package and transcribed here; the
    // converted host was then diffed against that capture and matched 13/13 byte-for-byte. These
    // are measurements, not beliefs about Go's formatting rules -- re-measure rather than reason if
    // one ever looks wrong.

    private static void Check(string expected, string format, params object[] args) =>
        Assert.AreEqual(expected, TestFormat.Sprintf(format, args), $"Sprintf(\"{format}\")");

    private static uintptr Ptr(ulong value) => new((nuint)value);

    [TestMethod]
    public void UintptrRendersInEveryIntegerBase()
    {
        // Defect 1, directly: before the fix every one of these was `%!<verb>(uintptr=3735928559)`.
        uintptr up = Ptr(0xdeadbeef);

        Check("x=deadbeef X=DEADBEEF o=33653337357 b=11011110101011011011111011101111 d=3735928559",
              "x=%x X=%X o=%o b=%b d=%d", up, up, up, up, up);
    }

    [TestMethod]
    public void GoIntInAHexVerbDoesNotThrow()
    {
        // Defect 2, directly. Go's `int` converts to nint, and the pre-fix coercion handed it to
        // Convert.ToInt64, which throws because IntPtr is not IConvertible -- taking down the whole
        // test as an infrastructure error. Asserting the RENDERING also proves it did not merely
        // stop throwing by falling back to a bad-verb disclosure.
        Check("x=1f o=37 b=11111", "x=%x o=%o b=%b", (nint)31, (nint)31, (nint)31);
        Check("x=1000 o=10000 b=1000000000000", "x=%x o=%o b=%b", (nuint)4096, (nuint)4096, (nuint)4096);
    }

    [TestMethod]
    public void NegativesRenderByMagnitudeNotTwosComplement()
    {
        // Go writes these by magnitude with a leading minus. The pre-fix shim rendered a signed
        // long as a two's-complement word: %x of -31 was ffffffffffffffe1.
        Check("x=-1f o=-37 b=-11111 d=-31", "x=%x o=%o b=%b d=%d", (nint)(-31), (nint)(-31), (nint)(-31), (nint)(-31));

        // long.MinValue has no positive counterpart, so negating it in the signed domain overflows.
        Check("x=-8000000000000000 d=-9223372036854775808", "x=%x d=%d", (nint)long.MinValue, (nint)long.MinValue);
    }

    [TestMethod]
    public void AlternateFormPrefixesTheBaseAndKeepsTheSignOutside()
    {
        uintptr up = Ptr(0xdeadbeef);

        Check("0xdeadbeef 0XDEADBEEF 033653337357 0b11011110101011011011111011101111",
              "%#x %#X %#o %#b", up, up, up, up);

        // The minus sits OUTSIDE the prefix, as Go writes it.
        Check("-0x1f -037", "%#x %#o", (nint)(-31), (nint)(-31));
    }

    [TestMethod]
    public void FullUnsignedRangeAndZeroSurvive()
    {
        // The top of uint64 exceeds long.MaxValue; a signed coercion could not represent it at all
        // and disclosed it as a bad verb.
        Check("x=ffffffffffffffff d=18446744073709551615", "x=%x d=%d", ulong.MaxValue, ulong.MaxValue);
        Check("x=ff o=377 b=11111111", "x=%x o=%o b=%b", (byte)255, (byte)255, (byte)255);
        Check("x=0 o=0 b=0", "x=%x o=%o b=%b", Ptr(0), Ptr(0), Ptr(0));
    }

    [TestMethod]
    public void RuneVerbAcceptsEveryIntegerKind()
    {
        // %c shares the coercion, so it carried defect 2 as well: a Go int rune threw rather than
        // rendering. uintptr is legal here in Go too.
        Check("c=A c=B", "c=%c c=%c", (nint)65, Ptr(66));
    }

    [TestMethod]
    public void HexOfBytesAndStringsStillRendersTheirBytes()
    {
        Check("x=6869", "x=%x", (@string)"hi");
        Check("x=dead", "x=%x", new byte[] { 0xde, 0xad }.slice());
    }

    [TestMethod]
    public void KnownGap_OctalAndBinaryOfAByteSliceDiscloseRatherThanRenderElementWise()
    {
        // PINNED DIVERGENCE, measured on go1.23.12: Go renders `%o` of []byte{0xde,0xad} as
        // "[336 255]" and `%b` as "[11011110 10101101]", element-wise. This shim discloses a bad
        // verb instead. That is a deliberate gap, not an oversight -- element-wise rendering applies
        // to every slice, not just bytes, and belongs with the shim's other unimplemented breadth
        // rather than being smuggled in behind the integer bases.
        //
        // The assertion is here so the gap is RECORDED and a future change to it is a deliberate
        // decision rather than a surprise. If someone implements element-wise rendering, this test
        // is the one that should fail and be updated to Go's strings above.
        slice<byte> bytes = new byte[] { 0xde, 0xad }.slice();

        StringAssert.StartsWith(TestFormat.Sprintf("%o", new object[] { bytes }), "%!o(");
        StringAssert.StartsWith(TestFormat.Sprintf("%b", new object[] { bytes }), "%!b(");

        // A STRING under %o is not a gap: Go bad-verbs it too (`%!o(string=hi)`).
        StringAssert.StartsWith(TestFormat.Sprintf("%o", new object[] { (@string)"hi" }), "%!o(");
    }
}
