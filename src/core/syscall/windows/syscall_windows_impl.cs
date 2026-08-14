// syscall_windows_impl.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// Hand-written implementations of the SOCKET-ADDRESS surface -- the sockaddr family of the syscall
// STRUCT-PASSING seam catalogued in docs/phase4/BOARD-next-validation-candidates.md. This is the
// member `net` forces: net.Listen on Windows died before any test logic ran, which walled net/smtp,
// net/http/cgi, net/http/httptest, net/rpc and eventually net itself.
//
// TWO defects sit on this path, and only fixing both makes a socket work.
//
// (1) THE PORT ALIAS. Go writes the port in network byte order through a two-byte alias over the
// raw struct's port field:
//
//     p := (*[2]byte)(unsafe.Pointer(&sa.raw.Port))
//     p[0] = byte(sa.Port >> 8)
//     p[1] = byte(sa.Port)
//
// The auto conversion of that is `var p = (ж<array<byte>>)(uintptr)(new @unsafe.Pointer(...))`, and
// an `array<T>` reconstructed from a raw address materializes `default(array<byte>)` -- a
// LENGTH-ZERO array -- so `p[0]` panics with `index out of range [0] with length 0` (golib
// array.cs:280 via syscall_windows.cs:881). `array<T>` is a managed container with its own header,
// not two inline bytes, so NO address reinterpret can produce one; the remedy is to stop aliasing
// and write the field arithmetically, which is what the sockaddr/Sockaddr methods below do.
//
// (2) THE STRUCT-PASSING SEAM. Even with the port written, `Bind` hands the kernel
// `unsafe.Pointer(&sa.raw)`. Native `sockaddr_in` is 16 bytes with the address and zero padding
// INLINE; the converted RawSockaddrInet4 holds `Addr [4]byte` / `Zero [8]uint8` as golib
// `array<byte>` MANAGED REFERENCES, so its C# layout is ~24 bytes with object references where
// Windows expects address octets. golib states the consequence itself, in ж.cs's note on why a
// reference-bearing pointee has no pinnable storage: "such a value's C# layout is not a native
// layout either, so no syscall can meaningfully be handed its address."
//
// The remedy is the established one -- a blittable [StructLayout(LayoutKind.Sequential)] mirror
// with `fixed` buffers for the inline arrays and an explicit field-for-field copy at the boundary,
// worked out for GetTimeZoneInformation / findFirstFile1 / Process32First in the sibling
// zsyscall_windows_impl.cs. Two things differ here and are worth stating:
//
//   - The mirror is a LOCAL at the call site, never a field. A sockaddr's native image is needed
//     only for the duration of one call, and a stack buffer is trivially stable for exactly that
//     long -- where a managed field's address would need a pin whose lifetime nothing owns.
//   - No new [LibraryImport] is declared. Because golib models `unsafe.Pointer` as a box over a
//     plain address, the package's OWN generated wrappers (`bind`, `connect`, `connectEx`) already
//     accept any address at all -- they were never the broken part. Handing them the mirror's
//     address reuses their existing errno handling verbatim and keeps the hand-owned surface to
//     the layout translation, which is the only thing that was actually wrong.
//     Getsockname/Getpeername are the exception: their generated wrappers take a typed
//     `ж<RawSockaddrAny>` rather than an address, so those two go through the package's Syscall
//     trampoline directly, mirroring the generated wrappers' error handling exactly.
//
// DELIBERATELY NOT COVERED, and each for its own measured reason.
//
//   - WAS deliberately excluded, and is NOW covered: RawSockaddrAny.Sockaddr, the DECODE. It
//     carries the same port alias as the encoders and panics identically wherever it is reached.
//     Hand-owning it was REJECTED at L10 on measurement, not on effort: the only casts of the three
//     Sockaddr types to ΔSockaddr in the package lived in ITS body, so skipping its emission dropped
//     the `[assembly: GoImplement<…>(Pointer = true)]` records from package_info.cs, and a reconvert
//     of `net` against that shortened package_info showed net minting its own
//     `syscall_SockaddrInet4жΔSockaddr` adapters instead of using syscall's -- the SECOND-IDENTITY
//     regression samePackageImplements.go exists to prevent (reflect and fmt see the wrapper where
//     the value's own type belongs, and a direct-boxed value compares unequal to an adapter-wrapped
//     one). Declaring the records in this file does not help either: a DEPENDENT package's converter
//     run reads package_info.cs, not this file.
//
//     The converter increment L10 named as the real answer has since landed --
//     recordSamePackageImplements records the POINTER method set as well as the value one, so the
//     three records are sourced from types.Implements(*T, Sockaddr) and survive the body's
//     suppression. Re-measured on this lane's own build before the decode was taken (see the method
//     below): records present, `net` still referencing syscall's adapters.
//   - WSASendto / wsaSendtoInet4 / wsaSendtoInet6 -- the UDP send path -- still pass the address
//     returned by `sockaddr()`, which for the reasons above is not a native image. They are out of
//     this lane's scope (the board's ruling is to fix a censused wrapper when a suite REACHES it,
//     never speculatively), and nothing in the TCP listen/dial/accept path touches them. They are
//     named here rather than left to be rediscovered: writeNativeSockaddr is what they would need.

