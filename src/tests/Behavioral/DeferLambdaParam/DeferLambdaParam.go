package main

import "fmt"

func main() {
	count := 1
	defer func(cnt int) {
		fmt.Println("Deferred count (closure):", cnt)
	}(count)

	// A VARIADIC deferred literal. `params ꓸꓸꓸT` converts to no Action<T1, T2>, so the arity-N
	// `defer` overload could not infer its type arguments (CS0411) -- html/template's
	// examplefiles_test.go:90 is the only site in the Go 1.23 tree and it walled 243 verdicts.
	// The arguments must still be snapshotted at DEFER time, so mutating them afterwards must not
	// change what the thunk prints.
	a, b := "one", "two"
	defer func(parts ...string) {
		fmt.Println("Deferred variadic:", len(parts), parts)
	}(a, b)
	a, b = "CHANGED", "ALSO-CHANGED"

	// Every arity around it: no fixed parameter and one argument, no fixed parameter and none at
	// all (an empty variadic still takes the temp-parameter form only when it has arguments), a
	// fixed parameter ahead of the tail, and a spread of a whole slice.
	defer func(parts ...int) {
		fmt.Println("Deferred variadic (1 arg):", parts)
	}(7)
	defer func(label string, parts ...int) {
		fmt.Println("Deferred variadic (fixed + tail):", label, parts)
	}("L", 1, 2, 3)
	// (A SPREAD argument -- `}(nums...)` -- is deliberately absent. It emits `nums.ꓸꓸꓸ`, a
	// Span<T>, as the type argument of `defer<T>`, which C# refuses for a ref struct (CS9244).
	// That wall is INDEPENDENT of the variadic-literal one and predates it: `defer f(nums...)` on
	// a NAMED variadic f emits the identical `defer(ᴛ1 => f(ᴛ1), nums.ꓸꓸꓸ, ref ᒐ)` and fails the
	// same way, with no func literal anywhere. Recorded on the board rather than widened into
	// here -- closing it means passing the SLICE and spreading inside the thunk, at every
	// variadic deferred call in the corpus.)
	// A variadic literal deferred with NO arguments at all: this is the arity-0 rung, where the
	// registration used to hand golib a bare method group -- which a `params` lambda has none of.
	// (`parts == nil` is NOT asserted here: Go passes a NIL slice for an empty variadic and the
	// conversion passes an empty non-nil one, but that is a corpus-wide argument-construction
	// difference visible from a plain `f()` on a named variadic f, with no defer and no literal
	// anywhere -- recorded on the board, not this change's to fix.)
	defer func(parts ...int) {
		fmt.Println("Deferred variadic (no args):", len(parts))
	}()

	// A variadic literal that RETURNS a value: the deferred call discards it, so this exercises
	// the Funcꓸꓸꓸ half of the delegate family rather than the Actionꓸꓸꓸ half.
	defer func(parts ...string) int {
		fmt.Println("Deferred variadic (with result):", parts)
		return len(parts)
	}("r1", "r2")

	// The IMMEDIATELY-INVOKED variadic literal, which had the same missing conversion one door
	// over (CS0149 -- a C# lambda cannot be invoked directly).
	fmt.Println("IIFE variadic:", func(parts ...int) int {
		total := 0
		for _, p := range parts {
			total += p
		}
		return total
	}(1, 2, 3))

	count = 10
	fmt.Println("Count before defer:", count)
}
