package main

import "fmt"

// A VALUE-receiver method value binds a COPY of its receiver at the moment the method value is
// EVALUATED, not when the resulting func is called. A closure over the same variable binds the
// VARIABLE and observes later writes. The two therefore disagree on purpose, and a converter that
// makes them share one receiver rendering is wrong whichever way it collapses them.

type frame struct {
	Name    string
	Inlined bool
}

func (f frame) label() string { return f.Name }

func main() {
	// 1. Composite literal holding a method value AND a sibling closure over the same variable,
	//    in ONE statement, with the variable written afterwards. The method value must report the
	//    pre-write receiver; the closure must report the post-write one.
	a := frame{Name: "a"}

	typed := []func() string{
		a.label,
		func() string { return a.Name },
	}

	a.Name = "A"

	fmt.Println("typed   ", typed[0](), typed[1]())

	// 2. The same shape through an EMPTY-interface element. This is the spelling that renders the
	//    receiver as a bare ref-local alias, which a lambda cannot capture at all.
	b := frame{Name: "b", Inlined: true}

	boxed := []any{
		b.label,
		func() bool { return b.Inlined },
	}

	b.Name = "B"

	fmt.Println("boxed   ", boxed[0].(func() string)(), boxed[1].(func() bool)())

	// 3. Two method values over one variable in DIFFERENT statements, each with its own write
	//    between them. They must NOT converge on a shared snapshot: each binds the receiver as it
	//    stood when that method value was evaluated.
	c := frame{Name: "p"}
	first := c.label
	c.Name = "q"
	second := c.label
	c.Name = "r"

	fmt.Println("distinct", first(), second(), c.label())

	// 4. A method value passed as a call ARGUMENT, the non-assignment position that has no
	//    statement of its own to hang a declaration on.
	d := frame{Name: "d"}
	call := func(f func() string) string { return f() }
	got := call(d.label)
	d.Name = "D"

	fmt.Println("argument", got, call(d.label))
}
