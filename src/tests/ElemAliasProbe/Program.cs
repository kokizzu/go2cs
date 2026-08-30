// ElemAliasProbe - isolates the golib element-aliasing behaviour that crypto/internal/boring/bcache's
// TestCache concurrent section exercises.  Nothing here is a converter product: every shape below is a
// hand transcription of the emitted C# (src/core/crypto/internal/boring/bcache/cache.cs) and of the
// generated named-array wrapper for `cacheTable`
// (Generated/go2cs-gen/go2cs.TypeGenerator/...cacheTable_K, V_.g.cs), so the probe measures the same
// machinery the corpus runs.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using go;
using go.sync;                 // brings atomic_package's extension methods into scope
using static go.builtin;
using atomic = go.sync.atomic_package;

namespace ElemAliasProbe;

// bcache's cacheEntry, minus the atomic value slot (the entry itself is what goes missing, not its value).
public struct Node
{
    public ж<int> k;
    public int v;
    public ж<Node> next;
}

// A verbatim transcription of the go2cs-gen named-array wrapper emitted for
//     type cacheTable[K, V] [cacheSize]atomic.Pointer[cacheEntry[K, V]]
// The load-bearing lines are the m_value slot and the lazy Value getter, which are exactly what door 2
// changes: the emission was
//
//     private array<atomic.Pointer<Node>>? m_value;
//     public array<atomic.Pointer<Node>> Value => m_value ??= new array<atomic.Pointer<Node>>(N);
//
// and `??=` is a read-modify-write, so concurrent first-touch threads each allocate and all but one
// allocation is orphaned (arm7).  The interlocked publish below is the current emission, verbatim apart
// from the `?` annotations the probe compiles without a nullable context (see ElemAliasProbe.csproj).
public struct Tbl : IArray<atomic.Pointer<Node>>
{
    public const int N = 1021;

    // Value of the struct 'cacheTable<K, V>'
    private System.Runtime.CompilerServices.StrongBox<array<atomic.Pointer<Node>>> m_value;

    public Tbl(array<atomic.Pointer<Node>> value) => m_value = new System.Runtime.CompilerServices.StrongBox<array<atomic.Pointer<Node>>>(value);

    public array<atomic.Pointer<Node>> Value
    {
        get
        {
            System.Runtime.CompilerServices.StrongBox<array<atomic.Pointer<Node>>> value = m_value;

            if (value is null)
            {
                System.Runtime.CompilerServices.StrongBox<array<atomic.Pointer<Node>>> created = new System.Runtime.CompilerServices.StrongBox<array<atomic.Pointer<Node>>>(new array<atomic.Pointer<Node>>(N));
                value = Interlocked.CompareExchange(ref m_value, created, null) ?? created;
            }

            return value.Value;
        }
    }

    public atomic.Pointer<Node>[] Source => Value;

    public nint Length => Value.Length;

    Array IArray.Source => ((IArray)Value).Source!;

    object IArray.this[nint index]
    {
        get => ((IArray)Value)[index];
        set => ((IArray)Value)[index] = value;
    }

    public ref atomic.Pointer<Node> this[nint index] => ref Value[index];

    public Span<atomic.Pointer<Node>> ꓸꓸꓸ => ToSpan();

    public Span<atomic.Pointer<Node>> ToSpan() => Value.ToSpan();

    public IEnumerator<(nint, atomic.Pointer<Node>)> GetEnumerator() => Value.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Value).GetEnumerator();

    public bool Equals(IArray<atomic.Pointer<Node>> other) => Value.Equals(other);

    // Emitted for the Array kind since door 2: with the value behind a reference slot, the inherited
    // ValueType.Equals/GetHashCode would compare and hash the HOLDER's identity rather than the
    // array's contents (arm10).
    public override bool Equals(object obj) => obj is Tbl other && Value.Equals(other.Value);

    public override int GetHashCode() => Value.GetHashCode();

    public object Clone() => new Tbl(Value.Clone());
}

// The PRE-door-2 emission of the same wrapper, kept so the cost of the interlocked publish can be
// measured against it in one process (arm9) instead of across two builds.  Correctness arms use Tbl.
public struct TblLazy : IArray<atomic.Pointer<Node>>
{
    public const int N = Tbl.N;

    private array<atomic.Pointer<Node>>? m_value;

    public TblLazy(array<atomic.Pointer<Node>> value) => m_value = value;

    public array<atomic.Pointer<Node>> Value => m_value ??= new array<atomic.Pointer<Node>>(N);

    public atomic.Pointer<Node>[] Source => Value;

    public nint Length => Value.Length;

    Array IArray.Source => ((IArray)Value).Source!;

    object IArray.this[nint index]
    {
        get => ((IArray)Value)[index];
        set => ((IArray)Value)[index] = value;
    }

    public ref atomic.Pointer<Node> this[nint index] => ref Value[index];

    public Span<atomic.Pointer<Node>> ꓸꓸꓸ => ToSpan();

    public Span<atomic.Pointer<Node>> ToSpan() => Value.ToSpan();

    public IEnumerator<(nint, atomic.Pointer<Node>)> GetEnumerator() => Value.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Value).GetEnumerator();

    public bool Equals(IArray<atomic.Pointer<Node>> other) => Value.Equals(other);

    public object Clone() => new TblLazy(Value.Clone());
}

// bcache's Cache, transcribed.  ptable is a plain Interlocked slot rather than atomic.Pointer<Tbl>
// only because atomic.Pointer's field is internal to sync.atomic; the semantics are identical
// (one winner installs the table box, everyone else reloads it).
public sealed class Cache
{
    private ж<Tbl> m_ptable;

    // Every head pointer this cache ever hands out, by the canonical backing storage behind it.
    public readonly ConcurrentDictionary<object, int> Backings = new(ReferenceEqualityComparer.Instance);
    public bool Instrument;

