using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using go.golib;
using Δdebug = go.runtime.debug_package;
using Δruntime = go.runtime_package;
using Δtime = go.time_package;

namespace GolibTests;

/// <summary>
/// S2/S3 of <c>docs/phase4/DESIGN-readmemstats-surface.md</c> — the guards for the landed pause
/// recorder, <c>HeapReleased</c> and <c>NumForcedGC</c>.
/// </summary>
[TestClass]
public class GcPauseHistorySurfaceTests
{
    // WHY THIS FILE EXISTS, and how it differs from GcMeasurementSurfaceProbes.
    //
    // That file is S1: measurement probes over a test-local PROTOTYPE of the recorder, which report
    // numbers so ⟨OQ-1⟩/⟨OQ-2⟩ could be ruled with evidence. This file is the guard over the
    // PRODUCTION recorder (golib's GcPauseRecorder, behind runtime.ReadMemStats and
    // runtime/debug.readGCStats). The consuming suite is Go's own runtime/debug TestReadGCStats and
    // TestFreeOSMemory, which run only through the -tests pipeline; these cases hold the same
    // properties from a clone, under the standing GolibTests gate, with no pipeline.
    //
    // §3.5 restates TestReadGCStats' nine assertions as an acceptance list, and the design's central
    // claim is that they close on MECHANISM rather than on tuning: both surfaces read ONE snapshot
    // taken under ONE lock, so there is no second source for them to disagree with. That is what is
    // asserted here.

    /// <summary>
    /// §3.5, assertions 4/8/9 and the ring convention — held from a SINGLE <c>ReadGCStats</c> call,
    /// so no GC can land between two reads and split them.
    /// </summary>
    [TestMethod]
    public void ReadGCStatsIsSelfConsistent()
    {
        // runtime.GC() drains the recorder before returning (§3.4), so NumGC is current here.
        Δruntime.GC();

        ж<Δdebug.GCStats> Ꮡstats = @new<Δdebug.GCStats>();
        Ꮡstats.Value.PauseQuantiles = new slice<Δtime.Duration>(10);

        Δdebug.ReadGCStats(Ꮡstats);

        ref Δdebug.GCStats stats = ref Ꮡstats.Value;

        long numGC = stats.NumGC;
        nint pauses = len(stats.Pause);
        nint ends = len(stats.PauseEnd);
        long expected = Math.Min(numGC, GcPauseRecorder.RingLength);

        Console.WriteLine($"[S2 §3.5] ReadGCStats: NumGC={numGC}, len(Pause)={pauses}, len(PauseEnd)={ends}, " +
                          $"PauseTotal={(int64)stats.PauseTotal:N0} ns, LastGC={stats.LastGC.UnixNano():N0}");

        Assert.IsTrue(numGC > 0,
            "no gen2 collection was observed even after runtime.GC(), which drains the recorder before returning. " +
            "Either the recorder never armed (GO2CS_GC_PAUSE_HISTORY) or the resurrecting sentinel is not firing.");

        // Assertion 4 and assertion 8 of §1.2, the two that fail without a recorder.
        Assert.AreEqual((nint)expected, pauses, "len(stats.Pause) must be min(NumGC, 256).");
        Assert.AreEqual((nint)expected, ends, "len(stats.PauseEnd) must be min(NumGC, 256).");

        // The ring is delivered MOST RECENT FIRST, so entry 0's end time IS the last GC. This is the
        // write ordering (slot, then counter) and the backwards walk agreeing by construction — the
        // property that makes assertions 5 and 9 hold rather than merely happen to.
        Assert.AreEqual(stats.LastGC.UnixNano(), stats.PauseEnd[0].UnixNano(),
            "the most recent PauseEnd entry must be LastGC — the ring's ordering and LastGC come from one snapshot.");

        // Assertion 7: the quantiles ReadGCStats computes for itself are monotone.
        for (nint i = 0; i < len(stats.PauseQuantiles) - 1; i++)
        {
            Assert.IsTrue((int64)stats.PauseQuantiles[i] <= (int64)stats.PauseQuantiles[i + 1],
                $"PauseQuantiles[{i}] > PauseQuantiles[{i + 1}] — the quantile fill is not sorted.");
        }
    }

