# H3 — the `std` package delta from Go 1.23.12 to 1.24.13 (2026-09-07)

**Measurement only.** No fixes, no converter changes, no roster edits. Every number below is derived
at run time from the two toolchain roots on one box; nothing is carried forward from a prior record.

| | |
|---|---|
| Corpus | `bef7a6dbd` (`claude/g-hop-h1`, = master `f4ced674d` + H1.2 + H1.3) |
| Outgoing | Go **1.23.12** (`C:\Users\<user>\sdk\go1.23.12`) |
| Incoming | Go **1.24.13** (`C:\Users\<user>\sdk\go1.24.13`) |
| Host | i7-5820K (6C/12T Haswell-E), Windows 11, `windows/amd64` |
| Instrument | `go list -tags purego,math_big_pure_go std`, `GO111MODULE=off`, `GOOS`/`GOARCH` per target |

⚠ **The instrument reproduces the converter's loader conditions rather than a convenient default.**
The queue is built by `packages.Load(cfg, "std")` (`stdLibConverter.go:270`) through
`stdLibLoadConfig`, which sets `GO111MODULE=off` and injects `GOOS`/`GOARCH`, carrying
`loaderBuildFlags()` — and the `-stdlib` default tag set is `purego,math_big_pure_go`
(`commandLineOptions.go:224`). A census run without those tags, or in module mode, answers a
different question.

## 1. Headline arithmetic

```
                 windows/amd64   linux/amd64   darwin/amd64
  go1.23.12          306            304            305
  go1.24.13          346            344            345
  net                +40            +40            +40
```

**The delta decomposes identically on every target: 54 added, 14 removed.** The added set and the
removed set are **identical across all three targets** (`diff` over the sorted lists returns empty
both ways), so the classification below is done once rather than three times.

⚠ **`+40` is the NET, not the additions.** My own first pass printed the added list under a heading
that said "THE 40 ADDED"; there are **54**. The two numbers differ by exactly the 14 removals, and
quoting the net as though it were the addition count understates the new conversion surface by a
third.

## 2. Class table

| class | n | notes |
|:--|--:|:--|
| **Added — relocation targets** | 13 | successors of removed packages; derived by file-name overlap, §3 |
| **Added — genuinely new** | 41 | of which **28** are the FIPS-140 module |
| **Removed — relocated** | 13 | every one has a successor in the added set |
| **Removed — deleted outright** | 1 | `go/internal/typeparams` |
| **Experiment-gated (membership)** | 1 | `testing/synctest`, §5 |

The 13 genuinely-new packages that are *not* FIPS-140 — the ones carrying independent risk:

```
crypto/internal/entropy                    crypto/mlkem     go/ast/internal/tests   internal/runtime/maps
crypto/internal/impl                       crypto/pbkdf2    internal/copyright      internal/synctest
crypto/internal/sysrand                    crypto/sha3      internal/exportdata     internal/syslist
crypto/internal/sysrand/internal/seccomp
```

## 3. The removals are RELOCATIONS, and the successor is measured rather than guessed

A name-similarity mapping would be a plausible story. **The mapping below is derived by `.go`
file-name overlap** between each removed package at 1.23.12 and every added package at 1.24.13,
reporting the best-scoring target with its denominator:

| removed (1.23.12) | successor (1.24.13) | file overlap |
|:--|:--|:--:|
| `crypto/internal/alias` | `crypto/internal/fips140/alias` | 1/2 |
| `crypto/internal/bigmod` | `crypto/internal/fips140/bigmod` | 4/4 |
| `crypto/internal/edwards25519` | `crypto/internal/fips140/edwards25519` | 11/11 |
| `crypto/internal/edwards25519/field` | `crypto/internal/fips140/edwards25519/field` | 9/9 |
| `crypto/internal/mlkem768` | `crypto/internal/fips140/mlkem` | 1/2 |
| `crypto/internal/nistec` | `crypto/internal/fips140/nistec` | 10/13 |
| `crypto/internal/nistec/fiat` | `crypto/internal/fips140/nistec/fiat` | 13/14 |
| `internal/concurrent` | `internal/sync` | 3/3 |
| `internal/weak` | `weak` (promoted to public) | 2/2 |
| `runtime/internal/math` | `internal/runtime/math` | 2/2 |
| `runtime/internal/sys` | `internal/runtime/sys` | 8/8 |
| `vendor/golang.org/x/crypto/hkdf` | `crypto/hkdf` | 1/1 |
| `vendor/golang.org/x/crypto/sha3` | `crypto/internal/fips140/sha3` | 5/11 |
| `go/internal/typeparams` | **none — deleted** | — |

