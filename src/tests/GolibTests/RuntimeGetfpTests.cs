using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

// runtime.getfp (src/core/runtime/stubs_impl.cs). A PartialStubGenerator stub until 2026-09-22, it
// killed the runtime row's test host on the first entersyscall: getcallerfp -> getfp threw
// NotImplementedException on a goroutine started by TestPreemptionAfterSyscall/10µs. Go's contract is
// "the frame pointer register of its caller or 0 if not implemented" (stubs_amd64.go), and 0 is what
// Go itself answers on 386 and wasm; the managed host has no Go frame-pointer chain to report.
[TestClass]
public class RuntimeGetfpTests
{
    [TestMethod]
    public void GetfpAnswersGosNotImplementedZero()
    {
        Assert.AreEqual((nuint)0, runtime_package.GoGetfp().Value,
            "getfp answers 0, Go's documented 'not implemented' value -- never a CLR frame address");
    }

    [TestMethod]
    public void GetcallerfpTakesTheZeroArmAndDereferencesNothing()
    {
        // getcallerfp dereferences getfp's answer twice when it is non-zero; on 0 it returns 0, which
        // is the value entersyscall stores as syscallbp and every fp-traceback consumer stops on.
        Assert.AreEqual((nuint)0, runtime_package.GoGetcallerfp().Value);
    }
}
