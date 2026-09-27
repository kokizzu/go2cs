using Microsoft.VisualStudio.TestTools.UnitTesting;
using static go.runtime_package;

namespace GolibTests;

/// <summary>
/// The runtime's stopTheWorld (runtime managed_impl.cs). The converted body took worldsema and then
/// died in stopTheWorldWithSema on the managed host's missing P, so every caller left worldsema held.
/// Once runtime's semaphore could park (sema_impl.cs), the next caller waited on that leaked permit
/// for ever: the runtime row's host hung in TestDebugLogInterleaving. The managed host cannot stop
/// the world, so stopTheWorld refuses by name, and it refuses before it takes worldsema.
/// </summary>
[TestClass]
public class RuntimeStopTheWorldTests
{
    private const int TimeoutMs = 30000;

    [TestMethod]
    public void StopTheWorldRefusesByNameAndLeavesWorldsemaFree()
    {
        (string? stopFailure, bool worldsemaFree) = GoStopTheWorldRefusalProbe(TimeoutMs);

        string reading = $"stopTheWorld: {stopFailure ?? "returned"}; worldsema acquired afterwards: {worldsemaFree}";

        Assert.IsNotNull(stopFailure, reading);
        StringAssert.StartsWith(stopFailure, "PanicException: runtime: stopTheWorld:", reading);
        Assert.IsTrue(worldsemaFree, $"the refusal leaked worldsema -- {reading}");
    }
}
