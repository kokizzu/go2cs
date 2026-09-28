using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

/// <summary>
/// The shapes COORD's review of the first panic-frames cut named (review-r-panic-frames-2026-09-27.md),
/// with Go's go1.24.13 answer on each probe. Two kinds of arm: a shape Go's frames are spliced for
/// asserts Go's list; a shape whose site does not provably reach the walked Run asserts NO splice (only
/// the deferring function below the deferred call), because a missing splice is a stated divergence and
/// a wrong one is a silent lie. Every probe runs on a goroutine of its own (onGoroutine).
/// </summary>
[TestClass]
public class PanicFramesReviewTests
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

    // ---- NO splice: the site does not provably reach this Run (each a stated residual) ----

    [TestMethod]
    public void ShapeA_AReplacingPanicReRaisedThroughAClosureSplicesNothing() =>
        AssertNoSplice(Run(panicframesprobe_package.shapeA), "shapeA");

    [TestMethod]
    public void ShapeB_ANormalReturnPanicReRaisedThroughAClosureSplicesNothing() =>
        AssertNoSplice(Run(panicframesprobe_package.shapeB), "shapeB");

    [TestMethod]
    public void TheCatchersCallerDeferredSplicesNothing() =>
        AssertNoSplice(Run(panicframesprobe_package.deferACallerOfTheCatcher), "deferACallerOfTheCatcher");

    [TestMethod]
    public void ANilDeferredFuncFaultingInACalleeSplicesNothing() =>
        AssertNoSplice(Run(panicframesprobe_package.nilDeferPropagated), "nilDeferPropagated");

    [TestMethod]
    public void ARecursiveActivationIsNotTheOwner() =>
        AssertNoSplice(Run(panicframesprobe_package.recursion), "rec");

    [TestMethod]
    public void AnUnmodelledRuntimeErrorSplicesNothing() =>
        AssertNoSplice(Run(() => panicframesprobe_package.indexPanic(5)), "indexPanic");

    [TestMethod]
    public void APanicADeferredCallRaisesDuringGoexitSplicesNothing() =>
        AssertNoSplice(panicframesprobe_package.goexitShape(), "panicDuringGoexit");

    // ---- Go's list ----

    [TestMethod]
    public void TheDeferredDelegateThatCaughtFirstIsSplicedWithThePanicBeneath() =>
        CollectionAssert.AreEqual(
            new[] { "runtime.gopanic", Pkg + "gDeferPanics", "runtime.gopanic", Pkg + "deferTheCatcher" },
            Below(Run(panicframesprobe_package.deferTheCatcher), "deferTheCatcher"));

    [TestMethod]
    public void ADeferredCallPanickingOnANormalReturnIsSpliced()
    {
        List<string> below = Below(Run(panicframesprobe_package.normalReturnPanic), "normalReturnPanic");

        Assert.AreEqual(3, below.Count, string.Join(" | ", below));
        Assert.AreEqual("runtime.gopanic", below[0]);
        StringAssert.StartsWith(below[1], Pkg + "normalReturnPanic.func");
    }

    [TestMethod]
    public void ADeferredCallPanickingAfterACompletedRecoveryIsSpliced()
    {
        List<string> below = Below(Run(panicframesprobe_package.panicAfterRecovery), "panicAfterRecovery");

        Assert.AreEqual(3, below.Count, string.Join(" | ", below));
        Assert.AreEqual("runtime.gopanic", below[0]);
        StringAssert.StartsWith(below[1], Pkg + "panicAfterRecovery.func");
    }

    [TestMethod]
    public void DeferPanicArgIsSpliced()
    {
        List<string> below = Below(Run(panicframesprobe_package.deferPanicArg), "deferPanicArg");

        Assert.AreEqual(3, below.Count, string.Join(" | ", below));
        Assert.AreEqual("runtime.gopanic", below[0]);
        StringAssert.StartsWith(below[1], Pkg + "deferPanicArg.func");
    }

    [TestMethod]
    public void ANilDeferredFuncFaultingWhileAPanicRunsKeepsTheOlderGopanic() =>
        CollectionAssert.AreEqual(
            new[] { "runtime.gopanic", "runtime.panicmem", "runtime.sigpanic", "runtime.gopanic", Pkg + "nilDeferWhilePanicking" },
            Below(Run(panicframesprobe_package.nilDeferWhilePanicking), "nilDeferWhilePanicking"));

    [TestMethod]
    public void ATwoLinkChainSplicesBothRaisersInOrder()
    {
        List<string> below = Below(Run(panicframesprobe_package.twoLinkChain), "twoLinkChain");

        CollectionAssert.AreEqual(
            new[] { "runtime.gopanic", panicframesprobe_package.p3Thrower, "runtime.gopanic", panicframesprobe_package.p2Thrower, "runtime.gopanic", Pkg + "twoLinkChain" },
            below, string.Join(" | ", below));
        Assert.AreNotEqual(panicframesprobe_package.p2Thrower, panicframesprobe_package.p3Thrower);
    }

    [TestMethod]
    public void RecoverThenPanicInOneDeferredCallKeepsTheOlderGopanic()
    {
        List<string> below = Below(Run(panicframesprobe_package.recoverThenPanic), "recoverThenPanic");

        Assert.AreEqual(4, below.Count, string.Join(" | ", below));
        Assert.AreEqual("runtime.gopanic", below[0]);
        StringAssert.StartsWith(below[1], Pkg + "recoverThenPanic.func");
        Assert.AreEqual("runtime.gopanic", below[2]);
    }

    // ---- [P2-3]: skip, capacity, and the spliced frames' lines ----

    [TestMethod]
    public void CallerOneInsideAPanicsDeferredCallIsGopanic()
    {
        (string file, long line) = panicframesprobe_package.onGoroutine(panicframesprobe_package.callerOneInDefer);

        StringAssert.EndsWith(file, "runtime/panic.go");
        Assert.AreEqual(792L, line);
    }

    [TestMethod]
    public void ASmallBufferFillsFromTheTopAndSkipCountsGopanic()
    {
        (List<string> three, List<string> skipTwo) = panicframesprobe_package.onGoroutine(panicframesprobe_package.capacityAndSkip);

        Assert.AreEqual(3, three.Count, string.Join(" | ", three));
        Assert.AreEqual("runtime.Callers", three[0]);
        Assert.AreEqual("runtime.gopanic", three[2], string.Join(" | ", three));

        Assert.AreEqual("runtime.gopanic", skipTwo[0], string.Join(" | ", skipTwo));
        Assert.AreEqual(Pkg + "f3", skipTwo[1], string.Join(" | ", skipTwo));
    }

    [TestMethod]
    public void TheSplicedSiteFramesCarryTheirGoLines()
    {
        List<(string function, long line)> frames = panicframesprobe_package.onGoroutine(panicframesprobe_package.plainPanicWithLines);

        (string function, long line) Frame(string name) => frames.First(frame => frame.function == Pkg + name);

        Assert.AreEqual((long)panicframesprobe_package.f3Line, Frame("f3").line, "f3: its throw line");
        Assert.AreEqual((long)panicframesprobe_package.f2Line, Frame("f2").line, "f2: its call of f3");
        Assert.AreEqual((long)panicframesprobe_package.f1Line, Frame("f1").line, "f1: its call of f2");
    }
}
