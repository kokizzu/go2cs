#!/usr/bin/env bash
# run-probe.sh WORKTREE OUTDIR -- the 29-shape slice-bounds probe against the go2cs tree at WORKTREE.
#
# Stages this directory's main.go/go.mod as an UNTRACKED behavioral project (SliceBoundsShapes) under
# WORKTREE/src/tests/Behavioral, transpiles and compiles it with that tree's run-behavioral.ps1, then runs the built
# program ONCE PER CASE (`SliceBoundsShapes N`), because a CLR exception that escapes recover() ends the process and
# would hide every later case. Writes, into OUTDIR:
#   go.txt      the go run reading (all cases in one process; Go recovers every one)
#   cs.txt      one line per case: its first three non-stack lines, joined by "/", then " || rc=<exit code>"
#   main.cs     the emitted C#, for reading the Range / .slice(...) forms
#   rb.log      the run-behavioral log
# then removes the staged project. Compare cs.txt line by line with another tree's cs.txt (two trees whose only
# difference is the seat under test), and with go.txt for "equals Go".
#
# Needs on PATH: go (the pinned toolchain, GOTOOLCHAIN=local), dotnet, pwsh. Runs at MSBuild -m:4 through a temporary
# WORKTREE/Directory.Build.rsp, which it refuses to overwrite. The worktree must be clean in its tracked files.
set -u
W="${1:?usage: run-probe.sh <go2cs worktree> <outdir>}"
O="${2:?usage: run-probe.sh <go2cs worktree> <outdir>}"
HERE=$(cd "$(dirname "$0")" && pwd)
N=SliceBoundsShapes
B="$W/src/tests/Behavioral"
TEMPLATE=ShiftPrecedenceUnsigned

command -v go >/dev/null && command -v dotnet >/dev/null && command -v pwsh >/dev/null || { echo "needs go, dotnet and pwsh on PATH"; exit 3; }
[ -z "$(git -C "$W" status --porcelain --untracked-files=no)" ] || { echo "worktree has tracked changes"; exit 3; }
[ -e "$B/$N" ] && { echo "$B/$N already exists"; exit 3; }
[ -e "$W/Directory.Build.rsp" ] && { echo "$W/Directory.Build.rsp already exists"; exit 4; }
[ -f "$B/$TEMPLATE/$TEMPLATE.csproj" ] || { echo "template project $TEMPLATE missing"; exit 3; }
mkdir -p "$O"
echo "tree $(git -C "$W" rev-parse --short=10 HEAD), $(go version)"

(cd "$HERE" && go run . > "$O/go.txt" 2>&1) || { echo "go run failed"; exit 3; }
echo "go cases: $(wc -l < "$O/go.txt")"

mkdir "$B/$N" && cp "$HERE/main.go" "$HERE/go.mod" "$B/$N/" && cp "$B/$TEMPLATE/go2cs.ico" "$B/$N/" || exit 3
sed "s#<AssemblyName>$TEMPLATE</AssemblyName>#<AssemblyName>$N</AssemblyName>#" "$B/$TEMPLATE/$TEMPLATE.csproj" > "$B/$N/$N.csproj"
grep -q "<AssemblyName>$N</AssemblyName>" "$B/$N/$N.csproj" || { echo "csproj rename failed"; rm -rf "$B/$N"; exit 3; }

printf -- '-maxcpucount:4\n' > "$W/Directory.Build.rsp"
(cd "$B" && pwsh -NoProfile -File ./run-behavioral.ps1 --filter $N --phase transpile,compile > "$O/rb.log" 2>&1)
echo "run-behavioral rc=$?"
rm -f "$W/Directory.Build.rsp"
tr -d '\r' < "$O/rb.log" | grep -E '^\s+(Transpile|Compile) |error CS' | head -6

E=$(ls "$B/$N"/bin/Release/*/$N.exe "$B/$N"/bin/Release/*/$N 2>/dev/null | head -1)
: > "$O/cs.txt"
if [ -n "$E" ]; then
  i=0
  while [ $i -lt "$(wc -l < "$O/go.txt")" ]; do
    out=$("$E" $i 2>&1)
    rc=$?
    first=$(printf '%s\n' "$out" | tr -d '\r' | grep -v '^\s*at ' | head -3 | paste -sd'/' -)
    printf '%s || rc=%s\n' "${first:-<no output>}" "$rc" >> "$O/cs.txt"
    i=$((i + 1))
  done
  echo "C# cases: $(wc -l < "$O/cs.txt")"
fi
cp "$B/$N/main.cs" "$O/main.cs" 2>/dev/null
rm -rf "$B/$N"
echo "tracked changes after cleanup: $(git -C "$W" status --porcelain --untracked-files=no | wc -l)"
