// Guards the reflection bridge's MAP-ENTRY TYPING, and with it the printability of a map that
// carries a NIL key.
//
// Go types every map entry Value by the map's DECLARED key/value type — an interface-keyed map hands
// out Kind Interface keys whatever the dynamic value is, and a nil key is a VALID nil Value of that
// type. The bridge used to infer each entry's type from the boxed object instead, so the nil key
// (which golib keeps in a slot of its own, because Dictionary rejects a null key) came out as the
// INVALID zero Value: no type, Kind Invalid. internal/fmtsort — which is how fmt orders map keys
// before printing — cannot compare that at all: `compare` fell straight through to
// `panic("bad type in compare: " + aType.String())` on a nil type, so printing ANY map with a nil
// key died inside fmt.
//
// Iteration order over a Go map is unspecified, so this test only prints ORDER-INDEPENDENT facts.
package main

import (
	"fmt"
	"reflect"
)

type namedAny map[any]int

func main() {
	// map[any]V: every key is Kind Interface, and exactly one of them is nil.
	anyKeys := map[any]int{nil: 1, "b": 2, 7: 3}
	iface, nils, invalid, sum := 0, 0, 0, 0
	iter := reflect.ValueOf(anyKeys).MapRange()
	for iter.Next() {
		k, v := iter.Key(), iter.Value()
		if !k.IsValid() {
			invalid++
			continue
		}
		if k.Kind() == reflect.Interface {
			iface++
			if k.IsNil() {
				nils++
			}
		}
		if v.Kind() == reflect.Int {
			sum += int(v.Int())
		}
	}
	fmt.Println("any:", iface, nils, invalid, sum, reflect.ValueOf(anyKeys).Len())

	// A NAMED interface-keyed map behaves identically.
	named := namedAny{nil: 4, "b": 5}
	iface, nils, invalid, sum = 0, 0, 0, 0
	iter = reflect.ValueOf(named).MapRange()
	for iter.Next() {
		k, v := iter.Key(), iter.Value()
		if !k.IsValid() {
			invalid++
			continue
		}
		if k.Kind() == reflect.Interface {
			iface++
			if k.IsNil() {
				nils++
			}
		}
		sum += int(v.Int())
	}
	fmt.Println("named:", iface, nils, invalid, sum, reflect.ValueOf(named).Len())

	// map[*int]V: keys are Kind Pointer, and the nil key is a valid nil pointer Value.
	n := 9
	ptrKeys := map[*int]string{nil: "nil", &n: "n"}
	ptrs, pnils, pinvalid := 0, 0, 0
	iter = reflect.ValueOf(ptrKeys).MapRange()
	for iter.Next() {
		k := iter.Key()
		if !k.IsValid() {
			pinvalid++
			continue
		}
		if k.Kind() == reflect.Pointer {
			ptrs++
			if k.IsNil() {
				pnils++
			}
		}
	}
	fmt.Println("ptr:", ptrs, pnils, pinvalid, reflect.ValueOf(ptrKeys).Len())

	// map[error]V: an interface key type again, with a nil entry only.
	errKeys := map[error]int{nil: 6}
	eiface, enils := 0, 0
	iter = reflect.ValueOf(errKeys).MapRange()
	for iter.Next() {
		k := iter.Key()
		if k.IsValid() && k.Kind() == reflect.Interface {
			eiface++
			if k.IsNil() {
				enils++
			}
		}
	}
	fmt.Println("err:", eiface, enils, reflect.ValueOf(errKeys).Len())

	// A CONCRETE key type is unaffected: Kind String keys, Kind Int values, no nils.
	plain := map[string]int{"a": 1, "b": 2}
	strs, snils, psum := 0, 0, 0
	iter = reflect.ValueOf(plain).MapRange()
	for iter.Next() {
		k, v := iter.Key(), iter.Value()
		if k.Kind() == reflect.String {
			strs++
		}
		if k.IsValid() && k.Kind() == reflect.String && k.String() == "" {
			snils++
		}
		psum += int(v.Int())
	}
	fmt.Println("plain:", strs, snils, psum, reflect.ValueOf(plain).Len())

	// Slice-valued entries keep the declared element type (the value side of the same rule).
	slices := map[string][]int{"k": {1, 2, 3}}
	iter = reflect.ValueOf(slices).MapRange()
	for iter.Next() {
		v := iter.Value()
		fmt.Println("slice value:", v.Kind() == reflect.Slice, v.Len(), v.IsNil())
	}

	// A nil VALUE in an interface-valued map is a valid nil Value, not the invalid zero Value.
	anyVals := map[string]any{"k": nil}
	iter = reflect.ValueOf(anyVals).MapRange()
	for iter.Next() {
		v := iter.Value()
		fmt.Println("nil value:", v.IsValid(), v.Kind() == reflect.Interface, v.IsNil())
	}
}
