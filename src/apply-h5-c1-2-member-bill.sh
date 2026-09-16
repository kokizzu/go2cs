#!/usr/bin/env bash
# apply-h5-c1-2-member-bill.sh -- the C1-2 runtime2.cs MEMBER BILL, applied to an H5 SCRATCH tree.
#
# WHY THIS IS A SCRIPT AND NOT A COMMIT, same reason as C1-1 and measured the same way: every edit
# below is CORRECT ONLY AFTER the 1.24.13 reconvert. `g.syncGroup` is `ж<synctestGroup>`, and
# `synctestGroup` is declared in synctest.go, which is a 1.24 file -- there is no synctest.cs in the
# corpus today, so landing this edit breaks a tree that is green. Hence: a prepared patch, applied
# between the reconvert and the build, with a precondition that REFUSES on a pre-hop tree.
#
#   apply-h5-c1-2-member-bill.sh <scratch-core-dir> [<goroot>]   apply, then assert the post-condition
#   apply-h5-c1-2-member-bill.sh --verify <dir> [<goroot>]       assert only; changes nothing
#   apply-h5-c1-2-member-bill.sh --self-test [<goroot>]          red-first, no clone touched
#
# GOROOT resolution: $2 (or $3 for --verify) > $H5_GOROOT > $GOROOT > `go env GOROOT`.
#
# Exit 0 applied-and-verified (or already applied) / 1 a post-condition failed / 2 misuse or refusal.
set -u

R2="runtime/runtime2.cs"

# ---------------------------------------------------------------------------------------------
# THE BILL (COORD 82de2fc7c, the CORRECTED text; C1's HOLD at 3ee0f07ff, C2's two rows at 2a6938f4b,
# i9's independent confirmation at 0259e007f).
#
#   2   g fields               syncGroup, fipsIndicator
#   14  constants RENUMBERED   +1 each, indices 24..37 -> 25..38
#   6   constants ADDED        SyncWaitGroupWait at 24, Synctest* at 39..43
#   6   waitReasonStrings entries
#   1   isIdleInSynctest       accessor + 12-keyed table, materialising dense at 44
#   0   m.mWaitList            omitted, reason recorded AT THE SITE
#   0   m's size-class padding omitted, reason recorded AT THE SITE
#
# ⚠ WHY THIS BILL NEEDED A HOLD, and why the falsifier below is numeric rather than a build. The six
# constants are an ADDITION IN THE MIDDLE: `waitReasonSyncWaitGroupWait` lands at index 24, so the
# fourteen constants from `waitReasonTraceReaderBlocked` up shift by one. The corpus spells every
# constant as an explicit `= N` literal, so a WRONG N COMPILES PERFECTLY, and `waitReasonStrings` is
# keyed SYMBOLICALLY, so it follows the constants wherever they go. Renumber correctly and everything
# lines up; renumber not at all and every constant from 24 up names its NEIGHBOUR's wait reason, with
# no error anywhere and a green suite. C1's own first sizing said "six appended" and was wrong, and
# none of the three falsifiers then on the table could fire on it.
#
# So NOTHING HERE IS TYPED. The names, the values and the strings are all extracted from Go's own
# runtime2.go at the resolved GOROOT, and the post-condition re-extracts and joins BY NAME. A typed
# table would be a second copy of the thing being checked.
#
# ⚠ AND THE EXTRACTOR REFUSES ON AN EMPTY READ, which is the other half of that near-miss. C1's first
# extraction returned ZERO constants for BOTH releases -- an awk range that never opened, then `\t`
# in `grep -E`, which is not a tab -- and the prefix check compared two empty files and printed
# "IDENTICAL: the six are APPENDED, no renumbering". A COMPARISON OF TWO EMPTY SETS REPORTS AGREEMENT,
# and "identical" is the most dangerous word an empty reading can produce, because unlike a zero count
# it does not look like nothing. Every extraction below has a floor and dies under it.
#
# ⚠ THE 38 IS i9's CORRECTION (0259e007f) AND IT BUYS THE POST-CONDITION. C1 reported "37 corpus
# constants matched" -- one short, because `waitReasonZero` is spelled `= /* iota */ 0;` with the
# comment BETWEEN the equals sign and the value, and the pattern skipped it. The corpus declares 38,
# 1.23.12 declares 38, and the correspondence is EXACT rather than near -- so the post-condition can
# be an equality (corpus count == Go count) instead of an inequality. The regex below carries the
# optional `/* iota */` for exactly that reason.
#
# ORDER vs C1-1: region-disjoint (C1-1 edits the usings and deletes `partial struct note`; this edits
# the g struct, the waitReason block, waitReasonStrings and appends the idle table), so they commute.
# COORD's dispatch runs C1-1 first; nothing here depends on that.
# ---------------------------------------------------------------------------------------------

die() { echo "REFUSE: $*" >&2; exit 2; }

# ⚠ THE TOOL GATE IS CARRIED FROM C1-1 DELIBERATELY, hardened by i9 (a50d4f8c1) and C2 (a2b892aef):
# it asserts an ANSWER and not an exit status, because a program that ignores its arguments and exits
# 0 -- /bin/true, /bin/echo, a Windows Store redirector -- satisfies a status probe and does no work,
# and the loop taking the first passer would then never reach the real interpreter. A TOOL THAT EXITS
# 0 HAS NOT TOLD YOU IT DID THE WORK. Copied rather than sourced so this script stands alone on a
# scratch tree; the twin is src/apply-h5-c1-1-rederives.sh.
PYBIN=""
# ⚠ THE ANSWER IS COMPARED WITH ITS CARRIAGE RETURNS STRIPPED, and that is a FIX, not a relaxation
# (q99, COORD 3ff-ruling; measured on G's box, where `py` runs and this gate refused it). The Windows
# Python launcher is `py.exe`, a native Windows program: through a Git-Bash pipe it answers
# `42\r\n`, and `$(...)` strips only the trailing newline -- so the comparison saw `42\r` and
# refused an interpreter that works perfectly. On a box whose `python3` and `python` are Store
# redirectors (they print nothing and exit 0, so they are correctly skipped), `py` is the ONLY
# candidate, and the run died naming "no working Python interpreter found" while one was installed.
#
# The strip does NOT widen the gate, and the arm below proves both halves: an answer of `43\r\n` is
# still refused, and /bin/echo -- which prints its arguments and exits 0 -- is still refused. What is
# tolerated is the line ENDING of a correct answer, which is a property of the pipe and not of the
# interpreter. A TOOL THAT EXITS 0 HAS STILL NOT TOLD YOU IT DID THE WORK.
py_answers() { [ "$("$1" -c 'print(6*7)' 2>/dev/null | tr -d '\r')" = "42" ]; }
resolve_python() {
  local c
  if [ -n "${H5_PYTHON:-}" ]; then
    py_answers "$H5_PYTHON" \
      || die "H5_PYTHON=$H5_PYTHON is not a working Python -- it did not answer 'print(6*7)' with 42.
      Refusing rather than reporting success over an edit that never ran. A program that merely exits
      0 (a Windows Store redirector, /bin/true) satisfies a status-only probe and does no work."
    PYBIN=$H5_PYTHON; return 0
  fi
  for c in python3 python py; do
    command -v "$c" >/dev/null 2>&1 || continue
    py_answers "$c" || continue
    PYBIN=$c; return 0
  done
  die "no working Python interpreter found (tried python3, python, py; set H5_PYTHON to override).
      This script derives its table and performs its edits in Python, and REFUSES here rather than
      reporting APPLIED over an edit that never ran."
}

# The Go source is the authority for every name, value and string this patch writes.
GOSRC=""
resolve_goroot() {
  local root=${1:-}
  [ -n "$root" ] || root=${H5_GOROOT:-}
  [ -n "$root" ] || root=${GOROOT:-}
  [ -n "$root" ] || root=$(go env GOROOT 2>/dev/null || true)
  [ -n "$root" ] || die "no GOROOT -- pass it as the last argument, or set H5_GOROOT/GOROOT.
      This patch DERIVES the bill from <goroot>/src/runtime/runtime2.go rather than carrying a typed
      copy of it, so it cannot run without one."
  GOSRC="$root/src/runtime/runtime2.go"
  [ -f "$GOSRC" ] || die "$GOSRC does not exist -- '$root' is not a GOROOT"
}

