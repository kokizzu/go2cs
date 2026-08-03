# Cleanup backlog — idle-point refactoring list

> **Low priority by design.** Items parked here are deliberate deferrals: real improvements that
> did not belong in the change that discovered them (charter §2 forbids folding cleanup into a
> fix's blast radius, and the merge that notices a vestigial arm is the wrong place to prune it).
> Pick items up at idle points or as a dedicated cleanup wave; every item still takes the full
> gate for its change class — parking here lowers priority, never rigor. Started 2026-08-02 from
> the r35/r36 waves' discoveries (user-requested).

## Converter / golib — semantics-adjacent (gate: full suite + corpus, some need the sweep)

1. ~~**Unify pointer-PARAMETER derefs on `DerefOrNull`**~~ — **DONE 2026-08-02 (r37b-paramderef).**
   Every direct-ж pointer entry alias, RECEIVER and PARAMETER alike, plus the pointer-reassignment
   re-alias, now takes `DerefOrNull()` unconditionally. Measured footprint: 457 corpus files, 2,551
   lines, 2,555 accessor sites, 100% one shape (`.Value`→ 2,079, `.DerefOrNil()`→ 413,
   `.ValueSlot`→ 59). Guarded by `NilPointerParamMethods` (neuter-proven: 42 diverging output
   lines with the eager arm restored). See *A pointer PARAMETER is nil-deferring for exactly the
   reason a receiver is* in ConversionStrategies-Reference.md.
2. ~~**Prune the vestigial receiver arms of the nil-safe analysis**~~ — **DONE 2026-08-02**, with
   item 1: `isDirectBoxReceiverIdent`, `collectNilSafePtrParams`, `reassignedBeforeDerefParamName`,
   `statementMentionsDerefdPointerParam`, `exprUsesParamWithoutDeref`, the package-wide
   `collectNilArgPtrParams` pre-pass and both visitor-state fields are deleted (−382 net lines).
3. ~~**`DerefOrNil` → null-ref semantics in the body**~~ — **DISSOLVED 2026-08-02**, exactly as
   predicted: no emission site selects the accessor any more, so the silent-`default(T)` hole is
   closed by construction. The golib method itself is retained (public surface, covered by
   `GolibTests.PointerNilPredicateTests`) but is no longer reachable from converted code.
4. **Remove GoFunc's 17 now-idempotent `panic.CaptureThrowSite(ex)` calls** — subsumed by
   `TryAsPanic`'s adoption-point snapshot (r36-time-tail). ⚠ PAIRED with the
   `GenGoFuncRefInstances` template repair: the template has drifted from the live 16-rung ladder
   (still emits bare `catch (PanicException ex)`), so regeneration without the repair silently
   reverts panic behavior. Do both together or neither.
5. **`channel.cs` beautification — UNBLOCKED.** The dead-code campaign fenced it "until the
   channels arc"; ground-truthing showed wave3 landed 2026-07-24/25, so the fence guarded nothing.
   Ordinary delicate-shared-machinery rules apply.

## Performance (found while validating, deliberately not disclosed as divergences)

6. ~~**The 136-byte `for range` enumerator allocation over `slice<T>`**~~ — **DONE (r37-time-os-fin,
   2026-08-02).** `slice<T>.GetEnumerator()` returned `IEnumerator<(nint, T)>` from an iterator
   method, so `foreach` could not bind it by pattern and every ranged loop paid a state machine plus
   the inner `SliceEnumerator` class — exactly 136 B/loop, measured. It now returns the nested
   `Enumerator` STRUCT (the interface members stay, explicit, for LINQ/interface consumers), and
   go2cs-gen's `ISliceTypeTemplate` forwards the struct so named slice types get it too. Guarded at
   zero bytes by `GolibTests/SliceRangeAllocationTests`.
   **Successor item:** `array<T>.GetEnumerator()` is the identical iterator-method shape and was
   deliberately left alone — Go's `range` over an array value ranges a COPY, so the eager-vs-lazy
   capture point is a semantic question there, not a mechanical one. Wants its own measured change.
7. ~~**`IByteSeq<T>` interface-boxing**~~ — **DONE (r38-ibyteseq, 2026-08-03).** The constraint is now
   self-referential (`IByteSeq<TSelf, T> : IByteSeq<T> where TSelf : IByteSeq<TSelf, T>`, emitted as
   `where bytes : IByteSeq<bytes, byte>`), so the sub-slice indexer returns the CONCRETE type and
   `s[a:b]` no longer boxes; `len` takes the constrained type parameter instead of the interface; and
   `[]byte(s)`/`string(s)` became the `ToSlice`/`ToGoString` extensions, which take the caller's
   concrete type (a constructor cannot — C# has no generic constructor — and a static factory cannot
   even be named, because `using static go.builtin` shadows the `slice`/`@string` type names).
   A `parseRFC3339`-shaped body over `slice<byte>` went **720 → 0 B/parse**; `@string` went 776 → 416
   (what remains is the `byte[]` Go's `[]byte(string)` copies too). Guarded by GolibTests
   `ByteSeqAllocationTests` with a `NoInlining`-pinned boxed control that must still read 720.
   A/B footprint was 4 corpus files (10 constraint lines + 6 conversion sites) and one behavioral
   golden.
   **Whole-chain answer (coordinator, r38 train, 2026-08-03): 3,544 -> 216 B/run.** The seam is
   zero.
   **CORRECTED (r39-timer, 2026-08-03) -- the "above `parseRFC3339`" attribution was WRONG.**
   Measured frame by frame with a probe borrowing `InternalsVisibleTo("time.tests")`:
   `Time.UnmarshalText` == `parseStrictRFC3339` == `parseRFC3339` == **88 B/run**, so the wrapper
   chain allocates **nothing**. The 216 is `88 + 128`, and neither half is contained:
   **88 B** is `parseRFC3339`'s `parseUint` func literal capturing `ok` -- a display class plus a
   `Func<>` delegate per call (the same body with the closure replaced by a static local function
   measures 0; a bare capturing lambda control measures exactly 88). Its general fix is a new
   converter emission mode: *a func literal bound to a local that is only ever CALLED should emit as
   a C# local function*, which captures without allocating -- reaching every closure in the corpus
   via `convFuncLit.go` / `captureModeOperations.go`.
   **128 B** is the converter heaping the TEST's own `var t Time`
   (`ref var t = ref heap(new Time(), out var Ꮡt)`) because a pointer-receiver call takes its
   address; Go stack-allocates it, which is why the assert says zero. The emitted `Ꮡt` is never
   referenced, so a narrow "don't heap when the box is unused" rule looks sound -- but it is an
   escape-analysis change and charter §7 puts it behind a reviewed design.
   Neither half alone flips the assert (216 -> 128 still fails `want 0`). Full write-up and the
   ruling options: the r39 entry under `time` in
   [`docs/Phase4/BOARD-next-validation-candidates.md`](Phase4/BOARD-next-validation-candidates.md).
   **Successor item:** the converter still wraps every union sub-slice in `((bytes)(…))`, which is now
   an identity conversion emitting no IL. It is pure noise in the rendering and `s = s[19..]` would
   match the Go exactly; removing it means restructuring the `typeParamIsStringByteUnion` branch in
   `convSliceExpr.go` (the branch also selects the range-indexer emission, so it cannot just be
   dropped). Cosmetic only — deliberately not taken on an allocation-scoped lane.

## Test host / pipeline

8. ~~**`TempDir` case-collision**~~ — **DONE (r37-time-os-fin, 2026-08-02).** The path component is
   now `TempDirName(Name)` = the sanitized name plus a deterministic 8-hex FNV-1a of the EXACT name,
   so `TestFileReaddir` and `TestFileReadDir` no longer resolve to one directory (and the lossy
   whitespace fold — `"a b"` vs `"a_b"` — is covered by the same discriminator). A hash rather than a
   counter because a temp dir must be STABLE run to run; a shared counter would move with thread
   interleaving under `-parallel`. `SanitizeName` itself is untouched — it also names subtests, where
   the string feeds the `go test -json` differential.
9. **`TestReporter` subtest-name escaping** — Go escapes control bytes in subtest names
   (`\x1a`), the C# host emits them raw: 924 cosmetically-paired lines in os's `TestReadStdin`
   report. Touches every package's report format → wants the full sweep as its gate.
10. **`-tests` csproj emission strips the `GoValidationProofFile` block** — the `-tests` and
    `-stdlib` csproj writers disagree on the 8-line validation-proof block, making every pipeline
    run dirty a validated package's production csproj (the standing `0 8` restore family). Fix =
    the `-tests` writer preserves/emits the block; removes a whole documented drift family.
11. **`Tests/PackageTests/ConvertedTestHarness` does not build — the end-to-end `-tests` fixture is
    dead** (found r37-time-os-fin, 2026-08-02; PRE-EXISTING, A/B'd on `fa87dd349` with a
    master-converter binary and reproduced from a clean slate). Its production `value.cs` emits
    `namespace go;` + `convertedtestharness_package`, while the external variant's `external_test.cs`
    qualifies the self-import as `go2cs.convertedtestharness_package` — the module prefix of
    `module go2cs/convertedtestharness` is treated as a namespace segment on the import side and
    dropped on the declaration side. `error CS0234` ×2, so the pipeline never gets to run. The
    fixture has not been touched since it was created in `fbdaf8017` (Step 2.3), so it rotted as the
    namespace scheme evolved. Cost: the documented "run the pipeline on the harness" check in its
    README is unavailable, and a host-behavior test added there (the `TempDir` case-collision guard
    in `value_test.go`) passes on the Go side but cannot be exercised on the C# side.

## Repo hygiene

12. **The whole-corpus rebank** — the umbrella item: ~119+ files of accumulated intended drift
    (satisfies-not-witnesses records, ptrset redirections, os/time/nilrecv footprints, the Δio
    `-tests`-closure family, stale banked test sources incl. `binary_test.cs` 78/78 and csv's).
    ONE deliberate regen + bank + full sweep, its own session.
13. **`C:\go2cs-build` debris** — ~30 stale scratch/probe/recon directories from r26–r34 (`ab*`,
    `fmtcheck*`, `r3x-*` leftovers, `scratch*`, `splitmain`, …) plus the landed chip worktrees.
    Delete after confirming each is branch-landed.
14. **ConversionStrategies-Reference.md ~line 10734** — two unrelated topics mashed onto one line
    (reflectlite mini-bridge paragraph runs into the AllocsPerRun discussion); chip-reported,
    cosmetic.

## Recorded residuals (no work owed unless the surrounding facts change)

14. **The `.ValueSlot` entry-alias residual is now empty — but the arm still exists elsewhere.**
    Retiring the type-selected `.ValueSlot` arm at the pointer ENTRY alias (r37b) was required to
    avoid regressing 9 nil-reachable aliases; `.ValueSlot` stays selected for box-of-pointer LOCALS,
    named-result boxes, `heap(out …)` and the reflection bridge's field paths, where the box is
    non-nil by construction. Nothing is owed unless one of those sites ever becomes nil-reachable —
    recorded so the asymmetry is deliberate rather than forgotten.
15. **Seven banked `DerefOrNil()` sites survive in committed `*_test.cs`** (`container/ring`,
    `go/token`, `index/suffixarray`, `testing/quick`). A `-stdlib` reconvert does not re-emit
    banked test sources, so they still carry the retired accessor; they re-emit as `DerefOrNull()`
    the next time each package's `-tests` pipeline runs. Levels naturally with the whole-corpus
    rebank (item 11) or with each package's next validation pass — no separate work.
