package main

import "fmt"

// A defined type over `string` keeps its named type across a concatenation in Go
// (`type version string; v + "-rc"` is a version). The generated [GoType("@string")]
// wrapper carried the span COMPARISON operators but no `+` at all, so C# fell back to
// converting both operands to a C# string and calling string.Concat: the result was a
// `string`, not the wrapper, and every use site that needed the named type back failed
// (CS0029), while a `u8` literal operand had no candidate to reach at all (CS0019).
// The wrapper now mirrors @string's concat set, so every position below binds.

type version string

func (v version) tag() string { return string(v) + "!" }

func bump(v version) version { return v + "-next" }

func main() {
	base := version("go1.21")

	// Plain concat against a literal, in the three declaration forms.
	short := base + "-rc"
	var explicit version = base + "-beta"
	viaCall := bump(base)

	// Compound assignment.
	acc := base
	acc += "-acc"

	// Named + named (both operands the wrapper).
	joined := short + version("/x")

	// Composite-literal elements, typed and elided.
	typed := []version{base + "-t1", base + "-t2"}
	elided := [][]version{{base + "-e1"}}

	// Map value and struct field slots.
	inMap := map[string]version{"k": base + "-m"}
	type rec struct{ v version }
	inStruct := rec{base + "-s"}

	fmt.Println(short, explicit, viaCall, acc, joined)
	fmt.Println(typed, elided, inMap["k"], inStruct.v)

	// The named type survives the concat, so its methods stay callable.
	fmt.Println(short.tag(), joined.tag())
}
