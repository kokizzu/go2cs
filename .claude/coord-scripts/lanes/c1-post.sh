#!/usr/bin/env bash
# c1-post.sh [--dry-run] <entry-file> <subject-file>
#
# C1's mailbox post tool, REBUILT 2026-09-19 from .claude/skills/mailbox/SKILL.md after the
# container restart took the previous one. Every rule below is that skill's; the comment names the
# defect each step exists for, so a future rebuild does not have to re-derive them.
set -uo pipefail

SP="$(cd "$(dirname "$0")" && pwd)"

# ── the two paths this tool needs, ENVIRONMENT-DERIVED (COORD 7bf197e27) ───────────────────────
# This file is readable in the tree so a claim about it can be settled by a tree reading instead
# of two posts (C1 e8b86aedf, C2 ac16d4140: C2 asserted a property of this tool it could not have
# read, because the file was not in the repository — and the same absence meant no claim of MINE
# about it could be checked either). Readability is the whole of the change: the tool is otherwise
# exactly what it was, and nothing here is a protocol change or a proposal that lanes converge.
#
# Both paths were absolute and lane-local until now. They are derived, with an override, so the
# file carries no profile path and runs from wherever it sits.
#
#   C1_POST_CLONE    the DEDICATED mailbox clone this tool appends in — never a worktree that has
#                    other work in it, because a failed step here must not dirty a working tree.
#   C1_CENSUS_CLONE  any clone with an `origin` remote; the census is materialised from ITS
#                    origin/master at call time, never read out of its working tree.
CLONE="${C1_POST_CLONE:-${TMPDIR:-/tmp}/c1-mailbox}"
# ⚠ NO FOURTH DEFINITION (ruled; C2 re-measured it at 881a37afd). This tool carries NO identifier
# arm of its own -- it CALLS the fleet's one census. My own private draft proved why the ruling is
# right: its raw predicate read 20 IPv4 and 80 email "hits" on the real MAILBOX.md, every one a
# false positive, and the six exemptions I wrote to silence them WERE the fourth definition.
# ⚠ MATERIALISED FROM origin/master AT CALL TIME, never read out of a working tree (COORD
# 381577a8a: "every lane's tool should", after C2 measured the live form at db9c69854 -- its
# working-tree copy predated the release-literal admit 43ee2ac8b3 and REFUSED 1.24.13.3, a literal
# COORD had already made postable). A working-tree copy of a shared definition goes stale the
# moment the definition moves, and NOTHING SAYS SO: this tool's copy was fresh only because C1
# happened to check that clone out to master an hour earlier. That is luck, not a mechanism.
#
# This is NOT a fourth definition -- it is THE definition, fetched from the ref that owns it. The
# materialisation is asserted (a truncated or missing blob REFUSES the post rather than running a
# partial census), and the ref it came from is printed so every post says which census judged it.
#
# ⚠ THE WHOLE DIRECTORY, not the script: the census resolves its patterns file as
# "$(dirname $0)/coord-identifier-patterns.txt" (census :193-195) and REFUSES(2) without it. C1's
# first cut of this fix materialised the script blob ALONE, and the refusal is what caught it --
# fails-closed, so nothing unscanned could have been posted, but the tool would have refused every
# post. The dep scan that missed it grepped for `source`/`.claude/` and not for `dirname`.
# Derived from this file's own position when it sits at .claude/coord-scripts/lanes/, so the
# committed copy needs no environment at all inside a checkout; overridable for any other layout.
# A missing remote REFUSES below rather than falling back to a working-tree census.
CENSUS_CLONE="${C1_CENSUS_CLONE:-$(cd "$SP/../../.." 2>/dev/null && pwd || echo "$SP")}"
# ⚠ A DOOR FOR THIS PATH TOO (COORD 7a959706f, from G's 664e6925b / R's fe5f4089c / this lane's
# own audit): a door is a property of a PATH, and this was the ONE path of four with no override --
# so an arm that carefully redirected the post clone and the anchor file still wrote the real
# materialisation directory. Three of four doors read as sandboxed and were not.
CENSUS_DIR="${C1_CENSUS_DIR:-${TMPDIR:-/tmp}/c1-census-from-master}"
CENSUS="$CENSUS_DIR/coord-identifier-census.sh"
ANCHOR_FILE="${C1_ANCHOR_FILE:-$SP/c1-mailbox-anchor.txt}"
MB=docs/phase4/MAILBOX.md
DRY=0
[ "${1:-}" = "--dry-run" ] && { DRY=1; shift; }

