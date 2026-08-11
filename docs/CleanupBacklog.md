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
18. ~~**The 16 committed `.cs.auto` review siblings are TRACKED, and 11 are stale.**~~ — **LEVELLED
    2026-08-08 (`08fbb564b`, at the r48 train head), exactly by the ritual this item prescribed:**
    seeded reconvert, marker gate 41 line-anchored hand-owns / 0 clobbered, harvest touched
    `.cs.auto` files and nothing else — 14 of 16 refreshed, 2 already current. The shape banked is
    the r48 wave's own fixes arriving in the review siblings (most visibly `sync/atomic/type.cs.auto`
    shedding all 58 dead named-result locals to the CS0219 fix). The drift MECHANISM is unchanged —
    the overlay still deliberately excludes `*.cs.auto` to protect the hand-owned `.cs` beside them —
    so they will drift again; re-level at each rebank head rather than re-opening this item.
    Original staleness census, for the record: `crypto/subtle/xor_generic`, `hash/crc32/crc32_amd64`,
    `runtime/mfinal`, `sync/{mutex,pool,poolqueue,rwmutex,waitgroup}`,
    `syscall/{dll_windows,exec_windows}`, `time/tick`.

19. **`bodyWrappedInDeferContext` no longer HAS to force the direct-`ж` receiver.** A method that
    defers at function level and references its receiver takes `this ж<T> Ꮡx` rather than
    `this ref T`, because a `ref T` receiver could not be referenced from inside the execution-context
    lambda (CS1628). The GoFrame emission (r41) puts the body inline in the method, so that constraint
    is gone and `this ref T` compiles. The rule was KEPT deliberately — the direct-`ж` form is also the
    alloc-free, race-free one, and switching receiver shapes is a corpus-wide change with its own blast
    radius, not a side effect of the frame. Whether to take it is a real question with a real answer on
    both sides; it wants its own measurement (how many methods, what the emitted diff looks like, what
    it costs or saves) rather than a reflex.
20. **A `-stdlib` reconvert PANICS on ~~two~~ THREE auto-sibling visits.** `internal/godebug/godebug.go`,
    `internal/concurrent/hashtriemap.go` and — **re-measured r59, 2026-08-11** —
    `internal/weak/pointer.go` report `visit file error: … invalid memory address or nil
    pointer dereference` and their `.cs.auto` REVIEW siblings are skipped. Production emission and
    package-wide state are unaffected — the auto-sibling pass is a separate re-visit whose only
    output is the review file — so this shows up as ~~two~~ three of item 18's stale siblings rather than as
    corpus damage. A/B'd at r41 against the master converter: identical, so it is pre-existing and
    was not introduced by the frame arc. Worth rooting when item 18 is levelled, since `hashtriemap`
    has never had a `.cs.auto` at all and `godebug`'s is frozen at whenever the panic started.
    `pointer.cs` joined the hand-own census at r43e, AFTER this item was written, and like
    `hashtriemap` has never had a `.cs.auto` — so the count tracks the hand-own census and must be
    re-measured, not carried forward.
    **What the three have in common, and it is the actual root shape:** each is a package whose
    ENTIRE (single) Go file is hand-owned, so `unmarkedFileCount == 0` and the auto-sibling re-visit
    is the only visit the package gets — which is why the panic costs nothing but the review file.
    That also generalizes a fact CLAUDE.md records in the singular: `internal/godebug` is described as
    the one package whose `.csproj`, `package_info.cs` and `README.md` are "hand-owned by
    consequence" and never re-emitted. It is a **class of three** on the same mechanism
    (`internal/concurrent`, `internal/godebug`, `internal/weak`), and r59's reconvert measured exactly
    three un-emitted `package_info.cs` for that reason.
22. **The four items the warning-suppression arc deliberately did not take.** r46b landed the
    configuration half of [`phase4/DESIGN-warning-suppression.md`](phase4/DESIGN-warning-suppression.md)
    and stopped there on purpose; §5 and §7 of that doc carry the full detail, this is only the
    parking record. (a) **`CS0219`, 1,219 sites across 136 packages** — the converter declares every
    named result as a local at function entry and 1,218 of those are never read. Emit the local only
    when the body reads, assigns, address-takes or defer-captures it; that keeps `CS0219` alive as
    the one static signal for a genuinely dropped assignment to a named result, which suppressing it
    would have destroyed. (b) **`CS8778`, 620 sites** — untyped Go constants taking `int`→`nint`
    instead of a composite literal's `int64` element type (607 in `math/rand/rng.cs` alone), plus 13
    folded constants missing their `unchecked`. That is a live 32-bit truncation, and the one warning
    in the corpus that is unambiguously right. Together (a) and (b) are **94.5 % of the 1,945
    warnings that remain**, so the next honest reduction is a converter fix, not another `NoWarn`
    entry. (c) **golib's 26 `IL####` warnings want `DynamicallyAccessedMembers` annotations and
    justified `UnconditionalSuppressMessage`, never a `NoWarn`** — golib is precisely the assembly a
    trimmed or AOT-published converted app breaks on, which is why its publish properties were left
    unconditioned when the corpus's were scoped off `Library`. (d) **golib's five `CS8618`**
    (`slice.m_array`, `ж.m_val`, one in `GoReflect.ValueMarshalling`) want `= null!` on the fields;
    the code was deliberately NOT added to golib's suppression list, so the to-do stays visible.

