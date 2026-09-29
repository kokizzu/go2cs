using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using go.golib;
using Δruntime = go.runtime_package;

namespace GolibTests;

// Guards golib's panicCheck1 (golib/runtime/RuntimePanicCheck.cs): a bounds or shift panic raised by
// code IN package runtime is Go's fatal error, while the same panic raised anywhere else stays an
// ordinary, recoverable panic.
//
// WHAT THIS FILE CAN AND CANNOT ASSERT. A HIT ends in FatalReport.Fatal, which exits the process, so
// the HIT's CONSEQUENCE is runtime's own TestRuntimePanic child (crash_test.go), never an arm here.
// What is here is the PREDICATE, driven through a real frame on each marker class (a HIT, the nested
// HIT and the excluded package runtime_test), and the MISS path end to end: every routed factory,
// called from a non-runtime frame, still hands back its panic and returns.
//
// The marker classes are declared in RuntimePanicCheckFixtures.cs, in THIS assembly: the predicate is structural (a type's full
// name, walked outward through DeclaringType), so a fixture named exactly like the converter's class
// exercises the same test without loading a -tests build.
//
// A8b rides here too: a nil *Func's Entry and FileLine fault as Go's do, and Name() still answers "".
[TestClass]
public class RuntimePanicCheckTests
{
    internal delegate bool RaisedProbe(out string frameName);

    // The predicate is internal to golib, and a reflection Invoke would put CoreLib's invoker frames
    // between it and the caller. A delegate bound to the method adds no reported frame, so the first
    // non-golib frame the walk sees is the fixture method that calls it.
    internal static readonly RaisedProbe s_raised = (RaisedProbe)Delegate.CreateDelegate(typeof(RaisedProbe),
        typeof(FatalReport).Assembly.GetType("go.golib.RuntimePanicCheck", throwOnError: true)!
            .GetMethod("RaisedInRuntimePackage", BindingFlags.Static | BindingFlags.NonPublic)!);

    private static readonly MethodInfo s_isRuntimePackageType =
        typeof(FatalReport).Assembly.GetType("go.golib.RuntimePanicCheck", throwOnError: true)!
            .GetMethod("IsRuntimePackageType", BindingFlags.Static | BindingFlags.NonPublic)!;

    private static bool IsRuntimePackageType(Type type) => (bool)s_isRuntimePackageType.Invoke(null, [type])!;

    [TestMethod]
    public void TheMarkerIsPackageRuntimeItsInternalTestBridgeOrANestedType()
    {
        Assert.IsTrue(IsRuntimePackageType(typeof(Δruntime)), "package runtime's class is the marker");
        Assert.IsTrue(IsRuntimePackageType(typeof(Δruntime.Func)), "a type nested in package runtime's class is runtime");
        Assert.IsTrue(IsRuntimePackageType(typeof(runtime_internal_test_package)), "package runtime's own _test.go files are runtime.*");
        Assert.IsTrue(IsRuntimePackageType(typeof(runtime_internal_test_package.Nested)), "nested in the internal-test bridge is runtime");

        Assert.IsFalse(IsRuntimePackageType(typeof(runtime_test_package)), "package runtime_test is not runtime.*");
        Assert.IsFalse(IsRuntimePackageType(typeof(RuntimePanicCheckTests)), "a non-runtime class is not the marker");
    }

    [TestMethod]
    public void TheWalkNamesTheFirstFrameOutsideGolib()
    {
        Assert.IsTrue(runtime_internal_test_package.Raise(out string frame), "a frame on the internal-test bridge must HIT");
        Assert.AreEqual("go.runtime_internal_test_package.Raise", frame);

        Assert.IsTrue(runtime_internal_test_package.Nested.Raise(out frame), "a frame on a nested type must HIT");
        Assert.AreEqual("go.runtime_internal_test_package+Nested.Raise", frame);

        Assert.IsFalse(runtime_test_package.Raise(out frame), "a frame on package runtime_test must MISS");
        Assert.AreEqual("go.runtime_test_package.Raise", frame);

        Assert.IsFalse(RaiseHere(out frame), "a frame outside package runtime must MISS");
        Assert.AreEqual($"{typeof(RuntimePanicCheckTests).FullName}.{nameof(RaiseHere)}", frame);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool RaiseHere(out string frameName) => s_raised(out frameName);

    // THE MISS PATH. Every routed factory, called from a non-runtime frame, must hand its panic back
    // and return: a factory whose check fired here would have exited the test host instead.
    [TestMethod]
    public void EveryRoutedFactoryStaysAPanicOutsidePackageRuntime()
    {
        PanicException[] raised =
        [
            RuntimeErrorPanic.IndexOutOfRange(5L, 3L),
            RuntimeErrorPanic.IndexOutOfRange(5UL, 3L),
            RuntimeErrorPanic.SliceBoundsOutOfRange(0, 5, 5, 3),
            RuntimeErrorPanic.StringSliceBoundsOutOfRange(0, 5, 3),
            RuntimeErrorPanic.ArrayConversionLength(1, 2),
            RuntimeErrorPanic.NegativeShiftAmount()
        ];

        foreach (PanicException panic in raised)
            Assert.IsNotNull(panic, "a factory must return its panic outside package runtime");
    }

    // A8b. Go's (*Func).Entry and FileLine read f.raw() with no nil check, so a nil *Func faults and the
    // program dies naming runtime.(*Func).Entry (TestTracebackRuntimeMethod). Only Name() checks.
    [TestMethod]
    public void ANilFuncFaultsInEntryAndFileLineAndNameAnswersEmpty()
    {
        ж<Δruntime.Func> f = null!;

        PanicException entry = Assert.ThrowsException<PanicException>(() => Δruntime.Entry(f));
        StringAssert.Contains(entry.Message, "invalid memory address or nil pointer dereference");

        PanicException fileLine = Assert.ThrowsException<PanicException>(() => Δruntime.FileLine(f, 0));
        StringAssert.Contains(fileLine.Message, "invalid memory address or nil pointer dereference");

        Assert.AreEqual("", (string)Δruntime.Name(f), "Name() on a nil *Func answers \"\"");
    }
}
