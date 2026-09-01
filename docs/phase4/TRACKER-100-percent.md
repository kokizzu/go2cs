# Phase 4 — The Road to 100% (living tracker)

> **LIVING coordinator-maintained surface.** Updated at every banking window; every number here
> is re-derived from [`docs/ValidatedTestPackages.md`](../ValidatedTestPackages.md) (the
> authority) and the mailbox record at update time — never carried forward. If this file and
> the roster disagree, the roster is right and this file is stale.
> Scope: the **Windows validation roster** (the 100% goal's primary axis). The per-OS parity
> axis is tracked in its own summary line below.

## Where we stand — updated 2026-08-31 evening (the day both bosses moved)

| Measure | Value |
|---|---|
| **On master (`bb2bb7a75`)** | **200 / 208 = 96.2% honest** · 27,729 matching · 154 disclosed · raw 200/215 = 93.0% — the number holds while the bosses close: **reflect 115 → 77 real mismatches in one day** (MakeFunc family 27→2, record cargo's both focal points landed, TestOffsetLock dead), **runtime 9 → 5 build errors** with the five rooted to a named coordinated fix (wave A1) |
| **reflect remaining distance (measured, not guessed)** | **77 rows = ≤64 ROOTS** at `bb2bb7a75` (8 empty rows across 2 parent throws; 59 single-row parents form a long flat tail). Root count sizes **WORK, not YIELD** — one root can convert 22 rows or 1 (both measured today) — so it is the effort estimate and forecasts nothing about the verdict curve; a ceiling, one host's snapshot, re-derived at each waypoint |
| Shipped in 1.23.12.2 (the release snapshot) | 189 / 208 = 90.9% · 26,043 matching · 148 disclosed |
| Landed overnight (two pushed trains, every merge union-gated) | G's nine reflect/fidelity branches + the §4.3 IVT answer · the W3a promoted-forwarder gen regression FOUND-FIXED (net's public TCPConn surface restored; caught by the derived net/http canary, invisible to CNR/stdlib gates) · R's gettimeofday intrinsics · os/user's full ptrout arc with the DC-independent 4/4 guard · both standing behavioral Output reds closed at one root (G's probe fix) · the zero-match filter guard · **attribute-flush-die at the test host (a throwing goroutine is now a 7-second loud red, not a 40-minute silent deadline burn)** · **reflect's four runtime offset-bridges BOUND — TestOffsetLock: unbounded hang → PASS in 6s; reflect iterates at ~88s** · chip-2 documented per its own dead-code measurement |
| **Rows remaining (implementable)** | **8** — reflect (offset bridges bound, suite fast, fidelity landed; remaining: the descriptor fresh pass per `DESIGN-descriptor-contract.md` + the 12-test block family), os (seam row settling, then bankable), unique (16/20, residual isolated pure, disclosure-shaped), os/user (E2 oracle stands; the ptrout members now proven — bank decision grows the denominator honestly), runtime (fresh-pass prompt staged; the semantic bill prints at zero), runtime/pprof, runtime/trace, testing (Option 1 ruled, implementation sequenced) |
| In flight | R: reflect's ranked list (the parent-throw fix, then the token-rows assertion census deciding fix-vs-disclose); i9: wave A1 assigned, landing ACK still outstanding; G: stood down with the day banked (runtime fresh-pass + descriptor arc + five merged branches); coordinator: merging waypoints as they post — master moved TEN times today; behavioral suite FULLY GREEN (652/652, zero Output failures); `docs/PLAN-rebank-wave.md` on master in fleet review |
| Linux parity axis | 178 of 195 applicable annotated · stdlib `GoTargetOS=linux` builds green at master (battery5) |
| Host-exception ledger (i7-class) | net (DNS oracle) · crypto/tls (BoGo wall) · net/http's 4 h2 deadline rows (measured 250–500ms bracket) · **net/http's GO oracle itself now package-fails on this host (1,326/1,326 test-level passes, exit-time leak check) — sweep-form comparisons are unmeasurable here; the C# side proved by full direct run.** Counter-observations from G's laptop: net PASSES there, crypto/tls's BoGo FANS OUT — a banked count is host-dependent, so a red on one machine is not evidence about the row until the same host reproduces it |

## The remaining 17, by distance

### Days-class — active arcs or complete blueprints
| Package | ~Verdicts | Status |
|---|---|---|
| **net/http** | ~1,345 | **ZERO unexplained rows — bank dispatched to i9** (`claude/i9-nethttp-bank`). The 9-row model resolved: 5 confirmed by the merged fixes (union-verified); TestTransportGCRequest → **GATED under the new liveness-hang class** (fourth codegen-liveness trigger: wrapper-field round-trip, 10-variant isolation; closes OQ-L3); TestWriteDeadlineExtendedOnNewRequest/h2 → **performance-class disclosure** (bracketed: fails 125/250ms, passes 500ms; upstream carries the no-retry asymmetry). Banks after the window push, rebased + swept at real master. |
| **net/http/httptrace** | 2 | **DONE on branch — the MakeFunc chip landed** (`681c71410`: hand-owned `MakeFunc` as `Value.Call`'s exact inverse, full gate set incl. re-derived 5-row canaries at 2,247 verdicts). Banks 2\|0 at the banking window; merge owes the post-merge filtered sweep. Bonus: `reflect/iter`'s rangefunc Seq/Seq2 become live consumers. |
| **iter** | 28 | **ARC COMPLETE on branch (`claude/local-iter-arc` @`dd342ac65`): 28\|28 matching, 0 disclosed, six-run reproduced.** Managed coro rendezvous per blueprint (golib `Coro.cs` + `iter_impl.cs` via ConditionalWeakTable; Goexit/panic cross the boundary under `Goroutine.Run`); `NumGoroutine` wired — it had NEVER answered truthfully (always 1). Gates: CNR 679 byte-identical, GolibTests 434/434, converter suite, guard 4/4 twice. Banks in the post-window pass; **fully gated including the full slnx build (0 errors, 1092s — the lane's "reaped" reading was its own probe unable to observe dotnet.exe outside the worktree, corrected on the record)**; owes only bank artifacts + roster row + post-merge sweep. Bonus for the rebank family: seven pre-existing drifted runtime files censused; `gcount()` consumers chipped. |
| **internal/godebug, internal/concurrent** | small | Hand-owned-by-consequence pair; bounded local-lane bank attempts, unattempted only for lack of a slot. |
| **os** | 686 | **First-contact census DONE (08-29): 682/686 matching = 99.4%, zero unreached — the old "week-class, unattempted" label was stale history.** 4 rows: NetShareAdd declared host limit; TestDirectorySymbolicLink candidate-rooted to the `adjustTokenPrivileges` struct-passing seam (fix lane dispatched, discriminating experiment named in the census); TestWriteStringAlloc zero-bound alloc arc (improved 3,168 → ~1,185 B/op); TestUTF16Alloc disclosed, prose refresh ruled. Bankable shape once the seam row settles. Census record: `CENSUS-os-first-contact.md`. |
| **net/http/pprof, net/http/cgi** | small | Satellites: pprof rides net/http's bank; cgi is process-spawn shaped (testenv/exec plumbing now proven by debug/pe's TestDWARF). |

### Week-class — decomposing or seam-heavy
| Package | ~Verdicts | Status |
|---|---|---|
| **reflect** | ~418 | **G's arc, 192 divergences confirmed at the RC** (was will-not-compile yesterday). 125-row ABI family closed by FuncForPC; tail = the 12-test block family (platform-liveness adjacent), the named panics (`SetMapIndex`, zero-Value), the method-table identity family, DeepEqualAllocs (38, disclosure-shaped). MakeFunc chip feeds `reflect/iter` consumers. |
| **internal/syscall/windows** | medium | Seam-heavy but Windows-native; every syscall-seam remedy from the net/syscall campaigns applies. (`internal/poll` left this row 08-29: first-contact census came back **19/19 clean** — banked with the honest note that the suite covers ~7% of the FD surface; the netpoll engine is validated through the os/net rows.) |
| **unique, internal/weak, crypto/internal/boring/bcache** | small | GC-liveness territory — deliberately deferred; the codegen-liveness disclosure class and the platform-liveness residual (CoreCLR frame-slot) are the governing precedents. |

### The bosses
| Package | ~Verdicts | Status |
|---|---|---|
| **runtime** | largest | Unattempted — scheduler/GC tests meet threads-per-goroutine (SCHED-S1's measured bill) and the platform-liveness residual head-on. Expect its own campaign with a disclosure framework. |
| **runtime/pprof, runtime/trace** | medium | Capability-heavy; pprof deliberately sequenced last on the Linux frontier already. |
| **testing** | meta | The hand-owned host validating itself — special-case by design; ruling owed on what "validation" honestly means for it. |

## Standing post-release queue (merges + arcs, in order)
1. Banking window: MakeFunc/httptrace (preflighted) + buffer + math/big + godebug → **193/208 = 92.8%**; each banking merge owes its post-merge filtered sweep at the merge result; three-way header composition is the coordinator's
2. ctor-initializers merge — **UNION GATE GREEN (08-29): clean merge verified whole (203-line adjacent-edit hazard checked symbol-by-symbol), CNR byte-identical 679 pkgs, behavioral 647/647 all phases, zero NOT MEASURED. Merge-ready.**
2a. `claude/local-areequal-uncomparable` merge (R4: uncomparable `==` panics with Go's texts; golib equality machinery ⟹ **owes the reflect-bridge canary set, derived fresh at gate time**; plausibly clears reflect's TestArrayOfAlg/TestStructOfAlg by shared-predicate construction)
2b. `claude/local-symlink-privilege` merge (**pushed @`ca24bcbab`, end state proven**: adjustTokenPrivileges blittable mirror + createSymbolicLink raw-metal gate with retirement note; os → 685 measured / 682 matching / 3 residual / 4 gated, bankable shape; touches two converter .go files ⟹ full CNR rides the union gate)
4. Position-table splitter fix merge (`507e0a4f1`, i9 — signed, protocol-complete: red reproducer + guard, blast radius 26 metadata files isolated by two seeded reconverts, predicate widened to block comments; superfluous-logs passes as side effect) — lands BEFORE the leveling regen; union CNR at merge
5. syscall-intrinsics (R's branch) + the sendmsg chip → syscall's Linux annotation path
6. Rebank family pass (go/doc/comment files, five-package test-info staleness, lookup_windows.cs.auto, and the funcLits position-map drift the os census rooted by control — corpus-wide extent unmeasured, needs the seeded-reconvert classification)
7. The 13-file `len()` folding leveling item

*History note: this tracker opened on the day the roster crossed 90% — 184 → 189 rows in one
day, with net (472, the largest networking row), reflect's first execution, and five converter/
golib/gen defect families closed. The remaining distance is 17 named rows, none a mystery.*
