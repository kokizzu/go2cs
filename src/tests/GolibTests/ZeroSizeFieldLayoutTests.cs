using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

[TestClass]
public class ZeroSizeFieldLayoutTests
{
    // Ruling A's second half: a Go struct containing a ZERO-SIZE field (`struct{}`, `[0]T`) is
    // smaller in Go than its naive C# surrogate, because a C# field always occupies at least one
    // byte. `sync/atomic.Int32` is `struct{ _ noCopy; v int32 }` -- 4 bytes in Go, 8 in C# once the
    // empty `noCopy` takes a byte and `v` is pushed to offset 4 by alignment.
    //
    // That difference is not cosmetic: `Reinterpret`'s size guard (correct, and untouchable per the
    // ruling) admits an alias only when `SizeOf(TDst) <= SizeOf(T)`, so the hammer family's
    // `(*Int32)(unsafe.Pointer(uaddr))` over a `*uint32` is refused at 8 > 4 and falls to the
    // address route. The remedy is explicit layout carrying Go's OWN offsets.
    //
    // These guard the MECHANISM at golib level -- that the emitted shape really does produce Go's
    // size and really does satisfy the guard -- so a converter or hand-own change that regresses it
    // fails here rather than only in a package suite.

    private struct EmptyMarker
    {
    }

    // The naive shape: what a zero-size field costs when it is laid out sequentially.
    private struct NaiveInt32
    {
        internal EmptyMarker _;
        internal int v;
    }

    // The emitted shape: Go's offsets, Go's size. The zero-size field is placed AT the offset Go
    // gives it, which is the same offset as the field that follows it -- legal precisely because
    // neither participant is a managed reference.
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    private struct GoLaidOutInt32
    {
        [FieldOffset(0)] internal EmptyMarker _;
        [FieldOffset(0)] internal int v;
    }

    [StructLayout(LayoutKind.Explicit, Size = 8)]
    private struct GoLaidOutInt64
    {
        [FieldOffset(0)] internal EmptyMarker _;
        [FieldOffset(0)] internal EmptyMarker __;
        [FieldOffset(0)] internal long v;
    }

    [TestMethod]
    public void NaiveLayoutIsLargerThanGoAndIsWhatTheGuardRefuses()
    {
        // The defect, stated as a measurement rather than an assumption.
        Assert.AreEqual(8, Unsafe.SizeOf<NaiveInt32>(), "a sequentially laid out zero-size field costs a byte plus alignment padding");
        Assert.IsTrue(Unsafe.SizeOf<NaiveInt32>() > sizeof(uint), "which is exactly why Reinterpret refuses the Go-legal alias");
    }

    [TestMethod]
    public void GoLaidOutStructMatchesGoSize()
    {
        Assert.AreEqual(4, Unsafe.SizeOf<GoLaidOutInt32>(), "explicit layout with Go's offsets must reproduce Go's size");
        Assert.AreEqual(8, Unsafe.SizeOf<GoLaidOutInt64>(), "two zero-size fields still cost nothing");
    }

    [TestMethod]
    public void GoLaidOutStructSatisfiesTheReinterpretSizeGuard()
    {
        // The point of the whole arc: the alias the hammer family needs is admitted, with the size
        // guard itself untouched.
        ref uint storage = ref heap(0xFEEDFACEu, out ж<uint> Ꮡstorage);
        _ = storage;

        ж<GoLaidOutInt32> aliased = Ꮡstorage.Reinterpret<uint, GoLaidOutInt32>();

        Assert.AreEqual(unchecked((int)0xFEEDFACE), aliased.Value.v, "the reinterpreted pointer must READ the source's storage");

        // ... and WRITE through it, which is what makes the hammer tests' atomics land on the
        // uint32 the test is hammering rather than on a detached copy.
        aliased.Value.v = 0x0BADC0DE;
        Assert.AreEqual(0x0BADC0DEu, Ꮡstorage.Value, "the alias must share storage with its source, not copy it");
    }

    // The SHARP EDGE the ruling's mechanism does not anticipate, pinned as a measurement so the
    // next reader meets it as a known fact rather than as a corruption bug.
    //
    // C# has no zero-size struct: an empty struct occupies ONE byte. Laid out at Go's offset it
    // therefore SHARES bytes with the field Go puts at that same offset, and writing it writes a
    // real zero byte over its neighbour. Go's write writes nothing. So the overlap is faithful for
    // reads, for whole-struct copies and for size -- and unfaithful for exactly one operation.
    [TestMethod]
    public void WritingAnOverlappedZeroSizeFieldCLOBBERSItsNeighbour()
    {
        GoLaidOutInt32 value = default;
        value.v = 42;
        value._ = default;

        Assert.AreEqual(0, value.v,
            "measured: assigning an overlapped zero-size field writes its one C# byte over the neighbour " +
            "(42 -> 0 on little-endian). This is why the emitted field must be readonly.");
    }

    // The remedy that keeps BOTH properties: the field stays declared -- so reflect's field walk,
    // NumField() and StructField.Offset still match Go -- while `readonly` makes the one unfaithful
    // operation unexpressible in converted code. A whole-struct assignment still writes all Size
    // bytes and stays correct, which is the only write Go itself performs here.
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    private struct GoLaidOutReadonlyInt32
    {
        [FieldOffset(0)] internal readonly EmptyMarker _;
        [FieldOffset(0)] internal int v;
    }

    [TestMethod]
    public void ReadonlyZeroSizeFieldKeepsGoSizeAndCannotClobber()
    {
        Assert.AreEqual(4, Unsafe.SizeOf<GoLaidOutReadonlyInt32>(), "the readonly form keeps Go's size");

        GoLaidOutReadonlyInt32 value = default;
        value.v = 42;

        // `value._ = default;` is a COMPILE error here (CS0191), which is the point: the clobber is
        // removed by the type system rather than by convention.
        Assert.AreEqual(42, value.v, "the neighbour survives because nothing can write the zero-size field");

        // A whole-struct write still behaves -- it writes all four bytes, exactly as Go's does.
        value = default;
        Assert.AreEqual(0, value.v, "a whole-struct assignment still clears the struct");
    }
}
