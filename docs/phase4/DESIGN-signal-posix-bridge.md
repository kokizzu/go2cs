# DESIGN — the os/signal PosixSignalRegistration bridge (Linux)

> Status: **implementing** (lane `claude/laneR-signal-arc`, 2026-08-27). Routed by the coordinator
> after the signal-wall probe confirmed the class is an ARC, not a disclosure. Scope ruled
> install-layer-only; residual keeps its refusal.

## The wall this dissolves

os/exec's signal family — `TestWaitInterrupt/*`, `TestSIGQUIT`, `TestSIGCHLD` — and os/signal's own
suite die on one root: the converted runtime's raw Linux signal syscalls **`rt_sigaction` and
`rt_sigprocmask` are unimplemented PartialStubGenerator stubs**. `signal.Notify`/`signal.Ignore`
reach the kernel through
`os/signal → signal_enable → runtime.sigenable → setsig → sysSigaction → rt_sigaction`, and
`sigenable`/`sigdisable` first hand off to `ensureSigM`'s goroutine over `rt_sigprocmask`. Both throw
— the second on a background goroutine ("unhandled exception outside any test"), which is why the
suite hangs rather than fails cleanly. Rooted 2026-08-27; the twelve-reproduction os/exec heap
corruption was a *different* bug, cured separately by B2's kind split — this is the ordinary residual
behind it.

Why it is a wall and not a bug: the CLR **owns** signal handling on Linux (its own
SIGSEGV/SIGCHLD/SIGTERM handlers; signals for GC and thread suspension), and there is no native Go
`sigtramp` to install via `sigaction`. Faithfully converting Go's own signal machinery lands on
syscalls the managed host cannot host.

## The probe that routed it (2026-08-27)

A standalone net10.0 program confirmed `System.Runtime.InteropServices.PosixSignalRegistration`
delivers every primitive the failing tests exercise, on Linux:

| Primitive | os/signal need | Probe result |
|---|---|---|
| delivery-on-send | `signal.Notify` | SIGINT/SIGQUIT/SIGTERM fire the handler on `kill(self)` |
| Cancel suppression | `signal.Ignore` / handler-installed | `ctx.Cancel=true` survives a would-be-fatal signal |
| SIGCHLD on child exit | os/exec + `TestSIGCHLD` | fires ~2 ms after a spawned child exits, alongside .NET's own reaping |
| N handlers per signal | many `Notify` channels | two registrations on one signal both fire |

The wall bisects at the **PosixSignal enum boundary**: the async-notify subset becomes an arc; the
raw `rt_sigaction` semantics (masks, `SA_ONSTACK`, `SA_SIGINFO` fault detail, handler forwarding,
synchronous in-context execution, real-time signals) stay the honest disclosure.

## The design — install layer only

`signal_enable`/`signal_disable`/`signal_ignore` (`sigqueue.go`) do the `sig.wanted`/`ignored`
bookkeeping and then call one of `sigenable`/`sigdisable`/`sigignore` to reach the kernel. **Only
those three are hand-owned**; everything above and below stays auto:

```
os/signal.Notify → signal_enable [AUTO: sets sig.wanted] → sigenable [HAND-OWN: register]
                                                                 ↓ (a signal arrives)
   PosixSignalRegistration handler → { ctx.Cancel = true; sigsend(sig); }
                                                                 ↓
   sigsend [AUTO: re-checks sig.wanted] → signal_recv [AUTO] → the os/signal channel [AUTO]
```

**One handler serves both Notify and Ignore.** `sigsend` already gates on `sig.wanted`, so:

- After `Notify` (`wanted=1`) the handler's `sigsend` delivers to the channel.
- After `Ignore` (`wanted=0`) `sigsend` drops the signal and `ctx.Cancel` has already suppressed the
  default disposition — which *is* `SIG_IGN`'s observable behavior.

The Notify/Ignore distinction lives entirely in the untouched `sigqueue.go` bookkeeping. The install
layer does not need to know which it is.

**`sigdisable`/Reset disposes the registration**, not merely detaches it: Go's Stop/Reset returns the
signal to *default* handling, and disposing the last `PosixSignalRegistration` restores the previous
(default) disposition — so a `default-death-after-Reset` assertion (the SIGQUIT family's shape) holds.

**`ensureSigM` is elided, not reimplemented.** Its `enableSigChan`/`maskUpdatedChan` handshake was the
protocol of the `rt_sigprocmask` goroutine; `PosixSignalRegistration` owns its own delivery thread and
signal mask. The auto members remain in `signal_unix.cs`, now unreferenced.

## The residual (stays refused)

`.NET`'s `PosixSignal` is a fixed enum. Signals with no member — SIGUSR1/2, SIGPIPE, the real-time
signals — cannot be registered; `MapPosixSignal` returns null and the install is a no-op, so any test
needing them stays the honest `rt_sigaction` disclosure with the probe as evidence. SIGKILL/SIGSTOP
are uncatchable in both runtimes by design.

Mapped set (Linux/amd64 numbers, mirrored by `defs_linux_amd64.cs`): SIGHUP 1, SIGINT 2, SIGQUIT 3,
SIGTERM 15, SIGCHLD 17, SIGCONT 18, SIGWINCH 28.

## Placement and gates

- **Converter:** `sigenable`/`sigdisable`/`sigignore` registered `goosLinux` in
  `manualConversionFuncs` (`manualTypeOperations.go`), the `getGOAMD64level` model — a Linux `-stdlib`
  emission drops the auto bodies to placeholders; `runtime/linux/signal_posix_impl.cs` supplies them
  with the `[module: GoManualConversion]` marker. The other ~1,440 lines of `signal_unix.cs` keep
  reconverting. Darwin's copy stays auto until its own arc.
- **Gates before banking:** the seeded **Linux-target reconvert + marker gate** (prove the converter
  reproduces the placeholders on a linux emission, the wait4 ritual — not only that Windows CNR is
  inert); the os/exec signal-family re-measure (retire the interim named-refusals as they pass); the
  os/signal suite measure (unbanked — may mint a new row on both platforms); GPG.
