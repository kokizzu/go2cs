# B, the panic-path frames: gate PREDICTION

Written 2026-09-27, before any gate runs. Base: the TRAIN G union claude/coord-trainG e6fc210500.
Cut: claude/r-panic-frames (red c85f716f0d, fix ca684cb8ed). golib and runtime's hand-owned
managed_impl.cs only, with no converter change: no emission footprint and no CNR owed.

## runtime row (windows, -tests, base vs cut)

Moves, all fail → pass:
- TestCallersPanic, TestCallersDoublePanic, TestCallersNilPointerPanic, TestCallersDivZeroPanic,
  TestCallersDeferNilFuncPanic.
- TestStackWrapperStackPanic/sigpanic/CallersFrames and TestStackWrapperStackPanic/panicwrap/CallersFrames.
  The panicwrap subtest passes on the wrapper's name only: its fault frames read panicmem and sigpanic
  where Go reads panicwrap, which is named, not modeled.

Stays:
- TestCallersDeferNilFuncPanicWithLoop stays FAIL, the ruled deferreturn disclosure.
- TestCallersAfterRecovery, TestCallersAbortedPanic and TestCallersAbortedPanic2 stay PASS.
- Every other verdict: 0 moved.

The parent verdicts: TestStackWrapperStackPanic moves to pass only if its /Stack subtests pass too,
and those are P2's F, already in the base.

## Ranked uncertainties

1. OTHER readers of Callers inside a panic's deferred call that now see the spliced frames: runtime
   tests that assert a depth or a frame list from a recovering deferred call, and runtime/pprof's heap
   and block recorders (they go through callers()). A move there is a finding, not a mechanism miss:
   Go's frames are the spliced ones.
2. The owner check failing on a real shape the arms do not cover, which would leave a predicted row
   at fail with its live stack only. That is visible in the row's failure text.
3. The per-deferring-return push/pop cost, on seat (iii)'s PerfDefer benchmarks (before and after,
   stated, not scored).

## Other gates

- GolibTests, Release and Debug: the base's failure set, with PanicFramesTests 11/11.
- Full behavioral suite: 0 moved. No behavioral program calls Callers from a panic's deferred call.
- runtime/pprof sweep: PASS. runtime/debug's TestSetCrashOutput: unchanged.
- The io flatten depth tests: unchanged, because no panic is running there.
- linux (WSL): the same five TestCallers* rows move, with sigpanic at signal_unix.go:925.

---

## RESTATED 2026-09-27, after COORD's review and the re-cut, BEFORE any gate runs (the text above stands, superseded)

Cut: claude/r-panic-frames at the re-cut, red 8e2dd0fa8a and fix 2ff712b522, on top of a419543c14.
The review (C6) was right that the first text contradicted itself on parent verdicts. This block
replaces it. The base verdicts it rests on were READ first: a filtered windows runtime read at the
TRAIN G union e6fc210500, `^(TestStackWrapperStackPanic|TestCallers)`, evidence
frames-evidence/gates/base-wrapper-comparison.json on R-LAPTOP. Both /Stack subtests PASS at base;
their parents and both /CallersFrames subtests FAIL.

### runtime row (windows, -tests, base e6fc210500 vs cut)

EXACTLY 10 verdicts move, all fail → pass:
- TestCallersPanic, TestCallersDoublePanic, TestCallersNilPointerPanic, TestCallersDivZeroPanic,
  TestCallersDeferNilFuncPanic;
- TestStackWrapperStackPanic/sigpanic/CallersFrames and TestStackWrapperStackPanic/panicwrap/CallersFrames;
- their parents TestStackWrapperStackPanic/sigpanic and TestStackWrapperStackPanic/panicwrap, and
  TestStackWrapperStackPanic itself, because all of its subtests then pass.

These stay as they are:
- TestCallersDeferNilFuncPanicWithLoop stays FAIL (deferreturn, the ruled disclosure).
- TestCallersEndlineno stays FAIL (not this cut's).
- TestCallers, TestCallersAfterRecovery, TestCallersAbortedPanic, TestCallersAbortedPanic2 and
  TestCallersFromWrapper stay PASS.
- Every other verdict: 0 moved.

### Ranked uncertainties

1. OTHER readers of Callers, or of runtime.Caller (callers() splices too), inside a panic's deferred
   call. A move there is a finding, not a mechanism miss.
2. A real shape reaching one of the re-cut's refusals where the arms say "no splice". That would leave
   a predicted row at fail with the live stack only, visible in its failure text.
3. The defer cost, measured and stated (not scored), on runtime's BenchmarkDefer shapes. The converted
   test host does not run top-level Benchmarks, so the gate times the same four shapes in-process
   against both golib builds.

### Residuals the re-cut states (each splices NOTHING, never a partial list)

- A panic re-raised past its first catcher. This covers every intermediate deferring frame, the
  closure-wrapped replacing panic (review shapes A and B), the catcher's caller deferred, a nil
  deferred func faulting in a callee, and recursion. The one accepted re-raise is the deferred delegate
  that is itself the first catcher.
- A runtime error Go raises through a runtime frame that is not modelled: goPanicIndex, the
  goPanicSlice family, panicdottypeE/I, mapassign*, closechan, and others.
- A panic a deferred call raises while a Goexit runs its sequence (runtime.Goexit is not modelled).
- deferreturn, and panicwrap, whose fault frames read panicmem/sigpanic.

### Other gates (as before)

- GolibTests, Release and Debug: the base's failure set, with PanicFramesTests 11/11 and
  PanicFramesReviewTests 17/17.
- Full behavioral suite: 0 moved.
- runtime/pprof, runtime/debug and io sweeps: PASS.
- linux (WSL): the five TestCallers* rows and the wrapper subtree move the same way.

---

## AMENDED 2026-09-28, after COORD's verification of the re-cut, before the gates' results are read

The runtime row prediction above (EXACTLY 10 moves, 0 else) stands unchanged: none of the verification's
shapes is in a predicted row. The RESIDUALS change, and each still splices NOTHING, never a partial list:
- EVERY panic a deferred call raises from a closure, while panicking or on a normal return. This covers
  the normal-return and after-recovery shapes, `defer panic(v)`, the two-link chain and recover-then-panic.
  The converter's defer wrappers cannot be told from Go closures at run time, and Go elides its deferwrap
  except over the panic machinery. The accepted ends-at-Run sites are now only the zero-argument
  nil-func thunk and the deferred delegate that is itself the first catcher.
- A nil func reached through golib's defer<T...> closure or the converter's `() => c()` (Go's deferwrap
  frame is not modelled).
- A site owned by a Run on another thread (range-over-func).
- A deferred call's panic during ANY Goexit: runtime.Goexit, the test host's FailNow/SkipNow, and a
  range-over-func seq's Goexit.
- Every runtime error except an explicit panic, a nil dereference and an integer divide.
