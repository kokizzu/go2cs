"""Compute the hop-era fleet shard map from per-row sweep wall times.

Method: PLAN-hop-campaign.md section 4.3, generalized into GoCorpusMigration.md section 3.2 (the
maintained copy) -- reserved set pinned to the i9, remaining rows LPT-greedy across W bins weighted
by provisional speed factors s_w (i9 = 1.00).

!! WHAT THIS EMITS IS A PROJECTION, NOT A DEAL. s_w are PLACEHOLDERS pending hop-recon calibration,
and the makespan printed below is an explicit LOWER BOUND because a fifth of the roster has no
measured cost at all (see UNSCHEDULED). Section 3.2: "a map built at placeholder factors is a
projection, not a deal -- say which it is, and gate dispatch on it."

REPAIRED 2026-09-13 (lane C2, coordinator ruling e0d5121e2 section 5), after the derivation in
DATA-h10-shardmap-projection-go124.md found it could not emit at the tip and carried four asserts
of one broken class. What changed, and why each mattered:

  1. THE BLOCK IS SELECTED BY A (OS, SHA, MACHINE) KEY, not by position. It used to take the FIRST
     fenced block in the DATA file, so inserting a newer measurement above the old one would have
     silently re-based the whole map. The headings were ALREADY labelled with that key; only the
     selector was positional. Zero or several matches refuse by name.
  2. THE ROW PARSER TOLERATES THE VERDICT-COLUMN VARIANT. The linux block carries a
     PASS/FAIL/CVAC column and 10 of its 162 rows have NO verdict count (`crypto/tls FAIL 711s`).
     The old pattern parsed 162 of 162 windows rows and 0 of 162 linux rows -- so the linux block
     was unreachable by position AND unparseable if reached.
  3. A CONTENT ASSERT beside the cardinality one. `assert len(rows) == 162` guarded the COUNT and
     nothing else, so corrupting one t_r in place (archive/zip 354s -> 99999s) passed every guard,
     printed "rows parsed: 162", and reported a makespan basis 14x wrong with the instrument fully
     green. The block's digest (rows, sum, sha256 over sorted `path\tt_r`) is declared in the DATA
     file's own digests section and this refuses when its parse does not reproduce it.
  4. EVERY HARDCODED COUNT IS GONE. The `162` literals (two of them) and the `7` in the checksum
     message are derived from the input; the message prints from the SAME variables the assert
     reads, so a completing run can no longer print arithmetic that does not add up. It used to
     say "162 rows assigned == 7 reserved + 151 bulk", and 7 + 151 = 158.
  5. R := reserved INTERSECT rows, AS THE CONSTRUCTION SAYS. It asserted membership instead, so a
     reserved row with no measured cost killed the run: `net` and `net/http` joined $longTimeouts
     on 2026-09-02 and have no t_r, and the script had been dead at the tip ever since. The assert
     was not wrong to care -- a silently dropped pin is exactly the failure it feared -- so the
     intersection REPORTS the fallout by name and refuses to pretend those rows are scheduled.
  6. THE RESERVED-SET EXTRACTION IS BRACE-MATCHED AND COUNT-CHECKED. Its pattern was
     `@\{(.*?)\}` -- NON-GREEDY, so a nested `@{ }` inside $longTimeouts closes the capture early
     and every floor after it is silently lost. DESIGN-peros-roster.md section 7 specifies exactly
     that nested shape for per-OS floors, with `time` as its worked example: applying the
     documented schema to the live table yields 6 of 11 floors, no error. BOARD entry 2026-09-13.
  7. THE POPULATION IS THE ROSTER, and rows without a measured cost are UNSCHEDULED rather than
     absent. 42 of the 204 banked rows have no t_r; the map is emitted over the costed rows and
     the 42 are listed with NO COST CLAIM (coordinator ruling e0d5121e2 section 2: (B) now,
     (C) at recon; a nominal is refused in every form, "including upper bound").
  8. W IS THE FOUR NAMED BOXES (ruling section 3). The placeholder fifth machine is gone: a count
     is not a deal, and "X (5th engaged machine)" with a placeholder factor was a count.
"""
import hashlib
import re
import statistics
import sys
from pathlib import Path

# ---------------------------------------------------------------- stdout, before anything prints
# ⚠ THIS GENERATOR DID NOT RUN ON THE LANE THAT DISPATCHES, and the reason was one glyph. Windows Python
# writes stdout in the console codepage (cp1252), which has no U+26A0, so `--emit-plan` died
# UnicodeEncodeError before writing anything and returned rc=1 (i9, measured on the i9 at 02b87b501;
# reproduced on the cloud lane with PYTHONIOENCODING=cp1252, which is the arm that makes this fix
# falsifiable from a box that is not Windows).
#
# TWO REMEDIES, both kept, because they cover different populations:
#
#   1. Every OUTPUT payload in this file is ASCII. That is C2's own .ps1 convention -- run-h10-dispatch.ps1
#      is pure ASCII for exactly this reason -- applied to the .py it was not applied to. It needs no
#      runtime support and no caller-side environment variable, which is the weakest of the three shapes
#      because nothing asserts a convention.
#   2. errors="replace" on stdout, for the glyphs THIS FILE CANNOT SEE. The DATA block's own heading
#      carries a middle dot and its digest table an ellipsis: those arrive from the INPUT and are printed,
#      so a source census of this file cannot find them (i9's census could not, and neither could mine).
#      cp1252 happens to encode both; cp437, a real console default, encodes neither. With replace, an
#      unencodable input glyph degrades to a substitute instead of killing a run that has already written
#      its artifact.
#
# ⚠ AND THE ORDERING HAZARD IS WHY 2 IS NOT OPTIONAL: the plan is written BEFORE the summary is printed,
# so a print that raises leaves a VALID PLAN ON DISK behind a non-zero exit -- a caller checking rc
# discards a good plan, one not checking rc uses a plan whose generator reported failure. i9 read that
# shape out of the source without being able to manufacture the data for it. `errors="replace"` makes the
# print unable to raise, which closes it by construction rather than by ordering care.
#
# The PLAN FILE itself is unaffected either way: it is written with an explicit encoding and newline, so
# the artifact never depended on the console.
if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(errors="replace")

