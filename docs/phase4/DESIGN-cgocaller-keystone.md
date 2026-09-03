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

#### AMENDMENT 2026-09-03 (increment 1, at the cut) — where `SET_RETVAL` lives, and what the `Setgroups` probe measured

Two things in §2.4 above are amended by the increment that implements it. Both are recorded here
rather than rewritten in place, because the reasoning that changed is the useful part.

**(a) The errno convention lives in the SHIMS, not in `cgocaller` — ruled 2026-09-03.** Item 2 places
`SET_RETVAL` in `cgocaller`. That is one layer too high, and Go's own structure says so:
`cgo_libc_setegid` does not point at libc's `setegid`, it points at a **shim** in
`runtime/cgo/linux_syscall.c` that applies the macro and then returns. The port therefore mints nine
`[UnmanagedCallersOnly]` managed shims, each over a `[LibraryImport("libc", SetLastError = true)]`
binding, and the nine `cgo_libc_*` hold **their** addresses. `cgocaller` stays a pure `uintptr`
bridge — an arity-dispatched indirect call and nothing else — which is exactly what item 4 and the
pointer-agnostic ruling want, and what §3 reuses unchanged.

The second reason is mechanical and would have bitten later: a raw `delegate* unmanaged<…>` call
**cannot use `SetLastError`**, so a `cgocaller`-side convention would have had to read
`__errno_location()` *after* the call and hope nothing on the thread clobbered errno in the window.
Inside a `[LibraryImport(SetLastError = true)]` shim the runtime captures errno at the call boundary
and `Marshal.GetLastPInvokeError()` reads it back with no window at all.

**(b) `Setgroups`'s call-site marshalling is CONFIRMED, and the confirming measurement corrected a
prediction that would otherwise have shipped a hazard.** The §2.5 ruling was challenged before the
cut on the reading that golib's `uintptr` operator on a box already pins durably
(`EnsureStableAddress` → `PinnedBuffer.PinOnly`, explicitly *not* a statement-scoped `fixed`), which
would have made the marshalling unnecessary and increment 1 a zero-converter-change cut. A four-arm
probe settled it, with the movement control run FIRST so that "stable" could mean anything:

| arm | what it varied | result |
|:--|:--|:--|
| 0 — control | an UNPINNED array across the same compacting GC | **moved** — the probe can observe movement |
| 1 — aliasing | write through the taken address | visible in the slice: same storage, not a copy |
| 2 — pin, box NOT held | only the `uintptr` kept, as at the call site | **address moved; the old address read zeroes** |
| 3 — pin, box HELD | a reference to the box kept alive | address stable, value intact |

Five runs, identical. The pin is real and is **scoped to the box's lifetime** — which the operator's
own comment says — and the call site passes the *address*, never the box, so the temporary is
unreachable before `cgocaller` is even entered, and `a` is a local whose last use is that
expression. The array is therefore collectable during the libc call. **The ruling stands: the
`gid_t` array is copied into unmanaged memory that lives for the whole call and is freed in a
`finally`.** The cost of the correction is one `manualConversionFuncs` entry (`Setgroups`,
`goosLinux`) with the converter suite and the two-seeded diff it brings; the other eight setters
pass scalars, and no scalar has a lifetime.

The general form is worth carrying beyond this design: **`(uintptr)Ꮡ(x)` handed to a native call is
safe only while something holds the box.** That is a sibling of the managed-struct-layout class this
corpus has been closing — the same "managed memory handed to the kernel" family, reached by
LIFETIME rather than by LAYOUT, and invisible to any layout remedy.

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

#### AMENDMENT 2026-09-03 (the class-B emission, at the cut) — the census re-derived over the whole darwin tree, and what §3.3's cross-check is really worth

§3.3's figures were taken over `zsyscall_darwin_amd64.go` alone (123 pragmas). Re-derived over all
**1650** `//go:cgo_import_dynamic` records in Go 1.23.12 outside `cmd/` and `vendor/`:

* **The resolvable darwin population is 345, selected by the library argument being an ABSOLUTE
  PATH.** Every darwin record names one; every other platform names a BARE library (windows' 51
  `kernel32.dll`, openbsd/solaris' `libc.so`, aix's `libc.a/shr_64.o`) or names none at all, as
  `runtime/race`'s **196** darwin records do. Selecting on `^/` and selecting on "`.dylib` or a
  framework path" are independent derivations of the same 345 and agree on every one. A `.dylib`
  SUFFIX gate is the near-miss: it drops exactly the 28 `crypto/x509/internal/macos` framework
  records, which carry no suffix.
