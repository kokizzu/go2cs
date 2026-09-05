using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using go.golib;
using static go.runtime_package;

namespace GolibTests;

/// <summary>
/// Guards Q54 (runtime/lock_managed_impl.cs): a goroutine that dies holding a converted runtime
/// lock leaves the key at keyAbandoned, and the NEXT locker dies by name on its first poll instead
/// of polling to the package deadline; a normal unlock leaves no entry; nested locks are abandoned
/// together; the converted `key = 0` re-init makes an abandoned lock usable again; abandoning with
/// nothing held is a no-op. The probes are two private runtime mutexes reached through Go-prefixed
/// public helpers. Negative control: the poisoning CAS removed from the abandon entry -> the arms
/// that expect a named panic read their locker still polling at the arm's deadline.
/// </summary>
[TestClass]
public class RuntimeLockAbandonTests
{
    private sealed class Outcome
    {
        public bool Locked;
        public string? Panic;
        public long Goid;
        public int Abandoned = -1;
        public Exception? Failure;
    }

    // Run `body` on a fresh goroutine (a dedicated thread, so the held-lock list is its own) and
    // wait for it; a goroutine still polling at the deadline is the RED this guard exists to show.
    private static (Outcome outcome, bool finished) OnGoroutine(Action<Outcome> body, int waitSeconds = 5)
    {
        Outcome outcome = new();
        using ManualResetEventSlim done = new(false);

        Goroutine.Start(() =>
        {
            try
            {
                outcome.Goid = Goroutine.Current!.Id;
                body(outcome);
            }
            catch (PanicException e)
            {
                outcome.Panic = e.Message;
            }
            catch (Exception e)
            {
                outcome.Failure = e;
            }
            finally
            {
                done.Set();
            }
        });

        bool finished = done.Wait(TimeSpan.FromSeconds(waitSeconds));
        return (outcome, finished);
    }

    private static void LockOn(Outcome o, int which)
    {
        GoRuntimeLockProbeLock(which);
        o.Locked = true;
    }

    [TestCleanup]
    public void ResetProbes()
    {
        GoRuntimeLockProbeReset(0);
        GoRuntimeLockProbeReset(1);
    }

    [TestMethod]
    public void AGoroutineThatDiesHoldingARuntimeLockPoisonsItAndTheNextLockerPanicsByName()
    {
        (Outcome dying, bool finishedA) = OnGoroutine(o =>
        {
            GoRuntimeLockProbeLock(0);
            Assert.AreEqual(1, GoRuntimeLocksHeldByCurrentThread(), "the held-lock list carries the lock");
            o.Abandoned = GoAbandonRuntimeLocksHeldByCurrentThread("GuardException: simulated death holding probe 0");
            Assert.AreEqual(0, GoRuntimeLocksHeldByCurrentThread(), "the list is cleared by the abandonment");
        });
        Assert.IsTrue(finishedA, "the dying goroutine returned");
        Assert.IsNull(dying.Failure, dying.Failure?.ToString());
        Assert.AreEqual(1, dying.Abandoned, "exactly the one held key was poisoned");

        (Outcome next, bool finishedB) = OnGoroutine(o => LockOn(o, 0));
        Assert.IsTrue(finishedB, "the next locker finished within the deadline instead of polling forever (the hang this guard exists for)");
        Assert.IsFalse(next.Locked, "the abandoned lock was never handed out");
        Assert.IsNotNull(next.Panic, "the next locker died on a Go panic");
        StringAssert.Contains(next.Panic, $"runtime lock abandoned by goroutine {dying.Goid}", "the panic names the goroutine that died holding it");
        StringAssert.Contains(next.Panic, "GuardException: simulated death holding probe 0", "the panic names the reason it died");
    }

    [TestMethod]
    public void AnUnlockedLockLeavesNoEntryAndTheNextLockerProceeds()
    {
        (Outcome first, bool finishedA) = OnGoroutine(o =>
        {
            GoRuntimeLockProbeLock(0);
            GoRuntimeLockProbeUnlock(0);
            Assert.AreEqual(0, GoRuntimeLocksHeldByCurrentThread(), "unlock2 pops the entry");
            o.Abandoned = GoAbandonRuntimeLocksHeldByCurrentThread("GuardException: nothing held");
        });
        Assert.IsTrue(finishedA);
        Assert.IsNull(first.Failure, first.Failure?.ToString());
        Assert.AreEqual(0, first.Abandoned, "nothing to abandon after a normal unlock");

        (Outcome next, bool finishedB) = OnGoroutine(o =>
        {
            LockOn(o, 0);
            GoRuntimeLockProbeUnlock(0);
        });
        Assert.IsTrue(finishedB);
        Assert.IsTrue(next.Locked, "the lock was free");
        Assert.IsNull(next.Panic);
    }

    [TestMethod]
    public void NestedLocksAreAbandonedTogether()
    {
        (Outcome dying, bool finishedA) = OnGoroutine(o =>
        {
            GoRuntimeLockProbeLock(0);
            GoRuntimeLockProbeLock(1);
            Assert.AreEqual(2, GoRuntimeLocksHeldByCurrentThread());
            o.Abandoned = GoAbandonRuntimeLocksHeldByCurrentThread("GuardException: died holding both probes");
        });
        Assert.IsTrue(finishedA);
        Assert.IsNull(dying.Failure, dying.Failure?.ToString());
        Assert.AreEqual(2, dying.Abandoned);

        (Outcome onZero, bool f0) = OnGoroutine(o => LockOn(o, 0));
        (Outcome onOne, bool f1) = OnGoroutine(o => LockOn(o, 1));
        Assert.IsTrue(f0 && f1, "both lockers finished within the deadline");
        Assert.IsFalse(onZero.Locked || onOne.Locked);
        StringAssert.Contains(onZero.Panic, "died holding both probes");
        StringAssert.Contains(onOne.Panic, "died holding both probes");
    }

    [TestMethod]
    public void TheConvertedKeyReInitMakesAnAbandonedLockUsableAgain()
    {
        (Outcome dying, bool finishedA) = OnGoroutine(o =>
        {
            GoRuntimeLockProbeLock(0);
            o.Abandoned = GoAbandonRuntimeLocksHeldByCurrentThread("GuardException: died before the re-init");
        });
        Assert.IsTrue(finishedA);
        Assert.AreEqual(1, dying.Abandoned);

        GoRuntimeLockProbeReset(0);   // mheap's `key = 0` re-init, the contract the header preserves

        (Outcome next, bool finishedB) = OnGoroutine(o =>
        {
            LockOn(o, 0);
            GoRuntimeLockProbeUnlock(0);
        });
        Assert.IsTrue(finishedB);
        Assert.IsTrue(next.Locked, "a re-initialised key is a free lock again");
        Assert.IsNull(next.Panic);
    }

    [TestMethod]
    public void AbandoningWithNothingHeldIsANoOp()
    {
        (Outcome o, bool finished) = OnGoroutine(o =>
        {
            o.Abandoned = GoAbandonRuntimeLocksHeldByCurrentThread("GuardException: never locked anything");
        });
        Assert.IsTrue(finished);
        Assert.AreEqual(0, o.Abandoned);
        Assert.IsNull(o.Failure);
    }
}
