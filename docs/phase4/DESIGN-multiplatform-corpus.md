# DESIGN — one corpus, three platforms: what a multiplatform standard library costs, and how it ships

> **STATUS: ACCEPTED** (user ruling 2026-08-08: recommendations accepted as written — layout L3 + packaging option (a) RID assemblies; increments proceed in order).
> **Increment 1 LANDED 2026-08-08** — the converter now takes the census itself (`-platforms` list +
> `-platform-census`), and it reproduces every number below; see §12. It changes no
> emitter, no corpus, and no packaging. Everything below is **measured** — four seeded full-standard-library
> conversions, three `go list` censuses and a `go/types` API-surface probe, run 2026-08-08 in lane
> `r47c-goosdesign` against `739f3606ad`. Where a measurement contradicts an earlier ruling
> ([`PLAN-linux-operation.md`](../PLAN-linux-operation.md) §A4 **N2**), it is flagged as such in §7 rather
> than quietly reversed; the user rules.
>
> **Prior art, read first:** `PLAN-linux-operation.md` §A2 (what `src/core` *is*), §A2.4 (corpus options
> 1–4, ruled: option 2), §A4 (packaging options N0–N3, ruled: N2 as destination). This document supplies
> the numbers those two sections were explicitly written *without* — §A2.4 says "do **not** plan the tree
> layout before that number exists". The number now exists.

---

## 1. The question

The standing goal is **"a single set of NuGet packages with binaries included that will work for all
target platforms"** — Windows, Linux, macOS (the big three, ruled 2026-08-06).

The corpus contradicts that goal today. `src/core` is one conversion, taken at `GOOS=windows/GOARCH=amd64`.
Go selects its platform sources at *build* time through filename suffixes and `//go:build` constraints, so
a conversion does not merely *target* a platform — it *is* that platform. `go.os` and `go.syscall` are
published under platform-neutral IDs while containing `kernel32.dll` P/Invokes.

Three questions have to be answered in order, and only the third is a matter of taste:

1. **How much of the corpus actually differs per platform?** (§3–§5 — it is far less than the Go source
   suggests, and the differing set is not the set you would predict.)
2. **What can a single compile-time reference surface truthfully carry?** (§6 — nearly everything.)
3. **Which packaging shape follows from 1 and 2?** (§7–§9.)

---

## 2. Method, and the control that makes it trustworthy

**Four seeded full-stdlib conversions**, each into its own scratch root, run strictly sequentially (never
two converter processes alive at once — the r41 corruption rule). Each root was seeded per the CLAUDE.md
reconvert discipline: `src/core` + `src/version.props` + `docs/validation`, mirroring the repository's
`src/` layout so the hand-own detector and the README badge composer both see what they expect.

| Run | `-platforms` | Wall | Packages queued | Failures |
|:--|:--|--:|--:|--:|
| control | `windows/amd64` | 197 s | 304 | **0** |
| A | `linux/amd64` | 184 s | 302 | **0** |
| B | `darwin/amd64` | 191 s | 303 | **0** |
| C | `darwin/arm64` | 196 s | 301 | **0** |

**The control validates the instrument.** `conv-windows/src/core` compared against the committed
`src/core`: every `.cs`, every `.csproj` and every `README.md` **byte-identical**; the converter rewrote
**0** `.csproj` files because none had changed. The only 12 differences in the entire tree are the
`.cs.auto` review siblings, which the overlay rule deliberately freezes and which CLAUDE.md already records
as stale (CleanupBacklog item 18 — 11 of 16 at r40, 12 of 16 now). So the diff instrument reads zero on a
null input, and every delta reported below is a genuine platform effect rather than converter
non-determinism.

