# DESIGN — closure emission: the lambda, the frame, and what a Go function should compile to

> **Status: §3 LANDED (r39e-closure, 2026-08-03); §4–§8 are a PROPOSAL for user review — do not
> implement from this document.** The user's "Rulings 2026-08-03" (board commit `87f807042`)
> commissioned ONE unified design for the closure findings rather than three separate arcs. §3 is the
> half that was already rooted, gated and shipped in this same change: the local-function emission
> mode and the escape-analysis narrowing that together took `time`'s `TestUnmarshalTextAllocations`
> from **216 B/op to 0** and banked the package. §4 onward is the part that stays on paper: the
> `ref struct` frame that r39-osalloc's arc item 3 sketched after measuring the
> `func<T>((defer, recover) => …)` machinery at **440 B/call**. It is written up here — with §3 — because
> the two are the same subject seen twice, and because §3's one deliberate exclusion (a literal that
> defers) is precisely what §4 dissolves.
>
> Related: [`BOARD-next-validation-candidates.md`](BOARD-next-validation-candidates.md) — r39-osalloc's
> decomposition (arc items 1–5) and r39e's measurement table;
> [`../ConversionStrategies-Reference.md`](../ConversionStrategies-Reference.md) — the emitted-form
> rules for both §3 mechanisms.

## 1. The subject in one paragraph

A Go function is a stack frame. It closes over variables without allocating, it registers `defer`
records in its own frame, and since Go 1.14 it usually *open-codes* those records straight into the
exit path. The converted C# allocates for all three: a closure becomes a heap display class plus a
delegate, an execution context becomes a `GoFunc<T>` object plus a body delegate plus two handler
delegates, and each `defer` becomes another closure plus another delegate plus, on first use, a
`Stack<Action>`. None of it is waste in the sense of a defect — each object buys back a Go semantic
the CLR does not give away — but all of it is *per call*, and it is why three separate Phase-4
`want 0 allocs` asserts (`time`, `os`, and the `strings`/`bytes` alloc family behind them) read as
package divergences when the real subject is one emission strategy.

## 2. The measured inventory

Every number below was measured on this repository with
`GC.GetAllocatedBytesForCurrentThread` deltas — the same instrument the converted `testing`
package's `AllocsPerRun` shim uses, so these ARE the numbers the Phase-4 asserts see.

| Cost | B/call | Where it was measured | Status |
|:--|--:|:--|:--|
| A capturing lambda bound to a local (display class + delegate) | **88** | `time` `parseRFC3339`'s `parseUint`, isolated control matched to the byte (r39-timer), re-measured as an A/B here | **fixed, §3.1** |
| A heap box for a struct local declared inside a closure | **128** | `time` `TestUnmarshalTextAllocations`'s `var t Time`, A/B'd here | **fixed, §3.2** |
| The `func<T>((defer, recover) => …)` execution context | **440** | `os.File.Write` → `internal/poll.FD.Write` frame-by-frame decomposition (r39-osalloc) | **§4 proposal** |
| ↳ `GoFunc<T>` object + body display class + body delegate | 224 | same | |
| ↳ one display class + delegate per `defer` registration | 128 | same (2 defers) | |
| ↳ the `Stack<Action>` on first registration | 88 | same | |

**One term inside that 224 has never been named, and naming it matters.** `GoFunc<T>.Execute` calls
`m_function(HandleDefer, HandleRecover)` — two *instance* method groups converted to delegates on
every execution. C# caches a method-group conversion only for a **static** target, so each of those is
a fresh delegate allocation per call (~64 B each on the 64-bit layout). r39-osalloc measured the
`func<>` object + closure + delegates bucket as a **total** of 224, so the pair is presumably inside
it rather than missing from it — but which is which decides whether §4's headline is 440 or something
larger, and it is answerable with one probe. It is also the one term removable **today, in golib
alone**: caching the two delegates in fields of the `GoFunc` instance trades two allocations for two
reference fields on an object that is itself per-call. Worth doing, small, and **not a substitute for
§4** — it removes two objects from a shape that allocates five.

## 3. What landed (r39e, this change)

Two independent converter fixes, both semantics-preserving, both gated by a behavioral guard whose
GOLDEN pins the emitted form and whose OUTPUT comparison pins the semantics.

