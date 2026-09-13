# DRAFT shard map — H10 roster re-derivation, Go 1.24.13 hop (lane C2)

**This is a PROJECTION, not a deal.** `k` and `s_w` are NOT MEASURED at 1.24, 42 of the roster's 204 rows
carry no measured wall time, and the generator does not run at the measured tip. Per
[`docs/GoCorpusMigration.md`](../GoCorpusMigration.md) §3.2 — *"A map built at placeholder factors is a
projection, not a deal — say which it is, and gate dispatch on it."* Nothing below is dispatchable as written.

**Record class.** Point-in-time record, dated 2026-09-13, amendable only by dated block (see AMENDMENTS).
Not a runbook, not procedure, never executed from. The runbook leads on procedure; where this file and
§3.2 differ, §3.2 wins.

**Measured tip.** All readings taken at `654343a5e29c42328abcd4003c9ce7148dac1d38`.

⚠ **`origin/master` moved TWICE while this record was being derived**, and the record is landed against the
third of them, so the chain is stated rather than a single "since moved to":

```
  654343a5e29c42328abcd4003c9ce7148dac1d38   the measured tip -- every number below was read here
  2e6cf71e4804fc907a1a7eca8f0b532351d728a1   train-47 base at derivation time
  a02ac3df346db4dc0bcfcbe040f060a8290e01cd   the doctrine landing; master then FROZEN for train 47
```

**Verified, not assumed: every input blob this record reads is byte-identical across the WHOLE chain**
(`654343a5e` → `a02ac3df3`), so no number moves — checked blob-hash by blob-hash over all eleven inputs
(`ValidatedTestPackages.md`, `CENSUS-h10-eligibility-go124.md`, `DATA-sweep-row-walltimes.md`,
`hopA-inputs/shardmap.py`, `hopA-inputs/roster.txt`, `run-validated-sweep.ps1`, `_roster.ps1`,
`check-roster-format.ps1`, `GoCorpusMigration.md`, `PLAN-hop-campaign.md`, `DESIGN-peros-roster.md`), with a
positive control on two files that DID move across the same chain (`.claude/skills/mailbox/SKILL.md` and
`src/migrate-gorelease.ps1`) so the comparison is not reporting sameness because it is broken. The chain's
whole footprint is 4 files, +297/−10, none of them an input here.

**Quote the SHA, never the branch name.** This record's own derivation names `origin/master` as
`654343a5e` at a moment when it was no longer that — caught by one of the verifiers, not by the deriver, and
harmless only because the blobs had not moved. A projection that names its base is re-derivable; one that
carries a stale base silently is not.

**Execution constraints on this lane.** No `.ps1` was executed; `src/run-validated-sweep.ps1`,
`src/_roster.ps1` and `src/check-roster-format.ps1` were read as TEXT only.
`docs/phase4/hopA-inputs/shardmap.py` was run read-only from a scratch mirror, never in the repo. The tree
was clean before and after every pass (`git status --porcelain` zero lines, unfiltered, both sides).

---

## 1. Population — which rows this map is over

Three candidate populations exist and they are **not** the same set. The map's scope is a COORD ruling, not
a lane finding.

| Population | Count | Source | Status |
|---|---|---|---|
| Banked roster at the measured tip | **204** | `docs/ValidatedTestPackages.md`, body L213–L416 | measured, 7 predicates / 4 engines |
| go1.24.13 implementable skeleton | **227** | `docs/phase4/CENSUS-h10-eligibility-go124.md` §6 + Appendix | quoted from the census, which marks it CONFIRMED |
| Costed dataset (the only rows with a `t_r`) | **162** | `docs/phase4/DATA-sweep-row-walltimes.md`, first fenced block | measured |

**204 is the roster's true row count at the tip.** First three rows `archive/tar | 97`,
`archive/zip | 100`, `bufio | 80 | 1`; last three `unicode | 28`, `unicode/utf16 | 8 | 1`,
`unicode/utf8 | 14`. 204 names, zero duplicates. Tests column sums to 28,459 and Disclosed to 167 —
character-for-character the two figures in the document's own guarded header, which pins the field indices
(a one-column offset cannot produce two independent matches).

The file holds exactly **two** markdown tables: the roster (4 columns, 204 rows) and the exclusion ledger
under `## Excluded packages` (5 columns, 6 rows, body L502–L507), whose first cell is a **bare** code span
and therefore cannot match the roster predicate. The third heading (L540) contains no table — it is a dated
2026-09-02 prose derivation that labels itself *"A dated record, not a live count"*, so its "thirteen rows"
is not a population.

### ⚠ 227 is not a stale roster count

The string `227` does not occur anywhere in `docs/ValidatedTestPackages.md`. It is the go1.24.13
**implementable eligibility denominator**, derived in the census §6 as
229 (axis C at 1.24) − 1 `internal/unsafeheader` (E3) − 1 `runtime/trace` (E4) − 0 E1 − 0 E2 = **227**, and
its Appendix is a *"227 rows, keyed by 1.24 identity, every count blank. H10 banks INTO this"* skeleton.
Across **all 133 refs in the repository** the maximum roster row count is 204 and **no ref holds 227**.

⚠ **The same digits carry two unrelated meanings in this campaign.** 227 is also the value of axis A at
go1.23.12 (raw `_test.go` on disk, constraint-blind) — the census's own §7 negative control says so:
*"axis B at 1.23.12 gives 217, not 215, and axis A gives 227."* A dispatch that says only "227 rows" does
not distinguish them.

### The 204 ↔ 227 set difference

194 common · **10 roster-only** · 33 skeleton-only. The 10 are the census's "ten departures", which §7
check 6 says *"appear only as receives-verdicts-from annotations, never as rows"*, and each resolves in the
skeleton's receives column:

| 1.23 roster name | 1.24 identity it feeds |
|---|---|
| `crypto/internal/bigmod` | `crypto/internal/fips140/bigmod` |
| `crypto/internal/edwards25519` | `crypto/internal/fips140/edwards25519` |
| `crypto/internal/edwards25519/field` | `crypto/internal/fips140/edwards25519/field` |
| `crypto/internal/alias` | `crypto/internal/fips140test` |
| `crypto/internal/nistec` | `crypto/internal/fips140test` |
| `crypto/internal/mlkem768` | `crypto/internal/fips140/mlkem` **+** `crypto/mlkem` (fans out) |
| `internal/concurrent` | `internal/sync` |
| `internal/weak` | `weak` |
| `runtime/internal/math` | `internal/runtime/math` |
| `runtime/internal/sys` | `internal/runtime/sys` |

**If the map is keyed at 1.24, these 10 need a rekey first** — and one of them is a reserved floor row
(§3).

### Limits on the population

- 204 is the **banked** row count. It is **not** the campaign's dispatch list; no dispatch naming the H10
  population was read. Whether the map runs over 204 or 227 is **NOT RULED** and is Q1 in §11.
- ⚠ 204 is **not machine-checked at this commit**. It is hand-written in the header and
  machine-check**able** by `src/check-roster-format.ps1`, which asserts the header equals the column sums —
  but no CI workflow invokes it (`os-matrix.yml` runs `check-no-regression.ps1`, `run-behavioral.ps1`,
  `run-validated-sweep.ps1` and `annotate-summary.ps1` only), it is operator-run via a `.bat` launcher, and
  neither pass executed it. Its logic is sound (a digit-bump mutation moves the column sums, so it would
  fire); its execution at this commit is unwitnessed.
