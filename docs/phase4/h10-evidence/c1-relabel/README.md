# The H10 allocation relabel at go1.24.13 -- what moved, and what is owed a ruling

**Record** (C1, 2026-09-23). Point-in-time. The manifests on this ref are the relabel; this file is
the per-entry account of it and the RULING OWED list, with every reading cited from the i7 reading run
(`claude/coord-h10-readings` ac9f8251ee, `docs/phase4/h10-evidence/i7-readings/readings-go1.24.13.tsv`:
tree bb54ff0920, Release with tiering off). Sizing it continues: `CENSUS-alloc-label-relabel-go1.24.13.md`
on `claude/c1-alloc-relabel-sizing` c8e6ca9034. Site attributions marked READ were read in the emission
at bb54ff0920; nothing here was compiled or run by C1.

## The instrument fact that moved three census families

golib's `AllocationCounter` charges golib's own allocation sites only: `@string` bodies, `array<T>`'s
`T[]`, slice backings, maps, channels and `ж` boxes (`src/core/golib/AllocationCounter.cs`, its
*Coverage* remarks). It does **not** see a CLR box at an interface conversion, a source-generated
interface shell (`src/gen` has no counter call), a closure display class, a delegate or a params
array. So a COUNT reading is never explained by a boxing or shell proof: the counted objects are
golib's. The census's STRUCTURAL proposals for `log/slog` (17), `encoding/binary` (8) and
`net/http/internal` (1) all rested on boxing or shell proofs, so they are **withdrawn** and owed a
ruling below. `encoding/binary`'s three value-typed `TestSizeAllocs` subtests read exact zero on
Windows (bytes zero too), which falsifies that family's proof outright.

## Relabelled on this ref: 79 entries in 8 manifests

| row | entries | from -> to | readings (per run, COUNT unless stated) | plan family |
|:--|--:|:--|:--|:--|
| `net/netip` | 54 | alloc-profile -> deferred | 1 to 106 (`TestAddrStringAllocs/ipv6` 106) | zh-box reduction arc (the reasons already name its netip harvest) |
| `strconv` | 10 | alloc-profile -> deferred | 1 to 4 | string-conversion family (tmpstring); Atoi READ: `string(b)` + a per-call `"Atoi"u8` local + `range []byte(s)` = 3 |
| `bytes` | 4 | alloc-profile -> deferred | TestIndex 2, TestLastIndex 2, TestIndexRune 1, TestNewBufferShallow 1 | TestIndex/LastIndex READ: generic `IndexRabinKarp[T]` renders `string(a) == string(b)` via `ToGoString` (bytealg.cs:82/90/110) where `bytes.Equal` already uses the `sstring` view; IndexRune: rune arm; NewBufferShallow: zh-box |
| `strings` | 2 | TestBuilderAllocs alloc-count-semantics -> deferred; TestIndexRune alloc-profile -> deferred | 2 (want 1); 2 | zh-box (READ: `ж<Builder>`, builder.cs:26-35); rune arm |
| `bufio` | 1 | alloc-profile -> deferred | 2 (want 1) | zh-box (READ: `heap(new strings.Builder(), ...)`, bufio.cs:516) |
| `crypto/internal/fips140test` | 6 | alloc-profile -> structural | pass-2 readings already in the reasons | as ruled (edwards/nistec pre-ruled; XAES 2026-09-22) |
| `log/slog/internal/buffer` | 1 | alloc-profile -> alloc-count-semantics | BYTES 176 B/run | none (incomparable unit) |
| `database/sql` | 1 | TestGrabConnAllocs alloc-profile -> alloc-count-semantics | BYTES 96 B/run | none; supersedes the census's owed floor proposal |

Every deferred entry carries `want`, `reading` (the TSV figure, the tree, the configuration and the
test's own message) and `plan`; every reason keeps its old text and gains one dated RELABEL paragraph.
`TestParsePrefixAllocs/<IP-prefix subtest>` (2) read the FIRST call's unit (ParseAddr's); the failing
figure is the test's own differential, and the reading says so.

**Gates run (C1, Go side only):** `go test -count=1 ./...` in `src/go2cs` at go1.24.13, rc 0.
Control: `TestEveryCommittedManifestLoadsUnchanged` went red naming `bufio`'s `TestReadStringAllocs`
when its `plan` was deleted, and the file was restored byte-identical. **Omitted:** every .NET leg,
`check-roster-format.ps1` (no PowerShell here; its 2c arm checks the same three fields), and the rows.

## 53 entries stay at their current label: 44 owed a ruling (O1-O5), 3 confirmed, 6 pending a reading

### O1. The fixed-array family (a Go array VALUE is `array<T>` over a heap `T[]`)
One question decides it: deferred against docs/phase4/DESIGN-value-field-representation.md option (C)
(Q74, PROPOSED; its recommendation (D) scopes (C) to native-boundary structs, which covers none of
these) plus the crypto/rand non-escaping-buffer precedent for locals -- or structural, as the
edwards25519/nistec field-element temporaries were ruled.

