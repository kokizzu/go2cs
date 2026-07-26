# Validated Test Packages

The packages below have had their **own Go test suites** (Go 1.23.1) converted to C#, built against
the converted standard library, run under the Go-semantics test host, and **differentially compared
against a clean `go test -json` baseline — verdict for verdict**. A package is listed only when every
`Test` function's result matches `go test` (`Example`/`Benchmark` execution is uniformly deferred).
See [Try it yourself](README.md#try-it-yourself--validate-a-converted-test-suite) to reproduce any
row from a clone with one command.

A few packages carry **disclosed divergences**: Go asserts the managed CLR provably cannot satisfy.
Two classes exist. `alloc-profile` — a test asserting an *exact allocation count*, where Go's compiler
stack-allocates and .NET must heap-allocate. `codegen-liveness` — a test asserting, from inside its own
frame, that an object it just stopped using is collectible: Go's GC consults per-safepoint liveness
maps and drops a local at its last use, while the CLR reports a frame's slots live for the frame's
lifetime (a by-value struct argument wider than a machine word is passed by hidden reference, so the
caller's temp is address-exposed and therefore untracked). Each is pinned by exact failure signature in
a hand-owned, committed
[`go2cs_test_disclosures.json`](https://github.com/ritchiecarroll/go2cs/blob/master/src/go-src-converted/bytes/go2cs_test_disclosures.json) —
any other failure is still a hard mismatch.

> ### Phase 4 progress: **51 / 215 testable packages validated — 23.7%**
>
> **1,286 matching test verdicts · 43 disclosed** *(updated 2026-07-26 — maintained as part of the
> Phase-4 validation campaign and grows as packages validate. Denominator: the 215 of 302 converted
> standard-library packages whose Go 1.23.1 sources define `Test` functions.)*

| Package | Tests | Disclosed | What it exercises |
|:--|:--:|:--:|:--|
| [`bytes`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/bytes) | 81 | 7 | Byte-slice algorithms; alloc-profile disclosures. |
| [`cmp`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/cmp) | 4 | | Generics with an ordered-type constraint. |
| [`compress/bzip2`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/compress/bzip2) | 4 | | Bzip2 decompression — bit readers, Huffman trees, the move-to-front decoder. |
| [`container/heap`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/container/heap) | 7 | | Heap interface over a slice. |
| [`container/list`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/container/list) | 10 | | Doubly-linked list — pointers and receiver methods. |
| [`container/ring`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/container/ring) | 8 | | Circular linked list — a pointer graph. |
| [`encoding/ascii85`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/encoding/ascii85) | 9 | | Ascii85 encode/decode and streaming wrappers. |
| [`encoding/base32`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/encoding/base32) | 26 | | Base32 round-trips; `io.Pipe` rendezvous over the real channel core. |
| [`encoding/base64`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/encoding/base64) | 17 | | Base64 round-trips; goroutine + `time.After` timer path. |
| [`encoding/binary`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/encoding/binary) | 137 | 9 | Reflection-driven Read/Write — the bridge's construction/write-back surface. |
| [`encoding/csv`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/encoding/csv) | 71 | | CSV parsing; wrapped-error `errors.Is` through the reflection bridge. |
| [`encoding/hex`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/encoding/hex) | 12 | | Hex encode/decode and error paths. |
| [`encoding/pem`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/encoding/pem) | 8 | | PEM block parsing and round-trips. |
| [`errors`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/errors) | 61 | | `errors.Is`/`As`/`Join` — reflection-bridge write-back (`Value.Set`, addressability). |
| [`go/build/constraint`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/go/build/constraint) | 89 | | Build-constraint expression parsing. |
| [`go/token`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/go/token) | 31 | | FileSet/Position machinery; a full `encoding/gob` serialization round-trip — the reflect type-relation mirrors driving real Encoder/Decoder engines. |
| [`go/version`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/go/version) | 3 | | Go version-string comparison. |
| [`hash/adler32`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/hash/adler32) | 2 | | Adler-32 checksum. |
| [`hash/crc32`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/hash/crc32) | 10 | | CRC-32 including **real SSE4.2/PCLMULQDQ hardware paths** via managed intrinsics. |
| [`hash/crc64`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/hash/crc64) | 5 | | CRC-64 checksum tables. |
| [`hash/fnv`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/hash/fnv) | 19 | | FNV-1/FNV-1a across widths. |
| [`internal/abi`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/internal/abi) | 2 | | Runtime ABI helpers (`FuncPC`). |
| [`internal/coverage/slicereader`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/internal/coverage/slicereader) | 1 | | Coverage slice reader. |
| [`internal/coverage/slicewriter`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/internal/coverage/slicewriter) | 1 | | Coverage slice writer. |
| [`internal/fmtsort`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/internal/fmtsort) | 3 | | `fmt`'s map-key ordering — `Value.Convert`, arithmetically-ordered pointer/channel tokens, `-tests` init-order relocation. |
| [`internal/gover`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/internal/gover) | 5 | | Toolchain version ordering. |
| [`internal/itoa`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/internal/itoa) | 3 | | Minimal integer formatting. |
| [`internal/saferio`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/internal/saferio) | 17 | | Allocation-capped I/O helpers. |
| [`io/fs`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/io/fs) | 18 | | The `fs.FS` interface family — named-interface runtime shells, `fs.Glob` deep recursion, `dirFS` walks. |
| [`maps`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/maps) | 14 | | Generic map helpers and iterators. |
| [`math`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/math) | 76 | | The core numeric package — IEEE edge cases, rounding, `Inf`/`NaN`. |
| [`math/bits`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/math/bits) | 26 | | Bit-manipulation intrinsics. |
| [`math/cmplx`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/math/cmplx) | 24 | | `complex128` transcendental math. |
| [`math/rand`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/math/rand) | 43 | | PRNG streams, including a child-process race test. |
| [`math/rand/v2`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/math/rand/v2) | 36 | | The v2 PRNG API (PCG, ChaCha8). |
| [`mime`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/mime) | 17 | 1 | MIME type tables and media-type parsing — the first package through the runtime process-control facade (`LockOSThread`, registry reads). |
| [`net/http/internal/ascii`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/net/http/internal/ascii) | 13 | | ASCII case-insensitive helpers. |
| [`os/signal`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/os/signal) | 1 | | Console-signal delivery (Ctrl+Break) through real channels and `select`. |
| [`path`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/path) | 9 | | Pure path manipulation (`Clean`/`Split`/`Join`/`Match`…). |
| [`regexp`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/regexp) | 45 | | The full RE2 engine — NFA/backtracker/one-pass executors, the RE2 exhaustive corpus, `TextMarshaler` round-trips. |
| [`regexp/syntax`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/regexp/syntax) | 12 | | Regexp parsing, simplification and program compilation; named-type constant tables. |
| [`sort`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/sort) | 63 | | Interface-driven sort, `sort.Slice` reflection swaps, NaN-aware ordering, stability. |
| [`strconv`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/strconv) | 55 | 11 | Number↔string conversion at full precision — Ryū/Grisu float formatting, arbitrary-precision decimal shifts, complex parsing; alloc-profile disclosures. |
| [`strings`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/strings) | 68 | 4 | String algorithms; alloc-count/alloc-profile disclosures. |
| [`sync`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/sync) | 41 | 10 | The concurrency crown — `Mutex`/`RWMutex`/`WaitGroup`/`Once`/`Cond`/`Map`/`Pool` over real parked-thread semaphores, a hand-owned lock-free pool ring, and GC-integrated cleanup; `Cond`'s copy detector on root-allocation identity; alloc-profile and codegen-liveness disclosures. |
| [`testing/quick`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/testing/quick) | 8 | | Property testing — `reflect` value generation and `Value.Call` dynamic invocation. |
| [`text/scanner`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/text/scanner) | 18 | | Rune-level source scanning. |
| [`text/tabwriter`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/text/tabwriter) | 3 | | Elastic-tab column formatting; panic-during-write recovery. |
| [`unicode`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/unicode) | 28 | | Category tables, case mapping (`SpecialCase`), script ranges. |
| [`unicode/utf16`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/unicode/utf16) | 8 | 1 | Encode/decode round-trips via `reflect.DeepEqual`. |
| [`unicode/utf8`](https://github.com/ritchiecarroll/go2cs/tree/master/src/go-src-converted/unicode/utf8) | 14 | | UTF-8 encode/decode — the first suite to pass (2026-07-17). |
