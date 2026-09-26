// GoStructLayoutTests.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

/// <summary>
/// Guards <see cref="GoReflect.GoFieldOffsets"/> — the Go (amd64) byte offset of each projected Go
/// field of a converted struct — and the range-over-any-integer <c>range&lt;T&gt;</c> overload.
/// </summary>
/// <remarks>
/// <para>
/// Both are what <c>unique.makeCloneSeq</c> stands on. Go's <c>(*structType)(unsafe.Pointer(t))</c>
/// prefix downcast has no managed form, so internal/abi's <c>StructType()</c> SYNTHESIZES the
/// specialization and fills each <c>StructField.Offset</c> from this walk; <c>buildArrayCloneSeq</c>
/// then steps a <c>for range atyp.Len</c> loop over a <c>uintptr</c>, which is the generic
/// <c>range&lt;T&gt;</c> overload. Before both landed, six of <c>unique</c>'s nineteen rows died with
/// an <c>IndexOutOfRangeException</c> reading a fabricated <c>StructField[]</c> out of the memory
/// behind a descriptor box, and the array walk had been emitted as a COMMENT.
/// </para>
/// <para>
/// The offsets asserted here are Go's own numbers, not the CLR's: a Go <c>string</c> is 16 bytes
/// where <c>@string</c> is an 8-byte reference, and a Go <c>[2]T</c> is inline where
/// <c>array&lt;T&gt;</c> is one reference. Reading the managed layout instead would silently pass a
/// shape test and produce the wrong clone offsets, so these are the exact shapes
/// <c>unique</c>'s <c>TestMakeCloneSeq</c> exercises, with the offsets Go computes for them.
/// </para>
/// </remarks>
[TestClass]
public class GoStructLayoutTests
{
    // The Go types unique's clone_test.go declares, mirrored as the converter emits them. The
    // fields exist for their LAYOUT and are never read, which is exactly the point.
#pragma warning disable CS0649
    private struct TestStringStruct
    {
        public @string a;
    }

    private struct TestStruct
    {
        public float64 z;
        public @string b;
    }

    private struct TestStringStructArrayStruct
    {
        public array<TestStringStruct> s = new(2);

        public TestStringStructArrayStruct() { }
    }

    private struct TestMixed
    {
        public uint8 a;
        public int64 b;
        public uint8 c;
        public @string d;
    }

    // The shape of a hand-owned runtime shim: a converted Go struct whose field is a managed
    // BACKING OBJECT rather than Go's own representation. sync.Mutex is the real one — Go's
    // `struct { state int32; sema uint32 }` is a SemaphoreSlim gate here — and its class graph
    // is the BCL's, which cycles (SemaphoreSlim -> TaskNode -> TaskNode).
    private struct TestShimStruct
    {
        public SemaphoreSlim? gate;
    }

    // A managed class that refers to itself: the minimal reproduction of the same cycle with no
    // dependency on the BCL's internals staying shaped as they are today.
    private sealed class SelfReferential
    {
        public SelfReferential? next;
        public int64 payload;
    }

    private struct TestSelfReferentialField
    {
        public SelfReferential? node;
    }

    // Go's own legal self-reference: a struct reachable from itself THROUGH A POINTER. Finite in
    // Go and finite here, and the size is an answer rather than a bail-out.
    private struct TestListNode
    {
        public @string value;
        public ж<TestListNode> next;
    }

    private struct TestEmbedInner
    {
        public nint X;
    }

    // `struct { TestEmbedInner }` — go2cs-gen emits a promoted-embed backing field named with the
    // ʗ marker plus the embedded type's name, whose own type IS the embedded type.
    private struct TestEmbeddingStruct
    {
        public TestEmbedInner ʗTestEmbedInner;
    }

    // `struct { TestEmbedInner TestEmbedInner }` — a DECLARED field that happens to be named after
    // its own type. Go's field name for an embed IS the type name, so these two structs project the
    // same field name and the same field type and differ in nothing else a field walk can see.
    private struct TestNamedFieldStruct
    {
        public TestEmbedInner TestEmbedInner;
    }

