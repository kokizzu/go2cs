# DESIGN — Reflection bridge Phase 3: write-back & dynamic call (chip working plan, v2)

> **Status: INCREMENT 1 IMPLEMENTED 2026-07-24 — errors VALIDATES (#34, 61/61 vs `go test -json`,
> zero disclosed/skipped).** The shipped record lives in
> [`DESIGN-reflection-bridge.md`](DESIGN-reflection-bridge.md); this file remains the chip's design
> + adversarial-review ledger. Implementation notes vs the plan: the Set store landed as
> ref-routed writes through `ж<T>.ValueSlot` (no DynamicMethod needed — assignment through the
> ref-returning property); X3 landed as `IInterfaceAdapter`-only transparency (instance-state
> discriminated), satisfying the B2 method-set constraints by construction; two additional
> shared-machinery defects were caught and fixed by the guards during implementation
> (`StructurallyImplements` counting `[GoRecv]` ref-receivers in the VALUE method set; the golib
> core interfaces' plain-`As` conversion hooks invisible to the assert fallback, which forced the
> converted fmt onto `&`-prefixed pointer printing). Chip session `optimistic-bassi-496625`,
> 2026-07-24.
>
> **Blessing record (user rulings):**
> - **Q1 model: blessed**, conditions: (a) the canonical nil-box singleton is **write-protected**
>   — a write through any `(*T)(nil)`-derived box panics exactly like Go's nil deref (structurally
>   enforced by ж's `Value` ref-getter throwing on `IsNull`; verified + guarded by test); (b) the
>   compiled ref-accessor cache is **thread-safe** (concurrent first-`Set` on one closed `ж<T>`).
> - **Q2 scope: chip lands X1–X5** in the same gated change-set; ledger lock extended by the
>   coordinator accordingly. Caveats: the channels track concurrently edits `builtin.cs`'s
>   channel/select region (disjoint; coordinator resolves textual overlap at integration); land on
>   the chip **branch**, not master (three live tracks — coordinator runs final all-ships-rise and
>   ff-merges in order); **one commit per X-fix, each with its own guard**.
> - **Q3: increment 1 first** (errors), increment 2 designed later against testing/quick +
>   encoding/binary differentials.
> - **⚠ Sequencing:** integration-wave3 (Change C reference-model test projects + unicode #33 +
>   CS0050 fix) lands on master imminently; `testConversion.go`'s `processTestConversion` is
>   restructured (X5 targets the NEW shape) and errors — black-box-only — flips to the reference
>   model. **Rebase onto post-landing master and re-measure the errors baseline differential
>   before implementing.** Coordinator flags when landed.
>
> Designed against the **demonstrated consumer**: the `errors` package pipeline
> (`go2cs -tests -test-action all "<GOROOT>/src/errors" src/go-src-converted/errors`).
>
> **v2 changes:** three independent adversarial reviews (lenses: Go-semantics, blast-radius,
> generalization) produced 10 substantive findings; every REWORK-class finding is folded into the
> model below and marked `[R:…]`. The §8 review ledger records them all.

## 1. Ground truth — the errors baseline differential (fresh converter, 2026-07-24)

| Test | Go | C# | Root cause (verified, not assumed) |
|---|---|---|---|
| `TestIs/#09 #10 #15 #27 #28` (poser / `&errorUncomparable`) | pass | **fail** | `err._<is_type>(ᐧ)` **never succeeds for any adapter-wrapped value** — probed at runtime: `assert ok=False` everywhere; the poser `Is` closure never runs. golib `TryTypeAssert`'s structural fallback (`Implements<T>` + `ᴛAs`) probes the **adapter class** (`poserжerror`) instead of the wrapped dynamic value (`ж<poser>`); the adapter has no `Is`. (The registry path *does* unwrap; the fallback doesn't.) |
| `TestAs` (all 18) | pass | **infrastructure-error** | Test preamble `rtarget.Elem().Set(reflect.Zero(...))`: bridge `Elem()` returns a **non-addressable copy** → `Set` → `mustBeAssignable` → `methodName()` → `runtime.Caller` → `callers` → **`getcallersp` `NotImplementedException`**. Behind it: `Set`, `Zero`, addressability, `AssignableTo`, `Implements` are all unimplemented on the bridge. |
| `TestAsValidation/*int(<nil>)` | pass | one-sided | `(*int)(nil)` boxed to `any` is a **null reference** — typed-nil loses its type, `%T` prints `<nil>`. Same loss breaks `errorType = reflectlite.TypeOf((*error)(nil)).Elem()` (wrap.cs:185) → nil Type → `Implements(errorType)` panics for every concrete target. |
| `TestAsValidation/*string(0x…)` | pass | one-sided | Subtest name embeds a pointer address — nondeterministic on *both* sides; exact-name keying can never match. |
| `TestIs` subtests | — | — | C# host names empty-name subtests `#00`, `#00#01`, … ; Go names them `#00`…`#29`. Every row one-sided. |

Everything else (TestUnwrap, TestJoin*, TestNewEqual, remaining TestIs) already passes on the
Phase-1/2 bridge.

## 2. The write-back model — one new primitive

Phase 1/2: carry `System.Type` + the boxed object instead of `{type,data}` words. Phase 3 adds
one primitive: **an addressable Value carries the `ж<T>` box it aliases**; every write goes
through that box. No `unsafe` — the box *is* Go's address, and golib boxes already alias struct
fields and array elements, so the same slot extends to `Field(i)`/`Index(i)` addressability
without a model change.

### 2.1 Value companion + the structural-nil predicate `[R:C1]`

```csharp
partial struct ΔValue {
    internal object? boxed;    // Phase 2 (existing): the boxed Go value this Value represents
    internal object? addrBox;  // Phase 3: the ж<T> box this Value ALIASES when addressable
}
```

- `ValueOf(ptrBox).Elem()` → `{ addrBox = ptrBox, typ_ = synthType(pointee), flag = kind | flagAddr | flagIndir }`.
- **Readers read through the box lazily** (`currentValue` accessor: `addrBox` set → live
  `box.Value` read; else `boxed`). Hard requirement: TestAs's `poser.As` writes through the *same*
  heap box directly, then the test reads `rtarget.Elem().Interface()` — a snapshot returns stale
  data.
- **`[R:C1]` The nil predicate is STRUCTURAL, never value-peeking.** `ж<T>.IsNull` returns true
  for a heap box whose *held reference-typed value* is null (`m_val is null` arm) — but such a box
  (`ᏑerrP`, a real `ж<ж<PathError>>` address) is a **non-nil pointer holding a nil value**. Probing
  `IsNull` makes `As` panic "non-nil pointer" on 8 of 18 TestAs targets and `Elem()` return the
  invalid Value at case #0. golib gains a structural predicate on `ж<T>` (true iff `m_isNull` — the
  canonical-nil/X2 form — with a null *reference* also structurally nil); the bridge's
  `IsNil`/`Elem`/nil-guards use **only** that. The existing value-peeking `IsNull` probes in the
  Phase-2 readers (`IsNil`, `Elem`, `reflectPointerToken`) are corrected in the same change.
