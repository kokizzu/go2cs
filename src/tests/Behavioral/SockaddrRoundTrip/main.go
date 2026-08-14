// SockaddrRoundTrip guards the Windows sockaddr struct-passing seam. Every socket call that names
// an address hands the kernel a `sockaddr`, and Go's RawSockaddrInet4 matches the native
// `sockaddr_in` exactly because its Addr [4]byte / Zero [8]uint8 are INLINE. The converted struct
// cannot be: golib holds a Go fixed array as an `array<byte>` MANAGED reference, one word where
// Windows expects 4 (or 8) bytes of storage, so the address the wrapper handed the kernel named a
// ~24-byte managed object with object references where the address octets belong. On top of that,
// Go writes and reads the port through a `(*[2]byte)(unsafe.Pointer(&raw.Port))` alias, and an
// `array<byte>` rebuilt from a raw address is LENGTH ZERO -- so the conversion panicked with
// `index out of range [0] with length 0` before any socket call happened at all.
// Bind / Connect / ConnectEx / Getsockname / Getpeername and the two sockaddr() encoders are now
// hand-owned against blittable mirrors (syscall/windows/syscall_windows_impl.cs).
//
// A no-fault check proves nothing here: a mirror with the wrong offsets returns garbage WITHOUT
// crashing -- which is exactly how the Process32First member of this same class failed, answering a
// parent PID of 0 while faulting nowhere. So this prints values that depend on every field the
// encode and the decode have to get right, and cross-checks them against each other:
//
//	Bind        -- the ENCODE. A wrong address or port here binds somewhere else, or fails.
//	Getsockname -- the DECODE. It reads back the address the kernel actually assigned, so the
//	               printed dotted quad and the "a port was assigned" answer both come from the
//	               kernel's own bytes rather than from anything the program supplied.
//	Connect     -- the ENCODE again, of the address Getsockname just decoded. A round trip that
//	               corrupts either direction cannot reach the listener.
//	Getpeername -- the DECODE again, from the CLIENT end. Its answer must equal the listener's
//	               bound address field for field, which is the strongest single check here: it
//	               closes encode -> kernel -> decode -> encode -> kernel -> decode.
//
// The ephemeral ports themselves are never printed -- only whether one was assigned and whether the
// two ends agree about it -- so the output is identical on any host.
//
// Windows-only, like the FindFirstFileData guard for the sibling member of this class: it drives
// syscall directly, and Go's Windows syscall surface is what the seam lives in.
package main

import (
	"fmt"
	"syscall"
)

// describe renders a Sockaddr as a stable address string plus its port. Both are read out of the
// decoded Go value, so a decode that lost or transposed bytes shows up in the printed text.
func describe(sa syscall.Sockaddr) (addr string, port int, ok bool) {
	switch s := sa.(type) {
	case *syscall.SockaddrInet4:
		return fmt.Sprintf("%d.%d.%d.%d", s.Addr[0], s.Addr[1], s.Addr[2], s.Addr[3]), s.Port, true
	case *syscall.SockaddrInet6:
		return fmt.Sprintf("%x", s.Addr), s.Port, true
	}
	return "", 0, false
}

// sameAddr reports whether two decoded Sockaddrs name the same host address, ignoring the port.
func sameAddr(a, b syscall.Sockaddr) bool {
	switch x := a.(type) {
	case *syscall.SockaddrInet4:
		y, ok := b.(*syscall.SockaddrInet4)
		return ok && x.Addr == y.Addr
	case *syscall.SockaddrInet6:
		y, ok := b.(*syscall.SockaddrInet6)
		return ok && x.Addr == y.Addr
	}
	return false
}

// roundTrip binds a listening socket to the loopback address, connects a second socket to whatever
// the kernel assigned it, and cross-checks what both ends report.
func roundTrip(label string, family int, bindTo syscall.Sockaddr) {
	server, err := syscall.Socket(family, syscall.SOCK_STREAM, syscall.IPPROTO_TCP)
	if err != nil {
		fmt.Printf("%s: socket failed\n", label)
		return
	}
	defer syscall.Closesocket(server)

	if err := syscall.Bind(server, bindTo); err != nil {
		fmt.Printf("%s: bind failed\n", label)
		return
	}

	bound, err := syscall.Getsockname(server)
	if err != nil {
		fmt.Printf("%s: getsockname failed\n", label)
		return
	}

	boundAddr, boundPort, ok := describe(bound)
	if !ok {
		fmt.Printf("%s: getsockname returned an unexpected Sockaddr type\n", label)
		return
	}
	fmt.Printf("%s bound: addr=%s portAssigned=%v\n", label, boundAddr, boundPort > 0)

	if err := syscall.Listen(server, 1); err != nil {
		fmt.Printf("%s: listen failed\n", label)
		return
	}

	client, err := syscall.Socket(family, syscall.SOCK_STREAM, syscall.IPPROTO_TCP)
	if err != nil {
		fmt.Printf("%s: client socket failed\n", label)
		return
	}
	defer syscall.Closesocket(client)

	// The kernel completes this from the listen backlog, so it needs no accept on the other end.
	if err := syscall.Connect(client, bound); err != nil {
		fmt.Printf("%s: connect failed\n", label)
		return
	}

	peer, err := syscall.Getpeername(client)
	if err != nil {
		fmt.Printf("%s: getpeername failed\n", label)
		return
	}

	peerAddr, peerPort, ok := describe(peer)
	if !ok {
		fmt.Printf("%s: getpeername returned an unexpected Sockaddr type\n", label)
		return
	}

	// The full closure: what the client's kernel reports as its peer must be the very address the
	// listener was assigned -- through two encodes and two decodes.
	fmt.Printf("%s peer matches bound: addr=%v port=%v\n",
		label, sameAddr(peer, bound) && peerAddr == boundAddr, peerPort == boundPort)

	local, err := syscall.Getsockname(client)
	if err != nil {
		fmt.Printf("%s: client getsockname failed\n", label)
		return
	}

	localAddr, localPort, ok := describe(local)
	if !ok {
		fmt.Printf("%s: client getsockname returned an unexpected Sockaddr type\n", label)
		return
	}

	// The client's own end is a DIFFERENT ephemeral port on the same loopback address -- so a decode
	// that simply echoed the address it was handed would fail this line.
	fmt.Printf("%s client local: addr=%s portAssigned=%v distinctFromListener=%v\n",
		label, localAddr, localPort > 0, localPort != boundPort)
}

