using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static go.builtin;
using static go.runtime_package;

// Go-source frames for the panic-path frame guards (PanicFramesTests). runtime's callers() counts a method
// as a Go frame when its top-level type is a `*_package` class in namespace go. Each shape is spelled the
// way the converter emits a deferring Go function: the body inside try, a GoFrame beside it, the IsPanic
// catch filter, and ᒐ.Run() in the finally. Every shape records the function names a Callers taken inside
// one deferred call reports, and recovers, so the probe returns normally.
namespace go;

internal static partial class panicframesprobe_package
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> callersHere()
    {
        slice<uintptr> pcs = new(64);
        pcs = pcs[..(int)Callers(0, pcs)];

        return namesOf(pcs);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<(string function, long line)> callersHereWithLines()
    {
        slice<uintptr> pcs = new(64);
        pcs = pcs[..(int)Callers(0, pcs)];

        return framesOf(pcs);
    }

    internal static List<string> namesOf(slice<uintptr> pcs) => framesOf(pcs).ConvertAll(frame => frame.function);

    internal static List<(string function, long line)> framesOf(slice<uintptr> pcs)
    {
        List<(string function, long line)> frames = [];
        var iterator = CallersFrames(pcs);

        while (true)
        {
            var (frame, more) = iterator.Next();

            if (frame.PC == 0 && !more)
                break;

            frames.Add(((string)frame.Function, frame.Line));

            if (!more)
                break;
        }

        return frames;
    }

    // Each of f1-f3 is on ONE line, which records that line: the throw line for f3, the call lines for f2
    // and f1, which are the lines Go reports for those frames.
    internal static int f1Line, f2Line, f3Line;

    internal static int Line([CallerLineNumber] int line = 0) => line;

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void f1() { f1Line = Line(); f2(); }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void f2() { f2Line = Line(); f3(); }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void f3() { f3Line = Line(); throw panic("f3"); }

    // TestCallersPanic: a panic raised three calls down, recovered by the deferring function's defer.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> plainPanic()
    {
        List<string> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => {
                recover();
                got = callersHere();
            }, ref ᒐ);
            f1();
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }

    // TestCallersDoublePanic: the deferred call recovers panic 1 and raises panic 2 from its own
    // deferring frame; a Callers in panic 2's deferred call sees both gopanics.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> doublePanic()
    {
        List<string> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => {
                GoFrame ᒐ1 = default;
                try {
                    defer(() => {
                        got = callersHere();
                        recover();
                    }, ref ᒐ1);
                    recover();
                    throw panic(2);
                }
                catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
                finally { ᒐ1.Run(); }
            }, ref ᒐ);
            throw panic(1);
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }

    // TestCallersNilPointerPanic: a nil dereference is a fault, so panicmem and sigpanic sit above the site.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> nilPointerPanic()
    {
        List<string> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => {
                recover();
                got = callersHere();
            }, ref ᒐ);
            ж<nint> p = default!;
            if (p.Value == 3) {
                got = ["unreachable"];
            }
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }

    // TestCallersDivZeroPanic: an integer divide by zero reaches gopanic through panicdivide.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> divZeroPanic(nint zero)
    {
        List<string> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => {
                recover();
                got = callersHere();
            }, ref ᒐ);
            nint n = 1 / zero;
            got = [n.ToString()];
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }

    // TestCallersDeferNilFuncPanic: a nil deferred func faults when the deferring function's exit calls it.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> deferNilFuncPanic()
    {
        List<string> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => {
                recover();
                got = callersHere();
            }, ref ᒐ);
            Action? f = null;
            defer(f!, ref ᒐ);
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }

    // TestCallersAfterRecovery: once the recovering deferred call RETURNS, the panic is gone.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> afterRecovery()
    {
        List<string> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => {
                got = callersHere();
            }, ref ᒐ);
            defer(() => {
                recover();
            }, ref ᒐ);
            throw panic(1);
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> helper()
    {
        List<string> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => {
                got = callersHere();
            }, ref ᒐ);
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }

    // [P2-1]: the panic's deferred call calls a helper that defers, and the helper's deferred call calls
    // Callers. The helper's Run is a NORMAL-return sequence between the Callers and the panicking Run, so
    // it must hold an entry of its own or the splice lands one sequence too high.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> helperDefers()
    {
        List<string> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => {
                recover();
                got = helper();
            }, ref ᒐ);
            f1();
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }

    // The TestCallersAbortedPanic shape, observed BEFORE the recovery: a deferred call panics while panic 1
    // is running, which replaces it as the sequence's panic. The sequence's next deferred call sees panic 2,
    // the deferred call that raised it, and panic 1 beneath, whose gopanic called that deferred call.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> replacedPanic()
    {
        List<string> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => {
                recover();
            }, ref ᒐ);
            defer(() => {
                got = callersHere();
            }, ref ᒐ);
            defer(() => {
                throw panic("panic2");
            }, ref ᒐ);
            throw panic("panic1");
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void deferringMiddle()
    {
        GoFrame ᒐ = default;
        try {
            defer(() => { }, ref ᒐ);
            f3();
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
    }

    // [P2-2]'s MISMATCH: the panic is first caught by an intermediate deferring frame, so its site ends
    // there and not at the function whose sequence is running. The owner check fails and NOTHING is
    // spliced (Go would show gopanic, f3 and deferringMiddle: a stated residual, never a wrong splice).
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> ownerMismatch()
    {
        List<string> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => {
                recover();
                got = callersHere();
            }, ref ᒐ);
            deferringMiddle();
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }

    // TestStackWrapperStackPanic/sigpanic/CallersFrames: I.M(nil) faults IN the wrapper, and Go keeps a
    // wrapper whose callee is the panic machinery.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> wrapperIsTheSite()
    {
        List<string> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => {
                recover();
                got = callersHere();
            }, ref ᒐ);
            wrapperprobe_package.interfaceWrapper(null!);
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }

    internal sealed class panickingImpl : wrapperprobe_package.I
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public slice<uintptr> M() => throw panic("in M");
    }

    // A wrapper that is NOT the site (it called an ordinary method that panicked) keeps the elision.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> wrapperBelowTheSite()
    {
        List<string> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => {
                recover();
                got = callersHere();
            }, ref ᒐ);
            wrapperprobe_package.interfaceWrapper(new panickingImpl());
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }
}
