# FINDING — darwin has no run layer: the libc trampolines are throwing stubs

> Lane `darwin-smoke-fix`, 2026-08-25. Companion to
> [`FINDING-linux-run-layer.md`](FINDING-linux-run-layer.md) (the same shape, one platform earlier)
> and [`DESIGN-multiplatform-corpus.md`](DESIGN-multiplatform-corpus.md) (layout L3, which is what
> makes a darwin flavor exist at all).
>
> **Status: characterized, NOT fixed.** No converter, golib or corpus change was made. The remedy is
> a run-layer implementation arc, which is design-with-user territory and cannot be iterated blind on
> CI hardware — there is no macOS box in the fleet, and the Linux precedent's whole method was a
> local edit/run loop with source-line stack traces (§1 of that finding).

## 1. Headline

The **first darwin `behavioral-smoke` ever executed** (run
[32852477992](https://github.com/ritchiecarroll/go2cs/actions/runs/32852477992), 2026-08-25) failed
with every one of its twenty programs reporting `exit code mismatch: C# 2 vs Go 0`, after Transpile,
Compile and Target all passed 20/20.

That is not a regression and it is **not caused by the Go 1.23.12 hop**. It is the first execution of
a surface that had only ever been *compiled*. The darwin corpus compiles because Go's darwin syscall
entry points are **assembly** in Go, which the converter emits as bodyless `partial` methods — and
`PartialStubGenerator` gives every bodyless partial a throwing body. So the census is honestly green
and the program dies on its first syscall.

## 2. The convicting path

Every one of the twenty projects is a `Defer` test whose only observable act is printing through
`fmt`. Take that print as the worked example of the mechanism — it is the shortest path from
converted Go to the gap, though §2.1 shows the program never survives long enough to run it:

```
fmt.Println → os.Stdout.Write → internal/poll.FD.Write → syscall.Write → syscall.write
```

`write` is, verbatim from `src/core/syscall/darwin/zsyscall_darwin_amd64.cs:1738`:

```csharp
var (r0, _, e1) = syscall(abi.FuncPCABI0(libc_write_trampoline), (uintptr)fd, (uintptr)_p0, (uintptr)len(p));
```

Both halves of that line are unimplemented on darwin:

| Callee | Declared at | Implementation |
|:--|:--|:--|
| `syscall(fn, a1, a2, a3)` | `syscall/darwin/syscall_darwin.cs:351` — bodyless `partial` | **none** → generated stub |
| `libc_write_trampoline()` | `syscall/darwin/zsyscall_darwin_amd64.cs:1746` — bodyless `partial` | **none** → generated stub |
| `abi.FuncPCABI0(f)` | `internal/abi/funcpc.cs:22` | `internal/abi/funcpc_impl.cs` — `return default;` (i.e. **0**) |

`PartialStubGenerator` (`src/gen/go2cs-gen/PartialStubGenerator.cs:111`) fills each with:

```csharp
throw new global::System.NotImplementedException(
    "syscall: external (assembly or cgo) function is not implemented");
```

The throw escapes to golib's unrecovered-exception handler (`src/core/golib/builtin.cs:121`), which
reports on stderr and calls `Environment.Exit(2)` — mirroring Go's exit status for an unrecovered
panic. Hence **exit 2, on every program, within ~0.2 s of start**: the whole Output phase for twenty
programs took 4.7 s.

Note that even a correct `syscall` implementation would receive a **null function pointer** today,
because `FuncPCABI0` returns `default`. The trampoline mechanism is unimplemented end to end, not
merely missing one entry point.

(The trampoline's *own* stub is never reached: `libc_write_trampoline` is only converted to a
delegate to be passed to `FuncPCABI0`, never invoked. It needs an implementing declaration purely
because an accessibility-modified `partial` is a C# 9 *extended* partial rather than an erasable one
— which is also why the package compiles at all. The throw comes from the `syscall`/`rawSyscall`
entry point, whichever the caller used.)

### 2.1 Confirmed on the runner

Re-dispatched three times on this lane's branch as the harness's stderr reporting was sharpened —
[32863205314](https://github.com/ritchiecarroll/go2cs/actions/runs/32863205314) (all twenty projects),
then [32864703627](https://github.com/ritchiecarroll/go2cs/actions/runs/32864703627) and
[32865899270](https://github.com/ritchiecarroll/go2cs/actions/runs/32865899270)
(`-f filter=DeferSimple`, to read the chain). The final line, **identical on both mac
architectures**:

```
exit code mismatch: C# 2 vs Go 0 -- C# stderr: "System.TypeInitializationException: The type
initializer for '<Module>' threw an exception. [+3 nested] ---> System.NotImplementedException:
rawSyscall: external (assembly or cgo) function is not implemented"; Go stderr: ""
```

with the intervening wrappers (read at run 32864703627, before the leaf-cause change) being
`'<Module>'` again and then **`'go.os_package'`** — the §2 mechanism, confirmed on the hardware
rather than inferred from the corpus.

Two corrections to the predicted diagnosis, both worth carrying:

- The leaf is **`rawSyscall`**, not `syscall`. Same family, different entry point: both are §2's
  bodyless partials and both get the same generated stub, so this is the predicted *class* firing at
  a sibling member. `rawSyscall`'s darwin users include `Getpid`, `Getuid`, `Getegid`, `ioctl` and
  `pipe` — an `os` initializer reaches one of them before it ever reaches `write`.
- **Which** call fires first is NOT pinned here. The report line carries the exception chain but no
  frames, so naming the exact Go function would be speculation; it does not change the finding,
  because every member of the family is equally unimplemented.

And two things predicted correctly, now read off the runner:

- **`'<Module>'`** — the failure is in a **module initializer**, i.e. a converted Go `init()`
  (`GoInitAttribute`), so the program dies *before* `Main`. That fits `os_package`'s static
  constructor, which runs `initᴛStdin/Stdout/Stderr/initCwd` (`os/darwin/package_init.cs`); the
  first of those to touch libc throws. So darwin does not fail when a program prints — it fails when
  a program *starts*, and would fail identically for a program that prints nothing.
- **Both architectures, identical message.** `osx-x64` matches the flavor's `_amd64` sources and
  fails the same way, which is what rules the amd64-only committed flavor (§5) out as the cause.

## 3. The asymmetry, quantified

Go's darwin syscalls do not use trap numbers; they call **libc** (`libSystem.B.dylib`) through
`//go:cgo_import_dynamic` assembly trampolines — 123 of them in `zsyscall_darwin_amd64.cs` alone.
Linux, by contrast, uses real syscall numbers, and the Linux campaign hand-owned an entry point for
them. The run layer each platform actually has, counted as `*_impl.cs` companions across the corpus:

| Flavor | `*_impl.cs` companions | Syscall entry point implemented? |
|:--|--:|:--|
| `windows` | 17 | yes — the reference flavor |
| `linux` | 7 | yes — `internal/runtime/syscall/linux/syscall_linux_impl.cs`, `syscall/linux/syscall_linux_impl.cs`, `syscall/linux/zsyscall_linux_amd64_impl.cs` |
| `darwin` | **2** | **no** |

The two darwin companions are `os/darwin/dir_darwin_impl.cs` (libc `readdir_r`, via
`DllImport("libc")`) and `runtime/darwin/lock_sema_impl.cs` (the mutex protocol). **Neither is a
syscall entry point.** Both were written for reasons other than making darwin run — which is exactly
why the gap survived to the first execution.

Bodyless `partial` declarations in the darwin flavor of `fmt`'s closure, by package:
`syscall` 147, `runtime` 55, `internal/syscall/unix` 37, `os` 4, `internal/poll` 2 — **245 total**.
Not all need bodies (many are unreachable for a `fmt`-only program), but the syscall entry points and
the trampoline mechanism are on the path of *every* converted program.

## 4. What would have to be built

The Linux keystone was one entry point over libc's `syscall(2)`. Darwin's is structurally larger,
because there is no single `syscall(2)` to call — each trampoline names a **distinct libc symbol**.
The shapes available, none of them chosen here:

1. **Per-symbol `DllImport`/`LibraryImport`** against `libc` (which resolves to `libSystem.B.dylib`,
   the precedent `os/darwin/dir_darwin_impl.cs` already sets), replacing the trampoline indirection
   entirely — 123 declarations in the amd64 file, mechanical but wide, and plausibly converter-generated
   rather than hand-written.
2. **A real `FuncPCABI0`** returning `NativeLibrary.GetExport(dlopen("/usr/lib/libSystem.B.dylib"), name)`
   for each trampoline, with `syscall`/`syscall6`/`rawSyscall`/… implemented once over
   `Marshal.GetDelegateForFunctionPointer` or `calli`. This keeps Go's own structure — one keystone
   plus a symbol table — and is the closer analogue to what Linux did.

Option 2 is the smaller surface and the better fit for the corpus's shape; it is recorded as an
observation, not a ruling.

**Amendment 2026-09-02 — the first casualty is pinned, and it sizes the keystone.** Read from
frames the runner now carries (through check-run annotations alone, no artifact download), a
converted program dies in **`syscall.init()` → `Getrlimit` → `rawSyscall`** — one package EARLIER
than this finding predicted, which named `os`'s static constructor. The minimum keystone to reach
`Main` is therefore **`rawSyscall` plus the `libc_getrlimit` trampoline**, and the consequence for
scoping is the useful half: **neither an `os`-only nor an `fmt`-only scope is the right unit** —
the entry point is reached before either package's own initialization runs. Option 2 above is the
shape this sizing favors; it remains an observation pending the owner's read.

## 5. Known-unknowns this settles, and one it does not

Settled from [`../CIMatrix.md`](../CIMatrix.md)'s darwin list:

- *"Whether anything past the census can run at all."* — **No.** Not until §4 exists. A darwin
  `behavioral-smoke` or `sweep-shard` dispatch will keep reporting exit 2 uniformly, and that is a
  known state rather than a new finding.
- *"What an explicit `darwin` binding does."* — It **works**. `GoTargetOS: darwin` in the workflow's
  `env:` block does reach MSBuild, and the darwin flavor is what was built and run. Proof: the
  identical mechanism on Linux (`GoTargetOS: linux`, env only) passed 20/20 in run
  [32613375229](https://github.com/ritchiecarroll/go2cs/actions/runs/32613375229), and `fmt`'s closure
  contains thirteen L3 packages including `os`, `runtime`, `syscall` and `time` — a windows flavor
  there would have faulted on `kernel32.dll` just as uniformly.

**Not** settled, and now unmeasurable until §4 lands: *"which platform the behavioral transpile
targets"* and whether the Windows-captured goldens hold on **arm64**. The Target phase passed 20/20 on
both mac architectures, which is real evidence that the converter's darwin/arm64 path reproduces the
goldens — but it is evidence about the *converter*, not about the run.

One incidental observation, deliberately not acted on: the committed darwin flavor is **amd64-only**
(`zsyscall_darwin_amd64.cs`, `zerrors_darwin_amd64.cs`, `ztypes_darwin_amd64.cs`), so `osx-arm64`
compiles amd64 constants. That is a second, independent darwin debt. It is **not** the cause of this
failure — `osx-x64`, where the arch matches, failed identically.

## 6. What this lane changed

Only the harness's diagnosability, in `src/tests/Behavioral/BehavioralRunner/Program.cs`: an
exit-code mismatch now quotes both sides' stderr. The runner already held that text and discarded it,
which is why twenty identical `exit code mismatch: C# 2 vs Go 0` lines named none of the twenty causes
and this diagnosis had to be reconstructed from the corpus instead of read from the log. The fix is
platform-neutral and helps every leg of the matrix.

It took **three** passes, and the later two are the more useful lesson. Quoting the first stderr line —
the reduction the stderr *comparison* uses, and the obvious one to reach for — bought nothing here:
the first line was `System.TypeInitializationException: The type initializer for '<Module>' threw an
exception.`, a wrapper that names no cause. That is the same evidence loss one layer in, and golib's
crash handler had already learned it from the other side (it writes `ex.ToString()` precisely because
a `TypeInitializationException`'s own message says only "see inner exception").

Following the chain from the TOP was then wrong for a second reason the runner had to show: managed
startup failures nest wrappers of the same type, so quoting the first few levels spent the whole line
budget on three `TypeInitializationException`s and truncated the one exception that named what broke
(`---> Syst ...`). `StdErrSummary` therefore reports the outermost line plus the **innermost** cause,
with the intervening depth as a count — where the program died, and why. A Go panic report still
reduces to its first line, unchanged, and an empty stderr stays empty so the "neither side wrote to
stderr" branch still fires.
