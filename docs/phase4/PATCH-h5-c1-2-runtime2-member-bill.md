# PATCH — the C1-2 `runtime2.cs` member bill at go1.24.13

**Status:** a RECORD until H5 consumes it. Ruled at COORD `f9c551a5c` on C1's sizing (`0df3d0991`),
then **re-ruled on the CORRECTED bill** at COORD `82de2fc7c` after C1's HOLD (`3ee0f07ff`); C2's two
rows come from `2a6938f4b`, i9's independent confirmation and the one corrected row from `0259e007f`,
and the `mWaitList` correction plus the C1-2b split from i9's build baseline `c2b26c50b` and COORD
`b3a32e52d` — which re-affirmed **six rows, cut now**.
Applier: `src/apply-h5-c1-2-member-bill.sh`. Guard: `src/go2cs/h5MemberBillGuard_test.go`.

## 1. Why this is a patch and not a commit

The same shape as C1-1, measured the same way. `g.syncGroup` is `ж<synctestGroup>`, and
`synctestGroup` is declared in Go's `synctest.go` — a **1.24 file with no build tag**, so it converts
everywhere at 1.24.13 and exists nowhere today:

| | on the corpus today | consequence |
|---|---|---|
| `src/core/runtime/synctest.cs` | **ABSENT** | `ж<synctestGroup>` names a type that does not exist |
| `runtime2.cs` `waitReason` block | **38 constants** | correct for 1.23.12, wrong for 1.24.13 |

So a landed edit breaks a corpus that is green. The applier's precondition **refuses a tree with no
`runtime/synctest.cs`** and says why; that refusal is arm 1 of its self-test.

Region-disjoint from C1-1 (which edits the usings and deletes `partial struct note` in the same file),
so the two commute. COORD's dispatch runs C1-1 first; nothing here depends on that.

## 2. The bill — 20 items, of which 14 are invisible to a build

```
  2   g fields               syncGroup, fipsIndicator
  14  constants RENUMBERED   +1 each, indices 24..37 -> 25..38
  6   constants ADDED        SyncWaitGroupWait at 24, Synctest* at 39..43
  6   waitReasonStrings entries
  1   isIdleInSynctest       accessor + 12-keyed table, materialising dense at 44
  0   m.mWaitList            omitted, reason recorded AT THE SITE  (see §2.3 -- the REASON
                             first given was measured on the pre-hop corpus and is wrong;
                             the omission stands and the four sites are C1-2b's)
  0   m's size-class padding omitted, reason recorded AT THE SITE
```

### ⚠ 2.1 The renumber, and why the first sizing missed it

`waitReasonSyncWaitGroupWait` is **inserted at index 24**, not appended, so the fourteen constants from
`waitReasonTraceReaderBlocked` up shift by one:

```
  waitReasonTraceReaderBlocked   24 -> 25    waitReasonStoppingTheWorld     31 -> 32
  waitReasonWaitForGCCycle       25 -> 26    waitReasonFlushProcCaches      32 -> 33
  waitReasonGCWorkerIdle         26 -> 27    waitReasonTraceGoroutineStatus 33 -> 34
  waitReasonGCWorkerActive       27 -> 28    waitReasonTraceProcStatus      34 -> 35
  waitReasonPreempted            28 -> 29    waitReasonPageTraceFlush       35 -> 36
  waitReasonDebugCall            29 -> 30    waitReasonCoroutine            36 -> 37
  waitReasonGCMarkTermination    30 -> 31    waitReasonGCWeakToStrongWait   37 -> 38
```

**Nothing about getting this wrong produces an error.** The corpus spells each constant as an explicit
`= N` literal, so a wrong `N` compiles. `waitReasonStrings` is keyed *symbolically*
(`[waitReasonCoroutine] = "coroutine"u8`), so it follows the constants wherever they go — the table
stays internally consistent while every reason from 24 up carries its **neighbour's** text. C2's hole
(§2.2) silently returns `"unknown wait reason"` for six; this silently returns the **wrong** reason for
fourteen. i9's rebuild falsifier — *a build naming a `waitReason` constant means incomplete* — fires on
the six additions and **cannot fire on the fourteen shifts at all**.

That is why the applier derives everything from Go's own `runtime2.go` and why the post-condition is a
numeric join by name rather than a build.

### 2.2 C2's two rows (`2a6938f4b`)

- **`waitReasonStrings` 38 → 44 keyed entries.** A constant without its row stringifies as
  `"unknown wait reason"`; `String(w)` guards on `w >= len(waitReasonStrings)` and returns that text.
