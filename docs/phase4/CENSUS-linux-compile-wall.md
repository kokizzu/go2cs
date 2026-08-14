# CENSUS — the Linux compile wall, 2026-08-13

> **STATUS: CENSUS COMPLETE; the mechanical wave is DIAGNOSED, PRICED and DELIBERATELY NOT EXECUTED
> on this host.** Measured on branch `claude/linux-compile-wall` against `058b37e49`, host **i7-5820K**
> (6C/12T, the interim coordinator machine). Toolchain `go1.23.1`, .NET SDK 9.0.
>
> **Doctrine:** the Phase-3 pattern applied to a platform — **COMPILE is the milestone**, operational is
> later. Prior art, read first: [`DESIGN-multiplatform-corpus.md`](DESIGN-multiplatform-corpus.md) §12
> (increments 3 → 4.5, which took the Linux corpus to **307/307** on 2026-08-08) and
> [`PLAN-linux-operation.md`](../PLAN-linux-operation.md) (the F-item ledger).
>
> **Two headlines, both of which reframe the exercise.**
>
> 1. **The Linux corpus is not walled by undone platform design. It is walled by corpus debt.** It
>    compiled 307/307 five days ago. Since then every regen has been **Windows-only**, so a package's
>    shared files and its per-GOOS files are now emitted by two different converter eras. **All 112
>    errors are that one class, in one package.**
> 2. **⚠ The repository's disk (`D:`) has effectively failed for small-file writes** — **1,606 ms per
>    4 KB file with the machine fully quiesced**, against **0.75 ms on `C:`**. That is a **2,140×**
>    penalty and it is why the remedy was not executed here (§6). Every wall-time figure in this
>    document is a measurement of that disk, not of the work.

---

## 1. The instrument

```
dotnet build src\go2cs-stdlib.slnx -c Debug -m -p:GoTargetOS=linux ^
             -p:UseSharedCompilation=false -clp:ErrorsOnly
```

