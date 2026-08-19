// CollisionRenamedForwardTests.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System.Collections.Generic;
using go2cs.Templates.InterfaceImpl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static go2cs.Common;

namespace go2cs.Tests;

/// <summary>
/// An interface member is IMPLEMENTED under the interface's name and FORWARDS under the name the
/// converter EMITTED for the implementation. Those are the same name almost everywhere, and part
/// company when the collision pass Δ-renames a declarator whose bare name would hijack a same-named
/// member the file carries via <c>using static</c> — which a `-tests` whitebox compilation does to
/// <c>flag_test.go</c>'s five <c>flag.Value</c> types, each declaring <c>String</c>/<c>Set</c>
/// against the production <c>flag</c> package the test variant dot-imports.
/// </summary>
/// <remarks>
/// The adapter had no notion of the distinction and spelled the interface's name at both positions,
/// so the forward bound nothing on the box: <c>flag</c>'s whole 24-verdict suite sat behind CS1929
/// ×10, with the compiler naming an unrelated <c>bytes_package.String</c> as its nearest candidate.
/// The resolution reads the methods the struct actually DECLARES — exact name first, the Δ-projection
/// only as a second pass — which is the same two-pass rule golib's shell binder and method-set probe
/// already run at run time (<c>TypeExtensions.GoMethodNameMatches</c>), so the compile-time adapter
/// cannot disagree with them about one name.
/// </remarks>
[TestClass]
public class CollisionRenamedForwardTests
{
    private static MethodInfo Member(string interfaceName, string memberName, IReadOnlyCollection<string> declaredNames, params (string type, string name)[] parameters) =>
        new()
        {
            Name = $"global::go.{interfaceName}.{memberName}",
            ForwardName = ResolveForwardMemberName(memberName, (ICollection<string>)declaredNames),
            ReturnType = "global::go.@string",
            Parameters = parameters,
            GenericTypes = "",
            TypeConstraints = []
        };

    [TestMethod]
    public void ExactDeclaredNameWinsOverTheProjection()
    {
        // A struct that declares the interface member under its own name forwards to it verbatim —
        // this is the whole production corpus, and the reason the fix is inert there.
        Assert.IsNull(ResolveForwardMemberName("String", new[] { "String", "Set" }));

        // Exact-first also protects a genuinely Δ-named Go method: `Δ` is a Unicode letter and so a
        // legal Go identifier character. A struct declaring BOTH must never have its exact match
        // displaced by the projection of the other.
        Assert.IsNull(ResolveForwardMemberName("String", new[] { "String", "ΔString" }));
    }

    [TestMethod]
    public void RenamedDeclarationIsResolvedThroughTheMarker()
    {
        // flag_test.go's shape: the declarator emitted Δ-renamed, so the forward must follow it.
        Assert.AreEqual("ΔString", ResolveForwardMemberName("String", new[] { "ΔString", "ΔSet", "IsBoolFlag" }));
        Assert.AreEqual("ΔSet", ResolveForwardMemberName("Set", new[] { "ΔString", "ΔSet", "IsBoolFlag" }));
    }

    [TestMethod]
    public void AnUndeclaredMemberResolvesToNothing()
    {
        // A member satisfied by promotion or by an embed hop declares neither spelling; leaving it
        // null is what keeps the hop machinery reading the interface member's own name.
        Assert.IsNull(ResolveForwardMemberName("Read", new[] { "Write", "Close" }));
    }

    [TestMethod]
    public void PointerAdapterImplementsTheInterfaceNameAndForwardsTheEmittedOne()
    {
        // The exact shape that failed: boolFlagVar's `[GoRecv] this ref` primaries emit ΔString/ΔSet
        // and bind the box through their RecvGenerator ж-twins.
        string[] declared = ["ΔString", "ΔSet"];

        AdapterImplTemplate template = new()
        {
            PackageNamespace = "go",
            PackageName = "flag_test",
            StructName = "boolFlagVar",
            InterfaceName = "global::go.flag_package.Value",
            AdapterName = "boolFlagVarжValue",
            AdapterScope = "internal",
            Methods =
            [
                Member("fmt_package.Stringer", "String", declared),
                Member("flag_package.Value", "Set", declared, ("global::go.@string", "_"))
            ],
            ForwardReceivers = new Dictionary<string, string> { ["ΔString"] = "m_box", ["ΔSet"] = "m_box" }
        };

        string generated = template.Generate();

        // The member being implemented carries the INTERFACE's name and must never be renamed.
        StringAssert.Contains(generated, "global::go.fmt_package.Stringer.String()");
        StringAssert.Contains(generated, "global::go.flag_package.Value.Set(");

        // ...and forwards to the name the converter actually emitted.
        StringAssert.Contains(generated, "=> m_box.ΔString()");
        StringAssert.Contains(generated, "=> m_box.ΔSet(_)");

        // The pre-fix emission, which bound nothing on the box.
        Assert.IsFalse(generated.Contains("=> m_box.String()"), "the forward must not spell the Go name");
        Assert.IsFalse(generated.Contains("=> m_box.Set(_)"), "the forward must not spell the Go name");
    }

    [TestMethod]
    public void RenamedValueReceiverStillResolvesItsReceiverExpression()
    {
        // URLValue's `ΔString(this URLValue v)` is a VALUE receiver with no ж-twin, so it needs
        // `m_box.Value`. ForwardReceivers is keyed by the DECLARED name, so a Go-name lookup missed
        // and fell through to the `m_box` default — stranding the call at CS1929 even once the call
        // TARGET was right. Both positions must read the resolved name.
        string[] declared = ["ΔString"];

        AdapterImplTemplate template = new()
        {
            PackageNamespace = "go",
            PackageName = "flag_test",
            StructName = "URLValue",
            InterfaceName = "global::go.flag_package.Value",
            AdapterName = "URLValueжValue",
            AdapterScope = "internal",
            Methods = [Member("fmt_package.Stringer", "String", declared)],
            ForwardReceivers = new Dictionary<string, string> { ["ΔString"] = "m_box.Value" }
        };

        StringAssert.Contains(template.Generate(), "=> m_box.Value.ΔString()");
    }

    [TestMethod]
    public void AnUnrenamedAdapterIsUntouched()
    {
        // Inertness, stated as a test: with nothing renamed the emission is exactly what it was
        // before the resolution existed. This is what the whole production corpus renders.
        string[] declared = ["String"];

        AdapterImplTemplate template = new()
        {
            PackageNamespace = "go",
            PackageName = "flag_test",
            StructName = "plainValue",
            InterfaceName = "global::go.flag_package.Value",
            AdapterName = "plainValueжValue",
            AdapterScope = "internal",
            Methods = [Member("fmt_package.Stringer", "String", declared)],
            ForwardReceivers = new Dictionary<string, string> { ["String"] = "m_box" }
        };

        StringAssert.Contains(template.Generate(), "=> m_box.String()");
    }
}
