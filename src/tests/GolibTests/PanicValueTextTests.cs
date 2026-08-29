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

    // ---- the CONVERTED error shapes: a Go error is not a managed `error` implementer ----

    [TestMethod]
    public void APointerHeldConvertedErrorRendersItsErrorText()
    {
        // THE DEFECT. `panic(&dialErr{25})` converts to `throw panic(Ꮡ(new dialErr(25)))`, so the
        // panic value is a ж BOX — which implements no managed interface at all, carries no String,
        // and whose ToString() is PrintPointer(). Every arm therefore declined and the report read
        // `panic: 0x2668d34e960`: the address of the one object that knew what went wrong.
        Assert.AreEqual("dial: port 25 refused", PanicException.PanicText(Ꮡ(new dialErr(25))));
        Assert.AreEqual("dial: port 25 refused", new PanicException(Ꮡ(new dialErr(25))).Message);
    }

    [TestMethod]
    public void AValueHeldConvertedErrorRendersItsErrorText()
    {
        // The same gap one shape over, and the reason `openErr : error` above did not catch it: that
        // type implements golib's `error` interface DIRECTLY, which is a hand-written C# shape. The
        // converter never emits it — a Go method becomes an EXTENSION METHOD over its receiver, so a
        // converted value-receiver error reaches the `state is error` arm no more than a boxed one.
        Assert.AreEqual("parse: bad rune", PanicException.PanicText(new parseErr(0)));
    }

    [TestMethod]
    public void TheRealReflectValueErrorRendersItsGoText()
    {
        // The measured case, against the REAL converted type rather than a model of it: reflect's
        // `panic(&ValueError{"reflect.Value.Type", Invalid})` (value.cs:1916), which is what cost
        // two investigations their time. Go prints the Error(); the host printed the address.
        object boxed = Ꮡ(new reflect_package.ValueError("reflect.Value.Type"u8, reflect_package.Invalid));

        Assert.AreEqual("reflect: call of reflect.Value.Type on zero Value", PanicException.PanicText(boxed));
    }

    [TestMethod]
    public void APointerReceiverErrorDoesNotSubstituteForTheValueShape()
    {
        // Go's method SET, not merely "is there a method with this name": `func (e *dialErr) Error()`
        // puts Error in *dialErr's set and NOT in dialErr's, so a `panic(dialErr{25})` is not an
        // error panic and Go prints the value. The control that the probe reads receivers rather
        // than names — it falls out of the parameter re-check, which a `ref` primary cannot pass.
        Assert.AreNotEqual("dial: port 25 refused", PanicException.PanicText(new dialErr(25)));
    }

    [TestMethod]
    public void AFailingConvertedErrorSubstitutionReportsRatherThanEscaping()
    {
        // The extension arm's own version of the fallback above, and NOT the same code path: this
        // one throws from inside a reflective Invoke, so the failure arrives wrapped in a
        // TargetInvocationException rather than as itself.
        Assert.AreEqual("panic while printing panic value", PanicException.PanicText(Ꮡ(new throwingConvertedErr(1))));
    }

    [TestMethod]
    public void AGoexitDuringSubstitutionEscapesRatherThanBeingReported()
    {
        // A Goexit is not a failure to report — it is the goroutine ending, and it must keep
        // unwinding. The interface arm has always let it through (`when ex is not GoexitException`),
        // but a reflective Invoke WRAPS what it catches, so the same filter reads the wrapper's type
        // and swallows the Goexit into "panic while printing panic value". The unwrap is what makes
        // the two arms agree.
        Assert.ThrowsException<GoexitException>(() => PanicException.PanicText(Ꮡ(new goexitingConvertedErr(1))));
    }
}

// The shapes the CONVERTER emits, transcribed. A Go method is an extension method over its receiver
// in a non-nested sealed static class (the `<pkg>_package` class); a POINTER receiver is the
// `[GoRecv] this ref X` primary plus the `ж<X>` overload go2cs-gen's RecvGenerator adds — written
// out here because GolibTests does not run the analyzer.
internal struct dialErr(int port)
{
    public readonly int Port = port;
}

internal static class dialErr_package
{
    [GoRecv]
    public static @string Error(this ref dialErr e) => (@string)$"dial: port {e.Port} refused";

    public static @string Error(this ж<dialErr> e) => (@string)$"dial: port {e.Value.Port} refused";
}

// A VALUE receiver — `func (e parseErr) Error() string`. No `ref`, so both parseErr and *parseErr
// carry Error in their Go method sets, exactly as in Go.
internal readonly struct parseErr(int n)
{
    public readonly int N = n;
}

internal static class parseErr_package
{
    public static @string Error(this parseErr e) => (@string)$"parse: bad rune{(e.N == 0 ? "" : $" {e.N}")}";
}

internal readonly struct throwingConvertedErr(int n)
{
    public readonly int N = n;
}

internal static class throwingConvertedErr_package
{
    public static @string Error(this ж<throwingConvertedErr> e) => throw new InvalidOperationException($"Error() itself failed ({e.Value.N})");
}

internal readonly struct goexitingConvertedErr(int n)
{
    public readonly int N = n;
}

internal static class goexitingConvertedErr_package
{
    public static @string Error(this ж<goexitingConvertedErr> e) => throw new GoexitException();
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