- **The `isIdleInSynctest` table's shape** — 12 keyed `true`, but its LENGTH must read 44.

Both were confirmed at the code before being taken: `golib`'s `SparseArray<T>` indexer is
`get => m_items[index]`, a raw `Dictionary` lookup, and its enumerator yields `0..maxKey` with gaps as
`default`, so `.array()` materialises **max key + 1**.

⚠ **The idle table reads 44 only because its highest key IS the last constant** (`SynctestSelect`, 43).
That is a property of Go's table, not of anything enforcing a length — see §6 for the sibling where the
same mechanism silently truncates. The applier pins it: regress the top key and the post-condition goes
red naming the materialised length (arm 10).

### 2.3 The two omissions, recorded at the site

- **`m.mWaitList`.** At 1.24 `nextwaitm muintptr` becomes `mWaitList mWaitList`. Omitted by C1-2 —
  **but not for the reason first given, and the correction is the more useful half.**

  ⚠ The sizing said the corpus converts none of Go's lock implementation files, so nothing could name
  the field. **That reading was taken on the PRE-HOP corpus.** i9 measured the reconverted tree
  (`c2b26c50b`) and found the opposite: at 1.24.13 `goexperiment.spinbitmutex` is ON, the converter
  selects and emits `lock_spinbit.go`, and **four sites need the field** —
  `windows/lock_spinbit.cs:220`, `:227` (twice), `:233`. COORD's batch e (`b3a32e52d`): *a "0
  references" measured on the pre-hop corpus is a fact about the pre-hop corpus; the bill for a hop is
  sized on the RECONVERTED tree.*

  The omission still stands, and COORD ruled the four sites are **C1-2b's input rather than C1-2's
  failure**. The corpus runs Go's mutex on the managed lock core (`lock_managed_impl.cs`, a hand-own
  shared by every target), and at 1.23.12 the selected tristate file did not reach the build. The
  question is **which mechanism kept it out** and whether that mechanism reaches `lock_spinbit.go` —
  not whether to add a field. Adding `m.mWaitList` and its type so `lock_spinbit.cs` compiles beside
  the managed core would put two lock protocols in one runtime and paper over which of them runs.
  The note at the site says exactly this.
- **`m`'s size-class padding**, `_ [goexperiment.SpinbitMutexInt * 700 * (2 - goarch.PtrSize/4)]byte`.
  Not in the approved text and not a fidelity gap: on a 64-bit target `goarch.PtrSize` is 8, so the
  length is `1 * 700 * 0` = **zero** — a blank field of no bytes, whose purpose is to keep Go's
  `runtime.m` inside the 2048-byte size class so the low bits of a `muintptr` stay free for spinbit
  flags. C# struct layout is the CLR's and there is no size class to hit. *Recorded here as a
  measurement C1 owed and had not reported: the m-struct diff has two rows, not one.*

Both notes are part of the deliverable, not decoration: strip either and the post-condition fails
(arm 12). An undocumented omission is indistinguishable from an oversight to the next reader holding a
1.24.13 diff beside this file.

### 2.4 One label corrected

C1's sizing called `fipsIndicator` "fidelity only". **That is wrong and the correction is measured:**
`runtime1.go` at 1.24 both reads and writes `getg().fipsIndicator` (lines 732 and 737), so the
converted `runtime1.cs` will not compile without the field. It is load-bearing exactly like
`syncGroup`. The action is unchanged — both fields are added either way — but the reason a reader would
give for keeping it was wrong.

## 3. Nothing is typed

The applier takes `<goroot>/src/runtime/runtime2.go` (argument, `H5_GOROOT`, `GOROOT`, then
`go env GOROOT`) and extracts, per constant: **name**, **`iota` index**, **`waitReasonStrings` text**.
Every corpus constant is then set to Go's index **joined by name** — one rule that covers the fourteen
renumbers and leaves the other twenty-four alone, with no "shift these" list to get wrong — and the six
missing names are inserted after their predecessors. `waitReasonStrings` is rebuilt whole from the same
table, in Go's order.

Three properties make that safe to trust:

1. **Every extraction has a floor and dies under it.** A const block, strings table or idle table that
   reads under 10 / 10 / 5 entries **refuses**; it never compares.
2. **Two independent spellings are cross-checked.** Go states each string twice — the const block's
   trailing comment and the `waitReasonStrings` row — and a disagreement between the two readers
   refuses the run.
3. **The version discriminator is derived, not typed.** `isIdleInSynctest` does not exist before 1.24,
   so a 1.23 GOROOT refuses *naming that table* rather than applying a short bill. There is no list of
   the six new names anywhere in the applier.

