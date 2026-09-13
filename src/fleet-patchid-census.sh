#!/usr/bin/env bash
# fleet-patchid-census.sh -- index every branch's commits by CONTENT and report what appears twice.
#
# THIS IS A CENSUS, NOT A TRAIN GATE. Its sibling src/seat-duplication-census.sh takes a SEAT LIST and
# refuses a train; this one takes a branch GLOB and answers "what does the whole remote carry twice".
# Over a remote that has been worked for weeks almost every reading is a LEGITIMATE supersession -- a
# re-cut replacing the branch it supersedes patch-matches it by construction -- so a red here is a
# QUESTION, not a verdict. Measured 2026-09-13 over 120 branches: 17 rows, of which 13 were superseded
# lineages, a declared probe pair, or clean re-cuts of the very branches they replace.
#
# WHY PATCH-ID AND NOT ANCESTRY. A lane that develops an item on its own branch and cherry-picks it
# onto a seat gives the same content a NEW SHA, so `merge-base --is-ancestor` reads clean on both while
# the diff sits on both. `git patch-id --stable` hashes the DIFF, so it sees through the re-application.
# Ancestry is not merely weaker here, it is WRONG IN BOTH DIRECTIONS, which is why no arm consults it:
# measured the same day, the fleet's real contamination WAS ancestor-related (0dab47858 an ancestor of
# 21222f2e8) while the cherry-pick that made it visible was not -- so "exempt the ancestors" would have
# spared the defect and refused the legitimate stack, exactly backwards.
#
# THE TWO CLASSES, as ruled (coordinator, mailbox c53db4e3a). They are not the same finding, and the
# report never merges them:
#   STACK      one commit SHA present on several branches. Git merges it once, so the union carries the
#              content once; the hazard is the RECORD -- which branch owns what -- not the tree. The
#              fleet builds these ON PURPOSE (a docs seat cut on the seat before it), so a census that
#              called this a duplicate would refuse the shape its own coordinator ruled. Measured: the
#              H6 chain 898cbfefe -> 191164e7a -> 47592cb3f, three lanes, all declared.
#   DUPLICATE  one patch-id under TWO OR MORE SHAs. The same content would land twice and a merge sees
#              two unrelated commits. This is the silent-duplication class, and it is what the exit
#              code keys on.
#
# usage:
#   src/fleet-patchid-census.sh [--base <ref>] [--glob <ref-glob>] [--ref <ref>]...
#   src/fleet-patchid-census.sh --self-test
#
# Exit 0 = no DUPLICATE rows. Exit 1 = at least one, each named with every branch carrying it.
# Exit 2 = misuse, INCLUDING fewer than two refs: a duplication census over one ref cannot go red, and
# a census that cannot go red must refuse rather than report clean.
set -uo pipefail

BASE="origin/master"
GLOB="refs/remotes/origin/claude/*"
SELFTEST=0
EXTRA=()

usage() {
  echo "usage: src/fleet-patchid-census.sh [--base <ref>] [--glob <ref-glob>] [--ref <ref>]..."
  echo "       src/fleet-patchid-census.sh --self-test"
}

