# BOARD — next validation candidates, each rooted

> Measured 2026-07-27 by running the `-tests` pipeline over every unbanked candidate the
> shared-fixture fix structurally unblocked, plus the packages a prior scout left build-blocked.
> Every entry carries the **first and most informative diagnostic**, so the next arc starts from a
> root cause rather than an exploration. **Revised 2026-07-27 (later)** by the reference-closure
> arc: the closure family is closed, `internal/zstd` is banked, and two claims in the original
> revision are retracted as measurement errors — see the sections below. Corpus state after that
> arc: **61 validated / 215 (28.4%)**.
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
| `image/draw` | `rand_package.Rand` | **build unblocked** — a `struct` field of `quick.Config` reached at an element-bearing composite literal. Does not yet validate: 4 of its tests fail on a separate runtime defect, below. |
| `io` | `io_package.Writer` | **NOT a closure defect** — see the next section. Adding the reference cannot fix it. |

**Minimality is the hard part, and it is measured, not asserted.** Regenerating all 60 banked
packages' `.tests.csproj` and diffing is the instrument, and it rejected three looser rules before
the landed one. Seeding from *every* file rather than the compiled ones drifted `compress/gzip`
(context, crypto/tls, mime/multipart, net/http, net/url — reached through `http.Request`'s fields,
from a Phase-4D-excluded `example_test.go` that is never compiled) and `go/token` (go/ast); firing
the struct edge on any *value use* drifted eleven more (`sync.Once`, `sync.Map`, `reflect.Value`);
firing it on an *empty* literal still drifted three, because an empty Go literal converts to
`new Δsync.Once(nil)` — go2cs-gen's nil constructor, which names no field. The landed rule drifts
**zero** of the 60.

⚠ **Run that probe with the converter's exit status checked.** A conversion that *fails* writes no
csproj, so an ignored failure reads exactly like "no drift" — a false-clean of the same family as
charter §9's false-green traps. That is how a real defect in the first cut hid through three
measurement rounds: a struct literal declared in the EXTERNAL test variant reached
`reach(<pkg>_test)`, a synthetic path that resolves to no importable package, and every affected
package died with F14b's `resolve test project dependency "bytes_test": package bytes_test is not
in std` — silently, until the validated sweep failed on `bytes` at the second package.

## `io` — a duplicate-type defect of the RECOMPILE model, not a missing reference

`io`'s `export_test.go` is an in-package variant, so the suite takes the recompile model and io's
production `.cs` compile *into* the test assembly. Every referenced assembly (bytes, fmt, strings,
hash, …) already references `io.dll` and names `io_package.Reader`/`Writer` in its own API, so the
test assembly ends up with **two** distinct `io_package.Writer` types. The first diagnostic is
CS0012, which reads like a missing reference — but the reference cannot fix it, and the proof is
already in the same build: `multi_test.cs(179,26): error CS1503: cannot convert from
'go.hash_package.Hash' to 'go.io_package.Writer'`. `hash.Hash` derives from **io.dll's** `Writer`;
the parameter is the **recompiled** one. Referencing io.dll turns the CS0012s into CS0436 warnings
and leaves that conversion just as impossible.

The real fix is to stop recompiling — i.e. extend the reference model ("Change C") to suites that
have an internal variant. The obvious mechanism is `[assembly: InternalsVisibleTo("<pkg>.tests")]`
on the converted production project: Go-unexported identifiers already emit as C# `internal`, so an
in-package test variant could bind them **across** the assembly boundary and the production assembly
would stay the single identity for its types. That is a design decision with corpus-wide blast
radius (private-emitted members, and production-anchored `GoImplement`/`GoImplicitConv` records that
cannot merge across assemblies — today's `recordsRequireProductionAnchor` fallback), so it wants
design-WITH-user before implementation. It would also close the same class for every other
whitebox suite whose dependencies name the package under test.

## Build-blocked, each its own root

| Package | First diagnostic | Note |
|:--|:--|:--|
| `image/jpeg` | `CS0111: Type 'jpeg_package' already defines a member called 'init'` | two `init` functions merged into one class |
| `index/suffixarray` | `CS0206: A non ref-returning property or indexer may not be used as an out or ref value` | an indexer passed by `ref`/`out` |
| ~~`internal/zstd`~~ | ~~`CS1929: 'testing_package.B' … 'Cleanup'`~~ | **DONE 2026-07-27 — 534/534, banked.** The `common` members are on `core/testing`'s `B`; see the retraction below. |
| `crypto/md5` | `CS0030: Cannot convert type 'System.Type' to 'uint'` | uninvestigated |
| `path/filepath` | `CS0103: The name 'ßÅælstat' does not exist` | a mangled identifier — encoding of a non-ASCII or symbol-marked name |
| `net` | `CS1031: Type expected` | a hard syntax error in emission |

## Runtime failures

| Package | State |
|:--|:--|
| `hash/maphash` | **21 of 22.** `TestSmhasherAvalanche` fails on a real defect: `const REP = 100000` emits as `UntypedInt`, so `mean := .5 * REP` evaluates as INTEGER arithmetic — `0.5` truncates and `mean` is 0 instead of 50000, collapsing the test's tolerance window. The hash itself is perfect (every avalanche count ~50000/100000). This is the constant-**ident** analogue of the already-fixed int-literal-in-float-slot class, and its blast radius is corpus-wide. |
| `compress/flate` | **63 of 64.** Only `TestWriterReset`, a whole-`Writer` `reflect.DeepEqual` after `Reset`. Uninvestigated. |
| `image/gif` | package-level failure, no per-test verdict — needs a first look |
| `image/png` | probed previously; does not validate |
| `image/draw` | **builds now** (closure fix). 4 of its tests fail — `TestDraw`, `TestDrawSrcNonpremultiplied`, `TestFloydSteinbergCheckerboard`, `TestPaletted`. `TestDraw`: `panic: runtime error: slice bounds out of range [::4] with capacity 0` at `draw.cs`'s `dst.Pix.slice(i, i+4, i+4)` inside the `mask.(image.RGBA64Image)` branch of `drawRGBA` — the `*image.RGBA` reached through `ᏑDst.Value` has an EMPTY `Pix`, while the sibling branches (`TestDrawOverlap`, `TestFill`) work. Smells like the interface type-assertion/box-copy class, not a draw bug. |

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
- **Untyped constants in a typed slot.** The int-literal case is fixed; the constant-**ident** case
  (`maphash` above) is not.

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
