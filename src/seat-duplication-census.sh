#!/usr/bin/env bash
# seat-duplication-census.sh -- refuse a train whose seats carry the same CONTENT twice.
#
# WHY THIS EXISTS, and why ancestry cannot do it. A lane that develops an item on its own branch and
# CHERRY-PICKS it onto a seat gives the same content a NEW SHA. Every ancestry check then reads clean:
# `git merge-base --is-ancestor <seat-sha> <other>` is false, `git log <base>..<seat>` lists a commit
# nobody recognises as a duplicate, and the two seats look disjoint right up until the train lands the
# same diff twice. Measured on 2026-09-13: C1's own c1-gctestisreachable and c1-mfinal-mint-door each
# carried THREE items already boarding as c1-lockosthread-body, c1-getcallerpc-erratum and
# c1-crashwhiletracing-marking -- and the contamination reached an acceptance run, where a tree
# carrying an unrelated seat was read as evidence about the seat under test.
#
# `git patch-id --stable` hashes the DIFF rather than the commit, so it is invariant across the
# cherry-pick, the rebase and the committer -- which is exactly the axis ancestry is blind to.
#
# TWO SHAPES SHARE ONE PATCH-ID AND THEY ARE NOT THE SAME FINDING (COORD c53db4e3a s1):
#
#   SAME commit SHA on two seats  = a STACK. Git merges one commit ONCE, so the union carries the
#                                   content once; the hazard is the RECORD, not the tree. Allowed
#                                   IFF --stack declares it; UNDECLARED it refuses BY NAME.
#   DIFFERENT SHAs, one patch-id  = a cherry-pick DUPLICATE. The content boards TWICE under two
#                                   names and a merge sees two unrelated commits. REFUSES ALWAYS --
#                                   no declaration excuses that, which arm 7 of the self-test proves.
#
# So --stack is a narrowing, not a weakening: it can only ever exempt the shape git already collapses.
#
# WHAT IT DOES NOT DO. It compares seats to EACH OTHER, not to the base: a commit already merged into
# the base is not a duplicate, it is history, and `<base>..<seat>` excludes it by construction. It says
# nothing about whether two different diffs touch the same file -- that is a merge conflict, which git
# already reports, and a different failure.
#
#   src/seat-duplication-census.sh --base origin/master <ref> [<ref> ...]
#   src/seat-duplication-census.sh --base origin/master --stack <child>:<parent> <ref> ...
#   src/seat-duplication-census.sh --self-test
#
# Exit 0 = nothing undeclared appears on two seats. Exit 1 = a cherry-pick duplicate or an UNDECLARED
# stack does, each named with every seat carrying it. Exit 2 = misuse.
set -u

BASE=""
SELFTEST=0
REFS=()
STACKS=()

while [ $# -gt 0 ]; do
  case "$1" in
    --base) BASE=${2:-}; shift 2 || { echo "--base needs a ref" >&2; exit 2; } ;;
    # A DECLARED STACK: <child>:<parent> means the child seat is deliberately built on the parent and
    # the two board together. G db6ab3484 found this the day the tool shipped -- run fleet-wide it
    # would have REFUSED R's accepted seat-7 alias block, a declared H6 docs stack, because a stack
    # and a contamination are the same SHAPE. Ancestry cannot be the exemption either: C1's own
    # contamination was ancestor-related (0dab47858 IS an ancestor of 21222f2e8) while a cherry-pick
    # is not, so the discriminator has to be the DECLARATION, as G proposed. Repeatable.
    --stack) STACKS+=("${2:-}"); shift 2 || { echo "--stack needs <child>:<parent>" >&2; exit 2; } ;;
    --self-test) SELFTEST=1; shift ;;
    -*) echo "unknown option: $1" >&2; exit 2 ;;
    *) REFS+=("$1"); shift ;;
  esac
done

