#!/usr/bin/env python3
"""C1's recon re-classifier: a committed evidence record -> the row's banked word and verdict count.

Every rule is one COORD ruled, cited beside it. It reads RECORDS, never a lane's `diverged` column:
the fifth wrapper blob rewrote every real 0 to `n/a` (PowerShell's `0 -eq ''` is TRUE), so that column
is uninformative for the fifth-blob lanes and the count must come from the document.
"""
import sys, os, json, io, re

# ⚠ DUPLICATE-KEY REGRESSION GUARD (COORD da3c01f2f (1), restated). Python's json keeps the LAST of a
# repeated key silently, so a document with two `TestFoo` entries loses one verdict with nothing said.
# This is a GUARD against a regression, not a demonstration: no committed document has one today, and
# its red is a PLANTED document carrying a genuine duplicate.
def no_dup_pairs(pairs):
    seen = {}
    for k, v in pairs:
        if k in seen:
            raise ValueError(f"duplicate key {k!r} in one object -- a verdict would be lost silently")
        seen[k] = v
    return seen


def strip_banner(text):
    """⚠ The seventh/eighth blob PREPEND a hash line to a results tail (C2 21012bd5e). A fifth-blob
    tail parses as one JSON document and an eighth-blob tail does not, and G relaunching mid-list
    guarantees BOTH shapes in one corpus -- so the tail is read as TEXT, and a leading hash line is
    stripped before any parse is attempted. Returns (text, stripped?)."""
    if text.startswith("#"):
        nl = text.find("\n")
        return (text[nl + 1:] if nl >= 0 else ""), True
    return text, False


def row_tail_segment(row):
    """CLAUSE (c) (COORD da3c01f2f (2)): the record's `package` equals the row's LAST path segment,
    or the one BEFORE it when the last is `vN` -- `math/rand/v2` renders `rand`. The field is a
    sanity check; the evidence DIRECTORY plus the run window is the provenance (G 343b59ddc0)."""
    parts = [p for p in row.split("/") if p]
    if not parts:
        return ""
    if len(parts) >= 2 and re.fullmatch(r"v\d+", parts[-1]):
        return parts[-2]
    return parts[-1]


def clause_c_tail(row, tail_path):
    """CLAUSE (c) ON A RESULTS TAIL (COORD 7ae9f1ca4): the tail's `package` is the FULL row name,
    EXACTLY -- not the last segment. Measured on R's twelve: the two documents spell this field
    differently and coincide only where the row has ONE segment (fmt, syscall), 2 of 12. The
    last-segment rule is the COMPARISON record's and refuses ten of twelve here; applying either
    rule to the other document is the shape that made this clause move three times."""
    raw = io.open(tail_path, encoding="utf-8").read()
    plain, _ = strip_banner(raw)
    try:
        doc = json.loads(plain, object_pairs_hook=no_dup_pairs)
    except ValueError:
        return "unparsed"
    pkg = doc.get("package", "")
    return "ok" if pkg == row else f"MISMATCH(want {row!r}, got {pkg!r})"


# ⚠ A `disclosed` ENTRY IS A SENTENCE, NOT A NAME (i9 64d873bb2, measured on the committed
# projections; the wrapper carries the same comment from `bufio` when the fifth blob was cut). It
# reads `TestCertCache (codegen-liveness): The test nils its own local and then asserts …` -- so the
# NAME is the LEADING TOKEN and a membership test against the whole string matches NOTHING and
# subtracts NOTHING, leaving every disclosed divergence in the count.
#
# This function read `n not in set(disclosed)` until 2026-09-20 and over-counted `diverged` by
# exactly the disclosed-entry count on every row that had one: crypto/tls 13 for 12, net 3 for 1,
# runtime/pprof 37 for its own. `net/http` agreed at 19 and was the control that made the shape
# legible -- it is the one row of the three with NOTHING to subtract.
#
# `len(disclosed)` was always right, which is why `verdicts` matched i9's figure on all three rows
# while `diverged` did not: the COUNT of entries is the count of names, and only the MATCH was wrong.
def disclosed_name(entry):
    """The test name a disclosed entry names: its leading whitespace-delimited token. A bare name
    (no explanation) returns itself, so this is correct for both shapes."""
    return entry.split(None, 1)[0] if entry.split(None, 1) else ""


