using System.Runtime.CompilerServices;
using System.Threading;
using go.golib;

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

    // func spawn() (parent, child string) {
    //     parent = tracebackprobe.StackText()
    //     done := make(chan struct{})
    //     go func() { child = tracebackprobe.StackText(); close(done) }()
    //     <-done
    // }
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static (string parent, string child) spawn()
    {
        string parent = tracebackprobe_package.StackText();
        string child = "";
        using ManualResetEventSlim done = new();

        Goroutine.Start(() =>
        {
            child = tracebackprobe_package.StackText();
            done.Set();
        });

        done.Wait();
        return (parent, child);
    }
}
