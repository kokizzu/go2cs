// A PARENTHESIZED type conversion — `(T)(x)` — must convert exactly as the bare spelling
// `T(x)` does. They are the same Go program, and the converter already states that doctrine
// for the two IIFE spellings (see the sibling guard ParenIifeNilFuncConv, which covers the
// parenthesized-INVOCATION half); this is the same rule one construct over, for conversions.
//
// `(unsafe.Pointer)(new(int))` emitted `(uintptr)(@unsafe.Pointer)(@new<nint>())` — CS0030,
// `ж<nint>` to `Pointer` — while the bare `unsafe.Pointer(new(int))` on the very next line
// emitted the correct `new @unsafe.Pointer(@new<nint>())`. Two spellings of one expression,
// two different emitters.
//
// The parens are the whole cause, and neither of the two obvious suspects was involved:
//   * `isTypeConversion` declines the shape (it strips the arg's pointer and asks
//     ConvertibleTo(int, unsafe.Pointer), which is false), so the conversion renderer never runs;
//   * `isConstructorCall` then switched on `callExpr.Fun` WITHOUT unwrapping parens, so an
//     *ast.ParenExpr fell to its default arm and the call was not treated as a constructor.
// With `constructType` left empty, a separate rule synthesized the `(uintptr)` prefix, and the
// callee rendered WITH its parens, so the peephole that would have produced
// `new @unsafe.Pointer(…)` matched neither of its two conditions.
//
// Guards reflect's all_test.go:1511, `{(unsafe.Pointer)(new(int)), false}` in TestIsZero's table.
//
// Pointer VALUES are not printed — they are not deterministic. Every read here is a nil/non-nil
// test or a round-tripped value, so the comparison against `go run` is stable.
package main

import (
	"fmt"
	"unsafe"
)

type Celsius float64
type Counter int

func main() {
	// ---- The failing shape: a PARENTHESIZED conversion to unsafe.Pointer ----

	p := (unsafe.Pointer)(new(int))
	fmt.Println("A1 paren, fresh allocation, non-nil:", p != nil)

	n := new(int)
	*n = 42
	q := (unsafe.Pointer)(n)
	fmt.Println("A2 paren, existing pointer, non-nil:", q != nil)
	fmt.Println("A3 round-trips to the same value:", *(*int)(q))

	// In an `any` table, which is reflect's actual shape.
	table := []struct {
		v    any
		want bool
	}{
		{(unsafe.Pointer)(new(int)), false},
		{(unsafe.Pointer)(nil), true},
	}
	for i, row := range table {
		fmt.Println("A4 row", i, "isNil:", row.v == any(unsafe.Pointer(nil)), "want:", row.want)
	}

	// ---- The bare spelling: control, must stay byte-identical ----

	r := unsafe.Pointer(new(int))
	fmt.Println("B1 bare, fresh allocation, non-nil:", r != nil)

	s := unsafe.Pointer(n)
	fmt.Println("B2 bare, existing pointer, round-trips:", *(*int)(s))

	// ---- Parenthesized conversions to other type kinds ----

	// A named numeric type, both spellings.
	c1 := (Celsius)(36.6)
	c2 := Celsius(36.6)
	fmt.Println("C1 named float both spellings:", c1 == c2, c1)

	k1 := (Counter)(7)
	k2 := Counter(7)
	fmt.Println("C2 named int both spellings:", k1 == k2, k1)

	// A builtin type, both spellings.
	i1 := (int64)(5)
	i2 := int64(5)
	fmt.Println("C3 builtin both spellings:", i1 == i2, i1)

	b1 := (string)([]byte{104, 105})
	b2 := string([]byte{104, 105})
	fmt.Println("C4 string both spellings:", b1 == b2, b1)

	// A pointer-type conversion, parenthesized.
	var f float64 = 1.5
	pf := (*float64)(&f)
	fmt.Println("C5 paren pointer conversion:", *pf)

	// uintptr from a parenthesized unsafe.Pointer — the syscall idiom.
	fmt.Println("C6 uintptr of paren pointer is non-zero:", uintptr((unsafe.Pointer)(n)) != 0)
}
