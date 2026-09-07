# DESIGN — byte-offset reinterpretation of managed storage: the class behind darwin increment 13 and `reflect`'s `TestIsZero`

> **Status: design record, not a cut.** Drafted 2026-09-07 by lane C2 after sizing darwin increment 13
> (mailbox `b89d98fec4`) and finding that `reflect`'s `TestIsZero` is the same class from the opposite
> direction (mailbox `a19d10d9cb`, off R's retraction `76849193a6`). It was offered to the coordinator
> with two alternatives and taken as the stated default when no routing arrived within the hour. **It
> cuts nothing, proposes no machinery, and deliberately does not choose a remedy** — its purpose is to
> put the measured population, the structural refusal and the one open question in front of whoever
> does. Corpus figures were measured at master `fd09034f53`; master is `19a4693954` as this is
> written, one roster-only commit later, which touches no corpus file.
>
> **Adjacent records, and where to go instead.** Both are ALLOCATION records — they ask how to avoid
> materialising a copy when handing bytes to a native boundary — and neither covers this class,
> confirmed by their author at mailbox `4604d36092`. **If your question is why a byte view costs
> objects, go there:** `DESIGN-string-byte-window.md` is candidate C of the `os` want-zero residue,
> on Go's zero-copy `unsafe.Slice(unsafe.StringData(s), len(s))` idiom; and
> `DESIGN-syscall-buffer-element-address.md` is candidate E, on the element-take box and the pin
> behind `&buf[0]` in a `//sys` wrapper. **This record asks a different question — whether a byte
> offset computed against C's layout names the same storage in a managed object — and it is named
> for that DEFECT rather than for the shape**, because the shape-named records are what a reader
> hunting this class would otherwise open first and leave believing it was already recorded.

## 0. The two sites, one from each direction

**READ — `net/cgo_unix.go:153` (go1.23.12), the site darwin increment 13 was named for:**

```go
p := (*[2]byte)(unsafe.Pointer(&sa.Port))   // sa.Port is uint16
```

emitted for darwin as

```csharp
var p = (ж<array<byte>>)(uintptr)(@unsafe.Pointer.FromPinnedBox(sa.of(syscall.RawSockaddrInet4.ᏑPort)));
```

and measured failing as an `IndexOutOfRange` out of `go.array<T>.get_Item` at
`net/darwin/cgo_unix.cs:228`, on both mac legs.

**WRITE — `reflect`'s `all_test.go`, the row R rooted and retracted their own sizing for:**

```go
{setField(struct{ _, a [256]S }{}, 0*unsafe.Sizeof(int64(0)), int64(1)), true}
// setField: *(*V)(unsafe.Add(unsafe.Pointer(&in), offset)) = value
```

An `int64(1)` written at **byte offset 0** of a struct whose first field is `[256]S`. In the managed
object that slot holds the `S[]` reference, so the write plants the integer over a reference — non-null
and not an object — and the next `Index` walks into `Backing[m_low + index]` on it. The panic is a nil
dereference inside `array.get_Item`, not an index panic, which is what discriminates this from the
null-backing case golib already handles.

**Both are the same operation: address a managed object at a BYTE OFFSET and read or write it at a
different type.** One reads a window, one writes a scalar. Neither is expressible.

## 1. Why the managed model refuses both, from the source

`golib/array.cs` — `array<T>` is a `T[]` reference plus bounds, never bytes:

```csharp
private readonly int m_low;      // :57
private readonly int m_length;   // :58
private T[] Backing => m_array ?? [];                    // :273
public ref T this[int index]  => ref Backing[m_low + index];        // :281, :288
public ref T this[nint index] => ref Backing[m_low + (int)index];   // :292, :299
```

**The READ direction** therefore has nothing to point at when the source element type differs from the
target's: there is no `T[]` of the target type behind a `uint16` field. `array<T>.AliasPointer`
(`array.cs:207`) says so in its own body — it windows when `TryGetElementStorage` succeeds and
otherwise returns `(ж<array<T>>)(uintptr)element!`, **the broken shape, as its documented fallback** —
and its remarks state the limit outright: *"A pointer with no managed element storage behind it — a
heap box, a struct field, a native address — keeps the raw-address route: no `T[]` exists to window,
and an `array<T>` can neither view native memory nor be fabricated from a scalar's bytes."*

**The WRITE direction** fails one layer lower and for a reason no window abstraction reaches: **the CLR
gives AUTO layout to any struct holding a reference-typed field and is free to reorder it**, so a byte
offset computed against Go's (C's) layout does not name the same field in the managed object. Where the
slot it lands on holds a reference, the write does not merely read wrong — it **corrupts** one.

