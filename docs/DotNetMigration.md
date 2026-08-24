# Migrating go2cs to a new .NET version

> The standing runbook for moving the repository — the runtime library, the converted standard
> library, the generated projects and the Phase-4 test host — from one .NET release to the next.
> It is **version-agnostic by design**: it names instruments, gates and traps, never a particular
> release. The .NET 10 move is its first instance, planned in
> [`PLAN-hop-campaign.md`](PLAN-hop-campaign.md); the audience is every move after it.
>
> Companion: [`GoCorpusMigration.md`](GoCorpusMigration.md), the same procedure for a new **Go**
> release. The two are deliberately separate documents because they are deliberately separate hops.

**No frozen figures.** Corpus-sized quantities — project counts, roster rows, verdict totals,
diagnostic counts, wall times — are **named by instrument and re-measured at the hop**. Where a
budget matters, this document names the row in CLAUDE.md's measured budget table rather than copying
a number that goes stale between hops. A migration that asserts last hop's number has already
started lying.

---

## 1. The invariant: one variable at a time

> **Schedule fact (recon, 2026-08-23): .NET 9 reaches end of support 2026-11-10** — the STS
> window was extended to 24 months, landing .NET 8 and 9 on the same EOL day. The .NET 10 hop is
> therefore mandatory-by-November, not discretionary; .NET 10 has been GA since 2025-11-11 and is
> LTS through 2028-11-14. Full survey: [`phase4/RECON-dotnet10.md`](phase4/RECON-dotnet10.md).

A .NET migration moves the runtime and the target framework moniker. **It moves nothing else.** In
particular it does not move the Go toolchain, the corpus's Go release, or the converter's language
level — those belong to a Go corpus migration, which is a separate hop with separate gates.

The invariant is not stylistic. Almost every trap in §3 produces a *plausible number* rather than a
failure, and a plausible number is only attributable when exactly one thing changed to produce it.
The stage ladder in §4 exists to keep that true: each stage changes one thing and is gated before the
next begins.

### 1.1 What a .NET migration does NOT trip

Three guards dominate a Go corpus migration and **none of them fires here**. Stating this removes
work rather than discovering later that it was never owed:

| Guard | Why it is inert |
|:--|:--|
| `checkCorpusToolchainPin` (`src/go2cs/toolchainResolution.go`) | Compares the **Go** release in `version.props` against GOROOT's `VERSION`, exactly. No Go release moves |
| `checkNuGetStdLibCompatibility` | Compares the Go release's `version.Lang`. Same reason |
| Release-tag emission (`releaseTagsForVersion`, `src/go2cs/directiveOperations.go`) | Derives the `go1.1 … go1.N` build-tag set from the **Go** version. No `//go:build` constraint flips |
| False-green route #4 (the stale-`go2cs.exe`-after-a-toolchain-hop trap) | Its mechanism is that no harness's rebuild predicate observes the **Go** toolchain. A .NET migration installs no Go toolchain |

**One route it DOES touch**: §5.2's embedded-asset stale binary — the TFM stage edits files compiled
into `go2cs.exe`. The predicates cover it since 2026-08-22; read §5.2 for what that means for the
stage's gate accounting.

---

## 2. Stage 0 — provisioning the fleet

1. **Install the target SDK side-by-side**, to a user-local directory, via the official
   `dotnet-install` script with `-NoPath`. **Do not disturb the machine's existing default** until
   the deployment-shape review (§9) says so: every pre-cutover measurement is an A/B that needs both
   runtimes present, and removing the old one destroys the control.
