// The cross-assembly twin of GoSyntaxIfaceFieldPointer: identical shape, except the
// concrete type and interface are imported from a sub-library, so the pointer adapter is
// generated in THIS assembly and not beside the type it wraps.
package main

import (
	"fmt"
	"reflect"

	"ForeignIfaceFieldPointer/addrlib"
)

func main() {
	ta := &addrlib.UnixAddr{Name: "/tmp/sock", Net: "unix"}

	var connAddrs = [3]struct{ got, want addrlib.Addr }{
		{ta, ta},
		{&addrlib.UnixAddr{Name: "/tmp/sock", Net: "unix"}, &addrlib.UnixAddr{Name: "/tmp/sock", Net: "unix"}},
		{ta, &addrlib.UnixAddr{Name: "/tmp/sock", Net: "unix"}},
	}

	for i, ca := range connAddrs {
		fmt.Printf("%d got  T=%T v=%#v\n", i, ca.got, ca.got)
		fmt.Printf("%d want T=%T v=%#v\n", i, ca.want, ca.want)
		fmt.Printf("%d deepequal=%v\n", i, reflect.DeepEqual(ca.got, ca.want))
	}

	fmt.Printf("direct T=%T v=%#v\n", ta, ta)

	var nilAddr addrlib.Addr
	fmt.Printf("nil T=%T v=%v\n", nilAddr, nilAddr)
}
