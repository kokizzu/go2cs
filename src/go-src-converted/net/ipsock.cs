// Copyright 2009 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

// package net -- go2cs converted at 2022 March 13 05:29:53 UTC
// import "net" ==> using net = go.net_package
// Original source: C:\Program Files\Go\src\net\ipsock.go
namespace go;

using context = context_package;
using bytealg = @internal.bytealg_package;
using runtime = runtime_package;
using sync = sync_package;


// BUG(rsc,mikio): On DragonFly BSD and OpenBSD, listening on the
// "tcp" and "udp" networks does not listen for both IPv4 and IPv6
// connections. This is due to the fact that IPv4 traffic will not be
// routed to an IPv6 socket - two separate sockets are required if
// both address families are to be supported.
// See inet6(4) for details.


using System;public static partial class net_package {

private partial struct ipStackCapabilities {
    public ref sync.Once Once => ref Once_val; // guards following
    public bool ipv4Enabled;
    public bool ipv6Enabled;
    public bool ipv4MappedIPv6Enabled;
}

private static ipStackCapabilities ipStackCaps = default;

// supportsIPv4 reports whether the platform supports IPv4 networking
// functionality.
private static bool supportsIPv4() {
    ipStackCaps.Once.Do(ipStackCaps.probe);
    return ipStackCaps.ipv4Enabled;
}

// supportsIPv6 reports whether the platform supports IPv6 networking
// functionality.
private static bool supportsIPv6() {
    ipStackCaps.Once.Do(ipStackCaps.probe);
    return ipStackCaps.ipv6Enabled;
}

// supportsIPv4map reports whether the platform supports mapping an
// IPv4 address inside an IPv6 address at transport layer
// protocols. See RFC 4291, RFC 4038 and RFC 3493.
private static bool supportsIPv4map() { 
    // Some operating systems provide no support for mapping IPv4
    // addresses to IPv6, and a runtime check is unnecessary.
    switch (runtime.GOOS) {
        case "dragonfly": 

        case "openbsd": 
            return false;
            break;
    }

    ipStackCaps.Once.Do(ipStackCaps.probe);
    return ipStackCaps.ipv4MappedIPv6Enabled;
}

// An addrList represents a list of network endpoint addresses.
private partial struct addrList { // : slice<Addr>
}

// isIPv4 reports whether addr contains an IPv4 address.
private static bool isIPv4(Addr addr) {
    switch (addr.type()) {
        case ptr<TCPAddr> addr:
            return addr.IP.To4() != null;
            break;
        case ptr<UDPAddr> addr:
            return addr.IP.To4() != null;
            break;
        case ptr<IPAddr> addr:
            return addr.IP.To4() != null;
            break;
    }
    return false;
}

// isNotIPv4 reports whether addr does not contain an IPv4 address.
private static bool isNotIPv4(Addr addr) {
    return !isIPv4(addr);
}

// forResolve returns the most appropriate address in address for
// a call to ResolveTCPAddr, ResolveUDPAddr, or ResolveIPAddr.
// IPv4 is preferred, unless addr contains an IPv6 literal.
private static Addr forResolve(this addrList addrs, @string network, @string addr) {
    bool want6 = default;
    switch (network) {
        case "ip": 
            // IPv6 literal (addr does NOT contain a port)
            want6 = count(addr, ':') > 0;
            break;
        case "tcp": 
            // IPv6 literal. (addr contains a port, so look for '[')

        case "udp": 
            // IPv6 literal. (addr contains a port, so look for '[')
            want6 = count(addr, '[') > 0;
            break;
    }
    if (want6) {
        return addrs.first(isNotIPv4);
    }
    return addrs.first(isIPv4);
}

// first returns the first address which satisfies strategy, or if
// none do, then the first address of any kind.
private static Addr first(this addrList addrs, Func<Addr, bool> strategy) {
    foreach (var (_, addr) in addrs) {
        if (strategy(addr)) {
            return addr;
        }
    }    return addrs[0];
}

// partition divides an address list into two categories, using a
// strategy function to assign a boolean label to each address.
// The first address, and any with a matching label, are returned as
// primaries, while addresses with the opposite label are returned
// as fallbacks. For non-empty inputs, primaries is guaranteed to be
// non-empty.
private static (addrList, addrList) partition(this addrList addrs, Func<Addr, bool> strategy) {
    addrList primaries = default;
    addrList fallbacks = default;

    bool primaryLabel = default;
    foreach (var (i, addr) in addrs) {
        var label = strategy(addr);
        if (i == 0 || label == primaryLabel) {
            primaryLabel = label;
            primaries = append(primaries, addr);
        }
        else
 {
            fallbacks = append(fallbacks, addr);
        }
    }    return ;
}

// filterAddrList applies a filter to a list of IP addresses,
// yielding a list of Addr objects. Known filters are nil, ipv4only,
// and ipv6only. It returns every address when the filter is nil.
// The result contains at least one address when error is nil.
private static (addrList, error) filterAddrList(Func<IPAddr, bool> filter, slice<IPAddr> ips, Func<IPAddr, Addr> inetaddr, @string originalAddr) {
    addrList _p0 = default;
    error _p0 = default!;

    addrList addrs = default;
    foreach (var (_, ip) in ips) {
        if (filter == null || filter(ip)) {
            addrs = append(addrs, inetaddr(ip));
        }
    }    if (len(addrs) == 0) {
        return (null, error.As(addr(new AddrError(Err:errNoSuitableAddress.Error(),Addr:originalAddr))!)!);
    }
    return (addrs, error.As(null!)!);
}

// ipv4only reports whether addr is an IPv4 address.
private static bool ipv4only(IPAddr addr) {
    return addr.IP.To4() != null;
}

// ipv6only reports whether addr is an IPv6 address except IPv4-mapped IPv6 address.
private static bool ipv6only(IPAddr addr) {
    return len(addr.IP) == IPv6len && addr.IP.To4() == null;
}

// SplitHostPort splits a network address of the form "host:port",
// "host%zone:port", "[host]:port" or "[host%zone]:port" into host or
// host%zone and port.
//
// A literal IPv6 address in hostport must be enclosed in square
// brackets, as in "[::1]:80", "[::1%lo0]:80".
//
// See func Dial for a description of the hostport parameter, and host
// and port results.
public static (@string, @string, error) SplitHostPort(@string hostport) {
    @string host = default;
    @string port = default;
    error err = default!;

    const @string missingPort = "missing port in address";
    const @string tooManyColons = "too many colons in address";
    Func<@string, @string, (@string, @string, error)> addrErr = (addr, why) => ("", "", error.As(addr(new AddrError(Err:why,Addr:addr))!)!);
    nint j = 0;
    nint k = 0; 

    // The port starts after the last colon.
    var i = last(hostport, ':');
    if (i < 0) {
        return addrErr(hostport, missingPort);
    }
    if (hostport[0] == '[') { 
        // Expect the first ']' just before the last ':'.
        var end = bytealg.IndexByteString(hostport, ']');
        if (end < 0) {
            return addrErr(hostport, "missing ']' in address");
        }

        if (end + 1 == len(hostport)) 
            // There can't be a ':' behind the ']' now.
            return addrErr(hostport, missingPort);
        else if (end + 1 == i)         else 
            // Either ']' isn't followed by a colon, or it is
            // followed by a colon that is not the last one.
            if (hostport[end + 1] == ':') {
                return addrErr(hostport, tooManyColons);
            }
            return addrErr(hostport, missingPort);
                host = hostport[(int)1..(int)end];
        (j, k) = (1, end + 1);
    }
    else
 {
        host = hostport[..(int)i];
        if (bytealg.IndexByteString(host, ':') >= 0) {
            return addrErr(hostport, tooManyColons);
        }
    }
    if (bytealg.IndexByteString(hostport[(int)j..], '[') >= 0) {
        return addrErr(hostport, "unexpected '[' in address");
    }
    if (bytealg.IndexByteString(hostport[(int)k..], ']') >= 0) {
        return addrErr(hostport, "unexpected ']' in address");
    }
    port = hostport[(int)i + 1..];
    return (host, port, error.As(null!)!);
}

private static (@string, @string) splitHostZone(@string s) {
    @string host = default;
    @string zone = default;
 
    // The IPv6 scoped addressing zone identifier starts after the
    // last percent sign.
    {
        var i = last(s, '%');

        if (i > 0) {
            (host, zone) = (s[..(int)i], s[(int)i + 1..]);
        }
        else
 {
            host = s;
        }
    }
    return ;
}

// JoinHostPort combines host and port into a network address of the
// form "host:port". If host contains a colon, as found in literal
// IPv6 addresses, then JoinHostPort returns "[host]:port".
//
// See func Dial for a description of the host and port parameters.
public static @string JoinHostPort(@string host, @string port) { 
    // We assume that host is a literal IPv6 address if host has
    // colons.
    if (bytealg.IndexByteString(host, ':') >= 0) {
        return "[" + host + "]:" + port;
    }
    return host + ":" + port;
}

// internetAddrList resolves addr, which may be a literal IP
// address or a DNS name, and returns a list of internet protocol
// family addresses. The result contains at least one address when
// error is nil.
private static (addrList, error) internetAddrList(this ptr<Resolver> _addr_r, context.Context ctx, @string net, @string addr) => func((_, panic, _) => {
    addrList _p0 = default;
    error _p0 = default!;
    ref Resolver r = ref _addr_r.val;

    error err = default!;    @string host = default;    @string port = default;
    nint portnum = default;
    switch (net) {
        case "tcp": 

        case "tcp4": 

        case "tcp6": 

        case "udp": 

        case "udp4": 

        case "udp6": 
            if (addr != "") {
                host, port, err = SplitHostPort(addr);

                if (err != null) {
                    return (null, error.As(err)!);
                }
                portnum, err = r.LookupPort(ctx, net, port);

                if (err != null) {
                    return (null, error.As(err)!);
                }
            }
            break;
        case "ip": 

        case "ip4": 

        case "ip6": 
            if (addr != "") {
                host = addr;
            }
            break;
        default: 
            return (null, error.As(UnknownNetworkError(net))!);
            break;
    }
    Func<IPAddr, Addr> inetaddr = ip => {
        switch (net) {
            case "tcp": 

            case "tcp4": 

            case "tcp6": 
                return addr(new TCPAddr(IP:ip.IP,Port:portnum,Zone:ip.Zone));
                break;
            case "udp": 

            case "udp4": 

            case "udp6": 
                return addr(new UDPAddr(IP:ip.IP,Port:portnum,Zone:ip.Zone));
                break;
            case "ip": 

            case "ip4": 

            case "ip6": 
                return addr(new IPAddr(IP:ip.IP,Zone:ip.Zone));
                break;
            default: 
                panic("unexpected network: " + net);
                break;
        }
    };
    if (host == "") {
        return (new addrList(inetaddr(IPAddr{})), error.As(null!)!);
    }
    var (ips, err) = r.lookupIPAddr(ctx, net, host);
    if (err != null) {
        return (null, error.As(err)!);
    }
    if (len(ips) == 1 && ips[0].IP.Equal(IPv6unspecified)) {
        ips = append(ips, new IPAddr(IP:IPv4zero));
    }
    Func<IPAddr, bool> filter = default;
    if (net != "" && net[len(net) - 1] == '4') {
        filter = ipv4only;
    }
    if (net != "" && net[len(net) - 1] == '6') {
        filter = ipv6only;
    }
    return filterAddrList(filter, ips, inetaddr, host);
});

private static IP loopbackIP(@string net) {
    if (net != "" && net[len(net) - 1] == '6') {
        return IPv6loopback;
    }
    return new IP(127,0,0,1);
}

} // end net_package
