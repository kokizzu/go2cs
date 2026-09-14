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
| 6 | `internal/abi/type_impl.cs` | `internal/abi/type.go` | touched-substantive | principal .auto — `internal/abi/type.cs` | §10 MEMBERS-REMOVED → RE-WRITE | — | — | — | — |
| 7 | `internal/chacha8rand/chacha8_impl.cs` | `internal/chacha8rand/chacha8.go` | touched-substantive | principal .auto — `internal/chacha8rand/chacha8.cs` | — | — | — | — | — |
| 8 | `internal/cpu/cpu_x86_impl.cs` | `internal/cpu/cpu_x86.go` | touched-substantive | principal .auto — `internal/cpu/cpu_x86.cs` | §10 MEMBERS-ADDED → RE-DERIVE | — | — | — | — |
| 9 | `internal/godebug/godebug.cs` | `internal/godebug/godebug.go` | untouched | .auto differential | §1 hand-owned by consequence | — | — | — | — |
| 10 | `internal/poll/darwin/runtime_netpoll_impl.cs` | `internal/poll/runtime_netpoll.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 11 | `internal/poll/fd_mutex_impl.cs` | `internal/poll/fd_mutex.go` | untouched | principal .auto — `internal/poll/fd_mutex.cs` | — | — | — | — | — |
| 12 | `internal/poll/linux/fd_writev_unix.cs` | `internal/poll/fd_writev_unix.go` | untouched | .auto differential · OQ-4 | 09-08 base §1: NO base banked | — | — | — | — |
| 13 | `internal/poll/linux/runtime_netpoll_impl.cs` | `internal/poll/runtime_netpoll.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 14 | `internal/poll/runtime_sema_impl.cs` | `internal/poll/runtime_sema.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 15 | `internal/poll/windows/fd_windows_impl.cs` | `internal/poll/fd_windows.go` | untouched | principal .auto — `internal/poll/windows/fd_windows.cs` | — | — | — | — | — |
| 16 | `internal/poll/windows/runtime_netpoll_impl.cs` | `internal/poll/runtime_netpoll.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 17 | `internal/reflectlite/swapper_impl.cs` | `internal/reflectlite/swapper.go` | untouched | principal .auto — `internal/reflectlite/swapper.cs` | — | — | — | — | — |
| 18 | `internal/runtime/atomic/atomic_impl.cs` | `internal/runtime/atomic/atomic.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 19 | `internal/runtime/syscall/linux/syscall_linux_impl.cs` | `internal/runtime/syscall/syscall_linux.go` | untouched | principal .auto — `internal/runtime/syscall/linux/syscall_linux.cs` | — | — | — | — | — |
| 20 | `internal/sync/hashtriemap.cs` | `internal/concurrent/hashtriemap.go` (absent at go1.24.13) | touched-substantive | .auto differential · OQ-2 | §1 hand-owned by consequence; §2 REMOVED; §4 #2 re-route; 09-07 concurrent: DELETE THE DIRECTORY; 09-08 date-screen §3: base GENUINELY STALE | — | — | — | PRINCIPAL CHANGED; RE-DERIVE IN PROGRESS (C1, ruled 4e42736e1) |
| 21 | `internal/syscall/unix/darwin/net_darwin_impl.cs` | `internal/syscall/unix/net_darwin.go` | touched-substantive | principal .auto — `internal/syscall/unix/darwin/net_darwin.cs` | §10 MEMBERS-ADDED → RE-DERIVE | — | — | — | — |
| 22 | `internal/syscall/unix/darwin/user_darwin_impl.cs` | `internal/syscall/unix/user_darwin.go` | untouched | principal .auto — `internal/syscall/unix/darwin/user_darwin.cs` | — | — | — | — | — |
| 23 | `internal/syscall/unix/linux/net_linux_impl.cs` | `internal/syscall/unix/net_linux.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | §10 minted shell → RE-DERIVE | — | — | — | — |
| 24 | `internal/syscall/unix/linux/siginfo_linux.cs` | `internal/syscall/unix/siginfo_linux.go` | untouched | .auto differential | — | — | — | — | — |
| 25 | `internal/syscall/windows/exec_windows_test.cs` | `internal/syscall/exec_windows_test.go` (absent at both) | no-upstream-counterpart | UNPLACED · OQ-6 · OQ-3 | §10 minted shell → ASK | — | — | — | — |
| 26 | `internal/syscall/windows/registry/registry_test.cs` | `internal/syscall/windows/registry/registry_test.go` | untouched | .auto differential · OQ-3 | — | — | — | — | — |
| 27 | `internal/syscall/windows/registry/windows/value.cs` | `internal/syscall/windows/registry/value.go` | untouched | .auto differential | — | — | — | — | — |
| 28 | `internal/syscall/windows/windows/net_windows_impl.cs` | `internal/syscall/windows/net_windows.go` | untouched | principal .auto — `internal/syscall/windows/windows/net_windows.cs` | — | — | — | — | — |
| 29 | `internal/syscall/windows/windows/syscall_windows_impl.cs` | `internal/syscall/windows/syscall_windows.go` | touched-substantive | principal .auto — `internal/syscall/windows/windows/syscall_windows.cs` | §10 MEMBERS-ADDED → RE-DERIVE | — | — | — | — |
| 30 | `internal/syscall/windows/windows/zsyscall_windows_impl.cs` | `internal/syscall/windows/zsyscall_windows.go` | touched-substantive | principal .auto — `internal/syscall/windows/windows/zsyscall_windows.cs` | §10 MEMBERS-ADDED → RE-DERIVE | — | — | — | — |
| 31 | `internal/syscall/windows/windows/zsyscall_windows_module_impl.cs` | `internal/syscall/windows/zsyscall_windows_module.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 32 | `internal/syscall/windows/windows/zsyscall_windows_privilege_impl.cs` | `internal/syscall/windows/zsyscall_windows_privilege.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | §10 minted shell → RE-DERIVE | — | — | — | — |
| 33 | `internal/syscall/windows/windows/zsyscall_windows_ptrout_impl.cs` | `internal/syscall/windows/zsyscall_windows_ptrout.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | §10 minted shell → RE-DERIVE | — | — | — | — |
| 34 | `internal/syscall/windows/windows/zsyscall_windows_version_impl.cs` | `internal/syscall/windows/zsyscall_windows_version.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 35 | `internal/syscall/windows/windows/zsyscall_windows_wsa_impl.cs` | `internal/syscall/windows/zsyscall_windows_wsa.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 36 | `iter/iter_impl.cs` | `iter/iter.go` | touched-trivial | principal .auto — `iter/iter.cs` | — | — | — | — | — |
| 37 | `math/bits/bits_impl.cs` | `math/bits/bits.go` | touched-trivial | principal .auto — `math/bits/bits.cs` | — | — | — | — | — |
| 38 | `math/rand/rand_impl.cs` | `math/rand/rand.go` | touched-substantive | principal .auto — `math/rand/rand.cs` | §10 MEMBERS-ADDED → RE-DERIVE | — | — | — | — |
| 39 | `math/rand/v2/rand_impl.cs` | `math/rand/v2/rand.go` | touched-trivial | principal .auto — `math/rand/v2/rand.cs` | — | — | — | — | — |
| 40 | `net/dnsclient_impl.cs` | `net/dnsclient.go` | untouched | principal .auto — `net/{windows,linux,darwin}/dnsclient.cs` | — | — | — | — | — |
| 41 | `net/windows/interface_windows_impl.cs` | `net/interface_windows.go` | untouched | principal .auto — `net/windows/interface_windows.cs` | — | — | — | — | — |
| 42 | `net/windows/lookup_windows.cs` | `net/lookup_windows.go` | untouched | .auto differential · OQ-4 | 09-08 base §1: NO base banked | — | — | — | — |
| 43 | `os/darwin/dir_darwin_impl.cs` | `os/dir_darwin.go` | untouched | principal .auto — `os/darwin/dir_darwin.cs` | — | — | — | — | — |
| 44 | `os/linux/wait_waitid.cs` | `os/wait_waitid.go` | touched-substantive | .auto differential | 09-07 scope-rule §4 MOVED-WITHIN-PACKAGE; 09-07 BOTH §4: REMOVED-only, not a collision; 09-08 date-screen §3: base GENUINELY STALE | — | — | — | — |
| 45 | `os/tempfile_impl.cs` | `os/tempfile.go` | untouched | principal .auto — `os/tempfile.cs` | — | — | — | — | — |
| 46 | `os/user/windows/lookup_windows_impl.cs` | `os/user/lookup_windows.go` | touched-substantive | principal .auto — `os/user/windows/lookup_windows.cs` | §10 MEMBERS-ADDED → RE-DERIVE | — | — | — | — |
| 47 | `os/windows/dir_windows_impl.cs` | `os/dir_windows.go` | untouched | principal .auto — `os/windows/dir_windows.cs` | — | — | — | — | — |
| 48 | `os/windows/file_windows_impl.cs` | `os/file_windows.go` | touched-substantive | principal .auto — `os/windows/file_windows.cs` | §10 MEMBERS-REMOVED → RE-DERIVE | — | — | — | — |
| 49 | `reflect/deepequal_impl.cs` | `reflect/deepequal.go` | touched-trivial | principal .auto — `reflect/deepequal.cs` | — | — | — | — | — |
| 50 | `reflect/makefunc_impl.cs` | `reflect/makefunc.go` | untouched | principal .auto — `reflect/makefunc.cs` | — | — | — | — | — |
| 51 | `reflect/value_impl.cs` | `reflect/value.go` | touched-substantive | principal .auto — `reflect/value.cs` | §10 MIXED → RE-WRITE | — | — | — | — |
| 52 | `runtime/cputicks_impl.cs` | `runtime/cputicks.go` | untouched | principal .auto — `runtime/cputicks.cs` | — | — | — | — | — |
| 53 | `runtime/darwin/libccall_impl.cs` | `runtime/libccall.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 54 | `runtime/darwin/lock_sema_impl.cs` | `runtime/lock_sema.go` | touched-substantive | principal .auto — `runtime/darwin/lock_sema.cs` | §10 MIXED → RE-DERIVE | — | — | — | — |
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
| 65 | `runtime/linux/lock_futex_impl.cs` | `runtime/lock_futex.go` | touched-substantive | principal .auto — `runtime/linux/lock_futex.cs` | §10 MIXED → RE-WRITE | — | — | — | — |
| 66 | `runtime/linux/mem_linux_impl.cs` | `runtime/mem_linux.go` | untouched | principal .auto — `runtime/linux/mem_linux.cs` | — | — | — | — | — |
| 67 | `runtime/linux/nanotime_impl.cs` | `runtime/nanotime.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 68 | `runtime/linux/os_linux_impl.cs` | `runtime/os_linux.go` | touched-substantive | principal .auto — `runtime/linux/os_linux.cs` | §10 SIGNATURE → RE-DERIVE | — | — | — | — |
| 69 | `runtime/linux/signal_posix_impl.cs` | `runtime/signal_posix.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 70 | `runtime/linux/sigprocmask_impl.cs` | `runtime/sigprocmask.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 71 | `runtime/linux/trace_impl.cs` | `runtime/trace.go` | touched-substantive | principal .auto — `runtime/linux/trace.cs` | — | — | — | — | — |
| 72 | `runtime/lock_managed_impl.cs` | `runtime/lock_managed.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 73 | `runtime/managed_impl.cs` | `runtime/managed.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 74 | `runtime/mbitmap_impl.cs` | `runtime/mbitmap.go` | touched-substantive | principal .auto — `runtime/mbitmap.cs` | §10 MEMBERS-REMOVED → RE-WRITE | — | — | — | RE-POINT |
| 75 | `runtime/mcleanup.cs` | `runtime/mcleanup.go` (absent at go1.23.12) | touched-substantive | .auto differential | — | — | — | — | — |
| 76 | `runtime/mem_persistent_impl.cs` | `runtime/mem_persistent.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 77 | `runtime/metrics/sample.cs` | `runtime/metrics/sample.go` | untouched | .auto differential | — | — | — | — | — |
| 78 | `runtime/mfinal.cs` | `runtime/mfinal.go` | touched-substantive | .auto differential | 09-07 scope-rule §5 RE-DERIVE | — | — | — | — |
| 79 | `runtime/mranges_impl.cs` | `runtime/mranges.go` | untouched | principal .auto — `runtime/mranges.cs` | — | — | — | — | — |
| 80 | `runtime/netpoll_impl.cs` | `runtime/netpoll.go` | touched-substantive | principal .auto — `runtime/netpoll.cs` | — | — | — | — | — |
| 81 | `runtime/panic_impl.cs` | `runtime/panic.go` | touched-substantive | principal .auto — `runtime/panic.cs` | — | — | — | — | — |
| 82 | `runtime/panicvalues_impl.cs` | `runtime/panicvalues.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | §10 minted shell → RE-DERIVE | — | — | — | — |
| 83 | `runtime/pinner_impl.cs` | `runtime/pinner.go` | touched-substantive | principal .auto — `runtime/pinner.cs` | — | — | — | — | — |
| 84 | `runtime/pprof/pprof_impl.cs` | `runtime/pprof/pprof.go` | touched-substantive | principal .auto — `runtime/pprof/pprof.cs` | — | — | — | — | — |
| 85 | `runtime/pprof/proflabel_impl.cs` | `runtime/pprof/proflabel.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 86 | `runtime/pprof/symtab_impl.cs` | `runtime/pprof/symtab.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 87 | `runtime/runtime2.cs` | `runtime/runtime2.go` | touched-substantive | .auto differential | 09-07 scope-rule §4 MOVED-WITHIN-PACKAGE, §5 RE-WRITE; 09-07 sync-collision: collision bill row 1; 09-07 BOTH §4: RE-DERIVED (ruling bd868d3fe) | — | — | — | — |
| 88 | `runtime/runtime2_impl.cs` | `runtime/runtime2.go` | touched-substantive | principal .auto — `runtime/runtime2.cs` | 09-07 scope-rule §4 MOVED-WITHIN-PACKAGE (mechanical) | — | — | — | — |
| 89 | `runtime/stubs_impl.cs` | `runtime/stubs.go` | touched-substantive | principal .auto — `runtime/stubs.cs` | §10 MEMBERS-REMOVED → RE-WRITE | — | — | — | — |
| 90 | `runtime/windows/lock_sema_impl.cs` | `runtime/lock_sema.go` | touched-substantive | principal .auto — `runtime/windows/lock_sema.cs` | §10 MIXED → RE-DERIVE | — | — | — | — |
| 91 | `runtime/windows/nanotime_impl.cs` | `runtime/nanotime.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 92 | `runtime/windows/os_windows_impl.cs` | `runtime/os_windows.go` | touched-substantive | principal .auto — `runtime/windows/os_windows.cs` | — | — | — | — | — |
| 93 | `runtime/windows/signal_windows_impl.cs` | `runtime/signal_windows.go` | touched-substantive | principal .auto — `runtime/windows/signal_windows.cs` | — | — | — | — | — |
| 94 | `runtime/windows/trace_impl.cs` | `runtime/trace.go` | touched-substantive | principal .auto — `runtime/windows/trace.cs` | — | — | — | — | — |
| 95 | `slices/slices_impl.cs` | `slices/slices.go` | touched-substantive | principal .auto — `slices/slices.cs` | — | — | — | — | — |
| 96 | `sync/atomic/type.cs` | `sync/atomic/type.go` | touched-trivial | .auto differential | — | — | — | — | — |
| 97 | `sync/atomic/value.cs` | `sync/atomic/value.go` | untouched | .auto differential | — | — | — | — | — |
| 98 | `sync/cond_impl.cs` | `sync/cond.go` | untouched | principal .auto — `sync/cond.cs` | — | — | — | — | — |
| 99 | `sync/mutex.cs` | `sync/mutex.go` | touched-substantive | .auto differential | §10 MIXED → RE-WRITE; 09-07 sync-collision: RE-WRITE (collision bill row 2); 09-07 BOTH §4: TWO deletions | — | — | — | — |
| 100 | `sync/once.cs` | `sync/once.go` | touched-substantive | .auto differential | §10 SIGNATURE → RE-DERIVE; 09-08 date-screen §3: base GENUINELY STALE | — | — | — | — |
| 101 | `sync/oncefunc.cs` | `sync/oncefunc.go` | untouched | .auto differential | — | — | — | — | — |
| 102 | `sync/pool.cs` | `sync/pool.go` | untouched | .auto differential | — | — | — | — | — |
| 103 | `sync/poolqueue.cs` | `sync/poolqueue.go` | untouched | .auto differential | — | — | — | — | — |
| 104 | `sync/runtime_impl.cs` | `sync/runtime.go` | touched-substantive | principal .auto — `sync/runtime.cs` | §10 MEMBERS-REMOVED → RE-WRITE | — | — | — | — |
| 105 | `sync/rwmutex.cs` | `sync/rwmutex.go` | touched-substantive | .auto differential | 09-08 date-screen §3: base GENUINELY STALE | — | — | — | — |
| 106 | `sync/waitgroup.cs` | `sync/waitgroup.go` | touched-substantive | .auto differential | — | — | — | — | — |
| 107 | `syscall/darwin/exec_libc2_impl.cs` | `syscall/exec_libc2.go` | untouched | principal .auto — `syscall/darwin/exec_libc2.cs` | — | — | — | — | — |
| 108 | `syscall/darwin/sockaddr_darwin_impl.cs` | `syscall/sockaddr_darwin.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 109 | `syscall/darwin/syscall_darwin_impl.cs` | `syscall/syscall_darwin.go` | untouched | principal .auto — `syscall/darwin/syscall_darwin.cs` | — | — | — | — | — |
| 110 | `syscall/linux/cgocaller_linux_impl.cs` | `syscall/cgocaller_linux.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 111 | `syscall/linux/exec_unix.cs` | `syscall/exec_unix.go` | untouched | .auto differential | 09-08 date-screen §3: base GENUINELY STALE | — | — | — | — |
| 112 | `syscall/linux/sockaddr_linux_impl.cs` | `syscall/sockaddr_linux.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 113 | `syscall/linux/structclass_linux_impl.cs` | `syscall/structclass_linux.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 114 | `syscall/linux/syscall_linux_amd64_impl.cs` | `syscall/syscall_linux_amd64.go` | touched-substantive | principal .auto — `syscall/linux/syscall_linux_amd64.cs` | §10 MEMBERS-REMOVED → RE-DERIVE | — | — | — | — |
| 115 | `syscall/linux/syscall_linux_impl.cs` | `syscall/syscall_linux.go` | touched-substantive | principal .auto — `syscall/linux/syscall_linux.cs` | — | — | — | — | — |
| 116 | `syscall/linux/zsyscall_linux_amd64_impl.cs` | `syscall/zsyscall_linux_amd64.go` | touched-substantive | principal .auto — `syscall/linux/zsyscall_linux_amd64.cs` | 09-07 scope-rule §4 MOVED-WITHIN-PACKAGE (mechanical) | — | — | — | — |
| 117 | `syscall/syscall_impl.cs` | `syscall/syscall.go` | untouched | principal .auto — `syscall/{windows,linux,darwin}/syscall.cs` | — | — | — | — | — |
| 118 | `syscall/windows/dll_windows.cs` | `syscall/dll_windows.go` | untouched | .auto differential | 09-08 date-screen §3: base GENUINELY STALE | — | — | — | — |
| 119 | `syscall/windows/exec_windows.cs` | `syscall/exec_windows.go` | untouched | .auto differential | 09-08 date-screen §3: base GENUINELY STALE | — | — | — | — |
| 120 | `syscall/windows/security_windows.cs` | `syscall/security_windows.go` | untouched | .auto differential | 09-08 date-screen §3: base GENUINELY STALE | — | — | — | — |
| 121 | `syscall/windows/syscall_windows_callback_impl.cs` | `syscall/syscall_windows_callback.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 122 | `syscall/windows/syscall_windows_impl.cs` | `syscall/syscall_windows.go` | touched-substantive | principal .auto — `syscall/windows/syscall_windows.cs` | — | — | — | — | — |
| 123 | `syscall/windows/zsyscall_windows_addrinfo_impl.cs` | `syscall/zsyscall_windows_addrinfo.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 124 | `syscall/windows/zsyscall_windows_certchain_impl.cs` | `syscall/zsyscall_windows_certchain.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 125 | `syscall/windows/zsyscall_windows_dnsrecord_impl.cs` | `syscall/zsyscall_windows_dnsrecord.go` (absent at both) | no-upstream-counterpart | principal .auto — principal not named · OQ-5 | — | — | — | — | — |
| 126 | `syscall/windows/zsyscall_windows_impl.cs` | `syscall/zsyscall_windows.go` | touched-substantive | principal .auto — `syscall/windows/zsyscall_windows.cs` | 09-07 scope-rule §4 MOVED-WITHIN-PACKAGE (mechanical) | — | — | — | — |
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
| 139 | `time/sleep_impl.cs` | `time/sleep.go` | touched-trivial | principal .auto — `time/sleep.cs` | — | — | — | — | — |
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
