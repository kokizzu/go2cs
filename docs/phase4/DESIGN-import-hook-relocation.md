# DESIGN — relocating the forced-init import hooks out of mainline code

**Status:** spec, not yet implemented. Owner-sourced scout (2026-08-31), accepted by coordinator
ruling the same day; the implementation is a corpus-wide emission change and lands in the **rebank
wave**, whose full-roster sweep is its gate.

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

**Structural note.** `package_info.cs` today contains ONLY assembly-level attributes in its
XML-comment-delimited regions. Hosting a `[GoInit]` method means it must also gain a
`static partial class <pkg>_package` body — a shape change to a file that is ALSO an INPUT to
dependent packages' transpiles (the `ImportedTypeAliases`/`GoImplement` records are read back off
disk), so the cross-package read path wants checking before anything is cut.

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

**Still unmeasured:** whether the move retires the `-tests`-closure drift class. The `initTests` hook
is emitted by a `-tests` run while the committed corpus rests on the `-stdlib` side, so no single
tree shows the interaction without a conversion the scout was not going to run mid-merge.

## 5. Blast radius, sequence, guard

Corpus-wide on **two** axes, not one: the emission AND the csproj `<Compile>` shape. So it lands with
a rebank-style regen.

Recommended sequence:

1. ~~Decide `package_info.cs` vs `package_init.cs`~~ — **settled in §4: `package_info.cs`.**
2. Land the **compile-ordering change first**, as its own gated step. It is independently correct and
   independently testable, and it is what makes step 3 safe.
3. Then relocate the hooks.

`tests/Behavioral/NamedImportInitOrder` must stay green throughout; per §2 it is a real positive
control, not a formality. A `-stdlib` reconvert plus CNR covers the emission change; the wave's
full-roster sweep is the bank gate.
