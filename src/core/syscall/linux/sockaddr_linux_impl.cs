// sockaddr_linux_impl.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// Hand-written implementations of the SOCKET-ADDRESS surface on the LINUX flavor -- the sockaddr
// family of the syscall STRUCT-PASSING seam, mirrored from the Windows lane's L10 hand-own
// (syscall/windows/syscall_windows_impl.cs, whose header carries the full write-up of the class).
// It is R5 of the 2026-08-22 Linux roster measurement: encoding/json's TestHTTPDecoding and
// crypto/tls's TestMain both died in SockaddrInet4.sockaddr before any socket call was made.
//
// THE SAME TWO DEFECTS, ON LINUX.
//
// (1) THE PORT ALIAS. syscall_linux.go writes the port in network byte order through a two-byte
// alias over the raw struct's port field -- `p := (*[2]byte)(unsafe.Pointer(&sa.raw.Port))` -- and
// anyToSockaddr reads it back through the same alias. The auto conversion of that alias is
// `(ж<array<byte>>)(uintptr)(new @unsafe.Pointer(...))`, and an `array<T>` reconstructed from a raw
// address is a LENGTH-ZERO array, so `p[0]` panics with `index out of range [0] with length 0`
// (golib array.cs via syscall_linux.cs:549 -- the stack the poll-seam lane measured). The remedy is
// the L10 one: write and read the field arithmetically.
//
// (2) THE STRUCT-PASSING SEAM. `Bind`/`Connect` hand the kernel `unsafe.Pointer(&sa.raw)`, and
// `accept4`/`getsockname`/`getpeername` hand it `&rsa` for the kernel to FILL. sockaddr_in is 16
// bytes with the address and zero padding INLINE; the converted RawSockaddrInet4 holds `Addr [4]byte`
// / `Zero [8]uint8` as golib `array<byte>` MANAGED REFERENCES, RawSockaddrAny holds `Data [14]int8`
// + `Pad [96]int8` the same way, so neither has a native layout and no address of either means
// anything to the kernel. The remedy is the class's established one: a blittable mirror in a STACK
// buffer for the duration of one call, an explicit field copy in each direction at the boundary.
//
// WHAT IS COVERED -- the TCP listen/dial/accept path, exactly L10's set plus Linux's own decode:
//   - the two INET encoders (the port alias);
//   - Bind / Connect: build the native image with writeNativeSockaddr and hand its address to the
//     package's OWN generated `bind`/`connect`, which take an `unsafe.Pointer` and were never the
//     broken part -- their errno handling stays where the converter put it;
//   - Getsockname / Getpeername / Accept4: their generated wrappers take a typed `ж<RawSockaddrAny>`,
//     so these go through the Syscall/RawSyscall trampoline directly with a stack buffer, mirroring
//     the generated wrappers' error handling exactly (RawSyscall for the two getters, Syscall6 for
//     accept4, as zsyscall_linux_amd64.cs has them), and decode with readNativeSockaddr;
//   - anyToSockaddr, Linux's decode (Go's is a free function here, the method form is Windows's):
//     FLATTEN the managed RawSockaddrAny back to the 112-byte native image its fields transcribe
//     (Family at 0, Data at 2..15, Pad at 16..111) and hand that to the one decode -- so any
//     remaining auto caller (the UDP receive path, Recvmsg) decodes correctly once ITS fill is.
//
// DELIBERATELY NOT COVERED, named rather than left to be rediscovered: Recvfrom / Sendto / Recvmsg
// / Sendmsg -- the UDP and ancillary paths -- still pass `&rsa` / the encoder's address; L10 drew
// the same line (fix a censused wrapper when a suite REACHES it), and nothing on the TCP path
// touches them. writeNativeSockaddr / readNativeSockaddr are what they would need. The
// SockaddrUnix / SockaddrLinklayer / SockaddrNetlink ENCODERS stay auto: they have no port alias,
// their raw structs are consumed here only through writeNativeSockaddr (which calls them for Go's
// own validation and length rules), and the address they return is never handed to the kernel by
// a covered wrapper.
//
// THE DEPENDENCY, MEASURED AND STATED UP FRONT. This file moves the socket wall; it does not open
// the gate. On the Linux flavor a socket is un-armable -- internal/poll/linux/runtime_netpoll_impl.cs
// answers EPERM from runtime_pollOpen for EVERY descriptor, the regular-file fallback applied
// package-wide -- so once Bind/Connect succeed, net's listenStream/dial reach FD.Init -> pollDesc.init
// and return "operation not permitted": an honest error, not a working socket, until a Linux
// readiness poller exists (the separate design DESIGN-netpoll-managed-poller.md §8 names). This
// file is that poller's prerequisite: with it, the first thing the poller will see on Linux is a
// correctly-encoded bind and a correctly-decoded accept.

