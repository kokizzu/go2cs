// FriendBridgeBoxReceiverTests.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace go2cs.Tests;

/// <summary>
/// A `-tests` white-box friend bridge declares pointer-receiver methods on a PRODUCTION struct whose
/// declaration is metadata-only in the test compilation (sync's export_test.go on `*poolDequeue`).
/// Those direct-ж primaries bind the BOX, so the pointer-interface adapter must forward
/// <c>m_box.M(…)</c>, not <c>m_box.Value.M(…)</c> — and finding them cannot depend on one fixed
/// spelling of the receiver's type, because the bridge qualifies it however its own file needs.
/// </summary>
[TestClass]
public class FriendBridgeBoxReceiverTests
{
    private static Compilation Compile(string source) =>
        CSharpCompilation.Create("bridge", [CSharpSyntaxTree.ParseText(source)]);

    [TestMethod]
    public void QualifiedBoxParameterStillMatchesTheProductionReceiver()
    {
        // The shape sync emits once `internal/testenv` pulls `go/build` into the closure and the
        // root namespace is shadowed: the bridge spells the receiver fully qualified.
        Compilation compilation = Compile(
            """
            partial class sync_internal_test_package
            {
                internal static bool PushHead(this ж<global::go.sync_package.poolChain> Ꮡc, any val) => true;
                internal static bool Unrelated(this ж<global::go.sync_package.poolDequeue> Ꮡd) => true;
            }
            """);

        HashSet<string> found = StructDeclarationSyntaxExtensions.GetBoxReceiverMethodNamesBySimpleName("poolChain", compilation);

        Assert.IsTrue(found.Contains("PushHead"), "the qualified ж parameter names poolChain");
        Assert.IsFalse(found.Contains("Unrelated"), "a different receiver type must not match");
    }

    [TestMethod]
    public void AliasedAndBareBoxParametersMatchToo()
    {
        // strings' bridge spells its box through the imported alias; a same-assembly bridge spells
        // it bare. Both are the same receiver.
        Compilation compilation = Compile(
            """
            partial class strings_internal_test_package
            {
                internal static void Reset(this ж<Replacer> Ꮡr) { }
                internal static void Bare(this ж<Builder> Ꮡb) { }
            }
            """);

        Assert.IsTrue(StructDeclarationSyntaxExtensions.GetBoxReceiverMethodNamesBySimpleName("Replacer", compilation).Contains("Reset"));
        Assert.IsTrue(StructDeclarationSyntaxExtensions.GetBoxReceiverMethodNamesBySimpleName("Builder", compilation).Contains("Bare"));
    }

    [TestMethod]
    public void ValueAndRefReceiversAreNotBoxReceivers()
    {
        // A `[GoRecv] this ref T` primary has a RecvGenerator ж-twin and forwards through the
        // box's ref-returning `.Value`; only the direct-ж form binds the box itself.
        Compilation compilation = Compile(
            """
            partial class sync_internal_test_package
            {
                internal static bool PopHead(this ref global::go.sync_package.poolChain c) => true;
                internal static bool Copy(this global::go.sync_package.poolChain c) => true;
            }
            """);

        HashSet<string> found = StructDeclarationSyntaxExtensions.GetBoxReceiverMethodNamesBySimpleName("poolChain", compilation);

        Assert.AreEqual(0, found.Count, "neither a ref nor a value receiver is a direct-ж primary");
    }
}
