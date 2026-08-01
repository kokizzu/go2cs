// stubs_impl.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// Hand-written bodies for runtime/debug's "Implemented in package runtime" declarations.
//
// These are bodyless Go funcs (runtime/debug/stubs.go, garbage.go, mod.go, stack.go) that the Go
// LINKER binds to a runtime function carrying the matching `//go:linkname <impl> runtime/debug.<name>`
// PUSH directive. go2cs has no cross-assembly linker, so it emits them as bodyless `partial`
// methods and the PartialStubGenerator fills each with a throwing stub — debug.SetGCPercent threw
// on its first call, which is the very first line of sync's TestPool / TestPoolNew.
//
// Forwarding to the converted runtime implementations would NOT help: every one of them drives
// Go's own GC pacer / scheduler (runtime.setGCPercent takes the mheap lock on the system stack and
// waits on a mark cycle), machinery that does not exist under the CLR. This is the same fork the
// sync runtime layer took — reimplement the SEMANTIC on managed primitives at the API boundary,
// never emulate the mechanism.
//
// What each knob means here, and where it honestly diverges:
//   - setGCPercent / setMemoryLimit / setMaxStack / setMaxThreads are TUNING knobs. The CLR's GC
//     and thread pool expose no equivalent runtime-settable analog, so each keeps Go's documented
//     GET/SET CONTRACT (remember the value, return the previous one, negative input = query only
//     where Go says so) and has no effect on collection. Programs that only save-and-restore them
//     — `defer debug.SetGCPercent(debug.SetGCPercent(-1))`, the standard test idiom — are exactly
//     right; a program that *asserts collection behaviour changed* is capability-divergent.
//   - freeOSMemory is a REAL operation: a blocking full collect plus a compacting LOH pass is the
//     managed equivalent of "collect and return memory to the OS".
//   - setPanicOnFault is per-GOROUTINE in Go, so it is [ThreadStatic] here (a goroutine is a
//     managed thread in go2cs). It has no effect: the CLR turns a bad access into an
//     AccessViolationException the process cannot resume from.
//   - readGCStats reports the aggregate counters the CLR really has (collection count, total
//     pause) and an EMPTY per-pause history — the CLR keeps no comparable pause/end-time log, and
//     fabricating one would be worse than reporting none. The packed layout ReadGCStats expects
//     (n pauses, n ends, lastGC, numGC, totalPause) is honored exactly.
//   - modinfo / WriteHeapDump / SetTraceback / runtime_setCrashFD have no managed form; they are
//     inert, matching what a binary built without module info or heap-dump support reports.
//
// Hand-owned: there is no stubs_impl.go, so a -stdlib reconvert never regenerates this file.

using System;
using System.Threading;

[module: go.GoManualConversion]

namespace go.runtime;

using time = go.time_package;

partial class debug_package
{
    // Go's own defaults, so a first GET returns what Go would report.
    private static int32 s_gcPercent = 100;                 // GOGC=100
    private static int64 s_memoryLimit = int64.MaxValue;    // math.MaxInt64 — "no limit"
    private static nint s_maxStack = 1_000_000_000;         // runtime.maxstacksize on 64-bit
    private static nint s_maxThreads = 10_000;              // runtime sched.maxmcount
    private static uintptr s_crashFD = ~(uintptr)0;         // ^uintptr(0) — "unset"

    // paniconfault is a per-goroutine flag in Go; a goroutine is a managed thread here.
    [ThreadStatic]
    private static bool t_panicOnFault;

    internal static partial int32 setGCPercent(int32 @in)
    {
        int32 old = Interlocked.Exchange(ref s_gcPercent, @in);

        // Go's setGCPercent waits out any in-flight mark when GC is being disabled, so the caller
        // returns with no collection running. Draining pending finalizers is the closest managed
        // equivalent and keeps the "quiet heap on return" property the test idiom relies on.
        if (@in < 0)
            GC.WaitForPendingFinalizers();

        return old;
    }

    internal static partial int64 setMemoryLimit(int64 @in)
    {
        // Documented: a negative input does not adjust the limit, it only reads it back.
        return @in < 0 ? Interlocked.Read(ref s_memoryLimit) : Interlocked.Exchange(ref s_memoryLimit, @in);
    }

    internal static partial nint setMaxStack(nint @in)
    {
        return Interlocked.Exchange(ref s_maxStack, @in);
    }

    internal static partial nint setMaxThreads(nint @in)
    {
        return Interlocked.Exchange(ref s_maxThreads, @in);
    }

    internal static partial bool setPanicOnFault(bool @new)
    {
        bool old = t_panicOnFault;
        t_panicOnFault = @new;
        return old;
    }

    internal static partial void freeOSMemory()
    {
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
    }

    internal static partial void readGCStats(ж<slice<time.Duration>> Ꮡp)
    {
        // ReadGCStats' packed layout: [n pauses][n pause end times][lastGC unix-ns][numGC][total
        // pause]. The CLR exposes the aggregate counters but no per-pause history, so n == 0.
        const nint entries = 3;

        slice<time.Duration> buffer = Ꮡp.Value;

        if (cap(buffer) < entries)
            buffer = new slice<time.Duration>(entries);

        buffer = buffer[..(int)entries];

        buffer[0] = 0;                                                          // lastGC: unknown
        buffer[1] = (time.Duration)(int64)GC.CollectionCount(GC.MaxGeneration); // numGC: real
        buffer[2] = (time.Duration)GC.GetTotalPauseDuration().Ticks * 100;      // total pause: real

        Ꮡp.Value = buffer;
    }

    internal static partial @string modinfo()
    {
        // Go returns the module info the linker embedded; a binary built without it returns "",
        // and ReadBuildInfo then reports ok == false. That is the honest answer here.
        return ""u8;
    }

    public static partial void WriteHeapDump(uintptr fd)
    {
        throw panic((@string)"runtime/debug: WriteHeapDump is not supported by the managed runtime"u8);
    }

    public static partial void SetTraceback(@string level)
    {
        // Traceback verbosity governs the Go runtime's own crash printer, which the managed
        // runtime does not use; the call is accepted and inert, as it is in a Go binary whose
        // GOTRACEBACK is already at or above the requested level.
    }

    internal static partial uintptr runtime_setCrashFD(uintptr fd)
    {
        // Go redirects the runtime's fatal-error output to fd and returns the previous one
        // (^uintptr(0) when unset, which tells SetCrashOutput not to close anything). The managed
        // runtime writes crashes through its own handler, so the slot is remembered but inert.
        uintptr previous = s_crashFD;
        s_crashFD = fd;
        return previous;
    }
}
