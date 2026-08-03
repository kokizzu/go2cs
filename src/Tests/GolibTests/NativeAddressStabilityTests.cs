using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

[TestClass]
public class NativeAddressStabilityTests
{
    // Go's `uintptr(unsafe.Pointer(&x))` names an address that stays valid while native code uses it
    // (unsafe.Pointer rules 3 and 4). Go's collector never moves heap objects, so that is free there;
    // the CLR's does move them, and go2cs's address operators ended in a `fixed` — a pin that lasts for
    // ONE statement — before returning the address as an integer. Every address handed to a syscall was
    // therefore a former address, surviving only because the window to the trampoline is short.
    //
    // A BLOCKING syscall reopens that window for as long as the kernel takes. os's TestPipeEOF parks in
    // ReadFile on a pipe for 10 ms per read while a parallel suite allocates around it; a gen0
    // collection in that window moves the caller's *uint32 byte-count box and the read buffer, the
    // kernel writes to neither, syscall.Read returns (0, nil), and FD.eofError turns that into a
    // premature io.EOF — with a probability that rises monotonically with parallelism, which is exactly
    // what was measured. The buffer write lands in freed heap, which is the moving-site
    // ExecutionEngineException recorded beside it.
    //
    // These are neutered-fix controls: with EnsureStableAddress removed, every MOVED assertion below
    // fails on the first forced collection. They cannot be written as Go-parity behavioral tests — the
    // property is "the collector cannot move this", which no Go program can observe directly.

    private static object s_sink;

    // Enough gen0 traffic to force a compacting collection, then one explicit collection so the test
    // never depends on the allocator's budget.
    private static void Churn()
    {
        for (int i = 0; i < 50_000; i++)
            s_sink = new byte[64];

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private struct Native
    {
        internal uint32 First;
        internal uint32 Second;
    }

    private struct Managed
    {
        internal slice<byte> Held;
    }

    private static ref uint32 nativeSecond(ref Native value) => ref value.Second;

    [TestMethod]
    public void HeapBoxAddressSurvivesACollection()
    {
        // `var done uint32; ... &done` — the shape every syscall out-parameter takes.
        ref var done = ref heap(new uint32(), out var box);
        nuint before = (nuint)(uintptr)box;

        Churn();

        Assert.AreEqual(before, (nuint)(uintptr)box, "heap box moved after its address was taken");

        // ...and the ref alias `heap` handed out is still the SAME storage the address names, which is
        // what makes the syscall's write visible to the caller (the slot must BE the storage, never a
        // mirror of it).
        done = 0x2A;
        Assert.AreEqual<uint32>(0x2A, box.Value);
        box.Value = 0x2B;
        Assert.AreEqual<uint32>(0x2B, done);
    }

    [TestMethod]
    public void SliceElementAddressSurvivesACollection()
    {
        // `&buf[0]` — the read/write buffer every syscall receives.
        slice<uint8> buf = new(4096);
        var element = Ꮡ(buf, 0);
        nuint before = (nuint)(uintptr)element;

        Churn();

        Assert.AreEqual(before, (nuint)(uintptr)element, "slice backing moved after &buf[0] was taken");

        // The address must name the real element, not a copy: a write through the box is visible in the
        // slice the caller still holds.
        element.Value = 0x5A;
        Assert.AreEqual<uint8>(0x5A, buf[0]);
    }

    [TestMethod]
    public void FixedArrayAddressSurvivesACollection()
    {
        // `unsafe.Pointer(&arr)` for a Go fixed array — already pinned before this change; guarded here
        // so the three address kinds are covered by one control.
        ref var arr = ref heap(new array<uint8>(64), out var box);
        nuint before = (nuint)(uintptr)box;

        Churn();

        Assert.AreEqual(before, (nuint)(uintptr)box, "fixed-array data moved after its address was taken");
        Assert.AreEqual<uint8>(0, arr[0]);
    }

    [TestMethod]
    public void StructFieldAddressSurvivesACollection()
    {
        // `&s.field` over a struct with a native layout: pinning the CONTAINER is what holds an
        // interior address still.
        ref var value = ref heap(new Native(), out var box);
        var field = box.of<uint32>(nativeSecond);
        nuint before = (nuint)(uintptr)field;

        Churn();

        Assert.AreEqual(before, (nuint)(uintptr)field, "struct storage moved after &s.field was taken");

        field.Value = 0x7C;
        Assert.AreEqual<uint32>(0x7C, value.Second);
    }

    [TestMethod]
    public void ReferenceBearingPointeeIsLeftAlone()
    {
        // A converted Go value carrying managed references has no native layout, so no syscall can be
        // handed its address and there is nothing to pin. It must still round-trip as an ordinary
        // pointer — the change is additive, never a new failure mode.
        ref var held = ref heap(new Managed { Held = new slice<byte>(4) }, out var box);

        held.Held[1] = 9;

        Assert.AreEqual<uint8>(9, box.Value.Held[1]);
        Assert.IsNull(((INilPointer)box).PinnableStorage);
    }
}
