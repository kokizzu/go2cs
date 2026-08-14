// NetListenSmoke guards the managed netpoller -- the ten //go:linkname runtime_poll* contracts that
// internal/poll declares and that go2cs hand-owns in internal/poll/windows/runtime_netpoll_impl.cs
// (docs/phase4/DESIGN-netpoll-managed-poller.md, §9 RULED 2026-08-13).
//
// Before that hand-own, EVERY pollable socket died before any test logic ran. internal/poll emits the
// ten linknames as bodyless partials, the PartialStubGenerator fills them with throwing stubs, and the
// first pollable FD.Init -- which is every socket the net package creates -- panicked inside
// serverInit.Do(runtime_pollServerInit):
//
//	System.NotImplementedException: runtime_pollServerInit: external (assembly or cgo) function is not implemented
//	  at internal/poll.runtime_pollServerInit          (PartialStubGenerator stub)
//	  at internal\poll.pollDesc.init -> internal\poll.FD.Init
//	  at net.netFD.init -> net.listenStream
//	  at net.Listen
//
// This program covers the four contracts a listener's LIFECYCLE reaches, with zero data flow (the
// overlapped read/write path is the S2 half of the arc and is guarded separately):
//
//	runtime_pollServerInit  -- contract 1, once, via serverInit.Do on the first pollable FD.Init.
//	runtime_pollOpen        -- contract 2, per listener; it must mint a nonzero ctx or net.Listen fails.
//	runtime_pollUnblock     -- contract 8, from Close -> pd.evict().
//	runtime_pollClose       -- contract 3, from Close -> decref -> destroy -> pd.close(), which asserts
//	                           it was preceded by an unblock.
//
// A no-fault check would prove very little here, so nothing below is merely "it did not throw". Each
// line prints a value the KERNEL supplied, or a relationship between two of them that only holds if
// the whole open/close cycle really happened:
//
//	portAssigned      -- the kernel picked an ephemeral port for :0, so a listener that never reached
//	                     bind (or whose ctx was bogus) cannot show true.
//	distinctFromFirst -- two live listeners hold two different ports, so pollOpen minting one shared
//	                     or colliding registration would show false.
//	reboundSamePort   -- re-listening on the very port the second listener just released. This is the
//	                     strongest line: it can only succeed if Close actually reached the kernel, which
//	                     means pollUnblock and pollClose both ran and pollClose released the poller's
//	                     registration BEFORE internal/poll closed the socket (FD.destroy's ordering).
//	closedIsSticky    -- a second Close reports an error rather than succeeding twice, which is the
//	                     observable face of the desc's sticky `closing`.
//	deadlineAccepted  -- SetDeadline/clear round trip (contract 7) on a live listener. Arming and
//	                     clearing are pure poller calls with no IO; the deadline SEMANTICS matrix
//	                     (sticky expiry, replace-while-blocked, cancel-and-harvest) needs a blocked
//	                     operation and belongs to the S2 guard.
//
// The ephemeral port numbers themselves are never printed -- only whether one was assigned, whether
// two differ, and whether a rebind landed on the same one -- so the output is identical on any host.
//
// Windows-first, like the sibling SockaddrRoundTrip guard for the layer beneath it: the ten bodies
// live in internal/poll/windows/ and other GOOS keep today's throwing stubs (design §8).
package main

import (
	"fmt"
	"net"
	"strconv"
	"time"
)

// portOf pulls the numeric port out of a listener's address. It goes through the concrete *net.TCPAddr
// rather than through Addr().String() so the answer comes from the decoded sockaddr the kernel filled
// in, not from text the program could have fabricated.
func portOf(l net.Listener) (int, bool) {
	addr, ok := l.Addr().(*net.TCPAddr)
	if !ok {
		return 0, false
	}
	return addr.Port, true
}

func main() {
	// Contracts 1 and 2: the first pollable FD.Init runs serverInit.Do(runtime_pollServerInit) and
	// then runtime_pollOpen. This single call is the wall the whole arc exists to retreat.
	first, err := net.Listen("tcp", "127.0.0.1:0")
	if err != nil {
		fmt.Println("listener1: listen failed")
		return
	}

	firstPort, ok1 := portOf(first)
	fmt.Printf("listener1: network=%s portAssigned=%v\n", first.Addr().Network(), ok1 && firstPort > 0)

	// A second registration, live at the same time as the first: two descs, two ctx tokens.
	second, err := net.Listen("tcp", "127.0.0.1:0")
	if err != nil {
		fmt.Println("listener2: listen failed")
		first.Close()
		return
	}

	secondPort, ok2 := portOf(second)
	fmt.Printf("listener2: portAssigned=%v distinctFromFirst=%v\n",
		ok2 && secondPort > 0, ok1 && ok2 && firstPort != secondPort)

	// Contract 7, smoke only: arm a deadline far in the future, then clear it. Both are pure poller
	// calls -- no IO is submitted -- so what is proved here is that SetDeadline reaches the managed
	// desc and returns cleanly for both the arm and the clear.
	armErr := first.(*net.TCPListener).SetDeadline(time.Now().Add(time.Hour))
	clearErr := first.(*net.TCPListener).SetDeadline(time.Time{})
	fmt.Printf("listener1: deadlineAccepted=%v deadlineClearAccepted=%v\n", armErr == nil, clearErr == nil)

	// Contracts 8 and 3: Close runs pd.evict() (unblock) and then, through decref -> destroy,
	// pd.close(). pollClose asserts it was preceded by an unblock, so a wrong order throws here
	// rather than leaking a registration quietly.
	closeErr := second.Close()
	fmt.Printf("listener2: closed=%v\n", closeErr == nil)

	// The strongest line: the port the second listener just released is available again. That is only
	// true if Close reached the kernel, which in turn requires pollClose to have released the poller's
	// registration before internal/poll closed the socket.
	rebound, err := net.Listen("tcp", "127.0.0.1:"+strconv.Itoa(secondPort))
	if err != nil {
		fmt.Println("rebound: listen failed")
		first.Close()
		return
	}

	reboundPort, ok3 := portOf(rebound)
	fmt.Printf("rebound: reboundSamePort=%v\n", ok3 && reboundPort == secondPort)

	// Closing is sticky: the second Close finds the FD already closed and reports it.
	fmt.Printf("listener2: closedIsSticky=%v\n", second.Close() != nil)

	fmt.Printf("cleanup: %v %v\n", rebound.Close() == nil, first.Close() == nil)
	fmt.Println("done")
}
