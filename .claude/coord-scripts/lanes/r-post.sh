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
# Paths are ENVIRONMENT-DERIVED with defaults, so this file carries no account, host or profile path.
#   R_MAILBOX_CLONE  the dedicated single-branch mailbox clone (fetch.unpackLimit=1, negative refspec)
#   R_POST_STATE     where the read anchor and the body-hash ledger live; defaults to this script's
#                    own directory, which is where they sit when it runs from its working home
CLONE="${R_MAILBOX_CLONE:-/c/go2cs-tmp/mailbox-r}"
FILE="docs/phase4/MAILBOX.md"
BRANCH="claude/mailbox"
SP="$(dirname "$(readlink -f "$0")")"
STATE="${R_POST_STATE:-$SP}"

# ⚠ REFUSE rather than relocate, when the default would write state INTO a repository (exit 14).
# Found by C2 (ad1560d2f0) on its own tool from R's e82b16d6d, and LATENT HERE TOO: the default puts
# the read anchor and the body-hash ledger in this script's own directory, which is correct while the
# only copy lives outside a tree and becomes WRONG the moment the file is published inside one --
# the anchor most of all, since the fleet's read discipline hangs off it.
# ⚠ AND MY OWN CONTROLS COULD NOT REACH IT: the writes are on the LIVE path and every control runs
# --dry-run, which exits first. A `git status` clean after a dry run says nothing about this.
# The remedy is C2's shape and the reason is C2's: silently relocating the default answers THIS
# instance and not the next, so the script fails CLOSED and names the variable instead.
if [ -z "${R_POST_STATE:-}" ] && git -C "$SP" rev-parse --is-inside-work-tree >/dev/null 2>&1; then
  echo "REFUSED(14): this copy sits inside a git work tree and R_POST_STATE is unset, so the read"
  echo "             anchor and the body-hash ledger would be written into the repository at:"
  echo "               $SP"
  echo "             Set R_POST_STATE to a path outside the tree and re-run."
  exit 14
fi
ANCHOR="$STATE/r-anchor.txt"
LEDGER="$STATE/r-post-bodyhashes.txt"
DRY=0; [ "${3:-}" = "--dry-run" ] && DRY=1
# ⚠ --bar-check exists because the control bar's NEGATIVE arm could not be tested safely (2026-09-20).
# The bar lives on the LIVE path by design, so proving it FIRES is safe (the run refuses and nothing
# posts) while proving it does NOT fire meant running to completion -- and not-firing IS posting. I
# ran that arm and put a junk entry on the channel, the second live-path test to do so tonight. This
# mode evaluates the bar against the heading and EXITS: 13 if it would refuse, 0 if it would pass,
# touching nothing. A guard whose negative arm can only be exercised by doing the dangerous thing
# needs a door, not more care.
BARCHECK=0; [ "${3:-}" = "--bar-check" ] && BARCHECK=1

# The anchor-advance decision, as ONE definition consulted by the live path and by its arms.
# ADVANCE only when nothing landed between the stored anchor and the tip this post appends to.
anchor_may_advance() {
  [ -z "$1" ] || [ "$1" = "$2" ]
}

# --anchor-check <prev-anchor> <pre-tip> evaluates that decision and EXITS, touching nothing —
# the same door --bar-check opens for the control bar, and for the same reason: the branch lives on
# the live path, and proving it HOLDS must not require making a real post to prove it.
if [ "${1:-}" = "--anchor-check" ]; then
  if anchor_may_advance "${2:-}" "${3:-}"; then echo "ANCHOR-CHECK: ADVANCE"; exit 0
  else echo "ANCHOR-CHECK: HOLD -- unread entries stand between the anchor and this post"; exit 20; fi
fi

# --- step 1: resolve BEFORE any cd (SKILL: relative entry path resolved to nothing after the cd)
ENTRY="$(readlink -f "${1:?entry file}")"
SUBJ="$(readlink -f "${2:?subject file}")"
[ -s "$ENTRY" ] || { echo "REFUSED(2): entry file empty or unreadable: $ENTRY"; exit 2; }
[ -s "$SUBJ" ]  || { echo "REFUSED(2): subject file empty or unreadable: $SUBJ"; exit 2; }
HEADING="$(grep -m1 '^## ' "$ENTRY" || true)"
[ -n "$HEADING" ] || { echo "REFUSED(3): entry has no '## ' heading line"; exit 3; }
# The bar's predicate, evaluated and reported WITHOUT proceeding. Same expression as the live bar
# below -- one definition, consulted twice, so the check cannot drift from the thing it checks.
# ⚠ `probe` is WORD-BOUNDED (with its inflections named) because the bare substring refused a real
# post on `probe_package` -- a FIXTURE'S PACKAGE NAME, quoted inside a compiler error in the heading.
# `_` is a word character, so `\bprobe(s|d)?\b` frees `probe_package` while still refusing `probe`,
# `probes` and `probed`. ⚠ The sentence DESCRIBING that false positive reproduced it, which is how a
# substring guard on prose behaves: three refusals in one post, one of them real.
# `plant` stays a substring deliberately -- `planted`/`planting` are the words a control heading
# actually uses, and no ordinary word carries it (transplant/supplant are not heading words here).
barmatch() { printf '%s' "$1" | tr 'A-Z' 'a-z' | grep -qE '\[ctl\]|census control|admission control|scratch test|self-test|plant|\bprobe(s|d)?\b'; }
if [ "$BARCHECK" -eq 1 ]; then
  if barmatch "$HEADING"; then echo "BAR-CHECK: WOULD REFUSE (13) -- heading reads as a control"; exit 13
  else echo "BAR-CHECK: would pass the control bar (0) -- nothing touched"; exit 0; fi
