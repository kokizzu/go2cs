# BOARD — next validation candidates, each rooted

> Measured 2026-07-27 by running the `-tests` pipeline over every unbanked candidate the
> shared-fixture fix structurally unblocked, plus the packages a prior scout left build-blocked.
> Every entry carries the **first and most informative diagnostic**, so the next arc starts from a
> root cause rather than an exploration. **Revised 2026-07-27 (later)** by the reference-closure
> arc: the closure family is closed, `internal/zstd` is banked, and two claims in the original
> revision are retracted as measurement errors — see the sections below. Corpus state after that
> arc, plus the 2026-07-29 `hash/maphash` bank and the 2026-07-31 `image/draw`, `image/gif`,
> `crypto/md5`, `compress/flate`, `image/jpeg` and `index/suffixarray` banks:
> **68 validated / 215 (31.6%)**.
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

Fresh `io` conversion now emits `testProjectModel: whitebox-reference`, references `io.csproj`, compiles no production `.cs` into `io.tests`, and builds with **0 errors**. The host runs all **54** included test functions: **45 pass**, proving the former CS0012/CS1503 wall is gone. `io` is not banked or counted as validated yet; the remaining nine top-level verdicts are separate runtime/semantic roots:

- `TestMultiReaderFlatten` and `TestMultiWriterSingleChainFlatten`: `runtime.getcallersp` is unimplemented — owned by the charter's reflection Phase-3 chip.
- `TestOffsetWriter_Seek`, `TestOffsetWriter_WriteAt`, `TestWriteAt_PositionPriorToBase`, plus `TestOffsetWriter_Write` subtests: `os.runtime_rand` is unimplemented in the tempfile path — owned by the `os` operational arc.
- `TestMultiWriter_StringCheckCall`: `WriteString` forwarding behavior mismatch (separate runtime/conversion investigation).
- `TestMultiWriter_WriteStringSingleAlloc` and `TestPipeAllocations`: exact allocation-profile assertions; no disclosure ruling has been made.

This board item is complete at its stated architectural boundary — the **45 / 54** split above is
the whole of what remains, and every one of the nine has a named owner. They must be handled by
those arcs rather than folded into the test-project-model change.

## Build-blocked, each its own root

| Package | First diagnostic | Note |
|:--|:--|:--|
| ~~`image/jpeg`~~ | ~~`CS0111: … already defines a member called 'init'`~~ | **DONE 2026-07-31 — 14/14, banked. NO converter change was needed** — the diagnostic was stale by the time the row was written. The converter has always uniquified multiple package `init`s from a package-scoped counter (`init`, `initΔ1`, … in `visitFuncDecl.go`), and jpeg's production pair (`reader.go` + `writer.go`) emits correctly. The collision was between PRODUCTION's `init` and INTERNAL test file `dct_test.go`'s, which the recompile model put in the same `jpeg_package`; the **whitebox-reference** model emits internal test declarations into `<pkg>_internal_test_package`, so it cannot form. A corpus scan finds **12** packages with both a production and a test `init` (`flag`, `net`, `os`, `runtime`, `sync`, `testing`, `time`, `crypto/x509`, `image/jpeg`, `net/http`, `os/signal`, `os/user`); every one takes a reference model. The recompile FALLBACK (`recordsRequireProductionMutation`) would still collide — latent, reachable by no package today, deliberately not fixed speculatively. Cross-file multi-`init` is now guarded by the `MultiFileInitOrder` behavioral test (five inits across three files, order-compared vs `go run`); `Solitaire` already covered two in one file. |
| ~~`index/suffixarray`~~ | ~~`CS0206: A non ref-returning property or indexer may not be used as an out or ref value`~~ | **DONE 2026-07-31 — 12/12, banked.** TWO go2cs-gen defects, stacked, both general. `suffixarray_test.go` declares `type index Index` — a defined type over the production struct — and Go gives it `Index`'s field set. (1) `GetStructDeclaration` resolves an underlying struct only from SOURCE, and a real MSBuild `<ProjectReference>` arrives as compiled METADATA, so under the white-box model NO members were forwarded and every `x.sa`/`x.data` was CS1061; a symbol-based fallback now resolves it, forwarding what `IsSymbolAccessibleWithin` permits — Go's exported/unexported rule projected into C#. (2) The forward was a get/set property, i.e. a VALUE, so `x.sa.len()` (a `this ref` receiver) and `&x.sa` could not bind — this row's original CS0206. It is now an `[UnscopedRef]` REF-returning property, a strict superset. Fixing (1) alone collapsed the CS1061 wall onto exactly the CS0206 recorded here: root-cause layering, the first diagnostic moving rather than clearing. Full rule: `docs/ConversionStrategies-Reference.md`, *The forwarded member must be a VARIABLE, and the underlying may be METADATA-ONLY*; guarded by the `DefinedTypeOverForeignStruct` behavioral test (whose A/B reproduces CS1061 and CS0206 separately). ⚠ `TestNew{32,64}/exhaustive3` run ~35 min in C# vs 12.4 s in Go — a performance gap, not a correctness one; `run-validated-sweep.ps1` gives the package a 60m deadline. |
| ~~`internal/zstd`~~ | ~~`CS1929: 'testing_package.B' … 'Cleanup'`~~ | **DONE 2026-07-27 — 534/534, banked.** The `common` members are on `core/testing`'s `B`; see the retraction below. |
| ~~`crypto/md5`~~ | ~~`CS0030: Cannot convert type 'System.Type' to 'uint'`~~ | **DONE 2026-07-31 — 11/11 (1 alloc-profile disclosure), banked.** TWO defects, both general. `unsafe.Alignof`/`Offsetof` built their `System.Type` argument by splitting the CONVERTED C# text on `.` as though it were a Go field selector, so `unsafe.Alignof(uint32(0))` emitted `(uint32)0.GetType()` — which C# parses as `(uint32)(0.GetType())`. Both now resolve the operand through `go/types` and emit `typeof(T)`. Behind it stood a second: `buf := buf` in `benchmarkSize` reads a package-level `buf` declared in `md5_test.go`, and the shadowed-global qualifier named the PRODUCTION class (`md5_package.buf`, CS0117) rather than the white-box bridge class that actually declares it. |
| `path/filepath` | `CS0103: The name 'ßÅælstat' does not exist` | a mangled identifier — encoding of a non-ASCII or symbol-marked name |
| `net` | `CS1031: Type expected` | a hard syntax error in emission |

