# CIMatrix — the on-demand OS matrix

> `.github/workflows/os-matrix.yml` runs go2cs's own instruments on GitHub-hosted runners, on
> demand only. It reaches platforms the fleet does not own — **both macOS legs above all** — and
> gives the Linux lane a second, independent host. It is **never a merge gate**: the fleet's
> machines and their measured budgets stay authoritative, and nothing this workflow produces
> lands in the repository. See [`../CLAUDE.md`](../CLAUDE.md) for corpus mechanics, `GoTargetOS`
> flavors and the budget table the timeouts are scaled from, and
> [`PLAN-linux-operation.md`](PLAN-linux-operation.md) for the F-series census-first pattern the
> darwin leg replays.

## The three fits

**1. The darwin census — the one thing nothing else can do.** Layout L3 emits a `darwin/` folder
for every platform-varying package, and `-p:GoTargetOS=darwin` selects it, so the flavor *exists*
in the corpus. It has only ever been compiled **cross-target from Windows**, where it stops at a
known wall in `os`'s directory walk (CLAUDE.md's L3 gate lessons; census it, don't re-diagnose).
What has never happened anywhere is that build **on Apple hardware** — and there are two of those,
arm64 and x64, which are different compilations of the same flavor. A darwin dispatch fans out to
both. This is the Phase-3 doctrine applied to a platform: **compile is the milestone, operational
is later**, and the census is the deliverable whether or not it is green.

**2. The native-Linux control.** Every Linux reading the campaign holds comes from one WSL distro
on one machine. A hosted `ubuntu-latest` is a second Linux with a different kernel, a different
filesystem and no Windows host underneath it. A finding that reproduces on both is a property of
the platform; a finding that reproduces on only one is a property of the topology. That
distinction is worth a dispatch before a Linux arc is planned around it.

**3. Hop-shard overflow.** The validated sweep is the fleet's long pole, and a corpus hop
multiplies it. When every machine is committed, a shard can run here instead of waiting — filtered,
one dispatch per shard, results read from the artifacts.

## How to trigger

Actions → **OS matrix (on demand)** → *Run workflow*. Or from the CLI:

```
gh workflow run os-matrix.yml -f goos=darwin -f stage=census
gh workflow run os-matrix.yml -f goos=linux  -f stage=sweep-shard -f filter=compress
```

| Input | Values | Meaning |
|:--|:--|:--|
| `goos` | `windows` · `linux` · `darwin` | The corpus flavor to bind. Each maps to a runner + RID pair; **`darwin` fans out to BOTH mac runners** (arm64 and x64) in one dispatch. |
| `stage` | `census` · `behavioral-smoke` · `sweep-shard` | What to run. One stage per dispatch. |
| `filter` | free text, optional | The shard. A package substring for `sweep-shard`, a project-name substring for `behavioral-smoke`. Blank takes the stage's documented default. |

There is no `all` value: one dispatch per flavor keeps each leg's artifacts, budget and verdict
separate, which is what makes a wall readable. Dispatching all three is three cheap clicks.

The Go toolchain is **derived, never written here** — the workflow reads `<GoStdLibVersion>` out of
`src/version.props` and hands that exact release to `setup-go`. Both the converter and the sweep
refuse a toolchain whose release differs from that pin (such a run is NOT MEASURED, never a
verdict), so a hardcoded version in CI could only duplicate the pin or silently contradict it.

## The stages

### `census` — compile the corpus flavor and bucket the wall

Builds `src/go2cs-stdlib.slnx` at `-c Debug -m --no-incremental -p:GoTargetOS=<goos>
-p:UseSharedCompilation=false -clp:ErrorsOnly`. `--no-incremental` is not optional for a
target-changing pass: what differs between flavors is the `<Compile>` **item set**, not any source
timestamp, so an incremental build validates the other target's assemblies.

