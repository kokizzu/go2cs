package main

import (
	"fmt"
	"reflect"
)

// Greeter is a named func type — the shape net/http's HandlerFunc has. Crossing an interface
// boundary as a nil value mints an IValueAdapter shell wrapping a null delegate (the golib
// mechanism this whole test audits across four consumption sites: equality, map-key hashing,
// reflect method values, and reflect.Value.Set).
type Greeter func(name string)

func (g Greeter) Greet(name string) {
	g(name)
}

type Greetable interface {
	Greet(name string)
}

func wrap(handler func(name string)) Greetable {
	return Greeter(handler)
}

func try(label string, f func()) {
	defer func() {
		if r := recover(); r != nil {
			fmt.Println(label, "panicked:", r)
		}
	}()
	f()
}

func main() {
	try("equality", func() {
		a, b := wrap(nil), wrap(nil)
		fmt.Println("a == b:", a == b)
	})

	try("map-hash", func() {
		m := map[Greetable]int{}
		m[wrap(nil)] = 1
		fmt.Println("stored ok")
	})

	try("reflect-set", func() {
		var dest Greetable
		rv := reflect.ValueOf(&dest).Elem()
		src := wrap(nil)
		rv.Set(reflect.ValueOf(&src).Elem())
		fmt.Println("set ok, dest == nil:", dest == nil)
	})

	try("method-value-call", func() {
		g := wrap(nil)
		v := reflect.ValueOf(g)
		mv := v.Method(0)
		fmt.Println("got method value, kind:", mv.Kind())
		mv.Call([]reflect.Value{reflect.ValueOf("world")})
		fmt.Println("called ok")
	})
}
