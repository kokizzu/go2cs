# DATA — per-row sweep wall times (the hop shard map's input)

> Point-in-time measurement DATA, labeled by (OS, corpus SHA, machine). This is the number the
> hop-era shard map (`docs/PLAN-hop-campaign.md`, H5) is parameterized by: the SWEEP's per-row
> wall clock (convert + build + both test hosts + compare), NOT the Go-side `go test -json`
> Time fields, which invert exactly the rows that dominate a shard. Native `[NNNs]` per-row
> timing exists in `run-validated-sweep.ps1` since `4e91a03e2`; these first tables predate it
> and were derived as described per section. Add a section per new (OS, SHA, machine)
> measurement at each hop recon; do not overwrite old sections — supersede them.

## windows · corpus `18770d083` · i9-13900K (ritchie-desk2) · 2026-08-23 (JOB-007)

162 rows, all PASS, 18,569 verdicts, aggregate 7,697 s. Derivation: artifact-mtime deltas in
roster order (run-start from the log file's creation time); self-check: the 162 deltas sum to
7,701 s vs the sweep's own 7,697 s. The two Go-vs-C# inversions to respect when packing shards:
`crypto/dsa` 1,317 s for 4 verdicts (the largest row) and `hash/maphash` 898 s for 22.

```
archive/tar                              97           60s
archive/zip                              100         354s
bufio                                    80            9s
bytes                                    82           18s
cmp                                      4             7s
compress/bzip2                           4             9s
compress/flate                           64          106s
compress/gzip                            15           21s
compress/lzw                             17            9s
compress/zlib                            6            18s
container/heap                           7             7s
container/list                           10            8s
container/ring                           8             7s
context                                  57           13s
crypto                                   6            15s
crypto/aes                               13            9s
crypto/des                               18            8s
crypto/dsa                               4          1317s
crypto/ecdh                              47           15s
crypto/ecdsa                             82           32s
crypto/ed25519                           8            15s
crypto/elliptic                          82           16s
crypto/hmac                              172           9s
crypto/internal/alias                    1             7s
crypto/internal/bigmod                   14           12s
crypto/internal/boring                   3             8s
crypto/internal/edwards25519/field       16           66s
crypto/internal/hpke                     19           10s
crypto/internal/mlkem768                 12          228s
crypto/md5                               11            8s
crypto/rand                              298          13s
crypto/rc4                               2             9s
crypto/rsa                               559         119s
crypto/sha1                              12            8s
crypto/sha256                            23            9s
crypto/sha512                            36            9s
crypto/subtle                            7            16s
crypto/tls                               400         659s
database/sql                             137          47s
database/sql/driver                      1             9s
debug/buildinfo                          197          18s
debug/dwarf                              40           11s
debug/elf                                31           12s
debug/gosym                              10           11s
debug/macho                              7            10s
debug/plan9obj                           2             9s
encoding/ascii85                         9             7s
encoding/asn1                            38           10s
encoding/base32                          26            8s
encoding/base64                          17            8s
encoding/binary                          137           9s
encoding/csv                             71           10s
encoding/hex                             12            9s
encoding/json                            491          28s
encoding/xml                             386          54s
encoding/pem                             8           105s
errors                                   61           10s
expvar                                   11           13s
flag                                     24           12s
fmt                                      63           10s
go/ast                                   9            11s
go/build/constraint                      89            9s
go/constant                              9             9s
go/doc/comment                           10059         18s
go/format                                4            10s
go/importer                              3            20s
go/internal/gccgoimporter                4            11s
go/internal/gcimporter                   583         306s
go/internal/srcimporter                  7            19s
go/parser                                173         259s
go/printer                               45           11s
go/scanner                               11           10s
go/token                                 31            9s
go/types                                 557         137s
go/version                               3             8s
hash                                     18           10s
hash/adler32                             2             7s
hash/crc32                               10            9s
hash/crc64                               5             8s
hash/fnv                                 19            8s
hash/maphash                             22          898s
html/template                            243          28s
image                                    8            12s
image/color                              10            8s
image/draw                               9            10s
image/gif                                28           14s
image/jpeg                               14           11s
image/png                                28           14s
index/suffixarray                        12          573s
internal/abi                             2            10s
internal/buildcfg                        3             8s
internal/coverage/cformat                2             9s
internal/coverage/cmerge                 2             9s
internal/coverage/pods                   1             9s
internal/coverage/slicereader            1             9s
internal/coverage/slicewriter            1             8s
internal/cpu                             8             9s
internal/dag                             6             9s
internal/diff                            13            9s
internal/fmtsort                         3             8s
internal/fuzz                            52           10s
internal/godebugs                        1           177s
internal/gover                           5             7s
internal/itoa                            3             8s
internal/profile                         1            10s
internal/reflectlite                     30           10s
internal/saferio                         17            7s
internal/singleflight                    5             9s
internal/sysinfo                         1             8s
internal/testenv                         7             9s
internal/types/errors                    155          14s
internal/xcoff                           3             9s
internal/zstd                            536          10s
io                                       60            9s
io/fs                                    18           10s
io/ioutil                                28            9s
log                                      8             9s
log/slog/internal/benchmarks             3             9s
maps                                     14            8s
math                                     76            9s
math/bits                                26            7s
math/cmplx                               24            8s
math/rand                                43           31s
math/rand/v2                             36           34s
mime                                     17           10s
mime/multipart                           52           71s
mime/quotedprintable                     5            10s
net/http/fcgi                            12           14s
net/http/internal/ascii                  13            8s
net/mail                                 11           10s
net/rpc/jsonrpc                          9            14s
net/textproto                            26           10s
net/url                                  48           10s
os/exec                                  74           43s
os/exec/internal/fdtest                  1             8s
os/signal                                1            12s
path                                     9             8s
path/filepath                            61           13s
plugin                                   1             8s
regexp                                   45          226s
regexp/syntax                            12           20s
runtime/debug                            4            11s
runtime/internal/math                    1             8s
runtime/internal/sys                     4             7s
runtime/metrics                          2            10s
sort                                     63           16s
strconv                                  55           16s
strings                                  68           56s
sync                                     44           18s
sync/atomic                              108           82s
syscall                                  62           14s
testing/iotest                           18            9s
testing/quick                            8             9s
testing/slogtest                         17           10s
text/scanner                             18            9s
text/tabwriter                           3             8s
text/template                            52           25s
text/template/parse                      52           10s
time                                     159         197s
unicode                                  28            8s
unicode/utf16                            8             9s
unicode/utf8                             14            6s
```

## linux · corpus `18770d083` · WSL on RITCHIE-LAPTOP (R) · pending

R's leg records wall-seconds natively in its ledger; its table lands here when the leg posts.
