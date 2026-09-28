using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

/// <summary>
/// Guards the frames runtime.Callers reports from inside a panic's deferred call (option B of
/// docs/phase4/DESIGN-panic-stack-frames.md, with P2's four changes). Go runs deferred calls ON the
/// panicking stack, so its unwinder finds runtime.gopanic, the fault frames and the panic site between
/// the deferred call and the deferring function. The expectations are Go's own, measured by P2 on
/// go1.24.13 (the design's §1): the Go test each shape mirrors is named on its probe.
/// </summary>
/// <remarks>
/// Each arm reads the frames strictly BELOW the deferred call that took the Callers (frames[2]: Callers,
/// the probe's callersHere, then the deferred call) down to the deferring function, inclusive. Red at
/// the base: the walk reports the live stack only, so every panic shape reads just the owner.
/// </remarks>
[TestClass]
public class PanicFramesTests
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

    private static void AssertBelow(List<string> names, string owner, params string[] expected)
    {
        List<string> below = Below(names, owner);
        CollectionAssert.AreEqual(expected, below, $"got {string.Join(" | ", below)}; whole stack {string.Join(" | ", names)}");
    }

    [TestMethod]
    public void APanicsDeferredCallSeesGopanicAndThePanickingFrames() =>
        AssertBelow(panicframesprobe_package.onGoroutine(panicframesprobe_package.plainPanic), "plainPanic",
            "runtime.gopanic", Pkg + "f3", Pkg + "f2", Pkg + "f1", Pkg + "plainPanic");

    [TestMethod]
    public void ADoublePanicSeesBothGopanics()
    {
        List<string> names = panicframesprobe_package.onGoroutine(panicframesprobe_package.doublePanic);
        List<string> below = Below(names, "doublePanic");

        Assert.AreEqual(4, below.Count, $"got {string.Join(" | ", below)}");
        Assert.AreEqual("runtime.gopanic", below[0]);
        StringAssert.StartsWith(below[1], Pkg + "doublePanic.func", "panic 2's site: the deferred call that raised it");
        Assert.AreEqual("runtime.gopanic", below[2]);
        Assert.AreEqual(Pkg + "doublePanic", below[3]);
    }

    [TestMethod]
    public void ANilDereferenceAddsPanicmemAndSigpanic() =>
        AssertBelow(panicframesprobe_package.onGoroutine(panicframesprobe_package.nilPointerPanic), "nilPointerPanic",
            "runtime.gopanic", "runtime.panicmem", "runtime.sigpanic", Pkg + "nilPointerPanic");

    [TestMethod]
    public void AnIntegerDivideByZeroAddsPanicdivide() =>
        AssertBelow(panicframesprobe_package.onGoroutine(() => panicframesprobe_package.divZeroPanic(0)), "divZeroPanic",
            "runtime.gopanic", "runtime.panicdivide", Pkg + "divZeroPanic");

    [TestMethod]
    public void ANilDeferredFuncFaultsFromTheDeferringFunctionsExit() =>
        AssertBelow(panicframesprobe_package.onGoroutine(panicframesprobe_package.deferNilFuncPanic), "deferNilFuncPanic",
            "runtime.gopanic", "runtime.panicmem", "runtime.sigpanic", Pkg + "deferNilFuncPanic");

    [TestMethod]
    public void OnceTheRecoveringCallReturnsThePanicIsGone() =>
        AssertBelow(panicframesprobe_package.onGoroutine(panicframesprobe_package.afterRecovery), "afterRecovery", Pkg + "afterRecovery");

    // [P2-1]: the helper's normal-return Run holds a null entry, so the splice pairs with the panicking Run.
    [TestMethod]
    public void ANormalReturnSequenceBetweenTheCallersAndThePanicPairsCorrectly()
    {
        List<string> names = panicframesprobe_package.onGoroutine(panicframesprobe_package.helperDefers);
        List<string> below = Below(names, "helperDefers");

        Assert.AreEqual(Pkg + "helper", below[0], $"got {string.Join(" | ", below)}");
        StringAssert.StartsWith(below[1], Pkg + "helperDefers.func");
        CollectionAssert.AreEqual(new[] { "runtime.gopanic", Pkg + "f3", Pkg + "f2", Pkg + "f1", Pkg + "helperDefers" }, below.Skip(2).ToList(),
            $"got {string.Join(" | ", below)}");
    }

    // A panic a deferred CLOSURE raises (here, replacing panic 1) splices NOTHING: the converter's defer
    // wrappers are indistinguishable from Go closures at run time, and Go elides its deferwrap, so the
    // splice refuses every closure-raised site (a stated residual; COORD's verification of the re-cut).
    // Go: replacedPanic.func2 | gopanic | replacedPanic.func3 | gopanic | replacedPanic.
    [TestMethod]
    public void AReplacingPanicRaisedByADeferredClosureSplicesNothing()
    {
        List<string> below = Below(panicframesprobe_package.onGoroutine(panicframesprobe_package.replacedPanic), "replacedPanic");
        CollectionAssert.AreEqual(new[] { Pkg + "replacedPanic" }, below, $"expected NO splice; got {string.Join(" | ", below)}");
    }

    // [P2-2]: a site that does not end at the deferring function splices NOTHING (a stated residual).
    [TestMethod]
    public void AnOwnerMismatchSplicesNothing() =>
        AssertBelow(panicframesprobe_package.onGoroutine(panicframesprobe_package.ownerMismatch), "ownerMismatch", Pkg + "ownerMismatch");

    // COORD 93c2bdd50b: a wrapper that IS the panic site is kept beneath the fault frames.
    [TestMethod]
    public void AWrapperThatIsThePanicSiteIsKept()
    {
        List<string> below = Below(panicframesprobe_package.onGoroutine(panicframesprobe_package.wrapperIsTheSite), "wrapperIsTheSite");

        CollectionAssert.AreEqual(new[] { "runtime.gopanic", "runtime.panicmem", "runtime.sigpanic", "wrapperprobe.I.M", Pkg + "wrapperIsTheSite" }, below,
            $"got {string.Join(" | ", below)}");
    }

    // ...and a wrapper that only called the panicking method keeps the elision.
    [TestMethod]
    public void AWrapperBelowThePanicSiteIsStillElided()
    {
        List<string> below = Below(panicframesprobe_package.onGoroutine(panicframesprobe_package.wrapperBelowTheSite), "wrapperBelowTheSite");

        Assert.AreEqual(3, below.Count, $"got {string.Join(" | ", below)}");
        Assert.AreEqual("runtime.gopanic", below[0]);
        StringAssert.EndsWith(below[1], ".M", "the panicking method");
        Assert.AreEqual(Pkg + "wrapperBelowTheSite", below[2]);
        Assert.IsFalse(below.Contains("wrapperprobe.I.M"), $"the wrapper must stay elided: {string.Join(" | ", below)}");
    }
}
