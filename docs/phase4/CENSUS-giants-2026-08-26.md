# CENSUS — the four deferred giants at Go 1.23.12 / net10.0

**Date:** 2026-08-26 · **Base:** `4e1a2006b` (master, B2 I1+I2 merge) · **Lane:** `claude/giants-census`
**Host:** coordinator i7-5820K (6C/12T, Windows 11), three sibling lanes active — all four pipelines run **sequentially**
**Toolchain:** go1.23.12 (`C:\Users\<user>\sdk\go1.23.12`), .NET 10 (`C:\Users\<user>\dotnet10`), `MSBUILDDISABLENODEREUSE=1`

> **This is a RECORD, not a plan and not a bank.** MEASUREMENT ONLY — no converter change, no golib change, no
> corpus bank. Every tracked file the pipelines touched was classified and restored; the worktree was verified
> clean (`git status --porcelain` → 0 entries) before this file was written. Amend with dated blocks; do not rewrite.

---

## 0. Why this census exists

The "deferred" labels on `net`, `net/http`, `reflect` and `runtime` predate the socket-wall collapse, the
exec-wall opening, the init-order arc landing corpus-wide, and the B2 I1+I2 merge. Walls move silently in this
project — the Windows socket wall fell without anyone touching it — and **an unmeasured giant is an unpriced
asset.** This pass re-measures all four at current master and prices the distance to a first bank.

**Headline: the labels were wrong in three cases out of four, and every remaining wall is a NAMED, SINGLE-SITE
converter defect rather than a capability wall.**

| Giant | Go verdicts | C# matching | Status at this base | First wall | Sites |
|---|---:|---:|---|---|---:|
| `reflect` | **396** | 0 | conversion-blocked | float-kind constant as array length | 3 call sites (1 trigger) |
| `net` | **474** | **120** | **runs; partial** | `syscall.Environ()` kills the CLR | 1 |
| `net/http` | **1,352** | 0 | conversion-blocked | capture-prologue in expression position | 7 sites / 2 files |
| `runtime` | **883** | 0 | build-blocked | `//go:linkname` → compile-time edge → cycle | 1 (+2 behind it) |
| **total** | **3,105** | **120** | | | |

For scale: the banked roster is 18,598 matching verdicts across 162 rows. These four hold **3,105 further
verdicts — roughly a sixth of everything banked to date — and 120 of them already match today.**

---

## 1. Method

Per package, one detached run (turn-boundary reaping defeats an inline child):

```
go2cs -tests -test-action all -test-timeout 20m -go2cspath <worktree>\src \
      C:\Users\<user>\sdk\go1.23.12\src\<pkg>  <worktree>\src\core\<pkg>
```

GOROOT passed in the **backslash** spelling `go env GOROOT` returns (the forward-slash form silently misroutes
the whole emission into `namespace go.std.*`). `-test-timeout` always explicit — the hand-invoked default is
2 m, five times smaller than the sweep's, and is a documented source of false mass-empty verdicts.

Counts are derived from each package's own `go2cs_test_comparison.json` (`go` and `csharp` verdict maps), not
from the tool's summary fields, so the arithmetic closes visibly: for `net`, 120 matching + 13 divergent +
341 empty = 474 = the Go denominator.

Between packages: `.tests` `obj/`+`bin/` purged, and process survey by **verified path parentage**
(`$_.Path.StartsWith($worktree)`) — never by bare name. Two `go2cs.exe` were alive throughout belonging to
sibling lanes; they were left untouched.

**Empty-set shape** was read before any diagnosis, per the standing rule: a contiguous alphabetical tail is a
run that died partway, scattered empties are genuine divergence, all-empty is the file-lock case.

---

## 2. `reflect` — the recorded wall reproduces, and is now isolated to one construct

### Prior state
Board (`BOARD-next-validation-candidates.md:4696-4699`, reaffirmed `:6925-6926`) recorded one hard converter
failure in 108 packages: `convert test file "…\reflect\all_test.go": 1e+06 not an Int`, described as "a
float-shaped untyped constant reaching a path that demands `constant.Int`". Never closed, no denominator ever
recorded, no second attempt.

### Measured
Reproduces **exactly**, in 29 s, fatal:

```
Failed to convert package tests in "…\src\reflect":
convert test file "…\src\reflect\all_test.go": 1e+06 not an Int
```

### Root cause — isolated to the construct, not merely the file

