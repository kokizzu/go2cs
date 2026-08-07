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
4. ~~**Remove GoFunc's 17 now-idempotent `panic.CaptureThrowSite(ex)` calls**~~ — **DISSOLVED
   2026-08-05 (r41-goframe).** Both halves of this item, and the pairing warning between them, went
   with the code they described: the GoFrame emission deleted `GoFunc<T>`, the whole
   `GoFunc<TRef1…TRef16>` ladder and the `builtin.func` overloads, so there are no 17 calls left to
   remove — the frame's single `catch` body sets the panic slot and nothing else, `TryAsPanic` having
   already taken the origin snapshot at the adoption point. The `GenGoFuncRefInstances` utility whose
   drifted template was the paired hazard is deleted with them. Original text, for the record:
   *"Remove GoFunc's 17 now-idempotent `panic.CaptureThrowSite(ex)` calls — subsumed by `TryAsPanic`'s
   adoption-point snapshot (r36-time-tail). ⚠ PAIRED with the `GenGoFuncRefInstances` template repair:
   the template has drifted from the live 16-rung ladder (still emits bare `catch (PanicException ex)`),
   so regeneration without the repair silently reverts panic behavior. Do both together or neither."*
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
   [`docs/phase4/BOARD-next-validation-candidates.md`](phase4/BOARD-next-validation-candidates.md).
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
10. ~~**`-tests` csproj emission strips the `GoValidationProofFile` block**~~ — **DONE (r39 train,
    2026-08-03).** `validationPackBlock` gates on `convertStdLib` OR a `-tests` rewrite whose output
    csproj lives under the runtime root's `core\` tree (`testsRewriteOfCorePackage` — structural, so
    fixtures and end-user modules keep their historical bytes). Guarded by
    `TestValidationPackBlockSurvivesTestsRewriteOfCorePackage`; the end-to-end proof is the next
    sweep's aftermath, which should no longer contain the `0 8` csproj family at all.
11. **`tests/PackageTests/ConvertedTestHarness` does not build — the end-to-end `-tests` fixture is
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

12. ~~**The whole-corpus rebank**~~ — **DONE 2026-08-04 (r40-rebank).** The umbrella item:
    accumulated intended drift (satisfies-not-witnesses records, ptrset redirections,
    os/time/nilrecv footprints, the Δio `-tests`-closure family, stale banked test sources incl.
    `binary_test.cs` 78/78 and csv's), landed as ONE deliberate regen + bank + full sweep in its
    own session. The forecast held: **699 `.cs` carried a genuine drift family**, against the 695
    measured 2026-08-03 by r39-nilcomplex — the difference is the arcs that landed in between.
    **1,316 files banked in all**, across sixteen named families with **zero unclassified**:
    deref-accessor 592, dead-param-alias 541, GoBigConst 304, README-badge 298, typed-nil 145,
    local-func 90, GoImplement 44, value-adapter 40, implicit-conv 23, closure-box 22,
    import-alias 20, wrapper-qualification 17, pointer-reinterpret 15, named-const-cast 12,
    fallthrough 12, alias-pointer 10. Two of those are the rebank's own corpus-wide relabels (the
    `GoUntyped` → `GoBigConst` rename and the `Go_tests` → `Tests` badge), which is why the total
    exceeds the drift-only forecast. The r39c pointer peephole showed no new drift, as predicted.
    Gates: hand-owned marker gate 40 marked / 0 clobbered; `go2cs-stdlib.slnx` 304 projects,
    0 errors; CNR byte-identical across 569 behavioral packages; full suite 544/544 with 514
    output-compared, 0 failed.
    Two findings worth carrying forward. (a) The `-tests`-closure family of
    DESIGN-named-interface-wrappers §7 contributed nothing to the bank, but is **NOT** discharged:
    the committed baseline has simply moved to the `-stdlib` side of the asymmetry, so a `-stdlib`
    reconvert reproduces it (four of the six byte-identical). A `-tests` run still emits
    `using Δio = io_package;` where `-stdlib` emits `using io = io_package;`, and the sweep still
    re-flips it on every roster package, where it is restored. The §7 debt is unchanged in
    substance — only which side the tree rests on between runs. (b) The 16 committed `.cs.auto`
    review siblings turned out to be **tracked, with 11 stale**, which is now item 18.
13. **`C:\go2cs-build` debris** — ~30 stale scratch/probe/recon directories from r26–r34 (`ab*`,
    `fmtcheck*`, `r3x-*` leftovers, `scratch*`, `splitmain`, …) plus the landed chip worktrees.
    Delete after confirming each is branch-landed.
14. **ConversionStrategies-Reference.md ~line 10734** — two unrelated topics mashed onto one line
    (reflectlite mini-bridge paragraph runs into the AllocsPerRun discussion); chip-reported,
    cosmetic.
15. **`reflect.Value.Len()` reports 0 for EVERY channel.** `src/core/reflect/value_impl.cs`'s `Len`
    switch has no `IChannel` arm, so a channel falls through to `_ => 0`; `internal/reflectlite`'s
    `chanlen` likewise returns `default`, and `reflect`'s own `chanlen`/`chancap` partials are dead
    `NotImplementedException` stubs. `Cap()` DOES have the arm and is correct. Found while reviewing
    the synchronous-timer-channel arc (r39b), which made the gap *accidentally* right for timer
    channels — `len` of one is legitimately 0 — while it stays wrong for every ordinary buffered
    channel. Pre-existing, no known consumer; the fix is one `IChannel c => c.Length` arm.
