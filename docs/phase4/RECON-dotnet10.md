# .NET 10 migration recon — DRAFT for coordinator review

> Read-only reconnaissance for the .NET 9 → 10 hop, slotted against the phases of
> `docs/DotNetMigration.md` (read first, per the runbook's own instruction). Web research
> performed 2026-08-23; repository facts measured against `C:\Projects\go2cs` (master).
> Nothing in the repo was modified. **DRAFT — coordinator review owed before any figure
> here is treated as a ruling input.**

---

## 1. Status / version (feeds Stage 0, runbook §2)

**.NET 10 is GA and has been for nine servicing months.** This gates nothing — the hop's
schedule question is not "is 10 ready" but "how long can 9 remain the target".

| Fact | Value | Source |
|:--|:--|:--|
| GA date | **2025-11-11** | [dotnet/core release notes](https://github.com/dotnet/core/blob/main/release-notes/10.0/README.md) |
| Support class | **LTS — supported through 2028-11-14** (3 years) | same |
| Current runtime (Aug 2026 servicing, 2026-08-11) | **10.0.11** | [.NET 10.0 Update — August 11, 2026](https://support.microsoft.com/en-us/servicing/dotnet/net-10/2026/net-10-0-update-august-11-2026), [servicing blog](https://devblogs.microsoft.com/dotnet/dotnet-and-dotnet-framework-august-2026-servicing-updates/) |
| Current SDK feature bands | **10.0.111 / 10.0.303 / 10.0.400** | [10.0.11 release notes](https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.11/10.0.11.md) |
| IDE pairing | VS 2026 (**18.9** current). ⚠ **Targeting net10.0 requires VS 18.x**; VS 17.14 can *load* the 10.0.100 SDK but cannot target net10.0 | [10.0.11 release notes](https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.11/10.0.11.md), [dotnet/docs #48320](https://github.com/dotnet/docs/issues/48320) |
| Language | C# 14 | [What's new in C# 14](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14) |
| Latest patch content | 10.0.11 fixed 9–10 CVEs + non-security fixes | [servicing blog](https://devblogs.microsoft.com/dotnet/dotnet-and-dotnet-framework-august-2026-servicing-updates/) |

**The schedule-forcing fact: .NET 9 reaches END OF SUPPORT on 2026-11-10** — STS support
was extended from 18 to 24 months, which lands .NET 8 and .NET 9 on the *same* EOL day
([.NET blog](https://devblogs.microsoft.com/dotnet/dotnet-8-9-end-of-support/),
[STS 24-month announcement](https://devblogs.microsoft.com/dotnet/dotnet-sts-releases-supported-for-24-months/)).
After that date the runtime the published packages target receives no security servicing.
**The hop is therefore mandatory-by-November, not discretionary** — roughly 11 weeks from
this recon. The runbook's §11 publication ruling should be taken with that date in view: a
"do not publish across the migration" choice has a hard far edge.

**Stage-0 provisioning recommendation:** install the **10.0.4xx** band (10.0.400) side-by-side
per runbook §2 — it is the band VS 18.9 pairs with and the one that will be receiving feature-band
servicing longest. Record both inventories per machine as §2.2 requires. `global.json` pin waits
for the TFM stage (§2.4) — and see §4 below for a key the pin must NOT contain.

---

## 2. Cross-cutting repo finding — C# 14 arrives at Stage 1, not Stage 2

**Every converted csproj pins `<LangVersion>latest</LangVersion>`** (verified: emitted
projects at line 28, test projects at line 37, corpus-wide). `latest` binds to the compiler,
not the TFM — so the moment the .NET 10 SDK is on PATH (**Stage 1, SDK-only, runbook §4**),
the entire corpus compiles as **C# 14**, while still targeting net9.0.

Consequence for the §1 "one variable at a time" invariant: the SDK stage moves **two**
things — the build toolchain *and* the language level. That is unavoidable given the pin and
does not break the ladder (the Stage-1 gate is the full behavioral suite, which is exactly
the instrument that detects a language-level behavior shift), but it should be **stated in
the Stage-1 record** so a C# 14-attributable divergence found there is not mis-filed as an
SDK defect. An optional control exists if attribution is ever needed: temporarily pinning
`LangVersion=13` isolates compiler-vs-language — worth knowing, not worth doing
pre-emptively.

The trap-3 warning delta (new Roslyn diagnostics on unchanged code) will also carry any
new C# 14-era analyzers' output; classify, don't count, per the runbook.

---

## 3. Breaking-change table (change → go2cs surface → risk → action)

Full index: [Breaking changes in .NET 10](https://learn.microsoft.com/en-us/dotnet/core/compatibility/10)
(page self-describes as still not exhaustive). Filtered to this codebase; everything
omitted (ASP.NET, EF, WinForms/WPF, containers, cryptography API renames, LDAP, mail,
Tar, MacCatalyst…) touches no go2cs surface.

| Change | Affected go2cs surface | Risk | Action |
|:--|:--|:--|:--|
| [C# 14 overload resolution with span parameters](https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/10.0/csharp-overload-resolution) — first-class span conversions make `Span`/`ReadOnlySpan` overloads applicable in more scenarios (incl. `T[]` receivers) | golib is exactly the shape this targets: `slice<T>`, `@string`, `array<T>` carry implicit conversions and Span-adjacent overloads; converted stdlib call sites bind against them. Shifts can surface as new CS0121 ambiguities (source) or as a *different overload silently binding* (behavioral) | **Medium** — the highest-attention item of the hop | Stage-1 full behavioral suite (549 projects, byte goldens + 515 stdout comparisons vs `go run`) is the detection net and is already owed. Triage any CS0121 at golib, not per-call-site. The Expression-interpretation sub-case is irrelevant (no Expression trees in golib) |
| `LangVersion=latest` pulls all of C# 14 in at Stage 1 (see §2 above) | Entire corpus | Medium (attribution risk, not correctness risk) | State in Stage-1 record; optional LangVersion=13 control for attribution only |
| [`dotnet` CLI commands log non-command-relevant data to stderr](https://learn.microsoft.com/en-us/dotnet/core/compatibility/sdk/10.0/dotnet-cli-stderr-output) | The PS 5.1 harness scripts — the CLAUDE.md-documented `$ErrorActionPreference='Stop'` + NativeCommandError trap means *informational* stderr from `dotnet build/restore` can now terminate a wrapper that pipes it. `BehavioralRunner`'s Output-phase stderr **comparison** is safe (it compares the transpiled *program's* stderr, run as an apphost exe — Program.cs:947) | **Medium** | Audit every harness invocation of `dotnet` for stderr piping/redirection under `Stop` before Stage 1; the r41 stderr-wrapper lesson already in CLAUDE.md is the same class |
| [.NET CLI `--interactive` defaults to true in user scenarios](https://learn.microsoft.com/en-us/dotnet/core/compatibility/sdk/10.0/dotnet-cli-interactive) | Long-running harness runs in interactive terminals could theoretically block on an auth prompt | Low — only auth-needing feeds prompt; the repo restores from nuget.org | None; add `--interactive false` only if a hang is ever observed at a restore |
| [PackageReference without a version raises NU1015](https://learn.microsoft.com/en-us/dotnet/core/compatibility/sdk/10.0/nu1015-packagereference-version) | Converter templates + `-recurse=nuget` emissions (they emit `Version="$(GoStdLibVersion)"` — believed compliant) | Low | One `-recurse=nuget` conversion at Stage 1 as a probe; owed anyway by the §5.3 `-tests`/recurse proving |
| [NU1510 raised for direct references pruned by NuGet](https://learn.microsoft.com/en-us/dotnet/core/compatibility/sdk/10.0/nu1510-pruned-references) | Any csproj directly referencing a framework-overlapping package | Low — corpus references are project refs + go.* packages | Watch the Stage-1 warning delta; disposition once |
| [Packages with no runtime assets dropped from deps.json](https://learn.microsoft.com/en-us/dotnet/core/compatibility/sdk/10.0/deps-json-trimmed-packages) | Consumers of the published `go.gen` analyzer package (analyzer = no runtime assets) | Low — deps.json bookkeeping only | Note for the §11 publication rehearsal; verify a consumer restore |
| [`dotnet restore` audits transitive packages](https://learn.microsoft.com/en-us/dotnet/core/compatibility/sdk/10.0/nugetaudit-transitive-packages) + HTTP-warnings-to-errors | Restore noise in gates; possible NU19xx warnings appearing fleet-wide | Low | Classify in the Stage-1 warning delta |
| [`dotnet new sln` defaults to SLNX](https://learn.microsoft.com/en-us/dotnet/core/compatibility/sdk/10.0/dotnet-new-sln-slnx-default) | None — repo is already `.slnx`-native (go2cs.slnx, go2cs-stdlib.slnx) | **Favorable** | None. (The VS shared-items save-prompt caveat is a SolutionPersistence issue, orthogonal to the hop) |
| `dotnet test` runner selection: **VSTest remains the default**; MTP (Microsoft.Testing.Platform) is opt-in via `global.json` `{"test":{"runner":"Microsoft.Testing.Platform"}}` ([MS Learn](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-integration-dotnet-test), [dotnet test docs](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test)) | MSTest-hosted `BehavioralTests` continues to run under VSTest unchanged; every testhost-lock caveat in CLAUDE.md remains applicable | Low | **Do not put a `test` key in the Stage-2 `global.json`** — an accidental MTP opt-in changes the test host shape mid-migration. MTP is the direction of travel (VSTest mode is "legacy" for MTP projects); evaluate as a *separate* post-migration item, never inside the hop |
| [Runtime no longer provides default SIGTERM handlers](https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/10.0/sigterm-signal-handler) | Converted `os/signal` / runtime shutdown behavior on the **Linux lane** (PLAN-linux-operation) | Low on Windows; Medium-flagged for Linux ops | Note in the Linux plan; no Windows-gate impact |
| [Consistent shift behavior in generic math](https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/10.0/generic-math) | golib numeric types if any implement `IShiftOperators`; Go shift semantics are converter-emitted with explicit masking | Low | Behavioral suite covers; no pre-work |
| Reflection/DAM annotation changes ([IReflect/InvokeMember annotations](https://learn.microsoft.com/en-us/dotnet/core/compatibility/reflection/10/ireflect-damt-annotations), [DefaultValueAttribute DAM removal](https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/10.0/defaultvalueattribute-dynamically-accessed-members)) | golib's reflective formatting layer + converted `reflect` — the exact surface the §8 trim audit re-measures | Low at JIT; feeds §8 | Absorbed by the §8 diagnostic re-measure (which the runbook already mandates) |
| [Single-file apps no longer search executable directory for native libraries](https://learn.microsoft.com/en-us/dotnet/core/compatibility/interop/10.0/native-library-search) | Only if §9's shape review adopts a single-file test host; syscall P/Invokes target system libraries and are unaffected | Low | Note in the §9 shape-pricing checklist |
| [BufferedStream.WriteByte no implicit flush](https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/10.0/bufferedstream-writebyte-flush) | None expected — converted Go IO (`bufio` etc.) is its own port, not `BufferedStream` | Low | One grep for `BufferedStream` in golib/harnesses before Stage 1 closes it |
| Custom MSBuild tasks may fail to load under SDK 10 (MSB4062, [dotnet/msbuild #12756](https://github.com/dotnet/msbuild/issues/12756)) | None — the repo ships props/targets logic only, no compiled MSBuild tasks | None | Verify-by-inspection line in the Stage-1 record |
| [`DefineConstants` not available at evaluation time](https://learn.microsoft.com/en-us/dotnet/core/compatibility/sdk/10.0/defineconstants-not-available-at-evaluation) | Any props/targets conditioning on `$(DefineConstants)` at evaluation — `$(GoTargetOS)` conditions are item/property-based, believed unaffected | Low | One grep across props/targets |

---

## 4. Generator / analyzer compatibility — verdict: **COMPATIBLE, no change owed**

- `go2cs-gen` targets **netstandard2.0** with `Microsoft.CodeAnalysis.CSharp 4.10.0` +
  `Microsoft.CodeAnalysis.Analyzers 3.3.4`, `PrivateAssets=all` (go2cs-gen.csproj:7,33-34).
- netstandard2.0 remains the **required and supported** analyzer/generator target under the
  .NET 10 SDK and VS 2026 — the hosting rationale is unchanged (the loading host may not run
  the latest runtime) ([roslyn discussion #72777](https://github.com/dotnet/roslyn/discussions/72777),
  [Andrew Lock on SDK-version support in analyzers](https://andrewlock.net/supporting-multiple-sdk-versions-in-analyzers-and-source-generators/)).
- Roslyn's analyzer ABI is backward-compatible: a generator compiled against CodeAnalysis
  4.10 loads in the newer compiler. The incremental-generator API surface
  (`IIncrementalGenerator`) carries no .NET-10-era break.
- No bump of `Microsoft.CodeAnalysis.CSharp` is needed: the generators parse
  **converter-emitted** C#, whose shape go2cs controls — C# 14 syntax nodes will never
  appear in their input unless the converter starts emitting them (a converter decision,
  not a migration one).
- The MSB4062 custom-task incompatibility class does **not** apply to analyzers.

One watch item: the four generators run inside the *new* compiler at Stage 1 — any
generator-driver behavior delta would surface as golden drift in `TypeGenerator`/`RecvGenerator`
output and be caught by the behavioral Target phase. No pre-work; the gate covers it.

---

## 5. Native AOT / ILC — verdict: **COMPATIBLE; measurement sequencing is the whole story**

- Native AOT is fully supported and further invested-in in .NET 10; ILC trims metadata more
  aggressively, and new SDK defaults resolve more trim warnings without manual
  `[DynamicDependency]` ([Native AOT overview](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/),
  [state of Native AOT in .NET 10](https://code.soundaranbu.com/state-of-nativeaot-net10)).
- **Trap 1 stands unchanged and is the plan's spine:** ILC is a runtime pack selected by
  the *TFM*, so **no .NET 10 AOT number is reachable until Stage 2** — the SDK-only stage
  publishing net9.0 resolves the .NET 9 ILC. Schedule AOT measurement after the TFM move
  only, per runbook §7.
- Repo facts confirmed: the perf tree sets `SuppressTrimAnalysisWarnings=true` in its
  `Directory.Build.props` (line 12; deliberate, with the §8.1 override-locally rule), AOT
  is `PerfAot`-gated with a separate intermediate tree, and the trim mode is `partial`
  (rooted-whole closure) for the documented reflection reason. The §8 audit re-measures
  the diagnostic census under the new ILC — expect the count to move in **both** directions
  (new aggressive-trim diagnostics appear; SDK-default resolutions remove others). A risen
  count is information, not a blocker (runbook §10 table).
- **Compile-time cost: no published evidence of a material ILC throughput change either
  way.** Budget the measured ~25 min/benchmark (i7-5820K, full converted-stdlib closure)
  until the hop's own purge-first publish re-measures it. Trap 2's seconds-long-publish
  tell applies to every cross-SDK A/B.
- The DAM-annotation breaking changes in §3 land exactly on the trim-audit surface —
  fold them into the §8 diagnostic classification rather than treating them separately.

---

## 6. Performance expectations (feeds §6/§7's falsifiable predictions)

Source: [Performance Improvements in .NET 10](https://devblogs.microsoft.com/dotnet/performance-improvements-in-net-10/)
(Stephen Toub; ~300 perf PRs), [Announcing .NET 10](https://devblogs.microsoft.com/dotnet/announcing-dotnet-10/).
The .NET 10 JIT work concentrates on **exactly the abstractions go2cs's emitted code leans
on**, which makes this hop's JIT column genuinely interesting:

| .NET 10 improvement | go2cs surface it lands on | Published magnitude (their benchmarks, not ours) |
|:--|:--|:--|
| Escape analysis: **delegates/closures stack-allocated** when non-escaping | defer machinery, closures, function values | closure+delegate pattern 19.53ns → 6.69ns (~2.9x) |
| **`try/finally` no longer blocks inlining** | the defer pattern's structural shape | inlinable `Monitor.Enter/Exit` cited |
| Interface **devirtualization + GDV** in shared generics; array-interface devirtualization | golib's duck-typed interface wrappers, `Interface<T>` sort, `IEnumerable` adapters | `ReadOnlyCollection` foreach +20%; generic equality +46%; IEnumerable-over-array 109.9ns → 35.5ns w/ PGO |
| **Bounds-check elimination** (lookup tables, switch-on-length, consecutive writes, span loop cloning) | slice/array indexing throughout the converted corpus | pattern-dependent |
| Array/span escape analysis; stack-allocated small arrays | `slice<T>` temporaries, conversion shims | array-in-span 9.77ns → 0.87ns |
| Inlining budget **more than doubled** | small emitted helper methods everywhere | — |
| GC write-barrier work (incl. Arm64 8–20% pause reduction; x64 gains smaller) | all benchmarks; Arm64 numbers do not transfer to the x64 perf-canon host | — |

**Predictions to state before the §6 run** (so the surprise is information):
1. JIT column improves broadly; the **interface-heavy** (IfaceCall, Iface, IfaceShell) and
   **closure/defer-heavy** benchmarks are the biggest candidates.
2. The Go column (same-day control) must not move — it is the control.
3. **The disclosure-manifest consequence (runbook §9.1) is live for this hop:** the
   alloc-count disclosures (`testing.AllocsPerRun`-pinned entries, e.g. bytes/strings) rest
   on CLR allocation behavior — .NET 10's escape analysis removes allocations, so some
   disclosed assertions **may begin passing**, breaking the sweep's disclosure arithmetic
   *loudly and by design*. Enumerate the affected manifest entries from the committed
   manifests (not prose) **before** the closing sweep, and treat an arithmetic break there
   as the predicted outcome, not an alarm. Conversely note §9.2.3: entries resting on
   deliberately-foreclosed properties must *keep failing*.
4. AOT: state the §7 attribution prediction (does the new ILC+framework pair reproduce the
   externally-observed advantage?) in the stage record before the run.

History mechanics are already in place: the perf README's `PERF-RESULTS` markers + History
section exist for exactly this .NET 9 → 10 table pair; bank via `--update-readme`, never
by hand (runbook §6.7).

---

## 7. Go / no-go signals

**Go-side facts (all green today):**
- GA + 9 months of servicing banked (10.0.0 → 10.0.11); LTS through 2028.
- Analyzer/generator stack compatible as-is; no repo change owed.
- VSTest remains the default test runner; no forced harness migration.
- `.slnx` is now the SDK default going forward — the repo is ahead of the curve.
- The TFM bump is one line by design (`src/Directory.Build.props:27`), with the inert-copy
  and non-inert-family accounting already written in runbook §5.

**Signals that would DELAY the hop (each maps to a stage gate):**
1. **Stage-1 behavioral divergence attributable to C# 14 span overload resolution** wider
   than a fix-forward in golib — the one breaking change aimed at this codebase's shape.
2. **Harness stderr trap firing** — a PS 5.1 wrapper dying on the CLI's new informational
   stderr; cheap to audit ahead, expensive to debug mid-gate.
3. **AOT publish cost balloon** on the i7-5820K beyond watchdog budgets — re-size from the
   measured run, per the safety-net doctrine.
4. **Trim-diagnostic census explosion** under the new ILC that re-prices the §8 audit.
5. **NuGet packing regressions** (NU1015/NU1510/deps.json) surfacing in the §11 release
   rehearsal across the ~300-package pack.
6. **Disclosure-arithmetic breaks that were NOT predicted** — a permanent entry passing is
   an investigation (runbook §9.2.3/§10), distinct from the predicted alloc-count retirements.

**Signals that would ACCELERATE it:** the .NET 9 EOL wall (2026-11-10). Working backward
from a pre-EOL close with the closing gates' measured budgets (solo AOT perf run = hours;
full sweep ≈ 1 hr+ solo; both coordinator-owned), Stage 0–1 should start with weeks of
margin, not days. VS 2026 (18.x) provisioning for any machine doing IDE work post-TFM
belongs in the Stage-0 checklist alongside the SDK.

**Bottom line: no blocker found. The hop is GO on readiness grounds and MANDATORY on
schedule grounds (.NET 9 EOL 2026-11-10). Highest-attention item: C# 14 span overload
resolution against golib at Stage 1; cheapest pre-work: the harness stderr audit.**

---

## Sources

- https://github.com/dotnet/core/blob/main/release-notes/10.0/README.md — GA/LTS dates, release table
- https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.11/10.0.11.md — SDK bands 10.0.111/303/400, VS 18.9
- https://support.microsoft.com/en-us/servicing/dotnet/net-10/2026/net-10-0-update-august-11-2026 — 10.0.11 servicing
- https://devblogs.microsoft.com/dotnet/dotnet-and-dotnet-framework-august-2026-servicing-updates/ — Aug 2026 servicing (10.0.11 / 9.0.19 / 8.0.30)
- https://devblogs.microsoft.com/dotnet/dotnet-8-9-end-of-support/ — .NET 8 & 9 EOL 2026-11-10
- https://devblogs.microsoft.com/dotnet/dotnet-sts-releases-supported-for-24-months/ — STS 24-month extension
- https://learn.microsoft.com/en-us/dotnet/core/compatibility/10 — breaking-changes index (self-described incomplete)
- https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/10.0/csharp-overload-resolution — C# 14 span overload resolution
- https://learn.microsoft.com/en-us/dotnet/core/compatibility/sdk/10.0/dotnet-cli-stderr-output — CLI stderr change
- https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-integration-dotnet-test — MTP opt-in via global.json; VSTest default
- https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test — dotnet test runner selection
- https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14 — C# 14 features
- https://github.com/dotnet/roslyn/discussions/72777 — netstandard2.0 generator requirement rationale
- https://andrewlock.net/supporting-multiple-sdk-versions-in-analyzers-and-source-generators/ — analyzer/SDK version support
- https://github.com/dotnet/msbuild/issues/12756 — MSB4062 custom-task class (not applicable here)
- https://github.com/dotnet/docs/issues/48320 — VS version requirements for .NET 10 SDK / net10.0 targeting
- https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/ — Native AOT deployment
- https://devblogs.microsoft.com/dotnet/performance-improvements-in-net-10/ — Toub perf post (escape analysis, devirtualization, bounds checks, inlining)
- https://devblogs.microsoft.com/dotnet/announcing-dotnet-10/ — GA announcement
- Repo (C:\Projects\go2cs, master, read-only): `docs/DotNetMigration.md`; `src/Directory.Build.props` (TFM fallback line 27); converted csprojs (`LangVersion=latest`); `src/gen/go2cs-gen/go2cs-gen.csproj` (netstandard2.0, CodeAnalysis 4.10.0); `src/tests/Performance/Directory.Build.props` (SuppressTrimAnalysisWarnings, PerfAot); `src/tests/Behavioral/BehavioralRunner/Program.cs` (Output-phase stderr comparison, line 947)