⚠ **Why the floors exist, in C1's own words to the fleet (`3ee0f07ff` §3).** The first attempt at this
measurement extracted **zero** constants from both releases — an awk range that never opened, because
the Go const block is column-aligned and the pattern matched single spaces — and the prefix check then
compared **two empty files** and printed *"IDENTICAL — the six are APPENDED, no renumbering."* The
second attempt still read zero, because **`\t` in `grep -E` is not a tab**. Only a refusal added on the
second attempt stopped a third zero being reported as a result. **A comparison of two empty sets reports
agreement**, and "identical" is the most dangerous word an empty reading can produce, because unlike a
zero count it does not look like nothing.

## 4. Running it, and the decidable post-condition

```
  apply-h5-c1-2-member-bill.sh <scratch-core-dir> [<goroot>]   apply, then assert
  apply-h5-c1-2-member-bill.sh --verify <dir> [<goroot>]       assert only; changes nothing
  apply-h5-c1-2-member-bill.sh --self-test [<goroot>]          red-first, no clone touched
```

Exit **0** applied-and-verified (or already applied) / **1** a post-condition failed / **2** misuse or
refusal. ⚠ **A refusal is not a verdict**: the dispatch distinguishes 2 from 1 and prints
`NO VERDICT — the run REFUSED above; nothing was measured`. The first cut did not, and printed
`POST-CONDITION FAILED` over a run that had never read its inputs — which sends a reader to look at the
corpus for a defect the instrument never saw. Caught by arm 13.

The post-condition, all of it decidable statically:

- the corpus declares **exactly** as many constants as Go does — i9's exact equality, and it exists
  because of their correction in §5;
- every Go name is present with **Go's value**, joined by name, and **every disagreement is NAMED**;
- `waitReasonStrings` keys the same 44 names with the same 44 texts, covering indices 0..43 so the
  sparse table materialises dense;
- `ΔisIdleInSynctest` keys Go's twelve, and its **highest key is index 43** (the length is max key + 1);
- the accessor `isIdleInSynctest(this waitReason w)` exists;
- `fipsIndicator` and `syncGroup` are present **inside `struct g`**;
- both omission notes are present **inside `struct m`**.

The runtime half is i9's and is not claimed here: `String(w)` for each of the six returns Go's text,
`String(waitReasonCoroutine)` returns `"coroutine"` and not its neighbour's, `len(waitReasonStrings)`
reads 44, and the idle table indexed at 43 does not fault.

## 5. i9's corrected row (`0259e007f`), and what it bought

i9 confirmed the renumbering independently on their own tree — the same fourteen names, the same
`24→25 … 37→38` shifts, the same six new at 24 and 39..43 — and corrected one row of C1's report:
**the corpus declares 38 constants, not 37.** `waitReasonZero` is spelled
`= /* iota */ 0;`, with the comment **between** the equals sign and the value, and C1's pattern skipped
it; the same shape produced the "37 matched" figure and a phantom seventh missing row.

That correction is load-bearing twice over:

- it makes the correspondence **exact** — 38 corpus against 1.23.12's 38 — so the post-condition can be
  an **equality** rather than an inequality, which is what refuses a half-applied file;
- the applier's constant regex carries the optional `/* iota */` for exactly that reason.

⚠ And the same defect **recurred in this applier's own self-test**, four hours later: arm 7's count
pattern demanded `= <digits>` and read 43 where there are 44. It landed as a red arm on a correct apply
rather than as a published number, which is the only reason it is a footnote instead of an erratum.

## 6. ⚠ A finding NOT in this bill: the sibling table is TRUNCATED

Reported here rather than fixed, because it is outside the ruled bill and is a **converter** defect
rather than a hop item.

Go declares both tables with an explicit length: `var isWaitingForSuspendG = [len(waitReasonStrings)]bool{…}`.
The corpus emits `new golib.SparseArray<bool>{…}.array()` — **no length** — so the array materialises at
max key + 1. `ΔisWaitingForSuspendG`'s highest key is `waitReasonFlushProcCaches`, so:

| | Go | corpus |
|---|---|---|
| before the hop | 38 slots | **33** (top key 32) |
| after the hop | 44 slots | **34** (top key 33) |

`isWaitingForSuspendG(w)` therefore **throws `IndexOutOfRangeException`** where Go returns `false`, for
every `w` above the top key. It is reachable from converted code today — `proc.cs`
(`casGToWaitingForSuspendG`), `stack.cs` and `tracestatus.cs:139` all call it, the last for any waiting
goroutine while tracing — so this is **pre-existing, not created by this cut**; the hop widens the
faulting range from 5 values (33..37) to 10 (34..43), and the five new `Synctest*` reasons land inside
it.

