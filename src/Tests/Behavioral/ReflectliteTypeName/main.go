package main

// Guards internal/reflectlite's rtype.String().
//
// context.WithValue's String() renders each key through context.stringify, whose fallback arm --
// for a key type that has no String() method of its own -- is reflectlite.TypeOf(v).String().
// The literal Go conversion of that method reads a type-descriptor NAME OFFSET into the
// linker-built name blob (t.nameOff(t.Str).Name()), which the managed reflection bridge's
// synthesized descriptor never populates, so it answered "" for every type -- silently, since ""
// is a legal name for an unnamed Go type. The regression is therefore invisible except through a
// caller that prints the name: this program is that caller.
//
// Covers three name shapes: a named scalar, a named struct, and an unnamed pointer composite.

import (
	"context"
	"fmt"
)

type ctxKey int

type namedKey struct{ id int }

func main() {
	bg := context.Background()

	c1 := context.WithValue(bg, ctxKey(1), "v1")
	fmt.Println(c1)

	c2 := context.WithValue(c1, namedKey{2}, "v2")
	fmt.Println(c2)

	n := 3
	c3 := context.WithValue(c2, &n, "v3")
	fmt.Println(c3)
}
