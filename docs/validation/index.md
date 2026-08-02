# Validation proofs

One page per validated package — the full per-test differential behind its row in
[Validated Test Packages](../ValidatedTestPackages.md): every eligible `Test` function, `go test`'s
verdict and go2cs's, side by side. The converter writes these pages itself, from the same
comparison record that decides whether a package validates at all.

Pages under `current/` are living proof — regenerated only when a package's verdicts change.
Versioned sibling directories are frozen publication snapshots: written once at release and never
rewritten, so the proof link for a published package stays the proof as of that binary.

| Package | Proof | Converted package |
|:--|:--|:--|
| `bufio` | [`bufio.md`](current/bufio.md) | [`src/core/bufio`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/bufio) |
| `bytes` | [`bytes.md`](current/bytes.md) | [`src/core/bytes`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/bytes) |
| `cmp` | [`cmp.md`](current/cmp.md) | [`src/core/cmp`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/cmp) |
| `compress/bzip2` | [`compress.bzip2.md`](current/compress.bzip2.md) | [`src/core/compress/bzip2`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/compress/bzip2) |
| `compress/flate` | [`compress.flate.md`](current/compress.flate.md) | [`src/core/compress/flate`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/compress/flate) |
| `compress/gzip` | [`compress.gzip.md`](current/compress.gzip.md) | [`src/core/compress/gzip`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/compress/gzip) |
| `compress/lzw` | [`compress.lzw.md`](current/compress.lzw.md) | [`src/core/compress/lzw`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/compress/lzw) |
| `compress/zlib` | [`compress.zlib.md`](current/compress.zlib.md) | [`src/core/compress/zlib`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/compress/zlib) |
| `container/heap` | [`container.heap.md`](current/container.heap.md) | [`src/core/container/heap`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/container/heap) |
| `container/list` | [`container.list.md`](current/container.list.md) | [`src/core/container/list`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/container/list) |
| `container/ring` | [`container.ring.md`](current/container.ring.md) | [`src/core/container/ring`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/container/ring) |
| `context` | [`context.md`](current/context.md) | [`src/core/context`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/context) |
| `crypto/hmac` | [`crypto.hmac.md`](current/crypto.hmac.md) | [`src/core/crypto/hmac`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/crypto/hmac) |
| `crypto/md5` | [`crypto.md5.md`](current/crypto.md5.md) | [`src/core/crypto/md5`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/crypto/md5) |
| `crypto/sha1` | [`crypto.sha1.md`](current/crypto.sha1.md) | [`src/core/crypto/sha1`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/crypto/sha1) |
| `crypto/sha256` | [`crypto.sha256.md`](current/crypto.sha256.md) | [`src/core/crypto/sha256`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/crypto/sha256) |
| `crypto/sha512` | [`crypto.sha512.md`](current/crypto.sha512.md) | [`src/core/crypto/sha512`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/crypto/sha512) |
| `crypto/subtle` | [`crypto.subtle.md`](current/crypto.subtle.md) | [`src/core/crypto/subtle`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/crypto/subtle) |
| `encoding/ascii85` | [`encoding.ascii85.md`](current/encoding.ascii85.md) | [`src/core/encoding/ascii85`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/encoding/ascii85) |
| `encoding/base32` | [`encoding.base32.md`](current/encoding.base32.md) | [`src/core/encoding/base32`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/encoding/base32) |
| `encoding/base64` | [`encoding.base64.md`](current/encoding.base64.md) | [`src/core/encoding/base64`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/encoding/base64) |
| `encoding/binary` | [`encoding.binary.md`](current/encoding.binary.md) | [`src/core/encoding/binary`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/encoding/binary) |
| `encoding/csv` | [`encoding.csv.md`](current/encoding.csv.md) | [`src/core/encoding/csv`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/encoding/csv) |
| `encoding/hex` | [`encoding.hex.md`](current/encoding.hex.md) | [`src/core/encoding/hex`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/encoding/hex) |
| `encoding/pem` | [`encoding.pem.md`](current/encoding.pem.md) | [`src/core/encoding/pem`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/encoding/pem) |
| `errors` | [`errors.md`](current/errors.md) | [`src/core/errors`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/errors) |
| `go/build/constraint` | [`go.build.constraint.md`](current/go.build.constraint.md) | [`src/core/go/build/constraint`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/go/build/constraint) |
| `go/token` | [`go.token.md`](current/go.token.md) | [`src/core/go/token`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/go/token) |
| `go/version` | [`go.version.md`](current/go.version.md) | [`src/core/go/version`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/go/version) |
| `hash/adler32` | [`hash.adler32.md`](current/hash.adler32.md) | [`src/core/hash/adler32`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/hash/adler32) |
| `hash/crc32` | [`hash.crc32.md`](current/hash.crc32.md) | [`src/core/hash/crc32`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/hash/crc32) |
| `hash/crc64` | [`hash.crc64.md`](current/hash.crc64.md) | [`src/core/hash/crc64`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/hash/crc64) |
| `hash/fnv` | [`hash.fnv.md`](current/hash.fnv.md) | [`src/core/hash/fnv`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/hash/fnv) |
| `hash/maphash` | [`hash.maphash.md`](current/hash.maphash.md) | [`src/core/hash/maphash`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/hash/maphash) |
| `image/draw` | [`image.draw.md`](current/image.draw.md) | [`src/core/image/draw`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/image/draw) |
| `image/gif` | [`image.gif.md`](current/image.gif.md) | [`src/core/image/gif`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/image/gif) |
| `image/jpeg` | [`image.jpeg.md`](current/image.jpeg.md) | [`src/core/image/jpeg`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/image/jpeg) |
| `image/png` | [`image.png.md`](current/image.png.md) | [`src/core/image/png`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/image/png) |
| `index/suffixarray` | [`index.suffixarray.md`](current/index.suffixarray.md) | [`src/core/index/suffixarray`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/index/suffixarray) |
| `internal/abi` | [`internal.abi.md`](current/internal.abi.md) | [`src/core/internal/abi`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/internal/abi) |
| `internal/coverage/slicereader` | [`internal.coverage.slicereader.md`](current/internal.coverage.slicereader.md) | [`src/core/internal/coverage/slicereader`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/internal/coverage/slicereader) |
| `internal/coverage/slicewriter` | [`internal.coverage.slicewriter.md`](current/internal.coverage.slicewriter.md) | [`src/core/internal/coverage/slicewriter`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/internal/coverage/slicewriter) |
| `internal/fmtsort` | [`internal.fmtsort.md`](current/internal.fmtsort.md) | [`src/core/internal/fmtsort`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/internal/fmtsort) |
| `internal/gover` | [`internal.gover.md`](current/internal.gover.md) | [`src/core/internal/gover`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/internal/gover) |
| `internal/itoa` | [`internal.itoa.md`](current/internal.itoa.md) | [`src/core/internal/itoa`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/internal/itoa) |
| `internal/saferio` | [`internal.saferio.md`](current/internal.saferio.md) | [`src/core/internal/saferio`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/internal/saferio) |
| `internal/zstd` | [`internal.zstd.md`](current/internal.zstd.md) | [`src/core/internal/zstd`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/internal/zstd) |
| `io` | [`io.md`](current/io.md) | [`src/core/io`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/io) |
| `io/fs` | [`io.fs.md`](current/io.fs.md) | [`src/core/io/fs`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/io/fs) |
| `maps` | [`maps.md`](current/maps.md) | [`src/core/maps`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/maps) |
| `math` | [`math.md`](current/math.md) | [`src/core/math`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/math) |
| `math/bits` | [`math.bits.md`](current/math.bits.md) | [`src/core/math/bits`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/math/bits) |
| `math/cmplx` | [`math.cmplx.md`](current/math.cmplx.md) | [`src/core/math/cmplx`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/math/cmplx) |
| `math/rand` | [`math.rand.md`](current/math.rand.md) | [`src/core/math/rand`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/math/rand) |
| `math/rand/v2` | [`math.rand.v2.md`](current/math.rand.v2.md) | [`src/core/math/rand/v2`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/math/rand/v2) |
| `mime` | [`mime.md`](current/mime.md) | [`src/core/mime`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/mime) |
| `net/http/internal/ascii` | [`net.http.internal.ascii.md`](current/net.http.internal.ascii.md) | [`src/core/net/http/internal/ascii`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/net/http/internal/ascii) |
| `os/signal` | [`os.signal.md`](current/os.signal.md) | [`src/core/os/signal`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/os/signal) |
| `path` | [`path.md`](current/path.md) | [`src/core/path`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/path) |
| `path/filepath` | [`path.filepath.md`](current/path.filepath.md) | [`src/core/path/filepath`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/path/filepath) |
| `regexp` | [`regexp.md`](current/regexp.md) | [`src/core/regexp`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/regexp) |
| `regexp/syntax` | [`regexp.syntax.md`](current/regexp.syntax.md) | [`src/core/regexp/syntax`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/regexp/syntax) |
| `sort` | [`sort.md`](current/sort.md) | [`src/core/sort`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/sort) |
| `strconv` | [`strconv.md`](current/strconv.md) | [`src/core/strconv`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/strconv) |
| `strings` | [`strings.md`](current/strings.md) | [`src/core/strings`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/strings) |
| `sync` | [`sync.md`](current/sync.md) | [`src/core/sync`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/sync) |
| `testing/quick` | [`testing.quick.md`](current/testing.quick.md) | [`src/core/testing/quick`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/testing/quick) |
| `text/scanner` | [`text.scanner.md`](current/text.scanner.md) | [`src/core/text/scanner`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/text/scanner) |
| `text/tabwriter` | [`text.tabwriter.md`](current/text.tabwriter.md) | [`src/core/text/tabwriter`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/text/tabwriter) |
| `unicode` | [`unicode.md`](current/unicode.md) | [`src/core/unicode`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/unicode) |
| `unicode/utf16` | [`unicode.utf16.md`](current/unicode.utf16.md) | [`src/core/unicode/utf16`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/unicode/utf16) |
| `unicode/utf8` | [`unicode.utf8.md`](current/unicode.utf8.md) | [`src/core/unicode/utf8`](https://github.com/ritchiecarroll/go2cs/tree/master/src/core/unicode/utf8) |
