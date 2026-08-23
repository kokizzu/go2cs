# The Linux UDP seam — the eight `//go:linkname` datagram helpers, and the last wall between the poller and name resolution

> **STATUS: RATIFIED (coordinator, 2026-08-23, at the docs merge to master).** All six ⟨OQ⟩s
> RULED per their §9 recommendations, with two refinements: ⟨OQ-4⟩ — **S1 is COMMISSIONED
> now; S2 (the msghdr pair) stays PROPOSED until a consuming row materializes** (evidence-ruled,
> not scheduled — no row exercises it today, and review surface is spent where a bill exists);
> ⟨OQ-5⟩ — R owns BOTH halves of the guard ritual (the lane's host is a Windows machine with
> WSL, so the solution registration + `UpdateTestTargets` pass and the Linux run are one lane's
> work, no handoff). The commissioning scope is AMENDED per §1.3's measurement: the bill is
> DNS/name-resolution and `crypto/tls` `TestVerifyHostname` — cgi (exec axis) and cookiejar
> (test-host gap) were never behind this wall. Implementation parks merged-ready under the
> release-eve freeze per §7.
>
> **⟨OQ-3⟩ AMENDED (coordinator, 2026-08-23, on the implementation's own measurements).** The
> ruled procedure named the wrong instrument, twice: a seeded SINGLE-PACKAGE reconvert emits
> **no csproj at all** (sources only — the csproj regenerator is the stdlib driver), and a
> single-TARGET stdlib run with a package filter regenerates the csproj **knowing only its one
> target, silently deleting the other GOOS ItemGroups** — landing that diff would have broken
> the darwin and windows builds of the package. **The correct instrument for an L3 package's
> csproj regen is the THREE-target emission** (`-platforms windows/amd64,linux/amd64,darwin/amd64
> -platform-stage <dir>`), under which the diff is exactly the one-line
> `<AllowUnsafeBlocks>` flip the ruling asked to prove. Measured, not assumed — lane R, S1. Commissioned in the overnight standing orders
> (2026-08-23, to R): *"The UDP wall arc — the big one, yours by domain: DESIGN doc first
> (`DESIGN-linux-udp.md`, OQs named per house style), post 'ratify?' … Scope: the UDP seam of the
> sockaddr/syscall family so the net UDP tests, `net/http/cgi` and `cookiejar` open and the
> `crypto/tls` `TestVerifyHostname` attribution shrinks."* **One part of that scope is corrected by
> measurement in §1.3 and the correction is the first thing this document does** — `net/http/cgi`
> and `cookiejar` are NOT behind this wall, and saying so now keeps the arc's bill honest.
> Companions: [`DESIGN-linux-readiness-poller.md`](DESIGN-linux-readiness-poller.md) (the arc that
> exposed this wall; its §7.3.3 is this document's opening measurement),
> `src/core/syscall/linux/sockaddr_linux_impl.cs` (the mirror whose `writeNativeSockaddr` /
> `readNativeSockaddr` are this design's tools), `src/core/internal/syscall/unix/linux/net.cs` (the
> eight stubs), `src/core/internal/poll/linux/fd_unix.cs` (the four live call sites),
> `src/core/internal/runtime/syscall/linux/syscall_linux_impl.cs` (the keystone). Written against
> master `18770d083`, Go 1.23.1.

---

## 0. In one paragraph

`internal/syscall/unix/linux/net.cs` declares **eight** `//go:linkname` datagram helpers —
`RecvfromInet4/6`, `SendtoInet4/6`, `SendmsgNInet4/6`, `RecvmsgInet4/6` — with no bodies, so the
`PartialStubGenerator` fills them with throwing stubs. They are the ENTIRE datagram surface of the
converted corpus: `internal/poll`'s `ReadFromInet4`, `WriteToInet4`, `ReadMsgInet4` and `WriteMsgInet4`
call them and nothing else does. Until they exist, every UDP socket in the corpus is write-only at
best and dead at worst, and because Go's pure-Go resolver speaks DNS over UDP, **no converted program
on Linux can resolve a name**. This design implements the eight in a new hand-own beside the stubs,
against the keystone `syscall(2)` binding, with the sockaddr mirror's existing native encode/decode
as its tools — the same rule the mirror and the `struct stat` mirror landed under: the kernel only
ever sees a native image this file owns, never a `ж<T>` address. Four of the eight
(`Recvfrom`/`Sendto`) are small and land first; the `msghdr` pair is materially larger because it
must build a native `msghdr`+`iovec` and carry control messages, and it is staged second so the
cheap half is measurable on its own.

## 1. The wall, measured

### 1.1 The stack

From the poller arc's S2 (`DESIGN-linux-readiness-poller.md` §7.3.3), on the provisioned distro with
the poller and `net.runtime_rand` in place:

```
System.NotImplementedException: RecvfromInet4: external (assembly or cgo) function is not implemented
  at internal/syscall/unix.RecvfromInet4   internal/syscall/unix/linux/net.cs:14   (PartialStubGenerator stub)
  at internal/poll.ReadFromInet4           internal/poll/linux/fd_unix.cs:278
  at net.readFrom → net.UDPConn.ReadFrom   net/linux/udpsock_posix.cs:58
```

A loopback UDP probe (no network, no DNS) isolates it exactly: `net.ListenPacket("udp", …)`
**succeeds** — bind, and therefore the sockaddr mirror's `Bind`, is fine — and the first `ReadFrom`
throws. Native Go on the same machine completes the same round trip. So the wall is precisely the
receive/send helpers, not the socket, not the poller, and not the address encoding.

### 1.2 What it gates

| consumer | today | with the eight |
|:--|:--|:--|
| **DNS, hence every name lookup** | `net.LookupHost` times out (the query is never received); with the stub present it *threw*, which is what made `crypto/tls` eat a 30-minute deadline | resolution works; `net.Dial("tcp", "host:port")` by NAME works |
| `crypto/tls` **`TestVerifyHostname`** (roster) | one of the row's two divergences — it dials `www.google.com` | closes, leaving `TestCertCache` (an object-lifetime divergence) as the row's only residual |
| `net`'s own UDP suite (off-roster) | unreachable | reachable, and worth a census then |
| every converted program that resolves a name | fails | works |

### 1.3 What it does NOT gate — a correction to the commissioning scope, with evidence

The standing order names `net/http/cgi` and `net/http/cookiejar` as opening with this arc. **Measured
in the poller arc's S3, they do not**, and the arc should not claim them:

- **`net/http/cgi` — 39 comparable, 15 agree, 24 differ**, and all 24 are `TestCGI*`/`TestChild*`,
  every one of which **spawns a child CGI process**. That is the exec axis (G's arc), not datagrams.
  Windows carries the same row at 36/39, so it is not even Linux-specific.
- **`net/http/cookiejar` — `conversion-blocked`**: its emitted TEST HOST does not resolve golib
  (`CS0234` on `go.GoPositionMap`, `go.time_package` in `package_info.cs`). A test-host emission gap
  on a package that has never been through `-tests`; a converter item, and no datagram is involved.

The other four socket-ledger packages measured in that same S3 — `net/smtp` 19/19,
`net/http/httptest` 55/55, `net/http/httputil` 53/53, `net/rpc` 15/15 — **already validate**, with
zero divergences, without this arc. So the honest bill is §1.2: DNS and everything downstream of it,
plus one `crypto/tls` verdict. That is a large bill, and it does not need borrowed rows.

## 2. The surface — eight functions, four consumers, and one dead copy

### 2.1 The eight, and who actually calls them

All eight are declared in `internal/syscall/unix/linux/net.cs` as bodyless partials, each carrying
`//go:linkname X syscall.<lowercase>` — i.e. in Go they ARE `syscall`'s unexported helpers, exported
into `internal/syscall/unix` by linkname. Their only consumers in the converted corpus are four sites
in `internal/poll/linux/fd_unix.cs`, each inside an `EINTR` retry loop and each already wrapped in
the poller's `prepare → syscall → EAGAIN → wait → retry` protocol:

| stub | signature (C#) | consumer |
|:--|:--|:--|
| `RecvfromInet4/6` | `(nint, error)(nint fd, slice<byte> p, nint flags, ж<SockaddrInet4/6> from)` | `FD.ReadFromInet4/6` (`:278`, `:318`) |
| `SendtoInet4/6` | `error(nint fd, slice<byte> p, nint flags, ж<SockaddrInet4/6> to)` | `FD.WriteToInet4/6` (`:578`, `:620`) |
| `RecvmsgInet4/6` | `(nint n, nint oobn, nint recvflags, error)(nint fd, slice<byte> p, slice<byte> oob, nint flags, ж<SockaddrInet4/6> from)` | `FD.ReadMsgInet4/6` (`:398`, `:444`) |
| `SendmsgNInet4/6` | `(nint n, error)(nint fd, slice<byte> p, slice<byte> oob, ж<SockaddrInet4/6> to, nint flags)` | `FD.WriteMsgInet4/6` (`:734`, `:777`) |

Note the asymmetry the names hide: the `from` parameter of the Recv pair is an **OUT** parameter the
kernel fills, while the `to` of the Send pair is an **IN** parameter. That is the whole difficulty
distribution of this arc — see §4.

### 2.2 The dead copy, and why the bodies do not go there

`syscall/linux/syscall_unix.cs` carries CONVERTED bodies for the same eight (`recvfromInet4` at
`:339`, `sendtoInet4` at `:472`, …), because Go defines them there. **Nothing calls them** — a census
of the linux flavor finds zero call sites; `internal/poll` reaches the `internal/syscall/unix`
linkname instead. They are dead converted code in the same sense as `runtime/linux/netpoll_epoll.cs`,
and like that file they are valuable as a SPECIFICATION rather than as code.

They are also a useful confession of what the auto-conversion gets wrong, because they contain both
defects this arc must avoid, in eleven lines:

```csharp
// syscall/linux/syscall_unix.cs:339 — the CONVERTED recvfromInet4, dead but instructive
ref var rsa = ref heap(new RawSockaddrAny(), out var Ꮡrsa);          // (1) a MANAGED box …
(n, err) = recvfrom(fd, p, flags, Ꮡrsa, Ꮡsocklen);                   //     … handed to the kernel by address
var pp = Ꮡrsa.Reinterpret<RawSockaddrAny, RawSockaddrInet4>();
var port = (ж<array<byte>>)(uintptr)(new @unsafe.Pointer(pp.of(RawSockaddrInet4.ᏑPort)));
from.Port = ((nint)port.Value[0] << (int)(8)) + (nint)port.Value[1];  // (2) the (*[2]byte) port alias
```

(1) is the STRUCT-PASSING class the board has open — a `ж<T>` address is the transient route and the
kernel writes into storage the GC may move or that is not laid out as the kernel expects. (2) is the
exact `(*[2]byte)(unsafe.Pointer(&pp.Port))` alias that L10 retired on Windows and that
`sockaddr_linux_impl.cs` retired on Linux for the ENCODE direction: converted, it produces a
length-zero `array<byte>` and reads garbage. So the decode direction still carries the bug the encode
direction was fixed for — this arc is, in one sentence, **the decode half of the sockaddr mirror**.

**Recommendation (⟨OQ-1⟩): implement in a new `internal/syscall/unix/linux/net_impl.cs`, and leave
the dead copies alone.** Every linkname-into-another-package in this corpus is answered where the
DECLARATION is, never by bridging to the other package's copy: `internal/poll`'s ten `runtime_poll*`
(the poller), `os/tempfile_impl.cs`'s `runtime_rand`, `math/rand`'s and `math/rand/v2`'s, `sync`'s
`runtime_*`, and `net/dnsclient_impl.cs`'s `runtime_rand` from the poller arc. Bridging would need
`syscall`'s unexported helpers made public (an emission-visible change to a package under the release
freeze) for no behavioral gain. Deleting the dead copies is NOT proposed either: they are what a
reconvert regenerates, and touching them is corpus churn for nothing.

### 2.3 What the consumers already guarantee

Read from `fd_unix.cs`, so the implementation need not re-derive them: the caller holds `fdmu` (one
reader, one writer), has already run `prepare` (`pollReset`), retries `EINTR` itself, and treats
`EAGAIN` as "park on the poller and retry" — so these bodies must return `EAGAIN` and `EINTR`
faithfully and must NOT retry internally. `p` and `oob` are managed slices whose bytes must reach the
kernel; `from`/`to` are managed `SockaddrInet4/6` boxes that must NOT.

## 3. The tools that already exist

This arc invents nothing. `syscall/linux/sockaddr_linux_impl.cs` (the sockaddr mirror, 2026-08-22)
already carries, for exactly these address families:

- `NativeSockaddrInet4` / `NativeSockaddrInet6` / `NativeSockaddrLinklayer` / `NativeSockaddrNetlink`
  — `[StructLayout(Sequential)]` mirrors with `fixed` buffers, the kernel's own layout;
- `writeNativeSockaddr(Sockaddr sa, byte* buffer)` — managed `Sockaddr` → native image, which calls
  Go's own `sockaddr()` first so there is ONE definition of what a `Sockaddr` means;
- `readNativeSockaddr(byte* buffer, _Socklen len)` — native image → managed `Sockaddr`, the decode
  `anyToSockaddr` needs, handling AF_INET/AF_INET6/AF_UNIX/AF_PACKET/AF_NETLINK;
- `nativeSockaddrLen = 128` — the `sockaddr_storage` size every stack buffer uses.

`SendtoInet4` is therefore nearly free: Go's own body is `to.sockaddr()` then `sendto(...)`, and
`to.sockaddr()` is ALREADY the mirror's hand-owned encoder. What the auto-converted version then gets
wrong is only the last step — handing the encoder's `unsafe.Pointer` (which points at a managed
`sa.raw` box) to `sendto`. The fix is to write the image into a stack buffer with
`writeNativeSockaddr` and pass THAT address, which is what `Bind`/`Connect` already do three lines
away in the same file.

The keystone reaches the four syscalls by number, no new binding: **`SYS_SENDTO 44`,
`SYS_RECVFROM 45`, `SYS_SENDMSG 46`, `SYS_RECVMSG 47`** (`zsysnum_linux_amd64.cs`).

**⟨OQ-2⟩ — how does `net_impl.cs` reach the mirror's helpers?** They are `private static` in a
different assembly (`syscall`). Three options: **(a)** make `writeNativeSockaddr`/`readNativeSockaddr`
`public static` in the mirror (a hand-owned file, so no emission changes; adds two members to
`syscall`'s public surface that Go does not have); **(b)** duplicate ~40 lines of encode/decode inside
`net_impl.cs`; **(c)** `[InternalsVisibleTo]` from `syscall` to `internal.syscall.unix`.
*Recommendation:* **(a)**, with the two helpers renamed to a deliberately go2cs-flavored spelling
(e.g. `GoWriteNativeSockaddr`) so nobody mistakes them for a Go API, and a header line in both files
naming the other. It keeps ONE definition of the address encoding — which is the whole point of the
mirror — where (b) guarantees the two copies drift and (c) adds an assembly-attribute mechanism the
corpus does not otherwise use.

## 4. The design — four easy, four hard, one rule

**The rule, unchanged from the mirror and the `struct stat` mirror:** every byte the kernel reads or
writes is a native or stack image this file owns, handed to the keystone as a `uintptr`; managed
storage is copied in and out by hand. No `ж<T>` address, no generated address-taking wrapper, ever
reaches the kernel.

### 4.1 `SendtoInet4` / `SendtoInet6` — the easy pair

```
sockaddr image ← writeNativeSockaddr(to)        // stack, 128 bytes; calls Go's own to.sockaddr()
payload        ← pinned p[0]                     // the managed slice's own storage, pinned for the call
SYS_SENDTO(fd, payload, len(p), flags, image, imagelen)
```

`p` is a managed `slice<byte>`; its element storage is the one managed thing golib CAN pin
(`ж.cs`: an array/slice-element reference is "the only reference kind whose storage is an object the
runtime can be asked to hold still"), so the payload goes to the kernel by pinned address without a
copy. An empty `p` passes a valid non-null address of a zero-length region — Go sends zero-length
datagrams and the tests exercise it.

### 4.2 `RecvfromInet4` / `RecvfromInet6` — the easy pair's mirror image

```
addr buffer ← stackalloc 128, addrlen ← 128 (by address, kernel updates it)
SYS_RECVFROM(fd, pinned p[0], len(p), flags, addr buffer, &addrlen)
on success: sa ← readNativeSockaddr(addr buffer, addrlen)
            from.Port = sa.Port ; from.Addr = sa.Addr        // copy into the caller's managed box
```

The OUT direction is where the dead copy's two defects live, and both vanish: the kernel writes into
a stack image, and the port is read arithmetically by the mirror rather than through the
`(*[2]byte)` alias. Note `from` is filled by ASSIGNMENT into the caller's box (Go's helper does the
same), so no address of it is exposed.

### 4.3 `RecvmsgInet4/6` and `SendmsgNInet4/6` — the hard pair, and why

These carry ancillary data (`oob`) and therefore need a native `msghdr` with a native `iovec` array:

```
struct msghdr { void *name; socklen_t namelen; PAD(4); struct iovec *iov; size_t iovlen;
                void *control; size_t controllen; int flags; PAD(4); }     // 56 bytes, amd64
struct iovec  { void *base; size_t len; }                                   // 16 bytes
```

(both confirmed against the converted `ztypes_linux_amd64.cs`, whose `Msghdr`/`Iovec` carry the same
fields and the same `Pad_cgo_*` placement). So one native block holds: the msghdr, one iovec, and —
for the receive direction — the address buffer; `control` points at the caller's `oob` slice pinned
in place. On return the implementation copies back `n`, `oobn = msg.Controllen`,
`recvflags = msg.Flags`, and decodes `msg.Name` through `readNativeSockaddr` exactly as §4.2.

Two facts make this pair genuinely harder than the first, and they are the reason for staging:

1. **`oob` is a two-way buffer with its own length semantics.** On receive the kernel writes control
   messages into it and reports how many bytes it used; `net` then parses them with
   `syscall.ParseSocketControlMessage`, which is converted code operating on the managed slice — so
   the bytes must be copied back into the caller's `oob` (pinning suffices; no copy if pinned).
2. **`SendmsgN`'s `to` may be nil** — `WriteMsgInet4` is used on connected sockets too, where the
   name is absent. `msg.Name = null, msg.Namelen = 0` is that case, and it must not be confused with
   a zero-value address.

### 4.4 Errno, and the one thing these bodies must not do

The keystone returns `(r1, r2, errno)`. These helpers return Go `error`, so errno maps through
`errnoErr(Errno)` exactly as the generated wrappers do — and critically, **`EAGAIN` and `EINTR` are
returned, never handled**: the consumer's loop parks on the poller for `EAGAIN` and retries `EINTR`
itself (§2.3). A body that retried internally would defeat the poller's deadline handling, which is
the sort of thing that reads as "deadlines do not work on UDP" three arcs later.

## 5. Blast radius

- **One new file**, `src/core/internal/syscall/unix/linux/net_impl.cs`, carrying
  `[module: go.GoManualConversion]` (no `net_impl.go` exists, so a reconvert never regenerates it) and
  `[module: go.GoRequiresUnsafe]` — this file DOES need `unsafe` for `stackalloc`, `fixed` and the
  native struct images, unlike the poller's two-field record. That flips
  `internal.syscall.unix.csproj`'s `<AllowUnsafeBlocks>` to `true` **for every `$(GoTargetOS)`**
  (`projectFileWriter.go`: the declaration is per PACKAGE by design), which is an emission-visible
  csproj change — see the freeze note in §7.
- **Two members made public** in `syscall/linux/sockaddr_linux_impl.cs` under ⟨OQ-2⟩(a) — a
  hand-owned file, so no emission change, but it is a Linux-flavor file edit.
- **Nothing else**: no converter change, no golib change, no `internal/poll`, `net`, or `os` edit, no
  Windows or darwin file, and the dead `syscall` copies stay as they are.

## 6. Gates

- `internal.syscall.unix.csproj` and `net.csproj`, linux flavor, native: 0 errors.
- `go2cs-stdlib.slnx -p:GoTargetOS=linux` native `--no-incremental`: 0 errors, warning count
  unchanged from 149.
- `go2cs-stdlib.slnx -p:GoTargetOS=windows`: 0 errors (the control — this arc is linux-flavor plus one
  shared csproj property).
- `GolibTests` (golib untouched, run anyway).
- **A new behavioral guard**, `UdpLoopbackRoundTrip`, modelled on `TcpLoopbackRoundTrip`: a
  `ListenPacket` round trip over IPv4 and IPv6, a `ReadFrom` peer-address assertion, a zero-length
  datagram, and a `ReadMsgUDP`/`WriteMsgUDP` pair for the ancillary path. The probe written during
  the poller arc's S3 is its first draft.
- **The measurement**: `crypto/tls` re-run (expect `TestVerifyHostname` to close, leaving
  `TestCertCache`), a `net.LookupHost` probe against native Go on the same box, and a Linux
  roster leg for regressions.
- CNR: not owed for the hand-own itself, but **owed if ⟨OQ-2⟩(a) or the csproj flip touches emission**
  — the csproj is regenerated by the converter, so the honest check is a seeded single-package
  reconvert of `internal/syscall/unix` showing the `<AllowUnsafeBlocks>` flip and nothing else.

## 7. Staged landing, and the release freeze

**This arc changes linux corpus emission (a csproj property at minimum), so under the release-eve
freeze it PARKS MERGED-READY and goes first after the anchor release.** That is stated here so the
staging is not mistaken for hesitancy.

- **S1 — `Recvfrom`/`Sendto` × Inet4/Inet6 (four bodies).** This is the DNS unlock: the resolver uses
  plain `recvfrom`/`sendto`, not `recvmsg`. Gate on the guard's non-ancillary half plus a
  `net.LookupHost` probe. Landing S1 alone closes §1.2's whole bill.
- **S2 — `Recvmsg`/`SendmsgN` × Inet4/Inet6 (four bodies).** The ancillary path: `net`'s
  `ReadMsgUDP`/`WriteMsgUDP`, IP_PKTINFO, and the oob half of the guard. No roster row demands it
  today, which is exactly why it is second rather than skipped — `net`'s own suite will.
- **S3 — the consumer re-measure**: `crypto/tls` (expect one divergence, not two), a `net` UDP census
  now that it is reachable, and the Linux roster leg.

## 8. Non-goals

- **No `net` operational campaign.** §1.2 is the bill; `net`'s own suite gets a census after S2, not
  a promise now.
- **No darwin.** The same eight stubs exist there; that corpus does not build, and an unmeasured copy
  is not shipped (the poller arc's rule).
- **No fix to the dead `syscall` copies** (§2.2), and no deletion of them.
- **No `Recvfrom`/`Sendto` for non-INET families.** `Sendmsg` to AF_UNIX/AF_PACKET goes through
  different call sites that are not stubbed; this arc is the eight named stubs only.
- **No performance work.** Correctness-first; the payload path avoids a copy because pinning is
  available, not because anything was tuned.

## 9. Open questions — each with a recommendation; none self-ruled

* **⟨OQ-1⟩ — where the bodies live** (§2.2). *Recommendation:* **a new hand-own beside the stubs**
  (`internal/syscall/unix/linux/net_impl.cs`), per every linkname precedent in the corpus; do not
  bridge to `syscall`'s dead copies, and do not delete them.
* **⟨OQ-2⟩ — reaching the mirror's encode/decode across the assembly boundary** (§3).
  *Recommendation:* **make the two helpers public in the hand-owned mirror**, distinctly named so they
  read as go2cs machinery rather than Go API, with cross-referencing headers. Duplication drifts;
  `InternalsVisibleTo` introduces a mechanism the corpus does not use.
* **⟨OQ-3⟩ — the `unsafe` flip.** This file needs `unsafe`, so `internal.syscall.unix.csproj` gains
  `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>` for every GOOS. *Recommendation:* **accept it and
  regenerate the csproj through a seeded single-package reconvert** (the marker-driven path, not a
  hand edit), and show in the gate that the flip is the only diff. It is a capability, not a use: no
  IL changes for the Windows sources.
* **⟨OQ-4⟩ — staging.** *Recommendation:* **land S1 (`Recvfrom`/`Sendto`) alone first.** It closes the
  entire measured bill; the msghdr pair has no consuming row today and doubles the review surface.
* **⟨OQ-5⟩ — the guard's shape.** *Recommendation:* **one new behavioral test `UdpLoopbackRoundTrip`**
  covering IPv4/IPv6 round trip, peer address, and a zero-length datagram in S1, extended with the
  oob pair in S2. Registering it needs the solution + `UpdateTestTargets` ritual, which is Windows-side
  work on a Linux-only capability — the test itself is platform-neutral Go, so it runs on both.
* **⟨OQ-6⟩ — whether `RecvmsgInet6`'s `Recvflags` and the `oobn` semantics need a disclosure.** Go
  reports `msg.Controllen` as `oobn`; if the kernel truncates control data it sets `MSG_CTRUNC` in
  `msg.Flags`, which `net` inspects. *Recommendation:* **reproduce both faithfully and disclose
  nothing** — but measure the truncation case in S2's guard rather than assuming, because it is the
  one place where a wrong copy-back is silent.

