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

| Lane | Machine | Status |
|---|---|---|
| L1 host-conditional roster | laptop-1 | OPEN |
| L2 allowlist derivation | laptop-1 (after L1) | OPEN |
| L3 ж-box implementation | laptop-1 | BLOCKED — design sign-off + post-1.23.1.6 harvest first |

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

## L3 — ж-box allocation-reduction implementation

**BLOCKED until (a) the design chip's document lands with user sign-off and (b) the coordinator
confirms the post-1.23.1.6 harvest is complete.** When unblocked: branch from current
origin/master, read the signed-off `docs/phase4/DESIGN-zh-box-reduction.md` (its staged landing
plan is the work order), `<clone>/CLAUDE.md`'s corpus mechanics, and the charter's §5 gate table —
this is golib-wide, so the full behavioral suite, GolibTests, the go2cs.slnx build, the corpus
build, and the validated sweep all apply, plus the allocation-counter instrument
(`src/tests/GolibTests/AllocationCounterTests.cs`) measuring each stage against the design's named
workloads. Nothing lands from the lane: the coordinator re-gates and merges. Commit per stage on
your branch (subjects start "L3:"), push often, signal per stage.