# ── step 0: resolve paths BEFORE any cd ────────────────────────────────────────────────────────
# ⚠ C1 f9f41e8d8 §7: this script cd's to the post clone, so a RELATIVE entry path resolved to
# nothing after the cd and `cat` appended an empty line. readlink -f first, always.
ENTRY="$(readlink -f "${1:-}" 2>/dev/null || true)"
SUBJF="$(readlink -f "${2:-}" 2>/dev/null || true)"
[ -n "$ENTRY" ] && [ -s "$ENTRY" ] || { echo "POST REFUSED: entry file missing or empty: '${1:-}'"; exit 2; }
[ -n "$SUBJF" ] && [ -s "$SUBJF" ] || { echo "POST REFUSED: subject file missing or empty: '${2:-}'"; exit 2; }
SUBJECT="$(head -1 "$SUBJF")"
ANCHOR_WAS="$(cat "$ANCHOR_FILE" 2>/dev/null)"

# ── step 0b: materialise the census from origin/master, and ASSERT it ───────────────────────────
# ⚠ EXPLICIT REFSPEC, and the leading `+` is deliberate. `git fetch origin master` SUCCEEDS in a
# SINGLE-BRANCH clone and writes NO `refs/remotes/origin/master`: the objects land in FETCH_HEAD and
# nowhere `rev-parse origin/master` can see, because the opportunistic remote-tracking update only
# applies to refs the configured refspec maps, and a single-branch clone's maps exactly one. So the
# `|| true` below swallows NOTHING -- the fetch genuinely returns 0 -- and the step AFTER it fails.
# That is how this tool refused two of C1's own posts on 2026-09-20, correctly and fails-closed,
# with a message that named the materialise and not the fetch. A command that succeeds while
# reaching nothing is this fleet's most expensive instrument class, and it was living in this line.
#
# Measured before the change, two clone shapes x two lines, the ref DELETED before each cell:
#     shape A, full clone     refspec +refs/heads/*:refs/remotes/origin/*
#         old line   rc 0, origin/master WRITTEN     new line   rc 0, WRITTEN   <- no regression
#     shape B, single-branch  refspec +refs/heads/<one>:refs/remotes/origin/<one>
#         old line   rc 0, origin/master ABSENT      new line   rc 0, WRITTEN   <- defect, and fix
#
# The `+` because the default refspec a full clone carries is itself forced: without it a
# non-fast-forward on master would be REJECTED here and reintroduce the same silent shortfall by a
# second route. The refusal downstream is unchanged and still fails closed.
# ⚠⚠ INTO A REF THIS TOOL OWNS, never `refs/remotes/origin/master`. The old line force-updated the
# operator's OWN tracking ref -- CENSUS_CLONE defaults to the repo this script sits in -- on EVERY
# run, dry ones included, 217 lines above the dry-run gate. RED-TESTED before this change: a
# `--dry-run` against a clone whose origin/master was deliberately stale moved it from c22f9b8e74ad
# to 7674ee7f4d19. That is C2's watcher defect (4f29a8742 §5) through a different door -- a tool
# moving a tracking ref in a clone whose refs the operator reads for measurements -- and it is why
# the doctrine line says a door must cover every path a tool WRITES **OR FETCHES INTO**.
#
# A private namespace rather than COORD's two suggestions (a clone nobody reads, or moving the fetch
# below the dry-run gate): no second clone to keep, and a REAL post stops moving the ref too, which
# "below the gate" would not. Nothing else reads refs/c1-post/*, so the tool owns the path outright.
# ⚠⚠ `--refmap=` (EMPTY) IS LOAD-BEARING AND A DESTINATION REFSPEC ALONE IS NOT ENOUGH. git
# OPPORTUNISTICALLY updates the remote-tracking branch for any ref named on the command line, so
# fetching refs/heads/master still moved refs/remotes/origin/master even once the destination was
# this tool's own. MEASURED: the first cut of this fix changed the refspec, read correctly, and the
# green arm STILL reported the operator's ref moving c22f9b8e74ad -> 7674ee7f4d19. An empty refmap
# tells git to rely entirely on the command-line refspec and update nothing else.
git -C "$CENSUS_CLONE" fetch --refmap= origin +refs/heads/master:refs/c1-post/census-master --quiet 2>/dev/null || true
CENSUS_REF="$(git -C "$CENSUS_CLONE" rev-parse --short refs/c1-post/census-master 2>/dev/null)"
mkdir -p "$CENSUS_DIR"
for f in coord-identifier-census.sh coord-identifier-patterns.txt coord-identifier-hashes.txt; do
    # ⚠ A LINE FLOOR PER FILE, not just on the census script. `-s` alone passes a file that is
    # genuinely short, and A SHORT PATTERNS FILE IS NOT A REFUSAL -- IT IS A QUIETER CENSUS. Measured
    # 2026-09-20 on this box: truncated to 140 of its 166 lines the patterns file passes `-s`, the
    # census runs 5 of its 16 arms, exits 0 and reports CLEAN over an entry the full file REFUSES.
    # The census's own guard does not cover it -- it refuses by name at ZERO arms and runs a REDUCED
    # battery for anything between -- and the arms are lost in FILE ORDER, so which identifiers
    # survive is decided by where their arm happens to sit: an 80-line cut drops fourteen of sixteen,
    # every nickname arm and every address arm among them.
    #
    # ⚠⚠ WHAT THESE FLOORS DO AND DO NOT CLOSE, measured rather than assumed, because a guard
    # described as more than it is, is worse than none:
    #   THEY CLOSE   gross truncation -- a file cut below its floor never reaches the cache, since
    #                this check runs BEFORE the mv.
    #   THEY DO NOT  close the 140-line case above. 140 >= the patterns floor of 100, so the very
    #                truncation that produced the false green PASSES. Arms are back-loaded: eleven of
    #                the sixteen live in the last 26 lines, so NO line floor short of the file's own
    #                length is a proxy for arm count, and a floor at its own length is a false refusal
    #                waiting for the day the fleet legitimately trims a pattern.
    #   NOR DOES     the battery count notice: the self-test reports 95 arms attempted on a 166-line
    #                AND on a 140-line patterns file -- it is not a function of this file (C2's point
    #                from a second direction, `7448c80de`: a self-reported count is not a measurement
    #                of the census).
    #   NOR DOES     any of this reach a census that LIES -- an arm-less body printing a plausible
    #                `SELF-TEST: pass=N fail=M` clears every floor and every count.
    # The predicate that closes all three is BYTES, not behaviour: i9's tool hashes its local copy
    # against master's blob before executing it (`d321609fa`) and refuses a truncated, an arm-less
    # and a lying census alike. This tool materialises from `origin/master`, so the same predicate is
    # available to it cheaply. Floors are what was ruled here (COORD `11afb1094`, C2's numbers at
    # `f4b736452f`).
    #
    # ⚠ THE BYTE PREDICATE IS NOW TAKEN -- COORD `7c3612fb5`, amending `ebd466e89` on C1's red at
    # `d7f8f842a`. It is added BELOW rather than replacing these floors: a floor refuses earlier and
    # says WHICH file was short and by how much, which is the more useful message in the common case;
    # the hash refuses the cases no floor can see. The red that moved the ruling: the patterns file
    # truncated to 150 of 167 lines passes `-s`, passes its 100-line floor, leaves the census RUNNING
    # and reporting CLEAN on TEN of twenty-one arms -- and the self-test figure is UNCHANGED at 116,
    # because that figure is pass+fail (79+37) and the 37 failures were read by nothing.
    #   census 1000 of 1415   patterns 100 of 166 (the tightest, 1.66x)   hashes 10 of 34
    case "$f" in
        coord-identifier-census.sh)    _floor=1000 ;;
        coord-identifier-patterns.txt) _floor=100  ;;
        coord-identifier-hashes.txt)   _floor=10   ;;
        *)                             _floor=1    ;;
    esac
    # ⚠ TMP THEN MV, never straight into place. `>` truncates its target BEFORE the command to its
    # left runs, so a FAILED `git show` leaves a ZERO-BYTE file behind and then exits 2. This tool is
    # protected by the two asserts below and refuses closed -- but the cache is a PATH, and anything
    # else reading it afterwards gets a census that exits 0 and scans nothing, which is a green over
    # nothing wearing the census's name. Measured 2026-09-20: this tool's own red arm for the refspec
    # fix above truncated the cache, and the next three BY-HAND census calls on that box returned
    # rc 0 with ZERO output lines before the empty file was noticed. A refusal must leave the
    # previous good copy exactly where it was.
    git -C "$CENSUS_CLONE" show "refs/c1-post/census-master:.claude/coord-scripts/$f" > "$CENSUS_DIR/$f.tmp" 2>/dev/null \
      || { rm -f "$CENSUS_DIR/$f.tmp"; echo "POST REFUSED: cannot materialise $f from origin/master"; exit 2; }
    [ -s "$CENSUS_DIR/$f.tmp" ] || { rm -f "$CENSUS_DIR/$f.tmp"; echo "POST REFUSED: $f materialised EMPTY"; exit 2; }
    _n="$(wc -l < "$CENSUS_DIR/$f.tmp")"
    [ "$_n" -ge "$_floor" ] || { rm -f "$CENSUS_DIR/$f.tmp"; echo "POST REFUSED: $f materialised at only $_n line(s), floor $_floor -- a short file is a QUIETER census, not a failed one"; exit 2; }
    # ⚠ THE CONTENT HASH, against the blob this file was materialised FROM. `git show` can succeed
    # and still write something that is not the blob -- a partial read, a mangled path on a shell that
    # rewrites arguments, a cache another process touched between the write and here. Comparing the
    # id is exact, costs one plumbing call, and needs no knowledge of what the file should contain.
    # BEFORE the mv, so a mismatched file never enters the cache and the previous good copy stands.
    _want="$(git -C "$CENSUS_CLONE" rev-parse "refs/c1-post/census-master:.claude/coord-scripts/$f" 2>/dev/null)"
    [ -n "$_want" ] || { rm -f "$CENSUS_DIR/$f.tmp"; echo "POST REFUSED: cannot read origin/master's blob id for $f"; exit 2; }
    _got="$(git hash-object "$CENSUS_DIR/$f.tmp" 2>/dev/null)"
    [ "$_want" = "$_got" ] || { rm -f "$CENSUS_DIR/$f.tmp"; echo "POST REFUSED: $f does NOT match origin/master -- blob $_want, materialised $_got"; exit 2; }
    mv -f "$CENSUS_DIR/$f.tmp" "$CENSUS_DIR/$f"
