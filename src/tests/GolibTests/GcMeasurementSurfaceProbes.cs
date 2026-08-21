using System;
using System.Diagnostics;
using System.Runtime;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using Δruntime = go.runtime_package;

namespace GolibTests;

/// <summary>
/// S1 of <c>docs/phase4/DESIGN-readmemstats-surface.md</c> §3.6 and §4.5 — the measurement probes the
/// ReadMemStats arc owes BEFORE its recorder lands, plus the named allocation guard §8.2 makes a
/// landing precondition.
/// </summary>
[TestClass]
public class GcMeasurementSurfaceProbes
{
    // WHY THIS FILE EXISTS.
    //
    // The design prices a pause-history recorder built on a RESURRECTING FINALIZABLE SENTINEL (§3.1's
    // recommended mechanism, §3.2's shape) and then says, in §3.6, that nothing about it is measured
    // yet and that four measurements are owed before it lands. §7 makes those measurements their own
    // stage, S1, gated on GolibTests alone and carrying ZERO production change -- deliberately, so the
    // two open questions a coordinator has to rule (⟨OQ-1⟩ activation model, ⟨OQ-2⟩ HeapReleased
    // formulation) can be ruled with numbers rather than with expectations. This file is that stage.
    //
    // Everything below therefore REPORTS. The only assertions are the ones that would make a reading
    // vacuous if they failed (an instrument that answered nothing, a mechanism that never fired) plus
    // the one real guard, ReadMemStatsPerCallAllocation, which pins a number the arc must not make
    // worse. `ProbeRecorder` is the design's §3.2 recorder transcribed as faithfully as a test-local
    // prototype can be -- it is NOT production machinery and nothing outside this file uses it; its
    // whole job is to make "the chosen mechanism's actual per-gen2 overhead" a measurable quantity
    // instead of an estimate.

    private const int Big = 32 << 20;   // Go's TestFreeOSMemory bigBytes
    private const int Slack = 16 << 20; // Go's slack on a 4 KiB-page host

    // Kept alive exactly the way Go's garbage_test.go keeps `big` alive: a package-level variable the
    // test clears, so the compiler cannot shorten the object's lifetime out from under the reading.
    private static byte[]? s_big;

    // ------------------------------------------------------------------------------------------
    // The §3.2 recorder, transcribed. Test-local prototype; no production code references it.
    // ------------------------------------------------------------------------------------------

    private sealed class ProbeRecorder
    {
        private readonly ulong[] m_pauseNs = new ulong[256];
        private readonly ulong[] m_pauseEndUnixNs = new ulong[256];
        private readonly object m_lock = new();

        private long m_baseline;
        private long m_observed;
        private long m_lastGcEndUnixNs;
        private ulong m_pauseTotalNs;
        private long m_committedHighWater;

        // Probe-only instrumentation, not part of §3.2's design.
        private long m_firings;
        private volatile bool m_disarmed;

        public long Observed => Interlocked.Read(ref m_observed);

        public long Firings => Interlocked.Read(ref m_firings);

        public ulong PauseTotalNs { get { lock (m_lock) { return m_pauseTotalNs; } } }

        public long LastGcEndUnixNs { get { lock (m_lock) { return m_lastGcEndUnixNs; } } }

        public long CommittedHighWater => Interlocked.Read(ref m_committedHighWater);

        /// <summary>Gen2 collections the CLR has completed since this recorder was armed.</summary>
        public long TrueCount => GC.CollectionCount(GC.MaxGeneration) - Interlocked.Read(ref m_baseline);

        public ulong PauseAt(long cycle) { lock (m_lock) { return m_pauseNs[cycle % 256]; } }

        public ulong PauseEndAt(long cycle) { lock (m_lock) { return m_pauseEndUnixNs[cycle % 256]; } }

        /// <summary>
        /// Arms the recorder with an UNROOTED sentinel — the live case. §3.3(a) arms this from
        /// runtime's package initializer; the probe arms it per test so a run can be measured
        /// against a disarmed control.
        /// </summary>
        public void Arm(bool rooted = false)
        {
            Interlocked.Exchange(ref m_baseline, GC.CollectionCount(GC.MaxGeneration));

            Sentinel sentinel = new(this);

            if (rooted)
                m_rootedSentinel = sentinel;   // the NEGATIVE control: strongly referenced, never finalized
        }

        // Deliberately the ONLY strong reference a sentinel can have, and only in the control case.
        // In the live case nothing here points at the sentinel -- if it did, it would never be
        // collected and the finalizer would never run, which is precisely what the control proves.
        private Sentinel? m_rootedSentinel;

        /// <summary>True when this recorder is the NEGATIVE control (its sentinel is strongly held).</summary>
        public bool ControlRooted => m_rootedSentinel is not null;

        public void Disarm() => m_disarmed = true;

        /// <summary>§3.4's drain: wait out the finalizer, then observe directly. Idempotent per collection.</summary>
        public void Drain()
        {
            GC.WaitForPendingFinalizers();
            Observe();
        }

