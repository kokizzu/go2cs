# `os` — first-contact census at the release candidate (2026-08-29)

**Measurement only.** No fixes, no commits, no roster or doc changes. Run in a disposable worktree
detached at the RC SHA; every artifact named below lives in that worktree.

| | |
|---|---|
| Corpus | `773afa2c2` (release freeze, 189-row / 90.9% roster) |
| Toolchain | Go **1.23.12** (`C:\Users\ritchie\sdk\go1.23.12`), .NET **10.0.400**, `net10.0` |
| Host | i7-5820K (6C/12T Haswell-E), Windows 11, `windows/amd64` |
| Command | `go2cs -tests -test-action all -test-timeout 20m -go2cspath <wt>\src <GOROOT>\src\os <wt>\src\core\os` |
| Wall time | **~3 min** (15:40:08 → 15:43:05) — convert 1 s, build ~2 min, run ~10 s, compare |
| Artifacts | `os-census-comparison.json` (the run's `go2cs_test_comparison.json`), `os-census-pipeline.err.log`, `os-census-control-stdlib.log`, `os-census-tests-closure-diff.txt` |

`os` is **not a roster row** and never has been. The board record it has carried since 2026-08-14 is
*679 matched of 683* on a capable host (Go 1.23.1, .NET 9). This census re-measures that record across
**both** runtime pins moving at once.

---

## 1. Headline arithmetic

| Metric | Count |
|---|---|
| Verdict rows, Go side | **686** |
| Verdict rows, C# side | **686** |
| **Matching** | **682** (99.42 %) |
| Non-matching | **4** |
| — of which disclosed | 1 |
| — of which reported as errors | 3 |
| Reached / unreached | **686 of 686 reached — zero unreached** |
| Empty C# verdicts | **0** |
| Matching skips (both sides skip) | 20 |
| Capability-gated (never executed) | 3 |
| Excluded, Phase-4D deferral | 35 (12 benchmarks, 23 examples) |
| Package-level events | 1 (`{"package":"os","test":"","action":"fail"}` — the host rollup; pipeline exits 1) |

**Verdict-shape check passes.** Zero empty C# verdicts, so this is neither the truncated-run signature
(a contiguous alphabetical tail) nor the file-lock signature (all-empty). Positive cross-check against the
host's own JSON stream: 687 `run` records = 686 tests + 1 package-level; terminal actions
662 pass + 21 skip + 3 fail + 1 infrastructure-error = 687 = 686 tests + 1 package rollup. Every test that
started, finished. **No MSB4166, no MSB3027/3021, zero `error CS####` anywhere in the log** — the suite
built and ran clean.

### Against the standing board record

| | Board, 2026-08-14 (Go 1.23.1 / .NET 9, capable host) | This census (Go 1.23.12 / .NET 10) |
|---|---|---|
| Verdict rows | 683 | **686** |
| Matched | 679 | **682** |
| Divergent rows | 4 | **4 — the same four** |

**No regression across either runtime pin, and no new divergence.** The +3 rows are all matching. Their
attribution to the Go 1.23.12 test sources is plausible but **not measured** — I did not diff the 1.23.1
and 1.23.12 `_test.go` sets.

---

## 2. Bucket table

| Bucket | Count | Members | Status |
|---|---|---|---|
| **syscall-seam — non-blittable struct handed to the kernel by address** | 2 | `TestNetworkSymbolicLink`, `TestDirectorySymbolicLink` | one **rooted** (declared host limit), one **candidate root, newly proposed** |
| **alloc-count semantics — zero-bound assert** | 1 | `TestWriteStringAlloc` | **rooted** as an architectural arc; re-measured here, improved |
| **alloc-count semantics — nonzero-count assert** | 1 | `TestUTF16Alloc` | **disclosed**; the disclosure's stated reason is now **stale** |
| **capability-gated — never executed** | 3 | `TestCmdArgs`, `TestDirectoryJunction`, `TestRemoveAllWithExecutedProcess` | pre-existing, recorded |

There is **no** path-shape bucket, **no** timing bucket, **no** panic-text bucket, **no** file-handle-
semantics bucket and **no** process-spawn bucket. Everything `os` gets wrong on this host is the syscall
seam or an allocation assert. That is a materially narrower first-contact profile than the package's
history suggests.

---

## 3. Exact examples

### 3.1 `TestNetworkSymbolicLink` — ROOTED (prior measurement), re-confirmed verbatim

Go said: **`pass`**. C# said: **`infrastructure-error`**.

```
System.NotSupportedException: internal/syscall/windows: NetShareAdd is not supported by the converted
runtime — SHARE_INFO_2 carries managed references, so the CLR auto-layouts it 48 bytes with the
references grouped first, and netapi32 would dereference the integer 1 as shi2_path (access violation).
The buffer reaches this wrapper as a raw address with its managed identity already discarded, so the
blittable-mirror remedy used elsewhere in this class cannot be applied here.
   at go.internal.syscall.windows_package.NetShareAdd(ж`1 Ꮡ serverName, UInt32 level, ж`1 Ꮡbuf, ж`1 ᏑparmErr)
      in ...\src\core\internal\syscall\windows\windows\zsyscall_windows_impl.cs:line 108
   at go.os_test_package.TestNetworkSymbolicLink(ж`1 Ꮡt) in ...\src\core\os\os_windows_test.cs:line 560
```

This is a **deliberate declared limit**, not a fresh defect — the wrapper raises by design and names its
own board entry (`RETRACTED — os's REGRESSION is a HOST CAPABILITY, and the killer is SHARE_INFO_2`,
2026-08-14). Its presence here confirms the guard still fires on this host. Note the board's qualifier
still applies: **`os` is measurable at all only because this short-circuits.** On a host with the Server
service reachable and the wrapper absent, the process used to die at test ~32.

### 3.2 `TestDirectorySymbolicLink` — the row the board records as NOT ATTRIBUTED. A candidate root is offered below.

Go said: **`pass`**. C# said: **`skip`**, with:

```
skipping some tests, could not enable "SeCreateSymbolicLinkPrivilege":
Not all privileges or groups referenced are assigned to the caller.
```

That message is `ERROR_NOT_ALL_ASSIGNED` (1300) surfaced from `AdjustTokenPrivileges`.

**What the failure path proves by itself (rooted by measurement).** `os_windows_test.go:369` runs, in
order: `windows.ImpersonateSelf` (would `t.Fatal` on error — it did not, so it **succeeded**),
`enableCurrentThreadPrivilege` → `windows.GetCurrentThread`, `windows.OpenThreadToken`,
`syscall.UTF16PtrFromString`, `windows.LookupPrivilegeValue`, `windows.AdjustTokenPrivileges`. Each of the
first five returns early on error with its own message. The message we got is the **last** call's. So
impersonation, thread-token open and privilege lookup all reported success, and the kernel rejected the
adjust. Go, in the same pipeline run, same user, same session, same elevation, **passed**.

**Candidate root — structurally measured, behaviourally unproven.** `TOKEN_PRIVILEGES` is the exact shape
the blittable-mirror class is defined by. Go
(`C:\Users\ritchie\sdk\go1.23.12\src\internal\syscall\windows\security_windows.go`):

```go
type LUID struct { LowPart uint32; HighPart int32 }
type LUID_AND_ATTRIBUTES struct { Luid LUID; Attributes uint32 }
type TOKEN_PRIVILEGES struct {
	PrivilegeCount uint32
	Privileges     [1]LUID_AND_ATTRIBUTES
}
```

Emitted C# (`src\core\internal\syscall\windows\windows\security_windows.cs`):

```csharp
[GoType] partial struct TOKEN_PRIVILEGES {
    public uint32 PrivilegeCount;
    public array<LUID_AND_ATTRIBUTES> Privileges = new(1);
}
```

Native layout is 4 bytes of count then **12 inline bytes** of `LUID_AND_ATTRIBUTES`. The converted struct
holds a golib `array<T>` — a **managed reference** — where advapi32 expects that inline record. And the
wrapper is **auto-generated, not hand-owned**; `src\core\internal\syscall\windows\windows\zsyscall_windows.cs:104`
hands the kernel the raw address of that managed object:

```csharp
var (r0, _, e1) = syscall.Syscall6(procAdjustTokenPrivileges.Addr(), 6, (uintptr)token,
    (uintptr)_p0, (uintptr)Ꮡnewstate, (uintptr)buflen, (uintptr)Ꮡprevstate, (uintptr)Ꮡreturnlen);
```

A LUID read out of a reference field's bytes is not a privilege LUID, and `ERROR_NOT_ALL_ASSIGNED` is
exactly what advapi32 answers for a LUID it does not recognise. **This is textbook the syscall
STRUCT-PASSING seam**, whose board definition reads: *"any struct holding a golib `array<T>` (Go's inline
`[N]T`) … where Windows expects inline bytes."*

