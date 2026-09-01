# DESIGN — relocating the forced-init import hooks out of mainline code

**Status:** step 2 of §5 LANDED 2026-09-01 (the compile-ordering precondition, §3.2); the hook
relocation itself is still spec, and its **sizing census is in** (§4.1 — size, two hand-own blockers,
and the `initᴛᴛtests`-retirement question answered NO). Owner-sourced scout (2026-08-31), accepted by
coordinator ruling the same day; the implementation is a corpus-wide emission change and lands in the
**rebank wave**, whose full-roster sweep is its gate.

**The goal is readability.** Every file that imports a package currently carries a `[GoInit]`
machinery block near the top of its class body. The owner asked whether those can move to
`package_info.cs`'s constrained machinery area, so the visible converted code loses a block per
importing file. Answer: **yes, and it makes the ordering contract STRONGER — but only with an
enabling change the emission does not have today.** A naive relocation reintroduces the exact bug
the hooks were added for.

Everything below was measured on this tree. Where something was not established, it says so.

## 1. Where the hooks come from, and what the ordering contract actually is

`writeImportInit` (`src/go2cs/visitImportSpec.go:372`) appends to `v.importInits`, which
`visitFile.go:111` splices into the importing FILE's class body at `ImportInitMarker`. Exactly ONE
hook per (assembly, imported package), deduped through `packageImportForces`, with a fence so a
hand-owned file's `.cs.auto` shows the hook without claiming the package-wide slot.

The contract, from the converter's own comment and confirmed by reading the emission:

* **Within a file:** the hook is emitted at the TOP of the class body, so it precedes that file's own
  `init` functions. This is the guarantee that is load-bearing today.
* **Across files:** there is **no converter-stated guarantee.** Roslyn orders module initializers by
  compilation file order, then declaration order — and the converted csproj compiles with
  `<Compile Include="*.cs" />`, a GLOB. Cross-file order is therefore filesystem/alphabetical,
  curated by nobody.

## 2. Is `package_info.cs` a legal home?

**Discovery: yes, and it is not a question of walkers.** `[GoInit]` is aliased in
`csproj-template.xml` to `System.Runtime.CompilerServices.ModuleInitializerAttribute` — a
COMPILE-TIME Roslyn feature. Roslyn emits the calls into the module `.cctor` regardless of which file
declares the method; there is no runtime scan whose reach could differ by file. Relocation cannot
break discovery.

**Ordering: no, not as a naive move.** This is the finding.

**One-package proof, on `log/slog` — the package that motivated the hooks in the first place.** Both
of these live in `logger.cs`, the hook first, which is exactly why it works today:

* `log/slog/logger.cs:23` — the `[GoInit]` import hook that forces `log`'s init.
* `log/slog/logger.cs:69` — slog's OWN `init`, which stores a default logger built from
  `loginternal.DefaultOutput`, i.e. it READS what `log`'s init installs.

But `"logger.cs"` sorts BEFORE `"package_info.cs"`. Move the hook and slog's own `init` runs FIRST,
`DefaultOutput` is still nil, and we are back to the nil-dereference that killed slog's test host —
the precise regression `tests/Behavioral/NamedImportInitOrder` exists to catch. **The move would
reintroduce the 2026-08-26 bug in the package that motivated the 2026-08-26 fix.** That behavioral
test is the positive control for this work, and on this proof it is a genuine one rather than a
formality.

**Structural note — WRONG, and corrected on measurement 2026-09-01.** This paragraph originally read
that `package_info.cs` "contains ONLY assembly-level attributes", so hosting a `[GoInit]` method
would mean the file must *gain* a `static partial class <pkg>_package` body — a shape change to a
file that is ALSO an INPUT to dependent packages' transpiles, with a cross-package read path to
clear before anything is cut. **It does not have to gain one: 359 of the 360 committed
`package_info.cs` already declare that class, with a populated body** (the `<TypeAccessibility>`
block lives inside it — e.g. `crypto/internal/boring/bcache/package_info.cs:58`). The single
exception is `unsafe`, which is hand-owned and skip-listed from conversion. The hook is therefore one
more member of a class body that already exists, and there is no shape change and no read-path risk
to clear.

