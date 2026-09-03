// Guards the IPv4 multicast group-join, which is the syscall STRUCT-PASSING class at the
// setsockopt boundary.
//
// net.ListenMulticastUDP asks the kernel for IP_ADD_MEMBERSHIP with a `struct ip_mreq`: two INLINE
// in_addr, eight bytes. Converted, syscall.IPMreq holds both as golib `array<byte>` MANAGED
// REFERENCES -- the managed struct measures 32 bytes where SizeofIPMreq promises the kernel 8 --
// and the generated wrapper handed that struct's address straight to setsockopt. The kernel read
// two object references as an address and refused the join: measured on Linux before the fix as
//
//     listen udp 224.0.0.254:12345: setsockopt: cannot assign requested address
//
// which is EADDRNOTAVAIL, the Linux errno spelling of the WSAEINVAL the Windows half of this same
// registration recorded for the identical call. The hand-own encodes the eight bytes into a stack
// buffer and hands the kernel that.
//
// WHAT MAKES THIS GUARD DISCRIMINATING. The join's ERROR is the discriminator, not decoration:
// before the fix it was non-nil for exactly this reason, so `err == nil` can only be true when the
// eight bytes the kernel read were the real group and interface addresses.
//
// PLATFORM-EXCLUSIVE, linux. The defect is the converted struct's rather than the platform's, but
// the Windows flavour of this wrapper has been hand-owned since the sockaddr arc and only the goos
// scope was left behind -- so the Linux side is what this guards. It also cannot share a golden:
// net's converted emission differs by platform (the Δ-prefixed alias flavour), the same reason
// SendtoSeam records for its own marker.
//
// HOST REQUIREMENT, stated rather than worked around: the join needs an interface that is UP and
// carries FlagMulticast. On a host with none, the program says so on its own line and exercises
// nothing -- both sides print that line, so the comparison still holds, but the output makes it
// visible that the join was not reached rather than reporting a silent pass.
package main

import (
	"fmt"
	"net"
)

func main() {
	ifis, err := net.Interfaces()
	if err != nil {
		fmt.Println("FATAL interfaces:", err)
		return
	}

	var chosen *net.Interface
	for i := range ifis {
		if ifis[i].Flags&net.FlagUp != 0 && ifis[i].Flags&net.FlagMulticast != 0 {
			chosen = &ifis[i]
			break
		}
	}
	if chosen == nil {
		fmt.Println("no multicast-capable interface: join NOT exercised on this host")
		return
	}

	// A link-local group, so nothing leaves the segment and no router is involved.
	gaddr := &net.UDPAddr{IP: net.IPv4(224, 0, 0, 254), Port: 0}

	c, err := net.ListenMulticastUDP("udp4", chosen, gaddr)
	fmt.Println("join err is nil  =", err == nil)
	if err != nil {
		fmt.Println("  join error      =", err)
		return
	}
	defer c.Close()

	local := c.LocalAddr().(*net.UDPAddr)
	fmt.Println("local ip is unset =", local.IP.IsUnspecified())
	fmt.Println("local port bound  =", local.Port != 0)
}
