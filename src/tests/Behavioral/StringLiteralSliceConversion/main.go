package main

import "fmt"

// Guards how a Go `[]byte("literal")` / `[]rune("literal")` conversion of a bare string *literal*
// (not a string variable) is emitted. A plain-text `[]byte` literal feeds the zero-allocation `u8`
// ROM span straight into the slice — `slice<byte>("hello"u8)` — skipping the intermediate heap
// `@string` the older `slice<byte>((@string)"hello")` form allocated. A `[]rune` literal keeps the
// `@string` form (it needs `@string`'s rune decoding, not raw UTF-8 bytes); a high-`\xHH`-byte
// `[]byte` literal keeps the byte-array-backed `@string` (its bytes do not round-trip through `u8`);
// and a string *variable* is already an `@string`. See convCallExpr.go / golib builtin.slice.
type vector struct {
	name string
	key  []byte
}

var vectors = []vector{
	{"split", []byte("this is a longer than block-size key " +
		"written as several pieces so it fits the " +
		"source width, exactly as crypto/hmac writes it")},
	{"plain", []byte("single")},
}

func main() {
	// Bare string-literal conversions: []byte → slice<byte>("…"u8), []rune → slice<rune>((@string)"…").
	bs := []byte("hello")
	rs := []rune("héllo")

	fmt.Println(len(bs), string(bs))
	fmt.Println(len(rs), string(rs))

	// Round-trip and a direct fmt of the literal conversion.
	fmt.Println([]byte("hi"))
	fmt.Println([]rune("aΩ"))

	// A raw (backtick) string literal is also a plain-text `u8` span — slice<byte>(@"ab"u8).
	fmt.Println([]byte(`ab`))

	// A high-\xHH-byte literal keeps the byte-array-backed @string form (u8 cannot carry those bytes).
	fmt.Println([]byte("\xff\xfe"))

	// Go's line-SPLITTING literal idiom: ONE constant string written as several `+`-joined
	// pieces so it fits the source width (crypto/hmac's long-key vectors). The pieces render as
	// bare C# string literals, and C# will not chain the two user-defined conversions
	// string → @string → byte[], so the whole operand takes the (@string) cast —
	// slice<byte>((@string)("first " + "second " + "third")).
	split := []byte("first " +
		"second " +
		"third")
	fmt.Println(len(split), string(split))

	// The same idiom through []rune, and a raw-literal piece in the chain.
	rsplit := []rune("α" + "β")
	fmt.Println(len(rsplit), string(rsplit))
	fmt.Println([]byte(`raw ` + "cooked"))

	// The shape that actually FAILED (crypto/hmac's vector table): the same split literal as a
	// positional element of a struct composite. That slot suppresses the `u8` span form, so the
	// pieces render as bare C# string literals with no @string anywhere in the expression —
	// without the cast the conversion is CS1503, "cannot convert from 'string' to 'byte[]'".
	for _, v := range vectors {
		fmt.Println(v.name, len(v.key), v.key[0], v.key[len(v.key)-1])
	}

	// String-variable conversions still work (the already-correct path) — same results.
	s := "hello"
	fmt.Println(string([]byte(s)) == string(bs))
}
