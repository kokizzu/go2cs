using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using fmt = go.fmt_package;

namespace GolibTests;

// The TestRegisterErr chip: net/http's TestRegisterErr formats a nil HandlerFunc handler with
// %#v to build a subtest name (`t.Run(fmt.Sprintf("%s:%#v", pattern, handler), ...)`), and the
// go2cs test-comparison pipeline matches Go's and C#'s JSON streams BY that subtest name — so the
// formatted string has to match Go's real `(http.HandlerFunc)(nil)`, not merely avoid a crash.
// HandlerFunc is a named func type; crossing a foreign interface boundary (a value-receiver
// method set implemented in another assembly from any consumer) mints an IValueAdapter shell
// wrapping a COPY of the delegate. A nil delegate is a null .NET reference — unlike a nil-valued
// STRUCT, whose boxed copy is never null — so it erases its own runtime type the moment it is
// read back, reproducing the shape without needing net/http itself.
//
// A converted named func type is always a delegate nested in its declaring `<pkg>_package` class
// (GoReflect.TypeNaming's isUnnamedFuncType keys on exactly that), so the double is nested the
// same way — a bare namespace-scope delegate reads as an UNNAMED Go func type instead.
internal static class sample_package
{
    internal delegate void SampleDelegate();
}

internal sealed class NilDelegateShell : IValueAdapter
{
    private readonly sample_package.SampleDelegate m_value;

    object? IValueAdapter.Value => m_value;
}

[TestClass]
public class ValueAdapterFormatTests
{
    [TestMethod]
    public void SharpVOnNilDelegateAdapterNamesTheWrappedTypeNotTheShell()
    {
        string text = fmt.Sprintf("%#v", new NilDelegateShell());

        StringAssert.Contains(text, nameof(sample_package.SampleDelegate));
        Assert.IsFalse(text.Contains(nameof(NilDelegateShell)), $"leaked the adapter shell's own type: {text}");
    }

    [TestMethod]
    public void SharpVOnNilDelegateAdapterShowsNilNotARandomAddress()
    {
        string text = fmt.Sprintf("%#v", new NilDelegateShell());

        StringAssert.EndsWith(text, "(nil)");
    }
}
