// Copyright 2009 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build darwin || dragonfly || freebsd || netbsd || openbsd
// BSD system call wrappers shared by *BSD based systems
// including OS X (Darwin) and FreeBSD.  Like the other
// syscall_*.go files it is compiled as Go code but also
// used as input to mksyscall which parses the //sys
// lines and generates system call stubs.
namespace go;

using Δruntime = runtime_package;
using @unsafe = unsafe_package;

partial class syscall_package {

public const bool ImplementsGetwd = true;

public static (@string, error) Getwd() {
    array<byte> buf = new(1024); /* pathMax */
    var (_, err) = getcwd(buf[..]);
    if (err != default!) {
        return ("", err);
    }
    nint n = clen(buf[..]);
    if (n < 1) {
        return ("", EINVAL);
    }
    return (((@string)(buf[..(int)(n)])), default!);
}

/*
 * Wrapped
 */
//sysnb	getgroups(ngid int, gid *_Gid_t) (n int, err error)
//sysnb	setgroups(ngid int, gid *_Gid_t) (err error)
public static (slice<nint> gids, error err) Getgroups() {
    slice<nint> gids = default!;
    error err = default!;

    (var n, err) = getgroups(0, nil);
    if (err != default!) {
        return (default!, err);
    }
    if (n == 0) {
        return (default!, default!);
    }
    // Sanity check group count. Max is 16 on BSD.
    if (n < 0 || n > 1000) {
        return (default!, EINVAL);
    }
    var a = new slice<_Gid_t>(n);
    (n, err) = getgroups(n, Ꮡ(a, 0));
    if (err != default!) {
        return (default!, err);
    }
    gids = new slice<nint>(n);
    foreach (var (i, v) in a[0..(int)(n)]) {
        gids[i] = (nint)(uint32)v;
    }
    return (gids, err);
}

public static error /*err*/ Setgroups(slice<nint> gids) {
    if (len(gids) == 0) {
        return setgroups(0, nil);
    }
    var a = new slice<_Gid_t>(len(gids));
    foreach (var (i, v) in gids) {
        a[i] = ((_Gid_t)(uint32)v);
    }
    return setgroups(len(a), Ꮡ(a, 0));
}

public static (nint n, error err) ReadDirent(nint fd, slice<byte> buf) {
    // Final argument is (basep *uintptr) and the syscall doesn't take nil.
    // 64 bits should be enough. (32 bits isn't even on 386). Since the
    // actual system call is getdirentries64, 64 is a good guess.
    // TODO(rsc): Can we use a single global basep for all calls?
    ж<uintptr> @base = @new<uint64>().Reinterpret<uint64, uintptr>();
    return Getdirentries(fd, buf, @base);
}

[GoType("num:uint32")] partial struct WaitStatus;

// Wait status is 7 bits at bottom, either 0 (exited),
// 0x7F (stopped), or a signal number that caused an exit.
// The 0x80 bit is whether there was a core dump.
// An extra number (exit code, signal causing a stop)
// is in the high bits.
internal static UntypedInt mask => 0x7F;
internal static UntypedInt core => 0x80;
internal static UntypedInt shift => 8;
internal static UntypedInt exited => 0;
internal static UntypedInt stopped => 0x7F;

public static bool Exited(this WaitStatus w) {
    return (WaitStatus)(w & (uint32)mask) == exited;
}

public static nint ExitStatus(this WaitStatus w) {
    if ((WaitStatus)(w & (uint32)mask) != exited) {
        return -1;
    }
    return (nint)(uint32)((w >> (int)(shift)));
}

public static bool Signaled(this WaitStatus w) {
    return (WaitStatus)(w & (uint32)mask) != stopped && (WaitStatus)(w & (uint32)mask) != 0;
}

public static ΔSignal Signal(this WaitStatus w) {
    ΔSignal sig = ((ΔSignal)(nint)((uint32)((WaitStatus)(w & (uint32)mask))));
    if (sig == stopped || sig == 0) {
        return -1;
    }
    return sig;
}

public static bool CoreDump(this WaitStatus w) {
    return w.Signaled() && (WaitStatus)(w & (uint32)core) != 0;
}

public static bool Stopped(this WaitStatus w) {
    return (WaitStatus)(w & (uint32)mask) == stopped && ((ΔSignal)(nint)((uint32)((w >> (int)(shift))))) != SIGSTOP;
}

public static bool Continued(this WaitStatus w) {
    return (WaitStatus)(w & (uint32)mask) == stopped && ((ΔSignal)(nint)((uint32)((w >> (int)(shift))))) == SIGSTOP;
}

public static ΔSignal StopSignal(this WaitStatus w) {
    if (!w.Stopped()) {
        return -1;
    }
    return (ΔSignal)(((ΔSignal)(nint)((uint32)((w >> (int)(shift))))) & 0xFF);
}

public static nint TrapCause(this WaitStatus w) {
    return -1;
}

//sys	wait4(pid int, wstatus *_C_int, options int, rusage *Rusage) (wpid int, err error)
public static (nint wpid, error err) Wait4(nint pid, ж<WaitStatus> Ꮡwstatus, nint options, ж<Rusage> Ꮡrusage) {
    nint wpid = default!;
    error err = default!;

    ref var wstatus = ref Ꮡwstatus.DerefOrNull();
    ref var status = ref heap(new _C_int(), out var Ꮡstatus);
    (wpid, err) = wait4(pid, Ꮡstatus, options, Ꮡrusage);
    if (Ꮡwstatus != nil) {
        wstatus = ((WaitStatus)(uint32)(int32)status);
    }
    return (wpid, err);
}

//sys	accept(s int, rsa *RawSockaddrAny, addrlen *_Socklen) (fd int, err error)
//sys	bind(s int, addr unsafe.Pointer, addrlen _Socklen) (err error)
//sys	connect(s int, addr unsafe.Pointer, addrlen _Socklen) (err error)
//sysnb	socket(domain int, typ int, proto int) (fd int, err error)
//sys	getsockopt(s int, level int, name int, val unsafe.Pointer, vallen *_Socklen) (err error)
//sys	setsockopt(s int, level int, name int, val unsafe.Pointer, vallen uintptr) (err error)
//sysnb	getpeername(fd int, rsa *RawSockaddrAny, addrlen *_Socklen) (err error)
//sysnb	getsockname(fd int, rsa *RawSockaddrAny, addrlen *_Socklen) (err error)
//sys	Shutdown(s int, how int) (err error)
internal static (@unsafe.Pointer, _Socklen, error) sockaddr(this ж<SockaddrInet4> Ꮡsa) {
    ref var sa = ref Ꮡsa.DerefOrNull();

    if (sa.Port < 0 || sa.Port > 0xFFFF) {
        return (default!, 0, EINVAL);
    }
    sa.raw.Len = SizeofSockaddrInet4;
    sa.raw.Family = AF_INET;
    var p = (ж<array<byte>>)(uintptr)(@unsafe.Pointer.FromPinnedBox(Ꮡsa.of(SockaddrInet4.Ꮡraw).of(RawSockaddrInet4.ᏑPort)));
    p.Value[0] = (byte)((sa.Port >> (int)(8)));
    p.Value[1] = (byte)sa.Port;
    sa.raw.Addr = sa.Addr.Clone();
    return (@unsafe.Pointer.FromPinnedBox(Ꮡsa.of(SockaddrInet4.Ꮡraw)), ((_Socklen)(uint32)sa.raw.Len), default!);
}

internal static (@unsafe.Pointer, _Socklen, error) sockaddr(this ж<SockaddrInet6> Ꮡsa) {
    ref var sa = ref Ꮡsa.DerefOrNull();

    if (sa.Port < 0 || sa.Port > 0xFFFF) {
        return (default!, 0, EINVAL);
    }
    sa.raw.Len = SizeofSockaddrInet6;
    sa.raw.Family = AF_INET6;
    var p = (ж<array<byte>>)(uintptr)(@unsafe.Pointer.FromPinnedBox(Ꮡsa.of(SockaddrInet6.Ꮡraw).of(RawSockaddrInet6.ᏑPort)));
    p.Value[0] = (byte)((sa.Port >> (int)(8)));
    p.Value[1] = (byte)sa.Port;
    sa.raw.Scope_id = sa.ZoneId;
    sa.raw.Addr = sa.Addr.Clone();
    return (@unsafe.Pointer.FromPinnedBox(Ꮡsa.of(SockaddrInet6.Ꮡraw)), ((_Socklen)(uint32)sa.raw.Len), default!);
}

internal static (@unsafe.Pointer, _Socklen, error) sockaddr(this ж<SockaddrUnix> Ꮡsa) {
    ref var sa = ref Ꮡsa.DerefOrNull();

    @string name = sa.Name;
    nint n = len(name);
    if (n >= len(sa.raw.Path) || n == 0) {
        return (default!, 0, EINVAL);
    }
    sa.raw.Len = (byte)(3 + n); // 2 for Family, Len; 1 for NUL
    sa.raw.Family = AF_UNIX;
    for (nint i = 0; i < n; i++) {
        sa.raw.Path[i] = (int8)name[i];
    }
    return (@unsafe.Pointer.FromPinnedBox(Ꮡsa.of(SockaddrUnix.Ꮡraw)), ((_Socklen)(uint32)sa.raw.Len), default!);
}

internal static (@unsafe.Pointer, _Socklen, error) sockaddr(this ж<SockaddrDatalink> Ꮡsa) {
    ref var sa = ref Ꮡsa.DerefOrNull();

    if (sa.Index == 0) {
        return (default!, 0, EINVAL);
    }
    sa.raw.Len = sa.Len;
    sa.raw.Family = AF_LINK;
    sa.raw.Index = sa.Index;
    sa.raw.Type = sa.Type;
    sa.raw.Nlen = sa.Nlen;
    sa.raw.Alen = sa.Alen;
    sa.raw.Slen = sa.Slen;
    sa.raw.Data = sa.Data.Clone();
    return (@unsafe.Pointer.FromPinnedBox(Ꮡsa.of(SockaddrDatalink.Ꮡraw)), SizeofSockaddrDatalink, default!);
}

internal static (Sockaddr, error) anyToSockaddr(ж<RawSockaddrAny> Ꮡrsa) {
    ref var rsa = ref Ꮡrsa.DerefOrNull();

    var exprᴛ1 = rsa.Addr.Family;
    if (exprᴛ1 == AF_LINK) {
        var pp = Ꮡrsa.Reinterpret<RawSockaddrAny, RawSockaddrDatalink>();
        var sa = @new<SockaddrDatalink>();
        sa.Value.Len = pp.Value.Len;
        sa.Value.Family = pp.Value.Family;
        sa.Value.Index = pp.Value.Index;
        sa.Value.Type = pp.Value.Type;
        sa.Value.Nlen = pp.Value.Nlen;
        sa.Value.Alen = pp.Value.Alen;
        sa.Value.Slen = pp.Value.Slen;
        sa.Value.Data = pp.Value.Data.Clone();
        return (new SockaddrDatalinkжSockaddr(sa), default!);
    }
    if (exprᴛ1 == AF_UNIX) {
        var pp = Ꮡrsa.Reinterpret<RawSockaddrAny, RawSockaddrUnix>();
        if ((~pp).Len < 2 || (~pp).Len > SizeofSockaddrUnix) {
            return (default!, EINVAL);
        }
        var sa = @new<SockaddrUnix>();
        nint n = (nint)(~pp).Len - 2; // subtract leading Family, Len
        for (nint i = 0; i < n; i++) {
            // Some BSDs include the trailing NUL in the length, whereas
            // others do not. Work around this by subtracting the leading
            // family and len. The path is then scanned to see if a NUL
            // terminator still exists within the length.
            if ((~pp).Path[i] == 0) {
                // found early NUL; assume Len included the NUL
                // or was overestimating.
                n = i;
                break;
            }
        }
        sa.Value.Name = ((@string)@unsafe.Slice(pp.at(RawSockaddrUnix.ᏑPath, 0).Reinterpret<int8, byte>(), n));
        return (new SockaddrUnixжSockaddr(sa), default!);
    }
    if (exprᴛ1 == AF_INET) {
        var pp = Ꮡrsa.Reinterpret<RawSockaddrAny, RawSockaddrInet4>();
        var sa = @new<SockaddrInet4>();
        var p = (ж<array<byte>>)(uintptr)(@unsafe.Pointer.FromPinnedBox(pp.of(RawSockaddrInet4.ᏑPort)));
        sa.Value.Port = ((nint)p.Value[0] << (int)(8)) + (nint)p.Value[1];
        sa.Value.Addr = pp.Value.Addr.Clone();
        return (new SockaddrInet4жSockaddr(sa), default!);
    }
    if (exprᴛ1 == AF_INET6) {
        var pp = Ꮡrsa.Reinterpret<RawSockaddrAny, RawSockaddrInet6>();
        var sa = @new<SockaddrInet6>();
        var p = (ж<array<byte>>)(uintptr)(@unsafe.Pointer.FromPinnedBox(pp.of(RawSockaddrInet6.ᏑPort)));
        sa.Value.Port = ((nint)p.Value[0] << (int)(8)) + (nint)p.Value[1];
        sa.Value.ZoneId = pp.Value.Scope_id;
        sa.Value.Addr = pp.Value.Addr.Clone();
        return (new SockaddrInet6жSockaddr(sa), default!);
    }

    return (default!, EAFNOSUPPORT);
}

public static (nint nfd, Sockaddr sa, error err) Accept(nint fd) {
    nint nfd = default!;
    Sockaddr sa = default!;
    error err = default!;

    ref var rsa = ref heap(new RawSockaddrAny(), out var Ꮡrsa);
    ref var len = ref heap(new _Socklen(), out var Ꮡlen);
    len = SizeofSockaddrAny;
    (nfd, err) = accept(fd, Ꮡrsa, Ꮡlen);
    if (err != default!) {
        return (nfd, sa, err);
    }
    if ((Δruntime.GOOS == "darwin"u8 || Δruntime.GOOS == "ios"u8) && len == 0) {
        // Accepted socket has no address.
        // This is likely due to a bug in xnu kernels,
        // where instead of ECONNABORTED error socket
        // is accepted, but has no address.
        Close(nfd);
        return (0, default!, ECONNABORTED);
    }
    (sa, err) = anyToSockaddr(Ꮡrsa);
    if (err != default!) {
        Close(nfd);
        nfd = 0;
    }
    return (nfd, sa, err);
}

public static (Sockaddr sa, error err) Getsockname(nint fd) {
    Sockaddr sa = default!;
    error err = default!;

    ref var rsa = ref heap(new RawSockaddrAny(), out var Ꮡrsa);
    ref var len = ref heap(new _Socklen(), out var Ꮡlen);
    len = SizeofSockaddrAny;
    {
        err = getsockname(fd, Ꮡrsa, Ꮡlen); if (err != default!) {
            return (sa, err);
        }
    }
    // TODO(jsing): DragonFly has a "bug" (see issue 3349), which should be
    // reported upstream.
    if (Δruntime.GOOS == "dragonfly"u8 && rsa.Addr.Family == AF_UNSPEC && rsa.Addr.Len == 0) {
        rsa.Addr.Family = AF_UNIX;
        rsa.Addr.Len = SizeofSockaddrUnix;
    }
    return anyToSockaddr(Ꮡrsa);
}

//sysnb socketpair(domain int, typ int, proto int, fd *[2]int32) (err error)
public static (byte value, error err) GetsockoptByte(nint fd, nint level, nint opt) {
    error err = default!;

    ref var n = ref heap(new byte(), out var Ꮡn);
    ref var vallen = ref heap<_Socklen>(out var Ꮡvallen);
    vallen = ((_Socklen)1);
    err = getsockopt(fd, level, opt, @unsafe.Pointer.FromPinnedBox(Ꮡn), Ꮡvallen);
    return (n, err);
}

public static (array<byte> value, error err) GetsockoptInet4Addr(nint fd, nint level, nint opt) {
    ref var value = ref heap(new array<byte>(4), out var Ꮡvalue);
    error err = default!;

    ref var vallen = ref heap<_Socklen>(out var Ꮡvallen);
    vallen = ((_Socklen)4);
    err = getsockopt(fd, level, opt, @unsafe.Pointer.FromPinnedBox(Ꮡvalue.at<byte>(0)), Ꮡvallen);
    return (value.Clone(), err);
}

public static (ж<IPMreq>, error) GetsockoptIPMreq(nint fd, nint level, nint opt) {
    ref var value = ref heap(new IPMreq(), out var Ꮡvalue);
    ref var vallen = ref heap<_Socklen>(out var Ꮡvallen);
    vallen = ((_Socklen)SizeofIPMreq);
    var err = getsockopt(fd, level, opt, @unsafe.Pointer.FromPinnedBox(Ꮡvalue), Ꮡvallen);
    return (Ꮡvalue, err);
}

public static (ж<IPv6Mreq>, error) GetsockoptIPv6Mreq(nint fd, nint level, nint opt) {
    ref var value = ref heap(new IPv6Mreq(), out var Ꮡvalue);
    ref var vallen = ref heap<_Socklen>(out var Ꮡvallen);
    vallen = ((_Socklen)SizeofIPv6Mreq);
    var err = getsockopt(fd, level, opt, @unsafe.Pointer.FromPinnedBox(Ꮡvalue), Ꮡvallen);
    return (Ꮡvalue, err);
}

public static (ж<IPv6MTUInfo>, error) GetsockoptIPv6MTUInfo(nint fd, nint level, nint opt) {
    ref var value = ref heap(new IPv6MTUInfo(), out var Ꮡvalue);
    ref var vallen = ref heap<_Socklen>(out var Ꮡvallen);
    vallen = ((_Socklen)SizeofIPv6MTUInfo);
    var err = getsockopt(fd, level, opt, @unsafe.Pointer.FromPinnedBox(Ꮡvalue), Ꮡvallen);
    return (Ꮡvalue, err);
}

public static (ж<ICMPv6Filter>, error) GetsockoptICMPv6Filter(nint fd, nint level, nint opt) {
    ref var value = ref heap(new ICMPv6Filter(), out var Ꮡvalue);
    ref var vallen = ref heap<_Socklen>(out var Ꮡvallen);
    vallen = ((_Socklen)SizeofICMPv6Filter);
    var err = getsockopt(fd, level, opt, @unsafe.Pointer.FromPinnedBox(Ꮡvalue), Ꮡvallen);
    return (Ꮡvalue, err);
}

//sys   recvfrom(fd int, p []byte, flags int, from *RawSockaddrAny, fromlen *_Socklen) (n int, err error)
//sys   sendto(s int, buf []byte, flags int, to unsafe.Pointer, addrlen _Socklen) (err error)
//sys	recvmsg(s int, msg *Msghdr, flags int) (n int, err error)
internal static (nint n, nint oobn, nint recvflags, error err) recvmsgRaw(nint fd, slice<byte> p, slice<byte> oob, nint flags, ж<RawSockaddrAny> Ꮡrsa) {
    nint n = default!;
    nint oobn = default!;
    nint recvflags = default!;
    error err = default!;

    ref var msg = ref heap(new Msghdr(), out var Ꮡmsg);
    msg.Name = Ꮡrsa.Reinterpret<RawSockaddrAny, byte>();
    msg.Namelen = (uint32)SizeofSockaddrAny;
    ref var iov = ref heap(new Iovec(), out var Ꮡiov);
    if (len(p) > 0) {
        iov.Base = Ꮡ(p, 0);
        iov.SetLen(len(p));
    }
    ref var dummy = ref heap(new byte(), out var Ꮡdummy);
    if (len(oob) > 0) {
        // receive at least one normal byte
        if (len(p) == 0) {
            iov.Base = Ꮡdummy;
            iov.SetLen(1);
        }
        msg.Control = Ꮡ(oob, 0);
        msg.SetControllen(len(oob));
    }
    msg.Iov = Ꮡiov;
    msg.Iovlen = 1;
    {
        (n, err) = recvmsg(fd, Ꮡmsg, flags); if (err != default!) {
            return (n, oobn, recvflags, err);
        }
    }
    oobn = (nint)msg.Controllen;
    recvflags = (nint)msg.Flags;
    return (n, oobn, recvflags, err);
}

//sys	sendmsg(s int, msg *Msghdr, flags int) (n int, err error)
internal static (nint n, error err) sendmsgN(nint fd, slice<byte> p, slice<byte> oob, @unsafe.Pointer ptr, _Socklen salen, nint flags) {
    nint n = default!;
    error err = default!;

    ref var msg = ref heap(new Msghdr(), out var Ꮡmsg);
    msg.Name = (ж<byte>)(uintptr)(ptr);
    msg.Namelen = (uint32)salen;
    ref var iov = ref heap(new Iovec(), out var Ꮡiov);
    if (len(p) > 0) {
        iov.Base = Ꮡ(p, 0);
        iov.SetLen(len(p));
    }
    ref var dummy = ref heap(new byte(), out var Ꮡdummy);
    if (len(oob) > 0) {
        // send at least one normal byte
        if (len(p) == 0) {
            iov.Base = Ꮡdummy;
            iov.SetLen(1);
        }
        msg.Control = Ꮡ(oob, 0);
        msg.SetControllen(len(oob));
    }
    msg.Iov = Ꮡiov;
    msg.Iovlen = 1;
    {
        (n, err) = sendmsg(fd, Ꮡmsg, flags); if (err != default!) {
            return (0, err);
        }
    }
    if (len(oob) > 0 && len(p) == 0) {
        n = 0;
    }
    return (n, default!);
}

//sys	kevent(kq int, change unsafe.Pointer, nchange int, event unsafe.Pointer, nevent int, timeout *Timespec) (n int, err error)
public static (nint n, error err) Kevent(nint kq, slice<Kevent_t> changes, slice<Kevent_t> events, ж<Timespec> Ꮡtimeout) {
    @unsafe.Pointer change = default!;
    @unsafe.Pointer @event = default!;
    if (len(changes) > 0) {
        change = @unsafe.Pointer.FromPinnedBox(Ꮡ(changes, 0));
    }
    if (len(events) > 0) {
        @event = @unsafe.Pointer.FromPinnedBox(Ꮡ(events, 0));
    }
    return kevent(kq, change, len(changes), @event, len(events), Ꮡtimeout);
}

public static (@string value, error err) Sysctl(@string name) {
    error err = default!;

    // Translate name to mib number.
    (var mib, err) = nametomib(name);
    if (err != default!) {
        return ("", err);
    }
    // Find size.
    ref var n = ref heap<uintptr>(out var Ꮡn);
    n = (uintptr)0;
    {
        err = sysctl(mib, nil, Ꮡn, nil, 0); if (err != default!) {
            return ("", err);
        }
    }
    if (n == 0) {
        return ("", default!);
    }
    // Read into buffer of that size.
    var buf = new slice<byte>((nint)(n));
    {
        err = sysctl(mib, Ꮡ(buf, 0), Ꮡn, nil, 0); if (err != default!) {
            return ("", err);
        }
    }
    // Throw away terminating NUL.
    if (n > 0 && buf[(nint)(n - 1)] == (rune)'\x00') {
        n--;
    }
    return (((@string)(buf[0..(int)(n)])), default!);
}

public static (uint32 value, error err) SysctlUint32(@string name) {
    error err = default!;

    // Translate name to mib number.
    (var mib, err) = nametomib(name);
    if (err != default!) {
        return (0, err);
    }
    // Read into buffer of that size.
    ref var n = ref heap<uintptr>(out var Ꮡn);
    n = (uintptr)4;
    var buf = new slice<byte>(4);
    {
        err = sysctl(mib, Ꮡ(buf, 0), Ꮡn, nil, 0); if (err != default!) {
            return (0, err);
        }
    }
    if (n != 4) {
        return (0, EIO);
    }
    return (~Ꮡ(buf, 0).Reinterpret<byte, uint32>(), default!);
}

//sys	utimes(path string, timeval *[2]Timeval) (err error)
public static error /*err*/ Utimes(@string path, slice<Timeval> tv) {
    if (len(tv) != 2) {
        return EINVAL;
    }
    return utimes(path, array<Timeval>.AliasPointer(Ꮡ(tv, 0), 2));
}

public static error UtimesNano(@string path, slice<Timespec> ts) {
    if (len(ts) != 2) {
        return EINVAL;
    }
    var err = utimensat(_AT_FDCWD, path, array<Timespec>.AliasPointer(Ꮡ(ts, 0), 2), 0);
    if (!AreEqual(err, ENOSYS)) {
        return err;
    }
    // Not as efficient as it could be because Timespec and
    // Timeval have different types in the different OSes
    ref var tv = ref heap<array<Timeval>>(out var Ꮡtv);
    tv = new Timeval[]{
        NsecToTimeval(TimespecToNsec(ts[0])),
        NsecToTimeval(TimespecToNsec(ts[1]))
    }.array();
    return utimes(path, array<Timeval>.AliasPointer(Ꮡtv.at<Timeval>(0), 2));
}

//sys	futimes(fd int, timeval *[2]Timeval) (err error)
public static error /*err*/ Futimes(nint fd, slice<Timeval> tv) {
    if (len(tv) != 2) {
        return EINVAL;
    }
    return futimes(fd, array<Timeval>.AliasPointer(Ꮡ(tv, 0), 2));
}

//sys	fcntl(fd int, cmd int, arg int) (val int, err error)
//sys	fcntlPtr(fd int, cmd int, arg unsafe.Pointer) (val int, err error) = SYS_FCNTL
//sysnb ioctl(fd int, req int, arg int) (err error)
//sysnb ioctlPtr(fd int, req uint, arg unsafe.Pointer) (err error) = SYS_IOCTL
internal static ж<mmapper> mapper;
internal static void initᴛmapper() { mapper = Ꮡ(new mmapper(
    active: new map<ж<byte>, slice<byte>>(),
    mmap: mmap,
    munmap: munmap
)); }

public static (slice<byte> data, error err) Mmap(nint fd, int64 offset, nint length, nint prot, nint flags) {
    return mapper.Mmap(fd, offset, length, prot, flags);
}

public static error /*err*/ Munmap(slice<byte> b) {
    return mapper.Munmap(b);
}

} // end syscall_package
