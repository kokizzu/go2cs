# DESIGN — the H10 per-row dispatch driver

**Status:** design + implementation, offered for the coordinator's parse gate and i9's one-slice
acceptance. Ruled at mailbox `e0d5121e2` §7 — *"an H10 dispatch script that reads the emitted map and
runs a worker's rows in order with the cooldown gaps of §4 — C2 designs it (PowerShell, parse-gated
here), i9 accepts it on one slice"* — and cut now on `f30063650` §3 ⑤.

**Lane C2, 2026-09-13.** C2 cannot run PowerShell (no `pwsh`, no Desktop edition), so the `.ps1` here is
**static-checked only** and carries no claim of having executed. Python *does* run on this box, so
everything on the Python side of the seam below is measured, and the cross-language contract is measured
by mirroring the driver's own digest steps. What remains unmeasured is named as such in §7.

<!-- Provenance. Ruling chain, in order: e0d5121e2 §7 (the driver is the instrument debt; per-row
     dispatch; no -ShardCount row-list mode) -> e0d5121e2 §4 (the reserved leg SPLITS with cooldowns;
     "~10 minutes" as the slice, which was superseded) -> 6571/6925/6956 (i9 and C2 establishing that
     the ten minutes was the COOLDOWN and never the slice; that host's four completed shards ran
     20.6/26.7/37.2/27.0 min) -> 4327ab7e1 §2 (RULED: cap 40 minutes, ten-minute cooldown; two slices,
     one gap; a blast-radius limit, not a ceiling) -> f30063650 §3 (cut it now).
     Measured on this box the same day: the generator's report shape (three parser defeats), the stale
     C_TARGET, the FFD-vs-listing-order divergence, and the four digest-contract arms. -->

## 1. What it does

Reads the machine-readable plan, selects **one worker at one fleet size**, and runs that worker's rows
in `seq` order, slice by slice, through the sweep's own documented per-package interface
(`-Filter <package> -Exact`), inserting the ruled cooldown **between slices only**. Emits a per-row
timing TSV. Derives its verdict from the failure count and exits on it.

```
  pwsh src/run-h10-dispatch.ps1 -Plan <plan.tsv> -Worker 'i9-13900K (sweeper)' -FleetSize 3 [-OnlySlice 1] [-DryRun]
```

`-Plan`, `-Worker` and `-FleetSize` are **mandatory with no defaults**. §2.3 is why.

## 2. The decision that shapes everything: the driver does not parse the map report

`shardmap.py` emits **only** a human report on stdout — there is no machine-readable artifact at
`33c29952d`. Measured, that report defeats a parser three ways, each of them silent:

| # | Shape | What a parser does with it |
|---|---|---|
| 2.1 | Row lists **wrap at column 118** | a line is not a record; a row is lost at every wrap boundary |
| 2.2 | Worker names carry **spaces and parentheses** (`i9-13900K (sweeper)`) | whitespace is not a delimiter |
| 2.3 | **The same worker appears in every `W` section with a different row set** | a grep for one's own name takes whichever section comes first |

**2.3 is the decisive one.** `R-LAPTOP` holds **85** rows at `W=3` and **60** at `W=4`. A driver that
greps the report for its own worker name dispatches 25 rows it was not assigned, and nothing anywhere
reads wrong — the silent-subtraction class arriving through the artifact's *shape* rather than through
anyone's mistake.

**So `W` is a column in the plan and a mandatory parameter of the driver.** A driver that does not state
which fleet size it is dispatching gets **no rows and a refusal**, never the wrong ones. That is the
whole reason the plan exists rather than a regex.

### 2.4 The plan format

TSV, LF, one dispatch row per line, `#`-prefixed metadata, emitted by `shardmap.py --emit-plan <path>`:

```
#version           1
#block             windows   18770d083   i9-13900K
#slice_cap_seconds 2400
#cooldown_seconds  600
#rows              324
#unscheduled       42
#projection        LOWER_BOUND   42 roster row(s) carry no measured t_r and are NOT in this plan
#digest            417d89707b9362b4d8613008ce7c040c46b95d6cf7d478312328ebd4727c4827
W  worker                 slice  seq  package      t_r_i9_seconds  reserved
3  i9-13900K (sweeper)     1      1    crypto/dsa   1317            1
```

The cap and the cooldown are **read from the plan, not written in the driver**, so the ruling lives in
one place and a plan generated under a different cap dispatches under that cap.

`#projection` is printed on **every run**, not merely recorded here. §3.2's rule is *"say which it is,
and gate dispatch on it"*, and an operator comparing a wall clock against these costs has to know they
are a lower bound before concluding the box is slow.

## 3. ⚠ Two defects found while designing this, both fixed here

### 3.1 `C_TARGET` was 90 minutes — stale, and it inverted the ruling

The generator's shard column was computed against a 90-minute target predating the cap ruling. The
difference is not cosmetic:

```
  i9 reserved leg = 4,722 s
     ceil(4722 / 5400) = 1   <- what the report printed: shards@90min=1, i.e. RUN IT UNSLICED
     ceil(4722 / 2400) = 2   <- what the ruling requires: two slices, one gap
```

