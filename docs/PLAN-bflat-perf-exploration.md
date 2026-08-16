# PLAN — bflat performance-floor exploration (laptop G)

> **✅ CONCLUDED 2026-08-16 — outcome §5.1, executed on laptop G (branch
> `claude/bflat-floor-exploration`, merged `987f40298`).** The floor was never a bflat question:
> `TrimMode=partial` rooting every converted assembly whole accounts for ~99% of the size gap, and
> stock-SDK `TrimMode=full` matches bflat's floor (size, startup, working set) from the toolchain
> already installed. No bflat adoption; no fourth column. Adopting the full-trim floor is gated on
> golib trim-safety (the diagnostics are named by file in the Exploration section of
> `src/tests/Performance/README.md`) and is folded into the .NET 10 hop evaluation in
> `PLAN-corpus-upgrade.md`, alongside the single-file host that retires the `host-limit`
> disclosures — one deployment-shape decision, two payoffs. The one CPU anomaly (Fib under bflat's
> .NET-10-preview codegen) is unattributable to bflat and stands as an argument for measuring the
> hop itself.

> Exploratory. Nothing in this plan changes the canonical performance table, user-facing docs, or
> any build default until the worthiness gate at the end passes. Executes on **laptop G** (the
> designated perf-canon host) while idle — no earlier than 2026-08-16 per user timing. Dated
> measurements are correct in this document by design (it IS the record); user-facing prose stays
> untouched and abstract per the standing docs rules.

## 0. The question

[bflat](https://github.com/bflattened/bflat) is Roslyn + the same NativeAOT ILC/RyuJIT the AOT
column already uses, packaged as a single Go-style compiler with aggressive feature-stripping
flags. The question is NOT "is bflat's codegen faster" — it is the same code generator, and
tight-loop rows are expected to land within noise of the existing AOT column. The question is:
**how much of the AOT floor — startup time, binary size, working set — is recoverable**, and how
much of that recovery needs bflat at all versus feature switches the stock SDK already exposes.
The floor is the table's weakest story (AOT startup ~3x Go; working set carries the whole compiled
closure), so even a null result closes a real question with receipts. CPU-bound improvement is not
this exploration's territory at all: it arrives with newer .NET versions on the corpus-upgrade
ladder (same ILC lineage, newer codegen), which is one more reason bflat is exploratory-only —
user + coordinator ruling, 2026-08-15 — and its release cadence alone disqualifies it from ever
being a first-class citizen of go2cs builds.

## 1. Facts pinned (verified against the repo, 2026-08-15)

- Latest **stable**: **v8.0.2** (2024-02-29), .NET 8 base, C# 12-era Roslyn. Latest artifact:
  **v10.0.0-rc.1** (2024-11-13), pre-release, own notes say "Reflection disabled mode is very
  disabled"; no stable release since. Single-maintainer cadence — treat any adoption as a PINNED
  release with a re-measurability caveat at every .NET hop.
- Corpus reality: converted projects target net9.0 / C# latest and REQUIRE the `go2cs-gen` source
  generators at compile time. bflat does not run Roslyn source generators. golib semantics are
  load-bearing on GC, exceptions, and reflection (`fmt` formatting, sort's `Interface<T>`).
  Therefore: `--stdlib:zero` and `--no-reflection` are OUT OF SCOPE for any benchmark that runs
  real converted code. The viable mode is `--stdlib` DotNet.
- Version-skew risk: try **v10.0.0-rc.1 first** (Roslyn recency for C#-latest sources); fall back
  to v8.0.2 only if the RC fails, and record which language features break if both do — that
  finding alone is worth the spike.

## 2. The two arms (attribution is the point)

**Arm A — stock-SDK "AOT-min" profile (the control, zero new toolchain).** The existing
`PerfAot=true` publish plus feature-stripping properties, applied per-benchmark:
`InvariantGlobalization=true`, `UseSystemResourceKeys=true`, `StackTraceSupport=false`,
`OptimizationPreference=Size` (and a second variant with `Speed`), `IlcGenerateStackTraceData`
off where distinct. Keep `TrimMode=partial` and reflection ON (golib needs both). Any benchmark
whose Verify-phase stdout changes under a switch DISQUALIFIES that switch (correctness first).
Arm A survives every future .NET hop; if it captures the floor win, it is the durable-path
outcome and bflat becomes a data point rather than a dependency.

**Arm B — the bflat spike.** Recipe per benchmark:
1. Normal Release build once with `EmitCompilerGeneratedFiles=true` → harvest the generator
   outputs (the four go2cs-gen generators' emitted sources).
2. Build the golib + referenced `core/*` closure as Release IL assemblies (normal dotnet build).
3. `bflat build` the benchmark's own sources + harvested generated sources, `-r` referencing the
   closure IL assemblies, DotNet stdlib, x64, `-O2`; a second configuration adds the stripping
   flags (`--no-globalization --no-stacktrace-data --no-exception-messages -Os`).
4. **Step 0 of the spike verifies the `-r` IL-reference path actually links a golib-dependent
   binary at all** — if it cannot, record why and stop Arm B early.
Verify before timing, always: identical timing-filtered stdout vs the Go binary, exactly the
PerformanceRunner doctrine — nothing that fails Verify gets a number.

## 3. Benchmark set and measurements

Five benchmarks give coverage without noise: **Startup** (the floor question itself), **Fib**
(pure compute — expected parity, the honesty row), **Map** (runtime-heavy current win),
**String** (string-materialization current loss), **Channel** (scheduling emulation). For each
variant record: elapsed (runner convention, same run counts as the canonical table), **binary
size on disk**, **peak working set**. Hardware/environment line per the perf README methodology.

## 4. Mechanics and hygiene on G

- Standalone script `src/tests/Performance/run-performance-bflat.ps1` (or a scratch script first —
  do NOT modify `PerformanceRunner` or `run-performance.ps1` in the spike). Results go to a NEW
  dated **"Exploration — performance floor"** section in `src/tests/Performance/README.md` (and
  mirrored context in `docs/Performance.md` ONLY if the gate below passes). The canonical
  three-column table is not touched.
- Pin and record the exact bflat release + SHA in the results section. bflat binaries are MIT.
- G runs this solo/idle (perf numbers on a quiet machine only). Purge bflat downloads, harvested
  trees, and publish output when done; note disk cost while running.
- Branch `claude/bflat-floor-exploration`; signed commits; push, no merge — coordinator merges.

## 5. Worthiness gate and outcomes

Three outcomes, each acceptable:
1. **Arm A captures the floor win** (startup/size/working set materially improve, Verify green):
   adopt as an optional documented publish profile (a `PerfAotMin` flag or docs recipe); bflat
   recorded as confirming data. No new toolchain dependency.
2. **Arm B shows something Arm A cannot reproduce** (e.g. materially smaller/faster-starting
   binaries with identical output): THEN price the fourth column — against the pinned-release
   re-measurability caveat (no stable since 2024-02; every .NET hop re-opens the question). A
   canonical column requires: runner integration, Verify parity, and the perf-canon rules
   (regenerated wholesale on G, History preserved). More than one bflat column only if the
   stripped vs unstripped configurations genuinely diverge.
3. **Null or blocked** (version skew, link failure, Verify divergence): record the census in the
   Exploration section and close — the "why not bflat" answer is then written down where the next
   person with this idea will find it.

Whatever the outcome, the floor numbers feed the .NET 10 hop evaluation (`PLAN-corpus-upgrade.md`)
— single-file/self-contained host work and the `host-limit` retirement path live there, and a
measured floor map makes that pricing honest.