Run from a clean tree (`git status` empty) with **no** `bin`/`obj` anywhere under `src/core`, so the
measurement is cold and nothing can be inherited from a previous Windows build. `--no-incremental` is
therefore not needed for this first pass and was omitted; it **is** required for any later pass that
changes target, because what differs between targets is the `<Compile>` **item set**, not any source
timestamp (§12 increment 4's trap).

`-p:GoTargetOS=linux` is the only platform decision in the build: layout L3 puts each platform-varying
package's varying files in `<pkg>/{windows,linux,darwin}/` and the `.csproj` compiles exactly one of
them, defaulting to `windows`. **37** packages carry that property; **30** have a `linux/` folder.

**Wall time: 53m 10s** (MSBuild's own `Time Elapsed 00:53:10.59`), of which ~12 min was the cold restore
of 307 projects. On a healthy disk this command is ~10–15 min on this CPU. Budget from §5, not from here.

## 2. The census — before the wave

| | Count |
|:--|--:|
| Projects in `go2cs-stdlib.slnx` | **307** |
| **Assemblies produced (packages compiling under `GOOS=linux`)** | **143** |
| Packages failing with **own-errors** | **1** (`runtime`) |
| Projects **skipped** as dependents of the failure | **163** |
| **Total errors** | **112** |

The arithmetic closes: 143 + 1 + 163 = 307.

**Every error is in one package.** Bucketed by project, the census is a single row — there is no second
failing leaf hiding behind the first, because MSBuild *skips* rather than errors the dependents of a
failed project. `runtime` is the deepest platform-varying package in the graph, so its 163 dependents
include `syscall`, `os`, `fmt`, `net`, `time`, `testing`, all of `go/*` and all of `crypto/*`.

### By CS code

| CS code | Count | Meaning | Which side of the seam |
|:--|--:|:--|:--|
| **CS1620** | **102** | *Argument N **must** be passed with the `ref` keyword* | `runtime/linux/*.cs` — the **stale** caller |
| **CS1615** | **7** | *Argument N **may not** be passed with the `ref` keyword* | `runtime/*.cs` shared — the **current** caller, stale callee |
| **CS1061** | **3** | `ΔClone` undefined on `metricData` / `mcentral` / `pageAlloc` | 1 shared `.cs`, 2 `TypeGenerator` outputs |

### By source file

| File | Errors | CS | Side |
|:--|--:|:--|:--|
| `runtime/linux/proc.cs` | 42 | CS1620 | stale per-GOOS |
| `runtime/linux/malloc.cs` | 14 | CS1620 | stale per-GOOS |
| `runtime/linux/mheap.cs` | 12 | CS1620 | stale per-GOOS |
| `runtime/linux/stack.cs` | 10 | CS1620 | stale per-GOOS |
| `runtime/linux/arena.cs` | 8 | CS1620 | stale per-GOOS |
| `runtime/linux/select.cs` | 7 | CS1620 | stale per-GOOS |
| `runtime/linux/trace.cs` | 4 | CS1620 | stale per-GOOS |
| `runtime/linux/os_linux.cs` | 2 | CS1620 | stale per-GOOS |
| `runtime/linux/mgcscavenge.cs` | 2 | CS1620 | stale per-GOOS |
| `runtime/linux/signal_amd64.cs` | 1 | CS1620 | stale per-GOOS |
| **subtotal — stale side** | **102** | | |
| `runtime/mcache.cs` | 2 | CS1615 | shared (current) |
| `runtime/{mgc,mgcpacer,mstats,netpoll,runtime1}.cs` | 5 | CS1615 | shared (current) |
| `runtime/metrics.cs` | 1 | CS1061 | shared (current) |
| `runtime/Generated/…/{mheap_central,mheap}.g.cs` | 2 | CS1061 | generator output |
| **subtotal — current side** | **10** | | |

## 3. Root cause — one class, seen from both ends of the same call

The two dominant CS codes are **the same defect observed from opposite sides**, which is what makes the
diagnosis certain rather than plausible.

The A2 **ref-lowering** arc (`0133b6aa7`, *"L3/A2 corpus regen — the ref lowering lands corpus-wide"*)
changed unexported package-level functions from pointer parameters to `ref` parameters and rewrote every
call site to match. It regenerated the corpus **for Windows only**.

*Shared file, regenerated, calling a function declared in a stale per-GOOS file* — caller passes `ref`,
callee still takes a pointer:

```csharp
// src/core/runtime/mcache.cs:105   (shared — current emission)
stackcache_clear(ref (Ꮡc).DerefOrNull());     // CS1615: may not be passed with 'ref'
//               ^^^ stackcache_clear is declared in runtime/linux/stack.cs, still pointer-shaped
```

*Stale per-GOOS file calling a function declared in a regenerated shared file* — caller passes a pointer,
callee now takes `ref`:

```csharp
// src/core/runtime/linux/malloc.cs:268   (per-GOOS — frozen 2026-08-08)
lockInit(ᏑprofInsertLock, lockRankProfInsert);   // CS1620: must be passed with 'ref'
//       ^^^ lockInit is declared in a shared file that WAS lowered
```

The 3 × CS1061 are the same staleness through a different arc. `ΔClone` is emitted by `TypeGenerator`
from the clone-eligibility records in `package_info.cs` — and `runtime`'s `package_info.cs` is
**per-GOOS** (`runtime/linux/package_info.cs`). The stale Linux copy does not record `metricData`,
`mcentral` or `pageAlloc`, so no `ΔClone` is generated for them, while the regenerated shared code calls
it.

### The mechanism, measured

`git diff --name-only 5b85c7ecc..HEAD -- src/core` — that is, everything the corpus has done since the
last commit to touch any Linux source:

| | Count |
|:--|--:|
| Corpus paths changed | **1,698** |
| …of which are per-GOOS paths | **86** |
| — `windows/` | **85** |
| — `linux/` | **1** |
| — `darwin/` | **0** |

And the single `linux/` path is `internal/runtime/syscall/linux/syscall_linux_impl.cs` — a **hand-owned**
file edited by hand, not a regenerated one.

**So: zero regenerated Linux files in five days, against 1,698 changed corpus paths.** Every regen in
that window carried the shared files and the `windows/` folders forward and left `linux/` and `darwin/`
where they were. That is the whole defect.

### Why this is not a platform-design wall

Every failure is a *signature mismatch between two emissions of the same package*. None of it is:

- a missing Linux syscall surface — the keystone landed at r53a
  ([`FINDING-linux-run-layer.md`](FINDING-linux-run-layer.md));
- a layout-L3 routing gap — increment 3.5 closed class (a); every file here is in the right folder;
- a hand-own flavor gap — increment 3.5b closed class (b);
- a converted-code or generator defect Windows structurally cannot exhibit — increment 3.5's class (c),
  all four closed by r51a.

It is a **regen-discipline** failure, so it has a mechanical remedy rather than a design.

## 4. The projected residual — what is still behind the wall

163 packages were never compiled, so their state is **unmeasured** and this section is a projection,
labelled as such. The projection is cheap and sound: the same divergence exists wherever a package's
*shared* `.cs` changed in the window while its `linux/` folder did not.

Packages with a `linux/` folder whose shared `.cs` files changed since `5b85c7ecc`:

| Package | Shared `.cs` changed | Reached by this build? |
|:--|--:|:--|
| `runtime` | 59 | **yes — the measured wall** |
| `syscall` | 8 | no |
| `crypto/x509` | 6 | no |
| `net` | 5 | no |
| `time`, `runtime/pprof`, `go/build`, `archive/tar` | 2 each | no |
| `os/user`, `os/signal`, `os/exec`, `internal/poll`, `internal/goos`, `internal/fuzz`, `internal/buildcfg` | 1 each | no |

**15 packages carry the divergence; 1 has been reached.** Note `os` is *absent* — its shared files did
not move in the window (only `os/windows/` did), which is a useful negative control on the method.

The honest reading: after the regen clears `runtime`, expect the build to advance and possibly stop
again in one of the other 14, **in the same class**, and to be cleared by the same single action. This
is not 14 separate pieces of work.

## 5. ⚠ The host finding — `D:` has failed for small-file writes

This is reported at census weight because it invalidates every wall-time budget in `CLAUDE.md` for this
machine and it is why §6 was not executed.

| Measurement | `D:` (repository) | `C:` |
|:--|--:|--:|
| 200 × 4 KB file writes, machine busy | **410.7 s** | — |
| 50 × 4 KB file writes, machine **fully quiesced** | **80.3 s → 1,606 ms/file** | — |
| 200 × 4 KB file writes | — | **0.15 s → 0.75 ms/file** |

**A 2,140× penalty on a quiesced machine.** Corroborating observations from the same session, all of
which read as "slow work" until this number explains them: `PhysicalDisk(0)` sat at queue length 12 and
2555 % disk time; a `robocopy /MT:8` of the corpus seed managed **0.8 files/s**; a per-file PowerShell
copy managed 63 files in 3.5 min; the 307-project build spent 12 minutes in restore and 53 minutes total
while using ~1 % CPU for long stretches.

Free space is not the cause (**559 GB free**), and `C:` on the same machine is healthy, so this is the
device, not the OS or the scanner. **This should be treated as a hardware finding on the interim
coordinator machine** — the same class of event that took the i9 desktop out on 2026-08-09.

### What it costs, in the units that matter

| Operation | Files written | At 1.6 s/file |
|:--|--:|--:|
| Seed a scratch root from `src/core` | ~4,780 | **~2.1 h** |
| Three-target `-platforms` emission | ~7,500 (3 staging roots) | **~3.3 h** |
| Overlay the per-GOOS folders back | ~300–600 | ~8–16 min |
| `check-no-regression.ps1` (re-transpiles 574 packages) | ~1,700 | **~45 min** |

## 6. The mechanical wave — diagnosed, priced, not executed

**The fix is a corpus regen, not a converter change.** There is no converter defect here: the converter
emits Linux correctly today, as increments 3–4.5 proved. What is owed is one three-target merge so the
per-GOOS folders catch up to the shared files.

```
go2cs -stdlib -comments -platforms windows/amd64,linux/amd64,darwin/amd64 ^
      -platform-stage <stage> -go2cspath <seeded-tmp>\src
```

seeded per the CLAUDE.md ritual (`src/core` + `src/version.props` + `docs/validation`, mirroring the
repository's `src/` layout), wiped and re-seeded for the run (the r41 "never convert twice into one
root" rule), with the path-precise marker gate before any overlay.

**Why it was not executed here.** At §5's measured write latency the seed alone is ~2 hours and the
emission ~3 hours, before the two verification builds (~1 h each on this disk) and the gates. Starting
it would have produced a half-finished, unverifiable corpus change on a failing disk — the exact
throwaway the *nothing-throwaway* principle rejects. **A corpus regen is worthless without its Windows
byte-identical control, and that control is itself a full reconvert.**

The two mechanics that took a lane's worth of iteration to get right are recorded here rather than
committed as scripts — no gate would run them, and an un-gated script in the tree is the `QuickTest` rot
pattern `CLAUDE.md` records:

- **the seed** must exclude `bin`/`obj`/`Generated`, take a **sentinel timestamp** immediately before the
  converter runs (emitted-vs-seeded is decided by mtime, not content, because the control target's
  emission is *supposed* to reproduce the seed byte for byte), and keep `$ErrorActionPreference` off
  `Stop` across the native call — a converter stderr WARNING otherwise becomes a terminating
  `NativeCommandError` and can leave a second `go2cs.exe` alive, which is the r41 corruption path;
- **the classifier** must split every difference into the **windows side** (shared files, `windows/`
  folders, `.csproj`, `README.md`) and the **linux/darwin side**, compare with **CR-stripped equality**
  so the documented mixed-CRLF/LF phantom cannot read as drift, and separately report files present in
  the repo but *absent* from the conversion — a seeded root can never reveal those any other way.

**The acceptance standard for whoever runs it**, stated now so it is not negotiated later:

1. **Windows side byte-identical**, or every differing file individually classified. A change that moves
   Windows emission is a **stop-and-report**, not a judgement call.
2. Overlay **only** `linux/` and `darwin/` folders. A `.csproj` whose *linux-conditioned* reference group
   changes is Windows-inert by construction (the condition is false there) but must still be named.
3. Marker gate path-precise, line-anchored (`^\s*\[module:\s*(go\.)?GoManualConversion\]`) — and
   **re-measured**, never carried forward: the census was 44 at r51b and 49 at r59.
4. Re-run the Linux build and re-bucket. Expect `runtime` to clear and the leaf to move.

## 7. Design-owed walls

**None was reached by this census**, and that is the honest statement: the build stops in `runtime` for
mechanical reasons, 163 packages behind it were never compiled, and no error in this log belongs to a
chartered arc. Naming them anyway, so the next lane does not mistake one for a mechanical class:

| Territory | Owning arc | Status against this census |
|:--|:--|:--|
| Linux netpoll (`epoll`), `internal/poll` runtime seam | **netpoll arc** — [`DESIGN-netpoll-managed-poller.md`](DESIGN-netpoll-managed-poller.md), chartered `058b37e49` | Not reached. `runtime/netpoll.cs` appears here **only** as one CS1615 ref-mismatch — a stale-emission error, **not** a netpoll design item. Do not attribute it to the arc |
| Goroutine parking, `entersyscall`/`exitsyscall`, P state | **scheduler arc** — [`DESIGN-cooperative-scheduler.md`](DESIGN-cooperative-scheduler.md), chartered `058b37e49` | Not reached. The Linux scheduler brackets are already a documented faithful no-op (r53a) |
| `syscall`'s 4 remaining raw declarations (`rawSyscallNoError`, `rawVforkSyscall`, `runtime_doAllThreadsSyscall`, `cgocaller`) | run layer, [`FINDING-linux-run-layer.md`](FINDING-linux-run-layer.md) §7 | Not reached, and **compile-irrelevant** — they are bodyless partials the `PartialStubGenerator` fills. They are an *operational* wall, not a compile one |

The distinction this table exists to protect: a `runtime/netpoll.cs` error in a Linux build looks like
netpoll work and is not. Classify by the CS code and the seam, never by the filename.

## 8. F-item mapping, and the finding the ledger was missing

| Bucket | F-item | Note |
|:--|:--|:--|
| CS1620 / CS1615 / CS1061 (all 112) | **F1**, execution | Not a new F1 finding — it is F1's **maintenance** cost, which no F-item had named |
| — | **F8** | Untouched. Still blocked behind a Linux corpus that *runs*, not merely compiles |

**The process finding, which is the durable half of this census.** Layout L3 made the Linux corpus
producible; increments 3–4.5 made it compile and run. But **nothing gates it.** Every standing gate —
CNR, the behavioral suite, `go2cs-stdlib.slnx` at its default target, the validated sweep — is blind to
the `linux/` and `darwin/` folders, so a regen that omits `-platforms` desynchronizes a package from
itself and **no instrument says a word**. Five days and 1,698 changed paths later, the corpus was 112
errors from where it had been left, and the only reason anyone knows is that this census was
commissioned.

That is the same shape as the FALSE-GREEN routes in `CLAUDE.md`, one layer out: not a gate that lies —
a **surface with no gate at all**. Two candidate remedies, both cheap relative to what they prevent, and
neither designed here:

- **make the regen ritual three-target by default** — the corpus has three platforms, so a single-target
  `-stdlib` reconvert into an L3 corpus is the anomaly, not the norm; or
- **a static desync gate** — assert, without building, that no package's shared files were emitted by a
  different converter run than its per-GOOS files (an emission stamp in `package_info.cs` would make
  this a string compare).

The second is the one that would have caught this on day one, in seconds, on any host.

## 9. The honest distance to a full Linux compile

| | |
|:--|:--|
| Packages compiling under `GOOS=linux` **before** this session | **143 / 307** |
| Packages compiling **after** | **143 / 307** — unchanged; the wave was priced, not landed |
| Errors | **112**, all in `runtime`, all one class |
| Work owed | **one three-target regen** plus its Windows byte-identical control |
| Blocked by | the host's disk (§5), not by design, not by the converter |
| Known-good reference | **307/307 on 2026-08-08** (§12 increment 3.5b) |

**Distance, stated plainly: one regen, on a machine with a working disk.** The prior art says the corpus
reached 307/307 five days ago and nothing in this census contradicts it; the 15-package projection in §4
says the regen may need to be followed by one more build/bucket cycle, not by fourteen fixes. What is
*not* in evidence, and must not be assumed, is that 307/307 returns in a single pass — the five-day
window contains arcs (`gen` interface-satisfaction, the Δ-rename spelling change) whose Linux-side
effects have never been compiled.