**A second, independent exposure sits in the same helper** and I cannot separate the two from this run's
evidence. `LookupPrivilegeValue` receives a nested-field address into the same struct
(`src\core\os\os_windows_test.cs:379`):

```csharp
err = windows.LookupPrivilegeValue(nil, privStr,
        Ꮡtp.at(windows.TOKEN_PRIVILEGES.ᏑPrivileges, 0).of(windows.LUID_AND_ATTRIBUTES.ᏑLuid));
```

and `zsyscall_windows.cs:135` passes `(uintptr)Ꮡluid` to the kernel. If that address does not alias `tp`'s
storage, the LUID lands in a detached box and `tp` keeps a zero LUID — same observable outcome, different
remedy layer (`**T`/out-parameter mechanism rather than a mirror).

**Honest labelling: this is grouped-by-symptom plus a measured layout mismatch. It is NOT rooted.** Neither
hypothesis was tested by instrumenting the call. A rooting lane's cheapest first move is to print
`tp.PrivilegeCount` and `tp.Privileges[0].Luid` immediately before `AdjustTokenPrivileges` — a zero LUID
indicts `LookupPrivilegeValue`, a correct LUID with the same kernel error indicts the struct-passing seam.

One alternative was considered and is **disfavoured by the evidence**: a `runtime.LockOSThread()` no-op
letting impersonation and adjustment land on different threads. `OpenThreadToken` succeeded, which
requires a thread impersonation token to exist on the calling thread, so thread affinity held.

