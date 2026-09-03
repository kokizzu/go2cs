// CanonicalTypeIdentity is the ACCEPTANCE guard for descriptor-cargo increment B: type IDENTITY, not
// names. A name guard would pass a repair that split one canonical type in two; this one asserts the
// relations Go guarantees between the CONSTRUCTED route (SliceOf/ChanOf/MapOf/PointerTo/ArrayOf) and
// the DECLARED route (TypeOf of a value), and the one consumer measured as the collapse's victim.
//
// Every literal here is NON-EMPTY on purpose: increment B seeds a value-site descriptor by measuring
// a PRESENT element or key, and states the empty container as its boundary (increment C carries the
// cargo on the value itself). The channel VALUE row lives in ChanElemDims for the same reason.
package main

import (
	"fmt"
	"reflect"
)

func main() {
	sixes := [][6]uint8{{}}
	eights := [][8]uint8{{}}
	t6 := reflect.TypeOf(sixes)

	fmt.Println("1 SliceOf(ArrayOf(6,uint8)) == TypeOf([][6]uint8):", reflect.SliceOf(reflect.ArrayOf(6, reflect.TypeOf(uint8(0)))) == t6)
	fmt.Println("2 TypeOf([][6]uint8) != TypeOf([][8]uint8):        ", t6 != reflect.TypeOf(eights))
	fmt.Println("3 PointerTo(ArrayOf(6,uint8)) == TypeOf(&[6]uint8):", reflect.PointerTo(reflect.ArrayOf(6, reflect.TypeOf(uint8(0)))) == reflect.TypeOf(&[6]uint8{}))

	m := map[[2]int]int{{}: 0}
	fmt.Println("4 MapOf(ArrayOf(2,int),int) == TypeOf(map[[2]int]int):", reflect.MapOf(reflect.ArrayOf(2, reflect.TypeOf(0)), reflect.TypeOf(0)) == reflect.TypeOf(m))
	fmt.Println("5 TypeOf(map[[2]int]int).Key().Len() == 2:        ", reflect.TypeOf(m).Key().Len() == 2)

	me := map[string][3]int{"": {}}
	fmt.Println("6 MapOf(string,ArrayOf(3,int)) == TypeOf(map[string][3]int):", reflect.MapOf(reflect.TypeOf(""), reflect.ArrayOf(3, reflect.TypeOf(0))) == reflect.TypeOf(me))

	// Populated inner arrays: a ZERO [2][3]int element is emitted with inner arrays of length 0 (a
	// converter composite-literal gap, routed), and this guard measures reflect, not that emission.
	nested := [][2][3]int{{{1, 2, 3}, {4, 5, 6}}}
	fmt.Println("7 TypeOf([][2][3]int).Elem().Elem().Len() == 3:   ", reflect.TypeOf(nested).Elem().Elem().Len() == 3)

	// Explicit &: the elided-& form `[]*[4]byte{{}}` trips a converter gap (CS0144 on the emitted
	// literal), routed separately; the guard measures reflect, not that emission.
	ptrs := []*[4]byte{&[4]byte{}}
	fmt.Println("8 TypeOf([]*[4]byte).Elem().Elem().Len() == 4:    ", reflect.TypeOf(ptrs).Elem().Elem().Len() == 4)

	fmt.Println("9 DeepEqual([][6]uint8, [][8]uint8) == false:     ", !reflect.DeepEqual(sixes, eights))
}
