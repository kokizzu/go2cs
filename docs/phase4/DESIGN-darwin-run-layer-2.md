# DESIGN — darwin run layer, increment 2: the keystone, re-sized against increment 1's evidence

> Third record in the darwin run-layer arc. Companions:
> [`FINDING-darwin-run-layer.md`](FINDING-darwin-run-layer.md) (the gap, and the runner evidence),
> [`DESIGN-darwin-run-layer.md`](DESIGN-darwin-run-layer.md) (the sizing record — §1.3's *ten
> members, not one*, §2's ABI read, §5.1's cost table), and
> [`DESIGN-darwin-run-layer-1.md`](DESIGN-darwin-run-layer-1.md) (increment 1, the `nanotime1`
> displacement, seated for train 18).
>
> Commissioned 2026-09-03 (COORD → C2): *"increment 2 of the run layer per your seated design's own
> sequence … if the design's increment 2 depends on something only the run layer's hardware could
> measure, say so and take the next hardware-free increment instead."* This record is the **say so**,
> and the hardware-free work it names instead. Every measurement below is against the corpus at
> master `6fa031d080`; nothing is carried from an earlier record, and where an earlier record is
> corrected this one says which line.

## 0. The one-paragraph version

Increment 2 **is** the keystone, and it is not hardware-free in its payoff — only a darwin dispatch
can show the advance. Three candidate increments that would have been hardware-free were measured
out of existence first (§1), which is the result worth having: they are cancelled with their
measurements attached rather than left open. The substantive finding is a **stop-and-post against
the sizing record's own §5.1** (§2): that table predicts *"0 new `[module: GoManualConversion]`
markers"* and *"corpus emission movement: none expected"*, and asks explicitly for a stop-and-post
if an implementation finds itself needing a converter change. It does. The keystone's `syscall`-side
half is companions-only exactly as designed; its **`runtime`-side half is not**, because
`runtime.libcCall` is a **bodied** function whose first statement is an intrinsic that throws — and
`os`'s own static constructor reaches it. So increment 2 is **two displacements of different shapes**,
not one, and the second is increment 1's shape (registry + hand-own + footprint), which this lane has
now executed once.

## 1. Three increments cancelled, each with its measurement

