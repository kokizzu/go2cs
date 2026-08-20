// WhiteboxBridgeAdapterTests.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace go2cs.Tests;

/// <summary>
/// A `-tests` white-box bridge can declare the very methods that satisfy an interface for a
/// PRODUCTION type — Go lets a package's test files add methods to its production types, and the
/// reference test model splits those across an assembly boundary: crypto/tls's
/// handshake_messages_test.go declares <c>marshal</c>/<c>unmarshal</c> for <c>*SessionState</c>,
/// whose <c>handshakeMessage</c> satisfaction exists ONLY through them. The generated pointer
/// adapter must forward those members to the bridge extensions (the ref form through its
/// RecvGenerator ж-twin), not classify them as cross-assembly package-sealing markers.
/// </summary>
/// <remarks>
/// The marker classification requires "no local implementation to forward to", but its evidence
/// set was built from the struct's own declaration syntax — which a production type referenced
/// from a test compilation does not have — so a bridge-implemented member read as a marker and the
/// adapter COMPILED with <c>=> default!</c> stubs: marshal answered an empty buffer with nil
/// error, unmarshal answered false, and crypto/tls's TestMarshalUnmarshal reported
/// "failed to unmarshal" with no diagnostic anywhere (the flagship row's 2026-08-19 regression,
/// entering with the local-iface-cast merge that flipped tls off the recompile fallback). These
/// tests run the REAL generator over the two-assembly shape, so the classification and the
/// forwarding are guarded together.
/// </remarks>
[TestClass]
public class WhiteboxBridgeAdapterTests
{
    // The minimal golib surface both compilations need: the box, the containers, the attribute the
    // generator matches by FULL NAME (go.GoImplementAttribute<TStruct, TInterface>), and the friend
    // grant that makes the production internals reachable — exactly the grant the real test model
    // mints via InternalsVisibleTo.
    private const string ProductionSource =
        """
        using System.Runtime.CompilerServices;

        [assembly: InternalsVisibleTo("whitebox-test")]

        namespace go
        {
            public class ж<T> { public ж(T value) { } public T Value = default!; }
            public interface error { }
            public class slice<T> { }

            public class GoImplementAttribute<TStruct, TInterface> : System.Attribute
            {
                public bool Promoted { get; set; }
                public bool Pointer { get; set; }
                public bool ConstraintProxy { get; set; }
            }
        }

        namespace go.crypto
        {
            public static partial class tls_package
            {
                public partial struct SessionState
                {
                    public int Dummy;
                }

                internal partial interface handshakeMessage
                {
                    (go.slice<byte>, go.error) marshal();
                    bool unmarshal(go.slice<byte> b);
                }
            }
        }
        """;

    // The bridge: the box-receiver form the converter emits directly, and the [GoRecv]-style ref
    // form whose RecvGenerator ж-twin is what the adapter's `m_box.unmarshal(…)` forward binds.
    private const string TestSource =
        """
        using go;

        [assembly: GoImplement<global::go.crypto.tls_package.SessionState, global::go.crypto.tls_package.handshakeMessage>(Pointer = true)]

        namespace go.crypto;

        public static partial class tls_test_package
        {
        }

        public static partial class tls_internal_test_package
        {
            internal static (go.slice<byte>, go.error) marshal(this ж<global::go.crypto.tls_package.SessionState> Ꮡs) => default;

            internal static bool unmarshal(this ref global::go.crypto.tls_package.SessionState s, go.slice<byte> b) => default;
        }
        """;

    private static IEnumerable<MetadataReference> CoreReferences()
    {
        string coreDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

        yield return MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        yield return MetadataReference.CreateFromFile(Path.Combine(coreDir, "System.Runtime.dll"));
    }

    private static string RunImplementGeneratorOverWhiteboxShape()
    {
        CSharpParseOptions parseOptions = new(LanguageVersion.Latest);
        CSharpCompilationOptions libraryOptions = new(OutputKind.DynamicallyLinkedLibrary);

        CSharpCompilation production = CSharpCompilation.Create(
            "whitebox-production",
            [CSharpSyntaxTree.ParseText(ProductionSource, parseOptions)],
            CoreReferences(),
            libraryOptions);

        using MemoryStream image = new();
        EmitResult emitted = production.Emit(image);
        Assert.IsTrue(emitted.Success, $"production compilation must emit clean: {string.Join("; ", emitted.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))}");
        image.Position = 0;

        CSharpCompilation test = CSharpCompilation.Create(
            "whitebox-test",
            [CSharpSyntaxTree.ParseText(TestSource, parseOptions)],
            CoreReferences().Append(MetadataReference.CreateFromImage(image.ToArray())),
            libraryOptions);

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ImplementGenerator());
        driver = driver.RunGenerators(test);

        GeneratorDriverRunResult result = driver.GetRunResult();
        GeneratedSourceResult adapterSource = result.Results
            .SelectMany(generator => generator.GeneratedSources)
            .FirstOrDefault(source => source.HintName.Contains("SessionState") && source.HintName.Contains("handshakeMessage"));

        Assert.IsNotNull(adapterSource.SourceText, "the pointer adapter for (SessionState, handshakeMessage) must be generated");
        return adapterSource.SourceText.ToString();
    }

    [TestMethod]
    public void BridgeImplementedMembersForwardInsteadOfStubbing()
    {
        string adapter = RunImplementGeneratorOverWhiteboxShape();

        // The direct-ж bridge form binds the box; the ref form binds the box through its
        // RecvGenerator ж-twin. Neither is a package-sealing marker.
        StringAssert.Contains(adapter, "=> m_box.marshal()");
        StringAssert.Contains(adapter, "=> m_box.unmarshal(b)");

        // The pre-fix emission: a required member satisfied with a silent default.
        Assert.IsFalse(adapter.Contains("default!"),
            "a bridge-implemented member must never be stubbed — the stub compiles and silently answers defaults");
    }
}
