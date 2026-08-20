// Regression test: a named untyped constant used as a bitwise operand (incl. the `~` of Go's
// `&^`) must be cast to the bitwise result type. The UntypedInt wrapper otherwise resolves to
// `int` under the operator, breaking a wider context — `Float64bits(f) &^ signBit` (uint64,
// signBit = 1<<63) → `ulong & int` (CS0019). This is the math/copysign pattern.
package main

import "fmt"

func copysign(f, sign uint64) uint64 {
	const signBit = 1 << 63 // 2^63, exceeds int64 -> UntypedInt
	return f&^signBit | sign&signBit
}

// clearLow is the BARE-LITERAL sibling of the same family, and the shape that was still open.
// A named untyped const routes through the cast copysign exercises, and a computed constant
// subtree carries a width cast from the shift-retype path (`x &^ (1 << 63)` emits
// `~(((uint64)1 << 63))`), but a plain literal reaches `~` with no type at all: it complements as
// a C# `int`, and `ulong & int` has no operator. net/netip's uint128_test.go:76,79
// (`^uint64(0) &^ 1`) is exactly this, and it was that package's last non-structural build error.
func clearLow(x uint64) uint64 { return x &^ 1 }

// clearLow32 is the negative control, and it is why the fix is uint64-only: `uint & int` promotes
// BOTH operands to `long`, so this shape compiled before the fix and must keep compiling after
// with no cast added. debug/macho's `Magic32 &^ 1` is the corpus instance.
func clearLow32(u uint32) uint32 { return u &^ 1 }

func main() {
	fmt.Println(copysign(0xFF, 0x8000000000000000)) // keep 0xFF magnitude, take the sign bit
	fmt.Println(copysign(0x8000000000000042, 0))    // clear the sign bit
	fmt.Println(clearLow(0xFFFFFFFFFFFFFFFF))       // 18446744073709551614
	fmt.Println(clearLow(1))                        // 0
	fmt.Println(clearLow32(0xFFFFFFFF))             // 4294967294
}