# ---------------------------------------------------------------------------- the census itself
# Prints the report and returns 0 clean / 1 duplicates found. Every number it prints is one it
# derived here: no caller supplies a count, so a silently-empty seat list cannot read as "clean".
census() {
  local base=$1; shift
  local refs=("$@")
  local seat sha pid subject
  local -a pids=() owners=()
  local indexed=0 merges=0

  echo "== seat-duplication census"
  echo "   base : $base"
  echo "   seats: ${#refs[@]}"

  if [ "${#refs[@]}" -lt 2 ]; then
    echo "REFUSE: fewer than two seats -- a duplication census over one seat cannot go red" >&2
    return 2
  fi

  for seat in "${refs[@]}"; do
    git rev-parse --verify -q "$seat" >/dev/null || { echo "REFUSE: unknown ref: $seat" >&2; return 2; }
    local n=0
    # --no-merges: a merge has no single diff and patch-id returns nothing for it. Excluded on purpose
    # and COUNTED, because a silent exclusion is how a census loses the thing it was asked about.
    while read -r sha; do
      [ -n "$sha" ] || continue
      pid=$(git show "$sha" | git patch-id --stable | awk '{print $1}')
      if [ -z "$pid" ]; then merges=$((merges+1)); continue; fi
      pids+=("$pid"); owners+=("$seat|$sha")
      indexed=$((indexed+1)); n=$((n+1))
    done < <(git log --no-merges --format=%H "$base..$seat" 2>/dev/null)
    local m; m=$(git log --format=%H "$base..$seat" 2>/dev/null | wc -l | tr -d ' ')
    merges=$((merges + m - n))
    printf "   %-46s %3d commit(s) indexed\n" "$seat" "$n"
  done

  echo "   indexed: $indexed commit(s); $merges merge commit(s) excluded (no single diff to hash)"
  echo

  # Is every seat carrying this patch-id inside ONE declared chain? Declarations are <child>:<parent>
  # edges; a set is declared iff each seat but one has a declared path to another seat in the set.
  declared_pair() {  # $1 child, $2 parent -- is there a declared edge or chain from $1 up to $2?
    local cur=$1 hop
    for _ in 1 2 3 4 5 6 7 8; do
      [ "$cur" = "$2" ] && return 0
      hop=""
      for d in ${STACKS[@]+"${STACKS[@]}"}; do
        [ "${d%%:*}" = "$cur" ] && hop=${d#*:}
      done
      [ -n "$hop" ] || return 1
      cur=$hop
    done
    return 1
  }
  declared_set() {  # every seat in "$@" chains to one of the others
    local a b ok
    for a in "$@"; do
      ok=0
      for b in "$@"; do
        [ "$a" = "$b" ] && continue
        declared_pair "$a" "$b" && { ok=1; break; }
        declared_pair "$b" "$a" && { ok=1; break; }
      done
      [ "$ok" -eq 1 ] || return 1
    done
    return 0
  }

  # dup and stacked are counted SEPARATELY and both reported: they are two findings with two
  # remedies (split the cherry-pick vs declare or split the stack), and one number covering both
  # would hand a reader a count they cannot act on.
  local i j dup=0 stacked=0 declared=0
  for ((i=0; i<${#pids[@]}; i++)); do
    local hits="" first=1
    for ((j=0; j<${#pids[@]}; j++)); do
      [ "${pids[$i]}" = "${pids[$j]}" ] || continue
      [ "$j" -lt "$i" ] && { first=0; break; }   # already reported under the earlier index
      [ "$j" -eq "$i" ] && continue
      hits="$hits ${owners[$j]}"
    done
    [ "$first" -eq 1 ] || continue
    [ -n "$hits" ] || continue
    subject=$(git log -1 --format=%s "${owners[$i]#*|}")
    local -a seatset=("${owners[$i]%%|*}")
    local mysha=${owners[$i]#*|} samesha=1
    for h in $hits; do
      seatset+=("${h%%|*}")
      [ "${h#*|}" = "$mysha" ] || samesha=0
    done

    # COORD c53db4e3a s1, the SHA-first split that makes G's declaration model mechanical:
    #
    #   SAME commit SHA on two seats  -> a STACK. Git merges one commit once, so the union carries
    #                                    the content ONCE; the hazard is the RECORD, not the tree.
    #                                    Allowed IFF declared; undeclared refuses BY NAME.
    #   DIFFERENT SHAs, one patch-id  -> a cherry-pick DUPLICATE. The content would board TWICE and
    #                                    a merge sees two unrelated commits. REFUSES ALWAYS -- no
    #                                    declaration excuses content boarding twice under two names.
    #
    # Ancestry is consulted nowhere, which is G db6ab3484 s4 measured: it separates neither case.
    if [ "$samesha" -eq 1 ]; then
      if [ "${#STACKS[@]}" -gt 0 ] && declared_set "${seatset[@]}"; then
        # REPORTED, never silent: an exemption nobody can see is how a census stops being one.
        declared=$((declared+1))
        echo "declared-stack SHA ${mysha:0:12}  (exempt: every seat carrying it is in one declared chain)"
        echo "   ${owners[$i]}   $subject"
        for h in $hits; do echo "   $h"; done
        echo
        continue
      fi
      stacked=$((stacked+1))
      echo "UNDECLARED STACK: SHA ${mysha:0:12} on more than one seat -- declare it with --stack <child>:<parent> or split it"
      echo "   ${owners[$i]}   $subject"
      for h in $hits; do echo "   $h"; done
      echo
      continue
    fi

    dup=$((dup+1))
    echo "DUPLICATE patch-id ${pids[$i]:0:12}  (DIFFERENT SHAs -- a cherry-pick; no declaration excuses this)"
    echo "   ${owners[$i]}   $subject"
    for h in $hits; do echo "   $h"; done
    echo
  done

  if [ "$((dup + stacked))" -eq 0 ]; then
    echo "==> CENSUS CLEAN: no UNDECLARED patch-id appears on two seats ($indexed commit(s) compared, $declared declared-stack exemption(s))"
    return 0
  fi
  # NAME ONLY THE CATEGORIES THAT ARE NON-ZERO. G measured the alternative on their own tool the same
  # night: a verdict line reading "0 DUPLICATE patch-id(s)" SATISFIES an arm asserting the substring
  # "DUPLICATE patch-id" on a report that found none -- a false green produced by the report's own
  # summary. Mine escaped it only on letter case (an arm greps "UNDECLARED STACK"; the line said
  # "undeclared stack(s)"), which is one case-insensitive assertion away from the same defect. So the
  # summary never prints a count of zero for a category, and arm 8 holds that.
  local verdict=""
  [ "$dup" -gt 0 ] && verdict="$dup cherry-pick duplicate(s)"
  [ "$stacked" -gt 0 ] && verdict="${verdict:+$verdict, }$stacked undeclared stack(s)"
  echo "==> CENSUS RED: $verdict across these seats"
  if [ "$dup" -gt 0 ]; then
    # G db6ab3484 s3, measured fleet-wide: over a whole remote almost every cherry-pick duplicate is a
    # LEGITIMATE supersession, because a clean re-cut patch-matches the branch it replaces -- so a
    # STALE ROW in the seat list produces this exact red. The tool cannot tell the two apart (nothing
    # in the content says which cut is current) and does not guess; it names the check instead, because
    # a red whose two readings have opposite remedies is worse unlabelled than unreported.
    echo "    note: a SUPERSEDED seat left in the list reads exactly like this -- a clean re-cut"
    echo "          patch-matches what it replaces. Check whether either seat is stale BEFORE"
    echo "          reading contamination; if one is, the remedy is to drop the row, not to split it."
  fi
  return 1
}

# ---------------------------------------------------------------------------- the self-test
# Eight arms in a hermetic repo -- no network, no shallow clone, nothing outside a temp dir. Each arm
# asserts the REASON it passed or failed, never merely an exit code, and the RED arms come first so a
# census that cannot go red is caught before any green is believed.
selftest() {
  local tmp; tmp=$(mktemp -d) || { echo "mktemp failed" >&2; return 2; }
  # shellcheck disable=SC2064
  trap "rm -rf '$tmp'" RETURN
  local arms=0 self="$PWD/${BASH_SOURCE[0]}"
  [ -f "$self" ] || self=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/$(basename "${BASH_SOURCE[0]}")

  echo "seat-duplication self-test -- hermetic repo at $tmp"
  echo "every arm asserts the REASON it passed, never merely a nonzero exit"
  echo

  (
    cd "$tmp" || exit 2
    git init -q .
    git config user.email c1@example.invalid; git config user.name c1
    git config commit.gpgsign false
    echo base > base.txt; git add base.txt; git commit -q -m base
    git branch -q trunk

    # one item, developed on a lane and CHERRY-PICKED to its own seat -- the shape ancestry misses
    echo item-a > a.txt; git add a.txt; git commit -q -m "item A"
    A=$(git rev-parse HEAD)
    echo item-b > b.txt; git add b.txt; git commit -q -m "item B"
    git branch -q seat-lane                                  # carries A AND B
    # NOTE: `cherry-pick` has NO -q flag -- passing one prints usage, does NOTHING and exits 0,
    # which left seat-a empty and the census correctly reading clean. Arm 1 caught it. (Second
    # time in one session for this lane; the tell is a branch that should carry a commit and does not.)
    git checkout -q -B seat-a trunk && git cherry-pick -x "$A" >/dev/null 2>&1   # carries A only
    [ "$(git rev-list --count trunk..seat-a)" = "1" ] || { echo "setup: seat-a did not receive the cherry-pick" >&2; exit 2; }
    git checkout -q -B seat-c trunk
    echo item-c > c.txt; git add c.txt; git commit -q -m "item C"              # disjoint

    # A DECLARED STACK, which has the SAME SHAPE as the contamination above and must not be refused
    # when declared: seat-ab is deliberately built ON seat-a and the two board together.
    git checkout -q -B seat-ab seat-a
    echo item-d > d.txt; git add d.txt; git commit -q -m "item D"
  ) || { echo "self-test setup failed" >&2; return 2; }

  local out rc

  # ARM 1 (RED FIRST): the cherry-picked item must be FOUND across seat-lane and seat-a.
  out=$(cd "$tmp" && bash "$self" --base trunk seat-lane seat-a 2>&1); rc=$?
  arms=$((arms+1))
  if [ "$rc" -ne 1 ]; then echo "ARM 1 FAILED: wanted exit 1 on a real duplicate, got $rc"; echo "$out"; return 1; fi
  case "$out" in *"DUPLICATE patch-id"*) ;; *) echo "ARM 1 FAILED: red exit without naming the duplicate"; echo "$out"; return 1 ;; esac
  case "$out" in *"item A"*) ;; *) echo "ARM 1 FAILED: did not NAME the duplicated commit"; echo "$out"; return 1 ;; esac
  echo "  ok   cherry-picked duplicate FOUND        names: item A, across seat-lane and seat-a"

  # ARM 2 (the discriminator): ancestry must NOT see it -- proving the census earns its keep.
  out=$(cd "$tmp" && git merge-base --is-ancestor "$(git -C "$tmp" rev-parse seat-a)" seat-lane 2>&1 && echo ANC || echo NOANC)
  arms=$((arms+1))
  case "$out" in *NOANC*) ;; *) echo "ARM 2 FAILED: ancestry saw the cherry-pick, so this tool is unnecessary"; return 1 ;; esac
  echo "  ok   ancestry BLIND to the same duplicate  (is-ancestor false while the content is present)"

  # ARM 3 (GREEN): disjoint seats must read clean, or the red above proves nothing.
  out=$(cd "$tmp" && bash "$self" --base trunk seat-a seat-c 2>&1); rc=$?
  arms=$((arms+1))
  if [ "$rc" -ne 0 ]; then echo "ARM 3 FAILED: wanted exit 0 on disjoint seats, got $rc"; echo "$out"; return 1; fi
  case "$out" in *"CENSUS CLEAN"*) ;; *) echo "ARM 3 FAILED: clean exit without the verdict line"; echo "$out"; return 1 ;; esac
  echo "  ok   disjoint seats read CLEAN             (so the RED arm is discriminating, not constant)"

  # ARM 4: one seat must REFUSE -- a one-seat census cannot go red and must not report clean.
  out=$(cd "$tmp" && bash "$self" --base trunk seat-a 2>&1); rc=$?
  arms=$((arms+1))
  if [ "$rc" -ne 2 ]; then echo "ARM 4 FAILED: wanted exit 2 on a single seat, got $rc"; echo "$out"; return 1; fi
  case "$out" in *"cannot go red"*) ;; *) echo "ARM 4 FAILED: refused without naming the reason"; echo "$out"; return 1 ;; esac
  echo "  ok   single-seat census REFUSES            (an instrument that cannot fire is not a control)"

  # ARM 5 (the false positive G found on the day this shipped): a DECLARED stack reads CLEAN.
  out=$(cd "$tmp" && bash "$self" --base trunk --stack seat-ab:seat-a seat-a seat-ab 2>&1); rc=$?
  arms=$((arms+1))
  if [ "$rc" -ne 0 ]; then echo "ARM 5 FAILED: wanted exit 0 on a DECLARED stack, got $rc"; echo "$out"; return 1; fi
  case "$out" in *"declared-stack SHA"*) ;; *) echo "ARM 5 FAILED: exempted silently instead of reporting the exemption"; echo "$out"; return 1 ;; esac
  echo "  ok   DECLARED stack reads CLEAN            and the exemption is PRINTED, never silent"

  # ARM 6 (the arm that keeps arm 5 honest): the SAME pair, undeclared, must still be RED. Without
  # this, --stack could be weakening the tool rather than narrowing it and nothing would say so.
  out=$(cd "$tmp" && bash "$self" --base trunk seat-a seat-ab 2>&1); rc=$?
  arms=$((arms+1))
  if [ "$rc" -ne 1 ]; then echo "ARM 6 FAILED: the same pair UNDECLARED must stay red, got $rc"; echo "$out"; return 1; fi
  case "$out" in *"UNDECLARED STACK"*) ;; *) echo "ARM 6 FAILED: red, but not as a STACK -- the two findings have two remedies"; echo "$out"; return 1 ;; esac
  echo "  ok   the same pair UNDECLARED stays RED    named as a STACK, so the remedy is actionable"

  # ARM 7 (COORD c53db4e3a s1, and the arm that bounds --stack): a DECLARED CHERRY-PICK must STILL
  # refuse. Same pair as arm 1 -- one item, two DIFFERENT SHAs -- now declared as loudly as arm 5's
  # stack was. If this ever goes green, --stack has become a way to wave content aboard twice, which
  # is the entire failure the instrument was built for, re-admitted through its own exemption.
  # Arms 5 and 7 are the two halves of one claim: the declaration exempts the shape git COLLAPSES
  # and nothing else. Without arm 7 there is no measurement anywhere saying so.
  out=$(cd "$tmp" && bash "$self" --base trunk --stack seat-a:seat-lane seat-lane seat-a 2>&1); rc=$?
  arms=$((arms+1))
  if [ "$rc" -ne 1 ]; then echo "ARM 7 FAILED: a DECLARED cherry-pick must still refuse, got $rc"; echo "$out"; return 1; fi
  case "$out" in *"DUPLICATE patch-id"*) ;; *) echo "ARM 7 FAILED: red without naming it a cherry-pick duplicate"; echo "$out"; return 1 ;; esac
  case "$out" in *"no declaration excuses this"*) ;; *) echo "ARM 7 FAILED: refused without saying the declaration was IGNORED, so a reader would retry it"; echo "$out"; return 1 ;; esac
  case "$out" in *"declared-stack"*) echo "ARM 7 FAILED: the declaration exempted a cherry-pick"; echo "$out"; return 1 ;; *) ;; esac
  echo "  ok   DECLARED cherry-pick STILL REFUSES    --stack narrows the census, it cannot weaken it"

  # ARM 8 (G, the night this shipped): THE REPORT MUST NOT SATISFY AN ARM IT CONTRADICTS. G's own tool
  # printed "0 DUPLICATE patch-id(s)" in its verdict line, which reads TRUE to an arm asserting the
  # substring "DUPLICATE patch-id" on a report that found NONE -- the summary handing back a green for
  # the very finding it is reporting the absence of. Every arm above is a substring match, so this is
  # a property of THIS suite and not only of G's. Measured here rather than reasoned about: a report
  # that found only stacks must not contain the duplicate keyword, and vice versa.
  local rs rd
  rs=$(cd "$tmp" && bash "$self" --base trunk seat-a seat-ab 2>&1)      # stacks only, zero duplicates
  rd=$(cd "$tmp" && bash "$self" --base trunk seat-lane seat-a 2>&1)    # duplicates only, zero stacks
  arms=$((arms+1))
  case "$rs" in *"DUPLICATE patch-id"*) echo "ARM 8 FAILED: a stacks-only report names the DUPLICATE keyword -- arms 1 and 7 are satisfiable by a report that found none"; echo "$rs"; return 1 ;; *) ;; esac
  case "$rd" in *"UNDECLARED STACK"*) echo "ARM 8 FAILED: a duplicates-only report names the STACK keyword -- arm 6 is satisfiable by a report that found none"; echo "$rd"; return 1 ;; *) ;; esac
  # and the same in the direction case alone was covering: no zero-count of either category anywhere.
  case "$rs$rd" in *"0 cherry-pick"*|*"0 undeclared"*) echo "ARM 8 FAILED: the verdict prints a ZERO count, which is the exact string G's tool tripped on"; echo "$rs"; echo "$rd"; return 1 ;; *) ;; esac
  echo "  ok   the report never names a ZERO finding  so no arm is satisfiable by the summary alone"

  echo
  echo "SELF-TEST CLEAN -- $arms arms"
  return 0
}

if [ "$SELFTEST" -eq 1 ]; then
  selftest
  exit $?
fi

[ -n "$BASE" ] || { echo "usage: seat-duplication-census.sh --base <ref> <seat-ref> [<seat-ref> ...]" >&2; exit 2; }
census "$BASE" "${REFS[@]}"
exit $?