HERE = Path(__file__).resolve().parent
DATA = HERE.parent / "DATA-sweep-row-walltimes.md"
ROSTER = HERE.parent.parent / "ValidatedTestPackages.md"
SWEEP = HERE.parent.parent.parent / "src" / "run-validated-sweep.ps1"

# The block this map is parameterized by, as a (OS, corpus SHA, machine) key rather than "the first
# one". Change this line to re-base the map, and the change is then visible in a diff.
BLOCK_KEY = ("windows", "18770d083", "i9-13900K")

# Rows whose measured seconds are a LOWER BOUND rather than a cost, because a person stopped the run.
# Named here, dropped by name, and the drop is asserted to have fired -- a silent no-op drop is how a
# basis change turns a documented exclusion into a scheduled row.
HAND_STOPPED = {"net"}


def die(msg):
    raise SystemExit(f"shardmap: REFUSED -- {msg}")


# ---------------------------------------------------------------- argv, before anything reads a file
# `--timings <tsv>` selects the recon basis over the DATA block. No default and no fallback: a path that
# does not resolve REFUSES rather than quietly reverting to the other basis, because the two bases differ
# by 24% in total seconds and 60% in coverage, and a run that silently swapped them would look healthy.
TIMINGS = None

for _i, _a in enumerate(sys.argv[1:], start=1):
    if _a == "--timings":
        if _i + 1 >= len(sys.argv):
            die("--timings needs a path to a banked per-row TSV")
        TIMINGS = Path(sys.argv[_i + 1]).resolve()
        if not TIMINGS.is_file():
            die(f"--timings path does not resolve to a file: {sys.argv[_i + 1]}")


# ---------------------------------------------------------------- the BASIS, one of two
# The map is parameterized by per-row cost, and there are now TWO banked sources for it. Which one
# a run used is printed, and is written into the emitted plan's header, because a plan derived from
# one basis and read as the other is a silent 24% error in the total and a 60% error in coverage.
def parse_data_block():
    """The original basis: a labelled, digest-declared block in DATA-sweep-row-walltimes.md."""
    # ---------------------------------------------------------------- parse: the costed rows
    # newline="" for the same reason the roster read below states: universal newlines would hide a CR.
    with open(DATA, encoding="utf-8", newline="") as fh:
        text = fh.read()
    if text.count("\r"):
        die(f"{DATA.name} carries {text.count(chr(13))} CR byte(s) -- these tables are LF")

    # Every "## <os> · corpus `<sha>` · <machine> ..." section and the first fenced block under it.
    sections = []
    for m in re.finditer(r"^## (?P<head>.+)$", text, re.M):
        head = m.group("head")
        rest = text[m.end():]
        nxt = re.search(r"^## ", rest, re.M)
        body = rest[: nxt.start()] if nxt else rest
        fence = re.search(r"```\n(.*?)```", body, re.S)
        sections.append((head, fence.group(1) if fence else None))

    wanted = [
        (h, b) for h, b in sections
        if b is not None
        and h.lower().startswith(BLOCK_KEY[0].lower())
        and BLOCK_KEY[1] in h
        and BLOCK_KEY[2] in h
    ]
    if len(wanted) != 1:
        die(f"the block key {BLOCK_KEY} matched {len(wanted)} labelled blocks with a fenced body "
            f"(of {sum(1 for _, b in sections if b is not None)} present) -- a map must name exactly one "
            f"measurement. Headings seen: {[h[:60] for h, b in sections if b is not None]}")
    BLOCK_HEAD, block = wanted[0]

    # name [VERDICT] [verdict-count] <seconds>s -- the verdict column and the count are both optional,
    # which is what makes the linux block readable. The seconds are not optional: a row with no measured
    # time is not a row this script may schedule, and it belongs in UNSCHEDULED below.
    ROW = re.compile(r"^(?P<name>\S+)\s+(?:(?P<verdict>[A-Z]{3,8})\s+)?(?:(?P<count>\d+)\s+)?"
                     r"(?P<secs>\d+)s\s*$")
    rows = []
    for line in block.strip().splitlines():
        m = ROW.match(line)
        if not m:
            die(f"unparsed row in block {BLOCK_HEAD!r}: {line!r}")
        rows.append((m.group("name"), int(m.group("count")) if m.group("count") else None,
                     int(m.group("secs"))))

    if not rows:
        die(f"block {BLOCK_HEAD!r} parsed EMPTY -- a verdict over no rows is clean by construction")

    dup = sorted({n for n, _, _ in rows if [x for x, _, _ in rows].count(n) > 1})
    if dup:
        die(f"duplicate paths in the costed block: {', '.join(dup)}")

    # ---------------------------------------------------------------- the CONTENT assert
    # Cardinality is not content. The digest is declared in the DATA file's own digests section, keyed by
    # the same (OS, SHA, machine) triple, and is computed over the ROWS ONLY -- so it is invariant under
    # reformatting and moves only when a path or a time moves.
    def digest_of(pairs):
        payload = "".join(f"{n}\t{t}\n" for n, t in sorted(pairs))
        return hashlib.sha256(payload.encode("utf-8")).hexdigest()


    parsed_pairs = [(n, t) for n, _, t in rows]
    parsed_sum = sum(t for _, t in parsed_pairs)
    parsed_digest = digest_of(parsed_pairs)

    declared = None
    for line in text.splitlines():
        if not line.startswith("|"):
            continue
        cells = [c.strip().strip("`") for c in line.strip().strip("|").split("|")]
        if len(cells) != 4:
            continue
        key = cells[0]
        if all(part in key for part in BLOCK_KEY) and re.fullmatch(r"[0-9a-f]{64}", cells[3] or ""):
            if declared is not None:
                die(f"two declared digests match the block key {BLOCK_KEY}")
            declared = (int(cells[1]), int(cells[2]), cells[3])

    if declared is None:
        print(f"!! CONTENT UNVERIFIED: no digest declared for {BLOCK_KEY} in {DATA.name}. "
              f"The parse is {len(rows)} rows / {parsed_sum} s / sha256 {parsed_digest} -- "
              f"declare it in the digests section so a corrupted t_r cannot pass.")
    else:
        d_rows, d_sum, d_digest = declared
        if (d_rows, d_sum, d_digest) != (len(rows), parsed_sum, parsed_digest):
            die("CONTENT DIGEST MISMATCH for block {}\n"
                "  declared: {} rows, {} s, sha256 {}\n"
                "  parsed:   {} rows, {} s, sha256 {}\n"
                "  A row's path or time has moved since the digest was written. This is the assert that "
                "a cardinality check cannot make: the count can be right while a time is 14x wrong."
                .format(BLOCK_KEY, d_rows, d_sum, d_digest, len(rows), parsed_sum, parsed_digest))
        print(f"content digest VERIFIED for {BLOCK_KEY}: {len(rows)} rows, {parsed_sum} s, "
              f"sha256 {parsed_digest[:16]}...")

    total = parsed_sum
    verdicts = sum(v for _, v, _ in rows if v is not None)
    times = sorted(t for _, _, t in rows)

    print(f"\nblock:            {BLOCK_HEAD}")
    print(f"rows parsed:      {len(rows)}")
    print(f"total verdicts:   {verdicts}"
          + (f"  (over the {sum(1 for _, v, _ in rows if v is not None)} rows carrying a count)"
             if any(v is None for _, v, _ in rows) else ""))
    print(f"total i9-seconds: {total}  ({total/60:.1f} min)")
    print(f"median row:       {statistics.median(times)} s")
    print(f"mean row:         {total/len(rows):.1f} s")
    p = lambda q: times[min(len(times)-1, int(q*len(times)))]
    print(f"p75: {p(0.75)} s   p90: {p(0.90)} s   p95: {p(0.95)} s")

    return rows, total, f"DATA block {BLOCK_HEAD}"


