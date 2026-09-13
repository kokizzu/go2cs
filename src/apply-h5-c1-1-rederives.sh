#!/usr/bin/env bash
# apply-h5-c1-1-rederives.sh -- the C1-1 hand-own re-derives, applied to an H5 SCRATCH tree.
#
# WHY THIS IS A SCRIPT AND NOT A COMMIT (COORD 5123a14a2 s2, on C1 68cf737). Both defects exist ONLY
# in the post-H5c world, so a landed fix would break the corpus that is green today:
#
#     src/core/runtime/internal/sys   PRESENT at a02ac3df3   -> the alias is CORRECT before H5c
#     src/core/internal/runtime/sys   ABSENT  at a02ac3df3   -> re-pointing early breaks the build
#     src/core/runtime/note_other.cs  ABSENT  at a02ac3df3   -> `note` is the ONLY definition today
#
# R's fifth rehearsal (4b4134242) measured the consequence: the landing tree + a seeded 1.24.13
# reconvert + H5c does NOT build `runtime` -- 120/120/120 unique sites, flavour-independent, every
# root one of the eight sites below -- so H4a's regen measures nothing past `runtime` until this runs.
#
#   apply-h5-c1-1-rederives.sh <scratch-core-dir>     apply, then assert the post-condition
#   apply-h5-c1-1-rederives.sh --verify <dir>         assert only; changes nothing
#   apply-h5-c1-1-rederives.sh --self-test            hermetic, red-first, no clone touched
#
# Exit 0 applied-and-verified (or already applied) / 1 a post-condition failed / 2 misuse or refusal.
set -u

R2="runtime/runtime2.cs"
MF="runtime/mfinal.cs"

OLD_ALIAS='using sys = runtime.@internal.sys_package;'
NEW_ALIAS='using sys = @internal.runtime.sys_package;'
OLD_NS='using runtime.@internal;'
NEW_NS='using @internal.runtime;'
OLD_QUAL='runtime.@internal.sys_package.NotInHeap'
NEW_QUAL='@internal.runtime.sys_package.NotInHeap'

# ---------------------------------------------------------------------------------------------
# THE TWO DEFECTS, and the derivation of every replacement -- measured, not guessed.
#
# (i) THE FOURTH RELOCATION. At go1.24.13 `runtime/internal/{sys,math}` are GONE and
#     `internal/runtime/{sys,math}` are present (probed at the pin: runtime/internal/sys/consts.go
#     404, internal/runtime/sys/consts.go 200, same for math). `startlinetest` and `wasitest` REMAIN
#     under runtime/internal/, so `namespace go.runtime.@internal` still exists -- it just no longer
#     holds sys. The new spelling comes from the package that was ALREADY there: the corpus aliases
#     internal/runtime/atomic as `@internal.runtime.atomic_package` (namespace go.@internal.runtime),
#     so sys becomes `@internal.runtime.sys_package` by the same rule.
#
#     ⚠ THE NAMESPACE IMPORT IS A DELETION, NOT A SUBSTITUTION, and this is the one place a naive
#     re-point goes wrong: BOTH files ALREADY carry `using @internal.runtime;` one line above
#     (runtime2.cs:24, mfinal.cs:23). Rewriting `using runtime.@internal;` to the new namespace
#     would emit a DUPLICATE using directive. Neither file references math_package or startlinetest
#     (grep = 0 in both), so nothing is lost by dropping it.
#
# (ii) THE NOTE DUPLICATE. `note_other.cs` is a 1.24 emission carrying `partial struct note`, and the
#     frozen runtime2.cs hand-own carries its own -- CS0102 on `key` plus CS0579. COORD 4327ab7e1
#     s7(ii): re-derive runtime2.cs with `note` deleted WHEN note_other.cs lands. This deletes it.
# ---------------------------------------------------------------------------------------------

die() { echo "REFUSE: $*" >&2; exit 2; }

