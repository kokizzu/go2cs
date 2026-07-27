package main

import "fmt"

// A named fixed-array type: its generated wrapper allocates its backing lazily, so its
// zero value already self-heals. Covered here to keep that working.
type row [4]byte

// A struct whose own zero value is broken by a fixed-array field: `default(inner)` skips
// the field's `= new(3)` initializer, so an ELEMENT of [2]inner needs real construction.
type inner struct {
	b [3]byte
}

type holder struct {
	grid [2][3]byte
}

var gNested [2][4]byte
var gDeep [2][3][4]byte
var gStructElem [2]inner

func main() {
	// Local nested array: the outer `new(2)` leaves every inner array default (len 0).
	var x [2][4]byte
	fmt.Println(len(x), len(x[0]), len(x[1]))

	// Inner storage must be real, not a shared/absent backing.
	x[1][2] = 7
	x[0][3] = 9
	fmt.Println(x[1][2], x[0][3], x[0][2])

	// Three levels deep.
	var deep [2][3][4]byte
	fmt.Println(len(deep), len(deep[1]), len(deep[1][2]))
	deep[1][2][3] = 5
	fmt.Println(deep[1][2][3], deep[0][0][0])

	// Nested array as a struct FIELD.
	var h holder
	fmt.Println(len(h.grid), len(h.grid[1]))
	h.grid[1][2] = 4
	fmt.Println(h.grid[1][2], h.grid[0][2])

	// Array whose ELEMENT is a struct needing construction.
	var se [2]inner
	fmt.Println(len(se), len(se[1].b))
	se[1].b[2] = 3
	fmt.Println(se[1].b[2], se[0].b[2])

	// Array of a NAMED fixed-array type.
	var nr [2]row
	fmt.Println(len(nr), len(nr[1]))
	nr[1][3] = 6
	fmt.Println(nr[1][3], nr[0][3])

	// Globals take a separate emission path from locals.
	fmt.Println(len(gNested), len(gNested[1]))
	gNested[1][2] = 8
	fmt.Println(gNested[1][2], gNested[0][2])

	fmt.Println(len(gDeep), len(gDeep[1]), len(gDeep[1][2]))
	fmt.Println(len(gStructElem), len(gStructElem[1].b))

	// Each element must be its OWN storage, not one shared inner array.
	var share [3][2]byte
	share[0][0] = 1
	share[1][0] = 2
	share[2][0] = 3
	fmt.Println(share[0][0], share[1][0], share[2][0])

	// An ADDRESS-TAKEN nested local takes a THIRD emission path — the heap-boxed
	// declaration (`ref var x = ref heap(new array<...>(N), out var Ꮡx)`) — which is
	// separate from the plain local and the global above and had no element factory:
	// every inner array stayed len 0 and the first indexed write panicked
	// (compress/flate's `leafCounts [16][16]int32` in bitCounts).
	var boxed [3][4]int32
	pb := &boxed
	fmt.Println(len(pb), len(pb[1]))
	pb[1][2] = 11
	pb[2][3] = 12
	fmt.Println(pb[1][2], pb[2][3], pb[0][2])

	// Slicing an element of the boxed array must see real backing too.
	pb[0][0] = 1
	pb[0][1] = 2
	copy(pb[2][:2], pb[0][:2])
	fmt.Println(pb[2][0], pb[2][1])

	// Boxed array whose ELEMENT is a struct needing construction.
	var boxedStruct [2]inner
	ps := &boxedStruct
	fmt.Println(len(ps), len(ps[1].b))
	ps[1].b[2] = 13
	fmt.Println(ps[1].b[2], ps[0].b[2])

	// `new([N]T)` is a FOURTH path: the builtin built the zero value through the
	// parameterless constructor, where the managed array carries no length at all, so the
	// box came back length 0 (compress/flate's `f.bits = new([maxNumLit+maxNumDist]int)`).
	np := new([12]int)
	fmt.Println(len(np), len(*np))
	np[3] = 14
	fmt.Println(np[3], np[0])

	// Nested, and with a struct element needing construction.
	nq := new([2][3]int)
	nq[1][2] = 15
	fmt.Println(len(nq), len(nq[0]), nq[1][2], nq[0][2])

	ns := new([2]inner)
	ns[1].b[2] = 16
	fmt.Println(len(ns), len(ns[1].b), ns[1].b[2], ns[0].b[2])

	// A NAMED array element type keeps the zero-value form (its wrapper self-allocates).
	nr2 := new(row)
	nr2[3] = 17
	fmt.Println(len(nr2), nr2[3], nr2[0])

	// `make([]E, n)` is a FIFTH path: the slice constructor fills its backing with the
	// element's default too, so a fixed-array element came back length 0 and the first
	// `grid[i][j] +=` panicked (hash/maphash's avalancheTest1; image/draw's Floyd-Steinberg
	// quantError rows and x/text/transform's chain buffers carry the same latent defect).
	grid := make([][4]int32, 3)
	fmt.Println(len(grid), len(grid[0]), len(grid[2]))
	grid[1][2] = 18
	grid[2][3] = 19
	fmt.Println(grid[1][2], grid[2][3], grid[0][2])

	// The CAPACITY form fills the whole backing, as Go's zeroing does, so the elements a
	// re-slice exposes beyond the length are already real storage.
	capped := make([][2]int, 1, 3)
	capped = capped[:3]
	capped[2][1] = 20
	fmt.Println(len(capped), cap(capped), len(capped[2]), capped[2][1], capped[0][1])

	// A struct element needing construction, and a NAMED array element that self-allocates.
	ms := make([]inner, 2)
	ms[1].b[2] = 21
	fmt.Println(len(ms), len(ms[1].b), ms[1].b[2], ms[0].b[2])

	mr := make([]row, 2)
	mr[1][3] = 22
	fmt.Println(len(mr), len(mr[1]), mr[1][3], mr[0][3])
}
