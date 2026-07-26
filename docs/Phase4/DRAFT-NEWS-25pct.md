# DRAFT — NEWS item for the 25%-validated milestone

> **Status: DRAFT. Not published, not linked from anywhere.** Prepared 2026-07-26 with the corpus at
> **47 / 215 testable packages (21.9%), 1,133 matching verdicts, 22 disclosed**. It is written *for*
> the moment the campaign crosses **25%** — 54 of 215 packages — so every count is a placeholder in
> `[BRACKETS]`. Fill them from the sources listed in §(c), then publish §(a) into
> [`README.md`](../README.md) and §(b) into [`NEWS.md`](../NEWS.md) per the existing convention.
> Delete this file once published.
>
> Every performance number below is quoted from the committed table in
> [`src/Tests/Performance/README.md`](../../src/Tests/Performance/README.md) (live block or an
> archived *History* block). Nothing is projected. Facts that could not be verified in-repo are
> listed at the end of §(c) rather than asserted in the prose.

---

## (a) README summary entry

Replaces the current `## 📰 NEWS — …` block at the top of [`docs/README.md`](../README.md)
(keep the trailing "➡ All announcements" line exactly as it is).

```markdown
## 📰 NEWS — [PUBLISH-DATE]: A quarter of the standard library's test suites pass in C#

**25% of the Go standard library's package test suites now validate.** [FINAL-COUNT] of the 215
converted standard-library packages that define `Test` functions run their own Go 1.23.1 test suites
in C# and agree with `go test -json` verdict for verdict — **[FINAL-VERDICTS] matching test results**,
with [FINAL-DISCLOSED] divergences disclosed by exact failure signature rather than skipped. The set
now reaches well past leaf packages: the reflection-driven ones (`encoding/binary`, `errors`,
`internal/fmtsort`, `go/token` — which round-trips a `FileSet` through the real converted
`encoding/gob` engines), the concurrent ones, and `hash/crc32` running genuine SSE4.2/PCLMULQDQ
hardware paths through managed intrinsics. Full per-package counts, and a one-command reproduction
from a clone, are in the [validated-package table](ValidatedTestPackages.md).

**➡ All announcements can be found in the [go2cs News Archive](NEWS.md).**
```

---

## (b) NEWS.md detailed entry

Insert directly under the `---` that follows the archive's header paragraph, above the
July 18, 2026 entry (newest first).

Nine paragraphs, longer than a routine archive entry because it covers a quarter-milestone rather than
a package. If you want it shorter, the two most cuttable are the **second** (validation mechanics —
already told in the July 17 and July 18 entries) and the **structural-interface** one; the
measurement-defect paragraph is the one to keep whatever else goes.

