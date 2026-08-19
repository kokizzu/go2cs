package main

// reflect's WINDOW operations over a string, and the three-index Slice3 over every kind that
// has one.
//
// Go's Value.Index and Value.Slice both accept a STRING — Index yields the i'th byte as a uint8
// Value, Slice yields a string of the receiver's OWN type, so a named string stays named. The
// bridge answered neither: a string Value has no element type, so both fell through to
// "reflect: call of reflect.Value.Index on string Value" — Go's message for a kind that does not
// support the operation AT ALL. text/template's `index` and `slice` builtins are the measured
// consumers ({{index `x` 0}}, {{slice .S 1 2}}), and every one of them errored.
//
// Slice3 was worse and quieter: it stayed on the auto conversion, which reinterprets the Value's
// never-populated ptr slot as a raw unsafeheader.Slice and edits Data/Len/Cap in place. That
// dereferenced nil outright, so a three-index template slice surfaced as "invalid memory address
// or nil pointer dereference" rather than as a missing feature.

import (
	"fmt"
	"reflect"
)

type named string

type box struct {
	S named
	B []byte
}

func show(label string, v reflect.Value) {
	fmt.Printf("%s: kind=%v type=%v len=%d val=%v\n", label, v.Kind(), v.Type(), v.Len(), v.Interface())
}

func main() {
	// 1. Index on a plain string yields the i'th BYTE as a uint8 Value — not a rune, and not
	//    addressable (Go's strings are immutable).
	s := reflect.ValueOf("héllo")
	for i := 0; i < s.Len(); i++ {
		e := s.Index(i)
		fmt.Printf("index %d: kind=%v type=%v val=%v canAddr=%v canSet=%v\n",
			i, e.Kind(), e.Type(), e.Interface(), e.CanAddr(), e.CanSet())
	}

	// 2. Slice on a string keeps the receiver's OWN type, so a NAMED string stays named. The
	//    struct field is what makes the Value's static type the named one.
	b := box{S: "abcdef", B: []byte("wxyz")}
	rb := reflect.ValueOf(b)
	sv := rb.Field(0)
	show("named whole", sv)
	show("named [2:5]", sv.Slice(2, 5))
	show("named [:0]", sv.Slice(0, 0))
	show("named [6:6]", sv.Slice(6, 6))

	plain := reflect.ValueOf("abcdef")
	show("plain [1:4]", plain.Slice(1, 4))

	// 3. Slice3 over a SLICE: the third index bounds the result's capacity, which can end before
	//    the shared backing store does.
	full := []int{0, 1, 2, 3, 4, 5, 6, 7}
	rf := reflect.ValueOf(full)
	t3 := rf.Slice3(2, 5, 6)
	fmt.Printf("slice3 slice: len=%d cap=%d val=%v\n", t3.Len(), t3.Cap(), t3.Interface())

	// The window SHARES the backing store — a write through it is a write the parent sees.
	t3.Index(0).SetInt(99)
	fmt.Printf("slice3 aliases parent: %v\n", full)

	// 4. Slice3 over an addressable ARRAY, where the capacity bound is the array's length.
	arr := [6]int{10, 11, 12, 13, 14, 15}
	ra := reflect.ValueOf(&arr).Elem()
	a3 := ra.Slice3(1, 3, 5)
	fmt.Printf("slice3 array: len=%d cap=%d val=%v\n", a3.Len(), a3.Cap(), a3.Interface())

	// 5. Byte slices still take the two-index form and still alias — the control that the string
	//    arms did not disturb the container path.
	bs := rb.Field(1)
	fmt.Printf("bytes [1:3]: %v\n", bs.Slice(1, 3).Interface())

	// 6. The out-of-range and wrong-kind arms report Go's own messages.
	fmt.Println("panics:", probe(func() { s.Index(99) }), probe(func() { plain.Slice(4, 2) }),
		probe(func() { reflect.ValueOf(42).Index(0) }), probe(func() { plain.Slice3(0, 1, 2) }),
		probe(func() { rf.Slice3(0, 2, 99) }))
}

func probe(f func()) (msg string) {
	defer func() {
		if r := recover(); r != nil {
			msg = fmt.Sprint(r)
		}
	}()
	f()
	return "no panic"
}
