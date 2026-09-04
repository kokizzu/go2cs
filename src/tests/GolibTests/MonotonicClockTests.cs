// MonotonicClockTests.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;
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

    // ---- MonotonicClock.Ticks: the clock behind runtime.cputicks ----

    // The stated rate must BE the rate, not a nearby constant. Everything below compares a derived
    // number against this, so if this is wrong the other two are vacuous.
    [TestMethod]
    public void TicksPerSecondIsTheStopwatchFrequency()
    {
        Assert.AreEqual(Stopwatch.Frequency, MonotonicClock.TicksPerSecond,
            "TicksPerSecond must be the frequency of the source Ticks() actually reads");
    }

    // THE LOAD-BEARING ONE, and it is not a restatement of the two below it.
    //
    // Go does not declare the tick rate, it DERIVES it: runtime.ticksPerSecond computes
    // (nowTicks - startTicks) * 1e9 / (nowTime - startTime) from a pair runtime.ticks.init writes
    // down. ticks.init is called from schedinit, which this corpus never reaches, so startTicks and
    // startTime are both ZERO and the expression collapses to Ticks() * 1e9 / Nanoseconds(). This
    // test is that expression, and it comes out at TicksPerSecond only because both readings share
    // one Stopwatch origin.
    //
    // Two clocks of the same RATE but different EPOCHS would pass MonotonicClockAdvancesOverRealTime
    // and TicksAdvanceAtTheSameRateAsNanoseconds below and fail here -- and every duration pprof
    // converts through the derived rate would be wrong by the ratio of the epochs, silently. That is
    // the regression this exists to catch.
    //
    // Computed in double for the reason Go's own comment gives at the same expression: "Perform the
    // calculation with floats. We don't want to risk overflow."
    [TestMethod]
    public void TicksAndNanosecondsShareAnEpochSoTheDerivedRateIsTheRealRate()
    {
        long ticks = MonotonicClock.Ticks();
        long nanoseconds = MonotonicClock.Nanoseconds();

        Assert.IsTrue(nanoseconds > 0,
            $"cannot derive a rate from a {nanoseconds} ns reading");

        double derived = (double)ticks * 1e9 / nanoseconds;
        double expected = MonotonicClock.TicksPerSecond;
        double error = Math.Abs(derived - expected) / expected;

        // 0.1% is generous for the microseconds between the two readings and stingy against the
        // failure this guards: an epoch mismatch is orders of magnitude, not tenths of a percent.
        Assert.IsTrue(error < 0.001,
            $"ticksPerSecond would derive {derived:N0} where the real rate is {expected:N0} " +
            $"({error:P4} error) -- Ticks() and Nanoseconds() are not reading one clock");
    }

    // The rate agreement, separately from the epoch. This is the weaker property -- any two clocks
    // of the same frequency satisfy it -- but it is the one that breaks if Ticks() is ever given a
    // scaling of its own, and it fails in a way that names scaling rather than origin.
    [TestMethod]
    public void TicksAdvanceAtTheSameRateAsNanoseconds()
    {
        long startTicks = MonotonicClock.Ticks();
        long startNanoseconds = MonotonicClock.Nanoseconds();
        Stopwatch spin = Stopwatch.StartNew();

        while (spin.ElapsedMilliseconds < 25)
        {
        }

        long elapsedTicks = MonotonicClock.Ticks() - startTicks;
        long elapsedNanoseconds = MonotonicClock.Nanoseconds() - startNanoseconds;

        Assert.IsTrue(elapsedNanoseconds > 0,
            $"nanosecond clock advanced {elapsedNanoseconds} ns across a measured 25 ms wait");

        double derived = (double)elapsedTicks * 1e9 / elapsedNanoseconds;
        double expected = MonotonicClock.TicksPerSecond;

        Assert.IsTrue(Math.Abs(derived - expected) / expected < 0.05,
            $"ticks advanced at {derived:N0}/s where the stated rate is {expected:N0}/s");
    }

    // Go's comment on cputicks warns it "is not guaranteed to be monotonic". Ours is, which is a
    // stronger guarantee than the contract asks for -- asserted so that a future implementation
    // cannot quietly weaken it, since sema.cs subtracts two readings and a backwards step there
    // produces a negative duration in a block profile.
    [TestMethod]
    public void TicksNeverGoBackwards()
    {
        long previous = MonotonicClock.Ticks();

        for (int i = 0; i < 10_000; i++)
        {
            long current = MonotonicClock.Ticks();

            Assert.IsTrue(current >= previous,
                $"tick clock went backwards: {previous} then {current} (delta {current - previous})");

            previous = current;
        }
    }
}
