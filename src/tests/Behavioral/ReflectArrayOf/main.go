// ReflectArrayOf guards reflect.ArrayOf -- building an array TYPE at run time, from a length and an
// element type, for a type no declaration in the program ever produced.
//
// The auto conversion of Go's ArrayOf cannot work and does not degrade: before it assembles its
// arrayType record it looks the type up by NAME through typesByString -> typelinks(), the runtime's
// LINKER-BUILT type table, which has no managed form and is a NotImplementedException stub. So every
// call threw, whatever it was asked for (encoding/gob's TestIgnoreDepthLimit is the measured
// consumer, where it reports as an infrastructure error rather than a failure).
//
// Nothing in that record is needed here. golib's array<T> IS the array type, and the one part of a Go
// array type the managed emission cannot hold -- its LENGTH -- is exactly what the reflection
// bridge's dims cargo already carries for every declared array. So ArrayOf composes the SAME
// descriptor a declared [n]T reaches, and the whole guard is that claim: every row below pairs a
// constructed type against the declared one and asserts they are the same reflect.Type by IDENTITY,
// which is what makes Len/Elem/Size/String/New/Zero agree without being asserted one by one.
//
// The dims COMPOSE (this array's length, then whatever the element already carried), which is why the
// nested rows matter: they are the shape gob's depth-limit test builds 101 deep.
package main

import (
	"fmt"
	"reflect"
)

// Celsius is a DEFINED scalar -- ArrayOf over it must name it and size it exactly as a declared
// [2]Celsius does (its managed form is a generated wrapper struct, not a float64).
type Celsius float64

// pair is a struct element, so the composed type's size comes from Go's own field layout.
type pair struct {
	A int32
	B [2]uint8
}

func describe(label string, t reflect.Type) {
	fmt.Printf("%s: %v | kind=%v len=%d elem=%v size=%d align=%d name=%q\n",
		label, t, t.Kind(), t.Len(), t.Elem(), t.Size(), t.Align(), t.Name())
}

func recovered(fn func()) (msg string) {
	defer func() {
		if r := recover(); r != nil {
			msg = fmt.Sprint(r)
		}
	}()
	fn()
	return "no panic"
}

// nest wraps elem in depth successive ArrayOf calls -- encoding/gob's TestIgnoreDepthLimit shape in
// miniature (it goes 101 deep; five proves the composition and keeps the output readable).
func nest(depth int, elem reflect.Type) reflect.Type {
	for i := 0; i < depth; i++ {
		elem = reflect.ArrayOf(2, elem)
	}
	return elem
}

func main() {
	byteT := reflect.TypeOf(uint8(0))

	// The NESTED rows below compare against a declared VARIABLE rather than an empty composite
	// literal, and the difference is not stylistic. go2cs emits `var x [2][3]uint8` as
	// `new(2, () => new(3))` -- the inner dimension is right there in the initializer, which is the
	// source the reflection bridge recovers a declared array's length from. It emits the empty
	// literal `[2][3]uint8{}` as `new array<uint8>[]{}.array(2)`, whose two elements are
	// `default(array<uint8>)`, i.e. length ZERO -- so the inner dimension is dropped and
	// reflect.TypeOf(lit).Elem().Len() answers 0 where Go answers 3. That is a converter EMISSION
	// gap, older than and independent of ArrayOf (it is reachable from any [N][M]T{} literal, with
	// no reflection involved), so it is recorded rather than papered over: the guard asserts against
	// the declared form, which is what "the type a declaration produces" means here. Do not switch
	// these two rows back to literals -- they will fail, and not for a reason about ArrayOf.
	var nestedDecl [2][3]uint8
	var deepDecl [2][2][2][2][2]uint8

	// ---- the round trip: ArrayOf reaches the type a DECLARATION already produces ----
	made := reflect.ArrayOf(3, byteT)
	declared := reflect.TypeOf([3]uint8{})
	describe("ArrayOf(3, uint8)", made)
	describe("declared [3]uint8", declared)
	fmt.Println("identical:", made == declared, "| elem identical:", made.Elem() == byteT,
		"| assignable:", made.AssignableTo(declared), "| comparable:", made.Comparable())

	// ---- the dims COMPOSE: an ArrayOf over an ArrayOf is the declared nested array ----
	outer := reflect.ArrayOf(2, reflect.ArrayOf(3, byteT))
	fmt.Println("nested:", outer, "| identical:", outer == reflect.TypeOf(nestedDecl),
		"| len:", outer.Len(), "| elem:", outer.Elem(), "| elem len:", outer.Elem().Len(), "| size:", outer.Size())

	// ---- a DEFINED element keeps its name and its size ----
	celsius := reflect.ArrayOf(2, reflect.TypeOf(Celsius(0)))
	describe("ArrayOf(2, Celsius)", celsius)
	fmt.Println("celsius identical:", celsius == reflect.TypeOf([2]Celsius{}))

	// ---- a POINTER element carries its own cargo, and an array hands it down unshifted ----
	ptr := reflect.ArrayOf(2, reflect.PointerTo(byteT))
	fmt.Println("pointer elem:", ptr, "| elem:", ptr.Elem(), "| identical:", ptr == reflect.TypeOf([2]*uint8{}))

	// ---- a STRUCT element: the composed size is Go's own field layout ----
	structs := reflect.ArrayOf(2, reflect.TypeOf(pair{}))
	fmt.Println("struct elem:", structs, "| len:", structs.Len(), "| size:", structs.Size(),
		"| identical:", structs == reflect.TypeOf([2]pair{}))

	// ---- zero length is a real Go type, never "length unknown" ----
	empty := reflect.ArrayOf(0, byteT)
	fmt.Println("zero length:", empty, "| len:", empty.Len(), "| size:", empty.Size(),
		"| identical:", empty == reflect.TypeOf([0]uint8{}))

	// ---- the VALUE side: New / Index / Set over a type no declaration produced ----
	v := reflect.New(made).Elem()
	for i := 0; i < v.Len(); i++ {
		v.Index(i).SetUint(uint64(10 * (i + 1)))
	}
	fmt.Printf("new+index: %v | type=%v | asserted=%v\n", v.Interface(), v.Type(), v.Interface().([3]uint8))
	fmt.Printf("zero: %v | %v\n", reflect.Zero(made).Interface(), reflect.Zero(outer).Interface())
	fmt.Println("deepequal:", reflect.DeepEqual(reflect.Zero(made).Interface(), [3]uint8{}))

	// ---- the gob shape: repeated composition ----
	deep := nest(5, byteT)
	fmt.Println("nest(5):", deep, "| size:", deep.Size(), "| len:", deep.Len(),
		"| identical:", deep == reflect.TypeOf(deepDecl))

	// ---- the contract's own panic ----
	fmt.Println("negative length:", recovered(func() { reflect.ArrayOf(-1, byteT) }))
}