def parse_timings_tsv(path):
    """The recon basis: a banked per-row TSV, read by COLUMN NAME and never by position.

    !! WHY A TSV AND NOT A NEW BLOCK IN DATA-sweep-row-walltimes.md. Copying 204 rows into a markdown
    table beside the TSV that already holds them is two sources of truth that will drift -- i9 flagged
    exactly that as a judgement call when banking the record and left the table out. Reading the banked
    file settles the call in the same direction: the rows have ONE home, and this function is how the
    map reaches it.
    """
    with open(path, encoding="utf-8", newline="") as fh:
        text = fh.read()

    if text.count("\r"):
        die(f"{path.name} carries {text.count(chr(13))} CR byte(s) -- the banked TSVs are LF, and a CR "
            f"would ride into every derived figure's provenance")

    lines = [ln for ln in text.split("\n") if ln.strip()]

    if len(lines) < 2:
        die(f"{path.name} holds {len(lines)} non-blank line(s) -- a basis over no rows is clean by "
            f"construction, which is the reading this script exists to refuse")

    # BY NAME. Position is what a reparse changes: pass 1 carries seven columns and pass 2 carries five,
    # so a positional read of `sweep_s` silently takes a different quantity from the other file.
    header = lines[0].split("\t")
    need = ("row", "word", "verdicts", "sweep_s")
    missing = [c for c in need if c not in header]

    if missing:
        die(f"{path.name} header lacks {missing} -- columns are read by NAME. Header seen: {header}")

    ix = {c: header.index(c) for c in need}
    seen, dropped, dups = {}, [], []

    for lineno, line in enumerate(lines[1:], start=2):
        cells = line.split("\t")

        if len(cells) <= max(ix.values()):
            die(f"{path.name}:{lineno} has {len(cells)} cell(s), too few for the named columns: {line!r}")

        name, word = cells[ix["row"]].strip(), cells[ix["word"]].strip()
        secs_cell, verdict_cell = cells[ix["sweep_s"]].strip(), cells[ix["verdicts"]].strip()

        if not re.fullmatch(r"\d+", secs_cell):
            die(f"{path.name}:{lineno} sweep_s is {secs_cell!r}, not an integer -- a row with no measured "
                f"cost is UNSCHEDULED, never nominal: {line!r}")

        secs = int(secs_cell)
        count = int(verdict_cell) if re.fullmatch(r"\d+", verdict_cell) else None

        # ⚠ THE HAND-STOPPED ROWS ARE NOT COSTS AND ARE DROPPED BY NAME. The record states it outright:
        # `net`'s figures in both passes are LOWER BOUNDS produced by a person stopping the row. Scheduling
        # on a lower bound is scheduling on a number that cannot be wrong in the safe direction.
        if name in HAND_STOPPED:
            dropped.append((name, secs))
            continue

        if name in seen:
            # A legitimate duplicate: the record says archive/tar appears twice, a control run and a
            # roster run. Take the LARGER -- the makespan is what this feeds, and the smaller reading
            # would under-book the row. Reported by name with both values, never folded silently.
            dups.append((name, seen[name][1], secs))
            if secs > seen[name][1]:
                seen[name] = (count, secs)
            continue

        seen[name] = (count, secs)

    # ⚠ THE DROP MUST STILL HAVE FIRED. A drop list that quietly matches nothing is the tolerance-become-
    # dead-code shape: the day the banked TSV renames or removes that row, this script would schedule on
    # whatever replaced it and print the same reassuring line it prints today.
    if not dropped:
        die(f"none of the hand-stopped rows {sorted(HAND_STOPPED)} appear in {path.name}. Either the "
            f"basis changed or the name did -- read the record's reading rules before scheduling on this.")

    rows = [(n, c, t) for n, (c, t) in seen.items()]

    if not rows:
        die(f"{path.name} parsed EMPTY after drops -- refusing a verdict over no rows")

    total = sum(t for _, _, t in rows)
    times = sorted(t for _, _, t in rows)
    counted = [c for _, c, _ in rows if c is not None]

    print(f"basis:            {path.name}")
    print(f"  sha256          {hashlib.sha256(text.encode('utf-8')).hexdigest()}")
    print(f"rows parsed:      {len(rows)}  (from {len(lines) - 1} data line(s))")
    print(f"total verdicts:   {sum(counted)}"
          + (f"  (over the {len(counted)} rows carrying a count)" if len(counted) != len(rows) else ""))
    print(f"total i9-seconds: {total}  ({total/60:.1f} min)")
    print(f"median row:       {statistics.median(times)} s")
    print(f"mean row:         {total/len(rows):.1f} s")
    q = lambda f: times[min(len(times)-1, int(f*len(times)))]
    print(f"p75: {q(0.75)} s   p90: {q(0.90)} s   p95: {q(0.95)} s")
    print(f"DROPPED as hand-stopped, NOT scheduled and NO cost claimed: "
          + ", ".join(f"{n} ({t} s, a lower bound)" for n, t in sorted(dropped)))

    for name, first, second in sorted(dups):
        print(f"duplicate row {name}: {first} s and {second} s -- taking the LARGER ({max(first, second)} s)")

    # The floor is the story this basis tells, and it is the one the assignment rule rests on, so it is
    # printed rather than left for a reader to derive from the percentiles.
    floor = times[0]
    near = sum(1 for t in times if t <= floor + 10)
    print(f"!! FLOOR-DOMINATED: min {floor} s, and {near} of {len(times)} rows ({near/len(times):.0%}) "
          f"are within 10 s of it -- which is why the light bulk is balanced by ROW COUNT and not by t_r")

    return rows, total, f"{path.name} (sha256 {hashlib.sha256(text.encode('utf-8')).hexdigest()[:16]}...)"


