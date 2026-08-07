package main

import "fmt"

// Guards range-over-integer for EVERY integer type, not just `int`.
//
// Go 1.22's `for range n` accepts any integer type and gives the iteration variable
// THAT type. go2cs recognized only `types.Int`/untyped-int, so every other integer
// kind fell through to visitRangeStmt's "unexpected 'ast.RangeStmt' expression" arm,
// which emits the whole statement as a C# COMMENT — the loop silently vanished from
// the conversion and the program quietly computed a different answer.
//
// The corpus site that proved it is internal/abi's `for range atyp.Len` inside
// unique.buildArrayCloneSeq (unique/clone.go): `atyp.Len` is a `uintptr`, the loop
// became a comment, and unique's cloneSeq for any array-of-string type came back
// EMPTY instead of carrying one offset per element.
//
// golib's `range(nint)` stays the `int` case (C# prefers the non-generic candidate,
// so existing emission is byte-identical); every other width binds the generic
// `range<T>` and the loop variable keeps the operand's own Go type.

type myLen uintptr

func main() {
	// The exact clone.go shape: blank key, uintptr operand.
	var n uintptr = 4
	count := 0
	for range n {
		count++
	}
	fmt.Println("uintptr blank-key iterations:", count)

	// The iteration variable takes the OPERAND's type, so it composes with values of
	// that same type without a conversion.
	var stride uintptr = 16
	var offset uintptr
	offsets := []uintptr{}
	for range n {
		offsets = append(offsets, offset)
		offset += stride
	}
	fmt.Println("uintptr offsets:", offsets)

	var b uint8 = 5
	sum8 := 0
	for i := range b {
		sum8 += int(i)
	}
	fmt.Println("uint8 sum:", sum8)

	var big int64 = 6
	sum64 := int64(0)
	for i := range big {
		sum64 += i
	}
	fmt.Println("int64 sum:", sum64)

	var u uint64 = 3
	var sumU uint64
	for i := range u {
		sumU += i
	}
	fmt.Println("uint64 sum:", sumU)

	var r rune = 4
	sumR := 0
	for i := range r {
		sumR += int(i)
	}
	fmt.Println("rune sum:", sumR)

	// A NAMED integer type over a non-int underlying.
	named := 0
	for i := range myLen(3) {
		named += int(i)
	}
	fmt.Println("named uintptr sum:", named)

	// Non-positive operands run no iterations, exactly as in Go.
	zero := 0
	for range int64(0) {
		zero++
	}
	for range int64(-9) {
		zero++
	}
	for range uintptr(0) {
		zero++
	}
	fmt.Println("non-positive iterations:", zero)

	// The `int` and untyped-constant cases are unchanged and must stay that way.
	plain := 0
	for i := range 3 {
		plain += i
	}
	size := 4
	for i := range size {
		plain += i
	}
	fmt.Println("int and untyped sum:", plain)
}
