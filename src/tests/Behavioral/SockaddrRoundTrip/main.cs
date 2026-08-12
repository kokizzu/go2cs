namespace go;

using fmt = fmt_package;
using syscall = syscall_package;

partial class main_package {

internal static (@string addr, nint port, bool ok) describe(syscallꓸSockaddr sa) {
    switch (sa.type()) {
    case ж<syscall.SockaddrInet4> s: {
        return (fmt.Sprintf("%d.%d.%d.%d"u8, (~s).Addr[0], (~s).Addr[1], (~s).Addr[2], (~s).Addr[3]), (~s).Port, true);
    }
    case ж<syscall.SockaddrInet6> s: {
        return (fmt.Sprintf("%x"u8, (~s).Addr), (~s).Port, true);
    }}
    return ("", 0, false);
}

internal static bool sameAddr(syscallꓸSockaddr a, syscallꓸSockaddr b) {
    switch (a.type()) {
    case ж<syscall.SockaddrInet4> x: {
        var (y, ok) = b._<ж<syscall.SockaddrInet4>>(ᐧ);
        return ok && (~x).Addr == (~y).Addr;
    }
    case ж<syscall.SockaddrInet6> x: {
        var (y, ok) = b._<ж<syscall.SockaddrInet6>>(ᐧ);
        return ok && (~x).Addr == (~y).Addr;
    }}
    return false;
}

internal static void roundTrip(@string label, nint family, syscallꓸSockaddr bindTo) {
    GoFrame ᒐ = default;
    try {
        var (server, err) = syscall.Socket(family, syscall.SOCK_STREAM, syscall.IPPROTO_TCP);
        if (err != default!) {
            fmt.Printf("%s: socket failed\n"u8, label);
            return;
        }
        defer(syscall.Closesocket, server, ref ᒐ);
        {
            var errΔ1 = syscall.Bind(server, bindTo); if (errΔ1 != default!) {
                fmt.Printf("%s: bind failed\n"u8, label);
                return;
            }
        }
        (var bound, err) = syscall.Getsockname(server);
        if (err != default!) {
            fmt.Printf("%s: getsockname failed\n"u8, label);
            return;
        }
        var (boundAddr, boundPort, ok) = describe(bound);
        if (!ok) {
            fmt.Printf("%s: getsockname returned an unexpected Sockaddr type\n"u8, label);
            return;
        }
        fmt.Printf("%s bound: addr=%s portAssigned=%v\n"u8, label, boundAddr, boundPort > 0);
        {
            var errΔ2 = syscall.Listen(server, 1); if (errΔ2 != default!) {
                fmt.Printf("%s: listen failed\n"u8, label);
                return;
            }
        }
        (var client, err) = syscall.Socket(family, syscall.SOCK_STREAM, syscall.IPPROTO_TCP);
        if (err != default!) {
            fmt.Printf("%s: client socket failed\n"u8, label);
            return;
        }
        defer(syscall.Closesocket, client, ref ᒐ);
        {
            var errΔ3 = syscall.Connect(client, bound); if (errΔ3 != default!) {
                fmt.Printf("%s: connect failed\n"u8, label);
                return;
            }
        }
        (var peer, err) = syscall.Getpeername(client);
        if (err != default!) {
            fmt.Printf("%s: getpeername failed\n"u8, label);
            return;
        }
        (var peerAddr, var peerPort, ok) = describe(peer);
        if (!ok) {
            fmt.Printf("%s: getpeername returned an unexpected Sockaddr type\n"u8, label);
            return;
        }
        fmt.Printf("%s peer matches bound: addr=%v port=%v\n"u8,
            label, sameAddr(peer, bound) && peerAddr == boundAddr, peerPort == boundPort);
        (var local, err) = syscall.Getsockname(client);
        if (err != default!) {
            fmt.Printf("%s: client getsockname failed\n"u8, label);
            return;
        }
        (var localAddr, var localPort, ok) = describe(local);
        if (!ok) {
            fmt.Printf("%s: client getsockname returned an unexpected Sockaddr type\n"u8, label);
            return;
        }
        fmt.Printf("%s client local: addr=%s portAssigned=%v distinctFromListener=%v\n"u8,
            label, localAddr, localPort > 0, localPort != boundPort);
    }
    catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
    finally { ᒐ.Run(); }
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object wsaStartupFailedˢ = (@string)"WSAStartup failed"u8;
private static readonly @string tcp4ˢ = "tcp4"u8;
private static readonly @string tcp6ˢ = "tcp6"u8;
private static readonly object doneˢ = (@string)"done"u8;

internal static void Main() {
    GoFrame ᒐ = default;
    try {
        ref var data = ref heap(new syscall.WSAData(), out var Ꮡdata);
        {
            var err = syscall.WSAStartup((uint32)0x202, Ꮡdata); if (err != default!) {
                fmt.Println(wsaStartupFailedˢ);
                return;
            }
        }
        defer(() => syscall.WSACleanup(), ref ᒐ);
        roundTrip(tcp4ˢ, syscall.AF_INET, new syscall.SockaddrInet4жΔSockaddr(Ꮡ(new syscall.SockaddrInet4(Addr: new byte[]{127, 0, 0, 1}.array()))));
        roundTrip(tcp6ˢ, syscall.AF_INET6, new syscall.SockaddrInet6жΔSockaddr(Ꮡ(new syscall.SockaddrInet6(Addr: new array<byte>(16){[15] = 1}))));
        fmt.Println(doneˢ);
    }
    catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
    finally { ᒐ.Run(); }
}

} // end main_package
