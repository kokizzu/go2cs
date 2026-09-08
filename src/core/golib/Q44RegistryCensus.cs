// Copyright 2026 The go2cs Authors. All rights reserved.
//
// THE Q44 §10.5 REGISTRY CENSUS -- dynamic, at the registry, because §10.5 rules out the alternatives
// by construction: the operators are reached through IMPLICIT conversions (every `unsafe.Pointer(&x)`
// in the corpus), so a call-site grep cannot find them, and frames inline, so a stack walk cannot
// attribute them. Attribution therefore rides on the CALL SITE classifying its own values -- the
// caller-supplied-tag discipline -- and this file is only the counters.
//
// WHAT IT COUNTS. The four arms of DESIGN-managed-pointer-token.md §10.3, decided at ж.cs's
// `uintptr -> ж<T>` operator from values the operator already has:
//
//   Resolve(n) is ж<T>                        ARM 1  same pointee type            (today's arm; pprof's case)
//   Resolve(n) non-null, NOT ж<T>             ARM 2  different pointee type       (the write's case)
//        ... and n == box.PointerOrderToken   ARM 2a offset 0 -- a prefix pun, expressible as an alias
//        ... and n != box.PointerOrderToken   ARM 2b offset != 0 via the pinned-provenance route
//   Resolve(n) null, IsTokenArithmetic(n)     ARM 3  inside a live block, not the token -- refuses today
//   Resolve(n) null, not token-arithmetic     ARM 4  a real address
//
// Mutually exclusive and exhaustive: every conversion lands in exactly one, which is what makes the
// totals reconcilable against the resolve count rather than merely suggestive.
//
// ⚠ ARM 2 IS NOT MERELY "NEW WORK". `IsTokenArithmetic` masks the low 32 bits and requires
// `allocationBase != number`, so it is FALSE when n IS the base. Arm 2 therefore reaches neither
// arm 3's refusal nor arm 1's alias -- it falls through to `new NativeBox<T>(n)`, a native box over a
// number that is not an address. Counting arm 2 is counting how often that happens today.
//
// OFF BY DEFAULT AND FREE WHEN OFF: one static bool read per conversion, and the counters are only
// touched when it is set. It is enabled by the environment variable named below so a census run needs
// no rebuild of anything but this file's consumers, and so no gate ever pays for it.

using System;
using System.Collections.Concurrent;
using System.Threading;

namespace go;

/// <summary>
/// Counters for the Q44 §10.5 registry census. Off unless <c>GO2CS_Q44_CENSUS</c> is set.
/// </summary>
internal static class Q44RegistryCensus
{
    // Read once. A census that can be switched on mid-run would make its own totals unreconcilable.
    internal static readonly bool Enabled =
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GO2CS_Q44_CENSUS"));

    private static long s_mints;
    private static long s_conversions;
    private static long s_arm1;
    private static long s_arm2a;
    private static long s_arm2b;
    private static long s_arm3;
    private static long s_arm4;

    // Per-arm TYPE PAIRS, because §10.5 asks "whether the pointee type matched" and a bare count
    // cannot answer falsifier (b) -- which needs to know WHICH types met at offset 0.
    private static readonly ConcurrentDictionary<string, long> s_arm2Pairs = new();

    // Conservative and CLOSED over fields: a struct counts as reference-bearing if it, or anything it
    // contains, is a managed reference. `RuntimeHelpers.IsReferenceOrContainsReferences<T>` answers
    // this exactly but needs a generic parameter, and here the type is only known as a `Type` -- so
    // the walk is explicit, and it fails REFERENCE-WARDS on anything it cannot decide, because the
    // consequence of a wrong "blittable" is corruption rather than a wrong answer.
    private static bool PointeeContainsReferences(Type t)
    {
        if (!t.IsValueType)
            return true;

        if (t.IsPrimitive || t.IsEnum || t.IsPointer)
            return false;

        foreach (var f in t.GetFields(System.Reflection.BindingFlags.Instance |
                                     System.Reflection.BindingFlags.Public |
                                     System.Reflection.BindingFlags.NonPublic))
        {
            if (f.FieldType == t)
                continue;

            if (PointeeContainsReferences(f.FieldType))
                return true;
        }

        return false;
    }

    internal static void Mint() => Interlocked.Increment(ref s_mints);