**Finding zero, obtained for free: the converter already produces a Linux and a macOS corpus, cleanly.**
All four runs completed with **zero failures** in ~3 minutes each. `-platforms os/arch` is already threaded
into every `go/packages` load and into the converter's own filename/`//go:build` constraint evaluator
(`directiveOperations.go`'s `goodOSArchFile` reimplementation). **No converter change is required to
*generate* a non-Windows corpus.** Everything this design proposes is about *organising and shipping*
that output, not about producing it.

A companion census used `go list` under each `GOOS` with the corpus's own tags
(`-tags purego,math_big_pure_go`, `CGO_ENABLED=0`, `GO111MODULE=off`) and a `go/types` probe enumerating
package-scope exported names per target.

---

## 3. Census A — the Go source is 89 % platform-neutral

Comparing `GoFiles + SFiles` per package across `windows`/`linux`/`darwin` at `amd64`, over the
convert-eligible set (the union of all three, minus the `isNonConvertedStdLibPackage` skips):

| | Packages |
|:--|--:|
| Convert-eligible (union of W/L/D) | **307** |
| **Identical source set on all three** | **274 (89.3 %)** |
| Differing source set | 27 |
| Present on only some platforms | 6 |

The six that do not exist everywhere:

| Package | Exists on |
|:--|:--|
| `internal/syscall/windows`, `.../registry`, `.../sysdll` | Windows only |
| `internal/runtime/syscall` | Linux only |
| `crypto/x509/internal/macos`, `vendor/golang.org/x/net/route` | macOS only |

The 27 with differing file sets, by size of the platform-exclusive part:

| Package | W files | L files | D files | common | W-only | L-only | D-only |
|:--|--:|--:|--:|--:|--:|--:|--:|
| `runtime` | 161 | 175 | 167 | 138 | 17 | 23 | 15 |
| `syscall` | 13 | 29 | 32 | 3 | 10 | 16 | 19 |
| `net` | 54 | 59 | 62 | 38 | 14 | 11 | 12 |
| `os` | 29 | 36 | 35 | 17 | 9 | 10 | 8 |
| `internal/syscall/unix` | 1 | 19 | 15 | 0 | 0 | 14 | 9 |
| `internal/poll` | 13 | 22 | 19 | 7 | 6 | 8 | 5 |
| `os/user` | 3 | 5 | 6 | 2 | 1 | 3 | 4 |
| `runtime/pprof` | 11 | 11 | 13 | 9 | 2 | 1 | 3 |
| `crypto/x509` | 12 | 13 | 12 | 11 | 1 | 2 | 1 |
| `internal/goos` | 3 | 3 | 3 | 1 | 2 | 1 | 1 |
| `crypto/rand` | 3 | 4 | 4 | 2 | 1 | 1 | 1 |
| `internal/sysinfo` | 2 | 2 | 2 | 1 | 1 | 1 | 1 |
| `net/internal/socktest` | 4 | 5 | 4 | 2 | 2 | 1 | 0 |
| `time` | 11 | 10 | 10 | 8 | 3 | 0 | 0 |
| `archive/tar` | 5 | 7 | 7 | 5 | 0 | 1 | 1 |
| *(12 more with a 1–2 file delta)* | | | | | | | |

Whole-corpus source volume: **1,408** Go+asm files for Windows, **1,477** for Linux, **1,479** for macOS.
The 33 platform-varying packages hold **381 / 448 / 448** of them — roughly 27–30 % of the source, but
only 11 % of the packages.

**Closure.** Of the 274 identical-source packages, **58 are fully platform-free** — nothing in their
transitive import closure varies. The other **216 have identical source but import something that varies**.
That 216 is an *upper bound* on "might still emit differently", not a measurement. §4 measures it, and the
answer is far smaller.

---

## 4. Census B — the converted corpus is 96 % byte-identical (the number that matters)

Windows vs Linux, both `amd64`, comparing the `.cs` each run actually emitted:

| | Files |
|:--|--:|
| Emitted by the Windows run | 1,665 |
| Emitted by the Linux run | 1,734 |
| Emitted by **both** | 1,563 |
| → **byte-identical** | **1,498 (95.8 %)** |
| → content differs | **65** |
| Windows-only filenames | 102 |
| Linux-only filenames | 171 |

**34 of 302 packages carry any delta at all. 268 emit byte-identical C#.**

Three-way at fixed `amd64` (Windows / Linux / macOS):

| | Files |
|:--|--:|
| Union of emitted `.cs` names | 1,928 |
| Byte-identical on all three | **1,489** |
| Same name, **different content** | **69** |
| Emitted by exactly two (the shared `unix` variants) | 88 |
| Platform-exclusive (exactly one) | 282 |
| **Packages with any delta** | **37** |

Project files track the same shape. At fixed `amd64` the Linux run rewrote **21** `.csproj` (20 differing
from the Windows control, plus the one Linux-exclusive package), the macOS run **22** (20 + its two
exclusives), and the Windows control **0**. Those are the `<ProjectReference>` lists — **24 packages have a
different direct import set** across the three (21 emitted everywhere whose reference list differs, plus the
3 platform-exclusive packages), which is exactly what a conditioned reference block must express.

> **Two numbers in this paragraph were corrected on 2026-08-08 by increment 1's census** (lane r47d), which
> re-measures every table in §3–§5 from the converter's own emission. It reproduced all of them exactly
> except here: this paragraph read "the macOS run 26 … 22 packages". The **26** is `darwin/arm64`'s rewrite
> count — run C, not run B — which §5's own two-target census reproduces exactly (`darwin/amd64` 22,
> `darwin/arm64` 26); §4 is fixed at `amd64`, so 22 is its number. The **22 packages** is not reproducible as
> a set from any run: the measured union across the three `amd64` targets is 24. Nothing else moved, and no
> `.cs` number did.

**Linux and macOS are close to each other and far from Windows** — the fact that makes the third platform
cheap:

| Pair | Emitted by both | Byte-identical | Differ | Packages touched |
|:--|--:|--:|--:|--:|
| Windows ↔ Linux | 1,563 | 1,498 | 65 | 34 |
| Windows ↔ macOS | 1,566 | 1,499 | 67 | 35 |
| **Linux ↔ macOS** | **1,633** | **1,597** | **36** | **20** |

### 4.1 The finding the plan did not anticipate: one Go file, two C# files

`PLAN-linux-operation.md` §A2.4's option 2 describes "one `src/core/<pkg>` holding `file_windows.cs` **and**
`file_linux.cs`, with `<Compile Include>` conditioned on `$(GoTargetOS)`". That shape assumes the
platform split is expressed in *filenames*. It is not. **38 converted source files have one Go source and
two or three different C# emissions**, under a neutral filename that is identical on every platform.

Five distinct mechanisms produce this, each verbatim from the probe:

**(a) Constant folding of a platform constant** — `path/filepath/path.cs`:

```diff
-public static UntypedInt Separator     => /* os.PathSeparator */ 92;
-public static UntypedInt ListSeparator => /* os.PathListSeparator */ 59;
+public static UntypedInt Separator     => /* os.PathSeparator */ 47;
+public static UntypedInt ListSeparator => /* os.PathListSeparator */ 58;
```

**(b) A folded constant reaching a literal** — `go/build/build.cs`:

```diff
-    @string sep = "\\";
+    @string sep = "/";
```

**(c) Escape analysis** — `syscall/syscall.cs`. On Linux the unix wrappers take the address of `_zero`, so
the converter promotes it to a heap box; on Windows nothing does, and it stays a plain static:

```diff
-internal static uintptr _zero;
+internal static ж<uintptr> Ꮡ_zero = new(default(uintptr));
+internal static ref uintptr _zero => ref Ꮡ_zero.Value;
```

