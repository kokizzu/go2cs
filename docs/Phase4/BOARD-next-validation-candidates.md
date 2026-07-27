# BOARD — next validation candidates, each rooted

> Measured 2026-07-27 by running the `-tests` pipeline over every unbanked candidate the
> shared-fixture fix structurally unblocked, plus the packages a prior scout left build-blocked.
> Every entry carries the **first and most informative diagnostic**, so the next arc starts from a
> root cause rather than an exploration. Corpus state at measurement: 60 validated / 215 (27.9%).
>
> Re-validate everything after any change here with `./src/run-validated-sweep.ps1` — it reads the
> roster and the expected counts from [`ValidatedTestPackages.md`](../ValidatedTestPackages.md) and
> fails on a count mismatch, so a package that still passes but asserts something different is
> caught rather than assumed.

## The `-tests` reference-closure family — the biggest cluster

`DisableTransitiveProjectReferences=true` means the generated test project lists only the imports
the converter computed, so any package named by a type the test code merely *touches* is missing
and the build fails with **CS0012**. `crypto/hmac` was the first case solved: an interface that
**embeds** another package's interface (`hash.Hash` embeds `io.Writer`), fixed by closing over the
interface types the compilation actually names. Two sub-cases remain, and neither is reached by
that rule:

| Package | Missing type | Why the interface rule misses it |
|:--|:--|:--|
| `io` | `io_package.Writer` | the package under test is itself the owner — a self-reference shape |
| `image/draw` | `rand_package.Rand` | a **struct**, not an interface, so no interface closure names it |

Generalizing the closure to every package named by a type the test compilation references — not
only interfaces — is the single highest-yield item on this board.

## Build-blocked, each its own root

| Package | First diagnostic | Note |
|:--|:--|:--|
| `image/jpeg` | `CS0111: Type 'jpeg_package' already defines a member called 'init'` | two `init` functions merged into one class |
| `index/suffixarray` | `CS0206: A non ref-returning property or indexer may not be used as an out or ref value` | an indexer passed by `ref`/`out` |
| `internal/zstd` | `CS1929: 'testing_package.B' does not contain a definition for 'Cleanup'` | **Measured: this one is worth 534 verdicts, and the obvious fix backfires — read below.** |
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

## Recurring classes worth a general fix rather than another point repair

- **Zero-value construction for a type that needs one.** Fixed three times now in three different
  emission paths: a heap-boxed local fixed array, `new([N]T)` dropping its length, and
  `make([]S, n)` where `S` carries a fixed-array field. One documented residue remains — a
  `default!` zero-var local. Every *new* emission path re-opens this class, which argues for
  centralizing zero-value construction instead of patching sites.
- **Untyped constants in a typed slot.** The int-literal case is fixed; the constant-**ident** case
  (`maphash` above) is not.

## Measured trap — `internal/zstd`, `testing.B`, and the reference closure

`internal/zstd` is worth **534 verdicts**, the largest unbanked package measured here, and the fix
looks trivial: Go's `B` and `T` both embed `common`, so a benchmark may call `Cleanup`, `Error`,
`Log`, `Name`, `TempDir` and the rest, while `core/testing`'s compile-only `B` surface declares
almost none of them. Adding them **does** make `internal/zstd` validate at 534/534 — measured, not
predicted.

**It also breaks `crypto/hmac`, and the mechanism is worth understanding before anyone tries
again.** `core/testing` declares a `TB` interface holding exactly that member set. Completing `B`'s
surface makes `B` structurally implement `TB`, which changes the set of interface types the
compilation names — and the `-tests` reference closure is computed from precisely that set. The
`io` project reference `crypto/hmac` needs then disappears from its generated `.tests.csproj`, and
the package stops building.

Verified both directions: with the `B` members added, a regenerated `crypto/hmac` loses
`<ProjectReference … io.csproj />` and fails `CS0012`; with them reverted, the reference survives
and the package validates. So the two are coupled, and the ordering is fixed:

**Generalize the reference closure first** (the top item on this board — it must cover packages
named by *any* referenced type, not only by interfaces, which is also what `io` and `image/draw`
need). Once the closure no longer depends on which interfaces happen to be implemented, completing
`B`'s `common` surface is a safe, one-file change worth 534 verdicts.

A second, smaller lesson from the same experiment: the fix to `crypto/hmac`'s closure is **not
reproducible from a standalone regeneration** — re-running its pipeline alone drops the reference
even on unmodified sources. The committed artifacts are correct and the package validates as
banked, but a future whole-corpus regeneration would lose it. That fragility is a symptom of the
same root: the closure depends on state that a single-package run does not reconstruct.
