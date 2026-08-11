# LANES — multi-machine lane assignments and protocol

> The coordination file for running go2cs campaign lanes across machines. The **coordinator**
> (currently the i7-5820K desktop session) assigns lanes here, merges, signs and lands everything;
> lane machines clone, work a branch, push the branch, and signal. Git is the bus: this file is the
> assignment board, and a lane's branch is its deliverable.

## Protocol

1. **One lane, one branch, one machine.** Take a lane by its section below. Branch from current
   `origin/master` as `claude/<lane-id>-<memorable-suffix>`; never commit to master, never push
   master. Branch pushes are encouraged (they are the crash-save).
2. **Prompts are self-contained.** Each lane section below IS the session prompt — paste it (or
   point the session at this file and name the lane). Paths are written repo-relative; `<clone>`
   means your clone root.
3. **Gates run on the lane machine, inline.** The coordinator re-gates at merge regardless — but a
   lane that arrives with its own gates green merges same-day.
4. **Merge signal.** Finish with a signed (or plain, if no key on that machine) commit whose subject
   starts with the lane id, push the branch, and tell the coordinator session "lane `<branch>`
   complete" (a one-line relay is enough; paste the session's final report if convenient). Do not
   edit this file's STATUS column from a lane — the coordinator owns it at merge time.
5. **Machine notes.** Timeout budgets machine-wide are slow-host-sized as of 2026-08-10 (the
   BehavioralRunner `--build-timeout` family, the sweep's `$longTimeouts` floors, `-TestTimeout`
   raises floors), so a laptop needs no configuration for correctness — only patience. If a run
   reports NOT MEASURED, raise the named budget and re-run; never read a timeout as a corpus
   failure. Standard rules ride CLAUDE.md: no `dotnet build-server shutdown`, no bare-name
   process kills, absolute paths in scripts, PS 5.1 syntax on Windows PowerShell.

## Laptop provisioning (once per machine)

Go **1.23.1 exactly** · .NET 9 SDK · `git clone https://github.com/ritchiecarroll/go2cs` · one
interactive `git push` for the credential-manager browser auth · Claude Code. VS 2022 "Desktop
development with C++" only if the lane runs Native-AOT work (none of the current lanes do).
Optional 10-minute baseline: `./src/tests/Behavioral/run-behavioral.ps1 --filter Atomic` and one
`./src/run-validated-sweep.ps1 -Filter 'container/heap'` — report the times with your first lane
so the coordinator can calibrate assignments.

## Assignments

| Lane | Machine | Status | Merge window |
|---|---|---|---|
| L1 host-conditional roster | laptop-1 | OPEN | anytime (harness-only) |
| L2 allowlist derivation | laptop-1 (after L1 — same file) | OPEN | anytime (harness-only) |
| L3 ж-box implementation | laptop-1 | BLOCKED — post-1.23.1.6 harvest only (design **SIGNED OFF** 2026-08-10, doc landed on master) | post-harvest |
| L4 init-order tuple-spec fix (Option A) | laptop-1 (parallel with L1/L2 — disjoint files; use a second clone or worktree if truly concurrent) | OPEN to develop | **post-1.23.1.6** (converter change; the release ships from the current gated tree) |

⚠ Two lanes running concurrently on one machine need **separate checkouts** (second clone or
`git worktree`) — CNR/behavioral gates re-transpile the tree they run in, and two lanes sharing
one checkout will trample each other's state even when their diffs are disjoint.

---

## L1 — Teach the roster host-conditional verdict counts

SMALL HARNESS MECHANISM. Work in a branch from current origin/master in your clone. Read
`<clone>/CLAUDE.md` first, then `docs/phase4/BOARD-next-validation-candidates.md` — search
"path/filepath" and the 2026-08-10 coordinator-ratifications section.

The problem: `path/filepath` is banked at 61 verdicts, but on a host with symlink privilege six
additional Windows symlink tests go skip->pass on BOTH runtimes (still agreeing verdict-for-verdict),
so the sweep reports "67 vs banked 61" count drift — a false red; banking 67 would false-red every
unprivileged host instead. The class recurs (elevation, network access, environment capabilities).

