#!/usr/bin/env bash
# =================================================================================================
# h8-comparand.sh -- the H8 gate's two missing arms, as one instrument.
#
# H8's gate reads: "the platform manifest's marker gate is zero per target, and the default-flavor
# build reproduces the single-target build byte-for-byte." The 2026-09-13 amendment stopped short of
# scoring it, because at this hop NO outgoing manifest is committed (verified: no platform-manifest
# file has ever been tracked on any ref) and the byte-identity arm had no procedure. This script is
# the procedure for both, plus the package-delta derivation the class-count predictions come from.
#
#   classify   four-way census partition over three per-target manifests   (the comparand's engine)
#   compare    two classifications, ONE predicate, and the class-count delta
#   identity   the default-flavour byte-identity arm
#   pkgdelta   the std package delta per target at two releases            (the prediction's source)
#   selftest   every arm above MADE TO FAIL, then restored
#
# ------------------------------------------------------------------------------------------------
# WHY A SEEDED-ROOT MANIFEST IS NOT A CENSUS, AND THE TELL THAT REFUSES ONE
#
# The preserved H6 half-A / half-B artifacts are per-file manifests of SEEDED staging roots. A
# seeded root's path set is (seed union emitted) and all three targets share one seed, so such a
# manifest carries no emitted-vs-seeded discriminator -- which is exactly why the converter's own
# census uses a sentinel MTIME for that question rather than content. Classify three seeded-root
# manifests and `partial` and `exclusive` read from the SEED's path set, not from any emission.
#
# The tell is sharp and cheap: in a true per-target emission census a `*_windows.*` artifact CANNOT
# be emitted by the linux or darwin target. If a platform-suffixed artifact appears in a foreign
# target's manifest, the manifests cover a seeded root and `classify` REFUSES rather than returning
# a number that looks like a census. `--seeded-content-only` accepts them for the one question they
# CAN answer -- which shared paths differ in content across targets -- and says so in its output.
#
# ------------------------------------------------------------------------------------------------
# MANIFEST FORMAT (the form the fleet's preserved artifacts already use)
#
#   "<sha256><two spaces><relpath>", LC_ALL=C sorted, one artifact per line -- i.e. `sha256sum`
#   output over a stable relative walk. The TREE HASH of a manifest is sha256 of the manifest file
#   itself, which is order-independent only because the lines are sorted; this script re-sorts every
#   input before use so a differently-ordered manifest cannot read as a difference.
# =================================================================================================
set -u
PROG=${0##*/}

die(){ echo "$PROG: $*" >&2; exit 2; }

# ---- input hygiene: every compare asserts BOTH sides non-empty BEFORE reporting a difference -----
norm(){ # $1 = manifest path -> normalized "sha<TAB>path" on stdout
  local f="$1"
  [ -r "$f" ] || die "cannot read manifest '$f'"
  [ -s "$f" ] || die "manifest '$f' is EMPTY -- an empty baseline reports total disagreement, never a difference"
  local bad
  bad=$(grep -v '^# h8-comparand ' "$f" | grep -cvE '^[0-9a-fA-F]{64}[[:space:]]+[^[:space:]]')
  [ "$bad" -eq 0 ] || die "manifest '$f' has $bad line(s) not in '<sha256>  <relpath>' form"
  grep -v '^# h8-comparand ' "$f" | sed -E 's/^([0-9a-fA-F]{64})[[:space:]]+/\1\t/' | LC_ALL=C sort -t"$(printf '\t')" -k2,2
}
paths(){ cut -f2; }
treehash(){ LC_ALL=C sort "$1" | sha256sum | cut -d' ' -f1; }

# ---- the seed tell --------------------------------------------------------------------------------
# A platform-suffixed artifact in a FOREIGN target's manifest. Suffix set is the converter's own
# GOOS suffix vocabulary for the three H8 targets, matched on the stem before any extension.
seed_tell(){ # $1=win-norm $2=lin-norm $3=dar-norm ; prints offenders, returns 1 if the tell fires
  local hits=0 out
  out=$(
    paths < "$2" | grep -E '(^|/|_)[^/]*_windows(_test)?\.' | sed 's/^/  linux  manifest holds a windows artifact: /'
    paths < "$3" | grep -E '(^|/|_)[^/]*_windows(_test)?\.' | sed 's/^/  darwin manifest holds a windows artifact: /'
    paths < "$1" | grep -E '(^|/|_)[^/]*_(darwin|linux)(_test)?\.' | sed 's/^/  windows manifest holds a foreign artifact: /'
  )
  if [ -n "$out" ]; then printf '%s\n' "$out" | head -8; hits=$(printf '%s\n' "$out" | wc -l); fi
  [ "$hits" -eq 0 ]
}

# ---- classify -------------------------------------------------------------------------------------
# The partition is platformManifest.go's, verbatim, and in ITS order: a name emitted by every target
# is identical or variant by content; by exactly two, partial; by exactly one, exclusive.
do_classify(){
  local SEEDOK=0 ASSUMEFLAT=0
  while :; do
    case "${1:-}" in
      --seeded-content-only) SEEDOK=1; shift ;;
      --assume-flat)         ASSUMEFLAT=1; shift ;;
      *) break ;;
    esac
  done
  [ $# -eq 3 ] || die "usage: $PROG classify [--seeded-content-only] [--assume-flat] <win> <lin> <dar>"
  # ⚠ THE KEYING GATE. classify's answer is only a class census if its inputs are keyed on the FLAT
  # artifact path. A raw-path-keyed manifest yields the L3 TREE partition, which sums, passes the
  # partition check, clears the seed tell, and is wrong (see `manifest` above). Shape cannot
  # distinguish the two, so the format identifies itself: `manifest` stamps what it builds and this
  # refuses what carries no stamp. --assume-flat is for a manifest produced elsewhere (the preserved
  # H6 half-A/half-B artifacts), and the caller owns the claim.
  if [ "$ASSUMEFLAT" -eq 0 ]; then
    local f miss=0
    for f in "$1" "$2" "$3"; do
      head -1 "$f" 2>/dev/null | grep -q '^# h8-comparand manifest v1 key=flat-artifact-path' || { echo "  unstamped: $f" >&2; miss=1; }
    done
    if [ "$miss" -ne 0 ]; then
      echo "REFUSED: the manifest(s) above carry no flat-artifact-path stamp." >&2
      echo "         Build them with: $PROG manifest <census-target-root>   (it strips L3 layout folders" >&2
      echo "         structurally and stamps the result). A raw-path-keyed manifest scores variant 0 and" >&2
      echo "         every platform-varying artifact as exclusive -- a well-formed WRONG answer." >&2
      echo "         Pass --assume-flat only for a manifest built elsewhere that you know is flat-keyed." >&2
      exit 6
    fi
  fi
  local W L D T; T=$(mktemp -d)
  norm "$1" > "$T/w"; norm "$2" > "$T/l"; norm "$3" > "$T/d"

  # three byte-identical manifests are not three targets' emissions
  if cmp -s "$T/w" "$T/l" && cmp -s "$T/l" "$T/d"; then
    echo "REFUSED: all three manifests are identical after normalization -- this is one emission counted three times, not a census." >&2
    rm -rf "$T"; exit 3
  fi
  if ! seed_tell "$T/w" "$T/l" "$T/d"; then
    if [ "$SEEDOK" -eq 0 ]; then
      echo "REFUSED: SEED TELL FIRED (above). These manifests cover seeded staging roots, so they carry no" >&2
      echo "         emitted-vs-seeded discriminator and their partial/exclusive counts would be the SEED's" >&2
      echo "         path set, not an emission's. Re-run -platform-census under the release's own pin, or pass" >&2
      echo "         --seeded-content-only to get the one question a seeded pair CAN answer." >&2
      rm -rf "$T"; exit 3
    fi
    echo "  ⚠ SEEDED-CONTENT-ONLY: the seed tell fired; partial/exclusive below are NOT emission classes."
  fi

  paths < "$T/w" > "$T/pw"; paths < "$T/l" > "$T/pl"; paths < "$T/d" > "$T/pd"
  LC_ALL=C sort -u "$T/pw" "$T/pl" "$T/pd" > "$T/union"
  local ident=0 variant=0 partial=0 excl=0 n
  while IFS= read -r p; do
    local hw hl hd c; c=0
    hw=$(awk -F'\t' -v p="$p" '$2==p{print $1; exit}' "$T/w"); [ -n "$hw" ] && c=$((c+1))
    hl=$(awk -F'\t' -v p="$p" '$2==p{print $1; exit}' "$T/l"); [ -n "$hl" ] && c=$((c+1))
    hd=$(awk -F'\t' -v p="$p" '$2==p{print $1; exit}' "$T/d"); [ -n "$hd" ] && c=$((c+1))
    case "$c" in
      3) if [ "$hw" = "$hl" ] && [ "$hl" = "$hd" ]; then ident=$((ident+1)); else variant=$((variant+1)); fi ;;
      2) partial=$((partial+1)) ;;
      1) excl=$((excl+1)) ;;
    esac
  done < "$T/union"
  n=$(wc -l < "$T/union")
  # the L3 pricing identity the manifest computes rather than multiplies
  echo "  identical                 $ident"
  echo "  variant                   $variant"
  echo "  partial                   $partial"
  echo "  exclusive                 $excl"
  echo "  union total               $n"
  [ $((ident+variant+partial+excl)) -eq "$n" ] || { echo "REFUSED: partition does not sum to the union ($ident+$variant+$partial+$excl != $n)" >&2; rm -rf "$T"; exit 4; }
  echo "  partition sums to union   yes"
  printf 'CLASSCOUNTS %s %s %s %s %s\n' "$ident" "$variant" "$partial" "$excl" "$n"
  rm -rf "$T"
}