# Is this a tree the patch BELONGS on? The synctest.cs check is the whole reason this is a patch:
# `g.syncGroup` is `ж<synctestGroup>`, and that type arrives with the 1.24 emission.
check_precondition() {
  local core=$1
  [ -f "$core/$R2" ] || die "no $R2 under $core -- not a corpus root"
  [ -f "$core/runtime/synctest.cs" ] || die "runtime/synctest.cs is ABSENT under $core -- so
      synctestGroup does not exist on this tree and adding g.syncGroup would not compile. This is a
      PRE-HOP tree; run the 1.24.13 reconvert first. (Do not create synctest.cs by hand.)"
}

# One program, two modes. verify() needs the same Go extraction apply() does -- it joins the patched
# file against Go BY NAME -- so unlike C1-1's pure-shell checker this one needs the interpreter too.
py() { "$PYBIN" - "$@" <<'PY'
import io, re, sys

MODE, CSPATH, GOPATH = sys.argv[1], sys.argv[2], sys.argv[3]

# ⚠ THE WINDOWS DEFECT i9 MEASURED (mailbox f73b56b18 §3), and only a Windows box could find it. Every
# name this reports carries a `Δ` -- ΔisWaitingForSuspendG, ΔisIdleInSynctest -- and on a console whose
# default codec is cp1252 the FAILING path dies mid-list:
#
#     UnicodeEncodeError: 'charmap' codec can't encode character 'Δ' in position 7
#
# i9 measured the direction, which is the whole question: rc=1 either way and the GREEN path never
# prints a Δ, so it can never produce a false PASS -- what it does is hand a Windows reader 10 of 17
# FAIL lines and a traceback, losing both g fields, both m omission notes, the idle-table presence check
# and the accessor. A reader debugging a half-applied tree would have been told about the constants and
# the strings and nothing about the fields: a diagnosis truncated exactly where it stops being about the
# rows a build can already see.
#
# Fixed HERE rather than by asking the operator for PYTHONIOENCODING=utf-8, because an env var is a
# thing to remember and this is a thing to guarantee. Guarded: reconfigure() is 3.7+, and a stream that
# refuses is left alone rather than crashing the run over its own diagnostics.
for _stream in (sys.stdout, sys.stderr):
    try:
        _stream.reconfigure(encoding='utf-8', errors='backslashreplace')
    except Exception:
        pass

def refuse(msg):
    sys.stderr.write("REFUSE: " + msg + "\n")
    raise SystemExit(2)

FAIL = []
def fail(msg):
    FAIL.append(msg)

# ------------------------------------------------------------------ the Go side (the authority)
def go_tables(path):
    src = io.open(path, encoding='utf-8', newline='').read().replace('\r\n', '\n')
    anchor = re.search(r'^type waitReason uint8$', src, re.M)
    if not anchor:
        refuse("%s does not declare `type waitReason uint8` -- this is not runtime2.go" % path)
    block = re.search(r'^const \(\n(.*?)\n\)$', src[anchor.end():], re.M | re.S)
    if not block:
        refuse("could not find the waitReason const block in %s" % path)

    consts = []                       # [(name, iota index, comment text or None)]
    for raw in block.group(1).split('\n'):
        s = raw.strip()
        if not s or s.startswith('//'):
            continue
        name = s.split()[0]
        if not name.startswith('waitReason'):
            refuse("unexpected line in Go's waitReason const block: %r" % raw)
        cm = re.search(r'//\s*"(.*)"\s*$', s)
        consts.append((name, len(consts), cm.group(1) if cm is not None else None))

    # THE FLOOR. An extraction this small is not a finding, it is a broken reader -- and a broken
    # reader compared against another broken reader reports agreement.
    if len(consts) < 10:
        refuse("extracted %d waitReason constants from %s. An extraction that small is a BROKEN "
               "READER, not a small release, and comparing it against anything reports agreement."
               % (len(consts), path))

    tbl = re.search(r'^var waitReasonStrings = \[\.\.\.\]string\{\n(.*?)\n\}$', src, re.M | re.S)
    if not tbl:
        refuse("could not find `var waitReasonStrings` in %s" % path)
    strings = {}
    for raw in tbl.group(1).split('\n'):
        s = raw.strip()
        if not s or s.startswith('//'):
            continue
        em = re.match(r'^(waitReason\w+):\s*"(.*)",$', s)
        if em is None:
            refuse("unparsed waitReasonStrings line in %s: %r" % (path, raw))
        strings[em.group(1)] = em.group(2)
    if len(strings) < 10:
        refuse("extracted %d waitReasonStrings entries from %s -- broken reader, see above"
               % (len(strings), path))

    # ⚠ THE VERSION DISCRIMINATOR, and it is DERIVED rather than a typed list of the six new names or
    # a magic count. isIdleInSynctest does not exist before go1.24, so a 1.23 GOROOT refuses HERE,
    # naming the table, instead of silently under-filling the bill.
    idle_tbl = re.search(r'^var isIdleInSynctest = \[len\(waitReasonStrings\)\]bool\{\n(.*?)\n\}$',
                         src, re.M | re.S)
    if not idle_tbl:
        refuse("%s has no `var isIdleInSynctest` table -- that table arrives with go1.24, so this "
               "GOROOT is older than the hop target. Point at the 1.24.13 GOROOT." % path)
    idle = []
    for raw in idle_tbl.group(1).split('\n'):
        s = raw.strip()
        if not s or s.startswith('//'):
            continue
        km = re.match(r'^(waitReason\w+):\s*true,$', s)
        if km is None:
            refuse("unparsed isIdleInSynctest line in %s: %r" % (path, raw))
        idle.append(km.group(1))
    if len(idle) < 5:
        refuse("extracted %d isIdleInSynctest keys from %s -- broken reader, see above"
               % (len(idle), path))

    # CONTROLS ON THE EXTRACTION ITSELF, free because Go states the same fact twice. The const
    # block's trailing comment and the waitReasonStrings entry are independent spellings of one
    # string; if the two readers disagree, one of them is wrong and neither reading is usable.
    names = [c[0] for c in consts]
    if sorted(names) != sorted(strings.keys()):
        refuse("Go's const block and waitReasonStrings disagree about WHICH constants exist "
               "(%d vs %d) -- one of the two readers is wrong" % (len(names), len(strings)))
    for name, _idx, text in consts:
        if text is not None and text != strings[name]:
            refuse("Go's own comment and waitReasonStrings disagree for %s (%r vs %r) -- the "
                   "extraction is not trustworthy" % (name, text, strings[name]))
    for k in idle:
        if k not in strings:
            refuse("isIdleInSynctest keys %s, which is not a waitReason constant" % k)
    for name, text in strings.items():
        if '"' in text or '\\' in text:
            refuse("waitReasonStrings[%s] contains a quote or backslash (%r); this patch emits C# "
                   "u8 literals verbatim and has no escaping rules" % (name, text))
    return consts, strings, idle

GO_CONSTS, GO_STRINGS, GO_IDLE = go_tables(GOPATH)
GO_INDEX = dict((n, i) for n, i, _t in GO_CONSTS)

# ------------------------------------------------------------------ the corpus side
CONST_RE = re.compile(
    r'^(internal static readonly waitReason (waitReason\w+) = (?:/\* iota \*/ )?)(\d+)(;)(.*)$')
STR_RE   = re.compile(r'^    \[(waitReason\w+)\] = "(.*)"u8(,?)$')
IDLE_RE  = re.compile(r'^    \[(waitReason\w+)\] = true(,?)$')

STR_OPEN  = 'internal static array<@string> waitReasonStrings = new golib.SparseArray<@string>{'
SUSP_OPEN = 'internal static array<bool> ΔisWaitingForSuspendG = new golib.SparseArray<bool>{'
IDLE_OPEN = 'internal static array<bool> ΔisIdleInSynctest = new golib.SparseArray<bool>{'
CLOSE     = '}.array();'
# ⚠ THE CLOSER IS NO LONGER ONE STRING. COORD b3a32e52d/486a3926a ruled both [len(waitReasonStrings)]bool
# tables must materialise with the DECLARED LENGTH passed explicitly, so their closers read
# `}.array(44);` while waitReasonStrings -- which Go declares `[...]string`, where max key + 1 IS the
# declared length -- keeps the bare form. Every close scan matches either shape and the length is read
# back rather than assumed.
CLOSE_RE  = re.compile(r'^\}\.array\((\d*)\);$')
G_OPEN    = '[GoType] partial struct g {'
M_OPEN    = '[GoType] partial struct m {'
LOCKEDM   = '    internal muintptr lockedm;'
COROARG   = '    internal ж<coro> coroarg;'
FIPS      = '    internal uint8 fipsIndicator;'
SYNCGRP   = '    internal ж<synctestGroup> syncGroup;'
NEXTWAITM = '    internal muintptr nextwaitm;'
LOCKSHELD = '    internal array<heldLockInfo> locksHeld = new(10);'
MWAIT_MARK = 'go1.24 RENAMES this field to `mWaitList mWaitList`'
PAD_MARK   = 'go1.24 adds a blank size-class padding field here'

raw = io.open(CSPATH, encoding='utf-8', newline='').read()
# Line endings are part of the artifact: a rewrite that normalises them shows as a whole-file diff
# and masks the real change. Refuse a mixed file rather than guess.
if raw.count('\r\n') and raw.count('\r\n') == raw.count('\n'):
    EOL = '\r\n'
elif '\r' not in raw:
    EOL = '\n'
else:
    refuse("%s has MIXED line endings -- refusing rather than normalising them" % CSPATH)
lines = raw.replace('\r\n', '\n').split('\n')

def find_one(text, what):
    hits = [i for i, l in enumerate(lines) if l == text]
    if len(hits) != 1:
        refuse("expected exactly ONE %s in %s, found %d" % (what, CSPATH, len(hits)))
    return hits[0]

def block_close(open_at, what):
    """Index of the closer, in EITHER shape (bare or length-carrying)."""
    for i in range(open_at + 1, len(lines)):
        if CLOSE_RE.match(lines[i]):
            return i
    refuse("no '}.array(...);' closing the %s" % what)

def block_length(close_at):
    """The length the closer passes, or None for the bare form. Read back, never assumed."""
    m = CLOSE_RE.match(lines[close_at])
    return int(m.group(1)) if m.group(1) else None

def struct_range(open_text, what):
    at = find_one(open_text, what)
    for i in range(at + 1, len(lines)):
        if lines[i] == '}':
            return at, i
    refuse("no closing brace for %s" % what)

def const_lines():
    idxs = [i for i, l in enumerate(lines) if CONST_RE.match(l)]
    if not idxs:
        refuse("no waitReason constant declarations in %s -- broken reader or wrong file" % CSPATH)
    if len(idxs) < 10:
        refuse("only %d waitReason constants in %s -- an extraction that small is a broken reader"
               % (len(idxs), CSPATH))
    if idxs != list(range(idxs[0], idxs[0] + len(idxs))):
        refuse("the waitReason constants in %s are not contiguous; this patch inserts by position "
               "and will not guess" % CSPATH)
    return idxs

def comment_col(line):
    """Column of '//' on a constant line, or -1. The block's alignment is IRREGULAR (measured: 73,
    74 and 84 all occur), so every emitted line copies the column of the line it is placed next to
    rather than a block-wide constant."""
    m = CONST_RE.match(line)
    if not m:
        return -1
    j = m.group(5).find('//')
    if j < 0:
        return -1
    return len(m.group(1)) + len(m.group(3)) + len(m.group(4)) + j

def set_value(line, want):
    m = CONST_RE.match(line)
    col = comment_col(line)
    head = m.group(1) + str(want) + m.group(4)
    if col < 0:
        return head + m.group(5)
    return head + ' ' * max(1, col - len(head)) + m.group(5)[m.group(5).find('//'):]

def new_const(name, value, model):
    head = 'internal static readonly waitReason %s = %d;' % (name, value)
    col = comment_col(model)
    text = '// "%s"' % GO_STRINGS[name]
    if col < 0:
        return head + ' ' + text
    return head + ' ' * max(1, col - len(head)) + text

# ------------------------------------------------------------------ apply
def apply():
    global lines
    # (1) + (2) the constants: every corpus constant takes GO's index, joined by NAME. That single
    # rule covers the fourteen renumbers and leaves the other twenty-four untouched; there is no
    # separate "shift these" list to get wrong.
    idxs = const_lines()
    have = {}
    for i in idxs:
        m = CONST_RE.match(lines[i])
        name = m.group(2)
        if name not in GO_INDEX:
            refuse("the corpus declares %s, which Go's own table does not know. Either the GOROOT is "
                   "wrong or this file is not the 1.23.12 hand-own." % name)
        have[name] = i
        want = GO_INDEX[name]
        if int(m.group(3)) != want:
            lines[i] = set_value(lines[i], want)

    # (3) the six additions, inserted after their predecessor by Go's own order.
    #
    # ⚠ ASCENDING, AND SORTED BY INDEX. Both halves were wrong in the first cut and the self-test
    # said so: descending order asks for waitReasonSynctestWait's predecessor (SynctestRun) before
    # SynctestRun has been inserted, and `sorted(missing)` on (name, index) pairs sorts by NAME,
    # which for these six is not their numeric order at all. Ascending keeps every predecessor in
    # place before it is needed; the bookkeeping below then shifts the recorded line numbers.
    missing = [(n, i) for n, i, _t in GO_CONSTS if n not in have]
    for name, value in sorted(missing, key=lambda p: p[1]):
        pred = [n for n, i, _t in GO_CONSTS if i == GO_INDEX[name] - 1]
        if not pred or pred[0] not in have:
            refuse("cannot place %s: its predecessor at index %d is not in the corpus"
                   % (name, GO_INDEX[name] - 1))
        at = have[pred[0]]
        lines.insert(at + 1, new_const(name, value, lines[at]))
        for k in have:
            if have[k] > at:
                have[k] += 1
        have[name] = at + 1

    # (4) waitReasonStrings. Rebuilt WHOLE from Go, in Go's order, rather than spliced -- the entries
    # are one line each with a fixed shape, so a rebuild reproduces the existing 38 byte for byte and
    # the diff is exactly the six insertions plus the comma the old last entry gains. Refuse first if
    # the corpus keys something Go does not, so a rebuild can never silently DROP a row.
    lo = find_one(STR_OPEN, 'waitReasonStrings opener')
    hi = block_close(lo, 'waitReasonStrings')
    for l in lines[lo + 1:hi]:
        m = STR_RE.match(l)
        if m is None:
            refuse("unparsed waitReasonStrings line in the corpus: %r" % l)
        if m.group(1) not in GO_STRINGS:
            refuse("the corpus keys waitReasonStrings[%s], which Go does not have" % m.group(1))
    body = ['    [%s] = "%s"u8' % (n, GO_STRINGS[n]) for n, _i, _t in GO_CONSTS]
    body = [b + ',' for b in body[:-1]] + [body[-1]]
    lines[lo + 1:hi] = body

    # (5) the isIdleInSynctest accessor and table, after its sibling, exactly as Go orders them.
    if IDLE_OPEN not in lines:
        sat = find_one(SUSP_OPEN, 'ΔisWaitingForSuspendG opener')
        send = block_close(sat, 'ΔisWaitingForSuspendG')
        keys = ['    [%s] = true' % k for k in GO_IDLE]
        keys = [k + ',' for k in keys[:-1]] + [keys[-1]]
        chunk = ['',
                 'internal static bool isIdleInSynctest(this waitReason w) {',
                 '    return ΔisIdleInSynctest[w];',
                 '}',
                 '',
                 '// isIdleInSynctest indicates that a goroutine is considered idle by synctest.Wait.',
                 IDLE_OPEN] + keys + ['}.array(%d);' % len(GO_CONSTS)]
        lines[send + 1:send + 1] = chunk

    # (5b) ⚠ BOTH bool tables take Go's DECLARED length explicitly (COORD 486a3926a). Go writes
    # `[len(waitReasonStrings)]bool` for each; the converter cannot fold a non-literal length and emits
    # a bare `.array()`, which SparseArray sizes at max key + 1 -- so isWaitingForSuspendG materialises
    # 36 today against Go's 38, and 37 against 44 after the renumber, and every index above its top key
    # THROWS where Go returns false (i9 confirmed 37..43 on a built tree). isIdleInSynctest reads 44
    # only because Go's twelfth key HAPPENS to be the last constant -- correct by coincidence of a top
    # key, which is not a property anyone should have to re-verify after the next insertion.
    #
    # The number is DERIVED from Go's own constant count, never typed, and the post-condition reads it
    # back and joins it against that count. The alternative spelling `.array(len(waitReasonStrings))`
    # would be self-maintaining and is NOT taken: static field initializer order is guaranteed textual
    # only WITHIN one part of a partial class, and runtime_package is spread over the whole package --
    # it happens to be safe here because all three fields sit in this file, which is exactly the kind
    # of coincidence this row exists to remove.
    for opener, what in ((SUSP_OPEN, 'ΔisWaitingForSuspendG'), (IDLE_OPEN, 'ΔisIdleInSynctest')):
        if opener not in lines:
            refuse("%s is absent -- cannot set its declared length" % what)
        at = find_one(opener, what + ' opener')
        close_at = block_close(at, what)
        lines[close_at] = '}.array(%d);' % len(GO_CONSTS)

    # (6) the two g fields, scoped to the g struct so the anchors cannot match a neighbour's.
    glo, ghi = struct_range(G_OPEN, 'struct g')
    if FIPS not in lines[glo:ghi]:
        for i in range(glo, ghi):
            if lines[i] == LOCKEDM:
                lines.insert(i + 1, FIPS)
                ghi += 1
                break
        else:
            refuse("could not find %r inside struct g" % LOCKEDM)
    if SYNCGRP not in lines[glo:ghi]:
        for i in range(glo, ghi):
            if lines[i].startswith(COROARG):
                lines.insert(i + 1, SYNCGRP)
                break
        else:
            refuse("could not find %r inside struct g" % COROARG)

    # (7) the two m-struct omissions, recorded AT THE SITE. Nothing is emitted into the struct; these
    # are the reasons, written where the next reader will be standing when they ask.
    mlo, mhi = struct_range(M_OPEN, 'struct m')
    if MWAIT_MARK not in '\n'.join(lines[mlo:mhi]):
        for i in range(mlo, mhi):
            if lines[i].startswith(NEXTWAITM):
                note = [
                  '    // ' + MWAIT_MARK + ' -- a list head, not a single m.',
                  '    // NOT CARRIED BY C1-2, and NOT because nothing needs it. At 1.24.13',
                  '    // goexperiment.spinbitmutex is ON, so the converter selects and emits',
                  '    // lock_spinbit.go, and FOUR of its sites name m.mWaitList -- measured on a',
                  '    // reconverted tree at windows/lock_spinbit.cs:220, :227 (twice) and :233.',
                  '    //',
                  '    // An earlier reading of "0 references in src/core" was taken on the PRE-HOP',
                  '    // corpus, where the selected lock file was a tristate the corpus emitted none of.',
                  '    // That is a fact about the pre-hop corpus and not about the tree this patch runs',
                  '    // on: a hop bill is sized on the RECONVERTED tree.',
                  '    //',
                  '    // The four sites are C1-2b\'s input rather than C1-2\'s omission. The corpus runs',
                  '    // Go\'s mutex on the MANAGED lock core (lock_managed_impl.cs, hand-own, shared by',
                  '    // every target), and at 1.23.12 the selected tristate file did not reach the build.',
                  '    // The open question is WHICH mechanism kept it out and whether that mechanism',
                  '    // reaches lock_spinbit.go -- not whether to add a field. Adding m.mWaitList and its',
                  '    // type so lock_spinbit.cs compiles beside the managed core would put two lock',
                  '    // protocols in one runtime and paper over which of them runs.',
                  '    // Measured 2026-09-13 (C1-2, COORD 82de2fc7c and b3a32e52d; i9 c2b26c50b).',
                ]
                lines[i:i] = note
                mhi += len(note)
                break
        else:
            refuse("could not find %r inside struct m" % NEXTWAITM)
    if PAD_MARK not in '\n'.join(lines[mlo:mhi]):
        for i in range(mlo, mhi):
            if lines[i] == LOCKSHELD:
                note = [
                  '',
                  '    // ' + PAD_MARK + ':',
                  '    //     _ [goexperiment.SpinbitMutexInt * 700 * (2 - goarch.PtrSize/4)]byte',
                  '    // NOT CARRIED, and it is not a fidelity gap. On a 64-bit target goarch.PtrSize is 8,',
                  '    // so the length is SpinbitMutexInt * 700 * 0 = ZERO -- a blank field of no bytes. Its',
                  '    // purpose is to keep Go\'s runtime.m inside the 2048-byte size class so the low bits',
                  '    // of a muintptr stay free for spinbit flags; C# struct layout is the CLR\'s and there',
                  '    // is no size class to hit. Measured 2026-09-13 (C1-2, COORD 82de2fc7c).',
                ]
                lines[i + 1:i + 1] = note
                break
        else:
            refuse("could not find %r inside struct m" % LOCKSHELD)

    io.open(CSPATH, 'w', encoding='utf-8', newline='').write(EOL.join(lines))

# ------------------------------------------------------------------ verify
def verify():
    idxs = const_lines()
    corpus = {}
    for i in idxs:
        m = CONST_RE.match(lines[i])
        corpus[m.group(2)] = int(m.group(3))

    # i9's exact equality (0259e007f): the corpus declared 38 against 1.23.12's 38, so after the
    # patch it must declare exactly what Go declares. An inequality would admit a half-applied file.
    if len(corpus) != len(GO_CONSTS):
        fail("constant COUNT is %d, Go declares %d" % (len(corpus), len(GO_CONSTS)))

    shifted, absent = [], []
    for name, idx, _t in GO_CONSTS:
        if name not in corpus:
            absent.append(name)
        elif corpus[name] != idx:
            shifted.append("%s corpus=%d go=%d" % (name, corpus[name], idx))
    for name in corpus:
        if name not in GO_INDEX:
            fail("the corpus declares %s, which Go does not" % name)
    # NAME every disagreement. A count would say "14 wrong" where the operator needs to know WHICH,
    # and this is the half of the bill that a build and a green suite are both blind to.
    if shifted:
        fail("%d constant(s) hold a value Go does not agree with -- this is the SILENT half, it "
             "compiles and it renames every reason it touches:\n       " % len(shifted)
             + "\n       ".join(shifted))
    if absent:
        fail("%d constant(s) missing: %s" % (len(absent), ", ".join(absent)))

    # waitReasonStrings: the same names, the same texts, and the key set covering 0..n-1 so the
    # SparseArray materialises DENSE. (SparseArray sizes itself at max key + 1; every key present
    # means the length is the count. The RUNTIME reading of len(waitReasonStrings) is i9's half.)
    lo = find_one(STR_OPEN, 'waitReasonStrings opener')
    hi = block_close(lo, 'waitReasonStrings')
    keyed = {}
    for l in lines[lo + 1:hi]:
        m = STR_RE.match(l)
        if m is None:
            fail("unparsed waitReasonStrings line: %r" % l)
            continue
        keyed[m.group(1)] = m.group(2)
    if len(keyed) != len(GO_STRINGS):
        fail("waitReasonStrings has %d entries, Go has %d" % (len(keyed), len(GO_STRINGS)))
    for name, text in GO_STRINGS.items():
        if name not in keyed:
            fail("waitReasonStrings is missing [%s] -- String() would return \"unknown wait reason\"" % name)
        elif keyed[name] != text:
            fail("waitReasonStrings[%s] is %r, Go says %r" % (name, keyed[name], text))
    covered = sorted(GO_INDEX[n] for n in keyed if n in GO_INDEX)
    if covered != list(range(len(GO_CONSTS))):
        fail("waitReasonStrings does not key every index 0..%d, so it does not materialise dense"
             % (len(GO_CONSTS) - 1))

    # the idle table: 12 keys, Go's twelve, topping out at the LAST constant.
    if IDLE_OPEN not in lines:
        fail("ΔisIdleInSynctest is absent")
    else:
        ilo = find_one(IDLE_OPEN, 'ΔisIdleInSynctest opener')
        ihi = block_close(ilo, 'ΔisIdleInSynctest')
        ikeys = []
        for l in lines[ilo + 1:ihi]:
            m = IDLE_RE.match(l)
            if m is None:
                fail("unparsed ΔisIdleInSynctest line: %r" % l)
                continue
            ikeys.append(m.group(1))
        if sorted(ikeys) != sorted(GO_IDLE):
            fail("ΔisIdleInSynctest keys %d name(s), Go keys %d; missing %s, extra %s"
                 % (len(ikeys), len(GO_IDLE),
                    sorted(set(GO_IDLE) - set(ikeys)) or "none",
                    sorted(set(ikeys) - set(GO_IDLE)) or "none"))
    if 'internal static bool isIdleInSynctest(this waitReason w) {' not in lines:
        fail("the isIdleInSynctest accessor is absent")

    # ⚠ THE DECLARED LENGTH, read back off BOTH bool tables and joined against Go's own count. This is
    # the row that makes the tables right BY DECLARATION rather than by where their top key happens to
    # land; the bare form is what truncated isWaitingForSuspendG to 36 against Go's 38, and what left
    # isIdleInSynctest correct only because Go's twelfth key is the last constant.
    want = len(GO_CONSTS)
    for opener, what in ((SUSP_OPEN, 'ΔisWaitingForSuspendG'), (IDLE_OPEN, 'ΔisIdleInSynctest')):
        if opener not in lines:
            fail("%s is absent" % what)
            continue
        got = block_length(block_close(find_one(opener, what + ' opener'), what))
        if got is None:
            fail("%s closes with a BARE `}.array();` -- Go declares it [len(waitReasonStrings)]bool, "
                 "so SparseArray sizes it at max key + 1 and every index above that THROWS where Go "
                 "returns false. It must pass the declared length %d." % (what, want))
        elif got != want:
            fail("%s materialises %d, Go declares %d" % (what, got, want))
    # And the three lengths must AGREE, which is the shape of Go's own declaration: both tables are
    # [len(waitReasonStrings)]bool, so a table sized right against a strings table sized wrong is not
    # right. (waitReasonStrings itself is Go `[...]string`, where max key + 1 IS the declared length,
    # so its bare closer is CORRECT and is deliberately left alone -- C2 c441e195a measured the rule.)
    if len(keyed) != want:
        fail("len(waitReasonStrings) reads %d while the tables declare %d -- the three lengths must "
             "agree" % (len(keyed), want))

    # the g fields, and they must be INSIDE struct g
    glo, ghi = struct_range(G_OPEN, 'struct g')
    for want, why in ((FIPS, 'runtime1.go reads and writes getg().fipsIndicator, so this one does '
                             'not compile without it'),
                      (SYNCGRP, 'chan/select/sema/time/proc all read gp.syncGroup at 1.24')):
        if want not in lines[glo:ghi]:
            fail("struct g is missing %r -- %s" % (want.strip(), why))

    # the two omissions are RECORDED, not merely omitted. An undocumented gap reads as an oversight
    # to the next person holding a 1.24.13 diff beside this file.
    mlo, mhi = struct_range(M_OPEN, 'struct m')
    mtext = '\n'.join(lines[mlo:mhi])
    if MWAIT_MARK not in mtext:
        fail("struct m carries no note about mWaitList -- the omission is undocumented")
    if PAD_MARK not in mtext:
        fail("struct m carries no note about the size-class padding field -- omission undocumented")

    return 0 if not FAIL else 1

if MODE == 'apply':
    apply()
elif MODE == 'verify':
    rc = verify()
    for f in FAIL:
        sys.stdout.write("  FAIL %s\n" % f)
    raise SystemExit(rc)
else:
    refuse("unknown mode %r" % MODE)
PY
}

