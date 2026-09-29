using System.Runtime.CompilerServices;

// Go-source frames for the traceback-decoration guards (census family A7): a method counts as a Go frame
// when its top-level type is a `*_package` class in namespace go, so these print as `tracebackdeco.<Func>`.
namespace go;

internal static class tracebackdeco_package
{
    // func genericFn[T any]() string { return tracebackprobe.StackText() }
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static string genericFn<T>() => tracebackprobe_package.StackText();

    // func plainFn() string { return tracebackprobe.StackText() }
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static string plainFn() => tracebackprobe_package.StackText();

    // type genericTyp[P any] struct{ x P }
    internal struct genericTyp<P>
    {
        public P x;
    }

    // func (t genericTyp[P]) M() string { return tracebackprobe.StackText() }
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static string M<P>(this genericTyp<P> t) => tracebackprobe_package.StackText();
}
