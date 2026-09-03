// Copyright 2011 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build aix || darwin || netbsd || openbsd || plan9 || solaris || windows
namespace go;

using atomic = @internal.runtime.atomic_package;
using @unsafe = unsafe_package;
using @internal.runtime;

partial class runtime_package {

// This implementation depends on OS-specific implementations of
//
//	func semacreate(mp *m)
//		Create a semaphore for mp, if it does not already have one.
//
//	func semasleep(ns int64) int32
//		If ns < 0, acquire m's semaphore and return 0.
//		If ns >= 0, try to acquire m's semaphore for at most ns nanoseconds.
//		Return 0 if the semaphore was acquired, -1 if interrupted or timed out.
//
//	func semawakeup(mp *m)
//		Wake up mp, which is or will soon be sleeping on its semaphore.
internal static uintptr locked => 1;

internal static UntypedInt active_spin => 4;

internal static UntypedInt active_spin_cnt => 30;

internal static UntypedInt passive_spin => 1;

// go2cs generated this placeholder — func mutexContended is hand-converted with managed semantics in the package's *_impl.cs ([module: GoManualConversion])

internal static void @lock(ж<mutex> Ꮡl) {
    lockWithRank(Ꮡl, getLockRank(Ꮡl));
}

// go2cs generated this placeholder — func lock2 is hand-converted with managed semantics in the package's *_impl.cs ([module: GoManualConversion])

internal static void unlock(ж<mutex> Ꮡl) {
    unlockWithRank(Ꮡl);
}

// go2cs generated this placeholder — func unlock2 is hand-converted with managed semantics in the package's *_impl.cs ([module: GoManualConversion])

// One-time notifications.
internal static void noteclear(ж<note> Ꮡn) {
    ref var n = ref Ꮡn.DerefOrNull();

    n.key = 0;
}

// go2cs generated this placeholder — func notewakeup is hand-converted with managed semantics in the package's *_impl.cs ([module: GoManualConversion])

// go2cs generated this placeholder — func notesleep is hand-converted with managed semantics in the package's *_impl.cs ([module: GoManualConversion])

// go2cs generated this placeholder — func notetsleep_internal is hand-converted with managed semantics in the package's *_impl.cs ([module: GoManualConversion])

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string notetsleepNotOnG0ˢ = "notetsleep not on g0"u8;

internal static bool notetsleep(ж<note> Ꮡn, int64 ns) {
    var gp = getg();
    if (gp != (~(~gp).m).g0) {
        @throw(notetsleepNotOnG0ˢ);
    }
    semacreate((~gp).m);
    return notetsleep_internal(Ꮡn, ns, nil, 0);
}

// go2cs generated this placeholder — func notetsleepg is hand-converted with managed semantics in the package's *_impl.cs ([module: GoManualConversion])

internal static (ж<g>, bool) beforeIdle(int64 _Δp0, int64 _Δp1) {
    return (default!, false);
}

internal static void checkTimeouts() {
}

} // end runtime_package
