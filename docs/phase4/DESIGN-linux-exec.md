# DESIGN — the Linux exec wall (R2): process launch for the converted corpus

**STATUS: PROPOSED** — design-only, per the 2026-08-22 assignment. No implementation ships with
this document. Written against the three-bodies lane's measured state (`184308a04`: 129/161 Linux
rows PASS, R2 the dominant remaining seam) and the banked Windows `os/exec` precedent (74 matched ·
27 disclosed `host-limit`). Template per `DESIGN-readmemstats-surface.md` / `DESIGN-zh-box-b-prime.md`:
the measured bill first, the design second, the adversarial review third, open questions with
recommendations last.

---

## 1. The measured bill — what the 18 rows actually need

R2 is not "implement fork". It is the specific set of process-launch behaviors the residual rows
exercise, measured across the poll-seam lane's classification (board table, 16 rows), the
three-bodies update (+`os/exec` now running 16/72, +`flag` 23/24 with `TestExitCode`), and this
lane's census logs.

**The three reaching paths** (every R2 test arrives through one of these):

1. **`exec.Command(os.Args[0], …)` self-re-exec** — the helper-process protocol Go's own suites
   use everywhere, and the exact shape the banked Windows row already proves end to end (pipes,
   `Output`/`CombinedOutput`/`Wait`, `ExtraFiles`, env de-dup, exit-status plumbing, `context`
   cancellation). `os/exec`'s own 56 remaining verdicts, `sync` `TestMutexMisuse`, `math/rand`
   `TestDefaultRace` ×7, `crypto/ed25519` `TestEd25519Vectors`, `flag` `TestExitCode`.
2. **`testenv.MustHaveGoBuild` / `GoToolPath`** — `go build -o /dev/null runtime` probes and real
   `go` tool invocations: `debug/buildinfo`, `debug/gosym`, `go/doc/comment` (`TestStd`),
   `text/template` (`TestLinkerGC`), `crypto/ecdh` (`TestLinker`), `crypto` (`TestPureGoTag`),
   `internal/abi`, `internal/testenv`, `internal/types/errors`, `internal/godebugs`.
3. **`go list` export-data enumeration** — `go/types` (TestCheck/TestAtomicAlign via `go list`,
   555/557 verdicts already produced), `go/internal/gcimporter` (`TestImportStdLib` via
   `go list std`), `go/importer`, `go/internal/srcimporter` (4/7, three R2-shaped
   infrastructure-errors after the JOB-005 gcc fix).

**The converted call chain and where it dies** (all paths converge):

```
exec.Cmd.Start → os.StartProcess → syscall.StartProcess → syscall.forkExec
  → forkAndExecInChild(1)            [exec_linux.cs:159/249 — converted, unrunnable]
      → rawVforkSyscall(SYS_CLONE …) [announcing stub]
      → runtime_BeforeFork / AfterFork / AfterForkInChild / BeforeExec / AfterExec  [stubs]
      → child-side body: the fd shuffle, sigmask, setpgid, chroot/chdir, execve    [managed code
        that may never run in a forked child — see §2]
```

Plus the **wait half**: `os.Process.wait` → `ensurePidfd`/`pidfdWorks` probing, then
`blockUntilWaitable` (`waitid` `WNOWAIT`) and `wait4`; `Process.Signal` → `kill(2)`;
`WaitStatus` decode (pure bit logic, converts fine). The keystone libc P/Invoke
(`internal/runtime/syscall` `Syscall6`, ruled and landed) already carries `waitid`/`wait4`/`kill`
— the wait half is wiring, not new native surface.

**What the rows do NOT need** (billed by absence across every R2 test list): `Chroot`,
`CLONE_NEW*` namespaces, cgroup fds, ptrace, `Pdeathsig` (present in `SysProcAttr` but unset by
these suites), credential switching in the default harness posture (but see §5.3 — the sweeps run
as root on the WSL distro, which un-gates `Credential` tests Go skips for non-root; the bill there
is a harness-posture decision, not a spawn feature).

**One honesty defect travels with the wall** (named in the poll-seam board entry): the converted
`sync.OnceValue` re-panics a foreign (non-Go) exception as `panic: nil` — `recover()` sees no Go
panic, `valid` stays false, `panic(p)` rethrows a nil box — so every R2 root behind a
`OnceValue`-guarded probe (`testenv`, `os/user`) reports as `panic: nil` instead of naming the
`NotImplementedException` underneath. §6 OQ-6 prices the fix; it is a one-line-class golib/sync
change and this design's measurement plan depends on the true exceptions being visible.

