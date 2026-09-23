# DESIGN (stub) -- REC-A: storage for a Go array VALUE (the zh-box record's class 4)

> **Status: STUB, 2026-09-23.** Minted by the H10 relabel ruling (ledger 2026-09-23 03:37, X(2) and
> O1) so that the fixed-array family's `deferred` entries cite a record that names a stage which
> removes the counted allocation. **Owner: C1. Full design: phase-4D kickoff.** Nothing is cut
> against this stub; every line number below was read at `bb54ff0920` (the batch-8b stamp) unless it
> says otherwise, and every figure is the i7 reading run's (`claude/coord-h10-readings` ac9f8251ee,
> Release with tiering off). Feasibility is stated UNMEASURED where it is not measured.

## 0. The class

A Go array value `[N]T` -- a struct field, a local, a by-value copy or return, or a composite-literal
temporary -- lives inline in its owner's storage in Go and costs no allocation of its own. go2cs emits
it as golib `array<T>`, a struct over a heap `T[]` (`src/core/golib/array.cs`, the `NewArray`/`CopyOf`
constructors at :68-123), so every array VALUE costs one counted object when it is created and one
more each time it is copied. `DESIGN-zh-box-reduction.md` §6 calls this **class 4** and names
`[InlineArray]` storage as the mechanism that deletes it (:519-521). net10.0 has value-typed fixed
storage, so the class is **expensive, not impossible**: O1 ruled it `deferred`, never `structural`.

## 1. Faces

| face | what allocates | read at |
|:--|:--|:--|
| **field** | the `= new(N)` initializer of a `[N]T` field, run by every explicit constructor | `chunkedReader.buf` (net/http/internal/chunked.cs:40); `digest.s`/`.x` (crypto/md5/md5.cs:39-40); `slog.Record.front` (log/slog/record.cs:38) |
| **by-value copy / return** | `ΔClone()` / `.Clone()` of a struct or array value | md5 `d0 = d.ΔClone()` (md5.cs:168); `return digest.Clone()` (md5.cs:198) |
| **composite-literal temp** | a literal array materialised per evaluation | md5 `var tmp = new byte[]{0x80}.array(72)` (md5.cs:184) -- a `new byte[]` the C# compiler emits and golib's counter does NOT see (`AllocationCounter.cs`, *Coverage*), then `.array(72)`, which is counted |

Local fixed arrays (`var a [65]byte`) are REC-B's local face (`DESIGN-nonescaping-locals.md`), not
this record's: their question is frame lifetime, not representation.

## 2. Members (by entry name)

