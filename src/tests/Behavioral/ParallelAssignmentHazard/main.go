package main

import "fmt"

// Guards the parallel-assignment simultaneity fix in the converter. A Go parallel
// assignment evaluates EVERY right-hand expression before performing ANY store, so a
// left-hand variable that is written by one target and read by a LATER target must be
// read at its ORIGINAL value. Sequential C# emission (one store at a time) would let the
// later read see the already-updated value. This is the strconv Ryu rounding pattern
// (`dc, fracc := dc>>extra, dc&extraMask`) that made math's FloatMinMax round down.
func main() {
	// Reassigned `x` + newly-declared `frac`, `frac` reads the ORIGINAL x.
	x := uint64(0b1011010) // 90
	x, frac := x>>3, x&7
	fmt.Println(x, frac) // 11 2  (2 = 90&7, NOT 3 = 11&7)

	// Two reassigned + one declared; every RHS reads the pre-assignment values.
	a, b := 10, 20
	a, b, c := b, a, a+b
	fmt.Println(a, b, c) // 20 10 30

	// Three reassigned + one declared (rotate with a running sum).
	p, q, r := 1, 2, 3
	p, q, r, s := q, r, p, p+q+r
	fmt.Println(p, q, r, s) // 2 3 1 6

	// No read-after-write hazard: stays a plain sequential store (minimal drift).
	m := 5
	m, n := m+1, 100
	fmt.Println(m, n) // 6 100

	// SELECTOR targets: a parallel assignment whose left-hand sides are all FIELDS. A field write
	// is a write to existing storage, exactly like the star-deref and index forms — but it was
	// counted as neither reassigned nor declared, so the statement matched no tuple-path gate and
	// shattered into sequential stores, losing the swap's implicit temporary. This is regexp's
	// onepass `inst.Out, inst.Arg = inst.Arg, inst.Out`, which left BOTH fields holding the
	// original Arg and silently disabled the one-pass engine for `^[a-c]*$` and friends.
	e := &edge{out: 7, arg: 9}
	e.out, e.arg = e.arg, e.out
	fmt.Println(e.out, e.arg) // 9 7   (not 9 9)

	// Field swap on a VALUE struct, plus a cross-struct rotate reading pre-assignment values.
	f := edge{out: 1, arg: 2}
	g := edge{out: 3, arg: 4}
	f.out, g.out, f.arg = g.out, f.arg, f.out
	fmt.Println(f.out, f.arg, g.out, g.arg) // 3 1 2 4

	// Package-level var targets take the same path.
	left, right = right, left
	fmt.Println(left, right) // 20 10
}

type edge struct {
	out int
	arg int
}

var left = 10
var right = 20
