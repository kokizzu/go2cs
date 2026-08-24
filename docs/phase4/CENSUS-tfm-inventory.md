# CENSUS — target-framework inventory for .NET migration Stage 2 (**DRAFT**, for coordinator review)

**Dated 2026-08-24.** Read-only census of `C:\Projects\go2cs` at `master` / `f534fcbfc`, working tree
clean. Nothing was modified. Every claim below is a file:line read from the tree, not an inference.

**Scope:** every place a target framework is spelled or implied, classified by what a Stage-2 bump
(`net9.0` → `net10.0`) owes it. The runbook's §5 was read first and is verified claim-by-claim in §1.

---

## 0. Headline

**§5.1 is correct about the csproj population, and incomplete about the harness.**

The one-line claim holds exactly: **all 1,127 tracked `.csproj` that spell `net9.0` carry the
*identical* conditioned form**, byte for byte —

```
<TargetFramework Condition="'$(TargetFramework)'==''">net9.0</TargetFramework>
```

— measured by set difference: the count of files matching that literal string equals the count of
files matching `net9.0` at all, and the complement is **empty**. There is no unconditional `net9.0`
in any `.csproj` anywhere in the tree. So `src/Directory.Build.props:27` really is the one line, and
1,127 files really are inert.

**What §5.1 does not enumerate is that the OUTPUT PATH moves with the property.** Both converter
templates and `golib` pin `<OutDir>bin/$(Configuration)/$(TargetFramework)/</OutDir>`
(`src/go2cs/csproj-template.xml:34`, `src/go2cs/test-csproj-template.xml:52`,
`src/core/golib/golib.csproj:41`), so on the day the property moves, **every build output relocates
from `bin/Debug/net9.0/` to `bin/Debug/net10.0/`** — and **eight sites across four harnesses and CI
hardcode the old path**. Each one fails as a *false red* of precisely the shape this repository
catalogs: the build succeeds, the probe looks in the wrong directory, and the instrument reports the
corpus as broken.

Ranked by blast radius:

| Site | Failure the day the TFM moves |
|:--|:--|
| `src/tests/Behavioral/BehavioralRunner/Program.cs:96` | `const string NetVersion = "net9.0"` → Compile phase finds **0 of 637** assemblies; Output phase finds 0 exes. Reads as total corpus regression. |
| `.github/workflows/os-matrix.yml:318` | census reports **"Assemblies produced: 0"** on a fully successful build |
| `src/tests/Performance/PerformanceRunner/Program.cs:73` | JIT variant exe not found; perf suite cannot run — and perf is §6's whole point |
| `src/tests/Behavioral/run-behavioral.ps1:71` | `& $runnerExe` → CommandNotFoundException after a *successful* `dotnet build` |
| `src/tests/Performance/run-performance.ps1:43` | same shape |
| `src/tests/Performance/run-performance-floor.ps1:263,297,304` | JIT-leg exe, output dir, and GlobalUsings probe all miss |

Second finding, also unstated in §5.1: **`push-nuget.ps1`'s asset merge is already TFM-agnostic.**
§5.1 lists "push-nuget.ps1's package layout" among the families that are "NOT inert". Verified
against the code: the merge globs `lib/*` and prefixes `runtimes/$rid/` (`src/push-nuget.ps1:913,
918, 926, 931`), and dependency union matches on the nuspec's own `targetFramework` attribute
(`:628–638`). **Every `net9.0` in that file is a comment** (`:15, :16, :519, :520, :545`). The one
literal TFM in its executable path is `netstandard2.0` at `:752`, which is a *must-not-change*. So
push-nuget needs **zero** functional edits — only comment refresh.

Third: **the emitted publish profiles never self-heal.** `writePublishProfiles`
(`src/go2cs/projectFileWriter.go:569–600`) explicitly **skips a profile that already exists**
(`:594–597`, *"user may change default parameters, so we don't overwrite"*). Editing the nine
templates therefore fixes new emissions only; every already-emitted `Properties/PublishProfiles/`
tree on every machine and every deploy root keeps `net9.0` until deleted by hand. They are
`.gitignore`d (`.gitignore:180 *.pubxml`), so they are invisible to `git status`.

---

## 1. §5 verified against the tree