### 3.3 `TestWriteStringAlloc` — ROOTED as an arc; re-measured, materially improved

Go said: **`pass`**. C# said: **`fail`**:

```
go2cs: testing.AllocsPerRun counted 1,700 go2cs-runtime object allocations (118,464 bytes) over 100 run(s)
— the figure reported above is an allocation COUNT per run, from go2cs's own runtime counter (golib's
allocation sites), which is the structural mirror of Go's runtime.MemStats.Mallocs. …
expected 0 allocs for File.WriteString, got 17
```

Per run: **17 allocations, 1,184.6 bytes**. The board's last recorded figure for this row (r39-osalloc,
2026-08-03) was **3,168 B/op**, printed then as `got 3168` because the host reported **bytes**. The host now
reports a genuine **count**, and bytes/op have fallen ~63 % since that record. The row is still non-zero and
the board's ruling stands: it is an architectural arc (ж-box reduction and friends), **not** a disclosure —
a zero bound is a bound a correct implementation could meet.

### 3.4 `TestUTF16Alloc` — disclosed, and the disclosure text no longer describes the instrument

Go said: **`pass`**. C# said: **`fail`**, disclosed by `os/go2cs_test_disclosures.json` on signature
`" allocs, want "`:

```
go2cs: testing.AllocsPerRun counted 20 go2cs-runtime object allocations (1,400 bytes) over 5 run(s) …
got 4 allocs, want 1
```

**Census observation, offered as an open question rather than a finding.** The disclosure's stated reason is:

> *"the managed shim is deliberately byte-derived (the CLR exposes no malloc counter), so a NONZERO count
> assert can never agree whatever the allocation behavior - measured 432 and 128, which are BYTES allocated
> per call, not mallocs"*

That reasoning describes an instrument that no longer exists. The host's own message now states the figure
is *"an allocation COUNT per run … the structural mirror of Go's `runtime.MemStats.Mallocs`"*, and it reports
**4 vs want 1** — a same-units comparison. The disclosure still *matches mechanically* (the signature is
unchanged), but the units-mismatch argument that justified it has been overtaken by the allocation-counting
work. Whether the row should now be re-classified as a real (small) divergence, kept with rewritten prose, or
kept as-is, is a **ruling**, not a lane call. Flagging it because a disclosure whose reason has gone stale is
exactly the kind of thing that quietly launders a real divergence.

### 3.5 The three capability-gated tests — never executed, pre-existing

| Test | Declared missing capability |
|---|---|
| `TestCmdArgs` | native output block with caller-side `LocalFree` (`syscall.CommandLineToArgv`) |
| `TestDirectoryJunction` | raw-metal struct overlay on managed bytes (`os_test.createMountPoint`, in TEST code that cannot be hand-owned) |
| `TestRemoveAllWithExecutedProcess` | relocatable single-file test executable (the .NET deployment model, rooted 2026-08-02 as environment divergence) |

These are **not** measured verdicts. They are excluded before execution and appear in the comparison's
`gated`/`excluded` arrays, not in the 686.

### 3.6 Every historically-troubled `os` row now passes

Spot-checked against the board's residual tables from the r35–r39 arc — all `pass`/`pass` on this run:

`TestNilFileMethods`, `TestReadStdin`, `TestReadlink`, `TestGetppid`, `TestRootDirAsTemp`,
`TestStartProcess`, `TestStartProcess/relative`, `TestPipeEOF` (the r36 lone-unreached row).
`TestStatLxSymLink` is `skip`/`skip` — a **matching** skip, not the old sharing-violation failure.

The load-sensitive child-process flake cluster the board recorded (`TestFileReaddir/TempDir`,
`TestLongPath`, `TestHostname`, `TestExecutable`, `TestStatStdin`) also came back clean, on a machine that
had a sibling lane active. One run is not a flakiness retirement, but nothing in that cluster fired.

### 3.7 The 20 matching skips (both sides skip — no action)

