using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

/// <summary>
/// The golib half of the reflection-bridge CLOSURE arc — the facts a Go program cannot reach
/// directly, so the behavioral <c>ReflectBridgeClosure</c> test (which compares the whole surface
/// against <c>go run</c>) cannot own them.
/// </summary>
/// <remarks>
/// Division of labour, the same one <see cref="GoTypeDefinednessTests"/> states: anything a Go
/// program can observe end to end belongs to the behavioral test, which is strictly stronger
/// because Go itself supplies the expected answer. What lives here is the golib API contract
/// underneath — the shapes only golib can construct (a natural-delegate variadic tail, a
/// <c>[GoType]</c> wrapper never emitted by any Go source in the suite) and the ALIASING promise,
/// which is a statement about storage rather than about printed output.
/// </remarks>
[TestClass]
public class GoReflectBridgeClosureTests
{
    // Stands in for a converted `<pkg>_package` static class — the nesting GoReflect's package
    // qualification looks for when it decides a type has a Go name.
    private static class functest_package
    {
        // `type handler func(int) error` WITH a method: a defined func type acquires a managed
        // identity exactly then, and the converter emits it as its own delegate nested here.
        public delegate error handler(nint n);
    }

    // `type definedByte byte` — a one-field wrapper over byte, the element representation the
    // Bytes()/SetBytes() alias is defined over.
    [GoType("num:byte")]
    private readonly struct definedByte
    {
        private readonly byte m_value;

        private definedByte(byte value) => m_value = value;

        public static implicit operator definedByte(byte value) => new(value);

        public static implicit operator byte(definedByte value) => value.m_value;
    }

    // ---- an UNNAMED func type renders structurally; a DEFINED one keeps its name ----------------

    [TestMethod]
    public void AnUnnamedFuncTypeRendersItsGoSignature()
    {
        Assert.AreEqual("func()", GoReflect.GoTypeName(typeof(Action)));
        Assert.AreEqual("func(int)", GoReflect.GoTypeName(typeof(Action<nint>)));
        Assert.AreEqual("func() int", GoReflect.GoTypeName(typeof(Func<nint>)));
        Assert.AreEqual("func(int, string) bool", GoReflect.GoTypeName(typeof(Func<nint, @string, bool>)));

        // Go's multi-return: a ValueTuple result unpacks to one Go result per element, parenthesized.
        Assert.AreEqual("func(int) (bool, int)", GoReflect.GoTypeName(typeof(Func<nint, (bool, nint)>)));

        // A pointer parameter is the shape fmt's own table reads back — `%#v` of a func value
        // printed the CLR delegate family (`Action`1`) until this arm existed.
        Assert.AreEqual("func(*int)", GoReflect.GoTypeName(typeof(Action<ж<nint>>)));
    }

    [TestMethod]
    public void AnUnnamedFuncTypeHasNoGoName()
    {
        // HasGoName must agree with GoTypeName ARM FOR ARM: a type that renders structurally has no
        // name, so reflect.Type.Name() answers "" and the descriptor's TFlagNamed stays clear.
        Assert.IsFalse(GoReflect.HasGoName(typeof(Action)));
        Assert.IsFalse(GoReflect.HasGoName(typeof(Func<nint, error>)));

        // A DEFINED func type is a delegate the converter declared inside a `<pkg>_package` class,
        // and it keeps its name — the distinction the whole arm turns on.
        Assert.IsTrue(GoReflect.HasGoName(typeof(functest_package.handler)));
        Assert.AreEqual("functest.handler", GoReflect.GoTypeName(typeof(functest_package.handler)));
    }

