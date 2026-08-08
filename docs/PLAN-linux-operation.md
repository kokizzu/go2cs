# PLAN — cross-platform operation (the big three: Windows, Linux, macOS)

> **Status:** partially executed. Arc 1 is complete (F2, F3), Arc 2 is complete except its
> whole-corpus item (F6 and F10 done; **F5 open** — it rewrites a line in every emitted `.csproj`, so
> it must land inside a rebank arc), and Arc 3 has landed its PowerShell tier plus `BehavioralRunner`
> and `UpdateTestTargets`. Per-finding disposition is the **Status** column of the findings table in
> §2, and the reasoning behind each is the *Execution log* directly beneath it — read that before
> re-planning anything here. Everything else stands as originally written: a plan, unimplemented.
>
> **Path convention.** Every repository path here uses the **normalized lowercase** folder casing
> (`src/tests`, `src/archived`, `src/utilities`, `docs/phase3`, `docs/phase4`). The evidence line numbers
> were read against the pre-normalization tree, where those five directories carried initial capitals; the
> file contents and line numbers are unchanged by a pure rename. There is no `src/Examples` — the samples
> live under `src/tour` and `src/tests/behavioral`.
>
> **Two sibling changes are assumed landed** and are referenced as prerequisites, not as work:
> **(a) CNR determinism** — every harness invocation of the converter pins `-go2cspath <repo>/src`, and the
> converter self-locates a valid go2cs root and warns loudly on an invalid one.
> **(b) folder-casing normalization** — the five renames above.

---

## 1. Executive summary

go2cs today is a Windows project in three independent senses, and they must be separated before any of them
can be planned against:

1. **The converter is a Go program and builds anywhere** — but it *emits* Windows-shaped artifacts and, when
   *hosted* on Linux, emits a malformed project reference. Two small, surgical defects (§A1).
