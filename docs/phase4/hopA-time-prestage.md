# hop-A pre-stage — the `time` row (the boarded hand-own collision), ANSWERED BY MEASUREMENT

> **State: EXECUTED, holding ONE LIVE OBLIGATION.** The measurement is complete and its two good
> findings are closed — the banked 159 verdicts are safe, and the fixed Stop/Reset semantics already
> hold on the shipping modes. What remains open is the third: **the `asynctimerchan=2`
> `AccessViolationException` is hop A's one named pre-H10 blocker**, carried where the campaign can
> see it ([`../PLAN-hop-campaign.md`](../PLAN-hop-campaign.md) §4.1's pre-H10 obligation) rather than
> only here. The two rules this measurement earned — *a disclosure cannot absorb a crash*, and how to
> read a mass-empty alphabetical tail — are generalized into
> [`../GoCorpusMigration.md`](../GoCorpusMigration.md) §4, and the technique itself into its H10.
> Amended, never rewritten.

R-2 of the runway dispatch (2026-08-24). The board carried `time` as a known hop risk: upstream
1.23.12 fixes timer races in `runtime/time.go`, our `time` rides a hand-owned managed timer, no
regen updates it, and the row's **159 banked verdicts carry no disclosure manifest** to absorb a
failure. The dispatch offered three closure shapes — hand-own edit ready to land, drafted
disclosure entry, or a written no-action. **The measurement says the truth is a fourth shape**,
and it is pinned to a stack frame.

## How it was measured (reproducible)

The 1.23.12 test suite was run against the CURRENT corpus through the real pipeline —
`go2cs -tests -test-action all -test-timeout 10m -goroot <go1.23.12> <go1.23.12>/src/time
<repo>/src/core/time` — with the Go control side on the FIXED runtime (`GOTOOLCHAIN=go1.23.12`;
verified by `go version` output per the H1 amendment, not by any file). The converter initially
**refused** — its skew guard caught `version.props` (1.23.1) against the 1.23.12 toolchain — so the
run mimicked the hop's own H2 state with a worktree-local pin bump, restored afterward. Production
`time` sources are identical between the releases (`handown-census.ps1`: `tick.go` untouched;
recon: the package's change is test-only), so only the TESTS differ from the banked row.

## The three findings

**1. The banked 159 are SAFE.** The 1.23.1 suite contains none of the new assertions; no banked
verdict depends on the changed behavior. The exposure is entirely the NEW tests the hop adds.

**2. The fixed Stop/Reset semantics ALREADY HOLD on the shipping paths.** The new
result-correctness tests pass against the managed timer:

| new test | verdict | elapsed (C#) |
|:--|:--|--:|
| `TestMultiWakeupTicker` | **pass** | 111.5 s |
| `TestMultiWakeupTimer` | **pass** | 0.5 s |
| `TestResetResult/asynctimerchan=0` (the default) | **pass** | 24.9 s |
| `TestResetResult/asynctimerchan=1` | **pass** | 4.2 s |

The managed implementation was already written against the fixed contract — the rehearsal's fear
("the new assertions find the same bug shape upstream fixed") is FALSIFIED for modes 0 and 1.

**3. `asynctimerchan=2` CRASHES the host — and that, not semantics, is the hop blocker.**

```
Fatal error. System.AccessViolationException: Attempted to read or write protected memory.
   at go.unsafe_package+Pointer.op_Implicit(Pointer)
   at go.time_package.NewTimer(Duration)
   at go.time_test_package+...testStopResetResultGODEBUG...
```

The path: `NewTimer` passes `(uintptr)syncTimer(c)` — an `unsafe.Pointer` wrapping the managed
channel box — into `newTimer`. Modes 0 and 2 both take that path (mode 1 returns nil), and the
hand-own's machinery separates them downstream (`time_impl.cs:367` `asyncTimerChan()`), so the
mode-2-only AV means the crash sits in what mode 2 subsequently DOES with that pointer, not in the
conversion NewTimer shares with the passing mode 0. Every empty verdict after it
(`TestStopResult/*`, `TestTimerGC/*`, `TestZeroTimer/*`, …) is the documented
alphabetical-tail-after-crash shape — the host died; those are not divergences.

## The pre-staged answer

- **Not (a) as dispatched**: no `tick.cs` edit is owed — its upstream source did not move, and the
  passing modes prove the fixed semantics are already in the managed timer.
- **Not (b) alone**: a disclosure cannot absorb a CRASH. Disclosures absorb verdict divergence;
  an AV kills the host and takes ~100 later verdicts with it. Even a mode-2-scoped disclosure is
  unreachable until the process survives the test.
- **The real closure: one bounded piece of runtime work before H10** — either fix the mode-2 path
  in the hand-owned timer machinery (locus above, `time_impl.cs`'s async handling of the registered
  channel pointer), or make the managed emulation treat `asynctimerchan=2` as an unsupported debug
  mode that degrades to a non-crashing behavior, and disclose THAT choice (upstream's own comment:
  "the only reason to use asynctimerchan=2 is for debugging a problem fixed by asynctimerchan=1").
  Both are legitimate; the choice is the hop lane's judgment call, per the census doctrine.

**What H10 will see if nothing is done**: the `time` row fails with a mass-empty tail that reads
like total conversion failure. It is one AV on an undocumented debug mode; the tail is fallout.