    [TestMethod]
    public void AVariadicTailIsRecognizedByShapeNotByDelegateFamilyName()
    {
        // The golib variadic families carry a name marker, and that is what the shape test used to
        // rely on. A declared `func(string, ...int)` used as a method group in an `any` position
        // instead acquires C#'s NATURAL delegate type, whose name carries no marker at all — so the
        // name test called it non-variadic and In(1) handed back a raw Span<int>, which rendered as
        // `func(string, Span\`1)`. A Span<T> parameter cannot arise any other way in converted code.
        Assert.IsTrue(GoReflect.TryFuncShape(typeof(SpanTail), out Type[]? ins, out Type[]? outs, out bool isVariadic));
        Assert.IsTrue(isVariadic);
        Assert.AreEqual(typeof(slice<nint>), ins![1]);
        Assert.AreEqual(0, outs!.Length);
        Assert.AreEqual("func(string, ...int)", GoReflect.GoTypeName(typeof(SpanTail)));

        // The non-variadic control: no Span tail, no variadic claim.
        Assert.IsTrue(GoReflect.TryFuncShape(typeof(Action<@string, nint>), out _, out _, out bool plain));
        Assert.IsFalse(plain);
    }

    private delegate void SpanTail(@string s, params Span<nint> rest);

    // ---- Value.Bytes / SetBytes ALIAS, they never copy -------------------------------------------

    [TestMethod]
    public void AByteSliceViewOverADefinedElementAliasesItsStorage()
    {
        slice<definedByte> source = new(new definedByte[] { (byte)'h', (byte)'i', (byte)'!' });

        Assert.IsTrue(GoReflect.TryByteSliceView(source, out slice<byte> view));
        Assert.AreEqual(3, (int)view.Length);
        Assert.AreEqual((byte)'h', view[0]);

        // The whole point: a WRITE through the view lands in the source's storage. A copy would pass
        // every read-only consumer and silently drop this.
        view[2] = (byte)'?';
        Assert.AreEqual((byte)'?', (byte)source[2]);

        // ...and back the other way, which is what a write through the SOURCE must show.
        source[0] = (byte)'H';
        Assert.AreEqual((byte)'H', view[0]);
    }

    [TestMethod]
    public void AByteSliceViewCarriesTheWindowAndTheNilSlice()
    {
        slice<definedByte> source = new(new definedByte[] { 1, 2, 3, 4, 5 });
        slice<definedByte> window = source.slice(1, 4);

        Assert.IsTrue(GoReflect.TryByteSliceView(window, out slice<byte> view));
        Assert.AreEqual(3, (int)view.Length);
        Assert.AreEqual(4, (int)view.Capacity);
        Assert.AreEqual((byte)2, view[0]);

        // The nil slice re-spells as the nil slice — Go's own header re-typing carries the nil data
        // pointer across the same way.
        Assert.IsTrue(GoReflect.TryByteSliceView(default(slice<definedByte>), out slice<byte> nilView));
        Assert.AreEqual(0, (int)nilView.Length);
    }

    [TestMethod]
    public void AByteSliceViewIsRefusedForANonByteElement()
    {
        // Go panics for a non-byte slice, and the caller owns that message — so the probe must say
        // no rather than pun something wider. `nint` is the case that would corrupt memory outright.
        Assert.IsFalse(GoReflect.TryByteSliceView(new slice<nint>(new nint[] { 1, 2 }), out _));
        Assert.IsFalse(GoReflect.TryByteSliceView(new map<@string, nint>(), out _));
        Assert.IsFalse(GoReflect.TryByteSliceView(null, out _));
    }

    [TestMethod]
    public void AByteSliceStoresBackUnderTheSlotsOwnElementName()
    {
        slice<byte> bytes = new(new byte[] { (byte)'a', (byte)'b' });

        Assert.IsTrue(GoReflect.TryByteSliceAs(typeof(slice<definedByte>), bytes, out object? stored));
        Assert.IsInstanceOfType(stored, typeof(slice<definedByte>));

        // The store is an alias too (Go's SetBytes assigns the slice HEADER), so the two views are
        // one slice: a write through either is visible in the other.
        slice<definedByte> typed = (slice<definedByte>)stored!;
        typed[0] = (byte)'z';
        Assert.AreEqual((byte)'z', bytes[0]);

        // A plain []byte slot passes straight through, and a non-byte slot is refused.
        Assert.IsTrue(GoReflect.TryByteSliceAs(typeof(slice<byte>), bytes, out object? plain));
        Assert.IsInstanceOfType(plain, typeof(slice<byte>));
        Assert.IsFalse(GoReflect.TryByteSliceAs(typeof(slice<nint>), bytes, out _));
    }