2. **Record both inventories per machine** — `dotnet --list-sdks` and `dotnet --list-runtimes` — in
   the machine's provisioning note, which lives in
   [`phase4/STAGE0-provisioning.md`](phase4/STAGE0-provisioning.md) (one section per machine;
   append, never rewrite — the note is the stage's record). *This location was the first
   execution's shakedown finding: the step named a note without naming its home.* Patch levels differ across a fleet, and a cross-machine
   comparison that does not name both SDK and runtime patch is not a measurement.
3. **Verify the leg; never assume it.** A project targeting the *old* TFM continues to run on the
   *old* runtime even under the new SDK. Selecting the new runtime for a measurement leg takes an
   explicit environment (`DOTNET_ROOT` plus a roll-forward policy), and the selection must be
   **proved by a `FrameworkDescription` probe** whose output is recorded — probe before, probe after,
   probe again on restore. **A leg without a probe is not a leg**; it is the old runtime wearing the
   new one's name.
4. **Pin the SDK with `global.json`** once the TFM moves (§5), not before. During the SDK-only and
   baseline stages both SDKs must remain selectable by environment, which is precisely what a pin
   fights. Choose the roll-forward policy deliberately — a pin to a *major* with tolerance inside it
   keeps a contributor on a different patch level working; a pin to an exact patch does not.

**Fleet note.** Provisioning is per machine and can proceed in parallel with anything, because it
changes nothing in the repository. It is the one stage of a migration with no dependencies.

---

## 3. The toolchain trap catalog

Four traps, each measured on a real scouting run, each stated here in the form that survives a
version change. **Every one produces a number rather than an error** — which is what makes the
catalog worth carrying.

### Trap 1 — the AOT compiler binds to the TFM, not the SDK

The Native-AOT compiler (ILC) arrives as a **runtime pack selected by the project's target
framework**, not by the SDK performing the publish. A new SDK publishing an old TFM therefore
resolves the **old** ILC, and every AOT number it produces is the previous generation's codegen.

**Consequence for planning:** *no AOT measurement of the new runtime is reachable before the TFM
moves.* A migration plan that schedules AOT measurement early has scheduled an impossibility. AOT
becomes measurable at §5 and not before.

**How it presents:** an AOT leg on the new SDK reproduces the old SDK's AOT timings to within noise,
across benchmarks, in a way that looks like "the new runtime changed nothing" — a plausible null
result rather than a blocked measurement.

**Detection:** read the resolved ILC package version out of the restore. Do not infer it from the
SDK version; that inference is the trap.

### Trap 2 — the runner's up-to-date check reuses a publish across an environment change

Switching runtimes by environment variable changes nothing on disk, so a publish-artifact freshness
check sees a current artifact and skips the publish. The next leg then measures the **previous** leg's
binary.

**The tell is the clock.** A real AOT publish of this corpus compiles the full converted-stdlib
closure and costs minutes to tens of minutes on every machine class the budget table records. **A
publish that completes in seconds has not happened.** Treat an implausibly fast publish as a
stale-artifact alarm, never as a win.

**Remedy:** purge the benchmark's intermediate and output directories before any cross-SDK A/B. This
is the same class as the repository's documented `GoTargetOS` hazard — an item set or an environment
changes while timestamps do not — and it has the same remedy.

### Trap 3 — a new compiler adds new diagnostics to unchanged code

A new Roslyn emits warnings the old one did not, on source nobody touched. These are usually benign
and always noise at exactly the wrong moment.

**Procedure:** the SDK-only stage (§4) produces a **classified warning delta**, not a count. Each new
diagnostic is named and dispositioned once — benign, suppressed with a reason, or fixed — so that a
later stage's warning output can be read as signal.

### Trap 4 — an old-TFM app runs on the old runtime under the new SDK

The default is compatibility: the app targets the old framework, the old runtime is installed, and
that is what loads. This is trap 1's sibling on the JIT path and it silently invalidates any
"new runtime" measurement taken without explicit runtime selection.

**Remedy is §2 step 3**, and it is worth repeating because the failure is silent: probe
`FrameworkDescription` inside the measured process, record the output, and restore the environment
afterward with a third probe.

---

## 4. Stage 1 — the SDK alone

**Changes:** the SDK on `PATH`. **Not** the TFM.

> **⚠ Stage 1 changes more than it looks like it does (recon finding, 2026-08-23,
> [`phase4/RECON-dotnet10.md`](phase4/RECON-dotnet10.md)):** every converted csproj pins
> `LangVersion=latest`, so the SDK hop alone recompiles the whole corpus as **C# 14** while still
> targeting net9.0 — and C# 14's first-class span conversions (`T[]` → `ReadOnlySpan<T>` as a
> built-in conversion) land squarely on golib's `slice<T>`/`@string`/`array<T>`
> implicit-conversion and overload surface. The one-variable invariant holds — the SDK IS the one
> variable — but the stage's record must state that the language version rode with it, and the
> behavioral suite is the detection net for any overload-resolution shift. Also from the same
> recon: the .NET 10 CLI writes informational output to **stderr**, a fresh surface for the
> repository's documented PS 5.1 `$ErrorActionPreference='Stop'`/NativeCommandError trap — audit
> harness call sites before trusting a Stage-1 red.

