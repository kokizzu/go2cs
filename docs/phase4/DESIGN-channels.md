# DESIGN — golib channels: real rendezvous, cap/len, single-fire select, uniform-random choice

> **Status: LANDED ON MASTER (integrated ~2026-07-24/25 via cherry-pick; ground-truthed 2026-08-02).**
> The branch's commits were integrated commit-for-commit — master's `channel.cs` history carries the
> train (`76aefaead` Unit 1 real rendezvous/cap-len/single-fire-selectgo/uniform-random,
> `74b1a347a` per-thread frame stack, `e63836204`, `2a35b9a44`, `ccd53d3ff`, `d56b1c93b` depth-cap
> revert) and all the guards named below exist as green behavioral projects (`ChannelRendezvous`
> output-matches `go run` on current master). The original branch ref was deleted post-integration,
> leaving its commits dangling — which later made this header's "pending-integration" read as an
> open item. ⚠ The charter's 2026-07-31 Tier-0 note "the gaps are still real" was written from that
> stale reading and was WRONG — §9's don't-trust-stale-claims trap, inside the charter. Corrected
> 2026-08-02; the Tier-0 channels frog is CLOSED.
>
> *Header as it stood before the correction:*
> **Status: IMPLEMENTED-pending-integration (2026-07-24, branch `claude/wave3-channels`).** Design
> blessed by the user; Units 1 and 2 are implemented as specified in §3: **Unit 1** =
> `d637e9c31` (ChanCore/selectgo golib rewrite + SelectOp registration + hardened pending slot +
> unbuffered-make converter flip + gen-template de-clamp + ThreadPool floor + the 7 new behavioral
> guards), **Unit 2** = `4281074fc` (default-form `trySelect` ordinal
> lowering + the 5 default-form golden re-baselines + these doc updates). Unit 3 (waiter pooling /
> lock tuning) remains deferred until profiled. **Adversarial verification round (post-gating):**
> semantics skeptic clean; protocol skeptic findings fixed on the branch — `e9b9d80ab` (MAJOR:
> the pending receive commit is a per-thread frame STACK, so a select nested in the winning
> guard's target expression cannot destroy the outer commit; guard `NestedSelectRecvTarget`,
> §3 amendment below), `64ec36bad` (racy `SendIsReady`/`ReceiveIsReady` probe surface deleted),
> `8fcfda655` (`channel.Wait` plain timed wait, no per-call SemaphoreSlim). **Round-2
> verification:** `bd0b41d79` (CRITICAL emission fix — every select receive-case channel operand
> hoisted into a select-scoped `selᴛN` temp, evaluated exactly once at select entry per Go's
> spec; supersedes the "blocking-select goldens byte-identical" property; guard
> `SelectOperandOnceEval`) and `c7ab16feb` (strand remarks corrected plus the close-wake recv-bias divergence note in §4;
> its depth-64 cap-and-drop was FALSIFIED by recursion-through-the-out-target and reverted —
> guard `DeepSelectRecursion`). **Round-2 completion:** the source-order divergence that round-2 left
> behind (send-case operands evaluating after receive-case ones) is now REPAIRED — `visitSelectStmt`
> hoists EVERY case's operands in strict source order, a send case lifting its whole registration
> call; §4's divergence bullet is marked RESOLVED and guard `SelectOperandSourceOrder` locks it in.
> Gates run on the
> branch after every round: CNR drift = exactly the intended re-baselines (every line inspected,
> byte-identical once committed), full behavioral suite green (466/466 with Output 436/0 after
> round 2); corpus reconvert-diff 100%-classified + full 302-package build 0 errors; banked
> canaries math/rand 43, text/scanner 18, sort 63; results in the branch reports. Coordinator re-gates all-ships-rise at integration before landing on master.
> Produced 2026-07-24 by an adversarial design panel (three independent design lenses + a critic
> that verified claims against the real goldens and golib source), synthesized by the campaign
> coordinator. Companion to
> [`Phase4-Autonomous-Loop-Charter.md`](Phase4-Autonomous-Loop-Charter.md) Tier-0 item 1.

## 1. The four gaps (all confirmed in `src/core/golib/channel.cs`)

1. **No real unbuffered rendezvous** — `make(chan T)` behaves as `make(chan T, 1)`: a sender
   completes with no receiver present.
