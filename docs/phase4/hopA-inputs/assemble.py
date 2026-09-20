#!/usr/bin/env python3
"""C1's recon-basis assembler: the per-lane recon TSVs -> the banked basis shardmap.py --timings reads.

NOT a copy of that reader. It prepares the file the reader gates, and every rule is one the reader
enforces at a line cited beside it.

RULED:
  COORD 96763d677 (2)  the banked basis EXCLUDES every row whose sweep_s is UNMEASURED
                       (shardmap.py:280 dies otherwise); for a HAND-STOPPED row alone,
                       sweep_s := wall_s whatever its word, so :308's drop still fires.
  COORD 2026-09-20     the lanes' headers are read BY NAME and the basis is their INTERSECTION.

⚠ WHY BY NAME, AND WHY THIS REPLACED AN EQUALITY CHECK. This tool used to require every lane's
header to be BYTE-EQUAL. Measured 2026-09-20: R's committed TSV carries TEN columns and the recon
wrapper's EIGHTH blob emits ELEVEN (the same ten plus a trailing `post_s`, at run-h10-recon.ps1:574),
and G relaunched on the eighth -- so the equality check would have refused the very union this tool
exists to build. shardmap.py:259-260 says why, in its own words: columns are read by NAME because
"pass 1 carries seven columns and pass 2 carries five, so a positional read of `sweep_s` silently
takes a different quantity from the other file". The reader this feeds was built for mixed lanes; the
writer refusing them was the defect.

⚠ INTERSECTION, NOT UNION-AND-PAD. A union pads the narrower lanes' rows with an empty cell, which
manufactures a column that LOOKS measured and is not -- the same shape as the `diverged` defect i9
reported (PowerShell's `0 -eq ''` is TRUE, so the fifth blob rewrote every real 0 to `n/a`). Dropping
a column loses a reading that survives in the per-lane TSV; minting one puts a false reading in the
BANKED basis. Every dropped column is reported by name and by lane, never silently.

The per-lane TSVs stay whole as the readings; this writes a different artifact.
"""
import re, sys, io

HAND_STOPPED = {"net"}          # mirrors shardmap.py:99

# The columns THIS tool must resolve by name. `row`, `word`, `verdicts` and `sweep_s` are
# shardmap.py:262's own `need`; `wall_s` is this tool's, for the hand-stopped substitution.
REQUIRED = ("row", "word", "verdicts", "sweep_s", "wall_s")


def die(msg):
    print(f"ASSEMBLY REFUSED: {msg}")
    sys.exit(2)


def read_lane(path):
    text = io.open(path, encoding="utf-8", newline="").read()
    if text.count("\r"):
        die(f"{path} carries {text.count(chr(13))} CR byte(s) -- shardmap.py:249 refuses the basis")
    lines = [l for l in text.split("\n") if l.strip()]
    if len(lines) < 2:
        die(f"{path} holds {len(lines)} non-blank line(s) -- a lane that measured nothing")
    header = lines[0].split("\t")
    if len(header) != len(set(header)):
        dup = sorted({c for c in header if header.count(c) > 1})
        die(f"{path} header repeats {dup} -- a by-NAME read cannot choose between two columns of one name")
    missing = [c for c in REQUIRED if c not in header]
    if missing:
        die(f"{path} header lacks {missing} -- columns are read by NAME. Header seen: {header}")
    return header, lines[1:]


