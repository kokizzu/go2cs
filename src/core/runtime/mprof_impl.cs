// mprof_impl.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// SPDX-License-Identifier: BSD-3-Clause
// Use of this source code is governed by a BSD-style license
// that can be found in the LICENSE file.

// The block/mutex/memory profile BUCKET STORE in managed storage, and runtime.saveblockevent over it.
// Increment I2 of docs/phase4/DESIGN-managed-profiling.md, the managed bucket store ruling 8fc439415f (B)
// banked and COORD ordered cut on 2026-09-26. Hand-owned by manualConversionFuncs["runtime"] (goosAny):
// newBucket, bucket.stk, bucket.mp, bucket.bp, stkbucket and saveblockevent. Declared once in mprof.go for
// every target, so these bodies serve windows, linux and darwin.
//
// WHAT GO'S STORE IS, AND WHY IT COULD NOT BE CONVERTED. newBucket persistentallocs ONE block holding a
// 48-byte bucket header whose next / allnext are *bucket (reference-bearing), followed by the stk array
// (nstk words), followed by the blockRecord (or the memRecord). bucket.stk(), bp() and mp() reach the two
// trailing parts by BYTE OFFSET (bucket + 48 + nstk*8), and buckhash is a sysAlloc'd native array of
// *bucket. That is the arm-2a class: a reference-bearing pointee at a Go-layout byte offset into storage
// the CLR lays out itself. The converted saveblockevent refused by name for that reason (COORD ruling (A),
// 2026-09-22).
//
// WHAT THIS STORE IS. The bucket header is an ordinary managed box; its stack and its record live in a side
// record keyed by that box, which the three accessors return. The hash is a managed array of boxes chained
// through bucket.next exactly as Go chains them. The ALL-BUCKETS lists (mbuckets / bbuckets / xbuckets)
// keep Go's shape, an atomic.UnsafePointer holding the newest bucket threaded through allnext, stored with
// unsafe.Pointer.FromPinnedBox (the retaining door) and read back by the converted readers'
// (*bucket)(p) conversion, which resolves the token to the same box. So blockProfileInternal,
// mutexProfileInternal, memProfileInternal and iterate_memprof stay CONVERTED and walk the store unchanged.
// Buckets are never freed, in Go or here.
//
// saveblockevent takes Go's `callers` branch (the frame-pointer branch reads getfp(), which has no managed
// answer): the hand-owned callers (managed_impl.cs) projects the CLR stack onto Go-logical frames with
// synthetic PCs, measured capturing the recording frame by name at 8fc439415f.
//
// debug.profstackdepth. Go sets it (default 128) in parsedebugvars on the schedinit path, which this host
// never runs, so it read 0: the recorders returned before recording and every reader sized its stack
// buffer from it (makeProfStack) and expanded nothing. The module initializer below gives it Go's default,
// the one debug variable the profile paths read. GODEBUG=profstackdepth=N is not parsed on this host, as
// no GODEBUG setting is.
//
// Hand-owned (no mprof_impl.go exists, so a reconvert never regenerates this file).
[module: go.GoManualConversion]

namespace go;

using System.Runtime.CompilerServices;
using System.Threading;
using atomic = @internal.runtime.atomic_package;
using @unsafe = unsafe_package;
using @internal.runtime;