`TestClosedStat`, `TestCopyFSWithSymlinks`, `TestDirFSPathsValid`, `TestDirFSReadFileProc`,
`TestExecutableDeleted`, `TestLargeWriteToConsole`, `TestMkdirAllAtSlash`, `TestMkdirAllExtendedLengthAtRoot`,
`TestMkdirAllVolumeNameAtRoot`, `TestPipeThreads`, `TestProcessLiteral`, `TestReadFileProc`,
`TestReadNonblockingFd`, `TestReaddirStatFailures`, `TestRemoveAllButReadOnlyAndPathError`,
`TestRemoveAllLongPath`, `TestRemoveAllNoFcntl`, `TestRemoveAllRace`, `TestStatLxSymLink`, `TestStdPipe`.

---

## 4. Which existing remedy families apply

| Family | Applies to | Notes |
|---|---|---|
| **Blittable-mirror / syscall STRUCT-PASSING seam** | `TestDirectorySymbolicLink` (candidate) | The remedy is the established one: a `[StructLayout(Sequential)]` mirror with `fixed` buffers for the inline array, a direct `[DllImport]`, a field-for-field copy at the boundary, declared in `manualConversionFuncs`. ⚠ **`internal/syscall/windows.adjustTokenPrivileges` is not in the standing 6-wrapper census, which is scoped to `src/core/syscall` — and the board already warns that scope "is not the class's boundary." This census proposes it as a member.** |
| **Blittable-mirror — explicitly ruled INAPPLICABLE** | `TestNetworkSymbolicLink` | The wrapper itself says so: the buffer arrives as a raw address with its managed identity discarded. Handled as a declared limit instead. |
| **`**T` OUT-parameter class** | `TestDirectorySymbolicLink` (the second, alternative exposure) | Only if the fault turns out to be `LookupPrivilegeValue`'s LUID out-parameter rather than the struct pass. The class mechanism — a native cell local to the call, published through `ValueSlot` — would be the shape. |
| **Byte-buffer-reinterpret fork** | none in this run | `os.readReparseLink` and `os`'s `readdir` already took it (`file_windows_impl.cs`, `windows/dir_windows_impl.cs`); both are quiet here. `TestDirectoryJunction` is this fork's stub arm and is capability-gated. `syscall.Readlink` remains the recorded LATENT member and was **not** reached. |
| **Process-spawn plumbing** | **none — and the premise needs correcting** | The tasking cited "testenv/exec process-spawn plumbing proven by `debug/pe` `TestDWARF`". **No such attribution exists in the docs.** `TestDWARF` appears only as a passing row on `docs/validation/current/debug.pe.md`; the only `TestDWARF`-adjacent prose is `debug/elf`'s `TestDWARFRelocations`, and it is about a `.gitignore` banking trap, not spawning. The three real process-spawn remedies are: the **testenv-shaped project-reference** fix (`aliasReferenceImports` suffix match — proven by `go/doc/comment` and `internal/abi`); the **Windows host-argv** fix (`src/core/testing/TestOptions.cs` — proven by `os/exec`, 48 → 74 agreeing); and the **Linux `posix_spawn` at the forkExec seam** (`DESIGN-linux-exec.md`). None of them is implicated here: **`os`'s process-spawn tests all pass** — `TestStartProcess`, `TestStartProcess/relative`, `TestKillStartProcess`, `TestKillFindProcess`, `TestGetppid`, `TestExecutable` are all `pass`/`pass`. |
| **Alloc-count semantics** | `TestWriteStringAlloc` (not disclosable — zero bound), `TestUTF16Alloc` (disclosed) | Ruling #1 of 2026-08-02 stands. §3.4 asks whether the second one's *reason* survives the counter rewrite. |

---

## 5. Incidental finding — unbanked converter drift in `os`'s `package_info.cs` (ROOTED by control)

The run left three tracked files modified. Two are the documented standing classes; **one is not.**

| File | Diff | Classification |
|---|---|---|
| `src/core/os/windows/exec_windows.cs` | empty numstat, LF↔CRLF only | **CRLF phantom** (class 1). Restore. |
| `src/core/os/windows/package_init.cs` | **+7** — adds `initᴛᴛtests();` and the `static partial void initᴛᴛtests();` declaration | **`-tests`-closure production drift, shape 4** (the `initᴛᴛtests` hook named in CLAUDE.md 2026-08-17). Standing restore. |
| `src/core/os/windows/package_info.cs` | **4/4** — four `GoPositionMap` records gain the optional 5th `funcLits` argument | **Unbanked converter drift — NOT closure drift.** |

**Rooted by a control, not assumed.** I restored `src/core/os` to HEAD and ran a **production-only**
conversion (`go2cs -comments -go2cspath <wt>\src <GOROOT>\src\os <wt>\src\core\os`, exit 0, no `-tests`).
It reproduced the `package_info.cs` change **byte-identically** and did **not** produce the
`package_init.cs` hook. So the funcLits argument is emitted by the ordinary `-stdlib`-shaped path and the
committed `os` corpus simply predates it.

Example (`os/dir.go`):