**(d) Cross-file name-collision renaming** — `os/proc.cs`. On Windows a second `init()` exists in
`exec_windows.go`, so this one is renamed; on Linux there is no collision:

```diff
-[GoInit] internal static void initΔ1() {
+[GoInit] internal static void init() {
```

**(e) A constant-driven dead branch** — `net/conf.cs`, where `!cgoAvailable` folds to a constant `true`
and the converter emits golib's non-foldable true marker:

```diff
-                case {} when !cgoAvailable: {
+                case {} when ᐧᐧ: {
```

**Consequence.** A flat union directory cannot hold "both platforms' files", because 38 file *names* would
need two or three contents. Any layout must give those files per-platform identities. (b), (c) and (d) also
kill any scheme based on rewriting filename suffixes: the divergence is produced by whole-package analysis,
not by the file's own name.

**Corollary — and it is good news.** Platform-*exclusive* files never collide with one another. Every
`GOOS` variant of a Go package lives in **one** GOROOT directory, so Go has already guaranteed that
`file_windows.go`, `file_unix.go` and `file_linux.go` have distinct names. Only the neutral-named shared
files can collide, and those are the 38.

### 4.2 The platform axis must be computed from the emission, not from the file sets

Four packages have **identical Go source on all three platforms but different emitted C#** — `go/build`,
`internal/buildcfg`, `os/signal`, `time/tzdata` — through mechanisms (a)/(b) above and through
`package_info.cs` closure changes.

Three packages have **differing Go source but identical Windows↔Linux emission** (their variance is
macOS-only): `crypto/x509/internal/macos`, `internal/testpty`, `vendor/golang.org/x/net/route`.

So the set of packages needing per-platform treatment is **not** derivable from `go list`. It must come
from the converter's own three-target emission. A converter that conditioned on filename suffixes or on
`GoFiles` deltas would be wrong in both directions.

### 4.3 The 69 same-name-different-content artifacts

| Kind | Count | Note |
|:--|--:|:--|
| Converted source `.cs` | 38 | the §4.1 set |
| `package_info.cs` | 27 | closure-derived: `ImportedTypeAliases`, `GoImplement` records, `TypeAccessibility` |
| `package_init.cs` | 4 | Go's `InitOrder` differs when the file set does |

The 38 source files, in full: `crypto/x509/verify.cs`, `go/build/build.cs`,
`internal/buildcfg/zbootstrap.cs`, `internal/sysinfo/sysinfo.cs`, `internal/testenv/exec.cs`, `net/conf.cs`,
`net/dial.cs`, `net/dnsclient_unix.cs`, `net/fd_posix.cs`, `net/internal/socktest/switch.cs`,
`net/iprawsock_posix.cs`, `net/ipsock_posix.cs`, `net/lookup.cs`, `net/net.cs`, `net/nss.cs`, `net/pipe.cs`,
`net/sock_posix.cs`, `net/sockaddr_posix.cs`, `net/tcpsock_posix.cs`, `net/udpsock_posix.cs`,
`net/unixsock_posix.cs`, `os/file.cs`, `os/proc.cs`, `path/filepath/path.cs`, `runtime/arena.cs`,
`runtime/extern.cs`, `runtime/malloc.cs`, `runtime/mcheckmark.cs`, `runtime/mgcscavenge.cs`,
`runtime/mheap.cs`, `runtime/proc.cs`, `runtime/runtime.cs`, `runtime/select.cs`, `runtime/stack.cs`,
`runtime/trace.cs`, `runtime/traceexp.cs`, `runtime/tracetime.cs`, `syscall/syscall.cs`.

A worked miniature, small enough to read whole — `internal/goos`, emitted per platform:

| Windows | Linux | macOS |
|:--|:--|:--|
| `goos.cs` | `goos.cs` | `goos.cs` *(byte-identical on all three)* |
| `nonunix.cs` | `unix.cs` | `unix.cs` |
| `zgoos_windows.cs` | `zgoos_linux.cs` | `zgoos_darwin.cs` |
| `package_info.cs` | `package_info.cs` | `package_info.cs` *(identical)* |

`unix.cs` and `nonunix.cs` are the whole of Go's `unix.go`/`nonunix.go`:

```csharp
// unix.cs  (linux, darwin)             // nonunix.cs  (windows)
partial class goos_package {            partial class goos_package {
public const bool IsUnix = true;        public const bool IsUnix = false;
} // end goos_package                   } // end goos_package
```

Keep that pair in mind for §7 option (d).

---

## 5. Census C — the GOARCH axis is real but an order of magnitude smaller

macOS at `amd64` vs `arm64`, everything else fixed:

| | Files |
|:--|--:|
| Emitted by both | 1,701 |
| Byte-identical | **1,687** |
| Content differs | **14** |
| `amd64`-only names | 32 |
| `arm64`-only names | 31 |
| **Packages touched** | **16** |

The 16: `hash/crc32`, `internal/abi`, `internal/buildcfg`, `internal/bytealg`, `internal/cpu`,
`internal/goarch`, `internal/runtime/atomic`, `math`, `runtime`, `runtime/internal/startlinetest`,
`runtime/internal/sys`, `runtime/pprof`, `runtime/race`, `runtime/race/internal/amd64v1`, `syscall`,
`vendor/golang.org/x/sys/cpu`.

**This matters for sizing.** A RID is `os-arch`, not `os`. If both architectures are shipped for each of
the big three, the runtime matrix is six (`win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64`,
`osx-arm64`), not three. Proportionally the arch axis is about a fifth of the GOOS axis: **77 of 1,764**
artifact names are arch-sensitive (4.4 %), against **439 of 1,928** that are GOOS-sensitive (22.8 %). Cheap
to carry — but not free, and a design that silently assumes three RIDs will be wrong the first time someone
runs on an ARM box. §12's staging therefore proves one RID pair before generalising.

