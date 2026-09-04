# CLAUDE.md — go2cs orientation

> Canonical orientation for any Claude/AI task working in this repo. This file is **authoritative**;
> where it disagrees with `docs/README.md` or the `.bat`/`.cmd` build scripts, those are considered **stale** —
> trust this file and the source. See companion docs: [`docs/Architecture.md`](docs/Architecture.md),
> [`docs/Roadmap.md`](docs/Roadmap.md).

> **Document authority — one ladder.** This file is repo doctrine and gates. The two migration
> **runbooks** — [`docs/GoCorpusMigration.md`](docs/GoCorpusMigration.md) and
> [`docs/DotNetMigration.md`](docs/DotNetMigration.md) — are the **living procedure** for release
> hops: they **lead**, amended in-stage from lessons learned, and no plan or record overrides them on
> procedure. `docs/PLAN-*.md` hold ruled strategy and instance campaigns; their OQ rulings are
> settled, and only a new ruling reopens one — but a ruling's **SCOPE** is a claim about *who reaches
> the seam*, not a permanent property: the netpoll ruling's "zero runtime edits" covered
> `internal/poll`'s consumers and never runtime's own suite reaching `netpollGenericInit` through an
> `export_test` re-export, so a NEW consumer re-opens the scope question without re-opening the
> ruling (2026-09-01; the remedy was one honest no-op equivalence, both halves measured). `docs/phase4/` RECON-/REHEARSAL-/CENSUS-/DATA-/STAGE0-
> files are point-in-time **records** — amended with dated blocks, never rewritten, never executed
> from. The BOARD is the append-only findings ledger; the mailbox is transport, not record. A lesson
> lands the day it is learned: procedure → the runbook; harness/gate doctrine → this file; findings
> and measurements → the board. (Doc-type definitions: [`docs/Glossary.md`](docs/Glossary.md),
> *Document types*.)

## What this is

`go2cs` is a **transpiler that converts Go source code into C#** that is both *behaviorally* and
*visually* similar to the original Go — the goal is that a Go developer can read the generated C# and
follow it easily. Go's compiler-provided conveniences (slices, maps, channels, multiple returns,
defer/panic/recover, goroutines, struct embedding, interface duck-typing) are emulated either by a
hand-written runtime library or by Roslyn source generators, so the visible converted code stays close
to the Go original.

