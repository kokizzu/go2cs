# Option A (verbatim ImportPath on [GoPackage]) — footprint PREDICTION

Written 2026-09-27, BEFORE any emission from the cut converter exists. Base: origin/master 1dae85e093.
Derived by predict.py / predict_behavioral.py from the COMMITTED trees, applying the cut's own rule:
stamp exactly where decoding (namespace tail minus the `go` root, '@' stripped, '.'→'/', + '/' +
[GoPackage] name) differs from Go's import path (package main → "main").

## Line kind (every target, every file)

Exactly ONE line changes per stamped package class:
`[GoPackage("<name>")]` → `[GoPackage("<name>", ImportPath = "<verbatim path>")]`.
So each file is +1/−1. There are ZERO GoPositionMap lines and ZERO other content changes. No
production .cs other than package_info.cs moves, because the emission is metadata only.

## Stdlib two-seeded -stdlib diff (package_info.cs only)

There are 26 stamped files in the committed tree: 18 flat and 8 per-GOOS.

Flat, all 3 targets:
- the vendor/golang.org/x packages except nettest, cpu and route (15 files):
  - crypto (6): chacha20, chacha20poly1305, cryptobyte, cryptobyte/asn1, internal/alias,
    internal/poly1305
  - net (5): dns/dnsmessage, http/httpguts, http/httpproxy, http2/hpack, idna
  - text (4): secure/bidirule, transform, unicode/bidi, unicode/norm
- math/rand/v2
- crypto/internal/fips140deps (name fipsdeps)
- internal/trace/internal/testgen/go122 (name testkit)

That is 15 + 3 = 18 flat files.

Per-GOOS:
- vendor/golang.org/x/net/nettest: windows, linux, darwin
- vendor/golang.org/x/sys/cpu: windows, linux, darwin
- vendor/golang.org/x/net/route: darwin only
- crypto/x509/internal/macos: darwin only (name macOS)

PREDICTED per target:

| Target | Files | Lines | Composition |
|---|---|---|---|
| windows | 20 | +20/−20 | 18 flat + nettest + cpu |
| linux | 20 | +20/−20 | 18 flat + nettest + cpu |
| darwin | 22 | +22/−22 | 18 flat + nettest + cpu + route + macos |

Falsifiers:
- any file other than a package_info.cs;
- any line other than the [GoPackage] line;
- a stamped path that is not the package's directory;
- a count off by more than the per-GOOS reading (see uncertainty 2).

## CNR (behavioral, 748 package_info.cs scanned)

4 existing projects change, one [GoPackage] line each:

| Project | Stamped ImportPath | Reason |
|---|---|---|
| VersionedImport/vlib | "vlib/v2" | major-version directory |
| AliasNamespaceShadow/sortlocal | "AliasNamespaceShadow/sortlocal" | name sort ≠ dir |
| Constraints | "go2cs/Constraints" | library package at the module root: Go's path is the module path |
| CrossPkgSameNameAlias | "go2cs/CrossPkgSameNameAlias" | same |

Plus the NEW DottedModulePath test (its dotted/ sub-package is stamped "example.com/dotted/v2").

## Ranked uncertainties

1. The two root-level library rows (Constraints, CrossPkgSameNameAlias). Stamping them is correct by
   Go's rule, but the trimGo2CSModulePrefix convention treats `go2cs/` as a repository marker. If the
   converter's packageImportPath for them is not "go2cs/<Name>" (loader-dependent), they do not
   stamp. Reading that would be an instrument fact, not a mechanism miss.
2. The per-GOOS counts assume the per-GOOS package_info.cs files are what each target re-emits. A
   target that does not emit a package (route, macos on windows) contributes nothing.
3. The -tests package_test_info.cs files converge only on a -tests run, so they are NOT in the
   -stdlib diff. They appear as sweep dirt for the banked rows whose test info carries a stamped
   class. math/rand/v2 is the only such row with a committed test info among the 26.

---

## SCORED 2026-09-27 (appended; the prediction above is unedited)

**Run 1, cut 776491b6b0, windows only (stopped at the fleet shutdown): FALSIFIER FIRED.** Windows read
20 files, +20/−20, all on the `[GoPackage]` line, so the count and the line kind were MET. But the
falsifier "a stamped path that is not the package's directory" fired on 17 of the 20: every vendored
package was stamped `golang.org/x/...` without the `vendor/` prefix Go's reflect reports. The -stdlib
load hands the converter the loader's path, which from inside std resolves un-vendored. Scored against
this prediction, the MECHANISM half (which files, which line) held and the SPECIFICS half (the path
text) did not. The fix is 9d46e5bff3: the stamp is keyed on the package DIRECTORY under
GOROOT/src/vendor, not on its import path.

**Run 2, base 1dae85e093 / cut 9d46e5bff3, all three targets: MET.**

| Target | Predicted | Measured | Other lines |
|---|---|---|---|
| windows | 20 files, +20/−20 | 20 files, +20/−20 | 0 |
| linux | 20 files, +20/−20 | 20 files, +20/−20 | 0 |
| darwin | 22 files, +22/−22 | 22 files, +22/−22 | 0 |

- Every file is a `package_info.cs`, each +1/−1 on its `[GoPackage]` line. There are 0 `GoPositionMap`
  lines.
- Every stamped path is Go's own: the 15 flat vendored packages, nettest and cpu read as
  `vendor/golang.org/x/...`; the others read `math/rand/v2`, `crypto/internal/fips140deps` and
  `internal/trace/internal/testgen/go122`; darwin adds `vendor/golang.org/x/net/route` and
  `crypto/x509/internal/macos`.
- Uncertainty 2 (per-GOOS) resolved as predicted. The 62 target-files fold into the 26 committed files
  this document names (18 flat + 8 per-GOOS).

**CNR at 9d46e5bff3: MET.** CHANGED is exactly the four predicted projects, one `[GoPackage]` line each,
with the predicted paths, plus the new DottedModulePath. Uncertainty 1 resolved toward stamping:
`Constraints` and `CrossPkgSameNameAlias` read `go2cs/<Name>`.

**Uncertainty 3** (package_test_info.cs converges only on a -tests run) is unmeasured by these two
instruments, as it stated it would be.
