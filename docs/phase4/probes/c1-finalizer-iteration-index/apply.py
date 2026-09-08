#!/usr/bin/env python3
"""Iteration-index instrument for runtime's TestFinalizerType (item 4, commit 2b).

WHAT IT MEASURES. TestFinalizerType loops six (object, finalizer) shapes; each
iteration registers a finalizer, calls runtime.GC(), then BLOCKS on a receive.
So the row does not fail, it STOPS: the FIRST shape whose finalizer never runs
holds the receive forever and the package deadline kills the host with ZERO
converted verdicts. Which iteration never delivers NAMES that shape.

WHY IT IS A PATCH AND NOT A COMMITTED EDIT. `runtime` is UNBANKED, so its test
emission is git-ignored and not in the tree; there is nothing to commit an edit
to, and committing one would bank an unbanked row's test sources. This is the
itemised, unbanked patch shape: apply it to the EMITTED file between convert
and build.

⚠ ORDER MATTERS, AND THIS BLOCK SAID THE WRONG THING UNTIL 2026-09-08. Both
`-test-action all` and `-test-action compare` RE-CONVERT every non-marked corpus
file before building, which wipes this patch and makes the probe read nothing
while looking healthy -- so `compare` is NOT safe here, and an earlier version of
this very docstring listed it as an option beside `run`. Settled at the converter
source rather than at the case labels (testConversion.go:6103-6115): `case "run"`
publishes and executes with no convert anywhere, while `case "compare", "all"`
share the re-converting path.

⚠ AND `-test-action run` IGNORES `-test-filter` (i9, measured): the host is
invoked with only --json and -timeout, and testFilter is consumed on the compare
path alone. The runbook used to ask for BOTH, which cannot hold. Drive the
PUBLISHED HOST directly instead -- it accepts -run itself, keeps the patch, and
needs no converter at all. GOROOT must be set or the host refuses before parsing
its arguments.

    go2cs -tests -test-action convert  <goroot>/src/runtime  <out>/src/core/runtime
    python apply.py                    <out>/src/core/runtime/mfinal_test.cs
    go2cs -tests -test-action build    <goroot>/src/runtime  <out>/src/core/runtime
    <published host> --json -run '^TestFinalizerType$' -timeout 5m  2> run.log
    python apply.py --verify <path>        # the PATCH survived
    python apply.py --ran    run.log       # the TEST actually RAN

⚠ THOSE LAST TWO ARE DIFFERENT QUESTIONS AND THE SECOND ONE IS WHY THIS ARM
EXISTS. `--verify` reads the FILE and answers "did a re-convert wipe the patch".
It CANNOT see a run that never reached the test: i9 drove an unfiltered `run`
that died at 25 s before TestFinalizerType, and `--verify` still said "patch
intact" over a reading that measured nothing. `--ran` reads the RUN OUTPUT and
answers "did the patched code execute".

⚠ `python3` MAY NOT BE AN INTERPRETER. On Windows it can be a Store alias that
prints an install advert and exits 0, while `python` is real. Following this
runbook literally with `python3` there gives no patch, no recognisable error, and
then a `--verify` of 0 that looks exactly like the re-convert trap -- two causes,
one symptom.

WHY println AND NOT t.Logf. The failure mode being measured is a HANG killed by
the package deadline, and the host buffers t.Logf; println goes to stderr
unbuffered and survives the kill. Verified call shape: golib builtin.cs:2279
`public static void println(params object[] args)`, called across runtime as
`println((@string)"..."u8, args)`.

NO GLYPH LITERALS. Every anchor is matched by SHAPE. The converter mints
identifiers like the loop's value variable, and retyping one by hand is how the
first draft of this script failed -- the wrong superscript character matched
zero times.
"""
import re
import sys

MARK = "c1-iterindex"
IDX = "iᴛc1"          # INTRODUCED by this script -- the range loop discards its index as
                      # `_` and the substitution below names it, so this identifier has to
                      # match nothing in the emission. The comment here used to claim it was
                      # "derived from the emission's own naming", which was simply false.


FUNC = "public static void TestFinalizerType("


def slice_function(text):
    """Patch INSIDE TestFinalizerType only.

    The done/GC/ch shape occurs TWICE in mfinal_test.cs, so a file-wide anchor is
    ambiguous -- the first version of this script refused on exactly that, which
    is the guard working rather than a bug to relax. Scope, do not loosen.
    """
    start = text.find(FUNC)
    if start < 0:
        raise SystemExit("TestFinalizerType not found -- refusing")
    if text.count(FUNC) != 1:
        raise SystemExit("TestFinalizerType is not unique -- refusing")
    end = text.find("\n}\n", start)
    if end < 0:
        raise SystemExit("could not find the end of TestFinalizerType -- refusing")
    return start, end + 3


