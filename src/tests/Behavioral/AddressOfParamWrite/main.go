package main

import "fmt"

// Guards the address-of-value-PARAMETER copy-box defect: a value parameter whose
// address is taken must be heap-boxed at function entry, so writes the callee makes
// through the pointer land in the parameter's own storage. Emitting a call-site
// Ꮡ(value) copy-box compiles but silently drops every such write (image/draw's
// DrawMask -> clip(dst, &r, ...) never saw its clipped rectangle).
//
// Also guards the same defect on a method's value RECEIVER -- the third and last
// signature-declared category. The receiver arrives on funcDecl.Recv, so neither the
// body define-walk nor the parameter scan reached it: `func (b Box) Bumped() int {
// bumpBox(&b); return b.N }` emitted the Ꮡ(b) copy-box and returned the UNBUMPED
// value, and an ARRAY receiver's &a[i] was worse -- it already spelled the identity
// box Ꮡa, which nothing declared (CS0103).

type Rect struct {
	Min, Max int
}

type Box struct {
	R     Rect
	Tag   int
}

// Nums is an INHERENTLY-HEAP (slice) receiver type: &n[i] addresses the shared
// backing array, so it must NOT box the receiver.
type Nums []int

// Trio is an ARRAY receiver type -- a by-value copy per call, like an array parameter.
type Trio [3]int

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

func bumpBox(b *Box) {
	b.Tag += 10
}

// DEFECT SHAPE (receiver): value receiver, address taken, callee WRITES through it.
func (b Box) BumpedRecv() int {
	bumpBox(&b)
	return b.Tag
}

// FIELD address of a value receiver (a value-field chain rooted at the receiver).
func (b Box) ClippedRecv() (int, int) {
	clip(&b.R, 5, 5)
	return b.R.Min, b.R.Max
}

// ELEMENT address of an ARRAY value receiver -- CS0103 before the fix: the emission
// already spelled Ꮡa, but no box was ever declared for a receiver.
func (a Trio) BumpedElemRecv() (int, int, int) {
	bump(&a[1])
	return a[0], a[1], a[2]
}

// CONTROL: an INHERENTLY-HEAP (slice) receiver -- &n[i] aliases the shared backing
// array, so the receiver must stay unboxed (Go does not heap-promote the header).
func (n Nums) BumpedElemSliceRecv() int {
	bump(&n[1])
	return n[1]
}

// CONTROL: receiver address taken but only READ through -- value must be unchanged.
func (b Box) ReadOnlyRecv() (int, int) {
	p := &b
	return p.R.Min, p.Tag
}

// CONTROL: a POINTER receiver is already a box; its emission must not change.
func (b *Box) BumpedPtrRecv() int {
	bumpBox(b)
	return b.Tag
}

// CONTROL: a value receiver whose address is never taken keeps its plain form.
func (b Box) PlainRecv() int {
	return b.Tag
}

// Bumper proves the boxed value receiver keeps its PUBLIC SURFACE: a value-receiver
// method is in the method set of both Box and *Box, so it must still satisfy an
// interface by value -- the box is an implementation detail inside the body.
type Bumper interface {
	BumpedRecv() int
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

	// The receiver arm. Every "recv:" value is what the METHOD saw after the callee
	// wrote through the pointer; every "caller:" value proves the receiver stayed
	// by-value, so the box never leaked back to the caller's variable.
	rb := Box{Rect{0, 16}, 6}
	fmt.Println("recv        :", rb.BumpedRecv(), "caller:", rb.Tag)

	rlo, rhi := rb.ClippedRecv()
	fmt.Println("recv field  :", rlo, rhi, "caller:", rb.R.Min, rb.R.Max)

	rt := Trio{7, 8, 9}
	e0, e1, e2 := rt.BumpedElemRecv()
	fmt.Println("recv elem   :", e0, e1, e2, "caller:", rt[0], rt[1], rt[2])

	rn := Nums{1, 2, 3}
	fmt.Println("recv slice  :", rn.BumpedElemSliceRecv(), "caller:", rn[1])

	rro, rrt := rb.ReadOnlyRecv()
	fmt.Println("recv ro     :", rro, rrt)

	rp := Box{Rect{0, 16}, 6}
	fmt.Println("recv ptr    :", rp.BumpedPtrRecv(), "caller:", rp.Tag)

	fmt.Println("recv plain  :", rb.PlainRecv())

	// Public surface, both halves: a boxed value receiver stays callable through a
	// POINTER (whose caller still sees its own value untouched -- the pointer path
	// copies into the receiver, exactly as Go does) and still satisfies an interface.
	rq := &Box{Rect{0, 16}, 6}
	fmt.Println("recv via ptr:", rq.BumpedRecv(), "caller:", rq.Tag)

	var iface Bumper = Box{Rect{0, 16}, 6}
	fmt.Println("recv iface  :", iface.BumpedRecv())
}
