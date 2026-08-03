// time_impl.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

namespace go;

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using @unsafe = unsafe_package;

partial class time_package
{
    // time's now() and runtimeNano() are //go:linkname'd into the Go runtime (runtime.now /
    // runtime.nanotime), so the converter emitted them as bodyless partials — throwing stubs. That
    // made the package UNUSABLE at load: the static initializer `startNano = runtimeNano() - 1`
    // (time.cs) runs runtimeNano() during the type's cctor, so merely IMPORTING time — or the first
    // touch of time.Now / Since / Sub — died with "runtimeNano: external (assembly or cgo) function
    // is not implemented" (surfaced by io/fs, crypto/subtle, and go/doc/comment, whose test-package
    // static init reaches time_package..cctor). These supply the equivalent managed bodies so the
    // clock RUNS. Living in an _impl.cs companion (never emitted by the converter) makes this durable
    // by construction: a -stdlib reconvert regenerates the pristine bodyless partials and these
    // bodies remain. The same applies to Go's runtime TIMERS (Sleep / newTimer / stopTimer /
    // resetTimer), realized in the region at the bottom of this file.

    // runtimeNano returns the current value of a monotonic clock in nanoseconds. Go's
    // runtime.nanotime reads a monotonic source (QueryPerformanceCounter on Windows); Stopwatch is
    // exactly that source on .NET. The tick count is scaled to nanoseconds with the seconds/remainder
    // split so the result stays EXACT and monotonic without overflowing: `ticks * 1e9` would overflow
    // int64 for any real uptime, whereas `seconds * 1e9` (seconds = ticks/Frequency) and
    // `rem * 1e9 / Frequency` (rem < Frequency) each stay well inside int64 and together preserve full
    // sub-tick nanosecond resolution. The absolute epoch is arbitrary — only differences are observed
    // (startNano rebases it), and Now().mono / Sub / Since all read this same source, so the monotonic
    // component is coherent across the package.
    internal static partial int64 runtimeNano()
    {
        int64 ticks = Stopwatch.GetTimestamp();
        int64 freq = Stopwatch.Frequency;
        int64 seconds = ticks / freq;
        int64 rem = ticks % freq;
        return seconds * 1_000_000_000L + rem * 1_000_000_000L / freq;
    }

    // now returns the current wall-clock time as (Unix seconds, sub-second nanoseconds) plus a
    // monotonic reading. Go's runtime.now returns seconds since the Unix epoch (Now() then rebases
    // them by unixToInternal - minWall = 2682288000); DateTime.UtcNow is the managed wall clock
    // (.NET uses GetSystemTimePreciseAsFileTime on modern Windows, ~100 ns granularity). nsec is the
    // sub-second remainder converted from 100 ns Ticks to nanoseconds. mono uses the same monotonic
    // source as runtimeNano() so Now()'s monotonic component agrees with Since / Sub.
    internal static partial (int64 sec, int32 nsec, int64 mono) now()
    {
        // `DateTime` (unqualified) binds to time's own layout constant here, so the wall-clock type
        // is spelled out fully.
        int64 unixTicks = System.DateTime.UtcNow.Ticks - System.DateTime.UnixEpoch.Ticks;
        int64 sec = unixTicks / System.TimeSpan.TicksPerSecond;
        int32 nsec = ((int32)(unixTicks % System.TimeSpan.TicksPerSecond)) * 100;
        int64 mono = runtimeNano();
        return (sec, nsec, mono);
    }