apply() {
  [ -n "$PYBIN" ] || die "apply() reached with no interpreter resolved"
  py apply "$1/$R2" "$GOSRC"
  # ⚠ CAPTURED ON THE VERY NEXT LINE (i9, a50d4f8c1; safety floor #7). The C1-1 twin printed APPLIED
  # over an edit that never ran because this status was discarded.
  local rc=$?
  [ "$rc" -eq 0 ] || die "the edit step ($PYBIN) exited $rc -- NOTHING is claimed applied."
}

verify() {
  [ -n "$PYBIN" ] || die "verify() reached with no interpreter resolved"
  py verify "$1/$R2" "$GOSRC"
  return $?
}

# ---------------------------------------------------------------------------------- self-test
selftest() {
  resolve_python
  resolve_goroot "${1:-}"
  local tmp; tmp=$(mktemp -d) || die "mktemp failed"
  # shellcheck disable=SC2064
  trap "rm -rf '$tmp'" RETURN
  local arms=0 notrun=0 out rc self here
  self="$PWD/${BASH_SOURCE[0]}"
  [ -f "$self" ] || self=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/$(basename "${BASH_SOURCE[0]}")
  here=$(cd "$(dirname "$self")" && pwd)

  echo "h5-c1-2 member-bill self-test -- tree at $tmp, Go source $GOSRC"
  echo "every arm asserts the REASON it passed, never merely an exit code"
  echo

  # THE FIXTURE IS REAL DATA AND IT IS FROZEN, and the second half is the q99 correction.
  #
  # It used to be the LIVE src/core/runtime/runtime2.cs. That was real data, so it was right on the
  # first count and STALE BY COMPLETION on the second: once the bill is APPLIED to the corpus, the
  # "unpatched" tree mktree builds is already patched, ARM 2 cannot be red, and every arm downstream
  # measures a diff against a file that already carries the thing it checks for. The suite reported
  # that as a FAILURE rather than as a green, which is the one mercy in it -- but a guard that can
  # only fail once its subject is done is not a guard, and this one is the HOP'S instrument and has
  # to survive to be used.
  #
  # So the fixture is a COMMITTED PRE-BILL COPY beside this script: src/core/runtime/runtime2.cs as
  # it stood at dc78fb0df^, the parent of the H5 checkpoint that applied C1-1 and C1-2. Still real
  # data -- the actual hand-own, not a synthetic const block written to satisfy the parser -- and it
  # cannot go stale, because nothing applies a bill to it. ARM 0 below is what keeps that true.
  local SRC="$here/h5-c1-2-fixture/runtime2.pre-bill.cs"
  if [ ! -f "$SRC" ]; then
    echo "  NOT RUN: $SRC is absent -- this self-test reads the COMMITTED PRE-BILL fixture and will"
    echo "           not substitute the live corpus file, which is stale by completion once the bill"
    echo "           is applied. Run it from inside the clone."
    return 2
  fi

  mktree() {   # a minimal POST-hop tree: runtime2.cs as it stands + the 1.24 file that gates this patch
    local d=$1; rm -rf "$d"; mkdir -p "$d/runtime"
    cp "$SRC" "$d/runtime/runtime2.cs"
    printf 'namespace go;\r\npartial class runtime_package {\r\n[GoType] partial struct synctestGroup { }\r\n}\r\n' > "$d/runtime/synctest.cs"
  }

  # ARM 0 (PROVENANCE, and it exists because a frozen fixture solves stale-by-completion exactly
  # ONCE): re-freezing this file from an already-patched corpus would put the defect straight back
  # with every arm still GREEN -- which is strictly worse than the state it replaces, because the
  # suite would then be quiet about it. So the fixture is measured against the bill it is supposed to
  # PRECEDE: none of the six added constants may be present, and neither may the accessor the bill
  # appends. The names are spelled out rather than derived from Go, because a derived list would read
  # the same source the bill reads and could agree with a wrong fixture.
  arms=$((arms+1))
  local pre_added pre_accessor
  pre_added=$(grep -c -E 'waitReason(SyncWaitGroupWait|SynctestRun|SynctestWait|SynctestSelect|SynctestChanReceive|SynctestChanSend)' "$SRC" || true)
  pre_accessor=$(grep -c 'isIdleInSynctest' "$SRC" || true)
  [ "$pre_added" -eq 0 ] || { echo "ARM 0 FAILED: the frozen fixture already carries $pre_added line(s) naming the bill's ADDED constants -- it is not a PRE-BILL file, so ARM 2 cannot be red and nothing below this line proves anything"; return 1; }
  [ "$pre_accessor" -eq 0 ] || { echo "ARM 0 FAILED: the frozen fixture already carries the isIdleInSynctest accessor the bill appends -- it is not a PRE-BILL file"; return 1; }
  echo "  ok   the FIXTURE is PRE-BILL          a fixture re-frozen from a patched corpus goes RED here"

  # ARM 1 (RED FIRST): a PRE-HOP tree -- no synctest.cs -- must be REFUSED. This is the reason the
  # C1-2 bill is a patch and not a commit, so it is the first thing the suite proves.
  mktree "$tmp/pre"; rm -f "$tmp/pre/runtime/synctest.cs"
  out=$(bash "$self" "$tmp/pre" "$GOROOT_ARG" 2>&1); rc=$?
  arms=$((arms+1))
  [ "$rc" -eq 2 ] || { echo "ARM 1 FAILED: wanted refusal (2) on a pre-hop tree, got $rc"; echo "$out"; return 1; }
  case "$out" in *"synctest.cs is ABSENT"*) ;; *) echo "ARM 1 FAILED: refused without naming synctest.cs"; echo "$out"; return 1 ;; esac
  echo "  ok   a PRE-HOP tree is REFUSED         g.syncGroup would not compile without synctestGroup"

  # ARM 2 (RED): the unpatched hand-own must FAIL --verify, and must fail naming the SILENT half.
  # rc=1 alone would also be produced by a checker broken on every input.
  mktree "$tmp/raw"
  out=$(bash "$self" --verify "$tmp/raw" "$GOROOT_ARG" 2>&1); rc=$?
  arms=$((arms+1))
  [ "$rc" -eq 1 ] || { echo "ARM 2 FAILED: --verify passed the UNPATCHED hand-own (rc=$rc)"; echo "$out"; return 1; }
  case "$out" in *"this is the SILENT half"*) ;; *) echo "ARM 2 FAILED: red, but not for the renumber reason"; echo "$out"; return 1 ;; esac
  case "$out" in *"waitReasonSynctestSelect"*) ;; *) echo "ARM 2 FAILED: did not report the missing constants"; echo "$out"; return 1 ;; esac
  # ⚠ NOT `\[`. Inside a QUOTED case pattern the brackets are already literal, and a backslash there
  # is matched as a backslash -- so the escaped form looked for text no output can contain and the
  # arm went red on a correct reading. (Its twin two arms down was written unescaped and was right.)
  case "$out" in *"missing [waitReasonSyncWaitGroupWait]"*) ;; *) echo "ARM 2 FAILED: did not report the missing strings entries"; echo "$out"; return 1 ;; esac
  echo "  ok   the UNPATCHED file FAILS --verify naming the renumber AND both additions"

  # ARM 3 (GREEN): apply, then verify.
  mktree "$tmp/go"
  out=$(bash "$self" "$tmp/go" "$GOROOT_ARG" 2>&1); rc=$?
  arms=$((arms+1))
  [ "$rc" -eq 0 ] || { echo "ARM 3 FAILED: apply+verify returned $rc"; echo "$out"; return 1; }
  echo "  ok   apply then verify is GREEN        the whole bill lands and the checker agrees"

  # ARM 4: THE BILL, counted off the patched file rather than asserted by the checker that wrote it.
  # 14 lines change a digit, 6 constant lines appear, 6 strings entries appear, 2 g fields appear.
  arms=$((arms+1))
  local d_changed d_added s_added g_added
  d_changed=$(diff "$SRC" "$tmp/go/runtime/runtime2.cs" | grep -c -E '^< internal static readonly waitReason .* = [0-9]+;')
  d_added=$(diff "$SRC" "$tmp/go/runtime/runtime2.cs" | grep -c -E '^> internal static readonly waitReason .* = [0-9]+;')
  s_added=$(diff "$SRC" "$tmp/go/runtime/runtime2.cs" | grep -c -E '^>     \[waitReason(SyncWaitGroupWait|Synctest[A-Za-z]+)\] = ".*"u8')
  g_added=$(diff "$SRC" "$tmp/go/runtime/runtime2.cs" | grep -c -E '^>     internal (uint8 fipsIndicator|ж<synctestGroup> syncGroup);')
  [ "$d_changed" -eq 14 ] || { echo "ARM 4 FAILED: $d_changed constant lines were rewritten, want exactly 14 (the shift 24..37 -> 25..38)"; return 1; }
  [ "$d_added" -eq 20 ] || { echo "ARM 4 FAILED: $d_added constant lines added, want 20 (14 rewritten + 6 new)"; return 1; }
  [ "$s_added" -eq 6 ] || { echo "ARM 4 FAILED: $s_added waitReasonStrings entries added, want 6"; return 1; }
  [ "$g_added" -eq 2 ] || { echo "ARM 4 FAILED: $g_added g fields added, want 2"; return 1; }
  echo "  ok   the BILL is 14 + 6 + 6 + 2        counted off the diff, not asserted by the writer"

  # ARM 5: the six land at Go's indices, read back from the patched file by NAME.
  arms=$((arms+1))
  local n
  for pair in 'waitReasonSyncWaitGroupWait 24' 'waitReasonSynctestRun 39' 'waitReasonSynctestWait 40' \
              'waitReasonSynctestChanReceive 41' 'waitReasonSynctestChanSend 42' 'waitReasonSynctestSelect 43' \
              'waitReasonTraceReaderBlocked 25' 'waitReasonGCWeakToStrongWait 38' 'waitReasonSyncRWMutexLock 23'; do
    set -- $pair
    n=$(tr -d '\r' < "$tmp/go/runtime/runtime2.cs" | grep -c -E "^internal static readonly waitReason $1 = $2;")
    [ "$n" -eq 1 ] || { echo "ARM 5 FAILED: $1 is not at $2 (matched $n lines)"; return 1; }
  done
  echo "  ok   the boundary values are right     23 unmoved, 24 inserted, 24->25, 37->38, 39..43 new"

  # ARM 6: CRLF survives byte for byte. Counted with tr/wc and compared as INTEGERS -- the string
  # compare this arm used to carry called two FAILED READS equal (i9, a50d4f8c1).
  arms=$((arms+1))
  local cr lf
  cr=$(tr -dc '\r' < "$tmp/go/runtime/runtime2.cs" | wc -c | tr -d '[:space:]')
  lf=$(tr -dc '\n' < "$tmp/go/runtime/runtime2.cs" | wc -c | tr -d '[:space:]')
  case "$cr" in ''|*[!0-9]*) echo "ARM 6 FAILED: CR count is not a number ('$cr') -- the reader failed"; return 1 ;; esac
  case "$lf" in ''|*[!0-9]*) echo "ARM 6 FAILED: LF count is not a number ('$lf') -- the reader failed"; return 1 ;; esac
  [ "$cr" -eq "$lf" ] || { echo "ARM 6 FAILED: CR=$cr LF=$lf -- line endings were normalised"; return 1; }
  echo "  ok   CRLF preserved byte for byte      a normalising rewrite would mask the real change"

  # ARM 7: IDEMPOTENT. The strings table is REBUILT WHOLE, so a second pass is the shape most likely
  # to duplicate rows; assert the counts, not just the exit code.
  out=$(bash "$self" "$tmp/go" "$GOROOT_ARG" 2>&1); rc=$?
  arms=$((arms+1))
  [ "$rc" -eq 0 ] || { echo "ARM 7 FAILED: second apply returned $rc"; echo "$out"; return 1; }
  # ⚠ THE `/* iota */` FORM AGAIN, in my own arm, four hours after i9 corrected the sizing for it
  # (0259e007f): waitReasonZero is spelled `= /* iota */ 0;`, so a pattern demanding `= <digits>`
  # counts 43 where there are 44 -- the same one-short reading, one file over, this time landing as
  # a RED arm on a correct apply rather than as a published number.
  n=$(tr -d '\r' < "$tmp/go/runtime/runtime2.cs" | grep -c -E '^internal static readonly waitReason waitReason[A-Za-z]+ = (/\* iota \*/ )?[0-9]+;')
  [ "$n" -eq 44 ] || { echo "ARM 7 FAILED: re-apply left $n constants, want 44"; return 1; }
  n=$(tr -d '\r' < "$tmp/go/runtime/runtime2.cs" | grep -c -E '^internal static bool isIdleInSynctest\(this waitReason w\) \{')
  [ "$n" -eq 1 ] || { echo "ARM 7 FAILED: re-apply left $n idle accessors, want 1"; return 1; }
  echo "  ok   re-apply is IDEMPOTENT            an H5 rerun cannot double-insert"

  # ARM 8 (RED, floor item 13): ONE constant left un-renumbered must go RED NAMING IT. This is the
  # arm the whole HOLD was about -- the state it detects compiles, links and passes a green suite.
  arms=$((arms+1))
  cp "$tmp/go/runtime/runtime2.cs" "$tmp/restore.cs"
  "$PYBIN" - "$tmp/go/runtime/runtime2.cs" <<'PY'
