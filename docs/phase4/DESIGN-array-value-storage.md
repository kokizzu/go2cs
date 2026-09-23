# DESIGN (stub) -- REC-A: storage for a Go array VALUE (the zh-box record's class 4)

> **Status: STUB, 2026-09-23.** Minted by the H10 relabel ruling (ledger 2026-09-23 03:37, X(2) and
> O1) so that the fixed-array family's `deferred` entries cite a record that names a stage which
> removes the counted allocation. **Owner: C1. Full design: phase-4D kickoff.** Nothing is cut
> against this stub; every line number below was read at `bb54ff0920` (the batch-8b stamp) unless it
> says otherwise, and every figure is the i7 reading run's (`claude/coord-h10-readings` ac9f8251ee,
> Release with tiering off). Feasibility is stated UNMEASURED where it is not measured.
>
> **Fixed up 2026-09-23** per COORD's ACCEPT-WITH-FIXES (ledger 3942e083ad; list
> `docs/phase4/briefs/h10-relabel-fixup.md` at claude/coord-handover 83e4f16ea6, items 2 and 4): the
> md5/sha LOCAL arrays moved to REC-B (COORD's ruling 2), a managed-element arm and a literal-face
> stage added, and §5's shares re-derived from the emission.

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
this record's: their question is frame lifetime, not representation. That includes the digests'
locals -- `tmp` and `digest` in each `checkSum` (md5.cs:184/:193, sha1.cs:180/:196,
fips140/sha256/sha256.cs:209/:225, fips140/sha512/sha512.cs:283/:302), the block functions' `w`
(sha1block.cs:19, sha256block.cs:82, sha512block.cs:98) and the package `Sum*` functions' `sum`
(crypto/sha256/sha256.cs:61/:73, crypto/sha512/sha512.cs:80 and siblings) -- per COORD's ruling 2
(O1: "REC-B for local arrays"). The `.Clone()` that copies such a local OUT stays this record's copy
face.

## 2. Members (by entry name)

`crypto/md5` TestAllocations; `crypto/sha1` TestAllocations; `crypto/sha256` TestAllocations;
`crypto/sha512` TestAllocations; `net/http/internal` TestChunkReaderAllocs; `crypto/ed25519`
TestAllocations (its Scalar/fiat arrays and SHA-512 digest copies); `crypto/internal/fips140test`
TestEdwards25519Allocations, TestNISTECAllocations/P224, /P256, /P384, /P521 (the fiat field-element
arrays); `log/slog` TestAlloc/* (F3, `Record.front`, through the managed-element arm of §3). Every
one is a MIXTURE and names its other families in its own entry.

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

3. **The literal face: element-wise initialisation.** `var tmp = new byte[]{0x80}.array(72)` (md5.cs:184)
   builds a C# array from the literal -- a `new byte[]` the counter does not see -- and then copies it
   into the 72-byte `array<T>`. Emit the literal's elements into the destination instead
   (`array<byte> tmp = new(72); tmp[0] = 0x80;`). *Removes:* the uncounted literal array, which md5
   needs because it retires only at ZERO BYTES; the counted destination is REC-B's local.
   *Precondition:* the literal's element count does not exceed the destination's length.

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

**The managed-element arm.** `[InlineArray]` accepts reference-containing elements, so a `[N]T` of a
MANAGED `T` is admitted when the array is never sliced, never address-taken as a whole, never pinned and
never viewed over native memory: the slice-backing and window questions then do not arise, while the
64 KB null-byref audit and the struct-growth cost still apply. `slog.Record.front` is `array<Attr>` (log/slog/record.cs:38) over a managed `Attr` (a `@string`
and a `Value` carrying an `any`), and every use of it is an element index or `len` (record.cs:85, :106,
:111, :145-146). *Removes:* F3, one counted object per `NewRecord`.

## 4. Refusals

An array that is sliced, address-taken as a whole (`&a`), converted to or from `unsafe.Pointer`, or
viewed over native memory stays `array<T>` until the backing question is answered for its face; a
managed element type is admitted only by the managed-element arm's four conditions. A copy whose source or destination escapes keeps its clone.

## 5. Predictions (per entry; counted objects per run, i7 readings)

| entry | today | after this record's stages alone |
|:--|--:|:--|
| crypto/md5 TestAllocations | 6 | 3: REC-A takes the copy face (ΔClone's 2 arrays at md5.cs:168, `.Clone()` at :198); left are REC-B's 2 locals (`tmp`, `digest`) and B′'s `heap<digest>` box (:167). The literal stage removes the uncounted `new byte[]` |
| crypto/sha1 TestAllocations | 7 | 4: REC-A takes the copy face (ΔClone 2 + `.Clone()` 1); left are REC-B's 3 locals (`tmp`, `digest`, the block's `w`) and B′'s box |
| crypto/sha256 TestAllocations | 46 | 24: REC-A takes 22 -- the 8 field arrays of the four `@new<Digest>` and the copy face (ΔClone 8, `.Clone()` 4, `sum.Clone()` 2); left are REC-B's 14 locals (`tmp` 4, `digest` 4, `w` 4, `sum` 2), 4 class-3b `@new<Digest>`, 4 B′ boxes and 2 slices; then BYTES on the 2 uncounted shells per run |
| crypto/sha512 TestAllocations | 106 | 46: REC-A takes 60 -- 32 field arrays (16 of `New*`'s `@new`, 16 zero fields of `Sum`'s `d0`, which increment 2 removes) and the copy face (ΔClone 16, `.Clone()` 8, `sum.Clone()` 4); left are REC-B's 28 locals, 16 class-3b and 2 slices; then BYTES on the 4 uncounted shells per run |
| net/http/internal TestChunkReaderAllocs | 2 | 1 (Go's own allocation); the retiring commit states the pass rests on the counter's coverage boundary (the shells at chunked.cs:33/:114 are uncounted) |
| the curve family and ed25519 | 75 to 17,086; 9,503 | UNMEASURED: not decomposed; the share this record removes is measured at the census gate |
| log/slog TestAlloc/* | per entry | one fewer per enabled call (F3, the managed-element arm) |

## 6. Gates

Census first: the population of array VALUE sites per face, measured before any increment, with
three positive controls that must appear in it -- the md5 digest copy (md5.cs:168), `chunkedReader.buf`
(chunked.cs:40) and sha512's `d0` (sha512.cs:272-273). Each increment then reads the members' rows
before and after at Release with tiering off, and a want-0 member retires only at ZERO BYTES
(testing.cs:743).
