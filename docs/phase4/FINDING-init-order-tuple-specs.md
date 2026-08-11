# FINDING — the init-ORDER arc is already built; what is open is a tuple-spec hole

**Status:** characterization scout, complete. **No converter or golib change made** — this is
design-with-user territory per the charter §10. Scout lane, 2026-08-10, i7-5820K.

**Headline.** The board records `crypto/internal/edwards25519` (0 of 55) as the first whole-package
casualty of an *open* init-ORDER arc, with the mechanism given as *"the converter emits C# static
field initializers in DECLARATION order."* That mechanism sentence is **stale**. The general
init-order arc **landed on 2026-07-11** (`e39855770`) and was refined four times through 2026-08-04;
the converter reproduces Go's `types.Info.InitOrder` exactly, and **36 of the 304 converted packages
already ship a generated `package_init.cs`** ordered static constructor. What is open is a single,
narrow hole: **the relocation machinery refuses to act on a package-level TUPLE var spec**
(`var a, b = f()`), and says so out loud. edwards25519 is the only production package in the whole
Windows corpus that falls in it.

---

## 1. Root cause — exact

The converter's analysis is **correct**. `collectMovedInitVars` (`src/go2cs/initOrderOperations.go`)
resolves each package-var initializer's dependencies transitively through same-package function and
method bodies, and correctly flags `identity` and `generator` as needing relocation. It then hands
them to an emission path that declines:

```go
// src/go2cs/visitValueSpec.go:1158-1164, in visitPackageTupleVarSpec
// A tuple-deconstructing package var flagged for init-order relocation is not yet
// supported (no stdlib occurrence) — surface it loudly instead of silently misordering.
if def := v.info.Defs[ident]; def != nil {
    if _, moved := v.movedInitOrdinal(def); moved {
        v.showWarning("package tuple var '%s' needs init-order relocation (unsupported for tuple specs) - left inline (init order NOT guaranteed)", ident.Name)
    }
}
```

**The "no stdlib occurrence" premise in that comment is falsified** — see the census in §3.

### The ordering that breaks

`go/types`' authoritative `InitOrder` for the package, beside the C# declaration order that actually
governs static field initializers:

| Go InitOrder | var | declared at | C# runs it |
|:--:|:--|:--|:--:|
| 0 | `feOne` | `edwards25519.go:135` | 3rd |
| 1 | `d, _` | `edwards25519.go:226` | 4th |
| **2** | **`identity, _`** | **`edwards25519.go:66`** | **1st** |
| 3 | `generator, _` | `edwards25519.go:77` | 2nd |
| 4 | `d2` | `edwards25519.go:231` | 5th |

Go initializes `feOne` and `d` **before** `identity`, because `identity`'s initializer reaches them
through `(*Point).SetBytes`. C# runs field initializers in textual order, so `identity` goes first and
reads `feOne` while it is still `null`.

### The observed failure

```
System.TypeInitializationException: 'go.crypto.internal.edwards25519_package'
 ---> System.NullReferenceException
   at ...edwards25519.field_package.Subtract(ж<Element> ᏑV, ж<Element> Ꮡa, ж<Element> Ꮡb)   field/fe.cs:116
   at ...edwards25519_package.SetBytes(ж<Point> Ꮡv, slice<byte> x)                          edwards25519.cs:172
   at ...edwards25519_package..cctor()                                                       edwards25519.cs:66
```

`edwards25519.cs:172` is `var u = @new<field.Element>().Subtract(y2, feOne);`. The cctor throws, so
every one of the 55 verdicts reports `infrastructure-error` or empty — hence 0 of 55.

Note this is **not** a cross-package ordering fault. .NET's cctor-on-first-access rule reproduces
Go's "imported packages first" guarantee by construction. The fault is strictly **intra-package,
intra-class**.

---

## 2. Why the *mechanism* already in the tree is the right one

