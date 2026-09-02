// ReflectChanNarrowing guards the LIVE-COPY narrowing position of channel-direction cargo -- the
// one position chanDirectionCargo.go's exclusion deferred until reflect measured a consumer.
//
// The construction-shaped positions (make / zero / new / nil-cast) stamp a channel's direction at
// BIRTH, and ReflectChanDirection guards those. A NARROWING has no construction to hook: Go makes a
// value of a new TYPE out of a value that already exists (`var r <-chan int = ch`), so without a
// re-stamp the descriptor reports the value as bidirectional and every direction-dependent answer
// is wrong for it. reflect's own TestMakeFuncInvalidReturnAssignments is the measured consumer --
// it narrows make(chan int) to <-chan int and returns it into a `chan int` result, which Go REFUSES
// because a directional channel cannot widen; unstamped, both sides look like `chan int` and the
// assignment is wrongly admitted.
//
// The four narrowing positions Go allows are var-init, assignment, call argument and return, and
// each is exercised below with its direction read back through the descriptor. The NON-narrowing
// rows are controls: a bidirectional target and an already-directional source must be unchanged,
// because a re-stamp there would be churn at best and a wrong direction at worst.
package main

import (
	"fmt"
	"reflect"
)

func dirOf(v any) reflect.ChanDir {
	return reflect.TypeOf(v).ChanDir()
}

// position 3: call ARGUMENT -- a bidirectional value passed to a directional parameter.
func takesRecv(c <-chan int) reflect.ChanDir {
	return dirOf(c)
}

func takesSend(c chan<- int) reflect.ChanDir {
	return dirOf(c)
}

// position 4: RETURN -- a bidirectional value returned as a directional result.
func returnsRecv() <-chan int {
	return make(chan int)
}

func returnsSend() chan<- int {
	return make(chan int)
}

// control: a bidirectional result stays bidirectional.
func returnsBoth() chan int {
	return make(chan int)
}

func main() {
	// ---- position 1: VAR-INIT narrowing ----
	var vr <-chan int = make(chan int)
	var vs chan<- int = make(chan int)
	fmt.Println("var-init recv:", dirOf(vr), "| send:", dirOf(vs))

	// ---- position 2: ASSIGNMENT narrowing ----
	var ar <-chan int
	var as chan<- int
	bidi := make(chan int)
	ar = bidi
	as = bidi
	fmt.Println("assign recv:", dirOf(ar), "| send:", dirOf(as))

	// ---- position 3: call ARGUMENT narrowing ----
	fmt.Println("arg recv:", takesRecv(make(chan int)), "| send:", takesSend(make(chan int)))

	// ---- position 4: RETURN narrowing ----
	fmt.Println("return recv:", dirOf(returnsRecv()), "| send:", dirOf(returnsSend()))

	// ---- controls: nothing here is a narrowing, so nothing may change ----
	var cb chan int = make(chan int)
	fmt.Println("control bidi var:", dirOf(cb), "| bidi return:", dirOf(returnsBoth()))

	// an already-directional source re-assigned to the same direction is not a narrowing
	var again <-chan int = vr
	fmt.Println("control recv<-recv:", dirOf(again))

	// ---- identity survives the re-stamp: the narrowed value is the SAME channel ----
	// Go compares channels by identity, so a narrowing that copied the core would break this.
	src := make(chan int, 1)
	var narrowed <-chan int = src
	src <- 99
	got, ok := reflect.ValueOf(narrowed).Recv()
	fmt.Println("identity: same channel ->", got.Int(), ok, "| len now:", reflect.ValueOf(src).Len())

	// ---- the assignability rule the whole cut exists for ----
	recvT := reflect.TypeOf(vr)
	bidiT := reflect.TypeOf(cb)
	fmt.Println("recv->chan assignable:", recvT.AssignableTo(bidiT), "| chan->recv assignable:", bidiT.AssignableTo(recvT))
}
