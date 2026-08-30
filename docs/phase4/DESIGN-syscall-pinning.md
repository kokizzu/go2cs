# DESIGN — the syscall-funnel pointer-lifetime hazard, and why the fix is a call-site closure

> **Status: IMPLEMENTED (2026-08-30).** A pointer-derived argument to any syscall-package funnel
> call (`Syscall`/`Syscall6`/`Syscall9`/`Syscall12`/`Syscall15`/`Syscall18`/`SyscallN`) is now
> captured into a statement-scoped temp at the CALL SITE — the converted zsyscall wrapper itself —
> held by a named local through the call, released via `GC.KeepAlive` immediately after. See
> [§6 Verification](#6-verification) for the full gate record. Two earlier, weaker designs were
> tried and rejected first (§3, §4); this document records why, so neither is re-proposed without
> new physics.

## 1. The hazard

Go's own `unsafe.Pointer` documentation states rule (4): a `uintptr` obtained by converting a
`Pointer` is only guaranteed to keep the referent alive when the conversion appears **directly**
in a call's argument list to a function documented to have this special behavior (`syscall.Syscall`
and its siblings are the stdlib's own instance). The compiler enforces this by extending the
referent's lifetime through the call.

The converter reproduces the shape (`uintptr(unsafe.Pointer(x))` → `(uintptr)Ꮡx`, golib's `ж<T>`
box) but not, originally, the lifetime extension. `ж<T>`'s own `uintptr` conversion operator
(`ж.cs`) DOES pin the ROOT storage for the box's own lifetime (`EnsureStableAddress`, a real
`GCHandle.Alloc(..., GCHandleType.Pinned)` wrapped in `PinnedBuffer`) — but the box produced by an
expression like `Ꮡ(buf, 0)` is TRANSIENT: nothing references it once the conversion completes, only
the bare `uintptr` it produced. The JIT is free to retire that box the moment its address is
extracted — before the syscall trampoline (`syscalln`, `src/core/syscall/windows/dll_windows.cs`)
ever runs. Box collected ⇒ pin released ⇒ the GC is free to move or reuse the storage ⇒ the kernel
call's read or write lands on whatever lives there now.

