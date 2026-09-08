# DESIGN — the syscall trampoline's VALUE-TAG refusal (the token door's second half)

**Status: DESIGN RECORD, not a cut.** Written to COORD's ruling `bea800804c` item (2), which
refused the registry- and `StorageKind`-keyed trampoline tests and admitted exactly one form — a
non-canonical VALUE TAG on minted order tokens — and which requires the tag's blast-radius census
posted **before** anything is cut. This record carries that census, and it reports that **the tag as
spelled does not fit the token space as minted**. It prices the three ways out and recommends one.
Nothing here is implemented.

Companion records: `DESIGN-managed-pointer-token.md` (Q44, the token arm itself),
`DESIGN-pointer-provenance.md` (the ratified provenance record).

⚠ **WHICH TREE THE CITATIONS ARE READ AT, because half this machinery is not at master yet.** File
and line references below are read at the **token trio's re-seat**, `claude/c2-token-trio-repair`
`4e133844c` (base master `f4ced674d`), which is the tree this increment lands on top of. Two of
them do not resolve at master `b6746ab185`: `ManagedPointerTokens.IsTokenArithmetic`
(`ж.PointerTokens.cs:192`) **does not exist there at all** — the trio adds it — and `AllocationBase`
sits at `ж.cs:439` rather than `:472`. Everything else cited (`ж.ElemRefBox.cs:189`,
`ж.FieldRefBox.cs:70`, `ж.StandardBox.cs:138`, `syscall/windows/dll_windows.cs:213`) is at the same
line in both trees, verified rather than assumed.

---

## 1. What the increment is for

The re-seat's first half (landed as the `rtlGetVersion` hand-own) removes ONE reached member of the
class where a reference-bearing pointee's order token is handed to native code as if it were an
address. The refusal is the second half: a **general** door, so the next member is a caught panic
naming itself rather than an access violation inside a kernel DLL.

**Where it goes, and why not at the token door.** Not in the `(uintptr)` conversion operator — that
is the Q44 arm, and it fires long before anyone knows whether the number will reach a syscall. At
the **trampoline**: `syscall/windows/dll_windows.cs:213`, `SyscallN(uintptr trap, params ꓸꓸꓸuintptr
argsʗp)`, which is **already hand-owned** (its `.cs.auto` sibling is preserved), so the refusal
needs **no converter change**.

**Why a VALUE test and not a registry lookup.** COORD's two adversarial reviews refuted the
registry- and `StorageKind`-keyed forms on one measurement:
`ManagedPointerTokens.Resolve` answers through **either** the projection arm (`CurrentToken(box) ==
token`) **or** the provenance arm (`IsPinnedAt(token)`), and `RegisterPinned` writes REAL ADDRESSES
into the same table that `Register` writes tokens into. A trampoline asking "is this a live token"
would therefore refuse `readFile`'s own pinned buffer on its first call. The only sound test is one
the VALUE answers by itself.

---

## 2. The soundness question, answered from the mint

COORD's design read asked it exactly: *the trampoline sees only numbers, so the test is sound only
if the token space is provably DISJOINT from HANDLEs, flags, lengths and real addresses.*

A value tag answers it **by construction rather than by table**, and the argument is the x86-64
canonical-address rule: a valid user-mode pointer has bits 63..47 **all equal**. Any token whose
bit 63 is 1 while some bit in 62..47 is 0 is **non-canonical** — it cannot be a valid pointer on any
x86-64 process, so no address can ever be mistaken for a token and no token can ever be mistaken for
an address.

The other three families fall out of magnitude, and the one that must be checked by name is the
negative integer:

| family | why disjoint |
|:--|:--|
| HANDLEs | Windows HANDLEs are small kernel-table indices; never near 2^63 |
| flags / lengths / counts | ditto |
| real addresses | non-canonical by construction, above |
| **negative integers widened to `uintptr`** | **checked by name.** `INVALID_HANDLE_VALUE` is `-1` → `0xFFFF_FFFF_FFFF_FFFF`; under the ruled test `(arg >> 48) == 0x8000` that reads `0xFFFF` and is **not** refused. The refused band is `[2^63, 2^63 + 2^48)`, i.e. `int64` in `[-2^63, -2^63 + 2^48)` — magnitudes around −9.22e18. No API passes those. |

