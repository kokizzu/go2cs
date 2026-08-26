// B1 increment-2 microbench — the review's amendments 1A/2A/3/4/5, one harness.
//
// The banked probe (docs/phase4/probes/b1-box-dispatch/) discharged P-F2 for Value/ValueSlot and
// is NOT re-done here. This probe answers what the review found missing:
//
//   A4  the PARENT-mandated union-slot V2 (kind byte + switch + object-typed union storage slot,
//       DESIGN-zh-box-reduction.md:465-467), built faithfully BEFORE any elimination — measured
//       for time, bytes, and its COUNT consequence per kind
//   A3  the full kind-dispatch surface: PointerOrderToken / Equals / GetHashCode, transcribed
//       from ж.cs's real branch chains (identity semantics, AllocationBase math, token equality)
//       vs V5's per-kind overrides
//   A5  §5's ACTUAL element-ref shape: (object m_storage, nint m_index) canonicalized at
//       construction with a `storage is T[]` fast arm — benched on both arms (managed backing,
//       foreign IArray fallback) against the current interface-dispatch shape
//   2A  a Pointer-typed `Value` site: unsafe.Pointer's model subclass under the current
//       NON-virtual Value (binds directly) vs the redesign's virtual Value through the subclass —
//       through both subclass-typed and base-typed variables (the 875-conversion-site shape)
//   1A  per-cell dispersion: every cell reports median [min..max] across the 12 interleaved
//       rounds, and the runner executes N isolated processes per arm
//
// Protocol: 12 interleaved rounds per process, medians + spread; JIT (warmed, PGO) and Native
// AOT from the same source; 4 isolated processes per arm at the runner level.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable CS0649

internal delegate ref T FieldRefFunc<T>(object source);

internal interface IArrayM { object? Source { get; } }

internal interface IArrayM<T> : IArrayM
{
    ref T ElementRef(int index);
}

// Managed-backed array surrogate (slice<T>-like: exposes its backing as Source).
internal sealed class ArrayM<T> : IArrayM<T>
{
    private readonly T[] m_items;
    public ArrayM(T[] items) => m_items = items;
    public object? Source => m_items;
    public ref T ElementRef(int index) => ref m_items[index];
}

// FOREIGN array surrogate: no T[] behind it (CanonicalElement's fallback arms — a PinnedBuffer
// with no PinnedTarget, a null-Source slice, a foreign IArray).
internal sealed class ForeignArrayM<T> : IArrayM<T>
{
    private readonly T[] m_hidden;                 // storage exists but is NOT exposed as Source
    public ForeignArrayM(T[] hidden) => m_hidden = hidden;
    public object? Source => null;
    public ref T ElementRef(int index) => ref m_hidden[index];
}

internal sealed class Holder
{
    public long Field;
}

// ----------------------------------------------------------------------------------------
// W1 — current: layout AND the kind-branching identity surface, transcribed from ж.cs
// ----------------------------------------------------------------------------------------

internal class W1Box<T> : IEquatable<W1Box<T>>
{
    private readonly (object, FieldRefFunc<T>, Delegate)? m_structFieldRef;
    private readonly (IArrayM, int)? m_arrayIndexRef;
    private readonly bool m_isNull;
    private T m_val;
    private readonly T[]? m_slot;
    private readonly nuint m_nativeAddr;
    private object? m_pin;

