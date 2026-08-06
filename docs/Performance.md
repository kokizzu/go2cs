<!-- AUTO-COPIED from src/tests/Performance/README.md by run-performance.ps1 -- edit that file, not this one. -->

# Go vs transpiled C# — runtime performance comparison

A small, targeted benchmark suite answering the question people ask first: **how fast is transpiled
C# compared to the original Go?** — startup time and memory, on both the normal JIT runtime and
**Native AOT** (self-contained, the closest deployment analog to a Go binary).

Each benchmark is a tiny Go program (same shape as the
[behavioral tests](https://github.com/ritchiecarroll/go2cs/tree/master/src/tests/Behavioral)),
chosen to exercise one Go construct with a real C# cost model — slices, strings, maps, channels,
interface dispatch — plus raw compute loops where the two runtimes should be close. This is not an
exhaustive benchmark game; it gives the common expected range of differences.

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

## Running it

```powershell
cd src/tests/Performance
./run-performance.ps1                    # full run: transpile, build (incl. AOT), verify, measure
./run-performance.ps1 --no-aot           # skip AOT publishes, faster while iterating
./run-performance.ps1 --filter Map       # one benchmark
./run-performance.ps1 --runs 10 --update-readme   # refresh the results block below
```

Requirements: Go toolchain, .NET 9 SDK, and — for the AOT column — MSVC C++ build tools (Visual
Studio 2022's "Desktop development with C++" workload, which supplies the `link.exe` ILC needs).

The runner (`PerformanceRunner`), a dependency-free console app structured like the behavioral suite's
`BehavioralRunner`, runs **Transpile → Build → Verify → Measure**. Verify requires all three binaries
to produce identical (timing-filtered) stdout before anything is timed, so the table can never
silently report a benchmark that computes something different in C#.

## Results

<!-- PERF-RESULTS:BEGIN -->

**Environment:** 13th Gen Intel(R) Core(TM) i9-13900K · Microsoft Windows 10.0.26200 · go1.23.1 · .NET SDK 9.0.316 · 2026-07-26

C# builds: JIT = framework-dependent `Release`; Native AOT = `-p:PublishAot=true` self-contained, partial trim. Median of 5 runs (1 discarded warmup). Workload time is measured in-program and excludes process startup; the Startup row is pure process wall time. Ratios are relative to Go.

**Execution time** (milliseconds -- lower is better):

| Benchmark | Go | C# (JIT) | C# (Native AOT) |
|---|---:|---:|---:|
| Startup | 14.5 | 38.6 (2.65×) | 16.8 (1.16×) |
| Fib | 79.6 | 97.9 (1.23×) | 87.5 (1.10×) |
| Sieve | 72.4 | 94.8 (1.31×) | 138.7 (1.92×) |
| MatMul | 55.3 | 132.6 (2.40×) | 199.4 (3.61×) |
| String | 70.3 | 749.4 (10.66×) | 764.1 (10.87×) |
| Map | 302.8 | 257.8 (0.85×) | 95.8 (0.32×) |
| Sort | 112.8 | 422.8 (3.75×) | 431.3 (3.82×) |
| Channel | 43.6 | 88.3 (2.03×) | 94.3 (2.16×) |
| StringView | 7.2 | 21.0 (2.91×) | 14.2 (1.97×) |
| StringMatch | 149.4 | 1,368.8 (9.16×) | 1,354.1 (9.06×) |
| IfaceCall | 98.3 | 274.0 (2.79×) | 241.4 (2.46×) |
| Iface | 63.5 | 370.8 (5.84×) | 265.7 (4.18×) |
| IfaceShell | 13.3 | 530.9 (39.83×) | 599.6 (44.99×) |

**Peak memory** (working set, MB -- lower is better):

| Benchmark | Go | C# (JIT) | C# (Native AOT) |
|---|---:|---:|---:|
| Startup | 2.5 | 19.3 | 2.6 |
| Fib | 5.4 | 18.5 | 10.8 |
| Sieve | 34.9 | 39.6 | 30.1 |
| MatMul | 10.6 | 28.3 | 17.0 |
| String | 5.4 | 39.8 | 29.2 |
| Map | 158.8 | 138.0 | 128.5 |
| Sort | 21.7 | 43.0 | 29.3 |
| Channel | 5.4 | 25.6 | 17.3 |
| StringView | 5.4 | 20.0 | 10.8 |
| StringMatch | 5.4 | 43.2 | 29.1 |
| IfaceCall | 5.3 | 21.5 | 10.8 |
| Iface | 5.3 | 20.9 | 11.1 |
| IfaceShell | 5.3 | 44.6 | 31.4 |

<!-- PERF-RESULTS:END -->

### Reading the results

- **Startup:** Go wins cold process start against the JIT by ~2.7× (runtime load + JIT-on-the-fly), and
  **Native AOT closes most of the gap** (16.8 ms vs Go's 14.5, at a few MB of memory) — the deployment
  story for CLI-shaped transpiled programs, and why C# can *appear* faster in casual warm-process
  timing comparisons.
- **Function calls / integers (Fib):** the closest workload — transpiled C# is within ~10–25% of Go.
- **Slices & floats (Sieve, MatMul):** 1.3–3.6×; the gap is `slice<T>` header emulation and bounds
  checks the JIT can't always elide, compounded on nested `[][]float64` access. **AOT is *slower*
  than the JIT here** — ILC lacks the JIT's dynamic PGO/OSR loop optimizations, trading tight-loop
  throughput for AOT's startup and memory wins.
- **String:** the biggest honest gap (~10–11×) — every `[]byte`→`string` round-trip is an allocation +
  copy through the `@string` emulation, versus Go's `append` chain inlining to a few instructions.
  This benchmark's conversions are all **ineligible** for the stack-string optimization (its `s` is a
  concat operand and its buffer is mutated), so they stay `@string` — see StringView for the eligible case.
- **StringView (JIT ~2.9×, AOT ~2.0×):** the same `[]byte`→`string` cost, but for the subset the
  converter proves non-escaping and read/compare-only, where it emits a zero-copy stack string
  (`sstring`) instead of `@string` (see [ConversionStrategies-Reference](https://github.com/ritchiecarroll/go2cs/blob/master/docs/ConversionStrategies-Reference.md)).
  The converter hoists one `sstring` view per call rather than re-materializing it per comparison,
  since the JIT won't lift a `ref struct` view out of a loop on its own. Closer to Go than String, and
  the number to watch as the eligibility surface widens — arc detail in
  [DESIGN-string-literal-allocation.md](https://github.com/ritchiecarroll/go2cs/blob/master/docs/phase4/DESIGN-string-literal-allocation.md).
- **StringMatch (JIT ~9.2×, AOT ~9.1×):** literal-heavy hot paths — switch-on-string dispatch,
  `strings.HasPrefix` against a literal prefix, literal returns, literal map-key counters. The
  instrument for the same literal-comparison optimizations StringView exercises (span operators,
  `u8` casts, hoisted literals); arc detail in
  [DESIGN-string-literal-allocation.md](https://github.com/ritchiecarroll/go2cs/blob/master/docs/phase4/DESIGN-string-literal-allocation.md).
- **Map:** transpiled C# is *faster than Go* — `map<K,V>` rides .NET's heavily-optimized
  `Dictionary`, and the AOT build is ~3× faster than Go on this insert/lookup/delete churn.
- **Sort (~3.8×):** the runtime's `sort.Interface` shim (`Interface<T>`) binds `Len`/`Less`/`Swap` via
  reflection-created delegates — cached, but a delegate hop per comparison.
- **Channel (~2.1×):** `channel<T>` + goroutine emulation over managed threading vs Go's runtime
  scheduler. Down from ~2.7–3.4× in the 2026-07-12 table (*History*) after the channels redesign —
  real unbuffered rendezvous, single-fire select, operand-once hoisting.
- **IfaceCall (JIT ~2.8×, AOT ~2.5×):** pure interface method dispatch — no asserts, no shell
  construction, just calling through an interface value built once, in a megamorphic hot loop. The
  floor for what *calling* through an interface costs, distinct from *obtaining* one (the two rows
  below).
- **Iface (JIT ~5.8×, AOT ~4.2×) — the everyday interface story:** dispatch through statically-known
  interface values, a comma-ok assertion, and a type switch over a closed set, all resolved by
  generated adapters and ordinary casts. This row was published at **158× (JIT) / 660× (AOT)** and
  marked OPEN; both figures turned out to measure two `golib` defects — an uncached attribute lookup
  and redundant type-tests on the assertion-miss path — rather than the cost of the construct. Both
  are fixed; full root-cause writeup in
  [DESIGN-iface-shell-caching.md](https://github.com/ritchiecarroll/go2cs/blob/master/docs/phase4/DESIGN-iface-shell-caching.md).
- **IfaceShell (JIT ~40×, AOT ~45×) — the one operation C# has no native answer for:** satisfying an
  interface **structurally at run time**. Go resolves this with a cached itab lookup — two machine
  words, a nanosecond hash probe. C# has no two-word interface value, so go2cs constructs a wrapper
  ("shell") that forwards to the concrete value on every assertion; the ratio is the price of that
  construction plus, on the value-typed tier, a reflective forwarded call. An assertion the converter
  can resolve at compile time (the Iface row above) skips this path via a generated adapter instead.
  AOT is *slower* here, unlike Startup: its generic shell tier can degrade to the reflective object
  shell, and AOT's reflective invokers can't emit IL stubs. Optimization directions:
  [DESIGN-iface-shell-caching.md](https://github.com/ritchiecarroll/go2cs/blob/master/docs/phase4/DESIGN-iface-shell-caching.md).

### History

> **Note:** when the toolchain moves (e.g. .NET 9 → .NET 10), copy the current results block into
> this section with its environment line before re-running `--update-readme`, so
> version-over-version comparisons accumulate here.

**Captured 2026-07-26 — pre-fix baseline for the `Iface` root-cause arc**, same toolchain as the
current table. `Iface` at **158.24× (JIT) / 660.42× (AOT)** is the published-OPEN figure the fix in
[DESIGN-iface-shell-caching.md](https://github.com/ritchiecarroll/go2cs/blob/master/docs/phase4/DESIGN-iface-shell-caching.md) explains; every
other row here is within run-to-run variance of the current table:

**Environment:** 13th Gen Intel(R) Core(TM) i9-13900K · Microsoft Windows 10.0.26200 · go1.23.1 · .NET SDK 9.0.316 · 2026-07-26

C# builds: JIT = framework-dependent `Release`; Native AOT = `-p:PublishAot=true` self-contained, partial trim. Median of 5 runs (1 discarded warmup). Workload time is measured in-program and excludes process startup; the Startup row is pure process wall time. Ratios are relative to Go.

**Execution time** (milliseconds -- lower is better):

| Benchmark | Go | C# (JIT) | C# (Native AOT) |
|---|---:|---:|---:|
| Startup | 15.4 | 41.4 (2.69×) | 16.7 (1.09×) |
| Fib | 80.3 | 99.1 (1.23×) | 82.6 (1.03×) |
| Sieve | 73.3 | 94.4 (1.29×) | 138.3 (1.89×) |
| MatMul | 55.2 | 133.8 (2.42×) | 196.3 (3.55×) |
| String | 69.7 | 748.8 (10.75×) | 754.7 (10.83×) |
| Map | 309.5 | 268.6 (0.87×) | 97.0 (0.31×) |
| Sort | 114.1 | 423.7 (3.71×) | 422.3 (3.70×) |
| Channel | 45.4 | 89.6 (1.97×) | 94.4 (2.08×) |
| StringView | 7.2 | 21.2 (2.93×) | 14.2 (1.97×) |
| StringMatch | 147.8 | 1,417.4 (9.59×) | 1,352.2 (9.15×) |
| Iface | 63.9 | 10,117.8 (158.24×) | 42,228.2 (660.42×) |
| IfaceShell | 12.9 | 575.4 (44.58×) | 624.2 (48.36×) |

**Peak memory** (working set, MB -- lower is better):

| Benchmark | Go | C# (JIT) | C# (Native AOT) |
|---|---:|---:|---:|
| Startup | 2.5 | 19.2 | 2.6 |
| Fib | 5.4 | 21.9 | 10.7 |
| Sieve | 35.3 | 39.2 | 30.1 |
| MatMul | 10.3 | 26.5 | 17.0 |
| String | 5.3 | 39.5 | 29.2 |
| Map | 158.9 | 140.0 | 128.5 |
| Sort | 21.6 | 42.3 | 29.2 |
| Channel | 5.3 | 29.6 | 17.3 |
| StringView | 5.3 | 20.5 | 10.8 |
| StringMatch | 5.4 | 44.2 | 29.1 |
| Iface | 5.4 | 41.3 | 29.6 |
| IfaceShell | 5.3 | 44.0 | 31.6 |

**Captured 2026-07-25 — pre-arc baseline for the `@string` literal-allocation arc**
([DESIGN-string-literal-allocation.md](https://github.com/ritchiecarroll/go2cs/blob/master/docs/phase4/DESIGN-string-literal-allocation.md),
Tiers A/A′/B/C). `StringMatch` is the instrument; `StringView`/`String`/`Map` are the non-regression
oracles:

**Environment:** 13th Gen Intel(R) Core(TM) i9-13900K · Microsoft Windows 10.0.26200 · go1.23.1 · .NET SDK 9.0.316 · 2026-07-25

C# builds: JIT = framework-dependent `Release`; Native AOT = `-p:PublishAot=true` self-contained, partial trim. Median of 5 runs (1 discarded warmup). Workload time is measured in-program and excludes process startup; the Startup row is pure process wall time. Ratios are relative to Go.

**Execution time** (milliseconds -- lower is better):

| Benchmark | Go | C# (JIT) | C# (Native AOT) |
|---|---:|---:|---:|
| Startup | 13.9 | 39.1 (2.80×) | 17.0 (1.22×) |
| Fib | 79.1 | 97.7 (1.24×) | 86.7 (1.10×) |
| Sieve | 72.9 | 92.8 (1.27×) | 135.7 (1.86×) |
| MatMul | 53.8 | 126.0 (2.34×) | 190.1 (3.53×) |
| String | 69.1 | 746.1 (10.79×) | 768.3 (11.11×) |
| Map | 297.6 | 256.3 (0.86×) | 91.1 (0.31×) |
| Sort | 112.6 | 416.0 (3.69×) | 429.2 (3.81×) |
| Channel | 45.9 | 78.3 (1.71×) | 92.4 (2.01×) |
| StringView | 7.3 | 22.1 (3.04×) | 14.1 (1.93×) |
| StringMatch | 144.7 | 1,699.5 (11.75×) | 1,762.5 (12.18×) |
| IfaceShell | 13.2 | 2,630.4 (198.64×) | 957.8 (72.33×) |

**Peak memory** (working set, MB -- lower is better):

| Benchmark | Go | C# (JIT) | C# (Native AOT) |
|---|---:|---:|---:|
| Startup | 2.5 | 16.9 | 3.5 |
| Fib | 5.4 | 18.7 | 10.8 |
| Sieve | 35.1 | 40.0 | 30.1 |
| MatMul | 10.3 | 26.2 | 17.0 |
| String | 5.4 | 39.5 | 29.2 |
| Map | 158.4 | 139.6 | 128.5 |
| Sort | 21.8 | 41.3 | 29.2 |
| Channel | 5.4 | 25.0 | 16.0 |
| StringView | 5.4 | 19.5 | 10.8 |
| StringMatch | 5.5 | 42.5 | 31.6 |
| IfaceShell | 5.4 | 44.0 | 31.3 |

**Captured 2026-07-12** on .NET SDK 9.0.315 — the last table before the `IfaceShell` row (runtime
duck-typed interface asserts) was added, kept for row-over-row comparison:

**Environment:** 13th Gen Intel(R) Core(TM) i9-13900K · Microsoft Windows 10.0.26200 · go1.23.1 · .NET SDK 9.0.315 · 2026-07-12

C# builds: JIT = framework-dependent `Release`; Native AOT = `-p:PublishAot=true` self-contained, partial trim. Median of 5 runs (1 discarded warmup). Workload time is measured in-program and excludes process startup; the Startup row is pure process wall time. Ratios are relative to Go.

**Execution time** (milliseconds -- lower is better):

| Benchmark | Go | C# (JIT) | C# (Native AOT) |
|---|---:|---:|---:|
| Startup | 12.2 | 34.4 (2.82×) | 16.0 (1.31×) |
| Fib | 79.9 | 99.4 (1.24×) | 87.5 (1.10×) |
| Sieve | 71.2 | 95.4 (1.34×) | 147.4 (2.07×) |
| MatMul | 54.5 | 132.4 (2.43×) | 192.7 (3.53×) |
| Map | 258.9 | 220.6 (0.85×) | 79.0 (0.31×) |
| Sort | 113.6 | 411.8 (3.63×) | 418.5 (3.68×) |
| Channel | 43.6 | 147.7 (3.39×) | 116.3 (2.67×) |
| String (heap) | 69.9 | 754.5 (10.79×) | 775.3 (11.09×) |
| StringView (stack) | 7.6 | 23.4 (3.09×) | 14.1 (1.86×) |

**Peak memory** (working set, MB -- lower is better):

| Benchmark | Go | C# (JIT) | C# (Native AOT) |
|---|---:|---:|---:|
| Startup | 2.6 | 17.1 | 2.6 |
| Fib | 5.5 | 18.7 | 10.7 |
| Sieve | 35.4 | 40.8 | 30.0 |
| MatMul | 10.2 | 26.5 | 16.9 |
| Map | 158.3 | 137.3 | 128.4 |
| Sort | 21.8 | 41.5 | 28.8 |
| Channel | 5.5 | 39.2 | 10.8 |
| String (heap) | 5.5 | 38.6 | 28.9 |
| StringView (stack) | 2.9 | 19.3 | 10.7 |
