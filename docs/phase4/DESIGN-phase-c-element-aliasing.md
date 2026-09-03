# Phase-C element-aliasing — the measured wall behind edwards25519's want-zero row

> **Status: design record, not a cut (COORD ruling 2026-09-03, mailbox `bd759362`).** This is the
> B′ arc's closing product alongside the eligibility fix `ae444cc48`. It exists so the next lane
> starts from a *measured* wall rather than a hypothesis. Every number below was measured on
> G-LAPTOP, go1.23.12, .NET 10, during the B′-S2 attempt (2026-09-03); the reverted increment is
> saved as `g-s2-femul-relaxation.patch`.

## 1. The wall, in one sentence

**edwards25519's `TestAllocations` (98 objects/run, `want 0`) cannot fall under the dual-recv arc
(B′-S0/S1/S2) because its box chain is pinned boxed by the POINT-LEVEL methods that take ALIASING
addresses of their receiver's value-struct fields — and the eligibility fix correctly EXCLUDES those
methods from ref-receiver promotion, so the chain never un-boxes.** The reduction needs a capability
the runtime does not have: an `Ꮡ(v.field)` taken from a `ref` receiver that ALIASES the receiver's
managed storage instead of boxing a copy of the field. That capability is Phase-C.

## 2. The chain, measured

`crypto/internal/edwards25519`'s want-zero path is `NewIdentityPoint().Add(p, NewGeneratorPoint())`
plus `Scalar`/`Point` encode round-trips. The bill decomposes (design `DESIGN-zh-box-b-prime.md`
§1.1) into 41 method-argument field-ref boxes + ~15 receiver `heap()` locals (both B′-attributable)
+ a class-3b floor (`@new<T>` temporaries + params-array + `Bytes` backing). The chain is:

```
Point.Add (Point-level)  →  projP1xP1.Add (Point-level)  →  Element.Multiply/Square (field)  →  feMul/feSquare (leaf function)
```

- `feMul`/`feSquare` are the leaf functions. Phase-A already ref-lowered their INPUT parameters
  (`feMul(ref Element v, ref Element x, ref Element y)` — all three refs; verified in the emission).
- `Element.Multiply`/`Square`/`Invert` (field ops) call `feMul(v, x, y)`, passing their receiver `v`.
- `projP1xP1.Add` / `Point.Add` (Point-level) compute point arithmetic by calling the field ops on
  their receiver's fields: `Ꮡv.of(projP1xP1.ᏑX).Subtract(…)`, `ᏑPP.Multiply(ᏑYplusX, Ꮡq.of(…))`,
  etc. Each `Ꮡv.of(field)` is an ALIASING pointer into the receiver's field — a write through it must
  land in the receiver.

The Point-level methods take those aliasing field addresses. Today only a BOX receiver can produce
an aliasing field pointer (`Ꮡv.of(field)`); a `ref` receiver's `Ꮡ(v.field)` boxes a COPY and drops
the write. So the eligibility fix `ae444cc48` correctly excludes them (`bodyTakesReceiverFieldAddress`
+ `bodyTakesImplicitReceiverFieldAddress`): they keep the box receiver. That exclusion is CORRECT —
without it they emit `Ꮡv.of(field)` against a receiver that has no `Ꮡv` (99 CS0103 across the
package, the pre-existing blocker the eligibility fix cleared). But it is also the wall: a boxed
`Add` passes boxes to `Multiply`, so `Multiply`'s parameters cannot lower, so the intermediate locals
cannot un-box, so nothing reduces.

## 3. The capability (Phase-C)

**An `Ꮡ(v.field)` taken from a `ref` receiver that ALIASES the receiver's managed storage** — an
interior pointer into the struct, not a boxed copy. With it:

- `projP1xP1.Add` / `Point.Add` can be ref-receiver primaries: `Ꮡ(v.X)` aliases, so the writes
  through the field ops land in `v` exactly as `Ꮡv.of(projP1xP1.ᏑX)` does today.
- Their field-op calls pass refs, so `Multiply`/`Square`'s parameters lower, so the intermediate
  `heap()` locals become value locals, and the chain un-boxes end to end.

