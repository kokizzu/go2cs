# DESIGN — a minimal managed execution tracer (Q28)

**Design only. Nothing here is cut, and the increments below are proposals with bars, not a plan of
record.** Dated 2026-09-04; amended by dated blocks.

This exists because `runtime/trace` was measured `0 of 2` and the row was ruled **unimplemented, not
untestable** (`RECON-runtime-trace-row.md`): both its tests are satisfiable by an implementation that
does not exist yet, which is expensive rather than impossible. This document sizes that
implementation against two acceptance bars, and states plainly what it does not buy.

## The two bars

**Bar A — the row's own suite.** `TestTraceStartStop` needs `trace.Start` to succeed, the buffer to
be **non-empty** after `Stop`, and **no writes after stop**. `TestTraceDoubleStart` needs a bare
`Stop()` to be harmless, the first `Start` to succeed and the **second to fail**. Neither test parses
the stream or inspects a single event. Bar A is therefore a *state machine plus any non-empty
output*, and it is important to say so, because clearing Bar A alone would be **laundering**: the two
verdicts would go green while nothing about the program had been observed. **Bar A is not proposed as
a stopping point.**

**Bar B — `go tool trace` opens a managed program's trace.** This is the bar that makes the work
honest, because the parser is an oracle we do not control: `internal/trace` rejects a stream that is
not self-consistent. Bar B is what turns "the tests pass" into "the trace means something".

## What the format actually demands

Read from the Go 1.23.12 sources rather than assumed:

- **Header** — `"go 1.%d trace\x00\x00\x00"`, so the literal `go 1.23 trace` followed by three NULs.
  The reader `Fscanf`s it and rejects an unknown version outright.
- **Batches** — every batch opens with an `EvEventBatch` (or `EvExperimentalBatch`) type byte; the
  reader errors with *expected batch event* on anything else. A batch is bound to a generation and an
  M, and carries its own timestamp and size.
- **Events** — the v2 (`go122`) set is large: goroutine lifecycle (`EvGoCreate`, `EvGoStart`,
  `EvGoStop`, `EvGoBlock`, `EvGoUnblock`, `EvGoDestroy`, `EvGoStatus`), processor scheduling
  (`EvProcStart`, `EvProcStop`, `EvProcSteal`, `EvProcStatus`), GC (`EvGCBegin`/`End`, sweep and
  mark-assist), heap (`EvHeapAlloc`, `EvHeapGoal`, `EvHeapObject*`), stacks and strings
  (`EvStack`, `EvStacks`, `EvString`, `EvStrings`), the user API (`EvUserTaskBegin`, `EvUserRegionBegin`,
  `EvUserLog`), and `EvFrequency` for the timer.

## What the managed runtime already has

This is the part that makes the design more than a wish, and it is why the row is *unimplemented*
rather than impossible. `golib/runtime/Goroutine.cs` already maintains a real registry:

| managed surface | what it gives the tracer |
|:--|:--|
| `s_live` (a concurrent map) + `Snapshot()` | the live goroutine set, ordered by id — the generation's `EvGoStatus` prologue |
| `Id` (internal, deliberately opaque to programs) | a stable goroutine id, which is exactly what the format keys on |
| `Current` (thread-local) + `Enter()` scope | create / start / destroy boundaries |
| `Park(WaitReason)` | block and unblock, with a **reason** already spelled in Go's own vocabulary |
| `WaitReason` (13 values) | `ChanSend`, `ChanReceive`, `Select`, `Semacquire`, `Sleep`, `SyncMutexLock`, `SyncRWMutexLock`, `SyncCondWait`, `IOWait` and their nil-channel variants — these map onto Go's block reasons directly |
| `MonotonicClock.Nanoseconds()` | the monotonic timebase `EvFrequency` describes |

So goroutine identity, lifecycle, park reasons and a monotonic clock all exist. **The tracer does not
need a scheduler to be invented; it needs the events it can already witness to be serialized.**

