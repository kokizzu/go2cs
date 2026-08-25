# REPORT — go2cs performance across the .NET 9 → 10 hop (JOB-018)

> Lane G (perf-canon host), 2026-08-24. Stages N4 (JIT CPU) and N5 (AOT/ILC) of the
> `docs/DotNetMigration.md` §6 ladder, measured against the N2-banked .NET 9 baseline minted on
> this same host earlier the same day. **STATUS: N4 closed, prediction N5 closed; the full
> 14-row AOT ladder (the README bank run) is in flight — its cells and the History-section bank
> land in the PENDING section's place when it completes.**

## Environment

AMD Ryzen 5 PRO 6650U (6C/12T, 32 GB) · Windows 11 Pro 10.0.26200 · go1.23.1 ·
.NET SDK 10.0.400, runtime/ILC **10.0.11** (resolved from the restore and confirmed off the
process image path: `runtime.win-x64.microsoft.dotnet.ilcompiler\10.0.11\tools\ilc.exe`) ·
leg constitution `DOTNET_ROOT=%USERPROFILE%\dotnet10` + `DOTNET_ROLL_FORWARD=Major` + PATH
prepend, proven by apphost probe (`FrameworkDescription` **and** `GetRuntimeDirectory()`, per the
corrected trap-5 rule). Comparison base: N2 tables (SDK 9.0.316), README History section.
Tree: `claude/n3-perf-leg` = Stage-2 apply (net10.0 TFM) + `4d0d300c2` (superseded `_paths.ps1`
literal) + `e4cb0ccf0` (AOT watchdog 4 h + `GO2CS_AOT_PUBLISH_TIMEOUT`) + `d72d40738` (benchmark
csproj leveling).

## N4 — .NET 10 JIT CPU verdict

Two full `--no-aot` runs (~150 s warm each): run 1 overlapped light session git activity, run 2
hands-off quiet-box; the pair brackets every claim (run-to-run spread datum). Control column (Go,
binaries unchanged) read FIRST per §6.1: median drift ~−3 % vs N2, worst −8…−12 % (Channel).
**Rows the control outmoves are VOID, not noise: Sieve, MatMul, Channel; Iface voids by own
spread** (490.7 ↔ 569.4 ms brackets N2's 538.7).

**Headline: the 10-JIT closes ~9 % of the corpus-wide gap to Go with zero converter changes —
geomean of the 13 workload ratios (Startup excluded) 3.13× → 2.86×.**

Signals beyond control, agreed by both runs (ms, N2 → r1/r2):

| row | N2 9-JIT | 10-JIT r1/r2 | Δ | ratio |
|:--|--:|--:|--:|:--|
| String | 1,208.3 | 705.3 / 710.5 | **−41 %** | 11.56× → 6.8× |
| Map | 546.4 | 419.1 / 427.1 | **−22 %** | 0.87× → 0.69× (beats Go, extended) |
| Sort | 461.4 | 393.2 / 405.4 | −13…15 % | 3.42× → 2.8× |
| StringMatch | 992.8 | 852.5 / 859.5 | −14 % | 5.00× → 4.6× |
| StringView | 21.7 | 18.7 / 18.6 | −14 % | 1.12× → 0.98×/1.00× (crosses to parity) |
| IfaceShell | 871.9 | 746.3 / 681.3 | −14…22 % | 40.9× → 30.5–35× |
| Fib | 180.3 | 164.4 / 167.9 | −7…9 % | 1.51× → 1.38–1.42× |
| IfaceCall | 395.8 | 379.1 / 381.3 | −4 % | marginal, consistent |

**One unambiguous regression: Startup** 245.3 → 282.4/277.1 (+13…15 %) against a control that
IMPROVED 9 % — 9.5× → ~12× vs Go. RefLower +3…4 % vs a −3 % control is a weak counter-signal.
Memory (JIT working set): a uniform **≈+2 MB (+3–4 %)** floor shift vs N2, with the Go control
itself drifting +0.4 MB uniformly — small, direction up, carries the instrument-drift caveat. No
allocation claims (count-gated rule; working-set reads only).

## N5 — prediction closed on its second branch

§6.2's falsifiable prediction offered two outcomes; the measurement selected: *"…it lands within
the named control row's envelope of the N2 9-AOT baseline, and the anomaly is attributed to the
preview/bflat packaging and CLOSED as not-a-hop-question."*

**Fib 10-AOT = 174.7 ms vs 9-AOT base 175.3 ms (−0.3 %)**, control row 119.8 vs 119.8 (0.0 %
drift), in-run Go/JIT controls matching the quiet-box N4 values (+0.8…1.6 % / −1.2…3.3 %). The
10 codegen did not move Fib. Independent corroboration: the bflat rows that exhibited the anomaly
were quarantined the same day by the route-#6 finding (the floor script's bflat arm could report
"ok" for benchmarks it never compiled), so the anomalous data and the attribution now agree.
§7 procedure was followed five-for-five (purge logged; ILC version from the restore; Verify
identical stdout across Go/JIT/AOT; Measure solo at suite counts; bank deferred to the full table).