## Recorded residuals (no work owed unless the surrounding facts change)

21. **A LEADING `[` is still read as a generic bracket, so `[]<-chan <module path>.T` renders
    mangled.** The r45a fix (issue #33's third report) stopped `convertToCSFullTypeName`'s
    import-path rewrite from eating the type CONSTRUCTOR in front of the path, which is what turned
    `<-chan …/mongo-driver/…` into `<_chan …` and then into an unbounded self-recursion. The
    `<-chan []T` nesting is fixed with it; the `[]<-chan T` nesting is not, because `genericStart`
    takes the FIRST `[` in the string as the start of a generic argument list and a leading slice
    constructor truncates the path scan to nothing. The fix is to require a generic bracket to
    FOLLOW a path byte (a generic `[` always follows the name it instantiates) — which is correct,
    but re-routes every `[]<pkg>/<sub>.T` in the corpus from the suffix-less `else` branch to the
    main one, i.e. a `_package`-suffix emission change corpus-wide. That did not belong in an urgent
    crash fix and is not owed now: the shape needs a slice OF receive-only channels of a
    module-path type, and the array-branch bound landed in the same change means it produces a named
    `WARNING` and a finite wrong name rather than killing the run. Pinned as an explicit decision at
    the end of `TestConvertToCSFullTypeNameConstructedModulePaths`; do this one WITH a corpus
    reconvert, never on its own.
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

23. **Published 1.23.1.5 nupkg READMEs show .NET Source @1.23.1.4 — the badge TEXT trails the
    deployed version by one (user-reported from the go.sync gallery page, 2026-08-09).** Known,
    root-caused, and already fixed for every future release: the release script's badge-text
    retarget pattern was written against the pre-tidy `Source-C%23_@` form, r51d renamed the text
    to `Source-@` the same day, so the 1.23.1.5 pack moved the badge's LINK but not its TEXT — and
    the verifier skipped non-matching files instead of failing (a vacuous pass). `f2b80a766` fixed
    the pattern AND made the verifier throw on any package README without the badge, so the class
    cannot ship silently again; the repository's READMEs were corrected in the same landing
    (`1ab15f998`). NuGet versions are immutable, so the published 1.23.1.5 pages keep the cosmetic
    one-version trail until the NEXT release replaces them — nothing to do besides ship the next
    release, which the now-throwing verifier guards. Do not re-diagnose from the gallery page.

24. ~~**Emitted-artifact comments carry converter HISTORY into user-level files — rewrite present
    tense, corpus-wide**~~ — **DONE 2026-08-11 (r59), riding backlog 25's regen bank per the
    coordinator's sequencing ruling.** Five comments rewrote: the csproj test-artifact exclusion, the
    `$(GoTargetOS)` default (which justified itself by "the single-platform package this layout
    replaced"), the `Nullable` rationale (which cited a 1,142-warning measurement from one past
    corpus build — a number that would go stale even if the history belonged there), the test
    csproj's MSB4006 incident report, and `package_info.cs`'s account of the run-time interface
    resolution that was not chosen. Measured corpus footprint: **297 emitted `.csproj`** (Nullable +
    test-artifact), **34 of those also the `$(GoTargetOS)` block** (only the L3 packages carry it),
    and **297 `package_info.cs`**. The audit found the pubxml profiles, the emitted
    `Directory.Build.props`/`.targets` and the README emitter clean. `package_init.cs` — a new
    emitted artifact L4 added mid-lane — was audited and already compliant.
    One mechanism had to exist first: `writePackageInfoFile` rebuilds only the marker SECTIONS of an
    existing file and copies every other line through verbatim, so a template rewrite would have
    reached NEW files only and the corpus would carry as many wordings as it has had rewrites. The
    `<TypeAccessibility>` block already had a bespoke in-place migration for that reason; it is now a
    shared `migrateProseBlock` both blocks use, so the next prose rewrite is two lines.
    **Platform residual, by design of the single-target ritual:** 57 non-Windows per-GOOS
    `package_info.cs` keep the old prose, as do the 3 fully-hand-owned packages and `unsafe`, because
    a `-stdlib` run emits for ONE target and never re-emits a package whose every file is hand-owned.
    The corpus therefore carries two wordings across platforms until a multi-platform `-platforms`
    emission levels the other two — which is uniform with how L4's and L7's emission changes landed
    in the same bank, not specific to this one.

    Original text, for the record: *The csproj template's test-artifact exclusion
    comment narrates its own past ("the old `*._test.cs` pattern matched nothing the converter
    emits...") — historical precedent that is meaningless in a file every package ships. The r42
    docs ruling (present tense, educate the new reader, no history) applies with MORE force to
    emitted files than to docs. Scope: audit EVERY emitted-artifact template for the same smell —
    both csproj templates, the pubxml profiles, the emitted Directory.Build.props/.targets (the
    -recurse artifacts redirect and the deploy root's), the validation-pack block, the README
    emitter's fixed prose — rewrite each comment to state only what the line DOES and the
    constraint it serves. Comment-only change but corpus-wide: every emitted csproj moves, so it
    lands as its own regen bank (CNR behavioral-csproj re-baseline included) at an idle point or
    riding the next rebank arc. History belongs in ConversionStrategies-Reference, not in the
    artifact.*

25. ~~**Investigate moving `[GoValueClone]` — and every movable extended attribute — off the
    mainline type declaration into `package_info.cs`'s `<TypeAccessibility>` records**~~ —
    **DONE 2026-08-11 (r59).** The investigation found a criterion sharper than "is it needed here":
    **a stamp can move whenever its consumer reads the attribute off the TYPE rather than off a
    particular declaration**, because C# unions the attributes of every part of a partial type, so the
    move is invisible to runtime reflection and to any generator that resolves the symbol. That
    criterion classifies the whole surface and the movable set is exactly two — `[GoValueClone]` and
    `[GoLocalName]` — both moved. `[GoType] [GoValueClone("intbuf")] partial struct pp {` now reads
    `[GoType] partial struct pp {`.
    Measured corpus footprint: **304 stamps left mainline declarations and 304 arrived on records** —
    the conservation is the evidence that the relocation loses nothing — of which 270 `GoValueClone`
    and 34 `GoLocalName`. The stamps and the access modifier travel together by construction:
    `recordTypeAccessibility` takes the stamps and RETURNS what the caller must still write inline,
    empty when the record absorbed them, so the two paths that write no record (a hand-owned file,
    whose emission goes to the non-compiled `.cs.auto` sibling, and a `-tests` bridge unit) keep their
    stamps and cannot lose them.
    The full classification is recorded in *Extended attributes: what stays on the declaration and
    what moves* in [`ConversionStrategies-Reference.md`](ConversionStrategies-Reference.md) — eleven
    rows, must-stay reasons distinguished by LEVEL (`[GoType]` is the syntax receiver's key;
    `[GoTag]` is field-level and a record is an empty `{}` body; `[GoRecv]` is method-level;
    `[GoInit]` is a `using` alias for `ModuleInitializerAttribute` and the compiler wants it on the
    method; `[GoArrayDims]` is parameter-level and not type-keyed at all, since
    `func([32]byte) bool` and `func([64]byte) bool` share one emitted delegate type).
    Guarded in the converter's own `go test` (the relocation returns empty on the recording path and
    the stamps verbatim on both skip paths; the sort key orders a stamped entry with its peers), and
    `LiftedLocalTypes` was strengthened rather than weakened: its guard pinned `[GoLocalName]` as
    golden TEXT, and `package_info.cs` has no golden, so it now prints `%T` of a function-local named
    type and must equal Go's `main.point *main.point` — the property the stamp exists for.
    **Residual, left knowingly and on evidence:** the record set is keyed by the RENDERED LINE, so one
    type yields one record only while every pass renders it identically, and the access modifier is
    the variable. Two records for one type would each carry `[GoLocalName]`, which golib matches as a
    single-element list, so `%T` would fall back silently — a quieter failure than the pre-existing
    `CS0262` two-conflicting-modifiers case. Measured across the regenerated corpus: 5,140 records,
    **zero collisions**.

    Original text, for the record: *The visible converted code should read as
    close to the Go original as the emission allows; `[GoType] [GoValueClone("intbuf")] partial
    struct fmt` carries machinery the READER never needs, and package_info.cs already exists to
    hold exactly this class of per-type record out of view. Scope: (1) find `GoValueClone`'s
    consumer (generator or golib reflection) and determine whether an assembly-level record keyed
    by type serves it identically — including any ordering/partial-class constraints; (2) if
    implementable, migrate emission + consumption with the full A/B discipline (the attribute
    appears corpus-wide, so this is a regen-bank change; behavioral goldens move); (3) AUDIT the
    whole extended-attribute surface for the same movability — every attribute the converter stamps
    on declarations beyond bare `[GoType]` (survey Symbols/golib attribute definitions and the
    emitters that write them), classifying each as movable / must-stay (e.g. anything a generator
    needs syntactically ON the declaration) / field-level-different (e.g. `[GoTag]`); (4) record
    the classification in ConversionStrategies-Reference and move what cleanly moves. Readability
    is the goal; behavioral identity is the bar.*
