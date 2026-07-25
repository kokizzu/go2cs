package main

import "fmt"

// An untyped constant materialized into an interface slot takes Go's DEFAULT type for its
// kind: untyped int -> int (go2cs nint), untyped rune -> rune (int32), untyped float ->
// float64. Interface equality compares the DYNAMIC TYPE first, so a value boxed under the
// wrong CLR type silently compares unequal to an identical Go value. This test pins the
// default-type materialization at every interface-typed slot the converter emits.

const namedInt = 7    // untyped int constant
const namedRune = 'A' // untyped rune constant
const namedFloat = 2.5
const namedWide = 1 << 40 // untyped int constant beyond the int32 range

type holder struct {
	v any
}

// A VARIADIC `...any` slot (the fmt/print/log family shape).
func variadicEq(args ...any) bool {
	return args[0] == args[1]
}

// A NON-variadic `any` parameter.
func paramEq(a any, b any) bool {
	return a == b
}

// Return INTO an `any` result.
func retLit() any    { return 0 }
func retNamed() any  { return namedInt }
func retRuneL() any  { return 'A' }
func retFloatL() any { return 2.5 }

// A type switch observes the boxed DYNAMIC type.
func kindOf(v any) string {
	switch v.(type) {
	case int:
		return "int"
	case int32:
		return "int32"
	case float64:
		return "float64"
	}
	return "other"
}

func readN() (int, error) { return 0, nil }

func main() {
	n, _ := readN()
	i := 7
	var r rune = 'A'
	f := 2.5

	// --- variadic ...any slot: the base32 testEqual shape (n == 0 after EOF) ---
	fmt.Println("var lit zero  :", variadicEq(n, 0))
	fmt.Println("var lit int   :", variadicEq(i, 7))
	fmt.Println("var named     :", variadicEq(i, namedInt))
	fmt.Println("var named+1   :", variadicEq(i+1, namedInt+1))
	fmt.Println("var lit expr  :", variadicEq(i+1, 3+5))
	fmt.Println("var neg lit   :", variadicEq(-i, -7))
	fmt.Println("var rune lit  :", variadicEq(r, 'A'))
	fmt.Println("var rune named:", variadicEq(r, namedRune))
	fmt.Println("var float lit :", variadicEq(f, 2.5))
	fmt.Println("var float nmd :", variadicEq(f, namedFloat))

	// --- non-variadic any parameter ---
	fmt.Println("par lit       :", paramEq(i, 7))
	fmt.Println("par named     :", paramEq(i, namedInt))
	fmt.Println("par rune lit  :", paramEq(r, 'A'))
	fmt.Println("par float lit :", paramEq(f, 2.5))

	// --- assignment into an any variable ---
	var av any = 7
	var ar any = 'A'
	var af any = 2.5
	fmt.Println("asn lit       :", av == any(i))
	fmt.Println("asn rune lit  :", ar == any(r))
	fmt.Println("asn float lit :", af == any(f))

	// --- []any / map[any] / map[...]any composite elements ---
	sl := []any{7, 'A', 2.5, namedInt}
	fmt.Println("slc lit       :", sl[0] == any(i))
	fmt.Println("slc rune lit  :", sl[1] == any(r))
	fmt.Println("slc float lit :", sl[2] == any(f))
	fmt.Println("slc named     :", sl[3] == any(i))

	mk := map[any]string{7: "seven", 'A': "letter", 2.5: "half"}
	fmt.Println("mapkey lit    :", mk[any(i)], mk[any(r)], mk[any(f)])
	// The stored key must round-trip through a LITERAL lookup too (both sides box alike).
	fmt.Println("mapkey lookup :", mk[7], mk['A'], mk[2.5], mk[namedInt])
	_, mkMiss := mk[int32(7)] // Go: int32(7) is a DIFFERENT dynamic type than int(7)
	fmt.Println("mapkey miss   :", mkMiss)

	mv := map[string]any{"i": 7, "r": 'A', "f": 2.5}
	fmt.Println("mapval lit    :", mv["i"] == any(i), mv["r"] == any(r), mv["f"] == any(f))

	// --- struct literal with an any field (keyed and positional) ---
	hk := holder{v: 7}
	hp := holder{7}
	fmt.Println("struct keyed  :", hk.v == any(i))
	fmt.Println("struct posit  :", hp.v == any(i))

	// --- channel send into a chan any ---
	ch := make(chan any, 1)
	ch <- 7
	fmt.Println("chan send     :", <-ch == any(i))

	// --- return into an any result ---
	fmt.Println("ret lit       :", retLit() == any(n))
	fmt.Println("ret named     :", retNamed() == any(i))
	fmt.Println("ret rune lit  :", retRuneL() == any(r))
	fmt.Println("ret float lit :", retFloatL() == any(f))

	// --- beyond the int32 range: the literal already renders `(nint)…L`, the named const does not ---
	wide := 1 << 40
	fmt.Println("wide lit      :", variadicEq(wide, 1<<40))
	fmt.Println("wide named    :", variadicEq(wide, namedWide))
	fmt.Println("wide kinds    :", kindOf(1<<40), kindOf(namedWide))

	// --- the dynamic types themselves, as a type switch sees them ---
	fmt.Println("kinds lit     :", kindOf(0), kindOf('A'), kindOf(2.5))
	fmt.Println("kinds named   :", kindOf(namedInt), kindOf(namedRune), kindOf(namedFloat))
	fmt.Println("kinds value   :", kindOf(i), kindOf(r), kindOf(f))

	// --- type assertion on a boxed untyped constant ---
	iv, iok := any(7).(int)
	rv, rok := any('A').(int32)
	fv, fok := any(2.5).(float64)
	fmt.Println("assert lit    :", iv, iok, rv, rok, fv, fok)
}
