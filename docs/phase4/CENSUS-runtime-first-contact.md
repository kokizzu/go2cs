# `runtime` — first-contact census

**Lane:** Phase-4 census, i7-5820K coordinator machine (Windows 11, PowerShell 5.1)
**Date:** 2026-08-30
**Tree:** isolated worktree of `C:\Projects\go2cs`, detached at `f8a20a255` (origin/master), clean at start
**Toolchain:** .NET SDK 10.0.400, Go 1.23.12 (windows/amd64), converter built from `src\go2cs` at that commit
**Scope:** MEASUREMENT ONLY. No fixes, no commits, no roster or doc edits. Two probe patches were applied
to *emitted, untracked* artifacts purely to reach the next layer; both are itemised and neither is banked.

---

## 0. Headline

> **`runtime`'s test surface converts completely and compiles nowhere.**
>
> The `-tests` pipeline reproduces **444 of 444** of the package's Windows-eligible top-level test
> functions — an exact set match against the native oracle, zero missing, zero spurious. It then fails
> at the **build** layer against **three** independent walls, each assembly-wide. **Reachable verdicts
> today: 0 of 444.** No test has ever been *run*, so nothing here is a divergence measurement — this is
> a wall census.
>
> Two numbers reframe the package. The oracle runs **un-gated in 118 s with 0 failures** (887 results,
> 843 leaf) — `runtime` is not the slow, hostile suite it was assumed to be. And the conversion layer,
> the one expected to break first, did not break at all.
>
> The good news is the shape: every wall is **rooted, named, and small in site-count**. The largest is
> 49 errors from one mis-qualified type reference; the deepest is 102 errors from one accessibility
> decision that already has its enabling half in place (`InternalsVisibleTo` is emitted). None of the
> three is a raw-metal or GC-semantics problem. **`runtime` is not blocked on runtime semantics; it is
> blocked on four mechanical emission defects.** The semantics bill comes due *after* those, and the
> census prices its first instalment (W4) without reaching it.

---

## 1. The oracle — total verdict surface

`go test -count=1 -short -json runtime`, run natively on this host.

| Metric | Value |
|---|---|
| Wall time (test binary) | **24.5 s** |
| Wall time (incl. build) | 27.8 s |
| Exit code | **0** |
| Total result events | **875** |
| Leaf results | **833** |
| Top-level results | **446** |
| — of which `Test*` | **444** |
| — of which `Example*` | 1 (`ExampleFrames`) |
| — of which `Fuzz*` | 1 (`FuzzPIController`, + 2 seed subtests) |
| Pass (all levels) | 819 |
| Skip (all levels) | 56 |
| **Fail** | **0** |

**The oracle is healthy on this host.** No E2-class host hostility: `-short` runs green in under half a
minute, which is far cheaper than expected for the campaign's largest package. Every test file maps;
all 444 top-level `Test` functions live in `package runtime_test` (the **external** test package) —
**none** in the internal one. The 20 internal (`package runtime`) test files are export helpers, not
tests. That single fact determines the whole census: **runtime's entire test surface reaches production
internals through `export_test.go`'s exported wrappers**, so anything that breaks those wrappers gates
the whole package. Section 4 shows that is exactly what happens.

### 1.1 What `-short` excludes (unmeasured, by design)

52 of the 446 top-level entries skipped. Taxonomy of all 56 skips (incl. subtests):

