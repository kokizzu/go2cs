# CENSUS — the runtime bank ledger: every windows TC0 divergence, its seat and that seat's state (read-only, 2026-09-29)

> **Record type:** CENSUS (point-in-time, read-only). Amend with dated blocks; never rewrite; never execute from.
> **Lane:** C2. **Asked by:** COORD (inbox 2026-09-29T10:58:32Z): map each of the i9 census's 83 undisclosed windows TC0
> divergences plus the 11 disclosed to its covering seat, give that seat's state, and predict the runtime windows row
> after TRAIN J.
> **Read from** (every ref read back by `ls-remote` before the read, 2026-09-29):
> - the i9's census `claude/i9-runtime-endgame-census` `9c9f02bf75`, `docs/phase4/CENSUS-runtime-endgame-windows-2026-09-29.md`
>   section 4 (the rows, read at the TRAIN I **run 1** union `9e1594dcd2`);
> - `docs/phase4/LEDGER.md` on `claude/mailbox` `cc2810830b`, every line from 2026-09-28 00:01 to 2026-09-29 05:58;
> - the TRAIN J draft `.claude/coord-scripts/trainJ/tJ-seats-draft.txt` on `claude/coord-handover` `399d9c61ce` (21 seats);
> - `src/core/runtime/go2cs_test_disclosures.json` at master `2ff42f7a16` (10 entries), the TRAIN I run 2 union
>   `61cf81290c` (15), `claude/p1-runtime-disclosures` `7c27acb6c5` (51) and `claude/c2-a12` `340dfc424c` (11).
>
> **Nothing was run for the prediction.** It is each seat's own ledger reading, carried to windows. Section 4 names what
> that carry assumes.

## 0. The answer

**States.** *landed* = on master `2ff42f7a16`. *TRAIN I* = in the run 2 union `61cf81290c`; TRAIN I is ruled to land
but is not on master. *TRAIN J draft* = a seat in the 21-seat draft. *ruled-uncut* = a ruling names the owner and the
cut, and no ref carries it yet. *cut, not accepted* = a ref carries it, and no ruling seats it. *unassigned* = no
seat and no owner.

**(1) The runtime windows row after TRAIN J as drafted.** These are the 94 census rows, 83 undisclosed plus 11
disclosed:

| After TRAIN J | Rows | By name |
|---|---:|---|
| **pass** | 14 | TestSchedPauseMetrics, /runtime/debug.WriteHeapDump (TRAIN I); TestRuntimePanic, TestTracebackRuntimeMethod (A8); TestFinalizerRegisterABI (A12); TestCleanupAfterFinalizer (A11); TestTracebackGeneric (A7 generic half); TestGoroutineProfileTrivial (A9); TestCallback, TestCallbackGC, TestBlockingCallback, TestCallbackPanic, TestCallbackPanicLocked, TestCallbackPanicLoop (W1 func cookie) |
| **disclosed** | 47 | the 11 already absorbed; TestLockRankGenerated (A12); and 35 rows P1's `7c27acb6c5` absorbs on windows: TestCmpIfaceConcreteAlloc, TestNonEscapingConvT2E/I, TestRangeStringCast, TestStringConcatenationAllocs, TestStringIndexHaystack/Needle, TestZeroConvT2x plus its 13 subtests, TestStartLineAsm, TestUnsafePoint, TestInlineUnwinder, TestTracebackArgs, TestG0StackOverflow, TestGCTestMoveStackOnNextCall, TestSystemstackFramePointerAdjust, TestTracebackSystemstack, TestArenaCollision, TestGCInfo, TestGCTestPointerClass, TestTinyAlloc, TestTinyAllocIssue37262, TestMemStats |
| **still failing** | 33 | listed with their owners in the next table |

The 33 still failing, by the state of the seat that covers them:

| State | Rows | Seat, and the rows |
|---|---:|---|
| ruled-uncut | 16 | **P1's next manifest commit**: TestCallersDeferNilFuncPanicWithLoop (STRUCTURAL); TestTracebackSystem, /panic, /trap (RUNTIME-CAPABILITY, 04:58); TestTracebackInlined and its 4 subtests, TestStackWrapperStackInlinePanic (RUNTIME-CAPABILITY, 04:22); TestLFStack, TestLFStackStress, TestNonEscapingMap (STRUCTURAL); TestArrayHash, TestIntStringAllocs, TestConcatTempString (DEFERRED; all 02:57) |
| ruled-uncut | 7 | **The i9's windows refusal seat** (04:22): TestSehLookupFunctionEntry, TestSehUnwind, TestSehUnwindDoublePanic, TestSehUnwindNilPointerPanic, TestSehUnwindPanic, TestNumCPU, TestNetpollBreak (the windows path) |
| ruled-uncut | 3 | **G's A3 cut** (04:35; TRAIN J if ready at assembly, else TRAIN K): TestRuntimeLockMetricsAndProfile, /runtime.lock, /runtime.lock/sample-1 |
| ruled-uncut | 1 | **C1's A15** (20:41; C1's NEXT at 05:22): TestPeriodicGC |
| ruled-uncut | 1 | **P2's created-by seat**, `claude/p2-created-by` (03:45; no ref yet): TestTracebackParentChildGoroutines |
| ruled-uncut | 1 | **P1's ifaceHash registration**, on top of C2's manual-signature lift `9c6305277f` (TRAIN J draft), which it needs: TestSmhasherAvalanche |
| cut, not accepted | 2 | **P1's `claude/p1-runtime-fixable` `bdfbd2244f`** (PASS on linux, 02:57): TestGroupSizeZero, TestStringW |
| **unassigned** | **2** | Test64BitReturnStdCall, TestRegisterClass: section 1 |

After every ruled-uncut and cut seat above lands, the prediction is 7 more pass (A3 ×3, A15, created-by,
TestGroupSizeZero, TestStringW), 1 more pass once ifaceHash is registered (TestSmhasherAvalanche), and 23 more disclosed.
The unassigned 2 still fail.

**(2) The EXACT unassigned list: 2 rows.** Test64BitReturnStdCall and TestRegisterClass.

**(3) Rows claimed by two seats: none.** Four rows come near it; each is resolved in section 2.

**Also standing after TRAIN J as drafted: the orphaned TestCaller entry.** It is still in `7c27acb6c5`, and TestCaller
passes on both flavours. The removal is ruled (04:22) and rides P1's next manifest commit, so TRAIN J carries the
orphan unless that commit is seated.

## 1. The unassigned two

The census credits all 8 W1 rows to R's func-cookie seat. The seat's ledger line (`d2f1759818`, 04:50) moves exactly
6 rows, TestCallback, TestCallbackGC, TestBlockingCallback, TestCallbackPanic, TestCallbackPanicLoop and
TestCallbackPanicLocked, the ARG 3 half. R's CB sizing (15da8805b2, 15:03) split the 8 by argument index. It ruled
the **ARG 0 half**, Test64BitReturnStdCall and TestRegisterClass, **DISCLOSED at the runtime bank as
runtime-capability**: test-local reference-bearing structs go by address through the generic Proc.Call, and the door's
refusal is correct and by name. It named a retirement plan: a golib Go-layout marshal at the door, which needs a
design and a ruling. **No seat carries the entry and no owner is named.** Two things make it quick to mint:

- the row already refuses by name (`syscall: argument 0 is a managed pointer token`), which the loader's fail arm
  admits;
- both texts are in the census.

It is windows-only (`syscall_windows_test.go`), so P1's linux-scoped manifest commit is not its natural home. The i9's
windows refusal seat is, since its manifest hunks already keep-both with P1's at TRAIN J.

## 2. Near-double claims, each resolved to one owner

