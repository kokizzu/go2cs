#!/usr/bin/env bash
# =================================================================================================
# coord-identifier-census.sh -- the fleet's ONE identifier census. Reference consumer of
# coord-identifier-patterns.txt (the ONE definition of the arms) and coord-identifier-hashes.txt
# (the ONE list of denied names, as hashes, generated from the repository's own Go guard).
#
# -------------------------------------------------------------------------------------------------
# HOW A POST TOOL ADOPTS IT
#
#   C="$(dirname "$0")/coord-identifier-census.sh"
#   "$C" entry   "$ENTRYFILE"        || exit $?      # THE GATE
#   "$C" subject "$COMMIT_SUBJECT"   || exit $?      # THE GATE
#   git fetch origin claude/mailbox                  # then, and only then:
#   TIP="$(git rev-parse FETCH_HEAD)"
#   "$C" tree docs/phase4/MAILBOX.md "$TIP"          # A READING. Do not gate the push on it.
#   # ... append, commit, push.
#
# Each gate call exits non-zero on a refusal, so `|| exit $?` is the whole wiring. THE CENSUS RUNS IN
# ITS OWN COMMAND, BEFORE the push and NOT in the same chain as it: a census composed into the push
# chain lets the push run on whatever the census printed. A census whose exit code does not gate the
# push is a guard built and not armed.
#
# ⚠ TREE MODE IS A READING, NOT THE GATE, AND ITS BASELINE IS THE FETCHED TIP.
#
# `entry` and `subject` are what decides whether a post may go out: they read the bytes this lane
# wrote, in STRICT mode, and nothing else. `tree` answers a different question -- what does the
# shared surface hold, and what would this append add to it -- and its answer is only as good as its
# baseline.
#
# THE BASELINE MUST BE THE TIP BLOB AS FETCHED IMMEDIATELY BEFORE THE APPEND. Never a lane's
# last-read sha, never a stored anchor, never "the sha I posted last time". Everything other lanes
# landed in between is otherwise attributed to THIS post. C2 measured both arms on one clean entry:
# baseline = its own last-read sha gave added=4 and REFUSED; baseline = the freshly fetched tip gave
# added=0 and CLEAN. The four were six other lanes' entries that had landed in the interval. Same
# post, same bytes, opposite verdicts -- the difference was entirely the baseline.
#
# So a post tool calls tree AFTER its fetch, with the sha that fetch produced, and treats a non-zero
# exit as something to READ rather than something to obey. A pre-existing hit on a shared surface is
# not this post's to fix and not this post's to be blocked by.
#
# NO POST TOOL EVER PASSES --unmask. See MASKING below.
#
# THE RULE: NO TOOL CARRIES A PRIVATE COPY OF ANY ARM. If an arm is wrong, it is wrong HERE, for
# everyone, and it is fixed HERE. A tool that adds a local pattern has re-created the three-tools-
# three-definitions state this file exists to end. A tool that needs an arm WIDENED for one case
# passes that widening as a per-arm admit set in the patterns file -- never as a list every arm
# shares, which widens all of them.
#
# -------------------------------------------------------------------------------------------------
# RELATION TO THE REPOSITORY'S OWN GO GUARD
#
# src/go2cs/internal/repoguard/fleetIdentifierCensus_test.go (TestNoFleetIdentifiersInTrackedFiles)
# is the TRACKED-TREE gate: it runs under the plain `go test ./...`, scans every tracked file, and
# is the authority on the structural classes and on which names are denied. This census is the
# PRE-POST gate: it runs before anything becomes tracked or pushed, over an entry body, a commit
# subject, or a shared transport file.
#
#   same classes  the profile/home arm is that file's fleetProfileRe, split into its two
#                 alternatives; the backslash share arm is its fleetNetworkRe; the placeholder admit
#                 set is its fleetIsPlaceholder plus fleetPlaceholderSegments; the UNC host admit
#                 set is its fleetNicknameHostSegments, exactly those four and no more.
#   same names    coord-identifier-hashes.txt is generated from its fleetDeniedTokens. Neither file
#                 carries plaintext.
#   same tokens   candidates are maximal [a-z0-9._-] runs PLUS each dot/hyphen/underscore component,
#                 which is what makes a machine name and the account name inside it both match.
#   what is new   the IPv4 arm, the forward-slash share arm, the host-assignment arm and the
#                 owner-name-in-prose arm have no counterpart there and are marked as additions.
#   what differs  the Go guard hashes every token of every tracked file in process. A shell census
#                 cannot, so this one inverts the comparison: it hashes the handful of literals the
#                 LOCAL BOX can derive and reports which hashed rows they matched. A denied row this
#                 box cannot supply plaintext for is NOT an arm here -- the Go guard still catches it
#                 at test time, and the local never-push token file is how an operator closes it for
#                 the pre-post gate. Every run prints hashes=N matched=M so the gap is a number.
#
# The two are COMPLEMENTARY and neither certifies the other.
#
# -------------------------------------------------------------------------------------------------
# NAMING CONVENTION -- USE A CLASS NAME WITH PROVENANCE, NEVER AN INSTANCE
#
# The fleet's own discussion of this class is the fastest way the class GROWS: arguing about the
# IPv4 arm put seven new matchable strings on the shared surface in forty minutes, and every one is
# indistinguishable to every arm from the thing being guarded. When a post, a commit message, a
# board row or a census report must refer to one of these, it names the CLASS AND ITS PROVENANCE:
#
#     the «ipv4» in <lane>'s <item> line          the «unc» in <file>:<line>
#     the «profile-path» in <lane>'s <item>       the «account» in <file>:<line>
#     the «version-quad» in <lane>'s <item>       the «host» in <lane>'s <item>
#
# The class alone is not enough -- a reader cannot tell a known negative from a leak from a bare
# «ipv4». The provenance is what makes the sentence decidable without the value.
#
# -------------------------------------------------------------------------------------------------
# MASKING -- THE REPORT AND THE REFUSAL ARE MASKED IDENTICALLY
#
# The REFUSAL path is the only path that runs when a real identifier is actually present, so it is
# the path that must not print one. It does not get its own renderer: refusal and report are the
# same lines from the same function. Every hit prints ARM, PASS, LINE NUMBER, a MASKED rendering and
# a short fingerprint -- and never the matched line and never the matched value.
#
#   «ipv4»            N.x.x.x                first octet only
#   everything else   <*REDACTED-<len>*>     COORD's rule: length only, no fragment
#   fingerprint       fp=<8 hex>             FNV-1a of the lower-cased value, so two different hits
#                                            stay distinguishable while neither is readable
#
# --unmask is for a LOCAL CONSOLE ONLY and is OFF by default. It adds the matched VALUE in the clear
# and the matched LINE rendered through COORD's line rule -- longest literal first, so a contained
# literal is never masked ahead of the literal that contains it (masking a 7-character account
# before the 13-character machine name that contains it renders the machine as
# `<*REDACTED-7*>-...`, which discloses exactly the infrastructure detail the order forbids), then
# the profile-root, home-prefix and share-prefix shape masks, then truncation at 160 characters. NO
# POST TOOL EVER PASSES IT, and nothing --unmask prints may be pasted into a post.
#
# -------------------------------------------------------------------------------------------------
# MODES
#   entry <file>              whole-entry census. Refuses on ANY hit. STRICT.
#   subject <string>          the same census over one string (a commit subject). STRICT.
#   tree <file> <baseline>    DELTA census over a tree-wide surface: hits in <file> at the worktree
#                             (or at HEAD if it is not on disk) MINUS hits in `git show
#                             <baseline>:<file>`. Refuses ONLY on hits this post ADDS. Prints the
#                             pre-existing count per arm loudly -- a pre-existing hit is not this
#                             post's to fix and not this post's to be blocked by, but it is never
#                             silent.
#   selftest                  the triad: PLANTS that must refuse, KNOWN NEGATIVES that must pass,
#                             and the DECLARED SET. Plants live only in the test's temp files and
#                             are never echoed; the output names arms and PASS/FAIL only.
#
# RULE 4 IS ADJACENCY, NEVER A WINDOW. A version word is allowed to excuse a quad only from the
# quad's OWN whitespace-delimited word or from the word immediately before or immediately after it.
# The first draft read a 56/32-character window and C2's A/B showed what that costs: a routable-
# shaped quad ALONE was refused, and the same quad with any version word loose in the window read
# CLEAN. A window is a laundering surface -- it lets a sentence exonerate an address that has nothing
# to do with it, and a lane that wanted to spell one only had to mention a version nearby.
#
# RUN-TIME TOKEN ARMS ARE BOUNDED BY THE DENIED SET. A derived token is a GUESS about what this box
# is called; on a container it is an ordinary English word. C2 measured 4348 pre-existing hits from
# RUNTIME_ACCOUNT and RUNTIME_OWNERNAME on its own box from exactly that. A derivation is now used
# only if it is 5+ characters, is not a stop-listed generic account name, AND is in the denied set --
# its hash a row in coord-identifier-hashes.txt, or its literal a line in the local never-push token
# file. The denied set is the authority on which names are forbidden; a derivation that is not in it
# is a name this fleet never denied. An arm that fails those bars prints
# `<ARM>: token not in the denied set, arm inert` and does not fire -- reported, never silent.
#
# STRICT vs DELTA. In STRICT mode the IPv4 arm runs with NO context exclusion at all: a lane must
# not be able to spell an identifier by making the sentence around it sound like documentation, and
# the cost of the strict reading falls on the writer, as one rewrite of their own post. The version,
# branch, assembly and documentation-constant context rules apply ONLY in tree mode, where their job
# is to CLASSIFY the pre-existing hits of a long shared surface -- an unclassified total is a number,
# never a finding. THE ONE EXCLUSION STRICT MODE TAKES is the release_literal ADMIT SET (rule 5): it
# is read on the QUAD'S OWN CHARACTERS and on nothing around it, so unlike a context rule it cannot
# be arranged by the sentence a lane writes -- which is exactly why a context rule is refused here
# and a shape admit is not. What it costs is stated on that arm's own line in the definition.
# Every other arm is identical in both modes.
#
# `entry`/`subject` ask "does what I am about to write carry one?". `tree` asks "does what I am
# about to write ADD one to a surface that already holds some?". Two questions; a clean reading from
# one does not certify the other.
#
# EXIT CODES
#   0  clean
#   1  refused -- one or more hits (for `tree`, one or more ADDED hits)
#   2  misuse, or the instrument could not measure. A gate that cannot measure does not pass.
#   3  self-test failed
#
# -------------------------------------------------------------------------------------------------
# THREE PASSES (R's shape, kept)
#   PASS 1  every arm, per line. Token candidates are tokenised as the Go guard tokenises them.
#   PASS 2  every arm, over each ADJACENT LINE PAIR joined with the whitespace at the break removed
#           (trailing on the left, indentation on the right). A line-anchored census cannot see a
#           token WRAPPED ACROSS A LINE BREAK, and joining on a bare newline alone still misses the
#           two commonest real shapes -- an indented continuation and a trailing space at the break.
#           Reported only when the match SPANS the join, so PASS 2 never re-reports PASS 1.
#   PASS 3  the run-time token arms ONLY, as a bare substring over an ALPHANUMERICS-ONLY reduction
#           of the line and of the joined pair. This catches a token broken by separators INSIDE a
#           component, which run-and-component tokenising cannot see. It is sound for pure
#           alphanumeric tokens and would over-fire wildly on the path arms, so it is scoped to the
#           token arms and to tokens of 4+ characters, and it skips what PASS 1 or 2 already found.
#
# INSTRUMENT FAILURE IS A REFUSAL, NEVER A CLEAN READ. R's tool gets this from `grep rc > 1`; there
# is no grep in this pipeline, so the same property is carried by four checks: the patterns file must
# be readable and yield 1+ arms; the input must be readable; awk must exit 0; and the arm count awk
# reports must equal the arm count this script counts independently from the same file. Any of them
# failing exits 2. A guard that cannot read its input PASSES it -- that is how a placeholder arm once
# failed open -- so none of these is allowed to be silent.
#
# No `set -o pipefail` and no `| grep -q` anywhere: awk writes its report, its keys and its status to
# FILES and this script reads them with redirects, so there is no pipeline whose exit code could be
# read from the wrong end. Every count is tested on its RAW value.
# =================================================================================================

