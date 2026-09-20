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
CENSUS_DIR="${TMPDIR:-/tmp}/c1-census-from-master"
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
git -C "$CENSUS_CLONE" fetch origin master --quiet 2>/dev/null || true
CENSUS_REF="$(git -C "$CENSUS_CLONE" rev-parse --short origin/master 2>/dev/null)"
mkdir -p "$CENSUS_DIR"
for f in coord-identifier-census.sh coord-identifier-patterns.txt coord-identifier-hashes.txt; do
    git -C "$CENSUS_CLONE" show "origin/master:.claude/coord-scripts/$f" > "$CENSUS_DIR/$f" 2>/dev/null \
      || { echo "POST REFUSED: cannot materialise $f from origin/master"; exit 2; }
    [ -s "$CENSUS_DIR/$f" ] || { echo "POST REFUSED: $f materialised EMPTY"; exit 2; }
done
# A truncated blob is the failure this assert exists for: a short census still RUNS and still exits
# 0 on a body it never finished scanning, which is a green over nothing of exactly tonight's class.
CENSUS_LINES="$(wc -l < "$CENSUS")"
[ "$CENSUS_LINES" -ge 1000 ] || { echo "POST REFUSED: census materialised at only $CENSUS_LINES lines"; exit 2; }
chmod +x "$CENSUS"
echo "census: origin/master $CENSUS_REF ($CENSUS_LINES lines)"

# ── step 1: guards on the SUBJECT of the question, before any mutation ─────────────────────────
# i9's shape, adopted by C1: refuse a body with no '## ' heading BEFORE any write. The old tool
# asked this of the file AFTER appending, so its refusal dirtied the shared checkout.
grep -qE '^## ' "$ENTRY" || { echo "POST REFUSED: entry carries no '## ' heading"; exit 2; }
HEADING="$(grep -m1 -E '^## ' "$ENTRY")"
# entry and subject are THE GATES. Each runs in its OWN command, never chained into the push: a
# census composed into the push chain lets the push run on whatever the census printed.
"$CENSUS" entry   "$ENTRY"   || { echo "POST REFUSED: identifier census -- entry";   exit 3; }
"$CENSUS" subject "$SUBJECT" || { echo "POST REFUSED: identifier census -- subject"; exit 3; }

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
# ⚠ TREE MODE IS A READING, NOT A GATE, AND ITS BASELINE IS THE TIP JUST FETCHED -- never the
# stored anchor. C2 measured the same bytes reading added=4/REFUSED against its last-read sha and
# added=0/CLEAN against the fresh tip; the four were other lanes' entries landed in the interval. A
# pre-existing hit on a shared surface is not this post's to fix and not this post's to be blocked by.
TREE_RC=0
"$CENSUS" tree "$MB" "$PRE" || TREE_RC=$?
echo "tree arm: rc=$TREE_RC -- a READING; the push is NOT gated on it"

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
printf '%s\n' "$OURS" > "$ANCHOR_FILE"
echo "anchor advanced to $OURS (absorbed $PRE + this post, which is mine), not to the current remote tip"

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
