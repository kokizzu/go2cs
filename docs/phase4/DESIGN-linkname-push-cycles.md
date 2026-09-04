# W1 — the linkname alias that closes a project cycle

**Status:** IMPLEMENTED — see the dated blocks below. Both halves landed 2026-08-30.
**Lane:** runtime walls campaign, i7-5820K coordinator machine
**Date:** 2026-08-30
**Spec:** [`docs/phase4/CENSUS-runtime-first-contact.md`](docs/phase4/CENSUS-runtime-first-contact.md) §W1
**Tree read:** `origin/master` @ `a2e726796`; toolchain go1.23.12 / .NET SDK 10.0.400

---

## 2026-08-30 — G2 and W1-M landed (branch `claude/local-w1-mechanism`)

Two commits, in the order §5 rules.

* **G2, alone and first.** The standing corpus cycle assertion is check 5 of
  `src/tests/Behavioral/check-solution-integrity.ps1` — CNR's own preflight. It DFSes the `src/core`
  `.csproj` graph once per `$(GoTargetOS)` and requires 0 cycles; measured 0 across 307 projects on
  **windows, linux AND darwin** at that HEAD (darwin joined because the graph read is free and the
  per-GOOS `<ItemGroup>` blocks make it a genuinely different graph). Positive control is a
  parameter, not a procedure: `-InjectReference 'runtime=internal/syscall/windows'` prints §1.3's six
  cycles verbatim, in this document's order, and exits 1. The same six also come out when the
  assertion is fed the HEAD converter's REAL `-tests` emission of `runtime.csproj` — the gate and the
  defect meet end to end.
* **W1-M, M1 with M2 as its fallback.** `linknamePullWouldCycle` now answers from three oracles in
  cost order: the convert-set graph when a batch driver built one; the current package's own
  transitive import closure (if this package already reaches the target, the target cannot reach back
  — free, and it is the arm the `LinknameVarPull` behavioral test rides); otherwise a memoized
  `packages.Load(NeedName|NeedImports|NeedDeps)` of the pull TARGET, walked for the current package.
  An unanswerable question refuses the pull and says so on stderr.
* **Measured (G5).** `-tests -test-action convert`, HEAD converter vs fixed, byte-compared:
  `math/bits` **unchanged** across all 24 emitted files (the 25th is the gitignored
  `go2cs_test_manifest.json`, whose `converterRevision` records the converter's own exe hash and
  therefore must differ); `runtime` differs in **exactly two** files, `runtime.csproj` (the windows
  group returns to `<ItemGroup Condition="'$(GoTargetOS)'=='windows'" />`) and
  `windows/os_windows.cs` (`internal static bool canUseLongPaths;`), both now **byte-identical to the
  committed `-stdlib` corpus**. The fifth closure family is gone.
