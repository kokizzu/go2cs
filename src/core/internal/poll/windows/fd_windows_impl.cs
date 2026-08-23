// fd_windows_impl.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// Hand-written implementations of fd_windows.go's two raw-sockaddr DECODERS, rawToSockaddrInet4 and
// rawToSockaddrInet6 -- the last managed-representation obstacle on the Windows datagram read path
// (docs/phase4/DESIGN-netpoll-managed-poller.md §4.8, RATIFIED).
//
// WHY THEY CANNOT BE CONVERTED. Go reads the address by pointer arithmetic over flat bytes:
//
//     pp := (*syscall.RawSockaddrInet4)(unsafe.Pointer(rsa))
//     p  := (*[2]byte)(unsafe.Pointer(&pp.Port))
//     sa.Port = int(p[0])<<8 + int(p[1])
//     sa.Addr = pp.Addr
//
// Neither line survives the managed representation, and they fail in DIFFERENT ways -- which is why
// this is a hand-own rather than a patch to either mechanism:
//
//   1. The REINTERPRET. `Ꮡrsa.Reinterpret<RawSockaddrAny, RawSockaddrInet4>()` asks golib to alias
//      one reference-bearing struct as another. The two managed layouts share no field offsets at
//      all: RawSockaddrAny holds `int8[14]` and `int8[100]` OBJECT REFERENCES where sockaddr_in has
//      four inline octets. Measured -- the reinterpreted `Addr` reports Length=14 (that is
//      RawSockaddr.Data) and `Zero` reports Length=100 (that is Pad). Line 4 above therefore clones
//      the WRONG FIELD, not a mis-sized one.
//   2. The BYTE VIEW. `(ж<array<byte>>)(uintptr)(…ᏑPort)` reinterprets the pointed-at bytes as a
//      managed `array<byte>` STRUCT -- whose first field is a `T[]` reference. Over zeroed memory it
//      reads null and yields Length=0 (a silent wrong answer); over real data it fabricates a
//      managed reference out of the sockaddr's own bytes and dereferences it. That is the
//      corpus-wide class DESIGN-native-array-view.md exists to close; this file does not wait for it.
//
// WHAT THEY DO INSTEAD is not a third mechanism but the one the package next door already ships.
// `Δsyscall.RawSockaddrAny.Sockaddr()` is hand-owned and does exactly this decode correctly: it
// FLATTENS the managed struct back to the 116-byte native image its fields are a transcription of
// (Family at 0, Addr.Data covering 2..15, Pad covering 16..115) and hands that to the single
// definition of the decode. Routing through it means the sockaddr layout is spelled in exactly one
// place in the corpus, and these two functions carry none of it.
//
// The managed image is faithful because the SUBMIT seam makes it so -- syscall's hand-owned
// WSARecvFrom stages a native buffer, lets the kernel write THERE, and transcribes the result into
// this box at harvest. Without that half the box holds whatever the kernel scribbled over a
// forty-byte managed object, and no decode could help. The two are a pair, exactly as
// AcceptEx/GetAcceptExSockaddrs are.
//
// ONE DELIBERATE DIVERGENCE, and it is in the safe direction. Go blindly reinterprets whatever
// family the raw address carries; these check it, and leave `sa` at its zero value on a mismatch
// rather than filling it with nonsense. Every caller (ReadFromInet4/6, ReadMsgInet4/6) has already
// bound the socket to that family, so a mismatch is unreachable in practice -- and where Go would
// hand back a garbage address, this hands back an empty one.

using System;
using golib = go.golib;

// Hand-owned (no fd_windows_impl.go exists, so a reconvert never regenerates this file); the two
// declarations it replaces are registered in the converter's manualConversionFuncs, which is what
// turns their generated bodies into placeholders.
[module: go.GoManualConversion]

namespace go.@internal;

// Δsyscall, not syscall: the enclosing namespace go.@internal already contains a `syscall`
// (internal/syscall), and a plain alias collides with it (CS0576). The generated fd_windows.cs
// spells it the same way for the same reason.
using Δsyscall = go.syscall_package;

partial class poll_package
{
    internal static void rawToSockaddrInet4(ж<Δsyscall.RawSockaddrAny> Ꮡrsa, ref Δsyscall.SockaddrInet4 sa) {
        var (decoded, err) = Ꮡrsa.Sockaddr();

        if (err != default!) {
            return;
        }
        if (decoded.type() is ж<Δsyscall.SockaddrInet4> Ꮡv4) {
            sa.Port = (~Ꮡv4).Port;
            sa.Addr = (~Ꮡv4).Addr.Clone();
        }
    }

    internal static void rawToSockaddrInet6(ж<Δsyscall.RawSockaddrAny> Ꮡrsa, ref Δsyscall.SockaddrInet6 sa) {
        var (decoded, err) = Ꮡrsa.Sockaddr();

        if (err != default!) {
            return;
        }
        if (decoded.type() is ж<Δsyscall.SockaddrInet6> Ꮡv6) {
            sa.Port = (~Ꮡv6).Port;
            sa.ZoneId = (~Ꮡv6).ZoneId;
            sa.Addr = (~Ꮡv6).Addr.Clone();
        }
    }
}