Fib's completed row (10.0.11, default ILC config per coordinator ruling):

| Fib | Go | C# JIT | C# AOT |
|:--|--:|--:|--:|
| time (ms) | 119.8 | 162.4 (1.36×) | 174.7 (1.46×) |
| WS (MB) | 5.7 | 47.9 | **15.5** |

**The unexpected headline is the memory column: 9-AOT's Fib working set was 75.8 MB; 10-AOT's is
15.5 MB — an 80 % collapse, now below the JIT floor and 2.7× Go.** The 9-era "AOT trades memory
for startup" reading is gone on this row. Hypothesis (not asserted): the 10-ILC's added work is
far more aggressive whole-program trimming/DCE — compile time traded for image and working set.
The full ladder decides whether the collapse generalizes; Startup's AOT cell is the most
interesting remaining measurement in the table. Time note: 10-AOT is now marginally slower than
10-JIT on Fib (174.7 vs 162.4); under 9 the order was reversed.

## The 10-ILC compile-cost finding (upstream-shaped)

**One publish of the full converted-stdlib closure: 11,862 s wall (3 h 17 m 42 s) vs 894–953 s
under the 9-ILC on the same host — 12.4–13.3× wall; ≥12,754 CPU-s vs ~1.2 kCPU-s derived for the
9-ILC — ~10.6× CPU work.** Cross-box controls (lane R, i7-5820K-class): BOTH ILC versions settle
at ~1.3 effective cores on this workload — parallelism never existed to lose, so the regression
is serial WORK VOLUME, and the `IlcMaxVcpuCount` line of inquiry is retired by evidence.

Per-sample series (120 s cadence, PID-stable): `evidence-ilc-fib-10.0.11-series.csv` beside this
report. The arc: ~1.3–1.6 effective cores for the first minutes → a multi-hour ~1.0-core serial
tail → a final ~1.6-core burst during which **working set spiked 8.0 → 14.9 GB in the last two
minutes** (emission/linking). Two operational facts: per-publish cost is closure-dominated and
uniform (PerfStartup's censored trajectory was identical at the comparable wall point: ≈7,770 vs
7,701 CPU-s at 6,780 s); and **⚠ a 16 GB machine cannot run a 10-ILC publish of this closure
without swapping** — provisioning constraint for any fleet box that runs AOT legs. Prepared for
a dotnet/runtime report at the owner's discretion; not sent.

## PENDING — lands at ladder completion

- The full 14-row 10-AOT column (in flight: ~46 h of publishes at the measured per-publish cost;
  Fib's completed publish is legitimately reused — same tree, same ILC, same day).
- README `PERF-RESULTS` bank via `--update-readme` (N2 9-table moves to the History section) and
  the `docs/Performance.md` mirror.
- The ladder-wide ILC series (`g-ilc-ladder-series.csv` → banked beside this report): 13–14
  replicates of the CPU-vs-wall arc, per-publish by PID.
- Generalization verdict on the AOT working-set collapse, and Startup's AOT time cell against the
  N4 Startup regression.
