# DESIGN — the `cgocaller` keystone

> **Status: SIZING ONLY. No code.** Coordinator ruling 2026-09-02 took option (a) — the
> keystone-backed `cgocaller`, OQ-1's ruled shape — over nine per-call hand-owns, and directed that
> it be sized ONCE for both consumers because they need the same piece. This file is that single
> sizing. **§2 is C1's (the Linux credential wrappers); §3 is C2's (the darwin run layer).**
> Nothing here is implemented, and nothing should be until the sizing is ruled.

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

### 1.1 The two consumers are the SAME primitive, differing on three axes

`syscall.cgocaller` and darwin's ten keystones (`Syscall`, `Syscall6`, `Syscall9`, `syscall`,
`syscall6`, `syscall6X`, `syscallX`, `syscallPtr`, `rawSyscall`, `rawSyscall6`) are one primitive —
an indirect call to a native function pointer with N `uintptr` arguments — differing on exactly
three axes, each stated here once rather than twice below:

| axis | Linux (§2) | darwin (§3) |
|---|---|---|
| how the fn pointer is obtained | package-level `unsafe.Pointer` (`cgo_libc_setegid`), cgo-populated at link time, **`nil` in our corpus** — which is why the ENOTSUP path runs | `abi.FuncPCABI0(libc_write_trampoline)`, **`0` today** |
| arity and result width | 1, 2, 3 | up to 9, three result widths |
| errno | folded into the return value by the shim (§2.4.2) | keystones set `libc_errno` via `__error()`, caller reads it (§3.4.2) |

Both pointer-acquisition rows reduce to one question — resolve a libc symbol to an address — with
one answer: `NativeLibrary.GetExport` over `libc.so.6` / `libSystem.B.dylib`. The arity row is why
the family is **sized by darwin**, with Linux taking a subset: .NET has no variadic indirect call,
so a managed `cgocaller` is necessarily a family of arity-specialised delegates rather than one
function. The errno row is a per-platform thin wrapper over one shared call mechanism.

---

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

**`Setgroups`'s pointer argument — RULED 2026-09-02, at the CALL SITE.** `Setgroups` passes a
**pointer** (`&a[0]`, a `*_Gid_t` into a Go slice); the other eight pass scalars. Under this corpus
that pointer is the same managed-memory-by-address class that produced the `Exec` fork bomb the same
day, so it must be marshalled into unmanaged memory for the duration of the call.

The ruling puts that marshalling **at the one call site that has a pointer, not in `cgocaller`**, and
the reason is structural rather than stylistic: `cgocaller` takes `uintptr`s. It cannot distinguish a
pointer argument from an integer one, so it could not do the marshalling correctly even if it were
the tidier place — it would have to guess. `cgocaller` therefore stays **pointer-agnostic**, and
`Setgroups` marshals its `gid_t` array exactly as `exec_unix.cs`'s `Exec` marshals argv/envp:
unmanaged for the duration, freed in a `finally`.

This is the same shape as the seam rule the corpus already carries — every buffer handed to a native
call lives in unmanaged memory for the duration of the call — applied one function over, and it means
the keystone's contract stays a pure `uintptr` bridge with no pointer semantics anywhere in it.

## 3. Consumer B — the darwin run layer (C2)

The full darwin sizing is on master at [`DESIGN-darwin-run-layer.md`](DESIGN-darwin-run-layer.md)
(census, ABI read off `sys_darwin_amd64.s`, cost, gates, open questions) and is **not repeated
here**. This section says only what the SHARED keystone must provide for darwin, and answers the
two questions §2 left for it.

### 3.1 Why darwin arrives at the same primitive, and by a much wider door

Linux reaches this bridge for **nine functions**. Darwin reaches it for **everything**: Go's darwin
port has no trap numbers at all, so every syscall is a libc call through an assembly trampoline
whose address is taken with `abi.FuncPCABI0` and handed to a keystone. Measured at master
`e4c5b5b8`:

| | count |
|:--|--:|
| `libc_*_trampoline` declarations, `syscall/darwin` | **126** |
| `libc_*_trampoline` declarations, whole darwin flavor | **142** |
| `//go:cgo_import_dynamic` pragmas in `syscall/zsyscall_darwin_amd64.go` | **123** |
| distinct libSystem symbols across ALL darwin Go sources in GOROOT | **267** |
| bodyless partials in `fmt`'s darwin closure (51 packages) | **255** |

So the consequence for §1's framing is sharper on darwin than on Linux: a converted darwin program
does not fail when it does something unusual, it fails **when it starts**. The first casualty is
pinned — `syscall.init()` → `Getrlimit` → `rawSyscall` — which is one package earlier than either
`os` or `fmt`, so neither is the right scope unit.

### 3.2 The ten keystones collapse to three axes, not ten bodies

