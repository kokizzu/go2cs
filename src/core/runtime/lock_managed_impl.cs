// Copyright 2011 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
using go;

// Hand-finished conversion — the MANAGED CORE of runtime's mutex + one-time note, shared by every
// target platform.
//
// Go has two flavors of this protocol and selects one per GOOS. lock_sema.go (windows, darwin,
// plan9, aix …) treats mutex.key as a tagged atomic slot — 0 unlocked, `locked` (1) held, or an *m
// ADDRESS|locked heading a waiter chain through m.nextwaitm — and parks waiters on OS semaphores
// (semacreate/semasleep/semawakeup). lock_futex.go (linux, freebsd, dragonfly) uses a {0,1,2} slot
// (mutex_unlocked/mutex_locked/mutex_sleeping) and parks them on a futex (futexsleep/futexwakeup).
//
// NEITHER OS primitive has a managed realization, and the managed answer to both is the same one:
// a managed runtime cannot smuggle an m reference through the uintptr slot (the manual muintptr
// rightly panics on a non-zero integer), so the key protocol is restricted to {0, keyLocked} and
// parking is replaced by SpinWait escalation (progressive Thread.Yield/Sleep — adaptive spinning).
// The note (one-time notification) never stores the waiting m either: it is a pure signaled/clear
// latch polled with SpinWait. That the two flavors COLLAPSE to one managed implementation is why
// this file is flat rather than copied per GOOS — the semantics are OS-agnostic once the OS
// primitive is gone, and a second copy would be a second thing to keep correct.
//
// What is NOT shared is the SIGNATURE Go gives one entry point: notetsleep_internal takes
// (n, ns, gp, deadline) in lock_sema.go and (n, ns) in lock_futex.go. Each flavor's own companion
// therefore declares that one function and delegates to noteSleepDeadline below — see
// windows/lock_sema_impl.cs, darwin/lock_sema_impl.cs and linux/lock_futex_impl.cs, which layout L3
// routes to exactly the platforms their principal is built on.
//
// PRESERVED contracts: mutual exclusion; release visibility (Interlocked full fences ≥ Go's
// atomics); notewakeup's double-wakeup throw; noteclear/mheap's `key = 0` re-init compatibility —
// and that last one is why the managed slot uses the value 1 for "held" on BOTH flavors: it is what
// Go's own `locked` and `mutex_locked` are, so a converted `key = 0` still means unlocked and a
// converted comparison against either constant still reads true.
// NOT modeled (deliberately, documented): the waiter QUEUE (fairness/FIFO wakeup), lock
// profiling (lockTimer/mLockProfile), and the m.locks/preempt bookkeeping — getg() is a Go
// compiler intrinsic with no managed realization yet (a [ThreadStatic] g/m model is the future
// root that unlocks it); when getg lands, the bookkeeping lines return here. Known divergence:
// Go's throw() is process-fatal while managed exceptions are catchable, so an exception
// unwinding between lock2 and unlock2 orphans key=keyLocked and later lockers poll forever —
// where Go would have died loudly (adversarial review, latent L2).
[module: GoManualConversion]

namespace go;

using System.Threading;