    // ---------------------------------------------------------------------------------------------
    //  Runtime timers — Sleep, newTimer, stopTimer, resetTimer
    // ---------------------------------------------------------------------------------------------
    //
    // Sleep and the newTimer/stopTimer/resetTimer trio are //go:linkname'd into the runtime's timer
    // machinery (runtime/time.go), so the converter emitted them as bodyless partials — throwing
    // stubs. A stub throw on a timer path is uniquely destructive: it lands on whichever goroutine
    // touched the timer, and a goroutine that dies takes the whole host down, so every package that
    // so much as calls time.Sleep once was unreachable. These bodies realize the runtime's timer
    // contract on .NET.
    //
    // SERVICE MODEL. Go keeps timers in per-P heaps, run by whichever P first notices a deadline.
    // The managed model is ONE global heap serviced by one dedicated background thread — the shape
    // Go itself used before per-P timers (the old runtime `timerproc`). One thread suffices and is
    // ordering-faithful because the only two callbacks package time ever installs are `sendTime` (a
    // NON-BLOCKING channel send) and `goFunc` (which only starts a goroutine): neither can block the
    // service thread and delay a later deadline. Callbacks run with the lock released, in deadline
    // order.
    //
    // WHY NOT System.Threading.Timer. Its resolution is the Windows timer tick — measured at ~15 ms
    // on this host, so a 1 ms Go timer would fire ~15x late and any two timers less than a tick
    // apart could fire OUT OF ORDER. The service thread instead waits on a Windows high-resolution
    // waitable timer, precisely the object the Go runtime uses for the same job
    // (runtime/os_windows.go createHighResTimer/usleep), and it reads the same monotonic clock as
    // runtimeNano(), so timer deadlines are coherent with Now()/Since/Sub.
    //
    // LOCKING DISCIPLINE. `s_timerLock` guards the heap AND every runtimeTimer field; nothing else
    // does. Go's finer-grained scheme (a per-timer lock plus a per-P heap lock) exists to scale
    // across Ps and to order timer-vs-heap acquisition; with a single heap there is exactly one
    // lock and therefore no lock-ordering hazard at all. No user code and no timer callback ever
    // runs under it — the service thread collects the due callbacks inside the lock and invokes
    // them after releasing it — so a callback cannot deadlock against a concurrent Stop/Reset.
    // A Stop or Reset that races the firing callback cannot double-fire or lose a re-arm: both the
    // firing decision and the cancellation are `when`/`gen` transitions made under this one lock,
    // and the loser of the race is detected by the generation check (see runtimeTimer.gen).
    //
    // ⚠ DIVERGENCE — asynchronous timer channels. Go 1.23 made a chan-based Timer/Ticker channel
    // SYNCHRONOUS (#37196): package time still creates it with `make(chan Time, 1)` but hands the
    // channel to the runtime via `syncTimer`, and the runtime then couples the channel's receive
    // path to the timer so no stale value can be observed after Stop/Reset. That coupling lives
    // inside the channel implementation, which this model does not have, so the `cp` argument is
    // ignored and the emitted cap-1 buffered channel keeps its buffered behavior. The result is
    // exactly Go's own documented pre-1.23 mode, still selectable upstream as
    // GODEBUG=asynctimerchan=1: unstopped timers are not GC-recovered early, and a receive that
    // races a Stop/Reset can observe one already-delivered value. Everything else here — the
    // Stop/Reset return values, the tick-drop behavior, the period phase — matches the runtime.
    //
    // ONE FIRING PER TIMER PER PASS — why the pass reads the clock exactly once (r39, 2026-08-03).
    // The service pass samples `now` ONCE and threads that one value through the whole drain. That
    // is not an optimization: it is the invariant. Go does the same and for the same reason — the
    // scheduler samples the clock in timers.check and hands that value down through timers.run(now)
    // to timer.unlockAndRun(now), never re-reading it inside a pass — and the consequence is a
    // theorem rather than a heuristic:
    //
    //     Within one service pass, every timer fires AT MOST ONCE.
    //
    // Two facts carry it. First, AT MOST ONE HEAP ENTRY PER TIMER IS EVER LIVE: every Enqueue in
    // this file is immediately preceded by a `gen++` on the same timer and queues the POST-increment
    // value, and `gen` only ever increases — so each generation is consumed by at most one entry and
    // every other entry for that timer fails the `entry.timer.gen != entry.gen` test and is dropped.
    // (Keep that pairing intact: moving the `gen++` in the drain below into the periodic branch
    // would silently break this.) A one-shot therefore cannot be re-peeked because it is dequeued
    // and NOT re-enqueued, not because `when` is cleared — the drain reads the heap's priority, not
    // `timer.when`.
    //
    // Second, the arithmetic. A periodic timer is re-armed to next = when + period*(1 + delay/period)
    // with delay = now - when >= 0; writing delay = q*period + r for 0 <= r < period gives
    // next = when + period*(1 + q) = (when + delay) + (period - r) = now + (period - r), and
    // r < period makes that STRICTLY greater than `now`. The re-peek therefore always takes the
    // `when > now` branch and the drain ends. It holds for every period, including the 1 ns one
    // testTimerChan resets to. Overflow cannot defeat it: the arithmetic is unchecked, the true
    // value is always in [1, 2^64-2), so a wrapped result is always NEGATIVE — never a small
    // positive that could land at or below `now` — and the `next < 0` clamp catches exactly that.
    //
    // Re-reading the clock per iteration broke that theorem and was the defect this replaced (r36
    // recorded it): the advanced `when` lands one nanosecond ahead, a freshly read `now` has already
    // passed it, and the same ticker fires again — for as long as consecutive reads of the ~100 ns
    // monotonic source keep advancing. The burst is invisible while nobody is receiving (sendTime's
    // non-blocking send onto the cap-1 channel drops all but one) but testTimerChan's drainAsync IS
    // receiving, so the two stale values an async ticker is allowed became three or more and `noTick`
    // reported "extra tick" — in ALL THREE asynctimerchan modes, which the divergence above (scoped
    // to the sync mode) never explained.
    //
    // What the invariant does NOT do is rate-limit, and it cannot delay anything. `next` depends on
    // `now` only through the floor delay/period, which is non-decreasing in `now`, so hoisting the
    // read can only make `next` — and therefore the pass's `deadline` — SMALLER or equal. waitUntil
    // recomputes `remaining` from a FRESH clock, and the OS wake is monotone in the deadline, so no
    // timer can wake later than the per-iteration version would have woken it; usually the deadline
    // is already past and waitUntil returns at once. (When delay < period, the common case, the floor
    // is 0 and the deadline is bit-for-bit identical either way.) A high-rate ticker is thus served
    // every pass, as in Go, where the scheduler simply calls check again with a fresh `now`. The
    // bound is on re-firing WITHIN a pass, which is Go's "drop ticks to make up for slow receivers".
    //
    // A timer coming due DURING a pass waits for the next one, and that is not a delay either: the
    // head of the heap is the minimum `when` among live entries, so `deadline` is <= that timer's
    // `when` <= the clock at the end of the drain, hence `remaining <= 0` and the next pass starts
    // immediately. Go behaves the same way.
    //
    // Where this model is NOT Go, so the fidelity claim above is not over-read: Go's `check` releases
    // the timer-set lock around EACH callback and re-validates the head between them, so a Stop
    // landing mid-pass cancels the callbacks after it; this drain commits the whole batch under one
    // lock hold and then runs it, so a Stop landing between the two does not. Go also keeps one heap
    // entry per TIMER (repositioned in place, zombies swept), where this keeps one per ARM and
    // reclaims a dead entry only when it reaches the head. Both predate the single clock sample and
    // are narrowed, not widened, by it — a frozen `now` commits a SMALLER batch. One consequence to
    // know: `delay` is now measured from the pass's clock, so sendTime's `Now().Add(-delay)` lands
    // slightly later than a per-iteration reading would place it — the same form Go uses, with a
    // larger magnitude because callbacks here are deferred to the end of the batch.
    //
    // Finally, a CONSTRAINT this establishes rather than merely satisfies. At the instant a ticker is
    // stopped or reset, at most TWO of its ticks can exist: one in the cap-1 buffer and one committed
    // to `due` but not yet sent. That is exactly the allowance testTimerChan's drainAsync drains for
    // a ticker, so the margin is zero. Any later change that lets two ticks for ONE timer be committed
    // before their callbacks run — batching two passes, a second service thread, moving the `due`
    // flush inside the lock-held drain — re-breaks `noTick` without touching a line of this file.

