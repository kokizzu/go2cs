package main

import (
	"fmt"
	"reflect"
)

func main() {
	// 1. Two typed nils of DIFFERENT pointer-to-array types are different Go types.
	t0 := reflect.TypeOf((*[0]byte)(nil))
	t3 := reflect.TypeOf((*[3]byte)(nil))
	fmt.Println("distinct types:", t0 != t3)
	fmt.Println("t0:", t0, "t3:", t3)

	// 2. The length is readable THROUGH the nil, both from the type and from the value.
	fmt.Println("t3 elem len:", t3.Elem().Len())
	fmt.Println("value Len of nil *[3]int:", reflect.ValueOf((*[3]int)(nil)).Len())
	fmt.Println("value Cap of nil *[3]int:", reflect.ValueOf((*[3]int)(nil)).Cap())

	// 3. Two typed nils of the SAME pointer-to-array type are still equal.
	var a any = (*[3]byte)(nil)
	var b any = (*[3]byte)(nil)
	fmt.Println("same type equal:", a == b)
	fmt.Println("same type, same reflect.Type:", reflect.TypeOf(a) == reflect.TypeOf(b))

	// 4. A nil pointer to array is a non-nil interface with a pointer dynamic type.
	fmt.Println("boxed nil is non-nil interface:", a != nil)
	fmt.Printf("dynamic type: %T\n", a)

	// 5. The named-array control: this shape already works, and must keep working.
	type named3 [3]byte
	tn := reflect.TypeOf((*named3)(nil))
	fmt.Println("named array elem len:", tn.Elem().Len())
}