---

## 6. Census D — the exported API surface, and why it decides the packaging

The packaging question is not "do the *implementations* differ" (§4 says: for 37 packages, yes). It is
**"can one compile-time reference assembly truthfully describe all three platforms?"** That is a question
about *exported* surface only. Measured with a `go/types` probe over every platform-varying package,
counting package-scope exported names:

| Package | W | L | D | common | Verdict |
|:--|--:|--:|--:|--:|:--|
| `syscall` | 992 | 2,186 | 1,899 | **270** | **DIVERGENT** |
| `internal/syscall/unix` | 1 | 34 | 62 | 1 | DIVERGENT *(internal)* |
| `internal/syscall/windows` | 202 | 0 | 0 | 0 | DIVERGENT *(internal)* |
| `internal/syscall/windows/registry` | 38 | 0 | 0 | 0 | DIVERGENT *(internal)* |
| `internal/syscall/windows/sysdll` | 2 | 0 | 0 | 0 | DIVERGENT *(internal)* |
| `internal/runtime/syscall` | 0 | 25 | 0 | 0 | DIVERGENT *(internal)* |
| `internal/poll` | 18 | 19 | 16 | 15 | DIVERGENT *(internal)* |
| `log/syslog` | **0** | 33 | 33 | 0 | **DIVERGENT** *(absent on Windows)* |
| `os` | 115 | 115 | 115 | 115 | identical |
| `net` | 102 | 102 | 102 | 102 | identical |
| `crypto/x509` | 109 | 109 | 109 | 109 | identical |
| `time` | 73 | 73 | 73 | 73 | identical |
| `runtime` | 48 | 48 | 48 | 48 | identical |
| `internal/testenv` | 38 | 38 | 38 | 38 | identical |
| `archive/tar` | 31 | 31 | 31 | 31 | identical |
| `path/filepath` | 27 | 27 | 27 | 27 | identical |
| `internal/goos` | 20 | 20 | 20 | 20 | identical |
| `net/internal/socktest` | 14 | 14 | 14 | 14 | identical |
| `runtime/pprof` | 14 | 14 | 14 | 14 | identical |
| `os/user` | 11 | 11 | 11 | 11 | identical |
| `mime` | 10 | 10 | 10 | 10 | identical |
| `os/exec` | 9 | 9 | 9 | 9 | identical |
| `internal/fuzz` | 9 | 9 | 9 | 9 | identical |
| `crypto/rand` | 4 | 4 | 4 | 4 | identical |
| *(6 more, 1–15 names each)* | | | | | identical |

**Only 8 of 29 varying packages have a divergent exported surface, and only two of those are user-facing:
`syscall` and `log/syslog`.** Everything a normal converted program touches — `os`, `net`, `time`,
`path/filepath`, `runtime`, `os/exec`, `os/user`, `crypto/x509`, `archive/tar`, `mime` — presents a
**name-for-name identical** public API on Windows, Linux and macOS.

This is the single most consequential measurement in the document. It means a **neutral compile-time
reference assembly is truthful for 27 of 29 varying packages and for all 268 non-varying ones** — the
compile surface is not the problem. Only the *runtime* implementation needs to vary, which is precisely
the shape .NET's RID-specific asset mechanism exists to express.

`syscall` is the genuine exception, and it is genuine: 992 vs 2,186 vs 1,899 exported names with 270 in
common. No honest single reference assembly describes that. `log/syslog` is the trivial exception — the
package exists on Windows but exports nothing, because Go's build constraint excludes its whole
implementation there.

---

## 7. Census E — the hand-owned files are almost all platform-neutral

51 hand-owned files (41 carrying a line-anchored `[module: GoManualConversion]`, plus the `*_impl.cs`
companions that do not).

| Class | Count |
|:--|--:|
| In a platform-neutral package | 33 |
| In a platform-varying package | 18 |
| — of which the *Go file they replace* is Windows-only | **5** |
| Carrying any P/Invoke at all | **4** |

The five genuinely Windows-bound hand-owns: `os/dir_windows_impl.cs`, `os/file_windows_impl.cs`,
`syscall/dll_windows.cs`, `syscall/exec_windows.cs`, `syscall/zsyscall_windows_impl.cs`.

The four P/Invoke carriers: those first three, plus `time/time_impl.cs` — whose single
`CreateWaitableTimerExW` call already sits behind `if (!OperatingSystem.IsWindows()) return null;` with a
documented coarse-wait fallback. **`golib` and `go2cs-gen` remain provably clean** (plan §A3/F10).

Two specifics worth carrying forward:

- **`runtime/lock_sema_impl.cs` needs a sibling, not a port.** `lock_sema.go` is selected on Windows *and*
  macOS; Linux uses `lock_futex.go` instead. The Linux corpus needs its own hand-own at a different
  filename — which the layout in §8 accommodates naturally.
- **`time` may need no Linux hand-own at all.** Go's Linux zone loading is `time/zoneinfo_read.go`, pure Go
  that converts cleanly; the Windows hand-own exists only because `GetTimeZoneInformation` is a syscall.
  This is an argument *for* per-platform emission and *against* hand-owning a dispatcher (plan §A7.1).

The remaining 33 neutral hand-owns (`sync/*`, `reflect/*`, `internal/abi/*`, `math/rand/*`, …) are go2cs
runtime machinery. They are unaffected by platform and ship once.

---

## 8. Layout options — what the corpus looks like on disk

