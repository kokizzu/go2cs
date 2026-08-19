package main

import (
	"fmt"
	"reflect"
	"sync"
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

// A named byte-slice type -- encoding/xml's CharData/Comment shape. The generated wrapper is a
// struct holding a slice<byte>, not a slice<byte> itself.
type charData []byte

// A named slice over a DEFINED byte element, and one over a non-byte element: the same wrapper
// shape reached through two element types the []byte fast path treats differently.
type myByte byte

type myBytes []myByte

type names []string

// A named slice that can hold ITSELF -- the only way a slice heads a reference cycle in Go.
type recur []any

type wrap struct{ m named }

type hooks struct {
	name string
	fill func(int) int
	step func()
}

// A struct holding sync primitives, always reached through a pointer so nothing copies a lock.
type guarded struct {
	mu   sync.Mutex
	rw   sync.RWMutex
	once sync.Once
	n    int
	name string
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

	// A struct holding SYNC primitives. Go compares their state words field by field, so two
	// used-then-released locks are deeply equal to two fresh ones. The managed shims replace those
	// words with a lazily-created backing object — an OPAQUE managed handle, which the reflection
	// bridge's descent rule classifies as a pointer one word wide. There is nothing behind such a
	// handle to descend into, so the walk must STOP at it (as it already does for a nil pointer)
	// rather than trying to read a pointee slot that does not exist.
	g1 := &guarded{n: 1, name: "a"}
	g2 := &guarded{n: 1, name: "a"}
	g1.mu.Lock()
	g1.mu.Unlock()
	g1.rw.RLock()
	g1.rw.RUnlock()
	fmt.Println(reflect.DeepEqual(g1, g2))
	fmt.Println(reflect.DeepEqual(g1, g1))
	fmt.Println(reflect.DeepEqual(g1, &guarded{n: 2, name: "a"}))
	fmt.Println(reflect.DeepEqual(g1, &guarded{n: 1, name: "b"}))
	fmt.Println(reflect.DeepEqual([]*guarded{g1}, []*guarded{g2}))
	fmt.Println(reflect.DeepEqual(map[string]*guarded{"k": g1}, map[string]*guarded{"k": g2}))
	// sync.Once carries REAL Go state (a done flag) alongside its lock, and both models keep it —
	// so stopping at the opaque handle must not make a used Once equal to a fresh one.
	g3 := &guarded{n: 1, name: "a"}
	g3.once.Do(func() {})
	fmt.Println(reflect.DeepEqual(g3, g2))

	// NAMED SLICE types, the slice half of the named-map defect above and with the same signature:
	// the wrapper holds its slice<T> one level down, so the backing-ARRAY probe resolved both sides
	// to null, the "same initial entry of the same underlying array" short-circuit matched them,
	// and two named slices of equal length were reported deeply equal REGARDLESS of content. That
	// is what made encoding/xml's TestCopyTokenCharData/TestCopyTokenComment fail their SECOND
	// assertion: CopyToken really does clone its buffer, but mutating the original still compared
	// equal to the clone, which reads as "uses same buffer".
	data := []byte("same data")
	c1 := charData(data)
	c2 := charData(append([]byte(nil), data...))
	fmt.Println(reflect.DeepEqual(c1, c2))
	data[1] = 'o'
	fmt.Println(reflect.DeepEqual(c1, c2))
	fmt.Println(reflect.DeepEqual(c1, c1))
	fmt.Println(reflect.DeepEqual(charData("ab"), charData("ab")))
	fmt.Println(reflect.DeepEqual(charData("ab"), charData("ac")))
	fmt.Println(reflect.DeepEqual(charData("ab"), charData("abc")))

	// The xml shape verbatim: a named byte slice reached through an INTERFACE, as a map value, and
	// as a slice element -- the wrapper is unwrapped at each of them or at none.
	var tok1 any = charData(data)
	var tok2 any = charData(append([]byte(nil), data...))
	fmt.Println(reflect.DeepEqual(tok1, tok2))
	tok2 = any(charData(data))
	fmt.Println(reflect.DeepEqual(tok1, tok2))
	fmt.Println(reflect.DeepEqual([]charData{charData("ab")}, []charData{charData("ac")}))
	fmt.Println(reflect.DeepEqual(map[string]charData{"k": charData("ab")}, map[string]charData{"k": charData("ac")}))

	// Nil vs empty vs same-identity, on the named type: a nil named slice has a null backing, so
	// the nil/empty rule has to survive the unwrap rather than be short-circuited by it.
	var nilCD charData
	fmt.Println(reflect.DeepEqual(nilCD, nilCD))
	fmt.Println(reflect.DeepEqual(nilCD, charData{}))
	fmt.Println(reflect.DeepEqual(charData{}, charData{}))

	// A named slice over a DEFINED byte element (the []byte fast path reaches it only by aliasing
	// the element storage) and one over a non-byte element (which the fast path never covers) --
	// so the fix cannot be byte-specific.
	fmt.Println(reflect.DeepEqual(myBytes{1, 2}, myBytes{1, 2}))
	fmt.Println(reflect.DeepEqual(myBytes{1, 2}, myBytes{1, 3}))
	fmt.Println(reflect.DeepEqual(names{"a", "b"}, names{"a", "b"}))
	fmt.Println(reflect.DeepEqual(names{"a", "b"}, names{"a", "c"}))

	// The same named slice compared to itself IS deeply equal by the &s[0] identity rule, even
	// when it holds a NaN that is not equal to itself elementwise -- the short-circuit the null
	// backing was firing accidentally has to keep firing where it is genuinely true.
	nanCD := names{"a"}
	fmt.Println(reflect.DeepEqual(nanCD, nanCD))

	// A named slice heading a reference CYCLE. Cycle detection keys on the backing array, so the
	// same null backing left every named slice out of the visited set; this terminates only if the
	// unwrap reaches the real array.
	r1 := make(recur, 1)
	r1[0] = r1
	r2 := make(recur, 1)
	r2[0] = r2
	fmt.Println(reflect.DeepEqual(r1, r2))
	r3 := make(recur, 1)
	r3[0] = "x"
	fmt.Println(reflect.DeepEqual(r1, r3))
}
