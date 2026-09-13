# KICKOFF -- go2cs fleet

## 0. Header and maintenance

**As of 2026-09-13, master `ddd509c1e`.** Current and rewritable; never evidence. Detail:
`docs/phase4/CENSUS-preservation-2026-09-12.md`. Until both land, the owner pastes this file plus one block.
- Log: `claude/coord-handover` `9e20af0ad`, by `git show` only. Its "GOTOOLCHAIN=local" clause (lines 36, 142)
  is wrong for battery shells.
- R-LAPTOP holds the drafting session's drafts and census directory off-git at `C:/go2cs-tmp/handover-2026-09-12/`
  (copied after 1e's counts; `MANIFEST.sha256` gives SHA-256 and line counts); the census directory holds pre-scrub
  text: never committed, posted or attached. Nicknames only.
- PROPOSED: land beside a "kickoff" amendment to `docs/Glossary.md:307` (records are only the RECON-,
  REHEARSAL-, CENSUS-, DATA-, STAGE0- files); rewrite section 4 after verified landings; merge master into
  `claude/coord-handover` before any log append.

## 1. Preservation first

**Every lane's first action: run this section on its own machine and report to COORD.** Delete nothing.

### 1a. Census, per clone (main, WSL, mailbox, probe)
Unfiltered: `git fetch --no-prune origin`; assert `git rev-parse --show-toplevel` is the clone; `git stash
list`; `git log --branches --tags --not --remotes=origin --oneline` (prints COMMITS; R-LAPTOP control: 24 lines, 19 on
branches and 5 tag-only, both tags on origin, so check tags with `git ls-remote origin refs/tags/<tag>`); per branch
`for b in $(git for-each-ref refs/heads --format='%(refname:short)'); do [ $(git rev-list --count $b --not
--remotes=origin) -gt 0 ] && echo $b; done` (R-LAPTOP control: 12 branches); `git fsck
--no-reflogs --unreachable`. Per worktree: assert toplevel; `git log --oneline HEAD --not --remotes=origin`;
`git status --porcelain --ignored=matching`. List non-git roots. WSL: set `user.email` first. R-LAPTOP: `git
for-each-ref refs/preserve`. Post counts. `git worktree prune` only on the owning machine: remotely, live
worktrees read "prunable" (G-LAPTOP 14 of 14); remote reads use `--git-dir`/`--work-tree`/`--no-optional-locks`.

### 1b. Rescue a local-only commit (SHARED text; step 6 amended against silent subtraction)
RESCUE A LOCAL-ONLY COMMIT. Nothing runs from the old base, so this works on every base, including bases older than src/safe-push.sh (added 8f849c952, 2026-09-06) or older than src/go2cs/internal/repoguard (e2f9b118f, 2026-09-12). Owner authorization 2026-09-12: lane sessions may commit UNSIGNED on lane branches with `git -c commit.gpgsign=false commit`. Run each step as its own command; capture its exit code before any pipe; its exit gates the next step; never chain a census to a push.

0. SETUP, in the clone that holds the ref. Run `git fetch --no-prune origin`; --no-prune overrides fetch.prune and remote.origin.prune. Check `git rev-parse --verify <ref>^{commit}`. Create a throwaway worktree in a SIBLING directory outside every clone and worktree: `git worktree add --detach <dir> origin/master`. cd into it; assert `git rev-parse --show-toplevel` equals it and `git merge-base --is-ancestor 9355669f8e6d461306837135688cea9fad68f8aa HEAD` succeeds. Steps 1-7 run HERE, where master's safe-push.sh and repoguard always exist.
1. POPULATION. MB=$(git merge-base origin/master <ref>); N=$(git rev-list --count $MB..<ref>). Stop unless N > 0. Print `git log --oneline $MB..<ref>`.
2. DUMP OF THE BRANCH'S OWN RANGE. Write an untracked file at the worktree root, e.g. rescue-dump.txt, containing: the ref name; `git log --no-color --format='commit %H%n%B' -p $MB..<ref>` (every message and every per-commit patch, so a token added and later removed inside the range is still seen); and `git diff --no-color $MB <ref>`. Assert its line count is nonzero.
3. LEG 1a, REPOSITORY GUARD. Run `git add -f rescue-dump.txt` (index only, never committed). Use a shell whose GOROOT is the go1.24.13 sdk as `go env GOROOT` prints it, with its bin first on PATH and GOTOOLCHAIN=local for this call; the converter module refuses older toolchains (measured: go.mod requires go >= 1.24.13). A session that cannot produce a go1.24.13 toolchain (probe `GOTOOLCHAIN=go1.24.13 go version`, never PATH alone) pushes no lane branch or tag, still runs legs 1b and 2, posts its question and tip SHA to COORD on the mailbox, and makes no further commits until COORD rules (its container kept up until then). Run `cd src/go2cs && go test -count=1 -v -run 'TestNoFleetIdentifiersInTrackedFiles|TestFleetIdentifierClearancesAreLive' ./internal/repoguard > <log> 2>&1; rc=$?`. PASS only if rc=0 AND the log contains '--- PASS: TestNoFleetIdentifiersInTrackedFiles' AND contains neither '(cached)' nor '[no tests to run]'. CONTROL: record the dump's sha256; append ONE line holding a profile path whose ACCOUNT SEGMENT IS FOREIGN -- never the environment's own and never a member of the guard's placeholder set -- after first asserting that the bare foreign token alone reads clean (measured 2026-09-13 on G-LAPTOP: an environment whose account segment is a placeholder-set member passes the old control vacuously); re-run, and the guard must FAIL naming rescue-dump.txt; delete that line, re-run, and it must PASS with the sha256 back to the recorded value.
4. LEG 1b, DERIVED-TOKEN PASS. This leg is REQUIRED: the repoguard denylist does not carry every identifier the 2026-09-01 scrub removed (measured 2026-09-12: the guard PASSES at 9355669f8 while two master lines carry one). Tokens are the words that scrub commits a7595da67 (master) and d72878d6d (mailbox) removed at least 3 times and never added (3 tokens, measured). Hold them in a shell variable or a temp file outside every clone; never put them in a worktree, commit, post or document. Lowercase both the tokens and the dump with `tr 'A-Z' 'a-z'` and match with `grep -F -f`. NEVER use `grep -i` in Git Bash: measured 2026-09-12, `grep -i -c -F` printed nothing, a fail-open zero. Print only the hit count and dump line numbers. PASS only on 0. CONTROLS, measured values: the added lines of `git diff <merge-base> 6815eba00` -- the PRE-FIX tip of claude/g-b1-box-design, PINNED: its current tip f632a942b (2026-09-13) removed those very lines, so a control read at the moving tip reads 0 and cannot fire -- read 2; origin/claude/laneR-h5-lastrung reads 0; one planted token line reads 1. Any hit refuses until COORD reads it in a terminal, never on a pushed surface; the shortest token also occurs in ordinary master content.
5. LEG 2, IDENTITY. ALLOW=$(git log --format='%an <%ae>%n%cn <%ce>' origin/master | sort -u | grep -v -e '^root ' -e '<root@' -e 'localdomain>'). Every line of `git log --format='%an <%ae>%n%cn <%ce>' $MB..<ref> | sort -u` must match a whole ALLOW line (`grep -qxF`). Never require equality with the master tip's identity: lane commits legitimately carry 'Claude <noreply@anthropic.com>' (C1), 'i9 <i9@local>' and the C2 lane identities. The filter is needed because master's own history carries one auto-derived root@<HOST> identity (056b2b06c). CONTROLS, measured 2026-09-12: 95bf02ad5 REFUSED, 056b2b06c REFUSED; c5fb9e0ed, 44ab61dad and 31668f43e ADMITTED. A refused identity is never pushed or re-authored in place; its files are re-cut in step 6.
6. CUT (the default). Unstage and delete the dump (`git rm --cached -q rescue-dump.txt`, then remove the file). Run `git switch -c <new lane branch>`; HEAD is origin/master. Bring over ONLY the non-regenerable files, by explicit path: `git checkout <ref> -- <path>` only where `git diff --quiet $MB origin/master -- <path>` succeeds; otherwise write `git diff --no-color --no-ext-diff --binary --src-prefix=a/ --dst-prefix=b/ $MB <ref> -- <path>` to a patch file OUTSIDE the worktree, assert exit 0 and a non-empty file, then `git apply -3 <patch>` (or `git -c commit.gpgsign=false cherry-pick -x $MB..<ref>`, which commits and names the original SHA itself: skip the commit below and take <paths> from `git diff --name-only $MB <ref>`), since a whole-file checkout silently reverts master (1800b04f8 changed typeNameResolution.go after 8a1b7e71c). Before the commit, assert `git diff --cached origin/master -- <paths>` adds and removes the same lines as `git diff $MB <ref> -- <paths>`; it reads the index, which holds a checked-out or `apply -3` import and equals HEAD after a cherry-pick (`git diff origin/master HEAD` compares commits only and stays empty until the commit). A committed lane branch is rebased instead where COORD rules; stage by explicit path, never `git add -A`. Commit with `git -c commit.gpgsign=false commit -F <msgfile>`, naming the original SHA in the message. Repeat steps 2-5 over origin/master..HEAD. Announce the new 40-character SHA on the mailbox. Then, in the same toolchain shell, run `src/safe-push.sh --branch <new> --new --announced <sha>`; its tree gate now covers the rescued content. ONLY ON COORD's RULING (an already-posted SHA that must stay resolvable) push the original ref from this worktree instead, with `src/safe-push.sh --branch <name> --ref <ref> --new --announced <40-char sha>`; its tree gate then scans master, and legs 1a, 1b and 2 are what gated the range.
7. AFTER. `git ls-remote origin refs/heads/<branch>` must equal the pushed SHA. `git status --porcelain | grep '^ D'` must be empty. Remove the throwaway worktree only after both checks. Never force-push; never rewrite a posted SHA. Never push 239f61940, claude/hopa-sweep-r or rescue/joint-measure-45.

### 1c. Do not push
R-LAPTOP security: `claude/mailbox` `239f61940`, `claude/hopa-sweep-r`, `rescue/joint-measure-45`. R-LAPTOP
stale: `claude/laneR-win-signal-exec-arc` (reverts landed `83ea02659`), `claude/f1-flavor-fix`,
`claude/laneR-promotion-pathscope` `23dc6e931`, `claude/laneR-typearg-cache`, `claude/stage2-tfm-prep`,
`laneR-probe-getoradd-closure`, `r-pprof-measure-throwaway`, `r-union`, `claude/reflect-cargo-r1-measure`
(tag `0dfc95e21` first). i7 `3a4f83aa7`. G-LAPTOP `5f0b75f86` (inside `g-seat-preorder-backup`).
**G-LAPTOP pre-scrub, never pushed without an OWN-range census (1b legs 1a, 1b, 2):** `g-regress` `fcd218e27`,
`g-ivt-ab` `b46aae8ea`, `g-ivt-probe` `40a2af690`, `claude/exec-wall-impl` `2147c9daf`,
`claude/gifted-einstein-a338d2` `c4ae4e3c6`. **R-LAPTOP `refs/preserve/g-laptop/*`** (11 verified copies of the
G-only branches; committed work only; not a publication): never pushed (five pre-scrub); delete a copy only after
its branch (seat 8: its re-cut) is on GitHub or COORD retires it.

### 1d. Never prune (origin)
`master`, `claude/mailbox`, `claude/coord-handover`, `release/go1.23`, the train-47 seats,
`claude/laneR-waitreason-47`, `claude/c1-h6-rewrites`, `claude/g-weak-rekey`, `claude/g-l3-testalias`,
`claude/laneR-prepin-baselines`, `claude/g-pprof-baseline`, `claude/g-hop-b-provisioning`,
`claude/g-b1-box-design`, `g-nilfunc-boxing`, `claude/c2-elemindex-probe`, `claude/c2-getaddrinfo-probe`,
`claude/coord-train30-head`, `claude/reflect-cargo-inc1`, `claude/reflect-cargo-r1`. Landed heads (e.g.
`claude/context-diet`) prune on COORD's word. **Local, G-LAPTOP:** no G-only branch (1e) is deleted until G
says whether it is wanted and COORD rules.

### 1e. Off GitHub, at risk (CENSUS record, section 5)
- G-LAPTOP (read-only share scan from R-LAPTOP): 11 G-only branches: `claude/g-generic-alias-qualifier`
  `ffaafeb19` (seat 8), `g-seat-preorder-backup` `a580978fd`, `claude/g-seg3-spike` `7050417a5`, `g-tmp-mergecheck`
  `b2b34ed2e`, `claude/g-mathbits-intrinsics` `8d28c52c8`, `claude/g-structof-embedded-methods` `e57fe22c7`, and
  1c's five. User Temp, archive before any cleanup: `g-repro-221225/repro/`, `g-gqdiff-20260908-225250`
  (conv-*.log, snap/), `g-parse/`, `g-cens/`, `g-lncensus/`, `g-lncensus2/`, `g-lncensus-a/`, `g-i1-probe/`
  (regenerable: `g-runA`, `g-runB`, `g-bfoot`, seeded `g-d*`/`g-e*`/`g-*-conv` roots). `go2cs-g1`: 58 untracked
  files unlisted; 8 diff-arm worktrees unmeasured. H6 scripts `g-h6census.sh`, `g-nstogo.sh`, `g-movedto.sh`.
  **WSL NOT MEASURED, not backed up** (09-08 linux runtime records).
- i7: `.claude/coord-scripts/`; coordinator memory (accumulator 1340-1349); run-8 stdout; `cfd71b0ba`;
  worktrees incl. `sub-orphan-check`; both mailbox clones.
- R-LAPTOP: `C:/go2cs-tmp` (70 loose files, 32 non-git directories); `r-h5b-convert.sh`; two DECISION
  worktrees; WSL clones.
- i9: 21 unpushed commits per i9 `63ec48417`. C1/C2: 0 unpushed at stand-down.

## 2. Roster (owner-approved 2026-09-12; medium only for mechanical legs)

|Session|Host|Model, effort|Status|
|---|---|---|---|
|COORD|i7|Fable 5.1, ultracode (only Fable)|offline since 09-08|
|R|R-LAPTOP, WSL|Opus 5, high|last lane post `a27342d03`|
|G|G-LAPTOP, WSL|Opus 5, high|cut complete at `ffaafeb19` 09-08 22:39, G-only, unannounced|
|C1|cloud; cannot build .NET (no dotnet), but both Go pins resolve: the converter builds, converts and its suites run there (measured 09d16d1d0)|Opus 5, xhigh|UP (owner 2026-09-13); ARMED 66e22a44f|
|C2|cloud; can convert, cannot compile (no dotnet, no PowerShell; measured f3555892d); disk-constrained, ephemeral|Opus 5, high|UP (owner 2026-09-13); ARMED f3555892d|
|i9|i9|Opus 5, high; ONE serial item at a time|offline since 09-08|

## 3. Division of labour

**Design goes to COORD**, with rulings, merges, gates and trains. Lanes execute their section-5 items, post
design questions and lessons to COORD, and move on.

## 4. Current state

**Master `31fe4925d` -- TRAIN 47 LANDED (2026-09-13 17:26, run 8 of the assembly), fifteen seats, signed, announced before
pushing and read back equal at origin.** The chain before it: `654343a5e` (the KICKOFF refresh) ->
`45b58dc86` (H2's instrument re-anchored) -> `2e6cf71e4` (six lessons into the skills) -> `a02ac3df3`
(three more lessons; the train's base, frozen from 04:07 to the landing). **Master is OPEN again for the
post-train landings in this order:** doctrine batches c `06e2920fb` and d `21821509c` (verified LANDABLE),
this KICKOFF amendment, then train 48 assembles on the result.
<!-- Train 47's four runs on the i7 (HANDOVER-coordinator.md 2026-09-13 ~05:40 and ~06:35 blocks): run 1
     RED at LEG C (four seat test files carrying the retired MIT header -- the guard-collision class) and
     at LEG 2 (seat 6's union compile, seat UNSEATED to train 48); run 2 RED at G6 (a redacted placeholder
     read as a real segment -- row 12 rewritten, the G6T template class); run 3 RED at G11(b) (a seat-number
     premise from train 46 carried inside a justification and asserted -- derived from the OWED vector);
     run 4 light gates clean 06:32:28, battery run 8 (14:12 -> 17:18) green on every leg: converter suite 299 s, LEG 0 dial guard, integrity x3, stdlib and go2cs solutions CS=0, LEG D six arms MET, LEG R/U restores, GolibTests 772 at both configurations, CNR 1053 s CHANGED 0 with the advisory expectation DERIVED from the tree (52 == 2 + 50) and E1' MET on the record, the full suite 664/0, seven canary rows PASS; runs 4-7 were refused by the instrument on real defects (LD1, LD2, LR1, LA1, LL1) and the landing itself found three more in the land's reader (LB1, LC1, the wrapper line), each fixed with a self-check lesson before the push. -->

**The position on the runbook's section-2 ladder MOVES ONE NOTCH: H4 CLOSES for the known sites with this
landing.** H1 and H3 are done; **H2 has never run** -- `src/version.props` still reads 1.23.12 -- and it
lands as the FIRST commit of the H5 series, not on its own. **H4a is a staging BASELINE regen** by the
same binary that runs H5 (H0's fresh `.cs.auto`, H6's old side, H5's overlay comparand), never a 1.23.12
landing. **R's FIFTH rehearsal has RUN** (on the train-47 union reproduced on the R-LAPTOP before its travel
standby -- tree `161af6c441ae1d8fa44f10b44a9740ba2c20ecea`, the same tree this landing carries): the
seeded three-target 1.24.13 reconvert is clean and H5c applies, and **the rung is 120/120/120 unique
sites, every one in `runtime`, every root one of eight sites in two frozen hand-owns** (`runtime2.cs`
:21/:25/:729 and `mfinal.cs` :20/:24, the fourth relocation `runtime/internal/sys` -> `internal/runtime/sys`;
`runtime2.cs` :119/:123, the `note` duplicate beside the emitted `note_other.cs`). Both are C1-1's ruled
items and both are HOP-CONDITIONAL (they cannot land on the 1.23.12 corpus without breaking it), so
**what now gates the H5 SERIES is C1's PREPARED PATCH for those eight sites, applied by the H5 scratch
after the reconvert and H5c** -> **H4a's baseline** -> the series itself (H2's pin, the three-target
reconvert, H5c's fourteen removals, the overlay, `go generate`, H9, the hand-own branch). A re-derive of
`mfinal.cs` takes the hand-own body from C1's mcleanup branch, never from the landing tree (the carry
hazard; the guard on that branch is the post-condition). Predictions scored as WORDED: G's
`unique/handle.cs` isync HIT at emission (compile masked); the four (C) sites PARTIAL, 2 of 4 (the
PROTECTED hand-owns of `internal/weak` and `internal/concurrent` keep their `package_info.cs`, H6 OQ-2);
R's 5-of-12 and G's zero NOT SCOREABLE (seat 6 unseated, masked). **The R-LAPTOP is on FLEET STANDBY from 2026-09-13
~11:35 (owner travel; R in spurts only): i9 executes H4a and the H5 series by the runbook as R amended
it before standby, G-LAPTOP is the linux arm, R is consulted in spurts.** The hand-own branch is C1's
(FILES only; directory removal is H5c's instrument).

**Train 47 as LANDED -- FIFTEEN seats on `a02ac3df3`.**

|#|Branch @ tip|Class|
|---|---|---|
|1|`claude/coord-orphan-disclosure-check` `8e8c9e3b6`|converter-test+docs|
|2|`claude/coord-stamp-guard` `fc8c8d8ac`|converter-test+docs|
|3|`claude/laneR-armc-guard` `49c309f8b`|converter-test+docs|
|4|`claude/c2-sync-disclosure-retire` `4221789e7`|manifest|
|5|`claude/laneR-h5-lastrung` `826045a74`|docs-data|
|6|`claude/coord-pprof-vacuous-audit` `5994c12b2`|docs|
|7|`claude/g-h6-alias-census` `898cbfefe`|docs|
|8|`claude/g-generic-alias-recut` `449ecce7a`|converter-guard-rebaseline (carries its own `allowed=` re-baseline ruling)|
|9|`claude/c2-census-reader` `44ab61dad`|golib-converter-docs|
|10|`claude/laneR-h5-s15-rungs` `ff40eee3a`|docs|
|11|`claude/coord-glossary-kickoff` `ff9d0fb47`|docs|
|12|`claude/g-census-2026-09-13` `31adad88c`|docs|
|13|`claude/c1-crashwhiletracing-marking` `d781b0251`|manifest|
|14|`claude/c1-getcallerpc-erratum` `3ca63093d`|docs|
|15|`claude/c1-lockosthread-body` `dc34e4b4a`|golib-corpus-handown|

<!-- Rows 1-3 moved from 36cbef240 / ec1fe2745 / bbd0afe43 by ONE header commit each (the MIT header ->
     the AGPL pair the licensing guard requires; proven at a throwaway union with the unfixed file as the
     firing control).  The earlier row 8 (claude/g-unfreeze-handown-recut ce2d9d082) was UNSEATED after
     run 1's LEG 2 and rides train 48 as claude/g-handown-metadata-t48 bb13897e6 (re-based after this
     landing); rows 9-16 became 8-15.  Row 12 moved 748beefbb -> 31adad88c (one cell, +1/-1, the
     placeholder segment retired).  The table of record is the assemble script's SEAT_TABLE. -->

**The two riders are DISCHARGED.** Row 15's accounting: i9's run at C1's clean seat `4a9ae8cbb` reads 185
C# verdicts with the `got 0, 0` panic PRESENT -- five trees, one axis (`lockedExt++` sites 0 -> 185 with
the panic, 1 -> 198 without it, twice), so C1-2 = 128 -> 185 (one verdict moved, `TestGCTestIsReachable`'s
honest divergence) and seat 15 = 185 -> 198; i9's race hypothesis is REFUTED rather than unsupported,
and the runtime row's floor of record is 185 at the base + C1-2, 198 at + seat 15. i9's first-parent
bisect (BOARD entry `68ad83c2c`, train 48) attributes the earlier 185 -> 128 door regression to train 46's
seat 3 fatal-path merge; the BOARD entry takes the split as a dated amendment when train 48 gives it a
base. G's Runs A/B/C ride train 48 with seat 6.

**The mailbox is ROTATED, at `5e70540f4`.** The body through `d3216183f` is
`docs/phase4/MAILBOX-archive-2026-09-13.md` (cite it as `MAILBOX-archive-2026-09-13.md:NNNN`); the new
`MAILBOX.md` continues at the same path and read anchors are still commits. **Fleet rule: no build clone
tracks `claude/mailbox`** (`^refs/heads/claude/mailbox` in the fetch refspec) and **`fetch.unpackLimit=1`
everywhere**, because a refspec governs the REF while the OBJECTS transfer regardless -- the mailbox is
read from a dedicated single-branch clone with its own object store, never fetched into a build clone.
**The post order, ruled 2026-09-13:** an EXISTING ref announces then pushes (floor 9 protects a reader
from a moving ref); a ref that does not exist yet pushes then announces in ONE post carrying the remote
read-back and the tool's explicit `--new` acknowledgement. The coordinator's own log is
`claude/coord-handover` at **`e43146bdb5111fa7742dda38c8cc1d42bf6998c1`**, read by `git show` only, appended as dated blocks.

**In flight.** C1: `mcleanup.cs` as a hand-own, then C1-3 (the `TestMapBuckets` shim), then the runtime
row's contract-table seat. C2: the goroot-fix rules line and the shallow-skip seat (it covers C1's shallow
clone too), then the recon leg -- ALL 204 rows under the dispatch mode at the campaign's corpus, the
isolated-vs-in-sweep axis folded in, the shard map re-derived from that data. i9: the `archive/tar` BOARD
entry (1 crash in 27; 25 consecutive clean; rate <= 11.3% at 95%; cause unattributed; NO roster
condition), then the linux/Windows run arms as ruled. G: seat 6 re-based; the fleet patch-id census is
the whole-remote instrument (a census by its header, not a gate). R: the fifth rehearsal.