2. **The harness — scripts, runners, utilities — is hard-coded to `\` and `.exe`.** Mechanical, well-bounded,
   one arc of work (§A5).
3. **The converted standard library in `src/core` is not portable code that happens to run on Windows. It
   *is* Windows.** It was converted with `GOOS=windows/GOARCH=amd64`; 87 `_windows` files, zero `_linux`
   files, `GOOS = "windows"` compiled in as a constant, and every byte of I/O bottoming out in a `kernel32.dll`
   P/Invoke. This is not a portability bug to fix — it is a second corpus to produce (§A2).

**Top findings, ranked by severity.**

| # | Finding | Severity |
|:--|:--|:--|
| **F1** | `src/core` is a `GOOS=windows/amd64` conversion. A converted `fmt.Println` program throws `DllNotFoundException("kernel32.dll")` on Linux. `go test` cannot even produce the reference baseline for a windows-target package on a Linux host, so the Phase-4 pipeline is structurally unavailable there. A Linux lane needs a **second, GOOS=linux corpus** — not a fix. | **S1** |
| **F2** | Committed `.cs` blobs are stored **LF** (`git ls-files --eol` → `i/lf`), while the converter deterministically emits **CRLF**. A fresh Linux clone (`core.autocrlf=false` by default) therefore materializes LF and every single converted file reports as modified. `check-no-regression.ps1` is 100 % false-positive on Linux before the first line of converter work. | **S1** |
| **F3** ✅ | `packageInfoWriter.go:52` splits a **read-back** `package_info.cs` on `"\r\n"`. The `.gitattributes` `eol=crlf` pins (commit `026762932`) cover the three *embedded templates* only — they do **not** cover this seam. On an LF checkout the section markers are never found and the converter `log.Fatal`s on the first package. Consequence of F2, but a separate remedy. | **S1** |<br>**FIXED** (landed early, as a blocker of the issue-#33 regression test — that test calls `processConversion`, and a `log.Fatal` ends the whole `go test` binary). Both splits are now EOL-agnostic, the template one too: the `eol=crlf` pin governs a *checkout*, so a tree materialized before the pin landed embeds an LF template and a **fresh** `package_info.cs` was fatal as well. Measured on Linux: 0/569 behavioral packages converted before, 569/569 after, and **42** `.cs` files that had been emitting an unresolved `«ADAPTER:…»` marker (the records live in the `package_info.cs` the converter could not read) now match their committed Windows goldens byte-for-byte modulo CRLF — the fix moves Linux output TOWARD the canonical corpus and no file away from it. |
| **F4** | Every harness instrument breaks on Linux at line 30-ish: `Join-Path $root "src\go2cs"`, `bin\go2cs.exe`, `-notmatch '\\(bin\|obj)'`, `($_.FullName -split '\\').Count`, `Path.Combine(base, @"..\..\..\..")`, `PublishProfile = "win-x64"`, `BinOutput.Split(@"\")`. The depth-sort and bin/obj filters fail **silently**, which reverts the deepest-first invariant that closed FALSE-GREEN route #3. | **S1** |
| **F5** | Hosted on Linux, the converter emits `$(go2csPath)core\fmt/\fmt.csproj` — `filepath.Join` on Unix does not normalize the `\` the code injects two lines earlier (`importOperations.go:263`, `:324`). Every emitted `ProjectReference` to a stdlib package is malformed. | **S1** |<br>**Now VISIBLE in `go test`.** F3 was masking it: the old binary `log.Fatal`ed inside the first `processConversion`, ending the whole test binary, so most of the suite never ran and the truncated output read as two failures. With F3 fixed the suite completes and reports **nine** — seven F5 (`TestIsSelfProjectReference`, `TestParseCoreProjectRefs`, both `TestCollectConvertedProjects*`, all three `TestRecurse*`) plus the two CRLF-template tests. Benign, and the expected count until Arc 2 lands; do not read the jump as drift. |
| **F6** | `pathReplace` (`importOperations.go:458`) gates case-insensitivity on `runtime.GOOS == "windows"`. On Linux the exact-match replace **silently no-ops** when the resolved `GOROOT` spelling differs from `go/build`'s (symlinked toolchains), leaving a machine-specific absolute path in the emitted csproj with no diagnostic. | **S2** |
| **F7** | `deploy-core.ps1` stages the tree with **robocopy**, which does not exist on Linux. It is the only script with a hard external Windows dependency. | **S2** |
| **F8** | Two behavioral tests are Windows-semantic by construction (`LocalTimeZone`, `FindFirstFileData`); a third (`StdLibInternalAbi`) is `-text`-pinned for runtime byte-exactness. `time.initLocal` → `syscall.GetTimeZoneInformation` → `DllImport("kernel32.dll")` is a hard Linux fault on any converted program that touches `time.Local`. | **S2** |
| **F9** | NuGet packing is already RID-agnostic and needs no packaging change — but the *content* is GOOS-pinned, so `go.os`/`go.syscall`/`go.net`/`go.time` published today are Windows-only libraries wearing platform-neutral package IDs. That is a correctness-of-claim problem, not a build problem. | **S2** |
| **F10** | `golib` and `go2cs-gen` are **clean**: zero P/Invoke, zero Windows APIs, zero path assumptions across 50 runtime files and the whole netstandard2.0 analyzer. The runtime layer costs nothing to port. (One triviality: `golib.csproj` lacks the `USERPROFILE`→`HOME` fallback the converter's template carries.) | **S4** |

**Recommended sequencing.** The three senses above are independent, and the cheap two are prerequisites for
measuring the expensive one. Do them in this order and each arc lands green on its own:

```
Arc 1  Determinism  ── .gitattributes eol=crlf + the \r\n read-back seam        (F2, F3)
Arc 2  Converter    ── Linux-host emission: separators, pathReplace, warnings   (F5, F6)
Arc 3  Harness      ── one portability pass over scripts + runners + utilities  (F4, F7)
        ↓  at this point: `go2cs` builds and runs on Linux, the corpus is
           byte-clean on a fresh Linux clone, and CNR is a truthful instrument.
Arc 4  Measure      ── produce a GOOS=linux corpus into a scratch root; bucket   (F1, measurement only)
Arc 5+ Build the Linux, then darwin, stdlib lanes — option 2 + N2 (ruled, §A2.4) (F1, F8, F9)
```

Arcs 1–3 are worth doing **on their own merit even if the Linux stdlib is never built**: they are
determinism and hygiene fixes that make the Windows lane more honest too (F2/F3 in particular remove a
whole class of "why is my tree dirty" investigation).

**Scope addendum — the big three (user ruling, 2026-08-06).** The target set for this plan is
**Windows + Linux + macOS, and no further** — Go's GOOS matrix is far wider than .NET meaningfully
deploys, and chasing it would dilute the plan. The macOS cost is small and lands where it is noted:
arcs 1–3 are OS-agnostic and serve macOS unchanged; Arc 4's measurement gains an optional
`darwin/arm64` scratch conversion at the same one-run price; the option-2 emission generalizes **by
value** (`$(GoTargetOS)` ∈ windows / linux / darwin) rather than by mechanism; and N2's packaging
needs one real design note for a third flavor (§A4). Two macOS-specific facts to carry: default APFS
is **case-insensitive** like Windows, so **Linux remains the only filesystem that proves casing
correctness**; and a `GOOS=darwin` corpus's syscall layer bottoms out in `libSystem.dylib` calls
rather than `kernel32.dll` — the same P/Invoke shape and the same blittability discipline, against a
different library.

---

## 2. Findings table

| # | Area | Severity | Effort | Status |
|:--|:--|:--|:--|:--|
| F1 | Converted stdlib platform identity | S1 | 3–6 arcs | open (Arc 4+) |
| F2 | Line endings / `.gitattributes` determinism | S1 | ~~0.5 arc~~ | ✅ **done** — pin landed, renormalize blast radius measured at **0 files** |
| F3 ✅ | `packageInfoWriter` `\r\n` read-back seam | S1 | ~~1 session~~ | ✅ **done** — plus the `testConversion` insert and guard tests |
| F4 | Harness scripts + runners + utilities | S1 | 1 arc | ◑ **partial** — PowerShell tier + `BehavioralRunner` + `UpdateTestTargets` done; `BehavioralTestBase`, `PerformanceRunner`, `run-validated-sweep`, `run-behavioral-tests`, `mod-init-all` remain |
| F5 | Converter path emission on a Linux host | S1 | 1 session | open — **whole-corpus rebank**, must land with a rebank arc |
| F6 | `pathReplace` silent no-match | S2 | ~~0.5 session~~ | ✅ **done** — no-match is now loud, plus a fallback-only symlink resolve |
| F7 | `deploy-core.ps1` robocopy | S2 | 0.5 session | open — deliberately deferred (no safe end-to-end gate; see below) |
| F8 | Windows-semantic behavioral tests + time zone seam | S2 | 1 session (gate) + carried by F1 | open |
| F9 | NuGet strategy for GOOS-pinned content | S2 | plan: this doc; execution 1 session after F1 | open |
| F10 | golib / go2cs-gen | S4 | ~~trivial~~ | ✅ **done** — `golib.csproj` `USERPROFILE`→`HOME` fallback |
| F11 | `.slnx` + `.gitattributes` path casing after (b) | S2 | folded into (b) | ✅ done by rename (b) |
| F12 | `docs/README.md` dual-platform presentation | S3 | 1 session | open (sequenced last, by design) |
| F13 | Native AOT on Linux (perf suite) | S3 | 0.5 session | open |
| F14 | `set-version.ps1` (Windows PE resource) | S4 | ~~doc only~~ | ✅ **done** — `$IsWindows` guard + header note |
| F15 | WSL vs native clone (workspace topology) | S2 | 0.5 session (setup doc) | ◑ measured, see below |

### Execution log — Arc 1 + Arc 2 (partial) + Arc 3 (partial), lane r46c-linux

**F2 — what actually landed, and the number that mattered.** The pin is the block at the top of
`.gitattributes` (`*.cs`, `*.cs.auto`, `*.cs.target`, `*.csproj`, `*.slnx`, `*.props`, `*.targets`,
`src/core/**/README.md`), ordered ABOVE the `-text` blocks. `*.cs.auto` is an addition to the set
proposed in §A6.1: the review siblings are converter-emitted CRLF like everything else, and all 17
are `i/lf attr/` so they carry no risk.

The renormalization risk §A6.1 flags as "check when landing" was measured before committing rather
than after: `git add --renormalize .` over all **9,380** tracked files stages **exactly one file —
`.gitattributes` itself**. Zero corpus files, zero index rewrites. The reason is structural and
worth recording so it need not be re-derived: every blob that is NOT `i/lf` is already `-text`
(**48** `i/crlf` + **6** `i/mixed` = 54, all inside the four `-text` behavioral projects and the
`testdata` trees), and no pinned extension is auto-detected as binary. So `text` normalizes nothing
that was not already normalized, and `eol=crlf` reproduces on checkout exactly what
`core.autocrlf=true` was already producing. **A whole-tree renormalization commit is therefore NOT
required** — §A6.1's "land it as its own commit" precaution has nothing to land.

Proven, not reasoned. First as an A/B on Windows: `git -c core.autocrlf=false checkout-index` is
precisely a Linux clone's conversion, and it yields **LF** for `fmt/print.cs` (1,543 bare LFs),
`fmt.csproj`, `fmt/README.md`, `go2cs.slnx`, `version.props` and `sync/mutex.cs.auto` WITHOUT the
pin, **CRLF** for all six WITH it — while `Solitaire.cs` stays mixed (84 CRLF + 11 bare LF, its
verbatim bytes) and `gettysburg.txt` stays CRLF in both.

Then for real. A genuine `git clone` onto **ext4 inside the WSL distro**, with git's Linux defaults
(`core.autocrlf` unset → false, `core.ignorecase` unset → false), on the branch carrying the pin:

```
i/lf    w/crlf  attr/text eol=crlf    src/core/fmt/print.cs          CRLF=1543  bareLF=0
i/lf    w/crlf  attr/text eol=crlf    src/core/fmt/fmt.csproj        CRLF=144   bareLF=0
i/lf    w/crlf  attr/text eol=crlf    src/core/fmt/README.md         CRLF=256   bareLF=0
i/lf    w/crlf  attr/text eol=crlf    src/go2cs.slnx                 CRLF=829   bareLF=0
i/lf    w/crlf  attr/text eol=crlf    src/version.props              CRLF=28    bareLF=0
i/lf    w/crlf  attr/text eol=crlf    src/core/sync/mutex.cs.auto    CRLF=251   bareLF=0
i/mixed w/mixed attr/-text            .../Solitaire/Solitaire.cs     CRLF=84    bareLF=11
i/crlf  w/crlf  attr/-text            .../compress/testdata/gettysburg.txt      CRLF=29
```

**`git status` on that fresh Linux clone: 0 modified files.** That is F2 closed — the state §A6.1
describes as "100 % false-positive on Linux before the first line of converter work" is now a clean
tree. The `-text` exemption survives untouched, and the casing control passes too: `src/tests`
exists and `src/Tests` does not, which only a case-sensitive filesystem can actually prove.

One correction to §A6.1's mechanics: a trailing `!eol` does NOT unspecify an `eol` inherited from an
earlier pattern (measured on git 2.42.0.windows.2), so `git check-attr eol` still echoes `crlf` for
the `-text` paths. That echo is inert — an explicitly unset `text` suppresses all conversion — and
the renormalize measurement is the proof. Read `text: unset` as the operative answer.

**F3 remainder.** The `testConversion.go` substring-insert (§A1.3's "Conditionally" row) is closed by
normalizing the read-back `package_test_info.cs` to CRLF. That file's CRLF-shaped `strings.Contains`
was the sharper half of the bug: on an LF copy the `[GoPackage]` block is never found and gets
appended AGAIN on every run. Inert on Windows by measurement — **zero** of the 998 committed
`package_info.cs`/`package_test_info.cs` files contain a bare LF, so the normalization is the
identity there and the `contents == string(data)` early-out still short-circuits the write.
`packageInfoWriter`'s two splits now share the same helpers (`normalizeToLF` / `normalizeToCRLF` /
`splitLines`, in `writeOperations.go`). Guards live in `src/go2cs/lineEndingSeams_test.go`, each with
a negative control asserting the pre-fix form would have failed.

**F6 — what was and was NOT done.** Part 2 (loud no-match) landed in full: `pathReplace` returns a
`matched bool` and the two call sites that KNOW the directory is under GOROOT/src warn once per run.
Part 1 landed in a deliberately narrower form than §A1.2 proposes. Resolving `goRoot`/`goPath`
through `EvalSymlinks` at STARTUP would move a value that `go/packages` and `build.Default` also
read, and on Windows `EvalSymlinks` additionally canonicalizes casing and 8.3 names — so it can
desynchronize `options.goRoot` from the `pkg.Dir` the loader reports, on a host this lane could not
test. Instead the symlink resolve is a FALLBACK inside `pathReplace`, reached only when the direct
replace already failed. That fixes the symlinked-toolchain case and is provably incapable of moving
emitted bytes, because a run whose direct replace matches never executes it. Startup resolution
remains available if a real symlinked-GOROOT Linux box ever shows it is needed.

**F4 — the slice that landed.** New `src/tests/Behavioral/_paths.ps1` (dot-sourced) owns
`$IsWindowsHost`, `$ExeSuffix`, `$SepPattern`, the four roots, `Get-PathDepth` and
`Get-RelativeDisplayPath`. `check-no-regression.ps1`, `check-solution-integrity.ps1` and
`run-behavioral.ps1` consume it; `BehavioralRunner/Program.cs` and
`utilities/UpdateTestTargets/Program.cs` got the equivalent in C#
(`RuntimeInformation.IsOSPlatform`-derived exe suffix, `Path.DirectorySeparatorChar`-built bin/obj
fragments, `Path.Combine` segments instead of embedded `@"..\..\.."`).

⚠ **§A5's mechanics rule 1 is wrong as written and was not followed.** It recommends "pwsh's
multi-argument `Join-Path`" — but the Windows lane runs **Windows PowerShell 5.1**, where
`Join-Path a b c` is a hard parameter-binding error ("A positional parameter cannot be found that
accepts argument 'c'"). Adopting it would have broken the platform the corpus is banked from. The
portable form is a SINGLE child argument containing forward slashes (`Join-Path $root 'a/b/c'`),
which both 5.1 and 7+ accept on both platforms and normalize to the host separator. Anyone executing
the rest of §A5 should use that form.

The count-guard assertions §A5 asks for landed as SHAPE assertions rather than pinned counts,
because CLAUDE.md's standing instruction for this corpus is to measure rather than to decrement: CNR
fails loudly if fewer than 400 packages enumerate, and separately if every discovered package
measures the SAME path depth (which is exactly what the `-split '\\'` collapse produces, and the
condition that silently reverts the deepest-first invariant behind FALSE-GREEN route #3).
`BehavioralRunner` carries the same floor on an unfiltered run.

**F7 deliberately deferred.** `Invoke-Robocopy` carries directory (`/XD bin obj Generated .vs`) and
file (`/XF Directory.Build.props *.tests.csproj`) exclusions, and the only end-to-end gate for a
rewrite is running `deploy-core.ps1` — which stages into `%GOPATH%\src\go2cs`, a machine-global
location shared with sibling worktrees. A rewrite that cannot be proven is the throwaway the
nothing-throwaway principle warns about, so it stays open with its remedy unchanged.

**F15 — WSL, measured 2026-08-08.** The distro list has changed since §4's probe: `Ubuntu` (WSL 2,
kernel 6.18.33.2) and `docker-desktop`; the `Ubuntu-22.04` entry is gone. `dotnet`, `git` and `make`
are present; **`go` and `pwsh` are still absent**, so a conversion cannot run there (go/packages
shells out to `go`) and neither can the PowerShell instruments. Installing a toolchain was out of
scope for this lane.

That gap was worked around WITHOUT an install, and the technique is worth keeping: cross-compile
from Windows (`GOOS=linux GOARCH=amd64 go build`, and `go test -c` for the suite) and run the ELF
binaries under WSL. The converter runs natively and prints its usage with Linux-shaped defaults
(`-go2cspath /home/<user>/go2cs`, `-gopath /home/<user>/go`), confirming §A7.3's claim that the env
defaults already agree cross-platform.

The cross-compiled test binary on a Linux host: **146 PASS / 55 FAIL / 3 SKIP**, of which **50
failures are environmental** (`go command required, not found` — the missing toolchain above). The
**5 real Linux-host findings** are all path-separator ones:

| Test | Cause |
|:--|:--|
| `TestParseCoreProjectRefs` | F5 |
| `TestCollectConvertedProjectsRecoversReferencedManualPackage` | F5 |
| `TestCollectConvertedProjectsFilteredRunSkipsOutOfFilterRefs` | F5 |
| `TestIsSelfProjectReference` | F5 |
| `TestValidationPackBlockSurvivesTestsRewriteOfCorePackage` | **not F5** — the TEST hard-codes Windows absolute paths (`H:\Projects\go2cs\src\core\time\time.csproj`). A fixture-portability defect; fix with the F4 sweep, not with F5 |

The three `TestRecurse*` failures §1's F5 note lists are present but masked here as environmental —
they need the `go` toolchain before they can reach the separator bug.

**The two CRLF-template failures §1 predicts are GONE.** Every `csprojTemplate_test.go` case passes
on Linux, as do all seven new line-ending guards. That is the F2 pin plus the F3 fix doing exactly
what they were landed for, measured on a real Linux kernel rather than argued.

---

## 3. Per-area findings

### A1 — The converter on Linux

The converter is a plain Go program (`src/go2cs/*.go`, ~67 files) with no cgo and no Windows-only imports; it
cross-builds today. Four seams nonetheless make a Linux *host* wrong.

#### A1.1 — Emitted project-reference separators (F5, S1)

**Evidence.**

```go
// src/go2cs/importOperations.go:263
projectReference := filepath.Join(strings.ReplaceAll(targetDir, "/", "\\"), "\\"+packageName+".csproj")
```
and the identical form at `src/go2cs/importOperations.go:324`.

Trace it on Linux. `pkg.Dir` is `/usr/local/go/src/fmt`; `pathReplace` yields `targetDir =
"$(go2csPath)core/fmt"`; `ReplaceAll("/", "\\")` yields `"$(go2csPath)core\fmt"`; then `filepath.Join` — which
on Unix is `Clean(strings.Join(elems, "/"))` and treats `\` as an ordinary filename character — produces:

```
$(go2csPath)core\fmt/\fmt.csproj
```

On Windows the same call cleans to `$(go2csPath)core\fmt\fmt.csproj`, which is what every committed csproj
contains (`src/core/fmt/fmt.csproj`, the `<ProjectReference>` block).

**Risk.** Every stdlib reference in every csproj emitted by a Linux-hosted converter is malformed. Silent at
emission; fails at restore.

**Remedy.** Emit forward slashes unconditionally and drop the hand-rolled join:

```go
projectReference := path.Join(targetDir, packageName+".csproj")   // "/" separators, both hosts
```

MSBuild accepts `/` on Windows in every path context, and the repo already relies on this: the `.slnx`
generator emits forward slashes (`src/go2cs/solutionGenerator.go`, asserted at
`solutionGenerator_test.go:37` — `<Project Path="gen/go2cs-gen/go2cs-gen.csproj" />`). Making the csproj
emission match removes the whole separator question rather than making it host-conditional.

**Cost of the change.** It rewrites one line in every emitted `.csproj` in the corpus (~300 stdlib projects,
571 registered test projects). That is a whole-corpus rebank, so land it **with** a rebank arc, not alongside
unrelated work — or land the converter change and rebank in the same commit, per the rebank doctrine.

**On MSBuild and backslashes — the authoritative answer.** MSBuild on Unix normalizes `\` to `/` for
path-valued strings; the mechanism is `FileUtilities.MaybeAdjustFilePath` / `FixFilePath` in
`dotnet/msbuild`, applied to expanded values in evaluation and in the item/`Exists()` paths. The documented
early-returns are: running on Windows; empty value; a value that is still **unexpanded** and begins with
`$(` or `@(`; and a UNC `\\` prefix.

The practical proof that `$(Property)\literal\path` forms resolve on Linux is that the .NET SDK's own targets
depend on it. `Microsoft.Common.CurrentVersion.targets:18` (SDK 9.0.316, byte-identical on Linux) reads:

```xml
<Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"
        Condition="… and Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')" />
```

If the normalization did not cover post-expansion `$(Property)\literal` strings, no SDK-style project would
evaluate on Linux at all. So the *existing committed* csprojs are expected to build on Linux as-is; the break
is in *emission*, not consumption.

Two caveats to carry:
- The exemption is for **unexpanded** `$(`-prefixed strings. A backslash that never reaches a path context —
  a property consumed as a literal by a task, or a path handed to `File.Exists`/`Path.GetFullPath` inside a
  custom task or tool — is **not** normalized. `PerformanceRunner`/`BehavioralRunner` are exactly that case
  (§A5).
- This is a documented-behavior determination, not a measured one for this repo. **Verification recipe** for
  the executing session, one command on a Linux box with the SDK:
  `dotnet build src/core/fmt/fmt.csproj -c Debug` — it either restores the ten backslashed
  `ProjectReference`s or fails with "The referenced project … does not exist", answering it definitively in
  under a minute.

#### A1.2 — `pathReplace` case gate and silent no-match (F6, S2)

**Evidence.**

```go
// src/go2cs/importOperations.go:458
func pathReplace(subject string, search string, replace string) string {
    if runtime.GOOS == "windows" {
        searchRE := regexp.MustCompile("(?i)" + regexp.QuoteMeta(search))
        return searchRE.ReplaceAllString(subject, replace)
    }
    return strings.ReplaceAll(subject, search, replace)
}
```

Call sites: `importOperations.go:44`, `:256`, `:258`, `:321`.

**Risk.** The failure mode on Linux is not case — Go import paths and `GOROOT/src` are lowercase — it is a
**silent no-match**. If `options.goRoot` and the directory `go/build` reports differ by even one character
(a symlinked toolchain: `/usr/lib/go` vs `/usr/lib/go-1.23`; `$HOME/sdk/go1.23.1` vs a `GOROOT` env pointing
at a symlink), the replace does nothing, `targetDir` stays the absolute GOROOT path, and the emitted
`ProjectReference` becomes a machine-specific absolute path to a `.csproj` under `GOROOT` — which does not
exist. No warning is printed.

The same class applies to `isPathUnder` (`importOperations.go:449`), whose doc comment already claims
"case-insensitive on Windows" but which uses `filepath.Rel` — correct on both, but equally exact-match.

**Remedy.** Two parts, both cheap:
1. Resolve symlinks once at startup — `filepath.EvalSymlinks` on `goRoot`/`goPath`/`go2csPath` in `main.go`
   right after the env resolution block (`main.go:45-80`) — so every later comparison is against the real
   path.
2. Make the no-match **loud**: `pathReplace` returns a `matched bool`, and the two stdlib call sites
   (`:256`, `:321`) `showWarning` when a path known to be under `GOROOT/src` fails to rewrite. This is the
   same "warn loudly on an invalid root" discipline that sibling change (a) introduces for `-go2cspath`;
   extend it rather than inventing a second convention.

**Effort.** 0.5 session. No golden impact on Windows (the replace already matches there).

#### A1.3 — The `\r\n` census (F3, S1) — **LANDED**

> **Status.** Both `packageInfoWriter.go` splits are now EOL-agnostic. The census below stands as written
> with one correction the fix had to make: the **template split** classified "Yes, given the `eol=crlf` pin"
> is only safe on a tree materialized AFTER the pin. `eol=crlf` is a *checkout* attribute — adding it does
> not rewrite files already in a working tree — so a clone predating `026762932` embeds an LF template, and
> a package with no existing `package_info.cs` took the same `log.Fatal` as one with an LF copy. Both are
> normalized; the write path is untouched, so emitted bytes are the writer's CRLF either way.
>
> The rest of the census (write-only sites, the already-EOL-agnostic read-backs, the
> `testConversion.go:1583` substring-insert) is unchanged and still open where marked.

The `.gitattributes` pin added in `026762932`:

```
src/go2cs/csproj-template.xml       eol=crlf
src/go2cs/test-csproj-template.xml  eol=crlf
src/go2cs/package_info-template.txt eol=crlf
```

covers exactly the three files **embedded** at compile time. Its reasoning is sound *for those*, and
`git ls-files --eol src/go2cs/csproj-template.xml` confirms `attr/text eol=crlf`. **It does not cover every
`\r\n` seam.** The full census (all `\r\n` sites outside `_test.go`), classified:

| Class | Sites | Safe on Linux? |
|:--|:--|:--|
| **Write-only** — `WriteString`/`Join` emitting CRLF into new output | `initOrderOperations.go:460-545`, `testConversion.go:935-978`, `:1816-1841`, `projectFileWriter.go:104,142-149,290,294`, `autoSiblingOperations.go:103,137-141`, `readme.go:107`, `moduleConverter.go:587`, `solutionGenerator.go:231,393`, `packageStateOperations.go:175`, `internal/stdlibmeta/generate.go:84-95` | **Yes.** Host-independent by construction — the converter always emits CRLF, which is what makes its output deterministic. Keep them. |
| **Template split** — splits the embedded skeleton | `packageInfoWriter.go:57` | **Yes**, given the `eol=crlf` pin. |
| **Read-back split** — splits a file read off disk | **`packageInfoWriter.go:52`** | **NO.** |
| **Read-back, EOL-agnostic** | `adapterNameCollisions.go:121` (splits `"\n"`), `directiveOperations.go:58,467` and `importOperations.go:748` (`bufio.Scanner`, which drops a trailing `\r`), `stdLibConverter.go:445`, `testConversion.go:2691` (split `"\n"`), `readmeValidationBadge.go:172` (`parseProofTotals`, explicitly CRLF-tested at `readmeValidationBadge_test.go:330`) | **Yes.** |
| **Read-back, substring-insert** | `testConversion.go:1583` (`strings.Replace(contents, productionUsing, productionUsing+"\r\n"+testUsing, 1)`) | **Conditionally.** The target `package_test_info.cs` is seeded from the production file in the same run, so it is CRLF in practice; it becomes wrong if a committed LF copy is ever the input. |
| **Byte-equality** | `projectFileWriter.go:417` `needToWriteFile` | Harmless — an LF-on-disk file simply gets rewritten. Contributes to the dirty-tree symptom (F2), not to a failure. |

**The one real seam:**

```go
// src/go2cs/packageInfoWriter.go:44-52
if _, err := os.Stat(packageInfoFileName); err == nil {
    packageInfoBytes, err := os.ReadFile(packageInfoFileName)
    …
    packageInfoLines = strings.Split(string(packageInfoBytes), "\r\n")
}
```

On an LF checkout the split returns **one** element. The marker scans that follow then fail, and each of the
five sections `log.Fatal`s:

- `packageInfoWriter.go:120` — `<ImportedTypeAliases>`
- `:175` — `<ExportedTypeAliases>`
- `:381` — `<InterfaceImplementations>`
- `:528` — `<ImplicitConversions>`
- `:586` — `<TypeAccessibility>`

**Risk.** A fresh Linux clone cannot convert *any* package that already has a committed `package_info.cs` —
which is all 302 stdlib packages and all 569 behavioral packages. Hard stop on the first invocation.

**Remedy.** Split EOL-agnostically and normalize:

```go
packageInfoLines = strings.Split(strings.ReplaceAll(string(packageInfoBytes), "\r\n", "\n"), "\n")
```

The writer at `packageInfoWriter.go:608` already appends `"\r\n"` per line, so output stays byte-identical on
Windows and becomes byte-identical on Linux. Fix the `testConversion.go:1583` insert the same way (search for
both forms, insert the platform-independent `"\r\n"`). **Zero golden impact** — verified by construction: the
read path changes, the write path does not.

**Effort.** 1 session including a converter unit test that feeds `writePackageInfoFile` an LF fixture.

#### A1.4 — `-platforms`, filename constraints, and Windows-only tools (informational)

`-platforms os/arch` (`main.go:98`, default `runtime.GOOS + "/" + runtime.GOARCH`) is threaded into every
loader:

- `conversionDriver.go:83` — `cfg.Env = append(os.Environ(), "GOOS=…", "GOARCH=…")`
- `stdLibConverter.go:243-244`, `:332-333`
- `moduleConverter.go:157`
- `testAliasShadowOperations.go:58-59` — `buildContext.GOOS/GOARCH`
- `testConversion.go:372-373`, `:1010-1011`, `:4389`

Filename build constraints are evaluated against the same target: `directiveOperations.go:357-408` implements
go/build's `goodOSArchFile` rule over `knownGOOS`/`knownGOARCH` tables. The mechanism is complete and
correct — it is what makes a `GOOS=linux` conversion *possible* (§A2), and it means the default target
follows the converter's host, which is the trap: **running `go2cs -stdlib` on Linux silently produces a
different corpus than running it on Windows.** Any Linux lane must pass `-platforms` explicitly rather than
relying on the default.

No Windows-only external tool is invoked by the converter. Child processes are `git` (`testConversion.go:3757`),
`go`, and `dotnet` (`:3807`, `:3810`, `:4189`, `:4191`), all through
`runCommandWithTimeout` (`testConversion.go:4381`), which uses `exec.CommandContext` — portable.

---

### A2 — The converted standard library's platform identity (the biggest finding)

#### A2.1 — What `src/core` actually is

Measured on the committed tree:

| Probe | Result |
|:--|:--|
| `find src/core -name '*_windows*.cs'` | **87** |
| `find src/core -name '*_linux*.cs'` | **0** |
| `find src/core -name '*_unix*.cs'` | 2 (`net/dnsclient_unix.cs`, `os/signal/signal_unix.cs` — GOOS-neutral filenames, included by constraint) |
| `find src/core -name '*_amd64*.cs' -o -name '*_arm64*.cs'` | 17 |

`src/core/internal/goos/zgoos_windows.cs:6` compiles the answer in as a constant:

```csharp
public static readonly @string GOOS = @"windows"u8;
public static UntypedInt IsLinux => 0;
```

`src/core/syscall/` has **no** non-Windows source at all: `dll_windows.cs`, `env_windows.cs`,
`exec_windows.cs`, `security_windows.cs`, `syscall_windows.cs`, `types_windows.cs`,
`types_windows_amd64.cs`, `wtf8_windows.cs`, `zerrors_windows.cs`, `zsyscall_windows.cs`. Same for `os`
(`dir_windows.cs`, `file_windows.cs`, `path_windows.cs`, `stat_windows.cs`, `sys_windows.cs`,
`executable_windows.cs`, `exec_windows.cs`, `types_windows.cs`) and for `net` (14 `_windows` files).

#### A2.2 — What that means at runtime

Only four files in the whole corpus carry P/Invoke:

```
src/core/syscall/dll_windows.cs
src/core/syscall/exec_windows.cs
src/core/syscall/zsyscall_windows_impl.cs
src/core/time/time_impl.cs
```

Three of them are load-bearing on Linux:

1. **All I/O.** `syscall.WriteFile` (`zsyscall_windows.cs:1365`) calls
   `Syscall6(procWriteFile.Addr(), …)`; `procWriteFile` is `modkernel32.NewProc("WriteFile")`
   (`zsyscall_windows.cs:159`); `LazyProc.Addr()` → `LazyDLL.Load()` → `LoadLibraryExW` /
   `GetProcAddress` P/Invoke into `kernel32.dll` (`dll_windows.cs`, the hand-owned native replacement).
   `os.Stdout` is `NewFile((uintptr)syscall.Stdout, "/dev/stdout")` (`os/file.cs:79`).
   **Therefore: a converted `fmt.Println("hi")` throws `DllNotFoundException("kernel32.dll")` on Linux.**
   That is 514 of the 514 behavioral stdout comparisons.
2. **Local time.** `time.initLocal` (`time/zoneinfo_windows.cs:251`) calls
   `syscall.GetTimeZoneInformation` (`zsyscall_windows_impl.cs:91`, `DllImport("kernel32.dll")`). Any
   converted program touching `time.Local` / `Weekday()` faults.
3. **Directory enumeration.** `FindFirstFileW`/`FindNextFileW` (`zsyscall_windows_impl.cs:184,187`),
   `Process32FirstW`/`Process32NextW` (`:282,285`).

Two things are already portable and should not be re-litigated:

- `time_impl.cs` `runtimeNano()` / `now()` are `Stopwatch` + `DateTime.UtcNow` — portable.
- The one P/Invoke in `time_impl.cs` (`CreateWaitableTimerExW`, `:970`) sits behind
  `if (!OperatingSystem.IsWindows()) return null;` (`:931`) with a documented coarse-wait fallback. `time.Sleep`
  and the timer machinery already work on Linux.

`golib` (§A3) is clean, so the *runtime* layer is not the problem — the **converted stdlib** is.

#### A2.3 — The `go test` corollary

`runCommandWithTimeout` (`testConversion.go:4381-4392`) exports `GOOS`/`GOARCH` from `-platforms` to **both**
sides of the differential oracle, including the `go test -json -count=1` baseline
(`testConversion.go:4189`). `go test` for `GOOS=windows` on a Linux host builds a Windows test binary it
cannot execute. **The Phase-4 `-tests` pipeline and `run-validated-sweep.ps1` cannot run against the
Windows-target corpus on Linux, at all.** They are not slow or flaky there; they are unavailable. Only a
`GOOS=linux` corpus makes them meaningful on Linux, and that corpus's validation roster starts from zero.

#### A2.4 — Options for a Linux-target standard library

| Option | Shape | Pros | Cons |
|:--|:--|:--|:--|
| **1. Parallel corpus** — a second conversion tree, e.g. `src/core-linux/` or `src/core/<goos>/` | `go2cs -stdlib -platforms linux/amd64` into its own root; its own `.slnx`; its own validation roster | Clean, mechanical, uses the machinery that exists today; both corpora regenerable; no runtime dispatch anywhere; Windows lane untouched | Doubles the corpus (~2,500 files → ~5,000); doubles rebank cost; **resurrects the two-tree doctrine** the 2026-08-01 consolidation deleted — both trees emit `namespace go` with `<pkg>_package` partials and would collide if ever referenced together |
| **2. Per-platform `Compile` items in one csproj** | One `src/core/<pkg>` holding `file_windows.cs` **and** `file_linux.cs`, with `<Compile Include>` conditioned on `$(GoTargetOS)` | One tree, one set of package IDs, one `.slnx`; the natural MSBuild idiom; matches how Go itself organizes the source | Requires the converter to emit BOTH platforms' files into one directory and to emit conditioned item groups — a real converter feature; `package_info.cs`, `package_init.cs` and the init-order file are per-closure and would need per-platform variants too; the two conversions' *production* `.cs` already differ under a wider closure (the standing `Δio`-alias restore), so merging them is not purely additive |
| **3. Runtime dispatch** — one assembly, `OperatingSystem.IsWindows()` branches | Hand-own the ~10 syscall seams against managed .NET APIs (`FileStream`, `TimeZoneInfo`, `Directory.Enumerate…`) and delete the platform files | One assembly, one package, genuinely portable; smallest published surface | Abandons literal conversion at exactly the layer where Go's semantics are subtlest (errno mapping, `FileMode`, path semantics, `os.SameFile`); the hand-owned surface grows without bound; every Go-side fix must be re-hand-applied; contradicts the project's "faithful conversion, hand-own only where a literal conversion *cannot* work" rule |
| **4. Do nothing; Linux = converter + harness only** | Ship a Linux-capable converter and harness; the *converted stdlib* stays Windows-target | Zero corpus cost; unblocks Linux contributors on converter work immediately | A Linux user cannot run the behavioral output comparisons or any Phase-4 validation — the two instruments that matter most |

**Recommendation: 4 now, then 1 — with 2 as the eventual destination.**

- Arcs 1–3 of §1 deliver **option 4** as a by-product and cost nothing extra. Land that first; it is real
  value (a Linux contributor can fix converter defects and run `check-no-regression`-style transpile gates
  against the *behavioral* corpus, which is platform-neutral Go).
- Then **measure** option 1 before committing to it: convert `-stdlib -platforms linux/amd64` into a scratch
  root and bucket the compile errors. This is one `go2cs` run (~3–4 min, per the CLAUDE.md budget table) plus
  a solution build. It answers the only question that matters — *how far is a Linux corpus from compiling?* —
  for the cost of an afternoon. Do **not** plan the tree layout before that number exists.
- Prefer option **1** for the first Linux corpus because it needs no converter feature and keeps the two
  lanes provably independent. Accept the two-tree cost knowingly: the 2026-06-25 doctrine was retired because
  the trees were *the same platform*, and this pair is not — a build references exactly one GOOS, which is
  the same invariant, differently enforced.
- Hold option **2** as the destination once a Linux corpus exists and is measured; converge onto it when the
  duplication cost is felt, not before. Option **3** is explicitly rejected: it is the shortcut, and the
  nothing-throwaway principle says the general fix (target the platform properly) beats hand-owning the
  seams.

**Ruling (user, 2026-08-06) — option 2 is the goal, not an eventual convergence.** The two-binary shape —
one package ID per Go package, `net9.0-windows` + `net9.0` flavors selected by the consumer's TFM (§A4 N2) —
is confirmed as the long-term solution, and the session executing this plan should treat **reaching it as
its goal**. This amends the *pacing* above, not the *path*: Arc 4's measurement still comes first (the tree
layout is still not designed before that number exists), and an option-1-style scratch conversion remains
the measurement instrument — but option 1 is a stepping stone, never a shipped state. No second solution
file, no second package set, no re-lived two-tree era lands on `master`; the lane is done when the option-2
emission (both platforms' files in one `src/core/<pkg>`, conditioned item groups, per-platform
`package_info`/`package_init` variants) and the N2 packaging exist. Scope is the **big three**
(Windows/Linux/macOS — same ruling): the conditioned-emission mechanism carries `GOOS=darwin` as a third
file-set by value, not by new machinery, with Linux the lead lane and darwin following once it is proven.
No wider GOOS target is in scope.

#### A2.5 — The behavioral corpus's Go side

Checked: the 569 behavioral Go packages are ordinary portable Go (`fmt`, `strings`, `sort`, channels,
generics). Two exceptions embed platform-specific behavior and are the only goldens that cannot survive a
platform change:

- **`src/tests/behavioral/LocalTimeZone`** — prints `t.Format("… MST")`, `t.Zone()`, `Weekday()`. The header
  comment states the design explicitly: the machine-varying parts "vary IDENTICALLY for Go and the converted
  C#", which is what makes the output comparison meaningful. That premise **holds on Linux only if the C#
  side reads the same zone database as Go** — which requires the `GetTimeZoneInformation` seam to have a
  Linux counterpart. Today it does not.
- **`src/tests/behavioral/FindFirstFileData`** — directly exercises `FindFirstFileW`/`FindNextFileW` through
  `path/filepath.EvalSymlinks`'s Windows `normBase`. Windows-only by construction.

`StdLibInternalAbi` is not platform-specific in *semantics*, but it is `-text`-marked (§A6) because its
program's output depends on a multi-line literal's exact bytes.

**Remedy for the Linux lane:** gate both by target platform in the runners' project enumeration (a
`platform: windows` marker in `package_info.cs` alongside `[GoTestMatchingConsoleOutput]`, or a sibling
`.platform` file) rather than deleting or `#if`-ing them. They are guards for real, still-open defect classes
on Windows.

---

### A3 — golib and the source generators

**`src/core/golib/` (50 C# files) is clean.** A full scan for `DllImport`, `LibraryImport`, `kernel32`,
`RuntimeInformation`, `OSPlatform`, `Environment.OSVersion`, `Process.Start`, `Path.DirectorySeparator` and
raw backslashes returns **zero** platform-coupled hits. The only `Marshal`/`MemoryMarshal` uses are
`Marshal.SizeOf` for `ж<T>` pinning (`ж.cs:361-375`) and `MemoryMarshal.GetReference`/`GetArrayDataReference`
(`sstring.cs:94`, `ж.cs:223,270`) — all RID-agnostic managed intrinsics. `builtin.cs:61` sets
`Console.OutputEncoding = Console.InputEncoding = Encoding.UTF8`, supported on .NET for Linux. `print`/`println`
go straight to `Console.Error` (`builtin.cs:1629,1638`), so a behavioral program that uses only the Go
built-ins — not `fmt` — would in fact run on Linux today.

**`src/gen/go2cs-gen/` is clean.** `netstandard2.0`, `Microsoft.CodeAnalysis.CSharp` 4.10.0, no file I/O, no
paths. The `_AddGeneratorToPackage` target packs to `analyzers/dotnet/cs` — RID-agnostic.

**The one triviality (F10, S4).** `src/core/golib/golib.csproj` carries

```xml
<PropertyGroup Condition="'$(go2csPath)'==''">
  <go2csPath Condition="'$(Configuration)'!='Debug'">$(USERPROFILE)\go2cs\</go2csPath>
```

**without** the `USERPROFILE`→`HOME` fallback that the converter's templates emit
(`src/go2cs/csproj-template.xml:27-28`, `test-csproj-template.xml:49-50`). A non-Debug build of `golib`
outside a solution context resolves `$(go2csPath)` to `\go2cs\` on Linux. Add the same two-line fallback.
`go2cs-gen.csproj` needs nothing (it never reads `$(go2csPath)`).

**The `time`/`syscall` seam and the board's census.** `docs/phase4/BOARD-next-validation-candidates.md`
records 9 further syscall wrappers that pass a non-blittable struct by address, deliberately unfixed because
nothing exercises them. Read against Linux, that census says something sharper than it does against Windows:
**every one of those wrappers is a `kernel32` entry point.** A Linux corpus does not need them fixed — it
needs them *absent*, replaced by the `_linux` files a `GOOS=linux` conversion selects. The census is
therefore a Windows-lane debt, and the Linux lane inherits an entirely different one (Linux's `syscall`
package is generated `zsyscall_linux_amd64.go` calling `Syscall`/`RawSyscall` trampolines, which have no
managed equivalent at all and will need their own hand-own strategy). **Scope this honestly in Arc 4's
measurement**: the Linux `syscall` package is likely the single hardest package in the Linux corpus, harder
than its Windows counterpart, because Windows at least routes through named DLL exports that P/Invoke can
reach.

**Effort.** golib/gen port cost: essentially zero (one csproj edit). The syscall strategy is carried by F1,
not by this section.

---

### A4 — NuGet for Linux consumers

#### What is packed today

`src/push-nuget.ps1` packs **`src/go2cs-stdlib.slnx` only** (~304 projects: the converted stdlib, the
hand-owned `unsafe`/`testing`, `core/golib` → `go.lib`, `gen/go2cs-gen` → `go.gen`). Version comes from
`src/version.props` (`GoStdLibVersion` 1.23.1 · `GoBuildNumber` 3 → `Version` 1.23.1.3).

Per-package pack inputs, from `src/core/fmt/fmt.csproj`:
- `<TargetFramework>net9.0</TargetFramework>`, **no `RuntimeIdentifier`, no `RuntimeIdentifiers`**
- `PackageId = go.$(AssemblyName)`, `PackageIcon`, `PackageReadmeFile = README.md`
- the validation-pack block: `<GoValidationProofFile>$(go2csPath)..\docs\validation\$(GoStdLibVersion).$(GoBuildNumber)\<dot-id>.md</GoValidationProofFile>` packed as `VALIDATION.md` under an `Exists()` guard
- the README carries a version-pinned green badge that `push-nuget.ps1:161-240` snapshots (write-once
  `docs/validation/<version>/`), retargets, and then **re-verifies** against the frozen proof page

#### Verdict

**Packaging is already RID-agnostic and needs no change.** Nothing platform-specific is packed: no `runtimes/`
folder, no native asset, no RID in any `TargetFramework`. `go.gen` packs to `analyzers/dotnet/cs`, which is
RID-agnostic by definition. A Linux consumer restores these packages successfully today.

**The problem is truthfulness, not mechanics.** `go.os`, `go.syscall`, `go.net`, `go.time`, `go.path.filepath`
and their closure are published under platform-neutral IDs while containing Windows-only code that throws
`DllNotFoundException` on Linux. A Linux user of `-recurse=nuget` gets a build that restores, compiles, and
fails at first output.

#### Strategy options

| Option | Shape | Assessment |
|:--|:--|:--|
| **N1 — Per-GOOS package IDs** | `go.os` stays Windows; add `go.linux.os` (or `go.os.linux`) | Explicit and unambiguous, but doubles ~300 package IDs on nuget.org and forces `-recurse=nuget` to rewrite every reference by target. Rejected: the ID sprawl is permanent and public. |
| **N2 — Multi-TFM / RID packaging** | One `go.os` with `lib/net9.0-windows/` and `lib/net9.0/` (or RID-specific `runtimes/`) | This is what the .NET ecosystem does, and NuGet/MSBuild resolve it automatically with no consumer change. `net9.0-windows` is a real, first-class TFM. Requires the converter to build the same package from **two** corpora and the pack step to combine them — i.e. it depends on F1 option 1 or 2 being done first. **This is the destination.** |
| **N3 — Single package, runtime checks** | One assembly branching on `OperatingSystem.IsWindows()` | Inherits every objection to F1 option 3. Rejected. |
| **N0 — Disclose, do not repackage** (interim) | Keep one package set; state the platform in `PackageDescription`, in each README, and in `PackageTags` | Zero cost, honest, reversible. |

**Recommendation.** **N0 immediately, N2 as the destination, gated on F1.** N2's destination status is a
**user ruling** (2026-08-06, recorded in §A2.4), not a proposal — the session executing the stdlib lane
targets it directly.

Concretely, for N0 (one session, no F1 dependency):
1. Add `<PackageTags>…;windows</PackageTags>` and a one-line platform statement to `PackageDescription` in
   the converter's `csproj-template.xml` — corpus-wide, one converter change, one rebank.
2. Add the same statement to the per-package README emitter (`src/go2cs/readme.go`), beside the validation
   badge, so it is visible on the nuget.org package page.
3. Note it once in `docs/README.md` §Requirements (§A8).

For N2, when F1 lands: `push-nuget.ps1` grows a second build+pack pass over the Linux solution and a merge
step, and `version.props` gains nothing (one version spans both TFMs, which is correct — they are the same
converted Go release). The validation-pack block needs a per-TFM proof page or a merged one; prefer
**per-TFM proof pages** (`docs/validation/<version>/<dot-id>.windows.md` / `.linux.md`) because the whole
point of the proof is that it describes the binary being shipped, and the two binaries validate differently.
That also means `docs/validation/current/` gains a platform dimension — plan for it in the same change rather
than retrofitting.

**Third flavor (macOS — ruled into scope 2026-08-06).** The TFM trick covers only Windows-vs-neutral: there
is no `net9.0-linux`, and `net9.0-macos` is the Catalyst/AppKit workload TFM, not a console-library target.
A three-flavor `go.os` therefore ships the linux and darwin flavors as **RID-specific runtime assets**
(`runtimes/linux-x64/lib/net9.0/`, `runtimes/osx-arm64/lib/net9.0/`, `runtimes/osx-x64/…`) under the neutral
`net9.0`, with `lib/net9.0` as the compile-time asset. That reopens the compile-surface question the
two-flavor design dodged: `syscall`'s **public surface differs between linux and darwin**, and one neutral
compile assembly cannot truthfully carry both. The executing session must design this seam — candidates: a
unix-intersection reference assembly; per-GOOS packages for `go.syscall` alone (the only package where the
sprawl is honest); or API-unifying shims. Flagged, not solved, per the do-not-get-carried-away ruling.

---

### A5 — Harness and scripts under pwsh on Linux

PowerShell 7 (`pwsh`) runs every one of these scripts' *language* constructs on Linux. What breaks is
**data**: backslash path literals, `.exe` suffixes, RIDs, and process names.

#### Per-instrument assessment

| Instrument | What breaks | Verdict |
|:--|:--|:--|
| **`src/tests/behavioral/check-no-regression.ps1`** | `:30` `Join-Path $PSScriptRoot "..\..\.."`; `:31` `"src\go2cs"`; `:32` `"bin\go2cs.exe"`; `:68` `-notmatch '\\(bin\|obj)(\\\|$)'` (**silently matches nothing** on Linux → `bin`/`obj` dirs are transpiled); `:70` `($_.FullName -split '\\').Count` (**silently returns 1 for every path** → the deepest-first sort collapses, reverting the FALSE-GREEN-#3 fix); `:85` `.TrimStart('\')` | Parameterize. High value: this is the authoritative drift instrument. |
| **`src/tests/behavioral/run-behavioral.ps1`** + **`BehavioralRunner/Program.cs`** | `run-behavioral.ps1:32,33` backslash + `BehavioralRunner.exe`. Program.cs `:97-98` `Path.Combine(AppContext.BaseDirectory, @"..\..\..\..")` — .NET does **not** normalize `\` on Unix, so this is one directory name, and `GetFullPath` yields nonsense; `:101` `go2cs.exe`; `:269-270` `d.Contains(@"\bin\")` (**silent** — bin/obj enumerate as packages); `:484` `.dll` ok, `:557,558` `{p}.exe` for both C# and Go binaries | Parameterize. `Path.DirectorySeparatorChar`, `RuntimeInformation.IsOSPlatform`, and a `ExeSuffix` constant cover it. |
| **`src/tests/behavioral/BehavioralTests/BehavioralTestBase.cs`** (MSTest) | `:38` `PublishProfile = "win-x64"` hard-coded RID; `:55,57,63` `$@"bin\{…}\"`; `:73,83` `bin\` + `go2cs.exe`; `:78` `BinOutput.Split(@"\", …)[^1]` (**silent** — `NetVersion` becomes the whole path); `:268` `\obj\`; `:279,300` `.exe` + `-r {PublishProfile}` | Parameterize. Lower priority than the standalone runner (which is the preferred instrument). |
| **`src/tests/behavioral/check-solution-integrity.ps1`** | Only `:34` `Join-Path … "..\.."`. `:43` `.Replace('\','/')` is already a no-op on Linux. `:49` regex `Path="(Tests/Behavioral/…` must become `tests/behavioral/` with rename (b) | Nearly portable. One line. |
| **`src/tests/behavioral/run-behavioral-tests.ps1`** | `:38,39,49` backslash literals; `:51-56` `Get-Process -Name … \| Stop-Process` — works on Linux but the **path-scoped `Where-Object` guard must survive the port** (the concurrent-session-kill hazard is identical on Linux) | Parameterize. |
| **`src/run-validated-sweep.ps1`** | `:29,30` `docs\ValidatedTestPackages.md`, `go2cs\bin\go2cs.exe`; `:62` `go build -o bin\go2cs.exe`; `:92,93` `('core\' + ($pkg -replace '/', '\'))` — **the import-path-to-directory mapping is backslash-based**; `:132` git pathspec already forward-slash | Parameterize — but note it is **inert on Linux until F1** (§A2.3): the sweep cannot run against a windows-target corpus. |
| **`src/tests/performance/run-performance.ps1`** + **`PerformanceRunner/Program.cs`** | `run-performance.ps1:38,39,52` backslash + `.exe`. Program.cs `:133` `go2cs.exe`; `:913-915` `{project}.exe` × 3 variants; `:871` `PROCESSOR_IDENTIFIER` (Windows-only env → "unknown CPU" in the README environment line). The **vswhere/`link.exe` block is already safe**: `:410-411` `Environment.SpecialFolder.ProgramFilesX86` returns `""` on Linux, `Directory.Exists` is false, and the PATH prepend is skipped (`:417`). `Directory.Build.targets` sets `RuntimeIdentifier=$(NETCoreSdkRuntimeIdentifier)`, which resolves to `linux-x64` correctly | Parameterize; Native AOT on Linux additionally needs `clang` + `zlib1g-dev` on the box (§F13) — document as a prerequisite, do not script the install. |
| **`src/deploy-core.ps1`** | `:48-70` **`robocopy`** — no Linux equivalent; `:78` `'src\go2cs'`; `:83` `.TrimEnd('\')`; `:101,111` backslash sources. `:145` `.TrimStart('\','/')` already handles both | Rewrite `Invoke-Robocopy` as a portable `Copy-Item -Recurse` + exclusion filter (or `robocopy`-on-Windows / `rsync`-on-Linux behind the same function signature). Prefer the pure-PowerShell copy: one implementation, no external tool on either OS. |
| **`src/check-symbol-sync.ps1`** | Nothing. Uses forward slashes throughout, `go run`, `git -C` | **Portable as written.** |
| **`src/clean-bin.ps1`** | Nothing structural. `Read-Host` makes it interactive (already true on Windows); `-Include "bin","obj","Generated"` is case-**sensitive** on Linux, and all three match MSBuild's actual casing | **Portable as written.** |
| **`src/set-version.ps1`** | `ReplaceInFiles.exe`, `go-winres`, `cmd /c "pause"` | **Windows-only by nature** — it stamps a Windows PE version resource. Do **not** port; add a one-line guard (`if (-not $IsWindows) { throw … }`) and say so in its header. |
| **`src/tests/behavioral/mod-init-all.ps1`** | `del go.mod` / `del *.csproj` (aliases resolve on pwsh Linux) | Portable; a maintenance one-off, low priority. |
| **`src/utilities/UpdateTestTargets/Program.cs`** | `:8` `const string RootPath = @"..\..\..\..\..\"`; `:11,41,91,117` `Tests\Behavioral\…` | **Hard break, and it is a required step in the documented add-a-test flow.** Must be ported with the runners. |
| **`src/tour/scripts/start.ps1`** | `:15` `"..\.."`; `:24` `bin\tour.exe` | Already has a **bash twin** (`src/tour/scripts/start.sh`, 19 lines) — see the strategy note below. |
| **`.bat`/`.cmd` launchers** (11 under `src/`) | `cmd.exe` only | Leave them. They are convenience wrappers around the `.ps1`; a Linux user invokes the `.ps1` directly. |

#### Strategy: parameterize, do not write bash twins

**Recommend a single strategy — parameterize the existing PowerShell, and require `pwsh` on Linux.**
Rationale, in order of weight:

1. **The logic is not trivial.** `check-no-regression.ps1`'s deepest-first recursive enumeration,
   `run-validated-sweep.ps1`'s roster parsing and drift classification, and `push-nuget.ps1`'s write-once
   proof snapshot with badge re-verification each encode hard-won invariants. A bash twin means two
   implementations of each invariant and a guaranteed drift — the exact failure this repo names
   *nothing-throwaway*.
2. **The breakage is data, not language.** Every finding above is a path literal, a suffix, or a RID. `pwsh`
   on Linux runs the constructs already.
3. **`src/tour/scripts/start.sh` is the counter-example that proves the rule.** It is a 19-line
   `go install` + `go run` wrapper with essentially no logic — a twin is cheap and stays correct. Nothing
   else in the inventory is that thin. Keep it; do not generalize from it.

**Mechanics of the parameterization** — three small, uniform rules, applied once:

- Replace every `Join-Path $x "a\b"` with `Join-Path $x 'a/b'` — a SINGLE child argument carrying
  forward slashes, which `Join-Path` accepts and normalizes on both platforms.
  ⚠ **Do NOT use pwsh's multi-argument `Join-Path $x 'a' 'b'`** (this bullet originally recommended
  it). `-AdditionalChildPath` is PowerShell 6+; the Windows lane runs **Windows PowerShell 5.1**,
  where the three-argument form is a hard parameter-binding error. Measured 2026-08-08 on 5.1.26100.
- Introduce one shared `src/tests/behavioral/_paths.ps1` (dot-sourced) defining `$ExeSuffix` (`''` on
  non-Windows), `$Sep`, and the repo/src/behavioral roots — so the four scripts that recompute them stop
  disagreeing.
- In the two C# runners and `UpdateTestTargets`, replace `@"..\..\.."` with `Path.Combine("..", "..", "..")`,
  `@"\bin\"` with `Path.DirectorySeparatorChar`-built fragments, and hard-coded `.exe` with a
  `RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : ""` constant. Replace `PublishProfile =
  "win-x64"` with `RuntimeInformation.RuntimeIdentifier`.

**Guard the silent ones with a test.** The three `-split '\\'` / `.Contains(@"\bin\")` / `Split(@"\")` sites
fail *without an error*. Add an assertion to each enumerator that the discovered package count is within the
expected band (569 today), so a separator regression on either platform fails loudly instead of quietly
transpiling nothing.

**Effort.** 1 arc. The scripts are ~1 session; the two runners + `UpdateTestTargets` are ~1 session; the
count-guard tests ~0.5.

---

### A6 — Test-corpus portability, line endings, and the `-tests` pipeline

#### A6.1 — The line-ending problem is the first thing a Linux clone hits (F2, S1)

Measured on the current tree:

```
$ git ls-files --eol src/core/fmt/print.cs src/core/fmt/fmt.csproj src/go2cs/csproj-template.xml
i/lf    w/crlf  attr/                  src/core/fmt/print.cs
i/lf    w/crlf  attr/                  src/core/fmt/fmt.csproj
i/lf    w/crlf  attr/text eol=crlf     src/go2cs/csproj-template.xml
```

The repository has **no `* text=auto`**, and the committing machine runs `core.autocrlf=true`, so git
normalized every text blob to **LF in the object store** while the Windows working tree sees CRLF. On a
Linux clone with git's default `core.autocrlf=false`, the working tree gets **LF** — and the converter
deterministically emits **CRLF**. Consequences, in order of arrival:

1. The converter `log.Fatal`s on the first package (§A1.3, F3).
2. Once that is fixed, every regenerated file differs from its checkout by line endings, so
   `check-no-regression.ps1`'s `git status` reports the entire behavioral corpus as changed — a total
   false positive, before any converter work.
3. `needToWriteFile` (`projectFileWriter.go:417`) rewrites every csproj on every run.

**What is already safe.** Golden *comparison* is line-ending-insensitive:
`TargetComparisonTests.FileMatch` and `BehavioralRunner.FilesEqual` both strip CRs. So `.cs.target`
comparisons would pass on Linux regardless. It is `git status` — the thing CNR actually asserts on — that
does not.

**What needs exact bytes.** The `-text` set in `.gitattributes`, for the runtime-correctness reason (a
compiled program observing a multi-line literal's newlines):
`src/tests/behavioral/{Solitaire,SortArrayType,StdLibInternalAbi,UntypedConstWideMask}/*.cs` and `*.cs.target`,
plus `src/core/**/testdata/**` and `src/core/**/testdata/*.base64` (Go ships LF; a converted suite reads them
byte-for-byte). `-text` means git stores and checks out the bytes verbatim on every platform — already the
correct, platform-independent choice. Keep it. Note the r40 corollary that `-text` paths show a **real,
non-empty `--numstat`** on a pure CRLF flip (`compress/testdata/gettysburg.txt`, `i/crlf`), so a Linux sweep
must test CR-stripped equality against `HEAD` rather than trusting `--numstat` — same rule as on Windows.

**The `.gitattributes` strategy that makes a fresh Linux clone deterministic end-to-end.** The converter is
the source of truth and it emits CRLF unconditionally. Therefore **pin the emitted artifact types to CRLF for
every platform**, exactly as the three templates already are:

```gitattributes
# The converter emits CRLF unconditionally (see initOrderOperations.go, projectFileWriter.go,
# packageInfoWriter.go, readme.go, solutionGenerator.go). Pinning eol=crlf makes every checkout —
# Windows, Linux, autocrlf on or off — materialize the exact bytes the converter regenerates, so
# `git status` after a re-transpile is a truthful drift signal on every platform.
*.cs        text eol=crlf
*.cs.target text eol=crlf
*.csproj    text eol=crlf
*.slnx      text eol=crlf
*.props     text eol=crlf
*.targets   text eol=crlf
src/core/**/README.md text eol=crlf
```

Order matters: these must come **before** the existing `-text` lines in the file, so the `-text` marks win
for the four runtime-sensitive projects and the `testdata` trees (last matching pattern wins in
`.gitattributes`).

This is the *same* end state the Windows lane has today, made explicit rather than depending on a
per-machine `core.autocrlf`. It also removes the CRLF-phantom class from sweeps on **both** platforms — the
"modified with an empty `--numstat`" noise documented in CLAUDE.md exists precisely because in-string LFs get
smudged on checkout; with `eol=crlf` the smudge is the committed state and re-emission matches it.

**Risk to check when landing:** a one-time whole-tree renormalization (`git add --renormalize .`) will show
every file as touched. Land it as its own commit, with `git diff --stat` before/after and a CR-stripped
equality proof, and do it **before** Arc 2 so no converter change is buried under it.

**Effort.** 0.5 arc, mostly verification.

#### A6.2 — The `-tests` pipeline's Windows assumptions

Better than expected. `runCommandWithTimeout` (`testConversion.go:4381`) drives `dotnet build`,
`dotnet run --project`, `go test -json` and `git rev-parse` through `exec.CommandContext` — no `.exe`, no
shell, no `taskkill`. Fixture staging uses `filepath.FromSlash` (`testConversion.go:3516`, `:3646`), and the
hand-owned host normalizes separators explicitly (`src/core/testing/TestHost.cs:253,262`:
`relativePath.Replace('/', Path.DirectorySeparatorChar)`).

The three real issues are elsewhere:

1. **§A2.3** — the pipeline exports `GOOS=windows` to `go test` on both sides. Unavailable on Linux until
   there is a Linux corpus.
2. **One disclosed divergence is host-specific.** `os_test.TestRemoveAllWithExecutedProcess` is disclosed
   because a .NET **apphost** cannot be single-file-copied (`testConversion.go:2319-2325`,
   `hostfxr 0x8000809a`). The apphost mechanism is identical on Linux, so the disclosure survives — but its
   *signature* (the error text) will not. Disclosure manifests are signature-pinned by design, so a Linux
   corpus needs its own `go2cs_test_disclosures.json` per package, not a shared one.
3. **`src/core/.gitignore`** covers the pipeline's regenerated inputs (staged `*.go`,
   `go2cs_test_manifest.json`, `go2cs_test_comparison/`). Verify it survives the casing normalization and
   that no pattern depends on Windows path casing.

---

### A7 — Runtime on Linux (converted net9.0 code)

Beyond the stdlib itself (§A2), five smaller runtime concerns:

1. **Time zones.** `time.initLocal` faults (§A2.2). Note the *shape* of the eventual fix: Go on Linux reads
   `/etc/localtime` and the IANA database through `time/zoneinfo_read.go`, which is **pure Go** and converts
   cleanly — so a `GOOS=linux` corpus likely needs **no** hand-own for `time` at all, where the Windows
   corpus needed one. That is an argument for F1 option 1 over option 3.

2. **Filesystem case sensitivity.** The `$(go2csPath)core\<pkg>` references are **not** at risk: `core` and
   every Go import-path segment are lowercase on disk and in the emitted reference (derived from the GOROOT
   directory names), and the csproj filenames match (`core/unicode/utf8/unicode.utf8.csproj`,
   `core/internal/abi/internal.abi.csproj`). The **actual** exposure was the two capitalized prefixes in
   `src/go2cs.slnx` — of its 679 `Path=` entries, `Tests/` and `Utilities/` were the only ones that changed
   under rename (b). **Rename (b) has landed** (`d3223d252`, 2026-08-06) and updated all of them in the same
   atomic commit: the `Path=` entries, the `check-solution-integrity.ps1` regex, and the four
   `.gitattributes` `src/tests/Behavioral/…` patterns (`git check-attr` re-verified at the new paths). On a
   case-insensitive Windows FS a missed reference would still build; on Linux it fails immediately — which
   makes the first Linux build the *best available check* that the rename sweep was complete.

3. **Env defaults already agree.** The converter defaults `-go2cspath` to
   `filepath.Join(os.UserHomeDir(), "go2cs")` (`main.go:71-80`) → `~/go2cs` on Linux. The csproj template
   defaults `$(go2csPath)` to `$(USERPROFILE)\go2cs\` **with** `<USERPROFILE>$(HOME)</USERPROFILE>` when
   `USERPROFILE` is empty (`csproj-template.xml:27-33`) → `$HOME/go2cs/`. The pair is already coherent
   cross-platform; only `golib.csproj` is missing the fallback (§A3).

4. **`GOPATH`.** `deploy-core.ps1` derives the deploy root from `go env GOPATH` (`:74-78`) →
   `~/go/src/go2cs` on Linux. Correct once the robocopy and separator issues are fixed.

5. **Shelling out.** `golib` shells out **nowhere** (§A3). `src/core/testing/TestHost.cs` uses `Console` and
   `Path` only. The only process launches in converted code are `os/exec`'s, which are Windows-syscall-based
   and carried by F1.

---

### A8 — `docs/README.md` dual-platform plan

**Plan only — do not edit `docs/README.md` as part of reading this document.**

#### Inventory of command blocks

| README section (line) | Block | Fence | Classification |
|:--|:--|:--|:--|
| Installing the converter (163) | `cd src/go2cs` / `go install .` | `shell` | **Portable already.** Only the surrounding prose is Windows-flavored (`%GOBIN%`, `%GOPATH%\bin`). |
| Usage (172) | `go2cs [options] <input_dir> [output_dir]` | `shell` | Portable. |
| Examples (178) | 11 `go2cs …` lines | `shell` | Portable. |
| Common options table (194) | `-recurse` / `-recurse=module` cells | table | Prose only: `src\` and `pkg\` written with backslashes. |
| Converting a real-world module — step 0 (236) | `mkdir colordemo && cd colordemo` / `go mod init` | `bat` | **Windows-only fence**, portable content. |
| step 1 (265) | `set GOTOOLCHAIN=local` + `go get`/`go mod tidy`/`go build` with `& :: …` comments | `bat` | **Windows-only.** `set` and `& ::` are cmd syntax. |
| step 2 (274) | `cd path\to\colordemo` / `go2cs -recurse=nuget . csharp` | `bat` | **Windows-only** (path separator only). |
| step 3 (306) | `cd "csharp\src\example.com\colordemo\"` / `dotnet build …slnx` | `bat` | **Windows-only** (separator only). |
| step 4 (313) | `cd "bin\Debug\net9.0\"` / `colordemo.exe` | `bat` | **Windows-only** — `.exe` is real, not cosmetic. |
| Optional: module-only (330) | `cd path\to\myapp` / `go2cs -recurse=module . csharp` | `bat` | **Windows-only** (separator). |
| Optional: local stdlib (357) | `cd path\to\go2cs\src` / `deploy-core` | `bat` | **Windows-only** — invokes the `.bat` launcher. |
| Optional: local stdlib (367) | `go2cs -recurse . -go2cspath %GOPATH%\src\go2cs` | `bat` | **Windows-only** — `%VAR%` expansion. |
| Try it yourself (421) | `go2cs.exe -tests -test-action all "C:\Program Files\Go\src\unicode\utf8" src/core/unicode/utf8` | `sh` | **Mislabeled** — an `sh` fence containing a Windows path, a `.exe`, and a parenthetical telling non-Windows readers to substitute. |
| Requirements (149) | prose | — | No platform statement at all; needs one (§A4 N0). |
| Project layout (376) | table | — | ~~`src/Tests/…` rows must become `src/tests/…`~~ **done** — rename (b)'s sweep updated them (`d3223d252`). |

#### Recommended presentation convention

**One convention: env-neutral single blocks, with paired blocks only where the shell genuinely differs.**

Rules, in priority order:

1. **Prefer a single `shell`-fenced block written in a form both shells accept.** Forward slashes work in
   `cmd`, PowerShell, and `sh` for every command in this README (`cd csharp/src/example.com/colordemo`,
   `go2cs -recurse=nuget . csharp`). `go`, `go2cs`, `dotnet` and `git` all accept `/`. This collapses eight
   of the eleven `bat` blocks to one portable block each with no loss.
2. **Where the shell genuinely differs, pair the blocks** under bold `**Windows (PowerShell)**` /
   `**Linux / macOS**` labels. Only three cases need it:
   - `set GOTOOLCHAIN=local` vs `export GOTOOLCHAIN=local`
   - `%GOPATH%\src\go2cs` vs `$(go env GOPATH)/src/go2cs`
   - `colordemo.exe` vs `./colordemo`
3. **Use `$(go env GOROOT)/src/...` in the "Try it yourself" block for both platforms.** It is correct on
   Windows too (`go env GOROOT` prints `C:\Program Files\Go`, and `go2cs` accepts it), which removes the
   parenthetical and the mislabeled fence in one move. Drop the `.exe` from `go2cs.exe`.
4. **Never use `bat` fences again.** Use `shell` for portable blocks; `powershell` / `bash` only inside a
   paired pair.

Rationale for rejecting the alternatives: *pwsh-core-everywhere* would require a Linux reader to install
PowerShell just to read the README (the harness needs it, the tutorial should not); *paired blocks
everywhere* doubles eleven blocks to twenty-two for a difference that is usually one slash.

#### Per-section change list

| Section | Change |
|:--|:--|
| **Requirements** (149) | Add a platform row: converter and harness run on Windows and Linux; the **converted standard library currently ships as a `GOOS=windows` conversion** (§A4 N0) and a converted program's stdlib calls require Windows until the Linux corpus lands. One sentence, linked to this plan. |
| **Installing the converter** (156) | Replace `%GOBIN%` / `%GOPATH%\bin` with "`GOBIN` (or `GOPATH/bin`)". Keep the existing cross-compilation sentence — it is already correct and now load-bearing. |
| **Usage / Examples** (170–192) | No change. |
| **Common options** (192) | In the `-recurse` and `-recurse=module` cells, write `src/` and `pkg/`. In `-go2cspath`, note the default is `~/go2cs` on Linux and `%USERPROFILE%\go2cs` on Windows — the code already does both. |
| **Converting a real-world module** (220) | Delete the `> **NOTE:** these steps are tested on Windows only — they assume a cmd.exe-type shell.` banner and replace it with a one-line statement that the commands are shell-neutral except where paired. |
| **steps 0–3** (236, 265, 274, 306) | Convert `bat` → `shell`; forward slashes. Pair **only** the `set GOTOOLCHAIN` line. |
| **step 4** (313) | Pair: `colordemo.exe` / `./colordemo`. |
| **Optional: module-only** (326) | `bat` → `shell`; forward slashes. |
| **Optional: local stdlib** (350) | Pair the `deploy-core` invocation (`.\deploy-core.ps1` / `pwsh ./deploy-core.ps1`) and the `-go2cspath` line (`%GOPATH%\src\go2cs` / `$(go env GOPATH)/src/go2cs`). Note that `deploy-core` requires `pwsh`. |
| **Project layout** (374) | ~~`src/Tests/…` → `src/tests/…`~~ **done** by rename (b)'s sweep (`d3223d252`); no README action remains for this row. |
| **Status** (391) | The behavioral-project count is stale (`519`; the measured figure is 544 projects / 569 transpiled packages). Correct it while in the file. |
| **Try it yourself** (410) | Rewrite the block per rule 3: drop `.exe`, use `"$(go env GOROOT)/src/unicode/utf8"`, delete the Windows-path parenthetical, keep the `sh` fence (now honest). |
| **Performance** (453) | No change; `docs/Performance.md`'s environment line will read `unknown CPU` on Linux until §F13 is addressed. |

**Effort.** 1 session, and it should be the **last** thing in the sequence — the README should describe what
works, and steps 0–4 of the module tutorial do not work end-to-end on Linux until the converter emission
(F5) and the runtime story (F1/N0 disclosure) are settled.

---

## 4. Workspace topology — WSL vs a native clone

The test systems run WSL 2 with the same project folder visible from Linux. Measured on this machine:

```
$ wsl --list --verbose         →  Ubuntu-22.04 (Running), Ubuntu, docker-desktop*  [WSL 2]
$ wsl -e uname -a              →  Linux 6.18.33.2-microsoft-standard-WSL2 x86_64
$ mount | grep 9p              →  C:\ on /mnt/c type 9p (rw,noatime,aname=drvfs;path=C:\;…;msize=65536)
$ ls -la /mnt/c/…/src/*.ps1    →  -rwxrwxrwx  (no `metadata` mount option → everything executable)
$ ls /mnt/c/Projects/go2cs/src/tests  →  resolves, though `src/Tests` was what was on disk (probed pre-rename)
$ command -v pwsh dotnet go    →  all missing in Ubuntu-22.04 (only git is present)
```

Four consequences:

1. **`/mnt/c` is 9p, not virtiofs.** The workloads here are pathologically many-small-file: 569 transpiles,
   571 project builds, a NuGet restore over ~300 packages. 9p's per-file round-trip cost is the worst case
   for exactly that. Expect the CLAUDE.md budget table's numbers to inflate substantially over `/mnt/c` —
   enough that a healthy run looks hung against the recorded baselines.
2. **`/mnt/c` is case-INSENSITIVE from Linux.** At probe time (pre-rename) `src/tests` resolved although
   `src/Tests` was on disk. This is the worst property for this particular project: it **hides** exactly the
   casing bugs a real Linux user hits, so a green WSL run over `/mnt/c` proves nothing about the rename
   sweep's completeness or about the `.slnx` path casing (§A7.2).
3. **No `metadata` mount option** → every file is `0777`, so a missing `chmod +x` on a new `.sh` is invisible
   and `core.fileMode` noise is masked (git auto-detects and disables it, so this is benign but misleading).
4. **A shared working tree means shared `bin/`, `obj/` and `Generated/`.** A Windows `obj/project.assets.json`
   and a Linux one differ in every path; alternating builds force a full restore each time and produce
   confusing NETSDK errors.

**Recommendation: a native clone on ext4 inside the distro (e.g. `~/src/go2cs`) is the primary Linux lane.**
Use `/mnt/c` only for cross-inspecting a file, never for a build or a gate. The reasons are, in order:
case sensitivity (correctness), 9p throughput (usability), and `bin`/`obj` collisions (reproducibility).
A native clone also gets git's Linux defaults — `core.autocrlf=false`, `core.ignorecase=false`,
`core.filemode=true` — which is precisely the configuration §A6.1's `.gitattributes` work must be proven
against.

**If a shared tree is nonetheless required**, three mitigations, none of them free:
- `/etc/wsl.conf` `[automount] options="case=dir,metadata"` restores case sensitivity and permission
  metadata for `/mnt/c` (requires `wsl --shutdown`).
- Per-OS output redirection: a repo-root `Directory.Build.props` setting
  `BaseOutputPath`/`BaseIntermediateOutputPath` under an OS-discriminated subdirectory, so the two hosts do
  not share `obj/`.
- Accept the 9p cost and re-measure the whole budget table for the WSL lane — a stale baseline is what makes
  a healthy run look hung.

**Toolchain to install in the distro** (none present today): `dotnet-sdk-9.0`, Go 1.23.x (match
`go env GOVERSION` — the Windows host reports `go1.23.2`), `powershell` (`pwsh`), and — only for the Native
AOT perf variant — `clang` and `zlib1g-dev`.

---

## 5. Phased execution plan

Sized in the repo's own units. "Session" ≈ one focused working block; "arc" ≈ a commissioned multi-session
line of work with its own gates.

### Arc 1 — Determinism (0.5 arc; ~2 sessions) — **do first**

*Prerequisite for everything. Valuable on the Windows lane alone.*

1. **`.gitattributes` `eol=crlf` pinning** (§A6.1) + `git add --renormalize .` as its own commit.
   Gate: `git ls-files --eol src/core/fmt/print.cs` reports `attr/text eol=crlf`; full
   `check-no-regression.ps1` green on Windows afterward.
2. **`packageInfoWriter.go:52` EOL-agnostic split** + the `testConversion.go:1583` insert (§A1.3), with a
   converter unit test feeding an LF `package_info.cs` fixture.
   Gate: goldens byte-identical (this is a read-path change only); full CNR green.
3. **Update `.gitattributes` and `src/go2cs.slnx` path casing** for rename (b) if not already carried there
   (§A7.2). Gate: `check-solution-integrity.ps1` exit 0.

### Arc 2 — Converter on a Linux host (0.5 arc; ~2 sessions)

4. **Forward-slash project-reference emission** (§A1.1). This is a whole-corpus change — land it inside a
   rebank commit. Gate: a `-stdlib` reconvert overlays byte-identical except the reference lines; a
   `dotnet build src/go2cs-stdlib.slnx` stays green on Windows.
5. **`pathReplace` symlink resolution + loud no-match** (§A1.2). Gate: CNR clean (no emission change on a
   non-symlinked Windows box); a deliberate symlinked-GOROOT probe produces the warning.
6. **`golib.csproj` `USERPROFILE`→`HOME` fallback** (§A3).

*Exit criterion for Arc 2:* `GOOS=linux go build` of the converter, run on a native Linux clone, transpiles
the behavioral corpus and produces the same `.cs` bytes as the Windows host. That single comparison is the
whole arc's proof and is cheap once Arc 1 is in.

### Arc 3 — Harness portability (1 arc; ~2–3 sessions)

7. Shared `_paths.ps1`; parameterize the seven `.ps1` instruments (§A5).
8. Parameterize `BehavioralRunner`, `PerformanceRunner`, `BehavioralTestBase`, `UpdateTestTargets`.
9. Replace `Invoke-Robocopy` with a portable copy in `deploy-core.ps1`.
10. Add the enumeration count-guard assertions (§A5, "guard the silent ones").
11. Guard `set-version.ps1` with an `$IsWindows` check and a header note.

*Exit criterion:* on a native Linux clone, `pwsh ./check-no-regression.ps1` enumerates **569** packages,
transpiles them, and reports NO REGRESSION; `pwsh ./run-behavioral.ps1 --phase transpile,target` is green.
(Compile and Output phases are **not** expected green — they need the stdlib, which is Arc 4+.)

### Arc 4 — Measure the Linux corpus (1 session)

12. `go2cs -stdlib -comments -platforms linux/amd64 -go2cspath <scratch>/src` into a **seeded** scratch root
    (the seeding gate in CLAUDE.md's corpus-mechanics section applies verbatim, and the `[module:
    GoManualConversion]` census must be re-measured, not assumed — it is Windows-specific and most of its 40
    entries will not exist for a Linux target).
13. Build the generated solution; bucket by `error CS####`; report **packages-compiling**, not error count.
14. Write the number into `docs/Roadmap.md`. **Do not design the tree layout before this number exists.**
14a. Optionally repeat 12–14 with `-platforms darwin/arm64` in a second scratch root — same one-run cost —
    so the option-2 design in Arc 5 is shaped by **both** non-Windows numbers rather than retrofitted for
    the third flavor (big-three scope ruling, §1).

### Arc 5+ — The Linux stdlib lane (3–6 arcs, scoped by Arc 4's number)

15. The tree shape is **ruled** (§A2.4, user 2026-08-06): option **2** + N2 is the goal of this lane. An
    option-1-style scratch corpus serves as Arc 4's measurement scaffold only and never ships to `master`.
16. Drive the Linux corpus to compile — a Phase-3-shaped campaign whose hardest package is `syscall`
    (§A3), which has no named-export P/Invoke route the Windows corpus could lean on.
17. Platform-gate `LocalTimeZone` and `FindFirstFileData` in the runner enumeration (§A2.5); add the Linux
    counterparts they imply.
18. Stand up a Linux validation roster (`docs/ValidatedTestPackages.md` gains a platform dimension) and its
    own per-package disclosure manifests (§A6.2).
19. NuGet **N2** packaging (§A4) — `net9.0-windows` TFM + RID-split linux/darwin assets, with per-flavor
    proof pages; the `go.syscall` compile-surface seam designed here (§A4, third-flavor note).
20. The darwin lane follows once Linux is proven: `GOOS=darwin` as a third `$(GoTargetOS)` value through
    the SAME option-2 mechanism — no new machinery, per the big-three scope ruling. `osx-arm64` first
    (Apple silicon), `osx-x64` behind it.

### Interleaved, unblocked by the above

- **N0 NuGet disclosure** (§A4) — 1 session, any time after Arc 1.
- **`docs/README.md` dual-platform rewrite** (§A8) — 1 session, **after** Arc 2 and N0.
- **Native AOT on Linux** (§F13) — 0.5 session, after Arc 3; document `clang`/`zlib1g-dev` as prerequisites
  and fix `PROCESSOR_IDENTIFIER` → `RuntimeInformation.OSDescription`.

---

## 6. Appendix — probes run, with results

Every command below is read-only. Run from the repository root unless noted.

| # | Probe | Result |
|:--|:--|:--|
| P1 | `wsl --list --verbose` | `Ubuntu-22.04` (Running, WSL 2), `Ubuntu`, `docker-desktop`, `docker-desktop-data` |
| P2 | `wsl -e uname -a` | `Linux … 6.18.33.2-microsoft-standard-WSL2 … x86_64 GNU/Linux` |
| P3 | `wsl -d Ubuntu-22.04 -e bash -lc 'command -v pwsh dotnet go git make'` | only `/usr/bin/git`; **pwsh, dotnet, go, make all missing** |
| P4 | `wsl … 'mount \| grep -E "9p\|drvfs\|virtiofs"'` | `C:\ on /mnt/c type 9p (rw,noatime,aname=drvfs;path=C:\;uid=1000;gid=1000;…;msize=65536)` — **9p, no `metadata`** |
| P5 | `wsl … 'ls -la /mnt/c/Projects/go2cs/src/*.ps1'` | all `-rwxrwxrwx ritchie ritchie` |
| P6 | `wsl … 'ls /mnt/c/Projects/go2cs/src/tests'` | resolves (disk then had `src/Tests`; probed pre-rename) → **`/mnt/c` is case-insensitive from Linux** |
| P7 | `wsl … 'cat /etc/wsl.conf'` | `[boot]\nsystemd=true` — no `[automount] options` |
| P8 | `git config --get core.autocrlf / core.filemode / core.ignorecase` | `true` / `false` / `true` (the Windows-clone triple) |
| P9 | `git ls-files --eol src/core/fmt/print.cs src/core/fmt/fmt.csproj src/go2cs/csproj-template.xml src/core/compress/testdata/*` | `i/lf w/crlf attr/` for the first two; `i/lf w/crlf attr/text eol=crlf` for the template; `i/crlf w/crlf attr/-text` for the testdata files |
| P10 | `go env GOOS GOARCH GOROOT GOPATH GOVERSION` | `windows` `amd64` `C:\Program Files\Go` `C:\Users\rcarroll\go` `go1.23.2` |
| P11 | `dotnet --info` | SDK 9.0.316, MSBuild 17.14.43, RID `win-x64` |
| P12 | `find src/core -name '*_windows*.cs' \| wc -l` | **87** |
| P13 | `find src/core -name '*_linux*.cs' \| wc -l` | **0** |
| P14 | `find src/core -name '*_unix*.cs'` | `net/dnsclient_unix.cs`, `os/signal/signal_unix.cs` (2) |
| P15 | `find src/core \( -name '*_amd64*.cs' -o -name '*_arm64*.cs' \) \| wc -l` | **17** (incl. `internal/goarch/zgoarch_amd64.cs`, `syscall/types_windows_amd64.cs`) |
| P16 | `sed -n '1,10p' src/core/internal/goos/zgoos_windows.cs` | `//go:build windows`; `public static readonly @string GOOS = @"windows"u8;`; `IsLinux => 0` |
| P17 | `ls src/core/syscall` | 100 % Windows: `dll_windows.cs`, `zsyscall_windows.cs`, `types_windows_amd64.cs`, … plus the hand-owned `syscall_impl.cs`, `zsyscall_windows_impl.cs` |
| P18 | `grep -rln 'DllImport\|LibraryImport' --include=*.cs src/core` | **4 files**: `syscall/dll_windows.cs`, `syscall/exec_windows.cs`, `syscall/zsyscall_windows_impl.cs`, `time/time_impl.cs` |
| P19 | `grep -n 'DllImport' src/core/syscall/zsyscall_windows_impl.cs` | `kernel32.dll`: `GetTimeZoneInformation` (:91), `FindFirstFileW`/`FindNextFileW` (:184,187), `Process32FirstW`/`Process32NextW` (:282,285) |
| P20 | `grep -n 'DllImport' src/core/time/time_impl.cs` | `:970` `CreateWaitableTimerExW`, `:973` `SetWaitableTimer` — both behind `if (!OperatingSystem.IsWindows()) return null;` at `:931` |
| P21 | `grep -n 'procWriteFile\|WriteFile' src/core/syscall/zsyscall_windows.cs` | `:159` `modkernel32.NewProc("WriteFile")`; `:1365` `Syscall6(procWriteFile.Addr(), …)` |
| P22 | `grep -n 'initLocal\|GetTimeZoneInformation' src/core/time/zoneinfo_windows.cs` | `:251` `internal static void initLocal()`; `:254` `syscall.GetTimeZoneInformation(Ꮡi)` |
| P23 | `grep -rn 'DllImport\|kernel32\|RuntimeInformation\|OSPlatform\|Process.Start\|DirectorySeparator' src/core/golib` | **no platform-coupled hits** (only `Marshal.SizeOf`/`MemoryMarshal` intrinsics) |
| P24 | `grep -rn 'Console\.' src/core/golib/builtin.cs` | `:61` `Console.OutputEncoding = Console.InputEncoding = Encoding.UTF8`; `:1629,1638` `print`/`println` → `Console.Error` |
| P25 | `grep -n '\\r\\n' src/go2cs/*.go` (non-test) | 100+ write-only sites; **one read-back split: `packageInfoWriter.go:52`**; template split at `:57` |
| P26 | `grep -rn 'os.ReadFile\|os.Open(' src/go2cs/*.go` (non-test) | 27 sites; all EOL-agnostic except `packageInfoWriter.go:46` and the substring-insert at `testConversion.go:1568` |
| P27 | `grep -n 'log.Fatal' src/go2cs/packageInfoWriter.go` | `:120 :175 :381 :528 :586` — one per marker section |
| P28 | `grep -n 'pathReplace(' src/go2cs/importOperations.go` | `:44 :256 :258 :321`; definition `:458` with the `runtime.GOOS == "windows"` gate |
| P29 | `sed -n '263p;324p' src/go2cs/importOperations.go` | `filepath.Join(strings.ReplaceAll(targetDir, "/", "\\"), "\\"+packageName+".csproj")` |
| P30 | `grep -n 'GOOS\|GOARCH' src/go2cs/*.go` (non-test) | `main.go:98`; `conversionDriver.go:83`; `stdLibConverter.go:243,244,332,333`; `moduleConverter.go:157`; `testAliasShadowOperations.go:58,59`; `testConversion.go:372,373,1010,1011,4389`; `directiveOperations.go:357-408` (`knownGOOS`/`knownGOARCH`) |
| P31 | `cat src/core/fmt/fmt.csproj` | 10 `<ProjectReference Include="$(go2csPath)core\…\….csproj" />`; `USERPROFILE`→`HOME` fallback present |
| P32 | `cat src/core/golib/golib.csproj` | `$(USERPROFILE)\go2cs\` **without** the `HOME` fallback |
| P33 | `grep -n 'Import Project="\$(MSBuild' "$SDK/Microsoft.Common.CurrentVersion.targets"` (SDK 9.0.316) | `:18`, `:27`, `:28`, `:6961`, … — `$(Property)\literal\path` with matching `Exists('$(Property)\…')` conditions |
| P34 | `grep -o 'Path="[A-Za-z]*/' src/go2cs.slnx \| sort -u` | `Tests/`, `Utilities/`, `core/`, `gen/`, `graphs/`, `reports/` — 679 `Path=` entries total |
| P35 | `grep -rn 'Join-Path' --include=*.ps1 src \| grep '\\'` | 22 backslash literals across 9 scripts (listed in §A5) |
| P36 | `grep -n '\.exe\|Get-Process\|Program Files\|vswhere' --include=*.ps1 -r src` | `.exe` in 7 scripts; `Get-Process`/`Stop-Process` only in `run-behavioral-tests.ps1:51-56` (already path-scoped) |
| P37 | `grep -n 'exe\|\\\\\|Path.Combine' src/tests/behavioral/BehavioralRunner/Program.cs` | `:97,98` `@"..\..\..\.."`; `:101` `go2cs.exe`; `:269,270` `@"\bin\"`/`@"\obj\"`; `:484,557,558` `.exe` |
| P38 | `grep -n 'PublishProfile\|NetVersion\|Split' …/BehavioralTests/BehavioralTestBase.cs` | `:38` `"win-x64"`; `:78` `BinOutput.Split(@"\", …)[^1]`; `:55,57,63,73` backslash path fragments |
| P39 | `sed -n '404,420p' src/tests/performance/PerformanceRunner/Program.cs` | vswhere block guarded by `Directory.Exists(vsInstaller)` — **safe no-op on Linux** |
| P40 | `cat src/tests/performance/Directory.Build.targets` | `RuntimeIdentifier=$(NETCoreSdkRuntimeIdentifier)`, `TrimMode=partial` — RID resolves to `linux-x64` automatically |
| P41 | `sed -n '48,70p' src/deploy-core.ps1` | `Invoke-Robocopy` — `& robocopy @args`, bitmask exit handling |
| P42 | `grep -n '\\\\\|Tests\\\\' src/utilities/UpdateTestTargets/Program.cs` | `:8` `@"..\..\..\..\..\"`; `:11,41,91,117` `Tests\Behavioral\…` |
| P43 | `ls src/tour/scripts` | `start.ps1` **and** `start.sh` (19 lines) — the only existing bash twin |
| P44 | `find .github -type f` | `FUNDING.yml` only — **no CI workflows exist** |
| P45 | `grep -rn -i 'linux\|cross-platform\|macos\|posix\|wsl' docs/{README,Roadmap,Architecture,CleanupBacklog}.md` | **no matches** — no prior Linux position is recorded anywhere in the docs |
| P46 | `grep -n -A 20 'func isNonConvertedStdLibPackage' src/go2cs/stdLibConverter.go` | `:211-218` — skips `unsafe`, `builtin`, `testing`, `cmd`, `cmd/*`. **No platform dimension** — the skip list is GOOS-independent |
| P47 | `grep -n 'Console\.\|DirectorySeparator' src/core/testing/*.cs` | `TestHost.cs:253,262` normalize `/` → `Path.DirectorySeparatorChar` — already portable |

**Not run** (deliberately, per the read-only constraint on a machine with a sibling session holding
multi-minute gates): any `dotnet build`, `dotnet test`, `go build`, `go run`, `go test`,
`check-no-regression.ps1`, `run-behavioral.ps1`, `run-validated-sweep.ps1`, or any WSL package install. The
two determinations that would benefit from execution are flagged inline with their one-command recipes:
`dotnet build src/core/fmt/fmt.csproj` on Linux (§A1.1) and the `filepath.Join` behavior (§A1.1, a static
determination from Go's documented Unix semantics).