using System;
using System.Runtime.InteropServices;

// The alias syscall_linux.cs declares for itself -- the declarations replaced below are its
// neighbors. A converted file's aliases are file-scoped, so a hand-owned companion restates it.
using @unsafe = go.unsafe_package;

// Hand-owned (no sockaddr_linux_impl.go exists, so a reconvert never regenerates this file); the
// declarations it replaces are registered in the converter's manualConversionFuncs under the
// windows+linux scope (the encoders, Bind/Connect/Getsockname/Getpeername) and the linux-only scope
// (Accept4, anyToSockaddr), which is what turns their generated bodies into placeholders.
[module: go.GoManualConversion]

// The blittable mirrors need `fixed` buffers and the helpers take raw pointers into stack buffers.
[module: go.GoRequiresUnsafe]

namespace go;

partial class syscall_package
{
    // sockaddr_storage is the largest address any of these calls can carry (128 bytes); every
    // encode and decode below works in a buffer of this size, so one constant covers the stack
    // allocations and the `addrlen` the kernel is told it has.
    private const int nativeSockaddrLen = 128;

    // sockaddr_in exactly as Linux lays it out: 16 bytes, the address and the trailing pad INLINE.
    // `fixed` is what keeps them inline -- a C# array field would be another managed reference,
    // which is the whole bug.
    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct NativeSockaddrInet4
    {
        public uint16 Family;
        public uint16 Port;             // network byte order
        public fixed byte Addr[4];
        public fixed byte Zero[8];
    }

    // sockaddr_in6: 28 bytes, with the 16-byte address inline between the flow info and scope id.
    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct NativeSockaddrInet6
    {
        public uint16 Family;
        public uint16 Port;             // network byte order
        public uint32 Flowinfo;
        public fixed byte Addr[16];
        public uint32 Scope_id;
    }

    // sockaddr_ll: 20 bytes, the 8-byte hardware address inline at the end.
    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct NativeSockaddrLinklayer
    {
        public uint16 Family;
        public uint16 Protocol;
        public int32 Ifindex;
        public uint16 Hatype;
        public uint8 Pkttype;
        public uint8 Halen;
        public fixed byte Addr[8];
    }

    // sockaddr_nl: 12 bytes of scalars -- already blittable, mirrored so the layout is stated here.
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeSockaddrNetlink
    {
        public uint16 Family;
        public uint16 Pad;
        public uint32 Pid;
        public uint32 Groups;
    }

    // Go stores the port as the two bytes `p[0] = hi, p[1] = lo` -- network byte order IN MEMORY --
    // so a little-endian load of that field is the byte-SWAPPED port, which is exactly what
    // sockaddr_in.sin_port carries on the wire. The swap is its own inverse, so encode and decode
    // share it.
    private static uint16 swapBytes(uint16 value) {
        return (uint16)((value >> 8) | (value << 8));
    }

    // (1) THE PORT ALIAS, IPv4. Identical to Go's body except that the port is written to the
    // field instead of through a two-byte alias over it; `raw` is left in exactly the state Go
    // leaves it, so anything that reads it afterwards reads Go's answer.
    internal static (@unsafe.Pointer, _Socklen, error) sockaddr(this ж<SockaddrInet4> Ꮡsa) {
        ref var sa = ref Ꮡsa.Value;

        if (sa.Port < 0 || sa.Port > 0xFFFF) {
            return (default!, 0, EINVAL);
        }

        sa.raw.Family = AF_INET;
        sa.raw.Port = swapBytes((uint16)sa.Port);
        sa.raw.Addr = sa.Addr.Clone();

        // The returned pointer keeps the Go shape and the Go meaning -- the address of `sa.raw`.
        // It is NOT a native image, for the layout reason in the file header, which is why every
        // in-package caller that actually reaches the kernel builds one with writeNativeSockaddr.
        return (new @unsafe.Pointer(Ꮡsa.of(SockaddrInet4.Ꮡraw)), SizeofSockaddrInet4, default!);
    }

