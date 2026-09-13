# DATA — the H10 shard map re-derived at the campaign's own corpus

> Lane `C2`, 2026-09-13. **A re-derivation, not a new method.** Coordinator ruling `dd9ea4a1d` §2d:
> re-derive the map from the recon's pass-1 TSV, `net` dropped, **no mode correction and no setup
> term**, ordering the light bulk by ROW COUNT with the heavy rows placed first. The method is
> unchanged — `GoCorpusMigration.md` §3.2, reserved set pinned to the i9, LPT across `W` bins weighted
> by `s_w`. **Only the basis moved**, and this record is what moved with it.
>
> **Status: DERIVED, not banked as a deal.** `s_w` are still placeholders, so the makespan is still a
> projection (§3.2: *"a map built at placeholder factors is a projection, not a deal"*). What changed is
> that the projection now covers 99.5% of the roster instead of 79.4%.

## 0. The one-paragraph version

The map was parameterized by a 2026-08-23 windows measurement at corpus `18770d083`, which costed **162
of 204** roster rows. The recon re-measured the whole roster at the campaign's own corpus `a02ac3df3`,
so the basis is now **203 of 204** (`net` alone excluded, and excluded *because* its figure is a lower
bound produced by a person stopping the run, not a cost). Two conclusions reverse on that change, and
both are decisions rather than numbers. **First, a fourth machine now buys something: it bought nothing
at the old basis** — `W=3` and `W=4` both projected 78.7 min because the i9's pinned reserved leg WAS
the critical path — **and at the campaign corpus `W=4` beats `W=3` by 9.2 minutes.** Second, **the
reserved leg fits one 40-minute slice**: 4,722 s → 1,724 s, so `ceil(t/2400)` goes from 2 to 1. The
ruled assignment rule is confirmed rather than merely assumed: `t_r` is floor-dominated (**134 of 203
rows within 10 s of the 10 s floor**), and LPT lands **0.06%** above the perfect-balance bound at
`W=3`.

## 1. Inputs, each named with its SHA so this is re-derivable and not re-assertable

| input | where | identity |
|:--|:--|:--|
| per-row costs | `hopA-inputs/recon-pass1-20260913T123456Z-reparsed.tsv` | i9's `fd3f22f3bb6d4a0fc9cb2635c3c652816c14d903` (`claude/i9-data-recon-2026-09-13`), file sha256 `83a07af7cc1e42aa5a8d59028fbdf9bd06fa7a8a87915f00ab4ce61879fed6cd` |
| generator | `hopA-inputs/shardmap.py` | this branch |
| roster, reserved floors | `ValidatedTestPackages.md`, `src/run-validated-sweep.ps1` `$longTimeouts` | read at generation time, never copied |

**The TSV is read, not transcribed.** i9 flagged as a judgement call that it did *not* add the 204-row
table to `DATA-sweep-row-walltimes.md`, because a table beside the TSV that already holds those rows is
two sources of truth that will drift. Teaching the generator to read the banked TSV (`--timings`)
settles that call in the same direction: the rows keep one home.

⚠ **This branch does NOT carry the TSV.** It is i9's file on i9's branch, and copying it here would
recreate exactly the duplication above. So `--timings` has **no default and no fallback**: a path that
does not resolve REFUSES rather than silently reverting to the other basis — the two bases differ by 24%
in total seconds and 60% in coverage, and a run that quietly swapped them would look healthy. Until
train 49 lands both branches, reproduce by extracting the TSV from i9's commit:

```bash
git show claude/i9-data-recon-2026-09-13:docs/phase4/hopA-inputs/recon-pass1-20260913T123456Z-reparsed.tsv > /tmp/t.tsv
python3 docs/phase4/hopA-inputs/shardmap.py --timings /tmp/t.tsv --emit-plan /tmp/plan.tsv
```

## 2. The two bases, side by side

| reading | OLD: windows · `18770d083` · i9 · 2026-08-23 | NEW: recon pass 1 · `a02ac3df3` | |
|:--|--:|--:|:--|
| rows costed | 162 | **203** | of 204 roster rows |
| coverage | 79.4% | **99.5%** | |
| UNSCHEDULED | 42 | **1** | `net` only |
| total i9-seconds | 7,701 (128.3 min) | **6,190 (103.2 min)** | −20% |
| median row | 10.0 s | **16 s** | |
| mean row | 47.5 s | **30.5 s** | −36% |
| reserved leg (pinned, i9) | 11 rows, 4,722 s (78.7 min) | **12 rows, 1,724 s (28.7 min)** | −63% |
| bulk set | 151 rows, 2,979 s | **191 rows, 4,466 s** | |
| makespan `W=3` | 78.7 min | **57.4 min** | |
| makespan `W=4` | 78.7 min | **48.2 min** | |

