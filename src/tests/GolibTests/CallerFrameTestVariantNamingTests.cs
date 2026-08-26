using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

// The FUNCTION half of a converted frame's identity — `goFrameName` in runtime's managed_impl.cs —
// derives a Go import path from the emitted namespace + package-class name. That derivation is the
// design's ruled position (DESIGN-position-map.md §8: the FILE half is RECORDED, but "a function
// name IS a property of the package, and Go's own traceback spells it from the package"), so the
// suffix rules the file half retired remain necessary HERE, and they are what this file pins.
//
// Go's own answer, measured against the go1.23.12 toolchain rather than reasoned about:
//
//     internal test (`package callerprobe`      in cp_test.go)  -> callerprobe.TestInternalCallerName
//     external test (`package callerprobe_test` in cpx_test.go) -> callerprobe_test.TestExternalCallerName
//
// The two suffixes are therefore NOT symmetric, which is the whole defect this guard exists for. An
// internal test file is compiled INTO the package under test, so Go names its frames with the
// package's own import path and no suffix at all; an external test file is a genuinely separate
// package and Go keeps the `_test`. The `-tests` pipeline emits the internal variant as
// `<pkg>_internal_test_package` (testConversion.go's `production.Name + "_internal_test" +
// PackageSuffix`), and before the rule existed the derivation carried that converter-invented token
// straight out to the program: `log/slog` asserts `wantFunc = "log/slog.TestCallDepth"` in
// logger_test.go and got `log/slog_internal_test.TestCallDepth`.
//
// WHY THIS TIER. The shape cannot be reached from a behavioral test: a behavioral project is a
// single `package main` compared against `go run`, so it has no `_test`/`_internal_test` variant to
// name. It cannot be reached from the converter's own `go test` either — the rule lives in the C#
// runtime, not in the emission. What it needs is a frame whose declaring type carries the emitted
// shape, which an MSTest tier can simply DECLARE — the same precedent Sha3ReinterpretVectorTests and
// TestExecutionOutputCapTests set for surfaces no other harness can reach. The classes at the foot
// of this file are hand-written stand-ins for the three shapes the converter emits, named exactly as
// it names them.
//
// Failing-first, measured before the fix landed: shape (2) answered
// `slcguard/probe_internal_test.internalTestShapedFrame`, while shapes (1) and (3) already answered
// correctly — so the guard reported exactly the one rule that was missing and nothing else.
namespace GolibTests
{
    [TestClass]
    public class CallerFrameTestVariantNamingTests
    {
        // The package-path-qualified function name runtime reports for the CALLER of this helper.
        // Callers(1, …) skips the helper's own frame, so the answer names the shaped method below.
        // A one-element pc slice is deliberate: Callers fills at most len(pc), so the single entry
        // is the caller's frame and no unfilled tail can reach Frames.Next.
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static string CallerFunctionName()
        {
            slice<uintptr> pcs = new slice<uintptr>(1);

            if (runtime_package.Callers(1, pcs) == 0)
                return string.Empty;

            var frames = runtime_package.CallersFrames(pcs);
            var (frame, _) = frames.Next();

            return frame.Function;
        }

        // (1) A production package class — the shape every converted non-test file compiles to. Go
        //     names such a frame with the bare import path, and this has always worked; it is here
        //     so a regression in the shared derivation cannot hide behind the two variant rules.
        [TestMethod]
        public void ProductionPackageFrameNamesTheImportPath()
        {
            Assert.AreEqual(
                "slcguard/probe.productionShapedFrame",
                go.slcguard.probe_package.productionShapedFrame(),
                "a production package class must name its bare import path");
        }

        // (2) THE DEFECT. An internal test file is compiled into the package under test, so Go names
        //     its frames with the package's OWN import path — the `_internal_test` token is a go2cs
        //     emission detail and naming it is a divergence a Go program can read.
        [TestMethod]
        public void InternalTestFrameNamesThePackageUnderTest()
        {
            Assert.AreEqual(
                "slcguard/probe.internalTestShapedFrame",
                go.slcguard.probe_internal_test_package.internalTestShapedFrame(),
                "an internal test variant must name the package under test, with no suffix — Go " +
                "compiles the file INTO that package (measured: callerprobe.TestInternalCallerName)");
        }

        // (3) The external test package is a genuinely separate Go package and Go KEEPS the `_test`.
        //     Pinned in the same file as (2) because the tempting over-broad fix — strip any trailing
        //     `_test` — breaks this one, and `runtime/debug`'s own TestStack greps a rendered
        //     traceback for `runtime/debug_test.(*T).ptrmethod`.
        [TestMethod]
        public void ExternalTestFrameKeepsTheTestSuffix()
        {
            Assert.AreEqual(
                "slcguard/probe_test.externalTestShapedFrame",
                go.slcguard.probe_test_package.externalTestShapedFrame(),
                "an external test package must KEEP its _test suffix — it is a separate Go package " +
                "(measured: callerprobe_test.TestExternalCallerName)");
        }
    }
}

// The three emitted shapes, declared exactly as the converter spells them: namespace `go.<dir>`,
// class `<pkg>_package` / `<pkg>_internal_test_package` / `<pkg>_test_package`. runtime's
// `isConvertedGoFrame` accepts a frame on these terms alone (namespace under `go`, top-level type
// name ending in `_package`), which is precisely why the naming rule has to be right about them.
namespace go.slcguard
{
    public static class probe_package
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string productionShapedFrame() =>
            GolibTests.CallerFrameTestVariantNamingTests.CallerFunctionName();
    }

    public static class probe_internal_test_package
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string internalTestShapedFrame() =>
            GolibTests.CallerFrameTestVariantNamingTests.CallerFunctionName();
    }

    public static class probe_test_package
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string externalTestShapedFrame() =>
            GolibTests.CallerFrameTestVariantNamingTests.CallerFunctionName();
    }
}
