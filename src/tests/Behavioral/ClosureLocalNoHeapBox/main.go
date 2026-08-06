package main

import "fmt"

// Guards the escape-analysis narrowing in performEscapeAnalysis's *ast.FuncLit arm.
//
// That arm exists to catch a variable a closure CLOSES OVER — code in another frame reaching this
// frame's storage, which the emitted C# serves through a shared `ж<T>` box. It matched on any
// mention of the variable lexically inside the literal's body, which for a variable DECLARED THERE
// is its own declaration: `var t Time; t.UnmarshalText(in)` inside a closure heap-boxed `t`, and the
// box `Ꮡt` was never referenced (128 bytes per call — the other half of `time`'s
// TestUnmarshalTextAllocations `want 0`), while the identical statements outside a closure emitted a
// plain local. Go scoping puts a literal's own local out of reach of every other frame, so there is
// nothing for a shared box to make visible.
//
// N2/N3/N5/N6/N8 are the boxes that must SURVIVE — one per escape route that a closure-declared
// local can still take. Each writes through the escaping alias and reads the result back, so an
// over-narrowed rule prints the wrong number rather than merely emitting a different shape.

type counter struct {
	n   int
	tag string
}

func (c *counter) add(k int) {
	c.n += k
	c.tag += "+"
}

func writeThrough(p *counter, k int) { p.n += k }

func run(f func()) { f() }

// N1 — a struct local DECLARED INSIDE a closure, mutated only through its pointer-receiver method.
// The C# `this ref counter` extension binds the variable directly; no box is needed.
func n1() {
	run(func() {
		var c counter
		c.add(3)
		c.add(4)
		fmt.Println("N1:", c.n, c.tag)
	})
}

// N2 — same declaration, but its ADDRESS is taken and written through: still boxed.
func n2() {
	run(func() {
		var c counter
		p := &c
		p.n = 10
		writeThrough(p, 5)
		fmt.Println("N2:", c.n)
	})
}

// N3 — declared in the OUTER literal and captured + written by a NESTED one: still boxed, because
// the nested literal genuinely closes over it. This is the case the narrowing must not swallow —
// the outer literal is skipped, the inner one still marks the escape.
func n3() {
	run(func() {
		var c counter
		inner := func(k int) { c.n += k }
		inner(7)
		inner(8)
		fmt.Println("N3:", c.n)
	})
}

// N4 control — declared in the enclosing FUNCTION and captured by a closure: the arm's original
// case, unchanged and still boxed.
func n4() {
	var c counter
	run(func() { c.n += 9 })
	fmt.Println("N4:", c.n)
}

// N5 — an ARRAY declared inside a closure whose ELEMENT address is taken: still boxed.
func n5() {
	run(func() {
		var a [3]int
		q := &a[1]
		*q = 42
		fmt.Println("N5:", a)
	})
}

// N6 — declared inside a closure and passed as a POINTER argument: still boxed.
func n6() {
	run(func() {
		var c counter
		writeThrough(&c, 11)
		fmt.Println("N6:", c.n)
	})
}

// N7 — declared inside a closure, written directly and copied by value: a plain local, and the
// copy must be independent.
func n7() {
	run(func() {
		var c counter
		c.n = 5
		d := c
		d.n = 6
		fmt.Println("N7:", c.n, d.n)
	})
}

// N8 — a pointer-receiver METHOD VALUE on a closure-declared local is Go's `(&c).add`, which binds
// a pointer into the variable's storage: still boxed.
func n8() {
	run(func() {
		var c counter
		bump := c.add
		bump(2)
		bump(3)
		fmt.Println("N8:", c.n, c.tag)
	})
}

func main() {
	n1()
	n2()
	n3()
	n4()
	n5()
	n6()
	n7()
	n8()
}