**The reserved leg gains a row while losing 63% of its seconds.** It gains one because `net/http` had no
measured cost at the old basis and has one now (238 s), so it is genuinely pinned instead of silently
dropped; `net` is still the one declared floor that cannot be pinned, and the leg's total is still
stated as a lower bound on the pin for that reason.

## 3. ⚠ Reversal one: the fourth machine bought nothing, and now it does

At the old basis `W=3` and `W=4` produced **the same 78.7 min**, and the reason is visible in the
figure itself: `4,722 s` is *exactly* the reserved leg. The i9's pin **was** the critical path, so
adding a worker moved bulk off machines that were not binding and changed the answer by zero.

At the campaign corpus the reserved leg is 1,724 s and the i9's total load is 3,441 i9-s, so the i9
carries bulk as well and the binding constraint is **balance rather than the pin**:

| fleet | makespan | perfect-balance bound | LPT overshoot |
|:--|--:|--:|--:|
| `W=3` | 3,441 s = 57.4 min | 3,439 s = 57.3 min | **+0.06%** |
| `W=4` | 2,893 s = 48.2 min | 2,879 s = 48.0 min | **+0.5%** |

**So engaging the fourth box is worth 9.2 minutes per pass, where at the old basis it was worth
nothing.** That is a fleet decision the stale basis was answering wrongly, and it is the strongest
argument in this record for re-deriving rather than carrying a number.

## 4. The ruled assignment rule, confirmed by the basis rather than assumed

The ruling's premise was that `t_r` is floor-dominated, so the light bulk should be balanced by ROW
COUNT with the heavy rows placed first. The basis says so outright:

```
  floor                     10 s
  within 10 s of the floor  134 of 203 rows (66%)
  median                    16 s
  p75 / p90 / p95           25 / 53 / 102 s
  max                       400 s (crypto/tls)
  heaviest 12 rows          2,277 s = 36.8% of the total over 5.9% of the rows
```

Two thirds of the roster is within 6 s of a 10 s floor, and a twentieth of it carries **over a third**
of the cost. **LPT on that shape lands 0.06% above the bound at `W=3`, which is the useful part of the
finding: there is nothing left for a cleverer scheduler to win.** The heavy-rows-first half is what
does the work; the light bulk's order is noise, so counting rows is not an approximation to balancing
`t_r` — at this distribution it *is* balancing `t_r`.

**No mode correction and no setup term**, as ruled. Pass 2 is the one-axis arm that removed mode from
the confound, and the withdrawn setup model would have added a per-dispatch constant that measurement
says does not exist.

## 5. Per-worker assignment

```
W = 3   makespan >= 3,441 s local = 57.4 min
  i9-13900K (sweeper)      s_w=1.00   rows=110   load=3,441 i9-s   local=57.4 min
  6850U R (R-LAPTOP)       s_w=0.45   rows= 52   load=1,545 i9-s   local=57.2 min
  i7-5820K (coordinator)   s_w=0.35   rows= 41   load=1,204 i9-s   local=57.3 min

W = 4   makespan >= 2,893 s local = 48.2 min
  i9-13900K (sweeper)      s_w=1.00   rows= 86   load=2,877 i9-s   local=48.0 min
  6850U R (R-LAPTOP)       s_w=0.45   rows= 47   load=1,302 i9-s   local=48.2 min
  i7-5820K (coordinator)   s_w=0.35   rows= 36   load=1,006 i9-s   local=47.9 min
  6650U G (G-LAPTOP)       s_w=0.35   rows= 34   load=1,005 i9-s   local=47.9 min
```

Every load above was re-derived **from the emitted plan file** by an independent sum, not read from the
generator's own prints, and both agree exactly. The emitted plan carries `#rows 406` (203 rows × two
fleets) and `#digest a1fd9307ee95b238660028e6d11690cdfd9d9577e39c0299edbad6b1fd2794ec`.

## 6. What is NOT measured, stated as plainly as what is