partial class runtime_package {

// Go's dbgvars default for profstackdepth (runtime1.go), applied because parsedebugvars never runs here.
[ModuleInitializer]
internal static void initProfStackDepth() {
    if (debug.profstackdepth == 0) {
        debug.profstackdepth = 128;
    }
}

// The trailing parts of Go's one-block bucket, held beside the header instead of after it.
private sealed class bucketParts {
    internal required slice<uintptr> stk;
    internal ж<memRecord>? mp;
    internal ж<blockRecord>? bp;
}

private static readonly ConditionalWeakTable<ж<bucket>, bucketParts> s_bucketParts = new();

// buckhash as a managed array of chain heads. Written under profInsertLock, read without it, as Go does.
private static ж<bucket>?[]? s_buckhash;

internal static ж<bucket> newBucket(bucketType typ, nint nstk) {
    var parts = new bucketParts { stk = new slice<uintptr>((int)nstk) };

    if (typ == memProfile) {
        parts.mp = Ꮡ(new memRecord());
    }
    else if (typ == blockProfile || typ == mutexProfile) {
        parts.bp = Ꮡ(new blockRecord());
    }
    else {
        @throw("invalid profile bucket type"u8);
    }

    var b = Ꮡ(new bucket());
    b.Value.typ = typ;
    b.Value.nstk = (uintptr)nstk;
    s_bucketParts.Add(b, parts);
    return b;
}

private static bucketParts partsOf(ж<bucket> b) {
    if (!s_bucketParts.TryGetValue(b, out bucketParts? parts)) {
        @throw("runtime: profile bucket not allocated by newBucket"u8);
    }
    return parts!;
}

internal static slice<uintptr> stk(this ж<bucket> Ꮡb) {
    if (Ꮡb.Value.nstk > maxProfStackDepth) {
        @throw("bad profile stack count"u8);
    }
    return partsOf(Ꮡb).stk;
}

internal static ж<memRecord> mp(this ж<bucket> Ꮡb) {
    if (Ꮡb.Value.typ != memProfile) {
        @throw("bad use of bucket.mp"u8);
    }
    return partsOf(Ꮡb).mp!;
}

internal static ж<blockRecord> bp(this ж<bucket> Ꮡb) {
    if (Ꮡb.Value.typ != blockProfile && Ꮡb.Value.typ != mutexProfile) {
        @throw("bad use of bucket.bp"u8);
    }
    return partsOf(Ꮡb).bp!;
}

internal static ж<bucket> stkbucket(bucketType typ, uintptr size, slice<uintptr> stk, bool alloc) {
    var bh = Volatile.Read(ref s_buckhash);
    if (bh is null) {
        @lock(ᏑprofInsertLock);
        bh = s_buckhash;
        if (bh is null) {
            bh = new ж<bucket>?[(int)buckHashSize];
            Volatile.Write(ref s_buckhash, bh);
        }
        unlock(ᏑprofInsertLock);
    }

    // Hash stack.
    uintptr h = default!;
    foreach (var (_, pc) in stk) {
        h += pc;
        h += (h << 10);
        h ^= (uintptr)(h >> 6);
    }
    // hash in size
    h += size;
    h += (h << 10);
    h ^= (uintptr)(h >> 6);
    // finalize
    h += (h << 3);
    h ^= (uintptr)(h >> 11);

    nint i = (nint)(h % (uintptr)buckHashSize);
    // first check optimistically, without the lock
    for (var b = Volatile.Read(ref bh[i]); b is not null && b != nil; b = b.Value.next) {
        if (b.Value.typ == typ && b.Value.hash == h && b.Value.size == size && eqslice(b.stk(), stk)) {
            return b;
        }
    }

    if (!alloc) {
        return default!;
    }

    @lock(ᏑprofInsertLock);
    // check again under the insertion lock
    for (var b = bh[i]; b is not null && b != nil; b = b.Value.next) {
        if (b.Value.typ == typ && b.Value.hash == h && b.Value.size == size && eqslice(b.stk(), stk)) {
            unlock(ᏑprofInsertLock);
            return b;
        }
    }

    // Create new bucket.
    var nb = newBucket(typ, len(stk));
    copy(nb.stk(), stk);
    nb.Value.hash = h;
    nb.Value.size = size;

    ж<atomic.UnsafePointer> allnext = default!;
    if (typ == memProfile) {
        allnext = Ꮡmbuckets;
    }
    else if (typ == mutexProfile) {
        allnext = Ꮡxbuckets;
    }
    else {
        allnext = Ꮡbbuckets;
    }

    nb.Value.next = bh[i] ?? (ж<bucket>)nil;
    nb.Value.allnext = (ж<bucket>)(uintptr)(allnext.Load());

    // Publish the bucket to its hash chain and its all-buckets list, as Go's two StoreNoWB do.
    Volatile.Write(ref bh[i], nb);
    allnext.StoreNoWB(@unsafe.Pointer.FromPinnedBox(nb));

    unlock(ᏑprofInsertLock);
    return nb;
}

internal static void saveblockevent(int64 cycles, int64 rate, nint skip, bucketType which) {
    if (debug.profstackdepth == 0) {
        // profstackdepth is set to 0 by the user, so no stack can be recorded (Go's own early return).
        return;
    }
    if (skip > maxSkip) {
        @throw("invalid skip value"u8);
    }

    // Go's callers branch into its own buffer (Go: mp.profStack, 1 + maxSkip + profstackdepth words).
    var stk = new slice<uintptr>(1 + (int)maxSkip + (int)debug.profstackdepth);
    nint nstk = callers(skip, stk);

    saveBlockEventStack(cycles, rate, stk[..(int)nstk], which);
}

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
