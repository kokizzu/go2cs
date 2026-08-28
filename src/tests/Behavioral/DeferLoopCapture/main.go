package main

import (
	"fmt"
	"sort"
	"time"
)

// closer mirrors net.Conn's shape: an interface whose method RETURNS a value. That is what
// forces the converter's synthesized-lambda arm (hasResults), where the defect lives -- the
// method-group arm binds its receiver eagerly and was always correct.
type closer interface {
	Close() error
}

type conn struct{ name string }

func (c *conn) Close() error {
	fmt.Println("close", c.name)
	return nil
}

// asyncConn reports through a channel so the `go` shape can be drained in a deterministic
// order; printing straight from three concurrent goroutines is unordered and would flake.
type asyncConn struct {
	name string
	out  chan string
}

func (a *asyncConn) Close() error {
	a.out <- a.name
	return nil
}

// CONTROL (green before and after the fix): a deferred call's RECEIVER is bound at the defer
// STATEMENT, so reassigning the variable afterwards must not change what is closed. The
// capture-snapshot machinery already hoists a simple identifier receiver.
func receiverReassignedNoLoop() {
	fmt.Println("-- control: receiver bound at defer-statement time --")
	x := &conn{name: "first"}
	defer x.Close()
	x = &conn{name: "second"}
	_ = x
}

// RED SHAPE 1 -- net's TestConcurrentSetDeadline: a deferred method call indexed by a
// 3-clause loop variable. Without per-iteration semantics for the SYNTHESIZED lambda the
// index is read when the defer fires, i.e. at the loop's final value: index out of range.
func deferIndexedReceiverInLoop() {
	fmt.Println("-- red 1: defer c[i].Close() --")
	var c [3]closer
	for i := 0; i < 3; i++ {
		c[i] = &conn{name: fmt.Sprint("conn", i)}
		defer c[i].Close()
	}
}

// RED SHAPE 2 -- the same defect at the OTHER synthesized-lambda site. Drained with a
// timeout so the guard terminates in the broken state instead of hanging on a goroutine
// that died indexing out of range.
func goIndexedReceiverInLoop() {
	fmt.Println("-- red 2: go c[i].Close() --")
	out := make(chan string, 3)
	var c [3]closer
	for i := 0; i < 3; i++ {
		c[i] = &asyncConn{name: fmt.Sprint("g", i), out: out}
		go c[i].Close()
	}
	got := make([]string, 0, 3)
	for j := 0; j < 3; j++ {
		select {
		case s := <-out:
			got = append(got, s)
		case <-time.After(5 * time.Second):
			got = append(got, "TIMEOUT")
		}
	}
	sort.Strings(got)
	for _, s := range got {
		fmt.Println("closed", s)
	}
}

// CONTROL: a SOURCE-level func literal capturing the loop variable already takes Go 1.22
// per-iteration semantics. Must stay green -- it is what proves the fix did not change the
// path that was already correct.
func closureCapture3Clause() {
	fmt.Println("-- control: closure capture (3-clause) --")
	for i := 0; i < 3; i++ {
		defer func() { fmt.Println("closure", i) }()
	}
}

// CONTROL: range-loop equivalent.
func closureCaptureRange() {
	fmt.Println("-- control: closure capture (range) --")
	for _, v := range []string{"x", "y", "z"} {
		defer func() { fmt.Println("range", v) }()
	}
}

// CONTROL: deferred ARGUMENTS are evaluated at the defer statement. The fix deliberately does
// NOT mint a per-iteration copy for a variable appearing only here, so this shape's emission
// must be unchanged.
func plainArgs() {
	fmt.Println("-- control: plain args (3-clause) --")
	for i := 0; i < 3; i++ {
		defer fmt.Println("arg", i)
	}
}

func main() {
	receiverReassignedNoLoop()
	goIndexedReceiverInLoop()
	deferIndexedReceiverInLoop()
	closureCapture3Clause()
	closureCaptureRange()
	plainArgs()
}
