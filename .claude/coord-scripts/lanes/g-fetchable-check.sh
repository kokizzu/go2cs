#!/usr/bin/env bash
# Refuse to post an entry that NAMES one of my own branches which nobody can fetch.
#
# Why this exists: on 2026-09-07 I announced `claude/g-hop-h1 bef7a6dbd` to the mailbox and COORD
# held a seat on it for a day. The branch had never been PUSHED -- it existed only in my worktree.
# The post asserted a state that was not true at the remote, and no gate could see it: the commit
# was real, the SHA was real, `git log` agreed, and every check I ran was LOCAL.
#
# The rule this mechanises: a base nobody can fetch is a base nobody can check.
#
# Predicate (deliberately narrow, so it cannot false-red on ordinary prose):
#   a token `claude/g-*`  -- MY lane's branches only; other lanes' branches are theirs to guarantee
#   AND absent from `ls-remote --heads origin`
#   AND not LANDED (no local ref whose tip is an ancestor of origin/master)
#   => REFUSE.
# A landed branch is pruned from the remote by design, so absence alone is never the test.
#
# ---------------------------------------------------------------------------------------------
# THE NEW-REF ACKNOWLEDGEMENT (GPOST_NEW), ruled by COORD at mailbox 00b5a7fae §2 after this guard
# REFUSED an announce-before-push and the two rules turned out to conflict for a branch that does
# not exist yet:
#
#   floor 9's announce-then-push protects a reader from a MOVING ref. A push that CREATES a ref moves
#   nothing, so for a NEW ref the only live hazard is the one this guard covers -- a named SHA nobody
#   can fetch. Ruled: an EXISTING ref announces then pushes; a NEW ref PUSHES THEN ANNOUNCES, in ONE
#   post carrying the remote read-back (`remote == local == <40-sha>`) and this explicit
#   acknowledgement. `src/safe-push.sh` already carries `--new` for exactly this.
#
# So `GPOST_NEW="<branch> [<branch>...]"` is an acknowledgement, NOT an exemption, and it NARROWS the
# guard rather than weakening it -- every declared branch must still be PRESENT at the remote, and the
# entry must additionally CARRY that branch's read-back line. A declaration can only ever add
# obligations here; there is no input that makes an unfetchable branch acceptable, and arm 6 of the
# self-test exists to keep it that way. (The shape is C1's arm 7 at `cbc12e499`: an exemption that can
# admit the very thing the instrument was built to refuse is not an exemption, it is a hole.)
# ---------------------------------------------------------------------------------------------
WORK="${GFC_REPO:-C:/Projects/go2cs/.claude/worktrees/wsl-su-auth-failure-43fec3}"

