# AUDIT — H6 hand-own re-audit, go1.23.12 → go1.24.13 (SKELETON)

> **Record type:** this migration's H6 audit file, per runbook [§H6](../GoCorpusMigration.md)
> (*"one audit file per migration under `docs/phase4/`"*). Created as a **SKELETON** on 2026-09-13 by
> lane R, per COORD `db6d9462f` §5. **State: SKELETON — every class and every hash is BLANK** until the
> H5 series produces the `.auto` pair (go1.23.12 and go1.24.13) from **ONE** converter binary.
> **Measured at `2e6cf71e4`.** Every `file:line` citation below resolves there. The file is cut on
> `a02ac3df3`, and between the two refs `git diff --quiet` reads rc=0 over `src/core`,
> `src/handown-census.ps1`, `src/_paths.ps1`, `src/go2cs/conversionDriver.go`, the dossier, the runbook
> and `PLAN-corpus-upgrade.md`, so no figure below moves.
> *Proposal, not a ruling:* rows are filled in place at H5/H6, and the dated-block convention applies to
> everything outside §4. Neither the runbook nor the Glossary's document types settles this (OQ-13).
> Input dossier: [`CENSUS-h6-handown-go124.md`](CENSUS-h6-handown-go124.md). Instrument:
> [`src/handown-census.ps1`](../../src/handown-census.ps1).

## 1. Population

**Predicate** — the instrument's own (`src/handown-census.ps1:122`), line-anchored, whole-file,
tracked `.cs` only, admitting both marker spellings:

```
git grep -l -E '^\s*\[module:\s*(go\.)?GoManualConversion\]' <ref> -- 'src/core/*.cs'
```

```
  at 2e6cf71e4 (and a02ac3df3)          146 files   = 23 bare [module: GoManualConversion]
                                                    + 123 [module: go.GoManualConversion]   (disjoint)
  at bd1d26faf                          146 files   (src/core identical between the two refs)
  handown-census.ps1, same tree         146 marked  (self-verify passed; §4 header)
```

⚠ **Re-measure, never carry.** At fill time the gate re-runs this predicate over the tree being
adopted; a row count that differs from 146 means this table is edited to match the **re-measured**
census before any class is written, never the reverse.

**Three figures have circulated for this population. They answer three different questions:**

| figure | predicate | tree | what it counts |
|:--|:--|:--|:--|
| **146** | the anchored predicate above | `2e6cf71e4` / `bd1d26faf` | marked hand-owns — **the audit population** |
| **105** | a command that reproduces the circulated figure; its original command is not recorded. `git grep -l -F 'module: GoManualConversion' <ref> -- src/core` (fixed string, unanchored, **every file type**) gives it, and so do the fixed string `[module: GoManualConversion]` and the unanchored bare regex, all with the identical file set | `bd1d26faf` (and `2e6cf71e4`: 105) | 104 `.cs` + 1 `runtime/runtime2.cs.auto`. Of the 104 `.cs`, only **26** are marked: the 23 bare-spelled files, plus three `go.`-spelled files that also carry the bare text in a `//` comment (`runtime/metrics/sample.cs`, `runtime/runtime2.cs`, `sync/atomic/value.cs`). **78** only *mention* it in comments: 77 generated placeholder lines (e.g. `crypto/internal/alias/alias.cs:13`) and `internal/concurrent/package_info.cs:65`. It **misses 120** of the 146, because `module: go.GoManualConversion` does not contain the fixed string. Restricted to `*.cs` it reads 104. |
| **153** | dossier §1: the anchored predicate **∪** every `*_impl.cs` | `2c0107614` | 142 marked + 11 **unmarked** `*_impl.cs` companions (109 companions, 98 of them marked). Re-measured at `2c0107614` today: 142 / 109 / 98 / 11 / 153, reproduced exactly. |

**The 153 → 146 difference (7) is two movements, both named:**

```
  + 11  unmarked *_impl.cs companions: in the dossier's union, outside the marker predicate
        (the same 11 names at 2c0107614 and at 2e6cf71e4)
          crypto/internal/boring/sig/sig_impl.cs      internal/abi/funcpc_impl.cs
          internal/bytealg/bytealg_impl.cs            internal/reflectlite/type_impl.cs
          internal/reflectlite/value_impl.cs          math/math_impl.cs
          os/linux/pidfd_linux_impl.cs                os/proc_impl.cs
          reflect/abi_impl.cs                         sync/atomic/doc_impl.cs
          time/time_impl.cs
  -  4  marked files ADDED after 2c0107614 (each absent at 2c0107614; commit that added it)
          internal/syscall/windows/windows/zsyscall_windows_version_impl.cs   4e133844c
          runtime/panic_impl.cs                                               8fdbd4704
          syscall/windows/syscall_windows_callback_impl.cs                    193af90f5
          time/sleep_impl.cs                                                  b50d08c42
  ---
  153 - 11 + 4 = 146        (today's union would read 146 + 11 = 157)
```

Not a movement of the population, recorded so it is not mistaken for one: ruling `cb24ac747` (dossier,
*RULING RECORDED*) took the dossier's population 153 → 149 by removing four `testing/` host files.
**All four are marked and remain in the 146** — see OQ-8.

Two dossier §10 rows sit in the 11 unmarked companions and so have **no row here**:
`os/linux/pidfd_linux_impl.cs` (§10 MEMBERS-REMOVED → RE-DERIVE) and `time/time_impl.cs`
(§10 MEMBERS-REMOVED → RE-DERIVE) — see OQ-1.

## 2. How a row is filled

Paraphrased from runbook [§H6](../GoCorpusMigration.md); the runbook leads on any disagreement.

**The diff is `.auto`(go1.23.12) vs `.auto`(go1.24.13)**, per hand-own, both from the same converter
binary — never `.auto` against the hand-owned `.cs`. A row's **class** is exactly one of:

| class | when | required record in *reason / work item* |
|:--|:--|:--|
| `unchanged` | the `.auto` diff is empty | both sha256 columns filled (the runbook: *"an empty diff still gets a record (`unchanged`, with both hashes)"*) |
| `a` ABSORBED | the upstream change is real and carried into the hand-own | the carrying commit, and a test or gate that observes it |
| `b` N/A | the upstream change does not apply to the managed implementation | **the reason, written out** — never the bare letter |
| `c` REWRITE OWED | the hand-own must change and has not | a named work item, gating the migration or deferred with owner and reason |

**A row whose hand-own got no `.auto` emitted is a DEFECT in the audit, not a pass** — the seed did not
take at that path, or the marker predicate could not see the marker. It is never filled `unchanged`.

**Two populations (ruled):** the audit covers **all** hand-owns; the `.auto` differential reaches only
the ones the converter re-emits. **Every row names its evidence class.** The rule applied here, in
precedence order:

1. a file in a **hand-owned package** (`testing`, `unsafe` — never converted) → **`manual upstream diff`**;
2. a **`*_impl.cs` companion** → **`principal .auto`**, audited against its principal's diff. The
   principal is named in the cell: the same-directory file with `_impl` removed, when it exists in the
   tree; the three per-GOOS files, where layout L3 put it there (2 rows); otherwise
   *principal not named* (41 rows, OQ-5);
3. a **whole-file** hand-own whose instrument class is not `no-upstream-counterpart` (its mapped Go file
   exists at either release) → **`.auto differential`**;
4. a whole-file hand-own the instrument reads `no-upstream-counterpart`, outside a hand-owned package →
   would be `manual upstream diff`; **both such rows are UNPLACED** instead, each for a measured reason
   (OQ-6, OQ-7).

```
  .auto differential         31      principal .auto, principal named      61
  manual upstream diff       11      principal .auto, principal not named  41
  UNPLACED                    2                                           ---
                                                                          146
```

**Dossier pointer column — a proposal, not a class.** It is a **selection**: disposition-bearing
citations from the dossier sections and dated blocks listed here. It is not every place the dossier names
a row. For example, the 09-08 stated-delta, ten-candidates and retraction blocks, and the per-file delta
tables, name about 30 whole-file rows with verdict words this column does not cite. Labels: **§1/§2/§4/§7/§10** are the dossier's numbered sections;
dated blocks are cited by their heading — *09-07 scope-rule* (SCOPE-RULE CORRECTION), *09-07
sync-collision* (A SECOND COLLISION, IN `sync`), *09-07 concurrent* (DOSSIER ADDITION:
`internal/concurrent`), *09-07 BOTH* (THE `BOTH` CLASS), *09-08 base* (THE BASE QUESTION, CLOSED OVER
ALL 44), *09-08 date-screen* (MY OWN DATE SCREEN IS A BAD SCREEN). 62 of the 146 rows carry a pointer.
The dossier's §6 names Go principal files rather than hand-owns; its needs-a-human rows are linked
through §10, which names the same rows by hand-own path.
"Base" pointers describe the **tracked** `.cs.auto` sibling, not the H5 pair this file is filled from.

## 3. The completeness gate

The runbook states it: no migration's corpus is adopted until every hand-own in the **re-measured**
census has a classified delta record here, and every (c) is closed or explicitly deferred with an owner.
Mechanically:

1. re-measure the line-anchored census over `src/core`;
2. assert every marked path appears **exactly once** in §4;
3. assert every row's class ∈ {`unchanged`, `a`, `b`, `c`};
4. assert every `b` carries a non-empty reason and every `c` a work-item reference;
5. assert **zero** rows in the "no `.auto` emitted" state;
6. exit non-zero on any violation — cheap, by-path, impossible to pass vacuously.

⚠ **No script implements this gate at `2e6cf71e4` (owed).** Measured: no file under `src/` references
an H6 audit file, and no tracked file named `check-handown-audit*` exists. `PLAN-corpus-upgrade.md`
proposed `check-handown-audit.ps1`; what landed is the census half only (`src/handown-census.ps1`),
which *"decides where H6 looks, never what H6 concludes"*. How the gate is run until the script exists
is OQ-12. A gate that has never been made to fail proves nothing.

## 4. The rows

Instrument run (reproduces the population): `powershell.exe -NoProfile -ExecutionPolicy Bypass -File
src/handown-census.ps1 -FromGoRoot <go1.23.12 GOROOT> -ToGoRoot <go1.24.13 GOROOT> -ListUntouched`,
tree byte-identical to `2e6cf71e4` in `src/core`, `src/_paths.ps1` and `src/handown-census.ps1`:

```
  exit 0      handown-census: 146 marked files
  no-upstream-counterpart    51
  touched-substantive        46
  touched-trivial             6
  untouched                  43
  TOTAL                     146      <- self-verify: classes sum to the census
```

The instrument prints substantive and trivial rows with their mapped Go file and untouched rows by
path; it does **not** print `no-upstream-counterpart` rows, so those 51 are the complement. Cross-checks
(all zero violations): every path once; the mapped Go file re-derived by the instrument's rule
(`handown-census.ps1:128-139`: drop a trailing `windows`/`linux`/`darwin` folder, drop `_impl`) matches
all 52 mappings it prints (a planted wrong mapping read 1 mismatch); every complement row's Go file is
absent at both releases; every untouched row is byte-identical at both; every touched row differs or
exists on one side only.

*Upstream Go file* is repo-relative to `GOROOT/src`. The last four columns are **BLANK** (`—`) until H5/H6.

