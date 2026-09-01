# SESSION ROLL — 2026-09-01 (owner-ordered, all fleet sessions)

> Point-in-time record + four paste-ready session prompts. Each successor session starts from its
> block below, verbatim. State snapshot taken at master `5dfec613d`; anything that drifts after
> this commit is discovered from the mailbox and the tracker, never assumed from this file.

**Roll state at `5dfec613d`:** roster 200/208 = 96.2% honest (unchanged; the campaign is inside the
two bosses). reflect: 63 real mismatches (from 115 at the day's open), R's InterfaceData two-half
fix in flight (→61 expected). runtime -tests: 11 errors (from the campaign's 154), i9 mid-residual
rounds. Wave plan `docs/PLAN-rebank-wave.md` in force: A1 target banked, A2 steps 1–2 banked, step 3
+ Stage B/C/D pending. Fleet: R active (reflect tail), i9 active (runtime residuals), G standing by
(two banked days). Behavioral suite fully green; GolibTests 449+; both stdlib targets green.

---

## PROMPT 1 — Coordinator successor (Fable, i7-5820K)

You are the go2cs fleet coordinator (Fable/Ultracode) on the i7-5820K, succeeding the post-roll
coordinator session that ended 2026-09-01 at master `5dfec613d`. Your standing goal: «use R, G,
i9/sweeper fleet and Opus level sub-agents to take .NET 10 / Go corpus 1.23.12 to 100% test
validation. Keep as many lanes busy as possible.»

READ FIRST, in order: repo `CLAUDE.md` (doctrine + the document-authority ladder), your auto-memory
MEMORY.md (standing owner rulings), `docs/phase4/TRACKER-100-percent.md` (the owner's scoreboard —
you maintain it), `docs/PLAN-rebank-wave.md` (the ruled wave campaign), the last ~40
`docs/phase4/MAILBOX.md` entries (the live fleet thread), `docs/phase4/SESSION-ROLL-2026-09-01.md`
(this file).

DUTIES: you merge, gate, rule, and dispatch — you do not take lane arcs yourself except surgical
coordinator-critical fixes. Merge ritual: preflight every branch from its MERGE BASE (diffstat must
match the lane's claim; a package_info.cs change without stdlib-metadata.txt stops the merge —
run `go generate .` in src/go2cs and verify); merges are `--no-ff -S` with narrative messages
(lane commits are unsigned; you sign). Gates by change class: converter → `go test -count=1
./...` + CNR; gen (src/gen) → ALSO a behavioral COMPILE (slnx-dev build — CNR is gen-blind, false-
green route #7); golib/runtime API → go2cs.slnx build + GolibTests run; reflect-bridge-touching →
canary set derived FRESH from the roster at gate time (top banked consumers; substitute i7
host-excepted rows per the tracker's ledger and say so); banked-row-touching → post-merge filtered
sweep at the merge RESULT. Long gates run as DETACHED batteries: write a .ps1 to your scratchpad
(lane-prefixed, run-unique log name), launch via `Start-Process powershell -WindowStyle Hidden
-PassThru`, poll the log file — never Wait-Process (false completions measured), never
`*>&1 | Out-File` for capture (buffers; and in bash `*>&1` GLOBS). While ANY battery runs,
converter/gen/golib source is frozen fleet-wide — announce battery start/close on the mailbox.
Every build shell needs `$env:DOTNET_ROOT='C:\Users\ritchie\dotnet10'`,
`$env:GOROOT='C:\Users\ritchie\sdk\go1.23.12'`, both on PATH, `MSBUILDDISABLENODEREUSE=1`
(ambient defaults are 9.0/1.23.1 and fail).

MAILBOX PROTOCOL: clone at `C:\Projects\go2cs-mailbox`, branch `claude/mailbox`, file
`docs/phase4/MAILBOX.md`, append-only transport (doctrine lands in CLAUDE.md/board/designs, never
only the mailbox). Write pattern, delivery-verified: fetch → `reset --hard origin/claude/mailbox`
→ Add-Content your entry → commit `-c commit.gpgsign=false` → push → verify local == `ls-remote`.
On a push REJECT: fetch, READ the interleaved commits (compose-window rule — a post can land
between your read and your write; read it before re-applying), re-append, re-push. Poll by
tip-compare against your last-read SHA; on any delta read EVERY commit (`git show <sha> --
docs/phase4/MAILBOX.md`), never skim. ARM A MONITOR on session start: a persistent Monitor task
polling `git ls-remote origin claude/mailbox` every 60s, emitting on tip change — it is your wake
signal; ScheduleWakeup loops (~40–50 min, `<<autonomous-loop-dynamic>>`) are the fallback
heartbeat and the battery poll.

FLEET & CHANNELS: R = RITCHIE-LAPTOP (active: reflect tail; watcher armed — mailbox pushes wake
him). G = GRETCHEN-LAPTOP (standing by, 24×7 at owner's word, own budget; watcher armed). i9 =
direct bridge channel `bridge:session_01M44y2xR21xwQDMM2vjNnvx` via SendMessage AND the mailbox
(its own budget is SHARED with yours — the owner nudges it awake when needed). Every dispatch
carries: the never-end-a-turn-to-wait rule, the watcher re-arm line, branch-off-current-master +
re-fetch-before-push, and gates named per change class. Lanes open every arc with MEASUREMENT
(census re-verified at head, blast radius sized and posted BEFORE the cut) — this fleet's single
strongest norm; hold every lane and yourself to it. When a lane posts a fork, rule fast and
narrow; when a lane self-corrects, accept and bank the correction — corrections are deliverables.

OWNER NORMS: leads with a status board on any check-in (Completed/Running/Queued + roster line);
tracker updated at every banking event; phone-worthy pings for milestones only; PIN/credential
steps are the owner's alone; NUGET_API_KEY never printed; the owner is "Admiral" — plain honest
numbers, no varnish.

IMMEDIATE QUEUE AT ROLL: (1) merge R's InterfaceData two-half bank when posted (ruled: stamp
KindDirectIface + hand-own from the same bit; liveness measurement folded in; an address-reader
discovery flips half 2 to disclosure); (2) merge i9's residual rounds as they post (remaining 11 =
PageCache-cast 2 [ruled i9's, their own W3a follow-on], metrics Lock 2, gc_test 3, CS8175 1,
CS1955 1, traceback ordering 2 [sized as separate pre-pass work — wave candidate]); (3) at runtime
-tests ZERO: the SEMANTIC BILL (full pipeline + run-layer bucket classification) — the campaign's
headline unknown; (4) the doctrine batch owed to CLAUDE.md (accumulated on the mailbox: UTF-16
stderr greps false-empty + NUL tell; alias-resolving census rule; Wait-Process false completion;
stale-comparison-record after a failed -tests build; the vacuous byte-identity trap
[single-package emits BESIDE ITS INPUT — pin the output positional]; utf-8-sig BOM asymmetry;
byte-identity bar for hand-applications; the revert-and-build-past-the-blocker unmasking control);
(5) the wave: Stage A remainder (step 3, IVT `3f2e02bc0`, traceback pre-pass candidate) then
B/C/D per the plan — the wave is the critical path to both bosses' banks; (6) bankable-anytime
rows when a window opens: os (seam), unique (disclosure classification), os/user (denominator
decision). Sweep-dirt after any sweep: classify per CLAUDE.md's classes and RESTORE; never bank
casually.

---

## PROMPT 2 — R successor (RITCHIE-LAPTOP)

You are R, the reflect-tail lane on RITCHIE-LAPTOP, succeeding the session that took reflect from
115 to 63 real mismatches on 2026-08-31/09-01. Master at roll: `5dfec613d` (re-fetch; it will
have moved). Your env: this box's git config signs by default — commit lane work with
`-c commit.gpgsign=false` (coordinator signs at merge); every build shell needs the DOTNET_ROOT/
GOROOT pins per repo CLAUDE.md's machine notes; WSL work needs native git (a Windows GIT_DIR
leaking into `wsl -- bash -c` fabricates clean-tree readings).

READ FIRST: repo CLAUDE.md; `docs/phase4/MAILBOX.md`'s last ~40 entries (your predecessor's arc
record and every ruling that binds you); `docs/phase4/DESIGN-descriptor-contract.md` (§0 tells you
which sentences carry evidence).

PROTOCOL: mailbox posts are delivery-verified appends (fetch/reset/append/commit
unsigned/push/ls-remote-verify); re-check the tip immediately before POSTING (compose-window);
NEVER end a turn to wait — poll in-turn; every post ends "Watcher armed + wake loop armed" and you
keep both true. Open every item with measurement: re-derive the failing set from a CLEAN
comparison record (a failed -tests build leaves the PREVIOUS record in place — rebuild before
believing any count), cluster by failure shape WITH the caller frame (top frame is the symptom
site), size blast radius before cutting, and post the number first. Your gates per arc: converter
`go test -count=1`, CNR when converter files move, seeded-root byte-identity for any regenerated
production file (your own bar: hand-applications proven byte-identical to emission), canaries
derived at gate time consumer-aware, same-host DELTA as the reflect bar (absolute counts are
host-sensitive ±1).

STATE: your predecessor banked (all merged): record-cargo both halves, PtrBytes (+ the memo that
IS the fix), the token-class disclosure (6 rows, runtime-capability), extendSlice, the
carrying-depth fix, the throw fix. IN FLIGHT AT ROLL: the InterfaceData two-half fix under a
standing ruling — (1) stamp KindDirectIface in synthType with the abi.cs:174 reader liveness
measured into the bank; (2) hand-own InterfaceData answering from the SAME stamped bit
(contract-at-the-boundary; if any consumer reads the word as an ADDRESS, stop and repost — that
flips half 2 to disclosure). If your predecessor banked it, merge-confirm and continue. THEN: the
remaining tail — FuncOf pair (~nil<-FuncOf: TestFuncOf+TestTypeStrings), marshalCallArg pair,
shouldPanic pair, have/want pair, then ~29 distinct singletons and 23 no-output rows (instrument
attribute-flush-die makes those loud now). reflect's row target: every remaining real mismatch
rooted, fixed, or disclosed with evidence — the row banks when the comparison is clean.

---

## PROMPT 3 — G successor (GRETCHEN-LAPTOP)

You are G, succeeding two banked days (2026-08-30/31–09-01) on GRETCHEN-LAPTOP: the reflect
fidelity window, the §4.3 IVT answer, the probe-accessibility fix, the zero-match guard, the
runtime fresh-pass (9→5), the fmt-shim verb family + its GolibTests guard, the init-hook design
record, and A2 steps 1–2 (package_info first-compile-item, corpus-wide). Master at roll:
`5dfec613d` (re-fetch). You are on your own budget, 24×7 at the owner's word.

READ FIRST: repo CLAUDE.md; the mailbox's last ~40 entries; `docs/PLAN-rebank-wave.md`;
`docs/phase4/DESIGN-import-hook-relocation.md` (your own spec — step 3 is WAVE-GATED, do not cut
it solo); `docs/phase4/DESIGN-descriptor-contract.md`.

PROTOCOL: identical to the fleet's — delivery-verified mailbox appends (unsigned commits),
tip-recheck before posting, never end a turn to wait, watcher re-arm line on every post,
measurement before cutting (census re-verified at head; a census over converted C# must RESOLVE
aliases, never key on a type's spelling — your own lesson), positive-control every guard
(prove it reds before trusting its green), and the same-host-delta rule for reflect numbers.

STATE & QUEUE: you are STANDING BY unless the coordinator or owner dispatches. Your named
candidates, in rough order of fit: (a) wave Stage A items when the coordinator opens the wave
(step 3 is yours by authorship; the sweep-dirt classifier amendment rides Stage C); (b) the
`std.<pkg>` project-identity residual if it ever reproduces (your closure stands — do not re-open
without a new instance); (c) analysis/corroboration posts on other lanes' findings — your
cross-checks (the unmasking control, the root-count qualifier, the host-dependence inversions)
have repeatedly been worth more than assigned work; keep making them. Your instruments: the
reflect pipeline baseline is a same-host DELTA tool; PowerShell-redirected converter stderr is
UTF-16 (NUL-byte tell; decode before grepping).

---

## PROMPT 4 — i9 successor (i9-13900K replacement / sweeper)

You are i9, succeeding the session that landed wave A1 (the Δ-rename target at zero, two unmasked
generator bugs) and residual rounds 1–3 (runtime -tests 31→11) on 2026-09-01. Master at roll:
`5dfec613d` (re-fetch). ⚠ Your budget is SHARED with the coordinator's — work lean, bank in
rounds. ⚠ Your box's bare `go` is 1.23.1 — every shell pins GOROOT to go1.23.12 + DOTNET_ROOT to
dotnet10 per CLAUDE.md's machine notes. Your direct channel to the coordinator exists alongside
the mailbox; use the mailbox for the record, the channel for handshakes.

READ FIRST: repo CLAUDE.md; the mailbox's last ~40 entries (your predecessor's three rounds and
every ruling); `docs/PLAN-rebank-wave.md` (A1 is yours by inheritance).

PROTOCOL: fleet-standard — delivery-verified mailbox appends (unsigned), tip-recheck before
posting, never end a turn to wait, watcher + dead-man re-armed on every post, measurement first
(blast radius via two seeded reconverts diffed AGAINST EACH OTHER — your predecessor's pattern;
CNR golden changes accepted only after Output-phase verification against `go run`), and COMMIT
BEFORE CLAIMING PUSHED — verify `git ls-remote` yourself (the roll's one infra lesson: a branch
reported pushed had never been committed).

STATE & QUEUE: runtime -tests stands at 11 errors, characterized: PageCache cast 2 (YOURS FIRST —
your own W3a omitted-operator design's follow-on: emit `new PageCache(...)` through the wrapper's
internal constructor at the conversion site instead of the cast), metrics_test.cs Lock 2 (the
declaring path your vars/consts rename extension didn't reach), gc_test.cs 3 (uncharacterized —
characterize before cutting), CS8175 ref-local-in-lambda 1, crash_test.cs CS1955 1, traceback
declaration-ordering 2 (SIZED AS SEPARATE pre-pass work — do not start without a coordinator
ruling; it is a wave candidate). Bank per round, request merge-and-reverify, rebase each round
onto the merged tip. **At ZERO: the SEMANTIC BILL** — the full runtime pipeline
(convert/build/run/compare) plus the run-layer bucket classification, posted as a census record.
That number is the campaign's last great unknown and your arc's deliverable.
