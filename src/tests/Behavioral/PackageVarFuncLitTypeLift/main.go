// Guards the lift of a type declared inside a func literal that sits in a PACKAGE-LEVEL
// initializer.
//
// `inFunction` says a func literal's BODY is function scope; it does NOT say there is an
// enclosing function DECLARATION. currentFuncName and currentFuncPrefix are owned by
// visitFuncDecl, so at package level they held whatever the PREVIOUS function declaration in the
// file left behind. Every type-lift site keys on `lifted && inFunction` and writes the
// declaration into that prefix buffer, so the lifted type was named after an UNRELATED function
// and written into a buffer already flushed -- the declaration vanished, leaving only its use
// site (`new Greet_type(...)`, CS1729 no such constructor, plus CS0103/CS0034 in the
// ImplementGenerator wrapper generated for the phantom type).
//
// This is the shape that held fmt's scan_test.go: `struct{ io.Reader }` returned from a func
// literal inside the package-level `readers` table. Here the embedded interface is local so the
// guard needs no io/strings reference.
//
// The sibling file varfirst.go covers the SAME root's other symptom -- see its comment.
package main

import "fmt"

type Greeter interface {
	Greet() string
}

type namedGreeter struct {
	name string
}

func (g namedGreeter) Greet() string {
	return "hello " + g.name
}

// A function DECLARATION precedes the var, so the stale prefix buffer is non-nil and the lifted
// declaration was silently DROPPED rather than crashing.
var makers = []struct {
	label string
	build func(string) Greeter
}{
	{"direct", func(s string) Greeter {
		return namedGreeter{s}
	}},
	{"embedded", func(s string) Greeter {
		// The guarded shape: an anonymous struct EMBEDDING an interface, lifted from inside a
		// func literal in a package-level var initializer.
		return struct{ Greeter }{namedGreeter{s}}
	}},
}

func main() {
	for _, m := range makers {
		fmt.Println(m.label, m.build("world").Greet())
	}

	fmt.Println(varFirstLabel, varFirst.Greet())
}
