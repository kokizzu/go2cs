// PartialStubGeneratorTests.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static go2cs.Symbols;

namespace go2cs.Tests;

/// <summary>
/// Pins the PartialStubGenerator's scope: it stubs bodyless partial methods (Go asm/cgo
/// functions) but must NEVER stub the -tests package-init hook — that classic partial method is
/// DESIGNED to erase when unimplemented (the production assembly excludes the test-side
/// implementation), and a throwing stub detonates the package class's static constructor for
/// every consumer of the production assembly (encoding/gob's ctor took down go/token's whole
/// serialize path before this rule).
/// </summary>
[TestClass]
public class PartialStubGeneratorTests
{
    private static GeneratorDriverRunResult RunGenerator(string source)
    {
        CSharpCompilation compilation = CSharpCompilation.Create("test",
            [CSharpSyntaxTree.ParseText(source)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new PartialStubGenerator());
        driver = driver.RunGenerators(compilation);
        return driver.GetRunResult();
    }

    [TestMethod]
    public void StubsAsmPartialsButNeverTheTestInitHook()
    {
        string source = $$"""
            namespace go;

            partial class demo_package
            {
                // The -tests package-init hook: erased by C# when unimplemented — never stubbed.
                static partial void {{PackageTestInitHookMethod}}();

                // An asm/cgo function emission: stubbed when no implementation part exists.
                internal static partial void realAsmFunc();
            }
            """;

        GeneratorDriverRunResult result = RunGenerator(source);
        string generated = string.Join("\n", result.GeneratedTrees.Select(tree => tree.ToString()));

        StringAssert.Contains(generated, "realAsmFunc", "the asm partial must receive a throwing stub");
        Assert.IsFalse(generated.Contains(PackageTestInitHookMethod),
            "the -tests package-init hook must never be stubbed — an unimplemented hook is erased by design");
    }

    /// <summary>
    /// The stub carries [go.GoExternalStub], which is the ONLY runtime evidence that nothing in the
    /// compilation implements a method.
    /// </summary>
    /// <remarks>
    /// internal/abi's FuncPCABI0/FuncPCABIInternal are handed a method group and must answer either
    /// a synthetic PC or a loud refusal, and neither available proxy decides that: a bodyless partial
    /// DECLARATION covers Go assembly routines and darwin's dylib trampolines alike, and
    /// [GeneratedCode] is minted once per generator in this analyzer, so RecvGenerator's ж-overloads
    /// carry it too — runtime/time.cs passes exactly one of those, (*timers).run, a real function
    /// with a real body. This generator is the exact oracle because of the two skips above it: a
    /// partial another generator implements, and one a hand-written *_impl.cs supplies, are never
    /// stubbed. See docs/phase4/DESIGN-synthetic-pc-registry.md.
    /// </remarks>
    [TestMethod]
    public void TheStubCarriesTheExternalStubMarker()
    {
        string source = $$"""
            namespace go;

            partial class demo_package
            {
                static partial void {{PackageTestInitHookMethod}}();

                internal static partial void realAsmFunc();
            }
            """;

        string generated = string.Join("\n", RunGenerator(source).GeneratedTrees
            .Select(tree => tree.ToString())
            .Where(text => text.Contains("realAsmFunc")));

        StringAssert.Contains(generated, "[global::go.GoExternalStub]",
            "the stub must be marked, or FuncPCABI0 cannot tell an assembly function from a converted one");
        Assert.AreNotEqual("", generated, "the asm partial must have produced a stub at all");
    }

    /// <summary>
    /// The stub's message states only what the generator ESTABLISHED -- that nothing in this
    /// compilation implements the method -- and never asserts WHY.
    /// </summary>
    /// <remarks>
    /// Until 2026-09-06 the message read "external (assembly or cgo) function is not implemented",
    /// asserting a cause the generator never determines: it sees ONE compilation, so whether a body
    /// exists elsewhere in the corpus is not a question available to it. Two symbols with real managed
    /// bodies one package over carried text asserting they were assembly -- runtime/pprof's
    /// pprof_mutexProfileInternal (bodied in runtime/mprof.cs) and readProfile (provided by
    /// runtime/cpuprof.cs through a linkname) -- and net/http/pprof's blocker was misclassified three
    /// times by readers taking that sentence at face value. The NEGATIVE assertion is the load-bearing
    /// half: the message may list candidate causes, but must never state one as fact.
    /// </remarks>
    [TestMethod]
    public void TheStubMessageClaimsOnlyWhatTheGeneratorKnows()
    {
        string source = $$"""
            namespace go;

            partial class demo_package
            {
                internal static partial void realAsmFunc();
            }
            """;

        string generated = string.Join("\n", RunGenerator(source).GeneratedTrees
            .Select(tree => tree.ToString())
            .Where(text => text.Contains("realAsmFunc")));

        Assert.AreNotEqual("", generated, "the asm partial must have produced a stub at all");

        StringAssert.Contains(generated, "no implementation reached this compilation",
            "the message must state the fact the generator established");

        Assert.IsFalse(generated.Contains("external (assembly or cgo) function is not implemented"),
            "the message must not assert a CAUSE the generator never determined -- it sees one " +
            "compilation and cannot know whether a body exists elsewhere in the corpus");
    }
}