All options below keep **one tree**. None resurrects the two-tree doctrine the 2026-08-01 consolidation
deleted; the invariant "a build references exactly one GOOS" is preserved by MSBuild conditioning rather
than by directory separation.

### L1 — flat union, conditioned `<Compile>` items *(the plan's option 2 as written)*

**Rejected by measurement.** §4.1: 38 file names would need two or three contents in one directory.

### L2 — every platform-varying package wholly duplicated into per-GOOS subfolders

Works, and is trivially explained. Cost: the 37 varying packages contain ~420 files that are *identical*
across platforms (`runtime` alone contributes ~138), and L2 triplicates all of them.

### L3 — shared files flat; platform-selected files in per-GOOS subfolders **(recommended)**

The converter, from a single run loading all three targets, classifies every emitted artifact:

- **byte-identical on all three** → written flat, `src/core/<pkg>/<file>.cs`
- **anything else** (content varies, or emitted for only some platforms) → written to
  `src/core/<pkg>/<goos>/<file>.cs`

Measured pricing:

| | Files |
|:--|--:|
| Flat shared `.cs` | 1,489 |
| Per-GOOS `.cs` (variants ×3, pair-shared ×2, exclusives ×1) | 665 |
| **Union tree total** | **2,154** |
| Today's single-platform tree | 1,665 |
| **Growth for full three-platform coverage** | **+29.4 %** |

The csproj change is **one line**. The existing block already removes subfolders and globs the flat
directory:

```xml
<Compile Remove="**/*.cs" />
<Compile Include="*.cs" />
<Compile Include="$(GoTargetOS)/*.cs" />   <!-- new -->
<Compile Remove="*_test.cs;package_test_info.cs;go2cs_test_host.cs" />
```

`package_info.cs` and `package_init.cs` fall out of the same rule with no special case: identical ones stay
flat, varying ones land in the per-GOOS folder (27 and 4 respectively). The `<ProjectReference>` block
gains conditioned `ItemGroup`s for the **24** packages whose direct imports differ (§4, as corrected).

**Converter work L3 implies** — this is the whole feature, and it is bounded:

1. `-platforms` accepts a **list** (`-platforms windows/amd64,linux/amd64,darwin/arm64`). The loader
   plumbing is already per-target; what is new is running the pipeline N times in one process and holding
   the N emissions.
2. A **three-way comparison** at write time decides flat vs per-GOOS. §4.2 is the reason this must be a
   comparison of *emissions*, not of Go file sets.
3. `writeProjectFile` emits the conditioned `Compile` line and per-GOOS `ProjectReference` groups.
4. `$(GoTargetOS)` needs a default. Proposal: default to the *host* OS in a plain `dotnet build`, so a
   Windows developer's `go2cs.slnx` build is unchanged, and set it explicitly per pack pass.
5. `solutionGenerator.go` is unaffected — the project set is the union, which it already computes.

### L4 — per-GOOS **filename suffixes** instead of subfolders

Rejected as fragile: a Go filename may legally contain a dot (`foo.bar.go`), so no suffix scheme is
provably collision-free against the source language, and `<Compile Remove="*.linux.cs">` patterns multiply
where L3 needs one include.

---

## 9. Packaging options — real NuGet mechanics

Three mechanical facts govern everything here:

- `lib/{tfm}/` is a **compile-time and runtime** asset, RID-agnostic. `ref/{tfm}/` is compile-time only.
  `runtimes/{rid}/lib/{tfm}/` is a **runtime-only, RID-selected** managed asset — the same mechanism used
  for native shims, applied to IL. It requires a compile-time asset to exist elsewhere in the package.
- A framework-dependent portable app *does* resolve `runtimes/{rid}/lib/{tfm}` — the assets are recorded in
  `deps.json` under `runtimeTargets` and the host selects by RID at startup. RID-specific assets also
  survive `PublishAot` and self-contained publish, where the RID is explicit.
- **NuGet `<dependencies>` are declared per target framework only — never per RID.** Any package whose
  dependency graph varies by platform must declare the **union**, and accept that some restored assemblies
  go unused on some platforms.

### (a) RID-specific assemblies in one nupkg — **recommended**

```
go.os/
  lib/net9.0/os.dll                        ← compile-time reference (one designated flavor)
  runtimes/win-x64/lib/net9.0/os.dll
  runtimes/linux-x64/lib/net9.0/os.dll
  runtimes/osx-arm64/lib/net9.0/os.dll
```

- **Package IDs unchanged** — ~304, exactly today's set. Nothing new appears on nuget.org.
- **Consumer changes: none.** `dotnet add package go.os` then `dotnet run` works on every platform, and
  `-recurse=nuget` emits the same `PackageReference` it emits today.
- **Sized by measurement: only 37 of 302 packages need RID-specific assets at all** (§4). The other 265
  ship a single `lib/net9.0/` assembly, because their C# is byte-identical *and* every assembly they
  reference presents an identical exported surface (§6) — so one IL image satisfies all platforms.
- **The compile surface is truthful for all but two packages** (§6). `go.syscall` and `go.log.syslog` are
  the named exceptions; §11 lists the options for them as an explicitly open seam.
- **Dependency union.** `go.os` must declare `go.internal.syscall.windows` *and* `go.internal.syscall.unix`
  as dependencies, because the graph cannot vary by RID. Both restore everywhere; only the RID-matched
  assemblies load. A platform-exclusive package still needs a `lib/net9.0/` compile asset — shipping its
  single real flavor there is harmless, since nothing on the other platforms binds it at runtime.
- **Pack cost:** `push-nuget.ps1` grows from one build+pack pass to N build passes (one per RID, differing
  only by `-p:GoTargetOS=`) plus an asset-merge step before `dotnet pack`.

