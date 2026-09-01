// Same-declared-package-name guard: duprenamed and dupplain are two different import paths that
// both declare `package dup`. go2cs's imported-type-alias table is keyed by declared package name,
// not import path, so duprenamed's collision-renamed Marker type used to leak into any reference
// to dupplain's unrelated, unrenamed Marker function that happened to share the key (CS1955) —
// exactly the shape that broke runtime's crash_test.go, where runtime/trace.Log picked up
// internal/trace's unrelated Log rename because both packages declare `package trace`.
package main

import (
	"fmt"

	dupmeta "collidea/dup"

	"collideb/dup"
)

func main() {
	fmt.Println(dupmeta.Greeting())
	fmt.Println(dup.Marker())
}
