# FINDING — the Linux run layer: what the first running corpus actually hits

> Lane r52b, 2026-08-08. Companion to
> [`DESIGN-multiplatform-corpus.md`](DESIGN-multiplatform-corpus.md) (increment 4 proved RID packaging
> and produced the first-ever Linux run). This file records the run-layer loop that followed it: what
> failed, what class each failure was, and where the loop hit its wall.
>
> **Status: THE WALL IS DOWN.** Lane r53a implemented the §4 keystone under the user's option-A ruling
> and resumed the loop; `fmt.Println("hello, 世界")` and a second program exercising `os.Args` +
> `os.Getenv` + `time.Now()` both run on Linux with output **byte-identical** to `go run` on the Linux
> toolchain. The loop log and the four failures it cleared are §7; §4 is kept as the record of what the
> wall was, and §6's three open questions are now answered there.

## 1. The loop's harness (use this, not the nupkg feed)

Increment 4 drove its Linux run through a local **nupkg feed**, because proving RID packaging *was*
the point. For run-layer iteration that pack step is pure overhead. This loop instead built the
scratch app against the corpus by **project reference**:

```
dotnet build hello.csproj -c Release -p:go2csPath=<repo>/src/ -p:GoTargetOS=linux \
             -p:UseSharedCompilation=false
# then, under WSL:
dotnet /mnt/c/.../bin/Release/net9.0/hello.dll
```

Managed IL is portable, so no RID and no publish are needed — `GoTargetOS=linux` is what selects the
per-GOOS sources, and that is the only platform decision in the build. Two practical wins: a rebuild
after a converter change is ~60 s instead of a full pack cycle, and the stack traces carry **source
file and line** (`... at go.syscall_package..cctor() in .../syscall/linux/env_unix.cs:line 18`), which
is what made each root cause a two-minute job rather than a bisect.

Reference: Go's own output on the same box, `~/golang/bin/go` (go1.23.1 linux/amd64).

### ⚠ Do not bank a `.csproj` produced by a single-package conversion into an L3 corpus

Converting one package with a single `-platforms` target to iterate quickly is fine for the `.cs`, but
its **`.csproj` is regenerated from a single-platform view** and differs from the committed one, which
the multi-platform merge produced from the union:

* `<AllowUnsafeBlocks>` flipped `true` → `false` for `syscall` (the linux file set alone does not need
  it; the union does);
* the validation-proof `<None Include=... PackagePath="VALIDATION.md">` block vanished.

Neither is a converter defect and neither has anything to do with the change under test — but both
read exactly like the "any change to a production `.csproj` is real drift" alarm CLAUDE.md rightly
raises. Restore the `.csproj` after a single-package iteration run and let the multi-platform regen be
the authority on it.

## 2. Measurement log

Each row is a real run of the scratch app (`fmt.Println("hello, 世界")`) under WSL.

| # | Failure | Class | Action |
|---|---|---|---|
| 0 | `NotImplementedException: runtime_envs` at `syscall_package..cctor()` → `os.init()` → `fmt` | **(w) wiring** | Root-caused to a converter **shape gap** (§3) and fixed; guarded by a fixture 2x2 and a GOROOT-conformance test |
| 1 | `NotImplementedException: Syscall6` at `syscall.RawSyscall6` ← `Getrlimit(RLIMIT_NOFILE)` ← `syscall.init()` (rlimit.go) | **(i) implementation** | **WALL.** Catalogued in §4; no code written, per lane charter |

The `hello, 世界` success condition was **not** reached. The loop cleared the wiring class and stopped
at the first genuine missing native surface, which is the outcome the lane was scoped to produce.

## 3. Failure 0 — the wiring root cause (fixed)

**Not** an L3 / per-GOOS-folder defect, which was the standing hypothesis going in. The corpus already
carried both halves: `runtime/linux/runtime.cs` has the body under
`//go:linkname syscall_runtime_envs syscall.runtime_envs`, and `syscall/linux/env_unix.cs` has the
declaration. They never paired because Go's standard library pushes into **two consumer shapes** and
the converter's matcher accepted only one:

* **handle shape** — a one-arg `//go:linkname <thisFunc>` above the bodyless declaration (`unique`,
  `internal/weak`), the only shape `funcLinknamePush` accepted;