    /// <summary>
    /// §3.5, assertions 1/2/3/5/9 — the CROSS-surface half, which is what
    /// <c>TestReadGCStats</c> actually checks.
    /// </summary>
    [TestMethod]
    public void ReadGCStatsAgreesWithReadMemStats()
    {
        // §8.3 names one genuine race and does not claim it away: Go's test earns "no GC during
        // ReadGCStats" with `defer SetGCPercent(SetGCPercent(-1))`, and in this tree setGCPercent is
        // a remembered value with no effect on collection, so a gen2 collection landing between the
        // two calls would move `observed` and split the two reads. The window is microseconds and is
        // entered right after runtime.GC() has forced two full compacting collections, so the heap is
        // as far from the next gen2 trigger as it ever gets — but the probability is NOT zero, and a
        // guard must not be flaky. Retrying a split read is honest precisely because the split is a
        // named, understood race rather than an inconsistency: a torn or half-updated read is
        // impossible (one lock, fixed storage), so only a real collection can cause one, and a real
        // collection makes both surfaces move TOGETHER on the next attempt.
        const int attempts = 8;

        for (int attempt = 1; attempt <= attempts; attempt++)
        {
            Δruntime.GC();

            ж<Δdebug.GCStats> Ꮡstats = @new<Δdebug.GCStats>();
            ж<Δruntime.MemStats> Ꮡmstats = @new<Δruntime.MemStats>();

            Δdebug.ReadGCStats(Ꮡstats);
            Δruntime.ReadMemStats(Ꮡmstats);

            ref Δdebug.GCStats stats = ref Ꮡstats.Value;
            ref Δruntime.MemStats mstats = ref Ꮡmstats.Value;

            bool last = attempt == attempts;

            if (stats.NumGC != (long)mstats.NumGC || (ulong)(int64)stats.PauseTotal != mstats.PauseTotalNs)
            {
                Console.WriteLine($"[S2 §8.3] attempt {attempt}: a collection split the two reads " +
                                  $"(NumGC {stats.NumGC} vs {mstats.NumGC}, PauseTotal {(int64)stats.PauseTotal} vs {mstats.PauseTotalNs}) — retrying");

                if (!last)
                    continue;
            }

            // Assertions 1, 2 and 3: one source, so all three are identities.
            Assert.AreEqual(stats.NumGC, (long)mstats.NumGC, "stats.NumGC != mstats.NumGC");
            Assert.AreEqual((ulong)(int64)stats.PauseTotal, mstats.PauseTotalNs, "stats.PauseTotal != mstats.PauseTotalNs");
            Assert.AreEqual(stats.LastGC.UnixNano(), (long)mstats.LastGC, "stats.LastGC.UnixNano() != mstats.LastGC");

            // Assertions 5 and 9: ReadGCStats' backwards walk over MemStats' ring, verbatim from
            // Go's own test — off := (NumGC + 255) % 256, decrementing.
            nint n = len(stats.Pause);
            nint off = (nint)((mstats.NumGC + GcPauseRecorder.RingLength - 1) % GcPauseRecorder.RingLength);

            for (nint i = 0; i < n; i++)
            {
                Assert.AreEqual(mstats.PauseNs[off], (ulong)(int64)stats.Pause[i],
                    $"stats.Pause[{i}] does not match mstats.PauseNs[{off}] — the two surfaces disagree about the ring.");
                Assert.AreEqual(mstats.PauseEnd[off], (ulong)stats.PauseEnd[i].UnixNano(),
                    $"stats.PauseEnd[{i}] does not match mstats.PauseEnd[{off}].");

                off = (off + GcPauseRecorder.RingLength - 1) % GcPauseRecorder.RingLength;
            }

            Console.WriteLine($"[S2 §3.5] the two surfaces agree at attempt {attempt}: NumGC={stats.NumGC}, " +
                              $"ring entries={n}, PauseTotal={(int64)stats.PauseTotal:N0} ns, LastGC={mstats.LastGC:N0}");
            return;
        }
    }

