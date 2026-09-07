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
