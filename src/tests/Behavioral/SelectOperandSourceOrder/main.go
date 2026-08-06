// Select case operands evaluate exactly ONCE, in SOURCE ORDER, on entering the select — a
// receive case's channel operand, and a send case's channel operand AND its right-hand-side
// value expression. Each helper below prints its tag as it is evaluated, and each tag carries
// its fixed SOURCE position, so a correct run prints the tags in ascending order and any
// misordering shows up as a visible permutation. Emitting a send case's registration inline in
// the select(...)/trySelect(...) argument list put every send operand AFTER the hoisted receive
// operands, so a select whose first case was a send printed `3:recv-chan 1:send-chan
// 2:send-val` where Go prints `1:send-chan 2:send-val 3:recv-chan`.
//
// Every select here is deterministic by construction — exactly one case can ever be ready (a
// FULL buffered channel can never send; an UNBUFFERED channel with no peer can never receive)
// — so the runtime's uniform-random commit among ready cases never affects the output.
package main

import "fmt"

// note prints an operand expression's source-position tag at the moment it is evaluated.
func note(tag string) {
	fmt.Printf("%s ", tag)
}

func chanInt(ch chan int, tag string) chan int {
	note(tag)
	return ch
}

func chanByte(ch chan byte, tag string) chan byte {
	note(tag)
	return ch
}

func chanAny(ch chan any, tag string) chan any {
	note(tag)
	return ch
}

func valInt(v int, tag string) int {
	note(tag)
	return v
}

func main() {
	// S1 — blocking select whose SEND case is textually FIRST and can NEVER be ready (its
	// channel's buffer is full with no receiver); the receive case is ready and wins. The
	// pre-fix emission printed the hoisted receive operand first.
	full := make(chan int, 1)
	full <- 1
	ready := make(chan int, 1)
	ready <- 42
	got := 0
	select {
	case chanInt(full, "1:send-chan") <- valInt(9, "2:send-val"):
		got = -1
	case got = <-chanInt(ready, "3:recv-chan"):
	}
	fmt.Printf("| S1 got:%d\n", got)

	// S2 — default form (trySelect) interleaving send / receive / send with NOTHING ready, so
	// every operand of every case is evaluated and the default clause runs. Covers an UNTYPED
	// constant 200 sent into a `chan byte` (the value keeps its argument position, so the
	// element type still narrows it — a separate `var t = 200` operand temp would be an int and
	// fail to convert) and a value boxed into a `chan any`.
	fullBytes := make(chan byte, 1)
	fullBytes <- 7
	noPeer := make(chan int)
	fullAny := make(chan any, 1)
	fullAny <- "x"
	hit := 0
	select {
	case chanByte(fullBytes, "1:byte-chan") <- 200:
		hit = 1
	case <-chanInt(noPeer, "2:recv-chan"):
		hit = 2
	case chanAny(fullAny, "3:any-chan") <- valInt(3, "4:any-val"):
		hit = 3
	default:
		hit = 0
	}
	fmt.Printf("| S2 hit:%d\n", hit)

	// S3 — the SEND case wins (its channel has free capacity) and the HOISTED value expression
	// is the one actually delivered: hoisting the registration call must move no send and must
	// not lose or duplicate the value.
	room := make(chan int, 1)
	blocked := make(chan int)
	which := 0
	select {
	case chanInt(room, "1:send-chan") <- valInt(7, "2:send-val"):
		which = 1
	case <-chanInt(blocked, "3:recv-chan"):
		which = 2
	}
	delivered := <-room
	fmt.Printf("| S3 which:%d delivered:%d\n", which, delivered)

	// S4 — receive case FIRST: the direction that was already correct before the fix, kept as a
	// regression anchor so a future change cannot repair the send order by breaking this one.
	src := make(chan int, 1)
	src <- 5
	sink := make(chan int, 1)
	sink <- 1
	v := 0
	select {
	case v = <-chanInt(src, "1:recv-chan"):
	case chanInt(sink, "2:send-chan") <- valInt(8, "3:send-val"):
		v = -1
	}
	fmt.Printf("| S4 v:%d\n", v)
}