done
# A truncated blob is the failure this assert exists for: a short census still RUNS and still exits
# 0 on a body it never finished scanning, which is a green over nothing of exactly tonight's class.
# Kept deliberately beside the loop's floor rather than replaced by it: the loop's check is BEFORE the
# mv, so a short file never enters the cache; this one is AFTER, so it also reads a cache some other
# process corrupted between the mv and here. Two readings of the same property at two moments.
CENSUS_LINES="$(wc -l < "$CENSUS")"
[ "$CENSUS_LINES" -ge 1000 ] || { echo "POST REFUSED: census materialised at only $CENSUS_LINES lines"; exit 2; }
chmod +x "$CENSUS"
echo "census: origin/master $CENSUS_REF ($CENSUS_LINES lines)"

# ── step 0c: ONE BATTERY, AND IT IS THE STRONGEST THIS BOX PRODUCES ───────────────────────────
# ⚠ C1 37a41093d, RULED by COORD 7c71a87f0. The census derives its run-time arms from the git
# identity visible at the INVOKING DIRECTORY, so the same bytes over the same file answer
# differently from two directories. Measured on this box: an entry carrying a denied-set name read
# REFUSED(1) (`RUNTIME_OWNERNAME occ=1 hits=1`) from the repo checkout and CLEAN from the post
# clone. The old shape ran entry/subject from the CALLER's cwd and tree from the clone -- two
# batteries in one invocation, with nothing recording which certified what, and the strong one only
# because of where I happened to call from.
#
# ⚠ NO FOURTH DEFINITION: this re-implements no arm. It runs the FLEET'S OWN selftest from each
# directory this tool already has, takes the strongest, runs every arm from that one, and REFUSES
# when the battery that would certify is weaker than the maximum this box produces. The census
# stays the only implementation; this reads the number it already prints.
# ⚠ ARMS ATTEMPTED (pass + fail), never pass alone -- C2's cross-lane finding on this very blob
# (3c41c49ca), RULED at 0483558ff. An arm that RAN AND FAILED is evidence the arm is live here; an
# arm that was SKIPPED is not. Counting passes alone CREDITS a directory for never running one, so
# where an arm fails in one directory and is skipped in another the pass-only count prefers the
# weaker. Measured on this box the moment it was pointed out: the post clone prints
# `pass=91 fail=1` -- 92 attempted, not 91 -- so the number this tool printed in every post it has
# made understated what actually ran there.
idc_arms() {   # the selftest's ATTEMPTED arm count for a directory, or 0 when it cannot run there
    [ -d "$1" ] || { echo 0; return; }
    ( cd "$1" 2>/dev/null && "$CENSUS" selftest 2>/dev/null ) \
        | sed -n 's/^SELF-TEST: pass=\([0-9][0-9]*\) fail=\([0-9][0-9]*\).*/\1 \2/p' | tail -1 \
        | awk 'NF==2 { print $1 + $2; seen=1 } END { if (!seen) print 0 }'
}
IDC_BEST_DIR=""; IDC_BEST=0
for _d in "$CENSUS_CLONE" "$CLONE" "$PWD"; do
    _n="$(idc_arms "$_d")"
    if [ "$_n" -gt "$IDC_BEST" ]; then IDC_BEST="$_n"; IDC_BEST_DIR="$_d"; fi
