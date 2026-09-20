#!/usr/bin/env bash
# Refuse a file that still carries an UNFILLED placeholder. Two forms:
#   (a) the @TOKEN@ form this lane's blocks use;
#   (b) an explicit token list (GATELINES-style bare words) via GPLACE_TOKENS, space separated.
# usage: g-placeholder-check.sh <file>...   -> exit 0 clean, 1 placeholder found (named), 2 usage
set -uo pipefail
[ $# -ge 1 ] || { echo "usage: g-placeholder-check.sh <file>..."; exit 2; }
TOKENS="${GPLACE_TOKENS-GATELINES TODO_FILL PLACEHOLDER XXXSHA}"
rc=0
for f in "$@"; do
  [ -f "$f" ] || { echo "PLACEHOLDER CHECK: missing file $f"; rc=1; continue; }
  hits=$(grep -n -E '@[A-Za-z0-9_]{2,}@' "$f" | head -20)
  if [ -n "$hits" ]; then echo "PLACEHOLDER (@TOKEN@ form) in $f:"; echo "$hits" | cut -c1-120; rc=1; fi
  for t in $TOKENS; do
    th=$(grep -n -w -- "$t" "$f" | head -5)
    if [ -n "$th" ]; then echo "PLACEHOLDER token '$t' in $f:"; echo "$th" | cut -c1-120; rc=1; fi
  done
done
[ $rc = 0 ] && echo "PLACEHOLDER CHECK CLEAN ($# file(s))"
exit $rc
