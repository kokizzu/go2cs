// ReflectStructOf guards reflect.StructOf -- building a struct TYPE at run time, from a field list,
// for a type no declaration in the program ever produced.
//
// StructOf is the one run-time type constructor with nothing to compose from. PointerTo and ArrayOf
// hand MakeGenericType an EXISTING managed type, because go2cs's ж<T> and array<T> ARE the Go type;
// a struct has no generic container to instantiate, so a real CLR value type has to be MINTED for
// every synthesized Go struct. The auto conversion dies where ArrayOf's did -- typesByString ->
// typelinks(), the linker-built type table, a NotImplementedException stub -- and everything past
// that lookup is Go's runtime reconstructing linker output, which the managed bridge never lost.
//
// What this guard is FOR, stated plainly, because the obvious proof cannot see the obvious bug:
// encoding/gob's TestIgnoreDepthLimit (the arc's motivating consumer) asserts on the DECODER's error
// over the wire type graph and discards the encoder's, so a StructOf that lost every array DIMENSION
// -- reporting [0]T for every [N]T -- would still turn that row green. Only a comparison against Go
// itself can go red on that, which is what every line below is: the same program run by `go run` and
// by the transpiled C#, compared byte for byte.
//
// So the rows cover, in order: identity/interning (the contract encoding/gob keys its type maps on),
// the shape key's separations (a System.Type alone cannot tell [1]int from [2]int), the DIMS round
// trip through Field/Type/Len and through real storage, the value side (New/Field/Set/Interface),
// embeddedness, tags, unexported fields with a PkgPath, Implements answering false without throwing,
// and the contract's own panics.
package main

import (
	"fmt"
	"reflect"
)

type stringer struct{ n int }

func (s stringer) String() string { return fmt.Sprint(s.n) }

func recovered(fn func()) (msg string) {
	defer func() {
		if r := recover(); r != nil {
			msg = fmt.Sprint(r)
		}
	}()
	fn()
	return "no panic"
}

// field is a one-line StructField builder, so the rows below read as the shapes they are.
func field(name string, t reflect.Type) reflect.StructField {
	return reflect.StructField{Name: name, Type: t}
}

