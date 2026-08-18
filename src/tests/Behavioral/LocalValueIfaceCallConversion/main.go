// Go writes one interface conversion two ways, and the converter must emit the same thing for
// both. `var i Iface = x` has always routed through convertToInterfaceType, which records the
// `[assembly: GoImplement<T, Iface>]` pair go2cs-gen mints the implementing partial from. The
// call-syntax twin `Iface(x)` routed only a POINTER source and a FOREIGN named value source; a
// LOCAL named value source took a plain C# cast that records nothing, so whenever no other site
// recorded the pair the emitted partial did not declare the interface and the cast had nothing
// to bind to — CS0030.
//
// "No other site" is not exotic. recordSamePackageImplements — the speculative recorder that
// covers pairs a package satisfies structurally without ever witnessing (SamePackageImplementNoWitness
// guards it) — declines exactly the two shapes below: an interface declared in ANOTHER assembly,
// because it pairs two LOCALS only, and an UNEXPORTED local interface, because a record is a
// cross-assembly contract and no other assembly could name one. Those are crypto/ed25519's
// `crypto.Signer(private)` over `type PrivateKey []byte` and internal/reflectlite's
// `pinUnexpMeth(EmbedWithUnexpMeth{})` — two packages, one root.
//
// The route is record-only for a non-func value source, so the emitted expressions here are
// character-for-character what the plain cast produced; what changes is that the partial now
// declares the interface. The named FUNC source is the one whose emission does move, and must:
// a C# delegate cannot be a partial struct, so the generator realizes that pair as a value
// adapter class the conversion site references instead.
//
// An INTERFACE source is deliberately ABSENT. `valued(d)` with `d` an interface is the same
// call-syntax-skips-the-route family, and it is measured broken on master — the plain cast it
// emits throws InvalidCastException at runtime where assignment syntax builds the `ᴠ`
// adapter — but routing it is the recordableInterface class, a wider emission change owing its
// own re-proof. It is recorded on the phase-4 board rather than smuggled in here, and this
// project's converter guard pins only that the value-source fix leaves that emission alone.
//
// Everything is printed rather than merely converted, because a record decides IDENTITY as well
// as bindability: the value must land in the interface slot directly, so its Go dynamic type
// survives %T, type assertion and equality.
package main

import "fmt"

// The crypto/ed25519 shape: a defined type over a SLICE, converted in call syntax to an
// interface in another assembly.
type LocalKey []byte

func (k LocalKey) String() string { return "key:" + string(k) }

// The same reach for a STRUCT source, so no fix can be a one-shape special case.
type Label struct {
	Text string
}

func (l Label) String() string { return "label:" + l.Text }

// The internal/reflectlite shape, verbatim in structure: a local value type converted in call
// syntax to a local UNEXPORTED interface, at package scope.
type unexpIface interface {
	f() string
}

type embedWithUnexpMeth struct{}

func (embedWithUnexpMeth) f() string { return "f" }

var pinUnexpMethI = unexpIface(embedWithUnexpMeth{})

// The NO-CHURN control: a local value type converted to a local EXPORTED interface. The
// speculative recorder already covers this pair, so nothing about it may move.
type LocalIface interface {
	G() string
}

type localImpl struct {
	N int
}

func (l localImpl) G() string { return fmt.Sprint("g", l.N) }

// A named FUNC type — the one source shape whose emission changes. A delegate has no partial
// struct to carry the interface, so the pair is realized as a value adapter class.
type meter func() int

func (m meter) Value() int { return m() }

type gauge int

func (g gauge) Value() int { return int(g) * 2 }

type valued interface {
	Value() int
}

func main() {
	// Case 1 — local SLICE-underlying value to a FOREIGN interface, in call syntax.
	k := LocalKey("abc")
	s := fmt.Stringer(k)
	fmt.Println("foreign-slice:", s.String())

	// %T and equality both read the DYNAMIC type: a wrapper in the slot would answer with the
	// adapter's identity instead of the Go value's, and a directly-boxed value would then compare
	// unequal to the one in the interface.
	fmt.Printf("foreign-slice-type: %T\n", s)

	// Case 2 — local STRUCT value to the same foreign interface.
	ls := fmt.Stringer(Label{Text: "x"})
	fmt.Println("foreign-struct:", ls.String())
	fmt.Printf("foreign-struct-type: %T %v\n", ls, ls)
	lv, lok := ls.(Label)
	fmt.Println("foreign-struct-assert:", lok, lv.Text, ls == fmt.Stringer(Label{Text: "x"}))

	// Case 3 — the reflectlite shape, a local UNEXPORTED interface target.
	fmt.Println("unexported-iface:", pinUnexpMethI.f())
	_, uok := pinUnexpMethI.(embedWithUnexpMeth)
	fmt.Println("unexported-iface-assert:", uok)

	// Case 4 — the no-churn control, a local EXPORTED interface target.
	li := LocalIface(localImpl{N: 2})
	fmt.Println("local-iface:", li.G())
	fmt.Println("local-iface-eq:", li == LocalIface(localImpl{N: 2}), li == LocalIface(localImpl{N: 3}))

	// Case 5 — a named FUNC source, whose conversion routes through a value adapter, and a named
	// NUMERIC source beside it so the two realizations are exercised against one interface.
	mv := valued(meter(func() int { return 11 }))
	gv := valued(gauge(4))
	fmt.Println("func-source:", mv.Value(), "numeric-source:", gv.Value())

	switch t := gv.(type) {
	case meter:
		fmt.Println("switch:", "meter", t())
	case gauge:
		fmt.Println("switch:", "gauge", int(t))
	default:
		fmt.Println("switch:", "default")
	}

	// A map keyed by the interface: the key hashes on the dynamic type plus value, so a value that
	// entered through the call-syntax conversion has to find one that entered by assignment.
	seen := map[valued]string{}
	seen[gv] = "call-syntax"
	var byAssign valued = gauge(4)
	fmt.Println("map:", seen[byAssign], len(seen))
}
