// The reflect `Value` singles (increment E3 of the reflect tail): one root per commit, each row
// pinned against `go run` beside the reflect suite's own verdict.
//
//   1. SetCap -- the fifth raw-slice-header member, hand-owned beside SetLen: Go's bound check
//      (len <= n <= cap) and its panic text, and the three-index window s[:len:n] written back
//      through the addressable slot (TestSetLenCap).
package main

import (
	"fmt"
	"reflect"
	"strings"
)

// expectPanic runs f and reports whether it panicked and whether the panic text mentions want --
// TestSetLenCap's shouldPanic, printed rather than asserted.
func expectPanic(label, want string, f func()) {
	defer func() {
		r := recover()
		msg := fmt.Sprint(r)
		fmt.Printf("%-16s panicked: %v  mentions %q: %v  text: %s\n", label, r != nil, want, strings.Contains(msg, want), msg)
	}()
	f()
}

func main() {
	// --- root 1: SetLen / SetCap ---
	xs := []int{1, 2, 3, 4, 5, 6, 7, 8}
	xa := [8]int{10, 20, 30, 40, 50, 60, 70, 80}
	vs := reflect.ValueOf(&xs).Elem()
	expectPanic("SetLen(10)", "SetLen", func() { vs.SetLen(10) })
	expectPanic("SetCap(10)", "SetCap", func() { vs.SetCap(10) })
	expectPanic("SetLen(-1)", "SetLen", func() { vs.SetLen(-1) })
	expectPanic("SetCap(-1)", "SetCap", func() { vs.SetCap(-1) })
	expectPanic("SetCap(6)<len", "SetCap", func() { vs.SetCap(6) }) // smaller than len
	vs.SetLen(5)
	fmt.Println("after SetLen(5): len, cap =", len(xs), cap(xs))
	vs.SetCap(6)
	fmt.Println("after SetCap(6): len, cap =", len(xs), cap(xs))
	vs.SetCap(5)
	fmt.Println("after SetCap(5): len, cap =", len(xs), cap(xs), "contents", xs)
	expectPanic("SetCap(4)<len", "SetCap", func() { vs.SetCap(4) })
	expectPanic("SetLen(6)>cap", "SetLen", func() { vs.SetLen(6) })
	va := reflect.ValueOf(&xa).Elem()
	expectPanic("array SetLen", "SetLen", func() { va.SetLen(8) })
	expectPanic("array SetCap", "SetCap", func() { va.SetCap(8) })
	// the re-capped slice still aliases the original backing: a write through it lands in the array
	backing := xs[:cap(xs)]
	backing[0] = 99
	fmt.Println("write through the re-capped window seen by the original:", xs[0] == 99)
}
