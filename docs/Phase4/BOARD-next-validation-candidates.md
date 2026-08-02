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
> **BANKED 2026-08-02 (later): `context` validates — 57 matching + 1 disclosed → roster 72/215 =
> 33.5%.** The reflection chip's increment 5 (reflectlite `rtype.String` over `GoReflect.GoTypeName`)
> closed TestValues, and the banking commit filed TestAllocs' signature-pinned
> `alloc-count-semantics` disclosure (wants 3/12/5/8 mallocs; a byte-derived shim measured
> 128–4,876 bytes). First bank since the proof-page renderer landed — `current/context.md`
> generated itself on the validating run.
>
> **Revised again 2026-08-02 (r35-os)**: `os` gets its own section below — it builds with 0 errors and
> reaches **158 of 178 top-level tests matching + 1 disclosed**, up from 48 at the start of the arc.
> Two converter roots and one host-killer closed; the residual is rooted row by row, and the largest
> single item (12 unreached) is heap corruption whose crash SITE moves between runs, not a defect at
> any of the three sites it has been credited to. `os` does not bank and the roster is unchanged at 71.
>
> A note the arc earned: a **first diagnostic is a starting point, not a diagnosis**. `io`'s first
> error is CS0012 and reads as a missing reference; it is not one. Two of the three claims below
> that were stated as "measured" did not survive re-measurement on a freshly built converter.
>
> Re-validate everything after any change here with `./src/run-validated-sweep.ps1` — it reads the
> roster and the expected counts from [`ValidatedTestPackages.md`](../ValidatedTestPackages.md) and
> fails on a count mismatch, so a package that still passes but asserts something different is
> caught rather than assumed.

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
| ~~`TestValues`~~ **CLOSED 2026-08-02** | `internal/reflectlite`'s `rtype.String()` was the literal Go conversion — `t.nameOff(t.Str).Name()` over a type-descriptor name offset the managed bridge never populates — so it returned `""`. Symptom: `context.Background.WithValue(, c1k1)` where Go prints `WithValue(context_test.key1, c1k1)` — the `stringify` fallback arm for a key with no `String()` method. **Fixed by the reflection-bridge chip**: hand-owned in `internal/reflectlite/type_impl.cs` over `GoReflect.GoTypeName`, the same answer `reflect`'s `rtype.String` gives. `TestValues` passes; **context is 37/38**. | reflection-bridge arc (landed) |
| `TestAllocs` | `testing.AllocsPerRun` unit mismatch, the established `alloc-count-semantics` class (io, strings, bytes). MEASURED before ruling: `Background() allocs = 128.000000 want 0`, `WithValue = 754 want 3`, `WithTimeout(1ns) = 3744 want 12`, `WithCancel = 2104 want 5`, `WithTimeout(5ms) = 4876 want 8` — bytes in every case, so no allocation behavior can satisfy a count assert. A signature-pinned disclosure is warranted; it is deliberately NOT written here, since a disclosure manifest belongs with the banking commit that verifies it end to end. | context's banking arc |

**Update 2026-08-02 — the reflectlite member landed.** The chip hand-owned `rtype.String`, so
`TestValues` passes and `context` is **37 of 38**, now **one disclosure away from banking** with
nothing else outstanding: the sole remaining mismatch is `TestAllocs`, whose measured
bytes-vs-count divergence is recorded above and whose signature-pinned manifest belongs to the
banking commit that verifies it end to end. The gap was never context-specific — any package whose
code path reached `reflectlite.TypeOf(x).String()` got an empty string, silently; `context` is
simply where a differential finally asserted on one. **The next gap of that exact shape is
`rtype.Name`**, which still answers `""` for every type (it gates on the `TFlagNamed` bit a
synthesized descriptor never sets); it is deliberately left until a consumer demonstrates it.

## Build-blocked, each its own root

