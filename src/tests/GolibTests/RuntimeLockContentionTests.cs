using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

// runtime.mutexContended and the runtime-lock wait metric (src/core/runtime/lock_managed_impl.cs).
// mutexContended answered a constant false, so TestRuntimeLockMetricsAndProfile/runtime.lock -- whose
// lock holder spins until `runtime.MutexContended(mu)` reports its partner waiting -- live-locked and
// ran the runtime row to its 30 m deadline. A lock2 on its slow path IS a waiting M; it now registers
// as one, and charges its wait to /sync/mutex/wait/total:seconds.
[TestClass]
public class RuntimeLockContentionTests
{
    private const int Probe = 1;

    [TestMethod]
    public void AContenderSpinningInLock2IsVisibleToMutexContended()
    {
        runtime_package.GoRuntimeLockProbeReset(Probe);
        Assert.IsFalse(runtime_package.GoRuntimeLockProbeContended(Probe), "an unheld, unwaited lock is not contended");

        runtime_package.GoRuntimeLockProbeLock(Probe);
        long before = runtime_package.GoTotalMutexWaitTimeNanos();
        using ManualResetEventSlim acquired = new();

        Thread contender = new(() =>
        {
            runtime_package.GoRuntimeLockProbeLock(Probe);   // spins: the lock is held
            acquired.Set();
            runtime_package.GoRuntimeLockProbeUnlock(Probe);
        }) { IsBackground = true };

        contender.Start();

        bool seen = false;
        Stopwatch clock = Stopwatch.StartNew();

        while (clock.Elapsed < TimeSpan.FromSeconds(10))
        {
            if (runtime_package.GoRuntimeLockProbeContended(Probe)) { seen = true; break; }
            Thread.Sleep(1);
        }

        Thread.Sleep(5);   // a measurable wait for the metric arm
        runtime_package.GoRuntimeLockProbeUnlock(Probe);

        Assert.IsTrue(acquired.Wait(TimeSpan.FromSeconds(10)), "the contender never acquired the released lock");
        Assert.IsTrue(contender.Join(TimeSpan.FromSeconds(10)));
        Assert.IsTrue(seen, "a goroutine spinning in lock2 must read as contention (Go: some M is waiting)");
        Assert.IsFalse(runtime_package.GoRuntimeLockProbeContended(Probe), "once the waiter has the lock, nobody waits");

        long after = runtime_package.GoTotalMutexWaitTimeNanos();
        Assert.IsTrue(after - before >= 1_000_000,
            $"the contended wait (~5 ms+) is charged to /sync/mutex/wait/total:seconds; grew {after - before} ns");
    }
}
