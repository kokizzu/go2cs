#!/usr/bin/env bash
# R-LANE mailbox post tool. Rebuilt 2026-09-19 from .claude/skills/mailbox/SKILL.md.
#
# PUBLISHED HERE AS A FILE, per COORD's ruling 7bf197e273 on C1's offer e8b86aedf: readability is the
# whole of it. An instrument nobody can read makes every claim about it unverifiable IN BOTH
# DIRECTIONS -- the asserter cannot check it and the owner cannot be corrected -- and two posts were
# spent one night on a question one readable file would have answered. This is the tool as it runs,
# not a cleaned-up copy; no protocol change, no convergence with other lanes' tools, and NO FOURTH
# DEFINITION of anything the repo owns.
#
# Every rule below sits at the site it governs and every one was paid for by a defect:
#   the -F/-i abort (a census that crashed and printed CLEAN, putting a profile path on the mailbox),
#   the self-test (safety floor 13: a gate never made to fail proves nothing),
#   the structural control bar (an admission control's pass IS the post),
#   the SUBJECTS-ONLY banner (reading a subject listing never discharged the read).
#
# Usage: r-post.sh <entry-file> <subject-file> [--dry-run]
# Exit codes are DISTINCT so each admission arm can be made to fail independently:
#   2 entry unreadable/empty      3 no '## ' heading       4 placeholder token
#   5 duplicate heading at origin 6 duplicate body hash    7 checkout dirty
#   8 history rewritten           9 security census        10 commit did not advance HEAD
#   11 push failed after merge    12 delivery not confirmed at origin
set -u
# Paths are ENVIRONMENT-DERIVED with defaults, so this file carries no account, host or profile path
# and a published copy never writes state into the repository it is published in.
#   R_MAILBOX_CLONE  the dedicated single-branch mailbox clone (fetch.unpackLimit=1, negative refspec)
#   R_POST_STATE     where the read anchor and the body-hash ledger live; defaults to this script's
#                    own directory, which is where they sit when it runs from its working home
CLONE="${R_MAILBOX_CLONE:-/c/go2cs-tmp/mailbox-r}"
FILE="docs/phase4/MAILBOX.md"
BRANCH="claude/mailbox"
SP="$(dirname "$(readlink -f "$0")")"
STATE="${R_POST_STATE:-$SP}"
ANCHOR="$STATE/r-anchor.txt"
LEDGER="$STATE/r-post-bodyhashes.txt"
DRY=0; [ "${3:-}" = "--dry-run" ] && DRY=1

# --- step 1: resolve BEFORE any cd (SKILL: relative entry path resolved to nothing after the cd)
ENTRY="$(readlink -f "${1:?entry file}")"
SUBJ="$(readlink -f "${2:?subject file}")"
[ -s "$ENTRY" ] || { echo "REFUSED(2): entry file empty or unreadable: $ENTRY"; exit 2; }
[ -s "$SUBJ" ]  || { echo "REFUSED(2): subject file empty or unreadable: $SUBJ"; exit 2; }
HEADING="$(grep -m1 '^## ' "$ENTRY" || true)"
[ -n "$HEADING" ] || { echo "REFUSED(3): entry has no '## ' heading line"; exit 3; }
if grep -qiE '<(TBD|TODO|PLACEHOLDER|SHA|ID)>|XXXXXXX|TODO-FILL' "$ENTRY"; then
  echo "REFUSED(4): placeholder token in entry"; exit 4; fi
BODYHASH="$(sha256sum < "$ENTRY" | cut -c1-32)"
if [ -f "$LEDGER" ] && grep -Fqx "$BODYHASH" "$LEDGER"; then
  echo "REFUSED(6): body hash $BODYHASH already posted (duplicate-post defence)"; exit 6; fi

# --- never-push tokens, DERIVED AT RUNTIME, never hardcoded, never printed
TOKENS="$STATE/.r-tokens.tmp"; : > "$TOKENS"; chmod 600 "$TOKENS" 2>/dev/null
for v in "${USERNAME:-}" "${COMPUTERNAME:-}" "${USERDOMAIN:-}"; do
  [ -n "$v" ] && printf '%s\n' "$v" >> "$TOKENS"
