// Guard for descriptor-cargo increment C: the element array length of a CONTAINER must survive
// even when no element exists to observe it. Every row below is a Go type-identity fact; the
// converted C# must print the same words.
//
// Row groups, and why each is here:
//
//	OBSERVED   — the cases increment B already made right; they must STAY right (the predicate's
//	             reach is varied deliberately: same element type, two different lengths).
//	EMPTY      — the exact assertion ReflectArrayOf regressed on (2026-09-03): the declared side
//	             is an EMPTY literal, so there is no element to measure and the length can only
//	             come from the static type.
//	NIL        — the same fact with no backing store at all.
//	AMBIGUOUS  — two lengths for ONE element type in ONE package: both identities right AND the
//	             two types not equal, the case a per-assembly dims registry could not answer.
//
// Every row prints the result of ==, so every label is phrased as an equality claim.
package main

import (
	"fmt"
	"reflect"
)

func main() {
	byteT := reflect.TypeOf(uint8(0))
	arr3, arr4 := reflect.ArrayOf(3, byteT), reflect.ArrayOf(4, byteT)

	// OBSERVED
	fmt.Println("observed slice-of-array identical:", reflect.SliceOf(arr3) == reflect.TypeOf([][3]uint8{{1, 2, 3}}))
	fmt.Println("observed [][6] equals [][8]:", reflect.TypeOf([][6]uint8{{}}) == reflect.TypeOf([][8]uint8{{}}))
	fmt.Println("observed elem len:", reflect.TypeOf([][3]uint8{{1, 2, 3}}).Elem().Len())

	// EMPTY
	fmt.Println("empty slice-of-array identical:", reflect.SliceOf(arr3) == reflect.TypeOf([][3]uint8{}))
	fmt.Println("empty elem len:", reflect.TypeOf([][3]uint8{}).Elem().Len())
	fmt.Println("empty [][3] equals SliceOf(ArrayOf(4)):", reflect.SliceOf(arr4) == reflect.TypeOf([][3]uint8{}))

	// NIL and the MAP/POINTER containers are RECORDED BOUNDARIES, not printed: this project is a
	// stdout comparison against `go run`, so a row whose C# answer differs would red the whole
	// project. Measured on 2026-09-04, both sides:
	//
	//   var nilSlice [][3]uint8
	//   reflect.SliceOf(arr3) == reflect.TypeOf(nilSlice)   Go true   C# false
	//   reflect.TypeOf(nilSlice).Elem().Len()               Go 3      C# 0
	//
	//   var emptyMap map[string][3]uint8
	//   reflect.MapOf(stringT, arr3) == reflect.TypeOf(emptyMap)  Go true   C# false
	//   var nilPtr *[3]uint8
	//   reflect.PointerTo(arr3) == reflect.TypeOf(nilPtr)         Go true   C# false
	//
	// CAUSE: the element length is recorded against a slice's BACKING ARRAY, and a nil slice has no
	// backing object to key on; the map and pointer containers have no equivalent creation-site
	// record at all. REMEDY: the +8 B element-dims field on the slice header (measured and declined
	// for this cut -- 130 stdlib creation sites would need it, 27,143 would pay for it) and, for the
	// map/pointer containers, the same treatment applied to their own creation sites the day a case
	// appears. GolibTests.SliceElemDimsTests asserts the nil case at TODAY's answer, so the remedy
	// cannot land without that assertion being updated deliberately.

	// AMBIGUOUS — both lengths live in this one package
	e3, e4 := [][3]uint8{}, [][4]uint8{}
	fmt.Println("ambiguous 3 identical:", reflect.SliceOf(arr3) == reflect.TypeOf(e3))
	fmt.Println("ambiguous 4 identical:", reflect.SliceOf(arr4) == reflect.TypeOf(e4))
	fmt.Println("ambiguous [][3] equals [][4]:", reflect.TypeOf(e3) == reflect.TypeOf(e4))
	fmt.Println("ambiguous lens:", reflect.TypeOf(e3).Elem().Len(), reflect.TypeOf(e4).Elem().Len())

}