- `Elem()` of a **structurally nil** pointer → invalid zero Value (Go). `Elem()` of an
  interface-kind Value re-derives from the dynamic value (existing).
- `CanSet`/`CanAddr`/`Kind`/`IsValid` keep working from auto flag code — entry points now set real
  `flagAddr|flagIndir`.

### 2.2 `Value.Set(x)` (hand-owned, both packages) `[R:C2,C4,G-F1]`

1. `v.mustBeAssignable()` — keep the auto flag-based check; its panic path is fixed by §2.5.
2. **Assignability decided FIRST, Go-style (`assignTo`), for every source including nil**
   `[R:C2]`: identity (`srcType == dstType`) or interface-implements (§2.3); a typed-nil src of
   the wrong pointer type panics exactly like Go. Marshalling per dst slot:

| dst slot | src | store |
|---|---|---|
| concrete struct `T` | boxed `T` (identity) | the struct value |
| pointer `ж<T>` | `IжAdapter` wrapping `ж<T>` | the unwrapped **box** (Go: interface holds the `*T`) |
| pointer `ж<T>` | raw `ж<T>` / canonical nil box (identity) | the box / `null` slot value for structural nil |
| interface `I` | implements-check passed | an `I` instance via the golib assert machinery (non-generic `TryTypeAssert(object, Type, out object)`, added by X1); **a typed-nil pointer src stores the canonical nil box wrapped for `I` — a NON-nil interface holding `(*T)(nil)`, Go's `packEface` result** `[R:C2-B]` |
| mismatch | — | Go panic `"reflectlite.Set: value of type X is not assignable to type Y"` |

3. **Store mechanism `[R:C4,G-F1]`:** `ж<T>.Value` is a **get-only ref-returning property** —
   `PropertyInfo.SetValue` cannot write it, and reflection-invoking a ref getter yields an
   unwritable copy. The store is a **cached compiled ref-accessor write** (per closed `ж<T>`: a
   generic helper closed via `MakeGenericMethod` that assigns `((ж<T>)box).Value = (T)v` through
   the `ref`, or equivalent DynamicMethod IL). This same shape is what `Field(i)`/`Index(i)`
   addressability needs in increment 2 (ж's typed `of(FieldRefFunc)`/array-index alias ctors route
   through `Value` — `FieldRef.Create`'s `m_val`-hardcoded IL is **wrong** for element/nested
   parents), so the contract is landed ref-based from day one rather than reworked later.
   **Blessing conditions (Q1):** the accessor cache is a `ConcurrentDictionary` keyed by the
   closed `ж<T>` type (`GetOrAdd`; a benign double-compile race is acceptable, a torn entry is
   not); and a store through a **structurally nil** box must panic Go-style — ж's `Value`
   ref-getter already throws `NilPointerDereference` on `IsNull` before the ref exists, which
   protects the shared canonical singleton structurally; the Set path re-checks and converts this
   to the Go panic, and a behavioral guard pins it (write through a `(*T)(nil)`-derived Value
   panics; the singleton is observably unmodified after).

### 2.3 `Type.Implements` / `Type.AssignableTo` (hand-owned, reflectlite now)

Bridged over the **existing** golib structural machinery — one method-set rule everywhere (§8
charter), never a second implementation:

- `directlyAssignable(T, V)` → `T.sysType == V.sysType` (identity; named-type distinctness free).
  The Go unnamed↔named underlying rule is deferred with a named consumer (encoding/binary) and a
  TODO in the impl.