| Row | The two claims | Resolved |
|---|---|---|
| Test64BitReturnStdCall, TestRegisterClass | the census column (R's func cookie); the 15:03 (4) ruling (disclose) | the cookie seat does not move them. Unassigned, section 1 |
| TestNetpollBreak | P1's entry in `7c27acb6c5`; the i9's windows seat | split by platform. P1's entry carries `platforms: [linux]` and pins linux's named refusal, and windows reads `asmcgocall`. The i9's seat owns the windows row; manifest hunks keep-both (04:22) |
| TestTracebackSystem, /panic, /trap | R's panic-frame design (04:22, "R SIZES"); P1's manifest | R's sizing (04:58) found the design does not carry the row, and ruled P1 to mint the three entries. One owner, P1 |
| TestCallersDeferNilFuncPanicWithLoop | R's panic-frames ruling `a7dc4570d3` keeps a disclosure; P1's manifest | the same entry. R's design names it and P1 mints it (04:22). One owner, P1 |

Two chains are ordered, not double: TestSmhasherAvalanche needs C2's `9c6305277f` (TRAIN J), then P1's ifaceHash
registration; TestGroupSizeZero and TestStringW need only `bdfbd2244f`.

## 3. Every row

"Covered by" is the census's own column. "After" is the predicted state after TRAIN J as drafted.

| # | Test | Family | Census covered by | Seat | State | After | Note |
|---:|---|---|---|---|---|---|---|
| 1 | TestCallersDeferNilFuncPanicWithLoop | A1 | NONE | P1 next manifest commit (rulings bdfbd2244f 02:57, 9c9f02bf75 04:22, 61cf81290c 04:58) | ruled-uncut | fail |  |
| 2 | TestTracebackSystem | A1 | NONE | P1 next manifest commit (rulings bdfbd2244f 02:57, 9c9f02bf75 04:22, 61cf81290c 04:58) | ruled-uncut | fail | ruled RUNTIME-CAPABILITY on R1, R2 stated |
| 3 | TestTracebackSystem/panic | A1 | NONE | P1 next manifest commit (rulings bdfbd2244f 02:57, 9c9f02bf75 04:22, 61cf81290c 04:58) | ruled-uncut | fail | ruled RUNTIME-CAPABILITY on R1, R2 stated |
| 4 | TestTracebackSystem/trap | A1 | NONE | P1 next manifest commit (rulings bdfbd2244f 02:57, 9c9f02bf75 04:22, 61cf81290c 04:58) | ruled-uncut | fail | ruled RUNTIME-CAPABILITY on R1, R2 stated |
| 5 | TestFinalizerRegisterABI | A12 | NONE | C2 claude/c2-a12 340dfc424c | TRAIN J draft | pass | child PASSes; the bare package PASS line |
| 6 | TestLockRankGenerated | A12 | NONE | C2 claude/c2-a12 340dfc424c | TRAIN J draft | disclosed | host-identity entry |
| 7 | TestRuntimeLockMetricsAndProfile | A3 | NONE | G A3 lock-profile cut (ruling 6f2240e680 04:35; TRAIN J if ready, else K) | ruled-uncut | fail |  |
| 8 | TestRuntimeLockMetricsAndProfile/runtime.lock | A3 | NONE | G A3 lock-profile cut (ruling 6f2240e680 04:35; TRAIN J if ready, else K) | ruled-uncut | fail |  |
| 9 | TestRuntimeLockMetricsAndProfile/runtime.lock/sample-1 | A3 | NONE | G A3 lock-profile cut (ruling 6f2240e680 04:35; TRAIN J if ready, else K) | ruled-uncut | fail |  |
| 10 | TestTracebackInlined | A6->B4 | NONE | P1 next manifest commit (rulings bdfbd2244f 02:57, 9c9f02bf75 04:22, 61cf81290c 04:58) | ruled-uncut | fail |  |
| 11 | TestTracebackInlined/excluded | A6->B4 | NONE | P1 next manifest commit (rulings bdfbd2244f 02:57, 9c9f02bf75 04:22, 61cf81290c 04:58) | ruled-uncut | fail |  |
| 12 | TestTracebackInlined/sigpanic | A6->B4 | NONE | P1 next manifest commit (rulings bdfbd2244f 02:57, 9c9f02bf75 04:22, 61cf81290c 04:58) | ruled-uncut | fail |  |
| 13 | TestTracebackInlined/simple | A6->B4 | NONE | P1 next manifest commit (rulings bdfbd2244f 02:57, 9c9f02bf75 04:22, 61cf81290c 04:58) | ruled-uncut | fail |  |
| 14 | TestTracebackInlined/wrapper | A6->B4 | NONE | P1 next manifest commit (rulings bdfbd2244f 02:57, 9c9f02bf75 04:22, 61cf81290c 04:58) | ruled-uncut | fail |  |
| 15 | TestStackWrapperStackInlinePanic | B2 | NONE | P1 next manifest commit (rulings bdfbd2244f 02:57, 9c9f02bf75 04:22, 61cf81290c 04:58) | ruled-uncut | fail |  |
| 16 | TestNetpollBreak | B6 | NONE | the i9 windows refusal seat (ruling 9c9f02bf75 04:22; no ref yet) | ruled-uncut | fail | P1's entry is linux-only (platforms [linux]); windows reads asmcgocall |
| 17 | TestSehLookupFunctionEntry | W2 | NONE | the i9 windows refusal seat (ruling 9c9f02bf75 04:22; no ref yet) | ruled-uncut | fail |  |
| 18 | TestSehUnwind | W2 | NONE | the i9 windows refusal seat (ruling 9c9f02bf75 04:22; no ref yet) | ruled-uncut | fail |  |
| 19 | TestSehUnwindDoublePanic | W2 | NONE | the i9 windows refusal seat (ruling 9c9f02bf75 04:22; no ref yet) | ruled-uncut | fail |  |
| 20 | TestSehUnwindNilPointerPanic | W2 | NONE | the i9 windows refusal seat (ruling 9c9f02bf75 04:22; no ref yet) | ruled-uncut | fail |  |
| 21 | TestSehUnwindPanic | W2 | NONE | the i9 windows refusal seat (ruling 9c9f02bf75 04:22; no ref yet) | ruled-uncut | fail |  |
| 22 | TestNumCPU | W3 | NONE | the i9 windows refusal seat (ruling 9c9f02bf75 04:22; no ref yet) | ruled-uncut | fail | refusal + entry, or a fix if sized cheap |
| 23 | TestSchedPauseMetrics | C2 | RUN2 | C1 claude/c1-heapdump-managed-write 31862ce991 | TRAIN I | pass |  |
| 24 | TestSchedPauseMetrics/runtime/debug.WriteHeapDump | C2 | RUN2 | C1 claude/c1-heapdump-managed-write 31862ce991 | TRAIN I | pass |  |
| 25 | TestRuntimePanic | A8 | TRAIN-J | C2 claude/c2-a8-r1a 7eeb468c76 (c2-a8 slot; carries c2-noinline via g-inline-locations) | TRAIN J draft | pass |  |
| 26 | TestTracebackRuntimeMethod | A8 | TRAIN-J | C2 claude/c2-a8-r1a 7eeb468c76 (c2-a8 slot; carries c2-noinline via g-inline-locations) | TRAIN J draft | pass |  |
| 27 | TestCleanupAfterFinalizer | A11 | APPROVED | C1 claude/c1-a11-cleanup-after-finalizer 7db31a5470 | TRAIN J draft | pass |  |
| 28 | TestPeriodicGC | A15 | APPROVED | C1 A15 periodic GC tick (ruling 20:41) | ruled-uncut | fail |  |
| 29 | TestTracebackGeneric | A7 | APPROVED | P2 claude/p2-traceback-decoration 0b7492b33a | TRAIN J draft | pass |  |
| 30 | TestTracebackParentChildGoroutines | A7 | APPROVED | P2 claude/p2-created-by (ruling 0b7492b33a 03:45; no ref yet) | ruled-uncut | fail |  |
| 31 | TestGoroutineProfileTrivial | A9 | APPROVED | C1 claude/c1-a9-goroutine-profile b415d4d9d9 | TRAIN J draft | pass |  |
| 32 | Test64BitReturnStdCall | W1 | APPROVED | none: ruled DISCLOSE at the runtime bank (15da8805b2 15:03 (4)), no seat or owner named | unassigned | fail | census credits R's func cookie; that seat moves the 6 arg-3 rows only (arg-0 layout half) |
| 33 | TestBlockingCallback | W1 | APPROVED | R claude/r-func-cookie d2f1759818 | TRAIN J draft | pass |  |
| 34 | TestCallback | W1 | APPROVED | R claude/r-func-cookie d2f1759818 | TRAIN J draft | pass |  |
| 35 | TestCallbackGC | W1 | APPROVED | R claude/r-func-cookie d2f1759818 | TRAIN J draft | pass |  |
| 36 | TestCallbackPanic | W1 | APPROVED | R claude/r-func-cookie d2f1759818 | TRAIN J draft | pass |  |
| 37 | TestCallbackPanicLocked | W1 | APPROVED | R claude/r-func-cookie d2f1759818 | TRAIN J draft | pass |  |
| 38 | TestCallbackPanicLoop | W1 | APPROVED | R claude/r-func-cookie d2f1759818 | TRAIN J draft | pass |  |
| 39 | TestRegisterClass | W1 | APPROVED | none: ruled DISCLOSE at the runtime bank (15da8805b2 15:03 (4)), no seat or owner named | unassigned | fail | census credits R's func cookie; that seat moves the 6 arg-3 rows only (arg-0 layout half) |
| 40 | TestArrayHash | B1 | P1-SEAT | P1 next manifest commit (rulings bdfbd2244f 02:57, 9c9f02bf75 04:22, 61cf81290c 04:58) | ruled-uncut | fail |  |
| 41 | TestCmpIfaceConcreteAlloc | B1 | P1-SEAT | P1 claude/p1-runtime-disclosures 7c27acb6c5 | TRAIN J draft | disclosed |  |
| 42 | TestConcatTempString | B1 | P1-SEAT | P1 next manifest commit (rulings bdfbd2244f 02:57, 9c9f02bf75 04:22, 61cf81290c 04:58) | ruled-uncut | fail |  |
| 43 | TestIntStringAllocs | B1 | P1-SEAT | P1 next manifest commit (rulings bdfbd2244f 02:57, 9c9f02bf75 04:22, 61cf81290c 04:58) | ruled-uncut | fail |  |
| 44 | TestNonEscapingConvT2E | B1 | P1-SEAT | P1 claude/p1-runtime-disclosures 7c27acb6c5 | TRAIN J draft | disclosed |  |
| 45 | TestNonEscapingConvT2I | B1 | P1-SEAT | P1 claude/p1-runtime-disclosures 7c27acb6c5 | TRAIN J draft | disclosed |  |
| 46 | TestNonEscapingMap | B1 | P1-SEAT | P1 next manifest commit (rulings bdfbd2244f 02:57, 9c9f02bf75 04:22, 61cf81290c 04:58) | ruled-uncut | fail |  |
| 47 | TestRangeStringCast | B1 | P1-SEAT | P1 claude/p1-runtime-disclosures 7c27acb6c5 | TRAIN J draft | disclosed |  |
| 48 | TestStringConcatenationAllocs | B1 | P1-SEAT | P1 claude/p1-runtime-disclosures 7c27acb6c5 | TRAIN J draft | disclosed |  |
| 49 | TestStringIndexHaystack | B1 | P1-SEAT | P1 claude/p1-runtime-disclosures 7c27acb6c5 | TRAIN J draft | disclosed |  |
| 50 | TestStringIndexNeedle | B1 | P1-SEAT | P1 claude/p1-runtime-disclosures 7c27acb6c5 | TRAIN J draft | disclosed |  |
| 51 | TestZeroConvT2x | B1 | P1-SEAT | P1 claude/p1-runtime-disclosures 7c27acb6c5 | TRAIN J draft | disclosed | parent: absorbed when all 13 subtests are disclosed (as rows 89 and 92 are at the union) |
| 52 | TestZeroConvT2x/E16 | B1 | P1-SEAT | P1 claude/p1-runtime-disclosures 7c27acb6c5 | TRAIN J draft | disclosed |  |
| 53 | TestZeroConvT2x/E32 | B1 | P1-SEAT | P1 claude/p1-runtime-disclosures 7c27acb6c5 | TRAIN J draft | disclosed |  |
| 54 | TestZeroConvT2x/E64 | B1 | P1-SEAT | P1 claude/p1-runtime-disclosures 7c27acb6c5 | TRAIN J draft | disclosed |  |
| 55 | TestZeroConvT2x/E8 | B1 | P1-SEAT | P1 claude/p1-runtime-disclosures 7c27acb6c5 | TRAIN J draft | disclosed |  |
| 56 | TestZeroConvT2x/Econstflt | B1 | P1-SEAT | P1 claude/p1-runtime-disclosures 7c27acb6c5 | TRAIN J draft | disclosed |  |
| 57 | TestZeroConvT2x/Eslice | B1 | P1-SEAT | P1 claude/p1-runtime-disclosures 7c27acb6c5 | TRAIN J draft | disclosed |  |
| 58 | TestZeroConvT2x/Estr | B1 | P1-SEAT | P1 claude/p1-runtime-disclosures 7c27acb6c5 | TRAIN J draft | disclosed |  |
| 59 | TestZeroConvT2x/I16 | B1 | P1-SEAT | P1 claude/p1-runtime-disclosures 7c27acb6c5 | TRAIN J draft | disclosed |  |
| 60 | TestZeroConvT2x/I32 | B1 | P1-SEAT | P1 claude/p1-runtime-disclosures 7c27acb6c5 | TRAIN J draft | disclosed |  |
| 61 | TestZeroConvT2x/I64 | B1 | P1-SEAT | P1 claude/p1-runtime-disclosures 7c27acb6c5 | TRAIN J draft | disclosed |  |
| 62 | TestZeroConvT2x/I8 | B1 | P1-SEAT | P1 claude/p1-runtime-disclosures 7c27acb6c5 | TRAIN J draft | disclosed |  |
| 63 | TestZeroConvT2x/Islice | B1 | P1-SEAT | P1 claude/p1-runtime-disclosures 7c27acb6c5 | TRAIN J draft | disclosed |  |
| 64 | TestZeroConvT2x/Istr | B1 | P1-SEAT | P1 claude/p1-runtime-disclosures 7c27acb6c5 | TRAIN J draft | disclosed |  |
| 65 | TestStartLineAsm | B3 | P1-SEAT | P1 claude/p1-runtime-disclosures 7c27acb6c5 | TRAIN J draft | disclosed |  |
| 66 | TestUnsafePoint | B3 | P1-SEAT | P1 claude/p1-runtime-disclosures 7c27acb6c5 | TRAIN J draft | disclosed |  |
| 67 | TestInlineUnwinder | B4 | P1-SEAT | P1 claude/p1-runtime-disclosures 7c27acb6c5 | TRAIN J draft | disclosed |  |
| 68 | TestTracebackArgs | B4 | P1-SEAT | P1 claude/p1-runtime-disclosures 7c27acb6c5 | TRAIN J draft | disclosed |  |
| 69 | TestG0StackOverflow | B6 | P1-SEAT | P1 claude/p1-runtime-disclosures 7c27acb6c5 | TRAIN J draft | disclosed |  |
| 70 | TestGCTestMoveStackOnNextCall | B6 | P1-SEAT | P1 claude/p1-runtime-disclosures 7c27acb6c5 | TRAIN J draft | disclosed |  |
| 71 | TestSystemstackFramePointerAdjust | B6 | P1-SEAT | P1 claude/p1-runtime-disclosures 7c27acb6c5 | TRAIN J draft | disclosed |  |
| 72 | TestTracebackSystemstack | B6 | P1-SEAT | P1 claude/p1-runtime-disclosures 7c27acb6c5 | TRAIN J draft | disclosed | ruled RUNTIME-CAPABILITY on R1, R2 stated |
| 73 | TestArenaCollision | B7 | P1-SEAT | P1 claude/p1-runtime-disclosures 7c27acb6c5 | TRAIN J draft | disclosed |  |
| 74 | TestGCInfo | B7 | P1-SEAT | P1 claude/p1-runtime-disclosures 7c27acb6c5 | TRAIN J draft | disclosed |  |
| 75 | TestGCTestPointerClass | B7 | P1-SEAT | P1 claude/p1-runtime-disclosures 7c27acb6c5 | TRAIN J draft | disclosed |  |
| 76 | TestGroupSizeZero | B7 | P1-SEAT | P1 claude/p1-runtime-fixable bdfbd2244f | cut, not accepted | fail | PASS on bdfbd2244f (linux) |
| 77 | TestLFStack | B7 | P1-SEAT | P1 next manifest commit (rulings bdfbd2244f 02:57, 9c9f02bf75 04:22, 61cf81290c 04:58) | ruled-uncut | fail | ruled STRUCTURAL; pinned on both flavours' texts |
| 78 | TestLFStackStress | B7 | P1-SEAT | P1 next manifest commit (rulings bdfbd2244f 02:57, 9c9f02bf75 04:22, 61cf81290c 04:58) | ruled-uncut | fail | ruled STRUCTURAL; pinned on both flavours' texts |
| 79 | TestSmhasherAvalanche | B7 | P1-SEAT | P1 ifaceHash registration on bdfbd2244f, on top of C2 claude/c2-manual-sig-lift 9c6305277f (TRAIN J draft) | ruled-uncut | fail | efaceHash cut on bdfbd2244f moves the panic to IfaceKey |
| 80 | TestStringW | B7 | P1-SEAT | P1 claude/p1-runtime-fixable bdfbd2244f | cut, not accepted | fail | PASS on bdfbd2244f (linux) |
| 81 | TestTinyAlloc | B7 | P1-SEAT | P1 claude/p1-runtime-disclosures 7c27acb6c5 | TRAIN J draft | disclosed |  |
| 82 | TestTinyAllocIssue37262 | B7 | P1-SEAT | P1 claude/p1-runtime-disclosures 7c27acb6c5 | TRAIN J draft | disclosed |  |
| 83 | TestMemStats | C6 | P1-SEAT | P1 claude/p1-runtime-disclosures 7c27acb6c5 | TRAIN J draft | disclosed |  |
| 84 | TestFunctionAlignmentTraceback | - | DISCLOSED | master manifest | landed | disclosed |  |
| 85 | TestLineNumber | - | DISCLOSED | TRAIN I manifest (run 1 union 9e1594dcd2) | TRAIN I | disclosed |  |
| 86 | TestPinnerConstStringData | - | DISCLOSED | master manifest | landed | disclosed |  |
| 87 | TestReadMetrics | - | DISCLOSED | TRAIN I manifest (run 1 union 9e1594dcd2) | TRAIN I | disclosed |  |
| 88 | TestReadMetricsConsistency | - | DISCLOSED | TRAIN I manifest (run 1 union 9e1594dcd2) | TRAIN I | disclosed |  |
| 89 | TestStartLine | - | DISCLOSED | TRAIN I manifest (run 1 union 9e1594dcd2) | TRAIN I | disclosed | parent, absorbed |
| 90 | TestStartLine/inline | - | DISCLOSED | TRAIN I manifest (run 1 union 9e1594dcd2) | TRAIN I | disclosed |  |
| 91 | TestStartLine/inline-closure | - | DISCLOSED | TRAIN I manifest (run 1 union 9e1594dcd2) | TRAIN I | disclosed |  |
| 92 | TestUserArenaLiveness | - | DISCLOSED | master manifest | landed | disclosed | parent, absorbed |
| 93 | TestUserArenaLiveness/Finalizer | - | DISCLOSED | master manifest | landed | disclosed |  |
| 94 | TestUserArenaLiveness/Free | - | DISCLOSED | master manifest | landed | disclosed |  |

## 4. What the prediction assumes, and where it can miss

- **Base drift, run 1 to run 2.** The census read the run 1 union `9e1594dcd2`; TRAIN J sits on the run 2 union
  `61cf81290c`. Run 2's own additions, the WriteHeapDump fix and R's zero-size view fix among them, may move rows the
  census reads failing. Only TestSchedPauseMetrics and its subtest are predicted here. Any other run 1 to run 2 move is
  unmeasured.
- **Windows verdicts for seats measured on linux.** A8's rows were read here on linux, at TC0 and TC1, on `7eeb468c76`.
  A11, A12, A9 and P1's seat state linux readings in their ledger lines. W1 is the windows reading (`d2f1759818`:
  6/6). TRAIN J's windows battery is the first windows read of the rest.