done
[ "$IDC_BEST" -gt 0 ] || { echo "POST REFUSED: the census selftest produced no arm count from any directory this tool has"; exit 3; }
# C1_CENSUS_DIR_FORCE is the RED ARM's forcing hook and nothing else -- it exists so the refusal
# below can be made to fire on a box where the axis can move. Never set in normal use.
IDC_DIR="${C1_CENSUS_DIR_FORCE:-$IDC_BEST_DIR}"
IDC_USED="$(idc_arms "$IDC_DIR")"
# ⚠ -lt, NEVER equality -- C2's second cross-lane finding, RULED at 0483558ff. The rule is "never
# WEAKER", and `-eq` also refused a STRONGER forced battery: a directory outside this tool's own
# candidate list can attempt more arms than any of them, and equality called that a violation.
if [ "$IDC_USED" -lt "$IDC_BEST" ]; then
    echo "POST REFUSED: the certifying battery attempts $IDC_USED arm(s) from '$IDC_DIR', but this box produces $IDC_BEST from '$IDC_BEST_DIR' -- a weaker battery must never certify a post"
    exit 3
fi
# ⚠ THE SELF-TEST'S FAIL COUNT REFUSES, not only its attempted count -- COORD `7c3612fb5`, the second
# half of the amended rule. `idc_arms` sums pass+fail ON PURPOSE (a pass-only count prefers the
# WEAKER directory where an arm fails in one and is skipped in another), and that sum is exactly what
# cannot notice a broken census: the red at `d7f8f842a` read `pass=79 fail=37` -- the SAME 116 the
# healthy pair gives. A non-zero fail is the materialised tool saying it is broken, in its own words,
# and until now nothing downstream consumed it. Read from the SAME directory that will certify.
idc_fails() {  # the selftest's FAIL count for a directory, or -1 when it cannot be read there
    [ -d "$1" ] || { echo -1; return; }
    ( cd "$1" 2>/dev/null && "$CENSUS" selftest 2>/dev/null ) \
        | sed -n 's/^SELF-TEST: pass=[0-9][0-9]* fail=\([0-9][0-9]*\).*/\1/p' | tail -1 \
        | awk 'NF==1 { print $1; seen=1 } END { if (!seen) print -1 }'
}
IDC_FAILS="$(idc_fails "$IDC_DIR")"
[ "$IDC_FAILS" = "0" ] || { echo "POST REFUSED: the certifying census self-test reports fail=$IDC_FAILS from '$IDC_DIR' (expected 0) -- a tool that says it is broken does not certify a post"; exit 3; }
echo "census battery: $IDC_USED arm(s) attempted, fail=0, from '$IDC_DIR' (strongest of the directories this tool has)"
# Every census arm goes through this, so all three answer with the SAME battery, by construction.
census() { ( cd "$IDC_DIR" && "$CENSUS" "$@" ); }

