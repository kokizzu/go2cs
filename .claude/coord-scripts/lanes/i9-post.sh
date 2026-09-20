#!/usr/bin/env bash
# Post one mailbox entry through the GitHub Git Data API, with ZERO local git objects.
# Used when the local clone's object store is corrupt (recurring large-blob write failure).
#
# Keeps the same invariants the git gate enforces:
#   - the census DECIDES, on BOTH surfaces (body and commit message)
#   - append-only: the new blob must be the old blob plus our bytes, nothing else
#   - no conflict markers
#   - the ref update is verified by reading the ref back
#
# The mailbox blob is ~13 MB, so its base64 is ~17 MB -- far past the argv limit.
# Every large payload therefore goes to gh on STDIN via --input, never as an argument.
set -uo pipefail

# This box's Bash PATH is the WINDOWS semicolon form, which bash splits on ':' -- so /usr/bin is
# absent and gh/base64/python are not resolvable unless the tool prepends them ITSELF. Measured
# 2026-09-13: this script died `gh: command not found` AFTER its census had passed, i.e. it failed
# safe but it failed. A tool that runs outside the harness's own shell cannot inherit that prelude,
# so it carries its own, and gates EACH tool separately -- `command -v a b c` exits 0 if ANY of them
# resolve, which is fail-open (measured the same day on this box: four of five missing, rc 0).
export PATH="/usr/bin:/mingw64/bin:/c/Program Files/GitHub CLI:/c/Python312:$PATH"
for t in gh base64 python git grep sed cmp stat tr; do
  command -v "$t" >/dev/null 2>&1 || { echo "GATE ABORT: missing tool $t"; exit 2; }
done

# DURABLE LOG, added 2026-09-13 after C1 (51bdf62cf-era) found a truncating reader on their own post
# tool. My invocation pipes through `tr`, which consumes everything, so I was not exposed -- verified by
# reading all 15 i9 entries back out of the REMOTE file (sign-off and watcher line present on every one).
# The tool keeps its own log regardless, because "my caller does not truncate" is a discipline and this
# is a property: fix the tool's durability, not the caller's habit.
# ⚠ NOT `exec > >(tee "$POSTLOG")`. Measured 2026-09-13 after C1 (8b70238a0) found their published
# remedy defective and I had adopted it untested: tee writes to the file and to stdout in ONE loop, so
# when a truncating reader exits, tee SIGPIPEs and DIES mid-write -- the log is only as durable as the
# reader, which is the property the remedy claimed to remove. Reproduced here: a 201-line producer under
# `| head -3` left 31 lines and no final marker.
#
# The FILE is the primary, unconditional sink (no reader can close it); the caller's stdout is kept on
# fd 3 and the log is dumped there at exit, where a dead reader costs only the echo. Measured BOTH ways,
# which is the arm both of us skipped: under `| head -3` the log reads 201/201 with its final marker and
# PIPESTATUS[0] is the SCRIPT's rc, and unpiped the caller still sees every line.
# ⚠ PER-INVOCATION, added 2026-09-16. This was ONE shared path, and two invocations of this script
# (a post and a `--mark-read`) overlapped: the second `exec >` TRUNCATED the log the first was writing,
# so the post's transcript came back showing the other run's lines. The anchor advanced under the running
# post and its claim gate then refused it -- correctly, but for a reason invisible in its own corrupted
# log. A shared sink between concurrent instances is not a durable log; it is a race with a filename.
# The newest run is also linked to the stable name, so "the last post's log" is still one path.
# ⚠ THE STATE DIRECTORY, AND WHY IT REFUSES RATHER THAN RELOCATES (2026-09-20).
# This resolved to `dirname "$0"` -- correct for as long as the only copy lived outside the tree, and
# WRONG the moment a copy is committed inside one: the scratch dir below is `rm -rf`'d on every run, so
# a published copy run from its repo path would delete a path inside the work tree, not merely write to
# it. R found this on its own tool (e82b16d6d) and C2 confirmed the same latency on its own (bbe99c2817);
# this lane is the third. Both chose a REFUSAL over a relocated default, and that is right: moving the
# default answers this instance and not the next one, and a composition that cannot find its state
# correctly should fail closed rather than guess.
SP="${I9_STATE:-$(dirname "$0")}"
if git -C "$SP" rev-parse --is-inside-work-tree >/dev/null 2>&1; then
  echo "REFUSED: state dir '$SP' is inside a git work tree -- set I9_STATE to the lane scratchpad" >&2
  exit 2
