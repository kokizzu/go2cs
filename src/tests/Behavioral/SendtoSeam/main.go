// SendtoSeam guards the LINUX syscall.Sendto struct-passing seam -- the send half of the wall
// whose receive half SockaddrRoundTrip guards on Windows and whose Recvfrom half is already
// hand-owned (syscall/linux/sockaddr_linux_impl.cs).
//
// Go's Sendto calls `to.sockaddr()` and hands the kernel the unsafe.Pointer it returns. That
// pointer addresses a MANAGED RawSockaddrInet4, whose Addr [4]byte and Zero [8]uint8 are
// `array<byte>` OBJECT REFERENCES rather than inline storage -- one word where the kernel expects
// four address octets and eight zero bytes. The kernel only READS here, so unlike Recvfrom's write
// it cannot smash the managed heap; it simply sends the datagram somewhere else, or fails. The
// hand-own builds the native image with writeNativeSockaddr and hands ITS address to the package's
// own generated `sendto`, exactly as Bind and Connect already do.
//
// A no-fault check proves nothing here, for the same reason SockaddrRoundTrip states it: a mirror
// with the wrong offsets sends a datagram to the wrong place WITHOUT crashing. So every line below
// is a value the KERNEL moved -- a payload that had to arrive at an address the encode produced,
// and a sender address the receiving kernel reported -- never merely the absence of a fault.
//
// The two cases are the two arms of Go's own Sendto:
//
//	addressed -- `to != nil`: sockaddr() runs, the encode is exercised, and the datagram can only
//	             arrive if the four address octets and the port reached the kernel intact.
//	nil-to    -- `to == nil`: Go NEVER calls sockaddr(), leaves the pointer nil and the length 0,
//	             and the kernel uses the socket's connected peer. A hand-own that routed this
//	             through the encoder would answer EAFNOSUPPORT and the datagram would not arrive;
//	             one that passed a bogus non-null pointer would send it elsewhere. Neither failure
//	             faults, and both are caught by the payload simply not being there.
//
// THE RECEIVER BINDS 127.0.0.2, NOT 127.0.0.1, AND THAT IS THE WHOLE POINT OF THE ADDRESSED ARM.
// This guard's first draft used 127.0.0.1 and PASSED against the defective generated body -- the
// control caught it, and the reason is worth stating so nobody re-simplifies it. Instrumenting the
// generated path printed the sixteen bytes it hands the kernel:
//
//	02 00  AE 54  00 00 00 00  30 04 4A 68 ED 7F 00 00
//	family port   ADDRESS      the array<byte> reference sitting where Zero belongs
//
// The family and the port are right -- they are inline scalars -- and the address is 0.0.0.0,
// because the four bytes at that offset are the managed struct's padding, not the octets. Linux
// treats 0.0.0.0 as a DESTINATION meaning "this host", so on 127.0.0.1 the datagram still arrived
// and the guard read green over a send to the wrong address entirely. Binding 127.0.0.2 removes
// that escape: a packet addressed to 0.0.0.0 lands on the local host's 127.0.0.1 and never reaches
// a socket bound to 127.0.0.2, so the payload's ARRIVAL now depends on the octets being transmitted.
//
// Ephemeral ports are never printed -- only that the two datagrams came from the SAME sender, which
// is the cross-check that the nil-to send used the connected socket rather than an address the
// encoder invented -- so the output is identical on any host.
//
// HOW A REGRESSION PRESENTS, stated so the next reader is not surprised. Recvfrom BLOCKS, and the
// failure this guards is "the datagram went somewhere else", so a broken encode does not error --
// it hangs, and the runner's run-timeout reports the project as NOT MEASURED while still failing
// the run and exiting non-zero. That is deliberate: the alternative is SO_RCVTIMEO, which is
// SetsockoptTimeval, which hands the kernel a managed *Timeval and is a member of the very
// struct-passing class under test. A guard must not depend on the thing it guards, so this one
// takes the blocking read and the louder-but-slower signal. Sends precede every receive, so on a
// working loopback the datagram is already queued and no read ever waits.
package main

import (
	"fmt"
	"runtime"
	"syscall"
)

func fatal(what string, err error) {
	if err != nil {
		fmt.Println(what, "failed:", err)
		panic(what)
	}
}

func main() {
	// This guards the LINUX seam only. Raw syscall sockets on Windows need WSAStartup (net performs
	// it, a raw program does not), so on any other host both Go and the converted C# print the one
	// fixed line below and stop before the first socket call -- the behavioral suite stays green
	// everywhere, and the linux run is the real guard. runtime.GOOS converts to the runtime's own
	// constant, so the two sides agree on every host.
	if runtime.GOOS != "linux" {
		fmt.Println("linux-only seam: skipped on", runtime.GOOS)
		return
	}

	// 127.0.0.2, not .1 -- see the header: .1 cannot tell a correct encode from 0.0.0.0.
	receiverAddr := [4]byte{127, 0, 0, 2}

	receiver, err := syscall.Socket(syscall.AF_INET, syscall.SOCK_DGRAM, 0)
	fatal("socket(receiver)", err)
	defer syscall.Close(receiver)

	fatal("bind", syscall.Bind(receiver, &syscall.SockaddrInet4{Addr: receiverAddr, Port: 0}))

	bound, err := syscall.Getsockname(receiver)
	fatal("getsockname", err)
	dst := bound.(*syscall.SockaddrInet4)
	fmt.Println("bound to the discriminating address:", dst.Addr == receiverAddr)
	fmt.Println("a port was assigned:", dst.Port != 0)

	sender, err := syscall.Socket(syscall.AF_INET, syscall.SOCK_DGRAM, 0)
	fatal("socket(sender)", err)
	defer syscall.Close(sender)

	// (1) THE ENCODE. An addressed send: the datagram arrives only if the address bytes did.
	to := &syscall.SockaddrInet4{Addr: dst.Addr, Port: dst.Port}
	fatal("sendto(addressed)", syscall.Sendto(sender, []byte("addressed"), 0, to))

	buf := make([]byte, 64)
	n, from, err := syscall.Recvfrom(receiver, buf, 0)
	fatal("recvfrom(addressed)", err)
	fmt.Println("addressed payload:", string(buf[:n]))

	first := from.(*syscall.SockaddrInet4)
	fmt.Println("addressed sender is in 127/8:", first.Addr[0] == 127)
	fmt.Println("addressed sender has a port:", first.Port != 0)

	// (2) THE NIL-TO ARM. Connect pins the peer; Sendto with a nil address must use it.
	fatal("connect", syscall.Connect(sender, to))
	fatal("sendto(nil-to)", syscall.Sendto(sender, []byte("connected"), 0, nil))

	n, from, err = syscall.Recvfrom(receiver, buf, 0)
	fatal("recvfrom(nil-to)", err)
	fmt.Println("nil-to payload:", string(buf[:n]))

	second := from.(*syscall.SockaddrInet4)
	fmt.Println("nil-to sender equals addressed sender:",
		second.Addr == first.Addr && second.Port == first.Port)
}
