// Guards the CROSS-PACKAGE composite literal over a mixed-visibility struct: the public
// field-subset constructor must still give every member it does NOT name its Go zero value.
// See addrlib/lib.go for why only a foreign-package literal can reach that constructor, and for
// the shipped syscall.SockaddrUnix case (AF_UNIX bind failing with "invalid argument").
package main

import (
	"fmt"

	"CrossPkgLiteralNestedField/addrlib"
)

func main() {
	// The literal sets only the exported field; `raw` must still hold a full [PathMax]int8.
	a := &addrlib.Addr{Name: "hello"}
	fmt.Println(a.Capacity())

	// The guard-then-fill path: a zero-length backing rejected this as "too long".
	fmt.Println(a.Encode())
	fmt.Println(a.PathByte(0), a.PathByte(4))

	// A name that genuinely exceeds the array still gets Go's answer, not a spurious one --
	// so the test distinguishes "array is right" from "guard always fails".
	long := &addrlib.Addr{Name: "0123456789abcdef"}
	fmt.Println(long.Capacity())
	fmt.Println(long.Encode())

	// The promoted-embed kind: the box the public subset ctor omitted must exist.
	e := &addrlib.Embedder{Name: "world"}
	fmt.Println(e.Slots())
	fmt.Println(e.Put(2, 7))

	// A zero-valued literal (no fields at all) goes through the same constructor.
	var z addrlib.Addr
	fmt.Println(z.Capacity())
	empty := &addrlib.Addr{}
	fmt.Println(empty.Capacity())
}
