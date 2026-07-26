<!-- AUTO-COPIED from src/Tests/Performance/README.md by run-performance.ps1 -- edit that file, not this one. -->

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
| **IfaceCall** | 50M iterations of PURE interface method dispatch — interface values of statically-known types built once, called in a megamorphic hot loop; no asserts, no switches. The row that answers "what does calling an interface method cost?" |
| **Iface** | 20M iterations of the **common** interface cases: method dispatch through interface values of statically-known types, concrete comma-ok assertions, and a type switch over a closed set — all resolved by the compile-time (nominal) machinery: generated adapters and cast-shaped asserts. The row that shows what ordinary Go interface code costs. |
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
- **Iface (JIT ~5.9×, AOT ~4.2×) — root-caused; this row was published at 158×/660× and marked OPEN.**
  The *default* interface story: dispatch through interface values of statically-known types, a
  known-type comma-ok assertion, and a type switch over a closed set — all resolved by the
  compile-time (nominal) machinery, generated adapters and ordinary casts, never the runtime shell
  machinery of the row below. The published figure was real, reproducible and **not** a regression,
  but it measured two `golib` defects rather than the cost of the construct. Both are now fixed, and
  both were on paths every converted program uses:

  1. **An uncached attribute lookup on every *failed* assertion.** golib's type-assert ladder ends,
     below its matching arms, in a tier answering Go's rule that only *anonymous* struct types
     convert to each other — and it asked that question with `GetCustomAttribute`, which materializes
     a fresh attribute instance on every call. Measured against live golib: **785.92 ns and 368 bytes
     per call on the JIT; 3,826.27 ns and 2,017 bytes under Native AOT.** An assertion that
     *succeeds* returns before reaching that tier, which is why no earlier benchmark saw it — this
     one asserts `s.(Circle)` against six shapes of which four are not `Circle`, so two thirds of its
     iterations took the miss path. At that rate the call alone predicts ~524 ns and ~2,552 ns per
     iteration against the measured 506 ns and 2,111 ns: **it accounts for essentially the whole of
     both columns.** It is also the entire explanation of the AOT column, whose 4.17× penalty over
     the JIT was simply that one call's own AOT/JIT ratio (4.87×) — ILC parses the attribute blob out
     of image metadata per call and has no equivalent of the JIT's caching. Nothing to do with
     generics, shared-generic dictionaries or ILC codegen. Fixed by memoizing the answer per type and
     hoisting it to a per-closed-generic constant, so the ordinary named-struct miss short-circuits
     ahead of it: **10,117.8 ms (158.24×) → 458.1 ms (7.20×)** on the JIT, with peak working set
     dropping 41.3 → 20.9 MB as ~4.9 GB of per-run attribute garbage stopped being allocated.
  2. **Four failing interface type-tests per iteration.** An interface value that came from a Go
     pointer, or from another interface, is carried by a generated wrapper the runtime must unwrap
     before matching — so the type switch probed two marker interfaces and the assertion probed the
     same two. A *failing* **interface** type test costs ~2.9 ns on the JIT (the runtime walks the
     type's interface map) where a failing sealed-**class** test is too small to measure. The two
     markers now share one empty base, probed once to gate both tiers: **458.1 → 379.7 ms (5.95×)**.

  What remains is the honest cost model. Per iteration Go does the whole body in **3.2 ns**, the
  transpiled C# in **18.5 ns** (JIT) and **13.1 ns** (AOT). A decomposition micro-benchmark against
  live golib attributes the JIT figure: slice-of-interface element read **1.8 ns**, two interface
  dispatches **3.9 ns**, the comma-ok assertion **8.1 ns**, the type switch **4.4 ns**. For scale, a
  plain C# `s is Circle c` over the same values costs 1.2 ns and a bare C# pattern ladder 1.4 ns —
  both indistinguishable from the slice read alone, so nearly all of the residual is the call into
  `golib` and the one surviving adapter probe inside it. That is the price of a real semantic
  obligation: Go's interface value is two words and an assertion is a compare against a cached itab,
  while C#'s is a single object reference, so the same question must first ask whether the value is a
  wrapper standing in for another one. Removing that last probe needs the marker to be a base
  *class* rather than an interface — a `go2cs-gen` change, recorded as the next candidate in
  [`docs/Phase4/DESIGN-iface-shell-caching.md`](../../../docs/Phase4/DESIGN-iface-shell-caching.md) §11.
  **Note this is one of the few rows where Native AOT beats the JIT**, and for the same reason: ILC's
  failing interface type tests are markedly cheaper (measured 3.7 ns for two, against 9.2 ns on the
  JIT), so the residual that remains is the residual AOT is best at.
- **IfaceShell — read this row differently from every other row.** It measures the one operation
  C# has *no* native answer for: satisfying an interface **structurally at run time**. Go resolves
  an interface assertion with a cached itab lookup — a hash probe into a global
  (concrete type, interface) table, roughly a nanosecond, with the interface value itself just two
  machine words. C# has no two-word interface value, so go2cs must *construct* a wrapper (a runtime
  "shell") that implements the interface by forwarding to the concrete value, and hand one back on
  every assertion.

  The mechanics of the gap: the *lookup* side is Go-shaped — one cache entry per
  (dynamic type, interface) behind a monomorphic slot, so a resolved pair costs a static field read
  and two compares. What remains per iteration is what Go genuinely does not pay: allocating the
  shell object per assertion, and, on the value-typed tier, a reflective forwarded call with a boxed
  return. The ratio is the price of the *capability*, not a regression. An assertion the converter
  can resolve nominally goes through a generated adapter (an ordinary cast) and never reaches this
  path. The Iface row above is the measurement of that claim: once its own two defects were fixed it
  **does** support it — the nominal path costs single-digit nanoseconds per operation against this
  row's hundreds, a ~7× separation between the two rows. A shell obtained
  once and called repeatedly pays only the per-call forwarding cost: a delegate hop for
  pointer-sourced values, a reflective invoke for value-typed ones. Remaining optimization directions are itemized
  in [`docs/Phase4/DESIGN-iface-shell-caching.md`](../../../docs/Phase4/DESIGN-iface-shell-caching.md).

  Why AOT is the *slower* column here, unlike Startup: under Native AOT the pointer tier's generic
  shell instantiation may not exist in the compiled image, so it degrades — by design — to the
  reflective object shell, and AOT's reflective invokers cannot emit IL stubs; both tiers then pay
  the slow forwarding path. The row's real purpose is that the numbers **exist at all** under AOT:
  the shells' predecessor (closing a generic conversion method with `MakeGenericMethod`) silently
  computed a *wrong checksum* under AOT. The shells produce the correct one, and the runner's
  Verify phase requires that before anything is timed.

### History

> **Note:** when the toolchain moves (e.g. .NET 9 → .NET 10), copy the current results block into
> this section with its environment line before re-running `--update-readme`, so
> version-over-version comparisons accumulate here.

**Captured 2026-07-26 -- the PRE-FIX baseline for the `Iface` root-cause arc.** Same toolchain as the
current table; preserved because the `Iface` row moved by more than two orders of magnitude and the
numbers it was published with are the measurement being explained. `Iface` at **158.24× (JIT) /
660.42× (AOT)** is the row that was marked OPEN; both figures are dominated by a single uncached
`GetCustomAttribute` call on golib's type-assert miss path (see *Reading the results* above). Every
other row here is within run-to-run variance of the current table, which is what attributes the
change to the fix rather than to machine state:

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