import io, sys
p = sys.argv[1]
t = io.open(p, encoding='utf-8', newline='').read()
old = 'internal static readonly waitReason waitReasonCoroutine = 37;'
new = 'internal static readonly waitReason waitReasonCoroutine = 36;'
if old not in t:
    raise SystemExit("fixture: the patched file does not carry %r" % old)
io.open(p, 'w', encoding='utf-8', newline='').write(t.replace(old, new, 1))
PY
  rc=$?; [ "$rc" -eq 0 ] || { echo "ARM 8 FAILED: could not regress the fixture (rc=$rc)"; return 1; }
  out=$(bash "$self" --verify "$tmp/go" "$GOROOT_ARG" 2>&1); rc=$?
  [ "$rc" -eq 1 ] || { echo "ARM 8 FAILED: a constant left at its 1.23 value PASSED (rc=$rc)"; echo "$out"; return 1; }
  case "$out" in *"waitReasonCoroutine corpus=36 go=37"*) ;; *) echo "ARM 8 FAILED: red, but did not NAME the constant"; echo "$out"; return 1 ;; esac
  cp "$tmp/restore.cs" "$tmp/go/runtime/runtime2.cs"
  out=$(bash "$self" --verify "$tmp/go" "$GOROOT_ARG" 2>&1); rc=$?
  [ "$rc" -eq 0 ] || { echo "ARM 8 FAILED: the restore did not return the file to green"; echo "$out"; return 1; }
  echo "  ok   ONE stale constant goes RED       named, on a file that compiles and tests green"

  # ARM 9 (RED): drop one strings entry -- C2's hole (2a6938f4b). String() would silently return
  # "unknown wait reason" for it; nothing in a build can see that either.
  arms=$((arms+1))
  "$PYBIN" - "$tmp/go/runtime/runtime2.cs" <<'PY'
