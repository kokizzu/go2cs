// Regression test: a named untyped constant whose value exceeds int64/uint64 is emitted as
// GoUntyped (= System.Numerics.BigInteger), which has no implicit operator with the built-in
// numeric types. Every CONCRETE numeric context therefore has to cast such a reference.
//
// Originally only COMPARISON was covered — `x > Two129` (double > BigInteger) failed with CS0019
// and convBinaryExpr cast the operand (the math/j0 pattern). That left every other position
// emitting the reference bare: strconv's `var ftoatests = []ftoaTest{{below1e23, 'e', 17, …}}`
// alone produced twelve `cannot convert BigInteger to double` CS1503s. The cast now belongs to the
// REFERENCE (bigIntegerConstMaterialization), so it applies uniformly — composite-literal elements
// (positional, keyed, map value, array, nested), call arguments, typed `var` initializers,
// assignments, returns, and channel sends — and the comparison arm no longer double-casts.
//
// Counter-proof: an in-range untyped const (`small`) is emitted as an UntypedInt WRAPPER, which
// does have implicit conversions, so it must stay UNCAST in all the same positions.
//
// The wrapper arm carries a second requirement: comparing a uint64 against it must be a VALUE
// comparison. golib's UntypedInt keeps its payload in an int64 plus an "originated unsigned" bit, so
// a uint64 at or above 2**63 reads as a NEGATIVE int64 — and the relational operators compared those
// raw bits, answering `u < small` TRUE for u = 1<<63. That is a silent wrong answer for the whole
// `if fastSmalls && i < nSmalls` idiom: strconv's FormatUint(9223372036854775808) took the
// small-integer fast path and returned "0" (TestUitoa), and FormatUint's varlen sibling threw on the
// truncated negative slice bound (TestFormatUintVarlen).
package main

import "fmt"

const (
	Two129    = 1 << 129 // 2**129 ~= 6.8e38, exceeds uint64 -> GoUntyped (BigInteger)
	below1e23 = 99999999999999974834176
	above1e23 = 100000000000000008388608
	small     = 1000 // fits int64 -> UntypedInt wrapper, must NOT gain a cast
)

// The strconv ftoa_test shape: a big untyped const as a positional float64 struct field.
type ftoaTest struct {
	f    float64
	fmtc byte
	prec int
	s    string
}

var ftoatests = []ftoaTest{
	{below1e23, 'e', 17, "9.99999999999999748e+22"},
	{above1e23, 'f', 17, "100000000000000008388608.00000000000000000"},
	{small, 'g', -1, "1000"},
}

var floats = []float64{below1e23, above1e23, small}
var floatArr = [2]float64{below1e23, above1e23}
var byName = map[string]float64{"below": below1e23, "above": above1e23, "small": small}
var keyed = ftoaTest{f: below1e23, fmtc: 'g', prec: -1, s: "keyed"}
var nested = [][]float64{{below1e23}, {above1e23}}
var asFloat32 float32 = below1e23

func take(f float64) float64 { return f }

func give() float64 { return above1e23 }

func main() {
	var x float64 = 1e40
	fmt.Println(x > Two129) // true
	fmt.Println(x < Two129) // false
	var y float64 = 1e30
	fmt.Println(y >= Two129) // false

	// Composite-literal elements.
	fmt.Println(ftoatests[0].f, ftoatests[1].f, ftoatests[2].f)
	fmt.Println(floats[0], floats[1], floats[2])
	fmt.Println(floatArr[0], floatArr[1])
	fmt.Println(byName["below"], byName["above"], byName["small"])
	fmt.Println(keyed.f, nested[0][0], nested[1][0], asFloat32)

	// Call argument and return.
	fmt.Println(take(below1e23), give())

	// Typed var initializer + assignment.
	var v float64 = above1e23
	v = below1e23
	fmt.Println(v)

	// Local composite + channel send.
	local := []float64{below1e23, small}
	fmt.Println(local[0], local[1])

	ch := make(chan float64, 1)
	ch <- below1e23
	fmt.Println(<-ch)

	// The in-range wrapper const still interoperates directly everywhere.
	fmt.Println(take(small), small*2, float64(small)/4)

	// A uint64 AT OR ABOVE 2**63 compared against the in-range wrapper const: the strconv
	// `i < nSmalls` shape. Every one of these is a VALUE comparison in Go.
	for _, u := range []uint64{0, 999, 1000, 1001, 1 << 62, 1 << 63, 1<<64 - 1} {
		fmt.Println(u, u < small, u <= small, u > small, u >= small, u == small, u != small)
	}

	// Signed operands are unaffected (the raw-bit reading was already correct for them).
	for _, i := range []int64{-1 << 63, -1, 0, 1000, 1<<63 - 1} {
		fmt.Println(i, i < small, i > small, i == small)
	}

	// FormatUint's own idiom, inlined: the fast path must NOT fire for a huge value.
	fmt.Println(fastPath(0), fastPath(999), fastPath(1000), fastPath(1<<63), fastPath(1<<64-1))
}

// fastPath mirrors strconv.FormatUint's small-integer fast-path PREDICATE over the wrapper const:
// FormatUint may take the small() branch only when the value really is below nSmalls.
func fastPath(u uint64) string {
	if u < small {
		return "small"
	}
	return "big"
}
