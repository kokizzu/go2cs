using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using go.sync; // the Pointer<T> method set is extension methods on atomic_package — an alias alone does not import them
using atomic = go.sync.atomic_package;
using @unsafe = go.unsafe_package;

namespace GolibTests;

[TestClass]
public class NativePointerSlotAtomicsTests
{
    // sync/atomic's TestHammerStoreLoad reinterprets a shared uint64 as `*unsafe.Pointer` and as
    // `*atomic.Pointer[byte]`, then hammers atomic loads and stores of FABRICATED pointer values
    // through it (Go's own comment: "values that aren't real pointers"). Go's semantics for that
    // memory are that the 8 bytes hold the POINTER'S VALUE.
    //
    // The defect these tests pin closed: the native-backed ж<Pointer> box's `ref Value` reinterprets
    // the slot as a managed REFERENCE slot, so a store planted a CLR reference in memory the GC does
    // not scan — the number never entered the slot at all, and the referenced box was collectible
    // the moment the store returned. Measured as `Pointer: 0 != N` after ~16k iterations (gen0
    // recycling under the loop's own allocation pressure), 107 of 108 verdicts behind it.
    //
    // The invariant is boundary-shaped and so are the guards: the SLOT ITSELF must hold the number
    // (asserted by reading the aliased storage back through its own managed box — deterministic, no
    // GC race), and the round-trip must survive a forced compacting collection (the dangling-
    // reference hazard, made deterministic by collecting rather than by racing).

    private const ulong Packed = 0x0000_0001_0000_0001UL; // the hammer's shape: low half == high half

    private static (ж<ulong> slot, ж<@unsafe.Pointer> addr) NativeSlot()
    {
        ref ulong value = ref heap(0UL, out ж<ulong> Ꮡslot);
        _ = value;

        // The emitted reinterpret, verbatim: pin the slot, take its real address, mint the
        // native-backed *unsafe.Pointer over it.
        uintptr address = Ꮡslot;
        var addr = (ж<@unsafe.Pointer>)(uintptr)address;

        Assert.IsTrue(addr.IsNative, "fixture is inert: the reinterpreted box must be NATIVE-backed");
        return (Ꮡslot, addr);
    }

    [TestMethod]
    public void StorePointerPutsTheNumberInTheSlot()
    {
        (ж<ulong> slot, ж<@unsafe.Pointer> addr) = NativeSlot();

        atomic.StorePointer(addr, new @unsafe.Pointer((uintptr)Packed));

        // The deterministic assert: the aliased storage itself holds the pointer's VALUE. The old
        // code wrote a CLR reference here — a live heap address, never equal to the number — so
        // this fails first without any dependence on collector timing.
        Assert.AreEqual(Packed, slot.Value, "the slot must hold the pointer's value, not a reference");
    }

    [TestMethod]
    public void LoadPointerRoundTripsTheNumberAcrossACollection()
    {
        (_, ж<@unsafe.Pointer> addr) = NativeSlot();

        atomic.StorePointer(addr, new @unsafe.Pointer((uintptr)Packed));

        // The hazard the number-in-slot model removes: nothing managed refers to what the slot
        // holds, so a full compacting collection between store and load must change nothing.
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        @unsafe.Pointer loaded = atomic.LoadPointer(addr);
        Assert.AreEqual(Packed, (ulong)((uintptr)loaded).Value, "the number must survive a collection");
    }

