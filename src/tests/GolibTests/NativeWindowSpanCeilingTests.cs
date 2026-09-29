using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

/// <summary>
/// Guards the Int32 ceiling of a slice's WINDOW (CENSUS-golib-int32-narrowings-2026-09-29, S4, S4b
/// and E1). A span cannot be longer than Int32, and a managed <c>T[]</c> cannot be longer than
/// <see cref="Array.MaxLength"/>, so two Go-legal shapes have no faithful answer here:
/// <list type="bullet">
/// <item>a native window of 2^32 + k elements. <c>ToSpan</c> narrowed its length with <c>(int)</c>,
/// so every bulk read saw k elements, silently. It must be refused BY NAME at the window's door, as
/// <c>MakeChanSizeBeyondManagedBuffer</c> refuses a channel buffer: a platform bound of go2cs.</item>
/// <item>an <c>append</c> whose grown length passes <see cref="Array.MaxLength"/>. The CLR's
/// OverflowException or OutOfMemoryException escaped <c>recover()</c>; Go's growslice raises its own
/// recoverable <c>growslice: len out of range</c> for a request it cannot allocate.</item>
/// </list>
/// </summary>
/// <remarks>
/// Every window here is minted over a 16-byte block and NEVER read past it. The refusals run before
/// any element is touched, and the append shapes need growth, which fails at the length check (or,
/// before the fix, at the array allocation) before the source window is copied.
/// </remarks>
[TestClass]
public class NativeWindowSpanCeilingTests
{
    private const int BlockBytes = 16;

    // 2^32 + k: the length the (int) narrowing wrapped to k.
    private const long K = 5;
    private const long WrappingLength = (1L << 32) + K;

    private const string GrowSliceText = "runtime error: growslice: len out of range";

    private static nuint AllocBlock()
    {
        nuint addr = (nuint)(nint)Marshal.AllocHGlobal(BlockBytes);

        unsafe
        {
            new Span<byte>((void*)addr, BlockBytes).Fill(0xA5);
        }

        return addr;
    }