# Is this a tree the patch BELONGS on? Refusing early is the point: applied to a pre-H5c tree these
# edits break a green corpus, which is the whole reason the fix is not a commit.
check_precondition() {
  local core=$1
  [ -f "$core/$R2" ] || die "no $R2 under $core -- not a corpus root"
  [ -f "$core/$MF" ] || die "no $MF under $core -- not a corpus root"
  if [ -d "$core/runtime/internal/sys" ]; then
    die "runtime/internal/sys is STILL PRESENT -- H5c has not run on $core. Applying here would
        re-point aliases at a package that does not exist yet and break a tree that builds."
  fi
  [ -d "$core/internal/runtime/sys" ] || die "internal/runtime/sys is ABSENT -- this is not a 1.24 emission"
  [ -f "$core/runtime/note_other.cs" ] || die "runtime/note_other.cs is ABSENT -- the note duplicate
        cannot exist yet, so (ii) would DELETE the only definition of note"
}

# The decidable post-condition. Named separately so --verify can run it against a tree this script
# never touched -- a checker that only runs inside the applier proves nothing about someone else's tree.
verify() {
  local core=$1 fail=0 n

  # ⚠ CRLF-AGNOSTIC BY CONSTRUCTION. The corpus is CRLF, so a line's content ENDS WITH \r and
  # `grep -x -F 'using @internal.runtime;'` matches NOTHING -- it read 0 on a correctly patched file
  # and the checker reported a duplicate-using failure that did not exist. Worse, it made the
  # unpatched-tree arm pass for the WRONG REASON: that arm only wanted a non-zero exit, and a checker
  # broken on every input supplies one. Strip \r once, here, and every count below asks its real
  # question.
  rd() { tr -d '\r' < "$1"; }

  for f in "$R2" "$MF"; do
    n=$(rd "$core/$f" | grep -c -F 'runtime.@internal.sys_package' || true)
    [ "${n:-0}" -eq 0 ] || { echo "  FAIL $f: $n site(s) still name the OLD sys package"; fail=1; }
    n=$(rd "$core/$f" | grep -c -x -F "$OLD_NS" || true)
    [ "${n:-0}" -eq 0 ] || { echo "  FAIL $f: the old namespace import is still present"; fail=1; }
    n=$(rd "$core/$f" | grep -c -x -F "$NEW_NS" || true)
    [ "${n:-0}" -eq 1 ] || { echo "  FAIL $f: expected exactly ONE '$NEW_NS', found ${n:-0} (duplicate using directive)"; fail=1; }
    n=$(rd "$core/$f" | grep -c -x -F "$NEW_ALIAS" || true)
    [ "${n:-0}" -eq 1 ] || { echo "  FAIL $f: expected exactly ONE re-pointed sys alias, found ${n:-0}"; fail=1; }
  done

  n=$(rd "$core/$R2" | grep -c -E '^\[GoType\] partial struct note \{' || true)
  [ "${n:-0}" -eq 0 ] || { echo "  FAIL $R2: partial struct note is still declared beside note_other.cs"; fail=1; }

  # THE CARRY POST-CONDITION (COORD 5123a14a2, into R's (b) verbatim). A re-derive of mfinal.cs that
  # re-applies the PRE-mcleanup hand-own silently returns runtime.AddCleanup to a no-op, and a clean
  # three-way merge is exactly the shape that does it. This is that hazard made decidable.
  n=$(rd "$core/$MF" | grep -c -F 'GoFinalizerQueue.EnsureRunner();' || true)
  [ "${n:-0}" -ge 2 ] || { echo "  FAIL $MF: createfing does not forward to GoFinalizerQueue.EnsureRunner()
       -- the mcleanup hand-own was NOT carried into this re-derive, and AddCleanup is a silent no-op"; fail=1; }
  # ⚠ CODE ONLY. mfinal.cs's own header comment NAMES goǃ(runfinq) while describing the body it
  # replaced, so a file-wide grep matches the documentation and fails a correctly-carried file. Caught
  # on real data: arm B below (the mcleanup branch, which is CORRECT) went red on its own comment.
  # Third instance of this class tonight -- an assertion about CODE must not read PROSE.
  n=$(rd "$core/$MF" | sed 's|//.*||' | grep -c -F 'goǃ(runfinq)' || true)
  [ "${n:-0}" -eq 0 ] || { echo "  FAIL $MF: goǃ(runfinq) is back IN CODE -- the dead runner is being started again"; fail=1; }

  return $fail
}

apply() {
  local core=$1
  python3 - "$core/$R2" "$core/$MF" <<'PY'
import sys, io
r2, mf = sys.argv[1], sys.argv[2]
OLD_ALIAS = 'using sys = runtime.@internal.sys_package;'
NEW_ALIAS = 'using sys = @internal.runtime.sys_package;'
OLD_NS    = 'using runtime.@internal;'
OLD_QUAL  = 'runtime.@internal.sys_package.NotInHeap'
NEW_QUAL  = '@internal.runtime.sys_package.NotInHeap'

def edit(path, drop_note):
    # newline='' so CRLF survives byte for byte -- the corpus is CRLF and a rewrite that
    # normalises it would show as a whole-file diff and mask the real change.
    with io.open(path, encoding='utf-8', newline='') as fh:
        text = fh.read()
    text = text.replace(OLD_ALIAS, NEW_ALIAS)
    text = text.replace(OLD_QUAL, NEW_QUAL)
    # Delete the namespace import as a WHOLE LINE, either line ending, once.
    for eol in ('\r\n', '\n'):
        line = OLD_NS + eol
        if line in text:
            text = text.replace(line, '', 1)
            break
    if drop_note:
        start = text.find('[GoType] partial struct note {')
        if start != -1:
            end = text.find('}', start)
            # consume the closing brace and its line ending
            end = text.find('\n', end)
            text = text[:start] + text[end + 1:]
    with io.open(path, 'w', encoding='utf-8', newline='') as fh:
        fh.write(text)

edit(r2, drop_note=True)
edit(mf, drop_note=False)
PY
}

# ---------------------------------------------------------------------------------- self-test
selftest() {
  local tmp; tmp=$(mktemp -d) || die "mktemp failed"
  # shellcheck disable=SC2064
  trap "rm -rf '$tmp'" RETURN
  local arms=0 self="$PWD/${BASH_SOURCE[0]}"
  [ -f "$self" ] || self=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/$(basename "${BASH_SOURCE[0]}")

  echo "h5-c1-1 re-derive self-test -- hermetic tree at $tmp"
  echo "every arm asserts the REASON it passed, never merely an exit code"
  echo

  mkpost() {   # a minimal POST-H5c tree: the shape this patch belongs on
    local d=$1; rm -rf "$d"; mkdir -p "$d/runtime" "$d/internal/runtime/sys"
    printf 'namespace go;\r\nusing atomic = @internal.runtime.atomic_package;\r\nusing sys = runtime.@internal.sys_package;\r\nusing @internal;\r\nusing @internal.runtime;\r\nusing runtime.@internal;\r\n\r\npartial class runtime_package {\r\n[GoType] partial struct note {\r\n    internal uintptr key;\r\n}\r\n\r\npublic partial ref runtime.@internal.sys_package.NotInHeap NotInHeap { get; }\r\n}\r\n' > "$d/runtime/runtime2.cs"
    printf 'namespace go;\r\nusing sys = runtime.@internal.sys_package;\r\nusing @internal;\r\nusing @internal.runtime;\r\nusing runtime.@internal;\r\n\r\npartial class runtime_package {\r\ninternal static void createfing() {\r\n    GoFinalizerQueue.EnsureRunner();\r\n}\r\nvoid other() { GoFinalizerQueue.EnsureRunner(); }\r\n}\r\n' > "$d/runtime/mfinal.cs"
    printf '[GoType] partial struct note { internal uintptr key; }\r\n' > "$d/runtime/note_other.cs"
    : > "$d/internal/runtime/sys/consts.cs"
  }

  # ARM 1 (RED FIRST): a PRE-H5c tree must be REFUSED, because applying there breaks a green corpus.
  mkpost "$tmp/pre"; mkdir -p "$tmp/pre/runtime/internal/sys"
  out=$(bash "$self" "$tmp/pre" 2>&1); rc=$?
  arms=$((arms+1))
  [ "$rc" -eq 2 ] || { echo "ARM 1 FAILED: wanted refusal (2) on a pre-H5c tree, got $rc"; echo "$out"; return 1; }
  case "$out" in *"H5c has not run"*) ;; *) echo "ARM 1 FAILED: refused without naming H5c"; echo "$out"; return 1 ;; esac
  echo "  ok   a PRE-H5c tree is REFUSED          applying there would break a tree that builds"

  # ARM 2 (RED): an unpatched post-H5c tree must FAIL --verify, or the post-condition proves nothing.
  mkpost "$tmp/raw"
  out=$(bash "$self" --verify "$tmp/raw" 2>&1); rc=$?
  arms=$((arms+1))
  [ "$rc" -eq 1 ] || { echo "ARM 2 FAILED: --verify passed an UNPATCHED tree (rc=$rc)"; echo "$out"; return 1; }
  # ⚠ ASSERT THE REASON, not just rc=1. As first written this arm took any non-zero exit -- and a
  # checker broken on CRLF (which is what the first draft was) supplies one on EVERY tree, so the arm
  # passed while proving nothing. It must name the defect it is looking at.
  case "$out" in *"still name the OLD sys package"*) ;; *) echo "ARM 2 FAILED: red, but not for the unpatched-alias reason -- the checker may be broken on every input"; echo "$out"; return 1 ;; esac
  case "$out" in *"partial struct note is still declared"*) ;; *) echo "ARM 2 FAILED: did not catch the surviving note duplicate"; echo "$out"; return 1 ;; esac
  echo "  ok   an UNPATCHED tree FAILS --verify   naming BOTH defects, so the green below discriminates"

  # ARM 3 (GREEN): apply, then verify.
  mkpost "$tmp/go"
  out=$(bash "$self" "$tmp/go" 2>&1); rc=$?
  arms=$((arms+1))
  [ "$rc" -eq 0 ] || { echo "ARM 3 FAILED: apply+verify returned $rc"; echo "$out"; return 1; }
  echo "  ok   apply then verify is GREEN         the four edits land and the checker agrees"

  # ARM 4: exactly ONE new namespace import -- the duplicate-using trap a naive re-point falls into.
  arms=$((arms+1))
  for f in runtime2 mfinal; do
    n=$(tr -d '\r' < "$tmp/go/runtime/$f.cs" | grep -c -x -F 'using @internal.runtime;')
    [ "$n" -eq 1 ] || { echo "ARM 4 FAILED: $f.cs has $n 'using @internal.runtime;' -- want exactly 1"; return 1; }
  done
  echo "  ok   no DUPLICATE using directive       the old namespace line is DELETED, not re-pointed"

  # ARM 5: CRLF survives. A rewrite that normalises line endings shows as a whole-file diff.
  arms=$((arms+1))
  for f in runtime2 mfinal; do
    cr=$(python3 -c "print(open('$tmp/go/runtime/$f.cs','rb').read().count(b'\r'))")
    lf=$(python3 -c "print(open('$tmp/go/runtime/$f.cs','rb').read().count(b'\n'))")
    [ "$cr" = "$lf" ] || { echo "ARM 5 FAILED: $f.cs CR=$cr LF=$lf -- line endings were normalised"; return 1; }
  done
  echo "  ok   CRLF preserved byte for byte      a normalising rewrite would mask the real change"

  # ARM 6: IDEMPOTENT -- a second apply must not double-edit or fail.
  out=$(bash "$self" "$tmp/go" 2>&1); rc=$?
  arms=$((arms+1))
  [ "$rc" -eq 0 ] || { echo "ARM 6 FAILED: second apply returned $rc"; echo "$out"; return 1; }
  n=$(tr -d '\r' < "$tmp/go/runtime/runtime2.cs" | grep -c -x -F 'using @internal.runtime;')
  [ "$n" -eq 1 ] || { echo "ARM 6 FAILED: re-apply duplicated the using directive ($n)"; return 1; }
  echo "  ok   re-apply is IDEMPOTENT             an H5 rerun cannot double-edit the tree"

  # ARM 7 (the CARRY hazard, made decidable): a re-derive that lost the mcleanup hand-own must FAIL.
  mkpost "$tmp/carry"
  python3 - "$tmp/carry/runtime/mfinal.cs" <<'PY'
