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
