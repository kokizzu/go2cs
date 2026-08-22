# The Linux readiness poller — hand-owning the ten `runtime_poll*` contracts on epoll, for `fd_unix`'s readiness-model consumer

> **STATUS: DESIGN — PROPOSED (2026-08-22, lane R, `claude/linux-poller-design`). Nothing in this
> document is ratified.** Every decision below is a proposal with a recommendation; §10 collects the
> ones that need a coordinator ruling before an implementation lane starts. Commissioned by the
> coordinator on the mailbox (2026-08-22, to lane R at the close of the sockaddr-mirror lane): *"The
> readiness poller: commissioned as YOUR NEXT lane's deliverable — the design note, not the
> implementation … measured bill (which rows it flips — the socket family incl. encoding/json's HTTP
> test and crypto/tls's Linux leg), the polling-loop poll(2) shape vs alternatives priced, deadline
> semantics, the §8 non-goal boundary it inherits from the netpoll design, adversarial pass, OQs with
> recommendations, STATUS PROPOSED."* It is the half the Windows design deferred in its §8 (*"No
> Linux/darwin poller … a separate design when it is real"*); it is real now because the Linux corpus
> builds, 129 of its 161 roster rows validate, and the socket wall has been measured down to exactly
> one errno in exactly one file. Companions: [`DESIGN-netpoll-managed-poller.md`](DESIGN-netpoll-managed-poller.md)
> (the Windows poller — its §2 contract inventory and §5 deadline story are INHERITED here, not
> re-derived), `src/core/internal/poll/fd_poll_runtime.cs` (the ten contracts and every caller),
> `src/core/internal/poll/linux/fd_unix.cs` (the readiness-model consumer),
> `src/core/internal/poll/linux/runtime_netpoll_impl.cs` (today's fallback poller, which this design
> replaces IN PLACE), `src/core/internal/poll/windows/runtime_netpoll_impl.cs` (the managed descriptor
> state machine this design lifts), `src/core/runtime/linux/netpoll_epoll.cs` (Go's own epoll poller,
> converted and dead — the mechanism this design reproduces outside the scheduler),
> `src/core/syscall/linux/` (the keystone `syscall(2)` binding, the sockaddr mirror, the `struct stat`
> mirror — the address-model precedents), `src/core/os/linux/file_unix.cs` (who arms what). Written
> against the sockaddr/posmap lane's tip `95b16505d` (master `1d6542c73` + that lane; 2026-08-22),
> Go 1.23.1, with the three Linux lanes' measurements (poll-seam, three-bodies, sockaddr/posmap at
> `e7800600d`) as its bill.

---

## 0. In one paragraph

Since the poll-seam lane the Linux flavor has had *a poller that owns no poller*: every descriptor is
un-armable, files take Go's own blocking fallback (28 rows flipped on that alone), and a socket's
`FD.Init` returns `operation not permitted` — measured in the sockaddr lane as exactly where the wall
now stands. This note proposes the readiness poller that makes sockets, pipes, FIFOs and ttys
pollable on Linux: **one epoll instance, one background drain thread, and the Windows flavor's managed
descriptor state machine lifted verbatim** — Go's own `netpoll_epoll.go` mechanism
with the scheduler's `gopark`/`goready` replaced by a monitor gate per descriptor; edge-triggered
exactly as Go arms it; keyed by an opaque token rather than a pointer; every kernel-facing buffer a
stack or native image through the keystone `syscall(2)` binding (no ж address is ever handed to the
kernel); landing in the one file that answers EPERM today, with the Windows flavor untouched. The
`poll(2)`-per-waiter loop the board priced is kept as the named alternative and rejected on two
measured facts, not on taste: it needs a second descriptor per descriptor, and it cannot reproduce
Go's refusal of regular files without an `fstat` the kernel otherwise does for free. Deadlines are
§5 of the Windows design, unchanged. The bill: `encoding/json`'s last test and `crypto/tls`'s whole
Linux leg on the roster; the S3 socket ledger, three existing behavioral guards and `os`'s pipe
semantics off it.

## 1. The wall, measured — what stops a Linux socket today

### 1.1 The path and the errno

Measured on the Linux roster re-run at `e7800600d` (the sockaddr-mirror lane, 2026-08-22). With
`Bind`/`Connect` landing their native `sockaddr` images correctly, `net.Listen("tcp", "[::1]:0")`
walks:

```
net.Listen → listenStream → socket() OK → Bind OK
  → netFD.init                                    net/linux/fd_unix.cs:41   (pollable: true)
  → FD.Init(net, true)                            internal/poll/linux/fd_unix.cs:48-66
  → pollDesc.init → serverInit.Do(runtime_pollServerInit)   [no-op today]
  → runtime_pollOpen(fd) → (0, EPERM)             internal/poll/linux/runtime_netpoll_impl.cs
  → errnoErr(EPERM); FD.Init sets isBlocking = 1 AND RETURNS the error
  → net.netFD.init propagates → listenStream fails
  → net.Listen: "listen tcp6 [::1]:0: operation not permitted"
```

Exact strings from the run: `encoding/json` `TestHTTPDecoding` — `httptest: failed to listen on a
port: listen tcp6 [::1]:0: operation not permitted`; `crypto/tls` — package-level `operation not
permitted` from `TestMain`'s listener, so zero of its verdicts run. Both rows had died one seam
earlier in each prior lane: in `SockaddrInet4.sockaddr()`'s `(*[2]byte)` alias (R5, closed by the
sockaddr mirror) and before that in the `runtime_pollServerInit` throwing stub (W1, closed by the
fallback poller).

For a FILE the same errno is discarded. `os.newFile` (`os/linux/file_unix.cs:167-236`) marks
`kindOpenFile`/`kindPipe`/`kindSock`/already-nonblocking descriptors pollable, puts them in
`O_NONBLOCK`, calls `Init`, and on error restores blocking mode — Go's own comment: *"An error here
indicates a failure to register with the netpoll system. That can happen for a file descriptor that
is not supported by epoll/kqueue; for example, disk files on Linux systems. We assume that any real
error will show up in later I/O."* That asymmetry is why the fallback poller flipped 28 rows with one
errno, and it is why the poller is a SOCKET-side increment: **nothing in `os` or `net` changes**;
the same `Init` that fails today succeeds, and the same `os.newFile` that restores blocking mode
today leaves the descriptor non-blocking — for pipes, FIFOs and ttys as well as sockets, exactly the
set Go arms on Linux.

### 1.2 The bill — what the poller moves, row by row

