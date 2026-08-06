package main

import (
	"fmt"
	"reflect"
)

type point struct {
	x, y int
	tags []string
}

type node struct {
	val  int
	next *node
}

type named map[string]int

type namedAny map[any]int

type namedSlices map[string][]int

type wrap struct{ m named }

type hooks struct {
	name string
	fill func(int) int
	step func()
}

func main() {
	// Slices: elementwise equality, content mismatch, and the []byte fast path.
	fmt.Println(reflect.DeepEqual([]string{"a", "bc"}, []string{"a", "bc"}))
	fmt.Println(reflect.DeepEqual([]string{"a"}, []string{"b"}))
	fmt.Println(reflect.DeepEqual([]int{1, 2, 3}, []int{1, 2, 3}))
	fmt.Println(reflect.DeepEqual([]int{1, 2, 3}, []int{1, 2, 4}))
	fmt.Println(reflect.DeepEqual([][]byte{[]byte("ab"), nil}, [][]byte{[]byte("ab"), nil}))
	fmt.Println(reflect.DeepEqual([][]byte{[]byte("ab")}, [][]byte{[]byte("ac")}))

	// Nil vs empty slices are not deeply equal; nil equals nil.
	fmt.Println(reflect.DeepEqual([]byte(nil), []byte{}))
	fmt.Println(reflect.DeepEqual([]byte{}, []byte{}))
	var nilInts []int
	fmt.Println(reflect.DeepEqual(nilInts, nilInts))
	fmt.Println(reflect.DeepEqual(nilInts, []int{}))

	// The same slice is deeply equal to itself regardless of content (identity
	// short-circuit on &s[0]), while a copy of a NaN is not equal elementwise.
	zero := 0.0
	nan := []float64{zero / zero}
	fmt.Println(reflect.DeepEqual(nan, nan))
	fmt.Println(reflect.DeepEqual(nan, []float64{nan[0]}))

	// Structs: field-by-field, including slice-typed fields.
	p1 := point{1, 2, []string{"n"}}
	p2 := point{1, 2, []string{"n"}}
	p3 := point{1, 3, []string{"n"}}
	fmt.Println(reflect.DeepEqual(p1, p2))
	fmt.Println(reflect.DeepEqual(p1, p3))

	// Maps: order-independent equality, length, missing key, differing value,
	// nil vs empty, and same-map identity.
	m1 := map[string]int{"a": 1, "b": 2}
	m2 := map[string]int{"b": 2, "a": 1}
	fmt.Println(reflect.DeepEqual(m1, m2))
	fmt.Println(reflect.DeepEqual(m1, map[string]int{"a": 1}))
	fmt.Println(reflect.DeepEqual(m1, map[string]int{"a": 1, "c": 2}))
	fmt.Println(reflect.DeepEqual(m1, map[string]int{"a": 1, "b": 3}))
	var nilMap map[string]int
	fmt.Println(reflect.DeepEqual(nilMap, nilMap))
	fmt.Println(reflect.DeepEqual(nilMap, map[string]int{}))
	fmt.Println(reflect.DeepEqual(m1, m1))

	// Pointers: deeply equal referents, same pointer, mutated referent, nils.
	q1 := &point{1, 2, nil}
	q2 := &point{1, 2, nil}
	q3 := q1
	fmt.Println(reflect.DeepEqual(q1, q2))
	fmt.Println(reflect.DeepEqual(q1, q3))
	q2.y = 9
	fmt.Println(reflect.DeepEqual(q1, q2))
	var np1, np2 *point
	fmt.Println(reflect.DeepEqual(np1, np2))
	fmt.Println(reflect.DeepEqual(np1, q1))

	// Self-referential pointer cycles terminate (in-progress checks assumed true).
	a := &node{val: 1}
	a.next = a
	b := &node{val: 1}
	b.next = b
	fmt.Println(reflect.DeepEqual(a, b))
	c := &node{val: 2}
	c.next = c
	fmt.Println(reflect.DeepEqual(a, c))

	// Distinct types are never deeply equal; untyped nils are.
	fmt.Println(reflect.DeepEqual([]int{1}, []string{"1"}))
	fmt.Println(reflect.DeepEqual(nil, nil))

	// NAMED map types: the wrapper holds a map<K,V> struct whose own backing Dictionary is one
	// level deeper, so the backing-store probe used to resolve BOTH sides to null and report every
	// same-length pair "deeply equal" no matter what they contained.
	n1 := named{"a": 1, "b": 2}
	n2 := named{"b": 2, "a": 1}
	n3 := named{"a": 1, "b": 3}
	n4 := named{"a": 1, "c": 2}
	fmt.Println(reflect.DeepEqual(n1, n2))
	fmt.Println(reflect.DeepEqual(n1, n3))
	fmt.Println(reflect.DeepEqual(n1, n4))
	fmt.Println(reflect.DeepEqual(n1, named{"a": 1}))
	fmt.Println(reflect.DeepEqual(n1, n1))
	var nilNamed named
	fmt.Println(reflect.DeepEqual(nilNamed, nilNamed))
	fmt.Println(reflect.DeepEqual(nilNamed, named{}))

	// A named map as a struct field, and a named map of slices (elementwise recursion).
	fmt.Println(reflect.DeepEqual(wrap{n1}, wrap{n2}))
	fmt.Println(reflect.DeepEqual(wrap{n1}, wrap{n3}))
	s1 := namedSlices{"k": {1, 2}}
	s2 := namedSlices{"k": {1, 2}}
	s3 := namedSlices{"k": {1, 3}}
	fmt.Println(reflect.DeepEqual(s1, s2))
	fmt.Println(reflect.DeepEqual(s1, s3))

	// NIL map keys through DeepEqual: the nil entry lives in a slot the backing walk cannot see,
	// on plain and named map types alike.
	k1 := map[any]int{nil: 1, "b": 2}
	k2 := map[any]int{"b": 2, nil: 1}
	k3 := map[any]int{nil: 9, "b": 2}
	k4 := map[any]int{"b": 2, "c": 1}
	fmt.Println(reflect.DeepEqual(k1, k2))
	fmt.Println(reflect.DeepEqual(k1, k3))
	fmt.Println(reflect.DeepEqual(k1, k4))
	j1 := namedAny{nil: 1, "b": 2}
	j2 := namedAny{"b": 2, nil: 1}
	j3 := namedAny{nil: 9, "b": 2}
	j4 := namedAny{"b": 2, "c": 1}
	fmt.Println(reflect.DeepEqual(j1, j2))
	fmt.Println(reflect.DeepEqual(j1, j3))
	fmt.Println(reflect.DeepEqual(j1, j4))

	// FUNC-typed values: "deeply equal if both are nil; otherwise not deeply equal" — including a
	// struct that is compared to ITSELF, which is NOT deeply equal once a func field is non-nil.
	// Nil-ness has to be asked of the value: a nil func reached as a struct FIELD (or a slice
	// element) is typed by its static func type and so is a VALID nil Value, unlike the invalid
	// Value a top-level nil `any` produces. Assuming the latter reported every pair of nil func
	// fields unequal, which failed all ten levels of compress/flate's TestWriterReset.
	h1 := hooks{name: "a"}
	h2 := hooks{name: "a"}
	h3 := hooks{name: "a", step: func() {}}
	fmt.Println(reflect.DeepEqual(h1, h2))
	fmt.Println(reflect.DeepEqual(h1, h3))
	fmt.Println(reflect.DeepEqual(h3, h3))
	fmt.Println(reflect.DeepEqual(hooks{name: "a"}, hooks{name: "b"}))
	fmt.Println(reflect.DeepEqual([]func(){nil, nil}, []func(){nil, nil}))
	fmt.Println(reflect.DeepEqual([]func(){nil}, []func(){func() {}}))
	fmt.Println(reflect.DeepEqual(map[string]func(){"k": nil}, map[string]func(){"k": nil}))
	var nilFn func()
	fmt.Println(reflect.DeepEqual(nilFn, nilFn))
	fmt.Println(reflect.DeepEqual(nilFn, func() {}))
}