    // Go's `struct { TestEmbedInner; x, X int }` — the embed declared FIRST — as the converter and
    // go2cs-gen actually emit it: the converter's partial declares x and X, the GENERATOR's partial
    // mints the ʗ backing field, and partial parts concatenate, so CLR metadata order is x, X,
    // ʗTestEmbedInner — the embed LAST, where Go declared it FIRST. The generator's all-fields
    // constructor is the surviving record of the Go declaration order (it is emitted from the
    // declaration syntax), which is what the projection reorders by.
    private struct TestEmbedFirstStruct
    {
        internal nint x;
        public nint X;
        public TestEmbedInner ʗTestEmbedInner;

        internal TestEmbedFirstStruct(TestEmbedInner TestEmbedInner = default, nint x = default, nint X = default)
        {
            ʗTestEmbedInner = TestEmbedInner;
            this.x = x;
            this.X = X;
        }
    }

    // Go's zero-size field shapes, as the converter emits them: `struct{}` is an empty value type
    // and `[N]T` of a zero-size T (or `[0]T`) is an array<T> whose length rides its initializer.
    private struct TestEmpty { }

    // runtime_test.TestTrailingZero's T1: struct { n int32; z [0]byte }.
    private struct TestTrailingZeroArray
    {
        public int32 n;
        public array<byte> z = new(0);

        public TestTrailingZeroArray() { }
    }

    // TestTrailingZero's T2: struct { n int64; z struct{} }.
    private struct TestTrailingEmptyStruct
    {
        public int64 n;
        public TestEmpty z;
    }

    // TestTrailingZero's T3: struct { n byte; z [4]struct{} }.
    private struct TestTrailingEmptyArray
    {
        public byte n;
        public array<TestEmpty> z = new(4);

        public TestTrailingEmptyArray() { }
    }

    // TestTrailingZero's T4: struct { a int32; b int16; c int8; z struct{} }, where alignment
    // already rounds the 7 bytes to 8 and absorbs the trailing byte.
    private struct TestTrailingZeroAbsorbed
    {
        public int32 a;
        public int16 b;
        public int8 c;
        public TestEmpty z;
    }

    // The noCopy shape: a LEADING zero-size field, which Go never pads.
    private struct TestLeadingEmptyStruct
    {
        public TestEmpty noCopy;
        public int64 v;
    }

    // Every field zero-size: the struct is size 0 and stays 0 (Go's rule is `size > 0 && ...`).
    private struct TestAllZeroSize
    {
        public TestEmpty a;
        public array<nint> b = new(0);

        public TestAllZeroSize() { }
    }

    // An array OF a trailing-zero struct: the element's padded size is the stride.
    private struct TestArrayOfTrailingZero
    {
        public array<TestTrailingEmptyStruct> s = new(3);
        public int64 after;

        public TestArrayOfTrailingZero() { }
    }
#pragma warning restore CS0649

    // The projection carries EMBEDDEDNESS because reflect's struct-identity walk compares it, and
    // nothing else in the projection can: `struct{T}` and `struct{T T}` are DIFFERENT Go types that
    // agree on field count, field name, field type, tag and offset. Go's
    // haveIdenticalUnderlyingType ends every field comparison with `tf.Embedded() != vf.Embedded()`
    // for exactly this pair, and reflect's own walk (value_impl.cs haveIdenticalStructShape) reads
    // that answer from here — the SAME projection NumField/Field and the value side read, so the
    // fields a type hands out and the fields its identity is decided by cannot disagree.
    [TestMethod]
    public void EmbeddedField_IsDistinguishableFromADeclaredFieldOfTheSameNameAndType()
    {
        GoReflect.GoFieldInfo[] embedded = GoReflect.GoFields(typeof(TestEmbeddingStruct));
        GoReflect.GoFieldInfo[] declared = GoReflect.GoFields(typeof(TestNamedFieldStruct));

        Assert.AreEqual(1, embedded.Length);
        Assert.AreEqual(1, declared.Length);

        // Everything else a struct-identity walk reads is IDENTICAL between the two...
        Assert.AreEqual("TestEmbedInner", embedded[0].Name, "an embed's Go field name is its type name");
        Assert.AreEqual(embedded[0].Name, declared[0].Name);
        Assert.AreEqual(embedded[0].Type, declared[0].Type);
        Assert.AreEqual(embedded[0].Tag, declared[0].Tag);
        CollectionAssert.AreEqual(
            GoReflect.GoFieldOffsets(typeof(TestEmbeddingStruct)),
            GoReflect.GoFieldOffsets(typeof(TestNamedFieldStruct)));

        // ...so this flag is the ONLY thing that keeps the two Go types apart.
        Assert.IsTrue(embedded[0].Embedded, "a ʗ-marked backing field is Go's embedded field");
        Assert.IsFalse(declared[0].Embedded, "an ordinary declared field is not embedded");
    }

