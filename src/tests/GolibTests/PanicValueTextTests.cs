using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using static go.builtin;

namespace GolibTests;

/// <summary>
/// How a panic VALUE renders — Go's <c>preprintpanics</c> rule, which substitutes an
/// <c>error</c>'s <c>Error()</c> and a <c>Stringer</c>'s <c>String()</c> before the runtime prints
/// anything.
/// </summary>
/// <remarks>
/// The Go-observable half is the behavioral <c>PanicValueRendering</c> test, which lets a real
/// <c>panic(err)</c> escape and has the runner compare the report's first stderr line against
/// <c>go run</c> — strictly stronger, because Go supplies the expected text. It can only carry ONE
/// shape though: the substitution happens on the way out of the process, so the program dies at the
/// first unrecovered panic. The arms it cannot reach are pinned here, along with the laziness rule
/// (which is a statement about WHEN the substitution runs, and so has no printed form at all).
/// </remarks>
[TestClass]
public class PanicValueTextTests
{
    // A converted Go error: `type openErr struct{...}` with `func (e openErr) Error() string`,
    // which reaches golib's error interface through the error<T> carrier the same way a converted
    // one does.
    private readonly struct openErr(int code) : error
    {
        private readonly int m_code = code;

        public @string Error() => (@string)$"open: code {m_code}";
    }

    // Counts its own Error() calls, so "was it called at all, and when" is assertable.
    private sealed class countingErr : error
    {
        public int Calls;

        public @string Error()
        {
            Calls++;
            return (@string)"counted";
        }
    }

    private sealed class throwingErr : error
    {
        public @string Error() => throw new InvalidOperationException("Error() itself failed");
    }

    [TestMethod]
    public void AnErrorPanicValueRendersItsErrorText()
    {
        Assert.AreEqual("open: code 13", PanicException.PanicText(new openErr(13)));
        Assert.AreEqual("open: code 13", new PanicException(new openErr(13)).Message);
    }

    [TestMethod]
    public void AStringerPanicValueRendersItsStringText()
    {
        // The arm the behavioral test cannot reach, and the one ToString() does NOT already cover: a
        // Go named type's generated ToString forwards to the UNDERLYING value, so `panic(2*time.Second)`
        // would print 2000000000 where Go prints 2s. `stringerish` carries its Go String() as an
        // extension method, which is where the converter puts a Go method.
        Assert.AreEqual("label<7>", PanicException.PanicText(new stringerish(7)));
    }

    [TestMethod]
    public void APlainStringPanicValueIsUntouched()
    {
        // The commonest panic value by far. Go prints it verbatim, and no substitution applies.
        Assert.AreEqual("boom", PanicException.PanicText((@string)"boom"));
        Assert.AreEqual("nil", PanicException.PanicText(null));
    }

    [TestMethod]
    public void AValueWithNeitherMethodFallsBackToItsOwnRendering()
    {
        // Go prints a plain value with its own formatting; the managed analogue is ToString(). The
        // control that the two substitution arms have not swallowed everything else.
        Assert.AreEqual("41", PanicException.PanicText((nint)41));
    }

    [TestMethod]
    public void TheSubstitutionRunsOnFirstReadAndNotAtConstruction()
    {
        // Go runs preprintpanics only once a panic has gone UNRECOVERED and is about to print, so a
        // recovered panic never calls a user Error() at all. Doing it eagerly would call user code
        // on every panic in the corpus — fmt's catchPanic, text/template's errRecover, every
        // `defer func(){ recover() }()` — which is both a cost and a behavior Go does not have.
        countingErr counter = new();
        PanicException panic = new(counter);

        Assert.AreEqual(0, counter.Calls, "constructing a panic must not call the value's Error()");

        Assert.AreEqual("counted", panic.Message);
        Assert.AreEqual(1, counter.Calls);

        // ...and it is computed ONCE however many readers there are, ToString() — which reads the
        // virtual Message, and is what an unhandled-exception dump prints — included.
        Assert.AreEqual("counted", panic.Message);
        StringAssert.Contains(panic.ToString(), "counted");
        Assert.AreEqual(1, counter.Calls);
    }

    [TestMethod]
    public void AFailingSubstitutionReportsRatherThanEscaping()
    {
        // Go throws a fatal "panic while printing panic value" here. Reproducing the FATALITY from a
        // Message getter would be worse than the divergence it reports — a reader asking a panic for
        // its text would kill the process — so the text is returned instead.
        Assert.AreEqual("panic while printing panic value", PanicException.PanicText(new throwingErr()));
    }
}

// Outside the test class: an extension method must live in a non-nested static class, which is also
// exactly where the converter puts a Go method (the `<pkg>_package` class).
internal readonly struct stringerish(int n)
{
    public readonly int N = n;
}

internal static class stringerish_package
{
    public static @string String(this stringerish s) => (@string)$"label<{s.N}>";
}