# ---- compare: one predicate, both sides, then the delta -------------------------------------------
do_compare(){
  local out=() in=() seen=0 FLAGS=()
  while :; do
    case "${1:-}" in --assume-flat|--seeded-content-only) FLAGS+=("$1"); shift ;; *) break ;; esac
  done
  for a in "$@"; do
    if [ "$a" = "--" ]; then seen=1; continue; fi
    if [ "$seen" -eq 0 ]; then out+=("$a"); else in+=("$a"); fi
  done
  [ "${#out[@]}" -eq 3 ] && [ "${#in[@]}" -eq 3 ] || die "usage: $PROG compare <out-win> <out-lin> <out-dar> -- <in-win> <in-lin> <in-dar>"
  echo "OUTGOING (the comparand):"
  local o; o=$("$0" classify "${FLAGS[@]}" "${out[@]}") || exit $?
  printf '%s\n' "$o" | grep -v '^CLASSCOUNTS'
  echo "INCOMING:"
  local i; i=$("$0" classify "${FLAGS[@]}" "${in[@]}") || exit $?
  printf '%s\n' "$i" | grep -v '^CLASSCOUNTS'
  local OV IV; OV=$(printf '%s\n' "$o" | grep '^CLASSCOUNTS'); IV=$(printf '%s\n' "$i" | grep '^CLASSCOUNTS')
  set -- $OV; local oi=$2 ov=$3 op=$4 oe=$5 ot=$6
  set -- $IV; local ii=$2 iv=$3 ip=$4 ie=$5 it=$6
  echo "DELTA (incoming - outgoing):"
  printf '  identical   %+d\n  variant     %+d\n  partial     %+d\n  exclusive   %+d\n  union       %+d\n' \
    $((ii-oi)) $((iv-ov)) $((ip-op)) $((ie-oe)) $((it-ot))
}

