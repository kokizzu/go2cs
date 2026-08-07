// Regression test: the shapes a converted function's DEFER SCOPE has to get right once the
// scope is a frame rather than an execution-context lambda.
//
// A Go function that defers is emitted with its body inline in try/catch/finally beside a
// `GoFrame` local that holds this call's defer list (docs/Phase4/DESIGN-closure-emission.md §4).
// The shapes below are the ones that construction has to answer for, and that the pre-existing
// defer tests do not cover:
//
//   - MORE defers than the frame's inline slots, so the overflow storage is exercised — and the
//     LIFO drain has to interleave the last inline slot with the first overflowed one correctly.
//   - defers registered in a LOOP: the count is not a syntactic property of the function, so this
//     is what an open-coded (bitmask) registration must never quietly swallow.
//   - a NAMED result mutated by a defer, exited from inside nested control flow. The results are
//     declared before the try and returned after the finally, so every `return` inside leaves
//     through a goto — from a loop body, a switch arm, and an if, not just the top level.
//   - a deferred closure that itself DEFERS. Go scopes that inner defer to the closure, so it runs
//     when the closure returns, BEFORE the next outer defer.
//   - a frame beside the things a ref-struct local coexists with: a capturing lambda, a local
//     function, and a nested closure that defers on its own account.
package main

import "fmt"

// Six defers exceeds the frame's inline slots, so registration spills; the drain must still be
// strictly last-in-first-out across both storage kinds.
func manyDefers() {
	for i := 1; i <= 6; i++ {
		defer fmt.Println("many defer", i)
	}
	fmt.Println("many body")
}

// Registration inside a loop, with the count decided at run time.
func loopDefers(n int) {
	for i := 0; i < n; i++ {
		defer fmt.Println("loop defer", i)
	}
	fmt.Println("loop body", n)
}

// A named result the deferred call mutates, exited from inside a for, a switch, and an if.
func classify(n int) (label string, score int) {
	defer func() {
		score += 100
		label = "<" + label + ">"
	}()

	for i := 0; i < 3; i++ {
		if i == 2 && n == 2 {
			label, score = "from-loop", i
			return label, score
		}
	}

	switch n {
	case 3:
		label, score = "from-switch", 3
		return
	case 4:
		return "from-switch-expr", 4
	}

	if n > 10 {
		label = "big"
		score = n
		return
	}

	label, score = "fallthrough", n
	return
}

// A deferred closure that defers on its own account: Go runs the inner defer when the closure
// returns, so it lands BETWEEN the closure's own body and the next outer defer.
func nestedDeferScopes() {
	defer fmt.Println("outer defer 1")
	defer func() {
		defer fmt.Println("  inner defer")
		fmt.Println("  closure body")
	}()
	defer fmt.Println("outer defer 2")
	fmt.Println("nested body")
}

// A frame beside a capturing lambda, a local function and a nested closure that defers.
func mixedScopes() int {
	total := 0

	defer fmt.Println("mixed defer, total =", total)

	add := func(n int) int { return n * 2 }
	bump := func() { total += add(3) }

	bump()
	bump()

	func() {
		defer func() { total += 1 }()
		total += 10
	}()

	fmt.Println("mixed body, total =", total)
	return total
}

// recover() inside a deferred closure of a function that also has an ordinary defer: the panic
// slot is reached statically, so the closure never needs a handle on the frame.
func guarded(boom bool) (msg string) {
	defer fmt.Println("guarded cleanup")
	defer func() {
		if r := recover(); r != nil {
			msg = fmt.Sprint("recovered ", r)
		}
	}()

	if boom {
		panic("frame boom")
	}

	msg = "no panic"
	return
}

func main() {
	manyDefers()
	loopDefers(3)
	loopDefers(0)

	fmt.Println(classify(2))
	fmt.Println(classify(3))
	fmt.Println(classify(4))
	fmt.Println(classify(42))
	fmt.Println(classify(0))

	nestedDeferScopes()
	fmt.Println("mixed returned", mixedScopes())

	fmt.Println(guarded(false))
	fmt.Println(guarded(true))
}
