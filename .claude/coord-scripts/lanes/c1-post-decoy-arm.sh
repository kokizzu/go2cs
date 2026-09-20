#!/usr/bin/env bash
# c1-post-decoy-arm.sh -- R's predicate (fe5f4089c), RUN rather than reasoned, against C1's own tool.
#
# "For every write, WHO chose the directory?" -- and R's point is that READING cannot answer it,
# because an argument-derived sibling contains a variable and so reads as parameterised. C1's
# 21579ad53 audit was an ENUMERATION. This is the measurement that subsumes it.
#
# Arms:
#   A  entry+subject in a directory the tool does not own, decoys beside them, CWD elsewhere
#   B  same, but CWD is a THROWAWAY GIT REPO (C2's d55a546 route: a default derived from caller CWD)
#   C  positive control -- a deliberate write at a decoy IS detected
#   D  census-dir fingerprint before/after (COORD 7a959706f)
#   V  VACUITY GUARD -- the run must have REACHED the census and the dry-run gate (R's rc-6 lesson)
set -uo pipefail
# ⚠ ABSOLUTE, and REFUSED if it is not runnable. Every arm cd's before invoking, so a RELATIVE
# tool path resolves to nothing -- and a tool that never ran leaves the caller's directory
# untouched, which arm A reads as a PASS. Caught on this arm's own one-axis run: the GREEN control
# passed arm A while the vacuity guard read "never reached the census". The guard is the only
# reason that false green was visible.
TOOL="$(readlink -f "${1:?tool path}" 2>/dev/null || true)"
[ -n "$TOOL" ] && [ -r "$TOOL" ] || { echo "ARM REFUSED: tool not readable: '${1:-}'"; exit 2; }
BASE="$(mktemp -d)"
trap 'rm -rf "$BASE"' EXIT
fail=0; pass=0
say(){ printf '%s\n' "$*"; }
ok(){ pass=$((pass+1)); say "  PASS  $*"; }
no(){ fail=$((fail+1)); say "  FAIL  $*"; }

fingerprint(){ # dir -> "name sha256" per line, sorted; a CREATED file appears whatever its name
  ( cd "$1" 2>/dev/null && find . -type f -printf '%P\n' 2>/dev/null | LC_ALL=C sort |
    while IFS= read -r f; do printf '%s %s\n' "$f" "$(sha256sum "$f" | cut -d' ' -f1)"; done )
}

plant(){ # dir -- the five sibling spellings R's own defect used, plus a bystander
  local d="$1"; mkdir -p "$d"
  cat > "$d/entry.md" <<'BODY'
## 2026-09-20 — C1 → FLEET: **decoy arm body, never posted.**

This body exists so the tool reaches its census and its dry-run gate. It is discarded.

— C1
BODY
  printf '%s\n' 'mailbox: C1 -> FLEET -- decoy arm subject, never posted' > "$d/subject.txt"
  for s in .lc.tmp .joined .tmp .lower .scan; do printf 'decoy-%s\n' "$s" > "$d/entry.md$s"; done
  printf 'decoy-subject-tmp\n' > "$d/subject.txt.tmp"
  printf 'bystander\n' > "$d/bystander.txt"
}

run_arm(){ # name cwd decoydir -> writes $BASE/<name>.out, echoes rc
  local n="$1" cwd="$2" d="$3"
  ( cd "$cwd" && "$TOOL" --dry-run "$d/entry.md" "$d/subject.txt" ) > "$BASE/$n.out" 2>&1
  echo $?
}

CENSUS_DIR_REAL="${C1_CENSUS_DIR:-${TMPDIR:-/tmp}/c1-census-from-master}"

# ── D(before) ────────────────────────────────────────────────────────────────────────────────
CD_BEFORE="$(fingerprint "$CENSUS_DIR_REAL")"

