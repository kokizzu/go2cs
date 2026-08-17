// DefinedElemStringConversion guards the string ↔ byte/rune-slice conversions in which a
// DEFINED type sits on one end or the other. Go writes all of these with the same syntax as
// the plain forms, but C# reaches the two ends through different machinery, and neither end
// is reachable by chaining conversions (C# applies at most one user-defined conversion):
//
//   - the STRING end, when it is a defined type over `string`: the [GoType] wrapper converts
//     to golib's `@string`, and `@string` converts to `byte[]` — two user-defined hops, so the
//     conversion must spell the `(@string)` step itself;
//   - the ELEMENT end, when the byte/rune element is a defined type: `slice<byte>` and
//     `slice<myByte>` are unrelated generic instantiations with no conversion between them at
//     all, so the elements are projected one at a time through the element wrapper's own
//     conversion.
//
// The shapes are ordinary Go and appear in encoding/json's suite (`[]byte(strMarshaler)`,
// `[]Uint8("hello")`, `renamedRenamedByteSlice("abc")`) — five of the eight errors that stood
// between that package and its first run.
package main

import "fmt"

// The string end.
type namedString string

// The element end.
type namedByte byte
type namedRune rune

// Slice types over a defined element — the element end reached through a named slice.
type namedByteSlice []namedByte
type namedRuneSlice []namedRune

// Control: a named slice whose element is the PLAIN basic type, which already converts
// through the underlying-slice hop (guarded on its own by NamedByteSliceFromStringLit).
type plainByteSlice []byte

func takesBytes(b []byte) int { return len(b) }

func takesNamedBytes(b []namedByte) int { return len(b) }

func main() {
	// ---- the STRING end is defined ----
	var ns namedString = "hi"
	fmt.Println([]byte(ns))        // [104 105]
	fmt.Println([]rune(ns))        // [104 105]
	fmt.Println(takesBytes([]byte(ns))) // 2

	// Through a pointer deref — json's strPtrMarshaler shape.
	pns := &ns
	fmt.Println([]byte(*pns)) // [104 105]

	// A defined string carrying multi-byte content: []byte counts BYTES, []rune counts
	// code points, so the two lengths differ.
	var wide namedString = "héllo"
	fmt.Println(len([]byte(wide)), len([]rune(wide))) // 6 5

	// ---- the ELEMENT end is defined ----
	nb := []namedByte("hello")
	fmt.Println(nb, len(nb)) // [104 101 108 108 111] 5

	nr := []namedRune("héllo")
	fmt.Println(nr, len(nr)) // [104 233 108 108 111] 5

	// From a string VARIABLE, not a literal.
	plain := "abc"
	fmt.Println([]namedByte(plain)) // [97 98 99]

	// Through a named slice type over the defined element.
	nbs := namedByteSlice("abc")
	nrs := namedRuneSlice("héllo")
	fmt.Println(nbs, nrs) // [97 98 99] [104 233 108 108 111]

	// The same, from a string VARIABLE rather than a literal.
	plainVar := "abc"
	fmt.Println(namedByteSlice(plainVar), plainByteSlice(plainVar)) // [97 98 99] [97 98 99]

	// And from a DEFINED string.
	fmt.Println(namedByteSlice(ns), plainByteSlice(ns)) // [104 105] [104 105]

	// At argument position.
	fmt.Println(takesNamedBytes([]namedByte("wxyz"))) // 4

	// ---- BOTH ends defined ----
	fmt.Println([]namedByte(ns)) // [104 105]

	// ---- back to string: the element end is defined ----
	fmt.Println(string([]namedByte{104, 105}))     // hi
	fmt.Println(string(nbs))                       // abc
	fmt.Println(string([]namedRune{104, 233, 105})) // héi
	fmt.Println(string(nrs))                       // héllo

	// The slice materialized by a defined-element conversion is DETACHED, exactly as Go's
	// string→slice conversion is: writing an element cannot be observed through the source.
	src := "abc"
	copyOf := []namedByte(src)
	copyOf[0] = 122
	fmt.Println(src, string(copyOf)) // abc zbc

	// ---- controls: no defined type on either end ----
	fmt.Println([]byte("lit"))          // [108 105 116]
	fmt.Println([]byte(plain))          // [97 98 99]
	fmt.Println([]rune(plain))          // [97 98 99]
	pbs := plainByteSlice("abc")
	fmt.Println(pbs, string(pbs)) // [97 98 99] abc
}