# ---- identity: the default-flavour byte-identity arm ----------------------------------------------
do_identity(){
  [ $# -eq 2 ] || die "usage: $PROG identity <manifest-A> <manifest-B>"
  local T; T=$(mktemp -d)
  norm "$1" > "$T/a"; norm "$2" > "$T/b"
  local na nb; na=$(wc -l < "$T/a"); nb=$(wc -l < "$T/b")
  paths < "$T/a" > "$T/pa"; paths < "$T/b" > "$T/pb"
  local onlya onlyb differ
  onlya=$(comm -23 "$T/pa" "$T/pb" | wc -l)
  onlyb=$(comm -13 "$T/pa" "$T/pb" | wc -l)
  differ=$(LC_ALL=C join -t"$(printf '\t')" -j0 -o 0,1.1,2.1 \
             <(awk -F'\t' '{print $2"\t"$1}' "$T/a" | LC_ALL=C sort) \
             <(awk -F'\t' '{print $2"\t"$1}' "$T/b" | LC_ALL=C sort) 2>/dev/null \
           | awk -F'\t' '$2!=$3' | wc -l)
  local ha hb; ha=$(treehash "$T/a"); hb=$(treehash "$T/b")
  echo "  A artifacts               $na"
  echo "  B artifacts               $nb"
  echo "  only in A                 $onlya"
  echo "  only in B                 $onlyb"
  echo "  same path, content DIFFER $differ"
  echo "  A tree hash               $ha"
  echo "  B tree hash               $hb"
  if [ "$onlya" -eq 0 ] && [ "$onlyb" -eq 0 ] && [ "$differ" -eq 0 ] && [ "$ha" = "$hb" ]; then
    echo "BYTE-IDENTITY ARM: PASS ($na artifacts, both sides non-empty)"; rm -rf "$T"; return 0
  fi
  echo "BYTE-IDENTITY ARM: FAIL"
  [ "$onlya" -gt 0 ] && { echo "  --- only in A (first 10) ---"; comm -23 "$T/pa" "$T/pb" | head -10 | sed 's/^/    /'; }
  [ "$onlyb" -gt 0 ] && { echo "  --- only in B (first 10) ---"; comm -13 "$T/pa" "$T/pb" | head -10 | sed 's/^/    /'; }
  rm -rf "$T"; return 1
}

# ---- pkgdelta: the prediction's source, with the H1 pin assertion built in ------------------------
# ⚠ MEASURED 2026-09-19 on a linux box: GO111MODULE=off SILENTLY CANCELS a GOTOOLCHAIN redirect --
# `GOTOOLCHAIN=go1.23.12 GO111MODULE=off go version` printed the AMBIENT toolchain at exit 0. The
# census instrument is specified WITH GO111MODULE=off, so that combination reads the wrong release
# and looks perfect. This mode therefore drives each release by its own GOROOT and asserts the
# release from `go version` OUTPUT before listing anything, per H1.
# ⚠ AND CGO_ENABLED is a real axis: it moves the LINUX count by exactly one package (runtime/cgo) at
# both releases. CGO_ENABLED=0 is pinned here, which is what reproduces the recorded census.
do_pkgdelta(){
  [ $# -eq 2 ] || die "usage: $PROG pkgdelta <goroot-outgoing> <goroot-incoming>"
  local T; T=$(mktemp -d)
  local names=(outgoing incoming) roots=("$1" "$2") i=0
  for R in "${roots[@]}"; do
    local which=${names[$i]}; i=$((i+1))
    [ -x "$R/bin/go" ] || die "no executable go at '$R/bin/go'"
    local v; v=$(GOTOOLCHAIN=local GOROOT="$R" GO111MODULE=off "$R/bin/go" version 2>/dev/null)
    case "$v" in *"go version go"*) ;; *) die "'$R/bin/go version' did not name a release: '$v'";; esac
    echo "  $which  $v"
    printf '%s\n' "$v" > "$T/$which.ver"
    for goos in windows linux darwin; do
      GOTOOLCHAIN=local GOROOT="$R" GO111MODULE=off GOOS=$goos GOARCH=amd64 CGO_ENABLED=0 \
        "$R/bin/go" list -tags purego,math_big_pure_go std 2>/dev/null | LC_ALL=C sort > "$T/$which-$goos.pkgs"
      [ -s "$T/$which-$goos.pkgs" ] || die "$which/$goos listed ZERO packages"
    done
  done
  cmp -s "$T/outgoing.ver" "$T/incoming.ver" && die "both GOROOTs run the SAME release -- the delta would be vacuous"
  echo
  echo "  per-target package counts"
  for goos in windows linux darwin; do
    printf '    %-8s outgoing %-4s incoming %-4s net %+d\n' "$goos" \
      "$(wc -l < "$T/outgoing-$goos.pkgs")" "$(wc -l < "$T/incoming-$goos.pkgs")" \
      "$(( $(wc -l < "$T/incoming-$goos.pkgs") - $(wc -l < "$T/outgoing-$goos.pkgs") ))"
  done
  echo
  echo "  added / removed per target"
  for goos in windows linux darwin; do
    comm -13 "$T/outgoing-$goos.pkgs" "$T/incoming-$goos.pkgs" > "$T/added-$goos"
    comm -23 "$T/outgoing-$goos.pkgs" "$T/incoming-$goos.pkgs" > "$T/removed-$goos"
    printf '    %-8s added %-4s removed %s\n' "$goos" "$(wc -l < "$T/added-$goos")" "$(wc -l < "$T/removed-$goos")"
  done
  local samea=yes samer=yes
  cmp -s "$T/added-windows" "$T/added-linux" && cmp -s "$T/added-linux" "$T/added-darwin" || samea=no
  cmp -s "$T/removed-windows" "$T/removed-linux" && cmp -s "$T/removed-linux" "$T/removed-darwin" || samer=no
  echo "    added set identical on all three targets:   $samea"
  echo "    removed set identical on all three targets: $samer"
  echo
  echo "  FILE-LEVEL classification of the added and removed sets (this is what moves the classes)"
  for side in added removed; do
    local R; [ "$side" = added ] && R="${roots[1]}" || R="${roots[0]}"
    for goos in windows linux darwin; do
      GOTOOLCHAIN=local GOROOT="$R" GO111MODULE=off GOOS=$goos GOARCH=amd64 CGO_ENABLED=0 \
        "$R/bin/go" list -tags purego,math_big_pure_go -f '{{range .GoFiles}}{{$.ImportPath}}|{{.}}
{{end}}' $(cat "$T/$side-$goos") 2>/dev/null | grep . | LC_ALL=C sort > "$T/$side-f-$goos"
    done
    LC_ALL=C sort -u "$T/$side-f-windows" "$T/$side-f-linux" "$T/$side-f-darwin" > "$T/$side-union"
    local on3=0 on2=0 on1=0
    while IFS= read -r n; do
      local c=0
      grep -Fxq "$n" "$T/$side-f-windows" && c=$((c+1))
      grep -Fxq "$n" "$T/$side-f-linux"   && c=$((c+1))
      grep -Fxq "$n" "$T/$side-f-darwin"  && c=$((c+1))
      case $c in 3) on3=$((on3+1));; 2) on2=$((on2+1));; 1) on1=$((on1+1));; esac
    done < "$T/$side-union"
    printf '    %-8s union %-4s on-all-3 %-4s on-2 (-> partial) %-4s on-1 (-> exclusive) %s\n' \
      "$side" "$(wc -l < "$T/$side-union")" "$on3" "$on2" "$on1"
    [ "$on1" -gt 0 ] && { echo "      the on-1 rows:"; while IFS= read -r n; do
        local c=0
        grep -Fxq "$n" "$T/$side-f-windows" && c=$((c+1)); grep -Fxq "$n" "$T/$side-f-linux" && c=$((c+1)); grep -Fxq "$n" "$T/$side-f-darwin" && c=$((c+1))
        [ "$c" -eq 1 ] && echo "        $n"
      done < "$T/$side-union"; }
  done
  rm -rf "$T"
}

