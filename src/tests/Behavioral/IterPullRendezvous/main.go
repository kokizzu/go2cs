// Guards the managed coro rendezvous that iter.Pull/Pull2 rest on — golib's go.golib.Coro, bound to
// iter's newcoro/coroswitch by core/iter/iter_impl.cs.
//
// Go implements the control transfer by switching stacks (runtime/coro.go); the CLR cannot, so the
// sequence function runs on its own thread and the two sides hand a turn back and forth. That is a
// real reimplementation of a language primitive, and the properties below are the ones a future
// change to the goroutine executor, the defer/panic machinery or the rendezvous itself could break
// WITHOUT breaking anything a compile catches:
//
//   - values arrive in order, and exhaustion latches (next keeps answering the zero value, false)
//   - an early stop unwinds the sequence and latches the same way
//   - stop is idempotent, including after exhaustion
//   - a panic INSIDE the sequence surfaces at whichever side resumed it — next for a panic on
//     entry, stop for a panic in the sequence's cleanup path — with the panic VALUE preserved
//   - the yield/next protocol's own violation panics carry Go's exact text
//   - runtime.Goexit inside the sequence propagates ACROSS the boundary and Goexits the puller
//
// iter's own converted test suite (28/28) is the primary proof and covers goroutine ACCOUNTING too;
// this guard deliberately prints no goroutine counts, because a count is only stable in Go under the
// stabilization loop that suite uses, and a flaky stdout comparison here would fire across the whole
// corpus gate rather than in one package.
package main

import (
	"fmt"
	"iter"
	"runtime"
)

func count(n int) iter.Seq[int] {
	return func(yield func(int) bool) {
		for i := 0; i < n; i++ {
			if !yield(i) {
				return
			}
		}
	}
}

func squares(n int) iter.Seq2[int, int64] {
	return func(yield func(int, int64) bool) {
		for i := 0; i < n; i++ {
			if !yield(i, int64(i)*int64(i)) {
				return
			}
		}
	}
}

// catch runs f and reports whatever it panicked with, so a panic that must cross the rendezvous is
// observable as ordinary output rather than as a dead process.
func catch(label string, f func()) {
	defer func() {
		if r := recover(); r != nil {
			fmt.Printf("%s panicked: %v\n", label, r)
			return
		}
		fmt.Printf("%s did not panic\n", label)
	}()

	f()
}

// goexits runs f on its own goroutine and reports whether f ended it via runtime.Goexit — recover
// sees nil (a Goexit is not a panic) and the body never reached its last statement.
func goexits(f func()) bool {
	done := make(chan bool)

	go func() {
		clean := false
		defer func() { done <- recover() == nil && !clean }()
		f()
		clean = true
	}()

	return <-done
}

func panicSeq() iter.Seq[int] {
	return func(yield func(int) bool) {
		panic("boom")
	}
}

func panicCleanupSeq() iter.Seq[int] {
	return func(yield func(int) bool) {
		for {
			if !yield(55) {
				panic("cleanup boom")
			}
		}
	}
}

func goexitSeq() iter.Seq[int] {
	return func(yield func(int) bool) {
		runtime.Goexit()
	}
}

// doubleNext calls next from INSIDE the sequence, which the protocol forbids: next has already set
// yieldNext, so the second call must panic before it ever reaches a switch.
var nextSlot func() (int, bool)

func doubleNext() iter.Seq[int] {
	return func(_ func(int) bool) {
		defer func() {
			if r := recover(); r != nil {
				fmt.Printf("double next panicked: %v\n", r)
			}
		}()

		nextSlot()
	}
}

func main() {
	// Drain to exhaustion, then keep pulling: the sequence is over and stays over.
	next, stop := iter.Pull(count(3))

	for {
		v, ok := next()
		fmt.Println("pull:", v, ok)

		if !ok {
			break
		}
	}

	v, ok := next()
	fmt.Println("pull after exhaustion:", v, ok)

	stop()
	stop()
	fmt.Println("stop after exhaustion is a no-op")

	// The same shape through Pull2, whose yield carries a pair.
	next2, stop2 := iter.Pull2(squares(3))

	for {
		k, v2, ok2 := next2()
		fmt.Println("pull2:", k, v2, ok2)

		if !ok2 {
			break
		}
	}

	stop2()

	// Stop BEFORE exhaustion: the sequence is unwound early and next latches.
	next3, stop3 := iter.Pull(count(100))
	a, aok := next3()
	b, bok := next3()
	fmt.Println("early pull:", a, aok, b, bok)

	stop3()

	c, cok := next3()
	fmt.Println("pull after early stop:", c, cok)

	// A panic on the sequence's first step surfaces at the next that resumed it, and the iterator
	// is invalidated rather than left mid-flight.
	nextP, stopP := iter.Pull(panicSeq())
	catch("first next", func() { nextP() })

	pv, pok := nextP()
	fmt.Println("pull after panic:", pv, pok)

	catch("stop after panic", func() { stopP() })

	// A panic in the sequence's CLEANUP path surfaces at stop instead, because stop is what resumes
	// the sequence to unwind it.
	nextC, stopC := iter.Pull(panicCleanupSeq())
	cv, cvok := nextC()
	fmt.Println("cleanup pull:", cv, cvok)

	catch("stop into cleanup", func() { stopC() })

	// The protocol's own violation, and its exact text.
	nextD, _ := iter.Pull(doubleNext())
	nextSlot = nextD
	nextD()

	// runtime.Goexit inside the sequence must cross the boundary and Goexit the PULLER.
	var goexitNext func() (int, bool)

	fmt.Println("goexit crossed:", goexits(func() {
		n, _ := iter.Pull(goexitSeq())
		goexitNext = n
		n()
	}))

	gv, gok := goexitNext()
	fmt.Println("pull after goexit:", gv, gok)

	// A sequence that was never started is stopped without ever running its body.
	nextI, stopI := iter.Pull(panicSeq())
	stopI()

	iv, iok := nextI()
	fmt.Println("pull after immediate stop:", iv, iok)
}
