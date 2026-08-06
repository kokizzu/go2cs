// EmbeddedValueHopTests.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static go2cs.Symbols;

namespace go2cs.Tests;

[TestClass]
public class EmbeddedValueHopTests
{
    [TestMethod]
    public void TypeCollisionRenamedPropertyStillCountsAsEmbeddedValue()
    {
        CompilationUnitSyntax unit = CSharpSyntaxTree.ParseText(
            """
            partial struct Buffer
            {
                public partial ref bytes.Buffer ΔBuffer { get; }
            }
            """).GetCompilationUnitRoot();

        StructDeclarationSyntax declaration = unit.DescendantNodes().OfType<StructDeclarationSyntax>().Single();
        (string Name, string TypeName) hop = declaration.GetEmbeddedValueHopNames().Single();

        Assert.AreEqual($"{ShadowVarMarker}Buffer", hop.Name);
        Assert.AreEqual("bytes.Buffer", hop.TypeName);
    }
}
