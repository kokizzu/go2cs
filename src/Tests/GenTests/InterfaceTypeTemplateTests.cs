// InterfaceTypeTemplateTests.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System.Collections.Generic;
using System.Linq;
using go2cs.Templates.InterfaceType;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static go2cs.Symbols;

namespace go2cs.Tests;

/// <summary>
/// Pins the dyn-interface runtime wrapper's emitted identifiers. These shapes are reachable only
/// through a keyword-named Go interface or member, which the behavioral corpus can exercise for
/// METHODS but not for the interface NAME — the converter always suffixes a lifted anonymous
/// interface with <c>_type</c>, so a Δ wrapper over a verbatim interface name (database/sql's
/// <c>decimal</c>) can only occur once named interfaces get runtime shells (stage 2 of
/// docs/Phase4/DESIGN-named-interface-wrappers.md).
/// </summary>
[TestClass]
public class InterfaceTypeTemplateTests
{
    private static MethodInfo Method(string name, string returnType, params (string type, string name)[] parameters) =>
        new()
        {
            Name = name,
            ReturnType = returnType,
            Parameters = parameters,
            GenericTypes = "",
            TypeConstraints = new Dictionary<string, string[]>()
        };

    private static string GenerateDyn(string interfaceName, params MethodInfo[] methods) =>
        new InterfaceTypeTemplate
        {
            PackageNamespace = "go",
            PackageName = "probe",
            Scope = "internal",
            InterfaceName = interfaceName,
            OperatorConstraints = [],
            Methods = methods,
            Dynamic = true,
            UsingStatements = []
        }
        .Generate();

    // The generated wrapper references types this test assembly does not have (ж<>,
    // IInterfaceAdapter, NilType), so only SYNTAX is asserted — which is exactly what a mid-token
    // '@' breaks.
    private static void AssertParses(string source)
    {
        Diagnostic[] errors = CSharpSyntaxTree.ParseText(source)
            .GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();

        Assert.AreEqual(0, errors.Length, string.Join("\r\n", errors.Select(error => error.ToString())));
    }

    [TestMethod]
    public void KeywordNamedInterfaceComposesABareShadowMarkerWrapperName()
    {
        // database/sql's `decimal`: the converter declares the interface escaped.
        string generated = GenerateDyn("@decimal", Method("Scan", "void"));

        AssertParses(generated);

        // The interface DECLARATION keeps the escape; the composed wrapper name drops it.
        StringAssert.Contains(generated, "partial interface @decimal");
        StringAssert.Contains(generated, $"class {ShadowVarMarker}decimal<{ShadowVarMarker}TTarget>");
        Assert.IsFalse(generated.Contains($"{ShadowVarMarker}@decimal"), $"composed the invalid '{ShadowVarMarker}@decimal'");
    }

    [TestMethod]
    public void KeywordNamedMethodComposesBareDelegateAndFieldNames()
    {
        // testing.TB's sealing method, declared DIRECTLY on the interface: syntax-sourced names
        // arrive already escaped.
        string generated = GenerateDyn("probe_type", Method("@private", "void"), Method("Label", "global::go.@string"));

        AssertParses(generated);

        // Declaration keeps the escape, and nameof() needs it too.
        StringAssert.Contains(generated, "public void @private()");
        StringAssert.Contains(generated, "nameof(@private)");

        // Every COMPOSED name drops it — `s_@privateByPtr` is not one token.
        foreach (string composed in new[] { "privateByPtr", "privateByVal", "s_privateByPtr", "s_privateByVal" })
            StringAssert.Contains(generated, composed);

        Assert.IsFalse(generated.Contains("s_@private"), "composed the invalid 's_@privateByPtr'");
    }

    [TestMethod]
    public void ForwardingLocalNeverShadowsAMethodParameter()
    {
        // net/http.Pusher's real shape: a parameter literally named `target`, the same name the
        // forwarding body gave its receiver local (CS0136) — and the by-value call then handed the
        // RECEIVER to the parameter's slot (CS1503). Neither is a syntax error, so this is asserted
        // on the emitted text.
        string generated = GenerateDyn("probe_type",
            Method("Push", "global::go.error", ("global::go.@string", "target"), ("nint", "weight")));

        AssertParses(generated);

        string receiver = $"target{CapturedVarMarker}";

        // The receiver local is marker-suffixed, so it cannot collide with any converted Go parameter.
        StringAssert.Contains(generated, $"{ShadowVarMarker}TTarget {receiver} = m_target;");
        StringAssert.Contains(generated, $"{receiver} = m_target_ptr.Value;");

        // Receiver first, then the method's OWN parameters — never the receiver twice.
        StringAssert.Contains(generated, $"s_PushByVal!({receiver}, target, weight)");
        StringAssert.Contains(generated, "s_PushByPtr!(m_target_ptr!, target, weight)");
        Assert.IsFalse(generated.Contains("s_PushByVal!(target, target"), "forwarded the receiver in the parameter's slot");
    }

    [TestMethod]
    public void VoidForwarderDispatchesExactlyOneArm()
    {
        // A void forwarder has no `return` to leave the method on, so the two dispatch arms must be
        // if/else — otherwise the by-value arm ran and fell through into the by-pointer arm,
        // dereferencing a null delegate.
        string generated = GenerateDyn("probe_type", Method("Reset", "void"));

        AssertParses(generated);
        StringAssert.Contains(generated, "s_ResetByVal!(");
        StringAssert.Contains(generated, "else");

        // One `else` per forwarder, immediately before the by-pointer call.
        int elseIndex = generated.IndexOf("else", generated.IndexOf("s_ResetByVal!("), System.StringComparison.Ordinal);

        Assert.IsTrue(elseIndex > 0 && elseIndex < generated.IndexOf("s_ResetByPtr!(", System.StringComparison.Ordinal),
            "the by-pointer arm is not guarded by an else");
    }
}
