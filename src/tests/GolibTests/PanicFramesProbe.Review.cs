using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using go.golib;
using static go.builtin;
using static go.runtime_package;

// The shapes COORD's review of the first cut named (docs/phase4/briefs/review-r-panic-frames-2026-09-27.md
// on claude/coord-handover), each with Go's answer measured on go1.24.13. A shape whose site does not
// provably reach the walked Run must splice NOTHING, never a list with frames missing; the stated
// residuals say so beside each.
namespace go;

internal static partial class panicframesprobe_package
{
    // A deferring function that panics: its own catch is the panic's FIRST.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void gDeferPanics()
    {
        GoFrame ᒐ = default;
        try {
            defer(() => { }, ref ᒐ);
            throw panic("p2");
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
    }

    // A deferring function whose own deferred call panics during its NORMAL return.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void gNormalDeferPanics()
    {
        GoFrame ᒐ = default;
        try {
            defer(() => { throw panic("p2"); }, ref ᒐ);
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void dCallsG() => gDeferPanics();

    // Review S01 shape A: `defer func(){ gDeferPanics() }()` while p1 runs. Go: A.func1 | gopanic |
    // gDeferPanics | A.func2 | gopanic | A. p2 was first caught by gDeferPanics and re-raised through the
    // closure, so its site does not reach this Run: NO splice (a stated residual).
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> shapeA()
    {
        List<string> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => { recover(); got = callersHere(); }, ref ᒐ);
            // Fully optimized from its first call, so the JIT may tail-call gDeferPanics and drop this frame
            // from the re-raise's trace: the acceptance must not depend on that trace (verification B1).
            defer([MethodImpl(MethodImplOptions.AggressiveOptimization)] () => { gDeferPanics(); }, ref ᒐ);
            throw panic("p1");
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }

    // Review S01 shape B: `defer func(){ gNormalDeferPanics() }()`. Go: B.func1 | gopanic |
    // gNormalDeferPanics.func1 | gNormalDeferPanics | B.func2 | gopanic | B. NO splice (residual).
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> shapeB()
    {
        List<string> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => { recover(); got = callersHere(); }, ref ᒐ);
            defer(() => { gNormalDeferPanics(); }, ref ᒐ);
            throw panic("p1");
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }

    // Review S06/S11 D2: `defer gDeferPanics()`, the deferred delegate IS the first catcher and its Run
    // re-raised straight here. Go: O2.func1 | gopanic | gDeferPanics | gopanic | O2. Spliced.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> deferTheCatcher()
    {
        List<string> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => { recover(); got = callersHere(); }, ref ᒐ);
            defer(gDeferPanics, ref ᒐ);
            throw panic("p1");
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }

    // Review S06 G shape: `defer D()` where D calls G, and G defers and panics. Go: O.func1 | gopanic | G |
    // D | gopanic | O. D lies between the first catcher and this Run: NO splice (residual).
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> deferACallerOfTheCatcher()
    {
        List<string> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => { recover(); got = callersHere(); }, ref ᒐ);
            defer(dCallsG, ref ᒐ);
            throw panic("p1");
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void nilDeferInCallee()
    {
        GoFrame ᒐ = default;
        try {
            Action? f = null;
            defer(f!, ref ᒐ);
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
    }

    // Review S07: a nil deferred func faults in a CALLEE, and the fault propagates here. Go: B.func1 |
    // gopanic | panicmem | sigpanic | A | B. The site-less site belongs to the callee's Run: NO splice.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> nilDeferPropagated()
    {
        List<string> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => { recover(); got = callersHere(); }, ref ᒐ);
            nilDeferInCallee();
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }

    internal static List<string> recGot = [];

    // Review S03/S08: rec(1) -> rec(0) panics, rec(1)'s deferred call reads. Go: rec.func1 | gopanic | rec |
    // rec. The site belongs to rec(0)'s activation, not rec(1)'s: NO splice (residual), never one rec.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void rec(nint n)
    {
        GoFrame ᒐ = default;
        try {
            defer(() => { if (n == 1) { recover(); recGot = callersHere(); } }, ref ᒐ);
            if (n == 0) throw panic("rec");
            rec(n - 1);
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> recursion()
    {
        rec(1);
        return recGot;
    }

    // Review S04/S09 NR: a deferred call panics during a NORMAL return. Go: NR.func1 | gopanic | NR.func2 | NR.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> normalReturnPanic()
    {
        List<string> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => { recover(); got = callersHere(); }, ref ᒐ);
            defer(() => { throw panic("d"); }, ref ᒐ);
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }

    // Review S09 AR: a deferred call panics AFTER a recovery completed. Go: AR.func1 | gopanic | AR.func2 | AR.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> panicAfterRecovery()
    {
        List<string> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => { recover(); got = callersHere(); }, ref ᒐ);
            defer(() => { throw panic("p3"); }, ref ᒐ);
            defer(() => { recover(); }, ref ᒐ);
            throw panic("p1");
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }

