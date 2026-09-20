#!/usr/bin/env bash
# ARM: the census fold temp must live under $STATE, never beside the CALLER's entry file.
#
# ⚠ G 664e6925b §4: "a door is a property of a PATH, not of a tool -- a tool with one door reads as
# sandboxed while it is not." Asked of this tool, the answer was the same: it has TWO doors
# (R_MAILBOX_CLONE, R_POST_STATE) and until 2026-09-20 the census still wrote `<entry>.lc.tmp` into
# whatever directory the caller's entry sat in, clobbering anything at that name. Found by AUDIT and
# confirmed by a DECOY, not by reading the line.
#
# Usage: r-post-scantemp-arm.sh <path-to-tool-under-test>
#   RED: derive the copy from the CURRENT tool and restore only the old path:
#        sed 's|low="$STATE/.r-census-scan.tmp"|low="$f.lc.tmp"|' r-post.sh > /tmp/oldtmp.sh
set -u
TOOL="$(readlink -f "${1:?usage: r-post-scantemp-arm.sh <tool>}")"
# C1 b68ed837d: the THIRD vacuity shape -- the subject never invoked at all. A relative tool
# path plus a cd read as a clean PASS in C1 arm. Refuse an unrunnable subject before any verdict.
[ -r "$TOOL" ] || { echo "REFUSED(2): the tool under test is not readable: $TOOL"; exit 2; }
ROOT="${2:-/c/go2cs-tmp/r-scantemp-arm}"
fails=0
ok(){ echo "  PASS  $1"; }
no(){ echo "  FAIL  $1"; fails=$((fails+1)); }

rm -rf "$ROOT"; mkdir -p "$ROOT/state" || exit 1
printf '## ARM ENTRY -- scan-temp locality, not for posting\n\nbody\n' > "$ROOT/entry.md"
printf 'subj\n' > "$ROOT/subj.txt"
printf 'DECOY-CONTENT-MUST-SURVIVE\n' > "$ROOT/entry.md.lc.tmp"
DECOY_BEFORE="$(sha256sum "$ROOT/entry.md.lc.tmp" | cut -d' ' -f1)"

echo "=== ARM: the caller's directory is not a scratch space ==="
OUT="$(R_POST_STATE="$ROOT/state" bash "$TOOL" "$ROOT/entry.md" "$ROOT/subj.txt" --dry-run 2>&1)"; RC=$?
echo "$OUT" | sed 's/^/    | /'

[ "$RC" -eq 0 ] && ok "the run still admits (rc 0)" || no "rc is $RC, expected 0"
echo "$OUT" | grep -q '4 of 4 planted tokens DETECTED' \
   && ok "the census self-test still fires 4 of 4" || no "the census self-test did not report 4 of 4"
# ⚠ THE LOAD-BEARING ONE
if [ -f "$ROOT/entry.md.lc.tmp" ] && [ "$(sha256sum "$ROOT/entry.md.lc.tmp" | cut -d' ' -f1)" = "$DECOY_BEFORE" ]; then
  ok "the decoy beside the entry SURVIVED byte-identical"
else
  no "the decoy beside the entry was clobbered -- the census wrote outside every door"
fi
# and the tool cleans up after itself inside its own state dir
[ -f "$ROOT/state/.r-census-scan.tmp" ] && no "the fold temp was left behind in \$STATE" \
                                        || ok "no fold temp left behind in \$STATE"
# nothing else appeared beside the caller's files
EXTRA="$(ls "$ROOT" | grep -v '^entry.md$' | grep -v '^entry.md.lc.tmp$' | grep -v '^subj.txt$' | grep -v '^state$')"
[ -z "$EXTRA" ] && ok "nothing else written beside the caller's files" || no "unexpected: $EXTRA"

echo
echo "ARM RESULT: $fails failing assertion(s)"
exit $fails