| §5 claim | Verdict | Evidence |
|:--|:--|:--|
| `src/Directory.Build.props` owns the TFM and says so in its own comment | **TRUE** | `src/Directory.Build.props:4` *"The repository-wide TARGET FRAMEWORK, and the one line a .NET hop edits"*; the property at `:27` |
| Emitted projects keep a CONDITIONED fallback | **TRUE, universally** | 1,127 / 1,127 `.csproj` carry the identical conditioned literal; complement of the two sets is empty |
| A guard enforces the shape, under plain `go test ./...` | **TRUE** | `src/go2cs/csprojMetadata_test.go:211–232`, `TestTemplatesLeaveTheTargetFrameworkOverridable`, loops **both** templates, rejects unconditional *and* absent |
| Nested props files import the root explicitly | **TRUE, and complete** | Only two nested props exist. `src/core/Directory.Build.props:6` and `src/tests/Performance/Directory.Build.props:6` both `GetPathOfFileAbove`. `src/tests/Performance/Directory.Build.targets` sets no TFM. |
| "thousands of" inert tracked mentions | **1,127 `.csproj` + 1 props**; total `net9.0`-bearing tracked files = **1,172** | `git grep -l net9.0` |
| A migration does not owe a corpus regen for the TFM | **TRUE** — no gate reads the emitted line while `src/Directory.Build.props` is above it | see §3 for the one exception: `deploy-core` |
| NOT-inert family: push-nuget's package layout | **FALSE — it is inert** | merge derives the TFM; all `net9.0` occurrences are comments. See §0. |
| NOT-inert family: the publish profiles under `src/go2cs/profiles/` | **TRUE** | 9 files, unconditional `<TargetFramework>net9.0</TargetFramework>` at `:12` **and** a TFM-bearing `<PublishDir>` at `:9` |
| NOT-inert family: the two templates' conditioned fallbacks | **TRUE only off-repo** | inert inside the repo (root props wins); load-bearing for `deploy-core` / `-recurse` / single-package output roots, where no props defines the property |
| §5.2: embedded assets are covered by `ConverterBuildInputs` since 2026-08-22 | **TRUE, and it covers `profiles/*`** | `src/tests/ConverterBuildInputs.cs:19–21, 45, 223–225, 250–294` — the embedded set is *derived* from the `//go:embed` directives, and `src/go2cs/embeddedTemplates.go:37` embeds `profiles/*`. A pubxml edit therefore rebuilds `go2cs.exe` on the next runner invocation with nothing to remember. |
| runbook says no `global.json` should exist before Stage 2 | **TRUE — none exists**, tracked or untracked. Also **no** `Directory.Packages.props`, **no** `NuGet.config`, **no** `.config/dotnet-tools.json`. | `git ls-files` + filesystem `find` |

**Not stated in §5 and owed:** the eight hardcoded output-path sites (§0), and the CI SDK-channel
default (§5).

---

## 2. Class A — generated corpus (change by regen; **inert**, no Stage-2 edit owed)

All carry the conditioned fallback. None is read by any gate while `src/Directory.Build.props` is in
scope. They level on the next `-stdlib` / behavioral regen, which a .NET migration does **not** owe.

| Family | Count | Emission site |
|:--|--:|:--|
| `src/core/<pkg>/<pkg>.csproj` — converted stdlib production | 306 | `csproj-template.xml` via `projectFileWriter.go:88` |
| `src/core/<pkg>/<pkg>.tests.csproj` — Phase-4 test hosts | 162 | `test-csproj-template.xml` via `testConversion.go:3231` |
| `src/tests/Behavioral/**` — behavioral projects + nested sub-libraries | 637 | `csproj-template.xml` |
| `src/tests/Performance/Perf*/Perf*.csproj` | 14 | `csproj-template.xml` |
| **Total Class A** | **1,119** | |

**NINE** of the 306 core production csproj are **hand-owned by consequence** and will *never* level on
a regen — the converter does not re-emit them: `src/core/golib/golib.csproj` (hand-written runtime),
`src/core/unsafe/unsafe.csproj` and `src/core/testing/testing.csproj` (skip-listed,
`isNonConvertedStdLibPackage`), `src/core/internal/{concurrent,godebug,weak}/*.csproj` (whole-file
hand-owns ⇒ `unmarkedFileCount == 0` ⇒ the driver `continue`s before `writeProjectFile`), and the
**platform-exclusive trio** — `src/core/internal/runtime/syscall`, `src/core/crypto/x509/internal/macos`,
`src/core/vendor/golang.org/x/net/route` — which are absent from the reference (Windows) target, so the
`-platforms` merge never writes their project files either. This paragraph said "six" until the Stage-2
hop measured it: the trio was missed here, though `067c95b6f` had already audited it as unreachable
(that commit's "seven" is this nine minus `golib` and `testing`, which are hand-written rather than
template-derived and so had no template comments to level).

