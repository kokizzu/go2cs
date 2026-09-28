package main

import "fmt"

// A NAMED empty interface declared with `interface{}` holds any value, exactly as `any` does: every
// type implements it. Values reach it by assignment, as arguments, and as return values, and come
// back out through type assertions and switches.

type I interface{}

type point struct{ X, Y int }

func show(v I) { fmt.Printf("I: %T %v\n", v, v) }

func anon(v interface{}) { fmt.Printf("interface{}: %T %v\n", v, v) }

func back(n int8) I { return n }

func pick(which int) I {
	switch which {
	case 0:
		return 42
	case 1:
		return "go"
	case 2:
		return point{1, 2}
	}

	return nil
}

func main() {
	// Assignment of each kind of value to the named interface.
	var a I = int8(-5)
	var b I = 42
	var c I = "go"
	var d I = point{3, 4}
	var e I
	fmt.Printf("%T %v | %T %v | %T %v | %T %v | %T %v\n", a, a, b, b, c, c, d, d, e, e)

	// As arguments, through the named interface and through an anonymous interface{} parameter.
	show(int8(7))
	show(9)
	show("s")
	show(point{5, 6})
	show(nil)
	anon(int8(7))
	anon(9)
	anon("s")
	anon(point{5, 6})
	anon(nil)

	// As return values.
	fmt.Printf("%T %v\n", back(-1), back(-1))
	for i := 0; i < 4; i++ {
		show(pick(i))
	}

	// Reassignment, comparison, and the values coming back out.
	e = a
	fmt.Println(e == a, e == b, e == nil)
	if n, ok := b.(int); ok {
		fmt.Println("int", n+1)
	}
	switch v := d.(type) {
	case point:
		fmt.Println("point", v.X+v.Y)
	default:
		fmt.Println("other")
	}

	// A slice and a map of the named interface.
	list := []I{int8(1), 2, "three", point{4, 4}, nil}
	fmt.Println(len(list), list)
	m := map[string]I{"n": 1, "s": "one"}
	fmt.Println(m["n"], m["s"])
}
