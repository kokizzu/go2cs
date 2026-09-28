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
