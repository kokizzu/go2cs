package main

import "fmt"

// A PARENTHESIZED expression must inherit its slot's literal context instead of starting from
// a fresh default. Parentheses are transparent in Go, so `(x)` lands in the enclosing slot
// exactly as `x` does; rendering the paren arm with a fresh context silently re-enabled the
// `"…"u8` ReadOnlySpan<byte> form in span-HOSTILE slots and, symmetrically, was the only
// reason the form survived in span-TOLERANT ones.
//
// The hostile half did not merely differ, it failed to compile: C# folds two utf8 literal
// constants into a single ReadOnlySpan<byte>, and a span has no boxing conversion to
// `object`. `panic(("a" + "b"))` was CS1503 where the unparenthesized `panic("a" + "b")`
// compiled — panic's argument is the one span-hostile slot that does not pick up the
// rescuing `(@string)` box cast, since it short-circuits before convExprList runs.
//
// Every slot below is spelled BOTH ways. The pair must render identically apart from the
// parentheses themselves — that equivalence is what this test pins.

func retConst() any { return ("r1" + "r2") }

func retConstParen() any { return ("r3" + "r4") }

func retVar(a string) any { return a + "-r" }

func retVarParen(a string) any { return (a + "-r") }

func pairConst() (any, int) { return "t1" + "t2", 1 }

func pairConstParen() (any, int) { return ("t3" + "t4"), 2 }

func main() {
	a := "v"

	// --- panic's object parameter: the slot that actually broke ---
	catch(func() { panic("pn1" + "pn2") })
	catch(func() { panic(("pn3" + "pn4")) })
	catch(func() { panic(a + "-pn") })
	catch(func() { panic((a + "-pn")) })

	// --- vararg `any` slot ---
	fmt.Println("p1" + "p2")
	fmt.Println(("p3" + "p4"))
	fmt.Println((("p5" + "p6"))) // nesting collapses to the same slot
	fmt.Println(a + "-x")
	fmt.Println((a + "-y"))

	// A standalone literal takes the boxable combined `(@string)"…"u8` form either way.
	fmt.Println("lit")
	fmt.Println(("lit"))

	// --- interface-typed assignment target ---
	var c1 any = "q1" + "q2"
	var c2 any = ("q3" + "q4")
	var v1 any = a + "-q"
	var v2 any = (a + "-q")
	fmt.Println(c1, c2, v1, v2)

	// --- interface-typed return slot ---
	fmt.Println(retConst(), retConstParen(), retVar(a), retVarParen(a))

	// --- ValueTuple element (multi-return into an `any` result) ---
	x, n := pairConst()
	y, m := pairConstParen()
	fmt.Println(x, n, y, m)

	// --- INVERSE: a composite's string element slot IS span-tolerant ---
	// Both spellings must KEEP the `u8` operand, now through convCompositeLit's per-element
	// gate rather than through the dropped context (see CompositeElementStringConcat).
	elems := []string{a + "-g", (a + "-h")}
	fmt.Println(elems)
}

func catch(f func()) {
	defer func() { fmt.Println("recovered:", recover()) }()
	f()
}