**Gate — the full compile-and-run ladder, by instrument:**

| Instrument | Bar |
|:--|:--|
| `dotnet build src/go2cs-stdlib.slnx -c Debug -p:UseSharedCompilation=false` at the default `$(GoTargetOS)` | zero errors; skipped-dependents enumerated and zero |
| the same at **every other supported `$(GoTargetOS)`** whose flavor currently builds | zero errors. ⚠ purge `bin`/`obj`/`Generated` between target switches — the `<Compile>` item set changes while timestamps do not, so an incremental build after a switch validates the *other* target's assemblies |
| `dotnet build src/go2cs.slnx` | zero errors — the only gate that compiles the non-generated solution members (utilities, examples) |
| `src/tests/Behavioral/run-behavioral.ps1` (all four phases) | green, zero `NOT MEASURED` |
| `GolibTests` | green |

**Warning delta classified** (trap 3), not counted.

**Not owed, and say so rather than skipping silently.** `check-no-regression.ps1` measures **converter
emission**, and the converter is a Go binary whose output cannot move because a .NET SDK changed. The
same accounting covers the converter's own `go test ./...`. Both become owed the moment a converter
source or **embedded asset** moves — see §5.2. State the accounting in the stage's record; the
repository's habit is *"not run; accounting stated"*, and it is what lets a reader tell a skipped gate
from a forgotten one.

---

## 5. Stage 2 — the target framework

**Changes:** one MSBuild property. **Nothing else.**

### 5.1 The bump is one line, and the inert copies are the interesting part

`src/Directory.Build.props` owns the repository-wide TFM and says so in its own comment: *"The
repository-wide TARGET FRAMEWORK, and the one line a .NET hop edits."* The hoist is deliberate —
before it, a framework hop meant rewriting both converter templates, every hand-written project, and
then regenerating the whole corpus to level the emitted copies.

Three mechanics ride on it, all guarded or documented in place:

1. **Emitted projects keep a CONDITIONED fallback**
   (`<TargetFramework Condition="'$(TargetFramework)'==''">…</TargetFramework>`), because an emitted
   project does not always have this file above it: `deploy-core.ps1` stages the corpus under a
   GOPATH root with its own props, a `-recurse` conversion writes under an arbitrary output root, and
   a single-package conversion can land anywhere. Where the root props IS in scope it wins and the
   project's own line is inert; where it is not, the project still names a framework and still builds.
2. **A guard enforces the shape.** `TestTemplatesLeaveTheTargetFrameworkOverridable`
   (`src/go2cs/csprojMetadata_test.go`) fails a template that sets `<TargetFramework>`
   unconditionally — *"an unconditional value cannot be hoisted"* — and fails one that sets none at
   all. It runs under the plain converter `go test ./...`.
3. **Nested props files must import the root explicitly.** MSBuild stops at the first
   `Directory.Build.props` walking up; the existing nested ones each carry an explicit
   `GetPathOfFileAbove` import. **Any new nested props file owes the same import or it silently
   shadows the hop** for everything beneath it.

**After the one-line edit, the tracked mentions of the old TFM are inert but wrong-looking** — there
are thousands of them, and they level on the next regeneration. Two consequences worth scheduling
around:

- **A .NET migration does not owe a corpus regen for the TFM.** Forcing one spends a gate cycle to
  change a string no build reads. **State the expected staleness in the commit message** so a later
  reader does not diagnose it as drift, and let a Go corpus migration's own reconvert level it.
- **The families that are NOT inert must be enumerated by hand**, because they are load-bearing
  rather than cosmetic:
  - **`push-nuget.ps1`'s package layout.** The TFM appears in `lib/<tfm>/` (the compile-time asset
    and RID-agnostic runtime fallback) and `runtimes/<rid>/lib/<tfm>/` (the RID-selected runtime
    asset). These are NuGet asset-selection facts.
  - **The publish profiles** under `src/go2cs/profiles/` — one per shipped RID, each carrying a
    `<TargetFramework>` and a TFM-bearing `<PublishDir>`.
  - **The two converter csproj templates'** conditioned fallbacks.