using System;
using System.Runtime.InteropServices;

// The same two aliases syscall_windows.cs declares for itself -- the declarations replaced below
// are its neighbors and use both. A converted file's aliases are file-scoped, so a hand-owned
// companion restates the ones it needs.
using @unsafe = go.unsafe_package;
using errorspkg = go.errors_package;

// Hand-owned (no syscall_windows_impl.go exists, so a reconvert never regenerates this file);
// marked for consistency with the other hand-owned operational files in this package. The
// declarations it replaces are registered in the converter's manualConversionFuncs, which is what
// turns their generated bodies into placeholders.
[module: go.GoManualConversion]

// The blittable mirrors below need `fixed` buffers, and the encode/decode helpers take raw
// pointers into stack buffers. Declared rather than inherited -- see zsyscall_windows_impl.cs.
[module: go.GoRequiresUnsafe]

namespace go;

partial class syscall_package
{
    // sockaddr_storage is the largest address any of these calls can carry (128 bytes); every
    // encode and decode below works in a buffer of this size, so one constant covers the stack
    // allocations and the `addrlen` the kernel is told it has.
    private const int nativeSockaddrLen = 128;

    // sockaddr_in exactly as Windows lays it out: 16 bytes, the address and the trailing pad
    // INLINE. `fixed` is what keeps them inline -- a C# array field would be another managed
    // reference, which is the whole bug.
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

    // Go stores the port as the two bytes `p[0] = hi, p[1] = lo` -- i.e. network byte order IN
    // MEMORY -- so a little-endian load of that field is the byte-SWAPPED port, which is exactly
    // what sockaddr_in.sin_port carries on the wire. The swap is its own inverse, so encode and
    // decode share it.
    private static uint16 swapBytes(uint16 value) {
        return (uint16)((value >> 8) | (value << 8));
    }

    // (1) THE PORT ALIAS, IPv4. Identical to Go's body except that the port is written to the
    // field instead of through a two-byte alias over it; `raw` is left in exactly the state Go
    // leaves it, so anything that reads it afterwards reads Go's answer.
    internal static (@unsafe.Pointer, int32, error) sockaddr(this ж<SockaddrInet4> Ꮡsa) {
        ref var sa = ref Ꮡsa.Value;

        if (sa.Port < 0 || sa.Port > 0xFFFF) {
            return (default!, 0, EINVAL);
        }

        sa.raw.Family = AF_INET;
        sa.raw.Port = swapBytes((uint16)sa.Port);
        sa.raw.Addr = sa.Addr.Clone();

        // The returned pointer keeps the Go shape and the Go meaning -- the address of `sa.raw`.
        // It is NOT a native image, for the layout reason in the file header, which is why every
        // in-package caller that actually reaches the kernel builds one with writeNativeSockaddr
        // instead of consuming this.
        return (new @unsafe.Pointer(Ꮡsa.of(SockaddrInet4.Ꮡraw)), (int32)16, default!);
    }

