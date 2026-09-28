using System;
using System.Reflection;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using static go.runtime_package;

namespace GolibTests;

/// <summary>
/// Guards class F's sync.Mutex half (docs/phase4/DESIGN-managed-profiling.md I4, and the Mutex slice of
/// I3): a CONTENDED hand-owned <c>sync.Mutex</c> records a mutex-profile event on the unlocker (Go's
/// <c>semrelease1</c>) and a block-profile event on the waiter (Go's <c>semacquire1</c>), each only while
/// its rate is above zero -- and an UNCONTENDED Lock records nothing, because Go's fast path
/// (<c>cansemacquire</c>) returns before any profiling. Red against the hand-own that recorded nothing at
/// all (runtime/pprof TestMutexBlockFullAggregation: "did not see any samples in mutex profile").
/// </summary>
[TestClass]
public class SyncMutexProfileTests
{
    private sealed class Shared
    {
        public sync_package.Mutex M;
    }

    private static int64 Total(bool mutex)
    {
        var (n, _) = mutex ? MutexProfile(default) : BlockProfile(default);
        var records = new slice<BlockProfileRecord>((int)n + 16);
        var (m, ok) = mutex ? MutexProfile(records) : BlockProfile(records);
        Assert.IsTrue(ok, "the profile must fit a buffer sized from its own count");
        int64 count = 0;
        for (int i = 0; i < (int)m; i++)
            count += records[i].Count;
        return count;
    }

    // ONE contended handoff, deterministically: a holder takes the lock and keeps it until this thread
    // is waiting on it, then releases it to this thread. (Two free-running workers are not a fixture:
    // one can finish before the other starts, and SemaphoreSlim lets a releasing thread take the lock
    // straight back, so how many handoffs find a waiter depends on thread start-up.)
    private static void ContendOnce(Shared shared)
    {
        using var held = new ManualResetEventSlim();
        var holder = new Thread(() =>
        {
            shared.M.Lock();
            held.Set();
            Thread.Sleep(50);
            shared.M.Unlock();
        });
        holder.Start();
        held.Wait();
        shared.M.Lock();
        shared.M.Unlock();
        holder.Join();
    }

    private static void WithRates(int mutexFraction, nint blockRate, System.Action body)
    {
        nint previous = SetMutexProfileFraction(mutexFraction);
        SetBlockProfileRate(blockRate);
        try
        {
            body();
        }
        finally
        {
            SetMutexProfileFraction(previous);
            SetBlockProfileRate(0);
        }
    }

    [TestMethod]
    public void AContendedMutexRecordsOneMutexAndOneBlockEvent()
    {
        WithRates(1, 1, () =>
        {
            int64 mutexBefore = Total(mutex: true), blockBefore = Total(mutex: false);
            ContendOnce(new Shared());
            // Fraction 1: the event is always sampled and adds count += rate = 1 (Go's saveblockevent);
            // block rate 1: a wait of at least one tick adds count + 1.
            Assert.AreEqual(mutexBefore + 1, Total(mutex: true), "the handoff's Unlock must record exactly one mutex event");
            Assert.AreEqual(blockBefore + 1, Total(mutex: false), "the waiting Lock must record exactly one block event");
        });
    }

    [TestMethod]
    public void AnUncontendedMutexRecordsNothing()
    {
        WithRates(1, 1, () =>
        {
            int64 mutexBefore = Total(mutex: true), blockBefore = Total(mutex: false);
            var shared = new Shared();
            for (int i = 0; i < 100; i++)
            {
                shared.M.Lock();
                shared.M.Unlock();
            }
            Assert.AreEqual(mutexBefore, Total(mutex: true), "an uncontended Unlock hands off to nobody: no mutex event");
            Assert.AreEqual(blockBefore, Total(mutex: false), "an uncontended Lock never blocks: no block event");
        });
    }

    [TestMethod]
    public void AtRateZeroContentionRecordsNothing()
    {
        WithRates(0, 0, () =>
        {
            int64 mutexBefore = Total(mutex: true), blockBefore = Total(mutex: false);
            ContendOnce(new Shared());
            Assert.AreEqual(mutexBefore, Total(mutex: true), "at fraction 0 the mutex profile records nothing");
            Assert.AreEqual(blockBefore, Total(mutex: false), "at rate 0 the block profile records nothing");
        });
    }

    // R's review arm: a stamped waiter that acquires WITHOUT a handoff (the snapshot race the hand-own
    // names: it saw the gate held, stamped, and the gate came free before it blocked) must not leave its
    // stamp behind. Otherwise the next contention episode's handoff charges the whole idle gap since that
    // stale stamp as one mutex event. Reached by reflection: WaitStamps is private to the hand-own.
    [TestMethod]
    public void AStampThatAcquiresWithoutAHandoffDoesNotLeakIntoTheNextEpisode()
    {
        Type type = typeof(sync_package).GetNestedType("WaitStamps", BindingFlags.NonPublic)!;
        Assert.IsNotNull(type, "the hand-own's WaitStamps type");
        object stamps = Activator.CreateInstance(type, nonPublic: true)!;
        MethodInfo enqueue = type.GetMethod("Enqueue", BindingFlags.NonPublic | BindingFlags.Instance)!;
        MethodInfo acquired = type.GetMethod("Acquired", BindingFlags.NonPublic | BindingFlags.Instance)!;
        MethodInfo tryHandoff = type.GetMethod("TryHandoff", BindingFlags.NonPublic | BindingFlags.Instance)!;

        const long stale = 1_000_000_000_000L;
        long now = GoCputicks();
        enqueue.Invoke(stamps, [now - stale]);   // episode 1: stamped, then acquired with no handoff
        acquired.Invoke(stamps, null);
        enqueue.Invoke(stamps, [GoCputicks()]);  // episode 2: a fresh contended wait
        object[] args = [0L];
        bool handed = (bool)tryHandoff.Invoke(stamps, args)!;
        acquired.Invoke(stamps, null);           // balance the process-wide stamped-waiter count

        Assert.IsTrue(handed, "episode 2's waiter is stamped, so its handoff charges it");
        Assert.IsTrue((long)args[0] < stale / 2, $"the handoff charged {args[0]} cycles: episode 1's stale stamp leaked into episode 2");
    }
}