## Runtime failures

| Package | State |
|:--|:--|
| ~~`hash/maphash`~~ | **DONE 2026-07-29 — 22/22, banked.** Computed float constants that directly use a named untyped integer wrapper now materialize once at the destination's float width; `TestSmhasherAvalanche`'s mean is 50000 and the full SMHasher matrix matches Go. |
| ~~`compress/flate`~~ | **DONE 2026-07-31 — 64/64, banked.** `TestWriterReset` was NOT a state difference: `deepValueEqual`'s `Func` arm returned false unconditionally, on the reasoning that two nil funcs would already have matched the `invalid == invalid` rule at the top. That holds only for a nil func boxed as `any`; a nil func reached as a struct FIELD is typed by its static func type and is a VALID nil Value, so the arm declared every pair of nil func fields unequal — and the test nils `fill`/`step`/`bulkHasher`/`bestSpeed` precisely so `DeepEqual` can compare the rest. Go's rule is "equal iff both nil"; the arm now asks it. The tell was that every field compared equal individually while the enclosing struct did not. |
| ~~`image/gif`~~ | **DONE 2026-07-31 — 28/28, banked.** `TestWriter` was the blank-import module-initializer gap and nothing else: with `_ "image/png"`'s `init()` forced, the PNG decoder registers and `image.Decode` reads `../testdata/video-001.png`. No `image/gif` defect existed. |
| `image/png` | probed previously; does not validate |
| ~~`image/draw`~~ | **DONE 2026-07-31 — 9/9, banked.** All four failures were two defects, both fixed at the root. `TestDraw` was the address-taken *value parameter* box-copy: `DrawMask`'s `clip(dst, &r, src, &sp, mask, &mp)` narrows all three in place, and `Ꮡ(r)` boxed a COPY, so the draw loop ran on the unclipped rectangle. (The empty-`Pix` panic above was that same unclipped geometry, not an assertion defect — the guess in this row was wrong.) The other three were value adapters carrying no Go dynamic type, so `image.Image` type switches took the wrong arm. |

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
- **Untyped constants in a typed slot — CLOSED 2026-07-29.** The int-literal case was already fixed;
  a computed float constant that directly uses a named untyped integer wrapper now folds once at the
  resolved float width. `hash/maphash` validates 22/22; `UntypedConstDefine` guards both `:=` and typed slots.
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