set -u

IDC_PROG="coord-identifier-census.sh"
IDC_DIR="$(cd -- "$(dirname -- "$0")" && pwd)"
IDC_SELF="$IDC_DIR/$(basename -- "$0")"
IDC_PATTERNS="${IDC_PATTERNS:-$IDC_DIR/coord-identifier-patterns.txt}"
IDC_HASHES="${IDC_HASHES:-$IDC_DIR/coord-identifier-hashes.txt}"
IDC_UNMASK=0
IDC_SHORT="${IDC_SHORT:-0}"   # self-test forcing hook only; see ipv4Extent. Never set in normal use.
IDC_TMP=""

idc_cleanup() {
    if [ -n "$IDC_TMP" ] && [ -d "$IDC_TMP" ]; then rm -rf -- "$IDC_TMP"; fi
}
trap idc_cleanup EXIT INT TERM

IDC_TMP="$(mktemp -d 2>/dev/null)"
if [ -z "$IDC_TMP" ] || [ ! -d "$IDC_TMP" ]; then
    echo "REFUSED(2): $IDC_PROG could not create a temp directory -- it cannot measure, so it does not pass"
    exit 2
fi
chmod 700 -- "$IDC_TMP" 2>/dev/null

idc_misuse() {
    echo "REFUSED(2): $1"
    echo "usage: $IDC_PROG [--unmask] entry <file> | subject <string> | tree <file> <baseline-sha> | selftest"
    exit 2
}

# -------------------------------------------------------------------------------------------------
# RUN-TIME TOKEN SET. Read from the local box, written to a 600 temp file, never printed, never
# committed. The summary reports SOURCE COUNTS and HASH MATCHES, never a value.
# -------------------------------------------------------------------------------------------------
IDC_TOKFILE="$IDC_TMP/tok"
IDC_TOKSUMMARY=""
IDC_TOKFILE_PRESENT="no"
IDC_HASHSUMMARY=""

idc_resolve_tokenfile() {
    # The never-push token file. Located, never printed.
    if [ -n "${IDC_TOKEN_FILE:-}" ] && [ -f "${IDC_TOKEN_FILE:-}" ]; then echo "$IDC_TOKEN_FILE"; return 0; fi
    if [ -f "$IDC_DIR/coord-identifier-tokens.local" ]; then echo "$IDC_DIR/coord-identifier-tokens.local"; return 0; fi
    if [ -n "${HOME:-}" ] && [ -f "${HOME:-}/.claude/coord-identifier-tokens" ]; then echo "$HOME/.claude/coord-identifier-tokens"; return 0; fi
    echo ""
}

# Hash one literal exactly as the Go guard does: SHA-256 of the LOWER-CASED token, hex, salt-free.
idc_hash() {
    local low="" out=""
    low="$(printf '%s' "$1" | tr 'A-Z' 'a-z')"
    out="$(printf '%s' "$low" | sha256sum 2>/dev/null)"
    printf '%s' "${out%% *}"
}

IDC_HASH_HITS=0
idc_hash_known() {
    # 0 if the literal's (len, hash) pair is a row in the shared hash list.
    local lit="$1" h="" len="" line="" f1="" f2=""
    [ -f "$IDC_HASHES" ] || return 1
    len="${#lit}"
    h="$(idc_hash "$lit")"
    [ -n "$h" ] || return 1
    while IFS=$'\t' read -r f1 f2 _rest || [ -n "$f1" ]; do
        f1="${f1%$'\r'}"; f2="${f2%$'\r'}"
        case "$f1" in '#'*) continue ;; '') continue ;; esac
        if [ "$f1" = "$len" ] && [ "$f2" = "$h" ]; then return 0; fi
    done < "$IDC_HASHES"
    return 1
}

# A DERIVED token is a GUESS about what this box is called. On a container it is an ordinary word --
# `root`, `user`, `ubuntu` -- and C2 measured 4348 pre-existing hits from RUNTIME_ACCOUNT and
# RUNTIME_OWNERNAME on its box from exactly that. An arm that fires on an English word is not a
# security arm, it is a denial of service against its own operator.
#
# So a derived token is admitted only if it clears THREE bars, and the third is the real one:
#   length >= 5          a four-character derivation is a word more often than a name
#   not stop-listed      the container and CI account names, named rather than inferred
#   IN THE DENIED SET    its hash is a row in coord-identifier-hashes.txt, or the literal is in the
#                        local never-push token file. THIS is the discriminator: the denied set is
#                        the authority on which names are forbidden, and a derivation that is not in
#                        it is a name this fleet never denied.
# Otherwise the arm is INERT and says so by name. An inert arm is reported, never silent -- a zero
# that nobody can tell from an arm that was never wired is the shape this whole instrument exists to
# avoid.
#
# The TOKENFILE source is exempt from the third bar: those literals ARE the local denied set.
IDC_STOPLIST=" root user users admin home ubuntu debian guest default runner agent claude coord coordinator "
IDC_INERT=""

idc_token_in_file() {
    local lit="$1" tf="$2" line=""
    [ -n "$tf" ] || return 1
    [ -f "$tf" ] || return 1
    while IFS= read -r line || [ -n "$line" ]; do
        line="${line%$'\r'}"
        case "$line" in '#'*) continue ;; '') continue ;; esac
        if [ "$line" = "$lit" ]; then return 0; fi
    done < "$tf"
    return 1
}

idc_add_token() {
    # $1 = source arm name, $2 = literal, $3 = minimum length
    local lit="$2" low=""
    if [ -z "$lit" ]; then return 1; fi
    if [ "${#lit}" -lt "$3" ]; then return 1; fi
    if idc_hash_known "$lit"; then IDC_HASH_HITS=$((IDC_HASH_HITS + 1)); fi
    case "$1" in
        TOKENFILE) : ;;                     # the local denied set itself; the bars below do not apply
        *)
            low="$(printf '%s' "$lit" | tr 'A-Z' 'a-z')"
            if [ "${#lit}" -lt 5 ]; then
                IDC_INERT="$IDC_INERT  $1: derived token under 5 characters, arm inert\n"; return 1
            fi
            case "$IDC_STOPLIST" in
                *" $low "*)
                    IDC_INERT="$IDC_INERT  $1: derived token is a stop-listed generic account name, arm inert\n"; return 1 ;;
            esac
            if ! idc_hash_known "$lit" && ! idc_token_in_file "$lit" "$IDC_TF_PATH"; then
                IDC_INERT="$IDC_INERT  $1: token not in the denied set, arm inert\n"; return 1
            fi
            ;;
    esac
    printf '%s\t%s\n' "$1" "$lit" >> "$IDC_TOKFILE"
    return 0
}

idc_build_tokens() {
    : > "$IDC_TOKFILE"
    chmod 600 -- "$IDC_TOKFILE" 2>/dev/null
    IDC_HASH_HITS=0
    IDC_TOKFILE_PRESENT="no"
    IDC_INERT=""
    IDC_TF_PATH="$(idc_resolve_tokenfile)"
    local nTF=0 nAC=0 nMA=0 nOW=0 nRows=0 skipped="" tf="" line="" nm="" piece=""

    nRows=0
    if [ -f "$IDC_HASHES" ]; then
        while IFS= read -r line || [ -n "$line" ]; do
            case "$line" in '#'*) continue ;; '') continue ;; esac
            case "$line" in *"	"*) nRows=$((nRows + 1)) ;; esac
        done < "$IDC_HASHES"
    fi

    if [ -n "${IDC_TEST_TOKENS:-}" ]; then
        # SELF-TEST OVERRIDE: synthetic tokens, so a control never depends on this box's real name
        # and never coincides with an arm it was not meant to prove.
        for piece in $IDC_TEST_TOKENS; do
            if idc_add_token "TOKENFILE" "$piece" 4; then nTF=$((nTF + 1)); fi
        done
        IDC_TOKFILE_PRESENT="synthetic"
        IDC_TOKSUMMARY="TOKENFILE=$nTF(synthetic) RUNTIME_ACCOUNT=0 RUNTIME_MACHINE=0 RUNTIME_OWNERNAME=0"
        IDC_HASHSUMMARY="hashes=$nRows matched=0 (synthetic token set)"
        return 0
    fi

    tf="$IDC_TF_PATH"
    if [ -n "$tf" ]; then
        IDC_TOKFILE_PRESENT="yes"
        while IFS= read -r line || [ -n "$line" ]; do
            line="${line%$'\r'}"
            case "$line" in '#'*) continue ;; '') continue ;; esac
            if idc_add_token "TOKENFILE" "$line" 4; then nTF=$((nTF + 1)); fi
        done < "$tf"
    fi

    # account: basename $HOME, then $USERNAME, then $USER. Floor 4 -- a shorter derivation is
    # ABORTED and NAMED, never installed as a two-character detector.
    nm=""
    if [ -n "${IDC_TEST_ACCOUNT:-}" ]; then nm="$IDC_TEST_ACCOUNT"     # self-test only
    elif [ -n "${HOME:-}" ]; then nm="$(basename -- "$HOME")"; fi
    if [ -z "$nm" ]; then nm="${USERNAME:-}"; fi
    if [ -z "$nm" ]; then nm="${USER:-}"; fi
    if [ -n "$nm" ] && [ "${#nm}" -ge 4 ]; then
        if idc_add_token "RUNTIME_ACCOUNT" "$nm" 4; then nAC=$((nAC + 1)); fi
    else
        skipped="$skipped RUNTIME_ACCOUNT(derivation empty or under 4 chars)"
    fi
    if [ -z "${IDC_TEST_ACCOUNT:-}" ] && [ -n "${USERNAME:-}" ] && [ "${USERNAME:-}" != "$nm" ]; then
        if idc_add_token "RUNTIME_ACCOUNT" "$USERNAME" 4; then nAC=$((nAC + 1)); fi
    fi

    # machine: $COMPUTERNAME, then hostname. Same floor.
    nm="${COMPUTERNAME:-}"
    if [ -z "$nm" ]; then nm="$(hostname 2>/dev/null)"; fi
    if [ -n "$nm" ] && [ "${#nm}" -ge 4 ]; then
        if idc_add_token "RUNTIME_MACHINE" "$nm" 4; then nMA=$((nMA + 1)); fi
    else
        skipped="$skipped RUNTIME_MACHINE(derivation empty or under 4 chars)"
    fi

    # owner name in ordinary prose: git user.name split on non-alphabetics, pieces of 3+ kept.
    nm="$(git config --get user.name 2>/dev/null)"
    if [ -n "$nm" ]; then
        nm="$(printf '%s' "$nm" | tr -c 'A-Za-z' ' ')"
        for piece in $nm; do
            if idc_add_token "RUNTIME_OWNERNAME" "$piece" 3; then nOW=$((nOW + 1)); fi
        done
    fi
    if [ "$nOW" -eq 0 ]; then skipped="$skipped RUNTIME_OWNERNAME(git user.name empty or no piece of 3+ chars)"; fi

    IDC_TOKSUMMARY="TOKENFILE=$nTF RUNTIME_ACCOUNT=$nAC RUNTIME_MACHINE=$nMA RUNTIME_OWNERNAME=$nOW"
    if [ -n "$skipped" ]; then IDC_TOKSUMMARY="$IDC_TOKSUMMARY  SKIPPED:$skipped"; fi
    IDC_HASHSUMMARY="hashes=$nRows matched=$IDC_HASH_HITS unmatched=$((nRows - IDC_HASH_HITS))"
    return 0
}