`initOrderOperations.go` emits, for a relocated var: a **bare** field, an `initᴛ<name>()` method
**in the var's own file** (so the rendered expression keeps that file's using aliases), and a
per-package `package_init.cs` whose static constructor calls them in `InitOrder` ordinal order. That
is exact, because C# runs **every** static field initializer of **every** partial-class part before
**any** static-ctor body — so a relocated initializer is guaranteed to see every non-relocated
dependency already assigned. It also covers the two hazards declaration order cannot: cross-**file**
dependencies (C# leaves cross-part initializer order undefined) and same-file forward references.

The machinery is live and working in this very package — the *test* variant is a separate C# class
and its generated `package_init_internal_test.cs` correctly orders `initᴛB(); initᴛI();`.

---

## 3. The census — how big is the class?

Measured, not estimated. Three instruments.

**(a) Syntactic candidate set (upper bound).** An AST scan of the whole GOROOT tree for package-scope
`var a, b = f()` specs: **37 specs in 11 packages** in all of Go 1.23.1, of which most are under
`cmd/` and therefore outside the converted corpus (`src/core` has no `cmd`).

**(b) Full-corpus converter census (exact, production).** A seeded whole-stdlib reconvert
(`go2cs -stdlib -comments`, 304 packages, into an isolated seeded root per the reconvert ritual)
produced **exactly two** init-order refusals in the entire production corpus:

```
WARNING: package tuple var 'identity'  needs init-order relocation (unsupported for tuple specs) …
WARNING: package tuple var 'generator' needs init-order relocation (unsupported for tuple specs) …
```

Both in `crypto/internal/edwards25519`. The other 26 stderr warnings in that run are unrelated
(14 const-conversion notes, 3 known hand-own `.cs.auto` sibling skips, and PowerShell's own
native-stderr noise).

**The sibling fallback shape has ZERO occurrences.** `visitValueSpec.go:543` carries a second,
independent bail-out — a moved var whose initializer has a multi-value *hoisted* inner call
(`globalHoist.Len() > 0`). It did not fire anywhere in the corpus.

**(c) Per-platform and test-side.** Targeted conversions, each verified against a positive control
(the same command and filter on edwards25519 reproduces its six warnings, so a silent filter is ruled
out):

| Target / scope | Result |
|:--|:--|
| `-stdlib` windows/amd64 (304 pkgs) | **2** — edwards25519 `identity`, `generator` |
| `os` @ linux/amd64 | 0 (`executable_procfs.go` has no tuple spec; `executable_path.go` is `aix \|\| openbsd`) |
| `os` @ **darwin/amd64** | **2** — `initCwd`, `initCwdErr` (`executable_darwin.go:15`) — **LATENT** |
| `os` @ windows/amd64 | 0 |
| `crypto/tls` `-tests` | 0 |
| `math/rand`, `math/rand/v2` `-tests` | 0 |
| `crypto/internal/edwards25519` `-tests` | 4 test-side (see below) |
| `syscall/exec_plan9.go` | plan9 only — never a go2cs target |

So the **complete** class today is:

- **2 production vars, 1 package, Windows** — `edwards25519`, actively failing, 55 verdicts.
- **2 production vars, 1 package, darwin** — `os`, **latent**. Layout L3 keeps
  `executable_darwin.go`'s emission in the darwin folder, so it costs nothing today and will bite the
  moment the darwin corpus is exercised. It is also the *other* emission sub-shape (two non-blank
  names → the hidden static tuple holder), so a fix that only handles edwards25519's single-non-blank
  shape would leave it open.
- **4 test-side vars in edwards25519** — `scOne`, `scMinusOne`, `dalekScalar`,
  `dalekScalarBasepoint`. These are flagged because the analysis correctly models Go's single-package
  view (production `.go` and `_test.go` are one Go package, so `scalarMinusOneBytes` in `scalar.go` is
  a cross-*file* dependency). The **emission** puts them in two different C# classes
  (`edwards25519_package` and `edwards25519_internal_test_package`) with independent static ctors, and
  the CLR already orders those correctly. Over-relocating them is harmless, so this is a modelling
  imprecision, not a defect — worth a note, not a blocker.

---

## 4. What a fix buys — measured, not predicted

The relocated emission was **hand-simulated in the generated C#** (converter untouched): `identity`
and `generator` made bare fields with `initᴛidentity()` / `initᴛgenerator()` companions, plus a
`package_init.cs` static ctor calling them in ordinal order. Then the package was driven through
`go2cs -tests -test-action compare`.

**Result: 0 of 55 → 52 of 55 matching.** The type-initializer failure is gone and the package runs
end to end (152 s). *(The experiment was reverted; the worktree is clean.)*

The three residual divergences are **separate, already-known roots** — none is init-order:

| Test | Root |
|:--|:--|
| `TestAllocations` | The **AllocsPerRun** class. With the r58a counter live it reports a real COUNT — *"counted 10,900 go2cs-runtime object allocations over 100 runs … expected zero allocations, got 109"* — but golib's counter covers golib's sites only, so it is an explicit **lower bound**. A **fifth** member of that class, and on the standing rule not disclosable as-is. |
| `TestScalarSetCanonicalBytes` | `panic: index out of range [-1] with length 0` in `array<T>.get_Item`, reached from `testing/quick`'s reflect-driven `Check`. |
| `TestScalarSetUniformBytes` | *"failed on input `[0]uint8{}`"* — **same root**: `quick.Value` via the reflection bridge synthesizes a **zero-length** array where the parameter is a fixed-size `[32]byte`/`[64]byte`. |

So two of the three share one root — fixed-size-array generation through `reflect`/`testing/quick` —
which is reflection-bridge territory and plausibly adjacent to the array-backing materializer work at
`d5c0c9c10`.

---

## 5. Remedy options

### Option A — extend the existing relocation to tuple specs ✅ **RECOMMENDED**

Teach `visitPackageTupleVarSpec` the relocation the plain path already performs. Two sub-shapes:

- **One non-blank name** (`identity, _`): bare field + companion, exactly parallel to the plain path.
  Blank siblings stay bare — their values are never read.
  ```csharp
  internal static ж<Point> identity;
  internal static error _ᴛ1ʗ;
  internal static void initᴛidentity() { identity = @new<Point>().SetBytes(…).Item1; }
  ```
- **Two or more non-blank names** (`initCwd, initCwdErr`): assign every component in **one** method
  from a **local**, which makes the hidden static tuple holder unnecessary — the relocated form is
  *simpler* than the inline one, and the call still runs exactly once:
  ```csharp
  internal static @string initCwd;
  internal static error initCwdErr;
  internal static void initᴛinitCwd() { var t = Getwd(); initCwd = t.Item1; initCwdErr = t.Item2; }
  ```
- **All-blank** (`var _, _ = f()`): unchanged — blanks are already excluded from
  `packageMovedInitVars`, so they are never flagged.

Ordinals need no new bookkeeping: Go's `InitOrder` yields **one** entry per spec with all names in
`Lhs`, so one registered method per spec lines up with `writeOrderedInitCalls` as-is.
`packageInitMethodName`, `recordMovedInitMethod` and `writePackageInitFile` are reused unchanged.

- **Cost:** one converter file, roughly 30 lines. No golib change, no new mechanism, no new file kind.
- **Blast radius:** 2 production files (edwards25519 on Windows, `os` on darwin) plus one new
  `package_init.cs`; everything else byte-identical — `check-no-regression` proves it.
- **Guard:** extend the existing `PackageVarInitOrder` / `MultiFileInitOrder` behavioral tests with a
  tuple-spec case in both sub-shapes, plus a converter unit test asserting the warning no longer
  fires for them. The warning itself should **stay** for any shape still unsupported.
- **Payoff:** closes the class completely (the analysis is already right), and turns a
  whole-package casualty into 52 of 55.
- **Risk:** low. The failure mode of a mistake is loud (a null-deref at type-init), not silent.

### Option B — a golib init orchestrator (eager, dependency-ordered package init)

Force every imported package's initialization eagerly in dependency order at startup — the
alternative already contemplated on this board for the blank-import case (§ *A blank import forces
the imported package's `init` to run*).

**Rejected for this class, on relevance rather than cost.** This fault is intra-package and
intra-class: an orchestrator that orders *packages* still leaves edwards25519's own field
initializers running in declaration order, so it would have to be paired with Option A regardless. It
also carries the known price of loading the whole transitive assembly closure at startup. It remains
a legitimate answer to a *different* question (cross-assembly `init` side effects under lazy load),
and nothing here argues against it there.

### Option C — lazy-init at first touch

Emit every order-sensitive package var as a property with a once-guard, so any order is correct and
no analysis is needed.

**Rejected.** It puts a branch on every read of every package var on hot paths (`feOne` and `d` are
read on every point decode); it erases the "reads like Go" property the project explicitly optimizes
for, replacing plain fields with guarded properties across the corpus; and it cannot express Go's
guarantee that an initializer runs **once, in a defined order, relative to side effects**. The
existing ordered-ctor mechanism is already exact and costs nothing at read time.

### Rejected without a section

**Reordering the emitted declarations** into `InitOrder` — it would break the visual fidelity that is
a stated project goal, and it cannot fix the cross-**file** case at all, since C# leaves partial-class
part order undefined. That is precisely why the relocation mechanism exists.

---

## 6. Recommendation

**Take Option A.** It is the durable, general fix rather than a one-off: it removes a documented
"not yet supported" hole in a mechanism that is otherwise complete and correct, it is small and
mechanically guarded, and the census bounds its blast radius to two production files. It also closes
the darwin `os` case **before** the darwin corpus is exercised, which is the difference between a
fix and a future incident.

Two things worth carrying into the ruling:

1. **The board row should be rewritten**, not just re-scored. Recording this as "the converter emits
   declaration order" understates what exists and would send the next lane to build a mechanism that
   is already there and working in 36 packages.
2. **Fixing this does not bank the package.** It yields 52 of 55; the remaining three belong to the
   AllocsPerRun class (one) and to a fixed-size-array-through-`reflect`/`testing/quick` root (two).
   Both are pre-existing arcs, and the second is a genuinely new root this scout surfaced.

---

## 7. Proposed replacement for the board's `crypto/internal/edwards25519` row

> | `crypto/internal/edwards25519` | 0 of 55 → **52 of 55 with the tuple-spec fix** | **Package-var init ORDER, tuple-spec hole.** Go initializes `feOne`(0) and `d`(1) before `identity`(2); C# field initializers run in declaration order, so `identity` (line 66) reads `feOne` (line 140) while null, `field.Subtract` null-derefs, and the package cctor throws before any test runs. The general init-order mechanism **already exists and is correct** (`initOrderOperations.go`, landed `e39855770` 2026-07-11; 36 packages ship a generated `package_init.cs`) and it **flags these two vars correctly** — it then declines to act because they are TUPLE specs (`var identity, _ = …`), warning loudly at `visitValueSpec.go:1158`. Whole-corpus census: **exactly 2** production occurrences (both here) on Windows, **2 latent** on darwin (`os` `initCwd`/`initCwdErr`), zero elsewhere; the sibling hoisted-initializer fallback never fires. Hand-simulating the relocation takes the package to **52 of 55**; residual = `TestAllocations` (AllocsPerRun class, 5th member) and `TestScalarSetCanonicalBytes`/`TestScalarSetUniformBytes` (one shared **new** root: `testing/quick` + reflection bridge synthesizes a zero-length array for a fixed-size `[32]byte`/`[64]byte` parameter). Options, costs and recommendation: [`FINDING-init-order-tuple-specs.md`](FINDING-init-order-tuple-specs.md). |

---

## 8. Reproduction

```powershell
# the failure (unmodified tree)
go2cs -tests -test-action all -test-timeout 30m "<GOROOT>\src\crypto\internal\edwards25519" "<repo>\src\core\crypto\internal\edwards25519"

# the production census (seeded root per the reconvert ritual — seed src/core, version.props, docs/validation)
go2cs -stdlib -comments -go2cspath <tmp>\src 2> census.stderr.txt
Select-String census.stderr.txt -Pattern "init-order relocation"

# the darwin case
go2cs -comments -platforms darwin/amd64 -go2cspath <repo>\src "<GOROOT>\src\os" <tmp>\os-darwin
```

Go's authoritative order for any package is `types.Info.InitOrder`; a throwaway `go/types` driver
printing it beside each var's declaration position is what produced the table in §1.
