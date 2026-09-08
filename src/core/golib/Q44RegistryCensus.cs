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

    internal static void Mint() => Interlocked.Increment(ref s_mints);

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

        // The pair is recorded by NAME rather than by Type so the dump needs no reflection at exit.
        string pair = $"{(atOffsetZero ? "2a" : "2b")}  requested={requested.Name}  resolved={box.GetType().Name}";
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
            ? named
            : System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                                     $"q44-census-{Environment.ProcessId}.txt");

    /// <summary>
    /// Writes the census to <see cref="OutputPath"/> (appending, so several hosts in one run each add
    /// a block) and to stderr as a secondary. Called from a process-exit hook AND callable directly,
    /// because whether that hook runs under a given test host is not a safe assumption.
    /// </summary>
    internal static void Dump() => DumpTo(OutputPath);

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

            sum == c
                ? $"Q44CENSUS-RECONCILES arms sum to {sum} == conversions {c}"
                : $"Q44CENSUS-BROKEN arms sum to {sum} but conversions is {c} -- the classification is NOT exhaustive",
        };

        foreach (var kv in s_arm2Pairs)
            lines.Add($"Q44CENSUS-ARM2 {kv.Value,8}  {kv.Key}");

        foreach (string line in lines)
            Console.Error.WriteLine(line);

        // The file is the channel that survives a host which swallows stderr. A failure to write is
        // reported rather than swallowed: an instrument that cannot report must say so.
        try
        {
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
        Console.Error.WriteLine("Q44CENSUS armed");
    }
}
