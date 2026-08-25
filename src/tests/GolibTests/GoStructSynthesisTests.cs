// GoStructSynthesisTests.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using go.golib;

namespace GolibTests;

/// <summary>
/// Guards <see cref="GoStructSynthesis"/> — the CLR value type minted per synthesized Go struct so
/// <c>reflect.StructOf</c> can hand back a type nothing declared.
/// </summary>
/// <remarks>
/// <para>
/// These sit BELOW the <c>ReflectStructOf</c> behavioral test rather than beside it, and the split is
/// deliberate. That test compares the whole answer against <c>go run</c>, which is the only gate that
/// can find a wrong answer; these rows pin the three MECHANISMS an adversarial review measured as
/// getting silently mis-built, each of which is invisible from the outside once it goes wrong:
/// </para>
/// <list type="number">
/// <item>the emitted parameterless CONSTRUCTOR, without which every synthesized array field reports
/// length ZERO — legally, 0 being a real Go length — because <c>GoReflect.FieldArrayDims</c> measures
/// an array field off a cached ZERO INSTANCE, not off the <c>[GoArrayDims]</c> stamp;</item>
/// <item>the shape KEY, which cannot be built from <c>System.Type</c>s: <c>[1]int</c> and <c>[2]int</c>
/// are one <c>array&lt;long&gt;</c>, so interning on the managed types alone silently merges them;</item>
/// <item>the <c>ʗ</c> field-name prefix for an EMBEDDED field, which is what
/// <c>collectGoFields</c> reads embeddedness from — and which a <c>Type.String()</c>-based assertion
/// cannot see, since an embedded field and a same-named regular field render identically.</item>
/// </list>
/// <para>
/// The dims key each row passes is written by hand here — <c>reflect.StructOf</c> supplies the real
/// one from <c>abi.descriptorDimsKey</c>, which golib sits below and cannot call. What is under test
/// is that the key SEPARATES, not how the separation is rendered.
/// </para>
/// </remarks>
[TestClass]
public class GoStructSynthesisTests
{
    private static GoSynthField Field(string name, Type type, string tag = "", bool embedded = false, nint[]? dims = null, string dimsKey = "") =>
        new(name, type, tag, embedded, dims, null, dimsKey);

    // ---- interning: the contract, not an optimization ----------------------------------------

    [TestMethod]
    public void ASameShapeMintsOneType()
    {
        // encoding/gob keys map[reflect.Type]gobType and enc.sent map[reflect.Type]typeId on the
        // StructOf result, so a fresh type per call makes every recursion a cache miss and every
        // mutually recursive type an infinite regress.
        Type a = GoStructSynthesis.SynthesizeStructType([Field("A", typeof(long)), Field("B", typeof(@string))], "");
        Type b = GoStructSynthesis.SynthesizeStructType([Field("A", typeof(long)), Field("B", typeof(@string))], "");

        Assert.AreSame(a, b, "the same Go struct shape must mint ONE managed type");
    }

    [TestMethod]
    public void AShapeKeySeparatesArrayLengths()
    {
        // THE AM-2 case. array<long> is ONE managed type for [1]int and [2]int alike — the length
        // lives only as descriptor cargo — so a key over field System.Types would intern these two
        // Go structs together and whichever arrived first would answer Field(0).Type.Len() for both.
        Type one = GoStructSynthesis.SynthesizeStructType([Field("F", typeof(array<long>), dims: [1], dimsKey: "1")], "");
        Type two = GoStructSynthesis.SynthesizeStructType([Field("F", typeof(array<long>), dims: [2], dimsKey: "2")], "");

        Assert.AreNotSame(one, two, "[1]int and [2]int are distinct Go types over one managed type");
        Assert.AreEqual(1, (int)GoReflect.GoFields(one)[0].ArrayDims![0]);
        Assert.AreEqual(2, (int)GoReflect.GoFields(two)[0].ArrayDims![0]);
    }