**Measured, not argued.** An adversarial harness (a background thread forcing continuous
`GC.Collect(2, Forced, blocking: true, compacting: true)` + `WaitForPendingFinalizers()`, racing a
foreground loop exercising the pattern under test 2,000,000+ times) reproduced real heap corruption
from this exact shape — not a theoretical race. A **write-disabled vs. write-enabled A/B** proved
the mechanism directly: every mode that crashed write-enabled ran clean write-disabled, meaning the
crashes were the write landing on freed/reused memory, not an unrelated harness instability. Four
earlier "clean" isolation rounds (raw `GCHandle` cycling, the exact `PinnedBuffer` finalizer shape
outside golib, allocation-volume-matched, `PinnableStorage`'s blittable-array resolution confirmed)
had all — without design — kept the pinned object referenced by a named local through the
simulated write, so none of them had actually exercised the vulnerable shape at all; the A/B is
what exposed that.

## 2. Design space

Three shapes were considered, in the order they were actually tried:

1. **Pin harder at the box** (a `GCHandle`/fixed-buffer split design). Superseded before
   implementation: measurement showed the pin already exists (`EnsureStableAddress`); the missing
   piece is LIFETIME, not pinning strength. Extending pin duration on an already-orphaned box does
   not help — the box needs to stay REFERENCED, which no amount of pinning fixes on its own.
2. **Resolve the address back to its box, inside the funnel itself** (a "keystone tether"). §3.
3. **Capture the box at the call site, before it can be orphaned** (the shape implemented). §5.

## 3. Rejected: the resolve-based keystone tether

Linux already had this shape (`internal/runtime/syscall/linux/syscall_linux_impl.cs`, dated
2026-08-26, predating this arc — rooted from an independent `os/exec` GC-mark SIGSEGV). Its
`Syscall6` resolves each bare `uintptr` argument back through `ManagedPointerTokens` — a
`ConcurrentDictionary<nuint, WeakReference<object>>` that `ж<T>`'s own `uintptr` conversion
operator populates UNCONDITIONALLY, for every such conversion corpus-wide — holds the resolved
object in a local, and `GC.KeepAlive`s it after the call. A miss (no token found — the box was
already collected) resolves to `null`, and `GC.KeepAlive(null)` is a documented no-op: a miss is
**silent zero protection**, not a loud failure.

This was ported to Windows (`syscalln`, the one funnel every `Syscall*`/`SyscallN`/`Proc.Call`/
`LazyProc.Call` entry point dispatches through) as the first escalation attempt, and put through
the same adversarial harness:

| | write-disabled | write-enabled |
|---|---|---|
| resolve-based tether | clean at 2,000,000 iterations | crashes (same "Internal CLR error" shape) |

Write-disabled, with nothing to interrupt the count, the **resolve-miss rate measured 68%**:

```
resolveHits=639,432  resolveMisses=1,360,568   (2,000,000 total, write-disabled)
```

Two out of three addresses converted through this path found no token by the time the tether
looked for one — the box was already gone most of the time, at this pressure. The design's own
comment called the residual window "orders narrower" than the one it replaced; measured under
sustained adversarial pressure, it was not narrow at all. **Rejected.** The Windows port was
reverted (never committed) rather than shipped disabled-by-default or partially protected; the
number above is the reason nobody should re-propose resolve-based protection for this hazard
without a materially different mechanism (e.g. strong-reference registration, which trades the
weak-registry's non-interference for a real retention cost that would need its own measurement).

The Linux tether itself was NOT reverted — see §7 for its disposition once this arc's fix landed.

## 4. The first call-site prototype's bug, and what it proved

A call-site capture was then tried directly: convert the pointer's INNER Go expression (`X` in
`uintptr(unsafe.Pointer(X))`) in isolation via `v.convExpr(source, nil)`, assign it to a temp, cast
the temp. This compiled almost everywhere and failed on exactly one real corpus shape:
`internal/syscall/windows/registry`'s `RegCreateKeyExW` wrapper, whose `sa` parameter arrives
boxed but whose function PROLOGUE rebinds it to a dereferenced ref-local
(`ref var sa = ref Ꮡsa.DerefOrNull();`) before the funnel call. Converting the bare identifier `sa`
in isolation picked up the REBOUND VALUE the ref-local now names, not the box — `CS0030: Cannot
convert type 'go.syscall_package.SecurityAttributes' to 'go.uintptr'`.

This is a real bug, but it is also the argument for the design that fixed it: routed through the
converter's GENERAL argument-conversion machinery instead of a narrow hand-conversion, a wrong
capture is a COMPILE ERROR, not a silent miss. That reframing is what the resolve-based tether's
68% number could never offer — a shape that goes wrong routed through the general path fails loud,
corpus-wide, at conversion or build time.

## 5. Implemented: call-site closure through the general machinery

`src/go2cs/syscallKeepAliveAnalysis.go`. Detection (`syscallFunnelCall`, `pointerDerivedArgSource`)
matches exactly Go's own contract: a call resolving to `syscall.{Syscall,Syscall6,…,SyscallN}` by
`go/types` package path (never a name-only guess), with an argument matching the
`uintptr(unsafe.Pointer(X))` shape Go's own rule (4) requires. `convSyscallFunnelCall` then, per
matching argument:

1. Converts the WHOLE argument expression through the general path — `v.convExpr(arg, nil)` — the
   exact call the non-pointer-derived branch already makes for every other argument. This is the
   fix for §4's bug: whatever context-dependent rebinding the general path applies to the operand
   (a boxed parameter's ref-local, or anything else) is already baked into the rendered text before
   this function ever inspects it.
2. Strips the leading `(uintptr)` cast that the general path's existing peephole
   (`markDeadUnsafePointerBox`, `convCallExpr.go`) guarantees for exactly this shape, recovering the
   box expression. **A prefix mismatch panics**, naming the argument and its rendered text, rather
   than silently falling back to something narrower — the loud-failure property §4 argues for,
   made structural.
3. Emits `var ᴋN = <boxExpr>;` as a statement-scoped temp (hoisted ahead of the call statement),
   rewrites the argument to `(uintptr)ᴋN`, and records `ᴋN` for `System.GC.KeepAlive(ᴋN);` after
   the statement (`drainSyscallKeepAlive`, called from `visitStmt.go` after both assignment and
   expression statements — the two shapes a funnel call can appear in).

The box is held by an ordinary named local in a frame that has not returned — a CLR liveness
guarantee, not a pin and not a resolve. There is no address to look up and therefore nothing to
miss.

## 6. Verification

All four gates the escalation ruling specified, run in sequence, all clean:

* **Static census guard** (`src/syscall-keepalive-census.ps1`, regenerable — counts
  `var ᴋN = …;` / `GC.KeepAlive(ᴋN);` pairs corpus-wide by text, no build required): **77 sites
  protected, 0 mismatches.** Red against the pre-fix corpus (0 captures), green after.
* **Isolated blast radius** (two full seeded `-stdlib` reconverts — corrected converter vs. a
  properly-stashed pre-fix control — diffed against EACH OTHER, not against the committed tree,
  which independently carried ~211 files of unrelated already-landed-but-unbanked position-map
  drift that a naive committed-tree diff would have misattributed to this fix): **exactly 7
  files**, all in the 3 packages that actually call the funnel
  (`internal/syscall/windows/registry`, `internal/syscall/windows`, `syscall/windows`), plus one
  inert one-line quirk in a never-compiled `.cs.auto` review sibling (pre-existing variadic-spread
  rendering, confirmed unrelated).
* **Full-corpus build**: 307 projects, **0 errors**, 57 warnings, ~1m52s. The panic guard (§5.2)
  never fired across either full reconvert — 304/304 packages, twice.
* **2,000,000-iteration write-enabled adversarial gate**, exercising the emitted shape (a box held
  in a named local across the call, released via `KeepAlive`): **0 corrupted / 2,000,000, 0.0000%**,
  under the identical harness that broke the tether at 68% miss and crashed the raw-box shape
  repeatedly. `resolveHits`/`resolveMisses` both 0 — confirmed there is no resolve step in this path
  to miss.

Standard converter ladder, both clean:

* **Full CNR**: `NO REGRESSION` — generated C# and `.csproj` byte-identical across all 682
  behavioral packages (2 advisory converter warnings, unrelated). Expected: the behavioral corpus
  carries no real Windows syscall wrappers, so this fix touches nothing there — the isolated
  blast-radius measurement above is what actually exercises the change.
* **Full behavioral suite**: `PASS`, 650/650 across Transpile/Compile/Target/Output, 624
  output-compared against `go run` with 0 failures (26 skipped, no `package main` — the standing
  count), 868.4s.

## 7. Linux tether disposition

**Not retired in this change; a recommendation is recorded here for the next arc that touches it.**

The Linux tether protects `internal/runtime/syscall.Syscall6` — a DIFFERENT package path than the
public `syscall.Syscall6` this arc's detection matches (`syscallFunnelCall` gates on
`fn.Pkg().Path() == "syscall"` exactly, reproducing Go's own documented contract rather than a
broader one). Two things are true simultaneously:

* The public `syscall.Syscall6` (Linux flavor, `syscall/syscall_linux.go`) is an ordinary converted
  function whose OWN pointer-derived callers — the real corpus traffic, same shapes as Windows —
  get this arc's call-site protection automatically, since detection is Go-source/package-path
  based, not platform-conditional. Its body forwards `trap, a1…a6` (already-erased scalars, not
  `uintptr(unsafe.Pointer(X))`-shaped at that specific line) into `internal/runtime/syscall.Syscall6`
  — a call `pointerDerivedArgSource` correctly does NOT match, because by that point there is
  nothing left to capture: the caller's box is already held alive by THIS arc's own `KeepAlive`,
  several frames up, for the statement's whole duration, which structurally includes every nested
  call inside it.
* `internal/runtime/syscall.Syscall6`'s only OTHER direct callers — `runtime/os_linux.go` and
  `runtime/netpoll_epoll.go` (both import it unaliased; its package name is `syscall`, so
  `syscall.Syscall6` inside `runtime` resolves there, not to the public package) — were checked
  directly: neither passes a pointer-derived argument to it. `runtime`'s OTHER raw-metal primitives
  (`futex`, `clone`, `mincore`, `mprotect`) take `unsafe.Pointer` parameters directly rather than
  eroding to `uintptr` first, so Go's rule (4) does not apply to them at all — a typed pointer
  parameter never loses GC visibility at a C#/CLR call boundary the way a bare `uintptr` does.

**Confirmed 2026-08-30 against a real `GOOS=linux` reconvert and build.** A full `-platforms
linux/amd64` `-stdlib` reconvert with the fixed converter, overlaid and built
(`-p:GoTargetOS=linux --no-incremental`, obj/bin purged first per the L3 doctrine): 307 projects,
**0 errors**. The census guard reports **88 sites protected** corpus-wide across both platforms
(77 Windows + a Linux-specific set — see §7a for why the Linux count needed a second pass to reach
its true size). Directly inspecting the fresh Linux emission for `runtime/os_linux.go`'s four
`internal/runtime/syscall.Syscall6` call sites confirms the source-level reading: none carry a
`Ꮡ`-derived argument, matching Go's own rule (4) exactly — there is nothing for either the tether
or this arc's fix to protect at that specific boundary.

**Recommendation: retire the Linux tether.** Every real pointer-derived call into the syscall
funnel that Linux's corpus contains is now covered by this arc's call-site closure — confirmed,
not inferred. The tether's own resolve step protects nothing today; keeping it "as documented
defense-in-depth" would mean shipping resolve-based protection with a **measured 68% miss rate at
adversarial pressure (§3)** for a set of call sites that is empirically empty. That is a worse
trade than having no tether at all — a defense-in-depth layer that fails silently 68% of the time
under exactly the pressure profile that would ever need it is not depth, it is a false sense of
one. Retiring it is a follow-up to this document, not executed here (§10).

## 7a. A real bug the Linux confirmation build found: temp-name collision (CS0128)

The Linux confirmation build (§7) did not merely confirm the design — it found a genuine defect in
the emission itself, one the Windows-only ladder in §6 could not have surfaced, because no Windows
corpus file happens to contain two funnel-call statements as direct siblings in one C# block with
no scope of their own between them. `syscall/linux/lsf_linux.go`'s `SetPromiscMode` does exactly
that: two `ioctl` calls (`SIOCGIFFLAGS` then `SIOCSIFFLAGS`), both pointer-derived, both direct
children of one `try` block. `convSyscallFunnelCall` named each statement's first temp `ᴋ0` —
`tempName := fmt.Sprintf("ᴋ%d", len(v.pendingSyscallKeepAlive))`, and `pendingSyscallKeepAlive` is
reset to `nil` after every drain — so the second statement's `var ᴋ0 = Ꮡifl;` collided with the
first's still-in-scope declaration: `CS0128: A local variable or function named 'ᴋ0' is already
defined in this scope`.

**Exactly one file, exactly one error, across the entire 307-project corpus on both platforms** —
the loud-failure design paid off again: a real gap surfaced as a build-breaking compile error at
the first corpus that happened to exercise it, not a silent miscompile.

**Fix:** a new `Visitor.syscallKeepAliveCounter int` field numbers every temp
`convSyscallFunnelCall` ever creates, monotonically, for the Visitor's whole run — never reset
alongside `pendingSyscallKeepAlive`. Temp names are no longer restarted at 0 per statement; they
are merely synthesized identifiers never read back, so global-within-the-file uniqueness costs
nothing and structurally cannot collide regardless of how sibling statements nest.

Re-verified end to end after the fix: both platforms reconverted and built clean (0 errors each,
307 projects), the census guard reports 88 sites/0 mismatches, and the Windows-side renumbering
(`ᴋ0`→`ᴋ1`, `ᴋ2`, …, wherever a file has more than one funnel-call statement) was spot-checked
against the pre-fix corpus and confirmed to be a pure name change with identical box expressions,
call sites and KeepAlive pairing — no windows package_info.cs needed re-baselining, because
renumbering a single digit does not shift any line's position.

## 8. Windows tether disposition

Shelved, not shipped. The port (§3) is not present in the corpus — implemented, measured (68%
resolve-miss, write-enabled crashes), and reverted rather than left disabled or partially wired.
Nothing in the committed tree references `ManagedPointerTokens.Resolve` from `syscalln`. This
document is the durable record of why; §3's numbers are the ones to cite if the idea resurfaces.

## 9. Realistic-pressure severity measurement

Explicitly a parallel data point for "how exposed was the shipped (pre-fix) corpus in practice,"
not a fix input — the fix's own correctness rests entirely on §6, not on this section.

The adversarial harness (§1, §6) forces blocking compacting Gen2 collections back-to-back with no
delay — a synthetic worst case no real workload sustains. A "moderate" variant (same harness,
`Thread.Sleep(20)` between forced cycles instead of none — still real, forced, compacting
collections, just not saturating the thread) ran the ORIGINAL, unprotected `box` shape (the exact
pre-fix pattern) for the same 2,000,000 write-enabled iterations:

```
mode=box pressureProfile=moderate writeEnabled=True
total=2000000 corrupted=0 rate=0.0000% gcCycles=1174
```

Clean — roughly 1,174 forced collections over 2,000,000 iterations (~1 per 1,700), against the
adversarial profile's ~245,000+ over a comparable span (~200x the rate), which corrupts readily.
Read together with §1's finding, the hazard is real and was worth fixing, but its pre-fix exposure
window is narrow enough that it needed sustained, unusually aggressive collection pressure to
manifest reliably — not "corrupts on a normal syscall-heavy workload." This single data point does
not map the transition curve between the two profiles; it was not pursued further, being
explicitly secondary to the fix itself.

## 10. Status: closed

`GOOS=linux` confirmed (§7); the Linux tether is retired, not just recommended for retirement —
`internal/runtime/syscall/linux/syscall_linux_impl.cs`'s `Syscall6` no longer resolves or
KeepAlives anything, with the retirement's own reasoning recorded in its header comment. Both
platforms reconvert and build clean (0 errors, 307 projects each), the census guard reports 88
sites/0 mismatches, and CNR is byte-identical across 683 behavioral packages. No open items
remain on this arc.
