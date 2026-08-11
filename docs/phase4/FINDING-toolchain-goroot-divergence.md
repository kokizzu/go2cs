# FINDING — go2cs assumes one toolchain; Go stopped guaranteeing that

**Status:** characterization complete, remedy staged. Lane **L5**, 2026-08-11, laptop.
**Origin:** [issue #37](https://github.com/ritchiecarroll/go2cs/issues/37) — "Missing standard packages",
`-recurse=nuget` emitting `PackageReference`s that nuget.org cannot satisfy.

**Headline.** The reporter's eight missing package ids are **three unrelated defects wearing one
costume**, and only the third is what the issue title says. Six of the eight are Go 1.24 standard-library
additions with no counterpart in the published Go 1.23.1 corpus — a version mismatch the converter
never checks for. Two are a name the converter mints wrong. And underneath both sits a structural
assumption that has quietly expired: **go2cs resolves GOROOT from the AMBIENT toolchain while
`go/packages` loads from the MODULE-SELECTED one**, and since Go 1.21's `toolchain` directive those
are routinely different machines' worth of standard library.

The converter already knows this can happen — `loaderReleaseTags` (`directiveOperations.go:233`)
resolves *release tags* by asking `go env GOVERSION` **from the loader's directory**, with a comment
on `getGoEnvFrom` spelling out the GOTOOLCHAIN hazard verbatim. That insight was simply never applied
to GOROOT. The inconsistency is the bug.

---

## 1. Root cause — exact

`main.go:52-62` resolves GOROOT once, before the input path is even parsed, from the process
environment or a bare `go env GOROOT`:

```go
if goRoot = os.Getenv("GOROOT"); len(goRoot) == 0 {
    if goRoot, err = getGoEnv("GOROOT"); err != nil {   // <- no dir: the AMBIENT toolchain
        goRoot = runtime.GOROOT()
    }
    ...
}
```

`getGoEnv` is `getGoEnvFrom("", name)`, and that empty dir is the whole defect. Compare the
established module-aware form two files over:

```go
// directiveOperations.go:249, inside loaderReleaseTags
if version, err := getGoEnvFrom(moduleRoot, "GOVERSION"); err == nil {
```

Meanwhile the loader is explicitly pointed at the module (`moduleConverter.go:161`,
`cfg.Dir = moduleDir`). So when the target module's `go`/`toolchain` directive requires a newer
release, `go list` re-execs that toolchain and reports its packages from a GOROOT under
`$GOPATH/pkg/mod/golang.org/toolchain@…`, while `options.goRoot` still names the ambient install.

### Measured (2026-08-11, laptop, ambient Go 1.23.1)

A module whose `go.mod` says `go 1.25.0`:

| Asked from | `go env GOROOT` | `go env GOVERSION` |
|---|---|---|
| the module dir | `C:\Users\Admin\go\pkg\mod\golang.org\toolchain@v0.0.1-go1.25.0.windows-amd64` | `go1.25.0` |
| the module dir, with `GOROOT` env pinned to `C:\Program Files\Go` | *same toolchain path* | `go1.25.0` |
| a non-module dir | `C:\Program Files\Go` | `go1.23.1` |

Two things this settles. The divergence is a pure function of **which directory you ask from** — so
asking from the loader's directory fixes it by construction. And **setting `GOROOT` in the environment
does not fight the toolchain switch** (row 2), so `main.go`'s `os.Setenv("GOROOT", …)` is inert with
respect to selection; it only makes the wrong value stickier.

---

## 2. What the divergence breaks

Every consumer of `options.goRoot` / `build.Default.GOROOT` silently inverts its answer:

| Site | Consumes | Consequence when GOROOT is the wrong toolchain |
|---|---|---|
| `moduleConverter.go:235` `classify` | `options.goRoot` | No package is `classStdLib`. The whole standard library is classified **third-party**, module `std`. |
| `importOperations.go:375` `getImportPackageInfo` | `pkg.Goroot` | Stdlib routed to the third-party branch. |
| `importOperations.go:455` `getLocalModulePackageInfo` | `options.goRoot` | The GOROOT branch never fires. |
| `visitImportSpec.go:619` `resolveGorootVendoredPath` | `build.Default.GOROOT` | The `vendor/` probe `os.Stat`s a directory that does not exist, so **no** GOROOT-vendored path is ever resolved. |
| `readmeValidationBadge.go` | `options.goRoot` | Vendored module pins read from the wrong `src/vendor/modules.txt`. |

### Reproduction — the converter exits 0 and emits an unbuildable project

```
go2cs -recurse=nuget -go2cspath <repo>\src <module> <out>
```

against a module importing `golang.org/x/net/http/httpproxy` (v0.57.0 requires go >= 1.25, so
`go mod tidy` writes `go 1.25.0` and the switch engages). Result: **exit 0**, "Closure: … referencing
144 stdlib" — and every emitted csproj carries relative project references to files that were never
generated, because `-recurse=nuget` does not convert the standard library:

```xml
<ProjectReference Include="../../../pkg/fmt/std.fmt.csproj" />
<ProjectReference Include="../../../../../net/netip/std.net.netip.csproj" />
```

`out\pkg` contains `golang.org` and nothing else. The `std.` prefix is the tell: it is the module
name `std`, reached because `classify` fell through to the dependency-module branch.

**This bypasses the guard that should have caught it.** `docs/README.md:264` documents the expected
failure for a too-new module — `package requires newer Go version go1.25` — but that error is raised
by a 1.23.1 toolchain *refusing* to load. The auto-switch means no 1.23.1 toolchain is ever asked, so
the load succeeds and the diagnosis never fires.

---

## 3. The published-corpus mismatch (the reporter's headline six)

Independent of GOROOT, and the actual cause of the reported `NU1101`s. `src/version.props` pins
`GoStdLibVersion` 1.23.1 / `GoBuildNumber` 5; the feed is a Go **1.23.1** conversion. All six of these
were added to the standard library in **Go 1.24**, so no `go.<pkg>` was ever packed for them:

`crypto/fips140` · `crypto/hkdf` · `crypto/mlkem` · `crypto/pbkdf2` · `crypto/sha3` · `weak`

Verified both directions. On nuget.org, `go.crypto.sha3` → **404**, while `go.lib`,
`go.internal.weak` and `go.vendor.golang.org.x.net.http.httpproxy` all resolve at **1.23.1.5**.
Against a local Go 1.23.1 `go/build` probe, `crypto/sha3` and `weak` report *"not in std"* while
`internal/weak` resolves under GOROOT — i.e. `weak` is the 1.24 promotion of `internal/weak`, not a
packaging omission.

`writeProjectFile` emits a `go.<pkg>` reference for **every** import classified stdlib
(`projectFileWriter.go:386`) with no check that such a package can exist. The user learns at restore
time, in bulk. Both numbers needed to catch it are already in hand at emit time: `goVersion()`
(`readme.go:59`) and the `GoStdLibVersion` the converter itself writes into the generated
`Directory.Build.props` (`moduleConverter.go:731`).

---

## 4. The vendored-name defect (the reporter's remaining two)

`go.golang.org.x.net.http.httpproxy` and `go.golang.org.x.net.http2.hpack` are **not** the published
ids — the corpus names those `vendor.golang.org.x.net.http.httpproxy` / `…http2.hpack`, the published
`go.net.http` nuspec depends on them under exactly those names, and both resolve at 1.23.1.5. So the
un-prefixed form was minted by the reporter's own conversion.

`getLocalModulePackageInfo`'s GOROOT branch (`importOperations.go:455-473`) derives `targetDir` from
`meta.Dir` — the real `…/src/vendor/golang.org/…` location — but derives the *name* from the import
path **as written**:

```go
importPathParts := strings.Split(importPath, "/")   // golang.org/x/net/http/httpproxy
packageName := strings.Join(importPathParts, ".")   // golang.org.x.net.http.httpproxy  <- no vendor.
```

It never calls `resolveGorootVendoredPath`, which `dependencyGraph.go:123`,
`importAliasOperations.go:114` and `importOperations.go:964` all do apply. The result is a reference
whose **directory is right and whose file/package name is wrong** — unresolvable as a project
reference, and unresolvable as a `go.<id>` NuGet package.

⚠ **NOT reproduced — and one attempt came back NEGATIVE.** Converting GOROOT's own `net/http`
standalone (`go2cs <goroot>/src/net/http <out>`, matched toolchain) emits all four vendored
references **correctly prefixed**:

```xml
<ProjectReference Include="$(go2csPath)core/vendor/golang.org/x/net/http/httpproxy/vendor.golang.org.x.net.http.httpproxy.csproj" />
<ProjectReference Include="$(go2csPath)core/vendor/golang.org/x/net/http2/hpack/vendor.golang.org.x.net.http2.hpack.csproj" />
```

So the branch is not reached on the obvious path: `visitImportSpec` vendor-resolves the import
BEFORE `getImportPackageInfo` sees it whenever the importing file lives under GOROOT, which is every
stdlib package. Reaching the defective naming needs an importer *outside* GOROOT whose
`golang.org/x/…` import nonetheless resolved into GOROOT's vendor tree, and no input constructed so
far produces that combination.

**What is still solid:** the published feed and the corpus both use the `vendor.`-prefixed names
(the `go.net.http` nuspec depends on them under exactly those ids, and all resolve at 1.23.1.5), so
the reporter's un-prefixed ids did originate in their own conversion. **What is not solid:** the
mechanism above being that origin. Treat §4 as an open question with a leading suspect, not a
diagnosed defect — the fix must not land until an input reproduces it, or the real path is found.
The issue-#37 reply asserted this more confidently than the evidence supports and is owed a
correction.

---

## 5. Remedy — as implemented (A, B, D landed; C open)

**A. Resolve GOROOT from the loader's directory.** `toolchainResolution.go` + `main.go`. After the
input path is known, `resolveLoaderGoRoot` asks `go env GOROOT` from the input's module root — the
same move `loaderReleaseTags` makes for GOVERSION. Precedence is explicit: an operator-supplied
`GOROOT` env or `-goroot` flag wins verbatim (`goRootPinned`); only a *derived* value is re-resolved.
The value is deliberately **not** exported — §1 row 2 shows the environment does not steer selection,
so exporting would only make a stale value stickier.

*Cost.* Re-resolution spends a `go env` subprocess (~300ms on Windows) and the converter runs once
per package — 580 of them in a full CNR. `toolchainSwitchPossible` is the gate that keeps this at
**exactly zero** for runs that cannot be affected: it reads the module's `go`/`toolchain` directives
and the `GOTOOLCHAIN` environment variable, all without a subprocess, and only pays when a switch is
actually possible. Nothing in the corpus or the behavioral suite asks for a newer toolchain.

**B. Make the reported release the LOADER's, and refuse a mismatch.** Two parts, both landed.
`pinGoVersion` (`readme.go`) fixes what `goVersion()` reports, because that value becomes the emitted
`$(GoStdLibVersion)` — the version every `go.<pkg>` reference restores at. Then
`checkNuGetStdLibCompatibility` refuses a `-recurse=nuget` run whose standard library comes from a
different Go **language** release than the one this converter publishes for, naming both and exiting
before a single file is written. Patch drift (1.23.1 vs 1.23.7) is deliberately allowed: same standard
library, and the floating revision already spans it.

*Design note — where "the published release" comes from.* `publishedStdLibRelease()` returns the
release the **binary itself was built with** (`runtime.Version()`), on the standing invariant that
`version.props`' `GoStdLibVersion` tracks the converter's own `go.mod`. That is free, exact, and
cannot go stale. It does **not** know which corpus versions are actually on nuget.org — if a release
is built but never published, this check still passes. Closing that gap needs an embedded
version.props value or a feed query; **flagged for coordinator ruling**, not decided here.

**C. Vendor-resolve the name in `getLocalModulePackageInfo`.** NOT DONE — still owed, and still
unreproduced (§4). Apply `resolveGorootVendoredPath` to the import path before deriving
`packageName`, so directory and name agree, with a unit guard.

**D. Document the pin.** `docs/README.md`: the `-recurse=nuget` option row now states the
one-Go-release constraint and the refusal, and the walkthrough NOTE now explains that its
`GOTOOLCHAIN=local` is *what makes the too-new-module error appear at all* — left unset, Go
downloads and re-execs the newer toolchain and the failure moves to restore time.

## 6. Traps this arc uncovered

**`modfile.ParseLax` silently drops the `toolchain` directive.** Its non-strict gate
(x/mod `modfile/rule.go:338-345`) keeps only `go`, `module`, `retract` and `require`; every other
verb returns before reaching the semantic layer, so `File.Toolchain` is **always nil** under
`ParseLax` no matter what the file says. Reading it there makes `go 1.21` + `toolchain go1.25.0` —
the exact shape `go mod tidy` writes when it bumps a module — report a 1.21 request, which disables
the whole GOROOT re-resolution in the commonest case that needs it. The directive is read from the
retained syntax tree instead (`toolchainDirective`). Strict `Parse` is not the alternative: it
rejects directives newer than the vendored x/mod, i.e. exactly the forward-compatible go.mod this
check exists to read. A unit guard pins both halves.

**The reporter's symptom shape is reproducible.** Converting the §2 module with the *unfixed*
binary emits `<GoStdLibVersion>1.23.1.*</GoStdLibVersion>` while converting the **1.25** standard
library. That is the reported failure exactly: every package present in both releases resolves, and
only the ones a later release added — `crypto/sha3`, `weak`, the FIPS-140 family — 404. With A+B the
same command refuses, naming both releases. (This does not by itself prove the reporter's instance
came from a toolchain *switch* rather than an ambient 1.24+ install; both produce this shape, and
both are caught.)

## 7. Gates — run on the lane machine (laptop, 2026-08-11)

| Gate | Result |
|---|---|
| Converter `go test ./...` | **green**, 149 s, with unit guards for A and B |
| `check-no-regression.ps1` (full) | **NO REGRESSION** — generated C# and `.csproj` byte-identical across all **580** behavioral packages; 147 advisory converter warnings, zero NOT MEASURED |
| solution-integrity + path-casing preflight | OK — 582 registered projects, 4,185 tracked paths |
| §2 reproduction | before: exit **0** with dangling `std.*.csproj` refs · after: **refuses**, names both releases, writes nothing |
| GOROOT-vendored naming (§4) | attempted repro **NEGATIVE** — names emit correctly prefixed; C not landed |

CNR is the claim that mattered: A and B must move **no emitted byte** on a matched toolchain. A
1.23.1 host resolves the GOROOT it always did and `toolchainSwitchPossible` never fires, so the
corpus is untouched — confirmed byte-for-byte, which is equally the empirical proof that the
subprocess gate costs the corpus path nothing.

**Merge window:** converter change — same constraint as L4, **post-1.23.1.6**.

**Open for coordinator ruling.** `publishedStdLibRelease()` uses the converter's own BUILD release as
the proxy for "which corpus versions exist", on the standing invariant that `version.props`'
`GoStdLibVersion` tracks the converter's `go.mod`. Free, exact, cannot go stale — but blind to what is
actually on nuget.org, so a release that is built and never published still passes the preflight.
Closing that needs either an embedded `version.props` value or a feed query at conversion time;
neither was invented here.
