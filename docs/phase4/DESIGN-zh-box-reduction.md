# The ж-box reduction arc — design

> **STATUS: DESIGN — SIGNED OFF (user, 2026-08-10).** The §10 decisions are ratified rulings, no
> longer open questions. Implementation remains gated exactly as commissioned by the 2026-08-10
> coordinator ratifications: this arc runs **AFTER the near-miss harvest completes**, and
> implementation start is confirmed with the coordinator session first. A four-lens adversarial panel (charter §7) ran against the first draft
> on 2026-08-10; its confirmed findings are folded into the sections below and the full record is
> §11. Companions:
> [`BOARD-next-validation-candidates.md`](BOARD-next-validation-candidates.md) (the r56d-allocdecomp
> and r39-osalloc decompositions this design is priced against),
> [`DESIGN-allocation-counting.md`](DESIGN-allocation-counting.md) (the instrument),
> `src/core/golib/ж.cs` (the mechanism under discussion), and the charter's §5 gate table. Written
> against the corpus at r58a (roster 110/215).

---

## 1. The bill — what this arc is priced against

Every number here is **measured, not modelled** — r56d's byte-exact decomposition, corroborated at
r58a by the golib `AllocationCounter` (an independent instrument, agreeing to 0.7 % on the headline
figure). Go's count for every row below is **zero** unless stated.

**`crypto/internal/nistec` `TestAllocations` (per run, want 0):**

| Curve | standard `ж` boxes | of which pinnable `T[1]` slots | field-ref `ж` boxes | `array<T>` backings | total objects | bytes |
|:--|--:|--:|--:|--:|--:|--:|
| P224 | 106,472 | 86,930 | 66,081 | 3,373 | 263,049 | 23,624,307 |
| P256 | 97,389 | 76,513 | 63,786 | 3,386 | **241,077** | 21,963,547 |
| P384 | 200,133 | 168,947 | 94,993 | 4,992 | 469,068 | 40,754,499 |
| P521 | 386,667 | 343,898 | 129,963 | 6,783 | 867,314 | 72,242,788 |

The unit costs close to the byte: every field-element operation prices at exactly
`(field pointers × 128 B) + (address-taken locals × 144 B)` — `P224Element.Mul` is 960 B =
3 × 128 + 4 × 144; `P224Point.Add` is 39,464 B ≈ 43 field ops + 8 `@new` boxes. **One hundred
percent of the bill is the ж box model**; the r39-killed waste classes were checked and are absent.

**The cost classes, and where each comes from.** ⚠ The panel's generality lens re-decomposed the
managed-box bucket (§11, G-F1/G-F2) and the class list below carries that correction — the first
draft mislabeled ~83 % of class 3.

1. **Field-ref boxes, 128 B** — Go's `&e.x`, emitted `Ꮡe.of(P224Element.Ꮡx)`: a fresh
   `ж<T>` per evaluation. Free in Go, and Go returns the *same address* every time.
   On P256, ~46,500 of the 63,786 sit at plain lowered-candidate argument positions; the other
   ~17,280 sit inside the **conversion shape** of class 3a below (the `Select`/`ToBytes`/
   `FromBytes` call sites).
2. **Address-taken locals, 144 B** — Go's `var x uint64; f(&x)`, emitted
   `ref var x = ref heap(new uint64(), out var Ꮡx)`: a ж box **plus** the eager `T[1]` pinnable
   slot its constructor allocates for an unmanaged `T` (2 counted objects for Go's 0).
3. The managed-`T` standard boxes (~20,876 on P256) split into two classes the first draft
   conflated:
   - **3a. Pointer-conversion boxes (~17,280)** — Go's `(*[4]uint64)(&v.x)`, free in Go, emitted
     `Ꮡ((Ꮡv.of(P224Element.Ꮡx)).Value.Value)`: an `of()` box plus a second standard box over a
     copied `array<uint64>` header. An **emission artifact**, class-1-like, owned by Mechanism A's
     conversion row (§3.3).
   - **3b. `@new<T>()` boxes (~3,600)** — Go's `new(fiat.P224Element)`: a *real* Go heap
     allocation that Go's compiler elides by inlining + escape analysis ("the explicit NewP224Point
     calls get inlined, letting the allocations live on the stack" — Go's own comment). The managed
     model has no inlining that turns a heap box into a frame slot. Phase-C territory.
4. **`array<T>` backings, 88 B** — Go's `[4]uint64` is inline in its struct; golib's `array<T>` is
   a struct over a heap `T[]`. Phase-C territory.

**What is gated on this arc — split honestly by which phase can reach it** (a panel-forced
correction; the first draft recruited prizes the committed phases cannot deliver):

- **Reachable by Phases A+B (the phases this document asks sign-off for): no banked rows.** The
  deliverable is corpus-wide allocation and GC-pressure reduction, a large measured cut in the
  nistec distance, readability gains, and a validated mechanism. `math/big` `TestMulUnbalanced`
  (allocation-model overhead vs a 51×-input budget — "it will move when that model does") *may*
  move; measured, not promised.
- **Phase C (horizon, not scheduled):** `crypto/internal/nistec`'s five want-zero rows
  (**2,200 verdicts**), `crypto/ecdsa` (82), and `math/big` `TestNewIntAllocs` (1 vs 0 — `NewInt`'s
  box is a returned real allocation Go elides by escape analysis; diagnosable now as class 3b).
- **Needs an r39-style decomposition before it can be assigned to ANY arc:**
  `net/http/internal` (2 objects vs budget 1 — the ratification named this arc as "the likely
  instrument," but its residual is `@string`/compiler-emitted shapes, not lowerable pointer
  traffic; nothing in this document provably closes it), and the perf table's interface rows
  (Iface 5.84×, IfaceShell 39.83× — adapter and interface-conversion costs this arc does not own).
  The first draft's attribution of **MatMul (2.40×) and Sort (3.75×) is withdrawn**: both
  transpiled benchmarks contain zero ж/`Ꮡ`/`heap(` occurrences (measured by the panel); their
  ratios belong to the slice-model and interface arcs.

**What is deliberately NOT gated on it:** `crypto/rsa`'s disclosure (340,756 objects vs 10 —
dominated by managed big-integer arithmetic, ratified as provably-cannot-satisfy);
`log` (its blocker is the `runtime.Caller` arc, not the alloc row); `io`'s
`TestMultiWriter_WriteStringSingleAlloc`, which **passes at exactly 1** and functions as this arc's
do-no-harm canary.

---

## 2. Goals and non-goals

**Goals, in priority order:**

1. **Remove ж-model allocations that Go the language never mandated** — the box minted for an
   address that never outlives a call, the box minted per-evaluation for a field address, and the
   boxes minted for a pointer conversion. These are artifacts of the emission strategy, not of Go
   semantics, and they are the measured bulk of the nistec bill: classes 1, 2 and 3a are
   **~97 %** of P256's objects (234,000 of 241,077).
2. **Preserve Go semantics exactly** — pointer identity, nil timing, aliasing, write-through.
   A mechanism that changes what a Go program observes is rejected regardless of its savings.
   Where today's emission itself diverges from Go (the lazy field-address nil panic — §3.3), the
   lowered emission converges **on Go**, never on a third behavior.
3. **Keep the output readable as Go** — the project's second-priority goal. The proposed emission
   is *closer* to the Go source than today's (see the worked example in §3.4).
4. **Measure every step with the counter** on the named workloads of §7 — the instrument this arc
   was ratified to wait for — plus a wall-clock instrument the panel showed the current perf suite
   does not provide (§7).

**Non-goals (explicit, so the panel and the user can veto the boundary rather than discover it):**

