# DESIGN — the descriptor contract: record cargo, shape cargo, and the prefix downcast

> Status: **one leg built and validated, two legs specified.** Routed to G by COORD (2026-08-30) as
> ONE design over three items that were separately rooted and would have produced three contradictory
> answers: the Reinterpret prefix-downcast, `reflect.StructOf`'s synthesis failure, and the missing
> channel-direction cargo on MakeFunc results. They are one question — *what does a descriptor carry,
> and what will the CLR let us build over it* — and this document answers it once.
>
> **§4.3 (synthesis) is ANSWERED, built and measured** — `g-synthesis-ivt` @ `3f2e02bc0`, parked for
> the rebank wave. **§4.1 (record cargo) and §4.2 (shape cargo) remain specified but unbuilt**, and
> this document is written to be their spec for a fresh-context pass: both open questions are ruled,
> the measured facts are separated from the proposals, and the gates are listed per item.

## 0. What is measured versus what is proposed

The split is explicit because **two of the three items here were mis-rooted at least once before
measurement corrected them**, and a reader deserves to know which sentences carry evidence. §4.3
additionally records the four readings it took to get right (§4.3.1), because the failure modes
generalize.

| Claim | Status |
|---|---|
| `ΔFuncType` embeds `abi.Type` **by value, first field** | measured (generated source) |
| `StructOf` fails on **internal** field types, not on layout or on `ΔFuncType` | measured, positive-controlled |
| MakeFunc results carry **no** direction and **no** result-side dims cargo | measured (`argChanDir=Unstamped`) |
| 62 prefix-downcast sites / 17 target types / 13 files | measured (census) |
| §4.3's remedy (IVT to the synthesis assembly, BOTH projects) | **built and measured** — full-census A/B, `TypeLoadException` 1 → 0, 0 regressions |
| §4.1 record cargo, §4.2 shape cargo | **proposed, unbuilt** — rulings applied, gates listed |

---

## 1. The prefix downcast — what it asks, and why the current refusal is right

Go writes its type descriptors as **prefix records**:

```go
type FuncType struct { Type; InCount uint16; OutCount uint16 }   // Type is EMBEDDED, first
type special  struct { next *special; offset uintptr; kind byte }
type specialfinalizer struct { special; fn *funcval; ... }        // special is EMBEDDED, first
```

and downcasts by address: `(*funcType)(unsafe.Pointer(t))`. The converter renders that as
`t.Reinterpret<abi.Type, funcType>()`.

**The C# layout genuinely has the prefix property.** This is the finding that reframes the item —
go2cs-gen emits the embed as a real first field:

```csharp
public partial struct ΔFuncType {
    private abi_package.Type ʗType;                    // FIRST, by value — Go's prefix, faithfully
    public partial ref Type Type => ref ʗType;
    public uint16 InCount; public uint16 OutCount;
}
```

So the downcast is asking something **structurally true**. What defeats it is not layout but
*carriage*: a `ж<abi.Type>` boxes an `abi.Type` **value**, not a window into a larger struct, so there
is no outer record to recover. `ReinterpretAliasesStorage` refuses, and **the refusal is correct** —
the damage is the address fallback behind it, not the refusal itself.

### 1.1 The class is bigger than the descriptor, and it is one class

Census over `src/core`:

```
abi.Type -> structType(7) arrayType(7) chanType(4) mapType(3) interfaceType(3) funcType(3)
            sliceType(2) rtype(2) interfacetype(2) structtype(1) ptrtype(1) ptrType(1)
special  -> specialfinalizer(8) specialWeakHandle(7) specialprofile(4) specialReachable(4)
            specialPinCounter(3)
                                        62 sites | 17 target types | 13 files
```

`special → specialfinalizer` is the **same operation** as `abi.Type → funcType`: an embedded-first
base widened to the record that embeds it. Any answer that solves one and not the other is answering
an accident of naming.

Not in this class, and deliberately excluded (they are genuine *puns*, not widenings):
`any/Value → efaceWords`, `byte → uintptr`, `byte → Cmsghdr`, `pallocBits → pageBits`.

---

## 2. Synthesis — rooted, and it is access checks

`reflect.StructOf` mints a CLR value type into the dynamic assembly `go2cs.SynthesizedStructs`.
Measured with a positive control inside that assembly:

```
CONTROL  array<ж<int>>     (all types PUBLIC)   CreateType OK
probe    abi.ΔFuncType     (public)             CreateType OK
probe    array<ж<rtype>>                        CreateType FAIL — TypeLoadException 0x80131522
```

Identical generic shape, identical `SequentialLayout`, identical assembly. The only difference is
that `int` is public and **`rtype` is `internal`** (`reflect/package_info.cs:128`).

**Root: a synthesized struct cannot carry a field whose type transitively references a type internal
to another assembly.** `initFuncTypes` requests exactly that — `struct{ FuncType abi.FuncType;
Args [n]*rtype }` — so `reflect.FuncOf` cannot build its func types, and the failed `TypeBuilder`
then poisons the dynamic assembly for every later `GetTypes()` (contained separately by
`g-dynamic-scan`).

Two hypotheses died here with evidence, recorded so they are not re-run: it is **not** the layout kind
(switching the mint to `AutoLayout` still failed, and `mint`'s own comment already states the layout
kind is not load-bearing), and it is **not** `ΔFuncType`'s ref-returning embed (it probes OK alone).

---

## 3. Cargo — what the descriptor carries today, and the two holes

A descriptor box carries a `System.Type` plus side cargo for what a `System.Type` cannot express.

```
carried today   funcParamDims   per-PARAMETER array dims           (Ꮡt.Value.funcParamDims)
                array dims      per-field / per-hop                ([GoArrayDims], [GoMapKeyDims])
                chan dir        on a VALUE, sometimes              (GoChanDir stamp)

NOT carried     per-RESULT dims          — no analogue of funcParamDims exists
                per-result chan DIRECTION — measured Unstamped at the MakeFunc result boundary
```

The measured consequence: `marshalMakeFuncResult(results[i], outs[i])` receives `outs[i]` as a bare
`System.Type` (`channel<nint>`), which cannot express direction, so a **receive-only channel marshals
into a bidirectional slot** — the assignment Go rejects. Measured:

```
GDIAG slot want=channel`1 src=channel`1 argChanDir=Unstamped wantIsIface=False => True
```

This is not a guard that can be added at the call site: the information required does not reach it.

---

## 4. PROPOSAL — one contract, two kinds of cargo

The unifying claim: a descriptor box should carry **the most-derived record**, and side cargo should
carry **only what no managed type can express**. Today the first is missing entirely and the second is
half-built, and each item above is a symptom of one of those.

### 4.1 Record cargo — box the derived record, make the downcast a type test

Let the descriptor box for a func type hold a `ΔFuncType`, not merely its `abi.Type` prefix. Then:

```
Reinterpret<abi.Type, ΔFuncType>(box)   ≡   "is the boxed record actually a ΔFuncType?"
```

a **runtime type test on the box**, not an address reinterpretation. Reading `.Type` on the result
yields the prefix, because the prefix is a real first field (§1). The same rule serves
`special → specialfinalizer` unchanged, which is the test of whether the rule is the right one.

Properties this buys:
- the refusal in `ReinterpretAliasesStorage` stays, and stops being reached for this class;
- a downcast to the *wrong* record fails as Go's would (the box is not that record);
- no new address machinery, and no `unsafe` widening.

Open question for review: whether the derived record is boxed at descriptor **construction** (every
`synthType` for a func kind mints a `ΔFuncType`) or **on demand**. Construction is simpler and keeps
one path; on-demand avoids widening every descriptor. I lean construction and want the argument
tested.

### 4.2 Shape cargo — complete it symmetrically

`funcParamDims` proves the mechanism; it is simply incomplete. Extend the same descriptor cargo to:

```
funcResultDims   per-result array dims      (mirror of funcParamDims)
funcParamDirs    per-parameter chan dir
funcResultDirs   per-result chan dir
```

so `marshalMakeFuncResult` can be handed the *Go* result type rather than a bare `System.Type`, and
the channel-direction assignment becomes decidable where it is currently invisible. This is the
minimum that closes §3 without inventing a parallel type system.

### 4.3 Synthesis — grant the dynamic assembly access — **ANSWERED AND MEASURED**

> Status: **built and validated**, `g-synthesis-ivt` @ `3f2e02bc0`. Rides the REBANK WAVE, not a
> standalone merge (COORD ruling): the grant line lands in every generated `.csproj`, so merging it
> alone would turn CNR standing-red at master and make every lane's csproj churn ambiguous against
> the "production-`.csproj` change is REAL drift" classifier. That is drift-masking for the whole
> fleet. Reflect work stacks on this branch in the interim; reflect's bank lists it as a dependency.

