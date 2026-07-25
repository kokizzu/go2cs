# FINDING — a managed box round-tripped through `uintptr` yields a DANGLING pointer

> Filed 2026-07-24 from the `go/doc/comment` arc (sub-agent `claude/r2-doccomment`). Folded into the
> reflection chip's increment-2 arc, which owns the affected files.
>
> **RULED 2026-07-24 (user, charter §10): fix the pointer MODEL generally — option 2.** Rationale
> below in *The ruling*. Reproduced deterministically first (`src/Tests/Behavioral/ReinterpretPointerLifetime`),
> then fixed at the ruled layer.

---

## Symptom that led here

`go/doc/comment`'s `TestWrap` **passes** at top level in both runtimes, but 9498 of its 10000
subtest names diverge, so the package cannot bank:

```
go:  TestWrap/n=1 … TestWrap/n=6   TestWrap/n=7    TestWrap/n=8   …
C#:  TestWrap/n=1 … TestWrap/n=6   TestWrap/n=_7   TestWrap/n=_8  …
```

The names come from `t.Run(fmt.Sprint("n=", n), …)`. Go's `Sprint` adds a space between two operands
only when **neither** is a string (`doPrint` → `isString := reflect.TypeOf(arg).Kind() == reflect.String`),
and `testing` rewrites the space to `_`. So from the 7th outer iteration on, C# stopped recognizing
`System.String` as `reflect.String` — **permanently**, mid-process.

## Minimal reproducer (no test host, no pipeline)

A console app referencing only `golib`, `go-src-converted/fmt` and `go-src-converted/reflect`:

```csharp
Console.WriteLine(reflect.TypeOf((object)"x").Kind());   // 24  (String)  — correct

object[] keep = new object[64];
for (int i = 0; i < 400_000; i++)
    keep[i & 63] = new byte[24];                          // churn the gen-0 heap

Console.WriteLine(reflect.TypeOf((object)"x").Kind());   // 0   (Invalid) — CORRUPTED
Console.WriteLine(fmt.Sprint("a", "b"));                 // "a b"  — Go says "ab"
Console.WriteLine(reflect.TypeOf((object)(long)1).Kind());// 6   — a type cached AFTER the churn is fine
```

A forced `GC.Collect(2, Forced, blocking, compacting)` alone does **not** reproduce it; the address
has to actually be re-allocated. That is why it looks nondeterministic in the wild (it first fired
around call ~3865 in a tight `fmt.Sprint` loop).

## Root cause

`reflect.toRType` (auto-converted, `reflect/type.cs:669`) is a Go pointer-type reinterpret between two
**managed** structs:

```csharp
internal static ж<rtype> toRType(ж<abi.Type> Ꮡt) {
    return (ж<rtype>)(uintptr)(new @unsafe.Pointer(Ꮡt));
}
```

`canonType` (`reflect/value_impl.cs`) caches the result **for process lifetime**:

```csharp
return s_canonTypeCache.GetOrAdd(st, _ => new rtypeжΔType(toRType(Ꮡt)));
```

The round-trip runs through golib's `ж<T> → uintptr` operator (`src/core/golib/ж.cs:743`), whose
managed-box path is:

```csharp
fixed (void* ptr = &value.Value)
    return (uintptr)ptr;
```

`fixed` pins **only for the duration of the statement**. The address escapes it, and
`(ж<rtype>)(uintptr)` builds a *native-backed* box (`m_nativeAddr`, `Unsafe.AsRef<rtype>((void*)addr)`)
that holds **no reference to the source box**. So the cached canonical `reflect.Type`:

1. does not keep the `ж<abi.Type>` box alive → it is collected, and
2. would be wrong even if it did → a compacting GC may move the box.

Once the address is reused by another allocation, `rtype.Kind()` reads whatever is there now — here,
`Kind_ == 0`.

**This is not a reflect bug.** It is a general property of the pointer model: *every*
`(ж<U>)(uintptr)(unsafe.Pointer(Ꮡmanagedbox))` in the corpus produces a pointer with no lifetime or
stability guarantee. It happens to work when the derived pointer is consumed immediately (the syscall
wrappers) and fails when it is retained (this cache). Note the same operator is used to hand buffers
to Win32 — those are also formally unpinned across the call, though the `IArray` arm above it does
take a real persistent pin (`pinnedArrayData`).

