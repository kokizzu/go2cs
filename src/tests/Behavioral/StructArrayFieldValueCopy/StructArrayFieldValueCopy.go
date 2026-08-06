package main

import "fmt"

// digest mirrors the crypto/sha* shape: a struct whose state lives in FIXED-SIZE
// ARRAY fields. Go copies the arrays inline on every struct value transfer, but
// the emitted `array<T>` is a struct over a shared T[] backing, so a plain C#
// struct copy would ALIAS the backing and let the copy's mutations reach back
// into the source.
type digest struct {
	h  [4]int
	x  [2]byte
	nx int
}

func (d *digest) bump() {
	for i := range d.h {
		d.h[i]++
	}
	d.x[0]++
	d.nx++
}

func (d digest) show() string {
	return fmt.Sprintf("%v %v %d", d.h, d.x, d.nx)
}

// nest embeds a clone-needing struct BY VALUE, so nest itself needs the same
// treatment transitively (its own copy must copy digest's arrays).
type nest struct {
	d   digest
	tag int
}

func takeValue(c digest) string {
	c.bump()
	return c.show()
}

func returnField(n nest) digest {
	return n.d
}

func newDigest() digest {
	return digest{h: [4]int{1, 2, 3, 4}, x: [2]byte{5, 6}, nx: 7}
}

// derefCopy is the crypto/sha256 `d0 := *d` shape: Sum() copies the digest so
// the caller can keep writing, then finalizes the COPY.
func derefCopy() {
	d := newDigest()
	p := &d
	d0 := *p
	d0.bump()
	fmt.Println("deref:      ", p.show(), "|", d0.show())
}

func identCopy() {
	d := newDigest()
	c := d
	c.bump()
	fmt.Println("ident:      ", d.show(), "|", c.show())
}

func selectorCopy() {
	n := nest{d: newDigest(), tag: 1}
	e := n.d
	e.bump()
	fmt.Println("selector:   ", n.d.show(), "|", e.show())
}

func compositeElement() {
	d := newDigest()
	n := nest{d: d, tag: 2}
	n.d.bump()
	fmt.Println("composite:  ", d.show(), "|", n.d.show())
}

func nestedStructCopy() {
	n := nest{d: newDigest(), tag: 3}
	n2 := n
	n2.d.bump()
	n2.tag = 4
	fmt.Println("nested:     ", n.d.show(), n.tag, "|", n2.d.show(), n2.tag)
}

func paramByValue() {
	d := newDigest()
	got := takeValue(d)
	fmt.Println("param:      ", d.show(), "|", got)
}

func returnValue() {
	n := nest{d: newDigest(), tag: 5}
	r := returnField(n)
	r.bump()
	fmt.Println("return:     ", n.d.show(), "|", r.show())
}

func indexCopy() {
	d := newDigest()
	arr := [2]digest{d, d}
	f := arr[0]
	f.bump()
	fmt.Println("index:      ", arr[0].show(), "|", f.show())

	s := []digest{d}
	g := s[0]
	g.bump()
	fmt.Println("sliceIndex: ", s[0].show(), "|", g.show())
}

func rangeCopy() {
	d := newDigest()
	arr := [2]digest{d, d}

	for _, g := range arr {
		g.bump()
	}

	fmt.Println("range:      ", arr[0].show(), arr[1].show())
}

func mapValueCopy() {
	m := map[string]digest{"k": newDigest()}
	mv := m["k"]
	mv.bump()
	stored := m["k"]
	fmt.Println("map:        ", stored.show(), "|", mv.show())
}

func valueReceiver() {
	d := newDigest()
	before := d.show()
	// A VALUE receiver takes its own copy of the digest.
	after := d.show()
	fmt.Println("receiver:   ", before, "|", after)
}

func main() {
	derefCopy()
	identCopy()
	selectorCopy()
	compositeElement()
	nestedStructCopy()
	paramByValue()
	returnValue()
	indexCopy()
	rangeCopy()
	mapValueCopy()
	valueReceiver()
}