def patch(whole):
    lo, hi = slice_function(whole)
    text = whole[lo:hi]
    n = 0
    # 1. the loop: give the discarded index a name. Shape-matched, no glyphs.
    loop = re.compile(r"foreach \(var \(_, (\S+)\) in finalizerTests\) \{")
    m = loop.search(text)
    if not m:
        raise SystemExit("ANCHOR 1 (the finalizerTests loop) not found -- refusing")
    if len(loop.findall(text)) != 1:
        raise SystemExit("ANCHOR 1 is not unique -- refusing")
    text = loop.sub(lambda mm: mm.group(0).replace("(_, ", f"({IDX}, ", 1), text, count=1)
    n += 1

    # 2. the per-iteration block: <-done ; runtime.GC() ; <-ch . Shape-matched.
    blk = re.compile(
        r"(\n([ \t]*))(\S+\(done\);)"
        r"(\n[ \t]*)(\S+\.GC\(\);)"
        r"(\n[ \t]*)(\S+\(ch\);)"
    )
    if len(blk.findall(text)) != 1:
        raise SystemExit("ANCHOR 2 (done/GC/ch) not found or not unique -- refusing")

    def ins(mm):
        nl, _ind, done, nl2, gc, nl3, recv = mm.groups()
        q = chr(34)                       # the C# string literal's own quote

        def log(msg):
            # Emits, with the marker text, the index name and the message substituted:
            #     println((@string)"MARK idx"u8, idx, (@string)"shape"u8, idx + 1,
            #             (@string)"msg"u8);
            # (spelled without angle brackets on purpose -- an uppercase token inside them
            # is indistinguishable from an unfilled template marker to a completeness gate,
            # and this file's own gate fired on the previous wording)
            #
            # NO %d. The converted println does NOT substitute a format -- it prints its
            # arguments separated by spaces, exactly as Go's println does -- so the first
            # version of this line emitted the verb LITERALLY with the number trailing the
            # whole sentence. The reading was unambiguous that time and one edit away from
            # not being (i9, 2026-09-08).
            #
            # BOTH NUMBERS, because a probe that prints one base and a runbook written in
            # the other is how a HIT gets scored as a MISS: the loop index is 0-based and
            # the shapes are numbered from 1, so index 2 IS shape 3.
            #
            # nl already carries the line's indent; adding ind again doubles it.
            return (nl + "println((@string)" + q + MARK + " idx" + q + "u8, " + IDX
                    + ", (@string)" + q + "shape" + q + "u8, " + IDX + " + 1"
                    + ", (@string)" + q + msg + q + "u8);")

        return (nl + done
                + log("registered, calling GC")
                + nl2 + gc
                + log("GC returned, waiting on ch")
                + nl3 + recv
                + log("DELIVERED"))

    text = blk.sub(ins, text, count=1)
    n += 1
    return whole[:lo] + text + whole[hi:], n


def main():
    if sys.argv[1:2] == ["--verify"]:
        raw = open(sys.argv[2], "rb").read()
        c = raw.decode("utf-8").count(MARK)
        print(f"marker occurrences: {c}")
        if c == 0:
            raise SystemExit("VOID -- the pipeline re-converted over the patch; the reading means nothing")
        print("patch intact")
        return

    if sys.argv[1:2] == ["--ran"]:
        # A DIFFERENT QUESTION FROM --verify. That one reads the FILE and answers "did a
        # re-convert wipe the patch"; this one reads the RUN OUTPUT and answers "did the
        # patched code execute at all". An unfiltered run that dies before reaching
        # TestFinalizerType leaves the patch perfectly intact, so --verify passes over a
        # reading that measured NOTHING -- which is what happened on 2026-09-08.
        raw = open(sys.argv[2], "rb").read()
        lines = [ln for ln in raw.decode("utf-8", "replace").splitlines() if MARK in ln]
        print(f"marker lines in the run output: {len(lines)}")
        if not lines:
            raise SystemExit(
                "NOT MEASURED -- the patched code never executed. The test was not reached "
                "(an unfiltered run, a build that did not publish, a host that refused). "
                "This is NOT the same as a wiped patch; check --verify separately.")
        for ln in lines:
            print("  " + ln.strip())
        return

    p = sys.argv[1]
    raw = open(p, "rb").read()
    if raw.count(b"\n") != raw.count(b"\r\n"):
        raise SystemExit("target is not uniformly CRLF -- refusing")
    out, n = patch(raw.decode("utf-8").replace("\r\n", "\n"))
    if out.count(MARK) != 3:
        raise SystemExit(f"expected 3 markers, produced {out.count(MARK)} -- refusing")
    open(p, "wb").write(out.replace("\n", "\r\n").encode("utf-8"))
    back = open(p, "rb").read()
    if back.count(b"\n") != back.count(b"\r\n"):
        raise SystemExit("introduced a bare LF -- refusing")
    print(f"applied {n} anchors, 3 markers, CRLF uniform")


main()