**Corrected 2026-08-24, and no longer merely a recommendation:** all nine are now folded into Class B
and carried by `src/migrate-tfm.ps1`'s apply set, together with the eight repo-only harness/tooling
projects below — seventeen project files in total, each with its reason. The test of Class A is
**reachability, not location**: twelve of the seventeen sit *inside* the Class-A directories, and
counting them there is what let them read as "a regen levels them" while no regen could. The script
now subtracts them so the Class-A count reads true.

Their `net9.0` line is inert **in-repo** — `src/Directory.Build.props` is imported above the project
body and wins — but that inertness is exactly why this sat unnoticed. It does **not** hold in a
DEPLOYED tree: `deploy-core.ps1` copies `src/core` while excluding core's `Directory.Build.props`, and
the root props it writes pins only `$(go2csPath)`, never the framework. There each project's own line
becomes authoritative, and a holdout at `net9.0` referencing a moved package is a real **NU1201**
(`net10.0` → `net9.0` is legal; `net9.0` → `net10.0` is not). Measured on a scratch deploy under SDK
10.0.400: `testing` reverted to `net9.0` produces `NU1201` against `go.time` and `go.lib`;
`internal/godebug` produces six. Post-fix the full 307-project deployed solution restores clean and
`core/testing` builds with 0 errors. **The shape is: every in-repo gate green, the deployed and
published artifact broken.**

### How a version change flows (emission sites)

```
src/go2cs/csproj-template.xml:5        ─┐
src/go2cs/test-csproj-template.xml:20  ─┤ //go:embed  (embeddedTemplates.go:22,25)
src/go2cs/profiles/*.pubxml:9,12       ─┘  →  compiled INTO go2cs.exe
                                              ↓
                              projectFileWriter.go:88   (fmt.Sprintf over the template)
                              testConversion.go:3231    (strings.Replacer over the test template)
                              projectFileWriter.go:479 → writePublishProfiles(:569)
                                              ↓
                                    emitted <pkg>.csproj / <pkg>.tests.csproj
                                    emitted Properties/PublishProfiles/*.pubxml   ← NEVER OVERWRITTEN
```

There is **no TFM literal in any converter `.go` file.** `grep -rn 'net9\.0\|net10\.0\|netstandard'
--include=*.go src/go2cs/` returns three hits, all prose comments
(`internal/gensymbols/main.go:223`, `moduleConverter.go:633,634`). The converter's only lever is
`-csproj <tmpl>` (`main.go:361`), which replaces the production template wholesale at run time.

---

## 3. Class B — hand-maintained project files (change by edit; **inert in-repo, authoritative deployed**)

Every one already carries the conditioned fallback, so none requires a Stage-2 edit for an **in-repo**
build to work — `src/Directory.Build.props` wins wherever it is in scope. Listed because they are the
files a coordinator would expect to touch, because a future maintainer adding one must copy the
conditioned shape, and — the point this section originally understated — because in a **deployed**
tree that props file is deliberately absent and these lines become the operative ones. See section 2's
correction for the measured NU1201 evidence.

The table below is the ORIGINAL nine. It is incomplete: the five core packages folded in from section
2, plus the platform-exclusive trio, bring the class to **seventeen** project files. All seventeen are
carried by `src/migrate-tfm.ps1`, which is the maintained list; this table is the historical snapshot.

| File | TFM line | Note |
|:--|:--|:--|
| `src/Directory.Build.props` | `:27` | **THE ONE EDIT.** Everything else in this table is downstream. |
| `src/core/golib/golib.csproj` | `:8` (+ `OutDir` `:41`, prose `:35`) | hand-written runtime; never re-emitted |
| `src/tests/Behavioral/BehavioralRunner/BehavioralRunner.csproj` | `:14` | |
| `src/tests/Behavioral/BehavioralTests/BehavioralTests.csproj` | `:4` | |
| `src/tests/Performance/PerformanceRunner/PerformanceRunner.csproj` | `:13` | |
| `src/tests/GolibTests/GolibTests.csproj` | `:4` | Stage-1/2 gate instrument |
| `src/tests/GenTests/GenTests.csproj` | `:4` | exercises go2cs-gen internals |
| `src/tests/ChannelTests/ChannelTests.csproj` | `:4` | |
| `src/tests/GenericTests/GenericTests.csproj` | `:5` | |
| `src/utilities/UpdateTestTargets/UpdateTestTargets.csproj` | `:5` | |
| **Total Class B** | **9 `.csproj` + 1 `.props`** (snapshot; the true class is **17 `.csproj` + 1 `.props`**) | |

