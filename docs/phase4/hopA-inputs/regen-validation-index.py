#!/usr/bin/env python3
"""Regenerate docs/validation/index.md's CURRENT table from the roster and the proof pages.

RULING (2) of the H10 driver's brief (COORD, mailbox 0cb09c354): docs/validation/index.md is
EXCLUDED from every shard ref and regenerated ONCE centrally after the last leg, with its row
count asserted against the roster. Nothing did that; this does.

WHAT IT REGENERATES, AND WHAT IT DOES NOT. Only the CURRENT table is derived — one row per
banked roster package, and the row is a pure function of the package name. Everything else in
the page is kept VERBATIM from the committed file:

  * the prose header,
  * the FROZEN SNAPSHOTS table, whose own text says the counts "are exact and cannot go stale:
    a frozen directory is written once and never rewritten". Deriving those from today's tree
    would replace a permanent statement with a re-measurement, which is the opposite of what a
    frozen snapshot is. They are history, not state.

So this is a regeneration of the page's DERIVED half, and the undeviated half is copied rather
than reproduced. Said plainly because "regenerate index.md" could reasonably mean either.

REFUSALS, each by name and before anything is written:
  * a roster package with no proof page under current/,
  * a proof page under current/ with no roster row (the reverse, which a count alone hides),
  * a roster that parses to zero rows (an instrument that reads nothing reports a clean page).

THE COMPARISON IS A SET, NOT A DIFF. The committed table and the regenerated one are compared
as SETS of package names first, and only then for ORDER. A set difference is a real
disagreement about which packages are validated; an order difference is a sorting question and
says nothing about content. Reporting them together is how a re-sort reads as a regression.

Usage:
  regen-validation-index.py                 verify: regenerate, compare, report, write nothing
  regen-validation-index.py --write         write the regenerated page over the committed one
  regen-validation-index.py --selftest      the arms, in a temp dir of its own
"""

import re
import sys
import shutil
import tempfile
from pathlib import Path

HERE = Path(__file__).resolve().parent
REPO = HERE.parent.parent.parent                       # docs/phase4/hopA-inputs -> repo root
ROSTER = REPO / "docs" / "ValidatedTestPackages.md"
VALDIR = REPO / "docs" / "validation"
TREE_URL = "https://github.com/ritchiecarroll/go2cs/tree/master/src/core"

# A roster row names its package in a link whose text is the import path.  The proof-page name is
# that path with '/' -> '.', which is the converter's own convention for these files.
ROSTER_ROW = re.compile(r"^\| \[`([^`]+)`\]")
INDEX_ROW = re.compile(r"^\| `([^`]+)`")
TABLE_HEAD = "| Package | Proof | Converted package |"


def die(msg):
    raise SystemExit(f"regen-validation-index: REFUSED -- {msg}")


def shown(path):
    """A path for a human, relative to the repo when it IS in the repo.

    ⚠ `relative_to` RAISES for a path outside its argument, and the arms build fixtures in a temp
    directory — so the first run of the self-test died here rather than reporting a verdict. A
    display helper that can abort the run it is describing is a defect, and it was found by the
    arms rather than by reading, which is the whole reason they exist.
    """
    try:
        return path.relative_to(REPO)
    except ValueError:
        return path


def page_name(pkg):
    return pkg.replace("/", ".") + ".md"


def index_row(pkg):
    return (f"| `{pkg}` | [`{page_name(pkg)}`](current/{page_name(pkg)}) "
            f"| [`src/core/{pkg}`]({TREE_URL}/{pkg}) |")


def read_roster(path):
    rows = []
    for line in path.read_text(encoding="utf-8").split("\n"):
        m = ROSTER_ROW.match(line)
        if m:
            rows.append(m.group(1))
    return rows


def read_index_rows(text):
    return [m.group(1) for m in (INDEX_ROW.match(l) for l in text.split("\n")) if m]


def compose(committed_text, packages):
    """Keep everything up to and including the table header + separator; replace the rows."""
    lines = committed_text.split("\n")
    try:
        head = lines.index(TABLE_HEAD)
    except ValueError:
        die(f"the committed page carries no CURRENT table header ({TABLE_HEAD!r})")
    # the separator row follows the header; the data rows run to the first non-row line
    keep = lines[: head + 2]
    rest = lines[head + 2:]
    tail = []
    seen_rows = False
    for i, l in enumerate(rest):
        if INDEX_ROW.match(l):
            seen_rows = True
            continue
        if seen_rows:
            tail = rest[i:]
            break
    return "\n".join(keep + [index_row(p) for p in packages] + tail)