### 3.1 A func literal that is only ever CALLED emits as a C# local function

`name := func(…){…}` used to emit `var name = (params) => { … };`. A C# lambda that captures anything
allocates a display class to hold the captured variables and a delegate bound to it, **on every
evaluation of the lambda expression** — i.e. on every call of the enclosing function, whether or not
the closure is called even once. It now emits a **local function**:

```csharp
//  Go:   parseUint := func(s bytes, min, max int) (x int) { … ok = false … }
nint /*x*/ parseUint(bytes sΔ1, nint minΔ1, nint max) {
    nint x = default!;
    …
    ok = false;          // the SAME `ok` — Roslyn rewrites both sites to one struct-closure field
    …
}
```

Roslyn compiles a local function that is never converted to a delegate with a **by-ref struct
closure**: the captured variables move into a struct that lives in the caller's frame and is passed
as a hidden `ref` parameter. Sharing is identical to the display class (there is still exactly one
storage location per captured variable, and the enclosing method's own uses are rewritten to it);
the heap object is simply gone.

The gate is the proof that keeps that compilation available: **every reference to the variable, other
than its declaration, must be the callee of a call.** The moment a local function is converted to a
delegate, Roslyn falls back to a heap display class — and a local function has no value form to give
a store, a return, an argument or a comparison anyway. The predicate (`localFunctionDefine` /
`objectOnlyCalled`, `convFuncLit.go`) therefore also subsumes reassignment and address-taking, since
both are non-call uses. Emission is a new `LambdaContext.localFuncName` mode in `convFuncLit`, so the
entire body pipeline — capture hoisting, boxed value parameters, variadic prologue, array clones,
named results, the single-return collapse — is shared verbatim with the lambda path.

**Deliberately excluded: a literal that uses `defer` or `recover`.** Its body is emitted inside a
`func((defer, recover) => …)` execution context whose 440 B dominate the 88 this rule removes, so
converting the outer binding alone would churn goldens for no measurable win. §4 is what makes that
shape cheap; when §4 lands, this exclusion is removed rather than worked around.

Guard: `Tests/Behavioral/LocalFunctionEmission` — ten probes, five of them negative controls, one per
disqualifying reason (value use, reassignment, defer/recover, the `var f func(); f = …` recursion
two-step, argument position).

### 3.2 A variable declared INSIDE a closure is not captured BY it

`performEscapeAnalysis`'s `*ast.FuncLit` arm exists to catch a variable a closure **closes over** —
another frame reaching this frame's storage, which the emitted C# serves through a shared `ж<T>` box.
It matched on any mention of the object lexically inside the literal's body, and for a variable
declared *there* that mention is its own declaration. So this:

```go
testing.AllocsPerRun(100, func() {
    var t Time
    t.UnmarshalText(in)
})
```

emitted `ref var tΔ1 = ref heap(new Δtime.Time(), out var ᏑtΔ1);` — 128 bytes per call, with the box
`ᏑtΔ1` **never referenced anywhere in the emitted body** — while the identical two statements outside
a closure emitted a plain `Δtime.Time tΔ1 = default!;`.

The narrowing is one containment test: if the object's declaration position lies inside the literal,
skip the arm — and keep descending, because a literal *nested* inside it does close over the variable
and gets its own correct turn. The proof that this cannot under-box: Go scoping puts a literal's own
local out of reach of every other frame, so there is nothing for a shared box to make visible; and
every route by which such a local can still genuinely escape — `&x`, `&x.f`, `&x[i]`, a pointer
argument, a capture-mode method, a pointer-receiver method value, a `go`/`defer` use — is decided by
an arm that walks the **whole enclosing body**, literal bodies included, so none of them is lost. This
is the *narrowing* direction of an escape rule, which is the dangerous one, so the guard spends five
of its eight probes proving the boxes that must survive still survive, each by writing through the
escaping alias and reading the value back.

Guard: `Tests/Behavioral/ClosureLocalNoHeapBox`.

### 3.3 Result

| `time` `TestUnmarshalTextAllocations` | allocs |
|:--|--:|
| before (branch base `18423efaf`) | 216 |
| with §3.1 only (box reverted by hand) | **128** |
| with §3.2 only (local function reverted by hand) | **88** |
| both | **0 — passes** |

216 = 128 + 88 exactly, each half independently measured, and the package moves 156 pass / 1 fail /
2 skip → **157 / 0 / 2**.

## 4. The proposal — replace the execution-context OBJECT with a frame

### 4.1 What the lambda is for, and what it is not for

The converter wraps any Go function that defers or recovers in

```csharp
public static (nint, error) Write(this ж<FD> Ꮡfd, slice<byte> buf) => func<(nint, error)>((defer, recover) => {
    …
    defer(Ꮡfd.writeUnlock);
    …
});
```

`GoFunc<T>.Execute` then supplies the two things Go's runtime gives for free: a `catch` that parks a
panic where `recover()` can read it, and a `finally` that drains the deferred stack on every exit
path. **Neither of those needs a lambda.** `try`/`catch`/`finally` are statements; the body could sit
directly in the method. The lambda is there because the execution context was modelled as an
*object* that owns the body, and once it owns the body the body must be a delegate.

The one thing that genuinely wants indirection is `recover()`, which is called from inside a
*deferred* closure, not from the body — and that is already solved: the panic slot is a
`ThreadLocal` on `GoFuncRoot`, reached statically. So the deferred closure never needs a handle on
the frame. **Only the defer LIST is per-frame, and only the BODY registers into it.** That is the
whole reason a `ref struct` is viable here.

### 4.2 Frame layout

```csharp
public ref struct GoFrame
{
    private Action? m_d0, m_d1, m_d2, m_d3;   // inline slots for the common arities
    private List<Action>? m_overflow;          // allocated only past four defers
    private int m_count;
    private uint m_armed;                      // open-coded-defer bitmask (§4.5)

    public void Push(Action deferred);         // fills m_d0..3, then m_overflow
    public void Run();                         // LIFO drain, re-panic rules unchanged
}
```

A `ref struct` local costs nothing: it lives in the caller's frame and the JIT can enregister the
slots. The corpus's defer arity is overwhelmingly 1–2 (`FD.Write` has 2), so `m_overflow` is dead
weight that is never allocated in practice; four inline slots is a starting number to be set from a
census, not a guess to be shipped.

`Run()` keeps today's `HandleFinally` semantics verbatim — the `HandledPanic` save/restore, the
re-panic `InheritThrowSite` rule, the final re-throw of an unrecovered panic. That logic is correct
and adversarially reviewed; it moves, it does not change.

### 4.3 Emission — the unnamed-result form

```csharp
public static (nint, error) Write(this ж<FD> Ꮡfd, slice<byte> buf)
{
    GoFrame ᒐ = default;
    try
    {
        …body, verbatim, with `return (0, err);` written as itself…
        deferǃ(Ꮡfd.writeUnlock, ref ᒐ);
        …
    }
    catch (Exception ex) when (RuntimeErrorPanic.TryAsPanic(ex, out PanicException? p))
    {
        GoFrame.Capture(p);          // static: the existing CapturedPanic ThreadLocal
        return default;
    }
    finally { ᒐ.Run(); }
}
```

A `return` inside a `try` with a `finally` is exactly Go's "run the defers on the way out", and the
body needs no rewriting at all. This form applies to every function whose results are unnamed or
whose deferred code provably cannot write them.

### 4.4 Emission — the named-result form

Go's `func f() (r int) { defer func(){ r++ }(); return 1 }` returns **2**: the deferred call runs
*after* the result is assigned and *before* the caller sees it. A C# `finally` cannot change a value
the `return` already evaluated, so the results are declared before the `try` and the exit goes through
a label:

```csharp
nint r = default!;
try { … r = 1; goto ᒐdone; … }
catch (…) when (…) { GoFrame.Capture(p); }
finally { ᒐ.Run(); }
ᒐdone:
return r;
```

`goto` out of a `try` that has a `finally` is legal C# and runs the `finally`, so this is a faithful
lowering. It is also structurally the same trick the converter already performs today (`litNamedDefer`
declares the named results outside the `func(…)` wrapper and returns them after it) — the wrapper is
replaced by a `try`, nothing conceptual is new.

### 4.5 Open-coding the static defers (the second half of the win)

§4.3 removes the GoFunc object, the body display class, the body delegate and the two handler
delegates. It does **not** remove the per-`defer` display class + delegate — 128 of the 440 in the
worked case. Go removes those by open-coding: a defer whose registration is unconditional and at
function scope is emitted straight into the exit path, with a bitmask recording which ones were
reached.

The managed analogue is the same, and the `m_armed` field above is the bitmask:

```csharp
//  Go:  defer fd.writeUnlock()                 → slot 0, unconditional
//       if fd.isFile { fd.l.Lock(); defer fd.l.Unlock() }   → slot 1, reached conditionally
ᒐ.Arm(0);                                       // m_armed |= 1u << 0
…
if (fd.isFile) { Ꮡfd.of(FD.Ꮡl).Lock(); ᒐ.Arm(1); }
…
finally
{
    if (ᒐ.Armed(1)) Ꮡfd.of(FD.Ꮡl).Unlock();     // reverse registration order: LIFO
    if (ᒐ.Armed(0)) Ꮡfd.writeUnlock();
    ᒐ.Run();                                     // dynamic defers, if any
}
```
The bit is what makes an *unreached* registration a no-op — an early `return` before the second
`defer` statement must not run it, which is exactly what Go's open-coded bitmask is for. Note that
"unconditional at function scope" governs the *arming site*, not the `finally` test: slot 1 above sits
inside an `if`, so it is NOT eligible under the conservative first cut below; a fully general
open-coding admits it precisely because the bit records whether control reached it.

Eligibility is a three-part proof, and every part is already computable by the converter:
1. the `defer` statement is **unconditional at function scope** (not inside a loop, `if`, `switch`,
   `select` or a nested block whose execution is conditional) — a syntactic property;
2. the deferred call's **argument expressions are evaluated at the defer statement** in Go, so each
   is hoisted to a temp there and read in the `finally` — which is what `deferǃ`'s arity ladder does
   today, minus the delegate;
3. the deferred callee is a **direct call** (`x.M()`, `f(a)`) rather than a computed func value.

A defer that fails any part keeps today's dynamic `Push`/`Run` path. Mixing the two in one function
is fine as long as the ordering is preserved — which means the dynamic list must interleave with the
open-coded slots by registration index, not simply run before or after them. **That interleaving is
the one genuinely fiddly part of this proposal and is where a review should push hardest.** The
conservative first cut is: open-code only when *every* defer in the function is eligible, and fall
back wholesale otherwise. That covers `FD.Write`, `ReadFile` and the great majority of the corpus,
and it has no ordering problem at all.

### 4.6 What each existing mechanism becomes

| Today | Under §4 |
|:--|:--|
| `builtin.func(…)` / `func<T>(…)` overloads | unreachable once every emission site migrates; deleted at the end of the migration, not before |
| `GoFunc<T>`, `GoFunc<TRef1,T>` … `GoFunc<TRef1…TRef16,T>` | **the whole 16-rung ladder disappears.** It exists solely because a lambda cannot capture a `ref` local or a `ref struct`, so each such variable had to be threaded through as an explicit `ref` parameter. An inlined body has no parameters to thread — it just uses its own locals. The `allows ref struct` asymmetry (rung 1 only) disappears with it |
| `deferǃ<T1…T16>` arity ladder | survives, but as `Push`-into-the-frame for the DYNAMIC case only; the open-coded case (§4.5) emits the call directly and needs no rung at all |
| `Stack<Action>` on first defer | replaced by the frame's inline slots; `List<Action>` allocated only past the inline arity |
| `HandleDefer` / `HandleRecover` method-group delegates | gone — `defer` becomes a `ref ᒐ` argument, `recover()` a static call |
| `CapturedPanic` / `HandledPanic` ThreadLocals, `InFlightPanic` | **unchanged.** This is what makes the `ref struct` viable (§4.1) |
| §3.1's local-function rule | **its defer/recover exclusion is lifted.** A literal that defers becomes a local function containing its own `GoFrame` local — the shape §3.1 had to refuse |
| §3.2's escape narrowing | orthogonal; unaffected in either direction |
| r39-osalloc item 4 (`heap(new uint32(), out Ꮡdone)` in the syscall seam) | **unaffected, and the doc says so rather than letting the reader assume otherwise.** That is `var done uint32; &done` where the address really *is* handed to a callee, so the box is required; its cost is the box's, which is osalloc arc item 1 (splitting `ж<T>`'s four kinds), not this design's |

