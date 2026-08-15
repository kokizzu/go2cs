package main

import "fmt"

type note struct{ x int }

// Unnamed parameters (Go allows `func(*T)` / `func(int)` with no parameter name). An unnamed
// POINTER parameter previously emitted a broken implicit deref (`ref var  = ref Ꮡ.Value;`) and an
// empty box name (`ж<note> Ꮡ`). It is never referenced in the body, so it needs no deref — just a
// valid placeholder name. The runtime package hits this (sigqueue_note `func sigNoteSetup(*note)`).
var count int

func setup(*note) { count += 1 }
func discard(int) { count += 10 }

// Unnamed and blank VARIADIC parameters. Go permits omitting a variadic parameter's name entirely
// (os/exec's test helpers `func cmdPipeTest(...string)`), and either spelling leaves the parameter
// unreferenceable from the body. The unpacked `slice<T>` local the converter emits for a variadic
// parameter is therefore dead — and emitting it was broken rather than merely redundant: an unnamed
// parameter named that local with the EMPTY string (`var  = ʗp.slice();`, three CS0103 in os/exec's
// converted tests) and a blank one declared a real `_` local that hijacks the body's discards.
// Neither spelling emits any unpacking now, matching the unnamed/blank pointer parameter above.
func unnamedVariadic(...string) { count += 100 }
func blankVariadic(_ ...int)    { count += 1000 }

// Unnamed non-variadic followed by an unnamed variadic — the placeholder names must not collide.
func bothUnnamed(int, ...string) { count += 10000 }

// A named parameter that IS read, alongside a variadic that cannot be. Go forbids mixing named and
// unnamed parameters in one signature, so the unreferenceable one is spelled `_` here.
func label(tag string, _ ...int) string { return tag + "!" }

// Method form of the same shape.
func (n note) tally(...byte) int { return n.x }

// CONTROL: a NAMED variadic still unpacks to a slice and is read. The skip must be scoped to the
// two unreferenceable spellings, not to variadic parameters at large.
func total(vals ...int) int {
	sum := 0
	for _, v := range vals {
		sum += v
	}
	return sum
}

func main() {
	var n note
	setup(&n)
	discard(5)
	unnamedVariadic("a", "b")
	blankVariadic(1, 2)
	bothUnnamed(3, "c")
	fmt.Println(count)
	fmt.Println(label("tag", 7, 8))
	fmt.Println(note{9}.tally(1, 2))
	fmt.Println(total(1, 2, 3, 4))

	// Function literals take the same three shapes. An unnamed variadic was doubly broken here: the
	// literal's signature renders the absent name as `_ʗp` while the prologue rendered `ʗp`, so the
	// dead local was emitted with an empty name AND under a name the signature never declared.
	litUnnamed := func(...int) string { return "lit-unnamed" }
	litBlank := func(_ ...string) string { return "lit-blank" }
	litNamed := func(parts ...string) int { return len(parts) }
	fmt.Println(litUnnamed(1, 2), litBlank("x"), litNamed("p", "q", "r"))
}
