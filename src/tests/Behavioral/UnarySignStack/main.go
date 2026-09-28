package main

import "fmt"

// A unary minus (or plus) whose operand is itself a unary minus (or plus). Go reads `- -a` as two
// negations and leaves a alone; C# lexes `--a` and `++a` as a pre-decrement and a pre-increment,
// which MUTATE a. Every line prints the expression's value and then a, so a mutation shows.

type Celsius float64

type Level int16

const k = 4

func main() {
	a := 5
	fmt.Println(- -a, a)
	fmt.Println(+ +a, a)

	var b int8 = -7
	fmt.Println(- -b, b)
	fmt.Println(+ +b, b)

	var f float64 = 2.5
	fmt.Println(- -f, f)
	fmt.Println(+ +f, f)

	var c Celsius = 36.5
	fmt.Println(- -c, c)
	fmt.Println(+ +c, c)

	var l Level = 3
	fmt.Println(- -l, l)
	fmt.Println(+ +l, l)

	// Parenthesized forms, and three signs deep.
	fmt.Println(-(-a), +(+a), a)
	fmt.Println(- - -a, + + +a, a)

	// Constants: the operand is a signed literal or a named constant.
	fmt.Println(- -1, + +1, - -k, + +k)

	// Assigned rather than printed.
	x := - -a
	y := + +b
	fmt.Println(x, y, a, b)

	// Mixed signs never stacked the same character, and stay as they were.
	fmt.Println(-+a, +-a, a)
}
