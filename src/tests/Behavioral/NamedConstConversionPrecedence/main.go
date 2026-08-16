// Guards the CAST-PRECEDENCE half of a conversion's operand handling.
//
// A C# cast binds tighter than every binary operator, so a conversion whose CONSTANT operand
// renders as a top-level binary expression must parenthesize it. The named-numeric
// identity-constant arm used to ask only the cast-vs-subtraction PARSE-AMBIGUITY question (a
// leading-sign text test), never the precedence one, and emitted `((rf)3 / 2)` — the cast claims
// the left operand alone.
//
// The defect has two symptoms and this program shows both:
//
//   - SILENT WRONG VALUE for a named int/float target, whose [GoType] wrapper supplies the
//     operator so the mis-bound form still compiles. Go folds `3 / 2` in exact arbitrary
//     precision as an untyped INTEGER division (1) and converts; `((rf)3 / 2)` converts first
//     and divides in the target's own float arithmetic (1.5).
//   - A HARD COMPILE ERROR for a named COMPLEX target, where the first leg has no conversion at
//     all: `((rc64)3F + 4F.i())` is CS0030 float -> rc64. This is the shape that held fmt's own
//     test suite (fmt_test.go's renamedComplex64/renamedComplex128 entries).
//
// A UNARY operand is deliberately included as a control: a cast and a unary operator share
// precedence and associate right, so `(T)~0` already means `(T)(~0)` and must NOT gain parens.
package main

import "fmt"

type rf float64
type rf32 float32
type ri int
type rc64 complex64
type rc128 complex128

var (
	// Untyped INTEGER division folded exactly by Go, then converted: 1, 3, 0 -- never 1.5.
	divA = rf(3 / 2)
	divB = rf(7 / 2)
	divC = rf(1 / 3)
	divD = rf32(5 / 4)

	// Other binary operators through the same arm.
	sumA = rf(1.5 + 2.5)
	mulA = ri(3 * 4)
	subA = ri(10 - 3)
	shfA = ri(1<<4 - 1)

	// Named COMPLEX targets: the CS0030 shape.
	cA = rc64(3 + 4i)
	cB = rc128(4 - 3i)
	cC = rc64(11 + 6e1i)
	cD = rc128(-11. + 7e+1i)

	// Controls that must keep their existing emission.
	negA = ri(-5)
	notA = ri(^0)
	litA = ri(42)
)

func main() {
	fmt.Println(divA, divB, divC, divD)
	fmt.Println(sumA, mulA, subA, shfA)
	fmt.Println(cA, cB, cC, cD)
	fmt.Println(negA, notA, litA)
}
