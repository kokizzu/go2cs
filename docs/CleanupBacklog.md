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

6. **The 136-byte `for range` enumerator allocation over `slice<T>`** — corpus-wide: the range
   form allocates a fixed enumerator per loop where the indexed form allocates zero
   (r36-time-tail's measurement inside `parseRFC3339`).
7. **`IByteSeq<T>` interface-boxing** — the range indexer returns the interface so every `s[a:b]`
   boxes (48 B), and `[]byte(s)` boxes again. Same measurement site.

## Test host / pipeline

8. **`TempDir` case-collision** — `SanitizeName(TestName)` collides test names differing only by
   case on case-insensitive filesystems (`TestFileReaddir` vs `TestFileReadDir`, both parallel,
   each deleting the other's dir). Remedy: disambiguate the sanitized name. Proven defect,
   r36-nilrecv finding.
9. **`TestReporter` subtest-name escaping** — Go escapes control bytes in subtest names
   (`\x1a`), the C# host emits them raw: 924 cosmetically-paired lines in os's `TestReadStdin`
   report. Touches every package's report format → wants the full sweep as its gate.
10. **`-tests` csproj emission strips the `GoValidationProofFile` block** — the `-tests` and
    `-stdlib` csproj writers disagree on the 8-line validation-proof block, making every pipeline
    run dirty a validated package's production csproj (the standing `0 8` restore family). Fix =
    the `-tests` writer preserves/emits the block; removes a whole documented drift family.

## Repo hygiene

11. **The whole-corpus rebank** — the umbrella item: ~119+ files of accumulated intended drift
    (satisfies-not-witnesses records, ptrset redirections, os/time/nilrecv footprints, the Δio
    `-tests`-closure family, stale banked test sources incl. `binary_test.cs` 78/78 and csv's).
    ONE deliberate regen + bank + full sweep, its own session.
12. **`C:\go2cs-build` debris** — ~30 stale scratch/probe/recon directories from r26–r34 (`ab*`,
    `fmtcheck*`, `r3x-*` leftovers, `scratch*`, `splitmain`, …) plus the landed chip worktrees.
    Delete after confirming each is branch-landed.
13. **ConversionStrategies-Reference.md ~line 10734** — two unrelated topics mashed onto one line
    (reflectlite mini-bridge paragraph runs into the AllocsPerRun discussion); chip-reported,
    cosmetic.
