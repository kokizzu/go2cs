![go2cs](images/go2cs-small.png)

# 📰 go2cs News Archive

All project announcements, newest first. The latest item is always
summarized at the top of the [README](README.md), full text kept here.

---

## July 26, 2026 — More than a quarter of the standard library's test suites pass in C#

**57 of the 215 testable standard-library packages now validate their own Go test suites in C# —
26.5% of the Phase-4 target.** The measure is unchanged since the first package passed nine days ago:
a package counts only when *every* `Test` function's verdict matches a clean `go test -json -count=1`
baseline, compared by full Go test name. The current total is **1,459 matching test results** across
the set, with **47** results disclosed as divergent rather than quietly dropped. The whole table, and
the single command that reproduces any row from a clone, is in
[Validated Test Packages](ValidatedTestPackages.md).

The mechanics have not moved because they did not need to. Each package's `_test.go` files are
transpiled to C#, built against the converted standard library, and run in an isolated process under
the hand-owned Go-semantics test host; the differential oracle then diffs the host's terminal results
against the `go test` baseline. A package that *almost* passes does not appear on the table. Where a
Go test asserts something a managed runtime provably cannot satisfy — an exact allocation count via
`testing.AllocsPerRun`, which Go reaches through compiler escape analysis, or a collectibility check
Go answers from per-safepoint liveness maps — the package carries a hand-owned, repo-committed
`go2cs_test_disclosures.json` pinning `{test, divergence class, expected failure signature}`. The
oracle reclassifies such a result only when the test name *and* the failure signature both match; a
disclosed test that fails any other way is still a hard mismatch, and packages without a manifest
compare strictly. That mechanism was built for two packages and has since carried 47 rows across the
corpus without being loosened once.

**The concurrency runtime became real, and `sync` validates.** Go's `Mutex` is a state machine over an
`int32` co-designed with the runtime's sleeping semaphore — starvation-mode ownership is handed to one
*specific* waiter with exact ticket semantics — and no .NET primitive reproduces that; an emulated
semaphore trips "inconsistent mutex state" under sustained contention. So `Mutex`, `RWMutex`,
`WaitGroup` and `Pool` are hand-owned native implementations answering Go's *contract*. Beneath them,
the `//go:linkname` runtime primitives `sync` reaches for are a faithful port of Go's own `sema.go`: a
sleeping semaphore keyed by the address of the `uint32`, with the count living *in* the word the
pointer addresses exactly as in Go — which is what lets `sync`'s own `TestSemaphore` seed `*s = 1` and
expect the first acquire not to park — and a ticketed notify list where releasing ticket *t* reaches
only the waiter holding *t*, so a later arrival cannot steal a `NotifyOne` intended for someone else.
`sync.Cond` rides on that as the ordinary converted file; only its "must not be copied" checker is
hand-owned, because Go implements it by storing the checker's own address inside itself and an address
is meaningless under a moving collector — the managed form asks the same question against reference
identity instead. `sync.Pool` shows the same discipline in miniature: Go's lock-free `poolDequeue`
ring is kept whole — packed head/tail, fullness test, CAS protocol — and the fork is confined to the
one thing that cannot survive the crossing. Go's ring hangs its ownership protocol on an `eface`
slot's *type word*, and a CLR `any` is a single reference, so that word had been doing double duty as
type tag and value; the slot becomes one managed reference with null as the empty sentinel.

Goroutines learned to leave properly, too. `runtime.Goexit` now has the shape Go actually specifies: a
dedicated `GoexitException` that is deliberately *not* a `PanicException`, so the goroutine's deferred
calls run during the unwind while `recover()` stays blind to it — the distinction `sync.OnceFunc` and
`testing.FailNow` both depend on. And a goroutine that crashes now fails one test instead of taking
down the whole run, which had been turning single defects into what looked like package-wide
infrastructure walls. Several long-standing `golib` defects fell out of this work rather than out of
inspection: a Go map must accept a `nil` key and give it a slot of its own, and a pointer *to* nil is
not the nil pointer — `atomic.Pointer` had been quietly eating one, which is why `sync.Map`'s
expunge protocol degenerated.

