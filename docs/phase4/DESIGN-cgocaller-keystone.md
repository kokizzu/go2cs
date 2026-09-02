# DESIGN — the `cgocaller` keystone

> **Status: SIZING ONLY. No code.** Coordinator ruling 2026-09-02 took option (a) — the
> keystone-backed `cgocaller`, OQ-1's ruled shape — over nine per-call hand-owns, and directed that
> it be sized ONCE for both consumers because they need the same piece. This file is that single
> sizing. **§2 is C1's (the Linux credential wrappers); §3 is C2's (the darwin run layer) and is
> left for C2 to write.** Nothing here is implemented, and nothing should be until the sizing is
> ruled.

## 1. What `cgocaller` is, and why both consumers arrive at it

Go's `syscall` package declares one bodyless bridge:

```go
//go:uintptrescapes
func cgocaller(unsafe.Pointer, ...uintptr) uintptr
```

linked by `runtime/cgocall.go`. It exists so that a Go binary **linked with cgo** can route a call
through a libc function pointer rather than through the runtime — and Go reaches for it precisely
when the runtime cannot do the job itself. The converted corpus emits it bodyless, so
`PartialStubGenerator` gives it a throwing stub; `syscall_linux_impl.cs`'s header parks it
explicitly as "the cgo boundary … a separate question with a separate answer".

The observation that makes it a keystone rather than a one-package repair: **a managed host is
structurally a cgo host.** It has OS threads the Go runtime never created and cannot enumerate.
That is the exact condition Go's own source tests for when it chooses between its two
implementations of a call. So wherever Go says "if cgo, do it this way", the converted corpus
should almost always be taking the cgo branch — and today it cannot, because `cgocaller` throws.

## 2. Consumer A — the Linux credential wrappers (C1)

### 2.1 The population: nine functions, one shape

Every credential setter in `syscall_linux.go` carries two implementations selected on one nil test:

```go
func Setegid(egid int) (err error) {
	if cgo_libc_setegid == nil {
		if _, _, e1 := AllThreadsSyscall(SYS_SETRESGID, minus1, uintptr(egid), minus1); e1 != 0 { … }
	} else if ret := cgocaller(cgo_libc_setegid, uintptr(egid)); ret != 0 { … }
}
```

| function | syscall branch | cgo branch pointer | args |
|---|---|--:|--:|
| `Setegid` | `SYS_SETRESGID` | `cgo_libc_setegid` | 1 |
| `Seteuid` | `SYS_SETRESUID` | `cgo_libc_seteuid` | 1 |
| `Setgid` | `sys_SETGID` | `cgo_libc_setgid` | 1 |
| `Setuid` | `sys_SETUID` | `cgo_libc_setuid` | 1 |
| `Setregid` | `sys_SETREGID` | `cgo_libc_setregid` | 2 |
| `Setreuid` | `sys_SETREUID` | `cgo_libc_setreuid` | 2 |
| `Setresgid` | `sys_SETRESGID` | `cgo_libc_setresgid` | 3 |
| `Setresuid` | `sys_SETRESUID` | `cgo_libc_setresuid` | 3 |
| `Setgroups` | `_SYS_setgroups` | `cgo_libc_setgroups` | 2 (n, ptr) |

Nine functions, one call shape, arities 1–3 plus `Setgroups`'s `(n, *gid_t)`.

### 2.2 Why the current answer is wrong for exactly these callers

The converted corpus takes the `cgo_libc_* == nil` branch, reaching
`runtime_doAllThreadsSyscall`, whose **banked** hand-own answers `ENOTSUP` by design — correctly,
with a written rationale, for *that function*. The measurement (2026-09-02, syscall row at master
`64a064098`, pin verified) shows one `ENOTSUP` doing two opposite things in a single record:

| test | go | c# | why |
|---|---|---|---|
| `TestAllThreadsSyscall` | skip | skip | calls the raw API; **opens by skipping on `ENOTSUP`** |
| `TestAllThreadsSyscallError` | skip | skip | same guard |
| `TestAllThreadsSyscallBlockedSyscall` | skip | skip | same guard |
| `TestSetuidEtc` | **pass** | **fail** | **no `ENOTSUP` guard exists** |