Running the reserved leg unsliced is precisely what `e0d5121e2` §4 calls *"a plan the hardware
refuses"* on a box with one recorded thermal death. **A driver trusting the report's own shard column
would have done the one thing the cap exists to prevent.** `C_TARGET` is now `40 * 60` with
`COOLDOWN_SECONDS = 10 * 60` beside it, both cited to the ruling at the site.

### 3.2 The slice packing is first-fit-decreasing, and it is *not* the report's listing order

The two disagree visibly, so this is recorded rather than left for a reader to trip over:

```
  packed greedily in the report's listing order : 3 slices, walls 1471 / 2355 / 896, leg 98.7 min
  first-fit-decreasing                          : 2 slices, walls 2370 / 2352, leg 88.7 min
```

FFD is what the ruling's own arithmetic used (`4327ab7e1` §2: *"two slices, one gap, +10 min over
unsliced"*), so FFD is what the plan must emit — packing in listing order would make the plan
contradict the ruling that sized it. A reader comparing the plan's order against the report's will see
different orders; that is intended.

**A row is indivisible** — the sweep's unit of dispatch is a package. `crypto/dsa` alone is 1,317 s,
which is why no cap below 21.95 min can exist and why a slice holding one over-cap row is legal rather
than an error.

## 4. The refusals, and why each is a refusal rather than a warning

| Refusal | Because the alternative |
|---|---|
| `#digest` does not reproduce over the plan's own rows | dispatching a truncated or hand-edited plan runs a set nobody derived |
| `#rows` disagrees with the parsed body count | a header/body disagreement in the generator, caught separately from damage |
| no rows at the given `-FleetSize` (names the sizes present) | §2.3 |
| no such worker at that size (names the workers present, quoted) | a name typo would otherwise read as "no work assigned" |
| no such slice (names the slices present) | same |
| plan `#version` ≠ 1 | a format change must not be read under the old contract |
| a row without exactly 7 fields | a partial line is not a record |

The digest gate is the principle the coordinator ruled for the generator's **input** (`e0d5121e2` §5 —
refuse when the parse does not reproduce the declared digest) applied to its **output**. It runs
**before any row is selected**, so a bad plan cannot dispatch even one package.

Each refusal **names what is available**. A bare "no rows" sends the reader back to read the plan by
eye, which is the manual step the digest and the mandatory `-FleetSize` exist to remove.

## 5. The cooldown

Ten minutes, from the plan, **between slices and never after the last** — a trailing sleep delays a
result and protects nothing. Written as an explicit ordinal test rather than an "if not last", because
an off-by-one there costs ten minutes of every run and would never look wrong.

`-CooldownSecondsOverride 0` is accepted for the ramp experiment that `4327ab7e1` gave a slot inside the
recon leg, and is **logged as a departure at every slice boundary** — a run without gaps cannot be
quietly mistaken for a run with them.

## 6. What is measured, and by what

Everything on the Python side, and the cross-language contract:

```
  emission at the tip                 rc=0, 324 dispatch rows over W={3,4}, 42 UNSCHEDULED and absent
  plan invariants                     digest reproduces; #rows == body; 0 slices over their cap;
                                      reserved set 11 rows / 4,722 i9-s
  the ruled arithmetic reproduced     i9 W=3 slices 2370 + 2352 i9-s = 2 slices, 1 gap, 88.7 min
  the 2.3 hazard now explicit         R-LAPTOP: 85 rows at W=3, 60 at W=4, both in the data
  digest contract, arm 1              the driver's steps mirrored in Python reproduce the declared digest
  digest contract, arm 2 (CRLF)       a Windows checkout (335 CR bytes) digests IDENTICALLY
  digest contract, arm 3 (3 controls) one t_r changed / one row deleted / two rows SWAPPED
                                      -> all three change the digest, so the gate fires
  static check of the .ps1            0 findings; and all five checker classes planted IN THIS FILE
                                      go red, pristine green, restore byte-identical
  encoding                            the .ps1 is pure ASCII (see 7.2)
```

The reorder arm matters on its own: a reordering changes **dispatch order** without changing the row
set, so a digest over an unordered set would have missed it.

### 6.1 AMENDMENT 2026-09-20 — the figures above are the PRE-RECON plan; the landed plan differs

**Nothing above is rewritten.** This block is added because §6's measurements were taken against the
plan as it stood *before* the H10 recon leg re-based the map, and a reader who checks them against
`docs/phase4/hopA-inputs/h10-dispatch-plan.tsv` will find every headline number different. The
measurements were correct when they were taken; they describe a plan that no longer exists.

```
                                   §6 above (pre-recon)      the LANDED plan (master, leg (4))
  dispatch rows                    324                       424
  UNSCHEDULED                       42                        14
  reserved set                      11 rows / 4,722 i9-s      11 rows /   624 i9-s
  W=3 makespan                      88.7 min (2 slices)       88.4 min (3 shards @ 40 min)
  rows per fleet size               --                        212 at W=3, 212 at W=4
  #digest                           --                        005aeab497fd35e8...
```

