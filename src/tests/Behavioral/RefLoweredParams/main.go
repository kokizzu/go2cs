package main

import "fmt"

// Behavioral guard for the ж-box ref-lowering arc (stage A2 —
// docs/phase4/DESIGN-zh-box-reduction.md §3.3/§3.4, §8 guards 1/3/4): write-through visibility
// through lowered parameters, D1′/D2 forwarding chains, an address-taken local both lowered-fed
// and KEPT in the same package, the defer/go boxed-site carve-out (the 0-vs-7 shape), and a
// func-value use (X5) that keeps the boxed convention.

type vec struct {
	x, y uint64
}

// addTo is ref-lowerable: every use of out is a dereference (D1).
func addTo(out *uint64, v uint64) {
	*out += v
}

// scale is ref-lowerable: field addresses of v feed lowered positions (D1′) and v forwards to
// another lowered position (D2).
func scale(v *vec, k uint64) {
	addTo(&v.x, k)
	addTo(&v.y, k)
	double(v)
}

// double is ref-lowerable: deref-only field writes (D1).
func double(v *vec) {
	v.x *= 2
	v.y *= 2
}

// showVec is ref-lowerable and lets a composite-literal address (§3.3 row 6) observe its pointee.
func showVec(v *vec) {
	fmt.Println("shown:", v.x, v.y)
}

// boxedBump is used as a func VALUE below (X5) — it must keep the boxed convention, and calls
// through both the value and the name must behave identically.
func boxedBump(out *uint64) {
	*out++
}

// guarded compares its pointer with nil (X1) — excluded from lowering, so a nil argument flows
// exactly as today.
func guarded(p *uint64) uint64 {
	if p == nil {
		return 42
	}
	return *p
}

// bump is ref-lowerable and is ALSO called under defer/go — the boxed-site carve-out: the
// defer/go frame stores the box, and the thunk derives the ref at invoke time.
func bump(p *uint64) {
	*p++
}

// printVal is ref-lowerable; under defer it must observe the value at INVOKE time through the
// eagerly-captured address (Go's defer semantics — the 0-vs-7 shape).
func printVal(p *uint64) {
	fmt.Println("defer sees:", *p)
}

func setVal(p *uint64, v uint64, done chan bool) {
	*p = v
	done <- true
}

func deferReads() {
	var x uint64
	defer printVal(&x)
	x = 7
}

func deferWrites() (result uint64) {
	defer bump(&result)
	return 6
}

func goWrites() uint64 {
	var x uint64
	done := make(chan bool)
	go setVal(&x, 21, done)
	<-done
	return x
}

func main() {
	// An address-taken local whose EVERY address use feeds lowered positions (reverts to a
	// plain local — §3.3).
	var total uint64
	addTo(&total, 5)
	addTo(&total, 7)
	fmt.Println("total:", total)

	// A local BOTH lowered-fed (&v below) and kept (its address is stored into p — one
	// surviving box use keeps the box; the lowered site still aliases the same storage).
	v := vec{x: 1, y: 2}
	scale(&v, 3)
	fmt.Println("vec:", v.x, v.y)

	p := &v
	double(p)
	fmt.Println("vec2:", v.x, v.y)

	// Element address at a lowered position (§3.3 row 4).
	arr := [3]uint64{10, 20, 30}
	addTo(&arr[1], 1)
	fmt.Println("arr:", arr[0], arr[1], arr[2])

	// Composite-literal address (§3.3 row 6): the callee observes the constructed value.
	showVec(&vec{x: 4, y: 5})

	// The defer/go boxed-site carve-out: writes and reads through eagerly-captured addresses.
	deferReads()
	fmt.Println("defer wrote:", deferWrites())
	fmt.Println("go set:", goWrites())

	// X5: a func value keeps the boxed convention; both call forms mutate the same storage.
	f := boxedBump
	var n uint64 = 9
	f(&n)
	boxedBump(&n)
	fmt.Println("n:", n)

	// X1: the nil-guarding callee tolerates nil exactly as today.
	fmt.Println("guarded nil:", guarded(nil))
	var g uint64 = 8
	fmt.Println("guarded val:", guarded(&g))
}
