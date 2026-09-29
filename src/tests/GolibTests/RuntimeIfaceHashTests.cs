using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using static go.runtime_package;

namespace GolibTests;

// runtime.ifaceHash (src/core/runtime/managed_impl.cs), the hash Go gives a value held in interface{ F() }.
// Its converted body hands interhash a pointer to the interface variable, which here is a managed
// reference with no address, so the call died in the arm-2a refusal ("*iface over 0x...") and took the
// IfaceKey half of runtime's TestSmhasherAvalanche with it, after efaceHash had made the EfaceKey half pass.
// A nil interface hashes to its seed in Go (interhash returns h when the itab is nil); the value-hashing
// arms are shared with efaceHash (RuntimeEfaceHashTests) and the dynamic-value row is the runtime test itself.
[TestClass]
public class RuntimeIfaceHashTests
{
    [TestMethod]
    public void ANilInterfaceHashesToItsSeed()
    {
        Assert.AreEqual(new uintptr(7), GoIfaceHashProbe(null, new uintptr(7)));
    }
}