Class A + Class B = 1,128 files spelling `net9.0` in a build file (1,127 csproj + the props). The
split between them moves by 8 once section 2's fold is applied — the total does not.

One further site the snapshot missed, now carried: `src/core/golib/golib.csproj:35` states in prose
that warnings 1701/1702 "cannot fire on net9.0". That is a present-tense fact about the framework the
project TARGETS, not dated measurement provenance, so it moves with the TFM rather than staying as
Class C.

---

## 4. Class C — MUST NOT CHANGE

| File / line | Value | Reason |
|:--|:--|:--|
| `src/gen/go2cs-gen/go2cs-gen.csproj:7` | `<TargetFramework>netstandard2.0</TargetFramework>` — **unconditional, deliberately** | Roslyn analyzer/generator requirement. `docs/phase4/RECON-dotnet10.md:92–100` rules it **"COMPATIBLE, no change owed"**: netstandard2.0 remains the *required and supported* analyzer target under the .NET 10 SDK, Roslyn's analyzer ABI is backward-compatible, and MSB4062 does not apply to analyzers (`:107`). **The unconditional spelling is what protects it** — `Directory.Build.props` is imported above the project body and would otherwise set it to net10.0. Do not "hoist" this line. |
| `src/push-nuget.ps1:752` | `.../gen/go2cs-gen/bin/$Configuration/netstandard2.0` | pairs with the above; correct as-is |
| `src/tests/Performance/Directory.Build.targets:3–6` | comment: a global `PublishAot` "breaks the netstandard2.0 go2cs-gen analyzer with NETSDK1207" | the `PerfAot` gate exists because of the analyzer's fixed TFM; unchanged by the hop |
| `src/core/runtime/managed_impl.cs:828` | `"288 B on net9.0/9.0.19 x64, measured"` | **measurement provenance** in a hand-owned file. Rewriting it falsifies the record. |
| `src/core/testing/testing.cs:540` | `"measured, not assumed (r56d, net9.0/9.0.18, x64)"` | same |
| `docs/ConversionStrategies-Reference.md:15594` | `"(net9.0/9.0.18, x64)"` survey conditions | same |
| 15 `docs/**` files — RECON, DESIGN-\*, FINDING-\*, PLAN-\*, MILESTONE-\*, BOARD-\*, StringsBytes-BlockerMap | 71 mentions total | historical records and dated measurements. `MILESTONE-75pct-prep.md:605–606` records exact packed byte sizes at `lib/net9.0/`; `PLAN-hop-campaign.md:241–284` records the scouting legs. **Do not sed the docs tree.** |
| `src/archived/**` — 92 `.csproj` at `net5.0` | unconditional `net5.0` | archived, unbuilt, not in any solution. Leave. |

---

## 5. Class D — scripts and CI that need a **coordinated** change (the real Stage-2 work)

These are the eight output-path sites plus the CI SDK default. All are consequences of
`<OutDir>bin/$(Configuration)/$(TargetFramework)/</OutDir>`.