fi
POSTLOG="$SP/i9-post-$$-$(date +%s).log"
ln -sf "$POSTLOG" "$SP/i9-post-last.log" 2>/dev/null || true
exec 3>&1
exec >"$POSTLOG" 2>&1
# ONE exit handler, composed -- C2 (cc3e20fd9) measured that a SECOND `trap ... EXIT` REPLACES the
# first in silence, which cost them their replay. Everything that must happen on exit goes in here.
# The END MARKER is the invariant: without it a log that is short because the writer DIED is
# indistinguishable from one that is short because the run was brief (a one-line refusal is a
# complete transcript). My sink is a plain redirect so a reader cannot truncate it -- verified under
# an early-closing reader -- but a crash or a kill still can, and the marker is what shows that.
i9_finish(){ rc=$?; printf -- '--- END OF LOG (rc=%d) ---\n' "$rc"; cat "$POSTLOG" >&3 2>/dev/null || true; }
trap i9_finish EXIT

REPO=ritchiecarroll/go2cs
BRANCH=claude/mailbox
PATHF=docs/phase4/MAILBOX.md
# SP is resolved at the top, above POSTLOG, because the log path derives from it.

# THE ANCHOR THE TOOL REMEMBERS, added 2026-09-14 at the resume (mailbox SKILL "Reading and anchors"; the
# resume prompt's POST TOOL clause). The stored file is the authority; the caller's LASTREAD argument is a
# CLAIM cross-checked against it, never the anchor itself. The anchor advances in exactly two ways: a
# `--mark-read <sha>` after a whole read (the sha must be at or behind the remote tip), or this tool's own
# delivered post (after the absorbed range is printed whole, below the delivery line).
ANCHORF="${I9_ANCHOR:-$SP/i9-mailbox-anchor.txt}"
is40hex(){ printf '%s' "$1" | grep -qxE '[0-9a-f]{40}'; }
if [ "${1:-}" = "--mark-read" ]; then
  S="${2:-}"; is40hex "$S" || { echo "GATE ABORT: --mark-read needs a full 40-hex sha, got '$S'"; exit 2; }
  TIPM=$(gh api "repos/$REPO/git/ref/heads/$BRANCH" --jq '.object.sha') || exit 2
  is40hex "$TIPM" || { echo "GATE ABORT: remote tip unreadable"; exit 2; }
  ST=$(gh api "repos/$REPO/compare/$S...$TIPM" --jq '.status') || { echo "GATE ABORT: compare failed (sha not on the remote?)"; exit 2; }
  case "$ST" in identical|ahead) ;; *) echo "GATE ABORT: $S is not at-or-behind the tip $TIPM (compare status $ST)"; exit 2;; esac
  echo "$S" > "$ANCHORF"; echo "ANCHOR SET: $(cat "$ANCHORF") (remote tip $TIPM, status $ST)"; exit 0
fi
BODY="$1"; MSG="$2"; CLAIM="${3:-}"
STORED=$(cat "$ANCHORF" 2>/dev/null | tr -d '[:space:]')
is40hex "$STORED" || { echo "GATE ABORT: no stored anchor at $ANCHORF -- read, then --mark-read <sha>"; exit 2; }
[ "$CLAIM" = "$STORED" ] || { echo "GATE ABORT: caller claims last-read '$CLAIM' but the tool's anchor is $STORED"; exit 2; }
echo "--- anchor: stored $STORED = caller's claim ---"
# ADOPTED 2026-09-16: THE FLEET'S ONE IDENTIFIER CENSUS (COORD e77b6d65fb2f, and the ruling that
# "each post tool calls entry, subject and tree ... and carries NO private copy of any arm").
# This tool no longer calls i9's census.sh. It carries no arm, no threshold, no exclusion and no
# hash of its own: all nineteen arms and the denied-name hashes live in the one definition, and
# i9's private census.sh and presweep are off every posting path.
#
# The local copy is PINNED and its sha is PRINTED on every run, so a stale copy is visible rather
# than silent. That is not hypothetical: the definition moved three times in the hour this was
# adopted (e77b6d6 -> 8343e98 -> fafcd9b), and a stale copy of the one definition is exactly as
# wrong as a private one.
CENSUS="${I9_CENSUS_DIR:-$SP/coord-scripts}/coord-identifier-census.sh"
CENSUSPIN="${I9_CENSUS_DIR:-$SP/coord-scripts}/PINNED-AT.txt"
[ -f "$CENSUS" ] || { echo "GATE ABORT: the shared census is not present at $CENSUS"; exit 2; }
echo "--- shared identifier census, pinned at $(cat "$CENSUSPIN" 2>/dev/null || echo UNKNOWN) ---"
W="$SP/apipost"; rm -rf "$W"; mkdir -p "$W"

