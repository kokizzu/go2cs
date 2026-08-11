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

**Environment:** Intel(R) Core(TM) i7-5820K CPU @ 3.30GHz · Microsoft Windows 10.0.26100 · go1.23.1 · .NET SDK 9.0.316 · 2026-08-11

C# builds: JIT = framework-dependent `Release`; Native AOT = `-p:PublishAot=true` self-contained, partial trim. Median of 5 runs (1 discarded warmup). Workload time is measured in-program and excludes process startup; the Startup row is pure process wall time. Ratios are relative to Go.

**Execution time** (milliseconds -- lower is better):

| Benchmark | Go | C# (JIT) | C# (Native AOT) |
|---|---:|---:|---:|
| Startup | 23.4 | 293.1 (12.51×) | 80.8 (3.45×) |
| Fib | 133.1 | 159.0 (1.19×) | 126.9 (0.95×) |
| Sieve | 122.2 | 158.4 (1.30×) | 380.3 (3.11×) |
| MatMul | 150.1 | 305.1 (2.03×) | 507.3 (3.38×) |
| String | 165.1 | 997.6 (6.04×) | 1,247.7 (7.55×) |
| StringView | 30.7 | 28.3 (0.92×) | 25.9 (0.84×) |
| StringMatch | 552.6 | 1,398.7 (2.53×) | 1,857.3 (3.36×) |
| Map | 596.5 | 567.4 (0.95×) | 240.3 (0.40×) |
| Sort | 159.1 | 437.7 (2.75×) | 415.0 (2.61×) |
| Channel | 105.7 | 97.3 (0.92×) | 92.2 (0.87×) |
| IfaceCall | 257.3 | 761.4 (2.96×) | 743.0 (2.89×) |
| Iface | 123.7 | 859.8 (6.95×) | 603.9 (4.88×) |
| IfaceShell | 29.4 | 941.8 (32.05×) | 1,474.9 (50.19×) |

**Peak memory** (working set, MB -- lower is better):

| Benchmark | Go | C# (JIT) | C# (Native AOT) |
|---|---:|---:|---:|
| Startup | 3.8 | 43.9 | 77.9 |
| Fib | 5.3 | 45.5 | 78.1 |
| Sieve | 34.6 | 65.1 | 97.7 |
| MatMul | 10.5 | 51.9 | 84.4 |
| String | 5.2 | 54.1 | 86.4 |
| StringView | 5.3 | 45.6 | 78.1 |
| StringMatch | 5.2 | 53.8 | 87.7 |
| Map | 158.3 | 164.3 | 196.6 |
| Sort | 21.4 | 70.3 | 102.0 |
| Channel | 5.2 | 50.1 | 81.4 |
| IfaceCall | 5.2 | 44.9 | 78.2 |
| Iface | 5.3 | 45.2 | 78.4 |
| IfaceShell | 5.3 | 65.2 | 96.8 |

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
- **Function calls / integers (Fib):** the closest workload — the JIT runs within ~20% of Go and
  **Native AOT edges past Go outright**.
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
  real unbuffered rendezvous, single-fire select, operand-once hoisting. **Runs at parity with Go
  or better in both C# variants** on this producer→consumer churn.
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