| File:line | Current | Consequence if missed |
|:--|:--|:--|
| `src/tests/Behavioral/BehavioralRunner/Program.cs:96` | `private const string NetVersion = "net9.0";` — consumed at `:715` (assembly probe) and `:863` (exe probe) | **637 × Status.Fail**, indistinguishable from a corpus regression |
| `src/tests/Performance/PerformanceRunner/Program.cs:73` | `private const string NetVersion = "net9.0";` — consumed at `:996` (`Variant.Jit`) | perf suite cannot locate the JIT leg; §6's whole protocol blocked |
| `src/tests/Behavioral/run-behavioral.ps1:71` | `BehavioralRunner/bin/Debug/net9.0/BehavioralRunner$ExeSuffix` | build passes, then `& $runnerExe` throws CommandNotFound |
| `src/tests/Performance/run-performance.ps1:43` | `PerformanceRunner/bin/Debug/net9.0/PerformanceRunner$ExeSuffix` | same |
| `src/tests/Performance/run-performance-floor.ps1:263` | `$Bench\bin\Release\net9.0\$Bench.exe` | JIT leg of the floor comparison |
| `src/tests/Performance/run-performance-floor.ps1:297` | `$jitDir = ...\bin\Release\net9.0` | |
| `src/tests/Performance/run-performance-floor.ps1:304` | `...\obj\Release\net9.0\$bench.GlobalUsings.g.cs` | |
| `.github/workflows/os-matrix.yml:318` | `bin/Debug/net9.0/$assembly.dll` | compile census reports **0 assemblies produced** on a green build |
| `.github/workflows/os-matrix.yml:41–47` | `dotnet` input default `9.0.x`, options `{9.0.x, 10.0.x}` | **SDK 9 cannot build `net10.0`.** Stage 2 must flip the default (or the census self-hosts a build it cannot perform). Description text at `:41` and step name at `:190` also become false. |

