// NetDeadlineMatrix guards the DEADLINE/UNBLOCK semantics of the managed netpoller -- contracts 4,
// 5, 6, 7 and 8 of docs/phase4/DESIGN-netpoll-managed-poller.md, whose §5 prices this as "the hard
// part" and says outright that the arc's iteration budget belongs here rather than on the happy-path
// round trip (that is the sibling TcpLoopbackRoundTrip). The managed implementation lives in
// internal/poll/windows/runtime_netpoll_impl.cs.
//
// The state machine is small; the RACE SURFACE is not, and every line below is one interleaving §5
// enumerates, driven adversarially rather than hoped for:
//
//	blockedReadTimesOut   -- a Read parked in the poller, released by its own deadline firing. The
//	                          error must satisfy os.ErrDeadlineExceeded (Go's contract, and what net's
//	                          own consumers branch on), not merely be non-nil.
//	timeoutIsSticky       -- §5 point 3. A fired deadline stays fired: the NEXT read fails IMMEDIATELY
//	                          (measured, not asserted -- it must not block for the original timeout),
//	                          until a later SetReadDeadline rewrites it. Getting this wrong reads as
//	                          "connection permanently broken after one timeout" or as "timeout not
//	                          sticky", and both are behaviorally loud.
//	clearedDeadlineWorks  -- SetReadDeadline(time.Time{}) is d == 0, which CLEARS both the deadline and
//	                          the sticky expiry; the read that follows must succeed on real data.
//	pastDeadlineNoBlock   -- §5 point 4. A deadline set in the past fires NOW, against the CURRENT
//	                          waiter, and the read returns without ever parking.
//	replacedWhileBlocked  -- §5 point 5, the one .NET semantics FORCE into Go's rseq/wseq shape:
//	                          Timer.Change does not synchronize with an in-flight callback, so without
//	                          a generation check a REPLACED deadline can be expired by the timer it
//	                          replaced. A short deadline is armed, then pushed far out while the reader
//	                          is parked; the read must then be released by DATA, not by a timeout.
//	writeModeIndependent  -- a read deadline that has already fired must not fail a Write ('r' and 'w'
//	                          are separate modes with separate expiry).
//	combinedModeBoth      -- SetDeadline arms 'r'+'w' together (mode == 'r'+'w'), so both fail.
//	closeBeatsTimeout     -- §5 point 7's check ORDER: closing outranks timeout. A conn with an expired
//	                          read deadline, then closed, reports the CLOSE error, not the timeout.
//	deadlineVersusData    -- where the deadline check sits relative to the data, in BOTH directions,
//	                          and the two answers are deliberately OPPOSITE. An already-expired mode
//	                          fails a read even with bytes waiting, because execIO calls pd.prepare()
//	                          before it submits anything; a completion arriving inside an ARMED
//	                          deadline is delivered as data and leaves no stale expiry behind. The
//	                          first was measured against Go rather than assumed -- an earlier draft of
//	                          this guard asserted the opposite and Go disagreed.
//
// Nothing host-varying is printed -- no ports, no error text, no elapsed times (only whether an
// elapsed time fell on the right side of a generous threshold), so the output is identical on any
// host and between Go and the conversion.
package main

import (
	"errors"
	"fmt"
	"net"
	"os"
	"time"
)

// pair returns a connected client/server pair on loopback plus a cleanup. Both ends are real kernel
// sockets going through the whole submit seam; nothing here is a fake.
func pair() (client net.Conn, server net.Conn, cleanup func(), ok bool) {
	listener, err := net.Listen("tcp", "127.0.0.1:0")
	if err != nil {
		return nil, nil, func() {}, false
	}

	accepts := make(chan net.Conn, 1)
	go func() {
		conn, err := listener.Accept()
		if err != nil {
			accepts <- nil
			return
		}
		accepts <- conn
	}()

	client, err = net.Dial("tcp", listener.Addr().String())
	if err != nil {
		listener.Close()
		return nil, nil, func() {}, false
	}

	server = <-accepts
	if server == nil {
		client.Close()
		listener.Close()
		return nil, nil, func() {}, false
	}

	return client, server, func() {
		server.Close()
		client.Close()
		listener.Close()
	}, true
}

