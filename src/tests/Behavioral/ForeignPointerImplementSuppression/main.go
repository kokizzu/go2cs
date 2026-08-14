// Cross-package POINTER-implementer guard — the pointer-form sibling of
// ForeignValueImplementSuppression.
//
// Converting a foreign package's `*T` into an interface that package itself declares emitted a
// LOCAL `<pkg>_<T>ж<Iface>` adapter even though the declaring assembly had already generated its
// own public `TжIface` — because the two sides of the record lookup spelled the key differently.
// A record parsed from a package_info file names an interface the recording package declares BARE
// (`Node`) while a cast site renders the whole namespace chain
// (`ForeignPointerImplementSuppression.tone_package.Level`), and the record carries the EMITTED
// type name (`ΔTone`, collision-renamed) where the use side named the Go type. Neither direction
// is reliably the longer one: go/types records its own `Object` whole and text/template/parse
// records its own `Node` bare, which is why one canonical spelling — not a heuristic — is the fix.
//
// Both sides now compose through implementRecordKey, so these conversions reference tone's own
// adapters. `Lone` was the negative for "a pair tone satisfies but never records"; the declaring
// side now records the POINTER pairs it merely satisfies (samePackageImplements.go), so it has
// become this test's proof that a record needs no witnessing cast. The negative that must survive
// is `shade.Level` — an interface with the same SIMPLE name as tone's, whose pair has no record
// and must keep its local adapter.
package main

import (
	"fmt"

	"ForeignPointerImplementSuppression/shade"
	"ForeignPointerImplementSuppression/tone"
)

func main() {
	t := &tone.Tone{D: 3}
	var l tone.Level = t

	fmt.Println("tone:", l.Tone(), l.Name())

	// ALIASING is the pointer form's defining property: Go's interface holds the *T, so a write
	// through the interface is visible on the original. A deref-copy in the slot loses this.
	l.Set(7)
	fmt.Println("alias:", t.D, l.Tone())

	// Identity. Two interface values made from ONE pointer are equal; from different pointers,
	// not — even with equal contents.
	var l2 tone.Level = t
	fmt.Println("eq:", l == l2, l == tone.Level(&tone.Tone{D: 7}))

	// A type assert back to *T unwraps to the SAME pointer, not a copy.
	back, ok := l.(*tone.Tone)
	fmt.Println("assert:", ok, back == t)
	_, badOK := l.(*tone.Plain)
	fmt.Println("assert-miss:", badOK)

	switch v := l.(type) {
	case *tone.Plain:
		fmt.Println("switch:", "plain", v.D)
	case *tone.Tone:
		fmt.Println("switch:", "tone", v.D)
	default:
		fmt.Println("switch:", "default")
	}

	// A map keyed by the interface hashes and compares on pointer identity, so one pointer
	// entered twice is ONE entry.
	seen := map[tone.Level]string{}
	seen[l] = "first"
	seen[l2] = "second"
	fmt.Println("map:", seen[l], len(seen))

	// The ordinary, non-collision-renamed implementer — the parse.ListNode shape.
	p := &tone.Plain{D: 4}
	var pl tone.Level = p
	pl.Set(6)
	fmt.Println("plain:", pl.Tone(), pl.Name(), p.D)

	// The declaring package's OWN conversions — the sites that wrote the records.
	fmt.Println("lib:", tone.Default.Tone(), tone.DefaultPlain.Tone())

	// Cross-assembly identity: a value the DECLARING package converted and one converted HERE
	// must be the same interface value. Two adapter classes over one box would still compare
	// equal by box, but only one of them is the type the declaring assembly registered.
	fmt.Println("describe:", tone.Describe(t) == l)

	// WITNESSLESS PAIR — tone satisfies it and never casts it, yet the record exists, so this
	// references tone's own adapter exactly as the cast pairs above do.
	var ln tone.Level = &tone.Lone{D: 5}
	ln.Set(2)
	fmt.Println("lone:", ln.Tone(), ln.Name())

	// NEGATIVE — a SAME-NAMED interface in another package. tone's Tone→tone.Level record must
	// not satisfy Tone→shade.Level; that one has no record and keeps its local adapter.
	var sl shade.Level = t
	sl.Set(9)
	fmt.Println("shade:", sl.Tone(), sl.Name(), t.D)

	// And the two interfaces stay distinct all the way through: the shade-typed value still
	// aliases the same object the tone-typed one does.
	fmt.Println("cross:", l.Tone(), sl.Tone(), back.D)
}