The trigger is `reflect/all_test.go:5185-5193`, `TestSliceOverflow`:

```go
const S = 1e6
...
var x [S]byte
```

`1e6` is an untyped **float**-kind `constant.Value` (go/constant renders it `1e+06`). `constant.Int64Val`
*panics* on a float-kind value — it only returns `(0,false)` for `unknownVal`. Three converter sites pass a
raw value with no `constant.ToInt` normalisation:

- `src/go2cs/visitArrayType.go:119` — `intLength, _ := constant.Int64Val(length)`
- `src/go2cs/visitValueSpec.go:296` — same, local branch
- `src/go2cs/visitValueSpec.go:344` — `constant.Int64Val(tv.Value)`, package-scope branch

Neighbouring sites already do it right (`convCompositeLit.go:1140`, `convKeyValueExpr.go:377`,
`convUnaryExpr.go:1125` all wrap in `constant.ToInt`), so this is an omission, not a design gap.

**Isolated by three minimal probes** (converted standalone, outside the repo tree):

| probe | construct | result |
|---|---|---|
| `constonly` | `const S = 1e2; s := uint(S)` | **clean**, exit 0, no warning |
| `localarr` | `const S = 1e2; var x [S]byte` | `WARNING: visit file error: 100 not an Int` |
| `globalarr` | `const G = 1e2; var y [G]byte` | `WARNING: visit file error: 100 not an Int` |

So the trigger is **an array length whose constant is float-kind**, in either scope. A float-kind constant
used anywhere else converts cleanly.

### A second, transferable finding: the same defect is advisory in production and FATAL in `-tests`

Note the probes report a *recovered* `visit file error` **with exit 0**. A single-package or `-stdlib`
conversion survives this defect as a warning; the `-tests` pipeline escalates the identical panic to a hard
package abort (`testConversion.go:1981` recovers it into an `err`). That asymmetry is why the corpus has never
shown it and why it presents as a reflect-specific wall. It is consistent with CNR's own doctrine — a
recovered "visit file error" is reported as NOT MEASURED — but it means **any converter panic of this shape is
a silent warning on the production side and a total blocker on the test side.**

### Priced read
- **Distance to first measurement: one converter fix, three call sites** (`constant.ToInt` normalisation), plus
  a behavioural regression test for a float-kind array length.
- **Behind it: genuinely unmeasured.** No verdict has ever been observed from reflect's own suite.
- **Denominator, recorded here for the first time: 396 verdicts** (395 pass, 1 skip; 209 top-level tests;
  Go runs the suite in **9 s** on this host).
- **Confidence the residual is shallow: moderate-to-good, by proxy.** 65 of 162 banked rows import `reflect`
  directly, including the five largest reflect consumers by verdict count (`go/types` 557, `encoding/json` 491,
  `crypto/tls` 400, `encoding/xml` 386, `html/template` 243). The reflect *runtime* is the most heavily
  exercised unbanked code in the corpus. What is untested is reflect's own suite, which probes `StructOf`,
  `MakeFunc`, GC interaction and unsafe layout far harder than any consumer does.
- **Arcs its residuals may wait on:** B2 kind-split (directly — reflect's suite is the densest exercise of
  `Kind` semantics in the tree), generic-typeargs.

---

## 3. `net` — the biggest correction in this census: it runs, and it already matches 120

### Prior state
The board's *first* `net` census (`BOARD:10528-10545`) recorded: suite converts, compiles and runs, but the
host produced **25 verdicts** before a 61-minute kill, **1 matched**, 461 `Go="pass" C#=""`. Its conclusion was
"a severe SLOWDOWN, not a correctness wall — `net` needs a poller/performance arc before its census is even
measurable." Latest disposition (`BOARD:18138`) keeps `net` a future arc on both platforms.

### Measured — the throughput diagnosis no longer holds

Whole pipeline, convert → build → run → compare: **907 s** at `-test-timeout 20m` (Go's own side takes ~696 s
on this host). It did **not** time out.

| measure | prior census | **this census** |
|---|---:|---:|
| Go verdicts | 474 | **474** (404 pass, 44 skip, 26 fail) |
| C# verdicts produced | 25 | **133** |
| **matching** | **1** | **120** |
| real divergences (both sides non-empty) | — | **13** |
| C# empty | 461 | **341** |

**120 matching verdicts, from 1.** The suite is not throughput-walled any more. It is walled by a single
process-killing crash, which the previous 61-minute run never reached because it was still crawling.

