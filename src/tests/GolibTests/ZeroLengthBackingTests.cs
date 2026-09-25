// ZeroLengthBackingTests.cs - Gbtc
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
/// Guards REC-F (iv) (docs/phase4/DESIGN-allocation-counting.md section 9): a ZERO-length backing is
/// the shared <c>Array.Empty&lt;T&gt;()</c> and is charged nothing, as Go's zero-size objects share
/// <c>runtime.zerobase</c> and allocate nothing.
/// </summary>
/// <remarks>
/// Each row that asserts the new behaviour has a sibling asserting what must NOT change with it:
/// a nonzero length is still charged, a made empty slice is still not nil, and two empty
/// slices-of-array still keep their own element lengths.
/// </remarks>
[TestClass]
public class ZeroLengthBackingTests
{
    [ClassInitialize]
    public static void EnableCounting(TestContext _) => AllocationCounter.Enable();

    [TestMethod]
    public void AZeroLengthBackingIsTheSharedEmptyArrayAndIsFree()
    {
        long before = AllocationCounter.CurrentThreadCount;

        int[] a = AllocationCounter.NewArray<int>(0);
        int[] b = AllocationCounter.NewArray<int>((nint)0);
        int[] c = AllocationCounter.NewArray<int>(0UL);

        Assert.AreEqual(0L, AllocationCounter.CurrentThreadCount - before, "a zero-length backing is charged nothing");
        Assert.AreSame(Array.Empty<int>(), a);
        Assert.AreSame(Array.Empty<int>(), b);
        Assert.AreSame(Array.Empty<int>(), c);

        before = AllocationCounter.CurrentThreadCount;
        _ = AllocationCounter.NewArray<int>(1);
        Assert.AreEqual(1L, AllocationCounter.CurrentThreadCount - before, "control: a nonzero length is still charged");
    }

    /// <summary>
    /// log/slog's <c>Value</c> carries <c>array&lt;Action&gt; _ = new(0)</c>, which every explicit
    /// constructor runs: one counted object per Value until (iv) (the F4 family).
    /// </summary>
    [TestMethod]
    public void AZeroLengthArrayValueIsFree()
    {
        long before = AllocationCounter.CurrentThreadCount;
        array<Action> _ = new(0);
        Assert.AreEqual(0L, AllocationCounter.CurrentThreadCount - before);
    }

    [TestMethod]
    public void AMadeEmptySliceIsStillNotNil()
    {
        slice<nint> made = new(0);
        slice<nint> none = default;

        Assert.IsFalse(made == nil, "make([]T, 0) is not nil in Go, and a shared empty backing is still non-null");
        Assert.IsTrue(none == nil, "control: the zero value is nil");
        Assert.AreEqual((nint)0, made.Length);
    }

    /// <summary>
    /// The make path now reaches <c>GoReflect.WithElemDims</c> holding the SINGLETON, which its
    /// substitution exists for; before (iv) only a hand-built singleton reached it.
    /// </summary>
    [TestMethod]
    public void TwoMadeEmptySlicesOfArrayKeepTheirOwnElementLengths()
    {
        slice<array<byte>> three = GoReflect.WithElemDims(new slice<array<byte>>(0), 3);
        slice<array<byte>> four = GoReflect.WithElemDims(new slice<array<byte>>(0), 4);

        CollectionAssert.AreEqual(new nint[] { 3 }, GoReflect.SliceElemArrayDims(three));
        CollectionAssert.AreEqual(new nint[] { 4 }, GoReflect.SliceElemArrayDims(four));
    }
}