## 2. The fork in the road — why the child side can never be managed

POSIX (and glibc's implementation reality) permits only async-signal-safe calls between `fork()`
and `execve()` in a multithreaded process. A CLR process is unconditionally multithreaded (GC,
finalizer, tiered-compilation workers), and **no managed instruction is async-signal-safe**: the
first JIT'd call in the child may take a runtime lock some other pre-fork thread held, touch a GC
data structure mid-mutation, or fault into a suspended-in-the-parent handler. This is unsound *by
rule*, not by measurement — no amount of testing makes it correct.

That single fact eliminates three shapes at once:

- **(a) P/Invoke `fork`/`clone`/`vfork` + run the converted child body**: the converted
  `forkAndExecInChild1` is managed code; see above. `rawVforkSyscall` with `CLONE_VFORK|CLONE_VM`
  is *worse* — the child shares the parent's address space while the parent's threads are frozen
  mid-anything.
- **(b) A native helper shim compiled at build** (a `.c` beside golib): sound, but it adds a native
  toolchain dependency to every consumer build — the repository just *removed* its last external
  Windows tool dependency, and the F15 gcc lesson (JOB-005) is precisely that C toolchains are not
  assumed present.
- **(c) `System.Diagnostics.Process`**: the contract misses on four billed points — no arbitrary
  fd table (`ExtraFiles` = child fds 3+ via dup2 map; `Process` offers exactly stdin/stdout/stderr),
  no `Setpgid`/session control, no access to the raw exit `WaitStatus` shape Go decodes
  (`Exited`/`Signaled`/`CoreDump` bits), and its internal reaper owns `waitpid` for its children,
  colliding with `os.Process.wait`'s own protocol.

**(d) `posix_spawn(3)` is the remaining shape, and it is sufficient for the bill.** glibc runs the
entire child side in its own native code (internally `CLONE_VFORK` on a fresh stack), configured
*declaratively* from the parent: `posix_spawn_file_actions_t` (`adddup2`/`addclose`/`addopen` —
the fd shuffle, expressed as a list instead of run as code) and `posix_spawnattr_t`
(`POSIX_SPAWN_SETPGROUP`, `SETSIGMASK`, `SETSIGDEF`, `SETSID` on glibc ≥ 2.26). The parent-side
protocol Go implements by hand (the CLOEXEC status pipe the child writes `errno` into when `execve`
fails) is *subsumed*: glibc's `posix_spawn` reports child-setup and exec failures synchronously in
its return value. Everything the 18 rows bill maps onto this surface; everything that does not map
(§4) gets an explicit, named error instead of a silent wrong answer.

## 3. The design

**One hand-owned file** — `src/core/syscall/linux/exec_linux_impl.cs`, carrying
`[module: go.GoManualConversion]`, per-GOOS under the linux folder exactly like R's
`runtime_netpoll_impl.cs` (NETPOLL-S1 placement: rides the `$(GoTargetOS)/*.cs` glob, no csproj
edit, Windows corpus byte-untouched). The seam is **`forkExec`** — one level above the Windows
precedent's `StartProcess` seam, because on Linux the parent-side pipe protocol AND the child body
are both replaced by the primitive, so cutting lower would leave converted code reimplementing a
protocol `posix_spawn` already owns. Everything above (`syscall.StartProcess`, `os.StartProcess`,
all of `os/exec`) stays converted and untouched; `forkAndExecInChild`/`forkAndExecInChild1` and
their helpers become unreachable-by-construction (the converter's placeholder-comment treatment,
`manualConversionFuncs`, exactly as `runtime`'s process-control surface did it).

Inside the hand-own, in Go's own order:

1. **Argument/env marshaling**: NUL-terminated `byte*` vectors built in UNMANAGED memory for the
   call window, freed in `finally` — the Windows arc's soundness rule verbatim (`ж → uintptr`
   pinning is transient; unmanaged copies remove the GC window).
2. **The fd shuffle, as data**: Go's child-side two-pass dup dance (shift colliding fds up, then
   dup2 into place, then close trailing) is *pure permutation logic* — computed parent-side in
   managed code, emitted as an ordered `file_actions` list (`adddup2` per mapping, `addclose` for
   the tail). `ProcAttr.Files` (stdin/stdout/stderr + `ExtraFiles` at 3+) arrives exactly as the
   converted callers built it. The opaque `posix_spawn_file_actions_t`/`posix_spawnattr_t` are
   allocated as generously-sized native blocks and driven ONLY through their `_init`/`_destroy`/
   `add*` functions — never by layout knowledge.
