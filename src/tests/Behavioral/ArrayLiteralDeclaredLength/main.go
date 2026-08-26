// Guards the fixed-size array COMPOSITE LITERAL length: a `[N]T{…}` literal is N long no matter
// how many elements it writes, and Go zero-fills the rest. The converter used to project the
// literal straight from its written elements (`new byte[]{}.array()`), so `[8]byte{}` became a
// length-0 array and the first index panicked at run time — it COMPILED, and gave a wrong answer.
// A slice literal is genuinely as long as its elements, and is the control here.
package main

import "fmt"

type named [6]byte
type aliased = [5]byte

// cell is a struct whose zero value NEEDS construction: its fixed-array field's initializer only
// runs inside an explicitly declared constructor, so a `default`-filled element of a [N]cell
// literal carries a null backing.
type cell struct {
	Buf [4]uint8
	Tag string
}

// idx is a CONSTANT IDENT rather than a literal, so an indexed literal keyed by it keeps the
// sparse projection: the converter cannot fold the key to an index, and the declared length has
// to arrive as padding.
const idx = 2

// Package-level declarations take the same path as locals.
var pkgEmpty = [8]byte{}
var pkgPartial = [8]byte{1, 2}
var pkgNested = [2][3]uint8{}

func main() {
	// Positional, unnamed: the headline case.
	empty := [8]byte{}
	fmt.Println("empty", len(empty), empty[0], empty[7])

	partial := [8]byte{1, 2}
	fmt.Println("partial", len(partial), partial[0], partial[1], partial[2], partial[7])

	// A FULL literal already had the right length; it must keep working.
	full := [3]byte{1, 2, 3}
	fmt.Println("full", len(full), full[0], full[2])

	// An ellipsis literal's length IS its element count.
	ellipsis := [...]byte{4, 5, 6}
	fmt.Println("ellipsis", len(ellipsis), ellipsis[2])

	// Indexed/keyed elements. The zero-index form is its own case: index 0 used to read as
	// "no constant key" and fell to a projection sized by the literal's extent, not by N.
	keyed := [8]byte{5: 1}
	fmt.Println("keyed", len(keyed), keyed[5], keyed[0], keyed[7])

	keyedZero := [8]byte{0: 9}
	fmt.Println("keyedZero", len(keyedZero), keyedZero[0], keyedZero[7])

	// Named and aliased array types wrap the same projection.
	fmt.Println("named empty", len(named{}))
	np := named{1, 2}
	fmt.Println("named partial", len(np), np[0], np[5])

	fmt.Println("alias empty", len(aliased{}))
	ap := aliased{9}
	fmt.Println("alias partial", len(ap), ap[0], ap[4])

	// Package-level.
	fmt.Println("pkg", len(pkgEmpty), len(pkgPartial), pkgPartial[1], pkgPartial[7])

	// Non-byte element types take the same path.
	ints := [4]int{7}
	fmt.Println("ints", len(ints), ints[0], ints[3])

	strs := [3]string{"a"}
	fmt.Println("strs", len(strs), strs[0], strs[2] == "")

	// CONTROL: a slice literal is exactly as long as its elements — it must NOT be padded.
	fmt.Println("slice ctl", len([]byte{}), len([]byte{1, 2}))

	// Writing into the zero-filled tail proves the backing is really N long.
	w := [8]byte{1}
	w[7] = 42
	fmt.Println("write", len(w), w[0], w[6], w[7])

	// NESTED elements — the same defect one level down, and it survived the fix above because
	// padding the OUTER dimension fills with the C# element type's `default`, which for an unnamed
	// inner array is a null backing. `[2][3]uint8{}` then reported an inner length of 0 where Go
	// says 3, while the DECLARED form `var x [2][3]uint8` (which routes through the zero-value
	// construction ladder rather than through this projection) was correct all along. The two
	// spellings of one type disagreeing is how it surfaced, through reflect.
	nestedEmpty := [2][3]uint8{}
	fmt.Println("nested empty", len(nestedEmpty), len(nestedEmpty[0]), len(nestedEmpty[1]))
	nestedEmpty[1][2] = 7
	fmt.Println("nested empty write", nestedEmpty[1][2], nestedEmpty[0][2])

	// A PARTIAL nested literal pads only the tail, so element 0 is written and element 1 is the
	// factory's — both must be 3 long.
	nestedPartial := [2][3]uint8{{1, 2, 3}}
	fmt.Println("nested partial", len(nestedPartial), len(nestedPartial[0]), len(nestedPartial[1]), nestedPartial[0][1], nestedPartial[1][1])

	// A FULL nested literal never pads; it is the control for the nested case.
	nestedFull := [2][3]uint8{{1, 2, 3}, {4, 5, 6}}
	fmt.Println("nested full", len(nestedFull), len(nestedFull[1]), nestedFull[1][2])

	// Three deep, so the element factory has to recurse rather than answer one level.
	deep := [2][3][4]uint8{}
	fmt.Println("deep", len(deep), len(deep[1]), len(deep[1][2]))
	deep[1][2][3] = 9
	fmt.Println("deep write", deep[1][2][3])

	// A NAMED array element is the counter-case: its generated wrapper allocates its own backing
	// from its own known size, so `default` is already usable storage and no factory is owed.
	namedElems := [2]named{}
	fmt.Println("named elems", len(namedElems), len(namedElems[0]), len(namedElems[1]))

	// A struct element whose zero value needs construction — the same class as the nested array,
	// reached through a field rather than through another dimension.
	cells := [2]cell{}
	fmt.Println("cells", len(cells), len(cells[0].Buf), len(cells[1].Buf))
	cells[1].Buf[3] = 5
	fmt.Println("cells write", cells[1].Buf[3], cells[0].Buf[3], cells[0].Tag == "")

	// INDEXED nested literals reach the padding through the other route, and a needy element's
	// zero values there are GAPS rather than a tail — every index the literal did not set has to be
	// constructed, wherever it sits.
	keyedNested := [4][3]uint8{1: {1, 2, 3}}
	fmt.Println("keyed nested", len(keyedNested), len(keyedNested[0]), len(keyedNested[1]), len(keyedNested[3]), keyedNested[1][2])

	// …and with a CONSTANT-IDENT key, which the converter cannot fold to an index, so the literal
	// keeps its sparse projection and the declared length arrives as padding.
	sparseNested := [4][3]uint8{idx: {4, 5, 6}}
	fmt.Println("sparse nested", len(sparseNested), len(sparseNested[0]), len(sparseNested[idx]), len(sparseNested[3]), sparseNested[idx][1])
	sparseNested[3][2] = 8
	fmt.Println("sparse nested write", sparseNested[3][2], sparseNested[0][2])

	// Package-level nested declarations take the same path as locals.
	fmt.Println("pkg nested", len(pkgNested), len(pkgNested[0]), len(pkgNested[1]))
}