    // Hidden runtime state for one Timer or Ticker. Go declares these fields on runtime.timeTimer,
    // AFTER the two fields package time can see (`C` and `initTimer`/`initTicker`); the comment in
    // sleep.go — "there are extra fields after the channel, reserved for the runtime and
    // inaccessible to users" — is describing exactly this record.
    private sealed class runtimeTimer
    {
        // Absolute deadline on the runtimeNano() clock, or 0 for NOT ARMED — stopped, or a one-shot
        // that already fired. Go marks that state the same way (`t.when = 0` in timer.stop and in
        // unlockAndRun's one-shot path), and it is the value both stopTimer and resetTimer report as
        // their "was still pending" answer.
        internal int64 when;

        // Repeat interval: 0 for a one-shot Timer, > 0 for a Ticker.
        internal int64 period;

        // The callback package time installed — `sendTime` for a chan-based Timer/Ticker, `goFunc`
        // for AfterFunc.
        internal Action<any, uintptr, int64> f = null!;

        // Callback argument — the channel<Time> for sendTime, the Action for goFunc.
        internal any? arg;

        // Arm generation, bumped on EVERY change to `when` (arm, stop, reset, ticker re-arm). Heap
        // entries carry the generation they were queued with, which is how a Stop or Reset cancels
        // an already-queued firing without removing it from the heap: the service thread drops any
        // entry whose generation no longer matches. This is the managed stand-in for Go's
        // timerModified/timerZombie bits, which likewise leave the stale heap entry in place.
        internal int64 gen;
    }