if TIMINGS is not None:
    rows, total, BASIS = parse_timings_tsv(TIMINGS)
else:
    rows, total, BASIS = parse_data_block()

# ---------------------------------------------------------------- the POPULATION and UNSCHEDULED
# The map's population is the roster, not the costed dataset: a row with no measured cost is a row
# this campaign still has to run, and dropping it silently is how a "complete" map covers 79% of the
# work. Read here rather than assumed, with the two controls the roster read owes.

# ⚠ newline="" DELIBERATELY, and it is the whole difference between a CR guard and a decoration.
# Path.read_text() and open() default to UNIVERSAL NEWLINES, which translate \r\n to \n before any
# caller sees them -- so `read_text().count("\r")` is 0 on a file that carries CR bytes on disk, and
# the guard below could never fire. Measured: a planted CR read 1 byte on disk, 0 through read_text(),
# 1 through newline="". Caught by the control for this guard, which returned exit 0 when it had to
# return non-zero; an unfireable check is worse than an absent one, because it prints reassurance.
with open(ROSTER, encoding="utf-8", newline="") as fh:
    roster_text = fh.read()
if roster_text.count("\r"):
    die(f"{ROSTER.name} carries {roster_text.count(chr(13))} CR byte(s) -- the roster is LF; a CRLF "
        f"smudge changes blob identity and every downstream diff")
roster_names = re.findall(r"^\| \[`([^`]+)`\]\(", roster_text, re.M)
if not roster_names:
    die(f"parsed 0 rows from {ROSTER.name} -- the row pattern is stale, fix it here")
roster_dups = sorted({n for n in roster_names if roster_names.count(n) > 1})
if roster_dups:
    die(f"duplicate roster rows: {', '.join(roster_dups)}")

costed = {n for n, _, _ in rows}
UNSCHEDULED = sorted(n for n in roster_names if n not in costed)
orphans = sorted(n for n in costed if n not in roster_names)

print(f"\npopulation:       {len(roster_names)} banked roster row(s)")
print(f"  costed          {len(costed)}  ({100*len(costed)/len(roster_names):.1f}%)")
print(f"  UNSCHEDULED     {len(UNSCHEDULED)}  -- no measured t_r, NO COST CLAIMED for any of them")
if orphans:
    print(f"  !! costed rows not on the roster (retired/renamed): {len(orphans)}: {', '.join(orphans)}")
if len(costed) + len(UNSCHEDULED) != len(roster_names):
    die(f"population arithmetic does not close: {len(costed)} costed + {len(UNSCHEDULED)} "
        f"unscheduled != {len(roster_names)} roster rows")

# ---------------------------------------------------------------- the reserved set
# TWO ideas, and only one of them is this script's to decide:
#   1. The $longTimeouts floor packages -- DERIVED from run-validated-sweep.ps1 AT GENERATION TIME,
#      never copied. A copied list drifted twice in the map's short life (crypto/tls joined the
#      table, two floors moved) before this derivation replaced it.
#   2. BIG_ROWS -- rows pinned for raw wall time rather than a timeout floor. An explicit, visible
#      editorial choice, kept separate so nobody mistakes it for the derived half.
sweep_text = SWEEP.read_text(encoding="utf-8")


def brace_matched_table(src, name):
    """The text inside $<name> = @{ ... }, balanced -- NOT the first '}' after it.

    A non-greedy `@\\{(.*?)\\}` stops at the first closing brace, so ONE nested `@{ }` truncates the
    capture and every entry after it is lost while both of the old guards still passed. Measured on
    the live 11-floor table: the nested shape DESIGN-peros-roster.md section 7 documents yields 6 of
    11 floors, silently; nested second, 1 of 11; nested first, 0 and loud. Balanced matching removes
    the class rather than the instance.
    """
    anchor = re.search(r"\$" + re.escape(name) + r"\s*=\s*@\{", src)
    if not anchor:
        die(f"cannot derive the reserved set: no ${name} table in {SWEEP.name}")
    i = anchor.end()
    depth = 1
    while i < len(src) and depth:
        if src.startswith("@{", i) or src[i] == "{":
            depth += 1
            i += 2 if src.startswith("@{", i) else 1
            continue
        if src[i] == "}":
            depth -= 1
            i += 1
            continue
        i += 1
    if depth:
        die(f"${name} table is unbalanced in {SWEEP.name} -- refusing rather than truncating")
    return src[anchor.end():i - 1]