## Why it was NOT fixed here

* **Ownership.** The demonstrated consumer is `reflect/value_impl.cs` + `internal/abi/type_impl.cs`,
  both inside the live reflection chip's lock (charter §6.1).
* **Blast radius.** Every candidate fix changes `ж<T>`'s `uintptr`/`void*` operators, which every
  syscall, `runtime` and `unsafe` path in the 302-package corpus goes through. Charter §7/§10 say
  design this *with* the user, adversarially reviewed, before writing it.

## Options (for the decision, not a recommendation to apply blind)

1. **Persistent pin + owner back-reference (golib).** Mirror `pinnedArrayData` for the value slot
   (`m_pinnedValueData`), register `address → source box`, and have `(ж<U>)(uintptr)` store the source
   as an owner reference on the derived box. Correct and general — but it makes *every*
   `uintptr(unsafe.Pointer(p))` permanently pin its box (heap fragmentation, unbounded registry), and
   `GCHandle.Alloc(…, Pinned)` cannot pin a box whose `T` contains references (`abi.Type` does), which
   is exactly the failing case.
2. **Managed reinterpret for managed-referent pointees (converter + golib).** This is what the
   project's own S1 fork ruling already prescribes — "managed-referent cases hold the `ж<T>`/`object`
   **directly**, never a `nuint` round-trip" (CLAUDE.md) — and `toRType` violates it. Emit
   `Ꮡt.Reinterpret<rtype>()` for `(*U)(unsafe.Pointer(p))` where both pointee types are managed
   structs; golib returns a box aliasing the SAME managed slot (a field-ref-shaped box over
   `Unsafe.As<abi.Type, rtype>`), which is GC-safe by construction and needs no pin at all. Requires a
   layout-compatibility judgment in the converter.
3. **Narrow (chip-local).** Have `canonType` build its cached wrapper without the address round-trip.
   Fixes this consumer only; leaves the general hazard latent.

Option 2 matches the documented architecture and is the durable one; option 1 is the one that keeps
the raw address model and pays for it.

---

## The ruling (2026-07-24, user, charter §10) — option 2, at full generality

**Fix the pointer MODEL, not the consumer.** Rejected: the chip-local `canonType` fix (option 3 —
leaves the general hazard latent, charter §2) and a golib-primitive-plus-hand-owned-reflect variant
(fixes only the sites we hand-write, and a converted END-USER program doing an ordinary struct
reinterpret would still corrupt silently — charter §8).

### Why this is Go's own rule, not a go2cs invention

Go draws exactly this line: a pointer obtained through `unsafe.Pointer` is a real reference the
collector tracks and keeps alive, while a `uintptr` is **a number that does not**. The old emission
inverted it — every reinterpret went through the number. The fix restores Go's own distinction, and
the residual (arithmetic-derived `uintptr` sources keep the address route) is not a gap in the model
but the faithful reproduction of Go's rule.

### What was measured before deciding

Reproduced first, deterministically (3/3 runs), as `src/Tests/Behavioral/ReinterpretPointerLifetime`
— Go prints `lifetime: true true true`, pre-fix C# printed `lifetime: true false false`. A forced
collection alone does not reproduce it; the address must actually be re-allocated, which is why it
looks nondeterministic in the wild.

Corpus census of `(ж<X>)(uintptr)` in `src/go-src-converted` (930 sites; ripgrep — plain `grep -P`
dies on this locale and silently returns nothing, charter §9):

| Cluster | Sites | Disposition |
|---|---|---|
| `runtime` | 592 | Compile-only stub tree; never executes in the managed model |
| `reflect` + `internal/reflectlite` + `internal/abi` | 231 | **Executing** — where the bug lives |
| syscall / net / internal/poll / os / windows | ~80 | Interop: the address IS the meaning; must keep the address model |

By source shape: 338 fused `(uintptr)(new @unsafe.Pointer(`, 76 `@unsafe.Pointer.FromRef(ref …)`,
516 number-sourced. So **414 sites corpus-wide / 130 in the executing reflect cluster** have
recoverable provenance; the rest are genuinely address-derived.

### The fix, at the two ruled layers

