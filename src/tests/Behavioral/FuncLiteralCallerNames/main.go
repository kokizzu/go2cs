// FuncLiteralCallerNames pins the FUNCTION half of a frame INSIDE a function literal: Go names an
// anonymous literal `Outer.funcN` — a per-enclosing-function, source-order counter starting at 1 —
// and a literal nested inside another `Outer.funcN.M`, each nesting level appending its own
// per-parent counter (measured against go1.23.12: `main.nested.func1.1`, `main.deep.func1.1.1`,
// `main.nestedSiblings.func1.2`). The converter RECORDS each literal's suffix and Go line span in
// the file's GoPositionMap record, and the runtime reads the record instead of deriving an ordinal
// from Roslyn's compiler-generated lambda name, whose closure-group numbering matches Go's counter
// only by coincidence.
//
// Every named function here is //go:noinline: gc renames a closure whose ENCLOSING function is
// inlined into its caller (`main.main.deep.func2.1.1` under default optimization on this exact
// program), and go2cs performs no inlining, so the un-inlined naming is the semantics being pinned.
package main

import (
	"fmt"
	"runtime"
)

// who spells the runtime name of ITS CALLER's frame — the function literal that invoked it.
// CallersFrames rather than FuncForPC because the converted runtime resolves names through
// Frame.Function (FuncForPC stays nil there by design). Callers skip=2: 0 is Callers itself,
// 1 is who, 2 is who's caller.
//
//go:noinline
func who() string {
	pc := make([]uintptr, 1)

	if runtime.Callers(2, pc) == 0 {
		return ""
	}

	frames := runtime.CallersFrames(pc)
	frame, _ := frames.Next()

	return frame.Function
}

// Two sibling literals in one function: func1, func2 — source order, from 1, and NOT from
// Roslyn's per-closure-group index (the fallback derivation answered func0 here).
//
//go:noinline
func siblings() {
	f1 := func() { fmt.Println("sibling-1:", who()) }
	f2 := func() { fmt.Println("sibling-2:", who()) }
	f1()
	f2()
}

// A nested literal appends a per-parent counter (func1.1) and does NOT consume a top-level
// number: the sibling declared AFTER the nest is func2.
//
//go:noinline
func nested() {
	outer := func() {
		inner := func() { fmt.Println("nested-inner:", who()) }
		fmt.Println("nested-outer:", who())
		inner()
	}
	outer()
	after := func() { fmt.Println("after-nest:", who()) }
	after()
}

// The counter restarts at 1 for each enclosing named function.
//
//go:noinline
func second() {
	g := func() { fmt.Println("second-fn:", who()) }
	g()
}

// Three levels of nesting: func1, func1.1, func1.1.1.
//
//go:noinline
func deep() {
	l1 := func() {
		l2 := func() {
			l3 := func() { fmt.Println("deep-3:", who()) }
			l3()
			fmt.Println("deep-2:", who())
		}
		l2()
		fmt.Println("deep-1:", who())
	}
	l1()
}

// Two siblings inside one nested literal: func1.1 and func1.2.
//
//go:noinline
func nestedSiblings() {
	o := func() {
		a := func() { fmt.Println("nest-sib-a:", who()) }
		b := func() { fmt.Println("nest-sib-b:", who()) }
		a()
		b()
	}
	o()
}

// run invokes its argument, so viaArg's literal stays a first-class function value — in C# a real
// lambda rather than the local function the only-ever-called literals above emit as; both emissions
// must answer the recorded name.
//
//go:noinline
func run(f func()) { f() }

//go:noinline
func viaArg() {
	run(func() { fmt.Println("via-arg:", who()) })
}

// A deferred literal is still a source literal and takes the next counter value; its frame is
// read while the enclosing function's defer list runs. The first line is the negative control: a
// NAMED function's own frame keeps its plain name, so the recording cannot leak into non-literal
// frames.
//
//go:noinline
func deferred() {
	fmt.Println("named-control:", who())

	defer func() { fmt.Println("deferred:", who()) }()
}

func main() {
	siblings()
	nested()
	second()
	deep()
	nestedSiblings()
	viaArg()
	deferred()
}
