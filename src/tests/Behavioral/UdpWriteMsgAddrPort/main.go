// UdpWriteMsgAddrPort guards internal/poll's two raw-sockaddr ENCODERS -- sockaddrInet4ToRaw and
// sockaddrInet6ToRaw (fd_windows.go) -- the exact twins of the two DECODERS
// internal/poll/windows/fd_windows_impl.cs already hand-owns. Its sibling UdpLoopbackRoundTrip
// covers the sendto/recvfrom datagram seam; nothing before this program reached the WriteMsg path,
// which is the only route into the encoders.
//
// WHY THE ENCODERS ARE A CRASH CLASS AND NOT A WRONG-VALUE ONE. Go fills the raw address by pointer
// arithmetic over flat bytes:
//
//	raw := (*syscall.RawSockaddrInet6)(unsafe.Pointer(rsa))
//	raw.Family = syscall.AF_INET6
//	p := (*[2]byte)(unsafe.Pointer(&raw.Port))
//	p[0], p[1] = byte(sa.Port>>8), byte(sa.Port)
//
// Both lines are the same two mechanisms the decoders' hand-own documents, run in reverse and
// therefore WRITING rather than reading. The reinterpret aliases a reference-bearing RawSockaddrAny
// as a reference-bearing RawSockaddrInet6 whose managed layout shares no offset with it, so
// `raw.Value.Family = AF_INET6` deposits a uint16 over the low half of a LIVE OBJECT REFERENCE; the
// byte view then fabricates an `array<byte>` out of whatever bytes follow. The measured symptoms are
// `index out of range [0] with length 0` on the v4 twin and the same panic with a garbage NEGATIVE
// length on the v6 twin -- and, once the corrupted reference is collected, a fatal
// ExecutionEngineException whose death site moves from run to run.
//
// WHAT EACH LINE BELOW PROVES, and why none of it can be faked:
//
//	writes  -- the encoder ran to completion at all. Before the hand-own this never exceeds 0.
//	reads   -- the datagram ARRIVED at the socket the encoded address named. A wrong port or family
//	           in the raw image sends it elsewhere and this read hits its deadline instead.
//	bytes   -- the payload survived, so the address image did not overlap the buffer registration.
//	sender  -- the receiver's view of the sender is the client's own port, which pins the whole
//	           round trip to the two sockets this function opened rather than to any datagram.
//
// BOTH entry points into the encoders are exercised: WriteMsgUDPAddrPort reaches
// poll.WriteMsgInet4/WriteMsgInet6, which call them directly, while WriteMsgUDP reaches the generic
// poll.WriteMsg, which calls them through sockaddrToRaw. They are separate call sites in the
// converted package and a hand-own that covered only one would pass a guard that used only one.
//
// FOUR HAND-OWNS STAND BEHIND THESE LINES, and each was found only once its predecessor was fixed:
// the encoders here, then internal/syscall/windows's loadWSASendRecvMsg (whose GUID for the
// WSASendMsg extension is not blittable, so the lookup failed and reported the CALLER's name), then
// WSASendMsg itself (which handed the kernel the address of the managed WSAMsg), then its HARVEST
// twin WSARecvMsg. The counts below guard the whole chain: the first defect kills the process, and
// any of the other three leaves a column at zero.
//
// IT IS A ROUND TRIP ON PURPOSE. The read side used ReadFrom while WSARecvMsg was still open, so the
// guard measured the write chain alone; it now reads with ReadMsg, which is what makes the DECODERS
// reachable from this program at all -- ReadFrom takes recvfrom, a different seam. Write and read
// therefore exercise the two directions of one surface, and `sender` is the line that binds them:
// the address the server reads back is transcribed by the harvest out of native memory into the very
// box internal/poll then decodes, so a wrong transcription cannot produce the client's own port.
//
// The oob buffer is nil, matching net's real Windows surface: censused across the 1.23.12 suite,
// every Windows-reachable ReadMsg call site passes literal nil, and the only non-empty case is
// unixsock_readmsg_test.go's SCM_RIGHTS, which is //go:build unix. `oobn0` therefore asserts that a
// nil control buffer reports zero control bytes rather than whatever the previous receive left --
// which is a real assertion about the harvest's writeback, not a tautology. `flags0` asserts the
// other writeback the harvest owes; no test in net's own suite ever reads that value.
//
// The rounds count is not decoration. The first call is enough to panic, but the reference the
// encoder overwrites is only fatal once the collector reaches it, so the loop allocates on every
// iteration to keep a collection likely inside the guarded window -- the difference between
// observing the panic and observing the process death that follows it.
//
// Nothing host-varying is printed: ports appear only as an equality between two of them, error text
// is never printed (only whether an error occurred), and IPv6's absence is reported as a value both
// sides agree on rather than as a failure.
package main

