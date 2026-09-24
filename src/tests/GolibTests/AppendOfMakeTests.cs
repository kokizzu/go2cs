// AppendOfMakeTests.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using static go.builtin;

namespace GolibTests;

/// <summary>
/// Guards Go's EXTENDSLICE as golib emits it: <c>append(x, make([]T, n)...)</c> converts to
/// <c>appendꓸꓸꓸ(x, makeꓸꓸꓸ&lt;T&gt;(n))</c> (REC-C, docs/phase4/DESIGN-slice-idiom-allocations.md §B).
/// </summary>
/// <remarks>
/// The rewrite is only sound if EVERY observable result equals the make-then-append form it replaces
/// -- length, capacity, contents, aliasing and panics -- so each test compares the two forms directly,
/// and the old form is the control that proves the comparison can see a difference.
/// </remarks>
[TestClass]
public class AppendOfMakeTests
{
    private static slice<nint> Filled(nint length, nint capacity, nint stale)
    {
        slice<nint> s = new(length, capacity);

        for (nint i = 0; i < length; i++)
            s[i] = i + 1;

        // Values beyond len in the shared backing, as an earlier, longer window leaves them. Go clears
        // the extended region, so neither form may expose these.
        slice<nint> whole = s[..(int)capacity];

        for (nint i = length; i < capacity; i++)
            whole[i] = stale;

        return s;
    }

    [TestMethod]
    public void EveryShapeMatchesMakeThenAppend()
    {
        (nint len, nint cap, nint n)[] shapes =
        [
            (0, 0, 0), (0, 0, 1), (0, 0, 5), (2, 4, 0), (2, 4, 1), (2, 4, 2), (2, 4, 3),
            (3, 3, 1), (5, 8, 20), (1023, 1023, 1), (1024, 1024, 1), (1500, 1600, 300),
        ];

        foreach ((nint len, nint cap, nint n) in shapes)
        {
            slice<nint> a = Filled(len, cap, 99);
            slice<nint> b = Filled(len, cap, 99);

            slice<nint> old = appendꓸꓸꓸ(a, new slice<nint>(n));
            slice<nint> now = appendꓸꓸꓸ(b, makeꓸꓸꓸ<nint>(n));

            string shape = $"len={len} cap={cap} n={n}";

            Assert.AreEqual(old.Length, now.Length, shape);
            Assert.AreEqual(old.Capacity, now.Capacity, shape);

            for (nint i = 0; i < now.Length; i++)
                Assert.AreEqual(old[i], now[i], $"{shape} element {i}");
        }

        slice<nint> none = default;

        Assert.IsTrue(appendꓸꓸꓸ(none, makeꓸꓸꓸ<nint>(0)) == nil, "append(nil, make(0)...) must stay nil");
        Assert.AreEqual(appendꓸꓸꓸ(none, new slice<nint>(3)).Capacity, appendꓸꓸꓸ(none, makeꓸꓸꓸ<nint>(3)).Capacity);
    }

    [TestMethod]
    public void WithinCapacityExtendsTheSharedBackingAndZeroesIt()
    {
        slice<nint> s = Filled(2, 4, 99);
        slice<nint> e = appendꓸꓸꓸ(s, makeꓸꓸꓸ<nint>(2));

        Assert.AreEqual((nint)4, e.Length);
        Assert.AreEqual((nint)0, e[2], "Go clears the extended region");
        Assert.AreEqual((nint)0, e[3], "Go clears the extended region");

        e[0] = 42;
        Assert.AreEqual((nint)42, s[0], "the in-place arm shares the backing, as Go's does");
    }

    [TestMethod]
    public void GrowingCountsOneObjectWhereMakeThenAppendCountsTwo()
    {
        AllocationCounter.Enable();

        {
            slice<nint> s = Filled(2, 2, 0);

            long before = AllocationCounter.CurrentThreadCount;
            _ = appendꓸꓸꓸ(s, new slice<nint>(3));
            long old = AllocationCounter.CurrentThreadCount - before;

            before = AllocationCounter.CurrentThreadCount;
            _ = appendꓸꓸꓸ(s, makeꓸꓸꓸ<nint>(3));
            long now = AllocationCounter.CurrentThreadCount - before;

            Assert.AreEqual(2L, old, "control: the make's backing and the growth are both counted");
            Assert.AreEqual(1L, now, "extendslice allocates only the growth, as Go does");

            slice<nint> roomy = Filled(2, 8, 0);
            before = AllocationCounter.CurrentThreadCount;
            _ = appendꓸꓸꓸ(roomy, makeꓸꓸꓸ<nint>(3));
            Assert.AreEqual(0L, AllocationCounter.CurrentThreadCount - before, "within capacity nothing is allocated");
        }
    }

    [TestMethod]
    public void ConstrainedFormMatches()
    {
        slice<nint> s = Filled(2, 4, 99);
        slice<nint> now = appendꓸꓸꓸ<slice<nint>, nint>(s, makeꓸꓸꓸ<nint>(3));
        slice<nint> old = appendꓸꓸꓸ<slice<nint>, nint>(Filled(2, 4, 99), new slice<nint>(3));

        Assert.AreEqual(old.Length, now.Length);
        Assert.AreEqual(old.Capacity, now.Capacity);
    }

    [TestMethod]
    public void NegativeLengthPanicsLikeMake()
    {
        string? expected = null, actual = null;

        try { _ = new slice<nint>(-1); }
        catch (PanicException ex) { expected = ex.Message; }

        try { _ = makeꓸꓸꓸ<nint>(-1); }
        catch (PanicException ex) { actual = ex.Message; }

        Assert.IsNotNull(expected, "control: make([]T, -1) panics");
        Assert.AreEqual(expected, actual);
    }
}
