package main

import "fmt"

// An untyped constant that meets a DEFINED complex type takes that type (Go spec, Constants). Every
// context below must hold the value as the defined type, never as a bare float.

type C128 complex128
type C64 complex64

// A defined type over another defined complex type
type C128b C128

type F64 float64

type pair struct {
	a C128
	b C64
}

const (
	intConst   = 7
	floatConst = 1.5
	cplxConst  = 2i
)

// typed constants of the defined complex types: real, integral and complex values
const (
	typedC128  C128  = 1
	typedC128r C128  = 0.25
	typedC64   C64   = 0.25
	typedC64n  C64   = 3
	typedC64c  C64   = 1 + 2i
	typedC128b C128b = 0.5
)

func classify(c C128) string {
	switch c {
	case 0:
		return "zero"
	case 1, 2.5:
		return "small"
	}
	return "other"
}

func deferred() {
	defer fmt.Println("deferred:", takeC128(4), takeC64(0.5))
}

func takeC128(c C128) C128 { return c }
func takeC64(c C64) C64    { return c }
func takeMany(cs ...C128) C128 {
	var sum C128
	for _, c := range cs {
		sum += c
	}
	return sum
}

func retC128() C128         { return 3 }
func retC64() C64           { return 2.5 }
func retMulti() (C128, C64) { return 1, 2 }

func main() {
	// assignment and declaration
	var a C128 = 0
	a = 1
	var b C64 = 4
	b = 0.5
	var ab C128b = 6
	fmt.Println("assign:", a, b, ab)

	// map key and index
	m := map[C128]string{0: "zero", 1.5: "one-half"}
	m[2] = "two"
	fmt.Println("map:", m[0], m[1.5], m[2], len(m))

	// call argument, variadic argument, return
	x, y := retMulti()
	fmt.Println("call:", takeC128(3), takeC64(5), takeMany(1, 2, 3), retC128(), retC64(), x, y)

	// composite elements: slice, array, struct field, map value
	s := []C128{1, 2, 3i}
	arr := [2]C64{1, 2}
	p := pair{a: 4, b: 5}
	mv := map[string]C64{"k": 9}
	fmt.Println("composite:", s, arr, p, mv["k"])

	// binary operands, compound assignment, comparison
	c := C128(2)
	c = c * 2
	c = 1 + c
	c += 2
	c -= 0.5
	fmt.Println("binary:", c, c == 6.5, c == 0, c != 0, b*2, 1-b)

	// named untyped constants and constant expressions
	var ni C128 = intConst
	var nf C64 = floatConst
	var nc C128 = cplxConst
	var ci C128 = 2i
	var neg C128 = -1
	var expr C64 = 1 + 2*3
	fmt.Println("named:", ni, nf, nc, ci, neg, expr, takeC128(intConst), takeC64(floatConst))

	// conversions of a constant
	fmt.Println("conversion:", C64(2), C128(-3), C128b(1.25), C128(complex(0, 0)))

	// typed constants, case labels, a deferred call's arguments
	const localC64 C64 = 1.5
	const localC128b C128b = -2
	fmt.Println("typed:", typedC128, typedC128r, typedC64, typedC64n, typedC64c, typedC128b, localC64, localC128b, typedC64*2, typedC128*2, classify(0), classify(2.5), classify(3))
	deferred()

	// channel send
	ch := make(chan C64, 1)
	ch <- 8
	fmt.Println("channel:", <-ch)

	// controls: plain complex and a defined float
	var pc complex128 = 3
	var pc64 complex64 = 4
	var df F64 = 5
	fmt.Println("controls:", pc, pc64, df)
}
