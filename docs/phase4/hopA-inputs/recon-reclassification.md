# H10 recon: the sixteen NOVERDICT rows, re-classified from evidence

**What this is.** The H10 recon leg produced 228 rows across three lanes. Sixteen carried no verdict
word: thirteen on R's list, three on i9's. `docs/phase4/hopA-inputs/reclassify.py` reads each row's
**committed evidence record** and derives the word the row should carry, so the leg's readings are not
lost to a wrapper defect that has since been fixed.

**Why from records and never from the lane's `diverged` column.** The fifth wrapper blob rewrote every
real `0` to `n/a` (PowerShell's `0 -eq ''` is TRUE), so that column is uninformative for the
fifth-blob lanes. The count comes from the document.

**The predicate, stated so anyone can reproduce or refuse it:**

```
  disclosed entries are SENTENCES; the NAME is the LEADING TOKEN
  verdicts = |go \ names(disclosed)|            a SET difference, never a count difference
  diverged = { n : n in go, leading-token(n) not among disclosed, csharp[n] != go[n] }
  word     = NOVERDICT (by cause)  when status == "conversion-blocked"
             PASS                  when matched and diverged is empty
             DIVERGED              otherwise
```

Every record is read with `object_pairs_hook` so a repeated key raises instead of silently losing a
verdict, and each kept map's size is asserted against the source's pair count.

⚠ **This file's first cut got the disclosed match wrong and the correction is i9's** (`64d873bb2`).
It read `n not in set(disclosed)` — a membership test of a bare name against a set of SENTENCES, which
matches nothing and subtracts nothing — so `diverged` was over-counted by exactly the disclosed-entry
count on every row that had one. The entry COUNT was always right, which is why `verdicts` agreed with
i9's published figures while `diverged` did not, and why `net/http` — the one row of the three with
nothing to subtract — agreed on both and was the control that made the shape legible.

## The sixteen

| row | lane | verdicts | go | disclosed | diverged | status | re-classified word |
|:--|:--|--:|--:|--:|--:|:--|:--|
| `fmt` | R | 63 | 63 | 0 | 2 | `failing` | **DIVERGED** |
| `internal/coverage/cfile` | R | 16 | 16 | 0 | 16 | `conversion-blocked` | **NOVERDICT (by cause)** |
| `internal/godebug` | R | 5 | 5 | 0 | 1 | `failing` | **DIVERGED** |
| `internal/runtime/atomic` | R | 16 | 16 | 0 | 1 | `failing` | **DIVERGED** |
| `internal/trace` | R | 92 | 92 | 0 | 92 | `conversion-blocked` | **NOVERDICT (by cause)** |
| `math/rand` | R | 47 | 47 | 0 | 0 | `validated` | **PASS** |
| `mime/multipart` | R | 52 | 52 | 0 | 0 | `validated` | **PASS** |
| `net/http/pprof` | R | 15 | 15 | 0 | 4 | `failing` | **DIVERGED** |
| `os/user` | R | 17 | 17 | 0 | 3 | `failing` | **DIVERGED** |
| `runtime/pprof` | R | 161 | 161 | 6 | 37 | `failing` | **DIVERGED** |
| `syscall` | R | 65 | 65 | 0 | 1 | `failing` | **DIVERGED** |
| `unicode/utf8` | R | 15 | 15 | 0 | 1 | `failing` | **DIVERGED** |
| `crypto/tls` | i9 | 4759 | 4760 | 1 | 12 | `failing` | **DIVERGED** |
| `net` | i9 | 477 | 479 | 2 | 1 | `failing` | **DIVERGED** |
| `net/http` | i9 | 1387 | 1387 | 0 | 19 | `failing` | **DIVERGED** |

**Outcome: 2 PASS recovered, 11 DIVERGED, 2 NOVERDICT-by-cause, 1 NOVERDICT-for-want-of-evidence.**

`math/rand` and `mime/multipart` are the two the leg would otherwise have thrown away: both carry
`rc 0`, a real verdict count and `diverged UNREAD` in their lane TSV, and both records say
`matched: true` with an empty diverged set. They re-classify to **PASS**.

**All three of i9's counts now reproduce exactly**, which is the check that the correction is the
right one rather than merely a different one:

```
  crypto/tls   i9= 12  here= 12   AGREES
  net          i9=  1  here=  1   AGREES
  net/http     i9= 19  here= 19   AGREES
```

## ⚠ `runtime/pprof`: the one record where `disclosed` is not a subset of `go`

The corrected predicate carries a guard the first cut did not have — **a disclosed entry whose derived
name is absent from the record's own `go` map is reported, never silently subtracted** — and it fired
immediately, on the one row nobody was looking at:

```
  R/runtime/pprof: 6 of 6 disclosed entr(ies) name a test `go` does not carry
       TestBlockMutexProfileInlineExpansion
       TestBlockProfile
       TestMutexProfile
       TestMutexProfileRateAdjust
       TestProfileRecordNullPadding
       TestProfilerStackDepth
```

All six are `host-fatal` disclosures — tests the host could not run at all — and **none of the six is
in `go`**. (`TestBlockProfile` has one `go` key beginning with it, `TestBlockProfileBias`, which is a
different test and not a subtest. Checked rather than assumed.)

⚠ **RULED (COORD `22d3b01e1` §2): `verdicts` is a SET difference, not a count difference.** The count
form `len(go) − len(disclosed)` assumes `disclosed ⊆ go`, which holds on every other record here and
fails on this one; it subtracted six names that were never among the 161 and read **155**. Subtracting
a name that is not there must subtract nothing, so **this row reads 161**, and the set form is
identical to the count form wherever the subset holds — which is everywhere else.

**The guard's report stays.** A disclosed name absent from `go` is still reported by name, because the
absence is a fact about the record worth surfacing even now that it costs no verdicts: it says the
`host-fatal` class names tests that never produced a Go verdict at all. `diverged` was never affected
either way — a name absent from `go` removes nothing from that set, so 37 stands.

## ⚠ `testing` — the thirteenth R row, and it has no record

R's list carries `testing` as `NOVERDICT / NOMATCH / rc 1`, and R's evidence commit holds **twelve**
records for **thirteen** rows: `testing` is the one without. It therefore stays NOVERDICT, and not by
the same cause as the two host rows — there is nothing to read.

⚠ **Worth a ruling rather than a silent carry:** `testing` is a **hand-owned** package
(`src/core/testing`, per CLAUDE.md's one-tree rule) and the runbook line landed at `ccdf252fb` says
testing is excluded from every `-tests` list. A hand-owned, excluded package appearing as a population
row at all is either a population-file defect or a deliberate inclusion nobody has stated. **It is one
row and it changes no arithmetic here**, because an unmeasured row is dropped from the basis either
way — but the next leg will meet it again.

## The two host rows: NOVERDICT **by cause**, and the tell fired on exactly those two

```
  R/internal/coverage/cfile: 16 of 16  status=conversion-blocked
  R/internal/trace: 92 of 92  status=conversion-blocked
```

The tell (a row whose net undisclosed diverged set equals its **whole** verdict count is what a side
that never ran looks like) named `internal/coverage/cfile` and `internal/trace` and **nothing else**
across all fifteen records. It is a detector, never a decider.

⚠ **Both are now confirmed host-refusal rows and one of them was hiding a real divergence.** Under
`GODEBUG=winsymlink=0` on the box where the failure lives, `cfile` reads **PASS, 15 verdicts** — the
privileged box's exact figure — and `internal/trace` reads a genuine **DIVERGED 4 of 92**:
`TestTraceCPUProfile` and its three subtests, `go=pass cs=fail`. Those four names go to this seat's
DIVERGED classification as a recon reading; the driver re-measures both rows at the tip once the host
seat lands. So 92-of-92 *was* a side that never ran and 4-of-92 is a reading, and the tell separated
them without knowing which it had.
