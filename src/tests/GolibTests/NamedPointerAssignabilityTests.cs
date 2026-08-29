using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

[TestClass]
public class NamedPointerAssignabilityTests
{
    // Go's assignability rule for a DEFINED type (spec, "Assignability"): a value x of type V is
    // assignable to a variable of type T when "V and T have identical underlying types but are not
    // type parameters and at least one of V or T is not a named type". `type P *int` is a named type
    // whose underlying type IS `*int`, so a plain `*int` — unnamed — assigns into a P slot, and a P
    // assigns back into a `*int` slot. Two DISTINCT named pointer types never assign to each other:
    // both sides are named, so neither arm of the rule applies.
    //
    // The spec's method-set interference clause ("...and at least one of V or T is not a named
    // type") has a companion restriction that makes this case unambiguous: a named POINTER type can
    // carry no methods at all, because a receiver base type may not be a pointer type
    // (spec, "Method declarations": "The receiver base type ... must not be a pointer or interface
    // type"). So there is no method set to interfere with here, in either direction.
    //
    // The bridge decides this in ONE place — GoReflect.TryMarshalAssignable — which is the write
    // half of reflect.Value.Set and also carries SetMapIndex (key and elem), MapIndex's key,
    // Value.Call's argument marshalling, internal/reflectlite's Set, and (through its first arm)
    // TryConvertTo's whole Convert/Set{Int,Uint,...} family. Guarding the relation here guards all
    // of them.
    //
    // WHAT WAS BROKEN: the named/unnamed arm matched the destination wrapper's constructor
    // parameter type against the source's runtime type by EXACT EQUALITY. For a pointer underlying
    // that test is unsatisfiable — the parameter type is `ж<T>`, which is ABSTRACT, so every live
    // box is necessarily a proper subclass (StandardBox<T> and friends) and no value can ever equal
    // it. Every `type P *T` slot therefore rejected every `*T` value, and testing/quick's
    // TestCheckEqual died on `reflect.Set: value of type *int is not assignable to type
    // quick.TestPtrAlias`.

    // ---- Stand-ins for the go2cs-gen named-pointer wrapper (`type P *int`) -----------------------
    //
    // Shape per Templates/InheritedType/{InheritedTypeTemplate,PointerTypeTemplate}: a class marked
    // [GoType("ж<...>")], holding the underlying in a private `m_value` field, with a public
    // single-argument constructor over that underlying and a NilType constructor. Those three facts
    // are exactly what the bridge reads (goTypeMarkerOf, wrapperConstructorOf, TryUnwrapWrapperValue),
    // so the stand-ins reproduce them literally rather than approximating.

    [GoType("ж<nint>")]
    private sealed class ptrAlias : IPointer<nint>, INilPointer
    {
        private readonly ж<nint> m_value;

        public ptrAlias(ж<nint> value) => m_value = value;

        public ptrAlias(NilType _) => m_value = default!;

        public ref nint Value => ref m_value.Value;

        public bool IsNull => m_value is null || m_value.IsNull;

        public bool IsNilPointer => m_value is null || m_value.IsNilPointer;

        public ж<TElem> of<TElem>(FieldRefFunc<TElem> fieldRefFunc) => m_value.of(fieldRefFunc);

        public ж<TElem> of<TElem>(FieldRefFunc<nint, TElem> fieldRefFunc) => m_value.of(fieldRefFunc);

        public ж<TElem> at<TElem>(nint index) => m_value.at<TElem>(index);

        static nint IPointer<nint>.operator ~(IPointer<nint> value) => value.Value;
    }

    // A SECOND named pointer type over the very same underlying — the negative control. Go refuses
    // `otherPtrAlias -> ptrAlias` in both directions (both named), and the bridge must too.
    [GoType("ж<nint>")]
    private sealed class otherPtrAlias : IPointer<nint>, INilPointer
    {
        private readonly ж<nint> m_value;

        public otherPtrAlias(ж<nint> value) => m_value = value;

        public otherPtrAlias(NilType _) => m_value = default!;

        public ref nint Value => ref m_value.Value;

        public bool IsNull => m_value is null || m_value.IsNull;

        public bool IsNilPointer => m_value is null || m_value.IsNilPointer;

        public ж<TElem> of<TElem>(FieldRefFunc<TElem> fieldRefFunc) => m_value.of(fieldRefFunc);

        public ж<TElem> of<TElem>(FieldRefFunc<nint, TElem> fieldRefFunc) => m_value.of(fieldRefFunc);

        public ж<TElem> at<TElem>(nint index) => m_value.at<TElem>(index);

        static nint IPointer<nint>.operator ~(IPointer<nint> value) => value.Value;
    }

    // `type P *uintptr`, for the N5 M-guard below.
    [GoType("ж<uintptr>")]
    private sealed class uintptrPtrAlias : IPointer<uintptr>, INilPointer
    {
        private readonly ж<uintptr> m_value;

        public uintptrPtrAlias(ж<uintptr> value) => m_value = value;

        public uintptrPtrAlias(NilType _) => m_value = default!;

        public ref uintptr Value => ref m_value.Value;

        public bool IsNull => m_value is null || m_value.IsNull;

        public bool IsNilPointer => m_value is null || m_value.IsNilPointer;

        public ж<TElem> of<TElem>(FieldRefFunc<TElem> fieldRefFunc) => m_value.of(fieldRefFunc);

        public ж<TElem> of<TElem>(FieldRefFunc<uintptr, TElem> fieldRefFunc) => m_value.of(fieldRefFunc);