⚠ **Platform limit, stated rather than assumed.** The canonicality argument is x86-64's. The
trampoline is a per-platform file and this increment is **Windows only**; a `linux` or `darwin`
trampoline owes its own census, and **Windows on ARM64 owes a re-derivation** — AArch64's address
tagging and 48/52-bit VA configurations make "non-canonical" a different predicate. Nothing here
transfers by assumption.

---

## 3. THE CENSUS, and the finding: the tag does not fit

### 3.1 What a token IS today

The mint is a single choke point — `ж.cs:472`:

```csharp
private protected static nuint AllocationBase(int identityHash)
{
    return unchecked((nuint)((ulong)(uint)identityHash << 32));
}
```

**A token is a 32-bit identity hash at bits 63..32, with the whole low 32 bits reserved for a
within-allocation displacement.** Three callers compose on that:

| site | token | displacement it adds |
|:--|:--|:--|
| `ж.StandardBox.cs:138` | `AllocationBase(hash(this))` | none — the base itself |
| `ж.FieldRefBox.cs:70` | `AllocationBase(hash(source)) + GoFieldDisplacement(...)` | a **byte offset** within the parent struct — small |
| `ж.ElemRefBox.cs:189` | `AllocationBase(hash(storage)) + (nuint)(uint)element` | an **absolute 32-bit element INDEX** |

`ж.HeaderSliceBox.cs:182` forwards its source's token; `NativeBox`/`NativeArrayBox` return the REAL
native address and are **never** tagged — they are addresses, and tagging them would be the defect
this door exists to prevent.

Two contracts ride on the low half, and both are stated in the code rather than inferred:

* **ORDERING.** `ElemRefBox`'s comment — *"same-storage element pointers order by index exactly like
  Go addresses"* — makes the displacement's monotonicity a contract, not an accident.
* **THE ARITHMETIC REFUSAL.** `ManagedPointerTokens.IsTokenArithmetic` (`ж.PointerTokens.cs:192`)
  reads the block back with `number & ~(nuint)0xFFFFFFFFu`, i.e. **the block size is written into
  the predicate**. It is what turns `unsafe.Add` over storage with no address into a caught panic
  instead of an uncatchable access violation — the fix that took reflect's `TestIsZero` from 167
  verdicts back to 388.

### 3.2 The budget, and why the ruled form does not fit

64 bits, three claimants:

```
   tag width  +  hash width  +  displacement width  =  64
```

Today there is no tag: **0 + 32 + 32**. COORD's ruled tag (bits 63..48 = `0x8000`) claims **16**,
leaving 48 to split between a hash whose collisions mis-resolve one allocation to another and a
displacement whose overflow **carries into the hash** and mis-resolves the same way.

* **16 + 32 + 16** — keep the hash, shrink the block to **64 KiB**. `FieldRefBox` is untroubled
  (struct field offsets). `ElemRefBox` is not: an absolute element index of 65,536 or more carries
  out of the displacement and into the hash, producing a token that **resolves to a different
  allocation**. That is a correctness hazard, not a cap, so it also requires `ElemRefBox` to
  saturate rather than wrap — which gives up the ordering contract above 2^16 elements.
* **16 + 16 + 32** — keep the block, shrink the hash to 16 bits. **Rejected on measured evidence, not
  on argument:** the vendored-alias twin's deterministic guard measured **two distinct 64-byte
  arrays sharing one 32-bit identity hash after 4,342 allocations**. A 16-bit hash collides at the
  scale of a few hundred objects, and a token collision here mis-resolves a pointer.

**So the ruled form costs one of two things, and both are real.** That is the census's finding and
the reason this record exists before a cut rather than after one.

### 3.3 A variant that fits, offered for a ruling rather than taken

