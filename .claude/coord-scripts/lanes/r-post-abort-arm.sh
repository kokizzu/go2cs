#!/usr/bin/env bash
# ARM: the failed-merge ABORT path of the R post tool.
#
# ⚠ WHY THIS EXISTS (C2, mailbox 4f29a8742 §2): "the dry run EXITS ABOVE the block being removed --
# an A/B whose arms cannot differ is not an A/B." I verified my own 2026-09-20 abort fix with a DRY
# RUN and the 15 admission arms. The dry run exits at step 3; the abort sits in step 4, BELOW it.
# Neither instrument can reach the line I changed, so the fix was UNARMED and I had said it was
# verified. This arm reaches it, by building a throwaway remote and LOSING A RACE on purpose.
#
# Usage: r-post-abort-arm.sh <path-to-tool-under-test>
set -u
TOOL="${1:?usage: r-post-abort-arm.sh <tool>}"
# C1 b68ed837d: the THIRD vacuity shape -- the subject never invoked at all. A relative tool
# path plus a cd read as a clean PASS in C1 arm. Refuse an unrunnable subject before any verdict.
[ -r "$TOOL" ] || { echo "REFUSED(2): the tool under test is not readable: $TOOL"; exit 2; }
TOOL="$(readlink -f "$TOOL")"
ROOT="${2:-/c/go2cs-tmp/r-abort-arm}"
GID='-c user.email=arm@local -c user.name=arm -c commit.gpgsign=false'
FILE="docs/phase4/MAILBOX.md"
BR="claude/mailbox"
fails=0
ok(){ echo "  PASS  $1"; }
no(){ echo "  FAIL  $1"; fails=$((fails+1)); }

rm -rf "$ROOT" || true
mkdir -p "$ROOT" || exit 1

# --- the throwaway channel -------------------------------------------------
git init --bare -q "$ROOT/remote.git"
git clone -q "$ROOT/remote.git" "$ROOT/seed" 2>/dev/null
mkdir -p "$ROOT/seed/docs/phase4"
# the duplicate-check floor refuses below 1000 lines, so the fixture must clear it HONESTLY
i=1; : > "$ROOT/seed/$FILE"
while [ $i -le 1200 ]; do echo "filler line $i" >> "$ROOT/seed/$FILE"; i=$((i+1)); done
git -C "$ROOT/seed" add "$FILE"
git -C "$ROOT/seed" $GID commit -qm seed
git -C "$ROOT/seed" push -q origin "HEAD:$BR"

git clone -q --single-branch --branch "$BR" "$ROOT/remote.git" "$ROOT/post"
git clone -q --single-branch --branch "$BR" "$ROOT/remote.git" "$ROOT/racer"
git -C "$ROOT/post"  config commit.gpgsign false
git -C "$ROOT/post"  config user.email arm@local; git -C "$ROOT/post"  config user.name arm
git -C "$ROOT/racer" config commit.gpgsign false
git -C "$ROOT/racer" config user.email arm@local; git -C "$ROOT/racer" config user.name arm

# --- the race, injected DETERMINISTICALLY by a pre-push hook ----------------
# The tool fetches and fast-forwards at step 3, so a race must be injected BETWEEN that and its push.
# The hook fires ONCE (sentinel), pushes a CONFLICTING append from the racer clone, and returns 0 --
# the tool's own push then fails as non-fast-forward, which is the real lost-race shape.
cat > "$ROOT/post/.git/hooks/pre-push" <<HOOK
#!/bin/sh
[ -f "$ROOT/.raced" ] && exit 0
touch "$ROOT/.raced"
printf '\n## RACER ENTRY -- a conflicting append at the same end of the same file\n\nbody\n' >> "$ROOT/racer/$FILE"
git -C "$ROOT/racer" commit -qam racer
git -C "$ROOT/racer" push -q origin "HEAD:$BR"
exit 0
HOOK
chmod +x "$ROOT/post/.git/hooks/pre-push"

# --- the entry under post --------------------------------------------------
mkdir -p "$ROOT/state"
printf '## ARM ENTRY -- the lane append that must lose the race\n\nbody of the arm entry.\n' > "$ROOT/entry.md"
printf 'arm: abort-path exercise\n' > "$ROOT/subject.txt"

echo "=== ARM: failed merge must ABORT and leave the clone clean ==="
set +e
OUT="$(R_MAILBOX_CLONE="$ROOT/post" R_POST_STATE="$ROOT/state" bash "$TOOL" "$ROOT/entry.md" "$ROOT/subject.txt" 2>&1)"
RC=$?
set -e
echo "$OUT" | sed 's/^/    | /'
echo "    rc=$RC"

# --- assertions ------------------------------------------------------------
[ "$RC" -eq 11 ] && ok "rc is 11 (push/merge refusal)" || no "rc is $RC, expected 11"
echo "$OUT" | grep -q 'ABORTED' && ok "output names the abort" || no "output does not name the abort"
echo "$OUT" | grep -q 'INTERLEAVED' && ok "the interleaved posts were listed before the merge" \
                                    || no "no INTERLEAVED listing -- the race did not happen"
# ⚠ THE LOAD-BEARING ONE: the clone must be re-runnable, i.e. NOT mid-conflict.
DIRT="$(git -C "$ROOT/post" status --porcelain)"
[ -z "$DIRT" ] && ok "clone is CLEAN after the refusal" || no "clone is DIRTY: $(echo "$DIRT" | tr '\n' ' ')"
[ -f "$ROOT/post/.git/MERGE_HEAD" ] && no "MERGE_HEAD still present -- merge in progress" \
                                    || ok "no MERGE_HEAD -- no merge in progress"
git -C "$ROOT/post" diff --name-only --diff-filter=U | grep -q . \
    && no "unmerged paths remain" || ok "no unmerged paths"

echo
echo "ARM RESULT: $fails failing assertion(s)"
exit $fails