**Train 48's board** (read every tip at origin at the freeze; declare the H6 stack `898cbfefe` ->
`191164e7a` -> `47592cb3f` with `stack-on=`): C2 `4140a8e55`, `191164e7a`, `0b24685bc`, `171d419f6` +
`33c29952d`, `baf1fbe72` (`-Hop`), `a0496fb93`, `7c1d8832f` + its rules line, the shallow-skip seat; i9
`68ad83c2c`, the `archive/tar` entry; R `d18059950`, `47592cb3f`, `becf28abc`; C1 `4a9ae8cbb`,
`3f1612524`, `5f0564da3`, `claude/c1-seat-duplication-census` `77e41300a`; G `bb13897e6` (re-based),
`claude/g-fleet-patchid-census` `9b78bfff6`; COORD doctrine batch e. **The assembly's derive items:** the
patch-id arm (C1's tool over the table's tips with the declared stacks; same SHA = STACK allowed iff
declared, different SHAs sharing a patch-id = DUPLICATE refused; a seat beside its own clean re-cut reads
"a stale row"); no seat-number literal inside any justification or refusal string (a self-check arm);
every assertion matches a ROW at line start, never a substring of a report that states its counts in
words; the post tool re-fetches and re-appends after its guard, immediately before pushing.

**Train 49's board, cut in one morning while the battery ran** (every one off `a02ac3df3`, read at
origin at its freeze): C1's prepared patch for the two frozen `runtime` hand-owns
(`claude/c1-h5-rederive-patch`, the H4a gate's instrument: the applier's precondition keys on the
production `.cs` H5c removes, the carry hazard enforced as a post-condition); C1's token-door census
(`claude/c1-token-door-census`: `syscall`'s bank predates the door; one reached row, six latent); G's
repoguard liveness-and-set seat (`claude/g-repoguard-liveness-set`); G's H6 completeness gate and its
guard (`claude/g-h6-completeness-gate`: the six assertions literal, the BOM tolerated in the census
predicate); C2's darwin option-2 sizing (`claude/c2-darwin-option2-sizing`: a stub census is a superset,
not a work list; eleven implementations under 215 keystone sites) and its step 1 trampoline map guard
(`claude/c2-darwin-trampoline-map`: keyed per package); C2's H5c instrument amendment
(`claude/c2-h5c-apply-amendment`: DELETE-ABSENT packages as directories, the full delete set emitted; the
UNRESOLVED clause waits on a measurement); i9's `archive/tar` BOARD entry chained on its bisect entry;
the recon DATA record when pass 2 lands. Declared stacks and the BOARD-tail insert collisions are the
train-48 assembly's derive items, resolved in table order, never by hand.

