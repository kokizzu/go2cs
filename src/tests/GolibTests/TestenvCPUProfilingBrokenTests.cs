using Microsoft.VisualStudio.TestTools.UnitTesting;
using testenv = go.@internal.testenv_package;

namespace GolibTests;

/// <summary>
/// Guards the hand-owned <c>internal/testenv.CPUProfilingBroken</c> (internal/testenv/testenv_impl.cs): the
/// managed runtime has no CPU sampler (Go's plan9 case), so the answer is true on every target and
/// runtime/pprof's CPU tests end in Go's own Skipf instead of doubling their duration into the package
/// deadline. Red against the converted body, which answers false for windows, linux and darwin.
/// </summary>
[TestClass]
public class TestenvCPUProfilingBrokenTests
{
    [TestMethod]
    public void CPUProfilingIsReportedBrokenOnTheManagedHost()
    {
        Assert.IsTrue(testenv.CPUProfilingBroken());
    }
}
