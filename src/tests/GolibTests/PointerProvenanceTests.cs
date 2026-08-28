using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

[TestClass]
public class PointerProvenanceTests
{
    // The provenance record's one consumer contract: the address a `ж→uintptr` conversion
    // registered at its pin moment MUST resolve back to the box that pinned it, because the
    // keystone tether (Syscall6's prologue, syscall_linux_impl.cs) re-roots every pointer-valued
    // syscall argument by resolving exactly that number. An entry that registers but never
    // resolves leaves its argument class untethered: the box retires at JIT liveness end, the
    // pin's finalizer releases the storage mid-syscall, and the kernel's write lands on recycled
    // heap — the measured 2026-08-26 corruption shape (a live object's MethodTable zeroed, the
    // GC's own mark phase the victim).

    [TestMethod]
    public void FixedArrayBufferAddressResolvesToItsBox()
    {
        // `unsafe.Pointer(&buf)` over a Go fixed array converts to the pinned DATA address
        // (element 0 of the backing array — a DIFFERENT allocation than the box's value slot),
        // and the conversion registers that address as provenance. Failing-first record
        // (2026-08-28): validate-on-read compared only `&ValueSlot`, so this resolve answered
        // null and the tether was blind to every fixed-array buffer argument — pipe2's
        // `*[2]int32`, readlinkat's `*[N]byte` — exactly the pipe-buffer-load smash HeapVerify
        // caught when the registration was first added without this answering arm.
        var buf = new array<int>(2);
        var box = Ꮡ(buf);

        uintptr addr = (uintptr)box;

        object? resolved = ManagedPointerTokens.Resolve((nuint)addr.Value);

        Assert.AreSame(box, resolved,
            "a fixed-array buffer's registered pin address must resolve to the box that pinned it — the syscall tether cannot re-root this argument class otherwise");
        GC.KeepAlive(box);
    }

    [TestMethod]
    public void StructSlotAddressResolvesToItsBox()
    {
        // The control: the non-array pin path (EnsureStableAddress over the T[1] slot) registers
        // `&ValueSlot` itself, the very address validate-on-read re-derives — this class was
        // never blind, and the fix must not disturb it.
        var box = Ꮡ<long>(42);

        uintptr addr = (uintptr)box;

        object? resolved = ManagedPointerTokens.Resolve((nuint)addr.Value);

        Assert.AreSame(box, resolved, "a struct slot's pinned address must resolve to its box");
        GC.KeepAlive(box);
    }

    [TestMethod]
    public void UnregisteredNumberStillMisses()
    {
        // The MISS half of the OQ-P2 contract survives the answering arm: a number no conversion
        // ever handed out must not resolve, because it may be a real native address.
        Assert.IsNull(ManagedPointerTokens.Resolve((nuint)0x4242),
            "an address this process never pinned must MISS");
    }
}