**And the mechanism has a banked precedent.** `package_test_info.cs` already hosts a forced-init
module initializer in exactly this position — `[GoInit] internal static void initᴛᴛproduction() {
builtin.initPackage(typeof(…)); }` — in `internal/weak`, `internal/concurrent` and
`internal/syscall/windows`. An info-file carrying a `[GoInit]` that forces another assembly's module
constructor is shipped code, not a new idea.

## 3. The enabling change (measured, not proposed)

The move is safe if and only if `package_info.cs` is compiled FIRST. That is expressible, and the
mechanism was proved on a scratch project rather than trusted as MSBuild folklore — two files, one
`[ModuleInitializer]` each:

| `<Compile>` shape | initializer order |
|---|---|
| default `<Compile Include="*.cs" />` | `aaa` then `zzz` (alphabetical) |
| `<Compile Include="zzz.cs" />` + glob with `Exclude="zzz.cs"` | `zzz` then `aaa` (**reordered**) |

So emitting `<Compile Include="package_info.cs" />` ahead of an excluding glob gives a deterministic
first position. With that, the move does not merely preserve the contract — it **UPGRADES** it, from
"hooks precede their own file's inits, and cross-file order is nobody's" to "ALL import hooks precede
ALL of the package's inits, deterministically, corpus-wide."

### 3.1 The file is NOT always at the package root — measured, and it complicates the item

The scout wrote the step above as though `package_info.cs` sat at one known path in every package.
**It does not.** Censused 2026-09-01 over the 305 production csprojs under `src/core`: **35 packages
have no `package_info.cs` at their root**, and every one of them is an L3 per-GOOS package that keeps
it in the platform folder instead — `os/windows/package_info.cs`, `net/linux/package_info.cs`,
`syscall/darwin/package_info.cs`, and so on for `runtime`, `time`, `mime`, `archive/tar`,
`internal/poll`, `path/filepath`, `os/exec`, `crypto/x509`, and the rest.

This matters because an explicit `<Compile Include>` of a path that does not exist is not a no-op:
MSBuild adds the item and the compiler then fails on a missing source file. So the emitted item has
to follow the package's layout:

* **flat package** — `<Compile Include="package_info.cs" />`.
* **L3 package** — the file is at `$(GoTargetOS)/package_info.cs`, so the item must be the
  conditioned form, alongside the conditioned `<Compile Include="$(GoTargetOS)/*.cs" />` that
  `platformLayout.go` already emits for exactly these packages.
* **platform-EXCLUSIVE package** — `crypto/x509/internal/macos` carries ONLY
  `darwin/package_info.cs`. Under the default windows target that project legitimately compiles
  nothing, so an item naming `$(GoTargetOS)/package_info.cs` unconditionally would name a file that
  does not exist and turn a package that currently builds empty into a hard error.

### 3.2 Resolved: ONE shape serves every layout, because `Exists()` absorbs the difference

§3.1 concluded that step 2 was "a per-layout emission change rather than one template line". That was
too pessimistic, and the implementation (2026-09-01) settles it: the item has to be layout-AWARE, but
it does not have to be layout-SWITCHED. Both forms are emitted unconditionally and each is guarded by
`Exists()`, so exactly the right one matches and the other is silently inert:

```xml
<Compile Include="package_info.cs" Condition="Exists('package_info.cs')" />
<Compile Include="$(GoTargetOS)/package_info.cs" Condition="Exists('$(GoTargetOS)/package_info.cs')" />
<Compile Include="*.cs" Exclude="package_info.cs" />
<Compile Include="$(GoTargetOS)/*.cs" Exclude="$(GoTargetOS)/package_info.cs" />
```

