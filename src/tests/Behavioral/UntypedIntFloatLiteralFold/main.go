package main

import "fmt"

// A constant expression that mixes a NAMED untyped-int constant with a float operand, typed as an
// integer by its context, is Go's exact value: `uint64(1.0/(retainExtraPercent/100.0))` is 10.

const retainExtraPercent = 10 // runtime/mgcscavenge.go
const baseline = 100 << 20    // runtime/debug/garbage_test.go
const pct = 29
const procs = 14
const capacityPerProc = 1e9 // runtime/mgclimit_test.go

type duration int64

func advance(d duration) int64 { return int64(d) }

func main() {
	// Explicit conversions.
	fmt.Println(uint64(1.0 / (retainExtraPercent / 100.0)))
	fmt.Println(int(1.2*baseline), int64(1.5*baseline), int(0.2*baseline))
	fmt.Println(uint64(pct / 100.0 * 100))
	fmt.Println(int(procs * capacityPerProc))

	// Implicit: a comparison against a typed operand, an assignment, and an argument of a named type.
	var capacity uint64 = 14000000000
	fmt.Println(capacity == procs*capacityPerProc, capacity == capacityPerProc*procs)
	var want int64 = 1.5 * baseline
	fmt.Println(want)
	fmt.Println(advance(2 * procs * capacityPerProc))
}
