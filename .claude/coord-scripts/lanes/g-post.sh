#!/usr/bin/env bash
# Delivery-verified mailbox append from a FILE (never a heredoc: a long inline command gets cut mid-line).
# usage: g-post.sh <entry-file> <commit-subject> <last-read-hash>
set -uo pipefail

# DURABILITY BEFORE ANYTHING ELSE (C1 f56077662, 2026-09-13; G hit the same defect twice the same
# night). This tool's most important output is the ABSORBED-RANGE listing, which the protocol says is
# read WHOLE and never headed or tailed -- and a caller that pipes this script through `head` silences
# exactly that, while floor 7's "capture the exit code before any pipe" reads as a rule about where you
# LOOK rather than about what the pipe DOES to the producer. G piped this tool through `head -25` and
# `head -12`, catching both only by re-reading the range afterwards; C1 did it five times.
#
# So the fix goes in the TOOL, not in the caller's memory: everything below is tee'd to a log first,
# and a truncating reader then costs nothing because the absorbed range is on disk whatever the
# terminal did. A discipline broken twice in one night is not a control.
POSTLOG="${G_POST_LOG:-/tmp/g-post-last.log}"

# `--output-error=warn` is the whole point and the first version of this line did not have it.
# A plain `tee LOG` writes to the LOG and to stdout; when a caller's `head` closes stdout, tee takes
# the write error and dies, and the log dies with it -- so the remedy was defeated by the exact
# mechanism it was written for, measured on its own next invocation (3 lines logged for a run that
# produced more). With warn, tee reports the broken stdout and KEEPS WRITING THE FILE.
#
# The first version was "controlled" only on a REFUSE arm, which is two lines long and finishes before
# any reader can close the pipe -- a control that cannot reach the failure mode. The arm below that
# proves this one is a truncating caller over a FULL run.
# SHAPE ADOPTED FROM C1 d84393c82 after we both shipped a defective tee within an hour.
#
# `tee LOG` is a FAN-OUT primitive, not a durability primitive: it writes the file and stdout in one
# loop, so a reader that exits SIGPIPEs tee and the file write dies with it -- the file is only as
# durable as the reader, the exact property the remedy claimed to remove. `--output-error=warn` keeps
# tee alive, which is an improvement and still leaves stdout in the path.
#
# This makes the FILE the primary unconditional sink that no reader can close, and dumps it to the
# caller at EXIT where a dead reader costs only the echo. It also restores the exit code: with the
# script's stdout no longer being the pipe, PIPESTATUS[0] reports the SCRIPT's rc instead of head's.
exec 3>&1
exec > "$POSTLOG" 2>&1
trap 'cat "$POSTLOG" >&3 2>/dev/null || true' EXIT

ENTRY="$1"; SUBJ="$2"; ANCHOR="$3"

# NOTE FOR WHOEVER RUNS AN ADMIT-ARM CONTROL NEXT, INCLUDING ME: this tool ALREADY has a dry run.
# Set GPOST_DRYRUN=1 and it stops after every guard and before any mailbox side effect (see the
# switch further down, whose comment cites the doctrine verbatim). On 2026-09-13 I published the junk
# entry 65c10500a by running an admit arm WITHOUT it, and then reported "my tool has no dry-run
# switch" as the root gap -- which was false. The gap was that I did not read my own tool before
# describing it. The switch has been there the whole time.

# ENTRY-FILE GUARD (C1's tool had one, mine did not -- found by controlling the tee on a refuse arm,
# 2026-09-13). Without it a mistyped path falls through to `cat` and `awk` and reads as a TOOL CRASH
# rather than a refusal, which is the wrong shape: a refusal names what is wrong and a crash invites
# the reader to wonder whether the tool is broken. Nothing published either way -- this closes a
# legibility gap, not a safety one, and it says so rather than overclaiming.
if [ ! -r "$ENTRY" ]; then
  echo "ENTRY FILE MISSING OR UNREADABLE: $ENTRY -- NOT POSTED"
  exit 4
