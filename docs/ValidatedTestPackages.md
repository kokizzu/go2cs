# Validated Test Packages

Each package below has its own Go 1.23.1 `_test.go` suite converted to C#, built against the
converted standard library, run under the Go-semantics test host, and differentially compared —
verdict for verdict — against a clean `go test -json` baseline. A row appears only when *every*
`Test` function's result matches `go test`; a package that almost passes never appears, which is
what keeps the denominator below honest. `Example`/`Benchmark` execution is deferred and never
factors into a row. [`src/run-validated-sweep.ps1`](../src/run-validated-sweep.ps1) re-validates
every listed package on demand, reading its own roster straight from the table below — see
[Try it yourself](README.md#try-it-yourself--validate-a-converted-test-suite) to reproduce any row
from a clone with one command.

A disclosure is a specific Go assertion the managed CLR provably cannot satisfy — not a skipped
test, not a tolerance. Two classes exist:

- **`alloc-profile`** — a test asserts an exact allocation count; Go's compiler stack-allocates the
  value where .NET must heap-allocate it.
- **`codegen-liveness`** — a test asserts, from inside its own frame, that an object it just stopped
  using is now collectible. Go's GC drops a local at its last use via per-safepoint liveness maps;
  the CLR reports a frame's slots live for the frame's whole lifetime.[^codegen-liveness]

Each disclosure is pinned by exact failure signature in a hand-owned, committed
[`go2cs_test_disclosures.json`](https://github.com/ritchiecarroll/go2cs/blob/master/src/core/bytes/go2cs_test_disclosures.json).
Any other failure is still a hard mismatch, and packages without a manifest compare strictly.

> ### Phase 4 progress: **72 / 215 testable packages validated — 33.5%**
>
> **2,553 matching test verdicts · 51 disclosed** *(updated 2026-08-02 — maintained as part of the
> Phase-4 validation campaign and grows as packages validate. Denominator: the 215 of 302 converted
> standard-library packages whose Go 1.23.1 sources define `Test` functions.)*

<!-- Row format is machine-parsed by src/run-validated-sweep.ps1 (regex:
     ^\|\s*\[`pkg`\]\(...\)\s*\|\s*tests\s*\|\s*disclosed\s*\|). Keep one row per line in this exact
     column order — reflowing, reordering, or adding columns breaks the sweep's roster parser. -->

| Package | Tests | Disclosed | What it exercises |
|:--|:--:|:--:|:--|
| [`bufio`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/bufio) | 80 | 1 | Buffered reader/writer/scanner — fill, rewind, split functions, `io` error propagation. · [proof](validation/current/bufio.md) |
| [`bytes`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/bytes) | 81 | 7 | Byte-slice algorithms; alloc-profile disclosures. · [proof](validation/current/bytes.md) |
| [`cmp`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/cmp) | 4 | | Generics with an ordered-type constraint. · [proof](validation/current/cmp.md) |
| [`compress/bzip2`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/compress/bzip2) | 4 | | Bzip2 decompression — bit readers, Huffman trees, the move-to-front decoder. · [proof](validation/current/compress.bzip2.md) |
| [`compress/flate`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/compress/flate) | 64 | | DEFLATE itself — all ten compression levels, the Huffman bit-writer's stored/fixed/dynamic block selection against golden bit streams, the LZ77 match chains and dictionaries, and a whole-`Writer` `reflect.DeepEqual` after `Reset`. · [proof](validation/current/compress.flate.md) |
| [`compress/gzip`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/compress/gzip) | 15 | | Gzip round-trips over the real DEFLATE coder — flate's Huffman encoder/decoder tables, multistream framing, CRC/ISIZE trailers. · [proof](validation/current/compress.gzip.md) |
| [`compress/lzw`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/compress/lzw) | 17 | | LZW coder in both bit orders (GIF's LSB, TIFF/PDF's MSB) — code-width growth, dictionary reset, and the reader/writer `Reset` matrix over the shared `../testdata` corpus. · [proof](validation/current/compress.lzw.md) |
| [`compress/zlib`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/compress/zlib) | 6 | | zlib framing over the real DEFLATE coder — Adler-32 trailer, preset dictionaries, and every compression level across the shared `../testdata` corpus. · [proof](validation/current/compress.zlib.md) |
| [`container/heap`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/container/heap) | 7 | | Heap interface over a slice. · [proof](validation/current/container.heap.md) |
| [`container/list`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/container/list) | 10 | | Doubly-linked list — pointers and receiver methods. · [proof](validation/current/container.list.md) |
| [`container/ring`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/container/ring) | 8 | | Circular linked list — a pointer graph. · [proof](validation/current/container.ring.md) |
| [`context`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/context) | 57 | 1 | Cancellation trees over real channel rendezvous — parent/child propagation, `Done` broadcast, `AfterFunc` registration races, `t.Deadline`-driven tree cancellation, value chains named through the reflectlite bridge; alloc-count disclosure. · [proof](validation/current/context.md) |
| [`crypto/hmac`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/crypto/hmac) | 172 | | HMAC over the real MD5/SHA-1/SHA-224/256/384/512 digests — block-size key folding, constant-time `Equal`, and `cryptotest.TestHash`'s stateful-write matrix per hash. · [proof](validation/current/crypto.hmac.md) |
| [`crypto/md5`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/crypto/md5) | 11 | 1 | MD5 — the golden digest matrix, binary marshal/unmarshal of a half-written state, large-input block handling, and `cryptotest.TestHash`'s stateful-write matrix; alloc-profile disclosure. · [proof](validation/current/crypto.md5.md) |
| [`crypto/sha1`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/crypto/sha1) | 12 | 1 | SHA-1 — the struct-carrying-arrays value copy `Sum` depends on; binary marshal round-trips. · [proof](validation/current/crypto.sha1.md) |
| [`crypto/sha256`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/crypto/sha256) | 23 | 1 | SHA-224/256 golden vectors and `cryptotest.TestHash`'s stateful-write matrix. · [proof](validation/current/crypto.sha256.md) |
| [`crypto/sha512`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/crypto/sha512) | 36 | 1 | SHA-384/512/512-224/512-256 — the four-variant digest state machine. · [proof](validation/current/crypto.sha512.md) |
| [`crypto/subtle`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/crypto/subtle) | 7 | | Constant-time primitives; word-at-a-time `XORBytes` over the full alignment matrix. · [proof](validation/current/crypto.subtle.md) |
| [`encoding/ascii85`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/encoding/ascii85) | 9 | | Ascii85 encode/decode and streaming wrappers. · [proof](validation/current/encoding.ascii85.md) |
| [`encoding/base32`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/encoding/base32) | 26 | | Base32 round-trips; `io.Pipe` rendezvous over the real channel core. · [proof](validation/current/encoding.base32.md) |
| [`encoding/base64`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/encoding/base64) | 17 | | Base64 round-trips; goroutine + `time.After` timer path. · [proof](validation/current/encoding.base64.md) |
| [`encoding/binary`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/encoding/binary) | 137 | 9 | Reflection-driven Read/Write — the bridge's construction/write-back surface. · [proof](validation/current/encoding.binary.md) |
| [`encoding/csv`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/encoding/csv) | 71 | | CSV parsing; wrapped-error `errors.Is` through the reflection bridge. · [proof](validation/current/encoding.csv.md) |
| [`encoding/hex`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/encoding/hex) | 12 | | Hex encode/decode and error paths. · [proof](validation/current/encoding.hex.md) |
| [`encoding/pem`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/encoding/pem) | 8 | | PEM block parsing and round-trips. · [proof](validation/current/encoding.pem.md) |
| [`errors`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/errors) | 61 | | `errors.Is`/`As`/`Join` — reflection-bridge write-back (`Value.Set`, addressability). · [proof](validation/current/errors.md) |
| [`go/build/constraint`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/go/build/constraint) | 89 | | Build-constraint expression parsing. · [proof](validation/current/go.build.constraint.md) |
| [`go/token`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/go/token) | 31 | | FileSet/Position machinery; a full `encoding/gob` serialization round-trip — the reflect type-relation mirrors driving real Encoder/Decoder engines. · [proof](validation/current/go.token.md) |
| [`go/version`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/go/version) | 3 | | Go version-string comparison. · [proof](validation/current/go.version.md) |
| [`hash/adler32`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/hash/adler32) | 2 | | Adler-32 checksum. · [proof](validation/current/hash.adler32.md) |
| [`hash/crc32`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/hash/crc32) | 10 | | CRC-32 including **real SSE4.2/PCLMULQDQ hardware paths** via managed intrinsics. · [proof](validation/current/hash.crc32.md) |
| [`hash/crc64`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/hash/crc64) | 5 | | CRC-64 checksum tables. · [proof](validation/current/hash.crc64.md) |
| [`hash/fnv`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/hash/fnv) | 19 | | FNV-1/FNV-1a across widths. · [proof](validation/current/hash.fnv.md) |
| [`hash/maphash`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/hash/maphash) | 22 | | Seeded and unseeded hash streams plus SMHasher avalanche/BIC quality checks; the 100,000-sample bounds exercise a computed float constant derived from a named untyped integer constant. · [proof](validation/current/hash.maphash.md) |
| [`image/draw`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/image/draw) | 9 | | Porter-Duff compositing over every image model — clip narrowing through address-taken value parameters, Floyd-Steinberg dithering, and paletted quantization. · [proof](validation/current/image.draw.md) |
| [`image/gif`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/image/gif) | 28 | | GIF encode/decode over the real LZW coder — interlacing, transparency and palette edge cases, animation loop counts and per-frame disposal, and `image.Decode` reading a PNG through a **blank import**'s registration. · [proof](validation/current/image.gif.md) |
| [`image/jpeg`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/image/jpeg) | 14 | | Baseline and progressive JPEG decode/encode — forward and inverse DCT against a reference implementation, zig-zag tables, restart markers, truncated and extraneous scan data, grayscale and CMYK, and a full encode/decode round trip over the shared `image/testdata` fixtures. · [proof](validation/current/image.jpeg.md) |
| [`image/png`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/image/png) | 28 | | The PNG codec end to end — the full PNGSuite decode corpus (every bit depth, palette, interlacing and transparency form) against its `.sng` goldens, Paeth filtering, malformed-stream error paths, and an encode/decode round trip whose RGBA→NRGBA row conversion writes through a **slice-to-array pointer**. · [proof](validation/current/image.png.md) |
| [`index/suffixarray`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/index/suffixarray) | 12 | | SAIS suffix-array construction in both 32- and 64-bit index widths, verified exhaustively over every string up to length 8 on 2- and 3-letter alphabets, plus lookup, regexp `FindAllIndex`, and gob save/restore round trips. · [proof](validation/current/index.suffixarray.md) |
| [`internal/abi`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/internal/abi) | 2 | | Runtime ABI helpers (`FuncPC`). · [proof](validation/current/internal.abi.md) |
| [`internal/coverage/slicereader`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/internal/coverage/slicereader) | 1 | | Coverage slice reader. · [proof](validation/current/internal.coverage.slicereader.md) |
| [`internal/coverage/slicewriter`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/internal/coverage/slicewriter) | 1 | | Coverage slice writer. · [proof](validation/current/internal.coverage.slicewriter.md) |
| [`internal/fmtsort`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/internal/fmtsort) | 3 | | `fmt`'s map-key ordering — `Value.Convert`, arithmetically-ordered pointer/channel tokens, `-tests` init-order relocation. · [proof](validation/current/internal.fmtsort.md) |
| [`internal/gover`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/internal/gover) | 5 | | Toolchain version ordering. · [proof](validation/current/internal.gover.md) |
| [`internal/itoa`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/internal/itoa) | 3 | | Minimal integer formatting. · [proof](validation/current/internal.itoa.md) |
| [`internal/saferio`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/internal/saferio) | 17 | | Allocation-capped I/O helpers. · [proof](validation/current/internal.saferio.md) |
| [`internal/zstd`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/internal/zstd) | 534 | | The Zstandard decompressor — FSE/Huffman table construction, the sliding window, xxhash checksums, and 500+ fuzz-corpus round-trips. · [proof](validation/current/internal.zstd.md) |
| [`io`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/io) | 59 | 2 | The core reader/writer contracts — pipes over real goroutine rendezvous, `MultiReader`/`MultiWriter` flattening via `runtime.Callers`, `OffsetWriter` on real temp files (`os.runtime_rand`), `WriteString` interface dispatch under `-tests` renaming; alloc-count disclosures. · [proof](validation/current/io.md) |
| [`io/fs`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/io/fs) | 18 | | The `fs.FS` interface family — named-interface runtime shells, `fs.Glob` deep recursion, `dirFS` walks. · [proof](validation/current/io.fs.md) |
| [`maps`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/maps) | 14 | | Generic map helpers and iterators. · [proof](validation/current/maps.md) |
| [`math`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/math) | 76 | | The core numeric package — IEEE edge cases, rounding, `Inf`/`NaN`. · [proof](validation/current/math.md) |
| [`math/bits`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/math/bits) | 26 | | Bit-manipulation intrinsics. · [proof](validation/current/math.bits.md) |
| [`math/cmplx`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/math/cmplx) | 24 | | `complex128` transcendental math. · [proof](validation/current/math.cmplx.md) |
| [`math/rand`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/math/rand) | 43 | | PRNG streams, including a child-process race test. · [proof](validation/current/math.rand.md) |
| [`math/rand/v2`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/math/rand/v2) | 36 | | The v2 PRNG API (PCG, ChaCha8). · [proof](validation/current/math.rand.v2.md) |
| [`mime`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/mime) | 17 | 1 | MIME type tables and media-type parsing — the first package through the runtime process-control facade (`LockOSThread`, registry reads). · [proof](validation/current/mime.md) |
| [`net/http/internal/ascii`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/net/http/internal/ascii) | 13 | | ASCII case-insensitive helpers. · [proof](validation/current/net.http.internal.ascii.md) |
| [`os/signal`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/os/signal) | 1 | | Console-signal delivery (Ctrl+Break) through real channels and `select`. · [proof](validation/current/os.signal.md) |
| [`path`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/path) | 9 | | Pure path manipulation (`Clean`/`Split`/`Join`/`Match`…). · [proof](validation/current/path.md) |
| [`path/filepath`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/path/filepath) | 61 | | Path algebra plus the Windows symlink machinery — `EvalSymlinks` through the hand-owned `FindFirstFile` blittable mirror, `Glob`/`Walk`, junction-aware `TempDir` cleanup, `testenv.GOROOT` via the pipeline's exported root, and 20 privilege-gated skips agreeing with Go's. · [proof](validation/current/path.filepath.md) |
| [`regexp`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/regexp) | 45 | | The full RE2 engine — NFA/backtracker/one-pass executors, the RE2 exhaustive corpus, `TextMarshaler` round-trips. · [proof](validation/current/regexp.md) |
| [`regexp/syntax`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/regexp/syntax) | 12 | | Regexp parsing, simplification and program compilation; named-type constant tables. · [proof](validation/current/regexp.syntax.md) |
| [`sort`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/sort) | 63 | | Interface-driven sort, `sort.Slice` reflection swaps, NaN-aware ordering, stability. · [proof](validation/current/sort.md) |
| [`strconv`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/strconv) | 55 | 11 | Number↔string conversion at full precision — Ryū/Grisu float formatting, arbitrary-precision decimal shifts, complex parsing; alloc-profile disclosures. · [proof](validation/current/strconv.md) |
| [`strings`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/strings) | 68 | 4 | String algorithms; alloc-count/alloc-profile disclosures. · [proof](validation/current/strings.md) |
| [`sync`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/sync) | 41 | 10 | The concurrency crown — `Mutex`/`RWMutex`/`WaitGroup`/`Once`/`Cond`/`Map`/`Pool` over real parked-thread semaphores, a hand-owned lock-free pool ring, and GC-integrated cleanup; `Cond`'s copy detector on root-allocation identity; alloc-profile and codegen-liveness disclosures. · [proof](validation/current/sync.md) |
| [`testing/quick`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/testing/quick) | 8 | | Property testing — `reflect` value generation and `Value.Call` dynamic invocation. · [proof](validation/current/testing.quick.md) |
| [`text/scanner`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/text/scanner) | 18 | | Rune-level source scanning. · [proof](validation/current/text.scanner.md) |
| [`text/tabwriter`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/text/tabwriter) | 3 | | Elastic-tab column formatting; panic-during-write recovery. · [proof](validation/current/text.tabwriter.md) |
| [`unicode`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/unicode) | 28 | | Category tables, case mapping (`SpecialCase`), script ranges. · [proof](validation/current/unicode.md) |
| [`unicode/utf16`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/unicode/utf16) | 8 | 1 | Encode/decode round-trips via `reflect.DeepEqual`. · [proof](validation/current/unicode.utf16.md) |
| [`unicode/utf8`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/unicode/utf8) | 14 | | UTF-8 encode/decode — the first suite to pass (2026-07-17). · [proof](validation/current/unicode.utf8.md) |

[^codegen-liveness]: A by-value struct argument wider than a machine word is passed by hidden
    reference, so the caller's temp is address-exposed and therefore untracked by liveness analysis.
