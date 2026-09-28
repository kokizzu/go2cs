# W1 — windows page-allocator pre-blockers: PREDICTION (written before any emission)

Seat: `claude/r-w1-windows-mem` on the TRAIN G union `e6fc210500`. COORD ruling 2026-09-28: W1 = (ii) + (iii).

## The change

- **(ii)** `sysReserveOS`, `sysUsedOS`, `sysUnusedOS` join `sysAllocOS`/`sysFreeOS` in `manualConversionFuncs`
  (`goosWindows`) and get Go's bodies over kernel32 in `runtime/windows/mem_windows_impl.cs`: `VirtualAlloc`
  MEM_RESERVE (hint first, then anywhere), MEM_COMMIT and `VirtualFree` MEM_DECOMMIT, with Go's halving retry
  and Go's messages.
- **(iii)** a `[ModuleInitializer]` in `runtime/windows/os_windows_impl.cs` sets
  `physPageSize = Environment.SystemPageSize` (osinit's `getPageSize()`, which never runs here).

## Two-seeded `-stdlib` footprint (base `e6fc210500` converter vs this seat's), three targets

| target | files | what |
|---|---|---|
| windows/amd64 | **1**: `runtime/windows/mem_windows.cs` | the three bodies each become the converter's one-line hand-own placeholder (the shape `sysAllocOS`/`sysFreeOS` already have); hoisted `@string` literals referenced only by those bodies drop out of the file's literal block; the file's `GoPositionMap` lines RE-ENCODE (a removal re-encodes the map; a map line REMOVED rather than re-encoded would falsify this) |
| linux/amd64 | **0** | the rows are `goosWindows` only |
| darwin/amd64 | **0** | same |

Falsifiers: any second windows file; any linux or darwin file; a body left in place.

## CNR

**0 CHANGED.** No behavioral test converts `runtime`.

## The family re-probe (the gate)

Windows page-allocator rows (`^(TestPage|TestScav|TestPalloc)`, TestPageAccounting excluded as on linux) reach
the **A17 AccessViolation** in `scavengeIndex.alloc` exactly as linux does: first row
`TestPageAllocAlloc/AllFree1`, same frames. The A16 golib shim is NOT part of this seat, so the prediction is
stated for the probe tree (this seat + the A16 shim), and separately for this seat alone: without A16 the rows
stop at A16's refusal, as linux does today.

## Rows that move from the physPageSize fix

To be LISTED from a windows `runtime` run at this seat against the base (COORD asked for every moving row).
Predicted direction only: rows that aligned to 0 now align to 4096, so movement is expected in mallocinit- and
page-size-dependent rows; no count is predicted.