| Class | n | Examples |
|---|---|---|
| `-short` mode gate | 15 | `TestCheckPtr`, `TestSmhasher*` (7), `TestMemmoveLarge*`, `TestPingPongHog`, `TestPreemptSplitBig`, `TestStopTheWorldDeadlock`, `TestExitHooks`, `TestPrintGC` |
| no cgo | 7 | `TestCoroCgoCallback`, `TestGdbPythonCgo`, `TestVectoredHandler*`, `TestLibraryCtrlHandler`, `TestIssue59213` |
| gcc missing | 7 | `TestStdcallAndCDeclCallbacks`, `TestSyscallN`, `TestFloatArgs/Return`, `TestBigStackCallbackSyscall`, `TestDLLPreloadMitigation`, `TestReturnAfterStackGrowInCallback` |
| gdb/lldb absent | 6 | `TestGdbBacktrace`, `TestGdbPython`, `TestGdbPanic`, `TestGdbConst`, `TestGdbAutotmpTypes`, `TestLldbPython` |
| build-tag gated | 6 | `TestDebugLog*` — "debug log disabled (rebuild with `-tags debuglog`)" |
| known-flaky gate | 5 | `TestGcSys`, `TestRuntimeLockMetricsAndProfile/*` (4, need `-flaky`) |
| opt-in flag gate | 3 | `TestConcurrentMap{Writes,ReadWrite,IterateWrite}` (need `-run_concurrent_map_tests`) |
| stress/time gate | 2 | `TestMemoryLimit`, `TestMemoryLimitNoGCPercent` |
| AES hash in use | 2 | `TestMemHash32Equality`, `TestMemHash64Equality` |
| platform | 1 | `TestFakeTime` — "faketime not supported on windows" |
| stress (non-short) | 7 | remaining `TestSmhasher*` / `TestCollisions` subtests |

**Consequence for the roster:** a future `runtime` row banks against the *un-gated* suite (the pipeline
passes no `-short`), so this skip list is **not** the row's unmeasured set. §6 measures the un-gated run
and shows 19 of these 52 come back green, leaving 33 genuinely environment-gated.

---

## 2. Layer 1 — CONVERSION: **passes, completely**

`go2cs -tests -test-action all -test-timeout 30m` converted production **and** tests with **zero errors**.
Only WARNINGs (taxonomy in §2.3).

| Metric | Value |
|---|---|
| `_test.go` files in GOROOT `runtime` | 110 |
| Emitted `*_test.cs` | **77** (+2 metadata: `package_info_internal_test.cs`, `package_init_internal_test.cs`) |
| Not emitted | 33 |
| Emitted host / metadata | `go2cs_test_host.cs`, `package_test_info.cs`, `runtime.tests.csproj` |
| `ProjectReference`s in `runtime.tests.csproj` | 66 |

### 2.1 The 33 non-emitted files are correct exclusions

32 of 33 are files **Go itself does not compile** on `windows/amd64` — verified against each file's own
build constraint, not inferred:

- other-OS (8): `export_{aix,darwin,linux}_test`, `memmove_linux_amd64_test`, `norace_linux_test`, `numcpu_freebsd_test`, `runtime_linux_test`, `conv_wasm_test`
- unix-only (7): `crash_unix_test`, `nbpipe_test`, `nbpipe_pipe_test`, `runtime_mmap_test`, `runtime_unix_test`, `runtime-gdb_unix_test`, `syscall_unix_test`
- unix export helpers (6): `export_debug_test`, `export_debug_amd64_test`, `export_mmap_test`, `export_pipe_test`, `export_pipe2_test`, `export_unix_test`
- other-arch (4): `export_arm_test`, `export_debug_arm64_test`, `export_debug_ppc64le_test`, `vlop_arm_test`
- cgo (2): `crash_cgo_test`, `trace_cgo_test`
- platform-constrained, confirmed by reading the constraint (5): `debug_test` (`linux`), `fds_test` (`unix`), `security_test` (`unix`), `semasleep_test` (`!windows`), `vdso_test` (`freebsd||linux`)

The **one** genuine omission is `example_test.go` — no build constraint, contains only `Example*`
functions. It costs exactly **1 verdict** (`ExampleFrames`). The host registers no `Example*` and no
`Fuzz*` entries.

### 2.2 Test-surface reproduction is EXACT

Registered `Test` names in `go2cs_test_host.cs` vs. the oracle's top-level `Test` set, compared
case-sensitively as sets:

```
host Test names (case-sensitive): 444
oracle top-level Test:            444
HOST-only:   []
ORACLE-only: []
```

**Zero drift.** This is the census's strongest positive result and it is worth stating plainly: the
conversion layer — the layer everyone expected to fail first on the campaign's biggest, weirdest package
— reproduced the entire eligible test surface exactly. *(Method note: an initial run of this diff used
`Select-String` without `-CaseSensitive` and reported a spurious 445th name, `testdata`, matching
`"Test..."` case-insensitively. The corrected, case-sensitive comparison is the one above.)*

### 2.3 Conversion warnings — the complete taxonomy

The run emitted **no errors** and exited 0. Every warning it did emit, classified:

| Warning | n | Meaning | Status |
|---|---|---|---|
| `Go const converted to C# using 'unsafe.Sizeof' may not match run-time value` | **12** | a `const` whose value is a Go struct size — the C# layout may differ | **latent run-layer risk**, unmeasured (see §5) |
| `Go 'unsafe.Sizeof' did not resolve to a constant - emitting run-time form` (+ its paired `may not produce same value` line) | **4** | `Sizeof` kept as a run-time expression | same class; one of the 4 is the **cause of W2c** |
| `Unresolved dynamic struct type` | **3** | an anonymous type the test pass could not name | **cause of W2a** — see §W2 |

The 12 const-`Sizeof` sites are `chan.go` (`hchanSize`), `malloc.go` (`arenaMetaSize`), `mheap.go`
(`gcBitsHeaderBytes`), `mpagealloc.go` (`l2Size`, `pallocSumBytes`), `mpagecache.go` (`pageCachePages`),
`netpoll.go` (`pdSize`), `pinner.go` (`pinnerRefStoreSize`), `traceregion.go`
(`traceRegionAllocBlockData`), plus three in test files (`export_test.go` `RuntimeHmapSize`, `gc_test.go`
`n`, `sizeof_test.go` `_64bit`).

**The signal-to-noise here is the point:** 3 of the 19 warnings are a *guaranteed* compile failure and
the run still exited 0. That asymmetry is W2b.

---

## 3. Layer 2 — BUILD: three walls

The pipeline's own `-test-action all` died **78 s** in, at `dotnet publish`, before compiling anything.
Each wall below was reached by neutralising the one before it; every neutralisation is named.

### W1 — the linkname-push project cycle (**restore layer; kills the run outright**)

```
error MSB4006: There is a circular dependency in the target dependency graph involving target
"_GenerateRestoreProjectPathWalk".
  [...\src\core\internal\syscall\windows\internal.syscall.windows.csproj]
```

**Root, positively controlled.** The `-tests` run **rewrites the production tree**. Two coupled edits:

`src/core/runtime/runtime.csproj`
```diff
-  <ItemGroup Condition="'$(GoTargetOS)'=='windows'" />
+  <ItemGroup Condition="'$(GoTargetOS)'=='windows'">
+    <ProjectReference Include="$(go2csPath)core/internal/syscall/windows/internal.syscall.windows.csproj" />
+  </ItemGroup>
```

`src/core/runtime/windows/os_windows.cs`
```diff
 //go:linkname canUseLongPaths internal/syscall/windows.CanUseLongPaths
-internal static bool canUseLongPaths;
+internal static bool canUseLongPaths { get => go.@internal.syscall.windows_package.CanUseLongPaths; set => go.@internal.syscall.windows_package.CanUseLongPaths = value; }
```

That reference closes a loop in the **production** project graph. A DFS over all 306 production
`.csproj` files found **6 cycles**, every one through the new edge:

```
runtime -> internal.syscall.windows -> runtime
runtime -> internal.syscall.windows -> sync -> runtime
errors -> internal.reflectlite -> runtime -> internal.syscall.windows -> errors
errors -> internal.reflectlite -> runtime -> internal.syscall.windows -> syscall -> errors
errors -> internal.reflectlite -> runtime -> internal.syscall.windows -> syscall -> internal.oserror -> errors
(+1 more of the same shape)
```

**Positive control:** reverting that single `ProjectReference` and re-running the same DFS yields
**0 cycles**. The attribution is exact.

**Why the asymmetry.** A plain `-stdlib` run converts `runtime` *before* `internal/syscall/windows`
(the latter imports `syscall`, which imports `runtime`), so the linkname-push target is not in scope and
the converter emits a plain local field. The `-tests` closure pulls `internal/syscall/windows` in — two
external test files import it (`runtime-seh_windows_test.go`, `syscall_windows_test.go`, both
`package runtime_test`) — the push now resolves, and the converter wires it *into the production
package*. Go has no such problem: `runtime_test` is a separate compilation unit that may legally import
packages importing `runtime`.