    // (1) THE PORT ALIAS, IPv6. See the IPv4 method above.
    internal static (@unsafe.Pointer, _Socklen, error) sockaddr(this ж<SockaddrInet6> Ꮡsa) {
        ref var sa = ref Ꮡsa.Value;

        if (sa.Port < 0 || sa.Port > 0xFFFF) {
            return (default!, 0, EINVAL);
        }

        sa.raw.Family = AF_INET6;
        sa.raw.Port = swapBytes((uint16)sa.Port);
        sa.raw.Scope_id = sa.ZoneId;
        sa.raw.Addr = sa.Addr.Clone();

        return (new @unsafe.Pointer(Ꮡsa.of(SockaddrInet6.Ꮡraw)), SizeofSockaddrInet6, default!);
    }

    // Encodes a Sockaddr into the caller's stack buffer as the native sockaddr Linux expects,
    // returning the byte length to pass as `addrlen`. Go's own validation and raw-filling logic is
    // reused by calling sockaddr() first -- so there is ONE definition of what a Sockaddr means and
    // this function does nothing but translate the layout, which is the only thing the conversion
    // gets wrong.
    private static unsafe (_Socklen len, error err) writeNativeSockaddr(Sockaddr sa, byte* buffer) {
        // The interface value wraps the receiver box; IжAdapter.Box is how a converted interface
        // hands back the `*T` it holds (see the go2cs-gen ImplementGenerator adapters).
        switch ((sa as IжAdapter)?.Box) {
        case ж<SockaddrInet4> box: {
            var (_, _, err) = box.sockaddr();

            if (err != default!) {
                return (0, err);
            }

            ref var raw = ref box.Value.raw;
            NativeSockaddrInet4* native = (NativeSockaddrInet4*)buffer;

            native->Family = raw.Family;
            native->Port = raw.Port;                    // already network order -- see sockaddr

            for (nint i = 0; i < 4; i++) {
                native->Addr[i] = raw.Addr[i];
            }

            for (nint i = 0; i < 8; i++) {
                native->Zero[i] = 0;
            }

            return (SizeofSockaddrInet4, default!);
        }
        case ж<SockaddrInet6> box: {
            var (_, _, err) = box.sockaddr();

            if (err != default!) {
                return (0, err);
            }

            ref var raw = ref box.Value.raw;
            NativeSockaddrInet6* native = (NativeSockaddrInet6*)buffer;

            native->Family = raw.Family;
            native->Port = raw.Port;                    // already network order -- see sockaddr
            native->Flowinfo = raw.Flowinfo;
            native->Scope_id = raw.Scope_id;

            for (nint i = 0; i < 16; i++) {
                native->Addr[i] = raw.Addr[i];
            }

            return (SizeofSockaddrInet6, default!);
        }
        case ж<SockaddrUnix> box: {
            // AF_UNIX needs no mirror STRUCT -- sun_path is just bytes following the family -- but
            // it does need the same copy, and its length is the one Go computed (which encodes the
            // abstract-socket and unnamed-socket conventions, including the leading NUL it wrote
            // into raw.Path[0] for an '@' name).
            var (_, sl, err) = box.sockaddr();

            if (err != default!) {
                return (0, err);
            }

            ref var raw = ref box.Value.raw;

            *(uint16*)buffer = raw.Family;

            nint pathBytes = (nint)(uint32)sl - 2;

            for (nint i = 0; i < pathBytes; i++) {
                buffer[2 + i] = (byte)raw.Path[i];
            }

            return (sl, default!);
        }
        case ж<SockaddrLinklayer> box: {
            var (_, _, err) = box.sockaddr();

            if (err != default!) {
                return (0, err);
            }

            ref var raw = ref box.Value.raw;
            NativeSockaddrLinklayer* native = (NativeSockaddrLinklayer*)buffer;

            native->Family = raw.Family;
            native->Protocol = raw.Protocol;
            native->Ifindex = raw.Ifindex;
            native->Hatype = raw.Hatype;
            native->Pkttype = raw.Pkttype;
            native->Halen = raw.Halen;

            for (nint i = 0; i < 8; i++) {
                native->Addr[i] = raw.Addr[i];
            }

            return (SizeofSockaddrLinklayer, default!);
        }
        case ж<SockaddrNetlink> box: {
            var (_, _, err) = box.sockaddr();

            if (err != default!) {
                return (0, err);
            }

            ref var raw = ref box.Value.raw;
            NativeSockaddrNetlink* native = (NativeSockaddrNetlink*)buffer;

            native->Family = raw.Family;
            native->Pad = raw.Pad;
            native->Pid = raw.Pid;
            native->Groups = raw.Groups;

            return (SizeofSockaddrNetlink, default!);
        }
        default:
            return (0, EAFNOSUPPORT);
        }
    }