The per-GOOS pair is still added only to L3 packages (by `platformLayout.go`), so a flat package's
project file never mentions `$(GoTargetOS)` at all. Verified on real corpus packages rather than a
scratch project: `bytes` compiles `package_info.cs` first, `os` compiles `windows/package_info.cs`
first, and darwin-only `crypto/x509/internal/macos` built for windows matches neither guard and
yields an EMPTY compile-item list — no CS2001, exactly the build-nothing behavior it had before.

Two mechanics worth keeping, both measured and both cheap to rediscover the hard way:

* The `Exclude` on each glob is **load-bearing, not tidiness.** Without it the file is included
  twice and the build emits `warning CS2002: Source file specified multiple times` on every project.
  Because the root glob's text is also `platformLayout.go`'s insertion anchor, changing it means
  moving that anchor in the same commit.
* `--` is illegal inside an XML comment, so the rationale comment cannot use it. The csproj metadata
  tests catch this immediately, which is how it was found.

## 4. `package_init.cs` and the static-constructor route — asked, and now CLOSED

The `initTests` hook is NOT in `package_info.cs`. It lives in **`package_init.cs`** (5 committed
sites), and that file uses a materially different mechanism: a **static constructor with an EXPLICIT
ordered call sequence**, not a set of independently-ordered module initializers. That made
`package_init.cs` look like the more interesting home, because an explicit sequence moots the
file-order question entirely.

The scout declined to recommend it on the grounds that the semantics might not be equivalent — a
static constructor is TYPE-scoped (runs on first access to that type) while `[ModuleInitializer]` is
MODULE-scoped (runs at module load) — and said plainly that this had not been measured and might well
be the better answer.

**Measured 2026-09-01. It is not the better answer; it is strictly weaker.** Two assemblies: a
library with a `PkgClass` carrying a static ctor, a second `OtherType` in the same assembly, and a
`[ModuleInitializer]`; an app that reads `OtherType.Y` and only later touches `PkgClass`:

```
  MODULE INITIALIZER          <- before Main's first statement, at module load
before touching OtherType
  OtherType.Y = 2             <- the package has now been USED, and the cctor has NOT run
after; now touching PkgClass
  CCTOR of PkgClass           <- only on first access to that specific type
```

An importing package that uses a package's structs or funcs but never touches `<pkg>_package` would
therefore never fire the cctor, so the forced init would not happen at all — which is precisely the
class of failure the hooks exist to prevent, and precisely why `builtin.initPackage` exists.

**Conclusion: `package_info.cs` with `[GoInit]`/`ModuleInitializer` is the home. The cctor route is
closed.** This does not impugn `package_init.cs`'s existing use: sequencing inits WITHIN a package's
own class is a different job, and an explicit ordered sequence is right for it. What a cctor cannot
be is the TRIGGER for a cross-package forced init.

