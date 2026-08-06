package main

import "fmt"

// A package-level untyped float constant emits as a golib UntypedFloat (implicitly convertible to
// BOTH float32 and float64). That is what exposes the complex() overload-selection bug below; a
// folded float literal would already emit `D` and rule out the float32 overload on its own.
const gHalfPi = 1.5707963267948966

// (5) A COMPLEX-kind CONSTANT must emit as a real complex value. go/constant's exact string for a
// complex is the parenthesized rational form — `5.5+1.5i` is `(11/2 + 3/2i)` — which is neither C#
// syntax nor a form strconv.ParseComplex accepts, so testing that text for complex128
// representability could never succeed: EVERY complex constant was classified as beyond-complex128
// and emitted through the untyped-BigInteger arm, which cannot hold a complex at all. Each half is
// now rendered and range-tested on its own (converter: exactComplexConstString).
const (
	cRational   = 5.5 + 1.5i          // halves fold to RATIONALS: (11/2 + 3/2i)
	cNegImag    = 2.25 - 0.75i        // a NEGATIVE imaginary part
	cPureImag   = 3i                  // a ZERO real part
	cWideEnough = 1.5e308 + 1.0e307i  // strconv atoc_test's shape: fits complex128, barely
	cFolded     = (1 + 2i) * (3 + 4i) // a folded complex EXPRESSION (-5+10i)
)

// A complex64-typed const takes the F-suffixed halves (and, like every complex, cannot be a C#
// `const` — complex64 is a struct).
const c64 complex64 = 1.5 + 2.5i

// (6) math.MaxFloat32's own definition, kept local so this guard needs no `math` reference. As a
// package-level untyped float const it emits as an UntypedFloat symbol — exactly the shape whose
// width a complex() call site has to pin (see the block at the end of main).
const maxFloat32 = 3.40282346638528859811704183484516925440e+38

func showComplex(name string, c complex128) {
	fmt.Println(name, real(c), imag(c))
}

func main() {
	// (1) complex128-context int-literal SHIFTS out of int32 range must fold to the exact value,
	// not a C# int32-masked shift (1<<35 masked to 1<<3 = 8). Guards floatContextConstLiteral's
	// complex128 arm.
	huge := []complex128{1 << 35, 1 << 240, -1 << 120, 1234567891234567 << 40}
	for _, h := range huge {
		fmt.Println(real(h))
	}

	// (2) An int-literal complex() argument in a complex128 context must bind the float64 element
	// overload, not float32 — else gHalfPi recomputes at float32 (1.5707963705062866) instead of
	// float64 (1.5707963267948966). Guards the convBasicLit int-literal float-context render.
	c := complex(0, gHalfPi)
	fmt.Println(real(c), imag(c))

	// (3) INTEGER division stays integer even in a float/complex context: Go evaluates untyped int
	// operands with integer division, so `7 / 2` is 3 (then 3.0), NOT 3.5. The int-literal render
	// must not push a float context onto a `/` operand. `e`'s 0D sibling still pins complex128, so
	// its real part is the integer quotient 3.0.
	var q float64 = 7 / 2
	e := complex(7/2, 0)
	fmt.Println(q, real(e), imag(e))

	// A FLOAT-kind operand keeps float division (gHalfPi is UntypedFloat): gHalfPi / 2 = 0.785...
	fmt.Println(gHalfPi / 2)

	// (4) The mirror of (2): an IMAGINARY literal whose imaginary part is ZERO (`0i`) may convert to
	// a REAL float argument. With a TYPED first argument (`rf` is float64), complex()'s signature is
	// complex(float64, float64), so go/types records `0i` as float64 — it must emit its real part
	// (0D), not `.i()` (a Complex the float overload rejects, CS1503). This is exactly internal/
	// fmtsort's `complex(math.NaN(), 0i)` shape. Guards convBasicLit's token.IMAG float-context arm.
	var rf float64 = 1.5
	r := complex(rf, 0i)
	fmt.Println(real(r), imag(r))

	// (5) continued — the complex constants above, read back through real/imag.
	showComplex("cRational", cRational)
	showComplex("cNegImag", cNegImag)
	showComplex("cPureImag", cPureImag)
	showComplex("cWideEnough", cWideEnough)
	showComplex("cFolded", cFolded)
	fmt.Println("c64", real(c64), imag(c64))

	// A function-LOCAL complex const takes the same rendering (a local of the complex type, never
	// a C# `const`).
	const local = 0.5 - 0.25i
	showComplex("local", local)

	// (6) An UNTYPED-FLOAT pair must build the complex at the width Go's typing gives the CALL.
	// golib's complex(float32,float32)=>complex64 and complex(float64,float64)=>complex128 are BOTH
	// applicable to an UntypedFloat operand, and C# prefers the better conversion target — the
	// NARROWER one — so a complex128 was silently constructed at float32 width. A literal operand
	// carries its width from the untyped-const context already; a NAMED untyped const cannot, so
	// the converter pins those arguments explicitly. Before the fix `over` was (+Inf+Infi) and the
	// float32-range question below answered "fits", which is how encoding/gob's TestOverflow found
	// nothing out of complex64's range to reject.
	over := complex(maxFloat32*2, maxFloat32*2)
	fmt.Printf("over %T %v %v\n", over, real(over), imag(over))
	fmt.Println("over-fits-float32", float64(float32(real(over))) == real(over))

	// The same operands in a named untyped const pair, and then the SAME pair in a complex64
	// context — the width follows the call's Go type, not the operands.
	pair := complex(gHalfPi, gHalfPi)
	fmt.Printf("pair %T %v %v\n", pair, real(pair), imag(pair))

	var narrow complex64 = complex(gHalfPi, gHalfPi)
	fmt.Printf("narrow %T %v %v\n", narrow, real(narrow), imag(narrow))

	// A MIXED call — one typed operand, one named untyped const — was always unambiguous (float64
	// has no implicit conversion to float32, so only the complex128 overload applied); it must stay
	// exactly as correct.
	var rf64 float64 = 2
	mixed := complex(rf64, gHalfPi)
	fmt.Printf("mixed %T %v %v\n", mixed, real(mixed), imag(mixed))
}
