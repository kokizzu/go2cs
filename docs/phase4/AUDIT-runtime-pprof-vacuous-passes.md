# AUDIT — `runtime/pprof`, the 120 MATCHED rows asked whether they could have failed

**Point-in-time record.** Audited 2026-09-07 by SUB-Q73 on the coordinator box (i7 class), at master
`6c861d366`. Amended with dated blocks, never rewritten, never executed from.

**Method is READ, not measurement.** Nothing was run: no sweep, no `-tests` pipeline, no build, no
converter invocation. Every statement below is derived from (a) Go 1.23.12's own test sources under
the pinned GOROOT — bare `go version` compared, not merely printed, and it reads
`go version go1.23.12 windows/amd64` — and (b) the committed converted corpus at `6c861d366`. The
row-level verdicts are taken from [`CENSUS-runtime-pprof-doors.md`](CENSUS-runtime-pprof-doors.md)
(SUB-Q43, 2026-09-04) and are not re-measured. `runtime/pprof` was **not re-run**: a gated
(`-test-filter`) run rewrites the package's comparison record with nothing marking it gated, so the
existing record is left alone.

This audit answers the item the census explicitly declined (§7): *"a bank must ask of each matched
row whether the converted side could have failed it, the bar `internal/abi`'s `TestFuncPC` set."*

---

## 1. The bar, taken from `internal/abi`'s `TestFuncPC`

`TestFuncPC` compares `abi.FuncPCABI0(FuncPCTestFn)` against `abi.FuncPCTestFnAddr`, a value Go
writes from `abi_test.s`:

```
GLOBL internal∕abi·FuncPCTestFnAddr(SB), NOPTR, $PTRSIZE
DATA  internal∕abi·FuncPCTestFnAddr(SB)/PTRSIZE, $internal∕abi·FuncPCTestFn(SB)
```

The pipeline never converts assembly, so on the managed side `FuncPCTestFnAddr` is *declared, read
and never written*, and `FuncPCTestFn` is a bodyless partial. Until 2026-09-03 `FuncPCABI0` was
`return default`. **Both arms were 0, the comparison held, and the row banked as a match** — while Go
passed the same test for the opposite reason, a real address on both arms. `funcpc_impl.cs`'s own
header names the failure mode: *"A silent wrong answer that agrees with the oracle by coincidence."*

Three things carry over from it, and they are the bar this audit applies:

1. **The question is not "did the verdicts agree" but "is there a state the converted implementation
   could have been in that makes this assertion fire?"** If no such state exists, the agreement is a
   coincidence of degeneracy, not evidence.
2. **Absence degenerates comparisons silently.** A never-written output arm, an empty collection, a
   loop whose bound is computed from an empty map — each turns an assertion into a no-op while the
   surrounding test reads normally.
3. **The remedy is not to delete the row.** The tree's own answer to `TestFuncPC` was to replace the
   silent zero with a *loud refusal*, converting a vacuous pass into an honest disclosed fail. A
   vacuous pass found here is a candidate for the same treatment.

### 1.1 The classes used

| class | meaning |
|:--|:--|
| **REAL** | the converted implementation supplies the value the assertion inspects, and a defect in that implementation changes the verdict. The row discriminates. |
| **WEAK** | the assertion *can* fire, but only on a residual or incidental property — not the property the test was written for — or only under conditions this host never produces. |
| **VACUOUS** | no state the converted implementation could produce makes the assertion fire. The pass carries zero information about the port. |

A parent row is classified by its strongest child, because a parent fails when any subtest fails.

---

## 2. Deriving the 14 declarations — and an arithmetic check that closes twice

The census reports *"44 top-level tests, of which 14 carry matched rows"* but does not name them.
They are derived here by subtraction and then checked against the row totals.

The 44 declarations are every `^func Test` in the windows-eligible test files —
`label_test.go`, `mprof_test.go`, `pprof_test.go`, `proto_test.go`, `protomem_test.go`,
`runtime_test.go`. (`rusage_test.go` is `//go:build unix`; `vminfo_darwin_test.go` is
darwin-by-filename.) The count is exactly 44, agreeing with the census.