### (b) TFM per OS (`net9.0-windows` + `net9.0`) — *the current N2 ruling; the measurement contradicts it*

This is what `PLAN-linux-operation.md` §A4 records as the user-ruled destination, on the reasoning that
"`net9.0-windows` is a real, first-class TFM" and "NuGet/MSBuild resolve it automatically". Both statements
are true. Two others are also true and were not available when the ruling was made:

1. **There is no `net9.0-linux`,** and `net9.0-macos` is a workload TFM (the macOS/AppKit bindings, as
   `net9.0-maccatalyst` is Catalyst's), not a console-library target that a `dotnet pack` of this corpus
   could produce. The TFM axis can express *Windows vs not-Windows* and nothing more — it cannot
   separate Linux from macOS, which §4 measures as 36 differing files across 20 packages. The plan's §A4
   already noticed this for the third flavor and proposed falling back to RID assets for linux and darwin;
   that hybrid uses two mechanisms where (a) uses one.
2. **TFM selection happens at compile time against the consumer's TFM, not at runtime against the host.**
   A portable app targeting plain `net9.0` and running on Windows would bind the *neutral* (non-Windows)
   assembly — the wrong one. To get the Windows flavor the app must itself target `net9.0-windows`, and a
   `net9.0` project cannot reference a `net9.0-windows` library at all (NU1201). So (b) requires every
   consumer to multi-target and build once per OS, which is the opposite of "a single set of packages that
   works for all target platforms".

**This is the one place where this design asks the user to revisit a ruling.** It is not a disagreement
about the goal — (b) and (a) target the same two-binary outcome — but the TFM mechanism cannot carry it for
three platforms, and it changes the consumer contract. Recommendation: replace N2's *mechanism* with (a),
keeping N2's *intent* (one package ID set, consumer picks nothing) intact.

### (c) Three package sets (`go.os.windows` / `go.os.linux` / `go.os.darwin`)

The plan rejected this as permanent public ID sprawl, and that judgment stands. The measurement does
improve the arithmetic — only 37 packages vary, so a hybrid would be 265 neutral IDs + 37 × 3 = **376 IDs**
rather than ~900 — but it still forces `-recurse=nuget` to rewrite references by target, still means a
user's project file names a platform, and still publishes an irreversible naming scheme. Recorded as
measured, not recommended.

### (d) One assembly, runtime OS dispatch — **rejected**

All three platforms' code compiled together under namespaced classes, selected at runtime. The smallest
counter-example is the §4.3 miniature:

```csharp
// unix.cs   → public const bool IsUnix = true;
// nonunix.cs → public const bool IsUnix = false;
```

Both are members of `goos_package`. Compiled together they are a duplicate-member error; and they are
`const`, so they cannot be converted to a runtime-selected property without breaking every downstream
`const` context. The same objection scales: `path/filepath.Separator` is a compile-time constant in both
languages, and `syscall` would need 992 + 2,186 + 1,899 names disambiguated in one assembly.

Beyond the collisions, the dispatch boundary is in the wrong place. Go's platform seam is *internal* —
`os` reaches its platform behaviour through `internal/syscall/windows` vs `internal/syscall/unix`, two
different assemblies with different surfaces (§6) — so a dispatcher at each package's *public* API would
not be sufficient; every internal cross-package call would need triplication.

**And Go itself does not do this.** Go selects files at build time via build constraints and compiles
exactly one platform; `GOOS` is a compiled-in constant, not a runtime query. Option (d) has no precedent in
the source language, contradicts the faithful-conversion rule, and is the shortcut the nothing-throwaway
principle exists to refuse.

---

## 10. Validation implications

- **The differential oracle needs a matching host.** `runCommandWithTimeout` exports `GOOS`/`GOARCH` to
  both sides, including the `go test -json` baseline; `go test` for a foreign `GOOS` builds a binary it
  cannot execute. A Linux corpus is validated on Linux (WSL is sufficient and already proven for the F2/F3
  work); macOS needs real hardware or CI. This is plan §A2.3 and it is unchanged by anything here.
- **The existing roster mostly transfers.** Of the **110** validated packages, only **6** are
  platform-varying (`crypto/rand`, `internal/sysinfo`, `mime`, `os/exec/internal/fdtest`, `path/filepath`,
  `time`); **15** are fully closure-clean. The converted C# *test sources* for the other 104 are
  platform-neutral and transfer unchanged.
- **Verdicts, however, must be re-earned per platform.** A package with identical test source still runs
  against a different closure (a different `os`, a different `runtime`), so a Windows pass is not evidence
  of a Linux pass. The honest accounting is one roster *per platform*, sharing sources.
- **Proof pages gain a platform dimension.** `docs/validation/<version>/<pkg>.<os>.md`, as §A4 already
  proposes. The measurement supports it for a reason worth stating: a proof page describes the binary being
  shipped, and (a) ships three binaries per varying package.
- **Two behavioral tests are Windows-semantic by construction** (`LocalTimeZone`, `FindFirstFileData`) and
  need a platform marker in the runners' enumeration rather than deletion — plan §A2.5.

---

## 11. Recommendation

**Layout L3 + packaging (a).** One tree, one solution, one package-ID set; 37 packages carry per-GOOS
sources and RID-specific assemblies; 265 ship exactly as they do today.

Honest trade-offs:

| | L3 + (a) |
|:--|:--|
| Corpus size | +29.4 % files (1,665 → 2,154) |
| Package IDs | unchanged (~304) |
| Consumer change | **none** — same `PackageReference`, same `dotnet run` |
| Converter work | `-platforms` list, three-way emission compare, conditioned csproj blocks |
| Pack work | N build passes + asset merge in `push-nuget.ps1` |
| Truthfulness of the compile surface | exact for 300 of 302 packages; `syscall` and `log/syslog` open |
| Rebank cost | one regeneration, but every subsequent CNR compares three emissions |
| Reversibility | high — L3 is additive; a single-platform build is `-platforms` with one value |

**The two seams this design does not close**, stated plainly rather than hidden in the recommendation:

1. **`syscall`'s compile surface.** 270 names in common out of 992/2,186/1,899. Candidate resolutions, in
   the order this lane would try them: (i) let `lib/net9.0/syscall.dll` carry the *host-of-record* flavor
   and document that portable code must not reference `go.syscall` directly — which is already true of Go,
   where `syscall` is frozen and `os` is the portable API; (ii) per-GOOS package IDs for `go.syscall`
   *alone*, the one place where the sprawl is honest; (iii) a unix-intersection reference assembly. Option
   (i) is cheapest and matches Go's own guidance; it needs a user ruling because it is a public promise.
2. **`log/syslog`** exports nothing on Windows. A neutral `lib/net9.0` reference carrying the Unix surface
   would let Windows code compile against APIs that cannot run. Simplest honest answer: ship the empty
   Windows surface as the compile asset, so the package is unusable at compile time on Windows exactly as
   it is in Go.

---

## 12. Staged migration — the first increment is small and provable

Each stage lands green on its own and is gated by an instrument that already exists.

**Increment 1 — a three-target census the converter itself produces. No emission change. — ✅ LANDED
2026-08-08 (lane r47d, against `223e4ffd3`).**
`-platforms` now accepts a comma-separated **list**, and the new `-platform-census <dir>` runs the pipeline
once per target into its own staging root, classifies every emitted artifact (shared / variant / partial /
exclusive) and writes `<dir>/platform-manifest.json`. Nothing is written into `src/core`: `-go2cspath` is
read as the SEED each staging root is copied from and never as an output, and no emitter was touched — CNR
is byte-identical across all 574 behavioral packages. A `-platforms` list *without* `-platform-census` is
rejected rather than silently converting the first target; multi-platform **emission** is increment 3.

Three mechanisms carry this document's own reasoning into the code:

- the classification compares **emissions**, never Go file sets (§4.2 is the reason);
- every staging root is **seeded** — `core` + `version.props` + `docs/validation`, mirroring `src/` — and is
  wiped and re-seeded per run, so CLAUDE.md's reconvert ritual and r41's never-convert-twice-into-one-root
  rule are mechanical rather than remembered. The manifest carries the **marker gate** per target (the
  hand-owned files the seed held, and any the run emitted as a plain `.cs` — which must be zero), so a
  seeding that did not take cannot be mistaken for a platform finding;
- emitted-vs-seeded is decided by a sentinel **modification time**, not by a content diff: the control
  target's emission is *supposed* to reproduce the seed byte for byte, so a content discriminator would
  report the control as having emitted nothing.

*Proof — the manifest reproduces this document's numbers.* Two censuses, five conversions (859 s + 522 s),
`-platforms windows/amd64,linux/amd64,darwin/amd64` and `-platforms darwin/amd64,darwin/arm64`:

| Measured here | § | Manifest | |
|:--|:--|:--|:--|
| Union of emitted `.cs` names 1,928 | §4 | 1,928 | ✔ |
| Byte-identical on all three 1,489 | §4 | 1,489 | ✔ |
| Same name, different content 69 | §4 | 69 | ✔ |
| Emitted by exactly two 88 | §4 | 88 | ✔ |
| Platform-exclusive 282 | §4 | 282 | ✔ |
| Packages with any delta 37 | §4 | 37 | ✔ |
| The 69 split 38 source / 27 `package_info.cs` / 4 `package_init.cs` | §4.3 | 38 / 27 / 4 | ✔ |
| Windows emitted 1,665 · Linux emitted 1,734 | §4 | 1,665 · 1,734 | ✔ |
| W↔L 1,563 both / 1,498 identical / 65 differ / 102 W-only / 171 L-only / 34 packages | §4 | all six | ✔ |
| W↔D 1,566 / 1,499 / 67 / 35 packages | §4 | all four | ✔ |
| L↔D 1,633 / 1,597 / 36 / 20 packages | §4 | all four | ✔ |
| L3 pricing: flat 1,489 + per-GOOS 665 = 2,154, against 1,665 (+29.4 %) | §8 | all five | ✔ |
| Packages queued 304 / 302 / 303 / 301, **zero** failures in every run | §2 | all four | ✔ |
| Arch axis 1,701 both / 1,687 identical / 14 differ / 32 + 31 exclusive / 16 packages | §5 | all six | ✔ |
| `.csproj` rewritten: Windows 0, Linux 21 | §4 | 0, 21 | ✔ |
| `.csproj` rewritten: "the macOS run 26" | §4 | **22** at `darwin/amd64`; 26 is `darwin/arm64` | corrected |
| "22 packages have a different direct import set" | §4 | **24** | corrected |

The two corrections are recorded in §4 with their evidence; both are `.csproj` counts and neither touches a
`.cs` number or any conclusion. One further disagreement is worth naming: §7 counts **41** files carrying a
line-anchored `[module: GoManualConversion]`, while the census — which asks the question with the
converter's *own* predicate, the one that actually decides whether a file's emission is diverted — counts
**40**. The odd file is `runtime/runtime2_impl.cs`, whose marker the predicate cannot see because an earlier
`//` comment line contains `*g/*p`, and `/*` opens a phantom block comment the file never closes. It is
inert today (an `*_impl.cs` companion has no Go counterpart, so that path is never probed) but it is the
exact shape that would silently clobber a whole-file hand-own; filed as its own fix rather than smuggled
into a census increment.

Cost, for the record: two new converter files and three edited lines of flag plumbing — no emitter, no
corpus file, no golden.

**Increment 2 — L3 for one package: `internal/goos`.**
Four files, no dependents' *surface* change (§6: 20 exported names, identical on all three), and it is the
clearest possible illustration. `src/core/internal/goos/{goos.cs, package_info.cs}` stay flat;
`{windows,linux,darwin}/` gain `zgoos_*.cs` and `unix.cs`/`nonunix.cs`. The conditioned `<Compile Include>`
line is exercised for real.
*Proof:* `dotnet build` of the package at `-p:GoTargetOS=windows` produces IL identical to today's; at
`linux` it compiles and `GOOS` reads `"linux"`.

**Increment 3 — L3 for the 37 measured packages, plus the conditioned `ProjectReference` blocks (22 of
them), plus per-GOOS `package_info`/`package_init`.**
*Proof:* `dotnet build src/go2cs-stdlib.slnx -p:GoTargetOS=windows` is byte-for-byte the corpus we ship
today (this is the real gate — the Windows lane must not move); `-p:GoTargetOS=linux` compiles, with its
error buckets reported. **This is the first point at which "does a Linux corpus compile?" can even be
asked** — see §13.

**Increment 4 — packaging (a) for the RID pair `win-x64` / `linux-x64` only.**
`push-nuget.ps1` grows the second build pass and the asset merge. Validate by restoring `go.fmt` into a
scratch console app on both platforms and running it.

**Increment 5 — macOS, then the second architecture.**
§4 measures macOS as 20 packages' distance from Linux, and §5 measures the arch axis at 16 packages; both
are additions *by value* to a mechanism already proven, exactly as the big-three ruling intends.

---

## 13. What this design does **not** answer

Recorded so nobody mistakes a measurement for a guarantee.

1. **IL identity is assumed, not measured.** §4 measures the *converter's* output. Two packages with
   byte-identical C# could still produce different IL if `go2cs-gen` generates differently against a
   Windows- vs Linux-flavored dependency (the generators read `[assembly: GoImplement]` records from
   referenced `package_info` assemblies, and 27 `package_info.cs` files vary). The "265 packages ship one
   assembly" claim in §9(a) therefore needs a **build-level check**: compile the same source against both
   flavors and compare IL. This should be the first verification inside Increment 3, and if it fails the
   only consequence is that more packages get RID-specific assets — the design shape does not change.
2. **Whether the Linux corpus compiles is still unmeasured — and cannot be measured today.** A seeded
   reconvert produces a *union* tree (the Linux emission laid over a seeded Windows tree), and the emitted
   csproj globs `*.cs` flat, so building it would compile both platforms' files and fail on duplicate
   definitions. Pruning it correctly *is* the layout question this document answers. That is why the
   compile number lives in Increment 3 and not here: **L3 is a prerequisite for asking it, not a
   consequence of knowing it.** Plan Arc 4's "one afternoon" estimate assumed option 1's separate tree; L3
   reaches the same number without one.
3. **`syscall`'s public contract** (§11 seam 1) needs a user ruling, not a measurement.
4. **cgo.** Every census here ran `CGO_ENABLED=0`. Real Go on Linux resolves `os/user` and `net` through
   cgo by default; the pure-Go paths measured here are the same class of decision as the standing
   `-tags purego` ruling, and should be confirmed as such rather than assumed.
5. **The Windows-semantic behavioral tests and the runners' platform gating** are scoped in plan §A2.5/§A5
   and are not re-derived here.

---

## Appendix — reproducing the measurements

**Censuses B and C are now one command each** (increment 1). The converter does the seeding, runs the
targets sequentially into isolated staging roots, and writes the manifest every number in §4, §5 and §8
above is read from:

```powershell
go2cs -stdlib -comments -platforms windows/amd64,linux/amd64,darwin/amd64 `
      -go2cspath <repo>\src -platform-census <scratch>\three-way   # census B (§4)

go2cs -stdlib -comments -platforms darwin/amd64,darwin/arm64 `
      -go2cspath <repo>\src -platform-census <scratch>\arch        # census C (§5)
```

`-go2cspath` is the SEED and is never written to; the manifest lands at
`<scratch>\<name>\platform-manifest.json`. The hand-rolled form the original measurements used is kept
below, since census A still needs it:

```powershell
# Four seeded conversions (sequential; never two converter processes at once).
# Seed each root with src/core + src/version.props + docs/validation, mirroring src/.
go2cs -stdlib -comments -platforms windows/amd64 -go2cspath <root-w>/src
go2cs -stdlib -comments -platforms linux/amd64   -go2cspath <root-l>/src
go2cs -stdlib -comments -platforms darwin/amd64  -go2cspath <root-d>/src
go2cs -stdlib -comments -platforms darwin/arm64  -go2cspath <root-a>/src
```

```bash
# Census A — per-GOOS source file sets.
GOOS=$OS GOARCH=amd64 CGO_ENABLED=0 GO111MODULE=off \
  go list -e -tags purego,math_big_pure_go \
  -f '{{.ImportPath}}|{{join .GoFiles ","}}|{{join .SFiles ","}}|{{join .CgoFiles ","}}' std
```

Censuses B/C compare the emitted `.cs` of two roots, discriminating emitted from seeded files by
modification time — that comparison is no longer a throwaway probe but `src/go2cs/platformCensus.go` +
`platformManifest.go`, which stamp the seed with a sentinel time so "emitted" is exact rather than
inferred. Census D enumerates `types.Package.Scope()` exported names via `go/packages` under each `GOOS`;
its probe source was throwaway and was not committed. The commands above are sufficient to regenerate every
number in this document.