    // Timer/Ticker box → hidden runtime state. Go's runtime allocates the timeTimer and thus owns
    // the storage for both halves; here the visible half is the ж<T> box the converted code holds
    // and the hidden half is looked up by that box's REFERENCE IDENTITY — stopTimer/resetTimer are
    // always handed the very box newTimer returned. Weak-keyed so an unreferenced Timer stays
    // collectible (Go 1.23 recovers unreferenced timers); an ARMED timer's state is independently
    // kept alive by the service heap, which is what lets a bare `time.After(d)` — whose Timer is
    // dropped on the spot, only the channel kept — still fire.
    private static readonly ConditionalWeakTable<object, runtimeTimer> s_timerState = new();

    // The single lock (see LOCKING DISCIPLINE above), the deadline-ordered heap, and the handle the
    // service thread waits on alongside its high-resolution timer so a newly-armed earlier deadline
    // interrupts an in-progress wait.
    private static readonly object s_timerLock = new();
    private static readonly PriorityQueue<(runtimeTimer timer, int64 gen), int64> s_timerHeap = new();
    private static readonly AutoResetEvent s_timerWake = new(false);
    private static Thread? s_timerThread;

    // Sleep pauses the calling goroutine for at least the duration d. Mirrors runtime.timeSleep:
    // a non-positive duration returns immediately, and the deadline is computed on the monotonic
    // clock with Go's overflow guard.
    public static partial void Sleep(Duration d)
    {
        if (d <= 0)
        {
            return;
        }

        int64 deadline = runtimeNano() + (int64)d;

        if (deadline < 0)
        {
            // N.B. runtimeNano() and d are both positive, so this is overflow — runtime.timeSleep
            // clamps to maxWhen identically.
            deadline = int64.MaxValue;
        }

        waitUntil(deadline, null);
    }

    // newTimer allocates a Timer (or, through the hand-owned tick.cs, a Ticker — same runtime
    // state) and arms it. Mirrors runtime.newTimer: build the state, modify(when, period, f, arg),
    // then mark it initialized.
    internal static partial ж<Timer> newTimer(int64 when, int64 period, Action<any, uintptr, int64> f, any arg, @unsafe.Pointer cp)
    {
        // cp is the same channel as `arg`, re-typed as an unsafe.Pointer so the runtime can mark it
        // a SYNCHRONOUS timer channel. This model reproduces the asynchronous channel behavior
        // instead (see the divergence note above), so the pointer is unused.
        _ = cp;

        ж<Timer> box = new(new Timer{ initTimer = true });
        armNewTimer(box, when, period, f, arg);
        return box;
    }

