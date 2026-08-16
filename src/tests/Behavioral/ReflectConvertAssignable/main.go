package main

import (
	"fmt"
	"reflect"
)

// reflect.Type.ConvertibleTo answers through convertOp -> haveIdenticalUnderlyingType, which
// recurses on the internal/abi descriptor's Elem(), Key() and Len(). Go reaches all three by
// prefix-downcasting the Type header onto the sliceType/arrayType/chanType/mapType/ptrType record
// the linker allocated behind it — an idiom with no managed form, so the bridge must synthesize
// each answer from the descriptor's carried managed type instead. Every named/unnamed pair below
// is one of those recursions.
type myBytes []byte
type myInts []int
type myMap map[string]int
type myWideMap map[string]int64
type myKeyMap map[int]int
type myPtr *int
type myWidePtr *int64
type myChan chan int
type myArray [3]byte
type myWideArray [4]byte

func main() {
	bytes := reflect.TypeOf([]byte(nil))
	named := reflect.TypeOf(myBytes(nil))
	ints := reflect.TypeOf([]int(nil))
	namedInts := reflect.TypeOf(myInts(nil))

	// SLICE — the database/sql TestUserDefinedBytes shape: convertAssign into a
	// `type userDefinedBytes []byte` reaches ConvertibleTo, whose slice arm immediately recurses
	// on Elem(). With Elem() answering nil this did not report false, it PANICKED: nothing tests
	// the accessor's result, so haveIdenticalType recursed into nameFor(nil) and nil-dereferenced.
	fmt.Println("slice", bytes.ConvertibleTo(named), named.ConvertibleTo(bytes))
	fmt.Println("slice", named.ConvertibleTo(named), bytes.ConvertibleTo(bytes))
	fmt.Println("slice", bytes.ConvertibleTo(namedInts), ints.ConvertibleTo(namedInts))
	fmt.Println("slice", named.ConvertibleTo(ints), ints.ConvertibleTo(bytes))

	// MAP — the only kind whose identity recurses on BOTH Key() and Elem(), so a differing key
	// and a differing element have to be distinguishable from an identical pair and from each
	// other.
	var unnamedMap map[string]int
	plain := reflect.TypeOf(unnamedMap)
	namedMap := reflect.TypeOf(myMap(nil))
	wideMap := reflect.TypeOf(myWideMap(nil))
	keyMap := reflect.TypeOf(myKeyMap(nil))
	fmt.Println("map", plain.ConvertibleTo(namedMap), namedMap.ConvertibleTo(plain))
	fmt.Println("map", plain.ConvertibleTo(wideMap), plain.ConvertibleTo(keyMap))
	fmt.Println("map", wideMap.ConvertibleTo(keyMap), namedMap.ConvertibleTo(namedMap))

	// POINTER.
	ptr := reflect.TypeOf((*int)(nil))
	namedPtr := reflect.TypeOf(myPtr(nil))
	widePtr := reflect.TypeOf(myWidePtr(nil))
	fmt.Println("ptr", ptr.ConvertibleTo(namedPtr), namedPtr.ConvertibleTo(ptr))
	fmt.Println("ptr", ptr.ConvertibleTo(widePtr), widePtr.ConvertibleTo(namedPtr))

	// ARRAY — identity compares Len() as well as Elem(), and Len() is the accessor whose downcast
	// failed worst: it read a length out of the memory following the descriptor, so two [3]byte
	// descriptors read two different numbers and compared UNEQUAL. Equal-length and
	// differing-length pairs are both asserted, so a Len() that answered a constant would fail too.
	arr := reflect.TypeOf([3]byte{})
	namedArr := reflect.TypeOf(myArray{})
	wideArr := reflect.TypeOf(myWideArray{})
	fmt.Println("array", arr.ConvertibleTo(namedArr), namedArr.ConvertibleTo(arr))
	fmt.Println("array", arr.ConvertibleTo(wideArr), wideArr.ConvertibleTo(namedArr))
	fmt.Println("array", arr.Len(), namedArr.Len(), wideArr.Len())

	myChanType := reflect.TypeOf(myChan(nil))

	// The element and key kinds the walk lands on, read back through the public Type surface so a
	// synthesized sub-descriptor is proved to carry a real kind rather than nil. The CHAN element
	// is read here even though no chan identity row is asserted below.
	fmt.Println("elem", named.Elem().Kind(), namedMap.Key().Kind(), namedMap.Elem().Kind())
	fmt.Println("elem", namedPtr.Elem().Kind(), myChanType.Elem().Kind(), namedArr.Elem().Kind())

	// Three things are deliberately NOT asserted here, each an open root with its own board entry.
	// Asserting Go's answer for them would pin a row the corpus cannot yet produce; asserting the
	// current answer would pin a defect as if it were a contract.
	//   * AssignableTo — reflect's rtype.AssignableTo is hand-owned as identity-on-the-managed-type
	//     plus interface-implements, with Go's unnamed<->named underlying rule recorded as deferred.
	//   * STRUCT identity — haveIdenticalUnderlyingType's struct arm still downcasts the descriptor
	//     directly instead of going through the synthesized StructType(), so it reads zero fields
	//     and calls any two structs of equal field count identical.
	//   * CHAN identity — its arm compares ChanDir() first, which downcasts the same way. A
	//     direction is not recoverable from the managed type at all for an UNNAMED directional
	//     channel (`<-chan int` and `chan int` are both channel<nint>), so unlike Elem/Key/Len this
	//     one has no synthesis waiting for it — it needs a ruling, not a fix.
}
