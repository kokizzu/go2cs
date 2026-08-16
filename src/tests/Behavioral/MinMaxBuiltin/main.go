// Regression test for the Go 1.21 `min` and `max` built-ins.
//
// Go added `min` and `max` as predeclared built-in functions in Go 1.21. They accept one or
// more ordered arguments of the same type and return the smallest / largest. The converter
// emits the calls verbatim (`min(...)` / `max(...)`), so they relied on golib's `builtin`
// static class providing matching generic methods. It previously had none, so any converted
// package using `min`/`max` failed to compile with CS0103 ("the name 'min' does not exist").
// golib now provides generic `min`/`max` constrained to IComparable<T> (covers the numeric
// primitives and @string). crypto/subtle's XORBytes (`min(len(x), len(y))`) was the trigger.
//
// Also: an argument that is a NAMED UNTYPED CONSTANT (`min(n, maxObletBytes)` — runtime
// mgcmark.go with a uintptr sibling; `min(depth, maxProfStackDepth)` — runtime1.go with int32)
// renders as its UntypedInt (BigInteger) static, which the `params ReadOnlySpan<T>` overloads
// reject (CS1503 — params-span element binding does not apply the user-defined implicit
// conversion). The converter casts such an argument to the call's Go-resolved result type:
// `min(n, (uintptr)(limit))`. Literal and typed arguments are unchanged.
//
// And: an argument of a NAMED numeric type is the go2cs-gen wrapper STRUCT, not the underlying
// primitive. golib's two-argument overloads bind `IComparisonOperators<T,T,bool>` (which the
// wrapper declares), but the N-argument `params ReadOnlySpan<T>` overloads bind `IComparable<T>`
// (which it did NOT) — so `min(a-got, got-a, a-got+q, got-a+q)` over crypto/internal/mlkem768's
// `type fieldElement uint16` was CS0315, "no boxing conversion from fieldElement to
// System.IComparable<fieldElement>". The generated wrapper now carries IComparable<T> and its
// CompareTo on the SAME kind-gate as its ordered operators (every numeric kind except complex,
// which Go orders no more than C# does), so a named numeric binds both overload shapes. The three
// named kinds below cover unsigned, floating and signed underlyings.
package main

import "fmt"

const limit = 128 << 10 // untyped
const floor = 16        // untyped

//go:noinline
func clampU(n uintptr) uintptr { return min(n, limit) }

//go:noinline
func clampI(d int32) int32 { return max(d, floor) }

type fieldElement uint16 // the crypto/internal/mlkem768 shape
type ratio float64
type delta int8

// spread is TestDecompressCompress's own call — four arguments of a named UNSIGNED type, so the
// params overload, over wrapper arithmetic that wraps exactly as Go's uint16 does.
//
//go:noinline
func spread(a, b fieldElement) fieldElement {
	return min(a-b, b-a, a-b+3329, b-a+3329)
}

func main() {
	// Two-argument integer min/max (the most common form, e.g. crypto/subtle).
	fmt.Println(min(3, 7)) // 3
	fmt.Println(max(3, 7)) // 7

	// Variadic (three or more arguments).
	fmt.Println(min(5, 2, 9, 1, 4)) // 1
	fmt.Println(max(5, 2, 9, 1, 4)) // 9

	// Single argument is valid in Go.
	fmt.Println(min(42)) // 42

	// Floating-point.
	fmt.Println(min(2.5, 1.5)) // 1.5
	fmt.Println(max(2.5, 1.5)) // 2.5

	// Strings are ordered, so min/max apply lexicographically.
	fmt.Println(min("banana", "apple", "cherry")) // apple
	fmt.Println(max("banana", "apple", "cherry")) // cherry

	// Used with len(), the crypto/subtle pattern.
	x := []byte{1, 2, 3}
	y := []byte{1, 2, 3, 4, 5}
	n := min(len(x), len(y))
	fmt.Println(n) // 3

	// named UNTYPED consts as arguments, typed by the sibling (uintptr and int32)
	fmt.Println(clampU(999999), clampU(7)) // 131072 7
	fmt.Println(clampI(3), clampI(100))    // 16 100
	// both arguments constant, one named-untyped (result type from the typed literal context)
	var big uintptr = 200000
	fmt.Println(min(big, limit, 500)) // 500

	// NAMED numeric types — the generated wrapper struct, not the primitive. Four arguments take
	// the IComparable<T> params overload; two take the IComparisonOperators one. Both must bind.
	a, b, c, d := fieldElement(10), fieldElement(3329), fieldElement(7), fieldElement(500)
	fmt.Println(min(a, b, c, d), max(a, b, c, d)) // 7 3329
	fmt.Println(min(a, c), max(a, c))             // 7 10
	fmt.Println(spread(10, 3))                    // 7

	// named FLOATING underlying
	p, q, r := ratio(2.5), ratio(-1.25), ratio(8)
	fmt.Println(min(p, q, r), max(p, q, r)) // -1.25 8
	fmt.Println(min(p, q), max(p, q))       // -1.25 2.5

	// named SIGNED underlying (negative values order below zero, not by bit pattern)
	i, j, k := delta(-5), delta(3), delta(-100)
	fmt.Println(min(i, j, k), max(i, j, k)) // -100 3
	fmt.Println(min(i, j), max(i, j))       // -5 3
}