| entry | reading | site |
|:--|:--|:--|
| `crypto/md5` TestAllocations | 6 | digest `[4]uint32` + `[64]byte` fields deep-copied by `d0 := *d` (READ: md5.cs:39-40) |
| `crypto/sha1` TestAllocations | 7 | same digest-copy shape |
| `crypto/sha256` TestAllocations | 46 | same digest-copy shape |
| `crypto/sha512` TestAllocations | 106 | same digest-copy shape |
| `net/http/internal` TestChunkReaderAllocs | 2 (want 1) | READ: `chunkedReader.buf` is `array<byte> buf = new(2)` (chunked.cs:40); the reason's second object, the interface shell, is uncounted |
| `bytes` TestWriteAppend | 900 | READ: `strconv.formatBits` declares `array<byte> a = new(65)` per call (itoa.cs:89); 900 = the 3-digit values of 0..999 |
| `crypto/ed25519` TestAllocations | 9,503 | edwards25519 temporaries (ruled structural in fips140test) + SHA-512 digest copies (this family); follows O1 |

### O2. Boxing proofs withdrawn; the counted sites are owed an emission read
| entry | reading | known |
|:--|:--|:--|
| `log/slog` TestAlloc/* (13) | 2, 2, 2, 2, 6, 10, 4, 27, 7, 12, 9, 21, 28 | `Record.front` is `array<Attr> front = new(5)` (READ: record.cs:38) -- O1 in part; the rest unread |
| `log/slog` TestAnyLevelAlloc | 1 | unread |
| `log/slog` TestTextHandlerAlloc | 20 on the first call; the failing call prints 44, unit unrecorded | unread |
| `log/slog` TestAttrNoAlloc, TestValueNoAlloc | 14, 15 | unread |
| `encoding/binary` TestAppendAllocs | 75 (one run) | unread |
| `encoding/binary` TestSizeAllocs `*Struct`, `[]Struct`, `[]Struct#01`, `[1]Struct` | 1 each | the reflect path, unread |

Proposal: deferred per site once read; none is structural on the evidence in hand.

### O3. An instrument question, not a label
`bytes` TestGrow: counted 14 objects over 100 runs. Go's `AllocsPerRun` divides as integers and would
report 0, which is what Go's own test relies on for amortized growth. The host reports
`Math.Max(1L, counted / runs)` = 1 (`src/core/testing/testing.cs`, AllocsPerRun). The floor exists so
that uncounted bytes cannot pass as zero; on the COUNT arm it also turns Go's own truncation into a
failure. A ruling on the COUNT arm's floor decides this entry. It is also first-call-only (the other
growLen/startLen legs are unrecorded).

### O4. alloc-count-semantics premises the readings contradict (COUNT, not bytes)
| entry | reading | proposal |
|:--|:--|:--|
| `strings` TestBuilderGrowSizeclasses | 3 (want <= 1) | deferred: `ж<Builder>` (zh-box) + READ: hand-owned `bytealg.MakeNoZero` allocates `new byte[n]` (bytealg_impl.cs:24) where Go rounds the capacity up to a size class (`runtime/slice.cs:408` `roundupsize`), so Grow(18) then 19 bytes regrows |
| `slices` TestGrow | 2 (want 1) | deferred: Go compiles `append(s[:cap(s)], make([]E, n)...)` without the `make` (extendslice); a converter idiom recognition, no design record yet |
| `io` TestPipeAllocations | 14 | deferred (zh-box), sites unread |
| `strings` TestBuilderGrow | 2 on the first call; later legs unrecorded | first-call only: keeps its label until the failing call's unit is read |
| `context` TestAllocs | 1 on the first call; the failing call prints 9 | first-call only, as above |
| `testing` TestAllocsPerRun | 1 on the first call; the failing call is `alloc complex128` = 2 | first-call only; the likely site is `ж<T>`'s pinnable slot over an unmanaged T (counts 2 by the counter's own remarks) |

`sync` TestMapClearOneAllocation (BYTES 896 B/run), TestMapRangeNoAllocations (BYTES 152 B/run) and
`slices` TestConcat (BYTES 168 B/run on the first call) are CONFIRMED alloc-count-semantics and unchanged.

### O5. The census's owed entries, now with readings
| entry | reading | proposal |
|:--|:--|:--|
| `crypto/rsa` TestAllocations | 174,351 (budget 10; was 340,756) | structural, consistent with nistec (its reason cites the same r56d shapes) |
| `slices` TestInsert | 58 (budget < 25; was 242) | deferred (zh-box); the reading is moving toward the budget |
| `database/sql` TestRawBytesAllocs | 28 (was 15: moved AWAY from the want, cause unattributed) | deferred with no floor: the census's floor was any-boxing, which is uncounted |
| `mime` TestLookupMallocs | 3 | deferred with no floor, for the same reason |
| `log` TestDiscard | 2 (want <= 1; was 3) | deferred: the params pack's drop is confirmed; the two counted sites are unread |
| `os` TestUTF16Alloc (windows) | 2 (want 1) | deferred against docs/phase4/DESIGN-string-byte-window.md, which names itself this entry's plan |
| `unicode/utf16` TestAllocationsDecode | 1 on the first call (three legs print 1) | deferred: the non-escaping `[]rune` result, the crypto/rand precedent |
| `math/big` TestMulUnbalanced | no unit note (MemStats.TotalAlloc): 10,506,112 bytes > 26x the inputs | deferred once a plan record exists (Go's nat pooling); none exists today |

### Pending a reading, not a ruling
- `encoding/binary` TestSizeAllocs complex64, complex128, binary.Struct: scoped `[linux, darwin]`; the Linux reading.
- `math/big` TestNewIntAllocs: scoped `[linux, darwin]`, Go fails it too; retires at the Linux refresh.
- `net` TestAllocs, TestTCPReadWriteAllocs, and the unpinned TestIPAppendTextNoAllocs (ruled deferred): the net reading.