## What it can emit honestly, and what it cannot

**Honestly emittable** — every one of these is a fact the managed runtime observes today:
`EvGoCreate`, `EvGoStart`, `EvGoStop`, `EvGoDestroy`, `EvGoStatus`, `EvGoBlock` and `EvGoUnblock`
with a real reason, `EvFrequency`, and the `EvString`/`EvStacks` tables that name them.

**Not honestly emittable, and this is the boundary the design refuses to blur:**

- **Per-P scheduling.** There are no Ps. The workable model is **one P per OS thread** — the CLR
  thread the goroutine is running on — which makes `EvProcStart`/`EvProcStop` a *description of the
  managed scheduler*, not of Go's. That is a modelling choice and must be labelled as one wherever
  the trace is read. `EvProcSteal` has no managed counterpart at all and should never be emitted.
- **GC phases.** The CLR's collector is not Go's. `EvGCBegin`/`EvGCEnd` and the sweep and
  mark-assist events describe a collector that is not running; emitting them would be fabrication.
  `GcPauseRecorder` can witness *pauses*, which is a different and smaller claim.
- **Syscall boundaries.** `EvGoSyscallBegin`/`End` mark a goroutine leaving Go's scheduler for the
  kernel. The managed runtime does not distinguish those transitions.
- **Heap events.** `EvHeapAlloc`, `EvHeapObject*` and `EvHeapGoal` describe Go's allocator.
- **Stacks.** `EvStack` wants Go frames. CLR stacks can be captured, but the mapping is a separate
  problem and an empty stack table is the honest first answer.

## Increments, in the ruled order

**C-1 — the state machine and the header.** `StartTrace` stops answering the
tracing-not-supported error and instead arms a tracer: an enabled flag with a compare-and-swap so the
second `Start` fails, a writer, the header, one `EvFrequency`, and an empty batch. `StopTrace`
disarms and flushes, and writes nothing afterwards. **Bar A clears here — and this increment must NOT
land alone**, for the laundering reason above; it lands with C-2 or not at all. Footprint: the two
existing hand-owns (`runtime/{windows,linux}/trace_impl.cs`) gain bodies rather than refusals; one
new golib file for the writer; no converter change.

**C-2 — goroutine events.** The registry's create/start/park/unblock/destroy points emit into the
active batch, with `Snapshot()` supplying the generation prologue so every goroutine referenced later
has a prior `EvGoStatus`. This is where the trace starts describing the program. Footprint: hooks at
the `Goroutine.Enter`/`Park` boundaries, the string and stack tables, and the batch writer; the
registry surface is **shared with Q27's goroutine profile**, and the two should agree on one snapshot
primitive rather than growing two.

**C-3 — `go tool trace` readability.** Bar B. Generation framing, batch sizing, and the ordering
rules the parser enforces; the P model labelled as one-per-thread. The acceptance test is external
and unforgiving: the tool opens the file, or it does not.

## What this design does NOT buy

- **It does not make `runtime/trace` a validated row on C-1 alone.** Bar A is too weak to be a
  finish line, and the design says so on purpose.
- **It does not give Go's scheduler.** Anything a reader infers about Ps, stealing, syscalls or GC
  from a managed trace is an artifact of the model, not a measurement of the program.
- **It does not give stacks** in its first three increments.
- **It does not settle `runtime/pprof`.** That row's CPU-profile class is a neighbour, not the same
  question, and it is classified on its own evidence.

## The falsifier that would change the row's disposition

If, in building C-1 through C-3, the v2 format proves **impossible to emit self-consistently from a
managed scheduler** — concretely, if the parser cannot accept a stream whose P model is one-per-thread
and whose GC, syscall and heap event classes are absent — then the row is not merely expensive, and
that finding is the evidence that would justify putting an owner-ruled fourth exclusion class to the
owner. **Until that is measured, it is not claimed:** today's honest statement is that the row is
unimplemented, and this document is the size of implementing it.
