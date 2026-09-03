using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using static go.builtin;

namespace GolibTests;

// The LIFETIME half of the address contract, and the sibling of NativeAddressStabilityTests: those
// prove an address STAYS PUT while its box is held, which is the property golib's
// EnsureStableAddress was written for. This file measures what happens when the box is NOT held,
// because that is the question every hand-owned syscall wrapper has to answer for itself.
//
// THE CONTRACT, as the code states it. `(uintptr)Ꮡx` on managed storage calls EnsureStableAddress
// (ж.cs), which stores a PinnedBuffer in the box's own `m_pin` field; the buffer owns a
// GCHandle.Alloc(..., Pinned) and a finalizer that frees it. So the pin's lifetime is the BOX's
// reachability -- not the referent's and not the address's. Two consequences that decide how a
// wrapper must be written:
//
//   * the provenance table cannot substitute for a holder: its entries are WeakReferences by
//     design ("this table must never be the reason a box stays alive", ж.PointerTokens.cs), so a
//     remembered address keeps nothing alive;
//   * holding the REFERENT is not enough either. A live but unpinned array is a relocatable array,
//     which is exactly what arm 2 below measures.
//
// WHY THIS IS A GOLIB GATE AND NOT A SYSCALL ONE. The damage shape at a real call site is a
// collection landing inside the kernel's window -- microseconds wide for ConvertSidToStringSid,
// seconds wide for a NetUserGetInfo that has to reach a domain controller. That is not something a
// test can schedule, so a value-level guard over those wrappers passes with the holder and without
// it (WindowsNetUserInfoTests and the PointerOutParameter behavioral test both do). What CAN be
// measured deterministically is the contract the holder exists to satisfy, which is what these four
// arms do -- and the first of them is a control that must MOVE, because "the address was stable" is
// not a measurement until something has been shown to move under the same churn.
//
// The two remedies the corpus uses are arms 3 and 4: a `System.GC.KeepAlive(<box>)` after a
// SYNCHRONOUS native call (the shape convSyscallFunnelCall emits, hand-written in the ptrout
// wrappers the converter cannot reach), and retention on a record for a submit whose flight
// outlives the call (OverlappedOp.m_pins, which is what ConnectEx uses).
[TestClass]
public class PinLifetimeAtTheNativeBoundaryTests
{
    private static object? s_sink;

    // A retention list of the shape OverlappedOp.m_pins is: an ordinary List<object> reachable from
    // a field, holding boxes for as long as the operation may still be in flight.
    private static readonly List<object> s_retained = new();

    // Enough gen0 garbage to fragment the heap, then two FORCED COMPACTING collections with a
    // finalizer drain between them. The drain is load-bearing: a dropped box's pin is released by
    // ~PinnedBuffer, so the first collection only makes the buffer finalizable and it takes the
    // second one to relocate anything.
    private static void Churn()
    {
        for (int i = 0; i < 50_000; i++)
        {
            s_sink = new byte[64];
        }

        s_sink = null;

        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
    }

    // Reads an ordinary array's current address WITHOUT leaving it pinned: the handle is freed
    // before the value is returned, so the next collection is free to move it again. This owes
    // nothing to golib -- it is the independent instrument the control arm needs.
    private static nuint CurrentAddress(byte[] array)
    {
        GCHandle handle = GCHandle.Alloc(array, GCHandleType.Pinned);

        try
        {
            return (nuint)handle.AddrOfPinnedObject();
        }
        finally
        {
            handle.Free();
        }
    }

    // ---- ARM 1: THE CONTROL ----------------------------------------------------------------------

    [TestMethod]
    public void AnUnpinnedArrayMovesUnderThisChurn()
    {
        // Stated on its own so the control is a named, readable result rather than a condition
        // buried in the arms that depend on it. If this ever stops moving, every "the address held
        // still" reading below becomes vacuous and must read NOT MEASURED, never green.
        byte[] control = new byte[64];
        nuint before = CurrentAddress(control);

        Churn();

        nuint after = CurrentAddress(control);

        Assert.AreNotEqual(before, after,
            "the collector did not relocate an ordinary unpinned array under a forced compacting " +
            "collection -- the churn cannot measure pin lifetime on this runtime");

        GC.KeepAlive(control);
    }

    // ---- ARM 2: THE DEFECT SHAPE -----------------------------------------------------------------

    // Takes an address the way an unprotected call-site argument does -- `(uintptr)Ꮡ(s, 0)` inside a
    // call whose frame then returns -- and drops the box. `Ꮡ(target, index)` allocates a FRESH
    // ElemRefBox on every call (builtin.cs), so nothing outlives this frame but the address.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static nuint PinAndDrop(slice<uint16> storage)
    {
        return (nuint)(uintptr)Ꮡ(storage, 0);
    }

