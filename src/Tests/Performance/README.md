# Go vs transpiled C# — runtime performance comparison

A small, targeted benchmark suite answering the question people ask first about go2cs: **"how fast is
the transpiled C# compared to the original Go?"** — including startup time and memory, and including
C# both on the normal JIT runtime and compiled with **Native AOT** (self-contained executables with
faster startup and lower memory, the closest deployment analog to a Go binary).

This is deliberately *not* an exhaustive benchmark game. Each benchmark is a tiny Go program (same
shape as the [behavioral tests](https://github.com/ritchiecarroll/go2cs/tree/master/src/Tests/Behavioral)) chosen to exercise one Go construct whose C# emulation
has a real cost model — slices, strings, maps, channels, interface dispatch — plus raw compute loops
where the two runtimes should be close. Results below give the "common expected" range of differences.

## The benchmarks

| Benchmark | What it exercises |
|---|---|
| **Startup** | Empty workload: pure process start + runtime init + one `fmt` round-trip. Wall time. |
| **Fib** | Recursive Fibonacci (`fib(34)` ×5): function-call and integer-op overhead. |
| **Sieve** | Sieve of Eratosthenes to 10M ×3: slice allocation, indexing, tight loops (`slice<T>` bounds/header emulation). |
| **MatMul** | 256×256 `float64` matrix multiply ×4: floating-point throughput, nested slice-of-slice access. |
| **String** | 10M iterations of byte-slice append → `string` conversion, indexing, concatenation (`@string` emulation). |
| **StringView** | 20M iterations of keyword checks `string(buf) == "null"/"true"/"false"` over a fixed buffer — the idiom the converter's stack-string (`sstring`) emission optimizes: a zero-copy view compared against a `u8` literal span, no per-comparison allocation. |
| **StringMatch** | 20M iterations of literal-string hot paths: switch-on-string dispatch, `strings.HasPrefix` with a literal prefix, literal returns, and literal map-key counters — the shapes where converted C# historically allocated a fresh `@string` per evaluation while Go allocates nothing (literals live in RODATA). The instrument for the tiered literal optimizations (span comparison operators / `u8` casts / hoisted literals). |
| **Map** | 2M inserts + 2M comma-ok lookups + 1M deletes on `map[int]int` (`map<K,V>` emulation). |
| **Sort** | `sort.Ints` on 2M deterministic pseudo-random ints (`sort.Interface` dispatch through the runtime's reflection-bound `Interface<T>`). |
| **Channel** | 1M ints producer→consumer through a buffered channel with one goroutine (`channel<T>` + goroutine scheduling emulation). |
| **IfaceShell** | 5M iterations × 2 duck-typed interface asserts + forwarded calls — one on a value-typed dynamic value (the reflective **object shell**), one on a pointer-sourced one (the delegate-bound **generic shell**). The one path with no compile-time answer, and the only shared mechanism whose Native AOT behavior is otherwise unexercised. |

Every benchmark prints a deterministic **checksum** (verified byte-identical across Go, C# JIT, and
C# AOT before anything is measured) plus its own workload time measured in-program via
`time.Now().UnixNano()` — so the headline numbers exclude process startup, which is reported
separately by the Startup row.

## Methodology / fairness notes

- **Three variants of the identical program:** the Go binary (`go build`, default optimized), the
  transpiled C# built `Release` framework-dependent (JIT column), and the same C# published with
  `PublishAot=true` self-contained, partial trim (Native AOT column).
- **Median of 5 runs** (configurable), after 1 discarded warmup run per variant; single-shot process
  executions, the way a Go CLI program actually runs. For the JIT column this deliberately *includes*
  in-process tiered-JIT warmup inside the workload — that is the honest cost of running a transpiled
  program once. Long-running server workloads would look better for the JIT than these numbers.
- **Peak memory** is the process peak working set, polled while the process runs.
- **Wall time and workload time are both captured**; tables report workload time (Startup row: wall).
- Benchmarks avoid nondeterminism (no `math/rand`; xorshift/LCG inline generators) so outputs are
  byte-comparable, and print timing on a filtered `elapsed_ns:` line.

## Running it

```powershell
cd src/Tests/Performance
./run-performance.ps1                    # full run: transpile, build (incl. AOT), verify, measure
./run-performance.ps1 --no-aot           # much faster while iterating (skips the AOT publishes)
./run-performance.ps1 --filter Map       # one benchmark
./run-performance.ps1 --runs 10 --update-readme   # refresh the results block below
```

Requirements: Go toolchain, .NET 9 SDK, and for the AOT column the MSVC C++ build tools (Visual
Studio 2022 with "Desktop development with C++" — the ILC native linker needs `link.exe`).

The runner (`PerformanceRunner`) is a dependency-free console app, structured like the behavioral
suite's `BehavioralRunner`: **Transpile → Build → Verify → Measure**. The Verify phase runs all three
binaries and requires identical (timing-filtered) stdout before any timing is recorded, so the table
can never silently report a benchmark that computes something different in C#.

## Results

<!-- PERF-RESULTS:BEGIN -->

**Environment:** 13th Gen Intel(R) Core(TM) i9-13900K · Microsoft Windows 10.0.26200 · go1.23.1 · .NET SDK 9.0.316 · 2026-07-26

C# builds: JIT = framework-dependent `Release`; Native AOT = `-p:PublishAot=true` self-contained, partial trim. Median of 5 runs (1 discarded warmup). Workload time is measured in-program and excludes process startup; the Startup row is pure process wall time. Ratios are relative to Go.

**Execution time** (milliseconds -- lower is better):

| Benchmark | Go | C# (JIT) | C# (Native AOT) |
|---|---:|---:|---:|
| Startup | 13.9 | 38.7 (2.79×) | 15.6 (1.12×) |
| Fib | 80.4 | 98.0 (1.22×) | 82.8 (1.03×) |
| Sieve | 73.1 | 94.0 (1.29×) | 138.0 (1.89×) |
| MatMul | 54.8 | 137.7 (2.51×) | 196.0 (3.57×) |
| String | 70.4 | 756.9 (10.75×) | 755.1 (10.72×) |
| Map | 311.4 | 266.5 (0.86×) | 104.3 (0.34×) |
| Sort | 113.4 | 406.1 (3.58×) | 413.6 (3.65×) |
| Channel | 42.9 | 88.1 (2.05×) | 91.0 (2.12×) |
| StringView | 7.2 | 22.0 (3.05×) | 14.1 (1.95×) |
| StringMatch | 150.9 | 1,386.1 (9.19×) | 1,321.4 (8.76×) |
| IfaceShell | 13.4 | 2,502.7 (186.95×) | 969.7 (72.44×) |

**Peak memory** (working set, MB -- lower is better):

| Benchmark | Go | C# (JIT) | C# (Native AOT) |
|---|---:|---:|---:|
| Startup | 2.5 | 15.1 | 8.8 |
| Fib | 5.3 | 18.7 | 10.8 |
| Sieve | 35.0 | 38.8 | 30.1 |
| MatMul | 10.1 | 28.0 | 17.1 |
| String | 5.4 | 39.2 | 29.2 |
| Map | 158.8 | 138.4 | 128.4 |
| Sort | 21.7 | 41.4 | 29.2 |
| Channel | 5.4 | 24.6 | 16.0 |
| StringView | 3.8 | 21.2 | 10.8 |
| StringMatch | 5.3 | 42.7 | 29.0 |
| IfaceShell | 5.3 | 45.3 | 31.3 |

<!-- PERF-RESULTS:END -->

### Reading the results

What the numbers above actually show, and why:

- **Startup:** Go wins cold process start against the JIT ~2× (runtime load + JIT-on-the-fly), and
  **Native AOT erases the gap entirely** (within a few percent of Go, at a few MB of memory). This is
  the deployment story for CLI-shaped transpiled programs — and also why C# can *appear* faster in
  casual test-harness timing comparisons that measure warm processes doing trivial work.
- **Function calls / integers (Fib):** the closest workload — the transpiled C# is within ~10–25% of Go.
- **Slices & floats (Sieve, MatMul):** 1.3–2.5×; the gap is `slice<T>` header emulation and bounds
  checks the JIT can't always elide, compounded on nested `[][]float64` access. Note **AOT is *slower*
  than the JIT here** — ILC lacks the JIT's dynamic PGO / OSR loop optimizations, so AOT trades tight-
  loop throughput for its startup and memory wins.
- **String:** the biggest honest gap (~10–11×): every `[]byte`→`string` round-trip is an allocation +
  copy through the `@string` emulation, plus the per-call `append` chain Go inlines to a few
  instructions. (Down from an initial ~11–14× — this suite caught a per-`append` array allocation and a
  single-element slow path in `golib`; it remains the number to watch when optimizing `@string`.) The
  String benchmark's conversions are all **ineligible** for the stack-string optimization (its `s` is a
  concat operand and its buffer is mutated), so they stay `@string` — see StringView for the eligible case.
- **StringView (JIT ~3.1×, AOT ~1.9×):** the same `[]byte`→`string` cost, but for the subset the converter
  can prove non-escaping and used only in safe reads/comparisons — where it emits a zero-copy stack string
  (`sstring`) instead of `@string` (see [ConversionStrategies-Reference](https://github.com/ritchiecarroll/go2cs/blob/master/docs/ConversionStrategies-Reference.md)).
  Both runtimes already stack-allocate `@string`'s byte[] here, so the win is eliminating the per-comparison
  **copy** and the **literal allocation** (`@string == "…"u8` materializes the literal every time; `sstring`
  compares spans in place): in isolation the eligible comparison runs **~12× faster than `@string` on the
  JIT and ~11× on Native AOT**. This row also now reflects **loop-invariant hoisting** (Roadmap increment 5):
  the converter was re-materializing the zero-copy view on each of this benchmark's three repeated
  comparisons, and the JIT will not lift a `ref struct` view out of a loop — so the converter now emits **one**
  hoisted `sstring` per call and reuses it. A clean back-to-back A/B on this machine measured the JIT drop
  from **4.84× → 3.04× Go** (35.9 → 22.5 ms) and Native AOT from **4.49× → 1.86×** (34.4 → 14.1 ms) — about
  the practical floor within .NET (the residual is `SequenceEqual`'s per-call setup on a tiny buffer vs Go's
  inlined `memcmp`; a decomposition micro-benchmark confirmed the `sstring` `==` operator itself adds zero
  over a raw span compare). Closer to Go than String, and the number to watch as the eligibility surface
  widens.
- **Map:** the transpiled C# is *faster than Go* — `map<K,V>` rides .NET's heavily-optimized
  `Dictionary`, and the AOT build is ~3× faster than Go on this insert/lookup/delete churn.
- **Sort (~3.5×):** the runtime's `sort.Interface` shim (`Interface<T>`) binds `Len`/`Less`/`Swap` via
  reflection-created delegates — cached, but a delegate hop per comparison.
- **Channel (~1.9×):** `channel<T>` + goroutine emulation over managed threading vs Go's runtime
  scheduler. Down from ~2.7–3.4× in the 2026-07-12 table (see *History*) — the channels redesign
  (real unbuffered rendezvous, single-fire select, operand-once hoisting) landed in between.
- **IfaceShell (JIT ~60×, AOT ~75×) — read this row differently from every other row.**
  > ⚠ **The JIT cell in the table above is stale and too slow.** Until 2026-07-26 the Native AOT
  > publish's build step overwrote the JIT binary in place — the converter csproj template pinned
  > `$(OutDir)` to the JIT tree, which outranks the `$(BaseOutputPath)` isolation in
  > `Directory.Build.props` — so the "JIT" column was timing a self-contained binary with
  > `IsDynamicCodeSupported=false`, whose reflective invokers can never emit their IL stubs. Fixed at
  > the template; `PerformanceRunner` now reads the runtimeconfig before measuring and **fails the
  > run** if the JIT binary is self-contained or has dynamic code disabled. Re-measured on the fixed
  > tree: **789.8 ms (60.43×)**, down from 2,504.4 ms (188.47×) — 3.17× — and a *full-suite* run,
  > with all eleven AOT publishes in the same pass, reproduced it at 798.8 ms (56.99×), which is the
  > proof the isolation holds under exactly the condition that used to break it. Those figures are
  > provisional (taken with another agent resident on the box); the whole table's authoritative
  > `--update-readme` re-measure on a quiet machine is owed.

  It measures the one operation C# has *no* answer for: satisfying an interface **structurally at run
  time**. Go answers it with a cached itab lookup that is essentially free (~13 ms for 10M asserts
  ≈ 1.3 ns each); go2cs has to *construct* an implementation. A memoized assert is not one dictionary
  hit but **two** lookups (one of which, the nominal `(Type,Type)` probe, cannot hit on this path by
  construction), three `GetType()` calls, ~7 type tests, a fresh shell allocation, and then the
  forwarded call — which on the reflective tier adds an `object[]` and a boxed return. The ratio is
  the price of the *capability*, not a regression, and it is **not** what ordinary interface use
  costs: an assertion the converter could record resolves through a generated nominal adapter
  (≈1.1 ns) and never reaches this path, while a shell obtained once and called repeatedly pays
  4.4 ns/call on the delegate tier and 22 ns/call on the reflective tier. The remaining work on this
  row is itemized in [`docs/Phase4/DESIGN-iface-shell-caching.md`](../../../docs/Phase4/DESIGN-iface-shell-caching.md).

  What this row is really for is that the numbers **exist at all** under Native AOT: before the
  shells, the equivalent assert was resolved by reflecting for a generated conversion method and
  closing it with `MakeGenericMethod`, which under AOT can only succeed for an instantiation `ilc`
  already rooted — and a measured A/B against that mechanism has the AOT binary answer **MISS for
  both tiers and print a checksum of 0**, silently computing the wrong result (and taking 9.8 s doing
  it, since a failing close is retried per iteration). With the shells the AOT binary produces the
  correct checksum, which is why this benchmark exists. One honest detail visible here: the **belt
  fires** under AOT — the pointer tier's `Δ<Iface><pointee>` instantiation is unavailable, so it
  degrades to the reflective object shell rather than to a miss (verified by tier name:
  `Δrun_typeᴛ1<box>` on the JIT, `Δrun_typeᴛ1ᴛObj` under AOT). That degradation, plus AOT's own
  `IsDynamicCodeSupported=false`, is why AOT is now the *slower* column here (976.5 ms vs 789.8):
  under AOT **both** tiers are reflective and neither can emit an invoke stub. The previous claim
  that AOT was ~2.7× faster on this row was an artifact of the clobbered JIT binary described above.

### History

When the toolchain moves (e.g. .NET 9 → .NET 10), copy the current results block into this section
with its environment line before re-running `--update-readme`, so version-over-version comparisons
accumulate here.

**Captured 2026-07-25 on .NET SDK 9.0.316 -- the PRE-ARC baseline for the `@string` literal-allocation
arc** (Tiers A / A′ / B / C, [`docs/Phase4/DESIGN-string-literal-allocation.md`](../../../docs/Phase4/DESIGN-string-literal-allocation.md)).
Measured on a verified-quiet machine per the design's §4.8 protocol, so the per-tier `--filter StringMatch`
deltas that follow attribute to the arc and not to machine load. `StringMatch` is the instrument
(literal `==` chains, literal `HasPrefix` arguments, literal returns, literal map keys); `StringView`
is the flatness oracle (sstring paths must not move) and `String`/`Map` the non-regression oracles:

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

**Captured 2026-07-12 on .NET SDK 9.0.315** -- the last table before the `IfaceShell` row
(runtime duck-typed interface asserts) was added, kept for row-over-row comparison:

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

