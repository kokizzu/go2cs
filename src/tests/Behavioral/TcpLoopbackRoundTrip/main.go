// TcpLoopbackRoundTrip guards the SUBMIT SEAM of the managed netpoller arc -- the overlapped socket
// wrappers go2cs hand-owns in syscall/windows/zsyscall_windows_wsa_impl.cs and
// internal/syscall/windows/windows/zsyscall_windows_wsa_impl.cs, driven by the ten runtime_poll*
// contracts in internal/poll/windows/runtime_netpoll_impl.cs
// (docs/phase4/DESIGN-netpoll-managed-poller.md §4.3/§4.4/§4.5).
//
// Its sibling NetListenSmoke proved a listener's LIFECYCLE with zero data flow. This program proves
// the thing no lifecycle test can: that BYTES ARRIVE, and arrive correctly. That distinction is the
// whole reason this guard exists -- the native WSABUF mirroring, the pinned user buffers and the
// transferred counts are exactly the "returns garbage without crashing" class, so every line below
// compares a VALUE the kernel moved, never the absence of a fault.
//
// What each section reaches, in the vocabulary of the design's contract inventory:
//
//	listen/dial/accept  -- AcceptEx + ConnectEx + GetAcceptExSockaddrs. ConnectEx is doubly loaded: it
//	                       needs the extension-function POINTER, whose WSAIoctl lookup was itself
//	                       defective (the converted GUID is reference-bearing, so the 16 bytes Windows
//	                       compared were a CLR auto-layout image and every dial died with
//	                       "failed to find ConnectEx: An invalid argument was supplied").
//	echo               -- WSARecv + WSASend through execIO: prepare (contract 6), submit, wait
//	                       (contract 4) on ERROR_IO_PENDING, harvest via WSAGetOverlappedResult.
//	address decode     -- GetAcceptExSockaddrs' output parsed by RawSockaddrAny.Sockaddr. The two are
//	                       a hand-owned PAIR: one transcribes the kernel's native accept buffer into
//	                       the managed struct, the other reads that managed image back out. Asserting
//	                       that the server sees the client's own local address is what proves the
//	                       transcription, and it cannot be faked -- the port is ephemeral.
//	close-unblocks     -- contract 8 (pollUnblock) against a BLOCKED reader and a BLOCKED writer on
//	                       other goroutines, then the cancel-and-harvest path (contract 5,
//	                       pollWaitCanceled): execIO may not abandon a kernel-pending operation, so it
//	                       issues CancelIoEx -- BY ADDRESS, which is why the overlapped must be the
//	                       record's one true native control block -- and waits for the completion the
//	                       kernel always posts for a cancelled operation.
//
// Nothing host-varying is printed: ephemeral ports appear only as relationships between two of them,
// never as numbers, and no error TEXT is printed (only whether an error occurred), so the output is
// identical on any host and between Go and the conversion.
package main

import (
	"fmt"
	"io"
	"net"
	"time"
)

// payload is deliberately larger than one TCP segment so a Read is not guaranteed to return it all
// in one call: io.ReadFull then exercises the submit/wait/harvest loop repeatedly, and a transferred
// COUNT that is wrong by a partial buffer shows up as a mismatch rather than as a lucky pass.
const payloadSize = 64 * 1024

func makePayload() []byte {
	buf := make([]byte, payloadSize)
	// A deterministic, non-constant fill: a wrong offset or a truncated copy changes the sum.
	for i := range buf {
		buf[i] = byte(i*7 + 11)
	}
	return buf
}

func checksum(b []byte) uint32 {
	var sum uint32
	for _, c := range b {
		sum = sum*31 + uint32(c)
	}
	return sum
}

// roundTrip runs one full listen -> dial -> accept -> write -> echo -> read cycle on the given
// network and loopback address, and reports the facts worth comparing.
func roundTrip(label, network, address string) {
	listener, err := net.Listen(network, address)
	if err != nil {
		fmt.Printf("%s: listen failed\n", label)
		return
	}
	defer listener.Close()

	payload := makePayload()
	want := checksum(payload)

	type accepted struct {
		conn net.Conn
		err  error
	}
	accepts := make(chan accepted, 1)

	go func() {
		conn, err := listener.Accept()
		accepts <- accepted{conn, err}
	}()

	client, err := net.Dial(network, listener.Addr().String())
	if err != nil {
		fmt.Printf("%s: dial failed\n", label)
		return
	}
	defer client.Close()

	got := <-accepts
	if got.err != nil {
		fmt.Printf("%s: accept failed\n", label)
		return
	}
	server := got.conn
	defer server.Close()

	// The ACCEPT-PATH ADDRESS DECODE. The accepted conn's local and remote addresses come from
	// GetAcceptExSockaddrs, decoded through RawSockaddrAny.Sockaddr -- the one route to a Sockaddr
	// that the hand-owned Getsockname/Getpeername do not cover. A wrong transcription shows here as
	// a mismatched ephemeral port or a zero address, both of which these comparisons catch without
	// printing either.
	fmt.Printf("%s: serverSawClientAddr=%v\n", label, server.RemoteAddr().String() == client.LocalAddr().String())
	fmt.Printf("%s: clientSawServerAddr=%v\n", label, client.RemoteAddr().String() == listener.Addr().String())
	fmt.Printf("%s: serverLocalIsListenAddr=%v\n", label, server.LocalAddr().String() == listener.Addr().String())

	// Server echoes exactly payloadSize bytes back.
	echoed := make(chan int, 1)
	go func() {
		buf := make([]byte, payloadSize)
		n, err := io.ReadFull(server, buf)
		if err != nil {
			echoed <- -1
			return
		}
		m, err := server.Write(buf[:n])
		if err != nil {
			echoed <- -2
			return
		}
		echoed <- m
	}()

	written, err := client.Write(payload)
	if err != nil {
		fmt.Printf("%s: client write failed\n", label)
		return
	}

	back := make([]byte, payloadSize)
	read, err := io.ReadFull(client, back)
	if err != nil {
		fmt.Printf("%s: client read failed\n", label)
		return
	}

	serverEchoed := <-echoed

	fmt.Printf("%s: clientWroteAll=%v serverEchoedAll=%v clientReadAll=%v\n", label, written == payloadSize, serverEchoed == payloadSize, read == payloadSize)
	fmt.Printf("%s: payloadMatches=%v\n", label, checksum(back) == want)
}

