package main

// Go 1.23 made a Timer/Ticker channel SYNCHRONOUS (proposal #37196). The whole change is one
// guarantee: a Stop or Reset prevents any tick generated BEFORE the call from being received AFTER
// it. This program checks that guarantee, its two visible consequences (Stop/Reset report `pending`
// when they revoke a tick; len/cap of a timer channel are always 0), and — just as important — its
// two boundaries: a Reset to an imminent deadline must still DELIVER, and a func timer (AfterFunc)
// has no channel and must be untouched by any of it.
//
// Sections D and E are deliberately repetitive rather than single-shot: revoking a tick races the
// timer service producing one, and that race has a window only a few instructions wide. Each
// iteration sleeps a different fraction of the tick period before stopping/resetting, so the call
// lands at every phase relative to a firing. A "stale" count of anything but 0 is the whole failure
// mode of this arc.

import (
	"fmt"
	"time"
)

func tryRecv(c <-chan time.Time) bool {
	select {
	case <-c:
		return true
	default:
		return false
	}
}

func main() {
	// A. Stop AFTER the timer has already fired. The timer is no longer armed, so the only thing
	// left to report is the tick sitting unreceived — pending is true because Stop revoked it, and
	// nothing can be received afterwards.
	a := time.NewTimer(1 * time.Millisecond)
	time.Sleep(25 * time.Millisecond)
	aPending := a.Stop()
	fmt.Println("A stop-after-fire pending:", aPending, "stale:", tryRecv(a.C))

	// B. Reset after the timer has already fired: same revocation, same answer, and the re-arm is
	// far enough out that nothing arrives in its place.
	b := time.NewTimer(1 * time.Millisecond)
	time.Sleep(25 * time.Millisecond)
	bPending := b.Reset(time.Hour)
	fmt.Println("B reset-after-fire pending:", bPending, "stale:", tryRecv(b.C))

	// C. A timer channel presents as unbuffered even while it holds an unreceived tick — the value
	// may still be confiscated, so len/cap must not advertise it.
	c := time.NewTimer(1 * time.Millisecond)
	time.Sleep(25 * time.Millisecond)
	fmt.Println("C len:", len(c.C), "cap:", cap(c.C))
	fmt.Println("C receive:", !(<-c.C).IsZero())

	// D. Ticker Stop, raced against the ticker firing. Each iteration waits a different fraction of
	// the period first, so Stop lands before, during and after a firing across the run.
	const period = 200 * time.Microsecond
	dStale := 0
	for i := 0; i < 300; i++ {
		tk := time.NewTicker(period)
		time.Sleep(time.Duration(i%9) * period / 4)
		tk.Stop()
		if tryRecv(tk.C) {
			dStale++
		}
	}
	fmt.Println("D ticker stop stale:", dStale)

	// E. Ticker Reset, same race. Resetting far out must leave nothing receivable; re-arming to the
	// period afterwards restarts the ticking for the next iteration.
	eStale := 0
	tk := time.NewTicker(period)
	for i := 0; i < 300; i++ {
		time.Sleep(time.Duration(i%9) * period / 4)
		tk.Reset(time.Hour)
		if tryRecv(tk.C) {
			eStale++
		}
		tk.Reset(period)
	}
	tk.Stop()
	fmt.Println("E ticker reset stale:", eStale)

	// F. The counter-property, and the reason revocation cannot simply empty the channel whenever it
	// feels like it: a Reset to an imminent deadline must still deliver its OWN tick. An
	// implementation that drains without excluding ticks produced after the re-arm hangs here.
	fDelivered := 0
	for i := 0; i < 200; i++ {
		t := time.NewTimer(time.Hour)
		t.Reset(time.Millisecond)
		<-t.C
		fDelivered++
	}
	fmt.Println("F reset-to-imminent delivered:", fDelivered)

	// G. AfterFunc has no channel at all, so none of the above applies to it: Stop reports only
	// whether the timer was still armed, and after the function has run that is false.
	done := make(chan bool, 1)
	g := time.AfterFunc(1*time.Millisecond, func() { done <- true })
	<-done
	fmt.Println("G afterfunc stop-after-fire pending:", g.Stop())

	// H. time.After returns a timer channel, so it is synchronous too.
	h := time.After(1 * time.Millisecond)
	time.Sleep(25 * time.Millisecond)
	fmt.Println("H after len:", len(h), "cap:", cap(h))
	fmt.Println("H after receive:", !(<-h).IsZero())

	// I. A stopped timer stays stopped: Stop on an already-stopped timer has nothing to revoke.
	i := time.NewTimer(1 * time.Millisecond)
	time.Sleep(25 * time.Millisecond)
	i.Stop()
	fmt.Println("I second stop pending:", i.Stop(), "stale:", tryRecv(i.C))

	// J and K. Stop and Reset landing at the DEADLINE ITSELF rather than comfortably after it.
	// Sections A, B and I all sleep 25 ms first, which puts the call far outside the window where a
	// tick is in flight — so they cannot see an implementation that revokes such a tick correctly
	// but reports it as never having existed. A whole batch of one-shots armed for one instant and
	// stopped right at it puts many calls inside that window at once. Go answers `true` for every
	// one, because a synchronous timer nobody is receiving from never fires at all; any answer
	// below the batch size means a tick was taken away and denied.
	// The batches and the sleep that follows them are armed against ONE absolute deadline rather
	// than each getting its own relative duration. That alignment is the whole trick: giving every
	// timer `d` from the moment it is created would make the caller's own `Sleep(d)` end well after
	// the last timer's, because building the batch takes milliseconds — the caller would wake up
	// long after every tick had already been delivered, and the window would never be sampled.
	// Deriving both from `start` makes the caller wake exactly as the batch fires.
	const batch = 600
	const armAt = 40 * time.Millisecond

	stopRound := func(reset bool) (int, int) {
		start := time.Now()
		timers := make([]*time.Timer, batch)
		for n := range timers {
			timers[n] = time.NewTimer(armAt - time.Since(start))
		}
		time.Sleep(armAt - time.Since(start))

		pending, stale := 0, 0
		for _, t := range timers {
			var was bool
			if reset {
				was = t.Reset(time.Hour)
			} else {
				was = t.Stop()
			}
			if was {
				pending++
			}
		}
		for _, t := range timers {
			if tryRecv(t.C) {
				stale++
			}
		}
		return pending, stale
	}

	jPending, jStale := stopRound(false)
	fmt.Println("J batch stop pending:", jPending, "of", batch, "stale:", jStale)

	kPending, kStale := stopRound(true)
	fmt.Println("K batch reset pending:", kPending, "of", batch, "stale:", kStale)
}
