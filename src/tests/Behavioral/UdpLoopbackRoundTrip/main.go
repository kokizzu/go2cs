// UdpLoopbackRoundTrip guards the LINUX UDP SEAM -- the //go:linkname datagram helpers go2cs
// hand-owns in internal/syscall/unix/linux/net_linux_impl.cs, which are the corpus's entire
// datagram surface (docs/phase4/DESIGN-linux-udp.md, S1).
//
// Its siblings guard the neighbouring seams: NetListenSmoke a listener's lifecycle,
// TcpLoopbackRoundTrip the STREAM path's bytes, NetDeadlineMatrix the poller's deadline semantics.
// This program proves the thing none of them touch -- that a DATAGRAM's bytes arrive AND that the
// address travelling beside them is correct in both directions.
//
// That second half is the point. Before the hand-own, `internal/poll.ReadFromInet4` reached a
// PartialStubGenerator stub and every UDP read threw; the converted `syscall` copies of the same
// helpers (dead code, but the conversion the hand-own replaces) got the address wrong TWICE over --
// they handed the kernel a managed RawSockaddrAny by address, then decoded the port through the
// `(*[2]byte)(unsafe.Pointer(&pp.Port))` alias that converts to a length-zero array<byte>. Both are
// silent-wrong-value classes, not crash classes, so every line below compares a VALUE the kernel
// moved or an address the kernel reported -- never merely the absence of a fault.
//
// What each section reaches:
//
//	ListenPacket/ReadFrom -- RecvfromInet4/6: the kernel fills a native sockaddr this file's
//	                         hand-own owns, and the mirror decodes it into the caller's box.
//	WriteTo               -- SendtoInet4/6: the managed Sockaddr is encoded into a stack image
//	                         through the mirror's seam, and its ADDRESS is what the kernel gets.
//	peer-address identity -- the server's ReadFrom must report exactly the client's own local
//	                         address. This is what proves the decode, and it cannot be faked: the
//	                         port is ephemeral, so a wrong-endian or zero-length read cannot match.
//	zero-length datagram  -- a real UDP case with no payload at all, where `n == 0` must mean
//	                         "a datagram arrived" and not "nothing happened".
//	connected UDP         -- Dial/Write/Read, which take the plain read/write path rather than
//	                         recvfrom, so the two paths are distinguished rather than conflated.
//
// Nothing host-varying is printed: ephemeral ports appear only as relationships between two of them,
// never as numbers, and no error TEXT is printed (only whether an error occurred), so the output is
// identical on any host and between Go and the conversion.
package main

import (
	"fmt"
	"net"
	"time"
)

func main() {
	unconnectedRoundTrip()
	zeroLengthDatagram()
	connectedRoundTrip()
	ipv6RoundTrip()
	fmt.Println("done")
}

// unconnectedRoundTrip is the seam's core case: WriteTo/ReadFrom on an unconnected socket, where
// every datagram carries an address the kernel writes and the hand-own must decode.
func unconnectedRoundTrip() {
	server, err := net.ListenPacket("udp4", "127.0.0.1:0")
	if err != nil {
		fmt.Println("ipv4: listen failed")
		return
	}
	defer server.Close()

	client, err := net.ListenPacket("udp4", "127.0.0.1:0")
	if err != nil {
		fmt.Println("ipv4: client listen failed")
		return
	}
	defer client.Close()

	payload := []byte("datagram-payload")
	if _, err = client.WriteTo(payload, server.LocalAddr()); err != nil {
		fmt.Println("ipv4: WriteTo failed")
		return
	}

	server.SetReadDeadline(time.Now().Add(5 * time.Second))
	buf := make([]byte, 64)
	n, from, err := server.ReadFrom(buf)
	if err != nil {
		fmt.Println("ipv4: ReadFrom failed")
		return
	}

	fmt.Printf("ipv4: bytesMatch=%v\n", string(buf[:n]) == string(payload))
	// The decode's real test: the address the server was told matches the client's own, port and all.
	fmt.Printf("ipv4: senderAddrMatchesClient=%v\n", from.String() == client.LocalAddr().String())

	// And the reverse direction, so the ENCODE is proven against an address the kernel just gave us
	// rather than one we constructed.
	if _, err = server.WriteTo([]byte("reply"), from); err != nil {
		fmt.Println("ipv4: reply WriteTo failed")
		return
	}
	client.SetReadDeadline(time.Now().Add(5 * time.Second))
	rn, rfrom, rerr := client.ReadFrom(buf)
	if rerr != nil {
		fmt.Println("ipv4: reply ReadFrom failed")
		return
	}
	fmt.Printf("ipv4: replyMatches=%v replyFromServer=%v\n",
		string(buf[:rn]) == "reply", rfrom.String() == server.LocalAddr().String())
}