        /// <summary>§3.2's single write path, idempotent per collection.</summary>
        public void Observe()
        {
            // Step 1 -- has the gen2 count advanced past what this recorder has seen? This is also
            // what filters the sentinel's first callbacks, which fire on ephemeral collections
            // before the sentinel is promoted.
            long count = TrueCount;

            if (count <= Interlocked.Read(ref m_observed))
                return;

            // Step 2 -- read the collection's facts. DateTime.UtcNow is the honest approximation of
            // an end time the CLR publishes no stamp for.
            GCMemoryInfo info = GC.GetGCMemoryInfo();

            ulong pause = 0;

            foreach (TimeSpan duration in info.PauseDurations)
                pause += (ulong)(duration.Ticks * 100L);

            long endUnixNs = (DateTime.UtcNow - DateTime.UnixEpoch).Ticks * 100L;
            long committed = info.TotalCommittedBytes;

            lock (m_lock)
            {
                long observed = m_observed;

                if (count <= observed)
                    return;

                // Step 3 -- Go's own ordering: write the slot, THEN advance the counter, so that
                // "the most recent pause is at PauseNs[(NumGC+255)%256]" holds by construction.
                m_pauseNs[observed % 256] = pause;
                m_pauseEndUnixNs[observed % 256] = (ulong)endUnixNs;
                m_lastGcEndUnixNs = endUnixNs;
                m_pauseTotalNs += pause;
                Interlocked.Exchange(ref m_observed, observed + 1);
            }

            if (committed > Interlocked.Read(ref m_committedHighWater))
                Interlocked.Exchange(ref m_committedHighWater, committed);
        }

        private sealed class Sentinel
        {
            private readonly ProbeRecorder m_owner;

            public Sentinel(ProbeRecorder owner) => m_owner = owner;

            ~Sentinel()
            {
                ProbeRecorder owner = m_owner;

                Interlocked.Increment(ref owner.m_firings);

                try
                {
                    owner.Observe();
                }
                catch
                {
                    // A recorder that throws in a finalizer would take the process down; a probe
                    // that measured that would be measuring its own defect.
                }

                if (!owner.m_disarmed && !Environment.HasShutdownStarted)
                    GC.ReRegisterForFinalize(this);
            }
        }
    }

    private static void ForceGen2()
    {
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
    }

    // ------------------------------------------------------------------------------------------
    // §3.6 item 3 — sentinel liveness, with the negative control the design names.
    // ------------------------------------------------------------------------------------------

    [TestMethod]
    public void SentinelLiveness()
    {
        ProbeRecorder recorder = new();
        recorder.Arm();

        // Promotion: the sentinel is born in gen0, so its first callbacks fire on ephemeral
        // collections. Count how many collections it takes before `observed` starts tracking.
        int promotionCollections = 0;

        for (int i = 0; i < 4; i++)
        {
            ForceGen2();
            GC.WaitForPendingFinalizers();

            if (recorder.Firings > 0 && promotionCollections == 0)
                promotionCollections = i + 1;
        }

        long firingsAfterPromotion = recorder.Firings;

        Console.WriteLine($"[S1 §3.6-3] first firing after {promotionCollections} forced gen2 collection(s); firings so far = {firingsAfterPromotion}");

        // The claim under test: `observed` tracks CollectionCount(MaxGeneration) over a long run,
        // lagging by at most 1.
        const int cycles = 40;
        long maxLagAfterDrain = 0, maxLagBeforeDrain = 0;

        for (int i = 0; i < cycles; i++)
        {
            ForceGen2();

            long lagBefore = recorder.TrueCount - recorder.Observed;

            if (lagBefore > maxLagBeforeDrain)
                maxLagBeforeDrain = lagBefore;

            // §3.4's mitigation, verbatim: runtime.GC() and debug.freeOSMemory() drain before returning.
            recorder.Drain();

            long lagAfter = recorder.TrueCount - recorder.Observed;

            if (lagAfter > maxLagAfterDrain)
                maxLagAfterDrain = lagAfter;
        }

        long trueCount = recorder.TrueCount, observed = recorder.Observed, firings = recorder.Firings;

        Console.WriteLine($"[S1 §3.6-3] after {cycles} drained cycles: trueCount={trueCount}, observed={observed}, firings={firings}, " +
                          $"maxLag(before drain)={maxLagBeforeDrain}, maxLag(after drain)={maxLagAfterDrain}, pauseTotalNs={recorder.PauseTotalNs:N0}, lastGcEndUnixNs={recorder.LastGcEndUnixNs}");

        // A BURST: several gen2 collections with no drain between them. §3.2 step 3 advances
        // `observed` by at most one per Observe() call, so this is where a permanent lag would show.
        long observedBeforeBurst = recorder.Observed, trueBeforeBurst = recorder.TrueCount;

        for (int i = 0; i < 8; i++)
            ForceGen2();

        long lagAtBurstEnd = recorder.TrueCount - recorder.Observed;

        recorder.Drain();

        long lagAfterBurstDrain = recorder.TrueCount - recorder.Observed;

        Console.WriteLine($"[S1 §3.6-3] burst of 8 undrained gen2 collections: true +{recorder.TrueCount - trueBeforeBurst}, " +
                          $"observed +{recorder.Observed - observedBeforeBurst}, lag at burst end={lagAtBurstEnd}, lag after ONE drain={lagAfterBurstDrain}");

        recorder.Disarm();
        Assert.IsFalse(recorder.ControlRooted, "the live case must not root its sentinel.");

        // The one assertion: the mechanism fires at all after promotion. Without it every number
        // above is a reading of nothing.
        Assert.IsTrue(firings > 0, "the resurrecting sentinel never fired -- the recorder mechanism does not work on this runtime.");
        Assert.IsTrue(observed > 0, "the recorder observed no collection despite the sentinel firing.");
    }

