package main

import (
	"errors"
	"fmt"
	"reflect"
)

// A NAMED string is a distinct type whose underlying kind is String. reflect.Value.Len must
// see through the wrapper to the string it wraps — IsZero's String arm is `Len() == 0`, so a
// wrapper-blind Len reports every named string EMPTY, which is silently wrong rather than a
// fault (encoding/gob then skips the field entirely and the decoder leaves the zero value).
type NS string

// A named ARRAY and a named SLICE, to keep the wrapper unwrap honest across container kinds.
type NA [2]uint8
type NB []byte
type NI int

type inner struct {
	S NS
	N int
}

type outer struct {
	I  inner
	A  NA
	P  *int
	Sl []int
	M  map[string]int
	E  error // an INTERFACE field: Go's IsZero reads it as nilness, not as its dynamic value
}

func z(label string, v any) {
	rv := reflect.ValueOf(v)
	fmt.Printf("%-22s kind=%-9v IsZero=%-5v", label, rv.Kind(), rv.IsZero())
	switch rv.Kind() {
	case reflect.String, reflect.Slice, reflect.Array, reflect.Map:
		fmt.Printf(" Len=%d", rv.Len())
	}
	fmt.Println()
}

func main() {
	// Scalars and their named forms.
	z("string non-empty", "val")
	z("string empty", "")
	z("NS non-empty", NS("val"))
	z("NS empty", NS(""))
	z("NI non-zero", NI(3))
	z("NI zero", NI(0))

	// Arrays: raw and named, zero and non-zero.
	z("[2]uint8{1,2}", [2]uint8{1, 2})
	z("[2]uint8{}", [2]uint8{})
	z("NA{1,2}", NA{1, 2})
	z("NA{}", NA{})
	z("[3]string{'','',''}", [3]string{})
	z("[2]NS{'a',''}", [2]NS{NS("a"), NS("")})

	// Slices / maps / pointers — nil vs empty-but-non-nil (Go: IsZero is nilness).
	z("NB(nil)", NB(nil))
	z("NB{}", NB{})
	z("[]byte(nil)", []byte(nil))
	z("[]byte{0}", []byte{0})
	var nilMap map[string]int
	z("map nil", nilMap)
	z("map empty", map[string]int{})
	z("*int nil", (*int)(nil))

	// Structs: a zero struct, and one made non-zero only through a NAMED string field —
	// the case a wrapper-blind Len turns into a false "zero".
	z("inner{}", inner{})
	z("inner{S:val}", inner{S: NS("val")})
	z("inner{N:1}", inner{N: 1})
	z("outer{}", outer{})
	z("outer{A:NA{0,1}}", outer{A: NA{0, 1}})
	z("outer{I.S:x}", outer{I: inner{S: NS("x")}})
	n := 0
	z("outer{P:&n}", outer{P: &n})
	z("outer{Sl:[]}", outer{Sl: []int{}})

	// A value reached through an interface keeps its dynamic type's zeroness.
	fmt.Println("any(NS(\"\")) IsZero:", reflect.ValueOf(any(NS(""))).IsZero())
	fmt.Println("any(NS(\"v\")) IsZero:", reflect.ValueOf(any(NS("v"))).IsZero())

	// An INTERFACE-typed FIELD is zero by NILNESS — including when it holds a value that is
	// itself the zero of its dynamic type. This is the arm the struct walk reaches but a
	// top-level ValueOf never does (ValueOf unwraps to the dynamic type).
	z("outer{E:nil}", outer{})
	z("outer{E:err}", outer{E: errors.New("x")})
	ev := reflect.ValueOf(&outer{}).Elem().FieldByName("E")
	fmt.Println("field E kind:", ev.Kind(), "IsZero:", ev.IsZero())
	ev2 := reflect.ValueOf(&outer{E: errors.New("x")}).Elem().FieldByName("E")
	fmt.Println("field E set kind:", ev2.Kind(), "IsZero:", ev2.IsZero())

	// Grow: reflect.Value.Grow(1) on an addressable slice must extend capacity in place,
	// which is how encoding/gob's decUint8Slice / decodeArrayHelper allocate incrementally.
	s := []byte{1, 2, 3, 4}
	rv := reflect.ValueOf(&s).Elem()
	rv.Grow(1)
	fmt.Println("Grow(1) len/cap:", rv.Len(), rv.Cap() >= 5, len(s), cap(s) >= 5)

	var empty []byte
	re := reflect.ValueOf(&empty).Elem()
	re.Grow(3)
	fmt.Println("Grow(3) from nil:", re.Len(), re.Cap() >= 3, empty == nil)

	// Growing then appending through reflect keeps the caller's slice header in step.
	big := make([]int, 2, 2)
	rb := reflect.ValueOf(&big).Elem()
	rb.Grow(8)
	rb.SetLen(3)
	rb.Index(2).SetInt(99)
	fmt.Println("Grow+SetLen:", len(big), big[2], cap(big) >= 10)

	// Grow(0) is a no-op that must not reallocate away the caller's contents.
	keep := []int{7, 8}
	rk := reflect.ValueOf(&keep).Elem()
	rk.Grow(0)
	fmt.Println("Grow(0):", len(keep), keep[0], keep[1])
}
