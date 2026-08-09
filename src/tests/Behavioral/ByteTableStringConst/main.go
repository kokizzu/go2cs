package main

import "fmt"

// A string constant used as a raw BYTE TABLE (math/bits' rev8tab shape): built by concatenation so it
// folds to one constant value with no single *ast.BasicLit, and holding bytes >= 0x80 that are NOT
// valid UTF-8. Go strings are byte sequences, so byte-indexing must return the exact bytes; a C#
// UTF-16/u8 string literal would UTF-8-re-encode each >=0x80 byte (0x80 -> 0xC2 0x80) and corrupt
// both indexing and len. Guards the byte-array-backed @string emission for such consts.
const revTab = "" +
	"\x00\x80\x40\xc0" +
	"\x20\xa0\x60\xe0"

// The SECOND way a folded const fails to round-trip, and the one that valid-UTF-8 alone cannot see:
// C#'s `\x` escape is GREEDY -- one to FOUR hex digits -- where Go's is exactly two. So a `\xHH`
// whose next CHARACTER is a hex digit re-parses as a different, longer escape and swallows it:
// `\x01F` is U+001F with the 'F' gone, and `\x0dB` is U+00DB with the 'B' gone. Every byte below is
// ASCII, so this value is perfectly valid UTF-8 and the table above's test says nothing about it.
// Shape taken from net/http/fcgi's FCGI_MPXS_CONNS record, where it silently corrupted the constant
// TestGetValues compares against.
const greedyHex = "\x0f\x01" + "FCGI_MPXS_CONNS1" + "\x0a\x0d" + "BEEF"

func main() {
	for i := 0; i < len(revTab); i++ {
		fmt.Printf("%d ", revTab[i])
	}
	fmt.Println()
	fmt.Println("len:", len(revTab))

	for i := 0; i < len(greedyHex); i++ {
		fmt.Printf("%d ", greedyHex[i])
	}
	fmt.Println()
	fmt.Println("len:", len(greedyHex))
	fmt.Printf("%q\n", greedyHex)
}