    // Review S04 P: `defer panic(v)`, as the converter emits it (DeferPanicArg). Go: P.func1 | gopanic |
    // P.deferwrap1 | P.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> deferPanicArg()
    {
        List<string> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => { recover(); got = callersHere(); }, ref ᒐ);
            defer(ᴛ1 => throw panic(ᴛ1), "p2", ref ᒐ);
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }

    // Review S02/S15 N: a nil deferred func faults while p1 runs. Go: N.func1 | gopanic | panicmem |
    // sigpanic | gopanic | N.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> nilDeferWhilePanicking()
    {
        List<string> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => { recover(); got = callersHere(); }, ref ᒐ);
            Action? f = null;
            defer(f!, ref ᒐ);
            throw panic("p1");
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }

    internal static string p2Thrower = "", p3Thrower = "";

    // Review S20 T: a two-link chain. Go: T.func2 | gopanic | T.func3 | gopanic | T.func4 | gopanic | T,
    // where func3 raised p3 and func4 raised p2 (func4 runs first).
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> twoLinkChain()
    {
        List<string> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => { recover(); }, ref ᒐ);
            defer(() => { got = callersHere(); }, ref ᒐ);
            defer(() => { p3Thrower = callersHere()[2]; throw panic("p3"); }, ref ᒐ);
            defer(() => { p2Thrower = callersHere()[2]; throw panic("p2"); }, ref ᒐ);
            throw panic("p1");
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }

    // Review S11 RP: recover() then panic() in ONE deferred call. Go: RP.func1 | gopanic | RP.func2 | gopanic | RP.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> recoverThenPanic()
    {
        List<string> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => { recover(); got = callersHere(); }, ref ᒐ);
            defer(() => { recover(); throw panic("p2"); }, ref ᒐ);
            throw panic("p1");
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }

    // Review S05/S19: an index out of range. Go: indexPanic.func1 | gopanic | goPanicIndex | indexPanic.
    // goPanicIndex is not modelled, so NO splice, never gopanic straight over the site.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> indexPanic(nint i)
    {
        List<string> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => { recover(); got = callersHere(); }, ref ᒐ);
            slice<nint> s = new(1);
            nint v = s[i];
            got = [v.ToString()];
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }

    // Review S20: [P2-3]'s skip path. runtime.Caller(1) inside a panic's deferred call is runtime.gopanic.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static (string file, long line) callerOneInDefer()
    {
        (string file, long line) got = ("", 0);
        GoFrame ᒐ = default;
        try {
            defer(() => {
                recover();
                var (_, file, line, _) = Caller(1);
                got = ((string)file, line);
            }, ref ᒐ);
            f1();
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }

    // ...and the capacity path: a 3-slot buffer fills from the top, and skip 2 starts at gopanic.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static (List<string> threeSlots, List<string> skipTwo) capacityAndSkip()
    {
        (List<string>, List<string>) got = ([], []);
        GoFrame ᒐ = default;
        try {
            defer(() => {
                recover();
                slice<uintptr> three = new(3);
                slice<uintptr> eight = new(8);
                got = (namesOf(three[..(int)Callers(0, three)]), namesOf(eight[..(int)Callers(2, eight)]));
            }, ref ᒐ);
            f1();
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }

    // Review S20: the spliced site frames' LINES (TestCallersPanic's testCallers reads them).
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<(string function, long line)> plainPanicWithLines()
    {
        List<(string function, long line)> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => { recover(); got = callersHereWithLines(); }, ref ᒐ);
            f1();
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }

    // A panic raised by a deferred call that a GOEXIT runs. Go shows runtime.Goexit beneath the deferred
    // call, which is not modelled: NO splice. Runs on its own goroutine, which the Goexit ends.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void panicDuringGoexit(channel<List<string>> result)
    {
        GoFrame ᒐ = default;
        try {
            defer(() => { recover(); result.Send(callersHere()); }, ref ᒐ);
            defer(() => { throw panic("px"); }, ref ᒐ);
            Goexit();
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
    }

    // Runs a probe on a goroutine of its own: a pooled thread starts with clean thread state, so a Goexit
    // an earlier test left on the test thread cannot reach the probe.
    internal static T onGoroutine<T>(Func<T> probe)
    {
        channel<T> done = new(1);
        Goroutine.Start(() => done.Send(probe()));
        return done.Receive();
    }

    internal static List<string> goexitShape()
    {
        channel<List<string>> result = new(1);
        Goroutine.Start(() => panicDuringGoexit(result));
        return result.Receive();
    }
}
