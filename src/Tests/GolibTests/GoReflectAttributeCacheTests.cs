using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

[TestClass]
public class GoReflectAttributeCacheTests
{
    // GoReflect reads two per-type markers -- [GoType] and [GoLocalName] -- from the reflection
    // bridge's hottest entry points: KindOf (under every reflect.ValueOf / Value.Field / Value.Elem
    // / MakeSlice / rtype.FieldByName), GoTypeName (under every %T and reflect.Type.String()), and
    // TryConvertTo / TryUnwrapWrapperValue (under the whole Value.Set / SetMapIndex / Call /
    // Convert marshalling surface). None of those callers caches its own result, so an uncached
    // read is paid per VALUE rather than per type -- and custom-attribute retrieval materializes
    // fresh attribute instances on every call (measured against live golib at 361 ns and 200 bytes
    // for one probe, which made KindOf of a converted struct cost 430 ns and 200 bytes).
    //
    // Why this is guarded HERE and not in the behavioral suite. The memo changes no behavior at
    // all: the answers are identical, so no Go-vs-C# output can move and no golden can differ.
    // What IS observable at the golib level is the ALLOCATION, because an attribute read allocates
    // and a dictionary hit does not -- so these tests fail outright if a memo is removed, which is
    // exactly the reintroduction they exist to catch. Allocation is measured with
    // GC.GetAllocatedBytesForCurrentThread, which is precise and inherently thread-scoped (the same
    // instrument testing.AllocsPerRun uses), after a warmup long enough to settle tiered JIT.
    //
    // Zero maps exactly, so the paths that can reach zero assert zero. The paths that must allocate
    // to do their job (building a name string, boxing a scalar result) assert a per-call CEILING
    // set well above what they measure and well below what the uncached read cost -- named at each
    // site rather than tuned to the current number.

    private const int Warmup = 20_000;
    private const int Ops = 20_000;

    // Stands in for a converted `<pkg>_package` static class, which is what GoReflect's
    // package-qualification looks for when it builds a Go type name.
    private static class cachetest_package
    {
        [GoType("num:float64")]
        public readonly struct Celsius(double value)
        {
            private readonly double m_value = value;
            public override string ToString() => m_value.ToString();
        }

        // A plain converted struct: [GoType] with an EMPTY definition, which is the shape every
        // `type T struct{...}` emits and by far the most common thing KindOf classifies.
        [GoType]
        public struct Point
        {
            public nint X;
            public nint Y;
        }

        // A lifted function-local type, as the converter emits one: the stamp carries the BARE Go
        // name and GoQualifiedName prefixes the package (`[GoLocalName("span")]` on
        // `FieldsFunc_span` in bytes renders `bytes.span`). Only its NAME is under test, so it
        // needs no fields.
        [GoType("dyn")]
        [GoLocalName("Person")]
        public struct Lifted;
    }

    // Site 1, the marker-PRESENT path: KindOf of a plain converted struct reads [GoType], finds an
    // empty definition, and falls through to Struct. It has no reason to allocate once the read is
    // memoized -- and 200 bytes per call if it is not.
    [TestMethod]
    public void KindOfAConvertedStructAllocatesNothing()
    {
        Assert.AreEqual(0L, AllocatedPerOp(static () => GoReflect.KindOf(typeof(cachetest_package.Point))),
            "KindOf of a converted struct must not re-read [GoType] per call");
    }

    // Site 1, the marker-ABSENT path: the [GoType] probe sits ABOVE KindOf's container checks, so
    // every slice/map/chan/pointer classification pays it too. A zero-attribute read still
    // allocates (measured 120 bytes for this case), so a missing memo shows up here as well.
    [TestMethod]
    public void KindOfAGolibContainerAllocatesNothing()
    {
        Assert.AreEqual(GoReflect.Slice, GoReflect.KindOf(typeof(slice<nint>)));
        Assert.AreEqual(0L, AllocatedPerOp(static () => GoReflect.KindOf(typeof(slice<nint>))),
            "KindOf of a golib container must not re-read [GoType] per call");
    }

    // Site 3b: TryUnwrapWrapperValue rejects a non-wrapper on the [GoType] definition alone, so it
    // returns false without touching the value -- allocation-free with the memo, 200 bytes without.
    [TestMethod]
    public void RejectingANonWrapperAllocatesNothing()
    {
        object boxed = new cachetest_package.Point { X = 1, Y = 2 };

        Assert.IsFalse(GoReflect.TryUnwrapWrapperValue(boxed, out object _));
        Assert.AreEqual(0L, AllocatedPerOp(() => GoReflect.TryUnwrapWrapperValue(boxed, out object _) ? 1 : 0),
            "the wrapper rejection must not re-read [GoType] per call");
    }

    // Site 2: GoTypeName has to build its name string, so it allocates by construction. What must
    // NOT scale with it is the [GoLocalName] read, so compare a type that carries the stamp against
    // one that does not: with the memo the two differ only by the length of the name each returns
    // (measured 96 vs 80 bytes); without it, materializing the attribute puts the stamped type
    // ~240 bytes per call clear of the other.
    [TestMethod]
    public void ALiftedTypesLocalNameStampIsNotReReadPerCall()
    {
        Assert.AreEqual("cachetest.Person", GoReflect.GoTypeName(typeof(cachetest_package.Lifted)));

        long stamped = AllocatedPerOp(static () => GoReflect.GoTypeName(typeof(cachetest_package.Lifted)).Length);
        long plain = AllocatedPerOp(static () => GoReflect.GoTypeName(typeof(cachetest_package.Point)).Length);

        Assert.IsTrue(stamped - plain < 64,
            $"a [GoLocalName] type must not cost an attribute read per call: stamped {stamped} B/op vs plain {plain} B/op");
    }

    // Site 3a: TryConvertTo boxes both the underlying value it converts and the wrapper it
    // constructs, so it allocates by construction (measured 112 bytes per call). The ceiling is set
    // below the 448 bytes the same call cost when every invocation re-read [GoType] AND re-scanned
    // the wrapper's constructors -- wrapperConstructorOf is memoized whole for that second reason.
    [TestMethod]
    public void ConstructingANamedWrapperDoesNotRescanItsConstructors()
    {
        Assert.IsTrue(GoReflect.TryConvertTo(36.6, typeof(cachetest_package.Celsius), out object converted));
        Assert.IsInstanceOfType(converted, typeof(cachetest_package.Celsius));

        long allocated = AllocatedPerOp(static () =>
            GoReflect.TryConvertTo(36.6, typeof(cachetest_package.Celsius), out object result) && result is not null ? 1 : 0);

        Assert.IsTrue(allocated < 250,
            $"constructing a named wrapper must not re-read [GoType] or re-scan constructors per call: {allocated} B/op");
    }

    // Bytes allocated per call on THIS thread, after a warmup long enough for tiered JIT to settle
    // and for every one-time cache inside golib to fill -- so what is measured is the steady-state
    // per-call cost a real caller pays, not first-call setup.
    private static long AllocatedPerOp(Func<int> op)
    {
        int sink = 0;

        for (int i = 0; i < Warmup; i++)
            sink += op();

        long before = GC.GetAllocatedBytesForCurrentThread();

        for (int i = 0; i < Ops; i++)
            sink += op();

        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        GC.KeepAlive(sink);

        return allocated / Ops;
    }
}