3. **Attributes from `SysProcAttr`**: `Setpgid`/`Pgid` → `SETPGROUP`; `Setsid` → `SETSID`;
   `Foreground` → `SETPGROUP` + the converted parent-side `tcsetpgrp` call that already follows;
   signal mask/defaults reset to Go's child contract (`SETSIGMASK` empty, `SETSIGDEF` for
   handled signals). Every `SysProcAttr` field OUTSIDE the mapped set, when non-zero, returns
   `ENOTSUP` **naming the field** — an honest wall in Go's own error currency, never a silent
   drop (the fdtest/`GOOS`-guard precedent: reach Go's own gate, fail Go's own way).
4. **`ForkLock` discipline unchanged**: converted callers already hold it around the spawn window
   to coordinate CLOEXEC races; the hand-own runs under it exactly as `forkExec` did. `posix_spawn`
   makes the window shorter, not different.
5. **The wait half is wiring, not surface**: v1 pins the **non-pidfd path** — `ensurePidfd`
   reports unsupported (one honest probe result, not a stub), so `os.Process` takes the
   `waitid(WNOWAIT)`/`wait4` road that the landed keystone already carries. `kill`, `WaitStatus`
   decode, and `ExitError` plumbing are already-converted logic over already-landed P/Invokes.
   pidfd (`P_PIDFD`, `CLONE_PIDFD`) is a v2 door `posix_spawn` cannot open (no pidfd attr) —
   priced in OQ-4, and nothing in the 18-row bill needs it.

## 4. What this design deliberately does not do

No pidfd in v1 (§3.5). No namespaces, cgroups, ptrace, `Chroot`, `Pdeathsig`, ambient capability
edits — all absent from the bill; all `ENOTSUP`-named if ever set. No credential switching
(`SysProcAttr.Credential`) in v1: `posix_spawn` has no portable setuid action, and §5.3 shows the
billed exposure is a harness posture question, not a spawn-feature question. No attempt to make
the 27 Windows `host-limit` disclosures (relocatable single-file test executable) pass on Linux —
the same apphost-beside-assembly constraint holds, the same signature-pinned disclosure class
transfers, and pretending otherwise would un-prove the Windows precedent.

## 5. Adversarial self-review

**5.1 "posix_spawn reports exec failure synchronously — does it, always?"** glibc ≥ 2.26 does
(the internal CLONE_VFORK protocol); musl also returns exec errors synchronously. But the CONTRACT
(`POSIX`) permits an implementation to return 0 and let the child die post-exec-failure. The
design binds glibc behavior on the fleet's Ubuntu distros — measured, and cheap to verify in the
implementation gate (spawn a deliberately-missing binary; assert `ENOENT` from the call, not a
127-exit child). If a future host's libc defers the error, Go's own contract still holds at one
remove (the error surfaces from `Wait` instead of `Start`) — a divergence to DISCLOSE if ever
measured, not a soundness hole. **Held, with the gate.**