    public ж<Tbl> table()
    {
        while (true)
        {
            ж<Tbl> p = Volatile.Read(ref m_ptable);

            if (p is null)
            {
                p = @new<Tbl>();

                if (Interlocked.CompareExchange(ref m_ptable, p, null) is not null)
                    continue;
            }

            return p;
        }
    }

    private ж<atomic.Pointer<Node>> Head(ж<int> k)
    {
        // The emitted form: `Ꮡc.table().at<atomic.Pointer<cacheEntry<K, V>>>((nint)((uintptr)Ꮡk % (uintptr)cacheSize))`
        ж<atomic.Pointer<Node>> head = table().at<atomic.Pointer<Node>>((nint)(k.PointerOrderToken % (nuint)N_));

        if (Instrument)
        {
            object storage = head.PinnableStorage;

            if (storage is not null)
                Backings.AddOrUpdate(storage, 1, static (_, c) => c + 1);
        }

        return head;
    }

    private const int N_ = Tbl.N;

    public void Put(ж<int> k, int v)
    {
        ж<atomic.Pointer<Node>> head = Head(k);

        ж<Node> add = null;
        ж<Node> noK = null;
        nint n = 0;

        while (true)
        {
            ж<Node> e = head.Load();
            ж<Node> start = e;

            for (; e is not null && !ReferenceEquals(e, noK); e = e.Value.next)
            {
                if (ReferenceEquals(e.Value.k, k))
                {
                    e.Value.v = v;
                    return;
                }

                n++;
            }

            if (add is null)
                add = Ꮡ(new Node { k = k, v = v });

            add.Value.next = start;

            if (n >= 1000)
                add.Value.next = null;

            if (head.CompareAndSwap(start, add))
                return;

            noK = start;
        }
    }

    public bool TryGet(ж<int> k, out int v)
    {
        ж<atomic.Pointer<Node>> head = Head(k);
        ж<Node> e = head.Load();

        for (; e is not null; e = e.Value.next)
        {
            if (ReferenceEquals(e.Value.k, k))
            {
                v = e.Value.v;
                return true;
            }
        }

        v = 0;
        return false;
    }
}

public static class Program
{
    private static int s_seq;

    private static ж<int> NextKey()
    {
        int n = Interlocked.Increment(ref s_seq);
        return Ꮡ(n);
    }

    public static int Main(string[] args)
    {
        string arm = args.Length > 0 ? args[0] : "all";
        int trials = args.Length > 1 ? int.Parse(args[1]) : 200;

        Console.WriteLine($"ElemAliasProbe  arm={arm} trials={trials}  gcServer={System.Runtime.GCSettings.IsServerGC}  procs={Environment.ProcessorCount}");
        Console.WriteLine($"  DOTNET_GCgen0size={Environment.GetEnvironmentVariable("DOTNET_GCgen0size") ?? "(unset)"}");
        Console.WriteLine();

        int rc = 0;

        if (arm is "arm1r" or "arm2r")
            return RepeatReplica(arm == "arm2r", trials);

        if (arm is "all" or "arm0")
            rc |= Arm0_MaterializationRace(trials);

        if (arm is "all" or "arm1")
            rc |= Arm1_BcacheReplica("arm1  cold table, no pre-materialization", prematerialize: false, gcPressure: false);

        if (arm is "all" or "arm2")
            rc |= Arm2_Prematerialized();

        if (arm is "all" or "arm3")
            rc |= Arm1_BcacheReplica("arm3  cold table + GC pressure thread", prematerialize: false, gcPressure: true);

        if (arm is "all" or "arm4")
            rc |= Arm4_DirectBackingCas();

        if (arm is "all" or "arm5")
            rc |= Arm5_ByValueElementPointer();

        if (arm is "all" or "arm6" or "arm6-arr" or "arm6-sli" or "arm6-named")
            rc |= Arm6_FastPathThroughput(arm);

        if (arm is "all" or "arm7")
            rc |= Arm7_DirectRefGetterRace(trials);

        if (arm is "all" or "arm8")
            rc |= Arm8_ByValueWholeWrapperBox();

        if (arm is "all" or "arm9")
            rc |= Arm9_WarmGetterThroughput();

        if (arm is "all" or "arm10")
            rc |= Arm10_HashAndEqualityParity();

        return rc;
    }

