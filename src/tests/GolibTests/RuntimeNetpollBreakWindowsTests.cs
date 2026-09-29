using System;
using System.Reflection;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

// runtime.netpollBreak on Windows (src/core/runtime/windows/netpoll_windows_impl.cs). proc_test.go's
// TestNetpollBreak calls NetpollGenericInit and then NetpollBreak. netpollGenericInit is a no-op here
// (netpoll_impl.cs), so no completion port exists, and the converted netpollBreak's
// PostQueuedCompletionStatus reached stdcall4 -> asmcgocall's throwing stub: an infrastructure error on
// the windows runtime row, which no disclosure can absorb. It must refuse BY NAME instead, as linux's
// does (linux/netpoll_epoll_impl.cs), with the same leading words. Both functions are internal to
// runtime, so the arm reaches them by name, the calls TestNetpollBreak makes through export_test.go.
// Windows-only by construction: the hand-own lives in runtime's windows/ folder.
[TestClass]
public class RuntimeNetpollBreakWindowsTests
{
    private static void CallRuntime(string name)
    {
        MethodInfo method = typeof(runtime_package).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static, Type.EmptyTypes)
            ?? throw new MissingMethodException(nameof(runtime_package), name);

        try
        {
            method.Invoke(null, null);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

    [TestMethod]
    public void NetpollBreakRefusesByNameWithNoCompletionPort()
    {
        Exception? thrown = null;
        bool returned = false;
        using ManualResetEventSlim done = new();

        goǃ(() =>
        {
            try
            {
                CallRuntime("netpollGenericInit");
                CallRuntime("netpollBreak");
                returned = true;
            }
            catch (Exception ex) { thrown = ex; }
            finally { done.Set(); }
        });

        Assert.IsTrue(done.Wait(TimeSpan.FromSeconds(30)), "the goroutine did not finish");
        Assert.IsFalse(returned, "netpollBreak returned: the managed host has no poller to break");
        Assert.IsInstanceOfType(thrown, typeof(PanicException), $"netpollBreak must be a Go panic, got {thrown?.GetType().Name}: {thrown?.Message}");
        StringAssert.StartsWith(thrown!.Message, "runtime: netpollBreak: the managed host has no runtime poller to break", thrown.Message);
    }
}
