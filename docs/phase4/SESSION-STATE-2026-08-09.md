# SESSION STATE — 2026-08-09 crash-save (pickup from any machine)

> Written during a hardware-failure evacuation: the coordinator machine is crashing, so the
> campaign state that lived in the session is recorded here. Everything below is on GitHub.
> **Read this file top to bottom before resuming any lane.**

## Where the campaign stands

**Standing goal №3 (user, 2026-08-09):** autonomous Phase-4 charter+board campaign, ≥3 parallel
lanes, chips rare, until **75% validated = 162 of 215**, then consider a NuGet push.
**Roster on master: 123/215 = 57.2%, 13,976 verdicts, 50 disclosed** (`docs/ValidatedTestPackages.md`
is authoritative — recompute totals from the table, never trust a header claim).

Master (`8721e9b7d` at save time) is green and fully gated: every merge on it passed converter
`go test`, CNR, and the suites its change class owed. The 1.23.1.5 NuGet release (first with Linux
binaries) is live; the `linux-first-run-2026-08-08` milestone is tagged and announced.

## The four in-flight lane branches — state and what remains

Each branch tip is an **UNSIGNED, UNGATED `wip(...)` crash-save commit** holding the worktree
verbatim. Below it sit the lane's real, signed commits. On pickup: create a worktree per branch,
read the wip diff, classify per the standing aftermath families, and finish the lane's list.

1. **`claude/r57b-nearmiss`** (3 signed + wip). Banked `go/ast` 9/9 on-branch; charter was the
   near-miss singles (`net/smtp`, `go/build` cwd row, `internal/testenv`, plus re-measuring
   `encoding/asn1` 28/38 which shares rsa's repaired tag root). It was parked on its FINAL SWEEP
   when the machine died — the sweep verdict is unknown; re-run `-Filter` over its banks. Its
   roster arithmetic is lane-local (base 123-era); union-recompute at merge.
2. **`claude/r57c-zipperf`** (2 signed + wip). THE @string WINDOW FIX — Go's O(1) string slicing
   was O(n)-copying corpus-wide; `@string` now carries backing+offset+length with a private
   backing. `TestZip64LargeDirectory` >45min → 20.2s; `archive/zip` measured 98/98. Gates already
   green on-branch: GolibTests 84/84, corpus 307/0, behavioral 554/554+527/527 (one failure proven
   pre-existing on master). REMAINING: CNR verdict read-back, full sweep, README badge refresh,
   the bank commit, board write-back. Handoffs it flagged: `ByteSeqAllocationTests`' @string bound
   is stale-loose (belongs to r58a's counting); `InterfaceInheritance`/`ValueOf(Type).Pointer()`
   root belongs to r58b's bridge area; its only touch on r58b's file is a comment.
3. **`claude/r58a-alloccount`** (2 signed + wip). USER-RULED: the golib-owned allocation COUNTER
   (mirrors Go's runtime-owned `Mallocs`) so `testing.AllocsPerRun` reports a true count — the CLR
   provably offers none (r56d's measured proof in merge `363e728bb`). Then the harvest: bank
   `crypto/rsa` 560/560 (largest single-row prize; 559/560 now, blocked only on this), then `log`,
   `net/http/internal`, `log/slog/internal/buffer`, then re-measure `nistec` (2,195/2,200 — its
   want-zero rows may become HONEST alloc-profile disclosures once the count is real; the r56d
   bill of 241,077 boxes/run supports it). Progress at crash unknown past its 2 commits — read
   the branch.
4. **`claude/r58b-typednil`** (2 signed + wip). USER-RULED, BOUNDED: the typed-nil narrow start at
   the reflection bridge's `Value.Interface()` ONLY (golib can represent the state via
   `ж<T>.IsNilStandardPointer`). Hard stop if the fix demands converter emission or golib equality
   changes — that full arc is design-with-user. Targets: math/big's 2 gob rows (222→224/226),
   then re-measure `encoding/gob`. Progress past its 2 commits unknown — read the branch.

## Pending user decisions (all with evidence packages in the board/docs)

- **ж-box arc** — nistec's 2,200 + the box alloc bill (byte-exact decomposition in `363e728bb`).
- **init-ORDER arc** — `edwards25519` 0/55 is its first whole-package casualty.
- **GOROOT-tree-reproduction** pipeline question — 4 packages, one design decision.
- Queued lanes: **r59 movable-attribute investigation** (backlog 25, user-commissioned;
  backlog 24's emitted-comment rewrite RIDES its regen bank).

## Operational doctrine reminders for the resuming coordinator

- Lanes run gates INLINE in their active turn; a detached child dies when the lane's turn ends
  (proven ~8 times). Coordinator-owned background waits are fine.
- Sweep aftermath: classify per the standing families (CRLF phantoms by CR-stripped compare; the
  `-tests`-closure production restore family INCLUDING the `initᴛᴛtests` package_init hooks;
  `.cs.auto` siblings). The `time/package_info_internal_test.cs` treadmill is FIXED (rebanked).
- Roster edits: recompute totals from the merged table; never take either side's header.
- Absolute paths for every [System.IO.File] call and roster read (relative reads in a worktree
  silently read the MAIN repo).
- Marker census at save time: **48** line-anchored (44 + r53a's growth); re-measure, never carry.
- The scratchpad is machine-local — lane-prefixed commit-message files, and nothing durable
  belongs there.

## Non-repo state that does NOT transfer (rebuild on the new machine)

- WSL Ubuntu provisioning (Go 1.23.1 at `$HOME/golang`, .NET SDK at `$HOME/.dotnet`, pwsh 7.5.x
  via `dotnet tool install -g powershell` — version-pin lesson in PLAN-linux-operation).
- The `%GOPATH%` deploy root, NuGet caches, scratch feeds (`r52a`/`r54b` evidence feeds), and all
  `C:\go2cs-build\*` worktrees/temp roots — all regenerable from the repo.
- Claude-side memory files are machine-local; this file supersedes them for campaign state.
