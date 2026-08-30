# Two-Row Measurement — `os/user` and `testing`

Measurement lane, i7-5820K coordinator. **No fixes, no commits, no roster edits.** Tree restored
clean at the end (`git status` empty, build output purged).

| | |
|:--|:--|
| Host | i7-5820K (6C/12T, 32 GB), Windows 11 Enterprise 10.0.26100 |
| Tree | isolated worktree, detached at `648cd743c` (= `origin/master` tip at fetch) |
| Go | `go1.23.12 windows/amd64`, `GOROOT=C:\Users\ritchie\sdk\go1.23.12` |
| .NET | SDK `10.0.400`, `DOTNET_ROOT=C:\Users\ritchie\dotnet10` |
| Converter | built this session from the worktree; `go version go2cs.exe` → `go1.23.12` |
| Roster at measurement | **200 / 215 testable packages — 93.0%** |

---

# JOB 1 — `os/user`: the E2 premise has fallen

## 1.1 Headline

**The oracle is clean. `TestGroupIds` passes.** The roster row's own stated re-entry condition —
*"Rejoins the moment the oracle passes"* — is **met**.

The row is nevertheless still unbankable, but for a completely different and far more tractable
reason: **one named runtime defect, at one call site, in an already-documented open class.** It is
no longer a host-hostile-oracle row; it is an ordinary blocked row with a sized fix.

## 1.2 The oracle, re-measured

```
$ go test -count=1 -v os/user          # 1.30s wall, exit 0
=== RUN   TestCurrent
--- PASS: TestCurrent (0.01s)
=== RUN   TestLookup
--- PASS: TestLookup (0.00s)
=== RUN   TestLookupId
--- PASS: TestLookupId (0.00s)
=== RUN   TestLookupGroup
    user_test.go:141: LookupGroupId("S-1-5-21-1464684589-1846858095-1569664222-1001"): lookupGroupId: should be group account type, not 1
--- PASS: TestLookupGroup (0.00s)
=== RUN   TestGroupIds
--- PASS: TestGroupIds (0.00s)
PASS
ok  	os/user	0.200s
```

**5 / 5 PASS, 0 FAIL, 0 SKIP, exit 0.**

The one logged line inside `TestLookupGroup` is **not** a failure and **not** a skip. It is Go's own
documented early-return branch (`user_test.go` ~line 133–142, comment: *"Maybe the group isn't
defined. That's fine."*) — a `t.Logf` followed by `return`. The test's verdict is `pass`. It does
mean the second half of that test (the `LookupGroup(g1.Name)` round-trip) does not execute on this
host, which is worth knowing when the row eventually banks, but it costs nothing: `go test -json`
reports `pass`, and the differential compares verdicts.

**Contrast with the recorded premise.** The roster row (`docs/ValidatedTestPackages.md:439`) reads:

> `| os/user | — | E2 | Go's own go test fails TestGroupIds on the validation host — the reference
> side never produces a clean baseline… Rejoins the moment the oracle passes. |`

and the board (`BOARD-next-validation-candidates.md:4721`, `:10597`, `:19287`) repeats it. On this
box, at Go 1.23.12, today: `TestGroupIds` **passes**. Whether the original recording was a different
host, a different Go release, or a since-fixed toolchain issue, I cannot say from here — but the
premise as written does not hold on the current coordinator at the current pin.

**Skippable-shaped vs hard-fail** (the question that would have decided E2-forever vs
host-conditional): **moot** — there is nothing failing to classify. For completeness, all five tests
carry Go's own skip guards (`checkUser` → `userImplemented`, `checkGroup` → `groupImplemented`,
`checkGroupList` → `groupListImplemented`, plus the `hasCgo || (hasUSER && hasHOME)` fallbacks); none
of them fired here, which is what a clean Windows oracle looks like.

## 1.3 The pipeline run

```
go2cs.exe -tests -test-action all -test-timeout 15m -go2cspath <wt>\src \
          C:\Users\ritchie\sdk\go1.23.12\src\os\user  <wt>\src\core\os\user
```

Elapsed **126.9 s**, exit **1**. `go2cs_test_comparison.json`:

```json
{ "package": "user",
  "status": "conversion-blocked",
  "go":      { "TestCurrent":"pass", "TestGroupIds":"pass", "TestLookup":"pass",
               "TestLookupGroup":"pass", "TestLookupId":"pass" },
  "csharp":  {},
  "matched": false,
  "skipped": [], "disclosed": [],
  "excluded": ["BenchmarkCurrent (benchmark): benchmark execution is deferred to Phase 4D"] }
```

### Verdict arithmetic

