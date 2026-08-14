// Copyright 2011 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go;

using syscall = syscall_package;
using route = vendor.golang.org.x.net.route_package;
using vendor.golang.org.x.net;

partial class net_package {

internal static (slice<route.Message>, error) interfaceMessages(nint ifindex) {
    var (rib, err) = route.FetchRIB(syscall.AF_UNSPEC, syscall.NET_RT_IFLIST, ifindex);
    if (err != default!) {
        return (default!, err);
    }
    return route.ParseRIB(syscall.NET_RT_IFLIST, rib);
}

// interfaceMulticastAddrTable returns addresses for a specific
// interface.
internal static (slice<ΔAddr>, error) interfaceMulticastAddrTable(ref Interface ifi) {
    var (rib, err) = route.FetchRIB(syscall.AF_UNSPEC, syscall.NET_RT_IFLIST2, ifi.Index);
    if (err != default!) {
        return (default!, err);
    }
    (var msgs, err) = route.ParseRIB(syscall.NET_RT_IFLIST2, rib);
    if (err != default!) {
        return (default!, err);
    }
    var ifmat = new slice<ΔAddr>(0, len(msgs));
    foreach (var (_, m) in msgs) {
        switch (m.type()) {
        case ж<route.InterfaceMulticastAddrMessage> mΔ1: {
            if (ifi.Index != (~mΔ1).Index) {
                continue;
            }
            IP ip = default!;
            switch ((~mΔ1).Addrs[syscall.RTAX_IFA].type()) {
            case ж<route.Inet4Addr> sa: {
                ip = IPv4((~sa).IP[0], (~sa).IP[1], (~sa).IP[2], (~sa).IP[3]);
                break;
            }
            case ж<route.Inet6Addr> sa: {
                ip = new IP(IPv6len);
                copy(ip, (~sa).IP[..]);
                break;
            }}
            if (ip != default!) {
                ifmat = append(ifmat, (ΔAddr)(new IPAddrжΔAddr(Ꮡ(new IPAddr(IP: ip)))));
            }
            break;
        }}
    }
    return (ifmat, default!);
}

} // end net_package