def classify(row, path):
    raw = io.open(path, encoding="utf-8").read()
    doc = json.loads(raw, object_pairs_hook=no_dup_pairs)

    go = doc.get("go") or {}
    cs = doc.get("csharp") or {}
    disclosed = doc.get("disclosed") or []

    dset = {disclosed_name(entry) for entry in disclosed}

    # ⚠ A SET DIFFERENCE, NOT A COUNT DIFFERENCE (COORD 22d3b01e1 §2, on C1 b612bfa1c §3). The ruled
    # formula `len(go) - len(disclosed)` assumes `disclosed` is a SUBSET of `go`, which holds on every
    # record here but ONE: `runtime/pprof`'s six entries are `host-fatal` and name tests its `go` map
    # never carried at all, so the count form subtracted six names that were never among the 161 and
    # read 155. Subtracting a name that is not there must subtract nothing. Identical to the count
    # form wherever the subset holds, which is everywhere else.
    verdicts = len(set(go) - dset)

    # ⚠ SAID OUT LOUD rather than silently subtracting nothing, which is the whole defect above: a
    # derived name the record's own `go` map does not carry means the leading-token rule did not fit
    # this document, and the count that follows is not the ruled quantity.
    unresolved = sorted(n for n in dset if n not in go)

    diverged = [n for n, g in go.items() if n not in dset and cs.get(n) != g]

    pkg = doc.get("package", "")
    want = row_tail_segment(row)

    return {
        "row": row,
        "verdicts": verdicts,
        "go": len(go),
        "cs": len(cs),
        "disclosed": len(disclosed),
        "diverged": len(diverged),
        "matched": doc.get("matched"),
        "status": doc.get("status", ""),
        "package": pkg,
        "unresolvedDisclosed": unresolved,
        "clause_c": "ok" if pkg == want else f"MISMATCH(want {want!r})",
        # ⚠ THE TELL (COORD 000dc3b5f (5)): a row whose NET UNDISCLOSED set equals its WHOLE verdict
        # count is what a side that never ran looks like. A DETECTOR, never a decider -- it NAMES the
        # row and COORD rules it.
        "tell": (verdicts > 0 and len(diverged) == verdicts),
    }


if __name__ == "__main__":
    root = sys.argv[1]
    rows = []
    for dirpath, _, files in os.walk(root):
        if "go2cs_test_comparison.json" not in files:
            continue
        row = os.path.relpath(dirpath, root).replace(os.sep, "/")
        try:
            r = classify(row, os.path.join(dirpath, "go2cs_test_comparison.json"))
            tail = os.path.join(dirpath, "results-tail.txt")
            r["clause_c_tail"] = clause_c_tail(row, tail) if os.path.exists(tail) else "(no tail)"
            rows.append(r)
        except ValueError as e:
            print(f"  {row:30s} REFUSED: {e}")
    print(f"  {'row':30s} {'verd':>5} {'go':>5} {'disc':>5} {'div':>5} {'match':>6}  {'status':11s} {'(c)/cmp':10s} {'(c)/tail':12s} tell")
    for r in sorted(rows, key=lambda x: x["row"]):
        print(f"  {r['row']:30s} {r['verdicts']:5d} {r['go']:5d} {r['disclosed']:5d} {r['diverged']:5d} "
              f"{str(r['matched']):>6}  {r['status']:11s} {r['clause_c']:10s} {r['clause_c_tail']:12s} {'⚠ NAMED' if r['tell'] else ''}")
    # ⚠ Never a silent pass: a disclosed entry whose derived name is absent from `go` means the
    # leading-token rule did not fit that document, and the subtraction it feeds is not the ruled one.
    for r in sorted(rows, key=lambda x: x["row"]):
        if r["unresolvedDisclosed"]:
            print(f"  ⚠ {r['row']}: {len(r['unresolvedDisclosed'])} disclosed entr(ies) name a test the "
                  f"record's `go` map does not carry: {', '.join(r['unresolvedDisclosed'])}")
