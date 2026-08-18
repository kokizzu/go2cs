package main

// Go's TYPED NIL crossing the reflection bridge. A nil *T read out of a slot — a slice
// element, an array element, a struct field, a map value, a func result — is NOT the nil
// interface: reflect.Value.Interface() packs it as a non-nil `any` carrying (type=*T,
// value=nil), so a type assertion to an interface *T implements SUCCEEDS and dispatches the
// method on the nil receiver. Methods written to handle that receiver — the entire reason
// `func (x *Int) GobEncode()` opens with `if x == nil` — then run.
//
// This is encoding/gob's exact shape: math/big's TestGobEncodingNilIntInSlice builds
// `make([]*Int, 1)`, and gob's encode walk reaches `v.Interface().(GobEncoder).GobEncode()`
// for the zero-filled element. Where the typed nil collapses to a plain untyped nil the
// assertion takes the failure arm, the nil-receiver method never runs, and the encode fails
// on a value Go transmits successfully.

import (
	"fmt"
	"reflect"
)

// Encoder mirrors gob.GobEncoder: a POINTER-receiver method that explicitly handles a nil
// receiver rather than dereferencing it.
type Encoder interface {
	Encode() (string, error)
}

type Blob struct {
	Data string
}

func (b *Blob) Encode() (string, error) {
	if b == nil {
		return "<nil blob>", nil
	}
	return b.Data, nil
}

type Tag struct {
	N int
}

func (t *Tag) Encode() (string, error) {
	if t == nil {
		return "<nil tag>", nil
	}
	return fmt.Sprintf("tag:%d", t.N), nil
}

// A holder whose FIELD is a typed nil pointer, so the struct-field slot is exercised too.
type holder struct {
	B *Blob
	T *Tag
}

// report is the single probe every slot shape runs: what Interface() handed back, whether the
// Value itself still reports nil, whether the assertion succeeded, and what the nil receiver
// returned.
func report(label string, v reflect.Value) {
	iface := v.Interface()
	fmt.Printf("%s: kind=%v isNil=%v ifaceIsNil=%v type=%v\n", label, v.Kind(), v.IsNil(), iface == nil, reflect.TypeOf(iface))
	e, ok := iface.(Encoder)
	fmt.Printf("  assert Encoder ok=%v\n", ok)
	if ok {
		out, err := e.Encode()
		fmt.Printf("  encode=%q err=%v\n", out, err)
	}
}

func main() {
	// 1. SLICE element — make() zero-fills with typed nils. gob's proven case.
	s := make([]*Blob, 2)
	s[1] = &Blob{Data: "second"}
	rs := reflect.ValueOf(s)
	for i := 0; i < rs.Len(); i++ {
		report(fmt.Sprintf("slice[%d]", i), rs.Index(i))
	}

	// 2. ARRAY element — the same zero fill through a fixed-extent slot.
	var a [2]*Tag
	a[1] = &Tag{N: 7}
	ra := reflect.ValueOf(a)
	for i := 0; i < ra.Len(); i++ {
		report(fmt.Sprintf("array[%d]", i), ra.Index(i))
	}

	// 3. STRUCT field — a declared *T field left at its zero value.
	h := holder{T: &Tag{N: 42}}
	rh := reflect.ValueOf(h)
	for i := 0; i < rh.NumField(); i++ {
		report("field "+rh.Type().Field(i).Name, rh.Field(i))
	}

	// 4. MAP value — a *T value slot holding nil. Read through MapRange (the bridge's iterator)
	//    and kept to a SINGLE entry so the walk is deterministic in both languages.
	m := map[string]*Blob{"nil": nil}
	it := reflect.ValueOf(m).MapRange()
	for it.Next() {
		report("map["+it.Key().String()+"]", it.Value())
	}

	// 5. reflect.New(...).Elem() — a freshly allocated *T slot, still the zero pointer.
	rn := reflect.New(reflect.TypeOf((*Blob)(nil))).Elem()
	report("new(*Blob).Elem()", rn)

	// 6. The pointer's own identity survives the round trip: a typed nil packed by the bridge
	//    compares equal to the language-level typed nil, and unequal to the nil interface.
	iface := rs.Index(0).Interface()
	fmt.Printf("roundtrip: ==(*Blob)(nil) %v, ==nil %v\n", iface == (*Blob)(nil), iface == nil)

	// 7. Elem() of the typed nil is the INVALID Value (there is nothing to point at), which is
	//    how a walker distinguishes "typed nil" from "pointer to zero value".
	fmt.Printf("elem-of-typed-nil valid=%v\n", rs.Index(0).Elem().IsValid())

	// 8. An INTERFACE-kind Value holding a typed nil pointer: the interface itself is NON-nil,
	//    so IsNil and IsZero answer about the INTERFACE, and only Elem() reaches the pointer's
	//    own nilness. This is encoding/gob's TestNilPointerInsideInterface — with the answer
	//    inverted, `!sendZero && v.IsZero()` skips the field and the "nil pointer inside
	//    interface" error path is never reached.
	var np *Blob
	si := struct{ I any }{I: np}
	f := reflect.ValueOf(si).Field(0)
	fmt.Printf("ifaceField: kind=%v isNil=%v isZero=%v elemKind=%v elemIsNil=%v\n",
		f.Kind(), f.IsNil(), f.IsZero(), f.Elem().Kind(), f.Elem().IsNil())

	// ...and the nil-INTERFACE control, where both answers flip together.
	sn := struct{ I any }{}
	fn := reflect.ValueOf(sn).Field(0)
	fmt.Printf("nilIfaceField: isNil=%v isZero=%v elemValid=%v\n", fn.IsNil(), fn.IsZero(), fn.Elem().IsValid())
}
