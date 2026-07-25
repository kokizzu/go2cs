package main

import "fmt"

// The Go standard library's 128-bit byte-classification bitmap idiom (go/doc/comment's
// isHost/isPath/isIdentASCII/importPathOK, net/textproto's validHeaderFieldByte/
// validHeaderValueByte). `mask` is an UNTYPED constant far wider than any 64-bit type — legal
// in Go because only the FINAL value of a constant expression must be representable — and both
// halves are extracted by constant expressions that ARE uint64-valued: `mask&(1<<64-1)` and
// `mask>>64`. Neither half may be emitted as run-time arithmetic over the 128-bit constant.

// isHost reports whether c is a byte that can appear in a URL host.
func isHost(c byte) bool {
	const mask = 0 |
		(1<<26-1)<<'A' |
		(1<<26-1)<<'a' |
		(1<<10-1)<<'0' |
		1<<'_' |
		1<<'@' |
		1<<'-' |
		1<<'.' |
		1<<'[' |
		1<<']' |
		1<<':'

	return ((uint64(1)<<c)&(mask&(1<<64-1)) |
		(uint64(1)<<(c-64))&(mask>>64)) != 0
}

// validHeaderValueByte inverts the two halves (`&^`), so the high half reaches the emission
// through a complement rather than a plain mask.
func validHeaderValueByte(c byte) bool {
	const mask = 0 |
		(1<<(0x7f-0x21)-1)<<0x21 |
		1<<0x20 |
		1<<0x09

	return ((uint64(1)<<c)&^(mask&(1<<64-1)) |
		(uint64(1)<<(c-64))&^(mask>>64)) == 0
}

// smallHigh extracts a high half whose folded value is SMALL (in int32 range) — the fold must fire
// on the operand's unrepresentability, never on the result's magnitude.
func smallHigh(c byte) uint64 {
	const mask = 1<<70 | 1<<3

	return (uint64(1)<<c)&(mask&(1<<64-1)) | (uint64(1)<<(c-64))&(mask>>64)
}

// nativeWidth targets uintptr rather than uint64 — the native-width arm of the same fold.
func nativeWidth(c byte) uintptr {
	const mask = 1<<126 | 1<<65 | 1<<7

	return (uintptr(1)<<c)&(mask&(1<<64-1)) | (uintptr(1)<<(c-64))&(mask>>64)
}

func main() {
	hosts := 0
	values := 0
	for i := 0; i < 256; i++ {
		c := byte(i)
		if isHost(c) {
			hosts++
		}
		if validHeaderValueByte(c) {
			values++
		}
	}
	fmt.Println("isHost count:", hosts)
	fmt.Println("validHeaderValueByte count:", values)

	fmt.Println("isHost samples:", isHost('a'), isHost('Z'), isHost('9'), isHost(':'), isHost(' '), isHost(200))
	fmt.Println("value samples:", validHeaderValueByte('a'), validHeaderValueByte(0x09), validHeaderValueByte(0x00), validHeaderValueByte(0x80))

	for _, c := range []byte{3, 6, 70, 8, 64} {
		fmt.Printf("smallHigh(%d) = %d\n", c, smallHigh(c))
	}
	for _, c := range []byte{7, 65, 126, 8, 64} {
		fmt.Printf("nativeWidth(%d) = %d\n", c, nativeWidth(c))
	}
}
