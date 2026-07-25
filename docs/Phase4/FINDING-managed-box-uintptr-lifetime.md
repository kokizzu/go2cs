# FINDING — a managed box round-tripped through `uintptr` yields a DANGLING pointer

> Filed 2026-07-24 from the `go/doc/comment` arc (sub-agent `claude/r2-doccomment`). **Not fixed** —
> the correct fix is a decision about the `ж<T>` pointer model with corpus-wide blast radius, and its
> demonstrated consumer (`reflect.canonType`) is inside the **live reflection chip's ownership lock**.
> Written up per charter §10 (surface the decision, keep moving).

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

## Consequence for the campaign

`go/doc/comment` **cannot bank** until this is resolved: `TestWrap`'s 10000 subtest names are part of
the differential. Its other blocker (`TestTestdata`'s remaining 16 subtests) is separately owned by
the reflection chip (`reflect.PointerTo` → `typelinks`, and `encoding/json` unmarshal into `int`).

Any package whose tests print through `fmt.Print`/`Sprint`/`Fprint` with mixed string/non-string
operands is exposed to the same corruption once the process has allocated enough — the failure is
**silent** (a spurious space), so a package that validated early in its run could regress later.