    // Decodes the native sockaddr the kernel just wrote into the Sockaddr the Go caller expects --
    // Go's anyToSockaddr, arm for arm, over a native image instead of a reinterpreted managed
    // struct. The one definition of that decode: Getsockname, Getpeername, Accept4 and
    // anyToSockaddr all land here.
    private static unsafe (Sockaddr, error) readNativeSockaddr(byte* buffer, _Socklen len) {
        uint16 family = *(uint16*)buffer;

        if (family == AF_NETLINK) {
            NativeSockaddrNetlink* native = (NativeSockaddrNetlink*)buffer;
            var sa = @new<SockaddrNetlink>();

            sa.Value.Family = native->Family;
            sa.Value.Pad = native->Pad;
            sa.Value.Pid = native->Pid;
            sa.Value.Groups = native->Groups;

            return (new SockaddrNetlinkжSockaddr(sa), default!);
        }

        if (family == AF_PACKET) {
            NativeSockaddrLinklayer* native = (NativeSockaddrLinklayer*)buffer;
            var sa = @new<SockaddrLinklayer>();

            sa.Value.Protocol = native->Protocol;
            sa.Value.Ifindex = (nint)native->Ifindex;
            sa.Value.Hatype = native->Hatype;
            sa.Value.Pkttype = native->Pkttype;
            sa.Value.Halen = native->Halen;

            var addr = new array<byte>(8);

            for (nint i = 0; i < 8; i++) {
                addr[i] = native->Addr[i];
            }

            sa.Value.Addr = addr;

            return (new SockaddrLinklayerжSockaddr(sa), default!);
        }

        if (family == AF_UNIX) {
            var sa = @new<SockaddrUnix>();

            // Go rewrites a leading NUL as '@' for textual display of an abstract socket and then
            // reads the name up to the first NUL, bounded by len(Path) = 108; the bound here is the
            // same, clipped to the length the kernel reported.
            nint pathMax = (nint)(uint32)len - 2;

            if (pathMax > 108) {
                pathMax = 108;
            }

            if (pathMax > 0 && buffer[2] == 0) {
                buffer[2] = (byte)'@';
            }

            nint n = 0;

            while (n < pathMax && buffer[2 + n] != 0) {
                n++;
            }

            sa.Value.Name = new @string(new ReadOnlySpan<byte>(buffer + 2, (int)n));

            return (new SockaddrUnixжSockaddr(sa), default!);
        }

        if (family == AF_INET) {
            NativeSockaddrInet4* native = (NativeSockaddrInet4*)buffer;
            var sa = @new<SockaddrInet4>();

            sa.Value.Port = (nint)swapBytes(native->Port);

            var addr = new array<byte>(4);

            for (nint i = 0; i < 4; i++) {
                addr[i] = native->Addr[i];
            }

            sa.Value.Addr = addr;

            return (new SockaddrInet4жSockaddr(sa), default!);
        }

        if (family == AF_INET6) {
            NativeSockaddrInet6* native = (NativeSockaddrInet6*)buffer;
            var sa = @new<SockaddrInet6>();

            sa.Value.Port = (nint)swapBytes(native->Port);
            sa.Value.ZoneId = native->Scope_id;

            var addr = new array<byte>(16);

            for (nint i = 0; i < 16; i++) {
                addr[i] = native->Addr[i];
            }

            sa.Value.Addr = addr;

            return (new SockaddrInet6жSockaddr(sa), default!);
        }

        return (default!, EAFNOSUPPORT);
    }

