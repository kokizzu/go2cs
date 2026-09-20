#!/usr/bin/env bash
# ARM: no repo outside the doors is written to OR FETCHED INTO by a post-tool run.
#
# ⚠ C1 66c860cb9 §4 adds the half G's rule did not cover: "a door must cover every path the tool
# WRITES OR FETCHES INTO, and the list is enumerated from the script rather than from memory."
# C1's tool force-fetches origin/master into the LANE'S MAIN WORKING CHECKOUT on every run, dry ones
# included, 217 lines above its dry-run gate. This arm answers that question for R by measurement.
#
# ⚠⚠ AND IT CARRIES ITS OWN TRAP, paid for on the first run: my first fingerprint spanned a run that
# exited at rc 6 (the body-hash refusal), which is ABOVE every git step -- so it measured a run that
# never touched a repo and read UNTOUCHED. That is C2's 4f29a8742 §2 for the third time in one day.
# The arm therefore ASSERTS the run reached step 3 before it reads its own verdict: a fingerprint
# across a run that exited above the subject is not evidence about the subject.
#
# Usage: r-post-doorscope-arm.sh <tool> [repo-that-must-not-move]
set -u
TOOL="$(readlink -f "${1:?usage: r-post-doorscope-arm.sh <tool> [repo]}")"
MAIN="${2:-/c/Projects/go2cs}"
ROOT="${3:-/c/go2cs-tmp/r-doorscope-arm}"
fails=0
ok(){ echo "  PASS  $1"; }
no(){ echo "  FAIL  $1"; fails=$((fails+1)); }
fp(){ git -C "$1" for-each-ref --format='%(refname) %(objectname)' | sha256sum | cut -d' ' -f1; }
st(){ git -C "$1" status --porcelain | sha256sum | cut -d' ' -f1; }

rm -rf "$ROOT"; mkdir -p "$ROOT/state" || exit 1
printf '## DOORSCOPE FIXTURE %s -- never posted\n\nbody unique to this run.\n' "$(date +%s%N)" > "$ROOT/entry.md"
printf 'doorscope fixture\n' > "$ROOT/subj.txt"

echo "=== ARM: the door's SCOPE -- every write AND every fetch ==="

# (a) ENUMERATED FROM THE SCRIPT, not from memory: no force refspec anywhere.
if grep -n 'fetch .*+.*:' "$TOOL" | grep -qv '^[[:space:]]*#'; then
  no "a FORCE refspec appears in a fetch"
else
  ok "no force refspec in any fetch"
fi

# (b) the fingerprint instrument must be able to see a change AT ALL (positive control)
CTL_B="$(fp "$MAIN")"
git -C "$MAIN" update-ref refs/heads/r-doorscope-ctl HEAD 2>/dev/null
CTL_A="$(fp "$MAIN")"
git -C "$MAIN" update-ref -d refs/heads/r-doorscope-ctl 2>/dev/null
CTL_R="$(fp "$MAIN")"
[ "$CTL_B" != "$CTL_A" ] && ok "control: the fingerprint CHANGES when a ref moves" \
                         || no "control DEAD: the fingerprint cannot see a ref move"
[ "$CTL_B" = "$CTL_R" ] && ok "control restored (post-condition, not assumed)" \
                        || no "control NOT restored -- $MAIN still carries the probe ref"

# (c) the measurement
R_BEFORE="$(fp "$MAIN")"; S_BEFORE="$(st "$MAIN")"
OUT="$(R_POST_STATE="$ROOT/state" bash "$TOOL" "$ROOT/entry.md" "$ROOT/subj.txt" --dry-run 2>&1)"; RC=$?
R_AFTER="$(fp "$MAIN")"; S_AFTER="$(st "$MAIN")"

# ⚠ THE SUBJECT MUST HAVE EXECUTED. Without this the next two assertions are vacuous.
# ⚠⚠ KEYED ON THE rc, NOT ON THE WORDING. This read `grep -q 'all admission arms passed'` until
# 2026-09-20, and changing that very sentence in the tool -- it overclaimed, since the control bar
# sits below the dry-run exit -- turned this arm RED on a run that had reached step 3 perfectly
# well. An arm keyed on a verdict's WORDING is coupled to the wording. rc 0 is structural here:
# the dry-run exit is the tool's ONLY zero-exit path and it sits below the fetch and the
# fast-forward. `DRY-RUN:` is kept as a marker beside it, never as the claim.
if [ "$RC" -eq 0 ] && echo "$OUT" | grep -q 'DRY-RUN:'; then
  ok "the run REACHED step 3 -- past the fetch and the fast-forward"
else
  no "the run exited ABOVE the git steps (rc=$RC) -- the verdicts below measure nothing"
fi
[ "$R_BEFORE" = "$R_AFTER" ] && ok "no ref in $MAIN moved" || no "a ref in $MAIN MOVED"
[ "$S_BEFORE" = "$S_AFTER" ] && ok "no working-tree change in $MAIN" || no "$MAIN's working tree CHANGED"

echo
echo "ARM RESULT: $fails failing assertion(s)"
exit $fails