⚠ **`vendor/.../sha3` splits.** Its implementation goes to `crypto/internal/fips140/sha3` (5/11)
while the public API surfaces as `crypto/sha3` (1/11). `crypto/internal/mlkem768` splits the same
way (`crypto/internal/fips140/mlkem` 1/2, public `crypto/mlkem` 0/2). A one-to-one table would be
tidier and wrong; both targets are named.

**`go/internal/typeparams` is a genuine deletion**: the directory is gone at 1.24.13, and its single
file `typeparams.go` has no successor package — the only tree-wide match for that filename is
`internal/types/testdata/check/typeparams.go`, an unrelated testdata file. It is **not banked**, so
the deletion costs no verdicts.

## 4. Roster exposure — 10 rows, 2,321 verdicts, and one of them is the cost canary

| removed package | verdicts | disclosed |
|:--|--:|--:|
| `crypto/internal/nistec` | **2,195** | 5 |
| `crypto/internal/edwards25519` | 54 | 1 |
| `internal/concurrent` | 20 | |
| `crypto/internal/edwards25519/field` | 16 | |
| `crypto/internal/bigmod` | 14 | |
| `crypto/internal/mlkem768` | 12 | |
| `internal/weak` | 4 | |
| `runtime/internal/sys` | 4 | |
| `crypto/internal/alias` | 1 | |
| `runtime/internal/math` | 1 | |
| **total** | **2,321** | **6** |

**10 of the 14 removed packages are banked roster rows.** The other four
(`crypto/internal/nistec/fiat`, `go/internal/typeparams`, and both `vendor/golang.org/x/crypto/*`)
are not banked and cost nothing.

⚠ **These verdicts are not LOST, and they are not automatically KEPT.** Every one of the 10 has a
measured successor path, so the tests still exist upstream — but a roster row is keyed on the import
path, and **H10 forbids carry-forward**. Each of the 10 must be **re-derived at its new path** after
the hop; a row merely renamed in place would be an unmeasured claim wearing a banked row's clothes.

⚠ **`crypto/internal/nistec` is the descriptor-synthesis COST CANARY** (recorded baseline 384 s after
memoization, having once blown a 600 s deadline at 354 s). **Its import path changes at the hop.**
Whatever re-banks that row must also re-point the canary, or the next reflect/descriptor arc measures
its wall against a row that no longer exists under the name it is looking for.

## 5. The experiment-gated class, and the axis it is NOT on

```
go1.24.13 default                : 346
go1.24.13 GOEXPERIMENT=synctest  : 347   ->  + testing/synctest
```

**`testing/synctest` is the only package the default `std` pattern hides at 1.24.13.** Its
implementation half, `internal/synctest`, is already in the default set and is counted among the 41
genuinely-new. So the experiment gates the PUBLIC surface only; a conversion run that does not set
`GOEXPERIMENT` will not see it. That is the current default and needs no action, but it should be a
decision rather than an accident.

⚠ **This is PACKAGE MEMBERSHIP, a different axis from R's rehearsal finding, and the two records must
not be read as disagreeing.** R measures three GOEXPERIMENTs that **default ON** at 1.24.13 —
`aliastypeparams`, `swissmap`, `synchashtriemap` — which change **which FILES are selected inside a
package** and drive part of the deletion pass. Measured here, none of them moves membership:

```
GOEXPERIMENT=noaliastypeparams   -> std=346      (default 346)
GOEXPERIMENT=noswissmap          -> std=346      (default 346)
GOEXPERIMENT=nosynchashtriemap   -> std=346      (default 346)
```