# ---------------------------------------------------------------------------------------------
# The check. Prints its rows, then a verdict DERIVED from the counts.
# ---------------------------------------------------------------------------------------------
fetchable_check() {
  local ENTRY="$1"
  [ -r "$ENTRY" ] || { echo "FETCHABLE: entry unreadable: $ENTRY"; return 2; }
  cd "$WORK" || { echo "FETCHABLE: repo unreachable: $WORK"; return 2; }
  git fetch -q origin master 2>/dev/null

  local BRANCHES
  BRANCHES=$(grep -o 'claude/g-[A-Za-z0-9][A-Za-z0-9-]*' "$ENTRY" | sed 's/[-.]*$//' | sort -u)

  # A declaration naming a branch the entry never mentions is a declaration about nothing, and it is
  # refused rather than ignored: a stale GPOST_NEW carried into the next post is exactly how an
  # acknowledgement becomes a habit nobody reads.
  local nd b
  for nd in ${GPOST_NEW:-}; do
    if ! printf '%s\n' "$BRANCHES" | grep -qx "$nd"; then
      echo "  DECLARED-NOT-NAMED $nd -- GPOST_NEW names a branch this entry never mentions"
      echo "-> NOT POSTED: the new-ref acknowledgement must name a branch the post actually announces."
      return 9
    fi
  done

  [ -z "$BRANCHES" ] && { echo "FETCHABLE: no claude/g-* branch named (0 checked)"; return 0; }

  local BAD=0 N=0 R declared
  for b in $BRANCHES; do
    N=$((N+1))
    declared=0
    for nd in ${GPOST_NEW:-}; do [ "$nd" = "$b" ] && declared=1; done
    R=$(git ls-remote --heads origin "$b" 2>/dev/null | awk '{print $1}')

    if [ -n "$R" ]; then
      if [ "$declared" = "1" ]; then
        # The read-back is the whole content of the acknowledgement: it is the reader's proof that
        # the SHA in the post is the SHA at the remote, which is what announce-first would otherwise
        # have given them.
        if grep -q "remote == local == $R" "$ENTRY"; then
          printf '  ok NEW   %-36s remote %s, read-back present\n' "$b" "${R:0:9}"
        else
          printf '  NO-READBACK %-33s declared NEW, remote %s, but the entry carries no read-back for %s\n' "$b" "${R:0:9}" "${R:0:9}"
          BAD=$((BAD+1))
        fi
      else
        printf '  ok       %-36s remote %s\n' "$b" "${R:0:9}"
      fi
      continue
    fi

    if git rev-parse --verify -q "$b" >/dev/null 2>&1 && \
       git merge-base --is-ancestor "$b" origin/master 2>/dev/null; then
      printf '  ok       %-36s landed (in master, pruned by design)\n' "$b"; continue
    fi

    if [ "$declared" = "1" ]; then
      printf '  UNFETCHABLE %-33s declared NEW and STILL ABSENT from the remote\n' "$b"
    else
      printf '  UNFETCHABLE %-33s not on remote, not in master\n' "$b"
    fi
    BAD=$((BAD+1))
  done

  echo "FETCHABLE: $N checked, $BAD unfetchable"
  if [ "$BAD" != "0" ]; then
    echo "-> NOT POSTED. The ruled order (COORD 00b5a7fae): an EXISTING ref announces then pushes;"
    echo "   a NEW ref PUSHES FIRST, then announces in one post carrying its remote read-back line"
    echo "   with GPOST_NEW=<branch> set. A declaration never admits a branch the remote does not have."
    return 9
  fi
  return 0
}

