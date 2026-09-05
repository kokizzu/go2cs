// stubs_impl.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// Bodies for the runtime's assembly primitives that DO have an exact managed form.
//
// runtime/stubs.go declares a large family of functions implemented in `.s`; go2cs emits each as a
// bodyless `partial` and the PartialStubGenerator fills it with a throwing stub. That default is
// right for genuine raw metal (duffcopy, the retpoline thunks, the write barriers) — but a few of
// them are not raw metal at all once the g/m/p model is gone, and stubbing THOSE turns working
// code into a crash. Only those few are implemented here; every other assembly stub deliberately
// keeps throwing, so an unported path fails loudly instead of silently doing nothing.
//
// Hand-owned: there is no stubs_impl.go, so a -stdlib reconvert never regenerates this file.
//
// getg() — the current goroutine's `g` AND the `m` that is its thread (Q40's design, Q47's cut;
// docs/phase4/DESIGN-managed-getg.md §6–§8, 2026-09-04/05). Until Q47 this file recorded the
// decision to leave getg throwing "while no reachable path needs it". The Linux runtime row's bill
// (2026-09-04) measured that decision: 47 of the 378 rows behind position 57 die on this one stub —
// the allocator suite through mheap locking, GCTest, GoroutineProfile, LFStack, ReadMemStats and
// ReadMetrics, UserArena, SignalM, StringW, TraceMap, the traceback pair, the DebugCall family, the
// semaphore and RWMutex rows — and the design's reader census showed why a `g` ALONE would have made
// it worse, not better: 202 of the 280 production readers read `gp.m` FIRST, so a `g` with a nil `m`
// throws an anonymous NullReferenceException one frame later, the same loud death minus its name.
// The honest shape is the pair, because golib's executor gives every goroutine its own dedicated
// thread for its whole life: a thread-static IS goroutine identity here (golib's own `t_current`
// rests on the same fact), and an `m` that names that thread with `curg` the goroutine it runs is a
// TRUE statement about the managed scheduler — one M per goroutine, no Ps — not a modelling choice.
//
// What is populated, and from where (nothing else):
//   g.goid, g.parentGoid      the goroutine registry (Goroutine.Current.Id / .ParentId), at mint;
//                             a thread with no goroutine (a host thread that never ran Go code)
//                             mints goid 0 — the id runtime.Stack already prints for such a thread.
//   g.gopc, g.startpc         GoSyntheticPC.Of(Creator) / .Of(Entry) — Q27's synthetic PC space.
//   g.atomicstatus            _Grunning — true of the caller by construction.
//   g.waitreason              waitReasonZero.
//   g.labels                  golib's profile-label mirror (Goroutine.GetProfileLabels), refreshed on
//                             EVERY call — it is the one H field programs mutate; the mirror stays the
//                             source of truth (runtime/pprof/proflabel_impl.cs), the g reads from it.
//   g.m / m.curg              each other, at mint.
//   everything else           its zero value, in three classes the design names: stack bounds and
//                             scheduling context (`stack`, `stackguard*`, `sched`, `syscall*`,
//                             `stktopsp` — the REPLACED representation, honestly absent); P, g0 and
//                             gsignal linkage (`m.p`, `m.g0`, `m.gsignal` — absent by construction:
//                             there are no Ps and no system stack); counters and bookkeeping
//                             (`locks`, `printlock`, `mallocing`, `throwing`, `dying`, `preemptoff`,
//                             `lockedExt/Int`, `libcall*`, `profilehz` — honest by persistence: the
//                             converted code that increments them is the code that reads them).
//
// What this buys and what it does not, as measured (the increment's own row re-read is the record):
//   - every reader that needed only the pair proceeds — acquirem/releasem, the m.locks and
//     mallocing bookkeeping, semacquire's `gp == gp.m.curg` assertion, LockOSCounts;
//   - a reader that dereferences the P (`gp.m.p.ptr().…`, 37 sites on the reachable set) dies one
//     frame later on the nil linkage — the design's stated falsifier class, the scheduler's replaced
//     representation beginning at the P, not at the g;
//   - the g0 assertions fire as Go's OWN throws: inside systemstack(fn) the managed fn runs on the
//     caller's goroutine, so `gp == gp.m.g0` is false and the reader `throw`s its own message.
//   No field is fabricated to make a row pass; a row that passes on this floor is a finding about
//   the floor, stated as such.
//
// Cost: one g box and one m box per goroutine that ever calls getg (lazily; sizes measured by the
// guard, RuntimeGetgTests), one thread-static read plus one AsyncLocal read per call. Zero for the
// banked roster by construction: a reached getg was a foreign exception no recover() adopts, so a
// banked row that reached it would have been red, and the roster is green.
//
// NOT here: any other goroutine's g (allgs, forEachG, sudog.g, m.curg for a goroutine that is not
// the caller stay unpopulated — tracebackothers and the profile's per-goroutine walk remain the
// registry's); no P, g0 or gsignal; no stack bounds; no scheduler state. See the design's §11.