**Durable-path recommendation (per the repo's nothing-throwaway principle).** The pattern that
already solves this lives in the tree: `BehavioralTestBase.cs:133` **derives** the TFM from its own
`AppContext`-relative bin tail —

```csharp
NetVersion = BinOutput.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries)[^1];
```

— and is therefore the one C# harness the hop does not touch. The two runner `const`s should adopt
the same derivation, and the PowerShell sites should read a single `$NetVersion` hoisted into
`src/_paths.ps1` beside `$ExeSuffix` / `$IsWindowsHost` (which today spells no TFM at all). That
converts nine hand-edits into one, and makes hop N+1 free. **Recommend doing this AS Stage 2**, not
after — a Stage 2 that only sed-replaces the strings leaves the same nine landmines for the next hop.

### Not owed, and say so rather than skipping silently

- **`src/deploy-core.ps1`** — no TFM anywhere. Verified: the root props it writes
  (`:187–200`) defines **only** `$(go2csPath)`, and it deliberately **excludes** `src/core/Directory.Build.props`
  from the staged copy (`:175`, with the reason at `:169–171`). Consequence worth stating in the stage's
  record: **the hop does not reach a deployed GOPATH tree.** There, the emitted csproj's own conditioned
  fallback is the only definition, so a deployed corpus builds at whatever the committed csproj say —
  `net9.0` until a corpus regen. `deploy-core`'s own verify build would therefore compile net9.0 under
  the net10 SDK, which is trap 4 (old TFM, old runtime) rather than a Stage-2 measurement.
- **`src/push-nuget.ps1`** — no functional edit. Comment refresh only (`:15, :16, :519, :520, :545`).
- **`check-no-regression.ps1`, `run-validated-sweep.ps1`, `check-solution-integrity.ps1`,
  `check-symbol-sync.ps1`, `clean-bin.ps1`, `set-version.ps1`, `src/_paths.ps1`,
  `src/tests/Behavioral/_paths.ps1`** — swept, zero TFM references of any kind.
- **`src/utilities/UpdateTestTargets/Program.cs:8`** — `net9.0` appears in a **comment** only; the
  path is built from `Path.Combine` segments (`:12`). No edit owed.
- **The converter's Go sources** — `dotnet build` / `dotnet run --project` throughout
  (`testConversion.go:4859, 4862, 5382`); no bin-path assumption anywhere. No edit owed.
- **`.slnx` / `.sln`** — zero TFM references.

---

## 6. Class E — documentation stating the TFM as present-tense fact

Per the standing *docs-are-present-tense-for-visitors* doctrine, these state what IS and go stale the
moment the property moves. Distinguish sharply from Class C's historical records.

| File:line | Text | Kind |
|:--|:--|:--|
| `docs/README.md:343` | `cd bin/Debug/net9.0` | **copy-pasteable command in the visitor-facing Try-it-yourself flow** — highest priority |
| `CLAUDE.md:195` | "standard `dotnet build` (target **net9.0**, C# latest)" | orientation fact |
| `CLAUDE.md:654` | "its `bin/Debug/net9.0`" (UpdateTestTargets run instructions) | operational instruction |
| `docs/Glossary.md:331` | TFM entry's example: `<TargetFramework>net9.0</TargetFramework>` | definition example |
| `docs/Glossary.md:58` | "*assembly*" defined as `bin/Debug/net9.0/<AssemblyName>.dll` exists | definition |
| `docs/CIMatrix.md:67` | census probe documented as `bin/Debug/net9.0/<AssemblyName>.dll` | must move **with** `os-matrix.yml:318` |
| `docs/ConversionStrategies-Reference.md:698` | the emitted `<TargetFramework Condition=…>net9.0</TargetFramework>` form | **must match the emitted golden** — this is the authoritative-record rule |
| `docs/DotNetMigration.md` (4 mentions) | the runbook itself | update alongside; also fold in §0's three corrections |

**Total Class E: 8 files.** `docs/phase4/DESIGN-warning-suppression.md:62, 96, 245` is a judgment
call: it argues golib's `NoWarn` should drop `1701;1702` because "net9.0 cannot emit them". The
reasoning survives the hop unchanged (net10.0 cannot either), so no edit is owed — but if the NoWarn
cleanup lands in the same window, restate it TFM-neutrally.

---

## 7. The count

| Class | What | Files | Stage-2 action |
|:--|:--|--:|:--|
| **A** | generated corpus `.csproj` | **1,119** | none — inert; levels on next regen |
| **A′** | hand-owned-by-consequence core `.csproj` (never re-emitted) | 6 | none required; permanent staleness unless hand-edited |
| **B** | hand-maintained project files (incl. the one props) | **10** | none required — all conditioned. **1 edit** (`src/Directory.Build.props:27`) does the work. |
| **C** | must-not-change | **1 csproj + 1 script line + 2 corpus `.cs` + 15 docs + 92 archived csproj** | leave |
| **D** | scripts / CI needing a coordinated change | **6 files, 9 sites** | edit (or, preferably, hoist) |
| **D′** | publish profiles (embedded assets) | **9 `.pubxml`, 2 lines each** | edit `<TargetFramework>` **and** `<PublishDir>` |
| **D″** | converter templates (embedded assets, off-repo load-bearing) | **2 `.xml`, 1 line each** | edit the conditioned fallback |
| **E** | docs stating it as present-tense fact | **8** | edit |

**Functional Stage-2 footprint: 1 + 9 + 18 + 2 = 30 line edits across 18 files.** Everything else in
the 1,172-file `net9.0` population is inert, historical, or archived.

---

## 8. Stage 2 dispatch checklist (ordered)

Ordered so that no gate runs against a half-moved tree, and so the false-red generators are closed
**before** the instruments that would emit the false reds.

1. **Preflight — confirm the environment can build the target.** SDK 10 present; no `global.json`
   appears (there is none today and none should be added); VS 18.x if the IDE will be used
   (`RECON-dotnet10.md:22` — VS 17.14 loads the 10.0.100 SDK but **cannot target net10.0**).
   Disk preflight per §6's cost discipline.
2. **Close the false-red generators FIRST — Class D, all nine sites, in one commit.** Prefer the
   derivation/hoist over the string swap (§5). Nothing here changes behavior at net9.0, so this
   commit is independently verifiable **on the old TFM**: run `run-behavioral.ps1` and confirm green
   *before* the property moves. That is what makes step 4's verdict trustworthy.
3. **Edit the embedded assets — Class D′ + D″, one commit.** Nine `.pubxml` (`<TargetFramework>` at
   `:12` **and** `<PublishDir>` at `:9` — both, or the publish lands in a net9.0-named folder) plus
   the two template fallbacks. **Then run the converter's `go test -count=1 ./...` from `src/go2cs`**:
   `TestTemplatesLeaveTheTargetFrameworkOverridable` must still pass (it asserts the *condition*, not
   the value, so it should), and the `embeddedAssets_test.go` guards must still pass. `-count=1` is
   required per §5.2's residual if any harness C# moved in step 2.
4. **THE EDIT: `src/Directory.Build.props:27`, `net9.0` → `net10.0`.** One line. Update its own
   comment block if it names a version (it does not today — it is version-neutral prose).
5. **Gate, in §4's ladder order:** `go2cs-stdlib.slnx` at default `$(GoTargetOS)`, then each other
   supported flavor (**purge `bin`/`obj`/`Generated` between switches**), then `go2cs.slnx`
   (the only gate covering non-generated members), then `run-behavioral.ps1` all four phases with
   **zero `NOT MEASURED`**, then `GolibTests`. Budget from the i7-5820K column, not the i9 one.
6. **`-tests` pipeline on a representative row set** (§5.3) — the generated test host carries its own
   fallback (`test-csproj-template.xml:20`) and its own `bin/tests/` publish shape.
7. **CNR + converter `go test ./...` are OWED at this stage, unlike Stage 1** — steps 2–3 touched
   converter build inputs (embedded assets) and harness C#. State the accounting explicitly; §5.2
   proves CNR's verdict is trustworthy over a template edit (content-addressed `go build` cache,
   verified by SHA-256).