    // (1) THE PORT ALIAS, IPv6. See the IPv4 method above.
    internal static (@unsafe.Pointer, int32, error) sockaddr(this ж<SockaddrInet6> Ꮡsa) {
        ref var sa = ref Ꮡsa.Value;

        if (sa.Port < 0 || sa.Port > 0xFFFF) {
            return (default!, 0, EINVAL);
        }

        sa.raw.Family = AF_INET6;
        sa.raw.Port = swapBytes((uint16)sa.Port);
        sa.raw.Scope_id = sa.ZoneId;
        sa.raw.Addr = sa.Addr.Clone();

        return (new @unsafe.Pointer(Ꮡsa.of(SockaddrInet6.Ꮡraw)), (int32)28, default!);
    }

    // Encodes a Sockaddr into the caller's stack buffer as the native sockaddr Windows expects,
    // returning the byte length to pass as `namelen`. Go's own validation and raw-filling logic is
    // reused by calling sockaddr() first -- so there is ONE definition of what a Sockaddr means and
    // this function does nothing but translate the layout, which is the only thing the conversion
    // gets wrong.
    private static unsafe (int32 len, error err) writeNativeSockaddr(ΔSockaddr sa, byte* buffer) {
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

            return (16, default!);
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

            return (28, default!);
        }
        case ж<SockaddrUnix> box: {
            // AF_UNIX needs no mirror STRUCT -- sun_path is just bytes following the family -- but
            // it does need the same copy, and its length is the one Go computed (which encodes the
            // abstract-socket and unnamed-socket conventions).
            var (_, sl, err) = box.sockaddr();

            if (err != default!) {
                return (0, err);
            }

            ref var raw = ref box.Value.raw;

            *(uint16*)buffer = raw.Family;

            for (nint i = 0; i < (nint)sl - 2; i++) {
                buffer[2 + i] = (byte)raw.Path[i];
            }

            return (sl, default!);
        }
        default:
            return (0, EAFNOSUPPORT);
        }
    }

    // Decodes the native sockaddr the kernel just wrote into the Sockaddr the Go caller expects.
    // The inverse of writeNativeSockaddr, and the one definition of that decode: Getsockname,
    // Getpeername and RawSockaddrAny.Sockaddr all land here.
    private static unsafe (ΔSockaddr, error) readNativeSockaddr(byte* buffer, int32 len) {
        uint16 family = *(uint16*)buffer;

        if (family == AF_INET) {
            NativeSockaddrInet4* native = (NativeSockaddrInet4*)buffer;
            var sa = @new<SockaddrInet4>();

            sa.Value.Port = (nint)swapBytes(native->Port);

            var addr = new array<byte>(4);

            for (nint i = 0; i < 4; i++) {
                addr[i] = native->Addr[i];
            }

            sa.Value.Addr = addr;

            return (new SockaddrInet4жΔSockaddr(sa), default!);
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

            return (new SockaddrInet6жΔSockaddr(sa), default!);
        }

        if (family == AF_UNIX) {
            var sa = @new<SockaddrUnix>();
            // sun_path runs from offset 2 to the reported length; Go rewrites a leading NUL as '@'
            // for textual display of an abstract socket, and otherwise stops at the first NUL.
            nint pathMax = (nint)len - 2;

            if (pathMax > (nint)UNIX_PATH_MAX) {
                pathMax = (nint)UNIX_PATH_MAX;
            }

            nint n = 0;

            while (n < pathMax && buffer[2 + n] != 0) {
                n++;
            }

            if (n == 0 && pathMax > 0 && buffer[2] == 0) {
                // Abstract socket: leading NUL displayed as '@', then the name up to the length.
                var abstractName = new array<byte>(pathMax);
                abstractName[0] = (byte)'@';

                nint m = 1;

                while (m < pathMax && buffer[2 + m] != 0) {
                    abstractName[m] = buffer[2 + m];
                    m++;
                }

                sa.Value.Name = ((@string)@unsafe.Slice(Ꮡ(abstractName, 0), m));

                return (new SockaddrUnixжΔSockaddr(sa), default!);
            }

            var name = new array<byte>(n);

            for (nint i = 0; i < n; i++) {
                name[i] = buffer[2 + i];
            }

            sa.Value.Name = ((@string)@unsafe.Slice(Ꮡ(name, 0), n));

            return (new SockaddrUnixжΔSockaddr(sa), default!);
        }

        return (default!, EAFNOSUPPORT);
    }

    // (2) THE STRUCT-PASSING SEAM. Bind/Connect/ConnectEx each build the native image in a stack
    // buffer and hand its address to the package's own generated wrapper, which already does the
    // right thing with an address (see the file header) -- so the errno handling, the trap lookup
    // and the call shape all stay exactly where the converter put them.
    public static unsafe error /*err*/ Bind(ΔHandle fd, ΔSockaddr sa) {
        byte* buffer = stackalloc byte[nativeSockaddrLen];
        var (n, err) = writeNativeSockaddr(sa, buffer);

        if (err != default!) {
            return err;
        }

        return bind(fd, new @unsafe.Pointer((uintptr)(void*)buffer), n);
    }

    public static unsafe error /*err*/ Connect(ΔHandle fd, ΔSockaddr sa) {
        byte* buffer = stackalloc byte[nativeSockaddrLen];
        var (n, err) = writeNativeSockaddr(sa, buffer);

        if (err != default!) {
            return err;
        }

        return connect(fd, new @unsafe.Pointer((uintptr)(void*)buffer), n);
    }

    public static unsafe error ConnectEx(ΔHandle fd, ΔSockaddr sa, ж<byte> ᏑsendBuf, uint32 sendDataLen, ж<uint32> ᏑbytesSent, ж<Overlapped> Ꮡoverlapped) {
        var err = LoadConnectEx();

        if (err != default!) {
            return errorspkg.New("failed to find ConnectEx: "u8 + err.Error());
        }

        byte* buffer = stackalloc byte[nativeSockaddrLen];
        int32 n;
        (n, err) = writeNativeSockaddr(sa, buffer);

        if (err != default!) {
            return err;
        }

        return connectEx(fd, new @unsafe.Pointer((uintptr)(void*)buffer), n, ᏑsendBuf, sendDataLen, ᏑbytesSent, Ꮡoverlapped);
    }

    // THE DECODE, and the third consumer readNativeSockaddr was written for. Go reinterprets the
    // RawSockaddrAny as a RawSockaddrInet4/6/Unix and then reads the port through the SAME two-byte
    // alias the encoders write it through -- so the auto conversion panics identically
    // (`index out of range [0] with length 0`), and net's ACCEPT path is the one route that reaches
    // it: netFD.accept decodes the GetAcceptExSockaddrs output with it
    // (net/windows/fd_windows.cs:255-256). Getsockname/Getpeername above never go near it.
    //
    // Neither the alias NOR the reinterpret survives the boundary, and the second is the deeper
    // reason this is hand-owned rather than patched. `Ꮡrsa.Reinterpret<RawSockaddrAny,
    // RawSockaddrInet4>()` asks golib to alias one reference-bearing struct as another, which it
    // correctly refuses (the two managed layouts share no field offsets at all -- RawSockaddrAny
    // holds an int8[14] and an int8[100] object reference where sockaddr_in has four inline octets).
    // So the decode is written the only way that is true on both sides: FLATTEN the managed struct
    // back to the 116-byte native image its fields are a transcription of, and hand that to the one
    // definition of the decode. The mapping is the Go declaration's own -- Family at 0, Addr.Data
    // covering 2..15, Pad covering 16..115 -- and nothing else in the corpus knows it, which is why
    // it is spelled out here rather than derived at the call site.
    //
    // WHO FILLS THE MANAGED STRUCT is the other half, and it is the submit seam's: the hand-owned
    // GetAcceptExSockaddrs (zsyscall_windows_wsa_impl.cs) transcribes the kernel's native accept
    // buffer INTO managed RawSockaddrAny values field for field, precisely so this method has a
    // faithful managed image to read. The two are a pair; neither is meaningful alone.
    public static unsafe (ΔSockaddr, error) Sockaddr(this ж<RawSockaddrAny> Ꮡrsa) {
        ref var rsa = ref Ꮡrsa.Value;

        // Go rewrites a leading NUL as '@' IN PLACE for an abstract Unix socket, with its own note
        // that "the callers below don't care" -- reproduced anyway so the observable state of the
        // caller's struct after the call is Go's, not merely the return value.
        if (rsa.Addr.Family == AF_UNIX && rsa.Addr.Data[0] == 0) {
            rsa.Addr.Data[0] = (int8)'@';
        }

        byte* buffer = stackalloc byte[nativeSockaddrLen];

        // Family is a plain uint16 in host order on both sides -- the port is the field that is not.
        *(uint16*)buffer = rsa.Addr.Family;

        for (nint i = 0; i < 14; i++) {
            buffer[2 + i] = (byte)rsa.Addr.Data[i];
        }

        // 2 + 14 + 100 = 116, which is what Go's unsafe.Sizeof(RawSockaddrAny{}) reports and what
        // internal/poll hard-codes as the AcceptEx per-address length; nativeSockaddrLen (128, a
        // sockaddr_storage) covers it with room to spare.
        for (nint i = 0; i < 100; i++) {
            buffer[16 + i] = (byte)rsa.Pad[i];
        }

        // The length matters only to the AF_UNIX arm, which scans sun_path for a NUL: Go bounds that
        // scan by len(RawSockaddrUnix.Path), so the equivalent bound here is 2 + UNIX_PATH_MAX.
        return readNativeSockaddr(buffer, (int32)(2 + UNIX_PATH_MAX));
    }

    // Getsockname/Getpeername go through the Syscall trampoline directly rather than their
    // generated wrappers, because those take a typed `ж<RawSockaddrAny>` -- the very managed struct
    // that cannot cross the boundary -- rather than an address. The error handling below mirrors
    // the generated wrappers exactly (`socket_error` result, errnoErr of the trap's errno).
    public static unsafe (ΔSockaddr sa, error err) Getsockname(ΔHandle fd) {
        byte* buffer = stackalloc byte[nativeSockaddrLen];
        int32 addrlen = nativeSockaddrLen;

        var (r1, _, e1) = Syscall(procgetsockname.Addr(), 3, (uintptr)fd, (uintptr)(void*)buffer, (uintptr)(void*)(&addrlen));

        if (r1 == socket_error) {
            return (default!, errnoErr(e1));
        }

        return readNativeSockaddr(buffer, addrlen);
    }

    public static unsafe (ΔSockaddr sa, error err) Getpeername(ΔHandle fd) {
        byte* buffer = stackalloc byte[nativeSockaddrLen];
        int32 addrlen = nativeSockaddrLen;

        var (r1, _, e1) = Syscall(procgetpeername.Addr(), 3, (uintptr)fd, (uintptr)(void*)buffer, (uintptr)(void*)(&addrlen));

        if (r1 == socket_error) {
            return (default!, errnoErr(e1));
        }

        return readNativeSockaddr(buffer, addrlen);
    }
}