    private static Exception? Capture(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    private static void AssertNamedRefusal(Exception? thrown, string arm, string readNote)
    {
        Assert.IsNotNull(thrown, $"{arm}: a native window of 2^32+{K} elements must be refused by name, but {readNote}");
        Assert.IsInstanceOfType(thrown, typeof(PanicException), $"{arm}: must be a recoverable PanicException, was {thrown.GetType().Name}: {thrown.Message}");
        StringAssert.Contains(thrown.Message, "a platform bound of go2cs", $"{arm}: the refusal must name the platform bound");
    }

    [TestMethod]
    public void ANativeWindowPastTheSpanCeilingIsRefusedAtTheDoorAndDoesNotReadKElements()
    {
        nuint addr = AllocBlock();

        try
        {
            int read = -1;

            Exception? thrown = Capture(() =>
            {
                global::go.slice<byte> window = global::go.slice<byte>.OverNativeMemory(addr, (nint)WrappingLength);
                read = window.ToSpan().Length;
            });

            AssertNamedRefusal(thrown, "OverNativeMemory", $"a bulk read saw {read} elements");
        }
        finally
        {
            Marshal.FreeHGlobal((nint)addr);
        }
    }

    [TestMethod]
    public void ReslicingANativeWindowPastTheSpanCeilingIsRefusedAndDoesNotReadKElements()
    {
        nuint addr = AllocBlock();

        try
        {
            // A short window over a long reservation (the header-slice rebase's (len, cap) shape) is
            // legal and stays accepted; cutting a window of 2^32+k elements out of it is the refusal.
            global::go.slice<byte> reserved = global::go.slice<byte>.OverNativeMemory(addr, (nint)K, (nint)WrappingLength);
            Assert.AreEqual((nint)K, reserved.Length);

            int read = -1;

            Exception? thrown = Capture(() =>
            {
                global::go.slice<byte> window = reserved.Reslice(0, (nint)WrappingLength, (nint)WrappingLength);
                read = window.ToSpan().Length;
            });

            AssertNamedRefusal(thrown, "Reslice", $"a bulk read saw {read} elements");
        }
        finally
        {
            Marshal.FreeHGlobal((nint)addr);
        }
    }

    [TestMethod]
    public void AnAppendThatGrowsPastArrayMaxLengthRaisesGosRecoverableGrowslicePanic()
    {
        nuint addr = AllocBlock();

        try
        {
            // len == cap == Array.MaxLength: the largest window a span can express, full, so any
            // append must grow to Array.MaxLength + 1 elements.
            global::go.slice<byte> full = global::go.slice<byte>.OverNativeMemory(addr, Array.MaxLength);

            Exception? thrown = Capture(() => _ = global::go.slice<byte>.Append(full, (byte)1));

            Assert.IsNotNull(thrown, "append past Array.MaxLength must panic");
            Assert.IsInstanceOfType(thrown, typeof(PanicException), $"append past Array.MaxLength must be recover()-able, was {thrown.GetType().Name}: {thrown.Message}");
            Assert.AreEqual(GrowSliceText, thrown.Message, "Go's own growslice text");
        }
        finally
        {
            Marshal.FreeHGlobal((nint)addr);
        }
    }

    [TestMethod]
    public void AnAppendOfZeroesThatGrowsPastArrayMaxLengthRaisesGosRecoverableGrowslicePanic()
    {
        nuint addr = AllocBlock();

        try
        {
            global::go.slice<byte> full = global::go.slice<byte>.OverNativeMemory(addr, Array.MaxLength);

            // append(full, make([]byte, 1)...), the REC-C spread the converter emits for it.
            Exception? thrown = Capture(() => _ = builtin.appendꓸꓸꓸ(full, builtin.makeꓸꓸꓸ<byte>(1)));

            Assert.IsNotNull(thrown, "append of zeroes past Array.MaxLength must panic");
            Assert.IsInstanceOfType(thrown, typeof(PanicException), $"append of zeroes past Array.MaxLength must be recover()-able, was {thrown.GetType().Name}: {thrown.Message}");
            Assert.AreEqual(GrowSliceText, thrown.Message, "Go's own growslice text");
        }
        finally
        {
            Marshal.FreeHGlobal((nint)addr);
        }
    }

    // Not run RED: before the refusal, both appends WROTE Array.MaxLength elements past a 16-byte
    // block, into memory this test does not own. The refusal must come before the write.
    [TestMethod]
    public void AnAppendInPlacePastTheSpanCeilingIsRefusedBeforeItWrites()
    {
        nuint addr = AllocBlock();

        try
        {
            // len == Array.MaxLength (the longest window accepted) over a reservation of 2^32+k.
            global::go.slice<byte> window = global::go.slice<byte>.OverNativeMemory(addr, Array.MaxLength, (nint)WrappingLength);
            Assert.AreEqual((nint)Array.MaxLength, window.Length, "the longest window a span can express is accepted");

            AssertNamedRefusal(Capture(() => _ = global::go.slice<byte>.Append(window, (byte)1)), "Append in place", "it was accepted");
            AssertNamedRefusal(Capture(() => _ = builtin.appendꓸꓸꓸ(window, builtin.makeꓸꓸꓸ<byte>(1))), "AppendZeroed in place", "it was accepted");
        }
        finally
        {
            Marshal.FreeHGlobal((nint)addr);
        }
    }

    [TestMethod]
    public void GrowthThatFitsClampsItsCapacityToArrayMaxLength()
    {
        nuint addr = AllocBlock();

        try
        {
            // One short of the ceiling, full: the growth rule's capacity (1.25x) passes Array.MaxLength
            // while the needed length does not. Only the capacity is computed; nothing is allocated.
            global::go.slice<byte> nearlyFull = global::go.slice<byte>.OverNativeMemory(addr, Array.MaxLength - 1);

            Assert.AreEqual((nint)Array.MaxLength, global::go.slice<byte>.GrowCapacity(nearlyFull, Array.MaxLength), "a length that fits clamps to the ceiling");

            Exception? thrown = Capture(() => _ = global::go.slice<byte>.GrowCapacity(nearlyFull, (nint)Array.MaxLength + 1));
            Assert.IsInstanceOfType(thrown, typeof(PanicException));
            Assert.AreEqual(GrowSliceText, thrown!.Message);
        }
        finally
        {
            Marshal.FreeHGlobal((nint)addr);
        }

        // Control: below the ceiling the growth rule is untouched (len 4, cap 4, one more: 8).
        global::go.slice<byte> small = new(4);
        Assert.AreEqual((nint)8, global::go.slice<byte>.GrowCapacity(small, 5));
    }
}