### 5.2 The embedded-asset false-green route — closed

**The TFM stage edits files that are compiled INTO `go2cs.exe`, and the harnesses now know it.**

- `src/go2cs/embeddedTemplates.go` `//go:embed`s the csproj templates, the `package_info.cs`
  skeleton, the icons and the whole `profiles/` directory into the converter binary — *"Embedding
  them makes `go2cs.exe` a single self-contained executable."* `stdlibMetadata.go` embeds
  `stdlib-metadata.txt` the same way.
- Editing one changes what the converter emits **without touching any `.go` file**. Until 2026-08-22
  every rebuild predicate asked whether a **top-level** `*.go` was newer than the binary, so the whole
  embedded set — plus the converter's `internal/` packages and `go.mod`/`go.sum` — invalidated the
  binary nowhere: 204 files seen against 224 real inputs. A template edit reported "up to date", the
  old template stayed embedded, and every runner gate validated the previous emission and printed
  PASS. A `runtime.Version()` toolchain stamp (§ route #4) does not cover this — a stamp says nothing
  about a template's modification time.

**What covers it now.** `src/tests/ConverterBuildInputs.cs` is the single definition of the
converter's build-input set, linked into `BehavioralRunner`, `BehavioralTestBase` and
`PerformanceRunner`; the embedded half is **derived from the `//go:embed` directives themselves**, so
a directive this stage adds is covered without anyone widening a list. Two guards ride the converter's
plain `go test ./...` (`src/go2cs/embeddedAssets_test.go`): the directive forms stay inside the subset
the resolver understands, and the three predicates still delegate to the shared helper. Editing a
template or a profile now rebuilds `go2cs.exe` on the next runner invocation, with nothing to remember.

**`check-no-regression.ps1` was never exposed**, and the reason is worth carrying into the stage's
accounting: it has no rebuild predicate at all — it runs `go build` unconditionally before
transpiling, and `go build`'s cache is content-addressed over embedded assets. Verified by
measurement: editing `csproj-template.xml` changes the linked binary's SHA-256, and reverting
reproduces the baseline byte-for-byte. So CNR's verdict on a TFM-stage template edit is trustworthy
on its own, exactly as it is for the stale-output and stale-toolchain routes.

**One residual, and it is narrow.** cmd/go's test cache drops files resolving outside the module root,
and the three predicate sources live under `src/tests`. A change that touches **only** harness C# can
therefore be served a cached PASS by the second guard; run `go test -count=1 ./...` from `src/go2cs`
after such a change. The first guard reads only in-module files and has no such gap.

### 5.3 The TFM stage's gate

The §4 ladder again, in full, **plus** the `-tests` pipeline proven on a representative row set — the
generated test host carries its own TFM fallback and its own publish shape, and it is the surface
§8 is about to change.

---

## 6. The performance re-measurement protocol

A .NET migration's whole CPU story is a comparison, so the protocol is about **attribution**, not
about running a benchmark.

**The paired same-session A/B is the only admissible evidence.** The repository's own doctrine, paid
for by a laptop measurement that read as a 30 % regression and turned out to be machine drift:
*"on a laptop, a perf comparison against an earlier-session baseline is not evidence — only a paired
same-session A/B is."* Both legs run on the same silicon, the same day, in the same session, on a
quiet machine.

**Protocol:**

1. **Capture the OLD-runtime baseline first**, at the stage *before* the variable moves. It is a
   separate, gated stage precisely so it exists.
2. **Run both legs over identical IL** where the TFM permits it — selecting the runtime by
   environment rather than by rebuild makes the delta pure runtime/JIT codegen, with nothing else in
   it. Verify each leg with the `FrameworkDescription` probe (trap 4).
3. **Carry a same-day control.** The Go binaries' columns must reproduce across legs within noise. A
   control that drifts invalidates the pass, and it is the cheapest possible check.
4. **Verify before timing, always** — `PerformanceRunner`'s standing doctrine: identical
   timing-filtered stdout across every variant before anything earns a number.
5. **Name the host, and name it as a host.** A scouting measurement taken on a machine that is not
   the perf-canon host produces ratios *internal to that box*. They are real and they are not the
   canonical table. Say which, in the record.
6. **Regressions are re-measured, not blocked on.** A narrow named regression on one box is a
   candidate, not a verdict; the migration's own measurement is what settles it. Report each whether
   or not it reproduces — a regression that vanished on other silicon is a finding about the scouting
   box, and worth as much as one that held.
7. **Bank through the instrument**, not by hand: the results table lives between the `PERF-RESULTS`
   markers in the performance suite's README and is rewritten by the runner's own flag, with prior
   toolchain tables accumulating in its *History* section — which is exactly the cross-version
   comparison that section exists for.

**Cost discipline.** A full AOT pass is hours and **must run solo**; concurrent load has pushed a
healthy publish past its watchdog. The publish also costs substantial disk, including a debug-symbol
file far larger than the executable. Run the disk preflight by hand first — the validated sweep
refuses below a documented free-space floor precisely because full-drive failures surface as *corpus
failures* that name everything except the disk.

### 6.1 Amendment — the control row is NAMED, allocation claims are COUNTED, and the scouting lessons are protocol (hop-era, per the user's perf directive)

Two additions promoted from ratified doctrine into this protocol's numbered steps, and the
pre-hop scouting folded in so its cost is paid once:

**Step 3 is strengthened: the control row is named IN THE RECORD, and read FIRST.** "Within
noise" is unfalsifiable until the row that *cannot* have changed is reported beside the rows that
could. The span-tranche measurement that ratified this: the untouched control row moved **+5.0 %**
and the unchanged Go binaries up to **+17 %** between two back-to-back same-session legs on the
perf-canon laptop — a noise floor several times a typical effect. So every banked comparison names
its control row (a benchmark the change cannot reach), reports it first, and a pass whose control
moved more than its subject rows is **void**, not "within noise". If the change reaches every
benchmark's path (a runtime hop does), the Go columns are the control — that is what step 3's
reproduce-across-legs already demands; this amendment makes the *reporting* of it mandatory.

**Step 4 gains a twin: where the claim is about ALLOCATION, gate by COUNT, never by time.**
`AllocationCounter` counts are deterministic and host-independent; a timing gate on a laptop
mostly measures the laptop (the ratified counting-gate doctrine, minted when a tranche of real
allocation deletions produced timing deltas smaller than the control row's drift). A migration
stage claiming "no allocation regressed" runs the count-gated GolibTests rows, not a stopwatch.

**The scouting lessons (2026-08-23, SDK 10.0.400 on the perf-canon laptop), folded in as
protocol facts:**

1. **The ILC binds to the TFM, not the SDK** — measured, not just cataloged (trap 1): SDK 10.0.400
   publishing `net9.0` resolves `ILCompiler 9.0.19`, and its "10-AOT" Fib is **identical** to the
   9-AOT Fib (177.1 vs 178.2 ms). Corollary: *there is no AOT measurement worth taking between N1
   and N3* — the AOT column cannot move until the TFM does, so scheduling one is spending hours to
   measure the null hypothesis of a variable that has not moved.
2. **A 51-second "publish" is the trap-2 tell on this corpus.** A real per-benchmark publish is
   964–1,138 s on the perf-canon laptop and ~25 min on the i7-5820K. Any AOT leg whose publish came
   in orders of magnitude under that re-measured a stale binary; purge and disbelieve.
3. **Roslyn 10's CS7022** on the runner's top-level-statements shape is benign and expected at N1.
4. **`net9.0` under the 10 SDK still executes on the 9 runtime** unless explicitly selected —
   the `FrameworkDescription` probe (trap 4) is what makes any leg's identity a fact rather than
   an assumption, and it runs on *both* legs, every time.

### 6.2 The N5 close plan — the AOT leg's falsifiable prediction, stated before the run

§7 demands the AOT stage state its expectation before running; this section states it now so N5
inherits a prediction instead of writing one under its own results.

**Background:** the bflat exploration's one CPU anomaly was Fib under bflat's .NET-10-preview
codegen — unattributable to bflat itself (same ILC/RyuJIT family), and left standing as "an
argument for measuring the hop itself". The scouting then showed the 10-SDK-on-net9.0 leg is
byte-for-byte the 9-ILC (lesson 1), so the anomaly's candidate cause narrows to exactly one
untested thing: **the real .NET 10 ILC/framework pair behind a `net10.0` TFM.**

**Prediction N5, falsifiable in both directions:** *running the suite's AOT column at N5 (net10.0,
ILC 10.x), the Fib row moves materially in the direction the bflat preview showed — closing the
anomaly's attribution as "the 10 codegen" — or it lands within the named control row's envelope of
the N2 9-AOT baseline, and the anomaly is attributed to the preview/bflat packaging and CLOSED as
not-a-hop-question.* Either outcome resolves the attribution; the prediction exists so the outcome
is information rather than narrative. The comparison base is N2's 9-AOT numbers, minted on the
same host, banked in the README's History section; the control row is the Go column, per §6.1.

---

## 7. AOT / ILC verification

**Only reachable after the TFM moves** (trap 1). The procedure is **verify, then bank**, in that
order:

1. **Purge** the benchmark's intermediates and outputs (trap 2 — the seconds-long publish is the
   tell).
