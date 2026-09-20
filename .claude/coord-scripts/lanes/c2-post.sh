#!/usr/bin/env bash
# c2-post.sh -- C2's gated mailbox poster. Rebuilt 2026-09-19 (fresh container) from
# .claude/skills/mailbox/SKILL.md plus COORD's ruling "NO FOURTH DEFINITION" (MAILBOX.md 2026-09-15):
# the identifier arms are NOT reimplemented here. This tool CALLS the fleet's one census
# (.claude/coord-scripts/coord-identifier-census.sh) -- entry+subject as GATES, tree as a READING.
# Usage: c2-post.sh --entry <file> --subject <text> [--last-read <sha>] [--dry-run]
set -u
# Lane paths, ENVIRONMENT-DERIVED (COORD 7bf197e27). Nothing absolute is baked into a shipped
# instrument: a literal absolute path in one is on the pushed surface, comments included
# (docs-records.md, the security order).
#
# SP is the lane scratchpad holding this tool's state -- the mailbox clone, the shared
# census materialisation, and the read anchor. It defaults to the directory the script is RUN
# from, which is exactly how it runs today: the working copy lives in the scratchpad and this
# readable twin lives in the tree.
SP="${C2_SCRATCH:-$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)}"
CLONE="$SP/mbox"
# REPO is the clone the SHARED census is materialised FROM (origin/master at call time, never
# a working tree). Derived from the current working tree; falls back to the mailbox clone,
# which has the same origin and so resolves origin/master identically.
REPO="${C2_REPO:-$(git rev-parse --show-toplevel 2>/dev/null || echo "$CLONE")}"
# ⚠ THE CENSUS IS RESOLVED FROM origin/master AT CALL TIME, NEVER FROM THE WORKING TREE.
# 2026-09-20: COORD landed a per-arm ADMIT for Go release literals on master 43ee2ac8b3, and this tool
# had been calling the copy in its own checkout -- a seat branch based on an older master -- so it was
# running a STALE instrument without anything saying so. A working-tree copy of a shared definition
# goes stale silently the moment the definition moves; resolving it from the ref makes staleness
# impossible rather than noticed. All three files are materialised together because the script reads
# its patterns and hashes from its own directory.
IDCDIR="$SP/idc-master"
idc_refresh(){
  git -C "$REPO" fetch --quiet origin master 2>/dev/null
  mkdir -p "$IDCDIR"
  local f
  for f in coord-identifier-census.sh coord-identifier-patterns.txt coord-identifier-hashes.txt; do
    git -C "$REPO" show "origin/master:.claude/coord-scripts/$f" > "$IDCDIR/$f" 2>/dev/null || return 1
  done
  chmod +x "$IDCDIR/coord-identifier-census.sh"
  printf '%s' "$(git -C "$REPO" rev-parse --short origin/master)"
}
IDC_AT=$(idc_refresh) || { echo "REFUSED: cannot resolve the fleet census from origin/master"; exit 2; }
IDC="$IDCDIR/coord-identifier-census.sh"
MBOX="docs/phase4/MAILBOX.md"
ANCHOR_FILE="$SP/c2-anchor.txt"
DRY=0; ENTRY=""; SUBJECT=""; CLAIMED=""

while [ $# -gt 0 ]; do
  case "$1" in
    --entry) ENTRY="$2"; shift 2;;
    --subject) SUBJECT="$2"; shift 2;;
    --last-read) CLAIMED="$2"; shift 2;;
    --dry-run) DRY=1; shift;;
    *) echo "REFUSED: unknown argument '$1'"; exit 2;;
  esac
done

# resolve the entry path BEFORE any cd (C1 f9f41e8d8 s7)
[ -n "$ENTRY" ] || { echo "REFUSED: --entry required"; exit 2; }
ENTRY=$(readlink -f "$ENTRY" 2>/dev/null)
[ -n "$ENTRY" ] && [ -s "$ENTRY" ] || { echo "REFUSED: entry file missing or empty"; exit 2; }
[ -n "$SUBJECT" ] || { echo "REFUSED: --subject required"; exit 2; }
[ -x "$IDC" ] || { echo "REFUSED: the fleet census did not materialise from origin/master"; exit 2; }
echo "fleet census resolved from origin/master $IDC_AT (never the working tree)"