- **`s_w` are still placeholders.** Every `local` minute above is a projection. The recon measured the
  i9; the other three factors are unchanged guesses, and the `W=4` advantage in §3 rests on them.
- **`net` has no cost in either mode.** Both its figures are lower bounds from a hand-stopped run. One
  later run, alone, with the grandchild sampled and nothing killed, is the instrument — i9's item, after
  the rung.
- **`crypto/rsa`'s 27 s is its ISOLATED cost** and is used as such. Its in-sweep verdict differs, and
  i9's `f5c401954` establishes that failure is a generator crash on a project in its build closure
  rather than a property of the mode, so it is not a cost difference and no correction is applied.
- **No pass-2 figure is used.** Pass 2 is the one-axis record that killed the setup model; pass 1 is the
  basis, per the ruling. The two agree on 190 of 199 comparable rows.
- **The makespan remains a LOWER BOUND** while any roster row carries no cost, and the generator prints
  it as one. One row does.
- ⚠ **NO PLAN FROM THIS GENERATOR HAS BEEN THROUGH THE DRIVER.** §7's claim that the `#basis` header is
  *additive* for `run-h10-dispatch.ps1` is **read from the driver's source** — it requires
  `version/digest/rows/slice_cap_seconds/cooldown_seconds` and collects every other `#` line generically
  — and C2 cannot execute a `.ps1`, so it is not measured. i9's 16-arm acceptance ran against a plan from
  the *old* header (`#block`, no `#basis`), so the recon-basis plan's header is an untested input to a
  tested driver. **One `-DryRun` arm against a recon-basis plan closes it**, and this belongs in the list
  rather than in a reader's assumption: a "what is not measured" section that omits the thing the author
  changed is the section doing the least work.

## 7. Generator changes this re-derivation required, and one defect it exposed

1. **`--timings <tsv>`** — the recon basis, read **by column name and never by position** (pass 1 has
   seven columns, pass 2 has five; a positional read of `sweep_s` silently takes a different quantity
   from the other file). Controlled by reordering the columns and asserting the map comes out identical.
2. **`HAND_STOPPED` is dropped by name, and the drop is asserted to have fired.** A drop list that
   quietly matches nothing is the tolerance-become-dead-code shape: the day the banked TSV renames that
   row, the map would schedule on it and print the same reassuring line.
3. **The duplicate row is reported, not folded.** `archive/tar` appears twice (17 s and 16 s, a control
   run and a roster run). The LARGER is taken — the makespan is what this feeds, and the smaller reading
   under-books the row — and both values print with the name.
4. **The plan header records the BASIS.** It used to emit `#block <BLOCK_KEY>` unconditionally, so a
   plan derived from the TSV would have carried the DATA block's label — **a plan stating provenance it
   does not have, which is worse than one stating none.** `#block` is now emitted only when the DATA
   block was actually read. Additive for an existing driver, which requires
   `version/digest/rows/slice_cap_seconds/cooldown_seconds` and reads other `#` lines generically.
5. ⚠ **A stale hardcoded label, and it is mine.** The per-worker line printed `shards@90min=<n>` while
   the count was computed against `C_TARGET = 40 min` — I moved `C_TARGET` from 90 to 40 in the dispatch
   driver cut and left the label behind. The number was right and its label was wrong, so a reader
   dividing the printed load by 90 minutes gets a different answer and concludes the generator is
   broken. Derived from `C_TARGET` now. **Same class as a hardcoded verdict string, in a file whose own
   repair notes say every hardcoded count is gone, and it survived a cut and a review.**

