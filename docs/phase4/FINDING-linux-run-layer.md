# FINDING — the Linux run layer: what the first running corpus actually hits

> Lane r52b, 2026-08-08. Companion to
> [`DESIGN-multiplatform-corpus.md`](DESIGN-multiplatform-corpus.md) (increment 4 proved RID packaging
> and produced the first-ever Linux run). This file records the run-layer loop that followed it: what
> failed, what class each failure was, and where the loop hit its wall.
>
> **Status: the wall is real and it is ONE declaration.** The wiring class is cleared; what remains is
> a single missing native surface, catalogued in §4 for a design-with-user conversation.

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

## 4. Failure 1 — the wall: `internal/runtime/syscall.Syscall6`

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

## 6. Open questions for the design conversation

1. **Bind libc, or map per call?** One keystone P/Invoke lights up the whole generated syscall surface
   at once; per-call mapping onto .NET APIs is more hand-owns but no ABI dependency. This decides the
   shape of Linux operation, so it is a design-with-user call, not a lane call.
2. **Is `r2` ever load-bearing on linux/amd64 for the paths that matter?** If not, libc's `syscall(2)`
   is sufficient and the answer to (1) gets much easier.
3. **Does `syscall.init()`'s rlimit dance need to succeed at all?** Go's own code tolerates the error
   (`if err := Getrlimit(...); err == nil`), so a bottom that reports failure honestly would let init
   proceed — which is a legitimate intermediate state, and notably *not* a fabricated answer.
