# DESIGN — runtime duck-typing shells for NAMED interfaces

> **Status: PROPOSED — awaiting user blessing (charter §7/§10).** Produced 2026-07-24/25 by an
> adversarial design panel (three lenses — full-generalization, runtime-pairing, demand-driven —
> plus a critic that re-measured every disputed number against the committed tree). Stage 1 (latent
> bug fixes that exist today) is pre-approved §2 territory and proceeds independently.
> Companion to [`Phase4-Autonomous-Loop-Charter.md`](Phase4-Autonomous-Loop-Charter.md).

## 1. The problem (measured)

A NAMED Go interface has no runtime duck-typing surface. TypeGenerator emits the `ᴛAs`/Δ-wrapper
machinery only for `[GoType("dyn")]` (anonymous) interfaces — 33 in the corpus vs **269 named**.
golib's `TryTypeAssert` resolves named-interface asserts via `AdapterRegistry` (compile-time
`GoImplement` records) and its structural fallback answers the *probe* correctly but cannot
*construct* a wrapper. The compile-time recorders (assertion-site `convTypeAssertExpr.go:48/110/162`,
declaration-site `visitInterfaceType.go:182/571`) are structurally incomplete approximations: a
dynamic type may live in an assembly converted *later* than the asserting package (io/fs is
converted before os; `fs/package_info.cs:48` records only `subFS→ReadDirFS`; `os.dirFS` is
unreachable by construction). Demonstrated consumers: io/fs stuck 16/18, bufio's `io.StringWriter`
probe (B3), the B9 adapter CS0535 class.

Panel ground truth (critic-verified): **1540 GoImplement records across 164 package_info.cs**;
269 named + 33 dyn + 3 constraint interfaces; 746 transitive methods across named interfaces
(mean 2.8, max 37). The brief's "transitive method-collection gap" is STALE — `AllInterfaces`
walking already works cross-assembly (proof: internal/testenv's 818-line `CommandContext_type : testing.TB`
wrapper); it needs a 3-deep-embedding guard, not a fix.

## 2. Panel verdicts (one line each)

- **D1 full-generalization**: strongest evidence — built and measured end-to-end in a scratch
  clone; only correct census; found the six latent bugs and the recorder-flip non-inertness; but
  its "boxed" shape pays reflective dispatch per CALL (~10× regression) even where AOT doesn't
  require it.
- **D2 runtime-pairing**: best mechanism — the tier theorem (no dynamic codegen ⟹ a per-interface
  compile-time artifact is the irreducible minimum), the ж-is-a-class insight, the AOT
  seed-hypothesis refutation; but its census numbers were wrong (~20% inflated).
- **D3 demand-driven**: honestly retired itself (demand set = 85.3% of full — false economy);
  keep its memoization-same-commit rule and internal-wrapper/Δ-collision hazards.

## 3. The synthesis (recommended build)

**Per-interface, provider-side, tiered, attribute-discovered.** For every non-generic,
non-constraint, non-empty `[GoType]`/`[GoType("dyn")]` interface `I`, TypeGenerator emits into
*I's own* `<pkg>_package` (the only placement that terminates — the asserting site always has the
interface's assembly loaded) **two sibling shells**, discovered via a new golib attribute
`[GoInterfaceShell(...)]` stamped on the interface — **no static members on the interface**
(eliminates the CS0108 noise class — 240 sites in net/http alone — the static-leak class, and
makes shell names non-contractual, retiring the Δ-name collision debt):

1. **`ΔI<TTarget>`** — delegate-bound generic shell (today's dyn template minus its operator/nil
   block), used when the dynamic value is **reference-typed** — i.e. every `ж<X>` box (ж<T> is a
   class), which is every pointer-sourced Go interface value. AOT-safe via canonical sharing.
   **2.78 ns/call measured.**
2. **`ΔIObj`** — non-generic object-held shell dispatching through cached `MethodInvoker`, used
   when the value is **value-typed** (the `os.dirFS` forcing case — a `[GoType("@string")]`
   struct). AOT-safe. **21.9 ns/call measured.**

Kind branch on `Type.IsValueType` with a `catch(NotSupportedException)` belt to the object shell
(AOT rooting is source-shape-sensitive; the "just seed a template" shortcut is refuted
permanently). **One golib `AdapterBinder`** owns binding, reusing `StructurallyImplements`'
receiver rule verbatim — the X3 method-set discipline in exactly one reviewed place.
**`TryCreate` is fail-soft** (null = miss): a false-positive structural match must reproduce
today's harmless miss, never a crash (measured: `Implements<Getter>(box<int>)` = True today).
Factories **memoize into the existing AdapterRegistry in the same commit** (uncached construction
is 1,123 ns; fmt probes three interfaces per formatted value). No cache clearing on AssemblyLoad
(Go method sets are compile-time-fixed — documented decision); negatives cached only after the
value's own assembly is loaded. `TryTypeAssert` tier order unchanged — the new tier fires only
where today answers MISS (the regression-floor argument).

## 4. Staged landing plan (each stage independently gated and revertible)

| Stage | Content | Gate highlights |
|---|---|---|
| **1** (≈0.5d, ships now) | Six latent-bug fixes that exist TODAY: golib `TypeExtensions.cs:570` base-interface static leak (poisons every derived-interface structural probe — live bug); analyzer `IsStatic`/`MethodKind` filter; bare-name+escaped identifier composition; Δ-name escaping; forwarding-local marker; `TryTypeAssert` fail-soft. | CNR byte-identical; guard `DerivedInterfaceStructuralProbe` |
| **2** (≈2-3d) — **unblocks io/fs** | Tiered shells + attribute + binder + memo + builtin consumption; skip zero-method/generic/constraint interfaces. | Converter untouched ⇒ CNR byte-identical; 7 behavioral guards (incl. cross-assembly late-assert = the io/fs shape, X3 negative, false-positive-must-miss, unexported-interface); corpus build = the rendering-risk gate (five ≥10-method interfaces); **acceptance: io/fs 16/18 → 18/18, 37-sweep clean** |
| **3** (≈1-1.5d) | Migrate the 33 dyn interfaces onto the tiered shape; delete the `MakeGenericMethod` path (`builtin.cs:1638/:1662`) — **closes the AOT hole that exists today**; PerfAot smoke benchmark. | Full suite + corpus + sweep; perf gates (memoized assert ~30 ns; pointer tier ≤3 ns/call) |
| **4** (≈1.5d) | Recorders behind a default-off flag; reconvert. **Not inert (measured): 1540→1329 records, −335/+124** — the recorders currently *suppress* 124 demanded records. Preserve the Promoted(52)/ConstraintProxy(11) record paths — compile-time nominal, non-retirable (96% of records ARE retirable). | Inspect every changed package_info.cs; sweep clean twice; independently revertible |
| **5** (≈0.5d) | Delete the recorder machinery (~241 lines + 12 plumbing refs). `ImplementGenerator` STAYS (declared conversions are not heuristics; nominal adapters remain the 1.1 ns fast path). Docs in the same change. | Final sweep + docs verified against real goldens |

Total ≈1.5–2 weeks. Deferred successor recorded: `IDynamicInterfaceCastable` (71 ns/call, larger
architectural change) if profiling ever demands it.

## 5. Decision requested (user)

1. Bless the synthesis (tiered shells, attribute discovery, fail-soft, staged retirement)?
2. Stage sequencing OK (2 unblocks io/fs; 4/5 retire the recorders as separate, revertible
   landings)?

Coordinator recommendation: bless as specified. Stage 1 is already in flight as independent bug
fixes. Stage 2 is the arc's first real landing and carries the io/fs acceptance signal.