import io, sys
p = sys.argv[1]
t = io.open(p, encoding='utf-8', newline='').read()
# the PRE-mcleanup hand-own: createfing starts the dead converted runner
t = t.replace('internal static void createfing() {\r\n    GoFinalizerQueue.EnsureRunner();\r\n}',
              'internal static void createfing() {\r\n    goǃ(runfinq);\r\n}')
io.open(p, 'w', encoding='utf-8', newline='').write(t)
PY
  out=$(bash "$self" "$tmp/carry" 2>&1); rc=$?
  arms=$((arms+1))
  [ "$rc" -eq 1 ] || { echo "ARM 7 FAILED: a re-derive that DROPPED the mcleanup hand-own passed (rc=$rc)"; echo "$out"; return 1; }
  case "$out" in *"silent no-op"*) ;; *) echo "ARM 7 FAILED: failed without naming the no-op"; echo "$out"; return 1 ;; esac
  echo "  ok   a LOST mcleanup hand-own FAILS     the carry hazard is decidable, not a comment"

  # ARM 8: the inverse of arm 7, and the one real data forced. A CORRECT file whose COMMENT names
  # goǃ(runfinq) -- which mfinal.cs's real header does -- must still PASS. Without this the checker
  # rejects the very tree it is meant to bless, and it did.
  mkpost "$tmp/prose"
  python3 - "$tmp/prose/runtime/mfinal.cs" <<'PY2'
