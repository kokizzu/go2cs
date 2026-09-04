// pprof_impl.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// pprof_memProfileInternal and pprof_goroutineProfileWithLabels -- two of the eight linkname
// destinations pprof.cs declares bodyless, standing in for a push this corpus does not perform.
//
// WHY THERE IS NO FORWARDER TO WRITE INSTEAD
//   runtime HAS both functions, with real bodies (mprof.cs:1095 and :1331). The push that would
//   connect them is an edge runtime -> runtime/pprof, and runtime/pprof imports runtime, so the
//   forwarder would close a project-reference CYCLE -- MSB4006, every project on the path dead.
//   That is W1 (DESIGN-linkname-push-cycles.md), and check-solution-integrity.ps1 asserts against
//   it on every CNR run. So these are not "not done yet": no forwarder can exist, and the
//   destination has to answer for itself.
//
// THE TWO ANSWER DIFFERENTLY, AND THAT IS THE POINT OF THIS FILE
//   A destination answers with whatever the managed runtime can state TRUTHFULLY, which is not the
//   same amount for every profile. The memory profile has no records to report and says so. The
//   goroutine profile has a real population to report -- golib maintains a live goroutine registry
//   -- so it reports it. Neither models anything.
//
// WHY (0, true) IS HONEST FOR THE MEMORY PROFILE
//   The contract is two values, and both are literally true there. `n` is the number of records
//   available -- this runtime keeps no memory-profile records -- so it is zero. `ok` is "the slice
//   you passed was large enough to hold them all" -- a zero-length result fits in anything, so it
//   is true. Go's own implementation returns exactly this pair when its profile is empty; nothing
//   here is modelled or approximated.
//
//   The alternative is PartialStubGenerator's throw, which surfaces as `infrastructure-error` -- a
//   classification that means a HOST DEFECT and is not a verdict at all -- or, when it escapes on a
//   goroutine, as a truncated results stream. Both make the row UNMEASURABLE. An empty profile is a
//   measurable, honest, WRONG answer, and a wrong answer that states itself is worth more than a
//   right answer that cannot be reached.
//
// WHAT THIS DELIBERATELY DOES NOT DO
//   It does not fabricate records to make a content assertion pass. The check on that is
//   TestFakeMapping, which reaches writeHeapInternal through Lookup("heap").WriteTo: with the
//   memory-profile body it gets a well-formed profile carrying zero samples and FAILS on its own
//   terms -- "want profile with at least one mapping entry, got 0 mapping". It must keep failing. A
//   change here that makes it pass has laundered a false green, and that is the assertion to re-run
//   before believing any future increment in this file.
//
// NOT COVERED, AND NOT AN OVERSIGHT
//   pprof_blockProfileInternal and pprof_mutexProfileInternal are the same shape as the memory
//   profile and the same honest (0, true), but every row that reaches them sits behind the
//   runtime.Stack(all) host-killer first, so bodies here would move nothing measurable.
//   pprof_threadCreateInternal, pprof_fpunwindExpand and pprof_makeProfStack likewise stay
//   throwing. Named so the next increment starts from a set rather than a search.
//
// Hand-owned (no pprof_impl.go exists, so a reconvert never regenerates this file).
[module: go.GoManualConversion]

namespace go.runtime;

using profilerecord = go.@internal.profilerecord_package;
using @unsafe = unsafe_package;