`Syscall`, `Syscall6`, `Syscall9`, `syscall`, `syscall6`, `syscall6X`, `syscallX`, `syscallPtr`,
`rawSyscall`, `rawSyscall6` differ on **arity** (3/6/9), **result width** (32-bit / 64-bit /
pointer) and **raw-vs-cooked**. Read off `TEXT runtime·syscall(SB)`, the width axis is the *only*
reason the `X` variants exist — `syscall`/`syscall6` compare the low 32 bits (`CMPL`),
`syscallX`/`syscall6X` compare all 64 (`CMPQ`), `syscallPtr` treats NULL as the error. One
parameterized managed helper covers all ten.

Three ABI facts that make this smaller than it looks, each answering a question a reader will have:

- **The g0 struct-pointer marshalling does not survive into the managed form.** Go's keystone takes
  a pointer to `{fn, a1, a2, a3, r1, r2, err}` because it is called on the g0 stack via `libcCall`.
  There is no g0 here; the arguments arrive as ordinary `uintptr` parameters on the emitted
  partial's own signature. The managed shape is *simpler* than the Go original.
- **There is no vararg problem.** `XORL AX, AX` is the System V AMD64 convention for "no vector
  registers used", and a fixed `uintptr`-only indirect call satisfies it exactly. The four
  genuinely variadic libc entries the corpus reaches (`ioctl`, `fcntl`, `open`, `openat`) are each
  called through one fixed arity, and Go calls them the same way.
- **No struct-by-value returns anywhere in the family.** Every keystone returns
  `(uintptr r1, uintptr r2, Errno err)`; the pair is `AX`/`DX`.

### 3.3 `FuncPCABI0` is the half nobody notices is missing

`src/core/internal/abi/funcpc_impl.cs` is a three-line hand-own whose entire body is
`return default;` — i.e. **0**. It compiles, it returns a plausible value, and it is wrong. So even
a perfect keystone would be handed a null function pointer today; the trampoline mechanism is
unimplemented end to end, not missing one entry point.

The map it needs is **derivable twice over, and the two derivations cross-check each other for
free**: every pragma has the form
`//go:cgo_import_dynamic libc_<n> <sym> "/usr/lib/libSystem.B.dylib"`, all 123 were compared and
`<n>` equals `<sym>` with **zero mismatches**, and the converter *preserves the pragma* into the
emitted C# (123 comment lines in `zsyscall_darwin_amd64.cs`). So the implementation can derive the
map from the trampoline name AND from the pragma and assert they agree — a standing guard that
costs nothing and is worth more than a map that merely works today.

### 3.4 What §3 needs from the keystone

1. **Arities up to 9 and three result widths.** This is the row that sizes the family; §2's 1/2/3
   is a subset. Same primitive, wider.