    // Field order is Go DECLARATION order, not CLR metadata order. go2cs-gen mints every embed's
    // backing field in a GENERATED partial, and partial parts concatenate — so a struct whose Go
    // declaration EMBEDS first carries its embed LAST in metadata. Everything indexed
    // (Field(i)/rtype.Field(i)/the offsets table) and everything ordered (fmt's %v walk, json's
    // member order) reads this projection, so the wrong order walked reflectlite's
    // TestCanSetField index chains into an int field (`Field index out of range` one hop later)
    // and printed Talias2's embeds reversed under %#v. The generator's all-fields constructor
    // parameters carry the declaration order, and the projection reorders by them exactly when
    // an embedded field is present (an embed-free struct's metadata order IS declaration order).
    [TestMethod]
    public void FieldOrder_IsGoDeclarationOrder_NotMetadataOrder()
    {
        GoReflect.GoFieldInfo[] fields = GoReflect.GoFields(typeof(TestEmbedFirstStruct));

        Assert.AreEqual(3, fields.Length);
        Assert.AreEqual("TestEmbedInner", fields[0].Name, "the embed Go-declared first projects first");
        Assert.IsTrue(fields[0].Embedded);
        Assert.AreEqual("x", fields[1].Name);
        Assert.AreEqual("X", fields[2].Name);

        // The offsets table pairs with the projection BY INDEX, so it must reorder with it:
        // Go's layout for struct{ TestEmbedInner; x, X int } is [0 8 16].
        nint[]? offsets = GoReflect.GoFieldOffsets(typeof(TestEmbedFirstStruct));

        Assert.IsNotNull(offsets);
        CollectionAssert.AreEqual(new nint[] { 0, 8, 16 }, offsets);
    }

    // The two hops a field's ZERO INSTANCE cannot measure — a nil pointer has no pointee, and a nil
    // map has no entry whose key or element could reveal a length — so the converter stamps them and
    // the projection reads them back. The dims mean what they mean on the descriptor: [GoArrayDims]
    // is what Elem() hands down (a POINTEE's, unshifted at any depth; a map ELEMENT's) and
    // [GoMapKeyDims] is what Key() does. An array field is deliberately NOT stamped — its
    // `= new(N)` initializer already carries the length, through a route that survives a copy —
    // which is why the last assertion here reads a stamp-free field and still gets its dims.
#pragma warning disable CS0649
    private struct TestFieldDimsStruct
    {
        [GoArrayDims(2), GoMapKeyDims(2)]
        public map<array<@string>, array<ж<float64>>> Marr;   // Go: map[[2]string][2]*float64

        [GoArrayDims(3)]
        public ж<ж<ж<array<nint>>>> Deep;                     // Go: ***[3]int

        public map<@string, nint> Plain;                      // Go: map[string]int — nothing to carry

        public array<byte> Sized = new(4);                    // Go: [4]byte — the initializer route

        public TestFieldDimsStruct() { }
    }
#pragma warning restore CS0649

