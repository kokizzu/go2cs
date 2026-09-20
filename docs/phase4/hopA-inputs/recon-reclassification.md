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
  verdicts = len(go) - len(disclosed)
  diverged = { n : n in go, n not in disclosed, csharp[n] != go[n] }
  word     = NOVERDICT (by cause)  when status == "conversion-blocked"
             PASS                  when matched and diverged is empty
             DIVERGED              otherwise
```

Every record is read with `object_pairs_hook` so a repeated key raises instead of silently losing a
verdict, and each kept map's size is asserted against the source's pair count.

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
| `runtime/pprof` | R | 155 | 161 | 6 | 37 | `failing` | **DIVERGED** |
| `syscall` | R | 65 | 65 | 0 | 1 | `failing` | **DIVERGED** |
| `unicode/utf8` | R | 15 | 15 | 0 | 1 | `failing` | **DIVERGED** |
| `crypto/tls` | i9 | 4759 | 4760 | 1 | 13 | `failing` | **DIVERGED** |
| `net` | i9 | 477 | 479 | 2 | 3 | `failing` | **DIVERGED** |
| `net/http` | i9 | 1387 | 1387 | 0 | 19 | `failing` | **DIVERGED** |

**Outcome: 2 PASS recovered, 11 DIVERGED, 2 NOVERDICT-by-cause, 1 NOVERDICT-for-want-of-evidence.**

`math/rand` and `mime/multipart` are the two the leg would otherwise have thrown away: both carry
`rc 0`, a real verdict count and `diverged UNREAD` in their lane TSV, and both records say
`matched: true` with an empty diverged set. They re-classify to **PASS**.

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


TELL-NAMED (net undisclosed diverged set == whole verdict count):
  R/internal/coverage/cfile: 16 of 16  status=conversion-blocked
  R/internal/trace: 92 of 92  status=conversion-blocked

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

## ⚠ Two counts disagree with the lane's own published figures

Read with the predicate above, two of i9's three records give a different diverged count from the one
i9's evidence entry published. **The words are unaffected — all three are DIVERGED either way — only
the counts differ.**

i9 count comparison (i9's published figure vs this record read with the stated predicate):
  crypto/tls   i9= 12  here= 13   DISAGREES
       TestBogoSuite
       TestBogoSuite/ALPNClient-TLS-TLS13
       TestBogoSuite/CertificateVerificationSucceed-Client-TLS1-CustomCallback-TLS-Sync
       TestBogoSuite/ChannelID-Client-TLS12-TLS-Async-SplitHandshakeRecords
       TestBogoSuite/ChannelID-Server-TLS12-TLS-Sync
       TestBogoSuite/Compliance-wpa-202304-TLS-Server-RSA_PKCS1_SHA384
       TestBogoSuite/FalseStart-SessionTicketsDisabled-TLS-Async-ImplicitHandshake
       TestBogoSuite/MinimumVersion-Server2-TLS12-TLS11-TLS
       TestBogoSuite/NoExtendedMasterSecret-TLS12-Server
       TestBogoSuite/TLS-TLS1-ECDHE_ECDSA_WITH_AES_128_CBC_SHA-server
       TestBogoSuite/TLS-TLS11-ECDHE_RSA_WITH_AES_128_CBC_SHA-LargeRecord
       TestBogoSuite/WrongMessageType-CertificateVerify-TLS
       TestCertCache
  net          i9=  1  here=  3   DISAGREES
       TestAllocs
       TestIPAppendTextNoAllocs
       TestTCPReadWriteAllocs
  net/http     i9= 19  here= 19   AGREES

**What was ruled out, at the record:** none of the disagreeing names appears in that record's
`excluded`, `gated` or `skipped` set, so no exclusion list the document itself carries closes the gap.
`net/http` agrees exactly at 19 under the same predicate, which is what makes the other two worth
naming rather than shrugging at — one predicate cannot be both right and wrong on three documents from
one instrument.

**Two shapes worth testing, neither claimed:** `crypto/tls`'s thirteen are `TestBogoSuite` plus eleven
of its subtests plus `TestCertCache`, so an instrument counting only LEAF names would read twelve.
That shape does not explain `net`, whose three names (`TestAllocs`, `TestIPAppendTextNoAllocs`,
`TestTCPReadWriteAllocs`) have no parent/child structure between them — though all three are
**alloc-assert** tests, which the banking rules do treat specially. ⚠ **Both are guesses about
someone else's instrument and neither is a finding.** The resolution is i9's: this record states what
the committed documents say under one stated rule, and names the gap rather than picking a number.