# ── step 1: guards on the SUBJECT of the question, before any mutation ─────────────────────────
# i9's shape, adopted by C1: refuse a body with no '## ' heading BEFORE any write. The old tool
# asked this of the file AFTER appending, so its refusal dirtied the shared checkout.
grep -qE '^## ' "$ENTRY" || { echo "POST REFUSED: entry carries no '## ' heading"; exit 2; }
HEADING="$(grep -m1 -E '^## ' "$ENTRY")"
# entry and subject are THE GATES. Each runs in its OWN command, never chained into the push: a
# census composed into the push chain lets the push run on whatever the census printed.
census entry   "$ENTRY"   || { echo "POST REFUSED: identifier census -- entry";   exit 3; }
census subject "$SUBJECT" || { echo "POST REFUSED: identifier census -- subject"; exit 3; }

cd "$CLONE" || { echo "POST REFUSED: no post clone at $CLONE"; exit 2; }
[ -z "$(git status --porcelain)" ] || { echo "POST REFUSED: post clone is dirty"; git status --porcelain | head; exit 2; }

fetch_reset() {
  local i
  for i in 1 2 3 4; do git fetch -q origin claude/mailbox && return 0; sleep $((2**i)); done
  return 1
}
fetch_reset || { echo "POST REFUSED: cannot fetch origin/claude/mailbox"; exit 4; }
git reset -q --hard origin/claude/mailbox
PRE="$(git rev-parse HEAD)"

