// Dotted-module-path guard: a package imported from example.com/dotted/v2 must report its VERBATIM
// import path everywhere Go reports one -- reflect's PkgPath, a runtime.Caller function name,
// FuncForPC, and the name gob registers a type under. The converter's C# namespace flattens the path
// (a '.' inside a segment is indistinguishable from a separator; /v2 and the package name differ),
// so each of these used to read back as a reconstruction instead of the path.
package main

import (
	"bytes"
	"encoding/gob"
	"fmt"
	"reflect"
	"runtime"

	"example.com/dotted/v2"
)

func main() {
	fmt.Println(reflect.TypeOf(dotted.T{}).PkgPath())
	fmt.Println(reflect.TypeOf(&dotted.T{}).Elem().String())

	fmt.Println(dotted.Where())
	fmt.Println(runtime.FuncForPC(reflect.ValueOf(dotted.F).Pointer()).Name())

	gob.Register(dotted.T{})

	var buf bytes.Buffer
	var in any = dotted.T{N: dotted.F()}

	if err := gob.NewEncoder(&buf).Encode(&in); err != nil {
		fmt.Println("encode:", err)
		return
	}

	fmt.Println(bytes.Contains(buf.Bytes(), []byte("example.com/dotted/v2.T")))

	var out any
	if err := gob.NewDecoder(&buf).Decode(&out); err != nil {
		fmt.Println("decode:", err)
		return
	}

	fmt.Printf("%T %v\n", out, out)
}