    public W1Box(in T value)
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            m_val = value;
        else
        {
            m_val = default!;
            m_slot = [value];
        }
    }

    public W1Box(object source, FieldRefFunc<T> accessor, Delegate token)
    {
        m_val = default!;
        m_structFieldRef = (source, accessor, token);
    }

    public W1Box(IArrayM array, int index)
    {
        m_val = default!;
        m_arrayIndexRef = (array, index);
    }

    public W1Box(nuint nativeAddr)
    {
        m_val = default!;
        m_nativeAddr = nativeAddr;
    }

    public bool IsNilPointer => m_structFieldRef is null && m_arrayIndexRef is null && m_nativeAddr == 0 && m_isNull;

    public unsafe ref T Value
    {
        get
        {
            if (m_nativeAddr != 0)
                return ref Unsafe.AsRef<T>((void*)m_nativeAddr);

            if (m_structFieldRef is null && m_arrayIndexRef is null)
            {
                if (m_isNull)
                    throw new NullReferenceException();

                return ref m_slot is null ? ref m_val! : ref MemoryMarshal.GetArrayDataReference(m_slot);
            }

            if (m_structFieldRef is not null)
            {
                (object source, FieldRefFunc<T> fieldRefFunc, Delegate _) = m_structFieldRef!.Value;
                return ref fieldRefFunc(source);
            }

            (IArrayM array, int index) = m_arrayIndexRef!.Value;

            if (array is IArrayM<T> typedArray)
                return ref typedArray.ElementRef(index);

            throw new InvalidOperationException();
        }
    }

    private static (object storage, nint index) Canonical(IArrayM array, int index)
    {
        // The model's two-arm CanonicalElement: managed-backed resolves to the T[]; foreign keeps
        // the IArrayM (the real five arms all reduce to one of these two storages).
        return array.Source is T[] backing ? (backing, index) : (array, index);
    }

    private static nuint AllocationBase(int identityHash) =>
        unchecked((nuint)((ulong)(uint)identityHash << 32));

    // Transcribed kind-branch chain (ж.cs PointerOrderToken), field displacement simplified to a
    // token-hash term (same shape: base + within-allocation displacement).
    public virtual nuint PointerOrderToken
    {
        get
        {
            if (IsNilPointer)
                return 0;

            if (m_nativeAddr != 0)
                return m_nativeAddr;

            if (m_arrayIndexRef is not null)
            {
                (object storage, nint element) = Canonical(m_arrayIndexRef.Value.Item1, m_arrayIndexRef.Value.Item2);
                return unchecked(AllocationBase(RuntimeHelpers.GetHashCode(storage)) + (nuint)(uint)element);
            }

            if (m_structFieldRef is not null)
            {
                (object source, FieldRefFunc<T> _, Delegate fieldId) = m_structFieldRef.Value;
                return unchecked(AllocationBase(RuntimeHelpers.GetHashCode(source)) + (nuint)(uint)fieldId.GetHashCode());
            }

            return AllocationBase(RuntimeHelpers.GetHashCode(this));
        }
    }

    public virtual bool Equals(W1Box<T>? other)
    {
        if (other is null)
            return m_isNull;

        if (ReferenceEquals(this, other))
            return true;

        if (m_isNull || other.m_isNull)
            return m_isNull && other.m_isNull;

        if (m_nativeAddr != 0 || other.m_nativeAddr != 0)
            return m_nativeAddr == other.m_nativeAddr;

        if (m_structFieldRef is not null || other.m_structFieldRef is not null)
        {
            if (m_structFieldRef is null || other.m_structFieldRef is null)
                return false;

            (object source1, FieldRefFunc<T> _, Delegate fieldId1) = m_structFieldRef.Value;
            (object source2, FieldRefFunc<T> _, Delegate fieldId2) = other.m_structFieldRef.Value;

            return ReferenceEquals(source1, source2) && fieldId1.Equals(fieldId2);
        }

        if (m_arrayIndexRef is not null || other.m_arrayIndexRef is not null)
        {
            if (m_arrayIndexRef is null || other.m_arrayIndexRef is null)
                return false;

            (object s1, nint i1) = Canonical(m_arrayIndexRef.Value.Item1, m_arrayIndexRef.Value.Item2);
            (object s2, nint i2) = Canonical(other.m_arrayIndexRef.Value.Item1, other.m_arrayIndexRef.Value.Item2);

            return ReferenceEquals(s1, s2) && i1 == i2;
        }

        return false;
    }

    public override bool Equals(object? obj) => obj is W1Box<T> other && Equals(other);

    public override int GetHashCode()
    {
        if (IsNilPointer)
            return 0;

        if (m_structFieldRef is not null)
            return RuntimeHelpers.GetHashCode(m_structFieldRef.Value.Item1);

        if (m_arrayIndexRef is not null)
        {
            (object storage, nint element) = Canonical(m_arrayIndexRef.Value.Item1, m_arrayIndexRef.Value.Item2);
            return HashCode.Combine(RuntimeHelpers.GetHashCode(storage), element);
        }

        if (m_nativeAddr != 0)
            return m_nativeAddr.GetHashCode();

        return RuntimeHelpers.GetHashCode(this);
    }
}

