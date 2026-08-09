![go2cs](images/go2cs-small.png)

# 📰 go2cs News Archive

All project announcements, newest first. The [README](README.md) summarizes where the project stands
today; every announcement is recorded here, and the detail-heavy ones link to a companion page carrying
their full text.

---

## August 8, 2026 — Go programs run on Linux

Converted Go programs now **run on Linux, byte-identical to `go run`**: `fmt.Println("hello, 世界")`,
a program crossing `os.Args`, `os.Getenv` and `time.Now()`, and the README's own
[real-world walkthrough](README.md#converting-a-real-world-module) — `fatih/color` printing true ANSI
colour under a real PTY, with the `isatty` branch agreeing with Go in both directions (plain when piped,
coloured on a terminal). The whole campaign landed in one continuous arc: the repository checks out
deterministically on any filesystem, the converter and every harness instrument run natively on Linux,
the standard library compiles for **windows, linux and darwin from one tree** (per-GOOS source folders
selected by `$(GoTargetOS)`, windows the default, with 141 of 141 shared-source packages measured
IL-identical across flavors), and each Go package ships as **one NuGet package** carrying RID-specific
assemblies only where source genuinely varies.

At the bottom of it all sits **one measured keystone**: Linux's entire syscall surface crosses the
kernel through a single `libc syscall(2)` binding whose three claims were probed rather than argued —
the variadic ABI with a real six-argument `mmap`, the second return register shown to be *exact* (the
kernel preserves `RDX`), and errno round-tripped through a deliberate fault. The road there surfaced
exactly two converter defect families and a handful of linkname wiring gaps — including the lesson that
a forward alone can *pass a run and be wrong* (`os.Args` silently empty), which is why every wiring row
now pairs with its populated truth. The FFI surface simultaneously converged on source-generated
`[LibraryImport]` bindings, where non-blittable signatures fail at **compile time** — three latent
marshalling hazards surfaced during the migration, each converted explicitly.

Stated plainly: the published `1.23.1.4` packages still carry Windows-only assemblies — the Linux
experience ships with the next release; a Linux consumer of the few platform-divergent packages also
needs the compile-surface answer scheduled next; and darwin binaries are compile-proven and
IL-identity-backed but have never been executed here. The Windows lane did not move a byte through any
of it: every merge held CNR byte-identical, the behavioral suite green, and the 110-package validated
sweep at 13,628 verdicts with zero failures.

## August 8, 2026 — Over half the standard library validates; defers reach zero allocation

Three days took the validated roster from 73 packages to **110 of 215 (51.2%)** — 13,628 matching
verdicts against `go test`, 50 disclosed divergences, not one added in the climb. The largest single
advance was also the quietest: a re-scout of never-measured packages found **34 that validated with no
changes at all** — the corpus had grown past them as shared machinery landed. The loudest was
architectural: **`defer` now compiles to an inline `try`/`catch`/`finally` over a `ref struct` frame** —
no closure object, no delegate per defer, zero allocation for non-capturing defers — replacing the
execution-context lambda while *improving* behavioral fidelity (a capture-semantics divergence class
died by construction). Three runtime capabilities were hand-implemented on managed primitives following
the established pattern: Go's concurrent hash-trie map, weak pointers, and caller-frame walks. And three
user-reported `-recurse` failures — a quoted `go.mod` directive, a C# keyword inside an import path, and
a type renderer chasing its own tail through `<-chan` — were each root-caused, fixed, and answered on
the issue the same day, ending with `gopkg.in/yaml.v3` converting, compiling, and running byte-identical
to `go run`.

## July 26, 2026 — More than a quarter of the standard library's test suites pass in C#

**57 of the 215 testable standard-library packages validated their own Go test suites in C# — 26.5%
of the Phase-4 target**, at **1,459 matching test results** with **47** honestly disclosed as
divergent rather than quietly dropped. A package counted only when *every* `Test` function's verdict
matched a clean `go test -json -count=1` baseline. The set moved well past leaf packages: `sync`'s own
concurrency suite, the RE2 engine in `regexp`, `strconv`'s float formatting, the `crypto/sha*` family,
and the reflection-driven `errors`, `encoding/binary` and `go/token` — the last round-tripping a
`FileSet` through the real converted `encoding/gob`. The same push made goroutine exit, string-literal
allocation and structural interface assertion behave the way Go specifies, and corrected two published
performance figures that had been measuring the wrong artifact. Per-package counts are in
[Validated Test Packages](ValidatedTestPackages.md); the measured numbers in [Performance](Performance.md).

*Full story: [More than a quarter of the standard library's test suites pass in
C#](news/2026-07-26-quarter-of-stdlib-tests-pass.md) · commit `44fcc4f04` · reproduce any row from a
clone via [Try it yourself](README.md#try-it-yourself--validate-a-converted-test-suite)*

---

## July 18, 2026 — `unicode/utf16` validates; disclosed-divergence generalizes

**Phase-4 package #5.** [`unicode/utf16`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/unicode/utf16)
validated its own Go test suite in C# — **8 tests agreeing outright** against `go test -json`, plus one
honestly disclosed. The structural twin of the very first validated package (`unicode/utf8`), it
round-trips UTF-16 encode/decode with results checked by `reflect.DeepEqual`, exercised through the
converted reflection bridge. Its significance was the ninth test: `TestAllocationsDecode` asserts a
zero-allocation `Decode`, which Go reaches only through compiler escape analysis and a managed runtime
provably cannot match. `unicode/utf16` was the first package to reuse the disclosed-divergence manifest
as a **general tool** rather than a two-package special case, pinning that one `alloc-profile` row by
exact failure signature while a separate test proved the decoded output correct — a mechanism that
generalizes cleanly is a mechanism that was designed right.

*Full story: [`unicode/utf16` validates; disclosed-divergence
generalizes](news/2026-07-18-utf16-validates.md) · Phase-4 package #5 · 8 + 1 disclosed (alloc-profile)
· reproduce from a clone via [Try it yourself](README.md#try-it-yourself--validate-a-converted-test-suite)*

---

## July 18, 2026 — `bytes` and `strings` tests pass, with disclosed-divergence

**Two more standard-library packages validated their own Go test suites in C#** — and they arrived with
a new piece of Phase-4 machinery. [`bytes`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/bytes)
validated **81 tests** and [`strings`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/strings)
**68** against `go test -json`, bringing the Phase-4-validated count to four after `unicode/utf8` and
`sort`. Both contain tests that assert an **exact allocation count** via `testing.AllocsPerRun` —
unsatisfiable by design in a managed runtime, where the divergence is the allocation *model*, not the
measurement. Rather than silently skip them, go2cs began disclosing them at test level: a hand-owned,
repo-committed `go2cs_test_disclosures.json` pins `{test, divergence class, expected failure
signature}`, and the differential oracle reclassifies a result only when both the test name and the
pinned signature match — an integrity guard, not a blanket exemption. Packages without a manifest
compare strictly.

*Full story: [`bytes` and `strings` tests pass, with
disclosed-divergence](news/2026-07-18-bytes-strings-disclosed-divergence.md) · Phase-4 packages #3 and
#4 · `sort` 63/63, `bytes` 81, `strings` 68 · reproduce from a clone via
[Try it yourself](README.md#try-it-yourself--validate-a-converted-test-suite)*

---

## July 17, 2026 — Go's own tests now pass in C#

**A standard-library package's own Go test suite — converted to C# — ran and agreed with `go test`,
verdict for verdict.** [`unicode/utf8`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/unicode/utf8)'s
real test suite (Go 1.23.1) validated **14/14** through the new converted-test pipeline: the `_test.go`
files are transpiled to C#, built against the converted standard library, executed under a Go-semantics
test host, and differentially compared against a clean `go test -json` baseline by full test name — with
every benchmark and example declaration honestly disclosed rather than silently skipped. One week after
"the whole standard library *compiles*," the answer to *"but does it **run**?"* had its first
machine-checked proof, [reproducible from a clone](README.md#try-it-yourself--validate-a-converted-test-suite).
This opened the Phase 4 operational era — **real Go tests, not compilation, are the currency of
correctness** — with `sort`, `strings` and `bytes` next in line.

*Tag: [`utf8-tests-green-2026-07-17`](https://github.com/ritchiecarroll/go2cs/releases/tag/utf8-tests-green-2026-07-17)
· commit `337a928df`*

---

## July 14, 2026 — The converted Go standard library is on NuGet

**The converted Go standard library, the `golib` runtime, and the `go2cs-gen` analyzer are published to
[nuget.org](https://www.nuget.org/packages?q=go2cs%20ritchiecarroll)** as `go.<pkg>` /
[`go.lib`](https://www.nuget.org/packages/go.lib) / [`go.gen`](https://www.nuget.org/packages/go.gen),
versioned `1.23.1.<build>` from `src/version.props`. The converter's new `-recurse=nuget` mode emits
matching `<PackageReference>` entries — defaulting `$(GoStdLibVersion)` to a floating release — so a
converted end-user app or library restores the whole go2cs stack from NuGet with **no local go2cs source
checkout**; the app's own and third-party converted packages stay project references. See
[Converting a real-world module](README.md#converting-a-real-world-module) for the end-to-end walkthrough.

*Tag: [`nuget-stdlib-2026-07-14`](https://github.com/ritchiecarroll/go2cs/releases/tag/nuget-stdlib-2026-07-14)
· commits `2363af0e6`, `2e15eec9d`, `dd821a556`*

---

## July 10, 2026 — The entire Go standard library compiles in .NET

**All 302 packages of the auto-converted Go standard library (Go 1.23.1) compile
cleanly as .NET assemblies — zero errors, zero exclusions.** Every package you'd expect to be hard is
in that number: `runtime`, `reflect`, `net/http`, `go/types`, `crypto/tls`, `database/sql`,
`encoding/json`. The transpiled output is not a demo subset — it is the standard library, end to end,
emitted by the converter, transpiled Go to C#, then compiled by Roslyn. NOTE: don't get _too_ excited,
this is _fully compilable_ not _fully runnable_ — that's the next phase (underway; see the July 17 item
above)! However, simple apps will run, try
[converting a real-world module](README.md#converting-a-real-world-module). Read more about this
[milestone's details](StdLibCompileMilestone.md) and
[current status](README.md#status) in the README.

*Tag: [`stdlib-green-2026-07-10`](https://github.com/ritchiecarroll/go2cs/releases/tag/stdlib-green-2026-07-10)
· commit `51ba5d9cf`*

---

## June 27, 2026 — The `math` package compiles clean

The full-conversion **`math` package compiles clean** — a core, widely-imported standard-library
package, and with it a major step in the Phase 3 drive to compile the whole auto-converted standard
library. The session that landed it greened nine full-conversion packages (`unicode`,
`internal/trace/event`, `unicode/utf16`, `internal/platform`, `image/color`, `runtime/internal/sys`,
`runtime/internal/math`, `math/bits`, and `math`) via 19 behaviorally-tested converter and generator
fixes. The dominant theme was comprehensive untyped-constant typing, plus shadowing fixes,
namespace-collision qualification, composite self-qualification, and relational-pattern guards.

*Tag: [`math-green-2026-06-27`](https://github.com/ritchiecarroll/go2cs/releases/tag/math-green-2026-06-27)
· commit `914d4bd72`*

---

## May 5, 2025 — First full standard-library auto-conversion

The rewritten Go-based converter completed its **first full standard-library auto-conversion**: the
whole Go standard library (~301 projects) converted end to end. "Converted" here meant the transpiler
did not crash and every Go source file received a corresponding C# file — not yet that the emitted C#
compiles. Driving this full conversion to a clean compile became the Phase 3 campaign, finished on
July 10, 2026 (above).

*Tag: [`full-conversion-2025-05`](https://github.com/ritchiecarroll/go2cs/releases/tag/full-conversion-2025-05)
(`cc14584c7`, May 11) · commit `6ca1c45b7`*

---

## January 12, 2025 — The converter is rewritten in Go ("go2cs" version 2)

**Major project restructuring** — the "go2cs iteration 2" generation begins: the converter is
re-implemented **in Go** on the official `go/ast` + `go/types` toolchain, replacing the original C#
converter built on an ANTLR4 Go grammar; T4 templates are replaced by raw string literals; and Roslyn
source generators take over the auto-generated ancillary code that supplies Go semantics at compile
time. The ANTLR4/C# converter is retired.

*Commit: `87465f5f5`*

---

## November 19, 2022 — .NET 7.0, C# 11, and UTF-8 string literals

From the ANTLR4-era converter's News:

* Project has been updated to use .NET 7.0 / C# 11.
* String literals are encoded using UTF-8 (C# `u8` string suffix) which uses the `ReadOnlySpan<byte>`
  ref struct. This should make Go strings faster since strings do not have to be converted to UTF-8
  from UTF-16. Also added an experimental
  [`sstring`](https://github.com/ritchiecarroll/go2cs/blob/master/src/core/golib/sstring.cs),
  a ref struct implementation of a Go string.
* Code conversions now better match original Go code styling.

*Commit: `d90f267d4`*

---

## March 13, 2022 — `v0.1.2` release

**go2cs `v0.1.2` is released** — a tagged release of the mature ANTLR4-era converter. Converted code
now targets **.NET 6.0 / C# 10**, using file-scoped namespaces and reduced indentation to better match
the original Go code's styling, with new command-line options for pre-C#-10-compatible output and ANSI
brace style, options to skip GOOS/GOARCH- and cgo-targeted files, and the ANTLR4 grammar synchronized
to the official source.

*Tag: [`v0.1.2`](https://github.com/ritchiecarroll/go2cs/releases/tag/v0.1.2) (`289b939db`)*

---

## January 5, 2021 — Go as a scripting language for Unity and Godot

Example usages of go2cs allow [Go](https://golang.org/ref/spec) to serve as the **scripting language
for the [Unity](https://unity.com/) and [Godot](https://godotengine.org/) game-engine platforms** —
see the [GoUnity](https://github.com/ritchiecarroll/GoUnity) and
[GodotGo](https://github.com/ritchiecarroll/GodotGo) projects. The project has also been updated to
**.NET 5.0** and supports
[publishing as a self-contained executable](https://docs.microsoft.com/en-us/dotnet/core/deploying/#publish-self-contained).

*Commits: `efb497b3a`, `e5c2d7cbc`*

---

## August 29, 2020 — First full conversion of the Go standard library (ANTLR4 era)

The initial conversion of the **full Go source library** completed without failing — the converter's first end-to-end pass over the entire standard library, committed to `src/go-src-converted`.
The warnings in that conversion's build log laid out the road map of the parsing and conversion work
remaining. Converted code at the time targeted .NET Core 3.1 / C# 8.0, and simple conversions depended
on `src/gocore` — the small, manually-converted subset of the Go library that survives today as the
curated baseline in `src/core`.

*Commit: `8e2d6e8e6`*