| # | path (under `src/core`) | upstream Go file (mapped) | instrument class go1.23.12→go1.24.13 | evidence class | dossier pointer (proposal, not a class) | `.auto`@go1.23.12 sha256 | `.auto`@go1.24.13 sha256 | class | reason / work item |
|--:|:--|:--|:--|:--|:--|:--|:--|:--|:--|
| 1 | `crypto/internal/boring/bcache/cache.cs` | `crypto/internal/boring/bcache/cache.go` | untouched | .auto differential | §1 hand-owned by consequence; §2 PRESENT-UNCHANGED (named control); §4 #7 keep | — | — | — | — |
| 2 | `crypto/internal/fips140/alias/alias_impl.cs` | `crypto/internal/alias/alias.go` (absent at go1.24.13) | touched-substantive | principal .auto — `crypto/internal/alias/alias.cs` · OQ-2 | §2 REMOVED; §4 #3 re-route | — | — | — | — |
| 3 | `crypto/internal/fips140/subtle/xor_generic.cs` | `crypto/subtle/xor_generic.go` (absent at go1.24.13) | touched-substantive | .auto differential · OQ-2 | §2 MOVED-or-NEW-SHAPE; §4 #4 re-derive against the new principal | — | — | — | — |
| 4 | `debug/pe/symbol_impl.cs` | `debug/pe/symbol.go` | touched-substantive | principal .auto — `debug/pe/symbol.cs` | — | — | — | — | — |
| 5 | `hash/crc32/crc32_amd64.cs` | `hash/crc32/crc32_amd64.go` | untouched | .auto differential | 09-08 date-screen §3: base GENUINELY STALE | — | — | — | — |
| 6 | `internal/abi/type_impl.cs` | `internal/abi/type.go` | touched-substantive | principal .auto — `internal/abi/type.cs` | §10 MEMBERS-REMOVED → RE-WRITE | 6ede9fd81a9abd945500d5bbf513e3c37be4b483e082767c0018ec6818af2658 | 39d191d52d6363fbde068e6c2a6249038d55124a8d90529cc602377f9e899583 | b | companion-vs-principal · NOT-APPLICABLE-TO-MANAGED (proposed `b` reason, block 6): the member-body arm names two members THIS companion owns as BODY-DIFFERS, Elem (type_impl.cs:556) and Key (type_impl.cs:641); their Go delta is only `(*MapType)(unsafe.Pointer(t))` -> `(*mapType)(unsafe.Pointer(t))`, the 1.24 map-type split behind a build-tagged alias; the managed Elem/Key resolve the element/key through the carried System.Type (GoReflect.ElementType / GoReflect.KeyType) and never read the linker's map record, so the change cannot reach them. Also in the pair, emitted and not the companion's: ΔMapType and its IndirectKey / IndirectElem / ReflexiveKey / NeedKeyUpdate / HashMightPanic leave type.go, KindGCProg is removed, TFlagUnrolledBitmap becomes TFlagGCMaskOnDemand, GcSlice gains an on-demand guard; search predicate over the companion's whole text, whole words: ΔMapType MapType mapType SwissMapType OldMapType KindGCProg TFlagUnrolledBitmap TFlagGCMaskOnDemand GcSlice IndirectKey IndirectElem ReflexiveKey NeedKeyUpdate HashMightPanic Bucket Hasher GCData = 3, every hit a comment (mapType at lines 539 and 540, TFlagUnrolledBitmap at line 248), none a reference (control synthType = 26); the other eight hand-converted members BODY-IDENTICAL (ArrayType, ChanDir, FuncType, InSlice, Len, OutSlice, StructType, TypeOf). Sides: 1.23.12 `internal/abi/type.cs` vs 1.24.13 `internal/abi/type.cs`, identical on all three targets. Rules 8808a00ad, 80c948a7f, identity f6c60275e, arm 8cf7fdf65 (G fill block 6). |
| 7 | `internal/chacha8rand/chacha8_impl.cs` | `internal/chacha8rand/chacha8.go` | touched-substantive | principal .auto — `internal/chacha8rand/chacha8.cs` | — | e2ba9b6ad6e66c4577a99cde083afaad73e1214d6b92f96d8d36f5f741250a28 | d5fa5d9b1015d03dd1fa604b481d6819c042f27eddd7431ecc75f1da62e64cc3 | b | companion-vs-principal · UPSTREAM-IN-PRINCIPAL: changed members the internal/byteorder call sites renamed at 1.24.13 (LeUint64 -> LEUint64, BeUint64 -> BEUint64, LePutUint64 -> LEPutUint64, BePutUint64 -> BEPutUint64) in Seed/MarshalBinary/UnmarshalBinary; the Go principal `internal/chacha8rand/chacha8.go` changes in 16 code lines, all those renames; search predicate over the companion's whole text, whole words: LeUint64 LEUint64 BeUint64 BEUint64 LePutUint64 LEPutUint64 BePutUint64 BEPutUint64 byteorder = 0 (control: the same whole-file search matched the companion's own member declarations, block at line 66 and block_generic at line 237); member bodies: 0 placeholders in the principal, so the Go functions the companion realises were compared by name: block (chacha8.go, 13 lines), block_generic and setup (chacha8_generic.go, 63 lines each) all BODY-IDENTICAL with identical declarations (control runtime stdcall fires in the same run). Sides: 1.23.12 `internal/chacha8rand/chacha8.cs` vs 1.24.13 `internal/chacha8rand/chacha8.cs`, identical on all three targets. Rules 8808a00ad, 80c948a7f, identity f6c60275e, shape f43ae82f1, arm 8cf7fdf65 (G fill block 5). |
| 8 | `internal/cpu/cpu_x86_impl.cs` | `internal/cpu/cpu_x86.go` | touched-substantive | principal .auto — `internal/cpu/cpu_x86.cs` | §10 MEMBERS-ADDED → RE-DERIVE | 59d6d3e6017eac1222aa20820845cc63b841ff178aaf444a6640629f3e0c3cf4 | 6026c5e273256a76bd55912732ddf36b3041c831e8ba3d883e5b0c6d062d1046 | b | companion-vs-principal · UPSTREAM-IN-PRINCIPAL: changed members the FSRM feature detection in doinit (cpuid_FSRM, the "fsrm" option, cpuid(7, 0)'s edx7, X86.HasFSRM), all emitted; search predicate over the companion's whole text, whole words: cpuid_FSRM HasFSRM fsrm FSRM edx7 = 0 (control getGOAMD64level = 4); member bodies: getGOAMD64level is a body-less assembly stub (`func getGOAMD64level() int32`, cpu_x86.go:18) whose declaration is identical at both releases (read after the extractor fix of block 6). Sides: 1.23.12 `internal/cpu/cpu_x86.cs` vs 1.24.13 `internal/cpu/cpu_x86.cs`, identical on all three targets. Rules 8808a00ad, 80c948a7f, identity f6c60275e, shape f43ae82f1, arm 8cf7fdf65 (G fill block 6). |
| 9 | `internal/godebug/godebug.cs` | `internal/godebug/godebug.go` | untouched | .auto differential | §1 hand-owned by consequence | 13d930b3ef39dd0e84602c66732fba121bb50e1dde1ae9277c0b7a1d43045325 | bbd2c00d33638e006d2bad5f2b0340e14b856794e4fa269f24770b516e60351e | b | auto-differential · EMISSION-ONLY: Go principal byte-identical at 1.23.12 and 1.24.13 (sha256 617c491be3f119672c81e7aeecd0b27b7262b00d364e8544ec7b0e46c1a30c14 = 617c491be3f119672c81e7aeecd0b27b7262b00d364e8544ec7b0e46c1a30c14, `internal/godebug/godebug.go`); the .auto delta is the converter's closure-dependent alias spelling (sync re-qualified, `using go;` added), the fourth-arm class ruled 2026-09-08 (runbook H5 window ruling); the hand-own has no such site (no sync alias; it declares bisect and godebugs only). Sides: 1.23.12 `internal/godebug/godebug.cs.auto` vs 1.24.13 `internal/godebug/godebug.cs.auto`, identical on all three targets. Rules 8808a00ad, 80c948a7f, identity f6c60275e, shape f43ae82f1 (G fill block 5). |
| 10 | `internal/poll/darwin/runtime_netpoll_impl.cs` | `internal/poll/runtime_netpoll.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 11 | `internal/poll/fd_mutex_impl.cs` | `internal/poll/fd_mutex.go` | untouched | principal .auto — `internal/poll/fd_mutex.cs` | — | — | — | — | — |
| 12 | `internal/poll/linux/fd_writev_unix.cs` | `internal/poll/fd_writev_unix.go` | untouched | .auto differential · OQ-4 | 09-08 base §1: NO base banked | — | — | — | — |
| 13 | `internal/poll/linux/runtime_netpoll_impl.cs` | `internal/poll/runtime_netpoll.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 14 | `internal/poll/runtime_sema_impl.cs` | `internal/poll/runtime_sema.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 15 | `internal/poll/windows/fd_windows_impl.cs` | `internal/poll/fd_windows.go` | untouched | principal .auto — `internal/poll/windows/fd_windows.cs` | — | 35044179db39221a494d95d42279cd9b6d64a7f6097d9bdabf86e8b910e450a3 | ccfc1a520f4a243f3d79c0933c438d3b4f070862f12cad6af570b23326eff08d | b | companion-vs-principal · EMISSION-ONLY: Go principal byte-identical at 1.23.12 and 1.24.13 (sha256 bcb24cce878d09fe5ec486c0c4085b86e5b6e00fe94e35f5ca9e62143447156b = bcb24cce878d09fe5ec486c0c4085b86e5b6e00fe94e35f5ca9e62143447156b, `internal/poll/fd_windows.go`); the .auto delta is the converter's closure-dependent alias spelling (sync re-qualified), the fourth-arm class ruled 2026-09-08 (runbook H5 window ruling); the hand-own has no such site (0 `sync` references). Sides: 1.23.12 `internal/poll/windows/fd_windows.cs` vs 1.24.13 `internal/poll/windows/fd_windows.cs`, windows-amd64. Rules 8808a00ad, 80c948a7f, identity f6c60275e, shape f43ae82f1 (G fill block 1, amended in block 2). |
| 16 | `internal/poll/windows/runtime_netpoll_impl.cs` | `internal/poll/runtime_netpoll.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 17 | `internal/reflectlite/swapper_impl.cs` | `internal/reflectlite/swapper.go` | untouched | principal .auto — `internal/reflectlite/swapper.cs` | — | — | — | — | — |
| 18 | `internal/runtime/atomic/atomic_impl.cs` | `internal/runtime/atomic/atomic.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 19 | `internal/runtime/syscall/linux/syscall_linux_impl.cs` | `internal/runtime/syscall/syscall_linux.go` | untouched | principal .auto — `internal/runtime/syscall/linux/syscall_linux.cs` | — | — | — | — | — |
| 20 | `internal/sync/hashtriemap.cs` | `internal/concurrent/hashtriemap.go` (absent at go1.24.13) | touched-substantive | .auto differential · OQ-2 | §1 hand-owned by consequence; §2 REMOVED; §4 #2 re-route; 09-07 concurrent: DELETE THE DIRECTORY; 09-08 date-screen §3: base GENUINELY STALE | — | — | — | PRINCIPAL CHANGED; RE-DERIVE IN PROGRESS (C1, ruled 4e42736e1) |
| 21 | `internal/syscall/unix/darwin/net_darwin_impl.cs` | `internal/syscall/unix/net_darwin.go` | touched-substantive | principal .auto — `internal/syscall/unix/darwin/net_darwin.cs` | §10 MEMBERS-ADDED → RE-DERIVE | 5c18376e316de0f636ced1132c89335090902826297a06e7ffd899ed4ac1ff56 | 5dde953ddf2c503c9c863e2732d4d6a7c7582a2eccb289465716eed5db1adafc | b | companion-vs-principal · UPSTREAM-IN-PRINCIPAL: changed member the new constant `EAI_ADDRFAMILY` (the Go principal `internal/syscall/unix/net_darwin.go` differs between the releases; the emitted delta is that one line); search predicate over the companion's whole text, whole words: EAI_ADDRFAMILY = 0 (control Getaddrinfo = 5); the companion's hand-converted members read BODY-IDENTICAL in the Go principal at both releases (Getaddrinfo 14 lines, Freeaddrinfo 5 lines). Sides: 1.23.12 `internal/syscall/unix/darwin/net_darwin.cs` vs 1.24.13 `internal/syscall/unix/darwin/net_darwin.cs`, darwin-amd64. Rules 8808a00ad, 80c948a7f, identity f6c60275e, shape f43ae82f1 (G fill block 3). |
| 22 | `internal/syscall/unix/darwin/user_darwin_impl.cs` | `internal/syscall/unix/user_darwin.go` | untouched | principal .auto — `internal/syscall/unix/darwin/user_darwin.cs` | — | — | — | — | — |
| 23 | `internal/syscall/unix/linux/net_linux_impl.cs` | `internal/syscall/unix/net_linux.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | §10 minted shell → RE-DERIVE | — | — | — | — |
| 24 | `internal/syscall/unix/linux/siginfo_linux.cs` | `internal/syscall/unix/siginfo_linux.go` | untouched | .auto differential | — | — | — | — | — |
| 25 | `internal/syscall/windows/exec_windows_test.cs` | `internal/syscall/exec_windows_test.go` (absent at both) | no-upstream-counterpart | UNPLACED · OQ-6 · OQ-3 | §10 minted shell → ASK | — | — | — | — |
| 26 | `internal/syscall/windows/registry/registry_test.cs` | `internal/syscall/windows/registry/registry_test.go` | untouched | .auto differential · OQ-3 | — | — | — | — | — |
| 27 | `internal/syscall/windows/registry/windows/value.cs` | `internal/syscall/windows/registry/value.go` | untouched | .auto differential | — | — | — | — | — |
| 28 | `internal/syscall/windows/windows/net_windows_impl.cs` | `internal/syscall/windows/net_windows.go` | untouched | principal .auto — `internal/syscall/windows/windows/net_windows.cs` | — | — | — | — | — |
| 29 | `internal/syscall/windows/windows/syscall_windows_impl.cs` | `internal/syscall/windows/syscall_windows.go` | touched-substantive | principal .auto — `internal/syscall/windows/windows/syscall_windows.cs` | §10 MEMBERS-ADDED → RE-DERIVE | fe19654e155d06a46566e0a051ca0c1c35a6abaea0d9c766fb9e9f733c703782 | ff334262db862ba59b3510b8a89c2139abfad9988be2d40b49ade703c567f474 | b | companion-vs-principal · UPSTREAM-IN-PRINCIPAL: changed members the new `NTStatus` type (with `Errno`/`Error` methods), `langID`, STATUS_FILE_IS_A_DIRECTORY / STATUS_DIRECTORY_NOT_EMPTY / STATUS_NOT_A_DIRECTORY / STATUS_CANNOT_DELETE / STATUS_REPARSE_POINT_ENCOUNTERED, ERROR_NO_TOKEN, ERROR_CANT_ACCESS_FILE, the //sys declarations NtCreateFile / NtOpenFile / NtSetInformationFile / rtlNtStatusToDosErrorNoTeb / GetModuleHandle, and `sync` re-qualified; search predicate over the companion's whole text, whole words: NTStatus Errno langID STATUS_FILE_IS_A_DIRECTORY STATUS_DIRECTORY_NOT_EMPTY STATUS_NOT_A_DIRECTORY STATUS_CANNOT_DELETE STATUS_REPARSE_POINT_ENCOUNTERED ERROR_NO_TOKEN ERROR_CANT_ACCESS_FILE NtCreateFile NtOpenFile NtSetInformationFile rtlNtStatusToDosErrorNoTeb GetModuleHandle = 0; `Error` = 1, a comment at line 140 ("Error handling is the generated wrapper's"), not the member (control WSASendMsg = 8). Sides: 1.23.12 `internal/syscall/windows/windows/syscall_windows.cs` vs 1.24.13 `internal/syscall/windows/windows/syscall_windows.cs`, windows-amd64. Rules 8808a00ad, 80c948a7f, identity f6c60275e, shape f43ae82f1 (G fill block 2). |
| 30 | `internal/syscall/windows/windows/zsyscall_windows_impl.cs` | `internal/syscall/windows/zsyscall_windows.go` | touched-substantive | principal .auto — `internal/syscall/windows/windows/zsyscall_windows.cs` | §10 MEMBERS-ADDED → RE-DERIVE | 52782bee252b129753e97e9ce3058b60678e0ed548d8c32112f5adbf06434080 | 8a76c3ed65df32a7a0b024fd4b73b7e5dd445c9e5b6a64dc0ee1431ef6cd8251 | b | companion-vs-principal · UPSTREAM-IN-PRINCIPAL: changed members the new generated wrappers and procs getSidIdentifierAuthority / getSidSubAuthority / getSidSubAuthorityCount / ImpersonateLoggedOnUser / IsValidSid / LogonUser / NetUserAdd / NetUserDel / NtCreateFile / NtOpenFile / NtSetInformationFile / rtlNtStatusToDosErrorNoTeb / GetModuleHandle, plus the converter's renumbering of its `ᴋN` pin temporaries (emission-only); search predicate over the companion's whole text, whole words: getSidIdentifierAuthority getSidSubAuthority getSidSubAuthorityCount ImpersonateLoggedOnUser IsValidSid LogonUser NetUserAdd NetUserDel NtCreateFile NtOpenFile NtSetInformationFile rtlNtStatusToDosErrorNoTeb GetModuleHandle = 0 (control NetShareAdd = 8). Sides: 1.23.12 `internal/syscall/windows/windows/zsyscall_windows.cs` vs 1.24.13 `internal/syscall/windows/windows/zsyscall_windows.cs`, windows-amd64. Rules 8808a00ad, 80c948a7f, identity f6c60275e, shape f43ae82f1 (G fill block 2). |
| 31 | `internal/syscall/windows/windows/zsyscall_windows_module_impl.cs` | `internal/syscall/windows/zsyscall_windows_module.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 32 | `internal/syscall/windows/windows/zsyscall_windows_privilege_impl.cs` | `internal/syscall/windows/zsyscall_windows_privilege.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | §10 minted shell → RE-DERIVE | — | — | — | — |
| 33 | `internal/syscall/windows/windows/zsyscall_windows_ptrout_impl.cs` | `internal/syscall/windows/zsyscall_windows_ptrout.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | §10 minted shell → RE-DERIVE | — | — | — | — |
| 34 | `internal/syscall/windows/windows/zsyscall_windows_version_impl.cs` | `internal/syscall/windows/zsyscall_windows_version.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 35 | `internal/syscall/windows/windows/zsyscall_windows_wsa_impl.cs` | `internal/syscall/windows/zsyscall_windows_wsa.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 36 | `iter/iter_impl.cs` | `iter/iter.go` | touched-trivial | principal .auto — `iter/iter.cs` | — | — | — | — | — |
| 37 | `math/bits/bits_impl.cs` | `math/bits/bits.go` | touched-trivial | principal .auto — `math/bits/bits.cs` | — | 49906a44f3728662dbcda2b966896ac63147b059a1b3eaa7fe401767bcab488a | 3c1a7fb3d3817ded4ce22806eb467939540ff5c3191b3d528dcd18b7857c8159 | b | companion-vs-principal · COMMENT-ONLY: the Go principal differs at 1.24.13 in comment lines only (2 changed lines: 2 comment, 0 directive (//go:), 0 code, 0 blank; classed by diff --strip-trailing-cr, first non-space characters // and not //go:; the deBruijn reference URL in `math/bits/bits.go`); the .auto delta is that one comment line; nothing executable changed; member bodies: Add, Mul, Sub BODY-IDENTICAL. Sides: 1.23.12 `math/bits/bits.cs` vs 1.24.13 `math/bits/bits.cs`, identical on all three targets. Rules 8808a00ad, 80c948a7f, identity f6c60275e, arm 8cf7fdf65, shape 999d5c784 (G fill block 5, re-shaped in block 6). |
| 38 | `math/rand/rand_impl.cs` | `math/rand/rand.go` | touched-substantive | principal .auto — `math/rand/rand.cs` | §10 MEMBERS-ADDED → RE-DERIVE | — | — | — | — |
| 39 | `math/rand/v2/rand_impl.cs` | `math/rand/v2/rand.go` | touched-trivial | principal .auto — `math/rand/v2/rand.cs` | — | — | — | — | — |
| 40 | `net/dnsclient_impl.cs` | `net/dnsclient.go` | untouched | principal .auto — `net/{windows,linux,darwin}/dnsclient.cs` | — | — | — | — | — |
| 41 | `net/windows/interface_windows_impl.cs` | `net/interface_windows.go` | untouched | principal .auto — `net/windows/interface_windows.cs` | — | — | — | — | — |
| 42 | `net/windows/lookup_windows.cs` | `net/lookup_windows.go` | untouched | .auto differential · OQ-4 | 09-08 base §1: NO base banked | 8d660c13e459a577e5680aa3d39be7faa5f89f724938b723a3100edff437104f | 7258eb81404515c4015afbbb0982f8bfa2cb416644e1a7a0ce96044cf90018fc | b | auto-differential · EMISSION-ONLY: Go principal byte-identical at 1.23.12 and 1.24.13 (sha256 f1ae3a0b18005bedeaf18e34973a3b72bac43f9b0bae0a0af0c54c299ab767b0 = f1ae3a0b18005bedeaf18e34973a3b72bac43f9b0bae0a0af0c54c299ab767b0, `net/lookup_windows.go`); the .auto delta is the converter's closure-dependent alias spelling (Δruntime->runtime), the fourth-arm class ruled 2026-09-08 (runbook H5 window ruling); the hand-own declares its own aliases at line 41. Sides: 1.23.12 `net/windows/lookup_windows.cs.auto` vs 1.24.13 `net/windows/lookup_windows.cs.auto`, windows-amd64. OQ-4 answered for this row: an .auto IS emitted at both releases. Rules 8808a00ad, 80c948a7f, identity f6c60275e, shape f43ae82f1 (G fill block 1, amended in block 2). |
| 43 | `os/darwin/dir_darwin_impl.cs` | `os/dir_darwin.go` | untouched | principal .auto — `os/darwin/dir_darwin.cs` | — | 660596539dce3d1da21140f694fdfbd3de8196963bee519bad0c04848564d047 | eded78b3b7597f662a7737dafd60e50c2a427c6ef0e680d4e55d4c30585ca9d4 | b | companion-vs-principal · EMISSION-ONLY: Go principal byte-identical at 1.23.12 and 1.24.13 (sha256 bff3978bc6e7257fb66c88f0b09d931d249d09eda9d87b042bfb81f9469ad8d1 = bff3978bc6e7257fb66c88f0b09d931d249d09eda9d87b042bfb81f9469ad8d1, `os/dir_darwin.go`); the .auto delta is the converter's closure-dependent alias spelling (Δruntime->runtime), the fourth-arm class ruled 2026-09-08 (runbook H5 window ruling); the hand-own declares its own aliases at line 68. Sides: 1.23.12 `os/darwin/dir_darwin.cs` vs 1.24.13 `os/darwin/dir_darwin.cs`, darwin-amd64. Rules 8808a00ad, 80c948a7f, identity f6c60275e, shape f43ae82f1 (G fill block 3). |
| 44 | `os/linux/wait_waitid.cs` | `os/wait_waitid.go` | touched-substantive | .auto differential | 09-07 scope-rule §4 MOVED-WITHIN-PACKAGE; 09-07 BOTH §4: REMOVED-only, not a collision; 09-08 date-screen §3: base GENUINELY STALE | — | — | — | — |
| 45 | `os/tempfile_impl.cs` | `os/tempfile.go` | untouched | principal .auto — `os/tempfile.cs` | — | — | — | — | — |
| 46 | `os/user/windows/lookup_windows_impl.cs` | `os/user/lookup_windows.go` | touched-substantive | principal .auto — `os/user/windows/lookup_windows.cs` | §10 MEMBERS-ADDED → RE-DERIVE | 3cf0257b852592ce15f570e6c8ab4842b969c5f9a2c7e175549fda6607840c57 | af4a056de467ae0005f1ed42c0a8b05b06889929aa2978836f38938a24dfb192 | c | companion-vs-principal · REWRITE OWED: one of the companion's three hand-converted members changed upstream. Go `listGroupsForUsernameAndDomain` body differs at 1.24.13 (45 -> 41 lines): an empty NetUserGetLocalGroups result now returns (nil, nil), and the "None"-group comment is gone; the companion (lookup_windows_impl.cs:291-292) still returns the 1.23 error for entriesRead == 0 and folds a null published buffer into that same error. Owed: take 1.24's empty-list result while keeping a null buffer an error. The other two hand-converted members are body-identical at both releases (lookupFullNameServer, lookupUserPrimaryGroup); the emitted-side changes count 0 in the companion, whole words: isServiceAccount isValidUserAccountType isValidGroupAccountType lookupUsernameAndDomain sidType runAsProcessOwner getCurrentToken newUserFromSid current listGroups SidTypeWellKnownGroup = 0 (control lookupUserPrimaryGroup = 3). Work item: BOARD, owner C1, EXPLICITLY DEFERRED (behavioural, off the H5 critical path), ruled COORD d36cea91d: a hand-own re-derive on the version branch after row 20, one commit; acceptance os/user's own tests at the gate tree on i9, the empty-groups case named, the null-buffer guard kept and stated. Sides: 1.23.12 `os/user/windows/lookup_windows.cs` vs 1.24.13 `os/user/windows/lookup_windows.cs`, windows-amd64. Rules 8808a00ad, 80c948a7f, identity f6c60275e (G fill block 2, amended in block 3). |
| 47 | `os/windows/dir_windows_impl.cs` | `os/dir_windows.go` | untouched | principal .auto — `os/windows/dir_windows.cs` | — | 9918b417feb1e85736445ba04a07079ea1896942c447759bf3b26c2315d20bde | ae128b9b984f50fb03fc7d297051edde53064819ec8186aa62da178a52018132 | b | companion-vs-principal · EMISSION-ONLY: Go principal byte-identical at 1.23.12 and 1.24.13 (sha256 bed7533115f62133558144ff8e3e5944a8c6f02338b4fbd29f453d85f6145a8c = bed7533115f62133558144ff8e3e5944a8c6f02338b4fbd29f453d85f6145a8c, `os/dir_windows.go`); the .auto delta is the converter's closure-dependent alias spelling (Δruntime->runtime), the fourth-arm class ruled 2026-09-08 (runbook H5 window ruling); the hand-own declares its own aliases at line 61. Sides: 1.23.12 `os/windows/dir_windows.cs` vs 1.24.13 `os/windows/dir_windows.cs`, windows-amd64. Rules 8808a00ad, 80c948a7f, identity f6c60275e, shape f43ae82f1 (G fill block 1, amended in block 2). |
| 48 | `os/windows/file_windows_impl.cs` | `os/file_windows.go` | touched-substantive | principal .auto — `os/windows/file_windows.cs` | §10 MEMBERS-REMOVED → RE-DERIVE | 948fba8975aedeb92a6a707d275a3b62484b46cfe34e9e045dedc0e31258e0c7 | 28e5c9ede374d64d73d57566761dfa1ccfbbc822b3d846f45664549ace230650 | c | companion-vs-principal · REWRITE OWED: an upstream split reaches the body this hand-own exists to replace. 1.24.13 splits `readReparseLink(path)` into `openSymlink` + `readReparseLinkHandle(h)`, and the new os.Root code calls `readReparseLinkHandle` directly (Go os/root_windows.go:176 and :221; emitted os/windows/root_windows.cs:180 and :230). The emitted `readReparseLinkHandle` (os/windows/file_windows.cs:451) reinterprets the buffer as REPARSE_DATA_BUFFER / SymbolicLinkReparseBuffer / MountPointReparseBuffer, the cast this companion's header records as the ACCESS_VIOLATION that killed the test host; the companion hand-converts only readReparseLink (`readReparseLinkHandle` = 0, whole words; control readReparseLink = 3), whose own Go body also differs (29 -> 8 lines). Owed: hand-convert readReparseLinkHandle on the companion's byte-offset decode and register it beside readReparseLink. Also in the delta, emitted and not the companion's: openFileNolog loses its EISDIR mapping (now inside syscall.Open), tempDir moves to sync.OnceValue (openFileNolog tempDir useGetTempPath2 EISDIR = 0). Work item: BOARD, owner C1, EXPLICITLY DEFERRED (behavioural, off the H5 critical path; live at 1.24 through os.Root, not latent), ruled COORD d36cea91d: a hand-own re-derive on the version branch after row 20, one commit; acceptance os TestReadlink and the os.Root tests at the gate tree on i9, the emitted readReparseLinkHandle displaced and named by the registry guard. Sides: 1.23.12 `os/windows/file_windows.cs` vs 1.24.13 `os/windows/file_windows.cs`, windows-amd64. Rules 8808a00ad, 80c948a7f, identity f6c60275e (G fill block 2, amended in block 3). |
| 49 | `reflect/deepequal_impl.cs` | `reflect/deepequal.go` | touched-trivial | principal .auto — `reflect/deepequal.cs` | — | — | — | — | — |
| 50 | `reflect/makefunc_impl.cs` | `reflect/makefunc.go` | untouched | principal .auto — `reflect/makefunc.cs` | — | — | — | — | — |
| 51 | `reflect/value_impl.cs` | `reflect/value.go` | touched-substantive | principal .auto — `reflect/value.cs` | §10 MIXED → RE-WRITE | — | — | — | — |
| 52 | `runtime/cputicks_impl.cs` | `runtime/cputicks.go` | untouched | principal .auto — `runtime/cputicks.cs` | — | — | — | — | — |
| 53 | `runtime/darwin/libccall_impl.cs` | `runtime/libccall.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 54 | `runtime/darwin/lock_sema_impl.cs` | `runtime/lock_sema.go` | touched-substantive | principal .auto — `runtime/darwin/lock_sema.cs` | §10 MIXED → RE-DERIVE | 634dc27a4c100c25f4c7a35459e5c9abac076cb97e945b834f54b5208042eb71 | 4c86103a7a32acc760bdffd10a5a2aeee7ba103cefc98fca752575e85ec181c5 | b | companion-vs-principal · UPSTREAM-IN-PRINCIPAL: changed members `lock`, `unlock`, `lock2`, `unlock2`, `mutexContended` and the constants active_spin / active_spin_cnt / passive_spin, all removed from lock_sema.go at 1.24.13 (the mutex moves to lock_spinbit.go, hand-owned by `runtime/lock_managed_impl.cs`, row 72); the hand-converted set in the principal goes 7 -> 4 (lock2, mutexContended, unlock2 leave) and the four that stay read BODY-IDENTICAL (notesleep, notetsleep_internal, notetsleepg, notewakeup); search predicate over the companion's whole text, whole words: lock unlock lock2 unlock2 mutexContended active_spin active_spin_cnt passive_spin = 0 (control notetsleep_internal = 3; this companion is byte-identical to row 90's, measured by cmp). Sides: 1.23.12 `runtime/darwin/lock_sema.cs` vs 1.24.13 `runtime/darwin/lock_sema.cs`, darwin-amd64. Rules 8808a00ad, 80c948a7f, identity f6c60275e, shape f43ae82f1 (G fill block 3). |
| 55 | `runtime/darwin/nanotime_impl.cs` | `runtime/nanotime.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 56 | `runtime/darwin/sigaction_impl.cs` | `runtime/sigaction.go` | untouched | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 57 | `runtime/darwin/signal_posix_darwin_impl.cs` | `runtime/signal_posix_darwin.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 58 | `runtime/darwin/sigprocmask_impl.cs` | `runtime/sigprocmask.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 59 | `runtime/darwin/sys_darwin_signote_impl.cs` | `runtime/sys_darwin_signote.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 60 | `runtime/debug/stubs_impl.cs` | `runtime/debug/stubs.go` | untouched | principal .auto — `runtime/debug/stubs.cs` | — | — | — | — | — |
| 61 | `runtime/goargs_impl.cs` | `runtime/goargs.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | §10 minted shell → RE-DERIVE | — | — | — | — |
| 62 | `runtime/goenvs_impl.cs` | `runtime/goenvs.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | §10 minted shell → RE-WRITE | — | — | — | — |
| 63 | `runtime/hash_impl.cs` | `runtime/hash.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 64 | `runtime/hostofrecord_impl.cs` | `runtime/hostofrecord.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | §10 minted shell → RE-DERIVE | — | — | — | — |
| 65 | `runtime/linux/lock_futex_impl.cs` | `runtime/lock_futex.go` | touched-substantive | principal .auto — `runtime/linux/lock_futex.cs` | §10 MIXED → RE-WRITE | 9472c734001e43da535d2eafbf672a3b9ebf2ef0e5b13bb89f95ae12cda328f2 | 186976c17d4e0fe6e1b252e8533a682774b1c2d695fb85f6cd1badb774dc936a | b | companion-vs-principal · UPSTREAM-IN-PRINCIPAL: changed members `lock`, `unlock`, `lock2`, `unlock2`, `mutexContended` and the constants mutex_unlocked / mutex_locked / mutex_sleeping / active_spin / active_spin_cnt / passive_spin, removed from lock_futex.go at 1.24.13 (the mutex moves to lock_spinbit.go, hand-owned by `runtime/lock_managed_impl.cs`, row 72), and the new futex semaphores `semacreate` / `semasleep` / `semawakeup` over the new `m.waitsema` (emitted, reached only from lock_spinbit's placeholders); search predicate over the companion's whole text, whole words: lock unlock lock2 unlock2 mutexContended mutex_unlocked mutex_locked mutex_sleeping active_spin active_spin_cnt passive_spin semacreate semasleep semawakeup waitsema = 4, every hit a header comment (lines 11 and 18), none a reference to the member (control notetsleep_internal = 3); member bodies: the hand-converted set goes 7 -> 4 (lock2, mutexContended, unlock2 leave) and the four that stay read BODY-IDENTICAL (notesleep, notetsleep_internal, notetsleepg, notewakeup). Sides: 1.23.12 `runtime/linux/lock_futex.cs` vs 1.24.13 `runtime/linux/lock_futex.cs`, linux-amd64. Rules 8808a00ad, 80c948a7f, identity f6c60275e, shape f43ae82f1, linux ruling 8b2cbccb4 (G fill block 4). |
| 66 | `runtime/linux/mem_linux_impl.cs` | `runtime/mem_linux.go` | untouched | principal .auto — `runtime/linux/mem_linux.cs` | — | — | — | — | — |
| 67 | `runtime/linux/nanotime_impl.cs` | `runtime/nanotime.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 68 | `runtime/linux/os_linux_impl.cs` | `runtime/os_linux.go` | touched-substantive | principal .auto — `runtime/linux/os_linux.cs` | §10 SIGNATURE → RE-DERIVE | 629770f35e527b033de94665623e27e005cc3bb9e3dadbe555dd5f5b6d040074 | ec5a0f345afbac2d132b41952340ec6f4a011c407cdf728ec47ca4ebab942ed6 | b | companion-vs-principal · UPSTREAM-IN-PRINCIPAL: changed members the `m` fields `vgetrandomState` and `waitsema`, `vgetrandomInit()` added to osinit, `mdestroy` gaining //go:nowritebarrierrec and a corrected comment, and comment-only edits inside `sysauxv` and `readRandom`; search predicate over the companion's whole text, whole words: vgetrandomState waitsema vgetrandomInit mdestroy readRandom = 0, and sysauxv archauxv vdsoauxv osinit = 8, every hit a header comment (lines 8-31) describing the osinit path the companion stands in for, none a reference (control parseHugePageSize = 3); member bodies: 0 placeholders in the principal, so the Go function the companion realises was compared by name: getHugePageSize BODY-IDENTICAL (23 lines); sysauxv's Go body differs by comment lines only. Sides: 1.23.12 `runtime/linux/os_linux.cs` vs 1.24.13 `runtime/linux/os_linux.cs`, linux-amd64. Rules 8808a00ad, 80c948a7f, identity f6c60275e, shape f43ae82f1, linux ruling 8b2cbccb4 (G fill block 4). |
| 69 | `runtime/linux/signal_posix_impl.cs` | `runtime/signal_posix.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 70 | `runtime/linux/sigprocmask_impl.cs` | `runtime/sigprocmask.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 71 | `runtime/linux/trace_impl.cs` | `runtime/trace.go` | touched-substantive | principal .auto — `runtime/linux/trace.cs` | — | 7fc96901cd7315670cff4d8f326e2c19177b1f35cc9a6a6edf70fa0c4f9efdf9 | cc2ee56c48976bda63c56cc325aca56f249b24ac7f8a56a5ff37040bbeba646d | b | companion-vs-principal · UPSTREAM-IN-PRINCIPAL: changed members `goBlockReasons` (16 -> 17), the `traceBufFlush` loop over the per-M trace buffers now indexed per experiment (`mTraceState.buf`), and `lockInit` in `newWakeableSleep` taking the lock pointer; search predicate over the companion's whole text, whole words: goBlockReasons traceBufFlush mTraceState buf lockInit wakeableSleep newWakeableSleep traceThreadDestroy = 0 (control StartTrace = 10; this companion is byte-identical to row 94's, measured by cmp); member bodies: StartTrace, StopTrace BODY-IDENTICAL. Sides: 1.23.12 `runtime/linux/trace.cs` vs 1.24.13 `runtime/linux/trace.cs`, linux-amd64. Rules 8808a00ad, 80c948a7f, identity f6c60275e, shape f43ae82f1, linux ruling 8b2cbccb4 (G fill block 4). |
| 72 | `runtime/lock_managed_impl.cs` | `runtime/lock_managed.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 73 | `runtime/managed_impl.cs` | `runtime/managed.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 74 | `runtime/mbitmap_impl.cs` | `runtime/mbitmap.go` | touched-substantive | principal .auto — `runtime/mbitmap.cs` | §10 MEMBERS-REMOVED → RE-WRITE | — | — | — | RE-POINT |
| 75 | `runtime/mcleanup.cs` | `runtime/mcleanup.go` (absent at go1.23.12) | touched-substantive | .auto differential | — | — | — | — | — |
| 76 | `runtime/mem_persistent_impl.cs` | `runtime/mem_persistent.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 77 | `runtime/metrics/sample.cs` | `runtime/metrics/sample.go` | untouched | .auto differential | — | — | — | — | — |
| 78 | `runtime/mfinal.cs` | `runtime/mfinal.go` | touched-substantive | .auto differential | 09-07 scope-rule §5 RE-DERIVE | — | — | — | — |
| 79 | `runtime/mranges_impl.cs` | `runtime/mranges.go` | untouched | principal .auto — `runtime/mranges.cs` | — | — | — | — | — |
| 80 | `runtime/netpoll_impl.cs` | `runtime/netpoll.go` | touched-substantive | principal .auto — `runtime/netpoll.cs` | — | — | — | — | — |
| 81 | `runtime/panic_impl.cs` | `runtime/panic.go` | touched-substantive | principal .auto — `runtime/panic.cs` | — | 39337c1a137d424498fa27ada0a7a586c12f4c1f51efcef8b06bd0ea2059843d | c8f6f9647b5c22ea09f25b9d5754a00dadedb83978be4a8297816bff9d44483f | b | companion-vs-principal · NOT-APPLICABLE-TO-MANAGED (proposed `b` reason, block 6): the member-body arm names `fatal` (panic_impl.cs:77, THIS companion) as BODY-DIFFERS; its Go delta is `printlock()` before and `printunlock()` after the report (issue 69447: multiple fatal reports must not interleave); the managed fatal calls FatalReport.Fatal (golib/runtime/FatalReport.cs:126), which builds the complete report as one string, writes it in ONE Console.Error.Write (line 132) and exits the process with status 2, so there is no multi-write report of its own to interleave -- this rests on documented .NET behaviour (Console's writers are synchronized), not on a measurement taken here. `throw` BODY-IDENTICAL. `Goexit` is also BODY-DIFFERS in this principal, but it is declared in runtime/managed_impl.cs:529 (row 73), not in this companion, so row 81 does not carry it. Also in the pair, emitted: 76 lines of getcallerpc/getcallersp -> sys.GetCallerPC/sys.GetCallerSP, the sys import move, and six new linkname wrappers (rand_fatal, sysrand_fatal, fips_fatal, maps_fatal, internal_sync_throw, internal_sync_fatal) that call this companion's own fatal/throw; search predicate over the companion's whole text, whole words: printlock printunlock getcallerpc getcallersp GetCallerPC GetCallerSP panicCheck1 deferreturn = 8, every hit a header comment (getcallerpc at lines 13, 14, 17, 22, 25, 27; getcallersp at lines 14, 27), none a reference (control FatalReport = 3). Sides: 1.23.12 `runtime/panic.cs` vs 1.24.13 `runtime/panic.cs`, identical on all three targets. Rules 8808a00ad, 80c948a7f, identity f6c60275e, arm 8cf7fdf65 (G fill block 6). |
| 82 | `runtime/panicvalues_impl.cs` | `runtime/panicvalues.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | §10 minted shell → RE-DERIVE | — | — | — | — |
| 83 | `runtime/pinner_impl.cs` | `runtime/pinner.go` | touched-substantive | principal .auto — `runtime/pinner.cs` | — | 45f0629bc26d103d12707b8d4016bb52d89108e34d3dc7c216ffff6c487b91c8 | 2303a61a6e42a7913eff9c148f6ee85c46120b966cf3055f2331ec41356810e0 | b | companion-vs-principal · UPSTREAM-IN-PRINCIPAL: changed member the `special.offset` field's type (the emitted `rec.special.offset = (uint16)offset` loses its cast; the Go principal `runtime/pinner.go` changes in 2 code lines); search predicate over the companion's whole text, whole words: offset = 0, special = 1, a comment at line 265 ("a pointer into the special"), not the field (control pinnerGetPinCounter, declared at line 267); member bodies: Pin, Unpin, isPinned, pinnerGetPinCounter BODY-IDENTICAL. Sides: 1.23.12 `runtime/pinner.cs` vs 1.24.13 `runtime/pinner.cs`, identical on all three targets. Rules 8808a00ad, 80c948a7f, identity f6c60275e, shape f43ae82f1, arm 8cf7fdf65 (G fill block 5). |
| 84 | `runtime/pprof/pprof_impl.cs` | `runtime/pprof/pprof.go` | touched-substantive | principal .auto — `runtime/pprof/pprof.cs` | — | — | — | — | — |
| 85 | `runtime/pprof/proflabel_impl.cs` | `runtime/pprof/proflabel.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 86 | `runtime/pprof/symtab_impl.cs` | `runtime/pprof/symtab.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 87 | `runtime/runtime2.cs` | `runtime/runtime2.go` | touched-substantive | .auto differential | 09-07 scope-rule §4 MOVED-WITHIN-PACKAGE, §5 RE-WRITE; 09-07 sync-collision: collision bill row 1; 09-07 BOTH §4: RE-DERIVED (ruling bd868d3fe) | — | — | — | — |
| 88 | `runtime/runtime2_impl.cs` | `runtime/runtime2.go` | touched-substantive | principal .auto — `runtime/runtime2.cs` | 09-07 scope-rule §4 MOVED-WITHIN-PACKAGE (mechanical) | — | — | — | — |
| 89 | `runtime/stubs_impl.cs` | `runtime/stubs.go` | touched-substantive | principal .auto — `runtime/stubs.cs` | §10 MEMBERS-REMOVED → RE-WRITE | — | — | — | — |
| 90 | `runtime/windows/lock_sema_impl.cs` | `runtime/lock_sema.go` | touched-substantive | principal .auto — `runtime/windows/lock_sema.cs` | §10 MIXED → RE-DERIVE | accef100f9433e0792894ac5b04c1e6928a62c8d3a285c875eae539e5ea75d04 | 48f354d110cdc6799ac570e07a35ddffefc300b5185e38290bb6e5cbb7714d4b | b | companion-vs-principal · UPSTREAM-IN-PRINCIPAL: changed members `lock`, `unlock`, `lock2`, `unlock2`, `mutexContended` and the constants active_spin / active_spin_cnt / passive_spin, all removed from lock_sema.go at 1.24.13 (Go moves the mutex to lock_spinbit.go; their placeholders now sit in runtime/{windows,linux,darwin}/lock_spinbit.cs and they are hand-owned by `runtime/lock_managed_impl.cs`, row 72, which already implements 1.24's unlock2Wake); the companion's one member notetsleep_internal keeps its placeholder in lock_sema.cs at both releases; search predicate over the companion's whole text, whole words: lock unlock lock2 unlock2 mutexContended active_spin active_spin_cnt passive_spin = 0 (control notetsleep_internal = 3). Sides: 1.23.12 `runtime/windows/lock_sema.cs` vs 1.24.13 `runtime/windows/lock_sema.cs`, windows-amd64. Rules 8808a00ad, 80c948a7f, identity f6c60275e, shape f43ae82f1 (G fill block 2). |
| 91 | `runtime/windows/nanotime_impl.cs` | `runtime/nanotime.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 92 | `runtime/windows/os_windows_impl.cs` | `runtime/os_windows.go` | touched-substantive | principal .auto — `runtime/windows/os_windows.cs` | — | ef8c44ace0e3af4d1314a91dcab1e481d1ac27bc624d4bea2126257a101b39b7 | 004ab13961752f12809ac30c78ad27d13c04c8f7eb2d7eff16cbb2a38153a2b8 | b | companion-vs-principal · UPSTREAM-IN-PRINCIPAL: changed members `mdestroy` (gains //go:nowritebarrierrec and a corrected comment) and the libcall profiler site (`getcallerpc`/`getcallersp` -> `sys.GetCallerPC`/`sys.GetCallerSP`, with a new `sys` using); search predicate over the companion's whole text, whole words: mdestroy getcallerpc getcallersp GetCallerPC GetCallerSP libcall libcallpc libcallsp nowritebarrierrec = 0 (control GoManualConversion = 1). Sides: 1.23.12 `runtime/windows/os_windows.cs` vs 1.24.13 `runtime/windows/os_windows.cs`, windows-amd64. Rules 8808a00ad, 80c948a7f, identity f6c60275e, shape f43ae82f1 (G fill block 1, amended in block 2). |
| 93 | `runtime/windows/signal_windows_impl.cs` | `runtime/signal_windows.go` | touched-substantive | principal .auto — `runtime/windows/signal_windows.cs` | — | dfee901640d3e229ce29cd5d763bd148572efaf7a874bb8ecf35bdbb2a486fc9 | bfd7840544b4e891e95c933f98e453738471edc84a3e841bed3ff6039201ea8c | b | companion-vs-principal · UPSTREAM-IN-PRINCIPAL: changed member the `sys` import (runtime/internal/sys -> internal/runtime/sys); search predicate over the companion's whole text, whole words: sys sys_package = 0 (control GoManualConversion = 1). Sides: 1.23.12 `runtime/windows/signal_windows.cs` vs 1.24.13 `runtime/windows/signal_windows.cs`, windows-amd64. Rules 8808a00ad, 80c948a7f, identity f6c60275e, shape f43ae82f1 (G fill block 1, amended in block 2). |
| 94 | `runtime/windows/trace_impl.cs` | `runtime/trace.go` | touched-substantive | principal .auto — `runtime/windows/trace.cs` | — | 183b001f0c7ced76c498fe99b2b0a678319366b3ac42136476e4f65a874b6598 | 16593a5bcf1e5ee7645dca6112784b8d06fc297ce606ea81db0511d03b388048 | b | companion-vs-principal · UPSTREAM-IN-PRINCIPAL: changed members `goBlockReasons` (16 -> 17), the `traceBufFlush` loop over the per-M trace buffers now indexed per experiment (`mTraceState.buf`), and `lockInit` in `newWakeableSleep` taking the lock pointer; search predicate over the companion's whole text, whole words: goBlockReasons traceBufFlush mTraceState buf lockInit wakeableSleep newWakeableSleep traceThreadDestroy = 0 (control StartTrace = 10). Sides: 1.23.12 `runtime/windows/trace.cs` vs 1.24.13 `runtime/windows/trace.cs`, windows-amd64. Rules 8808a00ad, 80c948a7f, identity f6c60275e, shape f43ae82f1 (G fill block 1, amended in block 2). |
| 95 | `slices/slices_impl.cs` | `slices/slices.go` | touched-substantive | principal .auto — `slices/slices.cs` | — | aefb4a910c8fa121326ced09e0b1fae2a02b404799eba18a047853502027b3db | fb3b351d218ed67973da1422fefe6b464a28a96dd53c429a37590d95b1b93fd2 | b | companion-vs-principal · UPSTREAM-IN-PRINCIPAL: changed members `Clone` (nil preserved explicitly; no s[:0:0], go.dev/issue/68488) and `Repeat` (bits.Mul hoisted; make from lo), both emitted, plus doc comments on Insert, Grow and Concat; search predicate over the companion's whole text, whole words: Clone Repeat Insert Grow Concat = 2, both comments (Insert at lines 12 and 16), none a reference (control overlaps = 4); member bodies: the one hand-converted member, overlaps (slices_impl.cs:36), differs in its Go body by ONE comment line only (crypto/internal/alias -> crypto/internal/fips140/alias in a see-also). Sides: 1.23.12 `slices/slices.cs` vs 1.24.13 `slices/slices.cs`, identical on all three targets. Rules 8808a00ad, 80c948a7f, identity f6c60275e, shape f43ae82f1, arm 8cf7fdf65 (G fill block 6). |
| 96 | `sync/atomic/type.cs` | `sync/atomic/type.go` | touched-trivial | .auto differential | — | 50e22797a64314f524b93968a070280fd9c7470d8abaa89feb9d8973626e52fc | 01b631b29013da03c1ac8bf9a0fceb82b7bcfff5e69cad47399c34e409432300 | b | auto-differential · COMMENT-ONLY: the Go principal differs at 1.24.13 in comment lines only (2 changed lines: 2 comment, 0 directive (//go:), 0 code, 0 blank; classed by diff --strip-trailing-cr, first non-space characters // and not //go:; Uintptr.Or's doc in `sync/atomic/type.go` now says it returns the old value); the .auto delta is that one comment line; nothing executable changed. Sides: 1.23.12 `sync/atomic/type.cs.auto` vs 1.24.13 `sync/atomic/type.cs.auto`, identical on all three targets. Rules 8808a00ad, 80c948a7f, identity f6c60275e, shape 999d5c784 (G fill block 5, re-shaped in block 6). |
| 97 | `sync/atomic/value.cs` | `sync/atomic/value.go` | untouched | .auto differential | — | — | — | — | — |
| 98 | `sync/cond_impl.cs` | `sync/cond.go` | untouched | principal .auto — `sync/cond.cs` | — | 17650b7ea3309d335aae11b000715015fd2f7f60cdeac8c8ae9d7952803928d6 | 9f826f7ae8e434fdc3072f57be430aee20039016ff09304085374d9503cd2703 | b | companion-vs-principal · EMISSION-ONLY: Go principal byte-identical at 1.23.12 and 1.24.13 (sha256 324314fd3a484897ce540a242f9de85d00407040c8cd2ee7db5d5761f5d14d4f = 324314fd3a484897ce540a242f9de85d00407040c8cd2ee7db5d5761f5d14d4f, `sync/cond.go`); the .auto delta is the converter's closure-dependent alias spelling (sync/atomic re-qualified, `using go.sync`), the fourth-arm class ruled 2026-09-08 (runbook H5 window ruling); the hand-own has no such site (no atomic alias, no sync using); member bodies: check BODY-IDENTICAL. Sides: 1.23.12 `sync/cond.cs` vs 1.24.13 `sync/cond.cs`, identical on all three targets. Rules 8808a00ad, 80c948a7f, identity f6c60275e, shape f43ae82f1, arm 8cf7fdf65 (G fill block 5). |
| 99 | `sync/mutex.cs` | `sync/mutex.go` | touched-substantive | .auto differential | §10 MIXED → RE-WRITE; 09-07 sync-collision: RE-WRITE (collision bill row 2); 09-07 BOTH §4: TWO deletions | — | — | — | — |
| 100 | `sync/once.cs` | `sync/once.go` | touched-substantive | .auto differential | §10 SIGNATURE → RE-DERIVE; 09-08 date-screen §3: base GENUINELY STALE | — | — | — | — |
| 101 | `sync/oncefunc.cs` | `sync/oncefunc.go` | untouched | .auto differential | — | — | — | — | — |
| 102 | `sync/pool.cs` | `sync/pool.go` | untouched | .auto differential | — | — | — | — | — |
| 103 | `sync/poolqueue.cs` | `sync/poolqueue.go` | untouched | .auto differential | — | d57e88baac798f3f86312c420875572b195e0cd691e24889dcd3fd1c794d563a | 289b8535ad692dc8b4517940aa4a23e8668eefe0a7ea088cdc654def209c29ef | b | auto-differential · EMISSION-ONLY: Go principal byte-identical at 1.23.12 and 1.24.13 (sha256 25b900ffeb803a9ecd4c1926a5faaa3ba8d7b4adcd71bbfbd373cb8606910dae = 25b900ffeb803a9ecd4c1926a5faaa3ba8d7b4adcd71bbfbd373cb8606910dae, `sync/poolqueue.go`); the .auto delta is the converter's closure-dependent alias spelling (sync/atomic re-qualified, `using go.sync`), the fourth-arm class ruled 2026-09-08 (runbook H5 window ruling); the hand-own declares its own aliases at lines 28-29 (`using go.sync;`, `using atomic = go.sync.atomic_package;`). Sides: 1.23.12 `sync/poolqueue.cs.auto` vs 1.24.13 `sync/poolqueue.cs.auto`, identical on all three targets. Rules 8808a00ad, 80c948a7f, identity f6c60275e, shape f43ae82f1 (G fill block 5). |
| 104 | `sync/runtime_impl.cs` | `sync/runtime.go` | touched-substantive | principal .auto — `sync/runtime.cs` | §10 MEMBERS-REMOVED → RE-WRITE | — | — | — | — |
| 105 | `sync/rwmutex.cs` | `sync/rwmutex.go` | touched-substantive | .auto differential | 09-08 date-screen §3: base GENUINELY STALE | — | — | — | — |
| 106 | `sync/waitgroup.cs` | `sync/waitgroup.go` | touched-substantive | .auto differential | — | — | — | — | — |
| 107 | `syscall/darwin/exec_libc2_impl.cs` | `syscall/exec_libc2.go` | untouched | principal .auto — `syscall/darwin/exec_libc2.cs` | — | ea3132afa2af16d1ca78a7ed8803002c8ea6e12c69e402f316ebc43ae5cc36b9 | 85927ed2a041bef8b26f3776b2bb39aa94fe574b23e610a6b4a0b10e7b8e027b | b | companion-vs-principal · EMISSION-ONLY: Go principal byte-identical at 1.23.12 and 1.24.13 (sha256 7cf1de2b168d8e1d2cef0063149dd52df80264c11cb61c5143b66e160f847f23 = 7cf1de2b168d8e1d2cef0063149dd52df80264c11cb61c5143b66e160f847f23, `syscall/exec_libc2.go`); the .auto delta is the converter's closure-dependent alias spelling (Δruntime->runtime), the fourth-arm class ruled 2026-09-08 (runbook H5 window ruling); the hand-own has no such site (0 `Δruntime`, 0 `runtime.GOOS`). Sides: 1.23.12 `syscall/darwin/exec_libc2.cs` vs 1.24.13 `syscall/darwin/exec_libc2.cs`, darwin-amd64. Rules 8808a00ad, 80c948a7f, identity f6c60275e, shape f43ae82f1 (G fill block 3). |
| 108 | `syscall/darwin/sockaddr_darwin_impl.cs` | `syscall/sockaddr_darwin.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 109 | `syscall/darwin/syscall_darwin_impl.cs` | `syscall/syscall_darwin.go` | untouched | principal .auto — `syscall/darwin/syscall_darwin.cs` | — | — | — | — | — |
| 110 | `syscall/linux/cgocaller_linux_impl.cs` | `syscall/cgocaller_linux.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 111 | `syscall/linux/exec_unix.cs` | `syscall/exec_unix.go` | untouched | .auto differential | 09-08 date-screen §3: base GENUINELY STALE | — | — | — | — |
| 112 | `syscall/linux/sockaddr_linux_impl.cs` | `syscall/sockaddr_linux.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 113 | `syscall/linux/structclass_linux_impl.cs` | `syscall/structclass_linux.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 114 | `syscall/linux/syscall_linux_amd64_impl.cs` | `syscall/syscall_linux_amd64.go` | touched-substantive | principal .auto — `syscall/linux/syscall_linux_amd64.cs` | §10 MEMBERS-REMOVED → RE-DERIVE | 43ca73ccee74a1692e1ec6f58216582beafd78d8a8c9460cf0b52f0d4ac69fc0 | 7e9190affe7dbf818f21a0ce5c0289d93d10d6a12b6860eb9415bf5f8606bc5b | b | companion-vs-principal · UPSTREAM-IN-PRINCIPAL: changed members the //sysnb declarations `Getrlimit` and `setrlimit` and the emitted `rawSetrlimit`, removed at 1.24.13 (Getrlimit/setrlimit move to syscall_linux.go over prlimit1, row 115), and the `unsafe` using; search predicate over the companion's whole text, whole words: Getrlimit setrlimit rawSetrlimit SYS_SETRLIMIT RawSyscall unsafe = 0 (control gettimeofday = 6); member bodies: 0 placeholders in the principal; the Go function the companion realises is the body-less assembly stub `func gettimeofday(tv *Timeval) (err Errno)`, whose declaration is identical at both releases. Sides: 1.23.12 `syscall/linux/syscall_linux_amd64.cs` vs 1.24.13 `syscall/linux/syscall_linux_amd64.cs`, linux-amd64. Rules 8808a00ad, 80c948a7f, identity f6c60275e, shape f43ae82f1, linux ruling 8b2cbccb4 (G fill block 4). |
| 115 | `syscall/linux/syscall_linux_impl.cs` | `syscall/syscall_linux.go` | touched-substantive | principal .auto — `syscall/linux/syscall_linux.cs` | — | b4575821912e8447eca7c06accfb2a05845da5c8b3ec86c026dfb239892756aa | dc072504a2cc20ef8aa61738966e6cd63b0827958cc0f40faf27b4d8067340b4 | b | companion-vs-principal · UPSTREAM-IN-PRINCIPAL: changed members the new `Accept` (calling the hand-converted Accept4), `Getrlimit` and `setrlimit` now over `prlimit1`, and the converter's alias Δruntime->runtime at the faccessat2 GOOS check; search predicate over the companion's whole text, whole words: Accept Accept4 anyToSockaddr Getrlimit setrlimit prlimit prlimit1 faccessat2 Δruntime = 0, and runtime = 8, seven of them comment lines (11, 13, 15, 24, 88, 92, 93) and one the namespace path `@internal.runtime.syscall_package` at line 84, none the alias (control runtime_entersyscall = 4); member bodies: 11 -> 11, all BODY-IDENTICAL (Accept4, Getsockname, GetsockoptICMPv6Filter, GetsockoptIPMreq, GetsockoptIPMreqn, GetsockoptIPv6Mreq, Setgroups, SetsockoptIPMreqn, anyToSockaddr, recvmsgRaw, sockaddr). Sides: 1.23.12 `syscall/linux/syscall_linux.cs` vs 1.24.13 `syscall/linux/syscall_linux.cs`, linux-amd64. Rules 8808a00ad, 80c948a7f, identity f6c60275e, shape f43ae82f1, linux ruling 8b2cbccb4 (G fill block 4). |
| 116 | `syscall/linux/zsyscall_linux_amd64_impl.cs` | `syscall/zsyscall_linux_amd64.go` | touched-substantive | principal .auto — `syscall/linux/zsyscall_linux_amd64.cs` | 09-07 scope-rule §4 MOVED-WITHIN-PACKAGE (mechanical) | 49956ab1f1907ee4acdaeb03cb3c1078baa989f1d0826f5ea756d9ea6a6226ea | de9999e9161f89a421fb5ad819f4e76fcbfc34c6f5e9e9757c6985afc8bc5905 | b | companion-vs-principal · UPSTREAM-IN-PRINCIPAL: changed members the generated `Getrlimit` and `setrlimit` wrappers, removed at 1.24.13 (moved to syscall_linux.go over prlimit1), plus 194 lines of the converter's `ᴋN` pin-temporary renumbering (emission-only); search predicate over the companion's whole text, whole words: Getrlimit setrlimit prlimit1 = 0 (control Fstat = 6); member bodies: 9 -> 9, all BODY-IDENTICAL (Adjtimex, Fstat, Fstatfs, Select, Statfs, Sysinfo, Uname, fstatat, wait4). Sides: 1.23.12 `syscall/linux/zsyscall_linux_amd64.cs` vs 1.24.13 `syscall/linux/zsyscall_linux_amd64.cs`, linux-amd64. Rules 8808a00ad, 80c948a7f, identity f6c60275e, shape f43ae82f1, linux ruling 8b2cbccb4 (G fill block 4). |
| 117 | `syscall/syscall_impl.cs` | `syscall/syscall.go` | untouched | principal .auto — `syscall/{windows,linux,darwin}/syscall.cs` | — | — | — | — | — |
| 118 | `syscall/windows/dll_windows.cs` | `syscall/dll_windows.go` | untouched | .auto differential | 09-08 date-screen §3: base GENUINELY STALE | — | — | — | — |
| 119 | `syscall/windows/exec_windows.cs` | `syscall/exec_windows.go` | untouched | .auto differential | 09-08 date-screen §3: base GENUINELY STALE | f8524bf78b2e7037ac9175849656905f79e13fb0c84ec7c77341a42d245da347 | 4f21e301ed808594fb7d19746307235d1f0282dc6bafc6ab4c536bf98e573304 | b | auto-differential · EMISSION-ONLY: Go principal byte-identical at 1.23.12 and 1.24.13 (sha256 9f24d0257b0969fdb67cf3bf479a9c28a2289a002b8100edc6aa5834ccf501f2 = 9f24d0257b0969fdb67cf3bf479a9c28a2289a002b8100edc6aa5834ccf501f2, `syscall/exec_windows.go`); the .auto delta is the converter's closure-dependent alias spelling (Δruntime->runtime), the fourth-arm class ruled 2026-09-08 (runbook H5 window ruling); the hand-own has no such site (0 `runtime` alias, 0 `KeepAlive`). Sides: 1.23.12 `syscall/windows/exec_windows.cs.auto` vs 1.24.13 `syscall/windows/exec_windows.cs.auto`, windows-amd64. Rules 8808a00ad, 80c948a7f, identity f6c60275e, shape f43ae82f1 (G fill block 1, amended in block 2). |
| 120 | `syscall/windows/security_windows.cs` | `syscall/security_windows.go` | untouched | .auto differential | 09-08 date-screen §3: base GENUINELY STALE | — | — | — | — |
| 121 | `syscall/windows/syscall_windows_callback_impl.cs` | `syscall/syscall_windows_callback.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 122 | `syscall/windows/syscall_windows_impl.cs` | `syscall/syscall_windows.go` | touched-substantive | principal .auto — `syscall/windows/syscall_windows.cs` | — | 1606ed327c4ce0d005ff4341344d6a145b0443b950d085094d784796867d05ad | acb416c860a1d7c457bdae2da2e1b1dd18fb259033ec3341525904978b381bbc | b | companion-vs-principal · UPSTREAM-IN-PRINCIPAL: changed members `Open` (rewritten on the new createFile: O_TRUNC by Ftruncate after opening, FILE_FLAG_BACKUP_SEMANTICS for directory opens, the EISDIR mapping moved in), `Ftruncate` (now one setFileInformationByHandle call), a new exported `CreateFile` wrapper, the //sys renames createFile / setFileInformationByHandle, and the converter's alias Δruntime->runtime; search predicate over the companion's whole text, whole words: Open createFile CreateFile Ftruncate setFileInformationByHandle Seek SetEndOfFile makeInheritSa FILE_FLAG_BACKUP_SEMANTICS O_TRUNC TRUNCATE_EXISTING CREATE_ALWAYS = 0 (control Getsockname = 6). Sides: 1.23.12 `syscall/windows/syscall_windows.cs` vs 1.24.13 `syscall/windows/syscall_windows.cs`, windows-amd64. Rules 8808a00ad, 80c948a7f, identity f6c60275e, shape f43ae82f1 (G fill block 2). |
| 123 | `syscall/windows/zsyscall_windows_addrinfo_impl.cs` | `syscall/zsyscall_windows_addrinfo.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 124 | `syscall/windows/zsyscall_windows_certchain_impl.cs` | `syscall/zsyscall_windows_certchain.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 125 | `syscall/windows/zsyscall_windows_dnsrecord_impl.cs` | `syscall/zsyscall_windows_dnsrecord.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 126 | `syscall/windows/zsyscall_windows_impl.cs` | `syscall/zsyscall_windows.go` | touched-substantive | principal .auto — `syscall/windows/zsyscall_windows.cs` | 09-07 scope-rule §4 MOVED-WITHIN-PACKAGE (mechanical) | 311caa2fa3a676560d520536f42639835217a03f890fa8d3cd938656856ede7c | 4cfae140f6cb97fdaa90812abb9f340a4955a54308db047c63a6b1528e0d6d38 | b | companion-vs-principal · UPSTREAM-IN-PRINCIPAL: changed members `createFile` (renamed from the exported CreateFile, failing also on ERROR_ALREADY_EXISTS), the new `setFileInformationByHandle` and its proc, plus 179 lines of the converter's `ᴋN` pin-temporary renumbering (emission-only); search predicate over the companion's whole text, whole words: createFile CreateFile setFileInformationByHandle procSetFileInformationByHandle ERROR_ALREADY_EXISTS = 0 (control findFirstFile1 = 4). Sides: 1.23.12 `syscall/windows/zsyscall_windows.cs` vs 1.24.13 `syscall/windows/zsyscall_windows.cs`, windows-amd64. Rules 8808a00ad, 80c948a7f, identity f6c60275e, shape f43ae82f1 (G fill block 2). |
| 127 | `syscall/windows/zsyscall_windows_ptrout_impl.cs` | `syscall/zsyscall_windows_ptrout.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 128 | `syscall/windows/zsyscall_windows_wsa_impl.cs` | `syscall/zsyscall_windows_wsa.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | §10 minted shell → RE-DERIVE | — | — | — | — |
| 129 | `testing/PackageAncestry.cs` | `testing/PackageAncestry.go` (absent at both) | no-upstream-counterpart | manual upstream diff · OQ-11 · OQ-8 | §7 host infrastructure; ruling cb24ac747: OUT of the H6 population; 09-08 base §1: skip-listed, no base | — | — | — | — |
| 130 | `testing/TestExecution.cs` | `testing/TestExecution.go` (absent at both) | no-upstream-counterpart | manual upstream diff · OQ-11 | 09-08 base §1: skip-listed, no base | — | — | — | — |
| 131 | `testing/TestFlagBridge.cs` | `testing/TestFlagBridge.go` (absent at both) | no-upstream-counterpart | manual upstream diff · OQ-11 | 09-08 base §1: skip-listed, no base | — | — | — | — |
| 132 | `testing/TestFormat.cs` | `testing/TestFormat.go` (absent at both) | no-upstream-counterpart | manual upstream diff · OQ-11 · OQ-8 | §7 host infrastructure; ruling cb24ac747: OUT of the H6 population; 09-08 base §1: skip-listed, no base | — | — | — | — |
| 133 | `testing/TestHost.cs` | `testing/TestHost.go` (absent at both) | no-upstream-counterpart | manual upstream diff · OQ-11 | 09-08 base §1: skip-listed, no base | — | — | — | — |
| 134 | `testing/TestOptions.cs` | `testing/TestOptions.go` (absent at both) | no-upstream-counterpart | manual upstream diff · OQ-11 | 09-08 base §1: skip-listed, no base | — | — | — | — |
| 135 | `testing/TestRegistry.cs` | `testing/TestRegistry.go` (absent at both) | no-upstream-counterpart | manual upstream diff · OQ-11 | 09-08 base §1: skip-listed, no base | — | — | — | — |
| 136 | `testing/TestReporter.cs` | `testing/TestReporter.go` (absent at both) | no-upstream-counterpart | manual upstream diff · OQ-11 · OQ-8 | §7 host infrastructure; ruling cb24ac747: OUT of the H6 population; 09-08 base §1: skip-listed, no base | — | — | — | — |
| 137 | `testing/TestRunner.cs` | `testing/TestRunner.go` (absent at both) | no-upstream-counterpart | manual upstream diff · OQ-11 · OQ-8 | §7 host infrastructure; ruling cb24ac747: OUT of the H6 population; 09-08 base §1: skip-listed, no base | — | — | — | — |
| 138 | `testing/testing.cs` | `testing/testing.go` | touched-substantive | manual upstream diff · OQ-11 | §10 MEMBERS-REMOVED → RE-DERIVE; 09-07 BOTH §4: BOTH, to the testing-host bill; 09-08 base §1: skip-listed, no base | — | — | — | — |
| 139 | `time/sleep_impl.cs` | `time/sleep.go` | touched-trivial | principal .auto — `time/sleep.cs` | — | c41bbeb7e5c0894e539df8463d311a9fb50b01cf360ca476b5616da49ca9928a | 5568740c7ee9898c06cafaa8b4d1025f94a6df18bdf23409ef6fd0cd7ff7c918 | b | companion-vs-principal · COMMENT-ONLY: the Go principal differs at 1.24.13 in comment lines only (6 changed lines: 6 comment, 0 directive (//go:), 0 code, 0 blank; classed by diff --strip-trailing-cr, first non-space characters // and not //go:; the Timer.Reset doc and a typo in `time/sleep.go`); the .auto delta is those comment lines; nothing executable changed; member bodies: syncTimer BODY-IDENTICAL. Sides: 1.23.12 `time/sleep.cs` vs 1.24.13 `time/sleep.cs`, identical on all three targets. Rules 8808a00ad, 80c948a7f, identity f6c60275e, arm 8cf7fdf65, shape 999d5c784 (G fill block 5, re-shaped in block 6). |
| 140 | `time/tick.cs` | `time/tick.go` | untouched | .auto differential | 09-08 date-screen §3: base GENUINELY STALE | — | — | — | — |
| 141 | `unique/clone.cs` | `unique/clone.go` | untouched | .auto differential | — | — | — | — | — |
| 142 | `unsafe/unsafe.cs` | `unsafe/unsafe.go` | untouched | manual upstream diff · OQ-11 | 09-08 base §1: skip-listed, no base | — | — | — | — |
| 143 | `vendor/golang.org/x/crypto/internal/alias/alias_purego_impl.cs` | `vendor/golang.org/x/crypto/internal/alias/alias_purego.go` | untouched | principal .auto — `vendor/golang.org/x/crypto/internal/alias/alias_purego.cs` | — | — | — | — | — |
| 144 | `vendor/golang.org/x/net/route/darwin/sys_impl.cs` | `vendor/golang.org/x/net/route/sys.go` | untouched | principal .auto — `vendor/golang.org/x/net/route/darwin/sys.cs` | — | — | — | — | — |
| 145 | `weak/pointer.cs` | `internal/weak/pointer.go` (absent at go1.24.13) | touched-substantive | .auto differential · OQ-2 | §1 hand-owned by consequence; §2 REMOVED; §4 #1 retire with the package | — | — | — | PRINCIPAL CHANGED |

## 5. What this skeleton does not claim

- **No class, no hash, no disposition.** The instrument class is where H6 looks; the dossier pointer is
  a proposal; neither is a row's class. Every class is written from the H5 `.auto` pair.
- **No `.auto` was produced for this file.** No converter was run; `.cs.auto` presence noted below is
  the *tracked* sibling at `2e6cf71e4`, which is not the H5 pair.
- **The instrument class of a companion whose principal is not named is a name-match, not evidence**
  (OQ-5).
- **The count is not carried.** 146 is the reading at `2e6cf71e4`; the gate re-measures.
- It does not decide the population-scope, removed-principal or hand-owned-package questions below;
  it names them so they are ruled before the fill, not discovered during it.

### Open questions — named, not guessed

**OQ-1 — population scope.** The gate keys on *marked* paths; the two-populations ruling says the audit
covers *all* hand-owns. The 11 unmarked `*_impl.cs` companions listed in §1 are outside the predicate
and have no row, and two of them carry dossier §10 proposals (`os/linux/pidfd_linux_impl.cs`,
`time/time_impl.cs`). Ruling owed: add rows (evidence class `principal .auto`), or record that the
predicate is the population.

**OQ-2 — principal gone at go1.24.13, so no `.auto`@go1.24.13 can be emitted.** Measured against both
GOROOTs: `crypto/internal/alias/alias_impl.cs` (package directory absent at go1.24.13),
`crypto/subtle/xor_generic.cs` (package present, `xor_generic.go` absent), `internal/concurrent/hashtriemap.cs`
and `internal/concurrent/hashtriemap_whitebox.cs` (package absent), `internal/weak/pointer.cs` (package
absent), `vendor/golang.org/x/crypto/sha3/xor.cs` (package absent). Under §3 step 5 each reads "no
`.auto` emitted" **by construction**. Ruling owed on the record shape for a removed principal, so the
gate is neither a false alarm nor a rubber stamp.

**OQ-3 — test-file hand-owns.** `internal/syscall/windows/registry/registry_test.cs` and
`internal/syscall/windows/exec_windows_test.cs` each carry a tracked `.cs.auto` today, but the dossier
(*09-08 date-screen* §3) records that `-stdlib` emits no test files. Owed: whether the H5 pair includes a
`-tests` emission for these two, or how they are recorded instead.

**OQ-4 — whole-file hand-owns with no tracked base.** `internal/poll/linux/fd_writev_unix.cs` and
`net/windows/lookup_windows.cs`: Go principal present at both releases (instrument: `untouched`), no
tracked `.cs.auto` at `2e6cf71e4`, mechanism unestablished (dossier *09-08 base* §2). They are the first
places a "no `.auto` emitted" defect would show; check the H5 emission at these two paths first.

**OQ-5 — 41 companions with no nameable principal.** No same-name `.cs` exists beside them and no
per-GOOS twin: go2cs-minted splits whose principal is a member set, not a file (dossier §2,
*package-resolved*). The principal must be named by hand before its `.auto` diff can be read. ⚠ **Their
instrument class is a name-match, not evidence:** `runtime/darwin/sigaction_impl.cs` reads `untouched`
only because its name maps to `runtime/sigaction.go`, whose `//go:build` line does not select darwin;
the file's own header locates the call it realizes in the converted `sys_darwin.cs`.

**OQ-6 — `internal/syscall/windows/exec_windows_test.cs`, a mapping hole in the instrument.** The rule
drops a trailing `windows` folder as L3 routing, but here `windows` is the **package's own last segment**
and the file sits at the package root. It maps to `internal/syscall/exec_windows_test.go` (absent) and
reads `no-upstream-counterpart`. Measured: `internal/syscall/windows/exec_windows_test.go` exists at both
releases, byte-identical, and a tracked `.cs.auto` exists. Rule 4 says `manual upstream diff`; the
measured twin says `.auto differential`. **Unplaced.** It is the only one of the 146 in a GOOS-named
directory that is itself a Go package directory (measured). Dossier §10 inherits the same mapping.

**OQ-7 — `internal/concurrent/hashtriemap_whitebox.cs`, a companion not spelled `_impl.cs`.** Its header
names it a companion to `hashtriemap.cs`; rule 2 cannot select it and rule 4 would mis-class it. Its
package is absent at go1.24.13 (OQ-2) and the dossier proposes DELETE THE DIRECTORY. **Unplaced.**

**OQ-8 — the four `testing/` host files ruled out of the H6 population** (`cb24ac747`:
`PackageAncestry.cs`, `TestFormat.cs`, `TestReporter.cs`, `TestRunner.cs`) are marked, so §3 step 2
requires their rows. Owed: `b` citing the ruling, or a gate-side exclusion list. The other six marked
`testing/` files are not named by that ruling.

**OQ-9 — what the sha256 columns hold for a companion.** The converter writes a `.cs.auto` only for a
marked destination (`src/go2cs/conversionDriver.go:442`); an unmarked principal is emitted as its
ordinary `.cs`. Of the 61 rows with a named principal, only `runtime/runtime2_impl.cs` has a marked
principal (`runtime/runtime2.cs`). For the other 60 the columns would hold the principal's emitted `.cs`
at each release — to be stated at fill time, or the columns renamed.

**OQ-10 — per-GOOS rows need the multi-target emission.** 68 rows sit in a folder named
`windows`/`linux`/`darwin` (the instrument's folder rule). 67 of them are in a true L3 folder (windows 32,
linux 20, darwin 15); `internal/syscall/windows/exec_windows_test.cs` is at its package root instead
(OQ-6). In the L3 case a `.cs.auto` follows its hand-own into its target's folder, so each is emitted
only by that target's run. The pair must come from the `-platforms` emission H5 runs; a single-target
pair leaves every other flavour's rows reading "no `.auto` emitted". The two companions whose principal
is three per-GOOS files (`net/dnsclient_impl.cs`, `syscall/syscall_impl.cs`) need all three flavours.

**OQ-11 — hand-owned packages.** `testing` (10 rows) and `unsafe` (1) are never converted, so no `.auto`
exists by construction and a `manual upstream diff` has no `.auto` pair. Owed: what fills their sha256
columns (the upstream Go file at each release is the natural reading), and whether §3 step 5 exempts this
evidence class. Nine `testing/` host files map to no Go file at either release.

**OQ-12 — the gate script is owed** (§3). Owed: an owner, and whether it asserts the evidence-class
column as well as the class. *Proposed interim, not a ruling:* until the script exists, steps 1–5 are
performed by hand against this table at fill time.

**OQ-13 — the document type of this file.** The runbook rules only *"one audit file per migration under
`docs/phase4/`"*. Owed: which Glossary document type it is, whether it carries a state line, and whether
§4's rows are filled in place (the proposal in the header) or by dated block.

### Rulings on the mailbox after the dossier that bear on rows (pointers, not classes)

Recorded so the fill starts from them, not from the dossier alone. None of them writes a class here.

```
  DELETED  internal/concurrent/hashtriemap_whitebox.cs   NO ROW -- the row was removed at 145 (COORD
           46198c1b9 §1: its principal's dumpMap/dumpNode are gone at 1.24.13). Kept here because the
           disposition still reads: it RETIRED with internal/concurrent, recorded as
           reason-expired (1.24.13's internal/sync/hashtriemap_test.go has no node[ and no dump helpers;
           export_test.go is a different surface). Measured C1 1bd493fda §2; ruled COORD 4327ab7e1 §7.
           Bears on OQ-7 and OQ-2.
  row 78   runtime/mfinal.cs     11 .auto hunks dispositioned (C1 f9f41e8d8 §2). Ruled COORD c58b4c01d §1-2:
           createfing rewired to the live runner; hunk #7 stays out of the hand-own's runfinq; a
           header sentence per bare-mint site; re-aliased for runtime/internal/{sys,math} ->
           internal/runtime/{sys,math} (§3 there).
  row 81   runtime/panic_impl.cs  an erratum is owed in its WHY: the "exits 2 through the backstop" premise
           is false under the -tests host (i9 9f00b7059 §4; C1 d79dbb317 §1; COORD 4327ab7e1 §1). It lands
           in C1-2's branch (COORD 1ef59adad §1).
  row 87   runtime/runtime2.cs   RE-DERIVE from the 1.24.13 .auto plus the two documented edits, not a
           3-way merge (C1 1bd493fda §4; ruled COORD 4327ab7e1 §7(ii)).
  row 99   sync/mutex.cs         the relocated wrapper becomes a hand-own without LayoutKind.Explicit
           (C1 1bd493fda §5; ruled COORD 4327ab7e1 §7(iii)).
  row 75   runtime/mcleanup.cs   a 1.24 file ruled a HAND-OWN OWED (COORD c58b4c01d §1). CUT at
           claude/c1-mcleanup-handown-clean; it has ENTERED the re-measured census (147) and has its
           row, per COORD 817f98813 §2 -- see the 2026-09-13 amendment below.
  new      runtime/mgc_impl.cs   a managed gcTestIsReachable companion, routed as C1-2 (COORD 1ef59adad §1).
           It is absent at a02ac3df3. It is now CUT and carries the marker at
           claude/c1-gctestisreachable-clean, whose tip 4a9ae8cbb is pinned as train row 12 -- so it is the
           NEXT entrant. NO row here until COORD rules it in, and the arithmetic has MOVED: "148"
           was 147 + 1 and the population is now 145, so do not carry that number -- re-measure
           at whatever tree the two tracks finally meet on (2026-09-14 amendment below).
```

## 2026-09-13 — AMENDMENT (lane G): the population moved 146 → 147 and `runtime/mcleanup.cs` has a row

Per COORD `817f98813` §2. **Row 77** is `runtime/mcleanup.cs`; rows 77–146 renumbered to 78–147, and the
four `row N` citations in the block above moved with them (79→80, 82→83, 88→89, 100→101). No row's
content changed, and the two `row 1` / `row 2` citations inside §4 are a *collision bill's* rows, not this
table's, so they were left alone.

**Re-measured, not carried** — §1's anchored predicate, run per ref:

```
  a02ac3df3                               146     the skeleton's base; its 146 rows are EXACTLY this set
  claude/c1-mcleanup-handown-clean        147     = 146 + src/core/runtime/mcleanup.cs, 0 removed
  claude/version-go1.24.13 (origin tip)   146     neither entrant present there yet
```

The row's three derived cells, each measured rather than inferred:

```
  upstream mapping   runtime/mcleanup.go      the instrument's own rule (handown-census.ps1:128-139)
  presence           ABSENT at go1.23.12, PRESENT at go1.24.13 (193 lines)
    -> instrument class   touched-substantive  the "appeared or vanished across the range" branch
  evidence class     .auto differential       §2 precedence rule 3 — whole-file hand-own, and its
                                              instrument class is not no-upstream-counterpart
  marker             mcleanup.cs:47, spelled [module: go.GoManualConversion]
  dossier pointer    —                        the dossier names neither entrant (measured)
```

⚠ **The 146 figures in §1, §2 and §4 are MEASUREMENTS at the trees they name and are deliberately NOT
rewritten** — this file is a record, amended rather than edited. What they no longer describe is the
tree being adopted: §2's evidence-class tally reads `.auto differential` 31 summing to 146; at a tree
carrying `mcleanup.cs` it is 32 summing to 147.

⚠ **A SECOND ENTRANT IS ALREADY CUT, AND DELIBERATELY HAS NO ROW HERE.** The block above always named
two. `runtime/mgc_impl.cs` carries the marker at `claude/c1-gctestisreachable-clean`, whose tip
`4a9ae8cbb` is the SHA COORD pinned as train row 12 — so it is cut and seated, not pending. Each seat
reads 147 **on its own** (146 plus its own single file); a tree carrying BOTH reads **148**. The ruling
names `mcleanup.cs` and 147, so only that row is written here: at a both-seats tree the gate's assertion
2 would fire `A2-missing` on `runtime/mgc_impl.cs`, which is the gate working. Measured and reported to
COORD; its row goes in on a ruling, never on this measurement.

## 2026-09-14 — AMENDMENT (lane G): the population moved 147 → 145 at the H5 relocation

Per COORD `46198c1b9` §5, with the judgment calls and the row-76 reading ruled at `3f54a3253`.
**Measured at the landed version-branch tip `a4ece44fff`** — not carried from the seat that proposed it,
and not from my own preview of the same commit on `claude/c1-h5-relocation`:

```
  census at claude/version-go1.24.13 = a4ece44fff        145
  the 145 rows below are EXACTLY that marked set (set-compared, both sides non-empty)
  PREDICTION: 145 was filed at G fd362f1af §1 BEFORE the commit existed, with falsifiers
              144 (a move landing as a delete the marker did not follow) and 146 (a delete
              that did not happen). Both silent. MET.
```

**Three moves** (the row follows its principal; the path changes, the row does not):

```
  crypto/internal/alias/alias_impl.cs  ->  crypto/internal/fips140/alias/alias_impl.cs   principal md5-IDENTICAL
  internal/concurrent/hashtriemap.cs   ->  internal/sync/hashtriemap.cs                  PRINCIPAL CHANGED
  internal/weak/pointer.cs             ->  weak/pointer.cs                               PRINCIPAL CHANGED
```

**Two rows REMOVED from the table, provenance kept here** (COORD `3f54a3253` (a): a row naming a path the
census no longer marks is an orphan the gate would NOTE on every run forever):

```
  internal/concurrent/hashtriemap_whitebox.cs   DELETED. Its principal was hashtriemap_test.go's
        dumpMap/dumpNode; the 1.24.13 test has neither and zero references to the node type, so the
        file's stated reason for existing is gone.            ruled COORD 46198c1b9 §1
  vendor/golang.org/x/crypto/sha3/xor.cs        DELETED. No xor.go in either 1.24 sha3 package and
        xorIn/copyOut are declared nowhere at 1.24.13, so a relocation would land a whole-file
        replacement with no principal — a file no reconvert ever writes an .auto beside.
                                                              ruled COORD 46198c1b9 §1
```

**Annotations** are in the reason/work-item cell; the class column stays blank until H6 fills it
(COORD `3f54a3253` (b), so the table's shape does not change for three rows):

```
  row  20  internal/sync/hashtriemap.cs   PRINCIPAL CHANGED   Store/Swap/CompareAndSwap/LoadAndDelete/
                                          Delete/Range/Clear/init are new; sync.Map at 1.24 is built on it
  row 145  weak/pointer.cs                PRINCIPAL CHANGED   91 lines differ; Strong() -> Value()
  row  74  runtime/mbitmap_impl.cs        RE-POINT            getgcmask -> pointerMask, SAME signature;
                                          every mask-construction and return line identical and the diff
                                          is the validation block the hand-own replaces (i9 4620838568,
                                          ruled 3f54a3253) — deliberately NOT principal-changed, so H6
                                          opens this row already knowing its answer
```

⚠ **THE TRACK DISTINCTION** (C1 `91131b6a8` §2), recorded because it is what makes the next number
readable: the two entrants that were pending sat on **different tracks** — `runtime/mcleanup.cs` on the
version branch (row 75 here) and `runtime/mgc_impl.cs` on master via train-48 row 12. **`mgc_impl.cs` still
has no row**, and gets one only when row 12 lands on master AND master merges into the version branch
(COORD `46198c1b9` §5) — not when it is merely cut. Any "+1" arithmetic against an older population is
stale; re-measure at the tree where the tracks meet.

Renumbering: rows are C-sorted and contiguous 1..145; the five surviving `row N` citations in the block
above moved with them (77→75, 80→78, 83→81, 89→87, 101→99), and the `row 9` citation became a DELETED note
because that row no longer exists. No row's content changed — bodies compare byte-identical after blanking
the number column. Every "146"/"147" figure earlier in this file remains a MEASUREMENT at the tree it
names and is deliberately not rewritten.

## 2026-09-14 — AMENDMENT (lane G): row 3 follows the fourth fips140 relocation

C1's `f0f8826894` (the three H6 rows, landed on the version branch) relocates `crypto/subtle/xor_generic.cs`
to `crypto/internal/fips140/subtle/`, per COORD `9c07f494f` item 4. **Re-measured at the landed tip, not
carried:**

```
  census at claude/version-go1.24.13 = f0f8826894      145   -- the COUNT did not move
  set difference against this table BEFORE the edit:
      + src/core/crypto/internal/fips140/subtle/xor_generic.cs   (census, no row)
      - src/core/crypto/subtle/xor_generic.cs                    (row, no longer marked)
  after the edit: row set IDENTICAL to the census, both directions empty
```

**A count that does not move is not a population that did not move** — this is the case the re-measure rule
exists for, and the gate would have fired `A2-missing` on the new path plus one orphan row for the old one.

The edit is one path on **row 3**, which sorts to the same index under C collation, so **no row was
renumbered and no `row N` citation moved**. Nothing else in the table changed.

The principal moved with it: `crypto/subtle/xor_generic.go` is absent at 1.24.13 while
`crypto/internal/fips140/subtle/xor_generic.go` is present (64 lines). C1 measured the delta as
**one build-tag line, four bodies byte-identical** (`8f1f7f090`), so this is a RELOCATION and not a
re-derive — the class stays for the seat that cut it.

## 2026-09-14 — FILL BLOCK 1 (lane G): seven windows-amd64 rows, all class `b`

Per COORD `2cd01f8d6` R3 and `f6c60275e` (item 3 UNBLOCKED for windows-amd64). **Rows filled in place in §4; this block
is their record.** COORD rules the pair per block.

**The pair this block read.**

```
  identity         CONTENT-NORMALIZED: sha256 of the file with every CR byte removed (COORD f6c60275e). The sha256 cells of the
                   rows below hold THAT identity, not raw-byte hashes.
  1.23.12 side     half B, stage windows-amd64 (G a5534b5de s2): go1.23.12 GOROOT, the outgoing version.props, seed a4ece44fff
  1.24.13 side     half A, stage windows-amd64, RE-CUT on G-LAPTOP (G dc7ce18be; determinism 74f40a1c2; usable for windows-amd64
                   by ARM 3, G b0b825af0, ruled f6c60275e)
  binary           go2cs.exe sha256 e0b2a4c109053c6b45ba01d731dc01b2b204a057bed50cfd5afdbb83502a347e (tree ddf7cb17c8), both halves
  side test        PRINCIPAL-EXISTENCE (8808a00ad): each side's mapped Go file exists at its release. Per-row pair rule 80c948a7f.
                   Resolver over all 145 rows: 0 rows whose principal exists with its side missing; 0 present sides carrying the seed stamp
  hand-owns read   at the version-branch tip f0f8826894 (a clean checkout), searched for every identifier each pair's diff touches
```

**The seven rows.** All are in a `windows/` folder, so the windows target alone determines them. Each is PRINCIPAL-CHANGED on the pair.

```
  row  hand-own                                   pair diff (CR-stripped)         reading
   15  internal/poll/windows/fd_windows_impl.cs   1 / 2 lines, using-aliases      EMISSION-ONLY: Go principal byte-identical
   42  net/windows/lookup_windows.cs              3 / 3 lines, Δruntime alias     EMISSION-ONLY: Go principal byte-identical
   47  os/windows/dir_windows_impl.cs             1 / 1 line,  Δruntime alias     EMISSION-ONLY: Go principal byte-identical
   92  runtime/windows/os_windows_impl.cs         3 / 6 lines                     UPSTREAM-IN-PRINCIPAL: 0 companion references
   93  runtime/windows/signal_windows_impl.cs     2 / 1 lines, sys path move      UPSTREAM-IN-PRINCIPAL: 0 companion references
   94  runtime/windows/trace_impl.cs              6 / 7 lines                     UPSTREAM-IN-PRINCIPAL: 0 companion references
  119  syscall/windows/exec_windows.cs            3 / 3 lines, Δruntime alias     EMISSION-ONLY: Go principal byte-identical
```

**Two `b` reason shapes, PROPOSED for your ruling, not assumed.** §2's class table has no row for a non-empty `.auto` diff behind a byte-identical Go principal:

- **EMISSION-ONLY**: the Go principal is byte-identical at both releases (the instrument reads `untouched`) and the `.auto` diff is the converter's own spelling, here using-aliases. It is not `unchanged`, because §2 reserves that word for an empty diff. It is not `a` or `c`, because there is no upstream change to absorb or owe. **Ruling asked:** is `b` with this reason the record shape, or does the table want a fifth word?
- **UPSTREAM-IN-PRINCIPAL**: the upstream change is real, but every changed member lives in the emitted principal, and the companion references none of them (0, by search).

**Deliberately NOT filled in this block.**

```
  row 20                   LAST, after C1's row-20 commit is at the version tip (R3)
  batch 2 (windows/ rows)  29, 30, 46, 48, 90, 122, 126 -- PRINCIPAL-CHANGED with diffs of 26 to 405 lines; the next block
  34 rows                  PRINCIPAL-CHANGED with target-independent sides: they fill only after the linux-amd64 and darwin-amd64
                           normalized joins, so a per-target variance cannot hide behind the windows reading
  row 75                   ARRIVED (no 1.23.12 side), record shape still to be written
  41 + 11 rows             principal not named (OQ-5) and hand-owned packages (OQ-11): no pair exists; rulings owed
```

## 2026-09-14 — FILL BLOCK 2 (lane G): block 1 re-shaped by ruling; seven more windows-amd64 rows, five `b` and two `c`

Per COORD `f43ae82f1` (block-1 rulings) and `8b2cbccb4` (linux usable, narrowed). Rows filled or amended in place in §4;
this block records them.

**1. Block 1's seven rows, AMENDED to the ruled cell shape.** Class and both hashes are unchanged; only the reason cell changes.

```
  (i)   EMISSION-ONLY rows 15, 42, 47, 119   now carry the Go principal's sha256 at both releases (equal), the alias spelling named,
                                             the 2026-09-08 fourth-arm ruling, and the hand-own's alias line or "no such site"
  (ii)  UPSTREAM-IN-PRINCIPAL rows 92-94     now carry the changed members by name, the whole-word predicate string over the
                                             companion's whole text, its count 0, and a firing control
  (iii) evidence class                       every row's reason cell LEADS with its evidence class (companion-vs-principal |
                                             auto-differential). It does not go in the class cell: the gate reads that cell whole
                                             and admits exactly unchanged/a/b/c (check-handown-audit.ps1:146, :209)
```

**2. The seven new rows.** All are in `windows/` folders and PRINCIPAL-CHANGED on the pair; the windows target alone determines them.

```
  row  hand-own                                                   class  reading
   29  internal/syscall/windows/windows/syscall_windows_impl.cs     b    UPSTREAM-IN-PRINCIPAL (NTStatus family, Nt* //sys); 0
   30  internal/syscall/windows/windows/zsyscall_windows_impl.cs    b    UPSTREAM-IN-PRINCIPAL (13 new wrappers + ᴋN renumbering); 0
   46  os/user/windows/lookup_windows_impl.cs                       c    REWRITE OWED: listGroupsForUsernameAndDomain changed upstream
   48  os/windows/file_windows_impl.cs                              c    REWRITE OWED: readReparseLinkHandle reached from os.Root
   90  runtime/windows/lock_sema_impl.cs                            b    UPSTREAM-IN-PRINCIPAL (mutex moved to lock_spinbit.go); 0
  122  syscall/windows/syscall_windows_impl.cs                      b    UPSTREAM-IN-PRINCIPAL (Open / Ftruncate rewrite); 0
  126  syscall/windows/zsyscall_windows_impl.cs                     b    UPSTREAM-IN-PRINCIPAL (createFile rename + setFileInformationByHandle); 0
```

**3. The two `c` rows, with the evidence each rests on.**

- **Row 46.** The Go body of `listGroupsForUsernameAndDomain`, one of the three members the companion hand-converts, differs between the releases (hashed per function at both GOROOTs; the other two are body-identical). At 1.24.13 an empty `NetUserGetLocalGroups` result returns `nil, nil`. The companion still errors on it at `lookup_windows_impl.cs:291-292`, in a branch that also covers a null published buffer, so the owed change must separate the two cases rather than simply delete the error.
- **Row 48.** At 1.24.13, `os/root_windows.go:176` and `:221` call `readReparseLinkHandle` directly; the emitted `os/windows/root_windows.cs:180` and `:230` do the same. The emitted body at `os/windows/file_windows.cs:451` performs the reinterpret over `PathBuffer` that this companion's own header (lines 8-24) records as crashing the test host. The companion hand-converts only `readReparseLink`. So a live 1.24 caller reaches exactly the body the hand-own exists to prevent.

Both name `BOARD` as their work item. **The owner and seat are COORD's to rule**; the gate's A4 reads the reference.

**4. Readings this block rests on, beyond the pair.**

```
  predicates   whole-word counts (grep -Fwo) over each companion's whole text at the version tip f0f8826894, one firing control per
               companion; every changed member 0, except row 29's `Error` = 1 (a comment, line 140, not the member) and row 48's
               readReparseLink family, which is the finding
  halfA2       normalized tree hashes EQUAL halfA on all three targets (windows 2aa5066a, linux da288e91, darwin 5e8dcc0f): the re-cut
               is reproducible under the ruled identity, not only on raw bytes
```

**5. Still NOT filled.** Row 20 (LAST). Darwin-folder rows (next). Linux rows as ruled at `8b2cbccb4`: rows 44, 111, 87, 88 keep their linux side for ARM 2, and rows 87/88 are per-target. The 34 target-independent PRINCIPAL-CHANGED rows. Row 75 (ARRIVED). The 41 + 11 rows with no pair (OQ-5, OQ-11).

## 2026-09-14 — FILL BLOCK 3 (lane G): the four darwin/ PRINCIPAL-CHANGED rows, all `b`; and the member-body check behind every companion row

Rows filled in place in §4 from the darwin-amd64 target (USABLE, COORD `8b2cbccb4`); this block records them.

**1. The four rows**

```
  row  hand-own                                        class  reading
   21  internal/syscall/unix/darwin/net_darwin_impl.cs   b    UPSTREAM-IN-PRINCIPAL: one new constant (EAI_ADDRFAMILY), 0 companion refs
   43  os/darwin/dir_darwin_impl.cs                      b    EMISSION-ONLY: Go principal byte-identical; alias line 68
   54  runtime/darwin/lock_sema_impl.cs                  b    UPSTREAM-IN-PRINCIPAL: the mutex moved to lock_spinbit.go (as row 90)
  107  syscall/darwin/exec_libc2_impl.cs                 b    EMISSION-ONLY: Go principal byte-identical; no alias site
```

**2. The member-body check — a gap in blocks 1 and 2, closed by measurement (no row moves class).**

For a `*_impl.cs` companion, the emitted principal carries only a one-line placeholder for each function the companion hand-converts. The `.auto` pair therefore **cannot show an upstream change inside a hand-converted body**. Blocks 1 and 2 read the emitted delta and a companion-reference predicate. That combination is blind to such a change, and row 46's `c` was found only by reading its Go function directly. So every companion row is now checked at the Go level:

```
  method      the hand-converted set = the placeholder names the converter wrote in the principal at EACH release; each name's Go
              body (from its "func" line to the first column-0 "}") extracted from the mapped Go principal at both GOROOTs and hashed
  scope       doc comments and //go: directives ABOVE a func line are outside the span, by construction (see the control below)
  controls    row 46: exactly listGroupsForUsernameAndDomain BODY-DIFFERS, its other two BODY-IDENTICAL (fires)
              runtime stdcall: BODY-DIFFERS, carrying getcallerpc -> sys.GetCallerPC (fires)
              runtime mdestroy: BODY-IDENTICAL although the pair shows it changed; the change is the //go:nowritebarrierrec directive
              and doc-comment lines above its func line, i.e. outside the span -- a mis-chosen control, read and recorded, not rounded
```

```
  row  hand-converted set (1.23.12 -> 1.24.13)          bodies
   15  4 -> 4, same                                     all BODY-IDENTICAL (rawToSockaddrInet4/6, sockaddrInet4/6ToRaw)
   47  1 -> 1, same                                     readdir BODY-IDENTICAL
   92  0 placeholders                                   no population: the companion's members are go2cs-minted ᴛ helpers realising
                                                        initSysDirectory and initLongPathSupport, compared by name: BODY-IDENTICAL
   93  0 placeholders                                   no population: the companion owns the SetConsoleCtrlHandler edge calling the
                                                        CONVERTED ctrlHandler, compared by name: BODY-IDENTICAL
   94  2 -> 2, same                                     StartTrace, StopTrace BODY-IDENTICAL
   29  3 -> 3, same                                     WSARecvMsg, WSASendMsg, loadWSASendRecvMsg BODY-IDENTICAL
   30  7 -> 7, same                                     all BODY-IDENTICAL
   46  3 -> 3, same                                     listGroupsForUsernameAndDomain BODY-DIFFERS -- the row's class c
   48  1 -> 1, same                                     readReparseLink BODY-DIFFERS (29 -> 8 lines) -- consistent with the row's class c
   90  7 -> 4, lock2 / mutexContended / unlock2 leave   the four that stay BODY-IDENTICAL
  122  10 -> 10, same                                   all BODY-IDENTICAL
  126  27 -> 27, same                                   all BODY-IDENTICAL
   21  2 -> 2, same                                     Getaddrinfo, Freeaddrinfo BODY-IDENTICAL
   43  1 -> 1, same                                     readdir BODY-IDENTICAL
   54  7 -> 4, as row 90                                the four that stay BODY-IDENTICAL
  107  0 placeholders                                   Go principal byte-identical, so every body is identical by construction
```

The whole-file rows (42, 119) are not affected: their `.cs.auto` carries every body, so the pair already sees everything. **From this block on, every companion row's cell records its member-body result.**

**3. Rows 46 and 48 amended by ruling (COORD `d36cea91d`).** Class `c` is unchanged. Each work-item sentence now names BOARD, **owner C1**, **EXPLICITLY DEFERRED** (behavioural, off the H5 critical path), the ruling SHA, the sequence (a hand-own re-derive on the version branch after row 20, one commit per row) and the acceptance i9 runs on the Windows arm. Row 48's cell also records the member-body result (`readReparseLink` 29 -> 8 lines). A4 still reads BOARD, and the completeness gate can close with both rows named and owned.

**4. Still NOT filled.** Row 20 (LAST). Linux/ rows as ruled at `8b2cbccb4` (next). The 34 target-independent PRINCIPAL-CHANGED rows. Row 75 (ARRIVED). The EQUAL rows (class `unchanged`). The 41 + 11 rows with no pair (OQ-5, OQ-11).

## 2026-09-14 — FILL BLOCK 4 (lane G): six linux/ rows, all `b`

From the linux-amd64 target, USABLE as narrowed by COORD `8b2cbccb4`. Rows filled in place in §4; this block records them.

**1. The six rows**

```
  row  hand-own                                       class  reading
   65  runtime/linux/lock_futex_impl.cs                 b    UPSTREAM-IN-PRINCIPAL: mutex to lock_spinbit.go; new futex semaphores emitted
   68  runtime/linux/os_linux_impl.cs                   b    UPSTREAM-IN-PRINCIPAL: m fields + vgetrandomInit; getHugePageSize identical
   71  runtime/linux/trace_impl.cs                      b    UPSTREAM-IN-PRINCIPAL: as row 94 (companion byte-identical)
  114  syscall/linux/syscall_linux_amd64_impl.cs        b    UPSTREAM-IN-PRINCIPAL: Getrlimit/setrlimit/rawSetrlimit leave the file
  115  syscall/linux/syscall_linux_impl.cs              b    UPSTREAM-IN-PRINCIPAL: Accept added; Getrlimit/setrlimit over prlimit1
  116  syscall/linux/zsyscall_linux_amd64_impl.cs       b    UPSTREAM-IN-PRINCIPAL: generated Getrlimit/setrlimit removed; 194 ᴋN lines
```

**2. Non-zero predicate hits, each read and named in its cell.** Per `f43ae82f1` (ii), a count above 0 for a changed member moves a row off `b`. Every non-zero count here was read in context and **none is a reference to a changed member**:

```
  row 65   lock2 / unlock2 / semasleep / semawakeup = 4  -> header comments, lines 11 and 18
  row 68   sysauxv / archauxv / vdsoauxv / osinit  = 8  -> header comments, lines 8-31 (the osinit path the companion replaces)
  row 115  runtime = 8                              -> comment lines 11, 13, 15, 24, 88, 92, 93, and the namespace path
                                                       `@internal.runtime.syscall_package` at line 84 (not the alias that changed)
```

**3. Member bodies (the check of block 3).** Row 46's control fired first. Rows 65 (7 -> 4, the four that stay identical), 71, 115 (11 identical) and 116 (9 identical) are covered by the placeholder set. Rows 68 and 114 have no placeholders, so their realised Go functions were compared by name, with the `stdcall` control firing: `getHugePageSize` BODY-IDENTICAL; `gettimeofday` is a body-less assembly stub whose declaration is identical at both releases. `sysauxv`'s Go body differs by comment lines only.

**4. Still NOT filled.** Row 20 (LAST). The linux side of rows 44 and 111, and the linux flavour of rows 87/88, wait for ARM 2 (`8b2cbccb4`). The 34 target-independent PRINCIPAL-CHANGED rows (next). The EQUAL rows (class `unchanged`). Row 75 (ARRIVED). The 41 + 11 rows with no pair (OQ-5, OQ-11).

## 2026-09-14 — FILL BLOCK 5 (lane G): eight target-independent rows, all `b`; a third `b` reason shape PROPOSED

The first batch of the target-independent PRINCIPAL-CHANGED rows (sides not under a GOOS folder). Rows filled in place in §4; this block records them.

**1. Why these eight fill once for all three targets.** For each row the left side reads one normalized hash across windows-amd64, linux-amd64 and darwin-amd64, and so does the right side (1 distinct each, measured). Of the target-independent PRINCIPAL-CHANGED rows, only rows 87 and 88 vary by target (ruled at `8b2cbccb4`). The eight are the smallest pairs in the set by `diff --strip-trailing-cr`, case- and order-exact. An earlier sizing pass used PowerShell's Compare-Object; it is case-insensitive and read row 7 as 0 changed lines against 16, so it was discarded.

```
  row  hand-own                           class  reason shape            anchor
    7  internal/chacha8rand/chacha8_impl.cs  b   UPSTREAM-IN-PRINCIPAL   byteorder renames, 16 code lines; 0 companion refs; realised bodies identical
    9  internal/godebug/godebug.cs           b   EMISSION-ONLY           Go 617c491b... = 617c491b...; sync re-qualified; no alias site
   37  math/bits/bits_impl.cs                b   COMMENT-ONLY (proposed) 2 changed Go lines, both comments
   83  runtime/pinner_impl.cs                b   UPSTREAM-IN-PRINCIPAL   special.offset type, 2 code lines; special = 1 is a comment (l.265)
   96  sync/atomic/type.cs                   b   COMMENT-ONLY (proposed) 2 changed Go lines, both comments
   98  sync/cond_impl.cs                     b   EMISSION-ONLY           Go 324314fd... = 324314fd...; sync/atomic re-qualified; no alias site
  103  sync/poolqueue.cs                     b   EMISSION-ONLY           Go 25b900ff... = 25b900ff...; own aliases at lines 28-29
  139  time/sleep_impl.cs                    b   COMMENT-ONLY (proposed) 6 changed Go lines, all comments
```

**2. COMMENT-ONLY — proposed for your ruling, not assumed.** Rows 37, 96 and 139 are the instrument's `touched-trivial` class: the Go principal differs, but only in comments. EMISSION-ONLY does not fit, because its shape asserts a byte-identical Go principal and these principals are not byte-identical. UPSTREAM-IN-PRINCIPAL does not fit either, because nothing executable changed. The proposed shape is: *"COMMENT-ONLY: the Go principal differs at 1.24.13 in comment lines only (N changed lines: N comment, 0 code, 0 blank); the .auto delta is those comment lines; nothing executable changed"*, plus the member-body result where the row is a companion. The test behind it is mechanical: each changed Go line is classed by whether it begins (after whitespace) with `//`.

**3. Member bodies (arm of record, `8cf7fdf65`).** Row 46's control fires. Rows 37, 83, 98 and 139 are covered by their placeholder sets, every body BODY-IDENTICAL. Row 7 has no placeholders, so the functions its companion realises were compared by name with runtime `stdcall` firing in the same run: `block` (chacha8.go), `block_generic` and `setup` (chacha8_generic.go), all BODY-IDENTICAL with identical declarations. Rows 9, 96 and 103 are whole-file hand-owns, so the pair sees every body.

**4. Still NOT filled.** The other target-independent PRINCIPAL-CHANGED rows. Seven carry a BODY-DIFFERS member and are read next: 6 (Elem, Key), 8 (getGOAMD64level), 51 (Type, plus ten map methods leaving value.go), 81 (Goexit, fatal), 95 (overlaps), 2 (AnyOverlap arrives at the fips140 path), 74 (pointerMask arrives; RE-POINT). Rows 87/88 per target. The EQUAL rows (`unchanged`). Row 75 (ARRIVED). Row 20 (LAST). The 41 + 11 rows with no pair.

## 2026-09-14 — FILL BLOCK 6 (lane G): four rows where the member-body arm fired, all `b`; an extractor defect fixed; a fourth `b` reason shape PROPOSED

Rows filled in place in §4 (each side identical on all three targets, measured); this block records them.

**1. An extractor defect of mine, found in this block and fixed before any row was filled from it.** The member-body arm extracts a Go function from its `func` line to the next column-0 `}`. A **body-less declaration** (an assembly or linkname stub, e.g. `func getGOAMD64level() int32`) has no brace of its own, so the extraction ran on and read the FOLLOWING code as the body. Row 8's `getGOAMD64level` read BODY-DIFFERS, while the text actually diffed was `doinit`'s new FSRM detection below it.

```
  scope     every placeholder name read in blocks 1-5 plus this block's candidates, checked for a func line that does not open a
            body: ONE hit, row 8 getGOAMD64level (cpu_x86.go:18 at both SDKs)
  direction a leak can only ADD code to a span, so it can produce a false BODY-DIFFERS and never a false BODY-IDENTICAL; every
            IDENTICAL reading of blocks 1-5 stands, and the one false DIFFERS was never filled from
  fix       a func line that does not end in "{" is its own whole text; re-run with both controls firing (row 46; runtime stdcall):
            getGOAMD64level now reads its one-line declaration, BODY-IDENTICAL
```

**2. Ownership of a changed member is traced before it moves a row.** A placeholder says that a member is hand-converted in *some* `*_impl.cs` of its package, not necessarily this row's companion. So every BODY-DIFFERS member was located by its declaration in the version checkout:

```
  Goexit (runtime/panic.go)      declared in runtime/managed_impl.cs:529 = row 73 (principal not named, OQ-5), NOT row 81's companion.
                                 Its body change (getcallerpc -> sys.GetCallerPC) is recorded here for OQ-5's naming of row 73.
  fatal, throw                   runtime/panic_impl.cs:77, :68 (row 81)
  Elem, Key (internal/abi)       internal/abi/type_impl.cs:556, :641 (row 6)
  overlaps                       slices/slices_impl.cs:36 (row 95)
  getGOAMD64level                internal/cpu/cpu_x86_impl.cs:78 (row 8)
  Type + 10 map methods (reflect) reflect/value_impl.cs (row 51) -- read in the next block (their 1.24 principal is map_swiss.go)
```

**3. The four rows**

```
  row  hand-own                        class  reading
    6  internal/abi/type_impl.cs         b    NOT-APPLICABLE-TO-MANAGED (proposed): Elem/Key bodies changed by MapType -> mapType only; the
                                              managed Elem/Key resolve through the carried System.Type (GoReflect), never the map record;
                                              the emitted map-type split, KindGCProg removal and TFlagGCMaskOnDemand rename count 0 in code
    8  internal/cpu/cpu_x86_impl.cs      b    UPSTREAM-IN-PRINCIPAL: FSRM detection emitted; predicate 0; getGOAMD64level stub identical
   81  runtime/panic_impl.cs             b    NOT-APPLICABLE-TO-MANAGED (proposed): fatal gains printlock/printunlock against interleaved fatal
                                              reports; the managed fatal writes the whole report in ONE Console.Error.Write (FatalReport.cs:132)
                                              and exits; plus the emitted getcallerpc -> sys.GetCallerPC move (76 lines) and six new linkname
                                              wrappers that call the companion's own fatal/throw
   95  slices/slices_impl.cs             b    UPSTREAM-IN-PRINCIPAL: Clone and Repeat rewritten in the emission; overlaps' body differs by one
                                              comment line only
```

**4. NOT-APPLICABLE-TO-MANAGED — proposed for your ruling, not assumed.** UPSTREAM-IN-PRINCIPAL says every changed member lives in the emission. Here the member-body arm names a changed member that the COMPANION owns (rows 6 and 81), so that shape would be false. Class `b` is §2's own definition for this case: *the upstream change does not apply to the managed implementation*. The proposed cell elements are: the changed member by name, with the Go-level delta quoted; what the managed body does instead, with a file:line; and why the delta cannot reach it. **Row 81's reading rests on documented .NET behaviour** (Console's writers are synchronized), not on a measurement taken here, and its cell says so.

**5. Rows 37, 96 and 139 re-shaped to the ruled COMMENT-ONLY wording (COORD `999d5c784`).** Class `b` is unchanged. Each cell now states *N comment, 0 directive (//go:), 0 code, 0 blank*, classed by `diff --strip-trailing-cr` on first non-space characters `//` and not `//go:`: 37 is 2/0/0/0, 96 is 2/0/0/0, 139 is 6/0/0/0. A control confirms the directive class fires: on `runtime/os_windows.go` it counts exactly one changed directive (`mdestroy`'s `//go:nowritebarrierrec`). **A gap of mine, recorded:** block 5's counts ran on plain `diff` with a `//` test that would have counted a `//go:` line as a comment. The re-measure under the ruled classifier reads 0 directives on all three, so no class moves.

**6. Still NOT filled.** Row 51 (reflect: Type plus ten map methods whose 1.24 principal is map_swiss.go) and rows 2 and 74 (members arriving) are next. Then the remaining target-independent rows, rows 87/88 per target, the EQUAL rows (`unchanged`), row 75 (ARRIVED), and row 20 (LAST).