// unsafe.Pointer under the CURRENT model: concrete subclass; Value is NON-virtual on the base and
// binds directly at Pointer-typed sites.
internal sealed class W1Ptr : W1Box<nuint>
{
    public W1Ptr(nuint value) : base(value) { }
    public override nuint PointerOrderToken => Value;
    public override bool Equals(W1Box<nuint>? other) => other is W1Ptr p ? PointerOrderToken == p.PointerOrderToken : base.Equals(other);
    public override int GetHashCode() => PointerOrderToken.GetHashCode();
}

// ----------------------------------------------------------------------------------------
// W2u — the PARENT-MANDATED union-slot variant: one class, kind byte + switch, object union
// ----------------------------------------------------------------------------------------

internal sealed class FieldPayload<T>
{
    public readonly object Source;
    public readonly FieldRefFunc<T> Accessor;
    public readonly Delegate Token;

    public FieldPayload(object source, FieldRefFunc<T> accessor, Delegate token)
    {
        Source = source;
        Accessor = accessor;
        Token = token;
    }
}

internal sealed class W2uBox<T>
{
    private const byte KindStandard = 0, KindFieldRef = 1, KindElemRef = 2, KindNative = 3;

    private readonly byte m_kind;
    private readonly bool m_isNull;
    private readonly object? m_union;      // standard: T[1] slot (managed T too — the count change)
                                           // fieldRef: FieldPayload<T> (+1 object — the count change)
                                           // elemRef: canonical storage (T[] or foreign IArrayM)
                                           // native: retained source (or null)
    private readonly nint m_index;
    private readonly nuint m_nativeAddr;
    private object? m_pin;

    public W2uBox(in T value)
    {
        m_kind = KindStandard;
        m_union = new T[] { value };       // managed T: +1 object vs today's inline m_val
    }

    public W2uBox(object source, FieldRefFunc<T> accessor, Delegate token)
    {
        m_kind = KindFieldRef;
        m_union = new FieldPayload<T>(source, accessor, token);   // +1 object vs today
    }

    public W2uBox(IArrayM array, int index)
    {
        m_kind = KindElemRef;

        if (array.Source is T[] backing)
        {
            m_union = backing;
            m_index = index;
        }
        else
        {
            m_union = array;
            m_index = index;
        }
    }

    public W2uBox(nuint nativeAddr)
    {
        m_kind = KindNative;
        m_nativeAddr = nativeAddr;
    }

    public bool IsNilStandardPointer => m_isNull;

    public unsafe ref T Value
    {
        get
        {
            switch (m_kind)
            {
                case KindStandard:
                    if (m_isNull)
                        throw new NullReferenceException();

                    return ref MemoryMarshal.GetArrayDataReference(Unsafe.As<T[]>(m_union!));

                case KindFieldRef:
                {
                    FieldPayload<T> p = Unsafe.As<FieldPayload<T>>(m_union!);
                    return ref p.Accessor(p.Source);
                }
                case KindElemRef:
                    if (m_union is T[] backing)
                        return ref backing[m_index];

                    return ref Unsafe.As<IArrayM<T>>(m_union!).ElementRef((int)m_index);

                default:
                    return ref Unsafe.AsRef<T>((void*)m_nativeAddr);
            }
        }
    }

    public ref T ValueSlot => ref Value;   // model: no null gate on the standard arm either way
}

internal static class W2uExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T DerefOrNull<T>(this W2uBox<T>? box)
    {
        if (box is null || box.IsNilStandardPointer)
            return ref Unsafe.NullRef<T>();

        return ref box.ValueSlot;
    }
}

// ----------------------------------------------------------------------------------------
// W5 — the landing shape, increment-2 form: per-kind storage, virtual accessors, base isNull;
// per-kind identity overrides; ElemRef in §5's REAL shape; Standard UNSEALED (unsafe.Pointer).
// ----------------------------------------------------------------------------------------

internal abstract class W5Box<T> : IEquatable<W5Box<T>>
{
    protected readonly bool m_isNull;

    protected W5Box(bool isNull = false) => m_isNull = isNull;

    public bool IsNilStandardPointer => m_isNull;

    public abstract ref T Value { get; }
    public abstract ref T ValueSlot { get; }
    public abstract nuint PointerOrderToken { get; }
    public abstract bool Equals(W5Box<T>? other);
    public override bool Equals(object? obj) => obj is W5Box<T> other && Equals(other);
    public override abstract int GetHashCode();

