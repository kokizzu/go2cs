# REVIEW — B1 box design (`claude/g-b1-box-design` @ `6815eba00`): REJECT, return for a second design increment

> Adversarial review, 2026-08-26, coordinator-commissioned. Verdict: **REJECT for redesign** —
> not because the idea is wrong: the measurement is real, the byte win is real and reproduced,
> the direction is right. It fails because §3 (the P-F5 resolution the design was commissioned
> to deliver) rests on two statements the cited code contradicts, and the blast radius is a ~3×
> undercount produced by exactly the census-vs-emission error the design's own rider R1 forbids.
> **§1 (the microbench) is BANKED AS ACCEPTED by coordinator ruling** — it discharges P-F2 for
> `Value`/`ValueSlot` and is not to be re-done.

## Axis 1 — the microbench: CLEAN (two amendments)

Reproduced on the coordinator i7-5820K, net10.0 (10.0.11 / SDK 10.0.400), probe verbatim, 7
isolated processes, 12 interleaved rounds. **35/35 cells ≤ 1.00**; the decision-bearing AOT
mixed cell (design: 1.01×) reads **0.74–0.82×** here. Bytes table reproduced exactly, every
row. V5-over-V3 confirmed (AOT `DerefOrNull`: V3 1.22–1.32× vs V5 0.64–0.72×). The design
states its ±3% band and flags the 1.01× honestly; the band is tighter than the parent §7's 5%.

- **1A** — the design's own AOT arm shows collapsed dynamic range (three structurally different
  dispatch shapes within 0.3% on four of five rows; this box separates them cleanly: V2 1.07×,
  V4 1.31×). Report per-cell dispersion, re-measure the AOT arm.
- **1B** — shape fidelity: V1's transcription is simpler than reality in two conservative ways
  (real `Value` gates on the five-term `IsNull` chain, `ж.cs:333`; real `IsNilStandardPointer`
  is three-term, `ж.cs:439`) — fine. But **§5's `ElemRefBox` was never benched in §5's proposed
  shape** (the probe holds `IArrayM`+`int`, not `(T[] m_backing, nint m_index)`), and the arc's
  largest allocation mass (nistec, netip) hangs on that path.
- **1C (CONFIRMED DEFECT)** — the mandated V2 was never built: the parent design (§4,
  `DESIGN-zh-box-reduction.md:465-467`) specifies kind-discriminator + switch **with an
  `object`-typed union storage slot**; the benched V2 has plain fields and no union slot, and
  the design then eliminates V2 for lacking exactly what the union slot provides.

## Axis 2 — kind-as-type (P-F5): CONFIRMED DEFECT, load-bearing

Site census confirmed (six `unsafe.Pointer` construction sites, sole subclass repo-wide). Both
conclusions drawn from it are contradicted by the cited code:

- **2A — `Value` is NOT virtual today and becomes virtual.** `ж.cs:245` is non-virtual;
  `Pointer` overrides only already-virtual members (`unsafe.cs:231,257,262`). §3's "nothing on
  the uintptr round-trip gains an indirection" is false for the one member that matters, at 875
  emitted conversion sites, and the bench never models the subclass.
