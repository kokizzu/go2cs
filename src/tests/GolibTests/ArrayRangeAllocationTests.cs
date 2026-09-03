// ArrayRangeAllocationTests.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

/// <summary>
/// Guards the allocation-free <c>for range</c> over <see cref="array{T}"/>, and the LIVE-read
/// contract the converter's range-expression snapshot depends on.
/// </summary>
/// <remarks>
/// <para>
/// The sibling <see cref="SliceRangeAllocationTests"/> locks the same property for
/// <see cref="slice{T}"/>, which shed ~136 B/loop when its <c>GetEnumerator</c> stopped being an
/// iterator method. <c>array&lt;T&gt;</c> kept that shape for a year longer because the semantics had
/// to be settled first: Go's <c>range</c> over an array VALUE iterates a COPY, so a snapshot has to
/// exist SOMEWHERE. It exists at the range EXPRESSION — the converter emits the same explicit
/// <c>.Clone()</c> every other Go array value-copy site takes — which leaves the enumerator free to be
/// the cheap, live-reading struct this file measures.
/// </para>
/// <para>
/// Both halves are guarded here, because each is silent to break: restoring the interface return type
/// still compiles and still produces correct output, it just allocates again; and making the
/// enumerator snapshot would allocate on every loop AND diverge from Go for <c>range p</c> over a
/// pointer-to-array and for <c>for i := range a</c>, where Go copies nothing. The Go-visible half is
/// output-compared by the <c>ArrayRangeSnapshot</c> behavioral test.
/// </para>
/// </remarks>
[TestClass]
public class ArrayRangeAllocationTests
{
    // Iteration count is high enough that a per-loop allocation is unmistakable against measurement
    // noise (the pre-fix cost was ~72 B per loop entry -> ~72 KB here) and low enough to stay fast.
    private const int LoopCount = 1000;

    /// <summary>
    /// Measures bytes allocated by N ranged loops over an array through the PATTERN path — the shape
    /// a converted <c>for i, v := range a</c> binds. A struct enumerator allocates nothing.
    /// </summary>
    private static long MeasurePatternBytes(array<byte> a, out long sum)
    {
        long total = 0;

        // Warm up: JIT the loop body and settle any first-call statics BEFORE the measurement window,
        // otherwise the tiering/JIT allocations land in the measured delta and read as a regression.
        for (int warm = 0; warm < 32; warm++)
        {
            foreach (var (i, v) in a)
                total += i + v;
        }

        total = 0;

        long before = GC.GetAllocatedBytesForCurrentThread();

        for (int run = 0; run < LoopCount; run++)
        {
            foreach (var (i, v) in a)
                total += i + v;
        }

        long after = GC.GetAllocatedBytesForCurrentThread();

        sum = total;
        return after - before;
    }

    /// <summary>
    /// The CONTROL: the same loop driven through <see cref="IEnumerable{T}"/>, which boxes the
    /// enumerator exactly as the old iterator method did. It must still allocate — a zero here would
    /// mean the measurement window, not the enumerator, is what changed.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long MeasureBoxedBytes(array<byte> a, out long sum)
    {
        long total = 0;

        for (int warm = 0; warm < 32; warm++)
        {
            foreach ((nint i, byte v) in (IEnumerable<(nint, byte)>)a)
                total += i + v;
        }

        total = 0;

        long before = GC.GetAllocatedBytesForCurrentThread();

        for (int run = 0; run < LoopCount; run++)
        {
            foreach ((nint i, byte v) in (IEnumerable<(nint, byte)>)a)
                total += i + v;
        }

        long after = GC.GetAllocatedBytesForCurrentThread();

        sum = total;
        return after - before;
    }