# ── ARM A ────────────────────────────────────────────────────────────────────────────────────
say "ARM A -- entry+subject in a directory the tool does not own, CWD elsewhere"
A="$BASE/callerA"; plant "$A"
A_BEFORE="$(fingerprint "$A")"
A_RC="$(run_arm A "$BASE" "$A")"
A_AFTER="$(fingerprint "$A")"
say "  rc=$A_RC  files before=$(printf '%s\n' "$A_BEFORE" | wc -l)  after=$(printf '%s\n' "$A_AFTER" | wc -l)"

# ── V: VACUITY GUARD -- did the run REACH the subject of the measurement? ────────────────────
# ⚠ THIS RUNS BEFORE ARM A's VERDICT IS PRINTED, and that ordering is the point (i9 `7b6609d1e`
# §2). Arm A used to print `PASS  caller's directory BYTE-IDENTICAL` even on a run that exited
# above the subject -- the SUITE was correct, because V's failures put the run at fail>0, but the
# per-case LINE read clean and a reader skimming it would take it at face value. i9's arms report
# the SECTION as VACUOUS instead of letting the case read unchanged, which is strictly better, so
# arm A's verdict is now conditioned on V rather than printed beside it.
say "VACUITY GUARD -- the run must have reached the census AND the dry-run gate"
V_FAILS=0
vchk(){ if eval "$1"; then ok "$2"; else no "$3"; V_FAILS=$((V_FAILS+1)); fi; }
vchk "grep -q '^census: origin/master ' '$BASE/A.out'" \
     "reached step 0b (census materialised from origin/master)" "never reached the census materialisation"
vchk "grep -q '^census battery: ' '$BASE/A.out'" \
     "reached step 0c (the battery)" "never reached the battery"
grep -qi 'census (DECIDES)\|identifier census' "$BASE/A.out" && ok "the census ran OVER THE FOREIGN-DIRECTORY BODY" || say "  note  no explicit per-surface census line in output"
vchk "grep -qi 'DRY RUN' '$BASE/A.out'" \
     "reached step 6 (the dry-run gate) -- every write site below step 0 executed" \
     "exited ABOVE the dry-run gate: this reading measures NOTHING"

# ── ARM A's VERDICT, conditioned on V ────────────────────────────────────────────────────────
say "ARM A verdict"
if [ "$A_BEFORE" != "$A_AFTER" ]; then
  # A REAL WRITE IS REPORTED WHATEVER V SAYS -- a directory that CHANGED is a fact about the run
  # that happened, and vacuity cannot excuse it. Only the CLEAN reading needs V to mean anything.
  no "caller's directory CHANGED:"; diff <(printf '%s\n' "$A_BEFORE") <(printf '%s\n' "$A_AFTER") | sed 's/^/        /'
elif [ "$V_FAILS" -gt 0 ]; then
  no "VACUOUS -- the caller's directory is unchanged, but the run never reached the subject, so this is not a reading"
else
  ok "caller's directory BYTE-IDENTICAL (list and content)"
fi

# ── ARM B -- C2's route: a default derived from the CALLER'S CWD ─────────────────────────────
say "ARM B -- CWD is a throwaway GIT REPO (C2 d55a546's force-fetch-into-the-repo-it-stood-in)"
B="$BASE/callerB"; plant "$B"
git -C "$B" init -q 2>/dev/null; git -C "$B" -c user.email=a@b -c user.name=a commit -q --allow-empty -m x 2>/dev/null
B_REFS_BEFORE="$(git -C "$B" show-ref 2>/dev/null | wc -l)"
B_ORIGIN_BEFORE="$(git -C "$B" rev-parse --verify -q refs/remotes/origin/master 2>/dev/null || echo NONE)"
B_BEFORE="$(fingerprint "$B" | grep -v '^\.git/')"   # BOTH sides filtered the SAME way -- a comparison whose
                                                     # two sides are measured differently is not a comparison
