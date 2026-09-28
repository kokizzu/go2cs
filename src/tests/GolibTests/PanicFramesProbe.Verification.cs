using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using go.golib;
using static go.builtin;
using static go.runtime_package;

// The shapes COORD's VERIFICATION of the re-cut named (the dated addendum and Appendix B of
// docs/phase4/briefs/review-r-panic-frames-2026-09-27.md on claude/coord-handover 886eb16694), each with
// Go's go1.24.13 answer. Where a raiser must be ACCEPTED for the arm to tell a fix from its absence, it is
// the zero-argument nil-func thunk: the one ends-at-Run site the splice still accepts.
namespace go;

internal static partial class panicframesprobe_package
{
    // Verification B1: a forwarder the JIT may tail-call. `defer dForward()` where dForward calls
    // gDeferPanics, which caught first. Go: O.func1 | gopanic | gDeferPanics | dForward | gopanic | O.
    // The delegate is dForward, not the catcher: NO splice, whatever the tail call did to the trace.
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    internal static void dForward() => gDeferPanics();

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> deferATailCallingForwarder()
    {
        List<string> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => { recover(); got = callersHere(); }, ref ᒐ);
            defer(dForward, ref ᒐ);
            throw panic("p1");
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }

    internal static bool registerNever;

    // Regression finder M0: the deferred delegate caught first but its Run registered no defer (an
    // unreached conditional defer). Go: M0.func1 | gopanic | dcUnreached | gopanic | M0. Spliced.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void dcUnreached()
    {
        GoFrame ᒐ = default;
        try {
            if (registerNever)
                defer(() => { }, ref ᒐ);
            throw panic("d");
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> deferACatcherWithNoDefer()
    {
        List<string> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => { recover(); got = callersHere(); }, ref ᒐ);
            defer(dcUnreached, ref ᒐ);
            throw panic("p1");
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }

    // Verification B2 NA: a nil func WITH arguments (golib's defer<T> closure) while p1 runs. Go:
    // NA.func1 | gopanic | panicmem | sigpanic | NA.deferwrap1 | gopanic | NA. deferwrap1 is not modelled:
    // NO splice.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> nilFuncWithArgWhilePanicking()
    {
        List<string> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => { recover(); got = callersHere(); }, ref ᒐ);
            Action<nint>? fn = null;
            defer(fn!, (nint)1, ref ᒐ);
            throw panic("p1");
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }

    // ...and NAR, on a normal return. Go: NAR.func1 | gopanic | panicmem | sigpanic | NAR.deferwrap1 | NAR.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> nilFuncWithArgOnNormalReturn()
    {
        List<string> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => { recover(); got = callersHere(); }, ref ᒐ);
            Action<nint>? fn = null;
            defer(fn!, (nint)1, ref ᒐ);
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }

    // Verification B2 NC: a nil value of a named func type, deferred through the converter's
    // `() => c()` lambda, while p1 runs. Go: NC.func1 | gopanic | panicmem | sigpanic | gopanic | NC, with
    // NO frame for the lambda. NO splice.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> nilNamedFuncTypeWhilePanicking()
    {
        List<string> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => { recover(); got = callersHere(); }, ref ᒐ);
            Action? c = null;
            defer(() => c!(), ref ᒐ);
            throw panic("p1");
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }

    // Verification C2: runtime errors Go raises through a runtime frame that is not modelled. Each
    // must splice NOTHING. Fully optimized, so the JIT tier cannot move the site.
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
    internal static List<string> runtimeErrorShape(int which)
    {
        List<string> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => { recover(); got = callersHere(); }, ref ᒐ);
            switch (which)
            {
                case 0: { array<nint> a = new(1); nint i = 5; got = [a[i].ToString()]; break; }               // goPanicIndex
                case 1: { @string s = "a"u8; nint i = 5; got = [s[i].ToString()]; break; }                    // goPanicIndex
                case 2: { map<@string, nint> m = default!; m["a"u8] = 1; break; }                             // mapassign_faststr
                case 3: { channel<nint> ch = new(1); close(ch); close(ch); break; }                            // closechan
                case 4: { object x = "s"; got = [x._<nint>().ToString()]; break; }                             // panicdottypeE
                default: { slice<nint> s = new(1); nint lo = 3; got = [s[(int)lo..].Length.ToString()]; break; } // goPanicSliceB
            }
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }

    // Verification C3: the TEST HOST's Goexit. t.FailNow / t.SkipNow on the test's own thread throw
    // TestAbortException, Go's runtime.Goexit there. A nil deferred func faulting during that unwind:
    // Go shows ... | sigpanic | runtime.Goexit | testing.(*common).FailNow | X. NO splice.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void nilDeferDuringHostGoexit(Action goexit, Action<List<string>> report)
    {
        GoFrame ᒐ = default;
        try {
            defer(() => { recover(); report(callersHere()); }, ref ᒐ);
            Action? f = null;
            defer(f!, ref ᒐ);
            goexit();
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
    }

    // A runtime.Goexit on the SAME thread, with the accepted raiser (the nil-func thunk).
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void nilDeferDuringGoexit(channel<List<string>> result)
    {
        GoFrame ᒐ = default;
        try {
            defer(() => { recover(); result.Send(callersHere()); }, ref ᒐ);
            Action? f = null;
            defer(f!, ref ᒐ);
            Goexit();
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
    }

    internal static List<string> goexitShapeNilThunk()
    {
        channel<List<string>> result = new(1);
        Goroutine.Start(() => nilDeferDuringGoexit(result));
        return result.Receive();
    }

    // Regression finder GX: a range-over-func seq calls Goexit, which the adapter re-raises on the ranging
    // goroutine; a nil deferred func faults during it. Go: GX.func1 | gopanic | panicmem | sigpanic |
    // runtime.Goexit | seqExit | GX. NO splice.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void nilDeferDuringRangeGoexit(channel<List<string>> result)
    {
        Action<Func<nint, bool>> seqExit = yield => { yield(1); throw new GoexitException(); };
        GoFrame ᒐ = default;
        try {
            defer(() => { recover(); result.Send(callersHere()); }, ref ᒐ);
            Action? f = null;
            defer(f!, ref ᒐ);
            foreach (nint _ in range(seqExit)) { }
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
    }

    internal static List<string> rangeGoexitShape()
    {
        channel<List<string>> result = new(1);
        Goroutine.Start(() => nilDeferDuringRangeGoexit(result));
        return result.Receive();
    }

    // Regression finder R1: seq's own nil deferred func faults on the coro's thread; the adapter re-raises
    // it on the ranging goroutine. Go: R1.func1 | gopanic | panicmem | sigpanic | seq | R1. The site's
    // owner is a Run on ANOTHER thread, so NO splice, even when the two threads' activation numbers
    // COLLIDE: both counters are planted to one value (reflection) so only the thread check refuses it.
    internal static readonly FieldInfo s_lastActivation =
        typeof(GoFrame).GetField("t_lastActivation", BindingFlags.Static | BindingFlags.NonPublic)!;

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void seqDeferringNil(Func<nint, bool> yield)
    {
        GoFrame ᒐ = default;
        try {
            s_lastActivation.SetValue(null, 1_000_000L);
            Action? f = null;
            defer(f!, ref ᒐ);
            yield(1);
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> crossThreadOwner()
    {
        List<string> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => { recover(); got = callersHere(); }, ref ᒐ);
            s_lastActivation.SetValue(null, 1_000_000L);
            foreach (nint _ in range<nint>(seqDeferringNil)) { }
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }

    // Verification C4/C5: a long chain of accepted links (nil-func thunks) while p0 runs, read through a
    // buffer of the given size. Go's list follows the measured one-link shape (N): every nil call's
    // sigpanic sits directly on the gopanic running it, so n thunks give (gopanic, panicmem, sigpanic) x n,
    // then p0's gopanic, then the deferring function.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> longThunkChain(int n, int buffer)
    {
        List<string> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => {
                recover();
                slice<uintptr> pcs = new(buffer);
                got = namesOf(pcs[..(int)Callers(0, pcs)]);
            }, ref ᒐ);
            Action? f = null;
            for (int i = 0; i < n; i++)
                defer(f!, ref ᒐ);
            throw panic("p0");
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }
}