table = brace_matched_table(sweep_text, "longTimeouts")
_floors = re.findall(r"'([^']+)'\s*=\s*'[^']+'", table)
# An INDEPENDENT count, from a second parser over the same text: every quoted key at this table's top
# level, whatever its value shape. A cardinality assert whose expected value comes from the same
# parse it is checking cannot fail -- the class this whole cut is about.
_keys = re.findall(r"'([^']+)'\s*=", table)
if not _floors:
    die(f"${'longTimeouts'} parsed EMPTY from {SWEEP.name} -- the pattern is stale, fix it here")
if len(_floors) != len(_keys):
    die(f"$longTimeouts: {len(_floors)} floor(s) parsed as 'name' = 'value' but {len(_keys)} key(s) "
        f"present ({', '.join(sorted(set(_keys) - set(_floors)))} did not match the scalar shape). "
        f"A nested or non-scalar entry is silently dropped by the scalar pattern -- see the BOARD "
        f"entry 2026-09-13 and DESIGN-peros-roster.md section 7.")

# ---- the relocation arcs, DERIVED from the one data file, never carried here ----------------
# A hop RE-PATHS rows, and a floor keyed by a 1.23 name reaches nothing at 1.24. `$longTimeouts`
# names `crypto/internal/mlkem768`, which does not exist at the version tip -- so without this the
# row's successors run at the sweep's DEFAULT deadline and are killed short, and step 2's intersect
# drops the pin (reported, but a report is not a floor).
#
# ⚠ THE MAP IS READ, NOT COPIED, and it is read from ONE file. C1's table (mailbox 350a301a, ruled
# at 37c10514a) is 10 rows / 14 arcs / 11 targets -- `crypto/internal/fips140test` receives THREE --
# so a source→target DICT silently drops arcs: four of the ten SPLIT. One line per arc is the only
# shape that cannot lose one. i9's wrapper reads this same file by this same contract, so the two
# derivations cannot disagree about the map.
#
# Per e0d5121e2 section 1 EVERY arm of a split INHERITS the floor: a budget copied is an
# over-estimate, which is the safe direction; a budget split is a guess.
RELOCATIONS_TSV = HERE / "relocations.tsv"
if not RELOCATIONS_TSV.exists():
    die(f"no relocation map at {RELOCATIONS_TSV.name} -- the reserved set's floors are keyed by "
        f"1.23 names and this file is how they reach their 1.24 successors. It lands with the "
        f"roster seat; refusing rather than scheduling a hop's successors at the default deadline.")
# Read with the file's own idiom -- open(..., newline="") -- so the CR check below sees the
# bytes as they are. Path.read_text() grew a newline= keyword only in 3.13 and would either
# TypeError here or, worse, translate the newlines out from under the check.
with open(RELOCATIONS_TSV, encoding="utf-8", newline="") as _fh:
    _reloc_text = _fh.read()
if _reloc_text.count("\r"):
    die(f"{RELOCATIONS_TSV.name} carries {_reloc_text.count(chr(13))} CR byte(s) -- LF only, for the "
        f"same reason the timings basis is: a CR rides into every name it touches.")
_reloc_lines = [ln for ln in _reloc_text.split("\n") if ln.strip()]
if not _reloc_lines or _reloc_lines[0].split("\t") != ["source", "target"]:
    die(f"{RELOCATIONS_TSV.name} must open with the header 'source\\ttarget' read BY NAME; saw "
        f"{_reloc_lines[0] if _reloc_lines else '(empty file)'!r}")
RELOCATIONS = []
for _ln in _reloc_lines[1:]:
    _cells = _ln.split("\t")
    if len(_cells) != 2 or not _cells[0].strip() or not _cells[1].strip():
        die(f"{RELOCATIONS_TSV.name}: every line is exactly source<TAB>target, one per ARC; saw {_ln!r}")
    RELOCATIONS.append((_cells[0].strip(), _cells[1].strip()))
# The thin guard, with its reason beside it exactly as the floors' own has. Ten rows relocate, so a
# map with fewer than ten ARCS cannot even name each source once -- and a short read here is silent:
# it would simply inherit fewer floors, which reads identical to a hop that relocated fewer rows.
if len(RELOCATIONS) < 10:
    die(f"{RELOCATIONS_TSV.name} yielded {len(RELOCATIONS)} arc(s); ten rows relocate at this hop and "
        f"four of them SPLIT, so fewer than ten arcs cannot name each source once. Refusing rather "
        f"than inheriting a partial map -- a short read is indistinguishable from a smaller hop.")

BIG_ROWS = ["go/doc/comment", "go/types"]
# A successor inherits its source's floor. Order is preserved and duplicates are skipped, so a target
# reached from two sources (fips140test, from three) is declared once.
_inherited = []
for _src, _tgt in RELOCATIONS:
    if _src in _floors and _tgt not in _floors and _tgt not in _inherited:
        _inherited.append(_tgt)
RESERVED_DECLARED = _floors + _inherited + [b for b in BIG_ROWS if b not in _floors and b not in _inherited]
print(f"\nreserved set derived at generation time: {len(_floors)} floor row(s) "
      f"({', '.join(_floors)}) + {len(_inherited)} inherited by successors "
      f"({', '.join(_inherited) if _inherited else 'none'}) + {len(BIG_ROWS)} big row(s)")
print(f"  relocation map: {len(RELOCATIONS)} arc(s) over "
      f"{len({s for s, _ in RELOCATIONS})} source(s) -> {len({t for _, t in RELOCATIONS})} target(s), "
      f"from {RELOCATIONS_TSV.name}")