- **P1's signatures on windows.** `7c27acb6c5` was validated on linux (47 disclosed, 0 errors, 0 orphans). For the 35
  rows it absorbs here:
  - 5 pinned signatures appear verbatim in the census's windows first line (TestUnsafePoint, TestInlineUnwinder,
    TestSystemstackFramePointerAdjust, TestTinyAlloc, TestTinyAllocIssue37262).
  - 25 pin a LATER line of the same test's output (the i9's column is the first line only), per the Go source at
    go1.24.13:
    - the B1 rows, where go2cs's AllocsPerRun note precedes the test's own `want 0 allocs` line;
    - TestMemStats, which Errorf's every field (windows' first is `Mallocs = 0`, the pin is `MSpanInuse = 0`);
    - TestGCInfo, which Errorf's per object (first `bss eface`, pin `heap PtrSlice`);
    - TestGCTestPointerClass, which Errorf's per check (first `class stack`, pin `class heap`);
    - TestGCTestMoveStackOnNextCall, which Logf's the pointers and then Fatalf's `should be a stack pointer`;
    - TestTracebackArgs, where the pinned traceback follows `got`.
  - 4 change TEXT on windows by construction. P1's refusals by name (TestStartLineAsm, TestTracebackSystemstack,
    TestG0StackOverflow, TestArenaCollision) sit in flat files (`export_impl_test.cs`,
    `internal/startlinetest/asmfunc_impl.cs`, `managed_impl.cs`), not per-GOOS, so they replace the windows stub texts
    the census read.
  - 1 is the TestZeroConvT2x parent, absorbed under the parent rule below.
  - All 29 that differ from the windows first line are predicted to absorb, and none has been read on windows.