`TestSetuidEtc`'s only skips are non-root, the swarming builder, and alpine. Go did not forget the
guard: **`ENOTSUP` is a legitimate answer for the raw API and an impossible one for the nine
wrappers**, because whichever branch a build takes the wrappers *work* — cgo routes to libc,
non-cgo to a runtime broadcast that really happens.

All 21 rows of the test's table fail with one string (`operation not supported`), across all nine
functions. That figure is the rooting's own control: it was predicted before the run on the theory
that the shared bottom is the root, and it held.

### 2.3 Acceptance evidence, measured rather than assumed

The design rests on a claim it would be cheap to assert and wrong to: *libc's `setegid` reaches
threads the caller did not create.* Measured with a lane-local probe — a thread parks, main calls
`setegid(1)`, both read their own `/proc/self/task/<tid>/status`:

```
BEFORE (uid=0):   main Gid: 0 0 0 0      thread Gid: 0 0 0 0
AFTER setegid(1): main Gid: 0 1 0 1      thread Gid: 0 1 0 1
```

The parked thread moved, and `0 1 0 1` is **byte-for-byte** what `TestSetuidEtc` compares against
for that row (`expect: "\t0\t1\t0\t1"`, filter `Gid:`). So this branch yields a **passing row**, not
a different error. glibc's nptl `setxid` broadcast is real on this host.

**Scope, stated because it is the one place this could fail:** the broadcast walks glibc's OWN
thread list, i.e. every `pthread_create` thread. .NET's Linux threads are pthreads, so CLR threads
are on it; a thread made by a bare `clone(2)` behind glibc's back would not be, and nothing in the
converted corpus makes one. The probe's parked thread is exactly the shape in question — foreign to
Go's runtime, ordinary to glibc.

**C2 then measured the same property from the MANAGED side, with a negative control, and that
supersedes the reading above as the acceptance evidence** (2026-09-02). One .NET process, a thread
parked on an event — running no managed code at the moment the credential changes, so nothing in
the runtime can be doing the work for us:

```
ARM 1 -- libc: setegid(1) through glibc                      3 runs, identical
  main egid: 0 -> 1     parked egid: 0 -> 1     <- FOLLOWED
ARM 2 -- raw: syscall(SYS_setresgid, -1, 1, -1)              3 runs, identical
  main egid: 0 -> 1     parked egid: 0 -> 0     <- did NOT follow
```

Arm 2 is what makes arm 1 mean anything: the probe demonstrably distinguishes the two mechanisms.
My C probe shows glibc broadcasts to pthreads; C2's shows the broadcast reaches **.NET's** threads,
which is the half Go's own `runtime/cgo/linux_syscall.c` comment cannot tell us.

**⚠ FRAMING CORRECTION, and it changes what this document may claim.** A plain
`[DllImport("libc")] setegid` ALREADY has the process-wide semantics — they come from calling libc
at all, **not** from the keystone. So nothing here should be read as "the keystone buys
all-threads semantics on Linux"; it does not, and a later reader drawing that conclusion would be
wrong about what is load-bearing. The keystone is chosen for the reason the ruling gave — it is the
same piece darwin needs (§3) — and that reason is untouched by this. On the Linux half the keystone
buys *uniformity with darwin*, not correctness that a `DllImport` lacks.

### 2.4 What §2 needs from the keystone

1. **`cgocaller(fnptr, args…) → uintptr`** — an indirect call to a native function pointer.
   ⚠ **NOT variadic, and my first draft was wrong to say so (corrected from C2, 2026-09-02):**
   .NET has no variadic indirect call. `Marshal.GetDelegateForFunctionPointer` (or `calli`) needs a
   FIXED signature, so a managed `cgocaller` is necessarily *a small family of arity-specialised
   delegates*. §2 needs arities 1, 2 and 3; §3 needs up to 9 with three result widths, so darwin
   sizes the family and Linux is a subset of it. This is the concrete reason the two consumers are
   one piece rather than two that merely rhyme.