| Bucket | Count | Detail |
|:--|--:|:--|
| Go-side verdicts (oracle) | **5** | all `pass` |
| C#-side verdicts | **0** | host process died before reporting any |
| Matching | **0** | |
| Disclosed | 0 | |
| Skipped | 0 | |
| Excluded (deferred) | 1 | `BenchmarkCurrent` — standing Phase-4D benchmark deferral |
| Orphans | 0 | |
| **Naive row value if unblocked** | **5** | |

### This is NOT the ambiguous mass-empty shape

CLAUDE.md's rule — *"a contiguous alphabetical tail is a run that died partway; scattered empties are
genuine divergence; ALL empty is the documented file-lock case"* — would nominate the file-lock
diagnosis here. **It is not that.** The crash text is in the log, and the process exit code is
`0xc0000005`, not a build failure:

```
converted tests: …\os.user.tests.exe --json -timeout 15m0s … failed: exit status 0xc0000005

{"package":"os/user","test":"","action":"run",…}
{"package":"os/user","test":"TestCurrent","action":"run",…,"source":"user_test.go","line":25}
Fatal error.
System.AccessViolationException: Attempted to read or write protected memory. This is often an
indication that other memory is corrupt.
   at go.ж`1[[go.syscall_package+SID, syscall, Version=1.23.12.2, …]].op_Implicit(go.ж`1<SID>)
   at go.syscall_package.ConvertSidToStringSid(go.ж`1<SID>, go.ж`1<go.ж`1<UInt16>>)
   at go.syscall_package.String(go.ж`1<SID>)
   at go.os.user_package.current()
   at go.os.user_package+<>c.<Current>b__4_0()
   at go.sync_package.doSlow(go.ж`1<Once>, System.Action)
   at go.sync_package.Do(go.ж`1<Once>, System.Action)
   at go.os.user_package.Current()
   at go.os.user_internal_test_package.TestCurrent(go.ж`1<T>)
   at go.testing_runtime.TestExecution.Execute(System.Action`1<go.ж`1<T>>)
