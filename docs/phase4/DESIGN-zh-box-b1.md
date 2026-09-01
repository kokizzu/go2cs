# B1 — the box itself: per-kind representation (design increment 2)

**Status: DESIGN, increment 2.1 — RATIFIED WITH BINDING CORRECTIONS (the dated AMENDED verdict
in [`REVIEW-b1-box-design.md`](REVIEW-b1-box-design.md)); this revision FOLDS the eight-item
correction list, and §5.1 records the OQ-4 elemRef PRE-gate as GREEN — B2 implementation may
staff on this text. Nothing here is implemented.**

Increment 1 (`claude/g-b1-box-design` @ `6815eba00`) was REJECTED for redesign on 2026-08-26;
its §1 microbench **BANKED as accepted** — independently reproduced at 35/35 cells ≤ 1.00 with
the decision cell more favorable on second hardware
([`evidence-b1rev-bench.md`](evidence-b1rev-bench.md)) — and is not re-done here. This increment
answers the review's eight amendments plus 1A/1B. The two S0b riders remain binding: **R1** —
yield claims priced by EMISSION, never census counts; **R2** — every corpus measurement carries
the byte-identical baseline control (seeded A/B, control half reproducing the committed corpus
byte-for-byte).

## 1. Banked: the P-F2 discharge for `Value`/`ValueSlot`

The five-variant bench (probe: [`probes/b1-box-dispatch/`](probes/b1-box-dispatch/); reproduced
raws: [`evidence-b1rev-bench.md`](evidence-b1rev-bench.md)) selected **V5** — per-kind sealed
storage, virtual accessors, `m_isNull` as a non-virtual base field — at ≤ V1-current on every
row of both runtimes, and eliminated the plain-fields V2 and the kind-byte-downcast V4 on time.
Bytes: fieldRef of a 560 B pointee 672 → 48 as benched. **N2 corrects the LANDING figures**: the
field-ref and element-ref kinds take a pin today (`EnsureStableAddress` via the 875-site operator
surface pins any kind's storage), so both keep `m_pin` — **fieldRef 672 → 56 (−92 %), elemRef →
56 in its final §5 shape**; the probe's 48/40 were the pin-less models, and the win against V1's
112/672 survives untouched.

## 2. This increment's measurements — amendments 1A, 2A, 3, 4, 5 (one probe, both runtimes)

Probe: [`probes/b1-box-dispatch-i2/`](probes/b1-box-dispatch-i2/) — self-contained models with
the identity surface transcribed from `ж.cs`'s real chains (`PointerOrderToken`'s
`AllocationBase` math, `Equals`'s kind-pair chain with the token-identity rule, `GetHashCode`'s
canonical-storage hash). Protocol: 12 interleaved rounds per process, **4 isolated processes per
arm** (the review lane's protocol), every cell reported as **median [min–max]** — the 1A
dispersion the first increment's output lacked. Arms: V1-current, **V2u** (the parent-mandated
union-slot variant, §2.1), and **V5i2** (V5 with §5's real element shape and amendment-7's nil
contract). Representative process shown; cross-process median ranges quoted where a verdict
depends on them. JIT = CoreCLR 10.0.11 PGO-warmed; AOT = Native AOT 10.0.11; G-LAPTOP.

| workload · JIT | V1 median [min–max] | V2u | V5i2 | V2u/V1 | V5/V1 |
|:--|--:|--:|--:|--:|--:|
| std `Value` (rw) | 2.118 [2.10–2.37] | 2.823 [2.79–5.46] | 1.440 [1.42–1.48] | 1.33× | **0.68×** |
| std `DerefOrNull`¹ | 2.118 [2.09–2.15] | 1.436 [1.42–1.49] | 1.172 [1.16–1.86] | 0.68× | **0.55×** |
| fieldRef `Value` | 1.622 [1.61–2.54] | 2.786 [2.76–3.51] | 1.615 [1.61–2.07] | 1.72× | **1.00×** |
| mixed 90/8/1.5/.5 | 3.465 [3.26–3.61] | 2.885 [2.77–3.29] | 1.890 [1.77–2.02] | 0.83× | **0.55×** |
| native `Value` | 0.460 [0.46–0.48] | 1.861 [1.84–1.90] | 0.465 [0.46–0.48] | 4.04× | **1.01×** |
| token-std | 2.315 [2.30–2.40] | — | 1.154 [1.15–1.17] | | **0.50×** |
| token-elemRef | 4.413 [4.37–4.73] | — | 1.391 [1.38–1.53] | | **0.32×** |
| equals-fieldRef | 3.035 [3.01–3.35] | — | 2.092 [2.08–2.15] | | **0.69×** |
| hashcode-elemRef | 5.102 [5.08–5.21] | — | 2.696 [2.68–2.80] | | **0.53×** |
| elem-Value-managed | 3.059 [3.02–3.59] | — | 1.489 [1.46–1.53] | | **0.49×** |
| elem-Value-foreign | 3.015 [2.99–3.26] | — | 3.963 [3.93–4.13] | | 1.31× |
| ptrVal-subtyped | 0.463 [0.46–0.48] | — | 0.464 [0.46–0.49] | | **1.00×** |
| ptrVal-basetyped | 0.692 [0.69–0.73] | — | 0.660 [0.64–0.71] | | **0.95×** |