import io, sys
p = sys.argv[1]
t = io.open(p, encoding='utf-8', newline='').read()
line = '    [waitReasonSynctestWait] = "synctest.Wait"u8,\r\n'
if line not in t:
    raise SystemExit("fixture: %r not present" % line)
io.open(p, 'w', encoding='utf-8', newline='').write(t.replace(line, '', 1))
PY
  rc=$?; [ "$rc" -eq 0 ] || { echo "ARM 9 FAILED: could not regress the strings table (rc=$rc)"; return 1; }
  out=$(bash "$self" --verify "$tmp/go" "$GOROOT_ARG" 2>&1); rc=$?
  [ "$rc" -eq 1 ] || { echo "ARM 9 FAILED: a missing strings entry PASSED (rc=$rc)"; echo "$out"; return 1; }
  case "$out" in *"missing [waitReasonSynctestWait]"*) ;; *) echo "ARM 9 FAILED: red, but did not name the entry"; echo "$out"; return 1 ;; esac
  case "$out" in *"materialise dense"*) ;; *) echo "ARM 9 FAILED: did not report the density loss"; echo "$out"; return 1 ;; esac
  cp "$tmp/restore.cs" "$tmp/go/runtime/runtime2.cs"
  echo "  ok   a MISSING strings row goes RED    C2's hole: String() would say 'unknown wait reason'"

  # ARM 10 and ARM 16 (RED): the DECLARED LENGTH, one table at a time. COORD 486a3926a ruled the arm
  # ("a regressed fixture, one table bare, going RED naming the table"), and it runs SEPARATELY per
  # table with a restore between -- regressing both at once would prove only that the first check is
  # reached, which is the vacuous-control shape C2 caught in the C1-1 suite (a2b892aef, arm 15 there).
  #
  # Note what is NO LONGER asserted: the idle table's TOP KEY. With the length passed explicitly the top
  # key does not size the table any more, and keeping that assertion would be an arm whose name outlived
  # its meaning -- this file's own recurring failure. The key SET is still checked one screen up.
  for pair in 'IdleInSynctest waitReasonSynctestSelect' 'WaitingForSuspendG waitReasonFlushProcCaches'; do
    tbl=${pair%% *}; lastkey=${pair##* }
    arms=$((arms+1))
    TBL=$tbl LASTKEY=$lastkey "$PYBIN" - "$tmp/go/runtime/runtime2.cs" <<'PYARM'
import io, os, sys
p = sys.argv[1]
tbl, lastkey = os.environ['TBL'], os.environ['LASTKEY']
t = io.open(p, encoding='utf-8', newline='').read()
key = '    [%s] = true\r\n' % lastkey
if key + '}.array(' not in t:
    raise SystemExit("fixture: no closer directly after [%s] for %s" % (lastkey, tbl))
i = t.index(key + '}.array(') + len(key)
j = t.index('\r\n', i) + 2
if not t[i:j].startswith('}.array(4'):
    raise SystemExit("fixture: %s does not close with an explicit length (%r)" % (tbl, t[i:j]))
io.open(p, 'w', encoding='utf-8', newline='').write(t[:i] + '}.array();\r\n' + t[j:])
PYARM
    rc=$?; [ "$rc" -eq 0 ] || { echo "ARM (bare $tbl) FAILED: could not regress the fixture (rc=$rc)"; return 1; }
    out=$(bash "$self" --verify "$tmp/go" "$GOROOT_ARG" 2>&1); rc=$?
    [ "$rc" -eq 1 ] || { echo "ARM (bare $tbl) FAILED: a BARE closer PASSED (rc=$rc)"; echo "$out"; return 1; }
    case "$out" in *"Δis$tbl closes with a BARE"*) ;; *) echo "ARM (bare $tbl) FAILED: red, but did not name THIS table"; echo "$out"; return 1 ;; esac
    cp "$tmp/restore.cs" "$tmp/go/runtime/runtime2.cs"
    out=$(bash "$self" --verify "$tmp/go" "$GOROOT_ARG" 2>&1); rc=$?
    [ "$rc" -eq 0 ] || { echo "ARM (bare $tbl) FAILED: the restore did not return the file to green"; echo "$out"; return 1; }
    echo "  ok   a BARE Δis$tbl goes RED   the length is DECLARED, never inferred from a top key"
  done

  # ARM 11 (RED): drop a g field. Unlike everything above, THIS one a build can see -- which is
  # exactly why it is worth pinning that the checker sees it too, and inside struct g.
  arms=$((arms+1))
  "$PYBIN" - "$tmp/go/runtime/runtime2.cs" <<'PY'