**5.2 "The CLR's own SIGCHLD handler will eat your children."** CoreCLR installs SIGCHLD for
`System.Diagnostics.Process`; its reaper is pid-targeted (it `waitpid`s only tracked children),
so untracked `posix_spawn` children are not reaped out from under `wait4`. But this is
runtime-internals knowledge, not contract — OQ-2 makes it a MEASURED gate (spawn, sleep past the
child's exit, then `wait4` and assert the status arrives) rather than an assumption. The known
hazard shape (Mono reaped everything) is exactly why the gate exists. **Held, with the gate.**

**5.3 "The distro runs sweeps as root — credential tests un-gate."** True and billed: Go skips
`Credential` tests for non-root, so the WSL-root posture RUNS them, and v1 returns `ENOTSUP`.
Three dispositions exist: implement setuid via spawn-then-fail (unsound), run the exec-wall rows
under a non-root sweep user (posture change, F15-adjacent), or disclose the credential subtests
against the v1 wall. OQ-3 recommends the posture change — it is one `useradd` + `runuser` line in
the harness, it restores Go's own gating exactly, and it removes a root-only failure class from
every OTHER suite too. **Resolved into OQ-3.**

**5.4 "`go build` children fork their OWN children — does the wall recurse?"** No: path 2/3
children are the REAL Go toolchain (`go list`, `go build`) — native ELF binaries spawning their
own processes with Go's real runtime. The wall exists only at the managed boundary; once the first
exec succeeds, everything below is Go's own machinery. The bill's hardest row (`go/types` via
`go list std`) needs exactly ONE working spawn per invocation.

**5.5 "Why not keep Go's status-pipe protocol and only nativize the clone?"** Because the child
side between clone and exec IS the protocol — the pipe write happens in the child. Any design that
runs child-side logic re-enters §2's impossibility. The protocol must be replaced by a primitive
that owns the child, and that is the definition of `posix_spawn`.

**5.6 "18 rows is the bill today — does the seam generalize?"** The seam is `forkExec`, which is
the ONLY process-creation door in the converted corpus (`os/exec`, `os.StartProcess` direct users,
and everything testenv-shaped all funnel through it). Closing it is closing the class; there is no
second exec path to chase later.

## 6. Open questions, each with a recommendation

- **OQ-1 — seam placement: `forkExec` (recommended) vs `forkAndExecInChild`.** Lower keeps more
  converted code but re-implements the status-pipe protocol around a primitive that already owns
  it; higher (StartProcess) would hand-own pure-Go validation logic that converts fine. `forkExec`
  is the narrowest cut that removes ALL child-side execution. **Recommend: forkExec.**
- **OQ-2 — the CLR reaper interplay.** Measured gate as §5.2: spawn → child exits unobserved →
  delayed `wait4` must return the status. Run it under load (GC pressure) in GolibTests. If it
  ever fails, the fallback is tracking spawns in a golib-side table and routing `wait` through a
  dedicated reaper thread — a design amendment, not a patch. **Recommend: gate first, believe the
  pid-targeted reaper only after it passes.**
- **OQ-3 — harness posture for credential-gated tests.** Non-root sweep user for the R2
  re-measure (restores Go's own skip gating; one harness line, F15 addendum) vs disclosing
  root-only subtests against the `ENOTSUP` wall. **Recommend: non-root user, and note the distro's
  root posture as a standing census caveat either way.**
- **OQ-4 — pidfd: v1 excludes it.** `posix_spawn` cannot mint one; Go's fallback path is complete
  and the bill never touches pidfd-only behavior. v2 could add `pidfd_open(pid)` post-spawn (a
  race Go itself tolerates on old kernels). **Recommend: defer, document the probe result as the
  honest "unsupported" answer, revisit only if a row ever bills it.**
- **OQ-5 — glibc version floor.** `SETSID` needs glibc ≥ 2.26; the fleet's 22.04 ships 2.35. The
  hand-own should probe-and-name (`ENOTSUP` with the attr named) rather than version-sniff.
  **Recommend: capability probing at first use, cached; never parse version strings.**
- **OQ-6 — the `sync.OnceValue` nil-panic mask.** One-line-class golib/sync fix (preserve the
  foreign exception instead of `panic(nil)`), sitting on settled sync surface → its own
  ratification line, and the R2 measurement plan WANTS it first (true exception names in every
  residual log). **Recommend: ratify and land BEFORE the exec implementation lane starts, as its
  first commit.**
- **OQ-7 — where the implementation lane measures.** The 18-row filtered re-sweep on Linux at the
  lane tip + the Windows control (the arc is linux-flavor-only; the i9 sweeps the same rows) +
  the §5.1/OQ-2 gates in GolibTests + one new behavioral test (`LinuxSpawnBasics`: spawn, pipe
  round-trip, ExtraFiles fd, exit-status decode — Windows-green by the existing machinery,
  Linux-green by this arc). **Recommend: exactly that ladder, per the poll-seam lane's shape.**

## 7. Expected yield, priced honestly

R's three-bodies measurement leaves 29 FAIL + 3 COUNT on Linux, 18 rows carrying R2 as the
dominant residual. This design's primitive covers every billed reaching path; the poll-seam
lesson ("28, not ~58 — five walls stood behind the first") applies here too, and the known
walls-behind-the-wall are already named: R5 sockaddr (2 rows), W1b mmap (2), R3 (1), R6 (1 test),
the W4 counts, and whatever the true exception names reveal once OQ-6 lands. The honest expectation
is **most of the 18 move — flips where R2 was the only residual, sub-count improvements where
another named seam stands behind it** — and the number that matters is the one the implementation
lane measures, not this paragraph.

---

*Prepared by lane G (`claude/design-linux-exec`), 2026-08-22, against master `786f9b225` +
R's `184308a04` merge signal. For ratification: the §3 shape, the §6 recommendations (OQ-6
sequenced first), and the OQ-3 posture change.*