**Owner hands.** CLOSED 2026-09-13: the R-LAPTOP mailbox refspec exclusion x4 and the poisoned
remote-tracking ref (R, owner-authorized); the 35 GOROOT strays (archived). OPEN: G-LAPTOP's stale
`.git/index.lock` (G's harness refuses deletions inside `.git`); **one cloud-allowlist entry for
`builds.dotnet.microsoft.com`** (fallbacks `dotnetcli.azureedge.net`, `dotnetbuilds.azureedge.net`) --
the cloud network policy returns 403 to CONNECT, and that entry alone gives C1 and C2 a LOCAL compile;
the untracked `src/lane-r-packrace.ps1` in the R-LAPTOP worktree `preflight-trio-de1c72` (delete; its
content is held in a preserved local commit); which host the 2026-09-02 thermal sentence describes; and
`claude/awesome-franklin-ba9agv` (a harness worktree name at `21222f2e8`, never a seat) on the
branch-deletion pass. **The R-LAPTOP is on FLEET STANDBY from ~11:35** (owner travel; R in spurts; its
local-only artifacts and durable logs are named by path in R's standby post `6f6528938`). **Archive:** the i7 archive (`C:/go2cs-archive/i7-2026-09-13`, 1.41 GB,
manifests verified) has been copied to the R-LAPTOP share and its verification is in progress -- that
closes the same-disk item on the reading. Still owed beyond the hands: the H5 hand-own branch's `.auto`
side, and a conforming-DNS Windows host for `net`.

## 5. Kickoff prompts

**Owner, per session** (worktree OUTSIDE every clone; ancestor CLAUDE.md files load):

```bash
( CLONE='<clone root>'; NEW='<sibling directory>'
  cd "$CLONE" && [ "$(cd "$(git rev-parse --show-toplevel)" && pwd)" = "$(pwd)" ] && git fetch --no-prune origin \
  && git worktree add --detach "$NEW" origin/master && cd "$NEW" \
  && git merge-base --is-ancestor 9355669f8e6d461306837135688cea9fad68f8aa HEAD && echo BASE-OK
  d="$NEW"; while [ "$d" != "$(dirname "$d")" ]; do [ -f "$d/CLAUDE.md" ] && wc -l "$d/CLAUDE.md"; d="$(dirname "$d")"; done )
```
Proceed only on BASE-OK with every listed CLAUDE.md under 1,000 lines.

> **Shared preamble.** You are **<SESSION>** of the go2cs fleet.
> **The owner's words, 2026-09-12:** "I explicitly authorize you, a lane session, to make UNSIGNED commits on
> lane branches with `git -c commit.gpgsign=false commit`. COORD signs merges and landing commits to master.
> Tags are signed. Mailbox commits stay unsigned." Valid only when the owner pastes it.
> **Step 0:** assert `git merge-base --is-ancestor 9355669f8e6d461306837135688cea9fad68f8aa HEAD`, else STOP.
> **First:** run section 1 on your machine; report to COORD. Mailbox fallback anchors (check by ancestry):
> COORD, R, G `827c8d7b00fe3f5934189f7cffd1b5764406736a`; C1 `30eb0316e5974563bf8ba38970390a0325eca585`; C2
> `7e0c20d1c8916e1cc67cfdabd878654d8d446cf8`; i9 `7c18aca21bbdb18cf1d00a37ee68199b643c4526`. Announce before
> pushing; never rewrite a posted SHA; seated branches take no commits; push via 1b; mailbox posts never force
> (safe-push refuses in the mailbox tree: tell COORD).
> **Toolchain (verbatim):** TWO-PIN PAIRING, as master states it (.claude/rules/converter.md:550-556; .claude/rules/harness-gates.md:46, :513, :518-519; docs/GoCorpusMigration.md:313, the fifth arm). Corpus batteries, CNR and the behavioral suite run in ONE shell. GOROOT is the go1.23.12 sdk, spelled exactly as `go env GOROOT` prints it; its bin comes FIRST on PATH; GOTOOLCHAIN stays UNSET (auto). The converter module declares go 1.24.13 and every corpus module declares go 1.23, so Go switches ONLY the converter's own build up to 1.24.13, through the module cache, and loads every corpus package at 1.23.12. converter.md: "`GOTOOLCHAIN` stays UNSET (auto) on the 1.23.12 pin so that switch can happen: at `GOTOOLCHAIN=local` CNR's OWN `go build` of the converter fails `go.mod requires go >= 1.24.13` and throws, which a wrapper then reports as "CHANGED 0"." Assert the pin from a directory with NO go.mod: inside src/go2cs, `go version` reports the switched toolchain. Abort unless the bare `go version` line reports go1.23.12. Verify the built binary with `go version <exe>`. Confirm `go env GOTOOLCHAIN` reads auto; an unset environment variable is not enough. Measured 2026-09-12 on R-LAPTOP: a user-level `go env -w` value (GOTOOLCHAIN=go1.23.1) makes bare `go version` report go1.23.1 even with the go1.23.12 sdk first on PATH. A hand-invoked two-arm instrument may use a SPLIT pin instead. It names one GOROOT on each `go build` (the go1.24.13 sdk) and another on each conversion (the go1.23.12 sdk). It needs no switch and works under either GOTOOLCHAIN setting. The conversion half still reaches a child `go`, so the split pin is sound only while every corpus module's directive stays below the convert pin. The runbook spells the two roots GOROOT_BUILD and GOROOT_CONVERT as prose names; no script, test or tool reads variables of those names. In the converter-pin window, -tests rows build the converter under 1.24.13 and run the pipeline under 1.23.12, and -SkipBuild is MANDATORY for the sweep. The old log's "converter builds under the go1.24.13 sdk with GOTOOLCHAIN=local" (log lines 36 and 142) is wrong as a battery-shell instruction and right only as the build half of a split pin. On a pushed surface, prove a pin by the bare `go version` line alone.

> **COORD (i7): Fable 5.1, ultracode.** (1) SECURITY at the two live tips; rule master's exposure with the
> owner. (2) PRESERVE the i7: process census by executable path; 1a in every worktree and mailbox clone;
> census, then commit or archive `.claude/coord-scripts/`; copy run-8 stdout; export coordinator memory;
> `cfd71b0ba` via 1b or ruled dropped; clear `/tmp/t46-assemble.lock` last. (3) Then: land this file and the
> CENSUS record; section 4's rulings and records owed; re-bases; seat 1's utf8 arm (check `sub-orphan-check`); seats 5, 6, 8; template
> defects with controls; C1's build arm; gate 2; the log block; accumulator 1322 onward.

> **R (R-LAPTOP): Opus 5, high.** First: section 1 on Windows and WSL; keep `refs/preserve/g-laptop/*` (1c); tag `0dfc95e21` locally (push and signing as COORD rules); archive
> `C:/go2cs-tmp` with hashes; locate `r-h5b-convert.sh`. Then: (1) seats `bbd0afe43`, `826045a74`,
> `ec1fe2745`: re-base as COORD rules. (2) After train 47: seeded ladder re-convert, predictions scored as worded.
> (3) H6 block on `898cbfefe`'s file. (4) Offer `87606f3a5`. (5) `eafcacdb7` HELD for H5. (6) At (2)'s re-convert, score whether `Ꮡr` at
> `os/root_openat.cs:123` (the 09-08 22:05 log block's defect) clears under `ce1ee957b`, landed in train 46; only a
> surviving site becomes a cut: minimal repro, announced first.

> **G (G-LAPTOP): Opus 5, high.** First: section 1 INSIDE WSL on every clone, then Windows; `git fetch --no-prune
> origin` before judging anything unpushed (last fetch 2026-09-03); delete no G-only branch until COORD rules;
> archive 1e's Temp items and H6 scripts with hashes. Then: (1) seat 8 is COMMITTED, never announced by SHA:
> `claude/g-generic-alias-qualifier` `ffaafeb19` (`4772d4907` converter, `ffaafeb19` guard and golden; base
> `8a1b7e71c`; 8 files +91/-3; clean). Re-cut it: 1b steps 0-5 on `ffaafeb19`, then step 6's cut
> through the commit only, on a NEW branch name (the local branch stays), by `git -c commit.gpgsign=false cherry-pick -x
> 8a1b7e71c..ffaafeb19` (a rebase in place only if COORD rules; sites now `typeNameResolution.go:423-425`,
> `:449-464`). Then, in the preamble's go1.23.12 battery shell: verify `1800b04f8`'s change and yours in that file BY
> NAME; CNR the branch's own goldens; re-score: guard RED pre-fix (CS0426); footprint ZERO x3; `unique/handle.cs:91-92`
> reads isync; `a27342d03`'s pairings hold. Then finish step 6 in the leg-1a shell: steps 2-5 over the range,
> ANNOUNCE the new SHA, push via `src/safe-push.sh`. (2) Seat 6 as COORD rules. (3) Offer `150b0264e`, `d7bf606f0`.
> (4) WSL is C1's linux arm. Push nothing to `claude/g-b1-box-design`.

> **C1 (cloud): Opus 5, xhigh; if restarted.** You cannot build: COORD's build arm runs golib, runtime and
> GolibTests first. Announced branches: (1) `LockOSThread` as Go's whole body. (2) Token door re-armed with the
> (API, argument) contract table. (3) Erratum block. (4) At H5, re-take `c5fb9e0ed` on a NEW branch.

> **C2 (cloud): Opus 5, high; if restarted.** `grep -c` form: `91e393d62`; retry-loop gap: `7f9e9f71f` (no fix).
> (1) H6 cross-check block on `898cbfefe`'s file. (2) Seats 4, 5 as COORD asks. (3) Increment 13 keeps AFTER
> `83385dad6` and BEFORE `9ecce1839`, blobs equal by hash.

> **i9: Opus 5, high; ONE serial item at a time; after restore.** First: section 1. `job-i9-root2`'s 4 `*Tests.cs` edits
> are very likely superseded by `31668f43e`: byte-compare before discarding. Then, one at a time as COORD confirms: (1) reflect
> census re-run on `claude/c2-census-reader`, ON beside OFF, read by C2's ARMED-ZERO rule. (The `runtime`
> results-file tail is ANSWERED: `0dd133719` on `44f858717`, 185 of 880 verdicts, next door `TestLockOSThreadNesting`,
> root-read by C1 at `18a34299f`; re-run only if COORD asks.) (2) When
> C1's `LockOSThread` seat exists, run it solo through the `runtime` pipeline and report by failure kind. Standing
> prediction, scored by whichever run first executes it: `TestRegisterClass` is unexecuted today; when it runs, the door
> must refuse its argument 0 (`Ꮡwc`) with the identical text, and any other outcome is a hole. Post each reading
> with its tree SHA, configuration and load.
