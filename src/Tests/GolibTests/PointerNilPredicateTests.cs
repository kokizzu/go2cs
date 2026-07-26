using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

[TestClass]
public class PointerNilPredicateTests
{
    // A ж<T> box answers TWO different nil questions, and conflating them is a defect class of its
    // own (see ж<T>.IsNull vs ж<T>.IsNilPointer): "is this THE nil pointer" (structural) versus
    // "would reading this box's own m_val slot read a null reference". The second one is true for
    // three unrelated things — the nil pointer, a real address whose reference-typed pointee is
    // legitimately nil (`&i` with `i == nil`), and a struct-field / array-element reference box,
    // whose m_val is an unused default because its storage lives in the referenced struct or array.
    //
    // The Go-EXPRESSIBLE half of the class is guarded by the PointerToNilPointerIdentity behavioral
    // test (pointer identity, map-key hash stability, field-reference deref). The shapes below are
    // not reachable from converted Go — the converter resolves a reference-typed pointee's deref
    // through ValueSlot, so `operator ~` is only ever reached from golib-internal, hand-owned and
    // reflection-bridge code — so they are guarded here, at the golib level, where they can be
    // exercised directly.

    private struct pair
    {
        internal ж<nint> p;
    }

    // `using static go.builtin` puts a Ꮡ-style `ж<T>(…)` method in scope, so the TYPE's static
    // members cannot be named directly here; the nil conversion yields the same canonical box.
    private static ж<ж<nint>> canonicalNilPointer => nil;

    // A stand-in for a go2cs-gen NAMED-pointer wrapper (`type P *T` — a non-generic class holding a
    // ж<T> and implementing IPointer<T>/INilPointer, per PointerTypeTemplate). The reflection
    // bridge's interface-routed slot reader only fires for this shape, and no converted-Go program
    // can produce a named pointer to a REFERENCE-typed pointee that also reaches it.
    private sealed class namedPointer : IPointer<ж<nint>>, INilPointer
    {
        private readonly ж<ж<nint>> m_value;

        public namedPointer(ж<ж<nint>> value) => m_value = value;

        public ref ж<nint> Value => ref m_value.Value;

        public bool IsNull => m_value is null || m_value.IsNull;

        public bool IsNilPointer => m_value is null || m_value.IsNilPointer;

        public ж<TElem> of<TElem>(FieldRefFunc<TElem> fieldRefFunc) => m_value.of(fieldRefFunc);

        public ж<TElem> of<TElem>(FieldRefFunc<ж<nint>, TElem> fieldRefFunc) => m_value.of(fieldRefFunc);

        public ж<TElem> at<TElem>(nint index) => m_value.at<TElem>(index);

        static ж<nint> IPointer<ж<nint>>.operator ~(IPointer<ж<nint>> value) => value.Value;
    }

    // Binds the IPointer<T> static-abstract `operator ~` (an interface operator is only reachable
    // through a constrained type parameter) — the arm that hand-owned generic runtime code takes.
    private static TElem derefViaInterface<TPtr, TElem>(TPtr pointer) where TPtr : IPointer<TElem>
    {
        return ~pointer;
    }

    [TestMethod]
    public void DerefOfRealAddressHoldingNilPointerYieldsNil()
    {
        // Go: `p := (*int)(nil); q := &p; *q` is nil — q is a real address, no dereference happens.
        ж<ж<nint>> q = new(default(ж<nint>)!);

        Assert.IsTrue(~q == nil);
        Assert.IsTrue(derefViaInterface<ж<ж<nint>>, ж<nint>>(q) == nil);
    }

    [TestMethod]
    public void DerefOfTheNilPointerPanics()
    {
        ж<ж<nint>> nilPointer = canonicalNilPointer;

        Assert.ThrowsException<PanicException>(() => ~nilPointer);
        Assert.ThrowsException<PanicException>(() => derefViaInterface<ж<ж<nint>>, ж<nint>>(nilPointer));
    }

    [TestMethod]
    public void DerefOfFieldReferenceReadsTheFieldNotTheUnusedSlot()
    {
        ж<pair> owner = new(new pair());
        ж<ж<nint>> fieldPointer = owner.of(FieldRef<pair>.Create<ж<nint>>(nameof(pair.p)));

        // While the field holds nil the field reference is still a real address that reads as nil.
        Assert.IsFalse(fieldPointer.IsNilPointer);
        Assert.IsFalse(fieldPointer.IsNull);
        Assert.IsTrue(~fieldPointer == nil);

        ж<nint> target = new(7);
        owner.ValueSlot.p = target;

        // ... and once assigned it reads the FIELD's value, not the box's unused default slot.
        Assert.AreSame(target, ~fieldPointer);
        Assert.AreSame(target, derefViaInterface<ж<ж<nint>>, ж<nint>>(fieldPointer));
    }

