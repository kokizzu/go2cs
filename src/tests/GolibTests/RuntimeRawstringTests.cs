using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using static go.runtime_package;

namespace GolibTests;

// runtime.rawstring (src/core/runtime/managed_impl.cs). Go's body allocates through mallocgc, which
// this host does not run (the managed model has no Go heap), so it died in mallocgcTiny on an
// anonymous nil dereference and took gostringw, and with it runtime's TestStringW, along. Its
// contract is the one Go states: the returned string and byte slice refer to the same storage.
[TestClass]
public class RuntimeRawstringTests
{
    [TestMethod]
    public void TheStringSeesWhatTheSliceWrites()
    {
        Assert.AreEqual("xxxxx", (string)GoRawstringProbe(5, (byte)'x'));
        Assert.AreEqual("", (string)GoRawstringProbe(0, (byte)'x'));
    }

    [TestMethod]
    public void ALargeStringIsTheRequestedLength()
    {
        Assert.AreEqual(1 << 20, (int)len(GoRawstringProbe(1 << 20, (byte)'y')));
    }
}
