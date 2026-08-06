// AndNotAssignNarrow guards the `&^=` (AND-NOT / bit-clear) compound assignment on a
// narrow or unsigned integer. C# has no `&^`, so it expands to `flags &= ~X`; the `~`
// promotes its operand to `int`, which is not implicitly convertible to byte/ushort/
// uint/… (CS0266), and for a CONSTANT operand `~X` folds to a negative int constant
// whose checked narrowing overflows (CS0221). The converter emits
// `flags &= unchecked((uint8)~X)` for such LHS types; an `int` LHS (which `int` widens
// to) is left as `&= ~X`. Both an ident LHS and a struct-field (selector) LHS are
// exercised since they route through different emission paths. Mirrors runtime's
// `h.flags &^= hashWriting`.
package main

import "fmt"

const writing = 4

type hmap struct {
	flags uint8
}

func main() {
	h := &hmap{flags: 7}
	h.flags &^= writing  // selector LHS, uint8, named const
	h.flags |= 8         // 3 | 8 = 11
	fmt.Println(h.flags) // 11

	var u uint16 = 0xFFFF
	u &^= 0x0F0    // ident LHS, uint16, literal
	fmt.Println(u) // 65295

	var x uint32 = 0xFFFFFFFF
	x &^= 0xFF
	fmt.Println(x) // 4294967040

	var i int = 0xFF
	i &^= 0x0F      // int LHS: no cast needed
	fmt.Println(i)  // 240

	// A STANDALONE `^x` on a sub-int unsigned type, WIDENED. Go's complement has the
	// operand's own type and wraps to its width (`^uint16(5)` == 65530); C# promotes
	// byte/ushort to int first, so the unwidened `~` is -6 — the same low 16 bits, but a
	// widening use carries the sign bits. compress/flate's stored-block header did exactly
	// `writeBits(int32(^uint16(length)), 16)`, and the sign-extended value flooded the
	// 64-bit bit accumulator: every NoCompression stream came out corrupt.
	length := 5
	fmt.Println(int32(^uint16(length)))  // 65530
	fmt.Println(uint64(^uint16(length))) // 65530

	var b8 uint8 = 5
	fmt.Println(int32(^b8), uint64(^b8)) // 250 250

	// Types at least 32 bits wide, and SIGNED narrow types, keep C#'s own result.
	var u32 uint32 = 5
	var i16 int16 = 5
	fmt.Println(^u32, int32(^i16), ^i16) // 4294967290 -6 -6

	// Round-tripping back into the narrow type is unaffected.
	var u16 uint16 = 5
	u16 = ^u16
	fmt.Println(u16) // 65530

	// A CONSTANT operand turns that same truncation into a C# CONSTANT conversion, which is
	// CHECKED whatever the enclosing context says: `~(ushort)0` promotes to the int -1 and
	// `(ushort)(-1)` is a hard CS0221. The all-ones idiom is exactly that shape — it is the
	// bound x/net/dnsmessage compares each section count against, seven times in one file.
	fmt.Println(int(^uint16(0))) // 65535
	fmt.Println(int(^uint8(0)))  // 255

	const seed uint16 = 5
	fmt.Println(uint64(^seed)) // 65530
}
