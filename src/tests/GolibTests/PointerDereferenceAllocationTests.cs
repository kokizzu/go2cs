// PointerDereferenceAllocationTests.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

/// <summary>
/// Guards the two allocations that a Go pointer must NOT cost: reading through one
/// (<see cref="ж{T}.Value"/>), and taking one at a struct field (<c>of(…)</c>).
/// </summary>
/// <remarks>
/// <para>
/// Both were silent — the code was correct, it merely allocated — and both were measured on
/// <c>os.File.WriteString</c>, whose <c>testing.AllocsPerRun</c> budget is ZERO and which was
/// spending 9,208 bytes per call. 4,760 of those were the dereference and 968 the field pointers.
/// </para>
/// <para>
/// <b>Dereference.</b> <see cref="ж{T}.Value"/> routes its standard-box branch through
/// <see cref="ж{T}.IsNull"/>, whose last term asks <c>m_val is null</c>. On an UNCONSTRAINED type
/// parameter that compiles to <c>box !T; ldnull; ceq</c>, so a term that is constant-false for every
/// struct <c>T</c> still allocated — and memcpy'd — a full copy of the pointee. A pointer to a large
/// record paid its own size on EVERY read; the <c>os.file</c> record is 592 bytes and the write path
/// walks eight links.
/// </para>
/// <para>
/// <b>Field pointer.</b> <c>of(…)</c> wraps the typed field accessor in an untyped one. The wrapper
/// closes over nothing but the accessor, so it is a pure function OF the accessor — but it was minted
/// per CALL, costing a display class plus a delegate on every <c>&amp;x.field</c> in the corpus.
/// </para>
/// <para>
/// Reintroducing either regression still compiles and still produces correct output, so the
/// assertions are on measured bytes rather than on shape.
/// </para>
/// </remarks>
[TestClass]
public class PointerDereferenceAllocationTests
{
    private const int Runs = 1000;

    // Warm-up passes run BEFORE the measurement window (JIT + first-call statics must not land in the
    // measured delta), so a body that accumulates observes Runs + WarmRuns iterations.
    private const int WarmRuns = 64;

    // A record big enough that a boxed copy per dereference is unmistakable against noise: 64 longs
    // is 512 bytes, ~528 boxed, so the pre-fix cost of the loop below was over half a megabyte.
    private struct BigValue
    {
        public long A;
#pragma warning disable CS0169 // padding to size; never read
        private long m_f01, m_f02, m_f03, m_f04, m_f05, m_f06, m_f07, m_f08;
        private long m_f09, m_f10, m_f11, m_f12, m_f13, m_f14, m_f15, m_f16;
        private long m_f17, m_f18, m_f19, m_f20, m_f21, m_f22, m_f23, m_f24;
        private long m_f25, m_f26, m_f27, m_f28, m_f29, m_f30, m_f31, m_f32;
        private long m_f33, m_f34, m_f35, m_f36, m_f37, m_f38, m_f39, m_f40;
        private long m_f41, m_f42, m_f43, m_f44, m_f45, m_f46, m_f47, m_f48;
        private long m_f49, m_f50, m_f51, m_f52, m_f53, m_f54, m_f55, m_f56;
        private long m_f57, m_f58, m_f59, m_f60, m_f61, m_f62, m_f63;
#pragma warning restore CS0169
    }

    // The same size, but CARRYING a reference — so the box keeps its value in m_val rather than in
    // the pinnable m_slot array (see ж<T>'s constructor). Both storage shapes reach IsNull.
    private struct BigRefValue
    {
        public long A;
        public string? Tag;
#pragma warning disable CS0169
        private long m_f01, m_f02, m_f03, m_f04, m_f05, m_f06, m_f07, m_f08;
        private long m_f09, m_f10, m_f11, m_f12, m_f13, m_f14, m_f15, m_f16;
        private long m_f17, m_f18, m_f19, m_f20, m_f21, m_f22, m_f23, m_f24;
        private long m_f25, m_f26, m_f27, m_f28, m_f29, m_f30, m_f31, m_f32;
#pragma warning restore CS0169
    }

    // A struct whose field pointers the of(...) tests take. Name is a REFERENCE-typed field on
    // purpose: a ж<string> holds its value in m_val, so a standard box and a field-reference box are
    // the same size and the two measurements below are directly comparable.
    private struct Record
    {
        public string? Name;
        public long Count;
    }

    private static ref string? NameOf(ref Record r) => ref r.Name;

    private static ref long CountOf(ref Record r) => ref r.Count;

    // Forces a real call per iteration: a ref-returning property could otherwise be hoisted out of
    // the measured loop, and a hoisted dereference measures nothing.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ReadA(ж<BigValue> p) => p.Value.A;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ReadA(ж<BigRefValue> p) => p.Value.A;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ж<string?> TakeNamePointer(ж<Record> p) => p.of<string?>(NameOf);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ж<string?> NewStringBox(string? value) => new StandardBox<string?>(value);