### 4.7 Blast radius

Every converted function that defers or recovers is re-emitted — by grep, on the order of a thousand
sites across the corpus, including the hottest paths in `os`, `internal/poll`, `sync`, `net`,
`encoding/*` and `runtime`. This is the largest single emission change contemplated since the
`ж<T>` model itself. Concretely it means:

- a full CNR re-baseline of the behavioral goldens (the change touches a large fraction of them);
- a full `go2cs-stdlib.slnx` build, since a lowering error shows up as a compile error corpus-wide;
- the full validated sweep, twice — this is a *semantics* change to panic/defer ordering machinery,
  and the sweep's banked packages are the only instrument that exercises it under real workloads;
- specific attention to the constructs a `ref struct` local forbids: it cannot live in an `async` or
  iterator method, cannot be captured by a lambda, and cannot be a field. The converter emits none of
  those around a function body today, but "none today" must be re-verified, not assumed — a Go
  `range`-over-func lowering, for instance, must not put the frame inside an emitted iterator.

### 4.8 Migration path

Staged, each stage independently gated and revertible:

1. **`GoFrame` + `Push`/`Run` in golib**, with the existing `GoFunc` machinery untouched, plus a golib
   unit test that reproduces `HandleFinally`'s ordering, re-panic and unrecovered-rethrow behaviour
   against the new type. No converter change; nothing emits it yet.
