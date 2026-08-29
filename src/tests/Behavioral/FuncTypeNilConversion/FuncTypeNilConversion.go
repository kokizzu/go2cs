// `(funcType)(nil)` — a CONVERSION of the untyped nil to a func type — must render its operand
// as the plain typed nil, not as a conversion to the func's FIRST PARAMETER type.
//
// It emitted `(Funcꓸꓸꓸ<Point, nint>)((Point)(default!))` and
// `(Actionꓸꓸꓸ<nint>)((nint)(default!))` — CS0030 both, the nil landing on `Point` / `nint`,
// which are exactly those func types' first parameters.
//
// The cause is not in the conversion renderer: `isTypeConversion` has no `*ast.FuncType` arm, so
// a func-type conversion is never classified as one and takes the regular CALL path instead.
// There the variadic-nil disambiguation arm fires — the arm that casts an untyped nil in a
// VARIADIC slot to the ELEMENT type, so C# cannot bind the params-array form and silently drop
// the argument (database/sql's `exec(t, db, "INSERT|…", nil)`, which answered
// "expected 1 arguments, got 0"). On a CONVERSION that arm is meaningless: there is no params
// expansion to disambiguate, the single operand is the conversion SOURCE, and `default!` already
// binds unambiguously to the one delegate target.
//
// The predicate is variadic AND exactly one declared parameter (`i == params.Len()-1` with i=0),
// which is why the sibling spellings below were unaffected — so this file pins the discriminator
// rather than just the symptom.
//
// The variadic CALLS at the end are the controls that must NOT change: they are the shape the arm
// exists for, and `len(args)` is a real behavioral read — if the cast were lost, C# would bind the
// normal form, pass a null array, and print 0.
package main

import "fmt"

type Point struct{ X, Y int }

func main() {
	// ---- The failing predicate: variadic, exactly ONE declared parameter ----

	a := (func(...int))(nil)
	fmt.Println("A1 variadic-1-param Action nil:", a == nil)

	b := (func(...Point) int)(nil)
	fmt.Println("A2 variadic-1-param Func nil:", b == nil)

	c := (func(...string) (int, bool))(nil)
	fmt.Println("A3 variadic-1-param multi-result nil:", c == nil)

	// ---- Sibling spellings that already compiled: controls ----

	// Variadic but TWO declared parameters, so i == params.Len()-1 was false at i=0.
	d := (func(Point, ...Point) int)(nil)
	fmt.Println("B1 variadic-2-param nil:", d == nil)

	// Non-variadic, one parameter.
	e := (func(Point) int)(nil)
	fmt.Println("B2 non-variadic nil:", e == nil)

	f := (func(int) bool)(nil)
	fmt.Println("B3 non-variadic bool nil:", f == nil)

	// No parameters at all.
	g := (func())(nil)
	fmt.Println("B4 no-param nil:", g == nil)

	// A non-nil func through the same conversion spelling still calls.
	h := (func(...int) int)(sum)
	fmt.Println("B5 conversion of a real func:", h(1, 2, 3))

	// ---- Controls for the arm being guarded: real variadic CALLS with nil arguments ----

	// The database/sql shape the arm exists for. A lost element cast binds C#'s normal form
	// and passes a null array, so these counts would read 0.
	fmt.Println("C1 one nil arg:", countArgs("x", nil))
	fmt.Println("C2 nil among others:", countArgs("x", 1, nil, "y"))
	fmt.Println("C3 no variadic args:", countArgs("x"))
	fmt.Println("C4 spread:", countArgs("x", []any{1, nil, 3}...))
	fmt.Println("C5 typed nil slice arg:", countPtrs(nil, nil))
	fmt.Println("C6 sum:", sum(4, 5, 6))
}

func sum(xs ...int) int {
	t := 0
	for _, x := range xs {
		t += x
	}
	return t
}

func countArgs(s string, args ...any) int { return len(args) }

func countPtrs(args ...*Point) int { return len(args) }
