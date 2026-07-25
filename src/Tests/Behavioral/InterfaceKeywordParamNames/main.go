// Guards go2cs-gen's handling of interface-method parameters whose Go names are C# RESERVED
// KEYWORDS — sync.Map's `CompareAndSwap(key, old, new any)`, the real shape this reproduces.
//
// The converter escapes such a name in its own emission (`any @new`), but go2cs-gen re-reads the
// members off the interface SYMBOL when it realizes a [GoImplement] record, and
// IParameterSymbol.Name arrives with the escape STRIPPED. The generated adapter then declared
// `object new`, which does not lex as a parameter: Roslyn's recovery reported CS0501 ("must
// declare a body") on the enclosing member — three of them in sync's converted test build.
//
// Both realization shapes are exercised: a POINTER-receiver implementation (the ж<T> adapter) and
// a VALUE-receiver one, so the escape is pinned on every path that renders these names into a
// declaration or a forwarding call.
package main

import "fmt"

type swapper interface {
	CompareAndSwap(key, old, new any) bool
	Guard(lock, base string, event int) string
}

// cell implements swapper with POINTER receivers -> the ж<T> adapter shape.
type cell struct {
	value any
	tag   string
}

func (c *cell) CompareAndSwap(key, old, new any) bool {
	if c.value != old {
		return false
	}
	c.value = new
	c.tag = fmt.Sprint(key)
	return true
}

func (c *cell) Guard(lock, base string, event int) string {
	return fmt.Sprintf("%s|%s|%d|%s", lock, base, event, c.tag)
}

// frozen implements swapper with VALUE receivers -> the value/promoted shape.
type frozen struct {
	label string
}

func (f frozen) CompareAndSwap(key, old, new any) bool {
	return old == new
}

func (f frozen) Guard(lock, base string, event int) string {
	return fmt.Sprintf("%s~%s~%d~%s", lock, base, event, f.label)
}

func exercise(s swapper) {
	fmt.Println(s.CompareAndSwap("k", 2, 3))
	fmt.Println(s.CompareAndSwap("k", 1, 3))
	fmt.Println(s.CompareAndSwap("k", 3, 3))
	fmt.Println(s.Guard("L", "B", 7))
}

func main() {
	exercise(&cell{value: 1})
	exercise(frozen{label: "static"})

	// A direct (non-interface) call keeps the converter's own emission honest too. The compared
	// values are ints deliberately: an untyped STRING constant reaching an `any` slot is boxed
	// inconsistently today (a composite-literal field gets `(@string)"…"`, a call argument a bare
	// C# string), which is an unrelated open defect this guard must not entangle with.
	c := &cell{value: 42}
	fmt.Println(c.CompareAndSwap("direct", 42, 43), c.Guard("l", "b", 1))
}
