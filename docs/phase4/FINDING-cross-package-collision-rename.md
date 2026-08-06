# FINDING — a foreign package's Δ-renamed member is only spelled correctly when that package is converted in the SAME run

> Filed 2026-07-24 from the timer arc (sub-agent `claude/r2-timers`), where it blocked the converted
> verification repro.
>
> **Severity is end-user-facing:** every converted end-user program that uses `time.Second`,
> `time.Minute` or `time.Hour` fails to COMPILE. That is a large share of real Go code, and it hits both
> end-user paths — a standalone `go2cs <dir>` and `-recurse` (which references the pre-converted stdlib
> rather than converting it).
>
> **FIXED 2026-07-25** (`claude/r5-collrename`). A foreign package's collision renames are now derived
> from that package's own `go/types` declarations (`src/go2cs/foreignNameCollisions.go`) whenever its
> `package_info.cs` is absent, so the spelling no longer depends on the run's package set. Proof: the
> four-line program below converts and **compiles and runs**; `go2cs -stdlib archive/tar` alone is now
> byte-identical to the committed full-run corpus; CNR byte-identical across 482 behavioral projects.
> See *Mechanism* below for the correction to this document's original diagnosis. Full write-up:
> [`docs/ConversionStrategies-Reference.md`](../ConversionStrategies-Reference.md) → *A foreign package's
> collision rename is derived from that package, not from the conversion run*.
>
> **The sibling class closed 2026-07-25** (`claude/r7-sibmeta`): a dependency's **re-exported type
> aliases** (`os`'s `type FileMode = fs.FileMode`) are derived the same way — `src/go2cs/foreignTypeAliases.go`.
> The third class, its **`GoImplement` pairs**, is ruled *underivable* rather than pending, and retires
> with the runtime interface shells. Detail in the last section of this document.

---

## Symptom

A four-line consumer of `time`:

```go
package main

import "time"

func main() { _ = time.NewTimer(time.Hour); _ = 2 * time.Second }
```

converted with `go2cs .` and compiled against `go-src-converted/time`:

```text
main.cs(5,28): error CS0019: Operator '*' cannot be applied to operands of type 'int' and 'method group'
main.cs(5,15): error CS1503: Argument 1: cannot convert from 'method group' to 'go.time_package.Duration'
main.cs(…):    error CS0023: Operator '-' cannot be applied to operand of type 'method group'
```

`time` declares **both** `const Second Duration` and `func (t Time) Second() int`, a const-vs-method
collision, so the converter Δ-renames the constant in `time` itself — `time.cs` declares
`ΔSecond`/`ΔMinute`/`ΔHour`. The consumer emitted the *unrenamed* `time.Second`, which binds to the
method group. Same for `Minute`/`Hour`.

## The emission depends on RUN COMPOSITION (reproducer)

`archive/tar/writer.cs` uses `time.Second`. Converting it **without** `time` in the same run:

```text
$ go2cs -stdlib archive/tar -comments -go2cspath /c/temp/a
writer.cs:98:  tw.hdr.ModTime = tw.hdr.ModTime.Round(time.Second);      ← wrong, will not compile
```

Converting it **with** `time` in the same run:

```text
$ go2cs -stdlib time archive/tar -comments -go2cspath /c/temp/b
writer.cs:98:  tw.hdr.ModTime = tw.hdr.ModTime.Round(time.ΔSecond);     ← correct (what master has banked)
```

The committed corpus is therefore right only because it was produced by a FULL `-stdlib` run in which
`time` was converted before `archive/tar`.

## Mechanism

> **Correction (2026-07-25, from the fix).** The diagnosis below is wrong in its causal detail, though
> right in its conclusion. `nameCollisions` does **not** leak across packages within a run —
> `resetPackageState` clears it per package (`packageStateOperations.go`). The real carrier is the
> dependency's emitted **`package_info.cs`**: `performNameCollisionAnalysis` records each collision as an
> exported `[assembly: GoTypeAlias("Second", "const:ΔSecond")]`, and a consumer reads those back through
> `loadImportedTypeAliases` → `importedTypeAliases`, which `getAliasedTypeName` consults at the reference
> site. So the dependency is on the **artifact existing in the output root the run resolves against**, not
> on run-accumulated map state — which is why the two-package run works (it writes `time`'s
> `package_info.cs` first) and why the risk direction 2 below (a bare-name false positive leaking into an
> unrelated later package) **cannot happen**. The fix accordingly derives the missing artifact's collision
> entries from the dependency's own `go/types` scope rather than package-qualifying `nameCollisions`.

`nameCollisions` (`main.go:545`) is a **bare-name** `map[string]bool` — no package qualification. It is
populated by `performNameCollisionAnalysis(pkg)` for the package being converted, and read by bare name
at every reference site (`convSelectorExpr.go:56`, `convIdent`, `convUnaryExpr.go:74`, `main.go:2568`).
A reference to a *foreign* package's colliding member has nothing package-aware to consult, so it is
spelled correctly only if some **earlier package in the same run** happened to put that bare name in the
map. The reproducers above show exactly that dependency.

Note the converter already solved the analogous problem for one case, and the fix shape is right there:
`packageHasMethodNamed` (`convSelectorExpr.go:101`) recomputes a *foreign* package's type-vs-method
collisions on demand from `types.Package` scope, with a per-run cache, precisely because "the current
package's `nameCollisions` map does not apply." The const/var-vs-method case needs the same treatment —
a foreign-package-aware lookup at the reference site, driven off `obj.Pkg()`.

## Two risk directions

1. **False negative (demonstrated).** A consumer converted without its dependency in the same run emits
   the unrenamed name → does not compile. This is the end-user path, always.
2. **False positive (structural, not yet demonstrated).** Because the map is bare-name and evidently
   survives across packages within a run, a name that collides in an earlier package can Δ-rename an
   *unrelated* identifier of the same spelling in a later package of the same run. Worth a positive-control
   scan when this is fixed.

## Scope of exposure

Any exported const/var whose name is also a method name in its own package. `time` is the worst offender
because `Second`/`Minute`/`Hour`/`Nanosecond` are among the most-used identifiers in Go, and `Time` has
same-named accessors. A corpus census (`grep` for `\.Δ` in `go-src-converted`) enumerates the rest.

## Workaround until fixed

None inside converted code — the consumer must avoid the colliding member (e.g. `1000 * time.Millisecond`
for `time.Second`), which is what the timer repro did. Converting the consumer *and* its dependencies in
one `-stdlib` run also produces correct output, but that is not the end-user path.

## The same shape, two other metadata classes — one CLOSED, one ruled underivable (2026-07-25)

A dependency's `package_info.cs` carries more than its collision renames. Both remaining classes were
observed directly while gating the collision fix: single-package reconverts of `archive/zip` and
`internal/testenv` matched the committed corpus in every respect **except** these two.

1. **Re-exported Go type aliases — FIXED 2026-07-25** (`claude/r7-sibmeta`, `src/go2cs/foreignTypeAliases.go`).
   `os` declares `type FileMode = fs.FileMode`, published as `("FileMode", "go.io.fs_package.FileMode")`.
   Without it a standalone consumer emits `os.PathError`, which is not a member of `os_package` (the
   re-export is an assembly-scoped `global using` inside `os`) — CS0426. Now derived from the dependency's
   own `go/types` scope under the same invariant, including the target package's own collision rename
   (`internal/reflectlite`'s `type Kind = abi.Kind` → `go.@internal.abi_package.ΔKind`) and the
   defined-type-over-an-interface shape (`crypto`'s `type PublicKey any` → `object`). Proof: a standalone
   `os.FileMode`/`FileInfo`/`PathError` + `fs.WalkDir` program compiles and **runs** byte-identically to
   `go run .` (pre-fix: the CS0426 above); `-stdlib crypto/ecdh` alone now reconverts `ecdh.cs`
   byte-identically to the committed corpus (pre-fix: the nonexistent `crypto.PublicKey`); whole-stdlib
   reconvert byte-for-byte unchanged. Five shapes are deliberately **not** derived — see
   [`docs/ConversionStrategies-Reference.md`](../ConversionStrategies-Reference.md) → *A foreign package's
   re-exported type ALIAS is derived from that package too*.
2. **`GoImplement` pairs** (`loadPackageImplements`) — **underivable, not merely undone.** Absent, the
   consumer records and emits its own adapter class: `archive/zip` converted alone emits
   `new io_SectionReaderжReader(rs)` where the full-run corpus emits the foreign
   `new io.SectionReaderжReader(rs)`. The pairs are recorded at CONVERSION time from the cast/witness sites
   the dependency's own bodies contain, so which adapters its assembly carries is a product of its
   **emission**, not of its declarations — there is nothing sound to compute from `go/types`, and an
   over-approximation would name adapters that do not exist (CS0246), strictly worse than today's behavior
   (the local duplicate compiles and behaves identically). The class retires with the **runtime interface
   shells** rather than with a derivation: once a concrete-to-interface conversion goes through a
   runtime-constructed shell instead of a compile-time adapter class, there is no per-pair record left to be
   missing. Do not re-file this as a derivation task.