It then reports the census the way the repository measures one — **packages-compiling first**:
projects under `src/core`, assemblies actually produced (each project's own
`bin/Debug/<TargetFramework>/<AssemblyName>.dll` — the probe **derives** that folder rather than
spelling it, so a framework hop cannot make it report zero on a green build), diagnostics bucketed
by code (raw and distinct), the same buckets by project, and the first distinct errors inline.
Everything lands in the job summary and in the artifact. Raw error count is deliberately *not* the
headline: fixing a file-inclusion defect raises it by surfacing what was hiding behind the failure.

### `behavioral-smoke` — prove the harness runs on the runner

Builds the converter explicitly (`go build` in `src/go2cs`) and then runs a filtered
`run-behavioral.ps1` pass over a small pure-compute family — the `Defer` projects by default:
defer/panic/recover ordering printed through `fmt`, with no clock, filesystem or socket in them, so
a failure indicts the harness or the corpus flavor rather than platform semantics.

The explicit converter build is there on purpose. A hosted runner has no `go2cs` binary at all, and
every harness rebuild predicate keys on a converter `*.go` being newer than the binary — which a
toolchain change never touches. Building it as its own step also separates a Go-toolchain failure
from a harness failure in the log.

### `sweep-shard` — a filtered validated sweep against `go test`