# -------------------------------------------------------------------------------------------------
# The awk program. Written to a temp file from a QUOTED heredoc, so nothing in it is interpolated,
# expanded or collapsed on the way through the shell.
# -------------------------------------------------------------------------------------------------
IDC_AWK="$IDC_TMP/census.awk"
cat > "$IDC_AWK" <<'IDCAWKEOF'
function rtrim(s) { sub(/[ \t]+$/, "", s); return s }
function ltrim(s) { sub(/^[ \t]+/, "", s); return s }

function pathSeg(mt,   t, i) {
    t = mt; gsub(/\\/, "/", t)
    i = length(t)
    while (i > 0 && substr(t, i, 1) != "/") i--
    return substr(t, i + 1)
}
function uncHost(mt,   t, i, rest, j) {
    t = mt; gsub(/\\/, "/", t)
    i = index(t, "//")
    if (i == 0) return ""
    rest = substr(t, i + 2)
    j = index(rest, "/")
    if (j > 0) rest = substr(rest, 1, j - 1)
    return rest
}
function hostVal(mt,   i, j, v) {
    i = index(mt, "="); j = index(mt, ":")
    if (i == 0 || (j > 0 && j < i)) i = j
    if (i == 0) return ""
    v = substr(mt, i + 1); sub(/^[ \t]+/, "", v)
    return v
}
# Anchored-whole membership in a CONTEXT arm used as an admit set. Per OCCURRENCE, on the decision
# token -- never on the line.
function admitted(setName, tok) {
    if (!(setName in RE)) return 0
    return (tolower(tok) ~ ("^(" RE[setName] ")$"))
}
# FNV-1a, 32-bit, over the lower-cased value. A DELTA KEY and a DISTINGUISHER, never a disclosure:
# it lets tree mode tell "the hit the baseline already had" from "a hit this post ADDED", and lets a
# reader tell two masked hits apart, without the instrument printing a value.
function fnv(s,   i, h, c) {
    s = tolower(s); h = 2166136261
    for (i = 1; i <= length(s); i++) {
        c = index(CHARS, substr(s, i, 1))
        h = xor32(h, c)
        h = (h * 16777619) % 4294967296
    }
    return sprintf("%08x", h)
}
function xor32(a, b,   i, r, p, x, y) {
    r = 0; p = 1
    for (i = 0; i < 32; i++) {
        x = a % 2; y = b % 2
        if (x != y) r = r + p
        a = int(a / 2); b = int(b / 2); p = p * 2
        if (a == 0 && b == 0) break
    }
    return r
}
# Literal, case-insensitive replace-all. Never gsub with a literal built into a regex: a name can
# carry '.' and '-', and '.' in a dynamic regex matches anything.
function replAll(s, find, repl,   out, i) {
    if (find == "") return s
    out = ""
    while (1) {
        i = index(tolower(s), tolower(find))
        if (i == 0) break
        out = out substr(s, 1, i - 1) repl
        s = substr(s, i + length(find))
    }
    return out s
}
# COORD's line rule, reproduced: LONGEST LITERAL FIRST, then the three shape masks, then 160 chars.
# Masking a contained literal ahead of the literal that contains it leaves the container's suffix
# standing, which discloses exactly the infrastructure detail the order forbids. Only --unmask
# reaches this; the default report prints no line at all.
function maskLine(line,   m, i) {
    m = line
    for (i = 1; i <= nT; i++) m = replAll(m, TSORT[i], "<*REDACTED-" length(TSORT[i]) "*>")
    m = shapeMask(m, "users")
    m = shapeMask(m, "home")
    m = shapeMask(m, "unc")
    if (length(m) > 160) m = substr(m, 1, 160) " ..."
    return m
}
function shapeMask(s, kind,   out, rest, re, i, pre, seg, c, j) {
    if (kind == "users") re = "users[\\\\/]+"
    else if (kind == "home") re = "/home/"
    else re = "[\\\\][\\\\]"
    out = ""; rest = s
    while (match(tolower(rest), re)) {
        pre = substr(rest, 1, RSTART + RLENGTH - 1)
        rest = substr(rest, RSTART + RLENGTH)
        j = 0
        while (j < length(rest)) {
            c = substr(rest, j + 1, 1)
            if (c ~ /[\\\/ \t"'`,;:)\]}>*|]/) break
            j++
        }
        seg = substr(rest, 1, j)
        rest = substr(rest, j + 1)
        if (length(seg) >= 2) out = out pre "<*REDACTED*>"
        else out = out pre seg
    }
    return out rest
}
function maskIpv4(q,   p) { p = index(q, "."); if (p < 2) return "x.x.x.x"; return substr(q, 1, p - 1) ".x.x.x" }
function maskValue(arm, v) {
    if (UNMASK == 1) return v
    if (arm == "ipv4") return maskIpv4(v)
    return "<*REDACTED-" length(v) "*>"
}

function record(arm, pass, lineno, value, line,   shown) {
    HITS[arm]++
    nH++
    shown = maskValue(arm, value)
    HL[nH] = sprintf("    %-18s pass%-2d line %-7d %-22s fp=%s", arm, pass, lineno, shown, fnv(value))
    if (UNMASK == 1) HL[nH] = HL[nH] "\n        line: " maskLine(line)
    print arm "\t" fnv(value) "\t1" > KEYS
}

# ---- structural arms -----------------------------------------------------------------------------
function scanArm(arm, lineno, text, lo, pass, joinAt,   pos, s, e, mt, tok, ok) {
    pos = 0
    while (1) {
        if (match(substr(lo, pos + 1), RE[arm]) == 0) break
        s = pos + RSTART; e = s + RLENGTH - 1
        if (RLENGTH < 1) break
        pos = e
        if (pass == 2 && !(s <= joinAt && e > joinAt)) continue
        OCC[arm]++
        if (MD[arm] != "refuse") continue
        mt = substr(text, s, RLENGTH)
        ok = 1
        if (arm ~ /^profile_/ || arm ~ /^home_/) {
            tok = pathSeg(mt)
            if (admitted("profile_placeholder", tok)) { EXC[arm "\t" "placeholder-segment"]++; ok = 0 }
        } else if (arm ~ /^unc_/) {
            tok = uncHost(mt)
            if (tok == "") { EXC[arm "\t" "no-host"]++; ok = 0 }
            else if (admitted("nickname_host", tok)) { EXC[arm "\t" "nickname-host"]++; ok = 0 }
        } else if (arm ~ /^host_/) {
            tok = hostVal(mt)
            # A nickname-PREFIXED value is admitted here and NOWHERE ELSE: the measured shape on the
            # live surface is a fleet host named by its prescribed nickname plus a qualifier, and
            # refusing that teaches the next writer to delete exactly the spelling the order asks
            # for. Per ARM, per OCCURRENCE, and the profile and unc arms do not get it.
            if (tok == "") { EXC[arm "\t" "no-value"]++; ok = 0 }
            else if (tolower(tok) ~ ("^(" RE["nickname_fleet"] ")([._-][a-z0-9._-]*)?$")) { EXC[arm "\t" "nickname-host"]++; ok = 0 }
        } else {
            tok = mt
        }
        if (ok) record(arm, pass, lineno, tok, text)
    }
}

# ---- the IPv4 arm --------------------------------------------------------------------------------
# STRICT (entry/subject): no CONTEXT exclusion of any kind; the release_literal ADMIT SET (rule 5),
# read on the quad's own shape, is the one exclusion it takes. DELTA (tree): rules 1-4, then rule 5.
# ipv4ParseAt walks a four-octet quad BY HAND from position p and sets IPV4E to the position of its
# last digit. It returns 0 unless all four octets are there, 0-255, unpadded, and not followed by a
# fifth digit -- the same acceptance the ipv4 ERE has, derived without the ERE.
function ipv4ParseAt(lo, p,   i, j, c, n, e) {
    e = p - 1
    for (i = 1; i <= 4; i++) {
        if (i > 1) {
            if (substr(lo, e + 1, 1) != ".") return 0
            e = e + 1
        }
        j = e + 1; n = 0
        while (j <= length(lo) && substr(lo, j, 1) ~ /[0-9]/ && n < 3) { j++; n++ }
        if (n == 0) return 0
        if (n > 1 && substr(lo, e + 1, 1) == "0") return 0   # a padded octet is not one, as the ERE has it
        c = substr(lo, e + 1, n) + 0
        if (c > 255) return 0
        e = j - 1
    }
    if (substr(lo, e + 1, 1) ~ /[0-9]/) return 0             # a fifth digit means this was never an octet
    IPV4E = e
    return 1
}
# ipv4Extent derives the candidate's TRUE start and end from RSTART alone, setting IPV4S/IPV4E.
#
# ⚠ RLENGTH IS NOT USABLE HERE. mawk 1.3.4's match() is not leftmost-longest: on a four-octet quad it
# returns RLENGTH=7 -- three octets' worth -- so `e = RSTART + RLENGTH` lands MID-QUAD, and every rule
# that reads from the quad's end (the token run, the 32-character after-window) is then computed from
# the wrong place. Measured on C1's box: the version-context exclusion never fired and honest version
# quads in prose were REFUSED, self-test 60/62 under mawk while gawk read 62/62. A regex engine's
# submatch extent is an ASSUMPTION; the characters on the line are the measurement.
#
# So: scan BACK over [0-9.] in case the engine's start is itself short, then parse forward from each
# candidate position up to RSTART -- a quad must begin at or before the position the engine matched.
# The three whitespace-delimited words rule 4 is allowed to read: the quad's OWN word, the one
# immediately before it, and the one immediately after. A WINDOW is a laundering surface -- C2's A/B
# measured a routable-shaped quad refused when alone and CLEAN when any version word sat loose in the
# same 56/32 characters. Adjacency cannot be arranged by a sentence that has nothing to do with the
# address.
function wordOwn(lo, s, e,   b, t, L) {
    L = length(lo)
    b = s; while (b > 1 && substr(lo, b - 1, 1) !~ /[ \t]/) b--
    t = e; while (t < L && substr(lo, t + 1, 1) !~ /[ \t]/) t++
    return substr(lo, b, t - b + 1)
}
function wordBefore(lo, s,   i, e2, b) {
    i = s; while (i > 1 && substr(lo, i - 1, 1) !~ /[ \t]/) i--
    e2 = i - 1
    while (e2 > 0 && substr(lo, e2, 1) ~ /[ \t]/) e2--
    if (e2 < 1) return ""
    b = e2; while (b > 1 && substr(lo, b - 1, 1) !~ /[ \t]/) b--
    return substr(lo, b, e2 - b + 1)
}
function wordAfter(lo, e,   i, b, e2, L) {
    L = length(lo)
    i = e; while (i < L && substr(lo, i + 1, 1) !~ /[ \t]/) i++
    b = i + 1
    while (b <= L && substr(lo, b, 1) ~ /[ \t]/) b++
    if (b > L) return ""
    e2 = b; while (e2 < L && substr(lo, e2 + 1, 1) !~ /[ \t]/) e2++
    return substr(lo, b, e2 - b + 1)
}
function ipv4Extent(lo, rstart,   s) {
    s = rstart
    while (s > 1 && substr(lo, s - 1, 1) ~ /[0-9.]/) s--
    while (s <= rstart) {
        if (substr(lo, s, 1) ~ /[0-9]/ && ipv4ParseAt(lo, s)) { IPV4S = s; return 1 }
        s++
    }
    return 0
}
function scanIpv4(lineno, text, lo, pass, joinAt,   pos, s, e, quad, lq, k, b, cand, rs, rr, run, own, before, after, rstart, lastEnd) {
    pos = 0; lastEnd = 0
    while (1) {
        if (match(substr(lo, pos + 1), RE["ipv4"]) == 0) break
        rstart = pos + RSTART
        # SHORT is the self-test's forcing hook: it perturbs the engine's reported start FORWARD, which
        # is a strictly harder version of what a short match does, so the extent derivation is proven
        # on gawk too and cannot regress silently back onto RLENGTH.
        if (SHORT > 0 && rstart + SHORT <= length(lo)) rstart = rstart + SHORT
        if (ipv4Extent(lo, rstart) == 0) { pos = rstart; continue }
        s = IPV4S; e = IPV4E
        pos = (e > rstart) ? e : rstart     # rstart > old pos, so this always advances
        if (e <= lastEnd) continue          # the back-scan re-found a span already decided
        lastEnd = e
        if (pass == 2 && !(s <= joinAt && e > joinAt)) continue
        OCC["ipv4"]++
        quad = substr(text, s, e - s + 1); lq = tolower(quad)

        if (STRICT == 0) {
            # RULE 1 -- C2's prefix exemption, read against the reconstructed prefix-plus-quad.
            k = s
            if (k > 1 && substr(lo, k - 1, 1) == "=") k = k - 1
            while (k > 1 && substr(lo, k - 1, 1) ~ /[a-z_]/) k--
            b = (k > 1) ? substr(lo, k - 1, 1) : ""
            cand = b substr(lo, k, s - k) lq
            if (cand ~ RE["ipv4_prefix_ex"]) { EXC["ipv4\tprefix-ex"]++; continue }

            # RULE 2 -- token run. The maximal identifier run around the quad, less one trailing dot.
            rs = s; while (rs > 1 && substr(lo, rs - 1, 1) ~ /[a-z0-9._-]/) rs--
            rr = e; while (rr < length(lo) && substr(lo, rr + 1, 1) ~ /[a-z0-9._-]/) rr++
            run = substr(lo, rs, rr - rs + 1); sub(/\.$/, "", run)
            if (run != lq) { EXC["ipv4\ttoken-run"]++; continue }

            # RULE 3 -- declared documentation constants.
            if (lq ~ ("^(" RE["ipv4_doc"] ")$")) { EXC["ipv4\tdoc-constant"]++; OCC["ipv4_doc"]++; continue }

            # RULE 4 -- version context by ADJACENCY, never by a window. Three words only: the quad's
            # own whitespace-delimited word, the one immediately before, the one immediately after.
            own = wordOwn(lo, s, e); before = wordBefore(lo, s); after = wordAfter(lo, e)
            if (own ~ RE["ipv4_vercontext"] || before ~ RE["ipv4_vercontext"] || after ~ RE["ipv4_vercontext"]) { EXC["ipv4\tversion-context"]++; continue }
        }

        # RULE 5 -- the Go RELEASE LITERAL, and the ONE exclusion the STRICT reading takes. It is a
        # per-arm ADMIT SET read on the DECISION TOKEN -- the quad itself, anchored whole, per
        # OCCURRENCE -- and on nothing around it. That is what separates it from rules 1-4: a
        # context rule can be arranged by the sentence a lane writes, and a SHAPE cannot, so this
        # one is safe in the mode where the others are refused. It is consulted LAST in delta mode
        # on purpose: every occurrence rules 1-4 already dispose of keeps ITS OWN reason in the
        # EXCLUSIONS block, so adding a rule cannot silently re-attribute what the others were
        # measured on. Per ARM: no other arm consults this set, and none of them gains an admit.
        if (admitted("release_literal", lq)) { EXC["ipv4\trelease-literal"]++; continue }

        record("ipv4", pass, lineno, quad, text)
    }
}

# ---- the run-time token arms ---------------------------------------------------------------------
# Tokenised exactly as the Go guard tokenises: maximal [a-z0-9._-] runs, plus each dot/hyphen/
# underscore component. That is what makes a machine name and the account name inside it both match,
# and what makes a path ENDING in the token fire -- the shape a both-sides separator rule misses.
function checkTok(t, lineno, pass, text,   i) {
    if (t == "") return
    for (i = 1; i <= nT; i++) {
        if (t == TVAL[i]) {
            OCC[TSRC[i]]++
            record(TSRC[i], pass, lineno, TVAL[i], text)
            SEEN[lineno "\t" i] = 1
            if (pass == 2) SEEN[(lineno - 1) "\t" i] = 1
        }
    }
}
function scanTokens(lineno, text, lo, pass, joinAt,   i, L, s, e, c, run, nc, k) {
    L = length(lo); i = 1
    while (i <= L) {
        c = substr(lo, i, 1)
        if (c ~ /[a-z0-9._-]/) {
            s = i
            while (i <= L && substr(lo, i, 1) ~ /[a-z0-9._-]/) i++
            e = i - 1
            if (!(pass == 2 && !(s <= joinAt && e > joinAt))) {
                run = substr(lo, s, e - s + 1)
                checkTok(run, lineno, pass, text)
                nc = split(run, CMP, /[._-]/)
                if (nc > 1) for (k = 1; k <= nc; k++) checkTok(CMP[k], lineno, pass, text)
            }
        } else i++
    }
}
# PASS 3 -- token arms only, over an alphanumerics-only reduction. Tokens of 4+ characters only: the
# reduction has no boundaries left, so a 3-character detector here would fire on ordinary prose.
function scanTokensReduced(lineno, red, keyline, text,   i, t) {
    for (i = 1; i <= nT; i++) {
        t = TVAL[i]
        if (length(t) < 4) continue
        if ((keyline "\t" i) in SEEN) continue
        gsub(/[^a-z0-9]/, "", t)
        if (t == "") continue
        if (index(red, t) > 0) {
            OCC[TSRC[i]]++
            record(TSRC[i], 3, lineno, TVAL[i], text)
            SEEN[keyline "\t" i] = 1
        }
    }
}
# Length-preserving blank, so blanking the public URL cannot shift a PASS-2 join offset.
function blankSpans(s, re,   out, rest) {
    out = ""; rest = s
    while (match(rest, re)) {
        out = out substr(rest, 1, RSTART - 1) sprintf("%*s", RLENGTH, "")
        rest = substr(rest, RSTART + RLENGTH)
    }
    return out rest
}

function scanAll(lineno, text, pass, joinAt,   a, arm, lo, blanked, red) {
    lo = tolower(text)
    for (a = 1; a <= nA; a++) {
        arm = ARM[a]
        if (RE[arm] == "@RUNTIME@") continue
        if (arm == "ipv4") { scanIpv4(lineno, text, lo, pass, joinAt); continue }
        # An arm marked [CONSULTED-ONLY] in the definition is an ADMIT SET or a decision input, not
        # a pattern with a standalone occurrence count. Scanning one directly is meaningless: the
        # placeholder set matches ANY SINGLE CHARACTER by its own "a single character is a stand-in,
        # not an account" rule, and read 32 occurrences on a clean line before this was added.
        if (CONSULTED[arm] == 1) continue
        scanArm(arm, lineno, text, lo, pass, joinAt)
    }
    if (nT > 0) {
        # The public URL is PUBLISHED ATTRIBUTION, not infrastructure: blanked before the token arms
        # run, as a SPAN and never as a line, so a denied token elsewhere on that line still fires.
        blanked = lo
        if ("public_url" in RE) blanked = blankSpans(blanked, RE["public_url"])
        scanTokens(lineno, text, blanked, pass, joinAt)
        red = blanked; gsub(/[^a-z0-9]/, "", red)
        scanTokensReduced(lineno, red, lineno, text)
    }
}

BEGIN {
    CHARS = " !\"#$%&()*+,-./0123456789:;<=>?@abcdefghijklmnopqrstuvwxyz[\\]^_`{|}~" "'"
    nA = 0; nT = 0; nH = 0
    if (PATFILE == "" || KEYS == "" || REPORT == "" || STATUS == "") { print "awk: missing -v" > "/dev/stderr"; exit 9 }
    while ((getline L < PATFILE) > 0) {
        # The definition is a .txt with no eol pin, so a Windows checkout materialises CRLF while a
        # Linux one materialises LF. Strip the CR here rather than relying on either: verify at the
        # layer the instrument actually reads, never at the one you assume it reads.
        sub(/\r$/, "", L)
        if (L ~ /^#/) continue
        if (L ~ /^[ \t]*$/) continue
        n = split(L, F, "\t")
        if (n < 3) continue
        nA++; ARM[nA] = F[1]; RE[F[1]] = F[2]; MD[F[1]] = F[3]; OCC[F[1]] = 0; HITS[F[1]] = 0
        CONSULTED[F[1]] = ((n >= 4) && (F[4] ~ /^\[CONSULTED-ONLY\]/)) ? 1 : 0
    }
    close(PATFILE)
    if (nA < 1) { print "awk: patterns file yielded 0 arms" > "/dev/stderr"; exit 9 }
    if (TOKFILE != "") {
        while ((getline L < TOKFILE) > 0) {
            sub(/\r$/, "", L)
            if (L ~ /^[ \t]*$/) continue
            n = split(L, F, "\t")
            if (n < 2) continue
            nT++; TSRC[nT] = F[1]; TVAL[nT] = tolower(F[2])
        }
        close(TOKFILE)
    }
    # LONGEST FIRST, once, for maskLine. A contained literal must never be masked ahead of the
    # literal that contains it.
    for (i = 1; i <= nT; i++) TSORT[i] = TVAL[i]
    for (i = 1; i < nT; i++)
        for (j = i + 1; j <= nT; j++)
            if (length(TSORT[j]) > length(TSORT[i])) { t = TSORT[i]; TSORT[i] = TSORT[j]; TSORT[j] = t }
}

{
    scanAll(NR, $0, 1, 0)
    if (NR > 1) {
        left = rtrim(prev); right = ltrim($0)
        if (length(left) > 0 && length(right) > 0) {
            joined = left right
            scanAll(NR, joined, 2, length(left))
            if (nT > 0) {
                jred = tolower(joined)
                if ("public_url" in RE) jred = blankSpans(jred, RE["public_url"])
                gsub(/[^a-z0-9]/, "", jred)
                scanTokensReduced(NR, jred, NR, joined)
            }
        }
    }
    prev = $0
}

END {
    printf "  DECLARED SET (arms=%d, strict=%d -- every arm prints, so a zero is still looking):\n", nA, STRICT > REPORT
    for (a = 1; a <= nA; a++) {
        arm = ARM[a]
        if (CONSULTED[arm] == 1)
            printf "    %-7s %-20s occ=(consulted by another arm)\n", MD[arm], arm > REPORT
        else if (MD[arm] == "refuse")
            printf "    %-7s %-20s occ=%-6d hits=%d\n", MD[arm], arm, OCC[arm], HITS[arm] > REPORT
        else
            printf "    %-7s %-20s occ=%-6d hits=-\n", MD[arm], arm, OCC[arm] > REPORT
    }
    ne = 0
    for (k in EXC) ne++
    if (ne > 0) {
        print "  EXCLUSIONS (arm / reason / count -- what the arms saw and decided against):" > REPORT
        for (k in EXC) {
            split(k, KP, "\t")
            printf "    %-18s %-18s %d\n", KP[1], KP[2], EXC[k] > REPORT
        }
    }
    if (nH > 0) {
        print "  HITS (arm, pass, line, MASKED, fingerprint -- never a value, never the line):" > REPORT
        for (i = 1; i <= nH; i++) print HL[i] > REPORT
    }
    print "arms=" nA > STATUS
    print "hits=" nH > STATUS
    # Machine-readable, so a consumer (and the self-test) asserts on ARMS rather than on printed
    # prose. Arm names and counts only -- never a value.
    for (a = 1; a <= nA; a++) if (HITS[ARM[a]] > 0) print "hitarm=" ARM[a] > STATUS
    for (k in EXC) { split(k, KP, "\t"); print "exc=" KP[1] "|" KP[2] "|" EXC[k] > STATUS }
    close(REPORT); close(KEYS); close(STATUS)
}
IDCAWKEOF

# -------------------------------------------------------------------------------------------------
IDC_HITS=""
IDC_ARMS=""

idc_count_arms_independently() {
    # A SECOND reader of the same file, so "awk read the patterns" is proven rather than assumed.
    local n=0 line=""
    while IFS= read -r line || [ -n "$line" ]; do
        case "$line" in '#'*) continue ;; esac
        case "$line" in '') continue ;; esac
        case "$line" in *"	"*) n=$((n + 1)) ;; esac
    done < "$IDC_PATTERNS"
    echo "$n"
}