    // stopTimer stops a timer, reporting whether it was still pending. Mirrors runtime.timer.stop:
    // `pending = t.when > 0`, then clear `when`.
    internal static partial bool stopTimer(ж<Timer> _)
    {
        return stopRuntimeTimer(_);
    }

    // resetTimer re-arms a timer, reporting whether it was still pending beforehand. Mirrors
    // runtime.timer.reset → timer.modify with a nil f (which leaves f/arg untouched).
    internal static partial bool resetTimer(ж<Timer> t, int64 when, int64 period)
    {
        return resetRuntimeTimer(t, when, period);
    }

    // Installs the hidden state for a freshly allocated Timer/Ticker box and arms it. Shared with
    // the hand-owned Ticker constructor in tick.cs (Go's runtime.newTimer serves both, since
    // "Ticker and Timer have the same layout").
    internal static void armNewTimer(object box, int64 when, int64 period, Action<any, uintptr, int64> f, any? arg)
    {
        runtimeTimer timer = new()
        {
            f = f,
            arg = arg
        };

        s_timerState.Add(box, timer);
        modifyRuntimeTimer(timer, when, period);
    }

    // Stops the timer owned by the given Timer/Ticker box, reporting whether it was still pending
    // (armed and not yet fired). Shared with tick.cs — runtime.stopTimer serves both.
    internal static bool stopRuntimeTimer(object box)
    {
        runtimeTimer timer = runtimeTimerOf(box);

        lock (s_timerLock)
        {
            bool pending = timer.when > 0;
            timer.when = 0;
            // Cancels any firing already queued for this timer (see runtimeTimer.gen). Doing it
            // under the same lock the service thread validates entries under is what makes the
            // stop-vs-fire race safe in both directions: either the service thread has already
            // taken the callback (and `when` is 0, so `pending` correctly reports false), or this
            // generation bump invalidates its queued entry and the callback never runs.
            timer.gen++;
            return pending;
        }
    }

    // Re-arms the timer owned by the given box, reporting whether it was pending beforehand.
    // Shared with tick.cs — runtime.resetTimer serves both.
    internal static bool resetRuntimeTimer(object box, int64 when, int64 period)
    {
        return modifyRuntimeTimer(runtimeTimerOf(box), when, period);
    }

    // runtime.timer.modify: set the period and the new deadline, reporting whether the timer was
    // still pending. A Reset of an already-fired or stopped timer therefore returns false while
    // re-arming it, which is precisely what Timer.Reset documents.
    private static bool modifyRuntimeTimer(runtimeTimer timer, int64 when, int64 period)
    {
        if (when <= 0)
        {
            throw panic("timer when must be positive");
        }

        if (period < 0)
        {
            throw panic("timer period must be non-negative");
        }

        lock (s_timerLock)
        {
            bool pending = timer.when > 0;
            timer.period = period;
            armLocked(timer, when);
            return pending;
        }
    }

    // Queues the timer for `when` and wakes the service thread. Caller holds s_timerLock.
    private static void armLocked(runtimeTimer timer, int64 when)
    {
        timer.when = when;
        timer.gen++;
        s_timerHeap.Enqueue((timer, timer.gen), when);

        if (s_timerThread is null)
        {
            // Started on first use, not at package init: a program that never touches a timer never
            // pays for the thread. Background, so it never keeps the process alive — Go's timer
            // service does not either.
            s_timerThread = new Thread(serviceTimers)
            {
                IsBackground = true,
                Name = "go2cs.time.timers"
            };

            s_timerThread.Start();
        }

        // An AutoResetEvent latches, so this cannot be a lost wakeup even though the service thread
        // computes its deadline and starts waiting outside the lock: a Set that lands in that window
        // makes its next wait return at once and re-derive the head of the heap.
        s_timerWake.Set();
    }