fi
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
#
# ⚠ EVERY TOKEN, NOT THE FIRST. This planted only `grep -m1 . "$TOKENS"` until 2026-09-20, which made
# the assertion INVARIANT under a degraded token set: with one of the four environment variables unset
# in the environment the tool inherits, the surviving first token is planted, fires, and the line
# printed is WORD-FOR-WORD the healthy one. The count was printed (`tokens=N`) and nothing asserted
# it, so a census 25% blind announced itself as sound. Found by applying C1's `d7f8f842a` -- a
# self-test figure invariant under a 90% truncation of its input -- to this tool rather than reading
# it as someone else's finding; the shape transferred, the two ruled lines did not (this tool
# materialises nothing, and its failure already REFUSES).
#
# The assertion now scales with the instrument: N tokens planted, N fires required, and the COUNT is
# printed beside the verdict so a degraded set is visible in the output rather than derivable from it.
selftest() {
  local plant="$STATE/.r-census-plant.tmp" tok rc planted=0
  [ -s "$TOKENS" ] || { echo "  SELF-TEST FAILED: no token to plant"; return 1; }

  while IFS= read -r tok; do
    [ -n "$tok" ] || continue
    printf '## plant\n\nleading text %s trailing text\n' "$tok" > "$plant"
    census "$plant" >/dev/null 2>&1; rc=$?
    rm -f "$plant" "$plant.lc.tmp"
    [ "$rc" -eq 1 ] || { echo "  SELF-TEST FAILED: a planted token did NOT make the census fire (rc=$rc)"; return 1; }
    planted=$((planted + 1))
  done < "$TOKENS"

  [ "$planted" -eq "$(grep -c . "$TOKENS")" ] || { echo "  SELF-TEST FAILED: planted $planted of $(grep -c . "$TOKENS") tokens"; return 1; }
  echo "  self-test: $planted of $planted planted tokens DETECTED -- the census can fire on this box"
}
echo "== SECURITY CENSUS (own step, before any mutation) =="
selftest || { echo "REFUSED(9): census self-test failed -- refusing to trust any clean verdict"; rm -f "$TOKENS"; exit 9; }
census "$ENTRY"; CRC=$?
[ "$CRC" -eq 0 ] || { echo "REFUSED(9): entry fails the identifier census (rc=$CRC)"; rm -f "$TOKENS"; exit 9; }

# --- step 2: the clone
cd "$CLONE" || { echo "REFUSED: cannot cd to mailbox clone"; rm -f "$TOKENS"; exit 2; }
[ -z "$(git status --porcelain)" ] || { echo "REFUSED(7): mailbox checkout is dirty before append"; git status --porcelain; rm -f "$TOKENS"; exit 7; }
# ⚠⚠ THE FETCH AND THE MERGE ARE GATED ON THEIR OWN EXIT STATUS, and the file the next two checks
# READ is asserted real before either verdict is believed. i9's near-miss (mailbox f80a0436a §1) is
# the reason and it was one command from a 14,209-file deletion: a fetch failed with
# `invalid index-pack output`, the read-tree after it left an index holding ONE entry, `write-tree`
# happily serialised that into a well-formed tree, and the zero-deletions gate read `0` from a diff
# that had itself died with `fatal: bad object`. **A count taken from a failed command is not a
# measurement.** What caught it was the fatal lines sitting on screen beside the zero, not the gate.
#
# Here the same shape runs the other way and is quieter: both commands below had their output
# discarded and their status unchecked, so a failed fetch or a refused fast-forward left a STALE
# working tree — and the duplicate-heading check that follows reads that tree. A miss there does not
# refuse; it APPENDS A DUPLICATE. The check fails OPEN, which is the direction that costs something.
if ! git fetch origin "$BRANCH" >/dev/null 2>&1; then
  echo "REFUSED(15): fetch of origin/$BRANCH failed -- every check below reads a stale tree"; rm -f "$TOKENS"; exit 15; fi
