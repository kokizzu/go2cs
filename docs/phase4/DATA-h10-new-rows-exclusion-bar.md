# DATA — the fourteen first-time H10 candidates against the exclusion bar: TEN rows, FOUR excluded

**Point-in-time record, 2026-09-22, lane C1.** Seat `claude/c1-h10-new-rows` off `c6fdbe73c3`.
Every package was read against E1–E4 at the pinned 1.24.13 source **before** a row was written. Ten
rows are minted; four are refused, with the reason per package. Verdict counts come from the s2 TSVs
(i9's `claude/i9-h10-s2-evidence` blob `7c1cc936d7`) and from COORD's s2 lines for the six on the i7's
and G's legs.

## Refused — 4

| package | verdicts | bar | why |
|:--|--:|:--|:--|
| `runtime/internal/wasitest` | 1 | **E1** | **No eligible test on the platform of record.** `nonblock_test.go` is `//go:build !aix && !plan9 && !solaris && !wasm && !windows`, so it does not select on windows/amd64 at all. The only test that does select, `TestTCPEcho` in the untagged `tcpecho_test.go`, opens with `if target != "wasip1/wasm" { t.Skip() }` — and the oracle target is windows/amd64. `host_test.go` declares no test. So the package's executing test surface on the platform of record is **empty**, and its single verdict is a skip. |
| `net/internal/cgotest` | 1 | **E4** | **The comparison is sound and validates nothing.** Its only test is `func Test(t *testing.T) {}` — an empty body — under Go's own comment: *"Nothing to test here. The test is that the package compiles at all. See resstate.go."* A pass carries no information about the port; the compile it stands for is already gated by the build. |
| `internal/copyright` | 1 | **E4** | **Validates the Go distribution, not the port.** `TestCopyright` (`copyright_test.go:34`) walks `testenv.GOROOT(t)/src` and errors `"%s: missing copyright notice"` per file. Both sides walk the SAME GOROOT and reach the same answer by construction, independent of anything go2cs emits. |
| `crypto/internal/fips140deps` | 1 | **E4** | **Validates the Go tree's import policy, not the port.** `TestImports` (`fipsdeps_test.go:32`) shells out to `go list` (`t.Fatalf("go list: %v\n%s", …)`) and asserts *"unexpected import of internal package"* and *"package %s does not import crypto/internal/fips140/check"* over the fips140 source tree. Same input, same answer, both sides. |

**The three E4 calls are judgements and are flagged as such.** Each turns on "a pass that carries no
information about the conversion" rather than on a mechanical property, and the bar's own text notes
that its members are judgements carrying a revisit condition. The revisit condition here is uniform:
if a row is wanted for the BUILD signal these packages do carry, that is a different column from
`Tests`, and the E-bar decision should be reopened deliberately rather than by adding the row. E1 for
`wasitest` is not a judgement — the test surface is measurably empty.

## Minted — 10

| package | Tests | Disclosed |
|:--|--:|--:|
| `crypto/hkdf` | 3 | — |
| `crypto/internal/fips140/ecdh` | 1 | — |
| `crypto/internal/fips140/rsa` | 123 | — |
| `crypto/internal/sysrand` | 6 | — |
| `crypto/mlkem` | 8 | — |
| `crypto/pbkdf2` | 5 | — |
| `go/ast/internal/tests` | 3 | — |
| `internal/coverage/test` | 6 | — |
| `internal/pkgbits` | 2 | — |
| `unique` | 21 | **1** |

All ten are `PASS`, `diverged 0`, `banked yes` in their s2 TSV row, and each has a proof page already
carried by its leg's ref (`a71adf7b91`, `8236d68a06` or `06f37e478e`), which the row links in the
roster's own relative form.

`unique`'s Disclosed is **1**, not 0, from the TSV's `manifest_pins` column: its tracked manifest
carries `TestMakeClonesStrings`, class `codegen-liveness`. Every other row's `manifest_pins` is 0.

`crypto/internal/sysrand`'s row states the one thing a reader would otherwise mis-count: 7 test
functions are declared but `TestReadError` sits behind `//go:build cgo` and does not select at CGO 0,
which is why the verdict count is 6.

## Two things deliberately NOT done

**No `· linux: N` annotation.** The s2 legs ran windows/amd64; a linux arm count for these ten does not
exist, and 13 of the roster's existing 203 rows already carry no such marker, so omitting it is
precedented rather than conspicuous. Inventing the figure would put an unmeasured number in the row of
record. **The linux arm count for these ten is OWED at the next linux pass.**

**No header edit**, as briefed — the guard re-derives the total. Note the total it will re-derive is
**213**, not the 209 the six-row brief anticipated: 203 + 10 minted, with 4 of the 14 refused.

## One pre-existing defect, reported and not touched

The roster's row block is alphabetically sorted except for one adjacent pair, `encoding/xml` followed
by `encoding/pem`. **This is present at the base `c6fdbe73c3`** (verified by re-deriving the ordering
from `git show c6fdbe73c3:docs/ValidatedTestPackages.md`: 203 rows, the same single inversion), so it
is not this seat's and is left alone. All ten insertions are individually in correct alphabetical
position, and no row is duplicated.

---

# AMENDMENT 2026-09-22 — the four refusals are now ROSTER FACTS, not just a record

The four packages this record refused at the bar are entered in the roster's own
**## Excluded packages** table, in its row shape (`| package | verdicts | class | mechanism |
rooting |`), grouped by class as that table already groups: `runtime/internal/wasitest` with the E1
rows, and `net/internal/cgotest`, `internal/copyright`, `crypto/internal/fips140deps` with the E4
rows. Ten exclusion rows now: 5 E1, 1 E3, 4 E4.

**Why this commit exists at all** is a consequence worth recording, because it came back around from
a different seat. `regen-validation-index.py` was taught to admit a proof page whose package the
exclusion table names with a bar class — and the durable source for that was ruled to be **the roster
alone**, never a `docs/phase4` record, because a record is by doctrine "amended with dated blocks,
never rewritten, NEVER EXECUTED FROM". So while these four refusals lived only *here*, they were not
roster facts and their pages went on orphaning. This record documents the reasoning; the roster
carries the fact. That division is the point.

**The verdict column.** Each of the four reads **1**, which is the figure its s2 TSV row recorded —
not the `0` the other E1 rows carry. The difference is real and the mechanism sentences carry it: for
`wasitest` the single verdict IS the immediate `t.Skip`, and for the three E4 rows it is the one
empty-or-vacuous test executing and passing. Writing `0` would have tidied the column at the cost of
contradicting the measurement.

**Not hand-edited:** the header's excluded count and the implementable denominator are derived by the
guard at the leg, so nothing above the tables moved.

## The prediction, tested before it was claimed

COORD's prediction was that the ten rows plus these four exclusion rows take the index tool's orphan
count at the tip from 14 to 0. **Simulated and MET**, by layering this seat's roster over the
re-bank train HEAD's proof pages (`07240495e6`, 226 pages) and running the tool's own readers:

| roster | orphans | by name | by link | by exclusion | page-less rows |
|:--|--:|--:|--:|--:|:--|
| train HEAD (203 rows, 6 exclusions) | **14** | 202 | 10 | 0 | `crypto/internal/fips140test` |
| + this seat (213 rows, 10 exclusions) | **0** | 212 | 10 | **4** | `crypto/internal/fips140test` |

Stated as what it is: a **simulation**, not the merged tree. It layers one roster over another leg's
pages, so if a concurrent leg adds pages or rows the real figure moves. What it does establish is that
the arithmetic closes and that all four of these rows are readable by the tool's own parser — checked
by calling `read_roster_exclusions` on this file and seeing all four come back with their classes.

`crypto/internal/fips140test`'s page-less row is untouched and still refuses. That is the hop debt and
the guard being right, not something for this seat to clear.

**The three fips140 rows are still HELD** — `claude/r-h10-rebank-s2b` had not landed when this commit
was cut (`git ls-remote` empty), so `crypto/internal/fips140/{aes,ecdsa,nistec}` are not in it and
their proof pages still do not exist on any ref.