# FRESHNESS, adopted from R (dd8f54f3b8) 2026-09-16. A local copy of the ONE definition goes stale
# SILENTLY, which is a private copy in all but name. i9's pin is PRINTED, which makes staleness visible
# but does not REFUSE it -- R's gate does, and R measured the same need i9 hit: master moved three times
# in the hour this was adopted and i9 fetched TWO copies that were already superseded before use.
#
# The LOCAL copy is what runs -- it is the one whose selftest i9 has actually read, and running a
# just-fetched script unread would trade a stale definition for an unvetted one. So the tool asserts the
# local copy is byte-identical to master AT THE ACT and REFUSES on any difference, which forces a
# deliberate re-fetch, re-selftest and re-pin rather than a silent drift in either direction.
for cf in coord-identifier-census.sh coord-identifier-patterns.txt coord-identifier-hashes.txt; do
  gh api "repos/$REPO/contents/.claude/coord-scripts/$cf?ref=master" -H "Accept: application/vnd.github.raw" > "$W/master.$cf" 2>/dev/null \
    || { echo "GATE ABORT: could not read $cf from master to check freshness -- an unchecked definition is not a fresh one"; exit 2; }
  LH=$(git hash-object --no-filters "$(dirname "$CENSUS")/$cf") || exit 2
  MH=$(git hash-object --no-filters "$W/master.$cf") || exit 2
  [ "$LH" = "$MH" ] || { echo "GATE ABORT: $cf differs from master ($LH vs $MH) -- the local pin is SUPERSEDED. Re-fetch, re-run selftest, re-pin, then post."; exit 2; }
done
echo "--- freshness VERIFIED: all three census files byte-identical to master at the act ---"

# THE GATE (COORD d4f169153c): `entry` + `subject`, BOTH STRICT -- no exclusion runs on either, so
# no lane can spell one for itself. `tree` is a READING and is opt-in, below.
# The BRANCH is a pushed surface too (COORD 8977703d8). It goes through the SAME shared strict mode
# rather than an arm of my own: `subject` censuses an arbitrary string, which is what a branch is.
echo "--- census (DECIDES): entry on the body, subject on the commit message and the branch name ---"
if ! bash "$CENSUS" entry "$BODY" >"$W/census.out" 2>&1; then
  echo "*** GATE REFUSED on the BODY -- nothing written ***"; cat "$W/census.out"; exit 1
fi
if ! bash "$CENSUS" subject "$(cat "$MSG")" >"$W/census.msg.out" 2>&1; then
  echo "*** GATE REFUSED on the COMMIT MESSAGE -- nothing written ***"; cat "$W/census.msg.out"; exit 1
fi
if ! bash "$CENSUS" subject "$BRANCH" >"$W/census.branch.out" 2>&1; then
  echo "*** GATE REFUSED on the BRANCH NAME -- nothing written ***"; cat "$W/census.branch.out"; exit 1
fi
echo "CENSUS CLEAN on body, commit message and branch name"

REF=$(gh api "repos/$REPO/git/ref/heads/$BRANCH" --jq '.object.sha') || exit 2
TREE=$(gh api "repos/$REPO/git/commits/$REF" --jq '.tree.sha')       || exit 2
echo "--- base commit $REF  tree $TREE ---"

BLOBSHA=$(gh api "repos/$REPO/git/trees/$TREE?recursive=1" \
          --jq ".tree[] | select(.path==\"$PATHF\") | .sha") || exit 2