// zeroLengthDatagram is a real UDP case the stream tests cannot express: n == 0 must mean a datagram
// arrived, not that nothing happened.
func zeroLengthDatagram() {
	server, err := net.ListenPacket("udp4", "127.0.0.1:0")
	if err != nil {
		fmt.Println("zerolen: listen failed")
		return
	}
	defer server.Close()

	client, err := net.ListenPacket("udp4", "127.0.0.1:0")
	if err != nil {
		fmt.Println("zerolen: client listen failed")
		return
	}
	defer client.Close()

	if _, err = client.WriteTo([]byte{}, server.LocalAddr()); err != nil {
		fmt.Println("zerolen: WriteTo failed")
		return
	}
	server.SetReadDeadline(time.Now().Add(5 * time.Second))
	buf := make([]byte, 8)
	n, from, err := server.ReadFrom(buf)
	fmt.Printf("zerolen: arrived=%v length=%v senderKnown=%v\n",
		err == nil, n == 0, err == nil && from.String() == client.LocalAddr().String())
}

// connectedRoundTrip uses Dial, whose Read/Write take the plain read(2)/write(2) path rather than
// recvfrom/sendto -- so a failure here is a DIFFERENT seam from the one above, and keeping them
// separate is what makes the guard diagnostic rather than merely red.
func connectedRoundTrip() {
	server, err := net.ListenPacket("udp4", "127.0.0.1:0")
	if err != nil {
		fmt.Println("connected: listen failed")
		return
	}
	defer server.Close()

	conn, err := net.Dial("udp4", server.LocalAddr().String())
	if err != nil {
		fmt.Println("connected: dial failed")
		return
	}
	defer conn.Close()

	if _, err = conn.Write([]byte("connected-payload")); err != nil {
		fmt.Println("connected: write failed")
		return
	}
	server.SetReadDeadline(time.Now().Add(5 * time.Second))
	buf := make([]byte, 64)
	n, from, err := server.ReadFrom(buf)
	if err != nil {
		fmt.Println("connected: ReadFrom failed")
		return
	}
	fmt.Printf("connected: bytesMatch=%v senderMatchesLocal=%v\n",
		string(buf[:n]) == "connected-payload", from.String() == conn.LocalAddr().String())

	if _, err = server.WriteTo([]byte("connected-reply"), from); err != nil {
		fmt.Println("connected: reply failed")
		return
	}
	conn.SetReadDeadline(time.Now().Add(5 * time.Second))
	rn, rerr := conn.Read(buf)
	fmt.Printf("connected: replyMatches=%v\n", rerr == nil && string(buf[:rn]) == "connected-reply")
}

// ipv6RoundTrip exercises the Inet6 half of the seam, whose sockaddr is a different native layout
// (flowinfo and scope id beside the 16-byte address) and therefore a genuinely separate encode and
// decode rather than a repetition of the IPv4 case.
func ipv6RoundTrip() {
	server, err := net.ListenPacket("udp6", "[::1]:0")
	if err != nil {
		// A host without IPv6 loopback is a legitimate environment, and Go and the conversion agree
		// on it, so the guard reports availability rather than failing.
		fmt.Println("ipv6: available=false")
		return
	}
	defer server.Close()

	client, err := net.ListenPacket("udp6", "[::1]:0")
	if err != nil {
		fmt.Println("ipv6: available=false")
		return
	}
	defer client.Close()

	fmt.Println("ipv6: available=true")

	if _, err = client.WriteTo([]byte("v6-payload"), server.LocalAddr()); err != nil {
		fmt.Println("ipv6: WriteTo failed")
		return
	}
	server.SetReadDeadline(time.Now().Add(5 * time.Second))
	buf := make([]byte, 64)
	n, from, err := server.ReadFrom(buf)
	if err != nil {
		fmt.Println("ipv6: ReadFrom failed")
		return
	}
	fmt.Printf("ipv6: bytesMatch=%v senderAddrMatchesClient=%v\n",
		string(buf[:n]) == "v6-payload", from.String() == client.LocalAddr().String())
}