# ---- manifest: build classify's input from a census root, KEYED ON THE FLAT ARTIFACT PATH ---------
# ⚠ THIS MODE EXISTS BECAUSE THE OBVIOUS HAND-BUILT MANIFEST IS SILENTLY WRONG (G, 2026-09-19,
# measured on the real incoming census). A census staging root is SEEDED from an L3 corpus, so a
# platform-varying artifact sits under a per-GOOS layout folder: `os/windows/file.cs` in the windows
# root, `os/linux/file.cs` in the linux one. Keyed on the RAW relative path those are two different
# names, so EVERY platform-varying artifact scores `exclusive` and `variant` collapses to exactly 0:
#
#     raw relative path   identical 1631 · variant 0  · partial 0  · exclusive 718 · union 2349
#     flat artifact path  identical 1631 · variant 83 · partial 93 · exclusive 283 · union 2090
#     the converter                 1631 ·         83 ·         93 ·           283 ·       2090
#
# The raw row SUMS TO ITS OWN UNION, PASSES the partition check and CLEARS the seed tell — it is the
# L3 TREE partition (1631 + 718 = l3UnionTreeTotal), a true answer to a different question. Only a
# reader noticing `variant 0` unaided would catch it. So the flat key is not a convention here: it is
# produced by this mode, the mode STAMPS the manifest, and `classify` REFUSES an unstamped one.
#
# platformCensus.go keys an artifact by its flat package-relative path (`variantFiles` reads
# `os/file.cs`, never `os/windows/file.cs`), and stripping to that reproduces its four classes exactly.
# Stripping uses the STRUCTURAL discriminator, never the directory names: measured on the incoming
# emission each target holds 100 GOOS-named directories, of which 99 are layout folders and ONE is a
# real package (`internal/syscall/windows`, which carries its own .csproj). A name filter deletes that
# package from two of the three views.
MANIFEST_STAMP='# h8-comparand manifest v1 key=flat-artifact-path'
do_manifest(){
  [ $# -eq 1 ] || die "usage: $PROG manifest <census-target-root>"
  local ROOT="$1"
  [ -d "$ROOT" ] || die "no such census root '$ROOT'"
  cd "$ROOT" || die "cannot enter '$ROOT'"
  local n=0 T; T=$(mktemp)
  while IFS= read -r f; do
    local rel=${f#./} out="" d base parent
    d=$(dirname "$rel"); base=$(basename "$rel")
    # strip EVERY layout-folder segment from the directory chain, innermost out
    local parts="" seg
    while [ "$d" != "." ] && [ -n "$d" ]; do
      seg=${d##*/}; parent=$(dirname "$d")
      case "$seg" in
        windows|linux|darwin)
          if compgen -G "$d/*.csproj" >/dev/null 2>&1 || ! compgen -G "$parent/*.csproj" >/dev/null 2>&1; then
            parts="$seg${parts:+/$parts}"      # a real package directory: KEEP it
          fi                                    # else: a layout folder -- drop it
          ;;
        *) parts="$seg${parts:+/$parts}" ;;
      esac
      d="$parent"
    done
    out="${parts:+$parts/}$base"
    printf '%s  %s\n' "$(sha256sum "$rel" | cut -d' ' -f1)" "$out" >> "$T"
    n=$((n+1))
  done < <(find . -type f -name '*.cs' -not -name '*.cs.auto' | LC_ALL=C sort)
  [ "$n" -gt 0 ] || { rm -f "$T"; die "manifest walked ZERO .cs files -- an empty manifest is not a census"; }
  # stripping must not merge two distinct artifacts onto one key
  local dup; dup=$(cut -d' ' -f3- "$T" | LC_ALL=C sort | uniq -d | head -5)
  if [ -n "$dup" ]; then
    echo "REFUSED: stripping produced DUPLICATE keys -- two artifacts merged onto one name:" >&2
    printf '%s\n' "$dup" | sed 's/^/  /' >&2; rm -f "$T"; exit 5
  fi
  printf '%s\n' "$MANIFEST_STAMP"
  LC_ALL=C sort -k2,2 "$T"
  rm -f "$T"
}

# ---- view: the DEFAULT-FLAVOUR manifest of a corpus root -------------------------------------------
# The default flavour of an L3 corpus is what a build for <host> compiles: the FLAT files in each
# package directory plus that package's <host>/ folder, and nothing from a foreign GOOS folder.
#
# ⚠ A GOOS-NAMED DIRECTORY IS NOT AUTOMATICALLY A LAYOUT FOLDER. `internal/syscall/windows` is a
# PACKAGE whose directory is named `windows`, and it exists on every target's view. A filter that
# excludes any path component in {windows,linux,darwin} silently drops that whole package from the
# linux and darwin manifests -- and dropping the same paths from BOTH sides makes the arm agree
# about files it never looked at, which is the vacuous green this arm exists to avoid.
#
# The discriminator is structural and exact: a directory is a LAYOUT folder iff its name is a GOOS
# name, it holds no .csproj of its own, and its PARENT holds a .csproj. A package directory named
# `windows` carries its own .csproj and is therefore kept.
do_view(){
  [ $# -eq 2 ] || die "usage: $PROG view <corpus-root> <host-goos>"
  local ROOT="$1" HOST="$2"
  [ -d "$ROOT" ] || die "no such corpus root '$ROOT'"
  case "$HOST" in windows|linux|darwin) ;; *) die "host goos must be windows, linux or darwin" ;; esac
  local n=0
  cd "$ROOT" || die "cannot enter '$ROOT'"
  while IFS= read -r f; do
    local rel=${f#./} keep=1 d
    d=$(dirname "$rel")
    # walk every ancestor directory of the file, looking for a LAYOUT folder that is not the host's
    while [ "$d" != "." ] && [ -n "$d" ]; do
      local base=${d##*/} parent; parent=$(dirname "$d")
      case "$base" in
        windows|linux|darwin)
          # layout folder iff no .csproj here AND the parent has one
          if ! compgen -G "$d/*.csproj" >/dev/null 2>&1 && compgen -G "$parent/*.csproj" >/dev/null 2>&1; then
            [ "$base" = "$HOST" ] || { keep=0; break; }
          fi ;;
      esac
      d="$parent"
    done
    [ "$keep" -eq 1 ] || continue
    printf '%s  %s\n' "$(sha256sum "$rel" | cut -d' ' -f1)" "$rel"
    n=$((n+1))
  done < <(find . -type f -name '*.cs' -not -name '*.cs.auto' | LC_ALL=C sort)
  [ "$n" -gt 0 ] || die "view produced ZERO artifacts -- a filter that selects nothing cannot be compared"
} 

# ---- selftest: every arm MADE TO FAIL, then restored ----------------------------------------------
st_pass=0; st_fail=0
st(){ # $1=label $2=expected-rc ; rest = command
  local label="$1" want="$2"; shift 2
  "$@" >/dev/null 2>&1; local got=$?
  if [ "$got" -eq "$want" ]; then st_pass=$((st_pass+1)); printf '  PASS  %-58s rc=%s\n' "$label" "$got"
  else st_fail=$((st_fail+1)); printf '  FAIL  %-58s rc=%s want=%s\n' "$label" "$got" "$want"; fi
}
do_selftest(){
  local T; T=$(mktemp -d)
  h(){ printf '%064d' "$1"; }
  # a clean three-target EMISSION: same paths, one content differs, plus one exclusive per target
  { echo "$(h 1)  a/x.cs"; echo "$(h 2)  a/y.cs"; echo "$(h 7)  a/only_windows.cs"; } | LC_ALL=C sort > "$T/w"
  { echo "$(h 1)  a/x.cs"; echo "$(h 3)  a/y.cs"; echo "$(h 8)  a/only_linux.cs";   } | LC_ALL=C sort > "$T/l"
  { echo "$(h 1)  a/x.cs"; echo "$(h 3)  a/y.cs"; echo "$(h 9)  a/only_darwin.cs";  } | LC_ALL=C sort > "$T/d"
  echo "A. classify -- the healthy case and its arithmetic"
  local out; out=$("$0" classify --assume-flat "$T/w" "$T/l" "$T/d")
  st "clean three-target emission classifies"        0 "$0" classify --assume-flat "$T/w" "$T/l" "$T/d"
  local got; got=$(printf '%s\n' "$out" | grep '^CLASSCOUNTS')
  if [ "$got" = "CLASSCOUNTS 1 1 0 3 5" ]; then st_pass=$((st_pass+1)); printf '  PASS  %-58s %s\n' "counts are identical1/variant1/partial0/exclusive3" "$got"
  else st_fail=$((st_fail+1)); printf '  FAIL  %-58s %s\n' "expected CLASSCOUNTS 1 1 0 3 5" "$got"; fi
  echo "B. classify -- every refusal MADE TO FAIL"
  : > "$T/empty"
  st "an EMPTY manifest refuses (never 'total disagreement')" 2 "$0" classify --assume-flat "$T/empty" "$T/l" "$T/d"
  printf 'not-a-manifest\n' > "$T/junk"
  st "a malformed manifest refuses"                 2 "$0" classify --assume-flat "$T/junk" "$T/l" "$T/d"
  st "three IDENTICAL manifests refuse"             3 "$0" classify --assume-flat "$T/w" "$T/w" "$T/w"
  # the seed tell: put the windows-only artifact into the linux and darwin roots (a seeded root)
  { cat "$T/l"; echo "$(h 7)  a/only_windows.cs"; } | LC_ALL=C sort > "$T/l_seeded"
  { cat "$T/d"; echo "$(h 7)  a/only_windows.cs"; } | LC_ALL=C sort > "$T/d_seeded"
  st "SEED TELL refuses a seeded-root triple"       3 "$0" classify --assume-flat "$T/w" "$T/l_seeded" "$T/d_seeded"
  st "  and --seeded-content-only admits it"        0 "$0" classify --seeded-content-only --assume-flat "$T/w" "$T/l_seeded" "$T/d_seeded"
  echo "C. identity -- the arm, and the three ways it must go red"
  cp "$T/w" "$T/w2"
  st "identical manifests PASS"                     0 "$0" identity "$T/w" "$T/w2"
  sed "s|$(h 2)  a/y.cs|$(h 5)  a/y.cs|" "$T/w" > "$T/w_content"
  st "one CONTENT change goes red"                  1 "$0" identity "$T/w" "$T/w_content"
  sed 's|a/y.cs|a/y_renamed.cs|' "$T/w" > "$T/w_path"
  st "one PATH change goes red (normalization is not eating it)" 1 "$0" identity "$T/w" "$T/w_path"
  st "an EMPTY side refuses rather than 'agreeing'" 2 "$0" identity "$T/empty" "$T/w"
  # order-independence must NOT read as a difference
  LC_ALL=C sort -r "$T/w" > "$T/w_rev"
  st "a reordered manifest still PASSES"            0 "$0" identity "$T/w" "$T/w_rev"
  echo "D. compare -- one predicate both sides, and a delta that is MEASURED, not assumed"
  # the incoming triple gains one shared artifact and loses the darwin-exclusive one, so the delta
  # must be identical +1 and exclusive -1. A control that compares a triple with ITSELF proves only
  # that the code runs; it cannot tell a working delta from a hardcoded row of zeros.
  { cat "$T/w"; echo "$(h 4)  a/z.cs"; } | LC_ALL=C sort > "$T/w2i"
  { cat "$T/l"; echo "$(h 4)  a/z.cs"; } | LC_ALL=C sort > "$T/l2i"
  { grep -v only_darwin "$T/d"; echo "$(h 4)  a/z.cs"; } | LC_ALL=C sort > "$T/d2i"
  st "compare runs on two DISTINCT triples"         0 "$0" compare --assume-flat "$T/w" "$T/l" "$T/d" -- "$T/w2i" "$T/l2i" "$T/d2i"
  local dlt; dlt=$("$0" compare --assume-flat "$T/w" "$T/l" "$T/d" -- "$T/w2i" "$T/l2i" "$T/d2i" | sed -n '/^DELTA/,$p')
  if printf '%s\n' "$dlt" | grep -q 'identical   +1' && printf '%s\n' "$dlt" | grep -q 'exclusive   -1'; then
    st_pass=$((st_pass+1)); printf '  PASS  %-58s %s\n' "the delta is non-zero and correct (+1 identical, -1 exclusive)" "measured"
  else
    st_fail=$((st_fail+1)); printf '  FAIL  %-58s\n' "delta wrong:"; printf '%s\n' "$dlt" | sed 's/^/        /'
  fi
  st "compare refuses an empty side"                2 "$0" compare --assume-flat "$T/empty" "$T/l" "$T/d" -- "$T/w" "$T/l" "$T/d"
  echo "E. view -- the default-flavour filter, and the package-vs-layout trap"
  local C="$T/corpus"
  mkdir -p "$C/pkg/windows" "$C/pkg/linux" "$C/pkg/darwin" "$C/internal/syscall/windows"
  : > "$C/pkg/pkg.csproj"
  echo flat    > "$C/pkg/shared.cs"
  echo win     > "$C/pkg/windows/only.cs"
  echo lin     > "$C/pkg/linux/only.cs"
  echo dar     > "$C/pkg/darwin/only.cs"
  # a PACKAGE directory named windows: it carries its own .csproj and must survive every host view
  : > "$C/internal/syscall/windows/internal.syscall.windows.csproj"
  echo pkgwin  > "$C/internal/syscall/windows/zsyscall.cs"
  local vl; vl=$("$0" view "$C" linux)
  st "view runs on a corpus root"                   0 "$0" view "$C" linux
  if printf '%s\n' "$vl" | grep -q 'pkg/shared.cs' \
     && printf '%s\n' "$vl" | grep -q 'pkg/linux/only.cs' \
     && ! printf '%s\n' "$vl" | grep -q 'pkg/windows/only.cs' \
     && ! printf '%s\n' "$vl" | grep -q 'pkg/darwin/only.cs'; then
    st_pass=$((st_pass+1)); printf '  PASS  %-58s\n' "linux view keeps flat+linux, drops windows/darwin layout"
  else st_fail=$((st_fail+1)); printf '  FAIL  %-58s\n' "linux view filter wrong:"; printf '%s\n' "$vl" | sed 's/^/        /'; fi
  if printf '%s\n' "$vl" | grep -q 'internal/syscall/windows/zsyscall.cs'; then
    st_pass=$((st_pass+1)); printf '  PASS  %-58s\n' "a PACKAGE dir named windows survives the linux view"
  else st_fail=$((st_fail+1)); printf '  FAIL  %-58s\n' "the package-vs-layout discriminator dropped a real package"; fi
  # and the arm must SEE a difference that lives in the host folder
  local v1="$T/v1.man" v2="$T/v2.man"
  "$0" view "$C" linux > "$v1"
  echo changed > "$C/pkg/linux/only.cs"
  "$0" view "$C" linux > "$v2"
  st "a change INSIDE the host folder is seen by identity" 1 "$0" identity "$v1" "$v2"
  # a change in a FOREIGN folder must NOT move the host view (that is the view's whole job)
  echo changed2 > "$C/pkg/windows/only.cs"
  "$0" view "$C" linux > "$T/v3.man"
  st "a change in a FOREIGN folder leaves the host view alone" 0 "$0" identity "$v2" "$T/v3.man"
  echo "F. manifest + the KEYING GATE -- the trap G measured, built as a control"
  # a synthetic L3 census root per target: one flat shared file, one per-GOOS layout folder holding
  # the SAME artifact name, and a real package directory named `windows` that must survive.
  local C2R="$T/census"
  for g in windows linux darwin; do
    mkdir -p "$C2R/$g/pkg/$g" "$C2R/$g/internal/syscall/windows"
    : > "$C2R/$g/pkg/pkg.csproj"
    echo shared                       > "$C2R/$g/pkg/shared.cs"
    echo "variant-body-for-$g"        > "$C2R/$g/pkg/$g/file.cs"
    : > "$C2R/$g/internal/syscall/windows/internal.syscall.windows.csproj"
    echo pkgwin                       > "$C2R/$g/internal/syscall/windows/zsyscall.cs"
  done
  st "manifest builds from a census root"           0 "$0" manifest "$C2R/windows"
  for g in windows linux darwin; do "$0" manifest "$C2R/$g" > "$T/m-$g" 2>/dev/null; done
  # the real package survives the strip; the layout folder does not
  if grep -q 'internal/syscall/windows/zsyscall.cs' "$T/m-linux" && ! grep -qE '(^|[[:space:]])pkg/(windows|linux|darwin)/' "$T/m-linux"; then
    st_pass=$((st_pass+1)); printf '  PASS  %-58s\n' "flat key keeps a real package dir, drops the layout folder"
  else st_fail=$((st_fail+1)); printf '  FAIL  %-58s\n' "flat-key stripping wrong:"; sed 's/^/        /' "$T/m-linux"; fi
  # THE POINT: flat keying finds the variant; raw keying calls it exclusive and reads variant 0
  local flat raw
  flat=$("$0" classify "$T/m-windows" "$T/m-linux" "$T/m-darwin" | grep '^CLASSCOUNTS')
  for g in windows linux darwin; do ( cd "$C2R/$g" && find . -type f -name '*.cs' | LC_ALL=C sort | while IFS= read -r f; do printf '%s  %s\n' "$(sha256sum "$f" | cut -d' ' -f1)" "${f#./}"; done ) > "$T/r-$g"; done
  raw=$("$0" classify --assume-flat "$T/r-windows" "$T/r-linux" "$T/r-darwin" | grep '^CLASSCOUNTS')
  # flat: shared.cs + zsyscall.cs identical, pkg/file.cs variant  -> identical 2 variant 1 exclusive 0
  # raw:  pkg/<goos>/file.cs are three DIFFERENT names            -> variant 0, exclusive 3
  if [ "$flat" = "CLASSCOUNTS 2 1 0 0 3" ] && [ "$raw" = "CLASSCOUNTS 2 0 0 3 5" ]; then
    st_pass=$((st_pass+1)); printf '  PASS  %-58s\n' "flat finds the variant; raw reads variant 0 (the trap reproduced)"
  else st_fail=$((st_fail+1)); printf '  FAIL  %-58s flat=%s raw=%s\n' "trap control" "$flat" "$raw"; fi
  st "an UNSTAMPED manifest triple is REFUSED"      6 "$0" classify "$T/r-windows" "$T/r-linux" "$T/r-darwin"
  st "  and --assume-flat admits it (caller owns it)" 0 "$0" classify --assume-flat "$T/r-windows" "$T/r-linux" "$T/r-darwin"
  mkdir -p "$T/emptyroot"
  st "manifest refuses a root with no .cs"          2 "$0" manifest "$T/emptyroot"
  # duplicate-key refusal: two layout folders holding the same artifact name collapse onto one key
  mkdir -p "$C2R/dup/pkg/windows" "$C2R/dup/pkg/linux"; : > "$C2R/dup/pkg/pkg.csproj"
  echo a > "$C2R/dup/pkg/windows/same.cs"; echo b > "$C2R/dup/pkg/linux/same.cs"
  st "stripping that MERGES two artifacts is REFUSED" 5 "$0" manifest "$C2R/dup"
  rm -rf "$T"
  echo
  echo "SELF-TEST: pass=$st_pass fail=$st_fail"
  [ "$st_fail" -eq 0 ] || { echo "SELF-TEST FAILED"; return 1; }
  echo "SELF-TEST PASSED"
}

case "${1:-}" in
  classify) shift; do_classify "$@" ;;
  compare)  shift; do_compare "$@" ;;
  identity) shift; do_identity "$@" ;;
  view)     shift; do_view "$@" ;;
  manifest) shift; do_manifest "$@" ;;
  pkgdelta) shift; do_pkgdelta "$@" ;;
  selftest) shift; do_selftest ;;
  *) echo "usage: $PROG classify [--seeded-content-only] <win> <lin> <dar>"
     echo "       $PROG compare <out-win> <out-lin> <out-dar> -- <in-win> <in-lin> <in-dar>"
     echo "       $PROG identity <manifest-A> <manifest-B>"
     echo "       $PROG view <corpus-root> <host-goos>"
     echo "       $PROG manifest <census-target-root>   (builds classify's input, flat-keyed and stamped)"
     echo "       $PROG pkgdelta <goroot-outgoing> <goroot-incoming>"
     echo "       $PROG selftest"; exit 2 ;;
esac
