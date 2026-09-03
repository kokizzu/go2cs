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
// WHY (0, true) IS HONEST AND NOT A STAND-IN
//   The contract is two values, and both are literally true here. `n` is the number of records
//   available -- this runtime keeps no memory-profile records and no goroutine stack records, so it
//   is zero. `ok` is "the slice you passed was large enough to hold them all" -- a zero-length
//   result fits in anything, so it is true. Go's own implementations return exactly this pair when
//   their profiles are empty; nothing here is modelled or approximated.
//
//   The alternative is what stands today: PartialStubGenerator's throw, which surfaces as
//   `infrastructure-error` -- a classification that means a HOST DEFECT and is not a verdict at
//   all -- or, when it escapes on a goroutine, as a truncated results stream. Both make the row
//   UNMEASURABLE. An empty profile is a measurable, honest, WRONG answer, and a wrong answer that
//   states itself is worth more than a right answer that cannot be reached.
//
// WHAT THIS DELIBERATELY DOES NOT DO
//   It does not fabricate records to make a content assertion pass. The check on that is
//   TestFakeMapping, which reaches writeHeapInternal through Lookup("heap").WriteTo: with these
//   bodies it gets a well-formed profile carrying zero samples and FAILS on its own terms --
//   "want profile with at least one mapping entry, got 0 mapping". It must keep failing. A change
//   here that makes it pass has laundered a false green, and that is the assertion to re-run before
//   believing any future increment in this file.
//
// NOT COVERED, AND NOT AN OVERSIGHT
//   pprof_blockProfileInternal and pprof_mutexProfileInternal are the same shape and the same
//   honest (0, true), but every row that reaches them sits behind the runtime.Stack(all) host-killer
//   first, so bodies here would move nothing measurable. pprof_threadCreateInternal,
//   pprof_fpunwindExpand and pprof_makeProfStack likewise stay throwing. Named so the next
//   increment starts from a set rather than a search.
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

// This runtime keeps no goroutine stack records. Note what is and is not being claimed: converted
// programs DO have goroutines, and golib dispatches them -- what does not exist is a registry of
// their stacks that could be walked here. So the profile is empty because nothing records it, not
// because nothing is running, and a later increment that grows such a registry replaces this body
// rather than contradicting it.
internal static partial (nint n, bool ok) pprof_goroutineProfileWithLabels(slice<profilerecord.StackRecord> p, slice<@unsafe.Pointer> labels) {
    return (0, true);
}

} // end pprof_package
