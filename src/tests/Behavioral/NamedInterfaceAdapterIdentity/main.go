// Identity of a runtime-resolved NAMED interface value. The shell that satisfies the
// interface is a C#-only artifact; Go never sees it, so it must stay invisible to
// %T, to a re-assert, to a type switch and to interface equality — otherwise a
// reflection-driven store or an `==` between the original value and the asserted
// interface would disagree with Go.
package main

import (
	"fmt"

	"NamedInterfaceAdapterIdentity/identlib"
)

// mark is VALUE-sourced — its shell wraps the struct itself.
type mark struct{ n int }

func (m mark) Greet() string { return fmt.Sprintf("mark%d", m.n) }

// loud is POINTER-sourced — its shell wraps the receiver box.
type loud struct{ n int }

func (l *loud) Greet() string { return fmt.Sprintf("loud%d", l.n) }

func main() {
	v := mark{n: 1}
	g, ok := identlib.TryGreet(v)
	fmt.Println("value", ok)
	fmt.Println(identlib.Describe(g, v))

	p := &loud{n: 2}
	g2, ok2 := identlib.TryGreet(p)
	fmt.Println("pointer", ok2)
	fmt.Println(identlib.Describe(g2, p))
}
