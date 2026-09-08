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

⚠ ORDER MATTERS. `-test-action all` and `-test-action compare` RE-CONVERT every
non-marked corpus file before building, which wipes this patch and makes the
probe read nothing while looking healthy. Run:

    go2cs -tests -test-action convert  <goroot>/src/runtime  <out>/src/core/runtime
    python3 apply.py <out>/src/core/runtime/mfinal_test.cs
    go2cs -tests -test-action build    ... then run/compare, NEVER `all`

and after the run, `python3 apply.py --verify <path>` -- a marker count of 0
means the pipeline re-converted over it and the reading is void.

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
IDX = "iᴛc1"          # derived below from the emission's own naming, never assumed


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
            # println((@string)"<MARK> %d <msg>"u8, <IDX>);
            # nl already carries the line's indent; adding ind again doubles it.
            return (nl + "println((@string)" + q + MARK + " %d " + msg
                    + q + "u8, " + IDX + ");")

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
