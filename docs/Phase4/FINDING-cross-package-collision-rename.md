# FINDING — a foreign package's Δ-renamed member is only spelled correctly when that package is converted in the SAME run

> Filed 2026-07-24 from the timer arc (sub-agent `claude/r2-timers`), where it blocked the converted
> verification repro. **Not fixed** — it is a converter change (`nameCollisions` scoping) whose gate is
> CNR + the 302-package corpus, unrelated to the timer work that found it. Written up per charter §10.
>
> **Severity is end-user-facing:** every converted end-user program that uses `time.Second`,
> `time.Minute` or `time.Hour` fails to COMPILE. That is a large share of real Go code, and it hits both
> end-user paths — a standalone `go2cs <dir>` and `-recurse` (which references the pre-converted stdlib
> rather than converting it).

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
