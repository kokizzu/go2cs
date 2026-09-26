using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using static go.runtime_package;

namespace GolibTests;

/// <summary>
/// Guards <c>runtime.saveblockevent</c> and the MANAGED bucket store under it (runtime/mprof_impl.cs;
/// increment I2 of docs/phase4/DESIGN-managed-profiling.md, ruling 8fc439415f (B)). Go's store is one
/// persistentalloc block per bucket (header, stack array, record) reached by byte offset, with a sysAlloc'd
/// hash; the managed store keeps each part in managed storage while the converted readers walk
/// bbuckets/xbuckets/mbuckets unchanged. A block event above rate 0 must land in the block profile, one
/// call site must share one bucket, and the bucket's stack must name the frame that recorded it. With the
/// rate at Go's default of 0, <c>blockevent</c> must return without reaching the recorder.
/// Red against the refusal by name that stood here before (every event above rate 0 panicked).
/// </summary>
[TestClass]
public class RuntimeBlockEventTests
{
    private static (int64 count, int64 cycles, nint records) BlockTotals()
    {
        var (n, _) = BlockProfile(default);
        var records = new BlockProfileRecord[(int)n + 16];
        var (m, ok) = BlockProfile(records.slice());
        Assert.IsTrue(ok, "BlockProfile must fit a buffer sized from its own count");

        int64 count = 0, cycles = 0;
        for (int i = 0; i < (int)m; i++)
        {
            count += records[i].Count;
            cycles += records[i].Cycles;
        }

        return (count, cycles, m);
    }

    [TestMethod]
    public void BlockEventAboveRateZeroIsRecorded()
    {
        SetBlockProfileRate(1);
        try
        {
            var before = BlockTotals();
            GoBlockEventProbe(1_000_000, 1);
            var after = BlockTotals();

            // cycles >= rate, so saveBlockEventStack takes Go's unscaled arm: count + 1, cycles + cycles.
            Assert.AreEqual(before.count + 1, after.count, "one event must add exactly one to the profile's count");
            Assert.AreEqual(before.cycles + 1_000_000, after.cycles, "the event's cycles must be recorded unscaled");
        }
        finally
        {
            SetBlockProfileRate(0);
        }
    }

    [TestMethod]
    public void EventsFromOneCallSiteShareOneBucket()
    {
        SetBlockProfileRate(1);
        try
        {
            GoBlockEventProbe(500, 1); // the site's bucket exists before the measured window opens
            var before = BlockTotals();
            GoBlockEventProbe(500, 3);
            var after = BlockTotals();

            Assert.AreEqual(before.count + 3, after.count, "three events must add three to the count");
            Assert.AreEqual(before.records, after.records, "one call site must hash to the bucket it already has");
        }
        finally
        {
            SetBlockProfileRate(0);
        }
    }

    [TestMethod]
    public void TheBucketStackNamesTheRecordingFrame()
    {
        SetBlockProfileRate(1);
        try
        {
            GoBlockEventProbe(1_000_000, 1);

            var (n, _) = BlockProfile(default);
            var records = new BlockProfileRecord[(int)n + 16];
            var (m, _) = BlockProfile(records.slice());

            bool found = false;
            for (int i = 0; i < (int)m && !found; i++)
            {
                foreach (var (_, pc) in records[i].StackRecord.Stack())
                {
                    if (FuncForPC(pc).Name() == "runtime.GoBlockEventProbe")
                    {
                        found = true;
                        break;
                    }
                }
            }

            Assert.IsTrue(found, "some recorded stack must resolve a frame to runtime.GoBlockEventProbe, the probe that called blockevent");
        }
        finally
        {
            SetBlockProfileRate(0);
        }
    }

    [TestMethod]
    public void BlockEventAtTheDefaultRateNeverReachesTheRecorder()
    {
        SetBlockProfileRate(0);

        // blocksampled(cycles, 0) is false, so blockevent returns before saveblockevent; the refusal above
        // is what would surface if it were entered.
        GoBlockEventProbe(1_000_000, 7);
    }
}
