using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

// Two package classes in the shape the converter emits for `example.com/dotted/v2` (package dotted):
// namespace `go.example.com` + class `dotted_package` -- one STAMPED with the verbatim path, as the
// converter now emits it, and one unstamped, which is what the namespace alone can say.
namespace go.example.com
{
    [GoPackage("dotted", ImportPath = "example.com/dotted/v2")]
    public static class dotted_package
    {
        public struct T { public long N; }

        public static long F() => 7;
    }

    [GoPackage("plain")]
    public static class plain_package
    {
        public struct T { public long N; }
    }
}

namespace GolibTests
{
    /// <summary>
    /// Every package-path decoder reads the stamped VERBATIM import path first
    /// (GoPackageAttribute.ImportPath): reflect's package path and the synthetic-PC function name
    /// agree with Go's for a package whose path the C# namespace cannot carry. The runtime's frame
    /// name (runtime.Caller / FuncForPC) is the third decoder; it is pinned end to end by the
    /// DottedModulePath behavioral test, because it is private to the runtime package.
    /// </summary>
    [TestClass]
    public class PackageImportPathStampTests
    {
        [TestMethod]
        public void ReflectReportsTheStampedPath()
        {
            Assert.AreEqual("example.com/dotted/v2", GoReflect.GoPackagePath(typeof(go.example.com.dotted_package.T)));
            Assert.AreEqual("example.com/dotted/v2", GoReflect.GoPackageClassPath(typeof(go.example.com.dotted_package)));
        }

        [TestMethod]
        public void TheSyntheticFunctionNameCarriesTheStampedPath()
        {
            Assert.AreEqual("example.com/dotted/v2.F",
                GoSyntheticPC.GoNameOf(typeof(go.example.com.dotted_package).GetMethod(nameof(go.example.com.dotted_package.F))!));
        }

        [TestMethod]
        public void AnUnstampedClassKeepsTheNamespaceDerivation()
        {
            // The control: without a stamp the decoder is exactly the old derivation -- which is why the
            // converter stamps every class whose path it cannot reproduce.
            Assert.AreEqual("example/com/plain", GoReflect.GoPackagePath(typeof(go.example.com.plain_package.T)));
        }
    }
}