```
-[assembly: go.GoPositionMap("os/dir.go", "dir.cs", "ADVQ…CgqQ=")]
+[assembly: go.GoPositionMap("os/dir.go", "dir.cs", "ADVQ…CgqQ=", "126-128:1;150-190:1")]
```

The four records affected are `os/dir.go`, `os/file.go`, `os/file_posix.go`, `os/file_windows.go`. The 5th
argument is documented in golib as *"the encoded function-literal name map, or an empty string when the file
declares no recorded literals."*

**Corpus-wide extent NOT measured.** The only datum I have: at HEAD, **3 of 360** tracked
`src/core/**/package_info.cs` carry a 5-argument record (`debug/pe`, `internal/trace/internal/oldtrace`,
`net/http/cookiejar`). That is consistent with either "a rare feature only some files trigger" or
"three packages regenerated since the feature landed, and the rest are latently drifted." **Distinguishing
those needs a seeded whole-corpus reconvert, which I did not run.** If it is the latter, this belongs on
`INVENTORY-unbanked-regen-drift.md` and is a pre-bank item for anything that regenerates.

---

## 6. What I did NOT measure

- **Any other platform.** Windows/amd64 only. No Linux, no darwin.
- **A second run.** One pipeline invocation. The board's history for `os` includes a host-killer whose
  crash site *moved between runs*; a single clean run does not by itself retire flakiness, though the
  zero-unreached, zero-empty shape is strong evidence the run was whole. **Re-running once more before
  anyone banks on this is cheap and advisable.**
- **The root of `TestDirectorySymbolicLink`.** §3.2 offers a candidate with a measured layout mismatch and
  a named discriminating experiment. It is not rooted.
- **Whether `os` could bank.** Not my call and not measured against roster rules. For the record, the
  arithmetic a banker would face: 682 matching + 1 disclosed, with 3 rows outstanding — 1 declared host
  limit, 1 unattributed, 1 architectural arc.
- **The 1.23.1 → 1.23.12 test-source diff**, so the +3 verdict rows are unattributed.
- **Corpus-wide funcLits drift** (§5) — `os` only.
- **The gated three.** They did not execute; nothing here says whether their gates are still correctly
  drawn.
- **Benchmarks and examples** (35 rows), deferred to Phase 4D by design.
- **`syscall.Readlink`'s latent byte-buffer defect** — recorded on the board, not reached by this suite.
- **`os/exec`, `os/signal`, `os/user`** — separate packages, out of scope. (`os/user` remains class E2:
  Go's own `TestGroupIds` fails in the oracle on this host class.)

---

## 7. One-line summary for the board

> **`os` first-contact at the RC: 686 verdict rows, 682 matching (99.42 %), zero unreached, zero empty —
> the standing 679-of-683 board record re-measured across BOTH runtime pins (go1.23.12 + net10.0) with the
> same four divergent rows and no new ones. Two are the syscall struct-passing seam
> (`TestNetworkSymbolicLink`, a declared limit; `TestDirectorySymbolicLink`, previously unattributed — this
> census proposes `internal/syscall/windows.adjustTokenPrivileges` passing a managed `array<T>` where
> advapi32 wants 12 inline bytes, and names the experiment that would settle it), two are allocation
> asserts (`TestWriteStringAlloc` improved 3,168 → ~1,185 B/op and is still the arc; `TestUTF16Alloc` stays
> disclosed but its stated reason no longer describes the counter). Incidental and rooted by control:
> `os`'s committed `package_info.cs` carries unbanked converter drift (the `funcLits` position-map
> argument), which is NOT the `-tests` closure class and would need banking, not restoring.**

---

## AMENDMENT — 2026-09-01: re-measured at master `bfc63d487` for a bank attempt. The seam row settled; the package still does NOT reach bankable shape, on two rows that standing doctrine refuses to disclose.

**Measurement only, again.** No converter change, no roster row, no proof page. Nothing from either
run is banked except this amendment and the `TestUTF16Alloc` prose refresh ruled on 2026-08-29.

| | |
|---|---|
| Corpus | `bfc63d487` (master; the symlink-privilege arc `868c6d3ed` is merged) |
| Toolchain | Go **1.23.12**, .NET **10.0.400**, `net10.0`, `windows/amd64` |
| Host | i7-5820K (6C/12T Haswell-E), Windows 11 — same machine class as the first contact |
| Command | `go2cs -tests -test-action all -test-timeout 10m -go2cspath <wt>\src <GOROOT>\src\os <wt>\src\core\os` |
| Wall time | **2 min 46 s** (run 1), **run 2 re-run as the manifest control** |
| Exit | 1, from the package rollup — the expected shape for a suite with any divergence |

### A.1 Headline arithmetic, and every delta from the first contact explained