B_RC="$(run_arm B "$B" "$B")"
B_REFS_AFTER="$(git -C "$B" show-ref 2>/dev/null | wc -l)"
B_ORIGIN_AFTER="$(git -C "$B" rev-parse --verify -q refs/remotes/origin/master 2>/dev/null || echo NONE)"
B_AFTER="$(fingerprint "$B" | grep -v '^\.git/')"
say "  rc=$B_RC  refs $B_REFS_BEFORE -> $B_REFS_AFTER   origin/master $B_ORIGIN_BEFORE -> $B_ORIGIN_AFTER"
# ⚠ ARM B GETS ITS OWN VACUITY GUARD, on ITS OWN output. V above reads A.out and says nothing
# about this run -- and the first cut of the conditioned verdict fixed arm A while arm B went on
# printing three clean lines for a run that never happened, which is the same defect one arm over.
# Per-SECTION guards, i9's shape (`7b6609d1e` §2), because a guard is about a RUN and this is a
# different run.
B_VAC=0
grep -q '^census: origin/master ' "$BASE/B.out" || B_VAC=1
grep -qi 'DRY RUN' "$BASE/B.out" || B_VAC=1
bchk(){ # clean-verdict-condition  ok-text  fail-text
  if eval "$1"; then
    [ "$B_VAC" -eq 0 ] && ok "$2" || no "VACUOUS -- $2, but the run never reached the subject"
  else no "$3"; fi; }
bchk '[ "$B_REFS_BEFORE" = "$B_REFS_AFTER" ]' "ref count unchanged in the repo the tool merely stood in" "REFS APPEARED: $B_REFS_BEFORE -> $B_REFS_AFTER"
bchk '[ "$B_ORIGIN_AFTER" = NONE ]' "no origin/master fetched into the caller's repo" "origin/master APPEARED: $B_ORIGIN_AFTER"
if [ "$B_BEFORE" != "$B_AFTER" ]; then
  no "caller's repo CHANGED:"; diff <(printf '%s\n' "$B_BEFORE") <(printf '%s\n' "$B_AFTER") | sed 's/^/        /'
elif [ "$B_VAC" -ne 0 ]; then no "VACUOUS -- the caller's repo is unchanged, but the run never reached the subject"
else ok "caller's repo working files BYTE-IDENTICAL"; fi

# ── ARM C -- POSITIVE CONTROL: "nothing changed" is what a BLIND method prints too ───────────
say "ARM C -- positive control: the method can SEE a write"
C="$BASE/callerC"; plant "$C"
C_BEFORE="$(fingerprint "$C")"
printf 'x\n' >> "$C/entry.md.lc.tmp"; printf 'created\n' > "$C/entry.md.unplanted"
C_AFTER="$(fingerprint "$C")"
[ "$C_BEFORE" != "$C_AFTER" ] && ok "a modified decoy AND a created file are both detected" || no "the method is BLIND -- it cannot see a write"

# ── D(after) ─────────────────────────────────────────────────────────────────────────────────
say "ARM D -- the real census dir, fingerprinted across every arm (COORD 7a959706f)"
CD_AFTER="$(fingerprint "$CENSUS_DIR_REAL")"
# ⚠ D IS A CLAIM ABOUT THE TOOL, so it needs a run to have happened -- and its subject spans arms
# A and B, so ONE of them reaching the subject is enough. (Arm C needs no such condition: it is a
# control of the METHOD and never invokes the tool, so its PASS is real on every input. That
# asymmetry is the test of whether a vacuity condition belongs on a case at all -- ask whether the
# case's claim is about the SUBJECT or about the INSTRUMENT.)
if [ "$CD_BEFORE" = "$CD_AFTER" ]; then
  if [ "$V_FAILS" -eq 0 ] || [ "$B_VAC" -eq 0 ]; then
    ok "census dir UNTOUCHED across all arms ($(printf '%s\n' "$CD_AFTER" | grep -c . ) file(s))"
  else
    no "VACUOUS -- the census dir is unchanged, but NEITHER arm reached the subject"
  fi
else
  say "  note  census dir CHANGED (re-materialisation is the tool's job):"; diff <(printf '%s\n' "$CD_BEFORE") <(printf '%s\n' "$CD_AFTER") | sed 's/^/        /'; ok "change is inside the door and reported"
fi

say ""
say "DECOY ARM: pass=$pass fail=$fail"
[ "$fail" = 0 ]