idc_run_awk() {
    # $1 input, $2 keys, $3 report, $4 status, $5 strict
    awk -v PATFILE="$IDC_PATTERNS" -v TOKFILE="$IDC_TOKFILE" -v REPORT="$3" \
        -v KEYS="$2" -v STATUS="$4" -v STRICT="$5" -v UNMASK="$IDC_UNMASK" \
        -v SHORT="${IDC_SHORT:-0}" \
        -f "$IDC_AWK" -- "$1"
}

idc_census() {
    local input="$1" label="$2" keys="$3" strict="$4"
    local report="$IDC_TMP/report.out" status="$IDC_TMP/status.out" rc=0 k="" v="" expect=""

    if [ ! -f "$IDC_PATTERNS" ]; then
        echo "REFUSED(2): the patterns file is not beside this tool -- there is nothing to census with"
        exit 2
    fi
    if [ ! -r "$input" ]; then
        echo "REFUSED(2): $IDC_PROG cannot read the input for '$label' -- it cannot know, so it does not pass"
        exit 2
    fi

    : > "$report"; : > "$keys"; : > "$status"
    idc_run_awk "$input" "$keys" "$report" "$status" "$strict"
    rc=$?
    if [ "$rc" -ne 0 ]; then
        echo "REFUSED(2): the census awk exited $rc on '$label' -- an instrument failure is not a clean read"
        exit 2
    fi

    IDC_ARMS=""; IDC_HITS=""
    while IFS='=' read -r k v; do
        case "$k" in
            arms) IDC_ARMS="$v" ;;
            hits) IDC_HITS="$v" ;;
        esac
    done < "$status"

    if [ -z "$IDC_ARMS" ] || [ -z "$IDC_HITS" ]; then
        echo "REFUSED(2): the census produced no arm/hit count for '$label' -- an empty reading is not a clean one"
        exit 2
    fi
    case "$IDC_ARMS" in ''|0) echo "REFUSED(2): the census declared $IDC_ARMS arms"; exit 2 ;; esac
    expect="$(idc_count_arms_independently)"
    if [ "$IDC_ARMS" != "$expect" ]; then
        echo "REFUSED(2): the census read $IDC_ARMS arms where this script counts $expect in the same file -- the instrument and its definition disagree"
        exit 2
    fi

    echo "IDENTIFIER CENSUS -- $label"
    echo "  patterns: $(basename -- "$IDC_PATTERNS")   $IDC_HASHSUMMARY"
    echo "  token file: $IDC_TOKFILE_PRESENT   run-time arms: $IDC_TOKSUMMARY"
    if [ -n "${IDC_INERT:-}" ]; then printf '%b' "$IDC_INERT"; fi
    if [ "$IDC_UNMASK" = "1" ]; then echo "  *** --unmask IS ON. LOCAL CONSOLE ONLY. NOTHING BELOW MAY BE PASTED INTO A POST. ***"; fi
    cat -- "$report"
    echo "  hits=$IDC_HITS"
    return 0
}