The rule these follow is the arc's own: a negative result is banked where the next reader will stand,
and a function with no reachable caller *stays auto and stays throwing rather than being hand-owned
speculatively* (`manualTypeOperations.go`'s stated posture, quoted by design-1 §3).

### 1.1 `walltime` — design-1 §2.2's conditional "increment 1b" — is DORMANT

Design-1 §2.2 deferred `walltime` with a stated condition: *"`walltime` is reached only if that GC
path runs. … If the GC path proves live, `walltime` is increment 1b."* It does not run.

The chain is three links and every one is measured:

| link | site | what it is |
|:--|:--|:--|
| `walltime` | `runtime/darwin/sys_darwin.cs:538` | bodied, `libcCall(FuncPCABI0(walltime_trampoline), …)` |
| its **only** caller | `runtime/darwin/timestub.cs:29` | `time_now()` |
| `time_now`'s **only** caller in the corpus | `runtime/mgc.cs:905` | `gcMarkTermination` |

And the disqualifying evidence is empirical rather than static, which is the form design-1 §3 used
for the semaphore trio: on **linux** (`runtime/linux/timeasm.cs:13`) and **windows**
(`runtime/windows/timeasm.cs:13`) `time_now` is a **bodyless partial**, and `PartialStubGenerator`
fills each with a throwing stub (`PartialStubGenerator.cs:73` — a partial definition with no
implementing part), which a built tree shows as
`runtime/Generated/…/go.runtime_package.time_now.142.stub.g.cs` and `.168.stub.g.cs`; those are
build output, so the committed fact is the bodyless declaration and the generator's predicate. Those two
flavours run the entire validated roster against the same flat `mgc.cs`, and neither ever fires it.
`gcMarkTermination` is the end of a real Go GC cycle; the managed model does not have one.

**Cancelled.** If darwin's scheduler is ever made to run, this is the second thing to re-measure
after design-1 §3's trio.

### 1.2 The "linux or windows already bind it to a managed body" class is EMPTY after increment 1

Increment 1 was hardware-free for a specific reason (design-1 §4): *"the hand-own removes the only
part that needs a mac"* — `nanotime1` binds to golib's `MonotonicClock`, which both other flavours
already bind, so the contract is host-neutral and Tier A runs everywhere. That reasoning generalizes
into a **class**, so the class was censused rather than guessed at.

Population: every function in the darwin runtime whose body contains `libcCall` — **50** in
`runtime/darwin/sys_darwin.cs`, plus `libcCall` itself in `runtime/darwin/sys_libc.cs`. For each,
what linux and windows do with the same name. Positive control: the census must find `nanotime1`,
and does.

The 50 partition **33 / 17**: thirty-three exist on darwin alone (`pthread_*`, `mach_vm_region`,
`sysctl`, `sysctlbyname`, `kqueue`, `kevent`, `issetugid`, `walltime`, the eight-member `syscall_*`
family, `crypto_x509_syscall`, …), and seventeen have a same-named counterpart on linux or windows
(`raise`, `mmap`, `munmap`, `madvise`, `read`, `closefd`, `exit`, `usleep`, `write1`, `open`,
`nanotime1`, `sigaction`, `sigprocmask`, `sigaltstack`, `raiseproc`, `setitimer`, `fcntl`). Of those
seventeen, **exactly one has a hand-owned managed body**: `nanotime1`
(`runtime/linux/nanotime_impl.cs`, `runtime/windows/nanotime_impl.cs`). The rest are either bodyless
partials that throw on linux exactly as they do on darwin (`madvise`, `closefd`, `exit`, `usleep`,
`write1`, `open`, `sigaltstack`, `raiseproc`, `setitimer`, `raise`) or Go's own platform
implementation rather than a hand-own (`windows/os_windows.cs`, `linux/cgo_mmap.cs`,
`linux/os_linux.cs`).

**Increment 1 took the only member, and the class is spent.** One false positive is worth recording,
because it is the census trap this file is warned about in the general case: a name-keyed scan
widened to the runtime's flat `*_impl.cs` files reports **`read`** as a second member. It is not one
— `managed_impl.cs:355` is `internal static void read(this ж<consistentHeapStats> Ꮡm, ж<heapStatsDelta> Ꮡout)`,
an extension method on an unrelated receiver, where darwin's is
`internal static int32 read(int32 fd, @unsafe.Pointer p, int32 n)` (`sys_darwin.cs:416`). The
signature is what settles it; the name never could.

### 1.3 Linux's own first-casualty remedy does not transfer

Linux's first casualty was not a libc entry point at all: it was
`runtime_entersyscall`/`runtime_exitsyscall`, bodyless partials that took throwing stubs and killed
*hello-world's own output call* — `syscall/linux/syscall_linux_impl.cs`'s header records the whole
diagnosis — and the remedy was **empty bodies**, because the managed host already discharges what
the pair exists to discharge.

That is the cheapest possible run-layer increment, so it was worth asking whether darwin has one.
It does not: both declarations exist **only** under `syscall/linux/` (`syscall_linux.cs:29–33`), and
a search of the whole `syscall` package finds no darwin counterpart. Go brackets darwin's libc calls
inside `runtime.libcCall` instead, which is §2's subject.

**Cancelled**, and worth stating because "an empty body is the remedy" is a shape this project has
been burned by in the other direction (the `runtime_BeforeExec`/`AfterExec` fork-bomb): the reason
this one is refused is that the declaration does not exist, not that the argument is weak.

## 2. STOP-AND-POST against the sizing record's §5.1

The sizing record's cost table (`DESIGN-darwin-run-layer.md` §5.1) predicts, for the whole keystone
wave:

> **new `[module: GoManualConversion]` markers — 0 expected** … *"these are `*_impl.cs`
> **companions**, which supplement bodyless partials rather than replacing a converted file"*
> **Corpus emission movement: none expected.** … *"If an implementation finds itself needing a
> converter change, that is a **stop-and-post**, not a scope increase."*

This is that stop-and-post. The prediction holds for one half of the keystone and fails for the
other, and the half it fails on is on hello-world's path.

### 2.1 The `syscall`-side half is companions-only, exactly as designed

Every member of the family is a **bodyless partial**, so every one is displaced **by writing a
body**: `PartialStubGenerator`'s predicate is `IsPartialDefinition && PartialImplementationPart is
null` (`PartialStubGenerator.cs:73`, in its negated form), so an implementing part steps the
throwing stub aside by construction. No registry entry, no converter change, no footprint. One
`syscall/darwin/syscall_darwin_impl.cs`, exactly the file §5.1 names.

**Count corrected: twelve, not ten.** The sizing record's §1.3 enumerates *"ten declarations"* —
`Syscall Syscall6 Syscall9 / syscall syscall6 syscall6X syscallX / syscallPtr rawSyscall
rawSyscall6` — and omits the exported raw pair. Measured at `6fa031d080`:

```
syscall/darwin/syscall_darwin.cs:19,21,23,25    Syscall  Syscall6  RawSyscall  RawSyscall6
syscall/darwin/syscall_darwin.cs:351..361       syscall  syscall6  syscall6X  rawSyscall  rawSyscall6  syscallPtr
syscall/darwin/syscall_darwin_amd64.cs:68,70    syscallX  Syscall9
```

Twelve declarations. The §1.3 **shape** conclusion is untouched — *"not 267 declarations, and not
one"*, and still three axes over one parameterized helper; only the count moves, and it moves by the
two the enumeration skipped.

### 2.2 The `runtime`-side half is a **bodied** function, and its first statement throws

`runtime/darwin/sys_libc.cs`'s `libcCall(fn, arg)` is **bodied**. A companion cannot supply a
function that already has a body; displacing it is a `manualConversionFuncs` registration — a
converter change, a `[module: go.GoManualConversion]` hand-own, and a corpus footprint. That is
increment 1's shape, not §5.1's.

And it is not optional, because `libcCall` cannot be reached *through*:

```csharp
internal static int32 libcCall(@unsafe.Pointer fn, @unsafe.Pointer arg) {
    var gp = getg();                       // ← runtime/stubs.cs:31, bodyless partial
    …
    mp.Value.libcallpc = getcallerpc();    // ← runtime/stubs.cs:343, bodyless partial
    mp.Value.libcallsp = getcallersp();    // ← runtime/stubs.cs:346, bodyless partial
    var res = asmcgocall(fn, arg);         // ← runtime/stubs.cs:372, bodyless partial
```

**None of those four has an implementing part anywhere in the corpus**, and the witness is the
generator's own output rather than a scan of mine: `PartialStubGenerator` emits a stub only for a
partial definition with no implementing part, and a built tree carries all four —
`go.runtime_package.getg.29.stub.g.cs`, `.getcallerpc.42.`, `.getcallersp.43.`, `.asmcgocall.45.` —
each one line, each `=> throw new NotImplementedException("<name>: external (assembly or cgo)
function is not implemented")`. `getg` has exactly one declaration in the whole runtime
(`stubs.cs:31`), so there is no other part to find. `managed_impl.cs`'s own header records why, in
the present tense: converted g/m/p code *"compile[s], then die[s] on the first `getg()`"*. So the
first statement of the runtime keystone's dispatch bottom throws, and it throws on **every**
platform — darwin is simply the only one that calls it.

The consequence for sequencing is the one that matters: **a real `FuncPCABI0` alone would not have
unblocked darwin.** Hand a perfect function pointer to `libcCall` today and the call dies one line
in, before `asmcgocall` is even reached. The resolution half and this displacement are **one
increment, not two**.

### 2.3 Hello-world reaches the runtime half, so it cannot be deferred

It would be tempting to scope increment 2 to the `syscall` package and leave `runtime`'s libc users
for later. The init graph forbids it. `os`'s static constructor
(`os/darwin/package_init.cs`) runs `initᴛStdin/Stdout/Stderr`, each of which is
`NewFile((uintptr)syscall.Stdin, …)` (`os/darwin/file.cs:76`), and `NewFile` calls
`unix.Fcntl(fdi, syscall.F_GETFL, 0)` (`os/darwin/file_unix.cs:122`). That resolves to
`internal/syscall/unix/darwin/fcntl_unix.cs:15`, which is a **linkname forward into the runtime**:

```csharp
//go:linkname fcntl runtime.fcntl
internal static (int32, int32) fcntl(int32 fd, int32 cmd, int32 arg) {
    var (ᴛ1, ᴛ2) = go.runtime_package.fcntl(fd, cmd, arg);
```

and `runtime/darwin/sys_darwin.cs:648` is bodied over `libcCall` (line 651). **Opening a standard
stream on darwin goes through the runtime keystone**, not the syscall one.

### 2.4 What the cost table becomes

| item | §5.1 said | measured |
|:--|:--|:--|
| `internal/abi/funcpc_impl.cs` — a real `FuncPCABI0` | 1 file rewritten | unchanged; it is COORD's item (2), waiting on C1's registry at master |
| the `syscall` keystone family | 1–2 companion files | **confirmed** — 12 bodyless partials (§2.1 corrects §1.3's ten), companions only, no converter change |
| the `runtime` dispatch bottom | *"possibly a `runtime/darwin` sibling"* | **NOT a companion** — `libcCall` is bodied ⟹ registry displacement, +1 marker, +1 hand-own, corpus footprint |
| new `GoManualConversion` markers | **0 expected** | **1** (`runtime/darwin/libccall_impl.cs` or equivalent) |
| corpus emission movement | **none expected** | **non-zero** — the two-seeded darwin arm moves, as it did for `nanotime1` |

The rest of §5.1 stands. The symbol map is still 0 new files (it is the class-B
`GoCgoImportDynamic` record set, already emitted and seated), and errno is still one more resolved
symbol (`__error`, whose record is the one outlier in that set — local `libc_error`, symbol
`__error` — noted here because a resolver keyed on the `libc_` + symbol convention would miss
exactly the entry the error path needs).

## 3. The init-graph symbol floor — FINDING §4's amendment is a lower bound

`FINDING-darwin-run-layer.md` §4's amendment sizes the minimum keystone as *"`rawSyscall` plus the
`libc_getrlimit` trampoline"*. That is the first **casualty**, correctly pinned; it is not the
minimum **set**, and the difference decides between the two shapes §4 offers.

Derived by reading the two initializers the finding names:

| initializer | chain | libc symbols |
|:--|:--|:--|
| `syscall.init()` (`syscall/darwin/rlimit.cs:36`) | `Getrlimit` → `rawSyscall(FuncPCABI0(libc_getrlimit_trampoline), …)` | `getrlimit` |
| … when `lim.Cur != lim.Max` | `adjustFileLimit` (`rlimit_darwin.cs:13`) → `SysctlUint32("kern.maxfilesperproc")` → `sysctl` | `sysctl` |
| … then | `setrlimit` | `setrlimit` |
| `os` static ctor → `initᴛStdin/Stdout/Stderr` | `NewFile` → `unix.Fcntl` → `runtime.fcntl` → `libcCall` | `fcntl` |
| `os` static ctor → `initᴛinitCwd` | `Getwd` (`syscall/darwin/syscall_bsd.cs:19`) → `getcwd` (`zsyscall_darwin_amd64.cs:1916`) | `getcwd` |

**At least five distinct libSystem symbols before `Main`, from two packages**, and the walk stops
there deliberately — `runtime`, `internal/poll` and `time` are not counted, so five is a floor and
not an estimate. On the macOS default the `lim.Cur != lim.Max` branch is taken, so the `sysctl` and
`setrlimit` links are live rather than hypothetical.

This settles §4's open choice between its two shapes. **Option 1 (per-symbol `DllImport`) does not
clear even the first initializer cheaply** — it would need five hand-owned displacements to reach
`Main`, and **every one is a registry displacement**, because all five are bodied wrappers rather
than bodyless partials: `Getrlimit` (`zsyscall_darwin_amd64.cs:868`), `setrlimit` (`:1544`),
`sysctl` (`:1846`), `getcwd` (`:1906`) and `runtime.fcntl` (`runtime/darwin/sys_darwin.cs:648`).
Five converter changes, five markers, five footprints — to reach `Main` and no further. **Option 2
(one keystone plus a symbol table)** pays once. The symbol table already exists as the class-B record set. The sizing record's
recommendation is confirmed by a route it did not take.

## 4. Is increment 2 hardware-free? — and a correction to design-1 §4

**No, in its payoff.** The keystone's correctness is Tier-B verifiable on any host (converter suite,
`-p:GoTargetOS=darwin` build, two-seeded emission diff), and the *dispatch* contract — given a
resolved native function pointer and an argument block, invoke it and report errno — is host-neutral
and testable in `GolibTests` against any platform's libc. But whether darwin then **advances** can
only be read from a darwin run.

**Correction to design-1 §4, made here rather than by rewriting a seated record.** That section
says: *"There is no mac in the fleet, and the standing rule is that a guard which can only run on
darwin is a guard that never runs."* That is true of **standing gates** — F8 skips a
`[GoPlatformExclusive("darwin")]` behavioral guard by name on every host the fleet has — and it
understates what a **dispatch** can measure. `FINDING-darwin-run-layer.md` §2.1 was itself measured
on GitHub-hosted macOS runners, three dispatches, both architectures, reading the exact failing leaf
off the check-run annotations. So the arc has a run oracle; it is just not a standing one. Increment
2's payoff is therefore **measurable but not gated**, which is a different and much better position
than design-1 §4 implies, and it is what makes the keystone worth cutting at all.

## 5. Increment 2, restated with its dependency

One increment, three parts, landing together because none of them advances darwin alone:

1. **Resolution** — `internal/abi/funcpc_impl.cs` returns a real address for a trampoline, over the
   class-B `GoCgoImportDynamic` record set. This is COORD's item (2), deferred until C1's PC-registry
   rewrite of that file is at master.
2. **`runtime` dispatch** — `libcCall` displaced through `manualConversionFuncs` to a managed body
   that calls the resolved pointer and drops the g0 switch and the `libcall*` profiler bookkeeping,
   on the argument the sizing record already made for the ABI struct (§2: *"an artifact of Go's
   assembly ABI [that] does not survive into the managed form at all"*) and that
   `syscall_linux_impl.cs` made for `entersyscall`/`exitsyscall` one platform over. **+1 marker,
   +1 hand-own, a corpus footprint** — increment 1's shape.
3. **`syscall` dispatch** — the twelve bodyless partials given bodies in one
   `syscall/darwin/syscall_darwin_impl.cs` companion, parameterized on the three axes §2 identified
   (arity, result width, raw-vs-cooked), with `__error` for errno. **0 markers, 0 footprint.**

Part 2 is the one this record adds, and the one the sizing table did not price.

## 6. Acceptance, stated before the cut

Enumerated per failure, not per row.

| # | Outcome | Reading |
|:--|:--|:--|
| 1 | converter suite green, darwin+linux+windows builds clean, two-seeded windows 0 / linux 0 / darwin > 0 | the displacement is sound; dispatch the run gate |
| 2 | two-seeded **linux or windows** non-zero | the registration is not `goosDarwin`-scoped — increment 1's own guard rail, and its control |
| 3 | dispatch still dies in `syscall.init()` → `Getrlimit` | the resolution half (part 1) is incomplete; parts 2–3 are not implicated |
| 4 | dispatch dies **past** `Getrlimit`, at `sysctl` or `setrlimit` or `fcntl` | §3's floor confirmed on the runner — an advance, and the next symbol names itself |
| 5 | dispatch reaches `Main` | the keystone is closed for hello-world; the amd64-only debt (`DESIGN-darwin-run-layer.md` §5.2) becomes the next question, on arm64 |

Outcome 4 is the one to expect, and it is worth saying so before the run: a floor of five symbols
means the first dispatch after the keystone lands should move the death, not remove it. A dispatch
that jumps straight to outcome 5 would mean the floor derivation missed that the later links are
already satisfied — which would be a finding about §3, not a bonus.
