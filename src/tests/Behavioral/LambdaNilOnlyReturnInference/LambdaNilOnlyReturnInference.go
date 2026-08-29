// A function literal in ASSIGNMENT position whose every return arm is the untyped `nil` must
// state its return type explicitly, or C# has nothing to infer the delegate from (CS8917).
//
// Each `return nil` renders as a bare `default!`, which carries no natural type at all — so no
// arm contributes one, and `var g = (slice<Value> @in) => { …; return default!; };` is
// uninferable. The remedy is the explicit prefix the converter already renders elsewhere:
// `var g = slice<Value> (slice<Value> @in) => …`.
//
// That rule EXISTED before this guard, and was correct — but it lived inside the arm handling a
// non-empty INTERFACE result (its driver was net_test's
// `client := func(*TCPConn) error { …; return nil }`), so a result of any other nilable kind
// never reached it. This file covers the kinds that were missed and keeps the interface arm as a
// control.
//
// Guards reflect's TestCallGC (all_test.go:6870), `g := func(in []Value) []Value { …; return nil }`,
// which is then handed to `MakeFunc` — so the natural delegate type has to be right for the call
// that consumes it, not merely for the declaration. A6/A7 assert exactly that.
//
// The lambda one line above it in the same Go function — `f := func(a, b, c, d, e string) {}` —
// compiled fine throughout, because a no-result literal is an Action and needs no inference. It
// is kept below as B2, so the discriminator is visible rather than implied.
package main

import "fmt"

type Value struct{ N int }

// Each literal below is handed to one of these, which is what keeps it a VALUE. That matters:
// a literal only ever CALLED is emitted as a C# local FUNCTION with an explicit return type and
// never needed inference at all, so passing it is what actually exercises the defect — and it
// is also reflect's own shape, where `g` is handed to `MakeFunc`.
func apply(f func([]Value) []Value) []Value { return f(nil) }

func applyPtr(f func() *Value) *Value { return f() }

func applyMap(f func(string) map[string]int) map[string]int { return f("x") }

func applyChan(f func() chan int) chan int { return f() }

func applyFunc(f func() func(int) int) func(int) int { return f() }

func applyErr(f func(int) error) error { return f(1) }

func main() {
	// ---- Nilable result kinds whose all-nil arms were missed ----

	g := func(in []Value) []Value {
		return nil
	}
	fmt.Println("A1 slice result:", g(nil) == nil, len(g(nil)))

	m := func(k string) map[string]int {
		return nil
	}
	fmt.Println("A2 map result:", applyMap(m) == nil, len(m("x")))

	p := func() *Value {
		return nil
	}
	fmt.Println("A3 pointer result:", p() == nil)

	c := func() chan int {
		return nil
	}
	fmt.Println("A4 chan result:", applyChan(c) == nil)

	fn := func() func(int) int {
		return nil
	}
	fmt.Println("A5 func result:", applyFunc(fn) == nil)

	// The reflect shape: the literal is CONSUMED by a call taking that func type, so the
	// inferred delegate must match the parameter, not just satisfy the declaration.
	fmt.Println("A6 slice-result lambda passed to a consumer:", apply(g) == nil)
	fmt.Println("A7 pointer-result lambda passed to a consumer:", applyPtr(p) == nil)

	// ---- Controls ----

	// A non-empty INTERFACE result — the arm the rule already lived in.
	e := func(x int) error {
		return nil
	}
	fmt.Println("B1 error result:", applyErr(e) == nil)

	// No result at all: an Action, nothing to infer.
	v := func(a, b string) {
		_ = a
		_ = b
	}
	v("x", "y")
	fmt.Println("B2 void lambda ran")

	// A slice result with a REAL value: natural inference must still work.
	r := func() []Value {
		return []Value{{N: 7}}
	}
	fmt.Println("B3 slice result non-nil:", r()[0].N)

	// MIXED arms — one nil, one typed — so allArmsUntypedNil is false.
	mix := func(b bool) []Value {
		if b {
			return nil
		}
		return []Value{{N: 3}}
	}
	fmt.Println("B4 mixed arms:", mix(true) == nil, mix(false)[0].N)

	// A basic result: unaffected by the nilable rule.
	num := func(x int) int {
		return x * 2
	}
	fmt.Println("B5 int result:", num(21))
}