| Package | First diagnostic | Note |
|:--|:--|:--|
| ~~`image/jpeg`~~ | ~~`CS0111: … already defines a member called 'init'`~~ | **DONE 2026-07-31 — 14/14, banked. NO converter change was needed** — the diagnostic was stale by the time the row was written. The converter has always uniquified multiple package `init`s from a package-scoped counter (`init`, `initΔ1`, … in `visitFuncDecl.go`), and jpeg's production pair (`reader.go` + `writer.go`) emits correctly. The collision was between PRODUCTION's `init` and INTERNAL test file `dct_test.go`'s, which the recompile model put in the same `jpeg_package`; the **whitebox-reference** model emits internal test declarations into `<pkg>_internal_test_package`, so it cannot form. A corpus scan finds **12** packages with both a production and a test `init` (`flag`, `net`, `os`, `runtime`, `sync`, `testing`, `time`, `crypto/x509`, `image/jpeg`, `net/http`, `os/signal`, `os/user`); every one takes a reference model. The recompile FALLBACK (`recordsRequireProductionMutation`) would still collide — latent, reachable by no package today, deliberately not fixed speculatively. Cross-file multi-`init` is now guarded by the `MultiFileInitOrder` behavioral test (five inits across three files, order-compared vs `go run`); `Solitaire` already covered two in one file. |
| ~~`index/suffixarray`~~ | ~~`CS0206: A non ref-returning property or indexer may not be used as an out or ref value`~~ | **DONE 2026-07-31 — 12/12, banked.** TWO go2cs-gen defects, stacked, both general. `suffixarray_test.go` declares `type index Index` — a defined type over the production struct — and Go gives it `Index`'s field set. (1) `GetStructDeclaration` resolves an underlying struct only from SOURCE, and a real MSBuild `<ProjectReference>` arrives as compiled METADATA, so under the white-box model NO members were forwarded and every `x.sa`/`x.data` was CS1061; a symbol-based fallback now resolves it, forwarding what `IsSymbolAccessibleWithin` permits — Go's exported/unexported rule projected into C#. (2) The forward was a get/set property, i.e. a VALUE, so `x.sa.len()` (a `this ref` receiver) and `&x.sa` could not bind — this row's original CS0206. It is now an `[UnscopedRef]` REF-returning property, a strict superset. Fixing (1) alone collapsed the CS1061 wall onto exactly the CS0206 recorded here: root-cause layering, the first diagnostic moving rather than clearing. Full rule: `docs/ConversionStrategies-Reference.md`, *The forwarded member must be a VARIABLE, and the underlying may be METADATA-ONLY*; guarded by the `DefinedTypeOverForeignStruct` behavioral test (whose A/B reproduces CS1061 and CS0206 separately). ⚠ `TestNew{32,64}/exhaustive3` run ~35 min in C# vs 12.4 s in Go — a performance gap, not a correctness one; `run-validated-sweep.ps1` gives the package a 60m deadline. |
| ~~`internal/zstd`~~ | ~~`CS1929: 'testing_package.B' … 'Cleanup'`~~ | **DONE 2026-07-27 — 534/534, banked.** The `common` members are on `core/testing`'s `B`; see the retraction below. |
| ~~`crypto/md5`~~ | ~~`CS0030: Cannot convert type 'System.Type' to 'uint'`~~ | **DONE 2026-07-31 — 11/11 (1 alloc-profile disclosure), banked.** TWO defects, both general. `unsafe.Alignof`/`Offsetof` built their `System.Type` argument by splitting the CONVERTED C# text on `.` as though it were a Go field selector, so `unsafe.Alignof(uint32(0))` emitted `(uint32)0.GetType()` — which C# parses as `(uint32)(0.GetType())`. Both now resolve the operand through `go/types` and emit `typeof(T)`. Behind it stood a second: `buf := buf` in `benchmarkSize` reads a package-level `buf` declared in `md5_test.go`, and the shadowed-global qualifier named the PRODUCTION class (`md5_package.buf`, CS0117) rather than the white-box bridge class that actually declares it. |
| ~~`path/filepath`~~ | ~~`CS0103: The name 'ßÅælstat' does not exist`~~ | **Build blocker CLOSED 2026-07-31; `FindFirstFile` host-killer CLOSED 2026-08-01; BANKED 2026-08-01 (r32 train) at 61 matching — see below.** |
| ~~`net`~~ | ~~`CS1031: Type expected`~~ | **Syntax cascade CLOSED 2026-07-31 — see below. Still does not compile: 94 SEMANTIC errors stood behind it.** |
| `encoding/gob` | `CS0246: The type or namespace name 'Pythagoras' could not be found` (`package_info_internal_test.cs:22`) | **NEW 2026-08-02, measured by the reflection chip; NOT reflection surface — test-project-model record anchoring.** gob declares **two different `Point` types in two different test variants**: `codec_test.go:1105` in `package gob` (internal) and `example_interface_test.go:19` in `package gob_test` (external, with `Hypotenuse()` — the one that actually implements the external `Pythagoras`). The recorder took the EXTERNAL variant's (Point → Pythagoras) relationship but anchored `Point` to the INTERNAL variant on the bare simple name, then emitted the record into `package_info_internal_test.cs` — where `Pythagoras` is not in scope at all, hence unqualified and unresolvable. Same FAMILY as increment 4's whitebox adapter-pair fix (a bare cast resolving to the first same-simple-name FOREIGN record, `bytes_BufferжReader` for io_test's own `Buffer`) and the `net` CS0426 row (`splitExternalVariantRecords`): a same-simple-name collision across test variants resolving to the wrong anchor. **Consequence is total** — the test host does not build, so all 106 verdicts come back EMPTY, which reads like a mass failure rather than a build blocker. This supersedes the "gob runs its Encoder/Decoder engines end-to-end / 79 of 98" state recorded for increment 3a; that measurement predates the blocker. Rooting it belongs to the test-project-model arc, not the reflection chip. |

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

## `time` — builds and RUNS (2026-08-02, r36): **145 pass / 11 fail / 2 skip / 1 infra-error of 159**

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

### r36 (2026-08-02) — **145 pass / 11 fail / 2 skip / 1 infra of 159**

The 17-failure table below has been worked. **Six verdicts converted** (139 → 145 pass), and the two
diagnoses it recorded were both wrong in an instructive way — recorded here rather than deleted,
because the way they were wrong is the reusable lesson.

**The "shared `HandleFinally` NRE" was not a failure mode at all — it was the ABSENCE of a traceback.**
`GoFunc.cs:190` is `throw CapturedPanic.Value;`, the *re-raise* site; re-raising a stored instance
resets `Exception.StackTrace`, so every panic in the corpus named that one frame no matter where it
faulted. The guess that followed from it — "formats a `Time` through `fmt` on its FAILURE path, so the
reflect/fmt bridge" — was reasonable and wrong: `TestDefaultLoc` formats nothing but a `string`.
Two golib changes made the trace honest (`PanicException.StackTrace` prefers the recorded origin;
`RuntimeErrorPanic.TryAsPanic` snapshots it at the adoption point, which is the only place a panic that
passes through **no** `GoFunc` — a function that never defers — can be caught in time). Five of the
nine had been printing `panic: …` and *nothing else* for exactly that reason. With the trace visible the
root was one frame deep and had nothing to do with `fmt`: `Location.lookup`'s entry alias deref'd a nil
`*Location` before `l = l.get()` could normalize it — Go's nil-receiver **normalization** idiom, now a
converter fix (see *A receiver RE-POINTED before first use…* in the strategies reference).

| Was | Now |
|:--|:--|
| `TestDefaultLoc`, `TestNanosecondsToUTC`, `TestSecondsToUTC`, `TestParse`, `TestTimeGob`, `TestTimeIsDST`, `TestZoneBounds` | **PASS** — the `Location.lookup` entry-alias fix |
| `TestParseErrors` | **PASS** — a `break` in a case that also `fallthrough`s fell through anyway, so RFC3339's `Z07:00` arm consumed the `Z` and then re-entered the numeric-offset arm. Converter fix; guard `SwitchBreakBeforeFallthrough` |
| `TestUnmarshalInvalidTimes` | still fails, but its `UnmarshalText` rows went with the same fix — only `UnmarshalJSON` rows remain |

**The `asynctimerchan=1` probe, answered.** Both halves of the question were live, and they split:

- **`t.Setenv("GODEBUG", …)` did NOT reach the converted `godebug`** — the hand-owned package served
  every lookup from a `Lazy` one-shot snapshot, so once *any* setting had been read (in a test binary,
  almost always before the test that sets one runs) later changes were invisible. That contradicted the
  contract in its own `Value` doc comment. Fixed: the snapshot is keyed on the raw `$GODEBUG` text and
  reparsed when it changes. **A/B-proven on `internal/godebug`'s own suite** — `TestGet` (twelve
  `t.Setenv` round-trips) fails before and passes after. This un-gates GODEBUG-dependent tests
  corpus-wide; no validated package sets GODEBUG, so nothing banked moves.