import io, sys
p = sys.argv[1]
t = io.open(p, encoding='utf-8', newline='').read()
line = '    internal ж<synctestGroup> syncGroup;\r\n'
if line not in t:
    raise SystemExit("fixture: %r not present" % line)
io.open(p, 'w', encoding='utf-8', newline='').write(t.replace(line, '', 1))
PY
  rc=$?; [ "$rc" -eq 0 ] || { echo "ARM 11 FAILED: could not regress struct g (rc=$rc)"; return 1; }
  out=$(bash "$self" --verify "$tmp/go" "$GOROOT_ARG" 2>&1); rc=$?
  [ "$rc" -eq 1 ] || { echo "ARM 11 FAILED: a missing g field PASSED (rc=$rc)"; echo "$out"; return 1; }
  case "$out" in *"struct g is missing"*syncGroup*) ;; *) echo "ARM 11 FAILED: red, but did not name the field"; echo "$out"; return 1 ;; esac
  cp "$tmp/restore.cs" "$tmp/go/runtime/runtime2.cs"
  echo "  ok   a MISSING g field goes RED        and it is looked for INSIDE struct g"

  # ARM 12 (RED): the two omissions must stay RECORDED. Strip the mWaitList note and the checker
  # must refuse the file -- an undocumented omission is indistinguishable from an oversight.
  arms=$((arms+1))
  "$PYBIN" - "$tmp/go/runtime/runtime2.cs" <<'PY'
