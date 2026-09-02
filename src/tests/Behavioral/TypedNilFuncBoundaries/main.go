package main

import "fmt"

// A nil FUNC inside an interface is a value WITH a dynamic type: `any((func())(nil))` is a NON-nil
// interface whose %T prints the func type. A Go func lowers to a managed delegate whose nil is a
// bare null, which carries nothing once boxed — so every slot where a func crosses into an empty
// interface has to carry the type across deliberately.
//
// This guards the SLOTS. The predicate that decides which values need carrying is guarded by the
// non-null half at the bottom, and the split that matters is `holder.fn` (a func-typed FIELD, which
// can be nil) against `holder.method` (a method value, which cannot) — both selector expressions,
// and treating them alike is wrong in one direction or the other.

type holder struct {
	fn func(int) int
}

func (h holder) method() {}

type row struct {
	v  any
	ok bool
}

func take(v any) any { return v }

func declared(x int) int { return x }

func main() {
	var zero func(int) int

	// ---- the slots: each must report a NON-nil interface, as Go does ----

	// 1. a declared `any` parameter
	argSlot := take(zero)

	// 2. an element of an []any composite literal. This is the one that was measurably WRONG
	//    before the widening: it emitted a bare null and compared equal to nil.
	elemSlot := []any{zero}

	// 3. a POSITIONAL `any` field of a struct composite literal
	fieldSlot := []row{{zero, true}}

	// 4. a KEYED `any` slot (a map value) — already carried before the widening; here so a
	//    regression in the older path fails too
	keyedSlot := map[string]any{"k": zero}

	// 5. append into an []any
	appendSlot := append([]any{}, zero)

	fmt.Println("arg    ", argSlot == nil)
	fmt.Println("elem   ", elemSlot[0] == nil)
	fmt.Println("field  ", fieldSlot[0].v == nil)
	fmt.Println("keyed  ", keyedSlot["k"] == nil)
	fmt.Println("append ", appendSlot[0] == nil)

	// A nil CONVERSION is not exempt, and this is where the func arm differs from the pointer arm:
	// `(*T)(nil)` already renders the canonical typed nil, while `(func())(nil)` renders a cast of
	// null — precisely the shape that loses the type word.
	fmt.Println("conv   ", take((func())(nil)) == nil)

	// ---- the predicate: values that provably cannot be null take NOTHING ----
	// Emitting the carrier for a method group does not merely add noise; an extension method
	// cannot be invoked on a method group at all.

	var h holder

	fmt.Println("declared", take(declared) != nil)
	fmt.Println("qualified", take(fmt.Sprint) != nil)
	fmt.Println("methodval", take(h.method) != nil)
	fmt.Println("literal ", take(func() {}) != nil)

	// THE SPLIT. Both are selector expressions; only one can be nil.
	fmt.Println("fieldfn ", take(h.fn) == nil)

	h.fn = declared
	fmt.Println("fieldset", take(h.fn) == nil)
}
