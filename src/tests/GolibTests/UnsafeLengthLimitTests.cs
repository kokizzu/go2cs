using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using @unsafe = go.unsafe_package;

namespace GolibTests;

// unsafe.Slice and unsafe.String reduced Go's length with int.CreateTruncating, so a length past Int32
// WRAPPED: 3 GiB went negative ("len is negative", which is how runtime's TestMemmoveOverflow died) and
// 4 GiB + 5 silently produced a 5-element slice over the wrong extent. slice<T> here is built over CLR
// arrays and spans whose length is Int32, so a longer length has no representation; the ruled answer is
// a NAMED refusal rather than a wrapped length (coordinator ruling 2026-09-29 11:33). The negative and
// nil checks stay as they were.
[TestClass]
public unsafe class UnsafeLengthLimitTests
{
    private static slice<byte> Source() => new slice<byte>(new byte[16]);

    private static void AssertRefused(Action call, object length, string function)
    {
        PanicException ex = Assert.ThrowsException<PanicException>(call, "a length past Int32 must be refused, not wrapped");
        StringAssert.Contains(ex.Message, $"{function}: length {length} exceeds the maximum slice length of this runtime");
    }

    [TestMethod]
    public void SliceRefusesALengthThatWouldWrapToAShortSlice()
    {
        slice<byte> source = Source();
        long length = (1L << 32) + 5;

        AssertRefused(() => @unsafe.Slice(Ꮡ(source, 0), length), length, "unsafe.Slice");
    }

    [TestMethod]
    public void SliceRefusesThreeGiBInsteadOfReportingItNegative()
    {
        slice<byte> source = Source();
        long length = 3L << 30;

        AssertRefused(() => @unsafe.Slice(Ꮡ(source, 0), length), length, "unsafe.Slice");
    }

    [TestMethod]
    public void StringRefusesALengthThatWouldWrapToAShortString()
    {
        slice<byte> source = Source();
        long length = (1L << 32) + 5;

        AssertRefused(() => @unsafe.String(Ꮡ(source, 0), length), length, "unsafe.String");
    }

    [TestMethod]
    public void StringRefusesThreeGiBInsteadOfReportingItNegative()
    {
        slice<byte> source = Source();
        long length = 3L << 30;

        AssertRefused(() => @unsafe.String(Ꮡ(source, 0), length), length, "unsafe.String");
    }

    [TestMethod]
    public void AnUnsignedLengthPastInt32IsRefusedToo()
    {
        slice<byte> source = Source();
        ulong length = (1UL << 63) + 1;

        AssertRefused(() => @unsafe.Slice(Ꮡ(source, 0), length), length, "unsafe.Slice");
    }

    [TestMethod]
    public void ANegativeLengthStillPanicsAsNegative()
    {
        slice<byte> source = Source();

        PanicException ex = Assert.ThrowsException<PanicException>(() => @unsafe.Slice(Ꮡ(source, 0), -1L));
        StringAssert.Contains(ex.Message, "len is negative");
    }

    [TestMethod]
    public void AnOrdinaryLengthStillWorks()
    {
        slice<byte> source = Source();

        Assert.AreEqual(4, len(@unsafe.Slice(Ꮡ(source, 0), 4L)));
        Assert.AreEqual(4, len(@unsafe.String(Ꮡ(source, 0), 4L)));
    }

    // unsafe.Add over a typed box reduced its offset with int.CreateTruncating as well, so an offset of
    // 2^32 + 8 added 8 to a native pointer (the i9's golib Int32 census, S3). The Pointer overload goes
    // through nint and wraps like Go's uintptr, which is faithful and is left alone.
    [TestMethod]
    public void AddRefusesAnOffsetPastInt32OnANativePointer()
    {
        IntPtr buffer = Marshal.AllocHGlobal(16);

        try
        {
            ж<byte> pointer = (void*)buffer;
            long offset = (1L << 32) + 8;

            PanicException ex = Assert.ThrowsException<PanicException>(() => @unsafe.Add(pointer, offset), "an offset past Int32 must be refused, not wrapped to 8");
            StringAssert.Contains(ex.Message, $"unsafe.Add: offset {offset} exceeds what this runtime can address through a typed element box");
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    [TestMethod]
    public void AddRefusesAnOffsetPastInt32OnAnElementBox()
    {
        slice<byte> source = Source();
        long offset = (1L << 32) + 1;

        PanicException ex = Assert.ThrowsException<PanicException>(() => @unsafe.Add(Ꮡ(source, 0), offset));
        StringAssert.Contains(ex.Message, $"unsafe.Add: offset {offset} exceeds what this runtime can address through a typed element box");
    }

    [TestMethod]
    public void AddStillStepsForwardAndBackWithinInt32()
    {
        IntPtr buffer = Marshal.AllocHGlobal(16);

        try
        {
            ж<byte> pointer = (void*)buffer;
            ж<byte> ahead = @unsafe.Add(pointer, 4L);
            ж<byte> back = @unsafe.Add(ahead, -4L);

            Assert.AreEqual((nuint)buffer + 4, ahead.NativeAddress);
            Assert.AreEqual((nuint)buffer, back.NativeAddress);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }
}
