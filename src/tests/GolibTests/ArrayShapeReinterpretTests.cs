using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

/// <summary>
/// The array-SHAPE reinterpret seam, measured against golib directly — the kernel-free witness for
/// <c>docs/phase4/DESIGN-native-array-view.md</c>, and the guard on the remedy the corpus uses today.
/// </summary>
/// <remarks>
/// <para>
/// Go's <c>internal/chacha8rand</c> opens one <c>[32]uint64</c> allocation as
/// <c>(*[16][4]uint32)(unsafe.Pointer(buf))</c> and then as <c>(*[16][2]uint64)(…)</c> — a
/// differently-TYPED and differently-RANKED view of the same bytes. <c>array&lt;T&gt;</c> is a
/// window on a real <c>T[]</c>, so a nested <c>uint32</c> view over a <c>ulong[]</c> has no managed
/// spelling; the literal conversion takes the raw-ADDRESS route and dereferences an
/// <c>array&lt;…&gt;</c> STRUCT out of the buffer's own DATA. That is the same fabrication
/// <c>vendor/…/sha3</c>'s <c>xor.cs</c> header records, reached here with no kernel, no socket and
/// no syscall in it: it is why <c>chacha8rand</c>'s <c>TestBlockGeneric</c> panicked
/// <c>index out of range [0] with length 0</c>.
/// </para>
/// <para>
/// The witness lives HERE rather than in that package's row on purpose. <c>chacha8_impl.cs</c> now
/// routes the package around the seam (the sha3/subtle remedy — an aliasing span view), so the
/// package no longer reproduces it; a golib test does, permanently, and cannot be fixed away by a
/// conversion change. <b>If the native-array-view arc lands, <see cref="TheRawAddressRouteCannotReconstructANestedArrayShape"/>
/// is the test that should be RE-READ rather than deleted</b> — it is the statement of what that
/// arc set out to change.
/// </para>
/// <para>
/// Only the ZEROED buffer is exercised. On filled data the same route materializes a managed
/// reference out of the data bytes and dereferencing it is an access violation that takes the test
/// host down rather than failing a test (the measurement is already recorded in the design's §1.1
/// table); a zeroed buffer reads a null backing, which is contained — and is exactly what the live
/// consumer met.
/// </para>
/// </remarks>
[TestClass]
public class ArrayShapeReinterpretTests
{
    // internal/chacha8rand's shape: 32 uint64s viewed as 16 rows of 4 uint32s.
    private const int BufferWords = 32;
    private const int Rows = 16;
    private const int Lanes = 4;

    [TestMethod]
    public void TheRawAddressRouteCannotReconstructANestedArrayShape()
    {
        array<uint64> buf = new(BufferWords);
        ж<array<uint64>> pointer = new StandardBox<array<uint64>>(buf);

        // Go's `(*[16][4]uint32)(unsafe.Pointer(buf))`. The emitted form routes through
        // unsafe.Pointer, which is a uintptr carrier and nothing else — these two operators ARE the
        // route, and taking them directly keeps this test inside golib.
        ж<array<array<uint32>>> view = (ж<array<array<uint32>>>)(uintptr)pointer;

        Assert.AreNotEqual(Rows, (int)view.Value.Length,
            "the raw-address route reconstructed a nested array shape — if that is real, the " +
            "native-array-view arc has landed and this witness needs re-reading, not deleting");

        // The specific value the live consumer met: a null backing read out of the zeroed buffer,
        // i.e. a LENGTH-ZERO array whose first index panics where Go reads a zero.
        Assert.AreEqual(0, (int)view.Value.Length,
            "the zeroed-buffer reading changed shape — re-measure the seam before trusting anything downstream");
    }

    [TestMethod]
    public void TheSpanViewAliasesTheSameStorageAndWritesThrough()
    {
        // The remedy chacha8_impl.cs, sha3's xor.cs and crypto/subtle's xor_generic.cs all take:
        // the reinterpret is taken over the array's OWN span, which is a genuine aliasing view.
        array<uint64> buf = new(BufferWords);
        Span<uint64> words = buf.ToSpan();
        Span<uint> lanes = MemoryMarshal.Cast<uint64, uint>(words);

        Assert.AreEqual(Rows * Lanes, lanes.Length,
            "the uint32 view is not the [16][4] shape the Go type says it is");

        // A write through the VIEW must land in the array every other consumer reads.
        lanes[0 * Lanes + 0] = 0x61707865u;
        lanes[0 * Lanes + 1] = 0x61707865u;
        lanes[15 * Lanes + 3] = 0xDEADBEEFu;

        Assert.AreEqual(0x61707865_61707865UL, buf[0],
            "a write through the uint32 view did not reach the uint64 array — a snapshot, not an alias");
        Assert.AreEqual(0xDEADBEEFUL << 32, buf[31] & (0xFFFFFFFFUL << 32),
            "the last lane's write missed the buffer's last word");

        // ...and the reverse: a write to the array is visible through the view.
        buf[4] = 0x1122334455667788UL;

        Assert.AreEqual(0x55667788u, lanes[2 * Lanes + 0], "the array's low half is not visible through the view");
        Assert.AreEqual(0x11223344u, lanes[2 * Lanes + 1], "the array's high half is not visible through the view");
    }

    [TestMethod]
    public void TheSpanViewKeepsGoWordOrderForTheUint64Half()
    {
        // chacha8rand's setup writes the SAME storage as [16][2]uint64 pairs, and block_generic
        // reads it back as [16][4]uint32. The two views must agree on which lane is which half,
        // or every seed row lands in the wrong place — silently, with plausible-looking output.
        array<uint64> buf = new(BufferWords);
        Span<uint64> pairs = buf.ToSpan();
        Span<uint> lanes = MemoryMarshal.Cast<uint64, uint>(pairs);

        // Row 12's counter row, written the way setup writes it on a little-endian host.
        const uint Counter = 7u;
        pairs[12 * 2 + 0] = (Counter + 0) | ((uint64)(Counter + 1) << 32);
        pairs[12 * 2 + 1] = (Counter + 2) | ((uint64)(Counter + 3) << 32);

        for (int lane = 0; lane < Lanes; lane++)
        {
            Assert.AreEqual(Counter + (uint)lane, lanes[12 * Lanes + lane],
                $"lane {lane} of the counter row disagrees between the uint64 and uint32 views");
        }
    }
}
