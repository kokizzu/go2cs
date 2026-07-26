# DESIGN — `runtime.Goexit`: the managed unwind shape

> **Status: DECISION REQUESTED (user-owned).** Raised by the r14 runtime-stub arc, which classified
> `Goexit` capability-blocked rather than land an architectural change unilaterally (charter §10).
> Until ruled, tests requiring it are capability-gated — reported honestly, never crashing the host
> (`bce7c12e7`). One sync test (`TestOnceFuncGoexit`) waits on the ruling; more will as coverage
> widens (`testing.T.FailNow` is specified in terms of Goexit).

## 1. What Go specifies

`runtime.Goexit` terminates **the calling goroutine only**:

1. All of that goroutine's **deferred calls run**, in the usual order.
2. `recover()` inside those defers returns **nil** — Goexit is *not* a panic, and a defer cannot
   cancel it.
3. Other goroutines are unaffected.
4. **From the main goroutine:** `main` terminates without returning, and the *program continues*
   running the other goroutines; if they all exit, the runtime crashes with a fatal
   "no goroutines" error.

Property 2 is the load-bearing subtlety: any implementation on `PanicException` would make
`recover()` observe and swallow a Goexit, which real Go code (sync's `OnceFunc` panic-replay
machinery, `testing`'s `FailNow`) explicitly distinguishes.

## 2. The sketched shape (from the r14 arc — cheap, fits existing machinery)

A dedicated **`GoexitException`** in golib:

- **Not a `PanicException`** — `recover()`'s implementation already keys on `PanicException`, so
  during Goexit unwinding it returns nil with **zero changes** to the recover path. Property 2
  falls out by construction.
- **Unwinds through `GoFunc.HandleFinally`** — the existing defer machinery runs deferred calls
  during the unwind exactly as it does for a panic. Property 1 falls out of the machinery that
  already exists.
- **Swallowed at the goroutine root** — the `GoFunc`/goroutine-thread entry catches
  `GoexitException` specifically and ends the thread silently (a `PanicException` reaching the same
  point remains the fatal-crash path, unchanged). Property 3 falls out of thread isolation.
- The **testing host's** per-test root treats a `GoexitException` escaping a test function the way
  Go's `tRunner` does: the test goroutine ended without completing — which is precisely `FailNow`'s
  contract, so this shape is also the future path for converted `testing.T.Fatal/FailNow` fidelity.

Estimated footprint: one golib exception type, one catch clause at the goroutine root, one in the
test host, and `runtime.Goexit`'s hand-owned body (`throw new GoexitException()`). No converter
change; no emission change.

## 3. The open question — main-goroutine Goexit

The one part with no obviously-right managed shape. Options:

| # | Shape | Fidelity | Cost/risk |
|---|---|---|---|
| **A** | Main thread's root catch **waits for all live goroutine threads**, then exits with Go's fatal "no goroutines" error text and code | Faithful to the spec, including the crash-when-all-exit | Needs a live-goroutine registry (golib's goroutine tracking already counts for the deadlock detector — verify reuse); some care with non-goroutine threads (timers, finalizers) |
| **B** | Main-goroutine Goexit = process exit | Unfaithful (kills goroutines Go would keep running) | Trivial, silently wrong for the exact programs that use it |
| **C** | Implement the goroutine-side shape now; **keep main-goroutine Goexit capability-gated** | Faithful where implemented; honest where not | Zero — the gate exists today; main-goroutine Goexit is vanishingly rare outside runtime tests |

## 4. Recommendation

**Land §2 now; take Option C for the main-goroutine case.** The goroutine-side shape is cheap,
semantically exact on all three properties, unblocks `TestOnceFuncGoexit`, removes a whole
host-crash class, and opens the door to `FailNow` fidelity. Option A can graduate from C later as
its own small arc if a real consumer appears — the capability gate makes the deferral visible
rather than silent. Option B is rejected outright (silently wrong).

**Verification plan when ruled:** behavioral guard `GoexitDefers` (a goroutine calls Goexit; its
defers observably run; `recover()` in those defers prints nil; main continues; output-compared vs
`go run`); sync re-run with `TestOnceFuncGoexit` expected to move capability-excluded → pass; full
suite + 44-package sweep (golib change class); the main-goroutine case stays gated with a
capability-gate positive control.