    /// <summary>
    /// §4.1 / ⟨OQ-2⟩ — <c>HeapReleased</c> is <c>max(0, committedHighWater - currentCommitted)</c>,
    /// which is what Go's <c>TestFreeOSMemory</c> reads. Both of its assertions, in its own shape.
    /// </summary>
    [TestMethod]
    public void FreeOSMemoryMovesHeapReleased()
    {
        const int bigBytes = 32 << 20;
        const int slack = bigBytes / 2;   // Go's own slack on a 4 KiB-page host

        // ⚠ THE ALLOCATION MUST HAPPEN IN A FRAME THAT HAS ALREADY EXITED (§7.1.10). GolibTests
        // builds Debug, and a Debug build can report a stack slot holding the new object on its way
        // into the field as live for the whole enclosing method — so clearing the field in the SAME
        // frame does not make the object unreachable, nothing reclaims it, and the probe measures its
        // own lifetime bug while looking exactly like a CLR decommit-policy finding. Go's own test has
        // the shape this restores (`big = make(...)` then `big = nil`) because the Go compiler does
        // not extend a temporary's lifetime that way.
        Allocate(bigBytes);

        Δruntime.GC();

        ж<Δruntime.MemStats> Ꮡbefore = @new<Δruntime.MemStats>();
        Δruntime.ReadMemStats(Ꮡbefore);

        long liveBefore = GC.GetTotalMemory(forceFullCollection: true);

        s_big = null;

        Δdebug.FreeOSMemory();

        ж<Δruntime.MemStats> Ꮡafter = @new<Δruntime.MemStats>();
        Δruntime.ReadMemStats(Ꮡafter);

        ulong before = Ꮡbefore.Value.HeapReleased;
        ulong after = Ꮡafter.Value.HeapReleased;
        long liveAfter = GC.GetTotalMemory(forceFullCollection: true);

        Console.WriteLine($"[S3 §4.1] HeapReleased {before:N0} -> {after:N0} B (delta {(long)after - (long)before:N0} B); " +
                          $"Sys {Ꮡbefore.Value.Sys:N0} -> {Ꮡafter.Value.Sys:N0} B; live {liveBefore:N0} -> {liveAfter:N0} B");

        // The control §7.1.10 asks every managed GC probe to carry: if live bytes did not come back
        // down, the reading above is about this test's own object lifetime, not about the field.
        Assert.IsTrue(liveAfter < liveBefore + (bigBytes / 2),
            $"the {bigBytes:N0} B object was not reclaimed (live {liveBefore:N0} -> {liveAfter:N0} B) — " +
            "THE HeapReleased READING IS INVALID, and this is a probe-lifetime defect rather than a runtime one.");

        // TestFreeOSMemory's two assertions.
        Assert.IsTrue(after > before, $"no memory released: {before} -> {after}");
        Assert.IsTrue(after - before >= (ulong)(bigBytes - slack),
            $"less than {bigBytes - slack:N0} B released: {before} -> {after}");
    }

    /// <summary>§4.2 — <c>NumForcedGC</c> counts the cycles the PROGRAM asked for.</summary>
    [TestMethod]
    public void NumForcedGCCountsForcedCycles()
    {
        ж<Δruntime.MemStats> Ꮡbefore = @new<Δruntime.MemStats>();
        Δruntime.ReadMemStats(Ꮡbefore);

        Δruntime.GC();
        Δruntime.GC();
        Δdebug.FreeOSMemory();

        ж<Δruntime.MemStats> Ꮡafter = @new<Δruntime.MemStats>();
        Δruntime.ReadMemStats(Ꮡafter);

        uint delta = Ꮡafter.Value.NumForcedGC - Ꮡbefore.Value.NumForcedGC;

        Console.WriteLine($"[S3 §4.2] NumForcedGC {Ꮡbefore.Value.NumForcedGC} -> {Ꮡafter.Value.NumForcedGC} (delta {delta}) " +
                          $"across two runtime.GC() calls and one debug.FreeOSMemory()");

        Assert.AreEqual(3u, delta,
            "NumForcedGC must count exactly the cycles the application forced — Go's field is " +
            "\"GC cycles that were forced by the application calling the GC function\", a fact about " +
            "the program rather than about the collector.");
    }