fi

if [ ! -s "$ENTRY" ]; then
  echo "ENTRY FILE IS EMPTY: $ENTRY -- NOT POSTED (an empty append would move the tip and say nothing)"
  exit 4
fi

MB="${G_MAILBOX_CLONE:-$(cd "$(dirname "$0")/../../.." && pwd)/../g2-mailbox}"
# CONTROL-ONLY CLONE OVERRIDE (R b89c04b75's isolation, adopted per C1 ab3f4a71a, 2026-09-15). A planted-line
# control runs against a THROWAWAY bare repo + fixture clone, never the live mailbox clone. GPOST_MB points the tool
# at that fixture and is REFUSED without GPOST_DRYRUN, before any git step, so no real post can point elsewhere.
if [ -n "${GPOST_MB:-}" ]; then
  [ -n "${GPOST_DRYRUN:-}" ] || { echo "GPOST_MB WITHOUT GPOST_DRYRUN -- REFUSED before any git step (a real post never points away from the mailbox clone)"; exit 8; }
  MB="$GPOST_MB"; echo "CONTROL CLONE: $MB (dry run only)"
fi
# Scrub gate: real identifiers only. A pattern DESCRIPTION (naming a posix home prefix as a class)
# BOTH pushed surfaces -- the mailbox BODY and the COMMIT MESSAGE. git pushes the message to GitHub
# just as publicly as the body, and a gate that censuses only the body prints green over the other
# half (i9's finding, 2026-09-08: found by luck, via an unrelated refusal one file over).
printf '%s' "$SUBJ" > /tmp/g-post-subj.$$
# The C:\Users arm deliberately does NOT fire on the REDACTED placeholder form `C:\Users\<user>\`,
# which is the correct spelling and appears in landed docs. A gate that refuses the redaction is a
# gate people route around. It fires on C:\Users\ followed by anything that is not the placeholder's
# '<' -- i.e. a real account name.
SPD="$(cd "$(dirname "$0")" && pwd)"
# Patterns live in a FILE, written by a tool that preserves bytes. They were inline until an edit
# collapsed the doubled backslashes and left `C:\Users\[^<]` -- which in ERE is the literal text
# `C:Users[^<]` and matches nothing. That arm was DEAD and six controls passed anyway, because none
# of them isolated it: the real-account-path probe was being caught by the account-name arm instead.
# Each arm is now controlled by a probe only THAT arm can catch.
# ================================================================================================
# ADOPTED: the fleet's ONE identifier census (COORD 51fa9ca29, landed on master e77b6d65fb2f).
#
# Everything this tool used to carry -- its own pattern file, its own allowlist, its own census()
# over them, its own split-token pass and its own masked refusal -- is GONE. The contract is
# explicit: NO TOOL CARRIES A PRIVATE COPY OF ANY ARM. If an arm is wrong it is wrong there, for
# everyone, and fixed there. Three calls, each in its OWN command with `|| exit $?`, never composed
# into the push chain, and this tool NEVER passes --unmask.
#
# What that supersedes here, named so the history is not lost rather than deleted silently:
#   - the seven-arm pattern file and the git-config-derived owner-token arm (arms 1-7)
#   - the split-token PASS 2 I built after i9's finding: the shared census has it as PASS 2, plus a
#     PASS 3 alphanumeric reduction mine never had
#   - my masked refusal from R's 0bb28dac1 finding, proven this session on a planted control: the
#     shared census masks report AND refusal identically from one function, so a wrapper that printed
#     its own rendering would be a second disclosure policy in one tool -- exactly what adoption ends
# ⚠ RE-MATERIALISED AT THE ACT, never a static copy (R dd8f54f3b, C1 65cd66c6c). A local extract goes
# stale SILENTLY, which is a private copy wearing another name: master moved three times in twenty
# minutes tonight and I re-extracted three times to keep up -- 62/62 against a copy already two commits
# behind the cured 73/73. So the definition is fetched from origin/master AT THE ACT, its SHA printed,
# and the three files written fresh; a fetch failure REFUSES rather than silently using whatever is on
# disk, because a gate running an unknown version is a gate that cannot say what it checked.
IDC_DIR="${G_CENSUS_DIR:-${TMPDIR:-/tmp}/coord-census}"
IDC="$IDC_DIR/coord-identifier-census.sh"
IDC_REPO="${G_REPO:-$(cd "$(dirname "$0")/../../.." && pwd)}"
mkdir -p "$IDC_DIR"
git -C "$IDC_REPO" fetch -q origin master 2>/dev/null || { echo "CENSUS FETCH FAILED -- NOT POSTED (refusing to censor with an unknown version)"; exit 3; }
IDC_SHA=$(git -C "$IDC_REPO" rev-parse origin/master 2>/dev/null)
[ -n "$IDC_SHA" ] || { echo "CENSUS SHA UNRESOLVED -- NOT POSTED"; exit 3; }
for f in coord-identifier-census.sh coord-identifier-patterns.txt coord-identifier-hashes.txt; do
  # ⚠ MSYS_NO_PATHCONV. Git Bash rewrites a `rev:path` argument as a Windows path: `origin/master:.claude/…`
  # arrives as `origin\master;.claude\coord-scripts\…` -- slashes flipped and the COLON turned into a
  # SEMICOLON -- and git dies "ambiguous argument". Measured: bare form rc=128, guarded form rc=0 and
  # 74,263 bytes. The first cut of this swallowed stderr with 2>/dev/null and reported "MISSING AT THE
  # ACT", so a present file read as an absent one and the gate could not say why it refused. It failed
  # CLOSED, which is right, but a gate that cannot name its own failure is half a gate.
  # ⚠ AND THE FIRST FIX TRADED ONE CONVERSION FAULT FOR ITS MIRROR. MSYS_NO_PATHCONV=1 disables
  # conversion for the WHOLE command, so `-C /c/Projects/go2cs` stopped being translated and git died
  # "cannot change to '/c/Projects/go2cs'". Measured, all four in this exact form:
  #   MSYS_NO_PATHCONV=1 + -C          rc=128  cannot chdir
  #   MSYS2_ARG_CONV_EXCL=<rev:path>   rc=0    74,263 bytes   <- this one: excludes ONLY the argument
  #   cd, no -C, no guard              rc=128  ambiguous argument (the original fault)
  #   cd, no -C, MSYS_NO_PATHCONV=1    rc=0    74,263 bytes   (works, but needs a subshell)
  # An earlier reading of MSYS2_ARG_CONV_EXCL='*' was taken WITHOUT -C and so proved nothing about this
  # call -- re-measured in the form actually used rather than carried over.
  idc_arg="origin/master:.claude/coord-scripts/$f"
  MSYS2_ARG_CONV_EXCL="$idc_arg" git -C "$IDC_REPO" show "$idc_arg" > "$IDC_DIR/$f.act" 2>"$IDC_DIR/$f.err"; idc_rc=$?
  [ -s "$IDC_DIR/$f.act" ] || { echo "FLEET CENSUS FILE UNREADABLE AT THE ACT ($f, rc $idc_rc): $(head -1 "$IDC_DIR/$f.err" 2>/dev/null) -- NOT POSTED"; rm -f "$IDC_DIR/$f.act" "$IDC_DIR/$f.err"; exit 3; }
  rm -f "$IDC_DIR/$f.err"
  mv "$IDC_DIR/$f.act" "$IDC_DIR/$f"
