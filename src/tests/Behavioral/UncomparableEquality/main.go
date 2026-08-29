// Regression test: `==` between two interface values whose shared dynamic type is
// UNCOMPARABLE panics with Go's runtime error, recoverably.
//
// Go gives an interface comparison a three-step rule, and the transpiled C# only
// implemented the first two: a nil operand answers without panicking, a dynamic-type
// MISMATCH answers false without panicking, and only a matching pair of uncomparable
// dynamic types reaches the type's equal algorithm — which does not exist, so the
// runtime panics "runtime error: comparing uncomparable type T". golib's AreEqual
// detected just one shape of that (two adapters over a nil named-func delegate) and
// answered every other one quietly with a bool: a map, slice or func held in an
// interface, and a struct or array that transitively CONTAINS one.
//
// The panic is a recoverable runtime error, so recover() observes it and the message
// names the dynamic type in Go's own spelling — including an array's LENGTH
// ([1][]int, not [][]int) and a named type's own name (main.myMap, not
// map[string]int). Both are asserted below, since the managed array<T> does not carry
// its length in its type and the name had to be recovered from the live value.
package main

import "fmt"

// ---- named types whose underlying type is uncomparable (panic under their OWN name) ----

type myMap map[string]int

type mySlice []int

type myFunc func(int) string

// ---- structs that contain an uncomparable field, directly and transitively ----

type withSlice struct {
	A int
	B []int
}

type withMap struct {
	A int
	M map[string]int
}

type withFunc struct {
	F func()
}

type inner struct {
	S []byte
}

type outer struct {
	I inner
	N int
}

// ---- comparable positive controls ----

type point struct {
	X int
	Y int
}

// withAny is COMPARABLE by Go's static rule (an interface field is comparable), so the
// comparison proceeds and panics naming the field's INNER dynamic type. It guards the
// recoverability of the panic across a reflection boundary: the emitted Equals compares
// an interface-typed field back through golib's AreEqual, and the outer comparison
// reaches that Equals through a reflective operator invoke — so the panic is raised one
// frame INSIDE the invoke. Wrapped by reflection it arrives as a TargetInvocationException,
// which recover() does not match, and the panic escapes every deferred recover and kills
// the process. Measured: before the fix this exact shape exited 2 with an unrecovered
// traceback where Go recovers cleanly.
type withAny struct {
	A int
	V any
}

type sliceErr struct{ S []int }

func (sliceErr) Error() string { return "sliceErr" }

// check runs f, reporting either the recovered panic value or that none occurred.
func check(name string, f func()) {
	defer func() {
		if r := recover(); r != nil {
			fmt.Printf("%-24s PANIC: %v\n", name, r)
			return
		}
		fmt.Printf("%-24s no panic\n", name)
	}()
	f()
}

// checkPanicOnly reports only WHETHER f panicked, never the message.
//
// Reserved for the one shape whose message the conversion cannot spell. A METHODLESS
// named func type is emitted inline as its base delegate — the converter says so in
// the generated main.cs — so no managed type carries the name `main.myFunc`, and the
// panic names the type structurally (`func(int) string`) where Go names it
// `main.myFunc`. That is an emission-level erasure of the type's identity rather than
// an equality defect: `%T` of the very same value diverges identically and by the same
// cause (measured: Go `main.myFunc`, C# `func(int) string`, while a named MAP type
// prints `main.myMap` on both sides), and it is equally visible through reflect.
//
// What this case does guard is the part that belongs to equality: a named func type
// held in an interface must PANIC rather than answer quietly, which is the defect the
// gate closes. Asserting its spelling as well would bake an unrelated converter
// limitation into an equality test.
func checkPanicOnly(name string, f func()) {
	defer func() {
		if r := recover(); r != nil {
			fmt.Printf("%-24s PANIC\n", name)
			return
		}
		fmt.Printf("%-24s no panic\n", name)
	}()
	f()
}