Runs `run-validated-sweep.ps1 -Filter <filter>` under `pwsh`, with `setup-go` supplying the
`go test -json` baseline toolchain the comparison needs. `container/heap` is the default filter —
the fleet's provisioning calibration row, and the right answer to "does a sweep run here at all".
Per-row evidence (each swept package's comparison record and results files) is collected into the
artifact under `rows/`, alongside the full log and a post-sweep `git status` of `src/core`.

Keep shards small. The sweep's own per-package deadline floors reach into the hours, and the job
budget is a ceiling, not an allowance.

## Results flow

Nothing is automatic and nothing is written back. Every leg uploads an `artifacts/` bundle
(**7-day retention**, uploaded on failure too) and writes its headline table into the run's job
summary, so a verdict is readable without downloading anything.

**The triggerer owns the relay.** Read the summary, download the artifact if the detail matters,
and carry the finding to the status board or the fleet mailbox in the same words the fleet uses —
a census as a census (assemblies produced, buckets by code), a sweep row as its arithmetic. A
result that stays in a GitHub run page is a result nobody has.

## Reading a run

- **The checkmark answers the instrument, not the exercise.** Each stage exits with its own tool's
  verdict, so a darwin census that walls shows red — and that red *is* the deliverable. Read the
  summary.
- **A non-Windows sweep exits non-zero even when it validates.** Rows with no per-OS annotation in
  the roster report as **comparison-validated-at-count**: the comparison matched verdict for
  verdict, but nothing is banked for that OS to bank against. That is a reading, not a regression,
  and it retires row by row as annotations land.
- **A timeout is NOT MEASURED, never a failure of the corpus.** If a leg dies on its budget, raise
  the named budget and re-run; do not read it as a wall.
- **The disk preflight is bypassed here.** Hosted images sit near or under the harness's free-space
  floor, so the harness stages pass `-IgnoreDiskPreflight`. The free-space number is recorded in
  each leg's environment report *before* anything runs, so a genuine mid-run exhaustion — which
  surfaces as corpus failures, not as a disk message — can still be recognized rather than guessed.
- **The environment report is calibration data.** Cores, RAM-class, toolchain versions and free
  space per runner: the first dispatches are what anchor these runners the way the fleet roster
  anchors its machines. Until then every budget here is an estimate.

## What this is NOT

- **Not a merge gate, ever.** No `push` trigger, no `pull_request` trigger, no schedule. A banking
  merge's proof still comes from the fleet's instruments on the fleet's machines.
- **Not a substitute for the sweep, CNR or the behavioral suite.** It runs the same scripts, but a
  hosted runner is an unanchored machine; a number measured here is provisional until a fleet
  machine agrees or the runner is itself calibrated.
- **Not a place that writes to the repository.** It has read-only permissions, banks nothing,
  updates no roster row and pushes no branch. A validated count seen here is a *candidate* for
  banking through the normal policy, on a pinned machine.
- **Not a budget baseline.** The timeouts below are scaled estimates, not measurements. Replace
  them with measured figures once real dispatches exist.

## Budgets

| Stage | Base | macOS legs | Scaled from |
|:--|--:|--:|:--|
| `census` | 150 min | 225 min | The full stdlib solution build `--no-incremental` on the fleet's slowest anchored box, plus a **cold** NuGet restore of every project. |
| `behavioral-smoke` | 90 min | 135 min | Converter `go build` plus a filtered four-phase run whose shared core closure is built from nothing. |
| `sweep-shard` | 210 min | 315 min | One convert/build/run/compare cycle per matched row; the sweep's own per-package floors are the reason this is the largest. |

The macOS multiplier is 1.5x because `macos-15` (arm64) is a 3-core runner and every phase here is
parallel-MSBuild bound. GitHub's hard ceiling for a hosted job is 360 minutes and every value stays
under it. A timeout is a safety net against a hung child, never a performance assumption — these
sit well above the top of each scaled range on purpose.

`BehavioralRunner` carries **its own** budgets that no caller-side timeout can influence, so the
smoke stage sets them explicitly through their documented environment variables
(`GO2CS_BUILD_TIMEOUT` and friends), sized for a cold four-core runner where the Transpile phase
rewrites every `.cs` immediately before Compile and warm state can never make the batch a no-op.

## darwin known-unknowns

Everything below is untested by construction — the first dispatch is the experiment. Named here so
a failure is recognized rather than re-diagnosed:

- **Whether the darwin corpus's wall is the same one.** The cross-target census from Windows names
  a specific stop in `os`'s directory walk. Compiling on macOS may hit it identically, hit it plus
  more, or reach further before stopping. Expect a wall; the shape of it is the finding.
- **Whether the two architectures wall identically.** `osx-arm64` and `osx-x64` compile the same
  sources, so a divergence between them is a finding about the corpus, not about macOS.
- ~~**Whether anything past the census can run at all.**~~ **SETTLED 2026-08-25 — it cannot, yet.**
  The first darwin `behavioral-smoke` compiled and golden-matched 20/20, then failed all twenty at
  Output with `exit code mismatch: C# 2 vs Go 0`. Darwin's syscall entry points are libc **assembly**
  trampolines in Go, emitted as bodyless partials and filled with throwing stubs, so a converted
  program dies on its first write. Evidence and remedy shapes:
  [`phase4/FINDING-darwin-run-layer.md`](phase4/FINDING-darwin-run-layer.md). Until that run layer
  exists a darwin `behavioral-smoke` or `sweep-shard` reports this uniformly — a known state, not a
  new finding, and not worth a runner hour to re-observe.
- ~~**What an explicit `darwin` binding does.**~~ **SETTLED 2026-08-25 — it works.** `GoTargetOS` in
  this workflow's `env:` block does reach MSBuild, and a macOS leg builds and runs the darwin flavor.
  Proved by the contrapositive: the identical env-only mechanism on Linux passed 20/20, and `fmt`'s
  closure carries thirteen L3 packages including `os`, `runtime` and `syscall`, so a windows flavor
  would have faulted on `kernel32.dll` just as uniformly. Do not re-diagnose a darwin failure as a
  flavor-binding failure.
- **Which platform the behavioral transpile targets.** The converter defaults `-platforms` to the
  *host*, so a mac leg re-transpiles the behavioral corpus for `darwin/arm64` (or `darwin/amd64`)
  and compares the result against goldens captured on Windows. Linux has already proven those
  goldens platform-invariant for the behavioral corpus; darwin has not, and whether the converter's
  darwin path is exercised on **arm64** at all is itself untested — the corpus census work used the
  amd64 target.
- **Case sensitivity.** Default APFS is case-**insensitive** like Windows, so the mac legs prove
  nothing about path casing. Linux remains the only filesystem that does.
- **Runner labels and images.** `macos-15-intel` is a comparatively new label; whether its image
  carries the expected `pwsh` and SDK story is something only a dispatch settles.
- **`pwsh` version drift.** The Linux campaign's readings come from one specific pwsh; hosted images
  ship whatever they ship. A PowerShell-level failure on a runner is a portability finding about the
  instrument, not about the corpus.
