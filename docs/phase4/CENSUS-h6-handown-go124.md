# CENSUS — H6 hand-own re-audit at go1.24.13

**Read-only. Proposals only — no cuts, no dispositions applied, no ruling implied.** The input the
H6 GATE consumes: every hand-owned file, its Go principal, and that principal's status at the
incoming release.

Author: R, 2026-09-07, at master `2c0107614`. Toolchain pins, bare version lines alone:

```
  go version go1.23.12 windows/amd64      <- the OUTGOING tree
  go version go1.24.13 windows/amd64      <- the INCOMING tree
```

---

## 1. THE POPULATION — 153, re-measured

```
  marked, LINE-ANCHORED  ^\s*\[module:\s*(go\.)?GoManualConversion\]     142 files
  *_impl.cs companions                                                   109 files
  union (the census population)                                          153 files
    of which marked AND *_impl.cs                                         98
    marked but NOT *_impl.cs  (whole-file rewrites)                       44
    *_impl.cs carrying no marker (pure supplements)                       11
```

⚠ **Re-measured, never carried.** The 2026-08-24 reading was *73 marked / 49 companions / 24
whole-file rewrites*. The population has roughly **doubled** since. Any H6 plan sized on the old
figures is sized on a corpus that no longer exists.

**CONTROL — the anchored scan is the one that ran.** Re-run with the unanchored pattern the
CLAUDE.md caveat warns about:

```
  ANCHORED     142        <- the census figure
  UNANCHORED   221        must read HIGHER, and does: PASS
```

The 79-file difference is the documented over-count and is named rather than assumed: files that
*mention* the marker in bodyless-partial placeholder comments (`reflect/deepequal.cs`,
`reflect/type.cs`, `reflect/value.cs`, `internal/reflectlite/value.cs`, and 70 more), plus the
attribute's own declaration `golib/GoManualConversionAttribute.cs`. **A head-window scan would
under-count and an unanchored scan over-counts by 55%; only the anchored whole-file scan is the
population.**

**Hand-owned by CONSEQUENCE (the class of four), re-verified rather than carried** — packages whose
every non-test Go file is hand-owned, so `unmarkedFileCount == 0` makes the driver `continue` and
their `.csproj`/`package_info.cs`/`README.md` are never re-emitted:

```
  crypto/internal/boring/bcache   5 .cs, 1 marked
  internal/concurrent             7 .cs, 2 marked      <- package REMOVED at 1.24.13
  internal/godebug                5 .cs, 1 marked
  internal/weak                   5 .cs, 1 marked      <- package REMOVED at 1.24.13
```

**Skip-listed (hand-owned by mechanism):** `unsafe` (2 .cs), `testing` (18 .cs).

## 2. PRINCIPAL STATUS AT go1.24.13

**Resolution is two-tier, and the second tier is a finding in itself.**

```
  file-resolved     103   the hand-own's name maps 1:1 to a Go file in the 1.23.12 tree
  package-resolved   50   the name is go2cs-INVENTED; no such Go file exists
```

⚠ **A third of the population carries a name Go never had.** `internal/poll/runtime_netpoll.go`,
`internal/syscall/windows/zsyscall_windows_ptrout.go`, `math/math.go`, `runtime/nanotime.go` — all
confirmed absent from the 1.23.12 tree. These are go2cs splits and supplements whose principal is a
*set of members inside a package*, not a file. **They are classified by PACKAGE status here and
their file-level principal must be named by hand at H6**; a census that silently mapped them to a
non-existent path would have reported a clean 1:1 world that does not exist.

```
  STATUS                     COUNT   TIER
  PRESENT-UNCHANGED            50    file      principal byte-identical across the trees
  PRESENT-CHANGED              48    file      principal exists and DIFFERS
  PKG-CHANGED                  48    package   some .go in the package differs
  REMOVED-package-gone          5    both      the package is gone at 1.24.13
  MOVED-or-NEW-SHAPE            1    file      package present, principal file absent
  PKG-UNCHANGED                 1    package
                              ---
                              153
```

**PRESENT-UNCHANGED is derived by HASH, never by reading.** Named control, one row stated in full:

```
  hand-own   src/core/crypto/internal/boring/bcache/cache.cs
  principal  crypto/internal/boring/bcache/cache.go
  sha256     5ec903ab72fbaf9ecea84e8a0ac0b634…  at go1.23.12
             5ec903ab72fbaf9ecea84e8a0ac0b634…  at go1.24.13   identical
```

### The REMOVED rows — 5