using System.Runtime.CompilerServices;
using System.Threading;
using go.golib;
using go.@internal.runtime;   // atomic.Uint32's Store is an extension method of atomic_package over ж<Uint32>
using @unsafe = go.unsafe_package;

[module: go.GoManualConversion]

namespace go;

partial class runtime_package
{
    // systemstack runs fn on the system stack. Go's own contract already says that when the caller
    // is ALREADY on a system stack (g0 or gsignal), systemstack "calls fn directly and returns" —
    // and in the managed model there is exactly one stack per goroutine and no g0 to switch to, so
    // that branch is the only branch. This is a faithful implementation, not an approximation.
    internal static partial void systemstack(Action fn) => fn();

    // procyield spins for the given number of iterations, emitting the architecture's pause hint.
    // Thread.SpinWait is the CLR's spelling of exactly that.
    internal static partial void procyield(uint32 cycles) => Thread.SpinWait((int)cycles);

    // ---- getg: the calling goroutine's g and its m, minted once per thread ----

    // A goroutine is a dedicated thread for its whole life (golib's executor), so the thread IS the
    // goroutine and a thread-static is the exact cache: the same fact golib's own Goroutine.Current
    // (`t_current`) rests on.
    [ThreadStatic]
    private static ж<g>? t_getg;

    internal static partial ж<g> getg()
    {
        ж<g>? gp = t_getg;

        if (gp is null)
        {
            gp = mintGoroutineDescriptor();
            t_getg = gp;
        }

        // The one H field programs mutate: refreshed from the mirror on every call, never cached,
        // so a runtime_setProfLabel between two reads is visible to the second (Go: `getg().labels`).
        gp.Value.labels = Goroutine.GetProfileLabels() as @unsafe.Pointer;

        return gp;
    }

    private static ж<g> mintGoroutineDescriptor()
    {
        Goroutine? current = Goroutine.Current;

        ж<g> gp = Ꮡ(new g());
        ж<m> mp = Ꮡ(new m());

        ref g gv = ref gp.Value;
        ref m mv = ref mp.Value;

        gv.goid = current is null ? 0UL : unchecked((ulong)current.Id);
        gv.parentGoid = current is null ? 0UL : unchecked((ulong)current.ParentId);
        gv.gopc = current?.Creator is { } creator ? GoSyntheticPC.Of(creator) : (nuint)0;
        gv.startpc = current?.Entry is { } entry ? GoSyntheticPC.Of(entry) : (nuint)0;
        gp.of(g.Ꮡatomicstatus).Store((uint32)_Grunning);   // the corpus's own spelling: atomic.Uint32's methods take the field-ref box
        gv.waitreason = waitReasonZero;

        gv.m = mp;
        mv.curg = gp;

        return gp;
    }

    // ---- the guard's view (RuntimeGetgTests): runtime keeps its internals, so the arms read through
    //      Go-prefixed public helpers, never a g they cannot name ----

    /// <summary>What <c>getg()</c> answers on the calling thread, read twice so the cache is observable.</summary>
    public readonly record struct GoGetgView(ulong Goid, ulong ParentGoid, nuint Gopc, nuint Startpc, uint Status, bool HasM, bool MCurgIsSelf, object? Labels, bool SecondCallIsSameG);

    public static GoGetgView GoGetgSnapshot()
    {
        ж<g> first = getg();
        ж<g> second = getg();
        ref g gv = ref first.Value;
        bool hasM = gv.m is not null && !gv.m.IsNilPointer;

        return new GoGetgView(
            gv.goid,
            gv.parentGoid,
            gv.gopc.Value,
            gv.startpc.Value,
            readgstatus(first),
            hasM,
            hasM && ReferenceEquals(gv.m.Value.curg, first),
            gv.labels,
            ReferenceEquals(first, second));
    }

    /// <summary>The unmanaged sizes of the two descriptors this thread's <c>getg()</c> mints (the design's provisional figures were 0.5 KB and 1.5–2.5 KB).</summary>
    public static (int GBytes, int MBytes) GoGetgDescriptorSizes()
    {
        return (Unsafe.SizeOf<g>(), Unsafe.SizeOf<m>());
    }

    /// <summary>Whether this thread has minted its descriptor yet — lets a guard measure the FIRST call's allocation on a fresh goroutine.</summary>
    public static bool GoGetgIsMinted()
    {
        return t_getg is not null;
    }

    // NOT implemented here, on purpose:
    //   mcall(fn)     — parks the current goroutine and runs fn on the system stack, never
    //                   returning to the caller. Its only callers are the scheduler's own
    //                   continuations (gosched_m, park_m, goexit0, exitsyscall0), and there is no
    //                   managed answer at THIS layer: the managed runtime has no g to park and no
    //                   run queue to hand it to. The public entry points that reach it are
    //                   reimplemented one level up instead (managed_impl.cs).
    //   getcallerpc / getcallersp / getclosureptr / getfp — read the caller's machine registers;
    //                   the managed equivalent (a StackTrace walk) answers a different question
    //                   and would make Go's PC arithmetic silently wrong.
}