```

An `AccessViolationException` is not catchable by the host's per-test guard — it is a *Fatal error*
that takes the whole process down, which is why zero verdicts are reported rather than one failure
and four passes.

### Per-test probe: ONE defect, ONE site, all five verdicts

I re-ran the published host directly, one test at a time, to separate "the process died on the first
test" from "each test independently hits this":

```
os.user.tests.exe -run "^TestGroupIds$"     → exit -1073741819 (0xC0000005), identical stack
os.user.tests.exe -run "^TestLookup$"       → exit -1073741819, identical stack
os.user.tests.exe -run "^TestLookupGroup$"  → exit -1073741819, identical stack
os.user.tests.exe -run "^TestLookupId$"     → exit -1073741819, identical stack
```

Byte-identical fault frames in all four. Confirmed against Go's source: **all five tests call
`user.Current()` as their first real action** (`user_test.go` lines 30, 74, 96, 122, 167). So the row
is gated on exactly one defect at exactly one site — and clearing it puts all 5 verdicts in play at
once.

## 1.4 Root characterization

This is the **third fork** of the Windows-syscall class that CLAUDE.md already names — *"the kernel
memory is a byte buffer the CALLER reinterprets, so no wrapper is at fault and no
mirror-the-wrapper remedy applies."* Same family as `net.adapterAddresses` (closed 2026-08-17 by
transcription hand-own) and `crypto/x509`'s CertContext chain walk (still open). `os/user` is the
**third measured consumer**.

The chain, with file evidence:

1. **Kernel fills a managed buffer.** `src/core/syscall/windows/security_windows.cs:320`
   ```csharp
   internal static (@unsafe.Pointer, error) getInfo(this Token t, uint32 @class, nint initSize) {
       var b = new slice<byte>((nint)(n));
       var e = GetTokenInformation(t, @class, Ꮡ(b, 0), (uint32)len(b), Ꮡn);
   ```
   The buffer is a managed `slice<byte>`; the kernel writes raw `TOKEN_USER` bytes into it. This part
   is fine — a byte buffer is blittable.

2. **The caller reinterprets those bytes as a managed struct.** `security_windows.cs:339`
   ```csharp
   public static (ж<Tokenuser>, error) GetTokenUser(this Token t) {
       var (i, e) = t.getInfo(TokenUser, 50);
       …
       return ((ж<Tokenuser>)(uintptr)(i), default!);
   ```
   `Tokenuser` is **not blittable** — `security_windows.cs:280-287`:
   ```csharp
   [GoType] partial struct SIDAndAttributes { public ж<SID> Sid; public uint32 Attributes; }
   [GoType] partial struct Tokenuser        { public SIDAndAttributes User; }
   ```
   `ж<SID>` is a managed **object reference**, not an address.

3. **`NativeBox<T>` has no blittability guard.** `src/core/golib/ж.NativeBox.cs:66`
   ```csharp
   public override unsafe ref T Value => ref Unsafe.AsRef<T>((void*)m_nativeAddr);
   ```
   So reading `u.User.Sid` fabricates an object reference out of the eight raw kernel-written bytes.

4. **The fault surfaces two frames later**, at the first use of that fabricated reference — inside
   `ж<T>`'s address operator, `src/core/golib/ж.cs:624`:
   ```csharp
   public static unsafe implicit operator uintptr(ж<T> value) {
       if (value is not null && value.NativeAddress != 0)   // ← AV here
   ```

**The wrapper is not at fault.** `ConvertSidToStringSid` is *already* hand-owned and correct — it
lives in `src/core/syscall/windows/zsyscall_windows_ptrout_impl.cs:132` and properly handles the
`**uint16` OUT-parameter half via the `publishPointerOut` native-cell pattern. That file's own header
even predicted this: *"A wrapper's absence from this file is NOT evidence it is sound."* Here the
inverse holds — the wrapper's **presence** is not evidence its *caller* is sound. The corrupt value
is created upstream, at step 2.

## 1.5 Sizing the remaining blocker

The class is small and closed:

| | |
|:--|:--|
| Members that reinterpret a kernel token buffer | **2** — `Token.GetTokenUser` (`security_windows.cs:339`), `Token.GetTokenPrimaryGroup` (`:350`) |
| Shared feeder | `Token.getInfo` (`:320`) — the only `getInfo` in the package |
| Payload to transcribe | `Tokenuser{User{Sid *SID; Attributes uint32}}` = `[8-byte SID ptr][4-byte attrs]`; `Tokenprimarygroup{PrimaryGroup *SID}` = `[8-byte SID ptr]` |
| Corpus-wide consumers of either | **1 package** — `os/user` (`src/core/os/user/windows/lookup_windows.cs`) |
| Precedent for the remedy shape | `net/windows/interface_windows_impl.cs` (IP_ADAPTER_ADDRESSES chain), `syscall/windows/zsyscall_windows_addrinfo_impl.cs` (ADDRINFOW) |

Favourable specifics: `SID` is Go's `struct{}` — a genuinely opaque handle nothing reads through in
managed code (the ptrout hand-own already states this and relies on it), so a `NativeBox<SID>` over
the raw address is not merely safe but exactly right. The transcription is two fields, not a linked
chain. It unblocks exactly one row, worth 5 verdicts.

## 1.6 What Job 1 changes

- **The E2 classification no longer describes reality.** Its own re-entry condition is satisfied.
- The row's real blocker is a **named defect in a documented open class**, with a bounded remedy and
  a value-level proof available (all five Go tests pass, so the fix is verifiable end-to-end).
- The recorded premise's staleness is now the **fifth** in the weekend's run of re-measured
  deferrals.

---

# JOB 2 — `testing`: the census behind the meta-ruling

## 2.1 What Go's own `testing` suite contains

```
$ go test -count=1 -v testing            # 4.14s wall (cached build), exit 0
59 top-level verdicts:  PASS 59  ·  FAIL 0  ·  SKIP 0
$ go test -count=1 -short -v testing     # 15.6s wall (cold build), exit 0
59 top-level verdicts:  PASS 56  ·  FAIL 0  ·  SKIP 3   (130 `=== RUN` lines incl. subtests)
```

**Go's `testing` oracle is perfectly clean on this host** — 59/59 in full mode. The three `-short`
skips (`TestBenchmark`, `TestRunParallel`, `TestTesting`) all run and pass without `-short`; the
last skips itself with *"skipping building a binary in short mode."* No oracle problem exists here.

### Surface

11 `_test.go` files (~1,945 test lines) against 4,400 production lines:

| File | Package clause | Test | Bench | Fuzz | Example |
|:--|:--|--:|--:|--:|--:|
| `allocs_test.go` | `testing_test` | 1 | | | |
| `benchmark_test.go` | `testing_test` | 7 | | | 3 |
| `export_test.go` | **`testing`** | 0 | | | |
| `flag_test.go` | `testing_test` | 1 | | | |
| `helper_test.go` | `testing_test` | 2 | 1 | | |
| `helperfuncs_test.go` | `testing_test` | 0 | | | |
| `match_test.go` | **`testing`** | 4 | | 1 | |
| `panic_test.go` | `testing_test` | 5 | | | |
| `sub_test.go` | **`testing`** | 15 | | | |
| `testing_test.go` | `testing_test` | 23 + `TestMain` | 2 | | |
| `testing_windows_test.go` | `testing_test` | 0 | 2 | | |

**The package-clause split is the single most consequential fact in this census.** Three files are
`package testing` — compiled *into* the package, testing its unexported state machine. Eight are
`package testing_test` — outside, using only the exported API (but, as §2.3 shows, mostly by
re-executing the test binary as a subprocess).

### What it exercises

- **T lifecycle** — `tRunner`, run/report/`Goexit` unwind, log-after-complete
- **Subtests** — `t.Run` nesting, naming/deduplication, concurrent `Run`, parent/child ordering
- **Parallel** — `t.Parallel` gating, parallel-subtest cleanup ordering, `Setenv`-vs-`Parallel` panic contract
- **Cleanup** — LIFO ordering, cleanup after `Goexit`, cleanup after panic, nested cleanup, `t.Run` inside cleanup
- **Skip/Fail semantics** — `SkipNow`/`FailNow` in every position, chatty-vs-quiet output shape
- **Helper attribution** — `t.Helper()` frame elision, including from parallel subtests
- **Panic reporting** — panic-in-test, panic-in-cleanup, `Goexit`-after-panic, the exact terminal text
- **TempDir / Setenv** — naming, per-test isolation, cleanup, restore
- **Benchmark plumbing** — `b.RunParallel`, `b.ReportMetric`, `BenchmarkResult` formatting, `b.N` ramp from 1
- **Fuzz plumbing** — `FuzzNaming` over the seed corpus
- **Flags** — `-test.*` registration and parsing via a re-executed child
- **Race integration** — 10 tests asserting the race detector's interaction with the harness
- **Matcher internals** — `-run` pattern splitting, `isSpace`, unique-name generation

## 2.2 What the hand-owned host already implements

`src/core/testing` — **10 C# files, 5,098 lines**. It is a **structural replacement**, not a
transcription (`stdLibConverter.go:203-209`): *"Go's implementation is a state machine over the
runtime's goroutine scheduler, and the converted-test host that stands in for it is go2cs machinery."*

| File | Lines | Role |
|:--|--:|:--|
| `TestHost.cs` | 1,103 | entry point, run loop, reporting |
| `TestExecution.cs` | 1,000 | the per-test state machine (the `common` analogue) |
| `PackageAncestry.cs` | 928 | frame/position attribution |
| `testing.cs` | 685 | the public `T`/`TB`/`B`/`F`/`M`/`PB` shim |
| `TestFormat.cs` | 334 | Go-shaped output formatting |
| `TestOptions.cs` | 295 | flag parsing |
| `TestFlagBridge.cs` | 291 | `flag.CommandLine` bridge for `TestMain` |
| `TestRunner.cs` | 249 | scheduling, parallel gating |
| `TestRegistry.cs` | 110 | conversion-time test registry |
| `TestReporter.cs` | 103 | JSON/JUnit emission |

Discovery is at **conversion time** (`TestRegistry.RegisterTest(name, action, source, line)`) — not
by reflection and not through Go's `InternalTest` slices.

### Implemented — real, live semantics

| Surface | Status |
|:--|:--|
| `T` (21 members) | `Error(f)`, `Fatal(f)`, `Log(f)`, `Fail`, `FailNow`, `Failed`, `Skip(f)`, `SkipNow`, `Skipped`, `Helper`, `Name`, `Run`, `Cleanup`, `Parallel`, `TempDir`, `Setenv`, `Deadline` — all backed by `TestExecution` |
| `TB` (18-member interface) | reached via a go2cs-gen `testing_TжTB` adapter over `ж<T>`; no per-suite wiring |
| `M` | `Run` implemented (`TestMain` supported, with the `flag.CommandLine` bridge) |
| `TestExecution` internals | `Start`/`Wait`/`Execute`, `ReleaseParallel`, `RunCleanups`, subtest naming + Go-exact name sanitization/escaping, goroutine-failure and goroutine-panic capture, `TempDir` with Windows-retry deletion, `Setenv` restore |
| `AllocsPerRun` | implemented (with a documented CLR-regime caveat; `alloc-count-semantics` disclosure class) |
| `Short()`, `Verbose()`, `Testing()` | implemented |
| `Benchmark(func)` | implemented in-process (drives `b.N`; `unicode`'s `TestCalibrate` uses it) |

### Stubbed — compile-only no-ops

| Surface | Status |
|:--|:--|
| `B` (except `N`) | `Run⇒true`, `RunParallel{}`, `ReportAllocs{}`, `ResetTimer{}`, `StartTimer{}`, `StopTimer{}`, `SetBytes{}`, `SetParallelism{}`, `ReportMetric{}`, `Failed⇒false`, `Name⇒""`, `TempDir⇒""`, all TB members no-op |
| `PB` | `Next()⇒false` — a `RunParallel` body never iterates |
| `F` (fuzz) | entirely no-op — `Fuzz{}`, `Add{}`, every TB member |
| `CoverMode()` | `⇒ ""` |

Rationale is stated in-source: benchmark declarations are `disclosed-unsupported`, never registered,
never invoked — the members exist only so converted bodies compile.

### Absent entirely from the host's surface

`MainStart`, `InternalTest`, `InternalBenchmark`, `InternalExample`, `InternalFuzzTarget`,
`RunTests`, `RunBenchmarks`, `RunExamples`, `Init`, `RegisterCover`, `CoverBlock`, `Cover`,
`T.Chdir` (Go 1.24). (`testing/internal/testdeps` — a normal *converted* package — mentions
`testing.MainStart` in a doc comment; the host provides no such entry point.)

### Flag surface

| Supported | Absent |
|:--|:--|
| `-json`, `-v`/`-test.v`, `-short`/`-test.short`, `-run`/`-test.run`, `-count`/`-test.count`, `-parallel`/`-test.parallel`, `-shuffle`/`-test.shuffle`, `-timeout`/`-test.timeout`, `--result`, `--junit` | `-bench`, `-benchtime`, `-benchmem`, `-cpu`, `-cover*`, `-fuzz*`, `-race`, `-cpuprofile`/`-memprofile`/`-blockprofile`/`-mutexprofile`/`-trace`, `-failfast`, `-list`, `-outputdir`, `-paniconexit0`, `-testlogfile`, `-fullpath` |

### Exercised-in-production — the evidence of a different kind

Census run today over the **870 committed `*_test.cs` files in 201 package directories** under
`src/core`. Each figure is *how many packages' banked suites reference that semantic*, and every one
of those packages is differentially re-proven against `go test -json` on every sweep:

| Semantic | Packages | | Semantic | Packages |
|:--|--:|:-:|:--|--:|
| `t.Error` / `Errorf` | **188** | | `t.Helper` | **51** |
| `t.Fatal` / `Fatalf` | **164** | | `testing.AllocsPerRun` | **39** |
| `t.Run` (subtests) | **144** | | `t.Parallel` | **30** |
| `t.Skip` / `Skipf` / `SkipNow` | **77** | | `t.TempDir` | **28** |
| `t.Log` / `Logf` | **73** | | `t.Setenv` | **15** |
| `testing.Short()` | **68** | | `TestMain` / `testing.M` | **12** |
| `testing.TB` | **59** | | `t.Cleanup` | **9** |
| `t.Failed()` | **13** | | `t.Deadline()` | **6** |
| | | | `testing.Benchmark(` | **6** |
| | | | `testing.Verbose()` | **5** |

**This is a real and unusual form of validation.** The host is not merely *asserted* correct — 200
banked rows and roughly 22,000 matching verdicts pass *through* it every sweep, and any drift in
`t.Run` naming, parallel gating, cleanup ordering, skip propagation, `Helper` attribution or
`TempDir` isolation would surface as verdict mismatches across dozens of unrelated packages
simultaneously. It is **end-to-end integration evidence at very large scale**. What it is *not* is a
targeted unit proof: it demonstrates the host is correct *enough for what 200 real Go suites ask of
it*, not that it satisfies every corner of Go's specified contract. Both statements are true and
the difference is exactly what the ruling has to price.

## 2.3 What running Go's `testing` suite through our host would mean

All 59 top-level verdicts classified. The four buckets are **disjoint and sum exactly to 59**.

| Bucket | Count | |
|:--|--:|:--|
| **A — host internals** (`package testing`, whitebox) | **20** | cannot compile; circular by construction |
| **B — subprocess re-exec** | **21** | needs a self-re-executable binary + Go-exact terminal text |
| **C — benchmark machinery** | **8** | tests a surface that is a declared no-op |
| **D — public-API, in-process** | **10** | **the honest, non-circular self-tests** |

### Bucket A — host internals (20) — *circular by construction*

`match_test.go` (4 + `FuzzNaming`) and `sub_test.go` (15). These are `package testing`: they build
`common{...}` struct literals directly, call `newTestContext`, `tRunner`, `newMatcher`,
`splitRegexp`, `isSpace`, `fullName`, and read `.chatty` — **none of which exists in the C# host**,
which has `TestExecution` / `TestRunner` / `TestRegistry` / `TestOptions.Filters` instead.

> `TestIsSpace, TestSplitRegexp, TestMatcher, TestNaming, FuzzNaming, TestTestContext, TestTRun,
> TestBRun, TestBenchmarkOutput, TestBenchmarkStartsFrom1, TestBenchmarkReadMemStatsBeforeFirstRun,
> TestRacyOutput, TestLogAfterComplete, TestBenchmark, TestCleanup, TestConcurrentCleanup,
> TestCleanupCalledEvenAfterGoexit, TestRunCleanup, TestCleanupParallelSubtests, TestNestedCleanup`

**The irony worth naming for the ruling:** this bucket contains precisely the tests that *sound*
most valuable — `TestTRun`, `TestCleanup`, `TestConcurrentCleanup`, `TestCleanupParallelSubtests`,
`TestNestedCleanup` are the exact semantics the host implements and relies on daily. But they are
written as **whitebox assertions against Go's state machine**, not blackbox assertions about
behavior. Their subject *is* the replaced representation. That is **E3** by the board's own
definition (`BOARD:19289`) — the same shape ruled for `internal/unsafeheader`, and the same tension
weighed for `internal/concurrent`.

### Bucket B — subprocess re-exec (21) — *the host would be judging its own output text*

These re-run `os.Args[0]` / `os.Executable()` with `-test.run=^X$` and `GO_WANT_HELPER_PROCESS=1`,
then regex the child's terminal output.

| Sub-shape | Count | Tests |
|:--|--:|:--|
| re-exec + output-text match | 10 | `TestFlag`, `TestPanic`, `TestPanicHelper`, `TestMorePanic`, `TestCallRunInCleanupHelper`, `TestGoexitInCleanupAfterPanicHelper`, `TestTBHelper`, `TestTBHelperParallel`, `TestRunningTests`, `TestRunningTestsInCleanup` |
| **+ requires `-race`** | 10 | `TestRaceReports`, `TestRaceName`, `TestRaceSubReports`, `TestRaceInCleanup`, `TestDeepSubtestRace`, `TestRaceDuringParallelFailsAllSubtests`, `TestRaceBeforeParallel`, `TestRaceBeforeTests`, `TestBenchmarkRace`, `TestBenchmarkSubRace` |
| **+ requires the Go toolchain** | 1 | `TestTesting` (`testenv.GoToolPath`, `go run` a generated program) |

Three observations:

1. **The race half is already settled precedent.** `runtime/race` is ruled **E1** for exactly this —
   *"only testable under the `-race` instrumented build… the converted corpus has no such build at
   all."* The 10 race tests inherit that ruling directly.
2. **The non-race half is a spectrum, not a wall.** The host *does* support `-test.run` and
   `-test.v`, and it *does* emit Go-shaped output (`TestFormat.cs`). Some of these are genuinely
   reachable with work.
3. **But a ruled position already contradicts several of them.** The standing **host-identity
   disclosure** (minted 2026-08-26 with `log/slog`'s `TestRecordSource`) says *the host never claims
   `testing/testing.go`* — it must not impersonate Go's testing-package identity in a frame that is
   actually the hand-owned host. Several bucket-B tests assert precisely that content in panic
   traces. Satisfying them would require reversing a doctrine that was ruled deliberately.

### Bucket C — benchmark machinery (8) — *tests a declared no-op*

> `TestPrettyPrint, TestResultString, TestRunParallel, TestRunParallelFail, TestRunParallelFatal,
> TestRunParallelSkipNow, TestReportMetric, TestTempDirInBenchmark`

`B` is a compile-only no-op and `PB.Next()⇒false`; benchmark execution is deferred to Phase 4D by
standing policy, and the roster header states *"`Example`/`Benchmark` execution is deferred and
never factors into a row."* All 8 fail against the current host, structurally rather than by defect.
This bucket is **gated on Phase 4D**, not on any ruling about `testing`.

### Bucket D — public-API, in-process (10) — *the honest self-tests*

> `TestAllocsPerRun, TestTempDir, TestTempDirInCleanup, TestSetenv,
> TestSetenvWithParallelAfterSetenv, TestSetenvWithParallelBeforeSetenv,
> TestSetenvWithParallelParentBeforeSetenv, TestSetenvWithParallelGrandParentBeforeSetenv,
> TestConcurrentRun, TestParentRun`

These use only the exported surface, run entirely in-process, and assert **externally observable
behavior**: `TempDir` naming/isolation/cleanup, `Setenv`'s restore contract and its
panic-if-parallel rule, concurrent `t.Run`, parent/child `Run` ordering, `AllocsPerRun`'s counting.
Every one targets machinery `TestExecution` actually implements.

### The circularity question, stated precisely

A differential comparison needs an **oracle** and a **subject** that are independent. When the
subject is the test host, the host runs the tests that judge it. That is **not automatically
fatal** — and the distinction is clean:

- **Bucket D is non-circular in the way that matters.** The assertions are about the observable
  behavior of an API. If `TempDir` returned a colliding path, `TestTempDir` would fail exactly as it
  should; the host has no way to make a wrong answer look right, because the check is a value
  comparison, not a self-report.
- **Bucket A is circular by construction.** The assertion is about the host's own internal
  bookkeeping. There is no bookkeeping to assert against, and inventing one would be fabrication.
- **Bucket B is circular in the reporting channel.** The host is simultaneously the thing being
  measured *and* the instrument producing the measurement (its own terminal output, its own process
  identity, its own flag registration). A host bug that corrupts output formatting corrupts the
  evidence about output formatting.
- **Bucket C is vacuous** while B/F are no-ops.

### A hard structural blocker in front of *any* option that runs code

**`-tests` on `testing` is not guarded.** `isNonConvertedStdLibPackage`
(`stdLibConverter.go:215`) gates the **`-stdlib` conversion queue** only; `testConversion.go`
carries no equivalent check. So a `-tests` run pointed at `testing` would attempt to convert Go's
`testing.go` production sources into `src/core/testing`. The hand-owned host files carry **no
`[module: GoManualConversion]` marker at all** (verified — zero matches across all 10 `.cs` files);
they don't need one under the queue skip, so **nothing protects them at the file level**. The result
would be the F15b *"ONE testing package, period"* collision the skip-list comment itself names:

> *"Converting it would write a second `[GoPackage("testing")] testing_package` into the very tree
> the host lives in — the F15b 'ONE testing package, period' collision (CS0433 on every testing type,
> reached via internal/testenv)."*

I did **not** run it, for exactly this reason. **Any option below that executes anything needs a
route around this first** — at minimum a `-tests` guard, plus a decision about where the converted
test sources would live and what package they would compile into. That cost is common to Options 1
and 3 and is not small.

## 2.4 Options for the ruling

*(No recommendation — the owner rules. Each option states its cost and, precisely, what it would
honestly let the project claim.)*

### Option 0 (the null / baseline) — rule the whole package excluded

**Do:** add `testing` to the exclusion ledger as E3 (*the test's subject IS the replaced
representation*), citing the whole-package structural replacement.
**Cost:** ~an hour of docs. No engineering.
**Claims honestly:** *"`testing` is a hand-owned structural replacement; Go's own suite tests the
implementation we replaced, so it is excluded — with the note that the host is validated in
production by 200 rows / ~22,000 verdicts passing through it daily."*
**Weakness:** it over-claims the exclusion. Bucket D (10 verdicts) is genuinely non-circular, and
E3's own wording (*"any pass would be fabrication"*) is **false** for those ten. Ruling them out
would be the first exclusion in the ledger that excludes something implementable — which the
ledger's anti-laundering clause exists to prevent.

### Option 1 — validate the meaningful subset; E-class the rest, by bucket

**Do:** build/run bucket **D** (10 verdicts) through the pipeline; rule A/B/C excluded with a
per-bucket mechanism.
**Cost (honest):**
- The `-tests` guard + a landing place for converted `testing` test sources that does not collide
  with the host (the F15b problem above) — **the real cost, and it is a converter/layout change, not
  a test change**.
- A mechanism to admit only bucket D — the pipeline compares *every* eligible `Test`, so partial
  admission needs either an eligibility filter or a new per-test exclusion class alongside the
  existing disclosure classes.
- Bucket A won't compile, so it must be excluded *before* the build, not disclosed after.
- Estimated: days, not hours — and it touches the converter, which no other roster row's admission
  has required.
**Claims honestly:** *"Ten of Go's 59 `testing` verdicts — the public-API, in-process ones — are
differentially validated against `go test`. The other 49 are excluded: 20 because they are whitebox
tests of a state machine we structurally replaced (E3), 21 because they re-execute the test binary
and assert its terminal text and process identity (of which 10 need `-race`, already E1 via
`runtime/race`), and 8 because benchmark execution is deferred (Phase 4D)."*
**Strength:** it is precise and every number is defensible.
**Weakness:** a 10/59 row is a strange-looking roster entry, and the roster's founding rule is
*"a row appears only when every `Test` function's result matches"* — this would be the first row
admitted on a **ruled subset** of its own suite. That is a real precedent to set deliberately or
not at all.

### Option 2 — behavioral-equivalence harness; no roster row

**Do:** don't run Go's suite. Instead write a **paired Go/C# conformance suite** for the host's
contract — a behavioral-test-shaped project (or a family of them) whose Go side runs under `go test`
and whose C# side runs under the host, comparing observable behavior: subtest naming and
deduplication, cleanup LIFO ordering across `Goexit`/panic/parallel, `Skip`/`Fail` propagation,
`Helper` frame elision, `TempDir` isolation, `Setenv` restore + parallel panic, `Parallel` gating,
`Deadline`. Bank it as guards, not as a roster row. Optionally seed it from bucket A's *intent*,
rewritten blackbox.
**Cost:** the largest *authoring* cost of the three (a real suite to design and write), but the
**smallest structural risk** — no converter change, no F15b collision, no precedent about partial
rows, and it fits the existing behavioral-test machinery exactly.
**Claims honestly:** *"`testing` is excluded from the roster as a structural replacement (E3). The
host's Go-equivalent behavior is separately guarded by a purpose-built conformance suite covering
N named semantics, in addition to the 200 banked rows that exercise it in production."*
**Strength:** it is the only option that produces *new, targeted* evidence rather than
re-interpreting existing evidence; it can cover semantics Go's own suite tests whitebox (all of
bucket A's intent) without any circularity; and it is durable — it guards the host against future
regression, which neither of the other options does.
**Weakness:** it is *our* suite, not Go's. It proves conformance to a contract we wrote down, and a
semantic we failed to think of is a semantic it does not cover. It also yields no roster movement,
so the campaign's headline number does not change.

### Option 3 — full self-hosting

**Do:** implement whatever is needed to run all 59 — `MainStart`/`InternalTest` plumbing, a
self-re-executable host binary with Go's full `-test.*` flag surface, byte-exact terminal output,
real benchmark execution (Phase 4D), fuzz plumbing, and a C# analogue of `common`/`testContext`/
`matcher` for the in-package tests.
**Cost:** very large, and parts are **not merely expensive but ruled or impossible**:
- `-race` has no .NET analogue (10 verdicts) — already E1 precedent.
- Bucket A wants the host to *be* Go's state machine, which is the thing the hand-own exists to
  avoid; reproducing it would recreate the goroutine-scheduler dependency `stdLibConverter.go`
  names as the reason for hand-owning.
- Several bucket-B tests require reversing the ruled **host-identity disclosure**.
**Claims honestly:** *at best* — *"the converted `testing` package validates at 49/59 with 10
disclosed"* — and that claim would rest on a host reshaped to pass its own tests, which is close to
the definition of laundering the ledger warns about.
**Included for completeness.** The measurement does not support it: ~10 verdicts are structurally
unreachable, ~20 more require undoing deliberate architecture, and the remedy's direction is
opposite to the F15b ruling.

## 2.5 Summary table for the ruling

| | `os/user` | `testing` |
|:--|:--|:--|
| Go oracle today | **5/5 pass, exit 0** | **59/59 pass, exit 0** |
| Recorded classification | E2 — broken oracle | *unruled* (in the naive 215 denominator) |
| Does the record hold? | **No** — premise falsified | n/a |
| Real blocker | one AV at one site (`ж<SID>` from a reinterpreted kernel buffer) | structural: the suite tests the implementation we replaced |
| Naive verdict value | **5** | **59** (58 `Test` + 1 `Fuzz`) |
| Honestly reachable | **5**, on a bounded 2-member transcription hand-own | **10** (bucket D), behind a converter/layout change |
| Class | third fork of the documented Windows-syscall class; 3rd measured consumer | E3-shaped for 20, E1-precedent for 10, Phase-4D for 8, implementable for 10 |
| Decision needed from owner | none — it's engineering now | **yes** — which of Options 0–3 |

---

## Appendix — provenance & hygiene

- All measurements taken this session on the stated host/toolchain; nothing carried forward from a
  prior record.
- **Positive control on the pipeline finding:** the AV was reproduced **five** independent times —
  once in the full pipeline run and once per test under `-run` isolation — with byte-identical fault
  frames and exit codes.
- **Tree restored.** The `-tests` run left `src/core/os/user/windows/user.cs` modified — verified a
  pure **CRLF phantom** (`git diff --numstat` empty, `git diff` empty), i.e. class 1 of CLAUDE.md's
  post-sweep taxonomy — and 4 untracked test artifacts plus gitignored staged sources. All restored
  / cleaned. `git status` is **empty**; HEAD unchanged at `648cd743c`.
- **Build output purged**: 172 `bin`/`obj`/`Generated` directories, **576 MB**, removed from this
  worktree only.
- No `dotnet build-server shutdown` was run. No process outside this worktree was touched. Scratch
  files are `tworow-`-prefixed.
- **Nothing was run against `src/core/testing`** — deliberately, per §2.3's F15b analysis.
