# RECON — upstream diff go1.23.1 → go1.23.12 (hop A input)

> **State: EXECUTED (2026-08-24).** Reviewed, independently re-derived, and used: its H3 = ∅ verdict
> was confirmed by a second derivation
> ([`REHEARSAL-go12312.md`](REHEARSAL-go12312.md) §2), its attention list drove the rehearsal's
> four-row characterization, and its raw inputs are **banked** (below). It carries two errata and one
> amendment, all dated and in place. Amended, never rewritten.

Date: 2026-08-23 · Method: `gh api repos/golang/go/compare/go1.23.1...go1.23.12` for the commit
list (83 commits), then per-commit `/commits/<sha>` file lists (the compare endpoint caps its files
array, so per-commit enumeration is the ground truth). Cross-referenced against
`docs/ValidatedTestPackages.md` (roster read 2026-08-23: **162 rows / 215 testable — 75.3%**), the
line-anchored `[module: GoManualConversion]` census (**70 marked files** on this checkout — up again
from the plan's "66–67 across the 2026-08-22 lanes"; it moves weekly, re-measured here, not
carried), and `PLAN-hop-campaign.md` §0.2's two instance facts. Raw data in this scratchpad:
`commits.tsv`, `files-by-commit.tsv`, `files-unique.txt`, `src-files-classified.tsv`, `roster.txt`
— *all five **banked** 2026-08-24 in [`hopA-inputs/`](hopA-inputs/); see the closing note.*

---

## Headline

| Reading | Value |
|:--|:--|
| Commits in range | **83** (11 of them version-tag commits, go1.23.2 … go1.23.12) |
| Unique changed files | **161** total · **150 under `src/`** · 11 outside (`VERSION`, `doc/godebug.md`, 9 `test/fixedbugs`/codegen) |
| `src/cmd/**` (toolchain, never converted) | **49** files |
| `runtime` tree (`runtime` + `cgo` + `pprof` + testdata) | **42** files |
| Stdlib-visible remainder (non-cmd, non-runtime-tree) | **~59** files across **~20 packages** |
| Packages ADDED or REMOVED in range | **zero** — every `added` file is testdata or a `_test.go` in an existing package |
| New language features / build-tag flips | **zero** visible — pure patch branch, consistent with §0.2 |

**Plan expectations confirmed by the data:** H3's census will come back **∅** (no package-set
delta); zero language delta holds; channel 1 (release-tag guards) stays inert. Nothing in the range
contradicts `PLAN-hop-campaign.md` §0.2. The range is *runtime-heavy and cmd-heavy*: roughly 60% of
the churn lands where go2cs either stubs (runtime internals) or never looks (`cmd/*`, C files,
non-target `.s`).

---

## Package-bucketed change list (non-cmd), with go2cs platform routing

Corpus platform set is windows/linux/darwin · amd64 (layout L3). "outside set" = the file's build
constraint excludes all three targets, so the regen cannot change the corpus for it.

