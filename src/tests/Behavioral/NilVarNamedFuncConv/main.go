package main

import "fmt"

// Greeter is a named func type — the shape net/http's HandlerFunc has: a defined func type
// whose method set gives it Greet, which the raw func signature does not carry on its own.
type Greeter func(name string)

func (g Greeter) Greet(name string) {
	g(name)
}

type Greetable interface {
	Greet(name string)
}

// wrap converts a raw func VALUE — never a nil literal — to the named type. This is exactly
// net/http's own `HandlerFunc(handler)` shape inside HandleFunc: handler is a PARAMETER that
// can be nil at runtime without the compiler ever seeing a nil literal at the conversion site.
func wrap(handler func(name string)) Greetable {
	return Greeter(handler)
}

func main() {
	nilGreetable := wrap(nil)
	nilGreeter, nilOk := nilGreetable.(Greeter)
	fmt.Println(nilOk, nilGreeter == nil)

	realGreetable := wrap(func(name string) {
		fmt.Println("hello", name)
	})
	realGreeter, realOk := realGreetable.(Greeter)
	fmt.Println(realOk, realGreeter == nil)
	realGreeter.Greet("world")
}
