# The managed netpoller — hand-owning the ten `runtime_poll*` contracts

> **STATUS: DESIGN — PROPOSED (2026-08-12, lane netpoll-design). Nothing in this document is
> ratified.** Every decision below is a proposal with a recommendation; §9 collects the ones that
> need a coordinator ruling before any implementation lane starts. Commissioned by the board's
> sockaddr RESOLVED note ([`BOARD-next-validation-candidates.md`](BOARD-next-validation-candidates.md),
> "RESOLVED 2026-08-11 (lane L10)": *"that is a design arc with a deadline/unblock story to settle,
> not a wrapper repair — it wants its own DESIGN doc and a coordinator ruling before anyone
> starts"*) and the matching L10 correction in [`LANES.md`](LANES.md) §L10. Companions:
> `src/core/internal/poll/fd_poll_runtime.cs` (the ten stubs and all their callers),
> `src/core/internal/poll/windows/fd_windows.cs` (`execIO`, the contract's driver),
> `src/core/runtime/netpoll.cs` + `src/core/runtime/windows/netpoll_windows.cs` (the converted
> runtime counterparts this design deliberately leaves dead), `src/core/golib/ж.cs` (the address
> model the submit seam prices against), and the precedent files cited inline. Written against the
> corpus at `0133b6aa7` (2026-08-12), Go 1.23.1.

---

## 1. The wall, measured — what stops `net.Listen` today

With L10's sockaddr fixes merged, `syscall.Bind` succeeds and `net.Listen` on Windows walks on to
this, the exact stack the board banked:

```
System.NotImplementedException: runtime_pollServerInit: external (assembly or cgo) function is not implemented
  at internal/poll.runtime_pollServerInit  (PartialStubGenerator stub)
  at internal\poll.pollDesc.init -> internal\poll.FD.Init  ... fd_poll_runtime.cs:48
  at net.netFD.init -> net.listenStream                    ... sock_posix.cs:216
  at net.Listen                                            ... dial.cs:933
```

`internal/poll` declares **ten** bodyless `//go:linkname` entry points into the runtime's network
poller (`fd_poll_runtime.cs:18–36`). The converter emits each as a bodyless `partial` method; with
no body provided, the `PartialStubGenerator` fills them with throwing stubs, and the first pollable
`FD.Init` — every socket `net` creates (`net/windows/fd_windows.cs:59` passes `pollable: true`;
`os` passes `false` for every file/pipe/console, `os/windows/file_windows.cs:83`) — dies in
`serverInit.Do(runtime_pollServerInit)`.

The counterparts EXIST in the converted runtime — `runtime/netpoll.cs:217` carries
`poll_runtime_pollServerInit` with its `//go:linkname` comment intact, and all nine others are
beside it — but nothing wires a linkname across assemblies, and §3 shows that wiring them would
not be sufficient: the bodies bottom out in the runtime scheduler, which does not exist under the
CLR. The honest remedy is the managed-API-boundary pattern this repo has used four times already
(§3.3): **hand-own the ten CONTRACTS in `internal/poll` against .NET's own completion machinery,
and leave the runtime's poller exactly as converted — dead.**

This is a **Windows-first** design. The corpus's compile target is `$(GoTargetOS)=windows`; the
Linux `internal/poll` is a *readiness*-model consumer (`fd_unix.go` retries the syscall after
`pd.wait`) rather than a *completion*-model one, and gets its own design when the Linux corpus
compiles (§8).

## 2. The contract inventory — ten functions, every caller

All ten stubs live in `fd_poll_runtime.cs` (build tag `unix || windows || wasip1` — the file is
FLAT in the L3 layout, shared by every GOOS). The callers below are the complete set inside
`internal/poll`; nothing else in the corpus calls them (the runtime's own `poll_runtime_*`
twins are unreferenced exports).

| # | Stub (`fd_poll_runtime.cs`) | Contract (from `runtime/netpoll.cs`) | Called by | Reached from |
|---|---|---|---|---|
| 1 | `runtime_pollServerInit()` (:20) | Initialize the poller, once (`:217–232`, `netpollGenericInit`) | `pollDesc.init` via `serverInit.Do` (:48) | first pollable `FD.Init` — `net.Listen`, `net.Dial`, accepted conns |
| 2 | `runtime_pollOpen(fd) (uintptr, nint)` (:22) | Register fd; return an opaque nonzero ctx or errno (`:251–284`) | `pollDesc.init` (:49) | same |
| 3 | `runtime_pollClose(ctx)` (:24) | Unregister; only legal after unblock (`:292–308`) | `pollDesc.close` (:61) | `FD.destroy` (last decref) |
| 4 | `runtime_pollWait(ctx, mode) nint` (:26) | Block until IO-ready in mode, or return the deadline/closing code (`:355–374`) | `pollDesc.wait` → `waitRead`/`waitWrite` (:92–106) | `execIO` after `ERROR_IO_PENDING` (`fd_windows.cs:188`) |
| 5 | `runtime_pollWaitCanceled(ctx, mode)` (:28) | Block until IO-ready, IGNORING deadline/closing — Windows-only, after a `CancelIoEx` (`:377–382`) | `pollDesc.waitCanceled` (:108–113) | `execIO` cancel path (`fd_windows.cs:219`) |
| 6 | `runtime_pollReset(ctx, mode) nint` (:30) | Clear consumed readiness; fail fast if closing/expired (`:335–347`) | `pollDesc.prepare` → `prepareRead`/`prepareWrite` (:73–87) | `execIO` before every submit (`fd_windows.cs:164`) |
| 7 | `runtime_pollSetDeadline(ctx, d, mode)` (:32) | Arm/replace/clear the read and/or write deadline; `d` is a RELATIVE ns duration (`:385–467`) | `setDeadlineImpl` (:163–189) | `FD.SetDeadline`/`SetReadDeadline`/`SetWriteDeadline` — `net.Conn` deadlines |
| 8 | `runtime_pollUnblock(ctx)` (:34) | Mark closing; wake both waiters with `pollErrClosing`; stop timers (`:473–505`) | `pollDesc.evict` (:66–71) | `FD.Close` (`fd_windows.cs:406`) |
| 9 | `runtime_isPollServerDescriptor(fd) bool` (:36) | Report whether fd IS the poller's own descriptor (`:242–244`) | `IsPollDescriptor` (:203–205) | test-only; linkname-pinned public surface (go.dev/issue/67401) |
| 10 | `runtimeNano() int64` (:18, `//go:linkname runtimeNano runtime.nanotime`) | Monotonic ns, arbitrary epoch | (declared for the shared file; no Windows-path caller today) | — |

Three contract facts that pin the implementation:

- **The error codes are shared constants, not an enum to invent.** `pollNoError`/`pollErrClosing`/
  `pollErrTimeout`/`pollErrNotPollable` = 0/1/2/3, declared on BOTH sides with "These must match"
  comments (`fd_poll_runtime.cs:119–127`, `runtime/netpoll.cs:44–50`). `convertErr`
  (`fd_poll_runtime.cs:129–146`) maps them to `errClosing(isFile)` / `ErrDeadlineExceeded` /
  `ErrNotPollable`, and `execIO` **panics** on any wait error outside that set
  (`fd_windows.cs:207`) — so the managed implementation may return exactly these four values and
  nothing else. `pollErrNotPollable` is never produced on the Windows path (it comes from the
  unix `eventErr` bit); it stays reserved.
- **One waiter per mode, by contract.** *"Concurrent calls to netpollblock in the same mode are
  forbidden, as pollDesc can hold only a single waiting goroutine for each mode"*
  (`runtime/netpoll.cs:577–578`). `fdMutex` (already operational via the hand-owned
  `runtime_sema_impl.cs`) serializes readers and writers above this layer. The managed pollDesc
  therefore needs a single-slot gate per mode, not a queue.
- **The ctx token is opaque.** Go returns the `*pollDesc` as a uintptr; `internal/poll` only
  stores it (`pollDesc.runtimeCtx`, copied into `operation.runtimeCtx` at `fd_windows.cs:365–366`)
  and passes it back. The managed side mints its own nonzero tokens (a monotonic counter keying a
  `ConcurrentDictionary`) — no pointer, no pollcache, no ABA (§4.1).

**The driver.** `execIO` (`fd_windows.cs:156–232`) is the loop every network operation runs:
`prepare` (reset) → `submit(o)` — an overlapped WSA call handed `&o.o` — → on `ERROR_IO_PENDING`,
`wait` → on success, harvest via `windows.WSAGetOverlappedResult(…, wait: false)`; on
deadline/close, `CancelIoEx(fd, &o.o)` → `waitCanceled` → harvest, mapping `ERROR_OPERATION_ABORTED`
back to the deadline/close error. The submit lambdas across `fd_windows.cs` + `sendfile_windows.cs`
cover, by native entry point: `WSARecv` (Read :459, RawRead :1236), `WSARecvFrom` (ReadFrom family
:615/:653/:691), `WSASend` (Write :757, Writev :885, RawWrite), `WSASendto` (WriteTo family
:912–:1004), `ConnectEx` (:1025), `AcceptEx` (:1041, paired with `GetAcceptExSockaddrs` in
`net/windows/fd_windows.cs:253`), `WSARecvMsg`/`WSASendMsg` (ReadMsg/WriteMsg families
:1352–:1463+), and `TransmitFile` (`sendfile_windows.cs:71`). This census is what §4.3 stages.

## 3. The wall behind the wall — why this design routes AROUND the runtime, not through it

### 3.1 The asmstdcall wall is real…

Wiring stub #1 to the runtime's `poll_runtime_pollServerInit` reaches `netpollGenericInit` →
`netpollinit` → `stdcall4(_CreateIoCompletionPort, …)` (`runtime/windows/netpoll_windows.cs:106`)
→ `asmstdcall`, a `PartialStubGenerator` stub: the runtime's syscall path is hand-written
assembly Go, and go2cs has never implemented it — the whole corpus dispatches through the
hand-owned managed trampoline instead (`syscall/windows/dll_windows.cs:93–122`, an unmanaged
function-pointer `calli` switch). That single call could be patched — `CreateIoCompletionPort` has
a working converted wrapper (`zsyscall_windows.cs:602`) — which is precisely why the wall must be
named as more than `asmstdcall`.

### 3.2 …but it is the SHALLOW wall. The deep wall is the scheduler.

Behind `netpollinit`, the runtime bodies consume, in order of appearance
(`runtime/netpoll.cs`): `lockInit`/`lock`/`unlock` on runtime mutexes with lock-rank bookkeeping
(:223–225), `pollcache.alloc` over `persistentalloc` non-GC memory (:736), the `timer` struct and
its `modify`/`stop` engine (:420–445) — the runtime timer subsystem entire — `gopark` with a
commit callback (:605), `goready` (:568), g-pointer CAS protocols storing goroutine identity in a
uintptr (:557), and the `eface`/`_type` reinterpret of `makeArg` (:757–768). Every one of those is
an organ of the Go scheduler. None exists under the CLR, and none CAN exist: a go2cs goroutine is
a managed thread the CLR schedules (`runtime/managed_impl.cs`, "Honest divergences"), so there is
no g to park, no P to hand a ready g to, and no sysmon to pump `netpoll(delta)`.

That last point is the decisive one. Go's poller is only half an API — the other half
(`netpoll(delta)`, `netpollBreak`, `netpollready`, `netpollAnyWaiters`; the platform interface
listed at `runtime/netpoll.cs:15–40`) is called by **the scheduler itself** from `findrunnable`
and sysmon. Under go2cs nothing would ever pump it: a perfectly-wired conversion would initialize
an IOCP and then block forever, because the thread Go dedicates to draining it IS the scheduler.
Emulating that means writing a scheduler. The ten-contract boundary is the only cut through this
subsystem that does not drag the scheduler across.

### 3.3 The doctrine, and the four precedents

`runtime/managed_impl.cs` states the fork this repo already took, verbatim: *"where a Go mechanism
has no managed counterpart but its PUBLIC CONTRACT does, reimplement the CONTRACT at the API
boundary and never emulate the mechanism… Everything below these entry points stays auto-converted
and simply becomes unreachable."* Four landings prove it at increasing depth:

1. **`sync`'s Mutex/RWMutex/WaitGroup** (`sync/mutex.cs`) — the runtime sleeping semaphore
   reimplemented on `SemaphoreSlim`/monitors; the co-designed starvation handoff explicitly NOT
   emulated.
2. **`internal/poll`'s own `runtime_Semacquire`/`runtime_Semrelease`**
   (`internal/poll/runtime_sema_impl.cs`) — the EXACT mechanical shape this design reuses: an
   `_impl.cs` beside the converted package, `[module: go.GoManualConversion]`, bodies for the
   bodyless partials (which suppresses the `PartialStubGenerator` stubs), keyed by ж-box identity
   where address identity is the contract.
3. **`runtime`'s process-control surface** (`runtime/managed_impl.cs`) — ReadMemStats, the
   traceback/Callers surface, GC entry points: contracts on CLR primitives, converted machinery
   below left dead.
4. **`syscall`'s runtime-provided primitives** (`syscall/syscall_impl.cs`,
   `syscall/windows/dll_windows.cs`) — Exit/Getpagesize and the entire Syscall/SyscallN
   trampoline: the runtime's assembly replaced by a managed dispatcher.

**Durability of the cut.** The ten linknames are one of Go's most compatibility-pinned internal
surfaces — `IsPollDescriptor` carries the hall-of-shame linkname notice ("Do not remove or change
the type signature", go.dev/issue/67401, `fd_poll_runtime.cs:194–202`), and Go 1.23 itself
rewrote `netpoll_windows.go`'s internals (source-tagged completion keys, timer sources —
`runtime/windows/netpoll_windows.cs:18–66`) while the ten crossed unchanged. A seam that Go's own
churn respects is a seam worth owning.

## 4. The design — the ten contracts on .NET's completion machinery

### 4.1 The managed pollDesc

A new hand-owned file, `src/core/internal/poll/windows/runtime_netpoll_impl.cs` (per-GOOS folder;
rides the existing `<Compile Include="$(GoTargetOS)/*.cs" />` glob, `internal.poll.csproj:145` —
no csproj change), carrying `[module: go.GoManualConversion]` per the marker rules, provides the
ten partial bodies over:

```
sealed class ManagedPollDesc {
    object gate;                    // ONE lock; §5 explains why lock-free is not owed
    bool closing;                   // pollUnblock ran (sticky for the desc's lifetime)
    ModeState r, w;                 // per-mode:
      bool ready;                   //   an IO completion arrived and is unconsumed
      bool expired;                 //   deadline passed and not since re-set (STICKY, §5)
      long generation;              //   invalidates stale timer callbacks (Go's rseq/wseq)
      Timer? timer;                 //   armed only while a deadline is pending
    nuint fd;                       // for isPollServerDescriptor bookkeeping only
}
static ConcurrentDictionary<uintptr, ManagedPollDesc> table;   // ctx token -> desc
static long nextToken;                                          // tokens start at 1; 0 = "no ctx"
```

Contract bodies, mapped one-to-one:

- `runtime_pollServerInit` — initialize the delivery mechanism (§4.2); idempotent under the
  caller's `sync.Once`.
- `runtime_pollOpen(fd)` — mint a token, create the desc, associate the fd with the completion
  mechanism; on association failure return `(0, errno)` so `pollDesc.init`'s
  `errnoErr(syscall.Errno(errno))` path stays live. Go REUSES pollDescs from a cache and defends
  staleness with `fdseq` (`runtime/netpoll.cs:199–203, 319–321`); the managed side allocates a
  fresh desc per open and retires the token at close, so the ABA machinery has nothing to defend
  and is deliberately absent.
- `runtime_pollReset(ctx, mode)` — under the lock: if `closing` → 1; if `mode` expired → 2; else
  `ready = false`, return 0. (Order per `netpollcheckerr`, `runtime/netpoll.cs:539–554`.)
- `runtime_pollWait(ctx, mode)` — under the lock, loop: **consume readiness first** (`ready` →
  `ready = false`, return 0 — Go consumes `pdReady` before checking errors,
  `runtime/netpoll.cs:585–589`, which is what lets a completion that raced a deadline still be
  harvested); else if `closing` → 1; else if expired → 2; else `Monitor.Wait(gate)` and loop.
- `runtime_pollWaitCanceled(ctx, mode)` — same loop but IGNORE `closing`/`expired`: wait until
  `ready`, consume, return (void). Liveness is §5's cancellation argument.
- `runtime_pollSetDeadline(ctx, d, mode)` — §5.
- `runtime_pollUnblock(ctx)` — under the lock: `closing = true`, bump both generations, stop both
  timers, `Monitor.PulseAll`. Sticky forever; Go's re-open reset does not apply (fresh desc per
  open).