    protected static nuint AllocationBase(int identityHash) =>
        unchecked((nuint)((ulong)(uint)identityHash << 32));
}

internal class W5Standard<T> : W5Box<T>          // UNSEALED — unsafe.Pointer derives (P-F5)
{
    private T m_val;
    private readonly T[]? m_slot;
    private object? m_pin;

    public W5Standard(in T value, bool isNull = false) : base(isNull)
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            m_val = value;
        else
        {
            m_val = default!;
            m_slot = [value];
        }
    }

    public override ref T Value
    {
        get
        {
            if (m_isNull)
                throw new NullReferenceException();

            return ref m_slot is null ? ref m_val! : ref MemoryMarshal.GetArrayDataReference(m_slot);
        }
    }

    public override ref T ValueSlot =>
        ref m_slot is null ? ref m_val! : ref MemoryMarshal.GetArrayDataReference(m_slot);

    public override nuint PointerOrderToken =>
        m_isNull ? 0 : AllocationBase(RuntimeHelpers.GetHashCode(this));

    public override bool Equals(W5Box<T>? other)
    {
        if (other is null)
            return m_isNull;

        if (ReferenceEquals(this, other))
            return true;

        return m_isNull && other.IsNilStandardPointer;
    }

    public override int GetHashCode() => m_isNull ? 0 : RuntimeHelpers.GetHashCode(this);
}

internal sealed class W5FieldRef<T> : W5Box<T>
{
    private readonly object m_source;
    private readonly FieldRefFunc<T> m_accessor;
    private readonly Delegate m_token;

    public W5FieldRef(object source, FieldRefFunc<T> accessor, Delegate token)
    {
        m_source = source;
        m_accessor = accessor;
        m_token = token;
    }

    public override ref T Value => ref m_accessor(m_source);
    public override ref T ValueSlot => ref m_accessor(m_source);

    public override nuint PointerOrderToken =>
        unchecked(AllocationBase(RuntimeHelpers.GetHashCode(m_source)) + (nuint)(uint)m_token.GetHashCode());

    public override bool Equals(W5Box<T>? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return other is W5FieldRef<T> fr && ReferenceEquals(m_source, fr.m_source) && m_token.Equals(fr.m_token);
    }

    public override int GetHashCode() => RuntimeHelpers.GetHashCode(m_source);
}

// §5's real shape: canonical (object storage, nint index) resolved AT CONSTRUCTION; Value takes
// the T[] fast arm and falls back to the foreign interface arm.
internal sealed class W5ElemRef<T> : W5Box<T>
{
    private readonly object m_storage;     // T[] (canonical) or a foreign IArrayM
    private readonly nint m_index;

    public W5ElemRef(IArrayM array, int index)
    {
        if (array.Source is T[] backing)
        {
            m_storage = backing;
            m_index = index;               // model: canonical index (absolute) computed here
        }
        else
        {
            m_storage = array;
            m_index = index;
        }
    }

    public override ref T Value
    {
        get
        {
            if (m_storage is T[] backing)
                return ref backing[m_index];

            return ref Unsafe.As<IArrayM<T>>(m_storage).ElementRef((int)m_index);
        }
    }

    public override ref T ValueSlot => ref Value;

    public override nuint PointerOrderToken =>
        unchecked(AllocationBase(RuntimeHelpers.GetHashCode(m_storage)) + (nuint)(uint)m_index);

    public override bool Equals(W5Box<T>? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return other is W5ElemRef<T> er && ReferenceEquals(m_storage, er.m_storage) && m_index == er.m_index;
    }

    public override int GetHashCode() => HashCode.Combine(RuntimeHelpers.GetHashCode(m_storage), m_index);
}

internal sealed class W5Native<T> : W5Box<T>
{
    private readonly nuint m_nativeAddr;
    private object? m_pin;
    private object? m_retainedSource;

    // Amendment 7's construction contract: a zero address IS the nil pointer, marked at mint.
    public W5Native(nuint nativeAddr) : base(nativeAddr == 0) => m_nativeAddr = nativeAddr;

    public override unsafe ref T Value => ref Unsafe.AsRef<T>((void*)m_nativeAddr);
    public override unsafe ref T ValueSlot => ref Unsafe.AsRef<T>((void*)m_nativeAddr);

