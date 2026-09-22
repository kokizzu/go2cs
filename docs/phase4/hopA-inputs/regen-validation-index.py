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

import io
import re
import sys
import shutil
import tempfile
import contextlib
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

# A row's OWN proof link, which is not always page_name(its package).  An H10 "banked by
# inheritance" row links the RETIRED SOURCE's page "unmoved and unrenamed", so the page's name is
# the source's import path and not the row's.  Deriving the wanted set from package NAMES alone
# therefore called every such anchor an orphan -- the tool and the doctrine disagreeing, with the
# doctrine right.  A row may carry MORE than one (fips140test links two), so every match counts.
ROSTER_PROOF = re.compile(r"\[proof\]\((?:validation/)?current/([^)/]+\.md)\)")
# ⚠ The first cut of this pattern required `current/` immediately, and the roster spells its
# links RELATIVE TO docs/ -- `validation/current/<name>.md`. It therefore matched NOTHING on
# the real roster while every fixture arm passed, because the fixture wrote the spelling the
# pattern expected. A hermetic fixture encodes its author's model and cannot falsify it; the
# real-root run found this in one pass, which is why the arms below now write the REAL
# spelling and why this tool is scored against the tree before it is believed. The `[^)/]+`
# also keeps the placeholder `[proof](…)` some rows carry from matching.

# A row of the roster's own "## Excluded packages" table: package, verdict count, bar class.
#
# THE DURABLE SOURCE IS THE ROSTER, deliberately, and it is the roster alone.  An exclusion is also
# written up in a docs/phase4 DATA record (the bar reads that produced it), but a phase4 record is a
# point-in-time document that doctrine says is "amended with dated blocks, never rewritten, NEVER
# EXECUTED FROM" -- so a tool that read one would be executing from a record, and an exclusion that
# exists only there is not yet a roster fact.  The consequence is intended: a package refused at the
# bar keeps orphaning its page until its exclusion row lands in the roster, and that refusal is the
# guard being right rather than a gap here.
EXCLUSION_ROW = re.compile(r"^\| `([^`]+)` \| \d+ \| (E[1-4]) \|")


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


def read_roster_proof_links(path):
    """Page names some ROSTER ROW's own [proof] link resolves to.

    Scanned from the table's LINKS rather than from its package names, which is the whole point:
    the anchor rule makes the link the authority on which page a row is backed by.  Only lines that
    are roster rows are scanned, so a [proof] spelling in the surrounding prose contributes nothing.
    """
    names = set()
    for line in path.read_text(encoding="utf-8").split("\n"):
        if ROSTER_ROW.match(line):
            names.update(ROSTER_PROOF.findall(line))
    return names


