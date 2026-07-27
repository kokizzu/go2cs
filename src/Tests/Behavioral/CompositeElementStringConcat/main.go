package main

import "fmt"

// A string CONCAT used as an ELEMENT of a composite literal must keep the `u8` span
// rendering for its literal operand, exactly as a bare literal element does. The typed
// slice/array path used to mark only its BasicLit elements as span-tolerant, so a concat
// element's literal fell back to a UTF-16 C# string — a UTF8.GetBytes transcode plus a
// throwaway intermediate @string on every evaluation. The sibling spellings below
// (parenthesized, elided) already rendered u8, which is what proved the slot accepts a
// span; all of them must agree now.

func take(s []string) []string { return s }

func main() {
	prefix := "go"

	// TYPED slice composite — the shape that regressed.
	typedSlice := []string{prefix + "-a", prefix + "-b"}

	// TYPED array composite — same path, array flavor.
	typedArray := [2]string{prefix + "-c", prefix + "-d"}

	// ELIDED inner composite — already rendered u8 (nil element context).
	elided := [][]string{{prefix + "-e"}}

	// PARENTHESIZED element — already rendered u8 (parens drop the literal context).
	parens := []string{(prefix + "-f")}

	// Bare literal element (control) and a constant-folded literal concat.
	bare := []string{"lit", "x" + "y"}

	// Composite feeding a call argument.
	viaCall := take([]string{prefix + "-g"})

	// Map value and struct field concats — already-working siblings, pinned so the
	// shared per-element flag cannot regress them.
	inMap := map[string]string{"k": prefix + "-h"}
	type rec struct{ a, b string }
	inStruct := rec{prefix + "-i", "plain"}

	fmt.Println(typedSlice, typedArray, elided, parens, bare, viaCall)
	fmt.Println(inMap["k"], inStruct.a, inStruct.b)

	// A concat in a vararg `any` slot must NOT take the span form — a
	// ReadOnlySpan<byte> cannot box to object. This is the suppression the
	// composite-element flag must not disturb.
	fmt.Println(prefix + "-j")
}