    public override nuint PointerOrderToken => m_nativeAddr;

    public override bool Equals(W5Box<T>? other)
    {
        if (other is null)
            return m_isNull;

        return other is W5Native<T> nb ? m_nativeAddr == nb.m_nativeAddr : m_isNull && other.IsNilStandardPointer;
    }

    public override int GetHashCode() => m_nativeAddr.GetHashCode();
}

// unsafe.Pointer under the REDESIGN: subclass of the (unsealed) standard kind; Value is VIRTUAL
// and dispatches through the hierarchy even at Pointer-typed sites.
internal sealed class W5Ptr : W5Standard<nuint>
{
    public W5Ptr(nuint value) : base(value, value == 0) { }
    public override nuint PointerOrderToken => IsNilStandardPointer ? 0 : ValueSlot;
    public override bool Equals(W5Box<nuint>? other) => other is W5Ptr p ? PointerOrderToken == p.PointerOrderToken : base.Equals(other);
    public override int GetHashCode() => PointerOrderToken.GetHashCode();
}

internal static class W5Ext
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T DerefOrNull<T>(this W5Box<T>? box)
    {
        if (box is null || box.IsNilStandardPointer)
            return ref Unsafe.NullRef<T>();

        return ref box.ValueSlot;
    }
}

internal static class W1Ext
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T DerefOrNull<T>(this W1Box<T>? box)
    {
        if (box is null || box.IsNilPointer)
            return ref Unsafe.NullRef<T>();

        return ref box.Value;
    }
}

// ----------------------------------------------------------------------------------------
// Harness
// ----------------------------------------------------------------------------------------

internal static class Program
{
    private const int Rounds = 12;
    private const int StdIters = 40_000_000;
    private const int IdIters = 20_000_000;
    private const int MixedIters = 8_000_000;

    private static long s_sink;
    private static nuint s_sinkN;
    private static bool s_sinkB;

    private static readonly Holder s_holder = new();
    private static readonly FieldRefFunc<long> s_accessor = static (object o) => ref Unsafe.As<Holder>(o).Field;
    private static long s_native;

    private static unsafe nuint NativeAddr()
    {
        fixed (long* p = &s_native)
            return (nuint)p;
    }

    private static (double median, double min, double max) Stats(List<double> samples)
    {
        List<double> s = [.. samples];
        s.Sort();
        int n = s.Count;
        double median = n % 2 == 1 ? s[n / 2] : (s[n / 2 - 1] + s[n / 2]) / 2.0;
        return (median, s[0], s[^1]);
    }

    // ---- generic timing helper: each CALL SITE below is its own generic body via the thunk ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double Time(Func<int, long> body, int iters)
    {
        Stopwatch sw = Stopwatch.StartNew();
        s_sink += body(iters);
        sw.Stop();
        return sw.Elapsed.TotalNanoseconds / iters;
    }

    private static void Report(string name, List<double>[] cells, string[] cols)
    {
        Console.Write($"{name,-22}");

        double baseMedian = 0;

        for (int i = 0; i < cells.Length; i++)
        {
            (double med, double min, double max) = Stats(cells[i]);

            if (i == 0)
                baseMedian = med;

            Console.Write($" {med,8:F3} [{min,6:F3}-{max,6:F3}]");
        }

        Console.Write("   ");

        for (int i = 1; i < cells.Length; i++)
        {
            (double med, _, _) = Stats(cells[i]);
            Console.Write($" {med / baseMedian,5:F2}x");
        }

        Console.WriteLine($"   ({string.Join("/", cols)})");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long MeasureAlloc(Func<object> mint)
    {
        object?[] keep = new object?[64];
        var samples = new List<double>(5);

        for (int b = 0; b < 5; b++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            long before = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < 64; i++)
                keep[i] = mint();

            samples.Add((GC.GetAllocatedBytesForCurrentThread() - before) / 64.0);
            GC.KeepAlive(keep);
        }

        samples.Sort();
        return (long)samples[2];
    }