`go env GOEXPERIMENT` prints empty at **both** releases, because a default-on experiment is not
listed there. **File selection and package membership are two questions**; this census answers the
second, R's rehearsal answers the first.

## 6. Cross-check against the converter's queue predicate

The predicate is four cases plus a prefix (`isNonConvertedStdLibPackage`,
`stdLibConverter.go:215-222`): `unsafe`, `builtin`, `testing`, `cmd`, and `cmd/`.

```
              std   skipped   QUEUE
  go1.23.12   306      2       304
  go1.24.13   346      2       344
```

**Of the 54 added packages the predicate skips ZERO — all 54 enter the conversion queue.**

⚠ **Two of the predicate's five arms never fire against `std`.** The only packages skipped at either
release are `testing` and `unsafe`; `builtin` and the `cmd`/`cmd/` prefix match nothing, because
`go list std` does not enumerate them. This is not a defect — the arms are cheap and defensive — but
a reader should not infer from the predicate that `cmd/` is being actively excluded from the queue.
It is excluded by the *pattern*, one layer earlier.

**The queue figure 344 now has three independent derivations**: this census (`go list` + the
transcribed predicate), R's rehearsal conversion (`344/344 packages, exit 0, Failed 0`), and R's
earlier `go list std` reading. Three routes, one number.

## 7. GOROOT-axis emission output — EIGHT Δ-alias goldens, not five

⚠ **The number carried into this census was FIVE. Measured under one axis (§8) it is EIGHT.**

| golden | +/− | first seen |
|:--|:--:|:--|
| `FuncForPCName` | 2/2 | carried |
| `FuncLiteralCallerNames` | 3/3 | carried |
| `GoexitDefers` | 2/2 | carried |
| `GoroutineWaitState` | 3/3 | carried |
| `IterPullRendezvous` | 2/2 | carried |
| **`RuntimeCallerFrames`** | **15/15** | **this run** |
| **`SetFinalizerBridge`** | **6/6** | **this run** |
| **`SyscallKeystonePulls`** | **2/2** | **this run** |

**The mechanism claim survives intact; the count and the shape claim do not.**

**Mechanism — CONFIRMED, by a decisive test rather than by inspection.** Normalizing the `-` side of
every hunk (`Δruntime` → `runtime`) reproduces the `+` side **exactly**, across all eight files:

```
minus lines 35   plus lines 35   identical after normalization -> 0 exceptions
```

So **every one of the 35 changed line-pairs is the Δ-alias rename**, and not one is a format string.
The converter mints `Δ` when a package name collides in scope, and §3 names the cause: **1.24 removes
`runtime/internal/sys` and `runtime/internal/math`** (and `internal/weak`), so the colliding set under
`runtime` is different.

⚠ **"2/2 shape" was wrong as a general claim.** Three of the eight are 3/3, 6/6 and 15/15 — the file
with more `runtime.` call sites has more renamed lines. **The invariant is `added == removed`** (a
pure rename that changes no line count), which holds on 8 of 8; `2/2` was the *smallest* instance
mistaken for the rule.

⚠ **Corpus growth does NOT explain the three extra goldens.** They were added
2026-08-07, 2026-08-29 and 2026-09-05 — two of them *older* than members of the carried five. **The
earlier count was simply incomplete**, and §8 says why.

This is **H9 golden-rebank input, not an H1 red.** At the hop all eight are re-baselined from the
rebuilt converter against the new GOROOT, with the drift classified per the runbook's §4. **A rebank
plan sized at five would have left three goldens unaccounted for.**

## 8. The one-axis isolation

The five goldens were first seen in a CNR carrying **three variables** (1.24.13 toolchain, 1.24.13
directive, the printf fix), which measured something other than what it appeared to. They are
attributed here by a run varying **one**.

**Arm A is already on the record**: H1.3's CNR at this same tree — converter built by 1.24.13,
`GOROOT` = the outgoing 1.23.12 root — **byte-identical across all 722 behavioral packages, 0 NOT
MEASURED.**

**Arm B**: the same tree, the same converter, `GOROOT` = the 1.24.13 root.