[ -n "$BLOBSHA" ] || { echo "GATE ABORT: could not locate $PATHF in the tree"; exit 2; }

gh api "repos/$REPO/git/blobs/$BLOBSHA" --jq '.content' | tr -d '\n' > "$W/old.b64" || exit 2
base64 -d "$W/old.b64" > "$W/old.md" || exit 2
echo "--- fetched $(stat -c%s "$W/old.md") bytes ---"
# PRISTINE SNAPSHOT, taken BEFORE the control plant can reach the local copy. The tree reading's
# baseline must be the bytes the REMOTE holds. A delta census tolerates pre-existing hits BY DESIGN,
# so a plant sitting in the baseline as well as the candidate would score added=0 -- the control would
# go silent while still reading green. With the pristine baseline the plant is in the candidate only:
# measured 2026-09-16 with live material, added=1, REFUSED.
# (i9's FIRST red control read added=0. NOT this cause: that plant was built for the old 13-arm
# census.sh and is INERT against the shared nineteen -- `entry` on the plant file itself reads CLEAN.
# A red control needs its own control, and this one did not have one until it was given one.)
cp "$W/old.md" "$W/pristine.md" || exit 2

# ⚠ THE DUPLICATE-POST DEFENCE, restored 2026-09-13. It existed in the git-based tool this one
# REPLACED (i9-post-v36.sh:66-68 refusing before the append, :109-112 verifying after the push), and the
# migration to the Git Data API silently dropped BOTH. Found by this tool's own new dry run: I pointed it
# at an already-posted entry expecting a refusal and it reported "every gate ran and PASSED".
#
# The append-only gate CANNOT substitute: appending the same entry twice IS a valid append -- the prefix
# is unchanged and bytes are added. It answers "did anything change above?", never "is this already here?".
# Measured when found: 124 headings in the live file, all distinct, 19 of them mine -- so nothing had
# duplicated, by discipline rather than by a guard, which is the same "working fine" evidence the tee
# remedy had.
HEADING=$(grep -m1 '^## ' "$BODY" || true)
[ -n "$HEADING" ] || { echo "GATE ABORT: body has no '## ' heading"; exit 2; }
PRE=$(grep -cxF "$HEADING" "$W/old.md" || true)
[ "$PRE" -eq 0 ] || { echo "GATE ABORT: heading ALREADY present $PRE time(s) at the remote -- refusing to duplicate"; exit 2; }
echo "--- heading is NEW at the remote (0 occurrences) ---"

# CONTROL-ONLY PLANT (COORD ruling ef0c5c7c98, mandatory control 1): a line placed ALREADY IN the fetched file,
# with a clean entry, to prove the tree census below refuses a tree that is dirty before this post touches it.
# The plant is appended to the LOCAL fetched copy only; it is REFUSED unless I9POST_DRYRUN=1, so no real post
# can ever carry it and nothing reaches the remote.
if [ -n "${I9POST_PLANT_FILE:-}" ]; then
  [ "${I9POST_DRYRUN:-0}" = "1" ] || { echo "GATE ABORT: I9POST_PLANT_FILE is a control and requires I9POST_DRYRUN=1"; exit 2; }
  PLB=$(stat -c%s "$W/old.md")
  cat "$I9POST_PLANT_FILE" >> "$W/old.md" || exit 2
  # A plant that is not the change it claims does not test the gate (R b89c04b759): assert the fetched copy grew by
  # EXACTLY the plant file's bytes, and that the plant is exactly one line.
  [ $(( $(stat -c%s "$W/old.md") - PLB )) -eq "$(stat -c%s "$I9POST_PLANT_FILE")" ] || { echo "GATE ABORT: the plant did not add exactly its own bytes"; exit 2; }
  [ "$(wc -l < "$I9POST_PLANT_FILE")" -eq 1 ] || { echo "GATE ABORT: the plant is not exactly one line"; exit 2; }
  echo "--- CONTROL: planted exactly 1 line ($(stat -c%s "$I9POST_PLANT_FILE") bytes) into the LOCAL fetched copy (dry run only) ---"
fi

cat "$W/old.md" "$BODY" > "$W/new.md"

