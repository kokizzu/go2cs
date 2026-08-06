// SymbolParameterInfoTests.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace go2cs.Tests;

/// <summary>
/// Pins the escaping of parameter names read from SYMBOLS. Every generator that binds an
/// interface's members (ImplementGenerator's adapter and explicit-impl paths, MethodInfo's
/// <c>IMethodSymbol</c> overload) reads <c>IParameterSymbol.Name</c>, which — unlike the syntax
/// path's <c>Identifier.Text</c> — arrives with the converter's <c>@</c> escape STRIPPED.
/// </summary>
[TestClass]
public class SymbolParameterInfoTests
{
    // Builds a real compilation so the parameters arrive as SYMBOLS — the exact path the generators
    // take when they enumerate an interface's members through AllInterfaces/GetMembers.
    private static IMethodSymbol MethodSymbol(string source, string typeName, string methodName)
    {
        CSharpCompilation compilation = CSharpCompilation.Create("probe",
            [CSharpSyntaxTree.ParseText(source)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        INamedTypeSymbol type = compilation.GetTypeByMetadataName(typeName) ??
            throw new MissingMemberException($"probe type '{typeName}' not found");

        return type.GetMembers(methodName).OfType<IMethodSymbol>().Single();
    }

    private static void AssertParses(string source)
    {
        Diagnostic[] errors = CSharpSyntaxTree.ParseText(source)
            .GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();

        Assert.AreEqual(0, errors.Length, string.Join("\r\n", errors.Select(error => error.ToString())));
    }

    [TestMethod]
    public void KeywordNamedSymbolParametersAreEscaped()
    {
        // sync.Map's `CompareAndSwap(key, old, new any)`: the converter emits the interface member
        // with `any @new`, but the generator re-read the parameter from the SYMBOL, where the name
        // is the bare keyword. The generated explicit impl rendered `object new`, which does not
        // lex as a parameter — the recovery diagnostic was CS0501 ("must declare a body") on the
        // enclosing member, three times over in sync's test build.
        IMethodSymbol method = MethodSymbol(
            "public interface I { bool CompareAndSwap(object key, object old, object @new, object @base, object @lock); }",
            "I", "CompareAndSwap");

        (string type, string name)[] parameters = method.Parameters.ToParameterInfos();

        CollectionAssert.AreEqual(new[] { "key", "old", "@new", "@base", "@lock" },
            parameters.Select(parameter => parameter.name).ToArray());
    }

    [TestMethod]
    public void KeywordNamedSymbolParametersRenderParseableMembers()
    {
        // The failure is a PARSE failure, so assert on the rendered member, not just the name.
        IMethodSymbol method = MethodSymbol(
            "public interface I { bool CompareAndSwap(object key, object old, object @new); }",
            "I", "CompareAndSwap");

        MethodInfo info = method.GetMethodInfo();

        // Both the DECLARATION (typed parameters) and the forwarding CALL (argument list) — the two
        // places every adapter/impl template renders these names — must lex.
        AssertParses($"class Probe {{ static bool Sink(object a, object b, object c) => true;" +
            $" public bool {info.GetSignature()} => Sink({info.CallParameters}); }}");

        StringAssert.Contains(info.GetTypedParameters(true), "@new");
        StringAssert.Contains(info.CallParameters, "@new");
    }

    [TestMethod]
    public void RefKindIsCarriedAndNonKeywordNamesAreUntouched()
    {
        // The ref-kind prefix (needed so an explicit impl matches its interface member, CS0539)
        // composes with the escape, and an ordinary name is passed through verbatim.
        IMethodSymbol method = MethodSymbol(
            "public interface I { void Probe(in int data, ref object @ref, out bool ok); }",
            "I", "Probe");

        (string type, string name)[] parameters = method.Parameters.ToParameterInfos(withRefKind: true);

        CollectionAssert.AreEqual(new[] { "in int", "ref object", "out bool" },
            parameters.Select(parameter => parameter.type).ToArray());

        CollectionAssert.AreEqual(new[] { "data", "@ref", "ok" },
            parameters.Select(parameter => parameter.name).ToArray());

        // Without withRefKind the modifier is absent (MethodInfo's symbol overload's shape).
        CollectionAssert.AreEqual(new[] { "int", "object", "bool" },
            method.Parameters.ToParameterInfos().Select(parameter => parameter.type).ToArray());
    }
}
