// mprof_impl.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// SPDX-License-Identifier: BSD-3-Clause
// Use of this source code is governed by a BSD-style license
// that can be found in the LICENSE file.

// runtime.saveblockevent -- the recorder under the block profile (blockevent) and the mutex profile
// (mutexevent). Hand-owned by manualConversionFuncs["runtime"]["saveblockevent"] as a REFUSAL BY NAME
// (COORD ruling (A), 2026-09-22).
//
// WHAT THE CONVERTED BODY DID. It returned before recording anything, because debug.profstackdepth reads 0:
// Go sets it (default 128) in parsedebugvars on the schedinit path, which this host never runs. With that
// supplied, it took the amd64 FRAME-POINTER branch, fpTracebackPartialExpand over getfp(), which has no
// managed answer. Go's OTHER branch, callers(skip, mp.profStack), IS expressible: the hand-owned callers
// (managed_impl.cs) projects the CLR stack onto Go-logical frames. MEASURED at 8fc439415f, it captured the
// recording frame by name and carried the event past the capture into saveBlockEventStack.
//
// WHY IT STILL CANNOT RECORD: THE BUCKET STORE IS GO-LAYOUT MEMORY. saveBlockEventStack -> stkbucket
// allocates the hash on first use with sysAlloc (Windows: sysAllocOS -> stdcall4 VirtualAlloc ->
// asmcgocall, which has no managed body). A VirtualAlloc body alone would only move the failure, because
// the store is not expressible:
//
//   newBucket persistentallocs ONE block holding a 48-byte bucket header whose next / allnext are *bucket
//   (reference-bearing), followed by the stk array (nstk words), followed by the blockRecord (or the
//   memRecord). bucket.stk(), bp() and mp() reach the two trailing parts by BYTE OFFSET
//   (bucket + 48 + nstk*8), and buckhash is a sysAlloc'd native array of *bucket.
//
// That is the arm-2a class: a reference-bearing pointee at a Go-layout byte offset into storage the CLR
// lays out itself. The design record for the alternative, a managed bucket store (hand-owned newBucket /
// stkbucket / stk / bp / mp and the hash, readers unchanged), is BANKED, not dispatched. It unlocks only
// the direct callers (runtime/pprof's TestBlockProfileBias). TestBlockProfile, TestMutexProfile, runtime's
// lock-profile sample and net/http/pprof's TestDeltaProfile also need EVENTS fed from the managed
// primitives (golib channels/select -> blockevent, the hand-owned sync.Mutex -> mutexevent, the managed
// lock2 -> mLockProfile), which is a managed-profiling arc rather than one seat.
//
// So the body refuses by NAME, replacing the misleading "assembly, cgo, or a linkname whose push did not
// arrive" throw. It is reached only when a block or mutex profile rate is above 0; at Go's default of 0,
// blocksampled returns false in blockevent and this body is never entered, so the common path does not move.
//
// Declared once in mprof.go for every target, so this one body serves windows, linux and darwin.
//
// Hand-owned (no mprof_impl.go exists, so a reconvert never regenerates this file).
[module: go.GoManualConversion]

namespace go;

partial class runtime_package {

// ---- the guard's view (RuntimeBlockEventTests): GolibTests is outside runtime's InternalsVisibleTo
//      grant, so this Go-prefixed public helper exposes the one operation ----

/// <summary>Records <paramref name="count"/> block events of <paramref name="cycles"/> each through
/// <c>runtime.blockevent(cycles, 1)</c> -- the call runtime/pprof's TestBlockProfileBias makes.</summary>
[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
public static void GoBlockEventProbe(int64 cycles, nint count) {
    for (nint i = 0; i < count; i++) {
        blockevent(cycles, 1);
    }
}

} // end runtime_package
