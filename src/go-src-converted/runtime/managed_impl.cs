// managed_impl.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// The runtime package's PROCESS-CONTROL surface, reimplemented on managed primitives.
//
// Everything here is a public runtime API whose Go body drives machinery that does not exist
// under the CLR — stopTheWorld/startTheWorld, gcStart and the mark/sweep engine, mcall(gosched_m),
// and the g/m/p stack walk. Converted faithfully they compile, then die on the first getg() or
// mcall() assembly stub: sync's TestPool died in debug.SetGCPercent, TestOnceXGC in runtime.GC →
// gcStart → acquirem → getg, TestParallelReaders in GOMAXPROCS → stopTheWorldGC → semacquire →
// getg, and runtime.Gosched → mcall took the whole test host down mid-run.
//
// The fork this takes is the one sync's Mutex/notifyList established (docs/Baseline-vs-
// FullConversion.md, "Hand-owning a package to make it OPERATIONAL"): where a Go mechanism has no
// managed counterpart but its PUBLIC CONTRACT does, reimplement the CONTRACT at the API boundary
// and never emulate the mechanism. The alternative — synthesizing a fake g/m so the converted
// scheduler can walk it — buys nothing: the code underneath would still need a real run queue, a
// real heap, and real stacks. Everything below these entry points stays auto-converted and simply
// becomes unreachable.
//
// The converter drops the auto forms of exactly these declarations (manualConversionFuncs
// ["runtime"] in go2cs/manualTypeOperations.go), leaving a placeholder comment at each site.
//
// Honest divergences, stated once:
//   - GOMAXPROCS is a real GET/SET of a remembered value but does NOT cap parallelism: a goroutine
//     is a managed thread and the CLR schedules it. The universal test idiom
//     `defer runtime.GOMAXPROCS(runtime.GOMAXPROCS(n))` is exactly right; a program that measures
//     actual parallelism against it is capability-divergent.
//   - Stack() walks the MANAGED stack. It cannot show frames that already unwound, so a
//     debug.Stack() called from a deferred function while a panic is in flight sees the deferred
//     frame's stack, not the panicking one (Go keeps the panicking frames alive until the panic
//     completes; a CLR exception pops them before the finally block runs). all=true reports only
//     the calling thread — the CLR has no supported way to walk another thread's stack.
//   - ReadMemStats fills the fields the CLR genuinely measures and leaves the allocator-internal
//     ones (Mallocs/Frees/HeapObjects/BySize, the per-pause histories) zero rather than inventing
//     numbers.
//   - Goexit is exact for the GOROUTINE case (defers run, recover() sees nil, no other goroutine is
//     affected) and GATED for the main goroutine, whose "main ends but the program keeps running"
//     shape has no managed counterpart yet — docs/Phase4/DESIGN-goexit.md option C.
//   - LockOSThread/UnlockOSThread are no-ops BY CONSTRUCTION, not by omission: go2cs runs each
//     goroutine on its own managed thread, so the guarantee they exist to provide — "this
//     goroutine will not be migrated to another OS thread" — already holds unconditionally.
//
// Hand-owned: there is no managed_impl.go, so a -stdlib reconvert never regenerates this file.

using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using go.golib;

[module: go.GoManualConversion]

namespace go;

partial class runtime_package
{
    // GOMAXPROCS' remembered setting. Go's starts at NumCPU.
    private static nint s_gomaxprocs = Environment.ProcessorCount;

    // GOMAXPROCS sets the maximum number of CPUs that can be executing simultaneously and returns
    // the previous setting. If n < 1, it does not change the current setting.
    public static nint GOMAXPROCS(nint n)
    {
        nint previous = Volatile.Read(ref s_gomaxprocs);

        if (n >= 1)
            Volatile.Write(ref s_gomaxprocs, n);

        return previous;
    }

    // Gosched yields the processor, allowing other goroutines to run. It does not suspend the
    // current goroutine, so execution resumes automatically.
    public static void Gosched()
    {
        // Thread.Yield offers the rest of the time slice to another ready thread on the same
        // processor and returns immediately either way — the same "give someone else a turn,
        // then carry on" contract Gosched has.
        Thread.Yield();
    }

    // registerPoolCleanup is where sync's //go:linkname runtime_registerPoolCleanup crosses into this
    // assembly. The symbol that linkname names, sync_runtime_registerPoolCleanup (mgc.cs), is
    // `internal` under the exported-ness rule, and a cross-assembly forwarder cannot reach an internal
    // target — the same constraint blockUntilEmptyFinalizerQueue documents in mfinal.cs. So sync calls
    // this shim, which hands the cleanup to the converted registration unchanged.
    public static void registerPoolCleanup(Action cleanup) => sync_runtime_registerPoolCleanup(cleanup);

