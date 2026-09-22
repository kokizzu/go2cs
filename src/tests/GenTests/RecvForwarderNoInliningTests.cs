// RecvForwarderNoInliningTests.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace go2cs.Tests;

/// <summary>
/// The converter marks a function whose frame runtime.Caller/Callers' skip count depends on
/// <c>[MethodImpl(MethodImplOptions.NoInlining)]</c> (computeNoInliningClosure). A <c>ref T</c>
/// receiver method is reached from a box through RecvGenerator's ж-forwarder, a two-line
/// deref-and-call the optimizing JIT inlines into the CALLER: the skip count survives (a go2cs-gen
/// frame is never counted), but the caller's return address then sits inside the inlinee and the
/// caller's frame resolves to file "" / line 0. log/slog's TestCallDepth measured it -- FAIL under
/// TieredCompilation=0, PASS with JitNoInline=1. The forwarder carries the source method's mark, and
/// only that mark: an unmarked method's forwarder stays inlinable.
/// </summary>
[TestClass]
public class RecvForwarderNoInliningTests
{
    private const string Source = """
        using System.Runtime.CompilerServices;

        namespace go
        {
            [System.AttributeUsage(System.AttributeTargets.Method)]
            public sealed class GoRecvAttribute : System.Attribute { }

            public class ж<T> { public ref T DerefOrNull() => throw null!; }

            public partial struct Logger { }

            public static partial class slog_package
            {
                [MethodImpl(MethodImplOptions.NoInlining)] [GoRecv] public static void Log(this ref Logger l, int level) { }

                [GoRecv] public static void Plain(this ref Logger l, int level) { }
            }
        }
        """;

    private static string ForwarderFor(string methodName)
    {
        CSharpCompilation compilation = CSharpCompilation.Create("recv",
            [CSharpSyntaxTree.ParseText(Source)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new RecvGenerator()).RunGenerators(compilation);
        GeneratorDriverRunResult result = driver.GetRunResult();

        string[] matches = result.GeneratedTrees
            .Select(tree => tree.ToString())
            .Where(text => text.Contains($" {methodName}(this ж<"))
            .ToArray();

        Assert.AreEqual(1, matches.Length, $"exactly one ж-forwarder is generated for {methodName}");
        return matches[0];
    }

    [TestMethod]
    public void ANoInliningReceiverMethodYieldsANoInliningForwarder()
    {
        StringAssert.Contains(ForwarderFor("Log"), "MethodImplOptions.NoInlining",
            "the forwarder of a NoInlining source method must not be inlinable either");
    }

    [TestMethod]
    public void AnUnmarkedReceiverMethodYieldsAnInlinableForwarder()
    {
        Assert.IsFalse(ForwarderFor("Plain").Contains("NoInlining"),
            "the mark is inherited, never invented: an unmarked method's forwarder stays inlinable");
    }
}