func main() {
	intT := reflect.TypeOf(int64(0))
	byteT := reflect.TypeOf(uint8(0))
	strT := reflect.TypeOf("")

	// ---- the contract encoding/gob stands on: StructOf is INTERNED ----
	// gob keys `map[reflect.Type]gobType` and `enc.sent map[reflect.Type]typeId` on the result, so a
	// fresh descriptor per call would make every recursion a cache miss and every mutually recursive
	// type an infinite regress. Interning is not an optimization here; it is the contract.
	shape := []reflect.StructField{field("A", intT), field("B", strT)}
	first := reflect.StructOf(shape)
	second := reflect.StructOf([]reflect.StructField{field("A", intT), field("B", strT)})
	fmt.Println("interned:", first == second, "| usable as a map key:",
		map[reflect.Type]int{first: 1}[second] == 1)

	// ---- what the type SAYS about itself: an anonymous struct has no name and renders structurally
	fmt.Printf("type: %v | kind=%v | name=%q | pkgpath=%q | numfield=%d | size=%d | align=%d\n",
		first, first.Kind(), first.Name(), first.PkgPath(), first.NumField(), first.Size(), first.Align())

	// ---- the shape key SEPARATES what one managed type cannot ----
	// [1]int and [2]int are ONE managed array<int64>, and `chan<- T` and `chan T` are one channel<T>:
	// the length and the direction live only as descriptor cargo. So a shape key built from field
	// types alone would intern these pairs TOGETHER and the first to arrive would answer for both.
	one := reflect.StructOf([]reflect.StructField{field("F", reflect.ArrayOf(1, intT))})
	two := reflect.StructOf([]reflect.StructField{field("F", reflect.ArrayOf(2, intT))})
	tagged := reflect.StructOf([]reflect.StructField{{Name: "A", Type: intT, Tag: `json:"a"`}})
	renamed := reflect.StructOf([]reflect.StructField{field("Z", intT), field("B", strT)})
	fmt.Println("by array length:", one != two, "| by tag:", tagged != reflect.StructOf(shape[:1]),
		"| by field name:", renamed != first, "| by field count:", first != reflect.StructOf(shape[:1]))
	fmt.Println("lengths:", one.Field(0).Type.Len(), two.Field(0).Type.Len(),
		"| sizes:", one.Size(), two.Size())

	// ---- the DIMS round trip: through the type, and through real storage ----
	// This is the row the gob test cannot take. A synthesized [3]uint8 field must report length 3
	// from the TYPE and hold three writable elements in a value built by reflect.New.
	dims := reflect.StructOf([]reflect.StructField{
		field("N", intT),
		field("Arr", reflect.ArrayOf(3, byteT)),
		field("Nested", reflect.ArrayOf(2, reflect.ArrayOf(3, byteT))),
	})
	fmt.Println("dims type:", dims, "| size:", dims.Size())
	for i := 0; i < dims.NumField(); i++ {
		f := dims.Field(i)
		fmt.Printf("  field %d: name=%s type=%v offset=%d index=%v anonymous=%v tag=%q pkgpath=%q\n",
			i, f.Name, f.Type, f.Offset, f.Index, f.Anonymous, f.Tag, f.PkgPath)
	}
	fmt.Println("elem lens:", dims.Field(1).Type.Len(), dims.Field(2).Type.Len(),
		dims.Field(2).Type.Elem().Len())

	// ---- the VALUE side: New / Field / Set / Index / Interface over a type nothing declared ----
	v := reflect.New(dims).Elem()
	v.Field(0).SetInt(42)
	for i := 0; i < 3; i++ {
		v.Field(1).Index(i).SetUint(uint64(10 * (i + 1)))
	}
	v.Field(2).Index(1).Index(2).SetUint(9)
	fmt.Printf("value: %v | type=%v | field0=%d | arr2=%d | nested=%d\n",
		v.Interface(), v.Type(), v.Field(0).Int(), v.Field(1).Index(2).Uint(),
		v.Field(2).Index(1).Index(2).Uint())
	fmt.Println("zero:", reflect.Zero(dims).Interface(), "| deepequal to fresh:",
		reflect.DeepEqual(reflect.Zero(dims).Interface(), reflect.New(dims).Elem().Interface()))

	// ---- TAGS reach StructField.Tag and the type's own rendering ----
	tags := reflect.StructOf([]reflect.StructField{
		{Name: "A", Type: intT, Tag: `json:"a,omitempty"`},
		{Name: "B", Type: strT, Tag: `xml:"b"`},
		{Name: "C", Type: byteT},
	})
	fmt.Println("tagged type:", tags)
	fmt.Printf("tags: %q %q %q | json key=%q xml key=%q\n",
		tags.Field(0).Tag, tags.Field(1).Tag, tags.Field(2).Tag,
		tags.Field(0).Tag.Get("json"), tags.Field(1).Tag.Get("xml"))

	// ---- EMBEDDED fields: the assertion is on .Anonymous and on nothing else ----
	// An embedded field and a same-named regular field render IDENTICALLY in String(), so a
	// String()-based check cannot go red when embeddedness is lost -- it would be one more green
	// that proves nothing.
	embedType := reflect.TypeOf(struct{ Q int64 }{})
	embedded := reflect.StructOf([]reflect.StructField{
		{Name: "Celsius", Type: reflect.TypeOf(Celsius(0)), Anonymous: true},
		field("Tail", intT),
	})
	fmt.Println("embedded type:", embedded)
	fmt.Println("embedded flags:", embedded.Field(0).Anonymous, embedded.Field(1).Anonymous,
		"| names:", embedded.Field(0).Name, embedded.Field(1).Name,
		"| embed type:", embedded.Field(0).Type, "| numfield:", embedType.NumField())

	// ---- UNEXPORTED fields carry a PkgPath, which is a property of the FIELD, not of the type ----
	// Go answers "" for an unnamed struct type's own PkgPath while its unexported field still names
	// the declaring package, so the two are read by different rules and are checked as a pair.
	unexported := reflect.StructOf([]reflect.StructField{
		{Name: "Shown", Type: intT},
		{Name: "hidden", Type: intT, PkgPath: "main"},
	})
	fmt.Printf("unexported: %v | field pkgpaths=%q,%q | exported=%v,%v\n", unexported,
		unexported.Field(0).PkgPath, unexported.Field(1).PkgPath,
		unexported.Field(0).IsExported(), unexported.Field(1).IsExported())

	// ---- ASKING about a synthesized type must be safe: it is the first type in the system with no
	// generator-registered method set at all, and gob asks about every type it sees (GobEncoder,
	// BinaryMarshaler, ...) on the way in.
	stringerT := reflect.TypeOf((*fmt.Stringer)(nil)).Elem()
	fmt.Println("implements Stringer:", first.Implements(stringerT),
		"| implements any:", first.Implements(reflect.TypeOf((*interface{})(nil)).Elem()),
		"| comparable:", first.Comparable(),
		"| declared stringer does:", reflect.TypeOf(stringer{}).Implements(stringerT))

	// ---- the gob shape in miniature: repeated composition, five deep instead of a hundred and one
	deep := byteT
	for i := 0; i < 5; i++ {
		deep = reflect.StructOf([]reflect.StructField{field("N", deep)})
	}
	fmt.Println("nested structs:", deep, "| size:", deep.Size())

	// ---- the contract's own panics, with Go's own messages ----
	fmt.Println("no name:", recovered(func() { reflect.StructOf([]reflect.StructField{field("", intT)}) }))
	fmt.Println("invalid name:", recovered(func() { reflect.StructOf([]reflect.StructField{field("1bad", intT)}) }))
	fmt.Println("no type:", recovered(func() { reflect.StructOf([]reflect.StructField{{Name: "A"}}) }))
	fmt.Println("duplicate:", recovered(func() {
		reflect.StructOf([]reflect.StructField{field("A", intT), field("A", strT)})
	}))
	fmt.Println("unexported no pkgpath:", recovered(func() {
		reflect.StructOf([]reflect.StructField{{Name: "lower", Type: intT}})
	}))
	fmt.Println("anonymous with pkgpath:", recovered(func() {
		reflect.StructOf([]reflect.StructField{{Name: "Celsius", Type: reflect.TypeOf(Celsius(0)), Anonymous: true, PkgPath: "main"}})
	}))
}

// Celsius is a DEFINED scalar with no methods -- the embedded-field row needs a named type, and one
// with a method set would hit the documented boundary (Go's own StructOf does not support promoted
// methods of embedded fields either).
type Celsius float64