| workload · Native AOT | V1 | V2u | V5i2 | V2u/V1 | V5/V1 |
|:--|--:|--:|--:|--:|--:|
| std `Value` (rw) | 4.214 [4.15–4.33] | 4.638 [4.61–4.69] | 3.713 [3.68–3.77] | 1.10× | **0.88×** |
| std `DerefOrNull`¹ | 3.933 [3.90–4.01] | 2.773 [2.75–2.86] | 1.852 [1.84–1.91] | 0.70× | **0.47×** |
| fieldRef `Value` | 2.310 [2.30–2.45] | 2.548 [2.53–2.58] | 1.852 [1.84–1.91] | 1.10× | **0.80×** |
| mixed 90/8/1.5/.5² | 4.370 [3.41–4.44] | 4.851 [4.65–5.09] | 4.175 [4.00–4.29] | 1.11× | **0.96×** |
| native `Value` | 1.738 [1.72–1.76] | 1.852 [1.85–1.90] | 1.388 [1.38–1.40] | 1.07× | **0.80×** |
| token-std | 5.325 [5.27–5.42] | — | 3.265 [3.22–3.32] | | **0.61×** |
| token-elemRef | 9.231 [9.16–9.31] | — | 3.474 [3.44–3.56] | | **0.38×** |
| equals-fieldRef | 3.947 [3.90–4.02] | — | 2.762 [2.75–2.82] | | **0.70×** |
| hashcode-elemRef | 11.314 [11.2–13.8] | — | 5.542 [5.50–5.78] | | **0.49×** |
| elem-Value-managed³ | 3.041 [2.99–3.18] | — | 3.482 [3.45–3.57] | | 1.15× |
| elem-Value-foreign³ | 2.779 [2.76–2.88] | — | 4.887 [4.82–4.96] | | 1.76× |
| ptrVal-subtyped⁴ | 1.731 [1.72–1.77] | — | 1.872 [1.84–1.89] | | 1.08× |
| ptrVal-basetyped⁴ | 1.742 [1.72–1.85] | — | 1.867 [1.84–1.96] | | 1.07× |

¹ This harness's V1 `DerefOrNull` routes through `Value` (with the nil gate), a transcription
deviation the banked probe did not make — cite the BANKED 0.56–0.67× for this row, not these.
² AOT mixed, corrected to its own raws: the four processes read V5/V1 at **1.10 / 1.16 / 0.96 /
0.94** — and the spread is V1's, not V5's: V5's median is stable at 4.09–4.21 while V1's is
bimodal at 3.63–4.40 (its per-round minima sit near 3.4 in every process), so the ratio tracks
which mode V1's median lands in. The review lane's second-hardware reading for this cell is
0.74–0.82× — the cleaner cross-check, and the basis on which the claim was banked. ³ the elem rows are the ISINST-form record — §5.1 re-benches the FINAL null-test shape and INVERTS both AOT regressions (managed 0.70×, foreign 0.82×). ⁴ priced in §2.3, band exceedance stated.
**Dispersion (1A): the three shapes now separate cleanly on AOT** (std-Value V1 4.21 / V2u 4.64
/ V5 3.71 — 10–12 % apart, vs the first increment's 0.3 % collapse), and every cell carries its
spread.

### 2.1 Amendment 4 discharged — the mandated union-slot V2, built and eliminated on three axes

The parent design's V2 (`DESIGN-zh-box-reduction.md` §4: kind byte + switch + **`object`-typed
union storage slot**) is now built faithfully: the union holds the standard kind's `T[1]` slot,
the field-ref kind's payload object `{source, accessor, token}`, the element-ref kind's
canonical storage, the native kind's retained source. What the construction itself proves: a
single class can shed the dead inline `m_val` ONLY by moving storage behind the union —
which for the standard-managed and field-ref kinds means **one more allocation per box**. The
measurement then closes all three doors:

| axis | V2u result |
|:--|:--|
| time | JIT: std 1.33×, fieldRef 1.72×, native 4.04×; AOT: 1.06–1.32× — above V1 on most rows, above V5 on all |
| bytes | 88/112/96/56/56 across the five census kinds — worse than V5i2's 80/72/48/40/48 on every row |
| **counts** | **+1 object per standard-managed box and per field-ref box** — a direct violation of §4's count-neutrality-except-the-element-ref-row, measured, not argued |

The non-virtual alternative is therefore eliminated **in the parent's own mandated shape**, on
the axis the parent cared most about (counts), not merely on time.

### 2.2 Amendment 3 discharged — the identity surface halves under per-kind dispatch

`PointerOrderToken`, `Equals`, `GetHashCode` — virtual today with kind-branch-chain bodies —
were the "2 of ~14 members benched" gap. Measured: per-kind overrides run **0.32–0.69× (JIT)**
and **0.38–0.70× (AOT)** of the transcribed chains on token/equals/hashcode workloads, with the
element-ref rows (construction-time canonicalization replacing per-call `CanonicalElement`)
the largest wins: token-elemRef 0.32×/0.38×, hashcode-elemRef 0.53×/0.49×. The map-keyed
consumers this surface serves — the address-keyed runtime semaphores, fmtsort, cycle detection —
sit on exactly these ops.

**The remaining kind-branching members, per-member (N1 — the increment-2 "branches deleted"
sentence was wrong for most of these and is withdrawn; each row is the honest disposition, read
from the cited body):**

| member | today | disposition | cost class |
|:--|:--|:--|:--|
| `PinnedBuffer` (`ж.cs:445-475`) | kind-branch chain (std/fieldRef/elem size sources) | **goes virtual per-kind** | the §2.2-measured class: branch chain → tiny body, favorable |
| `PinnableStorage` (`:1409-1428`) | kind-branch chain; `ж.Contracts.cs:245-252` says "implemented there per box kind" verbatim | **goes virtual per-kind** (the interface default stays) | same |
| `EnsureStableAddress` (`:1399-1406`) | non-virtual; reads `PinnableStorage`, writes `m_pin` | stays a base non-virtual helper over the now-virtual `PinnableStorage` — **which is why every pinnable kind keeps `m_pin` (N2)** | +1 virtual call on the address-take path |
| `uintptr`/`void*` operators (`:1266-1340`) | static, with inline kind tests (native early-out, nil, array-data, pin-then-fix) | static operators stay; their inline kind tests become **virtual probe calls** (the native early-out reads a per-kind member instead of a field) | +1–2 virtual calls at the syscall boundary — the same path 2A priced; gated by the sweeps + P-F4 |
| `IsPinnedAt` (`:1386-1397`) | reads `m_pin`/`m_nativeAddr`/`ValueSlot` | **goes virtual per-kind** (each kind consults its own pin) | provenance validate-on-read path, not hot |
| `ArrayRef` (`:477`), `TryGetElementStorage` (`:497`), `TryGetElementWindow` (`:533`) | `m_arrayIndexRef`-gated, base-resident | **virtual with base default (null/false), `ElemRefBox` overrides** — call sites are `ж<T>`-typed, so this **adds** dispatch where a field test answers today | added dispatch on the `unsafe.Slice`/element-window paths; honest cost, bounded by those paths' own rarity and the perf gates |
| `TryPinnedReinterpret` (`:1485`) | `m_arrayIndexRef`-gated | same pattern — virtual, `ElemRefBox` override | rare (reinterpret fallback) |
| `IsNull` (`:333`, five-term) | std-only value-peek behind guards | **virtual**: `StandardBox` keeps the peek (`s_valueCanBeNull && HeldValueIsNull`), the other kinds answer `m_isNull` alone — the current formula's own value on them | branch chain → tiny bodies, favorable |
| `ReferentObject` (`:1056`, already virtual) | kind-branch body | per-kind overrides | favorable |
| `pinnedArrayData` | std-kind fixed-array pinning | moves into `StandardBox`, branches genuinely deleted | the one member the withdrawn sentence was true of |

The net statement replacing the withdrawn one: the split converts ~ten branch-chain members into
per-kind bodies of the class §2.2 measured favorable, moves one into `StandardBox` outright, and
**adds** dispatch at two surfaces — the element-window family and the address-take operators —
both priced above and covered by the §6 gates. The first review's Axis-2D is answered member by
member, not by category.