```markdown
## [PUBLISH-DATE] — A quarter of the standard library's test suites pass in C#

**[FINAL-COUNT] of the 215 testable standard-library packages now validate their own Go test suites
in C# — 25% of the Phase-4 target.** The measure is unchanged since the first package passed in July:
a package counts only when *every* `Test` function's verdict matches a clean `go test -json -count=1`
baseline, compared by full Go test name. The current total is **[FINAL-VERDICTS] matching test
results** across the set, with **[FINAL-DISCLOSED]** results disclosed as divergent rather than
quietly dropped. The whole table, and the single command that reproduces any row from a clone, is in
[Validated Test Packages](ValidatedTestPackages.md).

The mechanics have not moved because they did not need to. Each package's `_test.go` files are
transpiled to C#, built against the converted standard library, and run in an isolated process under
the hand-owned Go-semantics test host; the differential oracle then diffs the host's terminal results
against the `go test` baseline. A package that *almost* passes does not appear on the table. Where a
Go test asserts something a managed runtime provably cannot satisfy — exact allocation counts via
`testing.AllocsPerRun`, which Go reaches through compiler escape analysis — the package carries a
hand-owned, repo-committed `go2cs_test_disclosures.json` pinning `{test, divergence class, expected
failure signature}`. The oracle reclassifies such a result only when the test name *and* the failure
signature both match; a disclosed test that fails any other way is still a hard mismatch, and packages
without a manifest compare strictly. That mechanism was built for two packages and has since carried
[FINAL-DISCLOSED] rows across the corpus without being loosened once.

**The concurrency runtime became real.** `sync` splits cleanly in two, and the split is the
interesting part. Go's `Mutex` is a state machine over an `int32` co-designed with the runtime's
sleeping semaphore — starvation-mode ownership is handed to one *specific* waiter with exact ticket
semantics — and no .NET primitive reproduces that; an emulated semaphore trips "inconsistent mutex
state" under sustained contention. So `Mutex`, `RWMutex`, `WaitGroup` and `Pool` are hand-owned native
implementations answering Go's *contract*. But the substrate underneath them is now a faithful port of
Go's own `sema.go`: a sleeping semaphore keyed by the address of the `uint32`, with the count living
*in* the word the pointer addresses exactly as in Go — which is what lets `sync`'s own `TestSemaphore`
seed `*s = 1` and expect the first acquire not to park — and a ticketed notify list where releasing
ticket *t* reaches only the waiter holding *t*, so a later arrival cannot steal a `NotifyOne` intended
for someone else. `sync.Cond` needs no hand-owning at all on top of that; it is the ordinary converted
file. `sync.Pool` shows the same discipline in miniature: Go's lock-free `poolDequeue` ring is kept
whole — packed head/tail, fullness test, CAS protocol — and the fork is confined to the one thing that
cannot survive the crossing. Go's ring hangs its ownership protocol on an `eface` slot's *type word*,
and a CLR `any` is a single reference, so that word had been doing double duty as type tag and value;
the slot becomes one nullable reference with an explicit empty sentinel.

Goroutines learned to leave properly, too. `runtime.Goexit` now has the shape Go actually specifies: a
dedicated `GoexitException` that is deliberately *not* a `PanicException`, so the goroutine's deferred
calls run during the unwind while `recover()` stays blind to it — the distinction `sync.OnceFunc` and
`testing.FailNow` both depend on. And a goroutine that crashes now fails one test instead of taking
down the whole run, which had been turning single defects into what looked like package-wide
infrastructure walls. Several long-standing golib defects fell out of this work rather than out of
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
things.

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
went from **11.75× to 9.49×** and Native AOT from **12.18× to 9.36×**, with the neighbouring
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
build step wrote its self-contained,
dynamic-code-disabled binary straight over the JIT binary, and the Measure phase timed *that* and
labelled the column "JIT". Worse, the damage was sticky: MSBuild's incremental check then saw the
outputs up to date, so later JIT-only builds did not repair it and the error survived every re-measure.
The same single line silently defeated two other isolation intents — every converted Phase-4 test host
has been writing into its production package's output directory, and any end-user project setting
`BaseOutputPath` was ignored the same way — so the fix went into the templates, which now defer to an
explicit `$(BaseOutputPath)`. Correcting the configuration alone took the row from ~189× to ~59×; a
Go-style per-interface itab cache, a monomorphic slot in front of it, and arity-based invoker dispatch
took it the rest of the way to the **43.39×** now published. The durable part is not the number but the
guard: the performance runner now inspects each JIT binary's `runtimeconfig.json` before timing it and
**fails the run** on a self-contained or dynamic-code-disabled binary rather than quietly shipping a
figure. On its first outing it rejected all eleven stale binaries, exactly the condition it was built
to catch. A benchmark suite that can silently measure the wrong artifact is not a measurement
instrument, and the project's standing rule is that a false green must be made impossible rather than
noticed.

**Where performance actually stands.** The honest table, measured on one machine across Go, C# JIT and
C# Native AOT, is in [Performance](Performance.md). Native AOT startup is within a few percent of Go
(1.08×); recursive integer work is 1.23×; `map` is *faster* than Go (0.83× on the JIT, 0.29× under
AOT, riding .NET's `Dictionary`). Channels are 1.95×, down from 3.39× before the channels redesign
brought real unbuffered rendezvous and single-fire select. The `[]byte`→`string` round-trip remains the
largest honest gap at 10.73×, and structural interface assertion the largest of all at 43.39× — that
one is the price of a capability C# does not have, not a regression, and ordinary interface use never
reaches that path. These are the numbers as measured; none of them are projections. Compiling was the
Phase-3 milestone and running is the Phase-4 one — a quarter of the way through it, the thing being
proved has not changed: real Go tests, not compilation, are the currency of correctness.

*Phase-4 progress: [FINAL-COUNT] / 215 testable packages ([FINAL-PCT]) · [FINAL-VERDICTS] matching
verdicts · [FINAL-DISCLOSED] disclosed · reproduce any row from a clone via
[Try it yourself](README.md#try-it-yourself--validate-a-converted-test-suite)*
```

---

## (c) Fill-in checklist for the project owner

### Placeholders

| Placeholder | Fill with | Source of truth |
|:--|:--|:--|
| `[PUBLISH-DATE]` | Publication date, `Month D, YYYY` in NEWS.md; the README heading uses the same form | — |
| `[FINAL-COUNT]` | Validated package count (54 or more — 25% of 215 is 53.75) | `docs/ValidatedTestPackages.md` progress line; equals the table's row count |
| `[FINAL-PCT]` | The percentage, one decimal | Same progress line (`… — NN.N%`) |
| `[FINAL-VERDICTS]` | Total matching test verdicts | Same progress line; equals the sum of the table's **Tests** column (verified: at 47 packages the column sums to exactly 1,133) |
| `[FINAL-DISCLOSED]` | Total disclosed divergences | Same progress line; equals the sum of the table's **Disclosed** column (verified: 22 at 47 packages) |
| `[PACKAGES-SINCE-LAST-NEWS]` | *Optional* — not used in the prose above. If you want to name the additions since the July 18 entry, they are every table row except `bytes`, `strings`, `sort`, `unicode/utf8`, `unicode/utf16` | `docs/ValidatedTestPackages.md` |

