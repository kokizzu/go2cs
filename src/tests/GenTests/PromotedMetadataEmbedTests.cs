// PromotedMetadataEmbedTests.cs - Gbtc
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
/// A `-tests` white-box test struct can EMBED a production type by pointer — net's
/// dnsclient_unix_test.go declares <c>resolvConfTest</c> over <c>*resolverConfig</c> — and under
/// the reference test model that embedded type is METADATA in the test compilation (the test
/// assembly references the production project; it never recompiles its sources). Go promotes the
/// embed's unexported fields AND methods there, because the test file IS the same Go package; the
/// friend grant (InternalsVisibleTo) is what projects that same-package visibility into C#. The
/// TypeGenerator's metadata fallback read only PUBLIC fields (correct for a genuine cross-package
/// embed, where Go itself hides unexported members) and had NO metadata method harvest at all, so
/// promotion did not happen and every promoted selection the converter emitted was a missing
/// member (net cgo-off Linux build: CS0117/CS1061/CS1929 ×8, one file, one type).
/// </summary>
/// <remarks>
/// These tests run the REAL TypeGenerator over the two-assembly shape. The discriminator under
/// test is Go's own promotion rule projected through existing metadata: a member is promoted when
/// it is accessible to this compilation AND (it is public, or the embedding struct's containing
/// class carries the same <c>[GoPackage]</c> identity as the embedded type's). Cross-package
/// method promotion stays converter territory (the explicit-hop emission in convSelectorExpr) —
/// the generator must NOT start minting cross-package forwarders.
/// </remarks>
[TestClass]
public class PromotedMetadataEmbedTests
{
    // The minimal production surface: the box, the attributes the generator matches by full name,
    // and the friend grant the real test model mints via InternalsVisibleTo. resolverConfig
    // mirrors net's: an INTERNAL struct with internal fields, an internal direct-ж (box) primary,
    // and internal [GoRecv]-style ref-receiver methods. Server is the cross-package control: a
    // public struct with a public and an internal field, and a public and an internal method.
    private const string ProductionSource =
        """
        using System.Runtime.CompilerServices;

        [assembly: InternalsVisibleTo("promo-test")]

        namespace go
        {
            public class ж<T> { public ж(T value) { } public T Value = default!; }

            public class GoTypeAttribute : System.Attribute
            {
                public GoTypeAttribute() { }
                public GoTypeAttribute(string definition) { }
            }

            public class GoPackageAttribute : System.Attribute
            {
                public GoPackageAttribute(string name) { }
            }

            [GoPackage("net")]
            public static partial class net_package
            {
                internal partial struct resolverConfig
                {
                    internal long lastChecked;
                    internal int initOnce;
                }

                public partial struct Server
                {
                    public int Port;
                    internal int secret;
                }

                internal static void init(this ж<resolverConfig> Ꮡconf) { }

                internal static bool tryAcquireSema(this ref resolverConfig conf) => true;

                internal static void releaseSema(this ref resolverConfig conf) { }

                // A method and a package-level FUNCTION sharing one name — legal in Go
                // (different scopes: net's LookupHost function vs (*Resolver).LookupHost).
                // The forwarder for the METHOD must be suppressed, or it shadows every bare
                // call of the FUNCTION inside the test class.
                internal static bool lookupColliding(this ref resolverConfig conf) => true;

                internal static bool lookupColliding(string host) => true;

                public static void ServePublic(this ref Server s) { }

                internal static void serveInternal(this ref Server s) { }
            }
        }
        """;

    // The white-box bridge class carries the SAME [GoPackage("net")] identity as production —
    // that is the same-Go-package statement. The external-test class is a DIFFERENT Go package
    // ("net_test"), so despite compiling in the same friend assembly its embeds promote only
    // what Go promotes cross-package: exported fields.
    private const string TestSource =
        """
        using go;

        namespace go
        {
            [GoPackage("net")]
            public static partial class net_internal_test_package
            {
                [GoType] internal partial struct resolvConfTest
                {
                    internal string dir;
                    internal partial ref ж<global::go.net_package.resolverConfig> resolverConfig { get; }
                }
            }

            [GoPackage("net_test")]
            public static partial class net_test_package
            {
                [GoType] internal partial struct serverProbe
                {
                    internal partial ref ж<global::go.net_package.Server> Server { get; }
                }
            }
        }
        """;

    private static IEnumerable<MetadataReference> CoreReferences()
    {
        string coreDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

        yield return MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        yield return MetadataReference.CreateFromFile(Path.Combine(coreDir, "System.Runtime.dll"));
    }

