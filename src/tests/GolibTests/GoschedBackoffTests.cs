using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using runtime = go.runtime_package;

namespace GolibTests;

[TestClass]
public class GoschedBackoffTests
{
    // The finding these guards pin closed (board 2026-08-21, "Gosched ring starvation"): converted
    // goroutines are dedicated OS threads and Gosched mapped to a bare Thread.Yield(), which Linux
    // lowers to sched_yield(2) — near-inert for CPU-bound threads under CFS. sync/atomic's
    // TestValueCompareAndSwapConcurrent is a strict token-passing ring (value k advanceable only by
    // one specific goroutine), so every one of its 100,000 handoffs paid a fair-share epoch: ≥45 min
    // on Linux against 183 s on Windows on the SAME silicon. The ratified remedy is an adaptive
    // backoff INSIDE Gosched: consecutive provably-inert yields escalate to Thread.Sleep(1), which
    // leaves the run queue so the one thread that can make progress gets the processor.
    //
    // "Inert" is decided by measurement, not by Thread.Yield's return value alone, because that
    // value LIES on Linux: measured 2026-08-21 (both hosts, 12 logical processors) — Windows idle
    // yield returns false at ~300 ns and contended returns true at ~4.1 µs; Linux returns true
    // 10000/10000 in BOTH states, idle ~431 ns vs contended ~6.5 µs. Timing separates the states
    // on both hosts with ≥7× margin around 2 µs, so the predicate is
    // `!switched || elapsed < 2 µs`.

    // A quiet single thread spinning on Gosched is the deterministic inert case on both hosts:
    // there is (almost) never another ready thread to switch to, so every yield is inert and the
    // escalation tier must engage. Before the backoff existed this loop completed in a few
    // milliseconds (6,400 × ~0.4 µs); with escalation at 64 consecutive inert yields it must pay
    // roughly 6,400/64 ≈ 100 one-millisecond sleeps. The 50 ms bar sits far above the pre-fix
    // ceiling and far below the post-fix expectation. Three attempts absorb the freak case where
    // ambient machine load makes some yields effective (an effective yield legitimately resets
    // the escalation counter).
    [TestMethod]
    public void InertGoschedSpinEscalatesToSleep()
    {
        double bestMilliseconds = 0;

        for (int attempt = 0; attempt < 3; attempt++)
        {
            Stopwatch timer = Stopwatch.StartNew();

            for (int i = 0; i < 6_400; i++)
                runtime.Gosched();

            timer.Stop();
            bestMilliseconds = Math.Max(bestMilliseconds, timer.Elapsed.TotalMilliseconds);

            if (bestMilliseconds >= 50.0)
                return;
        }

        Assert.Fail($"6,400 back-to-back Gosched calls on a quiet thread finished in {bestMilliseconds:F1} ms " +
            "on every attempt — inert yields are not escalating to sleep, which is the exact starvation " +
            "shape that made sync/atomic's CAS ring exceed 45 minutes on Linux.");
    }

    // The fast path must stay free: when yields are EFFECTIVE (real context switches, ≥4 µs on
    // both hosts), the escalation counter resets and no sleep tier is ever reached. Saturating
    // every processor with Gosched spinners makes each yield find a ready sibling, so 25,000
    // calls per worker must complete at context-switch cost (~ hundreds of ms total), never at
    // sleep cost. The bound is generous — its job is to catch an unconditional-sleep regression
    // (which would add ~390 ms per worker at minimum and scale with any tuning mistake), not to
    // benchmark the scheduler.
    [TestMethod]
    public void EffectiveGoschedYieldsStayCheap()
    {
        int workerCount = Environment.ProcessorCount * 2;
        Stopwatch timer = Stopwatch.StartNew();

        Thread[] workers = Enumerable.Range(0, workerCount).Select(_ => new Thread(() =>
        {
            for (int i = 0; i < 25_000; i++)
                runtime.Gosched();
        })
        { IsBackground = true }).ToArray();

        foreach (Thread worker in workers)
            worker.Start();

        foreach (Thread worker in workers)
            worker.Join();

        timer.Stop();

        Assert.IsTrue(timer.Elapsed.TotalSeconds < 10.0,
            $"{workerCount} contended workers × 25,000 effective Gosched calls took {timer.Elapsed.TotalSeconds:F1} s — " +
            "effective yields must never reach the sleep tier; the backoff's escalation is firing on the fast path.");
    }
}