The machinery to emit it correctly already exists and `golib` documents the case:
`array<T>(this IEnumerable<T> source, int length)` in `src/core/golib/array.cs` carries the comment
*"the SparseArray projection of an INDEX-KEYED literal whose highest key falls short of the declared
length"*. The converter does not pass a length when the declared length is a non-literal expression
such as `len(waitReasonStrings)`, which is the general form of the defect.

The new `ΔisIdleInSynctest` is emitted `.array()` to match the converter and its sibling, and reads 44
only because Go's twelfth key happens to be the last constant. **COORD to rule** whether the converter
fix, a `.array(44)` in the hand-own, or neither belongs in this hop.

## 7. Validation

`--self-test`, **15 arms clean**, run on linux/amd64 with `python3` and the box's own
`/usr/local/go1.24.7` GOROOT.

⚠ **The fixture is the clone's own `src/core/runtime/runtime2.cs`**, copied into a temp tree — real
data, not a synthetic const block written to satisfy the parser. If it is absent the suite reports
**NOT RUN** and refuses to substitute one; the Go guard fails on that string, because a skipped arm is
not a passing arm.

| Arm | Asserts |
|---|---|
| 1 | a PRE-HOP tree (no `synctest.cs`) is REFUSED, naming it |
| 2 | the UNPATCHED hand-own FAILS `--verify`, naming the renumber AND both additions |
| 3 | apply then verify is GREEN |
| 4 | the bill counted **off the diff**: 14 rewritten, 6 constants, 6 strings rows, 2 g fields |
| 5 | boundary values: 23 unmoved, 24 inserted, 24→25, 37→38, 39..43 new |
| 6 | CRLF preserved byte for byte (tr/wc, compared as INTEGERS) |
| 7 | re-apply is IDEMPOTENT — asserted on the counts, since the strings table is rebuilt whole |
| 8 | **one** constant left at its 1.23 value goes RED **naming it**; restore returns it to green |
| 9 | a missing `waitReasonStrings` row goes RED, and reports the density loss |
| 10 | a short idle table goes RED reporting the materialised length |
| 11 | a missing `g` field goes RED, looked for INSIDE `struct g` |
| 12 | a stripped omission note goes RED |
| 13 | an EMPTY Go extraction REFUSES and produces **no verdict** |
| 14 | a pre-1.24 GOROOT is REFUSED, by a derived discriminator |
| 15 | a probe-passing no-op (`/bin/echo`) is REFUSED as an interpreter |

Arms 13 and 14 build their Go fixtures by **mutating the real source** (emptying the const block;
deleting the 1.24 table), so neither is a hand-written stand-in either.

**The guard was made to fail before it was trusted** (floor item 13): arm 8's success line was renamed
in the applier, `go test -run TestH5MemberBillSelfTest` went red naming
`"ONE stale constant goes RED"`, and the restore was verified byte-identical by `sha256sum -c`.

### 7.1 i9's pre-cut baseline, and the prediction this cut is measured against

i9 took a build baseline on `rung1-scratch-postrung` (post-H5c, C1-1 applied, **pre**-C1-2) before the
cut landed (`c2b26c50b`): **100 errors, every one a bill row, 0 outside it.**

```
   86  g.syncGroup missing              6  waitReason constant undefined
    4  m.mWaitList missing              2  waitReason.isIdleInSynctest missing
    2  g.fipsIndicator missing
  ---
  100  outside the bill: 0        and 0 of the 100 concern the 14 renumbered constants
```

Two independent instruments — a C# compiler and a by-name join against `runtime2.go` — name the **same
six** constants, neither able to see the other's evidence. The renumber half is confirmed invisible to a
build from the outside rather than argued.

The prediction on record for after this cut is **100 → 4, all four in `lock_spinbit.cs` naming
`mWaitList`** (i9's branch (a); COORD ruled it the EXPECTED reading). Zero would mean the cut took a row
the bill does not list; any other number names a gap neither lane predicted.

**Not measured here, and not claimed:** this has never run against a real 1.24.13 emission, and nothing
in this repository builds C#. `go test -count=1 ./...` in `src/go2cs` is green **except**
`TestSafePushSelfTest`, which fails identically on a clean tree at `2c88415716` with the change stashed:
this container's clone is **shallow**, and that suite's hermetic origin cannot be seeded
(`! [remote rejected] … (shallow update not allowed)`). Environmental, pre-existing, unrelated.