    private static long Measure(Action body)
    {
        for (int warm = 0; warm < WarmRuns; warm++)
            body();

        long before = GC.GetAllocatedBytesForCurrentThread();

        for (int run = 0; run < Runs; run++)
            body();

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [TestMethod]
    public void DereferencingAStandardBoxAllocatesNothing()
    {
        ж<BigValue> p = new StandardBox<BigValue>(new BigValue { A = 7 });
        long sum = 0;

        long bytes = Measure(() => sum += ReadA(p));

        Assert.AreEqual(7L * (Runs + WarmRuns), sum, "the dereference did not read the pointee");
        Assert.AreEqual(0L, bytes,
            $"reading through ж<BigValue> allocated {bytes} bytes across {Runs} dereferences " +
            $"({bytes / (double)Runs:F1} B/deref) — IsNull must not evaluate `m_val is null` for a " +
            "value-typed T, which boxes the whole pointee.");
    }

    [TestMethod]
    public void DereferencingAStandardBoxOfAReferenceBearingStructAllocatesNothing()
    {
        ж<BigRefValue> p = new StandardBox<BigRefValue>(new BigRefValue { A = 3, Tag = "t" });
        long sum = 0;

        long bytes = Measure(() => sum += ReadA(p));

        Assert.AreEqual(3L * (Runs + WarmRuns), sum, "the dereference did not read the pointee");
        Assert.AreEqual(0L, bytes,
            $"reading through ж<BigRefValue> allocated {bytes} bytes across {Runs} dereferences " +
            $"({bytes / (double)Runs:F1} B/deref)");
    }

    [TestMethod]
    public void DereferencingAFieldPointerChainAllocatesNothing()
    {
        // The shape the write path walks: a pointer to a field, read repeatedly. Resolving it runs
        // the untyped wrapper, which dereferences the PARENT box — the link that was boxing.
        ж<Record> p = new StandardBox<Record>(new Record { Name = "n", Count = 5 });
        ж<long> count = p.of<long>(CountOf);
        long sum = 0;

        long bytes = Measure(() => sum += count.Value);

        Assert.AreEqual(5L * (Runs + WarmRuns), sum, "the field dereference did not read the field");
        Assert.AreEqual(0L, bytes,
            $"reading through a field pointer allocated {bytes} bytes across {Runs} dereferences " +
            $"({bytes / (double)Runs:F1} B/deref)");
    }

    [TestMethod]
    public void TakingAFieldPointerCostsNoMoreThanTheBoxItself()
    {
        // Under the B1 per-kind split a field reference is its own class (FieldRefBox: source +
        // accessor + token) and legitimately outweighs a standard ж<string?> by a field — the
        // premise this test used to lean on (same type, same size) retired with the unified box.
        // The defect it guards is unchanged: the TYPED of(...) overload must resolve its untyped
        // wrapper from the accessor-keyed cache, not mint a display class plus delegate (~88 B)
        // per call. So the control is the same field-ref box minted with the wrapper already in
        // hand — any difference is exactly the per-call wrapper cost, and it must be zero.
        ж<Record> p = new StandardBox<Record>(new Record { Name = "n", Count = 5 });

        long direct = Measure(() => { ж<string?> _ = new FieldRefBox<string?>(p, s_untypedNameOf); });
        long typedOf = Measure(() => TakeNamePointer(p));

        Assert.IsTrue(direct > 0, "control did not measure the cost of allocating one field-ref box");
        Assert.IsTrue(typedOf <= direct,
            $"of(...) allocated {typedOf / (double)Runs:F1} B/call against a {direct / (double)Runs:F1} " +
            "B/call direct field-ref box — the untyped accessor wrapper is being minted per call " +
            "instead of once per accessor.");
    }

    private static readonly FieldRefFunc<string?> s_untypedNameOf = static s => ref ((ж<Record>)s).Value.Name;

    [TestMethod]
    public void FieldPointerSemanticsSurviveTheSharedWrapper()
    {
        ж<Record> p = new StandardBox<Record>(new Record { Name = "before", Count = 1 });

        ж<string?> n1 = p.of<string?>(NameOf);
        ж<string?> n2 = p.of<string?>(NameOf);
        ж<long> c1 = p.of<long>(CountOf);

        // Go pointer identity: `&x.f == &x.f`, and `&x.f != &x.g`.
        Assert.AreEqual(n1, n2, "two pointers to the same field must be equal");
        Assert.AreEqual(n1.GetHashCode(), n2.GetHashCode(), "equal field pointers must hash equally");
        Assert.AreNotEqual<object>(n1, c1, "pointers to different fields must not be equal");

        // The pointer ALIASES the field: a write through it is visible in the pointee, and a write to
        // the pointee is visible through it.
        n1.Value = "after";
        Assert.AreEqual("after", p.Value.Name, "write through the field pointer did not reach the field");

        p.Value.Count = 42;
        Assert.AreEqual(42L, c1.Value, "the field pointer did not observe a write to the field");
    }

    [TestMethod]
    public void NilnessAnswersAreUnchanged()
    {
        // IsNull's value-peeking term is only SKIPPED where it was constant-false. Every answer it
        // could actually give must be unchanged.
        Assert.IsTrue(new StandardBox<BigValue>(nil).IsNull, "a nil-constructed box is nil");
        Assert.IsFalse(new StandardBox<BigValue>(new BigValue()).IsNull, "a box holding a zero struct is not nil");

        // A reference-typed T whose held value IS null: a real box holding a nil value. That is the
        // one case the term exists for, and it must still report true.
        Assert.IsTrue(new StandardBox<string?>((string?)null).IsNull, "a box holding a null reference reports IsNull");
        Assert.IsFalse(new StandardBox<string?>("x").IsNull, "a box holding a reference is not null");
        Assert.IsFalse(new StandardBox<string?>((string?)null).IsNilPointer, "holding null is not STRUCTURAL nilness");

        // Nullable<T> is the value type whose default IS null, so the term stays live for it too.
        Assert.IsTrue(new StandardBox<int?>((int?)null).IsNull, "a box holding an empty Nullable reports IsNull");
        Assert.IsFalse(new StandardBox<int?>(5).IsNull, "a box holding a Nullable with a value is not null");

        // A field reference is a real address even when the field holds nil.
        ж<Record> p = new StandardBox<Record>(new Record { Name = null });
        Assert.IsFalse(p.of<string?>(NameOf).IsNull, "&x.f is an address even while f holds nil");
    }
}