This is the same family as the runtime's existing **element-aliasing publish gate** (CLAUDE.md, the
golib change that gave `ж<T>` per-instance state so a slice element's pointer aliases the backing
array rather than a copy). Phase-C extends that from slice elements to struct fields reached from a
ref receiver.

## 4. What it buys

**edwards25519's 98 → the class-3b floor, predicted ≤10** (design §7.1). The B′-attributable classes
(41 method-argument field-ref boxes + ~15 receiver locals) go to zero; the residue is the ~5
`@new<T>` per run + `checkInitialized`'s params-array + `Bytes`' backing, which Phase-C does not
touch. The evidence is the measured 98 and its §1.1 decomposition — this is the only stdlib row whose
want-zero assert is gated entirely on this one capability.

The corpus-wide constituency is every method chain gated the same way: point arithmetic over
value-struct fields. nistec fell −96.5% under Phase-A precisely because its bill is leaf *functions*
(no receiver-field aliasing); edwards25519's is point-arithmetic *methods*, so it needs Phase-C.

## 5. What it costs

A golib change on the `ж<T>` path (an aliasing interior-pointer representation), so the **corpus-wide
byte-cost rule applies**: per CLAUDE.md, a change adding instance state to `ж<T>` (or any per-box base
class) is a +8 B/box change proportional to boxes allocated per path, and the commit must state that
cost even when correctness demands the field — the element-aliasing publish gate is the named
precedent, and its unfavorable direction shipped unmeasured once and burned an attribution run. Any
Phase-C cut measures both halves (the reduction it buys AND the per-box cost it adds) before banking,
and states the direction.

## 6. The two measured nulls that bound this

Both from the B′-S2 attempt (2026-09-03), so the next lane does not re-walk them:

1. **S1's zero reduction.** With the eligibility fix, edwards25519 flag-on (`-dual-recv
   -dual-recv-params`) COMPILES but the whole-package box census is byte-identical to flag-off:
   `ref heap(`=39, `.of(`=197, `@new<`=21; `projP1xP1.Add` byte-identical; measured `TestAllocations`
   = 98 = flag-off. The dual-emission infrastructure landed; the reduction did not, because the chain
   stays boxed (§2).
2. **S2's invalid mixed shape.** The feMul-caller relaxation (a base-lowerability pre-pass feeding a
   relaxed `bodyPassesReceiverAsPointerArg`, saved as `g-s2-femul-relaxation.patch`) DID promote
   `Multiply`/`Square`/`Invert` to ref receivers (field 16→20 primaries), but (a) still ZERO box
   reduction — their PARAMETERS stayed boxed because `Add` (excluded) passes boxes — and (b)
   uncompilable: `fe.cs` CS0411 ×2, a ref-receiver `Multiply(this ref Element v, ж<Element> Ꮡx, …)`
   calling `feMul(v, x, y)` which needs all-refs. Reverted. math/big's flag-on emission thins
   (`.of(` 41→32, 13 Float promotions) but ALSO does not compile (CS1929 ×8, the same mixed-shape
   family) — so dual-recv has no compiling reduction on either acceptance case.

The common cause of both nulls: promoting a receiver WITHOUT lowering its parameters (or vice versa)
is an inconsistent shape, and the parameters cannot lower while a field-address caller (`Add`) passes
boxes. Phase-C is the only lever that lets `Add` itself un-box, which is what unpins the whole chain.

## 7. Nothing-throwaway

The B′ arc's real products are (1) the eligibility fix `ae444cc48` — a genuine correctness fix (a
method taking a receiver-field address, explicit or implicit, cannot be a ref-return primary; it is
what first made edwards25519 flag-on compile, clearing 99 CS0103 + 4 CS1503), guarded by
`TestReceiverFieldAddressExcludesPrimary` and CNR-inert; and (2) this record. The next lane building
Phase-C starts from a measured wall, a predicted payoff with its evidence, a stated cost with its
precedent, and two bounded nulls — not a hypothesis.
