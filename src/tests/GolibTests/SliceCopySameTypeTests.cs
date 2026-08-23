using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using static go.builtin;
using @unsafe = go.unsafe_package;

namespace GolibTests;

[TestClass]
public unsafe class SliceCopySameTypeTests
{
    // C2 (span-unification census, tranche 1): the identical-element `copy` -- the only shape
    // converted Go emits -- now takes one span copy serving BOTH backings, instead of forking into
    // Array.Copy for managed and a per-element Unsafe.As loop for native. These pin the properties
    // the merge has to preserve: window correctness on each side, Go's permitted overlap in both
    // directions, and the native arm still reading and writing the real pages.

    private static ж<byte> NativePointer(IntPtr buffer)
    {
        ж<byte> pointer = (void*)buffer;
        return pointer;
    }

    [TestMethod]
    public void CopyRespectsBothWindows()
    {
        int[] backing = { 0, 0, 0, 0, 0, 0 };
        slice<int> destination = new slice<int>(backing)[1..4];
        slice<int> source = new slice<int>(new[] { 5, 6, 7, 8, 9 })[2..5];

        nint copied = copy(destination, source);

        Assert.AreEqual((nint)3, copied);
        Assert.AreEqual(0, backing[0], "wrote before the destination window");
        Assert.AreEqual(7, backing[1]);
        Assert.AreEqual(8, backing[2]);
        Assert.AreEqual(9, backing[3]);
        Assert.AreEqual(0, backing[4], "wrote after the destination window");
    }

    [TestMethod]
    public void CopyStopsAtTheShorterOfTheTwo()
    {
        slice<int> destination = new slice<int>(new int[2]);
        slice<int> source = new slice<int>(new[] { 1, 2, 3, 4 });

        Assert.AreEqual((nint)2, copy(destination, source));
        Assert.AreEqual(2, destination[1]);

        slice<int> wide = new slice<int>(new int[4]);
        slice<int> narrow = new slice<int>(new[] { 1, 2 });

        Assert.AreEqual((nint)2, copy(wide, narrow));
        Assert.AreEqual(0, wide[2], "copy ran past the end of the source");
    }

    // Go's copy explicitly permits overlapping operands, so the transfer must behave as if the
    // source were read in full before any of it was written -- memmove, not a naive forward loop.
    [TestMethod]
    public void OverlappingCopyShiftingLeftIsMemmoveCorrect()
    {
        int[] backing = { 1, 2, 3, 4, 5 };
        slice<int> whole = new slice<int>(backing);

        // copy(b[0:4], b[1:5]) -- destination starts BEFORE the source.
        copy(whole[0..4], whole[1..5]);

        CollectionAssert.AreEqual(new[] { 2, 3, 4, 5, 5 }, backing);
    }

    [TestMethod]
    public void OverlappingCopyShiftingRightIsMemmoveCorrect()
    {
        int[] backing = { 1, 2, 3, 4, 5 };
        slice<int> whole = new slice<int>(backing);

        // copy(b[1:5], b[0:4]) -- destination starts AFTER the source, the direction a naive
        // forward element loop corrupts.
        copy(whole[1..5], whole[0..4]);

        CollectionAssert.AreEqual(new[] { 1, 1, 2, 3, 4 }, backing);
    }

    [TestMethod]
    public void CopyIntoANativeSliceReachesTheRealMemory()
    {
        IntPtr buffer = Marshal.AllocHGlobal(16);
        try
        {
            for (int i = 0; i < 16; i++)
                Marshal.WriteByte(buffer, i, 0);

            slice<byte> native = @unsafe.Slice(NativePointer(buffer), 16);
            slice<byte> managed = new slice<byte>(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

            nint copied = copy(native, managed);

            Assert.AreEqual((nint)8, copied);
            Assert.AreEqual(1, Marshal.ReadByte(buffer, 0), "the copy did not reach the native pages");
            Assert.AreEqual(8, Marshal.ReadByte(buffer, 7));
            Assert.AreEqual(0, Marshal.ReadByte(buffer, 8), "the copy ran past the source length");
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    [TestMethod]
    public void CopyOutOfANativeSliceReadsTheRealMemory()
    {
        IntPtr buffer = Marshal.AllocHGlobal(16);
        try
        {
            for (int i = 0; i < 16; i++)
                Marshal.WriteByte(buffer, i, (byte)(0xA0 + i));

            slice<byte> native = @unsafe.Slice(NativePointer(buffer), 16);
            slice<byte> managed = new slice<byte>(new byte[4]);

            nint copied = copy(managed, native);

            Assert.AreEqual((nint)4, copied);
            Assert.AreEqual(0xA0, managed[0], "the copy did not read the native pages");
            Assert.AreEqual(0xA3, managed[3]);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    [TestMethod]
    public void NativeToNativeCopyRespectsWindows()
    {
        IntPtr source = Marshal.AllocHGlobal(16);
        IntPtr destination = Marshal.AllocHGlobal(16);
        try
        {
            for (int i = 0; i < 16; i++)
            {
                Marshal.WriteByte(source, i, (byte)(i + 1));
                Marshal.WriteByte(destination, i, 0);
            }

            slice<byte> from = @unsafe.Slice(NativePointer(source), 16)[4..8];
            slice<byte> into = @unsafe.Slice(NativePointer(destination), 16)[2..6];

            Assert.AreEqual((nint)4, copy(into, from));
            Assert.AreEqual(0, Marshal.ReadByte(destination, 1), "wrote before the destination window");
            Assert.AreEqual(5, Marshal.ReadByte(destination, 2), "the source window was not honored");
            Assert.AreEqual(8, Marshal.ReadByte(destination, 5));
            Assert.AreEqual(0, Marshal.ReadByte(destination, 6), "wrote after the destination window");
        }
        finally
        {
            Marshal.FreeHGlobal(source);
            Marshal.FreeHGlobal(destination);
        }
    }

    // Reference elements share the identical-type arm with value elements, and the span re-spelling
    // must not disturb them.
    [TestMethod]
    public void CopyOfReferenceElementsCarriesTheSameInstances()
    {
        object first = new();
        object second = new();
        slice<object> source = new slice<object>(new[] { first, second });
        slice<object> destination = new slice<object>(new object[2]);

        copy(destination, source);

        Assert.AreSame(first, destination[0]);
        Assert.AreSame(second, destination[1]);
    }
}
