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
}