# step 2 of the construction, AS WRITTEN: R := reserved set INTERSECT rows. The fallout is reported
# rather than asserted away -- a pinned row with no cost is not scheduled, and saying so is the whole
# difference between this and pretending it is.
byname = {n: (v, t) for n, v, t in rows}
RESERVED = [r for r in RESERVED_DECLARED if r in byname]
reserved_unscheduled = [r for r in RESERVED_DECLARED if r not in byname]
# ⚠ TWO CAUSES REACH THIS LIST, and one sentence for both names the wrong one for half of them.
# A declared reserved row is absent from the basis either because it WAS NOT MEASURED, or because
# ITS NAME DID NOT EXIST TO MEASURE: the basis is taken at the OLD release, so a successor this hop
# inherits a floor FOR is necessarily absent from it -- not a gap in the measurement, a name that
# post-dates it. The two want opposite reactions. The first is a hole the recon leg fills; the
# second is already right, because the floor rides on the predecessor row, which IS costed and IS
# pinned. Printed apart so the reader reacts to the cause they actually have.
_reloc_targets = {t for _, t in RELOCATIONS}
_unnamed = [r for r in reserved_unscheduled if r in _reloc_targets]
_uncosted = [r for r in reserved_unscheduled if r not in _reloc_targets]
if _unnamed:
    print(f"  !! {len(_unnamed)} declared reserved row(s) DID NOT EXIST at the release this basis "
          f"was measured on -- each inherited its floor from a predecessor that IS costed and IS "
          f"pinned, so the pin is carried under the old name (see {RELOCATIONS_TSV.name}): "
          f"{', '.join(_unnamed)}")
if _uncosted:
    print(f"  !! {len(_uncosted)} declared reserved row(s) have NO measured cost and are "
          f"UNSCHEDULED, not pinned: {', '.join(_uncosted)}")
    print(f"    (the reserved leg's total below therefore EXCLUDES them -- it is a lower bound on "
          f"the pin, not the pin)")

reserved_rows = [(n, byname[n][0], byname[n][1]) for n in RESERVED]
reserved_total = sum(t for _, _, t in reserved_rows)
bulk = [r for r in rows if r[0] not in set(RESERVED)]
bulk.sort(key=lambda r: (-r[2], r[0]))          # DESC by t, name tiebreak -> deterministic
if len(rows) != len(RESERVED) + len(bulk):
    die(f"checksum: {len(rows)} costed rows != {len(RESERVED)} reserved + {len(bulk)} bulk")
print(f"\nreserved set: {len(RESERVED)} row(s), {reserved_total} s "
      f"({reserved_total/60:.1f} min) pinned to the i9")
print(f"bulk set:     {len(bulk)} row(s), {total-reserved_total} s")

# ---------------------------------------------------------------- fleet
# Provisional speed factors (i9 = 1.00) -- PLACEHOLDERS pending hop-recon calibration; LANES.md marks
# historical cross-machine ratios SUSPECT. The engaged set is the four NAMED boxes (ruling section 3);
# C1 and C2 are dispatch-only, since the os-matrix census compiles and cannot run a row.
MACHINES = {
    "i9-13900K (sweeper)":      1.00,
    "6850U R (R-LAPTOP)":       0.45,
    "i7-5820K (coordinator)":   0.35,
    "6650U G (G-LAPTOP)":       0.35,
}
FLEETS = {
    3: ["i9-13900K (sweeper)", "6850U R (R-LAPTOP)", "i7-5820K (coordinator)"],
    4: ["i9-13900K (sweeper)", "6850U R (R-LAPTOP)", "i7-5820K (coordinator)",
        "6650U G (G-LAPTOP)"],
}

# The SLICE CAP, ruled by the coordinator at mailbox `4327ab7e1` §2: 40 minutes, with a ten-minute
# cooldown between slices.
#
# ⚠ THIS WAS 90 MINUTES AND THAT WAS STALE, not a preference (found 2026-09-13 while designing the
# dispatch driver). The 90-minute target predates the cap ruling, and the difference is not cosmetic:
# the i9's reserved leg is 4,722 s, so ceil(4722/5400) = 1 and ceil(4722/2400) = 2. The report printed
# `shards@90min=1` -- i.e. RUN THE RESERVED LEG UNSLICED -- which is precisely the plan section 4 of
# `e0d5121e2` calls "a plan the hardware refuses" on a box with a recorded thermal death. A driver
# trusting the report's own shard column would have done the one thing the cap exists to prevent.
#
# ⚠ AMENDED 2026-09-13 AT THE RECON BASIS, and the worked example above INVERTS -- kept rather than
# rewritten, because the reasoning is what earned the cap. That 4,722 s reserved leg is the OLD basis
# (windows, 18770d083). Re-derived from the recon's pass-1 TSV at the campaign's own corpus the same
# leg is 1,724 s = 28.7 min, so ceil(1724/2400) = 1 and the reserved leg now DOES run in one slice --
# not because the cap moved but because the leg is 63% smaller than the figure the old basis gave. The
# cap still governs and 28.7 min sits inside it. The lesson survives the inversion intact: the shard
# count and its label must both be derived, because the number that made "unsliced" wrong in August is
# the number that makes it right in September.
#
# The cooldown is NOT part of the cap: it is the gap BETWEEN slices and it belongs to the caller
# (`-ShardCount`'s own comment says so, and the P5 amendment records that a sliced run is not a
# substitute for the discipline). It is carried in the emitted plan so the driver does not re-derive it.
C_TARGET = 40 * 60          # slice cap, local wall seconds  (ruled: 40 min)
COOLDOWN_SECONDS = 10 * 60  # gap BETWEEN slices, the caller's to honour  (ruled: 10 min)


def lpt(W):
    names = FLEETS[W]
    s = {m: MACHINES[m] for m in names}
    load = {m: 0.0 for m in names}          # i9-seconds
    pkgs = {m: [] for m in names}
    i9 = names[0]
    for n, v, t in reserved_rows:           # step 2: reserved pinned to the i9
        load[i9] += t
        pkgs[i9].append((n, t, True))
    for n, v, t in bulk:                    # step 4: LPT-greedy, smallest projected LOCAL time
        target = min(names, key=lambda m: (load[m] / s[m], m))
        load[target] += t
        pkgs[target].append((n, t, False))
    return names, s, load, pkgs


def fmt_hm(sec):
    return f"{sec/60:.1f} min"


