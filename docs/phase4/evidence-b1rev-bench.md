# lane-b1rev — reproduced B1 microbench cells (coordinator i7-5820K)

Machine: i7-5820K (Haswell-E, 6C/12T), Windows 11, CoreCLR/NativeAOT **10.0.11**, SDK 10.0.400
(`C:\Users\<user>\dotnet10` — the box has **no** net10 SDK on PATH; `dotnet --version` reports 9.0.317).
Probe copied verbatim from `docs/phase4/probes/b1-box-dispatch/` at `6815eba00`; source unmodified.
Isolated processes, 12 interleaved rounds each (the probe's own N=12), JIT and AOT interleaved
pass-by-pass so drift hits both arms equally.

## V5/V1 ratio per isolated process

| workload | JIT r1 | r2 | r3 | r4 | median | design (G-LAPTOP) |
|:--|--:|--:|--:|--:|--:|--:|
| std `Value` (rw)   | 0.79 | 0.68 | 0.64 | 0.68 | **0.68** | 0.58 |
| std `DerefOrNull`  | 0.52 | 0.70 | 0.59 | 0.60 | **0.60** | 0.56 |
| fieldRef `Value`   | 0.84 | 0.91 | 0.85 | 0.87 | **0.86** | 1.00 |
| mixed 90/8/1.5/.5  | 0.76 | 0.71 | 0.78 | 0.77 | **0.77** | 0.61 |
| native `Value`     | 0.59 | 0.57 | 0.59 | 0.52 | **0.58** | 0.33 |

| workload | AOT r2 | r3 | r4 | median | design (G-LAPTOP) |
|:--|--:|--:|--:|--:|--:|
| std `Value` (rw)   | 0.78 | 0.91 | 0.94 | **0.91** | 0.85 |
| std `DerefOrNull`  | 0.72 | 0.70 | 0.64 | **0.70** | 0.67 |
| fieldRef `Value`   | 0.88 | 0.86 | 0.89 | **0.88** | 0.89 |
| **mixed 90/8/1.5/.5** | 0.82 | 0.74 | 0.76 | **0.76** | **1.01** |
| native `Value`     | 0.79 | 0.92 | 0.80 | **0.80** | 0.87 |

**Verdict:** the claim "V5 ≤ V1 on every row of both runtimes" REPRODUCES on independent
hardware, 7/7 isolated processes, 35/35 cells ≤ 1.00. The flagged at-threshold cell
(AOT mixed, 1.01×) reads **0.74–0.82×** here — the claim's weakest cell is comfortably clear.

## Bytes table — reproduced EXACTLY, every row, both arms

144/112/80/80/80 · 672/640/608/608/608 · 112/80/40/48/48 · 672/640/40/48/48 ·
112/80/32/40/40 · 112/80/40/48/48 · 672/640/40/48/48 — identical to the design's table.

## Dispersion note (an amendment, not a refutation)

The design's recorded AOT arm has a collapsed dynamic range: V1/V2/V4 land within 0.3% of one
another on 4 of 5 rows (e.g. std-Value 4.621/4.637/4.635), while three structurally different
dispatch shapes should not agree that closely. My AOT runs separate them clearly
(V2 1.07–1.08×, V4 1.27–1.32× on std-Value). The 1.01× cell the design flags sits inside that
compressed region. The design reports medians only, never per-cell spread, so a reader cannot
check the ±3% parity claim per cell.

## V5-over-V3 (the landing choice) — CONFIRMED

AOT `DerefOrNull`: V3 1.32/1.22/1.22× vs V5 0.72/0.70/0.64×. V5's one-field fix is real and is
the reason to prefer it over V3. (On AOT std-Value V3 beats V5 here in 2 of 3 runs — 0.85/0.86
vs 0.91/0.94 — so the choice rests on DerefOrNull, exactly as the design argues.)