### 2.3 Amendment 2A discharged — the `Pointer`-typed `Value` site, measured

The review's core catch: `Value` is **non-virtual today** (`ж.cs:245`), so the redesign makes it
virtual for the first time at `unsafe.Pointer`'s 875 emitted conversion sites — unmeasured in
increment 1. Measured now, on a model `Pointer` subclassing the (unsealed) standard kind, at
both site shapes: **JIT 1.00× subtype-typed / 0.95× base-typed** (PGO devirtualizes the sealed
leaf); **AOT +6–8 %** on both (1.06–1.08×, non-overlapping spreads), on a 1.7 ns op. **Stated
plainly: this reading EXCEEDS both this design's ±3 % parity band and the parent's P-F4 rule —
it is a real, measured regression on that op, not noise — and §7.2's sealed `Value` override on
`Pointer` is the PRE-COMMITTED P-F4 remedy for it**, not a contingency: if any §6 gate row that
exercises the `unsafe.Pointer` surface regresses past P-F4's threshold, the override lands as
part of B2, no new ruling required. The cost rides only `unsafe.Pointer`-typed dereferences
(875 conversion sites), and the syscall-heavy sweep rows plus the perf suite are the tripwire.

## 3. The landing shape, and the `.GetType()`/`BaseType` surface — amendment 1

The class shape is increment 1's, with amendment 7's contract added:

```
public abstract class ж<T> : IPointer<T>, IEquatable<ж<T>>, INilPointer
    m_isNull                      non-virtual base field; per-kind ctor contract (§3.2)
    abstract Value / ValueSlot / PointerOrderToken / Equals / GetHashCode
    of()/at() minting, operators, ToString — public surface unchanged
public class StandardBox<T> : ж<T>       UNSEALED (unsafe.Pointer derives)
    m_val; m_slot; m_pin; the standard-kind machinery of §2.2, kind branches deleted
public sealed class FieldRefBox<T> : ж<T>     m_source; m_accessor; m_token; m_pin (N2)
public sealed class ElemRefBox<T>  : ж<T>     §5's shape; m_pin (N2)
public sealed class NativeBox<T>   : ж<T>     m_nativeAddr; m_pin; m_retainedSource (§4)
public sealed class Pointer : StandardBox<uintptr>   (in unsafe; implements IUnsafePointer)
```

### 3.1 The enumerated surface and its fixes

