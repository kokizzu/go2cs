package main

import "fmt"

// A nil value of a NAMED func type inside an interface keeps its type's METHOD SET: `any(Fn(nil))`
// holds (type=Fn, value=nil), and every interface Fn's methods satisfy is satisfied by it. A method
// that never reads its receiver runs on the nil value -- fmt's TestSprintf pins exactly this with
// `{"%v", fnValue, "String(fn)"}`, where fnValue is a nil Fn whose String method is fmt's Stringer.
//
// TypedNilFuncBoundaries guards the SLOTS a nil func crosses into interface space through; this
// guards what the carried type ANSWERS once it is there: the assertion to an interface, fmt's
// Stringer dispatch direct and through reflect (the array element), a type switch, and the misses
// Go also misses -- an unnamed func type has no methods, and a method set the type lacks stays lacking.

type Fn func() int

func (fn Fn) String() string { return "String(fn)" }

type Getter interface{ Get() int }

type G func() int

func (g G) Get() int { return 7 }

type Both interface {
	Get() int
	String() string
}

func main() {
	var fn Fn

	// fmt's Stringer dispatch: direct, in Println, and through reflect for an array element.
	fmt.Printf("%v\n", fn)
	fmt.Println(fn)
	fmt.Printf("%v\n", [1]Fn{fn})
	fmt.Printf("%s|%T\n", fn, fn)

	// The assertion itself, comma-ok, and the method called on the nil receiver.
	var x any = fn

	s, ok := x.(fmt.Stringer)
	fmt.Println("stringer", ok, s != nil)

	if ok {
		fmt.Println("string  ", s.String())
	}

	// Back to the concrete type: ok, and still a nil func.
	f2, ok := x.(Fn)
	fmt.Println("concrete", ok, f2 == nil)

	// A type switch resolves the interface case the same way.
	switch v := x.(type) {
	case fmt.Stringer:
		fmt.Println("switch   Stringer", v.String())
	default:
		fmt.Println("switch   default")
	}

	// A second named func type against a user interface.
	var g G
	var y any = g

	if gg, ok := y.(Getter); ok {
		fmt.Println("getter  ", gg.Get())
	} else {
		fmt.Println("getter   miss")
	}

	// The misses Go also misses: G has no String, Fn has no Get, an unnamed func type has no methods.
	_, ok = y.(fmt.Stringer)
	fmt.Println("G->Stringer", ok)

	_, ok = x.(Both)
	fmt.Println("Fn->Both   ", ok)

	var plain func() int
	var z any = plain

	_, ok = z.(fmt.Stringer)
	fmt.Println("plain      ", ok, z != nil)
	fmt.Printf("%v|%T\n", plain, plain)
}