def run(roster_path, valdir, write=False):
    index_path = valdir / "index.md"
    current = valdir / "current"
    for p in (roster_path, index_path, current):
        if not p.exists():
            die(f"missing input: {p}")

    packages = read_roster(roster_path)
    if not packages:
        die(f"the roster parsed to ZERO rows -- {roster_path} is not the roster, or its row "
            f"shape moved. A generator that reads nothing writes a clean empty page.")

    pages = {p.name for p in current.glob("*.md")}
    missing = [p for p in packages if page_name(p) not in pages]
    if missing:
        die(f"{len(missing)} roster package(s) have NO proof page under {current}: "
            + ", ".join(missing[:8]) + (" ..." if len(missing) > 8 else ""))

    wanted = {page_name(p) for p in packages}
    orphans = sorted(pages - wanted)
    if orphans:
        die(f"{len(orphans)} proof page(s) under {current} have NO roster row: "
            + ", ".join(orphans[:8]) + (" ..." if len(orphans) > 8 else "")
            + " -- a row count alone would not see these")

    committed_text = index_path.read_text(encoding="utf-8")
    committed = read_index_rows(committed_text)
    regenerated = compose(committed_text, packages)

    print(f"roster        : {shown(roster_path)}  {len(packages)} row(s)")
    print(f"proof pages   : {shown(current)}  {len(pages)} page(s)")
    print(f"committed rows: {len(committed)}")

    cset, rset = set(committed), set(packages)
    only_committed, only_roster = sorted(cset - rset), sorted(rset - cset)
    if only_committed or only_roster:
        print(f"\n!! SET DIFFERENCE -- the committed table and the roster name different packages")
        for p in only_committed:
            print(f"     committed only : {p}")
        for p in only_roster:
            print(f"     roster only    : {p}")
    else:
        print("\nSETS EQUAL: the committed table and the roster name the same packages")
        if committed != packages:
            print("!! ORDER DIFFERS, which is a sorting question and not a content one:")
            for i, (a, b) in enumerate(zip(committed, packages)):
                if a != b:
                    print(f"     first at row {i + 1}: committed {a!r}, roster {b!r}")
                    break

    if write:
        index_path.write_text(regenerated, encoding="utf-8", newline="\n")
        print(f"\nWROTE {shown(index_path)}  ({len(packages)} row(s))")
    else:
        same = regenerated == committed_text
        print(f"\nbytes identical to the committed page: {'YES' if same else 'NO'}  "
              f"(nothing written; pass --write to regenerate)")
    return 0


# ----------------------------------------------------------------------------- the arms
def selftest():
    root = Path(tempfile.mkdtemp(prefix="c2-vindex-arm-"))
    # ⚠ EVERY path below is under `root`, which this function created. The fleet spent today
    # finding tools that wrote outside their doors (G's materialisation dir, R's temp derived
    # from a caller's argument, C1's fetch into the lane checkout, C2's own $REPO); an arm that
    # exercises a writer is exactly where that happens, so there is one directory and it is ours.
    npass = nfail = 0

    def check(label, ok):
        nonlocal npass, nfail
        if ok:
            npass += 1
            print(f"  PASS  {label}")
        else:
            nfail += 1
            print(f"  FAIL  {label}")

    def fixture(pkgs, pages=None, index_pkgs=None):
        d = Path(tempfile.mkdtemp(dir=root))
        val = d / "validation"
        (val / "current").mkdir(parents=True)
        roster = d / "roster.md"
        roster.write_text(
            "# fixture\n\n" + "".join(
                f"| [`{p}`](https://example.invalid/{p}) | 1 | | text |\n" for p in pkgs),
            encoding="utf-8")
        for p in (pkgs if pages is None else pages):
            (val / "current" / page_name(p)).write_text("proof\n", encoding="utf-8")
        body = ["# Validation proofs", "", "prose", "", TABLE_HEAD, "|:--|:--|:--|"]
        body += [index_row(p) for p in (pkgs if index_pkgs is None else index_pkgs)]
        body += ["", "tail line"]
        (val / "index.md").write_text("\n".join(body) + "\n", encoding="utf-8")
        return roster, val

    try:
        print("=== arms ===")
        # GREEN: roster and pages agree, and regenerating is byte-identical
        r, v = fixture(["a", "b/c"])
        out = []
        try:
            run(r, v)
            green = (v / "index.md").read_text(encoding="utf-8")
            check("a matching fixture regenerates byte-identical",
                  compose(green, ["a", "b/c"]) == green)
        except SystemExit as e:
            check(f"a matching fixture regenerates byte-identical (refused: {e})", False)

        # RED: a roster package with no proof page
        r, v = fixture(["a", "b/c"], pages=["a"])
        try:
            run(r, v)
            check("a MISSING proof page refuses", False)
        except SystemExit as e:
            check("a MISSING proof page refuses", "NO proof page" in str(e))

        # RED: a proof page with no roster row -- the reverse a count hides
        r, v = fixture(["a"], pages=["a", "b/c"])
        try:
            run(r, v)
            check("an ORPHAN proof page refuses", False)
        except SystemExit as e:
            check("an ORPHAN proof page refuses", "NO roster row" in str(e))

        # RED: a roster that parses to nothing
        r, v = fixture(["a"])
        r.write_text("# nothing that matches the row shape\n", encoding="utf-8")
        try:
            run(r, v)
            check("a roster parsing to ZERO rows refuses", False)
        except SystemExit as e:
            check("a roster parsing to ZERO rows refuses", "ZERO rows" in str(e))

        # CONTROL: the ORDER-only case is reported and is NOT a set difference
        r, v = fixture(["a", "b/c"], index_pkgs=["b/c", "a"])
        import io, contextlib
        buf = io.StringIO()
        with contextlib.redirect_stdout(buf):
            run(r, v)
        text = buf.getvalue()
        check("an ORDER-only difference reports as order, not as a set difference",
              "SETS EQUAL" in text and "ORDER DIFFERS" in text
              and "SET DIFFERENCE" not in text)

        # CONTROL: a genuine set difference is NOT reported as an order one
        r, v = fixture(["a", "b/c"], index_pkgs=["a", "zzz"])
        buf = io.StringIO()
        with contextlib.redirect_stdout(buf):
            run(r, v)
        text = buf.getvalue()
        check("a SET difference reports as a set difference",
              "SET DIFFERENCE" in text and "ORDER DIFFERS" not in text)

        print(f"\nSELF-TEST: pass={npass} fail={nfail}")
        return 0 if nfail == 0 else 1
    finally:
        shutil.rmtree(root, ignore_errors=True)


if __name__ == "__main__":
    if "--selftest" in sys.argv:
        raise SystemExit(selftest())
    raise SystemExit(run(ROSTER, VALDIR, write="--write" in sys.argv))