1. **golib** (`src/core/golib/ж.cs`) — `ж<T>.Reinterpret<TDst>()`. Provenance decides the model,
   **not the pointee type**: a NATIVE-backed box (`m_nativeAddr` — a Win32 API's returned address)
   reinterprets to the same address, which is what those call sites mean and the only thing correct
   for them; a nil box yields the derived type's `NilBox`; a box owning MANAGED storage aliases that
   storage through ж's existing struct-field-ref reference kind, recomputing the ref from a live
   object reference on each access (`Unsafe.As<T, TDst>(ref …ValueSlot)`). That is GC-safe by
   construction and needs no pin. It composes through the field-ref and array-element kinds, so a
   reinterpret of `&s.f` or `&a[i]` aliases the real storage, and it preserves Go pointer identity
   (ж equality compares source + accessor; the accessor is a static method, so two reinterprets of
   one box compare equal).
2. **converter** (`src/go2cs/convCallExpr.go`) — the `(*U)(unsafe.Pointer(p))` shape with `p` a Go
   pointer now emits `Ꮡp.Reinterpret<U>()` instead of the `(uintptr)` hop. This reuses the peeling
   the identity-reinterpret path already did (`pointerConversionSource`, extracted from
   `pointerReinterpretIdentitySource`): identical element types stay the existing identity elision,
   differing element types are the genuine reinterpret. A raw-address source has no box behind it
   and keeps the address route.

### What the adversarial review changed (§7, three independent lenses — Go-semantics /
### blast-radius / generalization)

The first implementation was **wrong in the same way for three different reasons**, and all three
reviewers found it independently with executed probes. The claim it rested on — *"the managed arm is
never worse than the address arm, since both reinterpret the same bytes"* — is **false**, because a
go2cs surrogate's C# layout is not its Go layout:

| Go type | Go size | C# surrogate | C# size |
|---|---|---|---|
| `[2]byte` | 2 | `array<byte>` | 8 (a reference to a backing store) |
| `string` | 16 | `@string` | 8 |
| `[]byte` | 24 | `slice<byte>` | 32 |

So a **valid** Go reinterpret becomes an oversized `Unsafe.As` that reads past the value slot into the
box's own private fields and materializes a *fabricated managed reference* — a CLR type-safety break
(access violation, or heap corruption on write), which is strictly worse than the contained
wrong-read the address route produces. Three concrete regressions were demonstrated, not predicted:

| # | Regression | Evidence |
|---|---|---|
| R1 | A reference-typed `TDst` makes the derived box report `IsNull` (a field-ref box has `m_val == null`), so `~` **panics** where the native box did not. 17 corpus sites, incl. `time`'s `syncTimer` on the `NewTimer`/`NewTicker` path and 7 `reflect` type constructors | probe: `~Reinterpret<object>()` → `PanicException` |
| R2 | A fixed-array pointee bypasses `pinnedArrayData`, aliasing the `array<T>` **wrapper struct** instead of the array data — `FixedArrayBufferPointer` (an output-compared guard for a *previous* fix in this area) regresses | probe: `2018915346` (correct) vs `1699976376` (the `ushort[]` object pointer) |
| R3 | A `null` **reference** source — the second nil representation, e.g. a zero-valued pointer field — throws `NullReferenceException` from an instance call, where the old route yielded nil | reproduced by the suite: `TypeConversion` **exit code 2 vs Go 0** |

Two further scope corrections:

* **The prefix-downcast family is structurally unrepresentable and is NOT fixed.** Go's reflect/runtime
  idiom allocates a larger struct, hands out a pointer to its embedded header, and casts back
  (`(*structType)(unsafe.Pointer(t))` where `t` is a `*abi.Type`). In Go the larger allocation is
  really there; in the managed model a `ж<abi.Type>` holds *only* an `abi.Type`. ~60+ sites. These keep
  the address route and remain the recorded raw-metal class.
* **Coverage is not "the pointer model generally."** Of 930–940 emitted reinterpret sites, ~55% are
  arithmetic-derived numbers that keep the address route by design, and the fused/`FromRef` shapes the
  fix reaches are the rest. The honest claim is: **the reinterpret is made sound wherever a source box
  is recoverable AND the reinterpret is representable in the managed model** — not everywhere.

### The corrected design

1. **`Reinterpret` is an extension method on `ж<T>?`**, so both nil representations (a nil box and a
   plain `null` reference) yield the derived type's nil, as Go does. This is why the signature carries
   both type arguments — `Ꮡt.Reinterpret<abi.Type, rtype>()`.
2. **The managed arm is GATED** (`ReinterpretAliasesStorage<T, TDst>`, cached per pair): both pointees
   value types; `SizeOf<TDst>() <= SizeOf<T>()` so the read stays inside the source's storage; and
   either neither type contains managed references (nothing can be fabricated) or the two are
   layout-compatible in the senses the converter actually generates (same type, a single-field wrapper
   over the other, or an identical recursive field-type sequence).
3. **Everything ungated falls back to the pre-existing address route** — so the change is *additive*:
   where it does not apply, behavior is exactly what it was, never something newly wrong.
4. **The converter intercepts at the two points that emit the address route**, not upstream. The first
   attempt intercepted before the conversion renderer and hijacked conversions that already had
   *correct* re-box routes — including named ARRAY wrappers, whose lazily-materialized backing store a
   storage reinterpret silently bypasses (caught as a `NamedArrayWrapper` stdout mismatch: three lines
   of `0 0` where Go prints real values). Behavioral drift fell from 16 projects to 8 once the
   interception moved to the address routes themselves.

Layout compatibility beyond what the gate proves remains the Go program's assertion, as in Go.

### Recorded, not fixed (each with a named next consumer)

* The **split shape** — `q := unsafe.Pointer(p)` in one statement, `(*U)(q)` in another (~52% of Go
  conversion sites). Provenance dies inside `unsafe.Pointer(p)` itself, which keeps only a number. The
  general answer is to carry the source box on the `Pointer` class so provenance rides the value,
  exactly as Go's GC-tracked `unsafe.Pointer` does — a follow-on chip, since it changes ~780 emitted
  sites.
* **Pointer identity** through a reinterpret holds for a plain heap box but not for `&s.f`/`&a[i]`
  sources (`of()`/`at()` mint a fresh box per call), nor for a chained reinterpret.
* `IsNull` **value-peeks on field-ref boxes** — a pre-existing golib defect (an ordinary
  `of()` pointer to a reference-typed field already reports nil and panics on deref). Not created here;
  the gate keeps this fix clear of it. Worth its own commit.
* `&a[i] == &a[i]` is **false** for both `array<T>` and `slice<T>` — a live Go-identity violation in
  golib, unrelated to this change.
* **Dereference cost** through the field-ref path is ~7× the raw-address read (≈1 ns → ≈7 ns), and
  `reflect` caches derived pointers and dereferences them forever. A dedicated reinterpret reference
  kind that inlines `Unsafe.As`, rather than reusing `m_structFieldRef`, is the answer if it shows up
  in a profile.

---

## S1 follow-up (2026-07-25) — the `Reinterpret<X, array<Y>>` class was a REGRESSION, now fixed

The rebank probe flagged ~24 emitted `Reinterpret<X, array<Y>>` sites as a suspected fabrication
class and made them Stage 0 of [`PLAN-corpus-rebank.md`](PLAN-corpus-rebank.md). Probed
(`claude/r9-s1probe`), the suspicion was right and **worse than assumed**: it was not a re-spelling
of an already-broken route, it was a live regression against the committed corpus.

**What was measured.** Seven distinct source shapes were built as standalone Go programs, converted,
and run against `go run`. All seven are broken on the fabricating emission — five die with a hard
`AccessViolationException` (process death, not a contained wrong read), one with an
`NullReferenceException`, one with a spurious `index out of range` on a zero-length fabricated array.
Then the same shapes were rebuilt with the *previous* emission to separate pre-existing breakage from
new:

| Corpus sites | Committed emission | Fresh emission | Verdict |
|---|---|---|---|
| registry `Get`/`SetStringValue`, `GetMUIStringValue` (×4) | `slice<T>` over a `ReadOnlySpan<T>` — **works** | `Reinterpret` → AV | **REGRESSION** |
| `reparse_windows.path()` (×2) | native span, memory-safe but wrong offset | `Reinterpret` → AV | **REGRESSION** |
| `reflect.gcSlice` | `Reinterpret` → AV | native span (wrong offset) | improvement |
| `os/user` ×1, `net` lookupTXT ×1 | native span over a managed-ref element — throws | NRE | both broken |
| syscall `sockaddr` Port ×6, `internal/poll` ×4, `net` SRV/NS ×2, registry `SetDWord`/`QWord` ×2, `reflect`/`reflectlite` nameOff ×3 | address route → AV | `Reinterpret` → AV | neutral, both broken |
| `internal/chacha8rand` ×2 | address route | `Reinterpret` | neutral — **dead code** (`block` is hand-owned in `chacha8_impl.cs`; `block_generic`/`setup` are never invoked, which is the whole reason math/rand/v2 validates 36/36 — *not* because the address route works there) |

The regression's fingerprint is the rebank plan's "5 csproj changes flip `AllowUnsafeBlocks`
true→false", which the drift inventory had classed as benign converter-consistency. It is not
benign: it is the marker that the `unsafe` native-span fusion was lost in those packages.

**Live confirmation.** A console app calling the converted
`registry.OpenKey(LOCAL_MACHINE, …)` + `GetStringValue("ProductName")` — the exact call
`time.initLocalFromTZI` → `toEnglishName` → `matchZoneKey` makes, and `mime.initMimeWindows` makes at
package init — returns `Windows 10 Pro` on the committed tree and hard-faults with the fresh
`value.cs` overlaid. `time` and `mime` both reference the registry package, so the fresh corpus would
have taken down essentially every Windows program that formats a local time or looks up a MIME type.

**Root cause.** Not golib and not the gate — the gate correctly refuses the managed arm for an
`array<Y>` destination. The interception changed the emitted **text**, and that text is an input to a
downstream fusion: `convSliceExpr.isPointerCast` matches on a leading `(ж<…>` to lower
`(*[N]T)(ptr)[:n]` into a `slice<T>` over a `ReadOnlySpan<T>`. `Reinterpret` does not match, so the
fusion silently fell through to `(~box).slice(…)` over a fabricated `array<T>`.

**Fixed** (`6c31a59d2`) by excluding array-underlying targets in
`pointerReinterpretManagedSource` — the emission is restored to exactly what it was, fused or not,
and the interception loses nothing since golib could never have aliased an `array<Y>` anyway. Guard:
`Tests/Behavioral/PointerCastSliceReinterpret` (output-compared; neutered-and-rebuilt to confirm it
fails `[Target,Output]` with the AV). Gates: CNR 491/491 byte-identical, suite 491/491 PASS.

**A second, pre-existing bug surfaced in the same fusion** and is fixed alongside (`ce2d5a743`): the
span was always built from element 0, dropping the slice's LOW bound. `os.Readlink` returned the
reparse buffer from offset 0 instead of the substitute name, and `internal/abi.FuncType.OutSlice()`
returned the in-parameters followed by the out-parameters, so `reflect.Type.Out(i)` indexed the wrong
half. Both now offset the base pointer and shorten the length.

**Not fixed, still recorded:** the 17 non-sliced array-target sites (syscall/poll `sockaddr` Port
byte-puns, registry `SetDWordValue`, `reflect`/`reflectlite` `nameOff`, `net` SRV/NS host reads) remain
on the address route and still fabricate. They are unchanged by the rebank, so they do not gate it,
but each is a latent AV on a live Windows path. A correct lowering needs an `array<T>`/`slice<T>` view
that can address native memory or pun a scalar's bytes — neither is representable by today's
`array<T>` (a wrapper over a managed `T[]`), so this is a golib data-model item, not a converter one.

## Consequence for the campaign

`go/doc/comment` **cannot bank** until this is resolved: `TestWrap`'s 10000 subtest names are part of
the differential. Its other blocker (`TestTestdata`'s remaining 16 subtests) is separately owned by
the reflection chip (`reflect.PointerTo` → `typelinks`, and `encoding/json` unmarshal into `int`).

Any package whose tests print through `fmt.Print`/`Sprint`/`Fprint` with mixed string/non-string
operands is exposed to the same corruption once the process has allocated enough — the failure is
**silent** (a spurious space), so a package that validated early in its run could regress later.
