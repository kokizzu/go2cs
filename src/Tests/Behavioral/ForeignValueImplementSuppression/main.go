// Cross-package VALUE-implementer guard, the complement of ValueAdapterDynamicType.
//
// There, the sibling package never converts its own values to its own interface, so no
// `[assembly: GoImplement<T, Iface>]` record exists and the conversion HERE must generate a local
// ᴠ value adapter. Here, `hue` DOES convert its own values, so its assembly really does implement
// the pairs — the conversions below must therefore use the bare value and generate NO adapter.
//
// The lookup that decides this is keyed by (declaring package, type, interface), and both sides of
// that key have to spell it the same way. They did not: a record parsed from a package_info file
// is class-relative (`hue_package.Tint`) while a cast site renders the whole namespace chain
// (`ForeignValueImplementSuppression.hue_package.Tint`), and the record carries the EMITTED name
// (`ΔShade`, collision-renamed) while the use side named the Go type (`Shade`). A wrapper left in
// the interface slot is not merely a wasted allocation — it is a SECOND identity for one Go value,
// which is how image/png found it: `%v` of a color.Color reflected over the ADAPTER object and
// threw (`Field 'R' … is not a field on the target object`).
//
// The named FUNC type at the end is the negative that has to survive the fix: `hue` records
// Tinter→Tint exactly like the struct pairs, but a C# delegate cannot be a partial struct, so that
// one IS realized as an adapter class there — trusting its record would hand a bare delegate to an
// interface slot. net/http's HandlerFunc → Handler is the corpus instance.
package main

import (
	"fmt"

	"ForeignValueImplementSuppression/hue"
)

// Img plays image.NRGBA: a method returning a FOREIGN struct value as a FOREIGN interface.
type Img struct{ px hue.Shade }

func (p *Img) At() hue.Tint { return p.px }

func main() {
	p := &Img{hue.Shade{L: 0x99, A: 0xff}}
	got := p.At()
	want := hue.Shade{L: 0x99, A: 0xff}

	// %v must render the STRUCT's fields and %T its Go dynamic type. A wrapper in the slot makes
	// fmt reflect over the WRAPPER instead — the exact failure image/png's `diff` hit.
	fmt.Printf("shade: %v %T\n", got, got)

	// Identity in both directions: an interface value and a concrete value compare on dynamic
	// type + value, so a wrapped one and a directly-boxed one must still be equal.
	fmt.Println("eq:", got == want, want == got)

	var w hue.Tint = want
	fmt.Println("iface-eq:", got == w)
	fmt.Println("ne:", got == hue.Shade{L: 1, A: 2})

	v, ok := got.(hue.Shade)
	fmt.Println("assert:", ok, v.L, v.A)
	_, badOK := got.(hue.Gray)
	fmt.Println("assert-miss:", badOK)

	switch t := got.(type) {
	case hue.Gray:
		fmt.Println("switch:", "Gray", t.Y)
	case hue.Shade:
		fmt.Println("switch:", "Shade", t.L)
	default:
		fmt.Println("switch:", "default")
	}

	// A map keyed by the interface hashes and compares on the dynamic value.
	seen := map[hue.Tint]string{}
	seen[got] = "from-iface"
	fmt.Println("map:", seen[want], len(seen))

	// The method still dispatches through the interface.
	l, a := got.Shade()
	fmt.Println("call:", l, a)

	// The declaring package's OWN conversions — the sites that write the records.
	fmt.Printf("lib: %v %T %v\n", hue.Default, hue.Default, hue.Default == hue.Shade{L: 1, A: 2})
	fmt.Printf("lib-gray: %v %T\n", hue.DefaultGray, hue.DefaultGray)

	// A second implementer, so no fix can be a one-type special case.
	var g hue.Tint = hue.Gray{Y: 5}
	fmt.Printf("gray: %v %T %v\n", g, g, g == hue.Gray{Y: 5})

	// A conversion the declaring package performs on our behalf must agree with ours.
	fmt.Println("describe:", hue.Describe(want) == got)

	// NEGATIVE — the named FUNC type. Its record exists too, and must NOT be trusted: this
	// conversion keeps its local adapter, and has to keep working.
	var f hue.Tint = hue.Tinter(func() (uint32, uint32) { return 11, 22 })
	fl, fa := f.Shade()
	fmt.Println("func:", fl, fa)

	dl, da := hue.DefaultFunc.Shade()
	fmt.Println("func-lib:", dl, da)
}