### Empty-set shape — read before diagnosis

- Last name with a C# verdict, in sorted order: `TestInterfaceAddrs`.
- Names sorting **after** it: **314, of which 314 are empty** — a perfect contiguous alphabetical tail, i.e.
  **the host process died**, exactly as the standing rule predicts.
- Names sorting **at or before** it: 160, of which **27 empty** — genuinely scattered.

### Root cause of the tail — `syscall.Environ()` hard-kills the CLR

```
Fatal error. Internal CLR error. (0x80131506)
   at go.ж`1[[System.UInt16…]].op_Explicit(go.uintptr)
   at go.syscall_package.Environ()
   at go.internal.syscall.execenv_package.Default(go.ж`1<SysProcAttr>)
   at go.os.exec_package.environ(Cmd ByRef)  →  Start → Run → CombinedOutput
   at go.net_internal_test_package.runCmd(…)
   at go.net_internal_test_package.TestInterfaceAddrsWithNetsh(…)
```

`src/core/syscall/windows/env_windows.cs:73` walks the raw environment block returned by
`GetEnvironmentStrings()`:

```csharp
@unsafe.Pointer end = new @unsafe.Pointer(envp);
while (~(ж<uint16>)(uintptr)(end) != 0) {          // ← EE error here
    end = (uintptr)@unsafe.Add(end, size);
}
…
envp = (ж<uint16>)(uintptr)(@unsafe.Add(end, size));
```

A `uintptr` → `ж<uint16>` round-trip cannot reconstitute a managed box from a raw address; the CLR raises
`COR_E_EXECUTIONENGINE` and the process dies outright — no panic, no recovery, no partial results.

**This is the third fork of the Windows-syscall wall CLAUDE.md already names** — not the non-blittable-layout
fork and not the `**T` OUT-parameter fork, but the one where *the caller reinterprets kernel memory*, so no
wrapper is at fault and no mirror-the-wrapper remedy applies. It is the same shape as `net.adapterAddresses`,
which was closed on 2026-08-17 by hand-owning the walk
(`core/net/windows/interface_windows_impl.cs`, guarded by the `IpAdapterAddresses` behavioral test). **The
remedy is a proven, already-executed pattern, applied to a new site.**

### The 27 scattered empties — deadline machinery, not the crash

All 27 belong to 8 top-level tests, and every one is a deadline/close test:
`TestAcceptTimeout` (3), `TestAcceptTimeoutMustNotReturn`, `TestAcceptTimeoutMustReturn`, `TestCloseRead` (4),
`TestCloseUnblocksRead`, `TestCloseWrite` (4), `TestConnClose` (4), `TestDialTimeout` (9). Each test runs on its
own thread in the host, so one that never terminates yields no verdict without blocking the others. This is the
residue of the netpoll-deadline seam — the only part of the old "poller arc" framing that survives.

### The 13 real divergences, bucketed by mechanism

| mechanism | names | evidence |
|---|---:|---|
| **`array<T>` index panic from a mis-derived length** | 3 | `TestIPv6WriteMsgUDPAddrPortTargetAddrIPVersion`: `index out of range [0] with length **-791150592**` in `internal/poll/windows/fd_windows.cs:1303 sockaddrInet6ToRaw`; `TestAllocs` `length 0`; `TestConcurrentSetDeadline` `index [10] with length 10` |
| **vectored write not wired** | 9 | `TestBuffers_WriteTo` + 8 subtests: `write calls = 0; want 1` (`writev_test.go:91`) |
| **socket-option / environmental** | 1 | `TestIPv4MulticastListener`: `setsockopt: The requested address is not valid in its context` |

The **negative** array length is the tell: it is the same `ж`/`uintptr` reinterpretation family as the
`Environ` crash, surfacing as corrupt data instead of a fatal error. Both are ж-box arc territory.

### Priced read
- **Already banked-quality today: 120 verdicts.**
- **One hand-own (`syscall.Environ`, following the `adapterAddresses` precedent) releases the 314-name tail.**
  That tail is not exotic: it contains `TestParseIP`, `TestParseCIDR`, `TestParseMAC`, `TestJoinHostPort`,
  `TestSplitHostPort`, `TestMarshalEmptyIP`, `TestNetworkNumberAndMask`, the whole `TestPipe` family (12),
  `TestTCPServer` (25) and `TestUDPServer` (33) — pure-compute and loopback-socket tests that Go passes and the
  converted stack demonstrably already supports.
- **Realistic reach after that single fix: on the order of 400+/474 (~85%)**, leaving the 27 deadline hangs and
  the 13 divergences.
- **Arcs the residuals wait on:** ж-box (the negative-length/`Environ` family), netpoll deadlines (the 27), and
  a `writev` capability decision (the 9).
- **Correction to the record:** the throughput framing (`BOARD:10528-10545`) and the socket-wall exclusion
  (`BOARD:10579-10581`) are both **superseded** for `net`. It is neither socket-walled nor throughput-walled.

---

## 4. `net/http` — the highest verdict-per-fix ratio in the entire census

### Prior state
Recorded once, Go-side only: `| net/http | 245 | CS1002 '; expected' |` (`BOARD:4681`), never re-measured,
excluded from the pure-compute harvest as part of "the socket-walled `net` family … behind the poller-throughput
arc" (`BOARD:10579-10581`). Fleet note: "Deliberately still deferred … the four hardest, largest packages"
(`MAILBOX.md:14778-14780`).

### Measured
Pipeline runs in **209 s**. `status: conversion-blocked`.

- **Go denominator is 1,352 verdicts, not 245** (1,333 pass, 19 skip, **0 fail**). The recorded figure was
  stale by 5.5×. Go's own `net/http` suite is *fully green* on this host — including HTTP/2.
- 35 converted `*_test.cs` files were emitted.
- The build reported **28 diagnostics total, all parse-level, in exactly 2 files**:
  `client_test.cs` (1 site) and `serve_test.cs` (6 sites) — 7 clusters of
  `CS1003` + `CS1026` + `CS1002` + `CS1513`. **No semantic (CS0xxx) diagnostic anywhere in the other 33 files.**

### Root cause — one converter defect, seven instances

The closure capture-copy prologue (`var <name>ʗ1 = <name>;`) is emitted **inside an expression argument list**
instead of before the enclosing statement, whenever a capturing func literal is wrapped in a named-func-type
conversion and passed to a `go` statement or a channel send.

`serve_test.cs:2539` — a `go` statement (6 of the 7 sites are this shape):

```csharp
goǃ((ᴛ1, ᴛ2) => Serve(ᴛ1, ᴛ2), new …oneConnListenerжListener(ls),
      new …http_HandlerFuncᴠΔHandler(new Δhttp.HandlerFunc(
    var connʗ1 = conn;                       // ← statement in expression position
    (Δhttp.ResponseWriter rw, ж<Δhttp.Request> req) => { … })));
```

`client_test.cs:2380` — the same defect through a **channel send** (`ᐸꟷ`), confirming it is not `go`-specific:

```csharp
handlerc.ᐸꟷ(
    var tsʗ1 = ts;                           // ← same shape
    var ttʗ1 = tt;
    (Δhttp.ResponseWriter w, ж<Δhttp.Request> r) => { … });
```

All 7 sites share the structure **capturing func literal → wrapped in a named-func-type conversion → passed as
an argument to `go` / channel-send**. The statement-level hoist that normally lifts the copies does not
traverse the conversion boundary. `CS1002 '; expected'` — the diagnostic the board recorded — is the third
diagnostic of each cluster, i.e. **the recorded wall is this same defect, seen from its least informative end.**

### Priced read
- **1,352 verdicts sit behind one converter defect at seven call sites in two files.** No other giant comes
  close on ratio.
- **Behind it: unmeasured**, and this must be said plainly. The absence of semantic diagnostics across the other
  33 test files is *suggestive* — Roslyn binds and reports semantic errors alongside syntax errors — but parse
  failure does suppress some downstream analysis, so it is not proof.
- **Confidence the residual is shallow: the strongest of the four, by a distance.** Five `net/http` subpackages
  are already banked — `httptest` 55, `httputil` 53, `internal` 14 (+1 disclosed), `internal/ascii` 13,
  `fcgi` 12 — and `ValidatedTestPackages.md:280` calls `httptest` "the broadest running proof the converted
  `net/http` has": the server under it is real. HTTP/2 negotiated over TLS loopback on 2026-08-25.
- **⚠ Coupling to §3.** `net/http`'s tests shell out via `testenv.Command` in three files
  (`fs_test.go` 2, `http_test.go` 3, `serve_test.go` 2). Every one of those will hit the **same
  `syscall.Environ` CLR kill** documented above and take the host down mid-suite. **Fixing `Environ` is a
  prerequisite for a clean `net/http` census, not merely a `net` improvement** — one hand-own serves two giants.
- **Correction to the record:** the "socket-walled" exclusion category no longer holds for `net/http` any more
  than it does for its siblings (`net/rpc/jsonrpc`, `net/http/pprof`, `net/smtp`, `net/http/cgi` all disproved
  it individually). `net/http` is **syntax-blocked**, and was never measured past its first diagnostic.

---

## 5. `runtime` — the recorded wall reproduces, but its recorded MECHANISM is wrong

### Prior state
`| runtime | 870 | build-blocked |` (`BOARD:4675`). Mechanism recorded at `BOARD:6837-6842`: a `-tests` run
"rewrites `src/core/runtime/runtime.csproj`'s windows-conditional `ItemGroup` to add `internal/syscall/windows`
— which references `syscall`, which references `runtime`", concluded (`:6856-6858`) to be "a converter defect —
a **test-closure-only reference** reaching a production `.csproj`."

### Measured
**Conversion SUCCEEDS.** All 110 test files convert; the run reaches
`dotnet run --project runtime.tests.csproj` and dies at *restore*:

```
NuGet.targets(1298,5): error MSB4006: There is a circular dependency in the target dependency graph
involving target "_GenerateRestoreProjectPathWalk".
[…\src\core\internal\syscall\windows\internal.syscall.windows.csproj]
```

Denominator: **883 Go verdicts** (846 pass, 37 skip, 0 fail) — 870 was the Go 1.23.1 figure.

`runtime.csproj` was confirmed mutated by the run (SHA-256 before/after), the added edge being exactly:

```xml
-  <ItemGroup Condition="'$(GoTargetOS)'=='windows'" />
+  <ItemGroup Condition="'$(GoTargetOS)'=='windows'">
+    <ProjectReference Include="$(go2csPath)core/internal/syscall/windows/internal.syscall.windows.csproj" />
+  </ItemGroup>
```

The cycle is **shorter than recorded**: `internal/syscall/windows` references `runtime/runtime.csproj`
**directly**, so this is a 2-node cycle, not the 3-node path through `syscall`.

### The recorded mechanism is wrong — probe 1

`runtime.tests.csproj` **already carries** `internal/syscall/windows` in its own reference list, correctly, as
part of the test closure. So the production edge is redundant *as well as* cycle-creating — which makes
"test-closure reference leaked into production" the obvious reading. It is not the right one.

Reverting **only** `runtime.csproj` to master and rebuilding the already-converted artifacts
(`-test-action build`, no reconversion): restore now **succeeds**, and the build fails with just **2 unique
diagnostics**:

```
runtime\windows\os_windows.cs(443,47):  error CS0234: The type or namespace name 'syscall'
                                        does not exist in the namespace 'go.@internal'
runtime\windows\os_windows.cs(443,108): error CS0234: (same)
```

### Actual root cause — closure-dependent `//go:linkname` lowering

`os_windows.cs:443` was **also** rewritten by the `-tests` run, +1/−1 line:

```diff
  //go:linkname canUseLongPaths internal/syscall/windows.CanUseLongPaths
- internal static bool canUseLongPaths;
+ internal static bool canUseLongPaths { get => go.@internal.syscall.windows_package.CanUseLongPaths;
+                                        set => go.@internal.syscall.windows_package.CanUseLongPaths = value; }
```

Go's `//go:linkname` is a **link-time symbol alias and creates no import edge** — which is precisely why the Go
runtime may name a symbol in a package that itself imports `runtime`. The converter lowers it to a
**compile-time property reference**, which in C#/MSBuild is an *assembly* reference, and assembly references
cannot be circular.

Critically, **the lowering is closure-dependent**: in an `-stdlib` run the topological order converts `runtime`
before `internal/syscall/windows`, the target is unresolvable, and the converter emits a plain field (what
master ships). In a `-tests` run the test closure pulls `internal/syscall/windows` in, the target resolves, and
the forwarding property is emitted. So the causal chain is:

1. `-tests` closure includes `internal/syscall/windows`
2. → the `//go:linkname` resolves and is lowered to a compile-time property (production source drift)
3. → production `runtime.csproj` must gain the reference to compile
4. → `internal/syscall/windows` references `runtime` → **cycle** → MSB4006 at restore

Steps 2 and 3 are **coupled**: removing the csproj edge alone leaves the property and yields CS0234. This is
the same family as the documented `-tests`-closure production drift (`Δio` alias, `global::go` escape, init
hook) — but unlike those, it is **load-bearing: it changes the project graph rather than the text.**

### Probe 2 — what is behind the cycle

Restoring `os_windows.cs` to master content as well (the plain field) and rebuilding: restore and the
production compile both pass, and the failure moves into the **converted test sources** — **404 diagnostics,
398 of them in one file**:

| file | diagnostics | first site |
|---|---:|---|
| `export_test.cs` | 398 | `(288,20)` |
| `arena_test.cs` | 6 | `(60,138)` |

Two further single-site converter defects, both distinct from anything above:

**(a) `arena_test.cs:60` — a nested block comment in emitted C#.** The `[GoType]` trailing comment renders a Go
type expression that itself contains an `unsafe.Sizeof` sub-expression rendered as its own `/* … */`:

```csharp
[GoType("[524289]ж<smallPointer>")] /* [ …UserArenaChunkBytes / /* unsafe.Sizeof(&smallPointer{}) */ (uintptr)8 + 1]ж<smallPointer> */
```

C# block comments do not nest, so the inner `*/` closes the outer comment early and the remainder parses as
code (`CS8124`, `CS1519`).

**(b) `export_test.cs:288` — a Go anonymous interface type emitted verbatim into a C# generic argument.**
From `var IfaceHash func(i interface{ F() }, seed uintptr) uintptr`:

```csharp
public static Func<interface{F()}, uintptr, uintptr> IfaceHash;
```

The anonymous-struct lift machinery has no anonymous-**interface** counterpart in this position. The 170
`CS0106` are cascade: once the parse derails at line 288 the rest of the file is read at the wrong nesting
level. `export_test.go` is the shim virtually every runtime test imports, so it must compile before anything
runs.

### Priced read
- **Three stacked, individually small walls**, in order: linkname→cycle (1 site, but a genuine design question),
  nested block comment (1 site), anonymous-interface lift (1 site).
- **The linkname one is not a one-liner.** The honest remedy is directional: the *push* direction has no cycle,
  since `internal/syscall/windows` already references `runtime` — so the write-through belongs on that side, or
  the lowering must not create an assembly edge at all. Emitting the plain field unconditionally would restore
  master's behaviour but silently break the linkname's semantics.
- **Behind all three: unmeasured, and this is the giant where the residual is most likely to be deep.** The
  board already records the adjacent evidence: `runtime/pprof` 0 of 174 (`CS0149` + `ᏑᏑsalts`), `runtime/trace`
  0 of 2 on the `getg` stub, `runtime/race` and `net/internal/socktest` E1 not-applicable. Runtime's own suite
  probes GC internals, stack traces, the scheduler and user arenas — the intrinsic classes the corpus stubs by
  design under the S1/CS0030 fork ruling.
- **Only 3 runtime subpackages are banked**, all tiny: `runtime/debug` 4 (+5), `runtime/internal/math` 1,
  `runtime/internal/sys` 4. There is no `httptest`-grade proxy proof anywhere for runtime.

---

## 6. Cross-cutting findings

**F1 — One defect family spans two giants and is already-proven to fix.** `syscall.Environ()`'s
`uintptr → ж<uint16>` round-trip over kernel memory kills the CLR outright. It caps `net` (314 names) and will
cap `net/http` (which shells out in 3 test files). The remedy is the pattern already executed for
`net.adapterAddresses`: hand-own the walk, transcribe into managed storage, guard with a behavioral test. The
corrupt **negative** array length in `internal/poll`'s `sockaddrInet6ToRaw` is the same family surfacing as bad
data rather than a fatal error.

**F2 — A converter panic is advisory on the production side and fatal on the test side.** `visit file error:
… not an Int` is a warning with exit 0 under `-stdlib`, and a hard package abort under `-tests`. Any defect of
this shape is therefore invisible to the corpus gates and total to the pipeline. Worth a board line
independent of reflect.

**F3 — Emission is closure-dependent in a load-bearing way, not only a cosmetic one.** The documented
`-tests`-closure drift classes (`Δio` alias, `global::go` escape, using reorder, init hook) are all textual. The
`//go:linkname` lowering changes the **project reference graph**. The standing "restore it" instruction still
applies, but the class needs the stronger warning: a closure difference can make a package unbuildable rather
than merely dirty.

**F4 — Go-side denominators drifted badly while these packages sat unmeasured.** `net/http` 245 → **1,352**
(5.5×), `runtime` 870 → **883**, `reflect` never recorded → **396**. Any prioritisation done against the old
figures under-valued `net/http` by more than five times.

**F5 — Not one of the four is blocked by a capability wall at its first diagnostic.** All four first walls are
converter defects. The capability walls are real but sit *behind* them, and only `runtime` is likely to meet
one immediately.

---

## 7. Ranked recommendation — which giant to staff first

**1. `net/http` — staff first.** 1,352 verdicts (the largest single unbanked block anywhere in the corpus)
behind **one** converter defect at seven sites in two files, with the strongest proxy evidence of the four that
the code underneath works (five banked subpackages; `httptest` explicitly recorded as the broadest running proof
`net/http` has; HTTP/2 over TLS loopback working). The defect — capture-prologue emitted in expression position
under a named-func-type conversion — is a general converter correctness fix, not a net/http special case, so it
pays out across the corpus.
**Prerequisite: land F1 first**, or the census will die mid-suite on `testenv.Command`.

**2. `net` — staff second, and it banks something immediately.** Already at **120 matching verdicts** with no
work at all. One hand-own of `syscall.Environ`, on an already-executed pattern, releases a 314-name tail that is
mostly pure-compute and loopback tests Go passes. Plausible reach ~85% of 474. It also de-risks #1, which is
why F1 should be the very first piece of work in either lane.

**3. `reflect` — cheapest fix in the census; do it regardless of staffing order.** Three call sites gaining
`constant.ToInt` is a smaller change than anything else here, it is a *general* converter correctness fix (any
package with a float-kind array length is affected — reflect is merely where it was caught), and it converts an
entirely unmeasured 396-verdict package into a measurable one. Its depth is unknown, and its residuals likely
wait on the B2 kind-split, so it is a poor *campaign* to commit to — but an excellent side-quest to land while
#1 or #2 is running.

**4. `runtime` — do not staff yet.** Three stacked walls, one of which (the linkname lowering) is a design
question rather than a fix, and the only giant whose residual is likely to be deep: no substantial banked
proxy, and the adjacent record (`runtime/pprof` 0/174, `runtime/trace` 0/2 on `getg`) points straight at the
intrinsic classes the project stubs by design. Its 883 verdicts are real, but they are the most expensive
verdicts on this board. **Its two cheap defects — the nested block comment and the anonymous-interface lift —
should still be fixed opportunistically**, since both are single-site and neither is runtime-specific.

**Suggested sequence:** F1 (`syscall.Environ` hand-own) → `net/http` capture-prologue fix → `net/http` census
→ `net` re-census and bank → reflect `constant.ToInt` as a parallel side-quest.

---

## 8. Reproduction

```powershell
$env:DOTNET_ROOT='C:\Users\<user>\dotnet10'
$env:PATH="C:\Users\<user>\dotnet10;C:\Users\<user>\sdk\go1.23.12\bin;$env:PATH"
$env:GOROOT='C:\Users\<user>\sdk\go1.23.12'      # backslash spelling is load-bearing
$env:MSBUILDDISABLENODEREUSE='1'

# per giant (run SEQUENTIALLY; detach so a turn boundary cannot reap the child)
go2cs -tests -test-action all -test-timeout 20m -go2cspath <worktree>\src `
      C:\Users\<user>\sdk\go1.23.12\src\<pkg>  <worktree>\src\core\<pkg>

# counts derived from, per package:
#   <worktree>\src\core\<pkg>\go2cs_test_comparison.json   (keys: go, csharp)
```

Runtime probes 1 and 2 are `-test-action build` against the already-converted artifacts, after
`git checkout HEAD -- src/core/runtime/runtime.csproj` and then additionally
`git checkout HEAD -- src/core/runtime/windows/os_windows.cs`.

**Post-run hygiene applied here:** all 57 modified tracked files were classified against the documented
`-tests`-closure drift classes (`global::go` root escape, `initᴛᴛtests` hook +7/−0, position-map re-emission,
4 CRLF phantoms) plus the two runtime items reported in §5, then restored; untracked pipeline artifacts were
cleaned; `git status --porcelain` verified empty before this file was added.
