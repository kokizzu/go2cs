# DESIGN (stub) -- REC-B: allocations Go's escape analysis keeps in the frame

> **Status: STUB, 2026-09-23.** Minted by the H10 relabel ruling (ledger 2026-09-23 03:37, X(2), O1,
> X(3)) so that the non-escaping-local family's `deferred` entries cite a record that names a stage
> which removes the counted allocation. **Mechanism owner: G. Stub written by C1. Full design:
> phase-4D kickoff.** Nothing is cut against this stub; line numbers are read at `bb54ff0920` and
> figures are the i7 reading run's (`claude/coord-h10-readings` ac9f8251ee, Release with tiering off),
> unless a line says otherwise. Feasibility is UNMEASURED.

## 0. The population

Every allocation Go's escape analysis proves stays in the frame, so Go never heap-allocates it, and
golib's model heap-allocates it on every evaluation:

| shape | example read at `bb54ff0920` |
|:--|:--|
| class-3b `new(T)` that stays in the frame after inlining | `@new<Digest>()` in crypto/sha256 and crypto/sha512 `Sum` (sha512.cs:272); the edwards25519 and nistec point/element temporaries |
| constant-capacity `make` | crypto/rand TestAllocations' `make([]byte, 32)`; unicode/utf16 `Decode`'s `make([]rune, 0, 64)` (utf16.go:119, whose comment says "Decode inlines, so the allocation can live on the stack"); XAES's five local slices |
| local fixed arrays | strconv `formatBits`' `array<byte> a = new(65)` (strconv/itoa.cs:89); mime `TypeByExtension`'s `array<byte> buf = new(10)` (mime/type.cs:123); log/slog's `array<uintptr> pcs = new(1)` (log/slog/logger.cs:293 and :315) |
| `[]byte(const)` handed to a callee that does not retain it | unicode/utf8 TestRuneCountNonASCIIAllocation's site 1 (its entry's `proof`) |

The zh-box record's §6 (`DESIGN-zh-box-reduction.md`:527-532) calls the first shape class 3b and
leaves it to "its own design document if it is ever wanted"; this is that document's stub.

## 1. Members (by entry name)

`crypto/internal/fips140test` TestEdwards25519Allocations, TestNISTECAllocations/P224, /P256, /P384,
/P521, TestXAESAllocations; `crypto/ed25519` TestAllocations; `crypto/rand` TestAllocations;
`crypto/sha3`'s four pins; `unicode/utf16` TestAllocationsDecode; `crypto/rsa` TestAllocations (`T`
and `NewNat`); `net` TestIPAppendTextNoAllocs (unpinned; ruled deferred on this record, 2026-09-22
21:09); `bytes` TestWriteAppend (formatBits' `a`); `crypto/sha256` and `crypto/sha512`
TestAllocations (class 3b and two slices each); `mime` TestLookupMallocs (type.cs:123); `log/slog`
TestAlloc/* (F2, `pcs`); and the TRIGGER for `unicode/utf8` TestRuneCountNonASCIIAllocation's floor
of 1, which is re-examined at this record's acceptance and before any seat brings that reading to 1.
`database/sql` TestRawBytesAllocs names formatBits' `a` as a candidate, unattributed.

## 2. Candidate mechanisms

1. **An escape oracle from the pinned toolchain.** The converter reads Go's own decision rather than
   re-deriving it: `go build -gcflags=-m` at the pinned toolchain prints, per site, "does not escape"
   or "moved to heap". Recorded per package at conversion time, it answers the question this whole
   family turns on with Go's own answer. *Precondition:* the oracle's output is keyed to source
   positions the converter already carries, and a site the oracle does not mention is treated as
   escaping.
2. **Frame-local storage for an oracle-proven site.** `stackalloc`/`Span<T>` for a constant-size
   unmanaged buffer; an `[InlineArray(N)]` local for a fixed array; a value carrier in place of a
   `ж<T>` box for a `new(T)` whose pointer never leaves the frame.

**The first stage that REMOVES a counted allocation:** a local fixed array of an unmanaged element
type and constant length, oracle-proven non-escaping, whose every use is an index, `len`, or a slice
expression consumed by a callee that copies from it and does not retain it (formatBits' `a[i:]` into
`append` or `string(...)`). Emitted as frame-local storage with the slice windowing it for the call.
*Removes:* one counted object per evaluation (900 per run on bytes TestWriteAppend).
*Preconditions:* the slice over frame storage needs a `slice<T>` backing that is not a `T[]` -- the
question REC-A §3 shares (`DESIGN-native-backed-slice.md` is the ratified precedent for a non-array
backing); and the 64 KB null-byref audit of zh-box §6 applies to any inline layout.

## 3. Refusals

A site the oracle does not prove non-escaping; a buffer whose slice is stored, returned, captured by a
closure, sent on a channel or passed to a callee not proven non-retaining; a managed element type; a
length that is not a constant.

## 4. Predictions (counted objects per run)

| entry | today | after the first stage |
|:--|--:|:--|
| bytes TestWriteAppend | 900 | 0 counted; about 56 B/call of uncounted residue (an inference) is attributed before a 0-byte retirement |
| mime TestLookupMallocs | 3 | 2 |
| log/slog TestAlloc/* | per entry | one fewer per enabled call (F2) |
| the class-3b and `make` members | per entry | unchanged by the first stage; the value-carrier and `make` stages are sized at the kickoff |

## 5. Gates

The oracle is controlled both ways before any emission uses it: a site Go reports "moved to heap" must
keep its allocation, and a site Go reports "does not escape" must be the only kind admitted. Each stage
then reads its members' rows before and after at Release with tiering off; a want-0 member retires
only at ZERO BYTES (testing.cs:743).