Design goal: the roster row (`docs/ValidatedTestPackages.md`, machine-parsed by
`src/run-validated-sweep.ps1` — its row regex is documented in the file's comment) gains a way to
express "N + up to M host-conditional verdicts, named", and the sweep accepts either count ONLY when
the delta consists exactly of the named conditional tests with matching verdicts on both sides.
Preserve the parser's strict column contract (the format comment warns reflowing breaks it) — prefer
an annotation that degrades gracefully, e.g. a suffix in an existing cell rather than a new column.
A mismatch OUTSIDE the named set must still fail loudly.

Gates: a filtered sweep of path/filepath must pass in BOTH privilege states (test the state your
session has; verify the other by construction and say so honestly); a filtered sweep of 2-3
unconditional packages must behave byte-identically; the path/filepath roster row updated with its
six named conditional tests; the format comment and CLAUDE.md's sweep row updated if the row shape
changes. Commit on your branch (subject starts "L1:"), push, signal per the protocol.

## L2 — Derive the sweep's closure-class allowlist structurally

SMALL HARNESS IMPROVEMENT. Branch from current origin/master. Read `<clone>/CLAUDE.md`, then
`src/run-validated-sweep.ps1`'s post-sweep drift classification — the hand-maintained allowlist of
`package_init.cs` files carrying the initᴛᴛtests hook (its own comment says it is "OWED BY EVERY
BANK"), and `git show 165c67ee5` (the r57b recovery: a missing syscall row false-redded every full
sweep, and layout L3's per-GOOS paths broke the flat-path shape bankers pattern-match).

Remedy: DERIVE the allowlist from the corpus — the hook's presence is detectable from file content
(the classification already content-checks candidates), so enumerate candidates structurally (any
`<pkg>/package_init.cs` or `<pkg>/<goos>/package_init.cs` whose diff-vs-HEAD is exactly the
documented hook shape) instead of consulting a name list. The content check is what keeps this safe:
a REAL change to a `package_init.cs` must still classify as drift, never be absorbed. Prove that
with a negative test (mutate a copy's hook line; the classification must flag it).

Gates: a filtered sweep over 3-4 banked packages including at least one L3 package (`syscall` is
one) and one hook-carrying package, classification output matching today's; the negative test; a
PS 5.1 parse check. Update the script's comment block to describe derivation instead of
maintenance. Commit on your branch (subject starts "L2:"), push, signal.

## L4 — init-order tuple-spec fix (Option A, ratified)

CONVERTER FIX, fully specified by [`FINDING-init-order-tuple-specs.md`](FINDING-init-order-tuple-specs.md)
(read it first, end to end — it carries the root cause, the census with positive controls, the
hand-simulated 0/55 → 52/55 measurement, and the reproduction commands). Branch from current
origin/master.

The work: extend the EXISTING init-order relocation (`src/go2cs/initOrderOperations.go`, landed
`e39855770`) to package-level tuple var specs — the refusal sits at `visitValueSpec.go:1158`.
Reuse `packageInitMethodName`/`recordMovedInitMethod`/`writePackageInitFile` unchanged; Go's
`InitOrder` yields one entry per spec, so ordinals need no new bookkeeping. Cover BOTH emission
sub-shapes the census found (edwards25519's deconstructing form AND the darwin `os`
`initCwd`/`initCwdErr` hoisted form — a fix that misses the second is half a fix). Remove the
falsified "no stdlib occurrence" comment and the warning it guards.

Gates: converter `go test ./...` (add a unit guard beside the existing init-order tests); a NEW
behavioral test exercising a tuple-spec package var whose initializer depends on a later-declared
var (per CLAUDE.md's regression-test steps — goldens, slnx registration, integrity check); CNR —
expect movement ONLY in that new test plus any behavioral package with tuple package-vars
(justify each; re-baseline via UpdateTestTargets after re-transpiling); the edwards25519 pipeline
re-measure (`-tests -test-action all -test-timeout 30m`) expecting **52 of 55** with the three
residuals matching the FINDING's attribution; a darwin single-package census
(`-comments -platforms darwin/amd64` over GOROOT's `os`) showing the refusal warning GONE.
Corpus impact: exactly the two edwards25519 files plus their package_init.cs on Windows — a
seeded single-package reconvert proves it; do NOT run a whole-corpus regen (r59 owns the next
one). Commit on your branch (subjects start "L4:"), push, signal. **Merges only after 1.23.1.6
ships** — develop freely, the branch waits.

## L3 — ж-box allocation-reduction implementation

**BLOCKED until the coordinator confirms the post-1.23.1.6 harvest is complete** (the design is
signed off — all six §10 rulings ratified 2026-08-10 and the doc is on master). When unblocked:
branch from current origin/master, read `docs/phase4/DESIGN-zh-box-reduction.md` (§9's staging
table is the work order — **start at stage A1**, the zero-emission census whose report confirms
the projection branch and classifies the 347 exported candidates before any golden moves), then
`<clone>/CLAUDE.md`'s corpus mechanics and the charter's §5 gate table —
this is golib-wide, so the full behavioral suite, GolibTests, the go2cs.slnx build, the corpus
build, and the validated sweep all apply, plus the allocation-counter instrument
(`src/tests/GolibTests/AllocationCounterTests.cs`) measuring each stage against the design's named
workloads. Nothing lands from the lane: the coordinator re-gates and merges. Commit per stage on
your branch (subjects start "L3:"), push often, signal per stage.