2. **Confirm the resolved ILC version from the restore**, not from the SDK version.
3. **Verify** — identical timing-filtered stdout across Go, JIT and AOT.
4. **Measure**, solo, on the perf-canon host, at the suite's own run counts.
5. **Bank** through the runner's update flag.

**State the expectation as a falsifiable prediction before running.** A migration whose AOT stage has
no prediction cannot be surprised, and the surprise is the information. If an external toolchain's
advantage was previously attributed to *"the newer ILC and framework pair it ships"*, then the new
TFM's own AOT numbers either reproduce that advantage — closing the attribution — or they do not, and
the attribution reopens with a sharper question. Both outcomes are worth the run; only an
unpredicted one is worth nothing.

---

## 8. Trim-safety audit

The deployment floor — binary size, startup time, working set — is dominated by **trim rooting
policy**, not by the compiler. A partial trim mode roots every assembly of the converted closure
whole; a reachability-based mode does not. The measured gap between them is large in every dimension
and the recovery is available from the stock SDK.

**The lever is fixed for a correctness reason, and that reason is binding.** golib's formatting and
the sort interfaces bind members through reflection, and full trim can remove exactly those. **A
sample of small programs passing is not evidence**; the population is the behavioral suite and the
validated roster.

**Procedure:**

1. **Run the audit build with trim warnings VISIBLE.** The performance tree sets
   `SuppressTrimAnalysisWarnings` in its own defaults deliberately — perf publishes should not fail
   on trim noise — and the standing disposition is that **the suppression must never be the reason
   the diagnostics go unread**. Override it locally on the audit build (`-p:SuppressTrimAnalysisWarnings=false`);
   never edit the perf tree's default to do it.