func isTimeout(err error) bool {
	return err != nil && errors.Is(err, os.ErrDeadlineExceeded)
}

// blockedReadTimesOut + timeoutIsSticky + clearedDeadlineWorks, in one connection's life so the
// STICKINESS is observed across operations rather than asserted about one.
func deadlineLifecycle() {
	client, server, cleanup, ok := pair()
	if !ok {
		fmt.Println("lifecycle: setup failed")
		return
	}
	defer cleanup()

	buf := make([]byte, 16)

	// (1) A read that parks in the poller and is released by its own deadline.
	client.SetReadDeadline(time.Now().Add(300 * time.Millisecond))
	start := time.Now()
	_, err := client.Read(buf)
	blockedFor := time.Since(start)
	fmt.Printf("lifecycle: blockedReadTimesOut=%v parked=%v\n", isTimeout(err), blockedFor >= 200*time.Millisecond)

	// (2) STICKY: the next read fails at once, with no fresh deadline set. "At once" is the load-
	// bearing half -- a non-sticky implementation would park again for the full original interval.
	start = time.Now()
	_, err2 := client.Read(buf)
	stickyFor := time.Since(start)
	fmt.Printf("lifecycle: timeoutIsSticky=%v immediate=%v\n", isTimeout(err2), stickyFor < 150*time.Millisecond)

	// (3) CLEARED: SetReadDeadline(zero) clears both the deadline and the sticky expiry, and the read
	// that follows succeeds on real data.
	client.SetReadDeadline(time.Time{})
	go func() {
		time.Sleep(50 * time.Millisecond)
		server.Write([]byte("clear"))
	}()
	n, err3 := client.Read(buf)
	fmt.Printf("lifecycle: clearedDeadlineWorks=%v bytes=%v\n", err3 == nil, n == 5)
}

// A deadline already in the past fires NOW against the current waiter: the read returns immediately
// with a timeout and never parks.
func pastDeadline() {
	client, _, cleanup, ok := pair()
	if !ok {
		fmt.Println("past: setup failed")
		return
	}
	defer cleanup()

	client.SetReadDeadline(time.Now().Add(-time.Second))
	buf := make([]byte, 16)
	start := time.Now()
	_, err := client.Read(buf)
	elapsed := time.Since(start)
	fmt.Printf("past: pastDeadlineNoBlock=%v immediate=%v\n", isTimeout(err), elapsed < 150*time.Millisecond)
}

// The generation check. A short deadline is armed, the reader parks, and the deadline is then pushed
// far into the future while the reader is still blocked. The read must be released by DATA -- if the
// superseded timer is allowed to expire the fresh deadline, it is released by a timeout instead.
func replacedWhileBlocked() {
	client, server, cleanup, ok := pair()
	if !ok {
		fmt.Println("replace: setup failed")
		return
	}
	defer cleanup()

	client.SetReadDeadline(time.Now().Add(300 * time.Millisecond))

	go func() {
		// Replace the deadline while the reader is parked, well before the original would fire.
		time.Sleep(100 * time.Millisecond)
		client.SetReadDeadline(time.Now().Add(30 * time.Second))
		// Then release the read with real data, comfortably after the ORIGINAL deadline would have
		// fired -- so a stale timer that survived the replace shows up as a timeout here.
		time.Sleep(400 * time.Millisecond)
		server.Write([]byte("late"))
	}()

	buf := make([]byte, 16)
	n, err := client.Read(buf)
	fmt.Printf("replace: releasedByData=%v bytes=%v notTimeout=%v\n", err == nil, n == 4, !isTimeout(err))
}

