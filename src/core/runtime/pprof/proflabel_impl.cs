// proflabel_impl.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// Per-goroutine profile labels — the managed realization of `getg().labels`.
//
// runtime/pprof.go declares the pair as linkname pulls from the runtime:
//
//     //go:linkname runtime_setProfLabel runtime/pprof.runtime_setProfLabel
//     //go:linkname runtime_getProfLabel runtime/pprof.runtime_getProfLabel
//
// so runtime/pprof/runtime.cs carries them as BODYLESS partials, which take throwing
// PartialStubGenerator stubs. Every caller — SetGoroutineLabels, Do, and the suite's own read-back
// — died there. The runtime-side definitions in runtime/proflabel.cs are faithful conversions and
// die one level deeper, in `getg()`.
//
// WHY THIS DOES NOT IMPLEMENT getg(). getg() is the honest floor for both bodies, and giving it a
// managed body to reach two functions would be the wrong trade by a wide margin: it is referenced
// at 574 sites across 92 files (runtime/{windows,linux,darwin}/proc.cs 75 each, os_windows.cs 20,
// panic.cs 15, the signal/GC/time/stack paths 8-13 each). A body there converts 574 LOUD THROWS
// into quiet partial behaviour over a `g` that models a fraction of what Go's carries — the
// false-green shape this corpus treats as worse than the throw. getg() stays a stub; the labels
// get their own storage.
//
// WHY AsyncLocal AND NOT [ThreadStatic]. golib gives every goroutine its own dedicated thread, so
// [ThreadStatic] looks exact here. It is wrong, and Go's own scheduler says why:
//
//     proc.go:5097   // Only user goroutines inherit pprof labels.
//     proc.go:5099   newg.labels = mp.curg.labels
//
// Labels are INHERITED at goroutine creation and independent thereafter. A [ThreadStatic] gives a
// spawned goroutine none, which is a silent wrong answer that passes every test that does not spawn
// under a label — and spawning under a label is the entire point of pprof.Do. AsyncLocal<T> under a
// flowing ExecutionContext expresses Go's rule exactly: the value is CAPTURED at thread start and
// later writes on either side do not cross, which is `newg.labels = mp.curg.labels` and then
// separate storage.
//
// That the flow actually happens is MEASURED, not read off Goroutine.Start's comment:
// GolibTests/GoroutineExecutionContextFlowTests covers inheritance through golib's real spawn
// primitive, independence of the child's writes, and — the control that makes the first mean
// anything — that a SUPPRESSED-flow spawn does NOT inherit. If golib's spawn path ever moves to
// Thread.UnsafeStart, ThreadPool.UnsafeQueueUserWorkItem, or wraps spawning in
// ExecutionContext.SuppressFlow, that guard goes red and these labels silently stop inheriting.
//
// SCOPE, stated because it is narrower than the Go original. Go stores labels on the g, where the
// SIGPROF handler also reads them (proc.go:5564, `tagPtr = &gp.m.curg.labels`) to tag CPU samples.
// This storage serves the pprof API surface — SetGoroutineLabels, Do, Labels, and the suite's
// read-back — and the managed corpus has no signal-based sampling profiler to read it from. When
// one exists it reads from here; nothing else changes.
//
// This file has no `<name>.go` counterpart, so a -stdlib reconvert never emits over it; the module
// marker states the ownership explicitly.

[module: go.GoManualConversion]

namespace go.runtime;

using System.Threading;
using @unsafe = unsafe_package;

partial class pprof_package {

// The per-goroutine label slot. AsyncLocal's ExecutionContext capture at thread start IS Go's
// inheritance at goroutine creation; see the file header for the measurement that establishes it.
private static readonly AsyncLocal<@unsafe.Pointer?> s_profLabels = new();

// runtime_setProfLabel: `getg().labels = labels`, on managed storage.
//
// Go's body also takes a race edge on labelSync under raceenabled (proflabel.go), so that
// profBuf.read synchronizes with all prior setProfLabel operations. There is no race detector in
// this corpus and the converted runtime's raceenabled is constant false, so the edge has nothing to
// carry and is deliberately not modeled — the same reasoning syscall_linux_impl.cs's
// entersyscall/exitsyscall pair records.
internal static partial void runtime_setProfLabel(@unsafe.Pointer labels) {
    s_profLabels.Value = labels;
}

// runtime_getProfLabel: `return getg().labels`.
//
// An unset slot answers null, which is the converted nil unsafe.Pointer — Go returns
// unsafe.Pointer(nil) for a goroutine that has never been labelled, and pprof's read-back
// (`(*labelMap)(runtime_getProfLabel())`) is a nil-tolerant conversion on both sides.
internal static partial @unsafe.Pointer runtime_getProfLabel() {
    return s_profLabels.Value!;
}

} // end pprof_package