    /// <summary>
    /// §4.3's rule, asserted rather than only documented: a field is answered only when a managed
    /// measurement means the same thing the Go field means.
    /// </summary>
    [TestMethod]
    public void RefusedFieldsStayZero()
    {
        Δruntime.GC();

        ж<Δruntime.MemStats> Ꮡm = @new<Δruntime.MemStats>();
        Δruntime.ReadMemStats(Ꮡm);

        ref Δruntime.MemStats m = ref Ꮡm.Value;

        Console.WriteLine($"[S3 §4.3] Alloc={m.Alloc:N0}, Sys={m.Sys:N0}, NextGC={m.NextGC:N0}, NumGC={m.NumGC}, " +
                          $"PauseTotalNs={m.PauseTotalNs:N0}, HeapReleased={m.HeapReleased:N0}, Mallocs={m.Mallocs}, " +
                          $"HeapObjects={m.HeapObjects}, GCCPUFraction={m.GCCPUFraction}");

        // r56d established BY MEASUREMENT that the CLR publishes no in-process object count at all.
        // The adjacent quantity is bytes; bytes are not counts.
        Assert.AreEqual(0UL, m.Mallocs, "Mallocs must stay zero — the CLR publishes no object count.");
        Assert.AreEqual(0UL, m.Frees, "Frees must stay zero.");
        Assert.AreEqual(0UL, m.HeapObjects, "HeapObjects = Mallocs - Frees must stay consistent at zero.");
        Assert.AreEqual(0UL, m.Lookups, "Lookups must stay zero.");

        // The adjacent quantity is GCMemoryInfo.PauseTimePercentage — pause time as a share of wall
        // time since the last GC — where Go's field is GC's share of this program's available CPU
        // time since it started. Different numerator, different denominator, different window.
        Assert.AreEqual(0d, m.GCCPUFraction, "GCCPUFraction must stay zero — no managed measurement means what it means.");

        // Go allocator arenas the CLR has no corresponding partition to report.
        Assert.AreEqual(0UL, m.StackSys, "StackSys must stay zero.");
        Assert.AreEqual(0UL, m.MSpanSys, "MSpanSys must stay zero.");
        Assert.AreEqual(0UL, m.MCacheSys, "MCacheSys must stay zero.");
        Assert.AreEqual(0UL, m.BuckHashSys, "BuckHashSys must stay zero.");
        Assert.AreEqual(0UL, m.GCSys, "GCSys must stay zero.");
        Assert.AreEqual(0UL, m.OtherSys, "OtherSys must stay zero.");
        Assert.IsFalse(m.DebugGC, "DebugGC must stay false — Go's own field is unused.");

        // And the fields this arc DID make real, so a regression that quietly zeroes them again is
        // caught here rather than in a pipeline run nobody has scheduled.
        Assert.IsTrue(m.NumGC > 0, "NumGC must report the observed gen2 collections.");
        Assert.IsTrue(m.PauseTotalNs > 0, "PauseTotalNs must be the ring's running sum, not zero.");
        Assert.IsTrue(m.LastGC > 0, "LastGC must be the end time of the last observed collection.");
        Assert.IsTrue(m.EnableGC, "EnableGC is always true.");
    }

    // Kept the way Go's garbage_test.go keeps `big` alive: a package-level variable the test clears.
    private static byte[]? s_big;

    private static void Allocate(int size)
    {
        s_big = new byte[size];
        s_big[0] = 1;
        s_big[size - 1] = 1;
    }
}