# ---------------------------------------------------------------------------------------------
# The self-test. Real refs, read-only (`ls-remote`, `rev-parse`, a `fetch` of master), no state moves
# anywhere. Every arm is RED-FIRST except the two that exist to prove the reds discriminate.
# ---------------------------------------------------------------------------------------------
self_test() {
  local tmp out rc arms=0 present absent sha
  tmp="$(mktemp -d)" || { echo "SELF-TEST FAILED: no temp dir"; return 1; }
  present="claude/g-fleet-patchid-census"
  absent="claude/g-no-such-branch-zzqx"
  sha="$(cd "$WORK" && git ls-remote --heads origin "$present" 2>/dev/null | awk '{print $1}')"

  # The instrument's own precondition, asserted rather than assumed: one ref that IS at the remote and
  # one that is NOT. Without both, every arm below is about the population instead of about the guard.
  if [ -z "$sha" ]; then echo "SELF-TEST UNMEASURED: $present is not at the remote, so no arm can distinguish"; rm -rf "$tmp"; return 1; fi
  if [ -n "$(cd "$WORK" && git ls-remote --heads origin "$absent" 2>/dev/null)" ]; then echo "SELF-TEST UNMEASURED: $absent exists at the remote"; rm -rf "$tmp"; return 1; fi

  printf 'naming %s and nothing else\n' "$absent"                > "$tmp/absent.md"
  printf 'naming %s\nremote == local == %s\n' "$present" "$sha"  > "$tmp/new-ok.md"
  printf 'naming %s and no read-back at all\n' "$present"        > "$tmp/new-noreadback.md"
  printf 'naming %s\nremote == local == %s\n' "$present" "$sha"  > "$tmp/decl-unnamed.md"

  echo "fetchable-check self-test -- real refs, read-only"

  # ARM 1 (RED): an absent branch with no declaration refuses, and names the ruled order.
  out="$(GPOST_NEW= fetchable_check "$tmp/absent.md" 2>&1)"; rc=$?
  if [ "$rc" -ne 9 ]; then echo "ARM 1 FAILED: wanted 9 for an absent branch, got $rc"; echo "$out"; rm -rf "$tmp"; return 1; fi
  case "$out" in *"NEW ref PUSHES FIRST"*) ;; *) echo "ARM 1 FAILED: refused without naming the ruled order"; echo "$out"; rm -rf "$tmp"; return 1 ;; esac
  arms=$((arms+1)); echo "  ok   absent branch REFUSES and names the ruled order"

  # ARM 2 (GREEN): a present branch with no declaration passes -- the ordinary post, unchanged.
  out="$(GPOST_NEW= fetchable_check "$tmp/new-noreadback.md" 2>&1)"; rc=$?
  if [ "$rc" -ne 0 ]; then echo "ARM 2 FAILED: wanted 0 for an ordinary post naming a pushed branch, got $rc"; echo "$out"; rm -rf "$tmp"; return 1; fi
  arms=$((arms+1)); echo "  ok   ordinary post naming a pushed branch PASSES (no read-back demanded)"

  # ARM 3 (GREEN): declared NEW, present, read-back carried -> passes, and SAYS the declaration was used.
  out="$(GPOST_NEW="$present" fetchable_check "$tmp/new-ok.md" 2>&1)"; rc=$?
  if [ "$rc" -ne 0 ]; then echo "ARM 3 FAILED: wanted 0 for a declared new ref with its read-back, got $rc"; echo "$out"; rm -rf "$tmp"; return 1; fi
  case "$out" in *"read-back present"*) ;; *) echo "ARM 3 FAILED: passed without reporting the acknowledgement"; echo "$out"; rm -rf "$tmp"; return 1 ;; esac
  arms=$((arms+1)); echo "  ok   declared NEW + remote + read-back PASSES  (and the acknowledgement is PRINTED, never silent)"

  # ARM 4 (RED): declared NEW, present, read-back MISSING -> refuses. This is what makes GPOST_NEW a
  # NARROWING: without the declaration the very same entry passes at arm 2, and with it, it must not.
  out="$(GPOST_NEW="$present" fetchable_check "$tmp/new-noreadback.md" 2>&1)"; rc=$?
  if [ "$rc" -ne 9 ]; then echo "ARM 4 FAILED: wanted 9 for a declared new ref with no read-back, got $rc"; echo "$out"; rm -rf "$tmp"; return 1; fi
  case "$out" in *"NO-READBACK"*) ;; *) echo "ARM 4 FAILED: refused without naming the missing read-back"; echo "$out"; rm -rf "$tmp"; return 1 ;; esac
  arms=$((arms+1)); echo "  ok   declared NEW without the read-back REFUSES (the declaration ADDS an obligation)"

  # ARM 5 (RED): a declaration naming a branch the entry never mentions refuses.
  out="$(GPOST_NEW="claude/g-some-other-branch" fetchable_check "$tmp/decl-unnamed.md" 2>&1)"; rc=$?
  if [ "$rc" -ne 9 ]; then echo "ARM 5 FAILED: wanted 9 for a declaration about an unmentioned branch, got $rc"; echo "$out"; rm -rf "$tmp"; return 1; fi
  case "$out" in *"DECLARED-NOT-NAMED"*) ;; *) echo "ARM 5 FAILED: refused without naming the stale declaration"; echo "$out"; rm -rf "$tmp"; return 1 ;; esac
  arms=$((arms+1)); echo "  ok   a declaration about an unmentioned branch REFUSES"

  # ARM 6 (THE BOUND): declaring an ABSENT branch NEW must STILL refuse. If this ever goes green,
  # GPOST_NEW has become a way to announce a SHA nobody can fetch -- the whole defect this guard was
  # written for, re-admitted through its own acknowledgement.
  out="$(GPOST_NEW="$absent" fetchable_check "$tmp/absent.md" 2>&1)"; rc=$?
  if [ "$rc" -ne 9 ]; then echo "ARM 6 FAILED: wanted 9 -- a declaration must never admit an absent branch, got $rc"; echo "$out"; rm -rf "$tmp"; return 1; fi
  case "$out" in *"declared NEW and STILL ABSENT"*) ;; *) echo "ARM 6 FAILED: refused without saying the declaration was ignored"; echo "$out"; rm -rf "$tmp"; return 1 ;; esac
  arms=$((arms+1)); echo "  ok   declaring an ABSENT branch NEW STILL REFUSES (the acknowledgement cannot become a hole)"

  rm -rf "$tmp"
  if [ "$arms" -ne 6 ]; then echo "SELF-TEST FAILED: $arms arm(s) ran, expected 6"; return 1; fi
  echo "SELF-TEST CLEAN -- 6 arms"
  return 0
}

if [ "${1:-}" = "--self-test" ]; then
  self_test
  exit $?
fi

fetchable_check "${1:-}"
exit $?