* **§3.3's cross-check holds where it was measured and does not generalize.** In §3.3's own notation
  (`libc_<n>`, `<n>` == `<sym>`) the claim is `local == "libc_" + symbol`, and that is true for
  **312** of the 345 — not 345, and not the 0 an earlier draft of this amendment reported by reading
  the claim as `local == symbol`. The 33 exceptions are 28 `x509_<sym>`, 3 `libresolv_<sym>`, one
  `libc<sym>` with no underscore, and one outlier, `libc_error` / `__error`, which is the ONLY record
  in all 345 whose local does not even end with its symbol. That single row is the argument against
  the cross-check: it would be right 344 times and silently wrong once.
* **The trampoline-to-pragma binding is mechanical OUTSIDE `runtime` and nowhere inside it.** Over
  all **340** bodyless `*_trampoline` declarations in darwin-reachable files,
  `trampoline == local + "_trampoline"` holds **297 of 297** outside `runtime` and **0 of 43** inside
  it — 37 of those bind on the SYMBOL instead, and 6 (`osinit_hack`, `exit`, `nanotime`, `walltime`,
  `sigprocmask`, `raiseproc`) carry no darwin pragma at all and are genuinely class C. So §3.3's
  "derivable twice over" is true for the population the converter emits and false for `runtime`,
  whose correspondence lives in the `.s` file the converter does not read.