    // ---------------------------------------------------------------------------------------------
    // ARM 10 - door 2 changes the DECLARED TYPE of the wrapper's single field, and a C# struct with no
    // GetHashCode override hashes through ValueType.GetHashCode, which reads that field.  A Go fixed-
    // size array is COMPARABLE and legal as a map key, so if the emission's hash silently moved from
    // structural to reference identity, equal Go arrays would stop landing in the same bucket - a
    // second silent wrong answer traded for the first.  Measured, not assumed: two DISTINCT wrappers
    // holding equal content, under both emissions, in one process.
    // ---------------------------------------------------------------------------------------------
    private static int Arm10_HashAndEqualityParity()
    {
        TblLazy la = new TblLazy(new array<atomic.Pointer<Node>>(TblLazy.N));
        TblLazy lb = new TblLazy(new array<atomic.Pointer<Node>>(TblLazy.N));
        Tbl a = new Tbl(new array<atomic.Pointer<Node>>(Tbl.N));
        Tbl b = new Tbl(new array<atomic.Pointer<Node>>(Tbl.N));

        bool lazyHash = la.GetHashCode() == lb.GetHashCode();
        bool casHash = a.GetHashCode() == b.GetHashCode();

        // The COMPILE-TIME bound overload (`Equals(IArray<T>)`) and the RUNTIME one a map lookup or
        // reflect.DeepEqual actually reaches (`object.Equals`) are different methods, and only the
        // second reads the m_value field's own Equals - so they must both be measured.
        bool lazyEq = la.Equals(lb);
        bool casEq = a.Equals(b);
        bool lazyObjEq = ((object)la).Equals(lb);
        bool casObjEq = ((object)a).Equals(b);

        Console.WriteLine("arm10 two DISTINCT wrappers holding equal content - hash and equality");
        Console.WriteLine($"      ??= emission          GetHashCode equal: {lazyHash,-5}   Equals(IArray): {lazyEq,-5}   object.Equals: {lazyObjEq}");
        Console.WriteLine($"      interlocked publish   GetHashCode equal: {casHash,-5}   Equals(IArray): {casEq,-5}   object.Equals: {casObjEq}");

        bool parity = casHash == lazyHash && casEq == lazyEq && casObjEq == lazyObjEq;

        Console.WriteLine($"      => {(parity ? "PARITY - the publish holder did not move either" : "MOVED - the slot's shape leaked into hashing or equality")}");
        Console.WriteLine();

        return parity ? 0 : 1024;
    }

    // ---------------------------------------------------------------------------------------------
    // ARM 9 - the door-2 fix's COST gate: the generated wrapper's own `Value` getter on the WARM
    // (already-materialized) path, reached by ref, with no golib on the call chain at all.  Door 2's
    // remedy adds an atomic publish to the COLD path only; if the warm path moves materially here,
    // the remedy is paying on every access and is the wrong shape.
    //
    // Both emissions are measured IN ONE PROCESS - `Tbl` (interlocked publish, what go2cs-gen emits
    // now) against `TblLazy` (the `??=` emission it replaced) - so the comparison needs no second
    // build and cannot drift as the machine does.
    //
    // Three shapes, because the cost of a one-word reference slot depends entirely on which:
    //   Value.Length, 64 tables  - the raw getter, varying receiver
    //   ref this[i],   64 tables - `ref Value[index]`, the form converted element reads and writes
    //                              emit, with a VARYING receiver so the getter cannot be hoisted.
    //                              This is the WORST case for a reference slot: 64 separate holder
    //                              objects, one dependent load each, none of them resident.
    //   ref this[i],   1 table   - the same indexer over ONE long-lived table, which is what the
    //                              corpus's named arrays actually are (crc tables, nistec point
    //                              tables, semtable). The getter is loop-invariant here and the JIT
    //                              hoists it, so the slot's shape stops mattering at all.
    // ---------------------------------------------------------------------------------------------
    private const int WarmCells = 64;

    private static int Arm9_WarmGetterThroughput()
    {
        const int Warmup = 500_000;
        const int Iters = 20_000_000;

        var cells = new Tbl[WarmCells];
        var lazyCells = new TblLazy[WarmCells];

        for (int i = 0; i < WarmCells; i++)
        {
            _ = cells[i].Source;            // materialize once each, single-threaded
            _ = lazyCells[i].Source;
        }

        Console.WriteLine("arm9  WARM generated-wrapper getter throughput (already-materialized fast path)");
        Console.WriteLine("      shape                        ??= emission        interlocked publish");

        Row("Value.Length, 64 tables",
            Bench3(n => { long s = 0; for (int i = 0; i < n; i++) s += lazyCells[i & (WarmCells - 1)].Value.Length; return s; }, Warmup, Iters),
            Bench3(n => { long s = 0; for (int i = 0; i < n; i++) s += cells[i & (WarmCells - 1)].Value.Length; return s; }, Warmup, Iters));

        Row("ref this[i],  64 tables",
            Bench3(n => { long s = 0; for (int i = 0; i < n; i++) s += ElemAddr(ref lazyCells[i & (WarmCells - 1)], i & 511); return s; }, Warmup, Iters),
            Bench3(n => { long s = 0; for (int i = 0; i < n; i++) s += ElemAddr(ref cells[i & (WarmCells - 1)], i & 511); return s; }, Warmup, Iters));

        Row("ref this[i],   1 table ",
            Bench3(n => { long s = 0; for (int i = 0; i < n; i++) s += ElemAddr(ref lazyCells[0], i & 511); return s; }, Warmup, Iters),
            Bench3(n => { long s = 0; for (int i = 0; i < n; i++) s += ElemAddr(ref cells[0], i & 511); return s; }, Warmup, Iters));

        Console.WriteLine();
        return 0;
    }

    private static unsafe long ElemAddr(ref Tbl table, int index) =>
        (nint)System.Runtime.CompilerServices.Unsafe.AsPointer(ref table[(nint)index]);

    private static unsafe long ElemAddr(ref TblLazy table, int index) =>
        (nint)System.Runtime.CompilerServices.Unsafe.AsPointer(ref table[(nint)index]);

    // Runs `body` once for warm-up then three timed passes, reporting the best (least-noise) one.
    private static double Bench3(Func<int, long> body, int warmup, int iters)
    {
        long sink = body(warmup);
        double best = double.MaxValue;

        for (int pass = 0; pass < 3; pass++)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            sink += body(iters);
            sw.Stop();
            best = Math.Min(best, sw.Elapsed.TotalMilliseconds * 1_000_000.0 / iters);
        }

        GC.KeepAlive(sink);
        return best;
    }

    private static void Row(string label, double before, double after)
    {
        double pct = (after - before) / before * 100.0;
        Console.WriteLine($"      {label}      {before,6:n3} ns/op         {after,6:n3} ns/op   ({pct:+0.0;-0.0;0.0}%)");
    }

