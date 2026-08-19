// Regression test for blank-identifier name-collision handling (CS0102).
//
// A package that declares blank `_` constants (e.g. to skip iota values) AND a blank
// `func _()` (a common stringer/compile-time-assertion idiom) used to break: the name-
// collision pass saw `_` as both a named element and a method name, flagged it as colliding,
// and Δ-prefixed every `_` to the same `Δ_` — so the multiple blank constants collided in C#
// (CS0102 "already contains a definition for 'Δ_'"). The fix excludes the blank identifier
// from collision analysis; each blank gets a unique generated name instead.
package main

import "fmt"

type Code int

const (
	A Code = iota // 0
	_             // 1 (skipped)
	B             // 2
	_             // 3 (skipped)
	C             // 4
)

// A blank function — the real-world trigger (go's stringer emits one as a compile-time
// assertion). Its mere presence puts `_` in both the named-element and method-name sets,
// which is what used to drive the spurious collision. It is given a unique generated name so a
// `_ = expr` discard in its body stays a discard rather than binding to the method group (CS1656).
func _() {
	if A+B+C < 0 {
		panic("unreachable")
	}
	x := A
	_ = x // a discard inside `func _()` — would bind to the method `_` without the rename
}

// A multi-blank discard assignment `_, _, _, _ = …` used to be split into four separate
// `var _ = x;` declarations — the first declared `_`, the rest collided with it (CS0128
// "A local variable named '_' is already defined"). Each blank LHS must stay a bare discard.
func multiBlank() {
	a, b, c, d := 1, 2, 3, 4
	_, _, _, _ = a, b, c, d
	fmt.Println("multiBlank ok")
}

// A named func type, so a discarded method group can match one. Go writes `_ = someFunc` to
// force a symbol to be linked — debug/elf's file_test.go does exactly `_ = net.ResolveIPAddr`.
type stateFn func(int) stateFn

func lexText(i int) stateFn { return lexNumber }

func lexNumber(i int) stateFn { return nil }

func pair(a string, b int) (string, error) { return a, nil }

func sink(a int) {}

func count() int { return 7 }

func total(a ...int) int {
	n := 0
	for _, v := range a {
		n += v
	}
	return n
}

type counter struct{ n int }

func (c *counter) bump(d int) int { c.n += d; return c.n }

// A discard whose RHS is a METHOD GROUP or a LAMBDA has no C# type of its own, so C# cannot
// infer the discard's type (CS8183) — it must be given a target type. The fix that supplies one
// must not turn the discard into a DECLARATION: when the signature matched a package named func
// type the emission was `stateFn _ = lexText;`, a local literally named `_`, which makes every
// other `_ = x` in the same scope an assignment to it (CS0841 before it, CS0123/CS0029 after)
// and collides outright with a second one (CS0128).
func blankFuncValues() {
	_ = pair        // unnamed signature, params and results
	_ = sink        // no results — Action
	_ = count       // no params — Func<T>
	_ = lexText     // named func type…
	_ = lexNumber   // …twice in one scope: the collision the declaration form caused
	_ = total       // variadic family
	_ = fmt.Sprintf // cross-package, also variadic

	c := &counter{}
	_ = c.bump                               // method VALUE — emits a lambda, not a method group
	_ = func(s string) int { return len(s) } // func literal — a lambda too

	// Controls. A func-typed VARIABLE already has a C# type and takes no cast, and a `:=` from a
	// method group must still DECLARE with the named delegate, so a later same-type assignment
	// stays interconvertible.
	f := pair
	_ = f

	state := lexText
	state = lexNumber
	_ = state

	// The discarded functions still work when actually called.
	s, _ := f("pair", 1)
	fmt.Println(s, count(), total(1, 2, 3), c.bump(5))
}

func main() {
	fmt.Println(A, B, C)
	multiBlank()
	blankFuncValues()
}
