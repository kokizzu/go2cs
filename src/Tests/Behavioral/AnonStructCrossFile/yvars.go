// File Y (sorts AFTER main.go): the sibling half of bvars.go's lifted-name collision — a
// DIFFERENTLY shaped package-level anonymous struct reaching for the same `type` fallback name,
// and a method that shares bvars.go's `probe` name so its function-local `inner` reaches for the
// same `probe_inner`. Both must land on distinct C# type names.
package main

import "fmt"

var yReserved = (*struct{ yx string })(nil)

type Why struct{}

func (Why) probe() string {
	type inner struct{ s string }
	v := inner{s: "seven"}
	return fmt.Sprintf("why inner s=%s reserved=%v", v.s, yReserved == nil)
}