    [TestMethod]
    public void RangeOverArrayAllocatesNothing()
    {
        array<byte> a = new(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        long bytes = MeasurePatternBytes(a, out long sum);

        // Sanity: the loop really ran and really read the elements (index 0..7 + values 1..8).
        Assert.AreEqual((0 + 1 + 2 + 3 + 4 + 5 + 6 + 7 + 1 + 2 + 3 + 4 + 5 + 6 + 7 + 8) * (long)LoopCount, sum,
            "range loop did not observe the expected (index, value) pairs");

        Assert.AreEqual(0L, bytes,
            $"`foreach` over array<T> allocated {bytes} bytes across {LoopCount} loops " +
            $"({bytes / (double)LoopCount:F1} B/loop) — GetEnumerator() must return the concrete " +
            "struct enumerator, not IEnumerator<(nint, T)>.");
    }

    [TestMethod]
    public void BoxedRangeOverArrayStillAllocates()
    {
        // The control that makes the zero above trustworthy: the interface path is the SAME loop over
        // the SAME data with the SAME warm-up, differing only in which GetEnumerator binds. If this
        // ever reads zero, the instrument has stopped measuring and the assertion above is vacuous.
        array<byte> a = new(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        long bytes = MeasureBoxedBytes(a, out long sum);

        Assert.AreEqual((0 + 1 + 2 + 3 + 4 + 5 + 6 + 7 + 1 + 2 + 3 + 4 + 5 + 6 + 7 + 8) * (long)LoopCount, sum,
            "boxed range loop did not observe the expected (index, value) pairs");

        Assert.IsTrue(bytes > 0,
            $"the boxed control allocated {bytes} bytes — it must allocate, or the zero asserted by " +
            "RangeOverArrayAllocatesNothing proves nothing about the enumerator.");
    }

    [TestMethod]
    public void RangeOverAliasWindowAllocatesNothingAndIsWindowRelative()
    {
        // Non-zero m_low: Go's `(*[4]byte)(s[2:])` windows the slice's storage, and the enumerator
        // must report indices RELATIVE to that window (0-based), not to the backing store.
        array<byte> a = array<byte>.Alias(new slice<byte>([1, 2, 3, 4, 5, 6, 7, 8])[2..], 4);

        long bytes = MeasurePatternBytes(a, out long sum);

        Assert.AreEqual((0 + 1 + 2 + 3 + 3 + 4 + 5 + 6) * (long)LoopCount, sum,
            "range over an alias window did not produce window-relative indices");

        Assert.AreEqual(0L, bytes, $"`foreach` over an array alias window allocated {bytes} bytes");
    }

    [TestMethod]
    public void RangeOverZeroValueArrayAllocatesNothingAndYieldsNoElements()
    {
        // `default(array<T>)` ran no constructor, so its backing is null; enumerating it is a
        // zero-iteration loop, never a fault (the same null-safe zero value every other read takes).
        array<byte> a = default;

        long bytes = MeasurePatternBytes(a, out long sum);

        Assert.AreEqual(0L, sum, "range over a zero-value array must yield no elements");
        Assert.AreEqual(0L, bytes, $"`foreach` over a zero-value array allocated {bytes} bytes");
    }

    [TestMethod]
    public void EnumeratorReadsLiveStorageRatherThanASnapshot()
    {
        // The semantic half, stated from golib's side: the enumerator does NOT copy. Go's array-value
        // range DOES see a copy, but that copy is the range EXPRESSION's and the converter emits it as
        // an explicit `.Clone()`; snapshotting here as well would double the cost and would also copy
        // for the two shapes Go leaves shared — `range p` over a pointer-to-array, and `for i := range a`.
        array<byte> a = new(new byte[] { 1, 2, 3, 4 });

        List<byte> observed = [];

        foreach (var (i, v) in a)
        {
            if (i == 0)
                a[1] = 91;

            observed.Add(v);
        }

        CollectionAssert.AreEqual(new byte[] { 1, 91, 3, 4 }, observed,
            "array<T>'s enumerator must read live storage — the Go-visible copy is the converter's " +
            "range-expression Clone(), not a snapshot taken here.");

        // And the converter's snapshot, exercised through the same member it emits, hides the write —
        // which is what `for i, v := range a` must print.
        array<byte> b = new(new byte[] { 1, 2, 3, 4 });

        List<byte> viaClone = [];

        foreach (var (i, v) in b.Clone())
        {
            if (i == 0)
                b[1] = 91;

            viaClone.Add(v);
        }

        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, viaClone,
            "the range-expression Clone() must snapshot the array Go copies");
    }

    [TestMethod]
    public void EnumeratorStillSatisfiesTheInterfaceContract()
    {
        // array<T> is IArray<T> is IEnumerable<(nint, T)>. The pattern path must not have cost the
        // interface path: LINQ, `foreach` over an interface-typed local, and anything holding the
        // array as IEnumerable<(nint, T)> still enumerate the same pairs (boxing, as they always did).
        array<byte> a = new(new byte[] { 10, 20, 30 });

        List<(nint, byte)> viaInterface = [];

        foreach ((nint i, byte v) in (IEnumerable<(nint, byte)>)a)
            viaInterface.Add((i, v));

        CollectionAssert.AreEqual(
            new[] { ((nint)0, (byte)10), ((nint)1, (byte)20), ((nint)2, (byte)30) },
            viaInterface,
            "IEnumerable<(nint, T)> enumeration diverged from the struct enumerator");

        // The T-typed interface view (IList<T>/IEnumerable<T>) is a separate, still-working path.
        List<byte> values = [];

        foreach (byte v in (IEnumerable<byte>)a)
            values.Add(v);

        CollectionAssert.AreEqual(new byte[] { 10, 20, 30 }, values,
            "IEnumerable<T> enumeration diverged");
    }
}