OLDLEN=$(stat -c%s "$W/old.md")
head -c "$OLDLEN" "$W/new.md" > "$W/prefix.md"
cmp -s "$W/old.md" "$W/prefix.md" || { echo "GATE ABORT: not an append"; exit 2; }
echo "--- append-only VERIFIED: +$(( $(stat -c%s "$W/new.md") - OLDLEN )) bytes, 0 changed ---"
[ "$(grep -cE '^(<{7}|={7}|>{7})[[:space:]]*$' "$W/new.md")" = 0 ] || { echo "GATE ABORT: conflict markers"; exit 2; }

# THE TREE READING (COORD d4f169153c: "tree is a READING, not the gate ... the per-post tree call is opt-in
# from here"). Kept and opt-in via I9POST_TREE=1, because it answers the one question a whole-file census
# cannot: what does THIS post ADD to the shared surface.
#
# BASELINE = THE FETCHED TIP, ruled at d4f169153c after C2 measured (3cd0ce25e3 §1) that a LAST-READ baseline
# counts every entry landing between a lane's read and its push as that lane's own -- a busy mailbox then
# refuses whichever lane is posting, which is the outage the delta was ruled in to END. i9 reached the same
# shape independently while wiring this and reports it as CORROBORATION, not as a second discovery.
#
# ⚠ tree mode requires a GIT REPOSITORY: it runs `git rev-parse --git-dir` and reads its baseline with
# `git show <base>:<path>`. This tool posts with ZERO local git objects BY DESIGN, because the mailbox clones'
# object stores keep corrupting -- both were found corrupt again on 2026-09-16, on DIFFERENT objects, and the
# second failure was not retried (retrying is what destroyed the previous one). So the reading runs in a
# THROWAWAY host repo, and its baseline identity is PROVEN rather than asserted: the materialized blob's sha
# must equal the blob sha the API reported for the tip, or the reading refuses as unfounded.
if [ "${I9POST_TREE:-0}" = "1" ]; then
  CHOST="${I9_CENSUS_HOST:-$SP/i9-census-host}"; CF=docs/phase4/MAILBOX.md
  mkdir -p "$CHOST/docs/phase4" || exit 2
  [ -d "$CHOST/.git" ] || git init -q "$CHOST" || exit 2
  cp "$W/pristine.md" "$CHOST/$CF" || exit 2
  CBLOB=$(git -C "$CHOST" hash-object -w "$CF") || exit 2
  [ "$CBLOB" = "$BLOBSHA" ] || { echo "*** TREE READING REFUSED: materialized baseline $CBLOB != the tip's blob $BLOBSHA -- an unfounded baseline is not a clean read ***"; exit 2; }
  CT1=$(printf '100644 blob %s\tMAILBOX.md\n' "$CBLOB" | git -C "$CHOST" mktree) || exit 2
  CT2=$(printf '040000 tree %s\tphase4\n' "$CT1" | git -C "$CHOST" mktree) || exit 2
  CT3=$(printf '040000 tree %s\tdocs\n' "$CT2" | git -C "$CHOST" mktree) || exit 2
  CBASE=$(git -C "$CHOST" -c user.name=i9 -c user.email=i9@local commit-tree "$CT3" -m baseline) || exit 2
  cp "$W/new.md" "$CHOST/$CF" || exit 2
  echo "--- tree reading: the post-append file against the FETCHED TIP $REF (baseline blob $CBLOB, proven equal to the tip's) ---"
  ( cd "$CHOST" && bash "$CENSUS" tree "$CF" "$CBASE" ) >"$W/tree-census.out" 2>&1
  TRC=$?
  # ⚠ THE CAUSE IS THE CENSUS'S TO STATE, NOT THIS WRAPPER'S. Until 2026-09-16 this arm printed
  # "this post ADDS an identifier" for EVERY non-zero exit. The census distinguishes them: rc 1 is
  # "added", rc 2 is an INSTRUMENT FAILURE ("an instrument failure is not a clean read"). Measured the
  # day this was fixed: the census's awk exited 134 and this tool reported it as an identifier i9 had
  # written -- a message asserting a cause it never measured, pointing the next reader at the wrong one.
  if [ "$TRC" -eq 1 ]; then
    echo "*** TREE READING REFUSED (rc 1): this post ADDS an identifier -- nothing written ***"; cat "$W/tree-census.out"; exit 2
  elif [ "$TRC" -ne 0 ]; then
    echo "*** TREE READING UNAVAILABLE (rc $TRC): the census could not complete -- this is NOT a finding about this post's content ***"
    echo "*** the GATE is entry + subject, both strict and both CLEAN above (COORD d4f169153); re-run without I9POST_TREE to post ***"
    cat "$W/tree-census.out"; exit 2
  fi
  grep -E 'added=|PRE-EXISTING|ADDED BY|^ +(ipv4|profile_root|home_unix|unc_)' "$W/tree-census.out" | sed 's/^/    /'
  echo "TREE READING CLEAN at tip $REF: this post adds no identifier"
