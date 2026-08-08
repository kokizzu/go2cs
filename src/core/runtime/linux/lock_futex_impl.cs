// Copyright 2011 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
using go;

// Hand-finished conversion (lock_futex.go) — the FUTEX flavor's one signature-specific entry point,
// and the Linux sibling of windows/darwin/lock_sema_impl.cs.
//
// It is a sibling rather than a port. Go selects lock_futex.go on linux/freebsd/dragonfly and
// lock_sema.go elsewhere, and the two differ in their OS primitive (futexsleep/futexwakeup vs
// semasleep/semawakeup) and in their key encoding ({0,1,2} vs a tagged *m address). Neither
// primitive has a managed realization, so both flavors converge on the SAME managed model — a
// {0, keyLocked} latch with SpinWait escalation — which lives once in the flat lock_managed_impl.cs
// rather than twice. What genuinely differs is the shape of one declaration, and that is all this
// file carries.
//
// Without it, `runtime` for linux compiled a placeholder for every member of the cluster and left
// 27 call sites unresolved (CS0103 on notewakeup/notesleep/notetsleep_internal/lock2/unlock2),
// which took the entire Linux corpus down two layers below `os`.
[module: GoManualConversion]

namespace go;

partial class runtime_package {

// lock_futex.go's notetsleep_internal is TWO parameters where lock_sema.go's is four: the futex
// wait needs neither the calling g nor a re-armed absolute deadline, because futexsleep takes the
// remaining budget directly. The managed core polls a latch, so it wants exactly this shape — the
// delegation is an identity, not an adaptation, and the four-parameter flavor is the one that
// discards arguments.
internal static bool notetsleep_internal(ж<note> Ꮡn, int64 ns) {
    return noteSleepDeadline(Ꮡn, ns);
}

} // end runtime_package