- **Parents.** A parent row counts as absorbed when every failing subtest is disclosed, as TestStartLine and
  TestUserArenaLiveness are at the union. TestZeroConvT2x is predicted on that rule.
- **The 84th error line** (the host's command line) is outside the 94 and is not addressed here.

## 5. The two never-written `-tests` review siblings (the small ask)

**MEASURED, both EQUAL.**
- **Emission.**
  - Setup: a root seeded from master `2ff42f7a16` (`src/core` plus `version.props`, 4,211 `.cs`), and master's
    converter built fresh from `src/go2cs` at `2ff42f7a16`.
  - Runs, once for `internal/syscall/windows` and once for `internal/syscall/windows/registry`, in sequence into
    the one root:
    `go2cs -tests -test-action convert -platforms windows/amd64 -go2cspath <root>/src <GOROOT>/src/<pkg> <root>/src/core/<pkg>`.
    Both rc 0; 36 and 18 files written.
  - This is the windows TARGET, emitted on a linux host; no windows host was reached.
- **Both siblings are WRITTEN and EQUAL** to the committed file (CR-stripped):
  - `internal/syscall/windows/exec_windows_test.cs.auto`
  - `internal/syscall/windows/registry/registry_test.cs.auto`
- **The other files the run wrote:**
  - 24 more non-Go files are equal too, `registry/windows/value.cs.auto` among them.
  - 2 are untracked `go2cs_test_manifest.json`.
  - 1 differs: `windows/package_init.cs` +7/-0. That is the `-tests` import-init hook (`initᴛᴛtests()` and its partial
    declaration), which a `-tests` run emits and the committed `-stdlib` file does not carry. It is not a review-sibling
    finding.
- **Why the `-stdlib` census could not see them:** only a `-tests` run writes a `*_test.cs.auto`. So the train emission
  check's review-sibling class (ruled 05:04) needs a `-tests` arm, or these two stay invisible to it.