for W in sorted(FLEETS):
    names, s, load, pkgs = lpt(W)
    makespan = max(load[m] / s[m] for m in names)
    print(f"\n{'='*100}\nW = {W}   makespan >= {makespan:.0f} s local = {fmt_hm(makespan)}"
          f"   !! LOWER BOUND: {len(UNSCHEDULED)} roster row(s) carry no cost and are not in it")
    for m in names:
        local = load[m] / s[m]
        shards = max(1, -(-load[m] // (s[m] * C_TARGET)))  # ceil
        print(f"\n  {m}  (s_w={s[m]:.2f})  rows={len(pkgs[m])}  "
              f"load={load[m]:.0f} i9-s  local={local:.0f} s ({fmt_hm(local)})  "
              # ⚠ THE LABEL IS DERIVED FROM C_TARGET, and it used to be the literal "90min". C2 moved
              # C_TARGET from 90 to 40 minutes and left the label behind, so the COUNT was computed at
              # 40 and ANNOUNCED as 90: a reader dividing the printed load by 90 minutes gets a
              # different answer and concludes the script is wrong. A hardcoded label beside a derived
              # number is the same defect class as a hardcoded verdict string, and it survived a cut
              # and a review.
              f"shards@{C_TARGET/60:.0f}min={int(shards)}")
        items = [f"{n}{'*' if r else ''}[{t}]" for n, t, r in pkgs[m]]
        line = "    "
        for it in items:
            if len(line) + len(it) > 118:
                print(line.rstrip(", "))
                line = "    "
            line += it + ", "
        print(line.rstrip(", "))
    # The checksum prints from the SAME variables the assert reads, so it cannot state an arithmetic
    # the assert would reject. It used to carry a hardcoded "7 reserved" against a real 11.
    n_assigned = sum(len(pkgs[m]) for m in names)
    if n_assigned != len(rows):
        die(f"assignment checksum: {n_assigned} rows assigned != {len(rows)} costed rows")
    print(f"\n  checksum: {n_assigned} row(s) assigned == {len(RESERVED)} reserved + {len(bulk)} bulk"
          f"   [+ {len(UNSCHEDULED)} UNSCHEDULED, not assigned]")

# ---------------------------------------------------------------- UNSCHEDULED, by name
print(f"\n{'='*100}\nUNSCHEDULED -- {len(UNSCHEDULED)} roster row(s) with NO measured t_r. "
      f"No cost is claimed for any of them.")
print("  The recon leg of H10 measures these on the calibration sweep and the map re-derives from")
print("  that DATA (ruling e0d5121e2 section 2: (B) now, (C) at recon; a nominal is refused in every")
print("  form, including 'upper bound'). Until then every makespan above is a LOWER BOUND.")
line = "    "
for n in UNSCHEDULED:
    mark = "!" if n in RESERVED_DECLARED else ""
    it = f"{n}{mark}"
    if len(line) + len(it) > 118:
        print(line.rstrip(", "))
        line = "    "
    line += it + ", "
print(line.rstrip(", "))
if reserved_unscheduled:
    print(f"  ! = a DECLARED RESERVED row that cannot be pinned for want of a cost "
          f"({len(reserved_unscheduled)} of them)")

# ---------------------------------------------------------------- sensitivity
print(f"\n{'='*100}\nSENSITIVITY (W={max(FLEETS)}): makespan vs. speed-factor perturbations")


def makespan_with(factors):
    saved = dict(MACHINES)
    MACHINES.update(factors)
    try:
        names, s, load, pkgs = lpt(max(FLEETS))
        return max(load[m] / s[m] for m in names)
    finally:
        MACHINES.update(saved)


base = makespan_with({})
print(f"  base (i9=1.00, R=0.45, i7=0.35, G=0.35): {base:.0f} s = {fmt_hm(base)}")
scenarios = {
    "slow laptops (R=0.35, G=0.25)": {"6850U R (R-LAPTOP)": 0.35, "6650U G (G-LAPTOP)": 0.25},
    "slow coordinator (i7=0.25)":    {"i7-5820K (coordinator)": 0.25},
    "fast laptops (R=0.55, G=0.45)": {"6850U R (R-LAPTOP)": 0.55, "6650U G (G-LAPTOP)": 0.45},
    "everything slow (R=0.35, i7=0.25, G=0.25)": {"6850U R (R-LAPTOP)": 0.35,
        "i7-5820K (coordinator)": 0.25, "6650U G (G-LAPTOP)": 0.25},
    "i9 degraded 20% (i9=0.80)":     {"i9-13900K (sweeper)": 0.80},
}
for label, f in scenarios.items():
    ms = makespan_with(f)
    print(f"  {label:45s}: {ms:.0f} s = {fmt_hm(ms)}  ({100*(ms-base)/base:+.0f}%)")

print("\nlower bounds:")
print(f"  i9 reserved-set floor (serial on i9): {reserved_total} s = {fmt_hm(reserved_total)}"
      + (f"  !! EXCLUDES {len(reserved_unscheduled)} uncosted pin(s)" if reserved_unscheduled else ""))
for W in sorted(FLEETS):
    cap = sum(MACHINES[m] for m in FLEETS[W])
    ideal = total / cap
    print(f"  W={W} perfect-balance bound (total/sum s_w = {total}/{cap:.2f}): "
          f"{ideal:.0f} s = {fmt_hm(ideal)}")
heaviest = max(rows, key=lambda r: r[2])
slowest = min(MACHINES.values())
print(f"  single-row floor on a {slowest:.2f} box: {heaviest[0]} {heaviest[2]}/{slowest:.2f} = "
      f"{heaviest[2]/slowest:.0f} s = {fmt_hm(heaviest[2]/slowest)} (why the reserved pin matters)")


# ---------------------------------------------------------------- the MACHINE-READABLE plan
#
# ⚠ EVERYTHING ABOVE IS A REPORT FOR A HUMAN AND A DRIVER MUST NOT PARSE IT. Measured 2026-09-13
# while designing the H10 dispatch driver, three ways it defeats a parser, each silent:
#
#   1. the row lists WRAP at column 118, so a line is not a record;
#   2. worker names carry SPACES and PARENTHESES ("i9-13900K (sweeper)"), so whitespace is not a
#      delimiter;
#   3. ⚠ THE SAME WORKER APPEARS IN EVERY `W` SECTION WITH A DIFFERENT ROW SET -- R holds 85 rows at
#      W=3 and 60 at W=4 -- so a driver grepping for its own worker name silently takes whichever
#      section comes first and dispatches 25 rows it was not assigned. That is the silent-subtraction
#      class, arriving through the report's shape rather than through anyone's mistake.
#
# So the plan is emitted as TSV with `W` as a COLUMN: one row per line, nothing wrapped, and a driver
# that does not state its W gets no rows at all rather than the wrong ones.
#
# The digest is the same principle the coordinator ruled for this script's INPUT (`e0d5121e2` section
# 5 -- refuse when the parse does not reproduce the declared digest) applied to its OUTPUT: the driver
# recomputes it over the rows it parsed and refuses a plan it cannot reproduce, so a truncated or
# hand-edited plan cannot dispatch.
#
# ⚠ THE SLICE PACKING IS FIRST-FIT-DECREASING, NOT THE REPORT'S LISTING ORDER, and the two differ
# visibly: the i9's reserved leg packs into 2 slices (walls 2370 / 2352 i9-s) under FFD and 3 slices
# (1471 / 2355 / 896) packed greedily in listing order. FFD is what the ruling's own arithmetic used
# ("two slices, one gap, +10 min over unsliced", `4327ab7e1` section 2), so FFD is what the plan must
# emit or the plan would contradict the ruling that sized it. A reader comparing the plan's order to
# the report's will therefore see different orders; that is intended and is why it is written here.
def slice_rows(items, cap_i9_seconds):
    """Pack (name, t, reserved) into slices, first-fit-decreasing, cap in i9-seconds.

    A row is INDIVISIBLE -- the sweep's unit of dispatch is a package -- so a row heavier than the
    cap gets a slice of its own rather than being split or refused. `crypto/dsa` at 1,317 s is the
    live case: it fits 2,400 but it is why no cap below 21.95 min can exist.
    """
    slices = []
    for item in sorted(items, key=lambda r: (-r[1], r[0])):
        for s in slices:
            if sum(x[1] for x in s) + item[1] <= cap_i9_seconds:
                s.append(item)
                break
        else:
            slices.append([item])
    return slices


def emit_plan(path):
    lines = []
    body = []
    for W in sorted(FLEETS):
        names, s, load, pkgs = lpt(W)
        for worker in names:
            cap_i9 = s[worker] * C_TARGET
            for slice_no, sl in enumerate(slice_rows(pkgs[worker], cap_i9), start=1):
                for seq, (name, t, reserved) in enumerate(sl, start=1):
                    body.append((W, worker, slice_no, seq, name, t, 1 if reserved else 0))

    # The digest covers exactly the fields a driver acts on, in the order it reads them, so a
    # reordering that changes dispatch changes the digest.
    h = hashlib.sha256()
    for r in body:
        h.update(("\t".join(str(x) for x in r) + "\n").encode("utf-8"))
    digest = h.hexdigest()

    lines.append("# go2cs H10 dispatch plan -- MACHINE-READABLE. Generated by shardmap.py.")
    lines.append("# Do not hand-edit: the driver recomputes #digest over the rows and refuses a mismatch.")
    lines.append("#version\t1")
    # ⚠ THE BASIS, NOT A FIXED BLOCK KEY. This line used to emit BLOCK_KEY unconditionally, so a plan
    # derived from the recon TSV would have carried the DATA block's label -- a plan that states the
    # provenance it does not have, which is worse than one stating none. `#block` is now emitted only
    # when the DATA block is what was actually read. The driver requires version/digest/rows/
    # slice_cap_seconds/cooldown_seconds and reads every other `#` line generically, so this is additive
    # for an existing driver rather than a format break.
    lines.append(f"#basis\t{BASIS}")
    if TIMINGS is None:
        lines.append(f"#block\t{BLOCK_KEY[0]}\t{BLOCK_KEY[1]}\t{BLOCK_KEY[2]}")
    lines.append(f"#slice_cap_seconds\t{C_TARGET}")
    lines.append(f"#cooldown_seconds\t{COOLDOWN_SECONDS}")
    lines.append(f"#rows\t{len(body)}")
    lines.append(f"#unscheduled\t{len(UNSCHEDULED)}")
    # Stated in the artifact itself, not only in the report, because the driver's refusal to treat a
    # projection as a deal is the whole point of ruling "say which it is, and gate dispatch on it".
    lines.append("#projection\tLOWER_BOUND\t"
                 f"{len(UNSCHEDULED)} roster row(s) carry no measured t_r and are NOT in this plan")
    lines.append(f"#digest\t{digest}")
    lines.append("W\tworker\tslice\tseq\tpackage\tt_r_i9_seconds\treserved")
    for r in body:
        lines.append("\t".join(str(x) for x in r))

    # newline="\n" explicitly: this file is READ BY POWERSHELL on Windows and the digest is computed
    # over LF-joined records. Letting the platform choose would make the same plan digest differently
    # on the box that writes it and the box that checks it -- the CR class this lane has already paid
    # for twice (the .gitattributes pins, and a guard defeated by universal newlines).
    Path(path).write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")
    print(f"\n{'='*100}\nplan written: {path}")
    print(f"  {len(body)} dispatch row(s) over W={sorted(FLEETS)}, digest {digest[:16]}...")
    print(f"  slice cap {C_TARGET} s ({C_TARGET/60:.0f} min), cooldown {COOLDOWN_SECONDS} s "
          f"({COOLDOWN_SECONDS/60:.0f} min), {len(UNSCHEDULED)} row(s) UNSCHEDULED and absent")


if "--emit-plan" in sys.argv:
    i = sys.argv.index("--emit-plan")
    if i + 1 >= len(sys.argv):
        die("--emit-plan needs a path")
    emit_plan(sys.argv[i + 1])
