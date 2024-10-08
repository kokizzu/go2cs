// Copyright 2020 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

//go:build goexperiment.staticlockranking
// +build goexperiment.staticlockranking

// package runtime -- go2cs converted at 2022 March 13 05:24:37 UTC
// import "runtime" ==> using runtime = go.runtime_package
// Original source: C:\Program Files\Go\src\runtime\lockrank_on.go
namespace go;

using atomic = runtime.@internal.atomic_package;
using @unsafe = @unsafe_package;


// worldIsStopped is accessed atomically to track world-stops. 1 == world
// stopped.

using System;
public static partial class runtime_package {

private static uint worldIsStopped = default;

// lockRankStruct is embedded in mutex
private partial struct lockRankStruct {
    public lockRank rank; // pad field to make sure lockRankStruct is a multiple of 8 bytes, even on
// 32-bit systems.
    public nint pad;
}

private static void lockInit(ptr<mutex> _addr_l, lockRank rank) {
    ref mutex l = ref _addr_l.val;

    l.rank = rank;
}

private static lockRank getLockRank(ptr<mutex> _addr_l) {
    ref mutex l = ref _addr_l.val;

    return l.rank;
}

// lockWithRank is like lock(l), but allows the caller to specify a lock rank
// when acquiring a non-static lock.
//
// Note that we need to be careful about stack splits:
//
// This function is not nosplit, thus it may split at function entry. This may
// introduce a new edge in the lock order, but it is no different from any
// other (nosplit) call before this call (including the call to lock() itself).
//
// However, we switch to the systemstack to record the lock held to ensure that
// we record an accurate lock ordering. e.g., without systemstack, a stack
// split on entry to lock2() would record stack split locks as taken after l,
// even though l is not actually locked yet.
private static void lockWithRank(ptr<mutex> _addr_l, lockRank rank) {
    ref mutex l = ref _addr_l.val;

    if (l == _addr_debuglock || l == _addr_paniclk) { 
        // debuglock is only used for println/printlock(). Don't do lock
        // rank recording for it, since print/println are used when
        // printing out a lock ordering problem below.
        //
        // paniclk is only used for fatal throw/panic. Don't do lock
        // ranking recording for it, since we throw after reporting a
        // lock ordering problem. Additionally, paniclk may be taken
        // after effectively any lock (anywhere we might panic), which
        // the partial order doesn't cover.
        lock2(l);
        return ;
    }
    if (rank == 0) {
        rank = lockRankLeafRank;
    }
    var gp = getg(); 
    // Log the new class.
    systemstack(() => {
        var i = gp.m.locksHeldLen;
        if (i >= len(gp.m.locksHeld)) {
            throw("too many locks held concurrently for rank checking");
        }
        gp.m.locksHeld[i].rank = rank;
        gp.m.locksHeld[i].lockAddr = uintptr(@unsafe.Pointer(l));
        gp.m.locksHeldLen++; 

        // i is the index of the lock being acquired
        if (i > 0) {
            checkRanks(_addr_gp, gp.m.locksHeld[i - 1].rank, rank);
        }
        lock2(l);
    });
}

// nosplit to ensure it can be called in as many contexts as possible.
//go:nosplit
private static void printHeldLocks(ptr<g> _addr_gp) {
    ref g gp = ref _addr_gp.val;

    if (gp.m.locksHeldLen == 0) {
        println("<none>");
        return ;
    }
    foreach (var (j, held) in gp.m.locksHeld[..(int)gp.m.locksHeldLen]) {
        println(j, ":", held.rank.String(), held.rank, @unsafe.Pointer(gp.m.locksHeld[j].lockAddr));
    }
}

// acquireLockRank acquires a rank which is not associated with a mutex lock
//
// This function may be called in nosplit context and thus must be nosplit.
//go:nosplit
private static void acquireLockRank(lockRank rank) {
    var gp = getg(); 
    // Log the new class. See comment on lockWithRank.
    systemstack(() => {
        var i = gp.m.locksHeldLen;
        if (i >= len(gp.m.locksHeld)) {
            throw("too many locks held concurrently for rank checking");
        }
        gp.m.locksHeld[i].rank = rank;
        gp.m.locksHeld[i].lockAddr = 0;
        gp.m.locksHeldLen++; 

        // i is the index of the lock being acquired
        if (i > 0) {
            checkRanks(_addr_gp, gp.m.locksHeld[i - 1].rank, rank);
        }
    });
}

// checkRanks checks if goroutine g, which has mostly recently acquired a lock
// with rank 'prevRank', can now acquire a lock with rank 'rank'.
//
//go:systemstack
private static void checkRanks(ptr<g> _addr_gp, lockRank prevRank, lockRank rank) {
    ref g gp = ref _addr_gp.val;

    var rankOK = false;
    if (rank < prevRank) { 
        // If rank < prevRank, then we definitely have a rank error
        rankOK = false;
    }
    else if (rank == lockRankLeafRank) { 
        // If new lock is a leaf lock, then the preceding lock can
        // be anything except another leaf lock.
        rankOK = prevRank < lockRankLeafRank;
    }
    else
 { 
        // We've now verified the total lock ranking, but we
        // also enforce the partial ordering specified by
        // lockPartialOrder as well. Two locks with the same rank
        // can only be acquired at the same time if explicitly
        // listed in the lockPartialOrder table.
        var list = lockPartialOrder[rank];
        foreach (var (_, entry) in list) {
            if (entry == prevRank) {
                rankOK = true;
                break;
            }
        }
    }
    if (!rankOK) {
        printlock();
        println(gp.m.procid, " ======");
        printHeldLocks(_addr_gp);
        throw("lock ordering problem");
    }
}

// See comment on lockWithRank regarding stack splitting.
private static void unlockWithRank(ptr<mutex> _addr_l) {
    ref mutex l = ref _addr_l.val;

    if (l == _addr_debuglock || l == _addr_paniclk) { 
        // See comment at beginning of lockWithRank.
        unlock2(l);
        return ;
    }
    var gp = getg();
    systemstack(() => {
        var found = false;
        for (var i = gp.m.locksHeldLen - 1; i >= 0; i--) {
            if (gp.m.locksHeld[i].lockAddr == uintptr(@unsafe.Pointer(l))) {
                found = true;
                copy(gp.m.locksHeld[(int)i..(int)gp.m.locksHeldLen - 1], gp.m.locksHeld[(int)i + 1..(int)gp.m.locksHeldLen]);
                gp.m.locksHeldLen--;
                break;
            }
        }
        if (!found) {
            println(gp.m.procid, ":", l.rank.String(), l.rank, l);
            throw("unlock without matching lock acquire");
        }
        unlock2(l);
    });
}

// releaseLockRank releases a rank which is not associated with a mutex lock
//
// This function may be called in nosplit context and thus must be nosplit.
//go:nosplit
private static void releaseLockRank(lockRank rank) {
    var gp = getg();
    systemstack(() => {
        var found = false;
        for (var i = gp.m.locksHeldLen - 1; i >= 0; i--) {
            if (gp.m.locksHeld[i].rank == rank && gp.m.locksHeld[i].lockAddr == 0) {
                found = true;
                copy(gp.m.locksHeld[(int)i..(int)gp.m.locksHeldLen - 1], gp.m.locksHeld[(int)i + 1..(int)gp.m.locksHeldLen]);
                gp.m.locksHeldLen--;
                break;
            }
        }
        if (!found) {
            println(gp.m.procid, ":", rank.String(), rank);
            throw("lockRank release without matching lockRank acquire");
        }
    });
}

// See comment on lockWithRank regarding stack splitting.
private static void lockWithRankMayAcquire(ptr<mutex> _addr_l, lockRank rank) {
    ref mutex l = ref _addr_l.val;

    var gp = getg();
    if (gp.m.locksHeldLen == 0) { 
        // No possibility of lock ordering problem if no other locks held
        return ;
    }
    systemstack(() => {
        var i = gp.m.locksHeldLen;
        if (i >= len(gp.m.locksHeld)) {
            throw("too many locks held concurrently for rank checking");
        }
        gp.m.locksHeld[i].rank = rank;
        gp.m.locksHeld[i].lockAddr = uintptr(@unsafe.Pointer(l));
        gp.m.locksHeldLen++;
        checkRanks(_addr_gp, gp.m.locksHeld[i - 1].rank, rank);
        gp.m.locksHeldLen--;
    });
}

// nosplit to ensure it can be called in as many contexts as possible.
//go:nosplit
private static bool checkLockHeld(ptr<g> _addr_gp, ptr<mutex> _addr_l) {
    ref g gp = ref _addr_gp.val;
    ref mutex l = ref _addr_l.val;

    for (var i = gp.m.locksHeldLen - 1; i >= 0; i--) {
        if (gp.m.locksHeld[i].lockAddr == uintptr(@unsafe.Pointer(l))) {
            return true;
        }
    }
    return false;
}

// assertLockHeld throws if l is not held by the caller.
//
// nosplit to ensure it can be called in as many contexts as possible.
//go:nosplit
private static void assertLockHeld(ptr<mutex> _addr_l) {
    ref mutex l = ref _addr_l.val;

    var gp = getg();

    var held = checkLockHeld(_addr_gp, _addr_l);
    if (held) {
        return ;
    }
    systemstack(() => {
        printlock();
        print("caller requires lock ", l, " (rank ", l.rank.String(), "), holding:\n");
        printHeldLocks(_addr_gp);
        throw("not holding required lock!");
    });
}

// assertRankHeld throws if a mutex with rank r is not held by the caller.
//
// This is less precise than assertLockHeld, but can be used in places where a
// pointer to the exact mutex is not available.
//
// nosplit to ensure it can be called in as many contexts as possible.
//go:nosplit
private static void assertRankHeld(lockRank r) {
    var gp = getg();

    for (var i = gp.m.locksHeldLen - 1; i >= 0; i--) {
        if (gp.m.locksHeld[i].rank == r) {
            return ;
        }
    } 

    // Crash from system stack to avoid splits that may cause
    // additional issues.
    systemstack(() => {
        printlock();
        print("caller requires lock with rank ", r.String(), "), holding:\n");
        printHeldLocks(_addr_gp);
        throw("not holding required lock!");
    });
}

// worldStopped notes that the world is stopped.
//
// Caller must hold worldsema.
//
// nosplit to ensure it can be called in as many contexts as possible.
//go:nosplit
private static void worldStopped() {
    {
        var stopped = atomic.Xadd(_addr_worldIsStopped, 1);

        if (stopped != 1) {
            systemstack(() => {
                print("world stop count=", stopped, "\n");
                throw("recursive world stop");
            });
        }
    }
}

// worldStarted that the world is starting.
//
// Caller must hold worldsema.
//
// nosplit to ensure it can be called in as many contexts as possible.
//go:nosplit
private static void worldStarted() {
    {
        var stopped = atomic.Xadd(_addr_worldIsStopped, -1);

        if (stopped != 0) {
            systemstack(() => {
                print("world stop count=", stopped, "\n");
                throw("released non-stopped world stop");
            });
        }
    }
}

// nosplit to ensure it can be called in as many contexts as possible.
//go:nosplit
private static bool checkWorldStopped() {
    var stopped = atomic.Load(_addr_worldIsStopped);
    if (stopped > 1) {
        systemstack(() => {
            print("inconsistent world stop count=", stopped, "\n");
            throw("inconsistent world stop count");
        });
    }
    return stopped == 1;
}

// assertWorldStopped throws if the world is not stopped. It does not check
// which M stopped the world.
//
// nosplit to ensure it can be called in as many contexts as possible.
//go:nosplit
private static void assertWorldStopped() {
    if (checkWorldStopped()) {
        return ;
    }
    throw("world not stopped");
}

// assertWorldStoppedOrLockHeld throws if the world is not stopped and the
// passed lock is not held.
//
// nosplit to ensure it can be called in as many contexts as possible.
//go:nosplit
private static void assertWorldStoppedOrLockHeld(ptr<mutex> _addr_l) {
    ref mutex l = ref _addr_l.val;

    if (checkWorldStopped()) {
        return ;
    }
    var gp = getg();
    var held = checkLockHeld(_addr_gp, _addr_l);
    if (held) {
        return ;
    }
    systemstack(() => {
        printlock();
        print("caller requires world stop or lock ", l, " (rank ", l.rank.String(), "), holding:\n");
        println("<no world stop>");
        printHeldLocks(_addr_gp);
        throw("no world stop or required lock!");
    });
}

} // end runtime_package