    // Resolves a Timer/Ticker box to its hidden runtime state.
    private static runtimeTimer runtimeTimerOf(object box)
    {
        if (box is not null && s_timerState.TryGetValue(box, out runtimeTimer? timer))
        {
            return timer;
        }

        // Where Go reaches its own "timer used without initialization" throw. Package time already
        // guards ordinary misuse ahead of this (Timer.Stop/Reset panic on !initTimer), so this is
        // reachable only by operating on a COPY of a Timer/Ticker value — which does not carry the
        // hidden runtime state, exactly as in Go, where the copy loses the runtime fields that
        // follow the two visible ones.
        throw panic("time: timer used without initialization");
    }

    // The service thread: run every due timer in deadline order, then wait for the next deadline
    // (or for an arm to interrupt the wait). Mirrors the runtime's timers.run/timer.unlockAndRun.
    private static void serviceTimers()
    {
        List<(Action<any, uintptr, int64> f, any? arg, int64 delay)> due = new();

        while (true)
        {
            // 0 == nothing armed; wait for an arm instead of a deadline.
            int64 deadline = 0;
            due.Clear();

            lock (s_timerLock)
            {
                // Sampled ONCE for the whole drain and never re-read inside it — the pass's clock,
                // exactly as Go's timers.check samples `now` and passes it down through run and
                // unlockAndRun. This is what bounds a periodic timer to one firing per pass; see
                // ONE FIRING PER TIMER PER PASS above for the proof and for why re-reading it here
                // let a 1 ns ticker burst.
                int64 now = runtimeNano();

                while (s_timerHeap.TryPeek(out (runtimeTimer timer, int64 gen) entry, out int64 when))
                {
                    if (entry.timer.gen != entry.gen)
                    {
                        // Stopped, reset or re-armed after this entry was queued — drop it; the live
                        // entry, if any, is elsewhere in the heap (see runtimeTimer.gen).
                        s_timerHeap.Dequeue();
                        continue;
                    }

                    if (when > now)
                    {
                        deadline = when;
                        break;
                    }

                    s_timerHeap.Dequeue();

                    // runtime.timer.unlockAndRun: capture the callback, advance or clear `when`, and
                    // run the callback only after the lock is released. `delay` is how LATE the
                    // firing is (Go's `delay := now - t.when`, always >= 0); sendTime subtracts it
                    // to send the time the tick was scheduled for rather than the time it ran.
                    int64 delay = now - when;
                    due.Add((entry.timer.f, entry.timer.arg, delay));
                    entry.timer.gen++;

                    if (entry.timer.period > 0)
                    {
                        // Advance by WHOLE periods past a late firing, so the tick phase stays
                        // aligned to the original schedule instead of drifting — Go's documented
                        // "adjust the time interval or drop ticks to make up for slow receivers"
                        // (unlockAndRun: next = when + period*(1 + delay/period)). Combined with
                        // sendTime's non-blocking send onto the cap-1 channel, a receiver too slow
                        // to keep up therefore LOSES ticks rather than seeing them queue up or the
                        // ticker fall behind.
                        int64 next = when + entry.timer.period * (1 + delay / entry.timer.period);

                        if (next < 0)
                        {
                            next = int64.MaxValue;
                        }

                        entry.timer.when = next;
                        s_timerHeap.Enqueue((entry.timer, entry.timer.gen), next);
                    }
                    else
                    {
                        entry.timer.when = 0;
                    }
                }
            }

            foreach ((Action<any, uintptr, int64> f, any? arg, int64 delay) in due)
            {
                // `seq` is the runtime's stale-send sequence for SYNCHRONOUS timer channels; the
                // asynchronous model reproduced here never consults it, and neither sendTime nor
                // goFunc reads the parameter.
                f(arg!, default, delay);
            }

            if (deadline == 0)
            {
                s_timerWake.WaitOne();
            }
            else
            {
                waitUntil(deadline, s_timerWake);
            }
        }
    }

