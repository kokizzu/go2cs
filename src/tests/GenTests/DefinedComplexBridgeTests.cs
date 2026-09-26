// DefinedComplexBridgeTests.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace go2cs.Tests;

/// <summary>
/// An untyped constant that meets a defined complex type (`type C128 complex128`) renders as a real
/// literal (`0D`, `0F`) or as a golib Untyped* wrapper, and neither reaches the wrapper in ONE
/// user-defined conversion. The TypeGenerator gives a wrapper over complex64/complex128 implicit
/// bridges from those values, INTO the wrapper only. These tests pin both halves of that scope: the
/// complex wrappers carry the bridges, and no other numeric wrapper gains them.
/// </summary>
[TestClass]
public class DefinedComplexBridgeTests
{
    private const string Source =
        """
        namespace go
        {
            public class GoTypeAttribute : System.Attribute
            {
                public GoTypeAttribute() { }
                public GoTypeAttribute(string definition) { }
            }

            partial class demo_package
            {
                [GoType("num:complex128")] partial struct C128;

                [GoType("num:complex64")] partial struct C64;

                [GoType("num:float64")] partial struct F64;

                [GoType("num:float32")] partial struct F32;

                [GoType("num:int32")] partial struct I32;
            }
        }
        """;

    // The source types the complex bridge converts FROM: the real literal's type and the golib
    // wrappers a named untyped const renders as
    private static readonly string[] BridgeSources = ["float64", "float32", "UntypedInt", "UntypedFloat", "UntypedComplex"];

    private static readonly Regex ImplicitOperator = new(@"implicit operator ([\w.]+)\(([\w.]+) value\)");

    private static Dictionary<string, string> RunTypeGenerator()
    {
        string coreDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

        CSharpCompilation compilation = CSharpCompilation.Create("bridge-test",
            [CSharpSyntaxTree.ParseText(Source, new CSharpParseOptions(LanguageVersion.Latest))],
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(Path.Combine(coreDir, "System.Runtime.dll"))
            ],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new TypeGenerator());
        driver = driver.RunGenerators(compilation);

        return driver.GetRunResult().Results
            .SelectMany(generator => generator.GeneratedSources)
            .ToDictionary(source => source.HintName, source => source.SourceText.ToString());
    }

    // Every implicit operator of the wrapper, as (returned type, parameter type)
    private static List<(string to, string from)> ImplicitOperatorsOf(Dictionary<string, string> sources, string wrapper)
    {
        string? key = sources.Keys.FirstOrDefault(hint => hint.Contains($".{wrapper}."));
        Assert.IsNotNull(key, $"the TypeGenerator must generate {wrapper}; got: {string.Join(", ", sources.Keys)}");

        return ImplicitOperator.Matches(sources[key!])
            .Select(match => (match.Groups[1].Value, match.Groups[2].Value))
            .ToList();
    }

    [TestMethod]
    public void ComplexWrappersBridgeFromRealAndUntypedValues()
    {
        Dictionary<string, string> sources = RunTypeGenerator();

        (string wrapper, string real)[] cases = [("C128", "float64"), ("C64", "float32")];

        foreach ((string wrapper, string real) in cases)
        {
            List<(string to, string from)> operators = ImplicitOperatorsOf(sources, wrapper);

            foreach (string from in new[] { real, "UntypedInt", "UntypedFloat", "UntypedComplex" })
                Assert.IsTrue(operators.Contains((wrapper, from)), $"{wrapper} must convert implicitly FROM {from}");

            // INTO the wrapper only: nothing converts the wrapper out to a bridge source
            foreach (string source in BridgeSources)
                Assert.IsFalse(operators.Contains((source, wrapper)), $"{wrapper} must not convert implicitly TO {source}");
        }

        // complex64 takes only its float32 component: a double literal never renders in its context
        Assert.IsFalse(ImplicitOperatorsOf(sources, "C64").Contains(("C64", "float64")), "C64 must not convert implicitly from float64");
    }

    [TestMethod]
    public void NonComplexWrappersGainNoComplexBridge()
    {
        Dictionary<string, string> sources = RunTypeGenerator();

        foreach (string wrapper in new[] { "F64", "F32", "I32" })
        {
            List<(string to, string from)> operators = ImplicitOperatorsOf(sources, wrapper);

            Assert.IsFalse(operators.Contains((wrapper, "UntypedComplex")), $"{wrapper} must not convert implicitly from UntypedComplex");

            // a float wrapper keeps exactly the underlying pair: no bridge from an Untyped* wrapper
            if (wrapper != "I32")
            {
                Assert.IsFalse(operators.Contains((wrapper, "UntypedInt")), $"{wrapper} must not convert implicitly from UntypedInt");
                Assert.IsFalse(operators.Contains((wrapper, "UntypedFloat")), $"{wrapper} must not convert implicitly from UntypedFloat");
            }
        }

        // the controls saw real operators, so the absences above are not a failed match
        Assert.IsTrue(ImplicitOperatorsOf(sources, "F64").Contains(("F64", "float64")), "F64 keeps its underlying conversion");
        Assert.IsTrue(ImplicitOperatorsOf(sources, "I32").Contains(("I32", "UntypedInt")), "I32 keeps its UntypedInt bridge");
    }
}
