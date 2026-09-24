package main

import (
	"fmt"
	"math"
	"unsafe"
)

// Go's value pun READ, *(*U)(unsafe.Pointer(&x)), is math.Float64bits' whole body. The conversion
// renders an equal-size numeric read as bitcast<T, U>(x) and leaves x unboxed when that was its only
// address use; a WRITE through the pun keeps the aliasing reinterpret. Every line below must print
// what `go run` prints.

func localInf() float64 {
	var bits uint64 = 0x7FF0000000000000
	return *(*float64)(unsafe.Pointer(&bits))
}

var kept *uint32

func alsoAddressed(b uint32) float32 {
	kept = &b
	return *(*float32)(unsafe.Pointer(&b))
}

// runtime/minmax.go's shape: the write aliases x, the read of y is a pun
func orBits(x, y float32) float32 {
	*(*uint32)(unsafe.Pointer(&x)) |= *(*uint32)(unsafe.Pointer(&y))
	return x
}

func main() {
	for _, f := range []float64{0, 1.5, -2.25, math.Inf(1), math.Inf(-1), math.MaxFloat64, math.SmallestNonzeroFloat64} {
		fmt.Printf("%v %#016x %v\n", f, math.Float64bits(f), math.Float64frombits(math.Float64bits(f)) == f)
	}

	negZero := math.Copysign(0, -1)
	fmt.Printf("-0 %#016x %v\n", math.Float64bits(negZero), math.Signbit(math.Float64frombits(math.Float64bits(negZero))))

	nan := math.Float64frombits(0x7FF8000000000123)
	fmt.Printf("nan payload %#016x %v\n", math.Float64bits(nan), math.IsNaN(nan))

	for _, f := range []float32{0, 1.5, -2.25, float32(math.Inf(1)), math.MaxFloat32} {
		fmt.Printf("%v %#08x %v\n", f, math.Float32bits(f), math.Float32frombits(math.Float32bits(f)) == f)
	}

	fmt.Println(localInf(), alsoAddressed(0x3FC00000), *kept == 0x3FC00000)
	negZero32 := math.Float32frombits(0x80000000) // a constant -0.0 is plain zero in Go
	fmt.Println(orBits(1.5, negZero32), math.Float32bits(orBits(1.5, negZero32)) == 0xBFC00000)
}