// closeBreaksBlockedRead: a Read parked in the poller on one goroutine, released by a Close on
// another. This is contract 8 driving contract 4's waiter, then execIO's cancel-and-harvest.
func closeBreaksBlockedRead() {
	listener, err := net.Listen("tcp", "127.0.0.1:0")
	if err != nil {
		fmt.Println("closeRead: listen failed")
		return
	}
	defer listener.Close()

	accepts := make(chan net.Conn, 1)
	go func() {
		conn, err := listener.Accept()
		if err != nil {
			accepts <- nil
			return
		}
		accepts <- conn
	}()

	client, err := net.Dial("tcp", listener.Addr().String())
	if err != nil {
		fmt.Println("closeRead: dial failed")
		return
	}
	server := <-accepts
	if server == nil {
		fmt.Println("closeRead: accept failed")
		return
	}
	defer server.Close()

	done := make(chan error, 1)
	go func() {
		buf := make([]byte, 16)
		_, err := client.Read(buf)
		done <- err
	}()

	// Let the reader reach the poller. Nothing is ever sent, so the Read can only be released by the
	// Close below -- a read that returned on its own would report a nil error and fail the line.
	time.Sleep(200 * time.Millisecond)
	client.Close()

	select {
	case err := <-done:
		fmt.Printf("closeRead: brokeBlockedRead=%v\n", err != nil)
	case <-time.After(10 * time.Second):
		fmt.Println("closeRead: brokeBlockedRead=false (timed out)")
	}
}

// closeBreaksBlockedWrite is the shape internal/poll's own TestConnCloseBreakingWrite drives, and the
// one the crypto/tls census banked as an indefinite HANG: a Write on one goroutine must be broken by
// a Close on another. The peer never reads, so the writer eventually fills the socket buffers and
// parks in the poller; whether it parks on this host or merely fails afterwards, the loop must END.
func closeBreaksBlockedWrite() {
	listener, err := net.Listen("tcp", "127.0.0.1:0")
	if err != nil {
		fmt.Println("closeWrite: listen failed")
		return
	}
	defer listener.Close()

	accepts := make(chan net.Conn, 1)
	go func() {
		conn, err := listener.Accept()
		if err != nil {
			accepts <- nil
			return
		}
		accepts <- conn
	}()

	client, err := net.Dial("tcp", listener.Addr().String())
	if err != nil {
		fmt.Println("closeWrite: dial failed")
		return
	}
	server := <-accepts
	if server == nil {
		fmt.Println("closeWrite: accept failed")
		return
	}
	// Deliberately never read from `server`, so the client's writes back up.
	defer server.Close()

	done := make(chan error, 1)
	go func() {
		buf := make([]byte, 64*1024)
		for {
			if _, err := client.Write(buf); err != nil {
				done <- err
				return
			}
		}
	}()

	time.Sleep(500 * time.Millisecond)
	client.Close()

	select {
	case err := <-done:
		fmt.Printf("closeWrite: brokeBlockedWrite=%v\n", err != nil)
	case <-time.After(20 * time.Second):
		fmt.Println("closeWrite: brokeBlockedWrite=false (timed out)")
	}
}

func main() {
	roundTrip("ipv4", "tcp", "127.0.0.1:0")

	// IPv6 loopback, when the host has it. Both sides of the comparison run on the SAME host, so a
	// host without IPv6 still produces matching output -- it just takes the other branch.
	if probe, err := net.Listen("tcp", "[::1]:0"); err == nil {
		probe.Close()
		fmt.Println("ipv6: available=true")
		roundTrip("ipv6", "tcp", "[::1]:0")
	} else {
		fmt.Println("ipv6: available=false")
	}

	closeBreaksBlockedRead()
	closeBreaksBlockedWrite()

	fmt.Println("done")
}
