# DESIGN — runtime duck-typing shells for NAMED interfaces

> **Status: COMPLETE — all five stages LANDED (2026-07-25). Blessed by the user; built as specified.** Produced 2026-07-24/25 by an
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
| **2** (≈2-3d) — **unblocks io/fs** — **LANDED 2026-07-25** | Tiered shells + attribute + binder + memo + builtin consumption; skip zero-method/generic/constraint interfaces. | Converter untouched ⇒ CNR byte-identical; 7 behavioral guards (incl. cross-assembly late-assert = the io/fs shape, X3 negative, false-positive-must-miss, unexported-interface); corpus build = the rendering-risk gate (five ≥10-method interfaces); **acceptance: io/fs 16/18 → 18/18, 37-sweep clean** |
| **3** (≈1-1.5d) — **LANDED 2026-07-25** | Migrate the dyn interfaces onto the tiered shape; delete the `MakeGenericMethod` path (`builtin.cs:1638/:1662`) — **closes the AOT hole that exists today**; PerfAot smoke benchmark. (Census correction: the corpus + behavioral goldens carry **92** `dyn` declarations in non-generated source, not 33 — the panel's figure counted production packages only.) | Full suite + corpus + sweep; perf gates (memoized assert ~30 ns; pointer tier ≤3 ns/call) |
| **4** — **LANDED 2026-07-25** | Recorders behind a default-off flag; reconvert. **Not inert (measured): 1540→1329 records, −335/+124** — the recorders currently *suppress* 124 demanded records. Preserve the Promoted(52)/ConstraintProxy(11) record paths — compile-time nominal, non-retirable (96% of records ARE retirable). | Inspect every changed package_info.cs; sweep clean twice; independently revertible |
| **5** — **LANDED 2026-07-25** | Delete the recorder machinery (~241 lines + 12 plumbing refs). `ImplementGenerator` STAYS (declared conversions are not heuristics; nominal adapters remain the 1.1 ns fast path). Docs in the same change. | Final sweep + docs verified against real goldens |

Total ≈1.5–2 weeks. Deferred successor recorded: `IDynamicInterfaceCastable` (71 ns/call, larger
architectural change) if profiling ever demands it.

### Stage 2 as built (2026-07-25) — deltas from §3 worth carrying forward

Everything in §3 was built as specified. Five things are worth recording because a later stage
depends on them:

1. **Dyn interfaces were NOT migrated.** §3 describes the END state (`[GoType]`/`[GoType("dyn")]`);
   the staged plan puts the 33 dyn interfaces in **Stage 3**, so Stage 2 emits shells for NAMED
   interfaces only and the dyn `ᴛAs` machinery is untouched. The consequence is deliberate but real:
   the new `InterfaceShellEmitter` and the old dyn block in `InterfaceTypeTemplate` are two renderers
   of the same idea — Stage 3 deletes the old one rather than merging them.
2. **The generic shell closes over the ELEMENT type (the pointee), not the box.** That is what makes
   *both* receiver forms bindable from one class, but it means a struct pointee is still a
   value-type instantiation, so the pointer tier is **AOT-graceful, not AOT-guaranteed** — the belt
   degrades it to the object shell rather than to a miss. The value-typed *dynamic value* case (the
   one that can never be rooted) is genuinely instantiation-free.
3. **Two defensive emission gates** beyond §3's list: an interface with a **ref-kind parameter** gets
   no shell at all (`MethodInfo.Parameters` carries the type only, so the member would be re-declared
   without the modifier — CS0535), and the object shell is skipped when any member cannot round-trip
   through `object` (a Go variadic tail is `params Span<T>`). Shells are always emitted `internal`.
4. **The binder must NOT construct through `ConstructorInfo.Invoke`** — measured 119.7 ns/assert vs
   71.9 ns with `ConstructorInvoker`. Tier costs on an identical trivial callee (Release, 50M
   iterations, 1.47 ns harness floor): **delegate 4.44 ns/call, reflective object 22.15 ns/call** —
   the panel's 2.78/21.9 reproduced. The memoized assert is **71.9 ns**, above the ~30 ns estimate;
   the remaining cost is two `(Type,Type)`-keyed dictionary lookups plus the allocation. An available
   follow-up is the `Cache<TInterface>` shape `Implements<T>` already uses (per-closed-generic static
   dictionary keyed on the value type alone, no tuple hashing) — **not** taken here because §3
   specifies the AdapterRegistry `(Type,Type)` key.
5. **The test host's isolated working directory had to change** for the last io/fs sub-case: it now
   carries the package's own directory name under a private parent, reproducing the shape `go test`
   provides (`os.DirFS("..")` + `*/glob.go` expects `fs/glob.go`). Unrelated to interfaces, but the
   shells are what exposed it.

Gates as run: CNR **byte-identical** (487 projects); full behavioral suite **PASS** (487/487
transpile + compile + target, 457 output-compared, 0 failed); fresh 305-package reconvert overlaid
and built — **302/302 compile clean, zero rendering fallout** (the ≥10-method interfaces, including
the 37-method one, all rendered); **io/fs 16/18 → 18/18**; banked subset re-validated at its banked
counts (errors 61, encoding/csv 71, hash/fnv 19, testing/quick 8, encoding/binary 137, bytes 81,
strings 68).

### Stage 3 as built (2026-07-25) — the AOT hole, measured shut

Everything in the §4 stage-3 row was built. What is worth carrying forward:

1. **The AOT hole was REAL, TOTAL and SILENT — and is now measured shut.** The claim was previously
   argued from ILC's rooting rules; it is now an A/B on a Native AOT binary
   (`PerfIfaceShell`'s program, published `PerfAot`, `IsDynamicCodeSupported = False`):

   | mechanism | value-typed assert | pointer-sourced assert | checksum | time (10M asserts) |
   |---|---|---|---|---|
   | pre-Stage-3 (`ᴛAs` + `MakeGenericMethod`) | **MISS** | **MISS** | **0 — WRONG** | 9,773 ms |
   | Stage 3 (tiered shells) | `Δrun_typeᴛObj` | `Δrun_typeᴛ1ᴛObj` | 8,000,000 — correct | 202 ms |

   So under AOT the old path did not merely lose the pointer tier: **every** anonymous-interface
   assert missed, and the program computed a wrong answer without raising anything (each iteration
   retried a failing close, which is the 48× time). This is the single strongest argument for the
   whole arc, and it was only obtainable by executing it.

2. **The `NotSupportedException` belt is OBSERVED FIRING, and it is the pointer tier that needs it.**
   On the JIT the pointer-sourced value binds the delegate shell (`Δrun_typeᴛ1<box>`); under AOT that
   instantiation is unavailable — the shell closes over the *pointee*, which is a struct — and the
   binder degrades it to the object shell (`Δrun_typeᴛ1ᴛObj`), tier name visible in both runs. The
   §3 "AOT-graceful, not AOT-guaranteed" wording is exactly right, and the graceful path is not
   theoretical. The value tier needed no belt in either build.

3. **Retiring the old renderer forced golib's three hand-written interfaces into the mechanism.**
   `error`, `fmt.Stringer` and `io.Reader` expose plain `As<T>` helpers that `TryTypeAssert` found by
   the *same* reflective probe as `ᴛAs`, so the probe could not be deleted without them. Each is now
   `[GoInterfaceShell]`-stamped over its existing `<I><T>` carrier (which always *was* the delegate
   shell), with `(in T)` → `(T)` on the constructor because the binder matches parameter types
   exactly. Object shell deliberately `null`: a reflective tier would have to reproduce those
   carriers' `%v`/`%T` formatting contract. **Follow-up recorded:** giving `error`/`fmt.Stringer`
   object shells (and with them a guaranteed AOT tier) means moving that formatting contract into
   the shell — worth doing, out of scope here. `sort.Interface` has a carrier but its `As` helpers
   are commented out, so it has no runtime duck-typing at all today; stamping it is a second,
   independent follow-up.

4. **The memoized-assert fast path was taken, and the saving is bigger than estimated.** A/B on the
   same binary, 10M asserts+calls, median of 3: per-interface `ShellCache<TInterface>` **278.6 ms**
   vs the `AdapterRegistry` `(Type,Type)` lookup **384.8 ms** — **≈53 ns saved per assert**. (Not
   directly comparable to Stage 2's 71.9 ns figure, which used a different harness; the *saving*
   alone exceeds what the ~40 ns target implied.) `AdapterRegistry` remains the authoritative record
   — the projection is filled by reading the registry back, never by forming a second decision.

5. **One `MakeGenericMethod` remains in `builtin`, and it is not this path.** The non-generic
   `TryTypeAssert(object, Type, out object)` entry — the reflection bridge's route into the assert
   machinery — still closes the generic definition over a run-time type. It is a *separate* AOT
   exposure belonging to the reflection-bridge arc, not to the shells.

6. **No new behavioral guard was added, deliberately.** Nothing observable changed: the dyn contract
   (nine existing projects) is green unchanged, CNR is byte-identical, and the corpus builds. The
   coverage this stage actually needed was *execution under Native AOT*, which no behavioral project
   provides — hence the benchmark. A census also confirmed the shell-eligibility gate excludes no dyn
   interface in the corpus or the behavioral goldens (none generic, none operator-constrained, and
   the three with empty bodies inherit non-empty method sets).

Gates as run: CNR **byte-identical** (490 behavioral projects, solution integrity OK); full
behavioral suite **PASS** (490/490 transpile + compile + target, 460 output-compared, 0 failed, 771 s);
full perf suite **PASS** (10/10, output verified identical across Go / JIT / AOT); fresh 305-package
reconvert (3 m 35 s) overlaid and built — **304/304 projects, 0 errors** (1,671 `.cs` overlaid, 23
hand-owned preserved, 301 `.csproj` rewritten); banked canaries re-validated at their banked counts —
**io/fs 18, errors 61, encoding/csv 71, bytes 81, testing/quick 8**.

### Stages 4 and 5 as built (2026-07-25) — the recorders are gone

Both stages landed as two independently-gated, independently-revertible commits on one branch: stage 4
put the recorders behind a default-off `-structural-implement-records` flag and rebaselined the record
set; stage 5 deleted the machinery and the flag. What is worth carrying forward:

1. **The panel's −335/+124 reproduced EXACTLY on the rebanked era.** Measured on a seeded 305-package
   reconvert: **1535 → 1324 records across 53 packages**, `Promoted` (50) and `ConstraintProxy` (11)
   preserved to the record. The rebank moved the base by five records and the delta by none.

2. **A THIRD structural producer was in scope, and the panel's row did not name it.**
   `recordTestPackageImplementers` (`-tests` only) enumerates every test-declared concrete against every
   local interface — no demanding site anywhere — and its motivating consumer, testing/quick's
   `reflect.Zero(t).Interface().(Generator)`, is precisely a run-time structural assert. It also shares
   `recordIfImplements`, so stage 5 could not have deleted that helper while it lived. Retired with the
   other two; **testing/quick re-validates 8/8 with its one record gone** — the arc's most direct
   operational proof.

3. **The dropped records split two ways, and the split is what makes the sweep safe.** Of 335 dropped,
   **73 RELOCATE** — the demanding cast site was always there; it referenced the *provider's* adapter
   only because the provider's structural record existed, so with that gone the consumer records the
   foreign pair itself and the emission changes shape (`io.SectionReaderжReader` →
   `io_SectionReaderжReader`; `(Scored)(Verdict)4` → `new CrossPkgLib_VerdictᴠScored(…)`). That accounts for
   the 117 of 124 ADDED records and for **all** the non-`package_info` churn. The other **262 are simply
   gone**, and a scan of all 1919 corpus `.cs` for composed adapter identifiers (711 distinct; positive
   control `io_SectionReaderжReader` fires) finds **261 of them named by no emitted C# at all**. The
   remaining 7 added records are base-interface pairs the interface-inheritance prune had been
   suppressing under a now-deleted derived record.

4. **The one referenced dropped adapter was a real signal, and it self-resolved.** `math/rand/v2`'s
   `PCGжSource` is named by banked `*_test.cs` — a demanded site inside the TEST closure, which a
   `-tests` run records for itself. Added as an eighth canary: 36 + 1 skip, its banked count, with
   `PCG→Source` kept and its three speculative siblings (ChaCha8/Rand/Zipf) dropped. Scanning the banked
   test sources, not just production, is what surfaced it.

5. **No new guard test was added, deliberately — the retirement made five EXISTING guards stronger.**
   `DerivedInterfaceStructuralProbe`, `OptionalInterfaceStructuralAssertion`,
   `InterfaceToInterfaceAssertion`, `AnonIfaceThroughPointerAdapter` and `IfaceToIfaceNarrow` now carry
   **zero** nominal records for the pairs they assert, so their output comparison IS the shell-resolution
   proof and fails if the shells stop resolving. A flag-shaped guard would have been throwaway.

6. **Stage 5 is provably emission-neutral.** The deletion (~495 lines: three recorders,
   `recordIfImplements`, `adapterCannotForward`, `stripPointerType`, the `structuralOnlyImplementations`
   plumbing, the adapter-name collision prune that existed only to arbitrate speculative pairs, and the
   flag) leaves CNR **byte-identical** and a full seeded reconvert **byte-identical to stage 4's tree**
   — no overlay was needed. `ImplementGenerator` is untouched.

7. **PRE-EXISTING bank drift found by the control, and excluded.** A flag-ON reconvert (converter
   behavior identical to master) still differs from the committed tree in six production files —
   `bytes/{buffer,reader}.cs`, `strings/{reader,replace}.cs`, `math/rand/v2/{pcg,rand}.cs` — all
   committed in their `-tests`-CLOSURE form (`Δio` alias, `global::go.math` qualification) rather than the
   `-stdlib` form, so the whole-corpus rebank did not level them. Restored, not swallowed. Orthogonal to
   this arc; owed to whoever owns the rebank.

   **Revisited by the r40 rebank (2026-08-04) — read this before re-deriving it.** The six files no
   longer rest on the `-tests` side: a seeded `-stdlib` reconvert now reproduces the committed tree
   exactly (four byte-identical; `bytes/buffer.cs` and `strings/replace.cs` differed only by that
   rebank's own deref-accessor drift). So the rebank had nothing to level here, and the corpus rests
   on the `-stdlib` form — the correct side for a corpus.
   **The ASYMMETRY ITSELF IS UNCHANGED**, and it is easy to misread the above as its disappearance:
   a `-tests` run still emits `using Δio = io_package;` where `-stdlib` emits `using io = io_package;`,
   so every validated-sweep run re-flips these files and they must still be RESTORED, never banked.
   What a rebank owner owes here is therefore not a one-off cleanup but the standing restore — until
   the two emissions agree on one alias for the same import in the same file. (Searching the committed
   tree for `Δio` will NOT find these six and is not evidence either way: the marker only exists
   between a `-tests` run and its restore.)

Gates as run, **twice** (once per stage): converter `go test ./...` ok; CNR **byte-identical** at stage 5
(stage 4: 12 changed + 1 golden, each classified); FULL behavioral suite **PASS — 491/491 transpile +
compile + target, 461 output-compared, 0 failed**; seeded 305-package reconvert with the path-precise
hand-owned gate clean (37 marked files: 14 `.cs.auto`, 23 not re-emitted, 0 clobbered); corpus
`go-src-converted.slnx` **304 projects, 0 errors**; pipeline canaries at banked counts — **io/fs 18,
errors 61, encoding/csv 71, hash/fnv 19, testing/quick 8, bytes 81, math/rand/v2 36+1 skip**.

## 5. Decision requested (user)

1. Bless the synthesis (tiered shells, attribute discovery, fail-soft, staged retirement)?
2. Stage sequencing OK (2 unblocks io/fs; 4/5 retire the recorders as separate, revertible
   landings)?

Coordinator recommendation: bless as specified. Stage 1 is already in flight as independent bug
fixes. Stage 2 is the arc's first real landing and carries the io/fs acceptance signal.
