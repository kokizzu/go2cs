package main

import (
	"fmt"
	"reflect"
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

	namedZeroSizeWrites()
}

// A NAMED zero-size field shares its offset with V, and is readonly in the emission. Go stores
// nothing for a zero-size value, whichever way the write arrives, so V must keep every byte.
type Carrier struct {
	Z nocopy
	V uint64
}

type Outer struct {
	C Carrier
}

const pattern = 0x0102030405060708

var sideCalls int

func side() nocopy {
	sideCalls++
	return nocopy{}
}

func namedZeroSizeWrites() {
	var x Carrier
	x.V = pattern
	rx := reflect.ValueOf(&x).Elem()

	// Through reflect: Set, and a write through the pointer reflect hands out.
	rx.Field(0).Set(reflect.ValueOf(nocopy{}))
	fmt.Printf("after reflect Set: %#x\n", x.V)

	x.V = pattern
	rp := rx.Field(0).Addr().Interface().(*nocopy)
	*rp = nocopy{}
	fmt.Printf("after write through reflect's pointer: %#x\n", x.V)

	// Reflect's address is the field's address.
	fmt.Println("reflect Addr == &x.Z:", rx.Field(0).Addr().UnsafePointer() == unsafe.Pointer(&x.Z))

	// Assignment through every addressable shape: the right side runs, nothing is stored.
	x.V = pattern
	p := &x
	arr := [2]Carrier{{V: pattern}, {V: pattern}}
	o := Outer{C: Carrier{V: pattern}}
	s := []Carrier{{V: pattern}, {V: pattern}}

	x.Z = side()
	p.Z = side()
	arr[1].Z = side()
	o.C.Z = side()
	s[1].Z = side()

	// A tuple assignment: the zero-size target stores nothing, its neighbour target still stores.
	var n int
	x.Z, n = side(), 7

	fmt.Printf("assignments ran %d right sides; n = %d; V: %#x %#x %#x %#x\n", sideCalls, n, x.V, arr[1].V, o.C.V, s[1].V)
}
