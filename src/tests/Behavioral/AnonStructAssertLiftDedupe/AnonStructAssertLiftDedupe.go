// One Go anonymous struct type written twice must lift to ONE C# type — including when the
// second occurrence is the operand of a TYPE ASSERTION.
//
// `var p struct{ X, Y int }` lifted `TestAddr_p` (named after the variable) and
// `v0.Interface().(struct{ X, Y int })` lifted `TestAddr_type` (named after a `type`
// placeholder). Go says those are one type and assigns one to the other; C# saw two structs and
// refused — CS0029, "cannot implicitly convert TestAddr_type to TestAddr_p".
//
// The dedupe machinery was already there and already keyed correctly (scope + the full
// types.String(), which is exactly what Go's own struct identity compares). It just never ran
// for this shape: its ANONYMITY test asked whether `identType` is a *types.Struct, while the KEY
// is built from `structSignatureType` — and the type-assertion path supplies no ident at all, so
// `identType` is nil, the test fails, and both dedupe channels are skipped on the way to minting
// a second name. go/types builds a distinct *types.Struct per syntactic literal, which is why the
// registry is keyed on the signature string rather than on pointer identity, and why the
// registry already HELD the right answer at the moment this path declined to consult it.
//
// Guards reflect's TestAddr (all_test.go:3429 and :3470).
//
// The POINTER spelling of the same assertion is a control: it reaches the lift through a
// different path that sources a real *types.Struct, so it deduped correctly all along.
package main

import "fmt"

func main() {
	var p struct {
		X, Y int
	}
	p.X = 1
	p.Y = 2

	var i any = p

	// A1: assign the assertion's result back into the ORIGINAL variable. This is the exact
	// reflect line, and it is what cannot compile when the two occurrences lift separately.
	p = i.(struct {
		X, Y int
	})
	fmt.Println("A1 assert into the original var:", p.X, p.Y)

	// A2: a fresh variable from the assertion, then assigned FROM the original — the
	// unification has to hold in both directions.
	q := i.(struct {
		X, Y int
	})
	fmt.Println("A2 assert into a new var:", q.X, q.Y)

	q = p
	fmt.Println("A3 original assigns into asserted var:", q.X, q.Y)

	p = q
	fmt.Println("A4 asserted var assigns into original:", p.X, p.Y)

	// A5: the comma-ok form.
	r, ok := i.(struct {
		X, Y int
	})
	fmt.Println("A5 comma-ok:", r.X, r.Y, ok)

	// A6: a THIRD textual occurrence, to prove the dedupe is not merely pairwise.
	var third any = struct {
		X, Y int
	}{X: 8, Y: 9}
	u := third.(struct {
		X, Y int
	})
	p = u
	fmt.Println("A6 third occurrence:", p.X, p.Y)

	// A7: comparison across the two occurrences (Go compares struct values of one type).
	fmt.Println("A7 equal across occurrences:", q == u)

	// ---- Controls ----

	// B1: the POINTER spelling, which already deduped.
	pp := &p
	var j any = pp
	pq := j.(*struct {
		X, Y int
	})
	fmt.Println("B1 pointer assertion:", pq.X, pq.Y)

	// B2: a DIFFERENTLY shaped anonymous struct must stay its own type.
	var s struct {
		A string
	}
	s.A = "hi"

	var k any = s
	t := k.(struct {
		A string
	})
	s = t
	fmt.Println("B2 different shape:", s.A)

	// B3: same field names, DIFFERENT field types — also a distinct type.
	var w any = struct {
		X, Y string
	}{X: "a", Y: "b"}
	x := w.(struct {
		X, Y string
	})
	fmt.Println("B3 same names different types:", x.X, x.Y)

	// B4: a failed assertion through the comma-ok form still reports false.
	_, bad := i.(struct {
		A string
	})
	fmt.Println("B4 mismatched assertion:", bad)
}