    private static Dictionary<string, string> RunTypeGeneratorOverFriendShape()
    {
        CSharpParseOptions parseOptions = new(LanguageVersion.Latest);
        CSharpCompilationOptions libraryOptions = new(OutputKind.DynamicallyLinkedLibrary);

        CSharpCompilation production = CSharpCompilation.Create(
            "promo-production",
            [CSharpSyntaxTree.ParseText(ProductionSource, parseOptions)],
            CoreReferences(),
            libraryOptions);

        using MemoryStream image = new();
        EmitResult emitted = production.Emit(image);
        Assert.IsTrue(emitted.Success, $"production compilation must emit clean: {string.Join("; ", emitted.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))}");
        image.Position = 0;

        CSharpCompilation test = CSharpCompilation.Create(
            "promo-test",
            [CSharpSyntaxTree.ParseText(TestSource, parseOptions)],
            CoreReferences().Append(MetadataReference.CreateFromImage(image.ToArray())),
            libraryOptions);

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new TypeGenerator());
        driver = driver.RunGenerators(test);

        return driver.GetRunResult().Results
            .SelectMany(generator => generator.GeneratedSources)
            .ToDictionary(source => source.HintName, source => source.SourceText.ToString());
    }

    private static string GeneratedFor(Dictionary<string, string> sources, string structName)
    {
        string? key = sources.Keys.FirstOrDefault(hint => hint.Contains(structName));
        Assert.IsNotNull(key, $"the TypeGenerator must generate for {structName}; got: {string.Join(", ", sources.Keys)}");
        return sources[key!];
    }

    [TestMethod]
    public void WhiteBoxEmbedOfProductionTypePromotesInternalFields()
    {
        string generated = GeneratedFor(RunTypeGeneratorOverFriendShape(), "resolvConfTest");

        // The promoted instance accessor and its static field-reference sibling — the exact
        // members the converter's emission binds (`conf.lastChecked = …`,
        // `conf.of(resolvConfTest.ᏑinitOnce)`).
        StringAssert.Contains(generated, "ref instance.resolverConfig.Value.lastChecked",
            "an internal field of the same-Go-package metadata embed must promote a Ꮡ field reference");
        StringAssert.Contains(generated, "lastChecked => ref resolverConfig.Value.lastChecked",
            "an internal field of the same-Go-package metadata embed must promote an instance accessor");
        StringAssert.Contains(generated, "ᏑinitOnce",
            "every promoted field carries its Ꮡ reference accessor");
    }

    [TestMethod]
    public void WhiteBoxEmbedOfProductionTypePromotesInternalMethods()
    {
        string generated = GeneratedFor(RunTypeGeneratorOverFriendShape(), "resolvConfTest");

        // The [GoRecv]-style ref-receiver methods forward through the deref'd embed hop; the
        // direct-ж (box) primary binds the embed's box itself, no `.Value`.
        StringAssert.Contains(generated, "tryAcquireSema(this ref resolvConfTest target",
            "an internal ref-receiver method of the same-Go-package metadata embed must promote a value forwarder");
        StringAssert.Contains(generated, "target.resolverConfig.Value.tryAcquireSema()",
            "the ref-receiver forwarder descends through the pointer embed's deref'd hop");
        StringAssert.Contains(generated, "releaseSema",
            "every accessible same-package method promotes");
        StringAssert.Contains(generated, "target.resolverConfig.init()",
            "a direct-ж (box) primary binds the embed's box, not the deref'd value");

        // The collision control: a promoted method whose name a package-level FUNCTION also
        // carries must NOT be minted — the forwarder would live in the test class and shadow
        // the `using static` import for every bare `lookupColliding(host)` call (net's 54
        // CS1501s on LookupHost/LookupIP/… when lookupCustomResolver embeds *Resolver).
        Assert.IsFalse(generated.Contains("lookupColliding"),
            "a forwarder colliding with a package-level function must be suppressed — it shadows the bare function call");
    }

    [TestMethod]
    public void CrossPackageEmbedStaysPublicFieldsOnlyAndMintsNoForwarders()
    {
        string generated = GeneratedFor(RunTypeGeneratorOverFriendShape(), "serverProbe");

        // The external-test class is Go package "net_test": Go promotes only Server's exported
        // members there, friend grant or not.
        StringAssert.Contains(generated, "ᏑPort",
            "an exported field of a cross-package metadata embed promotes (the pre-existing metadata path)");
        Assert.IsFalse(generated.Contains("secret"),
            "an unexported field must NOT promote across Go packages — the friend grant is not a Go visibility rule");

        // Method promotion across packages is converter territory (the explicit-hop emission);
        // the generator minting forwarders there would change every cross-package embed corpus-wide.
        Assert.IsFalse(generated.Contains("ServePublic"),
            "a public cross-package method must NOT gain a generated forwarder — the converter's explicit hop owns that call");
        Assert.IsFalse(generated.Contains("serveInternal"),
            "an unexported cross-package method is not promoted in Go at all");
    }
}
