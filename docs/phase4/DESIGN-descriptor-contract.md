# DESIGN — the descriptor contract: record cargo, shape cargo, and the prefix downcast

> Status: **design, no code cut.** Routed to G by COORD (2026-08-30) as ONE design over three items
> that were separately rooted and would have produced three contradictory answers: the Reinterpret
> prefix-downcast, `reflect.StructOf`'s synthesis failure, and the missing channel-direction cargo on
> MakeFunc results. They are one question — *what does a descriptor carry, and what will the CLR let
> us build over it* — and this document answers it once.

## 0. What is measured versus what is proposed

Everything in §1–§3 is **measured** on this tree; §4–§6 are **proposal**. The split is explicit
because two of the three items in this design were mis-rooted at least once by me before measurement
corrected them, and a reader deserves to know which sentences carry evidence.

| Claim | Status |
|---|---|
| `ΔFuncType` embeds `abi.Type` **by value, first field** | measured (generated source) |
| `StructOf` fails on **internal** field types, not on layout or on `ΔFuncType` | measured, positive-controlled |
| MakeFunc results carry **no** direction and **no** result-side dims cargo | measured (`argChanDir=Unstamped`) |
| 62 prefix-downcast sites / 17 target types / 13 files | measured (census) |
| The remedies in §4–§6 | **proposed, unbuilt** |

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

### 4.3 Synthesis — grant the dynamic assembly access

Emit `System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute` on the
`go2cs.SynthesizedStructs` assembly for each assembly a synthesized field type references. The
attribute is not currently used anywhere in the tree (checked). This is bounded — it changes the
assembly setup in `GoStructSynthesis.mint`, not the synthesis design — and it is what re-priced this
item down from a synthesis arc.

Open question for review: whether to emit it eagerly for the whole closure, or lazily per referenced
assembly as fields are added. Lazy is tighter; eager is simpler and the assembly is process-local and
run-only.

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