The emission cut on this basis is **173 records per darwin target** (126 `syscall`, 28
`crypto/x509/internal/macos`, 19 `internal/syscall/unix`), published as `GoCgoImportDynamic` assembly
attributes in a `<CgoDynamicImports>` section of `package_info.cs` and resolved by golib's
`GoCgoDynamicImports`. `runtime`'s 43 are deferred by name rather than reached with a normalizer that
would cover 334 of 340 and guess at the remainder. `funcpc_impl.cs` still reads none of it: the
consumer line lands when increment 1's rewrite of that file is at master.

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
   consequence: the reach-`Main` unit is keystone + `FuncPCABI0` + the pointer call sites, and
   **that last term is now censused** (it was named as owed here; this is the answer).

   **THE CENSUS — half the darwin keystone surface is pointer-bearing.** Measured two ways at
   master `62c63b572`, because a census keyed on one spelling under-reports by every other:

   | | emitted C# | Go source |
   |---|--:|--:|
   | keystone call sites | **149** | 151 |
   | ... passing a managed address | **75** | 66 |
   | ... pure scalars | 74 | 85 |
   | distinct trampolines | **126** | **126** |
   | ... pointer-bearing | **72** | 62 |

   The two derivations agree EXACTLY on the trampoline total (126) and the emission side is the
   operative one. The 10-trampoline gap is explained rather than split: `getcwd`, `getfsstat`,
   `mlock`, `mprotect`, `msync`, `munlock`, `pread`, `pwrite`, `sendto`, `writev` assign
   `_p0 = unsafe.Pointer(&buf[0])` on a line BEFORE the call and pass `uintptr(_p0)` inside it, so
   a call-local scan of the Go source cannot see the pointer while the emission's own `_p0` channel
   can (spot-checked on `sendto` and `writev`). The Go-side set is a strict SUBSET of the emission
   set — zero trampolines go the other way — which is what makes the larger number the safe one.

   **Three channels, not one, and the first count found only the first**: a direct `Ꮡx` argument
   (37 sites), a `_p0` local assigned from `Ꮡ(p, 0)` or `@unsafe.Pointer.FromBox` (46 sites), and a
   `ж<T>` parameter cast straight to `uintptr` with no `Ꮡ` in sight (`setgroups`'s `groups`,
   `setrlimit`'s `rlim`). A census keyed on `Ꮡ` alone reports 37 — half the truth.

   **The init path itself is small and both of its members are pointer-bearing.** `syscall`'s own
   init is `rlimit.go`'s: `Getrlimit(RLIMIT_NOFILE, &lim)` unconditionally, then `setrlimit` only
   when `Cur != Max`. Two calls, two managed addresses. So reaching `Main` needs the keystone,
   `FuncPCABI0`, and marshalling at **two** call sites — the other 73 are the cost of a WORKING
   darwin, not of a STARTING one, and that is the distinction the sizing needs.
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

- **The pointer call-site census is DONE** (§3.4) and the marshalling SHAPE is now settled too
  (§3.8): 75 of 149 sites, 72 of 126 trampolines, two on the init path — and the 92 pointer
  ARGUMENTS split into three populations, of which 45 need pinning rather than marshalling and
  11 need two lines, leaving ~10 struct mirrors as the whole cost. Neither of §3.7's original
  two options was the answer.
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

### 3.8 The marshalling SHAPE — three populations, not one, and the ceremony fits almost none of them

§3.7 left this as "one helper versus per-site", and the coordinator asked for it priced by what each
does to readability and to the audit rule (*every buffer handed to a native call lives in unmanaged
memory for the duration*). **Both framings assume one population. There are three, and they want
three different mechanisms with very different costs.**

Classified from Go's own signatures — the enclosing `func`'s declared parameter type for every pointer
argument at a darwin keystone call, paren-balanced scan, at master `62c63b572`. **The unit here is
pointer ARGUMENTS (92), not call SITES (75): a site can pass several.** Both numbers are right at
their own level and neither substitutes for the other.

| population | args | what it needs | ceremony |
|---|--:|---|---|
| **buffer** — `*byte`, `**byte`, `unsafe.Pointer`, slice/string element | **45** | a stable address for the call | **pin, do not marshal**: no copy, no free, no `finally` |
| **scalar out-param** — `*_Socklen`, `*_Gid_t`, `*_C_int`, `*uintptr` | **9** | one integer written back | `stackalloc` + one copy back: two lines |
| **scalar array** — `*[2]int32` (`pipe`, `socketpair`) | **2** | two integers written back | `stackalloc` + copy back |
| **struct pointer** — `Stat_t`, `Statfs_t`, `Rlimit`, `Rusage`, `Timeval`, `Timespec`, `RawSockaddrAny`, `Msghdr`, `FdSet`, `Dirent` | **30** | an explicit-layout mirror + copy in/out | the real work |
| unclassified — five inside `forkAndExecInChild`, one `sendfile length` | 6 | named, not guessed | sized when reached |

**The buffers are 45 of 92 and they need no unmanaged memory at all.** The audit rule's GUARANTEE is
that the address the kernel sees is stable for the call and the bytes are the caller's; pinning
(`fixed`, or a pinned `GCHandle`) gives exactly that, without a copy, without a free, and without a
`finally`. Reading the rule as "must be `AllocHGlobal`" would copy 45 buffers for nothing and make
the call sites worse. The rule should be stated by its guarantee, not by its mechanism.

**The `Exec` precedent's ceremony is needed for ~none of these, and the reason is structural.**
`exec_unix.cs`'s `posix_spawn` seam carries seven `IntPtr` locals, a long `try`, and a conditional
`finally` — because `posix_spawn` RETAINS its `file_actions` and `spawnattr` across several calls,
and because a `char**` vector has no managed original to pin. **Every darwin keystone call is a single
synchronous syscall that retains nothing**, so its buffers live exactly as long as the call and
`stackalloc` covers every out-param without a `finally` at all. The precedent is the right pattern for
the case it was written for and the wrong template for this one.

**So the answer to §3.7's question is neither of its two options.** Not one helper — the three
populations do not share a signature. Not 73 per-site marshallings — 45 need no marshalling and 11
need two lines. What remains is **~10 distinct struct mirrors** covering the 30 struct-pointer
arguments, and those are the whole cost.

**What the Linux side already gives, and what it does not.** The corpus carries native mirrors for
four of the ten types — but every one is arch- and OS-suffixed: `NativeStatLinuxAmd`,
`NativeTimevalLinuxAmd`, `NativeRusageLinuxAmd`, `NativeFdSetLinuxAmd`. Darwin's layouts differ
(darwin's `Timeval` is `{int64, int32}` where Linux's is `{int64, int64}`), so **the pattern transfers
and the layouts do not** — a mirror that exists is not a mirror darwin can use, and reading the name
as reusable would be the same mistake as reading `%#v`'s success on one shape as success on another.
`NativeTimespec` is the one unsuffixed mirror and is the only reuse candidate; it still gets checked
against darwin's header rather than assumed.

**Reach-`Main` is unchanged and still two call sites** (§3.4): `rlimit.go`'s `Getrlimit` plus
`setrlimit`, both `*Rlimit` — so the first increment needs the keystone, `FuncPCABI0`, and **one**
struct mirror. The other nine mirrors are the cost of a working darwin, not a starting one, and they
can land one at a time behind a running program.

---

## 4. What is NOT proposed

- **Not** nine per-call hand-owns — the shape OQ-1 rejected, restated in the 2026-09-02 ruling.
- **Not** a change to `runtime_doAllThreadsSyscall`. Its `ENOTSUP` is correct for its own contract
  and three tests depend on it (§2.4.3).
- **Not** a general cgo layer. `cgocaller` is one bridge with one signature; nothing here proposes
  converting cgo C halves, which the converter cannot process regardless.

-- C1 (§1, §2) and C2 (§3)
