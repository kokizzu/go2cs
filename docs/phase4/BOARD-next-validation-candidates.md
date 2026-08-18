<!-- {% raw %} — Jekyll/Liquid guard: this doc quotes Go composite-literal syntax ({{ … }}) that Liquid would otherwise parse; the HTML comment hides the tag on GitHub. -->
# BOARD — next validation candidates, each rooted

> Measured 2026-07-27 by running the `-tests` pipeline over every unbanked candidate the
> shared-fixture fix structurally unblocked, plus the packages a prior scout left build-blocked.
> Every entry carries the **first and most informative diagnostic**, so the next arc starts from a
> root cause rather than an exploration. **Revised 2026-07-27 (later)** by the reference-closure
> arc: the closure family is closed, `internal/zstd` is banked, and two claims in the original
> revision are retracted as measurement errors — see the sections below. Corpus state after that
> arc, plus the 2026-07-29 `hash/maphash` bank and the 2026-07-31 `image/draw`, `image/gif`,
> `crypto/md5`, `compress/flate`, `image/jpeg`, `image/png` and `index/suffixarray` banks:
> **69 validated / 215 (32.1%)**.
>
> **Revised again 2026-07-31** by the build-blocker arc: `path/filepath` and `net` — the last two
> unrooted build blockers — are both fixed at the converter, and both rows moved down into their own
> sections with what stood behind them. Neither package banks, and the roster is unchanged at 66; a
> build blocker closing is worth recording precisely because the *next* wall is now measurable.
>
> **Revised again 2026-07-31 (r28-net)**: six of the seven semantic roots the previous revision
> bucketed for `net` are fixed — `net` is down to **2 errors from one root**, and that root is a
> ruling (`testing.T.Deadline` needs a type the one-testing-package cannot name), not a defect. See
> the `net` section. The roster is **unchanged at 69** — the "66" in the paragraph above was already
> stale when it was written, and no package banks from this arc.
>
> **Revised again 2026-08-02 (r35-context)**: `context` gets its own section below — five converter
> roots closed, `T.Deadline` un-blocked, **36 of 38 verdicts match**, and two rooted failures left, one
> owned by the reflection-bridge arc and one a measured disclosure. The roster is **unchanged at 71**:
> nothing banks from that arc. Its most valuable measurement is a negative — the sharpest
> select/cancellation suite in the standard library finds **no channel defect at all**, which is
> independent confirmation of the wave3 landing.
>
> **Revised again 2026-08-02 (r35-os)**: `os` gets its own section below — it builds with 0 errors and
> reaches **158 of 178 top-level tests matching + 1 disclosed**, up from 48 at the start of the arc.
> Two converter roots and one host-killer closed; the residual is rooted row by row, and the largest
> single item (12 unreached) is heap corruption whose crash SITE moves between runs, not a defect at
> any of the three sites it has been credited to. `os` does not bank and the roster is unchanged at 71.
>
> **Revised again 2026-08-02 (r37-gob)**: `encoding/gob` gets its own section below — the build blocker
> that made all 106 of its verdicts read empty is closed at the test-project-model record-anchoring root,
> and gob is measured for the first time: **86 of 106 match**, every mismatch bucketed to one of seven
> roots. A second converter defect found through it — a dead deref alias that took down `unique`'s and
> `net/netip`'s package initializers corpus-wide — is fixed in the same arc, though it moves `TestNetIP`'s
> site rather than greening it. The roster is **unchanged at 71**: gob does not bank.
>
> **Revised again 2026-08-03 (r38-gob-fin)**: gob moves **86 → 88 of 106** on a converter fix, `unique`
> BUILDS for the first time and gets its first census, and four of r37's seven gob roots turn out to be
> mis-attributed — see the rewritten gob section. The roster is **still 71**: neither package banks.
>
> **Revised again 2026-08-03 (r39-nilcomplex)**: both converter items r38 handed on ROOTED — the
> typed-nil BOUNDARY and the `complex()` element-width pin — land, and gob moves **88 → 91 of 106**.
> Two of r38's seven roots close outright; the third typed-nil row does NOT, and its residual is now
> rooted one layer down in the reflection bridge (see *r39-nilcomplex* at the end of the gob section).
> The roster is **still 71/72**: gob does not bank.
>
> **Revised again 2026-08-07 (r41c-cloneseq)**: `unique`'s `makeCloneSeq` root closes and the package
> moves **1 → 4 of 19**, on an `internal/abi` hand-own plus a converter fix — but the row's own
> DESCRIPTION below was wrong in both halves (it is not a `slice<T>` enumerator edge, and it is
> reflection-bridge territory), so read *The `makeCloneSeq` root, CLOSED* rather than the table cell.
> The roster is **unchanged at 73**: `unique` does not bank.
>
> **Revised again 2026-08-07 (r43c-candidates)**: the first pure **measure-first breadth pass** — 47
> never-measured candidates run back to back through the pipeline. **Twenty-three validated on the
> first run with no converter change of any kind**, taking the roster **73 → 96 (44.7%)**; every one
> of the twenty-four that did not is rooted in the new section at the end of this file. The finding
> worth carrying forward is the negative one: the corpus had already grown past those packages and
> nothing was watching, so **the roster's denominator is limited by who has looked, not by what is
> broken**. Re-scout the tail after any capability lands, not only the packages that capability names.
>
> **Revised again 2026-08-07 (r44a-rescout)**: r43c's own instruction executed — 108 pipeline runs
> over BOTH r43c's rooted non-validators and the 76 never-measured tail packages. **Twelve bank**,
> taking the roster **97 → 109 (50.7%)** and past the campaign's 50 % mark; eleven needed nothing,
> and the twelfth (`internal/cpu`) took a one-declaration hand-own. The re-scout of r43c's OWN roots
> yielded exactly one package (`expvar`) and every other rooting re-measured verbatim, which sharpens
> the instruction rather than repealing it: **a rooted non-validator has been looked at; the yield is
> in what nobody has run.** Eighteen packages are now ONE OR TWO ROWS from banking, and the tail's
> build blockers are named with their verdict counts — see the r44a section at the end of this file.
>
> A note the arc earned: a **first diagnostic is a starting point, not a diagnosis**. `io`'s first
> error is CS0012 and reads as a missing reference; it is not one. Two of the three claims below
> that were stated as "measured" did not survive re-measurement on a freshly built converter.
> r41c is the same lesson at one more remove: the exception TYPE and the frame it is thrown in
> (`IndexOutOfRangeException` in `go.slice<T>.Enumerator`) named a component that had nothing wrong
> with it, because a garbage slice HEADER two frames up makes a correct enumerator throw.
>
> Re-validate everything after any change here with `./src/run-validated-sweep.ps1` — it reads the
> roster and the expected counts from [`ValidatedTestPackages.md`](../ValidatedTestPackages.md) and
> fails on a count mismatch, so a package that still passes but asserts something different is
> caught rather than assumed.

## ✅ BANKED (2026-08-17, lane `claude/connraw-hang`) — `database/sql` validates 137 of 139 as roster row **148**; `TestConnRaw` was never a lock, and the guilty layer was `GoFrame.Run`

**The hypothesis this lane was handed was wrong in every particular, and one `dotnet-stack` report
against the hung host said so before any code was read** — which is the sixth consecutive brief this
week to name the wrong layer, and the reason the instruction to measure first is worth its cost.

The brief's leading suspicion was a deadlock or lost wakeup in golib's hand-owned `sync.RWMutex`
shim, from `release(ErrBadConn)` → `closemuRUnlockCondReleaseConn` → `Conn.close` taking
`closemu.Lock()` against the test's own recover handler. **No thread was blocked on `closemu`, or on
any lock.** The only test thread was in `time.Sleep`, inside `waitCondition`, inside `closeDB` — the
package's ordinary teardown poll. `sync`'s shim is innocent and unchanged; so is the `Conn.Raw`
emission the previous entry had already cleared.

### The hang was never a hang — it is `waitCondition` sized by `t.Deadline()`

`closeDB` ends with `waitCondition(t, func() bool { return db.numOpenConns() == 0 })`, and
`waitCondition` does not poll for five seconds when the test has a deadline: it polls for
`time.Until(deadline)` minus 10 % headroom. So an assertion failure that leaves ONE connection open
does not fail fast — it consumes 90 % of whatever `-test-timeout` was passed, then reports. That is
the whole of the 3,418 s: at `-test-timeout 60m`, 0.9 × 60 min. The two previous entries' 1,712 s and
3,423 s package figures are the same arithmetic at their own deadlines, not machine load and not lane
contention.

**Consequence for the board's reading habits:** a Phase-4 row whose C# elapsed lands suspiciously
near `0.9 × -test-timeout` is a *failing assertion inside a deadline-sized wait*, not a deadlock. Run
it again with a small `-test-timeout` (20 s was enough here) and it prints its real failure in
seconds. Do not reach for `dotnet-stack` first — but if you do, it answers immediately too.

### The root: `GoFrame.Run` re-raised a panic the frame never caught

Probes on the converted `sql.cs` caught the panic being re-thrown from a frame that had nothing to do
with it. `Conn.Raw`'s deferred cleanup reaches `Conn.close`, which sets `c.dc = nil` only AFTER
`dc.releaseConn` → `db.putConn` → `dc.Close` → `finalClose` → `withLock`. `withLock` is two lines, it
holds one `defer`, and it panics nothing — but its `finally`'s `ᒐ.Run()` ended with

```csharp
if (GoFuncRoot.CapturedPanicValue is not null)
    throw GoFuncRoot.CapturedPanicValue;
```

and that slot is the THREAD's, non-empty for the whole of the panicking frame's deferred sequence. So
`withLock` threw `Conn.Raw`'s panic on the way out, `close` never reached `c.dc = nil`, `conn.dc`
stayed non-nil (the failure the previous entry recorded), the later deferred `conn.Close()` found
`done` already true and returned `ErrConnDone` without releasing (the "1 connections still open"),
and `waitCondition` sat on it for the rest of the deadline. **One root, both recorded symptoms, and
the hang.**

The rule is now stated instead of inferred: the re-raise belongs to the frame whose own `catch`
caught the panic. `GoFrame.Capture` arms a claim, the next `Run` on the thread claims it — always
that same frame's, because nothing runs between an emitted catch body and its `finally` — and a frame
that caught nothing claims null. **The emission is untouched**; the change is 33 lines inside
`golib/GoFrame.cs` + `GoFuncRoot.cs`, and no converted file moved.

This is a general class, not a `database/sql` fix: EVERY converted function with a `defer` was a
spurious re-raise site whenever it was called during another frame's deferred sequence. Cleanup paths
that call helpers are the common shape, so expect other Phase-4 rows in the "cleanup didn't finish"
family to move without being touched.

Doctrine: `ConversionStrategies-Reference.md` — *The re-raise of an unrecovered panic belongs to the
frame that CAUGHT it, not to the thread*, which also records the one adjacent hole left deliberately
open (a nested frame's `recover()` clears the outer frame's parked panic — same shared slot, predates
this change, no measured consumer).

### `database/sql` — 137 of 139, and the owed `$longTimeouts` floor is repriced away

139 rows, **137 agree, 2 disclosed, 0 skipped**, 27 excluded (the standard `Benchmark`/`Example`
deferrals). The two are `TestGrabConnAllocs` and `TestRawBytesAllocs`, the standing **`alloc-profile`**
class, pinned by signature in a hand-owned `go2cs_test_disclosures.json`.

**No `$longTimeouts` entry is owed, and the two previous entries' "`'database/sql' = '60m'` if it ever
banks" is formally repriced to zero.** The converted suite now runs in **3.5 s** (Go: 46 s); the whole
`-test-action all` round trip, both builds included, is 33 s. The sweep's default deadline clears it
by three orders of magnitude.

### Guards, and the corpus footprint

- **`PanicDeferCalleeFrame`** (new behavioral test) — output-compared against `go run`: the reduced
  `Conn.Raw` shape (acquire → panic in the callback → cleanup that must still reach `open = false`),
  three deferring callees stacked below one deferred call, and the two negatives (a panic raised by a
  frame's own deferred call still escapes it; one raised inside a deferred cleanup still replaces the
  panic already unwinding). Neuter-verified: reverted, the run diverges from Go.
- **`GolibTests.GoFrameTests`** — four rows pinning the rule at the frame itself, including the
  `m_count == 0` path that skips the deferred sequence and reaches the tail directly. Neuter-verified:
  the two that pin the fix fail with the exact truncated-cleanup collection; the other two are the
  over-suppression negatives and pass either way, by design.
- Corpus footprint: **zero regenerated files**. `database/sql`'s `sql.cs` and `database.sql.csproj`
  came back byte-identical from a seeded scoped reconvert, whose only diff was the README's Tests
  badge (`137/139`).

## ✅ CLOSED (2026-08-16, lane `claude/assignableto-arc`) — the `AssignableTo` deferral is retired, with the struct/func/interface identity arms and the chan direction fixed in the same change

The reflect-bridge lane recorded the retirement SEQUENCE rather than the fix, and the sequence was
right in every particular: `HasName()` had to become truthful first, and the struct and chan arms of
`haveIdenticalUnderlyingType` had to be fixed *with* the hand-own's retirement rather than after it.
Following it turned up one arm the sequence did not name — **func** — and one it did name as
undecidable turned out to be decidable after all, once the question was asked about the right object.

`database/sql` moves **135 → 136 of 139**. It does **not** bank: `TestConnRaw` is still open, and
this lane measured something about it that changes what "open" costs (below).

### What each of the four pieces actually was

| Piece | Before | Now |
|:--|:--|:--|
| `abi.Type.HasName()` | `false` for EVERY synthesized descriptor — `TFlagNamed` was never carried | `synthesizeDescriptor` stamps the bit from `GoReflect.HasGoName`, the SAME gate `reflect`'s own `rtype.Name()` already stood on |
| `implements` (the free function) | reinterprets the descriptor as an `interfaceType` and reads `.Methods` off a default promoted-embed box — **throws** for any non-empty interface | bridged over `GoReflect.GoImplements`, the probe `rtype.Implements` and the emitted `_<T>` asserts already share |
| `haveIdenticalUnderlyingType` struct / func / interface arms | prefix-downcast; read ZERO fields / ZERO in-out counts / ZERO methods and returned **true** | Go's own clauses over `GoReflect.GoFields` + `TryFuncShape`, at the `reflect` level |
| `ChanDir()` (both `abi.Type` and `rtype`) | reads a direction out of the memory FOLLOWING the descriptor's value slot — non-deterministically | `BothDir`, the real direction of the only channel type the bridge can describe |
| `rtype.AssignableTo` | hand-owned as identity-on-the-managed-type + implements | **RETIRED** — Go's `directlyAssignable(uu.t, t.t) \|\| implements(uu.t, t.t)` runs |

**The struct arm was worse than recorded, and the recorded shape understated it.** The board said "any
two structs of EQUAL FIELD COUNT compare identical". Measured: the count check itself reads
`len(Fields)` off the same dead downcast, so it is `0 == 0` for both operands and even a **differing
field COUNT** compares identical. `struct{B []byte; M map[string]int}` was reported convertible to the
same struct with `M map[string]int64`, to one whose second field is merely RENAMED, and to
`struct{B []byte}`.

**The FUNC arm is the one the sequence did not name, and it had to be fixed here.** It fails the same
way through `InCount`/`OutCount`, so any two func types compared identical. It was already live
through `ConvertibleTo`, and it would have widened to assignment the moment `AssignableTo` started
routing through the walk — the exact trade the recorded sequence exists to prevent. It is answered
from `GoReflect.TryFuncShape`, the same shape `rtype.NumIn`/`In`/`NumOut`/`Out` read.

### The CHAN ruling — decidable, because the question was being asked about the wrong object

The scout recorded `ChanDir` as "the only one of the family with **no synthesis waiting for it**:
`<-chan int` and `chan int` are both `channel<nint>`, so a direction is not recoverable from the
managed type for an unnamed directional channel. This one needs a ruling, not a fix." Both halves of
that are true, and the conclusion still does not follow — **because a directional channel type is
never an OPERAND here.** The bridge cannot build a descriptor for `<-chan int` at all; it builds one
for `channel<T>`, whose direction genuinely is `BothDir`, and whose `Type.String()` has said `chan T`
since the beginning. Answering `BothDir` is therefore not a guess about something unknown — it is the
correct answer about the descriptor actually being asked, and it makes the descriptor's kind, name and
direction agree where the downcast made one of the three disagree at random.

The limit is real but it lives **one layer up**, in the converter's channel emission: `reflect.TypeOf`
over a `<-chan int` reports `chan int`. That is stated in
`ConversionStrategies-Reference.md` rather than hidden, and it is **not disclosable and needs no
gate**, for a specific reason worth recording: no package on the validated roster observes it. The
one corpus consumer that branches on direction is `text/template`'s `walkRange` (rejecting a range
over a send-only channel), and `text/template` is not on the roster — only `text/template/parse` is.
Recovering the direction would mean carrying it as descriptor cargo the way array dims are carried,
which no measured consumer asks for (the r39d rule). **If `text/template` is ever taken up, this is
the row to expect**, and the remedy is cargo, not a disclosure.

Two residuals stated the same way: the **interface** arm proves "methodless" only for `object` (Go's
`any`), so a defined empty interface with a managed type of its own answers *not identical* — the
conservative direction, since a false negative degrades a caller to "needs a conversion" while a false
positive is a silent wrong assignment; and a **defined methodless func type has no managed identity at
all** (the converter renders it inline as its base delegate), so the named/unnamed pairs every other
kind asserts cannot be produced for funcs.

### `database/sql` — 136 of 139, and `TestConnRaw` is a HANG, which reprices the owed floor

`TestUserDefinedBytes` flips exactly as predicted, and `TestConversions` stays passed. 139 rows, **136
agree, 3 disagree, 0 skipped, 0 disclosed**, 27 excluded (the standard `Benchmark`/`Example`
deferrals). The three: `TestGrabConnAllocs` and `TestRawBytesAllocs`, the standing **`alloc-profile`**
class, and `TestConnRaw`.

⚠ **The brief for this lane predicted 137 of 139; the arithmetic was one high** (135 + the one flip =
136). Recording it so the next planner starts from the measured number.

**The new measurement.** `TestConnRaw` does not merely assert wrong — it **hangs**, and it is the
entire runtime of the package:

| | C# | Go |
|:--|--:|--:|
| whole-package terminal elapsed | 3,423.3 s | 46 s |
| `TestConnRaw` alone | **3,418.2 s** | 0.005 s |
| every other test COMBINED | **5.1 s** | — |

So the `'database/sql' = '60m'` `$longTimeouts` floor two previous entries record as owed is **not
"this package is slow"** — it is this one test. Fix `TestConnRaw` and `database/sql` runs in about
five seconds and needs **no floor entry at all**. Nothing is added to `$longTimeouts` today (the
package still has no roster row, and a floor for a package the sweep never visits is dead
configuration) — but the entry that banks it should re-measure before assuming it owes one. It also
retro-explains the previous two lanes' 1,712 s and 3,423 s figures, which were read as machine load
and lane contention: both were this test blocking until whatever bound applied.

**Where the root is NOT.** The converted `Conn.Raw` emission is structurally correct: `fPanic` and the
named result `err` are captured by REFERENCE while `dc`/`release` are snapshotted, the deferred lambda
runs from `finally { ᒐ.Run(); }`, and `ᒐdone: return err` sits after the try/finally so the deferred
write to `err` is observed. The suspect path is what the defer CALLS —
`release(driver.ErrBadConn)` → `closemuRUnlockCondReleaseConn` (`closemu.RUnlock()`, then
`errors.Is`, then `c.close(err)`) → `Conn.close`, which takes `closemu.Lock()`, a WRITE lock, before
setting `c.dc = nil`, while the test's own recover handler independently takes `conn.closemu.Lock()`.
A deadlock or lost wakeup in the hand-owned `sync.RWMutex` shim under that ordering is the leading
hypothesis; `errors.Is` answering false for an identity comparison is the cheap one to falsify first.
Not chased further here — it is a `sync`/defer-during-panic question, not an assignability one.

### Guards, and the corpus footprint

- **`ReflectConvertAssignable`** (extended, 14 → 34 compared rows) — Go's assignability rule clause by
  clause: both gates of the unnamed↔named rule INCLUDING the two-defined-types negative that
  `HasName()` alone decides, the interface clause both ways, the struct arm against a differing field
  type / a renamed field / a differing field count / a tag that conversion ignores and assignment
  honors, the func arm's parameter and result discrimination, and the chan rows the bridge can
  truthfully produce. **Measured failing-first**: the struct and func arms answered `true` where Go
  answers `false`, and the `assign` rows answered `false` where Go answers `true`.
- **`GolibTests.GoStructLayoutTests.EmbeddedField_IsDistinguishableFromADeclaredFieldOfTheSameNameAndType`**
  — pins the new `GoFieldInfo.Embedded` projection the struct arm stands on; `struct{T}` and
  `struct{T T}` agree on field count, name, type, tag and offset and are separated by nothing else.
  Neuter-verified (removing the flag fails it with the exact assertion).
- Corpus footprint: **two regenerated files**, `internal/abi/type.cs` and `reflect/type.cs` (four Go
  bodies replaced by placeholders, one placeholder replaced by Go's own restored `AssignableTo`).

Doctrine: `ConversionStrategies-Reference.md` — *Go's ASSIGNABILITY rule, and the identity walk
underneath it* (new), which also CLOSES the follow-on recorded at the end of *`reflect.Type.Name()` —
a DEFINED type HAS a name even when its underlying type is a composite*: `reflectlite.rtype.Name()`
becomes truthful for free, since it gates on the bit this lane started carrying.

## OPEN — `-recurse` emission is covered by NO standing gate, and issue #35 proves what that costs (2026-08-08)

**Every standing gate measures the behavioral corpus or the standard library. Neither can see a
`-recurse`-only defect, so an end-user conversion is guarded solely by hand-authored synthetic fixtures —
one per past issue.** Issue #35 is the demonstration: a truncated project name put **175 duplicate
`.csproj` names** into a user's 1,727-project solution (Visual Studio then refuses to open it and says
nothing), and *every gate stayed green through it*. `check-no-regression` reported byte-identical C# and
`.csproj` across all 574 behavioral packages, and `-stdlib` cannot reach the code at all — it returns on
the GOROOT branch before the module walk. See [`ConversionStrategies-Reference.md`](../ConversionStrategies-Reference.md),
*A project name is the package's FULL import path*.

That is structural, not bad luck. `-recurse` is one of the two end-user use cases
([`DESIGN-recursive-enduser-conversion.md`](../phase3/DESIGN-recursive-enduser-conversion.md)), and
the only thing exercising it is the **nine** fixtures in `moduleConverter_integration_test.go` — three that
cover a mode (`TestRecurseSyntheticModule`, `…NuGetReferences`, `…ModuleOnly`) and **six written after the
defect they cover** (`TestModuleCachePoisonedGoWorkLoad`, `TestModuleCacheVestigialReplaceLoad`,
`TestRecurseQuotedModulePath`, `…KeywordNamespaceSegment`, `…ChannelOfHyphenatedModulePath`,
`…GoFileFreeContainerDirsKeepDistinctProjectNames`). That is precisely the "enumerate the shapes we have
SEEN rather than state the property we need" tell this file already names as the recurring signature of a
point repair: the six shapes are the ones issues #32, #33 (×3) and #35 happened to hit.

**Two increments, the first nearly free:**

1. **State the property.** After any `-recurse` run the emitted project names must be *distinct* and each
   must equal its package's import path, dotted. That is one assertion over `convertedCsproj`, it closes
   the whole class rather than one shape, and it costs nothing to add to the existing fixtures. (Deliberately
   as a TEST assertion, not a converter runtime check — post-fix the name IS the import path by
   construction, so a runtime guard would be machinery for an unreachable state. The value is in pinning the
   invariant, which is a test's job.)
2. **One adversarial fixture instead of seven incidental ones.** A checked-in, network-free module whose
   layout is the *union* of every shape that has bitten: go-file-free container directories, `internal/`, a
   `/vN` submodule, a quoted `module` directive, a C#-keyword path element, a hyphenated path, same-named
   leaf packages, a co-located `replace`, a `go.work`. Convert it and golden-compare the emitted
   `.csproj`/`.slnx`/`.cs` the way the behavioral corpus is compared — which gives `-recurse` the drift
   detection it has never had, and makes the next shape a few lines of fixture rather than a new test.

Worth doing before the next end-user report rather than after it: the class has now produced four issues
(#33 ×2, #35, and #32's loader shape), and each arrived from a user rather than from a gate. The
derivation's own recurring-defect row is in the *Recurring classes* section below.

## CLOSED — the ARGUMENT-path exponential is fixed, and the corpus paid its 29-file debt in the same change (2026-08-07, r43a-argexp)

**Same bug class as the chained-call exponential closed directly below, one code path over, and closed the
same way: stop paying for a traversal whose answer the type system already holds. Nesting depth 22 went
from 13.7s to 0.54s, and the whole 302-package standard library still compiles.**

**What it was.** After rendering a call, `convCallExpr` re-walked every argument for its recording side
effects — the loop at the end of `convCallExpr`, whose own comment said it "re-converts each arg purely for
its side-effects (recording implicit conversions); the result is discarded" — and
`checkForImplicitConversion` opened with a full `expr := v.convExpr(arg, nil)`. So every argument subtree
was converted **twice**: once by `convExprList` for the emitted text, once again here for the recording. On
NESTED calls — `f(f(f(…)))`, where each argument IS the next call — that compounds to **2^depth**.

**Fix** ([`convCallExpr.go`](../../src/go2cs/convCallExpr.go)), exactly the split the rooting designed,
because the premise held on inspection: `expr` is pure text that flows only to the return value (two
pointer cases wrap it), while every recording decision comes from `funcType`, `argType`,
`targetTypeName`/`argTypeName` and `packageTypeSpecRHS`.

- `applyImplicitConversion(funcType, arg, targetTypeName, expr)` — the recording half, type-driven, takes
  the rendered text as a parameter instead of producing it;
- `checkForImplicitConversion` = `convExpr` + that, unchanged for its one caller that USES the return (the
  explicit type-conversion branch);
- the discard-the-result loop calls `applyImplicitConversion` directly with `""` and converts nothing.

Removing the traversal also retires the `hoistedDecls` save/restore that bracketed the loop: its only job
was to stop a func-literal argument's capture decls being written into the hoist buffer a second time by
the very conversion that is now gone.

**Measured A/B on the DESKTOP** (Windows, this repo's box — the rooting's table was a laptop, so both
columns are re-measured here). Paired runs, same seeded scratch module, single-package conversion, best of
two:

| argument nesting depth | before | after |
|---|---|---|
| 10 | 0.56s | 0.53s |
| 14 | 0.59s | 0.55s |
| 18 | 1.22s | 0.56s |
| 22 | **13.66s** | **0.54s** |
| 26 | **killed at 416s, unfinished** | 0.54s |
| 30 | (not attempted — extrapolates past half an hour) | 0.55s |

After is FLAT at the ~0.55s `go/packages` load floor through depth 30, i.e. the conversion component is
gone, not merely reduced. Before, subtracting that floor leaves a conversion component that doubles per
level: 0.67s at depth 18 → 13.11s at depth 22, a factor of 19.6 over four levels ≈ **2.1× per level**. The
excess over a clean 2× is GC of what the doubled traversal allocates, and it compounds — which is what put
depth 26 past 416s of wall at ~1.9 cores without finishing.

**Full-stdlib conversion wall.** `go2cs -stdlib -comments` over all 302 packages: **378.9s before →
221.2s after**. Read that as directional only — the two runs saw different sibling-lane load on a shared
box (the same before-converter measured 251.2s on an earlier, quieter run), and CLAUDE.md's own baseline
for this command is ~195–225s, which the after run sits inside. The honest claim is that the argument tax
is real but small against `go/packages` load time on ordinary code; the fixture is where it is dramatic.

**Gates — all green, and the arc is NOT emission-neutral in the way that mattered.**

1. **CNR: 4 of 571 changed** (550s) — `FuncLitCaptureInCondition`, `NilPointerPanic`,
   `NilPointerParamMethods`, `NilReceiverMethods`, all `main.cs`, all a pure capture-variable
   RENUMBERING (`lookupʗ3/5/7` → `lookupʗ2/3/4`), declaration and every use renamed together. Same
   mechanism the sibling arc hit: the discarded conversion had been consuming values from
   `getCapturedVarName`'s monotonic per-prefix counter, so removing it closes the gaps. Verified
   collision-free (every generated name declared exactly once per file) and then verified where it
   counts — all four **Compile pass** and **Output pass** against `go run` — before re-baselining.
   Goldens updated with the runner's `--update-targets`; only those four `.cs.target` moved, no
   test-method churn.
2. **Full behavioral suite: 546/546 Transpile, 546/546 Target, C# Compile 0 failed** — but its **Output
   phase was never reached**, and that is a machine story, not a result. Three consecutive full runs were
   killed externally, each truncated mid-run with no diagnostic (the signature CLAUDE.md documents for a
   sibling lane's name-matched cleanup); one of them also hit `CS8104`/`CS0016` "not enough space on the
   disk" in the one-shot batch while C: sat at 2.67 GB free, which the runner's own per-project
   re-attribution then cleared to 0 failed. What the completed phases DO establish is the part that
   matters here: all 546 goldens byte-match, so emission is stable across two independent full
   re-transpiles (CNR's and the suite's).
   **Output is covered where it can differ**, by filtered runs that completed: the 27 projects spanning
   all four changed goldens — `--filter Nil` (25) and `--filter FuncLitCapture` (2) — pass all four
   phases, **25 Output-compared against `go run`, 0 failed**. For the other 542 the generated C# is
   byte-identical to HEAD, and byte-identical generated C# ⟹ identical compile+run ⟹ identical results —
   the same reasoning that makes CNR the authoritative drift instrument.
3. **`go test ./...`: ok**, exit 0 (44.9s), including the new guard and the projitems gate.
4. **Full `go2cs-stdlib.slnx` build: 0 errors** (302 projects, 199s) on the overlaid corpus.

**The one thing the rooting said to prove, PROVEN — and CNR alone could not have proven it.** The recorded
SET is identical: a paired seeded full-stdlib A/B (both roots seeded per the measurement-loop rules, single
run each, seed gate clean at 39 marked files) puts **8,356 of 8,386 files byte-identical**, and normalizing
away the numeric suffix of the counter-driven generated identifiers makes the other 30 identical too —
**zero residual differences**. Not one `package_info.cs`, `.csproj` or `README.md` moved anywhere in the
corpus, which is exactly where a divergent recording would have surfaced (recorded conversions land in
`package_info.cs` and drive `ImplicitConvGenerator`).

The 30 split two ways, both counter renumbering: **19 capture (`ʗN`, `getCapturedVarName`'s per-prefix
counter)**, **10 type-switch temp (`ᴛN`, `getGlobalTempVarName("switch")`)**, one file both. The `ᴛN`
counter is **package-global**, which is why all ten of its files are in `go/types`: ONE extra hoist in the
discarded traversal shifts every later `switchᴛN` in the whole package. That half is a family CNR
structurally cannot see — no behavioral project puts a side-effecting type-switch tag inside an argument
subtree — so **the stdlib A/B, not CNR, is the instrument that closes this class**. Worth remembering for
the next converter change that touches a traversal: CNR's 571 small packages and the corpus's 302 real
ones fail in different places.

**Corpus levelled in the same change: 29 files overlaid** into `src/core` (+116/−116 lines, mechanically
verified to be counter renumbering and nothing else), and the full stdlib solution rebuilt clean on top of
them. `runtime/mfinal.cs.auto` also renumbered but is deliberately NOT overlaid — the standard overlay rule
excludes `*.cs.auto`, and those siblings are levelled together as CleanupBacklog item 18.

**Guard:** `TestNestedArgumentConversionIsNotExponential` (`nestedArgScaling_test.go`) converts a 30-deep
nested call under a 90s budget in a CHILD PROCESS — same plumbing as the chained-call guard, and for the
same reason (the conversion cannot be cancelled, so an in-process regression would keep `go test` alive
until the harness killed it) — then asserts every nesting level survived into the emitted C#, so it cannot
pass by dropping the expression. Negative control against the pre-fix source: **FAIL at 90.02s**; with the
fix, **PASS at 0.85s**. `runWithinBudget`'s timeout message is now generic, with each guard naming its own
defect in the `Fatalf` that wraps it.

**Reproduction fixture** (depth N nested calls; N=22 is the row above):

```go
package main

func f(x int) int { return x + 1 }

func main() {
	y := f(f(f(/* … N deep … */ 1)))
	println(y)
}
```

### How to profile the converter — the recipe, and three traps that cost the sibling arc an hour

The chained-call arc was diagnosed by profiling a live, still-spinning converter. That is now a
one-liner, and this section exists so the next session does not rebuild it:

```
GO2CS_PPROF=localhost:6060 go2cs -recurse ./app ./out       # off unless the var is set
go tool pprof -top -nodecount=35 http://localhost:6060/debug/pprof/profile?seconds=20
curl http://localhost:6060/debug/pprof/goroutine?debug=2    # every goroutine's stack
```

The endpoint is loopback-only by design (it serves goroutine stacks and heap contents); a bare
`:6060` is read as localhost and an explicitly non-loopback host is refused. See
[`diagnosticProfiling.go`](../../src/go2cs/diagnosticProfiling.go).

**Trap 1 — a `-cpuprofile`-style flag cannot see this class of bug at all.** Those write on exit, and
the failure mode is a run that never exits. Same blind spot for `-memprofile`. The live endpoint (or a
stack dump on a timer) is the only thing that observes it.

**Trap 2 — `dlv attach` KILLS its target.** On a non-terminal stdin it exits with
`Stdin is not a terminal, use '-r' …` and takes the process down with it, destroying a reproduction
that took minutes to reach. If a debugger is genuinely wanted, pass
`--allow-non-terminal-interactive=true` or run headless (`--headless --listen`) and connect
separately — but the pprof endpoint above is the cheaper answer.

**Trap 3 — Ctrl+Break does NOT dump goroutines on Windows.** The Go runtime's `ctrlHandler` maps
`CTRL_BREAK_EVENT` to SIGINT and calls `exit(2)` when nothing is handling it; there is no SIGQUIT-style
traceback the way there is on Unix. Do not plan a diagnosis around it.

**Read the process before the source.** The single most useful step in the sibling arc took seconds and
no tooling: sample CPU and working set. **CPU pegged + heap flat** ⇒ exhaustive re-work (this family).
**0% CPU** ⇒ deadlock. **Heap climbing** ⇒ runaway allocation/recursion. Stack depth *stable* while CPU
burns is the signature of re-walking a bounded tree, which is what separates an exponential from
runaway recursion — and it rules out most of the field before a profiler is even attached.

Per-package wall time is now printed at the end of every `-recurse` run (`Slowest N of M packages`), so
a superlinearity that has not yet become fatal is visible as an outlier rather than needing a
reproduction to find.

## CLOSED — the issue-#33 follow-up: the bsoncodec "hang" is an EXPONENTIAL, and it is fixed (2026-08-07)

**The reporter re-ran with the three fixes in, cleared the crash, and hit a different wall: a `-recurse`
run "hanging indefinitely" at `[1440/1726] Converting go.mongodb.org/mongo-driver/bson/bsoncodec`, over
half an hour on one package. It is not a hang. It is `(p+1)^N` work, and the whole arc is measured.**

**Diagnosed from the process, not from the source.** Reproduced locally in a 7-package closure (a scratch
module importing `bson/bsoncodec` from `go.mongodb.org/mongo-driver@v1.17.9`): the other six packages
convert in seconds, bsoncodec never finishes. The process is **CPU-bound at ~1.5 cores with a FLAT 345 MB
working set** — which is what rules the field down to one answer before any code is read: not a deadlock
(that is 0% CPU), not a leak (that grows). A CPU profile puts `convCallExpr`/`convExpr` at **66%
cumulative, mutually recursive**, the balance being GC of what they allocate; goroutine dumps show a
**stable ~40-deep** `convCallExpr → convExpr → convSelectorExpr → convExpr` cycle that does **not** grow.
Bounded depth with unbounded work is re-walking, not runaway recursion.

**Root cause.** A fluent chain nests LEFT, so each link's callee IS the rest of the chain.
`convCallExpr`'s argument classifier ran `funcName := v.convExpr(callExpr.Fun, nil)` **inside**
`for i := range params.Len()` — a full conversion of the entire callee subtree on every iteration — purely
to test whether the callee TEXT spelled `print`/`println`, and Phase 7 then converted it once more for
real. A call with p parameters walked its callee **p+1** times, which on a chain compounds to **(p+1)^N**.
`bsoncodec` registers its default codecs as **42-link** (encoders) and **63-link** (decoders)
`rb.RegisterTypeEncoder(t, codec).…` chains over a **2-parameter** method: 3^42 ≈ 1.2e19 callee walks for
one function.

**The fix is to stop asking the question in text.** `callFunIsUniversePrint` reads the name from the AST
and is O(1). It agrees with the old form by construction: `identIsUniverseBuiltin`
(`ObjectOf(ident).(*types.Builtin)`) already required a bare identifier resolving to Universe, and such an
identifier's name IS the built-in's name — a shadowing declaration makes both forms false.

**Rule this establishes, and it generalizes past this bug:** never derive a predicate from CONVERTED TEXT
when the AST or the type system answers it. Conversion is not a pure function of a node — it is a full
subtree walk with side effects — so a text probe inside a loop is a hidden complexity multiplier, and on
any LEFT-NESTING construct it is exponential rather than merely quadratic.

Paired A/B, idle machine, single-package conversion of a synthetic chain over a 2-parameter method (the
bsoncodec shape); `after` is flat at the go/packages load floor:

| links | before | after |
|---|---|---|
| 12 | 4,375ms | 1,902ms |
| 16 | **>120s (killed)** | 2,014ms |
| 20 | **>120s (killed)** | 2,342ms |
| 24 | **>120s (killed)** | 1,912ms |
| 42 | **>120s (killed)** | 1,974ms |

And the reporter's real shape: the `bsoncodec` closure converts **7/7 in 36.7s**, the package's 42-link
chain emitted faithfully (all links, interface adapters and `ж<T>` boxes intact).

**Gates — all green, and the arc is NOT emission-neutral, which CNR caught rather than argued.**

1. **CNR: 2 of 569 changed** — `DeferArgEnclosingCapture/main.cs` and `GoStmtValueReturn/main.cs`, both a
   pure capture-variable RENUMBERING (`doneʗ3`→`doneʗ2`, `oʗ2`→`oʗ1`), declaration and uses renamed
   together. The discarded callee conversion had been **bumping the capture counter as a side effect**, so
   removing it closes a gap in the sequence. Verified collision-free (every `…ʗN` occurs exactly twice,
   properly nested) and then verified where it counts: both projects **Compile pass** and **Output pass**
   against `go run`. Goldens re-baselined with `UpdateTestTargets --createTargetFiles`; only those two
   `.cs.target` moved, no test-method churn.
2. **Full behavioral suite: 544/544** Transpile+Compile+Target, **514/514** Output, 0 failed (1,792.5s).
3. **`go test ./...`: ok**, exit 0 (106s), including the new guard and the sibling lane's projitems gate
   (the new source file is registered in `go2cs-src.projitems`, BOM and CRLF preserved).

**Guard:** `TestChainedCallConversionIsNotExponential` (`chainedCallScaling_test.go`) converts a 40-link
chain over a 2-parameter method under a 90s budget **in a CHILD PROCESS** — the conversion cannot be
cancelled, so a regression would otherwise leave a goroutine spinning and keep `go test` alive until the
harness killed it minutes later — then asserts every link survived into the emitted C#, so it cannot pass
by dropping the chain. Negative control against the pre-fix source: **FAIL at 90.05s**; with the fix,
**PASS at 1.6s**.

### Finding handed on — a SECOND exponential of the same class, on the ARGUMENT path

Not owned by this arc and not what the reporter hit. After rendering a call, `convCallExpr` re-walks every
argument through `checkForImplicitConversion` — its own comment says it "re-converts each arg purely for
its side-effects (recording implicit conversions); the result is discarded" — which is a second full
conversion of each argument subtree, compounding to **2^depth** on NESTED calls (`f(f(f(…)))`). Measured
with the callee fix already in: nesting depth 18 → 3.4s, depth 22 → 24.9s. It did not block the reporter
(argument nesting that deep is rare where 42-link fluent chains are not), and the recording is
**entirely type-driven** — `expr` flows only to the return value — so the durable fix is to split the
recording from the rendering and let the discard-the-result call site skip `convExpr` entirely.
Deliberately NOT folded in here: it is an independent change with its own emission-regression surface (this
arc already moved two goldens), and entangling it with a one-line fix would cost the clean A/B. **Banked as
its own arc by user ruling (2026-08-07), and CLOSED the same day by r43a-argexp — see the section at the
top of this board for the fix, the desktop A/B and the gates. Holding it back was the right call: it moved
four behavioral goldens and 29 corpus files, none of which would have been separable inside the one-line
callee fix.**

## ~~OWED~~ DISCHARGED — the issue-#33 arc is measured on Windows (2026-08-06, same day)

**Every owed gate ran green, the 3a probe validated findings (b) and (c) end to end, and the probe paid
for itself with a new finding — (d), below, FIXED and CLOSED the same day.**

1. **CNR: NO REGRESSION — byte-identical across all 569**, exit 0 (1,088s). `go2cs.exe` was rebuilt
   immediately before the run per this entry's own route-#2 warning. This also discharges item 5
   corpus-wide: no bare-LF line boundary surfaced anywhere under F3's normalized split.
2. **Full behavioral suite: 544/544** Transpile+Compile+Target, **514/514** Output, 0 failed
   (3,508.5s under machine load). Honesty proven, not assumed: the exe was rebuilt after CNR's
   transpile (20:17:47 > every CNR-refreshed `.cs` at 20:02:56), the suite re-transpiled all 544
   (`DeepEqual/main.cs` → 20:21:13), and the tree was CLEAN after — emission unchanged, in agreement
   with CNR.
3. **`go test ./...`: ok, exit 0** (84.3s). The three new guards' first Windows run: 3/3 PASS —
   `TestModuleCacheVestigialReplaceLoad` (1.17s, both sides of its fixture, so the control still
   reproduces on Windows), `TestUntypedPackageConvertsWithoutPanic` (1.73s),
   `TestEscapeAnalysisPanicReachesCaller`. The container's nine Linux failures are absent here, as
   predicted.
3a. **The otel probe ran, with finding (c) honored first:** this box's native toolchain is go1.23.2 —
   *below* (c)'s floor — so the probe converter was built with `GOTOOLCHAIN=go1.25.0` into a scratch
   location (the repo's binary stays the native build the gates measured). A module importing
   `go.opentelemetry.io/otel@v1.44.0` (vestigial `./trace`/`./metric` replaces confirmed present in
   the cached `go.mod`): closure 209 discovered, **25/25 converted** (1 app + 24 third-party, 48.2s),
   **zero** `invalid package name`, **zero** `newer Go version` — (b)'s remedy and (c)'s guidance both
   hold on Windows, where the reporter hit them.
4. **Sweep waived by this entry's own condition** — 1–3 clean and byte-identical emission leaves no
   path into the banked suites.

The stray remote branch `claude/recurse-option-diagnosis-cb1ins` (fully contained in `master`) is
deleted. The container's original record follows.

**(d) CLOSED 2026-08-06 — the build-constraint evaluator could not parse a Go release tag, and the
`!go1.21` "asymmetry" was never an asymmetry.** Observed in the same otel probe: five
`github.com/go-logr/logr@v1.4.3` files gated `//go:build go1.21` each warned `failed to parse build
constraint: 1:4: expected 'EOF', found .21`, while the paired `context_noslog.go` (`!go1.21`) warned not at
all and was correctly excluded — with an identical dual-line header, which made the two look like they were
handled by different code paths.

**Root cause.** `EvaluateConstraint` ran the constraint through `parser.ParseExpr`, a Go **expression**
parser, for which `go1.21` is the identifier `go1` followed by an illegal `.21` selector. It fails on
`!go1.21` too — at `1:5` — so nothing diverged here. `context_noslog.go` produced no warning because
`go/packages` had **already excluded it upstream**: it is absent from `pkg.GoFiles` and never reaches this
code at all. Verified directly, both halves: `ParseExpr` errors on both forms, and a two-file probe module
loaded through `packages.Load` returns only the `go1.21` file.

**Why it was not cosmetic.** `conversionDriver.go` warns on a constraint error and falls THROUGH to
including the file, so on those five files the wrong machinery reached the right answer. It is wrong the
moment a constraint mixes a release tag with a platform: `//go:build go1.21 && windows` converted for linux
lost its platform half along with the rest of the expression and was included. Two further defects fell out
of the same layer — the regex scanner matched only `//go:`-prefixed lines, so a legacy `// +build`-only file
(the norm in pre-1.17 third-party modules, which is exactly what `-recurse` meets) converted as
**unconstrained**; and it scanned the WHOLE file, so a `//go:build` quoted in documentation *below* the
package clause gated the file.

**Fix.** The hand-rolled parse/eval layer is gone, replaced by `go/build/constraint` — the package the
toolchain itself uses. `constraint.IsGoBuild`/`IsPlusBuild` recognize the lines (column zero, header only,
`//go:build` winning over `+build` as go/build orders them), `constraint.Parse` parses both syntaxes, and
`Expr.Eval` drives a single `matchTag` callback that owns every tag class. Tag matching is now
case-sensitive, as the toolchain matches; the old evaluator lowercased the whole expression, which quietly
made a mixed-case `-tags MyTag` unsatisfiable.

**One hazard this fix creates and closes in the same change, and it is finding (c) wearing a different
hat.** Release-tag evaluation was previously inert — it *always* errored — so activating it puts the
compiled-in `build.Default.ReleaseTags` in charge of `go1.N`. Under `GOTOOLCHAIN=auto` that list is not the
loader's: go2cs.exe built with Go 1.23 converting a module that declares `go 1.25` would call `go1.24` false
while `go/packages` called it true, dropping every file gated between the two **along with** the `!go1.24`
sibling the loader had already excluded — leaving the package with neither half. That configuration is not
hypothetical; it is what this machine had (converter built go1.23.2, otel probe loading under go1.25.0).
Over-exclusion is this evaluator's recurring failure mode — the `purego` seeding and the `goexperiment`
ToolTags branch both exist to undo one — and it is the dangerous direction, because the loader has already
applied the full constraint for the target platform, so anything this pass subtracts is real code. Release
tags are therefore resolved by asking the go command (`go env GOVERSION` from the same directory
`packages.Load` uses), cached per module root so a `-stdlib` run pays one ~300 ms lookup rather than 302.
Note this does **not** retire (c): the linked-in *type checker* is still whatever release compiled go2cs,
and no toolchain switch reaches it. Build go2cs with a toolchain at least as new as the closure's newest
`go` directive regardless.

Guarded by `src/go2cs/buildConstraints_test.go` — release tags bare/negated/compound, the legacy `+build`
grammar, extraction precedence, and the loader-toolchain resolution. Verified against the pre-fix converter
rather than assumed: every new assertion fails on it, including the two the fix was not looking for (the
legacy-only file and the documentation-gated file).

For the next **local (Windows)** session: `master` carries the issue-#33 arc in three commits — `fe9bec0`
(the `package_info.cs` EOL-agnostic read-back, Linux finding F3), `6ca9565` (the panic fix itself), and the
main-module load shape for module-cache packages that closes finding (b) below — posted directly to master
under the same standing ruling as the issue-#32 entry, and for the same reason: a remote Linux container
where the .NET/PowerShell gates cannot run, so the arc ships with converter-level evidence only. All three
are emission-neutral for the corpus, and that is measured rather than argued: **569/569** behavioral
packages transpile byte-identically to the converter that predates each change.

**What the reporter hit.** A `-recurse` conversion of [renart](https://github.com/renart-data/renart) died
at `[736/1726]` on a nil dereference at `escapeAnalysisOperations.go:739`, discarding ~1,000 packages of
queued work; reported again under `-recurse=module`, where it lands at `[33/44]` on the app's own packages.

**Root cause, two independent halves — the second is the one that mattered.**

1. **The dereference.** `go/types` records **no type at all** for an expression whose operand went invalid
   (`Checker.record` returns early for `mode == invalid`), so `types.Info.TypeOf` returns a nil
   **interface** — not `Typ[Invalid]`. The reported crash is `TypeOf(call.Fun).Underlying()` for an
   address-taken argument of a call to an undefined function. The `addr=0x20` in the pasted trace is the
   itab's `fun[1]` slot, which is what distinguishes nil-interface from typed-nil. Reproduced in six lines
   of Go, same file, same line, same fault address.
2. **The containment hole.** `ModuleConverter.convertAll` and `StdLibConverter.convertPackage` each already
   wrap a conversion in `recover` so one unconvertible package fails alone — and `performEscapeAnalysis`
   runs its files in **goroutines**, where a panic unwinds only its own stack. Every fault raised on that
   side of the `go` statement was unrecoverable by anyone. That is what turned a one-package defect into a
   dead run. Workers now capture the first panic **with `debug.Stack()`** (before the frame is lost, so the
   report still names the faulting converter line rather than the re-raise site) and re-panic after `Wait`.

**Rule this establishes, and it generalizes past this bug:** any pass that spawns goroutines must re-raise a
worker panic on the caller's goroutine, or the per-package containment both batch drivers depend on is
silently void. Written up under *Packages That Do Not Type-Check* in
[`ConversionStrategies-Reference.md`](../ConversionStrategies-Reference.md), with the `underlyingOf()`
convention for any type reached through `TypeOf`/`getType` on an arbitrary source expression.

**What the container DID establish.** All 569 behavioral packages re-transpiled twice — once with the
converter that predates the arc, once with the fix — and the output is **byte-identical everywhere except
two Windows-only packages**, `UnsafeStringEmpty` and `FindFirstFileData`, which do not type-check on Linux
(`syscall.UTF16ToString`). Those are the in-repo proof rather than an exception: the old converter dropped
`UnsafeStringEmpty/main.go` **entirely** through the per-file recover, and the fixed converter emits a
`main.cs` matching the committed **Windows** golden byte-for-byte modulo CRLF. The converter's own
`go test ./...` failure set is **identical with and without** the arc (isolated by re-running with only the
F3 commit applied) — nine failures, all pre-existing Linux path-separator/CRLF findings, none in these paths.

Owed, in order (budgets from the CLAUDE.md table):

1. `./src/tests/Behavioral/check-no-regression.ps1` — timeout 700s. **Expect byte-identical.** Both commits
   are no-ops on Windows by construction: F3's read path only differs on an LF file (autocrlf gives CRLF
   working trees), and the #33 guards only fire on a package that does not type-check — the behavioral
   corpus has none on Windows. ⚠ Re-run `go build -o bin\go2cs.exe` first: a `git checkout` restore refreshes
   every `.cs` mtime and re-arms false-green route #2, exactly as the issue-#32 entry records.
2. `./src/tests/Behavioral/run-behavioral.ps1` (full) — timeout 2100s. Expect 544/544 + 514/514.
3. `go test ./...` from `src/go2cs` — expect `ok`, exit 0, including the three new guards
   (`TestUntypedPackageConvertsWithoutPanic`, `TestEscapeAnalysisPanicReachesCaller`,
   `TestModuleCacheVestigialReplaceLoad`) and the seven pre-existing recurse tests that the Linux container
   cannot pass. The third guard asserts both sides from one fixture, so a Windows pass also confirms the
   control still reproduces there.
3a. **Worth doing once, and it is not a gate:** a real `-recurse` run against a module that depends on
   `go.opentelemetry.io/otel@v1.44.0` (or any monorepo-layout module with relative replaces) — the container
   measured 2 → 0 `invalid package name` failures, and Windows is where the reporter hit it. Build the
   converter with **Go 1.25 or newer** first, per finding (c).
4. `./src/run-validated-sweep.ps1` only if 1–3 surface anything — byte-identical emission leaves no path
   into the banked suites otherwise.
5. **One Windows-specific risk worth a look, not a gate:** F3 now splits a read-back `package_info.cs`
   on normalized `\n`. A file containing a **bare LF** inside a line was previously kept as part of that
   line and is now a line boundary. Converter-written files are CRLF throughout and autocrlf normalizes on
   checkout, so this should be unreachable — CNR clean in step 1 confirms it across all 569.

### Findings for follow-up, neither owned by this arc

**(a) F3 was masking the Linux F5 failures.** With the read-back seam fixed, the converter's `go test ./...`
on Linux runs to completion for the first time and surfaces **nine** failures. That is not a regression: the
old binary `log.Fatal`ed inside the first `processConversion` and **ended the whole test binary**, so most
of the suite never ran and the truncated output read as two failures. Seven of the nine are F5 (Linux
`filepath.Join` does not normalize the `\` the code injects — `$(go2csPath)core\fmt/\fmt.csproj`) and two
are the CRLF-template tests. All nine are unchanged with the #33 arc removed. Recorded here because the
*count* of Linux failures moved for a benign reason, and the next Linux session should not read it as drift.
F5 remains Arc 2 of [`PLAN-linux-operation.md`](../PLAN-linux-operation.md), untouched.

**(b) ROOTED, REPRODUCED, and FIXED — the reporter's `invalid package name: ""` was the issue-#32 family,
one directive over.** Reproduced end-to-end the same session against the reporter's own dependency
(`go.opentelemetry.io/otel@v1.44.0`), so this is measured, not argued. The hypothesis first written here —
"the standalone module-cache load is a weaker context" — is **confirmed in mechanism and wrong in detail**:
it has nothing to do with MVS version selection or the app's own `replace` directives.

**The mechanism.** `otel@v1.44.0/go.mod` carries the monorepo's own relative replaces:

```
replace go.opentelemetry.io/otel/trace  => ./trace
replace go.opentelemetry.io/otel/metric => ./metric
```

Valid in the otel *source repo*, where those are sibling directories. The published module **zip excludes
them** — `trace` and `metric` are separate modules — so in the cache `./trace` does not exist. A `replace` is
honored **only in the main module**, and `processConversion` loading a package with `Dir` inside the cache is
exactly what promotes that dependency's `go.mod` to main-module status. The go command then says
`replacement directory ./trace does not exist`, `otel/trace` never loads, its `types.Package` stays
empty-named, and `go/types` reports `could not import go.opentelemetry.io/otel/trace (invalid package
name: "")` at every use site. Same root as issue #32 — **a module-cache directory is not a main module** —
and `GOWORK=off` cannot reach it, because `replace` is not a workspace feature.

**The three-way probe** (`packages.Load`, `LoadAllSyntax`, run under go1.25 so the language-version noise
below is out of the picture):

| Load shape | Result |
|:--|:--|
| **A** — `Dir` = the cache dir, pattern = that dir (**what `processConversion` does**) | `could not import go.opentelemetry.io/otel/trace (invalid package name: "")` — the reporter's error verbatim |
| **B** — `Dir` = the **app module**, pattern = the **import path** | **0 errors.** The dependency's replaces are ignored, as a non-main module's must be |
| **C** — `Dir` = `otel/trace@v1.44.0`'s own cache dir | 3 further failures from *its* vestigial `replace go.opentelemetry.io/otel => ../` |

**Blast radius, measured:** **189 of the 244** packages in the otel module zip import `otel/trace` or
`otel/metric`, so all 189 lose their types under load shape A. This is not an otel quirk — it is every
monorepo-layout module that carries relative replaces, which is the common shape for a multi-module Go repo.

**The remedy is validated, not sketched:** for a package under `GOMODCACHE`, load it from the **main
module's** directory by **import path** (shape B) instead of standalone by directory. That also makes the
issue-#32 `GOWORK=off` gate redundant for third-party packages — the go command never enters the
dependency's directory, so a vestigial `go.work` is not read either — though the gate should stay for the
non-recurse paths. `ModuleConverter` has both inputs already (`pkgPath` and the main module dir);
`processConversion` takes a directory, so the import path needs plumbing through. Worth weighing at the same
time: this is also 1,726 separate `packages.Load` invocations, the dominant cost of a recurse run, against a
closure `loadClosure` already type-checked correctly in one pass.

**LANDED 2026-08-07** (commit on master; design detail in
[`DESIGN-recursive-enduser-conversion.md`](../phase3/DESIGN-recursive-enduser-conversion.md), *The same seam,
one directive over*). A module-cache package is loaded from the main module's directory by import path
whenever the run is `-recurse` and both inputs are known; every other load keeps the directory shape and the
`GOWORK=off` gate with it. Measured: the otel `-recurse` run goes **2 → 0** `invalid package name` failures,
and with the converter rebuilt under Go 1.25 the closure converts **14/14 with no warning of any kind**. All
**569** behavioral packages transpile **byte-identically** to the converter that predates the change —
expected, since no behavioral package is under `GOMODCACHE`, and measured rather than assumed. Guarded by
`TestModuleCacheVestigialReplaceLoad`, which asserts BOTH sides from one fixture so the guard cannot pass
vacuously. The converter's own suite has the same nine pre-existing Linux failures, unchanged.

**Deliberately LEFT UNDONE — the one item this arc hands forward.** Closure reuse: `loadClosure` already
type-checks the whole graph in one pass in the main module's context, and now that each package is loaded
from that same context anyway, the per-package reload is re-deriving in **1,726 separate `packages.Load`
invocations** what one pass already had. That is the dominant cost of a recurse run. It was not folded into
this fix because it is a pipeline-shape decision rather than a bug fix, and because it has to respect
`-recurse=module`, which deliberately skips the full-closure type-check precisely so an unconvertible
dependency graph cannot block the app's own code (issue #32's mode). Rooted and ready; wants a measurement of
the real saving before it is worth the risk.

**(c) A second, independent finding from the same reproduction — the converter cannot type-check a module
whose `go` directive exceeds the Go release go2cs was BUILT with.** `otel@v1.44.0` declares `go 1.25.0`; a
go2cs built with go1.24 reports `package requires newer Go version go1.25 (application built with go1.24)`
and every downstream expression goes untyped. The go *command* switches toolchains automatically
(`GOTOOLCHAIN=auto`), but the type checker go2cs links in is whatever release compiled it, and no toolchain
switch reaches that. This is invisible until a dependency adopts a new language version, then it silently
degrades whole packages. Two things follow: build go2cs with a toolchain at least as new as the newest `go`
directive in any closure it is asked to convert, and consider making the converter **say so by name** rather
than letting it read as an ordinary type error. Independent of (b) — it reproduced on both load shapes and
disappeared on both when the probe was re-run under go1.25, and confirmed a third time end to end: rebuilding
the converter itself with Go 1.25 is what took the otel `-recurse` run from "14/14 with 13 best-effort
warnings" to "14/14, silent". **Left as guidance, not code** — the honest fix is a build requirement, and the
optional refinement (naming this condition in the diagnostic instead of letting it read as an ordinary type
error) is a small, separate change nobody is blocked on.

## ~~OWED~~ DISCHARGED — the issue-#32 go.work fix is measured on Windows (2026-08-06, same day)

**Every owed gate ran; the change is clean, and its emission-neutrality is proved against the converter
that predates it rather than argued from the diff.**

1. **CNR — the gate could not use the committed corpus as its reference, so it was run in a stronger
   form.** A plain CNR reported drift under *both* candidate roots, in opposite directions and on
   disjoint file sets (4 files vs 12) — a **pre-existing** condition of the committed
   `package_info.cs` corpus that has nothing to do with this change; it gets its own entry below. The
   gate's actual question was therefore answered **converter-vs-converter**: every one of the **569**
   behavioral packages transpiled twice in one environment, once with `master`'s converter and once
   with a converter built from `c57f1a878` (the commit before this arc), hashing all **1,176** generated
   `.cs`. Manifests **byte-identical** (`A8E0B75B…C15EC80` both sides), **0 transpile failures**. The
   change is emission-neutral across the whole corpus, which is what "expect byte-identical" was for.
2. **Full behavioral suite: 544/544** Transpile+Compile+Target, **514/514** output comparisons, 0
   failed, 30 skipped (2,124.3s). ⚠ The FIRST attempt was a **false green by documented route #2** and
   is recorded because the trap is easy to re-enter: restoring the tree with `git checkout` refreshes
   every `.cs` mtime, so `UpToDate`'s `csTime <= exe` guard sees fresh output, **Transpile is skipped
   for all 544**, and the suite validates the committed `.cs` instead of the converter's. The guard is
   sound — a *checkout* defeats it, not a converter rebuild. Re-running `go build -o bin\go2cs.exe`
   before the suite makes the exe newest again and forces the real pass; confirm it ran by checking that
   the transpile left the tree dirty.
3. **The recurse guards' first Windows run: all 7 PASS** (14.7s) — `TestModuleCachePoisonedGoWorkLoad`
   0.65s, `TestRecurseModuleOnly`, `TestRecurseSyntheticModule`, `TestRecurseNuGetReferences`,
   `TestRecurseNuGetResolvesForeignImplements`, `TestRecurseLinknameForwarder`, `TestRecurseModeFlag`.
   Full `go test ./...` is **`ok`, exit 0** — the container's 7 "failures" were Linux-path artifacts, as
   it predicted. *Additionally measured*, because the committed guard pins `goModCache` directly and so
   never exercises the real Windows resolution: `goModCacheDir()` resolves through `go env` to
   `C:\Users\rcarroll\go\pkg\mod` (the `GOMODCACHE` env var is unset here, so the second fallback is the
   live path), and `isPathUnder` classifies correctly against a real cache path — case-insensitive in
   both directions, separator-agnostic, root-inclusive, and **not** fooled by the sibling-prefix trap
   `…\pkg\mod-notthecache`. The gate fires on Windows.
4. **Sweep waived by this entry's own condition.** Items 1–3 surfaced nothing attributable to the
   change, and byte-identical emission leaves no path into the banked suites. The corpus finding below
   is confined to behavioral `package_info.cs` files and touches neither `src/core` nor any banked suite.
5. **The `eol=crlf` pin is invisible on this clone, positively.** `git check-attr eol` reports `crlf`
   for all three templates, all three are fully CRLF on disk, and `git status` stayed clean across the
   pull — the expected outcome, verified rather than assumed.

The container's original record follows, kept for its diagnosis.

For the next **local (Windows)** session: `master` carries the **second** issue-#32 arc — commit
`121c61d` (the `GOWORK=off` fix + its guard) and `0267629` (the template `eol=crlf` pin), the diagnosis
of the reporter's pasted `-recurse` failure log (the Renart project) and its fix, posted directly to
master per user ruling 2026-08-06. Same posture as the d00cac5 entry below, same reason: a remote Linux
container where the standing gates cannot run, so the change ships with unit-level evidence only.

What was found (full write-up: [`DESIGN-recursive-enduser-conversion.md`](../phase3/DESIGN-recursive-enduser-conversion.md),
*Module-cache loads and the vestigial `go.work`*): the reporter's abort was their **pre-d00cac5 binary**
(the fatal load path this board's discharged entry below already measured), but underneath it sits a real,
still-current loss — `cloud.google.com/go`'s module zip ships the monorepo's `go.work`, and
`processConversion`'s reload, running the go command from inside the module cache, enters workspace mode
and fails every package of that root module ("cannot load module ../accessapproval listed in go.work
file"). The fix appends `GOWORK=off` to the loader env **only when the input dir is under `GOMODCACHE`**;
ambient workspace behavior is preserved everywhere else. A second commit pins the three embedded converter
templates `eol=crlf` in `.gitattributes` — the checkout-level discharge of the CRLF seam the entry below
recorded as recorded-not-owed (an LF checkout's converter `log.Fatal`ed on every conversion; the
`"\r\n"`-splitting code seam itself is unchanged).

What the container DID establish: `TestModuleCachePoisonedGoWorkLoad` (new, network-free, both sides of
the gate) passes; the full `go test ./...` failure set is **identical to baseline** (the same 7
pre-existing Windows-path tests, nothing new — measured with-fix vs. master on the same box); an
end-to-end repro (a module importing `cloud.google.com/go/civil@v0.123.0`) goes from `1/2 converted
(civil failed)` to `2/2 converted` with the emitted `civil.cs`/csproj/slnx spot-checked.

Owed, in order (budgets from the CLAUDE.md table) — the d00cac5 pattern verbatim:

1. `./src/tests/Behavioral/check-no-regression.ps1` — timeout 700s. **Expect byte-identical**: the change
   is an env-var gate on a `-recurse`-only load path plus a checkout attribute; no emission logic moved.
2. `./src/tests/Behavioral/run-behavioral.ps1` (full) — timeout 2100s. Expect 544/544 + 514/514.
3. `go test -run 'TestRecurse|TestModuleCachePoisonedGoWorkLoad' ./` from `src/go2cs` — the new guard's
   first Windows run.
4. `./src/run-validated-sweep.ps1` only if 1–3 surface anything (no path into the banked suites otherwise).
5. ⚠ The `eol=crlf` pin takes effect on **checkout** — existing Windows clones already have CRLF working
   trees via autocrlf, so expect no visible change there; a `git status` after pulling the attribute
   commit should stay clean for the three templates. If it does not, that is a finding.

## ~~Open~~ CLOSED — CNR's verdict no longer moves with an ambient variable, and the split `package_info.cs` corpus is normalized (2026-08-06, same day)

**All three steps landed, in the order this entry insisted on** — pin the root, make an unusable one
loud, *then* normalize — because normalizing against a root no gate enforces would only have re-split the
corpus on the next machine. Commits `826b7e486` (the mechanism) and `9859dd993` (the data), kept apart so
each is reviewable on its own.

1. **Every seam names its root, computed from its own location.** Five invocation sites, from a
   tree-wide sweep for `go2cs.exe`: `check-no-regression.ps1` (a `$PSScriptRoot` walk),
   `BehavioralRunner` and `PerformanceRunner` (an `AppContext.BaseDirectory` walk, `s_srcRoot`), MSTest
   `BehavioralTestBase` (a new `Go2csRoot`, resolved in `Init` **before** its up-to-date early return and
   with the trailing separator trimmed — a backslash before a closing quote escapes it on a Windows
   command line), and `run-validated-sweep.ps1`. The sweep was **not** on the list above and is pinned
   deliberately: a `-tests` run self-locates only when the ambient root is **invalid**, so a `GO2CSPATH`
   aimed at some *other* real go2cs tree — a `deploy-core` staging root — would still have been honored,
   building a suite against one tree's metadata while compiling the other's sources. Confirmed first
   that `-go2cspath` cannot move WHERE single-package output is written (it feeds only
   `getImportPackageInfo`'s `TargetDir` substitution; `outputFilePath` is untouched): `DeepEqual`
   transpiled **with** the flag lands in place and leaves the tree clean, **without** it the same command
   drops its five `reflect` aliases.
2. **The converter recovers, and says so when it cannot.** `resolveGo2CSPath`
   (`commandLineOptions.go`) now runs for **every** single-package conversion, not just `-tests`: when
   the configured root is not a go2cs root (no `core\golib\golib.csproj`) it walks the ancestors of the
   conversion's **OUTPUT** path for one. Output, not input, is the anchor — the emitted
   `package_info.cs`/`.csproj` and their `$(go2csPath)core` references live there, so the tree that must
   satisfy them is the tree the output is written into; where the two differ (converting GOROOT sources
   into a repository tree) the input walks the wrong chain entirely, and where they are the same
   directory — the bare `go2cs <pkg-dir>` — it is exactly what makes an unconfigured run inside a clone
   resolve against that clone. An explicitly configured *working* root still wins. Found nothing, the run
   proceeds (standalone conversion with no deployed runtime is legitimate) but emits ONE prominent stderr
   warning naming the resolved path and both consequences. `-recurse` warns but never self-locates (its
   root doubles as the output root without a second positional, so moving it would move the generated
   tree); `-recurse=nuget` does neither; `-stdlib` does neither, because there the root **is** the output
   root the run itself populates and an absent `golib` is the normal state of a first conversion. Guarded
   by `TestResolveGo2CSPathSelfLocation` / `TestResolveGo2CSPathUnusableRootWarns` — network-free, both
   sides, the real stderr captured through an `os.Stderr` swap rather than a stand-in, with the
   precedence rule and both suppressions pinned; the once-per-run warning latch is package-level and
   test-pinnable in the `goModCache` manner.
3. **Exactly the twelve, exactly the predicted direction.** The now-deterministic CNR reported precisely
   the twelve files this entry named, all **pure additions — 46 lines, 0 removals**: 28 `time`, 10 `os`
   (the `io/fs` re-exports `FileInfo`/`FileMode`/`DirEntry`/`PathError` plus `os.Signal`), 5 `syscall`,
   2 `encoding/json`, 1 `runtime`. Banked alone in `9859dd993`.

**Gates, all green.** `go test ./...` from `src/go2cs`: **ok, exit 0** (103.3s cold), including the two
new guards. `check-no-regression` **after** the normalization commit: **NO REGRESSION — byte-identical
across all 569** behavioral packages, exit 0 (917.5s; the pre-normalization run was 864.2s and reported
the twelve). Full behavioral suite: **544/544** Transpile, Compile and Target, **514/514** output
comparisons, 0 failed, 30 skipped — **PASS in 2,453.3s**. Both long runs sat above the CLAUDE.md budgets
because a sibling worktree was active; forward progress was confirmed by watching the transpile advance
alphabetically, not assumed. The MSTest seam was spot-checked separately (`--filter DeepEqual`, 4/4).

⚠ **The false-green trap was avoided by construction, and the avoidance was measured.** CNR's own
transpile leaves every `.cs` newer than `go2cs.exe`, which is precisely the state that makes
`UpToDate`'s `csTime <= exe` guard skip Transpile for all 544. `go2cs.exe` was rebuilt immediately before
the suite and the skip was disproved with mtimes, not assumed: `DeepEqual/main.cs` moved 15:14:26Z →
15:27:19Z, strictly newer than the exe at 15:24:43Z. **A clean tree after a CONFIRMED transpile is the
pass condition**, and that is what this run produced.

**DECISION (the coordinator's, recorded and not relitigated): `package_info.cs` gets NO `.cs.target`
golden.** CNR is deterministic now and is the standing converter gate; 569 new golden files is
disproportionate footprint for a line CNR already holds.

**Left alone, recorded rather than fixed:** `deploy-core.ps1` still stages to `%GOPATH%\src\go2cs` while
the converter's `-go2cspath` defaults to `~/go2cs`, so running the documented deploy does not populate
the root a flagless converter run reads. That divergence no longer costs anything — every gate names its
root, and a bare run self-locates or says why it cannot — so the two roots stay as they are rather than
being unified in this arc.

## ~~OWED~~ DISCHARGED — the issue-#32 `-recurse` change is now measured on Windows (2026-08-05, same day)

**All four gates ran or were legitimately waived; the change is clean.** (1) `check-no-regression`:
**byte-identical across all 569** behavioral packages — the entry's highest-stakes expectation held
exactly. (2) Full behavioral suite: **544/544** Transpile+Compile+Target, **514/514** output
comparisons, 0 failed (1,092.5s). (3) The recurse tests' first real Windows run: `TestRecurseModuleOnly`
**PASS** (0.81s — the Windows-path assertion that could only fail-on-Linux now actually exercises),
`TestRecurseSyntheticModule` PASS, `TestModuleConverterPartitionScope` both scopes PASS; full
`go test ./...` ok with nothing new failing. (4) The sweep was waived by this entry's own condition —
1–3 clean and byte-identical emission leaves no path into the banked suites. The stray remote branch
`claude/go2cs-issue-32-5osg4q` is deleted. The container's original record follows, kept for its
observations (the CRLF-coupled `packageInfoWriter` seam remains recorded-not-owed).

For the next **local (Windows)** session: commit `d00cac5` — [issue #32](https://github.com/ritchiecarroll/go2cs/issues/32),
`-recurse=module` plus the load-failure fix — was authored and pushed from a **remote Linux container**,
where the standing gates **cannot run**. It is on `master` with unit-level evidence only. Nothing about it
is suspected; it is simply **unmeasured against the corpus**, and that is the whole point of this entry.

What the container could not do, and why it is not a converter defect:

- **No `pwsh`, no `dotnet`** — `check-no-regression.ps1`, `run-behavioral.ps1` and
  `run-validated-sweep.ps1` are all Windows/pwsh instruments; none of the three ran.
- **The converter cannot write `package_info.cs` on an LF checkout at all.** `packageInfoWriter`
  splits the template on `"\r\n"` (packageInfoWriter.go:52,57), so on a `core.autocrlf`-less clone the
  `<ImportedTypeAliases>` section is never found and every package conversion `log.Fatalf`s. The recurse
  integration tests therefore fail **identically before and after** the change there — a checkout
  artifact, not a regression. (Recorded as its own observation: the converter is Windows-line-ending
  coupled at that one seam. Not owed as work; noted so the next reader does not re-diagnose it.)

What WAS established, so the re-check knows what to expect:

- `go test -short ./` failure set is **identical to baseline** — the same 6 pre-existing Windows-path
  tests (`TestParseCoreProjectRefs`, `TestCollectConvertedProjects*`, `TestIsSelfProjectReference`,
  `TestValidationPack*`), nothing new.
- New guards pass: `TestRecurseModeFlag` (extended), `TestModuleConverterPartitionScope`; the new
  `TestRecurseModuleOnly` fails on Linux at exactly the one Windows-path assertion its sibling
  `TestRecurseSyntheticModule` fails on (`$(go2csPath)core\fmt\fmt.csproj` emitted as `core\fmt/\fmt.csproj`).
- Smoke-run end to end against a CRLF'd template: `-recurse=module` converts an app plus its
  sub-package in dependency order and writes no `pkg\` tree, and a later plain `-recurse` fills exactly
  the referenced `pkg\` path. `diff -r` of the two runs' `src\` trees: `.cs`/`.csproj` byte-identical,
  only the `.slnx` `/pkg/` folder differs.

Owed, in order (budgets from the CLAUDE.md table):

1. `./src/tests/Behavioral/check-no-regression.ps1` — timeout 700s. **Expect byte-identical**: the
   change touches only error paths and `-recurse`-scoped branches, and no emission logic. A non-empty
   `git status` here is a real finding and outranks everything else in this entry.
2. `./src/tests/Behavioral/run-behavioral.ps1` (full, 4 phases) — timeout 2100s. Expect 544/544 +
   514/514 output comparisons.
3. The three recurse integration tests on Windows — `go test -run 'TestRecurse' ./` from `src/go2cs` —
   which is the FIRST real run `TestRecurseModuleOnly` will get.
4. `./src/run-validated-sweep.ps1` (backgrounded, 46–53 min) only if 1–3 surface anything; a
   converter change confined to the recurse driver has no path to the banked suites, so a clean 1–2
   discharges this item without it.

Also owed, trivially: **delete the remote branch `claude/go2cs-issue-32-5osg4q`.** It is fully
contained in `master` (both point at `d00cac5`) and the local copy is gone, but the remote one could
not be deleted from the container — the session's git proxy rejects ref-deletion pushes
(`send-pack: unexpected disconnect`, twice, for both `--delete` and `:branch` forms), and the GitHub
MCP surface here has no delete-branch tool. One `git push origin --delete claude/go2cs-issue-32-5osg4q`
locally, or the button on GitHub.

## LANDED — the GoFrame arc (2026-08-05), and what it leaves behind

The frame is built. `DESIGN-closure-emission.md` §4 is now the AS-BUILT record; §4.10 carries the
findings and §4.11 the bang verdict. Landed in five gated checkpoints along §4.8's path — golib frame,
declarations with unnamed results, recover + named results, function literals, then the rename and the
deletion of the machinery it replaced — each with its own full behavioral gate.

**Verdicts and findings, in one place:**

- **The bang is DROPPED.** `deferǃ` is `defer`. `defer` is a Go *keyword*, so no Go identifier can ever
  be spelled that way; it is not a C# keyword; and the one binder that ever put the bare name in scope
  was the execution context's lambda parameter, which is gone. `goǃ` and `makeǃ` keep theirs, for
  reasons of their own (`go` is the root namespace; `make` is a predeclared Go identifier a package may
  shadow). Full analysis: design §4.11.
- **§4.5 (open-coding the static defers) is NOT in the arc**, and the measurement says why. A defer whose
  target is a cached static method group already costs **0 B** under the frame; the residue §4.5 would
  remove is entirely the display class + delegate of a defer that genuinely CLOSES OVER something —
  measured at 96 B for one and 192 B for two. There is therefore no cheap subset to take: the eligible
  shapes are exactly the ones needing argument and receiver temps hoisted OUT of the `try` (a `finally`
  cannot see a variable declared inside it), which is §4.5's own fiddly half. It is a separately
  reviewable increment worth ~192 B on a two-capturing-defer function and nothing on the rest.
- **A shape §4 did not anticipate: a DEFERRED literal that defers on its own account.** Go scopes that
  inner defer to the literal; the old emission registered it into the enclosing function instead. Zero
  instances in the corpus, so it was a latent hole rather than a live defect, and the frame closed it.
- **Two C# scoping facts, verified by compiling rather than reasoning.** A lambda or local function MAY
  declare a local spelled like one in the enclosing method (the pre-C#-8 CS0136 rule does not fire), so
  every frame reads under the same name; a LABEL may not (CS0158), so the named-result exit label alone
  is depth-numbered.
- **`bodyWrappedInDeferContext` is now OPTIONAL and deliberately kept.** It forced the direct-`ж`
  receiver because a `ref T` receiver cannot be referenced from inside a lambda; an inline body removes
  that constraint. Kept because the direct-`ж` form is also the alloc-free, race-free one and changing
  receiver shapes corpus-wide is its own change. **Open simplification.**
- **Pre-existing, NOT this arc: the auto-sibling visit panic.** A `-stdlib` reconvert reports
  `visit file error: … nil pointer dereference` for `internal/godebug/godebug.go` and
  `internal/concurrent/hashtriemap.go`, skipping their `.cs.auto` REVIEW siblings only (production
  emission and package-wide state are unaffected — it is a separate re-visit pass). A/B'd against the
  master converter: identical. Belongs with CleanupBacklog item 18, which already owns `.cs.auto`
  staleness.

**The measured result.** `os.File.WriteString` — the row that named the 440 B term in the first place —
goes **2,736 → 2,368 B/call**, the same 368 B coming off `os.File.Write` and off the wrapper band that
contains `internal/poll.FD.Write`'s two defers. Per-shape: the execution context cost 160 B with no
defers and 248 B with one or two non-capturing ones; the frame costs **0**.

## The arc's original commission (user rulings 2026-08-05), and two tasks it queues

**The closure-emission frame design is APPROVED** ([`DESIGN-closure-emission.md`](DESIGN-closure-emission.md)
§4–§5): the execution-context lambda gives way to the `ref struct` frame with the body emitted inline in
`try`/`catch`/`finally`. The user's context, recorded because it shapes the work: the lambda form was
chosen for *visual* parity and was long suspected of a capture-semantics divergence class (the lambda
captures variables the original Go never captured); the frame form removes that class by construction and
the allocation cost was never weighed. One ruling amends the design:

- **Evaluate the NEED for the `deferǃ` bang-suffixed name and DROP the bang if possible.** It exists
  solely to disambiguate calls against the `defer`-named delegate parameter of the GoFunc lambda — a
  parameter the frame design eliminates. Go source can never declare identifiers named `defer`/`recover`
  (keyword/builtin), so with the lambda gone the collision source should be gone too; the arc verifies
  there is no other collision (golib surface, generated code) and documents the verdict either way. Same
  evaluation for any sibling bang-named member of the defer/recover family. (Symbols.cs constants, never
  the literal glyph.)

Arc mechanics: lands with its OWN corpus regen (post-r40 doctrine — the corpus stays level with its
converter; no new standing-drift era), full gate battery including the sweep (the banked alloc rows are
the design's own motivation), and per-stage checkpoint commits along §4.8's migration path.

**Queued task 1 — the documentation-reality pass (dedicated sub-agent, AFTER the arc lands).** The frame
changes every deferred function's emitted shape: `ConversionStrategies.md` and
`ConversionStrategies-Reference.md` examples, and any doc quoting the lambda form, must be brought to
match reality. Style ruling: present tense, educating a new reader — no history in the teaching docs;
posterity lives in the design doc.

**Queued task 2 — the `[GoTestMatchingConsoleOutput]` audit — CLOSED (r41b-outputattr, 2026-08-07).**
Before `core/fmt` was real, some behavioral tests skipped output-matching because the stub could not
format their output. Measured 2026-08-05: **14 projects** had `package main` but no attribute. Each was
run via `go run .` (5+ repetitions per project, comparing stdout/exit-code across runs) to classify as
GRADUATE (deterministic stdout, exit 0), DELIBERATE-SKIP (nondeterministic or panic/deadlock by design),
or FIXABLE-MISMATCH (deterministic Go output, but the transpiled C# currently diverges). **4 of 14
graduated** and are now output-compared (`run-behavioral.ps1 --filter <Name>`, all four phases green);
one attempted graduate uncovered a genuine converter bug and was left un-annotated, reported below as a
new board candidate:

| Project | Verdict | Reason |
|:--|:--|:--|
| ChannelReceiveFromNil | DELIBERATE-SKIP | `<-` on a nil channel — Go's deadlock detector fires (`fatal error: all goroutines are asleep - deadlock!`), zero stdout, exit code 2. The message carries a goroutine stack trace (addresses/line offsets); a managed re-implementation cannot be expected to reproduce it byte-for-byte, and there is nothing on stdout to compare regardless. |
| ChannelSendToClosed | DELIBERATE-SKIP | Ten goroutines race to send on / close the same buffered channel with no synchronization — a deliberately racy program. Repeated `go run` showed both the *count* of values printed before the panic (0 vs 10 observed) and *which* goroutine panics vary between runs; output is provably nondeterministic. |
| ChannelSendToNil | DELIBERATE-SKIP | `c <- v` on a nil channel — same deadlock-detector shape as ChannelReceiveFromNil (zero stdout, exit code 2, non-reproducible stack trace). |
| **DeferSimple** | **GRADUATED** | Deterministic 3-line stdout (`Open file` / `Write data to file` / `Close file`), exit 0, confirmed across 5 runs. `run-behavioral.ps1 --filter DeferSimple` — 4/4 phases PASS (48.2s). |
| ForVariants | DELIBERATE-SKIP | Spawns unsynchronized goroutines (`go fmt.Println(...)`) whose print ordering interleaves with the main goroutine's loop output. Two consecutive `go run` invocations produced different line orderings/content, confirming scheduler-dependent nondeterminism. |
| GoCallVariations | DELIBERATE-SKIP | Exercises ~8 different `go`-statement call shapes (bare func, closure, method value, function-returning-function, etc.) with no synchronization between them; two consecutive runs printed the same lines in different relative order — nondeterministic by design (that's the point of the test). |
| **InferredForeignTypeNoImport** | **GRADUATED** | Deterministic 2-line stdout (`true` / `5`), exit 0, confirmed across 5 runs. `run-behavioral.ps1 --filter InferredForeignTypeNoImport` — 4/4 phases PASS (19.6s). |
| **InterfaceInheritance** | **GRADUATED** | Deterministic 2-line stdout (two `map[:N :M]` lines — Go's `fmt` sorts map keys since 1.12, so the single-key-per-map output is stable), exit 0, confirmed across 5 runs. `run-behavioral.ps1 --filter InterfaceInheritance` — 4/4 phases PASS (15.5s), proving the transpiled map-print ordering matches too. |
| **PointerCastSliceRange** | **GRADUATED** | Deterministic single-line stdout (`6 100 11`), exit 0, confirmed across 5 runs. `run-behavioral.ps1 --filter PointerCastSliceRange` — 4/4 phases PASS (17.9s). |
| RangePointerArrayConversion | **FIXABLE-MISMATCH (new board candidate)** | Go's stdout is deterministic (`63`, exit 0, confirmed across 5 runs) — a graduate by the audit's own criterion — but the transpiled C# prints `0`. Root cause is visible in the emitted code: `for i, x := range (*[3]int)(p)` (`p := unsafe.Pointer(&a)`) converts to `foreach (var (i, x) in ((ж<array<nint>>)(uintptr)(p)).Value)` — the round-trip through `uintptr` cannot recover the original managed box `Ꮡa`, so the cast yields a fresh/default `array<nint>` and the loop sums over zero elements instead of `{10,20,30}`. This is the same "unsafe.Pointer reinterpret via raw address" limitation already load-bearing in the neighboring `UnsafePointerReinterpret` test's own design comment (that test deliberately stays Compile+Target-only for exactly this reason). **The attribute was NOT added** — adding it would redden the Output phase — so this project is left exactly as measured (no diff). Candidate fix belongs with whichever arc next touches unsafe.Pointer reinterpret-cast codegen (see `ж<T>`/`Ꮡ` boxing notes); until then this stays a known, deliberate non-graduate for a different reason than the other nine (a real bug, not an inherent nondeterminism). |
| SelectStatement | DELIBERATE-SKIP | Go's `select` deliberately pseudo-randomizes among multiple ready cases. Two consecutive runs showed different orderings/values (`OK: true -- got: 12` at a different line position; final tuple `17 -5 12 3` vs `3 17 20 -5`) — confirmed nondeterministic. |
| StructWithPointer | DELIBERATE-SKIP | stdout embeds a raw pointer address (`Value of red = {2 red 0xc0...}`); 5 repeated runs showed two distinct addresses (`0xc000028180`, `0xc00010a150`) recurring at random. A memory address can never be expected to match between the Go runtime's allocator and the CLR's, so this can never be a stable golden regardless of transpile correctness. |
| TypeConversionReturnType | DELIBERATE-SKIP | Same shape as StructWithPointer — stdout embeds two raw pointer addresses (`{Go 0xc0... 0xc0... map[]}`) that varied across all 5 runs. Not stable in Go itself, so not a candidate for a byte-exact golden. |
| UnsafePointerReinterpret | DELIBERATE-SKIP | Explicitly documented in its own source comment as "a Compile + Target (golden byte-comparison) test, NOT an output-comparison test" — it uses `println` (Go builtin, writes to stderr) rather than `fmt.Println`, and exercises the same raw-address unsafe.Pointer reinterpret limitation that RangePointerArrayConversion's mismatch surfaces at runtime. |

**Net: 4 graduated (DeferSimple, InferredForeignTypeNoImport, InterfaceInheritance,
PointerCastSliceRange), 9 deliberate-skips (documented above, each for a distinct concrete reason —
deadlock detection, goroutine-scheduling nondeterminism, select randomization, or raw pointer-address
non-reproducibility), 1 new FIXABLE-MISMATCH board candidate (RangePointerArrayConversion — unsafe.Pointer
reinterpret-cast through a raw `uintptr` round-trip loses the original managed box).** Change footprint:
4 one-line `[GoTestMatchingConsoleOutput]` additions to `package_info.cs` + the UpdateTestTargets-generated
`OutputComparisonTests.cs` block (4 new `Check<Name>()` methods) — no golden re-baselining needed (no
emission changed), no `go2cs.slnx` registration changes (all 14 projects were already registered).

## The `-tests` reference-closure family — CLOSED (2026-07-27)

`DisableTransitiveProjectReferences=true` means the generated test project lists only the imports
the converter computed, so any package named by a type the test code merely *touches* is missing
and the build fails with **CS0012**. `crypto/hmac` was the first case solved (interface embedding);
the closure is now **generalized to the declaration edges of the types the compilation names**
(`declarationClosureImports`), covering both an interface's bases and a struct's field types. Full
rule, minimality gates and guards: `docs/ConversionStrategies-Reference.md`, *Reference closure (the
declaration-edge rule)*.

| Package | Missing type | Outcome |
|:--|:--|:--|
| `image/draw` | `rand_package.Rand` | **build unblocked** — a `struct` field of `quick.Config` reached at an element-bearing composite literal. Now **validated 9/9** (2026-07-31), once the two runtime defects below were fixed. |
| `io` | `io_package.Writer` | **NOT a closure defect** — see the next section. Adding the reference cannot fix it. |

**Minimality is the hard part, and it is measured, not asserted.** Regenerating every banked
package's `.tests.csproj` and diffing is the instrument, and it rejected three looser rules before
the landed one. Seeding from *every* file rather than the compiled ones drifted `compress/gzip`
(context, crypto/tls, mime/multipart, net/http, net/url — reached through `http.Request`'s fields,
from a Phase-4D-excluded `example_test.go` that is never compiled) and `go/token` (go/ast); firing
the struct edge on any *value use* drifted eleven more (`sync.Once`, `sync.Map`, `reflect.Value`);
firing it on an *unscoped empty* literal still drifted three (mime, testing/quick,
encoding/binary), because an empty Go literal converts to `new Δsync.Once(nil)` — go2cs-gen's nil
constructor, which names no field, and whose FIELDWISE overload is `internal` and so not even a
candidate outside the declaring assembly. Each of those gates drifts **zero** banked packages. The
one edge that is deliberately not zero is the **root-scoped** empty literal, re-measured at the
63-package roster on 2026-07-31: it changes exactly one project by exactly one line
(`math/rand/v2` gains `internal.chacha8rand.csproj`) — the root set itself, with all three
foreign-struct negatives byte-identical.

⚠ **Run that probe with the converter's exit status checked.** A conversion that *fails* writes no
csproj, so an ignored failure reads exactly like "no drift" — a false-clean of the same family as
charter §9's false-green traps. That is how a real defect in the first cut hid through three
measurement rounds: a struct literal declared in the EXTERNAL test variant reached
`reach(<pkg>_test)`, a synthetic path that resolves to no importable package, and every affected
package died with F14b's `resolve test project dependency "bytes_test": package bytes_test is not
in std` — silently, until the validated sweep failed on `bytes` at the second package.

## `io` — duplicate-type build blocker CLOSED (landed on master 2026-07-31); runtime blockers remain

The diagnosis was correct: recompiling `io` into its mixed internal/external test assembly created a second `io_package.Writer`, distinct from the one named by `hash.Hash`, `bytes`, `fmt`, and the rest of the referenced closure. The general fix is the new **`whitebox-reference`** test-project model. A production package with build-selected same-package tests conditionally grants friend access to `<assembly>.tests`; internal `_test.go` declarations emit into `<name>_internal_test_package`; external references to those declarations route to the bridge by `go/types.Object` identity; and test-contributed adapters live in the test metadata anchor. Production remains the only identity for its types. Records that truly require a production-type mutation still fall back to `recompile`.

Fresh `io` conversion now emits `testProjectModel: whitebox-reference`, references `io.csproj`, compiles no production `.cs` into `io.tests`, and builds with **0 errors**. The host runs all **54** included test functions.

⚠ **2026-07-31 (reflection chip): the 0-errors claim had silently regressed on landed master** — a
fresh conversion produced CS1503 ×20: `emittedAdapterPair`'s bare-cast fallback resolved io_test's
own `Buffer` to the first same-simple-name record in order, the FOREIGN `bytes_package.Buffer`
(`bytes_BufferжReader(rb)`), while the generator names the anchor-local record's adapter bare
(`BufferжReader`). Both A/B binaries (`8d55344cc` landing, `f73d62d71`) emit the same broken
pairing, so the recorded 45/54 was measured with an intermediate, not the final, binary — the §9
mixed-vintage lesson in the wild. Fixed in the chip's landing (anchor-local records win the
dotless fallback; exact-key matching is a full first pass; `anchoredAdapterMemberName` composes
bare for anchor-local records — guard `TestBareCastPrefersAnchorLocalRecordOverForeignSimpleNameMatch`).

With that repaired and the chip's `runtime.Callers`/`Frames.Next` managed traceback landed, the
host reached **47 pass / 54** (superseded 2026-08-01 — see the closing paragraph of this section);
the remaining seven top-level verdicts were separate runtime/semantic roots:

- ~~`TestMultiReaderFlatten` and `TestMultiWriterSingleChainFlatten`: `runtime.getcallersp`~~ —
  **CLOSED 2026-07-31 by the reflection Phase-3 chip (increment 4)**: `runtime.Callers` +
  `Frames.Next` hand-owned over a Go-logical managed stack projection; `getcallersp` stays an
  honest stub (see DESIGN-reflection-bridge.md and the ConversionStrategies-Reference section).
- `TestOffsetWriter_Seek`, `TestOffsetWriter_WriteAt`, `TestWriteAt_PositionPriorToBase`, plus `TestOffsetWriter_Write` subtests: `os.runtime_rand` is unimplemented in the tempfile path — owned by the `os` operational arc.
- ~~`TestMultiWriter_StringCheckCall`: `WriteString` forwarding behavior mismatch~~ — **CLOSED
  2026-08-01, and it was NOT a forwarding bug**: the emitted `multiWriter.WriteString` performs
  `w._<StringWriter>(ᐧ)` exactly as Go does. The assertion MISSED because golib's Go-method-set
  probe compares EMITTED C# names, and `-tests` B9 Δ-renames the test-file declarator
  `func (c *writeStringChecker) WriteString` to `ΔWriteString` (the bare name would hijack the
  dot-imported `io.WriteString` at every unqualified call site — C# resolves the enclosing class's
  method group ahead of `using static`). No `GoImplement` record exists for the pair either, by
  design since the structural recorders were retired, so the runtime shell tier was the only
  resolver and its gate said MISS. Fixed in `golib` — `TypeExtensions.GoMethodNameMatches` projects
  a leading `ShadowVarMarker` away as a SECOND pass, after an exact-name pass finds nothing, and
  `AdapterBinder.ResolveReceiverMethods` applies the same rule so binder and probe cannot disagree.
  Proven by A/B before the fix: renaming only the emitted method (and qualifying the three call
  sites the bare name would hijack) turns the test green with no other change. Full rule:
  `docs/ConversionStrategies-Reference.md`, *A candidate's EMITTED name is not always its GO name*.
  ⚠ The CLASS is open, not just this instance: any `-tests` Δ-renamed method that is also an
  interface member asserted at run time failed the same silent way, and the failure mode is
  valid-but-degraded (`MultiWriter` fell through to `Write`, which returns the same `(n, err)`).
- ~~`TestMultiWriter_WriteStringSingleAlloc` and `TestPipeAllocations`: exact allocation-profile
  assertions; no disclosure ruling has been made~~ — **RULED 2026-08-01: both are
  `alloc-count-semantics` disclosures** in io's hand-owned `go2cs_test_disclosures.json`, the class
  `strings` already established. Neither is an allocation-profile divergence; both are the UNIT
  mismatch the shim discloses by design — `testing.AllocsPerRun` counts mallocs in Go and allocated
  BYTES on the CLR, so a nonzero-count assert can never agree whatever the allocation behavior.
  Measured before disclosing, which is the point of the order: `num allocations = 406-407; want 1`
  and `too many allocations for io.Pipe() call: 1184.000000` (want ≤ 4) — bytes in both cases.
  Signature-pinned on `"num allocations = "` and
  `"too many allocations for io.Pipe() call: "`, so any OTHER failure of either test stays a strict
  mismatch.

With the two above settled, the host reaches **48 pass / 54 · 2 disclosed · 4 os-blocked**, and the
`os` `runtime_rand` row is the whole of what stands between `io` and a bank. Every remaining verdict
has a named owner and must be handled by that arc rather than folded into this item.

**BANKED 2026-08-01 (r32 train): `io` validates — 59 matching · 2 disclosed (alloc-count-semantics).**
The `os.runtime_rand` hand-own landed with the os-roots lane and the four OffsetWriter tests pass; the
probe fix and the disclosures above did the rest. One standing footprint note: the satisfies-but-never-
witnessed recorder (r32's converter increment) adds 2 `GoImplement` records to `io`'s production
`package_info.cs` on every `-tests` regeneration; the committed file predates the recorder and is
deliberately NOT rebanked (charter: no partial rebanks), so sweeps show that +2 as expected drift —
restore, don't chase — until the whole-corpus regen levels it, along with the rest of the increment's
measured 34-file footprint.

## `context` — five converter roots closed; 36 of 38 match; two rooted failures remain (2026-08-02)

Attempted after the wave3 channel semantics were ground-truthed. **The channels are not the problem
and never appear in this census** — `context`'s suite is the stdlib's sharpest select/cancellation
exerciser (100-node cancellation trees, interlocked cancels, closed-channel `Done()` broadcast,
`AfterFunc` registration races) and every one of those tests **passes**. That is a strong independent
confirmation of the wave3 landing, and the single most useful thing this arc measured.

Five converter roots stood between the package and a run; all five are fixed and documented in
[`ConversionStrategies-Reference.md`](../ConversionStrategies-Reference.md):

| # | First diagnostic | Root | Layer |
|--:|:--|:--|:--|
| 1 | `CS1003`/`CS1026`/`CS1513` ×195 in `x_test.cs` | a func literal inside a `for … range` composite literal emits its capture snapshot — a STATEMENT — into the element position; `visitRangeStmt` provided no pre-statement hoist sink (the fourth statement kind to need one) | converter |
| 2 | `CS0051` ×4 — `testingT` less accessible than `XTestParentFinishesChild` | `visitTypeSpec` asked the `testInlineTypeAccess` arm FIRST, so it decided the modifier's VALUE from the name and discarded the publicization signal | converter |
| 3 | `CS8030` — anonymous function converted to a void-returning delegate | a returned FUNC LITERAL is typeless in C#, which `allExecWrapperReturnsAreTypeless` (written for `nil`/constants) did not count | converter |
| 4 | `CS1929` — `timerCtx` has no `Done`, best overload wants `ж<afterFuncContext>` | the internal bridge re-recorded a production↔production pointer pair production already implements, minting a DUPLICATE adapter whose members resolved in the test class's scope (and whose `cancel` was an EMPTY body) | converter |
| 5 | `CS8917` + `CS8130` in `example_test.cs` | a func literal returned inside another literal has no natural type, so the enclosing lambda has none either — the sibling of `lambdaConstReturnCastType` | converter |

Root 1 is guarded by the `RangeExprFuncLitCapture` behavioral test (its A/B reproduces the cascade);
roots 2–5 are `-tests`-only shapes with no behavioral-corpus expression, so `context`'s own banked
suite is their guard when it banks.

**`T.Deadline` was ALSO still capability-blocked, and that was pure staleness.** The member landed with
the one-tree consolidation (`core/testing/testing.cs` `Deadline` + `TestHost.PackageDeadlineUtc`) but
`supportedTestCapabilities()` was never widened, so six of context's tests — `TestDeadline`,
`TestTimeout`, `TestSimultaneousCancels`, `TestInterlockedCancels`, `TestLayersCancel`,
`TestLayersTimeout`, i.e. the whole tree-cancellation family — were excluded rather than run. Widened,
with the charter §9 roster scan done first (positive control `context/x_test.go:50` + `net/net_test.go:78`
both fire): the only validated package whose `_test.go` calls it is `os/signal`, and both of its call
sites are in `//go:build unix` files this platform never builds. All six now run and **pass**.

**Census after all six changes: 38 top-level verdicts, 36 pass, 2 fail.** The two failures are rooted
and owned elsewhere:

| Test | Root | Owner |
|:--|:--|:--|
| `TestValues` | `internal/reflectlite`'s `rtype.String()` is the literal Go conversion — `t.nameOff(t.Str).Name()` over a type-descriptor name offset the managed bridge never populates — so it returns `""`. `reflect`'s equivalent is hand-owned over `GoReflect.GoTypeName` (`type.cs:517` placeholder); reflectlite's mini-bridge only ever landed `Len`/`Swapper`. Symptom: `context.Background.WithValue(, c1k1)` where Go prints `WithValue(context_test.key1, c1k1)` — the `stringify` fallback arm for a key with no `String()` method. | reflection-bridge arc |
| `TestAllocs` | `testing.AllocsPerRun` unit mismatch, the established `alloc-count-semantics` class (io, strings, bytes). MEASURED before ruling: `Background() allocs = 128.000000 want 0`, `WithValue = 754 want 3`, `WithTimeout(1ns) = 3744 want 12`, `WithCancel = 2104 want 5`, `WithTimeout(5ms) = 4876 want 8` — bytes in every case, so no allocation behavior can satisfy a count assert. A signature-pinned disclosure is warranted; it is deliberately NOT written here, since a disclosure manifest belongs with the banking commit that verifies it end to end. | context's banking arc |

So `context` is **one reflectlite member plus one disclosure away from banking**, with nothing
context-local left. Note the reflectlite gap is not context-specific: any package whose code path
reaches `reflectlite.TypeOf(x).String()` gets an empty string today, silently.

## Build-blocked, each its own root

| Package | First diagnostic | Note |
|:--|:--|:--|
| ~~`image/jpeg`~~ | ~~`CS0111: … already defines a member called 'init'`~~ | **DONE 2026-07-31 — 14/14, banked. NO converter change was needed** — the diagnostic was stale by the time the row was written. The converter has always uniquified multiple package `init`s from a package-scoped counter (`init`, `initΔ1`, … in `visitFuncDecl.go`), and jpeg's production pair (`reader.go` + `writer.go`) emits correctly. The collision was between PRODUCTION's `init` and INTERNAL test file `dct_test.go`'s, which the recompile model put in the same `jpeg_package`; the **whitebox-reference** model emits internal test declarations into `<pkg>_internal_test_package`, so it cannot form. A corpus scan finds **12** packages with both a production and a test `init` (`flag`, `net`, `os`, `runtime`, `sync`, `testing`, `time`, `crypto/x509`, `image/jpeg`, `net/http`, `os/signal`, `os/user`); every one takes a reference model. The recompile FALLBACK (`recordsRequireProductionMutation`) would still collide — latent, reachable by no package today, deliberately not fixed speculatively. Cross-file multi-`init` is now guarded by the `MultiFileInitOrder` behavioral test (five inits across three files, order-compared vs `go run`); `Solitaire` already covered two in one file. |
| ~~`index/suffixarray`~~ | ~~`CS0206: A non ref-returning property or indexer may not be used as an out or ref value`~~ | **DONE 2026-07-31 — 12/12, banked.** TWO go2cs-gen defects, stacked, both general. `suffixarray_test.go` declares `type index Index` — a defined type over the production struct — and Go gives it `Index`'s field set. (1) `GetStructDeclaration` resolves an underlying struct only from SOURCE, and a real MSBuild `<ProjectReference>` arrives as compiled METADATA, so under the white-box model NO members were forwarded and every `x.sa`/`x.data` was CS1061; a symbol-based fallback now resolves it, forwarding what `IsSymbolAccessibleWithin` permits — Go's exported/unexported rule projected into C#. (2) The forward was a get/set property, i.e. a VALUE, so `x.sa.len()` (a `this ref` receiver) and `&x.sa` could not bind — this row's original CS0206. It is now an `[UnscopedRef]` REF-returning property, a strict superset. Fixing (1) alone collapsed the CS1061 wall onto exactly the CS0206 recorded here: root-cause layering, the first diagnostic moving rather than clearing. Full rule: `docs/ConversionStrategies-Reference.md`, *The forwarded member must be a VARIABLE, and the underlying may be METADATA-ONLY*; guarded by the `DefinedTypeOverForeignStruct` behavioral test (whose A/B reproduces CS1061 and CS0206 separately). ⚠ `TestNew{32,64}/exhaustive3` run ~35 min in C# vs 12.4 s in Go — a performance gap, not a correctness one; `run-validated-sweep.ps1` gives the package a 60m deadline. |
| ~~`internal/zstd`~~ | ~~`CS1929: 'testing_package.B' … 'Cleanup'`~~ | **DONE 2026-07-27 — 534/534, banked.** The `common` members are on `core/testing`'s `B`; see the retraction below. |
| ~~`crypto/md5`~~ | ~~`CS0030: Cannot convert type 'System.Type' to 'uint'`~~ | **DONE 2026-07-31 — 11/11 (1 alloc-profile disclosure), banked.** TWO defects, both general. `unsafe.Alignof`/`Offsetof` built their `System.Type` argument by splitting the CONVERTED C# text on `.` as though it were a Go field selector, so `unsafe.Alignof(uint32(0))` emitted `(uint32)0.GetType()` — which C# parses as `(uint32)(0.GetType())`. Both now resolve the operand through `go/types` and emit `typeof(T)`. Behind it stood a second: `buf := buf` in `benchmarkSize` reads a package-level `buf` declared in `md5_test.go`, and the shadowed-global qualifier named the PRODUCTION class (`md5_package.buf`, CS0117) rather than the white-box bridge class that actually declares it. |
| ~~`path/filepath`~~ | ~~`CS0103: The name 'ßÅælstat' does not exist`~~ | **Build blocker CLOSED 2026-07-31; `FindFirstFile` host-killer CLOSED 2026-08-01; BANKED 2026-08-01 (r32 train) at 61 matching — see below.** |
| ~~`net`~~ | ~~`CS1031: Type expected`~~ | **Syntax cascade CLOSED 2026-07-31 — see below. Still does not compile: 94 SEMANTIC errors stood behind it.** |

### `path/filepath` — build blocker closed; the FindFirstFile root closed; 46 of 61 match; two runtime roots remain

The name was never mangled. `ßÅæ` is the bytes `E1 8F 91` rendered in **cp437** — the UTF-8 encoding
of `U+13D1 Ꮡ`, the `AddressPrefix` marker. The missing symbol is `Ꮡlstat`, the heap box for
`path.go`'s `var lstat = os.Lstat // for testing`, whose address `export_test.go` takes
(`var LstatP = &lstat`, the hook that lets a test swap the implementation `Walk` calls).
`go/packages` excludes `_test.go` from a production package, so the production emission never saw the
address-taking and left `lstat` a plain field, while the test variant emitted `Ꮡlstat`. Fixed
generally: the converter now scans the build-selected in-package `_test.go` half for addressed
globals and folds them into `packageAddressedGlobals` — in ordinary and `-tests` conversion alike, so
production storage shape stays mode-stable. Rule, the three properties that keep it safe, and the
`SiblingTestAddressedGlobal` guard: [`ConversionStrategies-Reference.md`](../ConversionStrategies-Reference.md),
*A global addressed only by the package's own `_test.go` is still heap-boxed*.

Its reach is wider than filepath. A whole-stdlib A/B put the footprint at **13 globals in 13 files,
every one a Go "for testing" hook** and no false positives: `os`'s `lstat` /
`testingForceReadDirLstat` / `allowReadDirFileID`, `runtime`'s `readRandomFailed` / `useAeshash` /
`doubleCheckReadMemStats` / `casgstatusAlwaysTrack` / `forcegcperiod` / `timeBeginPeriodRetValue`,
`reflect`'s `callGC`, `internal/poll`'s `logInitFD`, `net/http`'s `maxWriteWaitBeforeConnReuse` and
`testHookEnterRoundTrip`, and `time`'s `usPacific`. Those are exactly the hooks `os`, `runtime`,
`reflect`, `net/http`, `internal/poll` and `time` need aliasing real storage before their own suites
can pass — so this is prerequisite work already banked for six future arcs, not filepath-local cost.

filepath now **builds with 0 errors and the host runs**. Root 2 below is **closed (2026-08-01)**, and
closing it is what lets the host survive a whole-suite run — so the numbers no longer have to be
gathered per test. Measured in ONE `-tests -test-action all -test-timeout 10m` run: **46 of 61
match** (C# 40 `pass` + 6 `skip` against Go's 41 `pass` + 20 `skip`), with **zero empty verdicts**.
Every one of the 15 remaining mismatches reaches one of the two roots that are left — 14 the
symlink-privilege one, 1 the `gogetenv` one — and none is a marshalling failure:

| Root | Reached via | Note |
|:--|:--|:--|
| `os.runtime_rand` unimplemented | `os.MkdirTemp` → `nextRandom` → `testenv.MustHaveSymlink` / `initWinHasSymlink` | The **same root the `io` row names** — owned by the `os` operational arc. Go *skips* these tests for want of symlink privilege; C# infrastructure-errors before `testenv` can decide, so clearing this likely converts most of them to matching **skips** rather than passes. |
| ~~Win32 `FindFirstFile` struct marshalling~~ | `EvalSymlinks` → `toNorm` → `normBase` → `syscall.FindFirstFile` | **CLOSED 2026-08-01.** `findFirstFile1` handed `(uintptr)new @unsafe.Pointer(Ꮡdata)` to the raw `Syscall`, and the kernel wrote a 592-byte `WIN32_FIND_DATAW` over a C# struct whose `[MAX_PATH]uint16` field is an `array<uint16>` — an 8-byte **managed reference**, not inline storage. The write clobbered that reference, so the next read was an `IndexOutOfRangeException` in `PinnedBuffer` or a hard **AccessViolation (0xC0000005)** that killed the host. Fixed as the third member of the struct-passing class below: `findFirstFile1`/`findNextFile1` are hand-owned against a blittable mirror in `syscall/zsyscall_windows_impl.cs`, guarded value-level by the `FindFirstFileData` behavioral output test. `TestDriveLetterInEvalSymlinks` — the crash site — and `TestEvalSymlinksCanonicalNames`, `TestToNorm`, `TestGlob`/`TestWindowsGlob`/`TestGlobUNC`, `TestWalk`/`TestWalkDir` all now match Go. |
| `runtime.gogetenv` — `fatal error: getenv before env init` | `testenv.GOROOT` → `runtime.GOROOT` | `runtime.envs` is never populated (Go fills it in `goenvs` during scheduler init); `throw` then re-faults on the unimplemented `getcallerpc`. Only `TestBug3486` here, but it gates every `testenv.GOROOT` consumer. |

⚠ **Resolved with root 2, and worth remembering as a shape.** While that AccessViolation stood, one
full-suite run **under-reported badly**: the host died mid-`TestDriveLetterInEvalSymlinks` and every
later verdict read `C#=""`, which presents as a mass infrastructure wall rather than as one crash —
so the package had to be bucketed per test. A single host-killing defect will do this to any package;
the tell is a run whose empty verdicts all fall AFTER one particular test. `filepath`'s whole-suite
run now has zero empty verdicts, so per-test bucketing is no longer needed here.

The remaining 15 split cleanly by root. Fourteen are the symlink-privilege family — Go's
`testenv.MustHaveSymlink` **skips** them for want of `SeCreateSymbolicLinkPrivilege`, while C# never
reaches that decision: 3 die in `os.MkdirTemp` → `runtime_rand` first, 9 go on to attempt the symlink
and `fail` on the privilege, and 2 infrastructure-error on the consequences of having attempted it
(`TestNTNamespaceSymlink`'s `mklink`, `TestWalkDirectoryJunction`'s cleanup `UnauthorizedAccessException`
over the junction it created): `TestEvalSymlinks`, `TestEvalSymlinksAboveRoot`,
`TestEvalSymlinksAboveRootChdir`, `TestEvalSymlinksIsNotExist`, `TestEvalSymlinksTooManyLinks`,
`TestGlobSymlink`, `TestIssue13582`, `TestNTNamespaceSymlink`, `TestRelativeSymlinkToAbsolute`,
`TestWalkDirectoryJunction`, `TestWalkDirectorySymlink`, `TestWalkSymlink`, `TestWalkSymlinkRoot`,
`TestWindowsEvalSymlinks`. The fifteenth is `TestBug3486` (`getcallerpc` after the `gogetenv`
`throw`). Clearing root 1 should convert most of the fourteen to matching **skips**, exactly as
predicted — so filepath's remaining distance is entirely `os`/`runtime` work, with nothing
filepath-local left.

**BANKED 2026-08-01 (r32 train): `path/filepath` validates — 61 matching, 20 of them
privilege-gated skips agreeing with Go's.** The os-roots lane landed both remaining roots
(`os.runtime_rand` → the fourteen become matching skips; `runtime.envs` → `gogetenv` works), and
`TestBug3486` took one ruling on top: `runtime.GOROOT()` has no linker-baked `defaultGOROOT` in a
converted assembly, so the **pipeline now exports `GOROOT` to both children** (`go test` and the C#
host — user-ruled 2026-08-01, the run-time-export option over baking a machine path into committed
host metadata; `testConversion.go`'s `runCommandWithTimeout`). One FOURTH root surfaced only on the
merged tree — charter §9 layering: with the tempfile and mirror fixes in, `TestNTNamespaceSymlink`
got far enough to create its junction-to-a-volume-root and then `t.TempDir()` cleanup died
(`UnauthorizedAccessException`), because the host delegated to .NET's `Directory.Delete(recursive)`,
which opens some junction targets during its walk. Go's cleanup is `os.RemoveAll`, which removes a
reparse point AS THE LINK. `core/testing`'s `TempDir` now walks with exactly those semantics
(reparse points deleted as links, never traversed; read-only cleared and retried) — general for
every future junction/symlink-creating suite, `os`'s own first among them.

### `net` — syntax cascade closed; 94 semantic errors remain

`CS1031` was one defect with a ~90-error blast radius, and it was not about `net` at all: the
anonymous-struct lift probe descended exactly **one** level of the declared type, so `[]struct{…}`
lifted and `[]*struct{…}` did not. `ip_test.go`'s `var ipStringTests = []*struct{ in IP; str string;
byt []byte; error }{…}` therefore emitted raw Go type text into the C# declaration. (The shape had
stayed hidden because a composed occurrence still resolves when some *other* declaration registered
the identical signature first; the embedded `error` makes this signature unique.) The probe is now a
recursive descent over the type-composing syntax — pointer, array/slice element, `...T`, parens, map
value then key, channel element — shared by the struct and interface extractors, and the separate
one-off map-value probe it subsumes was deleted. Rule + the `AnonStructComposedTypes` guard:
[`ConversionStrategies-Reference.md`](../ConversionStrategies-Reference.md), *An anonymous struct
lifts from ANY depth of its declared type*. **Zero syntax errors remain in `net`** — no
CS1031/CS1003/CS1519/CS1002/CS1513.

`net` still does not compile. What the cascade was hiding, bucketed — charter §9's layering lesson in
its purest form, since Roslyn skips method-body binding while declaration errors stand:

| Count | Code | Root |
|--:|:--|:--|
| 52 | CS0426 | `The type name 'ConnᴠReader' does not exist in the type 'net_test_package'`. The ᴠ value-adapter for a **production↔production** pair (`net.Conn` → `io.Reader`) is generated into the PRODUCTION class, but an external-test use site qualifies it with the TEST class. One root, in test-project-model record anchoring (`splitExternalVariantRecords`); 55% of all remaining errors. |
| 14 | CS1929 | Two shapes: `core/testing`'s `T` declares no `Deadline` (so a same-named `contextWithNonZeroDeadline` extension is offered instead), and `socktest.Switch` methods want a `ж<Switch>` receiver where a value is supplied. |
| 6 | CS8130 | deconstruction of a result whose type did not bind |
| 4 each | CS1061 / CS8183 / CS8917 | member lookup, `var`-in-deconstruction inference, delegate-type inference |
| 2 each | CS1501 / CS1503 / CS0029 / CS8934 | arity; `ж<AddrError>` → `error`; a `(ctx, cancel)` tuple assigned to `Context`; lambda return type |

Rooting those is the next `net` increment. Note `net`'s own init gap (the `sync.OnceFunc` nil panic at
`fd_windows.cs:27`) sits behind all of it, and the Tier-0 channel/rendezvous frog behind that — so
compiling is the realistic near-term goal, not validating.

#### Revised 2026-07-31 — six of those seven roots are fixed; ONE architectural blocker remains

Re-measured on a converter carrying the r27 adapter-resolver chip: **46 unique errors** (the "94" above
counts each twice — MSBuild reports every error once per pass). Six roots landed, each a general fix
at its own layer; the count after each, in order:

| # | Root | Layer | Errors after |
|--:|:--|:--|--:|
| — | *(start)* | | 46 |
| 1 | A white-box **production** type is FOREIGN to go2cs-gen, so the interface-sourced adapter name must carry the package prefix — the carve-out the *value* arm already had (`whiteboxProductionTarget`) | converter | 17 |
| 2 | A pointer-receiver **method value** binds the address in **assignment** context too (`poll.CloseFunc = sw.Closesocket`) — the value-context arm already did | converter | 11 |
| 3 | `&x.(*T).field` — a **type-assertion** base is a pointer rvalue, so it field-refs the box instead of copy-boxing | converter | 11 |
| 4 | A literal whose **every** return arm is untyped `nil` states its return type (the single-result twin of the multi-result rule) | converter | 11 |
| 5 | `goǃ` gains the `Func<…, TResult>` twins `deferǃ` always had — `go f(…)` discards results for **any** `f`, including a func literal with a named result | golib | 11 |
| 6 | `var a, b = f()` gates on `identHasHeapBox`, not the blanket `identEscapesHeap` flag — every tuple with an interface or func result was falling back to the broken per-name path | converter | 3 |
| 7 | The elided **pointer** element composite (`[]*struct{…}{{…}}`) routes its interface fields, like both sibling composite paths | converter | **2** |

Rows 3–5 cleared together on the same measurement (2–4 were independent roots whose sites overlapped
in the same three files). Every one is documented in
[`ConversionStrategies-Reference.md`](../ConversionStrategies-Reference.md); behavioral CNR is
byte-identical across all 517 projects for the whole set, which is the expected shape — five of the six
converter roots are reachable only from Go that the behavioral corpus does not contain, and two only
under `-tests`.

The original bucketing held up well with one correction worth recording: row 1's mechanism was **not**
`splitExternalVariantRecords` and not an anchor split. Both sides agreed on the anchor all along — the
record lands in `package_test_info.cs` and the class is generated into the test metadata class — and
only the *simple name* disagreed, because the converter asks "is the source type in another **Go
package**?" where the generator asks "is it in another **assembly**?" Under the white-box model those
differ for exactly one set of types. The board's guess named the right file and the wrong seam; the
diagnostic (`does not exist in the type 'net_test_package'`) reads like an anchor problem and is not
one.

##### ~~The remaining blocker: `testing.T.Deadline` needs a type `core/testing` cannot name~~ — CLOSED 2026-08-01, option (d)

> **CLOSED.** The blocker was never about `Deadline`; it was about there being two `go.time_package`
> declarations on disk. On 2026-08-01 the stub baseline retired and the converted standard library moved
> into `src/core` (commit `2e8066da6`), so `core/testing` simply references `core\time` like any other
> consumer — the answer none of (a), (b) or (c) below could be, because it removes the *premise* rather
> than working around it. Call it **option (d): there is one `time`.**
>
> `testing.T.Deadline()` now returns a real `(time.Time, bool)`, reporting the instant the package
> deadline (`-timeout`) expires — see `src/core/testing/testing.cs` and `TestHost.PackageDeadlineUtc`.
> `DisableTransitiveProjectReferences` is not a problem here after all: the host is a FIXED reference of
> every generated test project, so `time` arrives through it directly.
>
> Everything below is the record of the blocker as it stood. The footprint table still says which
> packages the member unblocks.

Both remaining errors are `t.Deadline()` (`net_test.go:78`, `dial_test.go:391`). Go's signature is
`func (t *T) Deadline() (deadline time.Time, ok bool)`, and net uses the result as a real `time.Time`
(`deadline.Add(-time.Until(deadline)/10)`, `td.Add(-arbitraryCleanupMargin)`) — so no primitive or
golib stand-in can satisfy it.

`core/testing` is hand-owned and, per the F15b one-testing-package ruling, is bound by **every** test
host by path (`$(go2csPath)core\testing\testing.csproj`). It references only `golib` and the analyzer
today, and that is not an oversight — its whole public surface (`TB`, `T`, `B`, `F`) is expressible in
primitives and golib types. `Deadline` is the first member that needs a converted stdlib type, and
neither candidate works:

- **`core/time`** — collides. Every `.tests.csproj` already references `go-src-converted\time`, and
  both assemblies declare `go.time_package`, so a project seeing both gets CS0433 on every use.
- **`go-src-converted/time`** — inverts the layering `core` ↔ `go-src-converted` is built on, and
  drags the converted tree into `go2cs.slnx` (which registers `core/testing`).

Note `DisableTransitiveProjectReferences=true` on the test projects makes this worse, not better: the
reference would not flow, and a `core/testing` API mentioning `time.Time` would then be **CS0012** at
every consumer — the reference-closure family again.

Three ways out, none of them a converter fix, all of them a decision above a single package's arc:
(a) parameterize `core/testing`'s `time` reference per consumer (MSBuild `AdditionalProperties` on the
ProjectReference — works, but makes the one testing package polymorphic in its dependency and touches
every generated `.tests.csproj`); (b) promote `time` to a position both trees share, the way `golib`
already is; (c) rule that `testing`'s time-typed surface is out of scope and accept that packages using
it cannot compile their suites. **Owed to a ruling, not to this arc.**

*The ruling came as **(d)**: retire the second tree entirely (2026-08-01). (b) was the closest guess —
it just turned out the position `time` needed to share was the one `golib` already had, and moving ONE
package there would have left the same seam for the next member that needed a converted type.*

Footprint, so the ruling is sized rather than guessed. Scanning GOROOT `_test.go` for a *testing*
receiver (`\b(t|b|tb)\.Deadline\(\)`, positive control `net/net_test.go:78`) and dropping what this
platform and this campaign never build:

| Package | Note |
|:--|:--|
| `net` | this row |
| `net/http`, `net/http/httputil` | 4 sites |
| `os/exec` | 1 site |
| `runtime/pprof` | 1 site |
| `context` (`x_test.go`) | 1 site |
| ~~`os/signal`~~ | 7 sites, all in `//go:build unix` files — **never built on Windows**, which is how `os/signal` banks at 1 today while carrying the call |
| ~~`internal/poll`~~ | `splice_linux_test.go` only |
| ~~`cmd/go`, `cmd/cgo/...`~~ | not stdlib validation targets |

So **six** packages, not the wider set a naive `.Deadline()` grep suggests (that one also catches
`context.Context.Deadline`). The `os/signal` row is worth keeping visible: it is exactly the shape of
counterexample that would look like it disproves this blocker, and does not.

**`net` state: 2 errors, one root, no converter work left in it.** Everything the r27 lane bucketed is
closed. When the ruling lands, `net` should compile on the next run — and the init gap (`sync.OnceFunc`
nil panic at `fd_windows.cs:27`) plus the Tier-0 channel frog are what stand between compiling and
validating, exactly as this section said.

*Updated 2026-08-01: the ruling landed (option (d) above) and `net` builds — see the Deadline banner.
The init gap and the channel frog are what remain, exactly as predicted.*

#### Ground-truthed 2026-08-02 (r37-poll scout) — the census, and the `sync.OnceFunc` row is STALE

Measured on the post-r37-poll tree, one pipeline invocation
(`-tests -test-action all -test-timeout 20m`). The wall is exactly one root, and it is **not** the one
recorded above.

| | |
|:--|:--|
| production + test build | **0 errors** (warnings only) |
| Go side | 138 top-level tests |
| C# side | **0 reached** — `status: conversion-blocked`, every row `C#=""` |
| excluded declarations | 129 (unsupported capabilities) |

**The `sync.OnceFunc` nil panic at `net/fd_windows.cs:27` does not reproduce.** That line is
`poll.InitWSA()`, and nothing gets far enough to execute it — `InitWSA` appears nowhere in the run.
Whatever closed it closed it uncredited, exactly the staleness charter §9 warns about; probe, don't
inherit.

**Today's blocker is the OPEN pointer-PARAMETER nil-deref row**, the one the `os` nil-receiver arc
named as still outstanding ("the same defect is still open for pointer PARAMETERS … the complete fix
is to give parameters the same unconditional `DerefOrNull`"). The chain is identical whether `net` is
entered through a program or through its test host:

```
go.net_package..cctor()                          net/addrselect.cs
  → netip.AddrFrom16                             net/netip/netip.cs
    → go.net.netip_package..cctor()
      → unique.Make → go.unique_package..cctor() unique/handle.cs
        → concurrent.NewHashTrieMap
          → concurrent.newIndirectNode(nil)      internal/concurrent/hashtriemap.cs:372
            → PanicException: runtime error: invalid memory address or nil pointer dereference
```

```go
func newIndirectNode[K, V comparable](parent *indirect[K, V]) *indirect[K, V] {
	return &indirect[K, V]{node: node[K, V]{isEntry: false}, parent: parent}   // parent is nil here
}
```
```csharp
internal static ж<Δindirect<K, V>> newIndirectNode<K, V>(ж<Δindirect<K, V>> Ꮡparent) {
    ref var parent = ref Ꮡparent.Value;    // ← eager entry alias; the body never dereferences it
    return Ꮡ(new Δindirect<K, V>(node: new node<K, V>(isEntry: false), parent: Ꮡparent));
}
```

The body only ever uses `Ꮡparent`; the alias exists and panics. Neither `nilSafePtrParamNames`
heuristic fires (the parameter is not nil-compared in the body and no same-package call site passes a
literal `nil` — `NewHashTrieMap`'s does, but through a generic instantiation). So `net` is a
**one-root wall**, and that root is already designed: the parameter arm of `DerefOrNull`. It is a
much larger emission footprint than the receiver arm (3167 entry aliases) and wants its own
measurement and ruling — but it now has a second package demanding it, and `unique` and
`internal/concurrent` are blocked by the same line.

Nothing beyond it is measurable yet: with zero tests reached there is no second bucket to report.
Re-run this census the moment the parameter arm lands.

## `time` — builds and RUNS (2026-08-02, r35): 139 pass / 17 fail / 2 skip / 1 infra-error of 159

`time` was opened the day the channels frog was confirmed closed. It went from **260 build errors** to
**0**, and the host now runs the whole suite in ~60 s with **zero empty verdicts** — the timer
machinery in `time_impl.cs` (one global heap on a Windows high-resolution waitable timer) holds up:
`TestTicker`, `TestTickTimes`, `TestAfterTimes`, `TestAfterTick`, `TestTimerStopStress`,
`TestTimerModifiedEarlier`, `TestAdjustTimers`, `TestLongAdjustTimers`, `TestAfterFuncStarvation`
and the sleep family all pass against real rendezvous. **No channel-semantics defect was found**; the
one channel-shaped failure is a documented model divergence, not a wave3 regression (below).

Seven roots stood between the package and a build; all seven are fixed and none was `time`-specific.
Six are in the converter or go2cs-gen, one is a hand-owned reach:

| Errors | Root | Layer |
|--:|:--|:--|
| 1 (blocking all) | A mixed-accessibility `GoImplicitConv` pair whose less-accessible side is in ANOTHER assembly has no legal operator — skip it (`export_test.go`'s `type RuleKind int` over production `ruleKind`) | go2cs-gen |
| 176 | A DOT-imported collision-renamed CONST/VAR emitted its raw Go name (`Second`, `UTC`, `Hour`, …) | converter |
| 44 | A collision-renamed member kept the RAW package qualifier where the file's using is Δ-renamed (`time.ΔNanosecond` vs `Δtime.ΔNanosecond`) | converter |
| 33 | **A local/parameter that SHADOWS a package name was resolved as the package** — `getAliasedTypeName` applied to a rendered expression; `time.Year()` → `Δtime.Year()`, `time.Month()` → `timeꓸMonth()`, `time.Hour()` → `time.ΔHour()` | converter |
| 3 | A nested func literal's captures hoisted to the ENCLOSING statement's buffer, above the declaration they name | converter |
| 2 | A folded constant of a NAMED type lost its type (`8 * time.Hour` → a bare `long`) — the loud half is CS1929, the silent half is `d` printing as digits | converter |
| 1 | A concat of two SLICED string literals has no C# operator (span `+` span is literal-only) | converter |

Plus the runtime blocker behind the build: `time/tzdata`'s `init()` pulls
`time.registerLoadFromEmbeddedTZData` by `//go:linkname`, which was a throwing stub — inside a MODULE
INITIALIZER, so a blank `import _ "time/tzdata"` took the host down before `main`. Now a real
forwarder (see *A whitelisted target may be ORDINARY CONVERTED GO* in the reference). That fix pays
for itself twice: with tzdata registered, `loadLocation` falls back to the embedded database, which is
how the suite's `initTestingZone` reaches `America/Los_Angeles` at all — its hard-coded
`../../lib/time/zoneinfo.zip` cannot resolve from the C# host's working directory.

Guard for the six general converter/generator roots: the `PackageNameShadowing` behavioral test
(a `describe(time time.Time)` parameter, a `time :=` local, Δ-qualified renamed members, a
dot-importing sibling file, the named-type fold in both positions, and the sliced-literal concat —
output-compared vs `go run`) plus `FuncLitArgCapture` case 15 for the hoist.

**The 17 remaining failures, rooted, none of them `time`-local machinery:**

| Count | Tests | Root | Owner |
|--:|:--|:--|:--|
| 6 | `TestChan` and its five subtests | **Documented model divergence, not a defect.** Go 1.23 made a chan-based Timer/Ticker channel SYNCHRONOUS (#37196) by coupling the channel's receive path to the timer inside the runtime; `time_impl.cs` reproduces Go's own `GODEBUG=asynctimerchan=1` mode instead, so `tim.Stop() = false, want true` and "extra tick" are exactly what that mode produces. ⚠ The `asynctimerchan=1` SUBTEST also fails, which the divergence does NOT explain — either `t.Setenv("GODEBUG", …)` does not reach the converted `godebug`, or the async model has its own bug. That subtest is the honest next probe here. **⚠ HISTORICAL — superseded twice: the mode-1 failure was the one-firing-per-pass burst (r39-timer), and the mode-0 "divergence" is IMPLEMENTED (r39b); `TestChan` passes in all three modes. See *RESOLVED — r39b lands the synchronous timer channel* below.** | time / godebug |
| 9 | `TestDefaultLoc`, `TestNanosecondsToUTC`, `TestSecondsToUTC`, `TestParse`, `TestTimeGob`, `TestTimeIsDST`, `TestTimeJSON`, `TestUnmarshalInvalidTimes`, `TestZoneBounds` | All die with the same `nil pointer dereference` inside `GoFunc.HandleFinally`. Every one of them formats a `Time` through `fmt` on its FAILURE path (`%#v`, `%+v`, `%v` of a struct with a `*Location`), so the NRE is plausibly SECONDARY to a comparison that already failed — the reflect/fmt bridge, not the clock. Not rooted; the next increment should print the pre-format comparison rather than reason about the stack. | reflect/fmt bridge |
| 1 | `TestParseErrors` | A REAL parse divergence: Go reports `extra text: "07:00"` where C# reports `cannot parse "Z07:00" as "Z07:00"` — the `Z07:00` layout element consumes differently. `format.go` conversion defect, `time`-local. | time |
| 1 | ~~`TestTruncateRound`~~ | ~~`math/big.mulAddVWW` is an unimplemented asm stub (`NotImplementedException`), reached through `big.Int.Mul`.~~ **CLOSED 2026-08-02 (r37-time-os-fin), and it was never a `math/big` ARC — it was a build-tag selection.** math/big predates the `purego` convention and gates its portable fallbacks on its own `math_big_pure_go`, which the default tag set did not carry, so all EIGHT of `arith_decl.go`'s bodyless declarations became throwing stubs. The scope was not one test: the whole package compiled clean and could not do arithmetic — a direct probe dies inside `big.Int.SetString`, i.e. parsing a decimal string, because that is already a `mulAddVWW`. See *`purego` is not the only spelling of this decision* in ConversionStrategies-Reference.md. | ~~math/big arc~~ done |
| 1 | `TestUnmarshalTextAllocations` | `got 3784 allocs, want 0` — the established **alloc-count-semantics** unit mismatch (`AllocsPerRun` counts mallocs in Go, BYTES on the CLR). A disclosure candidate by the class `strings`/`io` already established; **not self-ruled here**. | ruling |

So `time`'s distance is: one `time`-local parse bug, one probe (`asynctimerchan=1`), one shared
reflect/fmt-bridge NRE family worth 9 verdicts, and two rows owned elsewhere. Nothing about timers,
sleeps, tickers or channel rendezvous is in the way.

### Re-measured 2026-08-02 (r37-time-os-fin): **146 pass / 11 fail / 2 skip / 0 infra-error of 159**

Measured as a same-session A/B, both arms on this tree, only `src/core/math/big` differing:

| Arm | Split of 159 verdict rows (137 top-level + 22 subtests) |
|:--|:--|
| math/big asm stubs (the r36 state) | 145 pass · 11 fail · **1 infrastructure-error** · 2 skip — reproducing the r36 record exactly |
| math/big pure-Go arith | **146 pass · 11 fail · 0 infrastructure-error · 2 skip** |

Exactly one row moved — `TestTruncateRound`, infrastructure-error → pass — which is what the
`math_big_pure_go` build tag was expected to do and nothing else. The **infrastructure-error column
is now empty**, so every remaining row is a real verdict disagreement rather than a host casualty.

The 11 failing rows, exhaustively, in three buckets:

| Rows | Tests | Bucket |
|--:|:--|:--|
| 8 | `TestChan` + `asynctimerchan={0,1,2}` + their `Timer`/`Ticker` children | The **timer-model** item, recorded and deliberately not taken: `time_impl.cs` §"⚠ OPEN — a periodic timer can fire an UNBOUNDED BURST in one service pass". The Timer half under `asynctimerchan=0` is the accepted sync-mode divergence; the Ticker half fails in all three modes and is the burst. The faithful fix ("fire each timer at most once per pass") changes the heart of the model and wants its own lane. The `t.Setenv("GODEBUG", …)` half of the old ⚠ is closed — r36 proved the converted `godebug` sees it. **⚠ HISTORICAL — both halves are now closed: the burst by r39-timer, the sync-mode divergence by r39b (see *RESOLVED — r39b lands the synchronous timer channel* below).** |
| 2 | `TestTimeJSON`, `TestUnmarshalInvalidTimes` | The reflect-bridge **chip's** rows — the last two survivors of the old 9-verdict NRE family (r36's honest traceback rooted the other seven at `Location.lookup`, and they pass). Untouched here by fence. |
| 1 | `TestUnmarshalTextAllocations` | Alloc-count-semantics, **awaiting the coordinator's disclosure ruling** — unchanged in status, but the number moved: `got 3544` → **`got 2728`**, an exactly-predicted −816 B/run (6 × 136, the six `parseUint` range loops in `parseRFC3339`'s UTC path) from the allocation-free `slice<T>` enumerator. Also measured as an A/B on this tree; the board's older `3784` predates other r36 fixes. Nonzero remains, so a ruling is still what settles this row — see `docs/CleanupBacklog.md` item 7 (`IByteSeq<T>` interface boxing) for the next lever. |

**`TestParseErrors` is gone from the failing set** (r36's `fallthrough`-placement fix), as are the
seven `Location.lookup` rows. `time`'s distance to a bank is now: **the timer-model item, the
reflect-bridge chip, and one ruling** — three owners, none of them the converter, and nothing
`time`-local outside the timer model.

### Re-measured 2026-08-03 (r39-timer): **152 pass / 5 fail / 2 skip of 159** — the timer model is CLOSED and every residual row is a RULING

The reflect-bridge chip's two rows (`TestTimeJSON`, `TestUnmarshalInvalidTimes`) closed on their own
between r37 and this lane — increment 5 landed, and the base commit `832f0960d` already measured
**148 pass / 9 fail / 2 skip**: the eight `TestChan` rows plus the one alloc row and nothing else.
This lane took the timer-model item and rooted the alloc row.

**The timer-model item is fixed, and the faithful fix was one statement.** The burst was never a
"fire at most once per pass" heuristic waiting to be invented — it is what Go gets for free by
sampling the clock ONCE per service pass. `timers.check` reads `nanotime()` once and threads that
value through `timers.run(now)` into `timer.unlockAndRun(now)`; the clock is never re-read inside a
pass. `serviceTimers` was re-reading it on every drain iteration, so the theorem that bounds Go did
not hold here. Moving `int64 now = runtimeNano();` above the drain loop restores it, and the bound is
then provable rather than enforced: for a periodic timer `next = when + period*(1 + delay/period)`
with `delay = now - when = q*period + r`, `0 <= r < period`, so `next = now + (period - r) > now`
strictly — the re-peek always breaks. One-shots clear `when`. Hence **every timer fires at most once
per pass**, for every period including the 1 ns `testTimerChan` resets to. It does not rate-limit: the
pass then waits until the new head deadline, which for a fast ticker is already past, so the next pass
begins at once — exactly Go's scheduler calling `check` again. Recorded in
ConversionStrategies-Reference.md, *ONE firing per timer per pass*.

Measured on this tree, same command both arms (`go2cs -tests -test-action all -test-timeout 10m`):

| Row | Base `832f0960d` | After the fix |
|:--|:--|:--|
| `TestChan/asynctimerchan=0/Timer` | fail — `tim.Stop() = false, want true` + `extra tick` | fail — **identical message** |
| `TestChan/asynctimerchan=0/Ticker` | fail — `extra tick` + **`early done`** | fail — `extra tick` ×4, **`early done` gone** |
| `TestChan/asynctimerchan=1/Ticker` | fail — `extra tick` ×2 + `early done` | **pass** |
| `TestChan/asynctimerchan=2/Ticker` | fail — `extra tick` | **pass** |
| `TestChan/asynctimerchan={1,2}` parents | fail | **pass** |
| `TestChan/asynctimerchan=0` parent, `TestChan` root | fail | fail (mode 0 only) |
| `TestUnmarshalTextAllocations` | fail — `got 216 allocs` | fail — `got 216 allocs` (untouched) |

**+4 rows, and `early done` — the burst's signature — is gone from every mode.**

**What the mode-0 ruling now decides over: exactly 4 rows, and they are the documented divergence,
row for row.** With the burst gone, `asynctimerchan=1` and `=2` pass **completely** — Timer *and*
Ticker. Those are the modes where `testTimerChan` sets `synctimerchan=false` and therefore drains
stale values explicitly. Only mode 0 fails, and each of its failures sits either inside a block the
test guards with `if synctimerchan` (the `tim.Stop() = false, want true` pair, which is #37196's
Stop-blocks-old-values semantics) or on a `noTick()` whose preceding `drainAsync()` is a deliberate
no-op in sync mode (the four `extra tick`s). The same implementation passes the identical test body
wherever the test expects asynchronous semantics and fails only where it switches to expecting
synchronous ones. That is the accepted `GODEBUG=asynctimerchan=1` divergence and nothing else — no
residual burst, no channel-rendezvous defect. Closing it for real means implementing Go 1.23's
synchronous timer channel (the ignored `syncTimer(c)` argument), which lives **inside golib's channel
implementation** — a Tier-0 golib capability, not a `time` fix.

**`TestUnmarshalTextAllocations` — rooted, and the board's previous attribution was WRONG.** The r38
train recorded "the FINAL 216 B live above `parseRFC3339` in the `Time.UnmarshalText` wrapper chain".
Measured directly (a probe project borrowing `InternalsVisibleTo("time.tests")`,
`GC.GetAllocatedBytesForCurrentThread()` over 2,000 runs), **zero bytes are above `parseRFC3339`**:

| Frame | B/run |
|:--|--:|
| `Time.UnmarshalText(data)` | 88 |
| `parseStrictRFC3339(b)` | 88 |
| `parseRFC3339<slice<byte>>(b, Local)` | 88 |
| `Date(...)`, `daysIn(...)`, `isDigit(...)` | 0 |
| the same `parseRFC3339` body with the closure replaced by a static local function | **0** |
| a bare capturing lambda, isolated control | **88** |
| the converted TEST body: `heap(new Time(), out var Ꮡt)` alone | **128** |
| the converted TEST body: `heap(...)` + `UnmarshalText` | **216** |

**216 = 88 + 128, and both halves are converter emission, not `time`:**

1. **88 B — `parseRFC3339`'s `parseUint` func literal.** It captures `ok`, so C# hoists `ok` into a
   display class and allocates that class **plus a `Func<>` delegate on every call** (24 + 64 = 88,
   matched exactly by the isolated control). Go stack-allocates both, because escape analysis proves
   the closure does not escape. The general converter fix is real and valuable — *a func literal bound
   to a local that is only ever CALLED should be emitted as a C# **local function**, which captures
   without allocating* — but it is a new emission mode in `convFuncLit.go` /
   `captureModeOperations.go` (847 + 1,136 lines) reaching every closure in the corpus.
2. **128 B — the converter heaps the test's own `var t Time`.** The emission is
   `ref var tΔ1 = ref heap(new Δtime.Time(), out var ᏑtΔ1);` because `t`'s address is taken by the
   pointer-receiver call `t.UnmarshalText(in)`. Go keeps it on the stack (that is *why* the assert
   says zero). Note `ᏑtΔ1` is **never referenced** in the emitted body — the box is minted dead — so a
   narrow rule ("don't heap when the emitted `Ꮡx` is unused, because a C# `ref` parameter provably
   cannot escape its callee") looks sound and would be a headline win. It is still an
   **escape-analysis** change, which charter §7 puts behind an adversarially-reviewed design.

**Consequence for the ruling: this row is NOT a clean disclosure candidate.** The established
`alloc-profile` class covers asserts the managed CLR *provably cannot* satisfy; both halves here are
fixable converter gaps, and §5 says a real bug is never a disclosure candidate. Equally, neither half
alone flips the row (216 → 128 still fails `want 0`), so it cannot be cleared incrementally either.
The honest options are: (a) land both converter fixes as their own gated arcs and green the row
outright, (b) hold the row open until they land, or (c) disclose it knowingly as a *converter-gap*
rather than a CLR-semantics divergence — which would be a new disclosure class and should be decided
as one. Not self-ruled here.

**`time`'s distance to a bank is now two RULINGS and zero open engineering:** the mode-0
sync-timer-channel divergence (4 rows, needs a golib channel capability to close for real) and
`TestUnmarshalTextAllocations` (1 row, needs two converter arcs to close for real).

### RESOLVED — r39b lands the synchronous timer channel; the 4 mode-0 rows close (2026-08-03)

Ruling #1 below commissioned the arc; it is implemented. The change is small because the guarantee
is small, once stated as a guarantee rather than as plumbing:

> **A `Stop` or `Reset` prevents any tick generated before the call from being received after it.**

Two mechanisms carry it, at Go's own two layers. **golib** gains the `hchan.timer` hook the wave3
design deliberately left out — `IChannelTimer` installed by `channel<T>.AttachTimer`,
`Capacity`/`Length` masked to 0 while the owner answers `HidesBuffer` (asked LIVE, because
`GODEBUG=asynctimerchan` selects the model at every observation), and `DrainBuffer()` =
`runtime.timerchandrain`, the only sanctioned way to **un-send**. **`time_impl.cs`** gains Go's
`timer.sendLock` + `timer.seq`: a service pass now only *offers* a tick — it captures `seq` with the
firing decision and re-checks it under `sendLock` before sending, so an offer a `Stop`/`Reset`
overtook is ABANDONED. `seq` is deliberately not `gen` (a firing bumps `gen`, so a delivery check
must not key off it). `Stop`/`Reset` bump `seq` and drain inside ONE `sendLock` hold — stronger than
Go's ordering, and necessarily so: Go can drain outside the lock because a sync-mode chan timer is
heaped only while a receiver blocks on it, and this model's service thread is always eager.

A **third** mechanism has no Go counterpart and is the arc's real lesson. The adversarial round
measured that mechanisms 1 and 2 revoke correctly but cannot between them ANSWER correctly: in the
window mechanism 1 exists to cover, a tick is in neither place a `Stop` looks — `when` cleared at
commit, buffer not yet filled — so `Stop` revoked the tick and reported that there had been none.
Hundreds of one-shots per run where Go answers `true` for every one. Go never reaches that state
because a sync-mode chan timer nobody is receiving from is not heaped at all and therefore never
fires; **eager firing opens the window, so eager firing has to close it** — `runtimeTimer.offered`
records the in-flight firing and `Stop`/`Reset` count it as pending. The general form of the lesson:
*a divergence in WHEN work happens is not free just because the observable end states match — check
the states in between.* Two more review findings landed with it: the mode selector no longer routes
through the punned `unsafe.Pointer` `cp` (its non-nil-ness was an accident of two type layouts, and
this very change added a field to `ChanCore`), and `asyncTimerChan` now reproduces
`runtime.atoi32`'s parse, so `asynctimerchan=00` is synchronous as in Go rather than asynchronous.

Measured on this tree, both arms with the same command, the fixed arm run twice with identical
verdicts:

| Row | After r39-timer (`df3da05d1`) | After r39b |
|:--|:--|:--|
| `TestChan/asynctimerchan=0/Timer` | fail — `tim.Stop() = false, want true` + `extra tick` | **pass** |
| `TestChan/asynctimerchan=0/Ticker` | fail — `extra tick` ×2 + `early done` | **pass** |
| `TestChan/asynctimerchan=0` parent, `TestChan` root | fail | **pass** |
| `TestChan/asynctimerchan={1,2}` × Timer/Ticker | pass | **pass** (async model untouched) |
| `TestUnmarshalTextAllocations` | fail — `got 216 allocs` | fail — unchanged (ruling #2's arc) |
| **package** | 152 pass / 5 fail / 2 skip | **156 pass / 1 fail / 2 skip of 159** |

`time` is therefore down to ONE row, and it is the alloc row ruling #2 already commissioned an arc
for. Nothing here is `time`-specific: the hook is on `ChanCore`, so any future owner-fed channel gets
the same revocation primitive. ⚠ `DrainBuffer` revokes values the channel already accepted and is
sound only for a channel whose producer owns it exclusively — it is not a general "clear the
channel" utility. Guard: the `SyncTimerChannel` behavioral project (stdout byte-compared against
`go run`), which asserts the `pending` answers, the absence of stale ticks, `len`/`cap` 0, that
`AfterFunc` is untouched, **200 `Reset`-to-imminent timers that must still DELIVER** (the
counter-property that keeps the drain honest), 600 ticker `Stop`/`Reset`-vs-firing races that must
revoke exactly nothing, and two 600-timer batches armed against ONE absolute deadline and
stopped/reset at that instant. ⚠ That last shape is load-bearing and fragile in a way worth
recording: it only samples the window because the batch and the caller's sleep share an absolute
deadline. The first draft gave each timer its own relative duration, so the caller woke milliseconds
after the flush and the neutered control PASSED — a guard that proved nothing. Neutered controls now
fire for all three mechanisms (drop `offered`: 315–483 of 600; drop the drain: 477 stale of 600;
drop the `seq` check: stale ticks in all four race sections).

## `math/big` — **224 of 226** (re-measured 2026-08-09, r58b); root 3 CLOSED, the two left are both the alloc model

> **r58b (2026-08-09): root 3 below is FIXED and both gob rows pass.** The reflection bridge now
> packs the typed nil — `reflect.Value.Interface()` re-encodes a null read out of a POINTER-kinded
> slot as that slot's canonical typed nil (`ж<T>.NilBox`, the same instance `reflect.Zero` and every
> emitted `nil`→`*T` conversion already produce), so `v.Interface().(GobEncoder)` succeeds and
> `big.Int.GobEncode`'s `if x == nil` arm is reached. `TestGobEncodingNilIntInSlice` and
> `TestGobEncodingNilRatInSlice` both pass: **222 → 224 of 226**, exactly those two rows. Guarded by
> the `ReflectTypedNilInterface` behavioral test.
>
> **`math/big` still does NOT bank**, for the reason root 3's own paragraph predicted: rows 1 and 2
> are the alloc model and neither is disclosable. *(Superseded 2026-08-13 — the ж-box A3 section at
> the end of this board carries the post-A2 pinned pipeline measure: 224/226, TestMulUnbalanced
> 20,499,128 B (+0.06 %, unmoved), TestNewIntAllocs exactly 1 obj/run.)* Both re-measured on this tree — `TestNewIntAllocs`:
> *"measured 81,600 allocated BYTES over 100 run(s) … got 816.000000"*; `TestMulUnbalanced`:
> *"multiplication uses too much memory (20487208 > 51 times the size of inputs)"* (inputs =
> (50000+40)×8 = 400,320 B, so the converted `nat.mul` allocates ~51× where Go bounds at 10×). Row 1
> waits on the AllocsPerRun ownership decision; row 2 is a truthful, comparable measurement of the
> box model, which a disclosure would launder rather than explain.
>
> **⚠ Attribution correction, measured as an A/B rather than reasoned.** The paragraph below says
> this root "also blocks part of `encoding/gob` (99 of 106)". The bridge half does **not**. r58b ran
> gob's full pipeline with and without the fix on the same tree: **99 of 106 both ways, the same seven
> divergent rows.** The typed-nil root has TWO halves that pay DIFFERENT packages — the reflection
> READ path (fixed here; it pays *math/big's* gob rows, because gob reaches math/big's types through
> `reflect`) and the EMISSION path (`var ip *int` boxed into an interface by ordinary converted
> code), which is what gob's own `TestNilPointerInsideInterface` and the `mustPanic` family need. The
> emission half remains chip-class / design-with-user and is untouched.

### Historical — the r57a state (superseded above)

## `math/big` — **222 of 226** (measured 2026-08-09, r57a); three roots left, two of them the alloc model

> **Supersedes the 2026-08-02 state below and the board's `9 of 226` census.** Both were taken with
> r56f's named-numeric shift-masking defect live — the defect whose corrupted Lehmer cosequences made
> `GCD`'s `for len(B.abs) > 1` loop stop converging, i.e. an infinite loop inside this very package.
> With it fixed the suite runs to completion: **226 verdicts, 222 matching, 83 excluded**
> (examples + benchmarks, Phase-4D). The "nil `x`/`y` GCD panic" recorded below did **not** reproduce;
> `lehmerGCD`'s converted entry guards its extended outputs correctly (`if (Ꮡx != nil) { x = Ua.Value; … }`,
> `int.cs:970`) and `big.Rat`'s `SetFrac` → `norm` → `GCD` path runs, so that root is closed too.
>
> **The four remaining rows are three roots, and only ONE is a defect:**
>
> 1. **`TestNewIntAllocs` — the AllocsPerRun-reports-BYTES shim, fifth member.** Measured 81,600 B over
>    100 runs; the assert wants `0` allocations from `x.Add(x, NewInt(0))` and is handed `816.000000`
>    "allocations" that are really bytes per run. Report-never-disclose, per the standing rule.
> 2. **`TestMulUnbalanced` — the same alloc model, measured honestly in bytes on both sides.**
>    *"multiplication uses too much memory (20487200 > 51 times the size of inputs)"*. Go reads
>    `runtime.MemStats.TotalAlloc` around the multiply and bounds it at 51× the input words, so unlike
>    row 1 the units ARE comparable — the converted `nat` simply allocates far more per word, which is
>    the `ж<T>`/`slice` box model r56d decomposed to the byte on nistec. **Not a disclosure and not a
>    correctness failure**: it is the allocation-model overhead stated as a budget, and it will move
>    when that model does, not before.
> 3. **`TestGobEncodingNilIntInSlice` / `TestGobEncodingNilRatInSlice` — a REAL defect, and a
>    general one: Go's TYPED-NIL interface does not survive the conversion.** Both panic identically
>    with *"interface conversion: interface {} is nil, not gob.GobEncoder"* inside
>    `gob.EncodeValue` (`encoder.cs:303`). In Go, an element of `make([]*Int, 1)` is a nil `*Int`, so
>    `v.Interface()` yields a **non-nil interface** carrying `(type=*Int, value=nil)`; the assertion
>    `.(GobEncoder)` therefore SUCCEEDS and `GobEncode` is dispatched on a nil receiver, which
>    `math/big` handles explicitly — `func (x *Int) GobEncode() { if x == nil { return nil, nil } }`
>    (`intmarsh.go:18`). The whole test exists to exercise that contract. In the conversion the nil
>    `ж<ΔInt>` reaches the interface as a plain `null`, losing its type identity, so the assertion
>    fails and gob's `catchError` re-panics (correctly — Go re-panics on a non-`gobError` too).
>    **Scope is corpus-wide, not `math/big`'s**: any `x.(I)` on a typed-nil pointer takes the wrong
>    arm, and this is one of Go's most load-bearing interface behaviors. Worth noting golib already
>    has the vocabulary — `ж<T>` distinguishes `IsNilStandardPointer` from a null reference
>    (`DerefOrNull`, `ж.PointerExtensions.cs:359`), so a typed nil is *representable*; what is missing
>    is producing one where a nil pointer is boxed into an interface (the reflection bridge's
>    `Value.Interface()` knows the static type and is the narrow place to start). **Chip-class /
>    design-with-user**, not a lane fix — it changes what `== nil` means for every converted
>    interface. Also blocks part of `encoding/gob` (99 of 106).
>
> **Consequence for banking: `math/big` cannot bank on roots 1 and 2 regardless of root 3**, so
> fixing the typed-nil defect pays `encoding/gob` and the corpus, not this row. The package is
> nonetheless now one of the most thoroughly exercised in the corpus — 222 verdicts across `Int`,
> `Rat`, `Float`, `nat`, decimal/float conversion, primality, GCD and the marshalling surfaces.

### Historical — the 2026-08-02 state (superseded above)

Until r37-time-os-fin `math/big` was in the 302-package clean compile and could not perform a single
operation: the `math_big_pure_go` build tag was missing from the default set, so all eight of
`arith_decl.go`'s assembly-backed declarations converted to throwing partial stubs (detail in the
`TestTruncateRound` row above and in ConversionStrategies-Reference.md). With the tag applied, a
direct Go-vs-C# probe — `SetString`, `Mul`, `Add`, `Sub`, `Lsh`, `Rsh`, `Quo`, `Rem`, `Exp`,
`big.Float.Mul`, and a 64-deep `Mul` chain — is **byte-identical to `go run`**. Before the fix the
same probe died on its first line, inside `big.Int.SetString`.

**One root remains before the package's own suite is worth running: `big.Int.GCD` with nil `x`/`y`
panics with a nil pointer dereference.** Repro is three lines —
`new(big.Int).GCD(nil, nil, a, b)` — and `big.Rat` reaches it on the ordinary path
(`SetFrac` → `norm` → `GCD`), so all of `big.Rat` is behind it. Go documents nil `x`/`y` as the
*normal* non-extended call, so this is a real conversion defect, not an unsupported shape.
Measured on BOTH the committed corpus and a fresh whole-stdlib reconvert, so it is **not** the
pending deref-accessor rebank: `lehmerGCD`'s entry aliases already take the current
`DerefOrNull`/`DerefOrNil` accessors in the reconverted emission and it panics identically. Not
rooted further — it was found in passing while verifying the build-tag fix and is out of that
lane's scope.

## Runtime failures

| Package | State |
|:--|:--|
| ~~`hash/maphash`~~ | **DONE 2026-07-29 — 22/22, banked.** Computed float constants that directly use a named untyped integer wrapper now materialize once at the destination's float width; `TestSmhasherAvalanche`'s mean is 50000 and the full SMHasher matrix matches Go. |
| ~~`compress/flate`~~ | **DONE 2026-07-31 — 64/64, banked.** `TestWriterReset` was NOT a state difference: `deepValueEqual`'s `Func` arm returned false unconditionally, on the reasoning that two nil funcs would already have matched the `invalid == invalid` rule at the top. That holds only for a nil func boxed as `any`; a nil func reached as a struct FIELD is typed by its static func type and is a VALID nil Value, so the arm declared every pair of nil func fields unequal — and the test nils `fill`/`step`/`bulkHasher`/`bestSpeed` precisely so `DeepEqual` can compare the rest. Go's rule is "equal iff both nil"; the arm now asks it. The tell was that every field compared equal individually while the enclosing struct did not. |
| ~~`image/gif`~~ | **DONE 2026-07-31 — 28/28, banked.** `TestWriter` was the blank-import module-initializer gap and nothing else: with `_ "image/png"`'s `init()` forced, the PNG decoder registers and `image.Decode` reads `../testdata/video-001.png`. No `image/gif` defect existed. |
| ~~`image/png`~~ | **DONE 2026-07-31 — 28/28, banked.** The old "does not validate" probe was stale by weeks: a fresh run split **15 of 17** top-level tests passing, and the remainder was ONE defect with a second stacked on top of it. The real root is that Go's slice-to-array-**pointer** conversion `(*[N]T)(s)` was emitted as a **copy**. png's `cbTCA8` row loop writes every un-premultiplied pixel through `d := (*[4]byte)(dst)`, so a non-opaque RGBA source encoded as an all-zero image — and the two `TestWriteRGBA` subtests that did pass passed by luck (the opaque one takes `cbTC8` entirely; the fully-transparent one wants all-zero output, which is also what a lost write produces). `array<T>` now carries a `(low, length)` window and the pointer form takes `array<T>.Alias`; the value form `[N]T(s)` still copies, because Go's does. Above it sat a redundant value adapter — see the row below — which only ever surfaced on `diff`'s failure path, so fixing the aliasing greened the package on its own. |
| ~~`image/draw`~~ | **DONE 2026-07-31 — 9/9, banked.** All four failures were two defects, both fixed at the root. `TestDraw` was the address-taken *value parameter* box-copy: `DrawMask`'s `clip(dst, &r, src, &sp, mask, &mp)` narrows all three in place, and `Ꮡ(r)` boxed a COPY, so the draw loop ran on the unclipped rectangle. (The empty-`Pix` panic above was that same unclipped geometry, not an assertion defect — the guess in this row was wrong.) The other three were value adapters carrying no Go dynamic type, so `image.Image` type switches took the wrong arm. |

## RETRACTED — the `encoding/base32`/`base64` "mode-unstable production emission" was STALE BANKED OUTPUT

This section previously recorded the receiver-box drift on `encoding/base32/base32.cs` (3/3 lines) and
`encoding/base64/base64.cs` (6/6) as a **mode** disagreement — the receiver-box analysis reaching a
different answer under `-tests` than under `-stdlib` — and ruled the drift "expected sweep output, and
must be restored, never banked". **Both halves of that are wrong.** Re-measured 2026-07-31 on master:

| Emission | `base32.cs` / `base64.cs` |
|:--|:--|
| whole-stdlib `-stdlib -comments` reconvert, master converter | boxed (`encʗp` + `ref var enc = ref heap(…)` + `return Ꮡenc`) |
| the `-tests` pipeline's regenerated production `.cs` | **byte-identical** to the above |
| the **committed** files | unboxed — the *pre-`c23caf4f9`* form |

The two modes agree exactly. What actually drifted is the **corpus**: `c23caf4f9` (*an address-taken
value RECEIVER heap-boxes*) landed before this row was written and moved these two files, and they were
never rebanked — so every sweep since compared a current emission against a stale bank and restored it
again, three times over. The prior "three measurements" attribution is charter §9's false-alarm trap (a)
in its textbook form: a `bin/go2cs.exe` built before `c23caf4f9` reproduces the reported result exactly,
including the claim that `-stdlib` "equals the committed file". Same origin as the `internal/zstd` /
`crypto/hmac` retraction above — **force `go build -o bin/go2cs.exe` before recording a coupling.**

**There is no mode-instability to close here, and there cannot be**: a method's receiver is
function-scoped, so its address can only be taken inside its own method body. A production method's body
is production source; a `_test.go` file cannot add a statement to it. The receiver-box analysis therefore
reads an input `-tests` mode cannot widen — structurally unlike the package-level-var case the
sibling-scan fix above exists for, where a `_test.go` `&g` genuinely does address production storage.
Recorded as a property of the rule in
[`ConversionStrategies-Reference.md`](../ConversionStrategies-Reference.md), *An address-taken VALUE
PARAMETER heap-boxes too*.

Both files are **banked** (2026-07-31) at the boxed emission, and both packages re-validate at their
exact counts (base32 26, base64 17). The standing sweep drift is closed.

## ~~Open~~ CLOSED — the REDUNDANT adapter was a key mismatch (value 2026-07-31, pointer 2026-08-02)

**DONE.** Both halves landed at the converter: ONE key spelling shared by the record loader and the
cast site (`implementRecordKey` / `canonicalImplementRecordIfaceName`, named `valueImplementKey` /
`canonicalValueRecordIfaceName` until the pointer set joined them), and the func-type exclusion this
row demanded (`valueRecordRealizesAsPartialStruct`, gating on the target's Go underlying being a
non-`*types.Signature`). Whole-stdlib A/B, both roots seeded, 302/302 converted per side: **13 files,
497 constructions removed**, every changed line the same edit, plus the 16 records that existed only
to generate those adapters; the rest of the corpus adapter census is identical count for count,
`HandlerFuncᴠΔHandler` included.

Two corrections to the row as filed below, both measured rather than reasoned:

- **A SECOND divergence sat underneath the reported one.** Besides the interface side, the record
  carries the EMITTED C# type name while the use side named the GO type — image/color's `RGBA` is
  `ΔRGBA` in its own metadata (collision-renamed against its `RGBA()` method). That divergence alone
  gates the 478-site group; fixing the interface side by itself would have recovered only 19.
- **The 79 `binary_*ᴠByteOrder` are NOT this defect.** `encoding/binary/package_info.cs` holds **no
  `GoImplement` lines at all** — the package never converts one of its own values to `ByteOrder`
  (Go's `var BigEndian bigEndian` carries no `var _ ByteOrder = …` witness), so there is no record to
  match and the consumer's local adapter is the only realization. `color.Palette`→`color.Model` (5)
  survives for the same reason. **A pair a package satisfies but never records is its own root** —
  the one place where "the declaring assembly implements it" is true in Go and false in the emitted
  C#. **That increment is now DONE** (`recordSamePackageImplements`, `samePackageImplements.go`):
  the declaring side records the VALUE pairs it satisfies, behind five gates — exported interface,
  underlying not a `*types.Signature`, neither side generic, both sides declared in a file the run
  converts, and every interface method reachable within ONE embed hop (ImplementGenerator forwards a
  promoted member exactly that far) — and a whole-stdlib A/B landed the prediction below exactly, 89
  constructions across 34 files (43 + 36 + 5 + 5), alongside 33 records added and 31 removed (3
  prune-subsumed, 28 consumer-local) across 16 declaring packages; 68 files total,
  `go2cs-stdlib.slnx` 0 errors. `HandlerFunc`→`ΔHandler` is absent, as the delegate gate requires.
  Owed, and the reason the depth gate is conservative: extending ImplementGenerator's promoted-member
  forwarding past one hop would recover `net`'s two `tcpConnWithout*`→`Conn` records.
  **The POINTER half of the SAME-PACKAGE recorder is now DONE too (2026-08-14, lane D)** — distinct from
  the foreign-record key increment two paragraphs below, which fixed how a record is LOOKED UP; this fixes
  which records EXIST. `recordSamePackageValueImplements` became `recordSamePackageImplements` and asks
  `types.Implements(*T, Iface)` as well, behind the five value gates with TWO changes, each forced by the
  different trust rule. **Added:** BOTH sides EXPORTED (`pointerRecordIsPubliclyRealizable`), because a
  `(Pointer = true)` record is consumed by NAMING the generated `<T>ж<Iface>` class and ImplementGenerator
  scopes it `public` only when both participants are — an unexported participant would advertise a class no
  consumer can reference (CS0122). **Tightened:** realizability requires DIRECT resolution (index length 1),
  no promotion, where the value bound allows one embed hop — the ж adapter's promoted-member arms are keyed
  on embedded POINTER fields and, with exactly one present, take every unbound member unconditionally, which
  is sound for a DEMANDED record and not for a speculative one.
  `StructPointerPromotionWithInterface`'s `MyCustomError` (embeds both the `Abser` interface and `*MyError`;
  `Abs` comes from the interface) is the corpus instance, and the `go2cs.slnx` build caught it as CS1929
  binding `Abs` to `time.Abs` — a reminder that the solution build is the only gate compiling generated
  adapters tree-wide, and that a speculative record must be bounded by what the GENERATOR can realize, not
  by what Go's method set says.
  Whole-stdlib A/B, both roots seeded, 304/304 per side: **75 files, 184 records added / 117 removed
  (net +67, 1,071 → 1,138), 318 adapter constructions repointed**, 0 `.csproj`, 0 `README.md`, marker gate
  0 violations. Every removal is a consumer-local duplicate the declaring assembly now owns — `go/parser`
  49 and `go/types` 28 of `go/ast`'s node types, the five `debug/*` readers' `io.SectionReader` pairs,
  `net/http`'s `io.Pipe*` and `sync.Mutex`→`Locker`. The 548-pair figure the deferral quoted was the raw
  same-package pair count, not the delta: most were already recorded from cast sites.
  **This retires the L10 second-identity trap at the root.** `syscall`'s three `Sockaddr` pairs were
  witnessed by one method body, so hand-owning `RawSockaddrAny.Sockaddr` dropped all three records and
  `net` minted duplicates. Re-running that exact probe on both converters: pre-increment the records are
  absent and `net` emits `syscall_SockaddrInet4жΔSockaddr`; post-increment all three are present and `net`
  emits `syscall.SockaddrInet4жΔSockaddr`. The netpoll arc's §7 blocker is unblocked at the record level
  (the port-alias decode defect itself still stands — see that doc).
  Guards: `SamePackageImplementNoWitness` (`*Tally`→`Metric` moved negative→positive; new
  unexported-target negative `tick`), `ForeignPointerImplementSuppression` (`Lone` likewise; `shade.Level`
  negative byte-identical). Still owed: the `-tests` closure emits these records too, so the committed
  test sources of the validated roster want a refresh at the next milestone rebank.
  Guard: `SamePackageImplementNoWitness`. Rule:
  [`ConversionStrategies-Reference.md`](../ConversionStrategies-Reference.md), *A package records the
  pairs it SATISFIES, not only the ones it witnesses*. Its whole corpus footprint, measured on the post-fix
  census by classifying every remaining `<pkg>_<T>ᴠ<Iface>` construction (is `<Iface>` declared in
  `<T>`'s own package, and is `<T>` a `partial struct` rather than an interface or a delegate?), is
  **89 constructions in 3 packages**: `encoding/binary` `bigEndian`/`littleEndian`→`ByteOrder` (79),
  `image/color` `Palette`→`Model` (5), `crypto` `Hash`→`SignerOpts` (5). Everything else remaining is
  either interface-sourced (`io.ReadWriteCloser`, `flate.Reader`, `net.Conn`, `ast.Expr` — a
  different adapter kind entirely) or genuinely cross-package (`syscall.Signal`→`os.Signal`), or is
  the deliberately-excluded delegate (`net/http` `HandlerFunc`→`ΔHandler`, 8+4).

**The deferred POINTER increment is now DONE too (2026-08-02).** `importedPointerImplements` carried the
same two divergences, and both sides now compose through the same shared `implementRecordKey` — no second
naming path, and `canonicalRecordIfaceName` retired with its last caller. The trust rule really is
different, and it turned out to be *weaker*, not stronger: `(Pointer = true)` is precisely the shape
`ImplementGenerator` realizes as the adapter class `<T>ж<Iface>`, so the record's existence IS the
answer and no `valueRecordRealizesAsPartialStruct` analogue is needed (the delegate hazard cannot arise
on a set whose every member already took the adapter route). Measured before deciding, per the row's own
discipline: an instrumented whole-stdlib run classified all 1,224 pointer lookups as 289 hits, 868
genuine no-records, and **67 near-misses** — every one a true pair, no candidate-key regressions.

Whole-stdlib A/B, both roots seeded, 304/304 per side: **31 files, 66 constructions** rewritten from the
consumer's local `<pkg>_<T>ж<Iface>` to the declaring package's own `<pkg>.<T>ж<Iface>`, plus the 37
`(Pointer = true)` records that existed only to generate those local classes. Zero additions; the total
adapter-construction census is unchanged at **4348**, so this is a one-for-one redirection rather than a
removal — the pointer form's dead machinery is a duplicate class, not an extra allocation. By declaring
package: `text/template/parse` 33, `go/types` 20, `image` 4, `net/http` 4, `net/url` 2, `net/textproto` 1,
`go/internal/srcimporter` 1, `go/build/constraint` 1. `go2cs-stdlib.slnx` 0 errors on the overlaid tree;
CNR byte-identical across all 544 behavioral packages.

Two findings worth carrying forward:

- **A dependent EMISSION defect that only the collision-renamed types reach.** A Δ-renamed foreign type
  resolves through a whole-TYPE `global using` alias (`imageꓸRGBA`), which is an identifier and not a
  path, so composing the adapter onto it names nothing — `imageꓸRGBAжImage`, CS0246 ×11 (confirmed by
  building, not predicted). The foreign-adapter arm now rebuilds a dotless base as the package qualifier
  plus the type's EMITTED simple name. The same latent composition sits in the neighbouring
  same-assembly (`-tests`) arm; nothing reaches it today and it was deliberately left alone.
- **No observable failure was reproduced, and that is the honest finding.** The generated pointer
  adapter's `Equals` compares `IжAdapter.Box` by reference, so a redundant local adapter and the
  declaring assembly's own one compare equal and alias the same object — unlike the value form, which
  really did break `image/png`'s `%v`. What is wrong is duplication plus a load-order-dependent dynamic
  type: `AdapterRegistry.Register` is first-wins, so which assembly's class a type-assert re-wraps into
  depends on which module initializer ran first.

Rule, both compositions, and the trust gates:
[`ConversionStrategies-Reference.md`](../ConversionStrategies-Reference.md), *A foreign implement record
is keyed in ONE spelling, and a VALUE one is trusted only for a partial struct*. Guarded by the
`ForeignValueImplementSuppression` behavioral test (a multi-segment sibling that DOES convert its own
values, a collision-renamed implementer, and a named FUNC type as the live negative — the pre-fix
converter emits five adapters where the fixed one emits the func's alone), with
`ValueAdapterDynamicType` as its byte-identical complement, and by the pointer sibling
`ForeignPointerImplementSuppression` (a self-converting sibling with a collision-renamed `*Tone` and an
ordinary `*Plain` as the positives, against two live negatives: `*Lone`, a pair the sibling satisfies but
never records, and `shade.Level`, an interface with the same SIMPLE name — pre-fix emits four local
adapters, fixed emits the two negatives' alone).

*The row as originally filed follows.*

Converting a foreign package's value into an interface that package **itself declares** emits a
local `<pkg>_<T>ᴠ<Iface>` adapter class even though the declaring assembly already implements the
pair. The converter already knows not to (`convCallExpr`'s both-foreign value arm consults
`importedValueImplements`, recorded from the dependency's `package_info.cs` `[assembly:
GoImplement<T, Iface>]` lines) — **the lookup simply never matches for a multi-segment import
path**, because the two sides compose the interface key differently. Measured, not reasoned
(`canonicalRecordIfaceName` called directly):

| import path | load side (from the package NAME) | use side (the rendered C# name) | |
|:--|:--|:--|:--|
| `bufio` | `bufio_package.Reader` | `bufio_package.Reader` | match |
| `image/color` | `color_package.Color` | `image.color_package.Color` | **miss** |
| `encoding/binary` | `binary_package.ByteOrder` | `encoding.binary_package.ByteOrder` | **miss** |

Corpus footprint of the redundant constructions: **478** `color_ΔRGBAᴠColor`, 79
`binary_{big,little}Endianᴠ ByteOrder`, plus the rest of `image/color`'s models — every same-package
value-form foreign record in the corpus is a nested path, and not one is single-segment.

It is not merely dead machinery. The adapter is a **second identity for one Go value**: `reflect`
and `fmt` see the adapter object where the Value's own type says the wrapped struct, which is how
it surfaced — `image/png`'s `diff` printing `%v` of a `color.Color` died with
`System.ArgumentException: Field 'R' … is not a field on the target object which is of type
'go.image_package+color_NRGBAᴠColor'`. (It masked the aliasing defect above: fixing the aliasing
removed the failure that reached the print.) A direct-boxed `NRGBA` and an adapter-wrapped one also
compare unequal in one direction.

**Any fix must clear one hazard first.** The record says nothing about how the DECLARING assembly
realized the pair, and a named FUNC type cannot be realized as a partial struct — `net/http`'s
`[assembly: GoImplement<HandlerFunc, ΔHandler>]` is realized as an adapter class there, so trusting
the record for it would emit a bare delegate into an interface slot (CS0029) in `expvar`,
`net/http/cgi` and three more. The usable gate is the target's Go underlying: trust the record only
when it is not a `*types.Signature`.

Two live consumers are named by the reflection arc (§6.1's adapter-type `Kind`/`Elem` follow-up),
and they do NOT overlap: this row removes adapters that were never needed, while the reflection
chip must still unwrap the ones that genuinely are (`color_PaletteᴠModel`, `syscall_ΔSignalᴠΔSignal`,
`net_Connᴠ*`). Both are real; neither subsumes the other.

## Open — intermittent, on an already-banked package

| Package | State |
|:--|:--|
| `hash/maphash` | **INTERMITTENT (filed 2026-07-31, not rooted).** Banked and validating at 22/22, but ONE validated sweep died mid-`TestSmhasher*` with a .NET **FailFast** on a worker thread, the fault attributed to `go.UntypedInt.CastTo<ulong>(Int64)` with `RhThrowHwEx` on the stack. Two sibling sweeps in the same wave ran maphash to its exact banked count, and so did the r26 integration train's own 66-package sweep over the three lanes combined (66 pass / 0 fail, 2,454 s), which ran maphash to its exact 22. The attribution is almost certainly misleading: `CastTo` is a raw reinterpret and cannot raise a hardware exception, so the likely fault is an NRE/AV in an inlined caller credited to the frame it was inlined into — e.g. unboxing a null `any` into `UntypedInt` on the worker path. SMHasher seeds randomly, which is what makes it probabilistic and why it reproduces on no fixed input. Rooted enough to file, not enough to fix: the next sighting should capture the full FailFast stack and the seed. |

## The blank-import module-initializer gap — CLOSED (2026-07-31)

Go's `_ "image/png"` imports a package **purely** for the side effect of its `init()`, and the
language guarantees that initializer runs before `main`. The converter maps a Go `init()` onto
`[GoInit]`, which `csproj-template.xml` aliases to .NET's `[ModuleInitializer]` — the right shape,
and a **weaker guarantee**: a module initializer fires at first access to something in its module,
so an assembly nothing in the program ever *names* is never loaded and its initializer never runs.
A blank import is by definition the case that names nothing, and the observable form was a registry
that stays empty: `image/gif`'s `writer_test.go` blank-imports `_ "image/png"` so png's `init()`
calls `image.RegisterFormat` (`image/png/reader.cs`), it never ran, and `TestWriter` failed with
`../testdata/video-001.png image: unknown format` at **27 of 28**.

The converter now emits, at the top of the importing file's class body, a hook that forces it:

```csharp
// blank import: go.image.png_package (side effects only; no using emitted — a `using _` alias hijacks C# discards)
[GoInit] internal static void initᴛᴛblankImportꓸimageꓸpng() { builtin.initPackage(typeof(go.image.png_package)); }
```

`builtin.initPackage` is `RuntimeHelpers.RunModuleConstructor`, which the runtime guarantees runs a
module constructor **at most once** (so several blank importers of one package are no-ops) and which
is measured AOT-safe — under Native AOT the gap does not arise at all, since a single native image
has no lazy assembly load. One hook per (assembly, imported package), named from the import path so
two blank imports in one file cannot collide; Go's pseudo-packages (`unsafe`, `builtin`, `C`) are
skipped because the language gives them no initialization, which holds the corpus blast radius to
**three files** — `crypto/x509` (sha1/sha256/sha512), `runtime/metrics` (runtime), `runtime/race`
(amd64v1) — rather than the seventy that carry `import _ "unsafe"` for `//go:linkname`. Full rule,
the ordering reasoning, and the deliberately-deferred alternative (forcing *every* import eagerly in
dependency order — the only way to reproduce Go's init ordering in full, at the cost of loading the
whole transitive assembly closure at startup): `docs/ConversionStrategies-Reference.md`, *A blank
import forces the imported package's `init` to run*. Guarded by the `BlankImportSideEffects`
behavioral test (a registry two blank-imported siblings fill from their `init`s, read back by an
importer that never names either) plus the `TestBlankImportInitName` / `TestNoInitPseudoPackages`
converter unit tests.

The other consumers this unblocks are all registration-by-blank-import: `database/sql` drivers
(`_ "github.com/…/mysql"` → `sql.Register`), `net/http/pprof` (its `init()` installs the
`/debug/pprof` handlers), `image/png`/`image/jpeg` as decoders for anything that calls
`image.Decode`, and `time/tzdata`. A blank import was never invisible to the build — it is in
`go/packages`' import list, so the project reference already existed; only the *load* did not happen.

## `os` — 681 of 683 rows agree + 1 disclosed; ONE residual, now ROOTED (r35-os → r39-osalloc, 2026-08-03)

> **Current state is the r39-osalloc sub-section at the END of this block** — 681 of 683 rows agreeing
> (173 of 175 top-level), 34 matching skips, 4 capability-excluded, and exactly one real divergence
> (`TestWriteStringAlloc`). r39 decomposed that divergence to the byte and closed 65.6 % of it in two
> golib fixes; the remainder is architectural and is recorded there as an arc, so `os` does NOT bank on
> this row. Everything between here and there is the arc that got it there, kept for its roots and its
> retractions. The header below is the r36 state.
>
> *Header as it stood before r38-os-fin:* **`os` — 164 of 178 match + 1 disclosed; the unreached block
> is gone (r35-os → r36-os-tail, 2026-08-02)**

Measured with `go2cs -tests -test-action all -test-timeout 35m "<GOROOT>/src/os" src/core/os`.
`os` builds with **0 errors** and the host runs. Progression across the arc, all from one pipeline
command: **48 agreeing → 141 → 158 → 164**; the first jump from the build blockers, the second from
the `readReparseLink` host-killer, the third from the element-alias arm and the run-directory shape
below. ⚠ **Give it 35 m, not 15** — at 15 m under sibling-worktree load the host self-terminated at
900 s and reported the tail as unreached.

| | Go | C# (r35) | C# (r36) |
|:--|--:|--:|--:|
| top-level tests | 178 (143 pass · 34 skip · 1 fail) | 166 reached (123 pass · 34 skip · 8 fail · 1 infra-error) | **177 reached** (129 pass · 34 skip · 12 fail · 2 infra-error) |
| **agreeing** | | 158 | **164** |
| disclosed | | 1 | 1 (`TestUTF16Alloc`, alloc-count-semantics) |
| real mismatches | | 7 | 13 |
| unreached (host died) | | 12 | **1** (`TestPipeEOF`) |

The mismatch count RISES while agreement rises because the r35 host died at test ~50: eleven of the
thirteen rows below were never *reached* before, so they were counted as unreached rather than as
failures. Six of them are load-sensitive (they pass standalone), and of the genuinely stable ones,
every row is now rooted.

⚠ **`TestReadStdin`'s 462 subtests still fill the `errors` list, and it is a NAME-ENCODING artifact,
not a failure.** Two of its inputs contain `\x1a` (SUB). `go test -json` renders that rune in the
subtest name as the ESCAPED text `\x1a`; the C# host emits the raw rune, so the oracle pairs each
subtest as `Go="pass" C#=""` plus `Go="" C#="pass"` — 924 lines that read like a mass failure and are
not one. The top-level `TestReadStdin` AGREES. Fixing it means escaping non-printable runes in
`TestReporter`'s reported names the way Go does; that changes every package's reported subtest names,
so it wants the full sweep as its gate and is recorded here rather than done in passing.

### Closed in this arc

- **Build blocker 1 — a production type ALIAS is invisible to its own test assembly.** Under the
  white-box reference model the production sources are not compiled into the test assembly, so the
  `global using FileInfo = go.io.fs_package.FileInfo;` that `os/types.cs` declares is out of scope for
  a converted `_test.go`. `export_test.go`'s `var Atime = atime` names `FileInfo` unqualified →
  CS0246 ×2, the whole build. Fixed at the same seam the foreign-alias arm already states: a
  same-package alias DECLARED IN A PRODUCTION FILE renders as its TARGET under
  `testWhiteboxReference` (an alias declared by a `_test.go` emits its own `global using` and is left
  alone). `typeNameResolution.go`; CNR byte-identical.
- **Build blocker 2 — a `GoImplicitConv` record with NO local operand.** `os_windows_test.go`'s
  privilege helper converts `syscall.Handle(t)` over a `syscall.Token`; both operands are foreign, so
  `ImplicitConvGenerator` had nothing to extend and minted a phantom `partial struct ΔHandle` inside
  `os_test_package` (CS1061 on `.Value`). Both arms of `checkForImplicitConversion` now require
  `conversionRecordHasLocalOperand`. Guard `ForeignPairNumericConv`; CNR byte-identical. Rule:
  [`ConversionStrategies-Reference.md`](../ConversionStrategies-Reference.md), *A GoImplicitConv record
  needs at least one LOCAL operand*.
- **Runtime root — a keyed element inherited the LHS variable's interface.** `TestCopyFS`'s
  `fsys = fstest.MapFS{"william": {Data: …}}` (with `fsys` an `fs.FS`) ran every `*MapFile` element
  through a spurious `*T → Iface` cast whose deref-copy collapse turned the elided `Ꮡ(new MapFile(…))`
  into the bare struct — CS0029 ×5. The element's target is now the composite's own value slot.
  Guard `ElidedPtrElemIfaceAssign`; CNR byte-identical. Rule: *A keyed element's interface target is
  the composite's own SLOT, never the LHS variable's type*.
- **Host-killer — `os.readReparseLink`, hand-owned.** A fourth member of the raw-metal-on-non-native-
  types fork, and the first to take the host down in `os`: the reparse-buffer structs end in
  `PathBuffer [1]uint16`, a Go inline array standing in for the variable-length name the kernel wrote
  after it and an 8-byte MANAGED REFERENCE in the conversion. golib correctly refuses to alias managed
  storage for a reference-bearing struct, so the reinterpret took the raw-address route and
  `&rb.PathBuffer[0]` resolved an object reference synthesized out of path bytes: ACCESS_VIOLATION in
  `array<uint16>.get_Item`, at test 50 of 178. `src/core/os/file_windows_impl.cs` decodes the record
  out of the byte slice at its documented offsets (same remedy as `dir_windows_impl.cs`);
  `manualConversionFuncs` gains `os.readReparseLink`. ⚠ `syscall.Readlink` carries the SAME defect over
  its own private `reparseDataBuffer`/`symbolicLinkReparseBuffer`/`mountPointReparseBuffer` copies —
  LATENT (nothing in the validated corpus reaches it), recorded rather than fixed speculatively.

### Closed in the r36-os-tail follow-up (2026-08-02)

- **Converter — an element pointer reinterpreted as an array pointer now ALIASES.**
  `(*[N]T)(unsafe.Pointer(p))` where `p` is a `*T` emits `array<T>.AliasPointer(p, N)` — a window over
  the storage `p` is an element of — instead of the raw-address route, whose two lowerings were both
  wrong for it: dereferenced it read an `array<T>` struct out of the pointed-at DATA, and under
  `convSliceExpr`'s `[:n]` fusion it produced a `slice<T>` COPY whose writes went nowhere. That copy is
  what made all 462 `TestReadStdin` subtests read zeros. Same element type is the gate (a `T[]` view
  over differently-typed storage has no managed spelling, so every genuine reinterpret keeps the
  address route), golib decides at RUNTIME whether real element storage is behind the pointer, and
  Go's `N` is clamped to the extent that exists — in this idiom `N` is a promise (`10000`, `1<<16`,
  `0xffff`) and the result is always re-sliced to the real count. One latent sibling defect fell out
  with it: `SliceExtensions.slice(this array<T>, …)` sliced the RAW backing, so explicit bounds over
  ANY window (`Alias`'s too) addressed the source's elements rather than the array's. Guard
  `ArrayPointerElementAlias`; rule in
  [`ConversionStrategies-Reference.md`](../ConversionStrategies-Reference.md). Corpus emission
  footprint: 3 production sites, all in the same raw-metal family, none of them reached
  (§*A/B footprint* below).
- **Pipeline — the isolated run directory reproduces the package's SHAPE, not just its files.** The
  converter enumerates the package directory's immediate subdirectory NAMES into the manifest and the
  input digest; the host creates them empty before staging fixtures. That is what `TestReadDir` needed
  (`exec` beside `read_test.go`) and it is the last of the environment-fidelity gaps in `os`.

#### A/B footprint of the element-alias arm — 13 files, all classified

Measured as a **two-temp-root reconvert** (base converter vs this one, same seed) rather than against
the committed tree: `src/core` at `af5df9e16` carries ~132 files of pre-existing drift from other
lanes' converter changes, which a diff-vs-HEAD would have mixed in. **No file is in a validated
package.** The corpus builds with 0 errors on the overlaid reconvert (304 projects).

| Sites | Files | Classification |
|:--|:--|:--|
| `(*[4]byte)(unsafe.Pointer(n.Data(off)))` in `abi.Name.pkgPath` | `reflect/type.cs`, `internal/reflectlite/type.cs`, `runtime/type.cs` | **Strict improvement.** The name blob IS managed byte storage, so the window is real where the address route punned an `array<byte>` struct (a reference + bounds) out of four name bytes. |
| `reflect.rtype.gcSlice` over `t.t.GCData` | `reflect/type.cs` | Read-only GC-bitmap view. Was a `ReadOnlySpan` copy of raw memory, now a window (or the identical address fallback when `GCData` is not managed storage). |
| reparse `PathBuffer` decode | `internal/syscall/windows/reparse_windows.cs` ×2, `syscall/syscall_windows.cs` ×2 | **Same raw-metal family as `readReparseLink`, unchanged in outcome.** `PathBuffer [1]uint16` is a variable-length tail standing in for kernel bytes, so a managed window over it is one element and the old span read GC heap past that one element. Neither can work; the new form fails LOUDLY (a Go-style slice-bounds panic) instead of returning garbage. `os` does not reach these — its own decode is hand-owned (`os/file_windows_impl.cs`). |
| Win32 DNS record strings | `net/lookup_windows.cs` ×3 | Native pointers, so golib takes the address fallback: byte-identical behavior. |
| runtime internals | `runtime/{select,heapdump,mbitmap,string}.cs` | Paths the managed runtime does not execute (`selectgo` is superseded by ChanCore). |
| `AllowUnsafeBlocks` `true`→`false` | `internal.syscall.windows.csproj`, `net.csproj`, `reflect.csproj` | Consequence, and a welcome one: the span fusion was those packages' ONLY `unsafe` usage. |

⚠ **The huge sentinel length is why the emission casts.** `runtime`'s `findnull`/`findnullw`/
`gostringw` convert to `*[1<<47-1]byte` / `*[1<<46-1]uint16`; such a literal types as `long` in C# and
has no implicit conversion to `nint` (CS1503 ×3, caught by the corpus build, fixed by `csNintLiteral`).
It is also why `AliasPointer` CLAMPS: an unclamped `(int)` of that length would overflow.

### The residual, every row rooted

| Row | Cost | Root |
|:--|--:|:--|
| **host-killer: an ExecutionEngineException whose SITE MOVES between runs** | 12–29 | Not a defect at the crash site. Three runs died in three different places (`TestReadlink`'s AV, then `syscall.Environ`, then `syscall.encodeWTF16` under `os.MkdirAll`), and each site runs CLEAN standalone — `syscall.Environ()` was probed end-to-end in its own converted program and returns the real block. That is accumulated heap corruption, and the strongest candidate is `os_windows_test.go`'s own `createMountPoint`: it reinterprets a managed `[]byte` as a `windows.MountPointReparseBuffer` and WRITES four `uint16` fields through it. golib's `Reinterpret` cannot alias a reference-bearing struct, so the fallback hands back `(ж<TDst>)(uintptr)box` — a **transient** pinned address of a managed slice, written through after its pin expired. Remedy candidates, both bigger than a package arc: make the non-representable fallback PIN the source for the derived box's lifetime, or make it fail loudly instead of returning a stale address. A blanket "fail loudly" is NOT available — reflect's prefix-downcast idiom (`(*structType)(unsafe.Pointer(t))`) deliberately depends on the address route. |
| `TestDirectoryJunction` | 1 | The same `createMountPoint` reinterpret, this time surfacing as a contained `IndexOutOfRangeException` at `&buf.PathBuffer[0]`. Raw metal on a non-native type, in TEST code that cannot be hand-owned — no converter or golib change can lay a managed array reference over inline OS bytes. |
| ~~`TestReadStdin` (462 subtests)~~ | ~~1~~ | **CLOSED 2026-08-02 (r36-os-tail).** The remedy this row named was the right one: `(*[N]T)(unsafe.Pointer(p))` over a `*T` now emits `array<T>.AliasPointer(p, N)`, a real window over the storage `p` is an element of, instead of the raw-address route whose `[:n]` fusion produced a `slice<T>` COPY. All 462 subtests pass. Guard `ArrayPointerElementAlias`; behavioral footprint one justified re-baseline (`PointerCastSliceReinterpret`'s same-element-type arm). |
| ~~`TestNilFileMethods`~~ | 1 | **CLOSED 2026-08-02 (r36-nilrecv)** — the alternative this row named is the one that works. See *A nil RECEIVER is nil-deferring, not nil-safe* below. |
| ~~`TestReadDir`~~ | ~~1~~ | **CLOSED 2026-08-02 (r36-os-tail).** The remedy this row named, implemented: the converter enumerates the package directory's immediate subdirectory NAMES (`testFixtureDirectories`, part of the manifest and the input digest) and the host creates them EMPTY in its run root before staging fixtures (`TestHost.CreateFixtureDirectories`). `ReadDir(".")` now sees the same shape `go test` does. Blast radius is far smaller than feared: across the validated roster only `os`, `io` and `math/rand` have any subdirectory beyond the `testdata` already staged with contents. |
| `TestCmdArgs` | 1 | Newly REACHED 2026-08-02 (it was inside r35's unreached block). Raw metal, pre-existing: `syscall.CommandLineToArgv` returns a NATIVE pointer, so `(ж<array<ж<array<uint16>>>>)(uintptr)(r0)` reads an `array<T>` STRUCT — a backing reference plus bounds — out of the pointer array's own bytes, and `(*argv)[:argc]` then slices with fabricated bounds: `ArgumentException: Indices low, high and max represent a range outside bounds of the array reference`. Untouched by the element-alias arm, which requires a Go POINTER source; a `uintptr` source keeps the address route by design. Same family as `TestDirectoryJunction` and the `createMountPoint` reinterpret. |
| `TestGetppid` | 1 | Newly REACHED 2026-08-02. The child runs and answers, but `syscall.Getppid()` reports `0` where the parent's pid is expected — `getProcessEntry`'s `Process32First`/`Next` walk finds no entry. A real, contained syscall gap (it does NOT fault, which is what the struct-passing census below already recorded for this wrapper). |
| `TestReadlink` | 0–1 | Newly REACHED 2026-08-02, and it is the **symlink-privilege row** (the `os.runtime_rand` → `testenv.MustHaveSymlink` row above) surfacing at last: standalone, its six `symlink_*` subtests fail with *"A required privilege is not held by the client"* while the three `junction_*` subtests PASS. Go SKIPS the symlink arms for want of the privilege; C# runs and fails them. Confirms that row's prediction — clearing `MustHaveSymlink` converts these to matching skips rather than passes. (It agreed in the final full run, so it is privilege/timing-sensitive as well.) |
| `TestRootDirAsTemp` | 1 | Newly REACHED 2026-08-02. The test re-execs the host with TMP/TEMP pointed at a drive ROOT to check `TempDir()`; the CHILD host then cannot create its own isolated run directory there — `DirectoryNotFoundException: Could not find a part of the path 'Z:\go2cs-tests\os\…'` out of `TestHost.Run`'s `Directory.CreateDirectory(workingDirectory)`. The isolation model and the test's premise collide: Go's test binary needs no scratch directory of its own. Pre-existing (same line before and after this lane's host change). |
| `TestWriteStringAlloc` | 1 | `AllocsPerRun` bounded at ZERO. Deliberately **not** disclosed: the byte-derived shim CAN report 0, so the io/strings unit-mismatch ruling does not cover it. Go's `WriteString` avoids the copy with `unsafe.Slice` over the string's own bytes; a go2cs `@string` is its own storage, so the write path allocates (measured 9088 bytes). A real divergence — an `sstring`-shaped optimization, not a disclosure. |
| `TestRemoveAllWithExecutedProcess` | 1 | **ROOTED 2026-08-02 (r36-os-tail), and it is the .NET deployment model, not a conversion defect.** The test copies `os.Executable()` — one file — into a fresh `t.TempDir()` 100 times and runs each copy, to make Windows hold an image handle. `os.Executable()` is CORRECT: it returns the test host's **apphost** (`os.tests.exe`). But an apphost is a stub bound at build time to a managed assembly of the same base name that must sit BESIDE it, so a single-file copy can never run. Reproduced standalone by copying any converted project's apphost alone into a temp directory: exit `0x8000809a` = hostfxr `LibHostAppRootFindFailure`, message *"The application to execute does not exist: '…\<AssemblyName>.dll'"* — byte-for-byte the code the test reports. Go's test binary is statically linked, which is the only reason its premise holds there. The sole fix that would satisfy it is publishing every converted test host **self-contained single-file** (≈70 MB and a publish instead of a build, per package) — disproportionate to one test. Environment divergence; leave failing. |
| `TestStartProcess/relative` | 1 | **RE-MEASURED 2026-08-02 (r36-os-tail): PASSES** — 3/3 standalone and in the full run, with nothing in this lane touching `joinExeDirAndFName`/`FullPath`/`StartProcess`. It belongs to the load-sensitive child-output class below, not to a code defect. |
| **load-sensitive child-process flakes** | ~6 | New classification 2026-08-02 (r36-os-tail). A set of tests that **pass standalone and fail only in the full parallel run**, all with one signature: the child process produced NO output (`system hostname of ""`, `Child returned "[]"`, `reports stdin is not pipe ''`) or a `t.TempDir()` that had vanished. The membership MOVES between runs, which is the tell: across the two full runs measured, `TestFileReaddir/TempDir`, `TestStatLxSymLink` and `TestReadlink` failed in one and passed in the other, while `TestStartProcess` and `TestLongPath` did the reverse; `TestHostname`, `TestExecutable` and `TestStatStdin` failed in both yet pass 3/3 standalone. Measured while three sibling worktrees ran their own pipelines. **Treat any single-run failure in this set as unconfirmed until it is reproduced standalone** — that is how `TestStartProcess/relative` came to be recorded as a rooted mismatch when it is not one. |
| `TestStatLxSymLink` | intermittent | `t.TempDir()` cleanup hit a file "used by another process" — a handle the host had not released yet. Same load-sensitive family as the row above. |

### A nil RECEIVER is nil-DEFERRING, not nil-safe — `TestNilFileMethods` closed (r36-nilrecv, 2026-08-02)

The row above asked for a ruling and named the alternative in its last clause. That alternative is the
right one, and it is now built and gated: golib's **`DerefOrNull`** binds `Unsafe.NullRef<T>()` for a nil
box, and every pointer-receiver entry alias uses it **unconditionally**. A null ref is legal to HOLD and
to pass on as `ref T`; it faults on USE. So the receiver panic is not raised at entry (today's defect)
and not discarded (the naive widening's defect) — it lands where Go's does, after any side effect the
body performed first, as `NullReferenceException` → `TryAsPanic` → Go's own
`runtime error: invalid memory address or nil pointer dereference`, recoverable and printed verbatim.

Because it is faithful whether or not the body guards, there is no predicate:
`isComparedDirectBoxReceiverIdent` is subsumed and deleted. **The alias is emitted in TWO places** and
go2cs-gen's `ReceiverMethodTemplate` — the bridge reaching a `ref T` receiver through a box — deref'd
eagerly too, one call frame EARLIER than Go; both now take the accessor.

**Measured.** `os` pipeline: `TestNilFileMethods` → **pass** (all fifteen methods return `ErrInvalid`).
Footprint against a control reconvert with the base converter (so pre-existing corpus staleness is
subtracted): **378 stdlib files in 132 packages**, and every changed line is the alias — 1858 `.Value`
+ 159 `.DerefOrNil` become 2017 `.DerefOrNull`, nothing else. CNR: 27 behavioral projects, 56 lines, one
shape. That is one project MORE than the reverted widening's 26, because `DerefOrNull` also subsumes the
`isInherentlyHeapAllocatedType` → `.ValueSlot` receiver arm. Full corpus builds 304/304 clean; behavioral
suite 528/528 + 498/498 output. Guard: `NilReceiverMethods`. No null-page cliff: a synthetic field 200 KB
past address zero still faults as a clean NRE, and a converted Go struct cannot reach that offset anyway
(inline `[N]T` → an 8-byte `array<T>` reference).

⚠ **The same defect is still open for pointer PARAMETERS** — 3167 entry aliases in the corpus keep the
eager `.Value`, mitigated only by the two heuristics (`nilSafePtrParamNames`: nil-COMPARED in the body, or
passed `nil` at a same-package call site), and those two route to the nil-SAFE `DerefOrNil`, which is the
silent-`default(T)` accessor. Go's rule is identical for a parameter and a receiver, so the complete fix is
to give parameters the same unconditional `DerefOrNull` — mechanically trivial now, but a much larger
emission footprint that wants its own measurement and its own ruling.

⚠ **The `os` run that closed this row reached FARTHER than the banked one** (177 top-level tests vs 166),
because the moving-site `ExecutionEngineException` above did not fire. The newly-reached tests bring their
own failures (`TestCmdArgs` slice-bounds in test code, plus `TestExecutable`/`TestGetppid`/`TestStatStdin`/
`TestRootDirAsTemp`/`TestHostname`/`TestUserConfigDir`/`TestLongPathAbs`), none of them receiver-shaped.
Read the arc's residual table as measured against 166 reached; a fresh baseline needs a quiet machine.

⚠ **A NEW member of the `-tests`-closure production-file family, found by this arc's canaries and owed
to the next rebank.** Since the validation-badge work (2026-08-02) every package's `.csproj` carries an
eight-line *"Ship this package's versioned validation proof sheet"* block, emitted by the `-stdlib`
driver, which has the roster. A single-package `-tests` run does not, so it regenerates the `.csproj`
**without** those eight lines — `0 8` on `git diff --numstat`, in EVERY banked package a sweep touches.
Confirmed on both canaries below (`path/filepath`, `io`) and on `os` itself; it predates this arc and is
caused by no change in it. Classify it with the other `-tests`-closure files: restore, never bank, and
let the whole-corpus regen level it.

**Spot-canaries on the post-change tree, both at their banked counts:** `path/filepath` →
`status: validated`, `matched: true`, 55 top-level (37 pass · 18 skip), 0 errors. `io` →
`status: validated`, `matched: true`, 54 top-level, 2 disclosed, 0 errors (its production
`package_info.cs` shows the documented `+2` satisfies-not-witnesses records — restore, don't chase).

**Re-run after the r36-os-tail changes, both still at their roster counts:** `path/filepath` →
**61 validated** (20 skips agreeing), `io` → **59 validated, 2 disclosed**. Their `-tests`-closure
churn is the documented set and nothing else: the `0 8` validation-proof block on every `.csproj`,
io's `+2` `package_info.cs` records, and — pre-existing, from converter changes landed since the last
whole-corpus regen — io's `package_test_info.cs` implicit-conv record set, `io_test.cs` and
`multi_test.cs`. **The committed `go2cs_test_host.cs` does NOT churn**: the run-directory list is
omitted entirely when a package has no subdirectories, which is 56 of the 71 banked packages, so only
the 15 that genuinely have one differ (and only by the lines that describe it).

**r36-pin, same day — the moving-crash row RETRACTED, and the real top row named.** The r35
attribution of the moving-site `ExecutionEngineException` to `createMountPoint`'s transient-pin
write was **wrong**: a pre-fix control run of the whole suite at base `af5df9e16` produced ZERO
ExecutionEngine/AccessViolation faults — whatever closed that crash closed it inside the r35 train
itself, uncredited. (The transient-pin defect is nonetheless REAL and fixed — `Reinterpret`'s
fallback pinned for one statement while the derived pointer lived on; deterministic guard
`ReinterpretPinLifetime`, rule in ConversionStrategies-Reference — it just was not os's crash.)
`os`'s dominant remaining cost is the **blocking-pipe family**: `internal/poll` on Windows does not
unblock an in-progress read on `Close`, hanging `TestPipeEOF`/`TestPipeIOCloseRace` + two siblings
and starving six more tests of child stdout (`TestHostname`, `TestExecutable`, `TestGetppid`,
`TestStatStdin`, both `TestStartProcess` arms, `TestRootDirAsTemp`) — its own future arc, and the
reason pipeline invocations leak `os.tests.exe`. *(Closed 2026-08-02 by r37-poll, below — with the
diagnosis half right: the hang was real and is fixed, but it was not in `internal/poll`, and the
six child-stdout rows did **not** follow it.)* ⚠ Scheduling: never run two lanes against ONE
package's pipeline — the host is named per package, so the rename defence cannot apply; the tell
for a sibling-killed run is `go2cs_test_results.json` carrying the PREVIOUS run's mtime.

**Attribution was measured, not asserted — FIVE runs, and only one test is converter-determined
(r36-nilrecv).** Three with the base converter, two with the fix. `TestNilFileMethods`: **fail 3/3
on base, pass 2/2 with the fix.** *Every other* test that moved, moved in BOTH arms —
`TestHostname` (2 base, 2 fix), `TestStatLxSymLink` (2, 1), `TestFileReaddir` (1, 2),
`TestReaddirnamesOneAtATime` (1, 1), `TestProgWideChdir` (1, 0), `TestCopyFS` (0, 1),
`TestLongPathAbs`/`TestUserConfigDir` (0, 1 each). The same-converter run-to-run spread is 3–4
tests and the outcome distributions coincide (base run 2 landed on 125 pass · 14 fail · 3 infra —
identical to the fix's run 1). **The lesson for the next arc: a single `os` run cannot attribute a
one-test delta.** Pair every claim with a control run of the unchanged converter.

### The blocking-pipe family — CLOSED 2026-08-02 (r37-poll), and it was never `internal/poll`

**Measured, `-test-action all -test-timeout 35m`, three runs on the fixed tree: 165 agreeing of 178
(twice, identically) against the banked 164, with 177 reached.** The whole blocking-pipe family flips
from HANG to PASS — `TestPipeCloseRace`, `TestPipeIOCloseRace`, `TestFdRace`, `TestFdReadRace`,
`TestCloseWithBlockingReadByFd`, `TestCloseWithBlockingReadByNewFile`, `TestClosedPipeRaceRead`,
`TestClosedPipeRaceWrite` — and no run leaks an `os.tests.exe`.

**The conversion of `internal/poll` was faithful all along, and so was everything under it.** Probed
bottom-up rather than reasoned about: `syscall.CancelIoEx` really does abort a blocking `ReadFile` on
a `CreatePipe` handle through the converted trampoline (a `syscall`-only program reproduces Go's
`ERROR_OPERATION_ABORTED` exactly), and `FD.Read` really does return Go's `read |0: file already
closed`. What never returned was **`FD.Close`**, parked forever in `runtime_Semacquire(&fd.csema)`
after the reader had already finished — the stack says so directly.

The root is **Go pointer identity**, in two layers, both now fixed and both corpus-wide:

1. **A field promoted through an embedded POINTER was rooted at the OUTER allocation.** `os.File`
   embeds `*file`, so `&f.pfd` reached through `ж<File>` and `&file.pfd` reached through `ж<file>`
   were different pointers where Go has one address. `internal/poll`'s semaphores are keyed by
   pointer identity, so `os.read`'s release and `os.close`'s acquire landed in **different buckets**.
   go2cs-gen now emits the pointer-crossing promoted accessor in a re-rooting shape
   (`instance.@file.of(file.Ꮡpfd)`), golib gains `FieldPtrFunc<T,TElem>` plus the matching
   `of`/`at` overloads, and **no call site changes** — the overload is chosen by the accessor's
   return type. 340 accessors corpus-wide take the new form; a **cross-package** embed keeps the old
   `ref` form by design (its member list comes from metadata and can name fields the inner
   declaration never had — `abi.Type.sysType`, promoted into `runtime.rtype`, has no generated
   accessor to re-root through), and that fallback is fail-loud (CS0117 at the corpus build).
2. **A field reference's SOURCE was compared by object reference, so a two-level `of()` chain broke.**
   `Ꮡo.of(Outer.Ꮡin).of(Inner.Ꮡv)` mints a fresh intermediate box per access, so `&o.in.v == &o.in.v`
   was **false** at depth two (true at depth one) and a `map[*T]V` grew one entry per access.
   `ж<T>.Equals`/`GetHashCode`/`PointerOrderToken` now resolve the source through the chain, the way
   `ReferentObject` already did.

Guards: `PipeCloseUnblocksRead` (goroutine blocked on a pipe read, closer, output-compared) and
`EmbeddedPointerFieldIdentity` (depth-2 equality, `map[*T]V` keying, both spellings of a
pointer-embed-promoted field). Both are deterministic *neutered-fix* controls — on the base tree the
first HANGS outright and the second prints `depth2: false`. Gates: full behavioral suite 535/535 +
505/505 output; CNR byte-identical across all 560 behavioral packages except the two new projects;
`go2cs-stdlib.slnx` 304 projects, 0 errors; `go test ./...` in `src/go2cs` green.

**The control run answered in twenty seconds, and it is worth knowing that it can.** The five-run
lesson above is about attributing a *one-test* delta; when the delta is a hang, the control does not
need to finish — it needs a stack. With the change stashed and go2cs-gen rebuilt at base, `os`'s host
was sampled 20 s in and had **three threads already parked in
`internal.poll.Close` → `runtime_Semacquire`** — `testClosedPipeRace` twice and `TestPipeIOCloseRace`
once, the very tests that pass on the fixed tree — plus `testPipeEOF` in the channel row below. Same
call site, same run, before and after: that is the attribution, at a cost of one build and one sample
rather than another 35-minute measurement.

#### What the fix did NOT do — two board predictions corrected

- **The six child-stdout rows do not follow.** `TestExecutable`, `TestGetppid`, `TestStatStdin`,
  `TestRootDirAsTemp` and `TestStartProcess` still fail with an empty child result, so their root is
  **not** pipe blocking; `TestHostname` passed in one of the three runs and failed in two, which puts
  it in the load-sensitive class rather than either. `TestExecutable` is the sharpest specimen and
  worth rooting next: it re-execs the host with a **relative** `cmd.Path`, `cmd.Dir` set to the
  parent directory, and a **forged `argv[0]` of `"-"`**, then reads `CombinedOutput`. (Its failure
  message also exposes a second, independent gap: Go renders `%q` of an empty `[]byte` as `""`, the
  converted `fmt` renders `[]`.)
- **The new top row is `TestPipeEOF`, and it is a CHANNEL row, not a pipe row.** With the pipe close
  unblocked the test now runs to its end and hangs there — reproducibly, at the identical site in
  both runs that hung (the third run instead died with `Fatal error. Internal CLR error.
  (0x80131506)`). Captured stacks: the test's deferred `<-writerDone` waits while the **writer
  goroutine is parked in `ChanCore<nint>.Recv` inside `channel<T>.GetEnumerator.MoveNext()` — a
  `for range` over a channel the main goroutine has already CLOSED and drained**. A lost wakeup (or a
  closed-and-empty receive that parks), in `golib/channel.cs`, which this lane is fenced from. It
  does **not** reproduce in isolation: a standalone probe of the same shape — buffered channel,
  ranging goroutine that sleeps between receives, sender that closes — terminated 10,000 times out of
  10,000, so it needs the suite's parallel load. Deliver it to the channels lane with the stacks;
  closing it should take `os` to 166.

#### `TestCmdArgs` — the blittable-mirror remedy does NOT apply, and the reason is specific

`syscall.CommandLineToArgv` returns `*[8192]*[8192]uint16` over a block the OS allocated, and the
caller frees it: `defer syscall.LocalFree(syscall.Handle(uintptr(unsafe.Pointer(argv))))`. The
converted wrapper makes a native-address box, so `~argv` reads an `array<ж<array<uint16>>>` **struct**
— a managed backing reference plus bounds — out of the pointer block's own bytes, and `(*argv)[:argc]`
then slices with fabricated bounds (`ArgumentException`). Hand-owning it to return a MANAGED
materialization of the block fixes the walk and **breaks the free**: for a `ж<T>` whose pointee is a
Go fixed array, `uintptr(unsafe.Pointer(p))` takes `ж.cs`'s `pinnedArrayData` path and hands back the
real **GC-heap** data address, so `LocalFree` would be asked to free GC memory — the exact
`STATUS_HEAP_CORRUPTION` failure mode `ж.cs`'s own banner records for the
`GetEnvironmentStringsW`/`FreeEnvironmentStringsW` pair. That trades a contained `ArgumentException`
for a process kill, so it was **not** done.

What would close it is a pointer flavor golib does not have: a box that answers ADDRESS questions with
the real native address while answering VALUE questions with a managed materialization — a *snapshot*
pointer, sound precisely for read-only native output blocks. `net/lookup_windows.cs`'s DNS-record walks
are the same shape, so it wants designing with them rather than minting for one test. Scope today is
exactly one test: nothing in the converted stdlib calls `syscall.CommandLineToArgv` (the only other
caller in GOROOT is the vendored `x/sys/windows` copy, which is not converted).

`TestDirectoryJunction` was characterized alongside it and is **not** the same family — no native
block, no free. Its `createMountPoint` helper is TEST code that reinterprets a managed `[]byte` as
`windows.MountPointReparseBuffer` and writes four `uint16` fields through it, then indexes
`&buf.PathBuffer[0]` — a `[1]uint16` inline tail standing in for kernel bytes. That is the raw-metal
fork's stub arm, in code that cannot be hand-owned, exactly as the residual table already recorded.

### FOUND while attributing the above — `t.TempDir()` collides two tests that differ only by CASE

`TestExecution.TempDir()` (hand-owned `src/core/testing/TestExecution.cs`) builds
`<work>/.tmp/<SanitizeName(TestName)>/<seq>`. On a case-insensitive filesystem — the Windows default —
`TestFileReaddir` and `TestFileReadDir` resolve to **one directory**, and `os`'s suite runs both
`t.Parallel()`. Whichever finishes first runs its `Cleanup(() => RemoveAll(path))` and deletes the
other's temp dir mid-test; the loser fails `open …\.tmp\TestFileReaddir\1: The system cannot find
the file specified`. Proven directly: creating `TestFileReaddir\1` makes `Test-Path
TestFileReadDir\1` true and leaves ONE directory, and every leftover run root under
`%TEMP%\go2cs-tests\os\` contains exactly one of the two names, never both — while the *passing*
test in each run is always the one that is present. Pre-existing, in the test HOST rather than in
conversion, and independent of the nil-receiver change (it fired in a base-converter run too); the
fix is to disambiguate the sanitized name (a case-marker suffix, or a per-execution sequence)
rather than trust the test name to be a unique path component. `TestFileReadDir` vs
`TestFileReaddir` is the only collision in `os`; the same generator will collide anywhere Go names
two tests with case-only differences.

### r38-os-fin (2026-08-03) — the premature-EOF root was the SYSCALL SEAM, and `os` lands on ONE residual

**Measured twice, identically, `-test-action all -test-timeout 35m`: 681 of 683 rows agree
(173 of 175 top-level), 1 disclosed, 34 matching skips, 4 capability-excluded, 1 residual.** The
run takes about five minutes now, where the base tree's timed out at 35. Progression across the
whole `os` arc: **48 agreeing → 141 → 158 → 164 → 681-of-683**.

| | base (`85ce6744c`) | r38-os-fin |
|:--|--:|--:|
| host run | **timed out at 35 m**, wedged in `TestPipeEOF` | completes in ~5 m |
| oracle error list | 937 lines (462 name-encoding PAIRS + 13 real rows) | **1** |
| real top-level mismatches | 13 | **1** (`TestWriteStringAlloc`) |
| rows agreeing (all levels) | not measurable — the run never finished | **681 of 683** |
| top-level agreeing | | **173 of 175** |
| disclosed | 1 | 1 (`TestUTF16Alloc`) |
| capability-excluded | 1 | 4 |

#### The root: every managed address handed to native code was a FORMER address

`bufio.Reader.ReadBytes` over a converted `os.Pipe` returning a premature `io.EOF` only under
parallel load — the r37-chanrace handoff, with its probability gradient (`-parallel 1`: 0/4 · `2`:
0/4 · `4`: 1/3 · `8`: 5/5 · `16`: 2/2 · default: 100%) and its two surviving suspects — is
**neither** a handle double-close nor a spurious zero-byte read. It is the `ж<T>` → `uintptr`
conversion, and the file that performs it had the defect written on its own front door:
`syscall/dll_windows.cs`'s soundness note said the argument uintptrs the zsyscall wrappers capture
are TRANSIENT addresses that "golib's ж→uintptr conversion cannot pin across the call", and judged
the window "short and allocation-free".

It is neither, for a BLOCKING syscall. Both operators end in a `fixed` block — a pin that lasts for
one statement — and then RETURN the address as an integer, so the window is not capture→`calli`, it
is capture→**the kernel's write**. `testPipeEOF` parks in `ReadFile` on a pipe for 10 ms per read
while the rest of a parallel suite allocates around it. Measured directly rather than argued: a
`heap(new uint32(), out var Ꮡdone)` box and a `Ꮡ(buf, 0)` element pointer BOTH report a different
address after one forced collection. The kernel then writes to neither — `done` stays 0,
`syscall.Read` returns `(0, nil)`, `internal/poll`'s `FD.eofError` turns that into `io.EOF`.

Every measured property of the row follows: monotone in parallelism (more threads ⇒ more allocation
⇒ more collections inside the same 10 ms window); indifferent to the finalizer bridge (a control had
already ruled that out); and the buffer's half of the same defect — 4 KB written into freed heap —
is the moving-site `ExecutionEngineException` and the `Fatal error. Internal CLR error.` recorded
beside it.

**The fix is golib-only, and it makes the ADDRESS MODEL sound rather than patching a caller.**
`ж<T>`'s `uintptr`/`void*` operators now pin before they read (`EnsureStableAddress`), taking a
lifetime `GCHandle` on the ROOT storage the pointer names — a heap box pins its own value slot, an
element reference the canonical backing array, a field reference recurses to the containing
allocation — on exactly the terms `pinnedArrayData` already used for the fixed-array case. The
enabler is that a standard heap box's value storage is now a one-element array for a `T` that
contains no references (`ж<T>.m_slot`): a box is a class with reference fields and `GCHandle` refuses
to pin anything containing pointers, so the value had nowhere pinnable to live. It is allocated
EAGERLY and never migrated — `heap<T>(out ж<T>)` hands out a `ref` alias before any address is taken,
so moving the storage on first address-take would strand that alias on the abandoned copy, which is
this very bug one level down. A reference-bearing `T` gets no slot and keeps the old transient
address; its C# layout is not a native layout either, so nothing can meaningfully be handed its
address. `RuntimeHelpers.IsReferenceOrContainsReferences<T>()` is a JIT constant, so a managed-`T`
box pays neither the branch nor the allocation. This also makes Go's unsafe.Pointer **rule 3**
(pointer arithmetic through `uintptr`) sound, which it silently was not.

Guard: `src/tests/GolibTests/NativeAddressStabilityTests.cs` — a neutered-fix control across all
four box kinds plus the reference-bearing negative case; with `EnsureStableAddress` removed every
address assertion fails on the first forced collection. Rule in
[`ConversionStrategies-Reference.md`](../ConversionStrategies-Reference.md).

**The gradient closes at every point it was measured at.** Same matrix, three host runs per point,
`TestPipeEOF` counted as a `pass` verdict rather than as the absence of an abort:

| `-parallel` | before (aborts) | after (passes) |
|--:|:--|:--|
| 4 | 1 of 3 | **3 of 3** |
| 8 | 5 of 5 | **3 of 3** |
| 16 | 2 of 2 | **3 of 3** |
| default (24) | 6 of 6, plus 2 of 2 through the pipeline | **3 of 3**, plus 2 of 2 through the pipeline |

**What it closed, in one change: 13 residual rows → 3.** `TestPipeEOF` and the whole
child-stdout family — `TestExecutable`, `TestStatStdin`, `TestHostname`, both `TestStartProcess`
arms, `TestRootDirAsTemp`'s spawn — because an empty child result WAS the same premature EOF, read
through `os/exec`'s pipe. The r37-poll prediction that those six "do not follow" was right about the
pipe-close fix and wrong about the family: they had one root after all, one layer down.

#### The other four rows, each rooted and closed

- **`TestGetppid` — the syscall STRUCT-PASSING seam's third member, and the one that fails SILENTLY.**
  `PROCESSENTRY32W` is 568 bytes ending in `szExeFile[260]` INLINE; the converted `ProcessEntry32`
  holds that as one `array<uint16>` reference, so the record is ~56 bytes and every field past
  `th32DefaultHeapID` reads from the wrong offset. Nothing faults — the kernel writes 568 bytes over
  a 56-byte object and the caller reads whatever lands — so `syscall.Getppid` answered **0**. Same
  remedy as `GetTimeZoneInformation` and `findFirstFile1`/`findNextFile1`: a blittable mirror + direct
  P/Invoke + field-for-field copy back (`syscall/zsyscall_windows_impl.cs`), with `Process32First`/
  `Process32Next` added to `manualConversionFuncs`. `dwSize` is an INPUT the mirror owns too — Go sets
  it from `unsafe.Sizeof(procEntry)`, which is the MANAGED size here. **The seam is now 6 wrappers,
  not 8.** ⚠ A quiet wrong ANSWER is the worst shape this class takes; the crash cases at least
  announce themselves.
- **`TestRootDirAsTemp` — the host's isolation must not depend on an environment variable the suite
  can rewrite.** The test re-execs the binary with TMP/TEMP pointed at a deliberately UNMOUNTED drive
  root (`findUnusedDriveLetter` picks a letter *because* `os.Stat` says it is not there). Go's test
  binary needs no scratch space; this host does, and it died in startup with
  `DirectoryNotFoundException` before running a test, which the parent read as a child that produced
  nothing. `TestHost.CreateRunDirectory` now tries the temp path first and falls back to
  `AppContext.BaseDirectory`, which exists by construction because the host is running out of it.
- **`TestStatLxSymLink` — NOT load-sensitive; Go retries Windows sharing violations and we did not.**
  Recorded on this board as an intermittent member of the load-sensitive family; on the fixed tree it
  reproduced **3 runs of 3** (`ERROR_SHARING_VIOLATION` on the `t.TempDir()` directory, which a WSL
  child had been run inside). Go's own `testing.removeAll` retries `ERROR_ACCESS_DENIED` and
  `ERROR_SHARING_VIOLATION` for ~2 s with jittered backoff (go.dev/issue/50051, /51442); the shim did
  not, making it reproducibly less tolerant than the runtime it stands in for. Now it does, with Go's
  timeout and backoff. 2 runs of 2 clean afterwards. **The general lesson: "intermittent" is a
  hypothesis, not a classification — this one was deterministic once the rows in front of it cleared.**
- **`TestReadStdin`'s 462 subtests — the NAME-ENCODING artifact is gone.** `TestExecution.SanitizeName`
  folded every non-printable rune to U+FFFD where Go's `testing.rewrite` emits the `strconv.QuoteRune`
  body (`\x1a`), and a subtest's NAME is what the oracle pairs by — so each of the 462 became a
  matched pair of one-sided rows, 924 lines that read like a mass failure on a top-level test that
  AGREED. `SanitizeName` is now Go's rewrite: `isSpace` → `_`, `unicode.IsPrint` decides, non-printable
  takes the Go escape. `TempDirName` folds the backslash the escape introduces, since a name is also a
  path component. Guarded by `TestingRuntimeTests.SubtestNamesEscapeNonPrintableRunesTheWayGoDoes`.
  (The `TestFileReaddir`/`TestFileReadDir` case-collision this board also records was already closed by
  `TempDirName`'s per-name hash.)

#### Capability-exclusions — the three sanctioned by the 2026-08-02 ruling, implemented

`unsupportedRuntimeCapabilities` now maps a SYMBOL to the NAME of the capability it requires, so the
manifest, the comparison and the proof page show *"relocatable single-file test executable"* rather
than a bare symbol. A key may name the test DECLARATION itself, which `requiredFor` honors by gating a
listed function on its own account — the shape a HOST capability takes, since nothing NAMES a test and
the caller-side arm can therefore never record it.

| Test | Capability | Key |
|:--|:--|:--|
| `TestCmdArgs` | native output block with caller-side `LocalFree` | `syscall.CommandLineToArgv` |
| `TestDirectoryJunction` | raw-metal struct overlay on managed bytes | `os_test.createMountPoint` |
| `TestRemoveAllWithExecutedProcess` | relocatable single-file test executable | `os_test.TestRemoveAllWithExecutedProcess` |

**§9 roster scan, with positive control.** All 72 validated packages' `_test.go` files scanned for the
three keys: **zero hits**. Controls fired: `AllocsPerRun` finds 18 of the same 72 (so the loop and the
paths resolve), and `os` itself — deliberately off the roster — hits all three. Guarded by
`TestUnsupportedRuntimeCapabilityGate` (the lookup answers with the capability, stays package-scope,
and every entry must name one) and `TestUnsupportedRuntimeCapabilityGatesTheDeclarationItself` (the
self-gating arm, with an unlisted sibling as the negative control).

#### The ONE residual — `TestWriteStringAlloc`, and it is honestly a residual

`AllocsPerRun` bounded at ZERO, measured **9184 bytes** per `f.WriteString(…)`. Not a disclosure
candidate — ruling #1 of 2026-08-02 stands, a want-zero assert is satisfiable and disclosing it would
soften the doctrine the badges depend on — and not a capability exclusion either, since nothing here is
unownable. It is a real divergence with a known shape and no cheap fix: Go's `WriteString` avoids the
copy with `unsafe.Slice(unsafe.StringData(s), len(s))`, while the converted path allocates a
`PinnedBuffer` + box for `StringData`, then pays the `func<T>((defer, recover) => …)` closure and defer
context of `os.File.Write` and `internal/poll.FD.Write`, then the syscall's own boxes. ~~The defer
machinery dominates, so this is the `sstring`/`GoFunc` performance arc, not an `os` row.~~ It moved from
9088 to 8856-9184 bytes across the arc — noise, not regression.

> ⚠ **RETRACTED by r39-osalloc (2026-08-03): the defer machinery does NOT dominate — it is 440 of
> 9,208 bytes, under 5 %.** The sentence above was an attribution, never a decomposition, and it named
> a component costing a twentieth of the bill. 62 % was two silent allocations inside `ж<T>`: `IsNull`
> boxing the whole pointee on every dereference (4,760 B) and `of(…)` minting its untyped accessor
> wrapper per call (968 B). Both are fixed; see the *r39-osalloc* sub-section below for the byte-exact
> decomposition and the arc that owns the rest.

**`os` is therefore an honest NEAR-BANK: every row accounted — 681 agreeing, 1 disclosed, 34 matching
skips, 4 capability-excluded — with exactly one real divergence, stable across two identical pipeline
runs and five direct host runs.**

⚠ **Owed to the rebank: every proof page's *Excluded declarations* preamble is one sentence out of
date.** The generator now says a declaration may need "a capability the managed runtime does not
provide — a `testing` member the host has not implemented, or a platform behavior it provably cannot
reproduce", because a runtime capability is no longer hypothetical. The 72 committed pages still carry
the old "a testing capability the host does not yet provide". Regenerating them here would mean
banking 72 pages that also carry a fresh date/converter stamp — a partial rebank by another name — so
they are RESTORED with the rest of the sweep's drift and will level at the scheduled whole-corpus
regen (ruling #6). The per-entry text, which is the substance, is already correct in the pages that
have such an entry.

### r39-osalloc (2026-08-03) — the 9,184 decomposes, and it was NOT the defer machinery

r38 attributed the residual to the `func((defer, recover) => …)` closure and defer context of
`os.File.Write` / `internal/poll.FD.Write`, and filed it against "the `sstring`/`GoFunc` performance
arc". **That attribution was plausible and wrong.** Decomposed to the byte, the defer machinery is
**440 of 9,208 bytes — under 5 %**; 62 % was two silent allocations inside `ж<T>` itself, both of
which are now gone. The lesson generalizes: an attribution that was never *decomposed* is a
hypothesis, and this one sent the fix at a component costing a twentieth of the bill.

**Method (reproducible).** A console probe references `core/os`, `core/syscall`, `core/internal/poll`
and `golib` and measures `GC.GetAllocatedBytesForCurrentThread` deltas across N calls — the same
instrument the `AllocsPerRun` shim uses, so the numbers ARE the ones the test sees. A temporary
`AllocMark` slot table (begin/end pairs with depth suppression, so nesting is charged once) was
threaded through every frame of `WriteString → File.Write → file.write → FD.Write → syscall.Write →
WriteFile`, plus per-`TElem` buckets inside `ж<T>.of` and `ж<T>.Value`. The instrumentation is
temporary by construction and was reverted; what survives is the arithmetic, which **closes exactly**
at every level — the ibyteseq standard. (The probe reads 9,208 where the pipeline read 9,184: the
probe writes to its own file rather than the host's `t.TempDir()` one, a 24-byte difference in the
path taken above `File.Write`. The two agree to the byte AFTER the fix, both at **3,168** — the
figure the pipeline now prints in `expected 0 allocs for File.WriteString, got 3168`.)

| Cost | B/op | Share | Root |
|:--|--:|--:|:--|
| `ж<T>.IsNull` boxing the pointee on every standard-box deref | **4,760** | 51.7 % | `m_val is null` on an unconstrained `T` compiles to `box !T` — 8 × a 592-byte `os.file` copy, + 24 for one `os.File` |
| `of(…)` minting the untyped accessor wrapper per call | **968** | 10.5 % | display class + delegate, 88 B × 11 field pointers |
| the `ж<T>` boxes themselves | 1,488 | 16.2 % | 11 boxes; `ж<FD>` alone is 608 B because a field-ref box still carries an inline `m_val` of the pointee type |
| syscall seam | 1,048 | 11.4 % | `heap(new uint32())` 136 · `Ꮡ(buf,0)` 152 · 3 × `new unsafe.Pointer` 664 · `procWriteFile.Addr()` 96 |
| `GoFunc` + defer machinery | 440 | 4.8 % | `func<>` object + closure + delegates 224 · defer delegates 128 · the `Stack<Action>` 88 |
| `unsafe.Slice(unsafe.StringData(s), len(s))` | 136 | 1.5 % | `PinnedBuffer` + `ж<byte>`; free in Go |
| loop/slice residues | 368 | 4.0 % | |

**The two fixes, both golib-only, both pure defect removal** (detail and the emitted-form rule:
[`ConversionStrategies-Reference.md`](../ConversionStrategies-Reference.md) *Reading a pointer and
taking a field pointer allocate NOTHING*). `IsNull`'s value-peeking term is now guarded by a per-`T`
`s_valueCanBeNull` — the question is only answerable for a reference type or a `Nullable<>`, and for
everything else evaluating it boxed the whole pointee for a constant-false answer; the guard also
made the peek read the right slot, correcting `ж<Nullable<T>>` (latent — Go has no `Nullable`).
`of(…)`'s untyped wrapper is a pure function of the accessor, and the accessor is a compiler-cached
static method group, so the wrapper is now memoized per accessor in a weak-keyed table.

| probe measurement | before | after |
|:--|--:|--:|
| `os.File.WriteString(s)` | 9,208 B/op | **3,168 B/op (−65.6 %)** |
| `os.File.Write(b)` | 9,072 | 3,032 |
| `syscall.Write(h, b)` | 1,072 | 784 |
| `ж<Mutex>.Value` (a field-pointer deref) | 592 | **0** |

Guard: `GolibTests.PointerDereferenceAllocationTests`, a neutered-fix control — with the fixes removed
it reports 528 B/deref for a 512-byte pointee, 288 for a reference-bearing one, 32 through a
field-pointer chain, and 200-vs-112 B/call for `of(…)` against a bare box of the same type.

#### `TestWriteStringAlloc` still does not reach zero — and the reason is architectural, not a missing fix

3,168 bytes remain and **none of them is waste**; each is the current model charging for something Go
gets from its compiler. `os` therefore does **not** bank on this row, ruling #1 still stands (a
want-zero assert is satisfiable in principle, so it is not a disclosure), and the honest statement is
that the row is an ARC, not a defect. The arc, in descending value, with what each item would cost:

1. **`ж<T>` serves four box kinds from one class (1,488 B, 47 % of the remainder).** A field-reference
   box, an element box and a native-address box all carry an inline `m_val` slot of the pointee type
   that they never read — `ж<FD>` is 608 bytes for a *pointer*. They also each carry BOTH
   `m_structFieldRef` (a `Nullable<(object, Delegate, Delegate)>`, 32 B) and `m_arrayIndexRef`
   (`Nullable<(IArray,int)>`, 24 B) although the kinds are mutually exclusive. Two independent moves:
   flattening the two nullable tuples into four plain fields is contained and worth ~28 B per box
   (~308 B here); removing the inline `m_val` from the three non-standard kinds needs the class split
   into per-kind subclasses, or `m_val` moved into `m_slot` unconditionally — which would ADD an
   allocation to every standard box, so it is a real trade and wants the whole-corpus measurement
   before it is taken. Blast radius: every converted package. **Chip-class, design-WITH-user.**
2. **`uintptr(unsafe.Pointer(x))` materializes a dead `Pointer` object (496 B here, 15.7 %).** The
   converter emits `(uintptr)new @unsafe.Pointer(x)` for Go's most common syscall idiom; the object is
   provably dead — the ctor takes `(uintptr)x` and the cast reads it straight back. A converter
   peephole would remove three allocations from EVERY zsyscall wrapper in the corpus. This is the
   cheapest remaining increment and the one with the widest reach outside `os`; it was deliberately
   NOT taken in this lane because it is a different change class (converter → CNR + corpus build +
   goldens) and would have made the A/B footprint non-minimal for a row that cannot bank either way.
3. **`GoFunc` is a heap frame (440 B, 13.9 %).** The `func<T>((defer, recover) => …)` shape costs a
   `GoFunc<T>`, a display class, the body delegate, one delegate per `defer`, and a `Stack<Action>`
   on the first registration. Go's `defer` record is stack-allocated and, since Go 1.14, usually
   *open-coded* into the frame. The managed analogue is a `ref struct` frame with the defers in
   inline fields — which cannot hold the body as a lambda, so it is an EMISSION change (the converter
   would have to emit the body as a local function taking `ref` to the frame). **Chip-class**; do not
   attempt it as a golib-local edit.
4. **The syscall seam boxes the arguments (288 B beyond item 2).** `heap(new uint32(), out Ꮡdone)` is
   Go's `var done uint32; &done` — a stack variable in Go, a heap box here — and `Ꮡ(buf, 0)` is
   `&buf[0]`. Both fall out of item 1 if a pointer stops being a class.
5. **`unsafe.StringData` pins eagerly (136 B).** It builds a `PinnedBuffer` view over the string's
   bytes so the pointer has a stable address. Since r38, `ж<T>`'s address operators pin on demand
   (`EnsureStableAddress`), so the eager pin is no longer load-bearing: returning an element reference
   into the string's own backing array would drop the `PinnedBuffer`, make
   `unsafe.Slice(StringData(s), len(s))` a true aliasing window (which is what Go's does), and give
   `StringData(s) == StringData(s)` for free. Small, principled, and touching a hand-owned file with
   subtle empty-string history — worth doing WITH the item-1 work rather than alone.

**What this lane changes about `os`'s accounting: nothing.** The row still diverges, so `os` stays at
681 of 683 agreeing + 1 disclosed + 34 matching skips + 4 capability-excluded, with one real
divergence — now measured at 3,168 bytes instead of 9,184, and rooted rather than attributed.

## `encoding/gob` — build blocker CLOSED; first real census: 86 of 106 match (2026-08-02, r37-gob)

`gob` had never been measured. `package_info_internal_test.cs` emitted
`[assembly: GoImplement<gob_internal_test_package.Point, Pythagoras>]` — the EXTERNAL suite's pair
anchored at the BRIDGE, where `Pythagoras` (declared only in `example_interface_test.go`) is not in
scope. One `CS0246`, therefore no test host, therefore all 106 verdicts read empty: a missing host
masquerading as mass runtime failure, and the reason `DESIGN-reflection-bridge.md`'s "gob 79/98"
residue list could not be re-measured.

**Root — test-project-model record anchoring (the `splitWhiteboxVariantRecords` family), not
reflection.** The bridge's declared-name set is a set of SIMPLE names, and the two `-tests` variants
are separate Go packages free to declare the same one: gob declares `Point` in `codec_test.go`
(`package gob`, implementing the internal `Squarer`) and again in `example_interface_test.go`
(`package gob_test`, implementing `Pythagoras`). Each variant's records are split as that variant
converts, and every cross-variant reference is routed by `go/types.Object` identity to a
CLASS-QUALIFIED spelling — so a BARE name recorded by the external suite is external-declared *by
construction*. The set is now consulted only while splitting the BRIDGE variant's own records, and
the emission mirror that names an adapter through its record's anchor carries the identical gate, so
the two cannot disagree. Write-time qualification could not have repaired it: it roots an ambiguous
bare name at the file it is ALREADY being written into, so a mis-anchored record merely comes out
qualified to the wrong variant. Rule:
[`ConversionStrategies-Reference.md`](../ConversionStrategies-Reference.md), *A BARE record name
resolves in the variant that RECORDED it*; guard
`TestSplitWhiteboxVariantRecordsResolvesBareNamesInTheRecordingVariant` (a fixture module declaring
`Point` in both variants, asserting the collision through the real `go/types` scan before exercising
either split). The fix is test-model-only — verified, not asserted: CNR is byte-identical across all
558 behavioral packages, and the whole-stdlib A/B reconvert shows it changing no production file.

**First measurement** (`go2cs -tests -test-action all -test-timeout 20m`, one run, **zero empty
verdicts**): **86 of 106 match** — C# 81 `pass` + 5 `skip` against Go's 101 `pass` + 5 `skip`; 19
declarations capability-excluded, 0 disclosed. The 20 mismatches reach seven roots, none of them
new-and-unrooted:

| Root | Tests | Note |
|:--|:--|:--|
| **A pointer REINTERPRET used as a VALUE boxes a copy** | `TestGobEncoderField`, `TestGobEncoderNonStructSingleton`, `TestGobEncoderPointerThenValue`, `TestGobEncoderValueThenPointer`, `TestGobEncoderValueEncoder` (5) | The largest single root, and precisely located. `Gobber.GobDecode` writes back through a reinterpreted named-type pointer — `fmt.Sscanf(string(data), "VALUE=%d", (*int)(g))` — which emits `fmt.Sscanf(…, Ꮡ((nint)(g)))`: the POINTEE is converted to a value and *that temporary* is boxed, so `Sscanf`'s write lands in a throwaway box and `g` never changes ("expected '23 got 0"; `TestGobEncoderValueEncoder` NREs on the unwritten value instead of mismatching). The managed-reinterpret route (`Reinterpret<U>()`, which aliases the source box) exists and is correct — but `reinterpretManagedEmission` is reached only when `context.isPointerCast` (the conversion is the operand of a deref) or the source is a RAW address. A `(*U)(p)` whose result is used as a VALUE — passed as an argument — satisfies neither and falls through to the ordinary value-conversion path. **Reinterpret area ⇒ chip-owned; recorded, not fixed here.** Fifth sighting of the address-of-copy-boxing shape, one base shape per fix. |
| **`GobDecode` write-back for a named-ARRAY pointer receiver** | `TestGobEncodeIsZero` (1) | `isZeroBugArray [2]uint8`'s `GobDecode` writes `a[0]`/`a[1]` through the pointer receiver, and the embedded `time.Time` decodes the same way; the round-trip returns `[0 0]` and a zero `Time` where Go returns `[1 2]` and `time.Unix(1e9,0)`. The direct-field-write case (`ByteStruct`) passes, so `Value.Addr`'s write-back path is sound — this is the element/receiver storage shape, adjacent to the root above. |
| **Reflection bridge** | `TestSingletons`, `TestIndirectSliceMapArray`, `TestIgnoreDepthLimit` (3) | Already recorded in `DESIGN-reflection-bridge.md` and now confirmed by measurement rather than inference. `array<T>` does not carry its LENGTH, so a type-only walk sees a slice where the wire says `[7]int` (`gob: decoding into local type *[]int, received remote type [7]int`) and a `[3]int` mismatch for a field declared `[3]int`; `TestIgnoreDepthLimit` is `reflect.ArrayOf` → the `typelinks` stub (a `NotImplementedException`, so it reports `infrastructure-error`, not `fail`). **Chip-owned.** |
| **Typed-nil pointer identity through `any`** | `TestTopLevelNilPointer`, `TestNilPointerPanics`, `TestNilPointerInsideInterface` (3) | `var ip *int` emits `ж<nint> ip = default!`, so `encodeAndRecover(ip)` hands gob a plain null and gob answers `gob: cannot encode nil value` where Go sees a typed `*int` nil and panics "nil pointer". Same shape for the four `mustPanic` cases and for a nil pointer inside an interface ("expected error, got none"). The canonical typed-nil boxing (`ж<T>.NilBox`) exists; a nil pointer VARIABLE's zero value does not reach it. One root, three tests. |
| **A nil deref inside the engine, re-panicked through `catchError`** | `TestEndToEnd`, `TestLargeSlice` + `/byte` + `/struct` (4) | The stack ends at `error.cs:45` — `catchError`'s `throw panic(e)` re-raising a value that is NOT a `gobError`, i.e. a genuine `NullReferenceException` from inside `Encode`/`DecodeValue`, with the original site consumed by `recover()`. Differential worth keeping: `TestLargeSlice`'s `int8` and `string` subtests PASS while `byte` and `struct` fault, so it is shape-dependent, not size-dependent. Unrooted below the recover boundary; the next visit should print before recovering rather than reason about the stack. |
| **Wire-level error-path divergences** | `TestBadData`, `TestIgnoreRecursiveType`, `TestOverflow` (3) | `TestBadData` case #8 gets `gob: bad data: field numbers out of bounds` where Go reports `exceeds input size`; `TestIgnoreRecursiveType` gets that same message on a stream Go accepts; `TestOverflow` produces no range error for **complex64** only (every int/uint/float width matches). Small, separable, and each names its own expected string. |
| **`unique`'s package initializer** | `TestNetIP` (1) | Two roots stacked in `internal/concurrent.NewHashTrieMap`. The FIRST — a dead deref alias, described below — is **fixed this arc**, and it was neither `net` nor reflection (the r18-era claim that this is `net`'s `sync.OnceFunc` in `fd_windows` is retracted; that is not on the stack). Fixing it MOVED the error site rather than greening the test: `NewHashTrieMap` now fails one line later with `ArgumentException: Delegate to an instance method cannot have null 'this'` at `keyHash: new Func<…>((~mapType).Hasher)`, i.e. `abi.TypeOf(m).MapType()` over a zero map yields a descriptor with no hasher. That second root is the descriptor surface — **chip-owned**. |

**The first `TestNetIP` root, fixed: a dead deref alias kept alive by a NAMED-ARGUMENT LABEL.**
`TestNetIP` reported `TypeInitializationException` for `go.net.netip_package` → `go.unique_package`
→ a nil deref in `internal/concurrent.newIndirectNode`. Go's
`newIndirectNode(parent *indirect) { return &indirect{node: …, parent: parent} }` never dereferences
`parent`, but the converter's alias-liveness scan is a whole-word TEXT match over the converted body
and the composite literal's field key emits as the C# named argument `parent: Ꮡparent` — so the
LABEL matched, the alias survived as a dead local, and `ref var parent = ref Ꮡparent.Value`
dereferenced the box at entry. `NewHashTrieMap` builds its ROOT node with `newIndirectNode(nil)`, so
`unique`'s package initializer threw and took `net/netip` and every dependent with it. The scan now
excludes a named-argument label (`isNamedArgumentLabel`); rule and A/B in
[`ConversionStrategies-Reference.md`](../ConversionStrategies-Reference.md), *A pointer parameter
used only through its box gets no deref VALUE alias*, guarded by the extended
`NilPointerParamUnsafePointer` behavioral test (the composite-literal shape plus a dereferencing
positive control). Whole-stdlib A/B: **39 files**, every hunk one removed dead `ref var` line and
nothing else; the reconverted corpus builds **304/304, 0 errors**.

⚠ **It moved the site, it did not green the test** — the charter's root-cause-layering warning, in the
wild again. Proving even that much needed the dependency regenerated: a `-tests` run regenerates only
the package under test, so gob's first re-measurement still linked the COMMITTED
`internal/concurrent/hashtriemap.cs` and reproduced the original stack verbatim. Overlaying that one
file from the reconvert is what showed the `newIndirectNode` frame gone and the next root exposed. gob's
verdict split is **identical before and after** (86/106) for exactly that reason; the value banked here
is the general converter defect and its 39-file corpus footprint, not a verdict.

gob does **not** bank (86 of 106), so the roster is unchanged and no gob artifact is committed.

## `encoding/gob` re-measured: **88 of 106**, and four of the seven roots above were mis-attributed (2026-08-03, r38-gob-fin)

Re-run on the same command (`-tests -test-action all -test-timeout 20m`, zero empty verdicts): **88 of
106** — C# 83 `pass` + 5 `skip` against Go's 101 `pass` + 5 `skip`; 19 capability-excluded, 0 disclosed.
The **+2** is `TestGobEncoderField` and `TestGobEncoderNonStructSingleton`, greened by the
aliasing-reinterpret converter fix below. The other 18 rows re-bucket to **seven** roots, and the
re-bucketing matters more than the +2: three separate rows above were one root, and it is not the row any
of them named.

| Root | Tests | Owner |
|:--|:--|:--|
| ~~**`reflect.Value.IsZero` is wrong for a named STRING and for an ARRAY**~~ — **CLOSED (increment 8); it was wrong for EVERY array and EVERY struct** | `TestGobEncoderPointerThenValue`, `TestGobEncoderValueThenPointer`, `TestGobEncoderValueEncoder`, `TestGobEncodeIsZero` (4) | reflect bridge — **chip, LANDED** |
| ~~**`reflect.Value.Grow` nil-derefs**~~ — **CLOSED (increment 8)** | `TestLargeSlice` + `/byte` + `/struct` (3 rows) | reflect bridge — **chip, LANDED** |
| **Typed-nil identity through `any`** | `TestTopLevelNilPointer`, `TestNilPointerPanics`, `TestNilPointerInsideInterface` (3) | converter — **LANDED r39 (r39-nilcomplex); 2 of 3 closed, the third re-rooted to the bridge** |
| **`array<T>` carries no LENGTH** | `TestSingletons`, `TestIndirectSliceMapArray`, `TestEndToEnd` (3) | reflect bridge — **chip** |
| **`reflect.ArrayOf` → the `typelinks` stub** | `TestIgnoreDepthLimit` (1) | reflect bridge — **chip** (reports `infrastructure-error`) |
| **The decoder's IGNORE path rejects a valid field number** | `TestBadData` #8, `TestIgnoreRecursiveType` (2) | gob decode path — **unrooted** |
| **`MapType().Hasher` over a zero map** | `TestNetIP` (1) | reflect bridge — **CLOSED 2026-08-03 by the ruled `internal/concurrent` hand-own (see the r39d section at the end of this file); `TestNetIP` still fails, on the linkname-PUSH root now behind it** |
| ~~**An untyped complex constant narrows to complex64**~~ — **CLOSED r39 (r39-nilcomplex)** | `TestOverflow` (1) | converter — **LANDED** |

**Root 1 — four tests, one root, and it is an ENCODE-side skip, not a decode write-back.** r37 read the
three `TestGobEncoder*Value*` failures as residue of the reinterpret row and `TestGobEncodeIsZero` as a
separate "`GobDecode` write-back for a named-ARRAY pointer receiver", reasoning from `ByteStruct` passing
that "`Value.Addr`'s write-back path is sound". `ByteStruct` is reached through a POINTER field
(`GobTest0{17, &ByteStruct{'A'}}`), so it never exercised `Value.Addr` at all — and a direct probe shows
`reflect.Value.Field(i).Addr()`, including a reinterpret through it, writes back correctly in C#. The
actual root is one line up, on the ENCODE side: `gobEncodeOpFor`'s `if !state.sendZero && v.IsZero() {
return }`. Probed directly against `go run`:

| value | Go | C# |
|:--|:--|:--|
| `NS("val")` (`type NS string`) | `IsZero=false Len=3` | **`IsZero=true Len=0`** |
| `"val"` (plain string) | `IsZero=false Len=3` | `IsZero=false Len=3` |
| `[2]uint8{1,2}` | `IsZero=false` | **`IsZero=true`** |
| `NA{1,2}` (`type NA [2]uint8`) | `IsZero=false` | **`IsZero=true`** |
| `NI(3)`, `NB("ab")` (named int / named slice) | correct | correct |

So gob omits the field from the wire entirely and the decoder leaves the zero value — visible as
`v = "", want "forty-two"` for the VALUE fields while the POINTER fields of the same type pass, and as
`TestGobEncodeIsZero`'s `[0 0]` where Go has `[1 2]`. A minimal `gob.Encode` probe confirms it at the
byte level: Go's wire carries `\x01\tVALUE=val\x01\tVALUE=ptr`, C#'s only `\x02\tVALUE=ptr`. In the
converted `reflect/value.cs` the String arm delegates to `v.Len()` (broken for the `[GoType("str")]`
wrapper — it sees the wrapper struct, not the underlying `@string`) and the Array/Struct arms take
raw-memory shortcuts (`typ.Equal(…)` against `zeroVal`, `isZero(unsafe.Slice(v.ptr, size))`) that cannot
mean anything in the managed model. **Chip-owned; recorded, not touched.**

**Root 2 — `reflect.Value.Grow`, and it is SIZE-dependent, not shape-dependent.** r37 kept the
`int8`-passes/`byte`-faults differential as evidence of shape-dependence. It is a threshold: `[]byte`
round-trips fine at 1 MiB and faults at ≥ 10 MiB, which is `internal/saferio`'s `chunk = 10 << 20`. Above
it gob only partially allocates and grows incrementally — `decUint8Slice` (decode.go:387) and
`decodeArrayHelper` (decode.go:553) both call `value.Grow(1)` — and `reflect.Value.Grow` nil-derefs. The
four-line probe is decisive on its own: `reflect.ValueOf(&s).Elem().Grow(1)` on a `[]byte` prints
`len/cap 4 8` in Go and panics in C#. `int8` and `string` pass only because their `decHelper` fast paths
(`decInt8Slice`, `decStringSlice`) return before the Grow loop. The stack that "ends at `catchError`'s
`throw panic(e)`" is genuine but says nothing; the probe is what roots it. **Chip-owned.**

**Root 3 — typed nil, rooted precisely, and deliberately NOT landed here.** `var ip *int` emits
`ж<nint> ip = default!` — a plain C# `null` — so boxing it into `any` yields interface-nil, and
`encodeAndRecover(ip)` gets `gob: cannot encode nil value` where Go sees a typed `*int` nil. The control
that names the root exactly: `ip2 := (*int)(nil)` emits `((ж<nint>)nil)`, goes through golib's canonical
`ж<T>.NilBox`, and probes IDENTICAL to Go (`kind=ptr isnil=true type=*int`). A nil pointer FIELD has the
same defect (`st.P` → interface-nil); a nil MAP is already correct. So the canonical typed-nil
representation exists and works, and the gap is only that a pointer VARIABLE's (and field's) zero value
never reaches it. Two candidate remedies — emit `ж<T>.NilBox` for a pointer variable's zero value, or
box at the interface-conversion boundary (`box ?? ж<T>.NilBox`) — and **both change emission at every
pointer declaration or every pointer→interface conversion in the corpus**, i.e. a change whose gate is
the full 71-package validated sweep plus a corpus rebuild, not something to land at the tail of an arc
for three tests. Handed on rooted rather than half-gated (charter §2/§5).

**Root 6 — the two IGNORE-path rows share a symptom and a reproducer.** `TestBadData` #8 (expected
`exceeds input size`) and `TestIgnoreRecursiveType` (a stream Go accepts) both die with
`gob: bad data: field numbers out of bounds`, and both decode into `nil` — the ignore path. The
converted `ignoreStruct` is faithful line-for-line, so the divergence is upstream, in how the ignore
ENGINE is compiled for a self-referential type: `fieldnum >= len(engine.instr)` rejects a field number Go
accepts. `TestIgnoreRecursiveType`'s 36-byte `data` literal is a complete standalone reproducer. Not
reflect; unrooted below the engine compile.

**`TestEndToEnd` moved rather than greened** — the charter's root-cause-layering warning again. It was an
NRE below `catchError`; it now reports `gob: length mismatch in decodeArray`, i.e. the array-length row,
which the crash had been masking. Counted under root 4, not as a fix.

**Root 7 — `TestOverflow`'s complex64, rooted precisely, and an attempted fix REJECTED by the gate.**
Not a decode-path divergence at all: `complex(math.MaxFloat32*2, math.MaxFloat32*2)` produces a
`complex64` of **+Inf** in C# and 6.8e38 in Go. `UntypedFloat` converts implicitly to BOTH `float32`
and `float64`, so both golib `complex` overloads are applicable and C# prefers the *better conversion
target* — the NARROWER one. gob's `float32FromBits` treats +Inf as legal in both widths, so the decode
produced no range error at all while every int/uint/float width matched. **The general class is worth
more than the row: any golib builtin overloaded on float width silently narrows an `UntypedFloat`
operand.**

The obvious remedy — name the untyped pair explicitly (`complex(UntypedFloat, UntypedFloat) =>
complex128`, Go's default type) — **does not work, and the full behavioral suite is what proved it.**
It made every MIXED call ambiguous: `complex(0D, gHalfPi)` has the float64 overload better on the first
operand and the untyped one better on the second, so neither wins (CS0121 in the
`ComplexConstContext` guard). Completing the set with all four width pairings does not rescue it
either: `UntypedFloat` converts implicitly **in both directions** with `float32` and `float64`, so for
an operand that is neither — `complex(7/2, 0D)`, an `int` — no candidate is strictly better and the
ambiguity simply moves. Overload resolution cannot express this rule; the change was reverted rather
than banked.

The remedy that can work is CONVERTER-side and deterministic: emit each `complex()` argument at the
element width Go's typing gives the call — `complex((float64)(x), (float64)(y))` for a complex128
result, `float32` for complex64 — which is the rule `assignUntypedConstContext` already computes for
literal rendering but cannot apply to a named untyped const (`Δmath.MaxFloat32`) or a constant
expression over one. Its footprint is every `complex()` site in the corpus (math/cmplx above all), so
it wants its own A/B, corpus build and re-validation of the math packages — deliberately not squeezed
in at the tail of this arc.

### `encoding/gob` fixes landed this arc

1. **A pointer REINTERPRET used as a VALUE boxed a copy — CONVERTER, fixed.** r37 located this precisely
   and routed it to the chip as "Reinterpret area". It is not: the shape never reaches
   `reinterpretManagedEmission`'s gate at all, because the `namedToNamed || namedToBasic || basicToNamed`
   re-box arm returns first — which is also why `context.isPointerCast` was a red herring (a *deref* of
   the same conversion took the copy route too). The arm now tries the aliasing emission first. Rule and
   the 14-file / 41-hunk A/B in [`ConversionStrategies-Reference.md`](../ConversionStrategies-Reference.md),
   *These three arms now ALIAS instead of boxing a copy*; guard = the extended
   `NamedNumericPointerReinterpret` behavioral output test (neuter-proven). **The blast radius is far
   larger than gob**: the copy silently broke write-through in `flag` (a parsed flag never reached the
   caller's variable), `crypto/tls` key-share/signature-scheme parsing, `crypto/cipher`'s CBC IV,
   `image/png`'s pooled encoder buffer and `go/types`. Reconverted corpus builds 304/304, 0 errors.
2. **The reference closure's MEMBER-ACCESS edge — CONVERTER (test model), fixed.** Landed for `unique`
   (below); it changes nothing for gob, whose host already linked.

gob still does **not** bank (88 of 106) and no gob artifact is committed.

### `encoding/gob` re-measured: **91 of 106** — both deferred converter items land (2026-08-03, r39-nilcomplex)

Same command, zero empty verdicts: **91 of 106**, 15 mismatches. The **+3** is `TestTopLevelNilPointer`
and `TestNilPointerPanics` (the typed-nil boundary) and `TestOverflow` (the complex width pin). The
remaining 15 re-bucket to **seven** roots, and not one of them is the converter's any more — six are
the reflection bridge (the chip) and the seventh is gob's own decode path:

| Root | Tests | Owner |
|:--|:--|:--|
| **`reflect.Value.IsZero` is wrong for a named STRING and for an ARRAY** | `TestGobEncoderPointerThenValue`, `TestGobEncoderValueThenPointer`, `TestGobEncoderValueEncoder`, `TestGobEncodeIsZero` (4) | reflect bridge — **chip** |
| **`reflect.Value.Grow` nil-derefs** | `TestLargeSlice` + `/byte` + `/struct` (3) | reflect bridge — **chip** |
| **`array<T>` carries no LENGTH** | `TestSingletons`, `TestIndirectSliceMapArray`, `TestEndToEnd` (3) | reflect bridge — **chip** |
| **`reflect.Value.IsNil` on an INTERFACE asks the POINTEE** | `TestNilPointerInsideInterface` (1) | reflect bridge — **chip, NEW, rooted below** |
| **`reflect.ArrayOf` → the `typelinks` stub** | `TestIgnoreDepthLimit` (1) | reflect bridge — **chip** (`infrastructure-error`) |
| **`MapType().Hasher` over a zero map** | `TestNetIP` (1) | reflect bridge — **chip** (`infrastructure-error`) |
| **The decoder's IGNORE path rejects a valid field number** | `TestBadData` #8, `TestIgnoreRecursiveType` (2) | gob decode path — **unrooted** |

**The two that closed.** `TestTopLevelNilPointer` needed only the boundary: `encodeAndRecover(ip)`
now hands gob a typed nil, `reflect.ValueOf` sees kind `ptr` with `IsNil` true, and gob panics
"nil pointer" exactly as Go does. `TestNilPointerPanics` needed one slot more — its table is
`[]struct{ value any; mustPanic bool }{{nilStringPtr, true}, …}`, a POSITIONAL element of a struct
literal whose field is `any`, which the first cut of the boundary did not cover; the rule and its
(zero-site) corpus footprint are in
[`ConversionStrategies-Reference.md`](../ConversionStrategies-Reference.md), *A pointer crossing
into an interface carries its static type*. `TestOverflow` closed on the `complex()` element-width
pin (same reference, *`complex()` over a NAMED untyped constant pins the element width*).

**The one that did NOT, and why — a NEW chip root, rooted with a five-line probe.**
`TestNilPointerInsideInterface` builds `struct{ I any }{I: ip}` and expects
`Encode` to fail with "nil pointer … interface". The converter's half is done and visible in the
emission (`I: ip.OrTypedNil()`), but the C# still reports *expected error, got none*. The reason is
one layer down: **`reflect.Value.IsNil` on an INTERFACE-kind value answers about the POINTEE, not
about the interface.** Probed directly against `go run`:

| | Go | C# |
|:--|:--|:--|
| `reflect.ValueOf(si).Field(0).Kind()` | `interface` | `interface` |
| `…Field(0).IsNil()` | `false` | **`true`** |
| `…Field(0).IsZero()` | `false` | **`true`** |
| `…Field(0).Elem().Kind()` | `ptr` | `ptr` |
| `…Field(0).Elem().IsNil()` | `true` | `true` |

`IsZero` for an interface IS `IsNil` (`reflect/value.cs`'s Chan/Func/Interface/Map/Pointer/Slice/
UnsafePointer arm), so the wrong answer makes gob's `if !state.sendZero && v.IsZero() { return }`
skip the field outright — `encodeInterface`, which is where the expected error lives, is never
reached. It is the same encode-side skip as root 1, from a different wrong predicate, and it is the
bridge's to fix: an interface value's nilness is a property of the interface, not of whatever
pointer it happens to carry. **Chip-owned; recorded, not touched** (the boundary fence).

gob still does **not** bank (91 of 106) and no gob artifact is committed.

## `unique` builds and RUNS for the first time — 0 of 19, one chip-owned wall (2026-08-03, r38-gob-fin)

`unique` had never linked a test host. `handle_test.cs` calls `cleanupMu.Lock()` on the production
package's `var cleanupMu sync.Mutex`, and the `-tests` csproj emitter did not reference `sync` —
`CS0012 … 'sync_package.Mutex'` ×2. **Root: the reference closure was missing its MEMBER-ACCESS edge**
(`declarationClosureImports` covered a named type's interface bases and struct fields, but not the type
of a RECEIVER — resolving `x.M` requires binding x's type, and when x is declared elsewhere that type is
spelled nowhere in the compilation); rule, minimality probe and the recompile-model no-op argument in
[`ConversionStrategies-Reference.md`](../ConversionStrategies-Reference.md), *The third closure edge — a
MEMBER ACCESS*. Test-model only, and **zero-drift**: regenerating all 73 banked `.tests.csproj` changes
exactly one line, unique's own `sync` reference.

⚠ The minimality probe is not ceremony — it rejected two successive forms of this rule that a reading
of C#'s binding rules would have justified. "The type of every var/const/func the compilation NAMES"
drifts **23 of 73**; narrowing to receivers but still seeding from the production sources drifts **13**.
Both were caught only by running it. (And the per-file scoping is load-bearing: go/packages loads the
INTERNAL test variant with the production files alongside its own, so a per-package gate lets every
production receiver straight back in — the 13-drift form, wearing the fix's clothes.)

First census, with `internal/concurrent/hashtriemap.cs` overlaid from a fresh reconvert (the committed
corpus predates r37's dead-alias fix, and a `-tests` run regenerates only the package under test —
without the overlay all 15 rows report r37's already-fixed `newIndirectNode` stack, which reads exactly
like a live defect): **0 of 19** — Go 19 pass; C# 4 `fail` + 15 `infrastructure-error`. Every one of the
15 is the **same** `TypeInitializationException`, and it is the second root r37 already named as
chip-owned: `NewHashTrieMap` → `keyHash: new Func<…>((~mapType).Hasher)` →
`ArgumentException: Delegate to an instance method cannot have null 'this'`, because
`abi.TypeOf(m).MapType()` over a zero map yields a descriptor with no hasher. `unique` is therefore a
**one-root wall**, and that root is the chip's; nothing else about the package is measurable until it
clears. ⚠ **Increment 8 rooted that wall and reported it NOT landable in the bridge** — the hasher's
contract is "hash the value at this address" and a managed address names no value (two boxes holding
equal strings have different addresses; a reference-containing pointee's address moves across a GC).
The recommended remedy is a hand-owned `internal/concurrent/hashtriemap.cs`, which needs an ownership
ruling; see *Increment 8* below. (The 4 `fail` rows are `TestMakeCloneSeq` subtests whose names Go takes from
`reflect.TypeFor[T]().String()`; C# reports that as `""`, so Go's `testString` becomes C#'s `#00` — the
`rtype.String`/`TypeFor` surface, also chip.) `unique` does not bank; the overlaid dependency was
restored, not banked.

**SUPERSEDED 2026-08-03** — the hand-own landed and this census is re-measured at **1 of 19** with the
single wall replaced by five distinct downstream roots; see *`internal/concurrent.HashTrieMap` HAND-OWNED*
at the end of this file. (The `rtype.String`/`TypeFor` chip row above is now known to be the SAME defect as
the third root there: `abi.TypeFor<T>()` returns the descriptor's `Equal` delegate for an interface `T`.)

⚠ **Overlaying a dependency `.cs` is not enough to re-measure it** — `Copy-Item` preserves the source's
LastWriteTime, so the older-than-the-`.dll` copy was skipped by MSBuild and the run reproduced the
ORIGINAL stack verbatim, which reads as "the fix did not work". Touch the file after overlaying.
### RETRACTED — `TestPipeEOF` is NOT a channel row: the channel was never CLOSED (r37-chanrace, 2026-08-02)

The r37-poll handoff recorded `TestPipeEOF`'s post-pipe-fix hang as *"a `for range` over an already
CLOSED, drained channel that never wakes — a lost wakeup, in `golib/channel.cs`"*, and routed it to
the channels lane as the first real channel-semantics defect since wave3. **It is not one.** The
"already closed" half was inferred from reading `testPipeEOF`'s source flow — `close(write)` sits
above the deferred `<-writerDone`, so a main goroutine parked in that defer looks like it must have
closed. Measured instead of inferred, it had not: this is §9's don't-trust-a-plausible-reading trap,
one hop further in.

**The instrument.** `ChanCore<T>.Recv`/`Send`'s park was env-gated onto a timed wait that reports the
core's state and the parked thread's stack once a threshold elapses, plus a line per `closechan`.
That is the cheap general answer to *any* future "a channel never woke" sighting: it distinguishes a
lost wakeup from a close that never ran, in one run, without a debugger.

**What it captured** — identically in both instrumented pipeline runs, and a third time driving the
host directly:

```
STUCK recv core#281  closed=False qcount=0 cap=1  recvqEmpty=False  elem=IntPtr
     channel<T>.GetEnumerator+MoveNext  ←  testPipeEOF's `for i := range write`   (the writer goroutine)
STUCK recv core#280  closed=False qcount=0 cap=0  recvqEmpty=False  elem=EmptyStruct
     GoFunc.HandleFinally → builtin.ᐸꟷ  ←  the deferred `<-writerDone`            (the test goroutine)
```

Both channels **open**. And the cross-check is absolute: across the whole suite run the close log
contains **125 closes, not one of them a `chan int`** — `close(write)` never executed on any core.

**The real control flow.** `rbuf.ReadBytes('\n')` returned **`io.EOF`**, so `t.Fatal(err)` fired at
`pipe_test.go:395`. `Fatal` → `FailNow` → `TestAbortException` unwinds → `GoFunc.HandleFinally` runs
the deferred func → `<-writerDone`. `close(write)` on the line below never runs, so the writer
goroutine ranges over a channel that will never close, and `writerDone` therefore never closes
either. **Real Go deadlocks identically here** — Go's own test code is not hang-safe on that branch;
Go simply never takes it, and its binary-level timeout panic would dump it if it did. The channel
runtime did exactly what Go specifies at every step.

**So the actual `os` row is: `bufio.Reader.ReadBytes` over a converted `os.Pipe` returns a premature
`io.EOF`, and only under parallel load.** Characterized on the r37-poll tree, driving
`os.tests.exe` directly:

| configuration | runs where `TestPipeEOF` aborts |
|---|---|
| `-run TestPipeEOF` alone | 0 of 5 |
| `-run` the whole pipe/fd family | 0 of 3 |
| full suite, `-parallel 1` | 0 of 4 |
| full suite, `-parallel 2` | 0 of 4 |
| full suite, `-parallel 4` | **1 of 5** |
| full suite, `-parallel 8` | **5 of 5** |
| full suite, `-parallel 16` | **2 of 2** |
| full suite, default (`TestOptions.Parallel` = `Environment.ProcessorCount`, 24 here) | **6 of 6**, plus 2 of 2 through the pipeline |

Monotone in the concurrency level and not attributable to one interfering test — every test still
runs at `-parallel 1`, and the abort signature is unmistakable in the host's own output (the whole
suite reports ~650 results and `TestPipeEOF` contributes *no* line at all, because its goroutine
never returns).

⚠ **The knee is a gradient, and an earlier revision of this row got that wrong.** It claimed a
*clean* threshold at 8 — 100% either side — on the strength of only **two** samples at `-parallel 4`.
A host reboot forced the whole measurement to be re-established from scratch, and on the quiet
machine `-parallel 4` aborted **1 of 3**. So 4 is not a safe configuration, it is a low-probability
one, and any future bisection of this row must budget more than two runs per point near the knee.
What the reboot did *not* move is the headline: default parallelism aborts 100% both before and
after (3/3 loaded, 3/3 cold), which is what makes the zero rows at `-parallel 1`/`2` worth trusting
rather than dismissing as luck. One default-parallelism run also died with `Fatal error. Internal
CLR error. (0x80131506)`, the same crash r37-poll saw once; whether that shares the root is open.

**Premature finalization is RULED OUT, by control rather than by argument.** `os.newFile` registers
`runtime.SetFinalizer((~f).file, close)` and `runtime/mfinal.cs`'s native bridge honors it for real —
instrumented, it runs **21 finalizer-driven `close` calls per three suite runs**, which is exactly
the mechanism Go's own `KeepAlive` doc warns about and made a compelling root. It is not this one:
with the bridge disabled outright (`SetFinalizer` registering nothing), `TestPipeEOF` still EOFs
**3/3**. Handle double-close / handle-value reuse across parallel tests, and a spurious zero-byte
read reaching `FD.eofError`, are the candidates left standing.

**The negative control for the channel verdict.** 93,000 racing instances across five shapes —
ranging receiver woken by close, direct hand-off racing close, the `testPipeEOF` choreography
itself, a blocked select woken by close, and select single-fire under contention — under ThreadPool
and GC pressure, **zero hangs and zero invariant violations**. Separately, `testPipeEOF`'s exact
choreography over the REAL pipe/`bufio`/`fmt`/`time` stack (transpiled, not synthetic) completed
200/200 rounds in C# and under `go run`. The select park path was checked as the twin of the
suspected window and is clean on the same evidence. Three of those shapes are now standing guards in
`src/tests/GolibTests/ChannelWakeupStrainTests.cs`; they are neutered-fix controls (with `closechan`
not draining `Recvq`, all three fail as `a parked channel operation was never woken`).

**Owed.** The premature-EOF root goes back to the `os`/`internal/poll` arc with the table above. And
the wedged host is **not reaped**: it outlived `-test-timeout 6m` by minutes and had to be killed by
PID — the leaked-`os.tests.exe` symptom already on this board is this, and Go's binary-level timeout
panic is the behavior the host still lacks.

> **CLOSED 2026-08-03 (r38-os-fin) — and it was neither surviving suspect.** Not a handle
> double-close and not a spurious zero-byte read: the `ж<T>` → `uintptr` conversion returned an
> address whose `fixed` pin had already expired, so a gen0 collection during the 10 ms blocking
> `ReadFile` moved the `*uint32` byte-count box out from under the kernel and `done` stayed 0. The
> gradient this table measured is exactly the probability of a collection landing in that window.
> Full account in the `os` block's *r38-os-fin* sub-section. The wedged-host / no-timeout-panic half
> of this Owed is untouched and still open — it simply stopped firing once nothing hangs.

## Open — the syscall STRUCT-PASSING seam: 6 wrappers still hand a non-blittable struct to the kernel

> **Down from 8 on 2026-08-03 (r38-os-fin):** `Process32First` / `Process32Next` joined the fixed
> set, and correct a claim this section made — the row below reads "reached-and-working", which it
> was not. It failed SILENTLY: `syscall.Getppid` answered **0**, because the kernel wrote a 568-byte
> `PROCESSENTRY32W` over a ~56-byte managed record and the caller read whatever landed. A quiet wrong
> ANSWER is the worst shape this class takes — a fault at least announces itself, and "it did not
> crash" is not evidence a wrapper works.

Named as a class 2026-08-01, after `syscall.GetTimeZoneInformation` became the second member of it
to be hand-owned (the first was `StartProcess`/`_STARTUPINFOEXW`, 2026-07-19). `findFirstFile1` /
`findNextFile1` followed the same day — the first members a real Go test suite *reached* rather than
a census predicted, and the reason `path/filepath`'s `EvalSymlinks` family took the C# test host down
mid-run.

**The class.** A generated wrapper passes `uintptr(unsafe.Pointer(&s))` for a converted struct whose
C# layout is not the native one — any struct holding a golib `array<T>` (Go's inline `[N]T`) or a
`ж<T>` (Go's pointer field) where Windows expects inline bytes or a raw address. The kernel then
writes the NATIVE-sized record over a smaller managed object: heap corruption past its end, and
fabricated object references in the reference-typed fields. It does not fail at the call; it fails at
the next read of one of those fields, usually as an `ACCESS_VIOLATION` deep inside golib. That is why
`time.Now().Weekday()` died in `slice<ushort>..ctor` and not in `GetTimeZoneInformation`.

**Census (`src/core/syscall`, positive control = `Timezoneinformation`): 32 non-blittable structs, 11
wrappers passing one by address** (the earlier count of ten collapsed the
`findFirstFile1`/`findNextFile1` pair into a single row). Three are fixed; the other **eight** are
latent — nothing in the behavioral suite or the 69-package sweep exercises them today:

| Wrapper | Struct | Reached by |
|:--|:--|:--|
| ~~`findFirstFile1` / `findNextFile1`~~ | `win32finddata1` (`FileName`, `AlternateFileName`) | **FIXED 2026-08-01** — `path/filepath.EvalSymlinks` → `toNorm` → `normBase`; guarded by the `FindFirstFileData` behavioral output test |
| ~~`Process32First` / `Process32Next`~~ | `ProcessEntry32` (`ExeFile`) | **FIXED 2026-08-03** — `os`'s `TestGetppid` → `syscall.Getppid` → `getProcessEntry`; the mirror owns `dwSize` too, since Go computes it from `unsafe.Sizeof` |
| ~~`Bind` / `Connect` / `ConnectEx` / `Getsockname` / `Getpeername` (+ the two `sockaddr()` encoders)~~ | `RawSockaddrInet4` / `RawSockaddrInet6` (`Addr`, `Zero`) | **FIXED 2026-08-11 (lane L10)** — `net.Listen` → `listenStream` → `syscall.Bind`; guarded by the `SockaddrRoundTrip` behavioral output test. The first member with TWO defects: a `(*[2]byte)` port alias panicked (`index out of range [0] with length 0`) *before* the struct-passing seam was even reached. Mirrors are stack LOCALS, and no new `[DllImport]` was needed — golib models `unsafe.Pointer` as an address box, so the package's generated `bind`/`connect`/`connectEx` already take any address. ⚠ Hand-owning `RawSockaddrAny.Sockaddr` (the decode) was REJECTED on measurement at L10: its body held the only ΔSockaddr casts in the package, so skipping its emission dropped the `GoImplement` records and made `net` mint duplicate adapters. **TAKEN 2026-08-14 (netpoll S2b)**, once the pointer method-set recorder made those records independent of that body — re-measured on the taking lane's own build (all three records present with the body displaced, `net` referencing syscall's adapters at all seven sites and minting none, A/B footprint exactly one file) and guarded at value level by four new `SockaddrRoundTrip` lines, including an IPv6 address that deliberately crosses the `Addr.Data`/`Pad` boundary. It decodes by FLATTENING the managed struct back to its 116-byte native image and reusing `readNativeSockaddr`, the encoders' own decode — see the `ConversionStrategies-Reference.md` entry |
| ~~`WSARecv` / `WSASend` / `AcceptEx` / `GetAcceptExSockaddrs` / `CancelIoEx` / `WSAGetOverlappedResult` (+ `LoadConnectEx`, and `ConnectEx` extended)~~ | `WSABuf` (`Buf`), `Overlapped` (as the operation's kernel-side IDENTITY), the `AcceptEx` sockaddr block, `GUID` (`Data4`) | **FIXED 2026-08-15 (netpoll S2b)** — every TCP read/write/accept/dial; guarded at VALUE level by the `TcpLoopbackRoundTrip` and `NetDeadlineMatrix` behavioral output tests. The first ASYNC members of this class, and async breaks the mirror-is-a-local rule twice: the kernel keeps the OVERLAPPED and the buffers until COMPLETION (unbounded), and `CancelIoEx` matches BY ADDRESS, so a fresh copy per call would break cancellation. Remedy is a per-operation RECORD keyed by the `ж<Overlapped>` (whose field-reference equality resolves an `of()` chain recursively, so all of `execIO`'s call sites AND separate `FD.Read` calls hit one record), holding the `PreAllocatedOverlapped`, the native staging and the `ж<byte>` boxes whose pins hold the caller's buffers. The completion→waiter signal is pushed through a new platform-neutral golib rendezvous (`GoAsyncIO`), because `internal/poll` references `syscall` and Go's own trick — reading the enclosing `operation` back out of the OVERLAPPED — has no go2cs equivalent. ⚠ `LoadConnectEx` was NOT on this census and the netpoll design recorded it as "already working": it is not. `syscall.GUID`'s `Data4 [8]byte` makes the struct reference-bearing, so `WSAIoctl` compared a CLR auto-layout image and answered `WSAEINVAL` on every host — `failed to find ConnectEx: An invalid argument was supplied`, the shape crypto/tls banked nine times. "Synchronous, therefore fine" is not a safe inference; only a census is |
| ~~`WSAStartup` / `WSAEnumProtocols`~~ | `WSAData` (`Description`, `SystemStatus`, `VendorInfo`), `WSAProtocolInfo` (`ProtocolName`, and the nested `GUID.Data4` / `WSAProtocolChain.ChainEntries`) | **FIXED 2026-08-16 (lane `claude/wsaenum-mirror`)** — `internal/poll`'s `InitWSA`, once per process that imports `net`; guarded at VALUE level by the `WsaProtocolInfo` behavioral output test. NEITHER was on this census (see the ⚠ below), and the pair is the class's **largest overwrite by an order of magnitude**: `WSAPROTOCOL_INFOW` is 628 bytes native with three inline arrays the conversion collapses to references, so the managed record is ~120 bytes — and `checkSetFileCompletionNotificationModes` asks for **32** of them, telling the kernel `unsafe.Sizeof(buf)` = **20,096 bytes** while handing over a ~3.8 KB managed array. `WSAStartup` is the same shape over `WSADATA` (408 native vs ~40 managed) and is UPSTREAM of it: reading `data.Description` after it dies with `ACCESS_VIOLATION` in `slice<byte>..ctor` **before** the enumeration is reached, which is why the two arrived together. ⚠ The `WSAStartup` overwrite has been live since the corpus first dialled a socket and was SILENT only because `net` never reads the `WSAData` it passes — the strongest instance yet of "it did not crash" proving nothing. The enumeration's answer is load-bearing too: it sets `useSetFileCompletionNotificationModes` → `FD.skipSyncNotif`, the IO path the netpoll design's OQ5 ratified keeping. Both mirrors live in `syscall/windows/zsyscall_windows_wsa_impl.cs` (the ws2_32 family, beside `LoadConnectEx`), and `WSAEnumProtocols` adds a third SIZE-IS-AN-INPUT edge after `Process32First`'s `dwSize`: the byte count is also an OUTPUT, rewritten on `WSAENOBUFS` with a required size expressed in NATIVE strides |
| `GetIfEntry` | `MibIfRow` (`Name`, `PhysAddr`, `Descr`) | `net.Interfaces` |
| `getStartupInfo` | `StartupInfo` (`Desktop`, `Title`) | ⚠ NOT `os` startup — corrected 2026-08-02 by the r35-os arc, which ran the whole suite without reaching it. Nothing in `os` calls it; in Go 1.23 the only caller is the public `syscall.GetStartupInfo`, exercised by syscall's own test. `Process32First`/`Next` above ARE reached from `os` (`TestGetppid` → `syscall.Getppid` → `getProcessEntry`) and did not fault, so that row is reached-and-working rather than latent. |
| ~~`GetAddrInfoW` / `FreeAddrInfoW`~~ | `AddrinfoW` (`Canonname`, `Addr`, `Next`) | **FIXED 2026-08-16 (lane `claude/tls-endgame`)** — `net.Dial` → `resolveAddrList` → `LookupPort`, i.e. every converted program that resolves a name or a service; measured first as a process AV from `crypto/tls`'s `TestVerifyHostname`. The class's first LINKED output, and the first where copying the top-level record is NOT enough: `net` reads the sockaddr THROUGH the result, so the whole chain and its sockaddrs are transcribed into managed boxes and the managed pointer is carried across the `unsafe.Pointer` field by `ManagedPointerTokens` (its second minter ever; the reflection bridge was the first). `FreeAddrInfoW` becomes a hand-owned NO-OP, because the native chain is freed eagerly at the copy and nothing native escapes the call. Guarded at VALUE level by the `LookupServicePort` behavioral output test, proven failing-first (`exit code mismatch: C# -1073741819 vs Go 0`). ⚠ Fixing it does NOT make `TestVerifyHostname` pass — `net.adapterAddresses` is the wall behind it; see the entry at the end of this file |
| `CertEnumCertificatesInStore`, `CertFreeCertificateChain`, `CertFreeCertificateContext` | `CertContext`, `CertChainContext` | `crypto/x509` on Windows |
| ⚠ **A SECOND class, censused 2026-08-16 and distinct from this one:** 13 wrappers take a `**T` OUT-parameter and receive **NULL**, silently — `ж<T> → uintptr` answers 0 for a `ж<ж<T>>` whose held pointer is null, which is every out-parameter before the call. Measured through `CertAddCertificateContextToStore` (`err == nil`, `storeCtx == nil`, and `crypto/x509`'s `systemVerify` then nil-derefs). **CLOSED AT THE BOUNDARY 2026-08-17 (lane `claude/x509-cryptoapi`)**: the operator has TWO wrong answers here, not one — a live MANAGED address once the held pointer is non-null, which is worse — and neither is fixable in `ж.cs`, so the remedy is a native out-cell plus a `ValueSlot` publish in the wrapper. 5 of the 13 taken (the SID pair, `NetGetJoinInformation`, and the two crypt32 members), guarded by the `PointerOutParameter` behavioral test; 8 left for stated reasons. ⚠ Attribution correction: `NetGetJoinInformation` and `NetUserGetInfo` are in `syscall`, not `internal/syscall/windows` — the split is 11 + 2. Full disposition in the entry at the end of this file | — | — |

**Remedy, per member:** the established one — a blittable `[StructLayout(LayoutKind.Sequential)]`
mirror with `fixed` buffers for the inline arrays, a direct `[DllImport]`, and an explicit
field-for-field copy at the boundary, declared in `manualConversionFuncs` so the generated wrapper
becomes a placeholder. Worked example: `src/core/syscall/zsyscall_windows_impl.cs`.

⚠ **The census UNDER-COUNTS, twice measured — treat 11 as a floor, not a total.** `LoadConnectEx`
(2026-08-15) and now `WSAStartup` / `WSAEnumProtocols` (2026-08-16) were all three defective members
that this wrapper list never held, even though each passes a struct of exactly the censused shape by
address. Two of them are on the `net` init path, and one of them — `WSAStartup` — has been
overwriting the heap in every converted program that imports `net` since the corpus first dialled a
socket. So a wrapper's ABSENCE from the table below is not evidence it is sound; it is evidence
nobody looked. When a lane reaches a new Winsock/kernel32 wrapper, re-derive the shape from the
struct it passes rather than consulting this list, and add the row.

⚠ **This census is scoped to `src/core/syscall`, and that scope is not the class's boundary.**
`internal/syscall/windows` holds six more wrappers of the same shape — one of them,
`NetShareAdd`/`SHARE_INFO_2`, is FATAL today on any host where the Server service is reachable, and
it is what a real suite reached first. Its table is in *`os`'s "REGRESSION" is a HOST CAPABILITY* at
the end of this file, together with the second failure shape the class takes: the kernel READING a
managed record as a native one, dereferencing a value field as a pointer, and faulting AT the call
rather than arbitrarily far away.

**Do them when a suite reaches them, not speculatively** — each needs its own value-level
verification (a mirror with wrong offsets returns garbage *without* faulting, so "it no longer
crashes" proves nothing; `LocalTimeZone` compares real zone abbreviations and offsets against Go, and
`FindFirstFileData` compares real directory entries — long names ASCII and non-ASCII, 8.3 alternate
names, the directory bit, byte sizes, and a distinct per-entry `LastWriteTime`).
`net` and `crypto/x509` are the two packages that will surface most of the rest.

Two details of the `findFirstFile1` implementation generalize and are worth cribbing for the next
member: the caller's UTF-16 name buffer is pinned with a `fixed` block wrapped **around the call**
rather than handed golib's TRANSIENT `ж`→`uintptr` address, and an inline `WCHAR[N]` buffer is copied
back **whole**, NULs included — Go reads it as `UTF16ToString(buf[:])`, which stops at the first NUL,
and the struct is reused across an enumeration, so a copy that stopped at the terminator would leave
the previous entry's runes behind it. Full write-up:
[`ConversionStrategies-Reference.md`](../ConversionStrategies-Reference.md), *A STRUCT handed to the
kernel by address must be blittable*.

## Recurring classes worth a general fix rather than another point repair

- **The import-path → C#-identity derivation. THREE sightings, each fix covering exactly ONE shape.**
  `getProjectName` (`importOperations.go`) mints **four** identities from one string — the `.csproj`
  filename, the library `<AssemblyName>`, the NuGet `PackageId`, and (minus the last segment) the C#
  **namespace** — all of which must be unique across the package graph. It has now been wrong three times:
  (i) a **quoted** `module "gopkg.in/yaml.v3"` directive carried its quotes into the csproj filename, which
  Windows rejects outright (#33); (ii) a path element containing a C# **keyword** was escaped on the
  declaration side and not by consumers, so the two sides of one namespace disagreed (#33); (iii) the
  upward walk for `go.mod` treated the first ancestor holding **no `.go` files** as the module boundary and
  truncated the name to its leaf segment (#35, 2026-08-08) — 743 of 1,727 names in one user's conversion,
  175 of them colliding, and 531 collapsed into the bare `go` namespace where 12 landed on converted-stdlib
  classes (`errors`, `strings`, `runtime`, `os`, …). Note the escalation: the third one is not merely a
  naming nuisance, it silently aliases third-party packages onto the standard library's own classes.

  **The shape to check for the next one:** the derivation still *reconstructs* the import path by walking
  the filesystem, even though the loader's canonical path is in hand at every call site —
  `options.packageImportPath` on the declaration side (`conversionDriver.go`), the `importPath` key on all
  three reference sides (`getLocalModulePackageInfo`, `getRecurseDependencyInfo`, and the stdlib arm of
  `getImportPackageInfo`, which already does exactly this and has never been wrong). Reconstruction was
  left standing after #35 because it now *provably* yields the import path for any module package (module
  path + relative path is the definition of one), and `pkg.PkgPath` risks `command-line-arguments` for a
  bare-directory conversion. But a fourth mangling means plumbing the canonical path through is the general
  fix and the heuristic is the point repair. Full rule:
  [`ConversionStrategies-Reference.md`](../ConversionStrategies-Reference.md), *A project name is the
  package's FULL import path*. ⚠ Every one of these passed all standing gates — see the `-recurse` gate
  gap at the top of this file.
- **Zero-value construction for a type that needs one.** Fixed **four** times now in four different
  emission paths: a heap-boxed local fixed array, `new([N]T)` dropping its length, `make([]S, n)`
  where `S` carries a fixed-array field, and (2026-07-27) `make` of a **defined** slice type, whose
  go2cs-gen wrapper has no element-factory constructor — `internal/fmtsort`'s
  `make(SortedMap, 0, n)` emitted a lambda into an `nint` parameter (CS1660). That fourth one was
  **live on master**, not latent, and it took 20 of 61 banked packages down in a single sweep:
  `-tests` regenerates production `.cs` on every run, so the one package that regenerated a broken
  `sort.cs` broke every later package downstream of `fmt` in the same tree. Residue: a `default!`
  zero-var local. Every *new* emission path re-opens this class, which argues for centralizing
  zero-value construction instead of patching sites — this is now the fourth data point for that.
- **A one-level probe of a COMPOSED type — closed for anonymous-type lifting (2026-07-31), and worth
  looking for elsewhere.** The extractor that finds an anonymous `struct{…}`/`interface{…}` in a
  declaration inspected the immediate child of each container kind, so it saw `*T`, `[]T` and (after a
  separate one-off patch) `map[K]V`, but no *composition* of them — `[]*struct{…}` fell straight
  through to raw Go text and a CS1031 cascade. The tell that this is a class rather than a bug: the map
  arm had already been added as its own function rather than as a rule, which is the shape a
  point-repair leaves behind. The fix replaced both extractors' dispatch with one recursive descent
  over the type-composing operands. Any other analysis that peels a type expression by hand — rather
  than through `go/types` or the shared walk — is a candidate for the same defect. ⚠ And scope any
  such site with an **A/B reconvert, not a source scan**: a grep for the shape reported zero
  production hits and would have called the corpus untouched, but the A/B found
  `encoding/gob/type.cs`, whose `(*struct{ r7 int })(nil)` reaches its literal through a
  parenthesized pointer conversion the pattern never looked for. Charter §9's rule earning its keep
  in the opposite direction — the scan had a positive control for `[]*struct{…}` and none for
  `(*struct{…})`.

  **The one site this bullet named is now closed too (2026-07-31).** `visitStructType.go`'s
  struct-FIELD arm kept its own hand-written peel and lifted `[N]struct{…}` but not `[N]*struct{…}`,
  `[]*struct{…}`, `map[K]struct{…}` or `chan struct{…}`; it now calls `extractStructType` /
  `extractInterfaceType` like every other lift site. (The bullet's list was one entry too generous —
  a *bare* `*struct{…}` field always had its own arm and always lifted.) A/B'd over all 305 projects:
  the widening itself has **no corpus consumer**, exactly as predicted, and the only change is one
  incidental canonicalization in 4 files / 2 packages — the shared helpers exclude the **empty**
  `struct{}` and the old arm did not, so `runtime.Func`'s `opaque` and `database/sql`'s two
  `_NamedFieldsRequired` fields now take golib's `EmptyStruct` instead of minting a private empty
  `[GoType("dyn")]` type apiece. Corpus builds 302/302 with 0 errors; nothing referenced the removed
  names. Full rule + the two properties that keep the shared helper faithful (lift naming, sub-struct
  tracking): [`ConversionStrategies-Reference.md`](../ConversionStrategies-Reference.md), *An
  anonymous struct lifts from ANY depth of its declared type*; guarded by `AnonStructArrayElement`.

  What it did **not** close, and is the honest next increment here: the **cross-context
  anonymous-lift identity split**. Constructing a value of an anonymous struct type lifts a second,
  function- or file-scoped name for the same Go type (`fill_s` beside the field's `S_One`), so a
  direct struct assignment survives only on go2cs-gen's dyn-struct implicit conversion and a
  *container* of it — `slice<ж<A>>` to `slice<ж<B>>` — has nothing to bridge it (CS1503). That is
  why the new guard reads its composed fields at their zero values, and why the pre-existing
  one-level guard never indexes `Stats.BySize` either. It predates this arm and is unaffected by it.

  **Two more instances of the class landed 2026-07-31, both in `net`, and both confirm the diagnosis.**
  (i) `convUnaryExpr`'s `&base.field` routing admits a base by an enumerated shape list (ident /
  selector / call / index / star) that a **type assertion** is not in, so `&c.(*UDPConn).conn`
  copy-boxed. (ii) `convCompositeLit` has *three* composite paths, and the elided **pointer** arm
  (`[]*struct{…}{{…}}`) never called the interface-field router its two siblings call. The shared tell
  is now unmistakable: **whenever an analysis enumerates shapes it has SEEN rather than stating the
  property it needs, the sibling composition is the one missing.** Both fixes state the property
  instead (a postfix rendering chains `.of(…)`; every composite path records its interface fields).
  ⚠ A related asymmetry is deliberately left standing and is worth a look with its own guard: that
  elided-pointer arm still does not call `markStringFieldLits`, relying on a blanket per-element
  `u8StringArgOK` instead of the typed path's per-field precision. It emits correctly for every
  corpus site today (net's `"?0123456789abcdef"u8` among them) and CNR is byte-identical, so there is
  no demonstrated consumer — the same reason the `visitStructType` item above was held back from the
  commit that predicted it, and then landed as its own guarded increment.
- **Untyped constants in a typed slot — CLOSED 2026-07-29.** The int-literal case was already fixed;
  a computed float constant that directly uses a named untyped integer wrapper now folds once at the
  resolved float width. `hash/maphash` validates 22/22; `UntypedConstDefine` guards both `:=` and typed slots.
- **A conversion that must ALIAS, implemented as a copy — the same silent-wrong-answer shape as
  the address-of family, at a different seam (fixed 2026-07-31).** Go's slice-to-array **pointer**
  conversion `(*[N]T)(s)` shares the slice's storage; go2cs boxed a copy of it, so every write
  through the pointer was discarded. It had been *recorded* as a known divergence ("aliasing stays
  faithful for reads back through the same pointer, and the corpus sites are read-only inputs") —
  true when written, false the moment a write site appeared, and `image/png` was that site. The
  lesson generalizes past this one construct: a documented "faithful for reads" divergence is a
  latent wrong answer with a timer on it, and the write case arrives without announcing itself.
  Two more sites the fix silently corrected: `net/http`'s data-chunk pools now return buffers that
  really are the pooled storage. Guarded by `SliceToArrayPointerAlias`.
- **The address-of box-copy family — CLOSED at all six paths (2026-07-31).** The sixth, the value
  RECEIVER, is fixed: `markAddressTakenBoxedReceiver` gives an address-taken value receiver the same
  entry-time `ref var b = ref heap(bʗp, out var Ꮡb)` preamble the value *parameter* takes, gated on
  emission by `recvBoxReasonHolds` (which `paramNeedsHeapBox` consults via `funcDecl.Recv`, since the
  params walk cannot see a receiver). Both silent-wrong-answer symptoms this row predicted are gone,
  plus one it did not: an **array** receiver's `&a[i]` was not silent but a hard **CS0103** — the
  emission already spelled `Ꮡa` (convUnaryExpr's array copy-box fallback is keyed on
  `identIsParameter`, which excludes the receiver), naming a box nothing declared. Corpus footprint,
  from a two-seeded-root A/B over all 305 projects: **3 receiver sites in 2 files**
  (`encoding/base64` `WithPadding`/`Strict`, `encoding/base32` `WithPadding`), every one a
  `return &enc` after the last mutation — correct-by-luck before, one storage identity now, no live
  victim. Closing the family at its root rather than after a sixth broken package is exactly what
  this row argued for. Full rule, the public-surface argument (the receiver's C# *type* never moves,
  so `RecvGenerator`/`[GoRecv]`, pointer calls and interface satisfaction are untouched), and the
  measured note that the inherently-heap restriction rejects **zero** receiver sites today — unlike
  the parameter arm's 48 of 149, whose over-boxing came from *also* recording
  `packageCaptureModeBoxIdents`, which the receiver arm never does: see
  `docs/ConversionStrategies-Reference.md`, *An address-taken VALUE PARAMETER heap-boxes too*.
  Guarded by `AddressOfParamWrite`, extended with the receiver arm and its four controls.

## RETRACTED — the `internal/zstd` / `testing.B` "trap" was a false alarm

`internal/zstd` is worth **534 verdicts**, and the fix is what it looked like: Go's `B` and `T` both
embed `common`, so a benchmark body may call `Cleanup`, `Error`, `Log`, `Name`, `TempDir` and the
rest, while `core/testing`'s compile-only `B` surface declared almost none of them. Adding the
missing `common` members makes `internal/zstd` validate at **534/534** — banked 2026-07-27.

**Two claims previously recorded here are wrong, and both were re-measured on master before the
retraction:**

1. *"Completing `B`'s surface breaks `crypto/hmac`."* It does not. With all 14 members added,
   `crypto/hmac` regenerates with its `<ProjectReference … io.csproj />` intact and validates at
   **172/172**. The stated mechanism cannot hold: `core/testing` is hand-owned **C#**, the closure
   is computed in **Go** from `go/types`, and the converter never reads the shim — no edit to
   `testing.cs` can change a byte of converter output. (Adding *extension* methods would not make
   `B` implement `TB` in C# either.)
2. *"`crypto/hmac`'s closure is not reproducible from a standalone regeneration."* It is. Deleting
   `crypto.hmac.tests.csproj` outright and re-running the pipeline on the committed tree
   regenerates it byte-identically, `io.csproj` included, with and without the `B` members.

The likely origin of both is charter §9's false-alarm trap (a): a `bin/go2cs.exe` built before
`60f99c505` — the commit that added the interface-base closure, and the one *immediately* before
hmac's banking commit — regenerates hmac without the io reference and fails exactly as described.
**Lesson to carry forward:** when a change in one language appears to alter output produced by
another, force `go build -o bin/go2cs.exe` and re-measure before recording a coupling.

## Rulings — 2026-08-02 (user; all recommended options adopted)

1. **`time`/`TestUnmarshalTextAllocations`: NO disclosure.** A want-zero alloc assert is
   satisfiable, so disclosing it would soften the doctrine the badges depend on. The `IByteSeq<T>`
   boxing redesign (CleanupBacklog #7) is PROMOTED onto `time`'s critical path.
   **The doctrine paid off twice on `os`'s instance (r39-osalloc, 2026-08-03).** Refusing the
   disclosure forced `TestWriteStringAlloc`'s 9,208 bytes to be DECOMPOSED rather than argued about,
   and the decomposition found two silent allocations in `ж<T>` — `IsNull` boxing the whole pointee on
   every dereference, and `of(…)` minting its untyped accessor wrapper per call — worth 62 % of the
   bill and paid by *every* pointer read and field address in the corpus, not just by `os`. A
   disclosure would have banked the package and left both in place. The row still does not reach zero
   and `os` still does not bank; the remainder is an architectural arc, recorded in the `os` block's
   *r39-osalloc* sub-section.
2. **Capability-exclusion SANCTIONED for the provably-unownable os class** — the hostfxr
   apphost-relocation limitation (`TestRemoveAllWithExecutedProcess`), `TestCmdArgs` (a managed
   materialization would let Go `LocalFree` GC memory), and `TestDirectoryJunction` (raw-metal on
   non-native types in test code). Implement via the established `unsupportedRuntimeCapabilities`
   mechanism, WITH the mandatory §9 roster scan (positive control) before widening. This plus the
   fixable rows is `os`'s path to a bank.
   **IMPLEMENTED 2026-08-03 (r38-os-fin)** — all three, with the roster scan clean (zero hits across
   72 packages) and both controls firing. The mechanism gained one generalization it needed: an entry
   now maps a SYMBOL to the NAME of the capability, so the proof page reads *"relocatable single-file
   test executable"* instead of a bare symbol, and a key may name the test DECLARATION itself for a
   capability that belongs to the host rather than to anything the test calls. Detail in the `os`
   block's *r38-os-fin* sub-section. The fixable rows all closed too; the path led to one residual,
   not to a bank — see ruling #1, which `TestWriteStringAlloc` is now the second instance of.
3. **Timer mode-0 divergence ruling DEFERRED** until the recorded one-fire-per-pass timer-model
   fix lands and reshapes the residual — no ruling on a measurement about to change.
4. **`GoUntyped` → `GoBigConst`** (see the charter §6.1 math/big row); rides the rebank.
   **LANDED 2026-08-04 (r40-rebank, commit A)** — a pure rename of the `System.Numerics.BigInteger`
   csproj `<Using Alias>`: converter emission + templates, `golib.csproj`, the behavioral goldens
   that carry it, and the strategy docs. The corpus said `GoUntyped` until the rebank's own regen
   levelled it in commit B. The behavioral project `GoUntypedConstArg` keeps its name — it is named
   for the Go-language *untyped const* concept, not for the C# alias.
5. **The native-address+managed-snapshot pointer flavor is DEFERRED** until `net`'s DNS work
   demands it; then a design-with-user session — not designed against one test.
6. **Whole-corpus rebank: scheduled immediately after the r37 train lands** (carries the
   accumulated intended drift + the param-unification footprint + the `GoBigConst` rename).
7. **NuGet release: after the rebank**, so the first badged release ships a corpus byte-current
   with the converter.

## The r37 train's sweep catch — reflection increment 6 REVERTED pending its atomic twin (2026-08-03)

The all-ships sweep failed `math/rand` AND `math/rand/v2` on the assembled train:
`panic: reflect: Method index out of range` in `TestRegress` — the EXACT successor gap increment
6's own report recorded ("a NumMethod() > 0 gate lets method-enumeration loops get further; the
first consumer that walks one demonstrates it"). The demonstration arrived one session later, in
two BANKED packages no lane had canaried — which is precisely the coverage the sweep exists to
provide. Reverted from the train (`39de5dd77` reverts `d75e0afcd`); both packages re-validate at
their exact banked counts (43, 36); `time` returns to 145 (its two JSON rows re-land with the
pair). **The durable scoping lesson: `NumMethod` and `Method(i)`/`Value.Method`/`Call` are one
ATOMIC increment** — a count without an enumerator converts silent vacuous passes into hard
panics. Increment 6's work survives on `claude/elated-hodgkin-12581e` (`d75e0afcd`); the chip's
increment-7 chit carries the pair, with `TestRegress`'s loop as the primary gate and math/rand ×2
as mandatory canaries.

### RESOLVED — increment 7 lands the pair (2026-08-03)

The count and the walk shipped together: `rtype.{NumMethod, Method, MethodByName}` + `Value.Method`
over ONE ordered table whose `.Count` IS `NumMethod`, with a method value represented as an
ordinary receiver-bound delegate so `Type()`/`NumIn`/`In`/`Out`/`Call` are existing surface
unchanged. Measured on this tree:

| Package | Before (master) | After | Note |
|:--|:--|:--|:--|
| `math/rand` | 43 (`TestRegress` passing **vacuously** — `NumMethod` 0 ⟹ zero loop iterations) | **43** | `TestRegress` now genuinely runs its 320 golden comparisons; the bridge reports `*rand.Rand NumMethod: 16` in Go's order |
| `math/rand/v2` | 36 (same vacuous pass) | **36** | 18-method table, same shape |
| `time` | 146 pass / 11 fail / 2 skip of 159 (the r37 re-measure above) | **148 pass / 9 fail / 2 skip** | `TestTimeJSON` + `TestUnmarshalInvalidTimes` re-land. Remaining 9 = `TestChan` ×8 (timer-model item) + `TestUnmarshalTextAllocations` (disclosure ruling) — neither this arc's |

Note the board's "`time` returns to 145" above was written against the older 145 figure; the
correct successor of the r37 re-measure (146) is **148**. The vacuous-pass detail is the part worth
carrying forward: the banked 43/36 were never evidence that `TestRegress` worked, because with
`NumMethod` at 0 its loop body never executed — a count of zero is indistinguishable from a type
with no methods, which is the same silent-degradation class as the `""` type name (increment 5).

Also fixed here, and it retroactively invalidates increment 6's numbers: a `this object` extension
method (golib's `TryCastAsInteger`) was entering **every** type's method table through the
candidate source's assignability safety net, and doing so nondeterministically — the same binary
reported `NumMethod` 4 or 6 for the same type depending on which assemblies had loaded when the
cache was first filled.

### Increment 8 — the ZERO test, and the one row that must NOT be landed (2026-08-03)

Two of gob's chip-owned roots close; the third is rooted and handed back with a recommendation
rather than a fix.

**Measured on the post-fix tree: `encoding/gob` 88 → 95 of 106.** One `-tests -test-action all
-test-timeout 20m` run, zero empty verdicts: the mismatch list goes from **18 rows to 11**, and the
seven that vanished are *exactly* the two roots below — `TestGobEncoderPointerThenValue`,
`TestGobEncoderValueThenPointer`, `TestGobEncoderValueEncoder`, `TestGobEncodeIsZero`,
`TestLargeSlice` + `/byte` + `/struct`. **No new mismatch appeared**, so this is not the
root-cause-layering case where one row's fix merely unmasks another. gob still does not bank and no
gob artifact was committed (the measurement tree was restored). The remaining 11 keep their existing
owners: array-length model (3), `ArrayOf`/typelinks (1), `MapType().Hasher` (1, below), typed-nil
converter (3), the gob ignore path (2), untyped complex narrowing (1).

**Closed — root 1 (`Value.IsZero`, 4 rows) and root 2 (`Value.Grow`, 3 rows).** The census
understated root 1 considerably. It is not "wrong for a named STRING and for an ARRAY": both the
Array and the Struct arm fall to `v.ptr == nil`, which the bridge never populates, so `IsZero`
answered **true for every array and every struct in the corpus whatever it held**. Measured against
`go run` on a purpose-built probe before the fix — `[2]uint8{1,2}`, `NA{1,2}`, `inner{N:1}`,
`outer{P:&n}`, `outer{I.S:x}` — every one `true` in C#, `false` in Go. A fourth read had to land with
it: `IsZero`'s String arm is `Len() == 0`, and `Len` was blind to a `[GoType("str")]` wrapper (every
other named container answers through its golib interface; a named string implements none), so the
arm could not be right until `Len` was. Both now hand-owned, plus `Grow`, which read a
`*unsafeheader.Slice` off the same absent `v.ptr` and nil-deref'd for **every** caller. Guard:
`tests/Behavioral/ReflectZeroAndGrow`, byte-identical to `go run` across 33 rows. Design:
ConversionStrategies-Reference *A ZERO test is a descriptor read too*.

**NOT landed, deliberately — `MapType().Hasher` / `Key.Equal` (unique's 15 of 19, net's last cctor
root, gob's `TestNetIP`).** This row is not the same shape as the others and populating it would be a
regression, not a partial fix. `Hasher(unsafe.Pointer, uintptr) uintptr` must hash *the value at an
address*; the address that call site produces cannot name a managed value. Three measurements settle
it: two boxes holding equal `@string` values necessarily have **different** addresses (so no
address-derived hash can make `unique.Make("hello")` agree with itself — the package's whole point);
a box whose pointee contains a reference has no pinnable slot and its address **moved across a forced
GC**; and the `unsafe.Pointer` the call site builds retains no link to its source box, its
constructor taking a `uintptr`. Key/elem *types* are recoverable from the carried `System.Type`, but
landing only those is strictly worse than today: `Key.Equal` is the comparability SIGNAL — a
pointer-identity compare — so a half-populated descriptor turns a loud `NewHashTrieMap` construction
failure into a map that silently mislays every key. **The increment-6 lesson inverted: a descriptor
field whose read cannot be honored must not be populated to look truthful.**

**Recommendation (needs a coordinator ownership ruling).** The remedy is one layer down and outside
this arc's declared files: hand-own `internal/concurrent/hashtriemap.cs` on the `sync.Mutex`
precedent. Its CONTRACT — a concurrent map from comparable `K` to `V` — is answered natively and
correctly by the CLR; only its MECHANISM (hash the bytes at an address) is raw-metal that the managed
model cannot express. That is exactly the documented S1 fork. It would clear `unique`'s single wall
(making 19 rows measurable for the first time), `net`'s last initializer root, and gob's `TestNetIP`.
The chip did not take it unilaterally because `internal/concurrent` belongs to no lane's declared
ownership and the file is a whole-package hand-own, not a bridge `_impl.cs`.

## Rulings — 2026-08-03 (user; both recommendations adopted)

1. **The mode-0 timer residual (time's 4 rows): COMMISSION THE SYNCHRONOUS-TIMER-CHANNEL ARC** rather
   than ruling a divergence — Go 1.23's sync timer channel (#37196: Stop/Reset that blocks stale
   values; no drain needed) implemented in golib's channel layer. Wave3's successor arc, §7
   adversarial discipline, r39-timer's zero-margin drain constraint (at Stop/Reset at most 2 ticks
   exist: 1 buffered + 1 committed-unsent) is required reading. time banks when it lands (152 + 4
   mode-0 rows + the alloc row below).
   **IMPLEMENTED 2026-08-03 (r39b-synctimer)** — all 4 rows closed, `time` at 156/1/2 of 159; detail
   in the `time` block's *RESOLVED — r39b lands the synchronous timer channel* sub-section. The bank
   is now gated solely on ruling #2's arc.
2. **time's alloc row (216 B, both halves fixable): NO disclosure — commission the CLOSURE-EMISSION
   arc.** The 88 B half = the local-function emission mode (a func literal bound to a local that is
   only ever CALLED emits a C# local function — captures without allocating; corpus-wide fidelity +
   perf win). The 128 B half = escape-analysis refinement (an address-taken local Go stack-allocates
   need not heap-box). Sequenced AFTER r39-osalloc's dock so its defer-closure findings unify with
   the local-function mode into ONE reviewed closure-emission design.
   **IMPLEMENTED 2026-08-03 (r39e-closure)** — both halves landed, `time` at **157 pass / 0 fail /
   2 skip of 159** and banked as package #73. The unified design the ruling asked for is
   [`DESIGN-closure-emission.md`](DESIGN-closure-emission.md): §3 records what landed, §4 is the
   ref-struct frame (r39-osalloc arc item 3) written up as a proposal for user review, NOT
   implemented. Detail in the section below.

## r39e-closure (2026-08-03) — 216 = 128 + 88, both halves are converter emission, and `time` banks

Ruling #2 commissioned this arc on r39-timer's decomposition. That decomposition was **exact**: each
half was re-measured here in isolation, by reverting one emitted form at a time in the built test
host and re-running the single row.

| `time` `TestUnmarshalTextAllocations` | allocs |
|:--|--:|
| branch base `18423efaf` | 216 |
| local-function fix only (test-body box restored by hand) | **128** |
| escape-narrowing fix only (parseUint lambda restored by hand) | **88** |
| both | **0 — passes** |

**Fix 1 — a func literal that is only ever CALLED emits as a C# local function (88 B).** A capturing
lambda allocates a display class AND a delegate on every evaluation of the lambda expression — per
call of the enclosing function, whether the closure runs or not. A local function that is never
converted to a delegate captures through a by-ref STRUCT closure: same single storage location per
captured variable, no heap object. The gate is the proof that keeps that compilation available —
every reference other than the declaration must be a call callee, which also subsumes reassignment
and address-taking. Emission is a new `LambdaContext.localFuncName` mode in `convFuncLit`, so the
whole body pipeline (capture hoisting, boxed value params, variadic prologue, array clones, named
results, the single-return collapse) is shared verbatim with the lambda path. A literal that
**defers or recovers is deliberately excluded**: its 440 B execution context dominates the 88 this
removes, and lifting the exclusion is §4 of the design, not a workaround here.

**Fix 2 — a variable DECLARED INSIDE a closure is not captured BY it (128 B).** The escape
analysis's function-literal arm matched any mention of an object lexically inside a literal's body,
and for a variable declared *there* that mention is its own declaration. `var t Time;
t.UnmarshalText(in)` inside a closure heap-boxed `t` — and the box `Ꮡt` was **never referenced in
the emitted body** — while the identical statements outside a closure emitted a plain local. One
containment test fixes it, and the skip keeps descending so a literal NESTED inside still marks the
escape it genuinely causes. The narrowing direction is the dangerous one, so the proof is explicit:
Go scoping puts a literal's own local out of reach of every other frame, and every route by which
such a local can still escape (`&x`, `&x.f`, `&x[i]`, a pointer argument, a capture-mode method, a
pointer-receiver method value, a `go`/`defer` use) is decided by an arm that walks the whole
enclosing body, literal bodies included.

**Whole-corpus footprint — two-temp-root A/B (both roots seeded per CLAUDE.md §1/§1a; base exe built
from `HEAD` versions of the four changed converter files):**

| | files | sites |
|:--|--:|--:|
| local-function emission | 91 | **152** (133 block-bodied, 19 expression-bodied) |
| heap box removed | 22 | **32** |
| both families in one file | 8 | |
| **total changed** | **105** | |

Every changed line in the 105 files falls in one of the two families — verified by attribution, not
by sampling: each removed line is a lambda declaration, a `};`→`}` close, a `= ref heap` box, or a
statement in a file that has a box removal (the collapse's second line, and in `reflect/iter.cs` the
`valueᴛ1` for-loop temp that only existed because the variable was a `ref` local). Marker gate:
**39** `[module: GoManualConversion]` files, line-anchored, **0 clobbered**, 16 carrying a `.cs.auto`.

Behavioral CNR: **41** files, 96 local-function sites + 1 box removal, 168 added / 169 deleted —
arithmetic closes exactly (96 + 71 `};` + 2 box lines removed = 169; 96 + 71 + 1 = 168). Guards:
`LocalFunctionEmission` (10 probes, 5 negative controls — one per disqualifying reason) and
`ClosureLocalNoHeapBox` (8 probes, 5 of them boxes that must survive, each writing through the
escaping alias and reading it back). Both neuter-proven: with the fix removed each golden
mismatches (24 and 11 changed lines respectively) and restoring it returns them to green.

**One incidental finding worth recording.** The committed `src/core` is **stale by 685 files**
against a seeded reconvert with the BASE converter — the r36 four-deref-accessor change
(`Ꮡp.Value` → `Ꮡp.DerefOrNull()` on pointer receivers and parameters) landed as a converter fix
without a corpus regen, which is correct policy but means a plain overlay-then-`git diff` is NOT a
usable A/B instrument on this branch. The two-temp-root form is, and it is what the numbers above
come from. The same staleness is what makes a `time` `-tests` run show `DerefOrNull` and
`fallthrough`-placement diffs in its production `.cs`; those are pre-existing, not `-tests`-closure
drift, and they are restored rather than banked.

**The sweep found a 74th thing: a disclosure that was never a CLR limit.** The full 73-package
validated sweep (2,783 s) reported **72 pass / 1 "fail"**, and the one flagged row was `bytes` at
**count 82, banked 81** — MORE matching verdicts than the roster claimed. `TestEqual` had an
`alloc-profile` disclosure since 2026-07-18 reading *"the managed runtime allocates during the
converted Equal comparison loop where Go's compiler-optimized code does not"*. It does not. The
converted test body was

```csharp
foreach (var (_, vᴛ1) in compareTests) {
    ref var tt = ref heap(new compareTestsᴛ1(), out var Ꮡtt);   // ← per ITERATION
    tt = vᴛ1;
    …
}
```

— the range variable of a loop inside the `AllocsPerRun` closure, heap-boxed by exactly the arm this
train narrowed, once per iteration of an assert that wants zero. It now emits
`foreach (var (_, tt) in compareTests)` and the test passes on its own merits. The disclosure is
**retired**, not re-signed: §5 of the disclosure policy says a real bug is never a disclosure
candidate, and this one had been standing in for a converter defect for two weeks. `bytes` moves to
**82 matched · 6 disclosed** (re-run twice, identical), the roster to **2,713 matching · 50
disclosed**, and its `TestEqual` verdict is now earned rather than excused.

That is also the general lesson worth keeping: **a want-zero alloc assert is a converter test, and a
disclosure filed against one should be re-examined every time the emission changes.** Five
alloc-profile disclosures remain in `bytes` and one in `bufio`; nothing here says they are wrong, but
nothing has re-derived them either. The cheap instrument is the one this lane used by accident — run
the sweep and read a count that is HIGHER than banked as a finding, not as noise.

Sweep aftermath, classified: 60 proof pages regenerated (a renderer wording change from an earlier
lane plus provenance — restored, they belong to a rebank), the documented 7-file `-tests`-closure
emission class, and corpus-wide production `.cs` churn that is the same 685-file staleness recorded
above. Only `bytes`'s test sources, its disclosure manifest and its proof page were banked, because
only they are the evidence for a row that changed.

## `internal/concurrent.HashTrieMap` HAND-OWNED — the wall falls, and three walls stand behind it (2026-08-03, r39d-hashtriemap)

The user-ruled hand-own landed: `src/core/internal/concurrent/hashtriemap.cs` is now a whole-file managed
reimplementation carrying `[module: go.GoManualConversion]` (corpus marker census **39 → 40**). Rationale,
the API map, and the equality-bridge verification live in
[`ConversionStrategies-Reference.md`](../ConversionStrategies-Reference.md), *`internal/concurrent.HashTrieMap`*;
the hand-own mechanics in [`Baseline-vs-FullConversion.md`](../../src/archived/Baseline-vs-FullConversion.md). Summary of
what was measured, because the shape of the result matters more than the row count:

**Gates.** `internal/concurrent` and `unique` build clean; `go2cs-stdlib.slnx` builds **304/304, 0 errors**.
A seeded full `-stdlib -comments` reconvert leaves `hashtriemap.cs` and `package_info.cs` **MD5-identical**;
strip the marker and the same run overwrites `hashtriemap.cs` with its own 21 KB emission and rewrites
`package_info.cs` — the protection proven in both directions. The behavioral suite and the 72-package
validated sweep are green/unchanged (`internal/concurrent` is in no banked package's closure — the gates
were insurance, not measurement).

**The equality bridge is NOT a problem — verified by probe, not by reading.** `EqualityComparer<K>.Default`
is Go's `==` for every key shape the corpus interns: `ж<T>` (pointer identity + matching identity hash, and
`abi.TypeFor<T>()` interns one descriptor box per `System.Type`, so a second call finds the first call's
entry), a `[GoType]` struct of `{bool; @string}` — netip's `addrDetail` shape — (generated field-wise
`Equals` + `HashCode.Combine`, matching for two keys built from *distinct* string storage), and `@string`
(content). `LoadOrStore` was contention-probed: exactly 1 winner in 64 racing callers.

**`encoding/gob`: 95 of 106, unchanged — `TestNetIP` does NOT flip.** Its root MOVED one frame, from
`NewHashTrieMap` → `ArgumentException: Delegate to an instance method cannot have null 'this'` to
`NotImplementedException: runtime_registerUniqueMapCleanup`. No row regressed (TestNetIP is the only gob row
whose closure reaches `unique`; the other ten failures are gob-internal and untouched).

**`unique`: 0 → 1 of 19 — and it is no longer a ONE-root wall.** That is the real deliverable. Its 15
identical `TypeInitializationException` rows resolve into **five distinct downstream roots**, each now
separately actionable:

| Root | unique rows | Shape |
|:--|--:|:--|
| ~~**`//go:linkname` PUSH never links: `unique.runtime_registerUniqueMapCleanup`**~~ | 1 (+ gob's `TestNetIP`, + `net`'s cctor) | **CLOSED 2026-08-07 (r43b-linkname).** `runtime/mgc.go` PUSHES its body into `unique`'s bodyless declaration and the converter's forwarder handled the **PULL** direction only, so the consuming side was a throwing `PartialStubGenerator` stub. It now FORWARDS to runtime's converted body — see *the linkname PUSH direction* below |
| ~~**Same class: `internal/weak.runtime_registerWeakPointer` / `runtime_makeStrongFromWeak`**~~ | 4 → 7 → **0** | **CLOSED 2026-08-07 (r43e-weak).** `runtime/mheap.go` pushes both; hit inside `weak.Make`, i.e. `unique.Make`'s `newValue()`. r43b took the linkname half only (registered UNHONORABLE, announcing itself by name), because linking was never the remedy — `runtime`'s converted bodies walk `mheap_` span metadata the managed model does not populate. The remedy was the hand-own it announced, and it has landed: `internal/weak/pointer.cs` on `System.WeakReference` + a `ConditionalWeakTable` canonical index. The seven rows it had absorbed now advance INTO the test body — see *`internal/weak` HAND-OWNED* at the end of this file for where each one stops instead |
| **`abi.TypeFor<T>()` is silently WRONG for an INTERFACE `T`** | 1 | `TypeFor`'s interface branch is `TypeOf((*T)(nil)).Elem()`, and `Type.Elem()` for `Kind == Pointer` reinterprets the descriptor as a `PtrType` and reads `.Elem` — which under the managed layout lands on the descriptor's **`Equal` field**. `TypeFor<any>()` and `TypeFor<error>()` return a `System.Func<unsafe.Pointer, unsafe.Pointer, bool>`, not a `ж<abi.Type>`. Shared generics store it into `ConcurrentDictionary<ж<abi.Type>, any>` uncast-checked, and the first key comparison dispatches `IEquatable<ж<abi.Type>>.Equals` on a delegate → `EntryPointNotFoundException`. **Corpus-wide, and it was invisible until now**: the old trie compared raw addresses through `keyEqual` and never dispatched on a key's runtime type. Reflection-bridge row |
| **`GCHandle: Object contains references`** | 1 | `abi.Escape` pinning a managed pointee on the `weak.Make` path |
| ~~**`IndexOutOfRangeException` in `go.slice<T>.Enumerator.get_Current`**~~ | 6 | **CLOSED 2026-08-07 (r41c-cloneseq).** Not the enumerator, and not "neither linkname nor reflection" — see *the `makeCloneSeq` root, closed* immediately below |

Plus the 3 `fail` rows the r38 census already recorded (`TestMakeCloneSeq/#00`, `#01`, `interface_{}` — Go
names those subtests from `reflect.TypeFor[T]().String()`, which C# renders `""`; note this is the *same*
`TypeFor` surface as the third root above). `unique` does **not** bank; its test artifacts were restored,
not committed.

⚠ **Two traps this arc paid for.** (1) In the PowerShell tool, `[System.IO.File]` resolves a RELATIVE path
against the *process* working directory, which is the MAIN checkout — not `Set-Location`'s. A
read-modify-write with a relative path silently read `H:\Projects\go2cs`'s copy of the file and wrote it
over the worktree's, reverting the hand-own. **Always use absolute paths with the `[System.IO.File]` APIs.**
(2) `emitAutoConversionSiblings` — the fully-hand-owned-package branch — runs only six of the whole-package
pre-passes, and panics on a generic file (`WARNING: visit file error: … nil pointer dereference in
"hashtriemap.go" (auto-conversion sibling skipped)`), so no `.cs.auto` review sibling is produced for
`internal/concurrent`. Pre-existing converter defect, harmless to the marker's protection, not chased.

### The `makeCloneSeq` root, CLOSED — `unique` 1 → 4 of 19 (2026-08-07, r41c-cloneseq)

**The board's guess about this root was wrong in both halves, and the way it was wrong is the finding.**
It is not a `slice<T>.Enumerator` edge — the enumerator behaves correctly given the header it is handed
— and it is squarely reflection-bridge territory rather than "the only root that is neither linkname nor
reflection". What made it look otherwise is that the *diagnostic* names golib and the *cause* is two
frames up, which is charter §9's layering lesson in a new dress: **a first diagnostic is a starting
point, not a diagnosis.**

**The root.** `makeCloneSeq` → `buildStructCloneSeq` opens with `styp := typ.StructType()`, and Go's
`(*structType)(unsafe.Pointer(t))` is the PREFIX-DOWNCAST idiom — the linker really allocated a
`structType` behind the `Type` header. Nothing sits behind a `ж<abi.Type>`, and golib's `Reinterpret`
rightly REFUSES to alias managed storage for a reference-bearing pair (aliasing would fabricate object
references), so it fell through to the raw-address route and read `ΔStructType`'s fields out of the
memory following the descriptor's value slot. Probed on `abi.TypeFor[testStringStruct]()`:

```
Fields.Length   8830452760576   <- an address fragment read as a slice length
Fields.Capacity 16              <- the descriptor's OWN Size_, bleeding through the shifted view
```

`m_array` landed on a real heap object, so the first `Current` threw `IndexOutOfRangeException` instead
of access-violating: a CLR type-safety break that happened to be caught. `internal/reflectlite`'s
`NumField`/`Len` read the same garbage.

**The fix, at the root's own layer.** `Type.StructType` and `Type.ArrayType` join `TypeOf` in
`manualConversionFuncs["internal/abi"]` and are SYNTHESIZED in `type_impl.cs` from the descriptor's
carried `System.Type` — field types via `synthType`, Go (amd64) field offsets and array `Len`/`Elem`/
`Slice` via golib. Offsets come from the same walk that stamps a descriptor's `Size_`
(`GoReflect.GoFieldOffsets`, factored out of `GoSizeOf`'s struct arm), so the two cannot disagree.
Nothing unknowable is invented: no `System.Type`, or a field whose Go size is unknowable, answers Go's
nil; `StructField.Name`/`PkgPath` stay the zero `ΔName` (its readers walk `addChecked` raw addresses —
the same route that produced the garbage — and Go's own `Name()` answers `""` for a nil `Bytes`, so the
zero value is a state the format defines). Full rationale:
[`ConversionStrategies-Reference.md`](../ConversionStrategies-Reference.md), *`abi.Type`'s
SPECIALIZATIONS are synthesized, not downcast*.

**A second, independent defect in the same file — a converter one, and silent.** `buildArrayCloneSeq`'s
whole body was emitted as a `/* … */` COMMENT. `visitRangeStmt` recognized range-over-integer only for
`types.Int`/untyped-int, so `for range atyp.Len` (a `uintptr`) fell through to the "unexpected
expression" arm and the loop VANISHED — `unique`'s `cloneSeq` for any array-of-string type came back
empty. It was the only such comment in the entire converted stdlib. Fixed generally (any integer kind,
golib `range<T>` with the operand's own Go width, explicit type argument at each non-`int` site) and
guarded by the `RangeOverIntegerTypes` behavioral test; details in the same reference doc,
*Range-over-integer covers EVERY integer type*. ⚠ Worth remembering: the first attempt at the golib
overload REGRESSED `range(3)` to `System.Int32`, because the generic is an identity match where
`range(nint)` needs a conversion and C#'s prefer-non-generic tie-break never fires — caught by probe,
not by reading, and closed with a third `range(int)` overload.

**Census, `unique`, matched rows: 1 → 4 of 19.** All six `IndexOutOfRangeException` rows are gone. The
three `TestMakeCloneSeq` ones (`testStringStruct`, `testStruct`, `testStringStructArrayStruct`) now
PASS; the three `TestHandle` ones MOVED to the `internal/weak.runtime_registerWeakPointer` root that
was always behind them. A fifth subtest, `TestMakeCloneSeq/testStringArray`, now computes the correct
`{[0 16 32]}` but still cannot MATCH, because C# names it `#01` — that is the `TypeFor`/`Name` root,
row three of the table above, untouched. `TestHandle/interface_{}/<nil>` also moved (from
`EntryPointNotFoundException` to a null `HashTrieMap` key); that root's `Type.Elem()` reinterpret is
the SAME defect class as this one and simply read different garbage this run — it was not chased, and
neither were `MapType()`/`FuncType()`/`InterfaceType()`/`Key()`/`Len()`, which all still reinterpret.
`unique` still does **not** bank; its test artifacts were restored, not committed.

**Blast radius.** `StructType()`/`ArrayType()` have exactly two corpus callers (`unique`,
`internal/reflectlite`), and the range widening has exactly one corpus site. Gates: behavioral CNR
byte-identical across all 570 packages apart from the new test project; `run-behavioral.ps1` full
**545/545** transpile+compile+golden and **515/515** stdout (30 skipped, no `package main`), 1,081 s;
`go2cs-stdlib.slnx` **304/304**, 0 errors; `go test ./...` in the converter ok; GolibTests **69/69**
(60 + 9 new), ChannelTests **24/24**.

### The linkname PUSH direction, CLOSED as a MECHANISM — one pair links, one announces itself (2026-08-07, r43b-linkname)

The converter's forwarder handled only the PULL direction (a bodyless declaration naming another
package's symbol). Go's other direction — the DEFINING package carries the body and names another
package's declaration, the consumer being a bodyless func under a **one-arg** `//go:linkname` handle
— linked nothing, so every consumer fell to the `PartialStubGenerator`. Mechanism and rationale:
[`ConversionStrategies-Reference.md`](../ConversionStrategies-Reference.md), *A cross-package
`//go:linkname` PUSH resolves per recorded disposition*.

| Pair | Disposition | Why |
|:--|:--|:--|
| `runtime.unique_runtime_registerUniqueMapCleanup` → `unique.runtime_registerUniqueMapCleanup` | **FORWARDED** | The pushed body is ordinary converted Go — a `chan struct{}` plus a goroutine that drains it and calls the callback. The managed model runs the real thing; nothing signals the channel because `clearpools()` is driven by Go's GC, which does not run. That is Go's own behavior for a program whose GC never fires (the intern map keeps its entries), not a fabricated answer |
| `runtime.internal_weak_runtime_registerWeakPointer` → `internal/weak.runtime_registerWeakPointer` | **LOUD STUB** → **HAND-OWNED** | `getOrAddWeakHandle` → `spanOfHeap` → `throw("getWeakHandle on invalid pointer")`: the body walks `mheap_` span metadata the managed model does not populate. **Answered 2026-08-07 (r43e-weak)** by the `internal/weak` hand-own; the registry row STAYS, because it is what a conversion into a root without the hand-own must still emit |
| `runtime.internal_weak_runtime_makeStrongFromWeak` → `internal/weak.runtime_makeStrongFromWeak` | **LOUD STUB** → **HAND-OWNED** | Re-derives an object pointer from a heap address. A forwarder would fault or — worse — return a plausible pointer derived from garbage, the inverse-atomic rule's exact prohibition. Same disposition, same answer, same reason for keeping the row |

**The registry is curated, and the reason is structural, not caution.** The converter never sees the
pushing package's directives while converting the consumer — a package is converted from its own
syntax, dependencies contribute types rather than comments, and the pusher need not even be a
dependency. Go 1.23 carries ~200 pushes outside `cmd/`; the corpus exposes **eleven** as bodyless
one-arg-handle declarations, and linking those wholesale would REGRESS working packages: `time`'s
timer trio is already answered by `time_impl.cs` and a converter-emitted body would collide with it,
while `internal/syscall/windows`'s `stdcall` wrappers and `internal/coverage/cfile`'s linker-section
walk push bodies the managed model cannot run at all.

**Measurement — the honest read is "the root moved", not "rows flipped".**

* **`unique`: 4 of 19, UNCHANGED.** The cleanup registration links and no longer throws anywhere; the
  seven `TestHandle` rows that stopped there now stop one frame later, inside `weak.Make`, on the
  ANNOUNCED weak pair. The remaining roots are untouched: `abi.TypeFor<T>()` for an interface `T`
  (`EntryPointNotFoundException`, still row three of the table above), `GCHandle: Object contains
  references` on `abi.Escape`, and the `TypeFor`/`Name` subtest-naming rows. `unique` does **not**
  bank; its test artifacts were restored, not committed.
* **`encoding/gob`: 98 of 106; `TestNetIP` does NOT flip.** Its root moves from
  `NotImplementedException: runtime_registerUniqueMapCleanup` to the announced
  `internal/weak.runtime_registerWeakPointer` inside `net/netip`'s `cctor` → `unique.Make` →
  `newValue()`. ⚠ The 98 is **not** this arc's delta: the board's 95 dates from r39d and the other
  seven failures (`TestBadData`, `TestEndToEnd`, `TestIgnoreDepthLimit`, `TestIgnoreRecursiveType`,
  `TestIndirectSliceMapArray`, `TestNilPointerInsideInterface`, `TestSingletons`) are gob-internal,
  outside anything three files in `unique`/`weak`/`runtime` can reach. The intervening arcs moved
  them; re-baseline the row from this number, do not credit it here.

**What `internal/weak` is now waiting on — and it is the ONLY thing.** A hand-owned managed weak
reference (`System.WeakReference` over the `ж<T>` box) under `[module: go.GoManualConversion]`, the
same shape `sync`'s Mutex family and `internal/concurrent.HashTrieMap` took: honor the observable
contract, never emulate the mechanism. Deliberately NOT attempted in this lane — the linkname
mechanism and a semantic hand-own are separate units of work, and the loud stub is what makes the
second one findable. Its single file (`internal/weak/pointer.go`) makes a whole-file replacement the
natural form. ✅ **Landed the same day (r43e-weak)**, in exactly that shape — see
*`internal/weak` HAND-OWNED* at the end of this file.

**Gates.** `go test ./...` in the converter ok (new `TestRecurseLinknamePush`, both arms
neuter-proven); CNR byte-identical across all **571** behavioral packages; a seeded full
`-stdlib -comments` reconvert is byte-identical to the committed tree across every `.cs`/`.csproj`/
`README.md` (zero unclassified; hand-own clobber gate 0 violations; no `DYNTYPE` markers);
`go2cs-stdlib.slnx` **304/304**, 0 errors. A/B footprint: **3** corpus files.

⚠ **A NEW environmental failure shape worth recognizing: the host DISK FILLED mid-suite.** The full
`run-behavioral.ps1` reported `FAIL (546 projects, 1,413.5s)` with **115 Go build failures** plus one
Output mismatch — and every one of the 115 reads verbatim `compile: writing output: write
$WORK\b001\_pkg_.a: There is not enough space on the disk` (C: was at **2.8 GB** free of 1.86 TB, three
lanes deep). The C#-side phases, the ones a converter change can actually move, all passed the WHOLE
corpus: **Transpile 546/546, Compile 546/546, Target 546/546**. Output read 404 pass / 1 fail / 141 skip,
where the skips are the 115 disk-killed Go builds on top of the usual no-`package main` set. The single
Output failure, `FindFirstFileData`, **re-runs PASS 1/1 across all four phases in isolation** once space
is freed — the standing rule for a Go-toolchain-side failure under load (*re-run that one project
filtered before believing it*) applied to a new cause. Read a wall of identical `not enough space on the
disk` lines as the machine: check `Get-PSDrive C` FIRST, and do not go hunting for a converter
regression — Target passing 546/546 already proves no golden moved.

## `internal/weak` HAND-OWNED — the announced pair gets its answer (2026-08-07, r43e-weak)

The third instance of the ruled precedent, after `sync`'s Mutex family and
`internal/concurrent.HashTrieMap`, and the easiest fit of the three: `src/core/internal/weak/pointer.cs`
is now a whole-file hand-own under `[module: go.GoManualConversion]` built on `System.WeakReference`
over the `ж<T>` box, with a `ConditionalWeakTable` keyed on `ж<T>.ReferentObject` standing in for the
runtime's canonical per-address `specialWeakHandle`. Design, the clause-by-clause contract table, the
ephemeron argument for why the canonical index does not pin what it indexes, and the guarding
measurements: [`ConversionStrategies-Reference.md`](../ConversionStrategies-Reference.md),
*`internal/weak.Pointer`*. Marker census **39 → 40** (line-anchored; note 39, not the 40 CLAUDE.md
records from r40 — `math/unsafe.cs` shed its marker in the interim). `internal/weak` joins
`internal/godebug` and `internal/concurrent` as **fully hand-owned**: `internal.weak.csproj`,
`package_info.cs` and `README.md` stop re-emitting and no `.cs.auto` sibling is produced — all three
confirmed in place, since a `-tests` run over the package left every one of them untouched.

**A SECOND defect was standing behind the first, and it is not weak's.** The `[GoType]` generator gates
struct equality on *every type parameter* carrying an `IEqualityOperators` constraint, so a Go type
declared `[T any]` (or `[T comparable]`, which the converter renders `new()`) emits
`Equals(other) => false /* missing equality constraints */` — **even when no field's type mentions the
parameter at all**. Both `weak.Pointer[T]` (field `unsafe.Pointer`) and `unique.Handle[T]` (field
`ж<T>`, which defines `==` for every `T`) were victims. `Pointer[T]`'s copy is fixed here by hand-writing
the struct; `unique.Handle`'s is a GENERATOR fix, chipped, and it is what six `TestHandle` rows now
report.

### Measurements — every root moved, no row count did

| Package | Before | After | What actually changed |
|:--|:--|:--|:--|
| `internal/weak` (own suite, first ever run) | — | **1 of 3** | `TestPointerEquality` **PASSES** vs `go test` — the canonicalization clause, the hardest one, validated end to end. `TestPointer`/`TestPointerFinalizer` fail on the roster's `codegen-liveness` class (below). **Does NOT bank**, and not because the count is short of the bar: `TestPointerFinalizer` does not fail an assertion that could be disclosed, it BLOCKS forever on `<-done` awaiting a finalizer a still-rooted object can never queue |
| `unique` | 4 of 19 | **4 of 19** | the announced weak panic is gone from every row; the host stops DEADLOCKING; the seven `TestHandle` rows resolve into four distinct new roots |
| `encoding/gob` | 98 of 106 | **98 of 106** | `TestNetIP` no longer throws — `net/netip`'s package initializer **completes for the first time** and the test produces a value: `decoded to ::ffff:1.2.3.4%, want 1.2.3.4`. A netip 4-in-6/zone rendering difference, in nothing this arc touches. The other seven failures are the same gob-internal set |

**The `unique` host used to hang, and closing weak is what exposed it.** `handle_test.go`'s `drainMaps`
arms a one-shot notification, calls `runtime.GC()`, then BLOCKS on `<-wait` until the intern-map cleanup
runs. `runtime.GC()`'s hand-owned managed body (`runtime/managed_impl.cs`) wired only the `sync.Pool` arm
of `clearpools()`, so the cleanup could never run and every `TestHandle` subtest deadlocked — taking the
whole test host to its package timeout and **erasing the verdicts of the 12 rows that had nothing to do
with it**. That deadlock only became reachable once `internal/weak` stopped panicking one frame earlier.
`GC()` now also does clearpools' unique arm — the same non-blocking send on `uniqueMapCleanup`, inert
until `unique.Make` has run. Result: a 10-minute timeout with 3 usable verdicts becomes a **2-minute run
with 19**.

**`unique`'s four surviving roots, all measured this arc:**

| Root | Rows | Shape |
|:--|--:|:--|
| **`[GoType]` equality gate — `unique.Handle<T>.Equals` is `false`** | 6 | every reachable subtest reports `v0 != v1` and **never** `v0.Value() != v1.Value()`, i.e. both `Make` calls interned the SAME `ж<T>`. Generator fix (chipped), not a hand-own |
| **`codegen-liveness` — a live C# local roots what Go proves dead** | 6 (same rows) | `checkMapsFor` reports `value X still referenced a handle`. The cleanup now RUNS and `CompareAndDelete` is reachable for the first time; `v0`/`v1` are simply still rooted where Go's per-safepoint liveness maps have already dropped them. The roster's existing disclosure class (`sync` carries several) |
| **`abi.TypeFor<T>()` for an interface `T`** | 2 | `EntryPointNotFoundException` at `IEquatable<ж<abi.Type>>.Equals` — unchanged, reflection-bridge row |
| **`GCHandle: Object contains references`** | 1 | now reached in `clone` → `ж<T>`→`uintptr` → `pinnedArrayData`, not on the `abi.Escape` path the old row named |
| **`array<T>.Equals` structural comparer** | 1 | `ArgumentException: Type of argument is not compatible with the generic comparer` for an array OF `[GoType]` structs. **New row**, previously masked |

Plus the standing `TypeFor().Name()` subtest-naming rows, which pair up by content but cannot match by
name.

**`TestPointer`'s failure is GC precision, and that is proven rather than argued.** Go's own
`pointer_test.go` does `st := wt.Strong()`, then `runtime.GC()`, then asserts nil — Go's compiler proves
`st` and `bt` dead; a C# frame reports them live. A dedicated probe separates the two by creating and
dropping the referent inside a `[MethodImpl(NoInlining)]` helper:

```
PASS  CONTROL plain object collects
PASS  CONTROL ж<int> in a self-keyed ConditionalWeakTable collects
PASS  CONTROL two-level CWT->ConcurrentDictionary keyed on ж<int> collects
PASS  Strong() is nil once the referent is unreachable (never probed)
FAIL  Strong() is nil once the referent is unreachable (probed first)
```

16 of 17 assertions pass, and the single failure is the probe's own frame holding the `ж<int>` that an
earlier `Strong()` returned. The controls make the ephemeron reasoning measured rather than assumed.

**Linkname PUSH registry — disposition unchanged, prose updated.** The two `internal/weak` rows STAY in
`linknamePushTargets` as loud stubs, because they describe what a conversion into a root that does not
already carry the hand-own must emit; the deployed corpus never regenerates the marked file. Their
`reason` strings now name `internal/weak/pointer.cs` instead of asking for it.

**Gates.** `internal/weak` + `unique` + `runtime` build clean; `go2cs-stdlib.slnx` **304/304**, 0 errors;
seeded full `-stdlib -comments` reconvert with the hand-own clobber gate; `go test ./...` in the
converter ok; GolibTests / ChannelTests at baseline; full behavioral suite. A/B footprint: **2** corpus
files (`internal/weak/pointer.cs`, `runtime/managed_impl.cs`) + the converter's registry comments.

## The WHOLE-CORPUS REBANK — 1,316 files, sixteen families, zero unclassified (2026-08-04, r40-rebank)

User ruling #6's one deliberate regeneration. The campaign's standing discipline is that the unit of
work is the CONVERTER FIX and that a corpus regen must never bury it, so arc after arc landed a gated
converter change and left `src/core` behind. This paid that debt in one session, in commits whose only
job is to BE that diff.

**It is a bank, not a repair** — every file is the already-gated output of a change that shipped with
its own behavioral guard, and the reconverted corpus builds 304/304 with zero errors.

### Family census — 1,299 files from the overlay (703 `.cs`, 298 `.csproj`, 298 `README.md`)

A file may carry several families; the count is files touched by that signature.

| Family | Files | What moved |
|---|---:|---|
| deref-accessor | 592 | `Ꮡx.Value` / `.ValueSlot` → `.DerefOrNull()` at pointer ENTRY aliases (r36 four-accessor, r37b param unification) |
| dead-param-alias | 541 | the entry alias is dropped outright where nothing reads it |
| GoBigConst | 304 | the rename reaching every emitted `.csproj` + 6 const sites |
| README-badge | 298 | `Go_tests` → `Tests` label (r39) + matched/total refresh; 298 removed / 298 added, so no README lost its badge |
| typed-nil | 145 | `Ꮡfd` → `Ꮡfd.OrTypedNil()` (r39-nilcomplex) |
| local-func | 90 | an only-called closure literal becomes a local function (r39e) |
| GoImplement | 44 | satisfies-not-witnesses: `encoding/binary` now records `bigEndian → ByteOrder`, which nothing ever cast to witness |
| value-adapter | 40 | …and therefore consumers stop minting `binary_bigEndianᴠByteOrder` |
| implicit-conv | 23 | importedPointerImplements retirement: `text/template` stops recording the foreign `parse` package's pairs |
| closure-box | 22 | a closure's own local needs no `ref heap<T>` box (r39e) |
| import-alias | 20 | the `using x = go.y_package` those records required, now unused |
| wrapper-qualification | 17 | `srcimporter_ImporterжImporter` → `srcimporter.ImporterжImporter` |
| pointer-reinterpret | 15 | `Ꮡ((T)(~p))` → `p.Reinterpret<F,T>()` (ruled, `70cbcad69`) |
| named-const-cast | 12 | an untyped const argument takes its named parameter type (`time.Sleep((time.Duration)(…))`) |
| fallthrough | 12 | the flag moves INSIDE the `do{}while(false)` so an early `break` no longer sets it — a real semantic fix |
| alias-pointer | 10 | `(ж<array<T>>)(uintptr)(new @unsafe.Pointer(x))` → `array<T>.AliasPointer(x, n)`, which stops copying the run |

**Reconciliation with the forecast.** 699 `.cs` carry a genuine drift family against the **695**
r39-nilcomplex measured on 2026-08-03 — agreement to within the arcs that landed between. The other 600
files are the rebank's own two corpus-wide relabels (302 GoBigConst, 298 badges), neither of which
existed when the forecast was taken. The r39c pointer **peephole showed no new drift**, as predicted:
every `@unsafe.Pointer` line in the diff belongs to alias-pointer or local-func, none to the peephole.

Plus 17 files the regen structurally cannot reach: the three hand-owned packages whose `.csproj` is
never re-emitted (`unsafe`, `internal/concurrent`, `internal/godebug`), the 13 `Perf*.csproj`
(regenerated by transpiling each benchmark — the Perf `.cs` proved to carry no drift at all), and the
Go comments in `BigUntypedConstComparison` that name the emitted type.

### Restored, not banked — and the third phantom shape

1. **28 auto-normalized CRLF phantoms** — dirty in `git status` with **no diff hunks at all** (they do
   not even appear in `--numstat`), each proven content-identical modulo CR, positive control fired.
2. **`-text` testdata copies** — ⚠ **the trap**: `src/core/compress/testdata/*` is marked `-text`, so
   git does NOT normalize it and a pure CRLF flip shows as a **real non-empty numstat**
   (`gettysburg.txt` 29/29). The standing rule "a phantom has an empty numstat" is therefore *false*
   for `-text` paths — test CR-equality directly instead of trusting the numstat.
3. **The `-tests`-closure production re-flip** — see the correction in DESIGN-named-interface-wrappers
   §7: the corpus now RESTS on the `-stdlib` side, but the asymmetry is intact and every sweep re-flips
   `using io = io_package;` to `using Δio = io_package;`. Restore, never bank.

### Confirmations this rebank was the right place to make

- **Hand-owned marker gate: 40 marked, 0 clobbered**, 16 `.cs.auto`. Unanchored grep reports 63 — the
  anchor is load-bearing. CLAUDE.md's census updated 32 → 40.
- **ZERO production-`.csproj` strips.** Backlog item 16 (a `-tests` run stripping the validation-pack
  block, "a loaded gun for the whole-corpus rebank") is defused by `ce82093b0`: the pack-block census
  held at 300 of 303 production csprojs across the full sweep, and no production `.csproj` changed.
- **The `.cs.auto` review siblings are TRACKED and 11 of 16 are STALE** — new backlog item 18. The
  overlay excludes them, and that exclusion is exactly what protects the hand-owned `.cs` beside them,
  so levelling them is a separate commit rather than something smuggled into a bank.

### The sweep — the policy inversion, and what it proved

A validated sweep is normally a GATE whose dirt is restored. Here the corpus itself had moved, so the
sweep's OUTPUT was the deliverable: **73 packages, 2,713 expected verdicts, 73 pass / 0 fail** in
2,736 s (45.6 min), every package at its exact banked count. **299 files banked** — 137 `*_test.cs`,
73 `*.tests.csproj`, 59 proof pages, 15 `go2cs_test_host.cs`, 12 `package_test_info.cs`,
3 `package_init.cs` — and **36 restored** across four shapes (13 closure re-flips, 10 `-text` testdata,
8 `.cs.auto`, 5 CRLF phantoms).

That a 1,316-file corpus bank moved no verdict anywhere the roster reaches is the strongest single
statement available that this was a bank and not a repair.

It also closed backlog residual #15 exactly as written — the seven banked `DerefOrNil()` sites in
`container/ring`, `go/token`, `index/suffixarray` and `testing/quick` re-emitted as `DerefOrNull()`
the moment each package's `-tests` pipeline ran, with no separate work. Zero remain.

⚠ **Two traps this half paid for, both worth carrying.**
1. **`-text` paths break the phantom rule.** `src/core/compress/testdata/*` is marked `-text`, so git
   does not normalize it and a pure CRLF flip shows a REAL non-empty numstat (`gettysburg.txt` 29/29).
   The standing "a phantom has an empty numstat" test is *false* there — compare CR-stripped content
   directly instead of trusting `--numstat`.
2. **Never amend a commit while a run that stamps its SHA is in flight.** The proof pages record the
   tree they validated against, so amending the corpus bank's message mid-sweep left 17 of 59 pages
   naming a commit that no longer existed. Recoverable only because the amend preserved the tree
   exactly (both SHAs point at tree `15e4eca18`), which made the stamp correctable textually rather
   than by re-running 17 packages.


## The r43c breadth pass — 58 candidates measured, 23 bank, every non-bank rooted (2026-08-07)

The charter says the cheapest scout is the pipeline itself, and until now it had only ever been
pointed at packages some *other* arc had named. This pass pointed it at the long tail as a batch:
**58 never-measured candidates** run back to back through
`go2cs -tests -test-action all -test-timeout 10m`, selected only by "its dependency closure is
already validated and it is not behind a known deep wall."

**Twenty-three validated on the first run, with no converter, golib or host change of any kind** —
roster **73 → 96 (44.7%)**, 13,070 matching verdicts. One candidate (`image/color/palette`) has no
eligible `Test` declarations at all. The remaining **34 are rooted below**.

The finding worth carrying forward is the negative one. None of the twenty-three needed anything;
the corpus had already grown past them, package by package, as forty banked packages' worth of
shared machinery landed, and **nobody had looked**. The roster's denominator was limited by
attention, not by defects. So: **re-scout the tail after any capability lands, not just the packages
that capability was aimed at** — a Tier-0 frog closes silently for packages nobody associated with it.

Two mechanical notes for the next person running a batch like this:

- The batch is cheap. A leaf package costs **9–20 s** end to end (convert, build, run both sides,
  diff). Fifty-eight candidates is under half an hour of wall time — far cheaper than reasoning
  about which ones *might* be close.
- The proof-page renderer and `docs/validation/index.md` update themselves on every successful
  `all` run, so a batch of banks costs no per-package documentation work. The roster row, its
  header arithmetic, and the closure-family restore are the only manual steps.

### The twenty-three

`crypto` · `crypto/aes` · `crypto/des` · `crypto/rc4` · `crypto/internal/alias` ·
`crypto/internal/bigmod` · `go/constant` · `go/doc/comment` · `go/format` · `go/printer` · `hash` ·
`image` · `image/color` · `internal/buildcfg` · `internal/coverage/cformat` ·
`internal/coverage/cmerge` · `internal/coverage/pods` · `internal/dag` · `internal/diff` ·
`mime/quotedprintable` · `net/url` · `testing/iotest` · `text/template/parse`

`go/doc/comment` alone contributes **10,059** verdicts — its `TestTestdata` walks every doc comment
in the standard library's own Go sources — and is now the largest single suite banked. `hash`,
`crypto` and `image` are worth noting for a different reason: each is a tiny *contract* package
whose suite exercises the whole family beneath it (`hash`'s marshal round-trip runs across all
eighteen stdlib digests; `crypto`'s out-of-bounds guards run every stream mode).

Four of the twenty-three flip a production `.cs` on every sweep, per the standing `-tests`-closure
family (`crypto/crypto.cs`, `hash/hash.cs`, `image/format.cs` — the `Δio` alias; and
`internal/buildcfg/package_init.cs` — the init-tests hook, which its test half implements nothing
of). All four are added to `run-validated-sweep.ps1`'s documented `$closureFiles` set so the sweep
keeps classifying them rather than reporting them as content drift.

### Build-blocked — eight roots

| Package | First diagnostic | Root, as far as it was taken |
|:--|:--|:--|
| ~~`log`, `go/scanner`~~ | ~~CS0012~~ | **CLOSED 2026-08-07 (r43f-closure-edge): both edges landed, `go/scanner` BANKED 11/11, `log` does NOT bank — two roots stand behind the closure one. Full account in the last section of this file.** The rooting below called both mechanisms correctly and was wrong about two details worth carrying: `log`'s literal is not `log.Logger{}` but `var l Logger` (a zero-value DECLARATION, in the INTERNAL white-box half — no composite literal exists, which is exactly why no literal walk could see it), and the implemented-interface gate is not `types.Implements` but the package's own emitted VALUE-form `GoImplement` RECORDS: satisfaction alone drifts 16 of the 96 banked projects. Original rooting: ~~**A fourth declaration-closure edge**, the same family the 2026-07-27 arc closed for interface bases, struct fields and member-access receivers. `log`'s external test half writes `log.Logger{}`; under the white-box `InternalsVisibleTo` grant the package-under-test's **internal fieldwise constructor IS a resolution candidate**, so binding it needs `atomic.Bool`'s assembly. `go/scanner`'s generated `ErrorList`↔`error` witness calls `m_value.Equals(…)`, and binding a member on `ErrorList` needs the assemblies of the interfaces **its own declaration implements** (`sort.Interface`, ×13). The existing rule's minimality gate fires the struct edge on an EMPTY literal only for a ROOT package — `log`'s case says the white-box grant is the same situation by a different route. Both are one edge each on `declarationClosureImports`, and both must be measured with that rule's own instrument: regenerate every banked `.tests.csproj` and require zero drift. **The cheapest two banks left on this list.**~~ |
| `slices` | CS0305 / CS0411 | Go infers `S ~[]E` **and** `E` from a single argument; C# cannot infer `E` from `S`. `Equal`/`EqualFunc`/`CompareFunc`/`Reverse`/`Insert`/`CompactFunc` emit as two-parameter generics and essentially every call site fails. Needs element-type deduction (or witness parameters) for constrained slice generics — the widest root in the batch, and it blocks the largest unbanked leaf (63 Test funcs). |
| ~~`archive/tar`~~ | ~~CS1537 ×3~~ | ~~`writer_test.cs` emits the same `using` alias **twice in one file** (`testFnc`, `fileMaker`), plus one CS0111. A test-half alias emission that does not dedupe within a file. Shallow.~~ — **BANKED 97/97 2026-08-15 (`claude/dup-append-emission`).** The rooting was right about the symptom and wrong about the scope in two ways: the collision is not per-file but per-COMPILATION (`global using`), and dedupe is not the fix — two functions declaring `type testFnc any` declare two unrelated Go types, so the alias takes the same enclosing-function lift every other local type-declaration kind already took. CS0121 on a one-field `nil` constructor stood behind it. See *CLOSED for `archive/tar`* at the end of this file. |
| ~~`archive/zip`~~ | ~~CS1929~~ | ~~The generated `ReadCloser`→`fs.FS` witness binds `Open` against a `ж<Reader>` receiver while holding a **value** `ReadCloser`~~ — **BUILD ROOT CLEARED 2026-08-09 (r56g).** The receiver split was a symptom: `Open` is a pointer-receiver method promoted from `ReadCloser`'s **exported** `Reader` value embed, and that promotion was not emitted at all (root 1), then emitted `internal` because the scope heuristic reads a tuple return's trailing `error)` as unexported (root 3). Package now BUILDS and RUNS at **95 of 98**; the residual is `TestZip64LargeDirectory` + 2 subtests as a **performance** row (Go 13.2 s, C# > 45 m), not a defect. See *r56g* below. — **BANKED 98/98 2026-08-09 (r57c)**: the performance row was `@string` slicing in O(n); see *r57c* at the end of this file. |
| `testing/fstest` | CS0030 | Converting the test-local named type `shuffledFS` to its underlying `map[string]*MapFile`. |
| `internal/types/errors` | CS0246 | `Error` / `Info` — names the emitted code does not declare for a test-local enumeration. |
| `crypto/ecdh` (CS1001), `crypto/ed25519` (CS0030), `crypto/internal/mlkem768` (CS0315), `runtime/debug` (CS0264) | — | not taken past the first diagnostic. |

### Runtime — rooted, not fixed

| Package | Root |
|:--|:--|
| `html` | **`map[K][N]T`'s missing-key read yields a zero-LENGTH array, not Go's zero-VALUE array.** `unescapeEntity` does `x := entity2[name]` over a `map[string][2]rune` and then tests `x[0]`; C#'s `default(array<rune>)` carries `m_length == 0`, so the read throws `IndexOutOfRange` where Go sees `0`. The class is wider than maps: **anywhere the Go zero value of `[N]T` (or of a struct containing one) is produced by C#'s `default(T)`, it is wrong the same way**. The converter already knows how to render the right thing (`arrayZeroValueArgs`, `visitArrayType.go`) — it simply is not consulted at a map read. |
| `internal/platform`, `crypto/internal/hpke` | **Same shape, two packages:** `json: cannot unmarshal array into Go value of type []T`, where `T` is a converter-**lifted anonymous struct** (`[]platform_test.listEntry`, `[]hpke.TestRFC9180Vectors_vectors`). A JSON array of arrays/objects decoded into a slice of a lifted type — worth one look, since two independent packages reach it. |
| `net/http/internal` | `TestChunkReaderAllocs` — an exact allocation-count assert, the established `alloc-count-semantics` class. Would be a disclosure candidate *only* after re-deriving the measurement; the rest of the package matched. |
| `go/ast` | `ast.Fprint` → `reflect.MapKeys` → `mapType.get_MapType()` fails an interface conversion inside go2cs-gen's promoted-field accessor. Reflection-bridge territory — that chip's, not a breadth lane's. |
| `go/parser` | `performance_test.cs`'s package initializer reads a testdata file **at cctor time** and panics, taking every test in the internal variant with it — the `-tests` init-relocation shape `internal/fmtsort` already needed a rule for. |
| `expvar` | Type-initializer failure inside a generated `ᴛRegisterAdapter` for `ΔStringжVar`; first divergent verdict `TestAppendJSONQuote`. |
| `internal/cpu` | `getGOAMD64level` is an unimplemented `PartialStubGenerator` stub; every GODEBUG-driven feature-mask row reaches it. |
| `testing/slogtest` | ✅ **BANKED 2026-08-07 (r44b-slog) — 17/17, no disclosures.** Both `log/slog` roots below are closed; see *`testing/slogtest` banks* at the end of this document. ~~`runtime.Caller` → the `getcallersp` stub, reached from a package initializer, so the whole package infrastructure-errors. Same `getcallersp` row the reflection arc carries.~~ **Caller root CLOSED 2026-08-07 (r43g-caller)** — the package now initializes and RUNS its whole matrix for the first time: `TestRun` 7 of 18 subtests pass. Two `log/slog` roots stand behind it, neither a slogtest defect: (1) **`unsafe.SliceData` over a reference-bearing element type** — `slog.GroupValue`'s `groupptr(unsafe.SliceData(as))` on `[]Attr` reaches `slice<T>.buffer` → `PinnedBuffer` → `GCHandle.Alloc(…, Pinned)`, which throws `ArgumentException: Object contains references` (5 infrastructure-errors: `groups`, `empty-group`, `inline-group`, `resolve-groups`, `resolve-WithAttrs-groups`); (2) a **`WithAttrs` attribute-loss** (4 fails: `WithAttrs`, `multi-With`, `empty-group-record`, `resolve-WithAttrs` — all "missing key"), whose likely shape is `Value.Kind()`/`isEmptyGroup` misclassifying a non-group value so `commonHandler.withAttrs`'s `countEmptyGroups(as) == len(as)` early-return drops the attrs. Both belong to a `log/slog` operational arc, which is unmeasured (`log/slog` is on neither the roster nor this board). |
| `internal/unsafeheader` | `TestTypeMatchesReflectType` / `TestWriteThroughHeader`: the converted `unsafeheader.Slice`/`String` do not alias the same storage a `slice<T>` does, so a write through the header is invisible. Structural — a managed slice is not a `{Data,Len,Cap}` triple. |
| `io/ioutil` | `TestReadDir` reads `..` and expects the **sibling** package's `io_test.go`. The pipeline stages Go sources only for the package under test, so the parent directory holds none. Environment, not conversion. |
| `internal/singleflight` | ✅ **BANKED 2026-08-14 (SCHED-S2) — 5/5, no disclosures.** ~~The only **hang** in the batch: `TestDoAndForgetUnsharedRace` never returns and the package hits the deadline.~~ The hang was never singleflight: it was the ThreadPool executor charging a parked goroutine against the capacity of ones that had not started. A dedicated thread per goroutine converges the test in **1.2 s** (was 28.7 min); see *Convergence measured 2026-08-12* → *RESOLVED 2026-08-14* above. |
| `crypto/cipher` (`TestGCMAsm`), `internal/godebugs` (`TestAll`) | one row each, both `Go="pass" C#="skip"` — a build-tag/capability gate the C# side answers differently. |
| `crypto/elliptic` (`TestInfinity/P224/Params`), `crypto/internal/edwards25519/field` (`TestBytesBigEquivalence`), `crypto/internal/boring/bcache` (init in `cache_test.cs`), `internal/chacha8rand` (`TestBlockGeneric`), `internal/profile` (`TestPackedEncoding` encodes empty), `encoding/asn1` (`TestCertificate`), `go/doc` (`Test/default/a`), `net/mail` (`TestAddressParser`), `net/http/httptrace` (`TestCompose`), `mime/multipart` (`TestLineContinuation`) | first divergent verdict recorded; not root-attributed. |

⚠ **One trap this pass hit, worth writing down: a corrupted GO BUILD CACHE reads exactly like a
package failure.** A host reboot mid-run left twelve zero-filled entries in `%LOCALAPPDATA%\go-build`,
and `crypto/internal/alias` then failed with `could not import crypto/internal/alias (EOF)` — reported
by the pipeline as `FAIL … [build failed]` on the **Go** side, i.e. the oracle itself. The tell is
that the same `go test` passes from a different working directory. `go clean -cache` is the blunt fix
and is machine-global (bad while siblings are running); the surgical one is to delete only cache files
whose first bytes are zero, which is a cache MISS rather than a corruption and is safe concurrently.
The same reboot zero-filled 566 files under `src/core/**/{bin,obj}` — those read as build failures too.

### The gate — 96 of 96, and what the aftermath said

The bank's gate is the full validated sweep at the NEW roster, and it ran clean: **96 packages, 96
matching at their exact banked counts, zero `COUNT` mismatches and zero failures.** (81 through
`run-validated-sweep.ps1`, which was killed externally at `path/filepath` — the machine-global
kill signature §9 warns about, not a verdict — and the remaining 15 driven straight through the
pipeline and cross-checked against the table's counts by hand.)

Two things in the aftermath are worth recording because neither is drift and both will recur.

**`src/core/time/package_init.cs` was a standing restore that no list named.** The `time` bank
recorded it in prose ("no committed `package_init.cs` in the corpus carries the hook, and time's
implements nothing") but never added it to `run-validated-sweep.ps1`'s `$closureFiles`, so every
sweep since has reported it under *CONTENT drift — inspect before banking or restoring*. It is now
listed, alongside the four this arc's own banks contribute.

**Twelve banked TEST sources are stale against the current converter, and it is pre-existing.**
`bytes/reader_test.cs`, `compress/flate/deflate_test.cs`, `context/benchmark_test.cs`,
`strings/reader_test.cs`, `sync/{cond,map,mutex,rwmutex,waitgroup,example}_test.cs`,
`time/{sleep,time}_test.cs` all re-emit differently — almost entirely the **capture suffix
renumbering** (`ʗ2` → `ʗ1`) that a later converter arc introduced, plus one comment-emission
difference in `sync/example_test.cs`. This lane changed no converter, golib or generator source
(`git diff master..HEAD -- src/go2cs src/core/golib src/gen` is empty), so the staleness is master's:
those packages were banked before the change and their test sources were never refreshed. Restored
here rather than banked — refreshing another package's test sources is a **rebank**'s job, not a
breadth lane's — and owed to the next one, alongside the `.cs.auto` review siblings (CleanupBacklog
item 18), eight of which drift the same way.

⚠ **One more environmental trap, alongside the build-cache one above: a full sweep at 96 packages can
FILL THE DISK.** Each package's test `bin` holds a copy of its whole closure, so a cold sweep writes
tens of gigabytes; this one exhausted `C:` mid-run with sibling lanes also building. The failure is
loud but misleading — the converter reports `failed to write to output source file … There is not
enough space on the disk` for `crypto/sha1/sha1.cs` and friends, i.e. it **truncates TRACKED corpus
files**, which then read as corpus corruption. `git checkout -- src/core` restores all of it, but the
lesson is to check free space before a full sweep and to prune `bin`/`obj` between chunks on a
contended box.

## The fourth and fifth closure edges — CLOSED; `go/scanner` banks 11/11, `log` has two roots behind it (2026-08-07, r43f-closure-edge)

The r43c rooting above named `log` and `go/scanner` "the cheapest two banks left on this list."
One of them was. Both build blockers are fixed by two new edges on `declarationClosureImports` —
the same family the 2026-07-27 arc closed for interface bases and struct fields and r38 extended to
member-access receivers — but only `go/scanner` banks. Full technical account, with both edges'
gates and their guards, in
[`ConversionStrategies-Reference.md`](../ConversionStrategies-Reference.md), *The fourth and fifth
closure edges*.

**Edge 4 — a ZERO-VALUE DECLARATION is a constructor call.** r43c read `log`'s blocker as
`log.Logger{}` in the external half. It is `var l Logger` in the INTERNAL (white-box) half, and the
difference is the whole point: there is no composite literal in the package's test sources at all,
so no `*ast.CompositeLit` walk could ever have found it. The converter renders Go's zero value of a
struct as a **constructor call** (`ref var l = ref heap(new Logger(), out var Ꮡl)` when the address
is taken, `new Logger()` otherwise), C# overload resolution materializes every accessible
constructor's signature before choosing one, and the white-box `InternalsVisibleTo` grant makes the
`internal` fieldwise overload accessible — `CS0012 … 'atomic_package.Pointer<>' … 'sync.atomic'`.
It is the existing root-scoped empty-literal edge's exact demand by another route, so it feeds the
same seed under the same gate.

**Edge 5 — a concrete type's bases live in its package's RECORDS, not in its declaration.**
`[GoType("[]ж<ΔError>")] partial struct ErrorList;` names no interface. `sort.Interface` reaches it
as a VALUE-form `[assembly: GoImplement<ErrorList, sort_package.Interface>]` record that go2cs-gen
realizes as `partial struct ErrorList : global::go.sort_package.Interface` **inside go.scanner.dll**
— so the metadata type declares the base and binding *any* member on it must resolve it. Thirteen
sites failed: `list.Sort()`, `len(list)`, `Ꮡlist.RemoveMultiples()`, and the generated
`ErrorList`→`error` value adapter's own `m_value.Equals(…)`.

⚠ **The correction worth carrying: the gate is the RECORDS, not `types.Implements`.** The natural
`go/types` statement of edge 5 — "the interfaces the receiver's type implements, from the declaring
package's imports", mirroring `interfaceBaseCandidates` one type-kind over — passes every unit test,
fixes `go/scanner`, and **drifts 16 of the 96 banked projects**. A record exists only where the
converter converted a CAST, so Go satisfaction wildly over-approximates the emitted base list:
`os.File` satisfies `syscall.Conn` and hands `syscall` to thirteen projects, though os records
`File` only against `io/fs.File` and `io.Writer` and both in **POINTER form**, which generates an
adapter CLASS rather than a base and demands nothing of a member binding; `bytes.Buffer` satisfies
most of `io` and hands `io` to `sort` and `unicode/utf8` though `bytes` emits **no records at all**;
`internal/buildcfg`'s `Stringer` hands it `fmt` from an equally empty set. All sixteen compile clean
today with none of it. Gating on the package's own value-form records — keyed **per type**, because
os's one genuine `syscall` record is for `rawConn` and not `File` — is **zero-drift** across all 96.
Two lessons generalize: (1) *satisfying an interface in Go is not carrying it as a base in C#*, and
the emitted `package_info.cs` is the authority on which is which; (2) this family's instrument keeps
earning its keep — it has now rejected **six** rules that a reading of C#'s binding rules justifies,
and this one was the most convincing of them.

**`go/scanner` — BANKED, 11 of 11**, roster 96 → 97 (45.1%), 13,081 matching verdicts. Whole token
and literal matrix, semicolon insertion, `//line` directives, `ErrorList` sort + one-per-line dedup,
CR stripping. No production `.cs` drift and no closure-family restore — the package contributes
nothing to `$closureFiles`.

**`log` — builds and RUNS for the first time, and does NOT bank.** Seven of its nine test functions
agree with `go test`; two roots stand behind the closure one, neither of them this family's:

| Test | Verdict | Root |
|:--|:--|:--|
| `TestAll` | `infrastructure-error` | `runtime.Caller` → `runtime.callers` → **`getcallersp`, an unimplemented `PartialStubGenerator` stub**. `log.output` calls `runtime.Caller(calldepth)` whenever the logger carries `Lshortfile`/`Llongfile`, and `TestAll` sweeps every flag combination. **This is the SAME `getcallersp` row `testing/slogtest` carries and the reflection arc tracks** — not a log defect, and the one root standing between log and a bank. A real `runtime.Caller` (managed `StackTrace`, or a hand-owned `extern.cs`) would likely bank log and slogtest together, and is worth its own arc. |
| `TestDiscard` | `fail` | `got 424 allocs, want at most 1` — an exact allocation-count assert, the established **`alloc-profile`** class. A legitimate disclosure candidate *once `TestAll` clears*; disclosing it alone banks nothing, so nothing was disclosed and no `log` artifact is committed. |

Both were reachable only after the closure fix, so the edge paid for itself twice over even where it
did not bank: `log`'s suite had never linked a host and had never been measured.

## `runtime.Caller` lands — and `log` still does not bank, for a reason worth naming (2026-08-07, r43g-caller)

The row above predicted "a real `runtime.Caller` … would likely bank log and slogtest together". The
`Caller` half was right and cheap; the prediction was wrong, in both packages, and the reasons are
different and both worth carrying.

**The fix is one entry, and it is on the FUNNEL.** `runtime.Caller`'s auto body calls the
*lower-case* `callers`, not the exported `Callers` the 2026-07-31 reflection chip hand-owned — and
`callers` is the declaration that opens with `getcallersp()`. Four call sites funnel through it
(`Caller`, `mprof`, `proc.createstack`, `tracestack`), so `"callers": true` on
`manualConversionFuncs["runtime"]` fixes all four and leaves `Caller` itself auto-converted and
Go-shaped. Corpus A/B footprint: **one file**, `src/core/runtime/traceback.cs` (the body becomes the
standard placeholder comment). Mechanism, the `skip + 1` / `skip + 2` frame budgets, the
`NoInlining` requirement, and the honesty boundary are in
[`ConversionStrategies-Reference.md`](../ConversionStrategies-Reference.md), *`runtime.Callers` /
`Frames.Next` walk the managed stack*. Guarded by the `RuntimeCallerFrames` behavioral test.

**`log` — 7/9 still, and `TestAll` is now an honest, measured divergence instead of a crash.** With
`Caller` alive, `TestAll` runs its whole flag matrix and produces real output. It fails on **Go
source geometry**, which the fix was never going to supply:

| Go asserts | The converted run reports |
|:--|:--|
| `` ^[A-Za-z0-9_\-]+\.go:(63\|65): hello 23 world$ `` | `C:\…\src\core\log\log_test.cs:69: hello 23 world` |
| `` ^.*/[A-Za-z0-9_\-]+\.go:(63\|65): … `` (Llongfile) | same, with `\` separators |

Three separate mismatches in one assert: the `.go` extension, the `/` path separator, and the exact
line numbers of the `Printf`/`Println` calls **inside `log_test.go`** (the test's own comment says
"must update if the calls to l.Printf / l.Print below move"). `Caller` reports the converted `.cs`
position because that is the source the running program has.

⚠ **This is deliberately NOT disclosed.** The bar for the disclosed-divergence manifest is an
assertion *unsatisfiable at any layer go2cs owns* (`alloc-profile`, `codegen-liveness`). This one is
satisfiable at a layer go2cs owns — a **Go-source position map**: either `#line` directives in the
emitted C# (the CLR's own transpiler mechanism; the PDB would then carry `.go` files and lines, and
`StackFrame.GetFileName`/`GetFileLineNumber` would answer in Go's terms for free), or a side-car map
per package consulted by `internCallerFrame`. Both are whole-corpus emission changes with real
trade-offs — `#line` noise cuts against the readability goal, a side-car adds a file and a csproj
item to every package — so this is an **architectural arc to design with the user**, not something
to slip in behind a bank. Until it lands, `log` stays off the roster; disclosing around it would
launder a missing capability as an unsatisfiable assert.

`TestDiscard` re-derived under the fix: **still `got 424 allocs, want at most 1`**, unchanged by this
arc (flag is `0`, so `Output` never reaches `Caller`). The figure is go2cs's `AllocsPerRun` shim
reporting **bytes** per run, not mallocs; Go allocates once (the `[]any{s}` variadic pack) for
`l.Printf("%s", s)` over a 102 400-byte string. It remains a legitimate `alloc-profile` candidate and
remains undisclosed, because disclosing it alone banks nothing — the same call r43f made.

**`testing/slogtest` — initializes and runs for the first time, 7 of 18 subtests pass, two `log/slog`
roots behind it.** Detail in the runtime-roots table above. The lesson is that slogtest is a *thin
wrapper over `log/slog`*: banking it is a `log/slog` operational arc, and `log/slog` has never been
measured at all. That is the recorded next candidate out of this lane.

## `testing/slogtest` banks, and `log/slog` gets its first census (2026-08-07, r44b-slog)

Both roots r43g left behind were real, both were converter/runtime defects with corpus-wide reach
beyond slog, and both closed. `testing/slogtest` went **7 of 18 -> 17/17 matched, no disclosures**
in one pass. `log/slog` was measured for the first time and does **not** bank, for reasons that are
now named rather than guessed at.

### Root 1 - `unsafe.SliceData` was a PIN where Go means an INTERIOR POINTER

`slog.GroupValue` stores a group as `groupptr(unsafe.SliceData(as))` plus `len(as)` and rebuilds it
with `unsafe.Slice` in `Value.group()`. That is identity and aliasing, never an address - but golib
answered `SliceData` with a pinned-buffer box over `slice.buffer`, and `GCHandle.Alloc(..., Pinned)`
refuses any storage whose element type carries a managed reference. Every grouping path in the
package infrastructure-errored with `ArgumentException: Object contains references`.

Go DEFINES `unsafe.SliceData(s)` as `&s[:1][0]`, so the faithful model is the array-element
reference the converter already emits for `&s[0]`. Pinning was never `SliceData`'s job: an address
is needed only at a `uintptr`/`void*` conversion, and the pointer box pins there on demand
(`EnsureStableAddress`), declining gracefully for storage that cannot be held still. Two further
latent defects fell out with it: the pin covered the whole backing array **from index 0**, so
`SliceData(s[2:])` addressed the wrong element and failed Go's `== &s[2]` identity; and
`PinnedBuffer` implements `IArray<byte>` alone, so the derived pointer was **undereferenceable for
every element type but `byte`**. A/B footprint: one hand-owned file, `src/core/unsafe/unsafe.cs`.
Guarded by the new `UnsafeSliceDataAliasing` behavioral test. Full mechanism in
[`ConversionStrategies-Reference.md`](../ConversionStrategies-Reference.md), *`unsafe.SliceData` is
an INTERIOR POINTER, not a pin*.

### Root 2 - the named-slice pointer reinterpret boxed a COPY, so out-parameters wrote nowhere

r43g's guess (a `Value.Kind()`/`isEmptyGroup` misclassification driving `countEmptyGroups`) was
wrong, and the real root is upstream of slog entirely. `commonHandler.withAttrs` writes its
pre-formatted attributes through `(*buffer.Buffer)(&h2.preformattedAttrs)` - a pointer conversion
from `*[]byte` to a named-slice pointer, whose whole purpose is that the bytes land in `h2`'s own
field. The converter emitted a wrapper box over a **copy**. Its own comment recorded the assumption
- *"aliasing with the original is not preserved ... but the reinterpret is used through the returned
pointer, which is the pattern"* - and that assumption is false for exactly the sites that matter.
`WithAttrs` dropped every attribute while still advancing `groupPrefix`/`nOpenGroups`, so the JSON
that followed was unbalanced: four slogtest rows (`WithAttrs`, `multi-With`, `empty-group-record`,
`resolve-WithAttrs`).

The fix routes the shape through golib's existing storage reinterpret -
`Reinterpret<slice<byte>, buffer.Buffer>()` over the field's own pointer - which re-views the same
slot as the wrapper. A generated named-slice wrapper is a single-field struct over the slice header,
precisely the correspondence `ReinterpretAliasesStorage` recognizes, so the managed alias arm engages
and writes reach the addressed storage. **The reach is wider than slog:** `crypto/tls`'s
`readUint{8,16,24}LengthPrefixed` and `parseECHConfigList`, and cryptobyte's `ReadASN1Bytes`, all
take `(*cryptobyte.String)(out)` on an out-PARAMETER or a struct FIELD - every one of them was
silently discarding what it parsed. Corpus A/B footprint: **5 files, 8 sites**. Guarded by the
extended `NamedSlicePointerReinterpret` behavioral test, whose previous version had written the
defect into its own comments as expected behavior and deliberately never read the source back; it
now does, on all four source shapes.

### `log/slog` - first census: 185 pass, 28 fail, 1 crash. It does not bank.

| Class | Rows | Disposition |
|:--|:--:|:--|
| **Go-source geometry** - `TestCallDepth`, `TestConnections` (+1 sub), `TestJSONAndTextHandlers` (+3), `TestPanics`, `TestRecordSource` | 9 | The SAME class r43g named on `log`'s `TestAll`: `runtime.Caller` honestly reports `logger_test.cs:905` where the assert wants `^logger_test\.go:\d+$`. **Not disclosable** - satisfiable at a layer go2cs owns (a Go-source position map: `#line` directives, or a per-package side-car). It is the architectural arc to design with the user, and it is what actually gates `log/slog` *and* `log`. |
| **`alloc-profile`** - `TestAlloc` (+13 subs), `TestAnyLevelAlloc`, `TestAttrNoAlloc`, `TestTextHandlerAlloc`, `TestValueNoAlloc` | 18 | The established disclosure class. **Nothing disclosed** - disclosing them alone banks nothing while the geometry class stands, which is the same call r43f and r43g made on `log`. |
| **Package initialization ORDER** - `TestLogLoggerLevelForDefaultHandler` (fail), `TestSetDefault` (the crash) | 1 + crash | A new, general root; see below. |

### Root 3 (found, NOT fixed) - Go initializes an imported package before its importer; C# does not

`slog`'s `init` captures `log/internal.DefaultOutput`, which **`log`'s own `init`** installs. Go's
spec orders that by the import graph, so the capture is always non-nil. A .NET module initializer
fires at first access to *its own* module, so whichever of `log`/`log/slog` is touched first wins:
touch `slog` first and `defaultHandler.output` is captured **nil**, and the next `slog.Info` is an
unrecovered nil-pointer panic that kills the process. In the census it aborted the run at
`TestSetDefault` and hid 34 further rows (re-measured separately: 32 of them pass).

Reduced to a 12-line standalone program that `go run` handles and the transpiled build crashes on:

```go
func main() {
	slog.Info("hello from slog")   // touch slog BEFORE anything in log
	var buf bytes.Buffer
	log.SetOutput(&buf)
	slog.Info("second")
	fmt.Printf("log buffer: %q\n", buf.String())
}
```

The mechanism to fix it already exists and is already documented - golib's
`builtin.initPackage(Type)` (`RuntimeHelpers.RunModuleConstructor`), which the converter emits today
for **blank** imports. Making every package force its DIRECT imports at module-init time reproduces
Go's ordering exactly and transitively (the import graph is a DAG, so direct-imports-only composes to
the full closure in post-order). That is precisely the extension
[`ConversionStrategies-Reference.md`](../ConversionStrategies-Reference.md) records as *"deliberately
deferred, not overlooked"*: it trades eager loading of the whole transitive assembly closure at module
init for fidelity. `log/slog` is the first case that NEEDS it, and it is a whole-corpus emission
change with a real startup trade-off - so it is left as an **architectural arc to design with the
user**, alongside the position map, rather than slipped in behind a bank. It buys 2 `log/slog` rows
on its own and would not bank the package.

### Aftermath noticed in passing: 24 README validation badges are stale

A full seeded `-stdlib` reconvert on this lane's converter differed from the committed tree in
**28** files: 3 are this lane's fix (above), 1 is `testing/slogtest`'s own new badge, and the other
**24 are `src/core/<pkg>/README.md` badges still reading `not_yet_validated`** for packages that
validated in `47ec27319` ("bank 23 packages from a measure-first breadth pass"). That bank wrote the
proof pages under `docs/validation/current/` but never overlaid the READMEs the converter composes
from them, so those 24 packages currently **under-report themselves on nuget.org**. Deterministic
converter output, zero risk to refresh - left for an idle-point overlay rather than folded into this
lane's commits. Affected: `crypto`, `crypto/aes`, `crypto/des`, `crypto/rc4`, `crypto/internal/alias`,
`crypto/internal/bigmod`, `go/constant`, `go/doc/comment`, `go/format`, `go/printer`, `go/scanner`,
`hash`, `image`, `image/color`, `internal/buildcfg`, `internal/coverage/cformat`,
`internal/coverage/cmerge`, `internal/coverage/pods`, `internal/dag`, `internal/diff`,
`mime/quotedprintable`, `net/url`, `testing/iotest`, `text/template/parse`.

**Resolved, and the CAUSE is standing (2026-08-08, r45b).** Those 24 were leveled by a later regen,
and r45b's Docs-badge overlay leveled the next batch — 12 of r44a/r44b's banks, plus
`internal/concurrent`'s label, which had frozen on the pre-2026-08-03 `Go_tests` spelling because the
package is hand-owned by consequence. The mechanism that creates them has not changed: a bank writes
`docs/validation/current/<dot-id>.md` and never re-emits the README the converter composes from it,
so **every bank leaves its own badge stale until the next corpus README overlay**. Treat a handful of
stale Tests badges as the EXPECTED state between overlays, not as a finding — and level them whenever
a lane is regenerating the corpus anyway.

**Recorded next candidates out of this lane.** Two architectural arcs, both now with named
beneficiaries: the **Go-source position map** (unblocks `log` *and* `log/slog`, ~9 rows across the
two) and **import-ordered package initialization** (correctness, not just verdicts - any converted
program that touches `log/slog` before `log` crashes today).
## The r44a re-scout — r43c's own lesson executed; 12 bank, and the tail's roots are named (2026-08-07)

r43c ended with an instruction rather than a finding: *re-scout the tail after ANY capability lands,
not just the packages that capability was aimed at.* Five capabilities landed between it and this
pass — managed weak references (`internal/weak`), per-field `[GoType]` struct equality,
`runtime.Caller` over a managed frame walk, range-over-every-integer-kind, and the `abi.Type`
StructType/ArrayType specializations — plus the linkname PUSH direction. This pass ran the pipeline
over BOTH halves of the tail that instruction names:

- **(a) the 32 still-unbanked packages r43c rooted** — its 34 minus `go/scanner` (banked at r43f) and
  minus `log`, whose position-map root is a board-documented architectural arc; and
- **(b) 76 never-measured tail packages** — everything unbanked and testable that is not a
  board-documented deep wall (`net`, `unique`, `os`) and not sibling-owned (`log/slog` and its
  subpackages, and `testing/slogtest`, which is a `log/slog` arc).

108 pipeline runs, `-test-action all -test-timeout 4m`, serial, on a corpus prewarmed by one
`go2cs-stdlib.slnx` build (304/304, 0 errors, 113 s), plus a 12-package re-run (below).
**Roster 97 → 109 (45.1% → 50.7%), 13,081 → 13,611 matching verdicts, 50 disclosed (unchanged).**

### The re-scout of r43c's own roots yielded exactly one package

`expvar` — r43c's "type-initializer failure inside a generated `ᴛRegisterAdapter` for `ΔStringжVar`"
— now validates **11 of 11**, with no change of any kind in this lane. **Every other package on
r43c's list re-measured verbatim**, down to the error code: the eight build blockers are unmoved and
the runtime roots reproduce their recorded shape.

That is a result, not a null: **the re-scout instruction is right, and its yield on an
already-rooted list is small.** A rooted non-validator has been looked at. The yield is in the
packages nobody has run — eleven of the twelve banks came from there.

### The twelve

`crypto/internal/boring` · `crypto/rand` (298) · `database/sql/driver` · `debug/buildinfo` (197) ·
`debug/plan9obj` · `expvar` · `go/importer` · `internal/cpu` · `internal/sysinfo` ·
`os/exec/internal/fdtest` · `plugin` · `runtime/internal/sys`

Eleven needed nothing at all. `internal/cpu` is the lane's ONE fix, below. `crypto/rand` (298) and
`debug/buildinfo` (197) carry the volume; `os/exec/internal/fdtest`'s single verdict is a
platform-gated **skip on both sides** — the converted run reaches Go's own `runtime.GOOS` guard and
declines exactly where Go does, which the proof page states plainly rather than dressing up.

### The one fix — `internal/cpu.getGOAMD64level`, and why 1 is a measurement

`TestDisableSSE3` opens `if GetGOAMD64level() > 1 { t.Skip(…) }`. Go reads 1 and walks on to a skip
inside `runDebugOptionsTest`; the converted run hit an unimplemented `PartialStubGenerator` stub and
infrastructure-errored, and that one row was the whole gap (7 of 8).

`getGOAMD64level` is declared in `cpu_x86.s` and its body is a **compile-time constant** — the
`GOAMD64_vN` define the toolchain sets from `go env GOAMD64`, with `#else MOVL $1` as the
fall-through. It answers *which microarchitecture level was this BINARY built for*, never *which
does this CPU support*; a v3 machine running a v1 build still reports 1, which is exactly why
`doinit` keeps the sse3/avx/avx512 GODEBUG knobs switchable at level 1. go2cs emits portable C# with
no GOAMD64 define and no microarchitecture-gated emission, so **1 is the same constant Go's own
assembly produces for go2cs's build configuration** — a measured property of the emission, not a
placeholder, and probing the CPU here would answer a different question. Registered in
`manualConversionFuncs["internal/cpu"]` with the body in `cpu_x86_impl.cs`. **A/B footprint: one
corpus file** (`cpu_x86.cs`'s declaration becomes the standard placeholder comment) plus the
hand-own. Marker census +1.

### ONE ROW AWAY — the list this pass most wants read

Eighteen packages match every verdict but one or two. Each cell is the whole gap.

| Package | Census | The row, and its root |
|:--|:--:|:--|
| ~~`runtime/internal/math`~~ | ~~0 of 1~~ | **BANKED** (roster line 143) — re-measured 1/1 by r57b |
| `internal/platform` | 0 of 1 | `json: cannot unmarshal array into` a slice of a converter-LIFTED anonymous struct (`crypto/internal/hpke` is the same shape). ⚠ r57b's naming arm changed the TEXT to Go's structural `[]struct { GOOS string; … }`; the row is a Kind question about the lift, not a naming one |
| ~~`internal/profile`~~ | ~~0 of 1~~ | **BANKED** (roster line 117) — re-measured 1/1 by r57b |
| `internal/godebugs` | 0 of 1 | `TestAll` reads GOROOT-relative `../../../doc/godebug.md`; the pipeline's working dir has none |
| `html` | 2 of 3 | the `array<T>` unshaped-instance class, producer (1) |
| `internal/chacha8rand` | 3 of 4 | the same class, producer (2) |
| ~~`internal/singleflight`~~ | ~~4 of 5~~ | **BANKED 5/5 by the scheduler arc** — the hang was the ThreadPool executor, not the package |
| ~~`internal/cpu`~~ | ~~7 of 8~~ | **BANKED this arc** |
| ~~`go/ast`~~ | ~~8 of 9~~ | **BANKED by r57b at 9/9** — two roots: the unbridged map read pair, then the lift's leaked C# name |
| `debug/gosym` | 8 of 9 | `TestPCLine`'s child process exits 1 |
| `debug/pe` | 9 of 10 | the `array<T>` unshaped class — `_ [3]uint8` prints `[0 0 0 0 0 0 0 0]` vs Go's `[0 0 0]` (r57b) |
| `net/http/internal` | 9 of 10 | `TestChunkReaderAllocs` — re-measured r58a as **2 objects/run against Go's budget of 1**, a lower bound; ruling pending, below |
| ~~`net/http/fcgi`~~ | ~~11 of 12~~ | **BANKED** (roster line 133) — re-measured 12/12 by r57b; the `TestGetValues` mismatch is gone |
| `crypto/cipher` | 13 of 14 | the oracle's build tags, below |
| `crypto/internal/edwards25519/field` | 13 of 16 | the `array<T>` class, producer (3) |
| `internal/poll` | 18 of 19 | `runtime_pollServerInit` — the netpoller has no managed body |
| ~~`net/textproto`~~ | ~~25 of 26~~ | **BANKED at 26/26 by L11 (2026-08-12)** — the want-ZERO row closed honestly: the 3 objects/816 B per `canonicalMIMEHeaderKey` call decomposed into `Once.Do`'s two ж field-box mints (fixed: atomic scalar ops → `[GoRecv] ref` form, once.cs hand-owned), the `m[string(b)]` probe-key copy (fixed: the converter now emits golib's zero-alloc `tmpstring` transient for map-READ keys, Go's own compiler special case), and 14 dead per-call `BigInteger.Parse`s from the emitted-but-folded 128-bit mask local (fixed: function-local int-kind `GoBigConst`s hoist their parse to a `static readonly` field). No disclosure, no test-shaping |
| `io/ioutil` | 27 of 28 | `TestReadDir` looks in `..` for the SIBLING package's `io_test.go`; also ORDER-DEPENDENT, since a sweep that ran `io` first leaves that file staged — a reason not to bank it even when it passes |
| `net/http/cgi` | 36 of 39 | three rows |
| `syscall` | 61 of 62 | **the pipeline's own path depth** — below |

### `syscall` — 61 of 62, and the one row is a bank the PIPELINE is costing itself

`TestGetwd_DoesNotPanicWhenPathIsLong` (Go issue 60051) calls `t.TempDir()`, then `os.Chdir`, and
skips itself if the Chdir fails. Go's run succeeds; the converted run's Chdir fails and the test
skips, because the C# host's temp root is
`%TEMP%\go2cs-tests\syscall\<32-hex-digest>\syscall\.tmp\<TestName>` — already deep before a test
whose entire purpose is to build a path past MAX_PATH adds its own. Shorten the staging root (short
prefix, truncated digest) and 62 verdicts should land. Rooted, not fixed: the staging path feeds the
input-digest manifest, so it is its own change with its own gate.

### The `array<T>` UNSHAPED-INSTANCE class — three producers, three packages, five rows

`array<T>` carries its Go length `N` in the INSTANCE — golib's own `IGoZeroShaped` says so, because
`[4]int32` and `[8]int32` are the same C# type. So every path that materializes one from TYPE
information alone must supply N, and three such paths do not:

1. **a map miss** — `html.unescapeEntity` reads `entity2[name]` over `map[string][2]rune`; the miss
   yields `default(array<rune>)`, length 0, and `x[0]` throws (`html` 2 of 3). r43c named this one.
2. **an unsafe reinterpret of an array pointer** — `internal/chacha8rand.setup` reaches
   `(*[16][4]uint32)(unsafe.Pointer(buf))` over a `*[32]uint64`, and the reinterpreted
   `ж<array<array<uint32>>>` has length 0 (`internal/chacha8rand` 3 of 4).
3. **the reflection bridge generating a value** — `testing/quick` → `reflect.Call` hands a
   zero-length `array<byte>` to a function taking `[32]byte`
   (`crypto/internal/edwards25519/field` 13 of 16, three rows).

`IGoZeroShaped` cannot serve any of them: it produces a zero value shaped like a value you ALREADY
have, and none of these three has one. Closing the class banks three packages (23 verdicts); each
producer is a separate fix and (3) is the reflection chip's.

### An untyped constant SHIFT computed in C# int32 — a silent wrong answer

`runtime/internal/math`'s `TestMulUintptr` reports `MulUintptr(1, 1) = 1, false want 1, true`. The
row is Go's `{1 << (UintptrSize / 2), 1 << (UintptrSize / 2), true}` with `UintptrSize == 64`; the
converter emitted `(uintptr)(1 << (int)((UintptrSize / 2)))`, and C# masks an `int` shift count to
five bits, so `1 << 32` is **1**. The NEIGHBOURING table row folds correctly —
`1<<(UintptrSize/2) - 1` → `(uintptr)(4294967296L - 1)` — because the shift is then an INNER node
whose recorded type is `UntypedInt` and `overflowingConstLiteral`'s SIGNED arm folds anything out of
int32 range. As the OUTERMOST node the shift carries the CONTEXT's `uintptr`, takes the UNSIGNED
arm, and that arm folds only values beyond int64. Its stated reason — "a TYPED unsigned constant
shift emits with a width-cast operand from the retype path" — is true for a shift the Go SOURCE
typed and false for a tree the context typed, which is precisely this case.

Corpus reach, measured: 69 `1 << (int)(<symbolic>)` sites; the counts are constants and nearly all
are below 32, but `runtime/mpagealloc_64bit.cs:234` is `(uintptr)(1 << (int)(heapAddrBits))` with
`heapAddrBits == 48` — `1 << 16` where Go computes 2⁴⁸. Latent, and the same silent-wrong-answer
shape. Deliberately NOT fixed here: `overflowingConstLiteral` already documents six carefully-scoped
rules and a wrong widening drifts the corpus silently, so this wants its own arc with an A/B.

### Three roots that each hold a whole package

- **`iter` — 0 of 28. `newcoro`/`coroswitch` are unimplemented stubs.** `iter.Pull`/`Pull2` are built
  on Go's coroutine primitive and every one of the package's tests goes through them. A
  self-contained arc of exactly the shape `sync`'s Mutex family and `internal/weak` took: the
  observable contract (a resumable producer, with `stop`, panic propagation and `Goexit`
  propagation) has a managed answer; Go's mechanism — switching stacks — does not.
- **`mime/multipart` — 7 of 52. A linkname PULL of an UNEXPORTED cross-package symbol.**
  `readmimeheader.go` is a bodyless `//go:linkname readMIMEHeader net/textproto.readMIMEHeader` —
  the PULL direction r43b never had to touch because it already worked, but only for a target the
  consumer can NAME. `net/textproto.readMIMEHeader` is unexported, so across the assembly boundary
  it is inaccessible and the declaration falls to the throwing stub. Remedy shape is an
  accessibility bridge, not a hand-own: the white-box test model already mints an
  `InternalsVisibleTo` grant for this exact problem.
  **CLOSED by L12 (2026-08-13): banked at 52/52.** This census was RIGHT — 7 of 52 reproduced to
  the verdict a week later, and the root is exactly the symbol named here. Two corrections worth
  carrying. (1) The remedy was over-designed: no `InternalsVisibleTo` grant was needed and none was
  written. The converter already owns both halves — `linknameForwardTargets` emits the forwarder
  body and `packageFuncAccess` widens a forward target its own package opened with a one-arg
  `//go:linkname` handle from `internal` to `public` — so the whole fix is ONE allowlist row, and
  the corpus moved by exactly two files (multipart's declaration becomes a forwarder; textproto's
  `readMIMEHeader` becomes `public`). The lesson generalizes: before designing an accessibility
  bridge, check whether the linkname machinery already covers the direction. (2) The differential
  was ENTIRELY this one symbol — all 41 infrastructure-errors carried the identical exception and
  the four `fail` rows were parent-test shadows — so the 45-row gap closed in a single step with no
  second root behind it.
- **`crypto/internal/nistec` — 0 of 2,200, build-blocked on four CS0311s**, all the same shape:
  `ж<P224Point>` (…P256/P384/P521) rejected as the type argument of the generic BENCHMARK helpers
  `benchmarkScalarMult<P>` / `benchmarkScalarBaseMult<P>`, whose Go constraint is `nistPoint[P]` — a
  self-referential interface constraint over a pointer receiver. Nothing EXECUTES those helpers;
  they merely have to compile. `crypto/ecdsa` (82) is blocked in the same family. **The largest
  single prize on this list.**

### Two findings that are NOT disclosures, and refuse for the same reason

**The differential oracle is not built with the corpus's build tags.** `crypto/cipher` matches on
every row but `TestGCMAsm`, where Go passes and C# **skips** with Go's own message, *"no assembly
implementation of GCM"* — the test's first act is `reflect.TypeOf(asm) == reflect.TypeOf(generic)`,
and under the standing `purego` ruling the converted corpus genuinely has one GCM implementation,
not two. **The C# side is right.** The oracle is what differs: `compareGoAndConvertedTests` runs
`go test -json -count=1 -timeout … .` with **no `-tags`**, while every conversion applies
`defaultStdLibBuildTags = {purego, math_big_pure_go}`. Go under the corpus's own tags would skip
that row too. It is satisfiable at a layer go2cs owns — one argument on one `exec.Command` — so
disclosing it would launder a comparison defect as an unsatisfiable assert. But it also changes what
EVERY roster row claims ("passes Go's tests" → "passes Go's tests as Go builds them for the pure-Go
configuration"), so it is an arc to design with the user and gate on a full sweep, not something to
slip in behind a bank.

**`AllocsPerRun` reports BYTES, and it now blocks a second package.** `net/http/internal` matches 9
of 10; `TestChunkReaderAllocs` reports `mallocs = 640; want 1`. r43g root-caused the same shape in
`log`'s `TestDiscard` (`got 424 allocs, want at most 1`): the shim measures allocated BYTES per run,
not allocation COUNT, because the CLR exposes `GC.GetAllocatedBytesForCurrentThread` and no object
counter. Two packages now stop here, which is the argument for owning it rather than disclosing
around it — until the shim reports a count, no `alloc-profile` disclosure at these sites can claim
the CLR *provably cannot satisfy* the assert, because nobody has measured the number the assert is
about.

> **r56d settled the units question by measurement, and the shim no longer presents bytes as a
> count.** The survey is recorded on the declaration itself (`testing.cs`, `AllocsPerRun`):
> net9.0/9.0.18 x64 exposes byte totals ONLY — `GetAllocatedBytesForCurrentThread` is exact
> (40.000 B/object over 1, 10, 1e3, 1e5 allocations of a 40-byte type) but cannot separate count
> from size, `GCAllocationTick` is a byte-threshold sample (378 events per 1,000,000 allocations,
> one per ≈105,820 B), `GCSampledObjectAllocation` — whose `ObjectCountForTypeSample` WOULD be a
> count — raises **zero** events through an in-process `EventListener` in every configuration tried
> (High `0x200000`, Low `0x2000000`, both, all keywords `0xFFFFFFFFFFFF`, Verbose and Informational)
> with the GC keyword's own tick count as the live positive control, `System.Runtime`'s 27
> EventCounters offer only `alloc-rate` (bytes/interval), and runtime events reach an in-process
> listener **asynchronously** (zero visible immediately after the loop, settling ≈117 ms later), so
> no event-derived figure could serve a synchronous call regardless. A nonzero result now notes its
> unit once on the running test; the zero case is left untouched because there the two units agree
> exactly, so no passing row's output moves (verified: 2,195 passing nistec rows carry no note).
> **The disclosure question is now answerable** — but it is still the user's, and it has a third
> option, below.

### r56d-allocdecomp — nistec's 21,964,011 decomposes, and 100 % of it is the `ж<T>` box model

The prize was gated on one number, so the number was decomposed the way r39-osalloc decomposed os's
9,184. **Method:** a console probe references the converted `crypto/internal/nistec` + `fiat` and
measures `GC.GetAllocatedBytesForCurrentThread` deltas — the same instrument the shim uses, so the
figures ARE the ones the test sees. Positive control: the probe's P256 body reads **21,963,547**
against the pipeline's **21,964,011**, the 464-byte gap being the `rand.Read` the probe substitutes.
Temporary counters in golib's `ж`/`array`/`slice` constructors (reverted; instrumentation is
temporary by construction) supplied exact per-class counts.

**Phase decomposition, P224 body (per run) — sums to within 156 B of the whole, the ibyteseq standard:**

| Phase | B/run | Share |
|:--|--:|--:|
| `ScalarMult(p, scalar)` | 13,042,167 | 55.2 % |
| `ScalarBaseMult(scalar)` | 5,592,992 | 23.7 % |
| `SetBytes(compressed)` | 4,556,755 | 19.3 % |
| `Bytes()` / `BytesCompressed()` | 203,226 / 203,194 | 0.9 % each |
| `NewP224Point().SetBytes(out)` | 17,681 | 0.1 % |
| `NewP224Point().SetGenerator()` | 8,344 | 0.0 % |
| `make([]byte, 28)` | 104 | 0.0 % |
| **whole body (control)** | **23,624,307** | 100 % |

**Unit costs close the bill to the BYTE** — three classes, and every field-element operation is
exactly `(number of field pointers × 128) + (number of address-taken locals × 144)`:

| Operation | Measured | Closes as |
|:--|--:|:--|
| `P224Element.Sub` | 528 | 3 × 128 + 1 × 144 |
| `P224Element.Mul` | 960 | 3 × 128 + 4 × 144 |
| `P224Element.Add` | 960 | 3 × 128 + 4 × 144 |
| `P224Element.Square` | 832 | 2 × 128 + 4 × 144 |
| `P224Point.Add` | 39,464 | ≈43 field ops + 8 `@new` boxes |
| `P224Point.Double` | 31,552 | same shape |

**Allocation COUNTS per run** (golib counters; Go's count for all four is **zero**):

| Curve | standard `ж` boxes | of which pinnable `T[1]` | field-ref `ж` boxes | `array<T>` backings | total objects | bytes |
|:--|--:|--:|--:|--:|--:|--:|
| P224 | 106,472 | 86,930 | 66,081 | 3,373 | **263,049** | 23,624,307 |
| P256 | 97,389 | 76,513 | 63,786 | 3,386 | **241,077** | 21,963,547 |
| P384 | 200,133 | 168,947 | 94,993 | 4,992 | **469,068** | 40,754,499 |
| P521 | 386,667 | 343,898 | 129,963 | 6,783 | **867,314** | 72,242,788 |

**Ownership, per class — none of it is established-class waste, and that is the finding:**

1. **field-ref boxes, 128 B (`of(…)`, i.e. Go's `&e.x`)** — a fresh `ж<array<uint64>>` per call.
   Go's `&e.x` is free and yields the same pointer every time, so *memoizing the box per
   (source, accessor)* is semantically faithful — but it is r39 item 1's territory and changes
   pinning lifetime, so it is **chip-class, design-WITH-user**, not a lane fix.
2. **address-taken locals, 144 B (`heap(new uint64(), out var Ꮡx)`)** — Go's `var x uint64; &x`
   handed to `p224CmovznzU64`, a stack variable there. 144 B = the `ж` box plus the `T[1]` pinnable
   slot its constructor allocates eagerly for an unmanaged `T`. Removing the eager slot needs the
   box pinned by handle instead — again the `ж<T>` architecture.
3. **`@new<T>()` boxes, 128 B** — Go's comment in `ScalarMult` says it outright: *"The explicit
   NewP224Point calls get inlined, letting the allocations live on the stack."* The managed model
   has no inlining that turns a heap box into a frame slot.
4. **`array<T>` backings, 88 B** — Go's `[4]uint64` is inline in the struct; golib's `array<T>` is a
   struct wrapping a heap `T[]`.

**The r39-killed classes did NOT reappear** — the hot path has zero dead `unsafe.Pointer` temps,
zero `GoFunc`/defer frames and zero capture boxes (the only closures are one-time `sync.Once`
initializers, outside the measured window). Checked explicitly, because a regression there would
have been a lane fix.

**So nistec does NOT bank, and the reason is honest**: five want-zero rows fail on a real
divergence, ruling #1 stands (a want-zero assert is satisfiable in principle, so it is not a
disclosure), and no established class remains to fix. Roster unchanged at **110/215**. The 2,200
verdicts are gated on the **`ж<T>` box arc** — the same arc `os`'s residual named — which makes that
arc's value 2,200 verdicts larger than it looked.

**The third option for the disclosure decision.** A true allocation COUNT *is* obtainable — not from
the CLR, but from go2cs's own runtime. golib allocates essentially every Go-semantic object, so
counting there mirrors precisely what Go's `Mallocs` already is: a runtime-owned counter, not a
platform facility. r56d proved it works (the count column above IS that instrument). It was
deliberately not landed: a count that silently omits allocation sites is worse than an honest byte
figure — the inverse-of-atomic rule — so making golib the counter requires an audited-total census
of its allocation sites and a ruling on what counts as an allocation. **Design-with-user.**

#### `log` and `net/http/internal` are a DIFFERENT case from nistec — and the difference decides them

Both remaining `AllocsPerRun` blockers assert a nonzero budget of **exactly 1**, where nistec asserts
zero. Measured through the shim itself (a temporary object-count readout, since these closures resist
a standalone probe — the pointer-to-interface conversions go2cs-gen mints are scoped to the declaring
assembly, so a hand-written probe cannot obtain them):

| Row | reported | true B/run | golib-tracked objects/run | Go's budget |
|:--|--:|--:|--:|--:|
| `log` `TestDiscard` | `got 424 allocs, want at most 1` | 424 | ≥ 2 | 1 |
| `net/http/internal` `TestChunkReaderAllocs` | `mallocs = 640; want 1` | 640 | ≥ 2 | 1 |
| `crypto/internal/nistec` `TestAllocations/P256` | `got 21964011.0` | 21,964,011 | 241,077 | 0 |

The top two are **the same order of magnitude as Go** — single-digit objects against a budget of one —
so their failures are dominated by the unit mismatch, not by over-allocation. nistec is five orders
away. That is the line the disclosure decision should follow.

⚠ **But they are still not disclosable today, and the reason is a result this lane produced against
itself.** The counter used above covers `ж`/`array`/`slice` only, and 424 bytes cannot be two objects
of ~50 B each — so allocations exist on that path which the instrument did not see (`@string`,
`object[]` varargs, delegates, boxing). **The partial counter demonstrating its own incompleteness is
the concrete evidence for the caveat above**: a golib-derived count is the right mechanism and is NOT
trustworthy until its census of allocation sites is audited-total. Until then no site can claim the
CLR *provably cannot satisfy* the assert, because the number the assert is about is still not known
exactly — which is precisely the standard r43g set. The lower bound is nonetheless decision-relevant,
and it points the opposite way from nistec.

### Build roots found in the never-measured tail

| Package | Verdicts | First diagnostic |
|:--|--:|:--|
| `crypto/tls` | 3,519 | CS0234 `'vendor' does not exist` — the test half's vendored import |
| `crypto/internal/nistec` | 2,200 | CS0311, above |
| `runtime` | 870 | build-blocked |
| `go/types` | 557 | CS0839 `Argument missing` |
| `encoding/json` | 491 | ~~CS0050 inconsistent accessibility~~ ~~· CS1061/CS1739/CS1503 ×8~~ — **the compile wall is CLOSED end to end** (2026-08-16, lanes `claude/json-measure` then `claude/json-unlock`: 76 → 8 → 0). The package RUNS: **400 of 491 matching**, 91 divergent in four named roots — see *`encoding/json` — the wall is down and the package RUNS* at the end of this file |
| `encoding/xml` | 386 | CS0426 `ΔToken` does not exist in `xml_package` |
| `crypto/x509` | 335 | CS0102 duplicate definition in `x509_package` |
| `net/netip` | 266 | CS1525 `Invalid expression term '<'` |
| `net/http` | 245 | CS1002 `; expected` |
| `html/template` | 243 | CS0030 on a test-local named type |
| `sync/atomic` | 108 | CS0103 `ᏑᏑX` — a DOUBLE address-prefix marker |
| `runtime/pprof` | — | CS0103 `ᏑᏑsalts` — **the same double-`Ꮡ` root** |
| `crypto/ecdsa` | 82 | the nistec family |
| `fmt` | 63 | ~~CS0111 `fmt_test_package.SE` already defines `Append`~~ — closed by `14bf20010`; ~~**five roots behind it**~~ — **the COMPILE WALL IS CLOSED** (2026-08-16, lane `claude/fmt-roots`): the host builds and **62 of 63 verdicts agree**. One verdict (`TestSprintf`) remains, behind three reflect/fmt-bridge divergences — see *`fmt`'s compile wall is CLOSED* at the end of this file |
| `text/template` | 52 | CS0030 on a test-local named type |
| `debug/elf` | 31 | CS8183 cannot infer the type of an implicitly-typed discard |
| `internal/reflectlite` | 30 | CS0016 could not write to output file |
| `database/sql` | 25 | CS0029 |
| `flag` | 24 | CS1929 on `ж<flag_test_package.URLValue>` |
| `os/exec` | 22 | CS0103 `The name 'var' does not exist` |
| `internal/concurrent` | 20 | CS0426 `node<,>` — the hand-owned `hashtriemap.cs` does not declare the internal type its WHITE-BOX test half references |
| `internal/runtime/atomic`, `internal/syscall/windows/registry`, `net/rpc/jsonrpc`, `go/internal/srcimporter`, `testing/fstest`, `internal/types/errors` | — | build-blocked, first diagnostic recorded |

**And one hard CONVERTER failure in 108 packages: `reflect`.** `go2cs.exe: Failed to convert package
tests in "…\src\reflect": convert test file "…\reflect\all_test.go": 1e+06 not an Int` — a
float-shaped untyped constant reaching a path that demands `constant.Int`. Every other package in
the batch CONVERTED; only the C# build or the run failed. This one has a one-line repro.

### Re-baselines this pass owes the board

- **`encoding/gob`: 98 → 99 of 106.** `TestNetIP` now passes (the `internal/weak` hand-own let
  `net/netip`'s initializer complete and the value render correctly). The seven remaining failures
  are the same gob-internal set.
- ⚠ **The first two rows below are SUPERSEDED — re-measured 2026-08-09 (r57a) after the r56f shift
  fix: `crypto/elliptic` is 82 of 82 and BANKED, `math/big` is 222 of 226.** Both were censused with
  the named-numeric shift-masking defect live, so they measured the defect rather than the package.
  Treat every census on this list as carrying a timestamp against the corpus it was taken on.
- **~~`crypto/elliptic` 4 of 82~~, ~~`math/big` 9 of 226~~, `go/doc` 24 of 85, `go/parser` 6 of 173,
  `mime/multipart` 7 of 52, ~~`encoding/asn1` 28 of 38~~ (**re-measured 34 of 38 by r57b**, below),
  `net/rpc` 6 of 15, `net/http/httputil` 16 of
  53, `net/http/httptest` 24 of 55, `net/http/cookiejar` 10 of 17, `debug/dwarf` 7 of 40,
  `internal/coverage/cfile` 4 of 16, `go/internal/gcimporter` 399 of 583** — first censuses, all
  recorded here rather than in prose. ⚠ The last two were **re-measured 2026-08-11 (L9, laptop G)
  and both reproduce EXACTLY** — walled, not stale, with their roots attributed in the L9 laptop-G
  section below.
- `net/internal/socktest`, `internal/syscall/unix`, `log/syslog`, `runtime/race` have **no eligible
  `Test` declarations on windows/amd64** — they are in the naive 215 denominator but cannot bank on
  this target.
- `os/user` cannot bank at all: Go's own `TestGroupIds` FAILS in the oracle.

### ⚠ Two self-inflicted traps, both worth the next lane's attention

1. **The corpus is an INPUT to a running batch.** Staging the `cpu_x86_impl.cs` hand-own while the
   batch was still running made six unrelated packages report
   `CS0111: Type 'cpu_package' already defines a member called 'getGOAMD64level'` — the impl
   implements a partial the CURRENTLY-BUILT converter still emits, and the error is reported against
   the CONSUMER package, not against `internal/cpu`. Never stage a converter-paired corpus file
   until the batch is idle and the converter is rebuilt.
2. **Clean the batch's untracked artifacts between passes.** The re-run then failed wholesale with
   `NuGet.targets(1311,5): error MSB4006: circular dependency … "_GenerateRestoreProjectPathWalk"`
   against `internal.syscall.windows.csproj`: a package whose run FAILED still leaves a generated
   `<pkg>.tests.csproj` on disk, and `internal/syscall/windows`'s test half imports
   `internal/syscall/windows/registry`, which imports `internal/syscall/windows` — a cycle NuGet's
   restore path walk rejects even though the C# compile would be fine. `git add` the banks, then
   `git clean -fd -- src/core`, before re-running anything.

## The one-row-away cluster, worked — 3 bank, and `syscall`'s root is not the one on record (2026-08-09, r56c-onerow)

Worked the ONE ROW AWAY list above in its own order. Three banked (`internal/profile` 1,
`net/http/fcgi` 12, `runtime/internal/math` 1); roster **110 → 113 (51.2% → 52.6%), 13,628 →
13,642 matching verdicts, 50 disclosed (unchanged)**. Every bank came from a converter or generator
defect that was producing a SILENT wrong answer — none needed a disclosure, and none was a
test-targeted patch.

### The three roots

1. **A Go package that spans two assemblies lost its unexported interface methods.**
   `internal/profile`'s `proto_test.go` is `package profile` — an internal white-box test — and it
   implements the production package's unexported `message` interface on its own `packedInts`.
   `ImplementGenerator` emitted the adapter's members as `=> default!` / `{ }`: a required member
   satisfied by a NO-OP. `marshal()` returned an empty buffer, `unmarshal` decoded nothing, and
   nothing at any layer said so. The stub is a real mechanism (Go's package-sealing markers —
   `ast.Expr.exprNode()`), but its test was `unexported name && declaring assembly != this
   assembly`, a proxy for "there is nothing to forward to" that answers wrongly for the one shape
   where a single Go package spans two C# assemblies. It now also requires the struct to declare no
   method of that name in the current compilation. **This class is corpus-wide**: any white-box test
   package whose test-local type implements a production unexported interface was silently no-op
   before this, and the failure mode is invisible — it compiles and it runs.

2. **C#'s `\x` escape is greedy where Go's is exactly two digits.** `net/http/fcgi`'s
   `const want = "\x0f\x01" + "FCGI_MPXS_CONNS1" + …` folds to one constant with no single
   `BasicLit`, so it bypassed `convBasicLit`'s byte-array diversion and the folded arm asked only
   `utf8.ValidString`. The value is pure ASCII, so that test passed it — and `\x01F` re-parsed as
   U+001F with the `F` eaten. `TestGetValues` compared a correct response against its own corrupted
   constant. The folded arm now runs the same predicate `convBasicLit` does. Measured reach: **one
   live site** — every other `\x`-plus-hex-digit run in the emitted corpus is inside a C# verbatim
   `@"…"` literal, where `\x` is two ordinary characters.

3. **`uintptr` was missing from `isWideShiftType`.** Go's `uint` renders as the C# primitive
   `nuint`, but Go's `uintptr` renders as golib's `uintptr` STRUCT — so it was the one wide unsigned
   type that fell to the narrow arm and got its shift cast on the RESULT, which is exactly what that
   arm's own comment says does not help. `1 << (4 * goarch.PtrSize)` emitted
   `(uintptr)(1 << (int)(32))`, C# masked the count to five bits, and the value was **1**.
   Whole-corpus A/B: eight files, one mechanical family, six sub-int32 reshapes and **two live wrong
   answers** — `MulUintptr`'s overflow fast path (guarding at 1, so every `uintptr` below
   `MaxUint32` "overflowed") and `runtime/mpagealloc_64bit.go`'s `1 << heapAddrBits` (2^16 where Go
   computes 2^48, the latent site this board already recorded). Both banked rather than deferred.

### ⚠ `syscall` 61/62 — the recorded root is WRONG, and the recorded remedy cannot work

This board says the row is "the pipeline's own path depth … shorten the staging root and 62 verdicts
should land." **Both halves are false, and the correction matters because the real remedy is cheap
and sits in another lane's file.**

`TestGetwd_DoesNotPanicWhenPathIsLong` skips on `Chdir failed: … The filename or extension is too
long`. `MkdirAll` SUCCEEDS — only `Chdir` fails. The arithmetic refutes the depth story on its own:
the test appends two 200-character segments, so it contributes **401 characters** whatever the root
is. The converted run's path is ~551; **Go's own is ~488**. No staging root gets the total under
`MAX_PATH` (260) — Go is not passing because its path is shorter, it is passing at 488 characters,
which is already 1.9x the limit.

Probed directly — same 446-character path, same machine:

| binary | `SetCurrentDirectoryW(plain)` |
|:--|:--|
| Go (`os.Chdir`) | succeeds |
| .NET (`dotnet run`, no manifest) | **fails, error 206** (`ERROR_FILENAME_EXCED_RANGE`) |
| .NET, `<ApplicationManifest>` carrying `<ws2:longPathAware>true</ws2:longPathAware>` | **succeeds** |

The root is that **converted Windows binaries are not long-path aware and every Go Windows binary
is**. `MkdirAll` worked because Go's `fixLongPath` prefixes `\\?\` explicitly; `Chdir` hands
`SetCurrentDirectoryW` a plain path, and without the opt-in the process is held to `MAX_PATH`.
(`\\?\` is no escape hatch here: `SetCurrentDirectory` rejects the extended form outright — it fails
206 too.)

**CLOSED 2026-08-09 (r56e) — `syscall` banks at 62/62. The diagnosis above held; the MECHANISM
attributed to Go did not, and the correction changed the remedy.** Go's linker bakes in no manifest.
`runtime/os_windows.go`'s `initLongPathSupport()`, called from `osinit()`, checks for Windows
10.0.15063 and then sets the undocumented `IsLongPathAwareProcess` bit in the PEB's bit field
itself — which is why every Go Windows binary is long-path aware.

That distinction is not academic, because the two routes are **not** equivalent: Windows honors a
manifest's `longPathAware` only when the machine-wide policy
`HKLM\SYSTEM\CurrentControlSet\Control\FileSystem\LongPathsEnabled` is ALSO 1. It is 1 on this
machine — which is exactly why the manifest measured as a fix in the row above — so a manifested
converted binary would still have diverged from the Go binary on a default install, where that value
is 0. Go asks for neither the manifest nor the policy.

So the remedy landed in **golib, not the csproj template**: `builtin.WindowsLongPaths.cs` sets the
same PEB bit from `InitializeGoLib`, golib's analogue of `osinit`. Probed both ways in one process —
without golib the PEB reads `0x04` and a 434-character `Directory.SetCurrentDirectory` fails
`0x800700CE`; referencing golib it reads `0x84` before the probe's own code runs and the same call
succeeds. It is also the far smaller footprint: no `<ApplicationManifest>` property, no per-project
manifest artifact, nothing in the emitted `.csproj` — so CNR stayed byte-identical across all 576
behavioral packages *including* their `.csproj`, and none of the banked `<pkg>.tests.csproj` went
stale. `internal/syscall/windows.CanUseLongPaths` is deliberately left false (golib cannot reference
a converted package, and the `\\?\` spelling still works with the bit set).

### Rooted, not fixed — carried back with evidence

- **`debug/pe` 9/10 — a byte-level struct pun across surrogate layouts.** `COFFSymbolAuxFormat5`
  prints `_:[0 0 0 0 0 0 0 0]` where Go prints `_:[0 0 0]`. Go reinterprets a `COFFSymbol` as the aux
  record (`(*COFFSymbolAuxFormat5)(unsafe.Pointer(&sym))`); the two have identical GO layouts but no
  field correspondence at all (`Name [8]uint8` vs `Size uint32 + NumRelocs uint16 + …`). golib's
  alias route correctly REFUSES this (6 fields vs 7, not layout-compatible), so it falls to the
  raw-address route — which reads the aux struct's `array<uint8>` field out of the bytes where
  `COFFSymbol.Name`'s reference sits: a fabricated managed reference that happens to be
  type-compatible, so it aliases `Name`'s 8-element array instead of a fresh 3-element one. The
  scalars round-trip only because the same wrong mapping is used in both directions. A correct
  answer needs a Go-LAYOUT marshalling view for the pun, not a shape patch; that is an arc, and the
  fallback's "never something newly wrong" claim in `ж.PointerExtensions.cs` deserves revisiting
  with it — here it fabricates a reference, which is the very thing the alias route refuses to do.
- **A GOROOT-tree-reproduction class: four packages, one question.** `go test` runs a package's
  tests with cwd = the package's GOROOT source directory; the converted host runs in the staged
  copy, and the staging deliberately bounds itself to paths carrying a `testdata` segment. So
  `debug/gosym` 8/9 (`TestPCLine` runs `go build` in `testdata` and dies on `go.mod file not found`
  — GOROOT/src has one, the staged tree does not), `internal/godebugs` 0/1
  (`../../../doc/godebug.md`), `internal/platform` 0/1 (reads `zosarch.go` from cwd, behind its own
  JSON root), and `io/ioutil` 27/28 (lists `..` for a sibling package's file) are ONE question: how
  much of the GOROOT tree around a package should the run reproduce? Pointing the host's cwd at the
  real GOROOT package directory answers all four and makes both sides see literally the same tree —
  and would also make `io/ioutil` order-INdependent, retiring the reason this board gives for not
  banking it. But it lets a test write into GOROOT and it trades away the staged copy's
  reproducibility, so it is a pipeline design decision, not a defect fix.
- **`net/http/cgi` 36/39 is TWO roots, not three rows.** `TestCopyError` infrastructure-errors on
  `GetSystemDirectory: external (assembly or cgo) function is not implemented` — a `//go:linkname`
  PUSH from `runtime` that is not in `linknamePushTargets`, and it throws out of `net_package`'s
  type initializer, so **every `httptest` consumer dies in `net`'s cctor**. Its pushed body reads
  `runtime.sysDirectory`, which `initSysDirectory` fills via `stdcall2` — nothing the managed model
  runs — so a bare forwarder would hand back `""`: a plausible-looking wrong answer, which is
  exactly what the registry's own rule forbids. The honorable shape is the one `os.runtime_args`
  already took: a hand-owned module initializer populating `sysDirectory` from
  `Environment.GetFolderPath(SpecialFolder.System)`, with the registry row landing WITH it rather
  than before it. `TestDir`/`TestEnvOverride` are the staging-cwd class above — the re-exec'd CGI
  child resolves a different `go2cs-tests` root than the parent's `os.Getwd` reports.
  **DONE 2026-08-09 (r56e), exactly as prescribed** — the row (`bareDecl: false`; this is the handle
  consumer shape, the first forwarded one since `unique`) and `runtime/windows/os_windows_impl.cs`
  landed together, reproducing Go's trailing backslash and its "Unable to determine system directory"
  throw. Measured over the built corpus: `GetSystemDirectory()` returns `C:\WINDOWS\system32\` and
  `net`'s cctor initializes.
  ⚠ **But "every httptest consumer dies in net's cctor" over-generalized from the `cgi` case, and the
  board should not carry it forward unqualified.** Re-measured `net/http/httptest` after the fix:
  the `GetSystemDirectory` throw is entirely ABSENT from the run (0 occurrences), yet the census is
  **~23 pass / 25 fail / 3 infrastructure-error of 55**, essentially unchanged from the 24-of-55
  first census recorded above. The cctor was a real blocker and it is gone; it was simply not
  `httptest`'s BINDING one. Its dominant remaining failure is the already-tracked `array<T>`
  unshaped-instance class (`panic: index out of range [0] with length 0` inside
  `go.array\`1.get_Item`), which is that arc's to own. So the unlock should be re-measured per
  package rather than assumed to free the family.
- **`internal/poll` 18/19** — `runtime_pollServerInit` is a `PartialStubGenerator` stub reached
  through `sync.Once` from `pollDesc.init`; the netpoller has no managed body. Unchanged from this
  board's own reading.
- The `array<T>` unshaped-instance class (`html` 2/3, `internal/chacha8rand` 3/4) was re-confirmed
  at both producers and left for the arc that owns it. One measurement worth carrying: the map-miss
  producer is **two sites in the whole corpus** (`html/entity.cs`'s `map[string][2]rune`, and a
  `map[int][2]int` inside `encoding/csv`'s already-banked test half), which is small enough that an
  index-site shaped zero — the same statically-known-shape route `arrayZeroValueArgs` already is —
  is a contained fix rather than a new mechanism. A map INSTANCE cannot carry the shape: a nil map
  is `default(map<K,V>)` and reading one is legal Go, so there is no construction site to record it
  at.

## The r56a breadth harvest — the packages with no board row at all; 4 bank, 1 fix, 12 rooted (2026-08-09)

r44a ran 108 packages and left an instruction of its own: *the yield is in what nobody has run.* This
pass took that literally and asked a narrower question than "what is unbanked" — **what is unbanked
and has never appeared on this board in any form**. Of the 106 unbanked testable packages, exactly
**sixteen** had no row, no census, and no mention: the residue r43c's 58 and r44a's 108 between them
never enumerated. Every one was run.

**Roster 110 → 114 (51.2% → 53.0%), 13,628 → 13,645 matching verdicts, 50 disclosed (unchanged).**

The pass's own lesson is a refinement of r44a's rather than a repeat: **three of the four banks came
from ONE fix, and that fix was already designed.** `golib/GoReflect.TypeLayout.cs` carried a written
deferral — "unifying `unsafe.Sizeof` onto this rule is deferred pending a named consumer" — and the
consumer had been sitting in the unmeasured tail the whole time, three packages deep. A deferred
unification with a named trigger is worth re-reading every time the tail is re-scouted; the trigger
does not announce itself.

### The four

`debug/macho` (7) · `go/internal/gccgoimporter` (4) · `internal/xcoff` (3) ·
`log/slog/internal/benchmarks` (3)

The first three are one root. The fourth needed nothing at all.

### The one fix — `unsafe.Sizeof` had two rules and only one of them was Go's

The converter FOLDS `unsafe.Sizeof` to a constant wherever `go/types` can compute one — 283 corpus
sites. The folding arc that landed that named what it could not reach: an operand whose type is a
**type parameter**, which Go's own spec calls variable-size and does not fold either. Seven run-time
call sites remain corpus-wide, and they kept riding `Marshal.SizeOf<T>`.

There the "latent throw" that arc documented was not latent. A type parameter binds at run time to
exactly the shapes `Marshal.SizeOf` refuses — a generic type (*"The specified Type must not be a
generic type"*) or a struct holding a managed reference (*"cannot be marshaled as an unmanaged
structure"*). Three packages died on it through the SAME one line, `internal/saferio.SliceCap[E]`,
which asks the size only to choose an allocation chunk:

| Package | `E` bound to | Reached from |
|:--|:--|:--|
| `debug/macho` | the `Load` **interface** | `NewFile`, `NewFatFile` |
| `internal/xcoff` | `ж<Section>` | `NewFile` |
| `go/internal/gccgoimporter` | `debug/elf.ΔSection` | `elfFromAr` → `elf.NewFile` |

The run-time form now answers through `GoReflect.GoSizeOf` — the same Go-layout walk that stamps a
descriptor's `Size_` and that `reflect.Type.Size()` reads — with `Marshal.SizeOf` retained as the
fallback for the shapes `GoSizeOf` declines, so nothing that resolved before stops resolving. It is
also *correct* where the old rule merely differed: `Marshal.SizeOf` reports a **bool** as 4 bytes
where Go says 1, so any struct holding one was being measured wrong at precisely the sites folding
could not reach. **A/B footprint: one method body.** Recorded in
`ConversionStrategies-Reference.md` beside the folding subsection.

### What the fix moved that did NOT bank — `debug/dwarf` 7 → 30 of 40, and its residual is ONE root

`debug/dwarf` opens its fixtures through `debug/macho` and `debug/elf`, so the Sizeof fix took it
from the board's recorded **7 of 40** to **30 of 40** with no work aimed at it. All ten residual
rows are one panic, at `debug/dwarf/type.cs:683`:

```
panic: interface conversion: interface {} is *dwarf.UintType, not dwarf.readType_type
```

Go's source asserts to an **anonymous interface** — `typ.(interface{ Basic() *BasicType })` — which
the converter lifts to a package-local `[GoType("dyn")] partial interface readType_type`. The
concrete types (`*IntType`, `*UintType`, `*CharType`, `*UcharType`, …) satisfy it in Go only through
a method **PROMOTED from an embedded `BasicType`**, and the value is held as a *different* named
interface (`Type`) at the assertion site. No witness is minted for that combination, so the assert
throws. Ten rows, one root, in the `go2cs-gen` `ImplementGenerator` family — the largest single
prize this pass leaves rooted, and the reason `debug/dwarf` is now a *near* miss rather than a
distant one. (`debug/elf` itself is unmoved: its blocker is the recorded CS8183 implicitly-typed
discard at `file_test.cs:1195`, a build root this fix does not touch.)

### The twelve rooted non-validators

| Package | Census | Root |
|:--|:--:|:--|
| `internal/runtime/syscall` | — | *"build constraints exclude all Go files"* on windows/amd64. Joins `net/internal/socktest`, `internal/syscall/unix`, `log/syslog` and `runtime/race`: in the naive 215 denominator, cannot bank on this target. |
| `runtime/trace` | 0 of 2 | `NotImplementedException: getg: external (assembly or cgo) function is not implemented`. Both tests enter the tracer through `getg`; no managed body exists. |
| `log/slog/internal/buffer` | 1 of 2 | `TestAlloc`: *"got 304 allocs, want 0"*. **Re-measured r58a with the counter live: golib charged NONE of the 304 B/run**, so AllocsPerRun fell back to bytes rather than report a zero it could not vouch for. Every object on this path is compiler-emitted or BCL-internal — the structural class no golib census reaches — so it is still not a disclosure candidate, now for a measured reason rather than an assumed one. |
| `internal/trace/internal/oldtrace` | 2 of 3 | `TestParseCanned`: the pre-1.22 trace parser rejects two of its own canned good traces — *"p 3 is running before start (time 369986239)"* and *"previous sweeping is not ended before a new one"*. Parser-state semantics, not I/O. |
| `internal/testenv` | 3 of 4 | `TestGoToolLocation` looks for `<staging root>/bin/go.exe`; the converted host's GOROOT is the pipeline's exported root, which has no `bin`. Same shape as `internal/godebugs`' GOROOT-relative `doc/godebug.md`. |
| `internal/fuzz` | 0 (build) | `minimize_test.cs(177): CS1003` — a **func-literal parameter whose type is an ALIAS to an anonymous struct** emits the Go type STRING verbatim: `(struct{Parent string; Path string; …} e) => …`. `CorpusEntry` is `type CorpusEntry = struct{…}`, and production emission handles it correctly (`global using CorpusEntry = …CorpusEntryᴛ1`), so the lift exists and the func-literal parameter position does not consult it. |
| `internal/trace` | 0 (build) | `batchcursor_test.cs(92): CS0149 Method name expected` — a parameter **named `heap`** shadows golib's `heap()` intrinsic that the same body calls (`ref var sb = ref heap(new strings.Builder(), …)`). A name-collision rule the analysis does not cover: a local or parameter whose name collides with a golib intrinsic the body invokes. |
| `crypto/internal/edwards25519` | 0 of 55 → **52 of 55 with the tuple-spec fix** | **Package-var init ORDER, tuple-spec hole.** Go initializes `feOne`(0) and `d`(1) before `identity`(2); C# field initializers run in declaration order, so `identity` (line 66) reads `feOne` (line 140) while null, `field.Subtract` null-derefs, and the package cctor throws before any test runs. The general init-order mechanism **already exists and is correct** (`initOrderOperations.go`, landed `e39855770` 2026-07-11; 36 packages ship a generated `package_init.cs`) and it **flags these two vars correctly** — it then declines to act because they are TUPLE specs (`var identity, _ = …`), warning loudly at `visitValueSpec.go:1158`. Whole-corpus census: **exactly 2** production occurrences (both here) on Windows, **2 latent** on darwin (`os` `initCwd`/`initCwdErr`), zero elsewhere; the sibling hoisted-initializer fallback never fires. Hand-simulating the relocation takes the package to **52 of 55**; residual = `TestAllocations` (AllocsPerRun class, 5th member) and `TestScalarSetCanonicalBytes`/`TestScalarSetUniformBytes` (one shared **new** root: `testing/quick` + reflection bridge synthesizes a zero-length array for a fixed-size `[32]byte`/`[64]byte` parameter). Options, costs and recommendation: [`FINDING-init-order-tuple-specs.md`](FINDING-init-order-tuple-specs.md). **Option A ratified 2026-08-10** (extend the existing relocation to tuple specs, ~30 lines reusing the landed machinery); implementation sequenced into the post-1.23.1.6 harvest window. |
| `net/smtp` | 9 of 14 | `TestNewClientWithTLS` fails with `loadcert: tls: failed to parse private key`; `TestSendMail`, `TestSendMailWithAuth`, `TestTLSClient` and `TestTLSConnState` infrastructure-error behind it. Shares its root with `crypto/rsa` below — PEM/ASN.1 private-key parsing. |
| `crypto/rsa` | **BANKED r58a — 559 matching + 1 disclosed = 560** | ~~0 of 592; the test package's own static initializer panics in `parseKey` → `x509.ParsePKCS1PrivateKey` → `asn1.Unmarshal` → `parseField` "sequence truncated".~~ **The cctor panic is GONE**, closed by r56f's `reflect.StructField.Tag` bridge exactly as that write-back predicted: `parseField` reaches its `asn1:"…"` parameters through `field.Tag.Get("asn1")` (`asn1.cs:971`, `marshal.cs:509/514`), so while every converted struct reported UNTAGGED the DER walk read every field as having no `optional`/`explicit`/`tag:` modifiers and desynchronized on the first one that mattered. With tags bridged the whole suite runs: **560 verdicts, 559 matching, 13 excluded** (8 benchmarks + 5 examples, Phase-4D). The single mismatch is **`TestAllocations`** — `testing.AllocsPerRun(100, …)` around `DecryptPKCS1v15` — and it is the **AllocsPerRun-reports-BYTES shim**, now its FOURTH member after `log`'s `TestDiscard`, `net/http/internal`'s `TestChunkReaderAllocs` and `log/slog/internal/buffer`'s `TestAlloc`. Measured: **2,851,392,000 bytes over 100 runs = 28,513,920 B/run**, reported where Go reports a malloc COUNT. **Not banked and NOT disclosable** on the standing rule — the shim has never reported the number the assert is actually about, so disclosing it would launder an unmeasured quantity. This is now the largest prize gated on that one decision: **560 verdicts held by a single row**, which is the strongest argument yet for the carried AllocsPerRun-ownership item (r56d showed golib's own `ж`/`array`/`slice` constructors can supply an exact object COUNT — that is the design-with-user path to banking this package). `net/smtp`'s five and `encoding/asn1`'s 28-of-38 shared this root and are both worth an immediate re-measure. |
| `go/build` | 57 of 58 verdicts (34 of 35 top-level) | `TestLocalDirectory`: `ImportPath="."`, want `"go/build"`. The test calls `ImportDir(os.Getwd())`; `go test` runs from the GOROOT package dir, the converted host runs from `src/core/go/build`, which is not inside a Go source tree. **The converted-host WORKING-DIRECTORY class**, third member after `internal/godebugs` (0 of 1) and `io/ioutil` (27 of 28). Not a disclosure: it is satisfiable at a layer go2cs owns (the staging root's identity), so disclosing it would launder a harness limitation as an unsatisfiable assert. |
| ~~`crypto/dsa`~~ | **DONE 2026-08-09 (r57a) — 4 of 4, banked.** The row's diagnosis was right and its conclusion was wrong by about ninety seconds. `TestParameterGeneration` **passes in 1,156.8 s (19.3 min)**, so the 20 m package deadline this row measured at was just UNDER what the package needs end to end — the deadline has to cover conversion, the C# host's startup and the `go test` oracle beside it, so it cut a run that was converging. At **30 m** it validates first try, and `crypto/dsa` is now the third `$longTimeouts` entry beside `hash/maphash` and `index/suffixarray`. ⚠ Two lessons worth carrying: "no `-test-timeout` is enough" is a claim no timeout can ever establish — only a completed run distinguishes a slow suite from a hung one — and this lane opened expecting r56f's named-numeric shift fix to be the root (a prime search over the converted `math/big` is precisely the shape that defect corrupted) and it was **not**: DSA reaches its slowness honestly, every verdict matching Go. |

### Two things the next lane should not have to rediscover

1. **A README validation badge can only be refreshed by a `-stdlib` run.** The badge emitter is
   gated on `options.convertStdLib`, so a plain single-package conversion does not write `README.md`
   **at all** — and worse, it regenerates the `.csproj` WITHOUT the validation-pack block (the
   `Exists`-guarded `VALIDATION.md` pack input), an 8-line silent removal that reads as nothing in
   `git status` until you diff it. A `-tests` run does not write the README either. The correct
   instrument for a rebank is `go2cs -stdlib <pkg…> -comments -go2cspath <src>`; it also re-copies
   the six root attribution files (`core/LICENSE`, `core/VERSION`, …) as pure CRLF phantoms, which
   are restored, not banked.
2. **The badge needs BOTH signals present on disk before that run.** Green requires the committed
   `<pkg>.tests.csproj` *and* the proof page. The proof page is written at the END of a successful
   `compare`, so the ordering is: run the pipeline, THEN the `-stdlib` regen, THEN commit. Running
   them the other way around produces an orange badge on a validated package and no error anywhere.

### The gate found one pre-existing staleness — `time`'s implicit-conversion record

The 114/114 sweep reported exactly one CONTENT drift outside the documented 20-file `-tests`-closure
family: `src/core/time/package_info_internal_test.cs`, one line —

```
-[assembly: GoImplicitConv<RuleKind, global::go.time_package.ruleKind>(… ValueType = "global::go.time_package.ruleKind")]
+[assembly: GoImplicitConv<RuleKind, global::go.time_package.ruleKind>(… ValueType = "nint")]
```

Banked at `34f593bf3` (`time` #73) and stale since some later emission change narrowed `ValueType` to
the UNDERLYING representation. **Not attributable to the lane that found it** — r56a touched no
converter source at all (`git diff <base> -- src/go2cs` empty, working tree clean there), and
`unsafe.Sizeof` is a run-time golib method the converter process does not even link. **Restored, not
rebanked**, per the standing doctrine; it belongs to the next deliberate test-source refresh. Worth
recording because it is precisely what the sweep exists to see: CNR covers behavioral projects and
the reconvert-diff covers production `.cs`, and neither of them can see banked *test* emission going
stale.

## r56g — dwarf's "missing witness" was a missing METHOD; three defects, one family (2026-08-09)

This board left `debug/dwarf` at **30 of 40** with all ten residual rows on one panic and one
attribution: *"No witness is minted for that combination... this is the `go2cs-gen`
`ImplementGenerator` family."* The family was right and the noun was wrong, in a way worth recording
because it will recur: **no witness CAN be minted for that combination.** An anonymous interface
asserted from a value held as a *different* named interface is exactly the shape the compile-time
recorders are blind to by construction — `convTypeAssertExpr` records nothing there deliberately,
and says so — which is precisely why golib carries a run-time tier. The tier was present, correct,
and answering MISS, because the method it was asked about **had never been emitted**.

**Roster 117 -> 118 of 215 (54.4% -> 54.9%), 13,659 -> 13,699 matching verdicts, 50 disclosed
(unchanged).** Lane-local arithmetic; totals recomputed by summing the table, whose pre-bank sum
reproduces the committed header exactly.

### Root 1 — an exportedness gate on a Go method set

`TypeGenerator` promoted a value embed's **box-receiver (pointer-receiver) primaries** only when the
embedded type was UNEXPORTED. Go has no such rule: the method set of `*S` contains every
pointer-receiver method of a value-embedded `E`, because `&s.E` is addressable, whatever `E`'s case.

The gate read as a *scoping* decision, and as one it was defensible — it arrived with the
cross-package-reachability shim (`testing.T.Errorf`, whose `Ꮡcommon` accessor is `internal`), and for
an EXPORTED embed the accessor is public, so the converter's own call sites descend inline and need
no shim. But the converter's call sites are not the only reader. **golib reconstructs a Go method set
at RUN TIME by scanning the emitted extension methods** (`GetGoMethodSetCandidates`, shared by
`StructurallyImplements` and `AdapterBinder`'s shell binder). An un-emitted promotion is therefore not
a missing convenience but an **ABSENT Go method**, and the type silently stops satisfying interfaces
Go says it satisfies.

**The transferable lesson: an emission gate that appears to control only "which callers can see this"
stops being a scoping decision the moment something reads the emission as a FACT.** The method-set
reconstruction is such a reader, and it fails silently — MISS, never a diagnostic. Any future
narrowing of what gets emitted should be checked against that reader specifically.

### Root 2 — a named field the adapter mistook for an embedded interface

With `Basic()` restored, dwarf reached 37 of 40, and the remaining three exposed something worse than
a miss. `ImplementGenerator` detects an embedded INTERFACE field by NAME — field name equals its
interface type's simple name, modulo the `Δ` marker — and that test cannot distinguish a Go embedded
interface from an ordinary named field whose name equals its type's. Both emit the same C# field.
dwarf carries both shapes in ONE struct:

```go
type PtrType struct {
	CommonType        // a real embed — promotes Common()
	Type       Type   // an ordinary field — promotes nothing
}
```

`Common()` was forwarded through the FIELD, returning the **referenced** type's `CommonType` rather
than the receiver's own — a silent wrong answer whenever `Type` was non-nil, and a null dereference
when it was not. Five dwarf structs carry that field shape.

Resolved by **precedence**, since no new signal exists (the two emissions are identical by
construction): marker-backed **depth-1** value-embed promotion — `public partial ref CommonType
CommonType { get; }`, a hard converter marker — now resolves ahead of the name heuristic. Legal Go
guarantees the two can never both be right at depth 1, because promoting one member from two depth-1
embeds is an ambiguity the Go compiler REJECTS; so a struct where both arms answer is a struct whose
"interface embed" is really a plain field. Deeper levels stay below the interface arm, matching Go's
shallower-wins rule. Implemented as two passes of the existing descent (`maxDepth` 1, then 4) so the
"what can bind at this hop" logic is not duplicated and cannot drift from itself.

### Root 3 — the shim was emitted, and emitted unreachable

Widening root 1 paid a second package immediately and exposed a third defect doing it. `archive/zip`
was recorded here as build-blocked on *"the generated `ReadCloser`->`fs.FS` witness binds `Open`
against a `ж<Reader>` receiver while holding a value `ReadCloser`"*. With root 1 fixed the promoted
`Open(this ж<ReadCloser>)` shim existed — and was emitted **`internal`**, so the test assembly still
could not bind it.

The scope came from the name heuristic, which reduces a return type to its last dotted segment. For a
Go MULTI-RETURN that segment is `error)` — lowercase — so **every tuple-returning promoted method**
read as unexported. The accurate test (`ReturnTypeIsPublic`, via `IsEffectivelyPublicType`, which
walks tuple elements) already existed but was keyed to the unexported-embed case alone. It now also
covers the value-embed box shim, which is the *stronger* case for it: that shim exists **to** be
reachable across assemblies, since it performs a descent the caller cannot spell, so emitting it
internal defeats its own purpose. Every other promotion keeps the conservative heuristic.

`archive/zip` went from **build-blocked (99 errors)** to **running at 95 of 98** on that one change.

### `archive/zip` — 95 of 98, and the residual is the SLOW class, not a defect

> **SUPERSEDED 2026-08-09 (r57c-zipperf) — the package BANKS at 98 of 98.** Everything measured
> below stands; the closing paragraph offered two routes and the *second* one was taken. The
> "throughput" was an ASYMPTOTE: `@string` held a bare `byte[]`, so `s[i:]` copied where Go's
> string header slices in O(1). See *r57c* at the end of this file.

The three residual rows are `TestZip64LargeDirectory` and its two subtests, and they are not
mismatches: the C# verdict is **empty**, with `{"action":"timeout","elapsed":900}` and all three still
in `run` state. That is the signature `run-validated-sweep.ps1`'s own `$longTimeouts` comment
describes — *"a timeout with every test up to the cut PASSING, which reads as a failure"* — and it now
has a third member beside `hash/maphash` and `index/suffixarray`.

Measured: **Go 13.2 s** (`go test -run '^TestZip64LargeDirectory$'`). The C# side did not complete
under a 15 m deadline, nor under 45 m. The test builds a central directory of `uint32max-1` and then
`uint32max` BYTES out of ~128 KB records (a 65,535-rune name plus a comment per record) — roughly 4 GiB
pushed through the converted writer twice, so it is throughput, not an algorithmic divergence: every
other assertion in the package matches, including the zip64 boundary logic these same tests check at
smaller sizes.

**Not a disclosure.** The roster admits only `alloc-profile` and `codegen-liveness` — assertions the
CLR *provably cannot* satisfy — and "too slow" is neither; the same call the board already made for
`crypto/dsa`. So `archive/zip` does NOT bank here, and is left with its blocker rewritten rather than
cleared: it is now a **performance** row, not a build row. Banking it needs either a measured deadline
(the `index/suffixarray` route — add `'archive/zip' = '<N>m'` to `$longTimeouts` once someone measures
where it actually lands) or the string/slice throughput work that would make the measurement moot. A
lane picking it up should start by timing the C# host solo with no deadline rather than re-rooting
anything.

### r56f-ecroots — the two EC roots, and a shift-count defect that was a HANG in `math/big`

**Banked: `crypto/ecdh` 47/47, `crypto/ecdsa` 82/82, no disclosures.** Roster 113 → **115** of 215
(52.6 % → **53.5 %**), 13,642 → **13,771** matching verdicts, 50 disclosed (unchanged). Four roots,
each isolated to a standalone Go program converted and run against `go run` before anything moved.

**1. An INITIALIZED var never lifted its explicit anonymous declared type.** `visitValueSpec` lifts
an anonymous struct/interface DECLARED type — but only on the bodyless arm. `var _ interface{
Equal(x crypto.PublicKey) bool } = &ecdh.PublicKey{}`, the documented-interface witness idiom Go's
own suites open with, emitted the raw Go text into both the declaration type and the adapter class
name: 40 diagnostics from one construct. The lift is named from the **Go** identifier, not
`csIDName` — a blank `_`'s C# name is a synthesized temp in no Go scope, so a lift named from it
takes the field's own name (CS0102).

**2. The same-assembly pointer-adapter arm composed onto a whole-type alias.** A collision-renamed
type resolves through `global using ecdhꓸPublicKey = …ΔPublicKey`, a single identifier; gluing the
adapter infix onto it names nothing (CS0246). The FOREIGN arm has carried the rebuild since
`imageꓸRGBA`; the same-assembly arm never got it. ecdh shows both halves side by side — `PrivateKey`
is not renamed, rendered `ecdh.PrivateKey`, and composed correctly all along.

**3. `reflect.StructField.Tag` had NEVER been read — corpus-wide, and silent.** The converter emits
`[GoTag(…)]` at every tagged field and nothing consumed it, so every converted struct reported as
UNTAGGED and every tag-driven decoder saw a tagless type. Surfaced as `crypto/x509` marshalling an
`optional` nil OID instead of omitting it ("asn1: structure error: invalid object identifier"),
which points nowhere near reflection. Behind it, `reflect.Copy` was still the auto two-header
`typedslicecopy` and NRE'd on the bridge's empty `ptr` slot. Both now bridged; `Offset`/`PkgPath`/
`Anonymous` deliberately left unpopulated.

**4. The one to carry forward — `TestINDCCA/P256/Generic` was a HANG, not a performance gap.** This
board recorded it as a 20-minute timeout with the question open. It is an infinite loop, and the
fixed path runs in **0.31 s** against Go's 0.66 s, so slowness was never the answer.

Go's shift count is unbounded; C# MASKS it. golib's `GoShift` guards exist for exactly this and the
converter applies them whenever it cannot prove a count in range — **but only for an UNNAMED basic
operand.** A NAMED numeric type resolves through the go2cs-gen wrapper operator instead, which did
the native masked shift, so that entire family kept the wrong answer. `math/big`'s `lehmerSimulate`
reads `a2 = B.abs[n-2] >> (_W - h)` on `Word`; for a normalized operand `h == 0`, so the count is
exactly 64. Go yields 0, C# yielded the word. The corrupted Lehmer cosequences make `GCD`'s
`for len(B.abs) > 1` loop stop converging — an infinite loop inside `math/big`, reached from
`crypto/elliptic`'s generic `CurveParams` path, so `elliptic.P256().Params().Double(Gx, Gy)` never
returned. It is value-dependent, which is why it hid: a garbage `a1`/`a2` that fails Collins'
stopping condition immediately costs only a Euclidean step, so equal-width pairs pass and only pairs
that make the condition iterate corrupt anything.

The guard now lives in `NumericTypeTemplate`'s `operator <<`/`>>`. That is a **corpus-wide runtime
semantics change**, so it was gated operationally, not just by compile: the full validated sweep is
**115/115, 13,771 verdicts, 0 failures**. Worth re-reading the board's own `math/big` 9-of-226 and
`crypto/elliptic` 4-of-82 censuses against it — both were measured with the masked shift in place.

**Escalation — pre-existing drift, not this lane's.** `src/core/time/package_info_internal_test.cs`
flips on every sweep: `GoImplicitConv<RuleKind, …ruleKind>(ValueType = …)` moves from
`"global::go.time_package.ruleKind"` to `"nint"`. The `nint` form is the correct one (the VALUE type
of `type ruleKind int`), so a converter fix landed after `time` was banked and its committed metadata
went stale. Confirmed NOT this branch's by building the converter at the merge base `363e728bb` and
re-running `time`'s `-tests` conversion: the base reproduces the identical flip. It needs a
re-bank of that one file by whoever owns the fix, not a restore in perpetuity.

### r57a-bignum — the post-fix re-measure: what two corpus-wide fixes were actually worth (2026-08-09)

**Banked: `crypto/dsa` 4/4, `crypto/elliptic` 82/82, no disclosures.** Roster 121 → **123** of 215
(56.3 % → **57.2 %**), 13,890 → **13,976** matching verdicts, 50 disclosed (unchanged). **No converter
change was made in this lane** — every delta below is a census that had gone stale against fixes
already on master, which is the finding.

| Package | Board's census | Re-measured | Outcome |
|:--|:--|:--|:--|
| `crypto/dsa` | 0 of 4, "no `-test-timeout` is enough" | **4 of 4** | **BANKED** — deadline was ~90 s short |
| `crypto/rsa` | 0 of 592, cctor panic | **559 of 560** | one row away: AllocsPerRun |
| `math/big` | 9 of 226 | **222 of 226** | 3 roots, 2 of them the alloc model |
| `crypto/elliptic` | 4 of 82 | **82 of 82** | **BANKED** — no work needed |

**The headline: a census taken under a live corpus-wide defect measures the defect, not the package.**
Three of these four rows moved without a line of code being written. r56f's named-numeric shift fix
alone carried `crypto/elliptic` from 4 to 82 and `math/big` from 9 to 222; r56f's
`reflect.StructField.Tag` bridge carried `crypto/rsa` from a static-initializer panic to 559 of 560.
Both fixes were landed and written up correctly — what was missing was the re-read, and the board
had explicitly asked for it. **Every census row on this board should be treated as timestamped
against the corpus it was taken on**, and a lane that inherits one is cheaper re-measuring it than
reasoning from it. The r44a lesson ("the yield is in what NOBODY HAS RUN") has a sibling: the yield
is also in what nobody has re-run since the thing that was blocking it got fixed.

**`crypto/rsa` was the campaign's largest single-row prize — CLOSED r58a.** 560 verdicts held by
`TestAllocations`, which was the AllocsPerRun-reports-BYTES shim measuring 28,513,920 B/run where Go
reports a malloc count. What unblocked it was not a disclosure ruling but an INSTRUMENT: golib now
keeps its own allocation counter (`AllocationCounter`), the structural mirror of what Go's
`runtime.MemStats.Mallocs` already is — a counter the runtime keeps at its own sites, not a platform
facility. Census, coverage boundary and overhead:
[`DESIGN-allocation-counting.md`](DESIGN-allocation-counting.md).

### r58a — the AllocsPerRun class, re-measured as a COUNT

Every row below is measured through the counter with the `@string` census taken (the gap that
document's §5 item 3 deferred to r57c is closed). The count is a **lower bound** — the C# compiler
emits closures, `params` arrays and interface boxing in CONVERTED code that golib never sees — so
each row is reported with that residual named, not laundered into a verdict.

| Row | Go's budget | Reported BEFORE (bytes) | Reported NOW (objects) | Outcome |
|:--|--:|--:|--:|:--|
| `crypto/rsa` `TestAllocations` | < 10 | 28,513,920 | **340,756** | **BANKED** — `alloc-profile`, five orders clear |
| `math/big` `TestNewIntAllocs` | 0 | 816 | **1** | not disclosable — see below |
| `log` `TestDiscard` | ≤ 1 | 424 | **4** | ruling; and `log` has a SECOND root |
| `net/http/internal` `TestChunkReaderAllocs` | 1 | 640 | **2** | ruling |
| `log/slog/internal/buffer` `TestAlloc` | 0 | 304 | **counter saw none** | still bytes — not decision-grade |

**The instrument did its job most visibly on `math/big`.** `TestNewIntAllocs` reported *"wanted 0
allocations, got 816"* — a figure no reader could act on, because 816 was bytes. It now reports
*"wanted 0 allocations, got 1"*, seven times, one per operand shape. That is not a disclosure
candidate under ruling #1 (a want-zero assert is satisfiable in principle) — it is a tractable
engineering target that was previously invisible: **one** golib object per `x.Add(x, NewInt(n))`.
Whoever takes it next knows exactly what to hunt. (`math/big` re-measures **224 of 226**; the other
miss, `TestMulUnbalanced`, is a memory-VOLUME assert, not an allocation-count one.)

**`log/slog/internal/buffer` is the honest negative.** The counter charged NONE of its 304 B/run, so
`AllocsPerRun` correctly fell back to the byte figure rather than reporting a zero it could not
vouch for — the false-pass arm working exactly as designed. Every object on that path is
compiler-emitted or BCL-internal, the structural class (§5 item 1) no golib census can reach. It
stays blocked, and now for a MEASURED reason rather than an assumed one.

**`log` was never one row away, and this re-measure confirms the earlier reading rather than adding
to it.** `TestAll` still fails on the `runtime.Caller` file-name capability already characterized
above as an architectural arc: Go asserts `^.*/[A-Za-z0-9_\-]+\.go:(63|65): hello 23 world$` and the
converted host emits the absolute path of the `.cs` file (`D:\…\src\core\log\log_test.cs:69`). So
even a favourable ruling on `TestDiscard` banks nothing here — 7 of 9 — which is the same call r43f
made and the reason `log` stays off the roster. What the counter adds is the alloc row's real
number: **4 objects/run against a budget of 1**, where the shim used to say 424.

**`crypto/internal/nistec` re-measured: still 2,195 of 2,200, and the count CORROBORATES r56d.**
*(Superseded 2026-08-13 — the table below is the PRE-lowering state; the ж-box A3 section at the
end of this board carries the post-A2 pinned measure: P256 **8,528**/run, acceptance met.)*
The five `TestAllocations` rows now report objects instead of bytes, against Go's budget of **0**:

| Curve | objects/run | bytes/run |
|:--|--:|--:|
| P224 | 264,540 | 23,625,160 |
| P256 | **242,665** | 21,964,357 |
| P384 | 471,424 | 40,755,611 |
| P521 | 870,534 | 72,244,419 |

P256's **242,665** lands within 0.7 % of the **241,077** r56d derived through a temporary hand-built
probe, which is an independent corroboration of that decomposition by a different instrument — and
the byte column reproduces r56d's 21,964,011 to four significant figures. **It still does not bank,
and the reason is unchanged**: ruling #1 holds that a want-ZERO assert is satisfiable in principle,
so it is not a disclosure, and the counter does not alter that — it only replaces a modelled number
with a measured one. The 2,200 verdicts remain gated on the `ж<T>` box arc, whose value this
measurement re-confirms rather than revises.

**`io` retires a disclosure — the counter satisfied the assert instead of excusing it.**
`TestMultiWriter_WriteStringSingleAlloc` asserts EXACTLY ONE malloc. The byte shim measured 406–407
and was disclosed `alloc-count-semantics`, which was the honest call while nobody had measured the
number the assert was about. The counter measures it: **1,024 objects over 1,000 runs = 1 per run**,
against a want of 1. It PASSES — the first want-exactly-one assert in the corpus the managed runtime
has ever satisfied — so the disclosure was DELETED rather than left dormant, a dormant one being a
signature-pinned licence to ignore that exact failure if it ever returns. `io` moves to **60 matched
+ 1 disclosed** (`TestPipeAllocations` remains a genuine divergence). This is the shape to look for
elsewhere in the class: not every disclosed alloc row is permanent.

⚠ **A roster verdict COUNT can be host-dependent, and `path/filepath` is the first proven case.**
The targeted sweep reported `COUNT path/filepath 67, banked 61` — not a regression and not an
improvement in the corpus, but six symlink tests (`TestEvalSymlinks*`, `TestGlobSymlink`, …) that
**Go itself skips** without symlink-creation privilege. On the machine that banked the row both
runtimes skipped 20 identically; on the current coordinator box both PASS 16 of them identically.
Either way the two sides AGREE, so the package is equally valid on both hosts — only the count
differs. The row is deliberately LEFT at 61 rather than raised: banking 67 would false-red every
sweep on a host without the privilege, which is the larger population. Worth a general remedy
(record privilege-gated skips as such, or normalize the count over identically-skipped tests)
before the next roster-wide arithmetic pass — flagged, not fixed here.

**`encoding/asn1` re-measures 35 of 38** (was 34 at r57b): r58b's typed-nil packing closed
`TestMarshalError` exactly as predicted. The three that remain are already characterized above —
`TestCertificate` (sequence tag mismatch), `TestMarshal` #37 (SET emitted where a SEQUENCE tag is
wanted) and `TestUnexportedStructField` (a `reflect.setKinded` panic on a value obtained through an
unexported field). None is an allocation row.

**One new root, characterized and escalated rather than half-fixed: Go's TYPED-NIL interface does not
survive the conversion.** Detail in the `math/big` section above. A nil `*Int` in a slice reaches an
interface as a plain `null` instead of a non-nil interface carrying `(type=*Int, value=nil)`, so
`.(GobEncoder)` takes the wrong arm where Go succeeds and dispatches on the nil receiver that
`big.Int.GobEncode` explicitly handles. Corpus-wide in scope, chip-class in cost — it changes what
`== nil` means for every converted interface — and golib can already *represent* the state
(`IsNilStandardPointer`), so the narrow starting point is the reflection bridge's `Value.Interface()`,
which knows the static type at the moment the box is made. Blocks 2 of `math/big`'s 4 and part of
`encoding/gob`'s 99 of 106.

> **r58b (2026-08-09) closed the reflection half and A/B'd the rest of that claim, which was wrong.**
> `Value.Interface()` now packs the typed nil, and it pays `math/big` exactly (222 → 224 of 226) —
> but `encoding/gob` measures **99 of 106 with AND without the fix, the same seven divergent rows**.
> The root has two halves paying two different packages: the reflection READ path (closed; gob
> reaches *math/big's* types through `reflect`, which is why math/big's rows moved) and the EMISSION
> path — a nil pointer VARIABLE boxed into an interface by ordinary converted code, which is what
> gob's own `TestNilPointerInsideInterface` and the `mustPanic` family need, and which remains
> chip-class / design-with-user. gob's current seven: `TestBadData`, `TestEndToEnd`,
> `TestIgnoreDepthLimit` (infrastructure-error), `TestIgnoreRecursiveType`,
> `TestIndirectSliceMapArray`, `TestNilPointerInsideInterface`, `TestSingletons`.

~~**`reflect.Value.MapIndex` is still the raw converted Go body — a bridge gap, found in passing
(r58b, 2026-08-09).**~~ **CLOSED before it merged: r57b bridged `Value.MapKeys` and `Value.MapIndex`
in its go/ast arc (`bfdb073be`), landing on master while r58b was still on its branch** — two lanes
found the same gap independently, one recorded it and the other fixed it. The claim below is kept
struck rather than deleted because its shape analysis was right (the `MapRange` iterator's
`iter.mapValueType` → `makeTypedValue` machinery is exactly what the fix used): unlike
`MapRange`/`SetMapIndex`, `MapIndex` read `v.ptr` as flat memory and called `mapaccess`, so it
faulted on any Value the managed bridge produced; `internal/fmtsort` was its first roster consumer
and re-validated 3/3 in r57b's recovered sweep.

**`crypto/dsa` — the negative result, recorded so it is not re-derived.** This lane opened expecting
the shift fix to be dsa's root too; a probabilistic prime search over the converted `math/big` is
exactly the shape that defect corrupted. It is not. `TestParameterGeneration` passes in **1,156.8 s**
having always been slow-but-correct, and the board's "no `-test-timeout` is enough" was a conclusion
no timeout can support — only a *completed* run distinguishes a slow suite from a hung one. It is now
the third `$longTimeouts` entry at 30 m, beside `hash/maphash` and `index/suffixarray`.

**Two rows the next lane should re-measure immediately, for the same reason:** `encoding/asn1`
(28 of 38) and `net/smtp` (9 of 14). Both were attributed to the same reflection-driven DER walk that
the `StructField.Tag` bridge just repaired for `crypto/rsa`, and neither has been run since.

## r57b — the near-miss singles, re-measured: five rows were already banked (2026-08-09)

A breadth pass over the board's smallest-gap rows, run under the r44a doctrine: measure cheaply,
bank what clears, characterize what does not. Its most useful product is not the one bank — it is
that **the ONE ROW AWAY table above is substantially stale**, and a lane that trusts it spends its
budget re-deriving closed rows.

### The stale table — verify before you plan

Five of the eighteen entries no longer exist. `internal/profile` (roster line 117),
`runtime/internal/math` (143), `net/http/fcgi` (133) and `syscall` are **banked**, and `internal/cpu`
is already struck through. Each was re-measured this pass and each returned a clean
`Validated N tests` — `net/http/fcgi` at 12/12, whose recorded `TestGetValues` byte-stream mismatch
is gone. Treat every row below as a HYPOTHESIS to re-measure, never as a work item to start from;
the roster table in `docs/ValidatedTestPackages.md` is the authority and the board is a lagging
index of it.

### `go/ast` — BANKED 9/9, and the row had two roots stacked

The recorded root (`ast.Fprint` -> `reflect.MapKeys` -> `mapType.get_MapType()`) was right about the
family and hid a second defect behind it. Both are closed in this arc's bank commit; the short form
is that **the map READ pair was never bridged** — `MapRange`/`MapIter.*`/`SetMapIndex` all live in
the bridge, `Value.MapKeys` and `Value.MapIndex` never joined them — and that with the panic gone,
an **unnamed struct reported its LIFT's C# name** (`ast_internal_test.typeᴛ1`) where Go renders it
structurally (`struct { X int; y int }`). The naming arm is corpus-wide and visible immediately:
`internal/platform`'s failure text moved from `[]platform_test.listEntry` to
`[]struct { GOOS string; GOARCH string; ... }` in the same pass. Its row does NOT close — the
residual is `encoding/json` refusing to unmarshal an array into a slice whose element is a lifted
struct, which is a Kind question about the lift, not a naming one.

### `net/smtp` — the recorded root is CLOSED; what is behind it is the Windows-socket class

The board's `loadcert: tls: failed to parse private key` is **gone**: the PEM/ASN.1 private-key
parse now succeeds, which retires the shared attribution with `crypto/rsa`'s cctor panic for this
package (that package is r57a's and is not re-measured here). All five rows now fail on ONE panic,
and it is not a TLS defect at all:

```
panic: runtime error: index out of range [0] with length 0
  at go.array`1.get_Item ... golib\array.cs:280
  at go.syscall_package.sockaddr(ж`1 Ꮡsa) ... syscall\windows\syscall_windows.cs:881
  at go.syscall_package.Bind(ΔHandle fd, ΔSockaddr sa)
  at net.listenStream -> socket -> internetSocket -> listenTCP -> net.Listen
```

`(*SockaddrInet4).sockaddr` does `p := (*[2]byte)(unsafe.Pointer(&sa.raw.Port))` to write the port
in network byte order. The emitted form is
`var p = (ж<array<byte>>)(uintptr)(new @unsafe.Pointer(Ꮡsa.of(...ᏑPort)))`, and `ж<array<byte>>`
over a raw address materializes `default(array<byte>)` — a LENGTH-ZERO array — so `p[0]` panics.
`array<T>` is a managed container, not two inline bytes, so no address reinterpret can produce one.

**This is `net.Listen` on Windows, so it is not one package's row.** `net/http/cgi` hits the
identical stack through `httptest.NewServer` -> `newLocalListener`, and every package that listens
on a TCP socket will. Note also that fixing the reinterpret alone is not enough: `Bind` then hands
the kernel `unsafe.Pointer(&sa.raw)`, and `RawSockaddrInet4`'s `Addr [4]byte` / `Zero [8]uint8` are
managed references — which is precisely the **open syscall STRUCT-PASSING seam** already censused
above, whose remedy is the established blittable mirror (`GetTimeZoneInformation`,
`findFirstFile1`/`findNextFile1`). The board predicted `net` would be the package that forces it.
It has.

> **RESOLVED 2026-08-11 (lane L10) — and the row does NOT close, because a second wall sits behind
> it.** Both defects above are fixed: the port alias and the struct-passing seam are hand-owned in
> `syscall/windows/syscall_windows_impl.cs`, `syscall.Bind` now succeeds, and the whole
> encode → kernel → decode round trip is proven value-for-value against `go run` by the new
> `SockaddrRoundTrip` behavioral test on both IPv4 and IPv6. **But "unblocks the whole net cluster"
> was wrong, and the correction is worth more than the fix.** With bind working, `net.Listen` walks
> on and stops here instead:
>
> ```
> System.NotImplementedException: runtime_pollServerInit: external (assembly or cgo) function is not implemented
>   at internal/poll.runtime_pollServerInit  (PartialStubGenerator stub)
>   at internal/poll.pollDesc.init -> internal/poll.FD.Init  ... fd_poll_runtime.cs:48
>   at net.netFD.init -> net.listenStream                    ... sock_posix.cs:216
>   at net.Listen                                            ... dial.cs:933
> ```
>
> `internal/poll` declares **ten** bodyless `//go:linkname` netpoll entry points
> (`runtime_pollServerInit`, `pollOpen`, `pollClose`, `pollWait`, `pollWaitCanceled`, `pollReset`,
> `pollSetDeadline`, `pollUnblock`, `runtime_isPollServerDescriptor`, `runtimeNano`), every one of
> them an unimplemented stub. The counterparts EXIST in the converted runtime —
> `runtime/netpoll.cs:217` carries `poll_runtime_pollServerInit` with its `//go:linkname` comment
> intact — but nothing wires a linkname across assemblies. And wiring it would not be sufficient:
> that body reaches `netpollGenericInit` → `netpollinit`, which on Windows is
> `stdcall2(_CreateIoCompletionPort, …)` and bottoms out in `asmstdcall`, itself a
> `PartialStubGenerator` stub. So this is an independent seam, one layer deeper than syscall, and
> the honest remedy is the managed-API-boundary pattern already used for `sync`'s Mutex and
> `runtime`'s traceback surface: hand-own the ten `runtime_poll*` CONTRACTS against .NET's own
> completion-port machinery rather than emulating Go's poller. That is a design arc with a
> deadline/unblock story to settle, not a wrapper repair — it wants its own DESIGN doc and a
> coordinator ruling before anyone starts.
>
> **Consequence for the board's socket-walled rows.** `net/smtp`, `net/http/cgi`, `net/http/httptest`,
> `net/http/httputil`, `net/http/cookiejar` and `net/rpc` stay walled, and their censuses remain
> walled-not-stale — but the wall they are behind is now the NETPOLL one, not the sockaddr one.
> Re-measuring any of them before that arc lands will still report the same counts; the L9 item-4
> canary reading is therefore already answered here and need not be spent.

### The `array<T>` unshaped-instance class has a sharper root than "producer (N)"

`html`'s row is a **map MISS**. Go's `if x := entity2[string(entityName)]; x[0] != 0` reads the ZERO
VALUE of `[2]rune` on a miss and indexes it legally; golib's indexer returns `default(array<rune>)`,
length zero, and `x[0]` panics. `debug/pe` is the same class at a different site — its
`_ [3]uint8` padding field prints `[0 0 0 0 0 0 0 0]` against Go's `[0 0 0]`, so there the shape is
wrong rather than absent. The class is therefore **"an `array<T>` zero value produced without its Go
length"**, with several distinct producer SITES, of which the map-miss is one.

golib already carries the contract (`IGoZeroShaped` / `builtin.GoZero<T>`), but it recovers shape
from a TEMPLATE, and a map miss has none. The natural general fix is the idiom the converter already
emits for slices — `new slice<ΔValue>(mlen, () => new(nil))` — extended to a map's miss value, since
the declared value type's Go shape is statically known at the construction site. That is a converter
+ golib arc with corpus-wide map-construction emission impact, not a near-miss single.

### The converted-host WORKING-DIRECTORY class — why no cheap subset exists

`go/build` re-measures unchanged at 57 of 58 (`TestLocalDirectory`: `ImportPath="."`), and
`internal/testenv` at 3 of 4, now with its exact mechanism: the host's working directory is
`<temp>/go2cs-tests/<flat pkg>/<guid>/<last segment>` (`TestHost.CreateRunDirectory`), so
`../../../bin/go.exe` resolves to `go2cs-tests/internal_testenv/bin/go.exe`.

All four members of the class (`internal/godebugs`, `io/ioutil`, `go/build`, `internal/testenv`)
want the SAME thing: `CWD == $GOROOT/src/<pkg>`, which is the working directory `go test`
guarantees. Reproducing it is honest — it is the harness's job to reproduce `go test`'s execution
environment, and CWD is part of that environment exactly as GOROOT and the env are.

**But there is no cheap subset, and the reason is worth recording.** Deepening the run directory to
`<runRoot>/src/<full import path>` costs nothing and fixes the SHAPE — and closes none of the four,
because every one of them needs CONTENT at the reconstructed ancestor: `bin/go.exe` for testenv,
`doc/godebug.md` for godebugs, the package's own `.go` sources for `go/build`'s `ImportDir`, the
sibling package's sources for `io/ioutil`. So the remedy really is the full synthetic-GOROOT
staging the board suspected, it changes the execution contract for all 122 banked packages at once,
and it interacts with the staging path that feeds the input-digest manifest. Design-with-user, not a
breadth lane's — and NOT a disclosure, for the reason already recorded: it is satisfiable at a layer
go2cs owns.

> **RESOLVED 2026-08-13 (lane `claude/synthetic-goroot-class`) — four of the six bank, and the
> class was TWO roots, not one.** Design: [`DESIGN-package-ancestry-view.md`](DESIGN-package-ancestry-view.md).
> The remedy is an ANCESTRY view, not a synthetic GOROOT, and the difference was measured rather
> than argued. `PackageAncestry` stages GOROOT's content from its top level down to the package —
> sibling directories as links, files as hard links, the path to the package materialized, the
> package's own directory real copies — under a working directory that gains the `src` level this
> section correctly predicted it needed. GOROOT itself keeps pointing at the real installation.
>
> **Why not the synthetic GOROOT this section expected.** A linked mirror is not walk-equivalent to
> the real tree: Go reports a junction from `Lstat` as an irregular file, so `filepath.WalkDir`
> steps over it rather than descending. Measured against Go 1.23.1 on a mirrored root — a walk
> counting `*.gz` under GOROOT finds **0** where the real tree has **4**, and a walk of
> `src/unicode` reports **1** entry against the real **19**. Two ALREADY-BANKED packages walk GOROOT
> that way (`compress/gzip`'s issue14937, `path/filepath`), so repointing GOROOT would have
> REGRESSED them. Reads through a junction are faithful and every member of this class resolves
> against its working directory, so leaving GOROOT real costs nothing here. The feared "changes the
> execution contract for all 122 banked packages at once" did not materialize: nothing about GOROOT
> resolution moved.
>
> **Banked: `go/parser` 173/173, `io/ioutil` 28/28, `internal/testenv` 7/7, `internal/godebugs` 1/1**
> — +209 verdicts, the arc's ledger paid in full and then some (the deferral price this board
> tracked was 167 + 12).
>
> **Two corrections to this board's own rows, both from reading the sources rather than the verdict
> names** — the same methodology note §*go/parser 6/173* already earned:
> - `go/parser`'s initializer reads **`../printer/nodes.go`**, not `parser.go`. That is the whole
>   design question: its own sources would be served by staging the package directory; a SIBLING
>   package's are not.
> - `internal/godebugs` needs more than `doc/godebug.md`. Past that read, `TestAll` runs
>   `go list -f={{.Dir}} std cmd` and reads every `.go` file it names — a working toolchain, not a
>   staged file.
>
> **The residue is a SECOND root — GOROOT-IDENTITY — and it holds the two that did not bank.**
> Both need the importing/asking file to sit under the GOROOT the toolchain itself uses, which no
> ancestry view can provide and which repointing GOROOT cannot fix either (the child `go` resolves
> its own GOROOT from its executable location — measured: `go list` returns real-GOROOT paths with
> `GOROOT` set to a mirror).
> - **`go/build` 57/58, unchanged and now precisely attributed.** `TestLocalDirectory`'s
>   `ImportDir(cwd)` derives the import path by relating cwd to the GOROOT the process REPORTS.
> - **`internal/coverage/cfile` 4/16 — same count, but a root MOVED, which is why it was
>   re-measured.** The module-resolution facet is **closed**: `TestIssue59563TruncatedCoverPkgAll`
>   no longer dies on `go.mod file not found`, it runs `go test -coverpkg=all` to completion and
>   emits a full profile, failing now on CONTENT (`wanted 1 found, got 0`) — a real coverage
>   question, no longer infrastructure. The internal-import facet stands:
>   `use of internal package internal/coverage/slicewriter not allowed`, because the staged
>   `harness.go` is outside the tree the toolchain resolves `internal/...` within. Read the row as
>   one GOROOT-identity failure plus one content failure, with the nine `TestCoverageApis` subtests
>   still shadows of their parent.
>
> **Two host defects surfaced on the way and are fixed here**, both general and both previously
> masked:
> - **PATH fidelity.** `go test` PREPENDS `$GOROOT/bin` to the test binary's PATH (measured:
>   `PATH[0]` is `$GOROOT/bin`, `exec.LookPath("go")` resolves there). The pipeline now does the
>   same, beside its existing GOROOT export. On a machine carrying two installations of the same Go
>   version this is the difference between pass and fail for `internal/testenv`.
> - **`t.TempDir()` placement.** It sat under the WORKING directory; Go's sits in the system temp,
>   with no `go.mod` above it. The staged ancestry puts `src/go.mod` above the package tree, so it
>   is hoisted to the run root. This unmasked a **false pass**: `go/build`'s
>   `TestImportPackageOutsideModule` wants "go.mod file not found in current directory or any parent
>   directory" and was getting it only because the old sandbox had no `go.mod` ANYWHERE.
>
> **And one wall behind the wall.** With its initializer working, `go/parser` ran and died on an
> uncatchable `Stack overflow.` — `TestParseDepthLimit` drives Go's own `maxNestLev` of 100,001
> levels deliberately, ~400k converted frames, which the host's 256 MB per-test thread served only
> if every frame fit in 671 bytes. Raised to Go's own 1 GB ceiling (reservation is address space;
> pages commit on demand). Any lane hitting a deep-recursion wall elsewhere should suspect this
> constant before suspecting the conversion.

`net/textproto` also re-measures unchanged at 25 of 26 — still the want-ZERO
`canonicalMIMEHeaderKey allocs = 816` against the AllocsPerRun-reports-BYTES shim, still not a
disclosure candidate under ruling #1.

### Escalation — `InterfaceInheritance` fails on master, and it is not this lane's

The full behavioral suite gating this arc came back **554/554 transpile + compile + target, 527 of
528 output**, with one failure: `InterfaceInheritance`, `map[:2 :1]` against Go's `map[:1 :2]`.

Confirmed **pre-existing**, by restoring `src/core/golib` and `src/core/reflect` to the merge base
(`7c7bc7d69`) and re-running the project filtered — it fails identically there. It is also
deterministic, not flaky: six consecutive runs give byte-identical output. The mechanism is
`internal/fmtsort.compare`'s Interface arm, which orders two keys of differing dynamic type by
comparing their type descriptors as VALUES — in Go a `Kind Pointer` compare of descriptor
ADDRESSES, which the linker assigns in declaration order. go2cs's canonical interned
`reflect.Type` has no such ordering, so the pair sorts by whatever box identity gives. Worth
deciding deliberately rather than patching: this is an ordering Go's own documentation treats as an
implementation detail, so the guard may be asserting something go2cs can only match by luck.

### `encoding/asn1` — the fourth charter row, re-measured: 34 of 38, and the tag root DID close

The board carried `encoding/asn1` at 28 of 38 with the standing hypothesis that it shared
`crypto/rsa`'s DER/tag root. Re-measured on this branch it is **34 of 38**: six rows closed on
their own, which is the hypothesis confirmed — the repaired tag handling reached here too. It is
still not bankable, and the converted test artifacts were deliberately NOT committed, per the
policy that test sources bank only when a suite validates.

What the hypothesis got WRONG is the shape of the remainder. The four survivors are not one root
waiting on one fix; they are four, and three of them belong to areas other lanes already own:

- **`TestMarshalError` — the TYPED-NIL class, and it is r58b's.** `panic: interface conversion:
  interface {} is nil, not *big.Int` inside `makeBody`. Go asserts a **nil `*big.Int`** out of an
  interface and the assertion SUCCEEDS, yielding a typed nil the marshaller then rejects with its
  own error; go2cs's `_<T>` sees an untyped nil and panics instead. That is exactly the state
  `claude/r58b-typednil` is bounded to at `Value.Interface()`. **Re-measure this row first when
  r58b lands** — it is a free second witness for that arc, on a package r58b is not otherwise
  touching.
- **`TestUnexportedStructField` — a reflection-bridge FIELD-FLAG gap, distinct from the map/naming
  pair this lane closed.** Go expects `Unmarshal` to RETURN `structure error: struct contains
  unexported fields`; go2cs returns `<nil>` and then panics in `mustBeAssignable`. So the read-only
  flag is not propagated onto a `Value` reached through an unexported field: `CanSet()` answers
  true where Go answers false, asn1's own guard never fires, and the write runs on to `setKinded`.
  The guard is asn1's, but the defect is `flagRO` propagation in `Value.Field`, so it will surface
  anywhere a package probes settability rather than trusting it.
- **`TestMarshal` #37 — one byte, and it is the tag.** `300302010a` against `310302010a`: `0x30`
  SEQUENCE emitted where Go writes `0x31` SET. The `set` field parameter is not reaching the
  emitted tag in `makeField`. Narrow and self-contained — the likeliest single-row win of the four.
- **`TestCertificate` — nested slice-of-slice-of-struct.** `sequence tag mismatch`, and the RDN
  name comes back EMPTY (`[]` where Go has the full `[[{[2 5 4 6] XX}] …]`). The only one still
  unattributed below the surface message.

> **L6 (2026-08-11) closes the last two of those bullets with ONE fix, and it is neither the
> converter nor `makeField`.** The `set` field parameter reaches the emitted tag correctly —
> `TestMarshalWithParams`, which is the `asn1:"set"` PARAMETER path, passed throughout. `TestMarshal`
> #37 is `testSET([]int{10})`, the TYPE-NAME path: `getUniversalType` selects SET over SEQUENCE on
> `strings.HasSuffix(t.Name(), "SET")` and nothing else. The bridge's `rtype.Name()` gated on
> `GoReflect.ElementType(st) is not null` — a proxy for "unnamed composite" that is equally true of a
> DEFINED container — so every `type S []T`/`[N]T`/`map[K]V`/`chan T`/`*T` in the corpus reported no
> name. `PkgPath()`, reading the same managed nesting, answered `"main"` for the same types, which is
> a pair Go's model cannot produce and is what named the defect. Fixed with `GoReflect.HasGoName`,
> the managed stand-in for the descriptor's `TFlagNamed` bit, mirroring `GoTypeName` arm for arm.
>
> **`TestCertificate` is the SAME root, and is hereby attributed:** its `RDNSequence` is a
> `[]RelativeDistinguishedNameSET`, so the inner elements were emitted as SEQUENCEs and the RDN came
> back empty — the "unattributed below the surface message" bullet needs no separate investigation.
> Measured A/B on one machine, same tree, same GOROOT: **35 of 38 before, 37 of 38 after**, the
> remainder being `TestUnexportedStructField` alone (L7's `flagRO` gap). The board's projected
> "36 of 38 when the tag row closes" was one row low for this reason.
>
> ⚠ **Two follow-ons for whoever plans next.** (1) `abi.Type.HasName()` is still `false` for every
> synthesized descriptor, so `internal/reflectlite.rtype.Name()` — the ordinary converted body, which
> gates on it — answers `""` for EVERY type, strictly worse than what `reflect` had. It is dormant
> (reflectlite's consumers `context` and `errors` use only `String`/`Kind`/`Comparable`/
> `AssignableTo`/`Implements`), so it was recorded rather than fixed: populating the bit also changes
> `directlyAssignable`'s `T.HasName() && V.HasName()` short-circuit, currently over-permissive in
> both packages, which is a corpus-wide assignability change and not a naming one. (2) The measure
> was taken on the laptop's Go **1.23.2** GOROOT against the corpus's pinned 1.23.1; the denominator
> was verified as **38 test functions**, unchanged between the two patch releases, and both sides of
> the comparison read the same sources — so the per-test agreement is sound and only the absolute
> count is developmental. Coordinator re-gates on the pinned machine.

### The final sweep, recovered after the hardware failure (2026-08-10)

This lane was parked mid-sweep when the coordinator machine died, so the verdict was lost with it.
Re-run FILTERED over the lane's own banked and re-measured rows on a replacement box: **8 packages,
137 verdicts, 8 pass / 0 fail** — `go/ast` 9, `syscall` 62, `go/printer` 45, `net/http/fcgi` 12,
`go/format` 4, `internal/fmtsort` 3, `internal/profile` 1, `runtime/internal/math` 1. The last three
of those are the rows this pass struck through as already-banked, so the strikethroughs are now
gate-backed rather than argued. `internal/fmtsort` and `go/printer` were added on purpose beyond the
lane's own list: the bank's real blast radius is the reflection bridge, and `internal/fmtsort` is the
direct consumer of the `MapKeys`/`MapIndex` pair this lane moved into it.

⚠ **The crash-save `wip` commit contained NOTHING that belonged.** All 22 files classified as
standing aftermath and were dropped: nine production `.cs` in the `-tests`-closure restore family
(the `Δio` alias and the root-qualification escape), four `package_init.cs` carrying the
`initᴛᴛtests` hook, three `-text`-marked `compress/testdata` fixtures showing a pure CRLF flip —
and six `log/slog/internal/benchmarks` files that were **100% NUL bytes**. That last group is a new
shape worth naming: NTFS committed each file's SIZE and lost its DATA in the power failure, and the
sizes match the committed content's CRLF-smudged length **exactly**, byte for byte, across all six.
So the package had no real drift at all — a crash-save `git status` can be dirty for reasons that
are neither a converter change nor a documented phantom, and a size-vs-content check separates them.

## r57c — `archive/zip` banks 98/98; the "performance row" was a WRONG ASYMPTOTE in `@string` (2026-08-09)

**Banked: `archive/zip` 98/98, no disclosures.** Roster 121 → **122** of 215 (56.3 % → **56.7 %**),
13,890 → **13,988** matching verdicts, 50 disclosed (unchanged). *Lane-local arithmetic against this
branch's base; the coordinator union-recomputes at merge.*

The board's own `archive/zip` section closed by naming two routes to a bank — "either a measured
deadline (the `index/suffixarray` route) or the string/slice throughput work that would make the
measurement moot" — and advised a lane to "start by timing the C# host solo with no deadline rather
than re-rooting anything". That advice was followed exactly, and it is what found the defect: the
host, timed solo with no deadline, **still had not finished after 45 minutes** against Go's 13.2 s.
A constant-factor throughput gap does not do that. Profiling it (`dotnet-stack`, both worker threads,
every sample) put the entire cost in one frame — `detectUTF8` → `Buffer._Memmove`.

`detectUTF8` is the ordinary Go rune walk, and the emission is a faithful 1:1 rendering of it:

```go
for i := 0; i < len(s); { r, size := utf8.DecodeRuneInString(s[i:]); i += size }
```

The defect was underneath, in the REPRESENTATION. A Go string header is a pointer **plus length**
into shared immutable storage, so `s[i:]` is O(1) and allocates nothing. `@string` held a bare
`byte[]`, so its range indexer had to *materialize* the sub-string: O(n), with an allocation. Over a
65,535-byte file name that makes the loop **accidentally quadratic** — ~2.1 GB copied per call, two
calls per record, 32,768 records. Not slowness; the wrong asymptote. `@string` now carries the
header's real shape (backing array, offset, length) and slices into a window; the backing array is
PRIVATE, so a consumer reading it instead of the window is a compile error rather than a wrong
answer, which is how the last three raw-array readers were found. Detail in the two signed commits
and in `ConversionStrategies-Reference.md`.

`TestZip64LargeDirectory`: **>45 min (never completed) → 20.2 s**, against Go's 11.3 s.

### What this row costs the sweep, and why the deadline entry is still needed

The pipeline builds **Debug**, where the non-inlined golib window accessors cost ~22x, so the banked
suite is minutes rather than seconds and `archive/zip` joins `hash/maphash` and
`index/suffixarray` in `run-validated-sweep.ps1`'s `$longTimeouts` — authored at `'20m'`, **raised
to `'30m'` at merge**: the i7-5820K re-measure below left 20m only ~35 % headroom, and a deadline
is a safety net against a hung run, never a performance assumption. The 391 s figure was measured on the
reference desktop (391 s for the whole suite). **Re-verified on the replacement box** (i7-5820K
6C/12T, ~3x slower, with two sibling lanes building): the suite ran **792.6 s**, of which
`TestZip64LargeDirectory` alone was **774.0 s**. Still inside 20 m, but with only ~35 % headroom on a
slow loaded box — so if a future sweep reports `archive/zip` as an empty verdict, suspect the
deadline before suspecting the package. **Two remedies landed 2026-08-10:** `$longTimeouts` is now
a FLOOR rather than an override, so a larger `-TestTimeout` raises these entries like it raises
every other package (a *smaller* value still loses to the table) — until that fix the table won
unconditionally and the flag was silently ignored for exactly the four packages that need it (an
i7-5820K sweep reported `hash/maphash` and `crypto/dsa` as `FAIL … package timeout after 00:30:00`
and re-running at `60m` died at 30:00 again, while the same package's pipeline driven by hand at
`60m` validated its banked 22/22). And the floors themselves were recalibrated to the slow host at
merge (maphash/dsa `60m`, suffixarray `120m`; archive/zip's `30m` stands on its 774 s measurement),
so a bare sweep passes on this machine class with tight nets kept on the other 121 packages.

### The crash-save classification refines r57b's NUL rule

r57b found the first instance of crash corruption in a `wip(...)` snapshot — files that are 100 %
NUL bytes, NTFS having committed each file's SIZE and lost its DATA — and proposed the
size-vs-committed-content check as the test that separates it from real drift. **This lane's wip
carried five more (`go/internal/gccgoimporter/{ar,gccgoinstallation,importer,package_info,parser}.cs`)
and that test would have MISSED all five**: their sizes do not match the committed content, they
match the *intended new* content, because each was mid-rewrite by a `-tests`-closure emission when
the machine died. The reliable discriminator is therefore the content itself — **a file that is
100 % NUL is corruption, whatever its size** — with the size comparison demoted to a corroborating
detail. `git diff --stat` names them for free: a `.cs` reported as `Bin <old> -> <new> bytes` is
never legitimate converter output.

The rest of the wip classified into the standing families with nothing unexplained: the
`-tests`-closure production restore family (`Δio` alias in `bufio`/`bytes`/`crypto`, the
`global::go.*` root escape in `crypto/md5`, and one `initᴛᴛtests` package_init hook in
`crypto/ecdh`), three `-text` `compress/testdata` CRLF phantoms, and a stray 16 MB `src/go2cs.exe`
build artifact at the repository root — which is worth one line of its own: the converter's
gitignore entry is `/src/go2cs/go2cs.exe`, so a binary built one directory up is **tracked**, and a
crash-save picks it up.

### Handoffs — neither owned by this lane

- **`ByteSeqAllocationTests`' `@string` bound is stale-LOOSE.** The window makes a sub-string
  allocation-free, so the test's asserted upper bound now passes with room to spare rather than
  measuring anything. It belongs to **r58a**'s allocation-counting arc, which is the lane that will
  have a true count to tighten it against.
- **`InterfaceInheritance` / `ValueOf(Type).Pointer()`.** The one behavioral failure seen while
  gating this lane was proven **pre-existing on master** (reproduced at the merge base), and its root
  is in the reflection bridge — **r58b**'s area. This lane's only touch on that file is a comment.

## Coordinator ratifications — the alloc-count rulings, user-confirmed (2026-08-10)

The user ratified the r58a merge's disclosure rulings, with the honest assessment carried here so
the decision and its evidence stay together:

- **`crypto/rsa` `TestAllocations` — DISCLOSED, ratified.** The true count is **340,756 objects
  per run against a budget of 10** — five orders of magnitude, dominated by managed big-integer
  arithmetic no golib optimization can remove. Squarely the provably-cannot-satisfy class the 38
  existing `alloc-profile` disclosures pin.
- **`net/http/internal` (2 objects vs budget 1) and `math/big` `TestNewIntAllocs` (1 vs 0) — NOT
  disclosED, ratified.** These are near-budget lower-bound counts: nothing proves the extra object
  is unavoidable rather than a golib inefficiency, and disclosing them would launder an
  optimization target as an impossibility — the exact move r56d refused. They stay characterized
  and undisclosed until either an optimization closes them honestly (the ж-box arc is the likely
  instrument) or a proof of unavoidability emerges. `log` (4 vs 1) is moot for banking regardless:
  its `TestAll` fails on the `runtime.Caller` architectural arc the board already carries.
- **`path/filepath` stays banked at 61 — ratified.** The six symlink rows are host-privilege-
  dependent and BOTH runtimes agree in both states; a general roster mechanism for
  host-conditional verdicts is commissioned as a chip rather than a count bump that would
  false-red unprivileged boxes.

Also ruled in the same pass: the ж-box arc is commissioned chip-class AFTER the near-miss harvest
(the counter gives it an exact instrument); the init-ORDER arc starts as a characterization scout;
GOROOT-tree-reproduction is DEFERRED past 75% (four packages against a harness-contract change
re-validating all 126); r59 runs as the next dedicated lane after the harvest with backlog 24
riding its regen; NuGet 1.23.1.6 is approved after the day's final consolidated sweep (release
push user-owned).

## Harvest r60 — the post-1.23.1.6 collection (2026-08-11)

The first release-gated harvest, run across two machines the same day the fixes landed. Every item
below supersedes its older census row; the roster is the authority as always.

**`encoding/asn1` — BANKED 38/38** (roster 127, `74cec76e3`). The full arc: 28/38 under one
hypothesized DER root → r57a's `StructField.Tag` bridge closed six for free → r57b split the
remainder into four TRUE roots → r58b's typed-nil packing took one, L6 took two
(`TestMarshal` #37 AND `TestCertificate` — via `reflect.Type.Name()` blanking defined container
types, NOT the hypothesized converter SET-tag defect; converter unmodified), L7 took the last
(`StructField.PkgPath` unset on the type side — NOT flagRO; the value side was already refusing
writes correctly). The two lanes' residual sets were exactly complementary and neither could
observe the union; the pinned-machine measurement confirmed 38/38.

**`crypto/internal/edwards25519` — measured 54/55 on merged L4+L7** (was 0/55, a whole-package
cctor casualty). L4's tuple-spec relocation lets the package RUN; L7's array-dims fix greens both
`quick.Check` rows with real `[32]byte`/`[64]byte` values. Sole residual: `TestAllocations`
(109 objects vs want 0 — the ж-box arc's row; NOT disclosed per the near-budget ruling). NOT
banked. ⚠ The fix's production emission (a new ordered `package_init.cs`) is deliberately
UNCOMMITTED — additive-only drift owed to r59's queued whole-corpus regen, per the
no-casual-regens rule.

**`math/big` (224/226) and `nistec` (2,195/2,200) — refresh deliberately SKIPPED.** Nothing in
this harvest touches their residual roots (the want-zero counter rows and TestMulUnbalanced's
truthful performance measurement — all ж-box territory). Their recent measurements stand; a
refresh would have measured the same defect-free packages against the same open arcs.

### New open items from the lanes' re-attributions

- **`rtype.PkgPath()` answers "main" for an UNNAMED struct where Go answers ""** — the sibling of
  L6's Name() fix, found by L7's cross-validation, fixed by neither. Latent until a consumer
  compares package paths of anonymous types.
- **`abi.Type.HasName()` is false for every synthesized descriptor** — dormant, but populating it
  changes `directlyAssignable`'s short-circuit corpus-wide; wants its own lane, not a drive-by.
- **`StructField.Anonymous` + embedded-field ORDER** — go2cs-gen emits promoted-embed boxes after
  declared fields, so bridge walk order differs from Go's declaration order. One increment, needs
  a demonstrated consumer.
- **`Out(i).Len()` for a func returning a fixed-size array** — no attribute position exists on a
  ValueTuple; recorded, unowned.
- **A bridge-minted method value keeps a dims-less descriptor** — adjacent to L7's fix, same
  remedy shape, needs a consumer.

### Machine traps (both cost real time on laptop-1; both now protocol)

- **`-tests` self-location does NOT fire when a deployed root exists**: a valid machine-global
  `%USERPROFILE%\go2cs` pre-empts self-location ("an explicitly configured working root always
  wins"), and the resulting version-mixed build dies with `MSB4006 circular dependency ...
  unsafe.csproj` — which reads exactly like a corpus defect and is not one. EVERY pipeline
  measurement passes an explicit `-go2cspath <checkout>\src`.
- **`Copy-Item` preserves `LastWriteTime`**, so a copy-aside/restore A/B leaves the restored file
  OLDER than build output and MSBuild skips the rebuild — surfacing as a phantom `CS0117` against
  source that plainly contains the member. Touch restored files; `git checkout` stamps fresh.

### Process rulings recorded in passing

- **Version flips belong to the release ritual; hand-fixes own numbers.** The io/rsa badge
  regeneration pinned 1.23.1.6 pre-release (safe only because Phase 1 had already bumped); the
  clean rule is mid-cycle regens pin the published version and the ritual does all flipping.
- **`push-nuget.ps1`'s badge preflight is blind to a MISSING badge** — a banked package with no
  Tests badge ships silently (crypto/rsa nearly did). Hardening owed: a banked proof page with no
  corresponding badge claim fails as loudly as a wrong one.
- **L5's publish-stamp follow-up stands**: the preflight still proxies "published" via the build
  release; the repo-recorded stamp written by the publish ritual (feed query advisory-only) is the
  ruled remedy.
- **The proof pages are an L8-guarded surface too**: a sweep on a mispinned toolchain rewrites
  `docs/validation/current/*`'s Go-version stamp with counts unchanged (observed:
  `encoding.binary.md` 1.23.1 → 1.23.2, restored not banked). L8's guard covers the sweep; this
  is the second thing it protects.

### Backlog: the AOT full-trim column, deferred with its reasoning (user query, 2026-08-11)

Full trimming (vs the suite's TrimMode=partial) would shrink the AOT binary and some of its
startup/memory floor -- but it strips exactly the metadata golib reaches reflectively (fmt's
formatting, sort's Interface<T>, the bridge's walks), so today it fails Verify rather than
producing numbers: a column of n/a at ~25 min of ILC per benchmark on the current coordinator
machine. DEFERRED, not declined: when the zh-box arc and a trim-eligibility pass shrink the
reflection surface, the fourth column measures a thing that works and shows the payoff. Note
also that trim is not the dominant startup lever -- Go package initializers are semantic roots
trim can never remove; the larger honest lever is lazy/dead-strippable package inits (Go's
linker dead-strips unreachable packages; go2cs loads and inits the whole referenced closure),
recorded beside the working-set note in the performance README.

One sharpening from the user (2026-08-11): hoisted string literals materialize at package init
(module initializers run eagerly at assembly load), so the hoist cost -- deliberately moved to
startup to kill per-use allocations and UTF16->UTF8 conversions, and still the right trade by the
StringMatch numbers -- COMPOUNDS with the eager-closure cost. The lazy-package-init arc therefore
recovers both at once: unused packages skip their init() AND their literal materialization. The
two items are one lever.

## ж-box arc stage A1 — the census is in, the projection HOLDS (lane L3, 2026-08-11)

The zero-emission census the design gated every golden move behind ran corpus-wide, three GOOS
targets, on laptop-1 (⚠ go1.23.2 — developmental until the pinned-machine re-derivation, which is
ONE command). Full report: [`CENSUS-zh-box-a1.md`](CENSUS-zh-box-a1.md). The instrument is
`-ref-census` over the new classification pass (`refLoweringAnalysisOperations.go`, wired into all
three drivers, `-debug` census per package; CNR byte-identical — analysis only). The headline
verdicts, so the next session doesn't have to open the report to know where the arc stands:

- **§3.6's projection CONFIRMED on the recommended (§10.3 hoisted-temp) branch** — fiat lowers
  **96/96** pointer params and **150/158** address-taken locals; every class the ~7k-P256-residual
  projection zeroes is statically covered (row-5 conversion sites measured *wider* than the
  panel's 16: 20 fiat + 7 edwards25519-scalar, all `conv-of-address` at lowered positions). The
  branch question stays CLOSED.
- **The §3.3 emission table covers the corpus**: zero `other-veto` argument shapes at lowered
  positions on all three targets (the caller-side strip never fired outside unit tests).
- **Per-GOOS delta = 25 positions across 5 packages, ALL already layout-L3** (runtime 14, net 7,
  os 2, filepathlite 1, syscall 1) — zero new L3 memberships; A2's merge churn is within-package.
- **Hand-own audit closes small**: census re-measured **49 marked / 41 `*_impl.cs` / 59 distinct**
  (the design's 44+26 moved, as CLAUDE.md says it does); 17 textual candidate references resolve
  to **4 real** (crc32's `castagnoliShift`/`slicingUpdate`, runtime's `getLockRank`/
  `lockWithRankMayAcquire` from `mfinal.cs`) + 13 comment collisions. A2 remedy: an X5
  declared-in-hand-own arm (mechanical, reuses the marker probe) + a 3-function curated list;
  **zero hand-own edits owed**.
- **A′'s prize measures small** (§10.1's checkpoint input): strict exported candidates **64**
  funcs / **69** corpus call-site records / +68 lowered positions (564→632). The design's 347 was
  a looser screen (census L1 = 354 ≈ 347; buckets and the constructor-shaped count deviate —
  reported, not reconciled; re-derive on the pinned machine before deciding A′).
- Findings that reprice A2 details, none the branch: the §3.2-vs-§3.3 defer/go tension resolved
  toward §3.3 (defer/go never strips the callee; the caller-side param mirror vetoes as
  `X2-defer-arg`); `p[:]` pointer-slice is a real non-D shape (19 params, tagged, copyFieldElement
  the fiat instance); `SetBytes`'s `in` keeps its box under the design's own `x[:]` predicate (§7's
  probe rows gain a small named residual term); `edwards25519/field`'s `feMul`/`feSquare` strip on
  the trailing `carryPropagate()` METHOD call — the second fiat family's field half is B′
  constituency, its scalar half lowers fully.

**Pinned-machine re-derivation (coordinator, go1.23.1, 2026-08-11): the instrument reproduces.**
Exact agreement on every aggregate the census itself prints — 25 positions / 5 packages
all-already-L3, hand-own 49/41/17, exported return-shaped 3 — with exported candidates 62 vs the
laptop's 64, exactly the GOROOT patch-release shape the developmental flag existed for. A2 is
GO on the go-ahead ratifications recorded with its opening.

## L9 wave, desktop share — six hypotheses measured, six honest answers (2026-08-11 night)

The stale-census re-measure wave's items 1–2 plus the seam canary, run on the pinned machine
against post-r59 master. One census was stale in each DIRECTION, and the rest attributed:

- **`debug/dwarf` — the ONE-ROW-AWAY row was stale-BANKED**: the package has been on the roster
  at 40 for some time; tonight's pipeline re-validated it 40/40 on merged master (a free
  re-validation, not a bank). The r57b lesson re-taught at the coordinator's own expense:
  **roster first, board second — every census row is a hypothesis, including the hypothesis
  that the package is still unbanked.**
- **`net/textproto` 25/26 unchanged, with its number UPGRADED**: `TestCommonHeaders` now measures
  **3 objects per run vs want 0** (was 816 shim-bytes) — the common-header fast path materializes
  three golib allocations Go's interning avoids. Near-budget: not disclosed, not banked; a precise
  interning/ж-box target. **CLOSED by L11 (2026-08-12): banked at 26/26** — the counter's 3 (and the
  bytes behind them) decomposed measured, not inherited: 2 obj/216 B in `Once.Do`'s `of()` chain,
  1 obj/40 B in the `m[string(b)]` probe-key materialization, plus 560 B/call of dead
  `BigInteger.Parse` from the folded mask local. All three fixed at their own layers (hand-owned
  atomic/once, converter `tmpstring` emission, converter big-const hoist); the L9 note's "likeliest
  instant bank" hypothesis was wrong in the instructive direction — the counter-shim fix alone
  changed the NUMBER, not the verdict.
- **`mime/multipart` ~11/52 — real roots, census confirmed**: `TestMultipartSlowInput` crashes the
  host mid-suite (`multipart_test.cs:172`), and the `ReadForm` limits family
  (`TestReadFormEndlessHeaderLine`, `TestReadFormLimits`, `TestReadForm_MetadataTooLarge`) plus
  `TestQuotedPrintableEncoding` fail on content. Needs its own characterization lane.
  **CLOSED by L12 (2026-08-13): banked at 52/52 — and this bullet was WRONG in every particular,
  which is the part worth keeping.** The lane's own baseline on `137b86552` measured **7 of 52**,
  not ~11, reproducing the ORIGINAL census (above) rather than this one. There is no host crash:
  all 52 verdicts are reported and `TestMultipartSlowInput` is simply one more
  `infrastructure-error`. Nothing "fails on content": the four named rows are the PARENT tests
  whose subtests errored — the `internal/coverage/cfile` "read the row as three failures, not
  twelve" shape, one bullet further down this very section. And the ~41 tests said to be hidden
  behind the crash are not hidden; they run, and all 41 carry the IDENTICAL exception
  (`readMIMEHeader: external (assembly or cgo) function is not implemented`), with no second root
  anywhere in the log. Read together with the `net/textproto` bullet above — whose "likeliest
  instant bank" hypothesis was also wrong — this section's own methodology note earns another
  entry: a differential summarized from verdict NAMES invents structure that the exception TEXT
  does not support. Attribute from the roots, and count parent rows as shadows before calling them
  a family.
- **`go/parser` 6/173 — the CWD class gains its FIFTH member**: the test package's static
  initializer runs `var src = readFile("parser.go")` and panics (`performance_test.cs:15`), so
  the host dies before test one. The deferred synthetic-GOROOT arc now holds **167 verdicts for
  this package alone** on top of its original four — its deferral price has materially risen and
  deserves re-prioritization when the wave settles.
- **`go/doc` 24/85 unchanged — attributed**: the `Test/AllDecls/*` golden-file family RUNS and
  fails on CONTENT (not infrastructure) — a real doc-extraction root, not the CWD class.
- **`net/rpc` 4/13 — the seam canary reproduces the diagnosis byte-for-byte**:
  `index out of range [0] with length 0` at `array.cs:280` via `sockaddr`
  (`syscall_windows.cs:881`) — the exact r57b stack, on demand, same-day baseline for L10.
  The three held rows (httputil/httptest/cookiejar) stay held, as ordered.

## L9 wave, laptop-G share — item 3's two toolchain-exec rows, both censuses CONFIRMED (2026-08-11 night)

Item 3 (`internal/coverage/cfile`, `go/internal/gcimporter`) measured on **laptop G** (Ryzen 5 PRO
6650U, 6C/12T, pinned go1.23.1) against `dc78eb63c`, `-tests -test-action all -test-timeout 30m`
with an explicit `-go2cspath`. Wall clock: cfile **199 s**, gcimporter **~630 s** (its C# suite
alone 235 s) — both cheap, as the wave promised.

**Neither census was stale. Both reproduce their first-census number to the verdict** — 4 of 16 and
399 of 583. These two rows are **walled, not stale**, and nothing in the nine landed fixes touches
either wall. The item's stated expectation — "both exec the Go toolchain, so expect the
GOROOT-tree/cwd class in the residue" — is **right for one and wrong for the other**, which is
exactly why the row was measured rather than inherited.

### `internal/coverage/cfile` — 4 of 16 confirmed; the WORKING-DIRECTORY class gains its SIXTH member

Go emits 16 verdicts, the host 7; **4 match** (`TestAuxMetaDataFiles`, `TestCoverageSnapshot`,
`TestTestSupport` pass/pass; `TestIssue56006EmitDataRaceCoverRunningGoroutine` skip/skip). Three
top-level tests fail — and **nine of the twelve mismatches are one root's shadow**:
`TestCoverageApis` `t.Fatal`s in its setup before any of its nine `t.Run` subtests register, so they
compare `Go="pass"` against `C#=""` (absent). Read the row as **three** failures, not twelve.

All three are the established converted-host WORKING-DIRECTORY class (§*The converted-host
WORKING-DIRECTORY class — why no cheap subset exists*), reached because each shells out to the Go
toolchain with a path relative to CWD whose MEANING depends on sitting under `$GOROOT/src`. The host
runs in `<temp>/go2cs-tests/<pkg>/<guid>/cfile` (`TestHost.CreateRunDirectory`, fixtures staged
beside it); `go test` runs in `$GOROOT/src/internal/coverage/cfile`. Two facets:

- **Internal-import visibility** — `TestCoverageApis`, `TestApisOnNocoverBinary`. `buildHarness`
  runs `go build … testdata/harness.go` (`emitdata_test.go:123`, RELATIVE) and `harness.go` imports
  `internal/coverage/slicewriter`. The toolchain grants internal visibility by the importing FILE'S
  LOCATION, so the staged copy under temp is refused: `testdata\harness.go:10:2: use of internal
  package internal/coverage/slicewriter not allowed`. **The staging is not at fault** — the error
  names line 10 column 2, which *is* that import, so the file was found and read; only its location
  was wrong.
- **Module resolution** — `TestIssue59563TruncatedCoverPkgAll` sets
  `cmd.Dir = filepath.Join("testdata", "issue59563")` (`emitdata_test.go:510`) and runs
  `go test -coverpkg=all`. Neither GOROOT's copy nor the staged one carries a `go.mod`; Go's run
  succeeds because the parent walk reaches **`$GOROOT/src/go.mod` (`module std`)**, while the host's
  walk reaches the filesystem root with none → `go: go.mod file not found in current directory or
  any parent directory`.

A textbook confirmation of that section's "no cheap subset" ruling, and it **extends the content
list**: alongside `bin/go.exe`, `doc/godebug.md` and sibling sources, a synthetic GOROOT must also
reconstruct **`src/go.mod`**, and must be a location the toolchain accepts for internal imports.
Both facets are satisfied by that one arc and by nothing smaller — a per-test accommodation would be
dishonest here, since these tests are measuring the real toolchain's behavior. **+12 verdicts** to
the arc's ledger, on top of `go/parser`'s 167.

### `go/internal/gcimporter` — 399 of 583 confirmed; NOT the cwd class — it is `go/types` generics

583 verdicts both sides, **399 match, 184 mismatch, and every mismatch is inside ONE top-level
test**: `TestImportTypeparamTests` (its parent row plus 183 subtests), all `Go="pass" C#="fail"`.
Everything else agrees, including **`TestImportStdLib`'s 303 matching subtests** and 82 of the
typeparam subtests themselves (68 pass, 14 skip).

The predicted GOROOT-tree/cwd class **does not appear**, for a precise reason worth keeping:
gcimporter's toolchain execs root their paths at `testenv.GOROOT(t)` — ABSOLUTE, and the pipeline
exports that GOROOT to both sides deliberately (`runCommandWithTimeout`) — whereas cfile's are
cwd-relative. Same "execs the toolchain" premise, opposite outcome.

The real root sits one layer down, **in the test's own ORACLE half**. Each subtest runs
`compile` → `importPkg` → `checkFile` in that order (`gcimporter_test.go:184-187`), and every
failure is in `checkFile`, i.e. `go/types.Check` over the source file. **So `compile` and
`importPkg` both SUCCEEDED for all 183** — the converted gcimporter read the gc export data fine,
and 303 stdlib packages import correctly — and the converted **`go/types` then failed to type-check
the generic source handed to it as the reference.** The subject of the test is healthy; its
yardstick is not.

| Shape | Count | Evidence |
|:--|--:|:--|
| Bogus type errors on TYPE PARAMETERS | 92 | `absdiff2.go:70:9: cannot use a.Value_ (variable of type T constrained by orderedNumeric) as T value in return statement` — T judged not assignable to T. Also `cannot assign map[K]V to map[K]V`, `S does not implement I[byte]`, `cannot infer S`, `cannot assign []int to [5]int` |
| Nil-pointer panic inside the checker | 91 | re-panicked value is `runtime error: invalid memory address or nil pointer dereference`, surfacing through `check.cs:430` |

⚠ **The panic stacks are the RE-PANIC, not the fault.** `check.cs:430` is the `default: throw
panic(p)` arm of `handleBailout` — a faithful conversion of Go's own re-panic for a non-`bailout`
recover value — so the frames shown are the bailout path (`430 → GoFrame.Run → 435 → 414`) and the
ORIGINATING frames are gone. Anyone working this must instrument the re-panic or disable the
recover; **taking `check.cs:430` as the defect site will waste the lane.**

Whether the 92 and the 91 are one root or two is **not established here and must not be assumed**.
The dominant signature — a type parameter judged not identical to itself — would plausibly produce
both, but that is a hypothesis, and this week's board has a four-for-four record against inherited
ones. `go/types` is unbanked and carries no board row of its own; on this evidence it has earned
one, and **`go/internal/gcimporter`'s 184 rows are downstream of it** — the row moves when
`go/types` generics do, and not before. Nothing in gcimporter itself is implicated.

#### The nil-panic is NOT confined to generics — and there is now a minimal reproducer (2026-08-13)

Filed from `claude/types-errors-delta-rename`, which cleared `internal/types/errors`' build blocker
and so ran that package's suite for the first time. Both its top-level tests die on **this exact
signature** — `runtime error: invalid memory address or nil pointer dereference` re-surfacing
through `check.cs:430` — and neither is a generics test.

Both fail at the SAME call, `codes_test.cs:70` (`walkCodes`), which is
`conf.Check("types", fset, []*ast.File{file}, info)` over **`codes.go` itself** — reached *before*
any Example snippet is ever type-checked. That input is about as small as the checker's surface
gets:

- **no imports at all**, so `importer.Default()` is never invoked — the importer is out of suspicion;
- **no generics** — `codes.go` declares exactly `type Code int` plus `iota` const blocks. (Five
  `[T any]` greps in the file are all inside doc comments; each was checked line by line.)

So the 91-count nil-panic class **cannot be wholly explained by the generics hypothesis**, and the
"one root or two" question above now has real evidence on the *two* side. Anyone taking this on
should prefer this reproducer over gcimporter's typeparam corpus: one import-free non-generic file,
two failing tests, ~7 s to the panic, versus 583 verdicts behind a compile+import pipeline.

⚠ The re-panic warning above applies here unchanged — `check.cs:430` is `handleBailout`'s faithful
`default: panic(p)` arm and the originating frames are gone. Instrument the re-panic or disable the
recover first; this lane re-derived that independently before finding the note, which is some
evidence of how naturally the stack misleads.

#### ROOT FOUND AND FIXED (2026-08-13, `claude/go-types-checker-wall`) — an interface MAP KEY compared by wrapper identity

The reproducer above did its job: the fault is **one golib defect**, it is **not** generics, **not**
the importer, and **not** the ж-box or scheduler arcs' territory.

**How it was reached.** The re-panic warning is correct and the stack really is useless, but the
originating frames were never destroyed — they are snapshotted at the point the .NET exception is
ADOPTED as a Go panic (`RuntimeErrorPanic.TryAsPanic` → `PanicException.CaptureThrowSite`). Dumping
`PanicTrace` there named the site in one run:

```
at go.ж`1.op_OnesComplement(ж`1 value)            in golib\ж.cs:957
at go.go.types_package.dependencyGraph(map`2 objMap) in go\types\initorder.cs:233
at go.go.types_package.initOrder(ж`1 Ꮡcheck)      in go\types\initorder.cs:33
at go.go.types_package.checkFiles(...)            in go\types\check.cs:515
```

*(Why `InheritThrowSite` did not already surface this: the re-panic at `check.cs:430` is thrown
INSIDE `handleBailout`'s own `try`, so the emitted `catch … when (GoFrame.IsPanic(…))` snapshots its
own one-frame site first, and `InheritThrowSite`'s `??=` then declines the origin. Worth a separate
diagnostic fix — it is what makes this stack mislead every reader — but it is not the wall.)*

**The mechanism.** `initorder.cs:233` is `for d := range objMap[obj].deps`, and `objMap[obj]`
**missed**, returning a nil `ж<declInfo>` that `~` dereferenced one frame later. In Go the lookup
cannot miss: every key of `M` came from ranging `objMap`. The two differ only in the STATIC interface
the key is held in — `Object` going in, `dependency` coming back out of `obj.(dependency)`.

Go compares interface values by (dynamic type, dynamic value), and that ONE relation serves both `==`
and map-key lookup. In the conversion they had diverged: emitted `==`/`!=` route through
`builtin.AreEqual`, which unwraps the three adapter tiers, while `map<K,V>`'s backing `Dictionary`
used the DEFAULT comparer and compared the WRAPPERS — and an interface value's wrapper is not stable,
since asserting to a narrower interface yields a different adapter object over the same receiver box.
**Equal but unfindable**: `Object(d) != obj` answered correctly (`AreEqual` unwrapped) while
`objMap[d]` missed. Only the compile-time `ImplementGenerator` adapters ever carried the
unwrap-and-hash contract; the runtime shells `go2cs-gen` builds for a duck-typed assert
(`Δ<iface><T>`, `Δ<iface>ᴛObj`) override neither `Equals` nor `GetHashCode`.

**The fix** is golib-only and centralizes rather than duplicates: `GoEqualityComparer` projects
`AreEqual` as an `IEqualityComparer<TKey>` and hashes the UNWRAPPED root (the same rule the
compile-time adapters already used), installed by `map<K,V>` only when `typeof(TKey).IsInterface` or
`TKey` is `any` — so concrete keys keep `EqualityComparer<TKey>.Default`'s devirtualized path, the
test being a JIT-time constant per instantiation. Restating the relation in each generated shell was
rejected: `AreEqual` is golib's single definition of Go equality and a per-shell copy is exactly the
drift that produced this. Guarded by the **`InterfaceAssertionMapKey`** behavioral test; documented in
`ConversionStrategies-Reference.md` under *An INTERFACE map key compares by Go equality*.

**Measured movement.** `internal/types/errors` **BANKS at 155/155** (0 mismatches, `status:
validated`) — the row's full 155 verdicts, from **0** before. The pre-fix host produced two
nil-panics and zero verdicts; post-fix it type-checks `codes.go` and every Example snippet, and the
subtests that pass include the generics family (`NotAGenericType`, `WrongTypeArgCount`,
`CannotInferTypeArgs`, `InvalidTypeArg`, `InvalidInstanceCycle`, `MisplacedTypeParam`).

**`go/internal/gcimporter` moves 399 → 475 of 583** (+76; mismatches 184 → 108), and the split the
row's census left open — "whether the 92 and the 91 are one root or two is NOT established here and
must not be assumed" — is now **answered: TWO**. The nil-panic class is *entirely* gone (zero
`invalid memory address` and zero `check.cs:430` occurrences across the whole 583-verdict run); every
one of the 108 residual mismatches is the OTHER class, the bogus type-parameter errors, unchanged in
signature (`absdiff2.go:70:9: cannot use a.Value_ (variable of type T constrained by orderedNumeric)
as T value in return statement`). That second root — a type parameter judged not identical to itself —
is **still open and is not this fix's**, and gcimporter's remaining rows stay downstream of it. The
row does NOT bank; test sources deliberately not committed.

**`go/types` itself: NOT measured — one BUILD blocker, and it is not the wall.** With the wall down,
`go/types`' own suite was taken through `-tests -test-action all -test-timeout 90m` for the first
time. The conversion **fully succeeds** — all 34 `_test.cs` files emit — and the host build produces
**exactly one** error:

```
check_test.cs(200,53): error CS0839: Argument missing
    defer(ᴛ1 => throw panic(errΔ2), , ref ᒐ);        // Go: `defer panic(err)` (check_test.go:170)
```

**Mechanism.** `visitDeferStmt.go:62-66` forces the temp-param lambda form for a BUILTIN callee, so
`paramCount == 1` and `lambdaContext.callArgs` is sized 1 — but `panic` is not rendered as a call.
It emits `throw panic(<expr>)` with the ORIGINAL argument expression inlined in the lambda body, so
the `ᴛ1` substitution never happens and `callArgs[0]` is never filled, leaving the empty argument slot
above. Note the near neighbours are fine: `defer delete(w.seen, typ)` (`infer.go:715`,
`typestring.go:121`) converts and compiles today, which is why the corpus never surfaced this — the
defect is specific to the one builtin that is a `throw`, not a call.

**Remedy shape.** Prefer routing `panic` through the same temp-param substitution as every other
builtin — `defer(ᴛ1 => throw panic(ᴛ1), errΔ2, ref ᒐ)`. The tempting alternative (drop the lambda
param and let the body capture the expression) is WRONG: Go evaluates a deferred call's arguments at
`defer` time, so capturing `errΔ2` would report whatever the variable held when the frame unwound.
Small and well-scoped, but it is a CONVERTER change and therefore owes its own CNR + full behavioral
gate, which is why this lane characterized it rather than folding it into a golib-only commit.

**Adjacent, NOT measured — a plausible sibling worth one probe.** go2cs-gen's struct-equality template
compares an INTERFACE-typed field with C# `==` and hashes it with `HashCode.Combine(field, …)` — e.g.
`go/types`' own `graphNode`: `this.obj == other.obj`. On a C# interface `==` is reference equality, so
a struct carrying an interface field would compare by ADAPTER identity rather than by Go's (dynamic
type, dynamic value) — the same class as this fix, one level up. Unlike the map defect it is at least
self-consistent (Equals and GetHashCode are both reference-based), so it produces no equal-but-
unfindable split and no nil-panic; it would show as two structs holding the same dynamic value
comparing unequal. **This is read off the generated template, not observed in a failing test** — it
may well be masked in practice, and it is stated here as a candidate to measure, not as a finding. The
remedy shape, if it reproduces, is the same one used here: route the field through `AreEqual` and hash
`GoEqualityComparer.RootOf`.

⚠ **One environmental note for anyone re-running these by hand:** four subtests
(`InvalidPkgUse`, `UnusedImport`, `UndeclaredImportedName`, `UnexportedName`) use
`importer.Default()` and fail with `could not import fmt … ($GOROOT not set)` when the host exe is
launched directly. That is the known GOROOT-resolution class, not a checker defect — the pipeline
exports GOROOT to both sides, and under it the package is 155/155. Running the host bare gives
150/156.

## Scout batch 1 — twelve never-run packages (2026-08-11)

Twelve packages that had never linked a test host were taken end to end through `-tests -test-action all`
on the pinned coordinator (i7-5820K, go1.23.1). **Nothing banks**, and no row is disclosed — the
2026-08-10 ratification stands, and none of these is an alloc row anyway. The value is the census: five
packages produced a verdict map, seven died before producing one, and all twelve now have a named root.
(**Six** produce one since 2026-08-12 — `net/rpc/jsonrpc`'s build-blocker was fixed and the package runs;
its row and §"Five converter defects" item 3 below carry the measured result. The "nothing banks"
above has since been overtaken by one row: `net/rpc/jsonrpc` **banked 9/9 on 2026-08-14**, three
converter defects after this census first named it.)

| Package | Go verdicts | C# matched | Outcome | Root / attribution |
|:--|--:|--:|:--|:--|
| `internal/chacha8rand` | 4 | **3** | near-miss, 1 row | array-SHAPE reinterpret (`array.cs:280`) — the seam L10 works through `sockaddr`, here with no kernel in it |
| `runtime/metrics` | 2 | **1** | near-miss, 1 row | a `//go:linkname` PUSHED into a test package is unwired; the implementation exists in converted `runtime` |
| ~~`internal/singleflight`~~ | ~~5~~ | ~~**4**~~ | **BANKED 5/5** | the scheduler arc retired the wall — `TestDoAndForgetUnsharedRace` converges in **1.2 s** (was 28.7 min), banked inside the DEFAULT deadline with no `$longTimeouts` entry (SCHED-S2) |
| `net/http/httptrace` | 2 | **0** | rooted | `reflect.MakeFunc` over func-typed struct fields → `abi.FuncType`'s promoted embedded `Type` ref is null |
| `internal/unsafeheader` | 6 | **0** | rooted (architectural) | the package's entire subject is the slice/string HEADER LAYOUT that `golib` deliberately does not have |
| `unique` | 19 | **0** | ⚠ REGRESSION — flagged, not decided | host dies: `Fatal error. Internal CLR error. (0x80131506)` in `System.GC.Collect` ← `runtime.GC()` ← `drainMaps`. Board has this package at **4 of 19** (r43e) |
| `internal/types/errors` | 155 | **0** | ~~converter defect~~ FIXED → now downstream of `go/types` | the Δ-renamed-imported-type defect is fixed (`claude/types-errors-delta-rename`); the package now BUILDS and RUNS, and both tests then die on the `go/types` checker nil-panic — see the sub-row below |
| `internal/fuzz` | 52 | **52** | ✅ **BANKED 2026-08-14** — two blockers deep | alias-to-anonymous-struct (`CorpusEntry`) cleared the BUILD (`79f2b5e62`); the run then died at `flag provided but not defined: -json` because `worker_test.go`'s `TestMain` calls `flag.Parse()`. The host's `TestFlagBridge` now declares its own command line on `flag.CommandLine` the way `testing.Init()` declares `-test.*`, and the suite validates whole |
| `net/rpc/jsonrpc` | 9 | **9** | ✅ **BANKED 2026-08-14** — three converter defects deep | embedded-pointer promotion (2026-08-12), the non-trailing-`default` switch lowering (2026-08-13), and a fixed-size array's LENGTH reaching reflect through a METHOD's POINTER parameter (2026-08-14). The last was one test, not the three recorded here: `TestBuiltinTypes` sorts first, its goroutine panic killed the host, and the other eight recorded no verdict — which is also why the host now reports a goroutine panic with its traceback and flushes what it has. Never socket-walled |
| `testing/fstest` | 7 | — | converter defect | a defined type over ANOTHER package's named map type — the emitted two-hop conversion has only one hop |
| `internal/syscall/windows/registry` | 6 | — | converter defect | the internal-test partial class is emitted non-`static`, and in this package nothing else declares it |
| `embed/internal/embedtest` | 7 | — | not a candidate | test-only Go package: there is no production package for the host to reference |

⚠ **The comparison JSON's `status` string is not a diagnosis.** Seven of these read
`conversion-blocked`, but that value only records "no result file appeared". Five of the seven are C#
COMPILE failures of the converted test sources, one (`embed/internal/embedtest`) is a package that
cannot have a production assembly at all, and `unique` **built and ran** and then killed its own
process. Read the errors array, not the status. (The recorded PS 5.1 `ConvertFrom-Json` duplicate-key
trap did not fire in this batch — a census of all 24 verdict maps found **zero** case-insensitive
duplicate keys — but the raw-text parse was used regardless.)

### The address-reinterpret seam owns three of the five packages that ran

`internal/chacha8rand`, `internal/unsafeheader` and `net/http/httptrace` fail at one architectural
place: **a raw address reinterpreted into a managed shape**. They are worth reading together because
each names a different facet of it, and fixing the narrowest one fixes none of the others.

- **`internal/chacha8rand` — 3 of 4, and the wall is already written down in this repo.**
  `TestBlockGeneric` panics `index out of range [0] with length 0` at `array.cs:280`, inside
  `setup` (`chacha8_generic.cs:66`) reached from `block_generic` (`:141`). The emitted form is
  `var b = (ж<array<array<uint32>>>)(uintptr)(new @unsafe.Pointer(Ꮡbuf))` for Go's
  `(*[16][4]uint32)(unsafe.Pointer(buf))` over a `[32]uint64` — the reinterpret materializes
  `default(array<…>)`, a LENGTH-ZERO array, exactly as `(*SockaddrInet4).sockaddr` does in r57b/L10.
  The other three rows pass **because `block` is hand-owned**: `chacha8_impl.cs`'s header comment
  already states that forwarding to the converted `block_generic` "is also not available — it opens
  the `*[32]uint64` output buffer as `(*[16][4]uint32)(unsafe.Pointer(buf))`, an array-SHAPE
  reinterpretation that a managed nested-array view cannot reconstruct." The hand-own routed around
  the wall for every consumer; the package's own test walks straight into it.
  ⚠ **This is a STRONGER form than sockaddr's and may not fall with it**: sockaddr needs a 2-byte
  window onto a `uint16` field, chacha8rand needs a `[16][4]uint32` view of a `[32]uint64` —
  different element type, different rank, same allocation. Its value to L10 is that it is a
  **pure, kernel-free witness**: one converted function, no socket, no syscall, reproducible in
  seconds, so it makes a far cheaper regression test than `net.Listen`.
- **`internal/unsafeheader` — 0 of 6, and this one is architectural rather than a defect.** The
  package exists to *describe the memory layout of a slice/string header*, and `golib`'s `slice<T>`
  and `@string` are managed containers with no such layout by deliberate design.
  `TestWriteThroughHeader` reports `alias of "Hello, checkptr!" constructed via String = ""` and
  `alias of slice(…) with cap 16 has cap 0` — a header assembled from a `Data` pointer aliases
  nothing. `TestTypeMatchesReflectType` fails one layer earlier and for a second reason: it prints
  `0x2839d0e63b0.Data has type 0x2839d0e6878, but …`, i.e. the reflect surface renders a type as a
  raw address (the known `rtype.String`/`TypeFor` naming row). **Recommend this package be marked
  NOT A CANDIDATE rather than left on the queue** — banking it would require `slice`/`@string` to
  have Go's memory layout, which is the one thing the runtime design says they will not have.
- **`net/http/httptrace` — 0 of 2, and the seam is embedded-struct promotion.** Both tests die on
  `panic: runtime error: invalid memory address or nil pointer dereference` at
  `abi_package.ΔFuncType.get_Type()`, through `ж.FieldRefWrappers…getFieldRef` (`golib/ж.cs:545`).
  `(*ClientTrace).compose` (`trace.go:179-202`) walks its own struct with reflect and calls
  `reflect.MakeFunc` for every func-typed field; reflect reaches the func type by reinterpreting a
  type descriptor as `*abi.FuncType`, and the generated
  `public partial ref abi_package.Type Type => ref ᏑʗType.ValueSlot` then dereferences a promoted
  embedded reference that the reinterpret never populated. Same family as the two above — a managed
  shape conjured from an address — with promotion rather than an array as the victim. httptrace is
  a two-test package sitting entirely behind it.

### `runtime/metrics` — 1 of 2, and the missing row is a linkname DIRECTION

`TestDocs` passes. `TestNames` reports `infrastructure-error`:
`System.NotImplementedException: runtime_readMetricNames: external (assembly or cgo) function is not
implemented`, from a `PartialStubGenerator` stub. **The implementation is not missing** — converted
`runtime/metrics.cs:800` holds `internal static slice<@string> readMetricNames()` carrying its
`//go:linkname readMetricNames runtime/metrics_test.runtime_readMetricNames` comment, and
`runtime/metrics/description_test.cs:35` holds the matching bodyless
`internal static partial slice<@string> runtime_readMetricNames();`. The two are never joined, because
this linkname PUSHES a symbol from a production package INTO a test package rather than pulling one in.
That is a narrower job than the `getg`-style "external (assembly or cgo)" rows it superficially
resembles: **both halves already exist in the corpus and the wiring is the whole task**, and doing it
takes this package to 2 of 2, i.e. to a bank. Cheapest bank-per-unit-work row in the batch.

**BANKED 2026-08-12 at 2 of 2 — but "the wiring is the whole task" was measured FALSE, by three
layers.** The push wiring landed general (a `linknamePushTargets` row whose key simply spells the
`_test` package path — `convertTestVariant` already sets `currentPackagePath` to the external
variant's own PkgPath, so a production package pushing into its own test package needs no new
machinery; the registry guard learned that an external test package's source lives in the base
package's `_test.go` files). Behind it, in the order the reruns surfaced them:

1. **`metricsLock` → `semacquire1` → `getg`** — the pushed body's first call was the runtime
   sleeping semaphore. Hand-owned at the `metricsLock`/`metricsUnlock` boundary
   (`manualConversionFuncs` + `managed_impl.cs`, a `SemaphoreSlim(1,1)`); the same fix unblocks
   `readMetrics` for every future consumer.
2. **`readMetricsLocked`'s slice-header reconstruct** — `metrics.Read` crosses by raw first-element
   address and the runtime rebuilds a `[]metricSample` over it: the L10 address-reinterpret seam,
   measured here as garbage `@string` names out of the fabricated slice. NOT chased through L10:
   the crossing is re-shaped instead — `runtime/metrics/sample.cs` is hand-owned and `Read`
   marshals names in / computed `(kind, scalar, pointer)` out through the public
   `runtime.readMetricsManaged` shim (the `registerPoolCleanup` precedent), preserving
   `readMetricsLocked`'s batch semantics; the `runtime_readMetrics` push row is recorded
   UNHONORABLE with the reason naming the hand-own. The metrics TABLE, `initMetrics`, and every
   compute closure stay auto-converted.
3. **Two `allm` walkers in the compute closures** — `NumCgoCall` and `totalMutexWaitTimeNanos`
   both walk the scheduler's m-list (nil here where Go always has m0). Hand-owned with their
   honest managed answers (no cgo calls exist → 0; the two real wait-time counters minus the
   per-m profile walk); `consistentHeapStats.read` likewise (no Ps, no allocator deltas → the
   zero snapshot, the `ReadMemStats` precedent).

The package is therefore no longer an L10 witness — its seam instance is routed around, not
fixed — and chacha8rand remains L10's cheapest kernel-free reproduction.

### `internal/singleflight` — 4 of 5, one test that does not come back

Go passes 5. The host passes 4 and then reports `{"action":"timeout","elapsed":1800,"output":"package
timeout after 00:30:00"}`; `TestDoAndForgetUnsharedRace` gets **no verdict at all**, so the emitted
JUnit reads `tests="4" failures="0"`, which must not be read as a clean run.

The test (`singleflight_test.go:145`) loops forever with no iteration bound: it launches n=1000
goroutines that each call `g.Do` on one key whose function sleeps `d`, and if `calls != 1` — i.e. if
the goroutines did not all park inside `g.Do` before the first call completed — it **doubles `d` and
retries**. Whether the converted host converges slowly (each retry costs at least `d`, and `d` doubles)
or never converges is **not established here and should not be assumed**; distinguishing them needs a
run with a much larger `-test-timeout` and a print per iteration, which this batch did not spend. What
is established: the other four rows agree, and the package is one row from a bank behind a question
about goroutine parking latency, not about singleflight itself.

#### Convergence measured 2026-08-12 — it CONVERGES: 20 iterations, 28.7 minutes, and the package validates 5/5

The instrumented run the paragraph above asked for was spent (branch `claude/singleflight-convergence`,
laptop lane — Ryzen 7 PRO 6850U, 16 hardware threads — solo, go1.23.1; NOT the coordinator the scout
used, which matters below). Method: the `-tests` pipeline split at its action seams —
`convert`, then a hand edit to the STAGED converted `singleflight_test.cs` adding a per-iteration
stderr print (iteration, `d`, `calls`, `shared`, `ThreadPool.ThreadCount` at iteration start/end,
pending-queue count, spawn/wait timings), then `build`, then `compare -test-timeout 90m` — the same
phases `-test-action all` runs, split only so the edit could sit between conversion and build (the
manifest's input digest covers Go sources, options and runtime sources, not the emitted `.cs`, so the
staleness gate accepts an instrumented staged copy by design). Everything was reverted after: the
measurement emitted a real proof page — the comparison genuinely printed `Validated 5 tests against
go test`, 5/5 matched — and that page was deliberately discarded, because a hand-instrumented host
banks nothing.

**The verdict: converges.** `TestDoAndForgetUnsharedRace` passes at **iteration 20 (d=524s), test
elapsed 1720.8s**; the other four rows total ~0.3s, package wall ≈ 1725s — a **75-second margin
under the 30-minute deadline the scout run died at**. `go test`'s whole package on the same machine:
**0.040s** (the race test itself 0.01s — Go converges on its first iteration; the gap is ~10^5).
`shared` was 0 at the converging iteration, so the assert the loop protects passed: the 28.7 minutes
is all scheduling, and none of it is singleflight.

The census (run 2; run 1, without the pool columns, matched it iteration-for-iteration through 15):

| iter | d | calls | pool start→end | wait |
|--:|--:|--:|:--|--:|
| 1-3 | 1-4ms | 20, 8, 6 | 12→258 | ~0.02s |
| 4-10 | 8-512ms | 4-9 | 258→258, flat | ~4×d |
| 11-15 | 1-16.4s | 4-7 | 258→354 (~+0.9/s) | ~3-4×d |
| 16 | 32.8s | 4 | 354→**162** | 98s |
| 17 | 65.5s | 3 | 162→221 | 197s |
| 18 | 131s | 2 | 221→567 | 262s (=2×d) |
| 19 | 262s | 2 | 567→**75** | 524s (=2×d) |
| 20 | 524s | **1** | **75→1002** | 524s (=1×d) |

Three mechanisms, each owning one phase of the table — and none of them is parking LATENCY:

- **The goroutines that miss the window never parked slowly; they never STARTED.** `spawn_s=0.00`
  every iteration (queueing 1000 work items is instant), and `calls ≈ ceil(1000 / live pool)`: the
  queue drains in WAVES of pool size, and each post-wave batch dispatches only after the previous
  call completed — which by construction mints a fresh call. Hence `wait ≈ calls×d` throughout, and
  no value of `d` helps while the pool is small: the tail is sitting in the pool queue, not in
  `wg.Wait`.
- **Iterations 1-10 are pinned at golib's own floor.** `Goroutine.Start` is
  `ThreadPool.QueueUserWorkItem` (`golib/runtime/Goroutine.cs:64`); the min-thread floor of
  max(4×cores, 256) (`golib/builtin.cs:78`) is why the pool leaps 12→258 in three iterations and
  then sits EXACTLY there — below the floor, creation is on demand; above it, only the starvation
  gate injects (~0.9-1.8/s), and an iteration must hold starvation ≥ ~1s continuously to trip it,
  which is why growth begins only at `d≥1s`.
- **Idle-thread retirement FIGHTS the injection, so capacity cannot accumulate across iterations.**
  Iteration 16 ends 192 threads BELOW its start — 162 live, under the min floor, which governs
  injection aggressiveness, not keep-alive — and iteration 19 ends at 75: once `d` exceeds the
  pool's ~20s idle timeout, every thread left idle through the final wave's sleep is culled.
  Convergence therefore arrives only when a SINGLE `d` is long enough for in-sleep injection alone
  to field all 1000: iteration 20 starts at 75 live and injects ~930 during one 524s sleep, every
  arrival parking into the one in-flight call.

**Why the scout run got no verdict:** the finish sits at 28.7 min ± the injection rate, and a miss
is quantized — the ladder doubles, so slipping one iteration moves the finish to ~55 min
(2245s+1049s), two to ~107 min. 1800s lands inside the measured run's iteration-19/20 window, so
any marginally slower gate — the scout's host was the i7-5820K coordinator, not this laptop, and
the rate is a runtime heuristic, not work — pushes convergence one rung up and the deadline eats
the test mid-iteration, verdict-less, exactly as recorded. That the same ladder shape produced
"4 of 5 + a consumed deadline" on one machine and "5/5 in 28.7 min" on another IS the fragility
finding: the two runs differ by one rung, and a rung is a doubling.

**Remedy recommendation — two options, priced:**

- **A `$longTimeouts` floor of 60m banks the row as measured** (28.7 min with 2× margin, and it
  survives a one-iteration slip at ~55 min by five minutes; only 120m survives two). ⚠ Priced
  honestly, this floor differs in KIND from the four standing ones: maphash's 15-min-vs-7.6s is
  deterministic work on a slow host, while this finish time is a race against the .NET thread
  pool's injection-vs-retirement heuristics with 2× penalty steps — under the standing solo-sweep
  rule it should hold, but it is the first deadline asked to cover a heuristic rather than work.
  It also adds ~29-55 min to EVERY full sweep for one row.
- **The durable path is the one golib already names.** `builtin.cs:75-76` calls the min-thread
  floor "a mitigation, not a scheduler: programs parking thousands of goroutines remain out of
  reach until a cooperative scheduler exists (documented divergence)". n=1000 sits exactly on that
  line, and this table is the divergence's first quantified witness — the row is ultimately a
  scheduler-arc row, and any floor is a bridge across it, not a fix for it.

Per the errand's charter nothing was chased: measurement only, aftermath reverted, no bank.

#### RESOLVED 2026-08-14 — the scheduler arc landed and the row banks 5/5 in 1.2 s (SCHED-S1/S2)

The recommendation above ("the durable path is the one golib already names") was taken. `Goroutine.Start`
now creates one dedicated background thread per goroutine instead of queueing a ThreadPool work item, and
the min-thread floor retired in the same commit (`DESIGN-cooperative-scheduler.md` §5.2, OQ1/OQ3). **No
`$longTimeouts` entry was ever landed** — OQ9 resolved in the ratifying direction, and the bridge is now
dead as designed.

Re-measured on **the same laptop, solo, go1.23.1** — the machine that produced the 1720.8 s table above, so
this is a clean A/B and not a hardware difference. Same method, same instrumentation seams:

| iter | d | calls | pool start→end | goroutines live (peak) | spawn | wait |
|--:|--:|--:|:--|--:|--:|--:|
| 1 | 1ms | 165 | 3→3 | 4 | 141ms | 141ms |
| 2 | 2ms | 112 | 3→3 | 10 | 141ms | 141ms |
| 3 | 4ms | 83 | 3→3 | 20 | 125ms | 125ms |
| 4 | 8ms | 54 | 3→3 | 41 | 125ms | 140ms |
| 5 | 16ms | 35 | 3→3 | 85 | 125ms | 125ms |
| 6 | 32ms | 30 | 3→3 | 45 | 125ms | 157ms |
| 7 | 64ms | 6 | 3→3 | 392 | 125ms | 140ms |
| 8 | 128ms | **1** | 3→3 | **1001** | 110ms | 172ms |

**Converges at iteration 8, d=128ms, test elapsed 1.2318 s** (was iteration 20, d=524s, 1720.8 s — a
**1,397× reduction**). Package wall 1.54 s against `go test`'s 0.040 s, so the residual gap is ~31×, not ~10⁵.

Read the pool column: it is **flat at 3 for every iteration**. Every mechanism the original table
identified is now structurally absent rather than merely faster — there is no queue to drain in waves, no
floor to sit at, and no injection-vs-retirement race, because nothing Go-semantic touches the pool at all.
What remains is honest work: `spawn ≈ 110-141ms` is the cost of creating 1000 real threads (~125 µs each),
and the ladder now climbs only until `d` exceeds that spawn window — which is why it stops at 128ms. The
live-goroutine count is the new registry's, and it returns to 2 after every iteration, so 1000 threads are
created and retired eight times over with no leak.

The fragility finding retires with the row: the finish no longer sits on a heuristic's knife edge, so it
cannot slip a rung between machines. Banked per the validated-package ritual (roster line, proof page,
committed test sources) inside the DEFAULT deadline with ~3 orders of margin.

### `unique` — a REGRESSION against this board's own record, flagged for a bisect lane

The r43e-weak entry above records `unique` at **4 of 19** in a "2-minute run with 19 verdicts", after
`runtime.GC()`'s hand-owned body learned clearpools' unique arm. Today, against `origin/master`
(`5fe713f98`), the host produces **zero** verdicts: every `TestHandle` subtest starts, and the process
then dies with

```
Fatal error. Internal CLR error. (0x80131506)
   at System.GC.Collect(Int32, System.GCCollectionMode, Boolean, Boolean)
   at go.runtime_package.GC()
   at go.unique_internal_test_package.drainMaps(go.ж`1<T>)
   at go.unique_internal_test_package+<>c__DisplayClass21_0`1[[…testStringStruct…]].<testHandle>b__0
```

**Reproduced twice**, per the standing "re-run before believing a crash" rule — identical stack, the
naming subtest differing only because the subtests run in parallel. The scout's own run of the same
package instead consumed its 30-minute deadline and was recorded by the pipeline as
`conversion-blocked`/`dotnet timed out after 31m0s`; a hang and an execution-engine crash are the same
"no verdicts" outcome from the pipeline's point of view, so **the recorded status understates this row
in both runs**.

`0x80131506` is `COR_E_EXECUTIONENGINE` — a GC-state fault, not a managed exception, and this board
already ties that exact string to the address model (see the `ж<T>`/`EnsureStableAddress` analysis
above, where a 4 KB write into freed heap surfaced as `ExecutionEngineException` and
`Fatal error. Internal CLR error.`). `golib` has moved materially since r43e — the `@string` window,
the allocation counters, the array-backing materializer — so a plausible story exists in more than one
place, and **that is precisely why this is flagged rather than attributed**. It wants a bisect between
r43e and `5fe713f98` with `unique`'s host as the probe, and it should be treated as higher severity
than an ordinary failing row: a memory-safety fault does not stay inside the package that reveals it.

#### RESOLVED (2026-08-12, unique-bisect lane) — bisected to the `@string` window; closed by a `clone.cs` hand-own

**The culprit is `fc6d8c179` (r57c-zipperf, 2026-08-09): "golib: a Go string is a WINDOW".** Bisect
over the 126-commit first-parent range `57abfe9e1..5fe713f98`, `unique`'s pipeline as the probe:
r43e anchor GOOD (full 19-verdict map, the recorded 4-of-19-era shape), `f2b80a766` (#63) GOOD,
`90059385b` (#80, r57b) GOOD, `515cea127` (#81, r57c) BAD, and `fc6d8c179` — the window commit,
probed directly against its own parent's lineage — BAD. The window is present in every BAD tree and
absent from every GOOD one.

**Mechanism — one defect, two eras.** Converted `unique/clone.cs:28` rewrote every string field of a
value IN PLACE the way Go does — `(*string)(unsafe.Pointer(uintptr(unsafe.Pointer(&value)) +
offset))` — emitted as a read/write of `@string` through `(uintptr)Ꮡvalue + offset`: an interior
address of the movable `ж<T>` heap box plus a **Go ABI** offset, against a CLR object whose field
layout is unrelated to Go's ABI. For any T whose strings sit at nonzero offsets (`testStruct`'s `b`
at +8, `testStringStructArrayStruct`'s `s[1].a` at +16 — the latter landing entirely OUTSIDE the
8-byte `array<T>` ref that is the whole CLR value), the access lands on the box's OWN fields.
- **Pre-window era:** `@string` was one 8-byte reference, so every mislaid store was a single
  aligned pointer-sized slot holding a valid object — type-confusing (part of the "v0 != v1" noise
  the r43e record shows) but nothing the collector trips over.
- **Window era:** `@string` is 16 bytes (`byte[]` + offset + length), so the same store's INTEGER
  tail lands in an adjacent GC-scanned reference slot of the box. The next collection walks a
  garbage pointer and the runtime fail-fasts — and `drainMaps` FORCES that collection via
  `runtime.GC()` in every `TestHandle` subtest, which is why the host died with zero verdicts (or
  hung: same corruption, discovered differently under load).

**Mechanism proven without `unique`:** a ~25-line program against golib alone — `ж<TT>` over
`struct { array<SS> s }` where `SS` is `struct { @string a }`, two `@string` stores at Go offsets
0/+16 through `(ж<@string>)(uintptr)`, then `GC.Collect` — dies with the identical
`0x80131506`-at-`GC.Collect` stack on window-era golib, and on pre-window golib (`7c7bc7d69`)
**survives both collections** and only faults when the program itself reads the type-confused slots
back. The A/B isolates the window as the escalation and proves the writes were corrupting values all
along.

**The fix (this lane): `src/core/unique/clone.cs` is hand-owned** (`[module: go.GoManualConversion]`),
the documented S1 managed-referent remedy. `clone<T>`'s contract — "MAY update string values found in
value with a cloned version" — is a retention optimization, never a semantic requirement, so the
hand-own clones the `T == string` case (right-sized copy via `stringslite.Clone`, no address
arithmetic) and returns aggregates unchanged; the only divergence from Go is retention (an interned
aggregate's strings keep sharing their original backing). `makeCloneSeq` and the builders stay in
their converted form — pure descriptor arithmetic, still covered by `TestMakeCloneSeq`. Post-fix
census on the `c33b3a67e` base: the host runs to completion — **5 of 19 matched** (the five
`TestMakeCloneSeq` passes; r43e recorded 4), 13 fail / 2 infrastructure-error, all on the
pre-existing roots this section already names (the `[GoType]` equality gate's `v0 != v1` rows, the
eface subtest-naming pair, the nil-vs-empty `cloneSeq` DeepEqual rows). `unique` still does not bank;
the REGRESSION row is closed.

**Residual, for the ж-box arc:** the corpus has seven more `(ж<@string>)(uintptr)` sites, all in
converted `runtime` (`map_faststr.cs:487` and `iface.cs:461` are the two WRITE sites; `alg.cs`,
`error.cs`, `arena.cs`×3 read) — dead or near-dead under the managed model's own map/iface, but the
same shape, and any future caller inherits the same two-era hazard. The general
`(uintptr)ж<T>`-for-managed-T transient-address model is unchanged by this fix; that is the ж-box
arc's charter, not this lane's.

### Five converter defects, each with a named mechanism

None of these five is a wall; all are ordinary emission bugs, listed with the evidence a fix needs.

1. ~~**`internal/types/errors` — a Δ-renamed IMPORTED type is spelled with its bare Go name.**~~
   **FIXED 2026-08-13** (`claude/types-errors-delta-rename`). `codes_test.cs` emitted
   `err._<Error>(ᐧ)` (38,42) and `new Info(…)` (65,22) → CS0246 ×2, against `go/types`' `ΔError`/
   `ΔInfo`, while the test's own `package_test_info.cs` already minted `typesꓸError`/`typesꓸInfo`
   and left them unused.

   **The diagnosis above was one step coarse, and the correction is the useful part.** It is not
   "the cross-package path" — the QUALIFIED cross-package spelling was always right
   (`shapelib.Marker{…}` → `new shapelibꓸMarker(…)`, verified directly). The loss is the **bare
   ident**, which only a DOT import produces (`codes_test.go` has `. "go/types"`). Type-DRIVEN
   positions — declaration, parameter, conversion, field — resolve from `types.Type` through
   `getCSharpTypeName`/`getScopeCheckedTypeName`, both of which already consulted
   `foreignAliasedTypeName`; that is why `var mu Mutex` through a dot import has worked since
   `DotImportRenamedPackage`. The two AST-IDENT type positions did not: a type-assertion target and
   a composite-literal type render through `convIdent`'s `isType` arm, which returned the bare
   sanitized Go name and consulted nothing. That arm now routes through the same lookup.
   Guarded by `DotImportRenamedType`; CNR byte-identical across 588 packages.

   **Blast radius, for whoever wonders whether a corpus regen is owed: none.** The only production
   (non-test) dot import in the converted corpus is `go/types` → `internal/types/errors`, and that
   package publishes ZERO collision renames; `generrordocs.go`'s dot import of `go/types` is
   `//go:build ignore` and is not converted. The fix therefore cannot move production `src/core` —
   it reaches `-tests` conversions only.

   **The row did not validate, and its remaining blocker is NOT its own.** With the build blocker
   cleared the host builds and runs; both top-level tests then fail with the `go/types` checker
   nil-panic re-surfacing through `check.cs:430` — Go 155 verdicts, C# **0**. `internal/types/errors`
   is now a second dependent of the unbanked `go/types` row, exactly as `go/internal/gcimporter`'s
   184 rows are. Not banked; test sources and proof page deliberately not committed.
2. **`internal/fuzz` — an alias whose RHS is an anonymous struct does not reach the test compilation.**
   `minimize_test.cs:26` and `worker_test.cs:52` emit
   `Func<struct{Parent string; Path string; Data []byte; …}, error>` — raw **Go** syntax in a C# file —
   producing CS1031/CS1525/CS1003 cascades. Go declares `type CorpusEntry = struct{…}`
   (`fuzz.go:463`), and the production emission handles it correctly by lifting it:
   `fuzz.cs:8` carries `global using CorpusEntry = go.@internal.fuzz_package.CorpusEntryᴛ1;`.
   `global using` is compilation-scoped, and this is a production-reference test project, so the alias
   never crosses into the test assembly — `package_test_info.cs` contains no `CorpusEntry` line at all.
   Remedy shape: re-emit the lifted alias into the test compilation, or spell `CorpusEntryᴛ1`.

   ✅ **FIXED (`79f2b5e62`, converter) and the package is now BANKED 52/52 (2026-08-14).** Clearing
   the build did not validate it: the host then died before any test on
   `flag provided but not defined: -json`, because `worker_test.go`'s `TestMain` calls `flag.Parse()`
   and nothing had ever declared the host's own command line on the converted `flag.CommandLine`.
   That second blocker is a whole CLASS, remedied host-only by `src/core/testing/TestFlagBridge.cs`
   — see the flag-bridge entry above for the class census, the measurements that ruled out a
   `testing` → `flag` project reference, and where the other three members stand.
3. **`net/rpc/jsonrpc` — promotion from embedded POINTER fields is invisible to `ImplementGenerator`.**
   `all_test.go:310` declares `type pipe struct { *io.PipeReader; *io.PipeWriter }`, whose
   `Read`/`Write`/`Close` come entirely by promotion. The generated
   `…pipe-global__go.io_package.ReadWriteCloser-ptr.g.cs` calls `.Read`/`.Write` on `ж<pipe>`, finds no
   member, and binds an unrelated extension method instead — CS1929 naming
   `io_package.Read(ref io_package.LimitedReader, slice<byte>)` and
   `io_package.Write(ref io_package.OffsetWriter, …)`. An error message pointing at `LimitedReader`
   from a jsonrpc test is the giveaway that this is an overload-resolution fallthrough, not a
   missing reference.

   ✅ **FIXED 2026-08-12 (`ImplementGenerator` + `InterfaceImplTemplate`, generators only — zero
   converter-emission change).** The hop forwarding was gated to a struct with exactly ONE embedded
   pointer; with several, no hop was named and every promoted member fell through to the bare
   `m_box.M(…)` / `this.M(…)` receiver. It now indexes the hop path **per member**, routing each to
   the UNIQUE embed declaring it — Go's depth-1 promotion rule, with a name two embeds declare
   dropped rather than guessed (which is why `*pipe.Close`, declared on the struct, still wins over
   the `Close` both halves of the pipe declare). Each embed's method set is read from local syntax
   where it is declared here and from METADATA where it is not, which is the whole jsonrpc case.
   Guarded by the `MultiPointerEmbedPromotion` behavioral test; detail in
   [`ConversionStrategies-Reference.md`](../ConversionStrategies-Reference.md#with-several-embedded-pointers-the-hop-is-chosen-per-member-not-per-struct).

   ⚠ **This row is NOT socket-walled, contrary to the batch's expectation.** With the host building,
   the package RUNS end to end: **9 Go verdicts, 6 C# matched, 3 failed**, no timeout and no netpoll
   involvement — because jsonrpc's own tests never open a socket. They talk over the in-memory
   `pipe` above (`myPipe()` from two `io.Pipe()`s, "Copied from package net"), which is precisely why
   that type exists in the file. Being under `net/rpc` is not the same as reaching the kernel; the
   walled-list membership was inherited from the parent path, not measured. The **three remaining
   failures share one root and it is a json defect, not an rpc one** — `TestBuiltinTypes`,
   `TestClient` and `TestServer` all report `json: cannot unmarshal array into Go value of type
   [1]interface {}`, i.e. `encoding/json` cannot unmarshal a JSON array into a Go **fixed-size
   array** type (jsonrpc passes params as `[1]any`). The other six pass: `TestMalformedInput`,
   `TestMalformedOutput`, `TestServerEmptyMessage`, `TestServerErrorHasNullResult`,
   `TestServerNoParams`, `TestUnexpectedError`. Nothing banked and no roster change — the row moves
   from "build-blocked" to "one named json root from a bank", and that root is worth checking against
   `encoding/json`'s own suite before anyone spends a lane on it here.

   **UPDATE 2026-08-13 — the json root is fixed, and it was WIDER than recorded here; jsonrpc now
   stands on a second, unrelated root.** The defect was not in the reflection bridge or in `array<T>`
   at all: it was the converter's `switch` lowering for a `default` clause that Go places **before**
   some of its cases (`claude/json-fixed-array-unmarshal`, guarded by `JsonFixedArrayUnmarshal`; full
   mechanism in `ConversionStrategies-Reference.md`). Scope correction: converted `encoding/json`
   could not decode a JSON array into **any** target except a bare `interface{}` — every `[N]T` *and*
   every `[]T` took the error arm, not only fixed-size arrays. The same converter bug independently
   made `internal/bisect` reject every pattern it was given.
   jsonrpc's 3 rows are unchanged in NAME but not in cause: `TestBuiltinTypes`, `TestClient` and
   `TestServer` now get past json and panic **`index out of range [0] with length 0`**. Two things
   make this worth a fresh lane rather than a footnote: (1) the panic escapes on a goroutine and
   **takes the host process down**, so the package now records **0** verdicts where it used to record
   6 — a full-suite run of this package reads worse than before even though the corpus is strictly
   better; run it filtered (`-test.run` over the other six) to see the 6 passes. (2) The obvious
   suspect is ruled OUT: a struct-typed field holding a `[1]any`, left at its zero value by a keyed
   composite literal that omits it, was probed directly (`codec{name:…, id:…}` → `c.req.Params[0] = …`,
   plus the bare-`var` and local-`var` shapes) and **matches Go exactly** — the generated constructor
   chain does run the `= new(1)` field initializer. No stack trace is available from the host: it
   prints `panic: {message}` with an empty `StackTrace`, which is itself worth fixing, because a
   goroutine panic with no frame is the hardest possible diagnostic to act on.

   ✅ **CLOSED 2026-08-14 (`claude/jsonrpc-goroutine-panic`) — the package VALIDATES 9/9 and is
   BANKED.** Two landings, and the first one is what found the second.

   **(a) The host no longer swallows a goroutine panic's traceback.** The frameless report was not
   a missing `catch` — it was golib's AppDomain backstop doing exactly its job: it prints the panic
   VALUE and exits 2, which is Go's own report for a *program* and useless to a *host* running many
   Go programs in one process. `Goroutine.ObserveUnhandledPanic` now lets a host WATCH a panic cross
   a goroutine root from an exception FILTER that always declines — so the fatal path stays
   byte-identical (Go fidelity, and the oracle keeps observing it) while the report is written with
   the stack still standing. The converted-test host installs one: it attributes the panic to the
   test whose goroutine it was, reports it as that test's terminal FAIL **with the full traceback**,
   and FLUSHES the result files the fatal path used to discard whole. A goroutine panic now costs
   the tail of a run instead of all of it. Guards: `GolibTests/GoroutineRootPanicTests` (5 tests over
   the root's whole policy — observed-and-still-escapes, the fault site surviving, a runtime-error
   panic, containment still taking non-panic exceptions, Goexit taking neither).

   **(b) With the trace liberated, the panic rooted in one read — and it was ONE test, not three.**
   The trace named `array.get_Item` under `all_test.cs`'s own `BuiltinTypes.Array(i int, reply
   *[1]int)`, called through `reflect.Call` from `net/rpc`'s `service.call`. net/rpc allocates every
   reply from the method type alone — `reflect.New(mtype.ReplyType.Elem())` — and the `[1]int`'s
   LENGTH did not survive the trip, so `New` built a zero-length array and the callee's first write
   panicked. Two hops were missing, both now closed: a METHOD's func type is built from the method
   TABLE and never passes through a delegate, so `GoReflect.MethodParamDims` reads the
   `[GoArrayDims]` stamps off the `MethodInfo` directly; and the array sits behind a POINTER, so the
   converter stamps a parameter's POINTEE dims and a pointer descriptor's dims pass through `Elem()`
   unshifted. The converter half had to go into `visitFuncDecl`'s REBUILT signature path, which is
   the one a `*[N]T` parameter always takes — having a pointer parameter is itself what triggers the
   rebuild — and that same gap had been silently dropping the stamp from VALUE array parameters in
   heap-boxing functions too. Full mechanism:
   [`ConversionStrategies-Reference.md`](../ConversionStrategies-Reference.md); guarded by the
   extended `ReflectFuncArrayParamDims` behavioral test (rpc's shape in miniature) and by
   `TestGoArrayDimsAttribute`.

   **The "three failing tests" were one.** `TestClient` and `TestServer` never failed on this root at
   all — `TestBuiltinTypes` sorts first, its panic killed the host, and the other eight recorded no
   verdict. That is precisely the misreading (a) exists to prevent, and it is worth carrying as a
   rule: **a package that records 0 verdicts has one failure and an unknown remainder, not N
   failures.** Corpus footprint of the converter half, over all 592 behavioral packages: 5
   declarations in 5 files, one line each. Gates: behavioral suite 566/566 + 540/540 output, CNR
   classified, GolibTests 102/102, converter `go test ./...`, filtered sweep `PASS net/rpc/jsonrpc 9`.
4. **`testing/fstest` — a defined type over ANOTHER package's named map type gets a one-hop conversion.**
   Go has `type shuffledFS MapFS` where `MapFS map[string]*MapFile`. The emission declares
   `[GoType("global::go.testing.fstest_package.MapFS")] internal partial struct shuffledFS;` and then
   writes `((MapFS)(map<@string, ж<MapFile>>)fsys).Open(name)` — CS0030 at (62,62), because the
   generated conversion offers `shuffledFS`→`MapFS` but not `shuffledFS`→ the raw map the intermediate
   cast asks for.
5. **`internal/syscall/windows/registry` — the internal-test partial class is emitted non-`static`.**
   `export_test.cs:9` opens `partial class registry_internal_test_package {` and then declares
   `internal static error SetValue(this …registry_package.Key k, …)` → CS1106. Banked packages get away
   with the identical non-static opener only **incidentally**: `sort`/`bytes`/`strings` each have a
   go2cs-gen `RecvGenerator` file that re-declares the class as `public static partial`. Registry's
   generated set covers `registry_package` and `registry_test_package` only, so nothing supplies the
   modifier. The emitter should not be relying on a generator for it.

### `embed/internal/embedtest` should be struck from the candidate list

CS0234: `embedtest_package` does not exist in `go.embed.@internal`. It never will —
`$GOROOT/src/embed/internal/embedtest` contains `embed_test.go`, `embedx_test.go`, `testdata/` and
`concurrency.txt` and **no non-test source**. There is no production package, hence no production
`.csproj` in `src/core`, and the `-tests` emitter nonetheless writes
`global using static global::go.embed.@internal.embedtest_package;` plus a production project
reference. The tests here exercise `embed`, not a package of their own. Two follow-ups, both small:
teach the emitter the test-only-package shape (no production reference, no `_package` using), and
route the coverage to `embed` where it belongs. Until then this row is not a measure of anything.

## ж-box arc stage A3 — the pinned measure: P256 lands at 8,528, under the wall (lane L3, 2026-08-13)

The §9 A3 row of [`DESIGN-zh-box-reduction.md`](DESIGN-zh-box-reduction.md), run whole on the pinned
laptop R (Ryzen 7 PRO 6850U, 32 GB, go1.23.1 exactly; ambient load one idle two-day-old MSBuild
node — effectively solo): the real pipeline on nistec and math/big, the r56d-method probe for the
unit targets, the two A2-owed confirmations from the 2026-08-13 sweep's evidence, and the perf suite
with the AOT column. The measured numbers below **supersede the r58a four-curve table and the
r58b/r60 math/big alloc-row measurements**; §7's table carries the same numbers in its A3 column.

**The verdict measure.** `-tests -test-action all -test-timeout 30m` on `crypto/internal/nistec`
(explicit `-go2cspath`, ~8 min wall): terminal results **2,195 matched / 5 divergent of 2,200** —
the identical arithmetic to the standing measure, the five being exactly the want-zero
`TestAllocations` rows. The counter's numbers, against Go's want of 0:

| Curve | obj/run (was, r58a) | B/run |
|:--|--:|--:|
| P224 | **8,484** (264,540) | 746,678 |
| P256 | **8,528** (242,665) | 733,766 |
| P384 | **12,572** (471,424) | 1,198,070 |
| P521 | **17,090** (870,534) | 1,867,811 |

**P256 8,528 ≤ 10,000: the §7 acceptance is MET on the recommended branch** — −96.5 % from the
bill, and BELOW A2's approximate mirror (10,105 on the artifact-laden host; L11's Once/tmpstring/
const-hoist landings plus the clean pinned host account for the direction). The §3.6 branch
question stays closed; nothing re-opens.

**The residual decomposes to the named classes and nothing else.** The r56d-method probe (a
scratchpad console probe over the converted fiat/nistec + the golib `AllocationCounter`; temporary
by construction, reverted with the session) reproduces the pipeline at **8,524 obj/run — Δ4 obj /
466 B, the rand.Read substitution, the same gap shape r56d recorded** (positive control passed).
Phase decomposition of the P256 body:

| Phase | obj/run | B/run |
|:--|--:|--:|
| `NewP256Point().SetGenerator()` | 31 | 2,970 |
| `make([]byte, 32)` + fill | 1 | 56 |
| `ScalarBaseMult` | 1,871 | 160,652 |
| `ScalarMult` | 6,485 | 558,077 |
| `Bytes` | 25 | 2,128 |
| `NewP256Point().SetBytes(out)` | 48 | 4,152 |
| `BytesCompressed` | 25 | 2,096 |
| `SetBytes(compressed)` | 38 | 3,168 |
| **whole-window control** | **8,524** | **733,299** |

98.0 % of the residual sits in ScalarMult + ScalarBaseMult — the `new(fiat.P256Element)` /
`NewP256Point` temporaries of point Add/Double/table construction (**class 3b**) and their
lazily-materialized `array<uint64>` backings (**class 4**), the two Phase-C classes. The remaining
~168 close **to the object** at the element level: fiat `SetBytes` = 12 exactly (3 × 3b
`minusOneEncoding` news + 5 backings + `in`'s kept box 2 + `Bytes`-chain `out` kept box 2), fiat
`Bytes` = 3 exactly (`out` keep 2 + tmp backing 1) — the §6.3/A1-named wrapper keeps, small and
named as priced. **Classes 1/2/3a measure ZERO**: the fiat five unit targets
(`Mul`/`Add`/`Sub`/`Square`/`Select`, P224 and the P256 SetBytes/Bytes twins) all read **0 B/op,
0 obj/op** (were 960 / 960 / 528 / 832 / ~1,344).

**Census note, attributed — NOT the A2 host artifact.** go test additionally reports
`TestP256OrdInverse` and `TestP256PrecomputedTable` (`(amd64||arm64) && !purego` files: the real
toolchain exercises its asm implementation's tests; the converted purego suite doesn't declare
them). The comparison's census gate records and excludes them — compared maps have go-only = 0 and
the terminal arithmetic is identical to the standing 2,195/2,200. Zero `go.std.*` namespaces
anywhere in the run: the A2 std/-prefix artifact did **not** recur on the repinned GOROOT.

**math/big re-measured through the pipeline: 224/226, both alloc rows unmoved.**
`TestMulUnbalanced` reads **20,499,128 B vs the 51× budget of 20,416,320** (51.21× on 400,320 B of
inputs; r58b read 20,487,208 — +0.06 %, noise), and `TestNewIntAllocs` reads **exactly 1 obj/run on
all seven `NewInt` shapes** (want 0 — class 3b, the escape-analysis elision, Phase-C). As §3.6
forecast: `nat`'s traffic is slice-backed real allocation on both sides; Phase A promised these
rows nothing and moved them nothing.

**os probe FINDING.** `os.File.WriteString` measures **2,368 B/op (17 golib objects)** against
§7's "3,168 unchanged (±0)" claim — moved **−800 B**, favorably. The 3,168 stamp is r39-era and
predates r41's inline-defer retirement of that decomposition's 440 B GoFunc/defer term plus
everything since, so the claim's baseline was stale when written; the per-term re-attribution of
the −800 belongs to the next os re-instrumentation (AllocMark), not to this stage's arithmetic.

**The two A2-owed validation items, confirmed from the 2026-08-13 sweep's evidence:**

- **§3.5 func-value adapter — covered, with one precision.** `internal/profile` (swept clean, its
  1 verdict `TestPackedEncoding` pass/pass) carries the boxed-shape adapter lambdas in its
  white-box decoder table — `(ж<buffer> b, message m) => decodeUint64s(ref (b).DerefOrNull(), …)`
  over the LOWERED `decodeUint64s(ref buffer b, ж<slice<uint64>> Ꮡx)` (`proto_test.cs:70-71` /
  `proto.cs:344`) — and those sources were refreshed BY the sweep-aftermath bank itself
  (`e4d9faa71`, `proto_test.cs` among the eleven), so the sweep validated with the adapters in
  play and exercised (the decode path runs through the table). The precision: the corpus instance
  is a func-LITERAL table in `proto_test.go`, not an `export_test.go` bare alias — **no swept
  export_test.go aliases a lowered function** (measured: zero adapter shapes in committed
  `export_test.cs` corpus-wide); that exact spelling is guarded at the converter level
  (`refLoweringAnalysis_test.go`'s §3.5 classification-equality guard, with positive control) and
  behaviorally (`RefLoweredParams`' func-value X5 arm). Not a gap — the mechanism is swept, the
  spelling is unit-guarded — but stated so nobody reads "export_test.go" as sweep-evidenced.
- **io canary — still exactly 1.** `io` swept clean at 60 matched / 1 disclosed (its roster row
  untouched by the aftermath commits, i.e. counts held), `TestMultiWriter_WriteStringSingleAlloc`
  among the matched (pass|pass, proof page `io.md`); the assert is want-EXACTLY-one, so the pass IS
  the "still exactly 1" claim.

**The perf suite — measured, AOT included, on the dedicated machine (2026-08-13; closes the §9 A3
row's last obligation via `HANDOFF-l3-a3-perf.md`).** The work laptop's stand-down transferred the
run to a solo, sleep-proofed machine: AMD Ryzen 5 PRO 6650U (6C/12T, 30.8 GB), Windows 11
10.0.26200, **pinned go1.23.1 (gate zero verified)**, .NET SDK 9.0.316, MSVC 14.44 `link.exe`
present — the AOT column is measured, not owed. Full `run-performance.ps1 --update-readme`
(default 5-run medians, `MSBUILDDISABLENODEREUSE=1`): **Verify passed 14/14** — identical
timing-filtered stdout across Go binary / C# JIT / C# Native AOT, the gate Measure sits behind —
total 14,171 s wall, 13,552 s (95.6 %) of it the fourteen sequential ILC publishes. The table
banks in `src/tests/Performance/README.md` (PERF-RESULTS; mirrored to `docs/Performance.md`), the
first to carry the ж-bound **RefLower** row: Go 226.3 ms · JIT 660.6 ms (2.92×) · AOT 1,827.8 ms
(8.08×). The row is the standing README record, not the arc's gate — the gate was A2's paired
same-machine A/B per §7's protocol, and cross-table comparison against the replaced i7-5820K
table (2026-08-11, 13 rows, same toolchain) is machine effect by construction. The §7-item-4
recording, from the run's ILC `ok (NNNs)` lines and the published
`Perf*\bin\Release\aot\<proj>.exe` sizes — every publish succeeded **first-try** (the work
laptop's exit-1-then-self-heal quirk did not recur, so no retry-inflated time exists anywhere),
each exe carrying the full converted-stdlib closure:

| Benchmark | ILC wall (s) | AOT publish size (MB) |
|:--|--:|--:|
| PerfStartup | 981 | 296.4 |
| PerfFib | 1,085 | 296.4 |
| PerfSieve | 1,094 | 296.4 |
| PerfMatMul | 1,081 | 296.9 |
| PerfString | 972 | 296.4 |
| PerfStringView | 929 | 296.4 |
| PerfStringMatch | 955 | 302.3 |
| PerfMap | 933 | 296.4 |
| PerfSort | 909 | 297.0 |
| PerfChannel | 923 | 296.4 |
| PerfIfaceCall | 936 | 296.4 |
| PerfIface | 921 | 296.5 |
| PerfIfaceShell | 908 | 296.9 |
| PerfRefLower | 925 | 296.4 |

Two run notes for the record: the JIT one-shot batch build reported errors that per-project
attribution resolved to **0 failed** — the known parallel-build race, exactly as the handoff
predicted, not a corpus defect. And an observation the A′/B′ checkpoint should see: on this
machine AOT trails JIT on the compute-bound rows (RefLower 8.08× vs 2.92×, MatMul 5.02× vs
1.66×, Sieve 3.28× vs 1.61×) while beating it on Startup and Map — the ref-heavy hot loop is
the widest such gap, so ILC codegen of the lowered emission is worth a look when B′'s
dual-emission increment prices its own AOT bill.

## Scout batch 2 — the never-measured/stale tail, 50 packages re-run; 3 bank, 8 roots MOVED (2026-08-14, lane B)

The derivation first, because the denominator is the part boards get wrong. **305** package
directories under `src/core` carry a production `.csproj`; **216** of them have a `func Test` in
their Go 1.23.1 GOROOT sources; subtracting hand-owned `testing` (skip-listed, never queued)
reconciles that to the roster header's **215** exactly, and 215 − 136 banked = **79** unbanked.
Of the 79, five have *no eligible `Test` declaration on windows/amd64* (`internal/runtime/syscall`,
`internal/syscall/unix`, `net/internal/socktest`, `log/syslog`, `runtime/race`) and `os/user`
cannot bank at all (Go's own `TestGroupIds` fails in the oracle); the named walls (netpoll sockets,
scheduler-walled `internal/singleflight`, `go/types`, synthetic-GOROOT `go/build`) and the censuses
taken since 2026-08-09 were left to their owners. **Fifty** packages were run end to end through
`-tests -test-action all`.

**Three bank, forty-six are rooted, and one is NOT MEASURED.** Roster **136 → 139 (63.3% →
64.7%), 15,137 → 15,181 matching verdicts, 47 disclosed (unchanged).**

### The three

`crypto/internal/hpke` (19) · `crypto/internal/edwards25519/field` (16) · `debug/gosym` (9)

None needed a change of any kind — each is a capability that landed in the preceding week finally
being *looked at*, which is r43c's lesson executed for the third time. `crypto/internal/hpke` was
held by the JSON-into-a-lifted-anonymous-struct root (`claude/json-fixed-array-unmarshal`);
`crypto/internal/edwards25519/field` by the `array<T>` unshaped-instance class it was recorded as
"producer (3)" of; `debug/gosym`'s `TestPCLine` by the converted host's working directory — it
shells out to the real Go toolchain to build `testdata/`, so it closed when the host learned to
reproduce a package's directory **ancestry** (`40beb776e`), not merely its shape.

### ⚠ The measurement hazard this pass paid for twice — MSB4006 is NOT a build root

r44a's trap #2 recorded that a failed run's leftover `<pkg>.tests.csproj` can produce
`NuGet.targets(1311,5): error MSB4006: circular dependency … "_GenerateRestoreProjectPathWalk"`.
This pass hit a **second, sharper form of the same hazard, and the poisoned file is TRACKED**: a
`-tests` run rewrites the production `.csproj` of packages in its closure, and the `runtime` run
rewrites `src/core/runtime/runtime.csproj`'s windows-conditional `ItemGroup` to add
`internal/syscall/windows` — which references `syscall`, which references `runtime`. That is a real
project-path cycle, and *every package whose closure reaches `runtime` then fails to restore*. Four
packages (`runtime`, `crypto/tls`, `go/internal/gcimporter`, `os`) were first recorded as
"conversion-blocked" this way, which reads exactly like a build root and is not one.

Two rules follow, and the second cost more than the first:

1. **Restore tracked `.csproj` drift between pipeline runs**, not just untracked artifacts, then
   re-measure. Proof: with `runtime.csproj` at HEAD and no prior `runtime` run, `crypto/tls`
   compiles and reaches its own diagnostic in 337 s — no cycle. All four were re-measured this way
   and the three that could be are reported below on their real roots.
2. **Do NOT "clean" by deleting `*.tests.csproj` wholesale.** 136 of them are *committed source*
   under the validated-package commit policy. An over-broad prophylactic delete wiped every banked
   package's test project in one command; `git checkout -- src/core` restores it, but a lane that
   did not notice would have banked the deletion. Scope such a clean to `git clean`, which by
   construction cannot touch tracked files — never to a `Get-ChildItem | Remove-Item` sweep.

`runtime` itself remains **NOT MEASURED**: its own `-tests` run is what writes the cycle, so the
package cannot be measured without first fixing that emission. That is the finding, and it is a
converter defect — a test-closure-only reference reaching a **production** `.csproj`.

### Eight roots MOVED — the board's recorded first diagnostic is stale for these

| Package | Census | Board had | Measured 2026-08-14 |
|:--|:--:|:--|:--|
| `archive/tar` | ~~0 of 97~~ **97 of 97, BANKED** | CS1537 ×3, duplicate `using` alias in one file | **CS0111** — `tar_internal_test_package.fileOps` already defines `Append`. The alias-dedupe root is CLOSED; behind it is the same defect as `fmt` — ⚠ **and that "CLOSED" was wrong**: CS1537 was still live and became this package's next wall once CS0111 fell. See *CLOSED for `archive/tar`* below (2026-08-15) |
| `fmt` | ~~0 of 63~~ **62 of 63** | CS0111 `fmt_test_package.SE` already defines `Append` | **COMPILE WALL CLOSED** (2026-08-16, lane `claude/fmt-roots`) — two converter roots, not four; the host builds and 62 verdicts agree. `TestSprintf` is the one open verdict, behind three reflect/fmt-bridge divergences. See *`fmt`'s compile wall is CLOSED* below |
| `sync/atomic` | 0 of 108 | CS0103 `ᏑᏑX`, double address-prefix | **CS0841** — cannot use local `magic64` before it is declared. The double-`Ꮡ` is closed |
| `internal/reflectlite` | 0 of 30 | CS0016 could not write to output file | **CS0715** — static classes cannot contain user-defined operators: a generic test-local type's `==`/`!=` emitted into the static `reflectlite_test_package` |
| `runtime/debug` | 0 of 9 | CS0264, not taken past the first diagnostic | **CS0264 + CS0715** — the same static-class-operator defect as `internal/reflectlite`. Two packages, one root |
| `database/sql` | 0 of 25 | CS0029 | **CS0121** ambiguous `stubDriverStmt(NilType)` constructor + **CS0117** `sql_package` has no `Δtable` |
| `crypto/x509` | 0 of 335 | CS0102 duplicate definition in `x509_package` | **CS0234** `cryptobyte_package` / **CS0246** `hash_package` unresolved *in the production files* under the `-tests` closure |
| `internal/platform` | 0 of 1 | JSON array into a slice of a lifted anonymous struct | **panic: nil dereference** in `text/template`'s `errRecover` — `TestGenerated` renders its expectation through a template. The lift question may still sit behind it, but it is no longer the failing edge |

### `crypto/tls` — 0 of 3,519, and the `vendor` root is in GENERATED code

The recorded root ("CS0234 `'vendor' does not exist` — the test half's vendored import") is right
about the token and wrong about the file. The errors are **CS0246 `vendor` could not be found** in
`go2cs-gen` RecvGenerator output — e.g.
`go.crypto.tls_package.NonceSize.global__go.crypto.tls_package.prefixNonceAEAD.g.cs(17,7)`. The
generator emits a receiver overload whose parameter type names the GOROOT-vendored root namespace,
so this is an emission question in the analyzer, not an import list. It is the largest verdict count
on the unbanked list by a factor of six.

### Re-baselines this pass owes the board

- **`go/internal/gcimporter`: 399 → 475 of 583.** The L9 laptop-G census was taken with the
  `go/types` checker nil-panic live; `25108403e` (2026-08-13) closed it and moved **76 rows** with
  no work aimed at this package. The residual is **108 rows, all `TestImportTypeparamTests/*`** —
  still the type-parameter class, so the wall is narrower, not gone. 1,391 s to measure.
- **⚠ `os`: 681 of 683 (board) → 31 of 679 (measured), and this is a REGRESSION to bisect.** The
  converted host dies with **exit status `0xc0000005` (access violation)** after recording 32
  verdicts, the last four being `TestLookupEnv`, `TestMkdirAllAtSlash`,
  `TestMkdirAllExtendedLengthAtRoot`, `TestMkdirAllVolumeNameAtRoot`. Reproduced on a clean,
  cycle-free re-measure, so it is not the MSB4006 artifact above. Per the jsonrpc rule, **a package
  that dies mid-run has one failure and an unknown remainder, not 648** — the 31 is a floor, not a
  census. Flagged for a bisect lane; not this lane's to root.
  **⚠ RETRACTED 2026-08-14 — not a regression.** The bisect lane reproduced the identical fault at
  the r39-osalloc commit that recorded the 681, and rooted it in one test whose syscall is reachable
  only on a host with the Server service running. The flag was right to raise it and right about the
  floor; the diagnosis "REGRESSION" is withdrawn. See *`os`'s "REGRESSION" is a HOST CAPABILITY, and
  the killer is `SHARE_INFO_2`* at the end of this file.
- **`log`: the `AllocsPerRun` shim now reports a COUNT here.** `TestDiscard` measures
  **300 go2cs-runtime object allocations over 100 runs = 3 objects/run** against Go's want-zero
  (the board's fourth-member row read "bytes"). `net/http/internal`'s `TestChunkReaderAllocs`
  reproduces r58a exactly at **2 objects/run against a budget of 1**. `log/slog/internal/buffer`
  is unchanged — still **bytes** (1,520 B over 5 runs), so r58a's "counter saw none" stands for
  that one. `log` is 7 of 9; `TestAll` is its second, separate row.
- **`log/slog`: 153 of 213 matched.** Recorded differently by r44b (185 pass / 28 fail / 1 crash),
  which counted the C# side rather than agreement with Go; the two are not comparable and this row
  is the differential. Not re-attributed here.

### Reproduced verbatim — no re-measure owed until a relevant capability lands

`go/doc` 24 of 85 · `encoding/gob` 99 of 106 · `crypto/cipher` 13 of 14 · `debug/pe` 9 of 10 ·
`html` 2 of 3 · `internal/trace/internal/oldtrace` 2 of 3 · `internal/weak` 1 of 3 ·
`log/slog/internal/buffer` 1 of 2 · `iter` 0 of 28 (`newcoro`/`coroswitch` stubs) ·
`runtime/trace` 0 of 2 (`getg` stub) · `internal/concurrent` 0 of 20 (CS0426 `node<,>`) ·
`debug/elf` 0 of 31 (CS8183 at `file_test.cs(1195,5)`) · `os/exec` 0 of 22 (CS0103 `var`) ·
`text/template` 0 of 52 and `html/template` 0 of 243 (CS0030 `S`→`I`) · `slices` 0 of 122
(CS0305/CS0411) · `encoding/xml` 0 of 386 (CS0426 `ΔToken`) · ~~`encoding/json` 0 of 491
(CS0050/CS0053)~~ — **the compile wall is CLOSED end to end and the package RUNS: 400 of 491
matching, 91 divergent in four named roots (2026-08-16)** · `net/netip` 0 of 266 (CS1002/CS1525) · `internal/trace` 0 of 92 and
`runtime/pprof` 0 of 174 (CS0149 `Method name expected`; pprof also still CS0103 `ᏑᏑsalts`) ·
`internal/runtime/atomic` 0 of 15 (CS0103 `ᏑᏑx`) · `flag` 0 of 24 (CS1929 on
`ж<flag_test_package.URLValue>`) · `crypto/ed25519` 0 of 9 (CS0030 `PrivateKey`→`crypto.Signer`) ·
`crypto/internal/mlkem768` 0 of 11 (CS0315, plus CS0841) · `reflect` — the converter itself still
fails on `all_test.go` (`1e+06 not an Int`).

### Four packages measured for the FIRST time — no prior board row of any kind

| Package | Census | Root |
|:--|:--:|:--|
| `net/http/pprof` | 5 of 15 | `TestHandlers` fails and seven of its subtests infrastructure-error (`/debug/pprof/{heap,mutex,trace,profile,block,goroutine}`); `TestDeltaProfile` skips where Go passes. Profile collection has no managed body — sibling of `runtime/pprof`'s and `runtime/trace`'s stubs |
| `internal/godebug` | 3 of 5 | `TestCmdBisect` and `TestMetrics` fail; `TestBisectTestCase` and the rest pass. The package is fully hand-owned, so this is the hand-own's own contract being measured for the first time |
| `internal/syscall/windows` | 0 of 2 | `TestRunAtLowIntegrity`, `TestSupportUnixSocket` — both fail. This is also the package whose `-tests` run participates in the MSB4006 cycle above |
| `crypto/internal/boring/bcache` | 0 of 1 | `NotImplementedException: registerCache: external (assembly or cgo) function is not implemented` — a `PartialStubGenerator` stub reached from `Register[K,V]`. The board had only "first divergent verdict recorded; not root-attributed" |

`net/mail` gets its first real census too — **7 of 11**, the four `TestAddress*` rows
infrastructure-erroring on `System.ArgumentException: Indices low, high and max represent a range
outside bounds of the array reference` — where the board had only "first divergent verdict
recorded". And `go/internal/srcimporter` (0 of 7) fails before any test with
`flag provided but not defined: -json`, i.e. the process the host launches is not the go2cs test
host; that is an infrastructure root, not the recorded build block.
⚠ **The last clause of that diagnosis is CORRECTED (2026-08-14, lane `claude/defect-batch-1`).**
The process IS the go2cs test host. `internal/fuzz` reaches the identical symptom once its build
blocker is cleared, and there the cause is exact: `worker_test.go`'s `TestMain` calls
`flag.Parse()`, and the converted `flag.CommandLine` has never been told about the host's own
`--json` / `--result` / `--junit` / `-timeout` arguments, so parsing rejects them before any test
runs. In Go, `testing.M` registers those flags on `flag.CommandLine` before `TestMain` executes,
which is what makes the same `flag.Parse()` legal there. The remedy belongs to the hand-owned
`src/core/testing` host — register its flags on the converted `flag.CommandLine` — and is a
separate, unclaimed item. Every package whose `TestMain` calls `flag.Parse()` sits behind it.

✅ **REMEDIED 2026-08-14 (lane `claude/testmain-flag-bridge`) — `src/core/testing/TestFlagBridge.cs`,
host-only, zero converter change.** The host now DECLARES its own command line on the converted
`flag.CommandLine` before it invokes a converted `TestMain`, which is precisely what
`testing.Init()` does for `-test.*` and precisely what was missing. Four things the implementation
had to get right, each measured rather than assumed:

1. **The whole `-test.*` set is registered, not just the spellings the host was given** — with this
   run's real values (`test.run` ← `-run`, `test.parallel` ← `-parallel`, `test.v`, `test.short`,
   `test.count`, `test.timeout`, `test.shuffle`; the rest at Go's defaults), because converted tests
   READ them: `os/exec`'s `TestMain` gates on `flag.Lookup("test.run").Value.String() == ""` and
   `flag.Lookup("test.list")`, and `runtime`'s gdb tests do
   `flag.Lookup("test.parallel").Value.(flag.Getter).Get().(int)`. Registering only what appeared on
   the command line would have traded a parse error for a nil dereference. For the same reason the
   TYPED registrars are used and not `flag.Func`/`BoolFunc`, whose `funcValue` has an empty
   `String()` and is not a `Getter`.
2. **The `flag` package is bound LATE, by name, not by project reference.** Go's `testing` imports
   `flag`, so the reference is the obvious mirror — and it was tried and MEASURED and it does not
   work here. The generated test csproj sets `DisableTransitiveProjectReferences=true` (load-bearing:
   CS0576 against the emitted `using` aliases), so a `testing` → `flag` reference does **not** deploy
   `flag.dll` beside the 124 of 141 test hosts whose own package does not import `flag` — an
   unconditional use would `FileNotFoundException` every one of them. It also cost every test
   project's build **+33%** (unicode/utf8: 7.5 s warm → 10.2 s). Late binding is the accurate
   statement of the dependency, not a dodge: the converted `flag` package is in a test compilation
   **iff** the package under test imports it, which is exactly when a converted `flag.Parse()` is
   reachable and `flag.CommandLine` observable at all. Only the `flag_package` TYPE is resolved by
   name; every argument type (`@string`, `nint`, `nuint`, `time.Duration`) is a golib/`time` type
   the host already references.
3. **A name the test package already defined is skipped** — the converted `FlagSet.Var` PANICS on
   redefinition, and the host must not turn a package's own flag into a crash. Only the host's
   unprefixed spellings can collide (Go's `test.` prefix exists to make collision impossible), and
   across all of GOROOT's non-`cmd` test sources exactly one such definition exists (`-v`, in
   `cmd/compile/internal/ssa`, which is not converted).
4. **Scope boundary, deliberate:** Go's `M.Run` also calls `flag.Parse()` when it is not yet parsed.
   That is NOT mirrored — no class member needs it (every one's `TestMain` parses explicitly), and
   an unconditional parse would newly reach `ExitOnError`/`os.Exit(2)` for the sixteen banked
   packages that merely reference `flag`. Registration alone is the minimal change that closes the
   class.

**Class census (GOROOT non-`cmd` test sources calling `flag.Parse()`, per package):** `internal/fuzz`,
`go/internal/srcimporter`, `os/exec`, `crypto/tls` — four members. (`runtime` and `syscall` match a
naive grep and are NOT members: `runtime/runtime-gdb_unix_test.go`'s two hits are inside a Go source
STRING literal for a helper program and the file is unix-only; `syscall/syscall_unix_test.go`'s is in
a unix-only re-exec helper. `flag/example_test.go` and `database/sql/example_cli_test.go` are Example
bodies.) Where the four stand after the bridge:

| Package | Before | After | Note |
|:--|:--:|:--:|:--|
| `internal/fuzz` | 0 (died at `flag.Parse`) | **BANKED 52/52** | the 141st roster row; its `TestMain` now parses the host's command line |
| `go/internal/srcimporter` | 0 of 7 (died at `flag.Parse`) | **5 of 7** | not banked; the two failures share ONE root and it is not this class — see below |
| `os/exec` | build-blocked | **builds; 22 of 22 match** | the unnamed-variadic block is FIXED (lane `claude/unnamed-variadic`, 2026-08-14) — 40 further tests are excluded for want of `TB.*`, see that section. That exclusion is CLOSED too (lane `claude/tb-surface`, same day): the 22 became **101 rows, 48 agreeing**, and what was behind it was two HOST roots — see the `TB.*` section at the end of this file |
| `crypto/tls` | not measured | **builds; blocked at RUN** | reached 2026-08-14 by the same lane; two roots, neither this one. Both closed 2026-08-15 (lane `claude/crypto-tls`) along with a third behind them — the package now compiles and its host reaches the flag bridge, where a different wall stops it: see the section at the end of this file |

**`go/internal/srcimporter` — 5 of 7, one root, and it belongs to `go/types`.** `TestIssue20855`,
`TestIssue23092`, `TestIssue24392`, `TestReimport` pass and `TestCgo` skips identically. The two
failures — `TestImportStdLib` and `TestImportedTypes` — both die inside the converted `go/types`
checker on the SAME construct: `internal/syscall/windows/version_windows.go:87`'s call to
`sync.OnceValue(func() bool {…})` reports *"in call to sync.OnceValue, cannot infer T"* /
*"cannot use (func() bool literal) (value of type func() bool) as func() T value"*. That is generic
type-parameter inference from a func-literal argument, failing in the converted checker where Go's
succeeds — a third dependent of the unbanked `go/types` row, alongside `internal/types/errors` and
`go/internal/gcimporter`'s 184. srcimporter type-checks GOROOT from source, so every package whose
import graph reaches `os` inherits it, which is why both failures name a different top-level package
and the same innermost cause.

**`os/exec` — still build-blocked, and the root is now exact.** Three `CS0103: The name 'var' does
not exist in the current context`, all one shape: an **UNNAMED variadic parameter**. `exec_test.go`
declares `func cmdPipeTest(...string)`, `func cmdStderrFail(...string)` and `func cmdStdinClose(...string)`
— Go permits a parameter with no name at all — and the converter emits the variadic unpacking with an
empty variable name: `internal static void cmdPipeTest(params ꓸꓸꓸstring ʗp) { var  = ʗp.slice(); … }`.
The parameter itself is named fine (`ʗp`); it is the *unpacked local* that inherits the absent Go name.
A one-line converter fix (emit no unpacking, or a discard, when the Go parameter is unnamed) puts
`os/exec` behind the bridge instead of in front of it.
⚠ **FIXED 2026-08-14 (lane `claude/unnamed-variadic`) — and it was not one line or one site.** The
prologue is emitted at TWO sites (`visitFuncDecl` and `convFuncLit`), and the blank (`_ ...T`)
spelling is a third symptom that COMPILED and so was invisible here. `os/exec` builds clean now.
See *the unnamed-variadic build block is fixed* below for the fix, the zero-mover census, and where
`os/exec` actually stands.

### The prize left on the table

Two packages, one defect: **CS0715 — a generic test-local type's `==`/`!=` operators are emitted
into the static `<pkg>_test_package` partial class**, which C# forbids. It holds
`internal/reflectlite` (30 verdicts) and `runtime/debug` (9). ~~And **CS0111 `Append`** holds `fmt`
(63) and `archive/tar` (97) — 160 verdicts on one duplicate-member emission.~~ **The `Append` half
is spent** — see the two entries below: `14bf20010` closed CS0111 itself, and
`claude/dup-append-emission` cleared the two walls behind it in `archive/tar`, which **banks 97/97**.
`fmt` did not follow; its five remaining roots are censused below. CS0715 is now the cheapest
remaining pair on this list.

## ✅ CLOSED for `archive/tar` (banks 97/97) — two walls behind the `Append` root; `fmt` does not follow (2026-08-15, lane `claude/dup-append-emission`)

**The brief was stale, and that is the first finding.** The CS0111 `Append` root this lane was sent
to take had already been fixed by `14bf20010` (2026-08-14) — a named `[]any` slice wrapper's
non-generic `ISlice.Append(object[])` became an EXPLICIT implementation, guarded by
`NamedAnySliceType` — but that commit did not update this board, so the "eight roots MOVED" table
and *The prize left on the table* above both still named it. **Re-measure before believing a board
row**; the row above is now struck through rather than deleted so the same mistake is not made twice.

Behind it, `archive/tar` had **two more walls**, both general converter defects, both fixed here:

1. **CS1537 ×3 — a function-LOCAL type declaration emitted as a COMPILATION-scoped `global using`.**
   The board's "eight roots MOVED" table recorded tar's alias-dedupe root as CLOSED; the
   `NamedAnySliceType` lane reported it as still live, and it was. Root: every local
   type-declaration kind (struct, interface, slice, map, channel, pointer, named-ident) takes
   `liftLocalTypeDecl` — enclosing-function prefix, `ᴛN` uniquification, `liftedTypeMap`
   registration — except the branch that emits a `using` ALIAS, which is what a real `type X = Y`
   and a defined-over-named-interface `type X any` both take. A `global using` is scoped to the
   whole compilation, so `type testFnc any` in `writer_test.go`'s `TestWriter` **and**
   `TestFileWriter`, and again in `reader_test.go`'s `TestFileReader` (with `fileMaker` alongside),
   claimed one alias name. Fix: the naming half of `liftLocalTypeDecl` is factored to
   `liftLocalTypeDeclName` and the alias branch calls it in-function, with the reference mapping
   registered only when the declaration's own object IS the type (`liftedTypeDeclaredBy` — keying it
   on the *target* would rename every `Header`, or every `int`, in the file). **Zero production
   emission impact by construction:** an AST scan of Go 1.23.1 finds no function-local
   alias-or-defined-over-interface declaration in any *compiled* stdlib file — all 50 hits are
   `internal/types/testdata`. Guard: `LocalTypeAliasScope` (the unfixed converter emits five
   duplicate `global using` lines).
2. **CS0121 ×9 — an ambiguous one-field `nil` constructor.** `testClose{nil}` emitted
   `new TestWriter_testClose(default!)`; the typeless `default!` converts to both generated
   one-argument constructors, `T(NilType)` and `T(error field = default!)`. The argument now carries
   the field's type. Narrow by construction — Go requires a positional literal to list every field,
   so only a one-field struct has the arity, and only `nil` renders typeless. **`database/sql`'s
   recorded `stubDriverStmt(NilType)` CS0121 is the same root** and should be re-measured.

**`archive/tar` census: 97 rows, 97 agree, 0 disagree, 0 skipped, 0 disclosed**, 3 excluded (the
standard Phase-4D `Benchmark`/`Example`/`Fuzz` deferrals). Banked — roster row, proof page,
committed test sources.

**A third defect this work found and did NOT fix** (chip raised, reported here for the record): an
alias whose target is an unnamed composite emits its type ARGUMENTS unrooted —
`type names = []string` → `global using names = go.slice<@string>;`, where `@string`, a nested
`slice`, `error`, `complex64`, a same-package `Header` and a foreign `io_package.Reader` all arrive
bare and do not resolve at compilation scope. It is **package-level and pre-existing**, unrelated to
the scope fix, and no converted stdlib package declares such an alias — but any converted user
module would. It surfaced only because the `LocalTypeAliasScope` guard was first written with such
an alias in it.

> **LANDED 2026-08-16, lane `claude/emission-defects`.** The alias RHS now renders in a
> ROOTED-NESTING mode: the target and every name it nests carry full qualification, with four
> qualifiers that are not interchangeable (`go.` for golib types, `System.` for the BCL
> `Func`/`Action`, `go.` again for the variadic `Actionꓸꓸꓸ`/`Funcꓸꓸꓸ` family, and
> `go.<ns>.<pkg>_package.` for a same-package name), while the csproj-alias names (`uint64`,
> `any`, …) are SUBSTITUTED rather than rooted — they are not members of `go` at all. Two shapes
> beyond this row's census turned out to be in the same class and are fixed with it: a **func-type**
> alias (`Func`/`Action` need `System.`, which is no more in scope than `go`) and an alias whose
> target is **itself an alias** (a C# using alias may not name another). The census re-verification
> confirms this row's "no converted stdlib package declares such an alias" for the type-ARGUMENT
> arm — the corpus holds exactly **four** package-level aliases with type arguments (fiat's
> `p224`/`p256`/`p384`/`p521`, each `[4]uint64`), all four taking a C# keyword as the argument, so
> that arm moves nothing. The **substitution** arm did have corpus sites the row did not predict,
> and they were live **CS0234**: a csproj-alias name standing as the WHOLE target was rooted
> (`global using _C_int = go.int32;` — `int32` is a `<Using Alias=…>` for `System.Int32`, not a
> member of `go`, and the existing safe-name sweep skips dot-qualified names by design). **Six**
> sites, all cgo `_C_*` typedefs in **darwin-exclusive** files (`os/user/darwin/`, `net/darwin/`),
> latent because the default `$(GoTargetOS)` is `windows` and nothing compiles them; they now emit
> `int`/`uint`/`long`. Guard: `PackageAliasRootedTypeArgs` (25 aliases,
> output-compared; the unfixed converter fails it with CS0246 and a golden mismatch). Detail:
> `docs/ConversionStrategies-Reference.md` §Type Alias Declarations, "The whole RHS is
> namespace-ROOTED".

### `fmt` — still 0 of 63, and it is FIVE roots, not one

Measured 2026-08-15 with both fixes above in place. Neither touches `fmt`: it has no function-local
alias declaration and no one-field `nil` literal. The `Append` root is genuinely gone; what is
behind it is a fan-out, not a queue, and three of the five cluster on one test type:

| Diagnostic | Site | Shape |
|:--|:--|:--|
| ~~`CS1955` non-invocable `map<TKey, TValue>`~~ **FIXED** | `fmt_test.cs(838)` | `map[int]byte(nil)` — a CONVERSION to a map type emitted an INVOCATION, `map<nint, byte>(default!)`, not a cast. **LANDED 2026-08-16, lane `claude/emission-defects`:** an untyped-nil operand against a map-underlying type LITERAL is now claimed by `isTypeConversion` (the identical-underlying guard rejected it, since untyped nil's underlying is itself) and the ordinary renderer casts — `((map<nint, byte>)default!)`. The row's "one emission rule, two spellings" reading of the sibling `[]int(nil)` was measured and does not hold: `slice<T>(default!)` binds `builtin.slice<T>(T[])`, golib's REAL conversion helper — the same one `[]byte("…")` is emitted against, documented as such — and yields the nil slice, so it is correct rather than lucky, and `(chan T)(nil)` already cast. Both are deliberately left on their existing routes; claiming them would rewrite ~25 corpus sites to no effect. Census, by SPELLING: the BARE `map[K]V(nil)` — the broken one — has **0** stdlib production sites and 13 test-file sites (`fmt`, `reflect`, `encoding/json`, `internal/reflectlite`), which is why the corpus never showed it; the PARENTHESIZED `(map[K]V)(nil)` reached the fork via `convParenExpr` and was already casting, so its single production site (`reflect/type.go:1837`) compiled all along and now only re-parenthesizes. Total corpus footprint: one line. Guard: `UnnamedMapNilConversion`. Detail: `docs/ConversionStrategies-Reference.md` §Canonical typed-nil pointer boxing, "A nil converted to an unnamed MAP type is a cast" |
| `CS0030` `float`→`renamedComplex64`, `double`→`renamedComplex128` ×4 | `fmt_test.cs(941,942,951)`, `scan_test.cs(394)` | a named type over `complex64`/`complex128` does not admit the real→complex conversion Go allows |
| `CS1729` `Scan_type` has no 1-arg constructor | `scan_test.cs(576)` | — |
| `CS0103` `Reader` does not exist | ImplementGenerator output for `Scan_type`→`io.Reader` | the generated wrapper names the interface unqualified |
| `CS0034` `==` ambiguous on `Scan_type` | same generated file | — |

The last three are one type's story and are likely one root; the first two are independent. This is
a materially different proposition from the 97-verdict single-wall `archive/tar` half — the "160
verdicts on one defect" framing above never held for `fmt`.

**Root 1 of the five is closed** (2026-08-16, lane `claude/emission-defects` — see the struck row).
`fmt`'s wall is now FOUR roots; the remaining four are untouched by that lane, so the package's
verdict count is unchanged until they are worked. Whoever picks `fmt` up next should re-measure
rather than assume this row's sites, since the CS1955 no longer masks whatever follows it in
`fmt_test.cs`.

## ✅ CLOSED — the unnamed-variadic build block is fixed; `os/exec` now BUILDS and its next wall is `TB.*` (2026-08-14, lane `claude/unnamed-variadic`)

The one-line prediction above was right about the root and wrong about the size: the defect has
**two** emission sites and **three** symptoms, and the probe found the third.

**The fix.** `visitFuncDecl` and `convFuncLit` each emit a variadic parameter's unpacking prologue
(`var <name> = <name>ʗp.slice();`). Both now skip it when the Go parameter is UNNAMED or BLANK, via
one shared predicate, `variadicParamIsUnreferenceable`. Either spelling is unreferenceable under Go's
own rules, so the local is dead by construction — and this is the same ruling, for the same reason,
that an unnamed/blank POINTER parameter's deref alias already takes (it would otherwise emit
`ref var  = ref Ꮡ.Value;`). The A2 rebank note's precedent chose *skip*, so this matches it rather
than synthesizing a name. A NAMED variadic still unpacks; the signature is untouched in every case,
keeping its `params` array under its own `ʗp` name and simply leaving it unread.

**Three symptoms, not one.** (1) UNNAMED at a top-level func or method — `var  = ʗp.slice();`, the
empty identifier the board recorded. (2) UNNAMED inside a FUNCTION LITERAL — worse, and previously
unrecorded: the literal's signature builder normalizes the absent name to `_` and declares
`params ꓸꓸꓸnint _ʗp`, while the prologue kept rendering `ʗp` from the raw name, so the dead local
carried an empty name AND a name the signature never declared. (3) BLANK (`_ ...T`) anywhere —
`var _ = _ʗp.slice();`, which *compiles*, so it was invisible, but declares a REAL local named `_`
(a plain `var _ = e;` declaration is a variable, not a discard) that then hijacks every `_ = …`
discard in the body — the CS0029 class `bodyUsesBlankDiscard` exists to prevent for a blank
*parameter* name.

**Corpus movers: zero, and that is measured, not assumed.** An AST census of GOROOT (production +
tests, `cmd` and `testdata` excluded) finds **five** sites total. Exactly one is production —
`syscall/syscall_linux.go`'s `func cgocaller(unsafe.Pointer, ...uintptr) uintptr` — and it is
**bodyless** (a `//go:uintptrescapes` linkname target, emitted
`internal static partial uintptr cgocaller(@unsafe.Pointer _Δp0, params ꓸꓸꓸuintptr ʗp);`), so it has
no prologue to skip on *any* target, Linux included. The other four are all `os/exec` test sources:
`exec_test.go`'s `cmdPipeTest`/`cmdStdinClose`/`cmdStderrFail` and `exec_posix_test.go`'s `cmdPwd`.
CNR agrees: one changed file across 601 behavioral packages, and it is the guard test itself.

**Guard:** `UnnamedParams` extended (not a new project — it is already the unnamed/blank *parameter*
guard, and the pointer precedent this follows lives there). It now pins all three shapes — unnamed,
blank, and a NAMED control that IS read — at declaration, method and function-literal positions,
output-compared vs `go run`.

### `os/exec` — the build block is GONE; 22 of 22 run, 22 of 22 MATCH, and the next wall is named

The pipeline (`-tests -test-action all -test-timeout 30m`) now builds `os/exec` with **zero compile
errors**, and the three helpers emit exactly as Go declares them — `internal static void
cmdPipeTest(params ꓸꓸꓸstring ʗp) {` with no prologue, registered as method groups
(`["pipetest"u8] = cmdPipeTest,`) into the converted `map[string]func(...string)`.

It is **not bankable**, and the reason is not `os/exec` and not the host environment:

| Class | Count | What it is |
|:--|:--:|:--|
| ran and **matched Go exactly** | **22** | 19 pass + 3 skip; the `go` and `csharp` verdict maps are identical, including the three Windows skips (`TestExtraFiles`, `TestPipeLookPathLeak`, `TestString`) |
| excluded — `TB.Fatal` unsupported | **26** | the roster in `supportedTestCapabilities` carries the whole `*testing.T` surface but **no `TB.*` member at all**; `os/exec`'s tests funnel through `exePath(t testing.TB)`, which every `helperCommand` call reaches |
| excluded — Phase-4D deferral | **14** | 13 `ExampleXxx` + 1 `BenchmarkExecHostname`; a standing pipeline policy, not an `os/exec` fact |

The run still exits 1, and that too is *not* a divergence: with the 26 `TB.Fatal` tests excluded, the
helper commands they drive go unused, and `os/exec`'s own `TestMain` self-audit fails a run that
leaves a registered helper unused (`helper command unused: "echo"`, …) — on **both** sides. So the
honest verdict is that `os/exec` has **no measured behavioral divergence at all**; it has 40 tests
that never ran. The process-spawn shapes the board expected to wall on host semantics
(`TestEcho`, `TestCatStdin`, `TestPipes`, `TestStdinClose`, `TestExitStatus`, `TestWaitInterrupt`,
`TestContextCancel`, …) are all in the `TB.Fatal` bucket — **excluded, never executed** — so nothing
is yet known about how they behave. Naming `TB.*` support in the hand-owned `core/testing` host is
the exact next domino, and it is a host change with no converter component.

### `crypto/tls` — measured for the first time: BUILD-BLOCKED on two roots, neither of them this one

The fourth flag-bridge class member, pipelined once (93 s) to record its outcome class. It does not
reach the bridge — it does not build. Four errors, two roots, both new board entries:

- **CS0012 ×3** — `crypto/tls/example_test.cs` (88, 99, 198): *the type `tls_package.Config` is
  defined in an assembly that is not referenced*, naming `crypto.tls`. A fresh instance of the
  `-tests` reference-closure family (recorded CLOSED 2026-07-27) arriving through **Example**
  declarations in the black-box `tls_test` package.
- **CS1503 ×1** — `crypto/tls/tls_test.cs:1893`: cannot convert `System.Func<ushort, ushort, int>`
  to `System.Func<ushort, ushort, nint>`. A comparator lambda whose return renders `int` where the
  target signature says `nint`.

No deep chase was commissioned and none was done.

✅ **BOTH ROOTS CLOSED, and a third behind them — `crypto/tls` BUILDS (2026-08-15, lane
`claude/crypto-tls`).** Re-measured at master `6dd7547e4` before anything was believed, per the
stale-premise rule: both reproduced **verbatim**, same codes, same four call sites. This board entry
was accurate. Its *attribution* of the first root was not — see the section at the end of this file.

## RETRACTED — `os`'s "REGRESSION" is a HOST CAPABILITY, and the killer is `SHARE_INFO_2` (2026-08-14, lane os-av-bisect)

Scout batch 2 flagged `os` as a REGRESSION — **31 of 679** measured against this board's **681 of
683**, the converted host dying with `0xc0000005` after 32 verdicts — and sent it to a bisect lane
under the `unique` precedent. **There is no culprit commit.** The access violation reproduces, frame
for frame, at **`a936c8025` (r39-osalloc)** — the very commit whose run recorded the 681. What moved
is the HOST, not the tree.

**The killer, named.** One test: `TestNetworkSymbolicLink`. The goroutine-panic containment from the
jsonrpc chip is what makes it nameable — the fault now arrives with a managed stack instead of a bare
exit status:

```
Fatal error. 0xC0000005
   at go.syscall_package.syscalln(UIntPtr, System.ReadOnlySpan`1<go.uintptr>)
   at go.syscall_package.SyscallN(go.uintptr, System.Span`1<go.uintptr>)
   at go.syscall_package.Syscall6(...)
   at go.internal.syscall.windows_package.NetShareAdd(go.ж`1<UInt16>, UInt32, go.ж`1<Byte>, go.ж`1<UInt16>)
   at go.os_test_package.TestNetworkSymbolicLink(go.ж`1<T>)
```

It is the **syscall STRUCT-PASSING seam** — the open class this file already carries — reached for
the first time by a real suite, and reached in the direction that class had not yet shown: not the
kernel WRITING a native record over a smaller managed one, but the kernel READING a managed record
as a native one and dereferencing a value field as a pointer. That shape faults AT the call, not
arbitrarily far away.

**The mechanism, measured rather than argued.** `internal/syscall/windows.SHARE_INFO_2` holds four
`ж<uint16>` pointer fields and four `uint32`s. The CLR auto-layouts a struct containing references,
so the references are grouped FIRST — dumped by reflection from the built
`internal.syscall.windows.dll`, the record is **48 bytes** against the native **56**:

| native `SHARE_INFO_2` (x64) | native off | C# storage actually at that offset | value handed to netapi32 |
|:--|--:|:--|:--|
| `LPWSTR shi2_netname` | 0 | `Netname` (object reference) | a managed reference, read as runes |
| `DWORD  shi2_type` | 8 | low half of `Remark` (nil) | 0 |
| `LPWSTR shi2_remark` | 16 | `Path` (object reference) | a managed reference |
| `DWORD  shi2_permissions` | 24 | low half of `Passwd` (nil) | 0 |
| `DWORD  shi2_max_uses` | 28 | high half of `Passwd` (nil) | 0 |
| `DWORD  shi2_current_uses` | 32 | `Type` | `0x40000000` |
| **`LPWSTR shi2_path`** | **40** | **`MaxUses` (=1) then `CurrentUses` (=0)** | **`0x0000000000000001`** |
| `LPWSTR shi2_passwd` | 48 | **past the end of the 48-byte record** | whatever follows on the heap |

netapi32 dereferences `shi2_path` — the pointer value **1** — and the process dies. `shi2_passwd` is
a second, independent defect in the same call: an 8-byte over-read past the managed record.

**Proven without go2cs.** A standalone C# program calling `netapi32!NetShareAdd` three ways, on this
host:

| Buffer | Result |
|:--|:--|
| A — blittable `[StructLayout(Sequential)]` record with real `LPWSTR`s | `rc=0`, the share is genuinely created, `NetShareDel rc=0` |
| B — object references at the NATIVE offsets | **survives**, `rc=123` (`ERROR_INVALID_NAME`) — an object reference is a readable address, so this alone is not fatal |
| C — the MEASURED go2cs layout (refs 0/8/16/24, uints 32/36/40/44) | **`Fatal error. 0xC0000005`**, exit `-1073741819` |

So the fault is not "a managed reference where `LPWSTR` belongs"; it is the **field REORDERING** that
puts an integer `1` under `shi2_path`. B is the control that makes C mean something.

**Why the board's 681 held and this host's run does not.** Go's own test treats exactly two
`NetShareAdd` failures as a skip — `ERROR_ACCESS_DENIED` and `NERR_ServerNotStarted` (2114) — and on
a host where either fires, netapi32 never reaches the buffer, both sides skip, and the row AGREES.
On this host neither fires: the session is elevated, `LanmanServer` is **Running**, probe A creates a
real share, and **Go's own `TestNetworkSymbolicLink` PASSES**. The buffer is therefore marshalled,
and the layout defect becomes fatal. The 681 was measured on the i9 that died 2026-08-09; it cannot
be re-measured there, and nothing about that record needs to be doubted — it needs a **precondition
written down**, which is what this entry is.

**The record survives — control run, HEAD, one test excluded.** With
`-run '^(?!TestNetworkSymbolicLink$)'` and nothing else changed, the host runs the suite **to
completion**: **683 verdicts — 659 pass, 21 skip, 2 test failures**, those two being `TestUTF16Alloc`
(the recorded alloc-count-semantics **disclosure**) and `TestWriteStringAlloc` (the ONE real residual
r39-osalloc rooted and left as an architectural arc). That is the recorded shape exactly.
`TestNetworkSymbolicLink` is the **sole** host-killer; there is nothing behind it. (C#-side census
only — no differential was run, and the skip count differs from the recorded 34 because a
more-capable host skips fewer tests, which is the same host-capability fact stated from the other
side. Do not read 683 here and 683 on the record as the same denominator.)

**Verdict for the roster and the board.**

- The REGRESSION row is **retracted**. `31 of 679` stands as a floor under the jsonrpc rule, and is
  not evidence of anything having broken.
- `os`'s **681 of 683 + 1 disclosed record stands**, now qualified: it is measurable only on a host
  where `NetShareAdd` short-circuits. On a host with the Server service reachable, `os` measures
  NOTHING — the process dies at test ~32 of 174.
- `os` still does not bank, for the reason it never did: `TestWriteStringAlloc`. It has never been a
  roster row; 681 of 683 is a board record.
- **No commit is implicated, so nothing is reverted and no guard is added.**

**The census this finding corrects.** The struct-passing census above is scoped to
**`src/core/syscall`** and therefore could never have listed this member. `internal/syscall/windows`
is a SECOND package holding the same class, and its own census is:

| Wrapper | Non-blittable struct | Reached by |
|:--|:--|:--|
| `NetShareAdd` | `SHARE_INFO_2` (`Netname`, `Remark`, `Path`, `Passwd`) | **`os`'s `TestNetworkSymbolicLink` — the only caller in all of GOROOT**; fatal on a capable host |
| ~~`GetAdaptersAddresses`~~ | `IpAdapterAddresses` (nine `ж<T>`, `array<byte> PhysicalAddress`, `array<uint32> ZoneIndices`) | **ROW WITHDRAWN 2026-08-17 — it was never a member of this census.** The wrapper is handed a byte BUFFER and fills it, which is what a byte buffer is for; it is correct and stays auto-converted. The defect is entirely in the CALLER, `net.adapterAddresses`, which reinterprets the filled buffer as the record — the `readReparseLink` / `dir_windows_impl` fork, not the mirror-the-wrapper one. Fixed in the `net` interfaces arc; see the entry at the end of this file |
| `Module32First` / `Module32Next` | `ModuleEntry32` (`array<uint16> Module`, `array<uint16> ExePath`) | `syscall`'s own suite |
| `GetFileInformationByHandleEx` | `FILE_ID_BOTH_DIR_INFO` / `FILE_FULL_DIR_INFO` (`array<uint16>` names) | `os`'s `readdir` — **already answered**, and it is the worked precedent: `src/core/os/windows/dir_windows_impl.cs` reads the kernel's buffer at NATIVE offsets instead of reinterpreting it as the managed surrogate |
| `WSASendMsg` / `WSARecvMsg` | `WSAMsg` (`ж<syscall.WSABuf>`) | `net`'s UDP OOB path |
| `NetUserGetLocalGroups` | `ж<ж<byte>>` out-buffer | `os/user` |

**Why this one is NOT fixed here, and what the candidate remedies cost.** The established remedy is a
hand-owned wrapper with a blittable mirror and a field-for-field copy at the boundary. It does not
reach this member, because the wrapper never sees the struct: `os_windows_test.go` writes
`(*byte)(unsafe.Pointer(&p))`, which the converter emits as
`Ꮡp.Reinterpret<windows.SHARE_INFO_2, byte>()`, and `Reinterpret` correctly REFUSES to alias a
reference-bearing struct as `byte` — so it falls to `(ж<byte>)(uintptr)box` and the wrapper receives
a NATIVE-address box with the managed identity already gone. There is nothing left to copy from.

1. **Recover the struct by reading the raw address** (`Unsafe.Read<SHARE_INFO_2>`) inside a
   hand-owned `NetShareAdd`. Rejected: it fabricates managed references out of a raw address, which
   `ж.PointerExtensions.cs` names as a CLR type-safety break and "strictly worse than the
   wrong-but-contained read the address route produces". It would also rest on a pin the address
   route does not promise.
2. **Hand-own `NetShareAdd` to fail by name** — a `manualConversionFuncs` placeholder returning a
   declared "non-blittable struct handed to the kernel" error, the `registerCache`-style
   announce-itself stub. Small, zero blast radius (one caller in GOROOT, and it is a test), and it
   converts a whole-suite process death into ONE loud row. But Go PASSES this test on a capable host,
   so the row would be a real mismatch rather than a skip, and the stub declares a capability limit —
   a coordinator ruling, not a lane's call. **Recommended, pending that ruling.**

   ✅ **RATIFIED and LANDED 2026-08-14 (lane `claude/defect-batch-1`).** The coordinator ruled for
   remedy 2; the wrapper is displaced through `manualConversionFuncs` and reimplemented in
   `src/core/internal/syscall/windows/windows/zsyscall_windows_impl.cs`, throwing a
   `NotSupportedException` that names the capability limit, the layout mechanism and this entry.
   The impl file's header carries the six same-shape wrappers above as a table, so the boundary of
   the limit is readable where the limit is declared. **The value is measured, on the capable host
   that produced this entry:** `os` no longer dies at test ~32 of 174 — it runs the suite to
   completion and the differential is **679 matched of 683**, with exactly four divergent rows:
   `TestNetworkSymbolicLink` (this declared limit, an honest mismatch — Go passes it),
   `TestUTF16Alloc` and `TestWriteStringAlloc` (the recorded alloc-count disclosure and the
   r39-osalloc architectural residual), and `TestDirectorySymbolicLink`, which SKIPS where Go
   passes and is not attributed here. Compare the pre-fix floor on the same host: **31 of 679**.
   `os` still does not bank, for the reason it never did.
3. The durable answer is the **ж-box arc's**: have the non-aliasing `Reinterpret` fallback retain the
   source object so a hand-owned wrapper can reach it, at which point remedy 1 becomes an ordinary
   field-for-field copy with no fabrication.

Whichever lands, verify at VALUE level as the class demands: probe A above is the oracle — the share
must actually be created and `NetShareDel` must remove it.

**A measurement rule this leaves behind.** A converted suite that dies with a native fault is not
automatically a regression, and a bisect is not automatically the right instrument. Root the fault
site FIRST — the containment work makes that cheap now — and ask whether the failing call depends on
a host capability the previous measurement did not have. Here one probe at the recording commit
settled it in six minutes, where a 230-commit first-parent bisect would have found nothing and said
so only after eight probes.

---

## RESOLVED (2026-08-14, lane `claude/go-types-av`) — `go/types`' access violation is a reflect POINTER TOKEN converted back to a pointer; 15 → 202 verdicts

`go/types`' converted test host builds and runs now that the CS0839 `defer panic(err)` blocker is
fixed, and it died with a bare access violation after 15 verdicts:

```
Fatal error. System.AccessViolationException: Attempted to read or write protected memory.
   at go.go.types_test_package.testFilesImpl(...)
   at go.go.types_test_package.testFiles(...)
   at go.go.types_test_package.testPkg(...)
```

Exit `0xc0000005`, zero verdicts after, 542 behind it. **First-contact territory, so the OS playbook
applied rather than a bisect** — and it paid the same way: the fault site named the defect outright,
with no probing of history at all.

**The fault site.** `testFilesImpl`'s FIRST statement after building the Config is

```go
*boolFieldAddr(&conf, "_Trace") = manual && testing.Verbose()      // check_test.go:166

func boolFieldAddr(conf *Config, name string) *bool {              // check_test.go:343
	v := reflect.Indirect(reflect.ValueOf(conf))
	return (*bool)(v.FieldByName(name).Addr().UnsafePointer())
}
```

There is no frame below `testFilesImpl` because the faulting store is inlined at the call site;
`boolFieldAddr` itself returns perfectly well. **`TestCheck` is the first test alphabetically that
reaches `testFilesImpl` at all** — not one of the nine before it (`TestAlias_Rhs` …
`TestBuiltinSignatures`) calls `testFiles`; they type-check through `mustTypecheck` or not at all, and
neither route uses this idiom — and `blank.go` is the first file in
`testdata/check`. So the crash is the FIRST EVER EXECUTION of the idiom, not corruption surfacing
late. `blank.go` being a five-line `package _` file is a coincidence of alphabetical order and means
nothing.

**The mechanism.** A Go pointer to managed storage has no machine address, so `reflect.Value.Pointer`
and `.UnsafePointer` both project through `reflectPointerToken` (`reflect/value_impl.cs`) and answer
with a stable **order token** — `INilPointer.PointerOrderToken`, whose own remarks say tokens "are
order keys, never an identity substitute". The hand-own's header says what it was written for: *"fmt
uses it only to test nil-ness and to print an address for %p."* `go/types` is a new caller with the
other need. Emitted, its line is

```csharp
return (ж<bool>)(uintptr)(v.FieldByName(name).Addr().UnsafePointer());
```

and `ж<T>`'s `explicit operator ж<T>(uintptr)` builds a **native-address** box over whatever number it
is handed. The subsequent `.Value` store writes a bool at the numeric value of an order token — an
access violation where that page is unmapped, silent heap corruption where it is not. Note this is the
`unique`/`clone.cs` family stated from the other end: there an interior address was fabricated by
ARITHMETIC on a movable box; here it is fabricated by a projection that never was an address.

**Proven without `go/types`, in 22 lines** — a struct with an unexported bool, string and int field,
the `boolFieldAddr` idiom verbatim, no other package involved. Go prints `x true`; the converted C#
dies `System.AccessViolationException` at `go.main_package.Main()`, the identical frame shape (the
store inlined into its caller). It is the whole defect, with go/types removed.

**The fix — golib remembers what the projection drops.** The information was never lost:
`reflect.Value.Addr` surfaces the real aliasing box (`addrBox`, minted by `GoReflect.FieldAliasBox`),
and only the scalar projection discards it. `golib/ж.PointerTokens.cs` adds `ManagedPointerTokens`, a
weak token→box table that `reflectPointerToken` registers into and the `uintptr → ж<T>` operator
consults first; a token that came from there recovers its box and aliases the original storage exactly
as Go's pointer would, and everything else keeps the native-address route unchanged.

Two properties are deliberate, and both exist to keep the blast radius at zero. **The token VALUE does
not change** — minting self-identifying handles from a reserved range would also move what `%p` prints
and what order pointer-keyed maps print in, since `fmt` and `internal/fmtsort` read the very same
token — so the association is carried out of band instead. **Reads are lock-free with an empty fast
path**: `Resolve` sits on 875 emitted cast sites corpus-wide (54 in the syscall wrappers), and a
program that never asks reflect for a pointer's scalar form answers from a single volatile load. The
type-descriptor path (`typeDescriptorOrderToken`) returns before registration and is untouched.

**Measured movement: 15 verdicts → 202** (169 pass, 33 fail), zero access violations anywhere in the
run. The 33 failures are entirely the KNOWN-OPEN type-parameter class — `TestCheck/{chans,
funcinference,typeinst1,typeparams,map0,map1,slices,issues1}.go`, `TestExamples/{functions,
inference}.go` — the same signature as `go/internal/gcimporter`'s residual 108.

**The next wall, named: the SAME open root, in its non-terminating form.** The run now dies at
`TestFixedbugs/issue48951.go` with `0xc00000fd` — **STATUS_STACK_OVERFLOW**, an unbounded recursion in
`validType0`:

```
   at go.go.types_package.validType0(...)      × until the stack is gone
   at go.go.types_package.validType(...)
   at go.go.types_package.processDelayed(...)
```

`issue48951.go` is the *invalid recursive type* testdata (`A2[P any] [10]A2[*P]`), and Go's cycle guard
is `for _, e := range nest { if Identical(e, t) { … } }` (`validtype.go:104`). A converted `Identical`
that judges a parameterized named type not identical to itself never finds the cycle, so the walk
recurses forever. That is **the board's already-open second root** — "a type parameter judged not
identical to itself" — appearing as a hang rather than as a bogus error message. It is a consequence
of that root, not a new defect, and it is not this lane's.

**Roster arithmetic: `go/types` does NOT bank.** 202 of 557 with a live process-killer behind it; test
sources deliberately not committed, and the pipeline's churn under `src/core/go/types` restored.

**`go/internal/gcimporter` re-measured on the same tree: 475 of 583 matched, 108 mismatched —
UNCHANGED from its baseline, to the verdict.** All 108 are still `Go="pass" C#="fail"` inside
`TestImportTypeparamTests`, and nothing else moved in either direction (14m20s, under concurrent
lane load). That is the expected answer and worth recording as a NEGATIVE result: gcimporter's
residual is the generics root in `go/types`' checker, which this fix does not touch, so a package
whose failures all sit there should not move — and did not. The two measurements now agree on the
same open root from opposite directions.

**Gates.** `GolibTests` 111/111. Full behavioral suite **PASS — 574/574 transpile, compile and
goldens; 548 stdout comparisons, 0 failed, 26 skipped (no `package main`); 3,769 s** (i7-5820K,
solo). `check-no-regression` **NO REGRESSION — generated C# and
`.csproj` byte-identical across all 601 behavioral packages, 1,025 s** (2 advisory converter
warnings, 0 NOT MEASURED).
`go2cs.slnx` **build succeeded, 0 errors, 585 s** — owed because golib's public surface
gained a type. Guarded by the new
`ReflectFieldAddrWrite` behavioral **output** test, which faults with an access violation on pre-fix
golib. Doctrine: `ConversionStrategies-Reference.md`, *A pointer `reflect` handed out as an
`unsafe.Pointer` must convert BACK*.

**What this leaves for the ж-box arc.** The arc's charter item — "have the non-aliasing fallback retain
the source object" (remedy 3 of the `NetShareAdd` entry above) — is the general form of what this table
does for one seam. This fix is deliberately narrower: it restores the round trip for pointers that
`reflect` itself handed out, and changes nothing about the transient-address model that
`(uintptr)ж<T>` uses for everything else. A pointer whose scalar form was produced by arithmetic
(`uintptr(unsafe.Pointer(&x)) + offset`) still cannot come back, and still should not.

## ✅ CLOSED — the `TB.*` domino: the whole `testing.TB` surface is supported, and `os/exec` is measured for the FIRST time (2026-08-14, lane `claude/tb-surface`)

The unnamed-variadic lane's closing prediction — "naming `TB.*` support in the hand-owned `core/testing`
host is the exact next domino, and it is a host change with no converter component" — was right about
the domino and inverted about where the work lived. Nothing in the host needed implementing, and the one
line of code that changed is in the CONVERTER: the capability ROSTER was the entire defect.

**The mechanism, because it is not obvious.** A capability name is keyed on the RECEIVER's named type
(`analyzeTestingCapabilities`): a call written `t.Fatal(err)` records `T.Fatal` when `t` is a
`*testing.T` and `TB.Fatal` when the enclosing helper declares `t testing.TB`. Those are two roster
entries over ONE implementation, and `supportedTestCapabilities` listed only the first. So any test
whose transitive same-package closure touched a `testing.TB`-typed helper was gated out wholesale —
not for anything it did, but for the type its helper's parameter happened to name.

**Why widening it is honest, and what "supported" mechanically requires.** Three things must hold, and
for `TB` all three already did:

1. `core/testing` declares the member — its `TB` interface carries Go 1.23's full public set (the 18,
   minus the unexported `private()`).
2. Something forwards it for the concrete receiver. The converter already emits
   `[assembly: GoImplement<testing_package.T, testing_package.TB>(Pointer = true)]` into every
   consuming package, and go2cs-gen's `ImplementGenerator` mints the `testing_TжTB` adapter from it —
   read out of the generated file rather than assumed:
   `void TB.Fatal(Span<object> args) => testing_package.Fatal(m_box, args);`, once per member, all 18.
3. The implementation is real. Each forward lands on the same `TestExecution`-backed member the `T`
   spelling has always used, so a `TB.FailNow` throws the same `TestAbortException` and aborts the
   same way.

So `T` never needed a base list and no per-suite wiring was ever owed — a standing note on
`core/testing`'s `TB` predicted that work, and the adapter had already made it unnecessary. That note
is now corrected in place.

**The one declared limit,** and it is a property of `B`, not of `TB`: an adapter built from a
`*testing.B` forwards to `B`'s compile-only no-ops. Benchmarks are never registered or run, so the
only path that puts a live `B` behind a `TB` parameter is a Test that calls `testing.Benchmark` itself
and hands the `b` onward. No suite does. If one appears its failure reports would be silently
swallowed — a Phase-4D benchmark-execution question, not a reason to withhold the `T`-backed surface
from every test that has one.

**Charter §9 discharged — the roster scan, with its positive control.** Widening
`supportedTestCapabilities` moves excluded tests into the RUN set of packages already banked, so the
rule is to scan every validated package first. Same-package `testing.TB` helpers exist in 15 GOROOT
packages (`cmd` excluded); the only two non-test files that declare one are `internal/testenv`'s, and
those are consumed cross-package, which the same-package reference graph deliberately does not follow.
Four of the 15 are banked, and one of those four — `internal/fuzz` — does not move, because its
`newWorkerForTest(tb testing.TB)` is reached only from `Benchmark` declarations already excluded by
kind. Positive control: the scan finds `os/exec`'s `exePath`, the helper this whole entry is about.

The three that DO move were re-measured, and every one moved by COUNT ONLY — the sweep's sole complaint
in each case, with no verdict disagreeing on either side:

| Package | Banked | Now | The tests that were being withheld |
|:--|:--:|:--:|:--|
| `debug/gosym` | 9 | **10** | `Test115PclnParsing` — pass/pass |
| `internal/zstd` | 534 | **536** | `TestAlloc`, `TestLarge` — skip/skip, both gated on a `zstd` binary this host lacks |
| `archive/zip` | 98 | **100** | `TestZip64`, `TestZip64EdgeCase` — pass/pass; the first streams 4 GiB through the writer, and is most of that row's 775 s |

`strings`, `bytes` and `context` re-validated at their exact banked counts as capability-regression
controls: their exclusion sets are unchanged, which is the property a widening most needs to prove.

One caveat is banked with `internal/zstd`, because a skip that agrees is not a test that agrees: on a
host WITH `zstd` installed both rows actually RUN, and `TestAlloc` asserts an exact **zero** allocations
— the `alloc-profile` class the CLR provably cannot satisfy. Expect it to need a disclosure there. The
row COUNT is stable either way, so the sweep's floor holds; only the verdict would move.

### `os/exec` — measured for the first time: 101 verdict rows, 48 agreeing, and TWO host roots

Before this change the pipeline compared **22** rows and all 22 matched, which read like a nearly-clean
package. It was not a measurement: the 26 excluded tests were every process-spawn shape `os/exec` has,
and nothing was known about them. With the surface widened the manifest carries **40 included test
declarations and zero capability-blocked ones** (13 excluded remain, all Phase-4D: 12 `Example`, 1
`Benchmark`), and the comparison covers **101 verdict rows**.

| Class | Rows | What it is |
|:--|:--:|:--|
| agree | **48** | 40 pass + 8 skip, the skips identical on both sides |
| disagree — **root A**, helper-command argv | **26** | every one `Go="pass" C#="fail"` |
| disagree — **root B**, relocatable single-file test executable | **27** | every one `Go="pass" C#="fail"` |

**No verdict disagrees for a reason belonging to `os/exec` itself.** Both roots are properties of the
converted TEST HOST, and one of the two is an impossibility this file already declared.

**Root A — the host throws on its own test binary's argv.** `os/exec` drives its helpers by re-executing
the test binary with a bare command name (`exec.Command(exePath(t), "cat")`), and its `TestMain`
dispatches on `os.Args[1]`. Go's `flag.Parse()` stops at the first non-flag argument and leaves the rest
for the program. The converted host's `TestOptions.Parse` instead has no stopping rule: its `default:`
arm throws `ArgumentException("unsupported converted test option: cat")`, so every helper child dies at
startup with exit 2 before `TestMain` is ever entered. The parent then reports the *downstream* symptom
— `echo: want "foo bar baz\n", got ""`, `ExitCode got 2, want 42`, `Wait: exit status 2`, `first output
line: EOF` — which is why this reads as twenty different failures instead of one. It is a small, closed
fix: `os.Args` is wired to `Environment.GetCommandLineArgs()` (`runtime/goargs_impl.cs`) independently
of the option parser, so the child already CAN see its argv; the host merely has to stop rejecting it,
with Go's stop-at-first-non-flag rule rather than by ignoring unknown tokens (`exe cat -n` must leave
`-n` to the child, not parse it as a host flag). **This is the next domino.**

**Root B — the same `0x8000809a` this file already names.** `TestLookPathWindows` and `TestCommand`
build their fixtures by COPYING the test binary into a temp directory and running the copy. Every such
row fails with `exit status 0x8000809a` and *The application to execute does not exist:
'…\p1\os.exec.tests.dll'* — byte for byte the `LibHostAppRootFindFailure` already declared for
`os_test.TestRemoveAllWithExecutedProcess` under the capability **"relocatable single-file test
executable"**: an apphost is a stub bound at build time to a managed assembly of the same base name that
must sit beside it. Go's statically-linked test binary is the only reason the premise holds there.

**Deliberately NOT gated.** Root B's 27 rows could be excluded by declaration name the way `os`'s one
test is, but doing it now would be half-work: root A's 26 rows would still fail, so `os/exec` would not
bank either way, and the next lane would have to revisit the same list. Root A is *unimplemented*, not
impossible, so it must never become a gate entry at all (the standing rule for
`unsupportedRuntimeCapabilities`). `os/exec` therefore stays unbanked, with both roots named, and its
converted test sources were removed rather than committed.

### The CLASS this domino was holding — every package with a same-package `testing.TB` helper

`internal/testenv`'s TB-typed helpers, which most of the corpus calls, never gated anything: the
capability graph closes over SAME-package references only, so a cross-package helper gates through its
own package's conversion instead. What gated was a package's own test-file helper. The complete GOROOT
census (`cmd` and `testdata` excluded), with the banked rows re-measured here and the rest recorded:

| Package | Banked? | TB-typed helper | Effect of the widening |
|:--|:--:|:--|:--|
| `os/exec` | no | `exePath` | **26 tests unblocked** — measured above; the reason this lane exists |
| `archive/zip` | yes | `testZip64` | re-measured, see the roster |
| `debug/gosym` | yes | `read115Executable` | re-measured, see the roster |
| `internal/zstd` | yes | `findXxhsum`, `bigData`, `findZstd`, `zstdBigData` | re-measured, see the roster |
| `internal/fuzz` | yes | `newWorkerForTest` | **none** — reached only from `Benchmark`s, excluded by kind |
| `database/sql` | no | 36 sites, the widest in the corpus | pending its own measurement |
| `net` | no | 9 sites | pending |
| `net/http` | no | 8 sites | pending |
| `crypto/tls` | no | 3 sites | pending; the two build roots are CLOSED (2026-08-15) and the package compiles, but its run does not reach these sites — see the section at the end of this file |
| `crypto/internal/mlkem768`, `crypto/x509`, `go/types`, `net/netip`, `os`, `runtime` | no | 1 site each | pending |

The nine unbanked rows are not claims — no run was commissioned for them. They are named so the next
lane to reach any of them knows this particular exclusion is already gone.

## ✅ CLOSED — the helper-argv domino: the host stops at the first non-flag argument, and `os/exec`'s root A is gone. It does NOT bank: root B holds, and a THIRD root appears underneath (2026-08-14, lane `claude/host-argv-stop`)

The `TB.*` lane named root A as the next domino and priced it as "a small, closed fix". It was: one
hand-owned file, `src/core/testing/TestOptions.cs`, and no converter component at all. **23 of root
A's 26 rows now agree.** The other 3 were never really root A — the helper child died at argv before
it could reach the thing that was ALSO going to kill it, and fixing the first revealed the second.

**The fix, and why it is Go's rule rather than a leniency.** `flag.(*FlagSet).parseOne` stops at the
first token that is not at least two characters long and beginning with `-`; that token and every one
after it belong to the program. A Go test binary IS a program — its `TestMain` may take arguments —
and `os/exec` drives its entire helper protocol that way: `exec.Command(exePath(t), "cat")`, with
`TestMain` dispatching on `flag.Args()[0]`. `TestOptions.Parse` had no stopping rule; its `default:`
arm threw `unsupported converted test option: cat` and the host exited 2 before `TestMain` was
entered. Nothing else was owed: the converted `os` package fills `os.Args` from the real command line
independently of this parser (Windows via `syscall.GetCommandLine` + `commandLineToArgv`; unix via
`runtime.argslice`), so the host's whole obligation is to stop and leave the rest untouched.

**Stopping is not ignoring**, in both directions, and the guard pins both: `exe cat -n` must leave
`-n` to the child, while an unrecognized `-flag` BEFORE any non-flag is the host's own command line
being wrong and still exits 2 — with Go's wording now (`flag provided but not defined: -x`), since
this host stands in for a Go test binary and its stderr is read beside one. The rest of `parseOne` is
mirrored for the same reason: a lone `-` is a non-flag, `--` terminates and is consumed, `---x`/`-=x`
are `bad flag syntax`, a non-boolean flag takes the next token as its VALUE even when that token looks
like a flag (`-run -v` filters on `-v`), and one or two leading dashes name the same flag — the
`--json` ≡ `-json` equivalence `TestFlagBridge` already assumed when it republishes these options
undashed. Nothing records the leftover tokens: the program reads its own argv, and an unread property
would be machinery this host does not need.

### `os/exec` re-measured: 101 rows, **74 agreeing** (67 pass + 7 skip), 27 disagreeing

| Class | Rows | Then | Now |
|:--|:--:|:--:|:--|
| agree | **74** | 48 → 71 | 67 pass, 7 skip |
| disagree — **root A**, helper-command argv | **0** | 26 | ✅ closed by the host-argv-stop lane |
| disagree — **root B**, relocatable single-file test executable | **27** | 27 | unchanged, and still a DECLARED limit |
| disagree — **root C**, `os/signal`'s runtime primitives are unimplemented stubs | **0** | 3 | ✅ closed by the os-signal-primitives lane |

**Root C is closed** (2026-08-14). The six primitives now forward through the `//go:linkname` push
registry into `runtime/sigqueue.cs`'s own state machine, and the two dead ends behind the forwarders —
nothing armed the Windows console control handler, and `notetsleepg` threw on `getg()` before it could
block — are closed with it. `TestWaitInterrupt/{Wait,Exit-hang}` and their parent all pass;
`cmdHang`'s `signal.Ignore(os.Interrupt)` runs. Detail:
[ConversionStrategies-Reference](../ConversionStrategies-Reference.md#manually-converted-declarations),
"`os/signal`'s six primitives".

**The disagreeing set is now exactly root B** — the 14 `TestCommand/*`, 11 `TestLookPathWindows/*`
and their two parents — so `os/exec` sits at all-agree-except-declared-limit, which is precisely the
shape the coordinator ruling below was reserved for. **This lane therefore reports and STOPS: no
bank.**

**`os/signal`'s own suite cannot pay this domino back, and the census says why** (2026-08-14). It was
re-run end to end (`-test-action all`) after the primitives landed and it **re-validates 1/1**,
unchanged — because its Windows-eligible RUN surface is exactly one test. `signal_test.go` is
`//go:build unix`, `signal_linux_test.go` is linux, `signal_cgo_test.go` needs cgo,
`signal_plan9_test.go` is plan9, and `example_test.go`'s two examples carry no `// Output:` comment,
so `go test` never executes them (they stay excluded on both sides). That leaves `TestCtrlBreak`,
which compiles a **native Go child** with the real go tool and sends it a console control event — so
it exercises `syscall.LoadDLL`/`GenerateConsoleCtrlEvent` and `os/exec`, and never touches a single
converted signal primitive. That is why it passed while all six were throwing stubs, and why it still
passes now: it is not a regression detector for this area in either direction. The guard that *does*
cover the primitives is the `SignalPrimitives` behavioral test (Notify/Stop/Ignore/Reset/Ignored
transitions compared against `go run`, which also proves `signal_recv` really parks — `Stop` blocks
in `signalWaitUntilIdle` until the watcher goroutine reaches `sigReceiving`).

Real console-event DELIVERY was **measured out of band** rather than left to inference: a scratch
two-process probe in `TestCtrlBreak`'s own shape — child started with `CREATE_NEW_PROCESS_GROUP`,
`GenerateConsoleCtrlEvent(CTRL_BREAK_EVENT, childPid)` from the parent — run against the converted
child and against a native Go child built from the same source. Both print `ready` then
`got: interrupt` and exit 0, repeatably. Worth recording from that probe: the FIRST attempt reported
`exit status 0xc000013a` (STATUS_CONTROL_C_EXIT) with **no output at all**, and it was a cold-start
race in the probe, not a defect — a cold Debug child carrying the full converted runtime closure had
not yet reached `signal.Notify` when the 3-second timer fired, so the `wanted` bit was unset,
`sigsend` correctly returned false and the default handler killed it (buffered stdout dying with it).
Raising the delay to 8 s made it deterministic. A probe that sends the event on a timer instead of
waiting for the child to announce readiness will keep re-finding this. DELIVERY still has no
SUITE guarding it — that fixture is the honest next item here.

⚠ **One row of the split is unreconciled, and is recorded rather than smoothed.** The totals move
exactly as root C predicts (101 unchanged; disagree 30 → 27; agree 71 → 74), but the internal split
moved by four passes and one skip (63 pass + 8 skip → 67 pass + 7 skip) where root C accounts for
three. The likely cause is that a subtest of the previously-FAILING `TestWaitInterrupt` parent was
counted as a skip while its parent failed and now runs and passes; that was not re-derived against
the old binary, so it is a hypothesis, not a finding. The 27 disagreeing rows were enumerated by name
from the run and are all root B.

**Two minor host observations, recorded rather than fixed** (neither moves a verdict): the host's
fmt-free `TestFormat` renders `*exec.Cmd` and `*strings.Builder` under `%v`/`%s` as raw pointers
(`0x1fc631592e0`), so `t.Log(cmd)` and `t.Logf("stderr:\n%s", cmd.Stderr)` print addresses — which is
what made root C's diagnostic invisible in the comparison output and cost a direct child run to see.

### ⛔ STOPPED for a coordinator ruling — and the ruling is no longer load-bearing for `os/exec`

The lane was chartered to stop, if `os/exec` landed at all-agree-except-declared-limit, and ask
whether declared-host-limit rows can carry a bank the way alloc-profile disclosures do. **It did not
land there** — root C is undeclared and unimplemented, so `os/exec` is unbankable on its own merits
whatever the answer. Its converted test sources were removed rather than committed, exactly as the
`TB.*` lane left them. The question is still worth answering, so here is the measured shape:

- **Can the disclosure machinery pin root B by signature? Mechanically, yes.** All **25** leaf rows
  (14 under `TestCommand`, 11 under `TestLookPathWindows`) carry `exit status 0x8000809a` and
  *The application to execute does not exist* in their C# failure text — verified, 25 occurrences for
  25 leaves. `matchTerminalStatuses` needs exactly that: an entry keyed on the test NAME, `Go="pass"`
  ∧ `C#="fail"`, and the declared `signature` present as a substring. The **2 parent rows** carry no
  own output and would ride the existing disclosed-parent aggregation rule (no own text, ≥1 disclosed
  descendant, no mismatched descendant), so 25 entries would cover all 27.
- **But the precedent for this exact capability is a GATE, not a disclosure.** `os_test.TestRemoveAllWithExecutedProcess`
  is listed in `unsupportedRuntimeCapabilities` under the same
  `relocatable single-file test executable` name — the test is EXCLUDED from the run set, never run
  and disclosed. A gate keys on the test DECLARATION, so `TestCommand` and `TestLookPathWindows`
  would be **2** entries rather than 25.
- **And no banked package currently carries either form of this limit.** `os` is not on the roster
  (only `os/exec/internal/fdtest` and `os/signal` are), so the 141-package roster contains no
  precedent for a bank resting on a declared HOST limit. Every existing disclosure class is about
  *measurement* the CLR cannot perform (alloc counts), not about a test the host cannot run at all.

That is the whole question, and it is a policy one: whether "the host provably cannot execute this
shape" is bankable at all, and if so whether it should read as an excluded capability (2 gate
entries, tests never run) or as disclosed divergences (25 signature entries, tests run and fail
visibly). Recorded here for the next lane that reaches a package where it IS load-bearing.

## RESOLVED (2026-08-14, lane `claude/typeparam-identity`) — the type-parameter identity wall is EMBEDDED-STRUCT COPY ALIASING, and the fix is one generator field

The campaign's last mega-wall — one root, three dependents, ~450 verdicts: `go/internal/gcimporter`'s
108 `TestImportTypeparamTests` mismatches (the `constrained by` signature), `go/types`' own 33
failures plus the `0xc00000fd` stack overflow at `TestFixedbugs/issue48951.go`, and
`go/internal/srcimporter`'s 2 `cannot infer T` failures. **None of it is generics, `Identical`, or
the instance caches.** It is a struct value copy that was not a copy.

**The hypothesis was wrong, and instrumentation said so in one run.** The standing suspicion —
`*TypeParam` compared by pointer identity through a conversion that mints distinct
wrappers/adapters/boxes, so `Identical(t, t)` fails reflexivity — is **false**. Instrumented at
`predicates.cs`'s `identical`, the two operands of the failing comparison are TWO DIFFERENT
`*TypeParam` objects (ids 2 and 3) and `AreEqual` answers `False` **correctly**; a same-object
comparison answers `True` with the same box on both sides. The `Context`/`instanceHash`/`lookup`
caches were instrumented too and behave exactly as Go's: distinct hashes (`00S[T₂]` vs `00S[T₃]`),
zero cache hits, no collapse. Everything the board pointed at was healthy.

**The 7-line reproducer.** Driving the converted `go/types.Check` directly over a tiny source (the
types/errors lane's harness shape — build once, then a ~1 s run per probe) minimizes
`test/typeparam/absdiff2.go`'s 13 errors down to:

```go
package main

type S[T any] struct{ V T }

func (a S[T]) M1() T { return a.V }

func (a S[T]) M2() T { return a.V }     // C#: cannot use a.V (variable of type T …) as T value
```

Go reports 0 errors. The converted checker reports 1, **on the SECOND method only** — delete `M1`
and it passes. Spelling the second method's parameter differently (`func (b S[U]) M2() U`) prints
the tell outright: `b.V` has type **`T`**, the FIRST method's type parameter.

**The mechanism, printed.** `Named.expandUnderlying` substitutes the origin's underlying per
instance. Instrumented:

```
[EXPAND] targs=[TP#2] origUnder=struct{TP#1} smap={TP#1->TP#2}  ->  newUnder=struct{TP#2}
[EXPAND] targs=[TP#3] origUnder=struct{TP#2} smap={TP#1->TP#3}  ->  newUnder=struct{TP#2}
                                        ^^^^ the ORIGIN was mutated by the first substitution
```

`subst.go`'s `substVar` is `copy := *v; copy.typ = typ`. `go/types.Var` EMBEDS `object`, which
carries `typ` — and go2cs-gen held a promoted embed in a **shared `ж<T>` box**, so the C# struct
assignment `copy = v` handed both sides one `object` and `copy.typ = typ` wrote into the ORIGINAL.
The origin's `struct{V T₁}` became `struct{V T₂}`; the second method then substituted `{T₁ → T₃}`
over a struct that no longer mentioned `T₁`, kept `T₂`, and `Identical(T₂, T₃)` correctly said no.
`cannot infer T` and `validType0`'s unbounded recursion are the same fact in their other costumes.

**Proven at golib level in 12 lines, no `go/types` involved** — a struct embedding another, one
plain assignment and one `c := *p`:

| | Go | C# (pre-fix) |
|:--|:--|:--|
| `a.v` after `b := a; b.v = 2` | `1` | **`2`** |
| `a.tag` (an ORDINARY field) after `b.tag = "b"` | `a` | `a` |
| `orig.v` after `c := *orig; c.v = 99` | `10` | **`99`** |

The ordinary field copies correctly; only the embed aliases. This is the gap
`GoValueCloneAttribute`'s own remarks had already NAMED and set aside — *"embedded-struct copy
aliasing is a separate, pre-existing gap"* — declared in the array-clone arc and never priced. Its
price was this wall.

**The fix is one field in `StructTypeTemplate`: the embed becomes an INLINE field.**

```csharp
private @object ʗobject;                                          // was: private readonly ж<@object> Ꮡʗobject;
[UnscopedRef] internal partial ref @object @object => ref ʗobject;  // was: => ref Ꮡʗobject.ValueSlot;
```

The box existed only because a struct member cannot ref-return its own instance state (CS8170);
`[UnscopedRef]` states the ref's lifetime is the receiver's — exactly the guarantee Go gives, since
the selection IS the enclosing value's storage — and the repo already used that technique in
`InheritedTypeTemplate`. Five emission sites in one file (field, accessor, promoted-field accessor,
two constructor initializers). **No converter change, no corpus regen, no golden churn**: the
generator's output is not committed, so the emitted `.cs` is byte-identical.

Two things improve for free: a `default(T)` reached where no constructor runs (a missing-key map
read, a freshly `make`d element) no longer carries a null embed box — the previously documented
residual gap narrows to embedded types that need construction in their own right; and the C# struct
`=` now means what Go's `=` means for every embedding type in the corpus.

**The one residue, named and unchanged.** A fixed ARRAY reached only THROUGH an embed is still
shared after a copy (`array<T>` is a struct over a shared `T[]`, and `typeNeedsValueClone` skips
embedded fields when deciding the `[GoValueClone]` stamp). It was shared before this fix and is
shared after, by a different mechanism. Widening the walk is now SOUND — the generated
`copy.<member> = <member>.ΔClone()` lands in the copy's own inline storage instead of corrupting the
source, which is precisely why it was excluded — but it moves converter EMISSION corpus-wide and
belongs to a change that owns that footprint.

### The unlock, MEASURED across all three dependents

| Package | Baseline | After | Verdict |
|:--|:--|:--|:--|
| `go/internal/gcimporter` | 475 of 583 (108 mismatches, all `TestImportTypeparamTests`) | **583 / 583, 0 mismatches** | **BANKED** — `status: validated`, 14 skipped identically |
| `go/internal/srcimporter` | 5 of 7 (`TestImportStdLib`, `TestImportedTypes` died on `sync.OnceValue(func() bool {…})` — *cannot infer T*) | **7 / 7, 0 mismatches** | **BANKED** — `status: validated`, `TestCgo` skipped identically |
| `go/types` | 202 verdicts (169 pass / 33 fail), then `0xc00000fd` in `validType0` at `TestFixedbugs/issue48951.go` | **513 verdicts, 512 pass + 1 skip, ZERO failures** | not banked — 44 verdicts never produced; a NEW wall, below |

`go/types` is the measurement that settles it: the type-parameter class is **entirely gone**. Every
`TestCheck/*`, every `TestExamples/*`, and every `TestFixedbugs/*` — issue48951 among them, the
invalid-recursive-generic that used to exhaust the stack because `Identical(e, t)` never fired —
now passes, and not one produced verdict disagrees with `go test`. Its 44 absent rows are one
process-killer, and it is a different animal (next entry).

### The NEXT wall, named: `TestSizeof` exhausts the stack in golib's reflect LAYOUT walk

> **RESOLVED 2026-08-15** (lane `claude/gosizeof-recursion`) — pre-existing golib defect, exactly as
> this section suspected but declined to assert; the standalone probe it asked for measured the
> cycle identically on both golibs. `go/types` banks at **557**. Entry at the end of this board.

`go/types`' run now dies at `TestSizeof` — the first test alphabetically past the ones that pass —
with an unbounded recursion whose frames alternate exactly two functions:

```
   at go.GoReflect.tryStructLayout(System.Type, IntPtr[] ByRef, IntPtr ByRef)
   at go.GoReflect.GoSizeOf(System.Type, IntPtr[])          x until the stack is gone
   at go.internal.abi_package.synthesizeDescriptor(...)
   at go.reflect_package.TypeOf(System.Object)
   at go.go.types_internal_test_package.TestSizeof(...)
```

It takes `TestSizeof` and everything alphabetically after it (`TestSpec/*` and the rest — 44
verdicts) and it is **not** the type-parameter class: `GoSizeOf` has no memo and no depth guard,
and the walk is reached the moment `reflect.TypeOf` is asked to synthesize a descriptor for a
go/types struct. Deliberately NOT attributed to the embed change on the evidence available: the
projection reports the SAME field TYPE for an embed before and after (the old arm unwrapped
`ж<T>` to `T`; the new one reads the inline field, whose type is `T`), so the walk sees the same
graph — but that is an argument, not a measurement, and nobody has run this test on the pre-change
golib because the type-parameter wall stopped the suite ~300 verdicts earlier. **Whoever takes it
should settle that first**, with a standalone `GoSizeOf` probe over the same types on both
golibs; if it is pre-existing it is a golib defect in its own right (a memo plus a cycle guard is
the obvious shape), and if it is not, the embed model is implicated and the probe says how.

One coupling the embed change DID break and this lane fixed: `GoReflect.collectGoFields`
recognized a promoted embed by the old box shape (a field named `ᏑʗName` whose type is `ж<T>`),
so an inline `ʗName` fell through to the generic arm and reported the Go field under its MANGLED
name. The projection now keys on the `ʗ` marker alone and reports the field's own type with no
pointer hop. It does NOT change the recursion above (measured: identical stack, identical 44
absent verdicts before and after), which is part of the evidence that the recursion is not the
embed's.

Guarded by the new **`EmbeddedStructValueCopy`** behavioral output test (assignment, by-value
parameter, a two-level `c := *p`, a slice-element read, and a pointer embed proving both halves of
Go's rule). Doctrine: `ConversionStrategies-Reference.md`, *An embedded struct is an INLINE field,
so a value copy copies it*.

## ⛔ MEASURED, DOES NOT BANK — the host-limit GATE is self-defeating on `os/exec`, and it costs 13 agreeing rows before it even gets there (2026-08-15, lane `claude/os-exec-gate-bank`)

> **RULING REVISED on this evidence, same day — the gate preference is WITHDRAWN and root B resolves
> as a DISCLOSURE.** `os/exec` banks at **74 matched · 27 disclosed (`host-limit`)** — see the
> disposition at the end of this entry. Everything below stands as measured; it is the evidence the
> revision rests on, not a live proposal.

The coordinator ruling delivered to this lane was to bank `os/exec` at 74 agreeing verdicts under the
GATE form: two declaration-keyed entries in `unsupportedRuntimeCapabilities`, tests excluded from the
run set, no disclosure. The gate itself works exactly as designed — the keys are right, the two
declarations are blocked, the manifest reads 38 included + 2 capability-blocked + 13 Phase-4D
excluded, and every surviving row agrees on both sides. **It still does not bank**, for two reasons
the ruling's evidence could not have contained, both measured here rather than argued.

### The keys, since the brief asked them to be verified empirically

`TestCommand` and `TestLookPathWindows` both live in `lp_windows_test.go`, which is `package
exec_test`, so the keys are the EXTERNAL test package's path:

```go
"os/exec_test.TestCommand":         "relocatable single-file test executable",
"os/exec_test.TestLookPathWindows": "relocatable single-file test executable",
```

`os/exec_test`, not `os/exec` and not `exec_test`. Both fired on the first attempt.

### Finding 1 — a declaration-keyed gate withdraws **40** rows, not 27, and `os/exec` under it is **61**

`eligibleTerminalTestResults` cuts a verdict row at its first `/`, so gating a declaration takes
every subtest with it — including the ones that were already agreeing. Measured on the same machine,
same toolchain, back to back:

| | rows | go | go2cs | agreeing |
|:--|:--:|:--|:--|:--:|
| ungated | 101 | 93 pass, 8 skip | 66 pass, 27 fail, 8 skip | **74** (66 pass + 8 skip) |
| gated (2 entries) | 61 | 53 pass, 8 skip | 53 pass, 8 skip | **61**, zero mismatches |

The 40 withdrawn rows are 18 under `TestCommand` (parent + 17 subtests) and 22 under
`TestLookPathWindows` (parent + 21). Only 27 of them were disagreeing; the other **13 are passes**
that the gate discards along with them — 10 of `TestLookPathWindows`' 21 subtests and 3 of
`TestCommand`'s 17. The reason a table-driven test splits at all is that not every case reaches a
copied executable: for `TestLookPathWindows` the correlation is exact, every failing case wanting a
`.exe` (which the ground-truth check then RUNS) and every passing case either expecting
`exec.ErrNotFound` — nothing is executed — or wanting a `.bat`, which `installBat` writes as text
rather than copying. So the choice is not "74 with a gate or 74 with disclosures" — it is **61 with a
gate or 74 with disclosures**. The 74 figure in the ruling is reachable only by the arm the ruling
declined.

The ungated re-measurement also reconciles the split the previous lane recorded as unreconciled: it
is **66 pass + 8 skip**, not 67 + 7. One row differs, and this run agrees with the arithmetic in both
directions (61 gated agreeing + 13 withdrawn passes = 74; 8 skips are all outside the gated set).

### Finding 2 — the gate ARMS a `TestMain` census that only the gated tests could have satisfied

This is the decisive one, and it is a property of gating rather than of `os/exec`. Under the gate the
run reports **zero mismatches** — and then the converted host exits 1 anyway:

```
helper command unused: "printpath"
```

`os/exec`'s `TestMain` runs a completeness census after `m.Run()`: every helper registered with
`registerHelperCommand` must have been marked used. `lp_windows_test.go`'s `init()` registers
`printpath`, and the ONLY callers of `maySkipHelperCommand("printpath")` are the two tests the gate
removed. Their `init()` still runs — a gate excludes declarations, not files — so the helper is
registered and never claimed.

**Gating the failures is what arms the census.** Go guards it with `code == 0`, so in the ungated run
the 27 failures made `m.Run()` non-zero and the census never executed; that is why no earlier
measurement of `os/exec` ever saw this line. The gate greens the suite, the census fires, and the
package fails at the process level with no failing test to point at. Under the gate `os/exec` does
not validate at 61 or at any other count.

**Why the precedent looked free.** `os_test.TestRemoveAllWithExecutedProcess` is gated under the same
capability name and has never shown this, because `os`'s `TestMain` is `Exit(m.Run())` with no census
— and because `os` is not on the roster, so nothing measures it. The precedent is therefore evidence
that the gate MECHANISM works, not that it is free.

### The general shape, and the seam where the gate form could be made viable

A capability gate is **invisible to the converted host**: the converter simply omits the declaration
from the emitted `TestRegistry`, so nothing at runtime knows a subset is running. Go's own vocabulary
for "a subset ran" is a non-empty `-test.run`, which is exactly what its census gates on
(`flag.Lookup("test.run").Value.String() == ""`), and `TestFlagBridge` publishes `test.run` as
`options.RunPattern` — empty on every pipeline run. So **any converted suite whose `TestMain` asserts
that the whole suite ran will answer wrongly while a gate is active.** `os/exec` is the first package
in the corpus where a gate and such a `TestMain` coexist; it will not be the last.

Making the gate arm viable therefore means making the host's `test.run` truthful: the converter would
have to emit the gated set into the generated host (it currently just drops it), and the bridge would
publish a pattern naming the included set. That is honest rather than lenient — it is Go's own
statement of the same fact — but it is a converter change plus a hand-owned `core/testing` change,
so it owes a full validated sweep, and **even then the package banks 61, not 74.**

### The disclosure arm, priced rather than assumed

Measured on the same binary with the gate disabled and a 25-entry signature manifest.

```
Validated 74 tests against go test (8 skipped identically on both sides,
27 disclosed-divergent (, host-limit), 13 disclosed-unsupported declarations excluded).
```

It **validates**, and at exactly the count the ruling named — 74 matched, 27 disclosed, a proof page
written. Nothing was banked from it: the artifacts are removed from this branch and the arm is
recorded here as a measurement, because admitting the class is the coordinator's call, not a lane's.

All 25 leaves carry `exit status 0x8000809a` (verified: 25 occurrences for 25 leaves) and both
parents carry no failure output of their own, so they ride `matchTerminalStatuses`' existing
disclosed-parent aggregation. Because the tests RUN and fail, `m.Run()` is non-zero and the helper
census never arms — Finding 2 does not exist on this arm. The `csErr` forgiveness path
(`goErr == nil && len(disclosed) > 0 && len(mismatches) == 0 && len(csResults) > 0`) covers the
host's nonzero exit.

The `(, host-limit)` in that line is quoted verbatim and is a pre-existing cosmetic defect, not a
symptom: the class list is built from `disclosures[name].Class` over every disclosed row, and the two
rows disclosed by the parent-aggregation rule carry no manifest entry, so the empty class joins the
set. Any package with an aggregated disclosed parent prints it (`encoding/binary`'s `TestSizeAllocs`
is the same shape). Recorded, not fixed here.

What it costs is doctrinal, and it is the cost the ruling already weighed: 25 signature entries
rather than 2 declaration keys, and a THIRD disclosure class alongside `alloc-profile` and
`codegen-liveness` — one that is not about a measurement the CLR cannot perform but about a test
whose whole premise the host cannot satisfy. `docs/ValidatedTestPackages.md`'s preamble defines a
disclosure narrowly enough that admitting this class is a decision, not an application.

### What landed on this branch regardless of the fork

The proof page now enumerates, per capability-gated declaration, **every verdict row `go test`
reports underneath it**, read from the UNFILTERED Go results (they exist nowhere after
`eligibleTerminalTestResults`) and published under a *Gated by a host capability* section. This was
the ruling's third annotation requirement, and it had to be a generator feature rather than a hand
edit, because a proof page is regenerated on every sweep and a hand-added section would be silently
overwritten. It is also what makes Finding 1 visible: without it a gate reads as "2 declarations
excluded" on the page while withdrawing 40 rows.

It is owed by the NEXT gated package to bank rather than by this one: three capability entries stand
today (`syscall.CommandLineToArgv`, `os_test.createMountPoint`,
`os_test.TestRemoveAllWithExecutedProcess`), all in packages not yet on the roster, and `os` — already
measured at 158/178 — carries two of them. Its page would otherwise claim a matched count while
saying nothing about the rows those gates take.

Guarded by `TestCapabilityGatedDeclarationsEnumerateSubtestRows` (the row roll-up, the sorted order,
the prefix-vs-segment distinction, and the empty case) and by
`TestDeclarationKeyedCapabilityEntries`, which pins the standing declaration entry by exact key and
requires any entry naming a `Test` to be keyed on an EXTERNAL test package path — a mis-keyed gate
fires silently, which is the trap this lane nearly walked into.

⚠ **The evidence for that feature is split across two halves rather than one end-to-end run, and it
has to be, today.** The producing half ran on real data — the gated `os/exec` comparison recorded
`TestCommand` → 18 rows and `TestLookPathWindows` → 22 — and the rendering half is pinned by the
fixture golden. What has never executed is the composition, because the page is written only for a
comparison that VALIDATES and no gated package validates yet (Finding 2 is why). The first package to
bank with a gate active is the end-to-end proof; until then this is two proven halves and one
function call between them.

**Not landed:** no roster row, no proof page, no committed test sources, no disclosure manifest —
and **not the two gate entries either.** `os/exec` remains unbanked and, more importantly, stays
MEASURABLE exactly as it was: 101 rows, 74 agreeing, 27 rooted failures. Landing the entries would
have traded that for one opaque process-level exit, so they live in this entry (verified, above,
ready to paste) and as a standing note beside `unsupportedRuntimeCapabilities` rather than in the map.
The branch carries the page feature, the guards, and this record.

### What is actually open, in the coordinator's terms

Three ways forward, each with its measured price:

1. **Gate, made viable** — publish a truthful `test.run` when declarations are gated (converter emits
   the gated set into the host; `TestFlagBridge` publishes a pattern). Costs a hand-owned
   `core/testing` change and therefore a full validated sweep; `os/exec` then banks **61**.
2. **Disclosure** — 25 signature entries land today and `os/exec` banks **74 matched · 27 disclosed**,
   with no new machinery. Costs a third disclosure class and a preamble that admits host limits
   alongside CLR-measurement limits.
3. **Neither** — drop the gate entries and leave `os/exec` unbanked but fully MEASURABLE at 101 rows
   with 27 rooted failures, which is what it is today. The cheapest, and it keeps the 74/27 shape
   visible for whenever the single-file-publish capability is actually built.

Option 3 is where this branch leaves things, because it is the only one a lane can choose on its own:
options 1 and 2 each change doctrine (a hand-owned host contract, or what the word *disclosure*
admits), and those are rulings. The one thing that must not happen is adding the gate entries without
option 1's work — that trades 27 visible, rooted, well-understood failures for a single opaque
process-level exit and makes the package measure worse than it does today.


### ✅ DISPOSITION (2026-08-15, ruling revised, lane `claude/os-exec-disclosure-bank`) — root B is a DISCLOSURE, `os/exec` BANKS at 74 + 27

The coordinator withdrew the gate preference on the measurement above and ruled root B a disclosure.
The deciding property is one this lane had priced only as a cost: **a disclosure keeps every row
running, so it retires itself.** A gate withdraws the 27 rows from the comparison entirely, which
means the day a self-contained single-file test host makes their premise true, *nothing happens* —
no signal, no failure, the entries simply stay forever. Disclosed, those rows keep executing and keep
being compared, so on that day they start passing, the disclosed arithmetic stops matching, and the
sweep fails loudly until the entries are removed. Add the 13 destroyed passes and the self-defeating
`TestMain` census, and the fork closes.

**Banked:** 74 matching (66 pass + 8 skip) + 27 disclosed-divergent. 25 leaf rows pinned by signature
on `exit status 0x8000809a`; the 2 parents carry no failure text of their own and ride
`matchTerminalStatuses`' disclosed-parent aggregation. Roster row 144;
`docs/validation/current/os.exec.md` names all 27 with class and reason.

**The third class, `host-limit`, is authorized and its bar is recorded** in two places —
`docs/ValidatedTestPackages.md`'s preamble (the reader-facing definition) and
`docs/ConversionStrategies-Reference.md`'s *"`host-limit` — the third disclosed-divergence class"*
(the full mechanism). The bar: an entry must pin a **structural** property of the current deployment
shape, provable from how the artifact is built, never an unimplemented-but-fixable defect. `log`
remains the negative control on the other side of that line — a Go-source position map would satisfy
its `.go:63` assert exactly, so it stays unbanked rather than disclosed around.

**QUEUED, not closed — the gate mechanism's truthful `test.run`.** The hazard Finding 2 exposed is
real and unfixed: a capability gate is invisible to the running host, so any suite whose `TestMain`
asserts that the whole suite ran will mis-answer while one is active. The hardening is to emit the
gated set into the generated host and have `TestFlagBridge` publish a `test.run` naming the included
set — Go's own vocabulary for "a subset ran". It is **load-bearing only when a gate meets a
completeness-asserting `TestMain`**, and nothing is broken today: the only gated declarations live in
`os` and `syscall`, and `os`'s `TestMain` is a bare `Exit(m.Run())`. It becomes owed the moment
either a gated package with such a `TestMain` is banked, or `os` itself banks with its gate entries
intact. Cost when it lands: a converter change plus a hand-owned `core/testing` change, therefore a
full validated sweep.

**One cosmetic defect left standing, deliberately.** The summary line prints
`27 disclosed-divergent (, host-limit)` — the empty class comes from the two aggregation-disclosed
parents, which carry no manifest entry and so contribute `""` to the class set. It is pre-existing
(any package with an aggregated disclosed parent prints it) and cosmetic. The generated proof page's
own preamble is likewise still worded for the two CLR-measurement classes only. Both are one-line
renderer fixes and BOTH were left alone on purpose: touching the renderer restyles all 144 proof
pages, and they only rewrite as each package is next re-validated, so the change would dribble a
whole-corpus docs diff through unrelated lanes. It belongs in a pass that re-validates the roster.
## ✅ CLOSED (2026-08-15, lane `claude/gosizeof-recursion`) — `TestSizeof`'s stack exhaustion is a PRE-EXISTING classification defect in `KindOf`, and `go/types` BANKS at 557

The wall named in the entry above is gone, and it was never the embed change's. `go/types` now runs
**557 verdicts, 557 agreeing with `go test`, zero mismatches** (1 skipped identically on both sides,
6 disclosed-unsupported declarations excluded) — `status: validated`. That is exactly the 513 the
type-parameter unlock produced plus the 44 the process-killer had been swallowing, and it is the
converted **Go type-checker itself**: `TestCheck`/`TestSpec`/`TestFixedbugs` over the language's own
conformance corpora, `TestStdlib` type-checking all of GOROOT from source, and `TestSizeof` — the
test that priced this defect — asking `reflect` for the amd64 size of all 25 type and object nodes.

### Attribution, settled FIRST and by measurement — pre-existing, not embed-implicated

The previous entry asked for this explicitly and declined to guess, correctly: the argument that the
projection reports the same field type before and after was an argument, not a measurement, and
nobody had run the walk on the pre-change golib because the type-parameter wall stopped the suite
~300 verdicts earlier.

A standalone probe settles it. It mirrors `GoSizeOf`/`tryStructLayout` exactly — same `KindOf`
classification, same `GoFields` projection — but carries a path stack, so instead of exhausting the
stack it NAMES the cycle. Built twice against the same converted `go/types` corpus, once against
golib at HEAD and once against golib at `48274ae2b` (the merge immediately BEFORE the embed change),
both runs print the same 30 lines:

```
  Named        CYCLE  Named -> Mutex -> SemaphoreSlim -> TaskNode -> TaskNode (REVISIT)
  ...
  SUMMARY cases=25 matched=24 mismatched=0 cycle=1 unknown=0 missing=0
  structs=89 cyclic=3     (Named, Context, lazyObject — one distinct cycle tail)
```

**Byte-identical on both golibs.** The other 24 of `TestSizeof`'s 25 cases already produced Go's
exact size on both. So: a golib defect in its own right, older than the embed model, and the embed
lane's decision not to attribute it to itself was right.

### Root cause — `KindOf` called a managed REFERENCE a struct, and Struct is the kind that descends

`KindOf`'s last line answered `Struct` for any reference type it did not otherwise recognize. Struct
is the one kind whose walks look INSIDE the type, so `GoSizeOf`/`GoAlignOf` — and
`StructFieldsComparable`, which `synthesizeDescriptor` calls on the very next line — enumerated the
CLR's own private fields and descended into the BCL object graph. A Go type graph cannot recurse
that way: Go's layout rule stops at every pointer, slice, map, chan, interface and func, and a Go
struct that contained itself by value would be a type Go itself rejects. The BCL graph has no such
rule, and `SemaphoreSlim`'s async wait queue is a linked list — `TaskNode.Next` is a `TaskNode` —
so the descent alternated `tryStructLayout`/`GoSizeOf` frames until the stack was gone.

The corpus entry point is `sync.Mutex`, hand-owned since 2026-07-11 on a lazily-created
`SemaphoreSlim` gate because Go's runtime sleeping semaphore has no managed form. Every struct that
holds one inherits the cycle: in `go/types` that is `Named` (via `mu sync.Mutex`), `Context`, and
`lazyObject` (via `sync.Once`). Nothing about this needed `go/types` — `fmt.Println` of any
`sync.Mutex`-bearing struct was on the same cliff.

**Why it was a process-killer and not a failure.** A `StackOverflowException` cannot be caught in
.NET. It took `TestSizeof` and every test alphabetically after it, and the run reported 44 ABSENT
verdicts rather than one failing test — the worst failure shape the campaign has, because absent
verdicts are unmeasurable rather than wrong.

### The fix — one classification line, then one memoized walk behind it

**1. The classification (the root cause).** go2cs emits every Go struct as a C# VALUE type — the
corpus carries exactly seven `[GoType] partial class` declarations and all seven are named-POINTER
types (`type P *T`), already classified `Pointer` structurally. So a reference type reaching that
final line is never a Go struct: it is an opaque managed handle, the backing object a hand-owned
shim holds in place of Go's own representation, and in the Go model a handle is one pointer word.
`KindOf` now says `Pointer`, the descent stops at the handle, and the answer is Go's own — a Go
`sync.Mutex` is 8 bytes and so, now, is the converted one. `Named` computes to **112**, which is
what `TestSizeof` wants.

**2. The durable shape on top.** Offsets, size and alignment now come out of ONE memoized pass
(`structLayoutOf`) instead of two walks that could disagree, and alignment is accumulated over every
field even after a size becomes unknowable, because the two questions are independent. A depth cap
(128) sits underneath as a safety net that answers "size unknown" — the r39d rule, a descriptor
field that cannot be read truthfully stays unpopulated — rather than overflowing. It is unreachable
by construction: only Struct and Array recurse, Struct is now answered for value types alone, and
C# forbids a value type from containing itself transitively (CS0523). Tripping it would mean the
CLASSIFICATION is wrong again, and the point is that the next such defect costs a wrong number
instead of a dead process and 44 unmeasurable verdicts.

**A cycle guard proper was deliberately NOT added.** The brief's requirement — a guard must produce
CORRECT sizes for legal self-referential graphs, not merely avoid crashing — is met by the
classification rather than by detection: a struct holding a pointer to itself terminates at the
pointer and answers 24, not "unknown". Guarded by
`GoStructLayoutTests.SelfReferentialThroughPointer_IsFiniteAndCorrect`.

### Guards

Three new tests in `src/tests/GolibTests/GoStructLayoutTests.cs`, the golib home of this walk
(114/114, was 111/111): a managed reference is one word and not a struct to descend into
(`SemaphoreSlim` included, so the real BCL graph is in the assertion); a self-referential managed
class terminates; and Go's own legal self-reference through a pointer is finite AND correct. The
first two are guards against a stack overflow, which no assertion can catch — reaching the assert at
all is the guard, and the value proves the walk stopped at the handle rather than merely stopping.

Doctrine: `ConversionStrategies-Reference.md`, *A managed reference is a Go POINTER, not a Go
struct — the reflection bridge's descent rule*.


## 💡 CAPTURED (2026-08-15, user, from the live NuGet page) — README badge layout + a partial-progress badge idea

**1. Badge-line wrap on NuGet — ✅ LANDED (lane `claude/badge-break`, merged 2026-08-15).** The four badges emit as one
markdown line (`readmeValidationBadge.go`), and NuGet's narrow README pane wraps them raggedly.
User ruling: hard line break between the docs badges and the source badges — line one carries
**Tests + Docs** (the Tests badge is the variable-width one and gets the room), line two carries
**Source·Go + Source·.NET**. Sequencing: land AFTER the in-flight second leveling regen merges
(the emitter change re-levels the whole README family at the next regen; published NuGet pages
pick it up at the next version push). Update `readmeValidationBadge_test.go`'s expectations in
the same change.

**2. Partial-progress badge for not-yet-validated packages — DEFERRED, captured verbatim.** User:
"does it make sense to carry partial test results, as a badge link to the 'not yet validated' so
users can get a sense of how close things sit?" Honest constraints, recorded so the future design
starts from them: the roster's integrity principle is validated-only rows (a package that almost
passes never appears — that is what keeps the denominator honest); partial counts have no
machine-maintained data source today (the sweep records validated packages only, and this board's
census entries are prose); and many unvalidated packages do not yet COMPILE their `-tests`
closure, so "how close" is not a number for them at all. A future shape would need a sweep mode
that records per-package partial verdicts and a badge/proof surface that cannot be mistaken for
validation. Revisit after the 75% terminal marker, possibly with the queued proof-renderer pass.

## ⛔ MEASURED FOR THE FIRST TIME, DOES NOT BANK — `database/sql` builds and runs at 133 of 139; the two compile roots are CLOSED and three divergence roots are named (2026-08-15, lane `claude/database-sql`)

The board carried `database/sql` as **0 of 25, CS0121 + CS0117** and the TB.* census listed it as
"36 sites, the widest in the corpus — pending its own measurement". Both compile roots are gone and
the suite now runs end to end: **139 rows, 133 agree, 6 disagree, 0 skipped, 0 disclosed**, 8
excluded (the standard Phase-4D `Benchmark`/`Example` deferrals). It does **not** bank — four of the
six are genuine divergences — but it is no longer build-blocked, and every remaining root is named.

**The recorded CS0121 was already closed, exactly as the tar lane predicted.** `stubDriverStmt(NilType)`
was the typed-nil constructor argument (`08afdd267`); it emits `new stubDriverStmt(want)` today and
never appeared in this lane's build. The prediction was worth acting on — but the roots BEHIND it
were two, not zero.

### Compile census — two roots, both general converter defects, both fixed here

| Diagnostic | Sites | Root |
|:--|:--:|:--|
| **CS0117** `'sql_package' does not contain a definition for 'Δtable'` | 6 | A box-field accessor (`Type.Ꮡfield`) qualified its owner with the PRODUCTION package class even when an internal `_test.go` declares the type. `fakedb_test.go`'s `type table` collides with `func (db *fakeDB) table(string)`, so the type is Δ-renamed — and Δ-renamed always qualifies — putting all six `t.mu.Lock()`/`Unlock()` sites in a class that has no such member. `packageScopeClassName` already drew the production/bridge line for package-level VALUE references; the accessor now uses it for the TYPE. Provably inert outside `-tests`: with no class override the helper returns the production class |
| **CS0029** cannot convert `ж<fakeStmt>` to `fakeStmt` | 1 | `func (s *fakeStmt) QueryContext` walks `s = s.next`. Go's pointer receiver is an ordinary local, so repointing it is legal; the converter deref-aliases the receiver to a value var, which cannot be repointed. `visitAssignStmt`'s repoint-and-re-alias arm (`Ꮡs = s.next; s = ref Ꮡs.DerefOrNull();`) has existed as long as the pointer-PARAMETER twin, but it is reachable only through the box — so it needed a direct-ж trigger the pre-pass did not have. `bodyReassignsReceiver` adds it, matched by object identity so a shadowing inner `:=` does not promote |

**The receiver-repoint trigger is corpus-invisible by construction, and that is why it surfaced in a
test file.** All ten production sites in the converted scope are *also* carried by a neighbouring
predicate — `container/ring`'s `Move` and `go/types`' `LookupParent` return their receiver;
`math/big`'s `fmtX`, `net/http`'s `addBytes` and `time`'s two `(*Location)` lookups already emit
`this ж<T> Ꮡx` — verified against the committed corpus before the change and re-verified by the
whole-stdlib A/B after it. (`internal/concurrent` and `testing` also match the shape and are
hand-owned, so they never convert.)

### Verdict census — 6 disagreeing rows, 4 distinct roots

| Rows | Root | Class |
|:--:|:--|:--|
| `TestGrabConnAllocs`, `TestRawBytesAllocs` | `testing.AllocsPerRun` want-zero asserts (`grabConn` 4 objects/run; `RawBytes` 15). The existing measured-allocation gap | **`alloc-profile`** — disclosable under the existing class if the other three roots ever close |
| `TestConversions`, `TestUserDefinedBytes` | **`abi.Type.Elem()` nil** — see below | real defect, `reflect`/`internal/abi` |
| `TestNullByteSlice` | **an untyped `nil` in a variadic slot vanished** — fixed here, see below | was a converter defect |
| `TestConnRaw` | `conn.Raw(func(…) { panic(…) })` must leave the connection CLOSED: `Raw`'s deferred cleanup sets `err = driver.ErrBadConn` while a panic is in flight and calls `release(err)`. The converted run panics correctly (the test's own `recover()` assertion passes) but `conn.dc` is still non-nil afterward, and `closeDB` then reports `1 connections still open`. Not rooted further by this lane — a defer-during-panic + named-result + capture question inside the converted `sql.cs` | real defect, unrooted |

**`TestNullByteSlice` was a CONVERTER root and is fixed.** `exec(t, db, "INSERT|t|id=10,name=?", nil)`
emitted `exec(…, insertTId10Nameˢ, default!)` against `params ꓸꓸꓸany argsʗp`. A typeless `default!`
converts to the params ARRAY as readily as to its element, and C# prefers a call's normal form over
its expanded one — so the argument bound as a null `any[]`, the callee saw `len(args) == 0`, and the
fake driver answered `sql: expected 1 arguments, got 0`. **A silent behavioral divergence, not a
compile error**, which is what makes it worth naming: the emission is valid C# that means something
else, and nothing but a differential run would have caught it. Every trailing argument of an
expanded variadic call that is the predeclared `nil` now states the element type — `(any)(default!)`.
A spread call is excluded (it passes the slice whole, so `describe(none...)` still yields 0).

**`TestConversions`/`TestUserDefinedBytes` are ONE root, and it belongs to `reflect`, not to
`database/sql`.** Both die in the same stack: `convertAssignRows` → `rtype.ConvertibleTo` →
`convertOp` → `haveIdenticalUnderlyingType` → `haveIdenticalType` → `nameFor` → the hand-owned
`reflect.Name`, which reads `Ꮡt.Value.t.sysType` and nil-dereferences. The nil comes from
`abi.Type.Elem()`: it reaches its element through the prefix-downcast idiom
(`Ꮡt.Reinterpret<Type, SliceType>()` and the Array/Chan/Map/Pointer siblings) — the exact route
`internal/abi`'s hand-owned `type_impl.cs` already documents as unusable under the managed bridge
("there is nothing behind a `ж<abi.Type>` to downcast to"), and which it replaced with SYNTHESIS
from the descriptor's carried `System.Type` for `StructType()` and `ArrayType()`. `Elem()` and
`Key()` were never routed through that synthesis, so they answer nil for a slice/pointer/chan/map
descriptor, and any `reflect` path recursing through them nil-derefs. `TestUserDefinedBytes` is the
minimal shape: `convertAssign(&u, v)` with `u userDefinedBytes` (a local named `[]byte`) and
`v []byte` reaches `sv.Type().ConvertibleTo(dv.Type())`, both Kind Slice, and the slice arm
immediately recurses on `Elem()`.

**This is a shared root worth its own lane, not a `database/sql` fix.** The remedy is the pattern
`type_impl.cs` already establishes — synthesize `SliceType`/`PtrType`/`ChanType`/`MapType` from
`sysType` and route `Elem()`/`Key()` through them, populating nothing that cannot be honored (the
r39d rule). It gates every `reflect.ConvertibleTo`/`AssignableTo` recursion in the corpus, not just
these two rows.

### Timing — a `$longTimeouts` floor is owed IF this package ever banks

The converted run took **1,712.7 s (28.5 min)** under `-test-timeout 30m`, i.e. it cleared the
deadline with about 90 seconds to spare on a quiet i7-5820K. At the sweep's default it would
false-red. Nothing is owed today (the package has no roster row), but the entry that banks it owes
`'database/sql' = '60m'` in `run-validated-sweep.ps1`'s `$longTimeouts` in the same change.

### What this lane banked

The two compile fixes and the variadic-nil fix, each with a guard and a reference-doc entry —
`bodyReassignsReceiver` (guard: `PointerReceiverRepoint`, a receiver-repointing list walk written to
avoid every other trigger), the bridge-qualified box accessor (guard:
`TestTestVariantBoxAccessorNamesBridgeDeclaringClass`, asserting both directions), and the variadic
untyped-nil cast (guard: the `VariadicSlotInterfaces` extension, arity in four positions against
three controls). Both compile-root guards were neuter-verified: reverted, each fails with the exact
diagnostic censused above. `database/sql`'s converted test sources are NOT committed — the
validated-package commit policy covers packages that validate, and this one does not.
## ✅ `crypto/tls` BUILDS — both recorded roots closed, a third behind them, and the RUN wall resolved into four named roots (2026-08-15, lane `claude/crypto-tls`)

**Both recorded roots reproduced verbatim** at master `6dd7547e4` before anything was believed —
same codes, same four call sites. That entry was accurate; the campaign's stale-premise rule cost one
93-second pipeline run to confirm and was worth paying. What was NOT accurate was its *attribution*
of the first root, and what nobody had seen was a **third root standing behind the first two**, which
is the reason a build-blocked package is only ever measured one wall at a time.

Four errors → **zero. The package compiles for the first time**, and its host runs.

### Root 1 — CS0012 ×3 is NOT a missing reference; it is the Phase-4D exclusion being too conservative

Recorded as "a fresh instance of the `-tests` reference-closure family". It is not: adding the
reference cannot fix it and makes the diagnostic worse. `crypto/tls` selects the whitebox-reference
model and then FALLS BACK to **recompile** (`recordsRequireProductionMutation`), which compiles the
production `.cs` into the test assembly. `example_test.go` is the package's ONLY black-box file, and
its Examples reach `http.Transport{TLSClientConfig: …}`, `http.Server{TLSConfig: …}` and
`httptest.Server.TLS` — fields whose type is `tls_package.Config` **in the production assembly**,
while the recompile has just made a second, distinct local copy. The field is therefore unnameable.
Referencing `crypto.tls` as well would let the compiler *name* the type and then reject the
assignment between the two copies: CS0012 becomes CS0029. The file simply must not be compiled.

Which is what the Phase-4D file-exclusion ruling exists to do — go/token's `example_test.go` is the
recorded precedent, *the same failure in the same model*. `crypto/tls`'s file differs in one respect:
its Examples need an `io.Reader` to hand `Config.Rand`, so it declares `type zeroSource struct{}` and
one `Read` method, and condition (1) disqualified the whole file for it. **Condition (1) now admits
pure TYPE declarations and METHODS** — they have no run-time behavior, nothing executes at package
init, and any use by a retained file is a reference condition (2) already resolves. The type and
method objects are now recorded in `declared`, without which the widening would have silently
disarmed condition (2) for exactly the declarations it just admitted. `var`/`const` (initializer side
effects) and plain funcs (`init`) stay disqualifying. Doctrine + the two new unit guards:
`ConversionStrategies-Reference.md`, *An Example/Benchmark-ONLY test file is dropped from the compile
set*.

**Phase-4D widening confirmed by coordinator ruling 2026-08-15; the 2026-07-24 conservatism's safety
property is preserved by the declared-object fixpoint.** The lane flagged this as a ruled boundary
being moved rather than assuming it. The original ruling's "conservative by design" existed to
guarantee one property — nothing a test file declares can vanish from the compilation while anything
still references it — and the widening RETAINS it, because the admitted type and method objects are
recorded in `declared`, so condition (2)'s fixpoint still forces the file back in whenever a
cross-file reference exists. What it removes is a false positive the alternative cannot fix at all
(the recompile model's duplicate type: adding the reference turns CS0012 into CS0029, never into a
fix). A conservatism whose protective content survives the refinement intact is not weakened by it,
only made precise. The three conditions the ruling attached are met and deliberately kept visible:
the fixpoint protection is stated in the code where the widening lives
(`classifyTestFileForExclusion`), the two unit guards
(`TestSelectCompileExcludedTestFilesDropsExampleWithHelperType`,
`…KeepsHelperTypeUsedByRetainedTest`), and the reference-doc section.

### Root 2 — CS1503 is a delegate-VALUED comparator, and the first fix silently did nothing

`isBetter := func(a, b uint16) int { …; return -1; …; return +1; …; return 0 }` passed to
`slices.IsSortedFunc`. All-INT-literal arms emit **bare**, so C# infers `Func<ushort, ushort, int>`
against a Go `int` (`nint`) result. Every *call* of the variable accepts that (`int` converts to
`nint`) — only the delegate-valued use rejects it, delegate types being invariant. `convFuncLit` now
states the declared return type for that arm set.

Two things this cost, both worth carrying:

1. **`numericBasicLit` stripped unary `-` but not unary `+`.** The comparator returns `-1` AND `+1`,
   so the predicate saw a non-literal arm, suppressed itself, and the first fix emitted **nothing at
   all** — a green build of a converter that had not changed its output. Go writes an explicitly
   positive literal precisely where it pairs with a negative one, which is the comparator shape this
   arm exists for. Both signs are stripped now.
2. **Two successive cuts OVER-applied, and only the emitted C# showed it.** The natural rule — "any
   numeric literal whose natural C# type differs from the declared result" — is wrong, because the
   converter does not emit every literal bare: a declared FLOATING result carries its width into the
   literal (`func() float32 { return 0.5 }` → `0.5F`; `func() float64 { return 3 }` → `3D`), so those
   already infer correctly. Only a declared INTEGER width other than `int32` leaves the literal bare.
   **A predicate about C# inference must be measured against emitted output, never derived from the
   Go-side type.** All five shapes are pinned side by side in the extended
   `FuncLitUntypedConstReturn` behavioral guard.

### Root 3 — CS7013: the hoisted-literal slug budget never bound the first word

Behind the other two, and previously unseen. `key_schedule_test.go` carries a **2,176-character** hex
test vector; the hoist slug is built from the literal's own content and truncated at a word boundary
within a 24-character budget — but the truncation only applied once the slug was non-empty, so the
leading word was written whole. A literal that is ONE long word became an identifier of exactly its
own length: `CS7013: Name '…' exceeds the maximum length allowed in metadata`.

The committed corpus was already past the design's intent without failing — **33 of 5,928** hoisted
names exceed 24 characters, the longest **256** — so this was luck, not a boundary case. Raising the
number would not close the class; making the budget total does, and `len(literalSlug(v)) ≤ 24` is now
an invariant. Those 33 inline instead of hoisting; **zero** behavioral goldens move.

**⚠ The A/B footprint splits 11 / 22, and the second half is LATENT — expect it, do not re-diagnose
it.** A seeded whole-corpus reconvert (single run, `version.props` + `docs/validation` seeded, marker
gate **52 marked / 0 violations**) emitted 1,960 files and changed 352, and the accounting closes
exactly:

- **~297 `README.md`** — the `badge-break` line-break emitter, already merged to master with its
  corpus re-level still pending. **Not this arc's**, and it will vanish at the next leveling regen.
- **49 `.cs` CRLF phantoms** — the known in-string-LF class; CR-stripped comparison is identical.
- **6 `.cs` real content diffs** — every one this fix, every one an over-budget first word now
  inlined (`crypto/elliptic/nistec.cs`, `go/parser/parser.cs`, `image/ycbcr.cs`,
  `os/exec/windows/lp_windows.cs`, `runtime/windows/os_windows.cs`,
  `syscall/windows/syscall_windows.cs`). **0 new files, and ZERO func-literal changes corpus-wide** —
  the root-2 fix has no stdlib footprint at all, which together with CNR's single-file result is two
  independent confirmations that its gate is tight rather than merely plausible.

That is 10 of the 33 names. The other 23: **one** is
`crypto/x509/internal/macos/darwin/security.cs`, darwin-exclusive and correctly not emitted by a
windows-target run; the remaining **22 live in committed `*_test.cs` sources** across ~14 banked
packages (`regexp`, `bufio`, `bytes`, `context`, `compress/flate`, `crypto/{dsa,ecdh,rsa}`,
`encoding/base32`, `go/scanner`, `internal/cpu`, `mime/multipart`, `os/exec`, `path/filepath`,
`strings`), which a `-stdlib` run never re-emits. They will re-level at each package's next `-tests`
run and show up in a future sweep or leveling regen as unexplained-looking test-source churn. It is
this fix, it is expected, and it is benign.

### The host RUNS — 23 of 184 top-level tests before the deadline, and the roots are named

⚠ **A live-diagnosis correction, recorded because the method failed, not just the guess.** This lane
first reported the host as "blocked from the start — 8.3 s CPU across twenty minutes, flat across
eleven samples, I/O-blocked". The CPU reading was accurate and the inference from it was **wrong**.
The host runs the suite alphabetically and got through **23 top-level tests in roughly the first
minute**, most of them failing fast; it then blocked inside `TestConnCloseBreakingWrite` and spent the
remaining ~38 minutes of the deadline parked there. The first CPU sample was taken about nine minutes
in, by which time the interesting part was already over — so a flat sample series was read as "never
started" when it meant "already finished the part that moves". **Flat CPU dates a stall; it cannot
date its beginning.** Sample from t=0 or read the verdict stream; do not infer a run's shape from
its tail.

**The measured outcome** (converted host, `-test-timeout 40m`): **26 pass, 12 fail, 1
infrastructure-error, 1 package timeout** over **23** top-level tests reached. Go's own run of the
same suite on the same host executes **184** top-level tests (1,251 pass / 2,381 skip / 12 fail).
Per the jsonrpc rule this board already applies, *a package that dies mid-run has one failure and an
unknown remainder* — the 26 is a **floor, not a census**, and no differential is claimable from it.

Four distinct roots, in order of weight:

1. **`localPipe: dial tcp 127.0.0.1:…: failed to find ConnectEx: An invalid argument was supplied.`
   ×9** — the dominant one, and far more specific than "the net stack". `localPipe()` dials a
   loopback listener, and the converted Windows socket layer cannot resolve the **`ConnectEx`
   extension function pointer** (`WSAIoctl` / `SIO_GET_EXTENSION_FUNCTION_POINTER`). Every
   handshake-driving test dies on it in ~2 ms. This is **S2b netpoll + `syscall`** territory and is
   the priority signal this lane hands over.
2. **`TestConnCloseBreakingWrite` blocks indefinitely** and consumes the entire remaining deadline —
   the reason nothing past `TestConn…` was reached. A separate defect from #1 (it does not fail fast;
   it never returns) and the single highest-leverage fix for coverage, since the alphabet beyond it is
   unexplored.
3. **A golib REFLECT gap, not a net one** —
   `System.InvalidOperationException: Not a pointer box type: go.sync_package+RWState` in
   `GoReflect.slotAccessorShape` (`golib/GoReflect.FieldAccess.cs:125`), reached
   `reflect.DeepEqual` → `deepValueEqual` → `deepValueEqualBoxed` → `reflect.Elem`, from
   `TestCloneNonFuncFields`. `sync.RWMutex`'s `RWState` box is not recognised as a pointer box, so
   `DeepEqual` cannot descend a struct containing one. Independently actionable, unrelated to sockets,
   and it will bite any package that `DeepEqual`s a struct holding a `sync` primitive.
4. **`TestCertCache`: "timed out waiting for expected ref count"** — weak-reference/GC observability,
   the `internal/weak` family. **`TestBogoSuite`** fails against the external BoGo shim and is not a
   conversion signal.

**One result worth stating: `TestCipherSuites` PASSES.** That is the exact test whose comparator
produced the CS1503 above — so root 2's fix is validated *by execution*, not merely by compiling.

⚠ **Go itself fails 4 top-level tests on this host** — `TestResumption`, `TestVerifyConnection`,
`TestResumptionKeepsOCSPAndSCT`, `TestCrossVersionResume` (12 verdicts with subtests). Whoever takes
the differential must treat those as host-environmental until proven otherwise, exactly as the `os`
`SHARE_INFO_2` retraction demands.

**Not banked, and not bankable on this evidence.** Builds-and-partly-runs is precisely the partial
result the roster's validated-only integrity principle excludes — no roster row, no proof page, no
disclosures. The three converter fixes and their guards are the deliverable; `crypto/tls` returns to
the board as a package whose *build* question is answered, whose *run* question is now four named
roots rather than one vague one, and whose next move is `ConnectEx` plus the
`TestConnCloseBreakingWrite` hang.

## ✅ CLOSED (2026-08-15, lane `claude/reflect-bridge-gaps`) — both named reflect-bridge gaps, and one root that stood behind them

Two entries on this board named two independent defects. Both reproduced verbatim before anything
was believed, both are fixed at the classification/routing principle rather than at the symptom, and
a **third** root of the same family stood behind the first — the pattern this board keeps recording:
a wall is only ever measured one layer at a time.

### Gap 1 — `abi.Type.Elem()` / `Key()` (and `Len()`) reached their answer by prefix downcast

Recorded as "`Elem()`/`Key()` were never routed through the synthesis path, so they return nil and
`reflect.Name` nil-derefs". Accurate, and the reproduction is sharper than "returns nil": a program
whose first statement is `reflect.TypeOf([]byte(nil)).ConvertibleTo(reflect.TypeOf(myBytes(nil)))`
**panics before printing a single line**.

That is the whole point of this root. `StructType()`/`ArrayType()` also answer nil when a layout is
unknowable, and every Go caller of those tests the nil. **Nothing tests `Elem()`.** Go's
`haveIdenticalType` recurses straight into `nameFor(t)`, which reads the descriptor's carried
`System.Type` — so a nil element is not a wrong answer that propagates, it is a nil dereference that
ends the process. `ConvertibleTo`/`AssignableTo` over any operand that is not a scalar was
unreachable corpus-wide.

Both are now hand-owned in `internal/abi/type_impl.cs` (`Type.Elem` / `Type.Key` in
`manualConversionFuncs`) and synthesized from the carried `System.Type` over
`GoReflect.ElementType` / `KeyType` — **the same golib resolution `reflect`'s own hand-owned
`rtype.Elem`/`rtype.Key` already use one layer up**, so the descriptor layer and the `reflect` layer
cannot disagree about what an element type is. The array-dims cargo threads by the rule `rtype.Elem`
already applies: an array's element takes the tail of `[outer]…[inner]`, a pointer's dims are the
pointee's and pass through unshifted.

**The third root, found by the guard rather than by the census.** With `Elem()`/`Key()` fixed the
new behavioral test still disagreed with `go run` on the ARRAY rows, and `Type.Len()` is why: it is
the same downcast, and its failure is the *nastiest* of the three, because it does not answer nil —
it reads a `uintptr` out of the memory following the descriptor's value slot. Two `[3]byte`
descriptors therefore read two DIFFERENT numbers and `haveIdenticalUnderlyingType` reported
`[3]byte` and `[3]byte` as different types. `Type.Len` is hand-owned too, over the same carried
dims; a length no source knew still answers Go's 0, so two dimension-less array descriptors compare
equal rather than randomly unequal.

### Gap 2 — `slotAccessorShape` threw because pointer KIND was being read as pointer BOX

Recorded as a golib descent-rule sibling of the `KindOf` fix, and that attribution is exactly right.
`KindOf` classifies every managed REFERENCE it does not otherwise recognize as `Pointer` — the fix
that stopped the layout walks descending into the BCL object graph. What that settled was the
LAYOUT question. It left a second, different question for the VALUE walks: *is there a slot behind
this handle at all?*

For an opaque handle there is not — a hand-owned shim's backing object (`sync.Mutex`'s
`SemaphoreSlim` gate, `sync.RWMutex`'s `RWState`, `sync.WaitGroup`'s `WaitGroupState`) stands in for
a representation Go has and the CLR does not, and nothing behind it has a Go form. So **"one word
wide, do not descend into it" is also "no slot, do not read through it"** — one rule at two layers,
of which only the first half had been stated.

`reflect.Value.Elem` asked the wrong one: it resolved a pointee with `GoReflect.ElementType` and, on
null, fell through to a "detached read" through `ReadPointerSlot`, which classifies the box shape
itself and threw `Not a pointer box type: go.sync_package+RWState`. The classification now lives in
ONE place — `GoReflect.TryPointerBoxElement`, which `slotAccessorShape` is refactored onto — so "can
I read through this?" and "what will I read?" can never be answered by two different probes.
`reflect.Value.Elem` and its `internal/reflectlite` twin ask first and answer the **invalid Value**
for a handle, which is what they already answer for a nil pointer.

**The resulting blindness is Go's own answer, not a concession.** Go's `sync.RWMutex` is state
*words*, and a used-then-released lock is back at its zero state, so two of them are deeply equal —
which is what two handles now compare as, whether or not the shim has lazily created one of them
(`crypto/tls`'s `TestCloneNonFuncFields` is the measured consumer). Real Go state beside the handle
is still seen: a `sync.Once` that has run differs from a fresh one, because `done` is an ordinary
field and not part of the handle. The `DeepEqual` behavioral test asserts both directions.

### Three adjacent roots this lane measured and deliberately did NOT chase

The new guard was written to cover every element-bearing kind and was then TRIMMED to what the
corpus can truthfully produce, because a guard that asserts an answer the corpus cannot give is a
standing false red, and one that asserts the current answer pins a defect as a contract. Each row
removed is named here instead:

| Surface | Root | Why not here |
|:--|:--|:--|
| `AssignableTo` (all kinds) | `reflect`'s `rtype.AssignableTo` is hand-owned as identity-on-the-managed-type plus interface-implements | A **recorded** deferral — and as of this lane it HAS a measured consumer, `database/sql`'s `TestUserDefinedBytes` (see below). Still a of Go's unnamed↔named underlying rule, and retiring it is not the one-liner it looks like: a synthesized descriptor never sets `TFlagNamed`, so `HasName()` is false for every type and `directlyAssignable`'s first gate would call two DISTINCT named types with one underlying type assignable — which Go rejects. Needs `HasName` before it needs the hand-own removed |
| STRUCT identity | `haveIdenticalUnderlyingType`'s struct arm downcasts `ж<abi.Type>` to `structType` DIRECTLY rather than through the synthesized `StructType()` | Reads zero fields, so any two structs of equal field count compare identical — a silent FALSE POSITIVE, measured: `struct{B []byte; M map[string]int; …}` and the same struct with `M map[string]int64` are reported convertible. The fix is a `reflect`-level hand-own, not an `abi` one |
| CHAN identity | its arm compares `ChanDir()` first, the same downcast | The only one of the family with **no synthesis waiting for it**: `<-chan int` and `chan int` are both `channel<nint>`, so a direction is not recoverable from the managed type for an unnamed directional channel. This one needs a ruling, not a fix |

`MapType()`, `FuncType()` and `InterfaceType()` remain on the same list for the reason they always
were — each awaits a measured consumer, and a synthesized `ΔMapType` would have to populate
runtime-map fields (`Hasher`, `KeySize`, the indirect-key/elem flags) that have no managed answer.

### Guards

- **`ReflectConvertAssignable`** (new behavioral test) — `ConvertibleTo` across slice, map, pointer
  and array, named and unnamed, in both directions, with differing-element and differing-key
  negatives so an accessor that answered a constant would fail too; plus `Len()` over equal and
  differing lengths, and an element/key kind readback through the public `Type` surface. 14 lines,
  compared to `go run`. **Neuter-verified in the right order**: measured as a nil-deref panic
  *before* the fix.
- **`DeepEqual`** (extended) — a `guarded` struct holding `sync.Mutex`, `sync.RWMutex` and
  `sync.Once`, compared through pointers, slices and maps after the locks have been used and
  released. Also measured failing first, with the exact `slotAccessorShape` stack.
- **`GolibTests.PointerNilPredicateTests`** (117/117, was 114) —
  `OpaqueManagedHandleIsPointerKindButNotAPointerBox` asserts BOTH halves of the rule in one test
  (either alone would let the other drift back), `PointerBoxShapesResolveTheirPointee` pins the
  positive side so the fix cannot turn real dereferences into nil, and
  `ElementAndKeyResolveForEveryKindTheDescriptorMustServe` pins the golib resolution the `abi`
  synthesis now stands on.

Doctrine: `ConversionStrategies-Reference.md` — *`abi.Type`'s SPECIALIZATIONS are synthesized, not
downcast* (extended with `Elem`/`Key`/`Len`) and *A managed REFERENCE is a Go pointer, not a Go
struct* (extended with a new subsection, *The VALUE side of the same rule: pointer KIND is not
pointer BOX*).

### Corpus footprint, measured

A seeded whole-stdlib reconvert A/Bs to **exactly one** regenerated file, `internal/abi/type.cs`
(three Go bodies replaced by placeholders). Two families of difference the same A/B reports are
**pre-existing regen debt at master, not this lane's and deliberately not carried**: 300 package
`README.md` files (the merged badge-line break awaiting its re-level) and seven `.cs` files
(`crypto/elliptic/nistec.cs`, `go/parser/parser.cs`, `image/ycbcr.cs`, `os/exec/windows/lp_windows.cs`,
`runtime/windows/os_windows.cs`, `syscall/windows/syscall_windows.cs`, `testing/slogtest/slogtest.cs`
— string-literal hoisting and the variadic untyped-nil cast, both banked without a corpus regen).

### `database/sql` — measured again after the fixes: **135 of 139**, and the last non-alloc row is now ROOTED

Re-run end to end (`-test-action all -test-timeout 60m`; 3,423 s under concurrent lane load, vs the
1,712 s the previous lane measured solo — the machine, not the package). The census moves from
**133 agreeing to 135**, and the change is exactly what this lane predicted plus one it did not:

| Row | Before | Now |
|:--|:--|:--|
| `TestConversions` | `abi.Type.Elem()` nil → `reflect.Name` nil-deref | **passes** |
| `TestUserDefinedBytes` | the same nil-deref | fails DIFFERENTLY, and the new failure is rooted below |
| `TestGrabConnAllocs`, `TestRawBytesAllocs` | `AllocsPerRun` want-zero | unchanged — the standing **`alloc-profile`** class |
| `TestConnRaw` | `conn.dc` non-nil after a callback panic | unchanged, still unrooted |

**It does NOT bank**, and the reason is worth more than the row would have been.

**`TestUserDefinedBytes` is now a MEASURED CONSUMER of the `AssignableTo` deferral.** The test does
`convertAssign(&u, v)` with `u userDefinedBytes` (a named `[]byte`) and `v []byte`, then asserts
`&u[0] != &v[0]` — "got potentially dirty driver memory". `convertAssignRows` has two arms in
sequence:

```csharp
if (sv.IsValid() && sv.Type().AssignableTo(dv.Type())) {
    case slice<byte> b: dv.Set(reflect.ValueOf(bytes.Clone(b)));   // arm 1 — CLONES
}
if (dv.Kind() == sv.Kind() && sv.Type().ConvertibleTo(dv.Type())) {
    dv.Set(sv.Convert(dv.Type()));                                 // arm 2 — SHARES the array
}
```

Go takes **arm 1**: `[]byte` is an unnamed type and `userDefinedBytes` is named with the identical
underlying type, so Go's assignability rule admits it, and the `[]byte` case clones. The converted
run takes **arm 2**, because `rtype.AssignableTo` is hand-owned as identity-on-the-managed-type and
the two managed types are distinct — so `Convert` hands back a view over the driver's own array and
the assertion fires.

Two things follow. First, arm 2 is *reachable at all* only because this lane's `Elem()` fix made
`ConvertibleTo` answer: before it, the same row died in `nameFor(nil)`. The fix moved the test from
a panic to the wrong arm, which is progress and is also how the root became visible. Second, the
`AssignableTo` entry in the table above should now be read as **has a named consumer**, not "awaits
one" — and the sequence it needs is fixed: `HasName()` must become truthful on a synthesized
descriptor *before* the hand-own can retire, and the struct and chan arms of
`haveIdenticalUnderlyingType` must be fixed *with* it, or retiring the hand-own trades one wrong
answer for a wider one (every two structs of equal field count would become assignable). That is a
second arc of this same family, not a tail of this one.

**The `$longTimeouts` floor is still owed if it ever banks** — `'database/sql' = '60m'`, per the
previous lane's note. Nothing is added today: the package has no roster row, and a floor for a
package the sweep never visits would be dead configuration.

Converted test sources are **not** committed, per the validated-package commit policy — that policy
covers packages that validate, and this one does not.

## ⛔ RE-MEASURED, DOES NOT BANK — `crypto/tls` reaches **127 of 180** Go-passing top-level tests; three of the four named roots are CLOSED and the whole remaining divergence is ONE converter defect class in two shapes (2026-08-16, lane `claude/tls-remeasure`)

The 2026-08-15 entry left four roots and said the next move was `ConnectEx` plus the
`TestConnCloseBreakingWrite` hang. Both landed (netpoll S2b), the reflect-bridge lane closed a third,
and this lane re-measured the package on the post-poller tree. **The three fixes all hold, and none of
the remaining divergence is a poller defect.**

### The headline, and the number that went DOWN

| Measure | 2026-08-15 | now |
|---|---|---|
| top-level tests the ONE-PROCESS run reaches before parking | 23 | **7** |
| top-level tests that PASS when each is run on its own | *not measured* | **127 of 180** |
| C# verdicts agreeing with Go | 26 (floor, uncensused) | **127 top-level / 274 incl. subtests** |
| divergences rooted or named | 4 roots, remainder unknown | **53 of 53, zero unrooted** |

⚠ **The single-process floor got WORSE, and that is progress, not regression — do not read the 23 → 7
as a loss.** Under the old `ConnectEx` failure every handshake-driving test died in ~2 ms, so the
alphabet advanced fast and *then* parked. Those same tests now do real work, and one of them parks
forever, so a single package deadline buys fewer completed tests than a fast-failing suite did.
**Once a suite contains a hang, "how far the one-process run gets" stops measuring capability and
starts measuring where the first hang sits.** The per-test census below is the measurement that
answers the question the floor was standing in for; the floor number is kept only because it is what
the previous entry recorded.

**Method.** Each of the 190 top-level `Test*` functions in the staged `_test.go` set was run in its
OWN process (`-run '^Name$' -timeout 20s`), so one hang costs 20 s instead of the whole package
deadline. 6 of the 190 are `//go:build boringcrypto` and are absent from Go's run — 190 − 6 = **184**,
which is exactly the count Go executes, so the enumeration is confirmed rather than assumed. Go's own
run on this host reproduces the previous entry's numbers exactly: **184 top-level (180 pass, 4 fail);
1,251 pass / 2,381 skip / 12 fail at all levels**, 46.5 s.

C# per-test: **127 PASS, 34 FAIL, 13 HANG, 9 infrastructure-error, 1 process CRASH** = 184, and the
cross-tab against Go closes with no remainder: 127 agreeing passes + 53 real divergences (Go passes,
C# does not) + 4 both-fail + 6 build-tag-excluded = 190.

### The four roots, re-measured

| # (2026-08-15) | verdict now | evidence |
|---|---|---|
| 1. `failed to find ConnectEx` ×9 | ✅ **CLOSED** | **zero** occurrences of the string across all 190 test outputs; ESTABLISHED loopback pairs observed live (`127.0.0.1:42281 ↔ :42159`); full TLS **1.2** handshakes complete end-to-end (`TestClientAuth/TLSv12`, `TestConnectionState/TLSv12`, …) |
| 2. `TestConnCloseBreakingWrite` blocks forever | ✅ **CLOSED** | **PASSES**, 3.5 s. S2b's second acceptance anchor holds at suite level, not just at golib level |
| 3. `Not a pointer box type: sync_package+RWState` | ✅ **CLOSED** | **zero** occurrences corpus-wide in the outputs; `TestCloneNonFuncFields` **PASSES** |
| 4. `TestCertCache` ref-count timing | ⛔ **still open** | `timed out waiting for expected ref count` — unchanged, 1 divergence |

### The 53 divergences, every one rooted

| count | root |
|---:|---|
| **30** | **A** — `ticketKey.aesKey` is length 0 → `tls: failed to create cipher while encrypting ticket: crypto/aes: invalid key size 0`. Every TLS **1.3** session-ticket path |
| **10** | **B** — `net/netip.As16` slices a zero-length array → `System.ArgumentException`, full stack captured |
| **7** | **B-shaped** — silent hang, zero verdicts: server parked in `pollBlock` reading the ClientHello while the client goroutine is *gone*. Same stack shape as B; the killing exception is NOT captured (see "why it hangs") |
| **2** | **F** — hang after partial progress (`TestConnectionState`, `TestHandshakeKyber`: TLS 1.2 subtests pass, the 1.3 subtest parks) |
| **1** | **D** — `TestCertCache` weak-ref timing (root 4 above) |
| **1** | **E** — `TestBogoSuite` against the external BoGo shim; not a conversion signal |
| **1** | **G** — `TestQUICHandshakeError`: `panic: runtime error: invalid memory address or nil pointer dereference` at `golib/ж.cs:957` `ж<T>.op_OnesComplement` |
| **1** | **H** — `TestVerifyHostname`: **process access violation `0xC0000005`** in `syscall.GetAddrInfoW` |

**A and B are the SAME converter defect**, and together they are 47 of the 53.

### The root: the zero value of an array-bearing type is emitted as `default!`

C# `default` produces the all-zero value and **does not run field initializers** — those run only
through a constructor. go2cs's `array<T>` is a struct carrying its own `m_length`, so a `default`
one has length **0**, not `N`.

**Shape 1 — a `[N]T` named return.** `net/netip`'s `func (ip Addr) As16() (a16 [16]byte)` becomes
(`src/core/net/netip/netip.cs:718`):

```csharp
public static array<byte> /*a16*/ As16(this ΔAddr ip) {
    array<byte> a16 = default!;                      // ← length 0, not [16]byte
    byteorder.BePutUint64(a16[..8], ip.addr.hi);     // ← throws here
```

**Shape 2 — a struct whose fields are fixed arrays.** `crypto/tls`'s `ticketKey` declares
`internal array<byte> aesKey = new(16);`, and `ticketKeyFromBytes` opens with
`ticketKey key = default!;` (`src/core/crypto/tls/common.cs:747`) — which skips exactly that
initializer, so `copy(key.aesKey[..], …)` copies nothing and `aes.NewCipher` is handed 0 bytes.

**The converter already knows the right emission** — three lines from the broken one,
`src/core/net/ip.cs:529` builds the same zero value correctly as `new byte[]{}.array(16)` in
composite-literal position. Only the zero-value *declaration* path emits `default!`.

**Census (Windows target, committed corpus — this is a SHIPPED defect, not something this run
emitted; `git status` on `src/core/net/netip` is clean).**

* **10** sites of the direct `array<T> x = default!;` form, in 5 packages: `net/netip` ×2
  (**`As16` and `As4`**), `vendor/golang.org/x/crypto/sha3` ×4, `internal/pkgbits` ×1,
  `syscall` ×3 (linux/darwin only).
* **24** sites declaring `= default!` on one of the **269** structs that carry `array<…> … = new(…)`
  field initializers. Not all 24 are live — a site that assigns the whole struct afterwards is
  harmless — but `ticketKey` is proven live by execution.

**Why it HANGS instead of failing, which is the expensive half.** The throw is a plain
`ArgumentException` from `slice<T>`'s **constructor** (`golib/slice.cs:227`), not the
`RuntimeErrorPanic.SliceBoundsOutOfRange` that the reslice path raises. A non-panic exception
satisfies `Goroutine.CanContain`, so the test host *contains* it and records it on the
`TestExecution`. If the test then completes, the record flushes and you get the
`INFRASTRUCTURE-ERROR` line with the full stack (the 10 B rows). If the dying goroutine was the one
another goroutine was waiting on, the test never completes, the record never flushes, and the whole
package deadline burns with **no output at all** (the 7 B-shaped rows). A Go-visible `panic:` here
would have failed 17 tests loudly in milliseconds instead.

**The one-line reproducer for whoever takes this:** `TestHostnameInSNI` **hangs** — a pure table test
with no sockets and no TLS, whose table contains IP literals. It reaches
`hostnameInSNI → net.ParseIP → parseIP → As16` and dies. `As16` is only reached when the string
genuinely parses as an IP, which is why hostname-SNI TLS 1.2 handshakes pass while everything
touching `127.0.0.1` dies. The second reach path is server-side and equally common:
`x509.VerifyHostname → net.ParseIP → As16` (captured in `TestFallbackSCSV`'s stack).

**Not fixed here, deliberately.** It is a converter emission change with a corpus-wide blast radius
(34 candidate sites across 5+ packages), so it owes the converter gate set plus a seeded reconvert —
its own lane, with a behavioral guard pinning both shapes (a `[N]T` named return and a
zero-valued struct with an array field) and the `slice<T>` constructor's panic-vs-exception question
decided alongside it.

### Two singletons worth their own lines

* **`GetAddrInfoW` access-violates.** `TestVerifyHostname` kills the process outright:
  `Fatal error. 0xC0000005` through `net.DialContext → resolveAddrList → LookupPort → lookupPort →
  syscall.GetAddrInfoW → Syscall6`. This is the **open non-blittable-syscall class** CLAUDE.md
  records after the `Timezoneinformation` fix — *"9 more syscall wrappers pass a non-blittable struct
  by address … Nothing exercises them today; `net` and `crypto/x509` will."* `net` just did, and
  `AddrinfoW` is the first member with a measured consumer.
* **`ж<T>.op_OnesComplement` nil-derefs** (`golib/ж.cs:957`) under `TestQUICHandshakeError`.

### Go's own 4 failures are EXPIRED TEST FIXTURES, not a host defect

The previous entry flagged `TestResumption`, `TestVerifyConnection`, `TestResumptionKeepsOCSPAndSCT`
and `TestCrossVersionResume` as "host-environmental until proven otherwise". They are, and the reason
is now named: the certificates those tests carry expired on **2025-01-01**, and this host's clock is
**2026-08-16** —

```
x509: certificate has expired or is not yet valid:
current time 2026-08-16T02:57:52-05:00 is after 2025-01-01T00:00:00Z
```

Both languages fail them identically, so they are correctly excluded from the differential (C# turns
them into hangs rather than failures, which is root B's containment behavior again). **This will
worsen with time on any machine, and it caps `crypto/tls` at 180 of 184 no matter what go2cs does** —
whoever eventually banks this package needs that fact before reading a differential, and it is a
Go-toolchain-version property, not a go2cs one.

### Not banked, and not bankable

Builds-and-partly-runs, exactly as the previous entry. No roster row, no proof page, no disclosures —
none of the 53 divergences is a disclosed-divergence candidate under `alloc-profile`,
`codegen-liveness` or `host-limit`: every one is either a real defect go2cs owns or an environmental
fixture expiry. Converted test sources are **not** committed, per the validated-package commit policy.

**Next move**, in dependency order: the `default!` zero-value emission (closes 47 of 53 at one
stroke), then `GetAddrInfoW`, then the two singletons, then `TestCertCache`. The 7 B-shaped silent
hangs should be re-measured *after* the emission fix rather than investigated now — the cheapest way
to learn whether they were the same root is to remove the root.

### Method notes worth keeping

* **A `-tests` pipeline run cannot be interrupted by killing `go2cs.exe` alone.** Doing so orphans the
  `dotnet run` child and its `crypto.tls.tests.exe` grandchild, which keep running, keep the suite's
  sockets open, and hold `runtime.dll` locked — the next run then dies with **MSB3027/MSB3021** and its
  comparison reports `Go="pass" C#=""` for ~180 tests, which reads exactly like a total conversion
  failure. It is a file lock. Kill the TREE (by verified parentage), not the parent.
* **`go2cs` does not relay the test host's stdout live** — it captures and prints at exit, so a log
  tail shows nothing at all for the entire run. CPU sampling and the process tree are the only live
  instruments; `dotnet-stack report -p <pid>` on the parked host is what produced every root above.
* **Flat CPU dates a stall but not its beginning** (the previous entry's lesson) — sampling from t=0
  put this stall at ~30 s into the host run. Both readings were 8.3 s of CPU, which is a coincidence
  worth not over-reading: it is where a `crypto/tls` host stops, by two different mechanisms.

## ⛔ STILL DOES NOT BANK — the `default!` zero-value emission LANDED and `crypto/tls` goes **127 → 163 of 184**; the remaining wall is ONE pre-existing root worth 10 tests (2026-08-16, lane `claude/zero-value-arrays`)

The previous entry's "next move, in dependency order" opened with the `default!` zero-value emission
and priced it at 47 of 53. It landed (`claude/zero-value-arrays`), and the re-census on the same
per-test method — one process per top-level `Test*`, `-run '^Name$' -timeout 20s`, the same 184
non-boringcrypto functions — measures **163 PASS, 0 FAIL, 4 HANG, 14 CRASH, 3 infrastructure-error**
in 691 s.

| Measure | 2026-08-16 (pre-fix) | now |
|---|---|---|
| top-level tests that PASS run on their own | 127 of 184 | **163 of 184** |
| real divergences (Go passes, C# does not) | 53 | **17** |
| distinct roots behind them | 8 | **5** |

Four of the 21 non-passing are Go's OWN expired-certificate failures (`TestResumption`,
`TestResumptionKeepsOCSPAndSCT`, `TestVerifyConnection`, `TestCrossVersionResume`) — unchanged, both
languages fail them, still the 180-of-184 ceiling this host cannot beat. 184 − 163 − 4 = **17**.

### The roots that closed

Roots **A** (30 tests, `ticketKey.aesKey` length 0 → `aes: invalid key size 0`) and **B** + **B-shaped**
(17 tests, `netip.As16` slicing a zero-length array) were the same defect and are both **CLOSED**.
Every TLS 1.3 session-ticket path now passes — `TestQUICSessionResumption`, `TestQUICEarlyData`,
`TestQUICPostHandshakeKeyUpdate` and the rest of the QUIC family are green — and the 7 silent
B-shaped hangs are gone, which answers the previous entry's open question: they *were* the same root,
and removing it was indeed the cheapest way to learn that. Root **F** (hang after partial progress)
is also gone as a category.

### The root that was HIDING behind them — 10 of the remaining 17

`TestHostnameInSNI`, the previous entry's one-line reproducer, no longer hangs: it drives a real
handshake and dies further down, in **`sha3.copyOut`**, with a fatal
`AccessViolationException` reading `d.storage[..rate]` — i.e. `ref state d` does not address a valid
managed `state`. Reach: `mlkem768.NewKeyFromSeed → kemKeyGen → sha3.Sum512`, which every TLS 1.3
ClientHello performs, so it now claims **10** tests: `TestDialTimeout`, `TestHandshakeKyber`,
`TestHostnameInSNI`, `TestKyberDecapsulate`, `TestKyberEncapsulate`, `TestSCTHandshake`,
`TestServerSelectingUnconfiguredApplicationProtocol`,
`TestServerSelectingUnconfiguredCipherSuite`, `TestTLS13OnlyClientHelloCipherSuite`, `TestVersion`.

**It is PRE-EXISTING, and that was proven rather than assumed.** The suspicion is natural — this
lane changed `sha3`'s four `Sum*` functions, whose `digest [N]byte` named results were four of its
14 sites — so the four sites were reverted to `default!`, the host rebuilt, and the test re-run: the
**identical** stack reproduces. The `in` argument plays no part in the faulting path either
(`Sum` fills a freshly `make`-d buffer and only touches `in` in the closing `append`). It was simply
unreachable behind the netip wall.

### The remaining 17, by root

| count | root | status |
|---:|---|---|
| **10** | `sha3.copyOut` AccessViolation via `mlkem768` key-gen — `ref state` not addressing managed storage | **NEW HEAD OF THE QUEUE**; pre-existing, newly reached |
| 3 | hangs with no output: `TestCipherSuitePreference`, `TestConnectionState`, `TestDialer` | unrooted — first census with no output at all to root them |
| 1 | `TestVerifyHostname` — process AV `0xC0000005` in `syscall.GetAddrInfoW` | unchanged; the open non-blittable-syscall class |
| 1 | `TestQUICHandshakeError` — `ж<T>.op_OnesComplement` nil-deref | unchanged singleton |
| 1 | `TestCertCache` — weak-ref timing | unchanged (old root 4) |
| 1 | `TestBogoSuite` — external BoGo shim | not a conversion signal |

**Next move**, in dependency order: the `sha3`/`mlkem768` `ref state` AccessViolation (10 at one
stroke, and it is a POINTER-materialization defect, so it likely reaches well past `crypto/tls`),
then re-census the 3 unrooted hangs *after* it — the same "remove the root before investigating what
sits behind it" that just paid off here — then `GetAddrInfoW`, then the two singletons.

Still builds-and-partly-runs; no roster row, no proof page, no disclosures, converted test sources
not committed.

## ⛔ STILL DOES NOT BANK — the `sha3` AccessViolation closes and `crypto/tls` goes **163 → 176 of 184**; real divergences **17 → 4**, and every one of them was already on this board (2026-08-16, lane `claude/sha3-copyout-av`)

The previous entry named the `sha3.copyOut` AccessViolation the new head of the queue, priced it at
10, and said to re-census the 3 unrooted hangs *after* removing it rather than investigating them
first. Both calls were right, and the second paid better than the first: the same per-test method —
one process per top-level `Test*`, `-run '^Name$' -timeout 20s`, the same 184 non-boringcrypto
functions — now measures **176 PASS, 7 FAIL, 0 HANG, 1 CRASH, 0 infrastructure-error** in 728 s.

| Measure | 2026-08-16 (pre-`default!`) | after `default!` | now |
|---|---|---|---|
| top-level tests that PASS run on their own | 127 of 184 | 163 of 184 | **176 of 184** |
| real divergences (Go passes, C# does not) | 53 | 17 | **4** |
| distinct roots behind them | 8 | 5 | **3** |
| silent HANGs | 13 | 4 | **0** |

184 − 176 = 8 non-passing, and **four of those are Go's OWN expired-certificate failures**
(`TestResumption`, `TestResumptionKeepsOCSPAndSCT`, `TestVerifyConnection`, `TestCrossVersionResume`)
— unchanged, both languages fail them, still the 180-of-184 ceiling this host cannot beat. So the
real divergence count is **4**.

### What closed

All **10** of the tests the previous entry attributed to this root now PASS: `TestDialTimeout`,
`TestHandshakeKyber`, `TestHostnameInSNI`, `TestKyberDecapsulate`, `TestKyberEncapsulate`,
`TestSCTHandshake`, `TestServerSelectingUnconfiguredApplicationProtocol`,
`TestServerSelectingUnconfiguredCipherSuite`, `TestTLS13OnlyClientHelloCipherSuite`, `TestVersion`.

And so do all **3** of the unrooted silent hangs — `TestCipherSuitePreference`,
`TestConnectionState`, `TestDialer`. They were the same root, exactly as the "remove the root
before investigating what sits behind it" heuristic predicted for the second time running. **The
package now has zero hangs**, which is worth recording on its own: a suite that contains a hang
cannot be measured by a single-process run at all (the 23 → 7 floor the 2026-08-16 entry had to
explain away), and this one no longer does.

### The root, named exactly — and it was not about sha3, or about crypto, or about the OS

The previous entry read the fault as "`ref state` not addressing managed storage". That is the
symptom, not the defect, and the receiver is fine. Reproduced in **25 lines with no crypto in it**:

```go
type state struct{ a [4]uint64 }
var d state
ab := (*[32]byte)(unsafe.Pointer(&d.a))
copy(b, ab[:])                            // Fatal AccessViolationException
```

which emits, identically to sha3's `xorIn`/`copyOut`:

```csharp
var ab = (ж<array<byte>>)(uintptr)(new @unsafe.Pointer(Ꮡd.of(state.Ꮡa)));
copy(b, (~ab)[..]);
```

A `byte[]` view over a `uint64[]` has no managed spelling, so `pointerReinterpretManagedSource`
correctly excludes pointer-to-ARRAY targets and the site keeps the raw-address route. That box is a
perfectly good **address**. The defect is the **dereference**: `~ab` reads an `array<byte>` STRUCT —
a backing-store *reference* plus bounds — out of the keccak state's own DATA, fabricating a managed
reference. The stack the previous entry recorded is the fabricated reference being *used*, one
frame removed from the reinterpret that built it:

```
System.AccessViolationException
   at go.slice`1[Byte]..ctor(Byte[], IntPtr, IntPtr, IntPtr)
   at go.array`1[Byte].Slice(Int32, Int32)
   at go.array`1[Byte].get_Item(System.Range)
```

⚠ The previous entry's "reading `d.storage[..rate]`" is a misattribution worth correcting: that is
the *argument* at the call site, and it evaluates fine. The fault is inside `copyOut`'s own `ab[:]`.

### The two fixes, at the two layers that have one

**No general fix exists**, and that is the finding rather than a shortfall: `slice<T>`/`array<T>` are
windows on a real `T[]`, and a `U[]` view over a `V[]` cannot be constructed. This is the raw-metal
fork the S1/CS0030 ruling already governs, and `crypto/subtle`'s `xor_generic.cs` is the same case
one type-pair over (a `uintptr[]` view over a `byte[]`).

**The site** is `src/core/vendor/golang.org/x/crypto/sha3/xor.cs`, which takes `[module:
GoManualConversion]` and `crypto/subtle`'s remedy: `MemoryMarshal.AsBytes` over the state array's own
span is a genuine ALIASING view, so the absorb's XOR lands in the real state and the squeeze reads
it. Go's `cpu.IsBigEndian` branch is left exactly as converted. Marker census **58 → 59**.

⚠ **The layer fix was BUILT, MEASURED, and REJECTED — and the reasons are the most transferable
thing in this entry.** An AccessViolation is the worst available failure mode (uncatchable, no
diagnostic, names the innocent consumer), so `ж<T>` was made to refuse the read with a contained
panic instead. Two counter-examples killed it, **both found by gates rather than by reasoning**:

1. **`RuntimeHelpers.IsReferenceOrContainsReferences<T>()` alone is too WIDE.** `time.syncTimer` is
   `return ~Ꮡc.Reinterpret<channel<Time>, unsafe.Pointer>()`, and `unsafe.Pointer` is a CLASS, so it
   lands on the same address route — but reading it yields the REAL channel object, type-CONFUSED
   rather than fabricated. That is the managed-referent model the corpus is built on. The wide form
   took down `time.NewTimer`, and with it every `crypto/tls` test that opens a pipe, on the first
   host run. Adding a `typeof(T).IsValueType` term fixed that one.
2. **The narrowed form still regressed `ArrayCastDerefClone`** — the behavioral guard for this very
   fork, caught by the full suite (Output `exit code mismatch: C# 2 vs Go 0`). Its
   `*(*Row)(unsafe.Pointer(&r))` over a ZERO-valued array reads an `array<nint>` whose fabricated
   backing reference is **null**, and `array<T>`'s null-safe zero value absorbs it — so the site
   produced garbage *harmlessly*, which is exactly the "compiles and does not crash" bar the S1
   ruling sets for raw-metal stubs. The refusal converted a tolerated stub into a panic.

**The distinction the remedy actually needs is not the pointee's TYPE but whether the fabricated
reference comes out NULL** — benign when it does, fatal when it does not. That cannot be tested
without first materializing a `T` with a wild reference in a stack slot, which is itself unsafe (a
GC scanning that slot is the same crash). So the class keeps the AccessViolation, and the sketch
above is the starting point for whoever revisits it. Anyone tempted to re-add the refusal should
read this paragraph first: the wide form and the narrow form have both already been tried.

### Census of the class, and of the siblings

The emitted shape appears in **43 corpus files**. Almost all are either address-only (never
dereferenced) or in `runtime` raw-metal paths nothing reaches — `runtime` and its per-GOOS folders
hold 26 of the 43. The live remainder is `internal/syscall/windows/registry`'s
`SetDWordValue`/`SetQWordValue`, `reflect`/`runtime`'s `name.pkgPath`, `internal/reflectlite`,
`internal/poll/windows`, `syscall/windows` and `go/types`. Each is now a **named panic** instead of
a process kill, and each gets fixed when a suite reaches it — not speculatively.

**Sibling vendored crypto is CLEAN.** sha3's two lines are the only `(*[N]T)(unsafe.Pointer(…))` in
all of `vendor/golang.org/x/crypto`: `chacha20`, `chacha20poly1305`, `internal/poly1305`,
`curve25519`, `hkdf` and `cryptobyte` use none (the only other `unsafe.Pointer` uses are
`internal/alias`'s address COMPARISONS, which are not reinterprets), and `blake2b` is not vendored
into GOROOT at all. The suspicion that "they use similar state tricks" does not survive the census.

### The 4 that remain, by root

| count | root | status |
|---:|---|---|
| 1 | `TestVerifyHostname` — process AV `0xC0000005` in `syscall.GetAddrInfoW` | unchanged; the open non-blittable-syscall class. **Now the head of the queue** |
| 1 | `TestQUICHandshakeError` — `ж<T>.op_OnesComplement` nil-deref on a goroutine | unchanged singleton |
| 1 | `TestCertCache` — weak-ref timing | unchanged (old root 4) |
| 1 | `TestBogoSuite` — external BoGo shim | not a conversion signal |

**Next move**: `GetAddrInfoW` (the last root shared with another package — `net` and `crypto/x509`
both want it), then the `op_OnesComplement` singleton. `TestCertCache` and `TestBogoSuite` are not
worth an arc: one is a GC-timing assertion the CLR cannot be made to satisfy on demand, the other is
an external binary. At 4 divergences, **the question `crypto/tls` now poses is a banking question,
not a debugging one** — what a roster row and a disclosure manifest would have to say about the
four.

### Guarding, honestly

No **behavioral** guard is available for the sha3 fix, and the reason is structural rather than an
omission: the vendored package has no `_test.go` in GOROOT, and a behavioral test cannot import
`golang.org/x/crypto/sha3` (the converter resolves it to `core/golang.org/…`, not
`core/vendor/golang.org/…`). What guards it instead:

* **`GolibTests.Sha3ReinterpretVectorTests`** — known-answer vectors run against the corpus package
  directly. FIPS-202's own SHA3-256("")/SHA3-256("abc")/SHA3-512("abc")/SHAKE256("abc"), plus
  lengths 135/136/137/200/1000/4096 and an offset-13 sub-slice checked against the **OS SHA-3
  implementation** — an oracle with no dependency on this repository. 136 is SHA3-256's exact rate,
  so those three straddle the multi-block boundary where `xorIn`'s fast path engages and the state
  is XORed and permuted repeatedly; the offset sub-slice makes the input's word-at-a-time read
  unaligned, which the always-aligned state span never exercises. What they really prove is that the
  aliasing view WRITES THROUGH — a snapshot instead of an alias gives a wrong digest on the first
  vector. ⚠ The "no test tier is shaped for this" instinct was wrong and worth un-learning:
  `GenericTests` already references `core/sort`, so an MSTest tier binding a converted package is
  established practice, not new infrastructure.
* **The marker gate** — proven by reconvert: `xor.cs` untouched, emission redirected to
  `xor.cs.auto`.

**Neutered-fix control, run rather than asserted.** Restoring the auto-converted `xor.cs` does not
merely fail the four vector tests — it KILLS the test host with an `AccessViolationException` inside
`slice<byte>..ctor`. That is the defect's whole character, and the reason the layer fix above was
attempted at all: expect a dead host, not a red test, if you re-run that control.

⚠ **`crypto/internal/mlkem768` is the natural operational guard and it is BLOCKED, on two defects
that have nothing to do with any of this.** Its suite is the direct consumer of sha3 (`TestRoundTrip`,
`TestPQCrystalsAccumulated` and the field/compress vectors all drive SHAKE), and the `-tests`
pipeline converts it but cannot build it:

* `mlkem768_test.cs(94)`: **CS0315** — `builtin.min<T>` over `fieldElement`, a `[GoType("num:uint16")]`
  named numeric, which the generated wrapper does not give `IComparable<T>`.
* `mlkem768_test.cs(182,194)`: **CS0841** — "cannot use local variable `q` before it is declared",
  an emission-ordering defect in the test file.

Fixing those two would bank mlkem768 as a roster package AND retire this guard gap in one arc; it is
the cheapest available roster growth in the crypto tree.

> **CLOSED 2026-08-16** by lane `claude/mlkem768`. Both defects are fixed at their real layer, the
> package banks **12/12 · 0 disclosed** as roster row 147, and the guard gap this paragraph opened is
> retired. One correction to the bullets above: the second defect is not an *emission-ordering*
> defect — nothing is emitted out of order. It is a package-level **const** taking neither half of
> the local-shadow defence, and it produces CS0165 as readily as CS0841 depending only on whether
> the C# declaration says `var`. See the entry at the end of this board.

### Method notes worth keeping

* **A per-test census script must be written against the CONVERTED host's verdict format, not
  `go test`'s.** The host prints `PASS<pad><Name>` / `FAIL<pad><Name> — reason`, not
  `--- PASS: Name`. A classifier carrying the `go test` shapes filed **every pass as CRASH** and
  reported 177 CRASH / 7 FAIL for a run that was really 176 PASS. The per-test stdout was saved to
  disk, so the verdict was recovered by reclassifying offline rather than by re-running — save the
  raw output, always, and treat a summary that is *uniformly* bad as a classifier bug before
  believing it.
* **A top-level verdict line can carry a trailing message** (`PASS  TestDialTimeout — with timeout
  100000, …retrying`), so an end-anchored name match under-counts passes. Anchor on "not followed by
  a name character or `/`" instead, or subtests are miscounted too.
* **A SINGLE-PACKAGE reconvert proves a MARKER, never an emission.** Run against sha3 it also
  rewrote `sha3.cs` (`keccakF1600(ref nonnil(ref d).a)` → `keccakF1600(Ꮡd.of(state.Ꮡa))`), emitted a
  `keccakf_amd64.cs` the `-stdlib` driver excludes, and dropped the csproj's validation-proof block —
  three single-package-vs-stdlib artifacts that read exactly like drift. Use the seeded `-stdlib`
  reconvert to measure emission.

## ✅ `crypto/internal/mlkem768` BANKS 12/12 — sha3's only available operational guard, and the two defects that stood in front of it (2026-08-16, lane `claude/mlkem768`)

The `sha3` entry above closed an AccessViolation and then recorded a gap it could not close itself:
`crypto/internal/mlkem768` is the only operational guard the hand-owned `sha3` fix can have — the
vendored `golang.org/x/crypto/sha3` ships no GOROOT test suite and is not importable from a
behavioral test — and the `-tests` pipeline converted mlkem768 but could not build it, on two
defects that had nothing to do with sha3, with crypto, or with each other. Both are now fixed at
their real layer, both proven failing-first by behavioral guards, and the package banks
**12 matched · 0 disclosed** as roster row **147** (16,573 verdicts, 68.4%).

### Defect 1 — CS0315: a named numeric was ordered by OPERATORS but was not `IComparable<T>`

`min(a-got, got-a, a-got+q, got-a+q)` over `type fieldElement uint16` (`TestDecompressCompress`)
did not compile: *"no boxing conversion from `fieldElement` to `System.IComparable<fieldElement>`"*.

Ordering has **two** surfaces in .NET and the generated `[GoType num:]` wrapper carried only one.
`IComparisonOperators<T,T,bool>` — which the wrapper declares — serves a constraint lifted from
`cmp.Ordered`, and golib's TWO-argument `min`/`max` bind it. The N-argument forms cannot: a
`params ReadOnlySpan<T>` element has to compare through a *member*, so they bind `IComparable<T>`,
which the wrapper did not implement. So a named numeric bound `min(a, b)` and failed
`min(a, b, c, d)` — an arity cliff, which is why nothing had tripped it before.

Fixed in **go2cs-gen**, not at the call site and not by relaxing golib's constraint:
`InheritedTypeTemplate` declares `IComparable<T>` on the **same kind-gate** as `IComparisonOperators`
(every numeric kind except complex, which Go orders no more than C# does) and `NumericTypeTemplate`
emits its one member inside the same gated block, `CompareTo` forwarding to the UNDERLYING value's:

```csharp
public int CompareTo(fieldElement other) => m_value.CompareTo(other.m_value);
```

Forwarding rather than re-deriving from the wrapper's own `<`/`>` is deliberate — it keeps a named
float on the BCL total order (NaN below everything), which is what makes `min` yield NaN when any
argument is NaN, as Go's does. The wrapper was already `IEquatable<T>`; it is ordered now too,
matching the golib `uintptr` and `@string` structs, which are both.

**Why this layer and not golib.** Relaxing the params overload to `IComparisonOperators` was the
alternative, and it is the wrong general fix: the two overloads differ only in constraint, so it
would have to REPLACE the `IComparable<T>` one, silently withdrawing N-argument `min`/`max` from
every type that is comparable without carrying operators. The wrapper's missing interface was the
actual incompleteness, and filling it also buys `Array`/`List.Sort`, `SortedSet<T>` and
`Comparer<T>.Default` over every named numeric in the corpus.

### Defect 2 — CS0841/CS0165: a package-level CONST had NEITHER half of the local-shadow defence

`q := big.NewInt(q)` (`TestZetas`, `TestGammas`, over `const q = 3329`). Legal Go — a short variable
declaration's scope begins *after* its own ValueSpec, so the initializer reads the constant — and
illegal C#, where the local owns the name for the whole block and its own initializer binds to it.

The converter already defends this shape twice over, and **both defences are keyed to `*types.Var`**:

* the LOCAL-rename half (`usedPackageVarNames`, variableAnalysisOperations) pre-scans for idents
  resolving to a package-level object found in `globalScope` — typed `map[string]*types.Var`, so a
  const is not in it and a const-shadowing local is never renamed;
* the QUALIFY half (convIdent) rewrites the *global's* reference to `<pkg>_package.name`, gated on
  `v.info.ObjectOf(ident).(*types.Var)` — a const does not match the type assertion.

A `*types.Const` fell between them. Fixed by giving convIdent a const arm beside the var arm, with
one deliberate difference: it consults `funcScopeVarNames` (every variable declared ANYWHERE in the
function) rather than `funcLevelDecls` (declarations directly in the function body), because the
same shape inside an `if`/`for` init is not function-level and the const has no rename to fall back
on. Qualifying a reference no local actually shadows costs verbosity and never changes meaning, so
the wider set is the safe side to err on.

**The board's characterization of this one was wrong and is corrected in place above.** It reads as
an "emission-ordering defect"; nothing is emitted out of order. It is also not reliably CS0841: that
diagnostic appears only when the C# declaration infers `var` (which `big.NewInt` does). Declare the
type and the identical Go source produces **CS0165, "use of unassigned local variable"** — one root,
two diagnostics, which is why the guard carries both shapes.

### Guards, proven failing-first

Both fixes extend an EXISTING behavioral project rather than adding one — each defect is a new
member of a family that project already documents.

| Guard | Extension | Pre-fix verdict |
|---|---|---|
| `MinMaxBuiltin` | `min`/`max` at 2 and 4 arguments over named **unsigned** (`fieldElement uint16`, including mlkem768's own four-argument call), **floating** (`ratio float64`) and **signed** (`delta int8`) underlyings | **CS0315 ×10** across all three kinds |
| `GlobalShadowedByLocal` | a self-referencing initializer at function level, the same shape in an `if` init, the `var`-inferred form that reproduces mlkem768's exact CS0841, and an unshadowed control that must keep the bare name | **CS0165 ×2** (explicit-type shapes); the `var` shape adds the CS0841 |

Both were run with the fixes reverted and the binaries rebuilt, not asserted.

### Corpus footprint: measured at ZERO

A seeded `-stdlib` reconvert (5,055 files seeded incl. `version.props` + `docs/validation`; 59
`[module: GoManualConversion]` files, **0** marker-gate violations; 1,664 files emitted) produced
**50 CRLF phantoms and exactly ONE real content diff** — `crypto/internal/mlkem768/README.md`, whose
Tests badge flips to `12/12 validated` because the package now has a proof page. That is the bank's
own artifact, not the converter's. So the const arm changes no stdlib package's emission: no
converted function both references a package const and declares a same-named local. The generator
change is compile-time and changes no emission at all; its gate is the corpus BUILD, which is clean.

### The census, honestly

**12 of 12 top-level tests match, 0 disclosed, 0 skipped**, 4 declarations excluded on both sides
(`BenchmarkKeyGen`, `BenchmarkEncaps`, `BenchmarkDecaps`, `BenchmarkRoundTrip` — the standard
Phase-4D benchmark deferral). No third defect appeared behind the two: the suite built and ran
first try once they were fixed, which is worth recording against this week's pattern.

C# runtime, i7-5820K, solo: **434.7 s**, and it is one test —

| Test | C# elapsed |
|---|--:|
| `TestPQCrystalsAccumulated` | 417.3 s |
| `TestFieldReduce` | 7.4 s |
| `TestFieldSub` | 3.2 s |
| `TestFieldMul` | 2.8 s |
| `TestFieldAdd` | 2.6 s |
| the other seven | < 1 s each |

`TestPQCrystalsAccumulated` runs 10,000 full key-gen/encapsulate/decapsulate rounds and accumulates
them into a SHAKE-128 digest. 435 s clears the sweep's 10 m default by only **1.4x** — inside the
spread a loaded sweep produces — so this package is owed a `$longTimeouts` floor and gets **30m**
(4x headroom). That is the sixth entry in that table; the rule it follows is unchanged (a floor, not
an override — a larger `-TestTimeout` still raises it).

### The guard gap closes

mlkem768's production code drives `sha3.Sum512`, `New512`, `Sum256`, `New256`, `NewShake256` and
`NewShake128` — six entry points across key generation, encapsulation, decapsulation and the
sampling PRFs — and every one of them is on the path of the banked verdicts. The `sha3` entry's
hand-owned `xor.cs` (the array-reinterpret fix) therefore now has a committed suite that exercises
it on every sweep, which is exactly what that entry said it lacked. `crypto/tls`'s 176-of-184
remains the wider witness; this is the *repeatable* one.

**Run, not asserted — the neutered-fix control.** Restoring the auto-converted `xor.cs` over the
hand-own (its `.cs.auto` sibling is committed beside it) and rebuilding kills the mlkem768 test host
outright, exactly as the `sha3` entry predicted for `crypto/tls`: a **fatal
`AccessViolationException`, exit `0xC0000005`**, on the stack

```
go.slice<byte>..ctor  ←  array<byte>.Slice  ←  sha3.copyOut  ←  padAndPermute  ←  Read
                      ←  sha3.Sum  ←  Sum512  ←  mlkem768.kemKeyGen  ←  generateKey
                      ←  GenerateKey  ←  TestBadLengths
```

The host dies at the FIRST test that generates a key, so the comparison reports `Go="pass" C#=""`
for all 12 rows — the documented dead-host signature, not twelve failures. Restore the hand-own and
all 12 pass again. That is the guard doing its job, measured on the committed suite.

⚠ **A restore-from-backup trap, worth one line because it cost a run.** `Copy-Item` PRESERVES the
source file's `LastWriteTime`, so copying a backup back over `xor.cs` gives the restored file an
*older* timestamp than the neutered build's assembly — MSBuild judges the project up to date and
keeps the NEUTERED dll. The re-run then reproduces the AV with clean, HEAD-matching source and a
`git status` that shows nothing at all. Touch the restored file (or build `--no-incremental`) before
believing any verdict that follows a hand-own swap.
## 🔎 SCOUT — golib's name surface leaks into every converted package, and ONE shape of collision compiles and lies (2026-08-16, lane `claude/golib-name-scout`)

**FIX IS QUEUED, NOT PRESSING** (user ruling at the time this was raised). Nothing in the corpus is
broken today; the exposure is user-code- and `-recurse`-facing. This entry is the census, the
measured resolution matrix, the defense recommendation and the guard spec, so the fix lane starts
from measurement rather than from re-derivation.

### The question (user-raised)

golib publishes helper names into the scope every converted file compiles under — `builtin` members
(`mapclone`, `tmpstring`, `subslice3`, …), the runtime types (`slice`, `map`, `@string`, `error`, …),
extension methods. End-user Go code may legally declare identifiers with those exact names, and Go
even permits shadowing its own predeclared identifiers (`func len(...)` at package scope is legal Go
— verified, not assumed). What happens in the emitted C#?

### The scope converted code compiles under — measured from the emission, not from doctrine

Three channels, and only the first is obvious:

1. **`<Using Include="go.builtin" Static="True" />`** in `csproj-template.xml` (every converted
   `.csproj` carries it) — every `public static` member of `go.builtin` is a **bare simple name** in
   every converted file.
2. **`namespace go;`** — converted code is emitted *inside* golib's own namespace, so every public
   golib type there (`slice<T>`, `map<K,V>`, `channel<T>`, `array<T>`, `@string`, `error`, `sstring`,
   `uintptr`, `comparable<T>`, `complex64`, `ж<T>`, …) is a **bare type name**, and the nested
   namespace **`go.golib`** is a bare namespace name.
3. **The alias `<Using>` items** — `any`, `uint8`…`uint64`, `int8`…`int64`, `float32/64`,
   `complex128`, `rune`, `GoBigConst`, `GoTagAttribute`, `GoInitAttribute` — plus `using System;` and
   `using System.Numerics;`. `package_info.cs` adds `using go;` + `using static go.<pkg>_package;`.

⚠ **`src/core/GlobalUsings.cs` is DEAD and misleads a reader into thinking it is the mechanism.**
There is no `.csproj` at `src/core/`; `golib.csproj` has no explicit `<Compile>` and SDK globbing is
rooted at its own directory; every converted csproj does `<Compile Remove="**/*.cs" />` then
`<Compile Include="*.cs" />` — current folder only. Nothing compiles it, and it has drifted (it still
declares a `GoTag` alias no live csproj uses). Live global usings come **exclusively** from
`csproj-template.xml`. Deleting the file, or wiring it, is a separate small cleanup.

### Census of the exposed surface

| Bucket | Count | Names |
|---|---|---|
| (a) all-lowercase ASCII | **46** | `append array builtin cap channel clear close comparable complex complex64 copy defer delete error fallthrough fatal heap i imag iota len make map mapclone max min nil nonnil panic print println range real recover select slice sslice sstring str subslice subslice3 tmpstring type uintptr widen zero` |
| (a′) lowercase-initial, mixed | +4 | `_` `errorExtensions` `initPackage` `trySelect` |
| (b) glyph-bearing | 9 | `ᐧ` `ᐧᐧ` `ꟷ` `ꓸꓸꓸ` `ᐸꟷ` `ж` `Ꮡ` `makeǃ` `goǃ` |
| (c) `@`-escaped | 1 | **`@string`** — the collision that matters, see p17 |
| (d) PascalCase | 11 + ~60 types | `AreEqual` `ConvertToType` `ConvertToUInt64` `GetGoTypeName` `GoZero` `Implements` `StackAllocThreshold` `ToUTF8Bytes` `TryTypeAssert` (+ `InitializeGoLib`/`ZeroIsDefault`, internal), plus the public type/attribute/interface surface |
| extension-method names in scope | 16 | `_ array DerefOrNil DerefOrNull i Lsh OrTypedNil PrintPointer Reinterpret Rsh slice sslice ToGoString ToSlice ToUTF8Bytes type` |

golib's `internal` members are **not** in scope for converted packages — `ж.cs:18-19` grants
`InternalsVisibleTo` only to `unsafe` and `GolibTests`. `go2cs.Symbols`' 15 constants are **not**
bare names either (no `using static go2cs.Symbols;` in any converted csproj); only the namespace
simple name `go2cs` leaks.

### Three C# resolution rules, MEASURED — this is what decides each row

* **R1 — a member of `<pkg>_package` HIDES the using-static member of the same name, and C# does not
  merge the candidate sets.** Lookup stops at the first *scope* that has the name, not the first
  *applicable* overload. So a signature-compatible user declaration is **silently captured**; an
  incompatible one is a compile error whose message names a helper the user never wrote.
* **R2 — a call site emitted with EXPLICIT TYPE ARGUMENTS is IMMUNE.** Arity participates in lookup,
  so `heap<box>(out var Ꮡb)` skips a non-generic member *and a local variable* named `heap`.
  Measured at p03 and p15. **This is load-bearing**: it removes `heap`, `zero`, `subslice`,
  `subslice3`, `slice`, `make`, `clear`, `min`, `max` from the dangerous class and shrinks the fix
  by most of its apparent size.
* **R3 — a nested type hides a `namespace go` type of the SAME ARITY only.** `type slice struct{}`
  (arity 0) does *not* hide `slice<T>` (arity 1) — but `type string struct{}` emits `@string`
  (arity 0) and *does* hide `go.@string` (arity 0). Measured at p17.

### The measured resolution matrix — 21 probes, transpiled, compiled, run and diffed against `go run`

Probes were behavioral-test-shaped single packages built against the worktree's live `src/`
(`-p:go2csPath=<worktree>/src/ -p:UseSharedCompilation=false`). Every probe's Go program runs clean
first, so a divergence is the conversion, never the source.

| # | Scenario | C# result | Class |
|---|---|---|---|
| p01 | pkg `func len([]int) int` (predeclared shadow) + slice exprs, range | compiles, matches | benign |
| p02 | pkg `func append(a, b int) int` | compiles, matches | benign |
| p03 | pkg `func heap(int) int` + `&T{}` and an escaping local | compiles, matches | benign (R2) |
| p04 | pkg `func mapclone(int) int` + `maps.Clone` | compiles, matches | benign (call emits qualified `maps.Clone<…>`) |
| p05 | pkg `func str([]byte) string` | compiles, matches | benign (no `builtin.str` site emitted here) |
| p06 | pkg `func nonnil(int) int`, no trigger | compiles, matches | benign |
| p07 | pkg `func tmpstring(int) int` + `m[string(b)]` | **CS1503** `cannot convert from 'go.slice<byte>' to 'nint'` | LOUD |
| p08 | `type error struct{}`, unused | compiles, matches | benign |
| p09 | `type string struct{}`, unused | compiles, matches | benign |
| p10 | `type slice struct{}` + real slices | compiles, matches — emitted `Δslice` | benign, **existing defense** |
| p11 | pkg `var append = 11` | compiles, matches | benign |
| p12 | pkg `func subslice3(int) int` + `s[1:3:4]` | compiles, matches (emits `s.slice(1,3,4)`) | benign |
| **p14** | pkg **`func tmpstring(b []byte) string`** + `m[string(b)]` | **compiles; Go `11 ZZ` vs C# `0 ZZ`** | **SILENT WRONG** |
| p15 | **LOCAL** `heap := 5` in a function with a heap-promoted local | compiles, matches | benign (R2) |
| p16 | pkg `func nonnil(int) int` + `setOne(&e.x)` on a pointer param | **CS1615** `Argument 1 may not be passed with the 'ref' keyword` | LOUD |
| p17 | `type string struct{}` **+ a real string in the package** | **CS0029** `Cannot implicitly convert 'ReadOnlySpan<byte>' to 'go.main_package.@string'` | LOUD |
| p18 | `type error struct{}` + a real `errors.New` value | compiles, matches | benign |
| p19 | pkg `func len` + `copy`/map/range-over-string/3-index/variadic sweep | compiles, matches | benign |
| p20 | **LOCAL** vars `str`, `zero`, `nonnil`, `subslice`, `tmpstring` | compiles, matches | benign |
| p21 | `type error struct{}` + dynamic `interface{ Error() string }` assert | compiles, matches | benign |
| p13 | `type array` + `type channel` alongside real arrays/channels | compiles, matches — `Δarray`/`Δchannel` | benign, **existing defense** |
| p23 | **`-recurse=module`** app with a package named **`golib`** | **CS0576** `Namespace 'go' contains a definition conflicting with alias 'golib'` | LOUD, `-recurse`-only |

**Verdict census: 1 compiles-and-WRONG, 4 compile errors, 16 benign.**

#### p14 is the whole finding

Go source says `table[string(b)]`. The converter emits `table[tmpstring(b)]` — `builtin.tmpstring`
is the zero-copy map-index-key optimization, a name the *Go source never spells*. A user who happens
to declare `func tmpstring(b []byte) string` gets `@string tmpstring(slice<byte>)` in
`main_package`, which by R1 hides the golib helper and by luck **matches its signature**. The map
lookup silently routes through the user's function, misses, and returns the zero value. **No
warning, no error, exit 0, wrong answer.** The converter's transpile step exits 0 and prints
nothing.

The same shape is live for every helper the converter emits implicitly and *without* explicit type
arguments. Derived from the census and the emission sites (`convIndexExpr.go:337` `tmpstring(%s)`,
`refLoweringEmissionOperations.go:530` `nonnil(ref %s)`, `visitFuncDecl.go:1788` `maps.clone →
mapclone`, and the `str`/`trySelect`/`fatal`/`i`/`widen` sites), the **dangerous class is 12 names**:

> `_ fatal i initPackage iota mapclone nil nonnil str tmpstring trySelect widen`

The other 34 of the 46 fall out for three reasons, and the first is free immunity worth naming:

* **Six are Go KEYWORDS and can never be a user identifier at all** — `defer`, `fallthrough`, `map`,
  `range`, `select`, `type`. golib spells them as C# members precisely *because* Go reserves them, so
  that slice of the surface defends itself.
* **Eighteen are Go universe names** (`append`, `cap`, `clear`, `close`, `complex`, `copy`, `delete`,
  `imag`, `len`, `make`, `max`, `min`, `new`, `panic`, `print`, `println`, `real`, `recover`) — already
  defended by `goBuiltinNames`/`packageBuiltinShadows`, below.
* **The remainder are generic-with-explicit-type-arguments** (`heap`, `zero`, `subslice`, `subslice3`,
  `slice`, `sslice`, `array`, `channel`, …) — immune by R2, or handled on the type side.

Two of the twelve are **not** function-call shaped and route through different emission paths, so
they need their own attention in the fix: **`nil`** (a `static readonly` field the converter emits in
every `== nil` / `= nil`, and `var nil int` is legal Go) and **`iota`** (a `const`, likewise legal as
a Go package-level name). Neither was probed; both belong in the guard project.

⚠ **Extension-form emissions (`i`, `_`, `type`, `slice`, `array`, `sslice`, `ToUTF8Bytes`) were NOT
probed for silent capture.** Extension-method lookup runs only after instance lookup fails and is not
hidden by a same-named *static* member, so they are low-risk by construction — but that is reasoning,
not measurement, and the fix lane should probe one before dismissing the bucket.

### Live corpus near-misses — 5 packages already do this, all benign TODAY

The corpus compiles and 130 packages validate, so nothing here is broken. But the stdlib is *already*
one emission away from p14:

| Package | Declaration | Shadows | Why it survives |
|---|---|---|---|
| `runtime` | `map.cs:1656` `internal static any mapclone(any mʗp)` | `builtin.mapclone(any)` — **identical signature** | `runtime` never emits a `maps.Clone` call. This is p14's exact shape, sitting in the tree. |
| `math` | `fma.cs:11` `internal static uint64 zero(uint64 x)` | `builtin.zero<T>()` | R2 — `zero<T>()` always carries explicit type args |
| `runtime`, `sync` | `panic.cs:1103`, `mutex.cs:35` `fatal(@string)` | `builtin.fatal(string, nint = 1)` | no `builtin.fatal` site in either package (`mutex.cs` is hand-owned, so this one is deliberate) |
| `log/slog` | `value.cs:354` `str(this Value v)`, plus local funcs `str` in `level.cs:65` and `json_handler.cs:201` | `builtin.str` | no `builtin.str` site in slog |
| `runtime` | `debuglog.cs:191` `i(this ж<dlogger> Ꮡl, nint x)` | `builtin.i` (extension) | extension-vs-extension on different receivers |

That is the honest answer to "does the corpus trip this today": **no — but it holds five loaded
guns, one of them (`runtime.mapclone`) with a signature that matches exactly.**

### What defends this today, and the exact gap

Five hand-maintained lists, none of them derived from golib:

| List | File | Size | What it does |
|---|---|---|---|
| `keywords` | `identifierNaming.go:33` | 65 | `@`-escape — this is what turns Go `string` into `@string`, i.e. it *creates* p17's collision |
| `reserved` | `identifierNaming.go:85` | 52 | Δ-rename (`Δfoo`, or `Δfooᴛ` when also collision-flagged) — holds `array`, `builtin`, `channel`, `slice`, `sstring`, `GoFrame`, `NilType`, `PanicException`, `AreEqual`, `GetGoTypeName`, `ToUTF8Bytes`, `type`, and 4 marker glyphs |
| `emitterSpelledTypeNames` | `nameCollisionAnalysisOperations.go:37` | **4** (`any`, `rune`, `nint`, `nuint`) | package-scoped Δ-rename, **TYPES only** |
| `goBuiltinNames` | `packageGlobalState.go:234` | 18 | a package-level func/method with a Go **universe** name → the converter's own builtin calls emit qualified as `builtin.X(…)` (`packageBuiltinShadows`). **This is the mechanism that covers the universe-name family — p01/p02/p11/p19.** |
| `csharpKeywordCastTypes` | `convCallExpr.go:26` | 15 | cast parenthesization, not naming |

**The gap, stated exactly:** *nothing in the converter enumerates golib's `builtin` member surface or
its type surface.* `reserved` covers 4 of the ~30 public `builtin` statics and misses `mapclone`,
`tmpstring`, `str`, `subslice`, `subslice3`, `nonnil`, `heap`, `zero`, `fatal`, `range`, `select`,
`trySelect`, `initPackage`, `widen`, `i`, `Implements`, `TryTypeAssert`; on the type side it misses
`error`, `comparable`, `complex64`, `uintptr`, `sslice`, and the **`go.golib` namespace**.
`goBuiltinNames` is correct but scoped to the 18 universe names and to func/method declarations only.

**The symbol table does not help and was checked because it looked like it should.**
`src/core/go2cs/symbols.json` → `gensymbols` → `symbols.go` + `Symbols.cs` is purely the *glyph and
marker* vocabulary (27 entries: `ж`, `Ꮡ`, `Δ`, `ᴛ`, `ʗ`, `ˢ`, `_package`, …). It carries **no**
reserved-name data, and `nameCollisionAnalysisOperations.go` does not read it at all — the only seam
is `identifierNaming.go:94`, which drops four marker glyphs into `reserved`. `check-symbol-sync.ps1`
verifies only that the two projections match the JSON, and **is wired into no gate** (not CNR, not
the behavioral runners, not `go test`; there is no CI workflow). So the generator precedent exists
and is good — it simply was never pointed at this problem.

**No unit test guards any of the five lists.** `sanitization_test.go` covers import-path segments
only. The 46 behavioral projects matching Collision/Shadow/Reserved/Keyword/Builtin cover the
machinery well — `ReservedNameShadows` is the closest — but every one of them tests names *already
in* the lists. None tests the gap, by construction.

### Recommendation — one generated list, feeding two mechanisms that already exist

The durable fix is **not** to grow `reserved` by hand. Two reasons, and the second is a trap:

1. Hand-maintenance is exactly what drifted; golib gains members freely and nothing notices.
2. ⚠ **`reserved` is the WRONG mechanism for these names.** Its own doc comment already warns that
   names the *emitter itself spells* must never go in it, because legitimate emissions flow back
   through the same string-based sanitizers (`slice<rune>` corpus-wide would corrupt to
   `slice<Δrune>`). `mapclone`, `tmpstring`, `nonnil`, `str` are precisely emitter-spelled names.
   Adding them to `reserved` would corrupt the converter's own output.

So: **generate the list, and feed it to the mechanisms already proven for the universe builtins.**

* **New:** `src/go2cs/internal/gengolibsurface` → committed `src/go2cs/golibSurface.go`, a
  `map[string]golibNameKind` (`builtinMember` / `namespaceType` / `namespaceChild` / `extensionMethod`)
  produced by scanning `src/core/golib/*.cs` for `public static` members of `partial class builtin`
  and public types in `namespace go`. Modeled on `gensymbols` — deterministic, `go generate`-driven,
  BOM/line-ending preserving.
* **Gate it in the plain `go test ./...` run**, the `projitemsIntegrity_test.go` precedent: regenerate
  in-memory, compare against the committed file, print the exact missing entry on failure. That is
  what makes the list *unable* to drift from golib. (Optionally also wire `check-symbol-sync.ps1`
  into CNR while in the neighbourhood — it currently gates nothing.)
* **Consumer 1 — builtin members → qualification, not renaming.** Extend `performNameCollisionAnalysis`
  so a package-level declarator (func, type, const **or var**) whose name is a `builtinMember`
  registers in `packageBuiltinShadows`. Then thread `builtin.` through the implicit emission sites —
  `convIndexExpr.go:337` (`tmpstring`), `refLoweringEmissionOperations.go:530` (`nonnil`),
  `visitFuncDecl.go:1788`'s helper map, and the `str`/`range`/`select`/`trySelect`/`fatal`/`widen`
  sites — via one small helper (`golibHelperName("tmpstring")` → `"builtin.tmpstring"` when shadowed).
  Qualification beats renaming here: it leaves the user's Go name intact in the emitted C#, which is
  the project's stated readability goal, and it is the mechanism already proven for the 18 universe
  names. **R2 means the generic-with-explicit-args sites need no change at all.**
* **Consumer 2 — golib TYPES → the existing package-scoped Δ-rename.** Add `namespaceType` names to
  what `emitterSpelledTypeNames` covers (`error`, `comparable`, `complex64`, `uintptr`, `sslice`,
  and — the p17 case — the post-`@`-escape spelling `@string`). Note the ordering hazard:
  `string` becomes `@string` in `getCoreSanitizedIdentifier` *before* the reserved check, so the
  collision test must run against the **emitted** spelling, not the Go one.
* **Consumer 3 — `namespaceChild` → the existing import-alias rename.** `importAliasOperations.go`
  already renames an alias that collides with a child namespace (`using Δunicode = unicode_package;`
  in `strings.cs` is that machinery working). It simply does not know `go.golib` is a child
  namespace. Adding `golib` to its set closes p23 in ~5 lines.

### Guard spec

Three behavioral projects, output-compared against `go run` (`[GoTestMatchingConsoleOutput]`), all
currently FAILING — so they land **with** the fix, never before it:

1. **`GolibHelperShadows`** — the p14/p07/p16 family in one package: `func tmpstring(b []byte) string`
   with a `m[string(b)]` map index; `func nonnil(a int) int` with a `setOne(&e.x)` pointer-field call;
   `func mapclone(...)` with a `maps.Clone`; `func str(b []byte) string`; `func fatal(s string)`.
   Each helper's Go-visible answer must survive AND the converter's own helper call must still do its
   job. This is the project that proves the qualification path.
2. **`GolibTypeShadows`** — `type string struct{}` *used alongside real strings* (p17), plus `type
   error`, `type comparable`, `type uintptr`, `type complex64`, `type sslice` each exercised next to
   the real golib type. `ReservedNameShadows` already owns `any`/`rune`/`nint`/`builtin`/`sstring`/
   `GoFrame`; this is its missing half and could reasonably be folded in rather than added beside it.
3. **`RecursePackageNamedGolib`** — p23. `-recurse`-shaped, so it may not fit the behavioral runner's
   single-package mold; if not, a converter integration test in
   `moduleConverter_integration_test.go` is the right home.

Plus the drift test above (`golibSurfaceIntegrity_test.go`), which is the guard that actually
prevents recurrence — the behavioral projects prove today's fix, the drift test prevents tomorrow's
golib member from re-opening the hole silently.

**Neutered-fix control the lane owes:** revert the qualification helper and confirm
`GolibHelperShadows` reports an OUTPUT divergence (not merely a compile failure) — p14's whole
character is that it compiles.

### Size estimate

**One lane, medium — roughly a day of work plus a CNR pass.** Generator + generated list + drift test
~250 lines across 3 new files; collision-analyzer extension ~40 lines across two existing files; the
emission-site qualification helper plus ~10–15 call-site swaps; ~5 lines for the import-alias set; 2–3
behavioral projects with goldens and `.slnx` registration.

⚠ **The lane owes a CNR run and must not assume zero corpus drift.** Five stdlib packages already
declare shadowing names (table above); the moment `packageBuiltinShadows` learns about them, any
implicit helper call *inside those packages* starts emitting `builtin.X(…)`. `runtime` in particular
declares three of them (`mapclone`, `fatal`, `i`) and is a large package. Expect a small, explainable
emission diff in `runtime`/`math`/`sync`/`log/slog`, verify each hunk is exactly a qualification, and
budget for a targeted regen rather than assuming none.

### Two side findings this scout tripped over, both worth their own chips

* **Δ-renamed unexported types are emitted PUBLIC — a live, corpus-wide over-export.** The
  `<TypeAccessibility>` block computes exported-ness from the *emitted* name, and `Δ` (U+0394, an
  uppercase Greek letter) reads as exported. Measured in the corpus, not inferred:
  `database/sql/package_info.cs:132` `public partial struct ΔconnStmt {}` (Go `connStmt`,
  unexported), `encoding/gob:101` `public partial interface ΔgobType {}` (Go `gobType`),
  `syscall/windows:76,150,151` `ΔSockaddr`/`ΔHandle`/`ΔSignal`, `debug/{pe,macho,plan9obj}` `ΔSection`,
  `database/sql:130,131` `ΔConn`/`ΔStmt`, `database/sql/driver:118` `ΔRowsAffected`, `syscall/linux:149`
  `ΔSignal`. It is already **banked in a golden** — `ReservedNameShadows/package_info.cs` carries
  `public partial struct Δbuiltin/Δsstring/Δany/Δrune/Δnint {}`. `getAccess` (`identifierNaming.go:260`)
  *does* strip `Δ` before judging exported-ness, so the `package_info` writer is reaching the answer
  by a different path; that path is the bug. Not a correctness break — C# over-visibility only — but
  it leaks unexported Go types into the public API surface of every NuGet-published package, so it
  matters more than it looks.
* **`src/core/GlobalUsings.cs` is dead and stale** (see above). Delete it or wire it; leaving a file
  that *looks* like the global-using mechanism next to the real one costs the next reader a full
  investigation.

### What cut against the brief

* **The brief expected "silent capture of converter-emitted helper calls" to be the common case for a
  user func in the same package. It is the RARE case.** R1 stops lookup at the first scope, so an
  incompatible signature is a hard compile error — 4 of the 5 non-benign rows are loud. Only an
  *accidentally signature-compatible* declaration goes silent (p14). That is better than feared, and
  also worse: the one silent row has no diagnostic whatsoever, and its trigger (`func tmpstring([]byte)
  string`) is a shape a real Go program would plausibly contain.
* **The brief flagged unicode-prefixed names (`ж`, `Ꮡ`, `Δ`, `ᴛ`) as low-probability collisions. They
  are effectively zero-probability *and already defended*** — `identifierNaming.go:94` puts
  `PointerPrefix`/`TrueMarker`/`OverloadDiscriminator`/`EllipsisOperator` into `reserved` from the
  generated symbol table, the one place the symbol table and the reserved logic already meet. No
  probe was spent there.
* **Type-name collisions were expected to be the soft spot; they are mostly the best-defended area.**
  `slice`, `array`, `channel`, `builtin`, `sstring`, `any`, `rune`, `nint`, `nuint`, `GoFrame` all
  Δ-rename correctly today (p10, p13). The type-side gap is narrow: `@string` (p17) and the
  `error`/`comparable`/`uintptr`/`complex64`/`sslice` set, which `ReservedNameShadows` currently
  treats as *pass-through controls* — and does so only as **locals**, never as package-level types.
  That is the precise blind spot.
* **R2 was not anticipated by the brief and materially shrinks the fix.** Half the scary-looking
  names (`heap` at 1,858 call sites, `zero`, `subslice3`, `slice`, `make`, `clear`, `min`, `max`) are
  immune because their call sites carry explicit type arguments — proven by p03/p15 (a *local
  variable* named `heap` sitting directly above a `heap<box>(out var Ꮡb)` call compiles and runs
  correctly). Do not spend the fix on them.
* **The corpus check came back richer than "clean".** The brief expected no stdlib package to trip
  this. Strictly true — but five packages already declare shadowing names and one of them
  (`runtime.mapclone`) has a byte-identical signature to the golib helper it hides. The risk is
  user-code-facing *today*; it is corpus-facing the moment `runtime` gains a `maps.Clone` call.

### Reproducing

The 21 probe packages were built under the session scratchpad and **deliberately not committed** —
they are all currently-failing or currently-passing-by-accident, so committing them would either
break the behavioral suite or bank the wrong baseline. Every probe's Go source is reproduced in the
matrix descriptions above and each is 10–25 lines; the guard spec is the durable form. Method:
`go2cs.exe -go2cspath <worktree>/src <dir>`, then
`dotnet build <dir>.csproj -c Debug -p:go2csPath=<worktree>/src/ -p:UseSharedCompilation=false`,
then run and diff against `go run .`.

## ✅ `fmt`'s compile wall is CLOSED — two roots, not four; 62 of 63 verdicts agree (2026-08-16, lane `claude/fmt-roots`)

**The census above was stale in the usual direction, and the usual way.** It recorded four
remaining roots after the `CS1955` map-nil row closed. Re-measured on master (`9c64f0a44`) the
compile wall is **two** roots — the census's last three rows were, as it predicted, "one type's
story", and they are indeed ONE root. Both are general converter defects; neither is
`fmt`-specific; both now carry failing-first behavioral guards.

### Root 1 — a cast's operand asks TWO questions, and the arm asked only one

`CS0030 float→renamedComplex64 / double→renamedComplex128` ×4 (8 diagnostics across the internal
and external test variants). The named-numeric **identity-constant** arm of `convCallExpr` (reached
because go/types gives a constant operand the target type, so the conversion reads as an identity)
consulted only `castOperandNeedsParens` — a leading-sign TEXT test that answers the *cast-vs-
subtraction parse ambiguity*. It never asked the **precedence** question: a C# cast binds tighter
than every binary operator, so a constant operand rendering as a top-level binary expression has
the cast claim its **left operand alone**.

```csharp
((renamedComplex64)3F + 4F.i())   // was; the cast applies to 3F alone, CS0030
((renamedComplex64)(3F + 4F.i())) // now
```

**The compile error is the mild symptom.** The same emission is **silently value-changing** on
every named int/float type, where the `[GoType]` wrapper supplies an operator for the mis-bound
first leg and the code compiles clean:

| Go | emitted (before) | Go value | C# value |
|:--|:--|:--|:--|
| `rf(3 / 2)` | `((rf)3 / 2)` | `1` | `1.5` |
| `rf(7 / 2)` | `((rf)7 / 2)` | `3` | `3.5` |
| `rf(1 / 3)` | `((rf)1 / 3)` | `0` | `0.3333333333333333` |

Go folds the constant expression in exact arbitrary precision — untyped INTEGER division — and
then converts; the mis-bound cast converts first and divides in the target's own float arithmetic.
Measured end to end, not argued: a four-line probe printed `1 3 0 1` under Go and
`1.5 3.5 0.3333333333333333 1.25` under the transpiled C#. A named **complex** target has no
float→named-complex conversion at all, which is the only reason the class ever surfaced as an
error rather than as a wrong answer.

Keyed on the AST (`*ast.BinaryExpr`), not the rendered text — only the written expression says
whether a binary operator is left exposed. **Unary operands are deliberately excluded**: a cast and
a unary operator share precedence and associate right, so `(T)~0` already means `(T)(~0)`.
Guard: `NamedConstConversionPrecedence` (output-compared, so the silent value divergence fails the
gate and not merely the CS0030). Detail: `docs/ConversionStrategies-Reference.md` §Named Numeric
Types and Constant Contexts, "A cast's operand asks TWO questions".

### Root 2 — `inFunction` does not mean "there is a function DECLARATION"

`CS1729` + `CS0103` + `CS0034` — the census's last three rows, and one root exactly as predicted.
`convFuncLit` sets `inFunction` for a literal's body (correctly — its locals are function scope),
but `currentFuncName` and `currentFuncPrefix`, the lift's **name prefix** and its **declaration
sink**, are allocated together by `visitFuncDecl`. For a literal in a package-level initializer
they therefore held whatever the PREVIOUS function declaration in the file left behind. Every
type-lift site keys on `lifted && inFunction` and writes into that prefix, so `fmt`
scan_test.go's

```go
{"ReaderOnly", func(s string) io.Reader { return struct{ io.Reader }{strings.NewReader(s)} }},
```

lifted a type named after an unrelated preceding function and wrote its declaration into that
function's already-flushed buffer. The declaration **vanished**; only the use site survived —
`new Scan_type(…)`, CS1729 (no one-argument constructor), plus CS0103/CS0034 in the
`ImplementGenerator` wrapper generated for the phantom type from its surviving
`[assembly: GoImplement]` record.

**And it has a second, worse symptom the census never saw.** With NO preceding function
declaration the buffer is not stale but **nil**, and the lift panics (nil receiver in
`strings.Builder.copyCheck`). That panic is recovered per file, so the entire FILE is skipped with
only a `visit file error` warning — a silent, total loss of one file's conversion. Which symptom
appears depends solely on declaration order within the file, which is why `fmt` showed the compile
errors while a minimal standalone reproduction showed the panic.

A package-level literal now gets its own sink, flushed at **package scope** — where a lifted type
belongs anyway, and exactly where the sibling package-level lift (`readersᴛ1`) already goes — with
its name seeded from the declaration being initialized (`readers_type`, unique per var as
`readersᴛ1` already is). Guard: `PackageVarFuncLitTypeLift`, whose two files cover both symptoms
(`main.go` puts a function declaration before the var; `varfirst.go` declares the var first).
Detail: `docs/ConversionStrategies-Reference.md` §Struct Types, "A lift inside a PACKAGE-LEVEL func
literal".

### Root 3 (bridge) — `reflect.Value.Bytes` had no Array arm

Behind the compile wall the first run threw
`InvalidCastException: go.array<byte> → go.slice<byte>` out of the hand-owned
`core/reflect/value_impl.cs`. Go's `bytesSlow` accepts an **addressable byte array** as well as a
byte slice, and fmt's `printValue` calls `Bytes()` whenever `f.Kind() == Slice || f.CanAddr()` — so
`Sprintf("%s", &[3]byte{'a','b','c'})`, whose pointer deref IS addressable, reached a function that
handled only slice shapes. `CanAddr()` was right; `Bytes` was incomplete. It now takes Go's Array
arm, aliasing the array's storage through `array<T>.Slice` exactly as Go's `unsafe.Slice(p, n)`
does (a copy would silently break writers), and panics with Go's own text on an unaddressable byte
array. **Zero regression risk by construction:** that shape previously always threw.

### `fmt` census — 63 verdicts, 62 agree, 1 open, 0 disclosed

| | |
|:--|:--|
| Go | 62 pass, 1 skip (`TestCountMallocs`, `GOMAXPROCS>1`) |
| C# | 61 pass, 1 skip, **1 infrastructure-error** |
| Divergences | **1** — `TestSprintf` |
| Excluded | 42 (standard Phase-4D `Benchmark`/`Example`/`Fuzz` deferrals) |
| Disclosed | 0 |

**Not banked**, and deliberately: banking wants genuine closure, and `TestSprintf` is a real
divergence rather than a disclosure candidate (the existing disclosure classes are alloc-count
asserts the CLR provably cannot satisfy; a formatting mismatch is not one).

### What `TestSprintf` is standing on — three co-located bridge divergences

All three live inside that one table-driven test, so none can move the verdict alone. Each is
measured, not inferred; the first two are reported by the test itself before the third aborts it.

| # | Entry (`fmt_test.go`) | Go | C# | Root |
|:--|:--|:--|:--|:--|
| 1 | `{"%#12.5g", 1230000 - 0i, …}` (line 565) | `+0.0000i` | `-0.0000i` | The converter emits Go's constant complex expression as RUNTIME arithmetic (`1230000D - 0D.i()`), and .NET's `double - Complex` computes `-right.Imaginary`, turning `+0` into `-0`. Go folds the constant exactly, and an exact zero has no sign. Isolated in a probe: only the SUBTRACTION form diverges — `complex(1230000.0, 0.0)` and `1230000D + 0D.i()` are both correct |
| 2 | `{"%#v", TestFmtInterface, "(func(*testing.T))(0xPTR)"}` (line 717) | `(func(*testing.T))(…)` | ``(Action`1)(…)`` | `GoReflect.TypeNaming.cs` has **no delegate handling at all** — rendering a Go func signature from a CLR `Action`/`Func`/`Actionꓸꓸꓸ`/`Funcꓸꓸꓸ` is an unimplemented bridge feature, not a defect. Sized as its own arc |
| 3 | `{"%x", []renamedUint8{…}}` and five siblings (lines 734–735, 839–842) | `68656c6c6f` | throws | `Bytes()` on a slice whose element is a NAMED uint8 type. Go permits it (`Elem().Kind() == Uint8`) and ALIASES through the slice header; the bridge holds `slice<renamedUint8>` of wrapper structs, which is not layout-compatible with `byte[]`, so no safe aliasing conversion exists. Deliberately NOT half-fixed here — a copy would silently break writers of a core reflect API. A representation-level question |

**Fix 1 was measured and deliberately NOT landed.** Folding complex constant expressions is the
right general answer (the established precedent is `foldedNamedFloatConstLiteral`, which folds
float constant conversions so Go's exact evaluation rounds only once). But the census says the
subtraction form has **zero production sites** corpus-wide — all 115 are test files in `fmt`,
`math/cmplx`, `strconv` and `internal/fmtsort`, three of which are BANKED — so landing it would
churn banked packages' committed test sources and owe them a re-sweep, while moving no verdict on
its own (2 and 3 would still fail). It belongs with the `TestSprintf` arc, not ahead of it.

**Recommendation for whoever takes `TestSprintf`:** it is one lane, and #2 is its centre of gravity
— #1 and #3 are each contained, #2 is a bridge feature. All three must land together for the
verdict to move, and `fmt` then banks at 63/63. That bank is worth more than its own row: the
behavioral suite's ~520 stdout comparisons all run through converted `fmt`, so `fmt`'s own
`%v`/`%T` table is the deepest test the reflection bridge has, and every Printf-comparing
behavioral test is strengthened by it.
## ⛔ STILL DOES NOT BANK — `crypto/tls` holds at **176 of 184**, but the four divergences are now four DIFFERENT things, and only one of them is a disclosure (2026-08-16, lane `claude/tls-endgame`)

The `sha3` entry above ended by saying "the question `crypto/tls` now poses is a banking question,
not a debugging one", and named the four remaining divergences as `GetAddrInfoW`, an
`op_OnesComplement` singleton, `TestCertCache` and `TestBogoSuite`. This lane took all four. The
headline number does not move — the same per-test method (one process per top-level `Test*`,
`-test.run '^Name$'`, 25 s cap, raw stdout saved) measures **176 PASS, 7 FAIL, 1 process-AV** in
853 s — and that is the finding rather than a disappointment: **`GetAddrInfoW` is FIXED and the test
that measured it still fails, because a second wall of the same family stood behind it.**

| Measure | after `default!` | after `sha3` | now |
|---|---|---|---|
| top-level tests that PASS run on their own | 163 of 184 | 176 of 184 | **176 of 184** |
| real divergences (Go passes, C# does not) | 17 | 4 | **4** |
| distinct roots behind them | 5 | 3 | **4** |
| of those, disclosable under an existing class | 0 | — | **1** |

Go on this host, re-measured rather than carried: **184 top-level, 180 pass, 4 fail**, 79.9 s. The
four failures are the same expired-fixture set (`TestResumption`,
`TestResumptionKeepsOCSPAndSCT`, `TestVerifyConnection`, `TestCrossVersionResume` — the test
certificates expired 2025-01-01), and the converted host fails **exactly those four, with the same
`x509: certificate has expired` text**, so they are AGREEING rows, not divergences. The cross-tab
closes with no remainder: 176 agreeing passes + 4 agreeing failures + 4 divergences = 184.

### 1. `GetAddrInfoW` — FIXED, and the class gained its LINKED-OUTPUT member

The open non-blittable-syscall class CLAUDE.md has carried since the `Timezoneinformation` fix
("`net` and `crypto/x509` will" reach it) now has its DNS member closed. Native `ADDRINFOW` is 48
bytes of scalars and raw pointers where the converted `AddrinfoW` holds `Canonname`, `Addr` and
`Next` as managed references, so the hints Windows read were garbage and the `*ADDRINFOW` it wrote
landed in a reference slot — `Fatal error. 0xC0000005` inside `Syscall6`, killing the process.

What makes this member different from every earlier one, and what the next member of the class
should read first: **copying the top-level record would not have been enough.** `net` reads the
sockaddr THROUGH the result (`(*syscall.RawSockaddrInet4)(unsafe.Pointer(result.Addr))`), and
`RawSockaddrInet4.Addr [4]byte` is an `array<byte>` — a backing-array REFERENCE plus bounds — so
reading that struct out of a native `sockaddr_in` fabricates managed references from address bytes.
That is the fork the `sha3` entry proved has no general fix. So the hand-own transcribes the WHOLE
chain into managed boxes, sockaddr included, typed by `ai_family`; frees the native chain eagerly
(which makes `FreeAddrInfoW` a hand-owned no-op — nothing native escapes the call); and carries the
managed sockaddr pointer across the `unsafe.Pointer` field through golib's `ManagedPointerTokens`,
whose only previous minter was the reflection bridge. Two properties worth carrying:

* the token must be wrapped in a `Pointer` built over a **native-address** `ж<EmptyStruct>`, because
  the generated named-pointer wrapper's `uintptr` conversion returns the address of the storage its
  box addresses — which for a native box IS the number handed in, so the token survives unchanged;
* the token table is **weak by design**, so the hand-own owns the strong reference — a
  `ConditionalWeakTable` keyed on the record box, which is exactly the Go lifetime. Without it a live
  token could name a collected box and the consumer's cast would fall back to a wild native read,
  turning a loud defect into a silent one.

Guarded by the new **`LookupServicePort`** behavioral output test: `net.LookupPort` for
`tcp`/`udp`/`tcp4`/`tcp6` services is the one reach into this pair needing neither DNS nor a network,
and it exercises the hints mirror, the chain copy, the token handoff and BOTH sockaddr flavors at
VALUE level (a byte-order slip prints a swapped port rather than failing). Proven failing-first: with
the hand-own removed the guard reports `exit code mismatch: C# -1073741819 vs Go 0` — the access
violation itself. Marker census: the two entries join `manualConversionFuncs["syscall"]`, and
`TestWindowsOnlyEntriesAreScopedToWindows` now pins their platform scope.

### 2. …and `TestVerifyHostname` STILL fails, one layer further out

It now dies in **`net.adapterAddresses`**, which is `dnsReadConfig`'s only source of DNS servers on
Windows:

```
Fatal error. System.AccessViolationException
   at go.ж`1[IpAdapterAddresses].op_Equality(ж`1<IpAdapterAddresses>, NilType)
   at go.net_package.adapterAddresses()
   at go.net_package.dnsReadConfig(string)      <- via getSystemDNSConfig, from lookupIP
```

Same family, bigger structure, and NOT a wrapper defect: `adapterAddresses` asks
`GetAdaptersAddresses` to fill a managed `slice<byte>` (that part is legitimate) and then walks it as
`Ꮡ(b, 0).Reinterpret<byte, IpAdapterAddresses>()`. `IP_ADAPTER_ADDRESSES` is a linked record with
three raw string pointers, a `[8]byte` physical address, a `[16]uint32` zone-index array and **six**
nested linked lists; reading it out of a byte buffer fabricates a managed reference on the first
field touched — here the `Next` comparison itself. The remedy is the same transcription shape this
lane just built for `ADDRINFOW`, one structure size up, and it belongs to a **`net` interfaces arc**
(`adapterAddresses` also backs `interfaceTable`, `interfaceAddrTable` and `Interfaces()`), not to
`crypto/tls`. It is what stands between the corpus and any name resolution at all on Windows.

### 3. The `op_OnesComplement` singleton is a SECOND syscall class — `**T` OUT-parameters arrive NULL, censused at 13

The brief for this lane (and the entry above) read `TestQUICHandshakeError` as "a golib operator on a
nil box … make it a Go-shaped panic, not an NRE". **Both halves are wrong, and the correction is the
most transferable thing here.** The panic already IS Go-shaped — golib's `~` raises
`RuntimeErrorPanic.NilPointerDereference`, printed as `panic: runtime error: invalid memory address
or nil pointer dereference` — and golib is the innocent frame. The nil comes from three frames out:

```
crypto/x509 systemVerify -> (*storeCtx).Store        <- storeCtx is nil, and err was nil
             createStoreContext -> CertAddCertificateContextToStore(handle, leafCtx, ADD_ALWAYS, &storeCtx)
```

`&storeCtx` is a `ж<ж<CertContext>>`, and golib's `ж<T> → uintptr` answers **0** for it: `IsNull` is
the VALUE-PEEKING question, and a heap-boxed POINTER legitimately holds null before the call. That
answer is deliberate and correct for the case it was written for (`syscall.Write` hands `writeFile` a
nil `*Overlapped`; `uintptr(unsafe.Pointer(nil))` is 0 in Go) and silently wrong for the shape every
out-parameter takes: the wrapper tells the kernel "no output wanted", `ppStoreContext` is documented
OPTIONAL, the call SUCCEEDS, and the caller reads back the nil it started with. Note the 0 is
accidentally the SAFER outcome — a real address would have had the kernel write a native pointer into
a slot the collector reads as an object reference — so the remedy is a per-wrapper hand-own, never a
change to the operator.

**Census of the emitted wrappers taking a `**T`: 13.** `CertAddCertificateContextToStore`,
`CertGetCertificateChain`, `ConvertSidToStringSid`, `ConvertStringSidToSid`, `DnsQuery`, `_DnsQuery`,
`GetFullPathName`, `getQueuedCompletionStatus`, `GetQueuedCompletionStatus` in `syscall`; plus
`CreateEnvironmentBlock`, `NetGetJoinInformation`, `NetUserGetInfo`, `NetUserGetLocalGroups` in
`internal/syscall/windows`. Two more of the shape are already hand-owned for other reasons
(`GetAddrInfoW`, `GetAcceptExSockaddrs`). Not fixed speculatively, on the standing
fix-it-when-a-suite-reaches-it rule.

Closing `TestQUICHandshakeError` is therefore a **`crypto/x509` Windows system-verifier arc**: the
out-parameter is only the first defect, because `CertContext`, `CertChainContext`, `CertSimpleChain`
and `CertChainElement` are all read back through raw addresses afterwards — the fabricated-reference
fork again, over six CryptoAPI wrappers.

### 4. `TestCertCache` IS `codegen-liveness`, and it is the only disclosable row

The test nils its own local, calls `runtime.GC()`, and polls for 4 s waiting for a finalizer to
decrement a ref count. Measured, not argued:

* Go passes it in 0.00 s; the converted host fails at the **first** check (`refs 2 → 1`) — 4.3 s of
  polling on top of a 3.8 s host startup, and `t.Fatal` aborts before the second check.
* It fails **identically in an optimized Release build** (a separately built Release host), so it is
  not the non-optimizing JIT's frame-wide liveness.
* The finalizer bridge itself works: `runtime.SetFinalizer` is the hand-owned
  `ConditionalWeakTable` + sentinel bridge, `runtime.GC()` is `Collect → WaitForPendingFinalizers →
  Collect`, and `sync`'s banked `TestPoolGC` measures 98 of 100 objects finalizing on the first try.

That is the roster's existing `codegen-liveness` class verbatim — "a test asserts, from inside its own
frame, that an object it just stopped referencing is collectible". The likely rooting slot is the same
one `sync`'s `TestOnceXGC` disclosure names: `var (certA, err) = cc.newCert(...)` materializes an
address-exposed tuple temp, and an address-exposed slot is not lifetime-tracked. That last step is
INFERRED from the class's known mechanism, not read out of the JIT — the three bullets above are the
measurements. **No new class, so no coordinator ruling is owed**; the row is disclosable whenever the
package can bank.

### 5. `TestBogoSuite` is NOT "external infrastructure" — it is a fixable test-host defect

The previous entry recorded it as "an external binary … not a conversion signal". Measured, that is
wrong. Go passes it on this host, so BoGo downloads, builds and runs here. It drives the shim as
`-shim-path=os.Args[0] -shim-extra-flags=-bogo-mode`, i.e. it re-executes **the test binary itself**.
Run the converted host that way and it answers:

```
> tlsendHost.exe -bogo-mode
flag provided but not defined: -bogo-mode
```

`bogo-mode` IS registered by the converted source (`handshake_test.cs:50`, a `flag.Bool` package-var
initializer) — but `testing`'s host parses `args` itself in `TestOptions.Parse` and **throws on any
name it does not recognize**, before the package's own `flag.Parse()` in `TestMain` ever runs. Go's
test binary cannot behave that way: `testing.Init()` merely DEFINES the `-test.*` flags on
`flag.CommandLine`, and one `flag.Parse()` then covers the host's flags and the package's together.
`TestFlagBridge` already does the first half (publishing the host's flags into `flag.CommandLine`); the
missing half is tolerating the package's. So every BoGo case fails instantly, the runner exits 1
without writing results, and the test reports `bogo failed: exit status 1`.

Sizing it honestly: fixing the pass-through is small and general (any package with its own test flags
benefits), but it lives in the shared hand-owned `testing` host, so it owes the **full validated
sweep** as a gate — and it must defer the unknown-flag ERROR to `flag.Parse()` rather than dropping
it, or the host stops rejecting typos. And even fixed, `TestBogoSuite` becomes a **long external TLS
interop measurement**, not a pass: it is its own arc. Deliberately not taken here.

### One more thing the bogo failure printed, unrelated and worth a look

`t.Fatalf("bogo failed: %s\n%s", err, out)` printed the second operand as `0x19c45d7c4f8`. `out` is a
`*strings.Builder`, whose pointer-receiver `String()` makes it a `fmt.Stringer` in Go; the converted
`fmt` reached its `p.arg._<Stringer>` assertion and missed. Reproducible on every run of that test.
Whether this is general (any `%s`/`%v` on a pointer whose `String()` was never cast to `Stringer` in
converted source) or specific to `strings.Builder` is UNMEASURED — recorded with its exact site so
whoever looks does not have to find one.

### Where `crypto/tls` stands

**Four divergences, four different owners, and only one of them is `crypto/tls`'s to answer.** Two
are Windows syscall arcs that other packages want more than this one does (`net`'s interfaces,
`crypto/x509`'s system verifier); one is a `testing`-host gap; one is a disclosure. Nothing here is a
TLS defect — the protocol work has been green since the `sha3` fix, TLS 1.2 and 1.3, QUIC, session
tickets, ECH and the whole handshake matrix. **The expired-fixture ceiling deserves repeating for
whoever eventually banks it: 180 of 184 is the most this host can score, both languages fail the same
four rows, and it worsens with time** — a Go patch release or regenerated fixtures changes the shape,
and a proof page written today must say so.

Still builds-and-partly-runs; no roster row, no proof page, no disclosures, converted test sources not
committed.

## ⛔ `encoding/json` — the wall is down and the package RUNS: 400 of 491, four roots (2026-08-16, lanes `claude/json-measure` + `claude/json-unlock`)

**First measurement of the package.** The board's prior rows — `encoding/json | 491 | CS0050
inconsistent accessibility on a test-local return type` and `encoding/json 0 of 491 (CS0050/CS0053)`
— **reproduce exactly**, and the diagnosis behind them was right about the symptom and one word off
about the cause: the offending types are not "test-local", they are **function-local**, and that
distinction is the whole fix.

### The wall: 76 errors, four codes, one cause — CLOSED

| Code | Count | Shape |
|:--|--:|:--|
| CS0053 | 35 | property type less accessible than property |
| CS0050 | 20 | return type less accessible than method (the generated `Ꮡ`-accessors) |
| CS0051 | 19 | parameter type less accessible than method (the generated constructors) |
| CS0052 | 2 | field type less accessible than field |

**A type declared inside a function body has no Go exportedness.** The export convention governs
PACKAGE-LEVEL identifiers; a function-local `S8` is exactly as unreachable from outside its function
as `embed2` is, and Go draws no distinction between them. go2cs hoists both to package scope as
`<Func>_<name>`, and at that point an accessibility rule reads a case out of the name — by either of
two routes, which is why the failure looked like several defects:

1. **the converter's bridge arm** (`visitTypeSpec`, under `testInlineTypeAccess`) asked
   `generatedTypeScope` for the **LOCAL** name, so the siblings one function declares split
   public/internal — `[GoLocalName("S8")] public partial struct TestUnmarshalEmbeddedUnexported_S8`
   holding a field of `[GoLocalName("embed2")] internal partial struct
   TestUnmarshalEmbeddedUnexported_embed2`;
2. **a lifted ANONYMOUS struct** carries no modifier at all, so **go2cs-gen's** own rule read the
   **HOISTED** name and inherited the case of the *enclosing function* — `TestEncoderSetEscapeHTML_type`
   is public because the `Test…` function is, and its exported fields over the package-level
   unexported `strMarshaler`/`strPtrMarshaler` are the two CS0052.

**Fix (landed):** a function-local type is emitted `internal`, always — `localTypeAccess` in
`typeAccessibilityOperations.go`, consumed at the three finalization points (`visitTypeSpec`'s bridge
arm, `visitStructType`'s and `visitInterfaceType`'s lift defaults). `internal` is both faithful and
sufficient: no Go consumer outside the function can name the type, and every emitted C# consumer
compiles into the same test assembly. Writing it **inline** is what makes the generator follow —
measured, not assumed: the generator reproduces a modifier the declaration already carries
(`internal partial struct TestUnmarshalEmbeddedUnexported_embed2` appears verbatim in its output) and
falls back to its name rule only for a bare one. Guarded by `TestFunctionLocalTypesShareOneAccessibility`
(converter `go test`), which pins all three shapes and **fails without the fix** (A/B verified —
`TestLocals_S8` reverts to `public`).

**Deliberately scoped to the bridge.** The production path leaves the modifier empty and lets
`recordTypeAccessibility` pin `generatedTypeScope` of the **mangled** name, which gives every local
type of one function the SAME modifier — uniform, and consistent for that reason rather than by
design. ⚠ **The identical latent mixture exists in production** (a function-local struct with an
exported field of a package-level unexported type) and no corpus package exhibits it today; flipping
production local types to internal would also move a public value adapter's operand out from under
it. Measured and left, not changed speculatively — if a production package ever presents the shape,
this is its root.

### Behind it: two roots, 8 errors, and the package still does not run

Re-running the pipeline on the fixed converter took the wall from 76 to **8**, in two families that
were **masked, not caused** — `renamedByte`, `strMarshaler` and `strPtrMarshaler` are package-level
types the fix does not touch, and they error now for the first time.

**R2 — a hoisted function-local type's EMBEDDED-field member keeps the hoisted name, while use sites
spell the Go field name** (CS1061 ×2, CS1739 ×1). The converter is internally inconsistent about one
name. `type S struct{ *myInt }` inside `TestAnonymousFields` emits the member as
`TestAnonymousFields_myIntᴛ1` (declaration *and* go2cs-gen's promotion and constructor), while the
converted body says `s.myInt` and the composite literal says
`new TestUnmarshalEmbeddedUnexported_S3(embed1: …)`. The Go field name of an embedded `*myInt` is
`myInt`, and `[GoLocalName]` already records it for `%T` — the promotion simply does not use it.
Naming the member by the Go local name is collision-free (it lives inside the struct), but the fix
spans the converter's embedded-field emission **and** go2cs-gen's promotion generator, so it is its
own arc.

**R3 — a string↔byte-slice conversion loses its footing when either side is a DEFINED type**
(CS1503 ×5), in two directions of one idea:

| Direction | Emitted | Sites |
|:--|:--|:--|
| `[]byte(namedString)` | `slice<byte>(v)` where `v` is `[GoType("@string")]` | `marshaledValue`, `strMarshaler`, `strPtrMarshaler` |
| `[]NamedByte(string)` | `slice<TestSliceOfCustomByte_Uint8>("hello")`, `slice<renamedByte>((@string)"abc"u8)` | `TestSliceOfCustomByte`, `TestEncodeRenamedByteSlice` |

Overload resolution lands on `slice<T>(T[])` and reports `cannot convert from '…strMarshaler' to
'byte[]'`. golib's string constructor is reachable only for the exact `@string` → `slice<byte>`
pairing; a named type on either side is not routed through it. **The emitted FORM is not the
problem** — positive control: the identical `slice<byte>(s)` conversion appears in the banked suites
of `archive/zip`, `bytes` and `compress/zlib` and compiles there, because the operand is a plain
`@string`. What is missing is the unwrap of a DEFINED type to its underlying at the conversion site
(or the participation of the generated implicit conversion in overload resolution) — a
golib/converter boundary question that wants a measurement of its own. The shape is ordinary Go, so
it is very likely NOT confined to this package.

### Where `encoding/json` stands

**0 of 491, still compile-blocked — but the block is now 8 errors in two named roots rather than 76
in one, and the converter fix that closed the first is general (any package whose tests declare types
inside a function body).** No roster row, no proof page, no disclosures, converted test sources not
committed. The suite converts cleanly end to end — every `_test.go` file emits, and the production
`encoding.json.dll` builds — so the remaining distance is exactly R2 and R3, and neither is a
reflection-bridge question. Nothing here touches the descriptor/assignability semantics the
`claude/assignableto-arc` lane owns; no cross-reference is owed.

---

### UPDATE 2026-08-16 (lane `claude/json-unlock`) — both roots closed; FIRST measurement: 400 of 491

R2 and R3 are fixed, the test half compiles with **0 errors**, and `encoding/json` ran for the first
time. It does not validate: **400 of 491 verdicts match**, 91 diverge. The reflection-heavy body is
now measured rather than guessed at, and the divergences cluster the way this week's other packages
did — **four roots and one loose end**, none of them an emission defect.

**R2 — an embedded field is named by GO, not by the C# rendering of its type.** `visitStructType`
derived the member name from the rendered type, which coincides with the Go field name for every
ordinary embed and parts from it the moment the converter RENAMES the type: a function-local `type
myInt int` hoists to `TestAnonymousFields_myIntᴛ1`, and the declaration (plus go2cs-gen's generated
constructor and promotion accessor, both read off it) then spelled that while every use site spelled
`s.myInt` / `S3{embed1: …}`. The name now comes from the Go OBJECT the embed resolves to — the field
`*types.Var` for a same-package embed, the embedded type's `TypeName` for a selector embed — which is
the Go field name by definition and is already unqualified and type-argument-free, so it REPLACES the
bracket/dot stripping rather than adding to it. It also settles the field's EXPORTEDNESS, which the
hoisted name silently flipped (`embed1` is unexported; the `TestUnmarshalEmbeddedUnexported_` prefix
made the member public — the opposite of what that test asserts). The generator followed: its
promoted-struct accessor scoped by TYPE name where every sibling accessor already scoped by MEMBER
name, so the corrected declaration met the opposite modifier (CS8799). Guarded by `LiftedLocalTypes`
(extended), proven failing-first.

**R3 — a string ↔ byte/rune-slice conversion with a DEFINED type on either end.** Two ends, two
remedies, one rule: the STRING end spells the `(@string)` hop explicitly (a `[GoType("@string")]`
wrapper needs wrapper→`@string`→`byte[]`, two user-defined conversions, and C# chains at most one);
the ELEMENT end projects element-wise through the wrapper's own operator with golib's `widen`
(`slice<byte>` and `slice<myByte>` are unrelated instantiations with NO conversion between them).
Go's string↔slice conversion always materializes fresh storage, so the element-wise copy is its cost
model, not a concession. **Corpus census (type-aware, `go/packages` over all of `std` with tests):
FIVE sites in the entire Go 1.23.1 standard library, production and test, all in `encoding/json`'s
own suite** — which is exactly why the corpus compiled clean without them; the `string([]myByte)`
direction has zero stdlib sites and is emitted by the same rule. Zero committed corpus files move.
A pre-existing sibling hole closed with it: a named byte-slice type converted from a string VARIABLE
(`plainByteSlice(s)`) was a bare cast, CS0030 — only the LITERAL form had ever been fixed. Guarded by
the new `DefinedElemStringConversion`, proven failing-first (23 errors without the fix).

### The 91 divergences

| # | Root | Verdicts | Evidence |
|:--|:--|--:|:--|
| A | **An embedded field is invisible to the reflection view as an EMBED** — `reflect` does not report the field as anonymous, so `encoding/json` never flattens it | ≈31 | every embed marshals as a NAMED object instead of promoting: `{"S1":{"X":2},"S2":{"X":4}}` for `want {}`, `{"S":"B","BugA":{"S":"A"}}` for `want {"S":"B"}`; `DisallowUnknownFields` then reports `unknown field "Level1b"` where Go reports `"extra"`. Covers `TestAnonymousFields`, `TestUnmarshalEmbeddedUnexported`, `TestEmbeddedBug`, `TestDuplicatedFieldDisappears`, `TestTaggedFieldDominates`, `TestMarshalEmbeds`, `TestUnmarshal/#56`–`#63`/`#89`/`#91`/`#93`/`#139`/`#140`. R2 was the naming PREREQUISITE for this; the anonymous FLAG is a separate seam |
| B | **`reflect.DeepEqual(ptr, reflect.New(T).Interface())` is false for a fresh zero** | ≈41 | `TestUnmarshal`'s own precondition — `unmarshalTest.ptr %#v is not a pointer to a zero value` — fires before the subtest's real assertion runs, so these 40-odd verdicts are UNREACHED rather than wrong. One fix would re-open them all |
| C | **The reflection view of a slice whose ELEMENT or whose SLICE TYPE is DEFINED** | 3 | `reflect.Value.Bytes()` (`core/reflect/value_impl.cs:616`) ends in `(slice<byte>)other!` and throws `InvalidCastException` for `slice<Uint8>` / `renamedRenamedByteSlice` (`TestSliceOfCustomByte`, `TestEncodeRenamedByteSlice`, both surfacing as `infrastructure-error`); `TestByteKind` instead marshals a local `type byteKind []byte` as a generic array, so its Kind is not seen either. ⚠ Note the asymmetry with R3: Go's `Value.Bytes()` ALIASES the storage, so this fix must project a view, not a copy |
| D | **Cycle detection recurses forever → `StackOverflowException` kills the host** | 13 unrecorded | `TestUnsupportedValues` marshals a self-referential map; Go returns `UnsupportedValueError: encountered a cycle`, the converted encoder recurses `interfaceEncoder`→`mapEncoder` without bound and the process dies with `0xc00000fd`. Per the jsonrpc rule this is ONE failure plus an unmeasured tail: `TestValid` ×7, `TestUnsupportedValues` ×4, `TestIndentBig`, `TestMarshalFloat` recorded no verdict at all |
| E | **A recovered panic value's boxed type** | 2 | `TestMarshalPanic`/`TestUnmarshalPanic` panic with `0xdead` and compare `reflect.DeepEqual(recover(), 0xdead)`; the C# side reports the right VALUE (`57005`) and still fails the compare — the untyped-int panic argument boxes at a different width than the comparand |
| — | loose end | 1 | `TestLargeByteSlice` round-trips a PLAIN 2000-byte `[]byte` and diverges at byte 0. No defined type anywhere in it, so it is not root C; unclassified, and worth one look because it is the only base64 round-trip failure |

**Roots A, B, C and E are all reflection-bridge seams; none is a converter emission defect.** Root A
is the biggest single unlock (a third of the divergences) and it is the classic Go-embedding contract
that `encoding/json`, `encoding/xml`, `encoding/gob` and `text/template` all read. ⚠ Root B is
adjacent to the descriptor semantics `claude/assignableto-arc` owns — cross-referenced, deliberately
not touched here.

No roster row, no proof page, no disclosures, converted test sources NOT committed: 400 of 491 is a
measurement, not a validation.

**Corpus footprint of both fixes: zero.** A seeded stdlib reconvert on the fixed converter (304
packages, 0 failed, 13m32s; marker gate 60 marked / 0 clobbered) emitted 1,681 artifacts, of which
1,629 are byte-identical to the committed tree and 51 of the remaining 52 are the documented CRLF
phantom (identical once CRs are stripped). ⚠ The one REAL difference is **not from this lane and is
owed by another**: `vendor/golang.org/x/text/unicode/bidi/core.cs` emits `((level)(maxDepth + 2))`
where the committed file has `((level)maxDepth + 2)` — the residue of the cast-precedence fix that
landed in `claude/fmt-roots` (`93ef5abaa`, guarded by `NamedConstConversionPrecedence`) without its
corpus regen. It is the arc's ONLY stdlib site; whoever next levels the corpus should expect exactly
that one file.

## ✅ `fmt` BANKS at 63/63 · ⛔ `encoding/json` reaches 482 of 491 — the REFLECT-BRIDGE CLOSURE arc (2026-08-17, lane `claude/reflect-closure`)

The union of the roots holding `encoding/json` (400/491) and `fmt` (62/63) turned out to be **one
subsystem with nine roots**, and they came out in dependency order exactly as the brief predicted:
descriptor cargo first, then the value operations, then the consumers. Two of the nine were on no
list at all — each stood *behind* one that was, which is this board's recurring shape.

| Package | Before | After |
|:--|--:|--:|
| `fmt` | 62 of 63 | **63 of 63 — BANKED, roster row 149** |
| `encoding/json` | 400 of 491, 13 unrecorded (process death) | **482 of 491, 0 unrecorded** |

`encoding/json`'s whole suite now runs in **24 s**, where before it either exhausted a 20-minute
deadline or died of stack exhaustion partway through.

### The nine roots

| # | Root | Where | Verdicts |
|:--|:--|:--|--:|
| 1 | `StructField.Anonymous` was never populated, so **no embed was an embed** to any Go encoder | `reflect/value_impl.cs` | ~31 |
| 2 | An embed's `[GoTag]` sits on the emitted partial PROPERTY while the backing FIELD is generated — so every embedded field read as untagged | `GoReflect.FieldAccess.cs` | with #1 |
| 3 | `new(T)` ran golib's ALLOCATING parameterless constructor, so `new([]T)` / `new(map[K]V)` pointed at a non-nil EMPTY container where Go points at nil | `builtin.cs` | ~41 |
| 3b | A POINTER descriptor never carried its POINTEE's array dims, so `reflect.New` over `TypeOf(new([3]int)).Elem()` allocated a ZERO-length array | `internal/abi/type_impl.cs`, `GoReflect.TypeLayout.cs` | with #3 |
| 4 | Three identity rules were each too FINE for a cycle detector: `unsafe.Pointer` compared by BOX, a map/slice `UnsafePointer` tokened the HEADER copy, and a struct's generated `Equals` compared an interface field with C# `==` | `unsafe.cs`, `golib/ж.cs`, `reflect/value_impl.cs`, **`go2cs-gen`** | 13 + the process |
| 5 | `Value.Bytes` threw for a DEFINED byte element, and `Value.SetBytes` wrote through the never-populated Go data word — silently, for EVERY byte slice | `reflect/value_impl.cs`, `GoReflect.ValueMarshalling.cs`, `slice.cs` | 3 + 1 (fmt) |
| 6 | An unnamed FUNC type had no Go rendering at all — `GoReflect.TypeNaming` had no delegate arm | `GoReflect.TypeNaming.cs` | 1 (fmt) |
| 7 | A COMPLEX constant expression was emitted as RUNTIME arithmetic, and .NET's `double - Complex` negates `+0` | converter, `convBinaryExpr.go` | 1 (fmt) |
| 8 | **(on no list)** An unexported EMBED took `flagStickyRO` where Go gives it `flagEmbedRO` — only the sticky bit inherits, so every field promoted through an unexported embed was read-only | `reflect/value_impl.cs` | 7 |
| 9 | **(on no list)** `rtype.FieldByName` searched DIRECT fields only, so a promoted name read as ABSENT — and `Value.FieldByName` then handed the EMPTY index to `FieldByIndex`, which answers the struct ITSELF | `reflect/value_impl.cs` | with #8 |

Two more surfaced as consequences of the fixes above and are closed with them: a NaN map key
compared equal to itself (BCL collection semantics, not Go's `==`) — `fmt`'s own `%v` of
`map[float64]int{NaN:1, NaN:1}` reads it back — and a variadic tail was detected by the delegate
FAMILY NAME, which C#'s natural delegate type does not carry, so `func(string, ...int)` reported
non-variadic and rendered `func(string, Span'1)`.

**Root 4 is the one worth reading twice.** Go's cycle detectors are written on identity —
`e.ptrSeen[v.Interface()]` for a pointer, `[v.UnsafePointer()]` for a map, and
`[struct{ptr any; len int}]` for a slice — and each of the three keys took a different route to
being unfindable. The third is a `go2cs-gen` defect with nothing to do with reflection: the
generated memberwise `Equals` compared an INTERFACE-typed member with C# `==`, i.e. by reference,
where Go compares interface values by dynamic type and value. Since a struct's `Equals` is also what
a map LOOKUP calls, such a struct could never be found under a key it had itself stored. The
consequence was not a wrong answer but a dead process: `Marshal` of a self-referential value recursed
until `0xc00000fd`, which is uncatchable and took every verdict the run had not yet produced with it.

Doctrine: `ConversionStrategies-Reference.md` gained seven sections — *An EMBEDDED field is an embed
to `reflect`*, *An UNNAMED func type renders STRUCTURALLY*, *`reflect.Value.Bytes`/`SetBytes` are
defined over the element KIND, and they ALIAS*, *`new(T)` is Go's ZERO value*, *An `unsafe.Pointer`
is compared BY ADDRESS*, *A NaN map key is never equal to anything*, and *A COMPLEX constant
expression must be FOLDED*.

### Where `encoding/json` still stands: 482 of 491, ONE root, and it is NOT rooted

`TestUnmarshal` rows **#56–#63** — eight subtests of one table — plus the aggregate row. Each decodes
into the 40-field `All` fixture and compares the result against the table's literal with
`reflect.DeepEqual`. `gotJSON` and `wantJSON` are **byte-identical**, so the decode itself is right;
the only structural difference the failure message shows is one element:

```
got  … [0x…  <nil>  0x…] …          // SliceP, the decoded side
want … [0x…          0x…] …         // SliceP, the table literal — middle element prints EMPTY
```

`allValue.SliceP` is `[]*Small{{Tag:"tag22"}, nil, {Tag:"tag23"}}`, emitted as
`new ж<Small>[]{Ꮡ(…), default!, Ꮡ(…)}.slice()`, so its middle element is a C# `null` where the
decoder writes the canonical typed-nil box. **That is the obvious hypothesis and it is measured
WRONG**: a minimal reproduction of exactly that shape — a nil hole in a `[]*T`, built once as a
literal and once through `reflect.MakeSlice` into a struct field, compared with `DeepEqual` and
printed with `%v` — agrees with `go run` on both counts, and it is committed as the
`ReflectBridgeClosure` behavioral test's `nil element` / `decoded vs literal` rows. Normalizing a
null pointer read to the canonical nil at `Value.live` was tried, moved nothing, and was
**reverted** rather than banked as speculative machinery.

So the root is something in the larger value, not the nil encoding alone. The next lane should start
by finding which of `All`'s forty fields `DeepEqual` actually separates — `%v` of the whole struct is
too lossy to say — and the cheapest instrument is a hand-written probe that walks the two values
field by field with `reflect.DeepEqual` rather than the test's single top-level call.

No roster row, no proof page, no disclosures, converted test sources NOT committed: 482 of 491 is a
measurement, not a validation.

### Adjacent, boarded rather than taken

* **A conversion of `nil` to a defined METHODLESS func type does not compile.** `handler(nil)` emits
  `new Func<nint, error>(default!)` — CS8716 + CS0149. It is converter emission work, outside this
  lane's boundary by the brief's own rule; the `ReflectBridgeClosure` test writes a declared value
  instead of the conversion so it fails on the naming rule alone.
* **A defined METHODLESS func type has no managed identity** — the converter renders it inline as its
  base delegate family — so `reflect` reports it as UNNAMED. A defined func type carrying a method
  does get its own delegate and keeps its name. Same shape as the `ChanDir` ruling: the bridge
  describes the type it can actually build a descriptor for.
* **Projected field ORDER puts every embed last.** `go2cs-gen` emits the promoted-embed backing field
  in a generated partial, after the declaring part's plain fields, so `Host{X; y; Inner; inner; Ptr}`
  projects as `X, y, Ptr, Inner, inner`. No measured consumer observes it — json's dominance rules
  read depth and tag, and its one order-sensitive test declares its single plain field first — so it
  is recorded rather than fixed; the remedy is declaration-order cargo, not a re-sort. The behavioral
  guard looks its fields up BY NAME so it cannot pin the gap as a contract.
* **A NAMED float type's wrapper, and a struct or array CONTAINING a float, still compare a NaN map
  key by the BCL rule.** Covering them would mean routing every struct-keyed map through the
  reflective relation, and no measured consumer reaches them.

### Corpus footprint, measured

A seeded whole-stdlib reconvert (304 packages, 0 failed, 9m00s; marker gate **60 marked / 0
clobbered**, full-file line-anchored scan) emitted 1,664 artifacts, of which 1,609 are byte-identical
and 49 of the remaining 55 are the documented CRLF phantom. **Six real differences, all owed:**
`encoding/gob/{encode,enc_helpers}.cs` (the complex fold renders `0+0i` as the bare `0D.i()` —
value-identical, Go's own spelling), `encoding/json/{decode,encode}.cs` (restored to the clean
emission after this lane's own pipeline runs), `fmt/README.md` (the validation badge `fmt` earns by
banking), and `vendor/golang.org/x/text/unicode/bidi/core.cs` — **the debt the `claude/json-unlock`
entry above predicted by name**, the residue of `claude/fmt-roots`' cast-precedence fix landing
without its corpus regen. It is paid here. `reflect/value.cs` also moves, by one Go body replaced with
a placeholder (`Value.SetBytes` joins the hand-owns).

⚠ **A caution for the next regen census: scan the WHOLE file for the hand-own marker.** A first pass
here read only each file's first 40 lines and reported **35** marked files against the real **60**,
which would have made the clobber gate vacuous for 25 hand-owns. The rule is already written as
line-anchored; it must also be whole-file.

## ✅ `encoding/json` BANKS at 491/491 — ONE root, and it was a map ENTRY read without its type (2026-08-17, lane `claude/json-lastroot`)

The entry above left `encoding/json` at 482 of 491 with the remainder **unrooted**: `TestUnmarshal`
rows #56–#63 plus the aggregate, all comparing a decoded 40-field `All` fixture against the table
literal with one top-level `reflect.DeepEqual`, and `Marshal` of both sides producing **byte-identical**
JSON. It also left the right next step — *find which of `All`'s forty fields `DeepEqual` separates* —
and that instrument cost about two minutes and answered on the first run.

| Package | Before | After |
|:--|--:|--:|
| `encoding/json` | 482 of 491 | **491 of 491 — BANKED, roster row 150** |

491 matched, **0 disclosed**, 0 skipped, 41 Benchmark/Example/Fuzz declarations excluded under the
standard Phase-4D deferral. The whole suite runs in ~24 s, so it needs no `$longTimeouts` floor.

### The leaf, and the root behind it

A field-by-field probe — walk both structs with `reflect`, `DeepEqual` each field, recurse, print the
first path that separates — named **`All.MapP`** immediately, and named it in the informative way:

```
DIFF All.MapP: DeepEqual FALSE but every child compares equal (kind map)
```

`All.MapP` is `map[string]*Small{"19": {Tag:"tag19"}, "20": nil}`. Reduced, the divergence is four
lines and does not involve `encoding/json` at all — it needs only that ONE side was written through
`reflect` and the other declared as a literal:

| | |
|:--|:--|
| two literals | equal ✓ |
| two decoded | equal ✓ |
| decoded vs literal | **false** ✗ |

**Root: `deepValueEqual`'s map arm built each entry's Value from the STORED OBJECT, not from the
map's declared element type.** Every other read in the bridge is slot-derived — `Field`, `Index`,
`Elem`, `MapIndex`, `MapIter.Value` all type by the declaring slot — but the DeepEqual map arm walks
the backing `Dictionary` directly (it must: golib keeps a nil KEY in a side slot no iterator can see)
and used `makeReflectValue`. A slot physically holding C# `null` therefore came back as the **invalid
zero Value** instead of a valid nil pointer. The two sides spell nil differently and always have — a
literal's `nil` element is stored as raw null, while a reflective write stores the canonical
`ж<T>.NilBox` that `packInterfaceValue` needs so a typed nil survives being handed out as an
interface — so one side read valid and the other invalid, and the map compared unequal. The same
blindness made a nil element compare EQUAL to a **missing key** (both invalid), which is the more
alarming half and had no consumer.

The fix is `mapElemValue`: type each entry by `Elem()` of the map's own type, exactly as `MapIndex`
does. Two nil elements then meet at the kind's nil rule; a missing key still fails on `Contains`; a
nil element still separates from a present non-nil one. **13 lines in `reflect/deepequal_impl.cs`**,
no converter change, no golib change.

**Why the visible-difference hypothesis missed it.** The previous lane read the failure text, saw
`SliceP`'s nil element print differently, reduced exactly that shape, and measured it AGREEING — a
correct measurement of the wrong object. `SliceP` is a slice, and the slice arm has always used
`v1.Index(i)`, which is typed. Only the map arm was blind. The lesson is the one the entry above
already wrote down and this lane simply executed: when `%v` is too lossy, bisect with a probe rather
than reason from the rendering.

**And the signature is reusable.** *A container reporting unequal while every one of its elements
compares equal* is what a lost slot type looks like from outside, because a walk that re-boxes each
element through `Interface()` re-enters the dynamic path on both sides and cannot see the difference.
The identical discrepancy named this function's FUNC arm on 2026-07-31 (`compress/flate`'s
`TestWriterReset`, ten levels failing while every field matched). Two arms of one function, four
months apart, found by the same tell.

### What the bank proves

`encoding/json` is **not** the roster's biggest suite by verdict count — at 491 it is sixth, behind
`go/doc/comment` (10,059), `go/internal/gcimporter` (583), `crypto/rsa` (559), `go/types` (557) and
`internal/zstd` (536) — and the claim worth making is a different one: it is by some distance the
most reflection-dense suite on the roster. Nearly every row of it is a
reflection walk checked against Go's own answer: struct-tag naming, embedding under Go's full
dominance rules, `Marshaler`/`Unmarshaler`/`TextMarshaler` dispatch at depth, `MakeMap`/`MakeSlice`/
`SetMapIndex` writes into every container kind, cycle detection keyed on `UnsafePointer`, and
`DeepEqual` over the results. The reflect-closure arc alone rooted nine defects, seven of them
credited to `encoding/json` verdicts, and this lane's map-element root is the tenth and last.
Banking it means the managed `reflect` is no longer *assumed* adequate for the encoding packages —
it is differentially proven against the standard library's own hardest consumer of it.

Roster: **150 / 215 (69.8%)**, 17,264 matching verdicts, 76 disclosed — header recomputed from the
table itself.

### Carried forward, unchanged

Everything the entry above boarded rather than took is still open and still unrooted-by-choice: the
`handler(nil)` methodless-func conversion (converter emission), a methodless defined func type having
no managed identity, projected field ORDER putting every embed last, and the NaN-in-a-composite map
key. None of them is reachable from any measured consumer, and this arc found no new one behind the
map-element root — the census is honest at one root, not one root plus a remainder.

## ⛔ The `**T` OUT-PARAMETER class is CLOSED at the boundary — and `crypto/x509` is walled twice more behind it (2026-08-17, lane `claude/x509-cryptoapi`)

The entry above censused a second syscall class at thirteen wrappers and named its root: `ж<T>` →
`uintptr` answers 0 for a `ж<ж<T>>` whose held pointer is still null, so an out-parameter tells
Windows "no output wanted". **That reading is right and incomplete, and the missing half decides the
fix layer.** Measured directly against golib on current master:

| the box | `IsNilPointer` | `IsNull` | `(uintptr)` |
|:--|:--|:--|:--|
| `&p` before the call (held pointer null) | false | **true** | **0** |
| `&p` after anything fills it (held pointer non-null) | false | false | **a live MANAGED address** |

So the operator has **two** wrong answers here, and the second is far worse than the first: a real
address of a slot the collector reads as an OBJECT REFERENCE, handed to a kernel that writes eight
raw bytes into it. The board already suspected as much ("the 0 is accidentally the SAFER outcome");
this measures it. `EnsureStableAddress` does not even pin such a slot — `GCHandle` pins only
reference-free storage — so the address is transient as well as wrong.

### The layer, decided by measurement rather than by preference

**Nothing golib can return would serve the boundary.** The kernel's representation is eight raw
bytes; the managed one is an object reference; no single address is both. Reconciling them needs a
SYNC POINT — the moment the raw word becomes a pointer box — and that moment is "after the call
returns", which only the wrapper knows. The remedy is therefore per-wrapper, but the *mechanism* is
one shape for the whole class: a native cell local to the call, its address handed over, and a
publish through `ValueSlot` afterwards (never `Value`, whose nil guard value-peeks and would panic
on the very write that fills the slot in). A zero report publishes the nil pointer with no special
case, because `ж<T>`'s native constructor already treats address 0 as nil.

`ж.cs` is **unchanged**. The operator's answer is correct for the case it was written for, and the
contract is now pinned from the other side by two `GolibTests` cases
(`AddressOfAPointerToPointerIsNotAKernelWritableSlot`,
`PublishingANativeAddressThroughValueSlotIsTheBoundaryRemedy`) so a future lane cannot "fix" it into
the dangerous answer.

### Class disposition: 13 wrappers, 5 taken, 8 left for stated reasons

The census re-measures at **13**, and one attribution on it is wrong: `NetGetJoinInformation` and
`NetUserGetInfo` are in **`syscall`**, not `internal/syscall/windows` — so the split is 11 + 2, not
9 + 4. Taken (`syscall/windows/zsyscall_windows_ptrout_impl.cs`, five entries in
`manualConversionFuncs`):

| Wrapper | Out-param | Why taken |
|:--|:--|:--|
| `ConvertSidToStringSid` | `**uint16` | `SID.String()`; half of a round trip |
| `ConvertStringSidToSid` | `**SID` | `StringToSid`; `SID` is Go's EMPTY struct — an opaque handle nothing reads through — so a native box is not merely safe but exactly right |
| `NetGetJoinInformation` | `**uint16` | a THIRD DLL (netapi32) with a different free routine (`NetApiBufferFree`) — what makes the guard evidence for a CLASS rather than one advapi32 accident |
| `CertAddCertificateContextToStore` | `**CertContext` | crypto/x509's measured consumer |
| `CertGetCertificateChain` | `**CertChainContext` | crypto/x509's measured consumer |

Left, each for a reason rather than for lack of effort: **`DnsQuery` / `_DnsQuery`** (`**DNSRecord`)
— the pointee is a LINKED native chain whose converted record holds managed references, so
publishing the address alone would replace a silent nil with a fabricated-reference landmine; it
wants the whole-chain transcription `zsyscall_windows_addrinfo_impl.cs` gives `ADDRINFOW`, in a `net`
DNS arc. **`getQueuedCompletionStatus` / `GetQueuedCompletionStatus`** (`**Overlapped`) — an
OVERLAPPED's identity belongs to the netpoll arc's per-operation record, and a bare native box would
mint an identity that arc does not know. **`GetFullPathName`**, **`NetUserGetInfo`**, and
`internal/syscall/windows`' **`CreateEnvironmentBlock`** / **`NetUserGetLocalGroups`** — the same safe
shape, no corpus consumer, therefore no value-level proof available (Go's own `syscall.FullPath`
passes nil for `fname`, so even its one caller does not exercise it).

### The guard, and what failing-first actually printed

New behavioral output test **`PointerOutParameter`**: the four well-known SIDs round-tripped
string→`*SID`→string, a malformed SID that must still be REJECTED, a stability check, and
`NetGetJoinInformation` whose returned buffer is WALKED to its NUL rather than merely tested for nil
— reading through the published pointer is what proves the address is the one Windows wrote.

Proven failing-first by neutering the cell address. The output is worth recording because it is
**not a crash**:

```
StringToSid error: The parameter is incorrect.      (x4)
malformed SID rejected: true
stable: true false                                   <- silently wrong, and self-consistent
NetGetJoinInformation error: The parameter is incorrect.
exit=0
```

advapi32 REQUIRES its out-parameter (unlike `ppStoreContext`, which is documented OPTIONAL), so the
defect surfaces there as a plausible-looking `ERROR_INVALID_PARAMETER` rather than as silence — and
`stable: true false` is the quiet-wrong-answer shape this class's history keeps warning about: two
empty strings agreeing with each other and with nothing else.

### `crypto/x509` census: the suite cannot be measured, and the verifier is walled twice

**The pipeline does not reach a verdict.** `go2cs -tests -test-action all` converts every `_test.go`
file cleanly and the production assembly builds, but the test host fails to compile with **5 errors
in 3 roots** — far smaller than the board's older `CS0102 duplicate definition` row implies, and none
of them about CryptoAPI:

| Root | Errors | What it is |
|:--|--:|:--|
| A name minted by the TEST half collides with one the PRODUCTION half already emitted | CS0111 ×2 | `x509.go` and `x509_test.go` both blank-import `crypto/sha256` and `crypto/sha512`; each emits `initᴛᴛblankImportꓸcryptoꓸsha256` into the same partial class. General to any package whose tests repeat a production blank import |
| The same collision through the BLANK-IDENTIFIER counter | CS0102 ×1 | `pem_decrypt.cs` has `_ᴛ1ʗ` (a blank const in an iota block) and `oid_test.cs` re-mints `_ᴛ1ʗ` for `var _ encoding.BinaryMarshaler = OID{}` — the counter restarts for the test half |
| `hash_package` unresolved in the PRODUCTION files under the tests closure | CS0246 ×2 | the tests csproj sets `DisableTransitiveProjectReferences`, and `hash` reaches `x509.cs` / `pem_decrypt.cs` only TRANSITIVELY (via `crypto/sha256`); the production csproj does not reference it either and does not need to |

All three are `-tests` emission defects, all three are general, and together they are what stands
between `crypto/x509` and any operational number at all. Deliberately not taken here — they are a
test-pipeline arc, not a CryptoAPI one.

**So the verifier was measured directly instead**, with an offline probe: a self-signed ECDSA leaf,
`Verify` with `Roots == nil` (which on Windows routes through `systemVerify`). Go reaches
`UnknownAuthorityError` immediately. The converted program, **with this lane's fix**:

```
created der: true
parsed cn: go2cs.example
<hangs>
```

It no longer nil-dereferences at `(*storeCtx).Store` — that defect is gone — and `dotnet-stack`
names the new wall exactly:

```
[Native Frames]
syscall!go.syscall_package.Syscall9(...)
syscall!go.syscall_package.CertGetCertificateChain(...)
crypto.x509!go.crypto.x509_package.systemVerify(...)
```

Blocked INSIDE the kernel call, ~1.7 s of CPU across minutes of wall time. The mechanism is the
OTHER, established class: `CertChainPara` is handed to the kernel BY ADDRESS while holding
`RequestedUsage.Usage.UsageIdentifiers` as `ж<ж<byte>>` and `CacheResync` as `ж<Filetime>` — managed
references — and `systemVerify` writes `para.Size = 80`, the NATIVE size, into a much smaller managed
object. Every field past the first therefore reads from the wrong offset, `dwUrlRetrievalTimeout`
among them, which is a blocking network budget. (`(~storeCtx).Store`, passed as `additionalStore` in
the same call, is read at a wrong managed offset for the same reason.)

**The full remaining CryptoAPI wall**, so the next lane needs no census:

| Direction | Structs | Reference-bearing fields |
|:--|:--|:--|
| PASSED by address (kernel READS) | `CertChainPara`, `CertChainPolicyPara`, `SSLExtraCertChainPolicyPara`, `CertChainPolicyStatus` | `ж<ж<byte>> UsageIdentifiers`, `ж<Filetime> CacheResync`, `Pointer ExtraPolicyPara`, `ж<uint16> ServerName`, `Pointer ExtraPolicyStatus` |
| READ BACK through raw addresses (kernel WROTE) | `CertContext`, `CertChainContext`, `CertSimpleChain`, `CertChainElement`, `CertRevocationInfo` | `ж<byte> EncodedCert`, `ж<CertInfo>`, `ж<ж<CertSimpleChain>> Chains`, `ж<ж<CertChainContext>> LowerQualityChains`, `ж<ж<CertChainElement>> Elements`, `ж<CertContext>`, `ж<CertEnhKeyUsage>` ×2, … |

⚠ The read-back half cannot be answered the way `GetAddrInfoW` was. That hand-own transcribes the
whole chain into managed boxes and makes the free a NO-OP, which works because nothing native has to
survive the call. Here `CertVerifyCertificateChainPolicy` and `CertFreeCertificateChain` both need
the ORIGINAL native pointer back, so any remedy needs a DUAL identity — a managed view that still
remembers its native address. That is the arc, and it is not a small one.

### Adjacent, found while guarding: `(*[N]T)(unsafe.Pointer(p))[:]` over a NATIVE pointer is the fabricated-reference fork again

The guard's first draft read `NetGetJoinInformation`'s result with Go's classic pre-`unsafe.Slice`
idiom and died:

```
Fatal error. System.AccessViolationException
   at go.slice`1[UInt16]..ctor(UInt16[], IntPtr, IntPtr, IntPtr)
   at go.array`1[UInt16].Slice(Int32, Int32)
```

`array<T>.AliasPointer` windows a real backing array when the pointer HAS managed element storage,
and otherwise falls to `(ж<array<T>>)(uintptr)element` — a native-address box over `array<T>`, which
is itself reference-bearing. Dereferencing it fabricates a `T[]` reference out of address bytes.
**Live corpus sites over genuinely native pointers**: `net/windows/lookup_windows.cs` lines 395, 465
and 500 (the DNS answer walk — SRV `Target`, NS `Host`, TXT `StringArray`) and `reflect/type.cs:1887`
(`t.t.GCData`). The `syscall`/`internal/syscall/windows` uses are over MANAGED element pointers and
are fine. Note where the DNS three sit: directly behind the `DnsQuery` member this lane deferred, so
that arc owns two walls, not one.

### Cross-package effect on `crypto/tls`, recorded rather than measured

`TestQUICHandshakeError` — the one `crypto/tls` divergence rooted in this class — is **expected to
still fail**, because the nil it dies on is only the first of three defects on that path; the chain
para and the chain walk stand behind it. `TestVerifyHostname` is likewise still expected to fail on
`net.adapterAddresses`, which this lane does not touch. **The full tls census was deliberately NOT
re-run**: a sibling lane is live on shared paths, and re-measuring 184 tests to confirm two
predictions is not worth the contention. Owed at post-merge: one `crypto/tls` per-test census, whose
honest prediction is **176 of 184, unchanged**, with `TestQUICHandshakeError`'s failure having moved
from a nil-pointer panic to whatever the chain para produces.

### Gate verdicts

Converter `go test ./...` ok (207 s) · `GolibTests` **148/148** · `PointerOutParameter` PASS on all
four phases, proven failing-first · `syscall` package builds clean · solution integrity 622/622 ·
full CNR and the `go2cs-stdlib.slnx` windows build recorded with the commit.

Still builds-and-partly-runs for `crypto/x509`: no roster row, no proof page, no disclosures,
converted test sources NOT committed — the package cannot yet produce a verdict to commit.

## ✅ WINDOWS NAME RESOLUTION WORKS — `net.adapterAddresses` transcribes the adapter chain, and the wall the tls-endgame lane found was never a wrapper defect (2026-08-17, lane `claude/net-interfaces`)

The tls-endgame entry above ended by naming `net.adapterAddresses` as "what stands between the
corpus and any name resolution at all on Windows", and filed it under the syscall struct-passing
class with `GetAdaptersAddresses` as the censused wrapper. **The wall is down, and the filing was
wrong — that correction is the most transferable thing here.**

### The row is WITHDRAWN, not marked fixed

`internal/syscall/windows`'s `GetAdaptersAddresses` is handed a byte BUFFER and fills it, which is
exactly what a byte buffer is for. The wrapper is correct, it stays auto-converted, and hand-owning
it would have fixed nothing. The defect was entirely in the CALLER — `net`'s own
`adapterAddresses`, which walks the filled buffer AS the record:

```
for aa := (*windows.IpAdapterAddresses)(unsafe.Pointer(&b[0])); aa != nil; aa = aa.Next
```

`IpAdapterAddresses` is the corpus's most reference-dense converted struct (nine `ж<T>`, an
`array<byte>` `PhysicalAddress`, an `array<uint32>` `ZoneIndices`) where the native record has raw
pointers and inline storage, so golib rightly declines to alias the byte run as it, the reinterpret
falls to a native-address box, and the loop's OWN nil test fabricates a managed reference out of
adapter bytes — `ACCESS_VIOLATION` in `ж<IpAdapterAddresses>.op_Equality`. So this belongs to the
**`readReparseLink` / `dir_windows_impl` fork** (a byte buffer the kernel filled, reinterpreted by
its consumer), not the mirror-the-wrapper one. The row is struck from the
`internal/syscall/windows` census above and from that package's own hand-own table, which had
asserted it was "repairable by the ORDINARY mirror remedy, because each receives the struct as a
typed pointer rather than through a byte reinterpret" — precisely backwards for this member.

**The lesson for the rows that remain: which struct is non-blittable does not tell you where the
repair goes. Who OWNS the memory it is read out of does.**

### The remedy, and three properties that generalize

`core/net/windows/interface_windows_impl.cs` holds the buffer in NATIVE memory that never escapes
the function, transcribes the whole chain into managed boxes, and frees it eagerly in a `finally`.

1. **The output is a chain OF CHAINS.** Each adapter record carries six nested linked lists
   (unicast, anycast, multicast, DNS-server, prefix, WINS-server and gateway — five sharing one
   native shape), and every consumer reaches THROUGH a record into them, so a top-level copy would
   have moved the fabrication one hop out rather than removing it.
2. **Whether a managed pointer needs a `ManagedPointerTokens` handoff is decided by the GO
   DECLARATION, not by the data.** `AddrinfoW.Addr` is an untyped `syscall.Pointer` the consumer
   casts by hand, so it needs a token; `SocketAddress.Sockaddr` is a TYPED `*syscall.RawSockaddrAny`
   that converts to a field carrying a managed box directly — no `unsafe.Pointer` round trip to
   survive, so no token, no weak table, no anchor. Reaching for that machinery here would have been
   ceremony, and the brief for this lane expected it.
3. **The transcription writes the image an EXISTING hand-own reads.** Consumers call `.Sockaddr()`,
   which is `syscall`'s hand-owned decode; it flattens the managed `RawSockaddrAny` back to its
   116-byte native image, so this is the same inverse-flattening `GetAcceptExSockaddrs` performs
   (Family at 0, `Addr.Data` 2..15, `Pad` 16..115).

The copy is FAITHFUL rather than minimal: `Length`, `AdapterName`, `DnsSuffix`, `Description`,
`Flags`, `ZoneIndices`, `FirstPrefix`, the link speeds and the WINS list are carried although no
consumer reads them today, because this record is the public shape behind `net.Interfaces` and a
declared field left nil is a SILENT divergence for the next consumer rather than a loud one.

### The payoff, measured

A probe run under Go and under the converted corpus, output byte-identical:

```
LookupIP(localhost) err: <nil> count>0: true
LookupHost(localhost) err: <nil> count>0: true
LookupPort(tcp,https): 443 <nil>
LookupHost(dns.google) err: <nil> count>0: true
Dial(dns.google:443): connected
```

The last two lines are the result: a LIVE DNS resolution through the server list `dnsReadConfig`
builds from `adapterAddresses`, then a TCP connection to the resolved address. It is deliberately
NOT promoted to a behavioral guard — it needs a network and an external host — so the suite keeps
the network-free `IpAdapterAddresses` guard instead and this stays a recorded probe.

`crypto/tls`'s `TestVerifyHostname` was NOT re-measured this lane; the probe answers the same
question more directly and an hour-plus census was not spent to restate it. Its verdict stays open.

### `net`'s FIRST pipeline census — it runs, and it does not bank

`go2cs -tests -test-action all -test-timeout 60m` over `net` (57 Windows-eligible `_test.go` files).
The suite CONVERTS and COMPILES, and the host RUNS — where before this arc anything reaching
`adapterAddresses` killed the process.

| Measure | Value |
|---|---|
| Go on this host | 474 verdicts, **695.6 s**, FAIL (network-dependent) |
| converted host | **25 verdicts**, then killed at the 61-minute safety net |
| matched / excluded / errors | 1 / 53 (benchmarks) / **461** |

Every one of the 461 "errors" is `Go="pass" C#=""` — a test the host never REACHED, not a conversion
failure. The 25 it did reach ran `TestAcceptError` → `TestClosingListener`, with `TestAddrList`,
`TestAddrListPartition`, `TestCancelAfterDial`, `TestCloseError` and the accept-path tests passing.
**So the root is a severe SLOWDOWN, not a correctness wall**, and `net` needs a poller/performance
arc before its census is even measurable. ⚠ This is exactly the mass-empty shape that reads like
total conversion failure; the 25 real verdicts are what prove it is not one.

### Gates

Converter `go test ./...` ok (216.8 s) · full CNR **byte-identical across 620 behavioral packages**,
nothing NOT MEASURED · seeded full reconvert **304/304**, and the overlay is **0 content differences
/ 0 file-set differences** with the marker gate at **61 marked files, 0 clobber violations** (18
`.cs.auto` emitted) — so the hand-own is stable under regeneration, including its L3 routing into
`net/windows/` and the `AllowUnsafeBlocks` flip its `[module: GoRequiresUnsafe]` causes · full
behavioral suite **593/593** across all four phases, 567 output comparisons, 0 failures (3,077.4 s) ·
solution integrity ok (622 projects). The guard is proven failing-first: with the auto body restored
it reports `exit code mismatch: C# -1073741819 vs Go 0`, the access violation itself.

⚠ Two process traps paid for here, both already in CLAUDE.md and both re-encountered: PowerShell's
`Start-Process -ArgumentList` ARRAY form does not quote a path containing a space, so the first
pipeline launch died with `Failed to access input file path "C:\Program"` — which reads exactly like
a missing GOROOT and is not (use the single-string form with embedded quotes); and an
`until ! powershell -Command "exit (…)"` wait-loop reported a still-running `go test` as finished,
the documented `exit $true` trap, caught only against a positive process count.

## ⛔ HARVEST — the pure-compute tail re-measured; nothing banks, `net/netip`'s SYNTAX wall falls, and three "reproduced verbatim" rows re-earn that phrase (2026-08-17, lane `claude/harvest`)

A ranked breadth pass over the unbanked pure-compute candidates, run on the r43c/r44a doctrine and
returning the least fashionable answer this board records: **six candidates measured, zero banked.**
That is worth writing down precisely because every previous breadth pass here banked something — the
cheap rows are spent, and what is left in the pure-compute tail is walls with names. The pass is
still worth its cost twice over: two converter defects that emit UNPARSEABLE C# are closed, and a
root the board has recommended twice as "the cheapest remaining pair" turns out to be spent.

### The enumeration, and what it excludes

**305** converted package directories under `src/core` carry a production `.csproj` (306 counting the
hand-written `golib`); **216** have a `func Test` in their Go 1.23.1 GOROOT sources; minus hand-owned
`testing` that is the roster header's **215**, and 215 − 150 banked = **65** unbanked. Of those, the following are excluded from a *pure-compute*
harvest rather than measured: the socket-walled `net` family (`net`, `net/http`, `net/http/{cgi,
cookiejar,httptest,httptrace,httputil,internal,pprof}`, `net/rpc`, `net/smtp`, `net/mail`,
`net/internal/socktest`, `log/syslog`) behind the poller-throughput arc; the runtime/OS internals
(`runtime`, `runtime/{debug,pprof,race,trace}`, `internal/runtime/*`, `internal/poll`,
`internal/syscall/*`, `internal/trace*`, `internal/coverage/cfile`); the lane-owned `crypto/tls`,
`crypto/x509` and `reflect`; the packages a ratified ruling already blocks (`math/big` 224/226 and
`net/http/internal` 9/10, both held by the *undisclosed* near-budget alloc counts; `log` 7/9 behind
the `runtime.Caller` arc; `os/user`, whose `TestGroupIds` fails in Go's own oracle); the
working-directory class (`go/build` 57/58, `internal/platform`, `internal/godebugs`); and the
architectural rows (`internal/unsafeheader` 0/6, `iter` 0/28 on the `newcoro`/`coroswitch` stubs,
`slices` 0/122 on element-type deduction for constrained slice generics).

### The six, in the order they were run

| Package | Verdicts | Board had | Measured 2026-08-17 |
|:--|:--:|:--|:--|
| `html` | 3 | 2 of 3 | **2 of 3, unmoved.** `TestUnescape` — the map-MISS producer of the `array<T>`-without-its-Go-length class. The `default!` zero-value emission (`claude/zero-value-arrays`) does NOT reach it: that fix covers declared zero values, and a map miss has no declaration to read a shape from. Still the converter+golib arc r57b priced |
| `internal/chacha8rand` | 4 | 3 of 4 | **3 of 4, unmoved.** `TestBlockGeneric` panics `index out of range [0] with length 0` at `chacha8_generic.cs:64` — and the producer is NOT a declaration either: it is the `unsafe.Pointer` array-SHAPE reinterpret (`[32]uint64` → `[16][4]uint32`) that hands back a length-0 outer `array<T>`. Same seam as `debug/pe`'s, with no kernel in it |
| `debug/pe` | 10 | 9 of 10 | **9 of 10, unmoved.** `TestReadCOFFSymbolAuxInfo` — the byte-level struct pun across surrogate layouts, exactly as recorded |
| `encoding/gob` | 106 | 99 of 106 | **100 of 106 — one row, from the reflect-bridge closure.** `TestSingletons` closed. The six that remain are four roots: the `array<T>` length class (`TestEndToEnd`, `TestIndirectSliceMapArray`), gob's own decoder IGNORE path (`TestBadData`, `TestIgnoreRecursiveType`), `reflect.Value.IsNil` on an INTERFACE asking the pointee (`TestNilPointerInsideInterface`), plus `TestIgnoreDepthLimit` infrastructure-erroring on the `reflect.ArrayOf`→`typelinks` stub |
| `net/netip` | 266 | 0 of 266, CS1002/CS1525 | **The SYNTAX wall is CLOSED — two converter defects, both fixed below — and three semantic roots stand behind it.** Still 0 of 266 |
| `internal/reflectlite` | 30 | 0 of 30, CS0715 | **0 of 30, and the root MOVED.** CS0715 is gone; two different diagnostics now: **CS0030** and **CS8130**, below |

### The two converter defects `net/netip` was the first thing in the corpus to reach

Both emit C# that does not PARSE, so nothing downstream of the compiler could have caught them, and
both are shapes the production corpus happens not to contain — which is why they survived to be
found by a Phase-4 measurement rather than by a build. Both are fixed, each with a failing-first
guard in `src/go2cs/mixedKeyedComposite_test.go`.

**1. Go's all-or-nothing keying rule is a STRUCT-literal rule.** An ARRAY or SLICE literal may MIX
positional and keyed elements — `[]byte{0xfe, 0x80, 15: 0x01}` is a SIXTEEN-byte value — and every
keyed path in `convCompositeLit` decided from `Elts[0]` alone (`compositeLitIsKeyed`'s comment stated
the wrong rule as its justification). A mixed literal therefore took the plain positional emission
while its keyed elements still rendered through the key/value arm, whose sparse form wants a target
ident that does not exist in an expression position:
`new byte[]{0xfe, 0x80, <nil>[15] = 0x01}` — CS1525. The fix normalizes the POSITIONAL elements to
the indices Go gives them (first element 0; after a keyed element at k, continue at k+1) so the
literal is all-keyed and the existing SparseArray machinery renders it — `new slice<byte>(16){[0] =
0xfe, [1] = 0x80, [15] = 0x01}`, which also recovers the length a wrong emission gets silently wrong.
An all-positional or already-all-keyed literal is untouched by construction, which is why the whole
corpus is byte-identical. A literal whose keys will not fold to constants is left exactly as it was:
an index the converter cannot compute is one it must not invent.

**2. Rooting a `global using` RHS was not idempotent.** `renderCSFullTypeName`'s default arm prefixes
the root namespace unconditionally, and a WHITE-BOX test conversion hands it names the test-alias
qualifiers (`testAliasShadowOperations`) have already rooted with an explicit `global::` — so
`export_test.go`'s `type Uint128 = uint128` emitted
`global using Uint128 = go.global::go.net.netip_package.uint128;`, CS7000 "unexpected use of an
aliased name". `global::` IS the root; prefixing it can only produce a name that is not one.

### What stands behind them — `net/netip`, three roots, all semantic

The package now COMPILES past the parser and stops on eight diagnostics in three families:

| Root | Sites | Note |
|:--|:--|:--|
| **An EXPORTED test declaration over an UNEXPORTED production type is emitted more accessible than the type** | CS0050 ×2, CS0052 ×3 in `export_test.cs` | `func MakeAddrDetail(…) addrDetail` and `var Z0 = …Handle[addrDetail]` are exported Go names, so they emit public; `addrDetail`/`uint128` are unexported, so they emit internal. Legal Go, illegal C#. A test assembly's `public` has no external consumer, so clamping an internal-test declaration's accessibility to its type's is the shape of the remedy |
| **A `global using` alias is assembly-scoped, not a namespace member** | CS0426 in `netip_test.cs` | `netip_package.AddrDetail` — the class `importOperations.go:894` names, whose CROSS-package two-hop form is already fixed; this is the SAME-package (white-box test) instance of it. `encoding/xml`'s recorded blocker reads identically (`ΔToken` does not exist in `xml_package`, 386 verdicts) and `html/template`'s CS0030 sits next door — whether one fix serves all three is a hypothesis this pass did NOT measure, but it is the cheapest one to test |
| **A generic test helper's constraint renders non-generically** | CS0305/CS0308 in `fuzz_test.cs` | `comparable<T>` used with no type argument, and `netipTypeCmp` used with one |

### `internal/reflectlite` — the recorded root is spent, and the two behind it are named

CS0715 (a generic test-local type's operators emitted into the static test partial) no longer
appears, which retires the board's "cheapest remaining pair" framing — the pair is now one package
(`runtime/debug`, 9, unmeasured this pass) plus two fresh roots here:

* **CS0030 — a Go conversion-to-interface written in CALL syntax takes a raw C# cast.**
  `pinUnexpMeth(EmbedWithUnexpMeth{})` emits `((pinUnexpMeth)new EmbedWithUnexpMeth(nil))`; the
  converted struct does not declare the interface (that is `go2cs-gen`'s adapter's job), so the cast
  is illegal. `T(x)` where T is an interface must route through `convertToInterfaceType`, exactly as
  an assignment to an interface-typed slot does.
* **CS8130 — a range over an `append(…)` result cannot infer its deconstruction.**
  `for i, tt := range append(assignableTests, implementsTests...)` emits
  `foreach (var (i, tt) in append(…))` and neither `i` nor `tt` has an inferable type.

### The ranked queue this pass hands on

1. **`encoding/xml` (386) and `net/netip` (266)** — the assembly-scoped-alias CS0426 root, measured
   in both and identical in shape. `html/template` (243) and `text/template` (52) sit behind a
   CS0030 that may or may not be the same family; measure `encoding/xml` first, since its
   diagnostic is the one that matches.
2. **`sync/atomic` (108)** — CS0841 (use of a local before its declaration); `debug/elf` (31) —
   CS8183 (implicitly-typed discard); `flag` (24) — CS1929; `crypto/ed25519` (9) — CS0030;
   `internal/concurrent` (20) — CS0426 `node<,>`. Five one-diagnostic build blocks, each unmeasured
   since scout batch 2 and each therefore a candidate for having MOVED, as reflectlite's just did.
3. **`encoding/gob` (106)** — three roots, of which `Value.IsNil` on an interface is a five-line
   bridge fix the board has carried since r39.
4. **`internal/reflectlite` (30) + `runtime/debug` (9)** — behind the two roots above.
5. **`crypto/internal/edwards25519` (55)** — the ratified Option A tuple-spec init-order fix is
   still unimplemented; the residual after it is one AllocsPerRun row.

Deliberately NOT re-measured: `unique` (4/19), `log/slog` (153/213), `go/doc` (24/85) — each has a
current census and a named owner, and r44a's lesson is that the yield is in what nobody has run.

### The measurement hazard this pass paid for

**MSB4166 "Child node exited prematurely" is NOT a build root.** `debug/pe` first measured as a hard
build failure with eleven MSB4166s and no CS diagnostic at all; re-run with
`MSBUILDDISABLENODEREUSE=1` it reached its real 9-of-10 in 45 s. The pipeline shells out to
`dotnet run`, whose MSBuild leaves node-reuse workers alive BETWEEN pipeline runs, and a reused node
that dies takes the next package's build with it. Set `MSBUILDDISABLENODEREUSE=1` for any queue that
runs several `-tests` conversions back to back — it is also the isolation flag a lane owes its
siblings.

### Gates

Converter `go test ./...` ok (274.3 s) · full CNR **byte-identical across all 621 behavioral
packages**, nothing NOT MEASURED, 2 advisory warnings (1,440 s) · **seeded full corpus reconvert**
(307 projects, 549 s) **1,665 emitted artifacts, 1,665 identical, 0 differing, 0 new** — the
zero-corpus-churn claim measured rather than argued, and self-validating on the hand-own marker gate
(a failed seeding would have emitted the hand-owns as plain `.cs` and they would have shown as
differing) · both guards proven failing-first (`<nil>[15] = 0x01` and
`go.global::go.net.netip_package.uint128` each reproduce with its fix neutered). No roster row, no
proof page, no disclosures, no converted test sources committed: six measurements, zero validations.

<!-- {% endraw %} — keep this the FINAL line: the board is append-only and every append must land INSIDE the raw guard, or Jekyll's Liquid chokes on quoted Go composite-literal syntax (this exact failure took the Pages build down at f37ba28ef). -->
