package main

import "fmt"

// Guards the address-of-value-PARAMETER copy-box defect: a value parameter whose
// address is taken must be heap-boxed at function entry, so writes the callee makes
// through the pointer land in the parameter's own storage. Emitting a call-site
// Ꮡ(value) copy-box compiles but silently drops every such write (image/draw's
// DrawMask -> clip(dst, &r, ...) never saw its clipped rectangle).

type Rect struct {
	Min, Max int
}

type Box struct {
	R     Rect
	Tag   int
}

// clip writes through the pointer, exactly like image/draw's clip().
func clip(r *Rect, lo, hi int) {
	if r.Min < lo {
		r.Min = lo
	}
	if r.Max > hi {
		r.Max = hi
	}
}

func bump(p *int) {
	*p += 10
}

// DEFECT SHAPE: value parameter, address taken, callee WRITES through it, and the
// parameter is read again afterwards.
func clipParam(r Rect) Rect {
	clip(&r, 5, 5)
	return r
}

// FIELD address of a value parameter (a value-field chain rooted at the parameter).
func bumpParamField(b Box) Box {
	bump(&b.R.Min)
	return b
}

// ELEMENT address of an ARRAY value parameter.
func bumpParamElem(a [3]int) [3]int {
	bump(&a[1])
	return a
}

// CONTROL: address taken but only READ through -- the value must be unchanged.
func readOnlyParam(r Rect) (int, int) {
	p := &r
	return p.Min, p.Max
}

// CONTROL: an address-taken LOCAL, already correct before the fix.
func clipLocal() Rect {
	r := Rect{0, 16}
	clip(&r, 5, 5)
	return r
}

// CONTROL: a value parameter whose address is never taken keeps its plain form.
func plainParam(r Rect) int {
	return r.Max - r.Min
}

func main() {
	p := clipParam(Rect{0, 16})
	fmt.Println("param       :", p.Min, p.Max)

	b := bumpParamField(Box{Rect{1, 2}, 3})
	fmt.Println("param field :", b.R.Min, b.R.Max, b.Tag)

	a := bumpParamElem([3]int{7, 8, 9})
	fmt.Println("param elem  :", a[0], a[1], a[2])

	lo, hi := readOnlyParam(Rect{3, 4})
	fmt.Println("param ro    :", lo, hi)

	l := clipLocal()
	fmt.Println("local       :", l.Min, l.Max)

	fmt.Println("plain       :", plainParam(Rect{2, 20}))

	// Go passes by value: the caller's argument must be untouched by the callee's
	// write-through-pointer, even though the parameter itself is now heap-boxed.
	orig := Rect{0, 16}
	clipped := clipParam(orig)
	fmt.Println("caller      :", orig.Min, orig.Max, "->", clipped.Min, clipped.Max)
}