    [TestMethod]
    public void AShapeKeySeparatesTagsNamesAndOrder()
    {
        Type plain = GoStructSynthesis.SynthesizeStructType([Field("A", typeof(long))], "");
        Type tagged = GoStructSynthesis.SynthesizeStructType([Field("A", typeof(long), tag: "json:\"a\"")], "");
        Type renamed = GoStructSynthesis.SynthesizeStructType([Field("Z", typeof(long))], "");
        Type embedded = GoStructSynthesis.SynthesizeStructType([Field("A", typeof(long), embedded: true)], "");

        Assert.AreNotSame(plain, tagged, "a struct tag is part of Go struct identity");
        Assert.AreNotSame(plain, renamed, "a field name is part of Go struct identity");
        Assert.AreNotSame(plain, embedded, "embeddedness is part of Go struct identity");
        Assert.AreEqual("json:\"a\"", GoReflect.GoFields(tagged)[0].Tag);
        Assert.AreEqual("", GoReflect.GoFields(plain)[0].Tag);
    }

    [TestMethod]
    public void AConcurrentMintOfOneShapeIsSafe()
    {
        // THE AM-4 case, and the reason the intern is a lock rather than a ConcurrentDictionary
        // factory: GetOrAdd runs its factory CONCURRENTLY on racing threads and discards the losers'
        // work — but the work here is DefineType, and a duplicate type name THROWS rather than being
        // discarded (measured on a bare probe: 3 of 4 threads failed).
        GoSynthField[] shape = [Field("Raced", typeof(long)), Field("Twice", typeof(@string))];
        Type[] results = new Type[8];

        Parallel.For(0, results.Length, i =>
            results[i] = GoStructSynthesis.SynthesizeStructType(shape, ""));

        foreach (Type t in results)
            Assert.AreSame(results[0], t, "every racing mint of one shape must yield the SAME type");
    }

    // ---- dims: the route that is easy to get backwards -----------------------------------------

    [TestMethod]
    public void AnArrayFieldReportsItsGoLengthFromTheZeroInstance()
    {
        // THE AM-1 case. In converted code the converter emits an array field's length as a field
        // INITIALIZER (`= new(3)`) that the generated parameterless constructor runs, and
        // FieldArrayDims measures it off Activator.CreateInstance(declaringType). A TypeBuilder
        // struct has no field initializers, so without the emitted constructor this reports 0 —
        // silently, and invisibly to encoding/gob's depth-limit test, which asserts on the DECODER's
        // error over the wire type graph and discards the encoder's.
        Type t = GoStructSynthesis.SynthesizeStructType(
        [
            Field("N", typeof(long)),
            Field("Arr", typeof(array<byte>), dims: [3], dimsKey: "3"),
            Field("Nested", typeof(array<array<byte>>), dims: [2, 3], dimsKey: "2,3")
        ], "");

        GoReflect.GoFieldInfo[] fields = GoReflect.GoFields(t);

        CollectionAssert.AreEqual(new nint[] { 3 }, fields[1].ArrayDims, "a synthesized [3]byte field must report length 3");
        CollectionAssert.AreEqual(new nint[] { 2, 3 }, fields[2].ArrayDims, "nested dims must survive whole");

        // The dims are load-bearing for LAYOUT too, not only for Len(): structLayoutOf sizes an
        // array field from exactly this vector, so a dims-losing mint also mis-sizes the struct.
        CollectionAssert.AreEqual(new nint[] { 0, 8, 11 }, GoReflect.GoFieldOffsets(t), "Go amd64 offsets");

        // 8 + 3 + 6 = 17, rounded UP to the struct's own alignment of 8 — Go's rule, and the exact
        // number `go run` prints for this shape in the ReflectStructOf behavioral guard. A mint that
        // lost the dims would answer 8 here and every assertion above would still be checkable only
        // against itself, which is why the guard's authority is the Go comparison, not this row.
        Assert.AreEqual(24, (int)GoReflect.GoSizeOf(t), "Go's own size over int64 + [3]byte + [2][3]byte");
    }