    // (2) THE STRUCT-PASSING SEAM. Bind/Connect build the native image in a stack buffer and hand
    // its address to the package's own generated wrapper, which already does the right thing with
    // an address -- so the errno handling and the call shape stay exactly where the converter put
    // them.
    public static unsafe error /*err*/ Bind(nint fd, Sockaddr sa) {
        byte* buffer = stackalloc byte[nativeSockaddrLen];
        var (n, err) = writeNativeSockaddr(sa, buffer);

        if (err != default!) {
            return err;
        }

        return bind(fd, new @unsafe.Pointer((uintptr)(void*)buffer), n);
    }

    public static unsafe error /*err*/ Connect(nint fd, Sockaddr sa) {
        byte* buffer = stackalloc byte[nativeSockaddrLen];
        var (n, err) = writeNativeSockaddr(sa, buffer);

        if (err != default!) {
            return err;
        }

        return connect(fd, new @unsafe.Pointer((uintptr)(void*)buffer), n);
    }

    // Getsockname/Getpeername go through the RawSyscall trampoline directly rather than their
    // generated wrappers, because those take a typed `ж<RawSockaddrAny>` -- the very managed struct
    // that cannot cross the boundary -- rather than an address. The error handling below mirrors
    // the generated wrappers exactly (errnoErr of the trap's errno); the kernel writes the address
    // into the stack buffer and its length into `addrlen`.
    public static unsafe (Sockaddr sa, error err) Getsockname(nint fd) {
        byte* buffer = stackalloc byte[nativeSockaddrLen];
        _Socklen addrlen = nativeSockaddrLen;

        var (_, _, e1) = RawSyscall(SYS_GETSOCKNAME, (uintptr)fd, (uintptr)(void*)buffer, (uintptr)(void*)(&addrlen));

        if (e1 != 0) {
            return (default!, errnoErr(e1));
        }

        return readNativeSockaddr(buffer, addrlen);
    }

    public static unsafe (Sockaddr sa, error err) Getpeername(nint fd) {
        byte* buffer = stackalloc byte[nativeSockaddrLen];
        _Socklen addrlen = nativeSockaddrLen;

        var (_, _, e1) = RawSyscall(SYS_GETPEERNAME, (uintptr)fd, (uintptr)(void*)buffer, (uintptr)(void*)(&addrlen));

        if (e1 != 0) {
            return (default!, errnoErr(e1));
        }

        return readNativeSockaddr(buffer, addrlen);
    }

    // Accept4 is Go's own body (syscall_linux.go) over the trampoline instead of the typed generated
    // wrapper: the kernel fills the stack buffer, the "RawSockaddrAny too small" panic and the
    // close-on-decode-failure are kept verbatim. Accept (syscall_linux_accept4.go) is pure Go over
    // this and stays auto.
    public static unsafe (nint nfd, Sockaddr sa, error err) Accept4(nint fd, nint flags) {
        byte* buffer = stackalloc byte[nativeSockaddrLen];
        _Socklen addrlen = nativeSockaddrLen;

        var (r0, _, e1) = Syscall6(SYS_ACCEPT4, (uintptr)fd, (uintptr)(void*)buffer, (uintptr)(void*)(&addrlen), (uintptr)flags, 0, 0);
        nint nfd = (nint)r0;

        if (e1 != 0) {
            return (nfd, default!, errnoErr(e1));
        }

        if (addrlen > SizeofSockaddrAny) {
            throw panic("RawSockaddrAny too small");
        }

        var (sa, err) = readNativeSockaddr(buffer, addrlen);

        if (err != default!) {
            Close(nfd);
            nfd = 0;
        }

        return (nfd, sa, err);
    }

