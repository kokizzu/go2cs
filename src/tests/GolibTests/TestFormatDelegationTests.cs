using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using go.testing_runtime;

namespace GolibTests;

[TestClass]
public class TestFormatDelegationTests
{
    // WHAT THIS GUARDS, and why the expectations are not hand-written.
    //
    // The test host formats through the converted `fmt` package, exactly as Go's own testing does
    // (Log is fmt.Sprintln, Logf is fmt.Sprintf). Before 2026-09-03 it carried a self-contained
    // formatter covering "the common verbs", which parsed the `#` flag and then DROPPED it for
    // %v -- so %#v rendered a per-object hash instead of Go-syntax fields. net's
    // TestUnixConnLocalAndRemoteNames reported a genuine address divergence as two hex words on
    // exactly that path, and the arc spent weeks looking at the reflect bridge for it.
    //
    // Every expected string below was TAKEN FROM GO, not from this implementation: each is the
    // output of the same call under `go run` on the pinned toolchain (go1.23.12). That direction
    // matters -- an expectation read off the thing under test is not a test of it, and a guard
    // whose oracle is its own subject cannot go red for the reason it exists.
    //
    // The cases are chosen so each can only pass for the right reason:
    //   - %#v on a string is quoted where %v is bare, so the `#` flag must actually REACH fmt;
    //     the retired shim passed the %v case and failed this one, which is the regression.
    //   - the MISSING and EXTRA forms are fmt's own disclosure styles, so a delegation that
    //     silently swallowed a formatting fault would not produce them.
    //   - Sprint exercises the Sprintln composition AND the single-newline trim.

    private static string Sprintf(string format, params object[] args) =>
        TestFormat.Sprintf(format, args);

    [TestMethod]
    public void AlternateVerbRendersGoSyntax()
    {
        // The defect this cut exists for: `#` must reach fmt, not be parsed and dropped.
        Assert.AreEqual("\"x\"", Sprintf("%#v", (@string)"x"));
    }

    [TestMethod]
    public void PlainVerbIsUnchangedByTheDelegation()
    {
        Assert.AreEqual("x", Sprintf("%v", (@string)"x"));
    }

    [TestMethod]
    public void AlternateVerbOnAnIntegerIsTheBareNumber()
    {
        Assert.AreEqual("42", Sprintf("%#v", 42));
    }

    [TestMethod]
    public void TypeVerbReportsTheGoTypeName()
    {
        // nint is Go's `int` in this corpus, which is what an untyped Go constant defaults to.
        // Written as a C# `int` this case reported "int32" -- correctly, since C# int IS Go int32.
        // Both spellings are asserted so the guard pins the MAPPING and not just one answer.
        Assert.AreEqual("int", Sprintf("%T", (nint)42));
        Assert.AreEqual("int32", Sprintf("%T", 42));
    }

    [TestMethod]
    public void MissingArgumentUsesFmtsOwnDisclosure()
    {
        Assert.AreEqual("1 %!v(MISSING)", Sprintf("%v %v", 1));
    }

    [TestMethod]
    public void ExtraArgumentUsesFmtsOwnDisclosure()
    {
        Assert.AreEqual("1%!(EXTRA int=2)", Sprintf("%v", (nint)1, (nint)2));
    }

    [TestMethod]
    public void QuoteVerbIsUnchangedByTheDelegation()
    {
        Assert.AreEqual("\"x\"", Sprintf("%q", (@string)"x"));
    }

    [TestMethod]
    public void SprintUsesSprintlnSpacingAndTrimsExactlyOneNewline()
    {
        // Go: fmt.Sprintln(1, "a", true) == "1 a true\n"; the host reports it without that newline.
        Assert.AreEqual("1 a true", TestFormat.Sprint(new object[] { 1, (@string)"a", true }));
    }
}