- **It does not un-gate `TestChan`,** because converted `time` ignores `asynctimerchan` by
  construction (`tick.cs` never calls `syncTimer`; `time_impl.cs` is unconditionally async). The
  divergence note is right about that.
- **But the divergence does not explain the remaining rows either, and the split says where to look:**
  the **Timer** half fails only in mode 0 — exactly the documented sync-mode divergence, accepted — while
  the **Ticker** half fails in *all three* modes (`0`, `1`, `2`) with `extra tick` / `early done`. An
  async-mode ticker failure is outside the documented divergence. **Rooted:** `serviceTimers` re-peeks
  a periodic timer against a freshly-read `now` immediately after re-arming it, so when the period is
  shorter than the loop's own iteration time — `testTimerChan` Resets to **1ns** — the advanced `when`
  is already in the past and the same timer fires again, and again, for the length of the pass. The
  burst is invisible while nobody receives (the cap-1 channel drops all but one), but `drainAsync` *is*
  receiving: each `drain1` frees the slot and the still-draining burst refills it, so the two stale
  values an async ticker is allowed become three. Go bounds this by returning to the scheduler rather
  than re-firing one timer within a pass; the faithful fix is **fire each timer at most once per pass**
  (which is also what "drop ticks to make up for slow receivers" means). Not taken in r36's tail — it
  changes the heart of the timer model and wants its own re-gate. Recorded in `time_impl.cs`. That is
  the live `time` row now: a ticker-model bug, not a channel-rendezvous or GODEBUG one.