* **bare shape** — no directive at all, just a bodyless func and a prose comment (`func runtime_envs()
  []string // in package runtime`). Predates the handle convention; the push's only directive lives on
  the pushing side. `syscall` and `os` still use it.

"It works on Windows" was **vacuous**: `env_unix.go` is `//go:build unix || (js && wasm) || plan9 ||
wasip1`, so the declaration does not exist in the Windows corpus, and the push registry's original
"eleven bodyless consumers" census was taken against a Windows-only emission. The Linux flavor is
simply the first thing to exercise the other shape — and it does so on the very first init path, since
`syscall.envs` is a package-level var initialized from the call.

Fix and guards: see `linknamePush` / `linknamePushDeclMatches`, and the
[ConversionStrategies reference](../ConversionStrategies-Reference.md#a-cross-package-golinkname-push-resolves-per-recorded-disposition--forwarder-or-announced-panic).
Emitted footprint is **five files** (push source widened to `public` in all three runtime flavors, the
consumer forwarded in the two unix ones), confirmed by a seeded three-target A/B: 0 new, 0 absent, 3
content differences, all intended; marker gate 44/44 intact.

## 4. Failure 1 — the wall: `internal/runtime/syscall.Syscall6` *(CLEARED by r53a — see §7)*

**The catalog. This is the whole implementation-class surface between here and a running Linux
program — one declaration.**

| Field | Value |
|---|---|
| Package | `internal/runtime/syscall` |
| Declaration | `public static partial (uintptr r1, uintptr r2, uintptr errno) Syscall6(uintptr num, uintptr a1, uintptr a2, uintptr a3, uintptr a4, uintptr a5, uintptr a6);` |
| File | `src/core/internal/runtime/syscall/linux/syscall_linux.cs:16` |
| Currently | bodyless partial → `PartialStubGenerator` throws `NotImplementedException` |
| First hit | `syscall/rlimit.go`'s `init()` → `Getrlimit(RLIMIT_NOFILE)`, i.e. during `os.init()` |

**What Go does underneath.** `internal/runtime/syscall/asm_linux_amd64.s`: moves `num` into `RAX` and
`a1..a6` into `RDI, RSI, RDX, R10, R8, R9`, executes the `SYSCALL` instruction, and returns `RAX` and
`RDX`. A return in `[-4095, -1]` is a negated errno, which the Go wrapper turns into the third result.
There is no linkname and no Go body anywhere — this is the raw kernel boundary.

**Why it is the keystone.** Linux's entire syscall surface funnels through it. `syscall/linux/
syscall_linux.cs`'s `RawSyscall` → `RawSyscall6` → `runtimesyscall.Syscall6`, and `Syscall`/`Syscall6`
take the same path with `runtime_entersyscall`/`exitsyscall` bracketing; every wrapper in the
generated `zsyscall_linux_amd64.cs` (open, read, write, close, stat, getrlimit, …) is built on those.
The same function also backs this package's own epoll helpers (`EpollCreate1`, `EpollWait`,
`EpollCtl`, `Eventfd`), which is how `internal/poll` and the netpoller reach the kernel.

**What a managed implementation would need.**

1. *The syscall bottom* — small. glibc exposes `syscall(2)` directly, so a `[LibraryImport("libc",
   SetLastError = true)] static partial nint syscall(nint number, nint a1, …, nint a6)` plus
   `Marshal.GetLastPInvokeError()` covers `num`, the six args, `r1` and `errno`. Roughly ten lines.
   Two real questions for the design conversation: `r2` (`RDX`) is not observable through libc's
   wrapper — it matters only for a small set of calls (`fork`, `pipe` on some ABIs) and may be
   acceptable to return 0 for; and whether to bind libc at all versus a narrower per-call mapping onto
   .NET APIs, which trades the single keystone for N hand-owns but avoids the ABI surface entirely.
2. *The pointer half* — **already solved, which is the good news.** These wrappers pass addresses as
   `uintptr`: `Getrlimit` emits `RawSyscall(SYS_GETRLIMIT, (uintptr)resource, (uintptr)Ꮡrlim, 0)`.
   golib's `ж<T>` → `uintptr` operator does not hand out a token — it calls `EnsureStableAddress()` to
   pin the managed storage and returns a real address (and `pinnedArrayData` for a Go fixed array), so
   the kernel can genuinely read and write through it.
3. *The residual risk is per-struct LAYOUT, not addressing.* A pinned address is only useful if the
   converted struct's layout matches the Linux ABI. `Rlimit` is two `uint64`s and is blittable, so the
   first caller should work; a struct holding `array<T>` or `@string` is a managed reference where the
   kernel expects inline storage. That is **the same open class** as the Windows non-blittable-wrapper
   census (`Timezoneinformation` and the nine remaining wrappers) already tracked in
   [`BOARD-next-validation-candidates.md`](BOARD-next-validation-candidates.md) — this finding does not
   widen it, it just says Linux will meet it through one door instead of many.

## 5. What is behind the wall (census, for sizing only)

Every `PartialStubGenerator` stub in the Linux build's closure — **284** across the whole reachable
corpus. This is a *superset*, not a work list: most are never called on any given path (`runtime`'s
170 are largely scheduler/GC internals the managed model deliberately never runs).

| Package | Stubs |
|---|---|
| `runtime` | 170 |
| `reflect` | 72 |
| `syscall` | 14 |
| `internal/poll` | 10 |
| `internal/syscall/unix` | 9 |
| `os`, `iter`, `internal/cpu`, `internal/bytealg` | 2 each |
| `internal/runtime/syscall` | **1** (`Syscall6`) |

`syscall`'s fourteen are worth a second look when the keystone lands: `runtime_entersyscall` /
`runtime_exitsyscall` are scheduler bookkeeping a managed model can almost certainly no-op;
`rawSyscallNoError` and `rawVforkSyscall` are two more raw bottoms; `runtime_BeforeFork` /
`AfterFork` / `AfterForkInChild` / `BeforeExec` / `AfterExec` belong to process creation. (Note the
census was taken from generated files on disk and still listed `runtime_envs`, which this lane's fix
had already removed — the `Generated/` folder accumulates, so read it as an upper bound.)

## 6. Open questions for the design conversation — ALL THREE ANSWERED

1. **Bind libc, or map per call?** **RULED (user, 2026-08-08): bind libc.** One keystone P/Invoke, not
   N per-call hand-owns. Implemented in §7.
2. **Is `r2` ever load-bearing on linux/amd64 for the paths that matter?** **Moot — `r2` is reproduced
   EXACTLY, so the question never has to be asked.** The Linux x86-64 convention clobbers only `RCX`
   and `R11`, so `RDX` still holds `a3` when the asm reads it; returning `a3` from the shim is
   bit-for-bit what the assembly produces. Measured under the real Go runtime, not inferred:
   `syscall.Syscall6(SYS_GETPID, …, a3=0xDEADBEEF, …)` → `r2=0xdeadbeef`; the failure path returns
   `r1=-1, r2=0, errno>0` and the shim mirrors that branch.
3. **Does `syscall.init()`'s rlimit dance need to succeed at all?** Moot for the same reason — it now
   genuinely succeeds. `Rlimit` is two `uint64`s, so the first struct to cross the boundary was
   blittable and the pinned-address path worked untouched, exactly as §4.2/§4.3 predicted.

## 7. The r53a run — the wall falls, and two programs run

The keystone landed first (`core/internal/runtime/syscall/linux/syscall_linux_impl.cs`; shape, the
three measurements behind it, and the one disclosed divergence are in the
[ConversionStrategies reference](../ConversionStrategies-Reference.md#the-linux-syscall-bottom--one-libc-pinvoke-and-why-r2-is-exact-rather-than-approximate)),
then §1's loop resumed against the same scratch harness. Four failures followed, and the striking
thing is the shape of the list: **not one of them was a missing native surface.** The keystone was
genuinely the whole implementation class on this path — everything after it was wiring, or a value
the managed model could produce but was not producing.

| # | Failure | Class | Root cause | Fix |
|---|---|---|---|---|
| 2 | `NotImplementedException: fcntl` at `unix.Fcntl` ← `os.NewFile` ← `os.init()`'s `initStdin` | **(w) wiring** | `internal/syscall/unix` PULLs `runtime.fcntl` (`//go:linkname fcntl runtime.fcntl`), whose Go body is three lines over the keystone. The forwarder machinery already existed; the target was simply not in the whitelist | one row: `linknameForwardTargets["runtime.fcntl"]` |
| 3 | `NotImplementedException: runtime_args` at `os.init()` | **(w) wiring** + a truth problem | The BARE push shape r52b's failure 0 introduced — but forwarding alone would have returned an EMPTY `os.Args`, because `runtime.argslice` is filled by `goargs()` off the initial stack | one row: `linknamePushTargets["os.runtime_args"]`, **plus** `runtime/goargs_impl.cs` so the forwarded body has real data |
| 4 | `NotImplementedException: runtime_entersyscall` at `syscall.Syscall` ← `os.File.Write` ← `fmt.Println` | **(w) wiring** | Scheduler brackets pulled from runtime; forwarding is impossible (`getcallerfp`/`getcallerpc`/`getcallersp` then the P state machine) and unnecessary — the CLR's thread model discharges the obligation | `core/syscall/linux/syscall_linux_impl.cs`, both as documented no-ops |
| — | *(none)* | | Program 2 ran on the **first** attempt after failure 4 — `os.Args`, `os.Setenv`/`Getenv`/`LookupEnv` and `time.Now()` all worked with no further change | |

Failure 3 is the one worth carrying forward as a lesson rather than a fix. It is the only failure in
either lane's log where the obvious change would have **passed the run and been wrong**: `os.Args`
would have come back empty, `len(os.Args)` would have printed `0`, and nothing would have thrown.
The registry row and the module initializer are one change for that reason.

### The success conditions

Both byte-compared against `go run` on the same box (`~/golang/bin/go`, go1.23.1 linux/amd64), C# side
built by project reference per §1 and run as `dotnet <app>.dll` under WSL2.

**(a) `fmt.Println("hello, 世界")` — BYTE-IDENTICAL, 14 bytes.**

```
hello, 世界
```
```
00000000: 6865 6c6c 6f2c 20e4 b896 e795 8c0a       hello, .......
```

The UTF-8 encoding of 世界 and the trailing newline are included in the comparison — this is `cmp`
over the two captured streams, not an eyeball match.

**(b) `os.Args` + `os.Getenv` + `time.Now()` — BYTE-IDENTICAL, 152 bytes.** Run as
`prog2 alpha beta` on both sides; every line is a deterministic assertion over a nondeterministic
value, so the comparison is meaningful rather than trivially true.

```
args: 3
argv0 nonempty: true
env get: round-trip
env lookup: round-trip true
env absent found: false
year>2020: true
month in range: true
nonzero: true
```

`args: 3` is the load-bearing line: it is what distinguishes a populated `argslice` from the
plausible-looking empty one failure 3 would otherwise have produced.

### Footprint

Three hand-owned files and two registry rows — no change to any converted `.cs` beyond what the two
rows emit, and nothing Windows-visible.

| Change | Kind |
|---|---|
| `core/internal/runtime/syscall/linux/syscall_linux_impl.cs` | hand-own (the keystone) |
| `core/syscall/linux/syscall_linux_impl.cs` | hand-own (scheduler-bracket no-ops) |
| `core/runtime/goargs_impl.cs` | hand-own (`argslice` from the CLR; carries Go's own Windows guard) |
| `linknameForwardTargets["runtime.fcntl"]` | converter registry row (PULL) |
| `linknamePushTargets["os.runtime_args"]` | converter registry row (bare-shape PUSH) |

### What this does and does not prove

It proves the **run layer**: init completes, the kernel boundary works in both directions, stdout is
real, and three of the most common standard-library entry points return true answers. It does not
prove the corpus *operates* on Linux — that is Phase 4's differential oracle, which has never been
run against a Linux target. The §5 census remains an upper bound on what is still stubbed, and the
`syscall` package's remaining four raw declarations (`rawSyscallNoError`, `rawVforkSyscall`,
`runtime_doAllThreadsSyscall`, `cgocaller`) are untouched: nothing on these two paths needs them, and
writing them speculatively is exactly what this loop is designed not to do.