2. **Converter emission behind an off-by-default option**, applied to the narrowest third first:
   functions with `defer` only, no `recover`, unnamed results, no nested literal that defers. A/B the
   whole stdlib reconvert; the diff must be exactly this shape.
3. **Extend to `recover`, then to named results (§4.4), then to function literals** — three separate
   gated steps, in that order, because each adds one lowering rule and each has its own failure mode.
4. **Open-code the static defers (§4.5)**, all-or-nothing per function first (§4.5's conservative cut),
   with the interleaved case deferred to its own review.
5. **Flip the default, then delete** `builtin.func`, the `GoFunc<…>` ladder and the now-dead `deferǃ`
   rungs — only once nothing in the corpus emits them.

Measurement at every stage: r39-osalloc's `os` probe (`os.File.WriteString` at 3,168 B/op today, of
which 440 is this design's), the `TestWriteStringAlloc` row itself, and the `Perf*` suite for the
throughput side — a `try`/`finally` per Go function is not free either, and the design must be shown
not to have traded allocation for wall time.

### 4.9 What is NOT proposed here

- Removing the `try`/`catch`/`finally`. Go's guarantee that deferred calls run on *every* exit path is
  the point; the CLR gives that only through `finally`.
- Changing panic identity, traceback capture, or `recover()` semantics in any way. §4 moves that code;
  it does not touch it.
- Anything about `ж<T>`'s four box kinds (osalloc arc item 1) or the
  `uintptr(unsafe.Pointer(x))` peephole (item 2). Both are larger wins than this one in the `os`
  decomposition and both are independent of it.

## 5. Recommendation

Land nothing from §4 without the user's decision on two questions the design cannot self-rule:

1. **Is the blast radius acceptable now?** §4 is correct and it is worth ~440 B/call on every
   deferring function, but it re-emits a large fraction of the corpus and re-opens goldens that have
   been stable for months. The alternative is to leave it until a Phase-4 row *requires* it — and
   `os`'s `TestWriteStringAlloc` does not, because 3,168 B remain above it even at zero (r39-osalloc
   §"still does not reach zero").
2. **Staged or wholesale?** §4.8 proposes five stages because each has a distinct failure mode; a
   wholesale change would be one shorter arc with one much larger blast.

The measurement in §2 that *should* be taken regardless of that decision is the
`HandleDefer`/`HandleRecover` method-group term — it is a golib-local question, answerable in an hour,
and it either adds ~128 B to the 440 (making §4 more valuable) or it does not (making the existing
decomposition exact).
