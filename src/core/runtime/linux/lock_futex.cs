// Copyright 2011 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build dragonfly || freebsd || linux
namespace go;

using atomic = @internal.runtime.atomic_package;
using @unsafe = unsafe_package;
using @internal.runtime;

partial class runtime_package {

// This implementation depends on OS-specific implementations of
//
//	futexsleep(addr *uint32, val uint32, ns int64)
//		Atomically,
//			if *addr == val { sleep }
//		Might be woken up spuriously; that's allowed.
//		Don't sleep longer than ns; ns < 0 means forever.
//
//	futexwakeup(addr *uint32, cnt uint32)
//		If any procs are sleeping on addr, wake up at most cnt.
internal static UntypedInt mutex_unlocked => 0;
internal static UntypedInt mutex_locked => 1;
internal static UntypedInt mutex_sleeping => 2;
internal static UntypedInt active_spin => 4;
internal static UntypedInt active_spin_cnt => 30;
internal static UntypedInt passive_spin => 1;

// Possible lock states are mutex_unlocked, mutex_locked and mutex_sleeping.
// mutex_sleeping means that there is presumably at least one sleeping thread.
// Note that there can be spinning threads during all states - they do not
// affect mutex's state.

// We use the uintptr mutex.key and note.key as a uint32.
//
//go:nosplit
internal static ж<uint32> key32(ж<uintptr> Ꮡp) {
    return Ꮡp.Reinterpret<uintptr, uint32>();
}

// go2cs generated this placeholder — func mutexContended is hand-converted with managed semantics in the package's *_impl.cs ([module: GoManualConversion])

internal static void @lock(ж<mutex> Ꮡl) {
    lockWithRank(Ꮡl, getLockRank(Ꮡl));
}

// go2cs generated this placeholder — func lock2 is hand-converted with managed semantics in the package's *_impl.cs ([module: GoManualConversion])

// Speculative grab for lock.
// wait is either MUTEX_LOCKED or MUTEX_SLEEPING
// depending on whether there is a thread sleeping
// on this mutex. If we ever change l->key from
// MUTEX_SLEEPING to some other value, we must be
// careful to change it back to MUTEX_SLEEPING before
// returning, to ensure that the sleeping thread gets
// its wakeup call.
// On uniprocessors, no point spinning.
// On multiprocessors, spin for ACTIVE_SPIN attempts.
// Try for lock, spinning.
// Try for lock, rescheduling.
// Sleep.
internal static void unlock(ж<mutex> Ꮡl) {
    unlockWithRank(Ꮡl);
}

// go2cs generated this placeholder — func unlock2 is hand-converted with managed semantics in the package's *_impl.cs ([module: GoManualConversion])

// restore the preemption request in case we've cleared it in newstack

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

// Sleep for an arbitrary-but-moderate interval to poll libc interceptors.
// Sleep for an arbitrary-but-moderate interval to poll libc interceptors.
internal static bool notetsleep(ж<note> Ꮡn, int64 ns) {
    var gp = getg();
    if (gp != (~(~gp).m).g0 && (~(~gp).m).preemptoff != ""u8) {
        @throw(notetsleepNotOnG0ˢ);
    }
    return notetsleep_internal(Ꮡn, ns);
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string notetsleepgOnG0ˢ = "notetsleepg on g0"u8;

// same as runtime·notetsleep, but called on user g (not g0)
// calls only nosplit functions between entersyscallblock/exitsyscall.
internal static bool notetsleepg(ж<note> Ꮡn, int64 ns) {
    var gp = getg();
    if (gp == (~(~gp).m).g0) {
        @throw(notetsleepgOnG0ˢ);
    }
    entersyscallblock();
    var ok = notetsleep_internal(Ꮡn, ns);
    exitsyscall();
    return ok;
}

internal static (ж<g>, bool) beforeIdle(int64 _Δp0, int64 _Δp1) {
    return (default!, false);
}

internal static void checkTimeouts() {
}

} // end runtime_package
