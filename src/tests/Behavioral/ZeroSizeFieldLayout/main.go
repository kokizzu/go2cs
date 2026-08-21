package main

import (
	"fmt"
	"unsafe"
)

// A zero-size Go field occupies NO bytes, so `Counter` is 4 bytes with `v` at offset 0. A C# field
// always occupies at least one, so without explicit Go layout the surrogate is 8 and every later
// offset shifts -- which is what made `Reinterpret`'s (correct) size guard refuse the Go-legal
// pointer alias below, leaving the write on a detached copy.
type nocopy struct{}

type Counter struct {
	_ nocopy
	v int32
}

// Two zero-size fields, both sharing offset 0 with the payload.
type Wide struct {
	_ nocopy
	_ nocopy
	v int64
}

// No zero-size field: layout must be untouched.
type Plain struct {
	a int32
	b int64
}

// A managed field (string) is present, so Go's offsets cannot be applied -- .NET forbids
// overlapping a managed reference. Must be untouched, and must still behave.
type Managed struct {
	_ nocopy
	s string
}

func main() {
	// Go's own sizes and offsets, which the emitted layout has to reproduce.
	fmt.Println("Counter size:", unsafe.Sizeof(Counter{}), "v offset:", unsafe.Offsetof(Counter{}.v))
	fmt.Println("Wide size:", unsafe.Sizeof(Wide{}), "v offset:", unsafe.Offsetof(Wide{}.v))
	fmt.Println("Plain size:", unsafe.Sizeof(Plain{}), "b offset:", unsafe.Offsetof(Plain{}.b))

	// The alias the layout exists for: a *int32 reinterpreted as a *Counter must SHARE storage, so a
	// write through the view is visible in the original. With the naive 8-byte surrogate the size
	// guard refuses and the write lands on a copy.
	var raw int32 = 7
	view := (*Counter)(unsafe.Pointer(&raw))
	fmt.Println("view reads:", view.v)

	view.v = 42
	fmt.Println("write through view reaches the original:", raw)

	// The zero-size field is readonly in the emission; reading it is still ordinary.
	c := Counter{v: 3}
	w := Wide{v: 4}
	p := Plain{a: 1, b: 2}
	m := Managed{s: "managed"}
	fmt.Println(c.v, w.v, p.a, p.b, m.s)

	// A whole-struct assignment writes every byte, which stays correct under explicit layout.
	c = Counter{}
	fmt.Println("cleared:", c.v)
}
