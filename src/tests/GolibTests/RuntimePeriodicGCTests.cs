using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using Δdebug = go.runtime.debug_package;
using Δruntime = go.runtime_package;

namespace GolibTests;

// THE PERIODIC GC (A15). Go's sysmon forces a GC when none has run for forcegcperiod (2 minutes by
// default): gcTrigger{kind: gcTriggerTime}.test() is true when GOGC is not off, a GC has run at least
// once, and more than forcegcperiod has passed since it. runtime's TestPeriodicGC sets
// *ForceGCPeriod = 0 and wants NumGC to rise by 2 within about a second of 5 ms sleeps. Every arm
// restores what it changes.
[TestClass]
public class RuntimePeriodicGCTests
{
    private static (uint numGC, uint numForcedGC) ReadCounts()
    {
        ж<Δruntime.MemStats> Ꮡms = @new<Δruntime.MemStats>();
        Δruntime.ReadMemStats(Ꮡms);
        return (Ꮡms.Value.NumGC, Ꮡms.Value.NumForcedGC);
    }

    // Polls as TestPeriodicGC does: up to 200 sleeps of 5 ms, stopping at `want` new cycles.
    private static (uint gcs, uint forced) CyclesWithForceGCPeriodZero(uint want)
    {
        int64 orig = Δruntime.GoForceGCPeriod;
        (uint numGC, uint numForcedGC) before = ReadCounts();
        (uint numGC, uint numForcedGC) after = before;

        Δruntime.GoForceGCPeriod = 0;

        try
        {
            for (int i = 0; i < 200 && after.numGC - before.numGC < want; i++)
            {
                Thread.Sleep(5);
                after = ReadCounts();
            }
        }
        finally
        {
            Δruntime.GoForceGCPeriod = orig;
        }

        return (after.numGC - before.numGC, after.numForcedGC - before.numForcedGC);
    }

    [TestMethod]
    public void AZeroForceGCPeriodRunsPeriodicCyclesThatAreNotForced()
    {
        // Go's "make sure we're not in the middle of a GC"; it is also the first GC lastgc needs.
        Δruntime.GC();

        (uint gcs, uint forced) = CyclesWithForceGCPeriodZero(want: 2);

        Assert.IsTrue(gcs >= 2, $"no periodic GC: got {gcs} GCs, want >= 2 (TestPeriodicGC's message)");
        Assert.AreEqual(0u, forced, "a periodic cycle counted as NumForcedGC; Go counts only the application's runtime.GC calls");
    }

    [TestMethod]
    public void GOGCOffStopsThePeriodicGC()
    {
        // A GUARD: gcTriggerTime's test answers false when gcPercent < 0. One spontaneous CLR gen2 in
        // the window is tolerated; a live periodic tick at forcegcperiod 0 would run several.
        nint previous = Δdebug.SetGCPercent(-1);

        try
        {
            Δruntime.GC();

            int64 orig = Δruntime.GoForceGCPeriod;
            (uint numGC, uint numForcedGC) before = ReadCounts();

            Δruntime.GoForceGCPeriod = 0;

            try
            {
                Thread.Sleep(500);
            }
            finally
            {
                Δruntime.GoForceGCPeriod = orig;
            }

            uint gcs = ReadCounts().numGC - before.numGC;

            Assert.IsTrue(gcs <= 1, $"{gcs} GCs in 500 ms with GOGC off; the periodic GC must not run");
        }
        finally
        {
            Δdebug.SetGCPercent(previous);
        }
    }
}