**`TestUnmarshalTextAllocations` — characterized, and it is NOT the disclosure class the table below
assumed.** `AllocsPerRun`'s bytes-for-mallocs mapping is exact at zero, so a *want-zero* assert is
faithfully representable and no unit mismatch is in play; the C# path genuinely allocates. Measured:
`Time.UnmarshalText` = **3664 bytes/run**, essentially all of it in `parseRFC3339`, and all of it in
shared machinery that could stop allocating — `IByteSeq<T>`'s range indexer returns the *interface*, so
each `s[a:b]` on a `string | []byte`-constrained value boxes (48 B), `[]byte(s)` boxes again (48 B), and
`for i, c := range s` over a `slice<T>` allocates a **fixed 136 B** enumerator per loop where the indexed
form allocates zero. Full breakdown in the strategies reference (*a third class … ELIMINABLE
inefficiency*). **Performance work, not a disclosure** — and the 136-byte range enumerator is a
corpus-wide finding worth its own item.

**Still open (11 fail + 1 infra):**

| Count | Tests | Root | Owner |
|--:|:--|:--|:--|
| 8 | `TestChan` and its seven subtests | Timer half = the documented sync-mode divergence (accepted). Ticker half fails in **async** modes too → a real `time_impl.cs` ticker-model bug, per the probe above. | time |
| 2 | `TestTimeJSON`, `TestUnmarshalInvalidTimes` | Now that the traceback is honest these read plainly, and every remaining line is the same one: `json: cannot unmarshal string into Go value of type time.Time`. `encoding/json` is not dispatching to `Time`'s `UnmarshalJSON` — the reflection bridge's method-invocation half (Tier-0 #2, Phase 3), not `time`. | reflect bridge chip |
| 1 | `TestTruncateRound` | `math/big.mulAddVWW` is an unimplemented asm stub (`NotImplementedException`), reached through `big.Int.Mul`. | math/big arc |
| 1 | `TestUnmarshalTextAllocations` | Eliminable allocation in the union-constraint emission + range enumerator, characterized above. | performance |

So `time`'s distance is now: **one `time`-local ticker-model bug**, two verdicts waiting on the
reflection bridge, and two rows owned elsewhere.

---

<details>
<summary>The r35 table this supersedes (kept for the diagnoses, both of which were wrong)</summary>

**The 17 remaining failures, rooted, none of them `time`-local machinery:**

| Count | Tests | Root | Owner |
|--:|:--|:--|:--|
| 6 | `TestChan` and its five subtests | **Documented model divergence, not a defect.** Go 1.23 made a chan-based Timer/Ticker channel SYNCHRONOUS (#37196) by coupling the channel's receive path to the timer inside the runtime; `time_impl.cs` reproduces Go's own `GODEBUG=asynctimerchan=1` mode instead, so `tim.Stop() = false, want true` and "extra tick" are exactly what that mode produces. ⚠ The `asynctimerchan=1` SUBTEST also fails, which the divergence does NOT explain — either `t.Setenv("GODEBUG", …)` does not reach the converted `godebug`, or the async model has its own bug. That subtest is the honest next probe here. | time / godebug |
| 9 | `TestDefaultLoc`, `TestNanosecondsToUTC`, `TestSecondsToUTC`, `TestParse`, `TestTimeGob`, `TestTimeIsDST`, `TestTimeJSON`, `TestUnmarshalInvalidTimes`, `TestZoneBounds` | All die with the same `nil pointer dereference` inside `GoFunc.HandleFinally`. Every one of them formats a `Time` through `fmt` on its FAILURE path (`%#v`, `%+v`, `%v` of a struct with a `*Location`), so the NRE is plausibly SECONDARY to a comparison that already failed — the reflect/fmt bridge, not the clock. Not rooted; the next increment should print the pre-format comparison rather than reason about the stack. | reflect/fmt bridge |
| 1 | `TestParseErrors` | A REAL parse divergence: Go reports `extra text: "07:00"` where C# reports `cannot parse "Z07:00" as "Z07:00"` — the `Z07:00` layout element consumes differently. `format.go` conversion defect, `time`-local. | time |
| 1 | `TestTruncateRound` | `math/big.mulAddVWW` is an unimplemented asm stub (`NotImplementedException`), reached through `big.Int.Mul`. | math/big arc |
| 1 | `TestUnmarshalTextAllocations` | `got 3784 allocs, want 0` — the established **alloc-count-semantics** unit mismatch (`AllocsPerRun` counts mallocs in Go, BYTES on the CLR). A disclosure candidate by the class `strings`/`io` already established; **not self-ruled here**. | ruling |

So `time`'s distance is: one `time`-local parse bug, one probe (`asynctimerchan=1`), one shared
reflect/fmt-bridge NRE family worth 9 verdicts, and two rows owned elsewhere. Nothing about timers,
sleeps, tickers or channel rendezvous is in the way.

</details>

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

> **⚠ Correction 2026-08-02 (reflection chip, ground-truthed against the code).** The
> "`GoReflect.KindOf`/`ElementType` still report the ADAPTER class" half of that follow-up **is
> stale and has been since 2026-07-24**: the unwrap landed in `94b5a1790` ("descriptors classify
> the Go DYNAMIC type through adapters (R10)"), which is increment 1's own commit —
> `KindOf` unwraps BOTH adapter shapes (`GoReflect.cs:129`, pointer-sourced → `Pointer`,
> value-sourced → the wrapped type's kind) and `ElementType` unwraps the pointer-sourced one
> (`:340`); `KindOf`'s own doc comment states it. Nothing here needs re-doing. This is the §9
> ground-truth-the-code trap that cost the channels arc nine days — the row outlived its fix.
>
> **What genuinely remains is narrower and is a CONSISTENCY defect, not a missing feature:**
> `ElementType` does not unwrap a **value-sourced** ᴠ-adapter, so for a ᴠ-adapter over a named
> CONTAINER type — `color_PaletteᴠModel` is exactly one, `color.Palette` being `[]Color` — `KindOf`
> answers `Slice` while `ElementType` answers `null`. The two are documented to mirror each other
> and there they disagree: a Slice-kind type whose `Elem()` is invalid. It is latent rather than
> live because descriptors never carry an adapter class in the first place — `abi.TypeOf` routes
> through `GoDynamicTypeOf`, which unwraps before `synthType` — so no `reflect.Type.Elem()` reached
> via `TypeOf` can hit it. Recorded, not fixed in the `rtype.String` increment: it is a separate
> change with its own gate, and the honest guard for it is a differential over a ᴠ-adapter-held
> named slice, not an assertion written from this paragraph.

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

## `os` — 159 of 178 match + 1 disclosed; the moving-crash row RETRACTED (r35-os → r36-pin, 2026-08-02)

Measured with `go2cs -tests -test-action all -test-timeout 15m "<GOROOT>/src/os" src/core/os`.
`os` builds with **0 errors**. Progression across the r35 arc, all from one pipeline command:
**48 agreeing → 141 → 158**, the first jump from the build blockers, the second from the
`readReparseLink` host-killer.

| | Go | C# — r35 (`620a935bf`) | C# — r36 (`af5df9e16`) |
|:--|--:|--:|--:|
| top-level tests | 178 (143 pass · 34 skip · 1 fail) | 166 reached (123 pass · 34 skip · 8 fail · 1 infra-error) | 174–176 reached (124–126 pass · 34 skip · 13–14 fail · 2–3 infra-error) |
| **agreeing** | | **158** | **159–160** |
| disclosed | | 1 (`TestUTF16Alloc`, alloc-count-semantics) | 1 (same) |
| real mismatches | | 7 | 14 |
| unreached | | 12 (host DIED) | 2–4 (host HANGS on the pipe family; the rest all answer) |

⚠ **Read the two columns as a VISIBILITY difference, not a regression.** The crash was hiding
failures, not successes: nine to eleven of the twelve tests the r35 host never reached now produce an
answer, and almost all of them FAIL. Nothing that agreed stopped agreeing. The r36 column is a range
because it rests on three COMPLETE runs at this base — one pre-fix control (175 reached, 160 agreeing)
and two post-fix (174/159 and 176/**160**) — and the pipe family is racy: the only rows that move
between them are `TestCloseWithBlockingReadByFd` and `TestCloseWithBlockingReadByNewFile`, which pass
when they win their race and hang when they do not. Post-fix therefore brackets the control from both
sides, which is the honest statement of the pin's effect on this package: **none measurable**.

⚠ **Measuring `os` while another lane measures `os` does not work, and the failure looks like data.**
Two further post-fix runs were cut short after roughly one minute each — 21 and 59 tests left with no
verdict, a truncated result stream, and no diagnostic. They were not crashes: `go2cs_test_results.json`
still carried the PREVIOUS run's timestamp, so those hosts died before writing it, while two sibling
worktrees were running their own `os.tests.exe`. That is charter §9's kill-by-name hazard
(`Get-Process os.tests | Stop-Process` matches across the whole machine), and unlike `BehavioralRunner`
the pipeline names the host itself, so the rename defence is not available. **Check
`go2cs_test_results.json`'s mtime before believing a short `os` run**, and do not schedule two lanes on
one package.

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

### The residual, every row rooted

| Row | Cost | Root |
|:--|--:|:--|
| ~~**host-killer: an ExecutionEngineException whose SITE MOVES between runs**~~ | ~~12–29~~ | **RETRACTED 2026-08-02 (r36-pin) — it does not reproduce at `af5df9e16`, and `createMountPoint` was not shown to be it.** A PRE-FIX control run of the whole suite (golib stashed back to base, `golib.dll` rebuilt and verified to lack the fix) produced **zero** `ExecutionEngineException`/`AccessViolation`, reached 175 of 178 top-level tests, and agreed on 160 — i.e. the crash the row is about did not fire at all, and the "12 unreached" figure does not hold at this commit. Two complete post-fix runs bracket it (159 and 160 agreeing), and neither of the two truncated post-fix runs (see the kill-by-name note above) shows a fault either: **zero** `ExecutionEngineException` across all five. Whatever closed the crash closed it between `620a935bf` and `af5df9e16`, in some other lane's work, not here. ⚠ The row's *diagnosis* was nonetheless a real defect and IS fixed: golib's non-representable reinterpret fallback returned a **transient** pinned address, so writes through the derived pointer landed wherever the storage had moved to. That is now pinned for the derived box's lifetime (`ж<T>.TryPinnedReinterpret`), reproduced 5-of-5 and fixed 8-of-8 by `Tests/Behavioral/ReinterpretPinLifetime`, and written up in [`ConversionStrategies-Reference.md`](../ConversionStrategies-Reference.md), *An address of MANAGED storage that outlives its statement must carry a PIN*. What it did NOT buy was any measurable change in this package — the pin is a correctness fix looking for the consumer that will need it, not the explanation for a crash that had already stopped happening. |
| **the host HANGS instead of exiting: the blocking-pipe family** | 2–4 unreached + 6 fails | The current top row, and the one worth the next arc. `TestPipeEOF` and `TestPipeIOCloseRace` (plus, when they lose their race, `TestCloseWithBlockingReadByFd` and `TestCloseWithBlockingReadByNewFile`) start and never finish — a blocking read is not interrupted by the `Close` that is supposed to unblock it, so the host reports the run and then never exits (each pipeline invocation leaves an `os.tests.exe` behind that has to be killed by PID). The SAME root shows up as six ordinary failures, all with an EMPTY child-process output where Go reads a value: `TestHostname` (`!= system hostname of ""`), `TestExecutable` (`Child returned "[]"`), `TestGetppid` (`reports parent process id ''`), `TestStatStdin`, `TestStartProcess` (both arms), `TestRootDirAsTemp` (`exit status 2`). Those tests all read a child's stdout through `os.Pipe`. Present identically in the pre-fix control, so it is neither new nor caused by the pin fix; `TestPipeCloseRace`, `TestReadClosed`, `TestDoubleCloseError` and `TestClosedPipeRace*` all pass, so the failure is specific to *unblocking a read in progress* — `internal/poll` on Windows, not `os`. |
| `TestDirectoryJunction` | 1 | The `createMountPoint` reinterpret, surfacing as a contained `IndexOutOfRangeException` at `&buf.PathBuffer[0]` (`ж<T>.at`). Raw metal on a non-native type, in TEST code that cannot be hand-owned — no converter or golib change can lay a managed array reference over inline OS bytes, and the pin above does not help: it makes the bytes read at that offset the buffer's REAL bytes rather than recycled memory, but a fabricated object reference is a CLR type-safety break either way. |
| `TestCmdArgs` | 1 | Same class, different API: `syscall.CommandLineToArgv` returns a native `**[8192]*[8192]uint16`, and `(*argv)[:argc]` reads an `array<ж<array<ushort>>>` — three managed references — straight out of OS memory. Manifests as whichever garbage it read: an `ArgumentException` from `array<T>.Slice` in one run, a plain assertion failure in another. |
| `TestReadStdin` (462 subtests) | 1 | The test's `poll.ReadConsole` fake writes into internal/poll's buffer through `(*[10000]uint16)(unsafe.Pointer(buf))`; the write lands nowhere, so every read returns zeros (`have [0 0 0…] want [abc…]`). Same class, and the general remedy is real: `(*[N]T)(unsafe.Pointer(p))` over a managed ELEMENT box should alias the backing from that element — the `array<T>.Alias` window the `SliceToArrayPointerAlias` fix already built for the slice→array-pointer form. |
| `TestNilFileMethods` | 1 | A pointer receiver's entry preamble `ref var f = ref Ꮡf.Value;` derefs at ENTRY; Go derefs only where the body uses the pointee. `os` guards by DELEGATING (`f.checkValid("chdir")` is what asks `f == nil`), so all 15 methods panic before their guard runs. `collectNilSafePtrParams` covers only the guard spelled INLINE. **Widening it to every direct-ж receiver was implemented and MEASURED: 26 behavioral projects change their preamble to `.DerefOrNil()`** — a corpus-wide emission change that also widens the arm's accepted trade-off (an unguarded deref reads `default(T)` instead of panicking) from "a receiver the body nil-tests" to every receiver. Reverted and left as a ruling with its footprint attached; the alternative is a golib accessor that binds a NULL ref so the panic moves to first use instead of vanishing. |
| `TestReadDir` | 1 | Environment fidelity, not conversion: the host runs each package in an isolated directory holding the staged `*.go` fixtures, so `ReadDir(".")` finds `read_test.go` but not the `exec` SUBDIRECTORY that Go's own package directory has. `os.ReadDir` itself is correct — probed against `go run` over the same 201-entry directory, byte-identical including `IsDir()`. Remedy: stage the GOROOT package directory's immediate subdirectory NAMES into the run root. Cheap, but it changes EVERY package's run environment, so it wants the full validated sweep as its gate. |
| `TestWriteStringAlloc` | 1 | `AllocsPerRun` bounded at ZERO. Deliberately **not** disclosed: the byte-derived shim CAN report 0, so the io/strings unit-mismatch ruling does not cover it. Go's `WriteString` avoids the copy with `unsafe.Slice` over the string's own bytes; a go2cs `@string` is its own storage, so the write path allocates (measured 9088 bytes). A real divergence — an `sstring`-shaped optimization, not a disclosure. |
| `TestRemoveAllWithExecutedProcess` | 1 | Child `exec failed: exit status 0x8000809a`, repeated. Not rooted. Spawning itself works — `TestKillStartProcess` and `TestKillFindProcess` pass, and they are the two child tests that never read the child's OUTPUT. |
| ~~`TestStartProcess/relative`~~ → `TestStartProcess`, both arms | 1 | Corrected 2026-08-02: the absolute arm fails too, and for the same reason as the row above it — `exec … returned ""`, an empty child stdout. Folded into the blocking-pipe row; it is not a path-resolution defect. |
| `TestStatLxSymLink` | intermittent | `t.TempDir()` cleanup hit a file "used by another process" — a handle the host had not released yet. Reproduced in both complete runs at `af5df9e16`, so it is a live flake rather than a one-off. |

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

## Open — the syscall STRUCT-PASSING seam: 8 wrappers still hand a non-blittable struct to the kernel

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
| `Process32First` / `Process32Next` | `ProcessEntry32` (`ExeFile`) | process enumeration |
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