* **Not landed:** W1-S (§3.2's S2 inversion) and the §4.2 populate half, which stay design only and
  keep the whole §5 gate list. G3/G6/G7/G8/G9 belong to that half and were not run.

---

## 2026-08-30 — W1-S landed (branch `claude/local-w1-semantics`)

§3.2's S2 and §4.2's populate half, both as written. The emission, measured against the committed
corpus by a seeded full reconvert:

```
internal/syscall/windows/windows/syscall_windows.cs
-  public static bool CanUseLongPaths;
+  public static bool CanUseLongPaths { get => go.runtime_package.canUseLongPaths; set => go.runtime_package.canUseLongPaths = value; }

runtime/windows/os_windows.cs
-  internal static bool canUseLongPaths;
+  public static bool canUseLongPaths;
```

**Both `.csproj` files are byte-identical** — the "zero new project references" claim of §3.2,
confirmed mechanically rather than argued: `isw → runtime` was already there.

* **The registry** is `linknameVarAliasTargets` (`linknameOperations.go`), one row, with
  `linknameVarAliasStorage` DERIVED from it for `packageVarAccess`'s publicize arm — the
  `linknamePushSources` pattern, so the storage side and the forwarding side cannot drift.
* **Go's authorization is required.** `varLinknameAliasForward` fails closed to a plain field when the
  target does not carry its one-arg handle, so a row that outlives Go's directive forwards nothing
  rather than inventing an alias.
* **The inherited guard** is `varLinknamePull`'s `!isAddressedGlobal` check, applied on the target
  side: an address-taken target keeps its heap box, because a forwarding property has no `Ꮡ` to name.
* **One asymmetry accepted, deliberately.** The storage side publicizes on the registry row alone,
  because while converting the storage package the target's syntax is exactly as invisible as the
  reverse — the same constraint that makes the registry necessary. A stale row therefore widens an
  inert member; that is caught where it is actually wrong, by the GOROOT-derived registry guard.
* **§4.1's severity correction needs a further correction, and it goes the same way.** The design
  expected the divergence to be observable ("any test that observes the path a syscall received will
  see a different string"). Through `os`, from Go code, it is **not observable at all**:
  `addExtendedPrefix` normalizes through `GetFullPathName` before prepending `\\?\`, so `.`, `..`,
  doubled separators, forward slashes and a trailing dot all resolve identically either way (seven
  probes, zero differing). That is why G8 pins the round TRIP and the outcome-not-intent rule rather
  than the flag's value — and it lowers the chip's severity one more notch.

---

## 0. Headline

> **The census named the right wall and mis-named its cause, and the correction makes the fix
> tractable.** W1 is not a conversion-ORDER problem. It is a **disarmed guard**: the converter
> already contains a cycle check for exactly this emission — `linknamePullWouldCycle` — and that
> check reads a dependency graph which is **`nil` on the `-tests` path**. Under `-stdlib` the guard
> fires and the pull is suppressed; under `-tests` the guard silently answers "no cycle" and the
> pull is emitted with its project reference. One variable, two answers, no diagnostic.
>
> The correction matters because the census's ordering theory implies a fix that is **provably
> impossible**. Go's own import graph contains `internal/syscall/windows → syscall → runtime`. So
> a C# project reference `runtime → internal/syscall/windows` is a cycle **no matter when it is
> emitted, by which mode, in which order**. Re-ordering the `-stdlib` queue cannot help. Neither
> can pruning back-edges: one of the six cycles runs entirely through real Go imports.
>
> What is left is the direction of the storage. A Go `//go:linkname` var alias makes two
> declarations **one variable**; C# must put that variable in exactly one assembly, and the
> converter always picks the one named on the right of the directive. Picking the **other** one —
> `runtime`, the package that is lower in the dependency order and where Go's own write already
> lives — costs **zero new project references**, because `internal/syscall/windows` already
> references `runtime`. Verified: 0 cycles.
>
> And the twin finding needs its severity corrected too. `canUseLongPaths` is unwired, but golib
> already knows, already says so in a signed comment, and already supplies the half that actually
> matters (the PEB `IsLongPathAwareProcess` bit). Long paths are not broken; the `\\?\` spelling is
> simply always used. Wiring the flag is a **two-part** change — forward *and* populate — and the
> populate half is what the census does not mention.

---

## 1. What actually happens

### 1.1 The emission

`go2cs -tests` rewrites two production files. Both come from one Go directive.

`C:\Users\<user>\sdk\go1.23.12\src\runtime\os_windows.go:447`
```go
//go:linkname canUseLongPaths internal/syscall/windows.CanUseLongPaths
var canUseLongPaths bool
```

`…\src\internal\syscall\windows\syscall_windows.go:16`
```go
//go:linkname CanUseLongPaths
var CanUseLongPaths bool
```

The first is a **two-argument** directive: `varLinknamePull` (`src/go2cs/linknameOperations.go:417`)
matches it and wants to emit runtime's field as a **forwarding property** into
`internal/syscall/windows`, queueing that package for a `ProjectReference`
(`src/go2cs/visitValueSpec.go:220-229`). The second is the one-argument **handle** — Go's
authorization for the alias — which is why the target is already emitted `public`.

Under `-stdlib` the property is *not* emitted and the field stays plain
(`src/core/runtime/windows/os_windows.cs:442-443`). Under `-tests` it is, and
`src/core/runtime/runtime.csproj`'s empty windows group

```xml
187	  <ItemGroup Condition="'$(GoTargetOS)'=='windows'" />
```

gains the reference that closes the loop.

### 1.2 The root, by code reading

```go
// src/go2cs/linknameOperations.go:37
func linknamePullWouldCycle(targetPath string) bool {
	if targetPath == currentPackagePath {
		return true
	}

	if conversionGraph == nil {
		return false          // ← W1 lives here
	}

	return conversionGraph.DependsOn(targetPath, currentPackagePath)
}
```

`conversionGraph` is assigned in exactly two places — `stdLibConverter.go:87` and
`moduleConverter.go:115`. A `-tests` run reaches neither: `main.go:628` calls `processConversion`
directly, the single-package driver. So during the `-tests` production pass
`currentPackagePath == "runtime"`, `conversionGraph == nil`, the function returns `false`, and the
pull is emitted. Under `-stdlib` the same call evaluates
`DependsOn("internal/syscall/windows", "runtime")`, which is **true**, and the pull keeps its plain
field form.

The variable's own doc comment states the assumption that fails:

> *"nil for a single-package or `-tests` conversion, **where no cross-package cycle can arise from
> the one package under conversion**"*

That is false for a linkname pull, and only for a linkname pull. Every *other* cross-package
reference a single package emits comes from an `import`, and Go's import graph is acyclic by
construction. A linkname edge is the one reference the converter emits that Go's own graph does not
contain — the whole reason `linknamePullWouldCycle` was written. The nil-graph shortcut assumed the
danger came from the convert-SET; it comes from the DIRECTIVE.

**This is not the ordering theory.** The census wrote that the push "fails to resolve during
`-stdlib` — runtime converts before isw — but resolves in the `-tests` closure". Nothing in
`varLinknamePull` consults a closure, a convert-set membership, or a conversion order; it reads the
comment text and asks the cycle guard. `collectSiblingTestClosure` (`testConversion.go:1163`) does
widen what the `-tests` run loads, but it is not what changes this emission. The single variable is
`conversionGraph`.

### 1.3 The cycles, independently reproduced

I rebuilt the production project graph from all 306 non-test `.csproj` files under `src/core`
(unconditional groups plus the `'$(GoTargetOS)'=='windows'` groups; darwin/linux groups stripped)
and ran a DFS.

| Graph | Cycles |
|---|---|
| At HEAD | **0** |
| HEAD + `runtime → internal/syscall/windows` | **6** |

```
errors -> internal/reflectlite -> runtime -> internal/syscall/windows -> errors
runtime -> internal/syscall/windows -> runtime
runtime -> internal/syscall/windows -> sync -> runtime
errors -> internal/reflectlite -> runtime -> internal/syscall/windows -> syscall -> errors
errors -> internal/reflectlite -> runtime -> internal/syscall/windows -> syscall -> internal/oserror -> errors
runtime -> internal/syscall/windows -> syscall -> runtime
```

Exactly the census's set, including the sixth it elided. Attribution is exact: the single added edge
takes the graph from 0 to 6.

### 1.4 The four back-edges — and the one that kills option (c)

`internal/syscall/windows.csproj:150-159` references `errors`, `golib`, `sysdll`, **`runtime`**,
`sync`, `syscall`, `unsafe` — all unconditional (the package is Windows-only, so its whole list is
de facto windows-only; unlike `syscall.csproj` it has no per-GOOS reference groups at all).

Of the edges that participate in a cycle:

| Edge | Origin | Incidental? |
|---|---|---|
| `isw → runtime` | **converter-introduced** — the `GetSystemDirectory` push forwarder (`internal/syscall/windows/windows/security_windows.cs:122` calls `go.runtime_package.windows_GetSystemDirectory()`), registry row `linknameOperations.go:262` | Yes — Go's `internal/syscall/windows` imports `sync, syscall, unsafe, errors, sysdll`, **never `runtime`** |
| `isw → sync` | real Go import (`syscall_windows.go:7-11`) | No |
| `isw → syscall` | real Go import (every file) | No |
| `isw → errors` | real Go import (`version_windows.go:7-12`) | No |
| `syscall → runtime`, `sync → runtime` | real Go imports | No |

So **removing the converter-introduced `isw → runtime` edge still leaves
`runtime → isw → syscall → runtime`**, a chain of nothing but real Go imports. Option (c) — "break
the back-edges instead" — is dead: there is no set of incidental edges whose removal makes
`runtime → isw` acyclic. The same argument kills option (a): no conversion order can make a
reference cycle acyclic, because a project reference graph is a static property of the emitted
files, not of the order they were written in.

**This is the design's load-bearing fact.** Two of the four candidate directions the brief lists are
eliminated by one DFS, and what remains is a question about *which package owns the storage*.

---

## 2. The mechanism, stated generally

A Go `//go:linkname` **var** alias is a link-time identity: `runtime.canUseLongPaths` and
`internal/syscall/windows.CanUseLongPaths` are the same word of memory, and Go needs no import in
either direction to arrange it. C# has no link-time identity. One assembly must hold the field; the
other must reach it through a member reference, which is a **compile-time** edge and therefore must
be acyclic.

So for every aliased var pair `(A.x ↔ B.X)` the converter must answer one question:

> **Which side holds the storage?**

The answer is forced by the project graph: **storage goes in whichever package the other one already
depends on.** If `B` depends on `A`, storage lives in `A` and `B.X` forwards. If `A` depends on `B`,
storage lives in `B` and `A.x` forwards. If neither depends on the other, either works. If *both*
would depend on each other — impossible, Go's imports are acyclic, but reachable once linkname edges
are in play — neither works and the storage must go somewhere both already reach.

`varLinknamePull` today answers this exactly one way: **storage always goes to the package named on
the right of the two-argument directive.** That is right whenever the pull points *down* the
dependency order and wrong whenever it points *up*. Today's guard handles the wrong case by
**giving up** — emitting two unrelated fields, which compiles and is silently incorrect. That is the
shipped `canUseLongPaths` state, and the guard's own comment says so:

> *"A downward pull (`math/bits → runtime`) is safe; the reverse
> (`runtime → internal/syscall/windows.CanUseLongPaths`) is not, and keeps its null-field form."*

**The whole of W1's semantic half is: stop giving up on the upward case, and invert instead.**

### 2.1 Blast radius — small, and measured

Corpus-wide, the emitted forwarding-property form appears **three times**:

```
math/bits/bits_errors.cs:12   overflowError    -> runtime
math/bits/bits_errors.cs:15   divideError      -> runtime
time/sleep_test.cs:42         haveHighResSleep -> runtime
```

All three point **down**. There is exactly one upward pair in the corpus today
(`canUseLongPaths`), and it is the one under discussion. Any change to `varLinknamePull` therefore
has a three-site regression surface, all of which must stay byte-identical.

---

## 3. The options, costed

W1 has two separable halves. They can land independently and should.

* **W1-M (mechanism):** the converter must not emit a cycle-forming reference under `-tests`.
* **W1-S (semantics):** the aliased pair must become one variable without a cycle.

W1-M is required whatever is decided about W1-S: it is a general trap that happens to have one
occupant today. W1-S is what the census actually wants fixed.

### 3.1 W1-M — arming the guard

| # | Option | Cost | Verdict |
|---|---|---|---|
| **M1** | **Answer the cycle question without the convert-set graph.** When `conversionGraph == nil`, resolve it from `go/packages`: `packages.Load(NeedName\|NeedImports\|NeedDeps)` on the pull TARGET and walk its transitive imports for `currentPackagePath`. Memoize per target. | ~30 lines + a cache. One `packages.Load` per distinct pull target per run — three in the whole corpus. | **RECOMMENDED** |
| M2 | **Fail closed:** with no graph, refuse any cross-package linkname reference. | 1 line. | Rejected as the primary. It would drop `math/bits`'s two legitimate downward pulls in every single-package conversion, reintroducing the null-field bug it exists to prevent — trading a rare wrong answer for a common one. **Keep it as M1's fallback** when the targeted load fails: an unanswerable cycle question must not be answered "no". |
| M3 | **Stop `-tests` rewriting the production emission.** | Large. | Rejected — it contradicts settled doctrine. `CLAUDE.md` records the production `.csproj`'s IP-4 test-artifact `<Compile Remove>` as *intended* `-tests` output, and four production `.cs` closure families (the `Δio` alias, the `global::go` root escape, the using reorder, the `initᴛᴛtests()` hook) as a **standing restore class**, not drift. The invariant is not "`-tests` writes nothing"; it is narrower and sharper — see §3.3. |

**Why M1 is cheap here specifically:** the `-tests` run *already* performs the load M1 needs.
`collectSiblingTestClosure` (`testConversion.go:1175-1183`) calls `packages.Load` with
`NeedName|NeedImports|NeedDeps` and `Tests: true` from the package directory, then walks
`pkg.Imports` transitively. `internal/syscall/windows` and its whole import closure are already in
that graph, loaded before the production conversion begins. M1 can reuse it and fall back to a
targeted load only when the target is absent — which is the general case a plain single-package
conversion needs anyway.

**Not viable:** deriving the answer from `stdlib-metadata.txt`. That file is 2,695 lines of
`package_info.cs` records and carries **no import edges** (checked). Adding them would be a second
mechanism for a fact `go/packages` already holds.

### 3.2 W1-S — where the storage goes

| # | Option | Cycles added | Verdict |
|---|---|---|---|
| S1 | **Conversion-order change** so the pull resolves under `-stdlib` too. | **6** | **Provably dead** (§1.4). A project-reference cycle is a static property of the emitted graph. The census suspected this; the DFS proves it. |
| **S2** | **Invert the alias.** Storage stays in `runtime` (where Go's own write is); `internal/syscall/windows.CanUseLongPaths` becomes the forwarding property to `go.runtime_package.canUseLongPaths`. | **0** — `isw → runtime` is already there (`internal.syscall.windows.csproj:155`), and would be reachable anyway via `isw → syscall → runtime` | **RECOMMENDED** |
| S3 | **A shared cell in `golib`.** Both sides forward to a `builtin`-owned static. | **0** — golib has **zero** ProjectReferences (a true graph sink) and all 305 converted packages reference it | Keep as the **general fallback** for a future pair where neither package dominates the other. Heavier: needs a naming scheme and a typed home for arbitrary aliased vars, and puts stdlib state in the runtime library. Not needed for this pair. |
| S4 | **Hand-own `internal/syscall/windows`'s declaration** to read golib directly (golib's own comment proposes this). | 0 | Smallest, and a real option — but it leaves `runtime.canUseLongPaths` diverged, is a hand-own where a converter rule generalizes, and **does not touch W1 at all**: the converter would still emit the cycle-forming edge on the next `-tests` run. A complement at best, never the fix. |

#### S2 in detail

Three converter-visible pieces, each with an exact in-tree precedent:

1. **A curated registry row.** Converting `internal/syscall/windows`, the converter cannot see
   `runtime`'s directive — a package is converted from its own syntax, and dependencies contribute
   types, not comments. This is the identical constraint that produced `linknamePushTargets`
   (`linknameOperations.go:161`), whose header explains it at length. So S2 needs a sibling map —
   call it `linknameVarAliasTargets` — keyed `"<declaringPkg>.<Symbol>"` and recording (a) the
   package that holds the storage, (b) the member to forward to, and (c) the judgment for why. One
   row today:

   ```go
   "internal/syscall/windows.CanUseLongPaths": {storage: "runtime.canUseLongPaths"},
   ```

2. **Publicize the storage.** `runtime.canUseLongPaths` is emitted `internal static`; a cross-
   assembly forwarder needs it `public`. `packageVarAccess` (`linknameOperations.go:80`) already
   publicizes a var on the strength of its one-arg handle; this adds the mirror arm for a var that
   is the *storage side of an inverted alias* — exactly the shape `packageFuncAccess`
   (`:109`) already carries for `linknamePushSources`, with the same reasoning quoted in its own
   comment. `bool` is publicly accessible, so `typeIsPubliclyAccessible` is satisfied and there is
   no CS0052/CS0053 exposure.

3. **Suppress the pull on the other side.** `runtime`'s two-arg directive must keep emitting a plain
   field — which is what happens today, and what M1 keeps happening once the guard is armed. So
   pieces 1–3 compose: **M1 makes runtime keep the storage; the registry makes `isw` forward to
   it.** Neither is correct alone.

**One guard S2 must inherit:** `varLinknamePull`'s existing `!v.isAddressedGlobal(ident)` check. A
forwarding property has no address, so an address-taken symbol must keep its field form
(`reflect`'s pull of `runtime.zeroVal` is the recorded case). The inverted emission needs the same
check on the *target* side, or an address-taken `CanUseLongPaths` would reference a nonexistent
`ᏑCanUseLongPaths` box (CS0103).

### 3.3 The ruling the census asked for

The census framed W1 as needing a ruling between "the push must not introduce a production project
reference" and "`-tests` must stop rewriting the production emission at all". **Both framings are
too broad**, and this design proposes a narrower invariant that follows from what is already
doctrine:

> **A `-tests` conversion's production emission may differ from `-stdlib`'s only in ways that do not
> change the project GRAPH.**

The four documented closure families all satisfy this — they change file text (an alias name, a root
escape, a using order, an init hook) and no reference. The `canUseLongPaths` flip is a **fifth,
undocumented** family and the first that moves an edge, which is precisely why it is fatal rather
than cosmetic. Stated this way the invariant is testable (§5, gate G2) rather than a matter of taste,
and it needs no change to the standing restore doctrine.

---

## 4. The `canUseLongPaths` fix, folded in

### 4.1 First, a severity correction

The census records:

> *"the `internal/syscall/windows` copy stays `false`, so `os`'s long-path support is disabled on
> Windows. A `//go:linkname` push that does not push, shipping today."*

The state is real; **"disabled" is not.** `src/core/golib/builtin.WindowsLongPaths.cs:45-51` already
records this exact situation as a *deliberate, conservative* choice, signed and reasoned:

> *"Go's initLongPathSupport also sets internal/syscall/windows.CanUseLongPaths, which makes
> os.fixLongPath stop adding the `\\?\` prefix. That flag lives in a converted package golib cannot
> reference — golib is the root of the dependency graph — and leaving it false is the conservative
> side: the extended-prefix form still works with the PEB flag set, so the only difference is which
> spelling reaches the kernel."*

And the half that actually makes long paths work **is already shipped**: golib sets the PEB
`IsLongPathAwareProcess` bit from `InitializeGoLib` (`golib/builtin.cs:72`), which is what lets an
un-prefixed >MAX_PATH path reach the kernel at all. What `CanUseLongPaths == false` costs is that
`os.fixLongPath` (`src/core/os/windows/path_windows.cs:49-54`) always takes the
`addExtendedPrefix` branch.

That is **not nothing** — extended-length paths skip Win32 normalization (no `.`/`..` collapsing, no
forward-slash translation), and any test that observes the path a syscall received will see a
different string than Go's. But it is a spelling divergence on a working path, not an outage, and
Go itself takes that same branch on every Windows older than 10.0.15063. **Chip it accurately:** the
finding is a genuine converter-model gap worth closing, not a shipped breakage.

### 4.2 Forwarding alone would be a no-op — the populate half

This is the part the census does not mention, and the project has already paid for the lesson three
times (`linknamePushTargets`' `GetSystemDirectory`, `os.runtime_args` and `os/signal` rows all say
some version of *"forwarding and populating are one change"*).

Wiring `CanUseLongPaths → runtime.canUseLongPaths` gives `os` a view onto a variable **that nothing
in the managed model ever sets**:

* `canUseLongPaths = true` is written only by `initLongPathSupport()`
  (`src/core/runtime/windows/os_windows.cs:462`).
* `initLongPathSupport()` is called only from `osinit()` (`:473`) — Go's bootstrap, which nothing
  invokes.
* Even if it were invoked, its body is
  `stdcall0(_RtlGetCurrentPeb)` (`:460`), and `stdcall` bottoms out in `asmstdcall`, a throwing stub.

So the alias would faithfully forward a permanent `false`. Worse, a naive "just set it true" fix
would be the failure mode this project explicitly rules against: if the PEB bit was *not* actually
set (an old Windows, a refused write, an unusual host — all of which golib deliberately swallows),
telling `os` to stop prefixing produces a **plausible-looking wrong answer** — paths that silently
fail instead of working.

**The design therefore ties the flag to the outcome, not to the intent:**

1. `golib/builtin.WindowsLongPaths.cs` records whether it actually set the bit —
   `InitializeWindowsLongPaths` currently returns `void` and swallows; it gains an internal
   `WindowsLongPathsEnabled` that is true only on the success path. golib remains the single place
   that knows.
2. `src/core/runtime/windows/os_windows_impl.cs` — the hand-owned Windows companion that **already
   exists** and already carries a `[ModuleInitializer] ᴛInitSysDirectory()` for precisely the
   `GetSystemDirectory` row — gains a sibling that copies that outcome into `canUseLongPaths`.
   This is the same slot, the same file, the same pattern.
3. The S2 inversion makes `internal/syscall/windows.CanUseLongPaths` read it, and
   `os.fixLongPath` behaves as Go's does.

Note this also **improves on golib's own recorded remedy**. Its comment proposes "a hand-owned
companion in `internal/syscall/windows`". Putting the companion in **`runtime`** is better on two
counts: it is where Go's own write lives, and it is the side that keeps the graph acyclic.

---

## 5. Gate plan

Ordered; each gate exists because something specific could go wrong.

| # | Gate | What it proves |
|---|---|---|
| **G1** | Converter `go test -count=1 ./...` (~232 s on this host), with **new guards**: (a) `linknamePullWouldCycle` returns true for an upward target with `conversionGraph == nil` — the M1 assertion, which must be **red-proven** by restoring the nil shortcut; (b) `linknameVarAliasTargets` and the publicize arm agree, the way `linknamePushSources` is derived from `linknamePushTargets` so the two cannot drift | The mechanism, in isolation |
| **G2** | **A cycle assertion over the emitted corpus**, new and standing: DFS the `src/core` `.csproj` graph per `$(GoTargetOS)` and require **0 cycles**. Positive control: inject `runtime → isw` and require exactly the 6 named cycles | §3.3's invariant, made mechanical. This census found W1 by hand; nothing would have caught it. **Worth landing on its own, before any W1 fix** — it is cheap, it is a pure read, and `check-solution-integrity.ps1` is its natural home |
| **G3** | Seeded `-stdlib` reconvert → overlay → build **both** `-p:GoTargetOS=windows` **and** `-p:GoTargetOS=linux`, each `--no-incremental` | The emission change touches `runtime.csproj`'s per-GOOS group and two per-GOOS `.cs` files — L3 layout, so the windows build alone would miss the linux half. Purge `bin`/`obj`/`Generated` between target switches |
| **G4** | CNR (`check-no-regression.ps1`, budget 2400 s) | Behavioral-corpus drift from the `varLinknamePull` change. The three existing forwarding-property sites (`math/bits` ×2, `time/sleep_test`) must be **byte-identical** — they are the regression surface |
| **G5** | `-tests -test-action convert` on `math/bits` and on `runtime`, HEAD converter vs. fixed, byte-compared | `math/bits`: unchanged (downward pull still forwards). `runtime`: the production `.csproj` windows group stays empty and `os_windows.cs` keeps its plain field — i.e. **W1 cleared and the fifth closure family gone** |
| **G6** | `-tests -test-action all` on `runtime` | Must now reach **W2** (202 errors) rather than MSB4006. That is the success criterion: the next wall, not this one. With the W2b gate already landed (`79f5708f5`), it will now stop at the *conversion* layer naming the three unresolved types instead |
| **G7** | `go2cs.slnx` Debug build (~3,546 s at 722 projects on this host) | golib's API changes (`WindowsLongPathsEnabled`); no other gate compiles the non-generated solution members |
| **G8** | Behavioral suite, plus a **new behavioral test** for the long-path semantics | The populate half. It must compare **real observed values** — a >MAX_PATH path round-tripped through `os` and its Go counterpart, matching `go run` — not merely the absence of a fault. The `LocalTimeZone` test is the model, and for the same reason: that arc's first fix "compiled and was operationally broken" |
| **G9** | Post-merge filtered sweep of any banked row this touches, **at the merge result** | The banked-row merge rule. `runtime` has no banked row, but `os`, `syscall` and `internal/poll` do and all three sit downstream of `fixLongPath` |

**Sequencing.** G2 is independent and should land **first**, alone: it is the standing instrument
that makes this class of defect impossible to reintroduce, and landing it before the fix means the
fix is measured by a gate that already exists rather than one written to match it.

Then W1-M (M1 + M2-as-fallback) alone, gated by G1/G4/G5 — it changes the emission of nothing in the
corpus today, which makes it a clean, separately-bankable change whose diff should be **empty**
outside the converter.

Then W1-S (S2 + the populate half) with the full list. The two halves are separable on purpose:
if the second stalls on a ruling, the first still closes the wall.

---

## 6. What is deliberately not proposed

* **No change to the standing `-tests` restore doctrine.** Four production-`.cs` closure families
  stay documented and stay restored. §3.3 narrows the invariant instead of widening the ban.
* **No golib cell (S3) for this pair.** It works and it is more general, but S2 is free here, and
  putting stdlib state in the runtime library for a case that does not need it is machinery bought
  ahead of a requirement.
* **No speculative sweep of Go's other ~200 pushes and 340 handles.** `linknamePushTargets`'
  header already rules on that: each row is a recorded judgment, and wholesale linking "would be a
  regression dressed as a feature". The same applies to var aliases — one row today, because the
  corpus exposes one.
* **No estimate of what lies behind W2/W3.** The census declined to guess runtime's semantic bill
  and this document keeps that discipline.