    // ---- new(T) is Go's ZERO value ----------------------------------------------------------------

    [TestMethod]
    public void NewOfAContainerKindIsTheNilContainerNotAnEmptyOne()
    {
        // golib's container structs declare a parameterless constructor that ALLOCATES, and
        // Activator.CreateInstance<T> honors it — so `new([]T)`/`new(map[K]V)`/`new(chan T)` used to
        // point at a non-nil EMPTY container where Go points at nil. The two zero-fabrication paths
        // must agree, so each is asserted against ZeroValueOf, the rule reflect.Zero/New already use.
        Assert.IsTrue(@new<slice<nint>>().Value == nil);
        Assert.IsTrue(@new<map<@string, nint>>().Value == nil);
        Assert.IsTrue(@new<channel<nint>>().Value == nil);

        Assert.AreEqual(GoReflect.ZeroValueOf(typeof(slice<nint>)), @new<slice<nint>>().Value);
        Assert.AreEqual(GoReflect.ZeroValueOf(typeof(map<@string, nint>)), @new<map<@string, nint>>().Value);
    }

    [TestMethod]
    public void NewOfAStructStillRunsItsFieldInitializers()
    {
        // The other half of the same rule, and the reason the carve-out is by KIND rather than
        // wholesale: running the parameterless constructor is what materializes a struct's
        // fixed-size ARRAY fields from the initializers the converter emits into it.
        withArray value = @new<withArray>().Value;

        Assert.IsNotNull(value.Fixed);
        Assert.AreEqual(4, (int)value.Fixed.Length);
    }

    [GoType]
    private struct withArray
    {
        public array<byte> Fixed = new(4);

        public withArray() { }
    }

    // ---- a pointer descriptor carries its POINTEE's array dims -------------------------------------

    [TestMethod]
    public void APointerToAnArrayReportsThePointeesDimensions()
    {
        // A POINTER carries its pointee's dims unshifted — the rule Elem() already applies when it
        // hands the cargo down. Nothing populated it, so `reflect.TypeOf(new([3]int)).Elem()`
        // described a dimension-LESS array and reflect.New of it allocated a zero-length one.
        ж<array<nint>> box = new(new array<nint>(3));

        CollectionAssert.AreEqual(new nint[] { 3 }, GoReflect.PointeeArrayDims(box));

        // Not a pointer to an array, a nil pointer, and a non-pointer each answer null rather than
        // guessing (the r39d rule).
        Assert.IsNull(GoReflect.PointeeArrayDims(new ж<nint>(1)));
        Assert.IsNull(GoReflect.PointeeArrayDims(new ж<array<nint>>(nil)));
        Assert.IsNull(GoReflect.PointeeArrayDims(new array<nint>(3)));
        Assert.IsNull(GoReflect.PointeeArrayDims(null));
    }

    // ---- a NaN map key is never equal to anything, including itself --------------------------------

    [TestMethod]
    public void AFloatMapKeyFollowsGosIEEERuleRatherThanTheBCLCollectionRule()
    {
        // BCL Double.Equals reports NaN equal to NaN so a NaN stored in a collection can be found
        // again; Go applies `==` unchanged, so each insert makes a NEW entry and none can ever be
        // read back or deleted. fmt's own TestSprintf reads the difference out of `%v` of a map.
        double nan = double.NaN;
        map<double, nint> m = new();

        m[nan] = 1;
        m[nan] = 2;

        Assert.AreEqual(2, (int)len(m));
        Assert.IsFalse(m.ContainsKey(nan));

        // An ordinary float key is unaffected — the comparer changes ONE answer, not the relation.
        m[1.5] = 3;
        Assert.IsTrue(m.ContainsKey(1.5));
        Assert.AreEqual((nint)3, m[1.5]);

        // The same rule at the other three float representations.
        map<float, nint> f32 = new();
        f32[float.NaN] = 1;
        f32[float.NaN] = 2;
        Assert.AreEqual(2, (int)len(f32));

        map<Complex, nint> c128 = new();
        c128[new Complex(double.NaN, 0)] = 1;
        c128[new Complex(double.NaN, 0)] = 2;
        Assert.AreEqual(2, (int)len(c128));
    }
}