done
chmod +x "$IDC"
echo "CENSUS AT THE ACT: origin/master ${IDC_SHA:0:12} (re-materialised, not a local copy)"
# ARM 7 -- the OWNER'S NAME AS A BARE TOKEN. Arms 1-6 all key on an ACCOUNT or on a PATH SHAPE, and
# the owner's personal name is neither: it appears as an addressee, in prose, with no path around it,
# so it passed every arm I had. PROVEN, not assumed -- a planted bare token read CLEAN through the
# static file on 2026-09-08, the same gap R measured in their own census after i9 disclosed theirs.
# DERIVED from git config and never written down, so this script does not carry the identifier it
# forbids (the denylist rule met without a hash table). Tokens under 3 characters are dropped: a
# bare initial would refuse on ordinary prose.
# (The owner-token arm, the split-token pass and the allowlist that used to live here are all in the
# shared census now -- its PASS 2 is the split-token pass, its PASS 3 an alphanumeric reduction mine
# never had, and its hashes file carries the denied names without plaintext. Nothing local remains.)
# SPLIT-TOKEN PASS. Every arm here is LINE-BASED, so a token wrapped across a line break is
# invisible to all of them at once -- measured on this gate 2026-09-08: an owner token inline
# REFUSED rc=3, the SAME token split across a break PASSED rc=0. The class was found by i9
# (ef946a6ca) probing their own census after my 931f362d1, then reproduced independently by C2 in
# the shared repo guard and by R in their pre-post census. FOUR gates, four holes; I measured mine
# rather than assuming it differed, which is R's phrasing and the right default.
#
# The fix is STRUCTURAL, not per-arm: a second copy of the surfaces with line breaks and the
# whitespace ADJACENT to them collapsed is appended to the buffer every arm already searches, so
# all arms gain the split form with no pattern touched and an arm added tomorrow is covered.
# Collapsing whitespace at BOTH ends of each line is what closes the two commonest real shapes --
# an indented continuation and a trailing space before the break (i9's re-probe found a bare
# newline strip closes only the no-whitespace case).
#
# IT WILL PRODUCE FALSE POSITIVES AND THAT IS THE CHOSEN DIRECTION: joining fuses the end of one
# line to the start of the next, so unrelated fragments can spell a token by accident. A false
# REFUSAL costs me one rewrite; a false PASS costs the fleet a scrub.
# ONE CENSUS FUNCTION, SEVERAL INPUTS (COORD ef0c5c7c9, 2026-09-15). The arms below used to run inline
# over the entry and the subject only. The same function now also runs over the WHOLE MAILBOX.md,
# post-append, at the fetched tip (the TREE PASS further down). It is ONE function called twice, never
# a second implementation of the predicate (C1 1904d295c §3(i)): an arm added here covers both inputs.
# census LABEL FILE... sets HITS and KEPT; an instrument fault refuses by name through scrubFault.
# (census() lived here. Its three-pass structure, its exit-checked greps and its masked refusal are
# all in the shared census now, which is the point of adopting: one definition, not a second
# implementation of the same predicate. The grep-exit lesson below is kept because it is why the
# shared census's own "a gate that cannot measure does not pass" exit-2 rule is the right shape.)
# ⚠ GREP'S EXIT IS DATA, NOT NOISE. This line used to read:
#     HITS=$(grep … 2>/dev/null | grep -c … || true)
# and it CONVERTED AN INSTRUMENT CRASH INTO A CLEAN READING: if the first grep aborts, it emits
# nothing, the counting grep counts 0, HITS is "0", and the gate PASSES. Measured on this gate
# (a forced exit-134 first stage yields HITS='0'), and grep 3.0 on this box really does abort —
# R hit SIGABRT on `-i -F` and had this same crash-to-clean idiom in their own security gate TWICE
# in one day (9413bae23); they asked the fleet to grep for it and mine had it. `2>/dev/null` is the
# other half: it hides the abort message that would otherwise be the only tell.
#
# So each stage's exit is now CHECKED: 0 = matched, 1 = no match, ANYTHING ELSE = instrument fault,
# refuse by name. A gate that cannot distinguish "found nothing" from "could not look" is not a gate.
TREE_TOUCHED=0
# STRICT census of the two surfaces this post WRITES. Separate commands, separate exit gates: a
# census composed into a chain lets the chain run on whatever the census printed.
"$IDC" entry "$ENTRY"; idc_entry_rc=$?
if [ "$idc_entry_rc" != "0" ]; then
  rm -f /tmp/g-post-subj.$$
  case "$idc_entry_rc" in
    1) echo "ENTRY CENSUS REFUSED (rc 1: an identifier arm fired) -- NOT POSTED" ;;
    2) echo "ENTRY CENSUS COULD NOT MEASURE (rc 2: misuse or instrument fault, NOT a refusal) -- NOT POSTED" ;;
    *) echo "ENTRY CENSUS rc $idc_entry_rc (unexpected) -- NOT POSTED" ;;
  esac
  exit 3