    [TestMethod]
    public void AnArrayFieldsZeroValueHasRealDistinctStorage()
    {
        // The seeded value must be built FRESH per instance: array<T> wraps a backing store, so a
        // shared prototype would alias storage across every value of the synthesized type.
        Type t = GoStructSynthesis.SynthesizeStructType([Field("Arr", typeof(array<byte>), dims: [4], dimsKey: "4")], "");
        FieldInfo f = t.GetFields(BindingFlags.Instance | BindingFlags.Public)[0];

        object first = Activator.CreateInstance(t)!;
        object second = Activator.CreateInstance(t)!;

        array<byte> a = (array<byte>)f.GetValue(first)!;
        array<byte> b = (array<byte>)f.GetValue(second)!;

        Assert.AreEqual(4, (int)a.Length, "the emitted constructor must size the backing store");
        Assert.AreEqual(4, (int)b.Length);

        a[0] = 7;
        Assert.AreEqual(0, (int)b[0], "two instances must not share one backing store");
    }

    [TestMethod]
    public void APointerHopDimsRideTheStampNotTheZeroInstance()
    {
        // The other half of AM-1, and the reason both routes are emitted: a zero instance holds a
        // NIL pointer, which has no pointee to measure, so a *[3]float64 field's dims have to be in
        // the metadata — exactly where the converter puts them (fieldCargoDims).
        Type t = GoStructSynthesis.SynthesizeStructType(
            [Field("P", typeof(ж<array<double>>), dims: [3], dimsKey: "3")], "");

        FieldInfo f = t.GetFields(BindingFlags.Instance | BindingFlags.Public)[0];

        Assert.IsNotNull(f.GetCustomAttribute<GoArrayDimsAttribute>(), "a pointer-hop field must carry the stamp");
        CollectionAssert.AreEqual(new nint[] { 3 }, GoReflect.GoFields(t)[0].ArrayDims);
    }

    // ---- embeddedness: asserted on the FLAG, never on String() ---------------------------------

    [TestMethod]
    public void AnEmbeddedFieldIsRecognizedByItsNamePrefix()
    {
        // THE AM-3 case. collectGoFields decides StructField.Anonymous from the `ʗ` CLR name prefix
        // and from nothing else — no attribute participates — so an embed emitted under its plain
        // name is silently NOT embedded. The assertion is on .Embedded on purpose: an embedded field
        // and a same-named regular field render IDENTICALLY through Type.String(), so a String()
        // check here could not go red on the defect it exists to catch.
        Type t = GoStructSynthesis.SynthesizeStructType(
            [Field("Celsius", typeof(double), embedded: true), Field("Tail", typeof(long))], "");

        GoReflect.GoFieldInfo[] fields = GoReflect.GoFields(t);

        Assert.AreEqual(2, fields.Length);
        Assert.IsTrue(fields[0].Embedded, "the ʗ-prefixed field must project as an EMBEDDED Go field");
        Assert.IsFalse(fields[1].Embedded);
        Assert.AreEqual("Celsius", fields[0].Name, "the Go name is the prefix stripped off");
        Assert.AreEqual("Tail", fields[1].Name);

        // Field ORDER is Go declaration order, which for a TypeBuilder type is DefineField order:
        // reorderToGoDeclarationOrder looks for an ALL-FIELDS constructor, finds only the
        // parameterless one, and keeps metadata order — which is already right.
        CollectionAssert.AreEqual(new[] { "Celsius", "Tail" }, fields.Select(f => f.Name).ToArray());
    }

    // ---- naming, package path, and the residual the pair exposes --------------------------------

