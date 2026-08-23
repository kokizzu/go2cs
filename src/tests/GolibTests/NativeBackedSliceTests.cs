using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using static go.builtin;
using @unsafe = go.unsafe_package;

namespace GolibTests;

[TestClass]
public class NativeBackedSliceTests
{
    // The W1b commission's guard family (DESIGN-native-backed-slice.md §5, ratified 2026-08-22).
    // The measured defect these pin closed: `unsafe.Slice` over a NATIVE pointer SNAPSHOTTED into
    // a managed slice — R's probe: `Mmap` returned twelve kilobytes that were not the mapping,
    // `Mprotect`/`Munmap` handed the kernel managed element addresses, and writes landed in a copy
    // the kernel never saw. The ratified shape: `slice<T>` carries a native backing beside the
    // managed one (the ж<T> dual-mode precedent), `unsafe.Slice`'s IsNative arm ALIASES, and
    // `Ꮡ(s, i)`/`SliceData` yield REAL addresses.
    //
    // The tests drive unmanaged heap memory (AllocHGlobal) rather than mmap so they are
    // platform-neutral: the semantics under test are the slice model's, not the kernel's.

    private static ж<byte> NativePointer(IntPtr buffer)
    {
        // The documented address-model door: golib's void* operator constructs a NATIVE-backed
        // ж aliasing the exact address (the #159 native-slot doctrine).
        unsafe
        {
            ж<byte> pointer = (void*)buffer;
            return pointer;
        }
    }

    [TestMethod]
    public void WritesThroughANativeSliceReachTheMemory()
    {
        IntPtr buffer = Marshal.AllocHGlobal(64);
        try
        {
            for (int i = 0; i < 64; i++)
                Marshal.WriteByte(buffer, i, 0xDD);

            slice<byte> view = @unsafe.Slice(NativePointer(buffer), 64);
            view[3] = 0xAB;
            view[63] = 0xCD;

            Assert.AreEqual(0xAB, Marshal.ReadByte(buffer, 3),
                "a write through the slice did not reach the native memory — the snapshot shape this family pins closed");
            Assert.AreEqual(0xCD, Marshal.ReadByte(buffer, 63), "the last element's write was swallowed");

            Marshal.WriteByte(buffer, 10, 0x77);
            Assert.AreEqual(0x77, view[10], "a native write was not visible through the slice — reads are of a copy");
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    [TestMethod]
    public void ElementAddressesAreTheRealOnes()
    {
        IntPtr buffer = Marshal.AllocHGlobal(32);
        try
        {
            slice<byte> view = @unsafe.Slice(NativePointer(buffer), 32);

            // The acceptance case verbatim: Mprotect(b[:pagesize]) hands the kernel
            // uintptr(unsafe.Pointer(&b[0])) — the address must be the mapping's, exactly.
            Assert.AreEqual((nuint)buffer, (nuint)(uintptr)Ꮡ(view, 0), "Ꮡ(s, 0) is not the base address");
            Assert.AreEqual((nuint)buffer + 7, (nuint)(uintptr)Ꮡ(view, 7), "Ꮡ(s, 7) is not base+7");

            slice<byte> window = view[8..16];
            Assert.AreEqual((nuint)buffer + 8, (nuint)(uintptr)Ꮡ(window, 0), "a resliced window's element address must track the offset");
            Assert.AreEqual((nuint)buffer + 8, (nuint)(uintptr)@unsafe.SliceData(window), "SliceData must be the interior pointer, exactly");
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    [TestMethod]
    public void AppendPastCapacityDetachesToManaged()
    {
        IntPtr buffer = Marshal.AllocHGlobal(8);
        try
        {
            for (int i = 0; i < 8; i++)
                Marshal.WriteByte(buffer, i, (byte)i);

            slice<byte> view = @unsafe.Slice(NativePointer(buffer), 8);
            slice<byte> grown = append(view, (byte)0xFF);

            // Go's own spec: append past cap returns a NEW backing; the old slice still aliases
            // the original storage and the mapping is untouched past its end by the append.
            grown[0] = 0x42;
            Assert.AreEqual(0x00, Marshal.ReadByte(buffer, 0),
                "append past capacity must DETACH — the write through the grown slice reached the mapping");
            Assert.AreEqual(0x42, grown[0]);
            Assert.AreEqual(0xFF, grown[8]);
            Assert.AreEqual(9, (int)len(grown));

            view[1] = 0x99;
            Assert.AreEqual(0x99, Marshal.ReadByte(buffer, 1), "the ORIGINAL slice must still alias the mapping after a detaching append");
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    [TestMethod]
    public void CopyCrossesBothBackings()
    {
        IntPtr buffer = Marshal.AllocHGlobal(16);
        try
        {
            slice<byte> native = @unsafe.Slice(NativePointer(buffer), 16);
            slice<byte> managed = new byte[16];

            for (int i = 0; i < 16; i++)
                managed[i] = (byte)(0xA0 + i);

            copy(native, managed);
            Assert.AreEqual(0xA5, Marshal.ReadByte(buffer, 5), "managed→native copy did not reach the memory");

            Marshal.WriteByte(buffer, 6, 0x11);
            slice<byte> back = new byte[16];
            copy(back, native);
            Assert.AreEqual(0x11, back[6], "native→managed copy read a stale value");
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    [TestMethod]
    public void ManagedAliasingStaysExactlyAsItWas()
    {
        // The crypto/subtle regression guard from the design's §5: unsafe.Slice(&s[i], n) over
        // MANAGED storage aliases it, and this change must not disturb that arm.
        slice<byte> source = new byte[8];
        slice<byte> window = @unsafe.Slice(Ꮡ(source, 2), 4);

        window[0] = 0x5A;
        Assert.AreEqual(0x5A, source[2], "the managed aliasing arm regressed — crypto/subtle's shape");
    }
}
