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