    [TestMethod]
    public void ASynthesizedStructIsAnonymousAndRendersStructurally()
    {
        Type t = GoStructSynthesis.SynthesizeStructType(
            [Field("A", typeof(long)), Field("B", typeof(@string), tag: "json:\"b\"")], "");

        Assert.IsFalse(GoReflect.HasGoName(t), "a StructOf result is an UNNAMED Go type, so Name() is \"\"");
        Assert.AreEqual("struct { A int64; B string \"json:\\\"b\\\"\" }", GoReflect.GoTypeName(t));
        Assert.AreEqual(GoReflect.Struct, GoReflect.KindOf(t), "a CLR value type is a Go struct; a class would be a Go POINTER");
    }

    [TestMethod]
    public void AnUnexportedFieldsPackageContainerCarriesTheImportPath()
    {
        // GoPackagePath(t) is GoPackageClassPath(t.DeclaringType), which reads the declaring class's
        // NAMESPACE plus its name with `_package` trimmed. So the container has to be class
        // `gob_package` in namespace `go.encoding` — the obvious `go.encoding.gob.gob_package`
        // spelling is measurably wrong and yields "encoding/gob/gob".
        Type t = GoStructSynthesis.SynthesizeStructType(
            [Field("Shown", typeof(long)), Field("hidden", typeof(long))], "encoding/gob");

        Assert.AreEqual("encoding/gob", GoReflect.GoPackagePath(t));

        Type top = GoStructSynthesis.SynthesizeStructType([Field("Shown", typeof(long))], "main");
        Assert.AreEqual("main", GoReflect.GoPackagePath(top), "a root-level import path nests directly under `go`");

        // ⚠ THE PAIR, RECORDED RATHER THAN CLAIMED TO AGREE. reflect's StructField.PkgPath is
        // `f.Exported ? "" : GoPackagePath(st)` and is exactly right here — the ReflectStructOf
        // behavioral test compares it against `go run` and it matches. rtype.PkgPath() reads the
        // SAME golib call with NO HasGoName gate, so a synthesized struct whose fields forced a
        // container answers a package path for the TYPE where Go answers "" (an unnamed type has no
        // package path). That is pre-existing and corpus-wide — every converter-lifted anonymous
        // struct reports its declaring package the same way — so it is pinned here rather than
        // changed under this arc, which would move every unnamed type in the corpus.
        Assert.IsFalse(GoReflect.HasGoName(t), "the type itself is still unnamed...");
        Assert.AreNotEqual("", GoReflect.GoPackagePath(t), "...yet it reports a package path: the recorded residual");
    }

    [TestMethod]
    public void ANoContainerIsMintedWhenEveryFieldIsExported()
    {
        Type t = GoStructSynthesis.SynthesizeStructType([Field("A", typeof(long))], "");

        Assert.IsNull(t.DeclaringType, "the common case must not pay for a package container");
        Assert.AreEqual("", GoReflect.GoPackagePath(t));
    }

    // ---- asking about a synthesized type must be safe -------------------------------------------

    [TestMethod]
    public void AskingWhetherASynthesizedTypeImplementsAnythingAnswersFalse()
    {
        // A synthesized type is the first CLR type in the system with NO generator-registered
        // extension methods at all, and encoding/gob asks about every type it sees on the way in
        // (GobEncoder, GobDecoder, BinaryMarshaler, BinaryUnmarshaler). "Answers false" is a claim
        // to be measured, not assumed: the runtime adapter tier is fail-soft by design, but this is
        // the case that tier has never met.
        Type t = GoStructSynthesis.SynthesizeStructType([Field("A", typeof(long))], "");

        // GoImplements takes the INTERFACE first, exactly as reflect's rtype.Implements calls it.
        Assert.IsFalse(GoReflect.GoImplements(typeof(IComparable), t), "no method set, so no interface");
        Assert.IsFalse(GoReflect.GoImplements(typeof(IFormattable), t));
        Assert.IsTrue(GoReflect.GoImplements(typeof(object), t), "Go's empty interface is satisfied by everything");
        Assert.AreEqual(0, GoReflect.GoMethodCount(t), "a StructOf result has an empty Go method set — Go's own answer");
    }

