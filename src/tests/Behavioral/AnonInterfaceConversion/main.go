package main

import "fmt"

type T struct{ n int }

func (t T) Foo() int { return t.n }

func main() {
	p := 42
	t := T{7}

	// 1. bare empty-interface CONVERSION (the routed defect's spelling)
	a := interface{}(p)

	// 2. the ASSIGNMENT form, said to be correct today
	var b interface{} = p

	// 3. anonymous METHOD-BEARING interface conversion (the lift's usual trigger)
	c := interface{ Foo() int }(t)

	// 4. its assignment form
	var d interface{ Foo() int } = t

	fmt.Println(a, b, c.Foo(), d.Foo())
}
