# CENSUS — the runtime row's endgame on windows at the TRAIN I union (read-only, 2026-09-29)

> **Record type:** CENSUS (point-in-time, read-only). Amend with dated blocks; never rewrite; never execute from.
> **Lane:** i9. **Asked by:** COORD (ledger 2026-09-29 00:33): (a) root why tests produce no C# verdict; (b) map every
> failing runtime row to its census family and to the seat or disclosure that addresses it, and name any row nothing
> covers.
> **Read at:** the TRAIN I union `claude/coord-trainI` `9e1594dcd2` (run 1), a fresh worktree, the FULL windows runtime
> row: `go2cs -tests -test-action all -test-config Release -test-timeout 120m`, cgo off, GOROOT set (go1.24.13), MSBuild
> -m:4, go -p 4. Two readings: **TC0** (Release's default, the validation configuration; 36 min) and **tiered**
> (`-test-tiered`; 116 min). WHEA 0 on both, no reboot.
> **Families** are those of `docs/phase4/CENSUS-runtime-divergences-2026-09-28.md` (claude/g-runtime-divergence-census
> `bcb532185b`, read on linux at the TRAIN G union). Coverage is read from the ledger at 2026-09-29 00:33 and from P1's
> `docs/phase4/SIZING-p1-runtime-disclosures-2026-09-29.md` (claude/p1-disclosure-sizing `3a574f110f`). Every failure
> text below is this run's own; host paths are written as `$GOROOT` and `<worktree>`.

## 0. The answer

| | TC0 (the validation configuration) | tiered |
|---|---|---|
| Go results | 10,890 | 10,890 |
| C# verdicts | **10,890** (none missing) | **10,688** (202 missing) |
| Undisclosed divergences | 83 (plus the host's command line as the 84th error line) | not read: the row is truncated |
| Disclosed and absorbed | 11 | — |
| Orphaned disclosure | TestCaller (runtime-capability; it PASSES) | — |

**(a) No verdict = the test host died, and every test that had not finished lost its verdict.** Measured end to end on
the tiered reading, section 1. At TC0 nothing is missing on windows. The TC0 row is also the configuration a bank
reads, so **on windows the endgame row is complete**.

**(b) The 83 undisclosed divergences at TC0:**

| Covered by | Rows | What |
|---|---:|---|
| TRAIN I run 2 | 2 | C1's WriteHeapDump fix `31862ce991` (TestSchedPauseMetrics and its WriteHeapDump subtest) |
| TRAIN J (seated) | 2 | C2's A8 `89b2e846c7` with noinline `04c4e6ae31` (TestRuntimePanic, TestTracebackRuntimeMethod) |
| An approved lane seat, not yet cut | 13 | R's func-cookie seat for W1 (8); P2's A7 (2); C1's A9, A11, A15 (1 each) |
| P1's disclosure seat (sizing `3a574f110f`) | 44 | B1 (25), B7 (10, of them 3 FIXABLE), B6 (4), B3 (2), B4 (2), C6 (1) |
| **Nothing** | **22** | section 3 |

## 1. (a) Why tests produce no C# verdict

**The mechanism, measured.** The converted test host is ONE process. When a goroutine outside any test panics, the
host records `infrastructure-error` "panic on a goroutine outside any test" and then `fail` "test binary died on an
unrecovered panic in a goroutine", and exits. Every test that had not yet reached its terminal event has no verdict:
(1) every `t.Parallel` test already started and parked waiting for the serial phase to end, with its subtests, and (2)
every serial test after the dying one. **Which tests those are depends on timing**: which parallel tests had started
or finished when the host died. That is the run-to-run variance P2 saw.

**The windows instance (tiered):**
- The killer is **TestStackGrowth**'s own watchdog (stack_test.go:123-135): `time.AfterFunc(95% of the time to the test
  deadline)`, which panics `finalizer did not start` if the finalizer registered on `s := new(string)` has not run.
- Under the tiered JIT that finalizer NEVER starts, so the test waits in `wg.Wait()` for 95% of the 120-minute deadline
  (the host is idle; CPU time frozen at 288.3 s over the sampling window), then panics on the timer's goroutine.
- At TC0 the same test PASSES.
- **INFERRED, not measured:** unoptimized tier-0 code keeps `s` alive after `s = nil`. That is the codegen-liveness
  class TestUserArenaLiveness is disclosed under.
- **The partition is exact:** 202 missing = 68 tests under the 37 started-but-unfinished tests + 134 serial tests
  sorting after TestStackGrowth. Other: 0.
- The 36 started-but-unfinished tests besides TestStackGrowth are the parallel set:
  - TestSmhasher* (9), TestGcPacer, TestGdb* (5), TestMemmove* (4), TestNetpoll* (2), TestGCTest* (2);
  - TestCheckPtr, TestCheckPtr2, TestChan*Barrier (2), TestCollisions, TestConcurrentReadsAfterGrowth*, TestCtrlHandler,
    TestDeferKeepAlive, TestPanicTraceback, TestRecoverBeforePanicAfterGoexit*, TestBigGOMAXPROCS.
  These are exactly P2's families (Smhasher*, GcPacer/Steady, TestMemmove, TestNetpoll*, TestGdb*, TestGCTest*).

**Not the event stream.** A second hypothesis was checked and EXCLUDED for both readings. The comparer reads the C#
side's verdicts from its combined stdout+stderr (one pipe), so an event line spliced by another writer would be lost.
- The mechanism is real, and it was reproduced standalone: `Console.WriteLine` under a lock, plus a concurrent
  `Console.Error` or raw stdout-handle writer, on one pipe.
  - An event line of about 256 chars or less is never lost.
  - Past that (the console writer's buffer) 15% to 65% of events are lost, each broken line carrying the other writer's
    bytes.
  - 153 of runtime's 10,890 terminal events exceed 256 chars.
- But in BOTH readings the host's own results file (`--result`, written from the reporter's retained event list, not
  from the stream) holds exactly the verdicts the stream delivered: 0 tests in the file and absent from the stream.
  And at TC0 the C# side wrote nothing outside its event stream except one junction-staging line.
- It is a latent hazard (a runtime `print` or a fatal text during the parallel phase would reach it), not today's
  cause.

**P2's linux readings (50 missing, about 9 names varying).** Their shape is a host death DURING THE PARALLEL PHASE:
after every serial test has finished, only parallel names are lost. TestStackGrowth is serial and its finalizer runs
at TC0, so on linux the killer is another goroutine panic. This census cannot name it; windows at TC0 has none. **The
decisive read is in P2's own artifacts:** the package-level events of each run's `go2cs_test_results.json` carry the
panic text ("panic on a goroutine outside any test" / "test binary died ..."). The missing set should equal the tests
started without a terminal event, plus their subtests. P1's tiered-off linux run read all 10,883, so the killer is
timing- or tier-dependent there too.

**What it takes.** A bank reads TC0 on windows with every verdict present (above). The general fix is the host's, not
a test's: a panic on a non-test goroutine ends Go's test binary too, so dying is faithful. The loss is that the dying
host cannot report the verdicts it never reached. Two readings would settle the linux instance: P2's package events,
and TestStackGrowth read tiered on linux.

## 2. Windows-specific facts for the disclosure seat (P1's sizing read linux only)

- **TestNetpollBreak** reads infrastructure-error on windows (`asmcgocall: no implementation ...`, via the windows
  netpollBreak write). P1's D entry pins linux's named refusal (`netpollBreak: the managed host has no runtime poller
  to break`), which windows never prints. So the windows row needs its own refusal by name first. It is listed as
  uncovered below.
- **TestLFStack / TestLFStackStress** fail on windows with `no lifo` and `Wrong sum -4314593837846761957/4950`, not with
  linux's anonymous `spanOf` dereference. So on windows the packed-pointer round trip, P1's "real question" behind
  spanOf, already fails. Its Q probe can read it here.
- **TestCaller is an orphan on windows as well** (Go pass, C# pass, disclosed runtime-capability). P1 found the same on
  linux, and its removal waited for this windows read.
- **Not on windows at all** (not measured here): B5's six debug-call rows, TestNewOSProc0, TestSignalM,
  TestMemmoveOverflow.

## 3. The 22 rows nothing covers

| Rows | Family | What is missing |
|---|---|---|
| TestCallersDeferNilFuncPanicWithLoop | A1 | R's panic-frames design (ruling `a7dc4570d3`) keeps a DISCLOSURE for the deferreturn (WithLoop) form; no manifest entry, no seat |
| TestTracebackSystem + /panic, /trap | A1 | The re-exec child's Callers list lacks Go's panic frames (redacted `runtime.gopanic`/`panicmem`/`sigpanic`), now that C2's source-path seat cleared the census's C5 reading. Nothing names it |
| TestRuntimeLockMetricsAndProfile + /runtime.lock + /sample-1 | A3 | runtime-internal lock contention never reaches the mutex profile; G's class F (i) (TRAIN I seat 18) fixed sync.Mutex only |
| TestTracebackInlined + 4 subtests | A6 -> B4 | Re-filed B4 by G's printer seat (16:29): Go's `(...)` inline-frame marker. A disclosure candidate, but P1's scope takes only TestTracebackArgs and TestInlineUnwinder from B4 |
| TestStackWrapperStackInlinePanic | B2 | `inlinablePanic not inlined`: runtime-capability per the census, outside P1's scope |
| TestFinalizerRegisterABI, TestLockRankGenerated | A12 | The re-exec child's `-test.v` output lacks Go's trailing PASS; `go run mklockrank.go` from the converted directory is refused Go's internal-import rule. Unowned (the census named the testing host) |
| TestSehLookupFunctionEntry, TestSehUnwind, …DoublePanic, …NilPointerPanic, …Panic | W2 | infrastructure-error (`GetCallerPC`). Disclosable only after a refusal by name (P1's finding 1); windows-only, outside P1's linux-scoped sizing |
| TestNetpollBreak | B6 (windows) | See section 2 |
| TestNumCPU | W3 | infrastructure-error (`asmcgocall`, via its re-exec child); the census left it unread |

## 4. Every divergent row (TC0, windows, union `9e1594dcd2`)

Coverage key: **RUN2** in TRAIN I's run 2; **TRAIN-J** seated in the TRAIN J draft; **APPROVED** a sized and approved
lane seat not yet seated; **P1-SEAT** P1's disclosure seat, with P1's bucket (D disclosable now, R after a refusal by
name, P/P\* deferred with/without a plan record, Q probe first, A fixable); **DISCLOSED** absorbed by the manifest at
this union; **NONE** nothing.

| # | Test | Go / C# | Family | Covered by | What addresses it | C# failure text (first line, this run) |
|---:|---|---|---|---|---|---|
| 1 | TestCallersDeferNilFuncPanicWithLoop | pass / fail | A1 | NONE | R's panic-frames B RESIDUAL: the approved design keeps a DISCLOSURE for the deferreturn (WithLoop) form (ruling a7dc4570d3), but no manifest entry exists and no seat carries it | wanted [runtime.Callers runtime_test.TestCallersDeferNilFuncPanicWithLoop.func1 runtime.gopanic runtime.panicmem runtime.sigpanic runtime.deferreturn runtime_test.TestCal |
| 2 | TestTracebackSystem | pass / fail | A1 | NONE | parent of /panic and /trap |  |
| 3 | TestTracebackSystem/panic | pass / fail | A1 | NONE | the re-exec child's Callers list lacks Go's panic frames (redacted.go:0 entries), after C2's source-path seat cleared the census's C5 reading; no seat or ruling names it | Callers: [0x800080000000e800 0x800080000000f800 0x8000800000010800] |
| 4 | TestTracebackSystem/trap | pass / fail | A1 | NONE | want runtime.gopanic / panicmem / sigpanic above trap3, got an empty redacted frame; no seat or ruling names it | panic: runtime error: invalid memory address or nil pointer dereference |
| 5 | TestFinalizerRegisterABI | pass / fail | A12 | NONE | the re-exec child's -test.v output lacks Go's trailing PASS line (census A12: the testing host, S); unowned | PASS                 TestFinalizerRegisterABI/Pointer |
| 6 | TestLockRankGenerated | pass / fail | A12 | NONE | `go run mklockrank.go` from the converted package directory: use of internal package internal/dag not allowed (census A12: the working-directory half may need a ruling); unowned | $GOROOT\bin\go.exe run mklockrank.go: exit status 1 |
| 7 | TestRuntimeLockMetricsAndProfile | pass / fail | A3 | NONE | parent | NumCPU 24 |
| 8 | TestRuntimeLockMetricsAndProfile/runtime.lock | pass / fail | A3 | NONE | parent of sample-1 |  |
| 9 | TestRuntimeLockMetricsAndProfile/runtime.lock/sample-1 | pass / fail | A3 | NONE | runtime.lock contention never reaches the mutex profile; G's class F (i) (seat 18) fixed sync.Mutex's events only, and no seat names the runtime-lock half | no increase in mutex profile |
| 10 | TestTracebackInlined | pass / fail | A6->B4 | NONE | RE-FILED A6 -> B4 by G's printer seat (16:29): Go's inline-frame `(...)` marker; a disclosure candidate that no disclosure seat carries (P1's sizing takes only TestTracebackArgs and TestInlineUnwinder from B4) |  |
| 11 | TestTracebackInlined/excluded | pass / fail | A6->B4 | NONE | RE-FILED A6 -> B4 by G's printer seat (16:29): Go's inline-frame `(...)` marker; a disclosure candidate that no disclosure seat carries (P1's sizing takes only TestTracebackArgs and TestInlineUnwinder from B4) | goroutine 635532 [running]: |
| 12 | TestTracebackInlined/sigpanic | pass / fail | A6->B4 | NONE | RE-FILED A6 -> B4 by G's printer seat (16:29): Go's inline-frame `(...)` marker; a disclosure candidate that no disclosure seat carries (P1's sizing takes only TestTracebackArgs and TestInlineUnwinder from B4) | goroutine 635530 [running]: |
| 13 | TestTracebackInlined/simple | pass / fail | A6->B4 | NONE | RE-FILED A6 -> B4 by G's printer seat (16:29): Go's inline-frame `(...)` marker; a disclosure candidate that no disclosure seat carries (P1's sizing takes only TestTracebackArgs and TestInlineUnwinder from B4) | goroutine 635529 [running]: |
| 14 | TestTracebackInlined/wrapper | pass / fail | A6->B4 | NONE | RE-FILED A6 -> B4 by G's printer seat (16:29): Go's inline-frame `(...)` marker; a disclosure candidate that no disclosure seat carries (P1's sizing takes only TestTracebackArgs and TestInlineUnwinder from B4) | goroutine 635531 [running]: |
| 15 | TestStackWrapperStackInlinePanic | pass / fail | B2 | NONE | census B2 (runtime-capability: go2cs never inlines); outside P1's sizing scope, no disclosure seat carries it | inlinablePanic not inlined |
| 16 | TestNetpollBreak | pass / infra-error | B6 | NONE | WINDOWS reads infrastructure-error (asmcgocall); P1's D entry pins LINUX's named refusal text, which windows never prints | System.NotImplementedException: asmcgocall: no implementation reached this compilation (assembly, cgo, or a linkname whose push did not arrive) |
| 17 | TestSehLookupFunctionEntry | pass / infra-error | W2 | NONE | infrastructure-error (GetCallerPC stub): needs a refusal by name, then a runtime-capability entry; windows-only, outside P1's linux-scoped sizing | System.NotImplementedException: GetCallerPC: no implementation reached this compilation (assembly, cgo, or a linkname whose push did not arrive) |
| 18 | TestSehUnwind | pass / infra-error | W2 | NONE | infrastructure-error (GetCallerPC stub): needs a refusal by name, then a runtime-capability entry; windows-only, outside P1's linux-scoped sizing | System.NotImplementedException: GetCallerPC: no implementation reached this compilation (assembly, cgo, or a linkname whose push did not arrive) |
| 19 | TestSehUnwindDoublePanic | pass / infra-error | W2 | NONE | infrastructure-error (GetCallerPC stub): needs a refusal by name, then a runtime-capability entry; windows-only, outside P1's linux-scoped sizing | System.NotImplementedException: GetCallerPC: no implementation reached this compilation (assembly, cgo, or a linkname whose push did not arrive) |
| 20 | TestSehUnwindNilPointerPanic | pass / infra-error | W2 | NONE | infrastructure-error (GetCallerPC stub): needs a refusal by name, then a runtime-capability entry; windows-only, outside P1's linux-scoped sizing | System.NotImplementedException: GetCallerPC: no implementation reached this compilation (assembly, cgo, or a linkname whose push did not arrive) |
| 21 | TestSehUnwindPanic | pass / infra-error | W2 | NONE | infrastructure-error (GetCallerPC stub): needs a refusal by name, then a runtime-capability entry; windows-only, outside P1's linux-scoped sizing | System.NotImplementedException: GetCallerPC: no implementation reached this compilation (assembly, cgo, or a linkname whose push did not arrive) |
| 22 | TestNumCPU | pass / infra-error | W3 | NONE | infrastructure-error (asmcgocall stub via its re-exec child); census left it unread | System.NotImplementedException: asmcgocall: no implementation reached this compilation (assembly, cgo, or a linkname whose push did not arrive) |
| 23 | TestSchedPauseMetrics | pass / fail | C2 | RUN2 | parent of the WriteHeapDump subtest (C1's 31862ce991) |  |
| 24 | TestSchedPauseMetrics/runtime/debug.WriteHeapDump | pass / infra-error | C2 | RUN2 | C1's claude/c1-heapdump-managed-write 31862ce991 (validated on the i9: the GolibTests arm red at the union, green at the fix) | System.NotImplementedException: asmcgocall: no implementation reached this compilation (assembly, cgo, or a linkname whose push did not arrive) |
| 25 | TestRuntimePanic | pass / fail | A8 | TRAIN-J | C2's claude/c2-a8 89b2e846c7 (with claude/c2-noinline 04c4e6ae31) | child process did not fail |
| 26 | TestTracebackRuntimeMethod | pass / fail | A8 | TRAIN-J | C2's claude/c2-a8 89b2e846c7 (with claude/c2-noinline 04c4e6ae31) | child process did not fail |
| 27 | TestCleanupAfterFinalizer | pass / fail | A11 | APPROVED | C1's A11 (ruling 20:38, second in C1's order) | result 2, want 1 |
| 28 | TestPeriodicGC | pass / fail | A15 | APPROVED | C1's A15 (owner ruling 20:41: a ~100 ms managed sysmon tick; third in C1's order) | no periodic GC: got 0 GCs, want >= 2 |
| 29 | TestTracebackGeneric | pass / fail | A7 | APPROVED | P2's A7 seat (ruling 20:47): goFrameName appends [...] for a generic function | traceback does not contain expected string: want "testTracebackGenericFn[...](", got |
| 30 | TestTracebackParentChildGoroutines | pass / fail | A7 | APPROVED | P2's A7 seat (ruling 20:47): appendCreatedBy for the current goroutine | did not see parent (goroutine 635533) and child (goroutine 635534) IDs in stack, got goroutine 635534 [running]: |
| 31 | TestGoroutineProfileTrivial | pass / fail | A9 | APPROVED | C1's A9 (ruling 20:38, first in C1's order) | panic: runtime: goroutineProfileWithLabels: filling the profile records each goroutine's stack through sys.GetCallerSP/GetCallerPC, which do not exist in the managed mode |
| 32 | Test64BitReturnStdCall | pass / fail | W1 | APPROVED | R's func-cookie seat (15:03/23:05/23:13): refuseManagedPointerTokens arg 3 (6) / arg 0 (2) | panic: syscall: argument 0 is a managed pointer token, not an address -- the pointee is reference-bearing, so passing it to native code would read or write memory that is |
| 33 | TestBlockingCallback | pass / fail | W1 | APPROVED | R's func-cookie seat (15:03/23:05/23:13): refuseManagedPointerTokens arg 3 (6) / arg 0 (2) | panic: syscall: argument 3 is a managed pointer token, not an address -- the pointee is reference-bearing, so passing it to native code would read or write memory that is |
| 34 | TestCallback | pass / fail | W1 | APPROVED | R's func-cookie seat (15:03/23:05/23:13): refuseManagedPointerTokens arg 3 (6) / arg 0 (2) | panic: syscall: argument 3 is a managed pointer token, not an address -- the pointee is reference-bearing, so passing it to native code would read or write memory that is |
| 35 | TestCallbackGC | pass / fail | W1 | APPROVED | R's func-cookie seat (15:03/23:05/23:13): refuseManagedPointerTokens arg 3 (6) / arg 0 (2) | panic: syscall: argument 3 is a managed pointer token, not an address -- the pointee is reference-bearing, so passing it to native code would read or write memory that is |
| 36 | TestCallbackPanic | pass / fail | W1 | APPROVED | R's func-cookie seat (15:03/23:05/23:13): refuseManagedPointerTokens arg 3 (6) / arg 0 (2) | wrong panic: syscall: argument 3 is a managed pointer token, not an address -- the pointee is reference-bearing, so passing it to native code would read or write memory t |
| 37 | TestCallbackPanicLocked | pass / fail | W1 | APPROVED | R's func-cookie seat (15:03/23:05/23:13): refuseManagedPointerTokens arg 3 (6) / arg 0 (2) | wrong panic: syscall: argument 3 is a managed pointer token, not an address -- the pointee is reference-bearing, so passing it to native code would read or write memory t |
| 38 | TestCallbackPanicLoop | pass / fail | W1 | APPROVED | R's func-cookie seat (15:03/23:05/23:13): refuseManagedPointerTokens arg 3 (6) / arg 0 (2) | wrong panic: syscall: argument 3 is a managed pointer token, not an address -- the pointee is reference-bearing, so passing it to native code would read or write memory t |
| 39 | TestRegisterClass | pass / fail | W1 | APPROVED | R's func-cookie seat (15:03/23:05/23:13): refuseManagedPointerTokens arg 3 (6) / arg 0 (2) | panic: syscall: argument 0 is a managed pointer token, not an address -- the pointee is reference-bearing, so passing it to native code would read or write memory that is |
| 40 | TestArrayHash | pass / fail | B1 | P1-SEAT | P1 bucket Q (probe or ruling first) | go2cs: testing.AllocsPerRun counted 3,260 go2cs-runtime object allocations (977,680 bytes) over 10 run(s) — the figure reported above is an allocation COUNT per run, from |
| 41 | TestCmpIfaceConcreteAlloc | pass / fail | B1 | P1-SEAT | P1 bucket P* (the interface-boxing plan record, ruled P1's to write, 22:01 (3)) | go2cs: testing.AllocsPerRun measured 72 allocated BYTES over 1 run(s) — the figure reported above is BYTES PER RUN, not an allocation count. The go2cs runtime allocation  |
| 42 | TestConcatTempString | pass / fail | B1 | P1-SEAT | P1 bucket P (DESIGN-string-byte-window §7) | go2cs: testing.AllocsPerRun counted 2,000 go2cs-runtime object allocations (88,000 bytes) over 1,000 run(s) — the figure reported above is an allocation COUNT per run, fr |
| 43 | TestIntStringAllocs | pass / fail | B1 | P1-SEAT | P1 bucket Q (probe or ruling first) | go2cs: testing.AllocsPerRun counted 2,000 go2cs-runtime object allocations (64,000 bytes) over 1,000 run(s) — the figure reported above is an allocation COUNT per run, fr |
| 44 | TestNonEscapingConvT2E | pass / fail | B1 | P1-SEAT | P1 bucket P* (the interface-boxing plan record, ruled P1's to write, 22:01 (3)) | go2cs: testing.AllocsPerRun measured 24,000 allocated BYTES over 1,000 run(s) — the figure reported above is BYTES PER RUN, not an allocation count. The go2cs runtime all |
| 45 | TestNonEscapingConvT2I | pass / fail | B1 | P1-SEAT | P1 bucket P* (the interface-boxing plan record, ruled P1's to write, 22:01 (3)) | go2cs: testing.AllocsPerRun measured 24,000 allocated BYTES over 1,000 run(s) — the figure reported above is BYTES PER RUN, not an allocation count. The go2cs runtime all |
| 46 | TestNonEscapingMap | pass / fail | B1 | P1-SEAT | P1 bucket Q (probe or ruling first) | go2cs: testing.AllocsPerRun counted 1,000 go2cs-runtime object allocations (232,000 bytes) over 1,000 run(s) — the figure reported above is an allocation COUNT per run, f |
| 47 | TestRangeStringCast | pass / fail | B1 | P1-SEAT | P1 bucket P (DESIGN-string-byte-window §7) | go2cs: testing.AllocsPerRun counted 1,000 go2cs-runtime object allocations (128,000 bytes) over 1,000 run(s) — the figure reported above is an allocation COUNT per run, f |
| 48 | TestStringConcatenationAllocs | pass / fail | B1 | P1-SEAT | P1 bucket P (DESIGN-string-byte-window §7) | go2cs: testing.AllocsPerRun counted 2,000 go2cs-runtime object allocations (80,000 bytes) over 1,000 run(s) — the figure reported above is an allocation COUNT per run, fr |
| 49 | TestStringIndexHaystack | pass / fail | B1 | P1-SEAT | P1 bucket P (DESIGN-string-byte-window §7) | go2cs: testing.AllocsPerRun counted 1,000 go2cs-runtime object allocations (32,000 bytes) over 1,000 run(s) — the figure reported above is an allocation COUNT per run, fr |
| 50 | TestStringIndexNeedle | pass / fail | B1 | P1-SEAT | P1 bucket P (DESIGN-string-byte-window §7) | go2cs: testing.AllocsPerRun counted 1,000 go2cs-runtime object allocations (32,000 bytes) over 1,000 run(s) — the figure reported above is an allocation COUNT per run, fr |
| 51 | TestZeroConvT2x | pass / fail | B1 | P1-SEAT | P1 bucket P* (the interface-boxing plan record, ruled P1's to write, 22:01 (3)) |  |
| 52 | TestZeroConvT2x/E16 | pass / fail | B1 | P1-SEAT | P1 bucket P* (the interface-boxing plan record, ruled P1's to write, 22:01 (3)) | go2cs: testing.AllocsPerRun measured 24,000 allocated BYTES over 1,000 run(s) — the figure reported above is BYTES PER RUN, not an allocation count. The go2cs runtime all |
| 53 | TestZeroConvT2x/E32 | pass / fail | B1 | P1-SEAT | P1 bucket P* (the interface-boxing plan record, ruled P1's to write, 22:01 (3)) | go2cs: testing.AllocsPerRun measured 24,000 allocated BYTES over 1,000 run(s) — the figure reported above is BYTES PER RUN, not an allocation count. The go2cs runtime all |
| 54 | TestZeroConvT2x/E64 | pass / fail | B1 | P1-SEAT | P1 bucket P* (the interface-boxing plan record, ruled P1's to write, 22:01 (3)) | go2cs: testing.AllocsPerRun measured 24,000 allocated BYTES over 1,000 run(s) — the figure reported above is BYTES PER RUN, not an allocation count. The go2cs runtime all |
| 55 | TestZeroConvT2x/E8 | pass / fail | B1 | P1-SEAT | P1 bucket P* (the interface-boxing plan record, ruled P1's to write, 22:01 (3)) | go2cs: testing.AllocsPerRun measured 24,000 allocated BYTES over 1,000 run(s) — the figure reported above is BYTES PER RUN, not an allocation count. The go2cs runtime all |
| 56 | TestZeroConvT2x/Econstflt | pass / fail | B1 | P1-SEAT | P1 bucket P* (the interface-boxing plan record, ruled P1's to write, 22:01 (3)) | go2cs: testing.AllocsPerRun measured 24,000 allocated BYTES over 1,000 run(s) — the figure reported above is BYTES PER RUN, not an allocation count. The go2cs runtime all |
| 57 | TestZeroConvT2x/Eslice | pass / fail | B1 | P1-SEAT | P1 bucket P* (the interface-boxing plan record, ruled P1's to write, 22:01 (3)) | go2cs: testing.AllocsPerRun measured 56,000 allocated BYTES over 1,000 run(s) — the figure reported above is BYTES PER RUN, not an allocation count. The go2cs runtime all |
| 58 | TestZeroConvT2x/Estr | pass / fail | B1 | P1-SEAT | P1 bucket P* (the interface-boxing plan record, ruled P1's to write, 22:01 (3)) | go2cs: testing.AllocsPerRun measured 32,000 allocated BYTES over 1,000 run(s) — the figure reported above is BYTES PER RUN, not an allocation count. The go2cs runtime all |
| 59 | TestZeroConvT2x/I16 | pass / fail | B1 | P1-SEAT | P1 bucket P* (the interface-boxing plan record, ruled P1's to write, 22:01 (3)) | go2cs: testing.AllocsPerRun measured 24,000 allocated BYTES over 1,000 run(s) — the figure reported above is BYTES PER RUN, not an allocation count. The go2cs runtime all |
| 60 | TestZeroConvT2x/I32 | pass / fail | B1 | P1-SEAT | P1 bucket P* (the interface-boxing plan record, ruled P1's to write, 22:01 (3)) | go2cs: testing.AllocsPerRun measured 24,000 allocated BYTES over 1,000 run(s) — the figure reported above is BYTES PER RUN, not an allocation count. The go2cs runtime all |
| 61 | TestZeroConvT2x/I64 | pass / fail | B1 | P1-SEAT | P1 bucket P* (the interface-boxing plan record, ruled P1's to write, 22:01 (3)) | go2cs: testing.AllocsPerRun measured 24,000 allocated BYTES over 1,000 run(s) — the figure reported above is BYTES PER RUN, not an allocation count. The go2cs runtime all |
| 62 | TestZeroConvT2x/I8 | pass / fail | B1 | P1-SEAT | P1 bucket P* (the interface-boxing plan record, ruled P1's to write, 22:01 (3)) | go2cs: testing.AllocsPerRun measured 24,000 allocated BYTES over 1,000 run(s) — the figure reported above is BYTES PER RUN, not an allocation count. The go2cs runtime all |
| 63 | TestZeroConvT2x/Islice | pass / fail | B1 | P1-SEAT | P1 bucket P* (the interface-boxing plan record, ruled P1's to write, 22:01 (3)) | go2cs: testing.AllocsPerRun measured 56,000 allocated BYTES over 1,000 run(s) — the figure reported above is BYTES PER RUN, not an allocation count. The go2cs runtime all |
| 64 | TestZeroConvT2x/Istr | pass / fail | B1 | P1-SEAT | P1 bucket P* (the interface-boxing plan record, ruled P1's to write, 22:01 (3)) | go2cs: testing.AllocsPerRun measured 32,000 allocated BYTES over 1,000 run(s) — the figure reported above is BYTES PER RUN, not an allocation count. The go2cs runtime all |
| 65 | TestStartLineAsm | pass / infra-error | B3 | P1-SEAT | P1 bucket R: refuse AsmFunc by name, then runtime-capability | System.NotImplementedException: AsmFunc: no implementation reached this compilation (assembly, cgo, or a linkname whose push did not arrive) |
| 66 | TestUnsafePoint | pass / fail | B3 | P1-SEAT | P1 bucket D | can't objdump exit status 1 |
| 67 | TestInlineUnwinder | pass / fail | B4 | P1-SEAT | P1 bucket D | failed to resolve tiuTest at PC 0xffff800000020000 |
| 68 | TestTracebackArgs | pass / fail | B4 | P1-SEAT | P1 bucket D (WEAK; ruling asked: whole or split) | traceback does not contain expected string: want "testTracebackArgs1(0x1, 0x2, 0x3, 0x4, 0x5)", got |
| 69 | TestG0StackOverflow | pass / fail | B6 | P1-SEAT | P1 bucket R (ruling 22:01 (2)) | INFRASTRUCTURE-ERROR TestG0StackOverflow — System.NotImplementedException: GetCallerSP: no implementation reached this compilation (assembly, cgo, or a linkname whose pus |
| 70 | TestGCTestMoveStackOnNextCall | pass / fail | B6 | P1-SEAT | P1 bucket R (spanOf answers nil, ruling 22:01 (1)) | old stack pointer 20081410180, new stack pointer 20081410180 |
| 71 | TestSystemstackFramePointerAdjust | pass / fail | B6 | P1-SEAT | P1 bucket D (named refusal present) | panic: runtime: shrinkstack: goroutines are CLR threads with no Go stack to shrink |
| 72 | TestTracebackSystemstack | pass / infra-error | B6 | P1-SEAT | P1 bucket R (ruling 22:01 (2): refuse at the representational point) | System.NotImplementedException: GetCallerPC: no implementation reached this compilation (assembly, cgo, or a linkname whose push did not arrive) |
| 73 | TestArenaCollision | pass / fail | B7 | P1-SEAT | P1 bucket R (KeepNArenaHints refusal) | FAIL                 TestArenaCollision — panic: runtime error: invalid memory address or nil pointer dereference |
| 74 | TestGCInfo | pass / fail | B7 | P1-SEAT | P1 bucket D (WEAK eface/iface arm) | bad GC program for bss eface: |
| 75 | TestGCTestPointerClass | pass / fail | B7 | P1-SEAT | P1 bucket R (spanOf nil) | for 0x20081a1c200, want class stack, got other |
| 76 | TestGroupSizeZero | pass / fail | B7 | P1-SEAT | P1 bucket A: FIXABLE (map descriptor Group), P1's after the disclosure seat (stamp 23:06) | panic: runtime error: invalid memory address or nil pointer dereference |
| 77 | TestLFStack | pass / fail | B7 | P1-SEAT | P1 bucket Q (behind spanOf; probe first). WINDOWS text differs from linux: `no lifo`, not the spanOf dereference | no lifo |
| 78 | TestLFStackStress | pass / fail | B7 | P1-SEAT | P1 bucket Q. WINDOWS text differs from linux: `Wrong sum`, not the spanOf dereference | Wrong sum -4314593837846761957/4950 |
| 79 | TestSmhasherAvalanche | pass / fail | B7 | P1-SEAT | P1 bucket A: FIXABLE (nilinterhash) | panic: runtime error: dereference of a managed pointer with no address (*eface over 0x80104a0500000000 — the order token of a reference-bearing pointee at offset 0, Q44 § |
| 80 | TestStringW | pass / fail | B7 | P1-SEAT | P1 bucket A: FIXABLE (rawstring) | panic: runtime error: invalid memory address or nil pointer dereference |
| 81 | TestTinyAlloc | pass / fail | B7 | P1-SEAT | P1 bucket D | no bytes allocated within the same 8-byte chunk |
| 82 | TestTinyAllocIssue37262 | pass / fail | B7 | P1-SEAT | P1 bucket D | unable to get a fresh tiny slot |
| 83 | TestMemStats | pass / fail | C6 | P1-SEAT | P1 bucket D (ruled disclosed 00:32) | Mallocs = 0: zero value |
| 84 | TestFunctionAlignmentTraceback | pass / fail | - | DISCLOSED | absorbed by the manifest at this union | panic: runtime error: invalid memory address or nil pointer dereference |
| 85 | TestLineNumber | pass / fail | - | DISCLOSED | absorbed by the manifest at this union | compLit[0].lineA on firstLine+5 want firstLine+9 (firstLine=61, val=66) |
| 86 | TestPinnerConstStringData | pass / fail | - | DISCLOSED | absorbed by the manifest at this union | not marked as pinned |
| 87 | TestReadMetrics | pass / fail | - | DISCLOSED | absorbed by the manifest at this union | live bytes is 0 |
| 88 | TestReadMetricsConsistency | pass / fail | - | DISCLOSED | absorbed by the manifest at this union | scannable GC space is empty: 0 |
| 89 | TestStartLine | pass / fail | - | DISCLOSED | absorbed by the manifest at this union |  |
| 90 | TestStartLine/inline | pass / fail | - | DISCLOSED | absorbed by the manifest at this union | panic: caller runtime_test.inlineFunc1 inlined got false want true |
| 91 | TestStartLine/inline-closure | pass / fail | - | DISCLOSED | absorbed by the manifest at this union | panic: caller runtime_test.inlineClosure.func1 inlined got false want true |
| 92 | TestUserArenaLiveness | pass / fail | - | DISCLOSED | absorbed by the manifest at this union |  |
| 93 | TestUserArenaLiveness/Finalizer | pass / fail | - | DISCLOSED | absorbed by the manifest at this union | expected arena-referenced object to be finalized |
| 94 | TestUserArenaLiveness/Free | pass / fail | - | DISCLOSED | absorbed by the manifest at this union | expected arena-referenced object to be finalized |