| Row / guard | Today (Linux, measured 2026-08-22) | With the poller |
|:--|:--|:--|
| `encoding/json` | FAIL **490/491** — `TestHTTPDecoding` (an `httptest` loopback HTTP round trip) | flips PASS at the banked 491 — the one test is the Windows poller's bread and butter |
| `crypto/tls` | FAIL at package level (`TestMain`'s `net.Listen`); **0 of 3,646** Go-enumerated verdicts run | the whole suite runs. It validates at its LINUX count — a W4 per-OS-arithmetic row (Windows banks 400 + 2 disclosed) — and it is this design's real gate: the flagship networking suite over loopback TCP, deadlines, half-closes and handshake timeouts |
| `PipeCloseUnblocksRead` (behavioral) | prints `read did NOT unblock` — a blocking pipe (measured at the fallback) | prints Go's `read unblocked: read \|0: file already closed` — `os.Pipe` arms, `evict` wakes the blocked reader |
| `NetListenSmoke`, `NetDeadlineMatrix`, `TcpLoopbackRoundTrip` (behavioral — the Windows poller's own guards) | not runnable on Linux (no poller) | become the Linux poller's guards — contracts 1–10, the §5 deadline matrix, the loopback round trip — the moment the behavioral runner binds the linux flavor (§7, ⟨OQ-6⟩) |
| the S3 socket ledger, off-roster: `net/smtp` (9/14 on Windows), `net/http/httptest`, `net/http/cgi` (36/39), `net/http/httputil`, `net/http/cookiejar`, `net/rpc` | walled on Linux at `FD.Init` | reach their Windows state on Linux; each then carries its own Windows-measured residual (the Windows design's S3 ledger applies to both platforms) |
| `net` itself | FUTURE on both platforms (the Windows design §6's adjacent walls: DNS, interfaces, `runtime_rand`) | status unchanged. The Linux DNS path is `dnsclient_unix` over `/etc/resolv.conf`, a different wall from Windows' `GetAddrInfoW`; its census is taken when the poller exists, not promised here |
| `os` — pipes, FIFOs, ttys | blocking: `SetDeadline` → `ErrNoDeadline`, `Close` cannot cancel a `Read` blocked in `read(2)` | Go's Linux semantics: pollable, deadlines honored, `Close` unblocks (§5) |

What it does NOT move, so the board's arithmetic stays honest: **R2** (the exec wall, 16 rows — G's
ratified posix_spawn arc), **W1b** (mmap — golib's native-snapshot slice, its own design), **R6**
(`time`'s `ZONEINFO` test), **W2/W3/W4/W6/W7**; and any socket path that reaches
`Recvfrom`/`Sendto`/`Recvmsg`/`Sendmsg`, which the sockaddr mirror deliberately did not cover (UDP and
ancillary data — the next syscall seam, not a poller one). The on-roster flip count is therefore
small (two rows) and the verdict count is not (3,646 + 1): the poller is priced as the arc that
makes the Linux socket family MEASURABLE, which is the Windows design's own framing of its S1.

### 1.3 What the fallback poller already gives — and what it withholds

The fallback (`linux/runtime_netpoll_impl.cs`, poll-seam lane) answers un-armable for every
descriptor: `pollServerInit` no-op, `pollOpen → (0, EPERM)`, `isPollServerDescriptor → false`,
`runtimeNano` on `Stopwatch`, and six ctx-taking bodies that throw `Unreachable` because no ctx is
ever minted. It is Go's regular-file behavior applied to everything, which is correct for files and
a documented degradation for the rest: a pipe read blocks its goroutine's thread in `read(2)`,
`SetDeadline` answers `ErrNoDeadline`, and `Close` cannot cancel an in-flight read. The poller
REPLACES that file: the EPERM arm becomes what it is in Go — the answer for the descriptors epoll
refuses — and the six bodies become real.

## 2. The contract — inherited from the Windows design, plus what the readiness-model consumer changes

The ten `//go:linkname` contracts are the same ten on every GOOS (`fd_poll_runtime.cs:18-36`):
`runtimeNano`, `pollServerInit`, `pollOpen(fd) → (ctx, errno)`, `pollClose(ctx)`,
`pollWait(ctx, mode) → code`, `pollWaitCanceled(ctx, mode)`, `pollReset(ctx, mode) → code`,
`pollSetDeadline(ctx, d, mode)`, `pollUnblock(ctx)`, `isPollServerDescriptor(fd) → bool`; the four
codes are `0 = pollNoError`, `1 = pollErrClosing` (→ `errClosing(isFile)`), `2 = pollErrTimeout`
(→ `ErrDeadlineExceeded`), `3 = pollErrNotPollable` (→ `ErrNotPollable`), through `convertErr`
(`:129-146`); `mode` is `'r'`, `'w'` or `'r'+'w'`; `ctx == 0` means "not pollable" everywhere
(`pollable()`, `prepare`, `evict`, `wait`, `close` all test it first). The Windows design's §2
inventories every caller and §5 fixes the deadline semantics; neither is re-derived here. What the
LINUX consumer changes is the call SHAPE around the contracts, and five consequences follow from it (§2.1–§2.5):


### 2.1 Readiness, not completion

Every I/O wrapper in `linux/fd_unix.cs` is
`prepare → syscall → (EAGAIN && pollable ? wait → retry : return)` — fifteen sites (`Read` `:173`,
`Pread`, `ReadFrom`, `ReadFromInet4/6`, `ReadMsg*`, `Write` `:244`, `Pwrite`, `WriteTo*`,
`WriteMsg*`, `Writev`, `Accept` `:820`, plus `Splice`/`copy_file_range`'s own arms). So
`pollWait` is reached **only after the kernel has answered EAGAIN**: at the instant of the wait the
read buffer is EMPTY or the write buffer is FULL, and the listen queue is empty. That is the
invariant edge-triggered readiness rests on (§4.5), and it holds by construction of the consumer,
not by anything the poller does.

### 2.2 There is nothing to harvest after a timeout

On Windows a deadline wakes a thread that still
owns a kernel-pending overlapped op, hence `CancelIoEx` + `pollWaitCanceled` (Windows §5 point 6).
On Linux the woken thread owns nothing: it returns `pollErrTimeout`, the consumer returns
`ErrDeadlineExceeded`, and the next operation re-issues its own syscall. `pollWaitCanceled` has
**no caller** in `fd_unix.cs` (`pd.waitCanceled` exists only for `fd_windows.cs`'s `execIO`) —
⟨OQ-5⟩ decides whether its body is the shared wait loop or stays `Unreachable`.

### 2.3 Close ordering is Go's unix one, and it is what makes "unregister before close" free

`FD.Close` → `fdmu.increfAndClose()` → `pd.evict()` (= `pollUnblock`: closing, wake both modes)
→ `decref()` → on the last reference `destroy()` → `pd.close()` (= `pollClose`) → **then**
`CloseFunc(sysfd)` — `destroy`'s comment says it outright: *"Poller may want to unregister fd in
readiness notification mechanism, so this must be executed before CloseFunc."* So the poller
always sees `EPOLL_CTL_DEL` (or its equivalent) before `close(2)`, every waiter is woken by
`evict` before either, and `Close` itself parks on `csema` until the last reference drops unless
`isBlocking` (`fd_unix.cs:85-110`). No waiter is ever blocked in the kernel ON the descriptor
being closed — they are blocked on the managed gate — which is the property the `poll(2)`-per-waiter
shapes in §3 have to pay for separately.

### 2.4 `isPollServerDescriptor` has one real consumer, and it is in G's rows

`os/exec`'s
`TestExtraFiles` (`os/exec/exec_test.cs`) enumerates descriptors 3..101 and skips the ones
`poll.IsPollDescriptor` claims, so a poller that OWNS descriptors must answer truthfully for
them. The fallback's `false` is truthful only because it owns none; the epoll shape owns one (§4.2).

### 2.5 `SetBlocking` leaves the registration in place

`os.File.Fd()` calls `pfd.SetBlocking()`
before handing a descriptor out (historically blocking), which sets `isBlocking = 1` and clears
`O_NONBLOCK` but does not `evict`/`close` the desc — Go's own behavior. The poller may keep
receiving events for such a descriptor; they set `Ready` on a desc nobody waits on, which is
harmless, and `Close` then skips the `csema` park because `isBlocking` is set. §5 carries the
`os/exec` consequence.

Everything else — the sticky per-mode expiry, the generation discipline on `Timer` callbacks,
"a deadline set in the past fires NOW against the current waiter", readiness consumed ahead of the
error checks, check order closing > timeout > eventErr, wake-without-ready for unblock — is the
Windows design's §5, inherited verbatim (§4.7).

## 3. The mechanism space, priced

Five candidates reach a kernel readiness fact from the corpus as it stands. The keystone
`[LibraryImport("libc", "syscall")]` binding (`internal/runtime/syscall/linux/syscall_linux_impl.cs:95`)
reaches any of them by number: `SYS_POLL 7`, `SYS_PPOLL 271`, `SYS_EPOLL_CREATE1 291`,
`SYS_EPOLL_CTL 233`, `SYS_EPOLL_WAIT 232`, `SYS_EPOLL_PWAIT 281`, `SYS_EVENTFD2 290`
(`syscall/linux/zsysnum_linux_amd64.cs`); the constants `EPOLLIN 0x1`, `EPOLLOUT 0x4`, `EPOLLERR 0x8`,
`EPOLLHUP 0x10`, `EPOLLRDHUP 0x2000`, `EPOLLET 0x80000000`, `EPOLL_CLOEXEC 0x80000`,
`EPOLL_CTL_ADD/DEL/MOD 1/2/3`, `EFD_CLOEXEC 0x80000`, `O_NONBLOCK 0x800` are in the converted
`syscall`/`internal/runtime/syscall` flavors (a hand-own defines its own private copies either
way). No new P/Invoke, no golib change, under any candidate.

| | **A** — `poll(2)` per waiter + a per-desc eventfd for wake | **A′** — `poll(2)` per waiter in bounded slices (the board's sketch) | **B** — epoll + ONE drain thread + managed gates (Go's own `netpoll_epoll.go`, minus the scheduler and its break eventfd) | **C** — .NET `Socket`/`SafeSocketHandle` over the fd | **D** — wire the converted `runtime/linux/netpoll_epoll.cs` |
|:--|:--|:--|:--|:--|:--|
| Who blocks, where | the waiting goroutine's thread, in `poll(2)` on `{fd, efd}` | same, in `poll(2)` on `{fd}` with a slice timeout | the waiting goroutine's thread in `Monitor.Wait`; one background thread in `epoll_wait` | .NET's private `SocketAsyncEngine` epoll thread | the Go scheduler's `findrunnable`/`sysmon` — which does not exist under the CLR |
| Descriptors owned by the poller | **+1 per desc** (an eventfd each) | none | **1 per process** (the epfd; Go's break eventfd is not needed here — §4.2, ⟨OQ-2⟩) | n/a | 2 |
| Extra syscalls while a waiter is blocked | 0 (one `poll` per wait) | **~20/s per blocked waiter** at a 50 ms slice — an idle listener in `Accept` pays it forever | 0 on the waiter; the drain thread's `epoll_wait` amortises over every descriptor | n/a | — |
| Wake on `evict`/unblock/deadline-replaced | immediate (8-byte write to the desc's efd) | **≤ one slice** of latency — `Close` of a pipe under a blocked `Read` returns late | immediate (`PulseAll` on the desc gate) | n/a | — |
| Regular files and directories (Go: refused by epoll with EPERM → blocking fallback) | `poll(2)` ACCEPTS them (always ready) → `pollOpen` must refuse them ITSELF with an `fstat` per open (`S_IFREG`/`S_IFDIR` → EPERM), or `os.File.SetDeadline` stops answering `ErrNoDeadline` for files | same `fstat` | **free** — `EPOLL_CTL_ADD` answers EPERM exactly where Go gets it | n/a | free |
| Deadlines | the `poll` timeout = min(remaining) — no timers, but the sticky/replace/past-fires-now semantics still need the flags AND the efd wake | slice-bounded precision unless timeout = min(slice, remaining) | the Windows §5 machinery verbatim (`Timer` + sticky flags + generations) | n/a | Go's |
| `isPollServerDescriptor` | must recognise EVERY desc's efd (a table lookup), and `os/exec`'s fd enumeration sees one extra fd per open socket/pipe | `false` stays truthful | `fd == epfd` — Go's answer, minus the break fd it does not own | n/a | Go's |
| Reuse of the Windows flavor's gated machinery | the flag semantics only | the flag semantics only | **the entire desc state machine** — `ManagedPollDesc`/`ManagedPollMode`, `pollBlock`, `pollReset`, `pollSetDeadline`/`applyDeadlineLocked`/`armDeadlineLocked`/`deadlineFired`, `pollUnblock` — ~200 lines already gated by `NetDeadlineMatrix` | none | none |
| Scope of fd kinds | sockets, pipes, FIFOs, ttys | same | same | **sockets only** — pipes/ttys have no .NET surface; and the `Socket` wrapper takes over the fd's blocking mode and lifetime | all |
| Principal risk | fd-budget doubling (net's connection-count tests, `ulimit -n 1024` on the distro), the extra `fstat`, the `IsPollDescriptor` table | the 50 ms tax and the close latency (`PipeCloseUnblocksRead` would print Go's line only after a slice) | the edge-triggered readiness argument (§4.5) and the drain thread's lifecycle (§4.2, §9) | not general enough to be a candidate | the scheduler wall the Windows design §3.2 measured |

**The decision this note recommends (⟨OQ-1⟩): B.** It is Go's mechanism — `netpoll_epoll.go` is
converted and sitting in the corpus as a byte-faithful blueprint, down to its
`EPOLLIN|EPOLLOUT|EPOLLRDHUP|EPOLLET` registration — with `gopark`/`goready` replaced by the one
thing the Windows flavor already built and gated: a monitor gate per descriptor with sticky flags and
generation-checked timers. It owns one descriptor per process instead of one per socket, it gets
Go's regular-file refusal from the kernel instead of an extra `fstat`, it adds zero syscalls to a
blocked waiter, and its one new moving part — the drain thread — is the part Go has too (`sysmon`
and `findrunnable` call `netpoll`; here one dedicated thread does). **A** is the honest alternative:
it is smaller to write (no drain thread, no edge-trigger reasoning, deadlines are `poll` timeouts) and
it is rejected on its two measured costs, the fd doubling and the `fstat`, both of which are visible
to Go's own tests (`os/exec`'s fd enumeration; `os`'s `ErrNoDeadline` for files). **A′** is rejected
outright: the slice tax is paid by every idle `Accept`, and a late `Close` wake is a behavior the
`PipeCloseUnblocksRead` guard would measure as a divergence in timing if not in text. **C** is not
general enough (no pipes, no ttys) and takes ownership of the fd's mode. **D** is the Windows
design's §3.2, unchanged: the converted runtime's poller bottoms in `netpollready → goready` and
`netpollblock → gopark`, the scheduler surface go2cs does not have; its VALUE here is as B's
specification, not as code to wire.

Under **B** a goroutine waiting for readiness costs exactly what it costs today under the blocking
fallback — one parked OS thread — because a go2cs goroutine IS a dedicated thread (the W7 finding);
the poller changes WHERE it parks (a managed gate instead of `read(2)`), which is what makes
`evict` and deadlines able to wake it. Nothing about the thread model moves; §8 keeps M:N out of
scope and §9 notes the one site a future scheduler would replace.

## 4. The design — epoll, one drain thread, the Windows descriptor lifted

### 4.1 The address model — one rule, no exceptions

Every byte the kernel reads or writes is a **native or stack image owned by the hand-own**, handed
through the keystone `RawSyscall6`/`Syscall6` as `(uintptr)` of its address, and copied to or from
managed state by hand. No `ж<T>` address, no generated address-taking wrapper, no managed array is
ever given to the kernel. This is the rule the sockaddr mirror (`syscall/linux/sockaddr_linux_impl.cs`)
and the `struct stat` mirror (`zsyscall_linux_amd64_impl.cs`) landed under, and the epoll surface is
the easiest case it will ever meet, because the only kernel record is:

```
struct epoll_event { uint32_t events; uint64_t data; }   // amd64: __attribute__((packed)), 12 bytes
```

Two fields. The hand-own writes `events` at offset 0 and `data` at offset 4, and reads the same two
offsets back out of the wait buffer — a 12-byte stride, asserted once at type-initialization by an
arch guard (the Linux corpus is amd64-only today: `zsyscall_linux_amd64.cs`; arm64's record is 16
bytes unpacked, so the guard refuses rather than misreads, the `struct stat` mirror's discipline).
`data` carries the descriptor's **token** (§4.6), never a pointer and never the fd.

Why not the generated wrappers, which exist: `EpollCreate1(flag)` takes no address and is fine;
`EpollCtl(epfd, op, fd, ж<EpollEvent>)` hands the kernel `(uintptr)Ꮡev` of a heap box, and a heap
box's address is the TRANSIENT route (`ж.cs`: *"the storage of a standard heap box … is never
blittable — GCHandle refuses to pin it"*) — exactly the STRUCT-PASSING class the board keeps open;
`EpollWait(epfd, slice<EpollEvent>, msec)` takes `Ꮡ(events, 0)`, an element reference, which golib
CAN pin, so that one would in fact be sound. The hand-own does not depend on which ж route a
generated wrapper happens to take: it goes through the keystone for all three and keeps the rule
unconditional. `syscall.EpollEvent` (`uint32 Events; int32 Fd; int32 Pad`) is blittable and the
right size, and it is still not used — its `Fd`/`Pad` split of the 64-bit `data` is Go's layout
trick, not a reason to route a managed struct to the kernel.

**Safe or `unsafe`?** The two-field image can be written with `Marshal.WriteInt32/WriteInt64` and
read with `Marshal.ReadInt32/ReadInt64` over a once-allocated native buffer (both are specified for
unaligned access, which the packed 12-byte stride needs for `data` at offset 4) — no `unsafe` block, no
`[module: go.GoRequiresUnsafe]`, and therefore NO change to `internal.poll.csproj`, whose
`<AllowUnsafeBlocks>` is `false` today and is ONE property serving every `$(GoTargetOS)`
(`projectFileWriter.go`: *"The declaration is per PACKAGE rather than per platform on purpose"*). The
alternative — a `[StructLayout(Sequential, Pack = 1)]` mirror with `stackalloc`, the sockaddr
mirror's idiom — reads better for a multi-field record and costs a seeded csproj regen that flips the
compile setting for the Windows build of `internal/poll` too (no IL change, but a shared-file change
under a "Windows untouched" lane). ⟨OQ-9⟩ recommends the safe `Marshal` form for this two-field case.

### 4.2 Process-wide state and the drain thread

`runtime_pollServerInit` runs once under `serverInit.Do` — Go's `netpollinit` — and does three
things: `epfd = epoll_create1(EPOLL_CLOEXEC)`; allocate the drain buffer (128 events × 12 bytes,
native, process-lifetime); start the drain thread, `new Thread(drainLoop) { IsBackground = true,
Name = "go2cs-netpoll" }`. A failure of `epoll_create1` is what it is in Go — `throw("runtime:
epollcreate failed")` — an `InvalidOperationException` out of `serverInit.Do`, i.e. the first
pollable `FD.Init` in the process panics; there is no fallback to "un-armable for everyone" because
that would silently re-introduce the blocking degradation this arc exists to remove.

**`CLOEXEC` matters here, not cosmetically**: G's posix_spawn arc spawns children from the same
process, and a child must not inherit the poller's descriptor (it would keep the epoll set alive
past the parent's expectations and appear in the child's fd table). `EPOLL_CLOEXEC` is what Go
passes; so does this.

The drain loop, which is Go's `netpoll(delta)` with `delta = -1` forever and no scheduler to hand the
ready list to:

```
for (;;) {
    n = RawSyscall6(SYS_EPOLL_WAIT, epfd, buffer, 128, -1, 0, 0);
    if (errno == EINTR) continue;                         // Go: `if errno == _EINTR { goto retry }`
    if (errno != 0) throw ...;                            // Go: throw("runtime: netpoll failed") — ⟨OQ-3⟩
    for (i = 0; i < n; i++) {
        events = ReadInt32(buffer, i*12); token = ReadInt64(buffer, i*12 + 4);
        if (!pollTable.TryGetValue(token, out desc)) continue;     // stale: closed under us (§4.6)
        r = (events & (EPOLLIN|EPOLLRDHUP|EPOLLHUP|EPOLLERR)) != 0; // Go's mode mapping, netpoll_epoll.cs:173-177
        w = (events & (EPOLLOUT|EPOLLHUP|EPOLLERR)) != 0;
        lock (desc.Gate) {
            desc.EventErr = events == EPOLLERR;           // Go: pd.setEventErr(ev.events == _EPOLLERR, tag) — set AND cleared
            if (r) desc.Read.Ready = true;
            if (w) desc.Write.Ready = true;
            Monitor.PulseAll(desc.Gate);                  // Go: netpollready → netpollunblock(ioready) → goready
        }
    }
}
```

**Why `EINTR` is the normal case, not a corner.** `epoll_wait` is in the class of calls that is
*never* restarted after a signal handler returns, `SA_RESTART` or not (`signal(7)`); the CLR and
`System.Native` install handlers (the activation signal for thread suspension, `SIGCHLD` for child
processes — which G's arc will raise on every spawned child's exit), so a long-blocked drain thread
WILL see `EINTR`. Go's loop retries; this one does too, and S0 measures how often (§7).

**Why no eventfd.** Go's `netpollBreak` writes its eventfd to interrupt a `netpoll` that the
SCHEDULER owns — a wait with a timeout that must be re-evaluated when timers move, when a G is
readied, when the world stops. Nothing here needs to interrupt the drain thread: deadlines are
`Timer` callbacks that wake waiters directly (§4.7), `pollUnblock` pulses the gate directly, and
`EPOLL_CTL_ADD`/`DEL` from another thread take effect inside an in-progress `epoll_wait` (the
canonical epoll pattern; a wait started before an `ADD` still reports the new descriptor's events).
The thread is a background thread, so process exit does not need to wake it either. The eventfd is
therefore omitted (⟨OQ-2⟩), which leaves the poller owning ONE descriptor; it can be added in an
hour if a future scheduler needs to interrupt the wait, and nothing in the contract depends on its
absence.

**Blocked in native code is the right place to be blocked.** A thread inside a `[LibraryImport]`
call is in preemptive GC mode — the collector does not wait for it, and it holds no managed
reference while it waits (the buffer is native, the table is touched only between waits). That is
also why a waiting goroutine — a dedicated thread parked in `Monitor.Wait` — costs exactly what the
fallback's `read(2)`-blocked thread costs today: one parked OS thread, no scheduler interplay.

### 4.3 The managed descriptor — lifted from the Windows flavor, plus one flag

The desc and its per-mode state are the Windows file's (`windows/runtime_netpoll_impl.cs:130-180`),
copied, with one addition:

```
sealed class ManagedPollMode { bool Ready; bool Expired; long DueNanos; Timer? Deadline;
                               long Generation; long ArmedGeneration = -1; ManagedPollDesc Owner; }
sealed class ManagedPollDesc { object Gate; bool Closing; bool EventErr;      // EventErr: unix-only, §4.5
                               ManagedPollMode Read, Write; int Fd; ulong Token; }
static ConcurrentDictionary<ulong, ManagedPollDesc> pollTable; static long nextPollToken;
```

`EventErr` is Go's `pdEventErr` info bit (`netpoll.go`), consulted by `netpollcheckerr` for mode
`'r'` only — `pollErrNotPollable` (3) — and absent from the Windows flavor because
`netpoll_windows.go` never sets it. What the Linux desc does NOT need that the Windows desc has is
the golib `GoAsyncIO` sink: on Windows the readiness FACT arrives in a completion callback that
lives in `syscall`'s overlapped wrappers, two assemblies away, and golib was the only rendezvous
both could see; here the fact is read by `internal/poll` itself, in its own drain thread, so there
is no seam and no golib footprint at all.

### 4.4 The ten bodies

| # | Contract | Linux body (Go's `netpoll.go`/`netpoll_epoll.go` line in brackets) |
|:--|:--|:--|
| 1 | `runtimeNano` | `Stopwatch`, as the fallback has it; the one clock deadlines and `DueNanos` share |
| 2 | `pollServerInit` | `epoll_create1(EPOLL_CLOEXEC)`, drain buffer, drain thread (§4.2) [`netpollinit`] |
| 3 | `pollOpen(fd)` | mint `token`, build the desc, **insert it into `pollTable` FIRST**, then `EPOLL_CTL_ADD(fd, {EPOLLIN\|EPOLLOUT\|EPOLLRDHUP\|EPOLLET, token})`; on errno: remove the entry, return `(0, errno)` — EPERM for a regular file or directory is the kernel's own answer and `os.newFile` discards it exactly as today; on success return `(token, 0)` [`netpollopen`, `:56-59`] |
| 4 | `pollClose(ctx)` | require `Closing` — Go's *"runtime: close polldesc w/o unblock"* assert, kept as an `InvalidOperationException` as on Windows; stop and dispose both timers; `EPOLL_CTL_DEL(fd)` (errno ignored, as `poll_runtime_pollClose` ignores `netpollclose`'s); `pollTable.TryRemove(token)` [`netpollclose`] |
| 5 | `pollWait(ctx, mode)` | unknown ctx → `pollErrClosing` (a stale ctx after close; the code `fd_unix` is prepared for); else `pollBlock(desc, mode, ignoreErrors: false)` — the Windows loop with the unix check order: Ready consumed first; then `Closing → 1`, `Expired → 2`, `mode == 'r' && EventErr → 3` [`netpollblock` + `netpollcheckerr` `:539-554`] |
| 6 | `pollWaitCanceled(ctx, mode)` | no Linux caller (§2.2); ⟨OQ-5⟩: `pollBlock(…, ignoreErrors: true)` — recommended — or the fallback's `Unreachable` |
| 7 | `pollReset(ctx, mode)` | `Closing → 1`; `Expired → 2`; `mode == 'r' && EventErr → 3`; else `Ready = false`, `0` [`poll_runtime_pollReset`] |
| 8 | `pollSetDeadline(ctx, d, mode)` | the Windows body verbatim (§4.7) |
| 9 | `pollUnblock(ctx)` | the Windows body verbatim: `Closing = true`, both generations bumped, both timers stopped, `PulseAll` — wake WITHOUT ready [`poll_runtime_pollUnblock` `:473-505`] |
| 10 | `isPollServerDescriptor(fd)` | `fd == epfd` — truthful for the one descriptor the poller owns; `os/exec`'s `TestExtraFiles` is the consumer (§2.4) [`netpollIsPollDescriptor`] |

### 4.5 Readiness delivery — edge-triggered, and why that is sound here

The registration is Go's, `EPOLLIN|EPOLLOUT|EPOLLRDHUP|EPOLLET` — **edge-triggered**: the kernel
reports each mode once per TRANSITION to ready, not continuously while ready. Level-triggered would
have the drain thread re-reporting a readable socket every pass until the consumer drained it, which
forces either `EPOLLONESHOT` plus an `EPOLL_CTL_MOD` per wait (a syscall per wait, per descriptor)
or a spinning drain thread; ET is what Go chose for the same reason, and the consumer protocol is
built for it. The argument that no edge is lost, stated once so the implementation does not
re-derive it:

1. `prepare` clears `Ready` (contract 7). An edge that arrived BEFORE this operation is discarded —
   safely, because if the transition happened, the data (or space, or connection) is still there,
   and step 2 will observe it directly.
2. The syscall runs. If it succeeds, no wait happens and the discarded edge was redundant. If it
   answers `EAGAIN`, the buffer is — at that instant — empty (read), full (write) or the listen
   queue is empty (accept). Any FUTURE readiness is therefore a new transition, hence a new edge.
3. `wait` (contract 5) consumes `Ready` first. If the edge from step 2's "future" already landed
   between `EAGAIN` and the gate — the drain thread got there first — `Ready` is set and the loop
   returns without parking; otherwise it parks until the drain thread pulses. Either way the retry
   syscall finds what the edge announced.

The only window in which an edge could be "lost" is between step 1's clear and step 2's syscall —
and an edge in that window means the syscall will NOT answer `EAGAIN`. One waiter per mode
(`fdMutex.rwlock`, above the contracts) means no second consumer can take the data the edge
announced and leave the woken one to re-park on a stale `Ready`. This is Go's own argument for ET;
the Windows file's `pollBlock` already implements "consume Ready before checking errors", so the
loop is literally shared.

Mode mapping is Go's (`netpoll_epoll.cs:173-177`): `EPOLLIN|EPOLLRDHUP|EPOLLHUP|EPOLLERR → 'r'`,
`EPOLLOUT|EPOLLHUP|EPOLLERR → 'w'` — a peer's half-close (`RDHUP`) and a hang-up wake the reader
so its retry sees EOF; an error wakes both so the retry syscall reports the real errno. `EventErr`
is set when the event is EXACTLY `EPOLLERR` and cleared by any other event (Go's
`setEventErr(ev.events == _EPOLLERR)`), and it surfaces as `pollErrNotPollable` on the read side
only — *"Report an event scanning error only on a read event. An error on a write event will be
captured in a subsequent write call that is able to report a more specific error."*

`Accept` under ET deserves one sentence because it is where ET designs usually go wrong: Go's loop
(`fd_unix.cs:812-834`) calls `accept4` until `EAGAIN` before it waits, so a burst of connections
arriving as one edge is drained by the loop, not by the poller — the consumer is ET-correct as
written, and `crypto/tls`'s parallel dials are its measurement.

### 4.6 Tokens, descriptor reuse, and the close sequence

`epoll_event.data` carries a 64-bit **token** minted by `Interlocked.Increment`, which is also the
`ctx` returned to `internal/poll` — the Windows flavor's choice, for the same reason: the kernel
cannot carry a managed reference, and the fd number is the wrong key because the kernel reissues it
the moment a descriptor closes. Go stores `*pollDesc` and defends it with `fdseq`; a token is that
defense in one field. Three orderings follow:

- **`pollOpen` inserts the table entry BEFORE `EPOLL_CTL_ADD`.** A readable socket can deliver its
  first edge before `epoll_ctl` returns; the drain thread must be able to resolve the token at that
  instant or the edge is lost (not merely delayed — under ET it will not be repeated).
- **`pollClose` runs `EPOLL_CTL_DEL` BEFORE `pollTable.TryRemove`**, and both run before
  `close(2)` by `FD.destroy`'s ordering (§2.3). The explicit `DEL` is not optional: the kernel drops
  a closed fd from an epoll set only when its LAST reference closes, and `os.File.Fd()`/`os/exec`
  dup descriptors into children (posix_spawn file actions), so without the `DEL` a parent's closed
  socket could keep reporting edges through a child's copy — the classic epoll pitfall, which Go
  avoids by the same explicit `netpollclose`.
- **A stale event for a closed desc is ignored by construction**: its token no longer resolves.
  A new desc on a reused fd number has a fresh token. No generation counter is needed beyond the
  token itself.

### 4.7 Deadlines — §5 of the Windows design, inherited, and what gets SIMPLER

The deadline machinery is lifted verbatim: `d > 0` arms, `d == 0` clears, `d < 0` expires now
(`setDeadlineImpl` normalizes exactly-now to `-1`, `fd_poll_runtime.cs:160-165`); expiry is STICKY
per mode until the next `pollSetDeadline` on that mode; a deadline set in the past wakes the current
waiter WITHOUT `Ready` so its loop re-checks and returns `pollErrTimeout`; every deadline change
bumps the mode's generation and a fired `Timer` callback re-validates its captured generation under
the gate (forced by .NET semantics — `Timer.Change`/`Dispose` do not synchronize with an in-flight
callback); the ~49.7-day `Timer` ceiling is clamped and re-armed on fire; `pollUnblock` bumps both
generations and stops both timers; check order is closing > timeout > eventErr with readiness
consumed first. The Windows design's §5 argues each of these from `runtime/netpoll.cs` line by line
and its `NetDeadlineMatrix` guard asserts them; nothing is re-argued here.

What the Linux flavor does NOT have, and is simpler for: there is **no cancel-and-harvest**. A
Windows timeout wakes a thread that still owns a kernel-pending overlapped operation and must
`CancelIoEx` then `waitCanceled` for the completion it cannot abandon (Windows §5 point 6, the
source of that design's "priced risk"). A Linux timeout wakes a thread that owns nothing — the
syscall already returned `EAGAIN` — so it returns `pollErrTimeout` and the consumer returns
`ErrDeadlineExceeded`, full stop. The race surface is therefore edge-vs-timeout-vs-unblock-vs-
deadline-replaced, without the `skipSyncNotif` and cancel dimensions; the same single lock, single
waiter per mode, generation checks and wake-vs-ready separation cover it.

## 5. What changes for `os` and `os/exec` — stated, not left to be discovered

Nothing in `os`, `os/exec`, `net` or `syscall` is edited, but their BEHAVIOR on Linux moves toward
Go's, and each move is named here because the fallback's header promised the degradations it
listed would be the visible ones:

- **Pipes, FIFOs and ttys arm.** `os.Pipe` → `kindPipe` → pollable → `O_NONBLOCK` stays set →
  `Read` on `EAGAIN` parks on the gate. `Close` of the read end runs `evict` → the parked reader
  wakes with `errClosing(isFile)` → `os` reports `file already closed`: `PipeCloseUnblocksRead`
  prints Go's `read unblocked: read |0: file already closed` instead of the fallback's `read did NOT
  unblock`. `SetDeadline` on a pipe stops answering `ErrNoDeadline` and is honored. A tty opened via
  `os.Open("/dev/tty")` is `kindOpenFile` and arms too (Go's behavior); `os.Stdin/Stdout/Stderr` are
  `kindNewFile` (blocking unless already non-blocking) and are unchanged.
- **Regular files and directories: unchanged, by the kernel's EPERM instead of the file's.** The 28
  rows the fallback flipped are this arc's CONTROL: the same `os.newFile` fallback runs, fed the same
  errno from `epoll_ctl` instead of from a constant. If the S1 roster re-run moves any of them, the
  design is wrong about the kernel, not the rows.
- **`os/exec`, the adjacency G's arc should hear about at ratification (a mailbox FYI, not a
  claim).** The parent's ends of the child's stdin/stdout/stderr pipes become pollable: the copying
  goroutines move from a `read(2)` that blocks to `EAGAIN` + a parked wait — the same bytes, a
  different parking. The CHILD's ends are unaffected: `os.StartProcess` builds `ProcAttr.Files` from
  `os.File.Fd()`, which calls `SetBlocking` and clears `O_NONBLOCK` on THAT descriptor (each end of a
  pipe is its own open file description, so the parent's end keeps its mode) — Go's existing
  discipline, which G's posix_spawn seam inherits because it consumes the same `ProcAttr`. The
  poller's `epfd` is `EPOLL_CLOEXEC` and is not inherited; `TestExtraFiles` will see it in the
  parent and `IsPollDescriptor` answers `true` for it (§2.4). `SetBlocking`'d descriptors stay
  registered (§2.5): events for them set `Ready` on a desc nobody waits on, and `Close` skips the
  `csema` park — harmless, and Go's.
- **Sockets.** `net.Listen`/`Dial` stop returning `operation not permitted`; `Accept`, `Read`,
  `Write`, deadlines and `Close`-unblocks-everything run the Windows-measured contract on Linux.
  What stays walled is the UDP/ancillary wrapper set the sockaddr mirror did not cover
  (`Recvfrom`/`Sendto`/`Recvmsg`/`Sendmsg` still take the un-mirrored `ж<RawSockaddrAny>` route);
  a suite that reaches them dies there, one seam further on, and that seam is named in the sockaddr
  mirror's header as the next one.

## 6. Blast radius

- **One file, replaced in place:** `src/core/internal/poll/linux/runtime_netpoll_impl.cs`. It keeps
  its `[module: go.GoManualConversion]` marker (no Go principal exists, so a reconvert never
  regenerates it; the marker keeps a `-stdlib` run from emitting one), it rides the csproj's existing
  `<Compile Include="$(GoTargetOS)/*.cs" />` glob, and under ⟨OQ-9⟩'s safe form it adds no
  `[module: go.GoRequiresUnsafe]` — so `internal.poll.csproj` is untouched and no regen is owed.
  (Under the `unsafe` alternative the marker joins the file and a seeded single-package regen of
  `internal/poll` flips the shared `<AllowUnsafeBlocks>` to `true` for every GOOS — a compile
  setting, zero IL change for the Windows sources, but a shared-file diff the Windows control must
  then cover.)
- **Windows flavor untouched** — `windows/runtime_netpoll_impl.cs` is the SOURCE the desc machinery
  is copied from; it is not edited (⟨OQ-7⟩ keeps the two flavors as per-GOOS authorities, the
  `lock_sema`/`lock_futex` precedent). `GoAsyncIO` and the rest of golib: untouched. The converter:
  untouched — the ten contracts are bodyless `//go:linkname` partials by construction, so there is
  no `manualConversionFuncs` entry to add, no placeholder to regenerate, and no CNR footprint. The
  keystone (`internal/runtime/syscall/linux/syscall_linux_impl.cs`): untouched; `RawSyscall6` is
  enough. `syscall`'s generated `Epoll*` wrappers: untouched and unused (§4.1).
- **darwin:** the fallback file's header said *"when it builds, this file is its remedy byte for
  byte"* — that remains true of the FALLBACK (copy the pre-poller file into `darwin/` when that
  corpus builds); the epoll poller is Linux-only, and darwin's readiness poller is a kqueue design
  when there is a darwin corpus to measure it against. Nothing here pre-empts it.
- **L3 layout:** a principal-less companion; `platformHandOwn_test.go` constrains nothing about it
  (measured in the poll-seam lane: `TestMergeLeavesPrincipalLessCompanionsWhereTheyAre`).
- **Marker census:** unchanged (the file already carries the marker; it is a rewrite, not an
  addition).

## 7. Gates and staged landing

Each stage states what it MEASURES and what is its CONTROL; the Linux measurements run on the WSL2
lane (the F15 recipe; the distro is a real Linux kernel, so epoll semantics are the real ones), the
Windows control is a JOB dispatch to the i9 as in the three prior lanes.

**S0 — measure before writing (an hour, not a lane).** Four probes in the distro, as converted or
plain-C# programs through the keystone: (a) `epoll_ctl(EPOLL_CTL_ADD)` on a regular file and on a
directory answers EPERM, on a pipe and a socket succeeds — trivial, and it is the entire file-
fallback argument, so it is measured rather than assumed; (b) the 12-byte stride guard holds on the
distro's amd64 build; (c) the `EINTR` rate of a long `epoll_wait` in a converted program that is
also allocating and spawning (expected non-zero — the proof that the retry loop is load-bearing);
(d) an `EPOLL_CTL_ADD` issued while another thread is inside `epoll_wait(-1)` delivers the new
descriptor's edge without a break write (the fact ⟨OQ-2⟩ rests on). None of these needs the desc
machinery; all four fit in one probe program.

**S1 — land the file; the loopback round trip is the milestone.** Gates, in order:
`internal.poll.csproj`, `net.csproj`, `os.csproj` linux-flavor native `dotnet build` → 0 errors;
`go2cs-stdlib.slnx -p:GoTargetOS=linux` native `--no-incremental` → 0 errors (the chain already does
this before every roster run); `go2cs-stdlib.slnx -p:GoTargetOS=windows` → 0 errors and
`internal/poll`'s Windows build byte-untouched (the control; trivial under ⟨OQ-9⟩'s safe form);
`GolibTests` (golib untouched — run anyway, the standing gate); the four behavioral programs run BY
HAND on the distro — `PipeCloseUnblocksRead` (expected: Go's line), `NetListenSmoke`,
`TcpLoopbackRoundTrip`, `NetDeadlineMatrix` (expected: stdout byte-identical to `go run`) — converted
with an explicit `-go2cspath`, built `-p:GoTargetOS=linux`, diffed against `go run` (⟨OQ-6⟩ on the
harness binding); then the **Linux roster re-run W1-style** over the 161 rows with the sockaddr
lane's ledger as baseline: the CONTROL is that the 129 PASS rows stay PASS (the regular-file fallback
via the kernel's EPERM), the MEASUREMENT is `encoding/json` → PASS at 491 and `crypto/tls` → runs,
reported at its Linux count; every other residual re-classified against the census's classes (none
is expected to move — R2, W1b, R6, W2–W7 are not poller seams). Windows control by JOB dispatch:
the 8-row set of JOB-R4 (`encoding/json`, `crypto/tls`, the six banked `net/*`) must stay green;
since the change is linux-only at the file level, a red there is a harness or merge fact, never a
poller one. Budget: the roster re-run is ~3 h on the distro (the sockaddr lane's chain measured it),
the 8-row Windows control ~30 min on the i9.

**S2 — the deadline matrix and `crypto/tls`'s residuals.** `NetDeadlineMatrix` byte-identical on
Linux is the S1 expectation; if it is not, the iteration budget goes here exactly as the Windows
design priced for its own S2 (its §5 "priced risk"), minus the harvest dimension. `crypto/tls`'s
Linux verdicts are classified against Windows' 400 + 2: a W4 per-OS count difference is reported,
not hidden; any test that passes on Windows and fails on Linux is a poller finding first and a
per-OS fact second.

**S3 — consumers.** The off-roster socket ledger's Linux legs (`net/smtp`, `net/http/httptest`,
`net/http/cgi`, `net/http/httputil`, `net/http/cookiejar`, `net/rpc`), measured W1-style against
their Windows state; `os` is not on the roster, so its pipe semantics are guarded by the behavioral
programs rather than a row; G's `os/exec` rows are re-measured after R2 lands (the pipe adjacency of
§5 is exercised there, and the FYI goes to G before that, not after).

**What is NOT a gate here and why:** `check-no-regression.ps1` — no converter change, no registry
entry, nothing emitted moves; the darwin build — that corpus does not build (pre-existing, censused);
a perf comparison — §8.

## 8. Non-goals — the boundary inherited from the Windows design's §8, and this design's own

Inherited, restated for Linux:

- **No scheduler-facing netpoll surface.** `netpoll(delta)`, `netpollBreak`, `netpollready`,
  `netpollAnyWaiters` have no caller in managed land; the converted `runtime/linux/netpoll_epoll.cs`
  stays dead. The drain thread is not a scheduler hook and is not named as one.
- **No performance targets.** Correctness-first; the design avoids the known cliffs (no per-event
  allocation, one lock per desc, edge-triggered registration, no syscall per wait on the waiter) and
  nothing benchmarks it. An `epoll_wait` batch of 128 is Go's number, not a tuned one.
- **No `net` operational campaign.** §1.2's bill is the boundary of the claim: `net` itself stays
  FUTURE, its Linux census is taken when the poller exists.
- **No os-file async IO.** Regular files and directories are NOT armed — by the kernel's EPERM,
  which is Go's own Linux behavior, not a go2cs decision.

This design's own:

- **No darwin / kqueue.** The darwin corpus does not build; the pre-poller fallback file is its
  remedy when it does, and kqueue is a separate design against a corpus that can be measured.
- **No M:N / cooperative-scheduler integration.** A waiting goroutine parks its dedicated thread
  in `Monitor.Wait`, exactly as the W7 finding describes the thread model today.
  `DESIGN-cooperative-scheduler.md` (PROPOSED) would replace that park with a scheduler-aware one;
  the design keeps every wait in ONE function (`pollBlock`) so that a future scheduler replaces one
  site, and claims nothing more.
- **No UDP / ancillary syscall mirrors.** `Recvfrom`/`Sendto`/`Recvmsg`/`Sendmsg` are the next
  syscall seam (the sockaddr mirror's header names them); a poller does not move them.
- **No break eventfd** (⟨OQ-2⟩), **no `pollWaitCanceled` consumer** (it has none on Linux), **no
  edits** to `os`, `net`, `syscall`, `internal/poll`'s converted files, golib, the converter, or the
  Windows flavor.
- **No roster changes.** Linux rows do not bank until the per-OS arithmetic ruling; this design's
  output is measured counts on the board, as the three prior Linux lanes'.

## 9. Adversarial pass — the design's first draft, attacked (charter §7)

### 9.1 Correctness — where could a wakeup be lost, or the wrong waiter woken?

- **A lost edge.** §4.5's argument: the only window in which an edge is discarded is between
  `prepare`'s clear and the syscall, and an edge there means the syscall succeeds. The argument
  depends on two facts the design does not control and therefore names: the consumer ALWAYS calls
  `prepare` before the syscall (every `fd_unix.cs` site does — `pollReset` is the Windows design's
  contract 6, unchanged), and `fdMutex` admits ONE reader and ONE writer at a time (unchanged
  converted code). A future consumer that waited WITHOUT first observing `EAGAIN` would break ET;
  none exists in Go's `fd_unix.go`. Measurement: the loopback round trip and `crypto/tls`'s
  thousands of read/write/accept cycles — a lost edge there is a hang, which the sweep's per-row
  timeout converts into a loud FAIL, never a silent pass.
- **The first edge beats `pollOpen`.** Table insert before `EPOLL_CTL_ADD` (§4.6) — if the order
  were reversed, a socket that is already readable (an accepted connection with data in flight)
  could deliver its only edge to a token the drain thread cannot resolve. Ordered, and stated in the
  body as a comment, because it is the kind of line a refactor swaps.
- **A stale event wakes a reused fd's new desc.** Impossible by token: the event carries the OLD
  token, which `pollClose` removed; the new desc has a new token. (Keying by fd would have this
  bug; Go's `fdseq` exists for it.)
- **`EventErr` sticks.** Go sets AND clears it on every event; the drain thread does the same
  assignment (`desc.EventErr = events == EPOLLERR`), so a descriptor that errors and then becomes
  readable reports the read, not `ErrNotPollable`.
- **`EINTR`.** Retried unconditionally (§4.2); an `epoll_wait` that returned `EINTR` treated as an
  error would kill the drain thread on the first child exit under G's arc — this is why S0(c)
  measures the rate rather than assuming zero.
- **The drain thread dies.** Any errno other than `EINTR` from `epoll_wait` on a valid `epfd` is a
  process-level invariant failure (`EBADF` means someone closed the poller's descriptor; `EFAULT`
  means the buffer moved — it cannot, it is native; `EINVAL` means `epfd` is not an epoll fd).
  ⟨OQ-3⟩ recommends Go's answer — `throw` — realized as an unhandled exception on the background
  thread, which terminates the process through the crash-report path rather than leaving every
  future waiter parked forever on a gate nobody will pulse. A `catch`-and-continue would be the
  worst of the three options: it turns an invariant failure into a silent hang. The per-event body
  (a dictionary lookup, a lock, two flag writes, a pulse) has no failure mode that is not a
  programming error, and a programming error should also be loud.
- **Spurious `Monitor` wakeups.** `pollBlock` is a `while (true)` re-check loop (Go's own retry
  comment is quoted in the Windows file); a spurious wake re-evaluates Ready/Closing/Expired and
  parks again.
- **`SetBlocking`'d descriptors.** Still registered; events set `Ready` on a desc nobody waits on
  and are otherwise inert; `Close` skips the `csema` park because `isBlocking` is set — but `evict`
  and `pollClose` still run in order, so the table entry and the epoll registration are retired
  before `close(2)` regardless.
- **GC.** The drain buffer is native; the desc objects are rooted by `pollTable` until `pollClose`;
  each `Timer` is rooted by its mode. The drain thread holds no managed reference while it is inside
  `epoll_wait` (preemptive mode), and takes the desc lock only between waits.
- **The Windows assert is kept.** `pollClose` without a prior `pollUnblock` throws *"runtime: close
  polldesc w/o unblock"* exactly as on Windows — it guards `internal/poll`'s own sequencing, which
  is unchanged converted code, and it is the one place a future `FD.Close` regression would be loud.

### 9.2 Cost — what does every converted program pay?

One background OS thread per process that ever arms a descriptor (none before the first pollable
`Init`); one epoll descriptor; per armed fd: one desc, two mode objects, up to two `Timer`s (the
Windows shape — `net/textproto`'s banked allocation-bracket test is on the roster, and it creates no
sockets, so it is unaffected; `net/http`'s allocation tests are off-roster and measured when they
are reached). Per wait: zero syscalls beyond the consumer's own retry; a lock, a `Monitor.Wait`,
a `PulseAll`. Per event: a dictionary lookup under no lock and a short critical section. Go pays the
same `epoll_ctl` per open/close and the same `epoll_wait` per batch; the only thing go2cs pays that
Go does not is the dedicated thread (Go drains on an existing M) and its reserved stack at the runtime's default.

### 9.3 Flakiness — which asserts become timing-dependent?

Deadline tests are timing tests by nature, and the Windows design priced exactly this surface for
its `NetDeadlineMatrix`; Linux adds one hop — drain thread → `PulseAll` → waiter — measured in
microseconds, below the margins those tests use. WSL2 runs the distro in a VM: loopback and
`EPOLLRDHUP` behave as native Linux (real kernel), but wall-clock margins are the VM's, so a Linux
deadline test that is marginal on the i9 can be flaky here — the remedy is the per-OS arithmetic's,
and the measurement is to be reported, not tuned around. One measurement trap the chain already
guards: a `GoTargetOS` switch poisons `obj/` (CLAUDE.md), so the native linux build before every
roster run is `--no-incremental`.

### 9.4 What S1 would NOT catch, said out loud

- **ET with `Writev`/`Splice`/`copy_file_range`.** Their arms use the same prepare/wait protocol, so
  the §4.5 argument covers them; but the roster's rows reach them rarely (`net/http`'s `sendfile`
  paths are off-roster). Named so the S3 consumer lanes know to look.
- **A second waiter per mode.** `fdMutex` forbids it in converted code; a hand-owned consumer that
  bypassed `fdMutex` would break the single-waiter assumption the gate's `Ready` flag rests on. None
  exists; the Windows design carries the same assumption.
- **Kernel differences.** Go's registration is the same on every Linux the corpus targets; the
  distro's kernel is the one measured. A container kernel that mis-reports `EPOLLRDHUP` would show
  up as a hang on half-close, loud not silent.

## 10. Open questions — each with this lane's recommendation; none self-ruled

* **⟨OQ-1⟩ — The mechanism** (§3). *Recommendation:* **B — epoll + one drain thread + the Windows
  descriptor state machine.** It is Go's own mechanism with the scheduler replaced by a gate; it owns
  one descriptor per process, not one per socket; it gets regular-file refusal from the kernel; it
  reuses ~200 gated lines. **A** (`poll(2)` per waiter + per-desc eventfd) is the priced alternative
  — smaller to write, rejected on fd doubling and the extra `fstat`, both visible to Go's own tests.
  **A′** (slices) is rejected on the idle-`Accept` tax and the late-`Close` wake.
* **⟨OQ-2⟩ — The break eventfd** (§4.2). *Recommendation:* **omit.** Nothing needs to interrupt
  the drain thread: deadlines and unblocks reach waiters directly, `EPOLL_CTL_ADD/DEL` take effect
  inside an in-progress wait (S0(d) measures it), and a background thread needs no shutdown signal.
  Go's eventfd serves a scheduler-owned wait; there is none here. `isPollServerDescriptor` answers
  for `epfd` alone. Add it the day a scheduler needs to interrupt the wait — an hour, and no contract
  changes.
* **⟨OQ-3⟩ — The drain thread's failure policy** (§9.1). *Recommendation:* **fail loud — Go's
  `throw("runtime: netpoll failed")`**, as an unhandled exception on the background thread (the crash
  report path), for any `epoll_wait` errno other than `EINTR`. Never catch-and-continue: that
  converts an invariant failure into a process that hangs on its next socket read.
* **⟨OQ-4⟩ — Regular-file refusal** (§1.1, §4.4 #3). *Recommendation:* **the kernel's EPERM
  alone, no `fstat`.** It is exactly Go's Linux behavior and the provenance the fallback's header
  already documents; the 129-row control in S1 is its measurement.
* **⟨OQ-5⟩ — `pollWaitCanceled`'s body** (§2.2). *Recommendation:* **the shared wait loop with
  `ignoreErrors: true`**, not the fallback's `Unreachable`. It has no Linux caller, so the choice
  costs nothing either way; the loop removes a throw from a contract surface and keeps the two
  flavors' desc machinery textually identical (⟨OQ-7⟩).
* **⟨OQ-6⟩ — The Linux behavioral guards** (§1.2, §7 S1). The four existing programs are the
  right guards, but the behavioral runner does not bind the linux flavor. *Recommendation:* **run
  them by hand on the distro in S1** (converted with an explicit `-go2cspath`, built
  `-p:GoTargetOS=linux`, stdout diffed against `go run`) and file the runner's linux binding as a
  harness item — not a precondition for the poller, which must not wait on a harness arc to be
  measurable.
* **⟨OQ-7⟩ — Sharing the descriptor state machine with the Windows flavor** (§4.3). Copy (two
  per-GOOS authorities, the `lock_sema`/`lock_futex` precedent) or hoist into a flat shared
  companion both flavors compile (touches the ratified Windows file and its gates)?
  *Recommendation:* **copy now**; the hoist is a leveling for after both flavors are measured, and
  it is out of a "Windows untouched" lane's scope. The copy is ~200 lines and its source is cited
  line by line in the file header so the two cannot drift silently.
* **⟨OQ-8⟩ — `crypto/tls`'s Linux count.** It will validate at a Linux number, not at Windows' 400
  + 2 (W4, per-OS arithmetic). *Recommendation:* **measure and report both; bank nothing** until the
  per-OS ruling — consistent with every Linux lane so far and with "no roster changes".
* **⟨OQ-9⟩ — Safe `Marshal` images or `unsafe` + `[module: go.GoRequiresUnsafe]`** (§4.1, §6).
  *Recommendation:* **the safe form.** The kernel record has two fields; `Marshal.ReadInt32/ReadInt64`
  over a native buffer reads as plainly as a struct, and it leaves `internal.poll.csproj`'s shared
  `<AllowUnsafeBlocks>` exactly where it is — no regen, no shared-file diff under a Windows-untouched
  lane. The `unsafe` mirror is the right idiom for the sockaddr/stat records and would be the right
  one here the day the file needs a third field.