2. **cap/len conflation** — the same modeling makes `cap()` report 1 (not 0) and `len()` wrong for
   unbuffered channels; `make(chan T)` and `make(chan T,1)` emit byte-identical C#
   (`new channel<T>(1)`), so no runtime information can separate them.
3. **Blocking select is not single-fire** — send cases commit eagerly via `Sending()` /
   `ProcessSendQueue` background sends during argument evaluation, so *every* send case whose
   channel ever becomes ready delivers a phantom value (Go commits exactly one case).
4. **Ready-case choice is not uniform-random** — `select(params WaitHandle[])` resolves via
   `WaitHandle.WaitAny` (deterministic lowest-index); Go picks uniformly at random among ready cases.

Also fixed by this design (verified latent bugs): the comma-ok **closed-before-drain** bug
(`Receive(bool _)` / `Received(out,out)` return `(zero, false)` even when buffered data remains —
Go drains first), and the shared `ManualResetEventSlim` lost-wakeup/Reset races (self-healing only
via 200 ms re-polls). The `WaitHandle.WaitAny` 64-case limit dies with the plumbing.

**Consumers blocked on this work:** encoding/base32 + base64 (io.Pipe), bufio, os/signal, sync,
time (timers), net.

## 2. Panel summary

| Lens | Verdict |
|---|---|
| **D1 — ".NET-native"** (honestly rejected `System.Threading.Channels`; converged on an hchan+selectgo port behind byte-identical select glyphs) | **Strongest.** Same faithful core as D2, but preserves the emitted select contract byte-for-byte for the blocking form. Two repairable holes found (pending-slot leak on send-case wins; non-green staging). |
| **D2 — Go-faithful** (hchan + selectgo, plus a `visitSelectStmt` emission rewrite) | Right algorithm, wrong integration cost: the emission rewrite discards ~366 lines of battle-hardened select-emitter machinery and re-baselines 6 behavioral + 25 corpus select files for zero semantic gain. Its runtime-routine specs are the most precise — mined for the synthesis. Its close spec had a CAS-claim hole (fixed in synthesis). |
| **D3 — minimal-delta** | **Rejected (fatal).** Its select() polls readiness but leaves recv commits in the case guards → under concurrency a blocking select can silently execute ZERO cases (Go requires exactly one). Its WaitAny(200ms)+spin-counter→fatal(DeadLock) kills slow-but-live selects (os/signal, `time.After(5s)`). Preserves the comma-ok and lost-wakeup bugs. Dual substrate = nothing-throwaway violation. |

