// GoschedBackoffEscalationLatencyTests.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go.golib;

namespace GolibTests;

// GOSCHED'S "1 ms" ESCALATION MUST BE 1 ms ON WINDOWS (i9, 2026-09-26, COORD ruling after 03df2f43aa).
// GoschedBackoff escalates the 64th consecutive inert yield to a 1 ms sleep (board finding 2026-08-21,
// ratified). On Windows 11, Thread.Sleep(1) runs at the PROCESS's timer resolution, and a process that
// never asked for a finer one sleeps a whole ~15.6 ms tick: measured in-process on the i9 at p50
// 15.75 ms / p99 16.49 ms, while NtQueryTimerResolution reported 1 ms machine-wide. A lone yielder is
// inert on every call, so it paid ~15.7 ms per 64 yields: 237 s per million, against ~0.1 s in Go.
// runtime/pprof's TestGoroutineSwitch (10 x 1M Gosched) overran its 30 m package deadline, and 32 later
// leaves had no verdict. The ruled remedy sleeps on a high-resolution waitable timer (the one time_impl
// and Go's own Windows runtime use): 1 ms is then ~1.1-1.5 ms.
//
// The arm drives the REAL GoschedBackoff.Yield on a fresh thread (a clean per-thread streak) for
// 64 x 200 lone yields, and bounds the MEAN COST PER ESCALATION, not the total. A loaded box turns some
// yields effective and so changes how many escalations there are, but not what each one costs. Margins:
// red ~15.7 ms per escalation, green ~1.1-1.5 ms, bound 5 ms -- over 3x clear on each side. It also
// requires at least 100 escalations, so the path it bounds actually ran (200 expected when every yield
// is inert). Windows only: linux keeps Sleep(1), which is ~1 ms there and is untouched.
[TestClass]
public class GoschedBackoffEscalationLatencyTests
{
    private const int Yields = 64 * 200;
    private const int MinimumEscalations = 100;
    private const double BoundMillisecondsPerEscalation = 5.0;

    [TestMethod]
    public void AnEscalationSleepsAboutAMillisecondNotATimerTick()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Inconclusive("the escalation's Windows timer is the subject; linux keeps Sleep(1), ~1 ms there");
            return;
        }

        long escalations = 0;
        double elapsedMs = 0;

        // A fresh thread: its [ThreadStatic] inert streak starts at zero, as a new goroutine's does.
        var worker = new Thread(() =>
        {
            long before = GoschedBackoff.Escalations;
            long started = Stopwatch.GetTimestamp();

            for (int i = 0; i < Yields; i++)
                GoschedBackoff.Yield();

            elapsedMs = (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;
            escalations = GoschedBackoff.Escalations - before;
        });

        worker.Start();
        worker.Join();

        Assert.IsTrue(escalations >= MinimumEscalations,
            $"only {escalations} escalations in {Yields} lone yields -- the escalation path barely ran, so its cost is unmeasured here");

        double perEscalation = elapsedMs / escalations;

        Assert.IsTrue(perEscalation < BoundMillisecondsPerEscalation,
            $"{escalations} escalations took {elapsedMs:F0} ms, {perEscalation:F2} ms each (bound {BoundMillisecondsPerEscalation} ms): " +
            "the 1 ms escalation is sleeping a whole Windows timer tick (~15.7 ms) instead of 1 ms");
    }
}
