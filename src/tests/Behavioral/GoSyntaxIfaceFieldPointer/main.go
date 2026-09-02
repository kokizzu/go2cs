// Guards the Go-syntax rendering of a POINTER held in an INTERFACE-typed field of an
// anonymous struct -- the exact shape of net/unixsock_test.go:290, where the converted
// `%#v` was measured printing a raw address where Go prints `&net.UnixAddr{...}`.
//
// Three properties are load-bearing and none of them is incidental:
//   - Addr is satisfied by a POINTER receiver, so storing &UnixAddr{} in it goes through
//     go2cs-gen's IжAdapter wrapper rather than a plain value box;
//   - the field is INTERFACE-typed and lives in an ANONYMOUS struct, which the converter
//     lifts to a generated `dyn` type;
//   - the array is ranged over, so each element is a COPY.
// fmt renders &T{...} only when the bridge answers that the pointer's Elem has struct
// kind; anything else falls through to hex, so this program's output is a direct read of
// that answer.
package main

import (
	"fmt"
	"reflect"
)

type Addr interface {
	Network() string
	String() string
}

type UnixAddr struct {
	Name string
	Net  string
}

func (a *UnixAddr) Network() string { return a.Net }
func (a *UnixAddr) String() string  { return a.Name }

func main() {
	ta := &UnixAddr{Name: "/tmp/sock", Net: "unix"}

	var connAddrs = [3]struct{ got, want Addr }{
		{ta, ta},
		{&UnixAddr{Name: "/tmp/sock", Net: "unix"}, &UnixAddr{Name: "/tmp/sock", Net: "unix"}},
		{ta, &UnixAddr{Name: "/tmp/sock", Net: "unix"}},
	}

	for i, ca := range connAddrs {
		fmt.Printf("%d got  T=%T v=%#v\n", i, ca.got, ca.got)
		fmt.Printf("%d want T=%T v=%#v\n", i, ca.want, ca.want)
		fmt.Printf("%d deepequal=%v\n", i, reflect.DeepEqual(ca.got, ca.want))
	}

	// Control: the same pointer NOT routed through an interface field.
	fmt.Printf("direct T=%T v=%#v\n", ta, ta)

	// A named-struct field, to separate "anonymous-struct lift" from "interface field".
	type named struct{ got Addr }
	n := named{got: ta}
	fmt.Printf("named T=%T v=%#v\n", n.got, n.got)

	// The second divergence in the same record: %T of a nil interface.
	var nilAddr Addr
	fmt.Printf("nil T=%T v=%v\n", nilAddr, nilAddr)
}