if ! git merge --ff-only "origin/$BRANCH" >/dev/null 2>&1; then
  echo "REFUSED(15): fast-forward to origin/$BRANCH failed -- the checkout is not at the tip"; rm -f "$TOKENS"; exit 15; fi
PRE_TIP="$(git rev-parse HEAD)"
PREV_ANCHOR="$(cat "$ANCHOR" 2>/dev/null || true)"
if [ -n "$PREV_ANCHOR" ] && ! git merge-base --is-ancestor "$PREV_ANCHOR" HEAD; then
  echo "REFUSED(8): HISTORY REWRITTEN -- anchor $PREV_ANCHOR is NOT an ancestor of tip $PRE_TIP"; rm -f "$TOKENS"; exit 8; fi
# The SUBJECT of the duplicate check, asserted before its verdict is read: an absent or truncated
# mailbox file makes `grep -Fqx` find nothing, which reads as "no duplicate" and appends.
MAILBOX_LINES="$(wc -l < "$FILE" 2>/dev/null || echo 0)"
if [ "$MAILBOX_LINES" -lt 1000 ]; then
  echo "REFUSED(15): $FILE holds $MAILBOX_LINES lines -- the duplicate check would read nothing and pass"; rm -f "$TOKENS"; exit 15; fi
if grep -Fqx "$HEADING" "$FILE"; then
  echo "REFUSED(5): that exact heading is already in the file at origin"; rm -f "$TOKENS"; exit 5; fi

if [ "$DRY" -eq 1 ]; then
  echo "DRY-RUN: all admission arms passed; nothing appended, nothing pushed."; rm -f "$TOKENS"; exit 0; fi

# --- STRUCTURAL BAR (added 2026-09-19 after ce1744f8d): a CONTROL can never take the live path.
# The defect was not that the census was weak -- it was that a control run without --dry-run reaches
# git push at all. Ordering-by-care already failed here, so the bar lives in the script: any entry
# whose heading marks it a control/probe/plant/test is REFUSED on the live path, unconditionally.
# ⚠ THE PREDICATE IS A MARKER, NOT A WORD -- narrowed 2026-09-20 after it refused a legitimate post
# whose heading merely SAID "my controls could not reach it". The bare word `control ` is ordinary
# prose in this fleet and a guard that cannot be written about without tripping itself is C2's A3
# (f82130834d) one tier over: the quotation-versus-marker hazard. So a control entry CARRIES `[CTL]`
# in its heading and the bar keys on that, with the narrow self-describing phrases kept beside it.
# ⚠ AND `--dry-run` CANNOT PREVIEW THIS BAR: it sits after the dry-run exit BY DESIGN, since
# dry-running a control is exactly what the bar tells you to do. A clean dry run is therefore not a
# faithful preview of the live path -- this bar and the state writes are live-only.
if barmatch "$HEADING"; then
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

# ⚠⚠ THE ANCHOR IS NOT ADVANCED PAST AN ENTRY THIS LANE HAS NOT READ. Until 2026-09-20 this wrote
# $REMOTE_TIP unconditionally, so POSTING marked every entry that had landed since the last read as
# read -- the banner above said they were owed and the next line recorded them as discharged. The two
# contradicted each other and the write won.
#
# THREE LANES, ONE TOOL SHAPE: C2 (d46dab971) swept three entries this way, i9 (69f320950) swept
# EIGHT -- 564 lines, including a delta read routed to it, which is how a ruled item went missing --
# and I did it by hand from the other direction the same day, setting the anchor to the remote tip
# rather than to the last entry I had read, and rolling it back when the diff showed one skipped.
# ⚠ It defeats "read every entry WHOLE" STRUCTURALLY rather than by anyone skipping something: the
# rule assumes the anchor moves only when a lane reads, and the poster moved it when a lane wrote.
#
# So: advance to the post's own tip ONLY when nothing landed between the stored anchor and the
# pre-append tip. Otherwise the anchor STAYS, the owed range above is what discharges it, and the
# lane advances it after reading. A post never claims a read.
if anchor_may_advance "$PREV_ANCHOR" "$PRE_TIP"; then
  printf '%s\n' "$REMOTE_TIP" > "$ANCHOR"
  echo "anchor advanced to $REMOTE_TIP (nothing landed between the stored anchor and this post)"
else
  echo "ANCHOR NOT ADVANCED -- it stays at $PREV_ANCHOR."
  echo "  Entries landed between your anchor and this post; they are listed above and are UNREAD."
  echo "  Read each one WHOLE, then advance the anchor yourself. This post is NOT a read."
fi
rm -f "$TOKENS"
