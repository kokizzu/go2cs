# CENSUS — G-LAPTOP preservation, 2026-09-13

**Record, not a runbook.** Point-in-time measurement; amend with dated blocks, never rewrite.
Measured at `origin/master` = `bd1d26faffe1dd063fda91399ec9a2b35910fd8c` (the KICKOFF file's own landing),
after `git fetch --no-prune origin` in every clone. Section 1 of `docs/phase4/KICKOFF-fleet.md`.

Nothing was deleted, pruned, moved, pushed or fast-forwarded. `git worktree prune` was deliberately NOT
run despite this being the owning machine: the census must not change what it measures.

## 0. Topology — the record's "clones" are mostly worktrees

TWO real clones on Windows, not four. `go2cs-g1` and `go2cs-gq` are **linked worktrees** of the one clone
(`.git` is a file, `git rev-parse --git-common-dir` resolves to the main clone's `.git`), so they share a
single object store. Losing that store takes the branch and every checkout at once.

| Root | Kind | Note |
|---|---|---|
| `Projects/go2cs` | real clone | 15 registered worktrees, 194 local branches |
| `Projects/g2-mailbox` | real clone, SHALLOW | `claude/mailbox` only; graft `2427a66b7`; `origin/master` absent |
| `Projects/go2cs-g1`, `Projects/go2cs-gq` | worktrees of the above | not independent copies |
| WSL `~/go2cs-lin` | real clone | **`origin` is the Windows clone, not GitHub** |

The WSL origin caveat is load-bearing: "not on origin" there means "not in the Windows clone", which is a
*stronger* risk statement, while a commit *present* in that origin may still be absent from GitHub. Both
readings were resolved by testing every SHA against the real GitHub remote inside the Windows clone.

## 1a. Counts

| Measure | Main clone | Mailbox clone | WSL clone |
|---|---|---|---|
| `git stash list` | 0 | 0 | 0 |
| local branches | 194 | 1 | 2 |
| local tags | 22 (all on origin) | 22 (identical set) | 22 (identical) |
| commits not on any origin ref | **33**, over **17** branches | 0 | 0 |
| working tree | clean | clean | clean |
| deleted-tracked (`^ D`) | **0 in all 15 worktrees** | 0 | 0 |
| `fsck --no-reflogs --unreachable` commits | 294 | 112 | 4,295 |

`refs/preserve` is absent here (R-LAPTOP holds those copies; `refs/preserve` is also absent from the
GitHub remote's full 147-ref `ls-remote`, consistent with the record's "never pushed").

The deleted-tracked zero was negative-controlled against a synthetic `' D src/foo.go'` line, so it is a
real zero rather than a fail-open — safety-floor assertion #8 holds across every tree.

## 1b. THE FINDING — the 11-branch list is incomplete; there are 17

All eleven G-only branches the 2026-09-12 remote scan names are present **at exactly the SHA recorded**.
Zero moved, zero vanished. But the scan missed six more branches carrying commits on no origin ref:

| Branch | local-only | tip | dated | content |
|---|---|---|---|---|
| `claude/g-typed-nil-func-parked` | 5 | `477869d5c` | 2026-09-01 | typed-nil func boundary arm; parked, not gated |
| `g-mapiter-complete` | 3 | `468d92bb4` | 2026-08-29 | reflect MapIter hand-own, both directions; R1/R2/R3 |
| `claude/g-typed-nil-func-sizing` | 1 | `f4065f27b` | 2026-09-01 | the wired slots only (also in `-parked`) |
| `claude/g-wsasendto-seat` | 1 | `52c01fbb9` | 2026-09-05 | syscall/windows WSASendto hand-own, 19 files |
| `claude/scout-correction` | 1 | `eb056c4f1` | 2026-08-22 | board: corrects the .NET 10 scout's silicon |
| `g-funcforpc` | 1 | `234db8642` | 2026-08-29 | merge commit; `90dc5d59f` itself reached origin |

All six confirmed local-only by `git branch -r --contains <tip>` returning empty. The per-branch counts sum
to 34 against a union of 33; the overlap is exactly `f4065f27b`, shared by the two typed-nil branches —
that arithmetic reconciles and is the internal consistency check on the census.

**Refuted:** `claude/l10-sockaddr-blittable-seam` (`aa846dc5a`) and `claude/g-a2-compile-order`
(`289b53a16`) are each 0 ahead of origin — checked-out but fully published. They are *not* unrecorded work.

## 1c. Dropped stashes survive in the object store

`git stash list` reports 0 in every clone, yet the main clone's unreachable set contains **30 dropped
stashes** (30 `WIP on`/`On <branch>:` tips plus their 30 `index on` parents), dated 2026-08-12 → 2026-09-08.
All 294 unreachable commits were resolved against the 10,956 origin-reachable commits: none is on origin.
They are reflog-held only (`gc.auto`, `gc.pruneExpire` and `gc.reflogExpire` are all unset, so stock
defaults apply) and a `git gc` would take them. A "clean tree" verdict hides exactly this class.

The mailbox clone's 112 unreachable commits resolve to 21 not on origin: 20 are `MAILBOX.md` post drafts
and one is a 6-byte gpg probe. Tested at TEXT level rather than SHA (the branch is rebuilt under push
contention), 17 of the 20 are superseded re-commits whose every added line is already published. **Three
carry wording absent from origin** — `0113da54e4` (9 of 18 lines), `3dac3a3924` (4 of 6), `2394682299`
(1 of 24) — including one dropped paragraph, the "hand C2 the shape rather than the commit" offer from the
2026-09-08 post. No whole post is unpublished; the loss would be earlier draft wording only.

## 1d. Seats

**Seat 8 — `claude/g-generic-alias-qualifier` `ffaafeb192eef897f00a4001ef97d1c749a39347`. AT RISK.**
Every recorded claim confirmed: base `8a1b7e71c`, two commits (`4772d4907` converter + `ffaafeb19` guard
and golden), 8 files, +91/−3 (numstat sums exactly), tree clean, not on GitHub by both `branch -r
--contains` and `ls-remote`. On this machine it exists in two object databases — the Windows store and the
WSL clone's mirror of it — but that is a same-machine mirror, not an off-machine hedge. No tag or backup
branch preserves it; `g-seat-preorder-backup`, listed beside it in the record, does **not** contain it.

Two corrections to the re-cut instruction:

- **The instruction's second site `:449-464` is not a change site.** Seat 8 touches
  `src/go2cs/typeNameResolution.go` in exactly ONE hunk (`@@ -419,7 +419,32 @@`, master lines 423-425).
  `:449-464` is the pre-existing NON-generic arm that already carries the identical alias rule — it is the
  *donor* pattern. Reading it as two edits would double-apply the rule.
- **The whole-file-checkout hazard is real but not a line overlap.** `1800b04f8` is the only master commit
  touching that file after the base; its one hunk is `@@ -1,8 +1,10 @@`, the MIT → AGPL-3.0-only header
  swap. Line ranges do not overlap (1-10 vs 419-425), but the blobs collide: the seat's blob still carries
  the MIT header. A whole-file checkout would silently revert the license change; the prescribed
  `cherry-pick -x` would not. The instruction's choice of cherry-pick is correct for a reason the
  instruction does not state.

**Seat 6 — `claude/g-unfreeze-handown-metadata` `7078dbada`. SAFE — already on GitHub** at the identical
SHA. Ahead/behind vs the current master is 5/49 (the record's 48 became 49 when master advanced).
**Hunk count refuted: 38 at `-U0`, not the recorded 2**, spread over all 17 files (+487/−33 vs merge-base
`44f858717`); the 7 `src/go2cs/` files carry 8 of them. The record's "re-measure" note was warranted.

## 1e. Off-git — where the real exposure is

Inventoried 13.74 GB. Free on C: is 376 GB, ~15× the 25 GB sweep floor, so an archive is not disk-constrained.

**The record's at-risk list is materially incomplete, and its named payloads are small.** The bulk of the
Temp `g-*` roots (9.36 GB) is checked-out worktrees and paired 19 MB converter binaries — tracked content,
not at risk. The genuinely unique parts are small: `g-cens` 13,859 B, `g-parse` 2.9 MB, `g-i1-probe` 5.4 MB,
`g-repro-221225/repro/` **644 B**, and the `conv-*.log` / `diff-*.txt` / `run.log` files at ≤56 KB each.
Several roots the record marks regenerable are already hollow (`g-d3`, `g-d3stage`, `g-e1`, `g-e1stage`,
`g-nilconv{,2,3}`, `g-backup` hold zero files; `g-master-conv` and `g-pre-conv` hold one `go2cs.exe` each).

Three gaps the record does not cover:

1. **The H6 scripts are not where the record implies.** The prescribed depth-4 find prints nothing; they
   live at depth 7 inside one Claude **session scratchpad** — which holds **94 top-level hand-written
   `.sh`/`.ps1` instruments**, of which the record names three. This is the densest concentration of
   non-regenerable hand-written work on the box, and a session scratchpad is exactly the kind of directory
   that gets reaped.
2. **141 loose Temp `g-*` files exist and the record names none** — including four more hand-written
   instruments (`g-sp.sh`, `g-h9-ctl.sh`, `g-guard-fixed.ps1`, `g-guard-fixed2.ps1`) and substantive text
   (three ~1.9 MB `g-board-*.md`, two 151,700 B `g-roster*.md`, `g-mem-backup.md` at 322,281 B).
3. **`g-dctrl` (67 MB, 145 files) and `g-dmarker` (54 MB, 269 files)** appear on neither the at-risk nor
   the regenerable list and hold real `docs/` + `src/` content.

Four further session scratchpads totalling ~5.8 GB appear in no fleet record; they read as probe/stage
output rather than instruments.

**Seat 8's own footprint evidence is unversioned and at risk.** `g-gqdiff-20260908-225250` holds the
two-seeded measurement — six `conv-{base,cut}-{windows,linux,darwin}.log`, `run.log`, `snap/`, six
conversion roots, and `diff-windows.txt` / `diff-linux.txt` / `diff-darwin.txt` **all zero bytes**, which
*are* the "footprint ZERO ×3" result the re-cut instruction requires re-scoring against. None of it is in
any object database.

## 1f. WSL — measured for the first time; git store fully redundant

The decisive test: every one of the **232,163 objects** in the WSL store (133,360 blobs, 87,965 trees,
10,818 commits, 20 tags) was tested for existence in the Windows clone. **Zero missing.** Not one object —
reachable, unreachable or dangling — exists only in WSL. All three ref tips are on GitHub. HEAD is detached
at `44f858717`; zero stashes; working tree clean (5,985 status lines, all `!!`).

**The WSL risk is entirely off-git: nine files from 2026-09-08, ~12.6 MB, that exist nowhere else.** Their
git blob hashes are absent from the Windows object store and no copies exist on the Windows filesystem.
They are unbacked *by design* — `src/core/.gitignore:19-20` ignore the comparison/results JSON and `:44`
hides `lin.trx`. A deliberate safety copy at `~/g-preserved/` is byte-identical (`cmp` rc=0) but sits on the
same disk: it protects against a sweep, not against loss of the machine.

Partial mitigation: the run's **conclusions** did reach GitHub. `origin/claude/mailbox` `3a8f3eca3` banks
the headline — linux runtime at `44f858717`, goroutine-panic death at `TestLockOSThreadNesting`,
873 go / 181 C# / 132 agreeing / 49 diverging / 692 unreached. What would be lost is the raw per-test
evidence — per-test verdicts, manifest, XML/TRX, and the 1.19 MB conversion log — not the banked numbers.

## 1g. Worktrees

All 15 `rev-parse --show-toplevel` to exactly their registered path. Exactly one tree has any non-ignored
working-tree content: `go2cs-g1`, with 98 porcelain entries / 102 files — **not the recorded 58**. All 102
are `-tests` pipeline output (converted C# test harness under `src/core/reflect/` and `src/core/runtime/`,
two `.tests.csproj`, one `lookup_windows.cs.auto`, 5 under `runtime/pprof/testdata/`); none is
`bin`/`obj`/`dll`/`exe`/`pdb` build output, and all are regenerable, but they are on no remote.

The nine Temp diff-arm worktrees — **nine, not the recorded 8** — are pristine, positive-controlled rather
than trusted: each holds 14,043-14,085 tracked files and exactly tracked+1 on disk (the `+1` is the `.git`
link), so they are untouched checkouts rather than empty or broken directories.

## Corrections owed to the record

| Record claim | Measured |
|---|---|
| 11 G-only branches | **17** branches carry local-only commits (the 11 + six unlisted) |
| `go2cs-g1`: 58 untracked | 98 entries / **102** files, all regenerable pipeline output |
| 8 diff-arm worktrees | **9** registered under Temp |
| seat 6: 2 hunks | **38** at `-U0` |
| seat 6: 5/48 | 5/**49** (master advanced) |
| seat 8 sites `:423-425` and `:449-464` | one hunk at `:423-425`; `:449-464` is the donor, not a site |
| H6 scripts (location unstated) | depth 7 in a session scratchpad holding **94** instruments |
| WSL not measured | measured: git store **fully redundant**; 9 off-git files unique |

## DEFECT — leg 1a's prescribed control cannot fire on this box

⚠ **KICKOFF section 1b, leg 1a specifies a control that reads GREEN on G-LAPTOP while proving nothing.**
It says: *"append ONE line holding a profile path taken from the environment, never typed (in Git Bash,
`printf '%s\n' "$HOME" >> rescue-dump.txt`); re-run, and it must FAIL naming rescue-dump.txt."*

Run here, that control **passes**. `$HOME` in Git Bash is the MSYS spelling of the profile root, whose
account segment on this box is `admin` — and `admin` (with `administrator`, `user`, `runner`, `agent`,
`ubuntu`, `vagrant`, `ci`, `build`, `dev`, `root`, …) is a deliberate member of
`fleetPlaceholderSegments` in `src/go2cs/internal/repoguard/fleetIdentifierCensus_test.go:134`. The
clearance is correct and documented — a generic account name is indistinguishable from a redaction
placeholder, and Go's own suites are full of such paths. The control is what is wrong.

**The guard itself is healthy.** Probed with a FOREIGN segment (per the exactly-one-arm rule), the
profile arm is live in all three spellings, and the bare foreign token alone fires nothing — so each
refusal is attributable to the profile arm and not to a second arm catching the probe:

| probe | guard rc |
|---|---|
| foreign token alone (`zqxwvutsrp`) | 0 — precondition holds, no other arm matches it |
| `C:\Users\<foreign>\x` | **1 — refused** |
| `C:/Users/<foreign>/x` | **1 — refused** |
| `/c/Users/<foreign>/x` (MSYS) | **1 — refused** |
| `C:\Users\<local account>\x` | 0 — segment cleared as a placeholder |
| `/home/<foreign>/x` | **1 — refused** |

File restored byte-identical after every probe (sha256 `a80d8f67…` before and after all nine plants).

**Consequence, and it is not local to this box.** Any lane whose account name falls in that placeholder
set runs leg 1a's control, sees green, and concludes its identifier gate is armed — when the control
fired no arm at all. This is safety-floor #13 one layer up: not a gate never made to fail, but a
*control specified so that it cannot fail*. **Proposed runbook amendment:** leg 1a's control plants a
FOREIGN account segment in a profile path, never `$HOME`, and asserts the bare token alone is clean
first. COORD rules; this record does not amend the runbook.

## Method

Six dimensions censused in parallel, each independently re-verified by a second reader that re-ran the
load-bearing commands and was instructed to refute rather than confirm. Two census claims were refuted by
their verifier and are excluded from this record: a "detached HEADs off GitHub" quotation that does not
appear in the KICKOFF file at all, and a flat "only on this machine" for seat 8 that had not measured the
WSL mirror. The branch dimension was additionally measured by hand, independently, and reproduced: 33
local-only commits, 17 branches, 194 branches, 22 tags all on origin, 294 unreachable commits.