import (
	"fmt"
	"net"
	"net/netip"
	"time"
)

// rounds is per family and per entry point.
const rounds = 200

func main() {
	writeMsgAddrPortRoundTrip("udp4", "127.0.0.1")
	writeMsgAddrPortRoundTrip("udp6", "::1")
	writeMsgUDPRoundTrip("udp4", "127.0.0.1")
	writeMsgUDPRoundTrip("udp6", "::1")
	fmt.Println("done")
}

// listenPair opens the two unconnected sockets every case below needs, reporting a family the host
// does not have as an availability fact rather than as a failure.
func listenPair(network, host string) (server, client *net.UDPConn, ok bool) {
	server, err := net.ListenUDP(network, &net.UDPAddr{IP: net.ParseIP(host), Port: 0})
	if err != nil {
		return nil, nil, false
	}
	client, err = net.ListenUDP(network, &net.UDPAddr{IP: net.ParseIP(host), Port: 0})
	if err != nil {
		server.Close()
		return nil, nil, false
	}
	return server, client, true
}

// writeMsgAddrPortRoundTrip drives poll.WriteMsgInet4/WriteMsgInet6, which call the encoders
// directly -- the shorter of the two routes and the one net's own
// TestIPv6WriteMsgUDPAddrPortTargetAddrIPVersion takes.
func writeMsgAddrPortRoundTrip(network, host string) {
	server, client, ok := listenPair(network, host)
	if !ok {
		fmt.Printf("addrport %s: available=false\n", network)
		return
	}
	defer server.Close()
	defer client.Close()

	fmt.Printf("addrport %s: available=true\n", network)

	target := netip.AddrPortFrom(netip.MustParseAddr(host), uint16(server.LocalAddr().(*net.UDPAddr).Port))
	clientPort := uint16(client.LocalAddr().(*net.UDPAddr).Port)

	payload := []byte("write-msg-addr-port-payload")
	buf := make([]byte, 64)
	writes, reads, bytes, sender, oob0, flag0 := 0, 0, 0, 0, 0, 0

	for i := 0; i < rounds; i++ {
		// Keep a collection likely while a corrupted reference would still be reachable; the
		// value is never read, so it cannot affect the printed result.
		_ = make([]byte, 512)

		n, oobn, err := client.WriteMsgUDPAddrPort(payload, nil, target)
		if err != nil || n != len(payload) || oobn != 0 {
			break
		}
		writes++

		server.SetReadDeadline(time.Now().Add(5 * time.Second))
		rn, roobn, flags, from, err := server.ReadMsgUDPAddrPort(buf, nil)
		if err != nil {
			break
		}
		reads++
		if string(buf[:rn]) == string(payload) {
			bytes++
		}
		if from.Port() == clientPort {
			sender++
		}
		if roobn == 0 {
			oob0++
		}
		if flags == 0 {
			flag0++
		}
	}

	fmt.Printf("addrport %s: writes=%d reads=%d bytes=%d sender=%d oobn0=%d flags0=%d\n",
		network, writes, reads, bytes, sender, oob0, flag0)
}

// writeMsgUDPRoundTrip drives the generic poll.WriteMsg, which reaches the same two encoders through
// sockaddrToRaw -- a different call site in the converted package, so a hand-own that missed it
// would still fail here.
func writeMsgUDPRoundTrip(network, host string) {
	server, client, ok := listenPair(network, host)
	if !ok {
		fmt.Printf("udpaddr %s: available=false\n", network)
		return
	}
	defer server.Close()
	defer client.Close()

	fmt.Printf("udpaddr %s: available=true\n", network)

	target := server.LocalAddr().(*net.UDPAddr)
	clientPort := client.LocalAddr().(*net.UDPAddr).Port

	payload := []byte("write-msg-udpaddr-payload")
	buf := make([]byte, 64)
	writes, reads, bytes, sender, oob0, flag0 := 0, 0, 0, 0, 0, 0

	for i := 0; i < rounds; i++ {
		_ = make([]byte, 512)

		n, oobn, err := client.WriteMsgUDP(payload, nil, target)
		if err != nil || n != len(payload) || oobn != 0 {
			break
		}
		writes++

		server.SetReadDeadline(time.Now().Add(5 * time.Second))
		rn, roobn, flags, from, err := server.ReadMsgUDP(buf, nil)
		if err != nil {
			break
		}
		reads++
		if string(buf[:rn]) == string(payload) {
			bytes++
		}
		if from.Port == clientPort {
			sender++
		}
		if roobn == 0 {
			oob0++
		}
		if flags == 0 {
			flag0++
		}
	}

	fmt.Printf("udpaddr %s: writes=%d reads=%d bytes=%d sender=%d oobn0=%d flags0=%d\n",
		network, writes, reads, bytes, sender, oob0, flag0)
}