    [TestMethod]
    public void SwapAndCompareAndSwapOperateOnNumbers()
    {
        (ж<ulong> slot, ж<@unsafe.Pointer> addr) = NativeSlot();

        atomic.StorePointer(addr, new @unsafe.Pointer((uintptr)Packed));

        const ulong Next = Packed + 1 + (1UL << 32);

        @unsafe.Pointer old = atomic.SwapPointer(addr, new @unsafe.Pointer((uintptr)Next));
        Assert.AreEqual(Packed, (ulong)((uintptr)old).Value, "Swap must return the previous VALUE");
        Assert.AreEqual(Next, slot.Value, "Swap must store the new VALUE");

        // CAS compares NUMBERS — a freshly minted box holding the current value must match, exactly
        // as Go compares unsafe.Pointer, and exactly as Pointer.Equals already answers.
        Assert.IsTrue(atomic.CompareAndSwapPointer(addr, new @unsafe.Pointer((uintptr)Next), new @unsafe.Pointer((uintptr)Packed)),
            "CAS with a re-minted box holding the current value must swap");
        Assert.AreEqual(Packed, slot.Value);

        Assert.IsFalse(atomic.CompareAndSwapPointer(addr, new @unsafe.Pointer((uintptr)Next), new @unsafe.Pointer((uintptr)0UL)),
            "CAS against a stale value must refuse");
        Assert.AreEqual(Packed, slot.Value);
    }

    [TestMethod]
    public void NilRoundTripsAsTheZeroWord()
    {
        (ж<ulong> slot, ж<@unsafe.Pointer> addr) = NativeSlot();

        atomic.StorePointer(addr, new @unsafe.Pointer((uintptr)Packed));
        atomic.StorePointer(addr, nil);

        Assert.AreEqual(0UL, slot.Value, "a nil pointer's value is the zero word");
        Assert.IsTrue(atomic.LoadPointer(addr) == nil, "a zero word loads as nil");
    }

    [TestMethod]
    public void PointerMethodFormSharesTheSameSlotSemantics()
    {
        // The hammer's OTHER arm: the same storage reinterpreted as *atomic.Pointer[byte], driven
        // through the generic method set. Same slot, same value semantics.
        ref ulong value = ref heap(0UL, out ж<ulong> Ꮡslot);
        _ = value;

        uintptr address = Ꮡslot;
        var addr = (ж<atomic.Pointer<byte>>)(uintptr)address;
        Assert.IsTrue(addr.IsNative, "fixture is inert: the reinterpreted box must be NATIVE-backed");

        addr.Store((ж<byte>)(uintptr)(nuint)Packed);
        Assert.AreEqual(Packed, Ꮡslot.Value, "Store must put the pointer's value in the slot");

        GC.Collect();
        GC.WaitForPendingFinalizers();

        Assert.AreEqual(Packed, (ulong)((uintptr)addr.Load()).Value, "Load must answer the value, across a collection");

        const ulong Next = Packed + 1 + (1UL << 32);
        ж<byte> old = addr.Swap((ж<byte>)(uintptr)(nuint)Next);
        Assert.AreEqual(Packed, (ulong)((uintptr)old).Value, "Swap must return the previous value");

        Assert.IsTrue(addr.CompareAndSwap((ж<byte>)(uintptr)(nuint)Next, (ж<byte>)(uintptr)(nuint)Packed),
            "CAS with a re-minted pointer holding the current value must swap");
        Assert.AreEqual(Packed, Ꮡslot.Value);
    }

    [TestMethod]
    public void MiniHammerHoldsThePackedInvariantUnderForcedCollections()
    {
        // hammerStoreLoadPointer's loop body, single-threaded, with the collector forced mid-loop —
        // the deterministic form of the storm. On the reference-in-slot model this dies or diverges
        // within one collection; on the value model the invariant holds for every iteration.
        (_, ж<@unsafe.Pointer> addr) = NativeSlot();

        for (int i = 0; i < 2_000; i++)
        {
            ulong v = (ulong)((uintptr)atomic.LoadPointer(addr)).Value;
            ulong vlo = v & 0xFFFF_FFFFUL, vhi = v >> 32;

            Assert.AreEqual(vlo, vhi, $"packed-pair invariant broke at iteration {i}: {vlo:x} != {vhi:x}");

            atomic.StorePointer(addr, new @unsafe.Pointer((uintptr)(v + 1 + (1UL << 32))));

            if (i % 500 == 250)
                GC.Collect();
        }
    }
}