- E2 subtractions from the 227 are **NOT MEASURED** here. The census records E2 as swept 2026-09-08 with
  zero members but states subtractions *"are still owed and will remove rows from this skeleton, never add
  them"*. 227 is quoted as that record states it.
- Per-row OS applicability is **NOT EXTRACTED**. The roster's Linux sub-header reads
  *"Linux: 198 of 202 applicable rows validated at their Linux counts"*, with 2 rows platform-exclusive
  (202 + 2 = 204) — so a Linux-leg map over all 204 includes rows permanently inapplicable on that leg.

---

## 2. ⚠ THE HEADLINE — the `t_r` coverage gap

**42 of the 204 banked roster rows have no measured wall time. Coverage is 162 / 204 = 79.4%.**

| | Rows |
|---|---|
| Roster at the tip | 204 |
| WITH a measured `t_r` | **162** |
| WITHOUT a measured `t_r` | **42** (20.6%) |
| Wall-time rows that are not roster rows (retired/renamed) | **0** |

Arithmetic closes both ways: 162 + 42 = 204 and 162 + 0 = 162. Matching rule: **exact byte equality of the
Go import path**, no normalisation applied and none needed — a near-miss audit over both populations found
zero hits for uppercase, leading or trailing slash, whitespace, characters outside `[a-z0-9/._-]`, or
markdown-link residue. Label-vs-URL-target join: 0 mismatches across all 204. ⚠ A **fuzzy or basename join
would be wrong here**: roster `runtime/internal/math` has basename `math`, and `math` is a separate real
package present in both lists.

Second, independent derivation of the same gap: `docs/phase4/hopA-inputs/roster.txt` is a frozen 162-name
snapshot that is **set-identical** to the wall-time package set (`diff` rc=0). The entire 42-row gap is the
roster of record having grown past that frozen snapshot — 42 in the live roster not in the snapshot, 0 the
other way. That also re-derives the 0-orphan result from a different file.

### The 42 uncovered rows, complete and unfiltered

`crypto/cipher`, `crypto/internal/boring/bcache`, `crypto/internal/edwards25519`,
`crypto/internal/nistec`, `crypto/x509`, `debug/pe`, `encoding/gob`, `go/build`, `go/doc`, `html`,
`internal/chacha8rand`, `internal/concurrent`, `internal/coverage/cfile`, `internal/godebug`,
`internal/platform`, `internal/poll`, `internal/runtime/atomic`, `internal/syscall/windows`,
`internal/syscall/windows/registry`, `internal/trace`, `internal/trace/internal/oldtrace`,
`internal/weak`, `iter`, `log/slog`, `log/slog/internal/buffer`, `math/big`, `net`, `net/http`,
`net/http/cgi`, `net/http/cookiejar`, `net/http/httptest`, `net/http/httptrace`, `net/http/httputil`,
`net/http/internal`, `net/netip`, `net/rpc`, `net/smtp`, `os`, `os/user`, `slices`, `testing`,
`testing/fstest`

### What can and cannot be said about their cost

| Signal over the 42 | Value |
|---|---|
| Tests column: n / sum / min / max / median | 42 / 6,589 / 1 / 2,195 / 19 |
| Rows with Tests ≥ 100 | 10 — `crypto/internal/nistec` 2195, `net/http` 1345, `os` 683, `net` 472, `crypto/x509` 341, `math/big` 224, `net/netip` 210, `log/slog` 194, `slices` 119, `encoding/gob` 106 |
| Rows with Tests ≤ 7 | 13 (min 1) |
| Rows carrying a `$longTimeouts` floor — the only cost-bearing signal that exists for them | **2**: `net` 40m, `net/http` 60m |

⚠ **The Tests column is not a cost proxy, and that is measured, not quoted.** Over the 162 rows where both
Tests and `t_r` exist: Spearman ρ = **0.488** (a mild monotone association in the bulk that the heavy tail
inverts) and Pearson r = **0.108**. The two named inversions are visible in the data: `crypto/dsa` is
4 Tests / 1,317 s — the single largest row — while `go/doc/comment` is 10,059 Tests / 18 s. So
`crypto/internal/nistec` at 2,195 Tests cannot be assumed heavy and `internal/platform` at 1 Test cannot be
assumed cheap. The runbook rules it outright at §3.2: *"Verdict count is a bad cost proxy… The honest proxy
is the previous full sweep's per-row wall time."*

⚠ **A `$longTimeouts` floor is a timeout CEILING chosen by the sweep author, not a measurement.**
`net/http`'s 60m floor does not mean `net/http` takes 60m.

### The two rows that are worse than uncovered

`net` and `net/http` are in the 42 **and** are `$longTimeouts` floor rows, i.e. they belong in the reserved
set. Their cost is **not** entirely unknown — it is measured, but on the wrong machine and in the wrong
units:

| Row | Recorded figure | Provenance | Converted to i9-seconds at the placeholder 0.35 |
|---|---|---|---|
| `net` | ~1,480 s at 472/472 | `src/run-validated-sweep.ps1`:928 | ~518 |
| `net/http` | 1,836 s (one arm) and 2,171 s (other arm, *"package timeout after 00:30:00"*) | `src/run-validated-sweep.ps1`:933–934 | ~760 |

Provenance is stated in the script itself at :932 — *"Two arms of the SAME row, i7 class, Debug"* — so these
are **i7-class wall seconds**, not i9-seconds. ⚠ Converting them needs the very `s_w` calibration this
campaign has not done: **the fix for the two rows that break the map is circular until recon runs.**

### ⚠ Do not manufacture a cost. Options for COORD (§11 Q2)

LPT-greedy sorts by cost descending, so a row with no cost has **no position**. The only wrong answer is to
let a default decide it silently.