**Rooted-vs-symptom:** rooted. The MSB4006 is the symptom; the emission asymmetry is the cause. Note the
converter *already* mitigates a **different** MSB4006 family (shared `obj/` between the colocated
production and test projects — documented in the emitted csproj's own header comment). This is a second,
unrelated instance.

**Estimate:** arc-with-price. The remedy is a ruling as much as code — either the push must not
introduce a production project reference when it would close a cycle (degrade to the current unwired
field, loudly), or `-tests` must stop rewriting the production csproj/emission at all. **Needs-ruling**
on which.

> **Latent corpus bug found in passing (out of scope, worth a chip).** At HEAD the push is silently
> **not wired**: `runtime.canUseLongPaths` is a separate field from
> `internal/syscall/windows.CanUseLongPaths`. `initLongPathSupport()` sets runtime's copy; the
> `internal/syscall/windows` copy stays `false`, so `os`'s long-path support is disabled on Windows. A
> `//go:linkname` push that does not push, shipping today. Discovered by this census, not fixed by it.

**Neutralised for the probe by** `git checkout HEAD -- runtime.csproj os_windows.cs` — i.e. restoring
the exact `-stdlib` emission, a consistent pair. *(An intermediate attempt that restored only the csproj
left the property form in place and produced 2 × `CS0234 'syscall' does not exist in namespace
'go.@internal'` — recorded because it confirms the two edits are coupled.)*

---

### W2 — unresolved anonymous types emitted as **literal Go source** (202 errors)

Build reached the compiler: **202 errors, all in `runtime.tests.csproj`. The production assembly
compiled clean.** All 202 came from **2 files** and, underneath, **3 types / 5 sites**.

**Verbatim, `export_test.cs:288`:**
```csharp
public static Func<interface{F()}, uintptr, uintptr> IfaceHash;
```

**Verbatim, `export_test.cs:1226`:**
```csharp
Δp.of(global::go.runtime_package.pageAlloc.Ꮡscav).of(struct{index runtime.scavengeIndex; releasedBg internal/runtime/atomic.Uintptr; releasedEager internal/runtime/atomic.Uintptr}.Ꮡindex).alloc(ci, pallocChunkPages);
```

Go type syntax — braces, semicolons, slash-bearing import paths — pasted into a C# expression. The
parser derails and cascades: 106 distinct error lines in `export_test.cs`, of which only **5** are real
sites (1 × `interface{F()}` at L288; 3 × the `scavengeIndex` struct at L1226/1227/1249; 1 × the
`spinAfterRaggedBarrier` struct at L2084).

**Root, definitively established.** The production emission **already lifted all three types and named
them correctly**:

| Anonymous Go type | Lifted name in production | Proof site |
|---|---|---|
| `interface{F()}` | `ifaceHash_i` | `alg.cs:518` — `[GoType("dyn")] internal partial interface ifaceHash_i` |
| `struct{index scavengeIndex; releasedBg …; releasedEager …}` | `pageAlloc_scav` | `mgcsweep.cs:428` — `…of(pageAlloc_scav.ᏑreleasedBg)` |
| `struct{spinAfterRaggedBarrier …; restartedDueTo27993 bool}` | `gcDebugMarkDoneᴛ1` | `mgc.cs:801` — `…of(gcDebugMarkDoneᴛ1.ᏑspinAfterRaggedBarrier)` |

So this is **not** "the converter cannot lift these shapes". It is: **the test pass does not consult the
production pass's anonymous-type lift registry, and falls back to printing the Go type text.** Rooted,
one family, one mechanism.

**The converter already knows.** The conversion log carries exactly three warnings —

```
WARNING: Unresolved dynamic struct type: interface{F()}
WARNING: Unresolved dynamic struct type: struct{index runtime.scavengeIndex; releasedBg internal/runtime/atomic.Uintptr; releasedEager internal/runtime/atomic.Uintptr}
WARNING: Unresolved dynamic struct type: struct{spinAfterRaggedBarrier internal/runtime/atomic.Bool; restartedDueTo27993 bool}
```

— one per type, **zero false positives, zero false negatives**, and the run still **exits 0**. That is a
free, already-implemented gate signal being discarded.

**Estimate:** the emission defect is **arc-with-price** (reuse the production lift registry in the test
pass). The warning is a **gate-candidate** and a cheap one: *"Unresolved dynamic struct type" should be
FATAL for a `-tests` conversion*, because it provably always yields uncompilable C#. Same family in
spirit as CNR's NOT-MEASURED rule — a warning that guarantees a broken artifact must not exit 0.

**Neutralised for the probe** by substituting the production's own lifted names at the 5 sites — i.e.
writing precisely what a fixed converter emits, so the probe stays faithful.

**A second, unrelated defect surfaced in the same round (3 errors, `arena_test.cs:60`) — kept separate:**

```csharp
[GoType("[524289]ж<smallPointer>")] /* [runtime_internal_test_package.UserArenaChunkBytes / /* unsafe.Sizeof(&smallPointer{}) */ (uintptr)8 + 1]ж<smallPointer> */
```

A **nested block comment**. C# does not nest `/* */`, so the outer comment ends at the inner `*/` and the
tail spills into code. The `[GoType]` attribute itself is correct (`524289` resolved). Cause: the
`unsafe.Sizeof` run-time-form comment is interpolated into an enclosing type comment. Rooted, cosmetic
in effect but fatal in practice, and **certain to recur** wherever a `Sizeof` appears inside an array
length. Estimate: **arc-with-price**, small.

---

### W3 — the `export_test` accessibility & qualification wall (154 errors)

With W1 and W2 neutralised the build advances and stops here: **154 unique errors, all still confined to
`runtime.tests.csproj`**, spread over ~50 files of which most are **go2cs-gen generated** adapters
(`go.runtime_internal_test_package.*.g.cs`).

| Family | Codes | n | Root |
|---|---|---|---|
| **W3a** accessibility | CS0050 34, CS0053 34, CS0051 19, CS0056 7, CS0057 7, CS0052 1 | **102** | `public` test wrappers exposing `internal` production types |
| **W3b** qualification | CS0426 | **49** | test-declared type qualified to the **production** package class |
| **W3c** metadata | CS0246 2, CS0234 1 | **3** | `unsafe` namespace + `go.go.` double root escape |

**W3a — verbatim:**
```
export_debuglog_test.cs(24): error CS0050: Inconsistent accessibility: return type
  'ж<runtime_package.dlogger>' is less accessible than method
  'runtime_internal_test_package.B(ж<runtime_package.dlogger>, bool)'
```
from
```csharp
public static ж<global::go.runtime_package.dlogger> B(this ж<global::go.runtime_package.dlogger> ᏑL, bool x) { … }
```

**This is a Go↔C# structural mismatch, and the census's most important sizing result is that it is
*smaller* than it looks.** In Go, `export_test.go` is compiled *into* package `runtime`, so an exported
name over an unexported type costs nothing. In go2cs the test assembly **references** the production
assembly instead of recompiling its sources (a deliberate design, documented in the emitted csproj
header). That makes production types `internal`-to-another-assembly.

Critically, **`InternalsVisibleTo` is already emitted** — `runtime.csproj:33`:
```xml
<InternalsVisibleTo Include="$(AssemblyName).tests" />
```
The errors are *"less accessible"*, never *"inaccessible"*: the test assembly **can see** these types.
The only thing wrong is the **accessibility level the converter picks for the test wrappers** — `public`
where `internal` would satisfy C# and lose nothing (nothing outside the test assembly consumes them).
So the remedy is a level change plus the matching generator change, not an architecture change.
**Estimate: arc-with-price, and the price is far below what "102 errors across 50 files" suggests.**

**W3b — verbatim:**
```
export_test.cs(23): error CS0426: The type name 'AddrRange' does not exist in the type 'runtime_package'
```
from
```csharp
using ꓸꓸꓸAddrRange = Span<runtime_package.AddrRange>;
```
`AddrRange` is declared **in the test package** — `export_test.cs:1064`,
`[GoType] public partial struct AddrRange` — so the alias must name
`runtime_internal_test_package.AddrRange`. The slice/array alias minting qualifies test-declared types
to the production package class. Rooted, one mechanism, 49 errors. **Estimate: arc-with-price, small.**

**W3c** — `package_test_info.cs:116` emits `global::go.go.runtime_internal_test_package.TestingT` (the
known `go.go.` double-root-escape family) and `:132` emits `objWith<@unsafe.Pointer>` where
`unsafe_package.Pointer` is meant. 3 errors, two known families. **Estimate: arc-with-price, trivial.**

---

### 2026-08-30 amendment — effort estimates corrected against measurement (i9, W3 arc)

W3a and W3b are closed (`claude/i9-w3-accessibility`, commits `2b08b924f`/`b943129cf`; design at
`docs/phase4/DESIGN-w3a-wrapper-scaffolding.md`), verified fresh against the current corpus: 154 → 39
→ 11, not carried from this document's original counts. Two corrections to how this census priced
the remainder:

- **W3c's counts were exactly right (3 errors, two families) — its "trivial" effort estimate was
  not.** Both families were invisible until the wall in front of them (W3a's wrapper-scaffolding
  residual, not originally distinguished from the rest of W3a here) came down — this census's own
  §3 "W2 gates W3" ordering applies one level deeper than stated: within W3, the accessibility wall
  gates the qualification wall reaching either W3c family at all. Neither is a one-line patch once
  actually reached: family 1 (`go.go.` double-root escape) needs `isStrippedGoPathPackageRef`'s
  registry taught about synthetic test-bridge classes, which it was never populated for; family 2
  (`@unsafe.` unresolved in a witness argument) needs the existing `visitTypeSpec.go` precedent
  fix's rewrite extended to a second, differently-built emission site. Both real, both small, but
  "trivial" undersold the investigation each one costs — the count was the free part, not the fix.
- **A fourth W3a-family site was not in the original census at all**: `AddrRanges`'s promoted
  `cloneInto` (inherited from its embedded production field by Go's ordinary struct-embedding
  promotion) forwards through an unconditionally-public overload, the same shape as the
  constructor/`.Value`/operator sites W3a's own fix now covers, just a different emission path
  (`TypeGenerator`'s promoted-member forwarding, not `InheritedTypeTemplate`'s wrapper scaffolding).
  2 of the original 102 were this, uncounted because — like the two W3c families — it was behind the
  same wall.

Net: the wall's real floor is 11 errors behind 154, not the 3 this document estimated for W3c alone
— the gap is not a miscount, it is unmeasured depth this census's own build-log method could not see
past a wall it hadn't yet reached. Full detail and verification numbers: mailbox 2026-08-30, i9.

---

## 4. Why *all* 444 are behind these walls

Not one of the three walls is test-specific — each breaks the **test assembly as a whole**, and there is
no partial-link path. Combined with §1's structural finding (100 % of runtime's tests are external, and
they reach internals **only** through `export_test.go`), the arithmetic is blunt:

