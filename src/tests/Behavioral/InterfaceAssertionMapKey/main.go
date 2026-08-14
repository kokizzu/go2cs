// An interface value's map-key identity must not depend on the STATIC interface
// type it is currently held in. Go compares interface values by (dynamic type,
// dynamic value), so a value obtained by asserting an `Object` to the narrower
// `dependency` is the same key as the `Object` it came from, and looking it back
// up in the original map must hit.
//
// This is the shape go/types' initorder.dependencyGraph relies on: it builds
// M[dependency] by asserting the keys of objMap[Object], then indexes objMap
// with the asserted value. A missed lookup there returns a nil *declInfo and the
// very next field access nil-panics inside the type checker.
package main

import "fmt"

type Object interface {
	Name() string
}

type dependency interface {
	Object
	isDependency()
}

// pointer-receiver implementor: the dynamic type is *myVar
type myVar struct{ n string }

func (o *myVar) Name() string  { return o.n }
func (o *myVar) isDependency() {}

// value-receiver implementor: the dynamic type is myConst (a struct value)
type myConst struct{ n string }

func (o myConst) Name() string  { return o.n }
func (o myConst) isDependency() {}

// an Object that is NOT a dependency, so the assertion arm is exercised both ways
type myFunc struct{ n string }

func (o *myFunc) Name() string { return o.n }

func main() {
	objMap := make(map[Object]int)
	objMap[&myVar{"var-a"}] = 1
	objMap[&myVar{"var-b"}] = 2
	objMap[myConst{"const-c"}] = 4
	objMap[myConst{"const-d"}] = 8
	objMap[&myFunc{"func-e"}] = 16

	// Narrow each key to `dependency` and use THAT as the key of a second map.
	M := make(map[dependency]bool)
	for obj := range objMap {
		if obj, _ := obj.(dependency); obj != nil {
			M[obj] = true
		}
	}
	fmt.Println("objMap:", len(objMap), "M:", len(M))

	// Index the ORIGINAL map with the narrowed interface value. Every one of
	// these must hit; a miss is what nil-panics the converted type checker.
	// The values are powers of two and the hit count is order-independent, so
	// the output stays deterministic under Go's randomized map iteration.
	hits, sum := 0, 0
	for d := range M {
		v, ok := objMap[d]
		if !ok {
			fmt.Println("MISS:", d.Name())
			continue
		}
		hits++
		sum += v
	}
	fmt.Println("hits:", hits, "sum:", sum)

	// The same value re-widened back to Object must still be the same key.
	for d := range M {
		var o Object = d
		if _, ok := objMap[o]; !ok {
			fmt.Println("MISS after re-widening:", d.Name())
		}
	}

	// Interface-to-interface equality must survive the round trip too.
	for obj := range objMap {
		d, ok := obj.(dependency)
		if !ok {
			continue
		}
		if Object(d) != obj {
			fmt.Println("IDENTITY LOST:", obj.Name())
		}
	}
	fmt.Println("done")
}
