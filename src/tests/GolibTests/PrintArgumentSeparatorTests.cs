using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

/// <summary>
/// The builtins' SEPARATORS, as gc compiles them. <c>print(a, b)</c> lowers to one printer call per
/// argument with nothing between them, so it writes <c>ab</c>; <c>println(a, b)</c> puts a space
/// between arguments and a newline after the last, so it writes <c>a b\n</c>. golib joined BOTH with a
/// space, so every converted <c>print</c> of two or more arguments -- 192 statements in runtime alone
/// -- wrote text Go never writes, onto the stderr a re-exec test reads back.
/// </summary>
[TestClass]
public class PrintArgumentSeparatorTests
{
    // MSTest runs this assembly serially (no [Parallelize]), which is what makes swapping the global
    // stderr writer safe; it is restored whatever the arm does.
    private static string CaptureStdErr(Action body)
    {
        TextWriter saved = Console.Error;
        StringWriter captured = new() { NewLine = "\n" };

        try
        {
            Console.SetError(captured);
            body();
        }
        finally
        {
            Console.SetError(saved);
        }

        return captured.ToString();
    }

    [TestMethod]
    public void PrintConcatenatesItsArguments()
    {
        Assert.AreEqual("bad element 5(x) at iter 3\n",
            CaptureStdErr(() => builtin.print((@string)"bad element ", 5, (@string)"(", (@string)"x", (@string)") at iter ", 3, (@string)"\n")));
    }

    [TestMethod]
    public void PrintOfTwoValuesWritesThemBackToBack()
    {
        Assert.AreEqual("12true", CaptureStdErr(() => builtin.print(1, 2, true)));
    }

    [TestMethod]
    public void PrintlnKeepsOneSpaceBetweenArgumentsAndEndsWithANewline()
    {
        Assert.AreEqual("1 2 true\n", CaptureStdErr(() => builtin.println(1, 2, true)));
    }

    [TestMethod]
    public void PrintOfOneOrNoArgumentsIsUnchanged()
    {
        Assert.AreEqual("x", CaptureStdErr(() => builtin.print((@string)"x")));
        Assert.AreEqual("", CaptureStdErr(() => builtin.print()));
        Assert.AreEqual("\n", CaptureStdErr(() => builtin.println()));
    }
}
