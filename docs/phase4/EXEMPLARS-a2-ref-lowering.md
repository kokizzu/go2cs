# The ж-box lowering, in its own emitted code — A2 exemplars

> Companion to [`DESIGN-zh-box-reduction.md`](DESIGN-zh-box-reduction.md) (the arc's design: §1 the
> measured bill, §3.3 the call-site row table, §3.4 the worked example, §11 the adversarial panel).
> Every snippet below is **verbatim from the merged behavioral goldens** — before = `c33b3a67e`
> (master before the A2 merge), after = `c28c40960` (the merge). The rebank commit `f7bf4dda2`
> classifies all 49 moved goldens into the six families shown here. Reproduce any pair with
> `git diff c33b3a67e c28c40960 -- src/tests/Behavioral/<name>`.
>
> Headline numbers, from the A2 lane report (laptop R, pinned go1.23.1): fiat P224
> Mul/Add/Sub/Square/Select at **0 B/op** (was 528–1,344); the P256 `TestAllocations` mirror at
> **10,105 obj/run vs 241,077** (−95.8 %; A3's pinned pipeline measure owns the final verdict); the
> ж-bound A/B benchmark at **652.9 ms lowered vs 6,084 ms boxed** (9.32×), taking lowered C# to
> 2.7× native Go from 24.9× boxed.

The arc in one sentence: a Go pointer parameter whose address provably never outlives the call —
not stored, not compared to nil, not captured, not handed to `defer`/`go` — is emitted as a native
CLR `ref T` parameter instead of a `ж<T>` heap box, and every uncovered shape falls back to the
boxed emission, so coverage is a win-counter, not a soundness premise.

## 1 · The signature itself — including generics

`GenericReceiverFieldAddress`. Go:

```go
func setT[T any](p *T, val T) { *p = val }

func (b *Box[T]) Set(val T) { setT(&b.v, val) }
```

Boxed emission (before):

```csharp
internal static void setT<T>(ж<T> Ꮡp, T val) {
    ref var p = ref Ꮡp.DerefOrNull();

    p = val;
}

public static void Set<T>(this ж<Box<T>> Ꮡb, T val) {
    setT(Ꮡb.of(Box<T>.Ꮡv), val);
}
```

Lowered emission (after):

```csharp
internal static void setT<T>(ref T p, T val) {
    p = val;
}

public static void Set<T>(this ж<Box<T>> Ꮡb, T val) {
    ref var b = ref Ꮡb.DerefOrNull();

    setT(ref nonnil(ref b).v, val);
}
```

The parameter *is* the alias: the `ж<T> Ꮡp` box and its `DerefOrNull()` preamble vanish, and the
lowering survives generic instantiation. The caller's row-1 field address `Ꮡb.of(Box<T>.Ꮡv)` — a
fresh 128-byte box per evaluation — becomes the allocation-free `ref nonnil(ref b).v`.

## 2 · A loop variable comes home from the heap

`ForVariants`. Go:

```go
i := 0
for i < 10 {
    f(&i)
    i++
}

func f(y *int) { fmt.Print(*y) }
```

Before → after:

```csharp
// before
ref var i = ref heap<nint>(out var Ꮡi);
i = 0;
while (i < 10) {
    f(Ꮡi);
    i++;
}
internal static void f(ж<nint> Ꮡy) {
    ref var y = ref Ꮡy.DerefOrNull();

    fmt.Print(y);
}

// after
nint i = 0;
while (i < 10) {
    f(ref i);
    i++;
}
internal static void f(ref nint y) {
    fmt.Print(y);
}
```

An address-taken local was 2 objects (the ж box plus its eager `T[1]` pinnable slot); now `i` stays
a stack local. The same project's labeled-loop variant used to mint a fresh box **per iteration**
for distinct addresses; A2 proves the requirement unnecessary there and collapses it to one plain
loop variable. Sibling exemplar in `AddressOfParamWrite`: a `[3]int` value parameter keeps Go's
copy semantics as a plain `a = a.Clone();` — no box — and `bump(&a[1])` becomes row 4's
`bump(ref a[1])`.

## 3 · Callee lowers, call site unwraps

`PointerFieldArrayElementAddress`. Go:

```go
func bump(c *cycle) { c.n++ }

func viaParam(p *rec, i int) {
    c := &p.future[i]
    bump(c)
}
```

```csharp
// before
internal static void bump(ж<cycle> Ꮡc) {
    ref var c = ref Ꮡc.DerefOrNull();

    c.n++;
}
internal static void viaParam(ж<rec> Ꮡp, nint i) {
    var c = Ꮡp.at(rec.Ꮡfuture, i);
    bump(c);
}

// after
internal static void bump(ref cycle c) {
    c.n++;
}
internal static void viaParam(ж<rec> Ꮡp, nint i) {
    var c = Ꮡp.at(rec.Ꮡfuture, i);
    bump(ref (c).DerefOrNull());
}
```

The seam between covered and uncovered shapes in one hunk: `bump`'s parameter lowers, but this
caller's `c` is still a box (it comes from `.at(…)` indexing), so the call unwraps it. Each
function makes its own deal; the convention change composes across the boundary.

## 4 · The pointer that gets reassigned

`PointerParamNilWalk`. Go:

```go
func advance(p *node) (*node, int) {
    return p.next, p.val
}
```

```csharp
// before
internal static (ж<node>, nint) advance(ж<node> Ꮡp) {
    ref var p = ref Ꮡp.DerefOrNull();

    return (p.next, p.val);
}

// after
internal static (ж<node>, nint) advance(ref node p) {
    return (p.next, p.val);
}
```

What does **not** lower is as telling: the returned `p.next` stays `ж<node>` (a pointer escaping
through a return keeps its box identity), and the caller's walk loop emits
`advance(ref (Ꮡp).DerefOrNull())` — dereferenced at each call, not bound once — so a pointer
variable reassigned between iterations always hands the callee its current target (row 3's
reassigned-pointer safety).

## 5 · Two boxes become one temp

`NamedArrayWrapper`. Go:

```go
var sm scal
fromBytes((*[4]uint64)(&sm.s), 7)  // Named→underlying reinterpret
double(&sm.s, (*nonMont)(&sm.s))   // sibling reinterpret, same storage
```

```csharp
// before
ref var sm = ref heap(new scal(), out var Ꮡsm);
fromBytes(Ꮡ((Ꮡsm.of(scal.Ꮡs)).Value.Value), 7);
@double(Ꮡsm.of(scal.Ꮡs),
        Ꮡ((nonMont)((Ꮡsm.of(scal.Ꮡs)).Value.Value)));

// after
scal sm = new();
var ᴛ1 = sm.s.Value;
fromBytes(ref ᴛ1, 7);
var ᴛ2 = (nonMont)((sm.s).Value);
@double(ref sm.s, ref ᴛ2);
```

The double-box shape (an `of()` box plus a second box over a copied array header — class 3a of the
bill, ~17,280 of P256's field-ref boxes) drops to a hoisted temp passed by `ref`. Parity argument:
the copied wrapper's header shares its `T[]` backing with the original, so writes through the temp
land in the storage Go's reinterpret would alias. The pairing is type-gated to the
identical-underlying-array family only — a string or numeric wrapper's value is a plain copy, so
those sites keep the identity box end to end. One purely type-level predicate,
`refConvPairingSupported`, decides both the census and the emission (the output gate caught the two
sides disagreeing once — `NamedNumericPointerReinterpret`'s lost write — and the shared predicate
closed that class structurally; see `171e48b5f`).

## 6 · nil keeps its panic timing

`GuardedNilPointerParamDeref`. Go:

```go
func digits(base int, invalid *int) int {
    n := 0
    for i := 0; i < 5; i++ {
        if i >= base && *invalid == 0 { // deref only behind the guard
            *invalid = i
        }
        n++
    }
    return n
}

c2 := digits(10, nil) // legal: the guard never fires
```

```csharp
// before
internal static nint digits(nint @base, ж<nint> Ꮡinvalid) {
    ref var invalid = ref Ꮡinvalid.DerefOrNull();
    ...
}
nint c2 = digits(10, nil);

// after
internal static nint digits(nint @base, ref nint invalid) {
    ...
}
nint c2 = digits(10, ref ((ж<nint>)default!).DerefOrNull());
```

A lowered parameter still has to accept Go's `nil`: the synthesized argument binds a null box and
defers the fault to the first actual use inside the callee — Go's "a nil pointer only panics when
dereferenced" timing, exactly. The `RefLoweredNilTiming` guard pins this timing against `go run`.

## 7 · The A/B benchmark

`src/tests/Performance/PerfRefLower` — new in A2, the fiat shape distilled: pointer params feeding
pointer params, address-taken locals, field addresses, twenty million iterations. Go:

```go
func cmov(out *uint64, cond, a, b uint64) {
    m := uint64(0) - (cond & 1)
    *out = (m & b) | (^m & a)
}

func mix(e, t *felem, k uint64) {
    norm(&e.x, k)
    norm(&t.x, k+1)
    for i := 0; i < 4; i++ {
        var s uint64
        cmov(&s, t.x[i]&1, e.x[i]+t.x[i], e.x[i]^t.x[i])
        e.x[i] = s
    }
}
```

Lowered emission (the file contains zero `ж`, `Ꮡ`, or `heap(` occurrences — the loop allocates
nothing):

```csharp
internal static void cmov(ref uint64 @out, uint64 cond, uint64 a, uint64 b) {
    var m = (uint64)0 - ((uint64)(cond & 1));
    @out = (uint64)(((uint64)(m & b)) | ((uint64)(~m & a)));
}

internal static void mix(ref felem e, ref felem t, uint64 k) {
    norm(ref nonnil(ref e).x, k);
    norm(ref nonnil(ref t).x, k + 1);
    for (nint i = 0; i < 4; i++) {
        uint64 s = default!;
        cmov(ref s, (uint64)(t.x[i] & 1), e.x[i] + t.x[i], (uint64)(e.x[i] ^ t.x[i]));
        e.x[i] = s;
    }
}
```

## What deliberately stays boxed

The `RefLoweredParams` guard keeps counter-examples beside the lowered ones, so the boundary is
itself under test:

| kept boxed | why |
|:--|:--|
| `guarded(ж<uint64> Ꮡp)` | compares the pointer to nil — its *identity* is observed, not just its target |
| `boxedBump(ж<uint64> Ꮡout)` | used as a func value — a method group cannot close over a `ref` |
| `defer(ᴛ1 => bump(ref ᴛ1.DerefOrNull()), Ꮡx, …)` | the defer/go carve-out: the address must survive past the frame, so the box crosses and the deref happens at invocation |
| in-lambda call sites | uniformly boxed-fallback wrapped `.DerefOrNull()` — identity-box aliasing preserved through captures |
| string/numeric wrapper reinterprets | outside row 5's array-wrapper pairing family; a value copy would lose the write |
| blank/unnamed pointer params (`func setup(_ *note)`) | nothing to gain — excluded after the vacuous lowering emitted an empty parameter name |

Every exclusion is the same statement: the lowering never guesses. A shape either carries its
escape proof or it keeps the box that made it correct yesterday.
