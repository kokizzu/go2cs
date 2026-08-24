using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using @unsafe = go.unsafe_package;

namespace GolibTests;

[TestClass]
public unsafe class UnsafeSliceProvenanceTests
{
    // The slice consumer of the ratified pointer-provenance mechanism (DESIGN-pointer-provenance
    // §3; AUDIT-unsafe-slice-provenance). unsafe.Slice's native arm used to trust IsNative alone,
    // but IsNative answers "does this box carry a raw address", not "is that address native".
    //
    // The mechanism already closes the SAME-TYPED round trip upstream: `(ж<T>)(uintptr)` consults
    // Resolve and returns the ORIGINAL box — carriers intact, IsNative false — so that shape never
    // reaches the native arm at all (the first guard below pins that boundary, because this
    // consumer's correctness leans on it). What still reached the native arm was the CROSS-TYPED
    // round trip: `(ж<byte>)(uintptr)(address-of-pinned ж<ulong>)` resolves a box the `is ж<T>`
    // pattern cannot match, so the conversion built a native-flagged box over PINNED MANAGED
    // storage — whose pin is held only by the ORIGINAL box's liveness — and unsafe.Slice minted a
    // native-backed slice that dangles the moment that box is collected. The consult closes it: a
    // Resolve HIT of ANY type means managed-pinned, so the pointer falls through to the managed
    // arms (here the documented snapshot arm — reads exact, no dangle) instead of aliasing an
    // address whose pin it does not hold.

    [TestMethod]
    public void SameTypedRoundTripRecoversTheBoxBeforeTheNativeArm()
    {
        byte[] backing = { 10, 20, 30, 40, 50, 60 };
        slice<byte> source = new slice<byte>(backing);

        ж<byte> pointer = Ꮡ(source, 2);
        uintptr address = (uintptr)pointer;

        // The upstream half this consumer leans on: the reverse conversion resolves the pin and
        // hands back an aliasing box, so Slice takes the element-window arm.
        ж<byte> roundTripped = (ж<byte>)address;
        slice<byte> rebuilt = @unsafe.Slice(roundTripped, 3);

        Assert.IsFalse(rebuilt.IsNativeBacked, "the same-typed round trip must recover the managed box upstream");
        Assert.AreEqual(30, rebuilt[0]);

        rebuilt[1] = 99;
        Assert.AreEqual(99, backing[3], "the recovered window stopped aliasing the backing");
    }

    [TestMethod]
    public void CrossTypedRoundTripOverAPinnedBoxMustNotMintANativeBackedSlice()
    {
        // A heap box, pinned by its own uintptr conversion — the registration the mechanism makes
        // at the only moment the address is known to be managed-held-still.
        ж<ulong> box = new ж<ulong>(0x1122334455667788UL);
        uintptr address = (uintptr)box;

        // The CROSS-TYPED rebuild: Resolve answers the ж<ulong>, the ж<byte> pattern cannot take
        // it, and the result is a native-flagged box over the pinned slot.
        ж<byte> reinterpreted = (ж<byte>)address;
        Assert.IsTrue(reinterpreted.IsNative, "premise: the cross-typed rebuild carries the raw address");

        slice<byte> view = @unsafe.Slice(reinterpreted, 8);

        // The fix's whole content: a Resolve HIT of any type means managed-pinned, so this must
        // NOT be a native-backed slice — the pin belongs to `box`, and a native-backed slice
        // retains nothing that keeps it alive (DESIGN-pointer-provenance §3: the slice would keep
        // reading the old address after collection releases the pin).
        Assert.IsFalse(view.IsNativeBacked,
            "a native-backed slice over pinned managed storage drops the pin — the consult must " +
            "send a resolve HIT down the managed arms");

        // The managed fall-through lands in the documented snapshot arm for this carrier-less
        // shape: the bytes are exact.
        Assert.AreEqual((nint)8, view.Length);
        ulong reassembled = 0;
        for (int i = 7; i >= 0; i--)
            reassembled = (reassembled << 8) | view[i];
        Assert.AreEqual(0x1122334455667788UL, reassembled, "the snapshot did not read the pinned slot's real bytes");

        GC.KeepAlive(box);
    }

    [TestMethod]
    public void GenuinelyNativeAddressStillTakesTheNativeArm()
    {
        // The MISS control: no pin ever registered this address, so Resolve answers null and the
        // mapping arm must proceed exactly as before the consult — the audit's 13 native sites,
        // mmap first among them, depend on it.
        IntPtr buffer = Marshal.AllocHGlobal(8);

        try
        {
            for (int i = 0; i < 8; i++)
                Marshal.WriteByte(buffer, i, (byte)(0x50 + i));

            ж<byte> pointer = (void*)buffer;
            slice<byte> view = @unsafe.Slice(pointer, 8);

            Assert.IsTrue(view.IsNativeBacked,
                "a resolve MISS means genuinely native — the mapping arm must not have moved");
            Assert.AreEqual(0x50, view[0]);

            view[3] = 0xAB;
            Assert.AreEqual(0xAB, Marshal.ReadByte(buffer, 3), "the native mapping stopped being written through");
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }
}