    [TestMethod]
    public void FieldDims_ComeFromTheConverterStamp_WhereNoZeroInstanceCanMeasureThem()
    {
        GoReflect.GoFieldInfo[] fields = GoReflect.GoFields(typeof(TestFieldDimsStruct));

        Assert.AreEqual(4, fields.Length);

        // A map field: each accessor's dims arrive on its own slot, independently.
        Assert.AreEqual("Marr", fields[0].Name);
        CollectionAssert.AreEqual(new nint[] { 2 }, fields[0].ArrayDims, "the map ELEMENT's dims — what Elem() hands down");
        CollectionAssert.AreEqual(new nint[] { 2 }, fields[0].KeyDims, "the map KEY's dims — what Key() hands down");

        // A pointer field: ONE stamp answers at any depth, because the cargo passes down unshifted.
        Assert.AreEqual("Deep", fields[1].Name);
        CollectionAssert.AreEqual(new nint[] { 3 }, fields[1].ArrayDims);
        Assert.IsNull(fields[1].KeyDims);

        // An unstamped field carries null on both slots, which is the state every field was in
        // before the stamp existed and remains the honest answer for a type with no array in it.
        Assert.AreEqual("Plain", fields[2].Name);
        Assert.IsNull(fields[2].ArrayDims);
        Assert.IsNull(fields[2].KeyDims);

        // An ARRAY field is not stamped and does not need to be: the value route still reads its
        // length off the declaring type's zero instance.
        Assert.AreEqual("Sized", fields[3].Name);
        CollectionAssert.AreEqual(new nint[] { 4 }, fields[3].ArrayDims);
        Assert.IsNull(fields[3].KeyDims);
    }

    [TestMethod]
    public void FieldOffsets_SingleString_IsZero()
    {
        CollectionAssert.AreEqual(new nint[] { 0 }, GoReflect.GoFieldOffsets(typeof(TestStringStruct)));
    }

    [TestMethod]
    public void FieldOffsets_Float64ThenString_UsesGoStringWidth()
    {
        // Go: struct { z float64; b string } -> 0, 8. unique's cSeq(8).
        CollectionAssert.AreEqual(new nint[] { 0, 8 }, GoReflect.GoFieldOffsets(typeof(TestStruct)));
        Assert.AreEqual((nint)24, GoReflect.GoSizeOf(typeof(TestStruct)));
    }

    [TestMethod]
    public void FieldOffsets_ArrayOfStringStruct_SizesTheWholeArray()
    {
        // Go: struct { s [2]struct{ a string } } -> the single field at 0, the struct 32 bytes wide.
        CollectionAssert.AreEqual(new nint[] { 0 }, GoReflect.GoFieldOffsets(typeof(TestStringStructArrayStruct)));
        Assert.AreEqual((nint)32, GoReflect.GoSizeOf(typeof(TestStringStructArrayStruct)));
    }

    [TestMethod]
    public void FieldOffsets_ApplyGoAlignmentPadding()
    {
        // Go: struct { a uint8; b int64; c uint8; d string } -> 0, 8, 16, 24 (size 40).
        CollectionAssert.AreEqual(new nint[] { 0, 8, 16, 24 }, GoReflect.GoFieldOffsets(typeof(TestMixed)));
        Assert.AreEqual((nint)40, GoReflect.GoSizeOf(typeof(TestMixed)));
    }

    [TestMethod]
    public void FieldOffsets_AgreeWithGoSizeOf()
    {
        // The offsets and the stamped Size_ come from ONE walk, so the last field must always end
        // inside the reported size. A future split of the two would break this without breaking
        // any single-shape assertion above.
        foreach (Type t in new[] { typeof(TestStringStruct), typeof(TestStruct), typeof(TestStringStructArrayStruct), typeof(TestMixed) })
        {
            nint[]? offsets = GoReflect.GoFieldOffsets(t);
            Assert.IsNotNull(offsets, $"{t.Name} has a knowable layout");
            Assert.IsTrue(offsets[^1] < GoReflect.GoSizeOf(t), $"{t.Name}: last field offset must fall inside the struct");
        }
    }

