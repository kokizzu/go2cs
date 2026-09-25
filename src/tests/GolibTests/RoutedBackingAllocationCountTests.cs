// RoutedBackingAllocationCountTests.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

/// <summary>
/// The V-fix 11 follow-on: each site the guard's former (b) UNCLASSIFIED block held is charged exactly as
/// its classification says, and each empty case, which allocates nothing, is charged nothing.
/// </summary>
/// <remarks>
/// Three groups. The REAL undercounts, now routed: <c>reflect.Value.Grow</c>'s new backing (Go's growslice),
/// <c>reflect.Select</c>'s per-case array (Go's <c>reflect_rselect</c> <c>sel</c>) and <c>widen</c>'s fresh
/// backing. The sites that were ALREADY charged but invisible to the guard's textual scan, which must read
/// exactly what they read before, except that an empty enumerable is now free, as <c>CopyOf</c>'s empty span
/// always was. The BY-DESIGN select scratch stays uncharged, which the select arm pins.
/// </remarks>
[TestClass]
public class RoutedBackingAllocationCountTests
{
    [ClassInitialize]
    public static void EnableCounting(TestContext _) => AllocationCounter.Enable();

    private static long Charged(Action action)
    {
        long before = AllocationCounter.CurrentThreadCount;
        action();
        return AllocationCounter.CurrentThreadCount - before;
    }

    [TestMethod]
    public void MaterializeChargesTheArrayAndNothingForAnEmptySource()
    {
        int[] fromList = [];
        Assert.AreEqual(1L, Charged(() => fromList = AllocationCounter.Materialize(new List<int> { 1, 2 })));
        Assert.AreEqual(2, fromList.Length);

        int[] fromIterator = [];
        Assert.AreEqual(1L, Charged(() => fromIterator = AllocationCounter.Materialize(Enumerable.Range(1, 3).Select(x => x * 2))));
        Assert.AreEqual(3, fromIterator.Length);

        int[] emptyList = [1], emptyIterator = [1];
        Assert.AreEqual(0L, Charged(() =>
        {
            emptyList = AllocationCounter.Materialize(new List<int>());
            emptyIterator = AllocationCounter.Materialize(Enumerable.Range(0, 0).Select(x => x));
        }), "an empty source allocates nothing");

        // The premise of the free empty case: Enumerable.ToArray hands back the shared empty array.
        Assert.AreSame(Array.Empty<int>(), emptyList);
        Assert.AreSame(Array.Empty<int>(), emptyIterator);
    }

    [TestMethod]
    public void ReflectGrowChargesTheNewBacking()
    {
        slice<int> full = new(new[] { 1, 2 });

        object? grown = null;
        Assert.AreEqual(1L, Charged(() => grown = GoReflect.GrowSlice(full, typeof(int), 3)), "growslice mallocs one backing");
        Assert.IsTrue(((slice<int>)grown!).Capacity >= 5);

        slice<int> roomy = new slice<int>(new int[8]).slice(0, 2);
        Assert.AreEqual(0L, Charged(() => GoReflect.GrowSlice(roomy, typeof(int), 3)), "growth within capacity allocates nothing");
    }

    [TestMethod]
    public void ReflectSelectChargesItsCaseArrayAndNotTheEngineScratch()
    {
        // One never-ready case (a nil channel) plus default: the default fires. The per-case array is Go's
        // reflect_rselect `sel` and is charged; the engine's lock and poll order are select scratch, which
        // Go's select keeps on the stack, and stay uncharged.
        nint winner = 0;
        Assert.AreEqual(1L, Charged(() => winner = GoReflect.RunSelect([null], [false], [null], hasDefault: true).opWinner));
        Assert.AreEqual((nint)(-1), winner);
    }

    [TestMethod]
    public void WidenChargesItsFreshBackingAndNothingWhenEmpty()
    {
        slice<byte> source = new(new byte[] { 1, 2, 3 });

        slice<long> widened = default;
        Assert.AreEqual(1L, Charged(() => widened = builtin.widen(source, b => (long)b)));
        Assert.AreEqual(3L, (long)widened.Length);

        slice<byte> empty = new(Array.Empty<byte>());
        slice<long> widenedEmpty = default;
        Assert.AreEqual(0L, Charged(() => widenedEmpty = builtin.widen(empty, b => (long)b)));
        Assert.IsFalse(widenedEmpty == builtin.nil, "a non-nil empty source still widens to a non-nil empty");
    }

    [TestMethod]
    public void StackSliceToArrayChargesItsCopy()
    {
        Span<int> window = stackalloc int[] { 4, 5 };

        long charged = AllocationCounter.CurrentThreadCount;
        int[] copy = new sslice<int>(window).ToArray();
        charged = AllocationCounter.CurrentThreadCount - charged;

        Assert.AreEqual(1L, charged);
        Assert.AreEqual(2, copy.Length);
    }

    // The sites that were already charged, only out of the scan's sight: their readings must not move.
    [TestMethod]
    public void AlreadyChargedConversionsReadAsBefore()
    {
        slice<byte> bytes = new(new byte[] { 104, 105 });
        slice<char> chars = new(new[] { 'h', 'i' });
        @string text = "hi";

        Assert.AreEqual(1L, Charged(() => _ = new @string(bytes)), "string(b) copies once");
        Assert.AreEqual(1L, Charged(() => _ = (byte[])bytes), "slice -> T[] copies once");
        Assert.AreEqual(1L, Charged(() => _ = (array<byte>)bytes), "slice -> array copies once");
        Assert.AreEqual(3L, Charged(() => _ = new @string(chars)), "slice<char> -> string: the char copy, the UTF-16 string, the UTF-8 backing");
        Assert.AreEqual(3L, Charged(() => _ = (char[])text), "string -> char[]: the enumerator, its UTF-16 string, the array");
        Assert.AreEqual(3L, Charged(() => _ = (slice<char>)text), "string -> slice<char>: the enumerator, its UTF-16 string, the array");
        Assert.AreEqual(1L, Charged(() => _ = new[] { 7, 8 }.AsEnumerable().slice()), "an enumerable's slice materializes once");
    }

    // The one reading this change moves: an EMPTY enumerable was charged one array that never reached the
    // heap, and now is charged nothing. Before, these three read 2 + 2 + 1 = 5; the string's enumerator is a
    // real object and is still charged (one each), so now they read 1 + 1 + 0 = 2.
    [TestMethod]
    public void EmptyEnumerableMaterializationsAreFree()
    {
        @string empty = "";

        Assert.AreEqual(2L, Charged(() =>
        {
            _ = (char[])empty;
            _ = (slice<char>)empty;
            _ = Enumerable.Empty<int>().slice();
        }));
    }
}
