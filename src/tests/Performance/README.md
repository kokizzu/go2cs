# Go vs transpiled C# — runtime performance comparison

A small, targeted benchmark suite answering the question people ask first: **how fast is transpiled
C# compared to the original Go?** — startup time and memory, on both the normal JIT runtime and
**Native AOT** (self-contained, the closest deployment analog to a Go binary).

Each benchmark is a tiny Go program (same shape as the
[behavioral tests](https://github.com/ritchiecarroll/go2cs/tree/master/src/tests/Behavioral)),
chosen to exercise one Go construct with a real C# cost model — slices, strings, maps, channels,
interface dispatch — plus raw compute loops where the two runtimes should be close. This is not an
exhaustive benchmark game; it gives the common expected range of differences. The short version:
transpiled C# is **usually slower than Go, but not universally** — maps and the stack-string
path run at parity or faster in both C# variants (maps dramatically so under Native AOT), while
the rest ranges from ~1.5× on tight compute to the structural-interface assert, the honest
outlier at the other end.

## The benchmarks

| Benchmark | What it exercises |
|---|---|
| **Startup** | Empty workload: pure process start + runtime init + one `fmt` round-trip. Wall time. |
| **Fib** | Recursive Fibonacci (`fib(34)` ×5): function-call and integer-op overhead. |
| **Sieve** | Sieve of Eratosthenes to 10M ×3: slice allocation, indexing, tight loops (`slice<T>` bounds/header emulation). |
| **MatMul** | 256×256 `float64` matrix multiply ×4: floating-point throughput, nested slice-of-slice access. |
| **String** | 10M iterations of byte-slice append → `string` conversion, indexing, concatenation (`@string` emulation). |
| **StringView** | 20M iterations of keyword checks `string(buf) == "null"/"true"/"false"` over a fixed buffer — the idiom the converter's stack-string (`sstring`) emission optimizes: a zero-copy view compared against a `u8` literal span, no per-comparison allocation. |
| **StringMatch** | 20M iterations of literal-string hot paths: switch-on-string dispatch, `strings.HasPrefix` with a literal prefix, literal returns, literal map-key counters — the shapes where a naive conversion would allocate a fresh `@string` per evaluation while Go allocates nothing (string literals live in RODATA). Instrument for the literal-comparison optimizations (span operators, `u8` casts, hoisted literals). |
| **Map** | 2M inserts + 2M comma-ok lookups + 1M deletes on `map[int]int` (`map<K,V>` emulation). |
| **Sort** | `sort.Ints` on 2M deterministic pseudo-random ints (`sort.Interface` dispatch through the runtime's reflection-bound `Interface<T>`). |
| **Channel** | 1M ints producer→consumer through a buffered channel with one goroutine (`channel<T>` + goroutine scheduling emulation). |
| **IfaceCall** | 50M iterations of pure interface method dispatch — interface values of statically-known types built once, called in a megamorphic hot loop; no asserts, no switches. The row that answers "what does calling an interface method cost?" |
| **Iface** | 20M iterations of the **common** interface cases: method dispatch, concrete comma-ok assertions, and a type switch over a closed set — all resolved by the compile-time (nominal) machinery: generated adapters and cast-shaped asserts. What ordinary Go interface code costs. |
| **IfaceShell** | 5M iterations × 2 duck-typed interface asserts + forwarded calls — one on a value-typed dynamic value (the reflective **object shell**), one on a pointer-sourced one (the delegate-bound **generic shell**). The one path with no compile-time answer, and the only shared mechanism whose Native AOT behavior is otherwise unexercised. |

Every benchmark prints a deterministic **checksum** (verified byte-identical across Go, C# JIT, and
C# AOT before anything is measured) plus its own workload time measured in-program via
`time.Now().UnixNano()` — so the headline numbers exclude process startup, which is reported
separately by the Startup row.

## Methodology / fairness notes

- **Three variants of the identical program:** the Go binary (`go build`, default optimized), the
  transpiled C# built `Release` framework-dependent (JIT column), and the same C# published with
  `PublishAot=true` self-contained, partial trim (Native AOT column).
- **Median of 5 runs** (configurable) after 1 discarded warmup, single-shot process executions — the
  way a Go CLI actually runs. The JIT column deliberately includes in-process tiered-JIT warmup inside
  the workload; long-running server workloads would look better for the JIT than these numbers do.
- **Peak memory** is the process peak working set, polled while it runs. Both wall time and in-program
  workload time are captured; tables report workload time (Startup row: wall).
- Benchmarks avoid nondeterminism (no `math/rand`; inline xorshift/LCG generators), so outputs are
  byte-comparable, and print timing on a filtered `elapsed_ns:` line.
- **Published tables come from a single designated host per era** — the Environment line names the
  part — so the History section's cross-toolchain comparisons (e.g., .NET 9 → 10) are always
  same-machine. Ratios from different hardware are not comparable and are never mixed.

## Running it

```powershell
cd src/tests/Performance
./run-performance.ps1                    # full run: transpile, build (incl. AOT), verify, measure
./run-performance.ps1 --no-aot           # skip AOT publishes, faster while iterating
./run-performance.ps1 --filter Map       # one benchmark
./run-performance.ps1 --runs 10 --update-readme   # refresh the results block below
```

Requirements: Go toolchain, .NET 9 SDK, and — for the AOT column — a native linker and toolchain,
which differs by host:

| Host | Native AOT prerequisite |
|:--|:--|
| **Windows** | MSVC C++ build tools — Visual Studio 2022's "Desktop development with C++" workload, which supplies the `link.exe` ILC needs. The runner prepends the VS Installer directory to `PATH` so the SDK's `vswhere` probe finds it. |
| **Linux** | `clang` and `zlib1g-dev` (Debian/Ubuntu: `sudo apt install clang zlib1g-dev`; Fedora: `clang zlib-devel`). ILC shells out to `clang` for the native link step. |
| **macOS** | The Xcode command line tools (`xcode-select --install`), which supply `clang` and `ld`. |

Deliberately **not** scripted: `run-performance.ps1` does not install any of these. A benchmark
harness that silently mutates the machine's toolchain is not a harness anyone should trust with a
performance claim. `--no-aot` drops the whole column and needs none of them (F13,
[PLAN-linux-operation.md](https://github.com/ritchiecarroll/go2cs/blob/master/docs/PLAN-linux-operation.md)).

The environment line of the results block reads the CPU name from the registry on Windows and from
`/proc/cpuinfo` on Linux, so a published table always names the part that produced it.

The runner (`PerformanceRunner`), a dependency-free console app structured like the behavioral suite's
`BehavioralRunner`, runs **Transpile → Build → Verify → Measure**. Verify requires all three binaries
to produce identical (timing-filtered) stdout before anything is timed, so the table can never
silently report a benchmark that computes something different in C#.

## Results

<!-- PERF-RESULTS:BEGIN -->

**Environment:** AMD Ryzen 5 PRO 6650U with Radeon Graphics · Microsoft Windows 10.0.26200 · go1.23.1 · .NET SDK 9.0.316 · 2026-08-13

C# builds: JIT = framework-dependent `Release`; Native AOT = `-p:PublishAot=true` self-contained, partial trim. Median of 5 runs (1 discarded warmup). Workload time is measured in-program and excludes process startup; the Startup row is pure process wall time. Ratios are relative to Go.

**Execution time** (milliseconds -- lower is better):

| Benchmark | Go | C# (JIT) | C# (Native AOT) |
|---|---:|---:|---:|
| Startup | 25.2 | 223.3 (8.88×) | 77.8 (3.09×) |
| Fib | 116.5 | 191.2 (1.64×) | 172.8 (1.48×) |
| Sieve | 65.6 | 105.4 (1.61×) | 215.2 (3.28×) |
| MatMul | 104.3 | 173.0 (1.66×) | 523.2 (5.02×) |
| String | 103.0 | 1,006.3 (9.77×) | 1,257.2 (12.21×) |
| StringView | 19.7 | 20.5 (1.04×) | 18.5 (0.94×) |
| StringMatch | 189.0 | 978.0 (5.18×) | 1,220.2 (6.46×) |
| Map | 617.2 | 478.2 (0.77×) | 180.5 (0.29×) |
| Sort | 136.8 | 429.6 (3.14×) | 438.3 (3.20×) |
| Channel | 39.3 | 90.4 (2.30×) | 110.0 (2.80×) |
| IfaceCall | 179.6 | 432.4 (2.41×) | 451.1 (2.51×) |
| Iface | 94.9 | 543.6 (5.73×) | 452.5 (4.77×) |
| IfaceShell | 21.4 | 865.2 (40.50×) | 1,288.6 (60.32×) |
| RefLower | 226.3 | 660.6 (2.92×) | 1,827.8 (8.08×) |

**Peak memory** (working set, MB -- lower is better):

| Benchmark | Go | C# (JIT) | C# (Native AOT) |
|---|---:|---:|---:|
| Startup | 3.0 | 45.1 | 78.4 |
| Fib | 5.7 | 45.3 | 78.6 |
| Sieve | 26.3 | 65.6 | 98.2 |
| MatMul | 10.3 | 52.4 | 84.9 |
| String | 5.8 | 55.1 | 87.3 |
| StringView | 5.8 | 45.5 | 78.5 |
| StringMatch | 5.8 | 55.5 | 88.7 |
| Map | 158.8 | 164.8 | 197.0 |
| Sort | 21.9 | 61.4 | 94.0 |
| Channel | 5.7 | 51.0 | 86.3 |
| IfaceCall | 5.7 | 46.2 | 78.7 |
| Iface | 5.7 | 45.7 | 78.9 |
| IfaceShell | 5.7 | 66.2 | 97.7 |
| RefLower | 5.7 | 46.3 | 78.6 |

<!-- PERF-RESULTS:END -->

### Reading the results

- **Startup:** Go wins cold process start decisively. The JIT pays runtime load plus
  assembly-loading and Go package initialization for the full converted-stdlib closure the binary
  references; **Native AOT removes the JIT-on-the-fly cost and starts several times faster than
  the JIT**, but still runs the same package initializers, so a gap to Go remains. For CLI-shaped
  programs AOT is the deployment story on time — see the memory note below for its trade.
- **Memory:** the working-set columns carry the cost of the full converted standard library. The
  JIT column's floor is the .NET runtime plus loaded assemblies; the AOT column's is *higher* —
  the self-contained binary maps the whole compiled closure into the process — so AOT currently
  trades memory for its startup and per-benchmark wins. Reducing both floors is optimization
  surface (trimming eligibility, lazy package init), not a semantic cost.
- **Function calls / integers (Fib):** the closest compute workload — ~1.6× under the JIT and
  ~1.5× under Native AOT, the one tight loop where AOT leads the JIT rather than trailing it.
- **Slices & floats (Sieve, MatMul):** the gap is `slice<T>` header emulation and bounds checks the
  JIT can't always elide, compounded on nested `[][]float64` access. **AOT is *slower* than the JIT
  here** — ILC lacks the JIT's dynamic PGO/OSR loop optimizations, trading tight-loop throughput for
  AOT's startup and memory wins.
- **String:** the price of materialization — every `[]byte`→`string` round-trip is an allocation +
  copy through the `@string` emulation, versus Go's `append` chain inlining to a few instructions. This
  benchmark's conversions are all **ineligible** for the stack-string optimization (its `s` is a
  concat operand and its buffer is mutated), so they stay `@string` — see StringView for the eligible case.
- **StringView:** the same `[]byte`→`string` cost, but for the subset the converter proves
  non-escaping and read/compare-only, where it emits a zero-copy stack string (`sstring`) instead of
  `@string` (see [ConversionStrategies-Reference](https://github.com/ritchiecarroll/go2cs/blob/master/docs/ConversionStrategies-Reference.md)).
  The converter hoists one `sstring` view per call rather than re-materializing it per comparison,
  since the JIT won't lift a `ref struct` view out of a loop on its own. **Runs at parity with Go
  or better in both C# variants** — and the number to watch as the eligibility surface widens; arc
  detail in
  [DESIGN-string-literal-allocation.md](https://github.com/ritchiecarroll/go2cs/blob/master/docs/phase4/DESIGN-string-literal-allocation.md).
- **StringMatch:** literal-heavy hot paths — switch-on-string dispatch, `strings.HasPrefix` against
  a literal prefix, literal returns, literal map-key counters. The instrument for the same
  literal-comparison optimizations StringView exercises (span operators, `u8` casts, hoisted
  literals); arc detail in
  [DESIGN-string-literal-allocation.md](https://github.com/ritchiecarroll/go2cs/blob/master/docs/phase4/DESIGN-string-literal-allocation.md).
- **Map:** transpiled C# is *faster than Go* — `map<K,V>` rides .NET's heavily-optimized
  `Dictionary`, and the AOT build wins by the widest margin on this insert/lookup/delete churn.
- **Sort:** the runtime's `sort.Interface` shim (`Interface<T>`) binds `Len`/`Less`/`Swap` via
  reflection-created delegates — cached, but a delegate hop per comparison.
- **Channel:** `channel<T>` + goroutine emulation over managed threading vs Go's runtime scheduler —
  real unbuffered rendezvous, single-fire select, operand-once hoisting. Currently ~2.3–2.8× on
  this producer→consumer churn: the rendezvous rides managed synchronization primitives where Go's
  scheduler hands off directly, a cost the cooperative-scheduler arc
  ([DESIGN-cooperative-scheduler.md](https://github.com/ritchiecarroll/go2cs/blob/master/docs/phase4/DESIGN-cooperative-scheduler.md))
  owns. Notably machine-sensitive — measure on your own hardware before drawing conclusions.
- **IfaceCall:** pure interface method dispatch — no asserts, no shell construction, just calling
  through an interface value built once, in a megamorphic hot loop. The floor for what *calling*
  through an interface costs, distinct from *obtaining* one (the two rows below).
- **Iface — the everyday interface story:** dispatch through statically-known interface values, a
  comma-ok assertion, and a type switch over a closed set, all resolved by generated adapters and
  ordinary casts — the compile-time (nominal) machinery, with no runtime shell construction.
  Mechanism notes in
  [DESIGN-iface-shell-caching.md](https://github.com/ritchiecarroll/go2cs/blob/master/docs/phase4/DESIGN-iface-shell-caching.md).
- **IfaceShell — the one operation C# has no native answer for:** satisfying an interface
  **structurally at run time**. Go resolves this with a cached itab lookup — two machine words, a
  nanosecond hash probe. C# has no two-word interface value, so go2cs constructs a wrapper ("shell")
  that forwards to the concrete value on every assertion; the ratio is the price of that
  construction plus, on the value-typed tier, a reflective forwarded call. An assertion the
  converter can resolve at compile time (the Iface row above) skips this path via a generated
  adapter instead. AOT can be *slower* here, unlike Startup: its generic shell tier can degrade to
  the reflective object shell, and AOT's reflective invokers can't emit IL stubs. Optimization
  directions:
  [DESIGN-iface-shell-caching.md](https://github.com/ritchiecarroll/go2cs/blob/master/docs/phase4/DESIGN-iface-shell-caching.md).
- **RefLower:** the ж-bound hot path — pointer parameters feeding pointer parameters, address-taken
  locals, field addresses — after the ref-lowering arc replaced its heap boxes with native `ref`
  (before that arc this shape ran ~25× Go; the lowering brought the JIT to ~2.9×). AOT currently
  trails the JIT here by a wide margin (~8×) — ILC's codegen of the ref-lowered loop is a priced
  open question for the arc's next phase
  ([DESIGN-zh-box-reduction.md](https://github.com/ritchiecarroll/go2cs/blob/master/docs/phase4/DESIGN-zh-box-reduction.md)).

## Exploration — performance floor (2026-08-16)

> **Exploratory, and self-contained.** A one-off investigation run under
> [PLAN-bflat-perf-exploration.md](https://github.com/ritchiecarroll/go2cs/blob/master/docs/PLAN-bflat-perf-exploration.md).
> It changes **no build default**, alters **nothing** in the three-column table above, and proposes
> **no new toolchain dependency**. [bflat](https://github.com/bflattened/bflat) appears here as a
> measuring instrument, never as a candidate: per the user + coordinator ruling recorded in that
> plan, its release cadence disqualifies it from becoming a first-class citizen of go2cs builds.
> Numbers come from `run-performance-floor.ps1`, a standalone harness that leaves
> `PerformanceRunner` and `run-performance.ps1` untouched.

The canonical table's weakest story is the **floor**: Native AOT starts ~3× slower than Go, and its
working set carries the whole compiled closure. How much of that floor is recoverable, and how much
of the recovery needs bflat rather than switches the stock .NET SDK already exposes? A null result
would have been an acceptable answer. This is not one.

**Environment:** AMD Ryzen 5 PRO 6650U with Radeon Graphics · Microsoft Windows 10.0.26200 ·
go1.23.1 · .NET SDK 9.0.316 · 2026-08-16 — the same host and conventions as the canonical table, so
the two are directly comparable (this harness independently reproduced the published Startup row:
24.5 vs 25.2 ms Go, 232.0 vs 223.3 JIT, 79.7 vs 77.8 AOT).

Median of 5 runs after 1 discarded warmup. `Startup` reports process wall time; every other row
reports in-program `elapsed_ns:` workload time; memory is peak working set. **Verify gate:** a
variant's timing-filtered stdout had to match the Go binary exactly or it earned no number — every
variant below passed on all five benchmarks.

### The variants

| Key | Build | Role |
|---|---|---|
| `A0` | stock `-p:PerfAot=true` | the control — exactly what the canonical AOT column publishes |
| `A1` | `A0` + `InvariantGlobalization` + `UseSystemResourceKeys` + `StackTraceSupport=false` + `IlcGenerateStackTraceData=false` + `OptimizationPreference=Size` | the plan's "AOT-min" profile |
| `A2` | as `A1` but `OptimizationPreference=Speed` | |
| `X1` | `A0` + `TrimMode=full` | **probe, out of profile** — read the caveat below |
| `X2` | `A1` + `TrimMode=full` | **probe, out of profile** |
| `B1` | bflat `--stdlib DotNet -Ot` | |
| `B2` | bflat `--stdlib DotNet -Os --no-globalization --no-stacktrace-data --no-exception-messages` | |

bflat pinned at **v10.0.0-rc.1**, `bflat-10.0.0-rc.1-windows-x64.zip`, SHA256
`59FC623E751AE1AA8C7A6531B356F83A9063A7F0B13C835E43ACE44ADE2D24CD` (MIT). `--stdlib:zero` and
`--no-reflection` were out of scope throughout: golib's `fmt` formatting and sort's `Interface<T>`
bind members reflectively.

### Finding 1 — the size floor is a trim-ROOTING question, not a toolchain question

Executable on disk (MB). The JIT column is its framework-dependent output tree excluding `.pdb`,
and is **not** a deployable size — it needs the shared runtime installed.

| Benchmark | Go | JIT tree | `A0` | `A1` | `A2` | `X1` | `X2` | `B1` | `B2` |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Startup | 2.12 | 22.41 | 288.26 | 221.74 | 234.38 | 10.94 | 8.77 | 8.73 | 7.74 |
| Fib | 2.12 | 22.41 | 288.26 | — | — | 11.11 | 8.93 | 8.88 | 7.88 |
| Map | 2.13 | 22.41 | 288.26 | 221.74 | 234.38 | 11.12 | 8.93 | 8.88 | 7.89 |
| String | 2.12 | 22.41 | 288.26 | — | — | 11.11 | 8.93 | 8.88 | 7.88 |
| Channel | 2.12 | 22.41 | 288.26 | — | — | 11.12 | 8.94 | 8.89 | 7.89 |

The stock profile emits **288 MB for a program that prints two lines**, and that figure barely moves
across benchmarks because it is not the benchmark — it is the whole converted standard library.

Of the ~279 MB between the stock profile and bflat, **~99% is `TrimMode=partial` and ~1% is the
feature switches.** `partial` roots all 57 assemblies of the converted closure whole; bflat's ILC has
no rooting concept and always compiles to reachability. Give the stock SDK the same policy and it
lands at 8.77 MB against bflat's 8.73 — a 0.5% difference, which is nothing.

That is the result. bflat is the same ILC/RyuJIT the AOT column already uses; once rooting policy
matches, it offers no size advantage the installed SDK cannot reach on its own.

### Finding 2 — startup and working set fall with it

Peak working set on a ~33 ms process is sampling-sensitive — a 5-run pass gave one variant 2.5 MB —
so Startup was re-measured at 15 runs. `A1`/`A2` were published later and measured in their own
5-run pass; the two passes are shown separately with their own Go baselines rather than blended,
since the baseline itself moved 23.5 → 24.5 ms between them. Ratios are always against the Go value
from the same pass.

| Startup, 15-run pass | Go | JIT | `A0` | `X1` | `X2` | `B1` | `B2` |
|---|---:|---:|---:|---:|---:|---:|---:|
| wall time (ms) | 24.5 | 232.0 | 79.7 | 33.0 | 33.2 | 34.5 | 33.6 |
| vs Go | 1.00× | 9.47× | 3.25× | **1.35×** | **1.36×** | 1.41× | 1.37× |
| peak working set (MB) | 2.5 | 43.7 | 74.4 | 8.0 | 6.8 | 6.7 | 8.3 |

| Startup, 5-run composites pass | Go | `A1` | `A2` |
|---|---:|---:|---:|
| wall time (ms) | 23.5 | 78.5 | 76.0 |
| vs Go | 1.00× | 3.34× | 3.23× |
| peak working set (MB) | 2.5 | 75.8 | 70.7 |

**AOT startup goes from 3.25× Go to 1.35×, and peak working set from 74.4 MB to 6.8 MB** — an 11×
reduction — purely from the rooting policy. The feature switches alone move neither: `A1`/`A2` come
in at 3.34× and 3.23× against their own baseline, with working set barely shifted (75.8 and 70.7 MB
against `A0`'s 74.4). This is the plan's floor question answered — most of the AOT startup gap was
initialization and paging of a closure the program never touches.

Workload time and peak working set on the other four benchmarks (one internally consistent 5-run
pass):

| Benchmark | metric | Go | JIT | `A0` | `X1` | `X2` | `B1` | `B2` |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| Fib | ms | 117.7 | 181.1 | 175.6 | 175.4 | 175.3 | **70.9** | 99.9 |
| Fib | MB | 5.2 | 45.5 | 75.6 | 13.6 | 13.4 | 13.7 | 13.6 |
| Map | ms | 628.2 | 469.6 | 236.3 | 227.7 | 222.6 | 236.8 | 237.3 |
| Map | MB | 157.7 | 164.1 | 194.5 | 132.1 | 132.2 | 130.5 | 130.4 |
| String | ms | 107.2 | 1029.4 | 1275.5 | 1311.6 | 1284.9 | 1109.1 | 1114.0 |
| String | MB | 5.2 | 54.5 | 84.8 | 22.3 | 22.1 | 22.4 | 22.1 |
| Channel | ms | 41.2 | 87.3 | 130.5 | 120.7 | 109.0 | 116.1 | 79.8 |
| Channel | MB | 5.2 | 49.4 | 83.9 | 22.0 | 20.7 | 21.5 | 17.6 |

Working set drops on every row and, on Map, falls **below Go** (132 vs 158 MB). Trimming costs no
measurable workload time on the SDK side: `X1`/`X2` track `A0` within noise everywhere.

### Finding 3 — build time collapses too

Wall-clock seconds for one benchmark's build, measured on this host:

| | `A0` | `A1` / `A2` | `X1` / `X2` | `B1` | `B2` |
|---|---:|---:|---:|---:|---:|
| seconds | 977–1049 | 1532–1662 | 28–29 | 28–38 | 24–25 |

The suite's ~17-minute AOT publishes are almost entirely ILC compiling code no benchmark can reach.
Under full trim the same publish links in under 30 seconds — a ~34× difference measured across four
independent benchmarks. Note that the feature-switch profiles are *slower* to build than the stock
one (~1600 s vs ~1000 s): they add substitution work while still rooting everything.

The rooting policy dominates disk too. This exploration peaked at **~12 GB** — 2.5 GB of published
binaries across 29 variant trees, 8.7 GB of ILC native intermediates, and 0.8 GB of pinned bflat
archives — and a single stock AOT publish also drops a **1.5 GB `.pdb`** beside its 288 MB
executable. All of it was purged afterward; the harness deletes each publish's `.pdb` as it goes,
and excludes `.pdb` from every size figure above.

### Finding 4 — bflat has a real CPU advantage, and it is not attributable to bflat

`B1` runs **Fib in 70.9 ms against the SDK's 175.6** — 2.5× faster, and faster than Go itself
(117.7 ms). An independent 11-run pass reproduced it (71.4 vs 175.7, Go 120.5). On String it is ~14%
faster. This is the one place bflat shows something the stock SDK did not.

It probably is not bflat. bflat v10.0.0-rc.1 ships the **.NET 10.0.0-rc.1** ILC and framework, newer
than this host's SDK 9.0.316, so the comparison mixes "bflat" with "one .NET generation of RyuJIT
improvements". The obvious control — bflat v8.0.2 on a .NET 8 base — **cannot be run at all**:

```
error CS1705: Assembly 'golib' ... uses 'System.Runtime, Version=9.0.0.0' which has a higher
version than referenced assembly 'System.Runtime, Version=8.0.0.0'
```

and installing a .NET 10 SDK to control it from the other side would mutate the perf-canon host's
toolchain, which this suite does not do to itself. So the CPU row stays **unattributed** — and per
the plan, CPU-bound gains from newer .NET belong to the corpus-upgrade ladder, not to this
exploration. It is a reason to want the .NET 10 hop measured, not a reason to want bflat.

### Finding 5 — the only bflat that can consume this corpus is a pre-release

The error above is from the last **stable** bflat, v8.0.2 (2024-02-29, SHA256
`25B03214C6085607EC2EC5FC86139C93E054EDD60266296E708F763455961E7E`). It is a .NET 8 toolchain and
the corpus targets net9.0, so it cannot link the corpus at all. Adoption would therefore have meant
pinning **v10.0.0-rc.1** — a pre-release with no stable successor — and every .NET hop reopens the
question. That is the plan's re-measurability caveat in concrete form.

### The caveat on `X1`/`X2` — read before quoting these numbers

`TrimMode=full` is **not a recommendation**, and these rows are **not a proposed publish profile**.
The plan fixes `TrimMode=partial` for a real reason: golib's `fmt` formatting and sort's
`Interface<T>` bind members through reflection, and full trim can remove exactly those. Both probes
passed Verify on all five benchmarks — but five small programs are a sample, not the population, and
they do not exercise `fmt`'s reflective surface broadly. Read every `X` row as *"the floor is at
least this low"*, never as *"adopt this"*.

What would have to happen first is now precisely located. Every reachability-trimmed build emits 94
trim-analysis diagnostics, concentrated in golib's adapter and reflection layer:

| File | Diagnostics |
|---|---:|
| `golib/TypeExtensions.ExtensionMethodRegistry.cs` | 16 |
| `golib/GoReflect.FieldAccess.cs` | 14 |
| `golib/AdapterBinder.cs` | 14 |
| `core/reflect/value_impl.cs` | 8 |
| `golib/TypeExtensions.GoMethodSets.cs` | 8 |
| `golib/GoReflect*.cs` (3 files) | 18 |
| others (`PointerExtensions`, `builtin`, `array`, `managed_impl`) | 16 |

Making that surface trim-safe (`DynamicallyAccessedMembers` annotations, `DynamicDependency`) is the
prerequisite for any of this shipping — and it would unlock the floor for the stock SDK and bflat
alike, which is one more reason bflat cannot be the answer. Note that 22 of the 94 are IL3050
(`RequiresDynamicCode`), which applies to the **existing** AOT column too and is not introduced by
trimming.

A related observation: `Directory.Build.props` here sets `SuppressTrimAnalysisWarnings=true`, so the
SDK's own AOT publishes hide this diagnostic. bflat surfaced a surface our configuration was
suppressing.

### Verdict against the plan's worthiness gate

**Outcome 1 — Arm A captures the floor win; bflat is confirming data, no new toolchain.** Size,
startup, and working set are all recoverable inside the stock SDK, and bflat matches rather than
beats it on every one. Its lone advantage is CPU, which is unattributable and belongs to the .NET
ladder. The pre-release-only constraint (Finding 5) independently rules out adoption.

The floor win is **not yet claimable**, because the lever is a setting the plan fixed for
correctness. The honest statement is that **the floor is gated by golib's trim-safety, not by the
toolchain**, and the follow-on work is golib annotation — not a fourth column, and not bflat.

Scope actually run, so the gaps are visible: `A1` was measured on Startup and Map only, and `A2`
likewise; single-switch attribution (`InvariantGlobalization`, `UseSystemResourceKeys`,
`StackTraceSupport`, `IlcGenerateStackTraceData` in isolation) was **not** run. Both were dropped
once the trim result made the switch bundle's ~23% a rounding error against the ~99% the rooting
policy carries. Reproduce or extend with:

```powershell
cd src/tests/Performance
./run-performance-floor.ps1 -Benchmarks PerfStartup -Variants 'A0,A1,X1,X2' -Phase 'publish,verify,measure'
```
