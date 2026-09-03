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

### AMENDMENT 2026-09-03 (C2) — the class-C reachability read: which of runtime's deferred trampolines are genuinely dormant, and the ONE that is not

The class-B emission arc (`claude/c2-darwin-classb`) deliberately deferred **43** runtime trampolines
rather than reaching them with a name normalizer: 37 bind on the pragma's SYMBOL rather than its local
name, and 6 carry no darwin pragma at all. This is the reachability read on that set, taken statically
against the corpus — darwin has no run layer, so nothing here is observed throwing.

**The population closes exactly.** All 43 have a `FuncPCABI0` call site (zero unmapped) and **41**
appear in the converted darwin runtime; the two absent, `pthread_key_create_trampoline` and
`pthread_setspecific_trampoline`, are `sys_darwin_arm64.go` and so are correctly outside an amd64
corpus (the set difference in the other direction is empty). All 41 sit in ONE emitted file,
`runtime/darwin/sys_darwin.cs`, and none is in a hand-owned file. By subsystem: pthread 13,
bootstrap/misc 7, signals 6, file/fd 6, memory 4, netpoll 2, time 2, exit 1.

**The pthread cond/mutex subset is genuinely dormant, and the tree had already decided it.** Those
seven are driven by `semacreate` / `semasleep` / `semawakeup` in `os_darwin.cs`, which are reached only
through the lock/note protocol — and that protocol is hand-owned FLAT at `goosAny`:
`runtime/lock_managed_impl.cs` supplies `lock2`, `unlock2`, `notesleep`, `notewakeup`, `notetsleepg`,
`noteSleepDeadline` and `mutexContended`, and `manualConversionFuncs` displaces them for every GOOS.
So they stay throwing correctly, matching the posture `manualTypeOperations.go` already states for the
sibling case — *"has no reachable caller, so it stays auto and stays throwing rather than being
hand-owned speculatively."* Note that `lock_sema_impl.cs`, one of the two companions §3 counts,
hand-owns exactly ONE function (`notetsleep_internal`) and does not displace the sema trio.

**The exception is TIME, and it sharpens §3's count into a named gap.** `nanotime_trampoline` and
`walltime_trampoline` are reached from `nanotime1` / `walltime`, and linux's own hand-own states the
stake: *"That throw is NOT a dormant edge: nanotime is read by cpuprof, metrics, mgc, mgcmark,
mgcpacer, mprof, netpoll and debuglog."* **Both `linux` and `windows` carry
`runtime/<goos>/nanotime_impl.cs`; `darwin` does not.** So §3's "darwin: 2 companions" has a first
concrete missing member whose remedy has already shipped twice.

**And it is priced differently from those two, which is the part an estimate from the precedent gets
wrong.** On linux and windows `nanotime1` is a **bodyless partial** in `stubs3.cs`, displaced simply by
writing a body — no registry entry, no converter change. On darwin it is a **BODIED** converted
function in `sys_darwin.cs` calling `libcCall(FuncPCABI0(nanotime_trampoline), …)`, so displacing it
requires a `manualConversionFuncs` entry: a converter change carrying a two-seeded emission diff and a
hunk-only corpus footprint. Same fix by name, the two different displacement mechanisms CLAUDE.md
separates.

**Deliberately not cut here.** With no run layer there is nothing to control such a hand-own against —
it could not be made to fail — which is the warm-design trap. This is recorded so the next darwin
increment starts from a measured population instead of re-deriving it.

#### CORRECTION 2026-09-03 (C2, same day) — two MECHANISMS above are wrong; both conclusions stand

Left in place rather than rewritten, because this is a dated record and the wrong reasoning is worth
seeing beside the right one. Ruled by COORD on the run-layer design's §0.

**(a) The pthread cond/mutex subset is dormant, but not for the reason given above.** The paragraph
says the seven are unreachable because the lock/note protocol is hand-owned flat at `goosAny`. The
displacement is real — `lock_managed_impl.cs` supplies `lock2`, `unlock2`, `notesleep`, `notewakeup`,
`notetsleepg`, `noteSleepDeadline`, `mutexContended`, and `runtime/darwin/lock_sema.cs` carries a
generated placeholder for each — but **`notetsleep` is NOT among them.** It keeps its converted body,
and that body is the trio's only caller (`semacreate`, `lock_sema.cs:68`).

The real argument is empirical and stronger. Measured: `semasleep` and `semawakeup` have **no caller
at all**; `semacreate` has exactly one, `notetsleep`; and `notetsleep`'s three callers are **identical
on all three flavours** — `proc.cs:1669` (stop-the-world), `proc.cs:2157` (safepoint), `proc.cs:6101`
(sysmon). **Linux and windows run real workloads against that exact call graph and their semaphore
trio never fires**, because the managed model does not enter those scheduler paths — the same measured
fact (`schedinit` never runs) that makes `internal/cpu`'s `doinit` unreachable. Below `notetsleep`,
darwin's graph is not merely similar to theirs; it is the same file.

