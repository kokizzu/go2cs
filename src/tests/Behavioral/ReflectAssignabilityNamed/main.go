// Positive control for the unwrap-arm classifier: Go's OWN answers for the pairs the census
// must classify. A predeclared type (string, int) is NAMED by the spec, so `type S string` into a
// `string` slot is two DIFFERENT named types and Go REFUSES it; `[]byte` into `type B []byte` has
// an unnamed side and Go ALLOWS it.
package main

import (
	"fmt"
	"reflect"
)

type S string
type B []byte

func try(label string, set func()) {
	defer func() {
		if r := recover(); r != nil {
			fmt.Printf("%s: PANIC %v\n", label, r)
			return
		}
		fmt.Printf("%s: ok\n", label)
	}()
	set()
}

func main() {
	// WRONG-if-admitted: type S string -> string (two different NAMED types)
	var dstString string
	try("S->string (must PANIC)", func() {
		reflect.ValueOf(&dstString).Elem().Set(reflect.ValueOf(S("x")))
	})

	// CORRECT: []byte -> type B []byte (unnamed source side)
	var dstB B
	try("[]byte->B (must be ok)", func() {
		reflect.ValueOf(&dstB).Elem().Set(reflect.ValueOf([]byte("y")))
	})

	// CORRECT: type B []byte -> []byte (unnamed destination side)
	var dstBytes []byte
	try("B->[]byte (must be ok)", func() {
		reflect.ValueOf(&dstBytes).Elem().Set(reflect.ValueOf(B("z")))
	})

	// WRONG-if-admitted: string -> type S string is ALSO named-vs-named? No: S is named, string is
	// named, so Go refuses this direction too.
	var dstS S
	try("string->S (must PANIC)", func() {
		reflect.ValueOf(&dstS).Elem().Set(reflect.ValueOf("w"))
	})

	// the assignability rule as reflect reports it, for the same four pairs
	st := reflect.TypeOf("")
	St := reflect.TypeOf(S(""))
	bt := reflect.TypeOf([]byte(nil))
	Bt := reflect.TypeOf(B(nil))
	fmt.Println("S->string:", St.AssignableTo(st), "| string->S:", st.AssignableTo(St))
	fmt.Println("[]byte->B:", bt.AssignableTo(Bt), "| B->[]byte:", Bt.AssignableTo(bt))
}
