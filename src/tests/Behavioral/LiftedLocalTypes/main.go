package main

import "fmt"

// describe takes `any`, so a func-literal argument is natural-typed by C# — the emission must
// state the literal's declared Go result type explicitly or inference picks the arms' literal
// type (`return 0` → C# int = Go int32), collapsing distinct Go func types under reflection
// (testing/quick's TestFailure #3).
func describe(f any) {
	_ = f
	fmt.Println("ok")
}

func main() {
	// Structurally identical anonymous struct types are ONE Go type: both occurrences must
	// lift to a single C# type or the assignment below cannot compile, and reflect.Type
	// identity splits per occurrence (encoding/binary's TestSizeStructCache cache counting).
	a := struct{ X int }{X: 1}
	var b struct{ X int }
	b = a
	fmt.Println(a == b)

	// A function-local named type keeps its Go source name for reflection: %T must print
	// main.point, never the lifted C# identifier (encoding/binary's TestNoFixedSize asserts
	// the exact error text this feeds). Printing it is what pins the [GoLocalName] stamp —
	// the stamp itself rides package_info.cs, which has no golden, so an emission-level pin
	// would guard nothing.
	type point struct{ X, Y int }
	p := point{X: 1, Y: 2}
	fmt.Println(p.X + p.Y)
	fmt.Printf("%T %T\n", p, &p)

	describe(func(x int) int { return 0 })
}
