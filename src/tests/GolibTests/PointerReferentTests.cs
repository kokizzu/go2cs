using System;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

[TestClass]
public class PointerReferentTests
{
    // A ж<T> is an expression TEMPORARY — `Ꮡ(s, i)` allocates a fresh box per call and
    // `Ꮡx.of(T.ᏑField)` one per field access — so anything asking "when does this object die?" or
    // "is this the same object?" must ask the REFERENT, not the box. INilPointer.ReferentObject is
    // that projection. Two consumers depend on it and were both broken without it:
    // runtime.SetFinalizer (which keyed its registration on the throwaway box, tracking a lifetime
    // nothing in the program shared) and sync.Cond's copyChecker (which cannot store a raw address
    // on a moving collector). Neither is expressible as a Go-parity behavioral test — one is a GC
    // timing property, the other lives in a hand-owned stdlib file — so the invariants are guarded
    // here, at the golib level.

    private struct Inner
    {
        internal nint Field;
    }

    private struct Outer
    {
        internal Inner Nested;
    }

    private static ref nint innerField(ref Inner inner) => ref inner.Field;

    private static ref Inner outerNested(ref Outer outer) => ref outer.Nested;

    [TestMethod]
    public void ElementPointerReferentIsTheBackingStorageNotThePerCallHeader()
    {
        slice<byte> s = new(4);

        object first = ((INilPointer)Ꮡ(s, 0)).ReferentObject;
        object second = ((INilPointer)Ꮡ(s, 3)).ReferentObject;

        // Distinct boxes, distinct indices — one allocation, so ONE referent (Go finalizes the
        // object, not the address within it).
        Assert.AreSame(first, second);
        Assert.IsInstanceOfType(first, typeof(byte[]));
    }

    [TestMethod]
    public void FieldPointerReferentResolvesTHROUGHIntermediateBoxesToTheRoot()
    {
        ref Outer outer = ref heap(new Outer(), out ж<Outer> Ꮡouter);

        // `Ꮡouter.of(Outer.ᏑNested).of(Inner.ᏑField)` — the intermediate ж<Inner> is minted fresh on
        // every access, so stopping at the immediate source would hand back a different object each
        // time and any identity question asked of it would answer "different" for one same object.
        object first = ((INilPointer)Ꮡouter.of<Inner>(outerNested).of<nint>(innerField)).ReferentObject;
        object second = ((INilPointer)Ꮡouter.of<Inner>(outerNested).of<nint>(innerField)).ReferentObject;

        Assert.AreSame(Ꮡouter, first);
        Assert.AreSame(first, second);
        GC.KeepAlive(outer);
    }

    [TestMethod]
    public void BoxedValuePointerIsItsOwnReferent()
    {
        ref nint value = ref heap<nint>(out ж<nint> Ꮡvalue);

        Assert.AreSame(Ꮡvalue, ((INilPointer)Ꮡvalue).ReferentObject);
        GC.KeepAlive(value);
    }

    // `Ꮡ<T>(IArray<T>, index)` takes its target BY VALUE. As `in IArray<T>` it elided no copy (an
    // interface is already one reference) but forced the caller's boxing temp — a slice<T> header is
    // a struct, so every call boxes one — to be ADDRESS-EXPOSED, and an address-exposed slot is not
    // lifetime-tracked: the JIT reported it live for the whole enclosing method, so one Ꮡ(s, i)
    // pinned s's backing array to the caller's frame until that method RETURNED. This test asserts
    // the pointer does not outlive its own scope; it deliberately does NOT assert the array is
    // collectible from a still-running frame, which the CLR's GC info does not promise (that is the
    // `codegen-liveness` divergence class).
    [TestMethod]
    public void ElementPointerDoesNotOutliveTheScopeThatTookIt()
    {
        WeakReference storage = takeElementPointerInItsOwnScope();

        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);

        Assert.IsFalse(storage.IsAlive, "Ꮡ(s, i) kept the backing array alive past its own scope");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference takeElementPointerInItsOwnScope()
    {
        slice<byte> s = new(1024);
        ж<byte> element = Ꮡ(s, 0);

        return new WeakReference(((INilPointer)element).ReferentObject);
    }
}