else
  echo "--- tree reading SKIPPED (opt-in, I9POST_TREE=1). The GATE is entry + subject, both strict (COORD d4f169153c) ---"
fi

# DRY RUN, added 2026-09-13. C1 (7e05c2878 §2) found their post tool had no dry run by READING it
# rather than recalling it, after G described their own instrument from memory and was wrong. I read
# mine: it had none either, so every claim I had made about it rested on the REFUSE arm plus reading
# landed entries back out of the remote -- never on a full path that was allowed to run and then stop.
#
# ⚠ WHAT THIS COVERS AND WHAT IT CANNOT. It runs the census on BOTH surfaces plus the branch name, the
# remote fetch, the blob decode, the APPEND-ONLY verification, the conflict-marker gate and the request
# JSON construction -- every GATE. It stops before the four API writes, so it writes NOTHING to the
# remote: no blob, no tree, no commit, no ref. The four writes are therefore still uncovered, and they
# cannot be covered without creating remote objects; each carries its own `|| exit 2` and the ref update
# is verified by a read-back. Saying which half is untested, rather than calling this a full-path control.
if [ "${I9POST_DRYRUN:-0}" = "1" ]; then
  echo "--- DRY RUN: every gate ran and PASSED; stopping before the first API write ---"
  echo "--- nothing written: 0 blobs, 0 trees, 0 commits, the ref UNMOVED at $REF ---"
  exit 0
fi

base64 -w0 "$W/new.md" > "$W/new.b64"
python -c "import json,io,sys; b=io.open(sys.argv[1],encoding='ascii').read().strip(); io.open(sys.argv[2],'w',encoding='ascii').write(json.dumps({'content':b,'encoding':'base64'}))" "$W/new.b64" "$W/blob.json" || exit 2
NEWBLOB=$(gh api -X POST "repos/$REPO/git/blobs" --input "$W/blob.json" --jq '.sha') || exit 2
echo "--- new blob $NEWBLOB ---"
# BYTE-COMPARE before the commit (R b89c04b759, C1 ab3f4a71ab): the census scanned $W/new.md; the blob is a SECOND
# construction of those bytes (base64 + JSON + the API). The git blob id of the censused file must equal the blob the
# remote stored, or the commit would carry bytes no census read. --no-filters hashes raw bytes; no object is written.
LOCALBLOB=$(git hash-object --no-filters "$W/new.md") || exit 2
[ "$LOCALBLOB" = "$NEWBLOB" ] || { echo "*** GATE REFUSED: stored blob $NEWBLOB != censused bytes $LOCALBLOB -- no tree, commit or ref written ***"; exit 2; }
echo "--- byte-compare VERIFIED: the stored blob IS the censused file ($LOCALBLOB) ---"

python -c "import json,io,sys; io.open(sys.argv[4],'w',encoding='utf-8').write(json.dumps({'base_tree':sys.argv[1],'tree':[{'path':sys.argv[2],'mode':'100644','type':'blob','sha':sys.argv[3]}]}))" "$TREE" "$PATHF" "$NEWBLOB" "$W/tree.json" || exit 2
NEWTREE=$(gh api -X POST "repos/$REPO/git/trees" --input "$W/tree.json" --jq '.sha') || exit 2
echo "--- new tree $NEWTREE ---"