    [TestMethod]
    public void SentinelLivenessNegativeControl()
    {
        // The control the design asks for: a STRONGLY-REFERENCED sentinel never fires at all, which
        // is what makes the live case's firings evidence of the resurrection mechanism rather than
        // of some other wake-up.
        ProbeRecorder recorder = new();
        recorder.Arm(rooted: true);

        for (int i = 0; i < 6; i++)
        {
            ForceGen2();
            GC.WaitForPendingFinalizers();
        }

        Console.WriteLine($"[S1 §3.6-3] negative control (rooted sentinel): trueCount={recorder.TrueCount}, firings={recorder.Firings}, observed={recorder.Observed}, rooted={recorder.ControlRooted}");

        recorder.Disarm();

        Assert.AreEqual(0L, recorder.Firings, "a strongly-referenced sentinel was finalized -- the control is broken, and the live reading proves nothing.");
    }

    // ------------------------------------------------------------------------------------------
    // §3.6 item 4 — GetGCMemoryInfo fidelity. Everything §3.2 says about this API is read from its
    // documented surface and is unverified against the live runtime until this runs.
    // ------------------------------------------------------------------------------------------

    [TestMethod]
    public void GetGCMemoryInfoFidelity()
    {
        Console.WriteLine($"[S1 §3.6-4] GC config: server={GCSettings.IsServerGC}, latency={GCSettings.LatencyMode}, procs={Environment.ProcessorCount}, runtime={System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");

        // Settle, so the reading below belongs to the collection this probe forces.
        ForceGen2();
        GC.WaitForPendingFinalizers();

        TimeSpan totalBefore = GC.GetTotalPauseDuration();
        long countBefore = GC.CollectionCount(GC.MaxGeneration);

        ForceGen2();

        TimeSpan totalAfter = GC.GetTotalPauseDuration();
        long countAfter = GC.CollectionCount(GC.MaxGeneration);

        Report("Any", GC.GetGCMemoryInfo());
        Report("FullBlocking", GC.GetGCMemoryInfo(GCKind.FullBlocking));
        Report("Ephemeral", GC.GetGCMemoryInfo(GCKind.Ephemeral));
        Report("Background", GC.GetGCMemoryInfo(GCKind.Background));

        GCMemoryInfo full = GC.GetGCMemoryInfo(GCKind.FullBlocking);
        double runtimeDeltaNs = (totalAfter - totalBefore).Ticks * 100.0;
        double reportedNs = 0;

        foreach (TimeSpan duration in full.PauseDurations)
            reportedNs += duration.Ticks * 100.0;

        Console.WriteLine($"[S1 §3.6-4] one forced blocking gen2 (+{countAfter - countBefore} collection): " +
                          $"GetTotalPauseDuration delta = {runtimeDeltaNs:N0} ns; sum(FullBlocking.PauseDurations) = {reportedNs:N0} ns; " +
                          $"ratio = {(runtimeDeltaNs == 0 ? 0 : reportedNs / runtimeDeltaNs):F3}");

        // A BACKGROUND gen2: §8.3 states (unverified) that it reports TWO entries in PauseDurations
        // rather than one stop-the-world pause, so the ring's single number would be their sum.
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: false);

        Stopwatch sw = Stopwatch.StartNew();
        long backgroundIndex = GC.GetGCMemoryInfo(GCKind.Background).Index;

        while (sw.ElapsedMilliseconds < 2000 && GC.GetGCMemoryInfo(GCKind.Background).Index == backgroundIndex)
            Thread.Sleep(10);

        Report("Background (after a non-blocking gen2 request)", GC.GetGCMemoryInfo(GCKind.Background));
        Report("Any (after a non-blocking gen2 request)", GC.GetGCMemoryInfo());

        // THE OVERLOAD HAZARD. §3.2 step 2 reads "the collection's facts from GC.GetGCMemoryInfo(…)"
        // without naming an overload. The default -- GCKind.Any -- reports the LATEST collection of
        // ANY kind, so an ephemeral collection landing between the gen2 and the recorder's Observe()
        // would be written into a gen2 ring slot. This measures whether that is a real hazard on
        // this runtime or a paper one.
        ForceGen2();
        GCMemoryInfo gen2Snapshot = GC.GetGCMemoryInfo();

        GC.Collect(0, GCCollectionMode.Forced, blocking: true);

        GCMemoryInfo anyAfterEphemeral = GC.GetGCMemoryInfo();
        GCMemoryInfo fullAfterEphemeral = GC.GetGCMemoryInfo(GCKind.FullBlocking);

