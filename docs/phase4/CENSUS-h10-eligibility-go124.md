# CENSUS — H10 eligibility at go1.24.13: the implementable denominator

**Dated record, 2026-09-08. C2, on COORD's `b1fd949c2a`.** Docs-only. The owner's roster question
left the 1.24 denominator at "~226"; this record replaces the tilde with an arithmetic.

Measured on a linux-x64 container against the **pinned** `go1.24.13` and `go1.23.12` toolchains
(three-arm preflight passed on both: `GOROOT`-unset root path, `VERSION`, and the binary). Every
figure below is `GOOS=windows GOARCH=amd64 CGO_ENABLED=0` unless stated. ⚠ One class — **E2,
broken oracle** — is **NOT DECIDABLE on this host** and is carried as a hole rather than a zero;
see §5.

---

## 1. ⚠ THE FINDING: "234" AND "215" ARE DIFFERENT AXES, AND THE ANCHOR'S AXIS IS NEITHER OBVIOUS NOR THE ONE THE DISPATCH NAMES

The dispatch describes its population as *"raw `_test.go` presence"* and gives **234**. Those are
two different things, and the record has to carry the number with its true axis or every later
stage inherits a denominator inflated by construction. Three axes exist; all three were measured:

| axis | definition | go1.23.12 | go1.24.13 |
|:--|:--|--:|--:|
| **A** | raw `_test.go` on disk, **constraint-blind** (`cmd/` and `vendor/` excluded) | 227 | **245** |
| **B** | test files **surviving** the windows/amd64 constraints (`TestGoFiles`+`XTestGoFiles`) | 217 | **234** |
| **C** | of those, packages **declaring at least one `func Test*`** | **215** | **229** |

**Axis C at go1.23.12 is 215 — the roster's anchor denominator, reproduced exactly.** That is the
calibration that settles which axis the anchor was built on, and it was derived here independently
rather than assumed from the roster's prose.

So: **the dispatch's 234 is axis B**, not the raw axis it is described as (raw is 245), and **the
anchor-comparable 1.24 population is 229, not 234.** Setting 234 beside 204/209 would compare
different measurements.

**The 11 packages in A but not B** — present on disk, excluded by Go's own constraints on this
target: `arena`, `crypto/boring`, `crypto/tls/fipsonly`, `internal/runtime/syscall`, `internal/syscall/unix`, `log/syslog`, `net/internal/socktest`, `runtime/cgo`, `runtime/race`, `syscall/js`, `testing/synctest`.

**The 5 packages in B but not C** — test files survive, but every function in them is an
`Example` or `Benchmark`: `crypto/internal/fips140/aes/gcm`, `crypto/internal/fips140/drbg`, `crypto/internal/fips140/nistec`, `crypto/internal/fips140/nistec/fiat`, `embed`.

---

## 2. The 1.23.12 exclusions, re-applied by name

⚠ **Four of the six are ALREADY OUTSIDE the population and must not be subtracted again.** They
are E1 *because* Go's constraints leave no eligible test on this target — which is exactly what
axis C already encodes. Subtracting them from 229 would double-count them.

| 1.23.12 exclusion | class | at 1.24.13 | disposition |
|:--|:--|:--|:--|
| `internal/syscall/unix` | E1 | `A··` | **already outside axis C** (axes `A··`); reason still holds and needs no subtraction |
| `net/internal/socktest` | E1 | `A··` | **already outside axis C** (axes `A··`); reason still holds and needs no subtraction |
| `log/syslog` | E1 | `A··` | **already outside axis C** (axes `A··`); reason still holds and needs no subtraction |
| `runtime/race` | E1 | `A··` | **already outside axis C** (axes `A··`); reason still holds and needs no subtraction |
| `internal/unsafeheader` | E3 | `ABC` | **IN the population — subtract explicitly.** Reason re-verified: The suite's subject IS the raw {Data,Len,Cap} header the managed runtime replaces. |
| `runtime/trace` | E4 | `ABC` | **IN the population — subtract explicitly.** Reason re-verified: The execution tracer; runtime.StartTrace() is hand-owned and refuses. Comparison sound, validates nothing. |