**Reachable verdicts today: 0 / 444 (0 %).**

`-test-filter`, the block-gated census mechanism, **cannot help here** — it filters *execution*, and
nothing executes; the file still has to compile. There is no diagnostic slice of `runtime` available
until W1–W3 land.

---

## 5. The run layer, priced but not reached

Nothing ran, so nothing below is measured — this is a forward price, flagged not chased.

**W4 — the subprocess wall.** **91 of 444 tests (20.5 %)** call `runTestProg` / `buildTestProg` /
`testenv.Command` / `exec.Command`. `runTestProg` invokes the **Go toolchain** to build
`runtime/testdata/testprog` and runs the resulting binary, then asserts on its output. The converted
host has self-exec machinery (the published single-file host an `os/exec`-style test re-execs), but that
covers re-executing *the host*; these tests need a **different, converted, separately-built program**.
That is its own arc, independent of W1–W3.

| File | subprocess tests |
|---|---|
| `crash_test.go` | 39 |
| `syscall_windows_test.go` | 11 |
| `proc_test.go` | 7 |
| `signal_windows_test.go` | 5 |
| `runtime-gdb_test.go` | 5 |
| `gc_test.go` | 4 |
| `stack_test.go` | 3 |
| `malloc_test.go` | 2 |
| (others) | 15 |

