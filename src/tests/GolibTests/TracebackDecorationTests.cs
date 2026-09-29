using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using go.golib;

namespace GolibTests;

/// <summary>
/// Guards two decorations of a printed traceback (census family A7; COORD approved 2026-09-29 01:47): a
/// generic FUNCTION prints as <c>fn[...]</c>, as Go's funcNameForPrint spells it, while a method of a
/// generic type keeps its <c>T[...].M</c> form; and the calling goroutine's block ends with Go's
/// <c>created by &lt;fn&gt; in goroutine &lt;parent&gt;</c>, which runtime's
/// TestTracebackParentChildGoroutines reads. Red against the printer seat, which prints <c>fn()</c> and
/// no created-by line for the calling goroutine.
/// </summary>
[TestClass]
public class TracebackDecorationTests
{
    [TestMethod]
    public void AGenericFunctionPrintsWithGosBrackets()
    {
        string stack = Task.Run(tracebackdeco_package.genericFn<int>).GetAwaiter().GetResult();

        StringAssert.Contains(stack, "\ntracebackdeco.genericFn[...]()\n", stack);
        Assert.IsFalse(stack.Contains("shape"), stack);
    }

    [TestMethod]
    public void APlainFunctionAndAGenericTypesMethodKeepTheirNames()
    {
        string plain = Task.Run(tracebackdeco_package.plainFn).GetAwaiter().GetResult();
        string method = Task.Run(() => new tracebackdeco_package.genericTyp<int>().M()).GetAwaiter().GetResult();

        StringAssert.Contains(plain, "\ntracebackdeco.plainFn()\n", plain);
        StringAssert.Contains(method, "\ntracebackdeco.genericTyp[...].M()\n", method);
        Assert.IsFalse(method.Contains(".M[...]"), method);
    }

    [TestMethod]
    public void AChildGoroutineEndsWithItsCreatorAndParent()
    {
        string parent = "", child = "";

        Task.Run(() =>
        {
            using Goroutine.Scope scope = Goroutine.Enter();
            (parent, child) = tracebackdeco_package.spawn();
        }).GetAwaiter().GetResult();

        string parentId = Regex.Match(parent, @"^goroutine (\d+) \[").Groups[1].Value;

        Assert.AreNotEqual("", parentId, parent);
        StringAssert.EndsWith(child, $"created by tracebackdeco.spawn in goroutine {parentId}\n", child);
        Assert.IsFalse(parent.Contains("created by"), "a goroutine the host entered has no creator: " + parent);
    }
}