func main() {
	fmt.Println("== uncomparable dynamic types: Go panics ==")

	var m any = map[string]int{"a": 1}
	check("map", func() { _ = m == m })

	var s any = []int{1, 2}
	check("slice", func() { _ = s == s })

	var fn any = func() {}
	check("func", func() { _ = fn == fn })

	var nm any = myMap{"a": 1}
	check("named map", func() { _ = nm == nm })

	var nsl any = mySlice{1}
	check("named slice", func() { _ = nsl == nsl })

	var nf any = myFunc(func(int) string { return "" })
	checkPanicOnly("named func", func() { _ = nf == nf })

	var ws any = withSlice{1, []int{2}}
	check("struct w/ slice", func() { _ = ws == ws })

	var wm any = withMap{1, map[string]int{}}
	check("struct w/ map", func() { _ = wm == wm })

	var wf any = withFunc{func() {}}
	check("struct w/ func", func() { _ = wf == wf })

	var nested any = outer{inner{[]byte("x")}, 1}
	check("struct w/ struct", func() { _ = nested == nested })

	fmt.Println()
	fmt.Println("== array length is part of the reported type ==")

	var aos any = [1][]int{{1}}
	check("array of slice", func() { _ = aos == aos })

	var aom any = [2]map[string]int{}
	check("array of map", func() { _ = aom == aom })

	var a2d any = [2][3][]int{}
	check("2-D array of slice", func() { _ = a2d == a2d })

	var aostruct any = [3]withSlice{}
	check("array of struct", func() { _ = aostruct == aostruct })

	fmt.Println()
	fmt.Println("== interface FIELD panics naming the inner type, recoverably ==")

	// The struct is comparable, so the panic names the FIELD's dynamic type, not the
	// struct's — and it is raised inside a reflective invoke, so this is also the
	// recoverability guard described on withAny.
	var wa any = withAny{1, map[string]int{}}
	check("struct w/ any=map", func() { _ = wa == wa })

	var wa2 any = withAny{1, []int{1}}
	check("struct w/ any=slice", func() { _ = wa2 == wa2 })

	// the same struct with a COMPARABLE payload must still answer, not panic
	var wc1, wc2, wc3 any = withAny{1, 5}, withAny{1, 5}, withAny{1, 6}
	fmt.Println("struct w/ any=int:", wc1 == wc2, wc1 == wc3)

	fmt.Println()
	fmt.Println("== non-empty interface ==")

	var e error = sliceErr{[]int{1}}
	check("error w/ slice", func() { _ = e == e })

	fmt.Println()
	fmt.Println("== nil compares never panic ==")

	check("map == nil", func() { _ = m == nil })
	check("slice == nil", func() { _ = s == nil })
	check("func == nil", func() { _ = fn == nil })
	check("struct w/ slice == nil", func() { _ = ws == nil })
	check("array of slice == nil", func() { _ = aos == nil })

	var nilIface any
	check("nil iface == map", func() { _ = nilIface == m })
	check("nil iface == nil iface", func() { _ = nilIface == nilIface })

	fmt.Println()
	fmt.Println("== differing dynamic types never panic ==")

	var i any = 5
	check("map == int", func() { _ = m == i })
	check("slice == map", func() { _ = s == m })
	check("int == slice", func() { _ = i == s })

	fmt.Println()
	fmt.Println("== comparable values still compare ==")

	var i1, i2, i3 any = 5, 5, 6
	fmt.Println("int:      ", i1 == i2, i1 == i3)

	var s1, s2, s3 any = "hi", "hi", "ho"
	fmt.Println("string:   ", s1 == s2, s1 == s3)

	var b1, b2 any = true, false
	fmt.Println("bool:     ", b1 == b1, b1 == b2)

	var f1, f2 any = 1.5, 2.5
	fmt.Println("float:    ", f1 == f1, f1 == f2)

	var p1, p2, p3 any = point{1, 2}, point{1, 2}, point{1, 3}
	fmt.Println("struct:   ", p1 == p2, p1 == p3)

	var arr1, arr2, arr3 any = [2]int{1, 2}, [2]int{1, 2}, [2]int{1, 3}
	fmt.Println("array:    ", arr1 == arr2, arr1 == arr3)

	x, y := 1, 2
	var ptr1, ptr2, ptr3 any = &x, &x, &y
	fmt.Println("pointer:  ", ptr1 == ptr2, ptr1 == ptr3)

	// A pointer TO an uncomparable value is itself comparable.
	pm := map[string]int{}
	var pmap1, pmap2 any = &pm, &pm
	fmt.Println("ptr-to-map:", pmap1 == pmap2)

	ch1, ch2 := make(chan int), make(chan int)
	var c1, c2, c3 any = ch1, ch1, ch2
	fmt.Println("chan:     ", c1 == c2, c1 == c3)

	// An interface holding an interface holding a comparable value.
	var e1, e2 any = any(7), any(7)
	fmt.Println("iface:    ", e1 == e2)

	// A struct whose fields are all comparable but of composite kinds.
	type nestedComparable struct {
		P point
		A [2]int
	}
	var nc1, nc2 any = nestedComparable{point{1, 2}, [2]int{3, 4}}, nestedComparable{point{1, 2}, [2]int{3, 4}}
	fmt.Println("nested:   ", nc1 == nc2)

	fmt.Println()
	fmt.Println("== the verdict is stable across repeated comparisons ==")

	// The comparability verdict is cached per type; probe each shape twice so a cache
	// that returned a stale or inverted answer on the second read would show up here.
	for i := 0; i < 2; i++ {
		check("repeat map", func() { _ = m == m })
		check("repeat comparable", func() { _ = i1 == i2 })
	}
}