# -------------------------------------------------------------------------------------------------
idc_mode_entry() {
    [ "$#" -eq 1 ] || idc_misuse "entry takes exactly one argument, the entry file"
    idc_build_tokens
    idc_census "$1" "entry $(basename -- "$1")" "$IDC_TMP/keys.entry" 1
    case "$IDC_HITS" in
        ''|0) echo "CLEAN: no identifier arm fired."; return 0 ;;
        *)    echo "REFUSED(1): $IDC_HITS hit(s). The arms, passes and line numbers are above; the values are not."; return 1 ;;
    esac
}

idc_mode_subject() {
    [ "$#" -eq 1 ] || idc_misuse "subject takes exactly one argument, the subject string"
    idc_build_tokens
    printf '%s\n' "$1" > "$IDC_TMP/subject"
    idc_census "$IDC_TMP/subject" "subject" "$IDC_TMP/keys.subject" 1
    case "$IDC_HITS" in
        ''|0) echo "CLEAN: no identifier arm fired."; return 0 ;;
        *)    echo "REFUSED(1): $IDC_HITS hit(s) in the COMMIT SUBJECT. A post censuses BOTH surfaces."; return 1 ;;
    esac
}

idc_mode_tree() {
    [ "$#" -eq 2 ] || idc_misuse "tree takes two arguments, <file> and <baseline-sha>"
    local file="$1" base="$2" cur="$IDC_TMP/cur" old="$IDC_TMP/old" curhits="" basehits="" added="" k="" v=""

    if ! git rev-parse --git-dir >/dev/null 2>&1; then
        idc_misuse "tree mode must run inside a git repository"
    fi
    if [ -f "$file" ]; then
        cp -- "$file" "$cur" || idc_misuse "could not read $file from the worktree"
    else
        git show "HEAD:$file" > "$cur" 2>/dev/null || idc_misuse "$file is neither on disk nor at HEAD"
    fi
    if ! git show "$base:$file" > "$old" 2>/dev/null; then
        echo "REFUSED(2): the baseline $base:$file could not be read -- an unreadable baseline is not 'no pre-existing hits'"
        exit 2
    fi

    idc_build_tokens
    echo "DELTA CENSUS -- $file"
    echo "  baseline: $base"
    echo "  ⚠ THIS IS A READING, NOT THE GATE. The pre-push gate is entry + subject (strict). The"
    echo "    baseline above MUST be the tip blob as fetched IMMEDIATELY BEFORE the append -- never a"
    echo "    lane's last-read sha. Anything landed by another lane in between is counted as ADDED by"
    echo "    this post, and a clean post is refused for someone else's entries."
    echo
    idc_census "$old" "BASELINE $base:$file" "$IDC_TMP/keys.base" 0
    basehits="$IDC_HITS"
    echo
    idc_census "$cur" "CURRENT $file" "$IDC_TMP/keys.cur" 0
    curhits="$IDC_HITS"
    echo

    awk -v BK="$IDC_TMP/keys.base" -v STATUSF="$IDC_TMP/delta.status" '
        BEGIN { while ((getline L < BK) > 0) { n = split(L, F, "\t"); if (n >= 3) B[F[1] "\t" F[2]] += F[3] } close(BK) }
        { n = split($0, F, "\t"); if (n < 3) next; C[F[1] "\t" F[2]] += F[3] }
        END {
            added = 0
            for (k in C) {
                split(k, KP, "\t")
                pre = (k in B) ? B[k] : 0
                PRE[KP[1]] += (pre < C[k]) ? pre : C[k]
                d = C[k] - pre
                if (d > 0) { added += d; ADD[KP[1]] += d }
            }
            print "  PRE-EXISTING ON THIS SURFACE (not this post to fix, and not silent):"
            any = 0
            for (a in PRE) if (PRE[a] > 0) { printf "    %-18s %d\n", a, PRE[a]; any = 1 }
            if (any == 0) print "    (none)"
            print "  ADDED BY WHAT IS BEING POSTED:"
            any = 0
            for (a in ADD) if (ADD[a] > 0) { printf "    %-18s %d\n", a, ADD[a]; any = 1 }
            if (any == 0) print "    (none)"
            print "added=" added > STATUSF
            close(STATUSF)
        }
    ' "$IDC_TMP/keys.cur" > "$IDC_TMP/delta.report"
    if [ "$?" -ne 0 ]; then
        echo "REFUSED(2): the delta awk failed -- an instrument failure is not a clean read"
        exit 2
    fi
    cat -- "$IDC_TMP/delta.report"

    while IFS='=' read -r k v; do
        case "$k" in added) added="$v" ;; esac
    done < "$IDC_TMP/delta.status"
    if [ -z "$added" ]; then
        echo "REFUSED(2): the delta produced no count -- an empty reading is not a clean one"
        exit 2
    fi
    echo "  baseline hits=$basehits  current hits=$curhits  added=$added"
    case "$added" in
        ''|0) echo "CLEAN: this surface adds no identifier. Pre-existing hits are reported above and are NOT a refusal."; return 0 ;;
        *)    echo "REFUSED(1): $added hit(s) ADDED to $file by what is being posted."; return 1 ;;
    esac
}