def read_roster_exclusions(path):
    """Page names of packages the roster's own exclusion table names WITH a bar class."""
    names = {}
    for line in path.read_text(encoding="utf-8").split("\n"):
        m = EXCLUSION_ROW.match(line)
        if m:
            names[page_name(m.group(1))] = m.group(2)
    return names


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
    # ⚠ The tail is found by walking PAST the existing rows, so with ZERO of them `seen_rows`
    # never becomes true, `tail` stays empty, and everything after the header is dropped. That is
    # the same shape as this tool's zero-row-roster refusal one layer over -- an instrument that
    # reads nothing produces a clean-looking page -- so it refuses for the same reason rather than
    # truncating silently. Unreachable on today's committed page twice over (204 rows, and nothing
    # after the table), which is exactly why it needed an arm and not a reading: `--write` is the
    # only path that could ever do the damage and no arm exercised it.
    if not seen_rows:
        die(f"the committed page's CURRENT table carries a header and ZERO data rows -- "
            f"regenerating would drop everything after it. A table with no rows is not a table "
            f"this tool can safely rewrite around.")
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
    linked = read_roster_proof_links(roster_path)
    excluded = read_roster_exclusions(roster_path)

    # Three ways a page is BACKED, and they are not the same question:
    #   by name      -- a roster row whose package derives this page's name;
    #   by link      -- a roster row whose own [proof] link resolves to it (the anchor rule);
    #   by exclusion -- the roster's exclusion table names its package with a bar class.
    admitted = wanted | linked | set(excluded)
    orphans = sorted(pages - admitted)
    if orphans:
        by_link = sorted((pages & linked) - wanted)
        by_bar = sorted((pages & set(excluded)) - wanted)
        die(f"{len(orphans)} proof page(s) under {current} are backed by NOTHING -- no roster row "
            f"derives them, no row's [proof] link resolves to them, and the roster's exclusion "
            f"table does not name them:\n"
            + "".join(f"     unbacked          : {o}\n" for o in orphans)
            + f"   admitted alongside them: {len(by_link)} by a row's [proof] link (the anchor "
            f"rule), {len(by_bar)} by a roster exclusion class"
            + (("\n" + "".join(f"     by link           : {o}\n" for o in by_link)) if by_link else "")
            + (("" if not by_bar else "".join(
                f"     by exclusion {excluded[o]} : {o}\n" for o in by_bar)))
            + "   -- a row count alone would not see any of this")

    committed_text = index_path.read_text(encoding="utf-8")
    committed = read_index_rows(committed_text)
    regenerated = compose(committed_text, packages)

    print(f"roster        : {shown(roster_path)}  {len(packages)} row(s)")
    print(f"proof pages   : {shown(current)}  {len(pages)} page(s)")
    print(f"              : {len(pages & wanted)} by name, "
          f"{len((pages & linked) - wanted)} by a row's [proof] link, "
          f"{len((pages & set(excluded)) - wanted)} by a roster exclusion class")
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

    def fixture(pkgs, pages=None, index_pkgs=None, links=None, exclusions=None):
        """links: {pkg: [page name, ...]} appended to that row as [proof] links.
        exclusions: [(pkg, "E1".."E4"), ...] written as the roster's exclusion table."""
        d = Path(tempfile.mkdtemp(dir=root))
        val = d / "validation"
        (val / "current").mkdir(parents=True)
        roster = d / "roster.md"
        links = links or {}
        rows = ""
        for p in pkgs:
            tail = "".join(f" · [proof](validation/current/{n})" for n in links.get(p, []))
            rows += f"| [`{p}`](https://example.invalid/{p}) | 1 | | text{tail} |\n"
        body = "# fixture\n\n" + rows
        if exclusions:
            body += "\n## Excluded packages\n\n"
            for pkg, cls in exclusions:
                body += f"| `{pkg}` | 0 | {cls} | reason | [ruling][exclusion-ruling] |\n"
        roster.write_text(body, encoding="utf-8")
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

        # RED: a proof page with no roster row -- the reverse a count hides.
        # ⚠ This arm FAILED when the refusal wording moved from "NO roster row" to "backed by
        # NOTHING" for the anchor rule, which is the arm doing its job: the message is part of the
        # contract, and a refusal that no longer says what it used to is a change a reader must see.
        r, v = fixture(["a"], pages=["a", "b/c"])
        try:
            run(r, v)
            check("an ORPHAN proof page refuses", False)
        except SystemExit as e:
            check("an ORPHAN proof page refuses", "backed by NOTHING" in str(e))

        # ---- the anchor rule (this seat) -------------------------------------------------------
        # GREEN: an orphan a row's OWN [proof] link resolves to is ADMITTED. This is the H10 "banked
        # by inheritance" shape: the row is `t`, the page is the retired SOURCE's, unmoved.
        r, v = fixture(["a", "t"], pages=["a", "t", "src.pkg"],
                       links={"t": ["src.pkg.md"]})
        try:
            run(r, v)
            check("an orphan a row's [proof] link resolves to is ADMITTED (the anchor rule)", True)
        except SystemExit as e:
            check(f"an orphan a row's [proof] link resolves to is ADMITTED ({e})", False)

        # GREEN: an orphan whose package the roster's EXCLUSION table names with a bar class.
        r, v = fixture(["a"], pages=["a", "gated.pkg"],
                       exclusions=[("gated/pkg", "E1")])
        try:
            run(r, v)
            check("an orphan named in the roster's exclusion table is ADMITTED", True)
        except SystemExit as e:
            check(f"an orphan named in the roster's exclusion table is ADMITTED ({e})", False)

        # RED: a PLAIN orphan -- no row derives it, no link resolves to it, no exclusion names it.
        # The made-to-fail control for both admissions above: same shape, neither backing present.
        r, v = fixture(["a"], pages=["a", "src.pkg"])
        try:
            run(r, v)
            check("a PLAIN orphan still refuses", False)
        except SystemExit as e:
            check("a PLAIN orphan still refuses",
                  "backed by NOTHING" in str(e) and "src.pkg.md" in str(e))

        # RED: a row with no page of its own still REFUSES even when it LINKS one -- rule (c), the
        # fips140test shape. An anchor backs a PAGE; it does not excuse a row from having one.
        r, v = fixture(["a", "t"], pages=["a", "src.pkg"], links={"t": ["src.pkg.md"]})
        try:
            run(r, v)
            check("a row with NO page of its own refuses even when it links an anchor", False)
        except SystemExit as e:
            check("a row with NO page of its own refuses even when it links an anchor",
                  "NO proof page" in str(e) and "t" in str(e))

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

        # ⚠⚠ THE WRITE PATH, WHICH NO ARM ABOVE TOUCHES. All six call `run(r, v)` and take the
        # default `write=False`, so the tool's only state-advancing path was unexercised -- on a
        # tool whose own note says `--write` must not be run against the real tree until the last
        # leg, which makes an arm the ONLY place it can ever run. The green arm above proves
        # `compose`, not the write.
        r, v = fixture(["a", "b/c"])
        index = v / "index.md"
        before = index.read_text(encoding="utf-8")
        with contextlib.redirect_stdout(io.StringIO()):
            run(r, v, write=True)
        after = index.read_text(encoding="utf-8")
        check("--write on a matching fixture is byte-identical (a real write, not compose)",
              after == before)

        # and the same path must CARRY what follows the table rather than eat it
        r, v = fixture(["a", "b/c"])
        index = v / "index.md"
        index.write_text(index.read_text(encoding="utf-8") + "\n## Notes\n\nafter the table\n",
                         encoding="utf-8")
        with contextlib.redirect_stdout(io.StringIO()):
            run(r, v, write=True)
        check("--write keeps content that FOLLOWS the table",
              "after the table" in index.read_text(encoding="utf-8"))

        # RED for the refusal this commit adds: a header with zero data rows.  Before it, --write
        # dropped the tail here and reported success.
        r, v = fixture(["a"])
        index = v / "index.md"
        body = [l for l in index.read_text(encoding="utf-8").split("\n") if not INDEX_ROW.match(l)]
        index.write_text("\n".join(body + ["", "## Notes", "", "after the table"]) + "\n",
                         encoding="utf-8")
        kept = index.read_text(encoding="utf-8")
        try:
            with contextlib.redirect_stdout(io.StringIO()):
                run(r, v, write=True)
            check("a committed table with ZERO data rows refuses", False)
        except SystemExit as e:
            check("a committed table with ZERO data rows refuses", "ZERO data rows" in str(e))
        check("...and nothing was written on that refusal",
              index.read_text(encoding="utf-8") == kept)

        print(f"\nSELF-TEST: pass={npass} fail={nfail}")
        return 0 if nfail == 0 else 1
    finally:
        shutil.rmtree(root, ignore_errors=True)


if __name__ == "__main__":
    if "--selftest" in sys.argv:
        raise SystemExit(selftest())
    raise SystemExit(run(ROSTER, VALDIR, write="--write" in sys.argv))
