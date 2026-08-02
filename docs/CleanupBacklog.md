# Cleanup backlog — idle-point refactoring list

> **Low priority by design.** Items parked here are deliberate deferrals: real improvements that
> did not belong in the change that discovered them (charter §2 forbids folding cleanup into a
> fix's blast radius, and the merge that notices a vestigial arm is the wrong place to prune it).
> Pick items up at idle points or as a dedicated cleanup wave; every item still takes the full
> gate for its change class — parking here lowers priority, never rigor. Started 2026-08-02 from
> the r35/r36 waves' discoveries (user-requested).

## Converter / golib — semantics-adjacent (gate: full suite + corpus, some need the sweep)

1. **Unify pointer-PARAMETER derefs on `DerefOrNull` — USER-RULED "should" (2026-08-02), footprint
   pending.** Go's nil rule is identical for a parameter and a receiver, so the ~3,167 parameter
   entry aliases that keep eager `.Value` (and the analysis-gated `.DerefOrNil` arms) should take
   the same nil-deferring accessor receivers now use. Retires `collectNilSafePtrParams` +
   `nilSafeEntryOnlyParamName` and their analyses outright. Needs its own measured corpus/behavioral
   footprint and the full gate chain — the emission churn is large even though the edit is one
   shape. See *The FOUR deref accessors of `ж<T>`* in ConversionStrategies-Reference.md.
2. **Prune the vestigial receiver arms of the nil-safe analysis** (`visitFuncDecl.go`): after r36,
   a direct-ж receiver takes `DerefOrNull` before the nil-safe sets are consulted, so
   `isDirectBoxReceiverIdent`'s consultation arms are dead paths (kept at the merge, commented as
   cleanup-pass material). Subsumed by item 1 if that lands first.
3. **`DerefOrNil` → null-ref semantics in the body** (zero emission churn): closes the residual
   silent-`default(T)` hole for the walk/nil-arg arms. Dissolves entirely if item 1 retires the
   accessor; measure only if taken standalone.
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
7. **`IByteSeq<T>` interface-boxing — PROMOTED (user-ruled 2026-08-02): gates `time`'s bank.**
   The range indexer returns the interface so every `s[a:b]` boxes (48 B), and `[]byte(s)` boxes
   again — now the WHOLE remaining share of `time.TestUnmarshalTextAllocations` (item 6 removed the
   six range loops' worth; measured remainder 2,728 B/run, all in this seam).
   `TestUnmarshalTextAllocations` wants ZERO allocations and the user ruled NO disclosure — a
   want-zero is satisfiable, so softening it would spend the credibility the badges exist to earn.
   The self-referential-generic redesign of the converter-emitted union-constraint interface is
   therefore on `time`'s critical path, not idle-point work.

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
