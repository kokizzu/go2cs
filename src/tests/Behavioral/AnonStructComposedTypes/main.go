package main

import "fmt"

// A slice of POINTER to an anonymous struct — net's `var ipStringTests = []*struct{…}{…}`.
// The struct sits two composition levels down (slice -> pointer), which a one-level probe of
// the declared type cannot see: the declaration then emitted the raw Go `struct{…}` text into
// the C# type and the whole file failed to parse (CS1031). The embedded interface field is
// part of the shape — it is what makes this signature unique, so nothing else in the package
// happens to have registered a lifted name for it.
var ptrElems = []*struct {
	in  int
	str string
	error
}{
	{1, "one", nil},
	{2, "two", nil},
}

// A map whose VALUE is a pointer to an anonymous struct.
var mapPtrValues = map[string]*struct {
	n int
}{
	"a": {10},
	"b": {20},
}

// Composition with no pointer at all: a slice of a slice of an anonymous struct.
var nested = [][]struct {
	tag string
}{
	{{"x"}, {"y"}},
}

func main() {
	for _, e := range ptrElems {
		fmt.Println(e.in, e.str, e.error == nil)
	}

	fmt.Println(mapPtrValues["a"].n + mapPtrValues["b"].n)
	fmt.Println(nested[0][0].tag, nested[0][1].tag)

	// A write through the pointer element must be visible in the slice.
	ptrElems[0].in = 42
	fmt.Println(ptrElems[0].in)
}
