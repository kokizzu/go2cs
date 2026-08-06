// File B (sorts BEFORE main.go): declares a package-level anonymous struct that carries NO
// usable name of its own — the converter lifts it under the generic `type` fallback, the same
// name yvars.go's differently-shaped anonymous struct starts from. Every lifted type nests in
// the ONE package class, so the uniquing must be package-wide: while it was per-FILE both files
// emitted `Δtype` and the class declared it twice (CS0579 on the doubled [GoType] attribute plus
// CS0111/CS0557 on every member go2cs-gen generated for it — encoding/gob's type.cs vs its
// encoder_test.cs, 32 errors). The method-local `inner` below is the second manifestation: a
// function-local lift is prefixed with the METHOD name only, which yvars.go's Why.probe shares.
package main

import "fmt"

var bReserved = (*struct{ bx int })(nil)

type Bee struct{}

func (Bee) probe() string {
	type inner struct{ n int }
	v := inner{n: 7}
	return fmt.Sprintf("bee inner n=%d reserved=%v", v.n, bReserved == nil)
}
