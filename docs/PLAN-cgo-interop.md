# PLAN — cgo interop for go2cs: from `import "C"` to a working P/Invoke bridge

> **STATUS: PROPOSED — AWAITING COORDINATOR RATIFICATION (2026-08-28). Nothing in this document is
> ratified; no implementation ships with it.** A **strategy plan** ([`Glossary.md`](Glossary.md),
> *Plan*): it fixes a ruled frame — which targets, in which order — and holds its OQ rulings until
> its ladder completes. It supplies **no procedure**; procedure stays in the runbooks. §10's ⟨OQ-1⟩
> through ⟨OQ-5⟩ carry recommendations awaiting ruling; ⟨OQ-6⟩ is ruled.
>
> Written against `be58eb4aa` (2026-08-25), Go 1.23.12, .NET 10. **Revised twice on 2026-08-28 under
> adversarial review**; §9 records every claim the review falsified, including four of the first
> draft's that were simply wrong and one of the second draft's. A reviewer finding against
> `phase4/DESIGN-cooperative-scheduler.md` (§5.3) is **flagged for the coordinator, not corrected
> here** — it is another document's to amend.
>
> Scopes the `cgo` slice of [`Roadmap.md`](Roadmap.md) Phase 5 ("replace the `PartialStubGenerator`'s
> throwing implementations for Go declarations backed by assembler, cgo, runtime/compiler intrinsics,
> or platform services") — specifically the part Phase 5A/5B's inventory-and-companion machinery does
> not reach: a Go file that itself declares `import "C"`.
> [`ToDo.md`](../src/go2cs/ToDo.md) item 48 has tracked this, unimplemented, since `87465f5f5`
> (2025-01-12) — the "go2cs now based on Go" restructuring that began this generation of the project.
>
> Companions: [`TestingInfrastructureRequirements.md`](TestingInfrastructureRequirements.md) (Phase 4 —
> the validation pipeline Phase 6 extends, and the determinism principle §5.2 answers to),
> [`phase4/DESIGN-pointer-provenance.md`](phase4/DESIGN-pointer-provenance.md) (RATIFIED) and
> [`phase4/DESIGN-native-backed-slice.md`](phase4/DESIGN-native-backed-slice.md) (RATIFIED, LANDED) —
> the pointer/slice foundations Phase 3 builds on.

---

## 1. Verdict and scope

cgo is not a fundamental platform mismatch for go2cs. Its hard parts — goroutine-stack switching,
P-detach around a blocking call, GC-visible-pointer rules, reverse-call thread registration — are
runtime-transition mechanics the CLR has mature, explicit answers for: `LibraryImport`, blittable
marshaling, `GCHandle`/`fixed` pinning, and `[UnmanagedCallersOnly]`. There is no clean, portable
"cgo API" separable from those mechanics to target instead — `cmd/cgo`'s `C.foo()` surface is thin,
but every call beneath it is wired into `runtime.cgocall`/`cgocallback`.

Go's side of that bargain is *cheaper* than the CLR's, not weaker: Go's heap is **non-moving**, which
is exactly what lets `&b[0]` be handed to C with no pinning at all. (Go does move goroutine **stacks**,
which is why `cgocall` switches to the system stack.) .NET compacts, so pinning is the price of
admission here — a cost to pay, not an advantage to claim.

It is a real subproject, not a flag flip, and §2's measurement shows the starting line is further back
than assumed. §5–6 stage the work in six phases sized to the common shape of cgo usage: declared
extern functions, simple structs, basic callbacks. §7 is deliberately speculative and unscoped — the
long tail is named, not estimated, per the no-frozen-figures discipline
([`Glossary.md`](Glossary.md), *Runbook*).

| # | Sub-problem | Weight in practice | .NET-side primitive | Phase |
|---|---|---|---|---|
| 1 | Declared extern C functions | the bulk of cgo usage | `LibraryImport` + blittable structs | 1–2 |
| 2 | Pointer / string / slice marshaling | underlies every call in row 1 | `PinnedBuffer` (Go→C), native-backed `slice<T>` (C→Go), `ж<T>` provenance | 3 |
| 3 | Inline C bodies in the preamble | common in small libraries | native side-build, then P/Invoke — never C→C# transpilation | 4 |
| 4 | Reverse callbacks (`//export`) | less common, load-bearing where used | `[UnmanagedCallersOnly]` | 5 |

## 2. Measured: what happens today

**Instrument.** A minimal cgo fixture plus a `packages.Load` probe using `packages.LoadAllSyntax` —
the exact mode `conversionDriver.go:89`, `stdLibConverter.go` and `moduleConverter.go:156` all use.
Run on the coordinator machine, 2026-08-28, against `be58eb4aa`.

**Finding 1 — the converting machine cannot run cgo at all.** `go env CGO_ENABLED` → `0`; no `gcc`,
no `clang`, no `cl.exe` on `PATH`. Go's cgo on Windows requires gcc or clang and **does not support
MSVC**, so the MSVC toolchain the Native-AOT perf path already depends on does not satisfy cgo.
Toolchain provisioning is a day-one problem, not a Phase-7 one — and it is why ⟨OQ-1⟩ below could not
be closed, and why every cgo-enabled reading in this section is bounded by "no C compiler present."

**Finding 2 — a cgo package under `CGO_ENABLED=0` loads SILENTLY EMPTY.** `packages.Load` returns one
package with `GoFiles=[]`, `CompiledGoFiles=[]`, a zero-name type scope, and a single *soft* error,
`build constraints exclude all Go files`. Not a hard failure — an absence.

**Finding 3 — the fatal gate is effectively dead code, and go2cs reports SUCCESS on a cgo package.**
The first draft claimed `import "C"` is "a hard stop" at
[`visitImportSpec.go:211`](../src/go2cs/visitImportSpec.go). Measured, that gate **never fires**: with
cgo disabled, build constraints exclude the file before any visitor runs; with cgo *enabled*, the ASTs
reaching the visitor are cgo-**rewritten** and no longer contain `import "C"` at all. Actual behavior,
identical with and without `-cgo=true` — **the flag is inert**:

```text
WARNING: cgorevfixture did not fully type-check; converting best-effort — code depending on
         the following is emitted untyped: [-: build constraints exclude all Go files in ...]
INFO: Skipping conversion: no target Go source files found for conversion in input path "..."
exit code: 0
```

A cgo package converts to **nothing** and the converter **exits 0**, having emitted only `go2cs.ico`.
This is the repo's catalogued false-green pathology — the shape CLAUDE.md names for the GOROOT
forward-slash trap, *"a path the converter half-recognizes is worse than one it rejects."* The
`did not fully type-check` warning means CNR would report such a package **NOT MEASURED**, so the gate
is not fooled; a hand-invoked conversion is.

**Finding 4 — `pkg.Syntax` is zipped against the WRONG file list in the production path.**
[`conversionDriver.go:227-228`](../src/go2cs/conversionDriver.go):

```go
for i, file := range pkg.Syntax {
    path := pkg.GoFiles[i]
```

`golang.org/x/tools@v0.36.0/go/packages/packages.go` — the version `src/go2cs/go.mod` pins — is
explicit that this is the wrong pairing. `:512` *"Syntax is the package's syntax trees, **for the
files listed in CompiledGoFiles**"*; `:518-519` *"kept in the same order as CompiledGoFiles… If
parsing returned nil, Syntax **may be shorter** than CompiledGoFiles"*; `:447-451` *"GoFiles… may
include files that should not be compiled… or are **subject to cgo preprocessing**"*.

For every package in the corpus today the two lists coincide, so the zip is correct *by accident*.
Under cgo they diverge — `CompiledGoFiles` holds cgo's generated output in a build cache — and the
pairing either overruns or, worse, **silently misaligns**, binding an AST to another file's path. That
path decides the emitted `.cs` name, the hand-own marker probe (`conversionDriver.go:244-246`), and the
`CheckBuildConstraints` target. The nils caveat makes the zip fragile even without cgo. The `-tests`
path already does it correctly ([`testConversion.go:1170-1175`](../src/go2cs/testConversion.go)):
zipped against `CompiledGoFiles`, bounds-guarded. *This is Phase 1's first change, and a latent
correctness bug independent of cgo.*

**Finding 5 — zero regression risk to the existing corpus.** No tracked `.go` file imports `"C"`. The
corpus is wholly `CGO_ENABLED=0` output, so cgo support cannot move an existing golden or CNR verdict.

**Finding 6 — three existing "C" accommodations, all avoidance, none a binding.** The fatal gate
(`visitImportSpec.go:211`), the `classSkip` bucket (`moduleConverter.go:226`), and a type-alias preload
skip (`importOperations.go:1027`). Nothing parses a preamble; nothing generates anything.

## 3. Where this sits in the existing roadmap

[`Roadmap.md`](Roadmap.md) Phase 5 already names cgo alongside assembler and runtime intrinsics, but
its machinery was built for a different shape of the word:

- **Bodyless stdlib declarations backed by asm/cgo *linkage*** already compile — `PartialStubGenerator`
  emits a throwing `partial`, and Phase 5A/5B replace each with a hand-owned `*_impl.cs` companion, the
  `sync/atomic` `doc.cs` + `doc_impl.cs` pattern ([`Roadmap.md:770-784`](Roadmap.md)). **Bounded and
  curated** — the stdlib's own declarations, known in advance.
- **A Go source file's own `import "C"`** is missing entirely (§2). **Unbounded** — user- or
  library-supplied.

This document designs the second. It reuses the first's *shape* — a converter-owned declaration paired
with an implementing companion — but the companion must be **generated**. That is Phases 1–4. The
Windows `syscall` interop already proved the hand-written approach works and where it stops:
`zsyscall_windows_impl.cs` and `interface_windows_impl.cs` found and fixed exactly this bug class —
non-blittable types needing blittable mirrors, `ж<T> → uintptr` answering 0 for a nil boxed pointer on
`**T` out-parameters, raw kernel buffers needing manual transcription — one hand-owned wrapper at a
time, over an enumerable Win32 surface. cgo raises the same classes against a surface nobody can
enumerate in advance.

## 4. Foundations already in place

- **Pointer provenance** ([`phase4/DESIGN-pointer-provenance.md`](phase4/DESIGN-pointer-provenance.md),
  RATIFIED 2026-08-23) teaches `ж<T>` which kind of address it holds — pinned-managed, pointer-shaped,
  or native. That is the classification a generated wrapper needs to decide whether a value handed to C
  must be pinned or is already stable.
- **Native-backed `slice<T>`** ([`phase4/DESIGN-native-backed-slice.md`](phase4/DESIGN-native-backed-slice.md),
  RATIFIED 2026-08-22, landed) covers the **C→Go** direction: wrapping a C-returned buffer as a slice
  that reads and writes through to the real memory.

**The Go→C direction is a different mechanism, and the second draft of this document got this wrong.**
A native-backed slice cannot be pinned — `slice.cs:396` *throws* (*"pinning is meaningless for native
memory — take an element address instead"*), and `OverNativeMemory` refuses any `T` carrying managed
references. Handing a **managed** Go slice to C uses the pre-existing `PinnedBuffer`
(`slice.cs:398`, and its siblings in `string.cs`/`ж.cs`). So Phase 3 is *partly* reuse: the C→Go half
is ratified work, the Go→C half is existing golib machinery, and only the glue is new.

## 5. Constraints the design must carry

### 5.1 A C toolchain, on the converting machine, per target

cgo preprocessing and Phase 4's inline-C build both require a working C compiler for the *target*
platform; §2 Finding 1 shows this project's own machine has none. Two consequences: cgo support must
**assert** `CGO_ENABLED=1` and a resolvable compiler, failing loudly (never inheriting §2's
silent-empty path); and `-platforms` multi-target emission needs a provisioned **cross** C toolchain
per target — materially harder than the pure-Go layout-L3 emission already solved. §7 keeps the
cross-compilation matrix speculative for this reason.

### 5.2 Determinism, and what that forbids

[`TestingInfrastructureRequirements.md`](TestingInfrastructureRequirements.md) §2 principle 8 requires
that equivalent inputs — *"Go version, converter version, and target platform"* — produce byte-stable
output; CNR gates emitted `.cs`/`.csproj` on byte identity. **The C toolchain is not in that
equivalence class**, and cgo threatens the principle from two directions:

1. **Generated C# derived from local system headers.** `#include <stdlib.h>` resolves against the
   converting machine's headers. `C.size_t`, `C.long`, `time_t` width and struct layout vary by libc
   (glibc vs musl), SDK version, and flags like `_FILE_OFFSET_BITS`. A generated `[StructLayout]` mirror
   is therefore a function of the machine. Layout L3 models per-**GOOS** variance only; it has no slot
   for per-libc or per-SDK variance.
2. **A compiled native artifact** (Phase 4) is machine- and architecture-specific by construction, and
   is a **binary** — a class the corpus neither tracks nor gates, and which fleet doctrine explicitly
   excludes ("never git-committed binaries").

**The `.s`-assembly precedent does not license this.** [`ToDo.md:49-51`](../src/go2cs/ToDo.md) proposes
compiling assembly to object code and wrapping it — but that is *Go's own assembler*: hermetic,
versioned with the toolchain, inside principle 8's equivalence class. An external gcc/clang is not. The
precedent has exactly the property Phase 4 lacks.

**Recommendation:** generated bindings and any native artifact are **build outputs, not corpus
content** — git-ignored as the `-tests` pipeline's regenerated inputs already are
(`src/core/.gitignore`), and excluded from byte-identity gates by construction. ⟨OQ-4⟩ carries the
open half, and the word "deterministic" is deliberately *not* claimed for the convert-time option.

### 5.3 Blocking calls — the pool-starvation risk no longer exists

**The first two drafts of this document built this section on a premise that was already false at
their own pinned commit, and the correction changes the conclusion.** Goroutines have not been
`ThreadPool` work items since `4f06d78ae` (2026-08-13, *"SCHED-S1: goroutines get their own threads —
the runtime owns capacity, and the pool floor retires"*), which is an **ancestor of `be58eb4aa`**.
`Goroutine.Start` (`golib/runtime/Goroutine.cs:202-209`) is now `new Thread(() => Run(body),
s_stackReserve)`, one dedicated thread per goroutine; the source says so directly at `:26` —
*"Goroutines **used to be** `ThreadPool.QueueUserWorkItem` work items."* The 28.7-minute
`internal/singleflight` ladder that
[`phase4/DESIGN-cooperative-scheduler.md`](phase4/DESIGN-cooperative-scheduler.md) measured is cited by
`Goroutine.cs:26-38` as the historical motivation **for the fix that removed it**.
([`phase4/DESIGN-cooperative-scheduler.md`](phase4/DESIGN-cooperative-scheduler.md) still describes
the launch path in the present tense as `QueueUserWorkItem` (§2) and still carries a
`PROPOSED / nothing ratified` status header, though `0b8287f07` chartered it and `4f06d78ae` landed
SCHED-S1, both on 2026-08-13 — flagged for the coordinator, not corrected here. CLAUDE.md itself is
clean on this point; it carries no `QueueUserWorkItem` claim.)

So a blocking cgo call from a goroutine occupies **that goroutine's own dedicated thread** and starves
nothing. Capacity is thread-bound (~10⁴ threads), not pool-heuristic-bound. cgo inherits no
starvation pathology, and the design owes no mitigation for one. The resulting position:

1. **Default: a direct P/Invoke on the calling thread.** Correct, fast, and already the goroutine's own
   thread. This is also what a naive P/Invoke does, so it is the zero-work default.
2. **`[SuppressGCTransition]` only for short, non-blocking, non-callback-taking calls.** The thread
   stays in cooperative mode, so a long call blocks **GC suspension process-wide** — a deadlock, not a
   stall. It must be excluded for any symbol reachable by a reverse callback (Phase 5's shape), and for
   anything that can throw, block, or take a lock. Opt-in and evidence-backed, never a default.
3. **Thread-hopping is not a general answer, and thread-*per-call* is actively wrong.** Beyond costing
   ~10³–10⁴× the call it wraps, it breaks correctness: `errno` — returned by cgo's two-value form
   `n, err := C.f()` — is thread-local, and thread-affine C libraries (OpenSSL's error queue,
   `setlocale`, GTK, SQLite under `SQLITE_CONFIG_SINGLETHREAD`) require call-to-call thread identity.
   If a future case needs it, the shape is a **persistent, reused affinity thread per cgo context**,
   never a thread per call.

For reference, real Go does not mint a thread per call either: `cgocall` enters the syscall state, the
goroutine keeps its M, and `sysmon` **detaches the P** so other goroutines proceed — P-detach with M
reuse. The .NET analogue of the useful half is the GC transition a P/Invoke already performs.

## 6. The phased plan — base operations

### Phase 1 — Recognition, file mapping, and declaration extraction

**Precondition, not a footnote:** `CGO_ENABLED=1` with a provisioned C compiler (§5.1). Everything
below is unreachable without one.

1. **Fix the file-list zip** (§2 Finding 4) — `conversionDriver.go` adopts `testConversion.go`'s
   bounds-guarded `CompiledGoFiles` pattern. Worth doing on its own merits.
2. **Fail loudly instead of silently empty** (§2 Findings 2–3) — a package whose files were all excluded
   by build constraints, or that needs a C toolchain that is absent, must be an error naming the cause,
   not `exit 0` with `INFO: Skipping conversion`.
3. **Establish the source ↔ generated file mapping.** For a cgo package the ASTs go2cs type-checks are
   cgo's *generated* files in a build cache, not the files on disk. Everything keyed off a filename needs
   a defined answer: the emitted `.cs` name, the hand-own marker probe, and comment/licence-header
   preservation — `-comments` is mandatory for derivative works, and a generated file's comments are not
   the source's. Map each rewritten AST back to its originating source entry rather than index-zipping.

**On preamble parsing (⟨OQ-1⟩).** No go2cs `packages.Config` call site overrides `CGO_ENABLED`, and
`LoadAllSyntax` includes `NeedCompiledGoFiles` (the probe confirms the field is live). When a C
toolchain is present, `cmd/cgo` rewrites `C.foo`/`C.sometype` into typed synthetic Go declarations
(`_Cfunc_foo`, `_Ctype_sometype`, `_Cvar_x`) before `go/types` runs — the mechanism `gopls` and
`go vet` rely on to type-check cgo code without embedding a C front end. If it holds, Phase 1 needs
**no C parser** — only recognition and routing, *on top of* the driver change in step 1, which is
required regardless. **Not verifiable on the coordinator machine** (§2 Finding 1); confirm on a
cgo-capable host before scoping parser work. The fallback is a bounded parser for the common preamble
subset only, explicitly not a general C grammar.

**Exit gate:** on a cgo-capable host, a fixture loads with every `C.*` reference resolved to a
classified declaration, and each converted AST maps to the correct source filename. No C# emitted.

### Phase 2 — P/Invoke and blittable struct generation

**Goal:** generate a `LibraryImport` extern and blittable mirrors per declaration. Natural home: a new
`cgoOperations.go`, beside `directiveOperations.go`/`importOperations.go`.

| C form | Emitted C# |
|---|---|
| integer/float scalar types | fixed-width C# equivalents |
| `struct { … }` | `[StructLayout(LayoutKind.Sequential)]` blittable mirror |
| fixed-size C array member | **`[InlineArray(N)]`** (.NET 8+) — *not* a C# `fixed` buffer, which accepts only primitive element types and so cannot express `struct sockaddr addrs[4]` |
| function pointer | `delegate*<…>` |
| `char *`, `void *`, sized buffers | a blittable `void*`/`nint` extern **plus** a generated wrapper that owns the pin scope (below) |
| `#cgo LDFLAGS: -lfoo` | resolves the shared-library name; per-OS naming is .NET's own native-library resolution. **Scoped to the `-lfoo` case** — `LDFLAGS` is arbitrary linker flags, and static linking is a Phase 4 problem |

**Two layers, not one.** `LibraryImportGenerator` marshals only types it knows; `ж<T>` and `slice<T>`
are not among them and each would need a full `CustomMarshaller`. So the generated surface is a
blittable extern taking `void*`/`nint`, wrapped by a generated method that owns the pin scope — which
is exactly what Phase 3 describes. `LibraryImport` also cannot express **varargs** at all (§7).

**One architectural constraint, stated so it is not moved later:** these declarations must be written
by **the converter, into real `.cs` files on disk** — never emitted from the `go2cs-gen` analyzer.
Roslyn source generators do not observe each other's output, so a `[LibraryImport]` declaration produced
by go2cs-gen would be invisible to the SDK's `LibraryImportGenerator` and never receive an
implementation. Converter-written files are ordinary compilation inputs. (`DllImport` remains the
fallback for any signature the generator rejects.)

**Exit gate:** an extern-function-only cgo package's Go-callable surface compiles and resolves.
Signature generation only — no live native library required.

### Phase 3 — Pointer, string, and slice marshaling

**Goal:** `C.CString`, `C.GoString`, `C.GoStringN`, `C.GoBytes`, `C.CBytes` and `C.free` become golib
helpers over §4's mechanisms, split by direction.

Three distinct cases the first draft conflated:

1. **Go→C copy** (`C.CString`). Exists because Go strings are **not NUL-terminated and are immutable**,
   and because it `malloc`s so C *may* retain the result — not because a Go value's layout is unreadable.
2. **C→Go copy** (`C.GoString`, `C.GoBytes`). The opposite direction; copies because the Go GC cannot own
   C memory.
3. **Go→C no-copy** (`C.f((*C.char)(unsafe.Pointer(&b[0])), C.size_t(len(b)))`). The standard idiom for
   handing a `[]byte` to C. cgo's rule constrains **retention past the call**, not the handing over. This
   is a first-class case, served by `PinnedBuffer` (§4).

Real cgo's pointer rules are a programmer discipline, optionally checked at runtime (`cgocheck`). Here
the discipline can be **structural**: the generated wrapper owns the pin scope, opening it immediately
before the call and closing it immediately after, so a caller cannot violate the rule by omission.

**Exit gate:** byte-identical round-trip in both directions through a real C function, including the
no-copy `&b[0]` idiom.

### Phase 4 — Inline C bodies (native side-build)

**Goal:** compile the preamble's C **definitions** with a real C compiler into a native shared library,
then bind it via Phase 2/3.

Explicitly not C→C# transpilation. §5.2 governs where the artifact may live; ⟨OQ-4⟩ carries timing.

**Two gaps that make this harder than the phase ordering suggests, both named rather than assumed:**

- **Idiomatic inline helpers are `static`, and a `static` C function exports no symbol.** The §2 fixture
  is itself an example — `static int add_ints(...)` is un-bindable by a mechanism that P/Invokes exported
  symbols. Phase 4 therefore needs a de-static-ing or shim step (generate a non-static wrapper per
  referenced `static` helper), not merely a compile step.
- **Much real-world cgo links C statically into the package** — `mattn/go-sqlite3` ships `sqlite3.c` —
  so there is **no shared library** to bind at all. Handling that means producing one, which is the same
  shim machinery pointed at a larger input, and it interacts directly with ⟨OQ-2⟩'s choice of validation
  target.

**Exit gate:** a cgo package whose preamble contains real C definitions builds and matches `go run`.

### Phase 5 — Reverse callbacks (`//export`)

**Goal:** a `//export`-marked Go function becomes an `[UnmanagedCallersOnly]` static entry point.

**Phase 4 and Phase 5 are mutually exclusive for a given file**, and the ordering must not imply
otherwise. `$GOROOT/src/cmd/cgo/doc.go:324-329`: *"Using //export in a file places a restriction on the
preamble: since it is copied into two different C output files, it must not contain any definitions,
only declarations."* Phase 5 targets declaration-only preambles; definitions live in other files.

The base mapping is favorable: cgo already restricts `//export` to top-level, receiver-less functions
with cgo-safe signatures — close to what `UnmanagedCallersOnly` independently requires. Three things
need real work:

- **Per-goroutine state.** A thread entering through a callback has none of golib's `[ThreadStatic]`
  state — `t_onGoroutine`, the defer/panic locals, `t_procId`, the high-resolution sleep timer. The entry
  point must establish it as `Goroutine.Run` does. This *is* the registration work; the CLR's automatic
  thread attach on reverse P/Invoke covers the runtime's bookkeeping, not golib's.
- **Panics at the boundary.** An exception escaping an `[UnmanagedCallersOnly]` thunk is a fatal rude
  abort. golib models Go panics as `PanicException` with a Go-faithful traceback; a panicking `//export`
  function would instead abort with none. The thunk must catch and map to cgo's own crash behavior.
- **Stack divergence.** An attached foreign thread's stack is whatever native allocated, so golib's
  `DefaultStackReserve` (256 MB, `Goroutine.cs:68`) does not apply — deep recursion inside a callback
  fails differently than inside a goroutine.

**Exit gate:** a Go→C→Go round trip matches `go run`, including from a thread the C library created
itself, **and** a panic inside the callback produces Go-faithful output rather than a bare abort.

### Phase 6 — Validation

**Goal:** earn "base operations work" as a measured claim by routing a cgo package through the Phase-4
`-tests` pipeline and comparing against `go test -json`, as
[`ValidatedTestPackages.md`](ValidatedTestPackages.md) holds every other package.

`-tests` and `-recurse` cannot be combined in one invocation —
[`main.go:344`](../src/go2cs/main.go) is a hard `log.Fatalln`. A third-party cgo library needs
`-recurse`, so it cannot be converted *and* test-validated in one command. The flag error names the
intended flow — *"convert the module first, then convert its package tests individually"* — so the real
question is whether that two-step path holds for a **cgo** dependency, where step two must re-resolve
the C toolchain and preamble for a package in a converted output tree. Unproven. The roster is also
stdlib-scoped today and has no shape for a non-stdlib row. ⟨OQ-2⟩ carries the fork.

**Exit gate:** at least one real cgo package validated end-to-end against `go test`.

## 7. Phase 7 (speculative) — toward 100%

Named, not estimated.

- **Full C semantic fidelity** — macros, **varargs** (which `LibraryImport` cannot express at all),
  bitfields, unions, nested anonymous structs, and `#cgo` conditional-directive syntax beyond the common
  subset.
- **`#cgo pkg-config` resolution** against arbitrary system package managers on every target. (Phase 2
  is scoped to `-lfoo` precisely so this stays here.)
- **Arbitrary `LDFLAGS`** — `-L`, `-rpath`, `-framework`, `.a` archives, `-static`.
- **A `cgocheck`-equivalent runtime verifier** — a debug-mode check that a generated wrapper's
  pin/lifetime discipline actually held. New machinery; nothing in golib is a starting point.
- **C++ interop** — a stretch even in real Go, which officially supports C only.
- **A cross-compilation host-toolchain matrix** (§5.1) — a provisioned cross C compiler per target
  before `-platforms` can emit cgo packages at all.

## 8. Non-goals

- **Transpiling arbitrary C into C#.** Native compile plus P/Invoke, never a second front end.
- **100% cgo fidelity as an entry criterion for Phases 1–6.** Staged on purpose, as the stdlib
  conversion itself staged.
- **Reopening goroutine scheduling.** §5.3 measured it as already solved for this design's purposes.

## 9. Adversarial pass — what review falsified

Recorded rather than silently corrected, per this repo's practice of keeping the wrongs on the record
(`DESIGN-pointer-provenance.md`'s "three recorded wrongs"). Two review rounds; the second round
falsified a claim the first round's revision had *introduced*.

| Claim | Verdict |
|---|---|
| "`import "C"` is a hard stop — `visitImportSpec.go:211` calls `log.Fatalf`" | **WRONG, measured.** The gate never fires in either configuration; conversion silently yields nothing and **exits 0** (§2 Finding 3). A false green, which is worse than a hard stop. |
| "Goroutines run as `ThreadPool.QueueUserWorkItem`; a blocking cgo call starves the pool" | **WRONG at this document's own pinned commit** — and it survived the first revision, which fixed the recommendation while keeping the false premise. `4f06d78ae` (2026-08-13) gave every goroutine a dedicated thread and is an ancestor of `be58eb4aa`. §5.3 rewritten; the mitigation it argued for is not owed at all. |
| "Route every cgo call through a dedicated `Thread.Start`" | **WRONG on cost and on correctness.** ~10³–10⁴× the call it wraps, and it breaks `errno` (thread-local) and every thread-affine C library. Withdrawn. |
| "Real Go dedicates an OS thread to a blocking cgo call" | **WRONG mechanism.** Go does P-detach with M reuse; Ms come from a free list, never one per call. |
| "`C.CString`/`C.GoBytes` exist because a raw `string`/`[]T` header was never meant to be handed to C" | **Wrong rationale, and it conflated directions.** cgo permits passing `&b[0]`; the rule constrains *retention past the call*. `CString` exists for NUL-termination and immutability; `GoBytes` runs the other way. |
| ".NET's interop story is stronger *because* its GC compacts" | **Backwards.** Compaction is why pinning exists; Go's non-moving heap is the cheaper side. |
| "Fixed-size C array member → C# `fixed` buffer" | **Wrong for non-primitives.** `[InlineArray]` is the general answer on net10.0. |
| "The native-backed slice is the mechanism for **both directions** of buffer exchange" | **WRONG, and the first revision made it broader.** `slice.cs:396` *throws* for pinning a native-backed slice; Go→C uses the pre-existing `PinnedBuffer`. §4 now splits by direction. |
| Phase 6 validates via `-tests`; ⟨OQ-2⟩ suggests a third-party library | **Internally inconsistent** — `main.go:344` forbids the combination. Re-scoped around the two-step flow. |
| Phase 4 compiles C at convert time, "deterministic" | **Asserts the opposite of what it delivers.** §5.2 states the constraint; the `.s` precedent is invalid because Go's assembler is hermetic and an external gcc is not. |
| Phase 4 → Phase 5 ordering implies composition | **Forbidden by cgo.** `//export` restricts the preamble to declarations only (`cmd/cgo/doc.go:324-329`). |
| Phase 4 binds "exported symbols" | **Cannot bind its own fixture.** `static` helpers export no symbol; static linking leaves no library. Both now named as Phase 4 sub-problems. |
| Phase 5: "no scheduler-registration dance to replicate" | **Self-contradicted** two paragraphs later. The `[ThreadStatic]` establishment *is* the work; plus a panic-boundary and stack-reserve divergence the draft never mentioned. |
| "`SuppressGCTransition` … a way to stall the GC" | **Understated.** It blocks GC suspension *process-wide* (deadlock) and must also exclude callback-taking symbols. |
| "Nothing handles `C` today" | **Imprecise.** Three accommodations exist, all avoidance (§2 Finding 6). |

Review also **added** findings the drafts lacked entirely: the `pkg.Syntax`/`GoFiles` zip defect (§2
Finding 4 — a latent bug independent of cgo), the source ↔ generated file mapping problem (Phase 1),
and the two-layer `LibraryImport` reality (Phase 2).

One reviewer claim was **not** adopted: that `packages.Package` exposes `CgoFiles` showing
`GoFiles=[]` for a cgo package. That field does not exist on `packages.Package` in the pinned x/tools
v0.36.0 (it belongs to `go list`/`go/build`); the divergence in §2 Finding 4 rests on the API's own
doc comments instead, which is the stronger citation and does not depend on a cgo-capable host.

## 10. Open questions

- **⟨OQ-1⟩ Does `go/packages` already deliver typed cgo declarations?** Unresolved — **not verifiable
  on the coordinator machine** (§2 Finding 1). *Recommendation:* run the §2 probe on a cgo-capable host
  before scoping C-parser work. Note it can only ever remove the *parser*, never the Phase 1 driver fix.
- **⟨OQ-2⟩ First validation target, and does Phase 6 own the two-step flow?** Interacts with Phase 4's
  static-linking gap: the obvious third-party candidates are the statically-linked shape.
  *Recommendation:* prefer a target the `-tests` path reaches directly; if only a third-party library
  will do, scope both the two-step flow and the static-link shim into Phase 6.
- **⟨OQ-3⟩ Is any thread-hopping mechanism owed at all?** §5.3 says no for goroutines. The residual case
  is a blocking call on the *main* thread — where Go blocks too. *Recommendation:* ship nothing; revisit
  only against a measured workload.
- **⟨OQ-4⟩ Native side-build timing and artifact home.** *Recommendation:* whichever is chosen, the
  artifact is a git-ignored build output, never corpus content (§5.2). Needs a ruling, since it is the
  point where a binary would otherwise enter the tree.
- **⟨OQ-5⟩ Future of the `-cgo` flag.** Measured inert today (§2 Finding 3). *Recommendation:* keep it
  opt-in through Phase 6; revisit at Phase 6 exit.
- **⟨OQ-6⟩ Document placement — RULED (owner, 2026-08-28).** Filed as a **strategy plan** at
  `docs/PLAN-cgo-interop.md`, beside the five existing `PLAN-*.md`, rather than as a
  `phase4/DESIGN-*.md`. The `PLAN-` prefix is the operative part: per [`Glossary.md`](Glossary.md) a
  strategy plan "fixes a ruled frame — which targets, in which order — and holds its OQ rulings; it
  stays live until its ladder completes," which is exactly this document's shape, and it supplies no
  procedure (that stays in the runbooks). Question closed.