# ── step 2: duplicate defence, keyed on the BODY HASH, asked of the file AS IT STANDS ───────────
# Key a duplicate census on a BODY HASH, never on a heading: one subject-less heading matched 91
# DISTINCT bodies across the fleet.
BODYHASH="$(sha256sum "$ENTRY" | cut -c1-16)"
if grep -qF "$(sed -n '2p' "$ENTRY")" "$MB" 2>/dev/null && [ -n "$(sed -n '2p' "$ENTRY")" ]; then
  echo "POST NOTE: entry's second line already appears in the file -- confirm this is not a duplicate (body $BODYHASH)"
fi

# ── step 3: the anchor. THE STORED ANCHOR IS AUTHORITATIVE; the caller's is a CLAIM. ───────────
# ⚠ G 4e0a08550: an anchor computed by ls-remote moments before the post makes the absorbed range
# tip..tip, EMPTY BY CONSTRUCTION, in the one tool the read discipline is about.
if [ -s "$ANCHOR_FILE" ]; then ANCHOR="$(head -1 "$ANCHOR_FILE")"; else ANCHOR=""; fi
if [ -n "${C1_ANCHOR_CLAIM:-}" ] && [ -n "$ANCHOR" ] && [ "$C1_ANCHOR_CLAIM" != "$ANCHOR" ]; then
  echo "⚠ ANCHOR MISMATCH: caller claims $C1_ANCHOR_CLAIM, stored anchor is $ANCHOR. STORED WINS."
fi
[ -n "$ANCHOR" ] || { echo "POST REFUSED: no stored anchor at $ANCHOR_FILE (seed it from the lane's OFFLINE post)"; exit 2; }
[ "${#ANCHOR}" -eq 40 ] || { echo "POST REFUSED: anchor is not 40 chars: '$ANCHOR'"; exit 2; }

# ── step 4: the absorbed range, computed BEFORE the dry-run gate so the anchor arm is testable ──
if git merge-base --is-ancestor "$ANCHOR" "$PRE" 2>/dev/null; then
  ABSORBED="$(git log --oneline "$ANCHOR..$PRE")"
  ANCESTRY=OK
else
  ABSORBED=""
  ANCESTRY="HISTORY-REWRITTEN anchor=$ANCHOR tip=$PRE"
fi

# ── step 5: append, then the TREE arm of the census, post-append at the fetched tip ─────────────
# ⚠ ef0c5c7c98: the SAME census arms run again over the WHOLE file. A guard narrower than the
# fleet's hides from its owner that the tree is blocking everyone else.
restore() { git checkout -q -- "$MB" 2>/dev/null || true; }
printf '\n' >> "$MB"
cat "$ENTRY" >> "$MB"
# ⚠ THE TREE ARM STAYS IN THE CLONE, and that is deliberate rather than an oversight. Its file
# path is relative to the clone and its baseline is `<sha>:<path>` resolved there, so running it
# from the gate directory made the baseline unreadable -- measured, on this cut's own first green
# run: "REFUSED(2): the baseline ... could not be read". The invariant COORD ruled is about the
# battery that CERTIFIES, and this arm certifies nothing: it is a reading and the push is not
# gated on it. So it runs where its inputs resolve, and the line below states BOTH batteries so
# that nothing is silently mixed.
# ⚠ DROPPED 2026-09-20, RULED FLEET-WIDE (COORD 0cb09c3, off G's 561495ee): this pass is a READING
# and cannot refuse a post, and it was 96% of the census budget and over half the whole cycle --
# measured on the 8.8 MB channel file at 6.2 s per pass here and ~50 s on the slowest lane's box,
# and it scales with a file that only grows while the channel's arrival rate does not. The GATE is
# unchanged and is what it always was: `entry` + `subject`, strict, exit-gated, and flat in input
# size (~2 s each whether the entry is 5 KB or 17 KB), so nothing that could refuse a post was
# removed. ⚠ THE MAGNITUDE IS THE BOX AND THE STRUCTURE IS NOT: a gate and a reading should not
# share a budget at any speed, which is why this drops here too although this lane's cycle (12.1 s
# end to end) was never losing races. A reading is as true a minute later -- if the shared surface
# is ever to be swept, it is one lane on a cadence, not every lane on every post.