**Why the row count ROSE while UNSCHEDULED fell.** The recon leg measured rows the pre-recon map had
no cost for, so 28 of the 42 moved out of UNSCHEDULED and into the plan; the dispatch-row total is
higher again because the landed plan emits a row per *(W, worker, slice, seq)* across **both** fleet
sizes — 212 + 212 — where the earlier one carried fewer scheduled rows to place.

⚠ **The reserved set's seconds are the figure most likely to mislead**, because the ROW COUNT is
unchanged at 11 and only the cost moved: 4,722 → 624 i9-s. It is the same eleven floor-and-inherited
rows; what changed is that four DECLARED RESERVED rows still carry no measured cost and so cannot be
pinned, which the landed plan states rather than absorbs. **So 624 s is a lower bound on the pin and
not the pin**, and it is not comparable to 4,722 as if the same quantity had shrunk.

**What does NOT change, and is why §6 is amended rather than replaced:** every *contract* it
measures still holds and was re-verified at the merge — the digest reproduces over all 424 rows from
the merged tree, `#rows` equals the body, no slice exceeds its cap, and the three digest-control arms
(one `t_r` changed, one row deleted, two rows swapped) remain the reason the gate fires. **The arms
are sound; only the numbers they were run on are historical.**

⚠ §2.4's plan-format example carries the same pre-recon `#rows 324` / `#unscheduled 42`. It is
**illustrative of the FORMAT** and is deliberately left alone: changing it would make the worked
example disagree with the prose around it for no gain, and this block is the notice that its figures
are not the live plan's. A reader wanting live numbers reads the plan file, which is authoritative.

*(Amended by C2 per COORD `0cb09c354`; a dated block, never a rewrite — `docs/phase4/` records are
point-in-time. A read, not a compile: the landed figures are read from the committed plan file and
from `shardmap.py --timings` output at leg (4), not re-generated here.)*

## 7. ⚠ What is NOT measured, stated so no reading implies it

1. **The `.ps1` has never executed.** C2 has no PowerShell. It is statically checked for brace/paren
   balance, inline-`(if …)` expressions, 5.1-incompatible constructs (`??`, ternary, `-Parallel`,
   `Join-String`), `$name:` scope qualifiers and `[switch]` shadowing — and the checker was made to go
   red on this file for every one of those classes before its green was believed. That is a parse-shaped
   guarantee, not an execution one. **The coordinator's i7 parse gate in both editions is the next
   arm**, and i9's one-slice `-DryRun` after it.
2. **No self-test is offered, deliberately.** A `--self-test` I cannot run is worse than none: it would
   be an untested remedy wearing a guard's name, which is the class three lanes hit tonight. `-DryRun`
   is the acceptance vehicle instead — it is decidable without a sweep and prints every row, slice
   boundary and cooldown it would take.
3. **`$hop`/`$Hop` case-insensitivity is remembered**, per the coordinator's note: PowerShell variable
   names are case-insensitive, and a `[switch] $Hop` shadowed by a `$hop = 0` counter threw
   `ArgumentTransformationMetadataException` under `Stop` and killed every run of the sweep. The driver
   has no name pair differing only in case; the checker asserts it.
4. **The plan's costs are a projection, not a deal** — 42 roster rows carry no measured `t_r` and are
   absent, so every makespan is a lower bound, and `t_r` is a full-roster measurement while this driver
   dispatches per row (i9's units finding, `1b36cef9d` §3). The recon leg re-measures all 204/227 under
   the dispatch mode at the campaign's own corpus and the map re-derives from that; `#projection` says
   so on every run until then.
5. **Encoding.** The `.ps1` is pure ASCII. PS 5.1 reads a BOM-less `.ps1` as ANSI rather than UTF-8, and
   the first draft carried `⚠` in two `Write-Host` strings — the one position where mangling is visible
   to an operator. `run-validated-sweep.ps1`'s own eight non-ASCII characters are all in **comments**,
   which is the safe convention; the driver now needs no convention at all.

## 8. Files

| File | Change |
|---|---|
| `src/run-h10-dispatch.ps1` | new, the driver |
| `docs/phase4/hopA-inputs/shardmap.py` | `--emit-plan <path>`; `C_TARGET` 90 → 40 min with `COOLDOWN_SECONDS` beside it; `slice_rows` (FFD, indivisible rows) |
| `docs/phase4/DESIGN-h10-dispatch-driver.md` | new, this record |

The generator change is **not** scope creep: a driver with nothing machine-readable to read is
untestable, and an untested driver is what this fleet refuses. It is kept to the minimum that makes the
driver measurable — an emission and a stale constant — and both halves are tested here.

**Branch note:** this cut is parented on `claude/c2-shardmap-repair` `33c29952d`, C2's own unlanded
repair seat, because it edits the same generator. That is the declared-stack chain the coordinator ruled
for i9's BOARD block on `68ad83c2c` and R's block on C2's `191164e7a`: the assembly table carries
`stack-on=<the 33c29952d row>`, the census reports a declared STACK, nothing refuses.
