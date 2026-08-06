// Cross-assembly VALUE identity guard. A struct with a VALUE receiver, declared in a SIBLING
// package, converted to its (also foreign) interface HERE. Go's dynamic type of that interface
// value is colorlike.NRGBA, so `==`, a type assert, a type switch and %T must all agree with the
// struct's own identity. image's `func (p *NRGBA) At(x, y int) color.Color { return
// p.NRGBAAt(x, y) }` is exactly this shape — image/draw's TestDrawSrcNonpremultiplied and
// image/gif's `ColorModel().(color.Palette)` are its downstream consumers.
//
// This USED to be the value-ADAPTER site: colorlike never converts its own values to its own
// interface, so no [assembly: GoImplement] record existed and go2cs-gen could only wrap a copy of
// the struct in an adapter CLASS here — a SECOND IDENTITY the assertions below were written to
// catch. The declaring side now records the pair it merely SATISFIES (samePackageImplements.go),
// so colorlike's assembly implements Color itself and every conversion below hands over the bare
// value; these assertions became the proof that it does. SamePackageImplementNoWitness guards
// that rule directly, with the negatives (a named FUNC type, a pointer-only implementer, an
// unexported interface, a generic) that must still take the adapter path.
package main

import (
	"fmt"

	"ValueAdapterDynamicType/colorlike"
)

type Img struct{ px colorlike.NRGBA }

// At converts a FOREIGN struct value to a FOREIGN interface — the value-adapter site.
func (p *Img) At() colorlike.Color { return p.px }

// Alt does the same through a second implementer, so no fix can be a one-type special case.
func (p *Img) Alt() colorlike.Color { return colorlike.Gray{Y: 7} }

func main() {
	p := &Img{colorlike.NRGBA{R: 0x99, G: 0x99, B: 0x99, A: 0xff}}
	got := p.At()
	want := colorlike.NRGBA{R: 0x99, G: 0x99, B: 0x99, A: 0xff}

	// `==` between an interface value and a concrete value compares dynamic type + value.
	fmt.Println("eq:", got == want, want == got)
	fmt.Println("ne:", got == colorlike.NRGBA{R: 1, G: 2, B: 3, A: 4})

	// Two interface values carrying the same dynamic value compare equal.
	var w2 colorlike.Color = want
	fmt.Println("iface-eq:", got == w2)

	// Type assert, both forms, hit and miss.
	v, ok := got.(colorlike.NRGBA)
	fmt.Println("assert:", ok, v.R, v.A)
	_, badOK := got.(colorlike.Gray)
	fmt.Println("assert-miss:", badOK)
	fmt.Println("assert-1:", got.(colorlike.NRGBA).G)

	// Type switch over the same interface value.
	switch t := got.(type) {
	case colorlike.Gray:
		fmt.Println("switch:", "Gray", t.Y)
	case colorlike.NRGBA:
		fmt.Println("switch:", "NRGBA", t.B)
	default:
		fmt.Println("switch:", "default")
	}

	// The same three questions through the OTHER implementer.
	alt := p.Alt()
	fmt.Println("alt-eq:", alt == colorlike.Gray{Y: 7}, alt == got)
	g, gok := alt.(colorlike.Gray)
	fmt.Println("alt-assert:", gok, g.Y)
	switch t := alt.(type) {
	case colorlike.NRGBA:
		fmt.Println("alt-switch:", "NRGBA", t.R)
	case colorlike.Gray:
		fmt.Println("alt-switch:", "Gray", t.Y)
	}

	// %T reports the Go dynamic type, never the C# wrapper class.
	fmt.Printf("type: %T %T\n", got, alt)

	// The interface value still dispatches its method.
	r, gg, b, a := got.RGBA()
	fmt.Println("rgba:", r, gg, b, a)

	// A map keyed by the interface must hash/compare on the dynamic value too.
	seen := map[colorlike.Color]string{}
	seen[got] = "from-iface"
	fmt.Println("map:", seen[want], len(seen))
}