# =================================================================================================
# SELF-TEST. The triad: PLANTS, KNOWN NEGATIVES, DECLARED SET.
#
# Every plant is assembled through printf from synthetic parts, so this SOURCE reads as a template
# while the runtime string is real-looking; every plant is written to a temp file and NEVER echoed;
# and the output names the ARM and PASS/FAIL only.
#
# Each case declares the EXACT arm set it must fire, and the test asserts the fired set EQUALS it.
# That is stronger than "catchable by exactly one arm" and gives the same property: a control that
# two arms can catch cannot tell you either one works. Where a plant legitimately fires two arms, it
# says so and both are required.
#
# The admit sets are controlled in BOTH DIRECTIONS. An admit-only control reads GREEN on a dead arm,
# so for every nickname/placeholder case that must PASS there is a sibling that must REFUSE.
# =================================================================================================
IDC_ST_PASS=0
IDC_ST_FAIL=0

idc_st_fired() {
    # The REFUSE-arm hit set, read from the status file the census writes -- not from printed prose,
    # and not by eye. A context arm is not in it by construction: a context arm cannot refuse.
    local status="$1" out="" line=""
    while IFS= read -r line; do
        case "$line" in hitarm=*) out="$out ${line#hitarm=}" ;; esac
    done < "$status"
    printf '%s' "${out# }"
}

idc_st_exc() {
    # $1 name, $2 "arm|reason", $3 status file. Asserts the exclusion FIRED at least once -- the
    # admit direction of an admit set, which an admit-only control cannot distinguish from a dead arm.
    local name="$1" want="$2" status="$3" got="0" line=""
    while IFS= read -r line; do
        case "$line" in
            "exc=$want|"*) got="${line##*|}" ;;
        esac
    done < "$status"
    case "$got" in
        ''|0) printf '  FAIL  %-48s exclusion %s never fired\n' "$name" "$want"; IDC_ST_FAIL=$((IDC_ST_FAIL + 1)) ;;
        *)    printf '  PASS  %-48s exclusion %s fired %s\n' "$name" "$want" "$got"; IDC_ST_PASS=$((IDC_ST_PASS + 1)) ;;
    esac
}

idc_st_case() {
    # $1 name, $2 expected arm set, $3 file, $4 strict
    local name="$1" expect="$2" f="$3" strict="$4" got="" rc=0
    local report="$IDC_TMP/st.report" status="$IDC_TMP/st.status" keys="$IDC_TMP/st.keys"
    : > "$report"; : > "$status"; : > "$keys"
    idc_run_awk "$f" "$keys" "$report" "$status" "$strict"
    rc=$?
    if [ "$rc" -ne 0 ]; then
        printf '  FAIL  %-48s instrument exited %d\n' "$name" "$rc"
        IDC_ST_FAIL=$((IDC_ST_FAIL + 1)); return 0
    fi
    got="$(idc_st_fired "$status")"
    if [ "$got" = "$expect" ]; then
        printf '  PASS  %-48s arms{%s}\n' "$name" "$got"
        IDC_ST_PASS=$((IDC_ST_PASS + 1))
    else
        printf '  FAIL  %-48s expected arms{%s} got arms{%s}\n' "$name" "$expect" "$got"
        IDC_ST_FAIL=$((IDC_ST_FAIL + 1))
    fi
    return 0
}

idc_st_assert_absent() {
    # $1 name, $2 needle, $3 file. The needle is NEVER echoed -- only the count is.
    local name="$1" needle="$2" f="$3" c=""
    c="$(grep -c -F -- "$needle" "$f" 2>/dev/null)"
    case "$c" in
        ''|0) printf '  PASS  %-48s occurrences=0\n' "$name"; IDC_ST_PASS=$((IDC_ST_PASS + 1)) ;;
        *)    printf '  FAIL  %-48s occurrences=%s -- the output SPELLED it\n' "$name" "$c"; IDC_ST_FAIL=$((IDC_ST_FAIL + 1)) ;;
    esac
}
idc_st_assert_present() {
    local name="$1" needle="$2" f="$3" c=""
    c="$(grep -c -F -- "$needle" "$f" 2>/dev/null)"
    case "$c" in
        ''|0) printf '  FAIL  %-48s occurrences=0 -- the check itself is dead\n' "$name"; IDC_ST_FAIL=$((IDC_ST_FAIL + 1)) ;;
        *)    printf '  PASS  %-48s occurrences=%s\n' "$name" "$c"; IDC_ST_PASS=$((IDC_ST_PASS + 1)) ;;
    esac
}

