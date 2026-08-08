// syscall_linux_impl.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// The scheduler brackets Go wraps every syscall in — and why the managed model's faithful
// realization of them is to do nothing.
//
// syscall_linux.go declares them as pulls from the runtime:
//
//     //go:linkname runtime_entersyscall runtime.entersyscall
//     func runtime_entersyscall()
//     //go:linkname runtime_exitsyscall runtime.exitsyscall
//     func runtime_exitsyscall()
//
// and every non-Raw wrapper in the package brackets its kernel call with the pair. With no body
// they took throwing PartialStubGenerator stubs, so the FIRST syscall any Linux program makes
// through the ordinary (non-Raw) path died — `fmt.Println` → os.File.Write → internal/poll →
// syscall.Write → syscall.Syscall → runtime_entersyscall, i.e. hello-world's actual output call.
//
// WHY NOT FORWARD. The pull has a real Go body, so forwarding is the shape this converter reaches
// for first (linknameForwardTargets). It is not available here, and not marginally: runtime's
// entersyscall opens with `getcallerfp()` / `getcallerpc()` / `getcallersp()` — raw-metal frame
// intrinsics with no managed realization — and hands them to reentersyscall, which drives the P
// state machine across `sched`, `mp.oldp` and `casgstatus`. None of that state is populated in the
// managed model. A forwarder would fault on the first intrinsic.
//
// WHY NOTHING IS THE RIGHT ANSWER, rather than a stub-shaped omission. The pair's entire job is to
// release the P around a blocking call so the scheduler can run other goroutines on another M
// while this one sits in the kernel, and to reacquire one afterwards. It is bookkeeping in the
// strict sense: both are `func()`, they compute nothing any caller consumes, and no converted code
// anywhere reads state they would have set. The obligation they discharge is real, but in a
// converted program the HOST already discharges it — a goroutine is a .NET thread, a blocking
// syscall blocks that thread exactly as the CLR expects, and the thread pool's own injection is
// what keeps other work running meanwhile. So this is not the "plausible answer" failure mode the
// project rules against (there is no answer to fabricate — the return type is void); it is the
// managed model already satisfying the contract by a different mechanism, which is the same
// judgment syscall_impl.cs records for runtimeSetenv/runtimeUnsetenv one folder up.
//
// SCOPE. This file deliberately covers only the pair the ordinary syscall path needs. The other
// bodyless declarations in syscall_linux.cs — rawSyscallNoError, rawVforkSyscall,
// runtime_doAllThreadsSyscall, cgocaller — are separate questions with separate answers (two are
// further raw bottoms, one is a whole-process operation, one is the cgo boundary) and are left as
// announcing stubs until something genuinely needs them. Catalogued in
// docs/phase4/FINDING-linux-run-layer.md §5.
//
// This file has no `<name>.go` counterpart, so a -stdlib reconvert never emits over it; the module
// marker states the ownership explicitly and matches the other hand-owned files in this package.
// It lives in linux/ rather than flat because the declarations it implements are linux-only
// (syscall_linux.go): a flat implementing part would have no defining declaration on Windows.

[module: go.GoManualConversion]

namespace go;

partial class syscall_package {

// Releases the P around a blocking kernel call. See the file header: the CLR's thread model
// already provides what this exists to provide, and the function returns nothing.
internal static partial void runtime_entersyscall() {
}

// Reacquires one afterwards. Same reasoning, same shape.
internal static partial void runtime_exitsyscall() {
}

} // end syscall_package