    // ---------------------------------------------------------------------------------------------
    // ARM 8 - runtime/mpallocbits.cs's door, which is NOT door 3.
    //
    //     internal static void allocRange(this ж<pallocBits> Ꮡb, nuint i, nuint n) {
    //         ref var b = ref Ꮡb.DerefOrNull();
    //         (Ꮡ((pageBits)(b))).setRange(i, n);
    //     }
    //
    // `Ꮡ((pageBits)(b))` binds golib's `Ꮡ<T>(in T target)` - the STANDARD-BOX overload - not the
    // two-arg element-aliasing one.  It boxes a value produced by the `(pageBits)(b)` conversion,
    // and the generated conversion operator takes its argument BY VALUE
    // (`implicit operator pageBits(pallocBits value) => value.view`), so a still-lazy `b`
    // materializes its backing on the operator's parameter copy and the caller's storage is never
    // written.  Once materialized, every copy shares the same uint64[] and the writes land - so the
    // exposure is exactly the FIRST touch.
    //
    // Transcribed here as View (the array-view wrapper over Tbl-shaped storage) so the claim is
    // measured rather than reasoned.
    //
    // FIXED 2026-08-30 (branch claude/i9-mpallocbits): this WAS the third door of the
    // element-aliasing family, tracked separately from doors 1-3 because it is not a materialization
    // race at all — single-threaded, no concurrency needed — but a BY-VALUE PARAMETER COPY made
    // before any materialization happens. pointerReinterpretManagedSource's pointer-to-array-target
    // exclusion (convCallExpr.go) used to route EVERY `(*U)(p)` with an array-underlying target to
    // the value-convert-and-rebox path, including the one sub-case where U and *p's element type are
    // BOTH named types over the IDENTICAL array shape (Go's `type pallocBits pageBits`) — exactly
    // where golib's Reinterpret CAN alias correctly, because go2cs-gen's Array-kind wrapper gives
    // every such type the same one-field shape (a StrongBox<array<E>> slot) regardless of its Go
    // name. Narrowing that exclusion routes mpallocbits' `(*pageBits)(b)` through
    // `Ꮡb.Reinterpret<pallocBits, pageBits>()` instead — verified against the real corpus emission,
    // src/core/runtime/mpallocbits.cs, and against a full corpus build (0 errors).
    //
    // Both shapes are measured below: ViaValueConversion is the ORIGINAL, still-exposed pattern (an
    // ordinary hand-written `(Dst)(src)` conversion over an Array-kind wrapper remains exactly this
    // shape and exactly this exposed — the fix is scoped to the ONE converter emission path, not a
    // golib-wide guarantee), and ViaReinterpret is what the converter emits now.
    // ---------------------------------------------------------------------------------------------
    private struct View
    {
        private Tbl m_value;

        // The generated array-view wrapper's ref-safe accessor: touch m_value.Value ON THE FIELD so
        // the backing materializes in THIS wrapper's own storage, then hand back the value copy,
        // which shares that array by reference.
        private Tbl view
        {
            get
            {
                _ = m_value.Value;
                return m_value;
            }
        }

        // The generated by-value conversion operator — still what a hand-written `(Dst)(src)` over
        // an Array-kind wrapper binds; mpallocbits' OWN call site no longer does (see below).
        public static implicit operator Tbl(View value) => value.view;

        public atomic.Pointer<Node>[] Backing => m_value.Value.Source;
    }

    private static int Arm8_ByValueWholeWrapperBox()
    {
        // COLD: nothing has touched b's backing yet - the state a first allocRange() meets.
        ж<View> cold = @new<View>();
        ref View b = ref cold.Value;

        ж<Tbl> viaConversion = Ꮡ((Tbl)(b));           // the ORIGINAL emission, still exposed
        object writeLandsIn = viaConversion.Value.Source;   // the array a write through it reaches
        object callersOwn = b.Backing;                      // b's own storage, materialized after
        bool coldReaches = ReferenceEquals(writeLandsIn, callersOwn);

        // WARM: the same call once b's backing already exists.
        ж<View> warm = @new<View>();
        ref View w = ref warm.Value;
        object warmOwn = w.Backing;                          // materialize first
        ж<Tbl> warmConv = Ꮡ((Tbl)(w));
        bool warmReaches = ReferenceEquals(warmConv.Value.Source, warmOwn);

        Console.WriteLine("arm8  runtime/mpallocbits shape - `Ꮡ((pageBits)(b))`, the Ꮡ(in T) STANDARD-BOX door");
        Console.WriteLine("      (a hand-written `(Dst)(src)` conversion over an Array-kind wrapper — the");
        Console.WriteLine("      converter itself no longer emits this for the pallocBits/pageBits call site;");
        Console.WriteLine("      see ViaReinterpret below for what it emits now)");
        Console.WriteLine($"      COLD  (backing not yet materialized) - box aliases the caller's storage: {coldReaches}");
        Console.WriteLine($"      WARM  (backing already materialized) - box aliases the caller's storage: {warmReaches}");
        Console.WriteLine($"      => {(coldReaches ? "not exposed" : "EXPOSED on FIRST touch - writes through the box are lost until something else materializes b")}");
        Console.WriteLine();

        // COLD, via Reinterpret — the ACTUAL fixed emission: `Ꮡb.Reinterpret<pallocBits, pageBits>()`.
        // No by-value copy of View/Tbl is ever constructed; the derived box aliases the SAME managed
        // slot the source box does, so there is nothing for a materialization race to diverge.
        ж<View> coldR = @new<View>();
        ref View br = ref coldR.Value;
        ж<Tbl> viaReinterpret = coldR.Reinterpret<View, Tbl>();
        object reinterpretWriteLandsIn = viaReinterpret.Value.Source;
        object reinterpretCallersOwn = br.Backing;
        bool reinterpretColdReaches = ReferenceEquals(reinterpretWriteLandsIn, reinterpretCallersOwn);

        ж<View> warmR = @new<View>();
        ref View wr = ref warmR.Value;
        object warmROwn = wr.Backing;
        ж<Tbl> warmRConv = warmR.Reinterpret<View, Tbl>();
        bool reinterpretWarmReaches = ReferenceEquals(warmRConv.Value.Source, warmROwn);

        Console.WriteLine("arm8b runtime/mpallocbits FIX - `Ꮡb.Reinterpret<pallocBits, pageBits>()`, the actual current emission");
        Console.WriteLine($"      COLD  (backing not yet materialized) - box aliases the caller's storage: {reinterpretColdReaches}");
        Console.WriteLine($"      WARM  (backing already materialized) - box aliases the caller's storage: {reinterpretWarmReaches}");
        Console.WriteLine($"      => {(reinterpretColdReaches && reinterpretWarmReaches ? "not exposed, cold or warm" : "STILL EXPOSED — the fix did not do what it was supposed to")}");
        Console.WriteLine();

        return reinterpretColdReaches && reinterpretWarmReaches ? 0 : 1;
    }

