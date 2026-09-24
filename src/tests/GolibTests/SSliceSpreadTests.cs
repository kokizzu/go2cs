// SSliceSpreadTests.cs - Gbtc
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
/// Guards the golib surface a variadic pack kept as a stack view reaches (REC-C,
/// docs/phase4/DESIGN-slice-idiom-allocations.md §A): its spread property, the append spread of it and
/// copy from it.
/// </summary>
/// <remarks>
/// The exact <c>in sslice&lt;T&gt;</c> overloads are load-bearing for the ALLOCATION, not only for
/// compiling: without them, <c>appendꓸꓸꓸ(dst, view)</c> still binds the <c>ISlice&lt;T&gt;</c> form through
/// sslice's implicit conversion to <c>slice&lt;T&gt;</c>, which copies -- the very object the view exists
/// to avoid. The slice-operand form is the control each test compares against.
/// </remarks>
[TestClass]
public class SSliceSpreadTests
{
    [TestMethod]
    public void SpreadPropertyIsTheViewsOwnStorage()
    {
        nint[] backing = [1, 2, 3];
        sslice<nint> view = backing.AsSpan().sslice();

        Span<nint> spread = view.ꓸꓸꓸ;
        spread[0] = 42;

        Assert.AreEqual(3, spread.Length);
        Assert.AreEqual((nint)42, backing[0], "a pass-through hands the callee the caller's storage, as Go does");
    }

    [TestMethod]
    public void AppendSpreadOfAViewMatchesTheSliceFormWithoutTheCopy()
    {
        AllocationCounter.Enable();

        nint[] pack = [7, 8, 9];

        slice<nint> dst = new(1, 8);
        long before = AllocationCounter.CurrentThreadCount;
        slice<nint> viaView = appendꓸꓸꓸ(dst, pack.AsSpan().sslice());
        long viewCount = AllocationCounter.CurrentThreadCount - before;

        slice<nint> dst2 = new(1, 8);
        slice<nint> viaSlice = appendꓸꓸꓸ(dst2, new slice<nint>(pack));

        Assert.AreEqual(viaSlice.Length, viaView.Length);
        Assert.AreEqual(viaSlice.Capacity, viaView.Capacity);

        for (nint i = 0; i < viaView.Length; i++)
            Assert.AreEqual(viaSlice[i], viaView[i]);

        Assert.AreEqual(0L, viewCount, "within capacity, appending a view allocates nothing");

        // Beyond capacity: the growth is the only object.
        slice<nint> small = new(1, 1);
        before = AllocationCounter.CurrentThreadCount;
        _ = appendꓸꓸꓸ(small, pack.AsSpan().sslice());
        Assert.AreEqual(1L, AllocationCounter.CurrentThreadCount - before);

        // The constrained form.
        slice<nint> constrained = appendꓸꓸꓸ<slice<nint>, nint>(new slice<nint>(1, 8), pack.AsSpan().sslice());
        Assert.AreEqual((nint)4, constrained.Length);
    }

    [TestMethod]
    public void CopyFromAViewCopiesTheMinimumAndHonoursOverlap()
    {
        nint[] pack = [5, 6, 7];
        slice<nint> dst = new(2);

        Assert.AreEqual((nint)2, copy(dst, pack.AsSpan().sslice()));
        Assert.AreEqual((nint)5, dst[0]);
        Assert.AreEqual((nint)6, dst[1]);

        // Overlap: source and destination windows of one backing, as Go's copy permits.
        nint[] shared = [1, 2, 3, 4];
        slice<nint> into = new slice<nint>(shared)[1..];
        Assert.AreEqual((nint)3, copy(into, shared.AsSpan(0, 3).sslice()));
        CollectionAssert.AreEqual(new nint[] { 1, 1, 2, 3 }, shared);

        // The ISlice destination form.
        ISlice<nint> boxed = new slice<nint>(3);
        Assert.AreEqual((nint)3, copy(boxed, pack.AsSpan().sslice()));
    }
}
