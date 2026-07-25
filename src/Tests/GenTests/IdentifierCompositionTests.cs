// IdentifierCompositionTests.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static go2cs.Common;
using static go2cs.Symbols;

namespace go2cs.Tests;

/// <summary>
/// Pins go2cs-gen's identifier-composition rule: a generated member or type name COMPOSED from a Go
/// identifier must be built from the BARE name, and only a DECLARATION may carry the C# verbatim
/// escape. C#'s <c>@</c> prefix is legal only at the START of a token, so composing on an
/// already-escaped name silently emits text that does not parse.
/// </summary>
/// <remarks>
/// These cases are not behaviorally expressible today: the converter never names a lifted anonymous
/// interface with a bare Go identifier (it always suffixes <c>_type</c>), so the Δ-wrapper name can
/// only meet a keyword-named interface — database/sql's <c>decimal</c> — once named interfaces get
/// runtime shells (stage 2 of docs/Phase4/DESIGN-named-interface-wrappers.md). Validity is proved
/// with Roslyn's own parser rather than asserted by eye.
/// </remarks>
[TestClass]
public class IdentifierCompositionTests
{
    // True when `name` parses as the identifier of a field declaration — the exact position the
    // generator's composed s_-prefixed backing fields and Δ-prefixed wrapper types occupy.
    private static bool ParsesAsDeclaredName(string name)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText($"class C {{ private static readonly int {name}; }}");
        return !tree.GetDiagnostics().Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [TestMethod]
    public void EscapedNameIsUsableAsADeclarationButNotAsACompositionBase()
    {
        // The DECLARATION form of a keyword-named Go member is the escaped name, and it is valid.
        Assert.AreEqual("@private", EscapeCsKeyword("private"));
        Assert.IsTrue(ParsesAsDeclaredName("@private"));

        // Composing on that same escaped name is NOT valid — `@` cannot appear mid-token, so Roslyn
        // reads `s_` followed by a second identifier. This is the defect that corrupted the dyn
        // wrapper's class body (CS0102 on a phantom `s_` member).
        Assert.IsFalse(ParsesAsDeclaredName($"s_{EscapeCsKeyword("private")}ByPtr"));

        // Composing on the BARE name is valid.
        Assert.IsTrue(ParsesAsDeclaredName($"s_{GetUnsanitizedIdentifier(EscapeCsKeyword("private"))}ByPtr"));
    }

    [TestMethod]
    public void ShadowMarkerWrapperNameComposesOnTheBareInterfaceName()
    {
        // database/sql's `decimal` interface: the converter declares it escaped.
        const string Declared = "@decimal";

        Assert.AreEqual(Declared, EscapeCsKeyword("decimal"));

        // Δ + the escaped name is invalid …
        Assert.IsFalse(ParsesAsDeclaredName($"{ShadowVarMarker}{Declared}"));

        // … while Δ + the bare name parses, and needs no escape of its own: the marker prefix means
        // the composed name can never itself be a C# keyword.
        string composed = $"{ShadowVarMarker}{GetUnsanitizedIdentifier(Declared)}";

        Assert.AreEqual($"{ShadowVarMarker}decimal", composed);
        Assert.IsTrue(ParsesAsDeclaredName(composed));
        Assert.AreEqual(composed, EscapeCsKeyword(composed));
    }

    [TestMethod]
    public void MarkerPrefixedAccessorNamesComposeOnTheBareMemberName()
    {
        // The same rule for the Ꮡ box-field accessors and the ʗ-suffixed box fields — a keyword-named
        // Go field (`base`) is declared `@base`, so every composed accessor drops the escape.
        foreach (string composed in new[]
                 {
                     $"{AddressPrefix}{GetUnsanitizedIdentifier("@base")}",
                     $"{AddressPrefix}{CapturedVarMarker}{GetUnsanitizedIdentifier("@base")}",
                     $"{ShadowVarMarker}{GetUnsanitizedIdentifier("@base")}"
                 })
        {
            Assert.IsTrue(ParsesAsDeclaredName(composed), composed);
        }

        Assert.IsFalse(ParsesAsDeclaredName($"{AddressPrefix}@base"));
    }

    [TestMethod]
    public void BareIdentifierHelperIsANoOpForUnescapedNamesAndIsIdempotent()
    {
        Assert.AreEqual("Temporary", GetUnsanitizedIdentifier("Temporary"));
        Assert.AreEqual("private", GetUnsanitizedIdentifier("private"));
        Assert.AreEqual("private", GetUnsanitizedIdentifier(GetUnsanitizedIdentifier("@private")));

        // EscapeCsKeyword is likewise a no-op for a non-keyword and for an already-escaped name, so
        // the two helpers compose in either order without double-escaping.
        Assert.AreEqual("Temporary", EscapeCsKeyword("Temporary"));
        Assert.AreEqual("@private", EscapeCsKeyword("@private"));
    }
}