    // ⚠ A ResolveCalls COUNTER STOOD HERE AND IS GONE (2026-09-08), with its measurement kept so
    // nobody rebuilds it. It was added to make neutrality provable in a unit test, after the two
    // obvious formulations were shown not to discriminate: "a conversion must not change the
    // registered count" FAILS ON CORRECT CODE, because the one resolve the operator legitimately
    // performs evicts a dead weak entry whether the census is on or off; and counting evictions
    // cannot see a second call either, since two resolves of one token cannot evict twice. Counting
    // Resolve ENTRIES did discriminate -- and cost an Interlocked increment on a path taken 264,167
    // times in a single roster row, which is precisely the kind of work that makes an instrument
    // non-neutral. The lesson is the general one: an instrument built to prove a property of the
    // hot path, ON the hot path, is a perturbation wearing a proof's clothes. The gate is the
    // banked `os` row.

    internal static void Arm1() { Interlocked.Increment(ref s_conversions); Interlocked.Increment(ref s_arm1); }
    internal static void Arm3() { Interlocked.Increment(ref s_conversions); Interlocked.Increment(ref s_arm3); }
    internal static void Arm4() { Interlocked.Increment(ref s_conversions); Interlocked.Increment(ref s_arm4); }

    /// <summary>
    /// Arm 2: the resolve hit a box of a DIFFERENT pointee type. <paramref name="atOffsetZero"/>
    /// separates 2a (n IS the box's order token -- Go's prefix pun) from 2b (n is not, so the entry
    /// resolved through the pinned-provenance route instead).
    /// </summary>
    internal static void Arm2(Type requested, object box, bool atOffsetZero)
    {
        Interlocked.Increment(ref s_conversions);

        if (atOffsetZero)
            Interlocked.Increment(ref s_arm2a);
        else
            Interlocked.Increment(ref s_arm2b);

        // ⚠ THE POINTEE TYPE, not just the box's class. `box.GetType().Name` answers `StandardBox`1`
        // for every box in the corpus -- a name that cannot distinguish one pointee from another, and
        // the 2a remedy's soundness predicate is a question ABOUT THE POINTEE (is the storage
        // reference-bearing? does T fit inside it?). Recording the class alone would have produced a
        // corpus table that looks complete and cannot answer the question it was collected for.
        string resolvedName = box.GetType() is { IsGenericType: true } g
            ? $"{g.Name[..g.Name.IndexOf('`')]}<{string.Join(',', Array.ConvertAll(g.GetGenericArguments(), static t => t.Name))}>"
            : box.GetType().Name;

        // Whether the POINTEE storage carries a managed reference decides whether an offset-0 alias is
        // even expressible: Unsafe.As over mismatched GC layout is memory corruption, not a wrong
        // value. Recorded per site so the remedy can be sized against the sound and unsound halves
        // separately rather than against arm 2a as a lump.
        Type pointee = box.GetType() is { IsGenericType: true } gp ? gp.GetGenericArguments()[0] : box.GetType();
        bool pointeeHasRefs = !pointee.IsValueType || PointeeContainsReferences(pointee);
        bool requestedHasRefs = !requested.IsValueType || PointeeContainsReferences(requested);

        string pair = $"{(atOffsetZero ? "2a" : "2b")}  requested={requested.Name}" +
                      $"{(requestedHasRefs ? "(refs)" : "(blittable)")}  resolved={resolvedName}" +
                      $"{(pointeeHasRefs ? "(refs)" : "(blittable)")}" +
                      $"  alias-expressible={(atOffsetZero && !requestedHasRefs && !pointeeHasRefs ? "YES" : "NO")}";
        s_arm2Pairs.AddOrUpdate(pair, 1, static (_, n) => n + 1);
    }

    /// <summary>
    /// A readable snapshot, so the census's own positive control can assert that each counter MOVED
    /// rather than merely that the run produced a number. Without this the control could only compare
    /// the final totals against an expectation, which is exactly the shape that cannot tell a wired
    /// counter from an unwired one.
    /// </summary>
    internal static (long mints, long conversions, long arm1, long arm2a, long arm2b, long arm3, long arm4) Snapshot()
    {
        return (Interlocked.Read(ref s_mints), Interlocked.Read(ref s_conversions),
                Interlocked.Read(ref s_arm1), Interlocked.Read(ref s_arm2a),
                Interlocked.Read(ref s_arm2b), Interlocked.Read(ref s_arm3),
                Interlocked.Read(ref s_arm4));
    }

