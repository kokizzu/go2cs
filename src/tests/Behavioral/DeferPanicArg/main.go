package main

import "fmt"

type myErr struct {
	msg string
}

func (e *myErr) Error() string {
	return e.msg
}

// A plain VALUE argument: the deferred panic unwinds with it.
func valueCapture() {
	defer func() { fmt.Println("valueCapture recovered:", recover()) }()
	defer panic(42)
}

// The defining case: the variable is REASSIGNED after the defer statement. Go evaluates a
// deferred call's arguments when the DEFER statement runs, so "first" is what unwinds --
// capturing the expression in the thunk instead would report "second".
func reassignedAfterDefer() {
	defer func() { fmt.Println("reassignedAfterDefer recovered:", recover()) }()
	err := fmt.Errorf("first")
	defer panic(err)
	err = fmt.Errorf("second")
	_ = err
}

// A COMPUTED expression is evaluated at defer time too, against the operand values held then.
func computedExpr() {
	defer func() { fmt.Println("computedExpr recovered:", recover()) }()
	n := 3
	defer panic(n*7 + 1)
	n = 100
	_ = n
}

// A POINTER panic value crosses the `any` boundary and must still answer a type assertion
// after the round trip through the deferred argument slot.
func pointerValue() {
	defer func() {
		r := recover()
		if e, ok := r.(*myErr); ok {
			fmt.Println("pointerValue recovered typed:", e.msg)
		} else {
			fmt.Println("pointerValue recovered UNTYPED:", r)
		}
	}()
	p := &myErr{msg: "boxed"}
	defer panic(p)
	p = &myErr{msg: "replaced"}
	_ = p
}

// A deferred panic that runs while another panic is already unwinding replaces it: the
// value the caller recovers is the deferred one.
func replacesInFlight() {
	defer func() { fmt.Println("replacesInFlight recovered:", recover()) }()
	func() {
		defer panic("from defer")
		panic("original")
	}()
}

// Several deferred panics in one frame run last-registered-first; the value that survives to
// the caller is the one thrown by the LAST deferred panic to run.
func multiple() {
	defer func() { fmt.Println("multiple recovered:", recover()) }()
	a := "A"
	b := "B"
	defer panic(a)
	defer panic(b)
	a = "A-changed"
	b = "B-changed"
}

func main() {
	valueCapture()
	reassignedAfterDefer()
	computedExpr()
	pointerValue()
	replacesInFlight()
	multiple()
}