// Read and write deadlines are separate modes with separate expiry.
func modeIndependence() {
	client, server, cleanup, ok := pair()
	if !ok {
		fmt.Println("modes: setup failed")
		return
	}
	defer cleanup()

	// Drain whatever the server end is sent, so a Write below cannot back up.
	go func() {
		drain := make([]byte, 64)
		for {
			if _, err := server.Read(drain); err != nil {
				return
			}
		}
	}()

	// Expire the READ mode only.
	client.SetReadDeadline(time.Now().Add(-time.Second))
	buf := make([]byte, 16)
	_, readErr := client.Read(buf)
	_, writeErr := client.Write([]byte("still writable"))
	fmt.Printf("modes: readExpired=%v writeModeIndependent=%v\n", isTimeout(readErr), writeErr == nil)

	// SetDeadline arms BOTH modes ('r'+'w' combined).
	client.SetDeadline(time.Now().Add(-time.Second))
	_, readErr2 := client.Read(buf)
	_, writeErr2 := client.Write([]byte("now neither"))
	fmt.Printf("modes: combinedModeBoth=%v\n", isTimeout(readErr2) && isTimeout(writeErr2))
}

// Check ORDER: closing outranks timeout. The conn's read deadline is already expired when it is
// closed, and the read after that must report the CLOSE, not the timeout.
func closeBeatsTimeout() {
	client, _, cleanup, ok := pair()
	if !ok {
		fmt.Println("order: setup failed")
		return
	}
	defer cleanup()

	client.SetReadDeadline(time.Now().Add(-time.Second))
	buf := make([]byte, 16)
	client.Read(buf) // expire the mode for real
	client.Close()

	_, err := client.Read(buf)
	fmt.Printf("order: closeBeatsTimeout=%v notTimeout=%v\n",
		err != nil && errors.Is(err, net.ErrClosed), !isTimeout(err))
}

// Where the deadline check sits relative to the data, in BOTH directions -- and the two answers are
// deliberately opposite, because they interrogate two different points in execIO's loop.
//
// (a) An ALREADY-EXPIRED mode fails a read even when bytes are sitting in the socket buffer, because
// execIO calls pd.prepare() BEFORE it submits anything and prepare's order is closing > timeout >
// clear-readiness (netpollcheckerr). The buffered bytes are never reached. Measured against Go, not
// assumed: Go answers exactly this.
//
// (b) A completion that arrives while a deadline is ARMED is delivered as data, and the timer that
// was armed for it must not fire afterwards against a mode that has already been satisfied. This is
// the pollWait side, where readiness is consumed ahead of both error checks.
func deadlineVersusData() {
	client, server, cleanup, ok := pair()
	if !ok {
		fmt.Println("race: setup failed")
		return
	}
	defer cleanup()

	buf := make([]byte, 16)

	// (a) Bytes first, then an expired deadline: prepare rejects before the data can be read.
	server.Write([]byte("ready"))
	time.Sleep(200 * time.Millisecond)
	client.SetReadDeadline(time.Now().Add(-time.Second))
	n, err := client.Read(buf)
	fmt.Printf("race: expiredBeatsBufferedData=%v noBytes=%v\n", isTimeout(err), n == 0)

	// (b) A live deadline with data arriving comfortably inside it: the read is released by the
	// completion, and the deadline that was armed for it leaves no residue -- the follow-up read
	// after a clear must succeed rather than inherit a stale expiry.
	client.SetReadDeadline(time.Now().Add(2 * time.Second))
	n2, err2 := client.Read(buf)
	fmt.Printf("race: dataInsideDeadline=%v bytes=%v\n", err2 == nil, n2 == 5)

	client.SetReadDeadline(time.Time{})
	go func() {
		time.Sleep(50 * time.Millisecond)
		server.Write([]byte("after"))
	}()
	n3, err3 := client.Read(buf)
	fmt.Printf("race: noStaleExpiryAfterwards=%v bytes=%v\n", err3 == nil, n3 == 5)
}

func main() {
	deadlineLifecycle()
	pastDeadline()
	replacedWhileBlocked()
	modeIndependence()
	closeBeatsTimeout()
	deadlineVersusData()
	fmt.Println("done")
}
