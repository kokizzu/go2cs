#!/usr/bin/env bash
# ARM 2 -- STALE SHA. Refuses a post that states a PRIOR TIP of one of my branches on any pushed
# surface without that branch's CURRENT tip appearing anywhere on that surface.
#
# THREE HOLES IN v1, all measured rather than reasoned (2026-09-08), prompted by C1's 6371d6907
# where an instrument was vacuous on the very miss it was built for:
#   v1 was LINE-scoped      -> missed a wrapped mention, the commonest prose form (entries wrap ~100)
#   v1 required `claude/`   -> missed a bare branch name
#   v1 saw only the ENTRY   -> never saw the COMMIT SUBJECT, a pushed surface that carried BOTH of my
#                              stale SHAs. i9's finding to exactly this effect is a comment twenty
#                              lines above where I added the arm, and I did not inherit it.
#
# THE HINGE IS ANCESTRY, NOT THE BRANCH NAME -- which is what makes the name unnecessary. My subject
# said "slices branch at 8e9e1808a": the branch name appears NOWHERE, so no name-matching rule of any
# width could have caught it, and a synonym table would rot. But the SHA is a real prior tip of a real
# branch of mine, and that is checkable without knowing what I called it.
#
#   fire  <=>  surface states s, where s is a commit, an ancestor of my branch B's tip T, and s != T
#              AND T appears NOWHERE on that surface
#
# Whole-surface (not paragraph) is both simpler and SAFER: a post that cites a prior tip while its
# standing table carries the current one is exactly the correct shape, and passes. A post that states
# a prior tip with the current tip nowhere in it is a position claim about a branch that has moved --
# which is the thing being caught -- whether or not it happens to name the branch.
# `X..Y` range tokens are excluded: a range is history by construction.
WORK="${GFC_REPO:-C:/Projects/go2cs/.claude/worktrees/wsl-su-auth-failure-43fec3}"
cd "$WORK" || { echo "STALE-ARM: repo unreachable"; exit 2; }
MINE=$(git ls-remote --heads origin 'claude/g-*' 2>/dev/null)
[ -z "$MINE" ] && { echo "STALE-ARM: no claude/g-* on remote -- nothing to check"; exit 0; }
git fetch -q origin 'refs/heads/claude/g-*:refs/remotes/origin/claude/g-*' 2>/dev/null
STALE=0; N=0
for F in "$@"; do
  [ -r "$F" ] || continue
  N=$((N+1))
  TOKS=$(sed 's/[0-9a-f]\{7,\}\.\.[0-9a-f]\{7,\}/ /g' "$F" | grep -oE '[0-9a-f]{7,40}' | sort -u)
  [ -z "$TOKS" ] && continue
  while read -r TIP REF; do
    [ -z "$TIP" ] && continue
    grep -qF "${TIP:0:7}" "$F" && continue          # current tip present on this surface -> fine
    for t in $TOKS; do
      git rev-parse --verify -q "$t^{commit}" >/dev/null 2>&1 || continue
      [ "$(git rev-parse "$t" 2>/dev/null)" = "$TIP" ] && continue
      # A commit on MASTER is shared history: my base a2e3b51c1 is an ancestor of EVERY
      # branch, so citing my own base fired once per branch -- a false positive found by
      # reading the claims rather than the count. Only a commit UNIQUE to my branch is
      # evidence about that branch's tip.
      git merge-base --is-ancestor "$t" origin/master 2>/dev/null && continue
      if git merge-base --is-ancestor "$t" "$TIP" 2>/dev/null; then
        printf '  STALE SHA  %-32s states PRIOR TIP %s ; tip is %s ; tip ABSENT from %s\n' \
               "${REF#refs/heads/}" "$t" "${TIP:0:9}" "$(basename "$F")"
        STALE=$((STALE+1))
      fi
    done
  done <<< "$MINE"
done
echo "STALE-ARM: $N surface(s) inspected, $STALE stale-tip claim(s)"
[ "$STALE" = "0" ] || exit 9
exit 0
