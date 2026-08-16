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

// Assignability adds the clauses identity alone cannot express: a SECOND named type over []byte
// (two named sides, which Go rejects), the struct shapes whose field walk the downcast could not
// perform, and a concrete type that satisfies an interface.
type myOtherBytes []byte

type fieldsA struct {
	B []byte
	M map[string]int
}
type namedFieldsA struct {
	B []byte
	M map[string]int
}
type fieldsWideElem struct {
	B []byte
	M map[string]int64
}
type fieldsRenamed struct {
	B []byte
	N map[string]int
}
type fieldsShort struct {
	B []byte
}
type fieldsTagged struct {
	B []byte         `json:"b"`
	M map[string]int `json:"m"`
}

type speaker struct{}

func (speaker) String() string { return "speaker" }

type mute struct{}

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

	// ==== ASSIGNABILITY — Go's rule, one row per clause of the spec ====
	//
	// `AssignableTo` was hand-owned as identity-on-the-managed-type plus interface-implements until
	// the row below named its consumer: database/sql's TestUserDefinedBytes assigns a driver's
	// []byte into a `type userDefinedBytes []byte`, which Go admits (identical underlying types,
	// one side unnamed) and which the identity rule rejected — so convertAssignRows fell through to
	// its CONVERT arm and handed back a view over the driver's own array instead of a clone.
	//
	// The rule has two gates and both are asserted. `at least one side unnamed` is the reason
	// abi.Type.HasName() had to become truthful first: a synthesized descriptor never carried
	// TFlagNamed, so HasName() was false for EVERY type and the gate would have admitted two
	// DISTINCT named types over one underlying type — which Go rejects.
	other := reflect.TypeOf(myOtherBytes(nil))
	fmt.Println("assign", bytes.AssignableTo(named), named.AssignableTo(bytes))
	fmt.Println("assign", named.AssignableTo(named), bytes.AssignableTo(bytes))
	// Both sides NAMED over one underlying type — Go says NO. This is the row HasName() gates.
	fmt.Println("assign", named.AssignableTo(other), other.AssignableTo(named))
	// Identical KIND, different element — no clause admits it.
	fmt.Println("assign", named.AssignableTo(namedInts), bytes.AssignableTo(ints))
	fmt.Println("assign", named.AssignableTo(namedMap), plain.AssignableTo(named))

	// The INTERFACE clause: a value is assignable to an interface type it implements, whether or
	// not either side is named.
	stringerType := reflect.TypeOf((*fmt.Stringer)(nil)).Elem()
	emptyType := reflect.TypeOf((*any)(nil)).Elem()
	fmt.Println("iface", reflect.TypeOf(speaker{}).AssignableTo(stringerType))
	fmt.Println("iface", reflect.TypeOf(mute{}).AssignableTo(stringerType))
	fmt.Println("iface", named.AssignableTo(emptyType), stringerType.AssignableTo(emptyType))

	// ==== STRUCT identity — the arm whose downcast read ZERO fields ====
	//
	// It reported any two structs of equal field count identical: a silent FALSE POSITIVE, and the
	// one that would have widened the moment AssignableTo started routing through the same walk.
	// Each negative below differs from `structA` in exactly one way the walk must see — a field's
	// TYPE, a field's NAME, the field COUNT — and the positive pair proves the arm did not simply
	// learn to answer false.
	structA := reflect.TypeOf(fieldsA{})
	structWideElem := reflect.TypeOf(fieldsWideElem{})
	structRenamed := reflect.TypeOf(fieldsRenamed{})
	structShort := reflect.TypeOf(fieldsShort{})
	namedStructA := reflect.TypeOf(namedFieldsA{})
	fmt.Println("struct", structA.ConvertibleTo(structA), structA.ConvertibleTo(structWideElem))
	fmt.Println("struct", structA.ConvertibleTo(structRenamed), structA.ConvertibleTo(structShort))
	fmt.Println("struct", structA.AssignableTo(namedStructA), namedStructA.AssignableTo(structA))
	fmt.Println("struct", structWideElem.AssignableTo(namedStructA), structShort.AssignableTo(namedStructA))
	// A struct TAG is ignored by CONVERSION and honored by ASSIGNMENT — the cmpTags flag, the one
	// place the two relations diverge and the reason the field walk must read tags at all. The
	// discriminator has to be an UNNAMED operand, or the two-named-sides gate rejects the pair
	// before any field is looked at: `unnamed -> fieldsA` is assignable and `unnamed -> fieldsTagged`
	// is not, while BOTH are convertible.
	structTagged := reflect.TypeOf(fieldsTagged{})
	unnamedStruct := reflect.TypeOf(struct {
		B []byte
		M map[string]int
	}{})
	fmt.Println("struct", structA.ConvertibleTo(structTagged), structTagged.ConvertibleTo(structA))
	fmt.Println("struct", unnamedStruct.AssignableTo(structA), unnamedStruct.AssignableTo(structTagged))
	fmt.Println("struct", unnamedStruct.ConvertibleTo(structTagged), unnamedStruct.AssignableTo(structShort))

	// ==== FUNC identity — parameter and result shapes, not a downcast in/out count ====
	//
	// Asserted through CONVERSION rather than assignment: a DEFINED func type has no managed
	// identity of its own (a methodless func type renders inline as its base delegate), so the
	// named/unnamed pairs every other kind asserts cannot be produced here — and between two
	// DISTINCT delegates the assignability walk answers at its named/named gate without ever
	// consulting the arm. Conversion has no such gate, so this is the relation that reaches it.
	// The arm read its in/out counts off the same downcast the struct arm used, so it too
	// reported any two funcs identical.
	funcIB := reflect.TypeOf(func(int) bool { return false })
	funcSB := reflect.TypeOf(func(string) bool { return false })
	funcII := reflect.TypeOf(func(int) int { return 0 })
	funcI2B := reflect.TypeOf(func(int, int) bool { return false })
	fmt.Println("func", funcIB.ConvertibleTo(funcIB), funcIB.ConvertibleTo(funcSB))
	fmt.Println("func", funcIB.ConvertibleTo(funcII), funcIB.ConvertibleTo(funcI2B))

	// ==== CHAN identity ====
	//
	// The bridge's ONE representational limit in this family, and it is upstream of the identity
	// walk rather than in it: a Go channel type emits as golib's `channel<T>` whatever its
	// direction, so `<-chan int` and `chan int` are one managed type and reflect.TypeOf over
	// either describes the BIDIRECTIONAL one — which Type.String() has always reported as
	// `chan int`. ChanDir() now answers BothDir to MATCH that descriptor rather than reading a
	// direction out of the memory following it. Only rows the bridge can produce are asserted:
	// a directional operand is not one of them (see ConversionStrategies-Reference.md).
	plainChan := reflect.TypeOf(make(chan int))
	wideChan := reflect.TypeOf(make(chan int64))
	fmt.Println("chan", plainChan.AssignableTo(myChanType), myChanType.AssignableTo(plainChan))
	fmt.Println("chan", wideChan.AssignableTo(myChanType), plainChan.AssignableTo(plainChan))
	fmt.Println("chan", plainChan.ConvertibleTo(myChanType), wideChan.ConvertibleTo(myChanType))
}