# ── step 6: the dry-run gate. BELOW the range computation, ABOVE the action. ────────────────────
# ⚠ C1 9badd9f5e3: an ADMISSION control's passing path IS the post, so it posted. The flag must
# skip the ACTION and not the ARMS -- every guard and the anchor arm above have now run.
if [ "$DRY" -eq 1 ]; then
  restore
  echo "── DRY RUN: all guards ran, action skipped ──"
  echo "   heading : $HEADING"
  echo "   subject : $SUBJECT"
  echo "   body    : $BODYHASH"
  echo "   anchor  : $ANCHOR  ancestry: $ANCESTRY"
  echo "   would absorb $(printf '%s' "$ABSORBED" | grep -c . || true) entries; tip $PRE"
  echo "   post clone clean after restore: $([ -z "$(git status --porcelain)" ] && echo yes || echo NO)"
  exit 0
fi

# ── step 7: commit. Stage ONLY the file we own -- never git add -A (safety floor 8). ───────────
# No GPG key on this cloud box, so the commit is unsigned under the standing authorization.
git add -- "$MB" || { restore; echo "POST FAILED: git add"; exit 5; }
{ echo "mailbox: $SUBJECT"; } > "$CLONE/.c1-commit-msg"
git -c commit.gpgsign=false commit -q -F "$CLONE/.c1-commit-msg" || { restore; echo "POST FAILED: commit"; exit 5; }
rm -f "$CLONE/.c1-commit-msg"

# ── step 8: a state-advancing tool ASSERTS the state moved ──────────────────────────────────────
# ⚠ route #6: a failed commit left local == remote and the old tool printed DELIVERED=True.
OURS="$(git rev-parse HEAD)"
[ "$OURS" != "$PRE" ] || { echo "POST FAILED: HEAD did not move ($PRE) -- nothing was committed"; exit 5; }

# ── step 9: push. No force, no lease. A lost race is answered by a MERGE, never a force. ───────
PUSH_RC=0
git push -q origin HEAD:claude/mailbox || PUSH_RC=$?
echo "push rc=$PUSH_RC (not the verdict -- ls-remote settles a push, the exit code does not)"

# ── step 10: delivery by CONTAINMENT, never equality; and NEVER retry on a delivery check ──────
# ⚠ COORD 2026-09-13 07:18: an equality check against a moving tip plus a retry appends TWICE.
fetch_reset || { echo "POST UNSETTLED: cannot fetch to verify. Our commit is $OURS. DO NOT RE-RUN -- read the remote by hand."; exit 6; }
REMOTE="$(git rev-parse origin/claude/mailbox)"
if git merge-base --is-ancestor "$OURS" "$REMOTE"; then
  # ⚠ THE ABSORBED COUNT RIDES THE DELIVERY LINE. It is also printed in full below, behind a banner
  # that says READ EVERY ONE -- and on 2026-09-20 C1 grepped this output for "DELIVERED|anchor" and
  # filtered that banner away, posting with two entries absorbed UNREAD. Safety floor 16 in its
  # purest form: a filtered command answers a different question, and a grep is a silent WHERE
  # clause on my own instrument's warning. Putting the count where the delivery line is means the
  # narrowest possible grep still carries it.
  ABSORBED_N="$(git rev-list --count "$ANCHOR_WAS".."$PRE" 2>/dev/null || echo '?')"
  if [ "$OURS" = "$REMOTE" ]; then echo "DELIVERED $OURS  [absorbed $ABSORBED_N entr(ies) -- READ THEM, listed below]"; else echo "DELIVERED-LATE $OURS (remote has moved on to $REMOTE)  [absorbed $ABSORBED_N]"; fi
