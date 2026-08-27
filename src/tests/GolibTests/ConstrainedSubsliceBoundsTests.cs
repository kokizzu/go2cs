using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using static go.builtin;

namespace GolibTests;

/// <summary>
/// A sub-slice of a CONSTRAINED slice type parameter (<c>S ~[]E</c>) must apply Go's bounds rules to
/// every bound it is given. It did not: the omitted-high form travelled as <c>high = -1</c> and the
/// omitted-low form as <c>low = -1</c>, and the method CLAMPED every negative low to 0 — so a
/// genuinely negative index produced a valid sub-slice where Go panics.
/// </summary>
/// <remarks>
/// <para>
/// The failure this pins is not academic: <c>slices.Insert</c> and <c>slices.Replace</c> open with
/// the expressions <c>_ = s[i:]</c> and <c>_ = s[i:j]</c> whose ONLY purpose is to panic on a bad
/// index, and Go's <c>TestInsertPanics</c>/<c>TestReplacePanics</c> require exactly that. Both
/// silently succeeded.
/// </para>
/// <para>
/// The remedy removes the sentinels rather than moving them: an omitted low is emitted as the 0 it
/// means, and an omitted high selects a two-argument overload. These guards therefore hold the
/// CONTRACT — every argument is a real bound — which is what makes the next converter change unable
/// to reintroduce a magic value quietly.
/// </para>
/// </remarks>
[TestClass]
public class ConstrainedSubsliceBoundsTests
{
    private static slice<int> Source() => new(new[] { 0, 1, 2, 3, 4 });

    [TestMethod]
    public void ANegativeLowPanicsInsteadOfClampingToZero()
    {
        slice<int> s = Source();

        // The defect, in both arities. Before the fix each answered s[0:…] and Go's panic never fired.
        Assert.ThrowsException<PanicException>(() => subslice<slice<int>, int>(s, -1), "s[-1:] must panic");
        Assert.ThrowsException<PanicException>(() => subslice<slice<int>, int>(s, -1, 2), "s[-1:2] must panic");
        Assert.ThrowsException<PanicException>(() => subslice3<slice<int>, int>(s, -1, 2, 3), "s[-1:2:3] must panic");

        // -1 was the sentinel's own value, so the neighbours matter as much as the boundary itself.
        Assert.ThrowsException<PanicException>(() => subslice<slice<int>, int>(s, -2, 2));
        Assert.ThrowsException<PanicException>(() => subslice<slice<int>, int>(s, nint.MinValue));
    }

    [TestMethod]
    public void ANegativeHighPanicsInsteadOfMeaningTheWindowEnd()
    {
        slice<int> s = Source();

        Assert.ThrowsException<PanicException>(() => subslice<slice<int>, int>(s, 0, -1), "s[0:-1] must panic, not mean s[0:len(s)]");
        Assert.ThrowsException<PanicException>(() => subslice3<slice<int>, int>(s, 0, -1, 3));
    }

    [TestMethod]
    public void TheTwoArgumentFormIsTheOmittedHighBound()
    {
        slice<int> s = Source();
        slice<int> tail = subslice<slice<int>, int>(s, 2);

        Assert.AreEqual((nint)3, len(tail), "s[2:] runs to the end of the window");
        Assert.AreEqual((nint)3, cap(tail));
        Assert.AreEqual(2, tail[(nint)0]);
        Assert.AreEqual(4, tail[(nint)2]);
    }

    [TestMethod]
    public void ZeroIsTheOmittedLowBound()
    {
        slice<int> s = Source();
        slice<int> head = subslice<slice<int>, int>(s, 0, 2);

        Assert.AreEqual((nint)2, len(head), "s[:2] is s[0:2]");
        Assert.AreEqual(0, head[(nint)0]);
        Assert.AreEqual(1, head[(nint)1]);

        slice<int> whole = subslice<slice<int>, int>(s, 0);
        Assert.AreEqual((nint)5, len(whole), "s[:] is s[0:]");
    }

    [TestMethod]
    public void OutOfRangeBoundsStillPanicAndValidOnesStillWork()
    {
        // The rows that were already correct, held so the fix cannot trade them away — Go's own
        // TestInsertPanics has four out-of-bounds cases beside its two negative ones.
        slice<int> s = Source();

        Assert.ThrowsException<PanicException>(() => subslice<slice<int>, int>(s, 6), "low > cap");
        Assert.ThrowsException<PanicException>(() => subslice<slice<int>, int>(s, 2, 6), "high > cap");
        Assert.ThrowsException<PanicException>(() => subslice<slice<int>, int>(s, 3, 2), "high < low");
        Assert.ThrowsException<PanicException>(() => subslice3<slice<int>, int>(s, 0, 3, 2), "max < high");
        Assert.ThrowsException<PanicException>(() => subslice3<slice<int>, int>(s, 0, 2, 6), "max > cap");

        Assert.AreEqual((nint)0, len(subslice<slice<int>, int>(s, 5)), "s[len(s):] is the empty tail, not a panic");
        Assert.AreEqual((nint)2, cap(subslice3<slice<int>, int>(s, 1, 2, 3)), "s[1:2:3] caps at max - low");
    }

    [TestMethod]
    public void ASubsliceSharesTheSourceBacking()
    {
        // Go's sub-slice never copies. The remedy routes through Reslice rather than the `slice()`
        // extension, so this is the property that would break if it had reached for a copy instead.
        slice<int> s = Source();
        slice<int> tail = subslice<slice<int>, int>(s, 2);

        tail[(nint)0] = 99;

        Assert.AreEqual(99, s[(nint)2], "a write through the sub-slice must be visible in the source");
    }
}