⚠ **The build axis is proven constant rather than assumed.** Building the converter under each
`GOROOT` setting produced **byte-identical binaries** (sha256 `a8cc7ae315b3eef4`, 19,340,800 bytes,
both stamped `go1.24.13` by `go version <exe>`), and CNR's own unconditional rebuild inside arm B
reproduced that same hash. The `go.mod` directive from H1.2 forces the 1.24.13 toolchain even when
`go build` is invoked through the 1.23.12 binary, which is the same hard cutover H1.5 measured. So
the only thing that differs between the arms is **the GOROOT the converter READS**.

### 8.1 Result, and the prediction scored against it

The prediction was written into the run's own log **before it started**, with its falsifiers:

```
PREDICTION: EXACTLY five goldens change -- FuncForPCName, FuncLiteralCallerNames, GoexitDefers,
            GoroutineWaitState, IterPullRendezvous -- at 2 added / 2 removed each, every hunk the
            Delta-alias family, and NOTHING else moves.
FALSIFIERS: any SIXTH golden changing        -> the 1.24 GOROOT reaches further than the alias family
            any hunk that is not that shape  -> the shape attribution is wrong
            any NOT MEASURED package         -> route #4, a front end that cannot read 1.24 sources
```

**MEASURED:** `EXIT=1`, 566 s, **8 CHANGED**, 0 NOT MEASURED, nothing dirty outside the eight.

| | predicted | measured | |
|:--|:--|:--|:--|
| goldens changed | 5 | **8** | ⚠ **MISS — falsifier 1 fired exactly as written** |
| hunk shape | Δ-alias family | Δ-alias family, 35/35 pairs | HELD |
| per-file line shape | 2/2 each | `added == removed`, 8/8 | ⚠ **MISS — 3/3, 6/6, 15/15 also occur** |
| NOT MEASURED | 0 | 0 | HELD — no route-#4 degradation |

⚠ **Why the count was wrong, stated plainly: I predicted from a number I had myself labelled
untrustworthy.** The five came from a CNR carrying **three** variables — the run whose own record in
this project's mailbox says *"that does not falsify my byte-identical prediction and it does not
confirm it: it measured something else."* I then carried its CHANGED count forward as the prediction
for a one-axis run. **A baseline is measured at the same scope in the same run, never quoted from a
record** — and the record I quoted was one I had already disowned.

**The three extra goldens are not corpus growth** (§7), so the earlier reading was incomplete rather
than merely dated. What the three-variable run reported was a subset, and nothing in it said so.

**What the isolation nonetheless establishes**, which is what it was run for: with the build axis
proven constant by identical binary hashes, **the 1.24 GOROOT alone moves exactly these eight goldens
and nothing else in 722 packages**, every hunk one mechanism, and **zero NOT MEASURED**.

⚠ **What the zero does NOT establish.** Both arms run a converter **built by go1.24.13** — the
control binaries are stamped `go1.24.13` by `go version <exe>`, because H1.2's directive forces the
toolchain switch — so its embedded `go/parser` and `go/types` are 1.24.13's, *matched* to the sources
it is reading. **This says nothing about whether a 1.23.12-built front end could read 1.24 sources**;
that is route #4's question and it is not on this axis. What falsifier 3 was actually guarding is the
weaker and still worthwhile claim that the matched front end does not degrade on the new corpus.

## 9. H4 items routed to this census — four, each sized

Every item below is **re-derived here from the sources**, not transcribed from the post that routed
it.

### 9.1 `internal/weak` — the linkname push registry, 2 rows

`TestLinknamePushRegistryMatchesGoSource` fails under the 1.24 GOROOT on **exactly 2 registry rows in
1 removed package** — `internal/weak`, whose successor is the promoted public `weak` (§3). The
registry names the old import path; at 1.24.13 no package lives there.

**Sized and closed-ended: 2 rows, 1 package.** It is the third commit of `claude/g-hop-h1` per the
standing ruling, and it retires *with* the hop, not before it.

### 9.2 The front-end ceiling — a CONSTRAINT, not a to-do