```
  src/core/crypto/internal/alias/alias_impl.cs          crypto/internal/alias/alias.go
  src/core/internal/concurrent/hashtriemap.cs           internal/concurrent/hashtriemap.go
  src/core/internal/concurrent/hashtriemap_whitebox.cs  internal/concurrent/hashtriemap_whitebox.go
  src/core/internal/weak/pointer.cs                     internal/weak/pointer.go
  src/core/vendor/golang.org/x/crypto/sha3/xor.cs       vendor/golang.org/x/crypto/sha3/xor.go
```

**CONTROL — the known-removed packages, checked DIRECTLY rather than only through hand-owns**, since
a package with no hand-own cannot appear in the table above:

```
  PACKAGE                 1.23.12   1.24.13   in corpus   hand-owns
  internal/weak           present   ABSENT    yes         1     <- fires
  internal/concurrent     present   ABSENT    yes         2     <- fires
  runtime/internal/sys    present   ABSENT    yes         0     removed, no hand-own
  runtime/internal/math   present   ABSENT    yes         0     removed, no hand-own
```

All four of G's H3 removals confirmed. **The last two are corpus removals with no hand-own row**, so
they are H6 findings without being hand-own dispositions.

### MOVED-or-NEW-SHAPE — 1

`src/core/crypto/subtle/xor_generic.cs` → `crypto/subtle/xor_generic.go`, **absent at 1.24.13**; the
package now carries `xor.go`. The principal was consolidated, not deleted.

## 3. REGISTRY ROWS TIED TO REMOVED OR CHANGED PRINCIPALS

```
  linknamePushTargets      22 rows    2 in REMOVED packages    19 present    1 not-a-std-pkg
  manualConversionFuncs    22 keys    1 in a REMOVED package
```

**POSITIVE CONTROL — the two `internal/weak` rows G measured failing `TestLinknamePushRegistryMatchesGoSource`:**

```
  internal/weak.runtime_registerWeakPointer   linknameOperations.go:503
  internal/weak.runtime_makeStrongFromWeak    linknameOperations.go:507
```

**Found — and only on the second pattern.** A grep for the quoted package path `"internal/weak"`
returns **zero**: the registry keys are `"<package>.<symbol>"`, so the package path never appears as
a standalone quoted string. The control did its job by disagreeing with a search that looked right.
Both rows already carry a comment recording that their remedy landed (the hand-owned managed weak
reference), which is context H6 should read before disposing of them.

`manualConversionFuncs`: `"crypto/internal/alias"` is the single key whose package is gone.

## 4. PROPOSED DISPOSITIONS — proposals, not rulings

| # | rows | proposal | why |
|---|---|---|---|
| 1 | `internal/weak/pointer.cs` + its 2 push rows | **retire with the package** | the package is gone at 1.24.13; `unique` moves to the new home. The push rows' own comments say the remedy already landed, so retiring them removes a registration with no destination rather than losing behaviour. |
| 2 | `internal/concurrent/hashtriemap.cs`, `hashtriemap_whitebox.cs` | **re-route** | 1.24 rebuilds `sync.Map` on `internal/sync`'s HashTrieMap. The hand-own is the same data structure at a new import path; the question H6 rules is whether it re-derives against `internal/sync` or retires in favour of the converted upstream principal. |
| 3 | `crypto/internal/alias/alias_impl.cs` + its registry key | **re-route** | package gone; the aliasing helpers move. One `manualConversionFuncs` key follows wherever the row goes. |
| 4 | `crypto/subtle/xor_generic.cs` | **re-derive against the new principal** | consolidated into `xor.go`; the hand-own's members must be re-read against the merged file. |
| 5 | `vendor/.../sha3/xor.cs` | **retire with the package** | vendored tree reshaped; go2cs follows GOROOT's vendor set. |
| 6 | the 48 PRESENT-CHANGED | **re-derive against the new principal, individually** | each principal exists and differs; the diff decides whether the hand-own's premise still holds. **This is the bulk of H6's judgement and it does not compress.** |
| 7 | the 50 PRESENT-UNCHANGED | **keep** | principal byte-identical; nothing to re-audit beyond confirming the hash at gate time. |
| 8 | the 50 package-resolved | **name the principal first, then classify** | these cannot be dispositioned until their file-level principal is stated; 48 sit in changed packages, so most will land in row 6. |
| 9 | `runtime/internal/sys`, `runtime/internal/math` | **retire with the package** | removed at 1.24.13, no hand-own rows, but corpus packages that must go. |

⚠ **Row 6 plus row 8 is 96 of 153.** The re-audit is not a sweep with a few exceptions; the majority
of hand-owns face a principal that moved, and the census cannot decide any of them — it can only say
which ones need a human to look.

## 5. WHAT THIS CENSUS DOES NOT CLAIM

