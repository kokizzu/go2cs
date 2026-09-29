# A17 writes seat — PREDICTION (committed before any footprint emission)

Seat: `claude/r-a17-zerosize-writes`, red `2db8892974`, fix `820ffb920c`, on the TRAIN I run-2 union
`61cf81290c` (ruled: early TRAIN J). Host: R-LAPTOP, windows, go1.24.13 pinned, GOTOOLCHAIN=local.

## What the change does

- **converter** — an assignment target that selects a readonly zero-size layout field is lowered to
  `T.ᏑZ(ref <base>) = <rhs>`.
- **golib** — reflect's field accessor answers `GoZeroSizeSlot` for a readonly zero-size field.

## Predictions

1. **Footprint, two-seeded `-stdlib`** (base converter `61cf81290c` vs cut `820ffb920c`, each target
   into its own seed): **0 files on windows, linux and darwin.**
   - Reason: the lowering fires only on a store to a readonly zero-size layout field. At the base every
     such store is CS0191, and the corpus compiles on all three targets, so none exists.
   - Falsifier: any file differs on any target.
2. **CNR** — NO REGRESSION across every behavioral package. ZeroSizeFieldLayout's committed `main.cs`
   already carries the fixed emission; its 6 lines moved in the fix commit.
3. **Behavioral full suite** — PASS on every phase. The golib change cannot alter an emission, and it
   reaches only readonly zero-size fields.
4. **GolibTests full** — the failing set is exactly the three link-staging host rows.
5. **Converter** — unfiltered `go test ./...` ok; `check-symbol-sync` clean (no symbol added).
6. **Reflect-bridge canaries** — five largest banked reflect consumers by verdict count, derived at gate
   time from the roster:
   - Roster: 222 rows, reconciled against an independent count.
   - Predicate: parsed imports, production + test, tags `purego,math_big_pure_go`.
   - Controls: `encoding/json` IN, `cmp` OUT, `go/doc/comment` OUT — all three held.
   - The five: `crypto/cipher` 27272, `crypto/tls` 4759, `net/http` 1387, `go/types` 574,
     `encoding/json` 532.
   - **Each PASSES at its banked count.** A readonly zero-size field exists only in an A17-layout struct
     (unmanaged, no embed), and a reflect write to a zero-size field of one is the only path that moved.
   - Falsifier: any canary FAIL, or any count off its banked figure. A red is then attributed with a
     same-box base arm at `61cf81290c` before anything is claimed.