# ---------------------------------------------------------------------------
# The census itself. Every number it prints is one it computed, and the verdict is DERIVED from the
# counts rather than asserted beside them -- a hardcoded verdict line is a check that cannot go red,
# and this repository has paid for that one more than once.
#
# The counters live in this shell, never inside a pipeline: a `for ... done | sort` increments its
# counters in a SUBSHELL and the totals read zero afterwards while every row printed correctly. That
# defect was in this instrument's own first draft.
# ---------------------------------------------------------------------------
census() {
  local base="$1" glob="$2"
  shift 2
  local refs=() r
  while IFS= read -r r; do
    [ -n "$r" ] && refs+=("$r")
  done < <(git for-each-ref --format='%(refname)' "$glob" 2>/dev/null)
  for r in "$@"; do [ -n "$r" ] && refs+=("$r"); done

  echo "== fleet patch-id census"
  echo "   base : $base"
  echo "   glob : $glob"
  echo "   refs : ${#refs[@]}"

  if ! git rev-parse --verify -q "${base}^{commit}" >/dev/null 2>&1; then
    echo "REFUSE: base '$base' does not resolve to a commit" >&2
    return 2
  fi
  # A census over one ref cannot go red. Refusing is the only honest answer: reporting CLEAN would be
  # a green earned by the population rather than by the tree.
  if [ "${#refs[@]}" -lt 2 ]; then
    echo "REFUSE: fewer than two refs -- a duplication census over one ref cannot go red" >&2
    return 2
  fi

  local idx dups stackf dupf
  idx="$(mktemp)"; dups="$(mktemp)"; stackf="$(mktemp)"; dupf="$(mktemp)"
  local indexed=0 merges=0 nopatch=0 c pid all nom

  for r in "${refs[@]}"; do
    if ! git rev-parse --verify -q "${r}^{commit}" >/dev/null 2>&1; then
      echo "REFUSE: ref '$r' does not resolve to a commit" >&2
      rm -f "$idx" "$dups" "$stackf" "$dupf"
      return 2
    fi
    all="$(git rev-list --count "${base}..${r}")"
    nom="$(git rev-list --count --no-merges "${base}..${r}")"
    # A merge has no single diff, so patch-id returns nothing for it. Excluded ON PURPOSE and COUNTED:
    # a silent exclusion is how a census loses the thing it was asked about, and the first hand-cut of
    # this class in the fleet threw a bad-subscript error on exactly that and its author read past it.
    merges=$(( merges + all - nom ))
    for c in $(git rev-list --no-merges "${base}..${r}"); do
      indexed=$(( indexed + 1 ))
      pid="$(git show "$c" | git patch-id --stable | cut -d' ' -f1)"
      if [ -z "$pid" ]; then
        nopatch=$(( nopatch + 1 ))
        continue
      fi
      printf '%s %s %s\n' "$pid" "$r" "$c" >> "$idx"
    done
  done

  echo "   indexed: $indexed non-merge commit(s); $merges merge commit(s) excluded (no single diff to hash); $nopatch with an empty diff"

  if [ "$indexed" -lt 1 ]; then
    echo "REFUSE: indexed 0 commits -- the enumeration is broken, and a census that scanned nothing passes everything" >&2
    rm -f "$idx" "$dups" "$stackf" "$dupf"
    return 2
  fi

  # A patch-id carried by two or more REFS. Deduplicated by (patch-id, ref) first, so one branch
  # carrying the same patch twice is not mistaken for two branches carrying it once.
  cut -d' ' -f1,2 "$idx" | sort -u | cut -d' ' -f1 | sort | uniq -d > "$dups"

  local d nshas
  while IFS= read -r d; do
    [ -n "$d" ] || continue
    nshas="$(grep "^$d " "$idx" | cut -d' ' -f3 | sort -u | grep -c .)"
    if [ "$nshas" -eq 1 ]; then
      printf '%s\n' "$d" >> "$stackf"
    else
      printf '%s\n' "$d" >> "$dupf"
    fi
  done < "$dups"

  local nstack ndup
  nstack="$(grep -c . "$stackf" 2>/dev/null)"; nstack="${nstack:-0}"
  ndup="$(grep -c . "$dupf" 2>/dev/null)"; ndup="${ndup:-0}"

  local kind file br sha
  for kind in STACK DUPLICATE; do
    if [ "$kind" = "STACK" ]; then file="$stackf"; else file="$dupf"; fi
    while IFS= read -r d; do
      [ -n "$d" ] || continue
      echo ""
      echo "$kind patch-id ${d:0:12}"
      while read -r _ br sha; do
        printf '   %s|%s   %s\n' "$br" "$sha" "$(git log -1 --format=%s "$sha")"
      done < <(grep "^$d " "$idx")
    done < "$file"
  done

  echo ""
  echo "==> CENSUS: $ndup DUPLICATE patch-id(s) (one diff under two or more SHAs) and $nstack STACK patch-id(s) (one SHA on several refs), $indexed commit(s) compared"
  rm -f "$idx" "$dups" "$stackf" "$dupf"
  [ "$ndup" -eq 0 ] && return 0
  return 1
}