        public ж<TElem> at<TElem>(nint index) => m_value.at<TElem>(index);

        static uintptr IPointer<uintptr>.operator ~(IPointer<uintptr> value) => value.Value;
    }

    // The shape of core/unsafe's `Pointer` (`public class Pointer : StandardBox<uintptr>,
    // IUnsafePointer`) — a ж<uintptr> subclass that is NOT an ordinary *uintptr. Modelled locally so
    // the guard needs no reference to the hand-owned unsafe package; the marker interface is the
    // whole probe, exactly as at the marshalling sites.
    private sealed class fakeUnsafePointer(uintptr value) : StandardBox<uintptr>(value), IUnsafePointer;

    // ---- The rule itself -------------------------------------------------------------------------

    [TestMethod]
    public void NamedPointerSlotAcceptsARawPointerOfTheSameUnderlying()
    {
        // Go: `type P *int; var p P = new(int)` — the source `*int` is unnamed, so it assigns.
        // This is quick.Value's `v.Set(reflect.New(concrete.Elem()))` for a `type TestPtrAlias *int`
        // field, and the assignment that panicked before the fix.
        ж<nint> box = new StandardBox<nint>(7);

        Assert.IsTrue(GoReflect.TryMarshalAssignable(box, typeof(ptrAlias), out object? marshalled));

        ptrAlias alias = (ptrAlias)marshalled!;

        // The SAME box, not a copy of it — quick then does `v.Elem().Set(elem)`, so a copied box
        // would accept the write and drop it.
        Assert.IsTrue(GoReflect.TryUnwrapWrapperValue(alias, out object? underlying));
        Assert.AreSame(box, underlying);
    }

    [TestMethod]
    public void TheWrappedPointerStillWritesThroughToTheOriginalAllocation()
    {
        // The behavioural half of the identity assertion above: aliasing, not reference equality, is
        // what the caller depends on.
        ж<nint> box = new StandardBox<nint>(7);

        Assert.IsTrue(GoReflect.TryMarshalAssignable(box, typeof(ptrAlias), out object? marshalled));

        ((ptrAlias)marshalled!).Value = 42;

        Assert.AreEqual((nint)42, box.Value);
    }

    [TestMethod]
    public void RawPointerSlotAcceptsANamedPointerOfTheSameUnderlying()
    {
        // The rule's other direction — `var q *int = p` where `p` is a P. T is unnamed here, so the
        // same clause applies. (This arm already worked: it is reached through the wrapper UNWRAP
        // path, whose subsumption test was never the exact one. It is guarded so the two directions
        // cannot drift apart.)
        ж<nint> box = new StandardBox<nint>(7);
        ptrAlias alias = new(box);

        Assert.IsTrue(GoReflect.TryMarshalAssignable(alias, typeof(ж<nint>), out object? marshalled));
        Assert.AreSame(box, marshalled);
    }

    [TestMethod]
    public void TwoDistinctNamedPointerTypesRefuseEachOther()
    {
        // The negative control: both sides named, so NEITHER arm of Go's rule applies and the
        // assignment is illegal — `cannot use p (variable of type P) as Q value`. A fix that merely
        // loosened the destination test would pass the three cases above and fail here.
        ж<nint> box = new StandardBox<nint>(7);

        Assert.IsFalse(GoReflect.TryMarshalAssignable(new ptrAlias(box), typeof(otherPtrAlias), out _));
        Assert.IsFalse(GoReflect.TryMarshalAssignable(new otherPtrAlias(box), typeof(ptrAlias), out _));
    }

    [TestMethod]
    public void NamedPointerSlotRefusesAPointerToADifferentPointee()
    {
        // Identical UNDERLYING types is the whole rule; `*int32` and `*int` are different types, so
        // no arm applies however the named side is spelled.
        Assert.IsFalse(GoReflect.TryMarshalAssignable(new StandardBox<int32>(7), typeof(ptrAlias), out _));
    }

    [TestMethod]
    public void NamedUintptrPointerSlotRefusesAnUnsafePointer()
    {
        // The N5 M-guard, carried into this arm alongside the two subsumption arms around it:
        // unsafe.Pointer derives from StandardBox<uintptr>, so plain subsumption would wrap it into
        // a `type P *uintptr` slot. Go treats unsafe.Pointer as its own type — not a *uintptr — and
        // refuses the assignment.
        Assert.IsFalse(GoReflect.TryMarshalAssignable(new fakeUnsafePointer(0), typeof(uintptrPtrAlias), out _));

        // The ordinary *uintptr still assigns, so the guard excludes only what it names.
        Assert.IsTrue(GoReflect.TryMarshalAssignable(new StandardBox<uintptr>(0), typeof(uintptrPtrAlias), out _));
    }

    [TestMethod]
    public void ConvertReachesTheSameRelation()
    {
        // A NON-REGRESSION companion, not a red proof: this one already passed before the fix, and
        // that is the interesting part. TryConvertTo tries TryMarshalAssignable first but has a
        // wrapper arm of its OWN below it, which reached the constructor by converting the source to
        // the parameter type rather than by comparing types — so reflect.Value.Convert kept working
        // over a `type P *int` while reflect.Value.Set did not, and the defect presented as
        // Set-only. Held here so the two routes cannot answer differently again.
        ж<nint> box = new StandardBox<nint>(7);

        Assert.IsTrue(GoReflect.TryConvertTo(box, typeof(ptrAlias), out object? converted));
        Assert.IsInstanceOfType(converted, typeof(ptrAlias));
    }
}