- **It does not summarise the Go diffs.** COORD's spec asks for a one-line "what moved" per
  PRESENT-CHANGED principal; 48 of those are one-line summaries of real Go changes, and writing them
  from a hash inequality would be fabrication. **They are named but not characterised here**; that
  read is H6's own work, or a follow-up census with the diffs actually read.
- It does not resolve the 50 package-resolved principals to files.
- It does not verify that a REMOVED package's replacement is what I have guessed it to be
  (`internal/sync` for the hash-trie map is from COORD's recon, not from my measurement).

---

## 2026-09-07 — FOLLOW-UP: the diffs READ, the member sets NAMED (appended, §1–§5 unchanged)

COORD's follow-up (`29ec04cd8`): read the Go diffs, one classified line per PRESENT-CHANGED row,
name the member set each package-resolved row displaces, and extend the registry check by symbol.
**Proposals stay proposals; no ruling implied.**

Instrument: `arm14_h6diff`, a `go/ast` classifier — signatures and bodies compared with comments
excluded via `go/printer`, so a reflowed comment cannot read as a changed signature. **Precedence,
because a file can be several classes at once and the spec wants exactly one:**
`BUILD-CONSTRAINT > MEMBERS-REMOVED > MEMBERS-ADDED > SIGNATURE > BODY-ONLY > COMMENT-ONLY`.

⚠ **CONTROL — the classifier was made to emit EVERY class before any row was believed**, on six
fixtures differing from one base by exactly one property:

```
  base vs comment     -> COMMENT-ONLY
  base vs body        -> BODY-ONLY 1 decl(s)
  base vs sig         -> SIGNATURE func Alpha
  base vs added       -> MEMBERS-ADDED func Beta
  base vs removed     -> MEMBERS-REMOVED func Alpha
  base vs constraint  -> BUILD-CONSTRAINT //go:build linux -> //go:build linux && amd64
```

## 6. THE 48 PRESENT-CHANGED ROWS, CLASSIFIED

### 6a. File-level class — what the principal file itself did

```
  MEMBERS-REMOVED    20      SIGNATURE           2
  BODY-ONLY          11      BUILD-CONSTRAINT    0   <- EMPTY
  MEMBERS-ADDED       8      PARSE-ERROR         0   <- every principal parsed
  COMMENT-ONLY        7                         ---
                                                 48
```

**BUILD-CONSTRAINT is EMPTY, and the diff that would have populated it is the control fixture above**
(`//go:build linux` → `//go:build linux && amd64`), which the classifier emits correctly. No
hand-own's principal changed its build constraint between the releases.

### ⚠ 6b. SCOPE-CORRECTED class — and it moves 11 of the 28 member-set rows

A file-level comparison cannot tell *removed from the package* from *relocated to a sibling file*,
and Go does the latter constantly. Every `MEMBERS-REMOVED`/`MEMBERS-ADDED` name was therefore
re-checked against **the whole package** in the other tree:

```
  BODY-ONLY                            11
  MEMBERS-REMOVED                       9   truly gone from the package
  MOVED-WITHIN-PACKAGE                  8   the file lost it; the package still has it
  COMMENT-ONLY                          7
  MEMBERS-ADDED                         6
  MIXED (some gone, some relocated)     5
  SIGNATURE                             2
                                       ---
                                        48
```

⚠ **The scope check was itself wrong the first time, and the error was mine in the instrument.** The
classifier truncated its name list at four with `(+N more)`, so the first scope pass compared a
SAMPLE and returned `MEMBERS-REMOVED 13 / MOVED 7 / MIXED 2`. With truncation removed it returns
**9 / 8 / 5** — four rows moved class. `sync/atomic/doc.go` reading "MEMBERS-REMOVED `AddInt64`" is
what exposed it: Go 1.24 did not delete `atomic.AddInt64`, so the instrument was answering a
narrower question than the one asked of it.

**THE NUMBER H6 IS SIZED ON:**

```
  MECHANICAL (re-derive without judgement)   COMMENT-ONLY + BODY-ONLY + MOVED-WITHIN-PACKAGE  = 26
  NEEDS A HUMAN                              MEMBERS-REMOVED + ADDED + MIXED + SIGNATURE      = 22
```

### 6c. The rows that need a human, named

**SIGNATURE (2)** — `runtime/os_linux.go` `type mOS`; `sync/once.go` `type Once`. Both are types a
hand-own mirrors structurally, so a changed shape is exactly the case that cannot re-derive
mechanically.

**MEMBERS-REMOVED (9)** include `internal/abi/type.go` (`KindGCProg`, `TFlagUnrolledBitmap`,
`MapType.HashMightPanic`, `MapType.IndirectElem`, +4), `runtime/mbitmap.go` (`heapSetType`,
`getgcmask`, `materializeGCProg`, `dematerializeGCProg`), `runtime/stubs.go` (`getcallerpc`,
`getcallersp`, `getclosureptr`), `runtime/lock_futex.go` and both `lock_sema.go` flavors (the
`active_spin`/`mutex_*` constant family), `sync/mutex.go`, `sync/runtime.go`
(`runtime_SemacquireMutex`, `runtime_canSpin`, `runtime_doSpin`, `runtime_nanotime`),
`testing/testing.go` (`testContext` and its methods), `time/time.go`.

**MIXED (5)** — `reflect/value.go` and `sync/atomic/doc.go` are the two large ones: part of the
named set survives in the package, part is gone. These need the per-member read, not a file verdict.

## 7. THE 50 PACKAGE-RESOLVED ROWS — MEMBER SETS, CHECKED BY NAME

```
  ALL-MEMBERS-PRESENT     36   every member the hand-own supplies still exists in the 1.24.13 package
  NO-NAME-MATCH           13   the C# names are go2cs-minted or host-only; no Go name to check
  PKG-REMOVED              1   internal/concurrent/hashtriemap_whitebox.cs
  SOME-MEMBERS-REMOVED     0
                          ---
                           50
```

⚠ **`SOME-MEMBERS-REMOVED` is ZERO: the package-resolved half is far LESS exposed than the
file-resolved half.** 36 of 50 need no member-level work at all.

**The 13 NO-NAME-MATCH rows split into two groups, and the distinction matters for H6:**

- **Host infrastructure, displacing no Go member at all (4):** `testing/PackageAncestry.cs`,
  `TestRunner.cs`, `TestFormat.cs`, `TestReporter.cs`. These are the hand-owned Phase-4 test host's
  own machinery — there is no Go principal by design, and arguably they do not belong in a
  *hand-own re-audit* population at all. **Proposal: reclassify out of the H6 population** rather
  than dispose of them per-row.
- **go2cs-minted shells over real Go members (9):** `runtime/goargs_impl.cs`, `goenvs_impl.cs`,
  `hostofrecord_impl.cs`, `panicvalues_impl.cs`, the `zsyscall_windows_*` splits,
  `internal/syscall/unix/linux/net_linux_impl.cs`. These displace Go members under C# names the
  matcher cannot follow; **H6 reads these by hand — the census can only say that it cannot.**

**METHOD LIMIT, stated rather than discovered later:** members are matched **by name** across the
package. An unexported helper Go renamed reads as removed; a C# name go2cs minted reads as
NO-NAME-MATCH. **This is a triage that tells H6 where to look, not a proof of what is there.**

## 8. THE REGISTRY, EVERY KEY CHECKED BY `<package>.<symbol>`

```
  linknamePushTargets, 22 rows
    SYMBOL-PRESENT    19      the symbol still exists at 1.24.13
    PKG-REMOVED        2      internal/weak.runtime_registerWeakPointer
                              internal/weak.runtime_makeStrongFromWeak
    PKG-NOT-STD        1      runtime/metrics_test.runtime_readMetricNames (a test package)
    SYMBOL-REMOVED     0
```

⚠ **`SYMBOL-REMOVED` is ZERO.** Beyond the `internal/weak` pair — G's two, reproduced here as the
positive control — **no registry key has lost its symbol at 1.24.13.** The registry's exposure to
this hop is exactly two rows in one removed package.

The key shape is the method, and it is the lesson this lane paid for twice: a grep for the quoted
package path `"internal/weak"` returns **zero**, because keys are `"<package>.<symbol>"` and the
path never stands alone.

## 9. PROPOSALS, UPDATED — still proposals

| # | rows | proposal |
|---|---|---|
| 10 | the 26 MECHANICAL (COMMENT-ONLY, BODY-ONLY, MOVED-WITHIN-PACKAGE) | **re-derive mechanically, no per-row ruling** — the principal's member set and signatures are intact |
| 11 | the 22 needing a human | **per-row ruling at H6**, prioritised SIGNATURE (2) → MIXED (5) → MEMBERS-REMOVED (9) → MEMBERS-ADDED (6) |
| 12 | the 36 ALL-MEMBERS-PRESENT package-resolved rows | **keep**; member sets intact |
| 13 | the 4 host-infrastructure rows | **reclassify OUT of the H6 population** — they displace no Go principal |
| 14 | the 9 go2cs-minted shells | **name by hand at H6**; the census cannot follow the name mapping |
| 15 | the registry | **only the 2 `internal/weak` rows are exposed**; the other 20 need no H6 action |
