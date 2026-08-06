package main

import "fmt"

func main() {
	f()
	panicValues()
	fmt.Println("Returned normally from f.")
}

func f() {
	defer func() {
		if r := recover(); r != nil {
			fmt.Println("Recovered in f", r)
		}
	}()
	fmt.Println("Calling g.")
	g(0)
	fmt.Println("Returned normally from g.")
}

func g(i int) {
	if i > 3 {
		fmt.Println("Panicking!")
		panic(fmt.Sprintf("%v", i))
	}
	defer fmt.Println("Defer in g", i)
	fmt.Println("Printing in g", i)
	g(i + 1)
}

// --- panic VALUE TYPE ---------------------------------------------------------------------------
// Go's panic takes an `any`, so the value's DYNAMIC TYPE is observable on the recover side: a
// comparison against a string, a type assertion, and a `case string:` arm all test it. A C# string
// reaching golib's panic boxing boundary (from a bare literal — the emission deliberately does not
// u8-suffix a panic argument — or from a stub returning C# string, e.g. fmt.Sprintf) used to box as
// System.String and matched nothing on the recover side, which compares against @string: sync's
// testOncePanicX reported the self-contradictory "want panic x, got x". golib now normalizes at the
// boxing boundary, so every spelling below agrees with Go.

func panicValueKind(f func()) string {
	var out string
	func() {
		defer func() {
			p := recover()
			switch v := p.(type) {
			case nil:
				out = "no panic"
			case string:
				out = fmt.Sprintf("string(%s) eq-x=%v", v, v == "x")
			case error:
				out = "error(" + v.Error() + ")"
			case int:
				out = fmt.Sprintf("int(%d)", v)
			default:
				out = "other (not a plain string)"
			}
		}()
		f()
	}()
	return out
}

func panicValues() {
	// A bare string LITERAL.
	fmt.Println(panicValueKind(func() { panic("x") }))
	// A computed string (a stub-returning-C#-string path in the converted runtime).
	fmt.Println(panicValueKind(func() { panic(fmt.Sprintf("%s", "x")) }))
	// A string VARIABLE, which already carries Go's string type.
	fmt.Println(panicValueKind(func() { s := "x"; panic(s) }))
	// A named string type must NOT collapse to plain string.
	type label string
	fmt.Println(panicValueKind(func() { panic(label("x")) }))
	// Non-string values are untouched.
	fmt.Println(panicValueKind(func() { panic(42) }))
	fmt.Println(panicValueKind(func() {}))
}