    [TestMethod]
    public void ManagedReferenceField_IsOneWord_NotAStructToDescendInto()
    {
        // go2cs emits every Go struct as a C# VALUE type, so a managed REFERENCE is never a Go
        // struct — it is an opaque handle, one pointer word wide. Answering Struct here is what
        // sent the layout walk into the CLR's own private fields.
        Assert.AreEqual(GoReflect.Pointer, GoReflect.KindOf(typeof(SemaphoreSlim)));
        Assert.AreEqual(GoReflect.Pointer, GoReflect.KindOf(typeof(SelfReferential)));
        Assert.AreEqual((nint)8, GoReflect.GoSizeOf(typeof(SemaphoreSlim)));

        // Go's sync.Mutex is 8 bytes; so is the shim that stands in for it.
        Assert.AreEqual((nint)8, GoReflect.GoSizeOf(typeof(TestShimStruct)));
        CollectionAssert.AreEqual(new nint[] { 0 }, GoReflect.GoFieldOffsets(typeof(TestShimStruct)));
    }

    [TestMethod]
    public void CyclicManagedReferenceGraph_Terminates()
    {
        // The regression this guards is a STACK OVERFLOW, which no assertion can catch — it takes
        // the whole test host. Reaching the assert at all is the guard; the value proves the walk
        // stopped at the handle rather than merely stopping somewhere.
        //
        // Measured 2026-08-15 on both the pre- and post-embed-change golib: go/types' TestSizeof
        // died in `Named -> Mutex -> SemaphoreSlim -> TaskNode -> TaskNode`, alternating
        // GoSizeOf/tryStructLayout frames until the stack was gone, and took the 44 verdicts
        // alphabetically after it with the process.
        Assert.AreEqual((nint)8, GoReflect.GoSizeOf(typeof(TestSelfReferentialField)));
        Assert.AreEqual((nint)8, GoReflect.GoAlignOf(typeof(TestSelfReferentialField)));
    }

    [TestMethod]
    public void SelfReferentialThroughPointer_IsFiniteAndCorrect()
    {
        // Go: type node struct { value string; next *node } -> 0, 16 (size 24). Legal in Go, and
        // the answer has to be the SIZE, not an "unknown" a cycle guard bailed out with.
        CollectionAssert.AreEqual(new nint[] { 0, 16 }, GoReflect.GoFieldOffsets(typeof(TestListNode)));
        Assert.AreEqual((nint)24, GoReflect.GoSizeOf(typeof(TestListNode)));
        Assert.AreEqual((nint)8, GoReflect.GoAlignOf(typeof(TestListNode)));
    }

    // Go pads a non-zero-size struct whose LAST field is zero-size by one byte before the final
    // alignment, so a pointer to that field cannot point past the object (cmd/compile's
    // CalcStructSize; reflect.StructOf's `size > 0 && lastzero == size`). The converter folds
    // unsafe.Sizeof from go/types, which applies the rule, so these are the numbers converted code
    // already carries; a layout without the byte disagrees with the program's own Sizeof.
    // The expected sizes are runtime_test.TestTrailingZero's.
    [TestMethod]
    public void TrailingZeroSizeField_IsPaddedLikeGo()
    {
        Assert.AreEqual((nint)8, GoReflect.GoSizeOf(typeof(TestTrailingZeroArray)), "struct { n int32; z [0]byte }");
        Assert.AreEqual((nint)16, GoReflect.GoSizeOf(typeof(TestTrailingEmptyStruct)), "struct { n int64; z struct{} }");
        Assert.AreEqual((nint)2, GoReflect.GoSizeOf(typeof(TestTrailingEmptyArray)), "struct { n byte; z [4]struct{} }");

        // The byte is PADDING after the last field, so no offset moves: the zero-size field still
        // starts where the previous field ends.
        CollectionAssert.AreEqual(new nint[] { 0, 4 }, GoReflect.GoFieldOffsets(typeof(TestTrailingZeroArray)));
        CollectionAssert.AreEqual(new nint[] { 0, 8 }, GoReflect.GoFieldOffsets(typeof(TestTrailingEmptyStruct)));
        CollectionAssert.AreEqual(new nint[] { 0, 1 }, GoReflect.GoFieldOffsets(typeof(TestTrailingEmptyArray)));
    }

