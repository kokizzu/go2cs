#!/usr/bin/env bash
# Merge probe with the two vacuity classes closed.
#
#   bash merge-probe.sh <A> <B> [<A> <B> ...]
#
# A merge probe's GREEN is readable only when the two sides are INDEPENDENT *and* OVERLAPPING.
# Both failures of that look exactly like a passed test:
#
#   CONTAINMENT   one side is an ancestor of the other  -> there is nothing to merge
#   DISJOINTNESS  the two sides share no path           -> there is nothing to collide
#
# So every row prints its CLASS and its SHARED PATH COUNT, and a row that cannot be read says so
# instead of contributing a green. Two further traps this closes:
#
#   * `git merge-tree` exits NON-ZERO for a MISSING OBJECT, indistinguishable from a conflict, so
#     both endpoints are asserted present before the exit code is read.
#   * a `--diff-filter=U` list is EMPTY both for a clean merge and for a merge git refused, so the
#     conflict count is taken from merge-tree's own CONFLICT lines, never from silence.
#
# Exit 0 if every readable row is clean; 1 if any readable row conflicts; 2 on a refusal.
set -u
[ $# -ge 2 ] && [ $(( $# % 2 )) -eq 0 ] || { echo "usage: merge-probe.sh <A> <B> [<A> <B> ...]" >&2; exit 2; }

RC=0; REFUSED=0; INFORMATIVE=0; VACUOUS=0
printf '%-52s %-6s %-7s %-12s %s\n' PAIR rc conflict shared class
while [ $# -gt 0 ]; do
  A="$1"; B="$2"; shift 2
  # Label: short branch name where there is one, the given spelling otherwise -- never EMPTY, because
  # the row that most needs to be identifiable is the refused one, whose ref does not resolve.
  name() { local n; n=$(git rev-parse --abbrev-ref "$1" 2>/dev/null); n=${n#claude/}; [ -n "$n" ] && printf '%s' "$n" || printf '%s' "$1"; }
  lbl="$(name "$A") x $(name "$B")"
  [ ${#lbl} -le 52 ] || lbl="…${lbl: -51}"
  if ! git cat-file -e "$A^{commit}" 2>/dev/null; then
    printf '%-52s %s\n' "$lbl" "REFUSED: left object absent -- NOT a conflict"; REFUSED=1; continue; fi
  if ! git cat-file -e "$B^{commit}" 2>/dev/null; then
    printf '%-52s %s\n' "$lbl" "REFUSED: right object absent -- NOT a conflict"; REFUSED=1; continue; fi

  if git merge-base --is-ancestor "$A" "$B" 2>/dev/null || git merge-base --is-ancestor "$B" "$A" 2>/dev/null; then
    printf '%-52s %-6s %-7s %-12s %s\n' "$lbl" "-" "-" "-" "CONTAINED -> VACUOUS, not a test"
    VACUOUS=$((VACUOUS+1)); continue
  fi

  base=$(git merge-base "$A" "$B") || { echo "REFUSED: no merge base for $lbl" >&2; REFUSED=1; continue; }
  shared=$(comm -12 <(git diff --name-only "$base" "$A" | sort) <(git diff --name-only "$base" "$B" | sort) | wc -l)
  out=$(git merge-tree --write-tree "$A" "$B" 2>&1); rc=$?
  nc=$(printf '%s\n' "$out" | grep -c '^CONFLICT') || nc=0

  if [ "$shared" -eq 0 ]; then
    printf '%-52s %-6s %-7s %-12s %s\n' "$lbl" "$rc" "$nc" "$shared" "DISJOINT -> VACUOUS, not a test"
    VACUOUS=$((VACUOUS+1)); continue
  fi
  printf '%-52s %-6s %-7s %-12s %s\n' "$lbl" "$rc" "$nc" "$shared" "OVERLAPPING -> INFORMATIVE"
  INFORMATIVE=$((INFORMATIVE+1))
  [ "$nc" -eq 0 ] || RC=1
done

echo
echo "informative rows: $INFORMATIVE    vacuous rows: $VACUOUS    refusals: $REFUSED"
[ "$INFORMATIVE" -gt 0 ] || echo "NOTHING WAS TESTED: every row was vacuous or refused. This run is not evidence."
[ "$REFUSED" -eq 0 ] || exit 2
exit "$RC"