2. **Re-measure the diagnostics.** They concentrate in golib's adapter and reflection layer and in
   the converted `reflect` implementation. The count and its per-file distribution are **re-measured
   at each migration** — a new ILC adds and drops diagnostics — and a subset are dynamic-code
   diagnostics that apply to the *existing* AOT publish and are not introduced by trimming. Separate
   the two before pricing the work.
3. **Annotate** the named surface (`DynamicallyAccessedMembers`, `DynamicDependency`).
4. **Accept against the population**: the behavioral suite's Output phase and a validated-roster
   sample, both published under the candidate trim mode, with zero output divergence. Any divergence
   **names the reflective site the trim removed**.
5. **A trim-safe golib is a golib API-surface change**, so the standing rule applies: build
   `src/go2cs.slnx` once before banking. No other gate covers the non-generated solution members.

---

## 9. Host-shape changes, and their roster / manifest consequences

A .NET migration is the natural moment to re-price *publish shapes*, because publish shapes are what
it changes. When a shape change makes a previously-impossible test possible, the consequence reaches
the validation roster — and the disclosure machinery is **built to force that revisit** rather than
allow drift.

### 9.1 The mechanism

A disclosure is a Go assertion the converted suite provably cannot satisfy, pinned by **exact failure
signature** in a committed, hand-owned manifest. Disclosed tests **keep running in every sweep**. So
the day a deployment-shape change makes one of them satisfiable, it begins passing, **the sweep's
disclosure arithmetic breaks loudly, and the entry must be removed**.