2. **The return convention is Go's cgo shim's, not C's** — quoting `runtime/cgo/linux_syscall.c`,
   because a faithful port has to match it and it is not the usual convention:

   ```c
   #define SET_RETVAL(fn) \
     uintptr_t ret = (uintptr_t) fn ; \
     if (ret == (uintptr_t) -1) {     \
       x->retval = (uintptr_t) errno; \
     } else                           \
       x->retval = ret
   ```

   The shim returns **errno itself** on failure, not −1 — which is what lets `syscall_linux.go`
   write `if ret := cgocaller(...); ret != 0 { err = errnoErr(Errno(ret)) }`. It holds only because
   every one of these functions returns 0 on success; a nonzero success would be read as an errno.
   The same macro serves all nine (C2, from the Go source).
3. **The nine `cgo_libc_*` pointers non-nil**, each resolving the libc symbol of the same name.
   `NativeLibrary.GetExport` over the already-bound libc handle is the obvious mechanism; the
   `[LibraryImport("libc")]` keystone at `internal/runtime/syscall/linux/syscall_linux_impl.cs`
   establishes the precedent and the musl caveat recorded there applies here unchanged.
4. **Nothing else.** No behavior change to `AllThreadsSyscall`, and none to
   `runtime_doAllThreadsSyscall`: the three guards above call the raw API and must keep receiving
   `ENOTSUP`. That is a hard requirement of this design, not a side effect — the fix must move the
   nine wrappers *without* moving the raw API.

### 2.5 Blast radius and what banks

Nine functions in one package; one test (`TestSetuidEtc`, 21 assertions) moves from fail to pass.
The syscall row's Linux residue falls by one. The `ENOTSUP` hand-own stays banked as the disclosed
state until this lands, per the ruling.

**Open question for the ruling, stated rather than assumed away:** `Setgroups` passes a **pointer**
(`&a[0]`, a `*_Gid_t` into a Go slice). Under this corpus that is the same
managed-memory-by-address class that produced the `Exec` fork bomb on 2026-09-02 — so `Setgroups`
needs its argument marshalled into unmanaged memory for the duration of the call, exactly as the
`exec_unix.cs` posix_spawn seam does. The other eight pass scalars and do not. Whether that
marshalling belongs in `cgocaller` (which cannot know which arguments are pointers) or at the one
call site that has one is a design decision this section does not make.

## 3. Consumer B — the darwin run layer (C2)

*Left for C2 to write (`FuncPCABI0` + the syscall keystone sizing; the darwin sizing it extends is
already on master at `DESIGN-darwin-run-layer.md`).*

**C2 has established the structural argument for why this is ONE document, and it is stronger than
"sized once to save effort": `syscall.cgocaller` and darwin's ten keystones (`Syscall`, `Syscall6`,
`Syscall9`, `syscall`, `syscall6`, `syscall6X`, `syscallX`, `syscallPtr`, `rawSyscall`,
`rawSyscall6`) are the SAME primitive** — an indirect call to a native function pointer with N
`uintptr` arguments — differing on exactly three axes, each of which this document should state
once rather than twice:

| axis | Linux (§2) | darwin (§3) |
|---|---|---|
| how the fn pointer is obtained | package-level `unsafe.Pointer` (`cgo_libc_setegid`), cgo-populated at link time, **`nil` in our corpus** — which is why the ENOTSUP path runs | `abi.FuncPCABI0(libc_write_trampoline)`, **`0` today** |
| arity and result width | 1, 2, 3 | up to 9, three result widths |
| errno | folded into the return value by the shim (§2.4.2) | keystones set `libc_errno`, caller reads it |

Both pointer-acquisition rows reduce to one question — resolve a libc symbol to an address — with
one answer: `NativeLibrary.GetExport` over `libc.so.6` / `libSystem.B.dylib`. The arity row is why
the family is sized by darwin and Linux takes a subset. The errno row is a per-platform thin
wrapper over one shared call mechanism.

*§3 should also say whether `runtime_doAllThreadsSyscall`'s `ENOTSUP` has a darwin analogue that
must likewise stay put (§2.4.4).*

## 4. What is NOT proposed

- **Not** nine per-call hand-owns — the shape OQ-1 rejected, restated in the 2026-09-02 ruling.
- **Not** a change to `runtime_doAllThreadsSyscall`. Its `ENOTSUP` is correct for its own contract
  and three tests depend on it (§2.4.3).
- **Not** a general cgo layer. `cgocaller` is one bridge with one signature; nothing here proposes
  converting cgo C halves, which the converter cannot process regardless.

-- C1 (§1, §2; §3 pending C2)
