// AtomicValueTypedNilFunc guards the eface TYPE WORD at atomic.Value's consistency check.
//
// Go's `Value.Store` compares the eface type words of the stored value and the incoming one. A nil
// func inside an interface is (type=func(T), value=nil) -- a NON-nil type word -- so storing a real
// func(int) and then a typed-nil func(int) is CONSISTENT and Go accepts it. The managed nil func is
// carried beside the object rather than in it, so an observer that asks the runtime class instead
// of the bridge is wrong in BOTH directions: it sees carrier-vs-live as unequal (a false
// inequality, the row internal/poll hits), and it sees nils of two DIFFERENT func types as equal
// (a false equality, which nothing panics on and no test covered before this one).
package main

import (
	"fmt"
	"sync/atomic"
)

func recovered(label string, fn func()) {
	defer func() {
		if r := recover(); r != nil {
			fmt.Printf("%s: PANIC %v\n", label, r)
			return
		}
		fmt.Printf("%s: ok\n", label)
	}()
	fn()
}

func main() {
	// ---- direction 1: carrier vs LIVE of the same func type -- Go ACCEPTS ----
	var v1 atomic.Value
	v1.Store(func(fd int) { _ = fd })
	recovered("live then typed-nil, same type", func() { v1.Store((func(int))(nil)) })

	// and the reverse order, which stores the carrier first
	var v2 atomic.Value
	v2.Store((func(int))(nil))
	recovered("typed-nil then live, same type", func() { v2.Store(func(fd int) { _ = fd }) })

	// ---- direction 2: carrier vs carrier of DIFFERENT func types -- Go REFUSES ----
	var v3 atomic.Value
	v3.Store((func(int))(nil))
	recovered("typed-nil then typed-nil, DIFFERENT type", func() { v3.Store((func(string))(nil)) })

	// ---- controls: no carrier involved ----
	var v4 atomic.Value
	v4.Store(func(fd int) { _ = fd })
	recovered("live then live, same type", func() { v4.Store(func(fd int) { _ = fd }) })

	var v5 atomic.Value
	v5.Store(1)
	recovered("int then string, different type", func() { v5.Store("x") })

	// the stored value survives and is still callable through the interface
	var v6 atomic.Value
	v6.Store(func(fd int) int { return fd * 2 })
	if f, ok := v6.Load().(func(int) int); ok {
		fmt.Println("loaded and called:", f(21))
	}
}
