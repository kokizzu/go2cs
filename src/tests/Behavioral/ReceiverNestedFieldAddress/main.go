package main

import "fmt"

// Guards the write-through shape `&recv.valueField.subField` — the address of a field reached
// through one or more VALUE struct hops from a pointer receiver.
//
// Until 2026-08-23 the converter recognized only the ONE-hop form `&recv.field`; a deeper chain
// matched no arm and fell through to the `Ꮡ(value)` copy-box, which boxes a COPY, so every write
// through the returned pointer was silently dropped. It compiled, it ran, and it produced wrong
// numbers. The corpus instance was dnsmessage's incrementSectionCount, whose four
// `count = &b.header.<section>` sites left the DNS header's QDCOUNT at 0.
//
// Coverage for this shape was ZERO before this test.

type inner struct {
	a uint16
	b uint16
}

type outer struct {
	name string
	in   inner
	ptr  *inner
}

// One value hop — the exact F1 shape.
func (o *outer) bumpA() {
	p := &o.in.a
	*p++
}

// Conditionally assigned through a switch, exactly as incrementSectionCount does: the pointer is
// chosen in one arm and written after the switch, so a copy-box loses the write at a distance.
func (o *outer) bumpSelected(which int) {
	var count *uint16
	switch which {
	case 0:
		count = &o.in.a
	case 1:
		count = &o.in.b
	}
	*count += 10
}

// The NEGATIVE control: an intermediate POINTER hop must keep addressing the POINTEE, not the
// pointer's own storage. This is the mirror-image defect the type-aware walk must not introduce.
func (o *outer) bumpViaPtr() {
	p := &o.ptr.b
	*p += 3
}

type leaf struct {
	n int
}

type mid struct {
	lf leaf
}

type deep struct {
	md mid
}

// Two value hops, to prove the chain is walked rather than special-cased at depth one.
func (d *deep) bumpLeaf() {
	p := &d.md.lf.n
	*p += 5
}

// A read-only chain must keep reading the real field after the writes above.
func (o *outer) readA() uint16 {
	p := &o.in.a
	return *p
}

func main() {
	o := &outer{name: "x", ptr: &inner{}}

	o.bumpA()
	o.bumpA()
	fmt.Println("one hop:", o.in.a)

	o.bumpSelected(0)
	o.bumpSelected(1)
	fmt.Println("selected:", o.in.a, o.in.b)

	o.bumpViaPtr()
	fmt.Println("through pointer hop:", o.ptr.b, "value field untouched:", o.in.b)

	d := &deep{}
	d.bumpLeaf()
	d.bumpLeaf()
	fmt.Println("two hops:", d.md.lf.n)

	fmt.Println("read back:", o.readA())
	fmt.Println("name intact:", o.name)
}
