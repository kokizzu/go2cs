// Copyright 2011 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
using go;

// Hand-finished conversion (lock_sema.go) — the SEMAPHORE flavor's one signature-specific entry
// point. Everything else in Go's mutex/note protocol converges on one managed implementation for
// every platform and lives in the flat lock_managed_impl.cs beside this folder; only
// notetsleep_internal differs in shape between Go's two flavors, so only it is per-GOOS.
//
// This file is compiled on windows AND darwin — Go selects lock_sema.go on both — so layout L3
// keeps one copy in each folder. The two copies must stay byte-identical; the reconvert merge
// refuses to reconcile hand-owned copies that have diverged, precisely so a fix applied to one
// flavor cannot disappear.
[module: GoManualConversion]

namespace go;

partial class runtime_package {

// lock_sema.go's notetsleep_internal carries the calling g and an ABSOLUTE deadline because its
// body re-arms semasleep after each interruption and must know how much of the budget is left
// (and, on the g0 path, which m's semaphore to park on). The managed core neither parks nor is
// interrupted — it polls a latch — so both are unused here and `ns` is the whole budget.
internal static bool notetsleep_internal(ж<note> Ꮡn, int64 ns, ж<g> Ꮡgp, int64 deadline) {
    return noteSleepDeadline(Ꮡn, ns);
}

} // end runtime_package
