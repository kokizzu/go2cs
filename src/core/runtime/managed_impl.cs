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
//   - Stack() walks the MANAGED stack and renders it in GO'S SHAPE (`<pkg>.<Func>()` + a
//     tab-indented `<file>:<line>`), because a traceback is observable output that Go programs and
//     Go's own tests grep by package-qualified function name. Frames that already unwound are
//     recovered for the case that matters: while a panic is in flight, golib's snapshot of the
//     panic's origin is appended below the live frames — where Go's traceback also shows them,
//     since a deferred call runs on top of gopanic. Frames that are not converted Go code (golib,
//     the BCL, the test host) keep their .NET names rather than being given invented Go ones, and
//     Go's `+0x<offset>` PC deltas are omitted. all=true reports only the calling thread — the CLR
//     has no supported way to walk another thread's stack.
//   - ReadMemStats fills the fields the CLR genuinely measures and leaves the allocator-internal
//     ones (Mallocs/Frees/HeapObjects/BySize, the per-pause histories) zero rather than inventing
//     numbers.
//   - Goexit is exact for the GOROUTINE case (defers run, recover() sees nil, no other goroutine is
//     affected) and GATED for the main goroutine, whose "main ends but the program keeps running"
//     shape has no managed counterpart yet — docs/phase4/DESIGN-goexit.md option C.
//   - LockOSThread/UnlockOSThread are no-ops BY CONSTRUCTION, not by omission: go2cs runs each
//     goroutine on its own managed thread, so the guarantee they exist to provide — "this
//     goroutine will not be migrated to another OS thread" — already holds unconditionally.
//   - Callers()/Frames.Next() walk the MANAGED stack projected to GO-LOGICAL frames: only
//     converted Go declarations and function literals count — adapter shells (IGoAdapter) and
//     go2cs-gen forwarders are dispatch plumbing Go has no frame for, and golib/the BCL/the test
//     host are not Go code. RELATIVE depths between two Callers calls on one goroutine therefore
//     match Go's logical model (io's multiReader flatten tests assert exactly this); ABSOLUTE
//     depth reflects the managed host's own frames below main. PC values are opaque
//     process-lifetime tokens, never addresses; Frame.Function is the Go spelling (goFrameName);
//     Frame.File/Line name the CONVERTED .cs source, the source that honestly exists. FuncForPC
//     and Frame.Func stay unimplemented/nil — a *Func has no managed referent. getcallersp
//     itself remains an honest stub: a caller's stack pointer has no managed answer, so the
//     chain is severed HERE, at the API boundary that does (the methodName precedent).
//
// Hand-owned: there is no managed_impl.go, so a -stdlib reconvert never regenerates this file.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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
        // sync.Pool's victim cache — without it a Pool never releases what it cached. Two of
        // clearpools' three arms are wired here; the boringcrypto cache is the one that is not,
        // because it is cleared by `atomicstorep(p, nil)` pointer stores that have no managed
        // meaning.
        if (poolcleanup != default!)
            poolcleanup();

        // unique's map cleanup, the second arm — verbatim clearpools(): a NON-BLOCKING send that
        // wakes the goroutine unique_runtime_registerUniqueMapCleanup parked on this channel, which
        // evicts every intern-map entry whose weak pointer has gone nil. Inert until unique.Make has
        // run (the channel is nil before registration), so nothing else pays for it.
        //
        // Wiring it is not cosmetic: `unique`'s own suite calls drainMaps() — arm a one-shot
        // notification, runtime.GC(), then BLOCK on `<-wait` until the cleanup runs — so with this
        // arm missing the cleanup could never run and every TestHandle subtest deadlocked, taking
        // the whole test host to its package timeout and erasing the verdicts of the rows that had
        // nothing to do with it. That deadlock only became REACHABLE once internal/weak stopped
        // panicking (its hand-own, same arc); before that the subtests died one frame earlier.
        if (uniqueMapCleanup != default!)
            uniqueMapCleanup.TrySend(new EmptyStruct());

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
                "must leave the other goroutines running (see docs/phase4/DESIGN-goexit.md). " +
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
        StringBuilder trace = new();

        trace.Append("goroutine 1 [running]:\n");
        appendGoFrames(trace, new StackTrace(skipFrames: 1, fNeedFileInfo: true));

        // Go keeps a panicking goroutine's frames on the stack until the panic completes, so a
        // debug.Stack() taken inside a deferred function shows the PANIC SITE. A CLR exception has
        // already unwound those frames before the finally-based defer runs, so the frames Go would
        // still be showing are appended from the in-flight panic's snapshot — below the live frames,
        // which is where Go's traceback puts them too (the deferred call runs on top of gopanic).
        PanicException? inFlight = GoFuncRoot.InFlightPanic;

        if (inFlight?.PanicTrace is StackTrace panicSite && panicSite.FrameCount > 0)
        {
            trace.Append("panic: ").Append(inFlight.State?.ToString() ?? "nil").Append('\n');
            appendGoFrames(trace, panicSite);
        }

        byte[] encoded = Encoding.UTF8.GetBytes(trace.ToString());
        nint count = Math.Min((nint)encoded.Length, len(buf));

        for (nint i = 0; i < count; i++)
            buf[i] = encoded[i];

        return count;
    }

    // Renders frames the way Go's traceback does — `<pkg>.<Func>()` on one line, a tab-indented
    // `<file>:<line>` beneath it — rather than the CLR's `at <Namespace>.<Type>.<Method>(...) in
    // <file>:line <n>`. This is observable output: Go programs (and Go's own tests) grep a traceback
    // for `<pkg>.<Func>`, which the CLR form never contains because a converted package's frames
    // live on a `<pkg>_package` class inside namespace `go`.
    private static void appendGoFrames(StringBuilder trace, StackTrace stack)
    {
        foreach (StackFrame frame in stack.GetFrames())
        {
            System.Reflection.MethodBase? method = frame.GetMethod();

            if (method is null)
                continue;

            trace.Append(goFrameName(method)).Append("()\n");

            string? file = frame.GetFileName();

            if (!string.IsNullOrEmpty(file))
                trace.Append('\t').Append(file).Append(':').Append(frame.GetFileLineNumber()).Append('\n');
        }
    }

    // `go.sync_test_package.onceFuncPanic` -> `sync_test.onceFuncPanic`;
    // `go.runtime.debug_package.Stack`     -> `runtime/debug.Stack` (Go names a package by its
    //                                         import path, and the namespace mirrors it);
    // a closure's compiler-generated `<Outer>b__N` on a nested display class -> `<pkg>.Outer.funcN`,
    // Go's own spelling for a function literal. A frame that is not converted Go code (golib, the
    // BCL, the test host) keeps its .NET name — inventing a Go name for it would be a lie.
    private static string goFrameName(System.Reflection.MethodBase method)
    {
        Type? declaring = method.DeclaringType;

        if (declaring is null)
            return method.Name;

        string typeName = declaring.FullName ?? declaring.Name;

        // LastIndexOf, not IndexOf: a Go package whose own name ends in `_package` would produce
        // `go.x_package_package`, and the FIRST match would truncate the import path.
        int packageSuffix = typeName.LastIndexOf("_package", StringComparison.Ordinal);

        if (!typeName.StartsWith("go.", StringComparison.Ordinal) || packageSuffix < 0)
            return $"{typeName}.{method.Name}";

        // "go.runtime.debug_package" -> "runtime/debug"
        string importPath = typeName[3..packageSuffix].Replace('.', '/');
        string name = method.Name;

        // A lambda is emitted as `<Outer>b__N` on a `<>c__DisplayClassX_Y` nested in the package
        // class; Go renders the same function literal as `Outer.funcN`.
        if (name.Length > 0 && name[0] == '<')
        {
            int close = name.IndexOf('>');

            if (close > 1)
            {
                string outer = name[1..close];
                int lastUnderscore = name.LastIndexOf('_');
                string ordinal = lastUnderscore >= 0 && lastUnderscore + 1 < name.Length ? name[(lastUnderscore + 1)..] : "1";
                name = $"{outer}.func{ordinal}";
            }
        }

        return $"{importPath}.{name}";
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

    // Pinner.Pin pins a Go object so its address is stable for the duration of the pin. The
    // guarantee it exists to provide — "the object will not be moved or freed while pinned" —
    // is about handing addresses to non-GC-aware code; a golib pointer is a managed ж<T> box
    // the GC tracks through every move, so the contract already holds unconditionally and the
    // pin set is a no-op BY CONSTRUCTION (the same class as LockOSThread above). The auto body
    // walks the scheduler (acquirem → getg) and the span table (setPinned → findObject) —
    // machinery that does not exist here. internal/fmtsort's test init (runtime.Pinner over
    // channel addresses, issue #49431's address-ordering guard) is the demonstrated consumer.
    [GoRecv] public static void Pin(this ref Pinner Δp, any pointer)
    {
    }

    // Pinner.Unpin unpins all pinned objects of the Pinner (no-op: nothing was pinned).
    [GoRecv] public static void Unpin(this ref Pinner Δp)
    {
    }

    // ---------------- The traceback surface: Callers / Frames.Next ----------------

    // One converted-Go call site observed on the managed stack, resolved at intern time so a
    // later Frames walk needs no live StackFrame. Tokens are process-lifetime, like Go's program
    // counters, so a pc slice recorded by Callers stays resolvable by any later CallersFrames.
    private sealed class CallerFrameRecord
    {
        public string Function = string.Empty;
        public string File = string.Empty;
        public nint Line;
    }

    private static readonly object s_callerTableLock = new();
    private static readonly Dictionary<string, nuint> s_callerTokens = new();
    private static readonly List<CallerFrameRecord> s_callerRecords = new();

    // Callers fills pc with the return PCs of function invocations on the calling goroutine's
    // stack, skipping `skip` frames (0 identifies the frame for Callers itself, 1 its caller).
    // The auto body enters the raw-metal unwinder on its first step (callers → getcallersp, an
    // assembly stub); the CLR answers the API CONTRACT natively — walk the managed stack and
    // project it to GO-LOGICAL frames. What counts as a frame is what Go's unwinder reports:
    // functions the GO SOURCE declares. Converted declarations count — free functions and
    // [GoRecv] receivers on the package class, methods on the structs nested in it, and function
    // literals (compiler display classes nested in the same scope). go2cs dispatch machinery does
    // not: an interface adapter shell or a generated forwarder has no Go frame, exactly as Go's
    // interface dispatch adds none. Depth DELTAS between two Callers calls on one goroutine
    // therefore match Go's logical model — the property io's flatten tests assert
    // (readDepth == myDepth+2) — while ABSOLUTE depth reflects the managed host (see the header).
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static nint Callers(nint skip, slice<uintptr> pc)
    {
        // Go picks off a 0-length pc before touching the walker (a nil pc array is the
        // print-a-traceback signal there); preserve the observable short-circuit.
        if (len(pc) == 0)
            return 0;

        StackTrace stack = new(skipFrames: 0, fNeedFileInfo: true);
        nint remainingSkip = skip;
        nint count = 0;

        foreach (StackFrame frame in stack.GetFrames())
        {
            System.Reflection.MethodBase? method = frame.GetMethod();

            if (method is null || !isGoSourceFrame(method))
                continue;

            if (remainingSkip > 0)
            {
                remainingSkip--;
                continue;
            }

            if (count >= len(pc))
                break;

            pc[count] = internCallerFrame(method, frame);
            count++;
        }

        return count;
    }

    // Frames.Next expands the next recorded PC into a Frame. The auto body resolves PCs through
    // findfunc's linker-built funcInfo tables, which have no managed form; the records minted by
    // Callers carry the same answers (Function in Go's spelling, File, Line). A PC this runtime
    // never minted resolves like Go's !funcInfo.valid() — skipped, not fatal. Frame.Func stays
    // nil (allowed by contract: "may be nil for non-Go code"), and Entry mirrors PC — entry
    // points are not distinct from call sites in the token model.
    [GoRecv] public static (Frame frame, bool more) Next(this ref Frames ci)
    {
        while (len(ci.callers) > 0)
        {
            uintptr pcToken = ci.callers[0];
            ci.callers = ci.callers[1..];

            CallerFrameRecord? record = callerFrameRecord(pcToken);

            if (record is null)
                continue;

            Frame frame = new()
            {
                PC = pcToken,
                Function = record.Function,
                File = record.File,
                Line = record.Line,
                Entry = pcToken
            };

            return (frame, moreCallerFrames(ci.callers));
        }

        return (default!, false);
    }

    // True when another Callers-minted PC remains — the precise "more" Go's two-frame prefetch
    // computes (a trailing foreign PC does not promise a Frame that will never come).
    private static bool moreCallerFrames(slice<uintptr> callers)
    {
        for (nint i = 0; i < len(callers); i++)
        {
            if (callerFrameRecord(callers[i]) is not null)
                return true;
        }

        return false;
    }

    // The Go-frame test (see Callers). The frame's TOP-LEVEL declaring scope must be a
    // `<pkg>_package` class in namespace `go` — covering the package class itself, the struct
    // types nested in it, and a function literal's display class — and the method must not be
    // go2cs machinery: a generated adapter (IGoAdapter, dispatch plumbing) or a go2cs-gen
    // synthesized member (RecvGenerator's ж-forwarders carry [GeneratedCode("go2cs-gen", …)]).
    // Everything outside a package class — golib, the BCL, the test-host runtime — is not Go
    // code and never counts.
    private static bool isGoSourceFrame(System.Reflection.MethodBase method)
    {
        Type? declaring = method.DeclaringType;

        if (declaring is null)
            return false;

        if (typeof(IGoAdapter).IsAssignableFrom(declaring))
            return false;

        foreach (object attribute in method.GetCustomAttributes(typeof(System.CodeDom.Compiler.GeneratedCodeAttribute), inherit: false))
        {
            if (attribute is System.CodeDom.Compiler.GeneratedCodeAttribute generated && generated.Tool == "go2cs-gen")
                return false;
        }

        Type topLevel = declaring;

        while (topLevel.DeclaringType is not null)
            topLevel = topLevel.DeclaringType;

        string? ns = topLevel.Namespace;

        if (ns is null || (ns != "go" && !ns.StartsWith("go.", StringComparison.Ordinal)))
            return false;

        return topLevel.Name.EndsWith("_package", StringComparison.Ordinal);
    }

    // Interns one observed call site to its process-lifetime token. Keyed by (module version id,
    // method metadata token, IL offset) — the managed spelling of "a PC": stable for the process
    // lifetime, distinct per call site, equal on every recurrence, so pc-equality comparisons
    // behave as they do in Go. Token 0 stays invalid, matching Go's zero-pc sentinel.
    private static uintptr internCallerFrame(System.Reflection.MethodBase method, StackFrame frame)
    {
        string key = $"{method.Module.ModuleVersionId}:{method.MetadataToken}:{frame.GetILOffset()}";

        lock (s_callerTableLock)
        {
            if (s_callerTokens.TryGetValue(key, out nuint token))
                return token;

            CallerFrameRecord record = new()
            {
                Function = goFrameName(method),
                File = frame.GetFileName() ?? string.Empty,
                Line = frame.GetFileLineNumber()
            };

            s_callerRecords.Add(record);
            token = (nuint)s_callerRecords.Count; // index + 1 — 0 stays the invalid sentinel
            s_callerTokens[key] = token;
            return token;
        }
    }

    private static CallerFrameRecord? callerFrameRecord(uintptr token)
    {
        nuint value = token;

        lock (s_callerTableLock)
        {
            if (value == 0 || value > (nuint)s_callerRecords.Count)
                return null;

            return s_callerRecords[(int)(value - 1)];
        }
    }
}
