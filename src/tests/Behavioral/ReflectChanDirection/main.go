// ReflectChanDirection guards channel DIRECTION as descriptor cargo, and the reflect.Value.Recv /
// Send bridge that cannot land without it.
//
// A Go channel's direction is part of its TYPE, and it is the one part the managed emission cannot
// hold: chan T, chan<- T and <-chan T all render as golib's channel<T>. The direction is therefore
// carried on the VALUE, the way a fixed-size array's length is, and read back as descriptor cargo
// (see ReflectFuncArrayParamDims for the same rule on the array side).
//
// The two halves are ONE change on purpose. Bridging Recv without the direction converts a fast,
// attributable error -- Go's "range over send-only channel" -- into an unbounded hang, because the
// guard that is supposed to refuse the range never fires and the receive blocks on a channel nobody
// will send to. walkRangeShape below reproduces exactly that shape, bounded by a timeout so a
// regression prints a named line instead of wedging the suite.
package main

import (
	"fmt"
	"reflect"
	"strings"
	"time"
)

type holder struct {
	send chan<- string
	recv <-chan int
	both chan bool
	Sent chan<- byte
}

// A DEFINED channel type is deliberately NOT stamped -- its managed form is a generated wrapper
// struct rather than channel<T>, the same carve-out a defined ARRAY type has. This one is
// bidirectional, so it is a control: the wrapper path must keep answering exactly as before.
type sink chan string

func describe(label string, t reflect.Type) {
	fmt.Printf("%s: %v | dir=%v | kind=%v\n", label, t, t.ChanDir(), t.Kind())
}

// walkRangeShape is text/template's walkRange channel arm in miniature: the direction guard, then
// the receive loop. It is the shape the whole arc exists for.
func walkRangeShape(v reflect.Value) string {
	if v.Kind() != reflect.Chan {
		return "not a channel"
	}

	if v.Type().ChanDir() == reflect.SendDir {
		return "range over send-only channel"
	}

	var out []string

	for {
		elem, ok := v.Recv()
		if !ok {
			break
		}
		out = append(out, fmt.Sprint(elem))
	}

	return "[" + strings.Join(out, " ") + "]"
}

// count is text/template's own test helper: a BIDIRECTIONAL chan string the range walks. The guard
// correctly does not fire here, so this row is what proves the recv bridge itself works -- it is
// the half no direction answer can satisfy.
func count(n int) chan string {
	ch := make(chan string)
	go func() {
		for i := 0; i < n; i++ {
			ch <- fmt.Sprint(i)
		}
		close(ch)
	}()
	return ch
}

func recovered(fn func()) (msg string) {
	defer func() {
		if r := recover(); r != nil {
			msg = fmt.Sprint(r)
		}
	}()
	fn()
	return "no panic"
}

func main() {
	// ---- birth site 1: make ----
	send := make(chan<- int)
	recv := make(<-chan int, 2)
	both := make(chan int)

	describe("make chan<-", reflect.TypeOf(send))
	describe("make <-chan", reflect.TypeOf(recv))
	describe("make chan", reflect.TypeOf(both))

	// ---- birth site 2: new(T), read through the POINTER hop ----
	ps := new(chan<- string)
	pr := new(<-chan int)

	fmt.Printf("new chan<-: %v -> %v | dir=%v\n", reflect.TypeOf(ps), reflect.TypeOf(ps).Elem(), reflect.TypeOf(ps).Elem().ChanDir())
	fmt.Printf("new <-chan: %v -> %v | dir=%v\n", reflect.TypeOf(pr), reflect.TypeOf(pr).Elem(), reflect.TypeOf(pr).Elem().ChanDir())
	// The value side of the same hop -- reflectlite's TestSetValue row reads exactly this.
	fmt.Printf("elem of new: %v | %v\n", reflect.ValueOf(ps).Elem().Type(), reflect.ValueOf(pr).Elem().Type())

	// ---- birth site 3: a struct FIELD's zero, which has no value to measure ----
	ht := reflect.TypeOf(holder{})
	for i := 0; i < ht.NumField(); i++ {
		f := ht.Field(i)
		fmt.Printf("field %s: %v | dir=%v\n", f.Name, f.Type, f.Type.ChanDir())
	}

	// ---- assignability: Go's two chan rules, which only differ once directions are real ----
	ci := reflect.TypeOf(new(chan int)).Elem()
	ri := reflect.TypeOf(new(<-chan int)).Elem()
	si := reflect.TypeOf(new(chan<- int)).Elem()
	fmt.Println("chan->recv:", ci.AssignableTo(ri), "recv->chan:", ri.AssignableTo(ci), "chan->send:", ci.AssignableTo(si), "send->recv:", si.AssignableTo(ri))
	fmt.Println("identical:", ci == reflect.TypeOf(both), "distinct:", ci != ri, ri != si)

	// ---- the DEFINED-type control: a wrapper is unaffected by any of this ----
	var sk sink = make(sink)
	describe("defined chan", reflect.TypeOf(sk))

	// ---- the recv bridge, over a bidirectional channel ----
	fmt.Println("range count(5):", walkRangeShape(reflect.ValueOf(count(5))))

	// ---- the direction guard, in the shape that used to hang ----
	done := make(chan string, 1)
	go func() { done <- walkRangeShape(reflect.ValueOf(make(chan<- int))) }()
	select {
	case got := <-done:
		fmt.Println("range send-only:", got)
	case <-time.After(5 * time.Second):
		fmt.Println("range send-only: HUNG -- the direction guard did not fire before Recv")
	}

	// ---- Send, and the mirrored guard ----
	buffered := make(chan int, 2)
	bv := reflect.ValueOf(buffered)
	bv.Send(reflect.ValueOf(41))
	bv.Send(reflect.ValueOf(42))
	first, ok1 := bv.Recv()
	fmt.Println("send/recv:", first.Int(), ok1, "len:", bv.Len(), "cap:", bv.Cap())

	// TryRecv/TrySend are the non-blocking pair through the same two bridged functions.
	drained, ok2 := bv.TryRecv()
	empty, ok3 := bv.TryRecv()
	fmt.Println("tryrecv:", drained.Int(), ok2, "| empty valid:", empty.IsValid(), ok3)
	fmt.Println("trysend:", bv.TrySend(reflect.ValueOf(7)))

	// A CLOSED channel yields the element's zero with ok false, after draining what it holds.
	closing := make(chan string, 1)
	closing <- "last"
	close(closing)
	cv := reflect.ValueOf(closing)
	drainedVal, okDrain := cv.Recv()
	zeroVal, okZero := cv.Recv()
	fmt.Printf("closed: %q %v then %q %v\n", drainedVal.String(), okDrain, zeroVal.String(), okZero)

	// ---- both guards, by their Go messages ----
	fmt.Println("recv on send-only:", recovered(func() { reflect.ValueOf(send).Recv() }))
	fmt.Println("send on recv-only:", recovered(func() { reflect.ValueOf(recv).Send(reflect.ValueOf(1)) }))
}
