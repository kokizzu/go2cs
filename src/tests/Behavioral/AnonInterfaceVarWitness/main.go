package main

import "fmt"

// A package var whose DECLARED type is an anonymous struct/interface literal is lifted to a named
// C# type — but that lift only ever ran on the BODYLESS arm. Give the same var an INITIALIZER and
// nothing lifted it, so the raw Go text landed both in the declaration type and in the value
// adapter's class name; its braces close the member, and every following declaration in the file
// then reads as a namespace-level one (CS1519/CS1002 at the site, CS0106 on each member after it).
//
// The witness idiom is why this mattered: crypto/ecdh's test half opens with exactly
//
//	var _ interface{ Equal(x crypto.PublicKey) bool } = &ecdh.PublicKey{}
//
// which is a compile-time assertion that a type implements a documented interface. The BLANK name
// is load-bearing too: `_`'s C# name is a synthesized temp that exists in no Go scope, so the lift
// must be named from the Go identifier or the type and the field collide (CS0102).

type thing struct {
	n int
}

func (t *thing) Equal(x any) bool {
	other, ok := x.(*thing)
	return ok && other.n == t.n
}

func (t *thing) Public() any {
	return t.n
}

func (t *thing) Label() string {
	return fmt.Sprintf("thing(%d)", t.n)
}

// Two blank witnesses over DIFFERENT anonymous interfaces, both initialized.
var _ interface {
	Equal(x any) bool
} = &thing{}

var _ interface {
	Public() any
	Equal(x any) bool
} = &thing{}

// A NAMED var of an anonymous interface type, initialized — the same lift, reached through a name
// rather than a blank, and then actually CALLED so the adapter it routes through has to work.
var labeler interface {
	Label() string
} = &thing{n: 41}

// An anonymous STRUCT declared type with an initializer takes the twin path.
var origin = struct {
	x, y int
}{x: 3, y: 4}

func main() {
	a := &thing{n: 7}
	b := &thing{n: 7}
	c := &thing{n: 8}

	fmt.Println(a.Equal(b), a.Equal(c), a.Public())
	fmt.Println(labeler.Label())
	fmt.Println(origin.x, origin.y)

	// The witness types are usable as ordinary interface values too.
	var eq interface{ Equal(x any) bool } = a
	fmt.Println(eq.Equal(b), eq.Equal(c))
}