**Expectations carried in — status after first contact:**

- **SCHED-S1 (threads-per-goroutine)** — **not reached, not observed.** `proc_test.go` (29 tests) and
  `chan_test.go` (14) are the exposure. Unbilled.
- **Platform-liveness / tier-0 (frame-local liveness)** — **not reached.** `gcinfo_test.go`,
  `unsafepoint_test.go`, `stack_test.go` (25) and `traceback_test.go` are the smell. **Flagged, not
  chased**, per the standing ruling that this class is handled elsewhere.
- **PartialStubGenerator stubs (getg, profiling internals)** — **did not surface as compile errors.**
  Production compiled clean; stubs are a *run*-layer risk here, not a build-layer one. Unbilled.
- **Raw-metal by definition (GC internals, memory-layout walkers)** — **did not surface either.** The
  21 `[module: GoManualConversion]`-marked files under `src/core/runtime` carried production through.
  The 12 `unsafe.Sizeof` WARNINGs (see §2.3) are the standing latent risk: each is a constant the C# side
  may compute differently from Go, and `sizeof_test.go` exists precisely to catch that — behind the walls.

**One inherited, honestly-labelled datum:** runtime's *production* side demonstrably **runs** — every
converted behavioral program links it, and the suite is 651/651 at master. That is banked repository
evidence, **not** measured by this census. It bounds the risk: W1–W3 are test-emission defects sitting
on top of a production assembly that works.

