// ParenIifeNilFuncConv guards two independent emission gaps that net/http's converted test suite
// met, both of which have exactly one correct answer and no design question attached.
//
//  1. A PARENTHESIZED immediately-invoked function literal — `(func(){ … })()`, Go's own idiomatic
//     spelling. go/parser puts a ParenExpr in the call's Fun, and the IIFE interception asserted the
//     type directly, so it declined: the general call path then rendered a bare C# lambda and
//     appended `(args)`, which cannot be invoked (CS0149, "Method name expected"). The two spellings
//     are the same program and must convert alike.
//
//  2. `T(nil)` where T is a named FUNC type. go2cs emits such a type as a C# delegate, and the
//     named-func-type conversion composed `new T(expr)` unconditionally — a delegate CREATION,
//     which accepts a method group or a delegate value and neither describes `nil` (CS0149), and
//     which gives the default literal no target type (CS8716). Go's `T(nil)` is just the typed nil
//     delegate; a cast is the whole conversion. net/http's server_test.go writes `HandlerFunc(nil)`
//     as a table entry precisely to exercise a nil handler.
package main

import "fmt"

type handlerFunc func(string) int

// A method on the named func type, so the nil value can be observed being CARRIED rather than only
// compared — a typed nil delegate must still reach its value-receiver method.
func (h handlerFunc) call(s string) int {
	if h == nil {
		return -1
	}

	return h(s)
}

func main() {
	// The parenthesized IIFE, niladic.
	(func() {
		fmt.Println("paren iife")
	})()

	// The parenthesized IIFE with arguments and a result.
	sum := (func(a, b int) int { return a + b })(3, 4)
	fmt.Println(sum)

	// The UNPARENTHESIZED spelling, which always worked — kept as the control.
	func() { fmt.Println("bare iife") }()

	product := func(a, b int) int { return a * b }(3, 4)
	fmt.Println(product)

	// The typed nil delegate.
	h := handlerFunc(nil)
	fmt.Println(h == nil, h.call("x"))

	// The same named type built from a real literal — the creation form, which was always right.
	h = handlerFunc(func(s string) int { return len(s) })
	fmt.Println(h == nil, h.call("abcd"))

	// A typed nil in a composite, the shape net/http's table uses.
	table := []handlerFunc{handlerFunc(nil), h}
	for _, entry := range table {
		fmt.Println(entry.call("zz"))
	}
}
