package main

import "fmt"

type inner struct{ v uint32 }
type outer struct{ in inner }

type core struct{ sema uint32 }
type holder struct{ c core }

// wrapper reaches holder's fields through an EMBEDDED POINTER, so w.c is by
// definition h.c and the two spellings name one address.
type wrapper struct{ *holder }

func main() {
	o := &outer{}
	fmt.Println("depth1:", &o.in == &o.in)
	fmt.Println("depth2:", &o.in.v == &o.in.v)

	m := map[*uint32]string{}
	m[&o.in.v] = "first"
	m[&o.in.v] = "second"
	fmt.Println("map entries:", len(m), m[&o.in.v])

	h := &holder{}
	w := &wrapper{holder: h}
	fmt.Println("promoted struct:", &w.c == &h.c)
	fmt.Println("promoted field:", &w.c.sema == &h.c.sema)

	pm := map[*uint32]int{}
	pm[&w.c.sema] = 1
	pm[&h.c.sema] = 2
	fmt.Println("promoted map:", len(pm), pm[&w.c.sema])

	// A write through one spelling must be visible through the other.
	*(&w.c.sema) = 7
	fmt.Println("shared storage:", h.c.sema, w.c.sema)
}