partial class runtime_package {

// The held value of a key slot. Go spells it `locked` in lock_sema.go and `mutex_locked` in
// lock_futex.go — both are 1, and neither name is declared on the other flavor's platforms, so the
// shared core carries its own spelling of the same value.
internal static uintptr keyLocked => 1;

internal static bool mutexContended(ж<mutex> Ꮡl) {
    // No waiter chain exists in the managed model, so contention beyond the held bit is not
    // observable (consumed only by lock-profiling paths, which are not modeled).
    return false;
}

internal static void lock2(ж<mutex> Ꮡl) {
    ref var l = ref Ꮡl.Value;

    // Speculative grab, then adaptive test-test-and-set spin (the Volatile.Read pre-test keeps
    // contended pollers off exclusive cache-line acquisition; SpinWait escalates spin → yield →
    // sleep, standing in for Go's active_spin/osyield/semasleep — futexsleep on the futex flavor —
    // ladder).
    if (Interlocked.CompareExchange(ref l.key.Value, keyLocked, 0) == 0) {
        return;
    }

    SpinWait spinner = default;

    while (Volatile.Read(ref l.key.Value) != 0 || Interlocked.CompareExchange(ref l.key.Value, keyLocked, 0) != 0) {
        spinner.SpinOnce();
    }
}

// We might not be holding a p in this code.
internal static void unlock2(ж<mutex> Ꮡl) {
    ref var l = ref Ꮡl.Value;

    // No waiter chain to dequeue and nobody parked to wake — release the slot; a spinning lock2
    // observes it. The futex flavor's mutex_sleeping state has no managed counterpart for the same
    // reason: nothing ever sleeps on the slot.
    Interlocked.Exchange(ref l.key.Value, 0);
}

// The rendezvous for note waiters that BLOCK instead of polling — today notetsleepg alone (see
// below for why it is the only one). notewakeup pulses this gate after it flips the key, and a
// blocking waiter re-tests the key while HOLDING the gate before it waits, so the
// set-then-pulse / test-then-wait ordering cannot drop a wakeup: a waker that flips the key
// between a waiter's test and its wait must still acquire the gate to pulse, and cannot, because
// the waiter holds it until Monitor.Wait releases it atomically.
//
// One gate for all notes rather than one per note, because nothing here needs to scale: the note
// is a rendezvous the runtime uses a handful of times per process, and a waiter re-tests its OWN
// key on every wake, so a spurious cross-note pulse costs one re-test. Spin-based waiters
// (notesleep, noteSleepDeadline) never touch the gate and still observe the key exactly as
// before — the gate is additive, not a replacement.
//
// Go's own sigqueue.go warns that a signal handler "cannot block, allocate memory, or use locks",
// which is what makes notewakeup's Go implementation lock-free. That constraint is a property of
// POSIX async-signal context and does not survive into the managed model: on Windows the console
// control handler — the one caller that reaches notewakeup from an OS callback — runs on an
// ordinary thread the OS spins up for the event, where taking a managed lock is unremarkable.
private static readonly object s_noteGate = new();

// One-time notifications.
internal static void notewakeup(ж<note> Ꮡn) {
    ref var n = ref Ꮡn.Value;

    uintptr v = Interlocked.Exchange(ref n.key.Value, keyLocked);

    if (v == keyLocked) {
        // Two notewakeups! Not allowed.
        @throw("notewakeup - double wakeup"u8);
    }

    // Release any blocking waiter. The key is already flipped, so a waiter that has not yet
    // reached its wait sees the new value on its pre-wait re-test and never waits at all.
    lock (s_noteGate) {
        Monitor.PulseAll(s_noteGate);
    }
    // v == 0: nothing was waiting — done. A non-zero non-locked value (an m address in the
    // semaphore original, a sleeper count in the futex one) cannot occur: the managed notesleep
    // never stores anything into the slot.
}

internal static void notesleep(ж<note> Ꮡn) {
    ref var n = ref Ꮡn.Value;

    SpinWait spinner = default;

    while (ᐧ) {
        uintptr v = Volatile.Read(ref n.key.Value);

        if (v == keyLocked) {
            return;
        }

        if (v != 0) {
            // The slot only ever holds {0, keyLocked} in the managed model — anything else is
            // corruption; keep Go's loud diagnostic rather than spinning silently.
            @throw("notesleep - waitm out of sync"u8);
        }

        spinner.SpinOnce();
    }
}

// The timed wait both flavors' notetsleep_internal delegates to. Managed timeout latch: poll until
// signaled or the budget elapses. ns < 0 waits forever (as Go's semasleep(-1) / futexsleep(-1));
// millisecond granularity stands in for Go's nanosecond budget (the note is a coarse rendezvous —
// parking precision is not part of its contract).
internal static bool noteSleepDeadline(ж<note> Ꮡn, int64 ns) {
    ref var n = ref Ꮡn.Value;

    if (ns < 0) {
        notesleep(Ꮡn);
        return true;
    }

    long deadlineMs = System.Environment.TickCount64 + (ns / 1000000) + 1;
    SpinWait spinner = default;

    while (ᐧ) {
        uintptr v = Volatile.Read(ref n.key.Value);

        if (v == keyLocked) {
            return true;
        }

        if (v != 0) {
            @throw("notetsleep - waitm out of sync"u8);
        }

        if (System.Environment.TickCount64 >= deadlineMs) {
            return false;
        }

        spinner.SpinOnce();
    }
}

// The one member of this family that genuinely BLOCKS. Go's notetsleepg is "notetsleep, but called
// on a user g rather than g0", and its body is getg() → semacreate(gp.m) → entersyscallblock →
// notetsleep_internal → exitsyscall: it tells the scheduler this M is about to sit in a syscall so
// another M can take the P. getg() is still an unimplemented intrinsic, so every caller threw here
// before reaching the note at all — which is why this wrapper is hand-owned while its siblings are
// not (manualConversionFuncs, "notetsleepg").
//
// The scheduler half is not modeled and is not owed: golib gives every goroutine its own dedicated
// thread, so a goroutine that blocks costs nothing but that thread and there is no P to hand off.
// That is also what makes a real block CORRECT here rather than the SpinWait escalation the rest of
// this file uses. Its two callers are the ones that idle indefinitely — sigqueue's signal_recv,
// which waits for a signal that may never come for the life of the process, and profbuf's reader —
// and polling either would burn a thread forever to observe nothing. The spin waiters above stay as
// they are: they back short, contended rendezvous where a wait would cost more than it saves.
//
// No park-accounting scope is taken. DESIGN-cooperative-scheduler.md §6 row 11 places the note
// protocol below the goroutine model deliberately — in Go it parks Ms, not Gs — so it is outside
// the gopark/goready contract by ruling rather than by omission; §5.3's Goroutine.Park is in any
// case still design-only in golib today.
internal static bool notetsleepg(ж<note> Ꮡn, int64 ns) {
    ref var n = ref Ꮡn.Value;

    long deadlineMs = ns < 0 ? 0 : System.Environment.TickCount64 + (ns / 1000000) + 1;

    lock (s_noteGate) {
        while (ᐧ) {
            uintptr v = Volatile.Read(ref n.key.Value);

            if (v == keyLocked) {
                return true;
            }

            if (v != 0) {
                // The slot only ever holds {0, keyLocked} in the managed model — anything else is
                // corruption; keep Go's loud diagnostic rather than waiting silently.
                @throw("notetsleep - waitm out of sync"u8);
            }

            if (ns < 0) {
                Monitor.Wait(s_noteGate);
                continue;
            }

            long remainingMs = deadlineMs - System.Environment.TickCount64;

            if (remainingMs <= 0) {
                return false;
            }

            Monitor.Wait(s_noteGate, (int)remainingMs);
        }
    }
}

} // end runtime_package