**The reflection bridge reached the packages that actually use reflection.** `errors` (61 tests)
exercises `Value.Set` and addressability; `encoding/binary` (137) drives reflective construction and
write-back; `testing/quick` (8) generates values and invokes them through `Value.Call`;
`internal/fmtsort` (3) is `fmt`'s own map-key ordering, which needs `Value.Convert` and arithmetically
ordered pointer and channel tokens. The one worth singling out is `go/token` (31): its tests serialize
a `FileSet` through `encoding/gob` and read it back, which means the converted gob `Encoder` and
`Decoder` — the real engines, not a stub — are driving the converted reflect type-relation mirrors
end to end, in C#. A reflection layer that satisfies gob is a reflection layer that satisfies most
things. (Gob's *own* test suite is not yet validated; that remains open.)

**What the campaign keeps finding is not cosmetic.** Every package on the table arrived by way of
defects that compiled perfectly and then behaved wrongly, and the fixes are general rather than local.
A Go struct carrying a fixed-size array field copies that array *inline*; the emitted `array<T>` is a
struct over a shared backing, so a C# struct copy shared it — which is why `crypto/sha256`'s `Sum`,
whose whole purpose is to finalize a copy while the caller keeps writing the original, destroyed the
caller's state and returned the hash of the empty string. A Go constant of a named, string, complex or
untyped type has no C# `const` form and becomes a `static readonly` field, so it carries the same
cross-file initialization-order hazard a package variable does: `regexp/syntax`'s `opNames` table read
every one of its `Op` keys as zero and collapsed nineteen entries into slot 0. The `regexp` pair alone
exposed five such silent miscompiles — among them a blank result mixed with a named one silently
dropping the named-return-defer machinery, so a recovered panic returned the zero tuple and every
expression that must fail to parse reported success instead. Each was root-caused against the emitted
C#, fixed at its own layer, and locked in by a regression test.

**Go string literals live in RODATA, and now the converted C# does too.** A Go binary allocates
nothing for `return "true"`, `s == "true"`, or `HasPrefix(line, "//go:build")`; the converted C# was
paying a heap allocation — usually with a UTF-16→UTF-8 transcode — at *every evaluation* of those
sites, because each literal materialized a fresh backing array. A four-tier arc closed the gap. String
comparison against a literal now happens span-in-place, for `@string` and for named string types
alike, so comparisons allocate nothing at all, ever. Literals in `any`/interface slots render as `u8`
spans instead of transcoding. And a whole-package pre-pass hoists every value-materializing literal to
a single `static readonly` field placed immediately above its first consumer, pre-boxed when every use
in the package is an interface target — so a literal now costs at most one allocation per program run.
On the `StringMatch` benchmark, which exists precisely to measure these shapes, the JIT gap against Go
went from **11.75× to 9.16×** and Native AOT from **12.18× to 9.06×**, with the neighboring
`StringView`, `String` and `Map` rows flat.

**Structural interface satisfaction moved entirely to run time.** Go satisfies interfaces
structurally; C# does not, so the converter used to guess — it scanned each package for concrete types
that structurally matched an interface and pre-recorded a nominal adapter for every match it found.
The guess was incomplete by construction: a dynamic type living in an assembly converted *later* than
the asserting package was unreachable, so `io/fs` recorded `subFS` and could never see `os.dirFS`.
Every non-generic named interface now emits a pair of runtime "shells" into its own assembly instead —
a delegate-bound generic shell for pointer-sourced values and a reflective one for value-typed ones —
so an assertion resolves where the concrete type actually lives, with none of the recorder's blind
spots. Once the shells were in place the heuristic recorders had nothing left to do: they were retired
behind a default-off flag, proved inert against a full corpus reconvert and the behavioral suite, then
deleted — about 495 lines of guessing. The rule is now simply that a record is written only for a
conversion the source *declares*, and structural satisfaction is resolved at run time.

**One of our published performance numbers was wrong, and this is how.** `IfaceShell` is the benchmark
that measures exactly those shells — runtime duck-typed assertion, the one Go operation C# has no
native answer for — and it was published at **189×** the Go time on the JIT. It was not measuring a JIT
binary. The converter's csproj template pinned `$(OutDir)` to `bin\$(Configuration)\$(TargetFramework)\`,
and MSBuild copies build outputs to `$(OutDir)`, not `$(OutputPath)` — so the perf suite's
`$(BaseOutputPath)` isolation for the Native AOT publish had never once taken effect. The AOT publish's
build step wrote its self-contained, dynamic-code-disabled binary straight over the JIT binary, and the
Measure phase timed *that* and labelled the column "JIT". Worse, the damage was sticky: MSBuild's
incremental check then saw the outputs up to date, so later JIT-only builds did not repair it and the
error survived every re-measure. The same single line silently defeated two other isolation intents —
every converted Phase-4 test host had been writing into its production package's output directory, and
any end-user project setting `BaseOutputPath` was ignored the same way — so the fix went into the
templates, which now defer to an explicit `$(BaseOutputPath)`. Correcting the configuration alone took
the row from ~189× to ~59×; a Go-style per-interface itab cache, a monomorphic slot in front of it, and
arity-based invoker dispatch took it the rest of the way to the **39.83×** now published. The durable
part is not the number but the guard: the performance runner now inspects each JIT binary's
`runtimeconfig.json` before timing it and **fails the run** on a self-contained or
dynamic-code-disabled binary rather than quietly shipping a figure. On its first outing it rejected all
eleven stale binaries, exactly the condition it was built to catch. A benchmark suite that can silently
measure the wrong artifact is not a measurement instrument, and the project's standing rule is that a
false green must be made impossible rather than noticed.

**Where performance actually stands.** The honest table, measured on one machine across Go, C# JIT and
C# Native AOT, is in [Performance](Performance.md). Native AOT startup is within a few percent of Go
(1.16×); recursive integer work is 1.23× on the JIT and 1.10× under AOT; `map` is *faster* than Go
(0.85× on the JIT, 0.32× under AOT, riding .NET's `Dictionary`). Channels are 2.03×, down from 3.39×
before the channels redesign brought real unbuffered rendezvous and single-fire select. The
`[]byte`→`string` round-trip remains the largest honest gap in ordinary code at 10.66×. The three
interface rows now separate the way the design says they should: pure method dispatch through an
interface value costs 2.79× (2.46× AOT), ordinary Go interface code — dispatch, a known-type comma-ok
assertion, a closed type switch — costs 5.84× (4.18×), and only runtime *structural* assertion reaches
39.83×. That last row is the price of a capability C# does not have, and ordinary interface use never
touches it. The 5.84× row is itself a correction: it was published at **158×**, a real and reproducible
figure that turned out to be measuring an uncached `GetCustomAttribute` call on `golib`'s type-assert
miss path rather than the cost of the construct — memoizing that answer and gating one marker probe
took it to where it is, and the pre-fix table is archived beside the current one rather than discarded.
These are the numbers as measured; none of them are projections. Compiling was the Phase-3 milestone
and running is the Phase-4 one — a quarter of the way through it, the thing being proved has not
changed: real Go tests, not compilation, are the currency of correctness.

*Phase-4 progress: 57 / 215 testable packages (26.5%) · 1,459 matching verdicts · 47 disclosed ·
commit `44fcc4f04` · reproduce any row from a clone via
[Try it yourself](README.md#try-it-yourself--validate-a-converted-test-suite)*

---

## July 18, 2026 — `unicode/utf16` validates; disclosed-divergence generalizes

**Phase-4 package #5.** [`unicode/utf16`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/unicode/utf16)
validates its own Go test suite in C# — **8 tests agreeing outright** against `go test -json`, plus one
honestly disclosed. The structural twin of the very first validated package (`unicode/utf8`), it round-trips
UTF-16 encode/decode with results checked by `reflect.DeepEqual` — exercised here through the converted
reflection bridge — and all eight correctness tests match verdict for verdict.

Its significance is what the ninth test demonstrates. `TestAllocationsDecode` asserts that `Decode` returns
its `[]rune` with **zero** heap allocations — a result Go reaches only through compiler escape analysis, so
the test guards itself with `testenv.SkipIfOptimizationOff`. The managed runtime provably cannot match it: a
returned `slice<rune>` is always a heap allocation, no matter how the method is written. This is the same
*allocation-model* divergence `bytes` and `strings` disclosed a day earlier — and `unicode/utf16` is the
first package to reuse the [disclosed-divergence manifest](README.md#try-it-yourself--validate-a-converted-test-suite)
as a **general tool** rather than a two-package special case. Its `go2cs_test_disclosures.json` pins one
`alloc-profile` row by exact failure signature (`"Decode allocated "`), while the separate `TestDecode`
independently proves the decoded output is correct — so the disclosure covers exactly the allocation
profile and nothing else. A mechanism that generalizes cleanly to the next package is a mechanism that was
designed right.

*Phase-4 package #5 · `unicode/utf16` 8 + 1 disclosed (alloc-profile) · reproduce from a clone via
[Try it yourself](README.md#try-it-yourself--validate-a-converted-test-suite)*

---

## July 18, 2026 — `bytes` and `strings` tests pass, with disclosed-divergence

**Two more standard-library packages validate their own Go test suites in C#** — and they arrive with a
new piece of Phase-4 machinery. [`bytes`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/bytes)
validates **81 tests** and [`strings`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/strings)
**68** against `go test -json`, bringing the count of Phase-4-validated packages to four (after
`unicode/utf8` and `sort`).

Both packages contain a handful of tests that assert an **exact allocation count** via Go's
`testing.AllocsPerRun` — for example, `strings`'s `TestBuilderAllocs` insists a `Builder` heap-allocates
*exactly once*. These are unsatisfiable by design in a managed runtime: the CLR has no malloc counter (the
shim measures allocated *bytes* instead), and .NET genuinely allocates where Go's escape analysis
stack-allocates — an addressed `var b Builder` heap-boxes per run; `string(r)` materializes a `byte[]`
where Go uses a 4-byte stack buffer. A malloc-counting shim would fail these identically; the divergence
is the allocation *model*, not the measurement.

Rather than silently skip them, go2cs now discloses them at test level. Each affected package carries a
hand-owned, repo-committed `go2cs_test_disclosures.json` — reviewed like source, never generated — that
pins `{test, divergence class, expected failure signature}`. The differential oracle reclassifies a
Go-passes/C#-fails result as **disclosed-divergent** *only* when both the test name and the pinned failure
signature match; a disclosed test that fails any *other* way is still a hard mismatch, so the pin is an
integrity guard, not a blanket exemption. The validation summary reports the reclassified rows explicitly
(`… 7 disclosed-divergent (alloc-profile), …`). Packages without a manifest — `sort` and `utf8` —
compare strictly and are wholly unaffected.

*Phase-4 packages #3 and #4 · `sort` 63/63, `bytes` 81, `strings` 68 · reproduce from a clone via
[Try it yourself](README.md#try-it-yourself--validate-a-converted-test-suite)*

---

## July 17, 2026 — Go's own tests now pass in C#

**A standard-library package's own Go test suite — converted to C# — now runs and agrees with `go test`,
verdict for verdict.** [`unicode/utf8`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/unicode/utf8)'s
real test suite (Go 1.23.1) validates **14/14** through the new converted-test pipeline: the `_test.go`
files are transpiled to C#, built against the converted standard library, executed under a Go-semantics
test host, and differentially compared against a clean `go test -json` baseline by full test name — with
every benchmark and example declaration honestly disclosed rather than silently skipped. One week after
"the whole standard library *compiles*," the answer to *"but does it **run**?"* has its first
machine-checked proof — and it's one you can
[reproduce yourself from a clone](README.md#try-it-yourself--validate-a-converted-test-suite)
(tag: `utf8-tests-green-2026-07-17`). This is the Phase 4 operational era: **real Go tests, not
compilation, are now the currency of correctness** — with `sort`, `strings`, and `bytes` next in line.

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
[milestone's details](README.md#about-standard-library-compile-milestone) and
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