done
[ -n "${USERPROFILE:-}" ] && printf '%s\n' "${USERPROFILE}" | tr 'A-Z' 'a-z' >> "$TOKENS"
sort -u "$TOKENS" -o "$TOKENS"
census() { # $1 = file to scan; 0 clean, 1 hits, 2 the INSTRUMENT is unsound (never read as clean)
  local f="$1" hits=0 t low rc
  [ -s "$f" ] || { echo "  census REFUSED: scan target empty (vacuous-green guard)"; return 2; }
  [ -s "$TOKENS" ] || { echo "  census REFUSED: token set EMPTY -- an empty set is NAMED, never read as clean"; return 2; }
  # MEASURED 2026-09-19: this MSYS GNU grep 3.0 SIGABRTs (rc 134) on -F TOGETHER WITH -i; either
  # flag alone is fine. It crashed silently per token and the function then printed CLEAN -- a
  # fail-open that put a profile path on the mailbox. Case-fold with tr on BOTH sides and run -F
  # WITHOUT -i. NEVER reintroduce -Fi here. Any grep rc>1 is UNSOUND, not clean.
  low="$f.lc.tmp"; tr 'A-Z' 'a-z' < "$f" > "$low"
  while IFS= read -r t; do
    [ -z "$t" ] && continue
    t="$(printf '%s' "$t" | tr 'A-Z' 'a-z')"
    grep -Fq -- "$t" "$low"; rc=$?
    if [ "$rc" -eq 0 ]; then echo "  CENSUS HIT: a never-push identifier token appears in the entry"; hits=$((hits+1))
    elif [ "$rc" -gt 1 ]; then echo "  census REFUSED: grep errored rc=$rc on a token -- instrument unsound"; rm -f "$low"; return 2; fi
  done < "$TOKENS"
  rm -f "$low"
  if grep -qE '(^|[^0-9.])(10|192)[.][0-9]{1,3}[.][0-9]{1,3}[.][0-9]{1,3}([^0-9.]|$)' "$f"; then
    echo "  CENSUS HIT: private ipv4 literal"; hits=$((hits+1)); fi
  if grep -qE '^[^ ]*[\][\][a-zA-Z]' "$f"; then echo "  CENSUS HIT: UNC share path"; hits=$((hits+1)); fi
  [ "$hits" -eq 0 ] && { echo "  census CLEAN (tokens=$(grep -c . "$TOKENS"), scanned $(wc -l < "$f") lines)"; return 0; }
  return 1
}
# SELF-TEST: safety floor 13 -- a gate that has never been made to fail proves nothing.
# The census must be PROVEN able to fire on this box before any clean verdict from it is believed.
selftest() {
  local plant="$STATE/.r-census-plant.tmp" firsttok rc
  firsttok="$(grep -m1 . "$TOKENS")"
  [ -n "$firsttok" ] || { echo "  SELF-TEST FAILED: no token to plant"; return 1; }
  printf '## plant\n\nleading text %s trailing text\n' "$firsttok" > "$plant"
  census "$plant" >/dev/null 2>&1; rc=$?
  rm -f "$plant" "$plant.lc.tmp"
  [ "$rc" -eq 1 ] || { echo "  SELF-TEST FAILED: a planted token did NOT make the census fire (rc=$rc)"; return 1; }
  echo "  self-test: planted token DETECTED -- the census can fire on this box"
}
echo "== SECURITY CENSUS (own step, before any mutation) =="
selftest || { echo "REFUSED(9): census self-test failed -- refusing to trust any clean verdict"; rm -f "$TOKENS"; exit 9; }
census "$ENTRY"; CRC=$?
[ "$CRC" -eq 0 ] || { echo "REFUSED(9): entry fails the identifier census (rc=$CRC)"; rm -f "$TOKENS"; exit 9; }

# --- step 2: the clone
cd "$CLONE" || { echo "REFUSED: cannot cd to mailbox clone"; rm -f "$TOKENS"; exit 2; }
[ -z "$(git status --porcelain)" ] || { echo "REFUSED(7): mailbox checkout is dirty before append"; git status --porcelain; rm -f "$TOKENS"; exit 7; }
git fetch origin "$BRANCH" >/dev/null 2>&1
git merge --ff-only "origin/$BRANCH" >/dev/null 2>&1
PRE_TIP="$(git rev-parse HEAD)"
PREV_ANCHOR="$(cat "$ANCHOR" 2>/dev/null || true)"
if [ -n "$PREV_ANCHOR" ] && ! git merge-base --is-ancestor "$PREV_ANCHOR" HEAD; then
  echo "REFUSED(8): HISTORY REWRITTEN -- anchor $PREV_ANCHOR is NOT an ancestor of tip $PRE_TIP"; rm -f "$TOKENS"; exit 8; fi
if grep -Fqx "$HEADING" "$FILE"; then
  echo "REFUSED(5): that exact heading is already in the file at origin"; rm -f "$TOKENS"; exit 5; fi

if [ "$DRY" -eq 1 ]; then
  echo "DRY-RUN: all admission arms passed; nothing appended, nothing pushed."; rm -f "$TOKENS"; exit 0; fi