**(b) Darwin's missing `nanotime_impl.cs` is deliberate and documented, not an unnoticed gap.** The
paragraph above frames it as a first concrete missing member that the other two flavours had already
filled. The linux file's own header says otherwise, and said it first: *"Per-GOOS rather than flat
because darwin already has a real body (sys_darwin.cs's nanotime1 over its own `$INTERNAL` trap), and a
flat implementation would collide with it."*

What survives — and what that header independently confirms — is the **sizing**, which is the half this
record was useful for: darwin's `nanotime1` is a BODIED function, so displacing it needs a
`manualConversionFuncs` entry (a converter change, with a two-seeded diff and a hunk-only footprint),
where linux and windows were bodyless partials displaced by writing a body. The novelty claim does not.

Both corrections were self-caught while designing the increment this record was written to inform, and
both came from the same failure: citing a file without reading what it already said.


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

## 7. Amendment 2026-09-03 — the run layer exists: the first converted programs run on macOS

**What changed since §4.** The keystone this finding sized (§4, option 2 — one `FuncPCABI0` over an
already-emitted symbol map plus a small dispatch family) landed on master with train 19 as
`88f01638c`: `runtime.libcCall` displaced through `manualConversionFuncs` to
`runtime/darwin/libccall_impl.cs`, golib `GoLibcCall` (arity 0–9 over unmanaged Cdecl function
pointers, `__error` as the errno reader), `GoCgoDynamicImports.SymbolOf` so a refusal names its
symbol, `syscall/darwin`'s twelve bodyless entry points over one helper, and the converter's second
pragma spelling (`libc_<stem>` → `<stem>_trampoline`) binding 36 runtime records. Its acceptance was
stated MEASURABLE-NOT-GATED: a mac-runner dispatch that MOVES the death past `getrlimit` — to
`sysctl`, `setrlimit` or `fcntl` per the floor — with the prediction posted before the run.

**The first acceptance read (dispatched at master `93a131a3f`, prediction posted first as mailbox
`f8cd28677`, result as `fc1ab7d97`).** The death did not move. It vanished for the set measured.

| run | leg | result, quoted from the step's own tail |
|:--|:--|:--|
| behavioral-smoke [33783959515](https://github.com/ritchiecarroll/go2cs/actions/runs/33783959515) (filter `Defer`, 24 projects) | osx-arm64 (macos-15) | `Transpile pass 24 · Compile pass 24 · Target pass 24 · Output pass 24, fail 0` — `[Output] running C# vs Go, comparing exit code + stdout... 24 compared, 0 failed` — `PASS (24 projects, 202.4s)` |
| | osx-x64 (macos-15-intel) | identical summary — `PASS (24 projects, 554.7s)` |
| census [33783950663](https://github.com/ritchiecarroll/go2cs/actions/runs/33783950663) (`dotnet build src/go2cs-stdlib.slnx -c Debug -m --no-incremental -p:GoTargetOS=darwin`) | osx-arm64 | 306 projects / **306 assemblies** / 0 with no assembly / 0 error lines / exit 0 / 510 s |
| | osx-x64 | 306 / **306** / 0 / 0 / exit 0 / 1245 s |

Before the keystone the same smoke stage failed every project at Output with `exit code mismatch:
C# 2 vs Go 0` — the module-initializer death this finding convicted in §2 (`syscall.init() →
Getrlimit → rawSyscall`, both architectures, runs 32852477992 / 32863205314). After it, no program
on either architecture died at all: `getrlimit` dispatched, and so did everything else the twenty-four
programs' init, `fmt` and defer/panic/recover paths reach, and the stdout + exit-code comparison
against `go run` passed 24 of 24. **Scoring the prediction honestly:** it said the death would MOVE
to one of three named symbols; none of the three was reached as a death because each resolved and
dispatched like the rest. The prediction was conservative, not wrong in direction, and it is recorded
as such. Read as a census, never as a wall: the smoke set is `Defer`-filtered (24 of ~700), so what is
measured is the init path, `fmt`, and defer/panic/recover — not the corpus. The full-enumeration
census (`behavioral-full`, four index slices with a purge between, both architectures) is the next
increment's measurement; its per-class prediction is posted before its dispatch and its reading
belongs in a later dated block here, not in this one.

**Two facts for the record.** (1) The committed darwin flavour is amd64-only (§5's "second,
independent darwin debt"), and osx-arm64 passed identically — so the arm64 tables debt is not on
this path; the keystone commit's recorded arm64 debt is narrower (variadic libc callees called
register-style, correct for amd64) and is exactly what a file-creating program would meet first.
(2) Both legs ran the pinned toolchain (`go1.23.12` from the runner's hostedtoolcache) with
`GoTargetOS: darwin` bound in the job env — the mechanism §5 had already proved reaches MSBuild.

**§5, re-read against this.** *"Whether anything past the census can run at all"* — settled the other
way now: **yes**, for every program on the smoke set's symbol reach. *"Which platform the behavioral
transpile targets"* and the arm64 goldens question — the Target phase passed 24 of 24 on both
architectures again, and now with a run behind it, so the Windows-captured goldens hold on darwin
for that set at the run level too.

**NEWS candidate for the owner's surfaces** (the owner decides where, if anywhere, it publishes):

> **2026-09-03 — go2cs programs run on macOS.** The darwin run layer's keystone landed with train 19:
> Go's libc trampolines, which the converter had emitted as throwing stubs since the first darwin
> execution on 2026-08-25, now resolve by symbol against `libSystem` and dispatch through one managed
> keystone (`runtime.libcCall` over golib's `GoLibcCall`). The first acceptance dispatch on GitHub's
> macOS runners ran the behavioral `Defer` smoke set — 24 converted programs — to Go-identical output
> on both Apple silicon and Intel, with the whole 306-package darwin corpus compiling clean on both.
> Windows and Linux remain the validated platforms; darwin is measured by the run layer's next
> increments, one census at a time.