    // ---- the `dyn` stamp's side effect, ruled INTENDED and therefore guarded --------------------

    // A converter-lifted anonymous struct, mirrored exactly as the converter emits one: a value type
    // stamped [GoType("dyn")] with no [GoLocalName].
    [GoType("dyn")]
#pragma warning disable CS0649
    private struct DynAB
    {
        public long A;
        public @string B;
    }
#pragma warning restore CS0649

    [TestMethod]
    public void ASynthesizedDynStructConvertsToAndFromALiftedOneOfTheSameShape()
    {
        // The [GoType("dyn")] stamp is taken for a NAMING reason — it is what makes Name() answer ""
        // and String() render structurally — and it arrives with a fourth reader: Type.IsDynamicType()
        // gates builtin.TryTypeAssert's struct-to-struct conversion between unnamed structs of the
        // same shape. Enrolling a synthesized struct there is correct (it IS a Go anonymous struct),
        // but because it is a SIDE EFFECT it gets a row rather than an argument.
        Type synth = GoStructSynthesis.SynthesizeStructType([Field("A", typeof(long)), Field("B", typeof(@string))], "");

        Assert.IsTrue(synth.IsDynamicType(), "a synthesized struct is a Go anonymous struct");

        object synthValue = Activator.CreateInstance(synth)!;
        FieldInfo[] synthFields = synth.GetFields(BindingFlags.Instance | BindingFlags.Public);
        synthFields[0].SetValue(synthValue, 42L);
        synthFields[1].SetValue(synthValue, (@string)"forty-two");

        // `_<T>(out T)` is the emitted spelling of Go's `v, ok := x.(T)`.
        Assert.IsTrue(synthValue._(out DynAB lifted), "synthesized -> lifted");
        Assert.AreEqual(42L, lifted.A);
        Assert.AreEqual("forty-two", lifted.B.ToString());

        // The other direction needs the non-generic entry point: the target type does not exist at
        // compile time, so `x.(T)` cannot be written for it in C# at all.
        Assert.IsTrue(builtin.TryTypeAssert(new DynAB { A = 7, B = (@string)"seven" }, synth, out object? back), "lifted -> synthesized");
        Assert.AreEqual(7L, synthFields[0].GetValue(back));
        Assert.AreEqual("seven", synthFields[1].GetValue(back)!.ToString());
    }

    // ---- the whole projection agrees with itself ------------------------------------------------

    [TestMethod]
    public void AFieldProjectionAndTheRenderedNameReadOneFieldTable()
    {
        // goStructTypeString is built from GoFields, the SAME projection NumField/Field and the
        // value side read, so the name a synthesized type reports and the fields it hands out cannot
        // disagree. This row exists because the mint is the one place they COULD: a field emitted
        // with a name the projection maps differently would show up here and nowhere else.
        Type t = GoStructSynthesis.SynthesizeStructType(
        [
            Field("Celsius", typeof(double), embedded: true),
            Field("_", typeof(long)),
            Field("_", typeof(long)),
            Field("Arr", typeof(array<byte>), dims: [2], dimsKey: "2")
        ], "");

        GoReflect.GoFieldInfo[] fields = GoReflect.GoFields(t);

        CollectionAssert.AreEqual(new[] { "Celsius", "_", "_", "Arr" }, fields.Select(f => f.Name).ToArray(),
            "Go permits repeated BLANK fields in one StructOf; the CLR does not permit two fields of one name, " +
            "so they are emitted `_`, `__` and mapped back exactly as the converter's own blanks are");
        Assert.AreEqual(4, GoReflect.GoFieldOffsets(t)!.Length, "the offset walk pairs with this projection BY INDEX");
    }
}
