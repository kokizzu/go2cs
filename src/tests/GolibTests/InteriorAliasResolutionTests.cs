using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

// SEAT (c), M4 -- the interior-alias step (GoReflect.InteriorAlias.cs; ruled 2026-09-26, ledger
// 19ab026dca). Go 1.24's reflect/all_test.go:1409 writes
//
//     *(*V)(unsafe.Add(unsafe.Pointer(&in), offset)) = value
//
// over a REFERENCE-BEARING struct, whose pointer has no address: the number is the box's order token
// plus a Go-layout byte offset. Before the step, arm 2a (offset 0) and the token-arithmetic refusal
// (any other offset) only ever refused it. The step resolves the offset through the base's GO layout
// and answers an alias over the REAL field -- but only where the offset lands EXACTLY on a node whose
// type IS V. Everything else keeps today's refusal, and OrderTokenOffsetZeroRefusalTests (long over
// delegate fields: a type mismatch at both offsets) is the standing proof that it still does.
[TestClass]
public class InteriorAliasResolutionTests
{
    // reflect's FLAT fixture, `struct{ _, a, _ func() }`: three delegates at Go offsets 0, 8, 16.
    private struct ThreeFuncs
    {
        internal Action first;
        internal Action second;
        internal Action third;
    }

    // reflect's NESTED fixture, `struct{ _, a [256]S }` with S = {i1, i2 int64}, at a smaller length:
    // a field hop, an array hop, then a field hop. The array field makes the struct reference-bearing
    // (array<T> holds a T[]), so its pointer is an order token exactly as the fixture's is.
    private struct Pair
    {
        internal long i1;
        internal long i2;
    }

    private struct TwoArrays
    {
        public TwoArrays() { }

        internal array<Pair> first = new(4);
        internal array<Pair> second = new(4);
    }

    // The BOX-HOP negative: a pointer field is an 8-byte scalar in Go's layout, and no offset may
    // follow it into its pointee -- that would be a second allocation reached by arithmetic.
    private struct Inner
    {
        internal Action f;
    }

    private struct WithPointer
    {
        internal ж<Inner> p;
        internal Action g;
    }

    private static ж<V> at<S, V>(ж<S> box, nuint offset) => (ж<V>)(uintptr)((nuint)(uintptr)box + offset);

    [TestMethod]
    public void Flat_EachOffsetLandsInItsOwnField()
    {
        Action one = static () => { }, two = static () => { }, three = static () => { };

        ж<ThreeFuncs> box = new StandardBox<ThreeFuncs>(new ThreeFuncs());
        Assert.AreEqual(PointerStorage.None, box.StorageKind, "the premise: a delegate-bearing pointee is an order token");

        // Offset 0 is arm 2a's door, 8 and 16 are the arithmetic refusal's -- the step serves both.
        at<ThreeFuncs, Action>(box, 0).ValueSlot = one;
        at<ThreeFuncs, Action>(box, 8).ValueSlot = two;
        at<ThreeFuncs, Action>(box, 16).ValueSlot = three;

        Assert.AreSame(one, box.Value.first, "offset 0 writes the first field");
        Assert.AreSame(two, box.Value.second, "offset 8 writes the second field");
        Assert.AreSame(three, box.Value.third, "offset 16 writes the third field");

        Assert.AreSame(two, at<ThreeFuncs, Action>(box, 8).Value, "and a READ through the same offset sees the field");
        GC.KeepAlive(box);
    }

    [TestMethod]
    public void Flat_OffsetZeroIsARealAliasNotTheFlaggedCarrier()
    {
        ж<ThreeFuncs> box = new StandardBox<ThreeFuncs>(new ThreeFuncs());
        ж<Action> answered = at<ThreeFuncs, Action>(box, 0);

        Assert.IsFalse(answered.IsNative, "a resolved interior pointer aliases managed storage, not a native number");
        GC.KeepAlive(box);
    }

    [TestMethod]
    public void Nested_FieldArrayFieldHopsLandInTheRealElement()
    {
        ж<TwoArrays> box = new StandardBox<TwoArrays>(new TwoArrays());
        Assert.AreEqual(PointerStorage.None, box.StorageKind, "the premise: an array-bearing struct is an order token");

        // reflect's own shape: offset 0 with V = int64 -- .first[0].i1.
        at<TwoArrays, long>(box, 0).ValueSlot = 11;
        // .first[1].i2: 1 * 16 + 8.
        at<TwoArrays, long>(box, 24).ValueSlot = 22;
        // .second[2].i1: the second array starts at 4 * 16 = 64, element 2 at 64 + 32.
        at<TwoArrays, long>(box, 96).ValueSlot = 33;

        Assert.AreEqual(11L, box.Value.first[0].i1, "offset 0 is .first[0].i1");
        Assert.AreEqual(22L, box.Value.first[1].i2, "offset 24 is .first[1].i2");
        Assert.AreEqual(33L, box.Value.second[2].i1, "offset 96 is .second[2].i1");
        Assert.AreEqual(0L, box.Value.first[0].i2, "and no neighbour moved");
        GC.KeepAlive(box);
    }