    // Blocks until the monotonic deadline, or until `interrupt` is signaled; returns true when
    // interrupted. Never returns early on the deadline path: Go guarantees a sleep of AT LEAST the
    // requested duration, so a short OS wake re-waits the remainder.
    private static bool waitUntil(int64 deadline, WaitHandle? interrupt)
    {
        while (true)
        {
            int64 remaining = deadline - runtimeNano();

            if (remaining <= 0)
            {
                return false;
            }

            if (!t_highResTimerProbed)
            {
                // Cached PER THREAD, matching the Go runtime's per-M highResTimer. The handle is
                // released by its SafeWaitHandle finalizer once the thread — and with it this
                // thread-static — becomes unreachable.
                t_highResTimerProbed = true;
                t_highResTimer = highResTimer.TryCreate();
            }

            highResTimer? timer = t_highResTimer;

            if (timer is not null && timer.Arm(remaining))
            {
                if (interrupt is null)
                {
                    timer.WaitOne();
                }
                else if (WaitHandle.WaitAny([timer, interrupt]) == 1)
                {
                    return true;
                }

                continue;
            }

            // Fallback where no high-resolution timer exists: wait coarsely to within a millisecond
            // of the deadline, then spin out the remainder. Coarse waits are quantized to the OS
            // timer tick, so this path can OVERSHOOT a short deadline — which is exactly why the
            // high-resolution timer above is preferred.
            int64 millis = remaining / 1_000_000;

            if (millis > 1)
            {
                int coarse = (int)Math.Min(millis - 1, int.MaxValue);

                if (interrupt is null)
                {
                    Thread.Sleep(coarse);
                }
                else if (interrupt.WaitOne(coarse))
                {
                    return true;
                }
            }
            else
            {
                Thread.SpinWait(1000);
            }
        }
    }

    [ThreadStatic]
    private static highResTimer? t_highResTimer;

    [ThreadStatic]
    private static bool t_highResTimerProbed;

    // A Windows high-resolution waitable timer, wrapped as a WaitHandle so the managed wait
    // primitives block on it directly. This is the same OS object the Go runtime creates for its own
    // sub-tick sleeps (runtime/os_windows.go: CreateWaitableTimerExW with
    // CREATE_WAITABLE_TIMER_HIGH_RESOLUTION, Windows 10 1803+) and it is what keeps a 1 ms Go timer
    // from becoming a ~15 ms one.
    private sealed class highResTimer : WaitHandle
    {
        private const uint CreateHighResolution = 0x2;
        private const uint TimerAllAccess = 0x1F0003;

        private highResTimer(nint handle)
        {
            SafeWaitHandle = new SafeWaitHandle(handle, ownsHandle: true);
        }

        internal static highResTimer? TryCreate()
        {
            if (!OperatingSystem.IsWindows())
            {
                return null;
            }

            nint handle;

            try
            {
                handle = CreateWaitableTimerExW(0, null, CreateHighResolution, TimerAllAccess);
            }
            catch (Exception)
            {
                // No such export at all (a non-Windows host reached through some other runtime) —
                // fall back to the coarse wait rather than failing the sleep.
                return null;
            }

            // Pre-1803 Windows rejects the high-resolution flag (ERROR_INVALID_PARAMETER). A plain
            // waitable timer would be no more precise than the coarse fallback, so there is nothing
            // to gain by retrying without the flag.
            return handle == 0 ? null : new highResTimer(handle);
        }

        // Arms the timer to signal `nanos` from now. A NEGATIVE due time is a relative interval in
        // 100 ns units; the truncation can only wake the waiter early, by under 100 ns, which the
        // caller's deadline loop absorbs.
        internal bool Arm(int64 nanos)
        {
            long dueTime = -(nanos / 100);

            if (dueTime == 0)
            {
                dueTime = -1;
            }

            return SetWaitableTimer(SafeWaitHandle, ref dueTime, 0, 0, 0, false);
        }

        [DllImport("kernel32.dll", EntryPoint = "CreateWaitableTimerExW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern nint CreateWaitableTimerExW(nint timerAttributes, string? timerName, uint flags, uint desiredAccess);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetWaitableTimer(SafeWaitHandle timer, ref long dueTime, int period, nint completionRoutine, nint completionArg, bool resume);
    }
}
