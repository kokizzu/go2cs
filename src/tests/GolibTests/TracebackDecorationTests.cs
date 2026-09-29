using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

/// <summary>
/// Guards the generic-function decoration of a printed traceback (census family A7; COORD approved
/// 2026-09-29 01:47): a generic FUNCTION prints as <c>fn[...]</c>, as Go's funcNameForPrint spells it,
/// while a method of a generic type keeps its <c>T[...].M</c> form. Red against the printer seat, which
/// prints <c>fn()</c>. (A7's other half, the calling goroutine's <c>created by</c> line, is held: Go prints
/// the <c>go</c> statement's position beneath it and parseTraceback requires that line.)
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
}