**ANSWERED 2026-09-01, and the answer is NO — the move cannot retire the `-tests`-closure
`initᴛᴛtests` drift class.** This paragraph previously read "Still unmeasured", and the wave plan's
Stage-A2 bullet inherited the same expectation ("may retire sweep-dirt class 2's `initᴛᴛtests`
shape — measure the retirement, don't assume it"). The two constructs are unrelated:

* An import hook is `[GoInit]`/`ModuleInitializer` forcing **another assembly's** module constructor.
* `initᴛᴛtests` is a **`static partial void` declared inside `package_init.cs`'s static
  constructor** (`Symbols.PackageTestInitHookMethod`), the hook by which a `-tests` run splices the
  internal test variant's relocated **variable** initializers into the production class's Go
  `InitOrder` sequence. Four committed sites: `net/http`, `math/big`, `internal/trace`,
  `internal/syscall/windows/windows`.

Different file, different construct, different trigger. Three controls over the committed corpus
confirm it rather than leaving it as a reading: **zero** `package_init.cs` carries an import hook;
the hook-carrying file set and the `initᴛᴛtests` file set are **disjoint**; and **zero**
`package_info.cs` carries an import hook today — which also makes the relocation target a clean
baseline for the emission A/B that step 3 still owes.

### 4.1 Sizing census (2026-09-01) — the size, and two blockers the spec did not carry

Measured over the committed corpus at `5b9038d8c` with `git grep` (a bare `rg` honors
`src/core/.gitignore` and under-counts), keyed on `builtin.initPackage(typeof(` — the hook body,
which is unique to it. Positive control: `log/slog/logger.cs` reads 2 hooks at lines 23 and 29,
the exact site §2's proof names.

| set | hooks | files |
|---|---|---|
| corpus-wide | 3,194 | 959 |
| **production emission** (what a `-stdlib` regen sees) | **2,125** | **684** |
| test variants | 1,069 | 275 |

314 production packages carry hooks; **median 5, max 44** (`net/http`). So the readability win is 684
production files each shedding 1–44 machinery blocks, consolidating into 314 `package_info.cs`.
A further 111 `[GoInit] internal static void init…` in the corpus are the packages' own converted
`init` functions and do not move.

**Two hand-owned files carry an import hook by hand, and both must be edited in the same commit as
the relocation.** Neither is visible from the converter source; both fell out of the census.

* `crypto/internal/boring/bcache/cache.cs` — `[module: go.GoManualConversion]`, carrying
  `initᴛᴛimportꓸsyncꓸatomic`. Its `.cs.auto` sibling carries the **identical name**, which proves
  the converter would emit it and that §1's hand-own fence is the only reason it does not claim the
  package slot today. Relocate, and that emission lands in `bcache_package` through
  `package_info.cs` while the hand-own keeps its copy: **CS0111 duplicate member**, a hard error in
  a package with a banked row.
* `runtime/metrics/sample.cs` — also hand-owned, carrying `initᴛᴛblankImportꓸruntime`. That name
  shape **no longer exists in the converter** (`blankImport` is gone from `src/go2cs`; the hand-own
  froze it before the 2026-08-26 widening). So it is NOT a collision and would fail silently: two
  forced inits of `runtime`, one of them dead-named machinery. It is the only claim on that slot in
  the package.

**Still owed by step 3:** the emission delta itself — two seeded conversions, pre- and post-change,
diffed against **each other** rather than against the committed tree (the committed tree is a moving
baseline; see CLAUDE.md's blast-radius rule).

## 5. Blast radius, sequence, guard

Corpus-wide on **two** axes, not one: the emission AND the csproj `<Compile>` shape. So it lands with
a rebank-style regen.

Recommended sequence:

1. ~~Decide `package_info.cs` vs `package_init.cs`~~ — **settled in §4: `package_info.cs`.**
2. ~~Land the **compile-ordering change first**, as its own gated step~~ — **LANDED 2026-09-01**
   (§3.2). It carries no behavioral change on its own: `package_info.cs` contributes no module
   initializers today, so moving it to first position reorders nothing that runs. That is exactly
   what makes it a safe, separately-gated precondition rather than part of the risky step.
3. Then relocate the hooks. **This is the step that has behavioral effect**, and it is where
   `tests/Behavioral/NamedImportInitOrder` stops being a formality — per §2 a relocation without
   step 2 reintroduces log/slog's nil-deref. **It carries the two hand-own edits of §4.1 in the same
   commit** (`crypto/internal/boring/bcache/cache.cs`, a hard CS0111; `runtime/metrics/sample.cs`, a
   silent duplicate), and it OPENS with the two-seeded-emissions blast-radius measurement §4.1 still
   lists as owed.

`tests/Behavioral/NamedImportInitOrder` must stay green throughout; per §2 it is a real positive
control, not a formality. A `-stdlib` reconvert plus CNR covers the emission change; the wave's
full-roster sweep is the bank gate.