idc_mode_selftest() {
    local d="$IDC_TMP/st" bs sl pc out="" rc=0 probe="" c=""
    # The per-case runner calls awk directly and does NOT go through idc_census, so a missing
    # definition file surfaces as "instrument exited 9" on every case instead of once, clearly. It
    # still fails CLOSED, which is right, but an unreadable red is a red nobody can act on -- it cost
    # two runs of a neuter control before the cause was read. One check, up front.
    if [ ! -f "$IDC_PATTERNS" ]; then
        echo "REFUSED(2): the patterns file is not beside this tool -- the self-test has nothing to test with"
        return 2
    fi
    mkdir -p -- "$d"; chmod 700 -- "$d" 2>/dev/null
    bs="$(printf '\134')"      # one backslash, built rather than written
    sl="/"
    pc="$(printf '%%')"

    IDC_TEST_TOKENS="zorbulax quennelbee zorbulaxqueen"
    export IDC_TEST_TOKENS
    idc_build_tokens

    echo "SELF-TEST -- coord-identifier-census.sh"
    echo "  patterns: $IDC_PATTERNS"
    echo "  token set: synthetic (3 literals, not printed)"
    echo
    echo "  A. PLANTS -- each MUST fire exactly the declared arm set"

    printf 'HOST=%s\n' "box7lab"                                           > "$d/p01"; idc_st_case "host assignment context"                  "host_ctx" "$d/p01" 1
    printf 'the address %d.%d.%d.%d answered\n' 192 168 4 20               > "$d/p02"; idc_st_case "bare private-range quad"                  "ipv4"     "$d/p02" 1
    printf 'HOST=%d.%d.%d.%d\n' 10 7 7 7                                   > "$d/p03"; idc_st_case "assignment context carrying a quad"       "host_ctx ipv4" "$d/p03" 1
    printf 'copied from %s%s%s%sshare%sx\n' "$bs" "$bs" "box7" "$bs" "$bs" > "$d/p04"; idc_st_case "backslash share path"                     "unc_backslash" "$d/p04" 1
    printf 'see %s%s%s%sshare%sx for the log\n' "$sl" "$sl" "box7" "$sl" "$sl" > "$d/p05"; idc_st_case "forward-slash share path"             "unc_slash" "$d/p05" 1
    printf 'built at C:%sUsers%s%s%sx\n' "$bs" "$bs" "sylvandeep" "$bs"    > "$d/p06"; idc_st_case "windows profile root"                     "profile_root" "$d/p06" 1
    printf 'built at %sc%sUsers%s%s%sx\n' "$sl" "$sl" "$sl" "sylvandeep" "$sl" > "$d/p07"; idc_st_case "msys profile root"                    "profile_root" "$d/p07" 1
    printf 'built at %shome%s%s%sx\n' "$sl" "$sl" "sylvandeep" "$sl"       > "$d/p08"; idc_st_case "linux home prefix"                        "home_unix" "$d/p08" 1
    printf 'built at %sUsers%s%s%sx\n' "$sl" "$sl" "sylvandeep" "$sl"      > "$d/p09"; idc_st_case "darwin home prefix"                       "profile_root" "$d/p09" 1

    # BOTH DIRECTIONS on the nickname admit set: admitted as a HOST, refused as a PROFILE SEGMENT.
    printf 'built at C:%sUsers%sR-LAPTOP%sx\n' "$bs" "$bs" "$bs"           > "$d/p10"; idc_st_case "nickname AS A PROFILE SEGMENT refuses"    "profile_root" "$d/p10" 1
    # PER OCCURRENCE, never per line: a nickname host and a real host on ONE line still refuses.
    printf 'from %s%sR-LAPTOP%sshare and %s%s%s%sshare\n' "$bs" "$bs" "$bs" "$bs" "$bs" "box7" "$bs" > "$d/p11"; idc_st_case "mixed line: nickname host + real host" "unc_backslash" "$d/p11" 1
    idc_st_exc "  and the nickname on that SAME LINE was admitted" "unc_backslash|nickname-host" "$IDC_TMP/st.status"
    # A PLACEHOLDER IS EXCLUDED AS A SEGMENT, NEVER AS A LINE.
    printf 'C:%sUsers%s<user>%sa and C:%sUsers%s%s%sb\n' "$bs" "$bs" "$bs" "$bs" "$bs" "sylvandeep" "$bs" > "$d/p12"; idc_st_case "placeholder segment does not clear the line" "profile_root" "$d/p12" 1
    idc_st_exc "  and the placeholder on that SAME LINE was admitted" "profile_root|placeholder-segment" "$IDC_TMP/st.status"

    # PASS 2 -- a token split across a line break, with an INDENTED continuation.
    printf 'owner column reads zorb\n    ulax here\n'                      > "$d/p13"; idc_st_case "token split across a line break (PASS 2)" "TOKENFILE" "$d/p13" 1
    # Go-guard tokenising: a token as a dot/hyphen/underscore COMPONENT of a larger run.
    printf 'row names x_%s_y and more\n' "quennelbee"                      > "$d/p14"; idc_st_case "token as an underscore component (PASS 1)" "TOKENFILE" "$d/p14" 1
    # PASS 3 -- a token broken by separators INSIDE a component, which tokenising cannot see.
    printf 'row names ab%s-%s2 here\n' "zorbul" "ax"                       > "$d/p15"; idc_st_case "token broken inside a component (PASS 3)"  "TOKENFILE" "$d/p15" 1
    # A path ENDING in the token -- the shape a both-sides separator rule misses.
    printf 'built at C:%sUsers%s%s\n' "$bs" "$bs" "zorbulax"               > "$d/p16"; idc_st_case "path ENDING in a denied token" "profile_root TOKENFILE" "$d/p16" 1
    # STRICT: the IPv4 arm takes NO CONTEXT exclusion in entry/subject mode. The quad planted here
    # is version-SHAPED but OFF the release shape (a first component of 2), because the release
    # shape itself is admitted in strict by rule 5 from this change forward -- left as it was, this
    # plant would have gone on reading green while proving the opposite of what its name says. The
    # release shape's own both-directions battery is A2 below; this case keeps its own question.
    printf 'the toolchain is go%d.%d.%d.%d here\n' 2 24 13 3               > "$d/p17"; idc_st_case "STRICT refuses a version quad off the release shape" "ipv4" "$d/p17" 1
    printf 'the loopback %d.%d.%d.%d appears\n' 127 0 0 1                  > "$d/p18"; idc_st_case "STRICT refuses a doc constant"            "ipv4" "$d/p18" 1

    echo
    echo "  A2. THE RELEASE-LITERAL ADMIT -- BOTH DIRECTIONS, IN THE GATE'S OWN MODE (STRICT)"
    echo "      (an admit-only battery reads GREEN on an arm that admits every quad, so each case"
    echo "       that must PASS has a sibling one digit off the shape that must still REFUSE, and"
    echo "       every pass asserts the RELEASE-LITERAL exclusion actually fired)"
    printf 'the hop landed go%d.%d.%d.%d on every lane\n' 1 24 13 3        > "$d/q01"; idc_st_case "STRICT admits a Go release literal"         "" "$d/q01" 1
    idc_st_exc "  and the RELEASE ADMIT is what admitted it"      "ipv4|release-literal" "$IDC_TMP/st.status"
    printf 'the package nuget-%d.%d.%d.%d is on the feed\n' 1 23 12 1      > "$d/q02"; idc_st_case "STRICT admits a package-prefixed release literal" "" "$d/q02" 1
    idc_st_exc "  and the RELEASE ADMIT is what admitted it"      "ipv4|release-literal" "$IDC_TMP/st.status"
    printf 'see docs%svalidation%s%d.%d.%d.%d%s for the roster\n' "$sl" "$sl" 1 24 13 0 "$sl" > "$d/q03"; idc_st_case "STRICT admits a release literal inside a path" "" "$d/q03" 1
    idc_st_exc "  and the RELEASE ADMIT is what admitted it"      "ipv4|release-literal" "$IDC_TMP/st.status"
    # THE REFUSE DIRECTION. The admit is bounded to ONE shape, so a private-LAN quad and a quad a
    # single component off the shape must both still be hits -- in STRICT mode, where nothing in the
    # sentence around them can help either way.
    printf 'the box answered on %d.%d.%d.%d last night\n' 10 0 0 1         > "$d/q04"; idc_st_case "a 10/8 quad is not the release shape"       "ipv4" "$d/q04" 1
    printf 'the box answered on %d.%d.%d.%d last night\n' 192 168 1 20     > "$d/q05"; idc_st_case "a private-range quad is not the release shape" "ipv4" "$d/q05" 1
    printf 'the build stamped %d.%d.%d.%d into the assembly\n' 1 3 4 5     > "$d/q06"; idc_st_case "second component off the shape still refuses" "ipv4" "$d/q06" 1
    printf 'the build stamped %d.%d.%d.%d into the assembly\n' 2 24 13 3   > "$d/q07"; idc_st_case "first component off the shape still refuses"  "ipv4" "$d/q07" 1
    # AND IN DELTA MODE TOO, by the release admit and not by rules 1-4: this quad carries no prefix,
    # its token run IS the quad, it is no doc constant, and neither neighbouring word is a context
    # word -- so under rules 1-4 alone it was a hit, and the reason printed is the discriminator.
    printf 'the page %d.%d.%d.%d is linked from the roster\n' 1 24 13 3    > "$d/q08"; idc_st_case "the admit is consulted in DELTA mode as well" "" "$d/q08" 0
    idc_st_exc "  and by the RELEASE ADMIT, not by rules 1-4"     "ipv4|release-literal" "$IDC_TMP/st.status"

    echo
    echo "  B. KNOWN NEGATIVES -- in DELTA mode, each MUST fire the arm set declared beside it"
    echo "     (mostly the empty set; the few that declare an arm are the REFUSE-direction siblings"
    echo "      of an admit rule, because an admit-only control reads GREEN on a dead arm)"
    printf 'older re-creations of nuget-%d.%d.%d.%d, their targets on origin.\n' 1 23 1 7 > "$d/n01"; idc_st_case "package-prefixed version quad" "" "$d/n01" 0
    idc_st_exc "  and it was excluded BY C2 PREFIX-EX"            "ipv4|prefix-ex" "$IDC_TMP/st.status"
    printf 'the pinned toolchain is go%d.%d.%d.%d on this box\n' 1 24 13 3 > "$d/n02"; idc_st_case "toolchain-prefixed version quad" "" "$d/n02" 0
    idc_st_exc "  and it was excluded BY THE TOKEN RUN"           "ipv4|token-run" "$IDC_TMP/st.status"
    printf 'linking the %d.%d.%d.%d validation page that a reader follows\n' 1 24 13 3 > "$d/n03"; idc_st_case "version quad, space-separated context" "" "$d/n03" 0
    idc_st_exc "  and it was excluded BY THE WINDOW (C2 gap)"     "ipv4|version-context" "$IDC_TMP/st.status"
    printf 'assembly Version=%d.%d.%d.%d inside the test host\n' 1 24 13 3 > "$d/n04"; idc_st_case "version quad after an assignment" "" "$d/n04" 0
    idc_st_exc "  and it was excluded BY C2 PREFIX-EX"            "ipv4|prefix-ex" "$IDC_TMP/st.status"
    # ADJACENCY, not a window: the nearest context word here is THREE words back, so this quad is no
    # longer excused. It was excluded by the 56-character window the first draft read, and that is
    # exactly the laundering surface C2's A/B found. The refusal is the intended cost.
    # The quad is OFF the release shape (first component 2) so that rule 5 cannot dispose of it:
    # with the release shape here, this case would still have read green -- for the wrong reason --
    # and the adjacency control it exists to be would have been dead without ever going red.
    printf -- '-> FileNotFoundException for internal/itoa %d.%d.%d.%d. The error\n' 2 24 13 3 > "$d/n05"; idc_st_case "version word THREE words back no longer excuses" "ipv4" "$d/n05" 0
    printf 'the loopback %d.%d.%d.%d is a documentation constant\n' 127 0 0 1 > "$d/n06"; idc_st_case "loopback constant" "" "$d/n06" 0
    idc_st_exc "  and it was excluded AS A DOC CONSTANT"          "ipv4|doc-constant" "$IDC_TMP/st.status"
    printf 'the unspecified %d.%d.%d.%d and broadcast %d.%d.%d.%d addresses\n' 0 0 0 0 255 255 255 255 > "$d/n07"; idc_st_case "unspecified and broadcast constants" "" "$d/n07" 0
    idc_st_exc "  and BOTH were excluded AS DOC CONSTANTS"        "ipv4|doc-constant" "$IDC_TMP/st.status"
    printf 'lanes R-LAPTOP G-LAPTOP i9 i7 C1 C2 reported in\n'             > "$d/n08"; idc_st_case "bare nicknames" "" "$d/n08" 0
    printf 'from %s%sR-LAPTOP%sshare only\n' "$bs" "$bs" "$bs"             > "$d/n09"; idc_st_case "nickname AS A NETWORK HOST is admitted" "" "$d/n09" 0
    idc_st_exc "  and the admit set is what admitted it"          "unc_backslash|nickname-host" "$IDC_TMP/st.status"
    printf 'HOST=C1 reported the leg\n'                                    > "$d/n09b"; idc_st_case "nickname AS AN ASSIGNED HOST is admitted" "" "$d/n09b" 0
    idc_st_exc "  and the WIDER fleet set is what admitted it"    "host_ctx|nickname-host" "$IDC_TMP/st.status"
    printf 'HOST=i9-runner took the leg\n'                                 > "$d/n09c"; idc_st_case "a nickname-PREFIXED assigned host is admitted" "" "$d/n09c" 0
    idc_st_exc "  and the prefix rule is what admitted it"        "host_ctx|nickname-host" "$IDC_TMP/st.status"
    # The narrowing that took host_ctx from 25 false hits to 0 on the live surface: a PROSE COLON is
    # not an assignment. Both directions -- prose admitted, the assignment shape still refused.
    printf 'the host: a fleet box, and the server = the one we use\n'      > "$d/n09d"; idc_st_case "a prose colon is not an assignment" "" "$d/n09d" 0
    printf 'hostname:%s in the pasted env dump\n' "box7lab"                > "$d/n09e"; idc_st_case "the unspaced assignment shape still refuses" "host_ctx" "$d/n09e" 0
    printf 'import golang.org/x/sys for the syscall shim\n'                > "$d/n10"; idc_st_case "a go import path" "" "$d/n10" 0
    printf 'see https://github.com/ritchiecarroll/go2cs for the tree\n'    > "$d/n11"; idc_st_case "the repository public URL" "" "$d/n11" 0
    # The ESCAPED spelling, which is how this arm's own line in the definition carries it, and the
    # printf-template spelling, which is how this very file carries it. Their absence from the
    # first draft's prefix-keyed exception is what made the census refuse its own diff.
    printf 'the arm reads github%s.com/ritchiecarroll/go2cs here\n' "$bs"  > "$d/n11b"; idc_st_case "the public URL in its ERE-ESCAPED spelling" "" "$d/n11b" 0
    # The REFUSE direction of this exception is in section D, not here, for two reasons: these
    # cases run on the SYNTHETIC token set, where the account arm is not live and the control would
    # be vacuous; and writing a bare handle into this source would itself put a matchable string on
    # the pushed surface. Section D plants the REAL literal from the run-time set into a temp file
    # and asserts it fires -- the refuse direction, spelling nothing here.
    printf 'the toolchain lives under %sUSERPROFILE%s%ssdk\n' "$pc" "$pc" "$bs" > "$d/n12"; idc_st_case "the profile ENVIRONMENT VARIABLE" "" "$d/n12" 0
    printf 'built at C:%sUsers%s<user>%ssdk\n' "$bs" "$bs" "$bs"           > "$d/n13"; idc_st_case "a redacted profile segment" "" "$d/n13" 0
    idc_st_exc "  and the placeholder set is what admitted it"    "profile_root|placeholder-segment" "$IDC_TMP/st.status"
    printf 'the tree built internal.itoa.dll v%d.%d.%d.%d today\n' 1 24 13 3 > "$d/n14"; idc_st_case "v-prefixed version quad" "" "$d/n14" 0
    printf 'a C++ // comment and a ratio 3/4 in prose\n'                   > "$d/n15"; idc_st_case "a doubled slash that is not a share" "" "$d/n15" 0
    printf 'the escaped literal C:%s%sUsers is quoted -json output\n' "$bs" "$bs" > "$d/n16"; idc_st_case "a doubled backslash from json escaping" "" "$d/n16" 0

    echo
    echo "  B3. C2's A/B -- ADJACENCY, NOT A WINDOW. A version word must be NEXT TO the quad."
    # ARM A: a routable-shaped quad with a context word three words away on the same line. Under a
    # 56/32 window this read CLEAN; a window that can be arranged is a laundering surface.
    printf 'HOST=%d.%d.%d.%d was the release build target\n' 203 0 113 7 > "$d/ab1"; idc_st_case "context word three words away STILL HITS" "host_ctx ipv4" "$d/ab1" 0
    # ARM B: the same quad with nothing around it -- the control that arm A is not just always-hit.
    printf 'HOST=%d.%d.%d.%d answered\n' 203 0 113 7                     > "$d/ab2"; idc_st_case "  the same quad with no context at all"   "host_ctx ipv4" "$d/ab2" 0
    # And the two shapes that MUST still be excused, so the narrowing is not simply a dead rule 4.
    printf 'assembly Version=%d.%d.%d.%d shipped\n' 1 24 13 3            > "$d/ab3"; idc_st_case "Version=<quad> is still excused"          "" "$d/ab3" 0
    printf 'the assembly %d.%d.%d.%d validation page\n' 1 24 13 3        > "$d/ab4"; idc_st_case "<context> <quad> <context> is still excused" "" "$d/ab4" 0
    idc_st_exc "  by ADJACENCY on the neighbouring words"         "ipv4|version-context" "$IDC_TMP/st.status"

    echo
    echo "  B2. THE SHORT-MATCH PATH -- the same cases with the engine's reported start PERTURBED"
    echo "      (mawk 1.3.4's match() is not leftmost-longest and returns RLENGTH=7 on a four-octet"
    echo "       quad; this forces a strictly harder perturbation so gawk exercises the same code)"
    IDC_SHORT=3
    idc_st_case "version quad, space-separated context (short match)" "" "$d/n03" 0
    idc_st_exc "  still excluded BY THE WINDOW, from the right end" "ipv4|version-context" "$IDC_TMP/st.status"
    idc_st_case "version word three words back (short match)"        "ipv4" "$d/n05" 0
    idc_st_case "package-prefixed version quad (short match)"        "" "$d/n01" 0
    idc_st_case "toolchain-prefixed version quad (short match)"      "" "$d/n02" 0
    idc_st_exc "  still excluded BY THE TOKEN RUN, from the right end" "ipv4|token-run" "$IDC_TMP/st.status"
    idc_st_case "loopback constant (short match)"                    "" "$d/n06" 0
    idc_st_exc "  still excluded AS A DOC CONSTANT (exact quad)"    "ipv4|doc-constant" "$IDC_TMP/st.status"
    # The refuse direction: the perturbation must not make the arm go BLIND, which an all-negative
    # short-match battery would read as green.
    idc_st_case "a real quad is STILL a hit under a short match"     "ipv4" "$d/p02" 1
    idc_st_case "assignment context + quad under a short match"      "host_ctx ipv4" "$d/p03" 1
    IDC_SHORT=0

    echo
    echo "  C. THE REFUSAL PATH MUST MASK -- entry mode end to end, output checked for the plant"
    printf 'built at C:%sUsers%s%s%ssdk\n' "$bs" "$bs" "sylvandeep" "$bs"  > "$d/r01"
    out="$d/r01.out"
    "$IDC_SELF" entry "$d/r01" > "$out" 2>&1
    rc=$?
    if [ "$rc" -eq 1 ]; then
        printf '  PASS  %-48s exit=1\n' "entry mode REFUSES a profile-path plant"
        IDC_ST_PASS=$((IDC_ST_PASS + 1))
    else
        printf '  FAIL  %-48s exit=%d (expected 1)\n' "entry mode REFUSES a profile-path plant" "$rc"
        IDC_ST_FAIL=$((IDC_ST_FAIL + 1))
    fi
    idc_st_assert_absent  "refusal output does not spell the plant segment" "sylvandeep" "$out"
    idc_st_assert_present "refusal output names the arm"                    "profile_root" "$out"
    idc_st_assert_present "refusal output carries a masked rendering"       "<*REDACTED-10*>" "$out"
    idc_st_assert_present "refusal output carries the hit count"            "hits=1" "$out"
    # The longest-literal-first property: a literal that CONTAINS another must not render as the
    # contained literal's mask plus a surviving suffix.
    printf 'owner column reads %s here\n' "zorbulaxqueen"                   > "$d/r02"
    out="$d/r02.out"
    IDC_TEST_TOKENS="zorbulax quennelbee zorbulaxqueen" "$IDC_SELF" entry "$d/r02" > "$out" 2>&1
    idc_st_assert_absent  "container literal leaves no surviving suffix"    "queen" "$out"
    idc_st_assert_present "container literal masked at its own length"      "<*REDACTED-13*>" "$out"

    echo
    echo "  D0. THE RUN-TIME ARMS ARE BOUNDED BY THE DENIED SET -- three bars, one control each"
    unset IDC_TEST_TOKENS
    for probe in "root:under 5 characters" "ubuntu:stop-listed generic account name" "buildbox7:not in the denied set"; do
        IDC_TEST_ACCOUNT="${probe%%:*}"; export IDC_TEST_ACCOUNT
        idc_build_tokens
        case "$IDC_INERT" in
            *"RUNTIME_ACCOUNT: "*"${probe#*:}"*)
                printf '  PASS  %-48s inert: %s\n' "a derived account probe is INERT" "${probe#*:}"
                IDC_ST_PASS=$((IDC_ST_PASS + 1)) ;;
            *)
                printf '  FAIL  %-48s expected inert(%s), got: %s\n' "a derived account probe is INERT" "${probe#*:}" "$(printf '%b' "$IDC_INERT" | tr -d '\n')"
                IDC_ST_FAIL=$((IDC_ST_FAIL + 1)) ;;
        esac
        # and it must not be in the live token set at all
        c="$(awk -F'\t' '$1 == "RUNTIME_ACCOUNT" { n++ } END { print n + 0 }' "$IDC_TOKFILE")"
        case "$c" in
            ''|0) printf '  PASS  %-48s live RUNTIME_ACCOUNT literals=0\n' "  and it is not a live arm"; IDC_ST_PASS=$((IDC_ST_PASS + 1)) ;;
            *)    printf '  FAIL  %-48s live RUNTIME_ACCOUNT literals=%s\n' "  and it is not a live arm" "$c"; IDC_ST_FAIL=$((IDC_ST_FAIL + 1)) ;;
        esac
    done
    unset IDC_TEST_ACCOUNT

    echo
    echo "  D. THE RUN-TIME ARMS ON THIS BOX -- fired yes/no only, never a value"
    idc_build_tokens
    # The FIRE direction of the same bars: a derivation whose hash IS a row in the shared hashes
    # file clears them. An all-inert battery above would read green on an arm that can never fire.
    case "$IDC_HASH_HITS" in
        ''|0) printf '  FAIL  %-48s hash matches=0 -- no run-time arm can fire here\n' "a denied-set token clears the bars" ; IDC_ST_FAIL=$((IDC_ST_FAIL + 1)) ;;
        *)    printf '  PASS  %-48s hash matches=%s\n' "a denied-set token clears the bars" "$IDC_HASH_HITS"; IDC_ST_PASS=$((IDC_ST_PASS + 1)) ;;
    esac
    local tfpresent="$IDC_TOKFILE_PRESENT" nlit=0 fired="no"
    nlit="$(awk 'END { print NR }' "$IDC_TOKFILE")"
    case "$nlit" in
        ''|0)
            echo "  SKIP  the run-time token set is EMPTY on this box -- no arm to control."
            echo "        token file present=$tfpresent. An empty token set is NAMED, never read as clean."
            ;;
        *)
            : > "$d/rt"; chmod 600 -- "$d/rt" 2>/dev/null
            awk -F'\t' 'NR == 1 { print "owner column reads " $2 " here" }' "$IDC_TOKFILE" > "$d/rt"
            : > "$IDC_TMP/st.keys"; : > "$IDC_TMP/st.status"
            idc_run_awk "$d/rt" "$IDC_TMP/st.keys" "$IDC_TMP/st.report" "$IDC_TMP/st.status" 1
            rc=$?
            fired="$(idc_st_fired "$IDC_TMP/st.status")"
            rm -f -- "$d/rt"
            if [ "$rc" -eq 0 ] && [ -n "$fired" ]; then
                printf '  PASS  %-48s token file present=%s, literals=%s\n' "run-time token arm fired=yes" "$tfpresent" "$nlit"
                IDC_ST_PASS=$((IDC_ST_PASS + 1))
            else
                printf '  FAIL  %-48s token file present=%s, literals=%s\n' "run-time token arm fired=no" "$tfpresent" "$nlit"
                IDC_ST_FAIL=$((IDC_ST_FAIL + 1))
            fi
            printf 'an ordinary line with nothing of the kind on it\n' > "$d/rt2"
            : > "$IDC_TMP/st.keys"; : > "$IDC_TMP/st.status"
            idc_run_awk "$d/rt2" "$IDC_TMP/st.keys" "$IDC_TMP/st.report" "$IDC_TMP/st.status" 1
            rc=$?
            fired="$(idc_st_fired "$IDC_TMP/st.status")"
            if [ "$rc" -eq 0 ] && [ -z "$fired" ]; then
                printf '  PASS  %-48s\n' "run-time token arm silent on a clean body"
                IDC_ST_PASS=$((IDC_ST_PASS + 1))
            else
                printf '  FAIL  %-48s arms{%s}\n' "run-time token arm fired on a clean body" "$fired"
                IDC_ST_FAIL=$((IDC_ST_FAIL + 1))
            fi
            # THE PUBLISHED-ATTRIBUTION EXCEPTION, controlled with the REAL token set -- the only
            # set it can possibly matter for. Both spellings: the URL as written in prose, and the
            # ERE-ESCAPED form the definition file itself carries. The refuse direction is the two
            # checks above; this is the admit direction, and an admit-only control would be green
            # on a dead arm, which is why it is not the only one here.
            printf 'see https://github.com/ritchiecarroll/go2cs and the arm reads github%s.com/ritchiecarroll/go2cs\n' "$bs" > "$d/rt3"
            : > "$IDC_TMP/st.keys"; : > "$IDC_TMP/st.status"
            idc_run_awk "$d/rt3" "$IDC_TMP/st.keys" "$IDC_TMP/st.report" "$IDC_TMP/st.status" 1
            rc=$?
            fired="$(idc_st_fired "$IDC_TMP/st.status")"
            if [ "$rc" -eq 0 ] && [ -z "$fired" ]; then
                printf '  PASS  %-48s\n' "public handle admitted in BOTH spellings"
                IDC_ST_PASS=$((IDC_ST_PASS + 1))
            else
                printf '  FAIL  %-48s arms{%s}\n' "public handle refused in some spelling" "$fired"
                IDC_ST_FAIL=$((IDC_ST_FAIL + 1))
            fi
            ;;
    esac

    echo
    echo "  E. DECLARED SET"
    printf 'nothing of interest on this line\n' > "$d/decl"
    idc_census "$d/decl" "declared set" "$IDC_TMP/keys.decl" 1

    echo
    echo "SELF-TEST: pass=$IDC_ST_PASS fail=$IDC_ST_FAIL"
    case "$IDC_ST_FAIL" in
        ''|0) echo "SELF-TEST PASSED"; return 0 ;;
        *)    echo "SELF-TEST FAILED"; return 3 ;;
    esac
}

# -------------------------------------------------------------------------------------------------
while [ "$#" -gt 0 ]; do
    case "$1" in
        --unmask) IDC_UNMASK=1; shift ;;
        --) shift; break ;;
        *) break ;;
    esac
done
if [ "$#" -lt 1 ]; then idc_misuse "no mode given"; fi
IDC_MODE="$1"; shift
case "$IDC_MODE" in
    entry)    idc_mode_entry "$@";    exit $? ;;
    subject)  idc_mode_subject "$@";  exit $? ;;
    tree)     idc_mode_tree "$@";     exit $? ;;
    selftest) idc_mode_selftest "$@"; exit $? ;;
    *)        idc_misuse "unknown mode '$IDC_MODE'" ;;
esac