partial class pprof_package {

// This runtime keeps no memory-profile records: zero are available, and zero of them fit in
// whatever the caller passed. writeHeapInternal's two-call loop takes the second branch on the
// first iteration and emits a profile with no samples.
internal static partial (nint n, bool ok) pprof_memProfileInternal(slice<profilerecord.MemProfileRecord> p, bool inuseZero) {
    return (0, true);
}

// The GOROUTINE PROFILE, over golib's live goroutine registry. Design: the 2026-09-04 Q27 section
// of BOARD-next-validation-candidates.md.
//
// WHAT IS REPORTED, AND WHY EACH PART IS A FACT RATHER THAN A MODEL
//
//   THE POPULATION is Go's. `Goroutine.ProfileSnapshot()` walks the registry and returns the
//   goroutines Go's `gcount()` would count -- user goroutines -- plus the finalizer goroutine while
//   it is running a finalizer body, which is Go's own special case (`isSystemGoroutine` answers
//   false for `runfinq` exactly while `fingStatus&fingRunningFinalizer` is set, and
//   goroutineProfileWithLabelsConcurrent adds one to n for it by name).
//
//   THE LABELS are the same pointers the goroutines set. runtime_setProfLabel writes golib's
//   per-goroutine slot (proflabel_impl.cs) and this hands the value straight back; nothing is
//   interpreted in between.
//
//   THE STACK is one frame: the goroutine's START FUNCTION, Go's gp.startpc. This is the part worth
//   being precise about. Go's saveg records the whole traceback, and the managed runtime cannot
//   walk a foreign thread's stack -- runtime.Stack(all) states that same limit with its
//   ForeignStackPlaceholder. What it CAN state is the bottom Go frame of the very traceback saveg
//   would have recorded. So this is an INCOMPLETE stack, not an invented one, and the distinction
//   is the whole reason a body is admissible here at all: a deeper stack would have to be made up.
//
//   THE PC is not invented either. GoSyntheticPC mints stable process-lifetime tokens for exactly
//   this case -- a function whose address is taken without calling it -- and runtime's
//   syntheticFrameRecord resolves them back through CallersFrames to an import-path-qualified Go
//   name, so both consumers symbolize the result with no further work: printCountProfile's debug
//   renderer and profileBuilder's proto encoder.
//
// A goroutine with no start function -- the main goroutine, or a thread a host entered directly --
// reports an EMPTY stack rather than borrowing a frame from somewhere. Go has no such goroutine, so
// there is no Go behaviour to match; an empty stack is the answer that claims nothing.
//
// NOT ATOMIC, and Go's is not either once the world restarts. Go stops the world to count and then
// lets goroutines add themselves; this reads a snapshot. A goroutine created between the caller's
// sizing call and its filling call is simply absent, which is the tolerance Go documents for its
// own concurrent collection: "New goroutines may not be in this list, but we didn't want to know
// about them anyway."
internal static partial (nint n, bool ok) pprof_goroutineProfileWithLabels(slice<profilerecord.StackRecord> p, slice<@unsafe.Pointer> labels) {
    // Go's own guard, at the top of goroutineProfileWithLabels: a labels slice that does not match
    // p is not usable, and is dropped rather than being an error.
    if (labels != default! && len(labels) != len(p)) {
        labels = default!;
    }

    var snapshot = global::go.golib.Goroutine.ProfileSnapshot();
    nint n = ((nint)snapshot.Length);

    // Go answers (gcount(), false) for an empty slice without collecting anything -- "an empty
    // slice is obviously too small" -- and says false unconditionally, including when the count is
    // itself zero. Kept as its own arm so that edge reads the same here.
    if (len(p) == 0) {
        return (n, false);
    }

    // Per the contract of runtime.GoroutineProfile: when p cannot hold the whole profile we are not
    // allowed to write to it AT ALL, and must answer (n, false) so the caller can resize and retry.
    if (n > len(p)) {
        return (n, false);
    }

    for (nint i = 0; i < n; i++) {
        var entry = snapshot[((int)i)];

        p[i] = new profilerecord.StackRecord(
            Stack: entry.Function is null
                ? default!
                : new uintptr[]{ ((uintptr)global::go.GoSyntheticPC.Of(entry.Function)) }.slice());

        if (labels != default!) {
            labels[i] = (entry.Labels as @unsafe.Pointer)!;
        }
    }

    return (n, true);
}

} // end pprof_package
