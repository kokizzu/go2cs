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
| `internal/zstd` | `CS1929: 'testing_package.B' does not contain a definition for 'Cleanup'` | `B` lacks a member `T` has — the compile-only benchmark surface again |
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
