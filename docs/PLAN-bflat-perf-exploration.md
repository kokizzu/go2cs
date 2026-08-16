# PLAN — bflat performance-floor exploration (laptop G)

> **✅ CONCLUDED 2026-08-16 — outcome §5.1, executed on laptop G (branch
> `claude/bflat-floor-exploration`, merged `987f40298`).** The floor was never a bflat question:
> `TrimMode=partial` rooting every converted assembly whole accounts for ~99% of the size gap, and
> stock-SDK `TrimMode=full` matches bflat's floor (size, startup, working set) from the toolchain
> already installed. No bflat adoption; no fourth column. Adopting the full-trim floor is gated on
> golib trim-safety (the diagnostics are named by file in the RESULTS section at the end of THIS
> document — relocated from the perf README, which stays targeted) and is folded into the .NET 10
> hop evaluation in
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

---

# RESULTS — executed 2026-08-16, laptop G (verbatim from the exploration, relocated 2026-08-16 from the perf README per user ruling: the README stays a targeted document)

## The exploration record

> **Exploratory, and self-contained.** A one-off investigation run under
> [PLAN-bflat-perf-exploration.md](https://github.com/ritchiecarroll/go2cs/blob/master/docs/PLAN-bflat-perf-exploration.md).
> It changes **no build default**, alters **nothing** in the three-column table above, and proposes
> **no new toolchain dependency**. [bflat](https://github.com/bflattened/bflat) appears here as a
> measuring instrument, never as a candidate: per the user + coordinator ruling recorded in that
> plan, its release cadence disqualifies it from becoming a first-class citizen of go2cs builds.
> Numbers come from `run-performance-floor.ps1`, a standalone harness that leaves
> `PerformanceRunner` and `run-performance.ps1` untouched.

The canonical table's weakest story is the **floor**: Native AOT starts ~3× slower than Go, and its
working set carries the whole compiled closure. How much of that floor is recoverable, and how much
of the recovery needs bflat rather than switches the stock .NET SDK already exposes? A null result
would have been an acceptable answer. This is not one.

**Environment:** AMD Ryzen 5 PRO 6650U with Radeon Graphics · Microsoft Windows 10.0.26200 ·
go1.23.1 · .NET SDK 9.0.316 · 2026-08-16 — the same host and conventions as the canonical table, so
the two are directly comparable (this harness independently reproduced the published Startup row:
24.5 vs 25.2 ms Go, 232.0 vs 223.3 JIT, 79.7 vs 77.8 AOT).

Median of 5 runs after 1 discarded warmup. `Startup` reports process wall time; every other row
reports in-program `elapsed_ns:` workload time; memory is peak working set. **Verify gate:** a
variant's timing-filtered stdout had to match the Go binary exactly or it earned no number — every
variant below passed on all five benchmarks.

### The variants

| Key | Build | Role |
|---|---|---|
| `A0` | stock `-p:PerfAot=true` | the control — exactly what the canonical AOT column publishes |
| `A1` | `A0` + `InvariantGlobalization` + `UseSystemResourceKeys` + `StackTraceSupport=false` + `IlcGenerateStackTraceData=false` + `OptimizationPreference=Size` | the plan's "AOT-min" profile |
| `A2` | as `A1` but `OptimizationPreference=Speed` | |
| `X1` | `A0` + `TrimMode=full` | **probe, out of profile** — read the caveat below |
| `X2` | `A1` + `TrimMode=full` | **probe, out of profile** |
| `B1` | bflat `--stdlib DotNet -Ot` | |
| `B2` | bflat `--stdlib DotNet -Os --no-globalization --no-stacktrace-data --no-exception-messages` | |

bflat pinned at **v10.0.0-rc.1**, `bflat-10.0.0-rc.1-windows-x64.zip`, SHA256
`59FC623E751AE1AA8C7A6531B356F83A9063A7F0B13C835E43ACE44ADE2D24CD` (MIT). `--stdlib:zero` and
`--no-reflection` were out of scope throughout: golib's `fmt` formatting and sort's `Interface<T>`
bind members reflectively.

### Finding 1 — the size floor is a trim-ROOTING question, not a toolchain question

Executable on disk (MB). The JIT column is its framework-dependent output tree excluding `.pdb`,
and is **not** a deployable size — it needs the shared runtime installed.

| Benchmark | Go | JIT tree | `A0` | `A1` | `A2` | `X1` | `X2` | `B1` | `B2` |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Startup | 2.12 | 22.41 | 288.26 | 221.74 | 234.38 | 10.94 | 8.77 | 8.73 | 7.74 |
| Fib | 2.12 | 22.41 | 288.26 | — | — | 11.11 | 8.93 | 8.88 | 7.88 |
| Map | 2.13 | 22.41 | 288.26 | 221.74 | 234.38 | 11.12 | 8.93 | 8.88 | 7.89 |
| String | 2.12 | 22.41 | 288.26 | — | — | 11.11 | 8.93 | 8.88 | 7.88 |
| Channel | 2.12 | 22.41 | 288.26 | — | — | 11.12 | 8.94 | 8.89 | 7.89 |

The stock profile emits **288 MB for a program that prints two lines**, and that figure barely moves
across benchmarks because it is not the benchmark — it is the whole converted standard library.

Of the ~279 MB between the stock profile and bflat, **~99% is `TrimMode=partial` and ~1% is the
feature switches.** `partial` roots all 57 assemblies of the converted closure whole; bflat's ILC has
no rooting concept and always compiles to reachability. Give the stock SDK the same policy and it
lands at 8.77 MB against bflat's 8.73 — a 0.5% difference, which is nothing.

That is the result. bflat is the same ILC/RyuJIT the AOT column already uses; once rooting policy
matches, it offers no size advantage the installed SDK cannot reach on its own.

### Finding 2 — startup and working set fall with it

Peak working set on a ~33 ms process is sampling-sensitive — a 5-run pass gave one variant 2.5 MB —
so Startup was re-measured at 15 runs. `A1`/`A2` were published later and measured in their own
5-run pass; the two passes are shown separately with their own Go baselines rather than blended,
since the baseline itself moved 23.5 → 24.5 ms between them. Ratios are always against the Go value
from the same pass.

| Startup, 15-run pass | Go | JIT | `A0` | `X1` | `X2` | `B1` | `B2` |
|---|---:|---:|---:|---:|---:|---:|---:|
| wall time (ms) | 24.5 | 232.0 | 79.7 | 33.0 | 33.2 | 34.5 | 33.6 |
| vs Go | 1.00× | 9.47× | 3.25× | **1.35×** | **1.36×** | 1.41× | 1.37× |
| peak working set (MB) | 2.5 | 43.7 | 74.4 | 8.0 | 6.8 | 6.7 | 8.3 |

| Startup, 5-run composites pass | Go | `A1` | `A2` |
|---|---:|---:|---:|
| wall time (ms) | 23.5 | 78.5 | 76.0 |
| vs Go | 1.00× | 3.34× | 3.23× |
| peak working set (MB) | 2.5 | 75.8 | 70.7 |

**AOT startup goes from 3.25× Go to 1.35×, and peak working set from 74.4 MB to 6.8 MB** — an 11×
reduction — purely from the rooting policy. The feature switches alone move neither: `A1`/`A2` come
in at 3.34× and 3.23× against their own baseline, with working set barely shifted (75.8 and 70.7 MB
against `A0`'s 74.4). This is the plan's floor question answered — most of the AOT startup gap was
initialization and paging of a closure the program never touches.

Workload time and peak working set on the other four benchmarks (one internally consistent 5-run
pass):

| Benchmark | metric | Go | JIT | `A0` | `X1` | `X2` | `B1` | `B2` |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| Fib | ms | 117.7 | 181.1 | 175.6 | 175.4 | 175.3 | **70.9** | 99.9 |
| Fib | MB | 5.2 | 45.5 | 75.6 | 13.6 | 13.4 | 13.7 | 13.6 |
| Map | ms | 628.2 | 469.6 | 236.3 | 227.7 | 222.6 | 236.8 | 237.3 |
| Map | MB | 157.7 | 164.1 | 194.5 | 132.1 | 132.2 | 130.5 | 130.4 |
| String | ms | 107.2 | 1029.4 | 1275.5 | 1311.6 | 1284.9 | 1109.1 | 1114.0 |
| String | MB | 5.2 | 54.5 | 84.8 | 22.3 | 22.1 | 22.4 | 22.1 |
| Channel | ms | 41.2 | 87.3 | 130.5 | 120.7 | 109.0 | 116.1 | 79.8 |
| Channel | MB | 5.2 | 49.4 | 83.9 | 22.0 | 20.7 | 21.5 | 17.6 |

Working set drops on every row and, on Map, falls **below Go** (132 vs 158 MB). Trimming costs no
measurable workload time on the SDK side: `X1`/`X2` track `A0` within noise everywhere.

### Finding 3 — build time collapses too

Wall-clock seconds for one benchmark's build, measured on this host:

| | `A0` | `A1` / `A2` | `X1` / `X2` | `B1` | `B2` |
|---|---:|---:|---:|---:|---:|
| seconds | 977–1049 | 1532–1662 | 28–29 | 28–38 | 24–25 |

The suite's ~17-minute AOT publishes are almost entirely ILC compiling code no benchmark can reach.
Under full trim the same publish links in under 30 seconds — a ~34× difference measured across four
independent benchmarks. Note that the feature-switch profiles are *slower* to build than the stock
one (~1600 s vs ~1000 s): they add substitution work while still rooting everything.

The rooting policy dominates disk too. This exploration peaked at **~12 GB** — 2.5 GB of published
binaries across 29 variant trees, 8.7 GB of ILC native intermediates, and 0.8 GB of pinned bflat
archives — and a single stock AOT publish also drops a **1.5 GB `.pdb`** beside its 288 MB
executable. All of it was purged afterward; the harness deletes each publish's `.pdb` as it goes,
and excludes `.pdb` from every size figure above.

### Finding 4 — bflat has a real CPU advantage, and it is not attributable to bflat

`B1` runs **Fib in 70.9 ms against the SDK's 175.6** — 2.5× faster, and faster than Go itself
(117.7 ms). An independent 11-run pass reproduced it (71.4 vs 175.7, Go 120.5). On String it is ~14%
faster. This is the one place bflat shows something the stock SDK did not.

It probably is not bflat. bflat v10.0.0-rc.1 ships the **.NET 10.0.0-rc.1** ILC and framework, newer
than this host's SDK 9.0.316, so the comparison mixes "bflat" with "one .NET generation of RyuJIT
improvements". The obvious control — bflat v8.0.2 on a .NET 8 base — **cannot be run at all**:

```
error CS1705: Assembly 'golib' ... uses 'System.Runtime, Version=9.0.0.0' which has a higher
version than referenced assembly 'System.Runtime, Version=8.0.0.0'
```

and installing a .NET 10 SDK to control it from the other side would mutate the perf-canon host's
toolchain, which this suite does not do to itself. So the CPU row stays **unattributed** — and per
the plan, CPU-bound gains from newer .NET belong to the corpus-upgrade ladder, not to this
exploration. It is a reason to want the .NET 10 hop measured, not a reason to want bflat.

### Finding 5 — the only bflat that can consume this corpus is a pre-release

The error above is from the last **stable** bflat, v8.0.2 (2024-02-29, SHA256
`25B03214C6085607EC2EC5FC86139C93E054EDD60266296E708F763455961E7E`). It is a .NET 8 toolchain and
the corpus targets net9.0, so it cannot link the corpus at all. Adoption would therefore have meant
pinning **v10.0.0-rc.1** — a pre-release with no stable successor — and every .NET hop reopens the
question. That is the plan's re-measurability caveat in concrete form.

### The caveat on `X1`/`X2` — read before quoting these numbers

`TrimMode=full` is **not a recommendation**, and these rows are **not a proposed publish profile**.
The plan fixes `TrimMode=partial` for a real reason: golib's `fmt` formatting and sort's
`Interface<T>` bind members through reflection, and full trim can remove exactly those. Both probes
passed Verify on all five benchmarks — but five small programs are a sample, not the population, and
they do not exercise `fmt`'s reflective surface broadly. Read every `X` row as *"the floor is at
least this low"*, never as *"adopt this"*.

What would have to happen first is now precisely located. Every reachability-trimmed build emits 94
trim-analysis diagnostics, concentrated in golib's adapter and reflection layer:

| File | Diagnostics |
|---|---:|
| `golib/TypeExtensions.ExtensionMethodRegistry.cs` | 16 |
| `golib/GoReflect.FieldAccess.cs` | 14 |
| `golib/AdapterBinder.cs` | 14 |
| `core/reflect/value_impl.cs` | 8 |
| `golib/TypeExtensions.GoMethodSets.cs` | 8 |
| `golib/GoReflect*.cs` (3 files) | 18 |
| others (`PointerExtensions`, `builtin`, `array`, `managed_impl`) | 16 |

Making that surface trim-safe (`DynamicallyAccessedMembers` annotations, `DynamicDependency`) is the
prerequisite for any of this shipping — and it would unlock the floor for the stock SDK and bflat
alike, which is one more reason bflat cannot be the answer. Note that 22 of the 94 are IL3050
(`RequiresDynamicCode`), which applies to the **existing** AOT column too and is not introduced by
trimming.

A related observation: `Directory.Build.props` here sets `SuppressTrimAnalysisWarnings=true`, so the
SDK's own AOT publishes hide this diagnostic. bflat surfaced a surface our configuration was
suppressing.

### Verdict against the plan's worthiness gate

**Outcome 1 — Arm A captures the floor win; bflat is confirming data, no new toolchain.** Size,
startup, and working set are all recoverable inside the stock SDK, and bflat matches rather than
beats it on every one. Its lone advantage is CPU, which is unattributable and belongs to the .NET
ladder. The pre-release-only constraint (Finding 5) independently rules out adoption.

The floor win is **not yet claimable**, because the lever is a setting the plan fixed for
correctness. The honest statement is that **the floor is gated by golib's trim-safety, not by the
toolchain**, and the follow-on work is golib annotation — not a fourth column, and not bflat.

Scope actually run, so the gaps are visible: `A1` was measured on Startup and Map only, and `A2`
likewise; single-switch attribution (`InvariantGlobalization`, `UseSystemResourceKeys`,
`StackTraceSupport`, `IlcGenerateStackTraceData` in isolation) was **not** run. Both were dropped
once the trim result made the switch bundle's ~23% a rounding error against the ~99% the rooting
policy carries. Reproduce or extend with:

```powershell
cd src/tests/Performance
./run-performance-floor.ps1 -Benchmarks PerfStartup -Variants 'A0,A1,X1,X2' -Phase 'publish,verify,measure'
```