- `implements(T, V)` → `T.sysType.IsInterface &&` (nominal `IsAssignableFrom` **or** golib
  `StructurallyImplements(V', T)`), `V'` = `ж<X>`→`X` receiver-element resolution (already inside
  `StructurallyImplements`, which also already enforces Go's value-vs-pointer method-set rule).
- `Comparable()` — already correct (Phase 2).

### 2.4 `reflect.Zero(t)` + ONE nil encoding `[R:C3,G-F2,C5]`

Hand-owned `Zero`:

- value kinds → `Activator.CreateInstance(sysType)` boxed zero; String → empty `@string`.
- pointer kind → a **valid Value whose `boxed` IS the canonical per-type nil box** (the same X2
  singleton — see below). `[R:G-F2]` There is exactly **one** typed-nil encoding in the whole
  system; `boxed == null` never means "typed nil", only "nil interface / invalid".
- interface kind → valid Value, `boxed = null` (Go's nil interface genuinely has no type).
- slice kind → `default(slice<T>)` (the nil slice). map/chan/func kinds: deferred with named
  consumers (gob/binary/quick), impl throws a scoped `NotImplementedException` naming them.

`Interface()` / `valueInterface` on a valid typed-nil pointer Value returns the canonical nil box
(a **non-nil** `any` holding `(*T)(nil)`, Go-correct `%T`/`!= nil`) `[R:C3]` — never null.
`reflect.New` is deferred to the gob/binary increment (cheap once Zero exists) rather than landed
untested.

### 2.5 The `getcallersp` path

`getcallersp` is unimplementable; the semantic boundary with a managed answer is `methodName()` —
the only operational consumer on this path. Hand-own `methodName` in `reflect` + `reflectlite`
over `System.Diagnostics.StackTrace` (best-effort Go-shaped `pkg.Method`, `"unknown method"`
fallback). Misuse of `Set` then panics like Go instead of dying in a stub. (Review confirmed no
errors test observes the message text.) The `getcallersp` stub stays — its other callers are
non-operational runtime paths.

### 2.6 Adapter-type unwrap at the descriptor (R10) `[R:B-R10]`

`abi.TypeOf`/`synthType`, `GoReflect.KindOf`, `GoReflect.ElementType` gain the unwrap
`GoTypeName` already does: pointer-sourced `IжAdapter` ⇒ descriptor for `*T`
(`sysType = typeof(ж<T>)`, Kind Pointer); ᴠ-adapter ⇒ the wrapped struct type. `Value.boxed`
keeps the original interface value (dispatch/`Interface()` untouched); only type identity
unwraps. Load-bearing for `AssignableTo`; also heals the latent Phase-2 hole where adapter-held
and raw-box values interned to different canonical Types. Review confirmed `%T`, `IsComparable`,
and the csv DeepEqual paths are stable under the flip.

`[R:B-R10]` In the same change, the **Value readers become adapter-aware** where kind now reports
Pointer: `Elem()`/`reflectPointerToken`/`UnsafePointer` unwrap `IжAdapter.Box` before probing —
review showed `Elem()` on an adapter currently returns the invalid Value and is masked only by
the equal-vs-equal DeepEqual shape; a guard test pins the fixed behavior (`%v` of a *methodless*
`*struct` in an interface prints Go's `&{…}`).

## 3. Registry additions (`manualConversionFuncs`)

- `reflect`: `Value.Set`, `Zero`, `methodName`.
- `internal/reflectlite`: `Value.Elem`, `Value.IsNil`, `Value.Set`, `rtype.Elem`,
  `rtype.Implements`, `rtype.AssignableTo`, `methodName`.

(reflectlite's auto `Elem`/`IsNil` read the never-populated `v.ptr` — `IsNil` currently answers
true for every pointer; both must be bridged for `errors.As` to reach its loop at all.)

## 4. Adjacent shared-machinery fixes errors REQUIRES that are NOT the reflect surface

Root-caused during this chip's survey; errors cannot validate without them; they live outside the
chip's declared file lock. **Ownership decision requested (§6 Q2).**

- **X1 — golib `TryTypeAssert` structural-fallback unwrap** *(the "multiError residual")*: the
  fallback (`Implements<T>` probe + `ᴛAs`) must probe the unwrapped dynamic value. **Guarded
  pattern REQUIRED `[R:B2]`: match `IжAdapter { Box: not null }`** (the null-guarded form
  `builtin.cs` and `error.cs` already use) — a bare `IжAdapter` match would null out value-backed
  Δ-wrappers (X3) and break their asserts. Also adds the non-generic
  `TryTypeAssert(object, Type, out object)` §2.2 needs. Fixes TestIs #09/#10/#15/#27/#28 and
  TestAs's poser `As` route.
- **X2 — typed-nil pointer boxing (converter + golib)**: a nil pointer crossing into interface
  space becomes the **canonical per-type nil box** (per closed *named* pointer type — `intRef`,
  not just `ж<int64>` `[R:B1]`), restoring Go's `any((*T)(nil)) != nil`, `%T`, and the pervasive
  `reflect.TypeOf((*T)(nil)).Elem()` idiom. Scope, per review:
  1. Conversion-expression sites (`(*T)(nil)` → the singleton) **and the comparison-operand
     emission symmetrically** `[R:B1]` — `NamedPointerReinterpret` compares `v == ((intRef)default!)`
     via *reference equality on `object`*, so both sides must yield the same singleton instance or
     its output flips `nilref`→`other`. That test is the X2 gate; its golden re-baselines.
  2. **Adapter construction sites** `[R:C5]`: a null pointer entering a generated adapter ctor
     wraps the canonical nil box (`Box` never null) — otherwise `errors.Is((*A)(nil)-err, (*B)(nil)-err)`
     compares two null Boxes equal (Go: false), typed-nil equality across the `any`/`error`
     boundary breaks, and `case *T:` on a nil-holding interface can't match.
  3. **ж equality unification** (chip-found): a canonical nil box vs a plain null `ж<T>`
     reference must compare equal (`p == q`, one from `(*T)(nil)`, one never assigned) — `Equals`
     and both operator forms treat structural-nil ↔ null-reference as equal.
- **X3 — Δ-dyn wrapper adapter transparency (go2cs-gen TypeGenerator template)**: the duck-typing
  wrappers join the unwrap protocol so a reflection-`Set` interface store compares equal to the
  original value in `AreEqual` (TestAs `&timeout` want-compare). **Shape constrained by review
  `[R:B2]`:** `IжAdapter.Box` returns `m_target_ptr` **only when ptr-backed** (null otherwise);
  the value-backed form exposes its value via `IInterfaceAdapter.Value` — the discriminator is a
  runtime field, and an unconditional `Box` would grant a value-sourced wrapper the pointer
  method set through X1's unwrap (a Go method-set violation). New behavioral guard: value-sourced
  dyn-interface value with a pointer-receiver-only method must NOT assert to the wider interface.
- **X4 — test-host empty-name subtest numbering** (`core/testing`): Go names repeated `t.Run("")`
  children `#00`…`#NN`; the host emits `#00`, `#00#01`, …. **Branch only the `name == ""` case**
  `[R:B-X4]` — the shared dedup dictionary also serves duplicate *non-empty* names
  (`sort.TestFind`'s `ab#01/#02`), which must keep their keys byte-identical.
- **X5 — oracle address normalization** (`testConversion.go`, compare-only): **two-phase
  matching** `[R:B-X5]` — exact names first; only the leftovers are re-keyed with
  `0x[0-9a-f]+ → 0x…` and paired **only when unambiguous 1:1**. Deterministic hex-literal names
  (text/scanner-style tables) match exactly in phase 1 and are never collapsed; collisions
  simply stay unmatched (fail loud, never mask).

## 5. Explicitly deferred Phase-3 surface (named next consumers — same chip, next increment)

| Item | Next consumer (demonstrated, not predicted) |
|---|---|
| `Value.Call` (delegate `DynamicInvoke`, multi-return tuple destructure into `NumOut`/`Out(i)`) | **testing/quick** (review: buildable on the triple; no `flagMethod` needed) |
| `MakeSlice`/`MakeMap`/`New`/`SetMapIndex`/`Set{Int,Uint,…}`; `Field(i)`/`Index(i)` addressability via ж field-ref/element-ref alias boxes + a runtime ref-accessor builder | **testing/quick** (`R`-struct round-trip), **encoding/binary** (Read/Write recursion), then **gob** |
| Go unnamed↔named `directlyAssignable` refinement | encoding/binary named-slice cases |
| reflect-side `Implements`/`AssignableTo` mirrors | first reflect consumer that calls them |
| map/chan/func `Zero` kinds | quick/gob |
| **Known limitation `[R:G-F3]`:** named func types collapse to their structural `Func<>`/`Action<>` under `canonType` System.Type interning — `TypeOf(namedFunc) == TypeOf(plainFunc)` wrongly true. Recorded now; needs carried named-func identity if a consumer (gob's type registry) lands on it. | gob |

## 6. Decisions requested (§10)

1. **Bless the v2 write-back model** (§2): box-carried addressable Values with the *structural*
   nil predicate, assignability-first Set with compiled ref-accessor stores, the single
   canonical-nil-box encoding shared with X2, methodName as the getcallersp boundary, R10 unwrap
   with adapter-aware readers.
2. **Scope ruling on X1–X5**: recommendation — this chip lands them **in the same gated
   change-set** (errors cannot validate without them; the chip already owes every §5 gate these
   layers require; splitting to the coordinator serializes the same gates twice and leaves the
   chip's consumer undemonstrable). The chip's ledger lock extends to: golib
   (`builtin.cs` fallback, `ж.cs` equality/structural-nil, `GoReflect.cs`), the TypeGenerator
   `InterfaceTypeTemplate`, the converter's nil-conversion/comparison emission sites,
   `core/testing` host naming, and `testConversion.go`'s oracle matching.
3. **Increment split**: land the errors-validating increment 1 first, then design increment 2
   (`Call`, `MakeSlice`/`MakeMap`, Field/Index addressability) against testing/quick +
   encoding/binary differentials. The §2 contracts (ref-accessor store, single nil encoding) were
   *specifically* hardened by review so increment 2 extends rather than reworks them.

## 7. Gates (§5) for increment 1

golib + gen + converter all touched → **every** gate:

- Full behavioral suite; CNR byte-identical **except** the X2 emission sites (each individually
  justified; `NamedPointerReinterpret` golden re-baseline expected and its **Output** must stay
  `nilref` — the X2 correctness gate); 302-corpus reconvert + build; operational re-validation of
  all 32 validated packages (isolation-reconvert-diff to skip byte-identical).
- **⚠ Deploy-root hazard (chip-found):** behavioral tests that reference `core\reflect`
  (`DeepEqual`) resolve `$(go2csPath)` to the **deployed** `%GOPATH%\src\go2cs\` tree, not the
  repo — refresh the deployed `reflect`/`golib` (deploy-core) before trusting those results.
- **⚠ Baseline-mirror drift (chip-found):** `src/core/internal/reflectlite` is a **pre-bridge**
  copy (no `makeReflectValue`/`ValueOf` bridge, no `swapper_impl.cs`) and `src/core/internal/abi`
  has **no `type_impl.cs`** — yet the baseline stub `errors` is the modern reflectlite-using
  emission, so its `Is`/`As` would NRE at first real call in a behavioral context (nothing calls
  it today, which is why the suite is green). **Guard placement therefore:** golib-machinery
  guards (X1 assert-through-adapter, X2 typed-nil `%T`/equality, X3 method-set negative) exercise
  golib directly in the behavioral suite — no baseline reflect needed; reflect-surface coverage
  (Zero/Elem/Set round-trip, Q1a write-protection) rides the **errors pipeline suite** (18 TestAs
  cases exercise the preamble shape every re-validation). Whether to sync the baseline
  reflectlite/abi mirrors (a partial-promotion question under the 2026-07-01 no-promotion ruling)
  is **explicitly out of this chip's scope** — reported to the coordinator instead, alongside the
  pre-existing `DeepEqual` deploy-binding fragility.
- New behavioral guards: typed-nil-interface semantics (`%T`, `!= nil`,
  `TypeOf((*T)(nil)).Elem()`, cross-type/nil-box equality); dyn-assert-through-adapter (poser
  shape); value-vs-pointer method-set negative (§4 X3); reflect Zero/Elem/Set round-trip (TestAs
  preamble shape); methodless-pointer `%v` (§2.6); `sort.TestFind` subtest-key stability (X4).
- New guard for blessing condition Q1(a): a `Set` through a typed-nil-derived Value panics
  Go-style and the canonical singleton is unmodified after.
- **Commit shape (Q2 ruling):** one commit per X-fix, each carrying its own guard; the reflect
  §2 surface as its own commit(s); all on the chip branch `claude/optimistic-bassi-496625` —
  the coordinator ff-merges after the final all-ships-rise.
- Docs in the same change: `ConversionStrategies(-Reference).md` (X2 typed-nil boxing is a
  headline strategy change), `DESIGN-reflection-bridge.md` Phase-3 increment-1 record.

## 8. Adversarial-review ledger (§7, 2026-07-24)

Three independent reviewers (Go-semantics / blast-radius / generalization lenses). Findings and
disposition — all folded above:

| # | Finding | Disposition |
|---|---|---|
| C1 | `IsNull` misclassifies `ж<ж<T>>`-holding-null as nil → TestAs dies at case #0 | §2.1 structural-nil predicate (**critical**) |
| C2 | Set's nil row skipped assignability; typed-nil→interface dst must store non-nil eface | §2.2 rule 2 + interface row |
| C3 | valid-nil `Interface()` returned null → type loss one call from X2's fix | §2.4 single encoding |
| C4/G-F1 | `ж<T>.Value` is get-only ref — reflection SetValue store unimplementable; field/element parents need ref-routed accessors | §2.2 step 3 compiled ref-accessor contract |
| C5 | X2 empty-interface-only scope left adapter `Box=null` typed-nils: `errors.Is((*A)(nil),(*B)(nil))` wrongly true | §4 X2.2 adapter-ctor seeding |
| G-F2 | Dual nil encoding (Zero's null vs X2 singleton) split-brains gob/json round-trips | §2.4 single encoding |
| G-F3 | Named func types collapse under System.Type interning | §5 recorded limitation |
| B1 | X2 asymmetry flips `NamedPointerReinterpret` output (`nilref`→`other`); comparison operands are reference-equality on `object` | §4 X2.1 symmetric emission + named-type singletons + gate |
| B2 | X1×X3 interaction can violate Go's value-vs-pointer method set (both directions) | §4 X1 `{Box: not null}` + X3 conditional exposure + new negative guard |
| B-R10 | Adapter-kind flip exposes `Elem()`-invalid latent fault, currently masked | §2.6 adapter-aware readers + `%v` guard |
| B-X4/X5 | Host renumbering entangled with duplicate-name dedup; hex normalization can collapse deterministic names | §4 X4 branch-only-empty; §4 X5 two-phase matching |
| chip | nil-box vs null-reference `ж` equality; DeepEqual behavioral test binds the *deployed* reflect | §4 X2.3; §7 deploy-root hazard |

Reviewer verdicts on the surviving core: the 18-case TestAs walk closes under §2+X1+X3 (correct
by case analysis, not hope); X1-alone and X3-alone could not be refuted against the 457-test
corpus; R10's `%T`/`IsComparable`/csv-DeepEqual consumers verified stable; Call confirmed
buildable on the `boxed/addrBox/typ_/flag` representation without `flagMethod`.

---

# INCREMENT 2 — the call & construction half (design v2, 2026-07-24)

> **Status: IMPLEMENTED 2026-07-24 — BOTH consumers validate.** testing/quick **8/8** vs
> `go test -json` (zero skips, zero disclosures); encoding/binary **137 tests** (9
> disclosed-divergent: 8 signature-pinned alloc-profile asserts + their aggregating t.Run
> parent under the new deepest-first oracle rule; 43 disclosed-unsupported declarations
> excluded per the Example/Benchmark policy). Implementation notes vs the v2 design:
> - The **ruled managed-box Reinterpret model** (FINDING-managed-box-uintptr-lifetime.md,
>   user ruling 2026-07-24 option 2, delivered into this arc mid-increment) replaced the
>   planned hand-owned `toRType`: the converter now emits `p.Reinterpret<T,U>()` for
>   managed-source pointer reinterprets corpus-wide, and the GC-corruption class it fixes
>   (canonType's cached descriptor reading recycled memory) was ALSO this increment's
>   nondeterministic quick-failure wall — independently root-caused here, converged.
> - canonType interning keys widened to (sysType, dims) — the blessed cargo-only variant was
>   internally inconsistent (first-interned wrapper would answer Len() for all lengths of one
>   element type); equal-dims arrays stay identity-equal, the dims-knowledge split is the
>   recorded under-equal residual (no measured consumer crosses it). Flagged as a deviation.
> - Two prerequisite build blockers (gen named-complex operator set; blank-scalar foreach
>   discard shadowing) plus four demonstrated fidelity fixes landed as their own guarded
>   commits: new-witness enumeration for test-package runtime asserts (myStruct×Generator),
>   lifted-anon-struct dedupe (TestSizeStructCache), [GoLocalName] stamps (TestNoFixedSize),
>   any-slot func-literal result typing (TestFailure #3).
> - Guards: NamedNumericIncDec (complex operator set), RangeStatements (blank scalar range),
>   ReinterpretPointerLifetime (aliasing + lifetime under GC churn; delivered), and
>   LiftedLocalTypes (anon dedupe + GoLocalName + any-slot lambda typing in one golden);
>   quick/binary banked suites are the operational guards.
> - §5-table items now closed: Value.Call/MakeFunc-adjacent introspection (NumIn/In/NumOut/
>   Out/IsVariadic), MakeSlice/MakeMap/New/SetMapIndex/Set* family, Field(i)/Index(i)
>   addressability (+ Value.Slice, discovered required), unnamed↔named directlyAssignable
>   (TestPtrAlias demonstrated), map/chan/func Zero kinds. Remaining recorded residuals:
>   named-func-type identity under interning (gob), variadic Call/CallSlice (text/template),
>   SetMapIndex delete-on-invalid (json), unnamed array-param dims (fArray vacuity, accepted
>   by ruling), cross-function anonymous-struct identity, typed-nil map/chan inside `any`.
> - **Sweep-caught regressions of the witness pass, both fixed in-increment** (the
>   all-ships-rise gate doing exactly its job): production-declared types must not re-record
>   (bytes CS0111 — the tests project compiles production package_info beside
>   package_test_info); imported interfaces must not enumerate (container/heap CS8646 — the
>   generated adapter class name `{Type}ж{InterfaceSimpleName}` collides for same-named
>   interfaces from different packages). TWO new recorded residuals from the second: the
>   adapter-naming collision itself (a latent gen limitation any future cross-package
>   same-named-interface record would hit), and external-test (X_test) types asserting
>   against the package under test's interfaces.

> **Blessing record (user rulings, 2026-07-24, in-session):**
> 1. **v2 core model blessed as folded** (I2.2/I2.3 corrected by I2.R); `unsafe.Sizeof`
>    rerouting onto `GoSizeOf` DEFERRED with the divergence recorded.
> 2. **All four converter fidelity fixes blessed** (new([N]T) length; lifted anon-struct
>    identity dedupe; local-type name via NEW attribute; lambda result-type inference) — each
>    its own commit + guard.
> 3. **Alloc-assert disclosures pre-authorized CONDITIONALLY** — only if sole residue,
>    signature-pinned, exact list reported with re-measured numbers.
> 4. **fArray unnamed-param dims residual: accept + record** (no param-dims attribute; no
>    other demonstrated consumer).

> Chip session `chip-reflect-incr2` (branch `claude/chip-reflect-incr2`, base master `5fa7a0f21`).
> Designed against the two demonstrated consumers' MEASURED differentials (below), per the §6.1
> spawn rule and the §5 deferral table. Increment-1's contracts (`addrBox` aliasing, ref-routed
> `WritePointerSlot` stores, the single canonical-nil encoding, `TryMarshalAssignable`) are
> extended, not reworked — exactly what the v2 review hardened them for.

## I2.1 Ground truth — measured baseline differentials (fresh master converter, 2026-07-24)

**Both consumers were COMPILE-blocked before any reflect call ran** — two non-reflect defects,
fixed first at their right layers (each its own commit + behavioral guard, landed on the chip
branch; they were prerequisites for measuring the real reflect differential at all):

| Blocker | Layer | Fix (committed) |
|---|---|---|
| `type C complex64/128` emitted `<,<=,>,>=,%` + `IComparisonOperators` (CS0019 ×10; whole quick host failed) | go2cs-gen `InheritedType` templates | complex kind-gate, same shape as the complement/shift gate (`2e467a1e6`; guard: `NamedNumericIncDec` named-complex block) |
| `for range b.N { _ = Size(x) }` emitted `foreach (var _ in range(…))` — C# declares a real variable named `_`, shadowing the discard; body blank-assign is CS1656 (whole binary host failed) | converter `visitRangeStmt` | scalar-blank positions emit a marked temp `_ᴛ1`; tuple `_` (true discard) untouched (`9588f483a`; guard: `RangeStatements` blank int/chan ranges) |

**testing/quick, post-unblock (8 Test funcs): 3 pass vacuously / 5 fail**, two root causes:

| Tests | Symptom | Verified root cause |
|---|---|---|
| TestCheckProperty, TestInt64, TestNonZeroSliceAndMap | `function does not return one value` (quick's SetupError; TestInt64's range check then sees 0,0 because `Check` bailed before ever calling `f`) | `rtype.NumOut()` reads the never-populated `funcType` sub-descriptor — func-type introspection (`NumIn/In/NumOut/Out/IsVariadic`) is unimplemented on the bridge |
| TestCheckEqual, TestFailure | `panic: reflect.Value.Call: call of nil function` (auto `call()` at value.cs:381 reads `v.ptr`, never populated) | `Value.Call` is unimplemented on the bridge |

(TestEmptyStruct/TestRecursive/TestMutuallyRecursive "pass" only because they ignore `Check`'s
error — they exercise the full §I2.3 value-generation surface once Check works.)

**encoding/binary, post-unblock: 47 pass / 49 fail**, four clusters:

| Cluster | Tests | Verified root cause |
|---|---|---|
| `index out of range [0] with length 0` ×17, `slice bounds [:4] capacity 0` ×5 | all En/Decode/Read/Write walls, TestReadTruncated, TestUnexportedRead | `sizeof(t)` → `t.Size()` — `synthType` never stamps `Size_`, every scalar sizes 0, every buffer allocates empty |
| `*binary.TestNoFixedSize_Person` vs Go's `*binary.Person` ×3; `*binary.TestSizeStructCache_typeᴛ1` ×1 | TestNoFixedSize/*, TestSizeStructCache | converter LIFTS function-local types with a `<Func>_` prefix (+ temp markers); `GoTypeName` renders the lifted C# name, Go prints the source-local name |
| `Expected no allocations, got N` ×8 | TestSizeAllocs/*, TestAppendAllocs | `AllocsPerRun` over `Size(v any)` — the `any` boxing alone allocates on the CLR; Go's cached descriptor read is 0-alloc (disclosed-divergence class, bytes/strings precedent) |
| children infrastructure-error ×24 | TestSliceRoundTrip/* | `ValueOf(&[100]T{}).Elem().Index(i).SetUint/…` — Index-element addressability + Set* (this increment's surface); re-measure after §I2.3 |

## I2.2 Design — func introspection & dynamic call (quick's wall)

**The bridge's func Value boxes a C# delegate** (converted Go funcs pass as method groups →
natural delegate types; the CS8974 conversions in quick_test.cs confirm it). Everything derives
from the delegate type's `Invoke` signature — no `funcType` sub-descriptors, ever:

- `GoReflect.FuncShape(Type)` (golib, shared): `(inTypes[], outTypes[], isVariadic)` from
  `Invoke` — parameters → ins; return type → outs with the **multi-return rule**: `void` → 0,
  `ValueTuple<…>` → its arity/items (a converted Go multi-return is ALWAYS a ValueTuple, and a
  converted Go struct is never one — unambiguous), else 1. Variadic = `params` detection on the
  last parameter (not consumer-demonstrated; conservative).
- Hand-owned `rtype.{NumIn, In, NumOut, Out, IsVariadic}` over FuncShape;
  `In/Out(i) = toType(abi.synthType(shape[i]))` — canonical.
- Hand-owned `Value.Call`: nil delegate → Go's `"reflect.Value.Call: call of nil function"`
  panic; args = each `in[i].live` marshalled by the EXISTING `TryMarshalAssignable` against the
  Invoke parameter types (one assignability rule everywhere, §8 charter); invoke via
  `Delegate.DynamicInvoke`; **unwrap `TargetInvocationException`** and rethrow the inner
  exception via `ExceptionDispatchInfo` so an in-callee Go panic propagates untouched; results
  wrapped **with the STATIC out types** (`makeTypedValue(object? boxed, Type staticType)` — a
  nil interface/pointer/map result still yields a VALID Value of the out type, never the invalid
  zero Value). `CallSlice`: scoped panic naming its next consumer (nothing demonstrated).

## I2.3 Design — construction & write-back family (quick's generator + binary's decoder)

One shared zero-construction rule, then thin hand-owned entry points:

- `GoReflect.ZeroValueOf(Type)` (golib): the boxed Go zero — pointer → `CanonicalNilPointer`
  (X2 singleton, one nil encoding); interface/func/**map/chan** → `null` (the emitted C# nil
  slot value: `map<K,V> m = default!` IS null — `Value.IsNil` already answers true; the
  typed-nil-map-inside-`any` `%T` fidelity is NOT demonstrated by any consumer and stays
  recorded); string → `""`; slice → `default(slice<E>)`; everything else →
  `Activator.CreateInstance`. `reflect.Zero` reuses it (replacing its private switch + its
  map/chan/func `NotImplementedException` — quick's `sizedValue` probes
  `Zero(t).Interface().(Generator)` for EVERY generated type, so Zero must be total).
- Hand-owned `reflect.New(t)`: a fresh `ж<T>` box holding `ZeroValueOf(t)` (cached generic
  factory), wrapped as a Pointer-kind Value — increment-1 `Elem()` then already yields the
  addressable Value aliasing the box.
- Hand-owned `Value.{SetBool, SetInt, SetUint, SetFloat, SetComplex, SetString, SetZero}`:
  flag checks as in Set, then `GoReflect.CoerceToSlotValue(slotType, wide)` →
  `WritePointerSlot`. Coercion is Go-exact: SetInt/SetUint **truncate** to the slot's width
  (quick feeds full-range `randInt64` into every width); SetFloat/SetComplex narrow to
  float32/complex64; a NAMED wrapper slot coerces to the underlying primitive first, then
  constructs the wrapper via its generated single-argument constructor (cached per type; exact
  primitive match, so reflection binding is unambiguous). SetZero writes `ZeroValueOf(slot)`.
- Hand-owned `MakeSlice(t, len, cap)` / `MakeMap(t)`: golib `slice<E>`/`map<K,V>` construction
  via cached generic factories; results are non-addressable Values (Go), their ELEMENTS are
  addressable through the shared backing (below). `Value.SetMapIndex(k, v)`: store through the
  live golib map (delete-on-invalid is not consumer-demonstrated; scoped panic).
- **`Value.Field(i)` addressability** — the increment-1 ref-accessor contract extended: when
  `v.addrBox` is set, the field Value's `addrBox` is a **field-alias `ж<F>`** built from the
  parent box + a cached **`ValueSlot`-routed accessor** (DynamicMethod: `castclass ж<S>; call
  get_ValueSlot; ldflda field` — routing through `ValueSlot` is what makes NESTED parents
  (field-of-field, element parents) correct where `FieldRef<T>.Create`'s `m_val`-hardcoded IL
  is not; the accessor doubles as the ж equality-identity token so `&s.f == &s.f` holds).
  Unexported (Go lowercase) and blank fields carry `flagRO` → `CanSet()` false (binary's
  TestUnexportedRead skip path). Non-addressable structs keep the detached-copy read.
- **`Value.Index(i)` addressability**: slice kind → element-alias `ж<E>` over the live slice's
  shared backing (`ж(IArray, int)` — golib slices/arrays are `IArray<T>` with ref-returning
  indexers; a struct COPY of the slice shares the backing store, so the ref lands in the real
  storage) — always addressable, matching Go; array kind → same alias iff the array Value is
  itself addressable, else the detached read; string indexing stays scoped-out (no consumer).
- Hand-owned `Value.Slice(i, j)` (TestSliceRoundTrip's `src.Slice(0, src.Len())`): array/slice
  kind → a `slice<E>` view over the SAME backing store (golib slices share backing by
  construction), wrapped as a slice-kind Value; Go's addressability requirement for slicing an
  array (panic on unaddressable) is honored via the flag.

## I2.4 Design — descriptor size & carried array length (binary's wall)

`synthType` stamps `Size_` from a new `GoReflect.GoSizeOf(Type)`: exact Go/amd64 sizes for
scalars (bool/int8…int64/int/uint/uintptr/float/complex), string 16, slice 24, pointer/map/
chan/func/unsafe.Pointer 8, interface 16, struct → recursive field sum with Go alignment
rules, array → elem × length. The auto `rtype.Size()` reads `Size_` and just works.

**Array length is REQUIRED type-level knowledge for binary** — `sizeof(t)`'s struct walk does
`sizeof(t.Elem()) * t.Len()` for every array-typed FIELD (binary's core `Struct` has six) —
and the managed `array<T>` type does not carry it (the §5 recorded limitation, now landed on
by a real consumer). Design:

1. The synthetic descriptor gains a **carried length** (`partial struct Type { nint arrayLen }`),
   stamped from three sources: (a) a VALUE (`abi.TypeOf` reads the live `IArray.Length`);
   (b) a **converter-emitted dimension attribute on array-typed struct fields**
   (`[GoDim(4)]`-shape, golib-declared; dims nest for `[4][8]T`), read by `rtype.Field(i)`;
   (c) an existing descriptor's element chain.
2. `canonType` interning key widens to **(System.Type, arrayLen)** — `[4]byte` and `[8]byte`
   become DISTINCT canonical Types (identity-correct: `TypeOf(a [4]byte)` ==
   `structField([4]byte).Type`), fixing the length-collapse for every future consumer
   (json/gob) rather than special-casing binary. Non-array keys are unchanged (len 0), so
   fmtsort/csv identity behavior is untouched.
3. Hand-owned `rtype.Len()` reads the carried length (auto form walks an unsafe sub-descriptor).
   A length the system genuinely cannot know (an unnamed `[N]T` reached purely as a TYPE with
   no value, no field attribute — e.g. via `SliceOf`-style construction we don't have) stays 0
   and is the recorded residual limitation.
4. `reflect.New` / `ZeroValueOf` of an ARRAY kind size the fresh backing from the carried
   length (`new array<E>(len)`) — TestSliceRoundTrip's `dst := reflect.New(src.Type()).Elem()`
   must produce a length-100 array (its `dst.Len()`/`dst.Slice(0, …)` depend on it), and a
   zero-length backing would silently DeepEqual-fail rather than error.

## I2.5 Design — name fidelity (binary's error-string cluster)

- `rtype.Field(i).Name` maps the converter's blank-field renames (`_`, `__`, `___`, …) back to
  Go's `"_"` (binary's decoder skips on exactly `Name == "_"`). A REAL Go field named `__` is
  a documented exposure, same class as the marker-shaped-identifier ruling on master.
- **Function-local lifted types** print their Go-source name: the converter stamps the original
  local name on the lifted type (a `[GoType]` definition token, e.g. `local:Person`), and
  `GoReflect.GoQualifiedName` prefers it — `*binary.Person`, not `*binary.TestNoFixedSize_Person`.
  General (every `%T`/`Type.String()` of a local type, gob/json type registries later), small,
  converter+golib layers. (Includes the temp-marker shape `TestSizeStructCache_typeᴛ1`.)

## I2.6 Deferred / scoped-out of increment 2 (named consumers)

| Item | Why deferred | Next consumer |
|---|---|---|
| `CallSlice`, variadic `Call` | no demonstrated caller | text/template |
| `SetMapIndex` delete-on-invalid, `MapKeys` | no demonstrated caller | encoding/json |
| named-func-type identity under interning | unchanged from §5 | gob type registry |
| typed-nil map/chan/func inside `any` (`%T`) | Zero()'s null slot suffices for every measured test | encoding/json nil-map round-trip |
| unnamed↔named `directlyAssignable` refinement | NOT yet demonstrated — binary's measured failures never reach it; re-measure after I2.3/I2.4 land | binary named-slice cases if they surface, else gob |
| alloc-count asserts (TestSizeAllocs/TestAppendAllocs) | `any` boxing provably allocates on CLR; Go is 0-alloc by cached descriptor | disclosed-divergence manifest AFTER the real fixes land and they are the sole residue (bytes/strings precedent) |

## I2.R Adversarial-review ledger, round 1 (§7, 2026-07-24) — and the v2 corrections

Three independent reviewers (Go-semantics / blast-radius / generalization lenses), 40+ verified
findings. **Where this section conflicts with I2.2–I2.5 above, THIS section wins** (v2). The
full reports live in the session record; this table is the durable disposition.

**Corrections that change the model (v2):**

| # | Finding (reviewer-verified) | v2 disposition |
|---|---|---|
| R-1 | Named slice/map/array/chan/pointer types are WRAPPER structs/classes; `KindOf` reports Struct for all of them (and for raw `complex64`, and `num:nint`/`num:nuint` tokens are unmapped) — quick's entire alias table dies; `[GoType]` token strings must not be parsed | Classification becomes STRUCTURAL: `ISlice`/`IMap`/`IArray`/`IChannel`/`IPointer`+`INilPointer` implementation, `typeof(complex64)`, token map completed. New golib pair `TryUnwrapNamedContainer(Type)` / wrap-for-slot used uniformly by KindOf/ElementType/GoTypeName/GoSizeOf/Make*/Zero/Coerce; wrapper arms added to `Len/IsNil/Bytes/String/Elem/Index/SetMapIndex`; `Value.Complex()` read-half coercion (golib `complex64` cannot unbox to `Complex`) |
| R-2 | `Field/Index/MapIter.Key/Value` build Values from the DYNAMIC type; Go requires the STATIC slot type (interface-typed fields must report Kind Interface; a null `ж<T>` field must be a VALID nil Value, not the invalid zero Value — TestRecursive dies otherwise; `Set` derives dstType from the descriptor, so dynamic typing also corrupts assignability) | `makeTypedValue(live, staticType)` becomes the constructor for ALL slot-derived Values (Field/Index/MapIter/Call results); `boxed == null` + typ_ set = valid nil Value of the static kind. This also fixes a latent increment-1 defect |
| R-3 | `ZeroValueOf(System.Type)` is the wrong signature: golib `map<K,V>`/`channel<T>` are STRUCTS (`default`, never null — the I2.3 text was factually wrong); `Activator` on `slice`/`map` structs runs parameterless ctors that allocate NON-nil backings; arrays need dims the Type cannot carry; named-pointer wrapper classes have no parameterless ctor | `ZeroValueOf` takes the DESCRIPTOR: pointer → canonical nil box; interface/func → null; string → ""; slice/map/chan → cached `default(T)` factories (never Activator); array → `new array<E>(dims…)` with recursive element factories; named wrappers → wrap-for-slot |
| R-4 | The `(sysType, arrayLen)` canonType key creates UNDER-equal identity (type-only paths never stamp a length → two canonical Types for one Go type — the reversed-map-sort class from the other side); a single `nint` cannot express nested dims (`[4][8]byte` vs `[4][4]byte` collapse); `0` conflates "unknown" with legal `[0]T` | **canonType key stays `System.Type` alone — interning is NOT widened.** Array dims ride the descriptor as non-identity CARGO (a dims vector; null = unknown, distinct from `[0]T`), consumed by `rtype.Len()`/`Size()`/`New`/`Zero`/`Slice`. Type identity stays length-blind — the §5 recorded limitation stands (with `GoTypeName` rendering `[N]T` when dims are known) |
| R-5 | The converter dimension attribute is UNNECESSARY: array dims are already recoverable at runtime — field initializers (`= new(4)`, nested `new(128, () => new(4))`) compile into the generated parameterless ctor, so a cached zero instance of the declaring struct yields every field's real dims (named array wrappers likewise via `TargetTypeSize`) | I2.4 source (b) is replaced by the **zero-instance dims walk** (golib-side, cached per type) — zero converter emission, zero golden churn. Dim sources: live value; declaring-struct zero instance; live value behind `Elem()`/addressable Values (the TestSliceRoundTrip path — I2.1's cluster attribution was incomplete: it ALSO needs `Value.Slice` and a dims-correct `New`) |
| R-6 | Embedded (promoted) Go fields are `private readonly ж<T> ᏑʗName` box fields; named-struct wrappers hold fields behind `m_value` — `goStructFields` reports wrong name/kind/size/order for both, `StructFieldsComparable` is already wrong on embeds, and a single-`ldflda` accessor cannot reach them | The Go-field table becomes a PROJECTION: `(Go name, Go type, exported, access path)` per struct type, unwrapping promoted-embed boxes, `m_value` forwarding, blank (`_`,`__`,…→`_`) and Δ renames; the ref accessor is emitted from the multi-step path; `NumField/Field/rtype.Field/GoSizeOf/StructFieldsComparable` all ride it; companion fields (`boxed`, `addrBox`, `mapEnum`, `sysType`) are excluded by attribute, not name list (all three reviewers hit this) |
| R-7 | `flagRO` as specified is not sticky (Go propagates through Field/Index/Elem) and "lowercase first rune" misclassifies `Ꮡʗ`-prefixed embeds and `_` blanks; the fmt-panic attack FAILED (converted fmt guards with `CanInterface()`), but %v output shifts must be re-measured, not assumed | RO derivation moves onto the projected Go field; RO ORs the parent's flag in `Field/Index/Elem`. Guard: Write-succeeds/Read-panics on the same unexported-field struct (Go's real asymmetry — reads through RO are legal; I2.3's "skip path" wording was wrong) |
| R-8 | I2.5's `local:` token in the `[GoType]` def string makes the TypeGenerator THROW (structs match `"dyn"` by exact equality) — corpus-wide compile break | Local names ride a NEW golib attribute (`[GoTypeName("Person")]`-shape) the gen never parses; `GoQualifiedName` prefers it. Sequenced before any converter emission |
| R-9 | TestSizeStructCache is an IDENTITY defect, not naming: four textual `struct{ A Struct }` occurrences (ONE Go type) lift to four distinct C# types → four cache entries vs Go's one. No reflect-layer fix exists | Converter dedupes structurally-identical lifted anonymous struct types (per package); its own commit + guard |
| R-10 | `new([N]T)` emits `@new<array<T>>()` — the LENGTH is dropped (a runtime-correctness converter bug independent of reflection: `len(*p)` is 0) | Converter fix, own commit + guard |
| R-11 | quick's TestFailure asserts `func(int) int` vs `func(int) int32` are DIFFERENT types; the converter emits both lambdas as natural-typed `(nint x) => 0` → `Func<nint,int>` for both → same canonical Type → no SetupError | Converter emits Go-faithful lambda result types where natural-type inference would misinfer (explicit lambda return type / typed delegate in untyped contexts); own commit + guard. The general delegate-shape lossiness is recorded beside the §5 named-func limitation |
| R-12 | Variadic func VALUES are golib `Funcꓸꓸꓸ`/`Actionꓸꓸꓸ` with `params Span<T>` (C#13 params-collections): `ParamArrayAttribute` detection returns false, `KindOf(Span<T>)` = Struct, and DynamicInvoke cannot bind byref-like params (NotSupportedException, not the designed panic) | `FuncShape` detects the golib variadic families by open generic definition (closed set): `IsVariadic` true, tail param reported as `slice<T>`, `Value.Call` raises the scoped panic BEFORE DynamicInvoke |
| R-13 | `TryMarshalAssignable` has no `object`/`any` row (assignment into an `any` slot panics "not assignable"), `KindOf(object)` = Struct, and the coercion helper's wide-primitive shape cannot grow into Go's CONVERTIBILITY relation (json/gob `Convert`); wrapper ctor targets are golib STRUCTS (`uintptr`, `@string`, `complex64`), not primitives | `object` dst passes through (incl. null src); `KindOf(object)` = Interface; the coercion lands as `GoReflect.TryConvertTo(src, dstType)` — THE convertibility relation, ctor-parameter-typed for wrappers — with Set* as one caller |
| R-14 | Stamping `Size_` wakes dormant auto code OUTSIDE the consumers (`makeInt`/`makeFloat`/`makeComplex` via `Convert` now reach the nil `unsafe_New` stub; `isPaddedField` without `Offset` becomes newly wrong); zero callers among the 37; `GoSizeOf` also creates a SECOND size authority beside `unsafe.Sizeof` = `Marshal.SizeOf` (183 corpus sites) | Recorded forward-risks with named consumers (`Value.Convert` → text/template, json). `GoSizeOf` is THE golib Go-size rule (scalars exact; string/slice/iface/ptr headers exact; structs Go-aligned; `Align_`/`FieldAlign_` stamped); rerouting `unsafe.Sizeof` onto it is deferred with the divergence recorded — not silently left as two rules |
| R-15 | `Value.IsNil` has no slice arm; `ReadPointerSlot/WritePointerSlot` crash on named-pointer wrappers (non-generic — `GetGenericArguments()[0]` throws); `Index(i)` via a detached `v.live` read loses writes on lazily-backed named-array wrappers (the pallocBits class); `Value.Interface()` lacks `mustBeExported` | Slice-nilness arm (representational, `m_array is null`); slot access gains an `IPointer<T>`-resolved arm; addressable `Index(i)` routes through `ж.at<E>()` (which materializes lazy backings on the REAL storage); `Interface()` honors RO. Reader polish: Go-shaped panics replace silent zeros/unchecked casts |

**Verified-safe (attacks that failed, on record):** the ValueSlot-routed accessor composes through
field-of-element-of-slice chains with no copies; the accessor-as-identity-token preserves
`&s.f == &s.f`; SetInt/SetUint TRUNCATE in Go (never overflow-panic) — I2.3's rule confirmed
against Go 1.23 source; the ValueTuple multi-return rule held against the corpus; blank-field
mapping reproduces Go's skip INCLUDING size advance; `MakeSlice(t,0,0)`/`MakeMap` yield non-nil
values (TestNonZeroSliceAndMap); registry additions have ZERO re-validation exposure (all corpus
callers of the newly-owned surface live outside the 37 and are broken today); the two landed
build-blocker commits withstood direct attack; converted fmt cannot panic on RO (guards with
`CanInterface`).

**Residual fidelity gaps recorded (not blocking validation):** unnamed array-typed func
PARAMETERS have no dims source (`fArray`'s generated `[4]byte` is length-0; the CheckEqual row
still passes honestly under the test's own semantics — both sides see the same value — but the
generator fidelity gap is recorded; a converter param-dims attribute is the named fix if a
consumer lands on it); `Type.String()` of anonymous lifted structs prints the lifted name;
delegate-derived func-type identity remains structurally lossy beyond R-11's literal fix.

## I2.7 Decisions requested (§10)

(v2 — after the I2.R fold; the original v1 questions 2/3 are superseded by R-4/R-5/R-8.)

1. **Bless the v2 model** (I2.2/I2.3 as corrected by I2.R): structural container
   classification + wrapper arms; static-slot-typed Value construction everywhere
   (`makeTypedValue`); descriptor-taking `ZeroValueOf` with struct-container defaults;
   dims-as-descriptor-cargo with the zero-instance recovery and canonType interning UNCHANGED;
   the Go-field projection (embeds/wrappers/blanks) under Field/NumField/GoSizeOf; sticky
   flagRO; `TryConvertTo` as the single convertibility relation; FuncShape/DynamicInvoke Call
   with variadic-family detection; `GoSizeOf` as the golib size rule (unsafe.Sizeof rerouting
   deferred, divergence recorded).
2. **Bless the converter fidelity bundle** — four root-caused converter fixes the consumers
   demonstrated, each its own commit + behavioral guard: (a) `new([N]T)` length emission
   (R-10); (b) structurally-identical lifted anonymous struct types dedupe (R-9); (c) lifted
   local-type name stamp via a NEW golib attribute, never the `[GoType]` def slot (R-8);
   (d) Go-faithful lambda result types where C# natural-type inference misinfers (R-11).
3. **Alloc asserts**: pre-authorize disclosed-divergence manifest entries for binary's
   AllocsPerRun tests IF they are the sole residue after the v2 surface lands (signature-pinned,
   bytes/strings precedent) — final list reported with the re-measured validation numbers.
4. **fArray dims residual**: accept the recorded unnamed-array-parameter fidelity gap (the
   CheckEqual row passes honestly, generator sees a length-0 array) — or direct a converter
   param-dims attribute now. Recommendation: accept + record; the attribute has no other
   demonstrated consumer.
