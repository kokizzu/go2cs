package main

import "fmt"

type box struct{ v int }

var calls int

func makeBox(v int) (*box, error) { return &box{v: v}, nil }

func pair() (int, string) {
	calls++
	return 42, "hello"
}

// single non-blank name: component read appended to the inline call (edwards25519's
// `var identity, _ = new(Point).SetBytes(...)` shape - the per-name field loop assigned
// the WHOLE result tuple to the first field, CS0029)
var defaultBox, _ = makeBox(7)

// two non-blank names: hidden once-evaluated tuple field + component reads
var n, s = pair()

func main() {
	fmt.Println(defaultBox.v) // 7
	fmt.Println(n, s)         // 42 hello
	fmt.Println(calls)        // 1 - proves the initializer call ran exactly once

	// FUNCTION-LOCAL grouped var spec deconstructing a multi-result call (time
	// appendFormat's `var (name, offset, abs = t.locabs() ...)`): the per-name path
	// assigned the whole tuple to the FIRST name and silently defaulted the rest
	// (CS0029) - must emit `var (ln, ls) = pair();`.
	var ln, ls = pair()
	fmt.Println(ln, ls, calls) // 42 hello 2

	// A tuple whose results are an INTERFACE and a FUNC: the escape analysis blanket-marks
	// both as "escaping" (they are already references), so a gate reading that raw flag -
	// rather than asking whether the name actually gets a heap box - rejected the
	// deconstruction, and the per-name fallback assigned the WHOLE tuple to the first name
	// and defaulted the rest (CS0029). net's `var ctx, cancel = context.WithCancel(...)`.
	var si, fi = ifaceAndFunc()
	fmt.Println(si.String(), fi()) // iface 9
}

type stringer interface{ String() string }

type sval struct{ s string }

func (v sval) String() string { return v.s }

func ifaceAndFunc() (stringer, func() int) {
	return sval{s: "iface"}, func() int { return 9 }
}
