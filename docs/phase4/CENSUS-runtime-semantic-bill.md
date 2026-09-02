# CENSUS — runtime's first semantic bill

**2026-09-02, i9.** First run of `src/tools/comparison-classifier` against a real corpus package
beyond its own fixtures and the `unicode/utf8` sanity check. Subject: `runtime`, on
`claude/i9-runtime-regen` @ `4df231e5a` (pre-merge; the branch COORD accepted and staged as train 8 —
this bill is stated as pre-merge per COORD's own instruction, since the ordering point — a runtime
whose init hooks are where the converter puts them — is what the regen exists to provide, and it
was available on this branch tip before train 8 lands).

This is the FIRST time `-test-action compare` has completed for `runtime` far enough to produce a
populated `go2cs_test_comparison.json` — every earlier attempt this session (the STOP-class
recheck, the commit-3 measurement, the regen's own control builds) stopped at `build`. The
init-hook and cast-drift regen is what got the host far enough to actually RUN tests instead of
dying at process init, and that is the reason this bill has 849 rows to classify instead of one.

## Bucket one: `TestCaller` / `textAddr` — a real nil-pointer panic, not an infrastructure gap

Per dispatch, this leads ahead of the other findings.

```
panic: runtime error: invalid memory address or nil pointer dereference

runtime.(*moduledata).textAddr()
	runtime/symtab.go:663
runtime.(*moduledata).textAddr()
	.../go2cs-gen/go2cs.RecvGenerator/go.runtime_package.textAddr.global__go.runtime_package.moduledata.g.cs:27
runtime.ΔfuncInfo.entry()
	runtime/symtab.go:851
runtime.(*Func).Entry()
	runtime/symtab.go:778
runtime_test.testCallerBar()   runtime/symtab_test.go:43
runtime_test.testCallerFoo()   runtime/symtab_test.go:35
runtime_test.TestCaller.func1() runtime/symtab_test.go:20
```

`Go="pass" C#="infrastructure-error"`, classified `go-panic-text` (Output contains `panic:`).
**Spot-checked against `go2cs_test_results.json` directly**: a `run` event, one `fail` event
carrying the real panic text and stack above, then three duplicate `infrastructure-error` events
from the same crash (the goroutine-panic reporter re-firing — see the file-lock note below). The
classifier's `classifyOneMismatch` takes the first `fail`/`panic` action it finds, which is the
real one; the duplicates don't change the bucket.

**The mechanism, read from the trace, not guessed:** `moduledata.textAddr()` dereferences a field
that is null at the point `Func.Entry()` calls it through the generated `RecvGenerator` shell
(`go.runtime_package.textAddr.global__go.runtime_package.moduledata.g.cs:27`). `TestCaller` builds
a `runtime.Frames` iterator via `runtime.Callers` inside a goroutine (`testCallerFoo` →
`testCallerBar` → the closure at `symtab_test.go:20`) and walks it with `Func.Entry()`. Something
in that path constructs or copies a `moduledata` whose text-section base pointer is never
populated for a goroutine-local call — plausible given this repo's own documented open class of
"managed-referent" pointer/descriptor gaps in the syscall/reflection boundary, but this bill is a
classification, not a root-cause; naming the exact uninitialized field is the next lane's work if
this one is picked up.

**A second, separately-panicking test, same shape:** `TestArenaCollision` (`go-panic-text`,
`KeepNArenaHints` → nil pointer, `export_test.cs:620`) — different site, same mechanism class
(managed-referent field null where Go's raw pointer arithmetic would just compute an address). Two
independent real panics is why runtime is a `go-panic-text` bucket of 2, not 1.

## The whole-host crash's shadow: 833 `empty-unreached`, 1 `empty-in-progress-killed`

`TestCaller` runs in the middle of the alphabetically-sorted test order and its panic — thrown on
a **goroutine**, not the main test thread — brought the whole host process down (per the run log:
`go2cs test host: could not record the goroutine panic: ... file is being used by another process`,
repeated as multiple report attempts raced each other, then the process exited). Everything
scheduled after that point never got a turn:

- **833 `empty-unreached`** — no C# event at all (`TestVersion` spot-checked: 0 events in
  `go2cs_test_results.json`).
- **1 `empty-in-progress-killed`** — `TestBigGOMAXPROCS`, which HAD started (`run` event,
  spot-checked) but never reached a terminal before the host died with it.

This is the doc comment's documented phantom-finding risk realized exactly as described: these are
NOT 834 independent divergences, they are ONE root cause (`TestCaller`'s panic) with 834 shadows.
The tool does not currently collapse this automatically (the parallel-set-equality collapse is
explicitly unimplemented — see the tool's own doc comment) — flagging it here by hand instead.

## Everything else

```
assertion-mismatch      4
empty-in-progress-killed 1
empty-unreached        833
go-panic-text            2
unclassified             9
-------------------------
total non-matching rows 849
```

**`assertion-mismatch` (4)**, all real Go=pass/C#=fail divergences with captured Output, spot-checked
one (`TestArrayHash`, allocation-count mismatch — go2cs's own runtime allocation counter vs Go's
`MemStats.Mallocs`, a structural-mirror comparison rather than a semantic bug; the other three —
`TestBigItems`, `TestCPUMetricsSleep`, `TestCPUStats` — read the same way from their first-line
text (missing map key, zero CPU/idle time) but were not individually re-verified against raw JSON
this pass.

**`unclassified` (9)**: 8 are `Go="pass" C#="infrastructure-error"` with no captured Output
(`TestAddrRangesAdd`, `TestBlockingCallback`, `TestCallback`, `TestCallbackGC`,
`TestCallbackInAnotherThread`, `TestCallbackPanic`, `TestCallbackPanicLocked`,
`TestCallbackPanicLoop`) — every one of these is a cgo-callback test; runtime's `-tests` conversion
runs `CGO_ENABLED=0` per the corpus's own pinned convention (see CLAUDE.md), so these are almost
certainly a SEPARATE, already-understood divergence class (cgo-conditional tests reached despite
the pin) rather than 8 new findings — not re-classified as such here because the tool has no cgo
awareness yet, stated as a real gap rather than silently folded into `unclassified`'s catch-all.

**A classifier gap found by this run, not by inspection:** the 9th `unclassified` entry is the
`hostCrashAtInit` signature itself (`converted tests: ... failed: exit status N`) — but it landed
in `unclassified`, not the `PACKAGE-LEVEL: host-crash-at-init` short-circuit, because that
short-circuit only fires when `results.json` fails to load AT ALL (`resultsErr != nil`). Here
`results.json` loaded fine — 849 real per-test rows came from it — but the OVERALL host process
*also* exited non-zero (from `TestCaller`'s goroutine panic taking down the process), and the
`-tests` pipeline appended that whole-process failure line to `comparison.errors[]` alongside the
849 per-test lines rather than instead of them. `hostCrashAtInit` is checked in the wrong place for
this shape: it needs a check inside the per-line classification loop too (or an early separate
scan of `errors[]` for its signature before the `errorLinePattern` match), not just the
`resultsErr != nil` branch. Filed as a finding, not fixed here — outside this bill's scope.

## Freshness

The tool printed its staleness warning (`results.json is OLDER than comparison.json`) — both
timestamps read identically to the second (`2026-09-02T00:59:22-05:00`), so this reads as same-second
write ordering rather than a genuinely stale record from an earlier run; the 849-row count and the
specific test names (real runtime test names, sensibly distributed) are consistent with a single
fresh run, not a leftover. Noted rather than silently trusted, per the tool's own doctrine.

## Gates

Report-only — no code changed to produce this bill. The classifier itself (`bc092c9f3`) already
carries its own gates (build/vet/test). The `-tests` run that produced the input data used
`claude/i9-runtime-regen`'s own tip directly (no scratch-seed overlay), avoiding the seed-gap class
of false reading from the earlier 1,925-error measurement.
