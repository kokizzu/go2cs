using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

/// <summary>
/// The shapes COORD's second verification of the panic-frames cut named (addendum 2 of
/// review-r-panic-frames-2026-09-27.md), each with Go's go1.24.13 answer on its probe. Every probe runs on a
/// goroutine of its own.
/// </summary>
[TestClass]
public class PanicFramesRound3Tests
{
    private const string Pkg = "panicframesprobe.";

    private static List<string> Below(List<string> names, string owner)
    {
        Assert.IsTrue(names.Count > 3, $"too few frames: {string.Join(" | ", names)}");
        Assert.AreEqual("runtime.Callers", names[0], string.Join(" | ", names));

        int end = names.IndexOf(Pkg + owner, 3);
        Assert.IsTrue(end >= 3, $"the owner {owner} is missing: {string.Join(" | ", names)}");

        return names.GetRange(3, end - 3 + 1);
    }

    private static void AssertNoSplice(List<string> names, string owner)
    {
        List<string> below = Below(names, owner);
        CollectionAssert.AreEqual(new[] { Pkg + owner }, below, $"expected NO splice; got {string.Join(" | ", below)}");
    }

    private static List<string> Run(System.Func<List<string>> probe) => panicframesprobe_package.onGoroutine(probe);

    // ---- C2: panics Go raises from runtime frames, reproduced in hand-owned C# or intrinsified ----

    [TestMethod]
    public void UnsafeSliceWithANegativeLengthSplicesNothing() =>
        AssertNoSplice(Run(panicframesprobe_package.unsafeSliceNegative), "unsafeSliceNegative");

    [TestMethod]
    public void SyncMapCompareAndSwapOnAnUncomparableValueSplicesNothing() =>
        AssertNoSplice(Run(panicframesprobe_package.syncMapUncomparable), "syncMapUncomparable");

    [TestMethod]
    public void Div64ByZeroTheAmd64IntrinsicSplicesNothing() =>
        AssertNoSplice(Run(() => panicframesprobe_package.div64ByZero(0)), "div64ByZero");

    // The control: Div32 is not an intrinsic, so Go shows its Go-source frame and so does the splice.
    [TestMethod]
    public void Div32sOverflowKeepsItsGoSourceFrame() =>
        CollectionAssert.AreEqual(
            new[] { "runtime.gopanic", "math/bits.Div32", Pkg + "div32Overflow" },
            Below(Run(() => panicframesprobe_package.div32Overflow(1)), "div32Overflow"));

    // R2: a nil *T through (*T).M is Go's panicwrap, modelled.
    [TestMethod]
    public void ANilPointerThroughTheValueMethodWrapperIsPanicwrap() =>
        CollectionAssert.AreEqual(
            new[] { "runtime.gopanic", "runtime.panicwrap", Pkg + "(*T).M", Pkg + "nilThroughPointerWrapper" },
            Below(Run(panicframesprobe_package.nilThroughPointerWrapper), "nilThroughPointerWrapper"));

    // The Explicit second guard: golib's own explicit panic is not Go source.
    [TestMethod]
    public void AGolibExplicitPanicSplicesNothing() =>
        AssertNoSplice(Run(panicframesprobe_package.golibExplicitPanic), "golibExplicitPanic");

    // ---- C3: a stopped range-over-func seq refuses every splice on its coro ----

    [TestMethod]
    public void ABodyPanicWhileSeqDefersANilFuncSplicesNothingOnTheCoro() =>
        AssertNoSplice(Run(panicframesprobe_package.bodyPanicsWhileSeqDefersNil), "seqWithNilDefer");

    // Go splices a break (seqA.func1 | gopanic | panicmem | sigpanic | seqA); the adapter cannot tell it
    // from a body panic, so it is a stated missing splice.
    [TestMethod]
    public void ABodyBreakWhileSeqDefersANilFuncSplicesNothingOnTheCoro() =>
        AssertNoSplice(Run(panicframesprobe_package.bodyBreaksWhileSeqDefersNil), "seqWithNilDefer");

    // ---- item 3: the nil func after a completed recovery lacks Go's runtime.deferreturn ----

    [TestMethod]
    public void ANilDeferredFuncAfterACompletedRecoverySplicesNothing() =>
        AssertNoSplice(Run(panicframesprobe_package.nilAfterRecovery), "nilAfterRecovery");

    // The control: a LATER panic running when the nil func faults stays spliced (DNILAR).
    [TestMethod]
    public void ANilDeferredFuncWhileALaterPanicRunsIsSpliced() =>
        CollectionAssert.AreEqual(
            new[] { "runtime.gopanic", "runtime.panicmem", "runtime.sigpanic", "runtime.gopanic", Pkg + "gDeferPanics", Pkg + "nilWhileALaterPanicRuns" },
            Below(Run(panicframesprobe_package.nilWhileALaterPanicRuns), "nilWhileALaterPanicRuns"));
}
