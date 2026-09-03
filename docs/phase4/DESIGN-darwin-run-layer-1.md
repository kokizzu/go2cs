# DESIGN — darwin run layer, increment 1: the time primitives

> Companion to [`FINDING-darwin-run-layer.md`](FINDING-darwin-run-layer.md), which established that
> darwin has no run layer and quantified the asymmetry. That document says *what is missing*; this one
> proposes the first increment and, more importantly, settles **how it can be verified on a fleet with
> no mac in it**.
>
> Commissioned 2026-09-03 (COORD → C2) as "the TIME hand-own displacement, the semaphore flavor, which
> guards a non-darwin host can fail". Every count below is measured against the corpus at master
> `6fa031d080`; nothing is carried from an earlier record.

## 0. Two corrections to the class-C reachability read, made before anything is built on it

The amendment this design follows from (`FINDING-darwin-run-layer.md`, AMENDMENT 2026-09-03) is right
in its conclusions and wrong in two of its stated mechanisms. Both are corrected here rather than in
place, because the amendment is a dated record.

**(a) The semaphore trio is dormant for a different reason than the amendment gives.** It said the trio
is unreachable because the lock/note protocol is displaced at `goosAny`. The displacement is real —
`lock_managed_impl.cs` supplies `lock2`, `unlock2`, `notesleep`, `notewakeup`, `notetsleepg`,
`noteSleepDeadline`, `mutexContended`, and `runtime/darwin/lock_sema.cs` carries a generated
placeholder for each — but `notetsleep` is **not** among them. It keeps its converted body, and that
body is the trio's only caller (`semacreate` at `lock_sema.cs:68`). The real argument is in §3, and it
is stronger than the one it replaces.