---

## 6. Full-suite oracle (roster pricing) — **measured**

The pipeline passes **no** `-short` (`goArgs := []string{"test", "-json", "-count=1", "-timeout", …}`),
so a real `runtime` row banks against the **un-gated** suite. Measured on this host:

`go test -count=1 -timeout 40m -json runtime`

| Metric | `-short` | **full (un-gated)** | Δ |
|---|---|---|---|
| Wall time (test binary) | 24.5 s | **118.2 s** | ×4.8 |
| Wall time (incl. build) | 27.8 s | **120.8 s** | — |
| Exit code | 0 | **0** | — |
| Total results | 875 | **887** | +12 |
| Leaf results | 833 | **843** | +10 |
| Top-level | 446 | 446 | 0 |
| — pass | 394 | **413** | **+19** |
| — skip | 52 | **33** | −19 |
| **Fail** | **0** | **0** | — |

**This is the census's second-best news, and it changes the row's price materially.** `runtime` was
expected to be a tens-of-minutes suite; un-gated it is **two minutes and zero failures** on
laptop-slower-than-laptop hardware. Concretely:

- **No E2-class host hostility, gated or un-gated.** The oracle is trustworthy here.
- **The Go side needs no `$longTimeouts` floor** in `run-validated-sweep.ps1`. (The *C#* side is
  unmeasured and could still need one — `hash/maphash` is the standing precedent for a suite that is
  seconds in Go and ~15 min in C#. `runtime`'s 18 `hash_test.go` tests are the same `smhasher` family,
  so a floor may well be owed once the row is reachable; it cannot be sized today.)
- **The verdict denominator for a future row is 843 leaf / 446 top-level**, not 444 — the Example and
  Fuzz entries are in the surface and the host currently registers neither (§2.1).
- The 19 tests `-short` gates all pass un-gated, so **§1.1's "unmeasured" list is now measured and
  green**; only the 33 environment-gated skips (cgo, gcc, gdb/lldb, `-tags debuglog`, `-flaky`,
  `-run_concurrent_map_tests`, faketime) remain genuinely unmeasured, and none of them is a go2cs
  concern.

---

## 7. Walls in dependency order

| # | Wall | Layer | Errors | Sites | Rooted? | Estimate |
|---|---|---|---|---|---|---|
| **W1** | linkname-push closes a production project cycle | restore (MSB4006) | fatal | 1 | yes, positive-controlled | arc-with-price + **needs-ruling** |
| **W2a** | anonymous types leak as literal Go source | compile | 202 | 3 types / 5 sites | yes | arc-with-price |
| **W2b** | "Unresolved dynamic struct type" warns, exits 0 | gate | — | — | yes | **gate-candidate** (cheap, high value) |
| **W2c** | nested block comment from `unsafe.Sizeof` form | compile | 3 | 1 | yes | arc-with-price, small |
| **W3a** | `public` test wrappers over `internal` production types | compile | 102 | ~50 files, 1 mechanism | yes | arc-with-price (**IVT already present**) |
| **W3b** | test-declared type qualified to production package | compile | 49 | 1 mechanism | yes | arc-with-price, small |
| **W3c** | `go.go.` escape + `unsafe` qualification in metadata | compile | 3 | 2 known families | yes | arc-with-price, trivial |
| **W4** | 91 tests build+exec a separate Go program | run (unreached) | — | 91 tests | priced, not observed | separate arc |
| **W5+** | scheduler / liveness / stub / raw-metal semantics | run (unreached) | — | — | **unbilled** | unknown until W1–W3 land |