// putRaw writes one byte at a NATIVE offset into a RawSockaddrAny. The Go declaration is
// `struct { Addr RawSockaddr; Pad [100]int8 }` over `RawSockaddr{ Family uint16; Data [14]int8 }`,
// so the 116-byte native image is Family at 0..1, Addr.Data at 2..15 and Pad at 16..115. Every
// address family below is laid out by its real sockaddr offsets, which is what makes the IPv6 case
// cross the Data/Pad boundary -- the one place a decode can silently lose eight bytes.
func putRaw(rsa *syscall.RawSockaddrAny, off int, v byte) {
	if off < 16 {
		rsa.Addr.Data[off-2] = int8(v)
		return
	}
	rsa.Pad[off-16] = int8(v)
}

// decodeRawAny drives (*RawSockaddrAny).Sockaddr directly -- the DECODE net's accept path runs on
// the GetAcceptExSockaddrs output, and the last member of this seam still carrying the
// `(*[2]byte)(unsafe.Pointer(&raw.Port))` port alias. Getsockname/Getpeername above cannot reach it
// (they decode from the kernel's own buffer through their own hand-owns), so without this the
// method has no value-level guard at all. Every field printed is one the decode has to place right.
func decodeRawAny(label string, family uint16, fill func(put func(off int, v byte))) {
	var rsa syscall.RawSockaddrAny
	rsa.Addr.Family = family
	fill(func(off int, v byte) { putRaw(&rsa, off, v) })

	sa, err := rsa.Sockaddr()
	if err != nil {
		fmt.Printf("%s decode: err=%v\n", label, err)
		return
	}

	switch s := sa.(type) {
	case *syscall.SockaddrInet4:
		fmt.Printf("%s decode: inet4 addr=%d.%d.%d.%d port=%d\n",
			label, s.Addr[0], s.Addr[1], s.Addr[2], s.Addr[3], s.Port)
	case *syscall.SockaddrInet6:
		fmt.Printf("%s decode: inet6 addr=%x port=%d zone=%d\n", label, s.Addr, s.Port, s.ZoneId)
	case *syscall.SockaddrUnix:
		fmt.Printf("%s decode: unix name=%q\n", label, s.Name)
	default:
		fmt.Printf("%s decode: unexpected Sockaddr type\n", label)
	}
}

func main() {
	// Winsock has to be started explicitly when syscall is driven directly -- net does this for
	// itself in sysInit, and nothing else here would.
	var data syscall.WSAData
	if err := syscall.WSAStartup(uint32(0x202), &data); err != nil {
		fmt.Println("WSAStartup failed")
		return
	}
	defer syscall.WSACleanup()

	roundTrip("tcp4", syscall.AF_INET, &syscall.SockaddrInet4{Addr: [4]byte{127, 0, 0, 1}})
	roundTrip("tcp6", syscall.AF_INET6, &syscall.SockaddrInet6{Addr: [16]byte{15: 1}})

	// sockaddr_in: port at 2..3 in NETWORK order, sin_addr at 4..7.
	decodeRawAny("raw4", syscall.AF_INET, func(put func(int, byte)) {
		put(2, 0x1F) // port 8080 == 0x1F90, high byte first on the wire
		put(3, 0x90)
		put(4, 203)
		put(5, 0)
		put(6, 113)
		put(7, 42)
	})

	// sockaddr_in6: port 2..3, flowinfo 4..7, sin6_addr 8..23, scope_id 24..27. The address spans
	// Addr.Data[6..13] AND Pad[0..7] -- a decode that stopped at the end of Data would keep the
	// first eight octets and zero the rest, which this line prints in full.
	decodeRawAny("raw6", syscall.AF_INET6, func(put func(int, byte)) {
		put(2, 0x01) // port 500 == 0x01F4
		put(3, 0xF4)
		for i := 0; i < 16; i++ {
			put(8+i, byte(0x10+i))
		}
		put(24, 7) // scope_id 7, little-endian in the native struct
	})

	// sockaddr_un: sun_path begins at offset 2 and runs to the first NUL.
	decodeRawAny("rawunix", syscall.AF_UNIX, func(put func(int, byte)) {
		for i, c := range []byte("go2cs.sock") {
			put(2+i, c)
		}
	})

	// The error arm: an unknown address family must report EAFNOSUPPORT rather than a zero Sockaddr.
	decodeRawAny("rawbad", 0xFEED, func(put func(int, byte)) {})

	fmt.Println("done")
}
