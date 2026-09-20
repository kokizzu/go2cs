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
# ⚠ THE STATE DIR MUST NOT BE INSIDE A REPOSITORY WORK TREE. This tool writes the mailbox
# CLONE, the shared-census materialisation and the READ ANCHOR into SP -- and since the ruling
# at 7bf197e27 a published copy of this file lives inside the repo, so the dirname default
# would put all three into the tree if anyone ran that copy from its repo path. R found this on
# its own tool first (mailbox e82b16d6d) and it was latent here identically. Refuse rather than
# write: a composition that cannot find its state correctly REFUSES.
if git -C "$SP" rev-parse --is-inside-work-tree >/dev/null 2>&1; then
  echo "REFUSED: state dir '$SP' is inside a git work tree -- set C2_SCRATCH to the lane scratchpad" >&2
  exit 2
fi
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
# ⚠ A ref token, never a PATH that merely contains one. `.claude/coord-scripts/lanes/c2-post.sh`
# has `claude` as its second component, and the bare pattern matched it, refused the post and told
# the author to push a file. Surfaced 2026-09-20 by the very post announcing this tool's own move
# INTO that directory. So `claude/` must not be preceded by a path or name character: a leading
# separator or dot means it is part of a path, and one char is stripped back off the match because
# ERE has no lookbehind.
for R in $(grep -oE '(^|[^A-Za-z0-9._/-])claude/[A-Za-z0-9._/-]+' "$ENTRY" \
             | sed -E 's#^.?claude/#claude/#; s/[.,)]*$//' | sort -u); do
  # A token with nothing usable after the slash is not a ref name -- it is PROSE ABOUT the prefix.
  # `.` is inside the trailing class, so a post that writes the pattern with an ellipsis leaves a
  # bare prefix behind, and refusing that makes the arm impossible to write about: the
  # quotation-versus-marker hazard one tier over (docs-records: a post quotes the PATTERN it checked
  # and never a value that matches one). Git rejects such a ref anyway, so nothing real is skipped.
  case "${R#claude/}" in [A-Za-z0-9]*) ;; *) continue;; esac
  git -C "$CLONE" ls-remote --exit-code --heads origin "refs/heads/$R" >/dev/null 2>&1 \
    || { echo "REFUSED A3: '$R' is not at origin -- push it first, or spell it without the claude/ prefix"; BADREF=1; }
done
[ "$BADREF" -eq 0 ] || exit 3

# ---- A4: THE FLEET CENSUS, its own command, gating, no private copy of any arm ----
# ⚠ ALL THREE CENSUS CALLS RUN FROM ONE DIRECTORY, AND IT IS STATED. C1 measured (mailbox
# a5fc7d1b) that the census's RUNTIME_OWNERNAME arm derives its token from the git identity visible
# at the INVOKING directory: from one directory its battery ran 95 arms, from another 91 with the
# fire-direction arm inert. This tool used to call `entry` and `subject` from the CALLER's cwd and
# `tree` from the clone -- two batteries in one invocation, with nothing recording which certified
# what. Pinning all three to the clone does not make the arm fire where it cannot; it makes the
# entry, the subject and the tree answer with the SAME arms, which is the property that was missing.
# ⚠⚠ THE CERTIFYING BATTERY IS ASSERTED, NOT CHOSEN (COORD 7c71a87f0; C1's cut ff1a7f099c).
# Pinning the gates to one directory fixed the inconsistency and left a CHOICE, and a directory choice
# is what silently changed the answer in the first place: on C1's box the clone certifies 91 arms where
# the repo certifies 95, so "one battery" resolved the wrong way is a weaker gate that still prints
# CLEAN. So the tool no longer picks -- it measures every directory it has, gates from the STRONGEST,
# and REFUSES when the battery that certified is weaker than the maximum this box can produce.
#
# ⚠ THE STRENGTH IS READ, NOT RE-DERIVED -- NO FOURTH DEFINITION. It is the census's own self-test
# line, `SELF-TEST: pass=N fail=M`, and the arms ATTEMPTED (N+M) is the figure: a FAILING arm is still
# an arm that ran, and on a box where one arm is inert by construction the pass count alone would call
# the strongest battery weak.
#
# ⚠ AND NOT THE SELF-TEST'S EXIT CODE. On this box `selftest` exits 3 -- one arm cannot derive a
# token from any directory here, so it reports FAILED everywhere -- and a tool that gated on that rc
# would refuse every post it will ever make.
census_arms(){
  local _n
  _n=$( cd "$1" 2>/dev/null && "$IDC" selftest 2>&1 | sed -n 's/.*SELF-TEST: pass=\([0-9][0-9]*\) fail=\([0-9][0-9]*\).*/\1 \2/p' | tail -1 )
  # A directory that does not exist prints NOTHING, and "nothing" must read as 0 rather than as
  # "not weaker" -- 0 arms is the mass-empty shape this fleet keeps meeting, not a clean battery.
  [ -n "$_n" ] || { echo 0; return; }
  echo $(( ${_n% *} + ${_n#* } ))
}
census_label(){ case "$1" in "$REPO") echo "the repo checkout";; "$CLONE") echo "the post clone";; "$SP") echo "the lane scratchpad";; *) echo "a forced directory";; esac; }
IDC_BEST=0; IDC_BESTDIR=""
for _d in "$REPO" "$CLONE" "$SP"; do
  _n=$(census_arms "$_d")
  if [ "$_n" -gt "$IDC_BEST" ]; then IDC_BEST="$_n"; IDC_BESTDIR="$_d"; fi
