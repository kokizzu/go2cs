package main

import "fmt"

// Guards CANONICAL typed-nil boxing (reflection-bridge Phase-3, X2): a nil pointer
// converted into interface space keeps its Go dynamic type — any((*T)(nil)) != nil,
// %T prints *T, typed nils of one type are equal while different types are not, and
// a type switch matches the nil-holding case. The old emission boxed a bare null,
// erasing the type.
//
// The second half of the file (r39) guards the general rule the first half is a case
// of: a POINTER entering interface space carries its static type HOWEVER it was
// produced — a declared variable, a struct field, a slice element, a map miss — and
// through every slot that boxes one, including the three built-ins (panic, append,
// delete) whose arguments never pass a declared parameter.

type AErr struct{ n int }

func (e *AErr) Error() string { return "a" }

type BErr struct{ n int }

func (e *BErr) Error() string { return "b" }

func main() {
	// Bare any boxing of a typed nil keeps the type.
	var x any = (*int)(nil)
	fmt.Printf("%T\n", x)
	fmt.Println("x==nil", x == nil)

	// Two typed nils of the same type compare equal as interfaces.
	var y any = (*int)(nil)
	fmt.Println("x==y", x == y)

	// Pointer-level compares still see nil.
	var p *int
	fmt.Println("p==typednil", p == (*int)(nil))

	// error interfaces holding DIFFERENT typed nils: non-nil, and unequal to each other.
	var e1 error = (*AErr)(nil)
	var e2 error = (*BErr)(nil)
	fmt.Println("e1==nil", e1 == nil)
	fmt.Println("e2==nil", e2 == nil)
	fmt.Println("e1==e2", e1 == e2)

	// The same typed nil across the any/error boundary compares equal.
	var ea any = (*AErr)(nil)
	fmt.Println("ea==e1", ea == e1)

	// A pointer-to-TYPE-LITERAL target — `(*[]byte)(nil)`, `(*struct{…})(nil)`,
	// `(*map[string]int)(nil)` (encoding/gob's bootstrapType table). A composite target has no
	// types.Object, so these were not claimed as conversions and rendered as a bare cast of
	// `default!` — a NULL reference, erasing the type: gob's package init died on
	// `reflect.TypeOf((*[]byte)(nil)).Elem()`.
	var sp any = (*[]byte)(nil)
	var sp2 any = (*[]byte)(nil)
	var mp any = (*map[string]int)(nil)
	var stp any = (*struct{ r int })(nil)
	fmt.Println("sp==nil", sp == nil)
	fmt.Println("sp==sp2", sp == sp2)
	fmt.Println("sp==mp", sp == mp)
	fmt.Println("stp==nil", stp == nil)

	// A type switch matches the nil-holding case with a nil pointee.
	switch v := e1.(type) {
	case *AErr:
		fmt.Println("switch-AErr", v == nil)
	default:
		fmt.Println("switch-other")
	}

	// ---- However the nil was PRODUCED ----
	//
	// The typed-nil representation above is reached by a `(*T)(nil)` conversion EXPRESSION. A
	// pointer's ZERO value — a declared variable, a struct field, a slice element, a map miss —
	// is a plain null with no type on it, and Go draws no distinction: every one of these is a
	// typed nil the moment it enters interface space. So the carry happens at the BOUNDARY, at
	// each slot that boxes a pointer into an interface, not at the sites that mint nil.
	var dp *int
	var st struct{ P *AErr }
	sl := make([]*BErr, 1)
	mm := map[string]*AErr{}

	// call argument (incl. the variadic ...any of the fmt family)
	fmt.Printf("%T %T %T %T\n", dp, st.P, sl[0], mm["missing"])
	fmt.Println("printf-v", fmt.Sprintf("%v|%v", dp, st.P))

	// var spec, then plain assignment
	var dpi any = dp
	fmt.Println("dpi==nil", dpi == nil, "dpi==x", dpi == x)
	dpi = st.P
	fmt.Println("assigned==nil", dpi == nil)

	// return value
	fmt.Println("returned==nil", boxed(sl[0]) == nil)

	// composite-literal element, keyed struct field, map value, map key
	anys := []any{dp, st.P}
	holder := struct{ V any }{V: dp}
	byName := map[string]any{"p": dp}
	byKey := map[any]int{dp: 7}
	fmt.Println("elem==nil", anys[0] == nil, "field==nil", holder.V == nil)
	fmt.Println("mapval==nil", byName["p"] == nil, "mapkey", byKey[dp], byKey[(*int)(nil)])

	// channel send
	ch := make(chan any, 1)
	ch <- dp
	fmt.Println("chan==nil", (<-ch) == nil)

	// A type assertion back out of the interface succeeds, with a nil pointee.
	if q, ok := dpi.(*AErr); ok {
		fmt.Println("assert-ok", q == nil)
	} else {
		fmt.Println("assert-failed")
	}

	// Three BUILT-INs take an `any` slot without ever passing a declared parameter, so each needs
	// the boundary applied at its own emission: panic's value, an `[]any` append element, and an
	// `any`-keyed map's delete key.
	fmt.Println(panicked())

	acc := append([]any{}, dp, st.P)
	fmt.Printf("append %T %T %v %v\n", acc[0], acc[1], acc[0] == nil, acc[1] == nil)

	// A POSITIONAL element of a struct literal whose field is `any` — the shape of gob's
	// TestNilPointerPanics table, where every nil-pointer row has to reach gob as a TYPED nil.
	rows := []struct {
		value any
		want  bool
	}{
		{dp, true},
		{st.P, true},
		{7, false},
	}
	for _, r := range rows {
		fmt.Printf("row %T %v %v\n", r.value, r.value == nil, r.want)
	}

	dm := map[any]int{dp: 1, st.P: 2}
	delete(dm, dp)
	fmt.Println("delete", len(dm), dm[st.P], dm[dp])
}

func boxed(p *BErr) any { return p }

// A nil pointer PANIC value is a typed nil like any other: recover() hands back a non-nil interface
// carrying *AErr, so the assertion succeeds with a nil pointee.
func panicked() (msg string) {
	defer func() {
		r := recover()
		if q, ok := r.(*AErr); ok {
			msg = fmt.Sprintf("panic-recovered *AErr nil=%v", q == nil)
		} else {
			msg = fmt.Sprintf("panic-recovered other %T %v", r, r == nil)
		}
	}()

	var p *AErr
	panic(p)
}
