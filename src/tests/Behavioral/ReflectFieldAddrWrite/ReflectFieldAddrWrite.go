// Guards the round trip a Go program is allowed to make through unsafe.Pointer: take an
// addressable field Value's address with reflect.Value.Addr, project it to a pointer with
// UnsafePointer/Pointer, convert that back to a typed pointer, and WRITE through it.
//
// A pointer to managed storage has no machine address in the converted model, so those
// projections answer with a stable order token instead. Without the token-recovery table in
// golib (ж.PointerTokens.cs) the conversion back builds a native-address box over the token's
// numeric value and the store lands on an arbitrary address — an access violation, which is
// exactly how go/types' converted test host died at TestCheck/blank.go.
//
// This is go/types' own check_test.go idiom (boolFieldAddr / stringFieldAddr), which is how it
// reaches the unexported Config._Trace field.
package main

import (
	"fmt"
	"reflect"
)

type conf struct {
	name    string
	trace   bool
	version string
	depth   int
}

func boolFieldAddr(c *conf, name string) *bool {
	v := reflect.Indirect(reflect.ValueOf(c))
	return (*bool)(v.FieldByName(name).Addr().UnsafePointer())
}

func stringFieldAddr(c *conf, name string) *string {
	v := reflect.Indirect(reflect.ValueOf(c))
	return (*string)(v.FieldByName(name).Addr().UnsafePointer())
}

// The deprecated Pointer() spelling reaches the same projection, and reflect's own type.go
// uses it in this shape.
func intFieldAddr(c *conf, name string) *int {
	v := reflect.Indirect(reflect.ValueOf(c))
	return (*int)(v.FieldByName(name).Addr().UnsafePointer())
}

func main() {
	c := conf{name: "cfg", version: "v0", depth: 3}

	// Write through the reinterpreted pointers...
	*boolFieldAddr(&c, "trace") = true
	*stringFieldAddr(&c, "version") = "v1"
	*intFieldAddr(&c, "depth") = 7

	// ...and observe every write through the ORIGINAL struct: the derived pointer must ALIAS
	// the field's storage, never a detached copy of it.
	fmt.Println(c.name, c.trace, c.version, c.depth)

	// Two derivations of one field name the same storage, and a write through either is seen
	// through the other.
	p1 := boolFieldAddr(&c, "trace")
	p2 := boolFieldAddr(&c, "trace")
	*p1 = false
	fmt.Println(*p1, *p2, c.trace, p1 == p2)

	// A field left alone keeps its value — the recovery must not smear one field's storage
	// over its neighbours.
	fmt.Println(c.name, c.version, c.depth)
}