python -c "import json,io,sys; io.open(sys.argv[4],'w',encoding='utf-8').write(json.dumps({'message':io.open(sys.argv[1],encoding='utf-8').read(),'tree':sys.argv[2],'parents':[sys.argv[3]]}))" "$MSG" "$NEWTREE" "$REF" "$W/commit.json" || exit 2
NEWCOMMIT=$(gh api -X POST "repos/$REPO/git/commits" --input "$W/commit.json" --jq '.sha') || exit 2
# NOT yet an announcement: this commit exists as an object but is on no branch until the ref update below lands. A lost
# race (HTTP 422, not a fast forward) leaves it unreferenced -- measured twice on 2026-09-15 -- so this line must never be
# quoted as a posted SHA; only the "CONFIRMED from remote" line is.
echo "--- commit object created, NOT yet on the branch: $NEWCOMMIT ---"

python -c "import json,io,sys; io.open(sys.argv[2],'w',encoding='utf-8').write(json.dumps({'sha':sys.argv[1],'force':False}))" "$NEWCOMMIT" "$W/ref.json" || exit 2
gh api -X PATCH "repos/$REPO/git/refs/heads/$BRANCH" --input "$W/ref.json" >/dev/null || exit 2
AFTER=$(gh api "repos/$REPO/git/ref/heads/$BRANCH" --jq '.object.sha')
if [ "$AFTER" = "$NEWCOMMIT" ]; then
  # The DELIVERY check, also dropped in the migration: the ref moving proves a commit landed, not that
  # THIS entry is in it exactly once. Keyed on the HEADING read back from the remote, never on the
  # commit subject.
  # ⚠ CONTENT-ADDRESSED, changed 2026-09-16. This read the Contents API BY BRANCH REF, which is served
  # from a cache: measured this day, the ref had already moved to this very commit and the branch read
  # still returned a body with 0 occurrences, so the gate cried DELIVERY SUSPECT on a post that had
  # LANDED. That is the dangerous direction for a duplicate defence -- the natural answer to it is to
  # post again, which is how the duplicate it exists to prevent would actually get created.
  # The blob is content-addressed: its sha IS its bytes, so it cannot be stale. The ref was already
  # verified equal to this commit above, and that blob is the one this commit carries.
  POST=$(gh api "repos/$REPO/git/blobs/$NEWBLOB" --jq '.content' | tr -d '\n' | base64 -d | grep -cxF "$HEADING" || true)
  [ "$POST" -eq 1 ] || { echo "*** DELIVERY SUSPECT: heading occurs $POST time(s) at the remote (want 1) ***"; exit 1; }
  echo "--- delivery VERIFIED: the heading occurs exactly 1 time at the remote ---"
  echo "CONFIRMED from remote: $NEWCOMMIT"
  # ABSORBED RANGE, printed WHOLE and AFTER the delivery line (SKILL: never tail it; entries here are owed a read).
  echo "=================== ABSORBED RANGE: $STORED .. $REF (read every line below) ==================="
  if [ "$STORED" = "$REF" ]; then
    echo "(empty: the stored anchor was the base -- nothing absorbed)"
  else
    gh api "repos/$REPO/compare/$STORED...$REF" --jq '(.commits[] | .sha + " " + (.commit.message|split("\n")[0]))' || { echo "*** ABSORBED LISTING FAILED -- anchor NOT advanced ***"; exit 1; }
    gh api "repos/$REPO/contents/$PATHF?ref=$STORED" -H "Accept: application/vnd.github.raw" > "$W/anchor.md" || { echo "*** ABSORBED READ FAILED -- anchor NOT advanced ***"; exit 1; }
    AL=$(stat -c%s "$W/anchor.md")
    head -c "$AL" "$W/old.md" | cmp -s - "$W/anchor.md" || { echo "*** HISTORY REWRITTEN? anchor blob is not a prefix of the base -- anchor NOT advanced ***"; exit 1; }
    tail -c +$((AL+1)) "$W/old.md"
  fi
  echo "=================== END ABSORBED RANGE ==================="
  echo "$NEWCOMMIT" > "$ANCHORF"; echo "ANCHOR ADVANCED to $(cat "$ANCHORF")"
  rm -rf "$W"
else
  echo "*** NOT LANDED -- remote is $AFTER, mine is $NEWCOMMIT ***"; exit 1
fi