        Console.WriteLine($"[S1 §3.6-4] OVERLOAD HAZARD: after a forced gen2 (Any: index={gen2Snapshot.Index}, generation={gen2Snapshot.Generation}) " +
                          $"followed by a forced GEN0 collection, Any reports index={anyAfterEphemeral.Index}, generation={anyAfterEphemeral.Generation}; " +
                          $"FullBlocking still reports index={fullAfterEphemeral.Index}, generation={fullAfterEphemeral.Generation} " +
                          $"— Any {(anyAfterEphemeral.Generation == GC.MaxGeneration ? "STAYED on the gen2" : "MOVED OFF the gen2")}");

        // The one assertion: the API answered for gen2 at all.
        Assert.AreEqual(GC.MaxGeneration, full.Generation, "GCKind.FullBlocking did not answer for a gen2 collection.");

        static void Report(string label, GCMemoryInfo info)
        {
            string pauses = "";

            foreach (TimeSpan duration in info.PauseDurations)
                pauses += $"{duration.Ticks * 100L:N0}ns ";

            Console.WriteLine($"[S1 §3.6-4] {label}: index={info.Index}, generation={info.Generation}, concurrent={info.Concurrent}, compacted={info.Compacted}, " +
                              $"pauseDurations=[{pauses.TrimEnd()}] (n={info.PauseDurations.Length}), pauseTimePercentage={info.PauseTimePercentage:F3}, " +
                              $"totalCommitted={info.TotalCommittedBytes:N0}, heapSize={info.HeapSizeBytes:N0}, fragmented={info.FragmentedBytes:N0}");
        }
    }

    [TestMethod]
    public void SentinelLivenessUnderNaturalAllocationPressure()
    {
        // The forced back-to-back regime in SentinelLiveness is a HARNESS pattern, not a program's.
        // The number §3.4's "lag by at most one collection" is really about is this one: how many of
        // the collections the RUNTIME decides to run, at its own pace, does the sentinel see?
        ProbeRecorder recorder = new();
        recorder.Arm();

        for (int i = 0; i < 3; i++)
        {
            ForceGen2();
            GC.WaitForPendingFinalizers();
        }

        long baseTrue = recorder.TrueCount, baseObserved = recorder.Observed, baseFirings = recorder.Firings;

        System.Collections.Generic.List<byte[]> retained = new();
        Stopwatch sw = Stopwatch.StartNew();
        long allocated = 0;

        while (sw.ElapsedMilliseconds < 4000)
        {
            for (int i = 0; i < 64; i++)
            {
                byte[] block = new byte[64 * 1024];
                block[0] = 1;
                allocated += block.Length;

                if ((i & 7) == 0)
                    retained.Add(block);
            }

            if (retained.Count > 2048)
                retained.RemoveRange(0, 1024);
        }

        sw.Stop();

        long trueDelta = recorder.TrueCount - baseTrue;
        long observedDelta = recorder.Observed - baseObserved;
        long firingsDelta = recorder.Firings - baseFirings;

        recorder.Drain();

        long afterDrain = recorder.Observed - baseObserved;

        Console.WriteLine($"[S1 §3.6-3b] {sw.Elapsed.TotalSeconds:F1} s of unforced allocation pressure ({allocated:N0} B): " +
                          $"the runtime ran {trueDelta} gen2 collection(s); the sentinel fired {firingsDelta} time(s) and the recorder observed {observedDelta} " +
                          $"({(trueDelta == 0 ? 0 : observedDelta * 100.0 / trueDelta):F1}%), lag={trueDelta - observedDelta}; after one drain, observed {afterDrain} (lag={recorder.TrueCount - baseTrue - afterDrain})");

        GC.KeepAlive(retained);
        retained.Clear();
        recorder.Disarm();

        Assert.IsTrue(allocated > 0, "the pressure loop allocated nothing.");
    }

    [TestMethod]
    public void BackgroundGen2Fidelity()
    {
        // The OTHER half of §3.6 item 4 -- "which GCKind overload answers for a BACKGROUND gen2
        // collection" -- and §8.3's unverified claim that a background gen2 reports TWO entries in
        // PauseDurations rather than one stop-the-world pause. Neither can be answered by forcing a
        // collection: GC.Collect(2, …, blocking: false) does not produce one. So this induces one
        // the only way there is, with allocation pressure, and reports whatever it gets -- including
        // "none occurred", which is an honest reading and not a failure.
        Console.WriteLine($"[S1 §3.6-4b] latency={GCSettings.LatencyMode} (background GC is {(GCSettings.LatencyMode == GCLatencyMode.Batch ? "OFF" : "ON")}), server={GCSettings.IsServerGC}");

        long startIndex = GC.GetGCMemoryInfo(GCKind.Background).Index;
        Stopwatch sw = Stopwatch.StartNew();
        System.Collections.Generic.List<byte[]> retained = new();
        long allocated = 0;

        while (sw.ElapsedMilliseconds < 15_000 && GC.GetGCMemoryInfo(GCKind.Background).Index == startIndex)
        {
            // Retain a rolling window so the gen2 budget is genuinely exceeded, and churn the rest
            // so the allocation rate is high. Nothing here forces a collection.
            for (int i = 0; i < 64; i++)
            {
                byte[] block = new byte[64 * 1024];
                block[0] = 1;
                allocated += block.Length;

                if ((i & 7) == 0)
                    retained.Add(block);
            }

            if (retained.Count > 2048)
                retained.RemoveRange(0, 1024);
        }

        sw.Stop();

        GCMemoryInfo background = GC.GetGCMemoryInfo(GCKind.Background);
        bool induced = background.Index != startIndex;

        string pauses = "";

        foreach (TimeSpan duration in background.PauseDurations)
            pauses += $"{duration.Ticks * 100L:N0}ns ";

        Console.WriteLine($"[S1 §3.6-4b] after {sw.Elapsed.TotalSeconds:F1} s and {allocated:N0} B allocated: background gen2 " +
                          $"{(induced ? "INDUCED" : "NOT induced -- this reading is UNMEASURED, not negative")}");
        Console.WriteLine($"[S1 §3.6-4b] GCKind.Background: index={background.Index}, generation={background.Generation}, concurrent={background.Concurrent}, compacted={background.Compacted}, " +
                          $"pauseDurations=[{pauses.TrimEnd()}] (n={background.PauseDurations.Length}), totalCommitted={background.TotalCommittedBytes:N0}");

        if (induced)
        {
            int nonZeroPauses = 0;

            foreach (TimeSpan duration in background.PauseDurations)
                if (duration.Ticks != 0)
                    nonZeroPauses++;

            Console.WriteLine($"[S1 §3.6-4b] §8.3's claim (a background gen2 reports TWO pause entries): {nonZeroPauses} of {background.PauseDurations.Length} entries are non-zero — " +
                              $"{(nonZeroPauses == 2 ? "CONFIRMED, and the ring's single number is their sum" : "NOT confirmed on this runtime")}");
        }

        GC.KeepAlive(retained);
        retained.Clear();

        // The probe reports. A runtime that never runs a background gen2 under this pressure is a
        // fact about the runtime, so there is nothing here to assert beyond the instrument working.
        Assert.IsTrue(allocated > 0, "the induction loop allocated nothing.");
    }

    // ------------------------------------------------------------------------------------------
    // §3.6 item 1 — recorder overhead per gen2 collection, armed vs disarmed.
    // ------------------------------------------------------------------------------------------

    [TestMethod]
    public void RecorderOverheadPerGen2Collection()
    {
        const int cycles = 60;

        // Alternate armed and disarmed rounds so machine drift falls out of the comparison rather
        // than into it. Each round is its OWN recorder: arming is one-way (§3.3(a) arms once, for
        // the process lifetime), so a fresh recorder is the only honest way to measure "disarmed".
        double armedMs = 0, disarmedMs = 0, armedPauseNs = 0, disarmedPauseNs = 0;
        long armedObserved = 0;
        const int rounds = 3;

        // One warm-up of each shape: the first armed round pays the sentinel's promotion.
        _ = Churn(true, 4, out _, out _);
        _ = Churn(false, 4, out _, out _);

        for (int round = 0; round < rounds; round++)
        {
            disarmedMs += Churn(false, cycles, out double dPause, out _);
            disarmedPauseNs += dPause;

            armedMs += Churn(true, cycles, out double aPause, out long observed);
            armedPauseNs += aPause;
            armedObserved += observed;
        }

        double armedPer = armedMs / (rounds * cycles), disarmedPer = disarmedMs / (rounds * cycles);

        Console.WriteLine($"[S1 §3.6-1] {rounds} rounds x {cycles} forced gen2 collections, allocation churn between each:");
        Console.WriteLine($"[S1 §3.6-1]   disarmed: {disarmedMs:F1} ms total, {disarmedPer:F3} ms/collection, pause {disarmedPauseNs / (rounds * cycles):N0} ns/collection");
        Console.WriteLine($"[S1 §3.6-1]   armed:    {armedMs:F1} ms total, {armedPer:F3} ms/collection, pause {armedPauseNs / (rounds * cycles):N0} ns/collection, observed {armedObserved} collections");
        Console.WriteLine($"[S1 §3.6-1]   overhead: {armedPer - disarmedPer:F3} ms/collection ({(disarmedPer == 0 ? 0 : (armedPer - disarmedPer) / disarmedPer * 100):F1}% of a collection), " +
                          $"pause delta {(armedPauseNs - disarmedPauseNs) / (rounds * cycles):N0} ns/collection");

        // The OTHER number this round produces, and the one §3.4's "lag by at most one collection"
        // is answerable against: in a BACK-TO-BACK forced-collection regime with no drain, how many
        // of the collections did the sentinel actually see?
        Console.WriteLine($"[S1 §3.6-1]   observation rate under undrained back-to-back collections: {armedObserved}/{rounds * cycles} = {armedObserved * 100.0 / (rounds * cycles):F1}%");

        Assert.IsTrue(armedObserved > 0, "the armed rounds observed no collection -- the overhead figure is not a figure.");

        static double Churn(bool armed, int count, out double pauseNsPerCycle, out long observed)
        {
            ProbeRecorder? recorder = null;

            if (armed)
            {
                recorder = new ProbeRecorder();
                recorder.Arm();

                // Promote the sentinel out of the ephemeral generations before the clock starts, so
                // the measured rounds are steady-state gen2 behavior.
                for (int i = 0; i < 3; i++)
                {
                    ForceGen2();
                    GC.WaitForPendingFinalizers();
                }
            }

            GC.WaitForPendingFinalizers();

            TimeSpan pauseBefore = GC.GetTotalPauseDuration();
            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < count; i++)
            {
                // Real churn, so the collections have something to do -- an empty forced collect
                // would price the sentinel against a no-op rather than against a collection.
                object[] garbage = new object[64];

                for (int j = 0; j < garbage.Length; j++)
                    garbage[j] = new byte[1024];

                GC.KeepAlive(garbage);
                ForceGen2();
            }

            sw.Stop();

            pauseNsPerCycle = (GC.GetTotalPauseDuration() - pauseBefore).Ticks * 100.0;
            observed = recorder?.Observed ?? 0;

            recorder?.Disarm();

            // Let the disarmed sentinel actually retire before the next round begins.
            GC.WaitForPendingFinalizers();

            return sw.Elapsed.TotalMilliseconds;
        }
    }

    // ------------------------------------------------------------------------------------------
    // §3.6 item 2 / §8.2 — the named guard. ReadMemStats must stay allocation-free, because
    // net/textproto's banked TestReadMIMEHeaderAllocations brackets each header read between two
    // ReadMemStats calls and asserts under 32,768 B per iteration.
    // ------------------------------------------------------------------------------------------

    // ⚠ TIGHTENED TO ZERO BY S2/S3 (2026-08-21). This constant was 320 for exactly one stage: S1
    // measured that ReadMemStats was NOT allocation-free -- 288 B/call, because GCMemoryInfo is a
    // struct wrapping a GCMemoryInfoData CLASS that GC.GetGCMemoryInfo allocates fresh on every call
    // (§3.6 item 2's parenthetical read that surface as free and it is not) -- so the ceiling stood
    // as the instrument that would be tightened once the read path stopped allocating, with zero
    // always the stated target and §8.2's landing precondition.
    //
    // S2/S3 did that: the committed/heap-size figures now come from GcPauseRecorder's own sample,
    // taken once per gen2 collection, and the ring is copied into the caller's already-allocated
    // array<T> backing store. GC.GetGCMemoryInfo is no longer on the read path at all once the
    // recorder has observed a collection, which is why this test forces one first.
    //
    // ZERO is the number that matters, not a small one: net/textproto's banked
    // TestReadMIMEHeaderAllocations brackets each header read between two ReadMemStats calls and
    // asserts under 32,768 B per iteration, and TotalAlloc's `precise: false` lag makes the old cost
    // LUMPY rather than absent -- most windows read 0 while roughly one in thirty absorbed a whole
    // ~8 KB allocation quantum, which put 25 % of that budget on the brackets alone in the worst
    // iteration. A guard at a non-zero ceiling would let that come back.
    private const long ReadMemStatsPerCallCeilingBytes = 0;

    [TestMethod]
    public void ReadMemStatsPerCallAllocation()
    {
        ж<Δruntime.MemStats> stats = @new<Δruntime.MemStats>();

        // The recorder must have observed a gen2 collection before the measured loop, or ReadMemStats
        // is legitimately on its FALLBACK path -- a direct GC.GetGCMemoryInfo per call, which is what
        // an opted-out (GO2CS_GC_PAUSE_HISTORY=0) or not-yet-collected process gets and is exactly
        // today's pre-recorder behavior. runtime.GC() drains the recorder before returning, so one
        // call puts the surface in the steady state the guard is about. Making this explicit rather
        // than relying on whatever the other tests in this assembly left behind is the difference
        // between a guard and a coincidence.
        Δruntime.GC();

        // Warm: the first call carries JIT and static initialization that no later call repeats.
        for (int i = 0; i < 64; i++)
            Δruntime.ReadMemStats(stats);

        const int calls = 2000;

        long before = GC.GetAllocatedBytesForCurrentThread();

        for (int i = 0; i < calls; i++)
            Δruntime.ReadMemStats(stats);

        long after = GC.GetAllocatedBytesForCurrentThread();
        double perCall = (after - before) / (double)calls;

        // The §8.2 shape measured directly: what a BRACKETED window is charged for the brackets
        // themselves. net/textproto reads TotalAlloc in the first call and again in the second, so
        // any allocation ReadMemStats performs before capturing TotalAlloc in the SECOND call lands
        // inside the measured window and is charged to the code under test. 200 windows, because
        // that is how many TestReadMIMEHeaderAllocations takes.
        const int windows = 200;
        long charged = 0, maxWindow = 0, nonZeroWindows = 0;

        for (int i = 0; i < windows; i++)
        {
            Δruntime.ReadMemStats(stats);
            uint64 openTotalAlloc = stats.Value.TotalAlloc;
            Δruntime.ReadMemStats(stats);
            uint64 closeTotalAlloc = stats.Value.TotalAlloc;

            long window = (long)(closeTotalAlloc - openTotalAlloc);

            charged += window;

            if (window > maxWindow)
                maxWindow = window;

            if (window != 0)
                nonZeroWindows++;
        }

        double perWindow = charged / (double)windows;

        Console.WriteLine($"[S1 §3.6-2] ReadMemStats over {calls} calls: {after - before:N0} B total, {perCall:F1} B/call (GetAllocatedBytesForCurrentThread — exact)");
        Console.WriteLine($"[S1 §3.6-2] EMPTY bracketed windows (two back-to-back ReadMemStats, nothing between), {windows} of them: " +
                          $"{charged:N0} B charged in total, {perWindow:F1} B/window mean, {maxWindow:N0} B worst, {nonZeroWindows} window(s) non-zero");
        Console.WriteLine($"[S1 §3.6-2] net/textproto's per-iteration budget is 32,768 B, so the bracket cost is {(perWindow * 100.0 / 32768):F3}% of it on average and {(maxWindow * 100.0 / 32768):F3}% at worst");
        Console.WriteLine(perCall == 0
            ? "[S1 §3.6-2] both instruments read ZERO, which is what §8.2's landing precondition asks for: with nothing allocated per call there is no cost for TotalAlloc's `precise: false` lag to mask into an occasional ~8 KB window"
            : $"[S1 §3.6-2] the gap between the two instruments is TotalAlloc's `precise: false` lag: a {perCall:F0} B allocation does not move the process-wide counter until the thread's allocation context is exhausted, so most windows read 0 and roughly one in thirty absorbs a whole quantum");

        Assert.IsTrue(perCall <= ReadMemStatsPerCallCeilingBytes,
            $"ReadMemStats allocated {perCall:F1} B/call, above the {ReadMemStatsPerCallCeilingBytes} B ceiling. " +
            "§8.2 of DESIGN-readmemstats-surface.md makes an allocation-free read path a LANDING PRECONDITION: " +
            "net/textproto's banked TestReadMIMEHeaderAllocations measures across ReadMemStats itself. " +
            "A reading near 288 B means the read path fell back to GC.GetGCMemoryInfo — check that the " +
            "recorder is armed and has observed a collection (GO2CS_GC_PAUSE_HISTORY, GcPauseRecorder.Arm); " +
            "a KB-scale reading means something on the path allocates per call, which is the regression " +
            "this guard exists for.");
    }

    // ------------------------------------------------------------------------------------------
    // §4.5 / ⟨OQ-6⟩ — the contingency probe the ruling declined to pre-rule. TestFreeOSMemory's
    // SECOND assertion needs >= 16 MiB of a 32 MiB LOH object to come back from the OS; whether
    // TotalCommittedBytes actually falls by that much is a property of the CLR's decommit policy.
    // ------------------------------------------------------------------------------------------

    [TestMethod]
    public void FreeOSMemoryReleaseMagnitude()
    {
        long highWater = 0;

        long Sample()
        {
            long committed = GC.GetGCMemoryInfo().TotalCommittedBytes;

            if (committed > highWater)
                highWater = committed;

            return committed;
        }

        long Released(long committed) => Math.Max(0, highWater - committed);

        long monotone = 0;      // Ruling B's LITERAL formulation, accumulated across every round

        // Settle first. Other tests in this assembly leave tens of megabytes of garbage behind, and
        // a reading taken over a heap that is still shedding it is a reading of the wrong thing.
        for (int i = 0; i < 3; i++)
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
        }

        long firstCommitted = Sample();

        Console.WriteLine($"[S1 §4.5] settled baseline before any 32 MiB allocation: committed={firstCommitted:N0} B");

        // Go's TestFreeOSMemory, step for step, three times over -- once is a sample, three is a
        // reading. The harsher variants exist so that a FAILING assertion 2 can never be dismissed
        // as "the CLR was simply not asked hard enough".
        // ⚠ THE ALLOCATION MUST HAPPEN IN A FRAME THAT HAS ALREADY EXITED. GolibTests builds Debug,
        // and a Debug build reports every stack slot -- including the temporary holding
        // `new byte[Big]` on its way into s_big -- as live for the whole enclosing method. Setting
        // s_big = null in the SAME frame therefore does not make the object unreachable, no
        // collection reclaims it, and the probe reads "nothing was released" for a reason that has
        // nothing to do with the CLR's decommit policy. Measured 2026-08-21: with the allocation
        // inline, live bytes did not move across a forced blocking compacting Aggressive collect
        // (34,445,360 -> 34,445,736 B); with it behind this call, they fall by the full 32 MiB.
        // Go's own test has the shape this restores -- `big = make(...)` then `big = nil` -- because
        // the Go compiler does not extend a temporary's lifetime that way.
        long AllocateAndSettle()
        {
            s_big = new byte[Big];
            s_big[0] = 1;
            s_big[Big - 1] = 1;

            // "Make sure any in-progress GCs are complete" -- runtime.GC(), which is exactly this pair.
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);

            return Sample();
        }

        long RunGoSequence(string label, int aggressivePasses, bool lohCompact)
        {
            long liveBefore = GC.GetTotalMemory(forceFullCollection: true);
            long committedBefore = AllocateAndSettle();
            long releasedBefore = Released(committedBefore);

            // "Clear the last reference to the big allocation, making it susceptible to collection."
            s_big = null;

            // debug.FreeOSMemory() as stubs_impl.cs implements it today.
            for (int pass = 0; pass < aggressivePasses; pass++)
            {
                if (lohCompact)
                    GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;

                GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);
                GC.WaitForPendingFinalizers();
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            }

            long committedAfter = Sample();
            long releasedAfter = Released(committedAfter);
            long delta = releasedAfter - releasedBefore;

            monotone += Math.Max(0, committedBefore - committedAfter);

            // SELF-VALIDATION. The reading below is only about the CLR's decommit policy if the
            // object was actually reclaimed; if live bytes did not come back down, the probe is
            // measuring its own lifetime bug and says so instead of reporting a number.
            long liveAfter = GC.GetTotalMemory(forceFullCollection: true);
            bool reclaimed = liveAfter < liveBefore + (Big / 2);

            Console.WriteLine($"[S1 §4.5] {label}: committed {committedBefore:N0} -> {committedAfter:N0} B (fell {committedBefore - committedAfter:N0} B of the {Big:N0} B dropped), highWater={highWater:N0} B; " +
                              $"HeapReleased {releasedBefore:N0} -> {releasedAfter:N0} B, delta={delta:N0} B; " +
                              $"assertion 1 (after > before) {(delta > 0 ? "PASSES" : "FAILS")}, " +
                              $"assertion 2 (delta >= {Big - Slack:N0} B) {(delta >= Big - Slack ? "PASSES" : "FAILS")}");
            Console.WriteLine($"[S1 §4.5] {label}: control — live {liveBefore:N0} -> {liveAfter:N0} B, object {(reclaimed ? "WAS reclaimed (the reading is about decommit policy)" : "was NOT reclaimed — THE READING ABOVE IS INVALID")}");

            return reclaimed ? delta : -1;
        }

        // The control that keeps a failing assertion 2 from being misread as "the object was never
        // collected": LIVE bytes must fall by the full 32 MiB even when committed bytes do not.
        // forceFullCollection: true, deliberately -- the `false` overload answers as of the last GC
        // and lags by a collection, which is exactly the ambiguity a control must not have.
        void ReportLiveVsCommitted(string when)
        {
            long live = GC.GetTotalMemory(forceFullCollection: true);
            GCMemoryInfo info = GC.GetGCMemoryInfo();

            Console.WriteLine($"[S1 §4.5] {when}: live(GetTotalMemory, forced)={live:N0} B, " +
                              $"committed={info.TotalCommittedBytes:N0} B, heapSize={info.HeapSizeBytes:N0} B, workingSet={Environment.WorkingSet:N0} B");
        }

        AllocateAndSettle();
        ReportLiveVsCommitted("with the 32 MiB object LIVE");
        s_big = null;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        ReportLiveVsCommitted("after it is dropped and aggressively collected");

        long r0 = RunGoSequence("round 0, FreeOSMemory as implemented", 1, false);
        long r1 = RunGoSequence("round 1, FreeOSMemory as implemented", 1, false);
        long r2 = RunGoSequence("round 2, FreeOSMemory as implemented", 1, false);
        long r3 = RunGoSequence("round 3, HARSHER: 2 aggressive passes + explicit LOH CompactOnce", 2, true);

        Console.WriteLine($"[S1 §4.5/⟨OQ-6⟩] assertion 1 (after > before) — the one §4.1 buys: {(r0 > 0 && r1 > 0 && r2 > 0 ? "PASSES in every round" : "does NOT pass in every round")}");
        Console.WriteLine($"[S1 §4.5/⟨OQ-6⟩] assertion 2 (>= {Big - Slack:N0} B released) — the one §4.5 refuses to assume: " +
                          $"{(r0 >= Big - Slack && r1 >= Big - Slack && r2 >= Big - Slack ? "PASSES in every round" : "FAILS")}; " +
                          $"deltas {r0:N0} / {r1:N0} / {r2:N0} B, harsher variant {r3:N0} B");

        // ⟨OQ-2⟩'s evidence, made concrete rather than argued: the two formulations agree on the
        // release and DISAGREE the moment the heap reacquires. Ruling B's literal wording accumulates
        // max(0, previous - current) and never decreases; §4.1's high-water form is a CURRENT
        // quantity that falls when the heap grows back, which is what Go documents HeapReleased to be.
        s_big = new byte[Big];
        s_big[0] = 1;

        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);

        long committedReacquired = GC.GetGCMemoryInfo().TotalCommittedBytes;
        long highWaterForm = Math.Max(0, highWater - committedReacquired);

        GC.KeepAlive(s_big);
        s_big = null;

        Console.WriteLine($"[S1 §4.5/⟨OQ-2⟩] after REACQUIRING {Big:N0} B: committed={committedReacquired:N0} B, highWater={highWater:N0} B — " +
                          $"high-water form HeapReleased={highWaterForm:N0} B (falls, as Go's documented field does), " +
                          $"literal cumulative form={monotone:N0} B (cannot fall, and now overstates by {monotone - highWaterForm:N0} B)");

        // The probe reports; §4.5 says the arc measures this BEFORE claiming the row, and ⟨OQ-6⟩
        // rules on the number. The only assertion is that the instrument answered.
        Assert.IsTrue(firstCommitted > 0, "TotalCommittedBytes reported nothing.");
    }
}