`x/tools` **v0.42.0** and `x/mod` **v0.33.0** are the ceiling under go1.24.13: every later version
declares `go 1.25.0` in its own published `go.mod` and is refused
(`golang.org/x/tools@v0.49.0 requires go >= 1.25.0`). The hop therefore crosses **six** minors of the
front end, not the thirteen `go list -m -u` appears to offer — that listing reports what EXISTS, not
what is USABLE at the target.

**Nothing to do here. The item is that 1.24.13 CAPS these dependencies**, so any later move to a
current `x/tools` is a **Go 1.25 question** and must not be smuggled into this hop.

### 9.3 `runtime/lock_spinbit.cs` — new file, 3 of 5 residual errors

```
runtime/lock_spinbit.go   1.23.12: absent      1.24.13: PRESENT
```

New at 1.24.13, so no hand-own and no prior emission exist for it. R's rehearsal attributes **three
of the five residual `runtime` errors** to this file, behind the two frozen hand-owns of §9.4. Sizing
is owed once §9.4 unblocks the package — a leaf gates the tree, so the residual five are measured
*after* the blocker moves, not beside it.

### 9.4 The `note` relocation — the first named H6 RE-WRITE

```
type note struct   1.23.12 -> runtime2.go        1.24.13 -> note_other.go  (//go:build !js)
note_other.go      1.23.12 -> absent             1.24.13 -> PRESENT
```

Go **relocated** `note` out of `runtime2.go`. The converter behaves correctly throughout: it emits
the new `note_other.cs` and protects the marked hand-own — and the hand-own `src/core/runtime/runtime2.cs`
(anchored marker count **1**) still declares `struct note`. Both declarations then compile, which is
CS0102 in the leaf almost everything depends on.

⚠ **The two frozen files are `runtime2.cs` and `mfinal.cs`, and they are H6 population failures**, not
converter defects: both carry the marker at master and neither appears in `CENSUS-h6-handown-go124.md`
(population 149). `mfinal.cs` is the .NET finalizer bridge, so its reconciliation is a **design item**
rather than a mechanical re-derivation.

**Scope note, so this is not read as a correction to R:** at 1.24.13 `note` is declared in *two*
build-tagged files, `note_js.go` and `note_other.go`. For the three targets we emit
(windows/linux/darwin) the selected file is `note_other.go`, exactly as the rehearsal record states;
`note_js.go` is js-only and out of scope.

## 10. Reconciliation against the prior H3 census in `RECON-go1.24-hop.md`

That record's §2 is an earlier H3 package census and its §7 an earlier roster-exposure bill. **This
census is not the first reading of either, and it is reconciled against them here rather than
published beside them.**

### 10.1 The package counts differ by exactly ONE, on every reading — and the axis is `CGO_ENABLED`

```
              RECON section 2 (CGO_ENABLED=1)     THIS census (CGO_ENABLED=0)
  windows          307 -> 347                          306 -> 346
  linux            305 -> 345                          304 -> 344
  darwin           306 -> 346                          305 -> 345
```

**A uniform −1. Isolated by varying one thing at a time:**

```
CGO=0  tags=purego,math_big_pure_go : 346      <- this census
CGO=1  tags=purego,math_big_pure_go : 347
CGO=0  no tags                      : 346
CGO=1  no tags                      : 347      <- the recon's conditions

the package: runtime/cgo
```

**The purego tags make no difference to package MEMBERSHIP** (they decide file selection *within* a
package); the whole −1 is `runtime/cgo`, and the deciding variable is the cgo state.

⚠ **The corpus-correct condition is `CGO_ENABLED=0`,** which this file's own doctrine rules as the
state of record on every platform, and which the corpus is emitted under. **So the recon's §2 counts
are one package high relative to what the converter actually queues**, and its 347/345/346 should be
read as 346/344/345 for queue purposes. That is a dated amendment owed on that record, not a defect
in it — its `CGO_ENABLED=1` was stated openly at the top of the section.

`runtime/cgo` is present at **both** releases under `CGO=1`, so it cancels in the diff: **both
censuses independently agree on 54 added, 14 removed, +40 net.**

### 10.2 The roster bill agrees EXACTLY, by two unrelated instruments

