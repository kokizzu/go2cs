// StringViewTierTests.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

/// <summary>
/// The string-literal tiers' golib seat (owner ruling c555d91c55; DESIGN-string-literal-allocation.md
/// §8.2R2 with COORD's verification note F8): (a) @string hands out only READ-ONLY views of its
/// backing, (b) G8, Go's empty-operand concatenation identity, and (c) G10, the one-byte string table.
/// </summary>
/// <remarks>
/// <para>
/// The counted-object arms read golib's own <see cref="AllocationCounter"/>, which counts at golib's
/// allocation sites and is the same number at Debug and Release. The zero-BYTES arms are
/// INCONCLUSIVE at Debug, as AliasOverlapRaceTests' are: a JIT-optimizer-disabled frame can box a
/// temporary the optimized one does not, so a Debug byte reading measures the frame, not golib.
/// </para>
/// </remarks>
[TestClass]
public class StringViewTierTests
{
    private static bool JitOptimizerDisabled(Assembly assembly) =>
        assembly.GetCustomAttribute<DebuggableAttribute>()?.IsJITOptimizerDisabled == true;

    // golib's per-thread count across body(). The counter has no Disable: the go2cs test host turns it
    // on for the whole process too, so leaving it on matches every consumer.
    private static long Counted(Action body)
    {
        AllocationCounter.Enable();
        long before = AllocationCounter.CurrentThreadCount;
        body();
        return AllocationCounter.CurrentThreadCount - before;
    }

    private static bool SameStart(@string a, @string b) =>
        Unsafe.AreSame(ref MemoryMarshal.GetReference(a.Bytes), ref MemoryMarshal.GetReference(b.Bytes));

    // ---- (a) read-only views ------------------------------------------------------------------

    [TestMethod]
    public void EveryPublicViewOfAStringIsReadOnly()
    {
        Type s = typeof(@string);

        Assert.AreEqual(typeof(ReadOnlySpan<byte>), s.GetMethod(nameof(@string.ToSpan), Type.EmptyTypes)!.ReturnType, "@string.ToSpan()");
        Assert.AreEqual(typeof(ReadOnlySpan<byte>), s.GetProperty("ꓸꓸꓸ")!.PropertyType, "@string's spread");
        Assert.AreEqual(typeof(ReadOnlySpan<byte>), s.GetMethod(nameof(@string.Slice), [typeof(int), typeof(int)])!.ReturnType, "@string.Slice(int, int)");
        Assert.AreEqual(typeof(ReadOnlySpan<byte>), s.GetMethod(nameof(@string.Slice), [typeof(nint), typeof(nint)])!.ReturnType, "@string.Slice(nint, nint)");
        Assert.AreEqual(typeof(ReadOnlySpan<byte>), typeof(IByteSeq<byte>).GetProperty("ꓸꓸꓸ")!.PropertyType, "IByteSeq<byte>'s spread");

        MethodInfo ext = typeof(SliceExtensions).GetMethod("slice", [typeof(@string), typeof(nint), typeof(nint), typeof(nint)])!;
        Assert.AreEqual(typeof(ReadOnlySpan<byte>), ext.ReturnType, "SliceExtensions.slice(this @string, ...)");
    }

    [TestMethod]
    public void ASliceKeepsItsWritableSpreadAndStillSatisfiesTheReadOnlyInterface()
    {
        slice<byte> b = new(new byte[] { 1, 2, 3 });
        b.ꓸꓸꓸ[1] = 9;
        Assert.AreEqual((byte)9, b[1], "a Go []byte's own spread stays writable");

        IByteSeq<byte> seq = b;
        Assert.AreEqual(3, seq.ꓸꓸꓸ.Length);
        Assert.AreEqual((byte)9, seq.ꓸꓸꓸ[1]);
    }

