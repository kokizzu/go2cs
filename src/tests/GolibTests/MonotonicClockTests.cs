// MonotonicClockTests.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go.golib;

namespace GolibTests;

// The clock behind runtime.nanotime1. Before it existed, PartialStubGenerator filled that partial
// with a throw and the first caller died -- runtime/pprof's StartCPUProfile through
// SetCPUProfileRate being the one that took a whole test host down with it.
//
// These assert the three properties Go's contract actually requires, and nothing it does not: the
// epoch is deliberately NOT asserted, because Go's own is arbitrary and only differences are
// observed.
[TestClass]
public class MonotonicClockTests
{
    // NEVER BACKWARDS. This is the property the naive `ticks * 1e9 / Frequency` scaling destroys:
    // that product overflows long within minutes of uptime on a 10 MHz timer and wraps NEGATIVE, so
    // a clock built the obvious way runs backwards on any machine that has been up a while. The
    // seconds/remainder split exists for this, and a regression to the naive form fails here.
    [TestMethod]
    public void MonotonicClockNeverGoesBackwards()
    {
        long previous = MonotonicClock.Nanoseconds();

        for (int i = 0; i < 10_000; i++)
        {
            long current = MonotonicClock.Nanoseconds();

            Assert.IsTrue(current >= previous,
                $"monotonic clock went backwards: {previous} then {current} (delta {current - previous} ns)");

            previous = current;
        }
    }

    // It must ADVANCE, not merely fail to retreat -- a constant would satisfy monotonicity while
    // making every duration zero, which is the shape that would let a profiler "work" and report
    // nothing.
    [TestMethod]
    public void MonotonicClockAdvancesOverRealTime()
    {
        long start = MonotonicClock.Nanoseconds();
        Stopwatch spin = Stopwatch.StartNew();

        // Busy-wait rather than sleep: this asserts the clock tracks elapsed time, and a sleep would
        // make the test's own scheduling the thing under measurement.
        while (spin.ElapsedMilliseconds < 25)
        {
        }

        long elapsed = MonotonicClock.Nanoseconds() - start;

        Assert.IsTrue(elapsed >= 20_000_000L,
            $"clock advanced only {elapsed} ns across a measured 25 ms wait");

        // Generous ceiling: this is a correctness bound against a scaling error of orders of
        // magnitude (a Frequency/1e9 mix-up), not a timing assertion about a loaded CI box.
        Assert.IsTrue(elapsed < 5_000_000_000L,
            $"clock advanced {elapsed} ns across a 25 ms wait -- the nanosecond scaling is wrong");
    }

    // The scaling must keep sub-tick resolution rather than truncating to whole seconds, which is
    // what a `seconds * 1e9` implementation that dropped the remainder term would do.
    [TestMethod]
    public void MonotonicClockHasSubSecondResolution()
    {
        long first = MonotonicClock.Nanoseconds();
        long second;

        do
        {
            second = MonotonicClock.Nanoseconds();
        }
        while (second == first);

        long delta = second - first;

        Assert.IsTrue(delta < 1_000_000_000L,
            $"smallest observed tick was {delta} ns -- the clock is quantized to seconds");
    }
}