R's independent control is the confirmation: **every REFERENCE kind dies, `uintptr` SURVIVES.** That is
exactly the split "the byte offset lands on managed reference storage" predicts.

## 2. The population, measured

At `fd09034f53`, `GoTargetOS`-independent grep over `src/core/**/*.cs` for
`(ж<array<…>>)(uintptr)`, comment lines excluded by a positive-controlled filter:

| shape | sites | where |
|---|---:|---|
| **A** address of a pinned managed **FIELD** | **12** | `syscall/linux` 5, `syscall/darwin` 4, `net/darwin` 2, `runtime` 1 |
| **B** address of a pinned managed **BOX** | **6** | `runtime` 3, `internal/reflectlite` 1, `reflect` 1, `runtime/linux` 1 |
| **D** a **native or computed** address | **34** | `runtime` 15, `runtime/linux` 11, `runtime/darwin` 5, `runtime/windows` 3 |
| **E** golib's own generic producer | **1** | `golib/array.cs:216` |
| | **53** | across 28 files |

A + B = **18 managed-memory sites**, cross-checked by a second, differently-shaped predicate
(occurrences of `FromPinnedBox` in the same set = 18).

⚠ **These 53 are PRODUCTION ONLY — zero `_test.cs` sites, measured.** `setField` lives in
`reflect/all_test.go`, so the WRITE site above is **outside this population** and the class is larger
than 53 by an unmeasured amount. A census whose motivating site is a test owes the `-tests` dimension,
and this one has not been run.

## 3. What the converter already does, and why this is not a routing bug

`arrayPointerAliasEmission` (`src/go2cs/convCallExpr.go:4672`) emits the CORRECT
`array<T>.AliasPointer(p, N)` form — with Go's `N` preserved — and gates it on

```go
if !ok || !types.Identical(srcPtr.Elem(), targetArr.Elem()) { return "", false }
```

`runtime/type.cs:276` carries **both** forms on one line, from two identical Go constructs at
`runtime/type.go:265`:

```csharp
copy((~(ж<array<byte>>)(uintptr)(@unsafe.Pointer.FromPinnedBox(ᏑnameOff)))[..],
     (~array<byte>.AliasPointer(n.Data(off), 4))[..]);
```

Read off the emission alone that is an obvious inconsistency. It is not: `n.Data(off)` is `*byte`
against `[4]byte` and aliases; `&nameOff` is `*abi.NameOff` against `[4]byte` and does not. **Same
predicate, two answers, both correct**, and the function's own comment gives the reason — *"a `T[]`
view over differently-typed storage has no managed spelling."* Relaxing the gate would route these
sites into `AliasPointer`'s fallback, which is the broken shape by another path.

*(The same Go idiom appears three times across three packages: `runtime/type.cs:276`,
`reflect/type.cs:391`, `internal/reflectlite/type.cs:193`.)*

## 4. What is NOT measured, stated so nobody sizes against a gap

- **Reachability.** Membership is not reachability. Whether any of the 34 native-address sites is
  reached at run time is unmeasured; only the two `net/darwin` sites and the one `reflect` row have a
  measured failure.
- **The `-tests` dimension.** Unrun. The WRITE direction has exactly one measured site and no census.
- **Whether one capability serves both directions.** §5.
- **The write direction's true population.** One row is not a class size.