The canonicality argument needs only that bits 63..47 are **not all equal** — it does not need a
16-bit constant prefix. Forcing **bit 63 = 1 and bit 47 = 0** satisfies it for every token, and
costs the hash **two** bits rather than the displacement sixteen:

```
   1 + 15 (hash hi)  +  0 + 15 (hash lo)  +  32 (displacement)  =  64
   bit63       62..48      bit47   46..32        31..0
```

* **displacement stays 32 bits** — `ElemRefBox`, `FieldRefBox` and `IsTokenArithmetic`'s
  `& ~0xFFFFFFFF` are untouched, and the ordering contract holds unchanged.
* **hash goes 32 → 30 bits.** Against the measured collision at 4,342 allocations with 32 bits, 30
  bits is roughly half that scale. **This is a degradation of an already-weak property and it is
  stated as one**, not waved past.
* **the trampoline test becomes two bit reads** — `(arg >> 63) == 1 && ((arg >> 47) & 1) == 0` —
  still O(1), still registry-free, still readable in the emitted C#.

**This is a divergence from the ruled spelling and it is stated as one.** The ruled form is sound;
it simply does not fit the mint as measured. COORD rules which axis the fleet spends.

### 3.4 The blast-radius census as a table

Every reader of a token VALUE, and whether it carries a top-bits assumption:

| site | what it does with the value | tag-safe? |
|:--|:--|:--|
| `ж.cs:472` `AllocationBase` | **the mint** — the one place a tag is applied | the choke point |
| `ж.PointerTokens.cs:197` `IsTokenArithmetic` | `number & ~0xFFFFFFFF` — **block size in the predicate** | **coupled**; moves with the displacement width |
| `ж.ElemRefBox.cs:189` | base **+ 32-bit element index** | **coupled**; the overflow hazard of §3.2 |
| `ж.FieldRefBox.cs:70` | base + field byte offset | safe at any displacement ≥ 16 bits |
| `ж.HeaderSliceBox.cs:182` | forwards the source's token | safe |
| `ж.NativeBox.cs:92`, `ж.NativeArrayBox.cs:143` | return the REAL address | **must never be tagged** — asserted by a guard arm |
| `unsafe/unsafe.cs:290,345,350,362,575` | equality, hashing, `ReferentToken` | **safe** — equality and hashing only, no top-bits read; a tagged token and a real address never collide, which is the point |
| `reflect/value_impl.cs:1260-1261` | `%p` / `UnsafePointer` projection | **safe mechanically, OBSERVABLE in output** — a tagged token prints as `0x8000…`; named as the measured risk below |
| `syscall/windows/zsyscall_windows_addrinfo_impl.cs:295` | `box.PointerOrderToken` equality | safe |
| `golib/GoLibcCall.cs:241` | `box.PointerOrderToken == blockAddress` | safe **provided mint and comparison move together** — it compares a token against a number recovered from a syscall argument |
| `ManagedPointerTokens.MintOpaque`, `RememberReinterpretSource`, `ReinterpretSource` | object-keyed, never value-keyed | safe |

**Two derivations, as the rules require.** The table above is a grep census over `src/core` for
`PointerOrderToken`, `AllocationBase` and `ManagedPointerTokens.*`; it is cross-checked by reading
the mint's own three composing callers forward from `ж.cs:472` — the two agree on the set, and the
forward read is what found `ElemRefBox`'s index width, which a name-keyed grep does not report.

---

## 4. What is still OWED before this can be cut

1. **The element-index census.** The displacement axis is the one §3.3 declines to spend and §3.2's
   first option would spend blind: **what is the largest absolute element index that actually
   reaches `ElemRefBox.PointerOrderToken`?** Unmeasured. It needs an instrumented run over a roster
   slice, and it is the one number that decides between §3.2's first option and §3.3's variant.
   ⚠ A zero from that instrument is believed only after a positive control fires it — the standing
   rule, and the one this lane paid twice in the re-seat.
