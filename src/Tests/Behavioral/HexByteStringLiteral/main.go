// Regression test: a Go interpreted string literal that carries RAW-BYTE escapes. Go spells these
// TWO ways — `\xHH` (exactly two hex digits) and `\NNN` (exactly three octal digits) — and BOTH
// denote one raw byte, so both must reach C# as a byte-array-backed @string once that byte is
// >= 0x80.
//
// Hex: C#'s `\x` escape is a GREEDY 1-to-4-hex-digit code-UNIT escape. Re-emitting the Go token
// verbatim as a C# string literal both (a) mis-parses `\xdb` followed by ASCII "5""0" (the token
// `\xdb50`) as the single UTF-16 code unit U+DB50 — a lone high surrogate that cannot UTF-8-encode
// into a golib @string (CS9026) — and (b) silently widens every byte >= 0x80 to two UTF-8 bytes, so
// @string byte indexing (s[i], len) would not match Go. This is exactly time/tzdata's embedded zip
// blob.
//
// Octal: `"\377"` is the single byte 0xFF, but the C# char escape `ÿ` it would otherwise render
// as is the CHARACTER U+00FF, which UTF-8-encodes to the TWO bytes 0xC3 0xBF — so len and byte
// indexing silently diverge from Go. A sub-0x80 octal escape is ASCII-safe and must KEEP the
// readable string form (the .cs.target golden is what locks that half of the rule).
//
// The converter must emit the high-byte literals as byte-array-backed @strings so the exact bytes
// survive.
package main

import "fmt"

// data mixes the zip magic ("PK\x03\x04"), the surrogate-forming `\xdb50` sequence, high bytes
// (0xff, 0x92), a NUL, and trailing ASCII "LMT" — the byte-exactness of all of which is asserted.
const data = "\x50\x4b\x03\x04\xdb50\xff\x92\x00LMT"

// octalData mixes high octal escapes (0xff, 0x80 — the low boundary of the diverted range — and the
// 0xc3/0xbf UTF-8 pair spelled octally) with sub-0x80 ones (\101 'A', \000 NUL, \177 DEL) and
// trailing ASCII. Every byte must survive exactly, so len is 8, not the 12 a UTF-8 re-encode gives.
const octalData = "\377\200\303\277\101\000\177Z"

// asciiOctal is ALL sub-0x80 octal escapes ("AB\tC") — ASCII-safe, so it must NOT divert to the byte
// array and keeps the readable string form; its bytes are asserted identical either way.
const asciiOctal = "\101\102\011\103"

// get4 reads a little-endian uint32 the way time/tzdata's get4s does, proving byte indexing matches Go.
func get4(s string, i int) int {
	return int(s[i]) | int(s[i+1])<<8 | int(s[i+2])<<16 | int(s[i+3])<<24
}

// dump prints a string's length then each of its raw bytes, so any UTF-8 re-encode shows up as both
// a longer length and different byte values.
func dump(label string, s string) {
	fmt.Println(label)
	fmt.Println(len(s))
	for i := 0; i < len(s); i++ {
		fmt.Printf("%d ", s[i])
	}
	fmt.Println()
}

func main() {
	fmt.Println(len(data))
	for i := 0; i < len(data); i++ {
		fmt.Printf("%d ", data[i])
	}
	fmt.Println()
	fmt.Println(get4(data, 0))

	dump("octalData", octalData)
	dump("asciiOctal", asciiOctal)

	// A non-const octal literal takes the ordinary expression path rather than the const path.
	local := "\376\375"
	dump("local", local)
}