That is the intended behavior, not a fault. Design the migration so it *expects* the break.

### 9.2 The worked pattern — a host-capability disclosure class

The pattern generalizes; the host-limit class is its worked example.

1. **Enumerate the constituency from the committed manifests, not from prose.** Prose drifts; the
   manifests are the authority. Count entries per package and note that a package's *disclosed
   verdict count* may exceed its entry count, because parent rows ride disclosed-parent aggregation.
2. **Separate the LEVERS.** Entries in one class do not all retire on one change. A relocatable
   single-file publish and a fast-starting publish are **different levers**, and a class can hold
   entries needing each. A migration that ships one lever retires the entries that need it and leaves
   the others standing — an honest outcome, but only if it was **predicted** rather than discovered.
3. **Identify the entries that never retire.** Some rest on a property the design deliberately
   forecloses; their manifest text must say so explicitly, and a *pass* on such an entry is cause to
   investigate, not to celebrate.
4. **Find the sibling GATES, not just the disclosures.** The same host property may also appear in
   the converter's `unsupportedRuntimeCapabilities` map — a *gate* that removes tests from the run
   set rather than disclosing them. The map is guarded by its own converter test, so removing an
   entry is a converter change owing the converter's `go test ./...`. A migration that retires the
   disclosures and forgets the gate has retired half the class.
5. **Price the shape before adopting it.** A self-contained single-file test host is *a publish
   rather than a build*, per package, at a substantially larger artifact size. Across a full roster
   that is a different campaign from a build-per-row. **Measure it**; do not assume it absorbs.

### 9.3 The consequences, in order

1. **The affected rows re-derive.** Previously-disclosed verdicts become matched verdicts; the row's
   matched and disclosed columns both move.
2. **The manifest loses its retired entries**, and at zero remaining entries **the file itself is
   removed** — the established precedent, because the roster's arithmetic moves when it goes.
3. **The roster header re-derives** in both components. It is recomputed from its own table, never
   typed.
4. **A row whose GO side is host-conditional cannot be banked from one measurement.** Where a
   manifest's own `hostConditional` note says the Go baseline varies per host and per run, the
   evidence bar is repeated measurement on more than one machine.
5. **Every affected package's proof page regenerates**, and the README validation badge recomposes
   from it. The badge reads `src/version.props` and the current proof directory, so the seeding rules
   in the reconvert ritual apply to any regeneration that follows.
6. **The disclosure class's prose narrows but need not disappear** — a permanent entry keeps a class
   alive, and the class text should already carry the "not every entry retires this way" paragraph
   that says so.
7. **The roadmap's retirement-path section is rewritten from a proposal into a record**, present-tense
   for visitors; the history goes to NEWS and the board.

---

## 10. The deployment-shape review

The migration's closing stage, and a ruling rather than a gate. It decides the corpus's published trim
mode, the performance suite's AOT profile, and the test host's publish shape.

**It may not be taken without its evidence**, and the evidence is a checklist of measurements and
named absences, not an impression. The generic checklist:

| Evidence | Bar |
|:--|:--|
| Trim diagnostics under the new SDK, per file | **re-measured**; a risen count is information, not a blocker |
| Trim correctness against the **population** | behavioral Output phase + a roster sample, zero divergence; any divergence names the site |
| The floor, re-measured on the migration's own toolchain | size / startup / working set against a **same-session** control |
| The AOT attribution | §7's prediction answered either way |
| Each retired disclosure entry measured **passing** | the arithmetic must break **loudly** — a silent pass means the entries are not being run |
| Each gate-map entry removed, with its converter guard green | `go test ./...` |
| Each **permanent** entry still failing, with its manifest text still saying why | a pass here is an investigation |
| Host-conditional entries measured repeatedly, on more than one machine | one reading is insufficient by declaration |
| Publish-shape cost, per package and in aggregate | feeds the roster-campaign cost model |
| Publication consequence of the TFM move | §11 |
| Named regressions re-measured | reported whether or not they reproduce |

---

## 11. The publication consequence