Subtracting the 30 declarations the census bills elsewhere — 7 withdrawn `host-fatal` (§3), 1
capability-gated (`TestFakeMapping`), 13 CPU-profile (§5.1), 3 memory-profile (§5.2), 2 inlining
(§5.4), 2 proto-fixture (§5.5), 1 label (§5.6), and `TestBlockProfileBias` (§5.3) — leaves 13
declarations wholly in the matched column, plus `TestMutexBlockFullAggregation`, whose parent
diverges while two subtests match. **14.**

**Both totals close on the derived per-declaration row counts**, which is what makes the table below
a derivation rather than an assignment:

- Every declaration's rows summed over all 44 = **183**, the census's Go-side row count.
- The 14 declarations' matched rows summed = **120**, the census's matched count.

The two skip/skip rows the census names (*"118 `pass`/`pass` + 2 `skip`/`skip`"*) fall out of the
same arithmetic as `TestCPUProfileMultithreadMagnitude` and `TestMapping` — the only two
declarations that reach a `t.Skip` before any subtest, on both sides.

---

## 3. The per-declaration table

| # | declaration | matched rows | class | what the assertion could have caught — and whether it could fire |
|--:|:--|--:|:--|:--|
| 1 | `TestGoroutineProfileConcurrency` | **104** | **MIXED** | 2 REAL + 101 WEAK + 1 parent. Split in §4 — this is the whole story of the matched count. |
| 2 | `TestProcSelfMaps` | 3 | **REAL** | Pure parser over string fixtures: `parseProcSelfMaps` (converted body, `proto.cs:673`) against two golden tables, `t.Errorf` on any byte difference. No runtime, no OS, no profiler. A converter defect in byte-slice scanning, `strings.Cut`, or the hex formatting fires immediately. The strongest row in the matched set. |
| 3 | `TestMutexBlockFullAggregation` (`/mutex`, `/block`) | 2 | **VACUOUS** | **A subtest that structurally cannot fail.** `assertNoDuplicates` is defined *before* the `t.Run`s and closes over the OUTER `t`; the inner `func(t *testing.T)` shadows it but the closure already captured the parent. Every `t.Errorf` — including `did not see any samples in %s profile` — is attributed to the parent, which is why the census records the parent as a divergence while both children pass. No implementation, converted or otherwise, can make these two rows fail. |
| 4 | `TestContextLabels` | 1 | **REAL** | Label-map semantics end to end: `WithLabels`/`Label`/`labelsSorted`, key replacement, duplicate-key last-wins, and `reflect.DeepEqual` over a `[]label`. Five distinct assertions, all reading values the converted `label.cs` computes. |
| 5 | `TestLabelMapStringer` | 1 | **REAL** | Table test of `labelMap.String()` across empty / single / five-entry-with-embedded-newline, asserting sorted key order and quoting. Converted body; a map-iteration or `strconv` defect fires. |
| 6 | `TestSetGoroutineLabels` | 1 | **REAL** | Exercises the hand-owned per-goroutine storage (`proflabel_impl.cs` → `golib.Goroutine.SetProfileLabels`) **and label inheritance across a `go` statement**. Cannot pass degenerately: the first assertion wants `{}` (which a permanently-nil `runtime_getProfLabel` would satisfy) but the second wants `{"key":"value"}`, which it would not. Three parent/child pairs, six assertions. |
| 7 | `TestDo` | 1 | **REAL** | Same storage, plus `Do`'s scoping discipline: labels present inside the body, inherited by a goroutine spawned inside it, and **restored to empty after `Do` returns**. Same non-degeneracy argument. |
| 8 | `TestEmptyCallStack` | 1 | **REAL** | The bar's own mechanism, on the right side of it. `p.Add("foo", 47674)` drives `runtime.Callers` to return 0, so `pprof.cs:418` substitutes `abi.FuncPCABIInternal(lostProfileEvent)`; the test then requires the debug=1 render to *contain the string* `lostProfileEvent`. Under the pre-2026-09-03 `return default` FuncPC this row would have **failed** — it needs the synthetic-PC registry to mint a token and `syntheticFrameRecord` to symbolize it back. Also asserts the `"%s profile: total 1\n"` prefix through `tabwriter`. |
| 9 | `TestConvertCPUProfileNoSamples` | 1 | **REAL** (narrow) | Drives production `newProfileBuilder` → `addCPUData` → `build` → `profile.Parse`, then `checkProfile` asserts `Period == 2_000_000`, `PeriodType{cpu,nanoseconds}`, `SampleType[{samples,count},{cpu,nanoseconds}]`, empty samples. A live protobuf encode/decode round trip. Narrow deliberately: its non-empty sibling `TestConvertCPUProfile` is a measured divergence (`"Mapping": null`, census §5.5), so this row covers the header half of a path whose sample half is known broken. |
| 10 | `TestEmptyStack` | 1 | **REAL** (narrow) | Same production path with 10 empty-stack samples; the only assertion is `err == nil`. A genuine error/throw canary on live converted code (issue 37967 was a panic), but it inspects nothing beyond "did not error". |
| 11 | `TestGoroutineProfileCoro` | 1 | **WEAK** | **Zero assertions in the body.** A background goroutine runs `iter.Pull2` coroutines forever while the test takes one `WriteTo(io.Discard, 1)`; the comment says outright *"We don't care about the output for this bug."* It can only fail by crashing. `iter` is a banked row with real `newcoro`/`coroswitch` bodies (`iter_impl.cs`), so the path is live — but the defect it hunts (#69998, the profiler reading a coro goroutine's runtime state) has no analogue: `pprof_goroutineProfileWithLabels` records a start function and nothing else. |
| 12 | `TestGoroutineProfileIssue74090` | 1 | **WEAK** | Same shape: zero assertions, 10 × 10 000 finalized objects and a concurrent profile write, its own comment saying *"if this test crashes at all, it's a clear signal."* The finalizer path is genuinely live (see row 1's finalizer subtests), so it is a real stress; the system↔user goroutine transition race it targets is not reproducible in a model that has no such state machine. |
| 13 | `TestCPUProfileMultithreadMagnitude` | 1 (skip) | **VACUOUS** | First statement is `if runtime.GOOS != "linux" { t.Skip(...) }`. Both sides skip, for the same reason, before one line of pprof code runs. Honestly vacuous — not a false green, but zero information. |
| 14 | `TestMapping` | 1 (skip) | **VACUOUS** | `testenv.MustHaveGoRun` then `testenv.MustHaveCGO`. The converted `HasCGO()` shells out to `go env CGO_ENABLED` exactly as Go's does, and the corpus's emission state is `CGO_ENABLED=0`, so both sides skip before any subtest. Zero pprof code runs. |

**Declaration roll-up:** REAL 8, MIXED 1, WEAK 2, VACUOUS 3 — **14**.

---

## 4. The 104-row declaration, split — this is where the matched count comes from

`TestGoroutineProfileConcurrency` is 1 parent + 3 named subtests + 100 repetitions of
`goroutine launches`.

| subtest | rows | class | reasoning |
|:--|--:|:--|:--|
| *(parent)* | 1 | REAL (derived) | Fails if any child fails; inherits the finalizer subtests' power. |
| `overlapping profile requests` | 1 | **WEAK** | The body contains **no `t.Errorf` at all**. Its only branch — `profilerCalls(prof) >= 2`, counting `"\truntime/pprof.runtime_goroutineProfileWithLabels+"` — is **structurally unreachable**: `pprof_impl.cs` records one frame per goroutine, its *start function*, never a currently-executing frame, so a live call to `runtime_goroutineProfileWithLabels` can never appear in the render. `cancel()` is therefore never called and the subtest always runs its full 10-second timeout. Residual value is real but incidental: two goroutines hammering `WriteTo(debug=1)` for 10 s is a crash/deadlock stress. |
| `finalizer not present` | 1 | **REAL** | Requires `runtime.runfinq` to be **absent** while no finalizer runs. Discriminating: `mfinal.cs:667` registers the finalizer thread via `Goroutine.EnterSystem(s_runfinq)`, and `CountsAsUser => !IsSystem \|\| m_userWork != 0` excludes it when idle. Had the runner registered as an ordinary goroutine, this row fails. |
| `finalizer present` | 1 | **REAL** | The mirror arm, and the strongest single row in the package. It parks a finalizer inside its body and requires `runtime.runfinq` to **appear**. That needs four converted mechanisms to be simultaneously right: the finalizer runner registers with `Entry = runfinq` (`mfinal.cs:582` mints the `MethodBase`, `:667` registers it), `EnterUserWork()` brackets the user body (`mfinal.cs:686`), `ProfileSnapshot()` admits it through `CountsAsUser`, and `GoSyntheticPC.Of` + `syntheticFrameRecord` symbolize the token back to the literal string `runtime.runfinq`. Any one of them wrong and the row fails. |
| `goroutine launches` | **100** | **WEAK** | See below. |

### 4.1 Why the 100 `goroutine launches` rows cannot fire their own assertion

The subtest's purpose is one ordering property:

```go
counts := make(map[string]int)
for _, s := range p.Sample {
    label := s.Label[t.Name()+"-loop-i"]
    if len(label) > 0 { counts[label[0]]++ }
}
for j, max := 0, len(counts)-1; j <= max; j++ { ... t.Errorf(...) }
```

**The converted goroutine profile carries no labels, by a deliberate and documented decision.**
`pprof_impl.cs:110` opens with `_ = labels;` under the comment *"`labels` is deliberately never
written"*, and the block beneath it records the measurement that put it there: a `labelMap` address
minted through `unsafe.Pointer.FromPinnedBox` went stale across a collection (`len == 1885431144`),
killing the host with `OutOfMemoryException` inside `printCountProfile`. The consumer side confirms
it end to end — `writeRuntimeProfile` allocates `labels = new slice<unsafe.Pointer>(n+10)` and hands
it to the fetch that ignores it; `runtimeProfile.Label(i)` is
`(ж<labelMap>)(uintptr)(p.labels[i])`, i.e. nil; and `printCountProfile`'s proto branch emits a
`pbLabel` only `if (p.Label(idx) != nil)`.

So `s.Label` is empty for every sample ⟹ `counts` stays empty ⟹ `max = len(counts)-1 = -1` ⟹
`for j := 0; j <= -1` **never executes**. The ordering assertion the subtest exists for is dead on
arrival, 100 times.

What survives is one live assertion, `t.Errorf("error parsing protobuf profile: %v", err)` — the
converted `profileBuilder` output must be parseable, checked three times per run. That is genuine
but incidental, and it is **one measurement repeated 100 times**, not 100 measurements: the 100
repetitions exist to shake data races, which is meaningful under `-race` and is not what this host
runs.

**A bank quoting 120 matched rows is quoting 100 repetitions of a smoke test as 100 validations.**

---

## 5. Roll-up by row

| class | rows | share of the 120 |
|:--|--:|--:|
| **REAL** | **13** | 10.8% |
| **WEAK** | 103 | 85.8% |
| **VACUOUS** | 4 | 3.3% |

REAL = `TestProcSelfMaps` 3, `TestContextLabels` 1, `TestLabelMapStringer` 1,
`TestSetGoroutineLabels` 1, `TestDo` 1, `TestEmptyCallStack` 1, `TestConvertCPUProfileNoSamples` 1,
`TestEmptyStack` 1, and `TestGoroutineProfileConcurrency`'s parent + two finalizer subtests 3.

WEAK = `goroutine launches` 100, `overlapping profile requests` 1, `TestGoroutineProfileCoro` 1,
`TestGoroutineProfileIssue74090` 1.

VACUOUS = `TestMutexBlockFullAggregation` `/mutex` + `/block` 2,
`TestCPUProfileMultithreadMagnitude` 1, `TestMapping` 1.

13 + 103 + 4 = **120**.

### 5.1 The reading a bank needs

The census already warned that the matched count is dominated by one declaration. This audit
sharpens it in a direction that matters more: **the concentration is not merely in one declaration,
it is in one declaration's dead assertion.** Of 120 matched rows, 13 discriminate. By declaration, 9
of 14 contain at least one assertion that could have fired.

That is not an argument that the package validates nothing — the 13 REAL rows are substantive, and
`finalizer present`, `TestEmptyCallStack` and `TestProcSelfMaps` each exercise machinery that would
be genuinely hard to get right by accident. It is an argument that **120 is not the number to quote**,
and that the honest headline for this package is closer to *"13 real matches, 103 repetitions of a
smoke test, 4 rows that cannot fail"*.

### 5.2 The two rows worth a remedy, in the `TestFuncPC` shape

`TestFuncPC`'s fix was not deletion — it was making the silent zero loud. Two candidates here take
the same shape:

- **The 100 `goroutine launches` rows retire the day the goroutine profile carries labels.**
  `pprof_impl.cs`'s own closing paragraph already says filling `labels[i]` from `entry.Labels` is a
  one-line change once a label pointer survives a collection. On that day these 100 rows become
  REAL — the ordering assertion starts firing — and the same day `TestGoroutineCounts` (census §5.6)
  and the withdrawn `TestGoroutineProfileLabelRace` (census §3) become reachable. **One fix moves
  three census buckets at once**, which is a sizing datum the doors census did not have.
- **`TestMutexBlockFullAggregation`'s two subtests cannot be remedied at all** — the vacuity is in
  Go's own closure capture, identical on both sides. They should be *named* in a bank rather than
  fixed, so nobody later reads them as evidence the mutex/block profile aggregates correctly. It
  does not; the parent says so.

---

## 6. What this audit could NOT settle, and why

Five items. Each needs a run, and no run was permitted or performed.

1. **`TestMapping` — which gate takes the skip on the converted side.** `MustHaveGoRun` precedes
   `MustHaveCGO`, and the converted `HasCGO()` also returns false when `GoTool()` errors, without
   consulting the toolchain. Both paths yield a skip and both mean zero pprof code runs, so the
   VACUOUS classification is unaffected — but *reason-parity with Go's skip is asserted, not
   measured*.
2. **`TestGoroutineProfileCoro` — whether any coroutine goroutine is actually in the registry when
   the single profile is taken.** The background goroutine is live and `iter.Pull2` is implemented,
   but whether `ProfileSnapshot()` observes a coro goroutine at that instant is a scheduling
   question. If it never does, the row degrades from WEAK to VACUOUS. Settling it needs an
   instrument, not a source read.
3. **The residual strength of the 100 `goroutine launches` rows.** That the ordering assertion is
   dead is *proved* from source. Whether the surviving `profile.Parse` check has ever been at risk —
   i.e. whether a plausible encoder defect would surface here rather than at the already-failing
   `TestConvertCPUProfile` — is a judgement. I classified WEAK; an argument for VACUOUS is available
   and I do not think it is settled either way.
4. **How much of `addCPUData`'s branch space rows 9 and 10 reach.** Both drive real production code
   with hand-written `[]uint64` fixtures. "Narrow" is stated from reading the fixtures (3 words and
   6 words respectively), not from coverage.
5. **Reproducibility.** The census is a single run. Nothing here re-runs it, so every "pass" in the
   120 is a pass measured *once*. A WEAK row whose only failure mode is a crash or a deadlock is
   exactly the shape that is intermittent, and `overlapping profile requests`,
   `TestGoroutineProfileCoro` and `TestGoroutineProfileIssue74090` are all in that shape. A bank
   owes a second ungated run before treating those three as stable.

**Unsettled: 2 declarations** (`TestMapping`'s skip reason; `TestGoroutineProfileCoro`'s liveness),
plus three cross-cutting limits (3–5) that qualify classifications without changing them.

---

## 7. One incidental finding, routed rather than fixed

`src/core/golib/runtime/Goroutine.cs:199–200` states, in the remarks on `IsSystem`:

> *The finalizer-goroutine nuance (runfinq counts as user while it runs a finalizer) is not
> modelled: the finalizer runner is a plain thread here, not a registered goroutine.*

That is **stale**. `src/core/runtime/mfinal.cs:667` registers it (`Goroutine.EnterSystem(s_runfinq)`)
and `:686` brackets the user body (`Goroutine.EnterUserWork()`), which is precisely the nuance the
comment says is unmodelled — and the `CountsAsUser` property ten lines above the stale text
(`Goroutine.cs:189`) is the mechanism that implements it. No classification in this audit depends on the comment, and no
behaviour is wrong; a doc line contradicts the code beside it. Left for whoever next touches that
file, per *a lesson lands the day it is learned* — this is a record, not a cut.

---

## 8. What this audit does NOT claim

- **It is not a bank, and it does not change the bank verdict.** The census's two structural
  blockers stand untouched: 2 `infrastructure-error` rows, and 10 Go-only rows whose withdrawal root
  can never exist (`TestTryAdd`'s C# status is `skip`, while a withdrawal root requires
  `csResults[name] == "fail"`). This audit adds a third reason to be careful with the row, not a
  path around the first two.
- **It re-measured nothing.** All row verdicts are the census's. Where this audit disagrees with a
  reading it says so explicitly; it does not disagree with any measurement.
- **It does not price the label fix.** §5.2 observes that one change moves three buckets; sizing it
  belongs to whoever owns the pointer-provenance and pin-lifetime arc that `pprof_impl.cs` routes it
  to.
