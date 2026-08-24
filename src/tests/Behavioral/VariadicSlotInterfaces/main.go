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

// anyList is the NAMED-slice control for nest/countArgs below: it renders as its own generated
// wrapper type, whose only route to the emitted `params Span<any>` collection would be
// wrapper→slice<any>→any[]→Span<any> — two user-defined conversions, which C# never composes —
// so it is structurally immune to the flip the unnamed slice suffers.
type anyList []any

// nest mirrors html/template jsValEscaper's shape: a `...any` function whose OUTPUT, not merely
// its arity, changes with the pack length. jsValEscaper's test nests every case as `[]any{x}` and
// expects one more level of array wrapping; when the slice is spread instead of passed as one
// element, exactly one level disappears (`"[42]"` renders as `" 42 "`).
func nest(args ...any) string {
	if len(args) == 1 {
		return fmt.Sprintf("[%v]", args[0])
	}

	return fmt.Sprintf("<%d>%v", len(args), args)
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

	// A SLICE of the variadic ELEMENT type, passed WITHOUT `...`, is ONE element: Go spreads only
	// on an explicit `a...`. C# 13 agreed by accident — `slice<any>` had no conversion to the
	// emitted `params Span<any>` collection, so only the expanded form was applicable. C# 14 made
	// the array→span hop a STANDARD conversion, which lets golib's `implicit operator T[]` chain
	// into it (`slice<any>` → `any[]` → `Span<any>`); the callee became applicable in its NORMAL
	// form, which C# prefers, and the pack silently arrived spread. html/template's
	// TestJSValEscaper caught it as one missing level of array wrapping on every nesting case.
	anys := []any{1, "two", nil}
	fmt.Println(countArgs(anys))
	fmt.Println(nest(anys))

	// The spread control: `anys...` really IS the whole pack, so three elements is correct.
	fmt.Println(countArgs(anys...))
	fmt.Println(nest(anys...))

	// A nil slice is still a VALUE — one element, not an empty pack — and the spread of one is
	// genuinely empty. The pair separates "passed whole" from "spread" at the boundary case.
	var noAnys []any
	fmt.Println(countArgs(noAnys))
	fmt.Println(countArgs(noAnys...))

	// Arity controls. A tail of two arguments cannot bind the normal form at all, so it was never
	// at risk; it pins that the fix did not change the ordinary path.
	fmt.Println(countArgs(1, anys))
	fmt.Println(nest(1, anys))

	// An ARRAY of the element type reaches the same chain through golib's `array<T>` operator.
	arr := [2]any{7, 8}
	fmt.Println(countArgs(arr))
	fmt.Println(nest(arr))

	// The NAMED-slice control: immune by construction (two user-defined conversions), and the
	// converter's cast rule deliberately excludes it — so this line pins the exclusion.
	named := anyList{1, "two"}
	fmt.Println(countArgs(named))
	fmt.Println(nest(named))
}
