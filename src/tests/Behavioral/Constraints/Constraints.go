// Copyright 2021 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

// Package constraints defines a set of useful constraints to be used
// with type parameters.
package constraints


type Frog struct {
	Name  string
	Color string
}

type ConstraintTest1 interface {
	string | []int | map[string]int | chan string | *int | [2]int | Frog
    Upper() string
}

type ConstraintTest2 interface {
	string | chan string | *int | [2]int | Frog
    Lower() string
}

// Signed is a constraint that permits any signed integer type.
// If future releases of Go add new predeclared signed integer types,
// this constraint will be modified to include them.
type Signed interface {
	~int | ~int8 | ~int16 | ~int32 | ~int64
}

// Unsigned is a constraint that permits any unsigned integer type.
// If future releases of Go add new predeclared unsigned integer types,
// this constraint will be modified to include them.
type Unsigned interface {
	~uint | ~uint8 | ~uint16 | ~uint32 | ~uint64 | ~uintptr
}

// Integer is a constraint that permits any integer type.
// If future releases of Go add new predeclared integer types,
// this constraint will be modified to include them.
type Integer interface {
	Signed | Unsigned
}

type PromotedTest1 interface {
	Signed
}

type PromotedTest2 interface {
	ConstraintTest1
}

type PromotedTest3 interface {
	ConstraintTest2
}

// Float is a constraint that permits any floating-point type.
// If future releases of Go add new predeclared floating-point types,
// this constraint will be modified to include them.
type Float interface {
	~float32 | ~float64
}

// Complex is a constraint that permits any complex numeric type.
// If future releases of Go add new predeclared complex numeric types,
// this constraint will be modified to include them.
type Complex interface {
	~complex64 | ~complex128
}

// Ordered is a constraint that permits any ordered type: any type
// that supports the operators < <= >= >.
// If future releases of Go add new ordered types,
// this constraint will be modified to include them.
//
// This type is redundant since Go 1.21 introduced [cmp.Ordered].
type Ordered interface {
	Integer | Float | ~string
}

// The unions above are all NUMERIC or ordered, so every one of them lifts a real C# operator
// constraint. A union whose terms are all STRUCTS lifts nothing, and that is the shape below —
// runtime/pprof's `[T runtime.StackRecord | runtime.MemProfileRecord | runtime.BlockProfileRecord]`
// reduced to its essentials, which was the whole of that package's 174-verdict build wall.
//
// Two emissions were wrong for it, one behind the other. Go's `==` works on comparable structs, so
// the operator sets counted Struct as comparable and lifted `IEqualityOperators<T, T, bool>` — but
// that clause is a claim about the type ARGUMENT implementing a BCL interface, which no `[GoType]`
// struct does (CS0315 at every instantiation, naming the concrete struct rather than the constraint
// that cannot admit it). Dropping the spurious lift then exposed the fall-through underneath: with
// no operator constraint and no interface to name, the Go union text was emitted VERBATIM as a C#
// constraint list (CS1003 ×4 — a syntax error rather than a type error).
//
// The emission that compiles keeps the union as a breadcrumb comment and constrains only on what C#
// can actually express: `where T : /* recordA | recordB | recordC */ new()`. Go's checker validated
// every instantiation before conversion, so the clause has nothing left to enforce. Instantiating at
// each term is what makes this a guard — an uninstantiated generic never binds its constraint.
type recordA struct{ n int }
type recordB struct{ n int }
type recordC struct{ n int }

// RecordUnion is a struct-only type set: no shared operator, no golib surface, no method set.
type RecordUnion interface {
	recordA | recordB | recordC
}

func firstOf[T RecordUnion](p []T) T {
	var zero T

	if len(p) == 0 {
		return zero
	}

	return p[0]
}

// UseRecordUnion instantiates the struct-only union at each of its terms.
func UseRecordUnion() int {
	a := firstOf([]recordA{{1}})
	b := firstOf([]recordB{{2}})
	c := firstOf([]recordC{{4}})

	return a.n + b.n + c.n
}
