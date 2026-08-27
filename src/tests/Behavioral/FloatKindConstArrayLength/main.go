// Guards the FLOAT-KIND (and COMPLEX-KIND) constant array length. `const S = 1e6; var x [S]byte`
// is legal Go — the spec asks only that the length be *representable as* an int — but go/types does
// not NORMALISE the recorded value for that position: `Checker.arrayLength` applies `constant.ToInt`
// to a local copy purely to test representability, then records the operand's own untyped-float
// value. `constant.Int64Val` PANICS (it does not return `(0, false)`) on any value that is neither
// Int nor Unknown kind, so the three converter sites that read an array length's constant straight
// from `info.Types` all died on it.
//
// The panic is RECOVERED under `-stdlib`/single-package conversion — a `visit file error` warning
// with exit 0 — and is a HARD package abort under `-tests`. That asymmetry is why no corpus gate
// ever saw it and why it presented as a reflect-specific wall: `reflect/all_test.go`'s
// TestSliceOverflow (`const S = 1e6; var x [S]byte`) blocked the whole package's test conversion,
// with 396 Go verdicts behind it and no C# verdict ever observed.
//
// Three shapes reach the defect, one per fixed site: a package-scope `var`, a local `var`, and a
// named array TYPE declaration. Everything else array-typed resolves its length through
// `types.Array.Len()` — an int64 go/types has already normalised — and is structurally immune; those
// are the controls here, kept so a future refactor that routes them back through the AST node fails
// loudly rather than silently.
//
// The class has exactly one sibling position — the SHIFT COUNT (`x << 1e0` is legal and records
// float kind too). That arm already normalises correctly (`constIntShiftValue`), and is guarded here
// so it stays that way.
//
// Not to be confused with FloatConstIntContext, which guards the OTHER half of the float-constant
// story: `takesInt(1.0)` is a slot go/types DOES convert, so the recorded value is already Int and
// the defect there was purely how the converted constant RENDERED. Here the recorded value itself is
// float-kind, and reading it was fatal.
package main

import "fmt"

// Float-kind constants. Each is integer-representable, so each is a legal array length.
const (
	globalLen = 1e2       // the package-scope and local `var` length
	namedLen  = 1e1       // the named-type length
	exprLen   = 1e3 / 1e1 // a FOLDED float expression, not a bare literal
	cplxLen   = 1e1 + 0i  // untyped COMPLEX with zero imaginary — also integer-representable
	overflow  = 1e6       // reflect/all_test.go TestSliceOverflow's own constant
	shiftBy   = 4e0       // the sibling arm: a float-kind SHIFT COUNT
)

// CONTROL: an ordinary Int-kind constant length, which never had a problem and must not move.
const intLen = 100

// Site 3 — package-scope `var` whose array length constant is float-kind.
var pkgArr [globalLen]byte
var pkgCplxArr [cplxLen]int
var pkgExprArr [exprLen]byte

// CONTROL for site 3: the same shape with an Int-kind constant.
var pkgIntArr [intLen]byte

// Site 1 — named array TYPE declarations.
type floatLenArr [namedLen]byte
type cplxLenArr [cplxLen]int32
type exprLenArr [exprLen]byte

// CONTROL for site 1.
type intLenArr [intLen]byte

// Structurally immune: a struct FIELD's array length is resolved through types.Array.Len().
type holder struct {
	buf  [globalLen]byte
	nest [namedLen][namedLen]byte
}

// Structurally immune: a type ALIAS to a float-length array.
type aliasArr = [namedLen]int

// Structurally immune: func PARAMETER and RESULT array types.
func roundTrip(a [namedLen]byte) [namedLen]byte {
	a[0] = 7
	return a
}

func main() {
	// Site 2 — LOCAL `var` of an array type with a float-kind length constant. This is
	// reflect/all_test.go:5185's exact shape, the one that blocked the package.
	var localArr [globalLen]byte
	localArr[0] = 1
	localArr[globalLen-1] = 2
	fmt.Println("local", len(localArr), localArr[0], localArr[99])

	// The literal 1e6 constant, at reflect's own magnitude.
	var big [overflow]byte
	big[overflow-1] = 3
	fmt.Println("big", len(big), big[999999])

	// A folded float expression as a local length.
	var localExpr [exprLen]byte
	fmt.Println("local expr", len(localExpr))

	// An inline float literal as a local length, with no named constant in the way.
	var localLit [1e2 / 2]byte
	fmt.Println("local literal", len(localLit))

	// Complex-kind local length.
	var localCplx [cplxLen]int
	localCplx[9] = 4
	fmt.Println("local complex", len(localCplx), localCplx[9])

	// MULTI-DIMENSIONAL local: both dimensions take the same path.
	var multi [namedLen][namedLen]byte
	multi[9][9] = 5
	fmt.Println("multi", len(multi), len(multi[0]), multi[9][9])

	// CONTROL: an Int-kind local length must emit exactly as before.
	var localInt [intLen]byte
	fmt.Println("local int", len(localInt))

	// Site 3 readback.
	pkgArr[0] = 6
	pkgCplxArr[9] = 7
	fmt.Println("pkg", len(pkgArr), pkgArr[0], len(pkgCplxArr), pkgCplxArr[9], len(pkgExprArr), len(pkgIntArr))

	// Site 1 readback.
	var fl floatLenArr
	fl[9] = 8
	var cl cplxLenArr
	cl[9] = 9
	var el exprLenArr
	var il intLenArr
	fmt.Println("named", len(fl), fl[9], len(cl), cl[9], len(el), len(il))

	// Immune controls: struct field, alias, parameter/result, composite literal, pointer,
	// map value and slice element.
	var h holder
	h.buf[99] = 10
	h.nest[9][9] = 11
	fmt.Println("field", len(h.buf), h.buf[99], len(h.nest), len(h.nest[0]), h.nest[9][9])

	var al aliasArr
	al[9] = 12
	fmt.Println("alias", len(al), al[9])

	var arg [namedLen]byte
	got := roundTrip(arg)
	fmt.Println("param", len(got), got[0])

	lit := [namedLen]byte{1, 2}
	fmt.Println("composite", len(lit), lit[0], lit[9])

	var ptr *[namedLen]byte = &lit
	fmt.Println("pointer", len(ptr), ptr[1])

	m := map[string][namedLen]byte{"k": lit}
	fmt.Println("map value", len(m["k"]), m["k"][1])

	sl := [][namedLen]byte{lit}
	fmt.Println("slice elem", len(sl), len(sl[0]), sl[0][1])

	// Sibling arm of the class: a float-kind SHIFT COUNT. Legal Go, records float kind, and is
	// already normalised by the converter — guarded so it stays normalised.
	var u uintptr = 1
	var n uint = 1
	var i64 int64 = 1
	fmt.Println("shift", uint64(u<<shiftBy), uint64(n<<shiftBy), i64<<shiftBy, 1<<shiftBy)
}