- **Banking nistec's want-zero rows in Phase A.** The honest projection (§3.6) leaves ~7,000
  objects per P256 run on the recommended branch — classes 3b and 4 are real Go allocations plus
  the array model, and they need the Phase-C horizon. A want-zero assert is satisfiable in
  principle (standing ruling #1), and this design does not change that classification; it narrows
  the distance by ~97 %.
- **No change to `testing.AllocsPerRun` semantics, the counter's census, or any disclosure
  ruling.** The instrument stays fixed while the thing it measures moves.
- **No box pooling or recycling.** Reusing a dead box requires observing its death; nothing in the
  CLR reports it, and a recycled box aliased by two Go pointer eras breaks pointer identity — the
  one property `ж.cs` is built around. Rejected, not deferred.
- **No field-ref box interning/memoization in Phase A** — evaluated and deferred, with reasons and
  a resurrection bar, in §5.
- **No `[InlineArray]` / value-carrier layout change in Phase A/B** — horizon work with named
  blockers (§6).
- **No change to the syscall pinning model** (`EnsureStableAddress`, `PinnedBuffer`, the eager
  slot's role in address stability). §4 explains why the slot is structural.
- **The interface rows' adapter/conversion costs** (`ᴠ`/`ж` adapter allocation, interface boxing
  at conversion sites) are a different arc; this design touches only pointer-box costs.

---

## 3. Mechanism A — interprocedural ref-lowering (the centerpiece)

### 3.1 The observation

The converter already computes, per pointer parameter, whether the body uses the parameter as a
*value* (dereference, field access, indexing) — that is what decides today's entry preamble
`ref var out1 = ref Ꮡout1.DerefOrNull();` (`visitFuncDecl.go` ~700). In fiat-shaped code the box
is then **never touched again**: every use flows through the `ref` alias. The callee needs an
alias; only the *emission convention* forces the caller to materialize a heap object to carry it.

C# has a first-class carrier for exactly this: **`ref` parameters**. A `ref T` argument is an
alias into the caller's storage — a managed interior pointer the GC tracks and updates on
compaction, so no pinning, no box, no allocation, and writes through it land in the caller's
storage by construction.

### 3.2 Callee-side classification — when is a pointer parameter *ref-lowerable*?

A pointer parameter `p *T` of a function `f` is **ref-lowerable** iff every use of `p` in `f`'s
body is one of:

- **D1** — a dereference: `*p` read or write, `p.field` access, `p[i]` indexing, `range p` over a
  pointer-to-array, `switch *p` (the uses that today route through the entry `ref` alias);
- **D1′** — a derived address that itself feeds a lowered position: `&p.field` / `&p[i]` passed as
  the argument to a ref-lowered parameter (this is the fiat `Sub` shape — the worked example of
  §3.4 does not classify without it);
- **D2** — forwarding `p` itself as the argument to another *ref-lowered* parameter position
  (computed to a fixed point across the package; see below).

And **none** of `p`'s uses is any of (each named after the corpus shape that motivates it):

- **X1** — pointer identity or nil-ness: `p == nil`, `p == q`, `switch p` / `case nil`, `p` as a
  map key, comparison of any kind;
- **X2** — escape: `p` returned, stored in a field/global/slice/map/channel, captured by a closure
  or by a `defer`/`go` statement's function body;
- **X3** — representation: `unsafe.Pointer(p)`, `uintptr(p)`, `reflect` over `p`, `%p`/`%v`
  formatting of the pointer itself, a method call **on** `p` (receivers stay `ж` in Phase A);
- **X4** — re-pointing: `p = q` assignment to the parameter itself;
- **X5** — the function's identity escapes: it is used as a **func value** or method value
  (assigned, passed, stored), it is exported outside the package, it participates in an interface
  method set, its parameter's type is a **named pointer type** (`type P *T` — the generated
  wrapper's operator surface must survive), it appears in any of the converter's `//go:linkname`
  registries (`linknameForwardTargets` / `linknamePushTargets` / the package's `linknameHandles` —
  a linkname pull publicizes an unexported function across the assembly boundary in the boxed
  convention, invisibly to a per-package scan), **or its body is absent** (`Body == nil`,
  the assembly/cgo partial-stub shape — an empty use set satisfies D vacuously, and lowering the
  emitted partial declaration would orphan its hand-written `*_impl.cs` companion).

**Soundness is whitelist-shaped, and that is the load-bearing statement:** D1/D1′/D2 enumerate the
only uses a lowered parameter may have, so *any* use the classifier does not positively recognize
disqualifies. (The first draft argued soundness from "every X3 emission routes through `Ꮡp`" —
circular, since it reasons from the current emission's spelling; the whitelist argument is the
stronger claim and replaces it.)

**The fixed point is two-sided.** The D2/D1′ iteration starts from "all candidates lowerable" and
strips until stable — and it also strips on **caller-side emittability**: a call site whose
argument shape has no `ref` emission row in §3.3 (or sits in a `defer`/`go` statement) un-lowers
that parameter position rather than dead-ending emission later. The panel demonstrated this is not
theoretical: the design's own flagship package calls `p224Selectznz` through a pointer conversion
(§3.3 row 5), and a purely callee-side classification would have validated the projection right up
until emission hit the wall after the golden churn.

**One further structural note.** D-classification runs on the production package's own files with
`go/types` (a sound AST-based scan, not the text heuristic the preamble machinery uses — that
heuristic errs toward keeping the alias, which is safe for its purpose but not for this one).

### 3.3 Caller-side lowering — arguments and address-taken locals

At every call site of a lowered parameter position, the argument (which today is a pointer-valued
expression that would mint or carry a box) is emitted as a `ref` expression:

| # | Go argument | today's emission (allocates) | lowered emission (allocates nothing) |
|:-:|:--|:--|:--|
| 1 | `&e.x` (field of deref'd param/receiver `e`) | `Ꮡe.of(P224Element.Ꮡx)` — 1 box | `ref nonnil(ref e).x` (see the nil doctrine below) |
| 2 | `&x` where `x` is an address-taken local | `Ꮡx` (the `heap()` box, made at decl) | `ref x` (plain local — see below) |
| 3 | a pointer variable `q` | `q` (carries the box) | `ref q.DerefOrNull()` |
| 4 | `&s[i]` | `Ꮡ(s, i)` — 1 box + 1 interface temp | `ref s[i]` (the indexers are `ref`-returning, incl. the generated named-array wrappers) |
| 5 | a pointer conversion `(*T2)(&v.x)` over a `[GoType]` named-array wrapper | `Ꮡ((Ꮡv.of(…)).Value.Value)` — 2 boxes | hoisted temp: `var ᴛ1 = v.x.Value; f(ref ᴛ1, …)` — 0 boxes (parity argument below) |
| 6 | a non-variable pointer expression: `&T{…}` composite literal, `new(T)` result, any call result | `Ꮡ(new T(…))` / carries the returned box | hoisted temp: `var ᴛ1 = new T(…); f(ref ᴛ1, …)` |
| 7 | the literal `nil` | `default!` | `ref ((ж<T>)default!).DerefOrNull()` — binds the null ref, faults on first callee use, per the nil doctrine |

**Rows 5–7 share one justification, and it comes from the callee's own classification.** A
lowered parameter's address is never compared, stored, escaped, or converted (the D/X rules), so a
caller-side temporary is observationally identical to a distinct heap box: nothing the callee can
do with the ref can tell them apart. Row 5 is additionally **byte-parity with today**: the current
`Ꮡ(expr.Value)` emission already boxes a *copied* `array<T>` header whose `T[]` backing is shared,
so element writes flow through and whole-header writes are lost — in both emissions equally. (The
generated wrapper's `Value` is a get-only, lazily-initializing, by-value property, so no direct
`ref` into it exists; a go2cs-gen ref-returning accessor is the more durable alternative,
recorded in §10.3 — **ruled for the temp rule**, which is converter-only; the accessor remains
the fallback if the temp rule disappoints at A1/A2.)

**`defer f(&x)` / `go f(&x)` call sites are boxed sites, categorically.** The defer/`go` machinery
stores eagerly-evaluated argument values in a frame and invokes a thunk later; a managed `ref`
cannot be stored there (CS8175 at best, a silent `Ꮡ(x)` copy-box at worst — the copy-box variant
compiles and silently loses writes, the panel's worked example returns 0 where Go returns 7).
Arguments at lowered positions inside `defer`/`go` statements therefore keep today's boxed
emission, with the thunk deriving the ref at invoke time (`ᴛ1 => f(ref ᴛ1.DerefOrNull())`), which
also preserves Go's defer-time argument evaluation.

**Address-taken locals fall out for free — with two carve-outs.** A local whose address is taken
*only* as arguments to lowered positions no longer needs the `heap()` box at all: the declaration
reverts to a plain local, and the escape-analysis verdict "address-taken → heap box" gains one
refinement: "address-taken *only into lowered positions* → stack". For an unmanaged `T` this
removes **two** counted objects per local (the box and its eager `T[1]` slot) — the single largest
class in the nistec bill. The carve-outs: (a) an address flowing to a lowered position **under a
`defer`/`go` statement** keeps the box (previous paragraph); (b) the reversion predicate consumes
the converter's existing full use-census — including *implicit* address-takes such as a
pointer-receiver method call on `x` or `x[:]` on an array local — not a new `&x`-only scan. If any
box use survives, the local keeps its box and lowered call sites use `ref x` through the box's
`ref` alias — correct either way, since the alias and the box name the same storage.

**The nil doctrine — corrected by the panel, and now stated against what today ACTUALLY does.**
The first draft claimed nil timing was preserved byte-for-byte; the panel refuted it (§11, S-F1/
S-F2) and the corrected doctrine is: **the lowered emission converges on Go's nil behavior, which
is not always today's.** The facts, per shape:

- *Field address of a nil base, `f(&e.x)`.* **Go panics eagerly**, at `&e.x`, before the call —
  later arguments unevaluated, callee never entered. **Today** diverges from Go in two spellings:
  a null-reference nil faults in the caller during `of(…)` (matching Go's frame by accident); a
  canonical `NilBox` nil mints the field-ref box lazily and faults at the *callee's entry
  preamble* — after every caller argument evaluated, before any callee statement or `defer`. A
  naive `ref e.x` through a null entry alias would create a **third** behavior (address arithmetic
  on a byref does not fault; the callee runs, performs side effects through its other ref
  parameters, registers defers that can then *catch* the eventual fault — a recover that can never
  fire today or in Go). **The doctrine: lowered field/element address formation is eagerly
  nil-checked** — a one-branch, zero-allocation golib helper (`nonnil(ref e)` raising Go's
  nil-pointer panic on `Unsafe.IsNullRef`) — landing the lowered form exactly on Go: eager panic,
  later arguments unevaluated. This is a deliberate, documented convergence *toward Go* from
  today's already-divergent deferral, guarded by a differential test against `go run` in **both**
  nil spellings.
- *Plain pointer variable, `f(q)` with nil `q`.* Go enters the callee and panics at first
  dereference. `ref q.DerefOrNull()` binds the identical null ref one frame earlier and the fault
  still happens at first callee use — here the byte-for-byte claim survives, for the standard-box
  spelling. For `q` holding a *field-ref box over a nil parent* (`q := &e.x` taken earlier under
  the NilBox spelling — legal today, no panic at formation), `DerefOrNull` resolves the parent
  eagerly and the fault moves from the callee's preamble to the caller's argument evaluation —
  which is **Go's ordering relative to the call** (Go panicked earlier still, at the original
  `&e.x`). Within-statement argument order can therefore differ from today (later argument side
  effects unevaluated where today they ran); in every such case the lowered behavior sits at or
  between Go's and today's, never outside both. Recorded, and covered by the nil-timing guard.

**Box-argument rooting, recorded honestly.** Today `f(Ꮡx)` happens to root the box for the call's
duration; `f(ref x)` roots only the storage the byref points into. For a box that earlier handed
its address to a still-pending native operation, argument-position rooting was never a guarantee
(JIT liveness can already collect the box after its last mention), and every such shape is
X3-excluded from lowering anyway — but the accidental-root narrowing is recorded so Phase B's box
work does not disturb the pin/finalizer coupling unknowingly.

**C# feasibility, verified against the language rules and the generated code:** a ref-returning
invocation is a valid `ref` argument (C# 7.3+); `DerefOrNull` is a `ref T`-returning extension
(not `ref readonly`); `ref e.x` through a `ref` local is legal; the `slice<T>`/`array<T>`/named-
array-wrapper indexers and the go2cs-gen promoted/forwarded members are `ref`-returning (the
panel verified each template); `ref` parameters are legal on static methods, in generics, beside
tuple returns, and inside the post-r41 inline GoFrame try/finally defer emission. The
classification's X2/X5 exclusions coincide with the places C# rejects a `ref` (capture, storage),
so **the C# compiler is a backstop for classification bugs** — with the panel's caveat adopted:
the backstop fires at corpus-build time, possibly in a *different assembly* than the cause (the
linkname shape), which is why the A1 census dry-runs both classification and call-site argument
shapes corpus-wide before any emission changes (§9).

### 3.4 The worked example — and the readability claim

Go (`fiat/p224.go` / `p224_fiat64.go`):

```go
func (e *P224Element) Sub(t1, t2 *P224Element) *P224Element {
    p224Sub(&e.x, &t1.x, &t2.x)
    return e
}
func p224Sub(out1, arg1, arg2 *p224MontgomeryDomainFieldElement) { ... }
```

Today's emission (charges 3 field-ref boxes per call; the callee's locals charge 2 objects each):

```csharp
public static ж<P224Element> Sub(this ж<P224Element> Ꮡe, ж<P224Element> Ꮡt1, ж<P224Element> Ꮡt2) {
    p224Sub(Ꮡe.of(P224Element.Ꮡx), Ꮡt1.of(P224Element.Ꮡx), Ꮡt2.of(P224Element.Ꮡx));
    return Ꮡe;
}
internal static void p224Sub(ж<p224MontgomeryDomainFieldElement> Ꮡout1, ...) {
    ref var out1 = ref Ꮡout1.DerefOrNull();
    ...
    ref var x200 = ref heap(new uint64(), out var Ꮡx200);   // 2 objects
    p224CmovznzU64(Ꮡx200, ((p224Uint1)x199), x190, x181);
}
```

Lowered emission (charges nothing on this path — Go-parity for classes 1 and 2; the `nonnil`
guards elide where the base is a just-dereferenced receiver alias, non-nil by dominating check):

```csharp
public static ж<P224Element> Sub(this ж<P224Element> Ꮡe, ж<P224Element> Ꮡt1, ж<P224Element> Ꮡt2) {
    p224Sub(ref e.x, ref t1.x, ref t2.x);
    return Ꮡe;
}
internal static void p224Sub(ref p224MontgomeryDomainFieldElement out1, ...) {
    ...
    var x200 = default(uint64);
    p224CmovznzU64(ref x200, ((p224Uint1)x199), x190, x181);
}
```

`ref` reads as Go's `&` (one glyph position, same argument order), the signature reads as Go's
`*T` parameter, and the entry preamble disappears because the parameter *is* the alias. This is
strictly closer to the Go source than today's form — the readability goal is paid, not taxed.

### 3.5 Where the pass lives, and the three-driver rule

A new package-scope analysis (`refLoweringAnalysisOperations.go`): after type checking, walk every
function body once collecting per-parameter use kinds (D/X classification) **and per-call-site
argument shapes** (the §3.3 rows, the defer/go carve-out, and an "other" bucket that vetoes), run
the two-sided fixed point, and record the lowered set on the package context the emission visitors
read. Per the charter's lesson, it is wired into **all three conversion drivers** (normal,
`-tests`, hand-owned-sibling) — a pass wired into one silently no-ops in the others.

**Determinism across emissions (the `-tests`-closure invariant):** classification reads **only the
production package's own files** — never `_test.go` — so the `-stdlib` and `-tests` emissions of
production sources agree by construction, and the standing closure-drift classes gain no new
member. This must hold against the merged white-box package (the `-tests` driver type-checks
`_test.go` bodies in the same package — a naive "walk every body" classifier would see
`export_test.go`'s `var X = unexportedFn` func-value alias and un-lower what `-stdlib` lowered),
so it is **guarded, not just stated**: a pipeline guard asserts classification-set equality between
the two conversions of a package containing exactly that alias shape (§8). A white-box test that
*calls* a lowered function simply emits `ref` at the lowered positions; one that takes it as a
func value gets a converter-emitted adapter lambda with the boxed shape at that use site.

**Hand-owned files are an audit obligation, not a bystander.** The 44 `[module:
GoManualConversion]` files and 26 `*_impl.cs` companions compile against the boxed convention and
are never regenerated; a lowered callee they reference breaks the corpus build with no gate that
schedules the hand edit. The A1 census therefore cross-references every candidate against the
hand-owned file set, and any function referenced from one is either X-excluded or its hand-own
edited in the same regen commit — decided per instance, in the census report.

**Multi-platform (layout L3):** classification is per-target like every other analysis — and it
**propagates**: a shared (flat) caller of a callee whose per-GOOS bodies classify differently
becomes platform-varying itself and migrates into per-GOOS folders at the merge. Existing
machinery handles it; the A1 census runs per-target and reports the L3-set delta so the merge
churn is priced, not discovered.

### 3.6 What Phase A pays — projection against the measured bill

⚠ **Re-derived after the panel's generality lens decomposed the Select path** (§11, G-F1): the
first draft's "~24,000 residual" was an incoherent midpoint between two real outcomes. Priced
against the r56d P256 rows, with the panel's measured sub-decomposition (element `Select`s =
(64 + 64) × 15 × 3 = 5,760/run; × 3 `of()` + × 3 conversion boxes = 17,280 + 17,280; the model
reproduces r56d's ScalarMult phase figure to ~3 %):

| Class | objects/run today | after Phase A (recommended branch) | why |
|:--|--:|--:|:--|
| address-taken locals (box + slot) | 76,513 × 2 = 153,026 | **~0** | fiat64's locals feed `p224CmovznzU64`-shaped lowered positions exclusively; survives even the veto branch |
| field-ref boxes at plain lowered positions | ~46,506 | **~0** | §3.3 rows 1–4 |
| conversion-shape boxes (class 3a: `of()` + `Ꮡ` pairs at `Select`/`ToBytes`/`FromBytes` sites) | ~34,560 | **~0** | §3.3 row 5's hoisted-temp rule |
| `@new<T>()` boxes (class 3b) | ~3,600 | unchanged | real Go allocations — Phase C territory |
| `array<T>` backings | 3,386 | unchanged | the array model — Phase C territory |
| **total** | **241,077** | **~7,000 (−97 %)** | |

**The branch structure is explicit because it is the design's own falsifier.** §10.3 ruled FOR
the temp rule, so the recommended branch is the committed one — but the veto branch stays
recorded: had row 5's mechanism been rejected, the conversion sites would keep boxed callees and
the residual would be **~41,500 (−83 %)**, failing the acceptance row below, which is keyed to
the recommended branch. The A1 census re-derives this table from the real corpus before any
golden moves, and a census result materially below the projection re-opens the branch question
rather than quietly shipping the smaller win.

Sharper, falsifiable unit targets: `P224Element.Mul`/`Add` (960 B/op), `Sub` (528), `Square` (832)
**and `Select` (~1,344 B/op — the conversion-shape probe the first draft's target list precisely
avoided)** must measure **0 B/op** after Phase A. A `SetBytes` probe is measured and its residual
must re-decompose to 3b + backings + its real slice allocations only.

**What Phase A does NOT pay, stated before anyone asks:** `os.File.WriteString`'s 3,168 B residual
moves **≈ 0** — its bill is receiver-position chains (`f.pfd.Write()` receivers stay `ж`; §3.7),
the syscall seam (whose addresses cross to native and are X3-excluded by definition), and
`GoFunc`/defer frames (a different arc item). `log`'s 4-vs-1 and `net/http/internal`'s 2-vs-1 are
`@string`/compiler-emitted shapes, not lowerable pointer traffic. `math/big`'s rows sit between:
`nat` arithmetic allocates through slices (real allocations on both sides) with *some* lowerable
pointer traffic; measured at the gate, promised nothing. And the static census the panel took
(1,822 `heap(` sites / 7,839 `of(` sites corpus-wide, single-digit density in most non-crypto
packages, dominance of receiver-position traffic) says the corpus-wide count win is
**concentrated where the bill is** — fiat-shaped leaf math — which is exactly where the measured
prize lives, and no broader claim is made.

### 3.7 Widening — the phases beyond A, sketched for direction (not committed)

- **A′ (same mechanism, wider set):** exported package-level functions. Callers exist in dependent
  assemblies, so the classification must be stable across the corpus (it is — it reads only the
  declaring package) and the NuGet-published corpus pairs converter and stdlib versions already.
  The `-recurse` end-user path converts against the *published* stdlib, so a converter and its
  matching corpus always agree on signatures. Risk is API-shape churn, not correctness.
- **B′ (methods, dual emission):** direct calls on a statically-known receiver could bind a
  `ref`-receiver overload (`this ref P224Element`) emitted *beside* the `ж` method, which stays
  for interface dispatch, method values, and generic constraints (`nistPoint[P]`).
  ⚠ **Re-aimed by the panel** (§11, G-F4): B′'s constituency is **not nistec** — the nistec point
  files contain zero receiver-position `of()` traffic (their fields are already `ж`-typed; what
  allocates there is class 3b, Phase C's problem). B′'s real constituency is the corpus's
  receiver-chain traffic — `runtime`'s proc (387 sites × 3 GOOS), `net/http`'s h2_bundle (212),
  `database/sql` (154), and `os`'s `of()` chains, with the os WriteString probe as its instrument.
  Dual emission doubles surface — it needs its own design increment and its own measurement, and
  it is NOT part of this sign-off.

---

## 4. Mechanism C — the box itself (the r39 item-1 territory), re-scoped by a measurement

The r39-osalloc arc left "split ж's four kinds" as chip-class item 1. This design *narrows* that
item with one new measured fact and one lifetime argument.

**The measured fact (probe, 2026-08-10, this lane — net9.0 JIT and Native AOT both):** the CLR
**can** pin a reference-free class instance directly — a sealed class with one unmanaged field, a
*generic* class over an unmanaged `T`, even a boxed struct all take `GCHandle.Alloc(…, Pinned)`
successfully on both runtimes. The long-standing assumption that only arrays could serve as
pinnable storage is **false** on the runtimes go2cs targets.

**The lifetime argument that kills the obvious use of it:** pinning the box itself cannot replace
the eager `T[1]` slot, and the reason is rooting, not pinnability. A pinned GCHandle is a *strong*
handle: it roots its target. The current design works because the pin target (the slot) and the
lifetime owner (the box) are different objects — when the box dies, the `PinnedBuffer` it holds
finalizes and frees the slot's handle. If the box *is* the pin target, the handle roots the box,
the box can never become unreachable, and nothing can ever observe the death that would free the
handle: **a structural leak**. Lazy slot allocation fails independently (the `heap()` `ref` alias
means storage must never migrate — `m_slot`'s own commentary). Conclusion: **the 2-objects-per-
unmanaged-box shape is structural under the boxed pointer model.** The way to reduce class 2 is to
not mint the box (Mechanism A), not to slim it.

**What survives as Phase B (golib-only, count-neutral except one row, byte-significant):**

1. **Flatten the two nullable tuples into plain fields** (~28 B per box, every box in the corpus).
2. **Per-kind representation** so the three non-standard kinds stop carrying an inline `m_val` of
   the pointee type (`ж<FD>` is 608 B *for a pointer*; a field-ref box needs ~40 B).
   ⚠ **The dispatch mechanism is an open engineering choice with a named regression risk** (§11,
   P-F2): `Value`/`ValueSlot` are today *non-virtual* branch chains the JIT folds and inlines;
   subclass-virtual dispatch is an indirect call that **Native AOT can never devirtualize** (no
   tiering, no GDV) and that blocks inlining at every exported-function entry preamble and
   receiver chain corpus-wide. The alternative that takes the bytes without the indirection: keep
   one class, flatten the tuples, add a `byte` kind discriminator + switch, and slim per-kind
   *storage* via an `object`-typed union slot. **B1's precondition is a three-variant microbench**
   (current / flattened-switch / subclass-virtual) of `Value`/`ValueSlot`/`DerefOrNull` on
   standard-box-dominant and mixed-kind workloads, JIT (warmed, PGO) **and** Native AOT — the
   virtual variant lands only if it is ≤ the current form on both runtimes. The bench must include
   a reinterpret/native-kind case, because:
3. **`unsafe.Pointer` subclasses `ж<T>`** and can *be* any kind today (kind is per-instance data);
   kind-as-type forces it to compose or forces the base to retain kind fields. **B1's design must
   state the `unsafe.Pointer` representation before implementation** — it has perf teeth on
   syscall-heavy packages and correctness teeth on the reflect bridge.
4. **A typed element-ref path** so `Ꮡ(s, i)` stops boxing the `slice<T>` header into `IArray<T>`
   (the census's one caller-side charge): an element-ref kind holding `(T[] backing, nint index)`
   directly, with `slice<T>`/`array<T>` overloads. Removes **1 counted object per `&s[i]`**
   corpus-wide and makes canonicalization (`CanonicalElement`) construction-time instead of
   per-comparison (a small added cost on never-compared boxes — accepted; the mint's allocation
   dominates it, and Mechanism A deletes many of these mints outright).

Phase B's acceptance is byte-measured (the os probe's 1,488 B box line and `ж<FD>`'s 608 B) plus
the `&s[i]` count row **plus the wall-clock microbench above** — the panel found the first draft
allowed a time regression to land formally unmeasured. Its census updates ride the same commit
(the counter tests assert exact per-site charges and will move deliberately).

---

## 5. Mechanism B — field-ref box interning: evaluated, deferred

Go's `&e.x` yields the same address every evaluation, so memoizing the field-ref box per
`(source box, accessor)` is *semantically faithful* — it makes converted pointer identity
strictly MORE Go-like (`&e.x == &e.x` is already true today via `Equals`, but interning would make
it reference-true). It was the board's first-listed candidate. Deferred, for three reasons:

1. **Dominated on the measured workloads.** The nistec field-ref traffic feeds lowered positions
   and vanishes under Mechanism A without any cache. What remains of class 1 after A is the
   corpus's receiver-position traffic (runtime/net/http/sql/os — the census in §3.7) — Phase B′'s
   territory, where the receiver overload also allocates nothing.
2. **It taxes the fast path to pay the counted one.** A `ConditionalWeakTable` (or per-box cache
   field) lookup costs more than the gen0 allocation it avoids on short-lived boxes; wall-clock
   perf would *regress* on exactly the hot paths the perf table watches, to improve a number only
   `AllocsPerRun` sees.
3. **Lifetime coupling.** An interned box (and any pin it acquires) lives as long as its parent —
   the pinning-lifetime change the board flagged when it routed this idea to design-with-user.

**Resurrection bar:** a workload where post-A, post-B′ field-ref traffic still dominates a counted
assert, measured by the counter, plus a cache design whose miss cost is at or below the allocation
it replaces.

---

## 6. Mechanism E — value carriers and inline layouts (Phase C horizon, direction only)

Classes 3b and 4 are real Go allocations that Go elides with compiler machinery the CLR does not
give us (stack allocation of escaping-looking locals after inlining; inline fixed arrays). The
managed analogues exist but each crosses a model boundary:

- **`[InlineArray]` storage for small unmanaged Go arrays** would delete class 4 and make by-value
  array copies Go-true without `.Clone()` — but `slice<T>` requires a `T[]` backing, so any array
  that is ever *sliced* (`e.x[:]`) or aliased as a slice cannot take inline storage without a
  backing-abstraction rewrite of `slice<T>` itself. That rewrite is the real cost, and nothing in
  this arc's bill justifies it alone. ⚠ **Recorded precondition from the panel** (§11, S-F6):
  inline layouts create the large field offsets at which a null-byref dereference surfaces as an
  unmappable `AccessViolationException` instead of the managed panic — every null-deferring byref
  path must be re-audited against the 64 KB null partition before any inline-layout increment.
- **Value-typed carriers for provably-contained objects** (Go's `new(T)` whose pointer never
  escapes a frame, nistec's point/table locals) would delete class 3b — but a `P224Point` holds
  `*P224Element` *fields*, so containment analysis must extend through stored pointers
  (whole-object escape analysis), and a value-carrier `P224Point` diverges from Go's own layout.
  This is re-implementing Go's escape analysis; it is the only road to nistec's literal zero, and
  it is emphatically its own design document if it is ever wanted.

The distance Phase C must close shrank under the panel's re-decomposition — **~7,000 objects on
P256 after Phase A** (3b + backings), not the first draft's ~24,000 — which materially changes
whether Phase C is ever worth its cost, and that judgment is deliberately left to the day it has
A3's measured numbers in hand. Phase C is recorded as direction so the phases before it are judged
against the right horizon; it is **not** scheduled, and nistec's want-zero rows stay unbanked
until it exists or a better idea does.

---

## 7. Measurement plan — the counter is the gate instrument, plus the wall-clock gap the panel closed

Every increment lands with a before/after taken by the **same instrument the tests see** (the
pipeline's `AllocsPerRun` object counts, plus the golib counter on targeted probes). The named
workloads:

| Workload | today (objects unless noted) | Phase A acceptance | A3 measured (2026-08-13, pinned laptop R, go1.23.1) |
|:--|--:|:--|:--|
| nistec `TestAllocations/P256` | 242,665 /run | ≤ 10,000 (recommended-branch projection ~7k) and the residual re-decomposes to classes 3b + 4 ONLY | **8,528 /run (−96.5 %) — MET**; 733,766 B/run; P224 8,484 · P384 12,572 · P521 17,090; residual = 3b + backings + the §6.3 wrapper keeps (classes 1/2/3a at zero — see the phase table in the board's A3 section) |
| fiat `P224Element.Mul` / `Add` / `Sub` / `Square` / **`Select`** | 960 / 960 / 528 / 832 / ~1,344 B/op | **0 B/op, all five** (`Select` is the conversion-shape probe) | **0 / 0 / 0 / 0 / 0 B/op (0 obj/op) — MET** |
| fiat `SetBytes` probe | measured at A1 | residual = 3b + backings + real slice allocations only | **1,016 B · 12 obj/op, closing exactly**: 3 × 3b (`minusOneEncoding` news) + 5 backings + `in`'s kept box (2) + `Bytes`-chain `out` kept box (2); `Bytes` 232 B · 3 obj (`out` keep 2 + tmp backing 1) — the A1-named terms, nothing else — MET |
| `io` `TestMultiWriter_WriteStringSingleAlloc` | exactly 1 (PASSES) | **still exactly 1** — the do-no-harm canary | **still exactly 1** — `io` swept clean 2026-08-13 at 60 matched / 1 disclosed, canary among the matched — MET |
| full validated sweep (110 pkgs, 13,628 verdicts) | banked counts | zero drift; no AllocsPerRun row increases | **zero drift** — the 2026-08-13 sweep at 129 pkgs / 14,712 verdicts; the one count catch was sync's three Once disclosures RETIRING (44/7, the L11 improvement) — MET |
| `os.File.WriteString` probe | 3,168 B/op | unchanged (±0) — claimed, so a movement either way is a finding | **2,368 B/op (17 obj) — moved −800 B, a FINDING**: favorable; the 3,168 stamp predates r41's inline-defer retirement of the 440 B GoFunc/defer term and A2 — per-term re-attribution owed to the next os re-instrumentation, not asserted here |
| `math/big` `TestMulUnbalanced` ratio, `TestNewIntAllocs` | fails / 1 vs 0 | measured and reported; no target promised | TestMulUnbalanced **20,499,128 B vs 20,416,320 budget (51.21×)** — +0.06 % vs r58b, unmoved, as §3.6 forecast for slice-backed `nat` arithmetic; TestNewIntAllocs **still exactly 1 obj/run** on all seven `NewInt` shapes (class 3b); suite 224/226 unchanged |
| perf suite (JIT + AOT) | README table | see the wall-clock protocol below | measured at A3 — see `src/tests/Performance/README.md` (PERF-RESULTS) and the board's A3 section for AOT publish size + ILC wall time |

**The wall-clock instrument — a panel-mandated addition** (§11, P-F1/P-F3/P-F4). The existing
perf suite is structurally blind to this arc: 12 of 13 transpiled benchmarks contain **zero**
`ж`/`Ꮡ`/`heap(` occurrences, so the "no row regresses" gate is vacuous in both directions —
Phase A's win cannot show, and a Phase-B dispatch regression on golib's hottest member would PASS
all 13 rows. Therefore:

1. **A ж-bound benchmark joins the perf suite before A2 lands** — a fiat-shaped hot loop
   (pointer params + address-taken locals + field addresses, ≥50 ms, deterministic), so the
   JIT+AOT gate actually exercises the mechanism under change.
2. **The perf gate for this arc is a paired, same-machine, same-session A/B** — pre-change and
   post-change binaries interleaved on the current coordinator machine, N ≥ 10, regression =
   median delta > 5 % *and* outside the interleaved spread. Never README-table-vs-README-table:
   the committed table's baseline machine (the i9) is dead, and cross-machine ratio comparison
   shows phantom regressions by construction.
3. **Wall-time claims come only from counter-disabled binaries** (counting-ON costs 11–15 % on
   tight allocation loops and the overhead is proportional to exactly the allocations Phase A
   removes — quoting counter-on numbers would overstate the win).
4. **A3 additionally records AOT publish size and ILC wall time per benchmark** — free data from
   runs already mandated, and the baseline B′'s dual-emission increment will need.

The perf-suite AOT column is **mandatory** at the Phase A gate: `ref`-heavy emission changes what
ILC sees, and the corpus's only AOT execution is the perf Verify phase (the array-backing
materializer lesson — no JIT-hosted gate can see an AOT-only defect).

---

## 8. Blast radius and gates (charter §5)

**Change class: converter + a minimal golib addition** (Phase A — the `nonnil` helper is new golib
surface; the recommended row-5 temp rule keeps go2cs-gen untouched, and the alternative gen
accessor would widen this to go2cs-gen, which is part of why the temp rule is recommended):

| Gate | Expectation |
|:--|:--|
| `check-no-regression.ps1` | **Massive intended re-baseline** wherever lowering fires. Every affected golden is re-baselined deliberately (`UpdateTestTargets --createTargetFiles` after a fresh transpile), and the change rides its own corpus regen commit per the r40 rebank pattern — never a scattered partial rebank. |
| Corpus reconvert + `go2cs-stdlib.slnx` build | 0 errors; the C# compiler is the classification backstop (§3.3), so a classification bug surfaces HERE as a CS error, not downstream — with the caveat that a linkname-shaped miss surfaces in a different assembly than its cause, which the A1 census exists to preclude. |
| Full behavioral suite | green, including the new guards below. |
| `run-validated-sweep.ps1` (full roster) | zero count drift across all 110 — the all-ships-rise proof. Backgrounded from the coordinator per the standing kill rules. |
| `go2cs.slnx` build | once, before banking (golib gains the `nonnil` helper). |
| `go test ./...` in `src/go2cs` | includes `projitemsIntegrity_test.go` — the new `refLoweringAnalysisOperations.go` must be registered in `go2cs-src.projitems`. |
| Hand-own audit | the A1 census's cross-reference of lowered candidates against the 44 marked files + 26 `*_impl.cs` companions resolves to zero unhandled references before A2 (§3.5). |

**New behavioral guards (Phase A lands with all of these):**

1. `RefLoweredParams` — write-through visibility, forwarding chains (D2), an address-taken local
   both lowered and *kept* (one surviving box use) in the same package.
2. Nil timing, differential against `go run`, in **both** nil spellings (null reference and
   canonical NilBox): the eager field-address panic (`f(&e.x)` with nil `e` — later arguments
   must be unevaluated, matching Go), the plain-variable deferred fault (`f(q)` with nil `q` —
   panic at first callee use), and a nil-*guarding* callee (X-excluded) still tolerating nil —
   the guard proves the classification boundary, not just the mechanism.
3. `defer f(&x)` / `go f(&x)` of a lowered function writing through the address — the boxed-site
   carve-out, differential against `go run` (the copy-box emission returns 0 where Go returns 7;
   this guard is what keeps that emission unshippable).
4. A func-value use of an otherwise-lowerable function (X5) — proves the exclusion and the
   test-side adapter shape — plus the **classification-equality guard**: the `-stdlib` and
   `-tests` conversions of a package containing an `export_test.go` func-value alias must produce
   identical production signatures (§3.5).
5. `ConversionStrategies.md` + `-Reference.md` updated in the same change (the emitted-form rule:
   *a pointer parameter whose every use is a dereference is a `ref` parameter*).

**Documented risks, with the honest severity:**

- **Golden churn is the biggest operational cost, not correctness.** Same class as r40: one
  deliberate regen, classified drift only.
- **Classification soundness** rests on the whitelist argument of §3.2 (any unrecognized use
  disqualifies), backed by the compiler backstop and the A1 shape census — the three layers catch
  different miss classes (analysis bugs, emission-shape gaps, cross-assembly shapes).
- **Debug/step experience** changes shape (no box object to inspect for lowered params) — accepted;
  it matches what Go shows for a stack variable.
- **`-tests` closure invariance** argued in §3.5 and guarded (guard 4); the sweep is the proof.

**Change class: golib** (Phase B) — full behavioral suite + full sweep + the §4 dispatch
microbench; counter census rows updated in the same commit; `unsafe`'s `ж` subclassing
representation stated in the B1 design before implementation; the reflect bridge re-validated
(`reflect`, `internal/reflectlite`, `errors`, `encoding/binary` in the roster cover the bridge
paths).

---

## 9. Staged landing plan

| Stage | Content | Gate | Bank |
|:--|:--|:--|:--|
| **A1** | Classification pass (analysis only) + a `-debug` census: per package, (a) how many params/locals lower; (b) **per-call-site argument shapes** (§3.3 rows 1–7 / defer-go / other-veto); (c) hand-own + linkname cross-references; (d) per-GOOS classification deltas (the L3 propagation); (e) coverage of BOTH fiat families — nistec/fiat AND `crypto/internal/edwards25519` (the corpus's second fiat family, 0/55 on the init-ORDER arc so unbankable either way, but the generality pricing belongs in the census); (f) **classification of the 347 exported-candidate functions** (same pass, one flag — the §10.1 ruling's decision input for A′ at the checkpoint; measured 2026-08-10: 115 in `internal/*`, 80 in `sync` [largely the hand-owned atomic surface], 46 in `syscall` [X3-dominated], 26 constructor-shaped/X2). No emission change. | converter tests; CNR byte-identical (nothing emits differently) | census report on the board — §3.6's table re-derived from the real corpus, branch chosen, BEFORE any emission changes |
| **A2** | Emission: lowered signatures, call-site `ref` args (all seven rows), the `nonnil` helper + eager field-address nil doctrine, plain locals, defer/go boxed-site carve-out, the behavioral guards, docs pair. **Plus the ж-bound perf benchmark** (§7 item 1). | full §8 ladder + the paired A/B wall-clock protocol | converter commit + the deliberate corpus regen commit (r40 pattern) |
| **A3** | Re-measure §7's table; update the board's nistec/math-big rows with measured numbers; perf suite re-run (solo, AOT included, paired protocol; record publish size + ILC time). | sweep + perf Verify | measurement commit |
| **B1** | golib kind work per §4 — tuple flattening + storage slimming + typed element-ref — with the three-variant dispatch microbench as precondition and the `unsafe.Pointer` representation stated first. | golib ladder + microbench | golib commit; census update |
| — | **Checkpoint with user**: A′/B′ widening (exported functions; method dual emission) is a NEW design increment, presented with A3's measured numbers. | | |

Each stage is individually shippable and individually revertible; A1 in particular is pure
analysis and exists so the projection AND the emission-shape table are validated against the whole
corpus before any golden moves — if the census says the win is smaller than §3.6 projects, or that
an argument-shape class is bigger than priced, that finding surfaces at zero cost.

---

## 10. Decisions — RATIFIED (user, 2026-08-10)

All six were presented as open questions with recommendations; the user ratified each on
2026-08-10, per the refined forms below. These are rulings, not proposals.

1. **Phase-A scope — RULED: unexported package-level functions only**, with the refinement that
   the A1 census ALSO classifies the 347 exported-candidate functions and counts their call-site
   traffic (§9 item f), so A′ is decided at the checkpoint on measured numbers rather than now.
   If a half-step proves justified, the natural scope is **`internal/*` packages** (115 of the
   347 — cross-assembly within the corpus, never end-user API, so the published-surface cost
   vanishes and the cross-package metadata stays corpus-internal). If the census shows the
   `sync/atomic` cluster dominating the exported win, that is its own small arc — it is tangled
   with hand-owned files, not converter emission.
2. **The corpus regen — RULED: confirmed.** Phase A rides one deliberate whole-corpus rebank
   commit (the r40 pattern); the golden churn is expected and classified, never scattered.
3. **The conversion-shape mechanism (§3.3 row 5) — RULED: the hoisted-temp rule** (converter-only,
   byte-parity with today's copied-header semantics). The go2cs-gen ref-returning accessor on the
   `[GoType]` named-array wrappers remains the recorded fallback if the temp rule disappoints at
   A1/A2. The projection's recommended branch (~7k residual) is priced on this ruling.
4. **The nil doctrine (§3.3) — RULED: the eager field-address nil check**, converging lowered
   sites on Go's panic timing where today's emission defers. Lowered and unlowered sites differ
   in a corner today's corpus never exercises (nil base at a field-address argument); the
   differential guard holds both to Go.
5. **Phase ordering — RULED: A before B.** A's census (A1) may shrink B's remaining value, and B1
   carries the dispatch-microbench precondition either way.
6. **Sequencing — unchanged standing ruling, acknowledged:** implementation starts after the
   near-miss harvest completes, with the start trigger confirmed via the coordinator session.
   This document's sign-off covers the design; it does not start the clock.

---

## 11. Adversarial review record (2026-08-10)

Four independent skeptic lenses, each prompted to refute the first draft (charter §7). Every
CONFIRMED finding below changed the document; the section that absorbed it is cited. Findings the
panel itself refuted are recorded as sound, because they are load-bearing negative results.

### Lens 1 — Go-semantics fidelity (S-F*)

- **S-F1 (CONFIRMED, adopted):** the draft's "nil semantics preserved byte-for-byte" was false —
  it misdescribed today (the entry preamble faults at callee ENTRY for a NilBox-spelled nil, and
  in the CALLER for a null-spelled one — not "at first use"), and a naive `ref e.x` through a null
  alias would create a third behavior in which callee side effects precede the panic and a
  callee's `recover` can catch a panic it can never catch today or in Go (worked example: Go
  prints 0, lowered-naive prints 7). **Fix adopted as doctrine:** eager `nonnil` check at lowered
  field/element address formation, converging on Go exactly → §3.3, §10.4, guard 2.
- **S-F2 (CONFIRMED, adopted):** `DerefOrNull` on a field-ref box over a nil parent throws during
  caller argument evaluation (not at the callee preamble), so within-statement argument order can
  differ from today — always moving toward Go's ordering. Recorded as doctrine in §3.3.
- **S-F3 (RISK, adopted):** `defer f(&x)` / `go f(&x)` call sites were unhandled; the natural
  copy-box fallback compiles and silently loses writes. **Fix:** boxed-site carve-out (§3.3) +
  guard 3.
- **S-F4 (RISK, adopted):** call sites DO constrain classification — the pointer-conversion
  argument shape has no callee-side signal. **Fix:** the two-sided fixed point with caller-side
  emittability veto (§3.2) and row 5 (§3.3).
- **S-F5 (RISK, recorded):** `ref` arguments stop accidentally rooting the box during the call;
  never a guarantee, X3 keeps the affected class boxed — recorded in §3.3 so Phase B doesn't
  disturb the pin/finalizer coupling unknowingly.
- **S-F6 (NITPICK now, Phase-C precondition):** the ≥64 KB null-byref offset AV hazard is
  unreachable in today's corpus (heap-backed arrays keep offsets tiny) but becomes real under
  inline layouts → §6.
- **S-N1..N5 (adopted):** D1′ added; receiver-method-call named in X3; range/switch classified;
  the reversion predicate consumes the full use-census; the circular "X3 routes through Ꮡp"
  soundness argument replaced by the whitelist argument (§3.2).
- **Sound:** byref aliasing (incl. `f(&x,&x)` and overlapping-ref wholesale writes), argument
  evaluation order, pointer-identity non-observability under the whitelist, D2 fixed-point
  direction, temporaries' GC-safety, reference-typed pointees, the compiler backstop for X2/X5,
  reverted-local closure capture, `ref s[i]` bounds-panic parity, zero-value init, `-tests`
  determinism.

### Lens 2 — performance regression (P-F*)

- **P-F1 (CONFIRMED, adopted):** the perf suite is structurally blind to ж (zero occurrences in
  12 of 13 benchmarks) — the draft's "no row regresses" gate was vacuous in both directions, and
  §1 misattributed MatMul/Sort to this arc. **Fix:** attribution withdrawn (§1); ж-bound
  benchmark added before A2 (§7).
- **P-F2 (RISK, adopted as B1 precondition):** Mechanism C's "virtual dispatch replaces branch
  chains" is a likely regression on the standard-box 90 % case and is worst on Native AOT (no
  tiering/GDV; `Value`/`ValueSlot` are today non-virtual and inline). **Fix:** three-variant
  microbench precondition + the kind-field/switch alternative (§4).
- **P-F3 (CONFIRMED, adopted):** Phase B had no wall-clock gate at all. **Fix:** §4 acceptance +
  §7 protocol.
- **P-F4 (RISK, adopted):** "run noise" was undefined and the README baseline machine is dead.
  **Fix:** paired same-machine interleaved A/B, N ≥ 10, stated threshold (§7).
- **P-F5 (RISK, adopted):** `unsafe.Pointer`-subclassing vs kind-as-type must be resolved in B1's
  design first (§4).
- **P-F6..F8 (NITPICKs, adopted):** construction-time canonicalization cost noted (§4);
  AOT size/ILC time recorded at A3 (§7); counter-on binaries never quoted for wall time (§7).
- **Sound (and better than the draft claimed):** ref-args vs register pressure/GC frame scanning
  (lowered frames hold the same or fewer byrefs, and ~23 MB/run of gen0 churn disappears at P224
  — the draft *undersold* the GC win); inlining improves (today's `DerefOrNull`→`ValueSlot` chain
  is a poor inline candidate, the lowered helpers are trivial); `ref q.DerefOrNull()` never pays
  more than today's preamble and D2 chains pay strictly less; no lowered shape adds a counter
  call; Phase A shrinks AOT size; the io canary is well-chosen.

### Lens 3 — corpus-compile (C-F*)

- **C-F1 (CONFIRMED, adopted):** the pointer-conversion argument (`(*[4]uint64)(&v.x)`, 16 sites
  across the four curve files) has **no legal `ref` emission** under the generated wrapper API
  (get-only lazily-initializing by-value `Value` property → CS0206/CS0122/CS1503 on every
  candidate). **Fix:** row 5's hoisted-temp rule with the byte-parity argument (§3.3), the gen
  accessor as the recorded alternative (§10.3).
- **C-F2 (CONFIRMED, adopted):** `f(&T{…})` lowers to `ref new T(…)` — invalid C#; live corpus
  instance found (`archive/zip` `readDirectoryHeader(Ꮡ(new File(nil)), …)`, whose callee body
  verifiably classifies lowerable). **Fix:** row 6's temp rule, justified from the callee's own
  classification (§3.3).
- **C-F3 (RISK, adopted):** the defer/go frame machinery cannot store a managed `ref`; same
  resolution as S-F3 (§3.3, guard 3).
- **C-F4 (RISK, adopted):** `//go:linkname` publicizes unexported functions across assemblies
  invisibly to a per-package scan — today's one instance is saved by accident (X3), nothing
  structural protects the next registry entry. **Fix:** the linkname strip rule in X5 (§3.2).
- **C-F5 (RISK, adopted):** bodiless (`Body == nil`) functions classify *vacuously* lowerable and
  would orphan hand-written `*_impl.cs` companions; hand-owned callers compile against the boxed
  convention forever. **Fix:** bodiless exclusion in X5 (§3.2) + the hand-own audit line (§3.5,
  §8).
- **C-F6 (RISK, adopted):** §3.5's production-files-only rule needed a guard against the merged
  white-box package (`export_test.go` func-value aliases). **Fix:** the classification-equality
  guard (§3.5, guard 4).
- **C-F7 (NITPICK, adopted):** classification differences propagate platform variance into
  previously-shared files (L3 growth) — priced by the per-target A1 census (§3.5, §9).
- **C-F8 (NITPICKs, adopted):** the literal-`nil` argument row (row 7, §3.3) and the
  named-pointer-type exclusion (X5, §3.2).
- **Sound (each verified in the generated templates/golib):** `DerefOrNull` is `ref T`-returning;
  all indexers including generated named-array wrappers are ref-returning into shared backing;
  promoted/forwarded members are ref-returning (the "field is secretly a property" attack failed
  everywhere except the wrapper `Value`, which is C-F1); address-taken globals expose ref
  properties; the post-r41 inline GoFrame defer emission accepts ref params (the func()-wrapper
  attack failed — those comments are stale); rangefunc loop bodies are inline; generics/variadics/
  multi-return/`**T` all legal; no overloading → no signature collisions; the three-driver and
  projitems obligations were already carried.

### Lens 4 — generality (G-F*)

- **G-F1 (CONFIRMED, adopted):** the draft's priced workload contained the conversion shape §3.3
  could not emit, and the "~24k residual" was an incoherent midpoint between the two real branches
  (~41.5k vetoed / ~7k handled); the four unit targets were precisely the ones avoiding the
  breaking shape. **Fix:** re-derived §3.6 with the Select decomposition (element Selects
  5,760/run × 3 `of()` + × 3 conversion boxes), branch-explicit acceptance, `Select` and
  `SetBytes` added to the unit targets (§7).
- **G-F2 (CONFIRMED, adopted):** class 3 was ~83 % mislabeled — ~17,280 of the ~20,876
  "real Go allocations" are conversion boxes (emission artifacts). **Fix:** classes 3a/3b split
  (§1), Phase C's distance re-priced to ~7k (§6).
- **G-F3 (CONFIRMED, adopted):** MatMul/Sort gating withdrawn (with P-F1) — both benchmarks
  contain zero box traffic; "an attribution that was never decomposed is a hypothesis" (§1).
- **G-F4 (RISK, adopted):** B′ was mis-aimed at nistec (zero receiver-position `of()` there;
  fields are already `ж`-typed) — its real constituency is the corpus receiver chains
  (runtime/net/http/sql/os). **Fix:** §3.7 re-aimed; §5 reason 1 corrected.
- **G-F5 (RISK, adopted):** the committed phases bank **zero verdicts**, and the draft
  simultaneously gated `net/http/internal` on the arc (§1) and declared it outside Phase A
  (§3.6). **Fix:** §1's gated list split three ways with the composite deliverable stated plainly.
- **G-F6..F8 (NITPICKs, adopted):** the 83 %→89.9 %→97 % arithmetic reconciled (§2 goal 1 now
  states 97 % for classes 1+2+3a); literal-nil census row; `edwards25519` (the second fiat family)
  added to the A1 census (§9).
- **Sound:** nil-timing doctrine (verified in golib), C# feasibility rows, §3.6 row 1 (the
  address-taken-local savings survive even the veto branch), the 2,200-verdict gating currency
  (re-verified against the board's r58a re-measure), p224/p256 shape parity (`p256Sqrt`'s ~260-op
  chain is pure classes 1+2 and falls to A), Mechanism B's deferral, Mechanism C's rooting/leak
  argument, the A1-before-A2 staging, the counter's monotonicity contract, the `-tests`
  invariance argument. The generality headline: Mechanism A is a sound general rule whose measured
  prize is *concentrated* where the bill is (static census: 1,822 `heap(` / 7,839 `of(` sites
  corpus-wide, single-digit density outside crypto) — the design claims exactly that and no more.
