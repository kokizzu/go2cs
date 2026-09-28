using Microsoft.VisualStudio.TestTools.UnitTesting;
using static go.runtime_package;

namespace GolibTests;

// runtime's `sched` is Go's zero-valued schedt. Its five time histograms (timeToRun and the four
// stop-the-world ones behind /sched/pauses/*) are embedded BY VALUE, each holding a fixed-size
// [timeHistNumBuckets*timeHistNumSubBuckets]atomic.Uint64. In C# that array is an `array<T>` field with a
// `= new(N)` initializer, which runs only when a constructor does, so sched must be built `new schedt()`.
// The hand-owned runtime2.cs kept `default(schedt)` from its freeze while the converter, and the file's own
// runtime2.cs.auto, emit `new schedt()`: every count array read length 0, a record indexed out of range,
// and /sched/pauses read 0 samples for ever.
[TestClass]
public class RuntimeSchedZeroValueTests
{
    [TestMethod]
    public void SchedsTimeHistogramsHoldGosZeroValue()
    {
        (nint timeToRun, nint stoppingGC, nint stoppingOther, nint totalGC, nint totalOther) = GoSchedHistogramLengths();
        nint want = GoTimeHistogramLength;

        string reading = $"want {want} each; timeToRun {timeToRun}, stw stopping gc {stoppingGC}, stopping other {stoppingOther}, " +
                         $"total gc {totalGC}, total other {totalOther}";

        Assert.IsTrue(want > 0, reading);
        Assert.AreEqual(want, timeToRun, reading);
        Assert.AreEqual(want, stoppingGC, reading);
        Assert.AreEqual(want, stoppingOther, reading);
        Assert.AreEqual(want, totalGC, reading);
        Assert.AreEqual(want, totalOther, reading);
    }
}