## 5. The question this record exists to answer

**Does ONE capability serve both directions?** The record's own reading, offered as a hypothesis with
its reasoning and explicitly NOT measured:

- The **READ** direction wants a *window abstraction* — something with `array<T>`'s surface that can be
  backed by a native address or by a differently-typed managed span. .NET has primitives in that shape
  (`MemoryMarshal`, `Unsafe.As`, `Span<byte>` over a pinned reference); the design question is what the
  emitted C# should be and what it costs, not whether the runtime can express it.
- The **WRITE** direction wants something a window cannot give: **a guarantee that the managed byte
  offset equals C's.** That is a LAYOUT property (`LayoutKind.Sequential`, no reference-typed fields),
  not an abstraction, and for a struct that genuinely holds references it is not obtainable at all.

**If that reading holds, the answer is NO — two capabilities, and the write direction's honest remedy
space is much narrower than the read direction's**, possibly reaching only blittable targets with the
rest being a documented refusal. **It is a hypothesis. Measuring it is the first thing an increment
here should do**, and it is cheaper than any cut: it needs the write-direction census and one layout
experiment, not a converter change.

## 6. Why this is not a darwin increment

**34 of the 53 sites are in `runtime` and its per-GOOS folders**, and the WRITE consumer blocks
`reflect`'s bank — `TestIsZero` is one of that row's four residual rows, R has retracted their sizing
and stated it is not theirs to close, and nobody has taken it. The two `net/darwin` sites that named
the increment are **2 of 53**, and a remedy scoped to them is throwaway against any capability that
later covers the class.

**Recommendation on the record: defer the darwin increment; the two `net/darwin` sites keep the
hand-own remedy `internal/syscall/unix/darwin/net_darwin_impl.cs` already uses for its own family.**

## 7. Provenance

Population, shape split and both cross-checks measured at master `fd09034f53` on a Linux host with the
corpus toolchain pinned to `go1.23.12` (the box's bare `go` is a different release; the pin aborts on
mismatch). Converter and golib lines read at that tree. The READ failure was measured on both mac legs
through the increment-12 acceptance A/B; the WRITE failure and its reference-kind control are R's,
cited at `76849193a6` and not re-run here. Every count in §2 is re-derivable by the §2 predicate alone.

## 8. AMENDMENT 2026-09-07 — the WRITE direction's refusal is already designed, in the token record

§5 offers as a hypothesis that the write direction's remedy space *"reaches only blittable targets
and the rest is a documented refusal"*, and marks it unmeasured. **That refusal exists as a design
already** — `docs/phase4/DESIGN-managed-pointer-token.md` §10 (the Q44 narrowing, appended the same
day this section was), whose **arm 3** is this class stated from the token side:

> `n` is inside a live token's block but is not the token (offset ≠ 0) → **a Go-layout byte offset
> into CLR-laid-out storage, which has no meaning.** Refuse by name, catchably.

**Two things it contributes that this record did not have.** First, the write direction is **not**
one row: §10.1 carries an eight-field-kind, two-platform table for `setField`
(`reflect/all_test.go:1399-1400`), and **Go 1.23.12 and master both write correctly 8/8** — so the
class's write half is a defect only where the offset lands on a reference, which is exactly what §1
predicts and §4 lists as unmeasured. Second, **arm 3's blast radius on `reflect` is measured at
ZERO** on Linux: seat `388 / 0 empty / 67 differing`, seat+refusal `388 / 0 / 67`, differing sets
identical name for name.

**What it does not settle**, and §10 says so itself: arm 2 — the offset-0 prefix pun the write's
correct cases take — is the new work, its population is unmeasured, and the census that would size
it is dynamic at the registry rather than a grep. **So §5's question is still open; what has moved
is that one of its two branches now has a designed answer with a measured cost.**

**Neither record cited a measurement the other made until now.** They were written a day apart from
opposite ends of the same defect and agree; this section and §10.6 are the reciprocal pointers.

-- C2