| Metric | Census 2026-08-29 | This run | Delta |
|---|---|---|---|
| Verdict rows (both sides) | 686 | **685** | **−1** — `TestDirectorySymbolicLink` left the measured set |
| Matching | 682 | **682** | 0 |
| Non-matching | 4 | **3** | −1, same cause |
| — disclosed | 1 | **1** | 0 |
| — undisclosed residual | 3 | **2** | −1, same cause |
| Capability-gated (never executed) | 3 | **4** | **+1**, same cause |
| Matching skips | 20 | **20** | 0 |
| Empty C# verdicts | 0 | **0** | 0 |
| Rows present on one side only | 0 | **0** | 0 |
| Excluded (benchmarks + examples) | 35 (12 + 23) | **35 (12 + 23)** | 0 |

**The single delta is one row changing bucket, and it is the merged arc's own predicted arithmetic.**
`868c6d3ed` states it in advance: *"Before: 686 measured / 682 matching / 4 non-matching / 3 gated.
After: 685 measured / 682 matching / 3 non-matching / 4 gated."* That is reproduced here exactly.

Closure, to the digit:

```
matched 682  +  disclosed 1  +  undisclosed residual 2   =  685 measured
measured 685 +  gated 4      +  benchmarks/examples 35   =  724 declared rows
Go side  : 665 pass + 20 skip                            =  685
C#  side : 662 pass + 20 skip + 2 fail + 1 infra-error   =  685
```

**Verdict-shape check passes.** Zero empty C# verdicts, so this is neither the truncated-run
signature (a contiguous alphabetical tail) nor the file-lock signature (all-empty). The comparison
record's TAIL was read FIRST and carries **no `action":"timeout"` event** — the deadline did not
fire, and the `-test-timeout` was an explicit `10m`, never the 2m default.

### A.2 The four rows, re-classified against today's tree

**(a) `TestDirectorySymbolicLink` — SETTLED, and it is neither passing nor privilege-blocked.** It is
now **capability-gated** and never executes: `gated` carries
`{"name": "TestDirectorySymbolicLink", "capabilities": "raw-metal struct overlay on managed bytes"}`,
from the `os_test.createSymbolicLink` entry that merged with the seam fix. The privilege half of
§3.2's candidate root was **confirmed and repaired** — root (A), the struct-passing seam, proven at
the byte level and fixed by the `adjustTokenPrivileges` blittable mirror; root (B), the detached
LUID out-parameter, refuted by the same probe. §3.2's proposed root was therefore correct.