| Package | Prod files | Test files | What changed / routing notes |
|:--|:--|:--|:--|
| `runtime` | 25 .go + 2 .s | 5 | Timer/ticker `isSending` races (`time.go`, 4 commits); GC & weak-pointer fixes (`mheap.go` ×4, `mgc.go`, `mgcmark.go`, `mcache/mcentral/arena`); goroutine-profile invariants (`mprof.go` ×3, `proc.go` ×4, `coro.go`, `tracestatus.go`); `lockrank.go` regen (119/116 lines); cgocall/g0-stack (`cgocall.go`, `runtime2.go` ×4); `panic.go` deferrangefunc-recover; `syscall_windows.go` SyscallX stack usage; `preempt.go`; `os_linux.go`/`signal_unix.go`/`os_unix_nonlinux.go` (pidfd feature-check). `.s` files: darwin/amd64 (in set, but asm is not converted) + s390x (outside set) |
| `runtime/cgo` | 5 (4 are C) | 0 | C-file warnings + one pragma in `cgo.go` — near-zero corpus effect |
| `syscall` | 8 | 1 | `exec_linux.go` (5 commits, pidfd hardening — linux corpus); `exec_unix.go` +4 (linux+darwin — **hand-own overlap**, see below); `exec_libc2.go` +5 (darwin); `dll_windows.go` +1 noescape (**whole-file hand-own**); `syscall_windows.go` +3 — the `O_CREATE\|O_EXCL` don't-follow-symlinks fix (CVE-2025-0913, **Windows production behavior**); `exec_bsd/freebsd/libc` outside set |
| `os` | 3 | 4 | `pidfd_linux.go` (3 commits, linux), `pidfd_other.go` (win+darwin), `exec_posix.go` (all targets) |
| `os/exec` | 4 | 2 | `lookPath` `""`/`.`/`..` expansion security fix (go1.23.11): `exec.go` +10 all targets, `lp_windows.go` +8 **Windows**, `lp_unix.go` +4, `lp_plan9` outside set; `dot_test.go` +56 new tests |
| `net/http` | 1 | 2 | `client.go` — sensitive-header stripping across repeated redirects (CVE-2024-45336) + proxy-header stripping (go1.23.10) |
| `net/http/internal` | 1 | 1 | `chunked.go` — reject newlines in chunk-size lines (CVE-2025-22871) |
| `net/http/internal/testcert` | 1 | 0 | regenerated test certificate (42/42 lines) — pure data churn in a converted package |
| `net` | 1 | 1 | `sendfile_unix_alt.go` +3 (darwin in set); new `sendfile_unix_test.go` |
| `internal/poll` | 3 | 0 | sendfile fixes: `sendfile_bsd.go` ×3 commits (darwin), `sendfile_linux.go` +3 (linux), solaris outside set |
| `database/sql` | 2 | 2 | **real behavior fix**: avoid closing `Rows` while a `Scan` is in progress (`sql.go` 12/14, `convert.go` −2); `sql_test.go` +56 |
| `reflect` | 2 | 1 | **two behavior fixes to `Seq`/`Seq2`**: method values handled correctly, iteration value gets the right type (`iter.go` reworked, `value.go` 18/3) |
| `crypto/x509` | 1 | 1 | `verify.go` 5/2 — proper IPv6-host check in URI constraints (go1.23.5) |
| `crypto/tls` | 0 | 4 | **test-only** — `Config.Time` pinned in tests whose certificates expired (go1.23.5) |
| `time` | 0 | 2 | **test-only** — `sleep_test.go` +115 lines over 3 commits (Stop/Reset result correctness probes for the runtime timer races), `time_test.go` |
| `unique` | 1 | 1 | `handle.go` 4/3 — don't retain uncloned input as key (use-after-free class) |
| `internal/godebugs` | 1 | 1 | `table.go`: +1 new knob `allowmultiplevcs` (go1.23.11) and winsymlink/winreadlinkvolume version metadata corrected to 1.23 |
| `internal/testenv` | 1 | 0 | +23 lines, additive helper (profiling-support probe) |
| `internal/weak` | 0 | 1 | **test-only** (+82 lines) — the production fix for weak→strong shading lives in `runtime/mheap.go`, NOT in `internal/weak/pointer.go` (the hand-owned `pointer.cs`'s source is untouched) |
| `runtime/debug` | 1 | 0 | `mod.go` +1 doc line — comment-only diff under `-comments` |
| `runtime/metrics` | 1 | 0 | `doc.go` +5 doc lines — comment-only diff |
| `runtime/pprof` | 0 | 2 | test-only, not on roster |
| `crypto/internal/nistec` | 1 (.s) | 0 | ppc64le asm — outside set, no corpus effect |
| `vendor/golang.org/x/net/http/httpproxy` | 1 | 0 | `proxy.go` 8/2 — vendored x/net security fix (CVE-2025-22870); plus `vendor/modules.txt` x/net version line moves |
| src root | `go.mod`, `go.sum` | | x/net requirement bump only — **no new `go.mod` verbs**, so H4's go.mod-reader item is a no-op for this hop |

---

## Roster intersection — validated packages with upstream changes (9 of 162)

| Validated package | Roster count | Upstream files | Test-only? | Hand-own overlap | Attention |
|:--|:--:|:--|:--:|:--:|:--|
| `syscall` | (see roster) | 8 prod + 1 test | no | **YES — direct**: `dll_windows.go` ↔ hand-owned `syscall/windows/dll_windows.cs`; `exec_unix.go` ↔ hand-owned `syscall/linux/exec_unix.cs`; `syscall_windows.go` beside `syscall_windows_impl.cs` | **HIGH** — Windows production fix (CVE-2025-0913 symlink) + `syscall_windows_test.go` +45 new test lines → count may move |
| `os/exec` | 74 / 27 disclosed | 4 prod + 2 test | no | no marked files in os/exec | **HIGH** — LookPath security fix changes `lp_windows.go` (the validated platform); `dot_test.go` +56 → count moves; the 27 host-limit disclosures need re-derivation at new counts |
| `database/sql` | (roster) | 2 prod + 2 test | no | no | **HIGH** — real Rows/Scan race fix; `sql_test.go` +56 → count likely moves |
| `time` | (roster) | 0 prod + 2 test | **yes** | package hand-own `time/tick.cs`; the semantics the new tests probe live in the hand-owned managed timer machinery | **HIGH** — upstream's new Stop/Reset-result tests are a genuine behavioral probe of go2cs's managed timers; count moves and pass is not guaranteed |
| `crypto/tls` | (roster) | 0 prod + 4 test | **yes** | no | MEDIUM — test-suite made robust against its own certificate expiry; banked test conversions refresh; see Surprises §1 |
| `internal/godebugs` | (roster) | 1 prod + 1 test | no | no (`internal/godebug` ≠ `internal/godebugs`; the hand-owned `godebug.cs` package is untouched) | LOW — one new table row; test modification small |
| `internal/testenv` | (roster) | 1 prod | no | no | LOW — additive helper; corpus diff only |
| `runtime/debug` | (roster) | 1 prod | no (doc-only) | package hand-own `stubs_impl.cs` (unaffected file) | LOW — comment-only diff |
| `runtime/metrics` | (roster) | 1 prod | no (doc-only) | package hand-own `sample.cs` (unaffected file) | LOW — comment-only diff |

**The other 153 roster rows have zero upstream file changes** — their H10 re-validation is a
re-run at banked counts, exactly the shape §4.2's shard map assumes. **Linux legs:** the 7 rows
carrying Linux counts (`bytes`, `crypto/rand`, `crypto/sha1`, `debug/buildinfo`,
`go/internal/gcimporter`, `mime`, `path/filepath`) intersect the upstream-touched set **nowhere** —
the Linux-relevant upstream changes (os pidfd, syscall exec_linux, poll sendfile) all land in
packages not on the Linux roster.

Not-yet-validated packages with production changes worth knowing at their eventual validation:
`net/http` (+internal, +testcert), `net`, `internal/poll`, `os`, `reflect`, `crypto/x509`,
`unique`, `internal/weak` (test-only), `runtime/pprof` (test-only) — several sit squarely on the
chartered netpoll/scheduler arcs' path.

---

## Hand-own overlap (H6 differential preview)

Direct file-level overlaps (upstream changed the Go source OF a hand-owned file — these rows WILL
light up in H6's `.cs.auto` differential and need human-eyed classification):

1. **`syscall/dll_windows.go`** (+1: `//go:noescape` on `SyscallN`) ↔ whole-file hand-own
   `src/core/syscall/windows/dll_windows.cs`. The annotation is meaningless in C# — expect
   classify-(b) no-action, but the differential must say so.
2. **`syscall/exec_unix.go`** (+4) ↔ hand-own `src/core/syscall/linux/exec_unix.cs`.
3. **`reflect/value.go`** (18/3, Seq method-value fix) ↔ `reflect/value_impl.cs` companion +
   bodyless-partial `value.cs`. The Seq fixes are real behavior; the converted `iter.cs` picks
   them up on regen, and the impl companion needs an eye for whether its surface intersects.
4. **`runtime/time.go`** (4 commits of timer-race fixes) ↔ the managed timer hand-owns
   (`runtime` hand-own family, `time/tick.cs`). go2cs's timers are a from-scratch managed
   implementation, so the Go-side race fixes don't port line-for-line — but the H6 pass should
   check whether the managed implementation has the same bug SHAPE (Reset/Stop returning the
   wrong result when racing a running timer), because upstream's new `time` tests now probe it.
5. **`runtime/runtime2.go` / `proc.go` / `mprof.go` / `mheap.go` etc.** ↔ the runtime hand-own
   family (`runtime2.cs`, `runtime2_impl.cs`, `managed_impl.cs`, …) — mostly stub-classify, but
   the volume (25 runtime prod files) makes this the bulk of the differential's noise.
6. **`unique/handle.go`** (retention fix) ↔ package-level: hand-own `unique/clone.cs` (its own
   source unchanged); converted `handle.cs` changes on regen.

Package-level only (hand-owns present, their sources untouched): `internal/poll` (netpoll impls
vs sendfile changes — different files), `internal/weak` (`pointer.cs` safe; upstream fix went into
`runtime/mheap.go`), `runtime/debug`, `runtime/metrics`, `time`.

Untouched hand-own families (no upstream change anywhere near): `sync`/`sync/atomic`,
`internal/abi`, `internal/cpu`, `internal/reflectlite`, `internal/godebug`, `math/rand`(+v2),
`hash/crc32`, `crypto/subtle`, vendored `x/crypto/sha3`, `os` dir/file impls, net dnsclient,
`internal/runtime/*` — i.e. most of the 70-file census sees no upstream pressure at all.

---

## Expected churn at the hop

**H5 overlay (the regen diff) — packages whose converted `.cs` should actually change** (~20
packages, modest): `runtime` (largest, mostly stub/comment-level plus `lockrank`), `syscall`
(linux+darwin+windows folders), `os`, `os/exec`, `net/http`, `net/http/internal`,
`net/http/internal/testcert` (pure data), `net` (darwin), `internal/poll` (linux+darwin),
`database/sql`, `reflect`, `crypto/x509`, `unique`, `internal/godebugs`, `internal/testenv`,
`runtime/cgo` (tiny), `runtime/debug` + `runtime/metrics` (comment-only),
`vendor/golang.org/x/net/http/httpproxy`. Plus two `.cs.auto` review siblings surfacing at the two
direct hand-own overlaps (§ above). Everything else in a clean seeded regen should be
byte-identical — an overlay diff outside this list is either a converter change riding along or a
finding.

**H9 behavioral goldens:** nothing in the range touches constructs the behavioral corpus
exercises via version-gated sources — expect the small-or-empty diff §4.3 predicts.

**H10 banked-count movement candidates** (the "Movement" section §4.4 wants, pre-attributed):

| Row | Why it may move | Upstream commits |
|:--|:--|:--|
| `os/exec` (74) | `dot_test.go` +56 (new LookPath-dot tests, Windows-relevant); `exec_posix_test.go` +56 (pidfd-side, likely linux-gated → Windows count may not see it) | `8fa31a2d7`, `c57e2bd22` |
| `database/sql` | `sql_test.go` +56/8, `fakedb_test.go` reworked | `8a924caaf` |
| `time` | `sleep_test.go` +115 over 3 commits, `time_test.go` (a TZ-accept fix, OpenBSD-only) | `3b2e846e1`, `8d79bf799`, `58babf6e0`, `777f43ab2` |
| `syscall` | `syscall_windows_test.go` +45 (Windows count) | `847cb6f9c` |
| `crypto/tls` | mostly modified-in-place; count movement possible but small | `3417000c6` |
| `internal/godebugs` | test modification only; probably static | `e9d2c032b` |

**Badges (H12):** every README's Source·Go badge moves 1.23.1 → 1.23.12 (toolchain-read,
corpus-wide, already the expected churn); additionally the **x/net-family vendored packages'**
Source·Go badges move independently because `src/vendor/modules.txt`'s x/net line changes — worth a
line in H12's expectation statement so it isn't re-diagnosed.

---

## Surprises / notables

1. **`crypto/tls`'s banked validation is standing on tests upstream had to fix because their
   certificates expire by wall clock** (go1.23.5 pinned `Config.Time` in the affected tests). If
   the 1.23.1 banked suite ever starts failing on a future sweep date, that is upstream cert
   expiry, not a go2cs regression — and the hop levels it for free. Worth a note wherever sweep
   false-reds get triaged.
2. **Seven CVE-class security fixes ride the hop** (net/http ×2 + chunked, x/net httpproxy,
   os/exec LookPath, os Windows O_EXCL symlink, crypto/x509 IPv6-URI; plus cmd/go fixes go2cs
   never ships). The hop is a security-posture move for published NuGet packages, not just
   corpus hygiene — a fair line for the release notes.
3. **The `internal/weak` production fix is in `runtime`, not in `internal/weak`** — the
   hand-owned `pointer.cs`'s own source is untouched; only its test file grew. Saves an H6 row
   that a package-level grep would have falsely flagged.
4. **One new GODEBUG knob** (`allowmultiplevcs`) enters `internal/godebugs/table.go` — table-only
   for go2cs (the knob gates `cmd/go` behavior), but the converted table is what the validated
   `internal/godebugs` test checks, so table and doc must move together at the regen.
5. **`reflect`'s Seq/Seq2 fixes are the range's most converter-relevant behavior change** —
   rangefunc iteration over method values returned wrongly-typed values in 1.23.1. go2cs
   converted `reflect/iter.go` faithfully, so the corpus currently reproduces the *bug*; the
   regen picks up the fix. If any behavioral test or validated suite exercises `Value.Seq`,
   expect a verdict-level change, not just an emission diff.
6. **Nothing surprising in size**: 83 commits / 150 src files for eleven patch releases is the
   small range the runbook hoped for. No package census delta, no language delta, no new go.mod
   verbs, no new platform files inside the target set. The hop's real work is exactly where the
   plan put it: the machinery (pin bump, `.auto` differential, badge churn, fleet H10), not the
   diff.

> ## ⚠ ERRATA (2026-08-24), from the rehearsal's independent re-derivation
>
> Two attributions in the tables above are wrong. The conclusions they support are unaffected; the
> attributions are corrected so a later reader does not hunt for something that cannot exist.
>
> 1. **`exec_posix_test.go` is misattributed to `os/exec`.** Upstream the file is
>    `src/os/exec_posix_test.go`, and this document's own package-bucketed table puts
>    `exec_posix.go` under **`os`**. There is **no banked `exec_posix_test.cs`** anywhere under
>    `src/core/os/`, and `os` is not a roster row — so the H10 movement-candidate row for `os/exec`
>    should read only `dot_test.go` for that package. Its conclusion (*"likely linux-gated → the
>    Windows count may not see it"*) stands.
> 2. **`src/core/time/time_impl.cs` is NOT in the marker census.** It carries
>    `[module: go.GoRequiresUnsafe]`, present because `[LibraryImport]` requires `/unsafe`, not
>    `GoManualConversion`. Anyone reconciling `time` against a census total should expect **one**
>    marked file in that package, not two.
>
> ## ⚠ AMENDMENT (2026-08-24) — H6's first-execution figures land here, from the runbook
>
> [`../GoCorpusMigration.md`](../GoCorpusMigration.md) H6 gained its instrument
> (`src/handown-census.ps1`) and ran the hand-own differential against **this** range. The result:
> **73 marked files → 6 substantive** — `reflect/value.go`, `runtime/runtime2.go` (two hand-owns),
> `syscall/exec_unix.go`, `syscall/dll_windows.go`, `syscall/syscall_windows.go` — with **50
> untouched** and **17 no-upstream-counterpart**. Every substantive row was independently
> cross-checked against this document's package-bucketed table above, and all six fall inside the
> packages it already flagged.
>
> **Why the figures live here rather than in the runbook.** The runbook carries **no frozen
> figures** by charter: its H6 keeps the *shape* the instrument produces — a census of dozens
> reducing to a single-digit review list, and a substantive class near the census size read as a
> stripper bailing out rather than upstream churn — and points at this record for the instance. A
> later migration's figures belong in *its* record, not in either of these two.
>
> One reading worth carrying: the marker census moved **70 → 73** in the single day between this
> recon and that run. That is the re-measure-never-carry rule working exactly as written, not a
> discrepancy to reconcile.

— Recon lane, 2026-08-23. Raw TSVs beside this file for re-derivation. *(Amended 2026-08-24: they
were not, in fact, beside this file — they lived in the recon session's scratchpad. They are now
**banked** in [`hopA-inputs/`](hopA-inputs/) (`5d4410b71`), re-derived independently on a second
machine with every count above reproduced exactly, and the derivation is written out in that
directory's README. "Beside this file" is true again.)*