    // ---------------------------------------------------------------------------------------------
    // ARM 7 - DOOR 2, isolated: the generated wrapper's OWN `Value => m_value ??= new array<E>(N)`
    // getter, reached through a plain `ref` and never through ж<T>.at().  This is a read-modify-write
    // of the struct's own field, so two threads that first-touch the SAME struct instance by ref can
    // each allocate a backing and the loser's writes are lost.
    //
    // No golib code is on this path at all - the shape is
    //     internal static ref semTable semtable => ref Ꮡsemtable.Value;   // then semtable[i] = x
    // so a golib-side publish gate CANNOT close it; only an atomic publish inside the generated
    // getter (go2cs-gen) can.  The arm exists to measure the residual honestly rather than assume it.
    // ---------------------------------------------------------------------------------------------
    private static int Arm7_DirectRefGetterRace(int trials)
    {
        int threads = Environment.ProcessorCount * 2;
        int raced = 0;
        var histogram = new SortedDictionary<int, int>();

        for (int t = 0; t < trials; t++)
        {
            // A heap cell holding the wrapper, reached by ref by every thread - `ref Ꮡx.Value`.
            ж<Tbl> cell = @new<Tbl>();
            var seen = new ConcurrentDictionary<object, byte>(ReferenceEqualityComparer.Instance);
            using var gate = new Barrier(threads + 1);
            var workers = new Thread[threads];

            for (int i = 0; i < threads; i++)
            {
                workers[i] = new Thread(() =>
                {
                    gate.SignalAndWait();

                    try
                    {
                        // The whole path: a ref to the struct, then its own lazy getter. No .at().
                        ref Tbl t2 = ref cell.Value;
                        atomic.Pointer<Node>[] backing = t2.Source;
                        seen.TryAdd(backing, 0);
                    }
                    catch (Exception)
                    {
                        // A torn read here is the same defect wearing a different coat.
                    }
                })
                { IsBackground = true };

                workers[i].Start();
            }

            gate.SignalAndWait();

            foreach (Thread w in workers)
                w.Join();

            int distinct = seen.Count;
            histogram.TryGetValue(distinct, out int c);
            histogram[distinct] = c + 1;

            if (distinct > 1)
                raced++;
        }

        Console.WriteLine("arm7  DOOR 2 - the generated wrapper's own lazy `Value` getter reached by ref (no .at() on the path)");
        Console.WriteLine($"      {threads} threads x {trials} trials");
        Console.WriteLine($"      trials whose ONE struct cell materialized >1 backing array: {raced}/{trials} ({100.0 * raced / trials:n1}%)");
        Console.Write("      distinct-backing histogram:");

        foreach (KeyValuePair<int, int> kv in histogram)
            Console.Write($"  {kv.Key}x{kv.Value}");

        Console.WriteLine();
        Console.WriteLine();

        return 0;
    }

    // ---------------------------------------------------------------------------------------------
    // ARM 6 - the SINGLE-THREADED hot-path cost of ж<T>.at<Telem>(), measured over the three shapes
    // the corpus actually reaches it through:
    //
    //   arr   ж<array<E>>  - golib's own array header; its Source is the RAW backing and never
    //                        materializes, so ensureArrayBacking's write-back rewrites identical
    //                        bytes.  This is the overwhelmingly common .at<> receiver corpus-wide.
    //   sli   ж<slice<E>>  - same, through the slice header.
    //   named ж<Tbl>       - a generated named fixed-size array wrapper, ALREADY materialized: the
    //                        state every .at<> after the first one sees.
    //
    // A fix that closes the materialization race must not make these three materially slower - a
    // per-access lock would show up here as a multiple, which is exactly what this arm exists to
    // catch before the perf suite does.
    // ---------------------------------------------------------------------------------------------
    // `arm6` runs all three shapes in one process, which is convenient but NOT a clean A/B: the
    // slice shape allocated ~7 GB of throwaway copies before the fix (see the sli row), and that
    // churn moves the GC state the shape measured AFTER it runs in. Use the single-shape arms
    // (`arm6-arr`, `arm6-sli`, `arm6-named`), one fresh process each, for any before/after claim.
    private static int Arm6_FastPathThroughput(string arm)
    {
        const int Warmup = 200_000;
        const int Iters = 5_000_000;

        bool all = arm is "all" or "arm6";

        Console.WriteLine("arm6  single-threaded .at<>() throughput (already-materialized fast path)");

        if (all || arm == "arm6-arr")
        {
            ж<array<atomic.Pointer<Node>>> arr = Ꮡ(new array<atomic.Pointer<Node>>(64));
            Bench("arr   ж<array<E>>.at<E>(i)", arr, Warmup, Iters);
        }

        if (all || arm == "arm6-sli")
        {
            ж<slice<atomic.Pointer<Node>>> sli = Ꮡ(new slice<atomic.Pointer<Node>>(64));
            Bench("sli   ж<slice<E>>.at<E>(i)", sli, Warmup, Iters);
        }

        if (all || arm == "arm6-named")
        {
            ж<Tbl> named = @new<Tbl>();
            _ = named.at<atomic.Pointer<Node>>(0);   // materialize once, single-threaded
            Bench("named ж<Tbl>.at<E>(i)     ", named, Warmup, Iters);
        }

        Console.WriteLine();
        return 0;
    }

    private static double Bench<TBox>(string label, ж<TBox> box, int warmup, int iters)
    {
        for (int i = 0; i < warmup; i++)
            _ = box.at<atomic.Pointer<Node>>(i & 63);

        // Three passes; report the best (least-noise) one, plus all three so a noisy host is visible.
        double best = double.MaxValue;
        var all = new List<double>(3);

        for (int pass = 0; pass < 3; pass++)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            object sink = null;

            for (int i = 0; i < iters; i++)
                sink = box.at<atomic.Pointer<Node>>(i & 63);

            sw.Stop();
            GC.KeepAlive(sink);
            double ns = sw.Elapsed.TotalMilliseconds * 1_000_000.0 / iters;
            all.Add(ns);
            best = Math.Min(best, ns);
        }

