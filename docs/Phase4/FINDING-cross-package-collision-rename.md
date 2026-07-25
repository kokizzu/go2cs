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
> See *Mechanism* below for the correction to this document's original diagnosis, and *Still open* for
> the two neighboring classes the fix does not cover. Full write-up:
> [`docs/ConversionStrategies-Reference.md`](../ConversionStrategies-Reference.md) → *A foreign package's
> collision rename is derived from that package, not from the conversion run*.

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

## Still open — the same shape, two other metadata classes (2026-07-25)

A dependency's `package_info.cs` carries more than its collision renames, and the rest is **not** derivable
from the collision rule. Both remain run-composition-dependent, with the same end-user symptom:

1. **Re-exported Go type aliases.** `os` declares `type FileMode = fs.FileMode`, published as
   `("FileMode", "go.io.fs_package.FileMode")`. Without it a standalone consumer emits `os.FileMode`,
   which is not a member of `os_package` (the re-export is an assembly-scoped `global using` inside `os`)
   — CS0426. `os.FileMode`/`FileInfo`/`DirEntry`/`PathError` are common in real Go code, so this is the
   next-most-valuable item of this class. Deriving it needs the alias TARGET's rendered C# type name (a
   `visitTypeSpec` computation), not just a rename.
2. **`GoImplement` pairs** (`loadPackageImplements`). Absent, the consumer records and emits its own
   adapter class: `archive/zip` converted alone emits `new io_SectionReaderжReader(rs)` where the full-run
   corpus emits the foreign `new io.SectionReaderжReader(rs)`.

Both were observed directly while gating the collision fix: single-package reconverts of `archive/zip` and
`internal/testenv` match the committed corpus in every respect **except** these two classes.