Every site that asks a runtime or declared type "are you a ж box?" or "are you unsafe.Pointer?",
with its fate under the split. **Fix W** = the shared base-chain walk, one helper modeled on the
already-correct `ж.cs:876` (`TryЖPointee(Type, out Type)` — walks `BaseType` for the
`ж<>` generic definition). **Fix M** = the marker interface `IUnsafePointer` (empty, owned by
golib, implemented by `unsafe.Pointer`), replacing `BaseType == typeof(ж<uintptr>)` — which is
ambiguous under ANY walk, because an ordinary `StandardBox<uintptr>` also carries `ж<uintptr>`
in its chain (the review's both-directions break, with `fmt/scan.cs:1182` and
`encoding/json`'s `PUintptr` inside a 491-verdict row as the banked instances).

| site | today | fate | fix |
|:--|:--|:--|:--|
| `GoReflect.cs:187` (KindOf) | `gd == ж<>` on runtime type, then `BaseType == ж<uintptr>` | both break | **M first, then W** — order is load-bearing: `Pointer` has `ж<>` in its chain, so the Pointer probe must precede the box walk |
| `GoReflect.TypeNaming.cs:159/162` | same pair (`%T` naming) | both break | **M first, then W** (same order) |
| `GoReflect.TypeNaming.cs:250` | `gd == ж<>` in the named-wrapper probe | a `StandardBox<X>` falls past it and reports a Go name it does not have (the asn1 SET/SEQUENCE class) | **W** |
| `GoReflect.FieldAccess.cs:157` | `gd == ж<>` → slot path | **severe**: silently flips to via-interface, `reflect.Value.Set` through `ж<ж<T>>` holding null starts panicking (the `ValueSlot` doc's own case) | **W**, before the `IPointer<>` probe |
| `GoReflect.MethodSets.cs:220` | runtime `GetType()` test → pointee copy | **severe**: value-receiver-through-`*X` copy stops firing | **W** |
| `TypeExtensions.GoMethodSets.cs:456` (`ResolveReceiverElement`) | mixed feeds (declared params hold; runtime values break) | pointer-receiver exclusion at `:321` mis-answers on runtime feeds | **W** (safe for both feeds) |
| `TypeExtensions.ExtensionMethodRegistry.cs:262` | equality + `IsAssignableFrom` arms | generic arm breaks | **W**; the `IsAssignableFrom` arms already subsume subclasses |
| `GoReflect.ValueMarshalling.cs:85` (canonical nil) | `gd == ж<>` → static `NilBox` | breaks | **W**, then read `NilBox` off the walked base type (statics on the abstract base remain valid) |
| `GoReflect.ValueMarshalling.cs:235/249` | `GetType() == dstType` exact equality | runtime `StandardBox<X>` ≠ declared `ж<X>` | `IsAssignableFrom` subsumption **plus the M-guard** (N5): `unsafe.Pointer` must NOT marshal into a `ж<uintptr>` destination — an admission plain subsumption would make, and one that is wrong INDEPENDENTLY of the split (the address-identity type is not an ordinary `*uintptr`) — so these sites EXCLUDE `IUnsafePointer` exactly as the naming sites include it |
| `builtin.cs:2721` (printed names) | `gd == ж<>` | breaks | **W** |
| `reflect/value_impl.cs:308` | `addrBox.GetType()` test | breaks | **W** |
| `GoReflect.cs:297`, `AdapterBinder.cs:188` | tests on DECLARED parameter types | **hold** — emitted signatures stay `ж<T>` | none (recorded) |
| `GoReflect.cs:371` (`ElementType`) | `gd == ж<>`, then container-interface fallback | **rescued** — a subclass instance answers through the inherited `IPointer<T>` arm | verify-only |
| `GoReflect.cs:117` (`GoDynamicTypeOf`) | returns `value.GetType()` | not itself a test — the feeder; consumers above carry the fixes | none |
| `abi/type_impl.cs:199-200`, `reflect/value_impl.cs:53` | via `GoDynamicTypeOf`+`KindOf` | fixed by KindOf's fix | (inherits) |
| `ж.Contracts.cs:180-187` | `GetField("m_val"/"m_slot")` on `ж<T>` — **breaks at type-init** (null `FieldInfo!`) | fields move | IL builder targets `StandardBox<T>`'s fields; the accessor it builds serves standard boxes, which is the only kind whose storage it ever addressed |
| `AllocationCounter.Count()` — 8 calls in the ctors | base+derived would **double-count every box**, corrupting the instrument §6 measures with | counting moves to **leaf ctors only**, base ctor charges nothing; charges unchanged: standard-managed 1, standard-unmanaged 2, fieldRef 1, elemRef 1, native 1, nil-box 1 — asserted by the counter's existing exact-charge unit tests |

The non-defects the review recorded stand: no reflective construction of the box exists (the
CS0144 abstract-base property holds), and `Equals`/`GetHashCode`/`PointerOrderToken` were
already virtual — §2.2 now measures them.

### 3.2 Amendment 7 — the nil contract, corrected to the real predicate

`IsNilStandardPointer` is **three-term** today (`ж.cs:439`): `m_structFieldRef is null &&
m_arrayIndexRef is null && m_isNull` — and a **zero-address native box answers TRUE** (nothing
in the predicate consults `m_nativeAddr`; `unsafe.Pointer`'s ctor marks `value == 0` nil), which
is what routes `DerefOrNil` to the safe throwaway slot instead of a faulting zero-address ref.
The base-field form reproduces this **by per-kind construction contract**, not by narrowing:
`FieldRefBox`/`ElemRefBox` never set `m_isNull` (their ctors take no nil — exactly the two terms
the predicate excludes); `StandardBox` sets it from its nil ctor; **`NativeBox` sets it when its
address is zero** (mirroring `Pointer(uintptr)`'s `value == 0` today, and generalizing it to
every native mint). `base.IsNilStandardPointer => m_isNull` is then EQUIVALENT to the three-term
predicate on every constructible instance, and `DerefOrNil`'s zero-address behavior is
unchanged. The probe's `W5Native` implements this contract, so §2's native rows measured it.

## 4. Source retention (NetShareAdd) — amendment 6

Increment 1's sentence "the address's validity window is unchanged (it remains the pin's)" was
**false on this path and is withdrawn**: the non-aliasing fallback takes `&p` inside a `fixed`
block that ends immediately, `EnsureStableAddress` pins reference-free storage only, and
`SHARE_INFO_2` carries references — **there is no pin**. The corrected contract:

- **The hand-owned wrapper copies from `RetainedSource` and never uses the address.** The
  recovery read is `(box as NativeBox<byte>)?.RetainedSource as ж<SHARE_INFO_2>` followed by the
  established field-for-field blittable-mirror copy; the raw address in such a box remains what
  it always was — wrong-but-contained, per the address route's own documentation. An implementer
  who dereferences instead of copying reproduces the original AV; this sentence is the
  implementation's contract, stated where the reviewer demanded it.
- **Reconciliation with `ManagedPointerTokens.RegisterPinned`**: the two records answer
  different questions and both stay. The weak table resolves a *uintptr* back to a live box
  (validate-on-read, ABA-closed, population-bounded) and remains the authority for address
  round-trips; `m_retainedSource` is a *strong, typed* field on the specific box the fallback
  minted, and is the authority for **struct recovery at a hand-owned boundary** — it must be
  strong precisely because the source may have no pin and no other liveness. Where both could
  answer (a fallback box later round-tripped through uintptr), the field is authoritative for
  recovery; the table never serves recovery, only resolution.
- **`TryPinnedReinterpret` — subsumed, uniformly.** The OTHER arm of the same fallback (the
  array-element reinterpret that CAN pin) also mints a native box; both arms populate
  `m_retainedSource`, so the retention rule is "a native box minted from managed storage retains
  its source", with kernel-returned native boxes carrying null. Its stale comment about
  non-blittable box FIELDS is rewritten in B2 (the kind split removes that reason);
  **pinnability analysis itself does not change** — `PinnedBuffer.PinOnly` still gates on the
  backing's element type, and nothing in the split alters which storages are pinnable.

## 5. The element-ref kind — amendment 5 and N3, resolved and pre-gate benched

**N3 first, because it constrains the shape.** Increment 2 promoted `CanonicalElement` from
identity reduction to deref storage — an obligation two of its five arms cannot meet: the
`PinnedBuffer` arm's `PinnedTarget` is `object?` (nullable, and not necessarily the deref
storage), and the `default` arm's `array.Source` **may materialize a copy** (the `ISlice` arm's
own comment: "`Source` cannot serve, it copies") — a deref through either would silently split
storage, trading the `&StringData` equality contract against correctness. The resolution is
**two slots, collapsed where equivalence is PROVEN**:

```
ElemRefBox<T>:  T[]? m_backing;  IArray? m_foreign;  nint m_index;  object? m_pin (N2)
```

| arm (`ж.cs CanonicalElement`) | lands as | why |
|:--|:--|:--|
| `slice<T>` with backing | **fast**: (`m_array`, `Low`+i) | the slice indexer IS `m_array[Low+i]` — deref-equivalence by definition |
| named `ISlice<T>` view | **fast**: unwrapped shared header | the view's indexer delegates to the same window — by definition |
| `array<T>` alias window | **fast**: (`Source`, `Low`+i) | `Alias` shares storage — by definition |
| `PinnedBuffer` arm | **foreign** — never fast | `PinnedTarget` is not a proven deref storage |
| `default` (null-Source slice, foreign `IArray`) | **foreign** — never fast | `Source` may copy |

The **fast arm holds a pair that is canonical AND deref-equivalent by the indexer definitions**
— one storage serves `Value`, `PointerOrderToken`, `Equals`, `GetHashCode`. The **foreign arm
derefs through the ORIGINAL `IArray` exactly as today** and canonicalizes identity **per-call
exactly as today** — its cost and its correctness are both unchanged, so nothing is traded
silently: the deref-equivalence obligation is not assumed anywhere it is not proven.

**The pre-gate bench (OQ-4, granted as a B2 PRE-gate) ran on this final shape** — pin field
present, null-test dispatch — 4 isolated processes per runtime
([`probes/b1-box-dispatch-i2/output-pregate.txt`](probes/b1-box-dispatch-i2/output-pregate.txt)):
results recorded in §5.1 below. The increment-2 isinst numbers (managed 0.49× JIT / 1.15× AOT,
foreign 1.31×/1.76×) stand as that shape's record.

- **The native-backed slice route is untouched**: `Ꮡ(s, i)` on a native-backed `slice<T>`
  already mints an address box (`builtin.cs:1712`) and continues to — a `NativeBox` under the
  split, never an `ElemRefBox`; its counter note reads unchanged.
- **N6 — the mechanism of the count claim, carried from the parent (§4 item 4):** the
  `Ꮡ(slice<T> s, nint i)` / `Ꮡ(array<T> a, nint i)` **overloads** construct the fast arm
  directly from the typed header — no `IArray<T>` interface boxing is ever minted at the call
  site — which is where the **−1 object per managed-backed `&s[i]`/`&arr[i]` site** comes from.
  Foreign-`IArray` and native-backed sites keep today's counts.

### 5.1 Pre-gate results — GREEN, with the isinst regressions inverted

4 isolated processes per runtime, 12 interleaved rounds each, median [min–max]; FINAL vs the
current shape (ratios per process, all four listed):

| workload | JIT | AOT |
|:--|:--|:--|
| elem-Value-managed | **0.24× / 0.24× / 0.24× / 0.24×** | **0.70× / 0.70× / 0.70× / 0.70×** |
| elem-Value-foreign | 0.59× / 0.58× / 0.62× / 0.61× | 0.81× / 0.82× / 0.82× / 0.82× |
| elem-token-managed | 0.50× / 0.50× / 0.50× / 0.50× | 0.47× / 0.47× / 0.47× / 0.47× |
| bytes/box | **56** (all processes — N2's corrected figure, measured) | 56 |

The null-test refinement did not merely bound the isinst form's AOT regressions (managed 1.15×,
foreign 1.76×) — it **inverted them**: the final shape is faster than current on every row of
both runtimes, 24/24 cells ≤ 0.82×. The foreign arm improves because the per-call interface
type test is replaced by a null test plus an unchecked cast. B2's staffing condition ("the
elemRef pre-gate bench is green") is met by this record.

The identity ops on this kind halve (§2.2) because canonicalization happens once at the mint
instead of per-comparison — the parent design's accepted trade, measured; the foreign arm keeps
per-call canonicalization, so the trade never applies where equivalence is unproven.

## 6. Blast radius and gates — amendments 2 and 8

**The radius, priced by EMISSION** (re-censused on this tree at master `346bde800`; the review's
independent counts in parentheses):

| class | count | regenerates via |
|:--|--:|:--|
| `new ж<T>(…)` explicit | **344** (344) | emission change + regen |
| `ж<T> x = new(…)` target-typed — `globalAddressOperations.go:190` and siblings | **754** (~693–740) | the SAME ctor, invisible to the increment-1 grep — the census-vs-emission error, corrected |
| **total constructions** | **≈1,098** (~1,084) | |
| of which in committed `*_test.cs` | **401 occurrences across 44 directories** — 243 explicit `new ж<` + 158 target-typed (the verifier's corrected figure; increment 2's 240/43 counted the explicit class only) | the per-package **`-tests` pipeline**, NOT the `-stdlib` batch — refreshed per the validated-package commit policy, not by one regen |
| hand-edited | 22 golib + 12 in four hand-owned files | in-arc |
| converter render sites for `PointerPrefix` ("ж") | **67 sites / 21 files** (67/21) | `symbols.go:32` renders BOTH declared-type and construction positions; the converter must first split the roles (declared stays `ж<T>`; constructions move to the standard-kind spelling) — OQ-1/OQ-2 are re-scoped to this 67-site audit, and "mechanical template swap" is withdrawn |

The safety property stands: the abstract base turns any missed construction into a compile
error, never a wrong-kind box.

**The complete gate list** (amendment 8 — the increment-1 list plus everything it omitted):
converter `go test -count=1 ./...`; CNR; **the aliasing-class seeded reconvert-and-BUILD**
(CLAUDE.md's rule for `Ꮡ`-machinery: both targets, control half byte-identical — R2's
instrument); **`-p:GoTargetOS=linux` `--no-incremental` build** (one hand-owned edit is a
per-GOOS darwin file, so the non-windows item sets must compile); full behavioral suite;
`go2cs.slnx` (golib API changes); **the five reflect-consumer canaries recomputed from
`docs/ValidatedTestPackages.md` at gate time** — at this writing that derivation yields
`go/internal/gcimporter` 583, `go/types` 557, `encoding/json` 491, `crypto/tls` ~400
(recompute), `encoding/xml` 386 — given §3.1 this is the gate most likely to catch a missed
site; **`go generate .` if any `package_info.cs` moves**; the counter's exact-charge unit tests
(§3.1's relocation); GolibTests; the perf suite under the P-F4 protocol, **including AOT
size/ILC wall recorded under `TrimMode=partial`** — five generic types now stand where one did,
and the trim behavior is recorded at the gate, not assumed.

**Acceptance exhibits, with expected movements stated as numbers** (all measured with R1/R2
discipline; "unchanged" is a claim to verify, not assume):

| exhibit | current | expected after B2 |
|:--|:--|:--|
| `os.File.WriteString` | 17.00 obj/op | **17 − (managed-backed `&s[i]` mints in the path)** — the probe's own per-site decomposition names the term at gate time; bytes/op DOWN by the shed dead-`m_val` mass of its fieldRef/native boxes |
| `math/big` `TestMulUnbalanced` | **59×** (Go bound 10×) — the board's 2026-08-25 re-measure at go1.23.12/.NET 10, twice at 0.07 % apart (23.77 MB on 400,320 B); the parent §7's 51.21× is the SAME assert at the old pins, superseded by the re-sizing ruling | bytes down where kind-slimming reaches `nat` box traffic; the arc owes the 51→59 hop-delta decomposition BEFORE claiming any share — a B2 pre-gate probe, per that ruling |
| `net/netip` gradient | 49 want-zero rows at 1–10; 5 want-one; `Addr.String()` IPv6 106 | counts move ONLY by the managed-backed element-ref term (several `TestNoAllocs` rows are `&s[i]`-shaped; the exact set is read from the counter's per-site charges at gate); everything else is the do-no-harm control — **B1 does NOT claim the want-zero rows**; their residual is Phase C's boundary, stated here so the headline yield reads honestly |
| nistec four curves | P224 8,484 · P256 8,528 · P384 12,572 · P521 17,090 obj/run | **must not regress**; may improve by the element-ref term only; the four `Perf*` pointer benchmarks + `PerfRefLower` within P-F4 noise |

## 7. Adversarial self-review (increment 2)

1. *"The identity-surface models are still models."* Yes — transcribed chains vs per-kind
   bodies, same harness, relative verdict; the B2 gates re-measure every exhibit on the real
   golib. The one transcription deviation found (¹ in §2) is disclosed and the banked probe's
   number is cited for that row instead.
2. *"The Pointer +6–8 % AOT cost could bite a syscall-heavy row."* It exceeds both stated bands
   (§2.3) and is treated accordingly: the sealed `Value` override on `Pointer` — legal now that
   the base is virtual, zero-cost at subtype-typed sites, confined to one class — is the
   **pre-committed P-F4 remedy**, triggered by any §6 gate row on the `unsafe.Pointer` surface
   regressing past P-F4's threshold, with no further ruling needed.
3. *"The elem foreign arm got slower and netip might live there."* netip's element traffic is
   managed-backed slices (the fast arm); the foreign arm serves `CanonicalElement`'s fallback
   storages, which the counter's per-site charges can enumerate at gate time. The two-field
   refinement bounds the fast arm's AOT cost from above at 1.15× against identity ops at 0.38×.
4. *"1,098 sites is still a regen."* Through R2's instrument, with the `-tests` closure
   explicitly split (43 packages refresh via their own pipeline per the commit policy) and the
   compile-error property covering misses.
5. *"The 67 PointerPrefix sites are converter surgery, not a rename."* Correct, and priced as
   such: the role split (declared vs construction) is B2's first converter work item, gated by
   CNR + the seeded A/B before any golib change lands.

## 8. Open questions for ratification (increment 2)

- **OQ-1 (re-scoped)** — the construction spelling after the role split: recommendation
  unchanged in intent (`new StandardBox<T>(…)` at construction positions, `ж<T>` at declared
  positions), now explicitly an audit of the 67 `PointerPrefix` render sites rather than a
  template swap.
- **OQ-2 (re-scoped)** — one lane for B2 (golib + generator + converter + regen + `-tests`
  refresh of the 43 packages), sequenced: converter role-split first (corpus-inert), then the
  golib/type split with the full ladder.
- **OQ-3** — probe records: increment 1's stays at `probes/b1-box-dispatch/` (banked); this
  increment's lands at `probes/b1-box-dispatch-i2/`; struck or kept at the coordinator's
  preference.
- **OQ-4 — DISCHARGED.** Granted as a B2 pre-gate; the pre-gate ran in this revision (§5.1) on
  the final shape and is GREEN, 24/24 cells ≤ 0.82× — B2's staffing condition is met.

---

*Inputs: `REVIEW-b1-box-design.md` (the spec for this increment); `evidence-b1rev-bench.md`;
the i2 probe (4 isolated processes per arm, both runtimes, this machine, 2026-08-26); the
emission re-census at master `346bde800` (344 + 754 constructions, 240/43 `-tests`, 67/21
`PointerPrefix`); `ж.cs:333/439/876`, `GoReflect.*`, `TypeExtensions.*`, `ж.Contracts.cs:180`,
`builtin.cs:1712/2721`, `ManagedPointerTokens`, `TryPinnedReinterpret`, `AllocationCounter`
ctor sites — all read at this tip; the S0b riders R1/R2.*