    [TestMethod]
    public void AppendingAStringSpreadDoesNotBoxTheSlice()
    {
        // Go's append(b, s...) with a string s: the read-only spread must bind the concrete
        // append<T>(slice<T>, params ReadOnlySpan<T>), not the constrained append<S, T> whose
        // `new slice<T>(s)` boxes the slice (uncounted, so only BYTES can see it).
        if (JitOptimizerDisabled(typeof(@string).Assembly) || JitOptimizerDisabled(typeof(StringViewTierTests).Assembly))
        {
            Assert.Inconclusive("a JIT-optimizer-disabled (Debug) build measures the frame, not golib: run GolibTests -c Release");
            return;
        }

        @string s = new("abc");
        slice<byte> buf = new(new byte[16], 0, 0, 16);
        buf = builtin.append(buf, s.ꓸꓸꓸ); // warm
        buf = new slice<byte>(new byte[4096], 0, 0, 4096);

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++)
            buf = builtin.append(buf, s.ꓸꓸꓸ);
        long bytes = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.AreEqual(3000, (int)buf.Length);
        Assert.AreEqual(0L, bytes, "append(b, s...) into spare capacity allocates nothing in Go, and nothing here");
    }

    // ---- (b) G8: x + "" and "" + x return x itself -------------------------------------------

    [TestMethod]
    public void AnEmptyOperandConcatenationReturnsTheOtherOperandWithNoCopy()
    {
        @string x = new("hello");
        @string empty = new("");
        @string zero = default;
        @string r1 = default, r2 = default, r3 = default, r4 = default, r5 = default, r6 = default;

        long n = Counted(() =>
        {
            r1 = x + empty;
            r2 = empty + x;
            r3 = x + zero;
            r4 = zero + x;
            r5 = x + ReadOnlySpan<byte>.Empty;
            r6 = ReadOnlySpan<byte>.Empty + x;
        });

        Assert.AreEqual(0L, n, "Go's concatstrings returns the single non-empty operand: no allocation");
        foreach (@string r in new[] { r1, r2, r3, r4, r5, r6 })
        {
            Assert.AreEqual("hello", r.ToString());
            Assert.IsTrue(SameStart(r, x), "the result IS the operand (runtime/string.go:49), not a copy");
        }
    }

    [TestMethod]
    public void AnAliasingOperandIsReturnedToo_AsGoReturnsIt()
    {
        // COORD's F8: no copy-on-alias precondition. Go's concatstrings returns a[idx] whatever it
        // aliases; golib does the same.
        slice<byte> backing = new(new byte[] { (byte)'a', (byte)'b', (byte)'c' });
        @string alias = @string.AliasOf(backing);
        @string r = default;

        long n = Counted(() => r = alias + new @string(""));

        Assert.AreEqual(0L, n);
        Assert.IsTrue(SameStart(r, alias), "an aliasing operand is returned as itself");
    }

    [TestMethod]
    public void TwoNonEmptyOperandsStillAllocateOne()
    {
        @string a = new("ab"), b = new("cd"), r = default;
        long n = Counted(() => r = a + b);
        Assert.AreEqual(1L, n, "control: a real concatenation allocates its one result");
        Assert.AreEqual("abcd", r.ToString());
        Assert.IsFalse(SameStart(r, a));
    }

    [TestMethod]
    public void TwoEmptyOperandsReturnTheEmptyString()
    {
        @string r = default;
        long n = Counted(() => r = new @string("") + default(@string));
        Assert.AreEqual(0L, n);
        Assert.AreEqual(0, r.Length);
    }

    // ---- (c) G10: string([]byte) of length 1 reads a static table ----------------------------

    [TestMethod]
    public void EveryOneByteConversionIsAllocationFreeAndShared()
    {
        for (int v = 0; v < 256; v++)
        {
            slice<byte> one = new(new[] { (byte)v });
            @string s1 = default, s2 = default;

            long n = Counted(() =>
            {
                s1 = new @string(one);
                s2 = (@string)one;
            });

            Assert.AreEqual(0L, n, $"string([]byte{{{v}}}) reads Go's static one-byte table (runtime/string.go:144-150)");
            Assert.AreEqual(1, s1.Length);
            Assert.AreEqual((byte)v, s1[0]);
            Assert.IsTrue(SameStart(s1, s2), $"both conversions of byte {v} share one table entry");
        }
    }

    [TestMethod]
    public void AOneByteStringDoesNotSeeLaterWritesToItsSourceSlice()
    {
        slice<byte> one = new(new[] { (byte)'x' });
        @string s = new(one);
        one[0] = (byte)'y';
        Assert.AreEqual("x", s.ToString(), "the table entry is not the caller's storage");
        Assert.AreEqual("y", new @string(one).ToString());
    }

    [TestMethod]
    public void TheTableIsScopedToStringOfByteSliceOnly()
    {
        slice<byte> two = new(new byte[] { 1, 2 });
        Assert.AreEqual(1L, Counted(() => _ = new @string(two)), "control: length 2 still copies");

        // string(rune) is intstring, which allocates in Go when it escapes: not the table's.
        @string a = default, b = default;
        long n = Counted(() => { a = (@string)(rune)'q'; b = (@string)(rune)'q'; });
        Assert.IsTrue(n >= 1, "string(rune) keeps its own conversion");
        Assert.IsFalse(SameStart(a, b), "string(rune) is not served from the one-byte table");
    }

    [TestMethod]
    public void TheOneByteConversionAllocatesZeroBytesAtRelease()
    {
        if (JitOptimizerDisabled(typeof(@string).Assembly) || JitOptimizerDisabled(typeof(StringViewTierTests).Assembly))
        {
            Assert.Inconclusive("a JIT-optimizer-disabled (Debug) build measures the frame, not golib: run GolibTests -c Release");
            return;
        }

        slice<byte> one = new(new byte[] { (byte)'z' });
        _ = new @string(one); // warm
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++)
            _ = new @string(one);
        long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.AreEqual(0L, bytes, "string([]byte{b}) allocates nothing in Go, and nothing here");
    }
}