`crypto/md5` TestAllocations; `crypto/sha1` TestAllocations; `crypto/sha256` TestAllocations;
`crypto/sha512` TestAllocations; `net/http/internal` TestChunkReaderAllocs; `crypto/ed25519`
TestAllocations (its Scalar/fiat arrays and SHA-512 digest copies); `crypto/internal/fips140test`
TestEdwards25519Allocations, TestNISTECAllocations/P224, /P256, /P384, /P521 (the fiat field-element
arrays); `log/slog` TestAlloc/* (F3, `Record.front`). Every one is a MIXTURE and names its other
families in its own entry.

## 3. Stages that REMOVE the counted allocation

**First increments (no representation change):**

1. **`ΔClone`/`.Clone()` elision.** A by-value copy whose source is dead after the copy, or whose
   destination is never written, needs no deep copy. md5's `d0 = d.ΔClone()` (md5.cs:168) is copied
   so the caller can keep writing `d`; the copy itself is the Go semantics, but the SECOND array
   allocation per field (`.Clone()` of the returned digest at :198, a value the caller copies again)
   is not. *Removes:* one counted object per elided copy. *Precondition:* a converter-side liveness
   read of the copy's source and destination within one function; refused when either escapes.
2. **`@new`-then-overwrite elision.** `var d0 = @new<Digest>();` followed by `d0.Value = d.ΔClone();`
   (read at crypto/internal/fips140/sha512/sha512.cs:272-273) allocates a zero `Digest` -- including
   its two array fields `h` and `x` (sha512.cs:60-61) -- and immediately overwrites it. Boxing the
   clone directly removes the zero value's arrays. *Removes:* two counted objects per `Sum`.
   *Precondition:* the overwrite is the box's first use.

**The representation stage (the one that deletes the class):** an `[InlineArray(N)]`-backed storage
for `[N]T` of an unmanaged `T` and small `N`, so a field or copy is inline and a copy is the C# struct
copy, exactly as Go's. *Removes:* every field-face and copy-face object for the admitted types.
*Preconditions:*

- **The shared `slice<T>`-backing question.** `slice<T>` requires a `T[]` backing (zh-box §6,
  :520-522), so an array that is ever sliced (`e.x[:]`, `cr.buf[:2]` at chunked.cs:114) cannot take
  inline storage until slice<T> can window something other than a `T[]`. The options, none chosen
  here: a third backing kind (an owner object plus an offset), or lowering the slice to a `Span<T>`
  once its non-escape is proven (shared with REC-B).
- **Q74-5** (`DESIGN-value-field-representation.md` §6): `array<T>`'s window (`m_low`/`m_length`)
  exists so `(*[N]T)(s)` can alias existing storage. An inline field and an aliasing view are two
  representations of one Go type, and each increment says which sites get which.
- **zh-box §6's 64 KB null-byref precondition** (:522-526, S-F6): inline layouts create the field
  offsets at which a null-byref dereference becomes an unmappable `AccessViolationException`; every
  null-deferring byref path is re-audited against the 64 KB null partition first.
- **The struct-growth byte cost.** A representation change that makes a struct BIGGER states it as a
  per-row formula in its own commit. Q74's precedent: `pallocData` goes from two 16-byte references to
  two 64-byte inline blocks, +96 B per instance (`DESIGN-value-field-representation.md` §4).
- `[GoValueClone]` decisions for the admitted structs become dead and are removed with them (Q74-4).

## 4. Refusals

An array that is sliced, address-taken as a whole (`&a`), converted to or from `unsafe.Pointer`,
viewed over native memory, or of a managed element type stays `array<T>` until the backing question
is answered for its face. A copy whose source or destination escapes keeps its clone.

## 5. Predictions (per entry; counted objects per run, i7 readings)

| entry | today | after this record's stages alone |
|:--|--:|:--|
| crypto/md5 TestAllocations | 6 | 1 (the `heap<digest>` box, md5.cs:167, which is zh-box B′'s); the uncounted literal temp's bytes remain until the literal face lands |
| crypto/sha1 TestAllocations | 7 | 1 (the `heap<digest>` box, B′) |
| crypto/sha256 TestAllocations | 46 | 10 (4 B′ boxes + 4 class-3b `@new<Digest>` + 2 slices, REC-B); then BYTES on the 2 uncounted shells per run |
| crypto/sha512 TestAllocations | 106 | 18 (16 class-3b + 2 slices, REC-B); then BYTES on the 4 uncounted shells per run |
| net/http/internal TestChunkReaderAllocs | 2 | 1 (Go's own allocation); the retiring commit states the pass rests on the counter's coverage boundary (the shells at chunked.cs:33/:114 are uncounted) |
| the curve family and ed25519 | 75 to 17,086 | not decomposed; the share this record removes is measured at the census gate |
| log/slog TestAlloc/* | per entry | one fewer per enabled call (F3) |

## 6. Gates

Census first: the population of array VALUE sites per face, measured before any increment, with
three positive controls that must appear in it -- the md5 digest copy (md5.cs:168), `chunkedReader.buf`
(chunked.cs:40) and sha512's `d0` (sha512.cs:272-273). Each increment then reads the members' rows
before and after at Release with tiering off, and a want-0 member retires only at ZERO BYTES
(testing.cs:743).