- `runtime_pollClose(ctx)` — dispose timers, remove the token from the table. (Go asserts
  "close w/o unblock" / "blocked read on closing polldesc" throws here,
  `runtime/netpoll.cs:295–305`; the managed body keeps the same asserts as `throw`-equivalent
  `InvalidOperationException`s — they guard `internal/poll`'s own sequencing, which is unchanged
  converted code.)
- `runtime_isPollServerDescriptor(fd)` — §4.2 (delivery-mechanism dependent; `false` under the
  recommendation).
- `runtimeNano()` — the `sync/runtime_impl.cs:248–251` shape verbatim: `Stopwatch.GetTimestamp()`
  scaled to ns against a static base. Monotonic, arbitrary epoch, exactly the `runtime.nanotime`
  contract.

The completion callback (§4.2) is the only writer of `ready`: under the lock, `ready = true`,
`PulseAll`. Timeout and close wake waiters WITHOUT setting `ready` — the two-layer separation
(wake vs readiness) is the load-bearing structure Go encodes in `pdReady`-vs-`pdNil`-wake, and it
is what makes `waitCanceled`'s ignore-errors loop correct.

**Why one Monitor instead of Go's lock-free CAS protocol.** Go splits `pollDesc` state across
atomics (`rg`/`wg`, `atomicInfo`) because `netpollcheckerr` runs where the pd lock cannot be taken
and `netpollblock` parks through `gopark`'s publication protocol (`runtime/netpoll.cs:83–95,
556–613`). Neither constraint survives the boundary: every managed caller is an ordinary blocked
thread, and the single-waiter-per-mode contract bounds convoying to one reader + one writer + one
timer callback per desc. A Monitor is the honest primitive; the CAS choreography would be emulation
of a mechanism whose reason evaporated. (Precedent: `runtime_sema_impl.cs` made the same reduction
for the runtime semaphore — "a bucket is just a count plus a FIFO waiter queue.")

### 4.2 Completion delivery — two candidate mechanisms

The poller's one irreducible job on Windows: when the kernel completes an overlapped operation
that `internal/poll` submitted, set `ready` on the right desc's mode. Go does it by owning an IOCP
and draining it from the scheduler (`GetQueuedCompletionStatusEx` in `netpoll()`); the completion
routes back via *"the overlapped is the first field of `operation`"* pointer arithmetic
(`fd_windows.cs:72–74`, `runtime/windows/netpoll_windows.cs:58–66`). The managed replacement has
two candidate shapes:

**(a) `ThreadPoolBoundHandle` — the CLR's own IOCP (RECOMMENDED).** At `pollOpen`, bind the socket
handle (`ThreadPoolBoundHandle.BindHandle(SafeHandle)`); per operation record (§4.3), allocate the
NativeOverlapped from a reusable `PreAllocatedOverlapped` whose callback closes over the record →
desc → mode. The CLR's IO thread pool dequeues the completion and runs the callback with
`(errorCode, numBytes, NativeOverlapped*)`; the callback signals `ready`. This is *".NET's own
completion-port machinery"* in the board's words, literally: no poller thread of ours, no
shutdown/teardown story, no key-routing table addressed by raw pointers, AOT-compatible, and the
same engine .NET's own `Socket` rides. `runtime_pollServerInit` reduces to state initialization;
`runtime_isPollServerDescriptor` returns `false` for every fd — there IS no exposed poll-server
descriptor (the contract's only consumer is the test-only `IsPollDescriptor`; a Go program
cannot legitimately hold the poller's fd on Windows anyway).

**(b) Own IOCP + a dedicated managed poller thread.** `runtime_pollServerInit` creates the port
(the converted `syscall.CreateIoCompletionPort` wrapper works — `zsyscall_windows.cs:602`);
`pollOpen` associates with a completion key; a background thread blocks in
`GetQueuedCompletionStatus(Ex)` and routes by key. Closer to Go's shape;
`isPollServerDescriptor` gets a real answer. Costs: a thread whose lifecycle nobody owns (host
shutdown, test-host isolation), a NativeOverlapped-to-record routing table keyed by raw native
addresses, and hand-rolled dequeue marshalling — all machinery (a) gets from the CLR for free.

Recommendation: **(a)**, with (b) documented as the fallback if a real `BindHandle` constraint
surfaces (e.g., a handle the CLR refuses to bind). Nothing in the contract layer changes between
them — the choice is confined to `pollServerInit`/`pollOpen` and the callback's plumbing — so a
later swap is not a redesign. **OQ1.**

> **RESOLVED at S1 (2026-08-13): mechanism (a) holds.** `ThreadPoolBoundHandle.BindHandle` accepts a
> Go-created socket handle against a real kernel — `net.Listen` completes and `NetListenSmoke` matches
> `go run` byte for byte. The fallback to (b) is not needed.
>
> **AMENDED at S2 (2026-08-14): the bind moves from `pollOpen` to the FIRST SUBMIT (lazy), inside the
> §4.3 record machinery.** This is the plumbing latitude this paragraph already grants ("confined to
> `pollServerInit`/`pollOpen` and the callback's plumbing"), taken for a reason S1 measured rather than
> a preference. Go's poller REJECTS completions it does not own: `pollOperationFromOverlappedEntry`
> (`runtime/netpoll_windows.go`) checks the completion key against the `pollDesc` pointer packed into
> it and returns nil on mismatch, citing go.dev/issue/58870 — the issue `internal/poll`'s own
> `TestWSASocketConflict` regression-guards. The CLR's `ThreadPoolBoundHandle` offers no equivalent:
> its callback resolves state FROM the `NativeOverlapped` it allocated, so a foreign overlapped
> arriving at that port is MISREAD rather than ignored.
>
> Binding at `pollOpen` therefore made every pollable socket eligible to receive a foreign completion,
> and `internal/poll` has a live path that produces one: `FD.WSAIoctl`
> (`windows/sockopt_windows.cs:11`) bypasses `execIO` entirely and hands the kernel the CALLER's
> `syscall.Overlapped` — which, being an all-scalar struct in a STANDARD box, really does pin and
> really does reach the kernel. Binding lazily removes the hazard structurally instead of detecting it:
> a socket that only ever sees foreign overlapped IO is never associated with the CLR's port, so its
> completions signal the caller's own event exactly as on an unregistered socket, and a socket doing
> our IO is bound at its first submit, after which every operation on it carries a CLR-allocated
> overlapped. The residual — one socket doing BOTH — is not reachable in the corpus: a census of every
> `FD.WSAIoctl` caller (`net/windows/fd_windows.cs:177` SIO_TCP_INITIAL_RTO,
> `net/windows/tcpsockopt_windows.cs:124` SIO_KEEPALIVE_VALS) shows both pass a **nil** overlapped, so
> no production path issues an asynchronous foreign operation at all.

One converted behavior interacts here and is kept: `FD.Init` enables
`SetFileCompletionNotificationModes(FILE_SKIP_COMPLETION_PORT_ON_SUCCESS)` for TCP/UDP when safe
and sets `skipSyncNotif` (`fd_windows.cs:333–345`), so synchronously-completing operations post
NO completion packet and `execIO` returns without waiting (`:171–177`). Under (a) that is
supported (it is how .NET's own sockets run) but obligates the record lifecycle to a
"submit may retire with no callback" path — `FreeNativeOverlapped` on the sync-return branch,
re-arm on the next submit. The smaller-state-space alternative — suppress the mode and let every
completion post — is **OQ5**.

### 4.3 The submit seam — the second wall, and the WSA mirror family

Making `pollWait` wake up is HALF the arc. The other half is that the overlapped submissions
`execIO` issues must actually reach the kernel and complete into memory the managed side can
read. Today they cannot, and the board's syscall STRUCT-PASSING census
(`BOARD-next-validation-candidates.md` §"Open — the syscall STRUCT-PASSING seam") predicted
exactly this: `net` is the package that forces the remaining members. Async adds a dimension the
census's synchronous members never had. Three distinct sub-walls, priced separately:

**(1) The native-layout wall (the established class, new members).** `syscall.WSABuf` is
`{uint32 Len; ж<byte> Buf}` (`types_windows.cs:555–558`) — a managed reference where native
`WSABUF` wants a raw `CHAR*`. Every submit lambda passes `&o.buf` / `&o.bufs[0]` / `&o.msg`
(`WSAMsg` — worse: it embeds a `ж<WSABuf>` Buffers pointer and a control buffer). Same class as
`Timezoneinformation`/`win32finddata1`/`RawSockaddrInet4`, same remedy: blittable
`[StructLayout(Sequential)]` mirrors with an explicit field-for-field copy at the boundary.
`AcceptEx`'s output buffer is the class's decode-side: `acceptOne` hands it a
`slice<RawSockaddrAny>` reinterpreted as bytes (`fd_windows.cs:1041`), whose managed layout cannot
hold the native sockaddr block — the mirror must be a NATIVE staging buffer, decoded at harvest
with the L10 mirror helpers (`syscall/windows/syscall_windows_impl.cs` already owns the
`RawSockaddrInet4/6` ↔ native translation both directions).

**(2) The lifetime wall (what async ADDS to the class).** The census's fixed members marshal with
a mirror that is *"a LOCAL at the call site… trivially stable for exactly that long"*
(`syscall_windows_impl.cs` header). An overlapped operation breaks that premise twice over:

- The kernel retains the OVERLAPPED pointer and the buffer pointers until COMPLETION — seconds,
  minutes, unbounded. golib's address model is explicit about what it can hold still
  (`ж.cs`, `EnsureStableAddress` remarks): a standard box of unmanaged `T` pins its value slot; an
  array/slice-element box pins the CANONICAL BACKING ARRAY (aliasing — the kernel writes the real
  bytes); but an interior field address inside a reference-bearing container — which is exactly
  what `&o.o` is, an `Overlapped` field inside `operation` inside `FD` inside a heap-boxed
  `netFD` — *"is left exactly as it was — a transient address"*, because `GCHandle` cannot pin an
  object containing references. Handing the kernel a transient interior address for an async op is
  the pipe-EOF defect (`ж.cs`'s own war story) with an unbounded window: heap corruption by
  design.
- The OVERLAPPED is the operation's kernel-side IDENTITY. `execIO` names the SAME `&o.o` in three
  separate wrapper calls — submit, `CancelIoEx` (`fd_windows.cs:212`), harvest
  (`WSAGetOverlappedResult`, `:220`) — and cancellation matches BY ADDRESS. Any scheme that
  produces a fresh native copy per call (the natural extension of the local-mirror pattern) breaks
  cancellation outright: `CancelIoEx` would target an address the kernel never saw.

**The remedy: a per-(FD, mode) operation RECORD owning native-lifetime state.** The hand-owned
WSA wrappers keep a table `ж<Overlapped> → OpRecord`. The key works because golib pointer equality
for field-reference boxes compares (source box, field identity) — `ж.cs:63–70` documents that this
exact property exists to serve *"the address-keyed runtime semaphores in the hand-owned
sync/internal-poll implementations"* — and each FD has exactly one read op (`rop`) and one write op
(`wop`) for its lifetime (`fd_windows.cs:242–244, 361–366`), so `&o.o` minted at any call site of
either resolves to the same record. The record owns: the `PreAllocatedOverlapped`/NativeOverlapped
(mechanism (a)) or a `NativeMemory.Alloc`'d OVERLAPPED (mechanism (b)); the native `WSABUF`
array/`WSAMSG`/sockaddr staging blocks; the pins (`ж<byte>` element boxes hold their backing-array
pins for the BOX's lifetime, and the record holds the boxes, covering the op's whole flight); and
the completion results (`errorCode`, `qty`) the callback deposits. `WSAGetOverlappedResult`'s
hand-own answers from the record; `CancelIoEx`'s hand-own targets the record's one true native
address. Out-parameters (`&o.qty`, `&o.flags`, `rsan`) are marshalled through call-local natives
and copied back after the call — never through interior pins — per the mirror-is-a-local doctrine,
which async does NOT break for out-params (they are written only during the synchronous portion
or at harvest, both call-bounded).

**(3) The displacement mechanism.** The wrappers to hand-own are converter-generated with real
bodies (`zsyscall_windows.cs:1516` for `WSARecv`, etc.), so unlike the ten bodyless stubs they
must be DISPLACED, not merely supplied. The established mechanism is `manualConversionFuncs`
(`src/go2cs/manualTypeOperations.go:92`, `goosWindows`-scoped) — a data-only converter map entry
per function that turns the generated body into a placeholder, with the hand-own in an `_impl.cs`
beside it; the sockaddr family (`:499–508`) is the freshest precedent. The affected converted
files regenerate once (the A/B footprint is exactly those files). **OQ2** confirms the mechanism;
**OQ3** confirms the staged scope:

| Stage | Wrappers (owning package) | Unlocks |
|---|---|---|
| S1–S2 (TCP core) | `WSARecv`, `WSASend`, `AcceptEx`, `GetAcceptExSockaddrs`, `ConnectEx`, `CancelIoEx` (`syscall`); `WSAGetOverlappedResult` (`internal/syscall/windows`, wrapper at `windows/zsyscall_windows.cs:521`) | listen/accept/dial/read/write/deadlines — every TCP consumer row |
| S3 (UDP) | `WSARecvFrom`, `WSASendto` (`syscall`); `WSASendtoInet4/6`, `WSARecvMsg`, `WSASendMsg` (`internal/syscall/windows`) | UDP-shaped suites, `ReadFrom`/`WriteTo`/`ReadMsg` paths |
| deferred until reached | `TransmitFile` (`sendfile_windows.cs:71`) | `net`'s sendfile fast path (has a non-sendfile fallback) |

Per the board's standing ruling for this class — *"Do them when a suite reaches them, not
speculatively"* — nothing outside a stage's gate lands with that stage. `LoadConnectEx`'s
`WSAIoctl` function-pointer lookup and the socket-creation path (`WSASocket`, `bind`, `listen`,
`setsockopt`) are synchronous and already work under the existing dispatcher + L10 mirrors; they
are NOT part of this arc's surface.

### 4.4 The submit seam, specified — findings from S2a that the implementation should not re-derive

Everything below was measured or read out of the corpus while landing S1/S2a. It is recorded because
each item cost a non-obvious investigation and each one constrains the implementation.

**The reference graph forces a PUSH, and golib is the only place the table can live.** The waiter is
`internal/poll`; the submissions are in `syscall` and `internal/syscall/windows`; `internal/poll`
REFERENCES both, so a completion callback cannot call back into the poller. The three candidate
resolutions and why two lose: a `public` seam on `syscall_package` would add a non-Go symbol to a
PUBLISHED package's API surface; reading the `operation` back from the overlapped (Go's own trick —
`pollOperationFromOverlappedEntry` casts the OVERLAPPED to the enclosing struct) is impossible here
because `ж<T>`'s `m_structFieldRef` is private with no accessor, so a field-reference box cannot be
walked back to its source object. What remains is a descriptor-keyed callback table in **golib**, the
one assembly both sides see. Keep it platform-neutral to belong there: `nuint handle → Action<nint
mode>`, plus an opaque `object` slot for the submitting package's per-descriptor state (naming
`ThreadPoolBoundHandle` in golib would drag Windows into it). The descriptor is the right key because
it is the one identity both sides independently hold — the poller gets it in `pollOpen`, a wrapper
gets it as its own first argument.

**The record key is verified, not assumed.** `ConcurrentDictionary<ж<Overlapped>, OpRecord>` is sound:
`ж<T>.Equals` compares `SameSource(source1, source2) && fieldId1.Equals(fieldId2)` for a struct-field
reference, and `GetHashCode` returns `SourceIdentityHash(source)` — coarser than Equals (every field of
one `operation` hashes alike) but consistent with it, which is all a dictionary requires. So
`Ꮡo.of(operation.Ꮡo)` minted at `execIO`'s three separate call sites — submit, `CancelIoEx`, harvest —
resolves to ONE record. `ж.cs`'s own comment says this property exists to serve "the address-keyed
runtime semaphores in the hand-owned sync/internal-poll implementations", so the arc is reusing a
guarantee the corpus already depends on rather than leaning on an accident.

**Why `&o.o` genuinely cannot be handed to the kernel, confirmed at the source.** `Overlapped` is
all-scalar (`Internal`, `InternalHigh`, `Offset`, `OffsetHigh`, `HEvent`) and so is blittable on its
own — but that is not what decides it. `EnsureStableAddress` pins `PinnableStorage`, and for a
struct-field reference that recurses to the CONTAINER's storage: the `ж<operation>` box's `m_slot`,
which is **null**, because `operation` holds managed references (`ж<FD> fd`, `WSABuf.Buf`,
`slice<WSABuf> bufs`, `ж<RawSockaddrAny> rsa`) and the value constructor allocates a pinnable slot only
for a `T` free of them. So the address is transient exactly as §4.3 claims. Note the contrast that makes
the `FD.WSAIoctl` hazard real: a caller's own `var ov syscall.Overlapped` is a STANDARD box of a
blittable `T`, so it *does* get a slot, *does* pin, and *does* reach the kernel.

**Wrapper inventory, corrected.** `ConnectEx` is **already hand-owned** by the L10 sockaddr lane
(`syscall/windows/syscall_windows_impl.cs:363`, placeholder at `syscall_windows.cs:1110`) and today
passes `Ꮡoverlapped` straight through to the generated `connectEx` — it must be EXTENDED to use the
record's native overlapped, not newly displaced, and it needs no new `manualConversionFuncs` entry.
The genuinely new entries are `WSARecv`, `WSASend`, `AcceptEx`, `GetAcceptExSockaddrs`, `CancelIoEx`
(all `"syscall"`, `goosWindows`) and `WSAGetOverlappedResult` (a new `"internal/syscall/windows"` key).
All six generated bodies share one shape — marshal through `Syscall`/`Syscall9`, test `r1` against
`socket_error` (or `0` for the BOOL-returning ones), `errnoErr(e1)` — so the hand-owns should reproduce
that error handling verbatim rather than inventing one.

**Mode is known from the wrapper, not from the operation.** The callback must name a mode, and the
record cannot read `operation.mode` (previous point). It does not need to: `WSARecv`/`WSARecvFrom`/
`AcceptEx`/`WSARecvMsg` are always the READ operation and `WSASend`/`WSASendto`/`ConnectEx`/
`WSASendMsg` always the WRITE one, because each `FD` has exactly one `rop` and one `wop` for its
lifetime. The wrapper knows which it is by being itself.

**Where the bind goes.** First submit, inside the record machinery, per the §4.2 amendment — not
`pollOpen`. `AllocateNativeOverlapped` must come from that same binding, and `pollClose` disposes it
through the golib table's opaque slot before `internal/poll` closes the socket.

**`AllowUnsafeBlocks` splits between the two displacement packages, and the difference is owed work.**
`syscall.csproj` already emits `true`, so its mirrors and `NativeOverlapped*` need no marker.
`internal/syscall/windows` emits **`false`**, so its `WSAGetOverlappedResult` hand-own must carry
`[module: go.GoRequiresUnsafe]` if it touches a pointer type — and the regenerated `.csproj` flipping
to `true` is then part of that stage's intended A/B footprint, not drift. (The marker scan reads the
package directory plus its per-GOOS folders, so a hand-own in `windows/` is seen; see
*A hand-owned file can declare that it needs `/unsafe`* in ConversionStrategies-Reference.)

## 5. The deadline/unblock story — the hard part, priced honestly

This section is the reason the board said "a deadline/unblock story to settle, not a wrapper
repair." The semantics to reproduce, each read out of `runtime/netpoll.cs`:

1. **`d` is a relative ns duration** (`setDeadlineImpl` computes `time.Until(t)`,
   `fd_poll_runtime.cs:170`): `d > 0` arm; `d == 0` NO deadline (clear); `d < 0` already expired
   (`setDeadlineImpl` normalizes an exactly-now deadline to `-1`, `:171–173`). Go adds `nanotime()`
   and clamps overflow to `int64.max` (`netpoll.cs:395–402`); the managed mirror clamps the
   relative due-time to `Timer`'s ~49.7-day ceiling and re-arms on fire under a generation check —
   an honesty note, not a behavior change (Go's ceiling is ~292 years; both are "never" for a
   socket deadline).
2. **`mode` is `'r'`, `'w'`, or `'r'+'w'`** — the combined form sets BOTH deadlines (`:403–408`).
   Go's single-combo-timer optimization (`:410–414`) is NOT reproduced: two timers with the same
   due time are observationally equivalent, and the combo machinery (rtf selection,
   `netpollDeadline` firing both modes) exists to save a runtime timer, a resource the managed
   side is not short of.
3. **Expiry is STICKY per mode.** On fire, `rd/wd = -1` → the info bit → every subsequent
   `prepare`/`wait` in that mode returns `pollErrTimeout` (`netpolldeadlineimpl` `:656–698`,
   `netpollcheckerr` `:544–546`) until a LATER `SetDeadline` call rewrites that mode's deadline —
   to zero (clears), to the future (re-arms), or to the past (re-expires). Managed: the `expired`
   flag, cleared/re-set on every `pollSetDeadline` for the modes it names. `net`'s
   deadline-dependent tests assert this sticky-until-reset shape; getting it wrong reads as
   "connection permanently broken after one timeout" or as "timeout not sticky" — both
   behaviorally loud.
4. **A deadline set in the past fires NOW, against the CURRENT waiter** (`:448–458`): after
   updating state, wake the blocked mode(s) without setting `ready`. The waiter's loop re-checks
   and returns `pollErrTimeout`; `execIO` then runs the cancellation path. Same wake-without-ready
   rule for `pollUnblock` (`:473–505`).
5. **Stale timer callbacks must be inert.** Go guards with `rseq`/`wseq` — bumped on every
   deadline change and unblock — checked by the fired callback under the pd lock (`:425, 481,
   660–670`). The managed mirror is FORCED into the same shape by .NET semantics:
   `Timer.Change`/`Dispose` do not synchronize with an in-flight callback, so the callback
   re-validates its captured generation under the desc lock and returns if it lost. This is not
   optional hardening; without it, a reset deadline can expire a fresh one.
6. **Expiry does not abandon the in-flight operation.** After a timeout wake, `execIO` still owns
   a kernel-pending overlapped op: it issues `CancelIoEx` and then `waitCanceled` — which waits for
   completion-readiness ONLY, ignoring the very timeout that woke it (`netpollblock(waitio: true)`,
   `:377–382, 604`). Liveness holds because the kernel ALWAYS posts a completion for a cancelled
   overlapped operation (with `ERROR_OPERATION_ABORTED`), and if the op won the race and completed
   first, `CancelIoEx` returns `ERROR_NOT_FOUND` and the completion is already in flight
   (`execIO` handles both, `fd_windows.cs:212–231`). Under mechanism (a) both arrive through the
   CLR callback → `ready` → `waitCanceled` returns. The one configuration that would BREAK
   liveness is an operation whose completion packet was SKIPPED (`skipSyncNotif`) reaching the
   cancel path — it cannot: skip-on-success suppresses the packet only for a submit that returned
   synchronous success, and that path returns from `execIO` before any `wait`
   (`fd_windows.cs:171–177`); every path that CAN reach `wait` — `ERROR_IO_PENDING`, or sync
   success with the notification mode NOT skipped — has a completion packet guaranteed in flight.
   This invariant gets a dedicated test in the S2 matrix.
7. **Check order is fixed**: closing > timeout (> eventErr, unix-only) — `netpollcheckerr`
   `:539–554` — and **readiness-consumption beats both on entry** (`:585–589`): a completion that
   raced the deadline is still delivered to the caller, matching Go's preference for returning
   real IO over a same-instant timeout.

**Why not `CancellationToken`.** The task's framing asks this to be priced explicitly. A
CancellationTokenSource models a one-shot cancellation of a linked operation; Go's deadline is
none of those things — it is (i) per-MODE, not per-operation; (ii) STICKY across future
operations until explicitly re-set (a fired CTS cannot be un-cancelled; a fresh CTS per op loses
the stickiness that lives BETWEEN ops); (iii) REPLACEABLE while an op is in flight
(`Timer.Change` ↔ `seq++`, without disturbing the op); and (iv) NON-ABANDONING — the timed-out op
must still be cancelled-and-HARVESTED by the same caller (point 6), where CTS-style composition
wants to throw/abandon at the wait site. Modeling all four with tokens reconstructs the
flags+timer state machine anyway, plus an allocation per operation and a linked-registration
lifetime problem. The recommendation (**OQ4**) is the direct map: `System.Threading.Timer` +
sticky flags + generations under the desc lock — ~5 state fields and 2 timers per desc, with the
complexity concentrated where it genuinely lives: the interleavings. The S2 gate matrix (§7)
enumerates them adversarially rather than hoping.

**The priced risk.** The state machine is small; the race surface is not: completion vs timeout vs
`CancelIoEx` vs `Close`, times `skipSyncNotif`, times deadline-replaced-mid-wait. The budget for
this arc should assume the deadline matrix — not the happy-path round trip — is where the
iteration goes. The mitigations built into the design: one lock (no lock-free interleavings to
reason about), single-waiter-per-mode (no queue/fairness dimension), generation checks forced by
.NET timer semantics, and the wake-vs-ready separation lifted intact from Go.

## 6. Blast radius — what stays converted, what becomes hand-owned

**Stays converted, byte-for-byte (the point of the seam):**

- `internal/poll/fd_poll_runtime.cs` — every caller of the ten, `convertErr`, the SetDeadline
  plumbing. Untouched.
- `internal/poll/windows/fd_windows.cs` — `execIO`, `FD`, all ~20 exported IO methods, `Init`'s
  `skipSyncNotif` logic. Untouched. (This is the design's central economy: the alternative of
  hand-owning `FD` against `System.Net.Sockets.Socket` rewrites ~1,500 lines of it, forfeits
  `SyscallConn`/`RawConn` fidelity, and diverges every option/half-close/dual-stack behavior
  `net`'s tests measure. Rejected.)
- `internal/poll`'s other files, `net` entire (its one adjacent stub noted below), `os` entire
  (files are never pollable — `pollable: false` at `os/windows/file_windows.cs:83`, so
  `runtimeCtx == 0` short-circuits every pd call, `fd_poll_runtime.cs:57–117`).
- `runtime` entire — `netpoll.cs`, `windows/netpoll_windows.cs` stay converted-and-dead, per the
  doctrine's "becomes unreachable" clause. Zero runtime edits.

**New hand-owned surface:**

| Artifact | Kind | Census effect |
|---|---|---|
| `internal/poll/windows/runtime_netpoll_impl.cs` — the ten bodies + `ManagedPollDesc` + delivery | new `_impl.cs`, `[module: go.GoManualConversion]`, no `.go` counterpart (never regenerated — the `runtime_sema_impl.cs` shape) | +1 marked file |
| `syscall/windows/zsyscall_windows_wsa_impl.cs` (name per OQ6) — WSA family mirrors + op records | new `_impl.cs` + `manualConversionFuncs` entries under `"syscall"` (`goosWindows`) | +1 marked file; displaced wrappers regenerate as placeholders |
| `internal/syscall/windows/windows/zsyscall_windows_wsa_impl.cs` — `WSAGetOverlappedResult` (+S3: `WSARecvMsg`/`WSASendMsg`/`WSASendtoInet4/6`) | same mechanism, new `"internal/syscall/windows"` key in `manualConversionFuncs` | +1 marked file |

Converter change: `manualConversionFuncs` map entries ONLY — data, not logic; no new converter
`.go` file (no `projitems` entry owed); `go test ./...` and CNR classify the one-time regen of the
displaced wrapper files as the change's intended A/B footprint. The hand-own census GROWS —
re-measure at the regen per the ritual, never carry forward (CLAUDE.md, corpus mechanics §1).

**Adjacent walls this design deliberately does NOT claim** (so nobody reads "netpoll landed" as
"net validates"): `net`'s own `runtime_rand` bodyless stub (`net/dnsclient.cs:20`), the DNS
resolver stack, `GetIfEntry`/`FreeAddrInfoW` (both still on the struct-passing census —
`net.Interfaces`, DNS), and `crypto/x509`'s cert-store members. Each is a later, smaller arc with
this design's machinery as precedent.

## 7. Gates and staged landing

Stage discipline per the repo's standing rules: behavioral guards compare REAL VALUES against
`go run` (the `LocalTimeZone`/`SockaddrRoundTrip` doctrine — never absence-of-fault), outputs
deterministic (ephemeral ports and timings printed as derived invariants, never raw). Each stage
banks separately; a later stage blocked does not un-bank an earlier one.

**S0 — mechanism in place, nothing reaches it.** The three `_impl.cs` files + map entries +
displaced-wrapper regen land compiling. Gates: full behavioral suite (existing 528 outputs
unaffected — nothing exercises sockets today); `check-no-regression` clean EXCEPT the enumerated
displaced-wrapper files (the intended footprint, named in the commit); converter `go test ./...`;
`go2cs.slnx` + `go2cs-stdlib.slnx` build; filtered sweeps over the packages whose closure touches
the displaced files — **`syscall` 62/62 must hold** (the banked row most exposed), plus `os`,
`path/filepath`, `time` spot checks (their syscalls ride the untouched dispatcher, but the sweep
is cheap and the claim should be measured, not argued).

**S1 — `net.Listen` smoke.** New behavioral test `NetListenSmoke`: `Listen("tcp",
"127.0.0.1:0")`, assert/print invariants (addr network, port > 0, distinct second listener,
`Close`, listen-after-close on the same port), byte-compared against `go run`. Exercises contracts
1, 2, 3, 8, 9 with zero data flow. This is the first observable retreat of the wall: the board's
`internal/poll` row stops dying in `pollServerInit`.

**S2 — the round trip + the deadline matrix (the arc's real gate).** Two behavioral tests:

- `TcpLoopbackRoundTrip` — the test L10's spec commissioned and the netpoll wall blocked:
  listen → dial → accept → write → read → echo → close, both directions, IPv4 + IPv6. Exercises
  contracts 4, 5, 6 end-to-end plus `AcceptEx`/`ConnectEx`/`WSARecv`/`WSASend` mirrors.
- `NetDeadlineMatrix` — the §5 interleavings, adversarially: read blocks then deadline fires
  (`os.ErrDeadlineExceeded` surfaced through `net.Conn`); sticky (second read fails instantly);
  cleared by `SetReadDeadline(zero)` then succeeds; deadline-in-past fails without blocking;
  deadline REPLACED while blocked (old never fires, new does); write-mode independence;
  `'r'+'w'` combined; `Close` from another goroutine unblocks a blocked read with the closing
  error; completion-races-deadline delivers the data (point 7); the pending-op cancel/harvest
  invariant (point 6).

> **⚠ BLOCKER (S2b lane, measured 2026-08-14): `TcpLoopbackRoundTrip` is UNREACHABLE until the
> sockaddr DECODE is fixed, and it blocks the seam's most dangerous surface from value-level
> verification.** `net`'s accept path calls `RawSockaddrAny.Sockaddr()` on the `GetAcceptExSockaddrs`
> output (`net/windows/fd_windows.cs:255–256`), and that decode still carries the port ALIAS L10
> hand-owned away on the ENCODE side — `var p = (ж<array<byte>>)(uintptr)(new
> @unsafe.Pointer(pp.of(RawSockaddrInet4.ᏑPort)))` (`syscall/windows/syscall_windows.cs:953`). An
> `array<T>` rebuilt from a raw address materializes length ZERO, so `p[0]` panics. Measured directly
> rather than inferred, with a throwaway probe that constructs an `AF_INET` `RawSockaddrAny` and calls
> `Sockaddr()`: Go answers `decoded AF_INET port=0 addr=[0 0 0 0]`; C# answers
> `panic: runtime error: index out of range [0] with length 0`.
>
> L10 left this auto-converted **deliberately and correctly**: hand-owning it drops the three
> `[assembly: GoImplement<Sockaddr{Inet4,Inet6,Unix}, ΔSockaddr>(Pointer = true)]` records
> (`syscall/windows/package_info.cs:45–47`) that its body's casts are the only witness for, and a
> MEASURED reconvert of `net` against the shortened `package_info` showed `net` minting duplicate
> adapters — the second-identity regression. The real answer L10 named is the converter's POINTER
> method-set recording, and that is **still deferred**: `samePackageImplements.go:68` states the
> pointer set "is owed its own increment with its own measured footprint" (548 same-package pairs vs
> 168 for the value set). So the fix is a converter increment, not a hand-own, and not this arc's.
>
> **What survives, precisely.** A census of every `.Sockaddr()` call site in `net` finds six: the two
> accept-path sites above, three in `interface_windows.cs` (`net.Interfaces`) and one in
> `dnsconfig_windows.cs` — the last four already named as adjacent walls in §6. **The DIAL path never
> touches it**, because `netFD`'s local/peer addresses come from `Getsockname`/`Getpeername`, which
> L10 hand-owned to decode natively. So a dialed client conn is fully usable (the kernel completes the
> handshake from the listen backlog with no accept on the other end — the shape `SockaddrRoundTrip`
> already relies on), which makes `NetDeadlineMatrix` achievable in full and exercises five of the six
> wrappers; `AcceptEx` is reachable too, but only up to a DEADLINE-cancelled accept, never a
> successful one.
>
> That is deliberately **not** treated as good enough to land the seam on. What no dial-only gate can
> prove is that bytes arrive correctly — the native `WSABUF` mirroring, the pinned user buffer and the
> transferred counts are exactly the "returns garbage without crashing" class the repo's standing rule
> covers: *verify at VALUE level, never at fault level*. A submit seam whose buffer marshalling is
> unproven is the wrong thing to bank, which is why this lane stops at the boundary rather than
> landing ~530 lines against a gate it knows it cannot meet.

Gates: full behavioral suite; then the pipeline's own measure — **filtered sweep of
`internal/poll`: the board row's target is 19/19** (from 18/19, sole miss `runtime_pollServerInit`
— the row this design exists to close).

> **⚠ FINDING (implementation lane, 2026-08-13, measured at S1): that gate has an unrecorded
> PREREQUISITE — `internal/poll`'s converted test host does not currently BUILD, so the row is not
> measurable at any value, before or after this arc.** Measuring it at S1 (regression diligence: S1
> lets `TestWSASocketConflict` run past the `fd.Init` that used to kill it, so "further than it has
> ever run" is where a new hang would live) produced a C# compile error rather than a verdict:
> ```
> export_test.cs(13,62): error CS0123: No overload for 'consume' matches
>                        delegate 'Action<ж<slice<slice<byte>>>, long>'
> ```
> `export_test.go`'s `var Consume = consume` is a func VALUE of a function whose `*[][]byte`
> parameter the production emission now lowers to a C# `ref` (`consume(ref slice<slice<byte>> v,
> int64 n)`, `fd.cs:92`), while the test file still spells the delegate in the `ж<T>` box form. A
> `ref`-taking method cannot bind to that delegate. Both files are ordinary converted output that
> this arc does not touch — the netpoll hand-own supplies partial BODIES for the ten `runtime_poll*`
> methods and cannot alter `consume`'s signature — so this is the ref-lowering arc meeting
> func-value conversion, and it is **not this arc's to fix** (recorded and handed to the coordinator
> rather than reached across, per OQ6's ownership line). The board's "18 of 19" reading therefore
> predates the ref-lowering landing and should be treated as stale until the host compiles again.
>
> **ATTRIBUTED AND CHARTERED ELSEWHERE (coordinator, 2026-08-14).** This CS0123 is the first real
> corpus witness of the ж-box arc's §3.5 **func-value adapter gap** — a Go func VALUE aliasing a
> ref-lowered function — which is precisely the evidence class A3 recorded as missing. It gets its
> own lane, and this note is where that lane starts. The netpoll arc does not fix it and does not
> wait for it: per the same ruling, S2 runs on its other gates and reports this row as
> **blocked-with-cause**, citing this error. When the lane lands, whoever re-measures should
> re-derive the target rather than expecting 19/19 — see the COM4 host-dependence below.
>
> Two facts worth carrying to whoever picks it up. The Go side of the same run is itself **not
> clean on every host**: `TestSerialFdsAreInitialised/COM4` fails wherever a real COM4 exists, so
> the differential's Go baseline is host-dependent and the target may not be a round 19/19 —
> re-derive it at measure time rather than treating a non-round result as a miss.
>
> The other fact this measurement was meant to probe — the `FD.WSAIoctl` foreign-overlapped hazard —
> is **closed structurally at S2a and no longer needs probing**: the CLR association moved from
> `pollOpen` to the first submit (§4.2's amendment), so a socket that only ever sees foreign
> overlapped IO is never bound at all. That is why the unmeasurable row costs this arc nothing it
> cannot recover: the one thing the row was uniquely positioned to catch has been removed rather than
> merely watched for.

**S3 — consumer re-measures (the board rows behind the netpoll wall).** The RESOLVED note freezes
the walled set and this design inherits it as its unlock ledger: `net/smtp` (9/14, five rows on
this exact stack — the first re-measure, per the L10 spec's consumer-proof pattern), then ONE of
the L9-held socket rows (`net/http/httptest` recommended there), then the remainder as breadth
lanes: `net/http/cgi` (36/39), `net/http/httputil`, `net/http/cookiejar`, `net/rpc`. UDP wrappers
land here gated by whichever suite reaches them first. `net` itself stays a FUTURE arc — its
suite needs the §6 adjacent walls (DNS, interfaces, `runtime_rand`) and its census should be
taken fresh when this machinery exists, not promised now.

## 8. Non-goals

- **No Linux/darwin poller.** The Linux corpus does not yet build (`DESIGN-multiplatform-corpus.md`
  §12), and `fd_unix.go` consumes these contracts in a READINESS model (wait-then-retry-syscall)
  that wants a different managed mechanism (epoll has no CLR surface; candidates are
  `Socket.Poll`-shaped or a native epoll thread — a separate design when it is real). The ten
  bodies land in `windows/`; other GOOS keep today's throwing stubs.
- **No scheduler-facing netpoll surface** — `netpoll(delta)`, `netpollBreak`, `netpollready`,
  `netpollAnyWaiters` have no caller in managed land and are not implemented (§3.2).
- **No performance targets.** Correctness-first per Phase-4 doctrine; the design avoids known
  cliffs (PreAllocatedOverlapped reuse, zero-copy pinned user buffers, no per-op allocation on
  the happy path) but no benchmark gates this arc. A perf pass belongs after the ж-box arc's
  instruments, if ever.
- **No `net` operational campaign** — §6's adjacent-walls list is the boundary of the claim.
- **No os-file async IO** — files stay non-pollable, exactly as Go 1.23 has them on Windows.

## 9. Open questions — RULED (coordinator, 2026-08-13)

> **All eight recommendations are RATIFIED as written.** The arc is chartered; §4.2's
> `ThreadPoolBoundHandle` plumbing, §4.3.3's displacement mechanism and staging, §5's deadline
> semantics list, and §6's file placement are now the implementation contract. One clause stays
> live by design: OQ3's UDP-fold — if the S2 sweep shows UDP-shaped `internal/poll` tests failing
> on the stubs, UDP folds into S2 without re-ruling. Each item below retains its original
> recommendation text as the record of what was ratified and why.

- **OQ1 — Completion delivery mechanism** (§4.2): `ThreadPoolBoundHandle` (recommended) vs own
  IOCP + poller thread. The contract layer is identical under both; ruling picks the plumbing and
  the `isPollServerDescriptor` answer (`false` vs real handle).
- **OQ2 — Wrapper displacement mechanism** (§4.3.3): `manualConversionFuncs` entries + `_impl.cs`
  (recommended, the sockaddr precedent) vs whole-file hand-own of the generated `zsyscall`
  files (rejected here: freezing ~1,900 generated lines to own ~10 functions is rot the marker
  census then has to carry forever).
- **OQ3 — Stage scope of the WSA family** (§4.3.3 table): TCP-core seven wrappers for S1–S2, UDP
  five at S3, `TransmitFile` deferred-until-reached. Confirm the split — it is the
  do-when-reached ruling applied, but it leaves `ReadFrom`/`WriteTo` stubbed for one stage on a
  package (`internal/poll`) whose sweep S2 gates, so the S2 19/19 target implicitly assumes the
  suite's UDP-shaped tests are either absent or already-failing-for-other-reasons; if the S2
  sweep says otherwise, UDP folds into S2.
- **OQ4 — Deadline machinery** (§5): `System.Threading.Timer` + sticky flags + generations under
  the desc lock (recommended) vs CancellationToken composition (rejected with the four-point
  analysis). Ratifying this ratifies the §5 semantics list as the implementation contract.
- **OQ5 — `skipSyncNotif`** (§4.2): keep Go's skip-completion-on-success (recommended;
  Go-faithful, and the record lifecycle handles the no-callback retire) vs suppressing the mode
  for a smaller state space at a small syscall-per-op cost. A defect surfacing in the S2 matrix
  on the skip path is grounds to flip this without re-ruling the design.
- **OQ6 — File placement and naming** (§6 table): the three `_impl.cs` names/locations as
  proposed; specifically whether the poller core belongs in `internal/poll/windows/` (recommended
  — it is package-private contract surface, per-GOOS by nature) or in golib (rejected: golib is
  Go-semantics-generic; a Windows IOCP poller is neither).
- **OQ7 — Gate ownership at S3**: which consumer re-measures belong to THIS arc's bank versus
  fresh breadth lanes. Recommendation: this arc banks through the S2 gates + `internal/poll`
  19/19 + the `net/smtp` re-measure as its consumer proof; everything further is L-lane work
  against the then-current board.
- **OQ8 — The adjacent `net.runtime_rand` stub** (`net/dnsclient.cs:20`): flagged for the future
  `net` arc, one `runtimeNano`-class hand-own; NOT taken here. Confirm it stays out of scope so
  the S3 `net/smtp` re-measure attributes any DNS-path miss to the right wall.