**A .NET migration changes the published packages' target framework while changing no Go release** —
so the published version number, which encodes the Go release plus a build counter, carries **no
signal that the runtime moved**. A consumer on the previous runtime restoring the next published
build would receive a compile asset it cannot load.

Three shapes exist and the choice is a ruling, not a default:

- **Do not publish across the migration.** Cleanest when the preceding release is already framed as
  the reference point on the outgoing runtime, and the next Go corpus migration publishes on the new
  one. The migration still **rehearses** the release ritual as a gate.
- **Multi-target.** NuGet's asset selection then answers for both runtimes. Correct, and it multiplies
  the build cost of every converted package — price it, do not assume it.
- **Publish and document the break.** Cheapest and worst; a version scheme that cannot express the
  break is not made to express it by a release note.

Whatever is chosen, the ordering assertion still applies: published versions must remain **monotonic**
across the whole ladder, and monotonicity is **verified with a scripted comparison before the first
publish**, never believed. A non-monotonic sequence on a public feed is not correctable.

---

## 12. Gate accounting — what a .NET migration owes

Stated as an accounting so a skipped gate is distinguishable from a forgotten one — the repository's
habit of writing *"not run; accounting stated"*.

| Gate | Owed? |
|:--|:--|
| `go2cs-stdlib.slnx`, every buildable `$(GoTargetOS)` flavor | **yes**, at the SDK stage and again at the TFM stage |
| `go2cs.slnx` | **yes** at both, and again after any golib API change |
| full behavioral suite (four phases) | **yes** at both |
| `GolibTests` | **yes** at both |
| `check-no-regression.ps1` | **no** for the SDK and TFM properties themselves — CNR measures converter emission and the converter is a Go binary. **Yes** the moment a converter source **or embedded asset** moves (§5.2) |
| converter `go test ./...` | same accounting, plus **yes** whenever the `unsupportedRuntimeCapabilities` map or a csproj template changes |
| full validated-roster sweep | **yes once**, at the migration's closing gate, coordinator-owned and backgrounded |
| full performance suite with AOT | **yes once**, solo, on the perf-canon host — this is the migration whose whole point includes it |
| release-ritual dry run | **yes**, as the closing rehearsal |

Budget every one of these from CLAUDE.md's measured budget table, **from the top of each range**, and
**re-measure and update the table** when a row is exceeded on healthy work — a stale baseline is what
makes a healthy run look hung.

---

## Sources

- `CLAUDE.md` — the measured budget table; the false-green route catalogue; the `GoTargetOS` /
  incremental-build hazard; the golib-API-change rule for `go2cs.slnx`
- [`Roadmap.md`](Roadmap.md) — *"Phase 4 — declared host limits and their retirement path"*, the
  standing answer §9 generalizes
- [`ValidatedTestPackages.md`](ValidatedTestPackages.md) — the disclosure classes, their
  signature-pinning rule, and the self-retiring text §9.1 relies on
- [`PLAN-corpus-upgrade.md`](PLAN-corpus-upgrade.md) — the ruling frame that schedules .NET migrations
  relative to Go corpus migrations, and the scope additions that place the trim-safety and
  deployment-shape work here
- [`PLAN-bflat-perf-exploration.md`](PLAN-bflat-perf-exploration.md) — the concluded floor
  exploration whose findings §8 generalizes, including the trim-diagnostic surface by file
- [`GoCorpusMigration.md`](GoCorpusMigration.md) — the companion runbook
- Source read directly: `src/Directory.Build.props`; `src/go2cs/embeddedTemplates.go`;
  `src/go2cs/csproj-template.xml`, `src/go2cs/test-csproj-template.xml`;
  `src/go2cs/csprojMetadata_test.go`; `src/go2cs/toolchainResolution.go`;
  `src/go2cs/directiveOperations.go`; `src/go2cs/testConversion.go`
  (`unsupportedRuntimeCapabilities`); `src/push-nuget.ps1`; `src/run-validated-sweep.ps1`;
  `src/tests/Behavioral/BehavioralRunner/Program.cs`,
  `src/tests/Behavioral/BehavioralTests/BehavioralTestBase.cs`,
  `src/tests/Performance/PerformanceRunner/Program.cs` (the `*.go` rebuild predicates)