**(b) `nanotime_impl.cs`'s absence on darwin is deliberate and documented, not an unnoticed gap.** The
amendment presented darwin's missing companion as a gap that the other two flavours had already
filled. The linux file's own header says otherwise, and says it first: *"Per-GOOS rather than flat
because darwin already has a real body (sys_darwin.cs's nanotime1 over its own `$INTERNAL` trap), and a
flat implementation would collide with it."* So the tree had already recorded both the absence and its
cause. What survives — and what the header independently confirms — is the **sizing**: darwin's
`nanotime1` is a bodied function, so displacing it is a registry change, not a body written into a
bodyless partial. That distinction is §2's whole subject. The novelty claim does not survive, and this
is the second time on this arc that citing a file without reading its header cost a framing.

## 1. Scope

Increment 1 makes darwin's **monotonic clock** reachable. It is the smallest change that moves darwin
from "throws before any managed code observes a time" to "reads a real clock", and it is chosen first
because the consumer set is the widest in the runtime and because it needs no syscall entry point.

Explicitly **not** in increment 1: the syscall entry point (`FINDING` §4 — structurally larger, since
darwin has no single `syscall(2)` to bind), the semaphore flavour (§3 shows why), signals, netpoll, and
the amd64-only-constants debt the FINDING records separately.

## 2. The TIME displacement — two functions, two different confidences

### 2.1 `nanotime1` — certain, and the increment's actual deliverable

Reached from `time_nofake.cs:33` (`nanotime()` → `nanotime1()`), and `nanotime()` is read by **eight**
files in the converted runtime: `mgc` (10 call sites), `cpuprof` (2), `mgcmark` (2), `mprof` (2),
`debuglog`, `metrics`, `mgcpacer`, `netpoll` (1 each). That is the same consumer list the linux
hand-own names in its own header as the reason it exists, arrived at independently here, which is the
second derivation the reachability claim needs.

Darwin's declaration is the load-bearing difference:

```csharp
// runtime/darwin/sys_darwin.cs — BODIED
internal static int64 nanotime1() {
    ref var r = ref heap(new nanotime1_r(), out var Ꮡr);
    libcCall((@unsafe.Pointer)abi.FuncPCABI0(nanotime_trampoline), new @unsafe.Pointer(Ꮡr));
    …                                     // mach_timebase numer/denom conversion
}
```

```csharp
// runtime/linux/stubs3.cs and runtime/windows/stubs3.cs — BODYLESS
internal static partial int64 nanotime1();
```

So the two flavours that already have a run layer were displaced by **writing a body**
(`PartialStubGenerator` steps aside by construction when `PartialImplementationPart` is non-null) —
no registry entry, no converter change. Darwin cannot use that mechanism: a bodied converted function
is displaced **only** through `manualConversionFuncs`. Increment 1 is therefore a **converter change**
and owes what a converter change owes: the converter's own `go test`, a two-seeded emission diff, and a
corpus footprint applied as hunks.

The implementation itself is one line against machinery that already exists — golib's
`MonotonicClock.Nanoseconds()` (`src/core/golib/runtime/MonotonicClock.cs`), which both other flavours
bind to. Darwin's hand-own is the same binding; the mach_timebase conversion the generated body
performs is exactly what the managed clock has already done.

### 2.2 `walltime` — conditional, and deliberately NOT bundled

`walltime` exists **only on darwin**: neither linux nor windows declares it anywhere in the converted
runtime. That is not an omission — Go reaches `time.now` through per-platform assembly on those two,
while darwin falls back to `timestub.go`, whose `time_now` is the `//go:linkname time.now` push source
and calls `walltime()` + `nanotime()`.

Its reachability is a shorter chain and a weaker one. `time/time.cs` declares `now()` bodyless and
**`time/time_impl.cs` hand-owns it FLAT**, so the `time` package never routes through runtime's
`time_now` on any platform. `time_now`'s only remaining caller in the corpus is `mgc.cs:905`. So
`walltime` is reached only if that GC path runs.

**Increment 1 displaces `nanotime1` and leaves `walltime` alone**, because bundling them would put a
function whose reachability is conditional into the same cut as one whose reachability is measured, and
the acceptance table could then not attribute a result to either. If the GC path proves live,
`walltime` is increment 1b and takes the identical shape.

## 3. The semaphore flavour — measured dormant, and the evidence is empirical rather than static

Go's darwin lock flavour is `lock_sema.go`, which parks waiters on `pthread_cond`/`pthread_mutex`
through `semacreate` / `semasleep` / `semawakeup` (`runtime/darwin/os_darwin.cs`). Those three account
for seven of the thirteen pthread trampolines in the deferred set.

Measured in the corpus: **`semasleep` and `semawakeup` have no caller at all**, and `semacreate` has
exactly one — `notetsleep`, which is *not* displaced and keeps its converted body. `notetsleep` in turn
has **three** callers, and they are **identical on all three flavours**: `proc.cs:1669`
(stop-the-world), `proc.cs:2157` (safepoint), `proc.cs:6101` (sysmon).

That identity is the argument, and it is worth more than a static reachability walk: **linux and
windows run real workloads against that exact call graph, and their semaphore trio never fires.** Both
flavours' `notetsleep` sits behind the same three scheduler entry points, and the managed model does
not enter them — `schedinit` never runs, which is the same measured fact that makes `internal/cpu`'s
`doinit` unreachable. Darwin's graph is not merely similar to theirs; below `notetsleep` it is the same
file.

**So increment 1 hand-owns none of the semaphore family**, and that is a decision with evidence behind
it rather than an omission. It also matches the posture `manualTypeOperations.go` already states for
this exact neighbourhood — *"has no reachable caller, so it stays auto and stays throwing rather than
being hand-owned speculatively."* If darwin's scheduler is ever made to run, this section is the first
thing to re-measure, and the trio is where to look.

## 4. Which guards a non-darwin host can fail

This is the section that decides whether increment 1 is verifiable at all. **There is no mac in the
fleet**, and the standing rule is that a guard which can only run on darwin is a guard that never runs.
F8 makes that concrete rather than theoretical: a behavioral guard marked
`[GoPlatformExclusive("darwin")]` is **skipped by name** on every host we have, so its golden and its
MSTest entries are verified nowhere. A `runtime.GOOS` early-out is the same thing wearing a different
hat — it is a skip, not a guard.

The way through is that **the hand-own removes the only part that needs a mac.** Once `nanotime1` binds
to `MonotonicClock`, nothing on the path is darwin-specific, and the increment splits into three tiers
of which the first two run anywhere:

**Tier A — CONTRACT, host-neutral, runs on every host.** The properties the replacement must have are
properties of the managed clock, not of darwin: strictly non-decreasing across calls, nanosecond units
(not ticks), a resolution fine enough that two adjacent calls can differ, and no wrap or negative delta
across a sleep. These belong in `GolibTests` beside the existing clock coverage and are exercised by
the Windows and Linux lanes on every run. This tier can be made to fail by returning a constant, by
returning `Stopwatch.ElapsedTicks` unconverted, or by binding a wall clock (which can go backwards).

**Tier B — WIRING, compile-time, also host-neutral, and this is the tier that catches the regression.**
The thing that can silently break is not the clock; it is the *displacement*. Three checks, all
runnable on Linux:

1. **The converter's own `go test`** — the registration is a `manualConversionFuncs` entry, and the
   existing both-sides seam guard already asserts that a registered name has zero generated bodies and
   exactly one placeholder. Adding `nanotime1` puts it under a gate every lane already pays for.
2. **A `-p:GoTargetOS=darwin` build** — darwin compiles clean at master and is a dispatchable
   regression guard, so the displaced emission is compiled without a mac.
3. **The two-seeded emission diff** — proves the footprint is exactly the placeholder swap and nothing
   else, and its darwin arm is where the change is visible at all.

Tier B's positive control is the one that must be run and stated: **remove the registration, re-emit,
and require the generated `libcCall(FuncPCABI0(nanotime_trampoline), …)` body to come back** — a guard
that cannot go red when the displacement is removed is not guarding the displacement.

**Tier C — needs a mac: nothing.** That is the design's actual claim. The libc call is what required
darwin hardware, and displacing it is what removes the requirement. What a mac would still add is
confirmation that the *rest* of a converted program gets far enough to read a clock — which is the run
layer's problem, not this increment's, and is precisely what `FINDING` §5 records as unmeasurable until
§4 lands.

**What this design does not claim.** It does not claim the increment makes darwin run. `nanotime1` is
one of 41 deferred sites in `sys_darwin.cs`; the next thing a converted darwin program meets after the
clock is the next throwing trampoline. The value of doing it first is that its consumer set is the
widest, its remedy has a two-flavour precedent, and — per §4 — it is the rare darwin increment that can
be verified end to end on the hardware the fleet actually has.

## 5. Acceptance, stated before the cut

Per the rule that an acceptance table is enumerated per FAILURE rather than per row, and that a cut
owes its own guard:

| # | Outcome | Reading |
|:--|:--|:--|
| 1 | Tier A green, Tier B green, darwin build clean | increment 1 lands |
| 2 | Tier A green, Tier B's positive control cannot go red | the guard is vacuous — fix the guard, do not land |
| 3 | Two-seeded darwin diff shows more than the placeholder swap | the registration reached further than intended; re-scope before landing |
| 4 | `walltime` also moves | a bundling error — §2.2 keeps it out deliberately |

The prediction on record: the two-seeded windows and linux diffs read **0**, the darwin diff touches
**`runtime/darwin/sys_darwin.cs`** (the body becomes a placeholder) plus the new
`runtime/darwin/nanotime_impl.cs`, and **no** `package_info.cs` moves — the registration adds no
assembly-level record, unlike the class-B emission that preceded it.