8. **Purge stale emitted publish profiles.** `writePublishProfiles` will not overwrite them
   (`projectFileWriter.go:594–597`) and they are `.gitignore`d, so nothing surfaces them: delete
   `**/Properties/PublishProfiles/` across the worktree and any deploy root before any publish.
9. **CI, as its own commit:** `os-matrix.yml:318` (probe path — must move together with
   `docs/CIMatrix.md:67`), `:44` default channel `9.0.x` → `10.0.x`, `:41` description, `:190` step
   name. Dispatch one `census` run per flavor to confirm the "assemblies produced" figure is
   nonzero before trusting any later dispatch.
10. **Docs — Class E, 8 files, one commit.** Do **not** run a tree-wide `sed`: 15 `docs/**` files and
    2 corpus `.cs` hold dated measurement provenance that must keep saying `net9.0` (Class C).
11. **Commit-message obligation (§5.1):** state the expected staleness — "1,119 generated `.csproj`
    and 6 hand-owned ones still read `net9.0`; inert, level on the next regen" — so a later reader
    does not diagnose it as drift.
12. **Deferred to their own stages, not Stage 2:** AOT/ILC verification (§7 — *now* reachable, trap 1),
    the perf re-measurement (§6, prediction N5 already stated), the trim audit (§8), and the
    publication rehearsal (§11). `deploy-core` / `push-nuget` are **not** Stage-2 gates.

---

## 9. Surprises

1. **`push-nuget.ps1` is already TFM-agnostic** — §5.1 lists it as a not-inert family; the code says
   otherwise. Zero functional edits. This is a correction to the runbook, and it *reduces* the
   stage's risk surface rather than adding to it.
2. **The runbook enumerates the csproj population but not the output path.** The bump is one line;
   the *directory every artifact lands in* is the second-order effect, and it is where all nine real
   edits are. Nothing in §5 mentions `bin/Debug/net9.0`.
3. **`BehavioralTestBase` already solved this and the two standalone runners did not.** The MSTest
   path derives its TFM (`:133`); `BehavioralRunner` and `PerformanceRunner` hardcode it. The fix is
   copyable from inside the repo.
4. **The hop does not reach a deployed GOPATH tree**, by design — `deploy-core.ps1` excludes core's
   props and writes one that pins only `$(go2csPath)`. Deployed corpora stay net9.0 until a regen.
   This is correct behavior, but it means "the TFM moved" is a statement about the repo, not about
   `%GOPATH%\src\go2cs`.
5. **Emitted publish profiles are write-once and gitignored** — a deliberate converter behavior
   (`:594–597`) that turns a Stage-2 template edit into a no-op on every existing tree, invisibly.
6. **`go2cs-gen` is protected by being *un*conditional.** The netstandard2.0 line wins precisely
   because `Directory.Build.props` is imported above the project body and the project's own
   unconditional assignment overrides it. Anyone "tidying" that line into the conditioned house
   style would silently retarget the analyzer to net10.0 and break it (`RECON-dotnet10.md:92–100`).
   Worth a comment on the line; there is none today.
7. **`src/tests/Behavioral/TypeSwitch/build.cmd:2`** is dead: it references `net8.0` and a
   `gocore/` path retired at `ba6fef6c9` (2025-03-08). Pre-existing rot, unrelated to Stage 2,
   but it is the only other `net<N>.0` in the live tree. Candidate for a cleanup chip.
8. **No `global.json`, no `Directory.Packages.props`, no `NuGet.config`, no `dotnet-tools.json`** —
   confirmed absent both tracked and on disk. The runbook's precondition holds; note that this also
   means **nothing pins the SDK**, so a machine with SDK 10 installed is already resolving it for
   every build (which is what makes Stage 1 the real variable it claims to be).

---

*DRAFT — census only. No file in `C:\Projects\go2cs` was modified, committed, or pushed.*
