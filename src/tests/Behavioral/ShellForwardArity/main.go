// Guards ARGUMENT FORWARDING through golib's reflective object shell at every arity.
//
// A duck-typed assert that cannot bind a delegate shell -- a VALUE-typed dynamic value, which is
// the tier Native AOT always falls back to -- forwards each interface call through
// GoShellBinding.Invoke, which hands the receiver plus the call arguments to a cached
// MethodInvoker. That forwarder dispatches on argument count: the BCL's fixed-arity overloads take
// the receiver plus up to three Go parameters, and anything wider goes through a Span built over a
// freshly allocated array. So the arity boundary is a real seam with two sides, and an off-by-one
// on either side of it either drops an argument or mispositions the receiver.
//
// The interface below therefore spans arities 0 through 5 -- 0..3 exercise one fixed-arity arm
// each, 4 and 5 exercise the Span fallback -- and every method mixes its parameters into the result
// so a dropped, duplicated or reordered argument changes the printed output rather than passing
// silently. Both an int-returning and a string-returning shape are covered, since the forwarder
// boxes the return.
//
// The dynamic value is held in an `any` slice so the assert has no statically known concrete type
// and the converter can record no adapter: resolution is the runtime shell tier by construction,
// the same trick src/tests/Performance/PerfIfaceShell uses.
package main

import "fmt"

type calc int

func (c calc) Zero() string { return fmt.Sprintf("z(%d)", int(c)) }

func (c calc) One(a int) string { return fmt.Sprintf("o(%d,%d)", int(c), a) }

func (c calc) Two(a, b int) string { return fmt.Sprintf("t(%d,%d,%d)", int(c), a, b) }

func (c calc) Three(a, b, d int) string { return fmt.Sprintf("h(%d,%d,%d,%d)", int(c), a, b, d) }

func (c calc) Four(a, b, d, e int) string {
	return fmt.Sprintf("f(%d,%d,%d,%d,%d)", int(c), a, b, d, e)
}

func (c calc) Five(a, b, d, e, f int) string {
	return fmt.Sprintf("v(%d,%d,%d,%d,%d,%d)", int(c), a, b, d, e, f)
}

// Sum returns an int so the boxed-return path is covered at a wide arity too, and weights each
// parameter distinctly so any permutation of the forwarded arguments changes the total.
func (c calc) Sum(a, b, d, e, f int) int {
	return int(c) + 10*a + 100*b + 1000*d + 10000*e + 100000*f
}

// Mixed takes non-int parameters as well, so the forwarder is exercised with arguments that box
// differently (a string reference and a bool) rather than six identically-shaped ints.
func (c calc) Mixed(name string, flag bool, n int) string {
	return fmt.Sprintf("m(%d,%s,%t,%d)", int(c), name, flag, n)
}

func main() {
	values := []any{calc(7)}

	v, ok := values[0].(interface {
		Zero() string
		One(a int) string
		Two(a, b int) string
		Three(a, b, d int) string
		Four(a, b, d, e int) string
		Five(a, b, d, e, f int) string
		Sum(a, b, d, e, f int) int
		Mixed(name string, flag bool, n int) string
	})

	fmt.Println("ok", ok)

	if !ok {
		return
	}

	// Called twice: the first call decides and memoizes the pair, the second runs the forwarder on
	// the fully warmed path where the arity dispatch is all that is left.
	for i := 0; i < 2; i++ {
		fmt.Println(i, v.Zero())
		fmt.Println(i, v.One(1))
		fmt.Println(i, v.Two(1, 2))
		fmt.Println(i, v.Three(1, 2, 3))
		fmt.Println(i, v.Four(1, 2, 3, 4))
		fmt.Println(i, v.Five(1, 2, 3, 4, 5))
		fmt.Println(i, v.Sum(1, 2, 3, 4, 5))
		fmt.Println(i, v.Mixed("go", true, 9))
	}
}