16. **A `-tests` run STRIPS the validation-pack block from a banked package's `.csproj`, so a full
    sweep drifts all 72 of them.** `validationPackBlock` (`src/go2cs/projectFileWriter.go`) returns
    `""` unless `options.convertStdLib`, and the `-tests` pipeline is not a `-stdlib` conversion —
    so every pipeline run regenerates `<pkg>.csproj` without the
    `GoValidationProofFile` / `VALIDATION.md` pack, a uniform `0/8` diff. Harmless while the drift
    is restored (a sweep is a gate, not a rebank), but it is a **loaded gun for the whole-corpus
    rebank**: banking a post-sweep tree would silently un-ship every package's VALIDATION.md. The
    block is `Exists`-guarded on both ends and therefore correct for any conversion, so the fix is
    to emit it whenever the output is a stdlib package rather than only under `-stdlib`. Found by
    the r39b sweep (72 pass / 0 fail, 321 drifting files: 132 banked test sources, 92 production
    `.cs`, 72 csproj, 15 test hosts, 8 `package_info.cs`, 2 `package_init.cs`).
17. **`Timer.C`/`Ticker.C` are emitted BIDIRECTIONAL, so Go-illegal sends compile.** `sleep.cs` and
    `tick.cs` emit `public /*<-*/channel<Time> C;` — the receive-only direction is a comment, not a
    type. Go rejects `t.C <- v`; converted C# accepts it. Newly consequential since r39b: a value a
    user pushed there is silently discarded by the next `Stop`/`Reset` drain, and because
    `DrainBuffer` deliberately does not service parked senders (matching Go's `timerchandrain`), a
    second such send parks forever. Reachable only from source Go itself rejects, so this is the
    general directional-channel-type fidelity gap, recorded at the place it now bites.
18. **The 16 committed `.cs.auto` review siblings are TRACKED, and 11 are stale.** Found by the
    r40 rebank. A `.cs.auto` is the converter's "here is what I would emit today" sibling dropped
    beside a `[module: GoManualConversion]` file, and its whole value is being CURRENT — a stale
    one misinforms exactly the reviewer deciding whether the hand-own is still needed. They drift
    because the overlay rule excludes `*.cs.auto`, and that exclusion is not incidental: it is what
    keeps a regen from clobbering the hand-owned `.cs` beside it. So the rebank deliberately did
    **not** smuggle them into its bank. Levelling them is a self-contained commit: reconvert into a
    seeded temp root (the rebank's own ritual), copy only the `*.cs.auto`, confirm no `.cs` moves.
    Stale as of r40: `crypto/subtle/xor_generic`, `hash/crc32/crc32_amd64`, `runtime/mfinal`,
    `sync/{mutex,pool,poolqueue,rwmutex,waitgroup}`, `syscall/{dll_windows,exec_windows}`,
    `time/tick`.

19. **`bodyWrappedInDeferContext` no longer HAS to force the direct-`ж` receiver.** A method that
    defers at function level and references its receiver takes `this ж<T> Ꮡx` rather than
    `this ref T`, because a `ref T` receiver could not be referenced from inside the execution-context
    lambda (CS1628). The GoFrame emission (r41) puts the body inline in the method, so that constraint
    is gone and `this ref T` compiles. The rule was KEPT deliberately — the direct-`ж` form is also the
    alloc-free, race-free one, and switching receiver shapes is a corpus-wide change with its own blast
    radius, not a side effect of the frame. Whether to take it is a real question with a real answer on
    both sides; it wants its own measurement (how many methods, what the emitted diff looks like, what
    it costs or saves) rather than a reflex.
20. **A `-stdlib` reconvert PANICS on two auto-sibling visits.** `internal/godebug/godebug.go` and
    `internal/concurrent/hashtriemap.go` report `visit file error: … invalid memory address or nil
    pointer dereference` and their `.cs.auto` REVIEW siblings are skipped. Production emission and
    package-wide state are unaffected — the auto-sibling pass is a separate re-visit whose only
    output is the review file — so this shows up as two of item 18's stale siblings rather than as
    corpus damage. A/B'd at r41 against the master converter: identical, so it is pre-existing and
    was not introduced by the frame arc. Worth rooting when item 18 is levelled, since `hashtriemap`
    has never had a `.cs.auto` at all and `godebug`'s is frozen at whenever the panic started.
## Recorded residuals (no work owed unless the surrounding facts change)

14. **The `.ValueSlot` entry-alias residual is now empty — but the arm still exists elsewhere.**
    Retiring the type-selected `.ValueSlot` arm at the pointer ENTRY alias (r37b) was required to
    avoid regressing 9 nil-reachable aliases; `.ValueSlot` stays selected for box-of-pointer LOCALS,
    named-result boxes, `heap(out …)` and the reflection bridge's field paths, where the box is
    non-nil by construction. Nothing is owed unless one of those sites ever becomes nil-reachable —
    recorded so the asymmetry is deliberate rather than forgotten.
15. ~~**Seven banked `DerefOrNil()` sites survive in committed `*_test.cs`**~~ — **LEVELLED
    2026-08-04 (r40-rebank, commit C), exactly as this item predicted and with no separate work.**
    (`container/ring`, `go/token`, `index/suffixarray`, `testing/quick`.) The rebank's 73-package
    sweep ran every one of those packages' `-tests` pipelines, and each site re-emitted as
    `DerefOrNull()`. Verified: zero `DerefOrNil` sites remain in any committed `*_test.cs`.
    Original text, for the record: *"A `-stdlib` reconvert does not re-emit banked test sources, so
    they still carry the retired accessor; they re-emit as `DerefOrNull()` the next time each
    package's `-tests` pipeline runs. Levels naturally with the whole-corpus rebank or with each
    package's next validation pass — no separate work."*