The three counts are mutually checkable: the progress line, the row count, and the two column sums
must agree. If they do not, the table was edited without the progress line being refreshed.

### Publication steps

1. **`docs/README.md`** — replace the `## 📰 NEWS — …` block (currently the July 17 entry, at the top
   of the file) with §(a). Keep the `**➡ All announcements …**` line. There is no "move the old entry"
   step: the README carries only the newest summary and the full text already lives in the archive.
2. **`docs/NEWS.md`** — insert §(b) as a new top entry, newest first, with a `---` separator after it.
   The prior entry stays exactly where it is.
3. **`docs/README.md` → Milestones table** — the convention is a row per major turning point, linking
   the NEWS anchor. Add one if this qualifies in your judgement; the anchor form is
   `NEWS.md#<lowercased-hyphenated-heading>`.
4. **`docs/README.md` → Status section** — it currently says "**Fifteen** standard-library packages'
   own Go test suites now pass in C# — more than 450 of Go's own tests". That sentence is stale at 47
   and will be badly stale at 54; refresh it in the same commit.
5. **Tag**, if you tag this one — prior Phase-4 tags follow `<subject>-tests-green-<date>`.

### Facts NOT verified in-repo — confirm or drop before publishing

- **⚠ `sync` is not a validated package on `master`, and the draft deliberately never says it is.**
  This is the single biggest factual risk in the entry. As of this drafting, master's validated table
  has **no `sync` row** and master's package #47 is `compress/bzip2`. A `sync` validation (41/41 with
  10 disclosed, and a *new* `codegen-liveness` disclosure class) exists only on the **unmerged** branch
  `claude/r18-syncend`, which was cut before `compress/bzip2` landed and therefore numbers `sync` as
  its own #47. If that branch merges before you publish, the row set and the numbering both change —
  re-derive every count from the merged `ValidatedTestPackages.md` rather than adding the two trees
  together. The draft's concurrency paragraph describes the sync **work** only (all of it verifiable
  from committed code) and asserts **no sync test score**; keep it that way unless you are publishing
  from a tree where sync is actually on the table. Note also that no in-repo document tracks a running
  sync pass count — remembered figures like "39/50" appear only in commit bodies and are already stale.
- **The `encoding/gob` claim is deliberately narrow.** `go/token`'s tests drive the *real* converted
  gob `Encoder`/`Decoder` — that is confirmed (its `serialize_test.cs` calls `gob.NewEncoder`/
  `NewDecoder` directly, and `encoding/gob` is ~5,500 lines of converted C# with no hand-owned
  `_impl.cs` files). But **gob's own test suite is not validated**, and both the increment-3 commit and
  the reflection-bridge design say full gob validation remains open. The draft says gob's engines are
  *driven* end to end, never that gob passes. Do not upgrade that sentence.
- **"25%"** is exact only if the denominator is still 215. That denominator is the count of converted
  packages whose Go 1.23.1 sources define `Test` functions; it is stated in `ValidatedTestPackages.md`
  and should be re-derived, not assumed, if the converted package set has changed.
- **The `189×` figure.** The commit that corrected the row (`2defc1838`) and the design doc's §9 both
  cite **189.18× / 189.2×** as the published JIT figure at the time the defect was found. The archived
  *History* block in `src/Tests/Performance/README.md` shows **198.64×** for the same row, because that
  block is a later pre-arc re-measure of the *same* polluted binary. Both are honest; the draft says
  "published at 189×", which matches the correcting commit. If you prefer the archived figure, say
  "~190–199×" rather than mixing them.
- **`hash/crc32`'s hardware paths.** "Real SSE4.2/PCLMULQDQ hardware paths via managed intrinsics" is
  quoted from the `ValidatedTestPackages.md` row and backed by commit `50366f016`; it has not been
  independently re-confirmed against the emitted C# for this draft.

### Sources used

- `docs/ValidatedTestPackages.md` — counts, per-package one-line hooks
- `docs/NEWS.md`, `docs/README.md` — established voice, entry shape, publication convention
- `src/Tests/Performance/README.md` — every performance figure (live block + *History* blocks)
- `docs/Phase4/DESIGN-string-literal-allocation.md` — Tiers A / A′ / B / C, the RODATA framing
- `docs/Phase4/DESIGN-iface-shell-caching.md` §10 — the measurement defect, diagnosed and measured
- `docs/Phase4/DESIGN-named-interface-wrappers.md` — the runtime shells that made the recorders retirable
- `docs/Phase4/DESIGN-goexit.md` — `GoexitException` semantics
- `src/Tests/Performance/PerformanceRunner/Program.cs:449-520` — the `runtimeconfig` guard
- `src/go2cs/csproj-template.xml:23`, `src/go2cs/test-csproj-template.xml:45` — the `$(OutDir)` fix
- Commits `2defc1838`, `96e20c577`, `8448d92a0`, `e9b28cf4d`, `dc78c1e54`, `4e9cdd40c`, `762434cb8`,
  `4fd53bcd2`, `eb286fae7`, `1562e7e13`, `ac13b91ee`, `50366f016`, `06755118e`
