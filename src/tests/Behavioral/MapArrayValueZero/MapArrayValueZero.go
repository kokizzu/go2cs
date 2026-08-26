// A map READ that MISSES yields the element type's Go zero value, and for a fixed-size array that
// zero is [N]T with N zeroed elements — not an empty array. `array<T>` carries its Go length in the
// instance, so `default(array<T>)` has LENGTH ZERO, and the first index into a missed entry panicked
// `index out of range [0] with length 0` where Go reads a zero.
//
// The live witness was html's unescapeEntity: `entity2` is `map[string][2]rune` and
// `if x := entity2[string(entityName)]; x[0] != 0` is the NORMAL path for any `&…` run that is not a
// two-rune entity, so `TestUnescape` died on ordinary input (html 2/3).
//
// Every read form is covered here because the shape has to come from the READ SITE — the map cannot
// supply it (a nil map has no entries and no type-level length), so each form needs its own seat:
// plain read, comma-ok read, a NESTED element whose inner lengths must survive too, a read through a
// NAMED map type (the generated wrapper forwards the overload), and both maps that can never have
// the entry — nil and empty.
package main

import "fmt"

var entity2 = map[string][2]rune{
	"NotEqualTilde;": {'≂', '̸'},
}

// Plain read, hit and miss, plus the comma-ok form.
func mapMiss() {
	x := entity2["notthere"]
	fmt.Println("miss:", len(x), x[0], x[1])

	y := entity2["NotEqualTilde;"]
	fmt.Println("hit:", len(y), y[0], y[1])

	z, ok := entity2["alsomissing"]
	fmt.Println("commaok:", ok, len(z), z[0], z[1])

	w, ok2 := entity2["NotEqualTilde;"]
	fmt.Println("commaok hit:", ok2, len(w), w[0], w[1])
}

// A NESTED element: the inner arrays are zero-length too unless the element factory rides along.
var nested = map[string][2][3]int{"a": {{1, 2, 3}, {4, 5, 6}}}

func mapMissNested() {
	n := nested["zzz"]
	fmt.Println("nested miss:", len(n), len(n[0]), len(n[1]), n[0][2], n[1][0])

	h := nested["a"]
	fmt.Println("nested hit:", len(h), len(h[0]), h[0][2], h[1][0])
}

// A NAMED map type reads through the generated wrapper rather than golib's map directly.
type quadMap map[int][4]byte

func namedMap() {
	var nilMap quadMap // nil: no store at all, so the zero can only come from the read site
	v := nilMap[7]
	fmt.Println("nil named:", len(v), v[0], v[3])

	empty := make(quadMap)
	e := empty[9]
	fmt.Println("empty named:", len(e), e[0], e[3])

	empty[9] = [4]byte{'a', 'b', 'c', 'd'}
	got, ok := empty[9]
	fmt.Println("named hit:", ok, len(got), got[0], got[3])
}

// An UNNAMED map local, both nil and empty, to prove the read site (not the construction) carries it.
func plainNilAndEmpty() {
	var nilMap map[string][2]rune
	a := nilMap["x"]
	fmt.Println("nil plain:", len(a), a[0], a[1])

	emptyMap := map[string][2]rune{}
	b := emptyMap["x"]
	fmt.Println("empty plain:", len(b), b[0], b[1])
}

// A STORE is untouched: it has a value and needs no zero.
func storeThenRead() {
	m := make(map[string][2]rune)
	m["k"] = [2]rune{'q', 'r'}
	fmt.Println("stored:", len(m["k"]), m["k"][0], m["k"][1])
	fmt.Println("other:", len(m["other"]), m["other"][0])
}

func main() {
	mapMiss()
	mapMissNested()
	namedMap()
	plainNilAndEmpty()
	storeThenRead()
}
