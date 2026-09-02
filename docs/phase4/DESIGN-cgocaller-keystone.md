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

### 2.4 What §2 needs from the keystone

1. **`cgocaller(fnptr, args…) → uintptr`**, variadic over `uintptr`, returning libc's `errno`-style
   int (these libc wrappers return 0 or an error number; Go feeds the result to `errnoErr`).
2. **The nine `cgo_libc_*` pointers non-nil**, each resolving the libc symbol of the same name.
   `NativeLibrary.GetExport` over the already-bound libc handle is the obvious mechanism; the
   `[LibraryImport("libc")]` keystone at `internal/runtime/syscall/linux/syscall_linux_impl.cs`
   establishes the precedent and the musl caveat recorded there applies here unchanged.
3. **Nothing else.** No behavior change to `AllThreadsSyscall`, and none to
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

*Left for C2: `FuncPCABI0` + the syscall keystone sizing, per the coordinator's 2026-09-02 ruling.
The requirement §2.4 places on the shared piece is the variadic `uintptr` bridge plus symbol
resolution; §3 should state whether darwin needs a different arity, return convention, or symbol
lookup, and whether `runtime_doAllThreadsSyscall`'s `ENOTSUP` has a darwin analogue that must
likewise stay put.*

## 4. What is NOT proposed

- **Not** nine per-call hand-owns — the shape OQ-1 rejected, restated in the 2026-09-02 ruling.
- **Not** a change to `runtime_doAllThreadsSyscall`. Its `ENOTSUP` is correct for its own contract
  and three tests depend on it (§2.4.3).
- **Not** a general cgo layer. `cgocaller` is one bridge with one signature; nothing here proposes
  converting cgo C halves, which the converter cannot process regardless.

-- C1 (§1, §2; §3 pending C2)
