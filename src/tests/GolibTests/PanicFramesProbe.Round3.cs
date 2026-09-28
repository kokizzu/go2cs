using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using go.golib;
using static go.builtin;
using static go.runtime_package;

// The shapes COORD's SECOND verification named (addendum 2 of review-r-panic-frames-2026-09-27.md on
// claude/coord-handover 65d4eefa4f), each with Go's go1.24.13 answer.
namespace go;

internal static partial class panicframesprobe_package
{
    // C2: unsafe.Slice(&b, -1). Go: gopanic | runtime.panicunsafeslicelen1 | runtime.panicunsafeslicelen |
    // owner. Raised by hand-owned C#, so NOT Go source: NO splice.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> unsafeSliceNegative()
    {
        List<string> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => { recover(); got = callersHere(); }, ref ᒐ);
            ж<byte> b = new StandardBox<byte>(1);
            got = [unsafe_package.Slice(b, (nint)(-1)).Length.ToString()];
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }

    // C2: sync.Map.CompareAndSwap on a stored []int. Go: gopanic | runtime.efaceeq | runtime.nilinterequal |
    // internal/sync... | sync.(*Map).CompareAndSwap | owner. NO splice.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> syncMapUncomparable()
    {
        List<string> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => { recover(); got = callersHere(); }, ref ᒐ);
            ж<sync_package.Map> m = @new<sync_package.Map>();
            slice<nint> stored = new(1);
            m.Store("k", stored);
            got = [m.CompareAndSwap("k", stored, new slice<nint>(1)).ToString()];
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }

    // C2: bits.Div64(0, 1, 0). Go on amd64 (the intrinsic): gopanic | runtime.panicdivide | owner. The
    // converted Div64 panics with runtime.divideError itself: NO splice.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> div64ByZero(ulong y)
    {
        List<string> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => { recover(); got = callersHere(); }, ref ᒐ);
            var (q, _) = math.bits_package.Div64(0, 1, y);
            got = [q.ToString()];
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }

    // ...and the control: Div32's overflow is NOT an intrinsic, so Go shows the Go-source frame:
    // gopanic | math/bits.Div32 | owner. Spliced.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> div32Overflow(uint hi)
    {
        List<string> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => { recover(); got = callersHere(); }, ref ᒐ);
            var (q, _) = math.bits_package.Div32(hi, 0, 1);
            got = [q.ToString()];
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }

    internal struct T
    {
        public nint n;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public nint M() => n;
    }

    // `(*T).M`, as the converter emits the pointer form of a value-method expression.
    internal static readonly Func<ж<T>, nint> pointerWrapper = ((Func<ж<T>, nint>)([GoWrapper("(*T).M")] (p0) => p0.Value.M()));

    // C2 / R2: a nil *T through (*T).M. Go: gopanic | runtime.panicwrap | (*T).M | owner. Modelled: Go's list.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> nilThroughPointerWrapper()
    {
        List<string> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => { recover(); got = callersHere(); }, ref ᒐ);
            got = [pointerWrapper(null!).ToString()];
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }

    // C2: the Explicit SECOND guard. golib raises an explicit panic from its own code (SyncTestBubble.Wait
    // outside a bubble): NO splice.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> golibExplicitPanic()
    {
        List<string> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => { recover(); got = callersHere(); }, ref ᒐ);
            SyncTestBubble.Wait();
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }

    // C3: a stopped range-over-func seq unwinds on its coro. Go (a panic in the body): seqA.func1 | gopanic |
    // panicmem | sigpanic | runtime.gopanic | body-range1 | seqA. The adapter cannot tell a body panic,
    // Goexit or break apart, so every splice on that coro is refused.
    internal static List<string> seqGot = [];

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void seqWithNilDefer(Func<nint, bool> yield)
    {
        GoFrame ᒐ = default;
        try {
            defer(() => { recover(); seqGot = callersHere(); }, ref ᒐ);
            Action? f = null;
            defer(f!, ref ᒐ);
            yield(1);
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> bodyPanicsWhileSeqDefersNil()
    {
        seqGot = [];
        GoFrame ᒐ = default;
        try {
            defer(() => { recover(); }, ref ᒐ);
            foreach (nint _ in range<nint>(seqWithNilDefer))
                throw panic("body");
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return seqGot;
    }

    // ...and a BREAK. Go: seqA.func1 | gopanic | panicmem | sigpanic | seqA. Refused too (a stated missing
    // splice: the adapter sees a break exactly as it sees a body panic).
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> bodyBreaksWhileSeqDefersNil()
    {
        seqGot = [];
        foreach (nint _ in range<nint>(seqWithNilDefer))
            break;

        return seqGot;
    }

    // Item 3, NILAR: a zero-argument nil deferred func run after a COMPLETED recovery. Go:
    // NILAR.func1 | gopanic | panicmem | sigpanic | runtime.deferreturn | NILAR. deferreturn is not
    // modelled: NO splice.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> nilAfterRecovery()
    {
        List<string> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => { recover(); got = callersHere(); }, ref ᒐ);
            Action? f = null;
            defer(f!, ref ᒐ);
            defer(() => { recover(); }, ref ᒐ);
            throw panic("p");
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }

    // ...DNILAR, the control: after the recovery a LATER panic (raised by a directly deferred catcher) is
    // running when the nil func is called. Go: DNILAR.func1 | gopanic | panicmem | sigpanic | gopanic |
    // gDeferPanics | DNILAR. Spliced.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static List<string> nilWhileALaterPanicRuns()
    {
        List<string> got = [];
        GoFrame ᒐ = default;
        try {
            defer(() => { recover(); got = callersHere(); }, ref ᒐ);
            Action? f = null;
            defer(f!, ref ᒐ);
            defer(gDeferPanics, ref ᒐ);
            defer(() => { recover(); }, ref ᒐ);
            throw panic("p1");
        }
        catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
        finally { ᒐ.Run(); }
        return got;
    }
}