import io, re, sys
p = sys.argv[1]
t = io.open(p, encoding='utf-8', newline='').read()
if 'mWaitList mWaitList' not in t:
    raise SystemExit("fixture: the mWaitList note is not present")
t = re.sub(r'    // go1\.24 RENAMES this field.*?Measured 2026-09-13 \(C1-2[^)]*\)\.\r\n', '', t, flags=re.S)
io.open(p, 'w', encoding='utf-8', newline='').write(t)
PY
  rc=$?; [ "$rc" -eq 0 ] || { echo "ARM 12 FAILED: could not strip the note (rc=$rc)"; return 1; }
  out=$(bash "$self" --verify "$tmp/go" "$GOROOT_ARG" 2>&1); rc=$?
  [ "$rc" -eq 1 ] || { echo "ARM 12 FAILED: a stripped omission note PASSED (rc=$rc)"; echo "$out"; return 1; }
  case "$out" in *"no note about mWaitList"*) ;; *) echo "ARM 12 FAILED: red, but not for the missing note"; echo "$out"; return 1 ;; esac
  cp "$tmp/restore.cs" "$tmp/go/runtime/runtime2.cs"
  echo "  ok   a STRIPPED omission note goes RED the reasons are part of the deliverable"

  # ARM 13 (REFUSAL): an EMPTY extraction must abort rather than compare. This is C1's own near-miss
  # made into an arm -- the reading that printed "IDENTICAL, no renumbering" was two empty sets.
  arms=$((arms+1))
  "$PYBIN" - "$GOSRC" "$tmp/empty.go" <<'PY'