fi
"$IDC" subject "$SUBJ"; idc_subj_rc=$?
if [ "$idc_subj_rc" != "0" ]; then
  rm -f /tmp/g-post-subj.$$
  case "$idc_subj_rc" in
    1) echo "SUBJECT CENSUS REFUSED (rc 1: an identifier arm fired) -- NOT POSTED" ;;
    2) echo "SUBJECT CENSUS COULD NOT MEASURE (rc 2: misuse or instrument fault, NOT a refusal) -- NOT POSTED" ;;
    *) echo "SUBJECT CENSUS rc $idc_subj_rc (unexpected) -- NOT POSTED" ;;
  esac
  exit 3
fi
# ⚠ THE REFUSAL PATH USED TO ECHO THE VALUE IT REFUSES (R 0bb28dac1, checked here as R asked every lane
# to check its own). This line printed `$KEPT` -- up to five MATCHED LINES verbatim -- so the message that
# says "you are about to publish an identifier" carried the identifier in its own body. It is dormant on
# every clean run and live in exactly the case the gate exists for, and it had never fired for me: luck,
# not a design property. The TREE refusal below already reported line numbers only, so one tool carried two
# disclosure policies. Now both mask: arm, count, and WHERE, never WHAT.
rm -f /tmp/g-post-subj.$$
# (My masked refusal lived here, built on R's 0bb28dac1 finding and proven on a planted control this
# session. The shared census masks its report and its refusal identically from one function, so this
# wrapper prints NOTHING of its own about a hit -- two renderings in one tool is the two-policies
# state adoption ends. GPOST_UNMASK is gone with it: this tool never passes --unmask.)
bash "$SPD/g-placeholder-check.sh" "$ENTRY" || { echo "PLACEHOLDER -- NOT POSTED"; exit 8; }
bash "$SPD/g-fetchable-check.sh" "$ENTRY" || { echo "UNFETCHABLE BRANCH NAMED -- NOT POSTED"; exit 9; }
# ARM 2 -- STALE SHA, over BOTH pushed surfaces. The scrub gate has censused body+message
# since i9's finding; this arm did not, and my own COMMIT SUBJECT carried two stale tips.
bash "$SPD/g-staleness-arm.sh" "$ENTRY" /tmp/g-post-subj.$$ || { rm -f /tmp/g-post-subj.$$; echo "STALE BRANCH TIP STATED -- NOT POSTED"; exit 9; }
rm -f /tmp/g-post-subj.$$
cd "$MB" || exit 4
git fetch origin claude/mailbox --quiet || { echo "FETCH FAILED"; exit 5; }

