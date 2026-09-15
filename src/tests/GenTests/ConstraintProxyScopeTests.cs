// ConstraintProxyScopeTests.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace go2cs.Tests;

/// <summary>
/// A self-referential constraint proxy is a TYPE ARGUMENT of whatever signature closes its constraint, so its accessibility
/// must follow the rule the interface adapters already use: public when both the element and the interface are public.
/// </summary>
/// <remarks>
/// EmitConstraintProxy used to declare every proxy <c>internal</c>. crypto/elliptic, the only package that used the proxy
/// before Go 1.24, never exposed it — its curves are unexported — so nothing noticed. Go 1.24's crypto/internal/fips140 ecdh
/// and ecdsa export <c>P224() *Curve[*P224Point]</c>, emitted <c>public static ж&lt;Curve&lt;P224PointжPoint&gt;&gt; P224()</c>
/// over an internal proxy: CS0050 at every curve constructor (RED 8). These tests run the REAL generator and read the scope
/// it gives each proxy, with elliptic's shape (a public element over an UNEXPORTED interface) as the arm that must stay internal.
/// </remarks>
[TestClass]
public class ConstraintProxyScopeTests
{
    private const string Source =
        """
        using go;

        [assembly: GoImplement<global::go.curves_package.PublicPoint, global::go.curves_package.Point<global::go.curves_package.PublicPoint>>(ConstraintProxy = true)]
        [assembly: GoImplement<global::go.curves_package.PublicPoint, global::go.curves_package.localPoint<global::go.curves_package.PublicPoint>>(ConstraintProxy = true)]
        [assembly: GoImplement<global::go.curves_package.hiddenPoint, global::go.curves_package.Point<global::go.curves_package.hiddenPoint>>(ConstraintProxy = true)]

        namespace go
        {
            public class ж<T> { public ж(T value) { } public T Value = default!; }

            public class GoImplementAttribute<TStruct, TInterface> : System.Attribute
            {
                public bool Promoted { get; set; }
                public bool Pointer { get; set; }
                public bool ConstraintProxy { get; set; }
            }

            public static partial class curves_package
            {
                public partial struct PublicPoint { public int X; }

                internal partial struct hiddenPoint { public int X; }

                public partial interface Point<T>
                {
                    T Add(T a, T b);
                }

                internal partial interface localPoint<T>
                {
                    T Add(T a, T b);
                }
            }
        }
        """;

    private static string GeneratedProxy(string proxyName)
    {
        string coreDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

        CSharpCompilation compilation = CSharpCompilation.Create(
            "constraint-proxy-scope",
            [CSharpSyntaxTree.ParseText(Source, new CSharpParseOptions(LanguageVersion.Latest))],
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(Path.Combine(coreDir, "System.Runtime.dll"))
            ],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ImplementGenerator()).RunGenerators(compilation);

        GeneratedSourceResult proxy = driver.GetRunResult().Results
            .SelectMany(generator => generator.GeneratedSources)
            .FirstOrDefault(source => source.HintName.Contains($"{proxyName}-proxy"));

        Assert.IsNotNull(proxy.SourceText, $"the constraint proxy {proxyName} must be generated");
        return proxy.SourceText.ToString();
    }

    [TestMethod]
    public void PublicElementOverPublicInterfaceYieldsPublicProxy()
    {
        StringAssert.Contains(GeneratedProxy("PublicPointжPoint"), "public sealed class PublicPointжPoint");
    }

    [TestMethod]
    public void UnexportedInterfaceKeepsTheProxyInternal()
    {
        // crypto/elliptic's shape: an exported element (nistec's P224Point) over an unexported constraint (nistPoint).
        StringAssert.Contains(GeneratedProxy("PublicPointжlocalPoint"), "internal sealed class PublicPointжlocalPoint");
    }

    [TestMethod]
    public void UnexportedElementKeepsTheProxyInternal()
    {
        StringAssert.Contains(GeneratedProxy("hiddenPointжPoint"), "internal sealed class hiddenPointжPoint");
    }
}