**The answer: `InternalsVisibleTo` for `go2cs.SynthesizedStructs`, in BOTH the production and the
test-host project.** IVT is the mechanism the corpus already uses to let a test assembly see internal
production types, and unlike the attribute first tried it is honoured by the type **loader** — it
changes accessibility itself rather than merely relaxing check enforcement.

```
full reflect census A/B, both sides on master + g-dynamic-scan:
  BASE   pass=202  fail=126  infra=23   TypeLoadException=1
  IVT    pass=203  fail=126  infra=22   TypeLoadException=0
  TestStructOfExportRules   infrastructure-error -> pass      regressions NONE
  go test -count=1 ./...    ok go2cs 162.397s
```
The `TypeLoadException` class is **eliminated**, not merely reduced — which is the result worth more
than the single row.

**Rejected, with the reason each fell:**

| Candidate | Outcome |
|---|---|
| `IgnoresAccessChecksTo` | **measured INERT.** Governs the JIT's access checks for compiled method bodies; `CreateType` is the type loader's field validation — a different enforcement layer, so it was the wrong remedy CLASS, not a faulty implementation. |
| Go-unexported types emitted C# `public` | **dead.** MORE permissive than Go: a public `rtype` is reachable by every C# consumer of the corpus, where Go grants external packages nothing. Go exportedness rides the NAME's case, not C# accessibility, so widening the C# surface buys nothing Go asked for — and it would collide with the W3a machinery, which is built on unexported production types being internal. |

### 4.3.1 The four readings — a method note that belongs with the result

This item produced **four readings, three of them wrong, each wrong for a different reason.** It is
recorded because the failure modes generalize well beyond synthesis:

```
IgnoresAccessChecksTo      "works"   -> INERT          measured a filtered run against a STALE baseline
hand-edited csproj         "3 -> 2"  -> NEVER APPLIED  the -tests run REGENERATES the csproj; the edit
                                                       was gone. `grep -c` returning 0 is the only
                                                       reason this was caught
converter IVT, production  "inert"   -> UNDER-APPLIED  the failing type lives in the TEST assembly
                                                       (`S1 : TestStructOfExportRules_S1
                                                        asm=reflect.tests public=False`)
both grants                 pass     -> holds, full-census confirmed
```

Two habits caught all three, and neither is cleverness:

1. **Print the PRECONDITION so a measurement proves it tested what it claims.** Every A/B here
   reports `ivt-in-csproj=0/1` per side. Without it, an experiment whose change never applied is
   indistinguishable from a change that did nothing.
2. **Instrument to NAME the failing thing rather than infer it.** The bare `TypeLoadException` names
   nothing; printing the field's type, assembly and accessibility is what turned "IVT is falsified"
   into "IVT is applied to the wrong assembly".

> **A correct remedy applied to an incomplete surface looks exactly like a wrong remedy.**

That sentence is the item's real lesson. The production-only grant measured identical to no grant at
all, and one more report would have told the coordinator their approved candidate was falsified when
it was in fact right.

---

## 5. Why these three had to be one design

Written separately they would have contradicted each other on cargo shape:

- a Reinterpret design alone would have invented per-call record recovery, duplicating cargo;
- a channel-direction design alone would have added a one-off direction side-channel for results,
  which §4.2 shows is one column of a table that also needs dims and parameters;
- a StructOf design alone would have treated a field-type accessibility failure as a synthesis
  redesign — which is exactly the over-pricing that measurement corrected.

COORD's routing call was better than my own framing, and this section exists to record why.

## 6. Gates this design owes when it becomes code

Nothing here is cut. When it is, per item:

```
record cargo    reflect census A/B vs its own base; the 62 prefix sites re-measured; CNR
shape cargo     the channel row (TestMakeFuncInvalidReturnAssignments) red-then-green; CNR
synthesis       TestFuncOf / TestTypeStrings red-then-green; the StructOf family re-censused
                (5 of 6 fail at master — that is the denominator, and it is a floor)
all             build from the COMMITTED shape, and the behavioral suite for any golib change
                (CNR cannot see a golib runtime change: it compares transpile output)
```