The recon's §7 reports **10 rows / 2,321 verdicts**, with the same ten packages and the same
per-row counts as §4 above (`crypto/internal/nistec` 2,195/5 included). That record derived it by
**hashing each row's `_test.go` set in both releases**; this one derived it from `go list` plus a
parsed roster. **Two instruments with nothing in common, one answer** — which is also what retires
the broken first-pass predicate of §9 rather than merely replacing it.

⚠ One refinement, in the sharper direction: the recon says *"nine of the ten have an obvious
successor path"*. **The file-overlap derivation of §3 finds a successor for all ten** — the weakest
matches being `crypto/internal/alias` (1/2) and `crypto/internal/mlkem768` (1/2), both named with
their denominators so a reader can judge them rather than take the word. The single removal with no
successor is `go/internal/typeparams`, which is **not banked** and therefore not one of the ten.

Row-count note: the recon parses **201** roster rows and this census **204**, which is roster growth
between the two dates, not a disagreement.

### 10.3 One count in the prior record does not reproduce

The recon's §2 states *"38 of the 54 additions are `crypto/internal/fips140*`"*. Enumerated here
(members printed beside the count, §2):

```
'fips140' matched anywhere              : 36
the recon's own prefix crypto/internal/fips140* : 34
```

**Neither is 38.** The shape claim it supports — *one reorganization wearing an add-and-remove
costume* — is unaffected and correct; only the figure is. Recorded here so the next reader who
counts gets the same answer twice, rather than a third number.

## 11. Instrument notes — what was controlled, and TWO instruments that were wrong

- ⚠ **The roster predicate was broken on its first run, and a control caught it before it reached a
  post.** A per-package `grep -cE ... || echo 0` reported **every** package as banked, including a
  planted `nosuchpkg/zzz`: `grep -c` prints `0` *and* exits 1, so the `|| echo 0` fires as well and
  the value becomes two lines, which is not equal to `"0"`. The reading it produced — *"all 14 removed
  packages are banked"* — was an instrument artifact, and the corrected figure is **10 of 14**. The
  replacement parses the roster **once** into a `pkg → tests, disclosed` map with no per-package
  regex, and is controlled five ways: two known-banked packages return their exact recorded values
  (`unicode/utf8` 14; `crypto/internal/nistec` 2195/5) and three known-absent ones
  (`crypto/internal/fips140`, `weak`, `nosuchpkg/zzz`) return not-banked.
- ⚠ **A glyph count "found" 27 non-alias lines that do not exist.** Checking whether every hunk was
  the alias family, I counted changed lines matching `Δruntime` and subtracted: 70 total, 43
  matching, **27 apparently unexplained**. The number is an artifact of the predicate — **a renamed
  line's `+` side no longer contains the glyph it was renamed away from**, so counting the glyph
  under-counts by exactly one side of every pair. The correct test is not a count at all but a
  *transformation*: normalize the `-` side by the rename and require it to reproduce the `+` side,
  which it does, 35 for 35, with zero exceptions. **A census of a rename must apply the rename.**
- **The added and removed sets were compared across all three targets** and are identical, so no
  per-target difference is hidden inside a windows-only reading.
- **Successors are derived, not assumed** — file-name overlap against every added package, with the
  best score reported *and* its denominator, so a weak match (`alias` 1/2, `mlkem` 1/2) is visible as
  a weak match rather than presented as a clean rename.
- **204 roster rows** parsed at this corpus.

## 12. What this census does NOT answer

- **It does not measure whether the 54 new packages CONVERT, or whether the corpus BUILDS.** That is
  R's `REHEARSAL-h5-go124.md` — which finds the conversion clean at 344/344 and the build gated by
  one package — and this document defers to it on both.
- **It does not enumerate the hop's DELETION pass.** R's rehearsal measures 25 would-be deletions;
  a seeded reconvert cannot reveal a file the converter has stopped emitting, and that is a distinct
  instrument from `go list`.
- **It says nothing about verdicts at the new paths.** Whether the 10 relocated banked rows still
  pass under their successors is an H10 question, and the answer is owed by re-derivation.