    // The padded size is the element STRIDE, so every field after an array of such structs moves
    // with it: Go's struct { s [3]struct{ n int64; z struct{} }; after int64 } is 3*16 + 8.
    [TestMethod]
    public void TrailingZeroSizeField_PaddedSizeIsTheArrayStride()
    {
        CollectionAssert.AreEqual(new nint[] { 0, 48 }, GoReflect.GoFieldOffsets(typeof(TestArrayOfTrailingZero)));
        Assert.AreEqual((nint)56, GoReflect.GoSizeOf(typeof(TestArrayOfTrailingZero)));
    }

    // The three shapes the rule must NOT change. Alignment already absorbs the byte (T4); a LEADING
    // zero-size field (the noCopy idiom) is never padded; and an all-zero-size struct stays size 0.
    [TestMethod]
    public void ZeroSizeField_NotLast_OrAbsorbed_OrAllZero_IsUnpadded()
    {
        Assert.AreEqual((nint)8, GoReflect.GoSizeOf(typeof(TestTrailingZeroAbsorbed)), "T4: alignment absorbs the byte");
        Assert.AreEqual((nint)8, GoReflect.GoSizeOf(typeof(TestLeadingEmptyStruct)), "a leading zero-size field is not padded");
        CollectionAssert.AreEqual(new nint[] { 0, 0 }, GoReflect.GoFieldOffsets(typeof(TestLeadingEmptyStruct)));
        Assert.AreEqual((nint)0, GoReflect.GoSizeOf(typeof(TestAllZeroSize)), "an all-zero-size struct stays 0");
    }

    [TestMethod]
    public void FieldOffsets_NonStructIsNull()
    {
        Assert.IsNull(GoReflect.GoFieldOffsets(typeof(int64)));
        Assert.IsNull(GoReflect.GoFieldOffsets(typeof(@string)));
    }

    [TestMethod]
    public void RangeOverUintptr_YieldsGoTypedValues()
    {
        // `for range atyp.Len` over abi.ArrayType.Len — the emission that had been dropped.
        List<uintptr> seen = new();

        foreach (uintptr i in range((uintptr)3))
            seen.Add(i);

        CollectionAssert.AreEqual(new uintptr[] { (uintptr)0, (uintptr)1, (uintptr)2 }, seen);
    }

    [TestMethod]
    public void RangeOverOtherIntegerWidths_CountsCorrectly()
    {
        Assert.AreEqual(4, Count(range((uint8)4)));
        Assert.AreEqual(5, Count(range((int64)5)));
        Assert.AreEqual(3, Count(range((uint64)3)));
        Assert.AreEqual(0, Count(range((int64)0)));
        Assert.AreEqual(0, Count(range((int64)(-7))), "a non-positive operand runs no iterations, as in Go");
        Assert.AreEqual(0, Count(range((uintptr)0)));
    }

    [TestMethod]
    public void RangeOverInt_StillBindsTheNonGenericOverload()
    {
        // The `int` case is the overwhelmingly common emission and must keep yielding nint —
        // a `var i` loop variable that silently became System.Int32 would diverge from Go's index
        // type across the whole corpus.
        foreach (var i in range((nint)1))
            Assert.IsInstanceOfType(i, typeof(nint));

        // A LITERAL operand is the dangerous shape: `range(3)` hands the overload set a C# int,
        // where the generic candidate is an identity match and the non-generic needs an implicit
        // numeric conversion. C#'s "a non-generic method is better than a generic method" tiebreak
        // is what keeps `range(3)` on nint — without it, `for i := range 3` would start yielding
        // System.Int32 and RangeIntIndexAppend's `append(s, i)` would go ambiguous again (CS0121).
        foreach (var i in range(3))
            Assert.IsInstanceOfType(i, typeof(nint), "a literal range operand must still yield Go's int (nint)");
    }

    private static int Count<T>(IEnumerable<T> source)
    {
        int n = 0;

        foreach (T _ in source)
            n++;

        return n;
    }
}