        Console.WriteLine($"      {label}  best={best:n2} ns/op   passes=[{string.Join(", ", all.ConvertAll(static x => x.ToString("n2")))}]");
        return best;
    }

    // ---------------------------------------------------------------------------------------------
    // ARM 5 - the SINGLE-THREADED sibling shape, transcribed from runtime/sema.cs:
    //
    //     internal static ж<semTable> Ꮡsemtable = new StandardBox<semTable>(default(semTable));
    //     internal static ref semTable semtable => ref Ꮡsemtable.Value;
    //     [GoRecv] internal static ж<semaRoot> rootFor(this ref semTable t, ...) {
    //         return Ꮡ(t, (int)(...)).of(semTableᴛ1.Ꮡroot);
    //     }
    //
    // `Ꮡ(t, i)` binds golib's `Ꮡ<T>(IArray<T> target, int index)`, which takes its target BY VALUE:
    // the ref receiver is BOXED at the call site and the ElemRefBox retains that boxing temp.  Unlike
    // ж<T>.at(), that overload never runs ensureArrayBacking, so the lazy backing materializes on the
    // private temp and is never written back.  If this reports distinct storage per call, the shared
    // table never materializes at all and every write through such a pointer is lost - no concurrency
    // required.
    // ---------------------------------------------------------------------------------------------
    private static int Arm5_ByValueElementPointer()
    {
        ж<Tbl> boxed = @new<Tbl>();
        ref Tbl t = ref boxed.Value;

        ж<atomic.Pointer<Node>> p1 = Ꮡ(t, 0);
        ж<atomic.Pointer<Node>> p2 = Ꮡ(t, 0);
        ж<atomic.Pointer<Node>> viaAt = boxed.at<atomic.Pointer<Node>>(0);
        ж<atomic.Pointer<Node>> viaAt2 = boxed.at<atomic.Pointer<Node>>(0);

        object s1 = p1.PinnableStorage;
        object s2 = p2.PinnableStorage;
        object sa = viaAt.PinnableStorage;
        object sa2 = viaAt2.PinnableStorage;

        // Write through the first by-value pointer, then read it back through the second.
        ж<Node> n = Ꮡ(new Node { k = Ꮡ(1), v = 42 });
        p1.Store(n);

        bool byValueLoses = p2.Load() is null;
        bool atSeesIt = viaAt.Load() is null;

        Console.WriteLine("arm5  by-value element pointer over a LAZY named-array wrapper (runtime/sema.cs shape)");
        Console.WriteLine($"      two consecutive `Ꮡ(t, 0)` calls name the SAME storage: {ReferenceEquals(s1, s2)}");
        Console.WriteLine($"      two consecutive `box.at(0)` calls name the SAME storage:  {ReferenceEquals(sa, sa2)}");
        Console.WriteLine($"      `Ꮡ(t, 0)` and `box.at(0)` name the same storage:          {ReferenceEquals(s1, sa)}");
        Console.WriteLine($"      a Store through Ꮡ(t,0) is INVISIBLE to a second Ꮡ(t,0):   {byValueLoses}");
        Console.WriteLine($"      a Store through Ꮡ(t,0) is INVISIBLE to box.at(0):         {atSeesIt}");
        Console.WriteLine();

        return Arm5b_ByValueThroughValueGetter();
    }

    // ---------------------------------------------------------------------------------------------
    // ARM 5b - the CANDIDATE emission for door 3.
    //
    // golib cannot fix `Ꮡ(t, i)`: the caller's `ref` is gone by the time the overload runs - it
    // receives a boxing temp the CALL SITE made.  And the `.at<>()` route is unavailable here,
    // because a Go pointer receiver renders as `this ref T t`, which has NO ж<> box companion
    // (convSelectorExpr.go:563 states that explicitly).
    //
    // But the wrapper's own `Value` getter is a MUTATING struct member, so invoking it on a `ref`
    // receiver runs `m_value ??= new array<E>(N)` against the REAL storage - no copy - and the
    // array<E> it returns SHARES that backing.  So `Ꮡ(t.Value, i)` aliases the caller's storage
    // using only what the converter can already see at the emission site.  This arm is the proof
    // of that claim before any converter line is touched.
    // ---------------------------------------------------------------------------------------------
    private static int Arm5b_ByValueThroughValueGetter()
    {
        ж<Tbl> boxed = @new<Tbl>();
        ref Tbl t = ref boxed.Value;

        // The candidate: materialize through the ref receiver's own getter, alias the shared array.
        ж<atomic.Pointer<Node>> p1 = Ꮡ(t.Value, 0);
        ж<atomic.Pointer<Node>> p2 = Ꮡ(t.Value, 0);
        ж<atomic.Pointer<Node>> viaAt = boxed.at<atomic.Pointer<Node>>(0);

        object s1 = p1.PinnableStorage;
        object s2 = p2.PinnableStorage;
        object sa = viaAt.PinnableStorage;

        ж<Node> n = Ꮡ(new Node { k = Ꮡ(1), v = 42 });
        p1.Store(n);

        bool p2Sees = ReferenceEquals(p2.Load(), n);
        bool atSees = ReferenceEquals(viaAt.Load(), n);

        Console.WriteLine("arm5b CANDIDATE `Ꮡ(t.Value, i)` - materialize through the ref receiver's own getter");
        Console.WriteLine($"      two consecutive `Ꮡ(t.Value, 0)` name the SAME storage:     {ReferenceEquals(s1, s2)}");
        Console.WriteLine($"      `Ꮡ(t.Value, 0)` and `box.at(0)` name the same storage:     {ReferenceEquals(s1, sa)}");
        Console.WriteLine($"      a Store through Ꮡ(t.Value,0) IS VISIBLE to Ꮡ(t.Value,0):  {p2Sees}");
        Console.WriteLine($"      a Store through Ꮡ(t.Value,0) IS VISIBLE to box.at(0):     {atSees}");
        Console.WriteLine();

        bool ok = ReferenceEquals(s1, s2) && ReferenceEquals(s1, sa) && p2Sees && atSees;
        Console.WriteLine($"      arm5b verdict: {(ok ? "PASS - the candidate aliases the caller's storage" : "FAIL")}");
        Console.WriteLine();

        return ok ? 0 : 16;
    }

    // ---------------------------------------------------------------------------------------------
    // ARM 0 - the mechanism, measured directly.
    //
    // A fresh `@new<Tbl>()` box (m_value == null, exactly what bcache's table() installs) is handed to
    // T threads that each immediately take an element pointer.  Every ж<T>.at<Telem>() call runs
    // ensureArrayBacking(ref Value), which BOXES A COPY of the wrapper struct, materializes the lazy
    // backing ON THAT COPY, and writes the copy back over the shared field.  If two threads box before
    // either writes back, each allocates its own atomic.Pointer<Node>[1021] and the second write-back
    // silently replaces the first.  This arm counts the distinct backings observed per trial: >1 means
    // the shared box handed out element pointers into more than one array.
    // ---------------------------------------------------------------------------------------------
    private static int Arm0_MaterializationRace(int trials)
    {
        int threads = Environment.ProcessorCount * 2;
        int raced = 0;
        int worstDistinct = 1;
        int torn = 0;
        var histogram = new SortedDictionary<int, int>();

        for (int t = 0; t < trials; t++)
        {
            ж<Tbl> box = @new<Tbl>();
            var seen = new ConcurrentDictionary<object, byte>(ReferenceEqualityComparer.Instance);
            using var gate = new Barrier(threads + 1);
            var workers = new Thread[threads];

            for (int i = 0; i < threads; i++)
            {
                workers[i] = new Thread(() =>
                {
                    gate.SignalAndWait();

                    // A TORN read of the shared wrapper is a second manifestation of the same
                    // unsynchronized write-back: `Tbl`'s m_value is a Nullable<array<E>> - four
                    // words - so a reader can observe hasValue=true beside a half-written header
                    // (null backing, zero length) and the bounds check then throws.  Counted, not
                    // fatal, so the arm still reports its distinct-backing arithmetic.
                    try
                    {
                        ж<atomic.Pointer<Node>> p = box.at<atomic.Pointer<Node>>(0);
                        object storage = p.PinnableStorage;

                        if (storage is not null)
                            seen.TryAdd(storage, 0);
                    }
                    catch (Exception)
                    {
                        Interlocked.Increment(ref torn);
                    }
                })
                { IsBackground = true };

                workers[i].Start();
            }

            gate.SignalAndWait();

            foreach (Thread w in workers)
                w.Join();

            int distinct = seen.Count;
            histogram.TryGetValue(distinct, out int c);
            histogram[distinct] = c + 1;

            if (distinct > 1)
            {
                raced++;
                worstDistinct = Math.Max(worstDistinct, distinct);
            }
        }

        Console.WriteLine($"arm0  materialization race over one shared box");
        Console.WriteLine($"      {threads} threads x {trials} trials");
        Console.WriteLine($"      trials whose ONE table box handed out pointers into >1 backing array: {raced}/{trials} ({100.0 * raced / trials:n1}%)");
        Console.WriteLine($"      worst distinct backings in a single trial: {worstDistinct}");
        Console.WriteLine($"      TORN reads of the shared wrapper (threw out of .at()): {torn}");
        Console.Write("      distinct-backing histogram:");

        foreach (KeyValuePair<int, int> kv in histogram)
            Console.Write($"  {kv.Key}x{kv.Value}");

        Console.WriteLine();
        Console.WriteLine();

        return raced > 0 || torn > 0 ? 0 : 8;   // 8 == the mechanism did NOT reproduce here
    }

    // ---------------------------------------------------------------------------------------------
    // ARM 1/3 - bcache's own concurrent section, replicated: 100 goroutines x 1021 Puts into a fresh,
    // unregistered cache, then every thread reads its own keys back.
    // ---------------------------------------------------------------------------------------------
    // Runs the replica `trials` times and reports the DISTRIBUTION of lost entries - the quantity the
    // Go test asserts (`if lost != 0`).  A run with >1 backing does not necessarily lose anything: loss
    // needs an entry to have been PUSHED into an array that is later replaced.
    private static int RepeatReplica(bool prematerialize, int trials)
    {
        var lossHist = new SortedDictionary<int, int>();
        var backHist = new SortedDictionary<int, int>();
        int failed = 0;

        for (int t = 0; t < trials; t++)
        {
            (int lost, int distinct) = RunReplica(prematerialize, gcPressure: false);
            lossHist.TryGetValue(lost, out int lc);
            lossHist[lost] = lc + 1;
            backHist.TryGetValue(distinct, out int bc);
            backHist[distinct] = bc + 1;

            if (lost > 0)
                failed++;
        }

        Console.WriteLine($"{(prematerialize ? "arm2r" : "arm1r")}  bcache replica x {trials}  (prematerialize={prematerialize})");
        Console.WriteLine($"      trials that LOST at least one entry: {failed}/{trials} ({100.0 * failed / trials:n1}%)");
        Console.Write("      lost-entry histogram:");

        foreach (KeyValuePair<int, int> kv in lossHist)
            Console.Write($"  lost{kv.Key}x{kv.Value}");

        Console.WriteLine();
        Console.Write("      distinct-backing histogram:");

        foreach (KeyValuePair<int, int> kv in backHist)
            Console.Write($"  {kv.Key}x{kv.Value}");

        Console.WriteLine();

        return 0;
    }

    private static int Arm1_BcacheReplica(string label, bool prematerialize, bool gcPressure)
    {
        (int lostCount, int distinctCount) = RunReplica(prematerialize, gcPressure, out ConcurrentDictionary<object, int> backings);

        Console.WriteLine($"{label}");
        Console.WriteLine($"      100 threads x {Tbl.N} Puts = {100 * Tbl.N} entries");
        Console.WriteLine($"      lost entries: {lostCount}");
        Console.WriteLine($"      distinct backing arrays handed out by the ONE table box: {distinctCount}");

        foreach (KeyValuePair<object, int> kv in backings)
            Console.WriteLine($"        backing #{System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(kv.Key):x8}  head-pointer derivations: {kv.Value}");

        Console.WriteLine();
        return 0;
    }

    private static (int lost, int distinct) RunReplica(bool prematerialize, bool gcPressure)
    {
        return RunReplica(prematerialize, gcPressure, out _);
    }

    private static (int lost, int distinct) RunReplica(bool prematerialize, bool gcPressure, out ConcurrentDictionary<object, int> backings)
    {
        const int N = 100;
        const int PerThread = Tbl.N;

        var c = new Cache { Instrument = true };

        if (prematerialize)
            _ = c.table().at<atomic.Pointer<Node>>(0);

        var stop = new CancellationTokenSource();
        Thread pressure = null;

        if (gcPressure)
        {
            pressure = new Thread(() =>
            {
                while (!stop.IsCancellationRequested)
                {
                    GC.Collect(0, GCCollectionMode.Forced, blocking: false);
                    Thread.Sleep(1);
                }
            })
            { IsBackground = true };

            pressure.Start();
        }

        int lost = 0;
        using var barrier = new Barrier(N);
        var threads = new Thread[N];

        for (int i = 0; i < N; i++)
        {
            threads[i] = new Thread(() =>
            {
                var mine = new List<(ж<int> k, int v)>(PerThread);

                for (int j = 0; j < PerThread; j++)
                {
                    ж<int> k = NextKey();
                    int v = Interlocked.Increment(ref s_seq);
                    mine.Add((k, v));
                    c.Put(k, v);
                }

                barrier.SignalAndWait();

                foreach ((ж<int> k, int v) in mine)
                {
                    if (!c.TryGet(k, out int got) || got != v)
                        Interlocked.Increment(ref lost);
                }
            })
            { IsBackground = true };

            threads[i].Start();
        }

        foreach (Thread t in threads)
            t.Join();

        stop.Cancel();
        pressure?.Join();

        backings = c.Backings;
        return (lost, c.Backings.Count);
    }

    // ARM 2 - the identical workload with the backing materialized ONCE, single-threaded, before any
    // worker starts.  Same code path, same .at() per operation; the only variable removed is the
    // concurrent first materialization.
    private static int Arm2_Prematerialized()
    {
        return Arm1_BcacheReplica("arm2  table pre-materialized single-threaded before launch", prematerialize: true, gcPressure: false);
    }

    // ARM 4 - the same pushes, but the head pointers are derived from the backing array ONCE (no
    // per-operation .at() through the boxed wrapper).  Isolates .at() from the CAS itself.
    private static int Arm4_DirectBackingCas()
    {
        const int N = 100;
        const int PerThread = Tbl.N;

        ж<Tbl> box = @new<Tbl>();
        ж<atomic.Pointer<Node>>[] heads = new ж<atomic.Pointer<Node>>[Tbl.N];

        for (int i = 0; i < Tbl.N; i++)
            heads[i] = box.at<atomic.Pointer<Node>>(i);

        int lost = 0;
        using var barrier = new Barrier(N);
        var threads = new Thread[N];

        for (int i = 0; i < N; i++)
        {
            threads[i] = new Thread(() =>
            {
                var mine = new List<(ж<int> k, int v)>(PerThread);

                for (int j = 0; j < PerThread; j++)
                {
                    ж<int> k = NextKey();
                    int v = Interlocked.Increment(ref s_seq);
                    mine.Add((k, v));
                    PutDirect(heads, k, v);
                }

                barrier.SignalAndWait();

                foreach ((ж<int> k, int v) in mine)
                {
                    if (!TryGetDirect(heads, k, out int got) || got != v)
                        Interlocked.Increment(ref lost);
                }
            })
            { IsBackground = true };

            threads[i].Start();
        }

        foreach (Thread t in threads)
            t.Join();

        Console.WriteLine("arm4  head pointers derived ONCE (single-threaded) then reused - .at() removed from the hot path");
        Console.WriteLine($"      {N} threads x {PerThread} Puts = {N * PerThread} entries");
        Console.WriteLine($"      lost entries: {lost}");
        Console.WriteLine();

        return 0;
    }

    private static void PutDirect(ж<atomic.Pointer<Node>>[] heads, ж<int> k, int v)
    {
        ж<atomic.Pointer<Node>> head = heads[(nint)(k.PointerOrderToken % (nuint)Tbl.N)];

        ж<Node> add = null;
        ж<Node> noK = null;
        nint n = 0;

        while (true)
        {
            ж<Node> e = head.Load();
            ж<Node> start = e;

            for (; e is not null && !ReferenceEquals(e, noK); e = e.Value.next)
            {
                if (ReferenceEquals(e.Value.k, k))
                {
                    e.Value.v = v;
                    return;
                }

                n++;
            }

            if (add is null)
                add = Ꮡ(new Node { k = k, v = v });

            add.Value.next = start;

            if (n >= 1000)
                add.Value.next = null;

            if (head.CompareAndSwap(start, add))
                return;

            noK = start;
        }
    }

    private static bool TryGetDirect(ж<atomic.Pointer<Node>>[] heads, ж<int> k, out int v)
    {
        ж<atomic.Pointer<Node>> head = heads[(nint)(k.PointerOrderToken % (nuint)Tbl.N)];
        ж<Node> e = head.Load();

        for (; e is not null; e = e.Value.next)
        {
            if (ReferenceEquals(e.Value.k, k))
            {
                v = e.Value.v;
                return true;
            }
        }

        v = 0;
        return false;
    }
}
