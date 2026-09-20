#!/usr/bin/env bash
# ARM: --mark-read, the door for the hand-written anchor (adopted from C2 130c9e43a).
#
# ⚠⚠ EVERY ARM ASSERTS THE REFUSAL **TEXT**, NOT THE rc. All five refusals share rc 16 on purpose,
# and C2's vacuous arm is the reason: their "not an entry" case was armed with a merge commit, it
# refused, the arm went green -- and the refusal was the BACKWARDS check firing first, so the check
# under test was never reached. A refusal is not a pass; the rc says a door closed, the TEXT says
# WHICH ONE. Each fixture below is built so EVERY EARLIER CHECK PASSES.
#
# Usage: r-post-markread-arm.sh <tool>
set -u
TOOL="$(readlink -f "${1:?usage: r-post-markread-arm.sh <tool>}")"
# C1 b68ed837d: the THIRD vacuity shape -- the subject never invoked at all. A relative tool
# path plus a cd read as a clean PASS in C1 arm. Refuse an unrunnable subject before any verdict.
[ -r "$TOOL" ] || { echo "REFUSED(2): the tool under test is not readable: $TOOL"; exit 2; }
ROOT="${2:-/c/go2cs-tmp/r-markread-arm}"
GID='-c user.email=arm@local -c user.name=arm -c commit.gpgsign=false'
FILE="docs/phase4/MAILBOX.md"
BR="claude/mailbox"
fails=0
ok(){ echo "  PASS  $1"; }
no(){ echo "  FAIL  $1"; fails=$((fails+1)); }

rm -rf "$ROOT"; mkdir -p "$ROOT/state" || exit 1
git init --bare -q "$ROOT/remote.git"
git clone -q "$ROOT/remote.git" "$ROOT/clone" 2>/dev/null
cd "$ROOT/clone" || exit 1
git config commit.gpgsign false; git config user.email arm@local; git config user.name arm
mkdir -p docs/phase4

# entry 1, entry 2: two real entries; plus a NON-entry commit on the same branch.
printf '## ENTRY ONE\n\nbody one\n' > "$FILE";  git add -A; git $GID commit -qm e1; E1=$(git rev-parse HEAD)
printf 'unrelated\n' > README.arm;             git add -A; git $GID commit -qm nonentry; NE=$(git rev-parse HEAD)
printf '## ENTRY TWO\n\nbody two\n' >> "$FILE"; git add -A; git $GID commit -qm e2; E2=$(git rev-parse HEAD)
git push -q origin "HEAD:$BR"
# a commit that exists in the clone but was never pushed -> not an ancestor of the live tip
printf '## ENTRY THREE (unpushed)\n\nbody three\n' >> "$FILE"; git add -A; git $GID commit -qm e3
UNPUSHED=$(git rev-parse HEAD)
git reset -q --hard "$E2"
cd - >/dev/null || exit 1

run(){ R_MAILBOX_CLONE="$ROOT/clone" R_POST_STATE="$ROOT/state" bash "$TOOL" --mark-read "$1" 2>&1; }
anchor(){ cat "$ROOT/state/r-anchor.txt" 2>/dev/null || echo "<none>"; }

arm(){ # $1 = label, $2 = sha, $3 = text that MUST appear
  local out; out="$(run "$2")"
  if printf '%s' "$out" | grep -q -- "$3"; then ok "$1"
  else no "$1 -- got: $(printf '%s' "$out" | head -1)"; fi
}

echo "=== ARM: --mark-read, each refusal by its OWN text ==="
A0="$(anchor)"
arm "not hex                -> names the sha"        "zzzz1234"        "is not a hex sha"
arm "too short              -> names the length"     "abc"             "too short to name a commit"
arm "hex but not a commit   -> names the clone"      "$(printf 'a%.0s' $(seq 40))" "is not a commit in the post clone"
arm "a commit, not on the ref -> names the ancestry" "$UNPUSHED"       "is not an ancestor of the live tip"
arm "on the ref, not an entry -> names the file"     "$NE"             "is not an entry"
[ "$(anchor)" = "$A0" ] && ok "the anchor is byte-unchanged across every refusal" \
                        || no "a refusal WROTE the anchor"

# GREEN, then the backwards refusal -- which needs the anchor AHEAD, so it runs last.
OUT="$(run "$E2")"
if printf '%s' "$OUT" | grep -q "MARK-READ: anchor -> $E2"; then ok "a real entry ahead of the anchor is WRITTEN"
else no "the green case was refused: $(printf '%s' "$OUT" | head -1)"; fi
[ "$(anchor)" = "$E2" ] && ok "and the anchor file holds it" || no "the anchor file does not hold it"

arm "behind the stored anchor -> names the direction" "$E1" "an anchor never moves back"
[ "$(anchor)" = "$E2" ] && ok "the backwards refusal left the anchor alone" || no "the anchor moved back"

echo
echo "ARM RESULT: $fails failing assertion(s)"
exit $fails
