"""Compute the hop-era fleet shard map from per-row sweep wall times.

Method: PLAN-hop-campaign.md section 4.3, generalized into GoCorpusMigration.md section 3.2 (the
maintained copy) -- reserved set pinned to the i9, remaining rows LPT-greedy across W bins weighted
by provisional speed factors s_w (i9 = 1.00).

⚠ WHAT THIS EMITS IS A PROJECTION, NOT A DEAL. s_w are PLACEHOLDERS pending hop-recon calibration,
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

HERE = Path(__file__).resolve().parent
DATA = HERE.parent / "DATA-sweep-row-walltimes.md"
ROSTER = HERE.parent.parent / "ValidatedTestPackages.md"
SWEEP = HERE.parent.parent.parent / "src" / "run-validated-sweep.ps1"

# The block this map is parameterized by, as a (OS, corpus SHA, machine) key rather than "the first
# one". Change this line to re-base the map, and the change is then visible in a diff.
BLOCK_KEY = ("windows", "18770d083", "i9-13900K")


def die(msg):
    raise SystemExit(f"shardmap: REFUSED -- {msg}")


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
    print(f"⚠ CONTENT UNVERIFIED: no digest declared for {BLOCK_KEY} in {DATA.name}. "
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
          f"sha256 {parsed_digest[:16]}…")

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
    print(f"  ⚠ costed rows not on the roster (retired/renamed): {len(orphans)}: {', '.join(orphans)}")
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

BIG_ROWS = ["go/doc/comment", "go/types"]
RESERVED_DECLARED = _floors + [b for b in BIG_ROWS if b not in _floors]
print(f"\nreserved set derived at generation time: {len(_floors)} floor row(s) "
      f"({', '.join(_floors)}) + {len(BIG_ROWS)} big row(s)")

# step 2 of the construction, AS WRITTEN: R := reserved set INTERSECT rows. The fallout is reported
# rather than asserted away -- a pinned row with no cost is not scheduled, and saying so is the whole
# difference between this and pretending it is.
byname = {n: (v, t) for n, v, t in rows}
RESERVED = [r for r in RESERVED_DECLARED if r in byname]
reserved_unscheduled = [r for r in RESERVED_DECLARED if r not in byname]
if reserved_unscheduled:
    print(f"  ⚠ {len(reserved_unscheduled)} declared reserved row(s) have NO measured cost and are "
          f"UNSCHEDULED, not pinned: {', '.join(reserved_unscheduled)}")
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
          f"   ⚠ LOWER BOUND: {len(UNSCHEDULED)} roster row(s) carry no cost and are not in it")
    for m in names:
        local = load[m] / s[m]
        shards = max(1, -(-load[m] // (s[m] * C_TARGET)))  # ceil
        print(f"\n  {m}  (s_w={s[m]:.2f})  rows={len(pkgs[m])}  "
              f"load={load[m]:.0f} i9-s  local={local:.0f} s ({fmt_hm(local)})  "
              f"shards@90min={int(shards)}")
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
      + (f"  ⚠ EXCLUDES {len(reserved_unscheduled)} uncosted pin(s)" if reserved_unscheduled else ""))
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
    print(f"  {len(body)} dispatch row(s) over W={sorted(FLEETS)}, digest {digest[:16]}…")
    print(f"  slice cap {C_TARGET} s ({C_TARGET/60:.0f} min), cooldown {COOLDOWN_SECONDS} s "
          f"({COOLDOWN_SECONDS/60:.0f} min), {len(UNSCHEDULED)} row(s) UNSCHEDULED and absent")


if "--emit-plan" in sys.argv:
    i = sys.argv.index("--emit-plan")
    if i + 1 >= len(sys.argv):
        die("--emit-plan needs a path")
    emit_plan(sys.argv[i + 1])
