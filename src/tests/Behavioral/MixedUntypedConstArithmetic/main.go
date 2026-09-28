package main

import "fmt"

// Named untyped constants: an expression mixing an untyped integer constant with an untyped
// floating-point one is an untyped FLOAT constant, evaluated exactly.
const procs = 14
const capacityPerProc = 1e9
const three = 3
const half = 0.5
const seven = 7
const two = 2.0
const quadrillion = 1e15

func main() {
	// A product past 2^31, into uint64 (runtime's TestGCCPULimiter shape).
	var capacity uint64 = 14000000000
	fmt.Println(capacity == procs*capacityPerProc, capacity == capacityPerProc*procs)
	fmt.Println(uint64(procs*capacityPerProc), uint64(capacityPerProc*procs))

	// A fractional product.
	fmt.Println(three*half, half*three, float64(three*half), float32(half*three))

	// Mixed comparisons.
	fmt.Println(three < half*seven, half*seven > three, three == seven*half-half, half*seven != three)

	// Mixed division is float division; integer division stays integer.
	fmt.Println(seven/two, two/seven > 0, float64(seven/two), int(seven/two*two), seven/three)

	// Past 2^53, where the product is still exactly representable.
	fmt.Println(uint64(procs*quadrillion), uint64(quadrillion*procs))
}