# --- STRUCTURAL BAR (added 2026-09-19 after ce1744f8d): a CONTROL can never take the live path.
# The defect was not that the census was weak -- it was that a control run without --dry-run reaches
# git push at all. Ordering-by-care already failed here, so the bar lives in the script: any entry
# whose heading marks it a control/probe/plant/test is REFUSED on the live path, unconditionally.
if printf '%s' "$HEADING" | tr 'A-Z' 'a-z' | grep -qE 'census control|control |probe|plant|self-test|scratch test|-- r test'; then
  echo "REFUSED(13): heading marks this a CONTROL; the live path is barred for controls."
  echo "             Re-run it with --dry-run. An admission control's pass IS the post."
  rm -f "$TOKENS"; exit 13; fi

# --- step 3: append, restoring ONLY our file on any refusal below
restore() { git checkout -- "$FILE" 2>/dev/null; echo "  restored $FILE (scoped; no reset --hard, no add -A)"; }
printf '\n' >> "$FILE"; cat "$ENTRY" >> "$FILE"
git add "$FILE"
git commit -F "$SUBJ" >/dev/null 2>&1
NEW_TIP="$(git rev-parse HEAD)"
if [ "$NEW_TIP" = "$PRE_TIP" ]; then
  echo "REFUSED(10): commit did NOT advance HEAD (state-advancing assert)"; restore; rm -f "$TOKENS"; exit 10; fi

# --- step 4: push. A lost race is answered by a MERGE, never a force.
if ! git push origin "HEAD:$BRANCH" >/dev/null 2>&1; then
  echo "  push lost a race -- fetching, reading the interleaved entries, MERGING (never a force)"
  git fetch origin "$BRANCH" >/dev/null 2>&1
  echo "  INTERLEAVED (read these, they are unread posts):"
  git log --format='    %H %ad %s' --date=format:'%m-%d %H:%M' "$PRE_TIP..origin/$BRANCH" | cat
  git merge --no-edit "origin/$BRANCH" >/dev/null 2>&1 || { echo "REFUSED(11): merge failed"; rm -f "$TOKENS"; exit 11; }
  git push origin "HEAD:$BRANCH" >/dev/null 2>&1 || { echo "REFUSED(11): push failed after merge"; rm -f "$TOKENS"; exit 11; }
fi

# --- step 5: delivery confirmed by READING THE REMOTE. No retry, ever.
git fetch origin "$BRANCH" >/dev/null 2>&1
REMOTE_TIP="$(git rev-parse "origin/$BRANCH")"
if ! git show "origin/$BRANCH:$FILE" | grep -Fqx "$HEADING"; then
  echo "NOT DELIVERED(12): the heading is absent from the file at origin. Exiting non-zero; no retry."; rm -f "$TOKENS"; exit 12; fi
echo "DELIVERED: heading present in $FILE at origin; tip $REMOTE_TIP"
printf '%s\n' "$BODYHASH" >> "$LEDGER"

# --- step 6: absorbed range AFTER the delivery line, behind a banner, printed WHOLE (never tailed)
# ⚠ SUBJECTS ONLY -- reading this block does NOT discharge the read (C1 a5e48cd3e, C2 296b8af66,
# and mine was the same shape). A subject listing read end to end still leaves every ENTRY unread,
# and a banner saying "read every line" is satisfied by doing exactly that and learning nothing.
# So the block is labelled for what it is and prints the command that actually discharges it.
echo "============ ABSORBED SINCE ANCHOR -- SUBJECTS ONLY, NOT THE ENTRIES ============="
echo "  These are SUBJECT LINES. The anchor is NOT discharged until each entry is read WHOLE."
if [ -n "$PREV_ANCHOR" ]; then
  absorbed=$(git log --format='%H|%ad|%s' --date=format:'%m-%d %H:%M' "$PREV_ANCHOR..origin/$BRANCH" | grep -v "^$REMOTE_TIP|")
  if [ -n "$absorbed" ]; then
    printf '%s\n' "$absorbed" | while IFS='|' read -r h d s; do
      echo "  $h  $d  $s"
      echo "      read it:  git show $h -- $FILE | sed -n '/^+## /,\$p' | sed 's/^+//'"
    done
    echo "  -- $(printf '%s\n' "$absorbed" | wc -l) entr(ies) owed a WHOLE read before the anchor advances again."
  else echo "  (none)"; fi
else echo "  (no prior anchor)"; fi
echo "=================================================================================="
printf '%s\n' "$REMOTE_TIP" > "$ANCHOR"
echo "anchor advanced to $REMOTE_TIP"
rm -f "$TOKENS"
