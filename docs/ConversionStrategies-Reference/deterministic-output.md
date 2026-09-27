# Deterministic Output

[Reference index](README.md) · [Summary of this topic](../ConversionStrategies.md#deterministic-output)

Converter output is **byte-reproducible**: converting the same Go source with the same converter build produces byte-identical C# every run. This is a hard guarantee the goldens, the full-conversion error measurements, and any future release tag all rest on. Three mechanisms enforce it (all landed 2026-07-01, proven by two consecutive full-stdlib conversions diffing to zero across 305 packages):

* **Files convert sequentially, in sorted-filename order.** Per-file conversion previously ran in concurrent goroutines sharing package-level state claimed at visit time — `initΔN` module-initializer indices, blank-identifier temp numbering (`_ᴛN`, an unsynchronized map — a genuine data race), and imported collision-rename visibility: a file could mark an imported `package_info.cs` "parsed" *before* the parse finished, so a concurrently-converting file skipped the wait and emitted an imported renamed const **bare** (`abi.String` instead of `abi.ΔString` — a compile error that came and went with goroutine scheduling). Sequential conversion costs nothing measurable: a full-stdlib conversion is 3m42s concurrent vs 3m39s sequential (the cost is `go/packages` type-graph loading, not emission).
* **The stdlib conversion queue is deterministic and dependency-complete.** The topological sort now walks sorted roots (map-iteration roots made unrelated packages' order flip run-to-run), and a GOROOT-**vendored** dependency (imported as `golang.org/x/…` but keyed on disk as `vendor/golang.org/x/…`) is resolved to its vendored key — previously the edge was silently dropped, so an importer (e.g. `x/text/secure/bidirule`) could convert *before* its dependency (`x/text/unicode/bidi`), whose `package_info.cs` — the source of imported collision-rename aliases like `bidiꓸClass` — did not exist yet at that point.
* **Multi-box re-alias emission is sorted.** A multi-assignment that repoints several pointer boxes (`(Ꮡx, Ꮡy) = (Ꮡy, Ꮡx)`) emits its independent `n = ref Ꮡn.Value` refreshers in sorted name order (the set backing them is a map).

---

[← Comments](comments.md) · [Index](README.md) · [Packages That Do Not Type-Check →](packages-that-do-not-type-check.md)
