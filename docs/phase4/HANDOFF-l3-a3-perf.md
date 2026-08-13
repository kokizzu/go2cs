# HANDOFF — L3 stage A3, perf-suite completion (machine transfer)

> **Written 2026-08-13** on the work laptop (Ryzen 7 PRO 6850U) after its A3 perf run was
> deliberately stopped: that machine is needed interactively and its lid closes during the day,
> which both contaminates the Measure phase and kills multi-hour runs (three sleep/crash kills
> this week). The remaining A3 work transfers to a dedicated idle machine via THIS branch
> (`claude/l3-zh-box-a3-pinned-measure`). Git is the bus — nothing else moves between machines.
> The session prompt for the new machine is at the bottom; it is self-contained.

## Arc state (do not redo any of this)

| Stage | State | Record |
|---|---|---|
| A1 census | **MERGED** to master | `docs/phase4/CENSUS-zh-box-a1.md`; projection confirmed |
| A2 emission core | **MERGED** to master (rode the L11+train leveling regen `c00bdadb7`) | nonnil, seven-row call sites, plain locals, defer/go carve-out, guards, 9.32× JIT wall-clock |
| A3 verdict measure | **BANKED on this branch** — `a3e32af44`, pushed | **P256 TestAllocations = 8,528 obj/run through the real `-tests` pipeline on pinned go1.23.1** — under the ≤10,000 acceptance (projection ~7,000; A2's artifact-laden mirror read 10,105). Residual decomposes to the named classes (3b + backings). §7 design table updated; board write-back done; the two A2-owed validation items (§3.5 func-value adapter, io canary) confirmed from the clean 129-package pinned sweep evidence. |
| A3 perf suite | **OPEN — this handoff's scope** | JIT + AOT columns, publish size + ILC time, README table, final report |

## What remains (the §9 A3 row's last obligation)

Full perf-suite run per `CLAUDE.md`'s Performance-comparison section, on a **solo, sleep-proof
machine**: phases Transpile → Build → Verify → Measure (Verify gates Measure — identical
timing-filtered stdout across Go binary / C# JIT / C# Native AOT before anything is timed).
Include the ж-bound benchmark A2 added (PerfRefLower, paired A/B protocol). Record per-benchmark
**publish size + ILC time** for the AOT column. Finish with `--update-readme` so the results
table banks between the PERF-RESULTS markers (prior toolchain tables accumulate in History).
The suite is now **14 benchmarks** — CLAUDE.md's "8" row predates the growth; expect the AOT
grind to dominate wall-clock (~25 min/publish on 2014-desktop-class hardware; measure your own).

## History the numbers must respect

- The work laptop's two aborted runs produced **no usable perf numbers**. Its first-run
  PerfStartup AOT publish (1,846 s) was retry-inflated (first attempt exit 1, self-heal from
  clean intermediates) and then the machine crashed; the re-run was killed deliberately for this
  transfer. None of that data may appear in the table — note the history as an ILC-time caveat
  only if a first-attempt failure recurs.
- The work laptop's 29 uncommitted `src/tests/Performance/Perf*/{.csproj,package_info.cs}`
  transpile artifacts were **deliberately not transferred** — the runner re-transpiles them on
  every run. Your run regenerates them; classify them at report time (perf-csproj regeneration
  is normal runner behavior per CLAUDE.md).
- JIT per-project attribution on the work laptop: **0 failed** — the one batch error was the
  known parallel-build race. If your batch build errors, attribute the same way before believing.

## Runner invocation and observed quirks (from the stood-down session)

- **Exact invocation:** `.\run-performance.ps1 --update-readme` from `src/tests/Performance`,
  with `MSBUILDDISABLENODEREUSE=1` in the environment; default 5-run medians (no `--runs`
  override — keeps the standing table's protocol).
- **JIT batch quirk:** a warm one-shot parallel build of the 14 benchmarks may report errors
  that per-project attribution then resolves to **0 failed** (the known parallel-build race).
  The post-reboot cold build compiled clean first try. If "build-all reported errors" appears,
  expect benign attribution — not a corpus defect.
- **AOT retry quirk:** PerfStartup's first publish attempt exited 1 **on a clean tree** and
  self-healed via the retry-from-clean-intermediates path (1,846 s total — retry-inflated, so
  the logged time of an absorbed retry is NOT usable in the table). A cold machine's first
  publish may exit-1 once and be absorbed; record the retry as a caveat, never its time.
- **Hygiene:** on a fresh clone this is moot, but if any pre-existing publish artifacts could
  be reused, clean every `Perf*/obj/aot` and `Perf*/bin/Release/aot` first.
- **Verify phase:** never reached in either aborted attempt — no known quirks; treat its
  verdicts fresh.
- **Owed alongside the table:** the board's A3 section deliberately carries **no perf paragraph
  yet**. The new machine owes that paragraph, plus §9's publish-size recording
  (`Perf*\bin\Release\aot\<proj>.exe` sizes) and ILC times (the runner's `ok (NNNs)` lines),
  alongside the README table `--update-readme` produces.

## New-machine provisioning (once)

1. `git clone https://github.com/ritchiecarroll/go2cs` and
   `git checkout claude/l3-zh-box-a3-pinned-measure`.
2. **Go toolchain pin — HARD GATE.** Install any Go ≥ 1.21, then
   `go env -w GOTOOLCHAIN=go1.23.1` and verify `go env GOVERSION` prints **go1.23.1**.
   If it prints anything else, STOP — no measurement is valid off the pin (L8's guard will also
   refuse). First use downloads the 1.23.1 toolchain automatically.
3. .NET 9 SDK (`dotnet --version` → 9.x).
4. **AOT prerequisite:** VS 2022 with the "Desktop development with C++" workload (MSVC
   `link.exe`). Check: `Get-ChildItem "C:\Program Files\Microsoft Visual Studio\2022\*\VC\Tools\MSVC\*\bin\Hostx64\x64\link.exe"`.
   If absent and installing is unwanted, run the suite `--no-aot` and report the AOT column as
   OWED with that reason — never fake it.
5. One interactive `git push` for credential-manager browser auth.
6. **Sleep-proof the machine**: AC power; sleep/hibernate OFF on AC; lid action "do nothing"
   (this arc has been killed by sleep three times).
7. GPG optional — plain commits are protocol-legal from a keyless machine.
8. Claude Code; paste the session prompt below.

## Session prompt for the new machine (paste verbatim)

```
You are completing go2cs lane L3 stage A3 — the perf-suite measurement — on a dedicated idle
machine. The stage's verdict work is already banked; ONLY the perf suite and the final report
remain. Branch: claude/l3-zh-box-a3-pinned-measure (already checked out in this clone).

Read first, in order: (1) CLAUDE.md end to end — authoritative, especially the Performance
comparison suite section, corpus mechanics, and the timing-budget doctrine; (2)
docs/phase4/HANDOFF-l3-a3-perf.md — the transfer document that brought you here: its arc-state
table is what you must NOT redo, and its history section constrains your numbers; (3)
docs/phase4/LANES.md protocol + L3 section; (4) docs/phase4/DESIGN-zh-box-reduction.md §7 and
§9's A3 row (your work order), §5 acceptance rows; (5) the board's ж-box A3 section.

Gate zero: go env GOVERSION must print go1.23.1 — STOP and report if not.

The work: run the full perf suite solo — src/tests/Performance/run-performance.ps1, phases
Transpile/Build/Verify/Measure, AOT included if MSVC link.exe exists (else --no-aot and the AOT
column is reported OWED, never faked). Verify must pass before any timing counts. Record
publish size + ILC time per benchmark. Include the ж-bound PerfRefLower paired A/B benchmark.
Finish with --update-readme so the results table lands between the PERF-RESULTS markers.
Budgets: this machine is unmeasured — start from CLAUDE.md's slow-host tops (AOT ~25 min per
publish on 2014-desktop class, 14 benchmarks = plan for hours), measure actuals, and report
them so the timing table can gain a row. A timeout is a safety net, never a performance
assumption; a healthy-but-slow publish is NOT a failure.

Rules: blocking waits only — never end a turn expecting a background wake-up; poll long
children with bounded in-call waits. Push after every commit (subjects "L3:"; plain commits
fine if no GPG key). PS 5.1 syntax on Windows PowerShell (no &&, no ternary); absolute paths;
converter stderr needs $ErrorActionPreference='Continue'. Never run -stdlib in any emitting
form against the repo tree. Never dotnet build-server shutdown; never bare-name process kills —
path-scope everything to this clone. Classify the Perf* transpile diffs your run regenerates
(normal runner behavior) rather than banking them blind; commit the README table update and
any classified artifacts per CLAUDE.md house rules.

Final report (deliver in-session for relay to the coordinator): (1) toolchain gate result and
machine identification (CPU, cores, RAM); (2) the perf table incl. AOT column, publish size +
ILC time, load conditions — state plainly the machine was solo; (3) Verify-phase results
(identical-output gate) per benchmark; (4) gate results; (5) branch/SHAs/push confirmation;
(6) coordinator items — anything that reprices the A'/B' checkpoint, which presents A3's
numbers next. STOP after A3 — no merge (the coordinator re-gates and merges), no A' work, and
do not edit LANES.md's STATUS column.
```