| Option | Size | Judgement |
|---|---|---|
| **(A) Nominal cost** | ~5–10 lines | Cheapest and most dangerous: the nominal's *value* decides the packing and the error is not symmetric. A median nominal (10 i9-s) against `net/http`'s 2,171 i7-s arm is **76x–99x** wrong unit-consistently (81x–99x at the i7 factors implied by the script's own calibration evidence) and places the campaign's heaviest uncovered row at the *tail* of the greedy order. If used at all it must be an **upper** bound — and for the 2 floored rows the floor is the natural one, leaving 40 rows with no bound of any kind. |
| **(B) Separate UNSCHEDULED bucket** | ~10 lines | The honest default and the only option that adds no fabricated number: emit the 162-row map, list the 42 as UNSCHEDULED with no cost claim, and label the makespan an explicit **lower bound** on the campaign. Composes with (A) or (C) as a staging step. |
| **(C) Recon measurement first** | a sweep leg | The only option that answers the question. Extrapolating the measured 162-row distribution brackets it: 42 × median 10 s = 420 s; × p75 16 s = 672 s; × mean 47.5 s = 1,997 s; × p90 71 s = 2,982 s — and `net/http` alone is recorded at 2,171 i7-s, so the low brackets are certainly wrong. |
| **(D) Hybrid** | — | Floors as upper-bound proxies for the floored subset, recon for the remaining 40. |

⚠ Per §3.2 the preceding sweep's per-row log is *"unrecoverable afterward"* — **these 42 readings do not
exist to be recovered.** They have to be measured. NOT MEASURED, by name: an actual `t_r` for any of the 42.

---

## 3. The reserved set R — two visibly separate halves

§3.2 requires: *"DERIVE the reserved set at generation time; never carry a copy… Keep the two halves of the
set visibly separate — the derived floor rows, and any row pinned for raw wall time, which is an editorial
choice and should not be mistaken for the derived half."* The halves below are never merged.
**|R| = 13** (11 derived + 2 editorial), of which **11 have a `t_r`**.

### 3a. HALF 1 — DERIVED (11 rows)

- **Derived from**: `src/run-validated-sweep.ps1` line 926, `$longTimeouts`, read as TEXT (never executed).
- **Derivation pattern** (the generator's own, reproduced verbatim): `\$longTimeouts\s*=\s*@\{(.*?)\}` with
  `re.S`, then `'([^']+)'\s*=\s*'[^']+'` over the captured body. Cross-checked by a brace-matched
  extraction with a structural split on `;`, by an anchor-free whole-file scan, and by arithmetic on the raw
  character counts of line 926 — four predicates, all 11, same names, same order.
- **Derived at**: tip `654343a5e`, 2026-09-13.
- **Liveness proven, not asserted** (safety floor #13): the printed set includes `net` and `net/http`, which
  the script's own comment block does not know about — output ahead of its source prose, which a copy cannot
  be. A canary regression on a scratch copy (one floor renamed) changed the printed set and made the assert
  name that exact site; restore reproduced byte-identical output.

| # | Row | Floor | In the roster? | `t_r` (i9-s, JOB-007) |
|---|---|---|---|---|
| 1 | `hash/maphash` | 60m | yes | 898 |
| 2 | `index/suffixarray` | 120m | yes | 573 |
| 3 | `crypto/dsa` | 120m | yes | 1317 |
| 4 | `archive/zip` | 60m | yes | 354 |
| 5 | `go/parser` | 90m | yes | 259 |
| 6 | `crypto/internal/mlkem768` | 30m | yes | 228 |
| 7 | `time` | 40m | yes | 197 |
| 8 | `crypto/tls` | 30m | yes | 659 |
| 9 | `sync/atomic` | 90m | yes | 82 |
| 10 | `net` | 40m | yes | **ABSENT** |
| 11 | `net/http` | 60m | yes | **ABSENT** |

**11 of 11 floors are banked roster rows — zero noise on this axis**, confirming the invariant
`docs/phase4/DESIGN-peros-roster.md`:580 asserts: *"Every `$longTimeouts` key is a package on the roster (a
floor for a non-roster package is dead config)."* `ABSENT` is recorded, never imputed.

### 3b. HALF 2 — EDITORIAL (2 rows). An admitted choice, not derived.

| Row | `t_r` | Rank of 162 | Share of the 162-row wall | Stated basis | Scored |
|---|---|---|---|---|---|
| `go/types` | 137 s | 12 | 1.78% | raw wall time | defensible |
| `go/doc/comment` | 18 s | 34 | 0.23% | raw wall time | ⚠ the data questions it |

⚠ On raw wall time — the stated basis for this half — pinning an 18 s row to the fastest box while leaving a
306 s row to LPT is inverted: **23 non-reserved rows are heavier than `go/doc/comment`**, headed by
`go/internal/gcimporter` 306 s, `regexp` 226 s, `internal/godebugs` 177 s, `crypto/rsa` 119 s,
`compress/flate` 106 s. The pin's *real* justification is a different quantity, per
`docs/phase4/hopA-inputs/shard-map-draft.md`:193–197 — *"reads 18 s here but carries 10,059 verdicts and
spawns `go build` throughout `TestStd` — its cost under `-test-action all` (the k regime) will diverge
hardest from this table."* That is a claim about `k`, which is NOT MEASURED. The editorial half should be
re-argued on the `k` axis or dropped, not defended on wall time.

*(No percentile is quoted for `go/doc/comment`: 18 s is tied five ways in the dataset, so the label is
convention-dependent — p77 to p80 across five standard conventions. The rank is convention-insensitive; use
the rank. `go/types` at p93 is convention-insensitive.)*

### 3c. ⚠ Four typed copies of R disagree with the derivation. Do not read any of them as the set.

| Document | What it types | Gap vs the live 11 |
|---|---|---|
| `docs/PLAN-hop-campaign.md` §4.3 | a 7-row table under a SUPERSEDED BY GENERATOR banner | ⚠ **the disclosure itself has drifted**: the banner names 2 missing floors (`go/parser`, `crypto/internal/mlkem768`) where the true gap is **6** — undisclosed residue is exactly `time`, `sync/atomic`, `net`, `net/http`. It also cites the table at `run-validated-sweep.ps1:495`; it is at **926**. And §4.3's symbol table still reads `| R | the reserved set | the table above |`, pointing R at the retired copy in contradiction of its own banner. |
| `docs/phase4/hopA-inputs/shard-map-draft.md` | the same 7 rows as *"all 7 present"*, 3,956 i9-s (65.9 min), and *"checksum: 162 rows assigned = 7 reserved + 155 bulk"* at L128/L151/L178 | scope, not arithmetic — the typed 3,956 s reproduces exactly as the sum of those 7 rows' `t_r`. See §4 for the correction. |
| `docs/phase4/MILESTONE-75pct-prep.md`:244 | *"the four `$longTimeouts` floors"* | missing **7 of 11**; no supersede banner on that block, though the document is record-class by its own header banner |
| `docs/phase4/DESIGN-peros-roster.md`:483 | *"`$longTimeouts` is eight entries"* | a typed **count**, wrong by three |

Not a disagreement: `docs/phase4/REHEARSAL-go12312.md`:391–396 quotes the then-live 7-entry table, but it is
a dated record of the bug itself and reads correctly as history.

### 3d. ⚠ Two blind spots in the derivation itself

1. **It keys on NAMES only.** A floor-value perturbation (`crypto/dsa` 120m → 121m) moved nothing in the
   output. So the *"two floors misquoted"* half of the failure §3.2 records is invisible to this derivation;
   only membership drift is caught.
2. **The extraction regex fails SILENTLY on a nested table.** Measured on synthetic formattings: a
   multi-line reformat is safe (11/11), but a nested `@{ }` truncates to a **non-empty subset** that passes
   both guards (`assert _m` and `assert _floors`) — 9 floors if the nested entry is late, 2 if early, and 0
   (loud) only if it is first. ⚠ This is not hypothetical: `docs/phase4/DESIGN-peros-roster.md` §7 already
   **specifies** that exact nested shape for per-OS floors (`'time' = @{ default = '40m'; linux = '90m' }`),
   with `time` as its worked example. Applying that documented schema to the live table yields **6 of 11**
   floors silently, losing `time`, `crypto/tls`, `sync/atomic`, `net`, `net/http`. The per-OS floor design,
   implemented as written, re-creates §3.2's exact failure mode inside the generator built to prevent it,
   with no error. A precedent for the nested shape already exists in the same file
   (`$capabilityConditionalBlocks`, nested on `crypto/tls`).

### 3e. ⚠ R's name-join breaks at 1.24 before any factor is measured

Twelve of the 13 reserved rows are present in the 227-row skeleton. **One is absent:
`crypto/internal/mlkem768`** — census line 80 records it as *"FANS OUT to two 1.24 rows"*, and the skeleton
carries `crypto/internal/fips140/mlkem` (ordinal 34) and `crypto/mlkem` (41) instead. So deriving R at 1.24
from a 1.23-keyed `$longTimeouts` emits a floor name with no row, firing the same assertion class that kills
the generator today — and **which of the fanned pair inherits the 30m floor is undefined**. NOT MEASURED: the
1.24 floor table.

---

## 4. Construction and the checksum

The construction is `docs/GoCorpusMigration.md` §3.2 (the maintained copy); `docs/PLAN-hop-campaign.md` §4.3
carries an anchor-preserving instance marked *"[GENERALIZED 2026-08-24 into GoCorpusMigration.md §3.2, which
is now the maintained copy]"*. Steps, as the plan states them:

1. `rows := roster at branch tip`
2. `R := reserved set ∩ rows` (pinned to the i9)
3. `P := ASC round-robin recon` over `rows \ R`
4. `B := DESC LPT-greedy` over `rows \ R`, each row onto the bin of smallest projected **local** time
   (`load[m] / s[m]`)
5. split any bin over `C`
6. emit the shard map with checksum **`|rows| == |R| + |B|`**

### The checksum cannot close over the roster. Here is the arithmetic that does close.

| Over | `|rows|` | `|R|` | `|B|` | Closes? |
|---|---|---|---|---|---|
| The costed dataset (162) | 162 | 11 (9 floors with a `t_r` + 2 editorial) | 151 | **YES** — 162 == 11 + 151 |
| The banked roster (204) | 204 | 13 | 191 *(by subtraction)* | row-counting only. ⚠ **No bin can be filled**: 42 rows have no cost, and 2 of them are reserved rows. |
| The 1.24 skeleton (227) | 227 | **cannot be formed** | — | ⚠ **Not statable.** `crypto/internal/mlkem768` has no 1.24 row (§3e); R needs a rekey before a checksum exists. |

⚠ The generator's own printed checksum line is a **live falsehood**: `shardmap.py`:148 prints
`"checksum: {n} rows assigned == 7 reserved + {len(bulk)} bulk"` with the **7 hardcoded**, so a completing
run prints *"162 rows assigned == 7 reserved + 151 bulk"* — and 7 + 151 = 158, not 162. The real split is
11 + 151. The guarding assert behind it (`n_assigned == 162`) is correct and does fire; only the message is
stale. Anyone cross-checking a map's arithmetic from that line will find it does not add up, and the
discrepancy is the message, not the map.

---

## 5. The map — what can be emitted, and the shape of what comes out

### 5a. ⚠ Nothing can be emitted today. The generator is dead at the measured tip.

`docs/phase4/hopA-inputs/shardmap.py` exits **1** with
`AssertionError: reserved row net not in dataset` at line 94, after printing
*"reserved set derived at generation time: 11 floor rows (…) + 2 big rows"*. Nothing downstream of line 94
runs: no bins, no makespan, no W tables, no sensitivity block. Reproduced independently three times from
separate scratch mirrors. **Line 94 is the script behaving correctly** — it is refusing to emit a map that
would silently omit two of the rows it is supposed to pin, and any change that makes it "run" by relaxing
that assert is a regression.

⚠ **Supplying the missing `t_r` is NOT sufficient.** Two constraints are now mutually unsatisfiable:

| Blocker | Line | Behaviour |
|---|---|---|
| `assert len(rows) == 162` — a hardcoded literal | 25 | Injecting the two missing rows trips it: *"expected 162 rows, parsed 164"*. A 204- or 227-row dataset aborts here before R is ever consulted. |
| `assert r in byname` for every reserved row | 94 | Fires today on `net`. |

Positive controls: removing only `net` and `net/http` from a **copy** of the floor table → exit 0, all three
W maps emit. Injecting the two rows **and** bumping the literal to 164 → exit 0, reserved 13 rows,
8,373 s pinned. (⚠ That 8,373 s / 139.6 min figure inserts **i7-class** seconds into an i9-second table —
unit-inconsistent; see §2 for the ~518 / ~760 i9-second estimates, which give ~6,000 i9-s.)

⚠ The generator also **deviates from the construction it cites**: step 2 says `R := reserved set ∩ rows`,
and the code asserts instead of intersecting. Under the plan's own construction `net` and `net/http` simply
fall out of R and the map emits. This is a one-line generator defect, not an input incompatibility — which
matters, because it tells a fixer what to change.

**Minimum mechanical repairs** (none of them the deliverable's difficulty): take the population from the
data at lines 25 and 147 (2 lines); make line 148's literal `7` become `len(RESERVED)` (1 line); intersect
rather than assert at line 94 (1 line); select the DATA block by a labelled `(OS, SHA, machine)` key instead
of by position, and tolerate the verdict-column row variant (~10–15 lines).

### 5b. What the projection's shape is, where it could run

These figures are the generator's LPT over the **162-row** dataset at **placeholder** factors. They are a
projection of a projection: a 162-row map is a lower bound on a 204-row campaign, and cost is i9-seconds
from a pre-hop corpus.

| Reserved set | Leg total | Share of the 162-row wall | Makespan at W=3 / 4 / 5 |
|---|---|---|---|
| Typed 7 rows (`shard-map-draft.md`) | 3,956 s = 65.9 min | 51.4% | 4,289 s / 3,956 s / 3,956 s |
| **Derived 11 rows with a `t_r`** | **4,722 s = 78.7 min** | **61.3%** | **4,722 s at W=3, W=4 and W=5 alike** |
| Delta | +766 s = +12.8 min | +19.4% | — |

My independent LPT reproduces the draft's typed figures exactly (W=3 4,289 s, W=4/5 3,956 s, bulk bins
3,262 / 2,498 against its *"~3,250 / ~2,497"*) — a calibration control proving the same algorithm over the
same rows before asserting its numbers are scoped wrong.

⚠ **Three structural facts the projection exposes, which matter more than its magnitude:**

1. **The LPT stage is inert and the sensitivity table is vacuous.** The reserved set is pinned to the i9
   *unconditionally, before the LPT pass*. The i9 then takes **zero bulk rows at every W**, and the pin
   (4,722 s) exceeds **all three** perfect-balance bounds the script itself prints (W=3 4,278 s, W=4
   3,582 s, W=5 3,080 s). So the makespan is **identical at W=3, 4 and 5** — the 4th and 5th machine change
   the headline by exactly zero, no W-sensitivity can be read off this map, and four of five sensitivity
   scenarios print `+0%` because the binding constraint is the pin, not the fleet. The draft's *"at W ≥ 4
   the makespan IS the reserved set"* becomes **at W ≥ 3**: its conclusion strengthens while its numbers
   move. A useful corollary: the SUSPECT placeholder factors are harmless to the makespan here, which the
   script computes and never states.
2. **The reserved set has no holder.** The pin requires the i9 to carry 11–13 floor rows. The roster's
   status column calls the i9 offline since 09-08 (§6), and at the placeholder `s_w` = 0.35 that load is
   **~3.4–6.6 h** on a laptop-class box. Meanwhile `docs/phase4/CENSUS-release-tc0-delta.md` records that
   this very host *"reboots under a continuous multi-hour sweep — the first attempt died that way at 13
   minutes"*, and split 201 rows into four shards (51/51/51/48; 1,233 / 1,601 / 2,234 / 1,622 s) with a
   ten-minute cooldown between them. **A 4,722 s+ continuous i9 bin has already been measured to be
   unholdable on the i9.**
3. **Reserved is two thirds to three quarters of the campaign.** With `net` and `net/http` costed from the
   script's own i7-class arms converted at 0.35, reserved is 6,000 / 8,979 = **66.8%**; at their timeout
   ceilings it is **78.3%**. The honest statement is a **67%–78% bracket**, not a point figure. Either way
   the pin rule now serialises the bulk of the campaign onto one box, and re-emitting over 204 or 227 rows
   without revisiting the pin rule produces an arithmetically valid map of a badly-shaped plan.

### 5c. ⚠ Two false-green routes in the generator the next map must not inherit

| Route | Measured behaviour |
|---|---|
| **Positional block selection** | The parse takes the **first fenced block**, not a labelled `(OS, SHA, machine)` key — while `DATA-sweep-row-walltimes.md`'s own header instructs *"Add a section per new (OS, SHA, machine) measurement at each hop recon; do not overwrite old sections — supersede them."* Control: inserting a superseding section with exactly 162 shape-compatible rows and every time **doubled** → **exit 0, zero stderr, all six asserts pass**, and every headline silently moved (total 7,701 → 15,402; median 10.0 → 20.0; mean 47.5 → 95.1; makespan 4,722 → 9,444 at all three W). The `162` assert is **no backstop** whenever the superseding section is the same size — and the existing Linux block is itself exactly 162 rows. |
| **Cardinality-only guard** | Corrupting one `t_r` in place (`archive/zip` 354s → 99999s, row count unchanged) passes every assert, prints *"rows parsed: 162"*, and reports 107,346 i9-seconds instead of 7,701 — a **14x wrong makespan basis with the instrument fully green**. Deleting a row *does* fire line 25 correctly (*"parsed 161"*), so the assert is live — but it guards cardinality only; content is unguarded. **A green run is not evidence the times are intact.** |

---

## 6. W — which lanes can be H10 workers

A shard dealt to a worker that cannot run it is a false red. `-test-action all` is the binding constraint.

**What an H10 row requires**, per `docs/GoCorpusMigration.md` H10 step 1 and the converter's own call graph:
(a) a **.NET SDK**, (b) a **Go toolchain at the target release**, (c) a **full corpus checkout** at the
migration branch tip. **PowerShell is not required for the row itself** — H10 forbids the sweep wrapper by
name (*"never the sweep wrapper: `run-validated-sweep.ps1` is the steady-state gate, enforcing the exact
banked count and a drift-clean corpus — both of which this step invalidates by design"*).

The `dotnet` requirement is **unconditional and structural**, not an inventory claim:
`src/go2cs/testConversion.go` `executeTestAction`:6065 → `case "compare", "all"` →
`compareGoAndConvertedTests`:7570 → runs `"go" test …` at :7616 → `publishTestHost`:7629, whose body
(:6140) holds the file's **only two** `"dotnet"` literals, both `"dotnet", "publish", testProject`
(:6143 Release / :6147 Debug) → executes the published host at :7639. There is no branch through
`-test-action all` that skips `dotnet publish`.

pwsh is a **hard** requirement for the steady-state sweep, `check-roster-format.ps1`,
`check-no-regression.ps1` and `check-solution-integrity.ps1` — so a box that runs rows but cannot run pwsh
**cannot self-verify its own preconditions**.

| Lane | Host, as the roster spells it | H10 worker? | Capability citation |
|---|---|---|---|
| COORD | i7 (i7-5820K desktop, C: clone) | **YES** (cited) | `SESSION-PROMPTS-cloud-lanes.md`: *"Every converter/golib merge owes a union battery on the i7"*; `LANES.md`: *"slowest fleet box; budget tables key off it"* |
| R | R-LAPTOP, WSL (6850U, 8C/16T, 31 GB) | **YES** (ran a full 162-row sweep) | `DATA-sweep-row-walltimes.md`:183 Linux leg; `REHEARSAL-go12312.md`:369 *"162 (149 PASS / 10 FAIL / 3 CVAC)"*. ⚠ `LANES.md`: *"probed at 34 GB free — below the 60 GB preflight"* |
| G | G-LAPTOP, WSL (6650U, 6C/12T, 31 GB, 210 GB free) | **YES** (banked a row through the pipeline's own invocation) | BOARD:22807 — `go2cs -tests -test-action all …` with `BANK_EXIT=0` (proves dotnet + go); BOARD:17850 `run-performance.ps1` (proves pwsh) |
| i9 | i9 (i9-13900K) | **YES** (ran the 162-row Windows sweep of record) | `DATA-sweep-row-walltimes.md`:11. ⚠ `LANES.md`: *"random ~daily reboots pending RMA"* |
| C1 | cloud container | **NO** — `dotnet` absent | `KICKOFF-fleet.md`:86 *"cannot build .NET (no dotnet), but both Go pins resolve: the converter builds, converts and its suites run there"* |
| C2 | cloud container (this lane) | **NO** — `dotnet` **and** pwsh absent | `KICKOFF-fleet.md`:87, and re-measured this session: `dotnet`, `pwsh`, `powershell` all `command -v` exit 1; both Go pins launch (`go1.23.12`, `go1.24.13`); converter builds exit 0 stamped `go1.24.13`; `src/core` holds 510 `.csproj`; 27 GB free |

**|W_capable| = 4** — {i7, i9, R-LAPTOP, G-LAPTOP}. **C1, C2 ∉ W.** This agrees with a ruling already on
record: `REHEARSAL-go12312.md` §4.6 — *"No native-Linux worker exists… the fleet is 4"*, with the fifth row
marked *"ASSUMED — no such machine in the roster"*.

⚠ **`FLEETS[5]` is unsatisfiable and the 5th slot cannot be filled by a cloud lane.** `shardmap.py`:63
carries `"X (5th engaged machine)": 0.35, # placeholder silicon, placeholder factor`; filling it with C1 or
C2 deals real rows to a worker that would report every one of them unmeasurable (the mass
`Go="pass" C#=""` signature — §3.4's false-green #1, the vacuous shard).

⚠ **|W_engaged ∩ capable| is NOT DETERMINABLE from the roster**, and this record does not assert a number.
`KICKOFF-fleet.md`'s status column mixes a dated offline marker, a last-post SHA and a cut description, and
only C1/C2 — the two **incapable** lanes — carry an explicit `UP`. Three predicates give three answers:
§5's active dispatch (which addresses COORD :178, R :185, G :192, C1 :205, i9 :213) implies 4 capable lanes
engaged; same-day activity implies 1 (handover commit `5e41c425c`, 2026-09-13 02:04, states *"COORD resumed
on the i7"*, while 36 commits across all refs since 09-09 are owner-authored or on the C1/C2 cloud branches
— **zero from the R, G or i9 lanes**); absence-of-an-offline-marker implies 2. A map deals to **members**,
not to a count, so the engaged set is Q3 in §11.

### What C1 and C2 CAN carry

Per §3.6's worker contract (*"a worker runs named instruments at stated budgets and reports raw output…
makes no rulings and never commits to master"*), anything Go-only or text-only is routable to a no-dotnet
lane: the converter build and every Go-side gate; conversion-only work (H0 seeding, H4a staging regen, H5's
seeded reconvert and the two-seeded emission diff — **not** their compile gates); census work (H3, and the
cgo census, which is a grep); H10 **step 5**, the per-package deadline-floor re-check (reading the `.ps1` as
text — done, §3a); map generation as a **derivation, never a deal**; records, ledgers and checksum
arithmetic. **Not** routable: H10 steps 1–3 (pipeline re-run, verdict re-derivation, disclosure re-signing —
all products of a compare run, whose manifest is written into the run's output dir); step 4's proof page
(emitted at the END of the compare); ledger **merging** into master; and the control-leg role.

---

## 7. Factors — every one of them NOT MEASURED

| Symbol | §3.2 meaning | Value used here | Status |
|---|---|---|---|
| `t_r` | previous full sweep's per-row wall time | JOB-007 i9 windows, 162 rows | **measured, but PRE-HOP and OS-SPECIFIC** — see limits |
| `k` | convert-and-build multiplier for `-test-action all` | 1 (the draft's assumption) | **NOT MEASURED.** An unfiltered grep of `DATA-sweep-row-walltimes.md` for `k`/multiplier returns zero hits; `k` genuinely does not appear. §3.2: *"measured on the recon phase's first rows; never assumed."* |
| `s_w` | worker speed factor, fastest = 1.00 | i9 1.00 · R 0.45 · i7 0.35 · G 0.35 · X 0.35 | **NOT MEASURED.** `shardmap.py`:56–64 self-labels them *"PLACEHOLDERS pending hop-recon calibration; LANES.md marks historical cross-machine ratios SUSPECT."* Only i9 = 1.00 is real, and only because it is the anchor by definition. |
| `R` | the reserved set | 13 rows, §3 | derived live at the measured tip; 2 members have no `t_r` |
| `C` | per-bin ceiling | 5,400 s (90 min) | the draft's value; no bin splits at any W |
| `W` | the fleet as engaged | see §6 | `|W_capable| = 4`; engaged ∩ capable NOT DETERMINABLE |

Two sanity checks on the placeholders, neither a calibration:

- **Implied `s_R`** from the two DATA legs at the same corpus SHA: 0.4029 over all 162 rows, **0.4334 over
  the 151 bulk rows** (the only ones R can receive), 0.3858 over the reserved 11. The 0.45 placeholder is
  within 4% of the bulk figure — defensible **by luck, not derivation**, and it conflates machine, OS and
  (on 10 rows) FAIL-vs-PASS.
- **Implied i7 factors** from the sweep script's own calibration evidence (i7-5820K, 2026-08-10):
  `archive/zip` 354/774 = **0.457**, `hash/maphash` 898/2,406 = **0.373** — both **above** the 0.35
  placeholder.

### ⚠ What §3.2 requires before the map leans on `s_w`

> *"The calibration workload is part of the protocol, not an afterthought. A row whose time is dominated by
> fixed convert-and-build overhead cannot discriminate a fast worker from a slow one — a few-second row
> measures the overhead, not the throughput. Pick a **mid-weight** row, state the repetition count and where
> the reading is recorded, and do it before the map leans on the number."*

Three obligations, none met: (1) a mid-weight row **named**, (2) a **repetition count** stated, (3) a
**recording location** stated. The runbook fixes none of the three — the hop supplies them.

The **band is measured** even though the choice is COORD's. The dataset's median row is 10 s — exactly the
overhead-dominated kind §3.2 forbids — and the mid-weight band runs **p90 = 71 s to p95 = 226 s**. In-band
candidates: `compress/flate` 106 s, `crypto/rsa` 119 s, `regexp` 226 s. Out of band:
`crypto/internal/edwards25519/field` 66 s (below p90) and `crypto/internal/mlkem768` 228 s (above p95, and
a reserved floor row besides). `REHEARSAL-go12312.md` §4.2 independently rules the previously-proposed pair
*"weak and under-specified"* — *"`container/heap` is a 7-second row… will not reliably discriminate a 0.45
box from a 0.35 box"* — and recommends a mid-weight row, naming `go/parser` (259 s i9).

By precedent the recording location is `docs/phase4/DATA-sweep-row-walltimes.md`, whose own header rules
retention: *"Add a section per new (OS, SHA, machine) measurement at each hop recon; do not overwrite old
sections — supersede them."*

### The `t_r` input's own limits

| Section | OS · corpus · machine · date | Rows | Aggregate | Generator-parseable |
|---|---|---|---|---|
| 1 (L11, fence L18/L181) | windows · `18770d083` · i9-13900K · 2026-08-23 (JOB-007) | 162 | **7,701 s** (its own self-check figure; the sweep's aggregate is 7,697 s) | **yes**, 162/162 |
| 2 (L183, fence L200/L363) | linux · `18770d083` · 6850U R-LAPTOP (WSL2 Ubuntu 22.04) · 2026-08-23 | 162 | **19,113 s** (149 PASS / 10 FAIL / 3 CVAC) | ⚠ **NO**, 0/162 — the verdict word occupies a column the row regex has no slot for |

Both sections cover the **identical** 162-name package set (0 in either direction), carry the **same**
corpus SHA and the **same** date, and the file has exactly **one commit** in its entire history
(`aa4e5fc2d`, 2026-09-06) — so neither is newer than the other on either the label axis or the git axis.
⚠ **Both therefore describe the PRE-HOP corpus**, and it is likely neither describes the corpus the next
map will run against. Whether `18770d083` is the current corpus SHA is **NOT MEASURED**.

Section 1 is derived by artifact-mtime **differencing**; section 2 is recorded directly by the per-row
driver — a stronger derivation. §3.2 rules the leg: the leg the shard will actually run on is the leg to
cost from, *"and the recon that measures k and s_w measures the row costs with them, on the leg the shard
will actually run"* — so if a Linux leg is dispatched, section 1 is the wrong arm regardless of its
position in the file.

⚠ `t_r` is **not portable across operating systems**: §3.2 records *"one leg at roughly 2.5× the other
overall and three times on the single row that bound the makespan"*, and it reproduces here — the same 162
rows total 19,113 s on Linux against 7,701 s on Windows (2.48x), with `crypto/dsa` at 4,366 s against
1,317 s.

### The distribution the packing has to work with (section 1, i9-seconds)

n = 162 · total 7,701 s (128.35 min) · min 6 s (`unicode/utf8`) · max 1,317 s (`crypto/dsa`) ·
mean 47.53 s · median 10.0 s · p75 16 s · p90 71 s · p95 226 s.

| Bucket | Rows | Seconds | Share of wall |
|---|---|---|---|
| 0–10 s | 95 | 831 | 10.8% |
| 11–30 s | 41 | 633 | 8.2% |
| 31–60 s | 8 | 357 | 4.6% |
| 61–120 s | 6 | 549 | 7.1% |
| 121–300 s | 6 | 1,224 | 15.9% |
| **> 300 s** | **6** | **4,107** | **53.3%** |

**The shape is what matters for packing**: 95 of 162 rows are ≤ 10 s and together are 10.8% of the wall,
while 6 rows over 300 s are 53.3%. Top-10 heaviest — `crypto/dsa` 1317, `hash/maphash` 898, `crypto/tls`
659, `index/suffixarray` 573, `archive/zip` 354, `go/internal/gcimporter` 306, `go/parser` 259,
`crypto/internal/mlkem768` 228, `regexp` 226, `time` 197 — sum 5,017 s = **65.1%** of the total wall.

*(Resolved in passing: `archive/zip`'s three figures are three machines, consistently ordered — i9 354 s,
R-LAPTOP 649 s, i7 774 s, the last recorded in `src/run-validated-sweep.ps1`:882–886 as
"archive/zip measured 774 s here". The roster prose's ~775 s is the i7 reading, not a contradiction.)*

---

## 8. Dispatch mechanics — the map's shape is constrained by what can execute it

⚠ **The sweep's native shard parameters cannot express this map.** `src/run-validated-sweep.ps1` exposes
`-ShardCount` / `-ShardIndex` (declared :105–:108, documented :98–:104, refusing `ShardIndex > ShardCount`
at :111–:114, slicing at :276–:282) into **contiguous ceil-sized chunks in ROSTER ORDER**. An LPT-greedy
deal is not contiguous in roster order. `docs/CIMatrix.md`:73's `sweep-shard` stage takes a package
**substring filter**, not a row list, and has the same limitation (job cap 210 min, 315 mac; *"Never a merge
gate"*). So a per-row deal needs either a row-list parameter that does not exist or a per-row invocation
loop — **open instrument debt, and it is not in this record's scope to design.**

⚠ **Citation hazard**: `docs/GoCorpusMigration.md` §3.1 line 786 at the measured tip still reads *"It
exposes no jobs, throttle, shard or resume parameter"*, which is **false at master** given the above. Cite
§3.1's worktree-is-the-unit-of-isolation rule, not that sentence. A local unmerged branch already corrects
it.

Two rulings the map must **honour**, not merely cite:

- **Platform affinity** — `docs/phase4/DESIGN-peros-roster.md`:656: a goos-scoped disclosure entry *"can
  only be re-signed by a shard running on that platform, because its signature comes from that platform's
  capture. The shard map… must therefore assign a scoped row to a machine of the matching OS."* And :655:
  *"Floors are re-checked per OS… the OS with the slowest legitimate host owns the number."*
- **The `time` row's pre-H10 blocker** — `docs/phase4/hopA-time-prestage.md`: an `asynctimerchan=2`
  AccessViolationException, *"hop A's one named pre-H10 blocker"*; *"What H10 will see if nothing is done:
  the `time` row fails with a mass-empty tail that reads like total conversion failure."* `time` is a
  `$longTimeouts` floor row, so it lands **in R** — the map must not deal it blind.
- **OQ-H6's fallback line** — *"Designate a named FALLBACK rather than a standing second control… The
  fallback runs the control leg at its own speed, with the budget raised, and its results carry its machine
  name."* One line, owed by the map. The draft §4.4 nominates R; this record does not rule it.

---

## 9. Prior art — cite / supersede / honour

**The decisive negative: no shard map document has ever existed on any ref.** A filename search over the
repository's whole history returns exactly two paths — `docs/phase4/hopA-inputs/shard-map-draft.md` and
`docs/phase4/hopA-inputs/shardmap.py`. `docs/phase4/SHARDMAP-go1.23.12.md`, which the construction's step 6
promises, has never existed. **So this record is the first shard map of record, not a replacement for one.**

| Artifact | What it is | Disposition |
|---|---|---|
| `docs/GoCorpusMigration.md` §3 | the runbook: §3.1 shardability + isolation, **§3.2 the maintained construction**, §3.3 shard preconditions, §3.4 the ledger, §3.5 signals + incremental merge, §3.6 the worker contract | **CITE, never supersede** — but not §3.1's no-shard-parameter sentence (§8) |
| `docs/PLAN-hop-campaign.md` §4.3 (L443–L600) + §4.3.1 | the hop-A instance; two SUPERSEDED banners at L451 and L513 | **SUPERSEDE** the 7-row reserved table, §4.3.1's numbers and its checksum split. **CITE** the symbol table's sources, the per-shard signal template (L583–L600) and the canary rule. ⚠ Its symbol table still points `R` at the retired table |
| `docs/phase4/hopA-inputs/shard-map-draft.md` (361 lines) | the only banked per-worker per-row deal in the tree; W=3/4/5; 7-row R; placeholder factors; labels itself a projection throughout | **SUPERSEDE.** Its own 2026-08-24 appendix already predicts this: *"re-running it today yields a larger reserved set than the 7 rows below"*, and *"`emit_md.py` was never banked… `shardmap.py` alone is the reproduction path"* |
| `docs/phase4/hopA-inputs/shardmap.py` (185 lines) | the generator | **FIX or SUPERSEDE, not merely re-run** (§5a). ⚠ Four blobs exist in its history; none emits a map on a Linux box — the two earliest abort on a hardcoded Windows clone root, both later ones on the `net` assertion |
| `docs/phase4/hopA-inputs/README.md` (84 lines) + its five recon inputs (`census-raw.txt` 152, `commits.tsv` 83, `roster.txt` 162, `h3-files.tsv` 160, `src-files-classified.tsv` 150) | banked hop inputs | **CITE.** ⚠ **None of the five is a shard-map input** — measured: `shardmap.py` opens exactly two paths, `DATA-sweep-row-walltimes.md` and `src/run-validated-sweep.ps1`, neither in `hopA-inputs`. `roster.txt` is a record of that day's population, and §4.3 step 1 says rows are *"re-read at the hop branch tip before dispatch, never carried"* |
| `docs/phase4/DATA-sweep-row-walltimes.md` | the `t_r` input, self-described as *"the hop shard map's input"* | **CITE and ADD A SECTION TO** at recon, per its own supersede-never-overwrite header |
| `docs/phase4/REHEARSAL-go12312.md` §4 | the point-in-time review of exactly this artifact; readiness row 4 *"PARTLY READY — computed but unbanked… every non-i9 speed factor is a self-declared placeholder"*; §4.1 *"What §4.3.1 actually contains is a makespan-by-fleet-size projection, not a worker→rows deal"*; §4.2's calibration-pair warning; §4.6 *"the fleet is 4"* | **CITE** — both its recommendations landed and its distinctions still hold |
| `docs/phase4/CENSUS-h10-eligibility-go124.md` | the 1.24 population of record: denominator final at 227, Appendix = 227-row skeleton *"H10 banks INTO this"* | **CITE as the population candidate.** Contains no shard content at all (measured: empty) |
| `docs/phase4/CENSUS-release-tc0-delta.md` | a different axis of sharding: one i9, 201 rows, four shards, thermal cooldown | **CITE as evidence the i9 cannot hold a continuous multi-hour bin** (§5b) |
| `docs/phase4/DESIGN-peros-roster.md` §655–§656, §7 | the platform-affinity and per-OS-floor rulings | **HONOUR** — and see §3d for the nested-table hazard §7's own schema creates |
| `docs/phase4/LANES.md` | the CANONICAL 4-box fleet table (probed 2026-08-22), the SUSPECT-ratios ruling, *"hop-shard overflow"* | **CITE.** ⚠ Its fleet table is itself behind the worker set: 17 BOARD entry headings name cloud lanes C1/C2, and LANES mentions C1/C2/cloud **zero** times |
| `docs/CIMatrix.md` | the `sweep-shard` overflow stage | **CITE as an extra bin source with a hard `C`** (§8) |
| `docs/phase4/hopA-time-prestage.md` | the `time` row's pre-H10 blocker | **HONOUR** (§8) |
| BOARD, 2026-08-25 entry | *"DERIVE the reserved set at generation time (the copied list drifted twice)"*; *"bank a migration's INPUTS in the commit that claims them, because the report is not the artifact"*; *"A hoist still needs an editor; a derivation needs nobody"*; the Linux-vs-Windows `t_r` question *"resolved by standing rule"* | **HONOUR.** ⚠ The BOARD's newest entry is 2026-09-08 — a ruling made 09-09..13 would live only off this ref |
| `docs/ValidatedTestPackages.md` | the roster of record | **NO standing ruling on sharding.** Measured: an unfiltered union grep returns 5 hits, all homographs or banking prose, none a ruling. It contributes the population, not doctrine |

---

## 10. Limits and NOT MEASURED, by name

**Scope.** Every reading is at tip `654343a5e` (blobs identical at `2e6cf71e4`). Nothing in the working tree
was written; no `.ps1` was executed; no Go test and no sweep was run; `shardmap.py` ran only from scratch
mirrors.

**NOT MEASURED:**

1. **`k`** for any row. Absent from `DATA-sweep-row-walltimes.md` entirely.
2. **`s_w`** for any worker, including this lane. All five factors remain the generator's self-declared
   placeholders; no calibration row was run and none can be run on a lane with no `dotnet`.
3. **`t_r` for any of the 42 uncovered rows**, `net` and `net/http` included. Not measurable from a
   read-only box, and §3.2 rules the preceding per-row log *"unrecoverable afterward"* — these readings do
   not exist to be recovered.
4. **The 1.24 `$longTimeouts` table**, and which of `crypto/internal/mlkem768`'s two 1.24 successors
   inherits the 30m floor.
5. **The current corpus SHA**, and therefore whether either DATA section describes the corpus the next map
   runs against.
6. **The makespan at the full 13-row reserved set in consistent units.** The 8,373 s control figure mixes
   i7-class seconds into an i9-second table; ~6,000 i9-s is an estimate at a placeholder factor, not a
   measurement.
7. **Section 2's distribution** beyond its row count (162), package set (identical to section 1) and
   aggregate (19,113 s).
8. **Run-to-run variance of any `t_r`.** Each value is a single observation and section 1's are
   mtime-differenced, so no error bar exists.
9. **Per-OS applicability of the 42**, and of the 204 generally. The per-row `linux:` / `n/a` /
   `execution:` annotations were not extracted; any platform impression from a package name is an
   impression.
10. **Per-row Disclosed counts.** 167 was summed only as a column-alignment control.
11. **The engaged fleet.** `|W_capable| = 4` is measured/cited; engaged ∩ capable is not determinable from
    the roster's status column. The current `dotnet` / pwsh / Go-pin state of i7, i9, R-LAPTOP and G-LAPTOP
    rests on records dated 2026-08-22 … 2026-09-02, not on a probe today; C1's state is cited, never
    probed.
12. **Whether a provisioned cloud lane could become a worker.** Measured only as far as candidate
    availability and host reachability; no install attempted, no smoke gate run, and 27 GB free against the
    sweep's 25 GB floor is untested under an SDK plus corpus build output.
13. **Whether `os-matrix.yml`'s `sweep-shard` can execute a sweep row end to end.**
14. **The campaign's dispatch list.** No dispatch or plan naming the H10 population was read; 204 is the
    banked roster, not a ruled target.
15. **The mailbox branch.** Not fetched, not read. §3.4's shard signals and any post-09-08 ruling could live
    only there, so the ruling set in §9 is complete for this ref, not for the fleet.
16. **Whether the 42-row gap has already been ruled on elsewhere.** The records were not searched for an
    existing ruling, so this may be a re-discovery.

**Bounded output, declared:** per-file classification greps over six documents were bounded to their first
12 matching lines, so shard content below that line in `REHEARSAL-go12312.md`,
`SESSION-PROMPTS-cloud-lanes.md`, `CENSUS-release-tc0-delta.md`, `DESIGN-peros-roster.md`, `CIMatrix.md` and
`LANES.md` would not have surfaced (REHEARSAL §4 and DESIGN-peros §650–665 were read in full to
compensate). The top-8-by-Tests and top-10-by-`t_r` listings are head-bounded displays over complete data.
Nothing outside `docs/` was content-searched except `src/run-validated-sweep.ps1`, `src/_roster.ps1`,
`src/check-roster-format.ps1`, `src/go2cs/testConversion.go` and the generator.

**Measurement predicates** (quoted as patterns so each count is re-runnable; every predicate's first three
matches were printed and eyeballed before its count was allowed to mean anything, and every population was
asserted non-empty):

| Population | Predicate |
|---|---|
| Roster rows (204) | first cell is a backticked markdown link: `^\| \[\`` — and the canonical `^\|\s*\[\`([^\`]+)\`\]\([^)]*\)\s*\|\s*(\d+)\s*\|\s*(\d*)\s*\|` from `src/_roster.ps1`:67, which is what the sweep parses the roster with. ⚠ The known-bad predicate `^\| *\`?[a-z]` reads **6** on this file — the six exclusion-ledger rows, whose first cell is a bare code span — which is why the link shape is the discriminator |
| Exclusion ledger (6) | same table, bare code span in cell 0, no link |
| Table enumeration (2) | a pipe-initial line whose cells are all `^:?-{2,}:?$` (separator by column **shape**, not by "contains `--`" — the `testing` row contains `---` in prose and defeats the naive form) |
| Wall-time rows (162 per block) | blocks sliced by the fence line numbers themselves; rows `^(\S+)\s+(\d+)\s+(\d+)s\s*$` (the generator's own), with an unfiltered negative scan for any last field not `^[0-9]+s$` |
| `$longTimeouts` (11) | `\$longTimeouts\s*=\s*@\{(.*?)\}` with `re.S`, then `'([^']+)'\s*=\s*'[^']+'` |
| 1.24 skeleton (227) | ordinal-prefixed rows in the Appendix body only; ⚠ the same shape unbounded reads **251** = 227 + §4's 24-row table |

**Controls that moved** (a gate never made to fail proves nothing): deleting a roster row moved 204 → 203
and the Tests sum 28,459 → 28,454; injecting a duplicate made the duplicate detector report 1, converting a
potentially vacuous zero into a measured one; one character of the roster predicate collapsed the count to
0; perturbing a **reserved** row's `t_r` moved the reserved leg by exactly +100 s while perturbing a
**non-reserved** row left it unchanged and moved only the total (sensitive **and** specific); renaming one
floor to a canary changed the derived set and made the assert name that exact site, with a byte-identical
restore; deleting one data row fired the generator's cardinality assert; a wrong-ref join moved
204 → 203 → 201 → 189 → 162 across refs. One perturbation correctly did **not** move: scope slop from
L213–L416 to L212–L417 still reads 204, because L212 is the separator and L417 is blank — the line bound is
redundant, not load-bearing.

---

## 11. Open questions for COORD

1. **Which population does the map run over — 204 or 227?** 204 is the banked 1.23.12 roster (what the
   runbook's words literally name); 227 is the go1.24.13 implementable skeleton H10 *"banks INTO"*. They are
   different sets (194 common, 10 roster-only, 33 skeleton-only). If the campaign is "reconvert and re-bank
   everything at 1.24", the skeleton is the population **and the 10 departed names need a 1.24 rekey
   first** — including one reserved floor row that fans out into two (§3e). This lane cannot rule it.
2. **How are the 42 uncovered rows treated?** Options A–D in §2, sized. This record takes no decision and
   fabricates no cost. The recommendation this lane would defend, if asked: **(B) now, (C) at recon** — but
   it is COORD's ruling.
3. **Which machines are the engaged fleet, by name?** `|W_capable| = 4`; the engaged set is not determinable
   from the roster (§6). A count is not a deal.
4. **Who holds the reserved set?** The pin requires the i9 for 11–13 floor rows at 4,722 s+ continuous, the
   i9 has a measured thermal failure at 13 minutes of continuous sweep and a status line calling it offline,
   and the pin binds the makespan at **every** W ≥ 3. Either the pin rule changes or the reserved leg is
   split with cooldowns — and either way the map's headline changes, not just its factors.
5. **Is the generator repaired or retired?** It cannot emit at this tip and the repair is four small edits
   plus a keyed block selector (§5a). Repairing it preserves the derive-never-copy property the BOARD ruled;
   retiring it re-opens the copy-drift failure §3.2 records.
6. **Which row, how many repetitions, recorded where** for the `s_w` calibration pair? Band measured
   (71–226 s), choice not taken (§7).
7. **Does `-ShardCount` get a row-list mode, or does the map dispatch per row?** The native parameter cannot
   express an LPT deal (§8). Open instrument debt either way.

---

## AMENDMENTS

*(empty)*

To amend: append a dated block below this line — `### AMENDED <YYYY-MM-DD> — <what changed, and the ref it
was measured at>` — and never rewrite a line above it. A number that moves is superseded by a new dated
block stating the old value, the new value, the predicate and the tip; it is not edited in place. If a
finding here is falsified, say so in the block and leave the falsified text standing, because the falsified
claim is part of the record.