import io, re, sys
src = io.open(sys.argv[1], encoding='utf-8', newline='').read().replace('\r\n', '\n')
# empty the waitReason const block, leaving its shape intact -- the exact silhouette of an awk range
# that opens and closes on the wrong lines
out = re.sub(r'(^type waitReason uint8$\n\nconst \(\n).*?(^\)$)',
             r'\1\t// emptied by the self-test fixture\n\2', src, flags=re.M | re.S)
if out == src:
    raise SystemExit("fixture: could not empty the const block")
io.open(sys.argv[2], 'w', encoding='utf-8', newline='').write(out)
PY
  rc=$?; [ "$rc" -eq 0 ] || { echo "ARM 13 FAILED: could not build the empty-block fixture (rc=$rc)"; return 1; }
  mkdir -p "$tmp/fakeroot/src/runtime"; cp "$tmp/empty.go" "$tmp/fakeroot/src/runtime/runtime2.go"
  out=$(bash "$self" --verify "$tmp/go" "$tmp/fakeroot" 2>&1); rc=$?
  [ "$rc" -eq 2 ] || { echo "ARM 13 FAILED: an EMPTY Go extraction did not refuse (rc=$rc)"; echo "$out"; return 1; }
  case "$out" in *"BROKEN READER"*) ;; *) echo "ARM 13 FAILED: refused without naming the empty read"; echo "$out"; return 1 ;; esac
  case "$out" in *POST-CONDITION*) echo "ARM 13 FAILED: an empty extraction produced a VERDICT"; echo "$out"; return 1 ;; esac
  echo "  ok   an EMPTY extraction REFUSES       two empty sets would have reported agreement"

  # ARM 14 (REFUSAL): a pre-1.24 Go source must refuse NAMING the discriminator, rather than apply
  # a six-item bill and call it done. Derived from the real source by deleting the 1.24 table.
  arms=$((arms+1))
  "$PYBIN" - "$GOSRC" "$tmp/old.go" <<'PY'
import io, re, sys
src = io.open(sys.argv[1], encoding='utf-8', newline='').read().replace('\r\n', '\n')
out = re.sub(r'\nfunc \(w waitReason\) isIdleInSynctest\(\) bool \{.*?\n\}\n', '\n', src, flags=re.S)
out = re.sub(r'\n// isIdleInSynctest indicates.*?\nvar isIdleInSynctest = .*?\n\}\n', '\n', out, flags=re.S)
if 'isIdleInSynctest' in out:
    raise SystemExit("fixture: isIdleInSynctest survived the deletion")
io.open(sys.argv[2], 'w', encoding='utf-8', newline='').write(out)
PY
  rc=$?; [ "$rc" -eq 0 ] || { echo "ARM 14 FAILED: could not build the 1.23-shaped fixture (rc=$rc)"; return 1; }
  mkdir -p "$tmp/oldroot/src/runtime"; cp "$tmp/old.go" "$tmp/oldroot/src/runtime/runtime2.go"
  out=$(bash "$self" --verify "$tmp/go" "$tmp/oldroot" 2>&1); rc=$?
  [ "$rc" -eq 2 ] || { echo "ARM 14 FAILED: a pre-1.24 GOROOT did not refuse (rc=$rc)"; echo "$out"; return 1; }
  case "$out" in *"older than the hop target"*) ;; *) echo "ARM 14 FAILED: refused without naming the version"; echo "$out"; return 1 ;; esac
  echo "  ok   a PRE-1.24 GOROOT is REFUSED      derived discriminator, not a typed list of names"

  # ARM 15 (REFUSAL): the tool gate. A probe-passing no-op must not be accepted as an interpreter --
  # carried from C1-1, where /bin/echo passed a status-only probe (C2, a2b892aef).
  arms=$((arms+1))
  out=$(H5_PYTHON=/bin/echo bash "$self" --verify "$tmp/go" "$GOROOT_ARG" 2>&1); rc=$?
  [ "$rc" -eq 2 ] || { echo "ARM 15 FAILED: /bin/echo was accepted as an interpreter (rc=$rc)"; echo "$out"; return 1; }
  case "$out" in *"did not answer 'print(6*7)' with 42"*) ;; *) echo "ARM 15 FAILED: refused for the wrong reason"; echo "$out"; return 1 ;; esac
  case "$out" in *POST-CONDITION*) echo "ARM 15 FAILED: a refused interpreter still produced a VERDICT"; echo "$out"; return 1 ;; esac
  echo "  ok   a probe-passing NO-OP is REFUSED  exit 0 is not evidence that the work was done"

  # ARM 16 (DETECTION, both directions): an interpreter whose correct answer carries a CARRIAGE
  # RETURN must be ACCEPTED, and the tolerance must not have widened into accepting a wrong answer.
  # This is the Windows `py` defect measured on G's box -- `py.exe` is a native Windows program and
  # answers `42\r\n` through a Git-Bash pipe, so the old comparison saw `42\r` and refused a working
  # interpreter while `python3` and `python` were Store redirectors with nothing to offer. The arm
  # tests py_answers DIRECTLY, because that function IS the unit that was wrong; ARM 15 already
  # covers the end-to-end refusal path.
  arms=$((arms+1))
  printf '#!/bin/sh\nprintf "42\\r\\n"\n' > "$tmp/cr-answer"; chmod +x "$tmp/cr-answer"
  printf '#!/bin/sh\nprintf "43\\r\\n"\n' > "$tmp/cr-wrong";  chmod +x "$tmp/cr-wrong"
  py_answers "$tmp/cr-answer" || { echo "ARM 16 FAILED: an interpreter answering 42 with a trailing CR was REFUSED -- this is the Windows 'py' defect, unfixed"; return 1; }
  py_answers "$tmp/cr-wrong"  && { echo "ARM 16 FAILED: the CR tolerance widened into accepting a WRONG answer (43)"; return 1; }
  py_answers /bin/echo        && { echo "ARM 16 FAILED: a probe-passing no-op was accepted after the CR change"; return 1; }
  echo "  ok   a CR-carrying ANSWER is ACCEPTED  Windows 'py' answers 42 CRLF; 43 and /bin/echo still refused"

  echo
  echo "SELF-TEST CLEAN -- $arms arms, $notrun not run"
  return 0
}

GOROOT_ARG=""
case "${1:-}" in
  --self-test) GOROOT_ARG=${2:-}; selftest "$GOROOT_ARG"; exit $? ;;
  --verify)
    CORE=${2:-}; [ -n "$CORE" ] || die "usage: --verify <scratch-core-dir> [<goroot>]"
    GOROOT_ARG=${3:-}
    resolve_python
    resolve_goroot "$GOROOT_ARG"
    [ -f "$CORE/$R2" ] || die "no $R2 under $CORE -- not a corpus root"
    echo "== verifying $CORE against $GOSRC"
    verify "$CORE"; VRC=$?
    # ⚠ A REFUSAL IS NOT A VERDICT, and the first cut of this dispatch turned one into the other:
    # `if verify; then MET; else FAILED; fi` reported "POST-CONDITION FAILED" over a run that had
    # REFUSED to read its own inputs (an emptied Go const block). That is the shape a reader acts on
    # -- they go looking at the corpus -- when the instrument never got far enough to have an opinion.
    # Found by arm 13, which asserts that an empty extraction produces no verdict at all.
    case $VRC in
      0) echo "==> POST-CONDITION MET"; exit 0 ;;
      1) echo "==> POST-CONDITION FAILED"; exit 1 ;;
      *) echo "==> NO VERDICT -- the run REFUSED above; nothing was measured"; exit "$VRC" ;;
    esac ;;
  ''|-*) die "usage: apply-h5-c1-2-member-bill.sh <scratch-core-dir> [<goroot>] | --verify <dir> [<goroot>] | --self-test" ;;
esac

CORE=$1
GOROOT_ARG=${2:-}
# Both gates run BEFORE any edit, so a refusal means the tree was not touched -- and means the run
# cannot reach the line that says APPLIED.
resolve_python
resolve_goroot "$GOROOT_ARG"
check_precondition "$CORE"
echo "== applying the C1-2 member bill to $CORE (edits via $PYBIN, table from $GOSRC)"
apply "$CORE"
echo "== verifying"
verify "$CORE"; VRC=$?
case $VRC in
  0) echo "==> APPLIED and POST-CONDITION MET"; exit 0 ;;
  1) echo "==> APPLIED but the POST-CONDITION FAILED -- read the FAIL lines above"; exit 1 ;;
  *) echo "==> APPLIED, but the CHECK REFUSED above -- the apply is NOT confirmed"; exit "$VRC" ;;
esac
