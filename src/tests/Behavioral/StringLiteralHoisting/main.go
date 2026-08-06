// StringLiteralHoisting guards Tier C of docs/phase4/DESIGN-string-literal-allocation.md: a Go
// string literal that materializes a VALUE is hoisted to one package-scoped `static readonly`
// field placed above the function that first uses it, so it costs at most one allocation per
// program run (Go keeps such literals in RODATA and pays nothing). Every §4.2 inclusion and
// exclusion has a case here; the `.cs.target` goldens are what pin the emitted form, and the
// Go↔C# output comparison is what pins that hoisting changes no behavior.
package main

import "fmt"

// --- HOISTED: value return (the design's headline shape) ---------------------------------------
func kind(n int) string {
	if n == 0 {
		return "zero value return"
	}

	return "other value return"
}

// --- HOISTED: @string argument, variadic @string element, named-string conversion ---------------
func echo(s string) string {
	return s
}

func joinAll(parts ...string) string {
	out := ""

	for _, p := range parts {
		out = out + p + "|"
	}

	return out
}

// --- NOT HOISTED: comparison operand (Tier A makes it zero-alloc inline) ------------------------
func isSentinel(word string) bool {
	return word == "comparison operand literal"
}

// --- NOT HOISTED: concat operand (@string's span operator+ already consumes the span) -----------
func withSuffix(suffix string) string {
	return "concat left operand " + suffix
}

// --- NOT HOISTED: []byte source (the result must be freshly mutable) ----------------------------
func mutableBytes() int {
	b := []byte("byte slice source literal")
	b[0] = 'B'

	return len(b)
}

func main() {
	// HOISTED: value returns.
	fmt.Println(kind(0), kind(1))

	// HOISTED: an @string parameter, and the same literal again from another function in another
	// file (cross-file dedupe — ONE field, declared above THIS function, the first package-wide use).
	fmt.Println(echo("shared across files"))

	// HOISTED: variadic @string elements.
	fmt.Println(joinAll("variadic element one", "variadic element two"))

	// HOISTED: struct-field assignment.
	var b box
	b.name = "struct field assignment"
	b.tag = "struct field second"
	fmt.Println(b.name, b.tag)

	// HOISTED: a named-string-type conversion in a function body.
	fmt.Println(string(label("named string conversion")))

	// HOISTED: a standalone map-index key, read and written.
	counts := map[string]int{}
	counts["standalone map key"]++
	counts["standalone map key"] += 2
	fmt.Println(counts["standalone map key"])

	// HOISTED PRE-BOXED: every package use of these is an `any` target, so the field is an
	// `object` holding the boxed @string and the call sites allocate nothing at all.
	fmt.Println("pre boxed any target")

	// HOISTED as ONE @string field (MIXED use): this literal reaches both a `string` parameter and
	// an `any` slot, so it can never be pre-boxed — the any use boxes per call.
	fmt.Println(echo("mixed use literal"), "mixed use literal")

	// NOT HOISTED: comparison, concat, []byte source.
	fmt.Println(isSentinel("comparison operand literal"), isSentinel("nope"))
	fmt.Println(withSuffix("tail"))
	fmt.Println(mutableBytes())

	// NOT HOISTED: a *f FORMAT-POSITION literal (2,985 corpus sites; a format string slugs badly
	// and a formatting call's cost is dominated by formatting itself). The trailing `any` argument
	// IS an ordinary any-slot literal and hoists.
	fmt.Printf("format position literal %s\n", "format trailing argument")

	// NOT HOISTED: a degenerate slug — two characters or fewer carries no information (§4.10
	// amended the floor from ≤3). `OK` folds to `ok` (2) and stays inline; `yes` slugs to `yes`
	// (3) and now hoists, as does `true` (4). All are value returns, so only the slug length
	// separates them.
	fmt.Println(echo("OK"), echo("yes"), echo("true"))

	// HOISTED with the §4.10 mixed-case leading-word fold: a leading uppercase run folds whole
	// (`BLAKE2s-256` -> blake2s256ˢ, not bLAKE2s256ˢ), and a run followed by a lowercase letter
	// keeps its last upper as the interior word's initial (`HTTPServer` -> httpServer).
	fmt.Println(echo("BLAKE2s-256"), echo("HTTPServer ready"))

	// NOT HOISTED: the empty literal already costs 0 B (an empty span's ToArray is Array.Empty).
	fmt.Println(echo("") == "")

	// NOT HOISTED: composite-literal elements and keys (a composite materializes its whole table
	// per evaluation anyway) — but they DO render `u8` after this arc's opener.
	parts := []string{"composite element one", "composite element two"}
	boxed := box{"struct composite element", "struct composite tag"}
	table := map[string]string{"composite key": "composite value"}
	fmt.Println(parts[0], parts[1], boxed.name, boxed.tag, table["composite key"])

	// NOT HOISTED: an any-ELEMENT composite keeps the boxed form, with u8 (the opener's other half).
	anys := []any{"any composite element", 7}
	fmt.Println(anys[0], anys[1])

	// The package-level initializer's literal stays inline; the init-order relocation makes the
	// derived value observable and NON-EMPTY.
	fmt.Println(pkgLevelLiteral)
	fmt.Println(derivedAtInit, len(derivedAtInit) > 0)

	// SLUG COLLISION: two distinct literals whose 24-character word-boundary slug is identical get
	// a deterministic first-occurrence ordinal.
	fmt.Println(echo("the quick brown fox jumps over"))
	fmt.Println(echo("the quick brown fox jumps under"))

	// A literal with no ASCII word content cannot form an identifier and stays inline.
	fmt.Println(echo("世界世界"))
}