**So exactly TWO of the six are live subtractions at 1.24: `internal/unsafeheader` (E3) and
`runtime/trace` (E4).** Both retain their 1.23 mechanism unchanged — the header struct is still
the replaced representation, and the tracer still refuses from its hand-own.

⚠ **The dispatch names three classes (E1/E2/E3); the roster carries FOUR.** `E4 — the comparison
is sound and validates nothing` was minted by owner ruling on 2026-09-07 and `runtime/trace` sits
in it. **E2 has zero members today.** This record classifies against all four.

---

## 3. The relocated banked rows — 10 rows, 2,321 verdicts

Each 1.24 row is listed under **its own key**, with *receives verdicts from* as an annotation.
**No count is carried**: a relocated row re-banks from zero at 1.24, and the verdict figure below
records only what the 1.23.12 anchor held.

| 1.23.12 row | banked | 1.24 package identity | Test funcs live in | note |
|:--|--:|:--|:--|:--|
| `crypto/internal/nistec` | 2195 | `crypto/internal/fips140/nistec` | `crypto/internal/fips140test` | package moved; its Test funcs live in fips140test/nistec_test.go + nistec_ordinv_test.go, so the 1.24 nistec row is axis-B-only (no Test declared) |
| `internal/concurrent` | 20 | `internal/sync` | `internal/sync` | HashTrieMap relocated; hashtriemap.go + export_test.go now under internal/sync |
| `crypto/internal/edwards25519` | 54 | `crypto/internal/fips140/edwards25519` | `crypto/internal/fips140/edwards25519` | straight relocation under fips140/ |
| `crypto/internal/edwards25519/field` | 16 | `crypto/internal/fips140/edwards25519/field` | `crypto/internal/fips140/edwards25519/field` | straight relocation under fips140/ |
| `crypto/internal/bigmod` | 14 | `crypto/internal/fips140/bigmod` | `crypto/internal/fips140/bigmod` | straight relocation under fips140/ |
| `crypto/internal/mlkem768` | 12 | `crypto/internal/fips140/mlkem + crypto/mlkem` | `crypto/internal/fips140/mlkem (10 Test funcs) and crypto/mlkem (4)` | FANS OUT to two 1.24 rows |
| `internal/weak` | 4 | `weak` | `weak` | promoted out of internal/ |
| `runtime/internal/sys` | 4 | `internal/runtime/sys` | `internal/runtime/sys` | runtime/internal/* -> internal/runtime/* |
| `crypto/internal/alias` | 1 | `crypto/internal/fips140/alias` | `crypto/internal/fips140test` | package moved and carries NO test file; its Test lives in fips140test/alias_test.go |
| `runtime/internal/math` | 1 | `internal/runtime/math` | `internal/runtime/math` | runtime/internal/* -> internal/runtime/* |

**Total: 2,321 verdicts** — derived here by summing the ten rows out of the 1.23.12 roster, and equal
to the figure the dispatch states, by an independent derivation.

⚠ **`crypto/internal/fips140test` is the aggregate that receives three of these** (alias, nistec,
edwards25519's check surface): **13 `.go` test files, 26 `Test` functions**. It is a single 1.24
row and it is where verdicts from several 1.23 rows land, so a naive per-row mapping would either
lose them or count them three times.

---

## 4. The new test-bearing packages

**24 on the anchor axis (C), not 27** — the dispatch's 27 is an axis-B or raw-axis count, the same
distinction as §1. Listed with the classification each is admitted under:

| # | 1.24 package | admitted |
|--:|:--|:--|
| 1 | `crypto/hkdf` | eligible — ordinary pure-Go package, tests declared and surviving on windows/amd64 |
| 2 | `crypto/internal/fips140/aes` | eligible — ordinary pure-Go package, tests declared and surviving on windows/amd64 |
| 3 | `crypto/internal/fips140/bigmod` | eligible — ordinary pure-Go package, tests declared and surviving on windows/amd64 |
| 4 | `crypto/internal/fips140/ecdh` | eligible — ordinary pure-Go package, tests declared and surviving on windows/amd64 |
| 5 | `crypto/internal/fips140/ecdsa` | eligible — ordinary pure-Go package, tests declared and surviving on windows/amd64 |
| 6 | `crypto/internal/fips140/edwards25519` | eligible — ordinary pure-Go package, tests declared and surviving on windows/amd64 |
| 7 | `crypto/internal/fips140/edwards25519/field` | eligible — ordinary pure-Go package, tests declared and surviving on windows/amd64 |
| 8 | `crypto/internal/fips140/mlkem` | eligible — ordinary pure-Go package, tests declared and surviving on windows/amd64 |
| 9 | `crypto/internal/fips140/rsa` | eligible — ordinary pure-Go package, tests declared and surviving on windows/amd64 |
| 10 | `crypto/internal/fips140deps` | eligible — ordinary pure-Go package, tests declared and surviving on windows/amd64 |
| 11 | `crypto/internal/fips140test` | eligible — ordinary pure-Go package, tests declared and surviving on windows/amd64 |
| 12 | `crypto/internal/sysrand` | eligible — ordinary pure-Go package, tests declared and surviving on windows/amd64 |
| 13 | `crypto/mlkem` | eligible — ordinary pure-Go package, tests declared and surviving on windows/amd64 |
| 14 | `crypto/pbkdf2` | eligible — ordinary pure-Go package, tests declared and surviving on windows/amd64 |
| 15 | `crypto/sha3` | eligible — ordinary pure-Go package, tests declared and surviving on windows/amd64 |
| 16 | `go/ast/internal/tests` | eligible — ordinary pure-Go package, tests declared and surviving on windows/amd64 |
| 17 | `internal/copyright` | eligible — ordinary pure-Go package, tests declared and surviving on windows/amd64 |
| 18 | `internal/pkgbits` | eligible — ordinary pure-Go package, tests declared and surviving on windows/amd64 |
| 19 | `internal/runtime/maps` | eligible — ordinary pure-Go package, tests declared and surviving on windows/amd64 |
| 20 | `internal/runtime/math` | eligible — ordinary pure-Go package, tests declared and surviving on windows/amd64 |
| 21 | `internal/runtime/sys` | eligible — ordinary pure-Go package, tests declared and surviving on windows/amd64 |
| 22 | `internal/sync` | eligible — ordinary pure-Go package, tests declared and surviving on windows/amd64 |
| 23 | `internal/synctest` | eligible — ordinary pure-Go package, tests declared and surviving on windows/amd64 |
| 24 | `weak` | eligible — ordinary pure-Go package, tests declared and surviving on windows/amd64 |

**All 24 are eligible on the static evidence.** None is Unix-only, none needs `-race`, none has the
replaced representation as its subject. ⚠ That is a *static* verdict: it says the tests exist and
build on this target, not that their oracle is clean — see §5.

**The 10 departures** are §3's relocations, each with a 1.24 identity; none is a retirement.

---

## 5. ⚠ THE E2 HOLE — CLOSED 2026-09-08 BY A WINDOWS SWEEP: ZERO MEMBERS, DENOMINATOR UNMOVED

> **AMENDMENT, 2026-09-08.** i9 ran the sweep this section says is owed: **all 227 packages,
> go1.24.13 windows/amd64, `CGO_ENABLED=0`, `GOTOOLCHAIN=local`, `go test -count=1 -timeout 30m
> -json`**, with the pin re-printed *inside the process that ran the tests* rather than only in the
> launching shell, and all 227 roster names resolved against `go list std` (346 at this release, 0
> missing) **before** the sweep — because a package that does not exist ERRORS, which is not the same
> as an oracle that fails.
>
> **Result: 225 pass, 2 fail, and NEITHER failure is E2.** `os` fails 161 leaves, every one a symlink
> test, on `root_test.go:78` — *"A required privilege is not held by the client."* `net` fails on the
> same host axis. **Both are properties of that HOST, not of Go's suite**, and both name sets
> reproduced identically on a second run.
>
> ⚠ **The i7 read the same package the other way**, holding `SeCreateSymbolicLinkPrivilege`: `os`
> passed with ZERO failures there. Same package, two hosts, opposite privilege, neither a broken
> oracle — which is precisely why this sweep is empty. The roster's own E2 note ("an E2 exclusion is
> only as durable as the HOST that measured it") is what the two readings together vindicate.
>
> **So the denominator stays 227 and nothing subtracts.** §6's `-?` term resolves to `-0`.

The section below is the original statement of the hole, kept as written because it is what the
sweep was run against.

### 5.0 The hole as originally stated


**E2 — broken oracle — is not decidable on this host, and no E2 verdict in this record should be
read as measured.** Establishing E2 means Go's *own* suite failing on the reference side, which
requires running `go test` on **windows/amd64**; this census ran on a linux container. The roster's
own E2 note already binds an E2 exclusion to the host that measured it and requires a fleet-wide
re-probe before machinery is built on one.

So the denominator below is stated **with the E2 hole beside it**, not folded in. Any package a
Windows host later finds E2 moves the denominator down; nothing here can move it up.

---

## 6. THE DENOMINATOR, with its arithmetic

```
  229    axis C at go1.24.13   (packages declaring func Test*, windows/amd64, CGO_ENABLED=0)
   -1    internal/unsafeheader   E3, reason re-verified
   -1    runtime/trace           E4, reason re-verified
   -0    E1                      the four 1.23 E1 rows are already outside axis C (§2)
   -0    E2                      SWEPT 2026-09-08 on windows/amd64: ZERO members (§5)
  ----
  227    implementable denominator at go1.24.13 -- CONFIRMED, no longer pending
```

**The denominator is final at 227** unless a later ruling adds a class member. When this record was
first written the E2 term was an open `-?` that could only ever move 227 down; the sweep resolved it
to `-0`, so the figure is unchanged and is now measured rather than pending.

Beside the anchor: **204 / 209 at go1.24.13's predecessor**; the 1.24 implementable set is **227**
before any E2 subtraction. The population grew 215 → 229 on a like-for-like axis (+14).

---

## 7. Self-check, and its negative control

The checks this record must pass, each stated so it can be re-run:

1. **axis C at 1.23.12 == 215** — the anchor's own denominator. *Measured: 215.* This is the check
   that makes every other number here comparable to the anchor; if it fails, the axis model is
   wrong and nothing below it stands.
2. **A ⊇ B ⊇ C** at both releases. *Measured: 227≥217≥215 and 245≥234≥229.*
3. **|C24 − C23| accounting closes**: 215 + 24 added − 10 gone == 229. *Measured: 229.*
4. **Every exclusion carries a class and a reason**, and every 1.23 exclusion appears exactly once
   in §2 with its 1.24 axis membership stated.
5. **The relocation total is derived, not copied**: summing the ten rows from the roster gives
   2,321, matching the dispatch's figure by an independent path.
6. **No 1.23-only key survives into the skeleton** — the ten departures appear only as
   *receives verdicts from* annotations, never as rows.

**The negative control, and it is the one that matters:** check 1 is the load-bearing arm, so it
was run against the WRONG axis deliberately — axis B at 1.23.12 gives **217**, not 215, and axis A
gives **227**. Only axis C reproduces the anchor. A check that cannot distinguish the three axes
would have accepted any of them, which is precisely how "234" entered the dispatch as a
population.

---

## 8. What this record does NOT establish

- **E2 for any package** (§5) — a Windows host owes the sweep.
- **That any eligible package will validate.** Eligibility is the denominator; the numerator is
  H10's to bank.
- **Per-test disposition.** The unit here is the package, exactly as the roster's metric is
  package-based and all-or-nothing.
- **The 1.24 identity of any row's verdict COUNT.** §3 records what the anchor held; a relocated
  row re-banks from zero.

---

## Appendix — THE go1.24.13 ROSTER SKELETON

**227 rows, keyed by 1.24 identity, every count blank.** H10 banks INTO this rather than deriving
it under time pressure. `receives` names a 1.23.12 row whose verdicts this 1.24 row inherits the
SUBJECT of — never its count, which re-banks from zero.

| # | 1.24 package | verdicts | disclosed | receives |
|--:|:--|--:|--:|:--|
| 1 | `archive/tar` | | |  |
| 2 | `archive/zip` | | |  |
| 3 | `bufio` | | |  |
| 4 | `bytes` | | |  |
| 5 | `cmp` | | |  |
| 6 | `compress/bzip2` | | |  |
| 7 | `compress/flate` | | |  |
| 8 | `compress/gzip` | | |  |
| 9 | `compress/lzw` | | |  |
| 10 | `compress/zlib` | | |  |
| 11 | `container/heap` | | |  |
| 12 | `container/list` | | |  |
| 13 | `container/ring` | | |  |
| 14 | `context` | | |  |
| 15 | `crypto` | | |  |
| 16 | `crypto/aes` | | |  |
| 17 | `crypto/cipher` | | |  |
| 18 | `crypto/des` | | |  |
| 19 | `crypto/dsa` | | |  |
| 20 | `crypto/ecdh` | | |  |
| 21 | `crypto/ecdsa` | | |  |
| 22 | `crypto/ed25519` | | |  |
| 23 | `crypto/elliptic` | | |  |
| 24 | `crypto/hkdf` | | |  |
| 25 | `crypto/hmac` | | |  |
| 26 | `crypto/internal/boring` | | |  |
| 27 | `crypto/internal/boring/bcache` | | |  |
| 28 | `crypto/internal/fips140/aes` | | |  |
| 29 | `crypto/internal/fips140/bigmod` | | | crypto/internal/bigmod (14) |
| 30 | `crypto/internal/fips140/ecdh` | | |  |
| 31 | `crypto/internal/fips140/ecdsa` | | |  |
| 32 | `crypto/internal/fips140/edwards25519` | | | crypto/internal/edwards25519 (54) |
| 33 | `crypto/internal/fips140/edwards25519/field` | | | crypto/internal/edwards25519/field (16) |
| 34 | `crypto/internal/fips140/mlkem` | | | crypto/internal/mlkem768 (12, fanned) |
| 35 | `crypto/internal/fips140/rsa` | | |  |
| 36 | `crypto/internal/fips140deps` | | |  |
| 37 | `crypto/internal/fips140test` | | | crypto/internal/alias (1), crypto/internal/nistec (2,195), edwards25519 check surface |
| 38 | `crypto/internal/hpke` | | |  |
| 39 | `crypto/internal/sysrand` | | |  |
| 40 | `crypto/md5` | | |  |
| 41 | `crypto/mlkem` | | | crypto/internal/mlkem768 (12, fanned) |
| 42 | `crypto/pbkdf2` | | |  |
| 43 | `crypto/rand` | | |  |
| 44 | `crypto/rc4` | | |  |
| 45 | `crypto/rsa` | | |  |
| 46 | `crypto/sha1` | | |  |
| 47 | `crypto/sha256` | | |  |
| 48 | `crypto/sha3` | | |  |
| 49 | `crypto/sha512` | | |  |
| 50 | `crypto/subtle` | | |  |
| 51 | `crypto/tls` | | |  |
| 52 | `crypto/x509` | | |  |
| 53 | `database/sql` | | |  |
| 54 | `database/sql/driver` | | |  |
| 55 | `debug/buildinfo` | | |  |
| 56 | `debug/dwarf` | | |  |
| 57 | `debug/elf` | | |  |
| 58 | `debug/gosym` | | |  |
| 59 | `debug/macho` | | |  |
| 60 | `debug/pe` | | |  |
| 61 | `debug/plan9obj` | | |  |
| 62 | `embed/internal/embedtest` | | |  |
| 63 | `encoding/ascii85` | | |  |
| 64 | `encoding/asn1` | | |  |
| 65 | `encoding/base32` | | |  |
| 66 | `encoding/base64` | | |  |
| 67 | `encoding/binary` | | |  |
| 68 | `encoding/csv` | | |  |
| 69 | `encoding/gob` | | |  |
| 70 | `encoding/hex` | | |  |
| 71 | `encoding/json` | | |  |
| 72 | `encoding/pem` | | |  |
| 73 | `encoding/xml` | | |  |
| 74 | `errors` | | |  |
| 75 | `expvar` | | |  |
| 76 | `flag` | | |  |
| 77 | `fmt` | | |  |
| 78 | `go/ast` | | |  |
| 79 | `go/ast/internal/tests` | | |  |
| 80 | `go/build` | | |  |
| 81 | `go/build/constraint` | | |  |
| 82 | `go/constant` | | |  |
| 83 | `go/doc` | | |  |
| 84 | `go/doc/comment` | | |  |
| 85 | `go/format` | | |  |
| 86 | `go/importer` | | |  |
| 87 | `go/internal/gccgoimporter` | | |  |
| 88 | `go/internal/gcimporter` | | |  |
| 89 | `go/internal/srcimporter` | | |  |
| 90 | `go/parser` | | |  |
| 91 | `go/printer` | | |  |
| 92 | `go/scanner` | | |  |
| 93 | `go/token` | | |  |
| 94 | `go/types` | | |  |
| 95 | `go/version` | | |  |
| 96 | `hash` | | |  |
| 97 | `hash/adler32` | | |  |
| 98 | `hash/crc32` | | |  |
| 99 | `hash/crc64` | | |  |
| 100 | `hash/fnv` | | |  |
| 101 | `hash/maphash` | | |  |
| 102 | `html` | | |  |
| 103 | `html/template` | | |  |
| 104 | `image` | | |  |
| 105 | `image/color` | | |  |
| 106 | `image/draw` | | |  |
| 107 | `image/gif` | | |  |
| 108 | `image/jpeg` | | |  |
| 109 | `image/png` | | |  |
| 110 | `index/suffixarray` | | |  |
| 111 | `internal/abi` | | |  |
| 112 | `internal/buildcfg` | | |  |
| 113 | `internal/chacha8rand` | | |  |
| 114 | `internal/copyright` | | |  |
| 115 | `internal/coverage/cfile` | | |  |
| 116 | `internal/coverage/cformat` | | |  |
| 117 | `internal/coverage/cmerge` | | |  |
| 118 | `internal/coverage/pods` | | |  |
| 119 | `internal/coverage/slicereader` | | |  |
| 120 | `internal/coverage/slicewriter` | | |  |
| 121 | `internal/coverage/test` | | |  |
| 122 | `internal/cpu` | | |  |
| 123 | `internal/dag` | | |  |
| 124 | `internal/diff` | | |  |
| 125 | `internal/fmtsort` | | |  |
| 126 | `internal/fuzz` | | |  |
| 127 | `internal/godebug` | | |  |
| 128 | `internal/godebugs` | | |  |
| 129 | `internal/gover` | | |  |
| 130 | `internal/itoa` | | |  |
| 131 | `internal/pkgbits` | | |  |
| 132 | `internal/platform` | | |  |
| 133 | `internal/poll` | | |  |
| 134 | `internal/profile` | | |  |
| 135 | `internal/reflectlite` | | |  |
| 136 | `internal/runtime/atomic` | | |  |
| 137 | `internal/runtime/maps` | | |  |
| 138 | `internal/runtime/math` | | | runtime/internal/math (1) |
| 139 | `internal/runtime/sys` | | | runtime/internal/sys (4) |
| 140 | `internal/saferio` | | |  |
| 141 | `internal/singleflight` | | |  |
| 142 | `internal/sync` | | | internal/concurrent (20) |
| 143 | `internal/synctest` | | |  |
| 144 | `internal/syscall/windows` | | |  |
| 145 | `internal/syscall/windows/registry` | | |  |
| 146 | `internal/sysinfo` | | |  |
| 147 | `internal/testenv` | | |  |
| 148 | `internal/trace` | | |  |
| 149 | `internal/trace/internal/oldtrace` | | |  |
| 150 | `internal/types/errors` | | |  |
| 151 | `internal/xcoff` | | |  |
| 152 | `internal/zstd` | | |  |
| 153 | `io` | | |  |
| 154 | `io/fs` | | |  |
| 155 | `io/ioutil` | | |  |
| 156 | `iter` | | |  |
| 157 | `log` | | |  |
| 158 | `log/slog` | | |  |
| 159 | `log/slog/internal/benchmarks` | | |  |
| 160 | `log/slog/internal/buffer` | | |  |
| 161 | `maps` | | |  |
| 162 | `math` | | |  |
| 163 | `math/big` | | |  |
| 164 | `math/bits` | | |  |
| 165 | `math/cmplx` | | |  |
| 166 | `math/rand` | | |  |
| 167 | `math/rand/v2` | | |  |
| 168 | `mime` | | |  |
| 169 | `mime/multipart` | | |  |
| 170 | `mime/quotedprintable` | | |  |
| 171 | `net` | | |  |
| 172 | `net/http` | | |  |
| 173 | `net/http/cgi` | | |  |
| 174 | `net/http/cookiejar` | | |  |
| 175 | `net/http/fcgi` | | |  |
| 176 | `net/http/httptest` | | |  |
| 177 | `net/http/httptrace` | | |  |
| 178 | `net/http/httputil` | | |  |
| 179 | `net/http/internal` | | |  |
| 180 | `net/http/internal/ascii` | | |  |
| 181 | `net/http/pprof` | | |  |
| 182 | `net/internal/cgotest` | | |  |
| 183 | `net/mail` | | |  |
| 184 | `net/netip` | | |  |
| 185 | `net/rpc` | | |  |
| 186 | `net/rpc/jsonrpc` | | |  |
| 187 | `net/smtp` | | |  |
| 188 | `net/textproto` | | |  |
| 189 | `net/url` | | |  |
| 190 | `os` | | |  |
| 191 | `os/exec` | | |  |
| 192 | `os/exec/internal/fdtest` | | |  |
| 193 | `os/signal` | | |  |
| 194 | `os/user` | | |  |
| 195 | `path` | | |  |
| 196 | `path/filepath` | | |  |
| 197 | `plugin` | | |  |
| 198 | `reflect` | | |  |
| 199 | `regexp` | | |  |
| 200 | `regexp/syntax` | | |  |
| 201 | `runtime` | | |  |
| 202 | `runtime/debug` | | |  |
| 203 | `runtime/internal/wasitest` | | |  |
| 204 | `runtime/metrics` | | |  |
| 205 | `runtime/pprof` | | |  |
| 206 | `slices` | | |  |
| 207 | `sort` | | |  |
| 208 | `strconv` | | |  |
| 209 | `strings` | | |  |
| 210 | `sync` | | |  |
| 211 | `sync/atomic` | | |  |
| 212 | `syscall` | | |  |
| 213 | `testing` | | |  |
| 214 | `testing/fstest` | | |  |
| 215 | `testing/iotest` | | |  |
| 216 | `testing/quick` | | |  |
| 217 | `testing/slogtest` | | |  |
| 218 | `text/scanner` | | |  |
| 219 | `text/tabwriter` | | |  |
| 220 | `text/template` | | |  |
| 221 | `text/template/parse` | | |  |
| 222 | `time` | | |  |
| 223 | `unicode` | | |  |
| 224 | `unicode/utf16` | | |  |
| 225 | `unicode/utf8` | | |  |
| 226 | `unique` | | |  |
| 227 | `weak` | | | internal/weak (4) |

**Excluded and therefore absent from the skeleton:** `internal/unsafeheader` (E3),
`runtime/trace` (E4). The four 1.23 E1 rows are absent because axis C never contained them.
**E2 subtractions are still owed and will remove rows from this skeleton, never add them.**