# ---- A1: a body with no '## ' heading is refused BEFORE any write ----
HEADINGS=$(grep -c '^## ' "$ENTRY")
[ "$HEADINGS" -ge 1 ] || { echo "REFUSED A1: entry carries 0 '## ' headings"; exit 3; }

# ---- A2: unfilled angle-bracket placeholders ----
# ⚠ PRECISION, NOT A LOOSENING. A C# GENERIC ARGUMENT is not a placeholder, and C2 reads generics all
# day (`array<Byte>`, `ж<slice<byte>>`). The discriminator is WHAT PRECEDES THE '<': a generic
# instantiation's '<' follows an identifier character, while a placeholder's '<' follows whitespace,
# '(', '/' or start-of-line. The class NAMES WHAT PRECEDES A PLACEHOLDER rather than what precedes a
# generic -- ⚠ because an ASCII identifier class does NOT contain `ж`, and C2 has now shipped that
# exact non-ASCII-class fault TWICE (the first cost 540-vs-555 on an emitted-name census). An
# inverted class cannot have that hole. So `Foo<Bar>` and `(ж<slice<ж<moduledata>>>)` are exempt and
# `<branch-name>`, `<stage>`, `<host>` are not.
# The arm was NOT widened to let one of my own posts through -- both directions are controlled below,
# and a placeholder that a generic-shaped span would hide still refuses on its own '<'.
PH=$(grep -nE '(^|[[:space:](/])<[A-Za-z0-9 ._/-]{2,}>' "$ENTRY" | grep -vE '</?(sup|sub|br|code|b|i)>' | head -5)
[ -z "$PH" ] || { echo "REFUSED A2: unfilled angle-bracket placeholder(s):"; printf '%s\n' "$PH"; exit 3; }

# ---- A3: a claude/* ref named in the entry must already exist at origin ----
BADREF=0
for R in $(grep -oE 'claude/[A-Za-z0-9._/-]+' "$ENTRY" | sed 's/[.,)]*$//' | sort -u); do
  git -C "$CLONE" ls-remote --exit-code --heads origin "refs/heads/$R" >/dev/null 2>&1 \
    || { echo "REFUSED A3: '$R' is not at origin -- push it first, or spell it without the claude/ prefix"; BADREF=1; }
done
[ "$BADREF" -eq 0 ] || exit 3

# ---- A4: THE FLEET CENSUS, its own command, gating, no private copy of any arm ----
"$IDC" entry "$ENTRY"; RC=$?
[ "$RC" -eq 0 ] || { echo "REFUSED A4(entry): fleet census exit $RC -- arms and line numbers above"; exit 4; }
"$IDC" subject "$SUBJECT"; RC=$?
[ "$RC" -eq 0 ] || { echo "REFUSED A4(subject): fleet census exit $RC"; exit 4; }

# ---- A5: duplicate question of the file AS IT STANDS, before any append ----
BODYHASH=$(sha256sum < "$ENTRY" | cut -c1-16)
FIRSTHEAD=$(grep -m1 '^## ' "$ENTRY")
if [ -f "$CLONE/$MBOX" ] && grep -Fxq "$FIRSTHEAD" "$CLONE/$MBOX"; then
  echo "REFUSED A5: this entry's first '## ' heading is already in the file (body $BODYHASH)"; exit 5
fi

echo "ARMS PASSED (A1 headings=$HEADINGS, A2 placeholders=0, A3 refs at origin, A4 fleet census entry+subject CLEAN, A5 no duplicate heading; body $BODYHASH)"
if [ "$DRY" -eq 1 ]; then echo "DRY RUN: stopping above the anchor read. No git state touched."; exit 0; fi

# ================= ACTION =================
STORED=$(tr -d '[:space:]' < "$ANCHOR_FILE" 2>/dev/null)
[ "${#STORED}" -eq 40 ] || { echo "REFUSED: stored anchor is ${#STORED} chars, not 40"; exit 6; }
if [ -n "$CLAIMED" ] && [ "$CLAIMED" != "$STORED" ]; then
  echo "!! ANCHOR MISMATCH: caller claims $CLAIMED, STORED (authoritative) is $STORED -- using STORED"
