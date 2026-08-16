package main

import "fmt"

// A variadic INTERFACE parameter (`...Shape`) receiving pointer elements that implement the
// interface via POINTER receivers: every trailing argument must get the *T→interface adapter
// wrap, not just the first (the declared-parameter index). go/types' builtins.go hit this with
// `makeSig(S, S, NewSlice(T))` — the ж<Slice> call result in the SECOND variadic slot passed
// loose and failed CS1503 while the first slot converted.

type Shape interface {
	Area() int
}

type Rect struct {
	w, h int
}

func (r *Rect) Area() int {
	return r.w * r.h
}

type Circle struct {
	r int
}

func (c *Circle) Area() int {
	return 3 * c.r * c.r
}

// newRect mirrors the go/types shape: a call RESULT (a pointer) landing in a variadic slot.
func newRect(w, h int) *Rect {
	return &Rect{w: w, h: h}
}

func totalArea(scale int, shapes ...Shape) int {
	sum := 0

	for _, s := range shapes {
		sum += s.Area()
	}

	return sum * scale
}

// countArgs reports the ARITY of the variadic pack, which is what an untyped `nil` element
// silently changes when it is emitted typeless: `default!` converts to the params ARRAY as
// readily as to its element, so C# prefers the call's normal form and the argument vanishes —
// the pack arrives null (length 0) instead of holding one nil. Go always reads a bare `nil`
// here as ONE element; passing the slice itself requires `nil...`.
func countArgs(args ...any) (int, bool) {
	first := false

	if len(args) > 0 {
		first = args[0] == nil
	}

	return len(args), first
}

// describe is the non-empty-interface sibling: the same arity question where the element type
// is a named interface rather than `any`.
func describe(shapes ...Shape) int {
	return len(shapes)
}

func main() {
	// Trailing args past the first: call results and addressed composite literals.
	fmt.Println(totalArea(2, newRect(3, 4), &Circle{r: 2}, newRect(1, 5)))

	// A value pointer local mixed in after an already-interface arg.
	r := &Rect{w: 4, h: 2}
	var s Shape = &Circle{r: 3}
	fmt.Println(totalArea(1, s, r))

	// Empty variadic call.
	fmt.Println(totalArea(3))

	// Spread form stays a slice pass-through (no per-element adapter).
	shapes := []Shape{&Rect{w: 2, h: 2}, &Circle{r: 1}}
	fmt.Println(totalArea(1, shapes...))

	// A bare `nil` is ONE variadic element, in every position — alone, leading, trailing, and
	// repeated — and its arity must survive the emission. database/sql's
	// `exec(t, db, "INSERT|t|id=10,name=?", nil)` lost exactly this argument and the driver
	// answered "expected 1 arguments, got 0".
	fmt.Println(countArgs(nil))
	fmt.Println(countArgs(nil, 1))
	fmt.Println(countArgs(1, nil))
	fmt.Println(countArgs(nil, nil, nil))

	// The no-argument and typed-value controls: the pack is genuinely empty only when Go says so.
	fmt.Println(countArgs())
	fmt.Println(countArgs(1, 2))

	// The same question for a NAMED interface element type, including a spread control whose
	// slice really is nil — there `nil...` IS the whole pack, so length 0 is correct.
	fmt.Println(describe(nil))
	fmt.Println(describe(nil, nil))
	var none []Shape
	fmt.Println(describe(none...))
}