**Ordering is strict.** W1 gates everything (nothing restores). W2 gates W3 (the parser derails before
the binder runs — which is exactly why W3's 154 errors were invisible in the first build). W3 gates the
run layer. W4 and beyond cannot be priced from a build log at all; **the first honest estimate of
runtime's semantic bill is only available after W3 lands**, and this census deliberately declines to
guess it.

---

## 8. Probe honesty

Two neutralisations were applied to reach deeper layers. Both touched **untracked/emitted** artifacts
only; the worktree is restored and **nothing is banked**.

1. **W1** — `git checkout HEAD --` on `runtime.csproj` and `windows/os_windows.cs`, restoring the exact
   `-stdlib` emission. Not an invention: it is the pair the corpus ships.
2. **W2** — 5 substitutions in `export_test.cs` using the production emission's **own** lifted names
   (`ifaceHash_i`, `pageAlloc_scav`, `gcDebugMarkDoneᴛ1`), plus un-nesting one comment in
   `arena_test.cs`. Faithful: this is what a fixed converter emits.

Everything reported downstream of a probe is labelled with the wall it sits behind. **No verdict in this
document comes from a test run, because no test ran.**

---

## 9. Reproduction ledger

| Step | Command | Wall | Result |
|---|---|---|---|
| Oracle (`-short`) | `go test -count=1 -short -json runtime` | 27.8 s | exit 0; 875 results, 0 fail |
| Oracle (full) | `go test -count=1 -timeout 40m -json runtime` | 120.8 s | exit 0; 887 results, 0 fail |
| Pipeline | `go2cs -tests -test-action all -test-timeout 30m -go2cspath <wt>\src <GOROOT>\src\runtime <wt>\src\core\runtime` | 78 s | **W1** MSB4006 |
| Build probe 1 | `-test-action build` (csproj restored only) | 87 s | 2 × CS0234 — the coupled-edit control |
| Build probe 2 | `-test-action build` (W1 neutralised) | 141 s | **W2**: 202 errors |
| Build probe 3 | `-test-action build` (W2 neutralised) | 71 s | **W3**: 154 errors |

Converter build: 6.9 s. Production assembly compiled clean in every probe.

**Total lane wall time: ~22 minutes**, of which the two oracles are 2.5 min and the three build probes
5 min. `runtime` is *cheap* to census; it was expensive only to have never tried.

---

## 10. Recommended next moves (for the coordinator — not actioned here)

Ordered by the dependency chain of §7, cheapest-first within each tier:

1. **W2b, the gate** — make "Unresolved dynamic struct type" fatal under `-tests`. Smallest change in
   this document, and it converts a silent exit-0 into a named failure for *every* future first-contact,
   not just `runtime`. Positive control is free: this package reproduces it three times.
2. **W1, the ruling** — decide whether a linkname push may introduce a production `ProjectReference`,
   and whether `-tests` may rewrite the production emission at all. Blocks everything else; also carries
   the shipped `canUseLongPaths` bug (§W1) as a separate chip.
3. **W2a / W2c / W3b / W3c** — four independent, small, mechanical emission fixes (lift-registry reuse;
   comment nesting; test-vs-production qualification; two known escape families). None interacts with
   the others.
4. **W3a** — the accessibility level for test wrappers. Largest error count, but `InternalsVisibleTo` is
   already in place, so it is a level change plus the matching go2cs-gen change, not an architecture
   change. Landing it is what first makes *any* `runtime` verdict reachable.
5. **Then, and only then, re-census.** The semantic bill (W4 subprocess arc, SCHED-S1, liveness, stubs,
   raw-metal) is unbilled by design — a build log cannot price it, and guessing it now would be the one
   dishonest number this document could contain.

**What this census changes about `runtime`'s reputation:** it was the campaign's largest unopened box and
was assumed to be blocked on Go runtime semantics that C# cannot express. It is not — not yet. It is
blocked on **four mechanical emission defects**, all rooted, all with sites in the single digits, sitting
on top of a production assembly that compiles clean and demonstrably runs. The semantics may still bite
hard at the run layer; nothing here says otherwise. But the *first* wall between go2cs and the biggest
package in the standard library is a project-reference cycle, and the second is a missing lookup in a
registry that already holds the right answer.