    // THE DECODE. Go reinterprets the RawSockaddrAny as a RawSockaddrInet4/6/Unix/… and reads the
    // port through the same two-byte alias the encoders write it through, so the auto conversion
    // panics identically; and `Ꮡrsa.Reinterpret<RawSockaddrAny, RawSockaddrInet4>()` asks golib to
    // alias one reference-bearing struct as another, which it correctly refuses. So the decode is
    // written the only way that is true on both sides: FLATTEN the managed struct back to the
    // 112-byte native image its fields are a transcription of -- Family at 0, Addr.Data at 2..15,
    // Pad at 16..111 (unsafe.Sizeof(RawSockaddrAny{}) = SizeofSockaddrAny) -- and hand that to the
    // one definition of the decode. Go's in-place '@' rewrite of an abstract socket's leading NUL
    // is reproduced on the managed struct too, so the observable state after the call is Go's.
    internal static unsafe (Sockaddr, error) anyToSockaddr(ж<RawSockaddrAny> Ꮡrsa) {
        ref var rsa = ref Ꮡrsa.Value;

        if (rsa.Addr.Family == AF_UNIX && rsa.Addr.Data[0] == 0) {
            rsa.Addr.Data[0] = (int8)'@';
        }

        byte* buffer = stackalloc byte[nativeSockaddrLen];

        *(uint16*)buffer = rsa.Addr.Family;

        for (nint i = 0; i < 14; i++) {
            buffer[2 + i] = (byte)rsa.Addr.Data[i];
        }

        for (nint i = 0; i < 96; i++) {
            buffer[16 + i] = (byte)rsa.Pad[i];
        }

        return readNativeSockaddr(buffer, SizeofSockaddrAny);
    }

    // ---- the datagram seam: what internal/syscall/unix's hand-own consumes ------------------------
    //
    // DESIGN-linux-udp.md ⟨OQ-2⟩, RULED: the eight //go:linkname datagram helpers
    // (internal/syscall/unix/linux/net_impl.cs) need this file's native encode/decode, and they live
    // in a DIFFERENT assembly. The ruling is to expose them here rather than duplicate the layout
    // there, for the reason the mirror exists at all: there must be ONE definition of what a Go
    // Sockaddr looks like to the kernel. These four wrappers are that seam and nothing else --
    // deliberately spelled `Go…` so they read as go2cs machinery rather than as Go API (Go's syscall
    // package has no such functions), and typed to the two INET families so the caller carries no
    // layout knowledge at all: it hands over a box and a buffer.
    //
    // Keep in step with net_impl.cs; if a family is added there, add its pair here rather than
    // reaching into the private helpers from outside.

    // The size every caller's stack buffer must be: sockaddr_storage, which fits every family below.
    public const int GoNativeSockaddrLen = nativeSockaddrLen;

    // IN direction (sendto/sendmsg): managed box -> native image, returning the addrlen to pass.
    public static unsafe (_Socklen len, error err) GoWriteNativeSockaddrInet4(ж<SockaddrInet4> sa, byte* buffer) =>
        writeNativeSockaddr(new SockaddrInet4жSockaddr(sa), buffer);

    public static unsafe (_Socklen len, error err) GoWriteNativeSockaddrInet6(ж<SockaddrInet6> sa, byte* buffer) =>
        writeNativeSockaddr(new SockaddrInet6жSockaddr(sa), buffer);

    // OUT direction (recvfrom/recvmsg): native image -> the caller's OWN box, filled by assignment.
    // Go's recvfromInet4 assigns Port and Addr into the caller's struct and leaves everything else
    // alone; these do the same, so a caller that reused a box sees exactly Go's fields change. A
    // datagram that decodes to another family is a kernel contract violation on an AF_INET socket,
    // and is reported rather than silently ignored.
    public static unsafe error GoReadNativeSockaddrInet4(byte* buffer, _Socklen len, ж<SockaddrInet4> into) {
        var (sa, err) = readNativeSockaddr(buffer, len);

        if (err != default!) {
            return err;
        }

        if ((sa as IжAdapter)?.Box is not ж<SockaddrInet4> box) {
            return EAFNOSUPPORT;
        }

        into.Value.Port = box.Value.Port;
        into.Value.Addr = box.Value.Addr;
        return default!;
    }

    public static unsafe error GoReadNativeSockaddrInet6(byte* buffer, _Socklen len, ж<SockaddrInet6> into) {
        var (sa, err) = readNativeSockaddr(buffer, len);

        if (err != default!) {
            return err;
        }

        if ((sa as IжAdapter)?.Box is not ж<SockaddrInet6> box) {
            return EAFNOSUPPORT;
        }

        into.Value.Port = box.Value.Port;
        into.Value.ZoneId = box.Value.ZoneId;
        into.Value.Addr = box.Value.Addr;
        return default!;
    }
}