    /// <summary>
    /// The file the census reports into. ⚠ A FILE and not stderr, and that is measured rather than
    /// preferred: the first version wrote to stderr from a ProcessExit hook, every counter fired
    /// under the control, and the dump reached NO log -- the MSTest host swallows it. The counters
    /// were wired and the instrument could not report, which is the same defect as a counter that
    /// never moves and is harder to see, because the arms all looked healthy.
    /// </summary>
    internal static string OutputPath =>
        Environment.GetEnvironmentVariable("GO2CS_Q44_CENSUS_FILE") is { Length: > 0 } named
            ? named.Replace("{pid}", Environment.ProcessId.ToString())
            : System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                                     $"q44-census-{Environment.ProcessId}.txt");

    /// <summary>
    /// Writes the census to <see cref="OutputPath"/> and to stderr as a secondary. Called from a
    /// process-exit hook AND callable directly, because whether that hook runs under a given test
    /// host is not a safe assumption.
    /// </summary>
    /// <remarks>
    /// ⚠ ONE BLOCK PER PROCESS, which is one block per swept ROW. The first write in a process
    /// TRUNCATES; later writes in that same process append. Until 2026-09-08 every write appended,
    /// so a sweep that set one <c>GO2CS_Q44_CENSUS_FILE</c> for the host and ran several rows
    /// through it produced a file whose blocks a reader would sum — i9's ask, and the failure it
    /// prevents is arithmetic rather than loud. Two ways to keep rows apart, both encoded here so
    /// the runner needs no per-row logic: put <c>{pid}</c> in the path and each host gets its own
    /// file; or leave it out and the last row's block is what remains, cleanly, never two summed.
    /// The header line names the process and the entry assembly so a block is attributable either
    /// way.
    /// </remarks>
    internal static void Dump() => DumpTo(OutputPath);

    // Which paths this process has already written, so the FIRST write truncates and the rest append.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, bool> s_written = new();

    /// <summary>
    /// Dumps to an explicit path, so a caller that must not disturb a live census run (the control)
    /// can report into its own file.
    /// </summary>
    internal static void DumpTo(string path)
    {
        long c = Interlocked.Read(ref s_conversions);
        long a1 = Interlocked.Read(ref s_arm1), a2a = Interlocked.Read(ref s_arm2a);
        long a2b = Interlocked.Read(ref s_arm2b), a3 = Interlocked.Read(ref s_arm3), a4 = Interlocked.Read(ref s_arm4);

        // The reconciliation is computed rather than assumed: the arms must sum to the conversions,
        // or the classification is not exhaustive and no count below it means anything.
        long sum = a1 + a2a + a2b + a3 + a4;

        var lines = new System.Collections.Generic.List<string>
        {
            $"Q44CENSUS mints={Interlocked.Read(ref s_mints)} conversions={c} " +
            $"arm1={a1} arm2a={a2a} arm2b={a2b} arm3={a3} arm4={a4}",

            // Attribution, and it goes AFTER the totals rather than before: an existing control
            // requires the totals line to be FIRST and greppable, and it caught this line in the
            // wrong place the first time it ran.
            $"Q44CENSUS-BLOCK pid={Environment.ProcessId} " +
            $"entry={System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? "?"} " +
            $"utc={DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}",

            sum == c
                ? $"Q44CENSUS-RECONCILES arms sum to {sum} == conversions {c}"
                : $"Q44CENSUS-BROKEN arms sum to {sum} but conversions is {c} -- the classification is NOT exhaustive",
        };

        foreach (var kv in s_arm2Pairs)
            lines.Add($"Q44CENSUS-ARM2 {kv.Value,8}  {kv.Key}");

        // ⚠ NOTHING ROUTINE GOES TO stderr, and that is the whole of arm 2 (2026-09-08). These lines
        // used to be written here as a "secondary" channel. stderr is not a spare channel: it is a
        // stream the PROGRAM UNDER TEST owns, and the census arms in EVERY process that loads golib,
        // including the helper CHILDREN a package's own tests spawn and whose output they compare.
        // The environment carries the gate to those children unchanged -- childEnvWithGo2CSPath
        // copies the whole parent environment and scrubs only go2csPath -- so a child inherits the
        // census whether or not it was meant to be measured. MEASURED with a two-arm probe whose
        // child does ZERO census work: census OFF, child stderr 0 bytes; census ON, child stderr 222
        // bytes carrying "Q44CENSUS armed" at start and the whole block at exit. That is why `os`
        // flips while go/types and encoding/json do not -- os is the row whose tests spawn helpers.
        //
        // The file is the channel that survives a host which swallows stderr. A failure to write is
        // reported rather than swallowed: an instrument that cannot report must say so.
        try
        {
            if (s_written.TryAdd(path, true))
                System.IO.File.WriteAllLines(path, lines);
            else
                System.IO.File.AppendAllLines(path, lines);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Q44CENSUS-UNREPORTED could not write {path}: {e.GetType().Name}");
        }
    }

    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Arm()
    {
        if (!Enabled)
            return;

        AppDomain.CurrentDomain.ProcessExit += static (_, _) => Dump();

        // ⚠ NO "armed" LINE ON stderr. It fired in every process that loaded golib with the gate
        // set -- including a spawned helper child that does no census work at all -- and it is half
        // of the 222 bytes the probe measured. Whether the census armed is answerable from its
        // output file, which is where an instrument's report belongs.
    }
}