    private static void Main()
    {
        bool isAot = !RuntimeFeature.IsDynamicCodeSupported;
        Console.WriteLine($"runtime: {(isAot ? "Native AOT" : "JIT (CoreCLR)")}  {Environment.Version}  {RuntimeInformation.OSArchitecture}  pid {Environment.ProcessId}");
        Console.WriteLine($"rounds: {Rounds} interleaved; cells report median [min-max] ns/op");

        // ---- fixtures ----
        var std1 = new W1Box<long>(1);
        var std2 = new W2uBox<long>(1);
        var std5 = (W5Box<long>)new W5Standard<long>(1);

        var fr1a = new W1Box<long>(s_holder, s_accessor, s_accessor);
        var fr1b = new W1Box<long>(s_holder, s_accessor, s_accessor);
        var fr2 = new W2uBox<long>(s_holder, s_accessor, s_accessor);
        var fr5a = (W5Box<long>)new W5FieldRef<long>(s_holder, s_accessor, s_accessor);
        var fr5b = (W5Box<long>)new W5FieldRef<long>(s_holder, s_accessor, s_accessor);

        long[] backing = new long[16];
        var managedArr = new ArrayM<long>(backing);
        var foreignArr = new ForeignArrayM<long>(new long[16]);

        var el1 = new W1Box<long>(managedArr, 3);
        var el1f = new W1Box<long>(foreignArr, 3);
        var el2 = new W2uBox<long>(managedArr, 3);
        var el5 = (W5Box<long>)new W5ElemRef<long>(managedArr, 3);
        var el5f = (W5Box<long>)new W5ElemRef<long>(foreignArr, 3);

        nuint nat = NativeAddr();
        var nb1 = new W1Box<long>(nat);
        var nb2 = new W2uBox<long>(nat);
        var nb5 = (W5Box<long>)new W5Native<long>(nat);

        var p1 = new W1Ptr(nat);
        var p5 = new W5Ptr(nat);
        W1Box<nuint> p1Base = p1;
        W5Box<nuint> p5Base = p5;

        // mixed-kind pools (90/8/1.5/0.5), same construction as the banked probe
        const int N = 4096;
        var w1mix = new W1Box<long>[N];
        var w2mix = new W2uBox<long>[N];
        var w5mix = new W5Box<long>[N];
        var rnd = new Random(20260826);

        for (int i = 0; i < N; i++)
        {
            double r = rnd.NextDouble();

            if (r < 0.90)
            {
                w1mix[i] = new W1Box<long>(7); w2mix[i] = new W2uBox<long>(7); w5mix[i] = new W5Standard<long>(7);
            }
            else if (r < 0.98)
            {
                w1mix[i] = fr1a; w2mix[i] = fr2; w5mix[i] = fr5a;
            }
            else if (r < 0.995)
            {
                w1mix[i] = el1; w2mix[i] = el2; w5mix[i] = el5;
            }
            else
            {
                w1mix[i] = nb1; w2mix[i] = nb2; w5mix[i] = nb5;
            }
        }

        // ---- workload bodies (lambdas over the fixtures; NoInlining timer isolates each) ----

        Func<int, long> v1Std = it => { long a = 0; for (int i = 0; i < it; i++) { std1.Value++; a += std1.Value; } return a; };
        Func<int, long> v2Std = it => { long a = 0; for (int i = 0; i < it; i++) { std2.Value++; a += std2.Value; } return a; };
        Func<int, long> v5Std = it => { long a = 0; for (int i = 0; i < it; i++) { std5.Value++; a += std5.Value; } return a; };

        Func<int, long> v1Deref = it => { long a = 0; for (int i = 0; i < it; i++) a += std1.DerefOrNull(); return a; };
        Func<int, long> v2Deref = it => { long a = 0; for (int i = 0; i < it; i++) a += std2.DerefOrNull(); return a; };
        Func<int, long> v5Deref = it => { long a = 0; for (int i = 0; i < it; i++) a += std5.DerefOrNull(); return a; };

        Func<int, long> v1Fr = it => { long a = 0; for (int i = 0; i < it; i++) a += fr1a.Value; return a; };
        Func<int, long> v2Fr = it => { long a = 0; for (int i = 0; i < it; i++) a += fr2.Value; return a; };
        Func<int, long> v5Fr = it => { long a = 0; for (int i = 0; i < it; i++) a += fr5a.Value; return a; };

        Func<int, long> v1Mix = it => { long a = 0; for (int i = 0; i < it; i++) a += w1mix[i & (N - 1)].Value; return a; };
        Func<int, long> v2Mix = it => { long a = 0; for (int i = 0; i < it; i++) a += w2mix[i & (N - 1)].Value; return a; };
        Func<int, long> v5Mix = it => { long a = 0; for (int i = 0; i < it; i++) a += w5mix[i & (N - 1)].ValueSlot; return a; };

        Func<int, long> v1Nat = it => { long a = 0; for (int i = 0; i < it; i++) a += nb1.Value; return a; };
        Func<int, long> v2Nat = it => { long a = 0; for (int i = 0; i < it; i++) a += nb2.Value; return a; };
        Func<int, long> v5Nat = it => { long a = 0; for (int i = 0; i < it; i++) a += nb5.Value; return a; };

        // A3 — identity surface
        Func<int, long> v1TokStd = it => { nuint a = 0; for (int i = 0; i < it; i++) a ^= std1.PointerOrderToken; s_sinkN ^= a; return 0; };
        Func<int, long> v5TokStd = it => { nuint a = 0; for (int i = 0; i < it; i++) a ^= std5.PointerOrderToken; s_sinkN ^= a; return 0; };
        Func<int, long> v1TokEl = it => { nuint a = 0; for (int i = 0; i < it; i++) a ^= el1.PointerOrderToken; s_sinkN ^= a; return 0; };
        Func<int, long> v5TokEl = it => { nuint a = 0; for (int i = 0; i < it; i++) a ^= el5.PointerOrderToken; s_sinkN ^= a; return 0; };
        Func<int, long> v1EqFr = it => { bool b = false; for (int i = 0; i < it; i++) b ^= fr1a.Equals(fr1b); s_sinkB ^= b; return 0; };
        Func<int, long> v5EqFr = it => { bool b = false; for (int i = 0; i < it; i++) b ^= fr5a.Equals(fr5b); s_sinkB ^= b; return 0; };
        Func<int, long> v1HashEl = it => { long a = 0; for (int i = 0; i < it; i++) a += el1.GetHashCode(); return a; };
        Func<int, long> v5HashEl = it => { long a = 0; for (int i = 0; i < it; i++) a += el5.GetHashCode(); return a; };

        // A5 — element-ref arms
        Func<int, long> v1ElFast = it => { long a = 0; for (int i = 0; i < it; i++) a += el1.Value; return a; };
        Func<int, long> v5ElFast = it => { long a = 0; for (int i = 0; i < it; i++) a += el5.Value; return a; };
        Func<int, long> v1ElForeign = it => { long a = 0; for (int i = 0; i < it; i++) a += el1f.Value; return a; };
        Func<int, long> v5ElForeign = it => { long a = 0; for (int i = 0; i < it; i++) a += el5f.Value; return a; };

        // 2A — Pointer-typed Value sites (subclass-typed and base-typed)
        Func<int, long> v1PtrSub = it => { nuint a = 0; for (int i = 0; i < it; i++) a ^= p1.Value; s_sinkN ^= a; return 0; };
        Func<int, long> v5PtrSub = it => { nuint a = 0; for (int i = 0; i < it; i++) a ^= p5.Value; s_sinkN ^= a; return 0; };
        Func<int, long> v1PtrBase = it => { nuint a = 0; for (int i = 0; i < it; i++) a ^= p1Base.Value; s_sinkN ^= a; return 0; };
        Func<int, long> v5PtrBase = it => { nuint a = 0; for (int i = 0; i < it; i++) a ^= p5Base.Value; s_sinkN ^= a; return 0; };

        // ---- warmup ----
        Func<int, long>[] all = [v1Std, v2Std, v5Std, v1Deref, v2Deref, v5Deref, v1Fr, v2Fr, v5Fr,
                                 v1Mix, v2Mix, v5Mix, v1Nat, v2Nat, v5Nat,
                                 v1TokStd, v5TokStd, v1TokEl, v5TokEl, v1EqFr, v5EqFr, v1HashEl, v5HashEl,
                                 v1ElFast, v5ElFast, v1ElForeign, v5ElForeign,
                                 v1PtrSub, v5PtrSub, v1PtrBase, v5PtrBase];

        for (int w = 0; w < 3; w++)
            foreach (Func<int, long> body in all)
                Time(body, 1_000_000);

        // ---- rounds ----
        (string name, Func<int, long>[] arms, string[] cols, int iters)[] rows =
        [
            ("std-Value(rw)", [v1Std, v2Std, v5Std], ["V2u", "V5"], StdIters),
            ("std-DerefOrNull", [v1Deref, v2Deref, v5Deref], ["V2u", "V5"], StdIters),
            ("fieldRef-Value", [v1Fr, v2Fr, v5Fr], ["V2u", "V5"], IdIters),
            ("mixed-90/8/1.5/.5", [v1Mix, v2Mix, v5Mix], ["V2u", "V5"], MixedIters),
            ("native-Value", [v1Nat, v2Nat, v5Nat], ["V2u", "V5"], StdIters),
            ("token-std", [v1TokStd, v5TokStd], ["V5"], IdIters),
            ("token-elemRef", [v1TokEl, v5TokEl], ["V5"], IdIters),
            ("equals-fieldRef", [v1EqFr, v5EqFr], ["V5"], IdIters),
            ("hashcode-elemRef", [v1HashEl, v5HashEl], ["V5"], IdIters),
            ("elem-Value-managed", [v1ElFast, v5ElFast], ["V5"], StdIters),
            ("elem-Value-foreign", [v1ElForeign, v5ElForeign], ["V5"], IdIters),
            ("ptrVal-subtyped", [v1PtrSub, v5PtrSub], ["V5"], StdIters),
            ("ptrVal-basetyped", [v1PtrBase, v5PtrBase], ["V5"], StdIters),
        ];

        var cells = new List<double>[rows.Length][];

        for (int r = 0; r < rows.Length; r++)
        {
            cells[r] = new List<double>[rows[r].arms.Length];

            for (int a = 0; a < rows[r].arms.Length; a++)
                cells[r][a] = [];
        }

        for (int round = 0; round < Rounds; round++)
            for (int r = 0; r < rows.Length; r++)
                for (int a = 0; a < rows[r].arms.Length; a++)
                    cells[r][a].Add(Time(rows[r].arms[a], rows[r].iters));

        Console.WriteLine();
        Console.WriteLine($"{"workload",-22} {"V1-current",24} {"(next arms)",24}");

        for (int r = 0; r < rows.Length; r++)
            Report(rows[r].name, cells[r], rows[r].cols);

        // ---- sizes + count consequences ----
        Console.WriteLine();
        Console.WriteLine("== per-box allocated bytes (median of 5x64 mints) ==");
        Console.WriteLine($"{"kind",-22} {"V1",8} {"V2u",8} {"V5i2",8}   (V2u objects vs today / V5 objects vs today)");

        var holder = s_holder;
        FieldRefFunc<long> acc = s_accessor;

        Console.WriteLine($"{"standard long",-22} {MeasureAlloc(() => new W1Box<long>(7)),8} {MeasureAlloc(() => new W2uBox<long>(7)),8} {MeasureAlloc(() => new W5Standard<long>(7)),8}   (2 vs 2 / 2 vs 2)");
        Console.WriteLine($"{"standard managed(obj)",-22} {MeasureAlloc(() => new W1Box<object>(new object())),8} {MeasureAlloc(() => new W2uBox<object>(new object())),8} {MeasureAlloc(() => new W5Standard<object>(new object())),8}   (2 vs 1: +1 COUNT / 1 vs 1)");
        Console.WriteLine($"{"fieldRef long",-22} {MeasureAlloc(() => new W1Box<long>(holder, acc, acc)),8} {MeasureAlloc(() => new W2uBox<long>(holder, acc, acc)),8} {MeasureAlloc(() => new W5FieldRef<long>(holder, acc, acc)),8}   (2 vs 1: +1 COUNT / 1 vs 1)");
        Console.WriteLine($"{"elemRef managed",-22} {MeasureAlloc(() => new W1Box<long>(managedArr, 1)),8} {MeasureAlloc(() => new W2uBox<long>(managedArr, 1)),8} {MeasureAlloc(() => new W5ElemRef<long>(managedArr, 1)),8}   (1 vs 1 / 1 vs 1)");
        Console.WriteLine($"{"native",-22} {MeasureAlloc(() => new W1Box<long>(nat)),8} {MeasureAlloc(() => new W2uBox<long>(nat)),8} {MeasureAlloc(() => new W5Native<long>(nat)),8}   (1 vs 1 / 1 vs 1)");

        Console.WriteLine();
        Console.WriteLine($"(sinks {s_sink} {s_sinkN} {s_sinkB})");
    }
}