def assemble(paths, out):
    lanes = [(p,) + read_lane(p) for p in paths]

    # The INTERSECTION, ordered by the FIRST lane's header so the basis has one stable column order.
    common = set(lanes[0][1])
    for _, header, _ in lanes[1:]:
        common &= set(header)
    order = [c for c in lanes[0][1] if c in common]

    # ⚠ UNREACHABLE TODAY, AND KEPT AS AN INVARIANT RATHER THAN A GUARD -- said plainly because a
    # check that cannot be made to fail proves nothing (safety floor 13), and this one cannot:
    # read_lane already refuses any lane missing a REQUIRED column, and an intersection of sets that
    # each contain REQUIRED contains REQUIRED. It is here so that WEAKENING the per-lane check --
    # admitting a lane that carries only four of the five, say -- cannot silently produce a basis
    # the reader can no longer read by name. It has no red in the battery below, ON PURPOSE, and
    # writing one would mean writing a fixture that cannot exist.
    for c in REQUIRED:
        if c not in order:
            die(f"the lanes' INTERSECTION lacks {c!r} -- unreachable while read_lane refuses a lane "
                f"missing it, so reaching this means that check was weakened. Intersection: {order}")

    dropped_cols = []
    for p, header, _ in lanes:
        extra = [c for c in header if c not in common]
        if extra:
            dropped_cols.append((p, extra))

    kept, dropped, substituted, seen = [], [], [], {}
    for p, header, rows in lanes:
        ix = {c: header.index(c) for c in header}
        for line in rows:
            cells = line.split("\t")
            if len(cells) != len(header):
                die(f"{p}: a row has {len(cells)} cell(s) against a {len(header)}-column header: {line!r}")
            name = cells[ix["row"]].strip()
            secs = cells[ix["sweep_s"]].strip()
            wall = cells[ix["wall_s"]].strip()

            if name in seen:
                die(f"{name!r} appears in {seen[name]} and {p} -- the lanes' row sets must be DISJOINT; "
                    f"a shard split that repeats a row double-books it in the schedule")
            seen[name] = p

            projected = "\t".join(cells[ix[c]] for c in order)

            if name in HAND_STOPPED:
                # RULED: bank the observed wall whatever the word. Dropped by name at shardmap.py:290
                # BEFORE any use (verified at the source), so the figure never reaches the schedule --
                # it exists only so :308's "the drop must still have fired" assertion can fire.
                if not re.fullmatch(r"\d+", wall):
                    die(f"{name}: wall_s is {wall!r}, not an integer, so the hand-stopped row cannot be "
                        f"banked and :308 would refuse the basis. The wrapper owes an integer wall_s "
                        f"for EVERY row (COORD 96763d677 (2)).")
                if secs != wall:
                    substituted.append((name, secs, wall, p))
                    cells[ix["sweep_s"]] = wall
                    projected = "\t".join(cells[ix[c]] for c in order)
                kept.append(projected)
                continue

            if re.fullmatch(r"\d+", secs):
                kept.append(projected)
                continue
            dropped.append((name, secs, p))

    if not any(l.split("\t")[order.index("row")].strip() in HAND_STOPPED for l in kept):
        die(f"no hand-stopped row {sorted(HAND_STOPPED)} survives -- shardmap.py:308 refuses the basis")

    io.open(out, "w", encoding="utf-8", newline="\n").write("\t".join(order) + "\n" + "\n".join(kept) + "\n")
    print(f"banked basis: {out}")
    print(f"  lanes         {len(lanes)}: " + ", ".join(f"{p} ({len(h)} cols, {len(r)} rows)" for p, h, r in lanes))
    print(f"  INTERSECTION  {len(order)} column(s): {' '.join(order)}")
    print(f"  DROPPED COLS  " + ("; ".join(f"{p}: {', '.join(e)}" for p, e in dropped_cols) or "(none -- every lane carried the same set)"))
    print(f"  kept          {len(kept)} row(s)")
    print(f"  SUBSTITUTED   " + (", ".join(f"{n}: sweep_s {s!r} -> wall_s {w} (from {p})" for n, s, w, p in substituted) or "(none)"))
    print(f"  DROPPED ROWS  " + (", ".join(f"{n} ({s!r} from {p})" for n, s, p in dropped) or "(none)"))


if __name__ == "__main__":
    if len(sys.argv) < 3:
        die("usage: assemble.py <lane.tsv> [<lane.tsv> ...] <out.tsv>")
    assemble(sys.argv[1:-1], sys.argv[-1])