This is the **"go2cs iteration 2"** generation of the project: the converter is now **written in Go** using the
official `go/ast` + `go/types` toolchain. The earlier converter (C# + ANTLR4 grammar) is fully retired —
the last build scripts that referenced it (`convert-gosrc.*`) were removed 2026-07-11.

> **General working principles** (think before coding, simplicity first, surgical changes, goal-driven
> execution) live in the user-global `~/.claude/CLAUDE.md` so they apply across all projects. This file adds
> the go2cs-specific discipline: root-cause against the real emitted `.cs`/`.cs.target` (the golden is the
> authoritative record) and **read the emission BEFORE spending a gate battery — it is the cheapest layer
> and it keeps paying**, keep the A/B footprint minimal, change *only* the goldens a fix must, and prove no
> corpus drift with `check-no-regression` — **compiling is not correctness** (that is the Phase-3 → Phase-4
> distinction). And **prefer the durable path over the shortcut**: when a task could be solved
> quickly-but-throwaway or correctly-but-harder, take the harder, general fix — a converter change over a
> one-off hand-patch, a real root cause over a workaround, the reproducible-from-repo result over a deploy-only
> hack. go2cs is a long-horizon project; work that advances the long-term vision is worth the extra effort, and
> throwaway code that has to be redone later is a net loss even when it ships faster today (the
> *nothing-throwaway* principle). This does not license speculative machinery — it is still the *minimal*
> solution, just the one that generalizes and lasts rather than the one that merely unblocks today.

## Architecture map

| Component | Location | Language | Role |
|---|---|---|---|
| **Converter** | `src/go2cs/*.go` (~67 files) | Go | Parses Go with `go/ast`/`go/types`, emits C#. |
| **Runtime library (`golib`)** | `src/core/golib/` | C# | Hand-written Go semantics: `slice<T>`, `map<K,V>`, `channel<T>`, `@string`, `array<T>`, `builtin` (`append`/`len`/`make`/`panic`/`recover`…), `ж<T>` heap box, `nil`, type aliases. **Shared by everything; never auto-overwritten.** |
| **Source generators** | `src/gen/go2cs-gen/` | C# (Roslyn) | Compile-time Go semantics: `ImplementGenerator` (interface impl), `RecvGenerator` (pointer-receiver overloads), `ImplicitConvGenerator` (type-alias conversions), `TypeGenerator` (struct embedding/promotion). Referenced as an **analyzer** by every converted project. |
| **Standard library** | `src/core/<pkg>` | C# (converted) | The whole Go stdlib (**305** converted packages on disk as of 2026-08-13 — **306** projects under `src/core` counting the hand-written `golib`; Go 1.23.12) auto-converted by `go2cs -stdlib`. **Compiles clean** (Phase-3 milestone, 2026-07-10); 146 packages **validate operationally** against `go test` as of 2026-08-16 — this figure drifts fast during campaigns; the roster header in `docs/ValidatedTestPackages.md` (recomputed from its own table) is the authority, swept by `src/run-validated-sweep.ps1`. Two packages are hand-owned and never queued for conversion: `unsafe` and `testing` (the Phase-4 test host). Its own `src/go2cs-stdlib.slnx`, generated by the converter and adopted verbatim. |
| **Behavioral tests** | `src/tests/Behavioral/` (593 top-level test projects = 621 transpiled Go packages incl. 28 nested sub-libraries; 622 registered `.csproj` incl. the `BehavioralTests`/`BehavioralRunner` tooling — re-measured 2026-08-17; this row drifts within DAYS during guard-heavy campaigns, so **measure, don't decrement**: `Get-ChildItem -Recurse -Filter *.go` on unique directories is the transpiled-package count, `check-solution-integrity.ps1` prints the registered count) | Go + C# | Per-feature Go↔C# equivalence (arrays, channels, defer, generics, interfaces…). |
| **Performance tests** | `src/tests/Performance/` (14 `Perf*` benchmarks + `PerformanceRunner`) | Go + C# | Go vs transpiled C# (JIT **and** Native AOT) time/memory comparison — results table in its `README.md`. |
| **Examples** | `src/Examples/` | Go + C# | Hand-converted Tour-of-Go / go101 / misc samples. |

**Two solutions, one tree:** `src/go2cs.slnx` = converter-dev workspace (golib + `go2cs-gen` + all
tests/examples/utilities + the ~61 `core/` packages their closure reaches) — **builds green**.
`src/go2cs-stdlib.slnx` = every `core/` package (**307** projects since layout L3 — the three platform-exclusive
adopted verbatim; it is what `push-nuget.ps1` packs. They overlap deliberately — same tree, same paths,
different scope — so a project can be opened from either. (The old hand-maintained classic `.sln` files
are all retired -- `src/go2cs-examples.sln` included, removed before the 75% anchor; only the two
`.slnx` remain.)

⚠ **NOTHING routinely builds `go2cs.slnx` end to end, so a broken solution member rots invisibly.**
Every harness — `BehavioralRunner`, MSTest, `check-no-regression.ps1`, `run-validated-sweep.ps1` —
builds each `.csproj` **by path**, never through the solution (the same by-path habit
`check-solution-integrity.ps1` polices from the other direction). A solution member that no gate
compiles can therefore break while every gate stays green. That is what happened to the
`utilities/QuickTest` scratch project: the r41 GoFrame arc (`6adab2909`, 2026-08-05) retired golib's
`func`/`Defer`/`Recover` execution-context API and carried the corpus with it, but QuickTest was
hand-written scratch nobody builds, so it took `go2cs.slnx` down for two days unnoticed. It was
**retired** on 2026-08-07 rather than hand-fixed again: it was the solution's only hand-written,
un-gated member, so it would have rotted at the next golib change, and the experiments it held
(struct/interface promotion hand-simulated *before* `go2cs-gen` existed) are covered by real
behavioral tests now. Git keeps it at `d3223d252` if a shape is ever wanted back. **After changing a
golib/runtime API, build `src/go2cs.slnx` once before banking** — ~90 s, and no other gate covers it.
⚠ Two golib cost rules, both measured 2026-09-01: **a golib change adding INSTANCE state to `ж<T>`
(or any per-box base class) is a corpus-wide byte-cost change** — +8 B lands on EVERY pointer box,
proportional to boxes allocated per path (measured 14/1/0 boxes across three alloc rows), so the
commit states the cost even when correctness demands the field (the element-aliasing publish gate
did; its unfavorable direction shipped unmeasured and later burned an attribution run). And **an
alloc row's B/op is only comparable against a figure taken at the same suite scope** — filtered vs
unfiltered differed by +167.04 B/op on ONE tree (AllocsPerRun's single warmup doesn't cover
one-time costs a full run has already paid), so a filtered census never compares its bytes against
a full-run record: the alloc-instrument sibling of the gated-census stream rule.

Converter internals (full taxonomy in [`docs/Architecture.md`](docs/Architecture.md)):
- Entry: `src/go2cs/main.go`. Stdlib driver: `src/go2cs/stdLibConverter.go` (builds the package
  dependency graph + topological `sortedQueue`).
- `visit*.go` — walk AST nodes → C# declarations/statements (e.g. `visitFuncDecl.go`, `visitRangeStmt.go`,
  `visitDeferStmt.go`, `visitSelectStmt.go`).
- `conv*.go` — convert expressions/types (e.g. `convCallExpr.go`, `convSliceExpr.go`, `convStarExpr.go`).
- Analysis passes: `escapeAnalysisOperations.go`, `variableAnalysisOperations.go` (shadowing),
  `nameCollisionAnalysisOperations.go`, `constraintOperations.go` (generics), `importOperations.go`.

## One tree (read this before touching `src/core`)

`src/core` is **the** go2cs standard library. Everything — behavioral tests, examples, the tour, the
Phase-4 test pipeline, NuGet — binds `$(go2csPath)core\<pkg>`, which is the exact reference the converter
has always emitted. There is no second tree and no path rewriting anywhere.

What lives under it:

1. **Converted packages** (`src/core/<pkg>`) — `go2cs -stdlib` output, regenerable wholesale. Nothing is
   hand-edited here long-term; fixes belong in the converter, in `golib`, or in a declared hand-own.
2. **Hand-owned packages** — `src/core/unsafe` and `src/core/testing`. Both are skip-listed in the
   conversion queue (`isNonConvertedStdLibPackage`, `stdLibConverter.go`) and both are recovered into the
   generated solution from their dependents' references, so they publish to NuGet like any other package.
   `unsafe` is a compiler intrinsic; `testing` is the Phase-4 test host, and hand-owning it is what makes
   F15b's "ONE testing package, period" structural instead of a remap. `testing`'s **subpackages**
   (`fstest`, `iotest`, `quick`, `slogtest`, `internal/testdeps`) are ordinary converted packages.
3. **Hand-owned FILES inside converted packages** — the `[module: GoManualConversion]` whole-file
   replacements and the `*_impl.cs` companions. A reconvert leaves the
   marked `.cs` alone and drops a `<name>.cs.auto` review sibling beside it.
4. **`golib`** (`src/core/golib/`) — the hand-written runtime, shared by everything, never auto-generated.
   `src/core/go2cs` (the `Symbols.cs` shared project) sits beside it.

**History (why this section used to say something else).** The hand-finished baseline lived at
`src/gocore/` (2020–2025), was renamed to `src/core/` on 2025-03-08 (`ba6fef6c9`), then **overwritten in
place** by the first full-stdlib conversion on 2025-05-05 (`6ca1c45b7`, +508k lines) — which stalled the
loop, because "conversion succeeded" there meant the transpiler didn't crash, not that the C# compiled.
The 2026-06-25 repair relocated that conversion to `src/go-src-converted/` and restored the stub into
`src/core`, giving a green baseline immediately and a **two-tree** doctrine: never reference both, because
both emit `namespace go` with `<pkg>_package` partial classes and would collide. That doctrine held for
six weeks and cost a rewrite pass on every csproj, two exact-path exceptions in the overlay, an inverse
rewrite in `deploy-core`, and a `-tests` remap. Its premise expired at Phase 3 — the corpus compiles under
a standing gate and 69 packages validate — so on **2026-08-01** the conversion moved home to `src/core`,
the stub retired, and all of that machinery was deleted rather than re-pointed. There is still only ever
ONE stdlib in a build; there is now only one on disk.

## Build / test workflow

- **Converter (Go):** built with the Go toolchain from `src/go2cs/`. Usage:
  `go2cs [options] <input_dir> [output_dir]`. Key flags (from `main.go`, authoritative):
  - `-stdlib` — convert the Go stdlib. `-stdlib fmt strings io` — convert only those packages (+filter).
  - `-recurse` — recursively convert an end-user module + its third-party deps (references the pre-converted
    stdlib via local `$(go2csPath)` project refs). A second positional output root isolates the generated
    `src\` app + `pkg\` dependency trees from that runtime root; converted packages reference one another
    relatively. Without it, recurse output defaults to `-go2cspath` for backward compatibility.
    `-recurse=module` narrows the SCOPE to the input module's own packages: every third-party package is
    still referenced into `pkg\<import-path>` but none is converted, so a dependency closure go2cs cannot
    convert can't hold up the module's own code (issue #32). Values compose — `-recurse=module,nuget`.
    A local-refs recurse conversion pins `$(go2csPath)` to the resolved runtime root in the output root's
    generated `Directory.Build.props` (condition-guarded default; relative `$(MSBuildThisFileDirectory)`
    form when the roots coincide, absolute otherwise) — before that pin an isolated output root fell back
    to the csproj template's `$(USERPROFILE)/go2cs/` default and no stdlib reference resolved (issue #36).
    `-recurse=nuget` instead emits NuGet PackageReferences
    (`go.<pkg>`/`go.lib`/`go.gen`, versioned `$(GoStdLibVersion)`) for the go2cs stdlib/runtime/analyzer so a
    converted app restores from nuget.org with no `deploy-core` staging; the app's own converted packages
    stay relative project refs, and the converter emits an output-root `Directory.Build.props` with a
    floating `GoStdLibVersion` default.
  - `-tests` — also convert the package's eligible `_test.go` suite + emit a runnable test-host project
    (default off; mutually exclusive with `-recurse` — `log.Fatal` on both). Forces `-comments` on (test
    conversions are derivative works), resolves the output path absolute, and self-locates `$(go2csPath)` by
    walking the output dir up to the first root containing `core/golib` — so the canonical two-argument form
    `go2cs -tests -test-action all <goroot-pkg-dir> <converted-pkg-dir>` needs no flags or env from a clone.
    ⚠ **Pass GOROOT EXACTLY as `go env GOROOT` spells it — a forward-slash path silently misroutes the
    whole emission into `namespace go.std.*`** (found 2026-08-24). `getProjectName`
    (`importOperations.go:48`) decides the namespace with `strings.HasPrefix(importPath, options.goRoot)`;
    on Windows a `C:/Users/.../go1.23.1/src/unicode/utf8` argument fails that prefix test against the
    backslash form `go env` returns, so the walk-up branch runs instead and finds **`$GOROOT/src/go.mod`,
    which declares `module std`**. Every file is then emitted into `go.std.unicode` rather than
    `go.unicode`, the conversion **exits reporting success**, and the damage surfaces as
    `error CS0117: 'utf8_package' does not contain a definition for …` in the CONSUMER packages
    (`strings`, `syscall/windows`) — pointing away from the cause and reading exactly like a converter
    regression that dropped public members. Same family as the `-go2cspath` empty-`<ImportedTypeAliases>`
    trap below: **a path the converter half-recognizes is worse than one it rejects.** Native paths convert
    clean first time. (The durable fix LANDED 2026-08-28, `433e9e4e0`: `isPathUnder` +
    `checkGoRootSpelling` + 3 guard tests — the loader-side comparison is path-normalized now. ⚠ The
    PROJECT-IDENTITY side has a measured open residual: `std.<pkg>`-named csproj artifacts dated AFTER
    the fix, with all sources namespace-correct — 2 csproj carrying `RootNamespace=go.std` while 13
    `.cs` declare `namespace go;` — from a run that exited reporting success. Mechanism unestablished,
    G owns the root-cause; do not re-diagnose the loader side, it is fixed and guarded.)
    ⚠ **It bites through the ENVIRONMENT just as readily as through an argument, and the Bash tool is
    where that happens** (paid again 2026-08-26). `run-validated-sweep.ps1` and the `-tests` pipeline
    read `GOROOT` from the environment, so `export GOROOT="C:/Users/.../sdk/go1.23.12"` — the
    forward-slash spelling a Bash-side lane naturally types, and which `go` itself accepts — routes the
    whole emission into `namespace go.std.*` exactly as the argument form does. **The visible tell is the
    project NAME**: the run writes `std.<pkg>.csproj` / `std.<pkg>.tests.csproj` beside the committed
    `<pkg>.csproj`, and the failure surfaces as CS0246 on a generated adapter type in a CONSUMER file
    (`writer.cs: 'sparseFileWriterжWriter' could not be found`) — which reads like a witness/generator
    regression and invites a hunt in the wrong package. It also survives an A/B: running the SAME sweep on
    a baseline converter reproduces it identically, so "it fails at master too" is NOT evidence the tree is
    at fault when the environment is the variable. Check for `std.*` artifacts before believing any such
    diagnosis, and set `GOROOT` from `go env GOROOT` verbatim (single-quoted in Bash so the backslashes
    survive).
  - `-test-action convert|build|run|compare|all` (default `convert`) — `convert`/`all` convert-and-hook
    (production sources then tests); `build`/`run`/`compare` act on EXISTING digest-validated artifacts
    without reconverting; `compare` (and `all`) diffs the C# host's terminal results vs `go test -json -count=1`.
  - `-test-timeout <dur>` — the **package deadline** for a converted-test action (build/run/compare);
    Go duration syntax, default `2m`, must be > 0. For `run`/`compare` it is handed to **both** sides
    (`go test -timeout` and the converted host's own `-timeout`) so they agree, and the child process
    is killed one minute later purely as a safety net. Before that threading each side fell back to
    its OWN 10-minute default, so **no** value of the flag could let a slower-than-Go suite finish —
    `hash/maphash` self-terminated at exactly 600 s under `-test-timeout 40m` and reported its
    still-running `TestSmhasherAvalanche` as an empty verdict that reads like a real failure. A suite
    whose C# run legitimately exceeds 10 min needs an explicit value (maphash: `-test-timeout 30m`,
    ~15 min in C# vs 7.6 s in Go — a performance gap, not a correctness one).
    ⚠ **The 2m default is FIVE TIMES SMALLER than the sweep's, so a hand-invoked `-tests` run fails
    where `run-validated-sweep.ps1` passes — on nothing but which default applied** (measured
    2026-08-24: `bytes` reported `Go="pass" C#=""` on 38 tests with ZERO reported, which is the exact
    signature the orphaned-`dotnet run` file lock produces and reads as total conversion failure; the
    sweep's `-TestTimeout` default is `10m`, and at that value the same tree validated 82/82, exit 0).
    **The tell is the SHAPE of the empty set, and it generalizes to any mass-empty comparison:** a
    contiguous **alphabetical tail** is a run that died partway (deadline or crash) because the host
    reports in sorted order; **scattered** empties are genuine divergence; **ALL** empty is the
    documented file-lock case. Check the ordering before believing the diagnosis — and pass an
    explicit `-test-timeout 10m` on any hand-invoked row so the default is never the variable.
    ⚠ **Refinement measured 2026-08-29 (`net`), and it is the one exception to "scattered =
    divergence": SCATTERED empties that EXACTLY EQUAL the package's `t.Parallel()` test set are ONE
    serial-phase death, not divergence.** The host reports in TWO phases — the serial tests first,
    then the parallel batch — so a single deadlock in the serial phase leaves a contiguous tail there
    AND parks the entire parallel batch unreported, and the union of the two reads as scattered
    because the parallel names interleave alphabetically with the serial ones. `net`'s 43-name
    "deadline family" was exactly this: one deadlock seen from two phases, and the arithmetic closed
    to the verdict once the TransmitFile seam landed. **Compare the empty set against the parallel
    set before reading scattered as genuine divergence** — a set equality is one grep, and it is the
    difference between one root and 43 phantom findings.
    ⚠ **A deadline RAISED with a byte-identical result is a BLOCK, not slowness** (measured 2026-09-02,
    `net` at 40m vs 60m: 501 terminal verdicts / 27 orphans on BOTH runs, both ending at the same test)
    — more budget cannot move a hang, and the stream's last `run` event is what places it. Compare the
    terminal and orphan SETS across the two deadlines before budgeting a longer one: equal sets mean the
    unreported names are one hang plus its serial tail and the parked parallel batch — the re-pricing
    shape (one test worth N verdicts), not N divergences.
    ⚠ **Before ANY shape analysis, read the results-file TAIL — a deadline kill states itself
    outright** (added 2026-08-29 after the third instance in one week): the C# host's
    `go2cs_test_results.json` ends with an explicit
    `{"test":"","action":"timeout","output":"package timeout after <hh:mm:ss>"}` event when the
    package deadline killed it, so the mass-empty diagnosis is a one-line read, not an inference.
    The three lanes that paid it — `bytes` at the 2m default, `sync/atomic` at a lane's own 30m,
    `net/http` at 25m (213 empties published as "divergences across 87 parents" before the tail
    was read) — each had the explicit event sitting in the file the whole time. The shape
    heuristics above remain for the cases the tail cannot settle (a crash leaves no timeout
    event), but the tail is checked FIRST and quoted in any census that reports empty verdicts.
    ⚠ **That tail is a SECOND artifact, and this file named the wrong one until 2026-09-02**
    (corrected against the converter source, not against habit): the package deadline is reported by
    `TestHost` into the host's own `--result` file — `go2cs_test_results.json` — and that event
    carries `"test":""`, so it never reaches the comparison record's per-test maps at all. The
    comparison record is the FLAT `<pkg>/go2cs_test_comparison.json` (`writeComparisonRecord`,
    `testConversion.go`); there is no `go2cs_test_comparison/` DIRECTORY anywhere in the pipeline, and
    `src/core/.gitignore` lists the three files under their real names. Two artifacts, two questions:
    the record answers WHICH verdicts diverged, the results file answers WHETHER the run was killed.
    ⚠ Not every crash is tail-silent: a **module-init** death states itself there too — a
    `NotImplementedException` thrown from the host's static constructor is written into the tail
    verbatim (2026-09-01) — so a mass-empty on a flavor with no run layer is the same one-line
    read, not an inference.
    ⚠ And the tail read has its own false-empty: the event can be carried as an ESCAPED JSON
    string, so a substring count of `"action":"timeout"` returned **0** on a record whose tail states
    the kill (2026-09-02) — match the escaped form too, or parse the field.
    ⚠ A new tail-stated member (2026-09-02, the `chanDir` arm): 388 divergences, every verdict `C#=""`,
    stream 0/0/0 — reading like a corpus-wide regression from the lane's own cut — with the tail saying
    `exit status 0xc0000142` (STATUS_DLL_INIT_FAILED), a TORN `bin`/publish tree from an interrupted
    run. Delete the publish dir and re-run; `0xc0000142` in the tail is a cleanup, never a finding.
    ⚠ **The tail rule presumes the results file is the RUN'S OWN — verify freshness before
    reading it** (measured 2026-08-29, the gated-census lane): a host invoked **DIRECTLY** with
    `--run` did not rewrite `go2cs_test_results.json`/`.xml` (four-way A/B: order- and
    exit-code-independent; `WriteResults` has no filter guard and the arg parse advances, so the
    mechanism is UNROOTED — do not assert one), while the comparison beside them was written
    fresh. **Scope NARROWED same day by the same lane's own re-test: the `-test-action compare`
    PIPELINE path with a filter DOES write results.json fresh** — the suppression reproduces
    only on direct host invocation, and which half of that difference is load-bearing is
    unmeasured (the routed chip re-measures both paths before anyone asserts a mechanism). The
    durable half of the rule stands regardless: a stale results.json next to a fresh comparison
    is NOT a deadline kill; a gated/filtered census gates on the CAPTURED STREAM; and the cheap
    check is the results file's timestamp against the comparison's.
    ⚠ **Three record-file rules, all measured 2026-09-02.** (1) A **gated** (`-test-filter`) run
    REWRITES the package's comparison record with nothing marking it gated — a harvest read
    `runtime/debug` as bankable off a filtered control's record, the only tell being 9 go entries where
    the full run had 10 — so after any gated diagnostic that record is poisoned for banking until an
    UNGATED run overwrites it. (2) A paired before/after measurement needs two FILES, not two runs: the
    record is git-ignored, so a branch restore cannot bring the "after" back and the baseline overwrites
    it in place — the diff then compares a file with itself and reads "zero moved"; copy each side's
    `results.json` to a distinct path first. (3) `git checkout HEAD -- src/core` + `git clean -fd` clears
    NONE of the pipeline's git-ignored state (`bin/`, `obj/`, the manifest, the comparison and results
    files), so a "restored" tree is WARM and a filtered run's record travels into the next one: delete
    the record files after every sweep, and state cold-vs-warm when comparing two runs.
    ⚠ **Two more, 2026-09-02.** (4) A gate PRESERVES a failed row's comparison record to a distinct
    path BEFORE any restore or cleanup — a union battery deleted the records after a `net/http` sweep
    FAILED, discarding the only evidence of which rows diverged; deletion is for hygiene, never for
    evidence. (5) `run-validated-sweep.ps1` walks the ROSTER, so `-Filter <pkg> -Exact` on an UNBANKED
    row throws "No banked packages matched" while the battery leg wrapping it exits 0 over the hole —
    route #6 in a coordinator instrument: run an unbanked row through the pipeline DIRECTLY, and carry
    every leg's failure in the wrapper's exit code.
    ⚠ **And what that preserved record answers, which the log cannot** (2026-09-02): a crashed host's
    EXIT CODE is nowhere in the sweep log's printed FAIL block (the stream's last three JSON events) —
    it is inside the comparison record's oracle-side error text (`child error exit status 0xc0000005`,
    with the child's stderr quoted). Two neighbours: the `-tests` pipeline is **SILENT on success**
    (nothing on stdout or stderr, exit 0), so a run's evidence is its ARTIFACTS — the `*_test.cs`, host
    and csproj files — never its exit code; and a diagnostic patch applied to a BANK host's tree is
    restored, and its records deleted, before anything banks from that host.
    ⚠ **And the filtered-status trap has a SEARCH costume** (2026-09-02): `find … | head` filled ten
    lines with unrelated paths and the truncated view was read as the ABSENCE of a preserved record
    that was sitting there. An unfiltered enumeration answers "is it there"; a head-limited one
    answers a different question — the same split as filtered vs unfiltered `git status --porcelain`.
    ⚠ **A FAILED `-tests` BUILD leaves the PREVIOUS comparison record in place** — the family's
    nastiest member, paid three times by one lane (2026-08/09). The pipeline rewrites
    `go2cs_test_comparison.json` only when a run completes, so a fix whose build DIED
    (e.g. a hand-own registered under a bare name where Go declares a method — key `"Type.method"`
    — displacing nothing and duplicating into CS0111) re-reads the OLD record and reports the OLD
    failures: it reads exactly like "the fix does not work", or worse, like a stable count. Before
    believing any post-fix count, verify the build the record claims actually succeeded and the
    record is newer than the edit; for the registry case, checking the placeholder was actually
    emitted is the cheap tell. The record is only the verdict when the run that wrote it completed.
  - `-convert-timeout <dur>` — the `-stdlib` driver's cap on ONE package's conversion; Go duration
    syntax, default **10m**, must be > 0 (`log.Fatal` otherwise). It is a **safety net against a hung
    conversion, never a performance assumption**: the value has to clear the slowest legitimate
    package on the slowest legitimate host, because a killed-but-healthy conversion is reported as a
    FAILED package — named in the log, counted in the summary, listed in `failed_packages.txt` — and
    reads exactly like a converter defect. It was hard-coded at 10m until 2026-09-02, when concurrent
    lane load on the i7 class pushed one package past it mid two-seeded A/B, which would have banked a
    whole package as a spurious emission difference. The fired message names the package, the elapsed
    budget and this flag, so raise it there (`-convert-timeout 90m`) rather than editing a constant —
    and pass the SAME value to both binaries of an A/B, since the cap is part of what a run measures.
  - `-go2cspath <dir>` — runtime/stdlib root and default output root for converted code (default `~/go2cs`;
    env `GO2CSPATH`). `go2cs -recurse <input> <output>` keeps generated code under the explicit output root
    while `$(go2csPath)` references continue to resolve against this runtime root. **It is also the root the
    converter reads each imported package's `package_info.cs` from** to mint the emitted
    `<ImportedTypeAliases>` block, so a stale/missing root used to emit a silently EMPTY block — no warning,
    exit 0 — and the OUTPUT varied with the shell's ambient `GO2CSPATH` (found 2026-08-06). Two protections
    since: **self-location** — any single-package or `-tests` conversion whose configured root is not a go2cs
    root (no `core\golib\golib.csproj`) walks its OUTPUT path's ancestors for one, so a bare
    `go2cs <pkg-dir>` inside a clone resolves against that clone with no flag or env; and a **loud
    once-per-run stderr warning** naming the resolved path and the consequence when none is found
    (deliberately NOT fatal — converting standalone code with no deployed root is legitimate). An explicitly
    configured *working* root always wins.
    ⚠ **Single-package mode emits BESIDE ITS INPUT — `-go2cspath` does NOT redirect its output**
    (measured 2026-08-31: `go2cs -go2cspath <tmp>\src <GOROOT>\src\internal\abi` wrote seventeen
    artifacts into GOROOT and nothing into the temp root, and the byte-identity gate then diffed
    the seeded copy against its own source — IDENTICAL, vacuously, with oracle contamination on
    top). Pass the output dir as the SECOND POSITIONAL for any single-package emission you intend
    to diff. Two tells, both cheap: an "emission" whose mtimes predate the seed's copy is not an
    emission; and a byte-identity green is only believable after its negative control (inject one
    blank line → the gate must go red → the restore must be byte-identical) — a gate diffing a
    seeded copy against its own source is a gate that cannot go red.
    ⚠ **Paid twice more on 2026-09-02, and the REPO-ROOT form is the silent one.** A lane omitting the
    positional wrote 167 `.cs` into a GOROOT — loud, because GOROOT is not supposed to hold `.cs`; the
    same omission with the repo root as cwd left **41 untracked byte-identical copies of
    `src/core/strconv` in the REPOSITORY ROOT for eight hours**, invisible to every `| head`/`| grep`
    status check shaped around expected files. A FILTERED `git status` answers "did my change land";
    only an UNFILTERED `git status --porcelain`, read whole, answers "is the tree clean".
    `-recurse` warns but never self-locates (without a second
    positional its root doubles as the output root, so moving it would move the generated tree);
    `-recurse=nuget` does neither (published package refs need no local root); `-stdlib` does neither (its
    root IS the output root the run itself populates, so an absent `golib` is the normal first-conversion
    state). Every harness that invokes the converter — `check-no-regression.ps1`, `BehavioralRunner`,
    `BehavioralTestBase`, `PerformanceRunner`, `run-validated-sweep.ps1` — now passes an EXPLICIT
    `-go2cspath <repo>\src` computed from its own location, so no gate's verdict can move with the ambient
    variable again.
  - `-platforms os/arch` — the ONE target a conversion emits for (default: the host). It also accepts a
    comma-separated **list** (`-platforms windows/amd64,linux/amd64,darwin/amd64`). With `-stdlib` a list
    now performs the multi-platform **EMISSION** (`platformEmit.go`): it converts once per target into a
    seeded staging root (`-platform-stage <dir>`) and MERGES the emissions into the `-go2cspath` corpus as
    layout L3 — shared files flat, platform-varying ones in per-GOOS folders, hand-owns routed to their
    principal's platform set. ~560 s for three targets (measured r51b). `-platform-census` remains the
    READ-ONLY instrument over the same staging (a manifest, no corpus output). A list without `-stdlib`
    is rejected rather than silently converting the first target.
  - `-platform-census <dir>` — the **multi-platform emission census** (increment 1, landed 2026-08-08).
    With `-stdlib` and ≥2 `-platforms` targets it converts once per target into `<dir>\<goos>-<goarch>\src`
    — each staging root SEEDED from `-go2cspath` per the reconvert ritual below, wiped and re-seeded per
    run so the r41 "never convert twice into one root" rule is mechanical rather than remembered — then
    classifies every emitted artifact (shared / variant / partial / exclusive) and writes
    `<dir>\platform-manifest.json`. It writes **nothing** into the corpus: `-go2cspath` is read as the seed
    and never as an output. ⚠ In any multi-target staging comparison, "differs" means NOTHING until
    you know which side was actually WRITTEN: a single-target conversion re-emits only its own
    target's per-GOOS files, so a per-PATH diff across staging roots reports fresh-vs-seeded pairs
    as differences (a confounded census nearly banked 60 false hits, 2026-09-01) — compare only
    paths BOTH conversions write, or classify by write-evidence first.
    ⚠ Its MIRROR, measured 2026-09-02: **IDENTICAL means nothing when the side was not WRITTEN
    either.** A windows-default single-target reconvert reported ZERO diff on an L3 package's
    `linux/` files — the very files another lane had measured, under a linux-target conversion, as
    carrying four missing forced-init hooks. Classify by write-evidence PER TARGET, and measure an L3
    package with the three-target `-platforms` emission rather than the host default.
    Emitted-vs-seeded is decided by a sentinel modification time, not by content,
    because the control target's emission is *supposed* to reproduce the seed byte for byte. The manifest
    carries the marker gate per target (hand-owned files the seed held, and any the run emitted as a plain
    `.cs` — must be zero) so a failed seeding cannot be mistaken for a platform finding.
  - `-goroot` / `-gopath`, `-indent 4`, `-var` (default on),
    `-uco` (channel operators, default on), `-comments`, `-cgo`, `-tree`, `-csproj <tmpl>`, `-debug`.
  - Single project/file: `go2cs package_dir` or `go2cs example.go [out.cs]`.
  - **Always pass `-comments` when converting the Go stdlib.** It defaults **off**, but the converted C#
    is a derivative work: the per-file `// Copyright … The Go Authors … BSD-style license` header **must be
    preserved** (license requirement), and the Go doc-comments are what make the output readable. Without
    it the header and all comments are stripped. (Behavioral-test goldens were captured *without* comments,
    so don't flip the default — pass the flag on stdlib `-stdlib` runs.)
- **Converted C# projects:** standard `dotnet build` (target **net10.0**, C# latest). Each converted
  `.csproj` references `golib`, the `go2cs-gen` analyzer, and the stdlib packages it imports. The
  `$(go2csPath)` MSBuild property resolves to `$(SolutionDir)` in Debug builds (so refs point at
  `src/core/...`); it is **distinct** from the converter's `-go2cspath` output flag.
- **Behavioral tests** (`src/tests/Behavioral/`): each test references `golib` + the `go2cs-gen` analyzer;
  most also reference `core/fmt` (a few reference `time`/`unsafe`/`strings`/`sort`/`math/rand`/`io`/
  `reflect`). Since 2026-08-01 those references bind the **converted** packages, so the suite's 515
  stdout comparisons against `go run` are also the broadest running validation the converted `fmt` gets —
  its closure is 57 projects (cold ~48 s, warm ~4 s).
  The `BehavioralTests` MSTest runner has these phases: `TranspileTests`, `CompileTests`,
  `OutputComparisonTests` (runs Go vs C#, compares stdout), `TargetComparisonTests` (byte-compares the
  transpiled `.cs` against a `.cs.target` golden).

### Test-harness mechanics (important when changing the converter)
- **`dotnet build` does NOT run the converter** — it only compiles committed C#. A clean build leaves the
  tree clean. **Running the tests re-runs the converter:** `BehavioralTestBase` rebuilds `go2cs.exe` via
  `go build` whenever any converter `*.go` is newer than the binary, then re-transpiles. So after a
  converter change, running the suite regenerates the behavioral `.cs` from current source (and may show
  them as modified in git — that's expected).
- **A hand-invoked `-stdlib` or `-tests` run REFUSES a stale binary — route #1 closed from inside the
  converter, where those two paths have no caller to instrument.** `go2cs` compares its own executable's
  mtime against every build input in the source tree beside it (`converterStaleness.go`, over the set
  `ConverterBuildInputs.cs` defines) and, when any is newer, ENUMERATES the extent: the count, the ten
  newest paths (all of them at ten or fewer), and which are emission-affecting — everything except a
  `_test.go`, which `go build` excludes from the binary. For those two drivers, whose output is banked
  or measured, it then exits non-zero; **`-allow-stale-converter`** proceeds deliberately and is what an
  A/B against a PRESERVED binary passes, so a stale run says so in its own command line. Every other
  shape — a single file or package, `-recurse` — keeps the warning and runs, because that is the
  scratch-probe loop and a pinned binary there is ordinary. Two limits to know: it is silent when no
  converter source tree sits beside the executable (a deployed binary), and it is blind to a TOOLCHAIN
  hop, which stays route #4's embedded-stamp comparison in the harness predicate.
- **FALSE-GREEN route #2 — stale OUTPUT (fixed 2026-07-20).** Distinct from the stale-`go2cs.exe` trap
  (route #1, where an un-rebuilt binary runs old logic): here the exe IS current but the runners *skip
  transpiling* and validate the **previous** converter's `.cs`. All three of `BehavioralRunner.UpToDate`,
  `PerformanceRunner.UpToDate`, and MSTest `BehavioralTestBase.TranspileProject` short-circuited on a
  `.cs`-newer-than-`.go` check alone. Converter work is exactly the case where the `.go` files *don't*
  change, so every project stayed "up to date", transpile was skipped for all of them, and Target/Output
  then compared the old converter's output against goldens that same converter had generated — everything
  matched and the suite printed **PASS**. A guard test "validated" that way guards nothing. All three now
  also require the `.cs` to be newer than **`go2cs.exe`**, so any converter rebuild invalidates the whole
  corpus. Verified by neutering a real converter fix (`lhsReusedInLaterRhs`) and rebuilding: the old
  runner reported PASS, the fixed runner reports `FAIL [Target,Output]` with no manual touch.
- **`check-no-regression.ps1` re-transpiles UNCONDITIONALLY** (it has no `UpToDate` equivalent), which is
  why CNR was immune to both false-green routes and remains the authoritative drift instrument for
  converter changes. Preserve that asymmetry: never add an up-to-date skip to CNR.
- **A new converter `.go` file must be registered in `src/go2cs/go2cs-src.projitems`** — the VS
  shared-project item list `go2cs-src.shproj` imports (and that shproj is a member of `go2cs.slnx`).
  Nothing *builds* from it (`go build` walks the directory), so a missing entry is invisible at the
  command line and only bites in Visual Studio, where the unlisted source is absent from Solution
  Explorer. It had drifted silently until 2026-08-06. **`projitemsIntegrity_test.go`** now gates it
  both ways — every `*.go` on disk (including `internal\*`) is registered, every registered path
  exists — under the plain `go test ./...` run from `src/go2cs`, so no new harness and nothing to
  remember; a failure prints the exact `<None Include=… />` line and the entry it goes after. (Same
  invariant `tests/Behavioral/check-solution-integrity.ps1` applies to `go2cs.slnx`.) The file is
  UTF-8 **with BOM** and its line endings are uniform — a third guard holds both, so edit it in place
  or via `[System.IO.File]::ReadAllText/WriteAllText`, never PS 5.1 `Get-Content`/`Out-File`.
  **Three more census/launch traps, each paid repeatedly (2026-08-17):** a DEFAULT ripgrep honors
  `src/core/.gitignore` and under-counts the marker census by one — census with `git grep` or a raw
  filesystem walk, never bare `rg` over `src/core`. A census over CONVERTED C# never keys on a
  type's spelled NAME: the converter deliberately mints aliases (`_type`, `Δio`, `abiꓸFuncType`,
  the whole `ꓸ` family), so a spelling-matched scan silently under-reports by every alias in scope
  — measured 2026-08-31 at ~1.9x, when 59 of 117 `Reinterpret` descriptor sites were spelled
  `_type` (runtime's `global using` alias for `abi.Type`) and were invisible to a name-keyed census
  however many times it was re-run. Resolve what the name denotes, or enumerate the aliases first
  and search for all of them. `Start-Process -ArgumentList` in ARRAY form does
  not quote a path containing a space (`C:\Program Files\Go` dies as `Failed to access input file
  path "C:\Program"`, reading exactly like a missing GOROOT — three lanes paid this); pass ONE
  pre-quoted argument string. And `MSB4166 "child node exited prematurely"` is a BUILD-INFRASTRUCTURE
  crash, not a package root — a `-tests` batch measured a package as a hard build failure (eleven
  MSB4166s, zero CS diagnostics) that reached its real 9-of-10 verdict in 45 s once
  `MSBUILDDISABLENODEREUSE=1` was set; set it for any back-to-back `-tests` queue before believing a
  diagnostic-free build failure.
  **Five more launch/instrument traps, each paid 2026-09-01/02.** (1) **Git Bash rewrites `cmd /c`
  into `cmd C:\`** — MSYS path conversion eats the `/c` — so the command never runs: `cmd` opens
  interactively, reads EOF, exits **0**. A "runner gate" passed that way with a log holding only the
  cmd banner; the EMPTY grep for its verdict line, not the exit code, is what caught it. Drive
  `cmd /c` from PowerShell (a `.ps1` launched by `powershell -File`) or set `MSYS_NO_PATHCONV=1`,
  and **grep a gate's log for its verdict line before believing "exit 0"** — route #6's shape,
  hand-typed. (2) **`robocopy` from Git Bash with forward-slash paths copies NOTHING and exits 1**,
  which is robocopy's SUCCESS code — a silent no-op that reads as a completed stage. (3) **The
  behavioral runner shells out to a BARE `dotnet`**, so the SDK must be on PATH: `DOTNET_ROOT`
  alone does not prevent NETSDK1045. (4) **Never locate a comparison binary by a recursive glob's
  first hit** — `Get-ChildItem -Recurse -Filter <name>.exe | Select -First 1` returned
  `bin\Release\Go\…` ahead of `bin\Release\net10.0\…` (G sorts before n), so a byte-identical
  289/289 "C# matches Go exactly" reading was ONE binary printed twice, and the runner reporting a
  real gap was right; name the TFM path, and positive-control an output comparison by making the C#
  side differ once. (5) **The LF-anchor trap is not converter-only** — harness C# is CRLF under the
  same `eol=crlf` pin, so an LF-anchored patch to `BehavioralRunner/Program.cs` matches zero times
  and the build that follows reports exit 0 with 0 errors *because the file was never changed*.
  (`strings` also cannot see a .NET UTF-16 literal: use `strings -el`, and check the checker against
  a literal known to be present.)
  **A sixth, 2026-09-02:** a WSL reconfiguration can silently change which USER a lane's automation
  runs as — after a resolver change the default user flipped, the lane's scripts became unreadable, and
  the wrapper EXITED 0 over a permission error in its log: route #6's shape again, a runner that cannot
  reach its own work reporting success. The LOG caught it, not the exit code; `wsl -u root` is the fix.
  **A seventh, 2026-09-02 — the exit code a PIPE throws away, in three costumes in one day.**
  `git push | tail` makes `$?` **tail's** status, so a mailbox tool reported a REJECTED push as success
  and advanced its read anchor to a local-only commit, marking unread posts read; `cmd | head -N`
  masked a toolchain wrapper's abort, which is why its first negative control read 0; and
  `cmd || true; echo "exit: $?"` reports `true`'s status. Capture the real exit BEFORE any pipe (to a
  file when the command must also be read), and make a failing state-advancing tool reset itself and
  exit non-zero — with its rejection path POSITIVE-CONTROLLED, since no normal run exercises it.
- **FALSE-GREEN route #3 — NESTED sub-library packages were never enumerated (fixed 2026-08-02).** All
  three transpile gates walked `tests\Behavioral\*` **top-level only**, so the 22 sub-library packages
  nested inside a test folder (`IoLike\FsLike`, `VersionedImport\vlib`, `CrossPackageArrayZeroValue\bufpkg`,
  `GoNamespaceShadow\nsshadowlib`, …) were transpiled by **no gate at all**. Two consequences, the second
  the dangerous one: (1) their committed `.cs` froze at whatever converter last touched them by hand — 17
  files across 13 packages had drifted by 2026-08-02, spanning three separate increments (the
  `// <TypeAccessibility>` block, string-literal hoisting, the compound-assign result cast); (2) a
  sub-library's `package_info.cs` is an **INPUT** to its parent's transpile — the parent reads the
  sibling's `[assembly: GoImplement]` records to decide whether to mint a local `ᴠ` value adapter — so a
  regression in that area could not make the parent's golden fail, because the parent kept reading the
  stale-but-plausible records. That silently disarmed the `ForeignValueImplementSuppression`,
  `ValueAdapterDynamicType` and `SamePackageImplementNoWitness` guards. All three gates now walk
  **recursively, DEEPEST-FIRST** (`GoPackageDirs` in `BehavioralRunner`/`BehavioralTestBase`; a recursive
  `Get-ChildItem` + depth sort in CNR) so a sub-library is regenerated before its parent consumes it, and
  `UpToDate` in both runners considers the nested packages too. Enumeration is now **570 packages**
  (545 top-level + 25 nested; it was 543 = 521 + 22 when this note was written), not 521. Note goldens remain top-level-only: nested packages have no
  `.cs.target` (`UpdateTestTargets` is deliberately unchanged), so nested drift is caught by CNR's
  `git status`, while the *cross-package* effect is caught by the parent's golden.
- **FALSE-GREEN route #4 — a TOOLCHAIN hop did not invalidate `go2cs.exe` (CLOSED 2026-08-24, H1.4).** A
  fresh instance of route #1's stale-binary trap that route #1's mitigation does not cover. Every rebuild
  predicate — `BehavioralTestBase`, `BehavioralRunner`, `PerformanceRunner` — rebuilds the converter when
  a converter **`*.go` file** is newer than the binary. Installing a new Go toolchain touches **none** of
  them, so after a hop every predicate still says "up to date" and every gate keeps running a binary that
  embeds the OLD release's `go/parser` + `go/types` front end (`conversionDriver.go` uses
  `packages.LoadAllSyntax`, i.e. the converter's OWN compiled-in type-checker) against the NEW release's
  sources. It does not fail cleanly: the old parser mis-parses or rejects the new constructs and the run
  degrades into the converter's best-effort "did not fully type-check" path — which CNR reports as **NOT
  MEASURED** (good) but the runners do not. **The remedy landed (H1.4, 2026-08-24) and came out smaller
  than planned: nothing needed stamping, because every Go binary ALREADY embeds its toolchain release and
  `go version <exe>` reads it back.** So the whole fix is one compare, in the ONE shared helper all three
  predicates already delegate to since route #5 — `src/tests/ConverterBuildInputs.IsConverterStale` — which
  fails stale-wards (unreadable stamp or unanswerable GOVERSION forces the rebuild) and is guarded by
  `TestConverterStalenessConsultsTheToolchain`. **No explicit `go build` is owed after a toolchain change
  any more; the predicates rebuild on mismatch exactly as on an mtime change.**
- **FALSE-GREEN route #5 — a converter build INPUT that is not a top-level `*.go` file invalidated
  `go2cs.exe` NOWHERE (found 2026-08-21 by the hop-campaign planning read; fixed 2026-08-22).** The
  third instance of route #1's stale-binary trap, and the one with the widest trigger. All three
  rebuild predicates — `BehavioralRunner` (`Program.cs`), MSTest `BehavioralTestBase`,
  `PerformanceRunner` (`Program.cs`) — asked whether any **top-level** `*.go` in `src\go2cs` was newer
  than the binary. The converter is built from more than that, and each omission changes what it
  **emits** while touching no top-level `.go` file at all: (a) the `//go:embed` assets —
  `embeddedTemplates.go` embeds both csproj templates, the `package_info.cs` skeleton, the icons and
  `profiles/*`, and `stdlibMetadata.go` embeds `stdlib-metadata.txt`; (b) the `internal\` packages the
  converter imports (`internal\stdlibmeta` and siblings), which a top-level walk never saw either; (c)
  `go.mod`/`go.sum`. Measured at the fix: **204 top-level `*.go` seen, 224 real inputs — 20 invisible.**
  Edit one and every predicate reports "up to date", the OLD binary keeps running, and every runner gate
  validates the PREVIOUS emission and prints PASS. The edit reads as a no-op, which is
  indistinguishable from "the change was already correct" — and a **.NET migration's TFM stage edits
  exactly those templates and profiles** (`docs/DotNetMigration.md` §5.2), which makes it the step in
  the project most likely to meet this route. Route #4's `runtime.Version()` stamp does not cover it: a
  stamp says nothing about a template's modification time. **Remedy (landed):**
  `src\tests\ConverterBuildInputs.cs` — one definition of the converter's build-input set, LINKED into
  all three projects (the two runners take no assembly dependency, so a shared assembly is not
  available), with the embedded half **DERIVED from the `//go:embed` directives themselves** rather
  than listed, so a directive added tomorrow is covered the day it is written. Two guards under the
  plain converter `go test ./...` (`embeddedAssets_test.go`): the directive **forms** stay inside the
  subset the C# resolver understands, and the three predicates still delegate to the shared helper.
  **`check-no-regression.ps1` was never exposed** — it has no rebuild predicate at all, it runs
  `go build` unconditionally, and `go build`'s cache is content-addressed over embedded assets
  (A/B-verified: editing `csproj-template.xml` changes the linked binary's hash, reverting reproduces
  it byte-for-byte). That is the same asymmetry that made CNR immune to routes #2 and #4 — preserve it.
  ⚠ One caveat on the second guard: cmd/go's test cache **drops files that resolve outside the module
  root** (`computeTestInputsID`, "Do not recheck files outside the module, GOPATH, or GOROOT root"), and
  the three predicate sources live under `src\tests`, outside `src\go2cs`. A narrowed predicate therefore
  reports `ok (cached)` and only fails under **`-count=1`** — so a change touching ONLY harness C# owes
  `go test -count=1 ./...`. The first guard has no such gap (every input it reads is inside the module).
- **FALSE-GREEN route #6 — an instrument that cannot find its own runner reports SUCCESS (found
  2026-08-24 by two lanes from opposite directions; closed the same day).** A different SHAPE from
  #1–#5: those all run a gate and measure the WRONG thing — a stale binary, stale output, an
  unenumerated package, an old front end — so each yields a verdict that is merely untrue. This one
  measures **nothing** and prints a pass over the hole. `src\_paths.ps1` spelled the corpus TFM as a
  **literal** (`$NetVersion = 'net9.0'`), the TFM census's Class-D hoist that had gathered nine
  hardcoded sites out of six files into that one line. Hoisting fixes the SPREAD, not the KIND: a
  hoisted literal is still a literal, and on a `net10.0` tree every consumer composes
  `bin/Debug/net9.0/`, which does not exist. `run-behavioral.ps1` fails loudly; **`run-performance.ps1`
  died in 20 seconds having run nothing**, and the only tell was the implausible speed — a full perf
  run is HOURS on this machine class. **No existing gate can see it**, because each wrapper's only
  preflight is the `dotnet build` exit code and **the build is genuinely green** (it writes to the TFM
  the projects declare); the runner that would have counted anything is never reached, so there is no
  phase to fail, no project list to come up short, and nothing to compare against a golden. Both
  halves are closed. **(a) `$NetVersion` is DERIVED** from the property of record —
  `src\Directory.Build.props`'s `<TargetFramework>` element, read by one file-read-plus-regex with no
  MSBuild and no `dotnet` (this module is dot-sourced by every instrument on every invocation),
  comments stripped so the props file's own prose cannot be read as the property, and **no fallback to
  a literal**: an instrument that cannot know its TFM throws, naming the file. Replacing the literal
  with `net10.0` is the tempting fix and the wrong one — it re-breaks at the next hop, which is what
  `docs/DotNetMigration.md`'s *derivation, not replacement* means. It also makes `migrate-tfm.ps1`
  honest: that instrument carries no site for `_paths.ps1` because its census already believed the
  PowerShell probe derived. **(b) Every wrapper that launches a runner asserts the executable EXISTS**
  before invoking it, and exits **non-zero** naming the expected path when it does not
  (`run-behavioral.ps1`, `run-performance.ps1`, and `run-performance-floor.ps1`'s bflat arm — which
  runs at `'Continue'`, so a missing compiler would otherwise report `ok` off a stale `$LASTEXITCODE`).
  Derivation removes today's trigger; the guards close the class, since any future cause of a missing
  runner is now loud. ⚠ The guards use an explicit `exit 1` rather than `throw` because the exit CODE
  is the property that matters and a `throw` leaves it to the host: on Windows PowerShell 5.1 the
  missing-runner path already exits 1 (measured, both wrappers, `-File` and `-Command`), so the
  exit-**0** sighting is a host- or wrapper-dependent swallow — which is the argument for stating the
  code rather than inheriting it.
- **FALSE-GREEN route #7 — a `go2cs-gen` (analyzer) change is invisible to EVERY standing gate except
  a behavioral COMPILE (found 2026-08-30, the W3a promoted-forwarder regression; fixed `0df5a3f2b`).**
  CNR is transpile-only, so generator output never enters its verdict; the stdlib solution compiles
  one assembly at a time, so an accessibility demotion that breaks only CROSS-assembly consumers
  stays green there (`internal` binds fine same-assembly); and the corpus 307/0 + CNR-byte-identical
  ladder a converter arc normally runs therefore proves NOTHING about gen changes. The W3 merge
  demoted net's public `TCPConn.Read/Write` promoted forwarders to internal and shipped green on
  exactly that ladder; the escape was caught days later by a derived net/http canary sweep — the only
  union gate that compiles a cross-assembly consumer of metadata-promoted surface. **Rule: any change
  under `src/gen/` owes a full behavioral COMPILE phase (slnx-dev build or the runner's Compile) and
  at least one cross-assembly consumer gate before banking.** Corollary paid the same night: ONE red
  behavioral project collapses the full-suite verdict into 651-suspect attribution (the Transpile
  phase rewrites every `.cs` first, so no assembly is up-to-date and the batch-build failure
  attributes everywhere) — measured: exactly 1 Release assembly written corpus-wide vs a clean
  78-project filtered batch. "651 suspects" means "one project is red", not "the corpus is broken".
- **⚠ An UNBANKED package's `-tests` assembly is in NO standing gate — route #7's shape, one
  assembly over (found 2026-09-01 by a lane's own sweep, by no gate).** CNR is transpile-only and
  the stdlib solution compiles PRODUCTION assemblies, so nothing at master ever builds the test
  emission of a package that has not banked: `reflect`'s `-tests` assembly sat compile-broken at
  master after a widened lift dedup bound a PUBLIC lifted struct's member shape to an INTERNAL
  prior lift — the dedup crossing ACCESSIBILITY tiers, CS0050/51/52 — with every standing gate
  green. **Standing amendment: any converter change touching lift identity, dedup registries or
  anonymous-type naming owes a `-tests -test-action build` of `reflect` at the MERGE RESULT, beside
  CNR.** The same hole has a second door: **the production-only two-seeded diff is blind to
  TEST-side emission** — a carrier stamp that dangled two banked rows lived in `x509_test.cs`,
  which `-stdlib` never writes, so the diff matched its prediction exactly and said nothing about
  the footprint that broke them. A converter change that emits CROSS-PACKAGE references therefore
  (a) lands its corpus footprint in the SAME train — the two-seeded diff applied verbatim,
  byte-identity asserted, exactly as a hand-own registration lands with its body — and (b) owes a
  `-tests` emission census of the banked rows it can reach, beside the `-stdlib` diff.
  ⚠ **The instrument that DOES walk every banked row's TEST emission is a roster-wide `-tests`
  reconversion, and it found what nothing else could (2026-09-02):** a BANKED row (`errors`, 61
  verdicts) whose test assembly no longer built at master — a production-registry dedup arm whose
  accessibility guard reasons within ONE assembly (`v.inFunction` short-circuits) bound the EXTERNAL
  test variant's function-local lift to the production assembly's INTERNAL lift (CS0122), bisected to
  `5442b402e`, whose own blast-radius census was a `-stdlib` two-seed diff and therefore structurally
  blind to test emission. Rules: a cross-assembly reuse is admissible only if REACHABLE (an internal
  variant, or a PUBLIC candidate); a documented invariant that a later seeding violates is a bug the
  doc cannot catch; **a lift/dedup change owes a `-tests` build of a row with an EXTERNAL test variant
  — `errors`, the cheapest — beside `reflect`**; and a delta table carries build failures as their own
  REGRESSION column, distinct from movers. (A bisect converging on adjacent commits with BOTH controls
  valid is an attribution; the named suspects were exonerated by measurement, not by argument.)
- **⚠ Route #7's ATTRIBUTION mirror: a crash INSIDE a generated shell is usually the shell being
  faithful** (measured 2026-09-02, runtime's `textAddr`). The `RecvGenerator` shell's
  DerefOrNull → NullRef → NRE on the first field touch IS Go's nil-receiver semantics; the nil came
  from `funcInfo()`'s module search, which can never succeed because the package's sole moduledata is
  a permanent empty stub (`len(pclntable)==0` skips it every time). A structurally guaranteed nil is
  not a race and not goroutine-specific — which tests crash is decided only by which ones reach the
  call at all. **Trace to the ASSIGNMENT, not the frame**, before billing `src/gen/`.
- **FALSE-GREEN route #8 — a guard DISARMED by a LEGITIMATE change (found 2026-09-01, the
  init-hook relocation).** Distinct from routes #1–#7: nothing is stale and nothing mis-runs — the
  guarded property genuinely moved house, and a guard asserting an assembly-level property by
  grepping ONE emitted file goes silently VACUOUS in its negative direction ("the bare form must
  not appear" is trivially satisfied by a file that no longer holds the construct at all). The
  positive direction fails loudly and gets fixed; the negative just stops testing, and the exit
  code says two failures when the real damage is four assertions. Glob-widening cannot fix the
  class when a DRIVER writes the artifact the exercised call never touches. Remedy: assert the
  DECISION (the recorded map/registry the pass writes — `packageImportInits` in the measured
  case), never the artifact's text, and re-check a guard's negative arm whenever the construct it
  greps for legitimately relocates.
  ⚠ **Route #8's sharper form KILLS the suite instead of going vacuous, and its verdict rides CLASS
  ORDER** (measured 2026-09-02, GolibTests): the guard's premise — "GolibTests does not reference
  converted `flag`" — was disarmed by a later `ProjectReference`, after which the converted `flag.Parse()`
  parsed MSTest's OWN command line through the process-global `flag.CommandLine` (`ExitOnError` →
  `os.Exit(2)`) unless a sibling class that replaced it with `ContinueOnError` happened to run first: one
  host's 460/460 was a lucky ordering, another's 82-then-abort the same defect. The fix SHAPE matters as
  much — the host parses its OWN args and **never mutates a process-global that converted tests read**
  (`os.Args` feeds `sync`'s `TestMutexMisuse`, `flag`'s `TestExitCode` and every self-re-exec): a
  divergence STATED against the ruling is how to diverge.
- **⚠ MID-BATTERY SOURCE FREEZE — while any gate battery is running, converter/gen/golib source is
  untouchable, on ANY branch (ruled 2026-08-30).** The behavioral runners rebuild `go2cs.exe` from
  DISK source the moment a `.go` file is newer than the binary, and golib/gen compile into every
  project the battery builds — so an edit mid-run makes the remaining legs measure a MIX of committed
  and uncommitted state (route #1's stale-binary trap inverted: a too-FRESH binary). Lanes queue
  their cuts until the battery's summary prints; the coordinator announces battery start/close on the
  mailbox for exactly this reason.
  ⚠ Scope, stated 2026-09-02 after a lane held a cut it never needed to: the freeze binds **the
  worktree the battery runs in, on any branch checked out THERE** — the runners rebuild `go2cs.exe`
  from that tree's disk and golib/gen compile into the projects that battery builds, so a lane
  editing its own clone on its own machine cannot reach a battery leg elsewhere on the fleet.
- **⚠ STANDALONE (no-solution-context) builds of tests/behavioral projects measure the DEPLOY ROOT,
  not the repo — and the errors look SEMANTIC (paid 2026-08-30, cost one full invalidated bisect).**
  Without `$(SolutionDir)`, `$(go2csPath)` falls back to the machine-global deploy root
  (`%USERPROFILE%/go2cs` / `%GOPATH%\src\go2cs`), which is STALE between deploys. A missing root is
  loud (CS0246 on `go`); a stale root is not — it produces plausible type-mismatch errors (CS1503,
  CS1929, CS0234 on a newer attribute) that read as real regressions and are COMMIT-INDEPENDENT,
  which is how a bisect probe built this way reported "no green endpoint" across three anchors whose
  in-solution builds were all green. **Any standalone build of a project under `src/tests` must pin
  `-p:go2csPath=<repo>/src/` (forward slashes), and a bisect probe must carry the pin.**
  collision site (root-caused 2026-08-21, fixed at the converter 2026-08-22).** A POSIX environment
  block is case-SENSITIVE, so `GO2CSPATH=/root/go2cs` and `go2csPath=/root/go2cs/src/` are two
  entries; MSBuild materializes environment variables as properties and resolves property NAMES
  case-INSENSITIVELY, so both fold into ONE `$(go2csPath)` and the winner is decided by enumeration
  order inside the .NET env-table plumbing — a per-process coin flip. The losing draw concatenated
  `$(go2csPath)gen/...` into `/root/go2csgen/...`, dangled the analyzer and every stdlib
  ProjectReference, and the build died in a CS0246 storm on every golib type: intermittent,
  package-shuffling Linux `-tests` failures that killed three measurement campaigns with every
  plausible suspect A/B-eliminated first. **Windows environment blocks are case-insensitive at the OS
  level — the two names are ONE slot — so five weeks of Windows sweeps could not see it.** The
  converter now (a) never exports its own derived `GO2CSPATH` (`resolveGo2CSPathDefault`, `main.go`)
  and (b) scrubs every case-variant from the inherited environment before appending the canonical
  entry (`childEnvWithGo2CSPath`, `testConversion.go`), so a child carries exactly one spelling
  whatever the invoking shell holds; guarded by `childEnvGo2CSPath_test.go`. The general rule outlives
  this variable: **anything a child reads through a case-insensitive resolver must be injected ONCE —
  scrub-then-append, never append-and-hope — and "Windows is fine" proves nothing about the class.**
  The Linux harness pin (`_paths.ps1`) STAYS until a Linux lane re-measures without it.
  ⚠ **A pin the CONVERTED side needs goes in the SHARED child-env base, never in one side's env**
  (measured 2026-09-02, the TZ pin): `runtime.envs` is filled by a `[ModuleInitializer]` before
  `Main`, so no host code precedes the snapshot and `TestHost.Run` cannot pin `TZ` from inside the
  process — and making the snapshot live would break Go's own set-at-process-start semantics. The fix
  is the process environment at LAUNCH, beside GOROOT/PATH, applied to BOTH sides of the comparison:
  a cross-SIDE divergence is worse than the cross-platform one it was meant to cure.
  ⚠ **A control harness reproduces the caller's ENVIRONMENT, not just its command** (measured
  2026-09-02): `BehavioralRunner` invoked DIRECTLY inherited neither the CI job's `GoTargetOS` nor
  `_paths.ps1`'s pin, so every L3 csproj took the windows default on a Linux host and the leg read
  "red by construction" — for a pin that already existed. Diff the CI step's environment against the
  repro's before believing a local red; a repro differing from its caller by one unstated variable is
  measuring its own shell.
- **⚠ TOOLCHAIN RESOLUTION: the pipeline's ORACLE side runs whatever bare `go` resolves on PATH —
  GOROOT alone does NOT pin it (measured 2026-08-29, the net/http bank lane).** `go2cs.exe` shells
  out to `go test -json` for the compare oracle, and that child inherits PATH; on a box whose system
  SDK differs from the pinned one (this machine class: ambient 1.23.1 vs pinned 1.23.12), a shell
  setting only GOROOT runs the WRONG release's oracle. The failure shape is a new member of the
  mass-empty family: **Go="" for every test** — the ORACLE side blank while the C# side reads
  plausible — the mirror of the file-lock signature (C# side blank), and it reads like total
  conversion failure. Prepend `$env:GOROOT\bin` to PATH in every pipeline shell and verify with bare
  `go version`, never just `go env GOROOT`. Same family, third member (lane R, 2026-08-29): a bare
  `go2cs -tests` on a **Linux** host bypasses the sweep's `GoTargetOS` pin and links the WINDOWS
  dependency set, minting phantom CS0426s that read as Linux defects — net-family Linux work routes
  through the SWEEP, always.
  ⚠ **Fourth member — the RIGHT SPELLING of the WRONG RELEASE, and every existing GOROOT check is
  blind to it** (measured 2026-09-02, a cloud lane): on a box carrying side-by-side SDKs bare `go`
  resolved 1.24.7 while the corpus pins 1.23.12 — `go env GOROOT` stays self-consistent, the conversion
  succeeds and exits 0, and the spelling/namespace guards pass because nothing about the PATH is wrong.
  **A conversion against the corpus prints `go version` AND GOROOT before it runs.** It is armed at
  BOOT too: a stale `/etc/profile.d` lane script exporting an older `/usr/local/go/bin` beat the newer
  fleet file (profile.d sources alphabetically — a `zz-` prefix fixes it), and `wsl.exe -- bash -lc` does
  not source profile.d like a real login: verify by bare `go version` in a real login shell.
  ⚠ **And its QUIET shape, with the seatbelt that is not one (2026-09-02, the container class):** where
  the loud form misroutes the namespace and exits 0, an oracle run under an ambient 1.24.7 against a
  1.23.12 corpus answers NORMALLY — no empties, no errors, a real comparison against a corpus the tree
  does not have. `GOROOT="$(go env GOROOT)"` is the trap wearing a seatbelt: pin explicitly, put its
  `bin` FIRST on PATH, ABORT unless bare `go version` reports the pinned release, and re-measure
  anything banked under an ambient one. The container class is NOT uniform (no bare `go` on one host,
  1.24.7 on another, 1.25.1 off PATH on a third) and a persistent USER-scope GOROOT can pin an old
  release on a laptop lane, so no lane assumes another's toolchain number — and pin `-go2cspath
  <worktree>/src` on every hand-invoked `-tests` run, whose generated csproj otherwise falls back to
  the machine-global deploy root (MSB4006 loud; a plausible verdict from uncompiled bits quiet).
  Because nothing recorded WHICH release ran the oracle, `oracleGoVersion` now goes into the comparison
  record, captured as OBSERVED — a `go version` through the same call, directory and environment the
  `go test -json` child inherits, `omitempty` so a late probe failure cannot invalidate a comparison.
  ⚠ **Third door, and the instrument that closes all three (2026-09-02).** The lane's OWN
  `export PATH=<toolchain>/bin:$PATH` defeats the fleet's `zz-` profile.d pin by construction — on a
  fleet host use the login shell and never prepend a toolchain path; a probe answering
  `command not found` is describing its own environment, not the host's. Cross the WSL boundary with a
  heredoc (`wsl -- bash -s <<'EOF'`): `wsl -- bash -lc '…'` expands `$(...)`/`$VAR` in the OUTER shell,
  so verification prints come back EMPTY (`GOROOT=`, `HEAD=`) and read as answers — three false
  "command not found" probes and one cut-presence line evaluated against the wrong tree. An empty
  verification print is a broken instrument, never a pass. And the wrapper that prints bare
  `go version` and ABORTS on a mismatch is itself NEGATIVE-CONTROLLED once against the box's other
  toolchain — the control must exit non-zero having run zero sweep stages — before any green it
  reports is believed.
  ⚠ **Two amendments to that third door, both measured 2026-09-02.** (1) **PRINTING a pin is not
  CHECKING it** — the "prints `go version` AND GOROOT before it runs" wording above is satisfied by a
  DECORATION: a control script printed `go1.24.7` on its first line, from a `go env GOROOT` taken in an
  unpinned shell, and carried on; three findings descended from that run and were withdrawn. An
  instrument that prints its pin and proceeds has no guard — it ABORTS on mismatch, and the print is
  only evidence of what the abort compared. (2) The WSL quoting rule WIDENS: **every substitution
  inside a single-quoted `wsl … -lc '…'` string is expanded by the OUTER shell** — verification prints,
  loop variables AND exit codes, which is how a false `(exit 0)` and three empty-path parse errors
  landed in one evening. The heredoc form (`wsl -- bash -s <<'EOF'`) is the only spelling for crossing
  that boundary.
- **`TargetComparisonTests` compares goldens with line endings NORMALIZED** (CRLF→LF; see
  `TargetComparisonTests.FileMatch` / `BehavioralRunner.FilesEqual`, both strip CRs). It was a raw
  byte-for-byte compare until 2026-07-07. Content diffs are still caught exactly; a pure line-ending
  difference is ignored (it can only come from autocrlf, never from the deterministic converter). To
  re-baseline goldens after an *intended* output change, run the **`UpdateTestTargets`** project with
  **`--createTargetFiles`** — it copies each project's current on-disk transpiled `.cs` over its
  `.cs.target` (it does **NOT** re-run the converter — re-transpile first, e.g. via
  `check-no-regression.ps1` or a runner pass, or the copy silently re-baselines stale output) —
  don't hand-edit goldens.
- **autocrlf gotcha (`core.autocrlf=true`) — two SEPARATE concerns:** the converter emits CRLF for C# line
  endings but preserves the Go source's LF inside multi-line string literals, so those `.cs`/`.cs.target`
  contain mixed CRLF/LF, and autocrlf rewrites the in-string LFs to CRLF on checkout.
  (1) **Golden text comparison** — no longer an issue: the comparison is line-ending-insensitive (above),
  so a smudged golden still matches and **no `-text` mark is needed just for the byte compare**.
  (2) **Runtime correctness** — still needs `-text`: if a project's *compiled program* embeds and observes
  a multi-line string literal at runtime (e.g. `Solitaire`'s board, printed via `println`), autocrlf smudges
  that literal's newlines to CRLF in the on-disk `.cs`, and any build that compiles the committed `.cs`
  *without* re-transpiling (VS, CI `dotnet build`, or the runner's up-to-date-skip) bakes the wrong `\r`
  runes into the value → the program misbehaves (Solitaire's board geometry breaks and the solver hangs).
  So `Solitaire`/`SortArrayType`/`StdLibInternalAbi` keep their `.cs` `-text` marks. A NEW multi-line-string
  test only needs `-text` if its program's *behavior/output* depends on the literal's exact bytes; if the
  literal is inert (never printed/measured), no mark is needed and the golden compare stays green regardless.
  **⚠ The CRLF working-tree form is now PINNED, not inherited from `core.autocrlf` (2026-08-08, r46c).**
  `.gitattributes` carries a `text eol=crlf` block for every converter-emitted artifact type — `*.cs`,
  `*.cs.auto`, `*.cs.target`, `*.csproj`, `*.slnx`, `*.props`, `*.targets`, `src/core/**/README.md` —
  ordered ABOVE the `-text` blocks so those keep their verbatim-bytes exemption (last matching pattern
  wins). Rationale: the converter emits CRLF *unconditionally*, so the checkout was the only variable,
  and a clone with `autocrlf=false` (git's default on Linux/macOS) materialized LF and made
  `check-no-regression` report the entire corpus as drifted before any work started. **Nothing about
  the Windows lane changed** — `eol=crlf` reproduces exactly what `autocrlf=true` was already doing,
  verified by `git add --renormalize .` over all 9,380 tracked files staging **zero** corpus files
  (every non-LF blob in the index was already `-text`). Two consequences worth carrying: a whole-tree
  renormalization is **not** owed, and the mixed-CRLF/LF phantom described above is *unchanged* in
  shape — it is simply platform-independent now. Do not "fix" a `.cs` to LF to match a Linux habit;
  the pin will put it back.
- **testhost lock gotcha:** a stray `testhost`/`vstest.console` from a prior run can lock
  `BehavioralTests.dll` → next build fails with `MSB3027` ("file locked by testhost"). Kill it (and
  `dotnet build-server shutdown` frees bin/obj locks) before rebuilding — not a real compile error.
  **Root cause + mitigation (2026-06-30):** the MSTest `Exec()` used an unbounded `WaitForExit()`, so a
  hung child (a deadlocked transpiled program, or a build blocked on a lock) hung the suite forever and
  orphaned testhost. `Exec` now has a per-call timeout (180s build/transpile, 30s run) that kills the
  whole child **process tree**, and disables MSBuild node reuse (`MSBUILDDISABLENODEREUSE=1`) so in-test
  builds don't leave lock-holding worker nodes; `AssemblySetup.[AssemblyCleanup]` runs
  `dotnet build-server shutdown` **only for a bare `dotnet test`** — a `run-behavioral-tests.ps1` run
  sets an env-var contract that suppresses it, since the script's default path isolates its own children
  instead (chip `6fe128108`, 2026-08-08). Prefer **`src/tests/Behavioral/run-behavioral-tests.ps1`**
  (clears stale hosts *before* the build — the lock manifests at build time — and runs with
  `--blame-hang`) over a bare `dotnet test`.
  **⚠ An MSTest verdict WORD is not a verdict — an ABORTED run prints one anyway** (measured 2026-09-02,
  GolibTests on a Linux lane): the second-to-last line reads `Passed! - Failed: 0, Passed: 82` and the
  LAST reads `Test Run Aborted.`, against a declared count near 470 — the exit code is honestly 1, but a
  verdict-word grep reads green, and `$?` after a pipe is the LAST command's status (grep's), so a piped
  invocation captures the raw exit first. **A GolibTests gate greps for `Test Run Aborted` AND compares
  the run's Total against the DECLARED count (`grep -c '\[TestMethod\]'`)**; an abort is an UNMEASURED
  suite, never a pass — the tell was adding 7 tests and watching the total stay 82. Run it `--no-build`
  behind the solution leg, too: a `dotnet test` that BUILDS raced twice in one night on a spurious
  CS0234/CS0246 that was gone on `--no-build` against the build just completed.
  ⚠ **And that DECLARED count is derived from the COMPILE SET, not from a raw `[TestMethod]` grep**
  (measured 2026-09-02): `GolibTests.csproj` `Compile Remove`s the Linux-only test files when
  `$(GoTargetOS)` is not linux, so a run reporting 474 against 479 grep-counted methods is
  COUNT-MATCHED, not an abort. Subtract the methods in `Remove`d files whose condition holds before
  reading a shortfall as a truncated suite.
- **⚠ CONCURRENT-SESSION KILLS — worktree isolation does NOT isolate `Get-Process <name> | Stop-Process`.**
  Those cleanup preambles (here, and the ad-hoc `Get-Process BehavioralRunner,testhost | Stop-Process` that
  is easy to type before a run) match by process NAME across the whole machine, so they kill a SIBLING
  worktree's in-flight suite. Signature: **exit `-1` with the log truncated mid-line and no diagnostic** —
  e.g. a full run died at 124s inside `PreBuildSharedDeps` and another at 163s inside `RunCompileGo`, and
  the same corpus then passed 521/521 untouched. Read that as "killed externally", NOT as a compile failure,
  and do not go hunting for a runner bug. Waiting for the other worktree's process to exit is not enough
  (it re-arms for its next run); the reliable defence is to be **unmatchable by name** — copy the apphost to
  a unique name in the same bin dir and run that (`Copy-Item BehavioralRunner.exe myRunner.exe`; it still
  launches the embedded `BehavioralRunner.dll` and `AppContext.BaseDirectory` is unchanged, so discovery is
  identical). Scope your own kills by path (`Where-Object { $_.Path.StartsWith($myWorktree) }`) so you are
  not the one doing this to somebody else.
  **⚠ The apphost rename does NOT cover the other reaper — harness background-task TREE reaping (measured
  2026-08-12, the A2 integration agent).** Being unmatchable by name defends against a sibling's
  `Get-Process <name> | Stop-Process`; it does nothing against the harness reaping a session's own process
  tree when a turn ends, because that walks parentage, not names. A long runner started as a background
  Bash/PowerShell child is IN that tree and dies with it — the same truncated-log, no-diagnostic signature,
  which is why it reads as the by-name kill and gets misdiagnosed as one. Surviving it required launching
  the runner **DETACHED** via `Start-Process` so it is not a child of the turn's process tree. Same shape
  as the sweep caveat in the budget table below (a LANE parking a detached sweep still loses it); the
  difference is that `Start-Process` detachment is what makes a long run survivable at all.
  **The detachment flags are load-bearing (measured 2026-08-14, the argv-stop and os-signal lanes):**
  `Start-Process -WindowStyle Hidden` with output redirected to a log file survives the reap;
  `Start-Process -NoNewWindow` followed by `Wait-Process` does NOT — the wait re-parents the session's
  fate onto the child and the turn boundary kills it exactly as if it had been spawned inline.
  ⚠ **The two detachment stories are measured and point OPPOSITE ways (2026-09-02).** A
  `Start-Process -WindowStyle Hidden` from INSIDE a PowerShell TOOL call died silently ~15 s in (the
  documented pattern covers a BASH-launched child surviving the turn boundary, not a tool call's own
  job scope), while a Bash `run_in_background` task is reaped with the SESSION's process tree — a
  2-hour solo sweep died ~13 min in and sat UNDETECTED for 76, with no completion notification.
  Anything longer than a turn runs DETACHED, env-pinned in the SAME command, logged unique-per-run
  and polled POSITIVELY by PID;
  clean-death evidence before a restore is modified files with ZERO untracked.
  ⚠ `Wait-Process` has ALSO reported a still-running target as exited, twice in a row (2026-09-01,
  the residual-pass lane): a background-wrapped `Wait-Process -Id` said done while
  `Get-CimInstance Win32_Process` showed the host alive with a live `go2cs.exe` child — one
  redundant CNR raced into the same behavioral tree before it was caught (the r41 overlap hazard,
  avoided only just). Mechanism unconfirmed; treat any `Wait-Process` "done" as unverified until a
  positive `Get-Process -Id` poll agrees (`while (Get-Process -Id $pid) { Start-Sleep 20 }` read
  correctly where the wait lied). Poll the
  log file (or the process by PID) instead of `Wait-Process` — and write the poll POSITIVELY
  (`while` + explicit `exit 0`/`exit 1`), never `until ! powershell -Command "exit (Get-Process …)"`:
  `exit $true` is exit code 1, so that loop ends instantly and reports "exited" while the process
  still runs (measured 2026-08-16 — the false reading launched a SECOND CNR into the same tree, two
  racing transpiles, caught only by PID inspection).
  ⚠ **The same false-"exited" reading has a SECOND, entirely different mechanism: Git Bash's
  `kill -0 <pid>` cannot see a WINDOWS pid** (measured 2026-08-26). The Bash tool's `kill` resolves
  pids in its own emulation namespace, so `while kill -0 $PID; do sleep 30; done` against a pid from
  `Start-Process -PassThru` (or any `Get-Process` id) exits on the FIRST iteration and reports the
  process gone while it is still running — no error, exit 0, indistinguishable from a real
  completion. It reproduced the 2026-08-16 damage exactly and then some: two CNR runs believed dead
  were alive, a third was launched, and THREE concurrent transpiles raced into one behavioral tree
  (the r41 "never let two conversions overlap" hazard), with a partial 2-package `git status` that
  read as a reassuring near-clean verdict. The tell was an mtime census — 288 of 641 packages never
  re-transpiled — and the proof was `Get-CimInstance Win32_Process` showing both "dead" hosts alive
  with live `go2cs.exe` children. **Rule: never wait on a Windows pid from Bash.** Wait from
  PowerShell (`Get-Process -Id`), or — better — make the long run the harness BACKGROUND TASK itself
  (`run_in_background`, the child of bash) so its real exit code is the task's, which is what
  PROTOCOL v3's mailbox monitor already relies on. And when auditing for strays, exclude your own
  querying process: a `Where-Object { $_.CommandLine -like '*check-no-regression*' }` sweep matches
  the very command line performing the sweep, so it reports a phantom survivor and, if you kill it,
  kills your own shell.
  **Three probe-hygiene rules from the same family (2026-09-01/02).** **Process AGE is read from
  `CreationDate` against `Get-Date`**, never against an assumed clock — a healthy three-minute-old
  run was killed in the belief it had been hanging for hours. **`pgrep -f <name>` matches its own
  wrapper's command line** — the bash edition of the self-match above — so a `while pgrep -f` wait
  loop spins forever on its own reflection after the child has exited; match on `/proc/*/exe`, i.e.
  the executable, never on a pattern that can match the process running the check. And **completion
  inferred from a SIDE EFFECT is not completion**: a file reverted because a running CNR had already
  transpiled it is a footprint, not an exit code — check the run.
  **⚠ Two more, 2026-09-02.** **Relaunching a chain while its predecessor's TAIL leg is still alive
  puts two runs in one worktree** — a third rebuild attempt met the second chain's in-flight `reflect`
  `-tests` convert as untracked `*_test.cs` and aborted on its dirt gate, the r41 overlap hazard
  caught only because that gate existed: census live processes (and wait for the task notification)
  before relaunching anything into a worktree. And **the harness's own
  `git status --untracked-files=all` over a worktree full of `bin`/`obj` can run for an HOUR** —
  slow, not hung, and not evidence of anything else.
  Two adjacent PS 5.1 traps the same lanes paid for:
  a repo script's `Write-Host` output goes to the INFORMATION stream, so capture with `*>&1`
  (a bare `2>&1` silently drops every `==>` status line and the log reads as hung); and the sweep's
  `-SkipBuild` expects the converter at `src\go2cs\bin\go2cs.exe` — an outside-the-repo binary path is
  not consulted, so a lane that built elsewhere re-pays the build or copies the exe there first.
  **⚠ The scratchpad directory is SHARED across concurrent lanes on one machine** (measured
  2026-08-15: two lanes both writing `cnr.log` — one clobbered the other's gate log mid-run, and the
  verdict had to be recovered from `git status`). It is session-scoped, not lane-scoped. Prefix every
  scratch filename with your lane/branch name (`<lane>-cnr.log`), and treat an unexpectedly truncated
  or rewritten scratch log as a collision first, a gate failure second. Make the name unique per RUN
  too, not just per lane — a REUSED log path on Windows can splice a fresh run's header onto a stale
  run's tail (file tunneling + partial overwrite) and fabricate readings like "CNR finished in 20 s"
  (measured 2026-08-15). And never census with `grep -P` on this box: it dies with "-P supports only
  unibyte and UTF-8 locales", so with stderr discarded it returns 0 matches and reads as "no sites"
  — a false-empty census that nearly got banked. Use ripgrep (`rg`)/the Grep tool.
  **⚠ The false-empty family has a deeper member: instrumentation that never compiled in (measured
  2026-08-28, the defer-multivalue-spread lane).** A type-aware census was built by patching an
  `fmt.Fprintf(os.Stderr, …)` marker into a converter helper via a heredoc python script, running
  `-stdlib` into a seeded temp root, and counting marker lines: ZERO hits across the whole stdlib,
  and the run looked entirely healthy — the stderr carried the normal spread of converter WARNINGs,
  proving the conversion had really traversed the corpus. The zero was an artifact. The converter's
  `.go` sources are CRLF in the working tree; the script's anchors were LF
  (`"\treturn tuple.Len()\n}"`), so they matched zero times and python's `assert` fired — but the
  script ran under `set -u` rather than `set -e`, so execution CONTINUED and built an
  UNINSTRUMENTED binary. Every downstream step then behaved normally, and the census counted a
  marker that was never compiled in. Two cheap tells were sitting there: the "instrumented" binary
  was BYTE-IDENTICAL IN SIZE to the uninstrumented one, and `grep -c SPREADCENSUS <binary>`
  returned 0 — the marker string was not in the executable at all. The durable rules: patch
  converter sources with the Edit tool (it matches the file's actual bytes), never an LF-anchored
  script — a script that must exist reads/writes with `newline=''` and anchors on CRLF, or
  normalizes first; `set -euo pipefail`, never bare `set -u`, in any instrument whose later steps
  assume an earlier edit succeeded; and ALWAYS positive-control a census before believing a zero —
  run the instrumented binary over a target KNOWN to contain the shape and confirm it fires with
  the expected count (the lane's control fired 12/12 on the behavioral guard's spread rows and
  stayed silent on its two controls, which is what made the real — also zero — production-corpus
  reading trustworthy). Same family as the `grep -P` and bare-`rg` notes: an instrument that
  cannot fail reports success over a hole.
  **⚠ The general form, named after five instances in ONE day (2026-09-01): an instrument built out
  of the thing under test cannot independently measure it — the corrective is a SECOND
  DERIVATION.** The sharpest instance is structural: **a `-stdlib` census answers "how much does the
  corpus change" and is BLIND to "does the fix reach the row" whenever the motivating site is in a
  `_test.go` file** — `-stdlib` never writes test emission, so those are two questions and they cost
  two runs. The others rhyme: a probe keyed on its own incomplete predicate reports the predicate;
  a probe blind to unwired slots reports the wiring; a type-name census was believable only once a
  `go/parser` derivation reproduced it, and a classifier's 66 only once an independent predicate
  reached the same number. Four corollaries, all measured 2026-09-01/02. **Name the LAYER a census
  is attached to**: a `claude/g-*` lookup over bare `g-*` refs returned a confident EMPTY, and a
  working-tree line-ending count under the `eol=crlf` pin was reported as a COMMITTED fact (the blob
  is LF, the checkout makes it CRLF, and `git checkout --` "cured" a state that was never in the
  index) — two retractions in one night, both from an unnamed layer. **An empty enumeration inside a
  redirected log is not evidence of absence** until a second instrument agrees. **An EMPTY diff
  after a "fix" is the fix saying it was not needed**, never the gate agreeing. And **count errors
  with the strict `error (CS|MSB|NETSDK)[0-9]+` pattern only** — a loose `grep -cE 'error '` scored
  1 error on a clean 831-assembly build by matching `internal.oserror ->`, and matched the word
  inside Go type names (`(…, error)`) where a case-sensitive `ERROR` read 0 against the loose
  grep's 140. ⚠ Finally, **a census attaches to the DEFECT's boundary — defined by the EXISTING
  marker set — not to the boundary the dispatch named**: a call-argument census missed two
  composite-literal sites the pointer twin's `anyBoxedPtrArgs` already marks, one of them a live
  defect. Grep the marker set first, and attach at every site it covers.
  ⚠ **Two 2026-09-02 refinements from one census that read ZERO against thirteen real sites.** Every nil
  construction of pointer-to-array type in Go 1.23.12 lives in a `_test.go` (reflect 10, runtime/arena 2,
  encoding/binary 1), so the production census of 64 nil-to-pointer conversions found none: **ask the
  `-tests` dimension whenever the motivating site is a test.** Where three derivations disagreed (grep 6,
  an instrument pointed at the grep-NOMINATED packages 11, an independent `go/packages` pass over all std
  packages 13) the disagreement was SCOPE, not predicate: scoping a census with the tool just shown to
  under-report reproduces its blind spot. Then **re-derive the population before any design is cut against
  it**: the 13 split into three tiers (6, 3, 4) and the "most interesting" members were the tier that
  needs nothing — a summary restating a lane's conclusion inherits its unvaried axis, so state what was
  MEASURED, not what was concluded.
  ⚠ **Three more, 2026-09-02, one shape: a property INFERRED from an artifact instead of measured.** A
  census can be exactly right about what EXISTS and exactly wrong about what it MEANS — thirteen
  typed-nil sites counted correctly by two derivations, then classified off the emissions and wrong
  twice (what the named spelling preserves is C# TYPE IDENTITY, not the dimension). **A converter hook
  that FIRES is not a hook that CHANGES the emission**: `getExprContext` returns the FIRST matching
  context, so cargo APPENDED as a second one is unreachable while the instrumentation reads healthy —
  instrument, then DISBELIEVE the instrument's agreement with the emission. And **a utility that exits
  0 with NO output is indistinguishable from one that never found its input**: its zero is a result
  only after a positive control (delete a known line, re-run unchanged, require it byte-identical).
  ⚠ **Three census rules from one shift, 2026-09-02.** **Attribution rides on a caller-supplied TAG,
  never on a stack walk** — a per-admit walk attributed 0 of 14 rows because the frames were inlined,
  while the tag read every row, whatever the walk costs. **A classifier applies the rule its CALLER
  asked**: 70,065 of 70,070 admits came through the marshalling (CONVERSION) callers, so the four pairs
  flagged WRONG under the ASSIGNMENT rule are legal Go conversions — attribute by caller MODE before
  classifying, verify flagged pairs against Go's own predicates, check whether a refusal at a fast path
  is RECOVERED downstream before predicting breakage, and prefer an explicit mode parameter over
  relying on that recovery. **And classify each site by the QUESTION it asks before counting it**,
  because a census can measure an option OUT OF EXISTENCE: 4 of 82 `GetType` uses were raw eface
  type-word comparisons and all four sat in ONE hand-owned file, leaving the converter-emission remedy
  with nothing to emit. A new census is also cross-checked against the HISTORICAL population (70,070
  against 70,071 admits) before its counts are believed.
  ⚠ **A SUBSTRING predicate over converter-minted GLYPH names over-matches BY CONSTRUCTION**
  (measured 2026-09-02): the `Δ`/`ж`/`ᴛ` families are prefixes of one another's identifiers, so
  `ΔHandle` matched inside `ΔHandler` and eight census hits were never real. Anchor on the WHOLE
  alias, or resolve what the name denotes — the alias-census rule above, one layer down. Its
  companion: **"carries the alias" is not "drifts on the other platform"** — only a transpile,
  mtime-verified, answers the class question, and the drift-measured number was one.
  **Scope DELETES by lane prefix too, not just writes** (measured 2026-08-16: a lane's cleanup swept
  the whole shared scratchpad and unrecoverably deleted sibling lanes' artifacts). A cleanup command
  must name your own `<lane>-*` files; `Remove-Item <scratchpad>\*` is a cross-lane destructive act.
  **⚠ Killing `go2cs.exe` alone ORPHANS its `dotnet run` child and the test host under it**, which
  keeps `runtime.dll` locked — the NEXT pipeline run then fails MSB3027/MSB3021 and its comparison
  reports `Go="pass" C#=""` for every test, reading exactly like total conversion failure (measured
  2026-08-16, cost one invalid run). It is a file lock: kill the process TREE by verified parentage,
  then re-run before believing any mass-empty verdict.
  **⚠ `dotnet build-server shutdown` is ALSO machine-global** (found 2026-08-03: one lane's startup
  cleanup yanked the shared MSBuild servers out from under a sibling's in-flight compile — same
  truncated-log signature, no Stop-Process anywhere). While sibling sessions may be building, do NOT
  run it; isolate your own builds instead (`MSBUILDDISABLENODEREUSE=1`, `-p:UseSharedCompilation=false`)
  and reserve `build-server shutdown` for solo contexts or coordinator-owned quiet points. The repo's
  own instruments are safe by default since 2026-08-08 (`db427e6e9`): `run-behavioral-tests.ps1` runs
  its shutdowns only under an opt-in `-ShutdownBuildServers` switch, and `AssemblySetup`'s teardown
  honors the same env-var contract — the hazard that remains is the ad-hoc, hand-typed invocation.
- **Faster alternative to MSTest — the standalone runner `src/tests/Behavioral/BehavioralRunner`
  (2026-06-30).** A dependency-free console app that runs the same four phases over every behavioral
  project but is **not** hosted in testhost, so the
  self-lock failure mode above is structurally absent. It collapses the per-project `dotnet build`
  calls into one parallel MSBuild invocation (pre-building the ~31 shared `golib`/analyzer/`core/*` deps
  sequentially first to avoid the parallel-build MSB3026/27 race, then fanning out). **All green**, at
  parity with MSTest — the parallel MSBuild invocation keeps wall-time from
  scaling linearly with project count. Drive it via **`run-behavioral.ps1 [--filter X]
  [--phase transpile,compile,target,output] [--update-targets] [--list]`**. Only output-compared
  (`[GoTestMatchingConsoleOutput]`) projects are `go build`- and stdout-compared, matching MSTest
  (library-style projects like `Constraints` have no `package main`). For a pure converter no-regression
  check with no compile/run at all, use **`check-no-regression.ps1`** (re-transpiles every behavioral dir
  and `git status`es the converter-emitted `.cs` **and `.csproj`** — the transpile rewrites both, and the
  `.cs`-only pathspec it had until 2026-08-08 made a csproj-emission change invisible on every platform.
  Converter stderr is captured, not discarded: a package the run could not fully regenerate — best-effort
  "did not fully type-check", a recovered "visit file error", or a non-zero exit — fails the gate by name
  as **NOT MEASURED** even with a clean `git status`, so the byte-identical verdict is never vacuous;
  other WARNINGs are counted as advisory, never fatal. Coordinator ruling 2026-08-08, from lane r48b's
  Linux `FindFirstFileData` finding — see `docs/PLAN-linux-operation.md`. Until F8 platform-gates the
  enumeration, a Linux CNR run therefore reports `FindFirstFileData` as NOT MEASURED by design).
  ⚠ **The class bites in BOTH directions now (2026-09-02): a behavioral guard written against ONE
  platform's syscall API cannot type-check on the other and turns THAT host's CNR red by name.** A
  lane's own-platform CNR green says nothing about the other host's gate — the union battery there is
  where it surfaces. F8 landed with train 11 (2026-09-02): a converter-preserved
  `[GoPlatformExclusive("<goos>")]` marker in `package_info.cs` naming the native platform(s), plus a
  LOUD skip-by-name BEFORE transpile in every enumerator (CNR, `BehavioralRunner`, MSTest as
  `Inconclusive`), its gating set DERIVED from the other platform's NOT MEASURED list (six
  windows-native, `ScmRightsSeam` linux) and positive-controlled both ways; commit markers before any
  CNR `-Revert`, which destroys uncommitted ones. Worse, a best-effort conversion on a
  NON-native host REWRITES the package's csproj and `package_info.cs` (the stdlib ProjectReferences and
  import aliases drop when the type-check that supplies them fails), so a Windows CNR POISONS a
  Linux-only behavioral package and every later leg of the chain measures the poisoned file — 5
  CS0246/CS0234 reading as a missing-reference regression. A chain therefore RESTORES behavioral dirt
  (`git checkout HEAD -- src/tests/Behavioral`) between CNR and any build leg, and F8's skip must
  precede the converter. Such a guard also carries a `runtime.GOOS` early-out as `main`'s first
  statement (raw `syscall.Socket` panics on Windows without the WSAStartup `net` performs), goldens
  stay WINDOWS-generated, and a Linux CNR-EQUIVALENT's DRIFT column is noisy by construction — the NOT
  MEASURED column is the honest one there.
  ⚠ **F8's consequences, measured at its landing (2026-09-02).** The marker has TWO halves — the
  harnesses' skip on a foreign host AND the `go2cs.slnx` UNREGISTRATION (the solution has one Windows
  flavour, so a non-windows-native package cannot compile there on any host) — and
  `check-solution-integrity.ps1` is the one gate that sees the second: run it. A platform-exclusive
  guard's golden (`.cs.target`) and its four MSTest entries are therefore verified ONLY on a
  native-host leg — on the other host F8 skips every phase, Target included, and CNR is transpile-only
  — which is how `ScmRightsSeam` landed with neither and nothing could see it. The same marker also
  covers a package that type-checks everywhere but FAULTS at run time (`LocalTimeZone`'s kernel32
  call): an Output-phase exclusive. The OTHER cross-platform class is ACCEPTED, not gated — a package
  that runs meaningfully on both platforms whose emission differs only by the `Δ`-alias flavour
  (`EnvironBlockWalk` and `SendtoSeam` — the class is EMPTY at master as of C2's marker seat, landing
  with train 14, and the COUNT is what retires there: the derivation below stands, because the next
  platform-varying guard brings the next member. Its two members were remediated by OPPOSITE
  mechanisms. `EnvironBlockWalk` is WINDOWS-native with a golden captured on Windows and read on
  Linux, so it takes the `[GoPlatformExclusive("windows")]` marker; `SendtoSeam` is LINUX-native with
  a golden captured on Windows, so it was REGENERATED on its own platform and marked linux
  (`e731145b7c`, train 12). **The remedy is decided by whether the package's NATIVE platform matches
  the host that captured its golden — never by how the drift looks in a diff.** The MECHANISM stays
  doctrine whatever the count: a generated ADAPTER TYPE NAME in production `.cs` follows the imported
  alias — `SockaddrInet4жΔSockaddr` on Windows, `жSockaddr` on Linux — measured against a master
  control with identical numstat on both trees; a follow-up census naming a third member was a glyph
  SUBSTRING over-match, `ΔHandle` inside `ΔHandler`, its transpile byte-identical. A class claim is
  re-derived at the TIP before it is quoted) is
  NAMED beside the package, with a standing Linux-CNR derivation (CHANGED files whose whole diff is
  the alias hunk or the adapter-name hunk) so its members surface by census rather than one at a time;
  a Linux CNR's honest verdict on this corpus WAS "clean modulo the windows-alias class" until C2's marker
  seat landed with train 14 — since then it is "clean" with no modifier (measured at `038c87786e`: 688
  byte-identical, 8 platform-exclusives skipped by name, 0 NOT MEASURED).
  ⚠ **The `.slnx` exemption criterion is platform-exclusive AND not-windows-native** (stated
  2026-09-02): the solution has ONE Windows flavour, so a `linux`/`darwin` marker unregisters the
  project and a `windows` marker changes registration not at all. A guard's own analogy check caught
  that in seconds — read the criterion's second half before predicting a registration change.
- **The emitted corpus's project-reference graph must be ACYCLIC, and that is now asserted on every
  CNR run (2026-08-30).** `check-solution-integrity.ps1` — CNR's preflight — DFSes the `src/core`
  `.csproj` graph once per `$(GoTargetOS)` (windows, linux, darwin: the per-GOOS `<ItemGroup>` blocks
  make each target a *different* graph) and requires 0 cycles, naming every cycle it finds. A C#
  project reference is a **compile-time** edge, so a cycle is MSB4006 and every project on the path
  stops building; Go's own imports are acyclic by construction, so the only thing that can create one
  is a reference the converter introduces that Go's graph does not contain — a `//go:linkname`
  forwarding property, which points wherever the directive names, in **either** direction. That is
  W1 (`docs/phase4/DESIGN-linkname-push-cycles.md`): a `-tests` conversion of `runtime` emitted
  `runtime → internal/syscall/windows`, and since Go's own imports contain
  `internal/syscall/windows → syscall → runtime`, **no conversion order can undo it** — the emitted
  edge itself has to go. The invariant this makes mechanical is narrower than "`-tests` must not
  rewrite the production emission" (which the four standing closure families contradict) and sharper
  than "the push must not add a reference": **a `-tests` conversion's production emission may differ
  from `-stdlib`'s only in ways that do not change the project GRAPH.** Positive control, kept as a
  parameter so it needs no tracked-file edit:
  `./check-solution-integrity.ps1 -TargetOS windows -InjectReference 'runtime=internal/syscall/windows'`
  must print exactly the six W1 cycles and exit 1.
- **Run the behavioral suite via the solution, not the project:** `dotnet test src/go2cs.slnx`. Running
  `dotnet test` on `BehavioralTests.csproj` directly breaks because `$(go2csPath)` (→ `$(SolutionDir)`)
  has no solution context, so the `core\golib` ref fails to resolve. The baseline solution is now an
  **`.slnx`** (`src/go2cs.slnx`); `src/go2cs-stdlib.slnx` is ALSO `.slnx` — auto-generated by the converter's `-stdlib` run (solutionGenerator.go) with solution folders mirroring the Go package namespaces. Since the trees unified its project paths match the repository's, so a fresh one is adopted by **copying it from the output root verbatim** (no rewriting; verified byte-identical). The old hand-maintained classic `.sln` is retired.
- **VS prompts to save `go2cs.slnx`/`go2cs-stdlib.slnx` on EVERY open — expected, harmless, and
  unfixable at the file level (bisected 2026-08-06, ten probe solutions).** Any `.slnx` containing a
  project that imports a `.projitems` shared-items file (`golib` and `go2cs-gen` both import
  `core/go2cs/go2cs.projitems`, the Symbols shared project) is marked dirty by VS's shared-project
  bookkeeping: classic `.sln` serialized it (`SharedMSBuildProjectFiles` section), `.slnx` has no
  element for it, so the model always differs from the parsed file while every save — including
  Save-As — writes **byte-identical** content (hash-verified; SolutionPersistence 1.0.52 round-trips
  both solutions exactly, and that is the version VS ships). Accept or dismiss the prompt, nothing
  changes on disk either way. Do NOT re-diagnose this as file drift, a generator formatting defect,
  or a reason to restructure the Symbols import. (Upstream: filed as
  [vs-solutionpersistence#156](https://github.com/microsoft/vs-solutionpersistence/issues/156) —
  the format has no shared-items element as of 1.0.52; if it gains one, or VS stops dirtying
  non-serializable state, this caveat retires.)
- **When iterating on regression work, use FILTERED + `--no-build` tests — don't run the full suite each
  time.** The full `dotnet test go2cs.slnx` rebuilds all **502** registered projects first and can take
  10+ min or hang under Visual Studio lock contention. Instead, from `src/tests/Behavioral/BehavioralTests`, run
  `dotnet test --no-build -c Debug --filter "FullyQualifiedName~<Name>"` — that reuses the existing test
  assembly and runs just that project's 4 phases (Transpile/Compile/TargetComparison/OutputComparison) in
  seconds. `--no-build` is valid as long as the `*Tests.cs` files haven't changed (`git status` them).
  Reserve a single full-suite run for final confirmation. Faster still for a pure no-regression check:
  re-transpile every behavioral dir and `git status` the `.cs` + `.csproj` — byte-identical generated code
  ⟹ identical compile+output ⟹ identical results, with no compile/run at all.
  ⚠ **A converter EMISSION change is measured by CNR BEFORE it is seated — a filtered behavioral run
  cannot see a project it does not build** (measured 2026-09-02). A seated commit's PREMISE was false
  (golib has `Func`-shaped defer overloads at arities 1–16; only arity 0 lacks one), so its rung
  rewrote corpus emission for a reason that does not exist and carried no footprint; the drift surfaced
  only when CNR ran for the NEXT commit and reported `DeferTypelessReturns` drifting on the rung alone.
  A lane that falsifies its own seated commit posts the HOLD before finishing the measurement.
- **Budget each command against its MEASURED baseline — the old flat "~3 min" cap is no longer right for
  the full runs (re-measured 2026-08-04 by r40, corpus at 569 transpiled packages / 571 registered `.csproj`).** The
  corpus keeps growing (371 → 457 → 518 → 543 → 569 packages), and both full instruments
  legitimately exceed three minutes. Timeouts must clear the real number or a healthy run gets killed
  mid-flight (a 600s ceiling killed a *passing* full suite once). ⚠ **These are DESKTOP numbers —
  and that desktop (the i9-13900K) DIED of hardware failure on 2026-08-09.** The
  same repo is also worked from a laptop (Ryzen 7 PRO 6850U, a 15–28W mobile part), where the parallel
  MSBuild phases run materially slower — a full behavioral suite measured **1,792s** there on 2026-08-07
  with nothing else running. A run over the table on the laptop is the machine, not corpus growth: do
  **not** re-baseline these rows from a laptop run, and size timeouts from the top of the range.
  ⚠ **The replacement coordinator machine (2026-08-10) is an i7-5820K — 2014 Haswell-E, 6C/12T,
  32 GB — and runs the table's rows at roughly 3–4x the i9 numbers.** Measured there on day one:
  full behavioral suite **2,820–4,131s** solo (the 4,131s end was a cold-ish tree; **2,820s**
  re-measured 2026-08-10 — either end is well over the table's 1,575s ceiling), CNR **1,505s** solo / **~3,190s** with two
  sibling lanes, converter `go test ./...` **200s** solo / **332s** loaded — ⚠ and go test's own
  DEFAULT `-timeout` is 10m, which a loaded run on this class now reaches: a healthy suite was
  killed at exactly 600.4s with a goroutine dump that reads like a hang (2026-09-01; 236s solo,
  578s under one sub-agent's load, dead at the wall under two) — pass an explicit
  `go test -timeout 30m` on any box carrying concurrent work, and read a FAIL at ~600s as the
  wall, not the code — full `go2cs.slnx` Debug
  build **1,432s** cold, `archive/zip`'s Debug test suite **774s** (vs 391s on the i9). ⚠ Those
  day-one figures are themselves STALE as the corpus grows — re-measured 2026-08-21 on the same
  i7-5820K: full behavioral suite **~6,552s at 603 packages** (and the runner batch-build default needed **9,000s** at 604 projects -- the stock 2,400s false-redded a healthy run, 2026-08-22), full `go2cs.slnx` Debug
  `--no-incremental` **~3,546s at 722 projects** — so budget those two from the 2026-08-21/22
  numbers and re-measure again at the next corpus jump. ⚠ **The `go2cs.slnx` row re-measured
  2026-08-29 on the same i7-5820K, and the spread is LOAD, not corpus growth: 845s wall SOLO at
  802 assemblies** (`--no-incremental -m -p:UseSharedCompilation=false`, golib rebuilt, 385 corpus
  warnings emitted — positive evidence of a genuine full compile rather than a skipped-work green,
  which is the only reason the number is worth quoting). The tree GREW over that interval (722
  projects → 802 assemblies) while the wall FELL 3,546s → 845s, and no corpus change runs that
  direction — so read the **3,546s as the under-sibling-load end** (it was never recorded as solo)
  and **845s as the current solo baseline**. Budget the row from the loaded end as this table
  always does — ~3,600s, not 845s — and treat a SOLO run materially past ~900s as contention to
  go find rather than work to wait out. Keep the i9
  columns as the historical reference the ratios hang off; budget commands from the i7-5820K figures
  (or 3–4x a row's i9 ceiling when unmeasured), and treat HARD-CODED harness watchdogs as suspects on
  this class of machine — at the old sizes, `PerformanceRunner`'s 600s AOT-publish cap and
  `BehavioralRunner`'s 300s build-all cap BOTH fired on healthy runs here and faked failures (each
  was raised 2026-08-10 with the evidence in a source comment; a timeout is a safety net against a
  hung child, never a performance assumption). Native-AOT perf publishes are the extreme case: ~7s
  each on the i9 in the stub era, **~25 min each** on this machine now that ILC compiles the full
  converted-stdlib closure per benchmark (post-unification), so a full perf run is hours, not
  minutes — and it must run SOLO: concurrent lane load pushed a healthy publish past even an 1,800s
  cap once, and only the Measure phase's numbers are trustworthy on a quiet machine anyway:

  | Command | Measured (warm) | Set timeout | Notes |
  |---|---|---|---|
  | `run-behavioral.ps1` (full, 4 phases) | **~370–1575s (6–26 min; 642s measured 2026-08-07 at 549 projects with a sibling lane converting; 416–957s on 2026-08-05 SOLO at 545 across four r41 stage gates — the spread is warm-vs-cold C# build state, not load; 626s on 2026-08-04 at 544, 1575s on 2026-08-02 with THREE sibling worktrees running pipelines)** | 2100s | 549/549 Transpile+Compile+Target; 523 Output-compared, 26 skipped (no `package main`); the top of the range is concurrent-lane load — budget for it. ⚠ At that load the **Go toolchain itself** can crash building one project (`panic: … compress/flate.(*huff…` inside `go build`) and the runner reports it as a Go build failure; re-run that one project filtered before believing it. Data point 2026-09-01: **1,916s at 652 projects, laptop-class host, SOLO, runner invoked DIRECTLY** (not via the Stop-preference wrapper) with `--build-timeout 10800 --build-one-timeout 900` — the stock 2400s batch cap sized at ~604 projects would have reported the whole corpus NOT MEASURED at 652 |
  | `check-no-regression.ps1` (full) | **~1,050–1,750s (17–29 min; re-measured 2026-08-17/19 at ~625 packages on the i7-5820K: 1,059s and 1,132s solo, 1,440s and 1,711s under sibling-lane load; laptops ran 720s (G) and 1,060s (R). The prior row read 350–510s/700s at 574 packages on the dead i9 — a timeout kept at that figure kills every healthy run on this corpus)** | 2400s | transpile-only, no compile/run; re-transpiles unconditionally |
  | `run-behavioral.ps1 --filter <Name>` | **~10–20s** (8 projects) | default | the iteration loop — use this, not the full suite |
  | `go2cs -stdlib -comments` (full reconvert) | **~195–240s (240s measured r47a 2026-08-08 with two sibling lanes; 223s at r41, 2026-08-05)** | 600s | 307 projects; per-file work is sub-second, the cost is `go/packages`. A three-target `-platforms` merge is ~3x this (545s measured r50a) |
  | single `core` pkg build | **~6s** (log/slog) – **~60s** cold (go/types) | 180–400s | cold includes the dependency chain |
  | full `go2cs-stdlib.slnx` build | **~92–188s** warm (307 projects; 149s measured r50a at `-p:GoTargetOS=windows`, 188s at r41 and 158s at r40, all with `-p:UseSharedCompilation=false`, the isolation flag a lane uses instead of `build-server shutdown`). ⚠ **i7-5820K on a healthy disk: 516s** `--no-incremental` (2026-08-14) | 600s (900s on the i7 class) | cold restore adds a few minutes. `-p:GoTargetOS=linux` is a DIFFERENT build and **completes clean: 307/307, 0 errors, 475s** (2026-08-14, after the three-target regen wave — `docs/phase4/CENSUS-linux-compile-wall.md` §10). It must be run `--no-incremental`: what differs between targets is the `<Compile>` ITEM SET, not any source timestamp |
  | full `go2cs.slnx` build | **~87s** `--no-incremental` / **~39s** incremental (573 projects; measured 2026-08-07) | 900s | the ONLY gate that compiles the non-generated solution members (utilities, examples) — run it after any golib/runtime API change. ⚠ Under concurrent-lane load a `go2cs-gen` run can die with `AccessViolationException` inside `TypeGenerator`'s recursive `PromotedStructDeclarations`, reported as an `error` against the package (seen once on `core/runtime`, NOT reproducible in two immediate retries with identical flags): re-run before believing it, exactly as with the Go-toolchain crash above |
  | `run-validated-sweep.ps1` (full roster) | **~46–53 min solo (3,138s measured 2026-08-07 at 109 packages / 13,611 verdicts; the roster is 131 packages / 14,769 matching verdicts / 47 disclosed, re-measured 2026-08-14 — so budget well ABOVE the 3,138s figure, and re-measure; ~90+ min under two concurrent lane loads — both r47 attempts were killed externally before finishing, so no clean loaded figure exists)** | run it BACKGROUNDED from the COORDINATOR session only — ⚠ a LANE parking a detached sweep and ending its turn gets it KILLED (the lane's process tree is reaped; happened twice on 2026-08-08 at 106/110 and 98/110, log ends between packages with no summary — recovery: re-run `roster − logged` inline and check the verdict arithmetic closes) | ~29 s for a typical package; i9 full roster measured **7,059-7,705s** at 159-162 rows (2026-08-22) -- the 46-53 min row is the dead i9-13900K era and stands only as the ratio anchor; use `-Filter` for anything but a final gate. ⚠ **ELEVEN** packages carry per-package deadline FLOORS in the script's `$longTimeouts` (re-counted 2026-09-02; `sync/atomic` 60m, `net` 40m and `net/http` 60m joined since the eight — the last sized to a TRUNCATED Debug measurement on the i7 class, where the row's two arms bracket the train's 30m at 1,836 s and a deadline-killed 2,171 s) (`hash/maphash` 60m, `index/suffixarray` 120m, `crypto/dsa` 120m, `archive/zip` 60m, `go/parser` 90m, `crypto/internal/mlkem768` 30m, `crypto/tls` 30m, `time` 40m -- its 1.23.12 suite is 169 tests and ~19 min on laptop-class — the table grew three rows and two floors moved by 2026-08-25, which is WHY the shard map derives its reserved set from the script at generation time instead of copying this sentence; **the script is the authority, this prose is a pointer**), **slow-host-calibrated** since 2026-08-10 — the original i9-sized values false-red every bare sweep on this machine class (hash/maphash and crypto/dsa both reported `FAIL … package timeout after 00:30:00` here; maphash then validated **22/22 in 2,406 s / 40.1 min** given room). The table is also a **floor, not an override** since the same date: a LARGER `-TestTimeout` raises it for a still-slower box (a smaller one still loses, since under-budgeting these four is the false red the table exists to prevent) — before that fix the flag was silently ignored for exactly the four packages that need it |

  Materially *past* these means the test host has hung under lock contention, not real work — stop and
  clear it rather than waiting 10–20 min. **Re-measure and update this table when the corpus grows again**;
  a stale baseline is what makes a healthy run look hung (and vice versa). The spreads above are real
  run-to-run variance on the same corpus (machine load), so budget from the TOP of the range, not the
  midpoint. A converter rebuild invalidates every project's up-to-date check, so the *next* full run
  after one always pays full price.
  ⚠ **An EXTRAPOLATION written in a MEASUREMENT's voice is a false measurement** (2026-09-02): a
  budget comment presented "~236 s fixed, ~62 min" as measured when the fixed term is not constant at
  all (6 shared deps in a 3-project slice against ~31 corpus-wide) and the runs behind it had timed a
  different flavour. The fix is a LABEL, not a better guess — mark the figure PROVISIONAL, state the
  measured points separately, and let the first real run replace it. Every row in the table above is
  a measurement or it does not belong in it.

  ⚠ **`BehavioralRunner` has its OWN internal timeout budgets, and no timeout the CALLER sets can
  influence them** — a generous outer budget on the `run-behavioral.ps1` call does nothing if the
  runner kills its own child first. They were hardcoded constants until 2026-08-10; they are now
  overridable, in SECONDS, at **flag > environment variable > default**:
  `--build-timeout`/`GO2CS_BUILD_TIMEOUT` (batch build, **2400**), `--build-one-timeout`/
  `GO2CS_BUILD_ONE_TIMEOUT` (per-project build, shared-dep pre-build, `go build`, **300**),
  `--transpile-timeout`/`GO2CS_TRANSPILE_TIMEOUT` (**60**), `--run-timeout`/`GO2CS_RUN_TIMEOUT`
  (one program run in the Output phase, **30**). The build defaults are sized for the slowest
  legitimate host per the safety-net doctrine (the i7-5820K measurement below is what sized them);
  a fast lane that wants the old fail-fast behavior opts DOWN explicitly (`--build-timeout 300`).
  **The slow-machine row this table was missing (measured 2026-08-10, i7-5820K 6C/12T, ~3x slower than
  the desktop rows, at 555 packages):** the one-shot parallel build exceeded the stock 300 s **cold and
  warm alike** — warm state cannot save it, because the Transpile phase rewrites every `.cs` immediately
  before Compile, so the batch is never an incremental no-op. For scale, a full
  `dotnet build src/go2cs.slnx -c Debug -m -p:UseSharedCompilation=false` of the same tree took **1,432 s
  cold** (573 projects, 0 errors), ~5x the old 300 s batch budget; a single cold filtered project
  measured 163 s. That measurement is what sized the current build defaults, so such a machine needs
  no configuration; the overrides exist to opt a fast lane back down or to survive a still-slower host.
  **A budget that expires is now reported as `NOT MEASURED`, never as a failure** — a fourth
  `Status.Timeout` alongside Pass/Fail/Skip, borrowing CNR's word for the same idea. This closes a
  **FALSE RED**, the mirror of the false-green routes catalogued above: on the cold slow machine the
  batch timed out, all 555 projects fell to the sequential per-project fallback, each *also* exceeded
  180 s (every one must first build the core dependency closure), and ~15 minutes produced zero
  assemblies and 555 `Status.Fail` entries that read exactly like a corpus regression. Timeouts still
  fail the run and still exit 1 — an unmeasured project must never read as a pass — but they are
  counted, listed and summarized separately. Two related traps the same change closed: an Output-phase
  run timeout used to surface as `exit code mismatch: C# -1 vs Go 0`, i.e. as a *behavioral* divergence
  naming a real test; and the per-project fallback now bails out after **3 consecutive** timeouts rather
  than spending the full budget on all 555 to re-learn one fact.
  ⚠ **A behavioral leg that must SHARD, and the two facts that decide how (2026-09-02).** Every
  behavioral project's build output copies the same ~55-dll core closure into its own `bin` (~29 MB
  each, ~20.5 GB at 695 projects), so an unfiltered Output leg cannot fit a hosted runner's disk in one
  batch: the ruled shape is shard-with-purge — alphabetical slices, `clean-bin` between them, verdicts
  unioned — never a narrowed enumeration (the durable follow-up is a shared-closure csproj template).
  And **`BehavioralRunner`'s `--filter` is a case-insensitive SUBSTRING** (filter `S` matched 455 of
  664), so no filter set can partition the enumeration: a sharding leg takes an INDEX SLICE over the
  deepest-first list and asserts the slice counts sum to the whole.
  ⚠ **Piping a long run through `Select-Object -Last N` buffers ALL output until it completes** — a
  backgrounded suite will look stuck at its first line for its entire duration. Check liveness with
  `Get-Process BehavioralRunner,dotnet`, not the output file. **`-First N` is WORSE: it terminates
  the pipeline once satisfied and KILLS the upstream native process mid-run** (measured 2026-08-16:
  a `-stdlib` reconvert died at ~100/304 with exit −1, reading exactly like a converter failure).
  Redirect long runs to a file and read the file — and redirect with **`Start-Process
  -RedirectStandardOutput`**, not `... *>&1 | Out-File`: the pipeline form BUFFERS, so a run that
  dies leaves a few-hundred-byte log ending mid-line, indistinguishable from an external kill
  (measured 2026-08-31, a 485-byte log from a dead full-suite run). In BASH, `*>&1` is not
  redirection syntax at all — the shell GLOBS it, silently no-op'ing the command (measured
  2026-08-31: one CNR and two runner attempts read as failures that never ran).
  **⚠ PowerShell-REDIRECTED output is UTF-16, and an ASCII grep over it returns a well-formed
  EMPTY** (measured twice 2026-08-31, independently): both `go2cs.exe … > log 2>&1` and a
  `Tee-Object` log land as UTF-16LE, so `grep <marker>` finds nothing and reads as "probes never
  fired" / "the run never happened" — a full retraction was built on six such empty greps, and a
  CNR verdict was nearly lost the same way. The tell costs one command:
  `head -c 200 <log> | tr -d -c '\000' | wc -c` — a nonzero NUL count means every grep against
  that log has been lying — then decode (`iconv -f UTF-16LE`) before grepping. Same
  silence-not-error family as the globbed `*>&1` and the buffered pipe above.
  **⚠ THE TRUNCATED-LOG READING INVERTS FOR POWERSHELL WRAPPERS (measured 2026-09-01, a
  self-inflicted two-runner race):** a wrapper running at `$ErrorActionPreference='Stop'`
  (`run-behavioral.ps1` line 49) dies on the FIRST native stderr line — killing the WRAPPER and
  leaving the runner alive, orphaned, and invisible. The truncated log reads exactly like the run
  being killed and invites the restart that puts two runners in one behavioral tree. Before
  believing a truncated wrapper log, census for the CHILD by executable path; a lane driving a
  long native child invokes it DIRECTLY (or at `'Continue'`), never through a Stop-preference
  wrapper. And never inject
  non-ASCII C# source (`Ꮡ`, `ж`, `Δ`) through a PowerShell command STRING — the argument pass
  mojibakes it even when file I/O is correct; write such content with the Edit/Write tools.
  **⚠ The same mojibake hits a `.ps1` SCRIPT FILE ITSELF when Windows PowerShell 5.1 parses it —
  and unlike the argument case, file I/O is NOT correct here, so the usual fix does not apply**
  (measured 2026-08-30, the syscall-pinning census-guard lane). A `.ps1` written UTF-8 without a
  BOM (the Write/Edit tools' default) is read back by 5.1's PARSER under the system codepage, not
  UTF-8 — so a literal non-ASCII glyph embedded in the script's own source (a regex pattern
  matching `ᴋ`, a string comparison against `Ꮡ`) silently decodes to mojibake at PARSE time, before
  the script ever runs. The instrument does not error: it runs, and reports whatever a
  never-matching pattern reports — in this case a false "0 sites found" that read as a correct RED
  result against a not-yet-fixed corpus, and stayed silently wrong against a freshly fixed one
  until the fresh run's *also* being zero broke the positive control. The fix is a UTF-8 BOM on the
  `.ps1` file itself (`[System.IO.File]::WriteAllText($path, $content, [System.Text.UTF8Encoding]::new($true))`
  after writing it any other way) — confirmed to make 5.1 parse the literal correctly. Positive-control
  any regex-bearing PowerShell instrument that embeds a converter glyph literally: run it against a
  known-populated target and confirm it finds a nonzero count before trusting a zero anywhere else.
  **⚠ A SHARED PowerShell instrument owes a run on BOTH editions before it banks (measured
  2026-09-02).** `_roster.ps1`'s comparison reader took `Add-Type -AssemblyName
  System.Web.Extensions` — a genuine PS 5.1 case-folding fix, smoke-proven on Windows only, and
  .NET-Framework-ONLY. Under pwsh 7 the script died at its second block, so the sweep's three
  absorption arms (host-conditional, capability-absent, host-limit) silently **DECLINED on every
  Linux host**, and the catch's own message named the missing assembly rather than the missing
  capability — pointing at the wrong artifact, in the file that decides whether a row banks. The
  fix is an edition-conditional reader (Desktop keeps `JavaScriptSerializer`; Core uses
  `System.Text.Json.JsonDocument`, explicit, never `-AsHashtable` behaviour inherited from a newer
  host), and the guard exercises both. **Rule: 5.1 on a Windows lane AND 7 on a Linux lane — or the
  OS-matrix linux leg — before a shared `.ps1` change merges.**
  ⚠ **What that check IS, stated 2026-09-02: the PARSE of every shared script under pwsh 7 Core, plus
  one row actually run.** A cloud container may carry NO PowerShell at all (`dotnet tool install
  --global PowerShell` lands one on the user's tool path) and its writable allowance may sit under the
  sweep's own disk-preflight floor — such a host runs the edition and gate checks with
  `-IgnoreDiskPreflight` STATED, and never banks a Linux row.
  **⚠ And a PowerShell FUNCTION named `Git` shadows `git.exe`** — command names resolve
  case-insensitively, so `& git` inside it recurses until "call depth overflow" (measured 2026-09-02,
  coordinator). The overflow line, captured through `2>&1`, then counted as ONE dirty entry in a
  `status --porcelain` check and aborted a rebuild twice with a message that read like real tree
  dirt. Name wrappers distinctly, invoke `git.exe` explicitly, and take `status --porcelain` with
  stderr dropped.
  **⚠ The same case-insensitivity binds PowerShell VARIABLE names** (measured 2026-09-02): a results
  array `$main = @()` silently overwrote the `$Main` worktree PARAMETER, so every main-tree round ran
  against an empty path and reported "(no verdict line) 0s" while the control rounds read fine. Name
  arrays distinctly from parameters, and let a function that RETURNS a number write its progress with
  `Write-Host` — a body that `Write-Output`s its progress returns those lines AS its value, and the
  caller's `Measure-Object` then chokes on strings. **And a `git` command run from a DELETED cwd prints
  plausible answers** — "0 commits not in master" for all seven branches, the only tell a
  `getcwd: cannot access parent directories` line at the END of the output: re-run from the repo root
  before believing any count taken after a worktree removal.
- **⚠ Banked-row protection at MERGE time — two rules, both paid for by the crypto/tls regression
  (found 2026-08-19; rooted and fixed by lane `claude/tls-regression`).** The flagship row banked
  green on its lane tip and was RED at master the moment its merge landed, because the guilty change
  (`d1ed1f7c1`, local-iface-cast) had merged to master AFTER the lane forked — each side green
  alone, the union never swept. A lane's sweep proof binds its OWN tree, never the merge result.
  (1) **A BANKING merge owes a post-merge filtered sweep of its own row at the merge RESULT** —
  `run-validated-sweep.ps1 -Filter <pkg>` on the merged master, not the lane tip; the lane-tip proof
  is necessary but not sufficient. (2) **Any reflect-bridge-touching change's canary set is the FIVE
  largest banked reflect consumers BY VERDICT COUNT — recomputed from
  `docs/ValidatedTestPackages.md` at gate time, never carried forward.** The consumer PREDICATE,
  explicit since 2026-08-29: **the package's OWN Go source — production OR `_test.go` — imports
  `reflect`** (test usage counts because the canary protects VERDICTS, and verdicts are produced by
  test code; a suite leaning on `reflect.DeepEqual` is exactly what a bridge regression breaks).
  Derivation is a grep of the GOROOT sources for `reflect` as an **IMPORT** — a name-LIST match
  over-matches (`go/doc/comment`'s `std.go` carries the name as data), so positive-control the
  predicate before using it (`encoding/json` in, `cmp` out) — plus the roster's counts, both at
  gate time. As of
  2026-08-29 it yields: `crypto/tls` 3,643 (bogo-capable hosts only; the collapsed-verdict path
  otherwise), `go/types` 557, `encoding/json` 491, `encoding/xml` 386, `crypto/x509` 341. ⚠ The
  PREVIOUS worked example here included `go/internal/gcimporter` (583) — a package with **zero
  reflect touchpoints in prod or test** — and that membership was then carried across several merge
  windows with only the counts re-read, until a lane's fresh grep caught it while holding an
  expensive sweep: the example had substituted for the derivation, which is precisely what this
  rule forbids, this time performed by the coordinator. Worked examples date and drift; the grep is
  the rule. ⚠ SECOND carried-membership catch (2026-09-01): `crypto/internal/nistec` (2,195) ALSO
  imports reflect nowhere — it had been travelling in the set beside gcimporter and was carried
  again the day before a lane's fresh derivation dropped both. "Reflect-bridge-touching" reads
  broadly: `src/core/reflect/*_impl.cs`, `src/core/internal/reflectlite`, golib's
  `GoReflect.*`/adapter/equality machinery, and the go2cs-gen adapter/shell templates all qualify.
  **And the canary RULE is now split (R's proposal, ratified 2026-09-01):** a change to the reflect
  BRIDGE takes the reflect-importer canaries above; a change to `abi.synthType`/golib **descriptor
  synthesis** takes a **COST canary as well**, because synthesis runs on every interface boxing
  corpus-wide — a blast radius the importer predicate cannot see at all (an unmemoized
  `GoPtrBytesOf` pushed nistec from 354s past its 600s deadline; the memoized re-measure is 384s).
  `crypto/internal/nistec` re-enters as exactly that cost canary: run it and compare its WALL TIME
  against the recorded baseline, not just its verdict.
  ⚠ **Derive the canary set in a clone whose refs you have verified.** A derivation run with the
  mailbox clone as cwd read `origin/master` **15 rows behind** and produced the SAME top five by luck
  (every dropped row was smaller); only a row-count reconciliation — 178 against the guard's 193 —
  caught it (2026-09-02). The mailbox clone's non-mailbox refs are stale BY DESIGN and are never read
  for repo content; reconcile any derived count against an independent one before using it.
  ⚠ The MECHANISM, stated 2026-09-02 after a second instance: the mailbox clone is TRANSPORT, and its
  refspec carries `claude/mailbox` ALONE — so `git fetch origin master` there moves nothing and its
  `origin/master` stays pinned wherever the clone was made (one such read was 15 rows behind and nearly
  escalated as "fifteen banked Linux rows lost"). Repo content is read only from a work tree, and
  `ls-remote` arbitrates when two refs disagree.
  ⚠ **Three ref-reading rules from one night, 2026-09-02.** **Name the REF you read**: a claim that
  says "at master" is read from `origin/master` AFTER a fetch, never from a branch's working tree — a
  branch's base is a snapshot of master at fork time and ages out from under every claim made through
  it (`git fetch origin master && git show origin/master:<path>`), the stale-base illusion applied to
  a single FILE rather than a diff. **After a fetch that PRINTED AN ERROR, verify the ref actually
  MOVED before reading anything off it**: a fetch dying on a clone's object corruption left
  `origin/master` unmoved, `git show origin/master:<path>` answered about the past while looking like
  the present, and "master is RED on the roster guard" was reported — falsely. "Benign for pushes"
  (they verified the remote moved) is not "benign for reads". **And an ANCESTRY question goes to a
  clone that HAS the ancestry**: a depth-200 shallow clone answered "NOT on the remote" for a ref the
  full-history repo showed contained, and it is the clone a lane reaches for by habit.
- **⚠ The three-run flake standard, and the A/B a re-converting SWEEP silently invalidates
  (2026-09-01/02).** A row that fails once is not a finding: the standard is **fail-WITH the change,
  pass CLEAN, pass again WITH the change restored** — three runs, in that order, before anything is
  attributed to a commit. The strong form is what costs lanes: **reverting the `.cs` is not an A/B
  when the instrument re-converts.** `run-validated-sweep.ps1` re-emits from the LIVE binary, so a
  hand-reverted corpus file is overwritten before the row runs and both arms measure the same
  converter — which is exactly what happened on the h2 deadline rows, identical signatures on both
  sides reading as "the change is innocent". Swap the **PRESERVED pre-change `go2cs.exe`** into the
  sweep path instead, and state which binary each arm ran.
  ⚠ **An A/B ARM that names a ref IS a ref derivation, and inherits the stale-ref rule** (2026-09-02):
  a Linux clone that had only ever fetched its own branch checked out `origin/master` at a commit
  predating the corpus hop, and the sweep's toolchain-pin guard refused it ("version.props pins
  1.23.1 … NOT MEASURED, never a verdict") — the guard caught it, the lane did not. Any arm naming a
  ref runs `git fetch origin <ref>` immediately before the checkout and PRINTS the resolved SHA; and a
  mid-run checkout makes a count belong to a tree nobody named, so check WHICH TREE a run measured
  before believing its number (a sweep measuring a 13-entry tree while the branch moved under it read
  exactly like a silently-rejected disclosure).
- **⚠ THE MEASUREMENT CONFIGURATION IS PART OF THE VERDICT — the `-tests` pipeline publishes DEBUG
  (measured 2026-09-02, the net/http h2 pair).** The generated `<pkg>.tests.csproj` pins no
  Configuration, so every roster verdict to date was taken at an optimization level no user ships: one
  published artifact flips `TestWriteDeadlineEnforcedPerStream/h2` fail→pass under Release (43.7 ms vs
  500–1000 ms per handshake), and default tiering flips it BOTH ways across consecutive runs of that
  same binary — a validation-integrity defect, the flake class arriving through the JIT. Ruled
  contract: **Release + `DOTNET_TieredCompilation=0`, both RECORDED** in two places that cannot
  silently drift — the comparison record (`testEnvironmentRecord{Configuration,Tiered}`, never
  `omitempty`: absence must not read as Debug) and the host's own `results.json` — plus the proof
  pages. The converter carries it as TWO flags since the tiering census — **`-test-config
  Debug|Release`** (Release publishes with an explicit `-p:go2csPath`, replacing the csproj template's
  Debug-conditional default, and disables the CLR's tiered JIT by default) and **`-test-tiered`** (the
  explicit opt back IN to tiered JIT, meaningless under Debug); the earlier `-test-release-tc0`
  spelling is RETIRED and survives only in one comment in `testConversion.go`. A bare `dotnet build
  -c Release` on the generated csproj is the trap they avoid: **grep the converter's flags before
  building an instrument**, and NAME both sides' configuration.
  ⚠ **Owner ruling (2026-09-02 11:44): the validation configuration of RECORD is Release with tiering
  off; Debug stays available by flag; the pipeline and sweep defaults flip after the Release census.**
  ⚠ **Falsify at the CHEAPEST layer, and separate a gate's PREMISE from its CONSEQUENCE** (2026-09-02,
  the Release census's own blocker): a `beforefieldinit` lazy-static-init hypothesis for a shim's
  Release-only flag rejection died to one grep of the pinned GOROOT — the flag does not exist in Go
  1.23.12 and the converted shim registers 45 = 45, i.e. an external runner/shim version skew — while
  the real event (an access violation at Release in a published single-file host, since unreproduced
  in two further runs and carried OPEN) stands unexplained. **A gate whose premise was wrong keeps its
  CONSEQUENCE when the consequence stands on its own**: a default flip that would UNMEASURE a
  3,643-verdict row is not taken on a corrected premise.
  ⚠ **THE FLIP LANDED 2026-09-02**, the census complete (`docs/phase4/CENSUS-release-tc0-delta.md`:
  195 of 201 rows unchanged, six disclosures retiring, nothing owed a root). `-test-config` defaults
  to **Release** and `run-validated-sweep.ps1`'s `-TestConfig` to **Release**; Debug is a flag away.
  **THREE rows opt back OUT via a new `execution: release-tiered` annotation** — `internal/godebug`
  (`TestCmdBisect`), `log/slog` (`TestCallDepth`) and `net/http` (`TestRegisterErr`) — all three
  PC/line-attribution assertions that tiering's presence supplies, each measured as a one-axis A/B,
  never inferred. `release-tc0` is retained though redundant. ⚠ **The sweep's override predicate had
  to change WITH the default and it is the trap in this flip:** it was `($TestConfig -ne 'Debug') -or
  $TestTiered`, which carried past the flip makes EVERY default run an override — and an override
  SUPERSEDES per-row annotations, so all three opt-outs would silently run at TC0 and fail while no
  run stayed bank-eligible. It now keys on whether the caller SPECIFIED the parameter
  (`$PSBoundParameters.ContainsKey`), so the default respects annotations and is bank-eligible while
  any EXPLICIT flag — the default's own value included — forces uniformity and is not. A default's
  value and a default's *explicitness* are different questions, and a predicate written when they
  coincided answers the wrong one afterwards. Proof pages and comparison records written before the
  flip still say Debug and are stale-until-reswept BY DESIGN; a rebank wave levels them.
  ⚠ **The stack-walk tiering class has a member in our OWN hand-own** (measured 2026-09-02, a one-axis
  A/B at `01a7fdefe`): `reflect`'s `valueMethodName` walks `StackTrace(2)` for a `_package` frame and
  LOSES the Recv frame under Release+TC0 inlining, so `TestValuePanic` passes at Debug and fails at
  Release on the SAME head. **A row that appears under the new default is attributed by the
  configuration A/B BEFORE any commit is suspected**; the remedy is the method name reaching `mustBe`
  explicitly with the walk retired, because a hand-own that infers identity from a STACK is
  configuration-fragile by construction.
  ⚠ **After the flip, every comparison NAMES its configuration beside the tree** (2026-09-02), and a
  set diff whose arms were taken at different times reads the configuration back from each RECORD
  rather than assuming it: a morning control at Debug against an evening pair at the new Release+TC0
  default made a row "appear" that had merely flipped on the configuration axis. Two runs agreeing
  prove DETERMINISM, not causation, when both sit on the same side of an unnoticed axis.
- **⚠ HOST QUALIFICATION for a network row: preflight `go test -count=1 net` BEFORE any net-family run
  (2026-09-02).** A host whose Go's OWN suite fails is disqualified as a bank host (a container
  answering `TestLookupCNAME` with the CDN CNAME and no IPv6; a WSL host failing that AND all 18
  `TestLookupNoSuchHost` leaves), and on an unqualified host the two arms of an A/B run different
  oracles — evidence, never a bank. A test asserting a live PUBLIC DNS record is UNIVERSAL drift once
  three independent resolvers agree (disclose it on the host-qualification ledger, not any one
  host's); and **a lane does not change a host's system configuration on its own initiative** — relay
  the commands to the owner, and RE-qualify afterwards (G-LAPTOP's WSL did, the same day: the 18
  leaves pass, wall 707 s → 35 s, and it is the fleet's Linux `net` bank host).
- **⚠ Before a divergence is NAMED, read the ORACLE at the ROW's own source and measure it under the
  SAME shape (2026-09-02).** A converted `crypto/tls` shim exiting 89 under bogo's flag set was
  compared against Go's 2 measured with the flag ALONE — and the answer was in Go's OWN source, not in
  a run: the row's TestMain exits 89 under bogo mode by its own line. The source to read is the
  row's own `TestMain`, not the file the flag was registered in: crypto/tls's prints `Usage of %s` over
  `os.Args` and exits 89 in bogo mode by its own line, and both were reported as divergences from what
  the flag package "normally does" — **"not what the package normally does" is not "not what Go does
  here."** Two neighbours from the same week. A shared CONVENTION name is not a shared MECHANISM:
  `GO_WANT_HELPER_PROCESS` spans suites whose re-exec paths differ (`exec.Command` →
  `posixSpawnForkExec` against `syscall.Exec` → `execve`), so a "row X after fix Y" dependency adopted
  from a lane's note was false and died on the fix's own measured null — read the CALL PATH before
  scheduling a row behind a fix. And a dramatic finding is re-derived from ITS OWN record before it is
  posted: a `"disclosed": []` grep read off `net/http`'s record was nearly published as `sync`
  falsifying its own `TestOnceXGC` disclosure — the record one file over is not this row's record.

### Performance comparison suite (`src/tests/Performance`, 2026-07-02)
- **Purpose:** answer "how fast is the transpiled C# vs the original Go?" — 14 small `Perf*` benchmark
  projects (Startup, Fib, Sieve, MatMul, String, StringView, StringMatch, Map, Sort, Channel, IfaceCall,
  Iface, IfaceShell, RefLower), each a behavioral-test-shaped folder,
  measured across **three variants**: Go binary, C# JIT (`Release`), C# **Native AOT** self-contained.
  Drive via **`run-performance.ps1 [--filter X] [--no-aot] [--runs N] [--update-readme]`** (standalone
  `PerformanceRunner`, no testhost; phases Transpile → Build → Verify → Measure; Verify requires identical
  timing-filtered stdout across all three binaries before anything is timed). The results table lives in
  `src/tests/Performance/README.md` between `PERF-RESULTS` markers (`--update-readme` rewrites it; prior
  toolchain tables accumulate in its *History* section for .NET 9 → 10 comparisons).
- **Mechanics gotchas:** benchmarks self-time via `time.Now().UnixNano()` (added to the baseline
  `core/time` stub for this) and print `elapsed_ns:` lines the runner strips before output comparison; the
  converter **regenerates each benchmark csproj on transpile**, so shared settings live in
  `Directory.Build.props`/`.targets` there (AOT is gated by custom `-p:PerfAot=true` — passing `PublishAot`
  globally breaks the netstandard2.0 `go2cs-gen` analyzer with NETSDK1207); AOT publish needs MSVC
  `link.exe` and the runner prepends the VS Installer dir to PATH for the SDK's `vswhere` probe; AOT trims
  with `TrimMode=partial` because golib `fmt` formatting and sort's `Interface<T>` bind members via
  reflection. ⚠ Cost changed at the 2026-08-01 tree unification: each AOT publish now ILC-compiles the
  full converted-stdlib closure (~7 s each in the stub era; **~25 min each on the i7-5820K**), so a full
  run is HOURS and must run SOLO — concurrent lane load once pushed a healthy publish past an 1,800s
  watchdog. `--no-aot` drops the whole column and stays fast. Keep each
  benchmark ≥50 ms and output deterministic (inline xorshift, no `math/rand`).
- **⚠ Two measured 2026-09-02, both from the TLS-handshake row.** Verify found a **SEMANTIC** divergence
  before anything was timed: the converted `crypto/tls` negotiates ChaCha20-Poly1305 where Go negotiates
  AES-128-GCM on the same host, because `internal/cpu`'s `doinit()` calls `cpuid` — x86 assembly, a
  throwing generated stub — and the throw is SWALLOWED, so x86 feature detection is all-false corpus-wide
  and every AES-NI/AVX fast path runs its software fallback. **A silently-ignored package init is a
  corpus-wide false green**; trace the swallow before pricing anything above it. And for a
  near-threshold SERIAL-latency row, **core count is the wrong lever — a NATIVE control on the same host
  is what exonerates the stack**: Go passed at 250 ms where the managed side failed at 250/500/1000 ms in
  the same run, leaving managed-vs-native handshake latency as the residual.
- **⚠ Both halves of that row are CORRECTED by later measurement (2026-09-02) — read them together.**
  There is no swallow: `schedinit` never runs, so `cpuinit`/`cpu.Initialize`/`doinit`/`cpuid` are
  UNREACHABLE and every `X86.Has*` is simply its zero value; the fix is a `[ModuleInitializer]`
  stand-in (the `goenvs`/`goargs` precedent) hand-owning `internal/cpu` over
  `System.Runtime.Intrinsics.X86`, 14 of Go's 20 flags mapped and 5 left false as the conservative
  direction. **A silently-UNREACHED package init is the same corpus-wide false green as a swallowed
  one** — trace the CALL CHAIN, not a `catch`. And the handshake residual was FALSIFIED as the h2
  pair's cause: a clean negative A/B moved 0 rows with AES-GCM negotiated, an isolated handshake is
  ~44 ms (which cannot blow a 250 ms rung), and the pair is a build-CONFIGURATION artifact — see the
  Debug-publish rule above; a cut's justification stays what it MEASURED.

### Adding a regression test when a converter defect is fixed
When a meaningful converter bug is fixed, lock it in with a behavioral test so later changes can't silently
reintroduce it. **Prefer extending an existing behavioral project** if one already covers a similar
construct; otherwise add a new one (example: `tests/Behavioral/GlobalStructFieldPointers`, which guards the
`&cpu.X86.HasADX` cross-file address-of-field fix). To add one:
1. **New folder** `src/tests/Behavioral/<Name>/` with a Go program that *exercises the specific construct*
   (multiple `.go` files are fine and run as one package — needed to reproduce cross-file bugs). Include a
   `go.mod` (`module go2cs/<Name>` — ⚠ but a test carrying a nested sub-library PACKAGE inside its own
   module takes a BARE `module <Name>` instead, or the sub-library's namespace and the consumer's
   emitted alias disagree and the parent fails CS0234; measured 2026-09-02, and the corpus agrees —
   24 of the 27 behavioral projects with a nested sub-package are bare, and the three that are not give
   the sub-library its own `go.mod`, i.e. a separate module path), and copy `go2cs.ico` + a
   `<Name>.csproj` from a sibling test (adjust
   `AssemblyName`; keep the `golib`/`fmt` refs the program needs). Verify it with `go run .` first.
2. **Make the Go↔C# output match** so `OutputComparisonTests` passes. Mind known runtime limitations — e.g.
   `Ꮡ(value)` (address of a non-boxed value) currently boxes a *copy*, so don't write through a
   `&global.field` pointer and then read the *original* global; read back through the same pointer.
3. **Register in the solution** — add a `<Project Path="tests/Behavioral/<Name>/<Name>.csproj" />` line under
   the `/tests/behavioral/target-projects/` folder in `src/go2cs.slnx` (alphabetical). **If the test pulls in
   a sibling library sub-project via `<ProjectReference>`** (e.g. `GoNamespaceShadow` → `nsshadowlib/go.nsshadow.csproj`),
   register **that** too, on the line right after its parent (the pattern used by `IoLike`→`IoLike/FsLike`,
   `NamedSliceChildPkg`→`.../netlike`). **Then verify it stuck** — run **`./check-solution-integrity.ps1`**
   (from `src/tests/Behavioral`): it asserts every behavioral `.csproj` on disk is registered in `go2cs.slnx`
   and flags any dangling entry, exit-1 on violation. (Also runs automatically as the preflight of
   `check-no-regression.ps1`.) This matters because the harness builds each `.csproj` **by path**, not via the
   solution, so a missing registration still passes the whole suite — it only breaks the `go2cs.slnx` build in
   Visual Studio (the unregistered project loses the Debug/`$(go2csPath)` context and its `core\*`/`gen\*` refs
   fail: CS0246/CS0234). That is exactly how `nsshadow` slipped through (added in `96eff53cd`, unregistered
   until `53dd2497e`). If Visual Studio has the `.slnx` open it can rewrite/reformat the file and silently drop
   an external edit — re-add and re-verify if so.
   **⚠ Windows CASE trap when you `git add` the new folder (found 2026-08-07).** `git add .` / `git add -A`
   — and any add run from a cwd *inside* the tree — records the path git gets from **readdir, i.e. the
   ON-DISK casing**, whereas an explicit lowercase pathspec (`git add src/tests/Behavioral/<Name>`) is
   canonicalized to the casing already in the index. Under `core.ignorecase=true` the difference is
   invisible locally, so a clone whose `src\tests` had drifted to a capital `src\Tests` on disk banked
   `DeferFrameScopes` at `src/Tests/Behavioral/…` while the other 4,240 files stayed `src/tests/…` — ONE
   directory on Windows, TWO on any case-sensitive filesystem (Linux clone, container CI, case-sensitive
   macOS volume), where the `.slnx`'s lowercase `tests/Behavioral/…` registration then fails to resolve.
   `check-solution-integrity.ps1` now asserts case-sensitively that every tracked path under the behavioral
   tree is exactly `src/tests/Behavioral/…`, so this cannot recur silently. If it fires: `git mv` will NOT
   do a case-only rename on Windows — rewrite the INDEX with plumbing (`git update-index --force-remove
   <wrong-cased-path>`, then `git update-index --add --cacheinfo 100644,<sha>,<lowercase-path>` reusing the
   SHAs from `git ls-tree -r HEAD`, which keeps the blobs byte-identical) — **and fix the on-disk directory
   casing too** (rename through a temp name, `Tests` → `__tmp__` → `tests`), or the next `git add -A`
   re-creates the wrong path. Both are working-tree-invisible: `git status` stays clean throughout.
4. **Transpile once** (`go2cs.exe src/tests/Behavioral/<Name>`, no `-comments` — behavioral goldens omit
   them) to generate the `.cs` + `package_info.cs`. For output comparison, add `[GoTestMatchingConsoleOutput]`
   to the generated `package_info.cs` class (a hand-added attribute the converter preserves).
5. **Generate tests + goldens:** run the **`UpdateTestTargets`** utility **with `--createTargetFiles`** (from
   its `bin/Debug/net10.0`). It scans every `tests/Behavioral/*` folder, rewrites the `// <TestMethods>`
   blocks in all four `*Tests.cs` classes (adding `Check<Name>()`), and copies each transpiled `.cs` to a
   `.cs.target` golden. It only emits an `OutputComparison` test for projects whose `package_info.cs` has
   `[GoTestMatchingConsoleOutput]`. Afterward, `git status` should show only your new project + four
   `+3`-line test-class diffs (no other `.target` churn).
   ⚠ **ONE WORKTREE PER CUT — `UpdateTestTargets` enumerates the DIRECTORY, not your change**
   (measured 2026-09-02): a stray untracked project left by ANOTHER cut was enumerated into this
   cut's four test classes, and the ASYMMETRY is the tell — one new project gives `3/3/3/3`, that run
   gave `6/3/6/6`. Two dirty converter files from the same neighbour would also have made any build
   there measure a MIX. Neither fails a gate, so the check is the diff's shape: count the added
   `Check<Name>()` lines per class before staging.
6. **Verify (filtered, fast):** preferred — from `src/tests/Behavioral`, run
   `./run-behavioral.ps1 --filter <Name>` → the 4 phases (Transpile, Compile, TargetComparison,
   OutputComparison) for that project via the standalone runner, in seconds, with no testhost/lock risk.
   Equivalent MSTest path (still valid): from `src/tests/Behavioral/BehavioralTests`, run
   `dotnet test --no-build -c Debug --filter "FullyQualifiedName~<Name>"`. Either way, avoid the full
   `dotnet test go2cs.slnx` while iterating — it rebuilds everything and can hang under VS lock contention
   (see the test-harness notes above). The golden comparison is line-ending-insensitive, so a multi-line
   string literal needs **no** `.gitattributes` handling for the byte compare — mark the `.cs` `-text` **only
   if** the compiled program's behavior/output depends on that literal's exact newlines (autocrlf gotcha above).
7. **Record the conversion decision (keep the strategy docs living).** The conversion strategy lives in
   **two** documents, and a notable decision updates the right one (often both):
   - [`docs/ConversionStrategies-Reference.md`](docs/ConversionStrategies-Reference.md) — the exhaustive
     **technical reference**. Nearly every conversion decision lands here: add or update the `###` subsection
     under the matching `##` topic with the emitted form, the edge case, the reasoning, and the guarding
     behavioral test. This is where the deep detail and history accumulate.
   - [`docs/ConversionStrategies.md`](docs/ConversionStrategies.md) — the high-level **summary** (one section
     per topic, tight prose + a couple of real Go→C# examples, each linking into the reference). Update it
     only when the decision changes the *headline* mapping of a construct or warrants a better/clearer
     example — not for every edge-case fix. Keep it short and readable; push the detail to the reference.

   Do this **in the same change** so both docs keep matching reality. Verify every C# snippet against the
   actual `.cs.target` golden (it is the authoritative record of emitted forms — e.g. `u8` format strings,
   `throw panic(...)`, `ж<T>`/`Ꮡ`); the summary's examples should prefer real snippets pulled from the
   converted stdlib in `src/core` (Go source ↔ converted C#). Skip only for pure bug-fixes that restore an
   already-documented behavior. (This rule is not limited to the regression-test flow — it applies to *any*
   commit that lands a notable conversion decision.)

### Corpus mechanics — measuring/iterating the converted stdlib (`src/core`)
- **⚠ 37 packages are in LAYOUT L3 and the ritual below is UNCHANGED because of it, not despite it.**
  Since 2026-08-08 a package whose emitted C# varies by `GOOS` keeps the varying files in per-GOOS
  subfolders (`<pkg>/{windows,linux,darwin}/`) and its `.csproj` carries a `$(GoTargetOS)` block that
  compiles exactly one of them, defaulting to **`windows`** — so a plain `dotnet build` and a plain
  `-stdlib` reconvert both still mean "the Windows corpus", byte for byte. That default is what keeps
  the seeded-reconvert control honest: a SINGLE-target run **honors** an L3 tree (it writes
  `<name>.cs` back to the `<goos>/` folder the tree already holds it in) rather than laying a flat
  duplicate beside it, so a seeded reconvert of an L3 corpus is still 0 new / 0 absent / 0 content
  differences. Nothing about seeding, the marker gate, the overlay rule or the phantom classification
  changes. What DOES change: an **unseeded** root now breaks layout adoption as well as the marker
  gate (there is no `<goos>/` folder to route into, so every varying file lands flat and the next
  build compiles two copies) — one more reason the seeding is non-negotiable. Hand-owned files are
  routed too, by their principal's platform set; the invariant is guarded by `platformHandOwn_test.go`
  under the plain `go test ./...`. Design: `docs/phase4/DESIGN-multiplatform-corpus.md`.
  **Two L3 gate lessons (measured 2026-08-15, the three-target leveling lane):** (1) a change whose
  files live in linux/darwin per-GOOS folders is NOT compiled by the default windows build — the
  windows `go2cs-stdlib.slnx` gate alone would have skipped 15 of that regen's 27 files, so L3 work
  owes a `-p:GoTargetOS=linux` build too. ⚠ **The darwin half of this note was STALE for ten days
  and is corrected here (2026-09-02): darwin COMPILES CLEAN** — census run 32649840220 at
  `c003d32af`, **zero errors on osx-x64 AND osx-arm64**, the wall history **19 → 10 → 9 → 0**
  closed by lane G within ~24 hours of the first darwin build ever attempted, re-confirmed green
  at master by the 2026-08-25 census (run 32852475367, both legs). The retired text said "darwin
  does not currently build — `os/dir.cs` cannot resolve `File.readdir`, 19 pre-existing errors";
  that is the state of 2026-08-22, and it survived here long enough to be copied into a lane
  prompt and to send a lane looking for a wall that no longer exists. **The darwin census is a
  REGRESSION GUARD now, cheap and dispatchable at any branch tip** (`.github/workflows/os-matrix.yml`,
  `goos=darwin stage=census`), not a wall to census. What darwin still lacks is a RUN layer, which
  is a separate and open question: `docs/phase4/FINDING-darwin-run-layer.md`).
  (2) **A `GoTargetOS` switch poisons `obj/`**: the `<Compile>` item set changes while timestamps
  don't, so an incremental build after a target switch silently validates the OTHER target's
  assemblies — purge `bin`/`obj`/`Generated` between target switches before trusting any build or
  suite that follows one.
- **⚠ The corpus's emission cgo state is `CGO_ENABLED=0`, and a conversion against the committed
  tree must MATCH it** (measured 2026-08-29, net's Linux first contact — a corpus-level fact, not a
  lane detail: `net/linux/cgo_stub.cs` is on disk and `cgo_stub.go` is selected ONLY when cgo is
  off). This is the coherent convention — the converter cannot process cgo C halves regardless (it
  skips toolchain intermediates loudly since the Syntax-pairing fix), and Go's own cgo-off file
  selections are fully functional pure-Go paths. The trap is a MIXED-state tree: converting under
  cgo-ON against a cgo-OFF corpus changes the build-tag file selection, so declarations MIGRATE
  between files while the stale other-selection file remains — measured as a CS0111 duplicate init
  forcer in `net` (the forcer moved from cgo_stub.cs to dial.cs with both on disk). Reads exactly
  like a converter defect; it is an environment mismatch. On any Linux host with gcc (where cgo-on
  is the default), set `CGO_ENABLED=0` before converting against or regenerating the corpus.
  ⚠ **It bites from the TEST side too, and there the state is PER-PACKAGE** (measured 2026-09-02 on
  a Linux lane as a one-variable A/B on `os/user`, whose Go file selection is cgo-conditional): a
  sweep converts under the session's `CGO_ENABLED`, so a cgo-ON run selects `_test.go` files the
  cgo-OFF corpus never carried, leaves untracked `cgo_*_test.cs` artifacts behind, and dies in the
  closure build in ~12 s with **zero verdicts** — a build failure that reads like a conversion
  defect. Both comparison sides must share ONE cgo state, and the converted side can only be the
  corpus's. `run-validated-sweep.ps1` pins it per package (`$cgoOffPackages`, beside
  `$longTimeouts`); a row whose file selection is cgo-conditional joins that table rather than
  depending on the session it happened to run in.
  ⚠ **And a row can AGREE by a COINCIDENCE OF ERRNO** (measured 2026-09-02, one variable proven both
  ways): Go's `AllThreadsSyscall` tests skip on `ENOTSUP` because the ORACLE is cgo-LINKED, and the
  converted side skips on the same word because an unimplemented stub answers `ENOTSUP` — pass/pass at
  cgo-OFF, skip/skip at cgo-ON. So the `$cgoOffPackages` predicate is not only "does cgo change which
  files convert": the ORACLE's own behaviour (its cgo-gated branches) is a second axis in the
  predicate, and every row number carries the cgo state it was taken under.
- **The on-disk corpus can be stale** relative to converter changes made since the last regen; building
  the committed tree measures *that* output, not today's. To measure the current converter you reconvert.
- **⚠ For ADDRESS-OF/ALIASING (`Ꮡ`-machinery) converter changes, a seeded corpus reconvert-and-BUILD
  joins the gate list — CNR alone is not sufficient** (proven 2026-08-15, the element-field-address
  fix: of the three defects in that arc, CNR caught ONE; the other two — a pointer-receiver named-array
  blind spot and its over-broad first fix — appeared in NO behavioral test's shape and were found only
  because the whole corpus was reconverted and compiled. The same census surfaced a real shipped lost
  write, `encoding/xml`'s attribute-namespace translation writing into a copy). The behavioral corpus
  is a SAMPLE of Go's shapes; the stdlib is the population — aliasing changes get measured against the
  population before banking.
- **⚠ Blast-radius measurement for a converter change: TWO seeded reconverts diffed against each
  other, never the committed-tree diff** (2026-08-29, the position-table splitter fix). A naive
  reconvert-vs-committed diff reported 147+ files — almost all PRE-EXISTING unbanked drift from
  arcs that landed without their regens (the standing position-map staleness two census lanes
  rooted independently the same day). Seeding one root at the PRE-change converter and one at the
  CHANGED converter and diffing the two emissions isolates exactly the change's own footprint
  (26 metadata files, zero production code, in the measured case). The committed tree is a moving
  baseline; two emissions of the same sources differ only by the change.
  ⚠ Two mechanical tells the ritual owes on EVERY run (paid 2026-09-01 — a lane's "old" binary
  never existed and the diff silently compared committed-tree-vs-fixed, reporting 724 phantom
  files): (1) `go build -o <path> <dir>` with a bare directory as the last positional can land
  the binary at `<dir>/go2cs.exe` and leave the named `-o` path NONEXISTENT while exiting 0 —
  verify the built binary exists at the exact path you will invoke; (2) before trusting any
  two-root diff, assert BOTH sides' emitted files carry THIS RUN's mtimes — a diff between a real
  reconvert and an untouched seed returns a normal-looking result with nothing marking it invalid
  (the emitted-before-seeded family, build-step edition).
- **⚠ The bank unit for a converter change's corpus footprint is the two-seeded diff's HUNKS, never
  its FILE set (measured 2026-09-02).** Applying the A/B's ten whole files onto a corpus that is
  stale in OTHER families carries those families in with them: the whole-file application landed
  six relocation hooks into one `package_info.cs` while the file that declares them — byte-identical
  between the two binaries, so never flagged — still declared three, and the result was CS0111 ×3.
  Byte-identity to the new emission PASSED and an exact path-set assertion PASSED; neither can see
  a file the diff never named. The tell was arithmetic: **279 applied diff lines against 32
  measured**. Apply the change's OWN lines (the re-done application was 9 hunks / 24 lines, zero
  `GoPositionMap` and zero import-hook lines in the delta, with one untouched package as the direct
  control, and built clean everywhere); position maps and relocation hooks belong to the deliberate
  regen, not to a converter train. ⚠ **And COMMIT the corpus edit BEFORE any sweep** (paid twice in
  one day): a sweep wrapper's restore step (`git checkout HEAD -- src/core`) cannot distinguish your
  uncommitted work from the sweep's own dirt, so hand-applied hunks vanished between two rows and
  the second row failed **invalidly** — a phantom red. Ordering is the fix, not the script; re-run
  the application's own assertions after any restore.
  ⚠ Two corollaries measured 2026-09-02. **"Byte-identical to the emission" is a property of the FILE, not
  of the CHANGE** — copying the footprint files wholesale out of the NEW seeded root is byte-identical BY
  CONSTRUCTION and still wrong, carrying every arc not yet regen'd into the corpus (numstat read 3/9,
  13/31, 3/15, 5/6, 1/7 against a change that owns six lines); numstat is the cheaper instrument, and the
  strongest-looking provenance check cannot see the difference. And **a footprint hunk is the fresh
  emission's STATEMENT even when the committed statement carries another arc's unbanked drift**: carry the
  one inert, byte-verified foreign line and SAY in the commit which line belongs to which arc — a
  hand-written shape no converter emits is worse than one foreign line named. Read the COMMITTED bytes,
  never the seed, before cutting a footprint against a base.
  ⚠ **The hunk rule binds METADATA files too, and the tell is the diff's line KINDS** (measured
  2026-09-02): re-emitting a `package_info.cs` from a `-tests` run imports closure family #2 wholesale
  — 139 `[assembly: go.GoPositionMap` lines became `global::go.GoPositionMap` while the other two GOOS
  folders kept the `go.` form — where exactly ONE map line was owed; counting the diff's KINDS
  (139 −/139 + of a single attribute) caught at the seat what reading it did not. Three companions.
  After ANY pipeline run, every file in the commit is diffed against the PRE-run tree and its line
  kinds counted — an INTENTIONAL file gets the same check as an unintentional one (one lane paid this
  twice in a session, past its own written lesson, because a `-tests` measurement run hands back the
  whole-file form of the metadata file a surgical hunk had touched). A "differs" is CR-STRIPPED before
  it is named (eight runtime files reported as additional drift at one regen were the in-comment-LF
  phantom under a raw-byte `diff -rq`; every one CR-strips identical). And the BEFORE arm of a
  before/after is taken at the CUT'S OWN BASE, never at an earlier landing on the reasoning that "none
  of the intervening merges touch it". A file that is not gofmt-clean at master leaves foreign
  whitespace lines in the next cut's diff: verify they are pure whitespace and NAME them.
- **Reconvert → overlay → build → bucket (the measurement loop):**
  1. **⚠ SEED FIRST — non-negotiable (learned 2026-07-25, cost a false operational-break alarm):**
     `cp -r src/core <tmp>/core` BEFORE reconverting. ⚠ The SEED ITSELF can fail halfway and
     carry on (fleet-confirmed twice in one day, 2026-09-01): `Copy-Item -Recurse` dies on
     go2cs-gen's long `obj\...\Generated\` paths, and under the ritual's own required
     `$ErrorActionPreference='Continue'` the copy continues past the death — a PARTIAL seed,
     which is the unseeded-root hazard through a door this rule never named. Exclude build output
     (`bin`/`obj`/`Generated`) from the seed copy and verify the seeded `.cs` COUNT before
     converting; afterward, an emitted-files control (untouched seeded files reproducing HEAD
     byte-for-byte) is what makes a suspect seed's readings trustworthy. The converter emits a hand-owned
     file as `<file>.cs.auto` ONLY when the `[module: GoManualConversion]`-marked file already
     exists at the output path; an EMPTY temp root gives the marker nothing to detect, so every
     hand-owned whole-file rewrite is emitted as plain `.cs` and the standard overlay rule
     ("copy `*.cs`, exclude `*.cs.auto`") protects NOTHING — 14 hand-owned files get clobbered
     with auto conversions that COMPILE but are operationally broken (godebug's auto `init()`
     throws in a module initializer and takes down every dependent). **Hard gate before
     overlaying — PATH-PRECISE, not a count:** for every `[module: GoManualConversion]`-marked
     committed file, the temp root must NOT contain a freshly-EMITTED plain `.cs` at that path
     (either a `.cs.auto` sits beside it, or nothing was emitted there). Counts intentionally
     differ — **41 marked files (re-measured r44a, 2026-08-07) but only 15 produce `.cs.auto`**; the
     other 26 are `*_impl.cs` companions and hand-owned packages the converter never re-emits at
     that path, so they need no protection. A same-count assertion is wrong in both directions.
     ⚠ The number is NOT stable: it was 40 at r40, fell to **39** when `math/unsafe.cs` shed its
     marker, returned to 40 when `internal/weak/pointer.cs` joined at r43e, and is **41** since
     `internal/cpu/cpu_x86_impl.cs` joined at r44a. This is exactly why the census is re-measured,
     never carried forward.
     **The marker scan must read WHOLE FILES** — a head-window scan (e.g. first 40 lines) reported
     35 marked files against the real 60 (measured 2026-08-17), which would have made the clobber
     gate vacuous for 25 hand-owns; some markers sit below long license/using blocks.
     **The marker scan must be LINE-ANCHORED (`^\s*\[module:\s*(go\.)?GoManualConversion\]`)** —
     `reflect/value.cs` and `internal/reflectlite/value.cs` *mention* the marker inside
     bodyless-partial placeholder comments; an unanchored `grep GoManualConversion` reports **63**
     against the real 40 and turns the gate into a false clobber alarm. (The census moves in BOTH
     directions — 32 at r14, 39 before `internal/concurrent`'s `hashtriemap.cs` joined in r39d, 40
     at r40, DOWN to 39 when the r41 train's regen retired `math/unsafe.cs`'s hand-own without
     saying so — the BitConverter bit casts went back to the auto
     `Ꮡf.Reinterpret<float32, uint32>()`, correct now that `Reinterpret` genuinely aliases managed
     storage, and `math`'s banked **76/76** re-proves it every sweep — and back to 40 when
     `internal/weak/pointer.cs` joined at r43e. Benign in that instance, but a hand-own disappeared
     under an overlay while the commit reported its marker gate "40/0": so re-measure the census,
     never assert last session's number, and treat a SHRINK as something to explain rather than to
     copy forward. ⚠ Since r50a the census counts **42**, and for a NEW reason: layout L3 routes a
     hand-owned file into its principal's per-GOOS folders, and `runtime/lock_sema_impl.cs`'s
     principal is selected on Windows *and* macOS — so one hand-own now exists as TWO files. The
     count of marked FILES is no longer the count of distinct hand-owns; both numbers are fine and
     the gate is still per-PATH. Since **r51b it is 44**: `runtime/lock_managed_impl.cs` (the flat,
     platform-neutral managed core of the mutex/note protocol) and `runtime/linux/lock_futex_impl.cs`
     (the futex flavor's 2-arg `notetsleep_internal`) both carry the marker. Multiple `[module:
     GoManualConversion]` attributes in ONE assembly are legal and already normal — `runtime` alone
     carries eight — so a new marked file never needs to displace an existing one. ⚠ At the r59
     regen bank (2026-08-11) the census is **49 marked files / 41 `*_impl.cs` companions / 59
     distinct hand-owns** — r52–r59 growth over r51b's 44; re-measure, never carry, as always.
     At the Linux regen wave (2026-08-14) it is **53 marked files / 42 `*_impl.cs` companions**,
     0 violations across 3 targets × 2 merge passes. At the post-merge rebank (2026-08-24) it is
     **73 marked files / 49 `*_impl.cs` companions / 24 whole-file rewrites**, 0 violations on the
     windows and the linux target alike.
     The regen ritual also gained a check the seed makes necessary: because seeding puts every
     repo file in the temp root, an overlay can never reveal a file the converter has STOPPED
     emitting — classify emitted-vs-seeded by the sentinel mtime and report would-be deletions,
     which is what surfaced the hand-owned-by-consequence class below.) ⚠ The `.cs.auto` siblings are **tracked in git but are NOT refreshed by the
     overlay**: the same exclusion that protects the hand-owned `.cs` beside them also freezes
     them, so they go stale on their own schedule and are RE-MEASURED at every rebank head rather
     than assumed (CleanupBacklog item 18). The measurement moves: 11 of 16 were stale at r40,
     and **0 of 23 at the 2026-08-24 post-merge rebank** — a seeded reconvert per target re-emits
     each sibling, and CR-stripped equality against the committed file is the test (a raw byte
     compare reports the whole set as differing, because a fresh emission carries the in-literal
     LF the working tree holds as CRLF).
  1a. **⚠ SEED `version.props` AND `docs/validation` TOO, and MIRROR THE `src/` LAYOUT** (added
     2026-08-02 with the README validation badges). Each package README's badge LINE carries two
     badges, and the **Tests** one is composed from two REPOSITORY files, not from the conversion:
     `src/version.props` (the published version that pins the proof URL) and
     `docs/validation/current/<dot-id>.md` (the matched/disclosed counts).
     The converter finds both by the same upward walk it uses for `$(go2csPath)` — version.props at the
     root holding `core/golib`, `docs/` as that root's SIBLING — and emits **no Tests badge at all**
     when either is missing, which is a silent, corpus-wide README diff on overlay. So seed
     `<tmp>/src/core`, `<tmp>/src/version.props` and `<tmp>/docs/validation`, and convert with
     `-go2cspath <tmp>/src` so the temp root mirrors the repository. (Seeding a versioned
     `docs/validation/<version>/` is NOT needed — the badge reads `current/`; the versioned directory
     is only the link target and the `Exists`-guarded pack input.)
     ⚠ The badge line holds FOUR badges and they split two-and-two on this exact question. **Docs**
     (2026-08-08) and **Source·Go** (2026-08-08) read the TOOLCHAIN, not the repository — `go env
     GOVERSION` and, for the 19 GOROOT-vendored `golang.org/x/*` packages, GOROOT's own
     `src/vendor/modules.txt` — so they need no seeding and survive an unseeded root. **Tests** and
     **Source·C#** read the repository's `version.props`, so both vanish without it. That is why an
     unseeded reconvert no longer produces a README with NO badge line: it produces one carrying the
     two toolchain badges alone, which is a subtler diff to spot. Seed anyway; the rule is unchanged,
     only the symptom is.
  1b. `go2cs.exe -stdlib -comments -go2cspath <tmp>/src` → output lands in **`<tmp>/src/core/<pkg>`**
     (the `core` subdir is hardcoded; `-go2cspath` is the *output* root, unrelated to the MSBuild
     `$(go2csPath)`). Full stdlib ≈ 3–4 min (per-file work is sub-second; the cost is `go/packages`
     loading the whole type graph, so **batch** — don't invoke per package).
  1c. **⚠ NEVER convert twice into the same temp root, and never let two conversions overlap in one
     (found r41, 2026-08-05).** A `-stdlib` run whose PowerShell wrapper aborted on the converter's
     stderr WARNINGs — `$ErrorActionPreference = 'Stop'` turns a native-stderr line into a terminating
     NativeCommandError, so wrap the converter call in `'Continue'` or do not pipe its stderr at all —
     left a `go2cs.exe` alive; a re-run into the same root raced it, and the result was ONE corrupted
     file: `runtime/arena.cs` with nine unresolved `«DYNTYPE:…:DYNTYPE»` anonymous-struct lift markers,
     which fails the corpus build with CS1056/CS1003 and reads exactly like a converter regression. It
     is not one — a clean-room reconvert (fresh root, seeded, single run) emits zero DYNTYPE markers
     anywhere in the corpus, and so does a single-package run. The rule is therefore mechanical rather
     than diagnostic: **delete the temp root and re-seed for every reconvert**, and confirm no
     `go2cs.exe` is alive before starting one.
  2. Overlay the fresh `.cs`, **`.csproj` and `README.md`** onto `src/core/<pkg>`. Since the trees
     unified (2026-08-01) the reconvert's paths ARE the repository's paths — a straight copy, no
     rewriting, no exceptions. A seeded reconvert of the whole stdlib is byte-identical to the
     committed tree (2518 `.cs`/`.csproj` verified on the consolidation commit; 300 `README.md` joined
     the byte-identical set on 2026-08-02), so any diff after an overlay is a real converter change.
     Two knowns that are NOT: the SIX root attribution files the converter re-copies (`src/core/README.md`
     and its five siblings — measured 2026-08-17; this note previously named only the one — all show
     modified with an EMPTY `git diff --numstat`, pure CRLF phantoms; restore them), and
     the hand-owned-by-consequence **class of FOUR** — `crypto/internal/boring/bcache`,
     `internal/concurrent`, `internal/godebug` and `internal/weak` (censused 2026-09-01 at
     `3e31de03a` over all 306 production packages; the note previously said three, and before that
     godebug alone — bcache was the member nobody had counted, evidenced by the hand-edited
     position-map hash in its `package_info.cs` at `f1df6cbd9`, which a re-emitting converter would
     never need a human to fix) — each a package whose every non-test Go file is hand-owned, so
     `unmarkedFileCount == 0` makes the driver `continue` before `writeProjectFile` and its
     `.csproj`, `package_info.cs` **and `README.md`** are hand-owned by consequence, never
     re-emitted. (`unsafe` is also fully hand-owned but by the OTHER mechanism — skip-listed.)
     Consequence counted the same day: the hand-own FENCE leaves **8 forced-init hooks missing**
     inside this frozen class (godebug 4, concurrent 3, weak 1) that only Stage B's frozen-README
     option (a) can fix — the relocation cannot, since these `package_info.cs` are never re-emitted.
  3. Build single packages with **`dotnet build <pkg>.csproj -c Debug`** — `src/core/Directory.Build.props`
     pins `$(go2csPath)` to the src root, so `core\golib` + the `go2cs-gen` analyzer resolve to live source
     with **no `-p:go2csPath` flag**; or build the whole `go2cs-stdlib.slnx` (~92–150 s warm, 305 assemblies — the 306th, `crypto/x509/internal/macos`, is darwin-exclusive and compiles nothing under the default `$(GoTargetOS)`).
     (If you ever do pass the flag explicitly, use forward slashes —
     `-p:go2csPath=H:/Projects/go2cs/src/` — a trailing `\` escapes the closing quote and mangles the path
     into phantom golib-not-found errors.)
  4. Bucket: `dotnet build … -clp:ErrorsOnly` then group by `error CS####`. Errors shown are *own-errors*
     of leaf-most failures — dependents of a failed project are skipped, not errored.
- **⚠ csproj I/O (2026-07-25):** any script that rewrites a csproj must read AND write with explicit
  UTF-8/no-BOM (`[System.IO.File]::ReadAllText/WriteAllText` + `UTF8Encoding($false)`) — PS 5.1
  `Get-Content` reads the converter's BOM-less UTF-8 as ANSI and `Out-File utf8` re-encodes the damage,
  double-encoding the `©` in `<Copyright>` on every pass (this is what created, then tripled, the
  258-file corpus mojibake; root-caused and leveled in the r11 bank). Python has the same trap in
  the OTHER direction: `utf-8-sig` STRIPS a BOM on read but always ADDS one on write, so a
  read-sig/write-sig round trip silently BOMs a BOM-less file (caught 2026-08-31 by the
  hand-application byte-identity bar during a probe restore). Three encodings, three silent
  corruptions — PS 5.1 ANSI, UTF-16 redirects, utf-8-sig — one rule: byte-compare any
  restore/round-trip against the original before trusting it.
- **Metric:** measure **packages-compiling**, not raw error count. Fixing file-inclusion bugs (e.g. the
  filename build-constraint fix) *raises* the error count because newly-included files surface their own
  latent defects — that's progress, not regression. The claim "my fix caused N new errors" is
  therefore never banked without the five-minute control (named 2026-09-01, after a
  substantially-correct fix was discarded on the misread): REVERT the fix, build PAST the original
  blocker, and see whether the "new" errors are still there. Unmasked errors appear precisely where
  compilation could not previously reach — i.e. in files OTHER than the ones you touched — so "the
  errors are in different files from my change" is evidence of unmasking, never evidence of
  causation.
- **A corpus regen that moves `package_info.cs` records owes `go generate .` in `src/go2cs`** —
  `stdlib-metadata.txt` is generated FROM the corpus and gated by `TestStdLibMetadataInSync` under the
  plain converter `go test`, so banking a regen without the regenerate leaves the converter gate red at
  master for whoever runs it next (happened 2026-08-15: the second leveling regen moved 6 records and
  the drift surfaced in an unrelated lane's gate run). Regenerate, verify the test, commit together.
- **Don't commit corpus regens casually.** `src/core/<pkg>` is regenerable; the unit of work is the
  **converter fix**. Keep the tree restorable (overlay into a branch or restore with `git checkout HEAD --`
  + remove untracked) so a converter-fix commit isn't buried under thousands of generated-file changes.

## Current state & known issues

- **One tree since 2026-08-01:** the stub baseline retired and the converted standard library moved into
  `src/core`. `src/go2cs.slnx` builds clean and the behavioral suite is GREEN against the CONVERTED
  packages — **547/547** transpile+compile+golden, **521/521** stdout comparisons vs `go run`
  (r43g, 2026-08-07; 26 skipped, no `package main`). All rewrite machinery is gone: one path scheme,
  `$(go2csPath)core\<pkg>`, everywhere.
- **Windows local time works (fixed 2026-08-01).** Binding the converted `time` exposed a pre-existing
  crash the stub had hidden: `time.Now().Weekday()` → `initLocal()` → `syscall.GetTimeZoneInformation`
  access-violated, because the wrapper hands the kernel the address of a managed `Timezoneinformation`
  whose `array<uint16>` name fields are managed references where Windows expects inline `WCHAR[32]`. That
  wrapper is now hand-owned against a blittable mirror (`core/syscall/windows/zsyscall_windows_impl.cs` — per-GOOS since r50a), guarded
  by the `LocalTimeZone` behavioral test — which compares real zone abbreviations and offsets against
  `go run`, not merely the absence of a fault. **The CLASS is still open, and it is now TWO classes**
  — wrappers passing a non-blittable struct by ADDRESS (the layout defect above), and wrappers taking
  a `**T` OUT-parameter, which arrive as NULL because `ж<T> → uintptr` answers 0 for a heap-boxed
  pointer that is still nil. The running census, the per-member remedy and why they are deliberately
  NOT fixed speculatively live on
  [`docs/phase4/BOARD-next-validation-candidates.md`](docs/phase4/BOARD-next-validation-candidates.md);
  re-measure there rather than carrying a count. The old note said "nothing exercises them today;
  `net` and `crypto/x509` will" — both now do: `net`'s DNS path forced `GetAddrInfoW`/`FreeAddrInfoW`
  (fixed 2026-08-16, guarded by `LookupServicePort`), and `crypto/x509`'s Windows system verifier is
  the measured consumer of the OUT-parameter class. Two walls stood behind them, both `net` /
  `crypto/x509` arcs rather than syscall ones — a **third** fork, where the kernel memory is a byte
  buffer the CALLER reinterprets, so no wrapper is at fault and no mirror-the-wrapper remedy applies.
  The first is CLOSED: `net.adapterAddresses` walked a native `IP_ADAPTER_ADDRESSES` chain out of a
  managed byte buffer and killed the process on the loop's own nil test; it is hand-owned since
  2026-08-17 (`core/net/windows/interface_windows_impl.cs` transcribes the whole chain — every
  record, its six nested lists and every sockaddr — into managed boxes), guarded by the
  `IpAdapterAddresses` behavioral test, and it is what unblocked Windows name resolution at all,
  since `dnsReadConfig` is `getSystemDNSConfig`'s only source of DNS servers. The second is still
  open: the CryptoAPI chain walk reads `CertContext` / `CertChainContext` back through raw addresses.
  ⚠ **The class has its ROOT, and it is not "one word where four bytes belong" (measured 2026-09-02):
  the CLR gives AUTO layout to any struct holding a reference-typed field and REORDERS it, so the
  KERNEL READS THE WRONG FIELD.** Converted `Msghdr`'s `Namelen` sits at managed offset 40 while the
  kernel reads `msg_namelen` at 8 — where it finds `Iov`, an object reference and therefore never zero:
  EISCONN on a connected stream, EINVAL on a datagram; `RawSockaddrInet4`'s `Addr` at managed offset 8
  is the heap pointer earlier instrumentation dumped. A correctly laid-out struct (`Iovec`) can still
  hand the kernel managed addresses, so the remedy is to ENCODE into a native buffer (a
  `writeNativeSockaddr`) or an explicit-layout blittable mirror — never a managed struct passed by
  address. A three-arm A/B (hand-own / generated body restored / hand-own back, `--no-incremental`) is
  what turns a "does not reproduce" into an attribution.
- **Phase 3 complete (2026-07-10 — commit `51ba5d9cf`, tag `stdlib-green-2026-07-10`):** all **302**
  packages of the full conversion (Go 1.23.1) compile clean — zero errors, zero
  exclusions (`runtime`, `reflect`, `net/http`, `go/types`, `crypto/tls`, `database/sql`, … all included).
  **Compiling is the milestone, NOT operational** — operational validation is Phase 4 (running Go's own
  package tests). Campaign detail: [`docs/Roadmap.md`](docs/Roadmap.md) (Phase 3 iteration log) and the
  [`docs/README.md`](docs/README.md) NEWS section.
  - **Promotion happened, once, wholesale (2026-08-01) — superseding the 2026-07-01 defer.** That ruling
    said not to promote package-by-package on a clean compile, and it was right: the chicken-and-egg it
    guarded against was real while the corpus was unproven. Phase 3 plus 69 operationally-validated
    packages dissolved it, so the whole tree moved at once instead. The hand-owned
    `[module: GoManualConversion]` / `*_impl.cs` files now simply LIVE in the one tree — no canonical copy,
    no overlay-back step, no two-tree exceptions.
  - **⚠ Swapping a hand-own's file contents (backup restore, A/B neutering) can leave a STALE dll
    winning the build**: `Copy-Item` preserves `LastWriteTime`, so the restored source is OLDER than
    the assembly built from the neutered version and incremental MSBuild keeps the wrong dll — a
    defect then "reproduces" against clean, HEAD-matching source with a clean `git status` (measured
    2026-08-16, cost one invalid run). After any hand-own swap: touch the file or build
    `--no-incremental` before believing a repro.
  - **⚠ A hand-APPLIED edit to a generated file must be proven BYTE-IDENTICAL to the converter's
    own emission before it banks** (standing bar, ruled 2026-08-31). Regenerate into a seeded root
    and byte-compare the hand-applied file against the emission: the first measured
    hand-application was ONE BLANK LINE short of what the converter emits, and without the check
    the next regen reports that cosmetic delta as drift and bills a phantom investigation to
    whoever runs it. The comparison is only meaningful when the emission actually landed in the
    compared root (see the single-package output-positional trap above) and only after the gate's
    negative control has been made to fail once.
    ⚠ **Two control-FORM rules, both measured 2026-09-02.** An ABSOLUTE "byte-identical to the committed
    file" control is unsatisfiable under standing corpus drift — three banked rows' `-tests` emissions
    changed WITH a cut and WITHOUT it (closure drift plus relocation debt), so a no-op would have failed
    the gate; the DIFFERENTIAL form (emission with the change vs without) is the one that carries
    information, and the five-minute control (revert, re-emit) runs BEFORE any violated control is
    reported. And **a positive control's premise must hold at the CONVERTER the measurement used**: "the
    landed hunk must reproduce with zero diff" assumed a binary carrying the merge while the
    measurement's binary was built pre-merge — it failed for its premise, not for the instrument. Name
    the converter a control assumes, and say which form the control took.
  - **⚠ Phase-4 operational: two hand-owned patterns, and a WHOLE-FILE rewrite MUST carry the marker.** Making a
    package *run* (not just compile) often needs a native reimplementation where the literal conversion compiles
    but cannot work — e.g. `sync`'s Mutex/RWMutex/WaitGroup (2026-07-11), whose Go runtime sleeping semaphore
    cannot be emulated, are hand-rewritten on `SemaphoreSlim`/monitors. A `<name>_impl.cs` companion
    *supplements* some declarations (bodyless `partial` + a comment placeholder the converter emits); a
    **whole-file** hand rewrite *replaces* the converted `<name>.cs` and **must carry `[module:
    go.GoManualConversion]`** — else a `-stdlib` reconvert regenerates the Go version over it (`main.go`'s
    `containsManualConversionMarker` drops marked files from the convert set; place it after the `using`s,
    before the file-scoped namespace). Further hand-own detail:
    [`docs/ConversionStrategies-Reference.md`](docs/ConversionStrategies-Reference.md) (the two-tree history
    is archived at `src/archived/Baseline-vs-FullConversion.md`).
    ⚠ **Two displacement mechanisms, priced differently (2026-09-02) — and read the NEIGHBOURING
    rulings before sizing one.** A BODYLESS `public static partial` (a linkname-declared destination)
    is displaced simply by WRITING a body: `PartialStubGenerator`'s predicate is
    `IsPartialDefinition && PartialImplementationPart is null`, so the throwing stub steps aside BY
    CONSTRUCTION — no `manualConversionFuncs` entry, no converter change, no two-seeded diff (though a
    change that removes generated stubs still owes a behavioral COMPILE, route #7's neighbourhood). A
    BODIED converted function is displaced ONLY through that registry — a converter change, with a
    two-seeded emission diff and a hunk-only corpus footprint. The cheap-looking third option (mark the
    whole file `GoManualConversion` and edit in place) freezes every function in the file to optimise a
    few and creates a permanent hand-merge obligation: rejected by the minimal-footprint rule even
    where the file is stable. Accessibility follows the pattern already in the tree — a `Go`-prefixed
    PUBLIC helper per operation, native mirrors PRIVATE to the seam file, so no consumer assembly sees
    a native type — and a hand-own's own "deliberately not covered" scope header is re-read and
    corrected in the SAME commit that changes the scope (one still named two functions that had been
    hand-owned three days and one hour earlier; a scope header that lies reads as the census).
    ⚠ **And an EMPTY body is not a no-op when the throwing stub was a BRAKE** (measured 2026-09-02):
    empty `runtime_BeforeExec`/`AfterExec` bodies were argued correctly from `execLock`'s readers and
    FORK-BOMBED the `syscall` row (96 children in 7 minutes), because `Exec` hands `execve` MANAGED
    argv/envp, the exec'd image comes up with garbage argv and an empty environ, loses its
    `-test.run`/helper-process markers and re-runs the whole suite — and a CHAIN of child processes is
    itself proof `execve` did not replace the image (it keeps the pid). "Semantically sound" and "safe"
    are two claims: a cut touching `execve` runs under a process ceiling, withdraws first and analyses
    second, and the marshalling fix precedes any body.
    ⚠ **Which FORMATTER runs decides what a converted test PRINTS, and a hand-own's private
    reimplementation of a stdlib contract diverges silently** (measured 2026-09-02): the hand-owned
    test host carries its own verb dispatch (`TestFormat.cs`) with a SMALLER contract than `fmt`'s —
    `#` parsed and dropped, `%T` of nil as `nil` — so every PRODUCTION-dimension control was green by
    construction, because production calls the converted `fmt` and the test dimension never does. Once
    the converted package banks, the hand-own DELEGATES to it rather than carrying a second
    implementation. The measurement that settled it was a probe printing ZERO lines where the failure
    reproduced: a function the path never enters is falsified by its own silence.
  - **⚠ The S1/CS0030 "architectural wall" was a FORK, not a wall (2026-07-01) — and the fork held to 302/302.**
    **Native-type** pointer/unsafe ops (identical memory semantics in both GC languages) get a faithful
    conversion in the converter/`golib`. **Managed-referent** cases (`guintptr`/`muintptr`/… hiding a managed
    pointer in a `uintptr`) hold the `ж<T>`/`object` **directly** (like `core/sync/atomic` `atomic.Pointer<T>`),
    never a `nuint` round-trip. Genuine **raw-metal on non-native types** (memory-layout math, type-descriptor
    walking, `*.asm`) is stubbed with `[module: GoManualConversion]` (a stub that compiles is an acceptable
    milestone solution).
  - **Next — Phase 4 (operational):** convert and run Go's own `_test.go` suites against the compiling
    packages; design in [`docs/TestingInfrastructureRequirements.md`](docs/TestingInfrastructureRequirements.md)
    and Phase 4 of [`docs/Roadmap.md`](docs/Roadmap.md). The `-tests` pipeline is live (`go2cs -tests
    -test-action all <goroot-pkg> <src/core-pkg>`): converts `_test.go` variants, builds a
    hand-owned `go.testing` host (`src/core/testing`), runs it isolated, and diffs terminal results
    against `go test -json`. `-tests` **always forces `-comments`** (test conversions are derivative
    works — the per-file Go copyright header must survive) and **self-locates `$(go2csPath)`** by walking
    the output dir up to the tree root (so the two-arg command works from a bare clone, no env). First
    validated package: `unicode/utf8` (2026-07-17, tag `utf8-tests-green-2026-07-17`).
  - **⚠ Validated-package commit policy (2026-07-17 user ruling):** when a package's Go test suite
    **validates** through the pipeline, COMMIT its converted C# test sources into
    `src/core/<pkg>` beside the production code — `*_test.cs`, `package_test_info.cs`,
    `go2cs_test_host.cs`, `<pkg>.tests.csproj` — so the passing suite is **visible and reviewable on
    GitHub**, and reproducible via the [README "Try it yourself"](docs/README.md#try-it-yourself--validate-a-converted-test-suite)
    instructions. The pipeline's regenerated inputs/outputs are **git-ignored** by
    `src/core/.gitignore` (the staged `*.go` source copies + `go2cs_test_manifest.json`
    [machine-specific exe-hash digest] + `go2cs_test_comparison.json` +
    `go2cs_test_results.json`/`.xml`). The production
    `<pkg>.csproj` also updates on this run (the IP-4 test-artifact `<Compile Remove>` exclusion) — that
    change is intended, not drift. Refresh the committed test sources at each milestone rebank alongside
    the production tree.
  - **⚠ After an operational SWEEP, `git status` is dirty and it is (almost always) NOTHING — classify,
    don't chase, and never bank it.** Two *different* phenomena get conflated here; a sweep produces only
    the first. ⚠ **The dirt is NOT confined to `src/core`: the sweep REWRITES each swept package's
    proof page under `docs/validation/current/`** (that rewrite is by design — it is why the
    host-conditional check reads the COMMITTED page from HEAD), so a restore scoped to the corpus
    leaves the pages behind — 56 of them measured on one lane's shift (2026-08-29). Restore both
    roots, or the next diff reads as proof-page drift.
    1. **CRLF phantoms — most of a healthy sweep's dirt.** The converter preserves the Go source's
       **LF** inside multi-line string literals while emitting CRLF everywhere else, and `core.autocrlf`
       smudges those in-string LFs to CRLF on checkout. A `-tests` run re-emits them as LF, so every
       banked file containing a multi-line literal shows **modified with no diff hunks at all** — it
       does not even appear in `--numstat`. Do **not** memorize a file list; the count tracks how many
       banked packages hold multi-line literals and grows with every bank (15 at the 47-package roster,
       16 once strconv's `testdata/testfp.txt` joined, **5 + 10 at the 73-package roster** — see the
       split below). *Positive control:* `git diff --numstat HEAD~1` must be non-empty, or your check
       is broken, not clean.
       ⚠ **The "numstat must be empty" rule is FALSE for `-text` paths (found r40, 2026-08-04).**
       `src/core/compress/testdata/*` is marked `-text`, so git does **not** normalize it and a pure
       CRLF flip shows a **real, non-empty numstat** (`gettysburg.txt` 29/29) that reads exactly like
       content drift. Verbatim `testdata`/`*.s` copies are therefore a SECOND phantom shape: test
       CR-stripped equality against `HEAD` directly rather than trusting `--numstat`.
    2. **`-tests`-CLOSURE production files.** A handful of production `.cs` differ between the two
       emissions (`Δio` alias, `global::go.*` root escape, the using-block REORDER the alias
       causes, and — the FOURTH shape, named 2026-08-17 — the `initᴛᴛtests()` hook a `-tests` run
       adds to a package's `package_init.cs` as +7 REAL lines `-stdlib` omits, which survives a
       numstat check that filters phantoms; same class, restore it — **AMENDED 2026-08-26,
       ratified at the leveling-rebank floor: for a row whose test sources are REBANKED at or
       after the init-order arc, the `initᴛᴛtests()` hook is BANKED, not restored** — a
       re-derived suite does not compile without it, so those packages' `package_init.cs` rests
       on the `-tests` side and it is the `-stdlib` overlay ritual that must classify-and-KEEP
       it; the restore rule stands only for rows still carrying pre-arc sources) because the
       `-tests` closure imports more. **Staging corollary (paid for 2026-08-26): never
       `git add -A`/`git add .` on a tree that has had a sweep or `-tests` run against it — name
       the paths; the hook shape survives numstat filters and lands in the commit silently.**
       Two additions, 2026-08-29/30: the **FIFTH shape** — a `GoPositionMap` funcLit/range
       argument the `-tests` emission adds and `-stdlib` omits (rooted independently by two
       lanes the same day; survives numstat filters; evidenced by banked `cookiejar` carrying
       it) — same class, classify-and-restore per the side the tree rests on. And ONE-WAY
       emission changes are NOT closure shapes: the `-tests` init-forcing hook (+7 lines in
       `package_test_info.cs`, landed 2026-08-30) appears at a row's next test-source
       REGENERATION and stays — 193 reference-model banked test infos are stale-until-rebank by
       design, no standing restore, and the rebank wave that levels them owes the full-roster
       sweep (the throwing-production-init regression shape can only materialize there).
       Whether a sweep SHOWS them depends on which
       side the committed tree currently rests on, so do not treat either state as the invariant:
       when the tree rests on the `-tests` side they are invisible to a sweep and surface only under
       an `-stdlib` reconvert control; when it rests on the `-stdlib` side — **where r40 left it** —
       every sweep flips them and they must be **RESTORED**. Measured at r40: **13 files**, wider than
       the six recorded in [`docs/phase4/DESIGN-named-interface-wrappers.md`](docs/phase4/DESIGN-named-interface-wrappers.md)
       §7 — also `bufio/{bufio,scan}.cs`, `crypto/md5/{md5,md5block}.cs`, `regexp/{regexp,exec,backtrack}.cs`.
       Both emissions are correct for their own closure — only the pipeline pairs them — so this is a
       STANDING restore, not a one-off cleanup, until the two agree on one alias per import.
    3. **`.cs.auto` review siblings.** Tracked, and refreshed by a `-tests` run but NOT by an
       `-stdlib` overlay (which excludes them to protect the hand-owned `.cs` beside them). Restore
       them in a sweep; re-measure the whole set at each rebank head, one seeded reconvert per
       target, rather than banking a count (CleanupBacklog item 18).
    4. **Deduplicated same-shape anonymous structs — LEVELED, so a reappearance is news.** The
       converter binds a second anonymous `[GoType("dyn")]` struct of identical shape to the FIRST
       declaration's type instead of minting its own (`e61758549`, the reflectlite arc). In a diff
       that reads as the duplicate `[GoType("dyn")]` block vanishing while the slice and element
       types rename onto the original's `ᴛ1`, with a knock-on in `package_test_info.cs`, whose
       witness list sheds the declarations that no longer exist. A suite banked before that commit
       keeps the old shape until its own pipeline rerun; the 2026-08-24 post-merge rebank ran the
       last five (`math/cmplx`, `go/build/constraint`, `regexp`, `strings`, `time`). Unlike classes
       1–3 this one does NOT stand: it is banked, so meeting it again means a NEW unbanked converter
       change — find that commit rather than restoring the file.
    Anything that is none of these — a non-empty `numstat` on a production `.cs` that is not a closure
    re-flip, or any change to a production `.csproj` — is **real drift**: stop and root-cause it before
    landing. (A production-`.csproj` change specifically meant the validation-pack block had been
    stripped; fixed in `ce82093b0` and proved clean across the full r40 sweep.)
    ⚠ **After a `-tests` run a package directory holds THREE populations — tracked corpus files,
    tracked hand-owns, and untracked generated emission — so any glob- or directory-wide operation hits
    the wrong one** (paid twice, 2026-09-02). `rm -f src/core/reflect/*_test.cs` deleted the TRACKED
    `export_impl_test.cs` hand-own (the glob encoded "test files under a converted package are
    generated" — true for 13 of 14), and `git checkout -- src/core/reflect` reverted the lane's own
    guard edit in `value_impl.cs`. Restore by FILENAME, clear emission with `git clean -nd` then `-fd`
    — the primitive that reads the tree's state beats the pattern encoding a belief about it.
- Open converter items: `src/go2cs/ToDo.md` (e.g. `visitMapType` completion, remaining dynamic-struct
  implicit-cast checks, optional recursive dependent-package conversion, comment conversion, cgo/asm targets).

### Deploying the core to the GOPATH root
`src/deploy-core.ps1` (cmd launcher `deploy-core.bat`) stages the runtime + standard library at
`%GOPATH%\src\go2cs` so converted projects — and, later, recursively converted end-user apps that target
that same root — resolve their `$(go2csPath)core\<pkg>` / `gen\go2cs-gen` references relatively. It has ONE
mode since the trees unified (the old `stub`/`stdlib` argument is gone): a straight copy of `src/core`,
because the repository layout and the deployed layout are now the same layout and no reference needs
rewriting. It also deploys the `go2cs-gen` analyzer, writes a root `Directory.Build.props` that pins
`$(go2csPath)` to the deploy root (so no `-p:go2csPath` is needed), generates `go2cs-core.slnx`, and builds
to verify. The other src PowerShell utilities `clean-bin.ps1` (remove bin/obj/Generated) and
`set-version.ps1` each also have a `.bat` launcher.
⚠ Purge with that instrument, or with an explicitly depth-UNLIMITED walk: an ad-hoc
`find … -maxdepth 3` purge missed 274 of 388 output directories and drove a lane's disk into the
harness's own free-space floor (2026-09-02).
**⚠ Its default target is MACHINE-GLOBAL** — `%GOPATH%\src\go2cs`, shared with every sibling worktree —
so never run it bare as a gate. It supports **`-WhatIf`** (a real dry run: the three non-cmdlet writes
are explicitly `ShouldProcess`-gated, and the solution enumeration reads the SOURCE so the projected
project count is truthful) and **`-Target <dir>`** for a scratch deploy. The copy is pure PowerShell
(`Copy-SourceTree`) rather than robocopy since 2026-08-08, and is byte-identical to what robocopy
produced (3,979 files A/B-verified); that also removed the repository's last external Windows tool
dependency. Harness path/platform primitives (`$IsWindowsHost`, `$ExeSuffix`, `$SepPattern`, the roots,
`Get-PathDepth`) live in **`src/_paths.ps1`**, dot-sourced by every instrument — never re-derive them,
especially `$IsWindowsHost` (`$IsWindows` does not exist on PowerShell 5.1, so a bare `-not $IsWindows`
reads backwards on the one platform 5.1 runs on).

### Known staleness (do not trust blindly)
- `docs/README.md` is a **maintained visitor surface** — its NEWS block is current and its references are
  corrected. What is historical is its milestone table's ANTLR-era rows: read those as history, not as
  instructions. (The older "carries a banner" note here was itself stale; there is no such banner.)
- The retired `net6.0` C# converter scripts (`src/convert-gosrc.cmd` / `convert-gosrc.bat`) were **removed**
  2026-07-11; the current converter is the Go build with the flags listed above.

## Conventions

- **⚠ SECURITY — no real machine names or other internal-infrastructure identifiers on ANY pushed
  surface (owner order, 2026-09-01).** Every committed file, mailbox entry, commit message and branch
  name refers to fleet machines ONLY by their nicknames — `R-LAPTOP`, `G-LAPTOP`, `i9`,
  `i7`/`coordinator`. Real hostnames, UNC paths carrying them, and any other detail that exposes the
  owner's internal network (share names, non-public usernames) stay off GitHub entirely. The
  2026-09-01 scrub replaced every occurrence at both public tips (master and the mailbox branch); git
  HISTORY retains the originals (owner-accepted) — so never reintroduce one by quoting a pre-scrub
  record verbatim: re-census with a case-insensitive grep before banking any doc that copies old text.
  ⚠ **Two scrub rules paid for on 2026-09-02.** (1) **A SCRATCH-directory transpile's emission is not
  postable**: it records an ABSOLUTE source path in `GoPositionMap` (the committed file carries the
  relative `main.go`) and drops the hand-added `[GoTestMatchingConsoleOutput]`, so such a
  `package_info.cs` is never copied into the corpus and never pasted onto a pushed surface — one was,
  carrying a profile path plus worktree and session layout, and had to be scrubbed off the mailbox.
  Post emissions from a repo-relative run, or redact before posting. (2) **The pre-post grep covers
  the PATTERNS you quote, not only your prose** — a post that described its own census as
  `<name>|<profile-root>|/home/` spelled the real account name onto the pushed surface — and a
  security census of the mailbox reads `origin/claude/mailbox` after a VERIFIED fetch (an
  already-scrubbed line was re-reported from a stale copy). Census case-insensitively over BOTH
  profile-root spellings and `/home/`.
- C# style: see [`docs/coding-style.md`](docs/coding-style.md) (Allman braces, 4 spaces, `m_`/`s_`/`t_`
  field prefixes, explicit types over `var`, language keywords over BCL types, `\uXXXX` for non-ASCII).
- Conversion strategy: [`docs/ConversionStrategies.md`](docs/ConversionStrategies.md) — a high-level,
  example-driven **summary** of how each Go construct maps to C#; each section links into the exhaustive
  [`docs/ConversionStrategies-Reference.md`](docs/ConversionStrategies-Reference.md) for the full detail.
- Process/gate terminology as used in commit messages and reviews (CNR, A/B footprint, census,
  chip, guard, golden, overlay, banked…): [`docs/Glossary.md`](docs/Glossary.md).
- Generated C# intentionally targets Go-like *behavior first* (no implicit async), and Go-like *appearance*
  second (extra machinery hidden in partial classes / generated files).

### Integrating concurrent lanes (the hazards that do NOT show up as conflicts)

When several machines work the same tree, the dangerous merges are the ones git reports as clean.
Each rule below was paid for.

- **⚠ Two lanes solving the same problem produce a SILENT DUPLICATION, not a conflict.** Independently
  added blocks land at different offsets under different names, so git merges both as ordinary
  additions and marks nothing. Measured 2026-08-24: two independently written 17-element apply-set
  arrays in `migrate-tfm.ps1` auto-merged *cleanly*, and the result would have appended every site to
  `$applySites` **twice**. The mirror case bites from the other side — a symbol introduced OUTSIDE the
  markers (`$shadowed`) is left undefined at its use site if you take one side of a marked hunk. Neither
  is visible from the conflict markers. **Resolving the marked hunks is not resolving the merge: read
  the merged file whole, and run the thing.**
  ⚠ **The shape reaches CUTS, not only merges** (2026-09-02): the same two-line guard fix was written
  twice within one hour by two sessions, caught only because both announced before either merged — the
  author's branch was taken and the duplicate deleted. A coordinator-critical fix to a LIVE lane's own
  file is announced as an ASK to that lane first.
- **⚠ An INSERT adjacent to a line the other side edited folds into ONE hunk, and BOTH single-side
  resolutions silently lose a line** (measured 2026-08-29: master inserted the `go/build` roster row
  directly above `go/build/constraint`, which the branch had annotated — `--ours` dropped the new
  row entirely, `--theirs` dropped the annotation; nothing in the markers says a line vanishes, so
  it reads like an ordinary either/or). Resolve by keeping BOTH sides' content, then **assert the
  structural invariant** (row/line count before == after + known inserts) instead of eyeballing it —
  and validate any re-derived aggregate by **positive control against a known-good blob first**: the
  same derivation must reproduce the other side's banked value exactly before its new value is
  believed. A derivation that cannot reproduce a known-good value is not a derivation.
- **⚠ Its MIRROR is the SILENT SUBTRACTION, and it is worse: one lane REMOVES a definition because
  another branch supplies the replacement, and the merge drops the supplier.** Both diffs are pure
  additions/removals, git merges them without a conflict or a warning, and the result compiles
  nowhere. Paid for 2026-08-29 (`syscall.Uname`): the converter registration that DISPLACES the
  generated wrapper merged, the hand-own `*_impl.cs` BODY it displaces to did not, and the whole
  **linux corpus went RED at master with a clean `git status`** — `kernel_version_linux.cs`
  CS0117 `'syscall_package' does not contain a definition for 'Uname'`, discovered days later by a
  lane building that flavor, not by any merge. Note this is one step PAST the regenerate-never-merge
  seam rule the guilty file's own header documents: the generated side was correct, the destination
  was missing. **Mechanical preflight, cheap and now owed:** if
  `git diff --name-only <base>..<branch>` shows a `manualConversionFuncs` registration or a
  generated-body deletion, **assert the matching `*_impl.cs` body is present in the MERGE RESULT** —
  the same shape as the `package_info.cs` ⟹ `stdlib-metadata.txt` preflight above.
- **⚠ A seam check that verifies a displacement HAPPENED but not that its destination EXISTS passes
  the exact failure it was written for, in mirror form.** The ten-names/zero-bodies property offered
  as the struct-passing merge instrument — every registered name has zero generated bodies and
  exactly one placeholder — was run twice over all ten names and reported as the check that would
  catch a lost registration. It is ONE-SIDED: a placeholder pointing at a hand-own body that does not
  exist passes it cleanly, which is exactly what master held, and the branch carrying the check
  carried the same gap (2026-08-29). **Every seam check carries both sides of the ledger** —
  registration ⇒ displaced wrapper ⇒ body, and the reverse (a dead hand-own nothing displaces) where
  the shape allows it cheaply. Put it in the tier every lane already pays for (the converter's own
  `go test ./...`, beside `projitemsIntegrity_test`) so the class turns into a red converter suite at
  the merge rather than a red corpus later.
  ⚠ **And check what the check's WITNESS is made of.** A displacement guard whose witness is
  ON-DISK placeholders is ENVIRONMENT-dependent for TEST-side hand-owns: it passes on a tree that
  has run that package's `-tests` and fails on every clean clone, because an unbanked row has no
  committed test emission (2026-09-02). Ruled remedy: a GOROOT `_test.go` witness arm, matched by
  CLASS rather than by name and counted separately as the weaker witness; the production arm is
  unchanged.
  ⚠ **The ledger's REVERSE arm earns its keep on hoisted literals** (2026-09-02): the converter hoists a
  body's string literals WITH the body, so a displaced body's `…ˢ` literals cease to exist and any
  hand-own referencing one dangles — the reverse side found it before a compile did; a hand-own spells
  its own panic text and depends on no hoist the displacement removes. ⚠ And a **linkname destination
  declared in a `_test.go` lands in the INTERNAL-test class, where a production-side push cannot reach
  it** (`reflect`'s `gcbits`, provided by runtime via linkname: emitted bodyless into the internal-test
  package and picked up by the throwing partial stub) — completion is the reflectlite pattern,
  registration plus a body in `export_impl_test.cs`, witnessed by the guard's test-side arm.
- **A branch behind master shows master's newer files as DELETIONS.** This is the stale-base illusion,
  not data loss, and it has been mis-read as a lane destroying work more than once. Diff from the
  **merge base** (`git merge-base A B`), never from a moving tip.
- **Check the diffstat against the claim BEFORE the push, never after.** A merge whose file list does
  not match what the commit says it does is stopped at that point, not explained afterwards.
- **⚠ A resolver that FAILS must stop the commit** (2026-09-02): a conflict-resolver script's
  assertion failed and the `git add; git commit` chained after it with `;` rather than `&&` committed
  a board carrying three conflict markers — caught only by the marker count printed beside the
  commit, and amended before the push. Chain `python … && git add … && git commit`, and grep every
  merge commit's blobs for `^<<<<<<<` before pushing.
  ⚠ **A docs seat can split a file's FINAL guard line, and no gate sees it** (2026-09-02): the board's
  closing `endraw` guard was deleted, a bare HTML-comment opener written, 284 lines appended and the
  tail half re-added LAST — so the new section published INSIDE a comment, invisible, while the commit
  read normally. A board-touching merge asserts the structural invariant before it lands: one `raw`,
  one `endraw`, the `endraw` FINAL, zero bare openers.
- **A separated stack must be verified from BOTH branches** — `git log --oneline master..<branch>` on
  each shows what a merge would really carry.
- **Re-fetch immediately before any merge in a live campaign.** Refs move under you; arithmetic against
  a SHA you read ten minutes ago is arithmetic against a tree nobody has. ⚠ A rebase REWRITES a SHA
  someone else has already been handed: **never force-push a tip whose SHA has been posted — post
  the fresh SHA first** (paid twice in one day, 2026-09-01, the second time crossing a coordinator
  merge that was reading the old one).
  ⚠ **And the rule binds a FAST-FORWARD too** (2026-09-02, three pushes announced AFTER the fact on the
  reasoning that the announced SHA was still reachable): the reader takes the REMOTE TIP, so an ADD
  moves the thing being read exactly as a rewrite does. The form is **announce, THEN push**, for any
  commit on a branch whose SHA has been posted, whatever the update's shape — and a sentence in an
  already-announced commit is retracted in the MERGE message, never by rewriting the SHA.
- **Three merge mechanics, measured 2026-09-01/02.** **Union CNR is never skipped on composition
  reasoning** — "both sides are transpile-clean, so the union is" is not a verdict, and the case it
  cannot see is exactly the one that bit: a merge carrying a NEW behavioral test cut from an older
  base went red at the union (the `CollidingPackageNames` red). **A conflict dry-run does not need
  `git merge-tree --write-tree`** — that subcommand is unavailable on this box's git; the form that
  works is a temporary-index `read-tree -m --aggressive -i <base> <ours> <theirs>` plus
  `git merge-file -p` over each unmerged path, a 3-way CONTENT check that never touches the
  worktree, so it is legal under the mid-battery source freeze. And **rebase equivalence is checked
  by TREE, not by commit list**: `git diff <merge-of-old-tip> <rebased-tip>` coming back EMPTY is
  what proves a running battery's verdicts transfer to a train rebuilt on new SHAs.
- **A gate that has never been made to fail proves nothing.** Before trusting a census/self-verify that
  reports zero, regress one site deliberately, confirm it reports exactly that site, then fix and
  re-verify — and confirm the restore is byte-identical. The same principle as the positive controls
  the corpus loop uses: a green that cannot go red is not a measurement. Two refinements, both
  measured 2026-09-01: **a positive control must neuter a check no OTHER check subsumes** — under
  defense-in-depth, a broken control still reads green because a downstream check catches the
  regression it injected, so the control proves nothing about the check it targets (verify the
  control's red names the RIGHT assertion); and **a finding's PROSE is not its record** — a routed
  finding's description (a chip, a board line, a relayed diagnosis) is re-derived from the captured
  comparison/measurement record before anything is built on it, because the sweep-encoding chip's own
  description ("go pass vs C# fail") was wrong on the load-bearing detail and a rule built as
  described would have refused the very host it existed for.
  Two more, 2026-09-02. **A control only tests the AXIS YOU VARIED**: eight plausible, well-formed,
  entirely wrong findings passed BOTH of a census's controls because every repro varied box-ref and
  none varied RECEIVER KIND — so list the axes the predicate actually reads, and vary each one in a
  control. **And a control that does not use the CALLER's input shape is not a control for the
  caller**: a helper's self-test passed lines with content while every real call site passes BLANK
  lines, and a `Mandatory [string[]]` parameter rejects an empty ELEMENT (`[AllowEmptyCollection]`
  does not cover it), so the helper threw on every real invocation while the step's verdict and its
  artifact both looked normal — a guard that could never go green, caught only because a dispatch's
  annotations arrived from something else. Test with the exact type and shape the call sites pass.
  Three more, 2026-09-02. **Isolate by the RELATION the defect travels on, not by textual mention**:
  "classes that mention `flag`/`testing`" found four, "classes that drive `TestHost.Run`" five, and two
  single-cause fixes each passed a green full suite while an each-class-ALONE control still aborted — run
  every member alone. **A probe green on one binder path says nothing about the other**:
  `Delegate.CreateDelegate`'s static overload refuses a `DynamicMethod`, so a row came back
  infrastructure-error where the bound-path probe was green — exercise BOTH paths or NAME the one you
  skipped. And a committed disclosure quoted a 125/250/500 ms ladder its source does not contain (the
  rungs are 250/500/1000): re-derive a disclosure's mechanism from the line it cites, and post the RAW
  numbers beside any reading, since a measurement outlives the interpretation attached to it.
  Four ATTRIBUTION rules from one night of probe work, 2026-09-02. **A variant table names what each
  variant REMOVES and the attribution line is DERIVED from that column** — a swapped label on a correct
  measurement survives review by looking self-consistent. **An attribution is a ONE-AXIS pair**: a pair
  differing on two axes (container AND assembly) read 2.7x where the one-axis pair read 4.17x, and the
  design is cut against the one-axis number. **A gap between two arms of the SAME code with the SAME
  attribute is a CONFOUND TELL, never a boundary cost** — identical IL inlined from two assemblies
  yields identical machine code, so 4.0 vs 11.1 ns/word means an unoptimized callee or a declined
  inline: read `DebuggableAttribute.IsJITOptimizerDisabled` INSIDE the probe process, and on a release
  runtime read inlining from `DOTNET_JitDisasmSummary=1` (an inlined callee is absent from the list),
  since `DOTNET_JitPrintInlinedMethods` prints nothing there. **A hand-transcribed proxy is diffed
  against the emission before its number is quoted** — one token moved a 2.75x reading — and a
  retraction's positive claim owes the same measurement as the claim it retracts. Three control-FORM
  rules beside them: **a gate is ruled only after its BEFORE shows it can MOVE** (the TZ-pin gate row
  was green before the pin existed; calibrate with the variable genuinely ABSENT, since `TZ=` empty
  means UTC in Go and reads exactly like the pin); **a body's own failure is earned by a control in a
  SEPARATE worktree at the same SHA**, never by splitting the cut into commits; and **count a guard's
  DISCRIMINATING lines, not its lines** — a loopback receiver on 127.0.0.1 was GREEN against the body
  it guarded, a destination zeroed to 0.0.0.0 arriving anyway (bind 127.0.0.2 so arrival depends on
  the octets, and exercise the OLD path in the control).
  Four more control-design rules, 2026-09-02. **A ruling's load-bearing assumption is MEASURED before
  any code exists, with a negative control that fires** — libc `setegid` reaches an already-parked .NET
  thread (glibc's setxid broadcast) where the raw `setresgid` syscall does not, so the design records
  what a plain `DllImport` buys and what it does not, and the keystone's reason stays the structural
  one; a probe that changes PROCESS CREDENTIALS lands as a guard only with a privilege check and a LOUD
  unprivileged skip. **A probe proving a fix's SHAPE includes the case the NAIVE fix would OVER-CLAIM**
  — minting `Promoted=true` flipped the 35 target verdicts and made a VALUE store assert true where Go
  says false — and a generator that emits NOTHING for a type under the alternative flag is route #7's
  neighbour and gets a guard note. **A control that needs a 25-minute CNR to run is a control nobody
  runs** — give it a check-only switch — and its arms must reach EVERY step: two positive arms never
  reached a strip step, so a `[char]` `Replace` overload bug lived there until a negative arm (drift
  PLUS one unrelated hunk) threw. **And when every synthetic axis comes back clean, the differentiator
  is INSIDE the row**: the next measurement goes inside the failing test (a `t.Logf` before the
  `Fatalf`, one gated run on a host that has the package), not beside it — the same reason an
  IN-CONTEXT ratio understates an isolated one (2.7x against 7.5–11x, the surrounding loop diluting the
  cost), and the reason a reduction is trusted only once its assertion string appears VERBATIM in the
  real row's output.
  Two more, 2026-09-02, both guards that could never FIRE. **A `[string]`-typed PowerShell parameter
  coerces `$null` to `''`**, so a refusal written as "no readable tail" could not trigger on any input
  — an unreadable deadline tail would have read as clean. Untype the parameter and assert BOTH
  spellings, absent and empty. **And a BEFORE arm that produces NO output makes every arm read
  DIFFERS** — an instrument failure wearing a finding's clothes (the extracted copy was correctly
  throwing on a missing `Directory.Build.props`). A comparison that cannot report IDENTICAL on a
  known-identical arm proves nothing: control that the BEFORE arm prints at all, THEN positive-control
  the arm that must go red.
- **The warm-design trap:** the speculative branch is easiest to write while the design is still warm
  — and twice in one day (2026-09-01) a lane built guard/fix machinery, could not make it FAIL under
  its own control, and deleted it with the measurement recorded in a comment at the site. An
  unexercisable branch in a guard is a false-green seed; deleting it with its evidence is the
  deliverable, not a loss.
  **Its positive twin: a negative result is BANKED — in CODE at the gate, or in the RECORD**
  (both 2026-09-01/02). A measured-wrong next step recorded where the next reader will stand — the
  `MapIndex` follow-up marked *measured wrong: 0 fixed, 1 broken*, in the code at the site it would
  be attempted from — and a commissioned fix **cancelled with its measurement attached** each cost
  one line and save the next lane the whole attempt. The cancellation carries its own rule:
  **a predicate the converter already holds beats a metadata field nothing reads** (the proposed
  flag was dropped because an existing classifier already answers the same question, counts and all).
  **And a cut whose only demonstrated motivating failure is NON-REPRODUCIBLE is HELD** (2026-09-02,
  an L3 alias cut withdrawn): the mechanism read from the code and the emission actually measured
  disagreed — `mergeExisting=true` at the write sites READ as "preserves a windows alias into a linux
  run", while the merge is seeded per flavour and re-derives the whole imported-alias section — so a
  275-line filter nothing can exercise shipped on a static census with no dynamic measurement.
  **Measure the path once before building on a flag**, and withdraw the predicate with its census
  kept, which is the warm-design rule paid forward.
  **And a CORRECT cut with zero measured payoff is WITHDRAWN, not banked on fidelity** (2026-09-02, the
  `math/bits` hand-own over the BCL intrinsics): three nulls in one arc — the RSA-2048 signature moved
  0.1% (64.59 → 64.65 ms, one variable, the after-assembly proven to carry the intrinsics),
  `hash/maphash` −3.7%, the handshake by construction — against sixteen hand-owned functions that are a
  permanent maintenance obligation. The primitives ARE faster per call (Mul64 5.76 → 3.03 ns, OnesCount
  5.18 → 2.91, RotateLeft 4.18 → 2.62, and Add64 TIED at 4.88 → 4.81 because `UInt128` is not lowered
  to `adc`), yet even the fast form is 6.4x Go's 0.47 ns for what is ONE instruction on both sides — so
  the residual is the emission's call/return, tuple-return and value plumbing, a golib/emission
  question no leaf hand-own can reach. Post the PREDICTION before the numbers and note which kind it
  was: the op-count prediction (2–4x) failed publicly, the one made after a measured mechanism held.
  The withdrawal keeps the census, records the nulls in the file's own header, and leaves the chain on
  the board so nobody re-walks the eliminations — and the arc's LATER, narrower cut (word-size
  Mul/Add/Sub plus one inlining attribute, RSA-2048 66.4 → 20.2 ms measured) is the one that banked,
  because it was cut at the seam the nulls located: the emitted body, not the leaf.
  **Where a rule is PLACED decides which cases can reach it** (measured 2026-09-02, both directions):
  the same rule at a HELPER's arms — after the identity arm has already returned — cannot break an
  identity case, while as a CALLER-side gate it runs ahead of identity and did (0 fixed, 1 broken).
  Put a rule where the cases that must not reach it have already returned. Its retirement half: when a
  rule is spelled once at a helper, a caller-side copy is retired only if it duplicated the RULE — a
  copy that enforces ORDER (`SetMapIndex` checking the key BEFORE its nil-map panic, Go's own sequence)
  is load-bearing and STAYS, with the reason at the site, because the helper answers the question but
  cannot express when the caller must ask it; and each retirement carries the row its copy used to
  catch as a positive control. Mechanically: a REBASE leaves `go2cs.exe` stale (route #1) — rebuild
  before re-transpiling a golden.
  **A cut owes its OWN behavioral guard, and an acceptance table for a row with TWO independent
  failures cannot be built from one of them** (measured 2026-09-02). Three outcomes were enumerated
  for a row that carried a second, unrelated failure and none of them admitted the one that happened —
  "the named failure resolves and the other remains"; enumerate outcomes per FAILURE, not per row.
  Borrowing a lane's roster row as a cut's acceptance test also couples the cut's evidence to that
  row's other defects: it cost a 55-minute run on a restarting host and proved nothing about the cut.
  Pin both acceptance directions in a guard the cut owns — including the direction no consumer
  exercises.
- **⚠ A merge that touches `package_info.cs` must carry the matching `stdlib-metadata.txt` change —
  check it in the PREFLIGHT.** `stdlib-metadata.txt` is generated FROM the corpus (`go generate .` in
  `src/go2cs`, gated by `TestStdLibMetadataInSync` under the converter's own `go test`), and a corpus
  bank that moves `GoImplement` records without it leaves that guard red for whoever runs the
  converter suite next. Three banked regens missed it in two days (2026-08-24/25) — the step was
  documented and still skipped, because no MERGE checked for it: if
  `git diff --name-only <base>..<branch>` lists a `package_info.cs` but no `stdlib-metadata.txt`,
  stop and have the branch run the generate before it merges.
- **⚠ Two branches writing the SAME wrong number auto-merge CLEANLY.** The roster's header is the
  measured case (2026-08-29, the banking window): master and an incoming bank both moved the row
  count 189 → 190 — identical text on both sides, so git folded them without a conflict while the
  union's truth was 191. The silent-duplication rule's arithmetic twin: at any multi-branch window,
  header/summary numbers are RECOMPOSED from the merged table, never accepted from either side, and
  the format guard (guard-as-calculator) runs after EVERY resolution — it caught this one and a
  hand-composed Linux-denominator slip the same evening.
- **A liveness/health probe must be able to OBSERVE the thing it asks about** (2026-08-29, the iter
  lane): a process filter on the worktree path can never match `dotnet.exe` running from Program
  Files, so a healthy 18-minute build read as reaped and was reported as owed. Silence is not
  evidence of death any more than exit 0 is evidence of success — the rule cuts both directions:
  read the output, and first verify the check CAN see its target (positive-control the probe the
  way gates are positive-controlled).
  ⚠ **"Armed" is a claim about a task verifiably STILL RUNNING** (2026-09-02): a task id that has
  EXITED is evidence of a PAST arming, and a lane went silent for hours with BOTH legs down — its
  exit-on-change watcher had fired on the lane's own post and was never re-armed, while the backstop
  that exists to catch exactly that first failure was itself gone. A protocol step that must be
  remembered at the end of the busiest turn, and whose failure is silent, fails on a schedule: DELETE
  the step (a persistent monitor needs no re-arm on a local lane; on the cloud-container class it is
  hard-capped at ~30 min, so there the relaunch leg is load-bearing) rather than reminding harder, and
  back it with a leg that verifies LIVENESS, not existence, and checks its own existence on every
  firing. Its reading
  half: a filter built from expectations can be simply where you stopped reading — read every numbered
  item of a post addressed to you, and read anchor..tip before starting the next one.
- **Positive-control the DETECTOR, not just the gate** (2026-08-30, the pinning census guard): a
  BOM-less `.ps1` under Windows PowerShell 5.1 mis-reads non-ASCII literals through the system
  codepage, so a guard's `ᴋ`-matching regex was silently broken and its "0 findings" red was
  accidentally right for the wrong reason. A new false-signal species: a red whose detector is
  dead. Any regex-bearing guard on PS 5.1 gets a BOM if it carries non-ASCII, and gets its
  detection deliberately regressed once before its verdicts are believed.
  ⚠ Same species one layer up (2026-09-02, met independently by two lanes): a checker printed
  **PARSES CLEAN** while its own `[ref]` binding had thrown on an undeclared variable — the `else`
  branch prints clean regardless. Declare a checker's ref targets, and run it once against a
  deliberately BROKEN copy before believing any "clean".
- **GC/liveness probes: ONE ARM PER PROCESS** (2026-08-30, the StringData lane): running probe
  arms back-to-back contaminates them — an in-frame arm's object collects as soon as a LATER arm
  clobbers the frame, so only the last arm's reading is honest; three arms flipped verdicts on
  run order before isolation. Same family as the tier-0 finding: what the frame holds decides
  what collects, so each measurement gets a fresh process.
  ⚠ **A FITTING story is not a root — and this family's most convincing one was measured FALSE**
  (2026-09-02). A non-optimizing JIT roots every local for its method's life, so a test looping
  `runtime.GC()` for finalizers cannot see them become due at Debug; the mechanism is real,
  `mfinal.cs`'s own comment predicted it, and it fit `TestSplicePipePool`'s symptom perfectly — total,
  permanent, immune to repeated GC. One one-axis run killed it: `internal/poll` at Release+TC0 fails
  IDENTICALLY to Debug (zero rows moved, identical fd set, 2.6 s across a 54-minute window). Four
  candidates are measured out now — SetFinalizer keying, `sync.Pool` aging, the `runtime.GC` sequence,
  the JIT tier — and after four the next step is an INSTRUMENT (a heap root-path read), never a fifth
  hypothesis. Prediction-on-record is what made that run decisive, and what makes a falsification
  cheap.
- **The `-tests` graph invariant (ruled 2026-08-30, from the W1 arc):** a `-tests` conversion's
  production emission may differ from `-stdlib`'s only in ways that do NOT change the project
  GRAPH. The documented closure families all change file text and no reference; the
  `canUseLongPaths` csproj flip was the first edge-mover and it was fatal (6 cycles), which is
  the boundary's proof. Mechanical form: `check-solution-integrity.ps1`'s per-GOOS cycle
  assertion (G2), whose positive control injects the historical edge and requires exactly the
  six named cycles.

## Git anchors

| Commit | Date | Meaning |
|---|---|---|
| `9792eeea2` | 2020-07-09 | Original hand-converted stub created (`src/gocore`). |
| `ba6fef6c9` | 2025-03-08 | Renamed `src/gocore` → `src/core`. |
| `3426298eb` | 2025-05-05 01:51 | Last clean stub baseline — **restored into `src/core`** on 2026-06-25. |
| `6ca1c45b7` | 2025-05-05 01:59 | First full stdlib conversion — overwrote the baseline. |
| `cc14584c7` | 2025-05-11 | Full-conversion work; tagged `full-conversion-2025-05`. |
| `3c8b3a848` | 2026-06-25 | Separation + stub-baseline restore + converter fixes → green baseline. |
| `05a53e8c0` | 2026-06-26 | First full-conversion package promoted — `sync/atomic` into `core`. |
| `914d4bd72` | 2026-06-27 | `math` compiles clean (tag `math-green-2026-06-27`). |
| `51ba5d9cf` | 2026-07-10 | **First clean full-standard-library compile** — all 302 converted packages (then at `src/go-src-converted`; tag `stdlib-green-2026-07-10`); Phase-3 milestone. |
| `337a928df` | 2026-07-17 | **First real Go test suite validated in C#** — `unicode/utf8` 14/14 vs `go test -json` through the Phase-4 `-tests` pipeline (tag `utf8-tests-green-2026-07-17`); §12.8 opened. |
| `f999c8f78` | 2026-07-18 | **Second validated package** — `sort` 63/63 vs `go test` (tag `sort-tests-green-2026-07-18`); first with real algorithmic depth (interface-driven sort, `sort.Slice` reflection, NaN ordering). |
| `40f39d2be` | 2026-07-18 | **Packages #3 and #4 validate** — `bytes` 81, `strings` 68 (tag `bytes-strings-tests-green-2026-07-18`), via the hand-owned signature-pinned **disclosed-divergence manifest** (`go2cs_test_disclosures.json`) for the alloc-count asserts the managed CLR provably cannot satisfy. |
| `2e8066da6` | 2026-08-01 | **The two trees become one** — the stub baseline retires and the converted stdlib moves to `src/core`; every rewrite/remap path is deleted, `testing` joins `unsafe` as hand-owned, the generated `go2cs-stdlib.slnx` becomes adoptable verbatim. |
| `f6e9c0cf0` | 2026-08-04 | **The whole-corpus rebank** (r40) — the one deliberate regeneration that levels the accumulated intended drift of every converter arc that landed without its corpus regen: 1,316 files across sixteen named families, zero unclassified. `bf1458b5d` banks the matching test-source + proof-page refresh behind a 73/73 sweep. The `GoUntyped` alias becomes `GoBigConst` in `4d71935ff` on the way in. |
| `10c78227a` | 2026-08-22 | **Over 75% of the testable stdlib validates** — 162/215 packages, 18,569 matching verdicts, 85 disclosed (tag `stdlib-tests-75pct-2026-08-22`); Go 1.23.1's TERMINAL validation marker — `release/go1.23` cut, the campaign continues on 1.23.12. |
| `4f0fd0b5c` | 2026-08-23 | **The anchor NuGet release** — the 1.23.1 corpus publishes as **`1.23.1.7`** (tag `nuget-1.23.1.7`), the over-75% roster's pre-hop .NET 9 anchor, Windows + Linux in one combined story; `docs/validation/1.23.1.7/` freezes the 162 proof pages the packed badges link. |
| `925e48067` | 2026-08-24 | **Master moves to .NET 10** -- Stage 2 of the framework hop merges: 955 project files to net10.0 with zero corpus-emission drift, three OS flavors green, carrying the C#14 params-flip converter fix the hop itself exposed (under C#13 the corpus's variadic-slice binding was correct *by accident*). |
| `a2e079259` | 2026-08-25 | **The roster re-banks at Go 1.23.12** -- the corpus hop completes: 162/162 rows re-validated from the new release's own test sources, **18,598** matching verdicts (+29 = exactly the four re-derived rows), three machines' shards reconciling to the digit. With 925e48067 two days earlier, both runtime pins (net10.0, go1.23.12) moved in one campaign, each through a runbook that led. |