2. **The per-syscall cost**, as COORD's design read required: a 10M-iteration hot loop over the tag
   test, plus one syscall-dominated roster row measured with and without. Two bit reads should be
   unmeasurable against a syscall; *should be* is not a measurement.
3. **A ruling on which axis is spent** (§3.2 option 1, or §3.3's variant).

## 5. The guard, whose NEGATIVE arm is load-bearing

COORD's ruling names it and this record does not soften it. Three arms:

* **positive** — a reference-bearing box's token handed to `SyscallN` as a direct pointer argument
  is refused, by a `PanicException` so the test host classifies a **verdict** rather than an
  infrastructure error.
* **negative, and this is the arm that earns the increment** — a run that must refuse **NOTHING**:
  every honest argument shape at the trampoline (a pinned buffer's real address, a HANDLE, a length,
  a flag word, and `-1`) passes untouched. A door that refuses a real address is worse than no door.
* **provenance** — a `NativeBox`'s token, which IS a real address, is never tagged and never refused.

**Scope: direct pointer arguments only**, per the ruling. A pointer reached through a structure the
trampoline does not decode is out of scope and stays the mirror-and-transcribe remedy's business.

## 6. What this record does NOT establish

It does not establish that the refusal is worth its cost — §4.2 is unmeasured. It does not establish
the element-index bound — §4.1 is unmeasured. It does not transfer to `linux` or `darwin`
trampolines, or to Windows on ARM64. And it does not claim the ruled spelling is wrong: the ruled
spelling is **sound**, and the finding is only that the mint has no sixteen spare bits to give it.


---

# AMENDMENT — 2026-09-08: §4.1 IS MEASURED, and both outcomes of the decision rule are written out

§4.1 left one number owed and it is now measured. This amendment records the measurement, scores the
prediction **as worded** including where it missed, and — to COORD's `1fa2647422` — pre-writes **both**
outcomes of §3's decision rule as designs, so the choice between them is a ruling over two written
things rather than a design done after the fact. §D states the criterion COORD ruled and what settles
it.

## A. The measurement — i9's fourteen rows

Run by i9 against a merge of the probe branch `claude/c2-elemindex-probe` onto master; instrument,
controls and caveats in that branch's commit. Reported at mailbox `fab03eebdf`.

| | value |
|:--|--:|
| `max_ctor`, highest of any row (`go/types`) | **3,022,479** |
| `max_token`, **every** measured row | **0** |
| `ctor_calls`, all rows | 629,240,995 |
| `token_reads`, all rows | 4,532 |

**The zero is a real measurement and not a vacuous one, and `token_reads` is what makes that
statement possible.** Tokens ARE minted — `net/http` 4,529 and `encoding/json` 3 — and **every one of
them is at index exactly 0, 4,532 out of 4,532**. Eleven of thirteen measured rows read no tokens at
all. Had the probe carried only maxima, a zero would have been indistinguishable from a path never
entered; the counter that separates them is the reason the answer is usable.

**`reflect` is NOT MEASURED, and the reason is a host crash rather than an absent instrument.** Its
`ELEMPROBE-CONTROL` line FIRED — the instrument was compiled in — and the summary never printed
because the process died with `0xC0000005` in `setField`, reached from `TestIsZero`, after 169 run /
123 pass / 43 fail / 1 skip. By the rule this record states, that row has no numbers. Two different
ways of reading nothing, and the control line is what tells them apart. It is also the row ranked
FIRST as most likely to produce a deep `m_index`, so the population's most interesting member is
precisely the one still unmeasured.

**One mechanism finding that changed how the run had to be done, and it is i9's:**
`run-validated-sweep.ps1` does **not** surface the probe — zero `ELEMPROBE` lines in the sweep log,
its stderr, or the results file. A thirteen-row sweep plan would have completed **GREEN having
measured nothing**. Caught by a 77-second pilot on one row, and caught *because* the control line is
read first. Driving the published host directly surfaces both lines.

### A.1 The prediction, scored as worded

| clause | as predicted | outcome |
|:--|:--|:--|
| 1 | `max_ctor` ≥ 65,536 on ≥1 row, ~70% | **HIT** — three rows; the maximum is 46× the threshold |
| 2 | `max_token` ≥ 65,536 on any row, ~25% | **not reached** |
| 2 (magnitude) | "expected in the **low thousands**" | **MISSED.** It is **zero.** |
| 3 | `token_reads` ≪ `ctor_calls` | **HIT** — about 139,000 : 1 |
| 4 | if 1 and 2 both hold the counters disagree, and that disagreement IS the finding | **HIT**, maximally: 3,022,479 against 0 |

The magnitude clause is recorded as a **miss**, not softened. It was wrong in the safe direction, and
the true shape — *every* minted token at index exactly 0 — is a **stronger** statement than the one
predicted, which is precisely why the wording is left as written rather than repaired.

## B. OUTCOME A, as measured: the 16-bit split, and what its saturation guard actually costs

`tag(16) + hash(32) + displacement(16)` — `AllocationBase` becomes
`(1UL << 63) | ((ulong)(uint)hash << 16)`, and `IsTokenArithmetic`'s mask narrows from `~0xFFFFFFFF`
to `~0xFFFF`. `FieldRefBox` is untroubled: struct field offsets are far inside 64 KiB.

`ElemRefBox` is the problem, because its displacement is an **absolute element index** and a value at
or above 2^16 carries **into the hash**, producing a token that resolves to a *different allocation*.
So the split requires a guard, and **the guard's shape is the hard part.** Three candidates, with what
each actually costs:

| guard | what it does | what it costs |
|:--|:--|:--|
| **saturate** | clamp the displacement to `0xFFFF` | **UNSOUND.** `unsafe.Pointer` equality is token-based (`unsafe.cs:345`), so two distinct elements at index ≥ 65,535 would compare EQUAL; and `ManagedPointerTokens.Register` is keyed by token, so they would collide in the registry and `Resolve` would return the wrong box. This is a correctness break, not a precision loss. |
| **fall back** | above the block, mint an **untagged** token | Sound, and **holes the door exactly where the biggest arrays are** — the trampoline cannot refuse what carries no tag. Honest only if the hole is counted rather than assumed. |
| **refuse at the mint** | throw when the displacement would overflow | Turns a legal Go program into a panic at an address-take that Go performs without complaint. |

**What the measurement does and does not license here.** `max_token = 0` on every measured row means
the guard's path is never taken on those workloads — which is what makes this outcome *arguable*. It
is not a bound: nothing static bounds the slice arm, `max_ctor` reached 3,022,479 on the very same
run, and the row most likely to go deep is unmeasured. **A run is not a bound** — this record said so
before the run and the run does not change it.

If this outcome is taken, the **fall-back** guard is the only sound one of the three, and it ships
with a counter so the hole's size is a measured quantity rather than an assumption.

## C. OUTCOME B, pre-written: the high-bit variant

`bit 63 = 1` and `bit 47 = 0`, hash in `62..48` and `46..32` (**30 bits**), displacement `31..0`
(**32 bits, unchanged**).

```
AllocationBase(hash):  (1UL << 63) | ((hash >> 15 & 0x7FFF) << 48) | ((hash & 0x7FFF) << 32)
trampoline test:       (arg & 0x8000_8000_0000_0000) == 0x8000_0000_0000_0000
```

* **`ElemRefBox`, `FieldRefBox` and `IsTokenArithmetic` are untouched**, the ordering contract holds,
  and **no saturation guard exists to design** — §B's whole problem does not arise.
* Soundness is the same argument as §2: bits 63..47 are not all equal, so every token is
  non-canonical and can never be a valid x86-64 user-mode pointer.
* **The cost is the hash, 32 → 30 bits, and it is a degradation of an already-weak property.**

### C.1 The hash cost is OPEN, and the honest number is not 2^32

The measured collision — two distinct 64-byte arrays sharing one identity hash after **4,342**
allocations — is itself evidence that **the CLR's identity hash is far narrower in practice than its
32-bit slot**: a uniform 32-bit hash collides around 77,000 allocations, not 4,342. Reasoning about
this change from the nominal width would therefore be reasoning from the wrong number.

**Whether dropping two bits costs anything at all depends on WHERE the entropy sits**, and the
packing above drops the **top** two bits. If the identity hash's entropy is concentrated low — which
the 4,342 figure is consistent with — the cost is near zero; if it is spread, the collision scale
roughly halves to ~2,200. **This is not asserted either way.** The measurement that settles it is
cheap and self-contained: take the identity hashes of N objects, pack them both ways, and count
collisions at each width. It is a precondition of taking this outcome, not of writing it down.

## D. The ruling criterion, and what settles the choice

COORD's `74af7f4465`: *the 16-bit split is ARGUABLE with a saturation guard, and a refusal on a
CHOICE is taken only if the high-bit variant BENCHES WORSE.* So **§C is the default and §B is taken
only against a measured regression.**

**A prediction on that bench, on record before it is run: §C will not bench worse, and the reason is
that its test is not two tests.** Written as `(arg & 0x8000_8000_0000_0000) == 0x8000_0000_0000_0000`
it is **one AND and one compare** — the same shape and the same instruction count as §B's
`(arg >> 48) == 0x8000`. The mint side costs §C one extra shift-and-or per token, on a path this
run measured at **4,532 events against 629,240,995 constructor calls**.

**What would falsify it:** a measured per-syscall regression for §C over §B at the trampoline, or a
mint-side regression large enough to show against 4,532 events. Either would be a real finding and
would select §B *with* the fall-back guard of §B's table — never with saturation, which is unsound
whichever outcome is chosen.

This record still does not cut either one. §4.2's per-syscall cost measurement remains owed, and
§C.1's entropy measurement joins it as a precondition of the default outcome.

---

# AMENDMENT — 2026-09-08: §4.2 IS MEASURED on linux, and the falsifier DOES NOT FIRE

C2, on COORD's `642fc46b29`. Probe and outputs of record: `probes/c2-token-door-cost/`. The
per-syscall half on the binding host is still owed (§E.4 below).

## E. The bench

Six arms over 10,000,000 arguments, seven reps, best-of; 21 processes across four bench
compositions × two orders × two tiering modes, on a 4 vCPU Xeon @ 2.80 GHz linux container,
CoreCLR 10.0.111, Release throughout. Three of the six arms exist only to make the other three
legible:

- **`A2`, a byte-identical twin of `A`** — the noise floor. Whatever two methods running one
  predicate differ by is what this harness cannot resolve.
- **`CONTROL`, a dictionary probe** — a deliberately expensive door. Without it "no difference
  between A and B" is indistinguishable from "this harness cannot resolve a difference", which is
  the vacuous-green shape. It fired **21/21 at 6.3–8.4×**.
- **`ANCHOR`, one P/Invoke to a trivial native function** — the managed→native transition the
  trampoline pays anyway, **104.9–111.3 ns/call**. A *lower bound* on the guarded call, never the
  syscall's cost.

`A`, `B` and the spelling variant `B2` each assert they refuse the **same set** (498,043 of
10,000,000, `-1` not among them). They agreed in every run.

### E.1 The readings

| quantity | tiering OFF (the configuration of record) | tiering ON |
|:--|:--|:--|
| door cost over no door, `A` | 0.42–0.54 ns/test | 0.38–0.51 ns/test |
| door cost over no door, `B` | 0.46–0.70 ns/test | 0.39–0.48 ns/test |
| **`B − A`** | **−0.015 … +0.204, twelve of thirteen positive, mean +0.097** | **−0.034 … +0.012, five of eight NEGATIVE, mean −0.011** |
| `B2 − A` | +0.656 … +0.763 | +0.679 … +0.732 |
| noise floor `\|A2 − A\|` | 0.0001 … 0.042 | 0.008 … 0.091 |

### E.2 What makes the `B − A` reading legible: `B2` is the calibration

`B2` computes **B's exact predicate** spelled to avoid the two 64-bit immediates that x86-64 cannot
encode inline — `(long)arg < 0 && ((arg >> 47) & 1) == 0`. It was written to *test the mechanism
this lane was about to assert* for a B-over-A cost, and it refutes it: the immediate-free spelling
is **4–5× worse than either door**, stably, everywhere. The literal `B` is the spelling to cut.

Its value beyond that is as a **calibration of the instrument**. A genuine per-test difference in
this harness looks like `B2`: stable to ±7 %, same sign and magnitude in every order, both tiering
modes, every composition. `B − A` is none of those — its **sign flips with tiering**, its magnitude
**moves when unrelated arms are added to the bench**, and it falls inside the noise floor in 9 of
21 runs. The honest reading is therefore that **B's excess over A is at or below what this
instrument resolves ON THIS HOST**, with a weak positive lean at tiering-off that is reported rather
than explained away. ⚠ **That sentence is NARROWED by §F below: on windows-x64 the effect is real,
larger, and lands on the OTHER tiering setting.** The linux reading is not wrong; it is
host-conditional, and §E wrote it as though it were not.

### E.3 The prediction of §D, scored as worded

§D predicted **"§C will not bench worse"**, reasoned from "its test is not two tests … the same
shape and the same instruction count", with the falsifier **"a measured per-*syscall* regression
for §C over §B at the trampoline"**.

- **The reason is not confirmed.** Twelve of thirteen tiering-off readings lean positive; equal
  instruction cost is not what that looks like. The claim was stated more strongly than the
  measurement supports, and §E.2 is why it cannot be resolved either way here.
- **The conclusion, at the falsifier's own granularity, HOLDS — the falsifier does not fire.**
  ⚠ Narrowed by §F: the criterion is still met on the binding host, but "B does not bench worse than
  A" is HOST-CONDITIONAL and FALSE on windows under default tiering. The
  door runs once per **argument** on `SyscallN(uintptr trap, params ꓸꓸꓸuintptr argsʗp)`, so the
  per-call cost is per-test × arity, and quoting the per-test figure as per-call would understate
  it by up to 18×. Taking the **worst** reading (+0.204 ns) at the **worst** arity (18) against the
  anchor's **lower** bound: **3.7 ns on a call of ≥ 105 ns, ≤ 3.5 %**, and a real syscall is
  strictly larger than the anchor. At realistic arities it is under 1 %.

**So COORD's criterion is not met and §C (outcome B) stands as the default**, on the measurement
rather than on the prediction's reasoning.

### E.4 What §4.2 still owes, and it is the binding host's

1. **The per-syscall row.** §4 item 2 asks for "one syscall-dominated roster row measured with and
   without". The trampoline is Windows; this reading is linux. Not transferable by assumption.
2. **The mint side.** §D's second clause — one extra shift-and-or per token against 4,532 events —
   is untouched by this bench.
3. **A windows-x64 reproduction** of the table above, which is what an i7 or i9 run of the same
   probe supplies; the probe carries a `-text` mark so a checkout anywhere runs byte-identical
   source, per the probes directory's own convention.

---

# AMENDMENT — 2026-09-08: §F, the WINDOWS-x64 reproduction, which INVERTS §E's tiering axis

The i7, the binding host, quiet box, 24 timed processes, against the committed probe. Relayed by
COORD (`cc877aed09`). **Outcome B still stands on the criterion as §D worded it** — and §E's
supporting sentence does not transfer.

| ns per test, per argument | tiering OFF (of record) | tiering ON | unset |
|:--|:--|:--|:--|
| door cost A | 0.480 | 0.320 | 0.320 |
| door cost B | 0.497 | 0.702 | 0.693 |
| **B − A** | **−0.002 … +0.054 (6/8 positive)** | **+0.372 … +0.399 (8/8)** | **+0.363 … +0.391 (8/8)** |
| B2 − A | 0.590 | 0.560 | 0.556 |
| noise floor μ | 0.016 | 0.017 | 0.006 |

Control fired 24/24 at 6.0–11.0×; refusal sets agree A = B = B2 at 498,043 per pass; medians carry
the same reading as best-of-7.

**What this changes.** Under DEFAULT tiering B's door costs +0.377 ns/test more than A — **33× the
noise floor, ±5 % over 16 processes, order-independent (0.376 forward / 0.378 reverse)**. That is not
an unresolvable difference; it is a real one. At tiering OFF, the configuration of record, B − A is
within the floor in 7 of 8 (μ +0.016 against a floor of 0.016). **§E's linux sign flip is INVERTED
here**: linux leaned positive at tiering-off and was null at tiering-on; windows is null at
tiering-off and clearly positive at tiering-on.

**So the ruled sentence for this record is: "B does not bench worse than A" is HOST-CONDITIONAL and
FALSE on windows under default tiering.** It is the both-tiering clause of §D's criterion — the
configuration of record being tiering OFF — that keeps outcome B seated. Materiality holds by
arithmetic: 0.377 × 18 arguments = **6.8 ns per guarded call** against a real Windows syscall's
hundreds, and that arithmetic is exactly what the still-owed per-syscall row (§F.2) exists to
replace with a measurement.

**Mechanism, half-rooted and labelled so nobody invents the rest.** Under tiering every arm compiles
as `Tier1-OSR with Synthesized PGO` (the arms are called eight times, so the 10M-iteration loop is
promoted by on-stack replacement); ArmB's OSR body is 99 bytes against ArmA's 87. Forcing full opts
(`DOTNET_TC_QuickJitForLoops=0`) removed OSR and HALVED the gap (0.377 → ~0.19), so OSR owns roughly
half and **the residual ~0.19 ns is UNROOTED**. Axes not varied: alignment, Haswell-E vs Xeon,
CoreCLR 10.0.11 vs 10.0.111. Not chased.

## F.1 Two corrections the windows run forces on §E and on the probe's own README

1. **The B2 ratio was overstated, and the denominator is why.** §E and
   `probes/c2-token-door-cost/README.md` said the immediate-free spelling is "4–5× worse than either
   door". As a **door-cost ratio** it is ~2.2–2.75× on windows and ~2.5× on the same README's own
   linux figures. The 4–5× came from dividing B2's EXCESS by B's excess — a different denominator,
   and neither sentence said which it meant. B2 remains stably the worst door in all 24 windows runs
   and all 12 linux ones, so the imm64 story stays falsified in both readings; only the multiplier
   was wrong.
2. **The probe's WINDOWS anchor is not a syscall, and its percentages are void.**
   `GetCurrentProcessId` is a user-mode PEB read at 6–9 ns — a ~15× weaker lower bound than linux's
   `getppid` at 105–111 ns. So the probe's "% of the lower bound" line is an ANCHOR ARTIFACT on
   windows and **is not quoted**, and **cross-host percentages from this probe are invalid**. §E's
   linux percentages stand for linux only.

## F.2 The per-syscall row, OWED ON WINDOWS before this increment banks its cost claim

§4 item 2 asked for "one syscall-dominated roster row measured with and without". Specified here so
the box that runs it need not infer it:

- **A real kernel transition through the trampoline** — not `GetCurrentProcessId`, per F.1(2). The
  call must actually enter the kernel, so the anchor is a lower bound worth dividing by.
- **With and without the door**, and **under both tiering settings**, since §F shows the two
  settings do not agree on this host.
- Reported as ns per guarded call AND as a share of that call, **with the arity stated** — the door
  runs once per argument, so per-call is per-test × arity, and the two must not be conflated.

## F.3 The MINT-SIDE clause is UNMEASURED, and is named rather than left implied

§D's prediction had a second clause: outcome B costs the mint one extra shift-and-or per token,
against a path measured at 4,532 events over 629,240,995 constructor calls. **No bench in §E or §F
touches it.** It is not "small"; it is UNMEASURED, and it is recorded as such rather than waved past.