- **2B — the `BaseType` equality arms break in BOTH directions.** `GoReflect.cs:187` (twin
  `GoReflect.TypeNaming.cs:162`) tests `t.BaseType == typeof(ж<uintptr>)` exactly. Under
  `Pointer : StandardBox<uintptr>`: false negative (every `unsafe.Pointer` reclassifies via the
  `INilPointer` net and stops naming itself in `%T`) and false positive (an ordinary `*uintptr`
  box's `BaseType` becomes `ж<uintptr>` and **every `*uintptr` reports `UnsafePointer`**).
  Banked instances: `fmt/scan.cs:1182`, `fmt/scan_test.cs:68`,
  `encoding/json/decode_test.cs:1461` (`PUintptr`, inside a 491-verdict row).
- **2C — the `.GetType()`-fed surface: 14+ breaking sites.** Feeders `GoReflect.cs:117`,
  `internal/abi/type_impl.cs:199-200`, `reflect/value_impl.cs:53`. Severe: (i)
  `FieldAccess.cs:157` silently flips `ReadPointerSlot`/`WritePointerSlot` to the
  via-interface arm — `reflect.Value.Set` through a `ж<ж<T>>`/`ж<slice>`/`ж<map>` holding null
  would begin panicking where it succeeds today (the exact case `ValueSlot`'s doc comment
  exists for, `ж.cs:281-297`); (ii) `TypeExtensions.GoMethodSets.cs:456` returns
  `isPointer=false` → `GoMethodSets.cs:321` excludes every pointer-receiver method —
  duck-typed interface assertion on `*X` fails corpus-wide. Full list: `GoReflect.cs:181`,
  `:371`(rescued), `FieldAccess.cs:157`, `MethodSets.cs:220`, `TypeNaming.cs:159/162/250`,
  `ValueMarshalling.cs:85/235/249`, `builtin.cs:2721`, `GoMethodSets.cs:456`,
  `ExtensionMethodRegistry.cs:262`, `reflect/value_impl.cs:308`.
- Non-defects, recorded: `Equals`/`GetHashCode`/`PointerOrderToken` are subclass-transparent;
  no reflective construction of the box exists (the CS0144 property holds); `ж.cs:876` already
  demonstrates the correct base-chain-walk fix shape.
- **2D — unstated cost: the kind split is a whole-class rewrite; 2 of ~14 kind-branching
  members were benched.** 93 non-comment lines read the four kind fields; `Equals`,
  `GetHashCode`, `PointerOrderToken`, `IsNull`, `PinnableStorage`, `IsPinnedAt`,
  `EnsureStableAddress`, `pinnedArrayData`, `Slice`, `ArrayRef`, `TryPinnedReinterpret` and the
  `uintptr`/`void*` operators are merged branch chains that must each go virtual (unmeasured)
  or take the eliminated V4 shape. `ж.Contracts.cs:180-182` reflects on `m_val`/`m_slot` by
  name — moving them breaks it at type-init.

## Axis 3 — NetShareAdd retention: RISK

The path traced end-to-end; `m_retainedSource` does satisfy the 2026-08-14 limit's retirement
text and the mirror-and-copy then applies. Three gaps: (i) **§4's "the address's validity
window is unchanged (it remains the pin's)" is false on this path — there is no pin**
(`EnsureStableAddress` pins reference-free storage only; `SHARE_INFO_2` carries references;
the `fixed` block ends immediately). The design must state: the wrapper copies from
`RetainedSource` and never uses the address — or an implementer reproduces the original AV.
(ii) The ratified `ManagedPointerTokens.RegisterPinned` provenance record (weak, validate-on-
read) is never compared with the new strong-reference record; which is authoritative is owed.
(iii) `TryPinnedReinterpret` — the other arm of the same fallback, which also mints a native
box — appears nowhere in the design; subsumed-or-duplicated is left open. Leak shape:
acceptable (only the reinterpret fallback populates the field).

## Axis 4 — blast radius: CONFIRMED DEFECT, ~3× undercount

`new ж<` = 344 occurrences (design correct, incl. "12 in four hand-owned files" exactly). But:
(1) **~693–740 target-typed `ж<T> x = new(…)` constructions are invisible to that search** and
hit the same constructor (emitted from `globalAddressOperations.go:190`) — true radius ≈
**1,084**; (2) **243 of the 344 live in committed `*_test.cs` across 43 banked packages**,
which regenerate via the per-package `-tests` pipeline, not the `-stdlib` batch — the "310
converter-regenerable" filing mis-scopes the work; (3) the 310+22+12 decomposition mixes lines
with occurrences; (4) OQ-1's "via the emission template (mechanical)" is unavailable —
`PointerPrefix` (`symbols.go:32`) renders BOTH declared and construction positions at 67 sites
across 21 converter files; the converter must first distinguish the roles.

**Missing mandated gates**: the aliasing-class seeded reconvert-and-BUILD (CLAUDE.md rule; §6
names only CNR); the five reflect-consumer canaries recomputed at gate time (gcimporter 583,
go/types 557, encoding/json 491, crypto/tls 400 — recompute, not 402 —, encoding/xml 386) —
given Axis 2 this is the gate that would actually fail; the `-p:GoTargetOS=linux` build (one
hand edit is a per-GOOS darwin file); `go generate .` if any `package_info.cs` moves. Benign
and worth stating: the position map is line-based, so same-line token renames do not move it;
W7/Gosched has no interaction.

## Axis 5 — missing

AOT trimming under `TrimMode=partial` (five generic types where there was one);
`ElemRefBox`'s `T[]` backing cannot represent `CanonicalElement`'s five arms (incl. null-Source
foreign `IArray`; and `Ꮡ(s,i)` on a native-backed slice already returns a native box,
`builtin.cs:1712`); **`AllocationCounter.Count()` in 6 ctors — a base+derived chain that counts
in both doubles every box, corrupting the instrument the acceptance set is measured with**;
the stale `TryPinnedReinterpret` comment about non-blittable box fields (the kind split removes
that reason — state whether pinnability analysis changes); mechanism B stays deferred ✓.

## Axis 6 — acceptance set: RISK

Nistec honestly "reduce-and-remeasure, not zero" — so B1 does NOT unblock the four want-zero
rows; the arc's headline yield reads accordingly. No exhibit carries a numeric target, and the
B1-done/C-remaining boundary is never stated.

## Amendments required for re-submission

1. Re-do §3: enumerate the `.GetType()`/`BaseType` surface (14+), specify the fix (base-chain
   walk per `ж.cs:876`; `unsafe.Pointer` needs a marker interface or explicit probe — BaseType
   equality is ambiguous under any walk); bench a `Pointer`-typed `Value` site.
2. Re-price the radius by EMISSION (target-typed `new()` ≈ 740; the `-tests` closure 243/43
   packages); re-scope OQ-1/OQ-2 against the 67 `PointerPrefix` sites.
3. Bench the full kind-dispatch surface (min: `PointerOrderToken`, `Equals`, `GetHashCode`).
4. Build the mandated union-slot V2 before eliminating the non-virtual alternative.
5. Bench §5's actual `(T[], nint)` shape; map the five `CanonicalElement` arms + the
   native-backed-slice case onto it.
6. Correct §4's pin sentence (wrapper copies from `RetainedSource`, never the address);
   reconcile with `ManagedPointerTokens` and `TryPinnedReinterpret`.
7. Correct §7.5: `IsNilStandardPointer` is not `m_isNull`-only (`ж.cs:439`); a zero-address
   native box answers true today and would answer false — `DerefOrNil` flips from safe shared
   slot to faulting ref.
8. Add to §6's gates: seeded reconvert-and-BUILD, `-p:GoTargetOS=linux`, the recomputed five
   reflect canaries, `go generate`; state expected exhibit movements as numbers.

Bench raw data: the review lane's scratch (`lane-b1rev-bench/`); read-only worktree left at
`.claude/worktrees/lane-b1rev`.