    [TestMethod]
    public void Nested_AnArrayElementOfTheRequestedTypeIsItselfANode()
    {
        ж<TwoArrays> box = new StandardBox<TwoArrays>(new TwoArrays());

        at<TwoArrays, Pair>(box, 64 + 16).ValueSlot = new Pair { i1 = 5, i2 = 6 };

        Assert.AreEqual(5L, box.Value.second[1].i1, "a Pair-typed node at .second[1] takes the whole element");
        Assert.AreEqual(6L, box.Value.second[1].i2);
        GC.KeepAlive(box);
    }

    [TestMethod]
    public void TypeMismatch_IsRefusedByTodaysName()
    {
        ж<TwoArrays> box = new StandardBox<TwoArrays>(new TwoArrays());

        // int at the offset of an int64 field: a REINTERPRET, never an alias.
        PanicException caught = Assert.ThrowsException<PanicException>(
            () => _ = at<TwoArrays, int>(box, 8), "a type mismatch at a node's offset is refused");

        StringAssert.Contains(caught.Message, "unsafe pointer arithmetic", "by the arithmetic refusal's own name");

        // At offset 0 the refusal stays where it was: the flagged carrier, refused on dereference.
        ж<int> carrier = at<TwoArrays, int>(box, 0);
        Assert.IsTrue(carrier.IsNative && ((NativeBox<int>)carrier).AliasesAnOrderToken,
            "at offset 0 a mismatch keeps today's flagged native carrier");
        StringAssert.Contains(Assert.ThrowsException<PanicException>(() => _ = carrier.Value).Message, "arm 2a");
        GC.KeepAlive(box);
    }

    [TestMethod]
    public void MidScalar_IsRefusedByTodaysName()
    {
        ж<TwoArrays> box = new StandardBox<TwoArrays>(new TwoArrays());

        PanicException caught = Assert.ThrowsException<PanicException>(
            () => _ = at<TwoArrays, int>(box, 4), "an offset inside an int64 names no Go node");

        StringAssert.Contains(caught.Message, "unsafe pointer arithmetic");

        ж<ThreeFuncs> flat = new StandardBox<ThreeFuncs>(new ThreeFuncs());
        Assert.ThrowsException<PanicException>(() => _ = at<ThreeFuncs, Action>(flat, 12), "nor one inside a delegate");
        GC.KeepAlive(box);
        GC.KeepAlive(flat);
    }

    [TestMethod]
    public void BoxHop_AnOffsetNeverFollowsAPointerField()
    {
        ж<WithPointer> box = new StandardBox<WithPointer>(new WithPointer { p = new StandardBox<Inner>(new Inner()) });

        // Inner's own field sits at Inner's offset 0 -- but Inner is behind the pointer, so asking for
        // it at WithPointer's offset 0 is a second allocation reached by arithmetic.
        ж<Action> carrier = at<WithPointer, Action>(box, 0);
        Assert.IsTrue(carrier.IsNative && ((NativeBox<Action>)carrier).AliasesAnOrderToken,
            "offset 0 names the POINTER field, whose type is not Action: today's carrier stands");

        ж<Inner> pointee = at<WithPointer, Inner>(box, 0);
        Assert.IsTrue(pointee.IsNative, "nor is the pointee itself reachable through the pointer field");

        // The field AFTER the pointer is a direct field and resolves normally -- the control.
        Action g = static () => { };
        at<WithPointer, Action>(box, 8).ValueSlot = g;
        Assert.AreSame(g, box.Value.g, "the next direct field still resolves");
        GC.KeepAlive(box);
    }

    [TestMethod]
    public void Identity_TwoResolutionsOfOneOffsetAreOnePointer()
    {
        ж<ThreeFuncs> flat = new StandardBox<ThreeFuncs>(new ThreeFuncs());

        Assert.IsTrue(at<ThreeFuncs, Action>(flat, 8) == at<ThreeFuncs, Action>(flat, 8), "&s.a == &s.a");
        Assert.IsFalse(at<ThreeFuncs, Action>(flat, 8) == at<ThreeFuncs, Action>(flat, 16), "&s.a != &s._");

        ж<TwoArrays> nested = new StandardBox<TwoArrays>(new TwoArrays());

        Assert.IsTrue(at<TwoArrays, long>(nested, 96) == at<TwoArrays, long>(nested, 96), "&s.second[2].i1 is one pointer");
        Assert.IsFalse(at<TwoArrays, long>(nested, 96) == at<TwoArrays, long>(nested, 104), "and its neighbour is another");
        GC.KeepAlive(flat);
        GC.KeepAlive(nested);
    }
}