Key facts the panel established (verified against code/goldens):
- `System.Threading.Channels` **cannot** express Go channels: no capacity-0 rendezvous (min bounded
  capacity is 1 — exactly today's conflation), no multi-channel select, no atomic single-commit
  spanning SEND cases, no uniform-random ready choice. A custom monitor-based core is required.
- Goroutines are **synchronous ThreadPool work items** (`goǃ` → `QueueUserWorkItem`), so parked
  channel ops block pool threads. True rendezvous parks more of them than today.
- No golden contains `WaitHandle`: flipping the select-registration return types
  (`WaitHandle` → `SelectOp`) is **invisible to overload resolution at every emitted call site** —
  the blocking-select goldens stay byte-identical.
- **Staging trap (verified):** landing rendezvous *before* the select rework regresses the legacy
  `Sending()` path (losing send cases park background threads forever on a now-truly-unbuffered
  channel and later deliver phantom values) — `SelectStatement`'s fibonacci select goes red
  mid-stage. **Rendezvous + select rework must land as ONE gated unit.**

## 3. The synthesized design (recommended)

**Core (from D1, specified to D2's precision):** replace `channel<T>` internals with a single-field
struct over `ChanCore<T>`:
- Monitor lock object (`hchan.lock`), circular `T[] buf` (null when `dataqsiz == 0`),
  `sendx/recvx/qcount`, `closed`, intrusive `recvq`/`sendq` waiter queues, monotonic
  `Interlocked`-incremented `Id` (the total lock order for select).
- `Waiter` (sudog analog): boxed elem slot, ok flag, `SemaphoreSlim(0,1)` park, `SelectState sel`,
  `opIndex`. `SelectState`: `int winner = -1` claimed via `Interlocked.CompareExchange` (the
  single-fire authority) + a shared park semaphore.
- `chansend`/`chanrecv`/`closechan` follow Go's routines exactly, including the buffered-full
  parked-sender head-take/tail-enqueue rotation and the **drain-before-zero comma-ok fix**.
  **Every waker — plain send, plain recv, select commit, AND close — claims a select waiter via the
  winner CAS or skips it**; close never touches a waiter it failed to claim (the hole the critic
  found in D2's close spec).
- `cap() = dataqsiz`, `len() = qcount`, `IsUnbuffered = dataqsiz == 0` — gaps 1+2 by construction.
  (**Amended 2026-08-03:** a channel with an owning `IChannelTimer` masks `cap()`/`len()` to 0 while
  that owner hides its buffer — Go's own `chancap`/`chanlen` branch. `IsUnbuffered` still reports
  `dataqsiz == 0`. See §4's timer entry.)
- Publish-before-signal discipline (set value/ok before `Release`); park = unlock **then** wait,
  never hold the channel lock across a park.

**Select (selectgo port, emitted TEXT unchanged for the blocking form):** registration methods
return a type-erased `SelectOp` descriptor instead of `WaitHandle`; `select(params SelectOp[])`:
partition out nil channels (never registered — Go semantics), `Id`-sorted `Monitor.Enter` over
distinct cores, Fisher-Yates pollorder scan (thread-local RNG), **commit exactly one ready op under
the held locks** (gaps 3+4 for the blocking form), else park one `SelectState`-linked waiter per
case; on wake re-lock, unregister losers. The committed recv value crosses to the unchanged
`case N when ch.ꟷᐳ(out v):` guard via a `[ThreadStatic]` pending slot, **hardened**: stash ONLY
recv commits, explicitly clear on send-case wins (a select can have send and recv cases on the SAME
channel — `SelectStatement` does), guards consume unconditionally, debug-assert the slot is empty on
`select()` entry. **[Amended by the adversarial verification round:** the slot is a per-thread
pending-frame STACK popped by channel-core match — the guard's out-argument target expression is
evaluated BEFORE the guard call, and legal Go can run another select there
(`case a[f()] = <-ch:` where `f()` selects), which destroyed a single slot (outer value lost or the
next buffered value stolen). Frames push/pop balanced across nesting, so the clear-on-send-win and
assert-empty-on-entry hardenings above are superseded (both are destructive in a nested context);
the accepted residual — a panic unwinding between commit and consume strands a frame, unbounded
under a panic/recover retry loop (accepted benign memory residual; live frames are bounded by the
call stack and the stack must never cap — `DeepSelectRecursion`); frames are never
mis-consumed, and a strand above a live frame abandons (never misdelivers) the outer commit — is
documented in `SelectPending` with Debug-only depth warnings. Guard:
`NestedSelectRecvTarget`.**]**

**Emission/generator footprint (the entire visible change):**
- Converter: `convCallExpr.go` unbuffered-make default literal `"1"` → `"0"` (covers plain and named
  channels). Golden churn: ~21 constructor literal lines across 8 behavioral projects (verified);
  `make(chan T, 1)` sites correctly stay `(1)`. Corpus sites regenerate on reconvert.
- go2cs-gen `IChannelTypeTemplate.cs`: 3 forwarder return types + remove the `size < 1 ? 1 : size`
  clamp (named channels can finally be unbuffered).
- golib: the rewrite above; the `Sending`/`Receiving`/`ProcessSendQueue`/`WaitHandle` plumbing is
  deleted with the rework.
- **No change** to the emitted select/send/recv shapes for the blocking form — blocking-select
  goldens stay byte-identical, and the battle-hardened `visitSelectStmt` emitter is untouched.

**Staged landing:**
- **Unit 1 (ONE gated commit):** ChanCore + chansend/chanrecv/closechan + SelectOp/selectgo +
  hardened pending slot + ctor-accepts-0 + the make-default converter flip + gen-template changes +
  a `ThreadPool.SetMinThreads` floor at golib module init. (Rendezvous and selectgo cannot be split —
  the verified staging trap.) Gate: CNR (expect exactly the ~21 ctor-literal flips, zero
  select-golden drift), `UpdateTestTargets --createTargetFiles` AFTER the CNR re-transpile, full
  behavioral suite, corpus recompile (gen change ⇒ suite + corpus per standing rule), re-validate
  all validated Phase-4 packages.
- **Unit 2 (follow-up):** default-form (non-blocking) select uniform-randomness — the one thing
  provably impossible against an ordered C# `switch`: route the default form through a non-blocking
  `trySelect` returning an ordinal; re-baselines the 3 default-form goldens. Until it lands, the
  divergence is documented and fairness-only (the default form is already single-fire because C#
  evaluates ordered guards until the first true).
- **Unit 3 (deferred until profiled):** waiter pooling / SpinLock tuning.

**Test plan (new behavioral projects, each output-compared vs `go run` unless noted):**
`ChannelRendezvous` (send blocks until receive; cap==0/len==0), `ChannelCapLen` (buffered fill/
drain), `SelectSingleFire` (two send-ready cases; drain both channels after; exactly one delivery —
deterministic), `SelectSendRecvMix` (only the committed op mutates state), `SelectRandomFairness`
(N iterations of a 2-ready blocking select; both branches taken — tolerance-bounded, not a stdout
golden), `CloseWakesBlockedSenders`/`Receivers` + `CloseDuringBlockedSelect` in both directions
(parked select-send → panic on wake; parked select-recv → zero,false), `NilChannelInSelect`,
comma-ok drain-after-close on a buffered channel, unbuffered named channel (exercises the
de-clamped template). Operational proof: drive encoding/base32+base64, bufio, os/signal, sync, time
through the `-tests` pipeline; re-validate all banked packages 0-fail.

## 4. Documented divergences / notes

- **Blocked goroutines hold pool threads.** True rendezvous increases simultaneously-parked pool
  threads; `ThreadPool.SetMinThreads` floor is a mitigation, not a fix. Programs with thousands of
  blocked goroutines remain out of reach until a cooperative scheduler exists (explicitly out of
  scope here). Documented divergence.
- **Deadlock detection** stays the existing nil/all-nil approximation; a genuinely deadlocked
  all-real-channel program now parks forever (more Go-correct than the old accidental escape).
- **Close-wake recv bias on a dual-case select (determinism bias, deferred).** A PARKED select
  holding both a receive and a send case on ONE channel that then closes always fires the receive
  case: `closechan` drains `Recvq` before `Sendq`, and the first claim wins the select's CAS, so
  the recv waiter is always claimed first. Go re-polls the woken select and may uniformly-randomly
  take the SEND case instead — and panic ("send on closed channel"). Both outcomes are legal
  single-fire commits; ours is deterministic where Go's is random, and never takes the
  panic branch. Pre-existing Unit-1 scope, recorded by the round-2 verification — deferred, do not
  fix without re-gating the close family.
- **~~Send-case operands evaluate after receive-case operands (source-order divergence).~~
  RESOLVED 2026-07-24.** Recorded by the round-2 verification (in
  [`ConversionStrategies-Reference.md`](../ConversionStrategies-Reference.md), the hoist section) and
  user-ratified for repair. The round-2 hoist (`bd0b41d79`) lifted only receive-case channel operands
  into `selᴛN` temps; send-case channel and value expressions stayed inline in the registration
  argument list, which C# evaluates in argument order — i.e. after every hoisted temp — so a select
  whose FIRST case was a send observed `[recv-chan, send-chan, send-val]` where Go's order is
  `[send-chan, send-val, recv-chan]`. `visitSelectStmt` now hoists EVERY case's operands, emitted in
  strict source order, and the registration list names only temps; a send case hoists its WHOLE
  registration call (`var selᴛN = <chan>.ᐸꟷ(<value>, ꟷ);`), which is both legal and stronger — the
  call only BUILDS a `SelectOp` descriptor (the commit inside `select`/`trySelect` performs the
  communication, so no send moves), its receiver-then-argument evaluation is exactly Go's
  channel-then-value order, and the value keeps its ORIGINAL argument position so every implicit
  conversion `convSendValueExpr` relies on the `in T` parameter to apply survives by construction (a
  separate value temp would infer `var t = 200;` as an `int` for a `chan byte` — CS1503). Guard
  `SelectOperandSourceOrder`; counter-proven against the pre-fix converter, which prints
  `3:recv-chan 1:send-chan 2:send-val` where Go prints `1:send-chan 2:send-val 3:recv-chan`.
- **A channel can now have an OWNING TIMER — the successor arc, landed 2026-08-03 (r39b).** The
  design above deliberately stopped at Go's `hchan`; Go 1.23's SYNCHRONOUS timer channel (#37196)
  is the one piece of `hchan` that was left out, because it is the only place a producer may
  **un-send**. It is now here, as the general hook rather than a `time` special case: `ChanCore`
  carries an optional `IChannelTimer` (Go's `hchan.timer`), installed by `channel<T>.AttachTimer`
  before the timer is armed; `Capacity`/`Length` report 0 while that owner answers `HidesBuffer`
  (Go's `chanlen`/`chancap` branch, asked LIVE because `GODEBUG=asynctimerchan` selects the model at
  every observation); and `channel<T>.DrainBuffer()` — Go's `runtime.timerchandrain` — empties the
  buffer without servicing parked waiters. The other half lives in `time_impl.cs` (a per-timer send
  lock plus a stale-send `seq`, so a firing committed before a `Stop`/`Reset` is ABANDONED rather
  than delivered after it). `IsUnbuffered` still reports the PHYSICAL shape — a timer channel's send
  does not rendezvous — since Go exposes only `cap()`. This closed `time`'s last four semantic rows
  (`TestChan` now passes in all three `asynctimerchan` modes, Timer and Ticker); guard
  `SyncTimerChannel`, and the property statement lives in `time_impl.cs` under SYNCHRONOUS TIMER
  CHANNELS and in [`ConversionStrategies-Reference.md`](../ConversionStrategies-Reference.md).
  ⚠ `DrainBuffer` is a revocation primitive: it is sound ONLY for a channel whose producer owns it
  exclusively. Do not reach for it to "clear" an ordinary channel. Go states that precondition in a
  comment; here it is ENFORCED (a parked sender throws), because emptying the buffer under one
  breaks the `parked sender implies full buffer` invariant `TryCommitRecvLocked`'s hand-off branch
  rests on — the next receive then hands back a fabricated zero and swallows the sender's value.
  Measured by the adversarial round against a hand-written C# caller; unreachable from converted Go,
  since `Timer.C` is `<-chan Time` upstream.
- **NuGet lockstep:** golib signatures and the gen template change together — `go.lib` and `go.gen`
  must version-bump in the same release or `-recurse=nuget` apps can restore mismatched pairs.
  The timer hook widens that to a THIRD pair: `go.time` from build N calls golib members that build
  N−1 does not have, so a consumer pinning `go.lib` while floating `go.time` fails at RUNTIME with a
  `MissingMethodException`, not at compile time. `version.props` is single-source and
  `push-nuget.ps1 -Push` ships every package at one version, so the shipped flow cannot produce the
  mismatch — but it is exactly what this note exists to predict, and it belongs in the release notes.
- **A parked receiver is NOT evidence of a lost wakeup — read the core's state, never the source
  flow (2026-08-02).** The first post-wave3 "channel defect" sighting (`os`'s `TestPipeEOF`: a
  goroutine parked in `ChanCore.Recv` inside a `for range`, reported as ranging over an
  already-closed channel) dissolved on measurement — the channel was **open**, because the test
  body had aborted via `t.Fatal` before its `close`, and Go deadlocks on that same branch. The
  close/receive protocol is airtight by construction here: `Recv` checks `Closed`, decides to park,
  and enqueues on `Recvq` inside ONE `SyncRoot` hold, and `closechan` takes that same lock before
  draining, so no window exists between the check and the park for a plain waiter. Adjudicate the
  next such sighting the same cheap way: gate `Recv`/`Send`'s park onto a timed wait that dumps
  `Closed`/`Qcount`/queue-emptiness plus the parked stack, and log every `closechan` — one run
  separates "never woken" from "never closed" with no debugger. Standing racing evidence lives in
  `src/tests/GolibTests/ChannelWakeupStrainTests.cs` (ranging receiver, direct hand-off, and blocked
  select, each raced against a close under pool/GC pressure); the deterministic behavioral guards
  above prove the protocol on one interleaving, these prove it on thousands.
  **Epilogue (2026-08-03, r38-os-fin): the row that was routed here was not in this layer at all, and
  it was not in `internal/poll` either — it was two layers further down.** `TestPipeEOF` reached its
  `t.Fatal` because `bufio.Reader.ReadBytes` got a premature `io.EOF`, and that came from the
  `ж<T>` → `uintptr` conversion handing a syscall an address whose `fixed` pin had already expired: a
  gen0 collection during the 10 ms blocking `ReadFile` moved the `*uint32` byte-count box, so the
  kernel's write landed nowhere, `done` stayed 0, and `FD.eofError` read that as EOF. Three layers of
  plausible attribution — channels, then poll, then handle lifetime — each dissolved on measurement.
  The instrument above is what made the first two cheap to disprove; the third needed a different
  one (compare the address across a forced collection), and the general lesson is the same either
  way: **measure the layer you are accusing before you accuse it.**

## 5. Decision requested (user)

1. **Bless the synthesized design** (hchan+selectgo core behind unchanged blocking-form emission)?
2. **Unit 2 timing** — implement immediately after Unit 1 (recommended; small, completes gap 4), or
   defer with the documented fairness-only divergence?
3. **Accept the pool-thread divergence note** as the standing position on goroutine scheduling?

Coordinator recommendation: bless as specified; Unit 1 as one gated commit implemented by a
top-tier agent (or the coordinator) with adversarial review on the park/claim paths; Unit 2
immediately after as its own gated commit.

## 6. Dated stub, 2026-09-23 (C1, REC-G of the H10 relabel ruling) -- a single-object channel core

Ruled at ledger 2026-09-23 03:37 (X(2) REC-G, O4). **Owner: R, after its S-arc. Full design: phase-4D
kickoff.** Nothing above this block is rewritten; read at `bb54ff0920`.

**The claim to refute or prove.** golib's channel core charges FOUR counted objects per channel
(src/core/golib/channel.cs:363-368): the core instance plus the three its field initializers allocate
(`SyncRoot`, `Recvq`, `Sendq`), because "the .NET shape needs four objects to hold the same state".
Go's `makechan` allocates the `hchan` with its lock and both wait queues inside one struct. A
single-object core -- the lock and both queue heads held as value fields of the core itself -- either
refutes that sentence or proves it; this stub asks for the answer, not a particular layout.

**The stage that REMOVES the counted allocations:** the single-object core, if the answer is that it
can be built. *Removes:* three counted objects per channel created. *Preconditions:* the park/claim
paths of section 3 keep their single-fire and fairness properties with the queues as value fields (the
adversarial review that section 5 asked for on those paths re-runs); a `Monitor`-style lock over the
core object itself replaces `SyncRoot` only if nothing else locks on the core.

**Refusals:** any layout that makes a channel value copyable (a channel is a reference in Go); any
lock whose object is reachable from user code.

**Members:** io TestPipeAllocations (want at most 4). Its 14 per run read at src/core/io/pipe.cs:245-253
as one `PipeWriter` box, three channels at four objects each, and one field-ref view
(`pw.of(PipeWriter.Ꮡr)`, :253, the first view minted for a new box).

**Prediction (UNMEASURED):** io TestPipeAllocations 14 -> 5 = Go's own 4 + 1 view. Go's `io.Pipe`
allocates four objects itself -- the `PipeWriter` (with its embedded `PipeReader` and `pipe`) and the
three channels (go1.24.13 io/pipe.go) -- so the `PipeWriter` box is Go's own allocation, not an excess,
and zh-box B′ has nothing to take here (B′ does not reach a returned interior pointer either). The one
object above Go's four is the field-ref VIEW `pw.of(PipeWriter.Ꮡr)` (io/pipe.cs:253), the `*PipeReader`
Pipe returns, which points INTO the `PipeWriter`'s storage. **No record removes it today:** under `ж<T>`,
a pointer is a reference to an object, so a pointer to a field of another box needs a view object of
its own; a representation in which such a pointer is a value (owner plus field offset) would remove it,
and no record proposes one. So 5 against the want of 4 is stated here as the prediction, NOT as a floor
-- a floor would need a proof on some basis other than "Go keeps it off the heap", which
ConversionStrategies-Reference.md:21461-21464 forbids, and none is offered. *(Restated 2026-09-23 per
COORD's ACCEPT-WITH-FIXES, ledger 3942e083ad, item 11: the first version called 5 a floor and gave the
residue to B′.)*

**Gate:** the channel behavioral tests and the golib channel suite, then io's row before and after at
Release with tiering off.

## 7. REC-G design, 2026-09-23 (R) -- the single-object core is buildable: one counted object per channel

**Status:** DESIGN, docs only. R owns it after the synctest arc (COORD, 2026-09-23). No code;
`channel.cs` is untouched. Read at `fc6269b0bf` (`claude/version-go1.24.13`, `channel.cs` last
touched at `c3d8bb388b`) and against the arc's `channel.cs` at `7984c46149`
(`claude/r-s4-synctest-pulls`). Nothing above this block is rewritten.

**The answer to §6: REFUTED.** The sentence at `channel.cs:363-368`, "the .NET shape needs four
objects to hold the same state", is false. The three extra objects are a layout choice, and a
layout that holds the same state in the core instance alone keeps every property §3 relies on. Go's
`hchan` holds `recvq waitq`, `sendq waitq` and `lock mutex` inside the struct (go1.24.13
`runtime/chan.go`), and `makechan` of an unbuffered channel is one `mallocgc(hchanSize, ...)`. After
this design an unbuffered channel costs golib one counted object too.

### 7.1 The layout

- **The lock is the core itself.** `Monitor.Enter(this)` replaces `Monitor.Enter(SyncRoot)`, and
  the `SyncRoot` field goes. A thin lock lives in the object header and allocates nothing on the
  managed heap. Contention, or a lock on an object whose header already holds a hash code, inflates
  it to a SyncBlock. A SyncBlock is runtime-native memory: not a GC object, not counted by
  `AllocationCounter`, not seen by `GC.GetAllocatedBytesForCurrentThread`. `System.Threading.Lock`
  is refused: it is an object, so it would be the same second allocation under a new name.
- **The two queues are inline head/tail pairs on the core.** `m_recvHead`, `m_recvTail`,
  `m_sendHead` and `m_sendTail` are fields of `ChanCore`, as `waitq{first, last}` are fields of
  `hchan`. The operations `Enqueue`, `Remove`, `DequeueForWake` and `IsEmpty` become `ChanCore`
  methods that select the pair.
- **The pair is selected by the waiter's own `IsSend`.** Measured at the tree: all four enqueue
  sites put a send waiter on `Sendq` and a receive waiter on `Recvq`, with no exceptions
  (`channel.cs:462`, `:547`, `:868`, `:872`). A waiter's queue is therefore fixed by `IsSend`.
- **The waiter's back-reference.** `Waiter.Queue` (a `WaiterQueue?`, `channel.cs:234`) becomes
  `ChanCore? QueuedOn`. `Enqueue` sets it; `Remove` and `DequeueForWake` clear it. `Remove`'s no-op
  test becomes `waiter.QueuedOn != this`, with the pair chosen by `waiter.IsSend`. The one outside
  caller, select's loser unregistration (`waiter.Queue?.Remove(waiter)`, `channel.cs:896`), becomes
  `waiter.QueuedOn?.Remove(waiter)`.
- **Two alternatives are refused.** A `WaiterQueue` *struct* held as a field would work only
  through `ref`; one by-value copy (`var q = core.Recvq;`) would enqueue onto a copy and lose a
  waiter silently, with no compile error. The inline fields cannot be copied. A layout that makes a
  channel value copyable is refused by §6; this one keeps `ChanCore` a class, and `channel<T>`
  stays a struct holding a reference to it, as today.

### 7.2 §6's two preconditions, checked at the tree

1. **Nothing else locks on the core.** Census at `fc6269b0bf`:
   - every lock and `Monitor` call on channel state is in `channel.cs`, on `SyncRoot`: 19 use sites
     beside the declaration and the constructor comment (20 at the arc tip);
   - `LockAll`/`UnlockAll` (`:1038`, `:1044`) go through the same field;
   - no `Monitor.Wait`, `Pulse` or `TryEnter` is used on it;
   - no file outside `channel.cs` names `SyncRoot` on a core.

   After the change, the core is the lock and nothing else holds it.
2. **The core is not reachable from user code.**
   - `ChanCore` is `internal` to golib, and `channel<T>.m_core` is `private` (`:1056`).
   - The two other holders are both internal: `SelectOp.Core` (`:164`) and `SelectPending`'s
     frames (`:757`). `GoReflect.Select.cs:27` states that the reflect bridge adds no public route.
   - golib's `InternalsVisibleTo` grants (`ж.cs:14-16`: `unsafe`, `GolibTests`, `runtime`) and
     the synthesized-structs grant name no file that touches a core.

   Converted Go has no lock-on-object construct at all.

**One layout dependency, already neutralized.** `time_impl.cs:519-539` records that sleep.cs's
`cp` argument is a punned read of `ChanCore<Time>`'s fields. It is discarded (`_ = cp`) precisely so
that a field change cannot flip it. Removing three reference fields is such a change and is safe
for that reason.

### 7.3 What the change does NOT touch

- **Single-fire.** The claim CAS (`SelectState.TryClaim`) and the claim-before-touch discipline in
  `DequeueForWake` are unchanged.
- **FIFO order.** Wake order is unchanged: head-first, as `waitq.dequeue` is.
- **Lock order.** The select lock order is by `Id` (the §3 total order) and unchanged.
- **Commit order.** The arc's rule (park under the lock, unlock, then wait, readying before the
  release) is unchanged.
- **The lock is still never held across a park.**

The adversarial review §5 asked for on the park and claim paths re-runs over the changed queue
code, per §6.

### 7.4 Costs and residuals, named and not staged here

- **SyncBlock inflation (a cost, not a count).** `channel<T>.GetHashCode` and
  `PointerOrderToken` hash the core (`RuntimeHelpers.GetHashCode(m_core)`, `:1207` and `:1705`). A
  channel that is hashed (a map key, `%p`) and later locked inflates its header to a SyncBlock
  once. Today that happens to `SyncRoot` only under contention; after the change it also happens on
  the first lock of a hashed channel. It does not change the count.
- **Buffered channels of pointer-free `T`.**
  - The count: golib charges the buffer as its own counted array (`m_buf`, `:407`). Go puts it in
    the same malloc as the `hchan` when the element has no pointers
    (`mallocgc(hchanSize+mem, nil, true)`), and in a second malloc when it does. After REC-G such
    a channel counts 2 against Go's 1.
  - Why no stage: a .NET class cannot hold a length chosen at run time inline, since
    `InlineArray` fixes the length at compile time.
  - Impact: none on this block's members, which are all unbuffered.
- **One blocked operation is Waiter + SemaphoreSlim, counted 2** (`:460`, `:545`, `:850`, `:862`).
  Go uses one pooled sudog. That is a per-operation cost, not a per-channel one, and it is outside
  REC-G.

### 7.5 Members and predictions (UNMEASURED, stated to be scored)

- **io `TestPipeAllocations`: 14 -> 5**, as §6 predicts. The three unbuffered channels drop from
  4 to 1 each (-9). The `PipeWriter` box and the field-ref view are untouched. The want is 4, so
  the row still fails, by the view.
- **context `TestAllocs`: unchanged, still failing** (9 against 8 in the
  `WithTimeout(bg, 5*time.Millisecond)` case at `7984c46149`). That case calls `cancel()` before
  `Done()`, and `cancelCtx.cancel` stores the shared `closedchan`, so it makes no channel and
  REC-G does not reach it. The `1*time.Nanosecond` case does make one (`Done()` before cancel), so
  its count drops by 3; it already passes (limit 12).
- **The population, not classed.** In the go1.24.13 GOROOT, 25 `_test.go` files mention both
  `AllocsPerRun` and a channel-making call: `make(chan`, `io.Pipe`, `.Done()`, `NewTimer`, or
  `time.After(`. The predicate is over-inclusive, because a mention is not a channel made inside the
  measured closure. Reading each closure is the implementation seat's first step, before any
  prediction beyond io and context.

### 7.6 Sequencing, arms and gates for the implementation seat (for COORD to cut)

- **Base.** The implementation lands on post-hop master AFTER the synctest arc's train. The arc
  rewrites the same types: `Waiter.Parker`, `Wake`'s ready-before-release, `ChanCore.Bubble`,
  `RefuseOutsideBubble`, and `HandoffCrossesBubble` at `7984c46149`. A seat cut beside it would
  conflict on every park and claim path.
- **Red-first arms (GolibTests).**
  - **The count:** one `make(chan T)` of an unbuffered channel counts exactly 1. Today's 4 is the
    red.
  - **Loser unregistration on a shared core:** a select with two cases on the same channel (a
    send and a receive; two receives), where exactly one fires and the loser is gone from the
    right queue. Planted red: `Remove` choosing the pair by anything but `IsSend`.
  - **A second unregistration is a no-op:** planted red, `Remove` without the `QueuedOn` test.
  - **The lock census as a guard:** no lock or `Monitor` call in golib or its friend assemblies
    targets a core except `ChanCore`'s own. Controlled by a planted foreign lock.
  - **The existing park/claim contention-stress arms,** with ten repeats.
- **Gates.**
  - §6's: the channel behavioral tests, the golib channel suite, and io's row before and after, at
    Release with tiering off.
  - For a golib concurrency change: the FULL behavioral suite, all of GolibTests, and the stdlib
    build.
  - Rows equal to the roster: `sync`, `iter`, `context`, `time`, and `net/http` (the arc's
    re-entry row).
