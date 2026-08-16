using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using go.golib;

namespace GolibTests;

[TestClass]
public class NativeReinterpretRefusalTests
{
    // Go's `(*U)(unsafe.Pointer(p))` reinterprets a pointer, and golib takes the ALIASING arm wherever
    // the managed model can express it (PointerExtensions.ReinterpretAliasesStorage). Where it cannot,
    // the derived pointer names its source by ADDRESS — and a pointer-to-ARRAY target over
    // differently-typed storage is always that case, because `array<U>` is a backing-store REFERENCE
    // plus bounds and no `U[]` view over a `V[]` exists in the managed model.
    //
    // Dereferencing such a box therefore reads an `array<U>` STRUCT — including its backing
    // reference — out of the pointed-at DATA. That fabricated reference is a CLR type-safety break
    // whose first use is an AccessViolationException: a process kill with no diagnostic, no
    // recover(), and a stack naming whichever consumer touched the reference rather than the
    // reinterpret that built the box.
    //
    // The witness was sha3's `ab := (*[25 * 64 / 8]byte)(unsafe.Pointer(&d.a))` over its `[25]uint64`
    // sponge state — `copy(b, ab[:])` faulted inside slice<byte>'s constructor, and crypto/tls reached
    // it on every TLS 1.3 ClientHello through mlkem768 key generation. That site is hand-owned now;
    // the CLASS is not closed, and these tests hold the line that it fails LOUDLY rather than fatally.
    //
    // Neutered-fix control: remove the s_nativeReadFabricatesReference guard in ж<T> and
    // RefusesToDereferenceAReferenceCarryingPointee kills the test host with an AccessViolation
    // instead of failing. It cannot be written as a Go-parity behavioral test — Go performs the
    // reinterpret successfully, so there is no Go program whose output records the refusal.

    [TestMethod]
    public void RefusesToDereferenceAReferenceCarryingPointee()
    {
        ref var data = ref heap(new array<uint64>(4), out var box);
        data[0] = 0x0102030405060708UL;

        // `(*[32]byte)(unsafe.Pointer(&arr))` — the sha3 shape, and the emission the converter
        // produces for it (see pointerReinterpretManagedSource's pointer-to-array exclusion).
        ж<array<byte>> reinterpreted = (ж<array<byte>>)(uintptr)box;

        PanicException panic = Assert.ThrowsException<PanicException>(() => _ = (~reinterpreted).Length);

        StringAssert.Contains(panic.Message, "cannot dereference a native address");

        // The message names the POINTEE, which is what points a reader at the offending reinterpret
        // rather than at the innocent consumer the AccessViolation used to name.
        StringAssert.Contains(panic.Message, "array<Byte>");
    }

    [TestMethod]
    public void RefusesThroughTheValueSlotToo()
    {
        // ValueSlot is Value without the nil check, and it reaches the same native branch — a
        // guard on only one of the two would leave the class open through closure-captured locals.
        ref var data = ref heap(new array<uint64>(4), out var box);
        data[0] = 1;

        ж<array<byte>> reinterpreted = (ж<array<byte>>)(uintptr)box;

        Assert.ThrowsException<PanicException>(() => _ = reinterpreted.ValueSlot);
    }

    [TestMethod]
    public void PureDataPointeeStillDereferencesNatively()
    {
        // The guard is about FABRICATING a reference, not about native memory: a pointee whose layout
        // is pure data reads through unchanged, which is what every syscall out-parameter and every
        // GetEnvironmentStringsW-style walk depends on. Same source storage, same address route.
        ref var data = ref heap(new array<uint64>(4), out var box);
        data[0] = 0x0102030405060708UL;

        ж<uint64> asWord = (ж<uint64>)(uintptr)box;

        Assert.AreEqual(0x0102030405060708UL, asWord.Value);

        ж<byte> asByte = (ж<byte>)(uintptr)box;

        // Little-endian hosts read the low byte first; the point is only that the read HAPPENS.
        Assert.IsTrue(asByte.Value is 0x08 or 0x01);
    }

    [TestMethod]
    public void DisplayNeverRaisesTheRefusal()
    {
        // A diagnostic must not be the thing that fails a program. `ToString()` on a pointer routes
        // through PrintPointer, which reaches the pointee for its address token — so a refused box
        // would raise the panic exactly when someone printed it while trying to ROOT the panic.
        ref var data = ref heap(new array<uint64>(4), out var box);
        data[0] = 1;

        ж<array<byte>> reinterpreted = (ж<array<byte>>)(uintptr)box;

        string token = reinterpreted.ToString();

        StringAssert.StartsWith(token, "0x");
        Assert.AreNotEqual("0x0", token);
    }

    [TestMethod]
    public void AddressUseIsUnaffected()
    {
        // A refused pointee is still a perfectly good ADDRESS — Go's `uintptr(unsafe.Pointer(p))`
        // round-trip must keep working, since the refusal is at the dereference and nowhere else.
        ref var data = ref heap(new array<uint64>(4), out var box);

        ж<array<byte>> reinterpreted = (ж<array<byte>>)(uintptr)box;

        Assert.AreNotEqual((nuint)0, (nuint)(uintptr)reinterpreted);
        Assert.AreEqual((nuint)(uintptr)box, (nuint)(uintptr)reinterpreted);
    }
}