# THE READ ANCHOR COMES FROM STATE THIS TOOL REMEMBERS, NOT FROM THE CALLER'S ARGUMENT.
#
# Measured on myself, 2026-09-13: I computed the argument as `git ls-remote ... | cut -f1` moments
# before invoking this tool, so the absorbed range was `tip..tip` -- EMPTY BY CONSTRUCTION, on a guard
# whose entire job is to stop exactly that. One entry (C1's 521f421ff) was skipped; it happened to be a
# read-back confirmation owing me nothing, which is luck and not a control. The mailbox skill names this
# shape in those words: "a guard whose INPUT the caller can derive from the same source it checks
# against is not a guard", and I walked into it in the one tool the sentence is about.
#
# So: the stored anchor is AUTHORITATIVE and the caller's third argument is a CLAIM to cross-check.
# A mismatch is printed loudly and the STORED one is used -- the caller cannot silently narrow the
# range, whatever it passes, because the range is derived from state it does not hold.
ANCHOR_FILE="${GPOST_ANCHOR_FILE:-/tmp/g-post-anchor}"
STORED=""
if [ -r "$ANCHOR_FILE" ]; then STORED=$(tr -d '[:space:]' < "$ANCHOR_FILE"); fi
if [ ${#STORED} -eq 40 ]; then
  if [ "$STORED" != "$ANCHOR" ]; then
    echo "ANCHOR CLAIM MISMATCH: you passed ${ANCHOR:0:9}, this tool last delivered at ${STORED:0:9} -- USING THE STORED ONE."
    echo "  (the argument is a claim, not the anchor; a range derived from what the caller just read is empty by construction)"
  fi
  ANCHOR="$STORED"
else
  echo "NO STORED ANCHOR (first run, or the state file is gone) -- falling back to the caller's claim ${ANCHOR:0:9}, which this tool cannot verify."
fi

ABS=$(git log --oneline "$ANCHOR"..origin/claude/mailbox | wc -l)
ABSLIST=$(git log --oneline "$ANCHOR"..origin/claude/mailbox)

# TREE PASS (COORD ef0c5c7c9, 2026-09-15, option (b) in C1's shape). The census above answered for the
# entry and the subject; the file this tool WRITES is the tree it must answer for. A share-shaped line
# already in MAILBOX.md (9badd9f5e3) blocked COORD's post tool while every lane tool, this one included,
# appended over it cleanly. So the SAME census() runs again over the WHOLE MAILBOX.md, post-append, at
# the FETCHED tip, exit-gated before the commit. It sits BELOW the fetch and the reset (C1's lesson (ii):
# a census above the fetch reads a stale tree) and ABOVE the dry-run exit, so a dry run exercises it.
# The reset moves only this LOCAL clone to the fetched tip; nothing is committed or pushed above the
# dry-run exit. No history is re-scanned: the tip is the question.
#
# EXEMPTIONS: none declared. The ruling allows a bare environment-variable NAME (R's line 38909) to be
# exempted AT THE SITE with a firing control; this census's arms were measured over that tip and do not
# fire on it, so there is nothing to exempt, and none is added speculatively.
#
# THE PLANTED CONTROL runs in a THROWAWAY bare repo + fixture clone through GPOST_MB (above), never in this
# clone's working copy: an earlier cut of mine planted here and restored with proof, and C1 ab3f4a71a is right
# that a restore-proved plant in the live clone is one `commit -a` from the real branch. No plant hook exists here.
git reset --hard origin/claude/mailbox --quiet || { echo "RESET FAILED"; exit 5; }
TIPBLOB=$(git rev-parse origin/claude/mailbox:docs/phase4/MAILBOX.md)
restoreTree() {
  git reset --hard origin/claude/mailbox --quiet
  local w p; w=$(git hash-object docs/phase4/MAILBOX.md); p=$(git status --porcelain | wc -l | tr -d ' ')
  echo "TREE RESTORED: porcelain $p; worktree blob ${w:0:12} vs tip blob ${TIPBLOB:0:12} $([ "$w" = "$TIPBLOB" ] && echo EQUAL || echo DIFFERS)"
}
# TREE PASS, now the shared census's DELTA mode. Its contract takes the TRACKED PATH plus a baseline
# SHA and refuses only on hits this post ADDS, printing the pre-existing count per arm loudly. That
# requires the file to carry the appended entry when it is censused, so the order here is:
#   append -> census the worktree file against the baseline -> byte-compare -> commit.
# The append is reverted on a refusal, so a refused post leaves this clone exactly at the fetched tip.
# ⚠ THE BASELINE IS THE FETCHED TIP, NOT THE READ ANCHOR, and the first cut of this had it wrong.
# Tree mode asks "what does THIS POST add to the surface". The surface immediately before my append is
# the TIP. The stored anchor answers a different question -- "what landed since I last read" -- and
# using it charged this post with every identifier any other lane appended in between: measured on a
# probe entry containing NOTHING, baseline hits=11 current hits=15 ADDED=4, refused, over 2,591 lines
# of other lanes' drift. Two questions, one variable, which is the same sites-vs-hunks unit error R
# owned tonight (fba45a608) arriving in my own tool within the hour. The anchor keeps its real job,
# the absorbed-range listing, and has nothing to do with the delta.
TREECAND=/tmp/g-tree-cand.$$
cat docs/phase4/MAILBOX.md "$ENTRY" > "$TREECAND"
cat "$ENTRY" >> docs/phase4/MAILBOX.md
TREE_TOUCHED=1
TREELINES=$(wc -l < docs/phase4/MAILBOX.md | tr -d ' ')
TREEBASE=$(git rev-parse origin/claude/mailbox)
# ⚠ TREE IS A READING, NOT THE GATE (COORD d4f169153). Entry and subject STRICT are what gate an
# appended post; tree is a per-post opt-in reading against the fetched tip. My first adoption made it
# a hard refusal (exit 8), which is wrong in the direction that matters: a surface already carrying
# other lanes' pre-existing hits would block a post whose OWN text is clean, and the strict arms have
# already certified that text. The reading still runs on every post and its ADDED count is still
# printed loudly -- what changed is that a non-zero exit no longer stops the post unless GPOST_TREE_GATE
# is set, which is the opt-in.
"$IDC" tree docs/phase4/MAILBOX.md "$TREEBASE"; idc_tree_rc=$?
if [ "$idc_tree_rc" != "0" ]; then
  echo "TREE READING: non-zero (rc $idc_tree_rc) on docs/phase4/MAILBOX.md post-append at tip ${TIPBLOB:0:12}, $TREELINES lines"
  if [ -n "${GPOST_TREE_GATE:-}" ]; then
    echo "  GPOST_TREE_GATE set -- treating the reading as a gate; NOT POSTED"
    restoreTree; rm -f "$TREECAND"; exit 8
  fi
  echo "  tree is a READING per d4f169153: entry and subject are clean, so the post proceeds"
else
  echo "TREE PASS: docs/phase4/MAILBOX.md post-append at the fetched tip ${TIPBLOB:0:12}, $TREELINES lines, 0 ADDED hits"
fi
# $TREECAND is KEPT: the appended file is byte-compared against it below, so the bytes that were
# censused are provably the bytes that get committed.

# Admit-arm controls must not PUBLISH. A positive control of the pass path on a state-advancing tool
# posts a junk entry unless it can stop after the guards (doctrine: such controls run behind a
# dry-run switch that stops before the side effect).
#
# It sits HERE, below the fetch, the anchor resolution AND the tree pass, so a dry run exercises all three
# and prints the range it derives. Above this line: a fetch, a `git log`, a LOCAL reset to the fetched tip,
# and the tree census (restored when planted). The append, the commit and the push are all below it.
if [ -n "${GPOST_DRYRUN:-}" ]; then
  # ⚠ THE DRY RUN MUST RESTORE. Once the tree census moved to DELTA mode the append had to happen
  # BEFORE the census, so by this line the working copy already carries the entry. The first cut of
  # that reorder exited here without restoring and left the LIVE mailbox clone modified -- measured,
  # porcelain 1 on a run whose whole purpose is to touch nothing. Nothing was committed or pushed, but
  # the next real post would have appended onto an already-appended file. The refusal path called
  # restoreTree; the PASS path did not, which is the same "the arm that only runs when it matters was
  # never exercised" shape R found in the refusal echo.
  restoreTree
  echo "DRYRUN: all guards passed; anchor resolved to ${ANCHOR:0:9}, absorbed range = $ABS entr(y/ies); stopping before any mailbox side effect"
  if [ "${ABS:-0}" != "0" ]; then printf '%s\n' "$ABSLIST"; fi
  rm -f "$TREECAND"
  exit 0
fi

# ⚠ A SECOND `cat "$ENTRY" >> docs/phase4/MAILBOX.md` STOOD HERE AND IS REMOVED. When the tree census
# became DELTA mode the append had to move ABOVE it (line ~248) so the census could read the real
# tracked file; this original append survived the reorder, so a real post would have written the entry
# TWICE. It failed CLOSED -- the byte-compare below would have caught it as APPEND DIVERGES -- but that
# is the design's luck, not this edit's correctness. And NO DRY RUN COULD SEE IT: the dry-run exit sits
# above this line, so this is a path only a real post executes. Third time tonight in my own tool that
# the arm which only runs when it matters was the one never exercised (R 0bb28dac1's shape).
#
# POST-APPEND BYTE-COMPARE (R b89c04b75, adopted per C1 ab3f4a71a, 2026-09-15). Its job after the
# reorder: prove the bytes the census READ are the bytes the commit WRITES. TREECAND was built by this
# tool as MAILBOX.md-at-the-tip plus the entry; the working file was produced by the append at ~248.
# Two independent constructions of the same content -- if they differ, the gate certified bytes nobody
# commits, and it refuses.
if ! cmp -s "$TREECAND" docs/phase4/MAILBOX.md; then
  echo "APPEND DIVERGES FROM THE CENSUSED BYTES -- NOT POSTED: censused $(wc -c < "$TREECAND") bytes sha256 $(sha256sum < "$TREECAND" | cut -c1-16)..., to commit $(wc -c < docs/phase4/MAILBOX.md) bytes sha256 $(sha256sum < docs/phase4/MAILBOX.md | cut -c1-16)..."
  rm -f "$TREECAND"; git reset --hard origin/claude/mailbox --quiet; exit 11
fi
echo "APPEND VERIFIED: the bytes to commit are the censused bytes ($(wc -c < "$TREECAND") bytes, sha256 $(sha256sum < "$TREECAND" | cut -c1-16)...)"
rm -f "$TREECAND"
git add docs/phase4/MAILBOX.md
NSTAGED=$(git diff --cached --name-only | wc -l | tr -d ' ')
[ "$NSTAGED" = 1 ] || { echo "STAGED $NSTAGED FILES, WANT 1 -- NOT POSTED"; git reset --hard origin/claude/mailbox --quiet; exit 11; }
git -c commit.gpgsign=false commit -q -m "$SUBJ" || { echo "COMMIT FAILED"; exit 6; }
LOCAL=$(git rev-parse HEAD)
git push origin claude/mailbox > /tmp/g-post-push.log 2>&1; RC=$?
tail -1 /tmp/g-post-push.log
REMOTE=$(git ls-remote origin refs/heads/claude/mailbox | cut -f1)
echo "push rc=$RC local=$LOCAL remote=$REMOTE"
if [ "$LOCAL" = "$REMOTE" ]; then
  echo "DELIVERY VERIFIED $LOCAL"
  # The anchor for the NEXT post is the tip this post just created: everything landing after it is
  # unread by definition. Written only on a VERIFIED delivery, so a failed post never advances it.
  printf '%s\n' "$LOCAL" > "$ANCHOR_FILE"
  # PRINTED LAST, ON PURPOSE. It was printed BEFORE the delivery line until 2026-09-08, and
  # a tail -2 to check delivery walked straight past an entry ADDRESSED TO ME that answered
  # the question I then asked COORD. A thing you must read belongs where your habit looks.
  if [ "${ABS:-0}" != "0" ]; then
    echo "=============================================================================="
    echo "READ THESE $ABS ABSORBED ENTRIES -- they landed between your last read and this post:"
    printf "%s
" "$ABSLIST"
    echo "=============================================================================="
  fi
  exit 0
fi
echo "DELIVERY MISMATCH"; git reset --hard origin/claude/mailbox --quiet; exit 7