    [TestMethod]
    public void ReadPointerSlotResolvesAFieldReferenceOverAReferenceTypedField()
    {
        ж<pair> owner = new(new pair());
        ж<nint> target = new(9);
        owner.ValueSlot.p = target;

        ж<ж<nint>> fieldPointer = owner.of(FieldRef<pair>.Create<ж<nint>>(nameof(pair.p)));

        // The reflection bridge's slot read (reflect.Value.Elem of an addressable field) must see
        // the field, not default(T) — which is what the value-peeking predicate reported.
        Assert.AreSame(target, GoReflect.ReadPointerSlot(fieldPointer));

        // Same read through the INTERFACE-routed arm a named-pointer wrapper takes.
        Assert.AreSame(target, GoReflect.ReadPointerSlot(new namedPointer(fieldPointer)));
    }

    [TestMethod]
    public void ReadPointerSlotOfANamedPointerWrapper()
    {
        ж<nint> target = new(21);

        Assert.AreSame(target, GoReflect.ReadPointerSlot(new namedPointer(new ж<ж<nint>>(target))));

        // A real address whose pointee is nil reads as nil; the nil pointer reads as the zero value.
        Assert.IsNull(GoReflect.ReadPointerSlot(new namedPointer(new ж<ж<nint>>(default(ж<nint>)!))));
        Assert.IsNull(GoReflect.ReadPointerSlot(new namedPointer(canonicalNilPointer)));
    }

    [TestMethod]
    public void DerefOrNilOfARealAddressReturnsTheRealSlot()
    {
        ж<ж<nint>> box = new(default(ж<nint>)!);
        ж<nint> target = new(11);

        ref ж<nint> slot = ref box.DerefOrNil();
        slot = target;

        // The re-alias must alias the box's OWN storage, so the write persists (a throwaway slot
        // silently swallowed it).
        Assert.AreSame(target, box.ValueSlot);
    }

    [TestMethod]
    public void DerefOrNilOfTheNilPointerYieldsAThrowawaySlot()
    {
        ж<ж<nint>> nilPointer = canonicalNilPointer;

        ref ж<nint> slot = ref nilPointer.DerefOrNil();
        slot = new ж<nint>(13);

        // The canonical typed-nil singleton must stay unwritten — it is shared process-wide.
        Assert.IsTrue(canonicalNilPointer.IsNilPointer);
        Assert.IsTrue(canonicalNilPointer == nil);
    }

    [TestMethod]
    public void PointerHashIsStableWhileThePointeeIsAssigned()
    {
        ж<ж<nint>> key = new(default(ж<nint>)!);
        Dictionary<ж<ж<nint>>, string> map = new() { [key] = "present" };

        key.ValueSlot = new ж<nint>(17);

        // An address does not move when the value at it changes.
        Assert.IsTrue(map.TryGetValue(key, out string found));
        Assert.AreEqual("present", found);
    }

    [TestMethod]
    public void DistinctBoxesOverOneReferentAreDistinctAddresses()
    {
        ж<nint> shared = new(19);
        ж<ж<nint>> a = new(shared);
        ж<ж<nint>> b = new(shared);

        // Go: `c := &v; d := &v; &c == &d` is false — two variables, two addresses.
        Assert.IsFalse(a == b);
        Assert.IsFalse(a.Equals(b));
        Assert.AreEqual(2, new Dictionary<ж<ж<nint>>, string> { [a] = "a", [b] = "b" }.Count);
    }

    [TestMethod]
    public void NativeAliasesOfOneAddressAreOnePointer()
    {
        // Go: `(*T)(unsafe.Pointer(p)) == (*T)(unsafe.Pointer(p))` — a uintptr round-trip yields a
        // fresh box each time, and the aliased address is the whole of its identity.
        ж<nint> a = new(0x1000u);
        ж<nint> b = new(0x1000u);
        ж<nint> c = new(0x2000u);

        Assert.IsTrue(a == b);
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        Assert.IsFalse(a == c);
        Assert.AreEqual(2, new Dictionary<ж<nint>, string> { [a] = "a", [b] = "b", [c] = "c" }.Count);
    }
}