    [TestMethod]
    public void ThePinIsReleasedWhenTheBoxDies()
    {
        byte[] control = new byte[64];
        nuint controlBefore = CurrentAddress(control);

        // The caller's buffer stays perfectly alive throughout -- this is the "holding the referent
        // is not enough" half of the contract. Only the BOX is dropped.
        slice<uint16> storage = new(8);
        storage[0] = 0x1234;

        nuint pinnedBefore = PinAndDrop(storage);

        Churn();

        if (controlBefore == CurrentAddress(control))
        {
            Assert.Inconclusive("the control array did not move; this arm measured nothing");
            return;
        }

        // The address the kernel would still be writing through now names other memory. Re-taking
        // it through a fresh box reports where the storage actually IS.
        nuint pinnedAfter = (nuint)(uintptr)Ꮡ(storage, 0);

        Assert.AreNotEqual(pinnedBefore, pinnedAfter,
            "the pin outlived the box that owned it -- if this is now a deliberate golib change, " +
            "the hand-owned KeepAlive/retention closures it made unnecessary should be retired with it");

        // And the storage itself is intact: this is a MOVE, not a loss, which is exactly what makes
        // the defect silent at a real call site -- the managed reads afterward are all correct.
        Assert.AreEqual<uint16>(0x1234, storage[0]);

        GC.KeepAlive(control);
    }

    // ---- ARM 3: THE SYNCHRONOUS REMEDY -----------------------------------------------------------

    [TestMethod]
    public void AKeepAliveAcrossTheCallHoldsThePin()
    {
        byte[] control = new byte[64];
        nuint controlBefore = CurrentAddress(control);

        slice<uint16> storage = new(8);
        var box = Ꮡ(storage, 0);
        nuint before = (nuint)(uintptr)box;

        // Churn() stands in for the native call: the shape being measured is that the box is
        // reported live ACROSS it, which is all System.GC.KeepAlive after the call buys -- and all
        // the ptrout wrappers need, since their calls are synchronous.
        //
        // Stated rather than overclaimed: under a DEBUG build the JIT extends every local to the end
        // of its method, so this arm would pass here even without the KeepAlive. It records the
        // remedy's SHAPE; arm 2 is the one that discriminates, because its box genuinely leaves
        // scope when the helper returns.
        Churn();

        System.GC.KeepAlive(box);

        if (controlBefore == CurrentAddress(control))
        {
            Assert.Inconclusive("the control array did not move; this arm measured nothing");
            return;
        }

        Assert.AreEqual(before, (nuint)(uintptr)Ꮡ(storage, 0),
            "a held box must hold its pin: the storage moved while the kernel could still be using it");

        GC.KeepAlive(control);
    }

    // ---- ARM 4: THE FLIGHT REMEDY ----------------------------------------------------------------

    // The OverlappedOp.m_pins shape: the submitting frame parks the box on the operation record and
    // returns, and the pin has to survive that return because the kernel's use has only just begun.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static nuint SubmitAndRetain(slice<uint16> storage)
    {
        var box = Ꮡ(storage, 0);
        nuint address = (nuint)(uintptr)box;

        s_retained.Add(box);

        return address;
    }

    [TestMethod]
    public void ARetentionListHoldsThePinAfterTheSubmittingFrameReturns()
    {
        s_retained.Clear();

        byte[] control = new byte[64];
        nuint controlBefore = CurrentAddress(control);

        slice<uint16> storage = new(8);
        nuint before = SubmitAndRetain(storage);

        Churn();

        if (controlBefore == CurrentAddress(control))
        {
            Assert.Inconclusive("the control array did not move; this arm measured nothing");
            return;
        }

        Assert.AreEqual(before, (nuint)(uintptr)Ꮡ(storage, 0),
            "a retained box must hold its pin past the submitting frame -- this is the property " +
            "ConnectEx's asynchronous send buffer rests on, and a KeepAlive cannot supply it");

        GC.KeepAlive(control);
    }

    // The other half of the contract, and a SEPARATE arm rather than a second reading inside the one
    // above -- measured, not assumed: a control array that has already survived one forced compacting
    // collection sits settled in gen2 and legitimately does not move again, so the second reading
    // reported Inconclusive and measured nothing. Each arm gets a control and a buffer freshly
    // allocated in gen0.
    [TestMethod]
    public void ClearingTheRetentionListReleasesThePin()
    {
        s_retained.Clear();

        byte[] control = new byte[64];
        nuint controlBefore = CurrentAddress(control);

        slice<uint16> storage = new(8);
        nuint before = SubmitAndRetain(storage);

        // What OverlappedOp.Rearm does at the next submit, and what Dispose does when the socket
        // retires: nothing is meant to hold a caller's buffer still forever.
        s_retained.Clear();

        Churn();

        if (controlBefore == CurrentAddress(control))
        {
            Assert.Inconclusive("the control array did not move; this arm measured nothing");
            return;
        }

        Assert.AreNotEqual(before, (nuint)(uintptr)Ꮡ(storage, 0),
            "clearing the retention list must release the pin");

        GC.KeepAlive(control);
    }
}