    // GC runs a garbage collection and blocks the caller until the garbage collection is complete.
    public static void GC()
    {
        // Go's gcStart runs clearpools() at the START of every cycle, and that is what ages
        // sync.Pool's victim cache — without it a Pool never releases what it cached. Only the
        // sync.Pool arm of clearpools is wired here: the boringcrypto cache is cleared by pointer
        // stores that have no managed meaning, and unique's map cleanup is a channel handoff, both
        // separate arcs.
        if (poolcleanup != default!)
            poolcleanup();

        // Go's GC() is documented to complete a full cycle, and callers (sync's pool/oncefunc
        // tests among them) rely on finalizers having RUN by the time it returns. The second
        // collect reclaims what the finalizers released, matching the state a completed Go cycle
        // leaves behind.
        System.GC.Collect(System.GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        System.GC.WaitForPendingFinalizers();
        System.GC.Collect(System.GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
    }

    // Goexit terminates the goroutine that calls it. No other goroutine is affected. Goexit runs
    // all deferred calls before terminating the goroutine. Because Goexit is not a panic, any
    // recover calls in those deferred functions will return nil.
    public static void Goexit()
    {
        // Calling Goexit from the MAIN goroutine terminates main without returning while the
        // program keeps running its other goroutines — and crashes with "no goroutines" once they
        // all exit. That needs a live-goroutine registry and a main-thread parking protocol the
        // managed model does not have yet (DESIGN-goexit.md option A), so the main-goroutine case
        // stays GATED rather than silently doing something else: ending the process here would
        // kill goroutines Go would keep running. The gate is honest and loud, never a no-op.
        if (!Goroutine.OnGoroutine)
        {
            throw new NotSupportedException(
                "runtime.Goexit from the main goroutine is not supported: main-goroutine Goexit " +
                "must leave the other goroutines running (see docs/Phase4/DESIGN-goexit.md). " +
                "Goexit from a goroutine is fully supported.");
        }

        // The goroutine case: unwind. GoFunc's finally-based defer machinery runs this goroutine's
        // deferred calls on the way out, recover() cannot observe the unwind (GoexitException is
        // deliberately not a PanicException), and the goroutine root swallows it — Go's three
        // documented Goexit properties, each falling out of machinery that already existed.
        throw new GoexitException();
    }

    // Stack formats a stack trace of the calling goroutine into buf and returns the number of
    // bytes written to buf.
    public static nint Stack(slice<byte> buf, bool all)
    {
        // `all` would mean "every goroutine": the CLR has no supported cross-thread stack walk, so
        // the calling thread's trace is what can honestly be produced (see the header).
        string trace = new StackTrace(fNeedFileInfo: true).ToString();
        byte[] encoded = Encoding.UTF8.GetBytes(trace);
        nint count = Math.Min((nint)encoded.Length, len(buf));

        for (nint i = 0; i < count; i++)
            buf[i] = encoded[i];

        return count;
    }

    // ReadMemStats populates m with memory allocator statistics.
    public static void ReadMemStats(ж<MemStats> Ꮡm)
    {
        ref var m = ref Ꮡm.Value;

        GCMemoryInfo info = System.GC.GetGCMemoryInfo();

        uint64 live = (uint64)System.GC.GetTotalMemory(forceFullCollection: false);
        uint64 committed = (uint64)Math.Max(info.TotalCommittedBytes, 0L);

        m.Alloc = live;
        m.HeapAlloc = live;
        m.TotalAlloc = (uint64)System.GC.GetTotalAllocatedBytes(precise: false);
        m.Sys = committed;
        m.HeapSys = committed;
        m.HeapInuse = live;
        m.HeapIdle = committed > live ? committed - live : 0;
        m.NextGC = (uint64)Math.Max(info.HeapSizeBytes, 0L);
        m.PauseTotalNs = (uint64)(System.GC.GetTotalPauseDuration().Ticks * 100L);
        m.NumGC = (uint32)System.GC.CollectionCount(System.GC.MaxGeneration);
        m.EnableGC = true;

        // Deliberately left zero: Mallocs/Frees/Lookups/HeapObjects/HeapReleased, the Stack/MSpan/
        // MCache/BuckHash/GC/OtherSys breakdown, LastGC, and the PauseNs/PauseEnd/BySize histories
        // are Go-allocator bookkeeping the CLR does not expose (see the header).
    }

    // LockOSThread wires the calling goroutine to its current operating system thread.
    public static void LockOSThread()
    {
        // Already true by construction — a goroutine IS a managed thread here (see the header).
    }

    // UnlockOSThread undoes an earlier call to LockOSThread.
    public static void UnlockOSThread()
    {
    }

    // The runtime-internal variants, reached through syscall and startTemplateThread.
    internal static void lockOSThread()
    {
    }

    internal static void unlockOSThread()
    {
    }
}