# ---------------------------------------------------------------------------
# The self-test. A hermetic repository in a temp dir -- no network, no clone, nothing outside it -- and
# every arm is RED-FIRST: the instrument is made to report the finding before it is trusted to report
# its absence. Arm 4 is what keeps arms 1 and 3 honest; without it, a census that reported EVERYTHING
# would pass them both and no arm would say so.
# ---------------------------------------------------------------------------
self_test() {
  local tmp rc out arms=0
  tmp="$(mktemp -d)" || { echo "SELF-TEST FAILED: cannot create a temp dir"; return 1; }
  echo "fleet-patchid-census self-test -- hermetic repo at $tmp"

  (
    cd "$tmp" || exit 1
    export HOME="$tmp" GIT_CONFIG_NOSYSTEM=1 GIT_CONFIG_GLOBAL=/dev/null
    git init -q -b main . || exit 1
    git config user.name "self test"
    git config user.email "self@test"
    git config commit.gpgsign false
    echo base > base.txt; git add base.txt; git commit -q -m "base" || exit 1

    # The lane workbench. It carries an EARLIER item, and that is not decoration: a cherry-pick onto
    # the same parent, with the same tree, author and second, produces a BYTE-IDENTICAL COMMIT and
    # therefore the SAME SHA -- so the arm below would have been handed a stack to find instead of a
    # duplicate. Measured: this self-test's own first run reported STACK 1 / DUPLICATE 0 and arm 1
    # failed, which is the arm doing its job on its own setup.
    git checkout -q -b lane
    echo workbench > w.txt; git add w.txt; git commit -q -m "lane workbench" || exit 1
    echo "item A" > a.txt; git add a.txt; git commit -q -m "item A" || exit 1
    git rev-parse HEAD > itemA.sha

    # SEAT A: the same item CHERRY-PICKED onto the base -- a new SHA for the same diff.
    git checkout -q -b seat-a main
    git cherry-pick "$(cat itemA.sha)" >/dev/null 2>&1 || { echo "SETUP FAILED: cherry-pick did not apply" >&2; exit 1; }
    # The tell of a setup that silently did nothing is a branch that should carry a commit and does
    # not. Assert it rather than trust the exit status of a command that may have printed usage and
    # exited 0 -- that exact shape cost another lane its first self-test run the same day.
    [ "$(git rev-list --count main..seat-a)" -eq 1 ] || { echo "SETUP FAILED: seat-a carries no commit" >&2; exit 1; }
    # And assert the premise of arm 1 explicitly, at the setup, where a failure names its cause.
    [ "$(git rev-parse HEAD)" != "$(cat itemA.sha)" ] || { echo "SETUP FAILED: the cherry-pick reproduced the SAME SHA, so arm 1 would be handed a stack" >&2; exit 1; }

    # SEAT B: cut ON seat-a, so it carries seat-a's commit by the SAME SHA -- a stack's shape.
    git checkout -q -b seat-b seat-a
    echo "item B" > b.txt; git add b.txt; git commit -q -m "item B" || exit 1

    # SEAT C: disjoint from everything above.
    git checkout -q -b seat-c main
    echo "item C" > c.txt; git add c.txt; git commit -q -m "item C" || exit 1

    # SEAT D: carries a MERGE, so the merge-exclusion counter has something to count.
    git checkout -q -b seat-d main
    echo "item D" > d.txt; git add d.txt; git commit -q -m "item D" || exit 1
    git merge -q --no-ff -m "merge seat-c into seat-d" seat-c >/dev/null 2>&1 || { echo "SETUP FAILED: merge" >&2; exit 1; }
    [ "$(git rev-list --count --merges main..seat-d)" -eq 1 ] || { echo "SETUP FAILED: seat-d carries no merge" >&2; exit 1; }
    git checkout -q main
  ) || { rm -rf "$tmp"; echo "SELF-TEST FAILED: hermetic setup"; return 1; }

  run() { ( cd "$tmp" && export HOME="$tmp" GIT_CONFIG_NOSYSTEM=1 GIT_CONFIG_GLOBAL=/dev/null && bash "$SCRIPT_PATH" "$@" ); }

  # A ROW header, anchored at line start -- see the note on arm 1.
  has_row() { printf '%s\n' "$1" | grep -q "^$2 patch-id "; }

  # ARM 1 (RED): a cherry-picked duplicate must be FOUND and NAMED as a DUPLICATE.
  out="$(run --base main --glob refs/heads/lane --ref refs/heads/seat-a 2>&1)"; rc=$?
  if [ "$rc" -ne 1 ]; then echo "ARM 1 FAILED: wanted exit 1 on a cherry-picked duplicate, got $rc"; echo "$out"; rm -rf "$tmp"; return 1; fi
  # ROW HEADERS ARE MATCHED AT LINE START, never as a substring: the VERDICT line contains the words
  # "0 DUPLICATE patch-id(s)", so a `case $out in *"DUPLICATE patch-id"*` reads TRUE on a clean run.
  # That defect was in arm 3 of this file's first draft and arm 3 caught it -- an assertion loose
  # enough to match the report's own summary is an assertion about the wording, not about the finding.
  if ! has_row "$out" DUPLICATE; then echo "ARM 1 FAILED: red exit without naming the duplicate"; echo "$out"; rm -rf "$tmp"; return 1; fi
  case "$out" in *"item A"*) ;; *) echo "ARM 1 FAILED: did not NAME the duplicated commit"; echo "$out"; rm -rf "$tmp"; return 1 ;; esac
  arms=$((arms+1)); echo "  ok   cherry-picked duplicate FOUND and named   (item A, on lane and seat-a, under two SHAs)"

  # ARM 2 (the reason this tool exists): ancestry is BLIND to the duplicate arm 1 just found.
  if ( cd "$tmp" && git merge-base --is-ancestor refs/heads/lane refs/heads/seat-a ) >/dev/null 2>&1; then
    echo "ARM 2 FAILED: ancestry saw the cherry-pick, so this instrument's premise does not hold here"; rm -rf "$tmp"; return 1
  fi
  arms=$((arms+1)); echo "  ok   ancestry BLIND to that same duplicate     (is-ancestor false while the content is on both)"

  # ARM 3 (the classification this tool adds): one SHA on two refs is a STACK, never a DUPLICATE.
  out="$(run --base main --glob refs/heads/seat-a --ref refs/heads/seat-b 2>&1)"; rc=$?
  if [ "$rc" -ne 0 ]; then echo "ARM 3 FAILED: wanted exit 0 for a stack, got $rc"; echo "$out"; rm -rf "$tmp"; return 1; fi
  if ! has_row "$out" STACK; then echo "ARM 3 FAILED: the shared commit was not reported as a STACK"; echo "$out"; rm -rf "$tmp"; return 1; fi
  if has_row "$out" DUPLICATE; then echo "ARM 3 FAILED: a stack was reported as a DUPLICATE"; echo "$out"; rm -rf "$tmp"; return 1; fi
  arms=$((arms+1)); echo "  ok   one SHA on two refs classified as STACK   (and NOT as a duplicate)"

  # ARM 4 (GREEN): disjoint refs must read clean, or the reds above prove nothing.
  out="$(run --base main --glob refs/heads/seat-c --ref refs/heads/seat-d 2>&1)"; rc=$?
  if [ "$rc" -ne 0 ]; then echo "ARM 4 FAILED: wanted exit 0 on disjoint refs, got $rc"; echo "$out"; rm -rf "$tmp"; return 1; fi
  case "$out" in *"0 DUPLICATE patch-id(s)"*) ;; *) echo "ARM 4 FAILED: clean exit without a clean verdict line"; echo "$out"; rm -rf "$tmp"; return 1 ;; esac
  arms=$((arms+1)); echo "  ok   disjoint refs read CLEAN                  (so the reds discriminate rather than being constant)"

  # ARM 5 (MISUSE): one ref must REFUSE, because such a census cannot go red.
  out="$(run --base main --glob refs/heads/seat-c 2>&1)"; rc=$?
  if [ "$rc" -ne 2 ]; then echo "ARM 5 FAILED: wanted exit 2 on a single ref, got $rc"; echo "$out"; rm -rf "$tmp"; return 1; fi
  case "$out" in *"cannot go red"*) ;; *) echo "ARM 5 FAILED: refused without naming the reason"; echo "$out"; rm -rf "$tmp"; return 1 ;; esac
  arms=$((arms+1)); echo "  ok   a single-ref census REFUSES               (it cannot go red, so it must not report clean)"

  # ARM 6: a merge is excluded AND COUNTED. Seat D carries exactly one.
  out="$(run --base main --glob refs/heads/seat-c --ref refs/heads/seat-d 2>&1)"
  case "$out" in *"1 merge commit(s) excluded"*) ;; *) echo "ARM 6 FAILED: the merge was not excluded AND counted"; echo "$out"; rm -rf "$tmp"; return 1 ;; esac
  arms=$((arms+1)); echo "  ok   merge commits EXCLUDED and COUNTED        (a silent exclusion loses what the census was asked about)"

  rm -rf "$tmp"
  if [ "$arms" -ne 6 ]; then echo "SELF-TEST FAILED: $arms arm(s) ran, expected 6"; return 1; fi
  echo "SELF-TEST CLEAN -- 6 arms"
  return 0
}

SCRIPT_PATH="$(cd "$(dirname "$0")" && pwd)/$(basename "$0")"

while [ $# -gt 0 ]; do
  case "$1" in
    --base) BASE="${2:-}"; shift 2 ;;
    --glob) GLOB="${2:-}"; shift 2 ;;
    --ref)  EXTRA+=("${2:-}"); shift 2 ;;
    --self-test) SELFTEST=1; shift ;;
    -h|--help) usage; exit 2 ;;
    *) echo "unknown argument: $1" >&2; usage >&2; exit 2 ;;
  esac
done

if [ "$SELFTEST" -eq 1 ]; then
  self_test
  exit $?
fi

census "$BASE" "$GLOB" ${EXTRA+"${EXTRA[@]}"}
exit $?