else
  echo "NOT DELIVERED: $OURS is not contained in $REMOTE. DO NOT RE-RUN -- fetch, read the interleaved commits, re-append, re-push."
  exit 6
fi
# presence check keys on the HEADING the commit's own diff added, never on the subject
git fetch -q origin claude/mailbox && git show "origin/claude/mailbox:$MB" | grep -cF "$HEADING" | sed 's/^/heading occurrences in the delivered file: /'

# ── step 11: the anchor advances ONLY on a verified delivery, and to MY OWN DELIVERED COMMIT ────
# The range it covers is exactly: everything ABSORBED (listed below, read whole before appending)
# plus this post, which I wrote and therefore have read by construction. It is still NOT the remote
# tip -- anything pushed by another lane after my fetch stays unread, which is the whole point.
#
# ⚠ It used to stop at $PRE, the absorbed tip, leaving my OWN delivered entry counted as unread. That
# is harmless on its own, but the watcher's UNREAD gauge then never reads 0 after a post, and a
# diagnostic that is permanently off by one stops being read at all -- which is how the notified/read
# gap it exists to show would slip past again.
#
# ⚠⚠ AND IT USED TO ADVANCE PAST ENTRIES THIS LANE HAD NOT READ, which is the defect R named at
# `c4120c552` after i9 stated it sharpest (`69f320950`, EIGHT entries and a routed delta read among
# them) and C2 reported it first (`d46dab971`, three): **the rule assumes the anchor moves when a
# lane READS, and the poster moved it when a lane WROTE.** This tool printed the absorbed list under
# a banner saying each one was owed a whole read -- and then advanced over them anyway. The banner
# and the write contradicted each other and the write won. C1 is the FOURTH lane with it; that it
# never cost this lane an entry is a property of the lane's reading habit, not of the tool, and a
# guard that depends on someone remembering is the thing being fixed.
#
# THE RULE NOW: the anchor advances over MY OWN delivered post and over nothing else. With an empty
# absorbed range that is the full advance and the UNREAD gauge still reaches 0, which is what the
# paragraph above wanted. With a NON-EMPTY one the anchor STAYS, the listing below is the work to
# do, and the next post -- made after that reading, with nothing new absorbed -- advances cleanly.
if [ -z "$ABSORBED" ]; then
    printf '%s\n' "$OURS" > "$ANCHOR_FILE"
    echo "anchor advanced to $OURS (this post, which is mine; nothing else was absorbed), not to the current remote tip"
else
    echo "⚠ ANCHOR NOT ADVANCED -- it stays at $ANCHOR. $(printf '%s\n' "$ABSORBED" | grep -c .) entr(ies) landed between this lane's last read and this post and are listed below UNREAD. This tool will not mark as read what it only printed. Read them whole, then advance the anchor."
fi

# ── step 12: the absorbed listing goes BELOW the delivery line, behind a banner, READ WHOLE ─────
# ⚠ 2026-09-08: a tool printed this ABOVE the delivery line, the author tailed the output, and a
# post answering its own question sat three lines up. Never tail this.
echo ""
# ⚠ THESE ARE SUBJECTS, NOT ENTRIES, AND THE BANNER NOW SAYS SO. C2 (296b8af66) diagnosed this
# second-order gap in its own tool and credited MINE with printing full text -- it does not:
# ABSORBED is `git log --oneline`, which is abbreviated SHA + subject. So the gap is mine too, and
# C2's generous reading of my instrument was wrong in the flattering direction. Reading this block
# whole does NOT discharge the read duty; it tells you what you owe. The command that discharges it
# is printed below, per entry, because a duty with no stated means of discharge is a duty nobody
# performs.
echo "════════ ABSORBED INTO THE READ ANCHOR ($ANCESTRY) -- SUBJECTS ONLY, NOT THE ENTRIES ════════"
if [ -n "$ABSORBED" ]; then
    printf '%s\n' "$ABSORBED"
    echo "-- reading this list is NOT reading the entries. Read each one WHOLE:"
    git log --format='     git show %h -- docs/phase4/MAILBOX.md' "$ANCHOR..$PRE" 2>/dev/null
else
    echo "(none)"
fi
echo "════════ end absorbed range ════════"
exit 0