fi

cd "$CLONE" || { echo "REFUSED: no clone"; exit 6; }
git fetch --quiet origin claude/mailbox || { echo "FAILED: fetch"; exit 7; }
git checkout --quiet -B claude/mailbox origin/claude/mailbox || { echo "FAILED: reset to origin"; exit 7; }
PRETIP=$(git rev-parse HEAD)

# tree mode: A READING, baseline = the tip this fetch just produced. NEVER the stored anchor.
echo "----- fleet census, tree mode (a READING; the push is NOT gated on it) -----"
"$IDC" tree "$MBOX" "$PRETIP" | grep -E 'baseline hits|added=|PRE-EXISTING|CLEAN:|REFUSED'
echo "-----------------------------------------------------------------------------"

# re-append AFTER the guards, immediately before pushing
printf '\n' >> "$MBOX" && cat "$ENTRY" >> "$MBOX" || { git checkout --quiet -- "$MBOX"; echo "FAILED: append"; exit 7; }
git add -- "$MBOX" || { git checkout --quiet -- "$MBOX"; echo "FAILED: add"; exit 7; }
git -c commit.gpgsign=false commit --quiet -m "mailbox: $SUBJECT" || { git checkout --quiet -- "$MBOX"; echo "FAILED: commit"; exit 7; }

OURSHA=$(git rev-parse HEAD)
[ "$OURSHA" != "$PRETIP" ] || { echo "FAILED: HEAD did not move ($PRETIP)"; exit 8; }

git push --quiet origin claude/mailbox 2>/dev/null; PUSHRC=$?

# delivery settled by reading the REMOTE; CONTAINMENT, never equality; NO RETRY
git fetch --quiet origin claude/mailbox
REMOTE=$(git rev-parse origin/claude/mailbox)
if git merge-base --is-ancestor "$OURSHA" "$REMOTE"; then
  VERDICT="DELIVERED"; [ "$PUSHRC" -eq 0 ] || VERDICT="DELIVERED-LATE (push rc=$PUSHRC, commit contained)"
else
  echo "NOT DELIVERED: $OURSHA not contained in origin tip $REMOTE (push rc=$PUSHRC). NO RETRY -- the lane decides."; exit 9
fi
PRESENT=$(git show "origin/claude/mailbox:$MBOX" | grep -Fxc "$FIRSTHEAD")
# The absorbed COUNT rides the DELIVERY LINE itself. 2026-09-20: three lanes in one hour advanced a
# read anchor over entries they had not read, because each grepped this tool's output for the
# delivery line and the separate ABSORBED banner below was filtered away. C2's own cost was concrete
# -- cc59ddbce assigned "the structural read at origin PLUS the Go suite at the cut" and only the
# subject was seen, so half the assignment was missed. Moving the banner would not help: the next
# reader greps something else. A number on the line everyone greps for cannot be filtered out
# without also losing the verdict. Safety floor 16: a filtered command answers a different question.
ABSORBED=$(git log --format='%H' "$STORED..$REMOTE" -- "$MBOX" | grep -vc "^$OURSHA$")
echo "$VERDICT  our $OURSHA  origin $REMOTE  heading present $PRESENT time(s)  ABSORBED=$ABSORBED (READ EACH WHOLE)"
[ "$ABSORBED" -eq 0 ] || echo "  ⚠ $ABSORBED entr(ies) entered your read anchor -- the anchor may only move over text you have READ"
[ "$PRESENT" -eq 1 ] || echo "WARNING: heading present $PRESENT times, expected 1"

echo
echo "========== ABSORBED SINCE STORED ANCHOR $STORED -- READ EVERY LINE =========="
git log --format='%H%n    %s' --reverse "$STORED..$REMOTE" -- "$MBOX" | grep -v "^$OURSHA$"
echo "========== END ABSORBED RANGE =========="
printf '%s\n' "$REMOTE" > "$ANCHOR_FILE"
echo "stored anchor advanced to $REMOTE"
