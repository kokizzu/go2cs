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
> A note the arc earned: a **first diagnostic is a starting point, not a diagnosis**. `io`'s first
> error is CS0012 and reads as a missing reference; it is not one. Two of the three claims below
> that were stated as "measured" did not survive re-measurement on a freshly built converter.
>
> Re-validate everything after any change here with `./src/run-validated-sweep.ps1` — it reads the
> roster and the expected counts from [`ValidatedTestPackages.md`](../ValidatedTestPackages.md) and
> fails on a count mismatch, so a package that still passes but asserts something different is
> caught rather than assumed.

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

What was found (full write-up: [`DESIGN-recursive-enduser-conversion.md`](../Phase3/DESIGN-recursive-enduser-conversion.md),
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

1. `./src/Tests/Behavioral/check-no-regression.ps1` — timeout 700s. **Expect byte-identical**: the change
   is an env-var gate on a `-recurse`-only load path plus a checkout attribute; no emission logic moved.
2. `./src/Tests/Behavioral/run-behavioral.ps1` (full) — timeout 2100s. Expect 544/544 + 514/514.
3. `go test -run 'TestRecurse|TestModuleCachePoisonedGoWorkLoad' ./` from `src/go2cs` — the new guard's
   first Windows run.
4. `./src/run-validated-sweep.ps1` only if 1–3 surface anything (no path into the banked suites otherwise).
5. ⚠ The `eol=crlf` pin takes effect on **checkout** — existing Windows clones already have CRLF working
   trees via autocrlf, so expect no visible change there; a `git status` after pulling the attribute
   commit should stay clean for the three templates. If it does not, that is a finding.

## Open — CNR's verdict depends on an AMBIENT environment variable, and the `package_info.cs` corpus is already split (found 2026-08-06)

**CNR is not deterministic across machines**, and the non-determinism is silent: it changes which files
the gate reports, never whether it errors. Found while discharging the entry above, where it presented
as a plausible converter regression that took an A/B against a rebuilt converter to clear.

The mechanism. `getImportPackageInfo` maps a stdlib import to `$(go2csPath)core\<pkg>` and substitutes
**`options.go2csPath`** — the converter's `-go2cspath`, default `~/go2cs` (env `GO2CSPATH`), which is
*not* the MSBuild `$(go2csPath)`. The imported package's `package_info.cs` is read from there to mint the
`<ImportedTypeAliases>` block. If that root is missing, stale, or partial, the aliases are **silently
omitted** — no warning, no error, exit 0. Three things let it persist unnoticed:

- **CNR and both behavioral runners invoke `go2cs.exe` with no `-go2cspath`**, so every run inherits
  whatever `GO2CSPATH` the shell happens to carry.
- **`deploy-core.ps1` stages to `%GOPATH%\src\go2cs`, while the converter defaults to `~/go2cs`** —
  different roots, so running the documented deploy does not populate the root the converter reads.
- **`main.go`'s `isGo2CSRoot` / `findGo2CSRootAbove` self-location is gated on `options.convertTests`**,
  so the recovery that would catch exactly this protects `-tests` runs only.

Measured on this box, where `~/go2cs` is a **March–May 2025 stub-era deploy** (15 packages, `errors-old`
still in it, no `reflect`):

| resolved root | CNR reports | direction |
|:--|:--|:--|
| `GO2CSPATH` unset → `~/go2cs` (stale stub) | **4** files | **lose** 16 `reflect` aliases (DeepEqual, ReflectMapRangeNilKey, ReflectMethodTableWalk, ReflectZeroAndGrow) |
| `GO2CSPATH=<repo>\src` (converted stdlib) | **12** files | **gain** 46 `time`/`syscall`/`encoding/json`/`io` aliases (ExprSwitch, ForVariants, GoCallVariations, GoexitDefers, SelectSendDefault, SyncTimerChannel, UnsafeStringEmpty, StructPromotionWithInterface, StructPointerPromotionWithInterface, JsonUnmarshalerDispatch, FindFirstFileData, PipeCloseUnblocksRead) |

The two sets are **disjoint**, so **no single root reproduces the committed corpus** — it was written by
sessions with differing environments and is already split: 565 of 569 files match the stale root, 557
match the repo root. Nothing catches it, because **`package_info.cs` has no `.cs.target` golden** (0 of
580), so `TargetComparison` structurally cannot see it and CNR's `git status` is the only instrument —
the one whose answer moves with the ambient variable.

**Not a live defect.** Every alias in both directions is an *unused declaration*; both configurations
compile and run, proved by a full 544/544 + 514/514 suite over freshly transpiled root=`<repo>\src`
output. So this is drift and a gate-integrity problem, not breakage.

**Which root is canonical is not a judgement call**: the behavioral `.csproj`s bind
`$(go2csPath)core\<pkg>` with MSBuild `$(go2csPath)` → `$(SolutionDir)` → `src\`, so at compile time the
tests link `src/core`. The emitted aliases must describe *those* assemblies ⟹ **`<repo>\src` is
canonical and the 12 files are stale**, left over from before the trees unified.

The durable fix is three small pieces, and the order matters — make the gate deterministic *first*, or
the normalization just re-splits:

1. **Make the root explicit at the seam** — CNR, `BehavioralRunner` and `BehavioralTestBase` pass
   `-go2cspath <repoRoot>\src` rather than inheriting the ambient variable.
2. **Make an unusable root loud** — the converter should refuse, or at minimum warn, when the resolved
   `go2csPath` is not a go2cs root (no `core\golib`), instead of emitting a quietly alias-free
   `package_info.cs`. Extending the `-tests` self-location to every conversion whose output lands inside
   a go2cs source tree would fix the default outright and help anyone running `go2cs.exe` by hand.
3. **Then normalize the 12** in one commit, and consider whether `package_info.cs` should carry a golden
   so `TargetComparison` can hold the line afterwards.

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

1. `./src/Tests/Behavioral/check-no-regression.ps1` — timeout 700s. **Expect byte-identical**: the
   change touches only error paths and `-recurse`-scoped branches, and no emission logic. A non-empty
   `git status` here is a real finding and outranks everything else in this entry.
2. `./src/Tests/Behavioral/run-behavioral.ps1` (full, 4 phases) — timeout 2100s. Expect 544/544 +
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

## COMMISSIONED — the GoFrame arc (user rulings 2026-08-05), and two tasks it queues

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

**Queued task 2 — the `[GoTestMatchingConsoleOutput]` audit (idle-point, measured).** Before `core/fmt`
was real, some behavioral tests skipped output-matching because the stub could not format their output.
Measured 2026-08-05: **14 projects** have `package main` but no attribute — ChannelReceiveFromNil,
ChannelSendToClosed, ChannelSendToNil, DeferSimple, ForVariants, GoCallVariations,
InferredForeignTypeNoImport, InterfaceInheritance, PointerCastSliceRange, RangePointerArrayConversion,
SelectStatement, StructWithPointer, TypeConversionReturnType, UnsafePointerReinterpret. Evaluate each
against `go run` under the real fmt; some are deliberate (nondeterministic or panic-exit programs), but
the number that can graduate to output-compared is likely not zero. Adding the attribute + regenerating
the test classes via UpdateTestTargets is the whole change per graduate.

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

## `math/big` — arithmetic RUNS as of 2026-08-02; one nil-argument root stands in front of a probe

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
  C#. **That increment is now DONE** (`recordSamePackageValueImplements`, `samePackageImplements.go`):
  the declaring side records the VALUE pairs it satisfies, behind five gates — exported interface,
  underlying not a `*types.Signature`, neither side generic, both sides declared in a file the run
  converts, and every interface method reachable within ONE embed hop (ImplementGenerator forwards a
  promoted member exactly that far) — and a whole-stdlib A/B landed the prediction below exactly, 89
  constructions across 34 files (43 + 36 + 5 + 5), alongside 33 records added and 31 removed (3
  prune-subsumed, 28 consumer-local) across 16 declaring packages; 68 files total,
  `go2cs-stdlib.slnx` 0 errors. `HandlerFunc`→`ΔHandler` is absent, as the delegate gate requires.
  Owed, and the reason the depth gate is conservative: extending ImplementGenerator's promoted-member
  forwarding past one hop would recover `net`'s two `tcpConnWithout*`→`Conn` records.
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
| `TestRootDirAsTemp` | 1 | Newly REACHED 2026-08-02. The test re-execs the host with TMP/TEMP pointed at a drive ROOT to check `TempDir()`; the CHILD host then cannot create its own isolated run directory there — `DirectoryNotFoundException: Could not find a part of the path 'Z:\go2cs-tests\os\…'` out of `TestHost.Run`'s `Directory.CreateDirectory(workingDirectory)`. The isolation model and the test's premise collide: Go's test binary needs no scratch directory of its own. Pre-existing (same line before and after this lane's host change). || `TestWriteStringAlloc` | 1 | `AllocsPerRun` bounded at ZERO. Deliberately **not** disclosed: the byte-derived shim CAN report 0, so the io/strings unit-mismatch ruling does not cover it. Go's `WriteString` avoids the copy with `unsafe.Slice` over the string's own bytes; a go2cs `@string` is its own storage, so the write path allocates (measured 9088 bytes). A real divergence — an `sstring`-shaped optimization, not a disclosure. |
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

Guard: `src/Tests/GolibTests/NativeAddressStabilityTests.cs` — a neutered-fix control across all
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
`src/Tests/GolibTests/ChannelWakeupStrainTests.cs`; they are neutered-fix controls (with `closechan`
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
| `GetIfEntry` | `MibIfRow` (`Name`, `PhysAddr`, `Descr`) | `net.Interfaces` |
| `getStartupInfo` | `StartupInfo` (`Desktop`, `Title`) | ⚠ NOT `os` startup — corrected 2026-08-02 by the r35-os arc, which ran the whole suite without reaching it. Nothing in `os` calls it; in Go 1.23 the only caller is the public `syscall.GetStartupInfo`, exercised by syscall's own test. `Process32First`/`Next` above ARE reached from `os` (`TestGetppid` → `syscall.Getppid` → `getProcessEntry`) and did not fault, so that row is reached-and-working rather than latent. |
| `FreeAddrInfoW` | `AddrinfoW` (`Canonname`, `Next`) | `net` DNS |
| `CertEnumCertificatesInStore`, `CertFreeCertificateChain`, `CertFreeCertificateContext` | `CertContext`, `CertChainContext` | `crypto/x509` on Windows |

**Remedy, per member:** the established one — a blittable `[StructLayout(LayoutKind.Sequential)]`
mirror with `fixed` buffers for the inline arrays, a direct `[DllImport]`, and an explicit
field-for-field copy at the boundary, declared in `manualConversionFuncs` so the generated wrapper
becomes a placeholder. Worked example: `src/core/syscall/zsyscall_windows_impl.cs`.

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
`Tests/Behavioral/ReflectZeroAndGrow`, byte-identical to `go run` across 33 rows. Design:
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
the hand-own mechanics in [`Baseline-vs-FullConversion.md`](../../src/Archived/Baseline-vs-FullConversion.md). Summary of
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
| **`//go:linkname` PUSH never links: `unique.runtime_registerUniqueMapCleanup`** | 1 (+ gob's `TestNetIP`, + `net`'s cctor) | `runtime/mgc.go` PUSHES its body into `unique`'s bodyless declaration. The converter's forwarder handles the **PULL** direction only, so the consuming side is a throwing `PartialStubGenerator` stub. Hit by `unique.Make`'s `setupMake.Do(registerCleanup)`. `sync.Once` marks done through the panic, so later `Make` calls proceed — which is why only one row shows this root |
| **Same class: `internal/weak.runtime_registerWeakPointer` / `runtime_makeStrongFromWeak`** | 4 | `runtime/mheap.go` pushes both. Hit inside `weak.Make`, i.e. `unique.Make`'s `newValue()`. ⚠ Even once linked, `runtime`'s converted bodies walk `mheap_` span metadata that the managed model does not populate (`getWeakHandle` → `spanOfHeap` → `throw("getWeakHandle on invalid pointer")`), so this row wants a `internal/weak` hand-own on managed weak references, not a linkname fix alone |
| **`abi.TypeFor<T>()` is silently WRONG for an INTERFACE `T`** | 1 | `TypeFor`'s interface branch is `TypeOf((*T)(nil)).Elem()`, and `Type.Elem()` for `Kind == Pointer` reinterprets the descriptor as a `PtrType` and reads `.Elem` — which under the managed layout lands on the descriptor's **`Equal` field**. `TypeFor<any>()` and `TypeFor<error>()` return a `System.Func<unsafe.Pointer, unsafe.Pointer, bool>`, not a `ж<abi.Type>`. Shared generics store it into `ConcurrentDictionary<ж<abi.Type>, any>` uncast-checked, and the first key comparison dispatches `IEquatable<ж<abi.Type>>.Equals` on a delegate → `EntryPointNotFoundException`. **Corpus-wide, and it was invisible until now**: the old trie compared raw addresses through `keyEqual` and never dispatched on a key's runtime type. Reflection-bridge row |
| **`GCHandle: Object contains references`** | 1 | `abi.Escape` pinning a managed pointee on the `weak.Make` path |
| **`IndexOutOfRangeException` in `go.slice<T>.Enumerator.get_Current`** | 6 | inside `unique.makeCloneSeq` (`clone.cs`), reached from both `TestHandle` and `TestMakeCloneSeq` — the only root here that is neither linkname nor reflection, and the cheapest next step |

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

<!-- {% endraw %} -->
