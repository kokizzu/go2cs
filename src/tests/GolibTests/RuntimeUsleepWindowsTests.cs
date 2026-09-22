using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

// runtime.usleep on Windows (src/core/runtime/windows/usleep_windows_impl.cs). The converted body waited
// through stdcall -> asmcgocall and threw NotImplementedException; the runtime row's host died on one
// inside TestRuntimeLockMetricsAndProfile/runtime.lock/sample-1. Windows-only by construction: the
// body and its GoUsleep seam live in runtime's windows/ folder.
[TestClass]
public class RuntimeUsleepWindowsTests
{
    private static (Exception? thrown, TimeSpan elapsed) OnGoroutine(uint us)
    {
        Exception? thrown = null;
        TimeSpan elapsed = default;
        using ManualResetEventSlim done = new();

        goǃ(() =>
        {
            try
            {
                Stopwatch clock = Stopwatch.StartNew();
                runtime_package.GoUsleep(us);
                elapsed = clock.Elapsed;
            }
            catch (Exception e) { thrown = e; }
            finally { done.Set(); }
        });

        Assert.IsTrue(done.Wait(TimeSpan.FromSeconds(30)), "the goroutine did not finish");
        return (thrown, elapsed);
    }

    [TestMethod]
    public void UsleepOneMillisecondWaitsAtLeastThatAndNotFarLonger()
    {
        var (thrown, elapsed) = OnGoroutine(1000);

        Assert.IsNull(thrown, $"usleep threw: {thrown}");
        Assert.IsTrue(elapsed >= TimeSpan.FromMilliseconds(1), $"usleep(1000) returned after {elapsed.TotalMilliseconds} ms");
        Assert.IsTrue(elapsed < TimeSpan.FromMilliseconds(100), $"usleep(1000) took {elapsed.TotalMilliseconds} ms");
    }

    [TestMethod]
    public void UsleepZeroReturns()
    {
        var (thrown, elapsed) = OnGoroutine(0);

        Assert.IsNull(thrown, $"usleep threw: {thrown}");
        Assert.IsTrue(elapsed < TimeSpan.FromMilliseconds(100), $"usleep(0) took {elapsed.TotalMilliseconds} ms");
    }

    [TestMethod]
    public void UsleepBelowAMillisecondStillWaitsItsMicroseconds()
    {
        var (thrown, elapsed) = OnGoroutine(200);

        Assert.IsNull(thrown, $"usleep threw: {thrown}");
        Assert.IsTrue(elapsed >= TimeSpan.FromTicks(200 * TimeSpan.TicksPerMillisecond / 1000),
            $"usleep(200) returned after {elapsed.TotalMilliseconds * 1000} µs");
    }
}