import io, sys
p = sys.argv[1]
t = io.open(p, encoding='utf-8', newline='').read()
t = t.replace('namespace go;\r\n',
  'namespace go;\r\n// the converted body started the dead runner via goǃ(runfinq); it is rewired now\r\n', 1)
io.open(p, 'w', encoding='utf-8', newline='').write(t)
PY2
  out=$(bash "$self" "$tmp/prose" 2>&1); rc=$?
  arms=$((arms+1))
  [ "$rc" -eq 0 ] || { echo "ARM 8 FAILED: a CORRECT file was rejected because its COMMENT names goǃ(runfinq) (rc=$rc)"; echo "$out"; return 1; }
  echo "  ok   a COMMENT naming the old body OK   the code check does not read prose"

  echo
  echo "SELF-TEST CLEAN -- $arms arms"
  return 0
}

case "${1:-}" in
  --self-test) selftest; exit $? ;;
  --verify)
    CORE=${2:-}; [ -n "$CORE" ] || die "usage: --verify <scratch-core-dir>"
    echo "== verifying $CORE"
    if verify "$CORE"; then echo "==> POST-CONDITION MET"; exit 0; else echo "==> POST-CONDITION FAILED"; exit 1; fi ;;
  ''|-*) die "usage: apply-h5-c1-1-rederives.sh <scratch-core-dir> | --verify <dir> | --self-test" ;;
esac

CORE=$1
check_precondition "$CORE"
echo "== applying the C1-1 re-derives to $CORE"
apply "$CORE"
echo "== verifying"
if verify "$CORE"; then echo "==> APPLIED and POST-CONDITION MET"; exit 0; fi
echo "==> APPLIED but the POST-CONDITION FAILED -- read the FAIL lines above"; exit 1
