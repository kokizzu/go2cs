using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

// The linux thread primitives runtime's TestNewOSProc0 and TestSignalM reach: clone (a raw clone syscall
// that starts a function pointer on a new OS thread), getpid and tgkill (a signal to one thread of the
// process). They are assembly declarations, so the host filled them with a NotImplementedException stub,
// which the test host records as an infrastructure-error that no manifest entry can disclose. The
// contract: getpid answers the process id; clone and tgkill refuse by name (goroutines here are CLR
// threads with no M and no g0 stack, so neither has a managed meaning). Linux-only by construction: the
// probes exist in the linux flavour of the runtime, so on any other target each test reports Inconclusive.
[TestClass]
public class RuntimeLinuxThreadPrimitivesTests
{
    private static MethodInfo Probe(string name)
    {
        MethodInfo? method = typeof(runtime_package).GetMethod(name, BindingFlags.Public | BindingFlags.Static);

        if (method is null)
            Assert.Inconclusive($"{name} exists only in the linux flavour of the runtime");

        return method!;
    }

    private static string RefusalOf(string probe)
    {
        MethodInfo method = Probe(probe);
        TargetInvocationException ex = Assert.ThrowsException<TargetInvocationException>(() => method.Invoke(null, null));
        Assert.IsInstanceOfType(ex.InnerException, typeof(PanicException), $"{probe} must panic, not throw {ex.InnerException?.GetType().Name}");
        return ex.InnerException!.Message;
    }

    [TestMethod]
    public void GetpidAnswersTheProcessId()
    {
        Assert.AreEqual((nint)Environment.ProcessId, (nint)Probe("GoGetpidProbe").Invoke(null, null)!);
    }

    [TestMethod]
    public void CloneRefusesByName()
    {
        StringAssert.Contains(RefusalOf("GoCloneProbe"), "clone");
    }

    [TestMethod]
    public void TgkillRefusesByName()
    {
        StringAssert.Contains(RefusalOf("GoTgkillProbe"), "tgkill");
    }
}