Two things this run cannot say, stated rather than glossed: because the gate is applied at
CONVERSION time the row never runs, so **this run does not itself re-measure the privilege grant**
— that measurement is `ca24bcbab`'s own probe (`AdjustTokenPrivileges err = <nil> — privilege
GRANTED`). And **no elevation or Developer-Mode question arises on this host at all**: the old
`ERROR_NOT_ALL_ASSIGNED` message that blamed the machine is gone, which was the entire point of the
fix. The gate carries its own retirement note and must retire with the byte-buffer-reinterpret arc.

**(b) `TestNetworkSymbolicLink` — unchanged, and NOT authored as a disclosure here.** Go=`pass`,
C#=`infrastructure-error`, verbatim the declared `NetShareAdd` / `SHARE_INFO_2` limit. It is left
undisclosed deliberately, because on the evidence it does not clear the `host-limit` bar the roster
itself sets: *"an entry must name a structural property of the deployment shape, never an
unimplemented-but-fixable defect."* This is not a deployment-shape property — it is a converted
wrapper whose remedy is named and whose board entry is **OPEN**, and the gate comment 20 lines away
in `testConversion.go` says so in the same breath (*"the durable remedy is the byte-buffer-reinterpret
fork at the CONVERTER level — the same arc that owes NetShareAdd its repair"*). The board's own
ratification of remedy 2 called the result *"a real mismatch rather than a skip … an honest
mismatch — Go passes it."* Disclosing it would convert a measured, open arc into a declared limit,
which is the laundering the class's bar exists to prevent. **Ruling owed; not a lane call.**

**(c) `TestWriteStringAlloc` — re-measured, still non-zero, still not disclosable under ruling #1.**

```
go2cs: testing.AllocsPerRun counted 1,700 go2cs-runtime object allocations (132,064 bytes)
over 100 run(s) …
expected 0 allocs for File.WriteString, got 17
```

Per run: **17 allocations, 1,320.64 B/op.**

| Record | allocs/run | B/op |
|---|---:|---:|
| r39-osalloc, 2026-08-03 | — | 3,168 |
| First contact, 2026-08-29 | 17 | 1,184.64 |
| **This run, 2026-09-01** | **17** | **1,320.64** |

⚠ **The byte figure ROSE by exactly 136.00 B/op while the count did not move.** Flagged, not rooted:
136 B is precisely the figure the arc's own item 5 carries (*"`unsafe.StringData` pins eagerly
(136 B)"*), and `unsafe.StringData` was changed in the interval (`e1ef6ca85`, merged `67d875d11`,
which removed an unconditional pinned `GCHandle`). Whether that is the same 136 bytes reappearing
elsewhere, or an unrelated coincidence of magnitude, is **UNMEASURED** — no decomposition was run.
It is recorded because a silent 11.5 % regression on the one row that blocks this package's bank is
worth someone's attention even when it does not change the verdict.

The classification is unchanged and is **not** the measuring lane's to change: **owner ruling #1 of
2026-08-02** holds that *"a want-zero alloc assert is satisfiable, so disclosing it would soften the
doctrine the badges depend on"*, and records that refusing the disclosure is exactly what forced the
9,208-byte figure to be decomposed rather than argued about — finding two silent `ж<T>` allocations
that every pointer read in the corpus was paying. The coordinator re-affirmed it on 2026-08-29
(*"Still a zero bound, still an arc, not a disclosure"*). **Overturning it is an owner ruling.**

**(d) `TestUTF16Alloc` — disclosed, and the ruled prose refresh is CARRIED by this amendment's
commit.** Measured `got 4 allocs, want 1` — 20 allocations / 1,440 bytes over 5 runs = **4 allocs,
288 B/op** (census: 4 allocs, 280 B/op; the count has not moved). The 2026-08-29 ruling — *"stays
disclosed, prose rewritten at os's bank to the honest current reason (managed allocation-profile
overhead, alloc-profile class)"* — is implemented in `src/core/os/go2cs_test_disclosures.json`: the
class moves `alloc-count-semantics` → `alloc-profile`, and the retired units-mismatch argument is
replaced by the structural one, which Go's own source makes plain — `syscall.UTF16ToString([]uint16{'a','b','c'})`
allocates one object in Go because escape analysis keeps the array literal in the frame, where the
managed model must heap-allocate that array and its slice descriptor before the produced `@string`
is counted. **The signature is deliberately unchanged** (`" allocs, want "`), so the mechanical
match is preserved; run 2 is the positive control that it still absorbs the row.

### A.3 What the tree still carries — classified by control, and all of it restored

Both runs left the same five tracked files modified. A **production-only control**
(`go2cs -comments -go2cspath <wt>\src <GOROOT>\src\os <wt>\src\core\os`, exit 0, no `-tests`) was run
against a restored tree, and the two diffs are **byte-identical except for one file**:

| File | Diff | Classification |
|---|---|---|
| `src/core/os/os.csproj` | **+2** — `<InternalsVisibleTo Include="go2cs.SynthesizedStructs" />` | **Unbanked converter drift.** Reproduced by the production-only control, so NOT the `-tests` closure class and NOT the IP-4 test-artifact exclusion (which is already committed). |
| `src/core/os/windows/exec.cs` | **3/3** — `(Func<…>)(Release)` → `((Func<…>)(Release))` | **Unbanked converter drift**, same control. |
| `src/core/os/windows/file_windows.cs` | **1/1** — same extra-parenthesized delegate cast | **Unbanked converter drift**, same control. |
| `src/core/os/windows/exec_windows.cs` | empty numstat, LF↔CRLF only | **CRLF phantom** (class 1). |
| `src/core/os/windows/package_init.cs` | **+7** — `initᴛᴛtests();` and its `static partial void` declaration | **`-tests`-closure drift, shape 4.** The ONLY file the control did not reproduce. Standing restore while `os`'s test sources are unbanked. |

**All five restored to HEAD; the ~25 untracked test artifacts removed.** Nothing here is banked,
because the validated-package commit policy commits test sources when a package *validates*, and
`os` does not.

⚠ **§5's `funcLits` finding is CLOSED and needs no further action** — the four `package_info.cs`
position-map records it reported did **not** reappear in either run. The regen train (`f1df6cbd9`,
three-target seeded reconvert) banked them corpus-wide in the interval, which also settles §5's open
question — it was *"three packages regenerated since the feature landed"*, and the rest have since
been levelled — in the rare-feature-vs-latent-drift dichotomy §5 could not decide. Two *new* members
joined the same family in its place (the `SynthesizedStructs` grant and the delegate-cast parens
above), so the family is alive; only this instance of it is gone.

### A.4 Verdict

**`os` does not bank today, and the blocker is doctrine rather than measurement.** The suite is in
excellent shape — 682 of 685 matching (99.56 %), zero unreached, zero empty, identical across two
runs — and the row the first contact could not attribute has been rooted, fixed and settled. What
stands between it and the roster is two rows that standing doctrine explicitly declines to disclose:
one an open converter arc that `host-limit`'s own bar excludes (b), one an owner ruling that only the
owner can reopen (c). Both need a decision, and neither is a measurement problem.


---

## AMENDED 2026-09-01 (the +136 attribution run, coordinator sub-agent) — §A.2(c)'s hypothesis REFUTED; two-step decomposition reconciled to the byte

The +136.00 B/op on TestWriteStringAlloc (1,184.64 → 1,320.64, count 17 unchanged) is NOT the
item-5 eager pin returning — that match was magnitude coincidence. Same-host A/B, full interval,
unfiltered suite scope, endpoints reproducing the census figures exactly:

- **+112.00 at `a6b951a55`** (the element-aliasing merge; golib `3598acf5d` adds the 8-byte
  `m_publishedArrayBacking` field to the abstract base `ж<T>` — the per-box publish gate that took
  the materialization race from 261/300 to 0/300). The tax is **8 B × boxes allocated**,
  proportional not flat: 14 boxes on this path (+112), one on TestUTF16Alloc (+8.00, measured same
  runs), zero on unicode/utf16 TestAllocationsDecode (+0.00, measured). Correctness-load-bearing;
  not revertible.
- **+24.00 at `e1ef6ca85`** (StringData's eager pin DELETED — it was a leak class via the pinned
  handle's strong root and broke Go's sub-string aliasing by materializing offset copies;
  `StringDataIdentity` guards the repair). The correct interior-pointer window costs a 56 B boxed
  `slice<byte>` where the 32 B `PinnedBuffer` used to sit. Correctness-driven; not revertible.
- **±0.64** — one 64-byte one-off inside the 100-iteration window, non-monotone across history,
  cancels between the endpoints. Jitter, not a step.

**Breadth: no banked or disclosed verdict at risk** — every alloc assert in the corpus is a COUNT
(counts moved nowhere measured: 17→17, 4→4, 10→10) and no disclosure signature pins a
go2cs-produced byte figure (all 32 manifests swept).

**The arc's first increment (proposed by the run, queued):** an `ElemRefBox<T>` internal
`(T[] backing, nint absoluteIndex)` ctor removes one object + ~56 B per StringData call — the row
lands BELOW the pre-regression figure (17 → 16 allocs) with no change to identity, aliasing or
lifetime. Owes GolibTests + `go2cs.slnx` + the String/Unsafe behavioral filters + StringDataIdentity.

**Instrument law (measured here, +167.04 between scopes on ONE tree):** an alloc row's B/op is
comparable only against a figure taken at the same suite scope — `AllocsPerRun`'s single warmup
does not cover one-time costs a full run has already paid, so a filtered census must never compare
its bytes against a full-run record.

## AMENDED 2026-09-04 — the SECOND instrument law, beside the first: a 100-run `AllocsPerRun` sample cannot resolve a change under ~150 B/run, so quote the FLOOR or a high-`runs` figure and name the unit AND the configuration

Nothing above is rewritten. The law above governs **which two figures may be compared** (same suite
scope). This one governs **whether a single figure is a measurement at all**, and it was paid for by
this very row: §3.3's `TestWriteStringAlloc` has been quoted at 1,184.6, 1,320.64, 1,457.8 and 1,510.8
B/run across four records, and the movement between three of those was attributed to the
Debug/Release × tiered/TC0 configuration axis. It was not. Measured at `26ff0c45b` by a per-frame byte
probe (Release + `DOTNET_TieredCompilation=0`, 1,000,000 runs; 40 reps of 100 runs per cell for the
A/B, **minimum taken**; mailbox `2f77a03d0`, coordinator dispositions `a8f4525f4`; full decomposition
on the BOARD under *2026-09-04 — the `os` want-zero row has a FLOOR*):

**Sampling law.** `AllocsPerRun`'s byte reading carries allocation-accounting slop with a **fixed
per-window term that does not scale with `runs`** — on this row it ranges 0–800 B/run at 100 runs and
is under 1.5 B/run at 1,000,000, concentrating in whichever segment pins a freshly allocated object
every run. So **a single 100-run sample cannot resolve a change smaller than about 150 B/run.** Quote
the **FLOOR** — the minimum over repetitions, the slop-free draw — or a high-`runs` figure, and SAY
which, because a floor-derived figure and a draw-derived figure are not comparable in either
direction. A reduction claim that compares one against the other is measuring the sampler, not the
cut. (This row's floor is **1,320.00 B/run at 17.00 objects**, identical in Release/TC0, Debug/tiered
and Debug/TC0; only Release/tiered differs, at 1,256.00, and by exactly one box that tier-1 escape
analysis stack-allocates.)

**Unit-and-configuration law, its companion.** golib's `AllocationCounter` charges the object at the
`new`, so on a tiered runtime the COUNT and the BYTES **diverge**: the same box reported 1.00 object
and 0.28 B/run once tier-1 stack-allocated it. Every allocation claim on this family therefore names
its **UNIT** (count or bytes) and its **CONFIGURATION**. This is not bookkeeping — `AllocsPerRun`
reports the COUNT on a want-zero row, so no JIT improvement can satisfy such an assert; only not
constructing the object can.