6. ⚠ **AND THE GENERATOR DID NOT RUN AT ALL ON THE LANE THAT DISPATCHES — also mine, found by i9
   (`29815a704`) on this file's first execution on Windows.** Windows Python writes stdout in the console
   codepage, so `--emit-plan` died `UnicodeEncodeError` on a `⚠` in a `print()` **before writing
   anything**, rc=1. `PYTHONIOENCODING=utf-8` fixes it caller-side, which is a workaround for a tool that
   should not need one. This is C2's own `.ps1` doctrine — `run-h10-dispatch.ps1` is pure ASCII for
   exactly this reason — applied to the `.py` it was not applied to, and my own re-derivation commit had
   just added three more such lines.
   - **Every output payload in this file is ASCII now**, docstrings included, since `__doc__` is printable
     by construction even though nothing prints it today.
   - **And `sys.stdout.reconfigure(errors="replace")`, which is the part that actually closes the
     class.** ASCII payloads are a convention and nothing asserts a convention; `replace` makes a print
     structurally unable to raise. It also covers the glyphs *no source census of this file can see*: the
     DATA block's heading carries a middle dot and its digest table an ellipsis, both arriving from the
     INPUT and printed. cp1252 encodes those two; **cp437, a real console default, encodes neither** —
     so i9's source census could not have found them, and neither could mine.
   - **This closes an ordering hazard i9 read out of the source without being able to manufacture data
     for it:** the plan is written BEFORE the summary prints, so a raising print leaves a **valid plan on
     disk behind a non-zero exit** — a caller checking rc discards a good plan, one not checking rc uses a
     plan whose generator reported failure. A print that cannot raise removes that by construction rather
     than by ordering care.
   - ⚠ **A repo-wide "no non-ASCII in a `.py`" guard was considered and REFUSED**, because the rule it
     would enforce is not the rule that matters: `hopA-inputs/rosterdelta.py` carries a middle dot inside
     a *regex that must match* one, and `probes/c1-finalizer-iteration-index/apply.py` carries a
     deliberately non-ASCII *generated identifier*. Both are load-bearing. The failure mode is
     specifically **what a script writes to stdout**, and that is now closed at the stream rather than by
     a file-level pattern that would need exceptions on the day it was written. (Noted for C1, not
     touched: `apply.py`'s `⚠` glyphs sit in its module docstring, which `__doc__` or a `--help` would
     print — the same one-line exposure this file just closed.)

**Controls.** The DATA-basis output is **byte-identical** before and after the refactor that made the
basis selectable (sha256 equal), with the single intended exception of the label in item 5 — which is
the regression control that the refactor changed no behaviour. Six refusal arms on the new parser (the
hand-stopped row renamed; a blank `sweep_s`; the `sweep_s` column renamed; a header-only file; a junk
one-line file; an unresolvable `--timings` path), each refusing and naming its cause. One positive
control: columns reordered, map identical.

**And the encoding fix is controlled two-sided from a box that is not Windows**, by reproducing i9's
environment with `PYTHONIOENCODING`:

| arm | cp1252 | cp437 | ascii | utf-8 |
|:--|:--|:--|:--|:--|
| the committed version before the fix | rc=1 | rc=1 | rc=1 | rc=0 |
| after the fix, with `--emit-plan` | **rc=0, plan written** | **rc=0, plan written** | **rc=0, plan written** | rc=0 |

The emitted plan is **byte-identical across all four** (`sha256 9f872103…`), which is the separate claim
that the artifact never depended on the console. A further arm plants a *new* `⚠` into a `print()` payload
and runs under cp437: **rc=0**, the glyph rendered `?`. That is the arm that distinguishes "we removed the
glyphs" from "a glyph can no longer break this", and only the second one survives someone editing the file
next week.

<!--
Provenance, 2026-09-13 (zero-token: block comments are stripped before this file enters context).

Ruling: dd9ea4a1d §2d -- "The re-derivation input is the pass-1 re-parsed TSV (sweep_s, never a shell
wall), net dropped, pass 2 beside it as the one-axis record" and "order the light bulk by ROW COUNT with
the heavy rows placed first, because t_r is floor-dominated noise". §3 AWAITING: "C2: ... the map
re-derivation when the TSV is on origin (row count for the light bulk, heavy rows first, no setup term)".

Input verified on origin at fd3f22f3bb6d4a0fc9cb2635c3c652816c14d903 before any figure here was written
(git ls-remote, exact match to i9's announced SHA in f5c401954).

Branch base: this branch is stacked on claude/c2-h10-dispatch-driver (02b87b501), NOT on a02ac3df3,
because the generator this re-derivation changes lives there and 02b87b501 is the SHA i9 is about to
-DryRun. Committing here instead of there leaves that SHA where i9 reported it. If COORD would rather
the train take one branch, C2 re-bases onto a02ac3df3 after the dry-run settles -- said in the
announcement rather than decided quietly.

The old basis' W=3 == W=4 == 4722 s coincidence: 4722 is exactly the reserved leg, which is what makes
it a pin-dominated result rather than a balance-dominated one. Checked by reading the reserved total and
the makespan as the same number rather than by inferring it from the two fleets agreeing.
-->