2. **Errno by `__error()`, not folded into the return.** `result == -1` ⇒ call `__error()`
   (imported as `libc_error`) and dereference the returned `int*`. This is the axis where the two
   consumers genuinely differ (§2.4.2's shim folds errno into the return value), and it is a thin
   per-platform wrapper over one shared call mechanism — not two mechanisms.
3. **A real `FuncPCABI0`** resolving trampoline → symbol → `NativeLibrary.GetExport` over
   `/usr/lib/libSystem.B.dylib`. `os/darwin/dir_darwin_impl.cs` already proves that mechanism for
   one symbol.
4. **Pointer arguments marshalled at their CALL SITES, per COORD's `Setgroups` ruling (§2.5) —
   which lands on darwin's CRITICAL PATH, not on an edge case.** The ruling is that `cgocaller`
   stays pointer-agnostic because it takes `uintptr`s and cannot tell a pointer from an integer;
   darwin's keystones take `uintptr`s for the same reason, so it transfers verbatim. What makes it
   urgent here rather than incidental is WHICH call needs it first. The pinned first casualty is
   `syscall.init()` → `Getrlimit`, and its emitted body is

   ```csharp
   // syscall/darwin/zsyscall_darwin_amd64.cs:871
   var (_, _, e1) = rawSyscall(abi.FuncPCABI0(libc_getrlimit_trampoline), (uintptr)which, (uintptr)Ꮡlim, 0);
   ```

   `Ꮡlim` is a MANAGED box address handed to the kernel through the `uintptr` channel, and
   `getrlimit` **writes** through it. That is the struct-passing seam, on darwin, at the first
   syscall a converted program makes. **So this revises §3.1's implication that a keystone plus a
   real `FuncPCABI0` reaches `Main`: it does not.** Those two make the CALL happen; the call site's
   own marshalling is what makes it correct, and on this path the two are needed together. Sizing
   consequence: the reach-`Main` unit is keystone + `FuncPCABI0` + the pointer call sites on the
   init path, and the last of those is not enumerated here — a census of pointer-bearing
   trampoline call sites in the init closure is owed before an implementation is scheduled.
5. **Nothing else.** As in §2.4.4, no behavior change is asked of anything outside the keystone.

### 3.5 The question §2 left: darwin has NO `AllThreadsSyscall` analogue, and nothing to keep put

§2.4.4 requires that `AllThreadsSyscall` and `runtime_doAllThreadsSyscall` keep returning `ENOTSUP`.
**That requirement is Linux-only and imposes nothing on §3.** `AllThreadsSyscall` is declared in
`syscall_linux.go` and nowhere else in `syscall`; `doAllThreadsSyscall` lives in
`runtime/os_linux.go` and `runtime/proc.go`. Neither exists on darwin, so there is no darwin
`ENOTSUP` guard to preserve and no darwin equivalent of §2's cgo/no-cgo fork.

**The reason is worth stating, because it reframes §2.** Darwin's `Setegid` is not a special case at
all — it is trampoline number N:

```csharp
// src/core/syscall/darwin/zsyscall_darwin_amd64.cs:1413
public static error /*err*/ Setegid(nint egid) {
    var (_, _, e1) = syscall(abi.FuncPCABI0(libc_setegid_trampoline), (uintptr)egid, 0, 0);
```

The nine credential wrappers are special **on Linux only**, and only because Linux's raw syscall is
task-local while glibc's wrapper broadcasts to every pthread via nptl's setxid. Darwin routes
*every* syscall through libc already, so the credential functions inherit correct process-wide
semantics from the same keystone that makes `fmt.Println` work. **One keystone; §2's nine are a
consequence of it on Linux and are not even a category on darwin.** That is the strongest form of
"these are one piece", and it is the reason this section is short: most of §2's careful reasoning
about *which* mechanism the credential wrappers need has no darwin counterpart to state.

### 3.6 Blast radius

| item | size |
|:--|--:|
| `internal/abi/funcpc_impl.cs` rewritten (today `return default;`) | 1 file |
| the keystone family — 10 declarations over one parameterized helper | ~1–2 files |
| the symbol map, errno | 0 new files (derived; `__error()` is one more resolved symbol) |
| **new `[module: GoManualConversion]` markers** | **0 expected** — these are `*_impl.cs` companions |
| **new `*_impl.cs` companions** | **+2 to +3** (darwin goes 2 → 4–5; linux has 13) |

**No corpus emission movement expected**: a companion supplements declarations the converter already
emits. An implementation that finds itself needing a converter change is a **stop-and-post**, not a
scope increase. Gates the cut will owe: the darwin census (compile), `behavioral-smoke` on **both**
mac legs (the run gate, and darwin's first), `check-solution-integrity.ps1` across all three
targets, and the marker-census delta posted with the commit — with **no CNR and no converter suite**
if it stays companions-only, said explicitly rather than implied.

### 3.7 What §3 does not settle, named rather than hidden

- **Trampoline identity in the managed model — the single largest open question.**
  `FuncPCABI0(libc_write_trampoline)` receives a *delegate*. Whether the implementation can recover
  the trampoline's NAME from it at runtime, or whether the converter must emit an explicit symbol
  table, decides whether `FuncPCABI0` is a lookup or a converter change. It is a managed-reflection
  question that cannot be settled without running it on darwin.
- **Whether `GetExport` resolves all 267 symbols.** Some may be macros, weak, or versioned. One is
  proven; 267 is an assertion.
- **`crypto/x509/internal/macos`'s 29 bodyless partials** use `syscall_x509`, a keystone variant
  with its own ABI. Out of scope for reaching `Main`; named so it is not met as a surprise.
- **The arm64 debt, priced separately.** Every committed darwin arch file is `_amd64` (8 of them,
  zero `_arm64`), so Apple silicon compiles amd64 constants. **The keystone design does not change
  with the arch; the tables do** — this is a layout question (a per-GOARCH dimension inside a GOOS,
  which L3 does not have) and should be ruled on its own, named here only so a green arm64 run layer
  built on amd64 tables is never mistaken for done.
- **The feedback loop.** No darwin hardware in the fleet: ~10–17 minutes per CI round trip with no
  stack traces. Coarse keystone failures (symbol not found, wrong arity, errno not read) survive
  that loop; chasing an initializer chain call by call does not. The recommendation on record is to
  **instrument for the loop rather than shorten it** — have the probe print the resolved symbol
  table and the first N resolutions before the first call, so one dispatch answers a batch — and to
  hold the hardware ask until that probe says how deep the chase goes.

## 4. What is NOT proposed

- **Not** nine per-call hand-owns — the shape OQ-1 rejected, restated in the 2026-09-02 ruling.
- **Not** a change to `runtime_doAllThreadsSyscall`. Its `ENOTSUP` is correct for its own contract
  and three tests depend on it (§2.4.3).
- **Not** a general cgo layer. `cgocaller` is one bridge with one signature; nothing here proposes
  converting cgo C halves, which the converter cannot process regardless.

-- C1 (§1, §2) and C2 (§3)