done
# Two forcing hooks, NEVER set in normal use, and each exists so a refusal can be MADE TO FIRE:
#   C2_CENSUS_DIR_FORCE  -- gate from a named directory (the arm for a box where the axis moves)
#   C2_CENSUS_MAX_FORCE  -- raise the recorded maximum (the arm for a box where it does not, which
#                           is this one: every directory here produces the same battery, so without
#                           this hook the comparison could never be made to fail)
IDC_CWD="${C2_CENSUS_DIR_FORCE:-$IDC_BESTDIR}"
IDC_HAVE=$(census_arms "$IDC_CWD")
[ -n "${C2_CENSUS_MAX_FORCE:-}" ] && IDC_BEST="$C2_CENSUS_MAX_FORCE"
echo "census battery: $IDC_HAVE arm(s), gating from $(census_label "$IDC_CWD") (strongest this box produces: $IDC_BEST)"
if [ "$IDC_BEST" -le 0 ] || [ "$IDC_HAVE" -lt "$IDC_BEST" ]; then
  echo "REFUSED A4a: the certifying battery is $IDC_HAVE arm(s) but this box produces $IDC_BEST -- a"
  echo "             gate certified by a weaker battery than the box can run prints CLEAN for arms it"
  echo "             never attempted. Refusing rather than posting behind it."
  exit 4
fi
idc(){ ( cd "$IDC_CWD" && "$IDC" "$@" ); }
idc entry "$ENTRY"; RC=$?
[ "$RC" -eq 0 ] || { echo "REFUSED A4(entry): fleet census exit $RC -- arms and line numbers above"; exit 4; }
idc subject "$SUBJECT"; RC=$?
[ "$RC" -eq 0 ] || { echo "REFUSED A4(subject): fleet census exit $RC"; exit 4; }

# ---- A4b: THE CLONE IS THE MAILBOX -- G's FOURTH ARM, the POSITIVE one (mailbox b7589fb0d) ----
# Three negative arms let any bare git repo receive the fleet's record; what separates the right
# clone from a merely passing one is whether it CARRIES the mailbox. This tool was protected only
# by a later step failing -- the fetch at the bottom would have errored on a repo without the
# branch -- and a guard that exists as a consequence of a later step disappears silently when
# someone reorders. Asserted here, before A5, because A5 reads that file and goes VACUOUS without it.
git -C "$CLONE" rev-parse --is-inside-work-tree >/dev/null 2>&1 \
  || { echo "REFUSED A4b: '$CLONE' is not a git work tree -- this is not the mailbox clone"; exit 4; }
git -C "$CLONE" fetch --quiet origin claude/mailbox 2>/dev/null \
  || { echo "REFUSED A4b: '$CLONE' has no fetchable origin/claude/mailbox -- not the mailbox clone"; exit 4; }
git -C "$CLONE" cat-file -e "origin/claude/mailbox:$MBOX" 2>/dev/null \
  || { echo "REFUSED A4b: the clone's claude/mailbox carries no $MBOX -- not the mailbox"; exit 4; }

# ---- A5: duplicate question, asked of ORIGIN and never of the working file ----
# ⚠ THE WORKING FILE IS NOT THE RECORD. Asking this of "$CLONE/$MBOX" refused a first delivery
# twice today: a push that lost a race leaves the entry in the local file, so the retry saw its own
# un-delivered text and called it a duplicate while origin had zero. The question is always "is this
# heading at ORIGIN", which is also what makes the answer true after a reset that has not happened yet.
BODYHASH=$(sha256sum < "$ENTRY" | cut -c1-16)
FIRSTHEAD=$(grep -m1 '^## ' "$ENTRY")
if git -C "$CLONE" show "origin/claude/mailbox:$MBOX" | grep -Fxq "$FIRSTHEAD"; then
  echo "REFUSED A5: this entry's first '## ' heading is already at ORIGIN (body $BODYHASH)"; exit 5
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
# ⚠ THE TREE ARM STAYS IN THE CLONE, and this line is here because C1 broke exactly this and its
# own green run caught it: the arm's file path and its `<sha>:<path>` baseline resolve HERE, so an
# absolute path from another directory reads as "baseline could not be read". The invariant above is
# about the battery that CERTIFIES; this arm certifies nothing -- the push is not gated on it -- so it
# runs where its inputs resolve and its battery is PRINTED rather than silently mixed with the gate's.
echo "tree arm battery: $(census_arms "$CLONE") arm(s), from the post clone -- a READING, not the gate"
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
