package main

import "fmt"

// PublicNestedShape is a PACKAGE-LEVEL variable (not function-local) whose anonymous type
// nests the exact same shape (struct{ X int }) that a_shape.go's call-boundary literal already
// registered under an internal-but-name-reads-public identifier — mirroring reflect's own
// Δtypeᴛ37 (visiblefields_test.go's package-level fieldsTests table, whose "val: struct{ A
// struct{ X int } }{}" element nests this exact inner shape). A public field may never resolve
// to a less-accessible type (C#'s TYPE >= MEMBER accessibility rule), so this is the shape that
// must NOT dedupe onto the internal type RegisterInternalShape registered.
var PublicNestedShape = struct {
	A struct{ X int }
}{A: struct{ X int }{X: 7}}

func main() {
	RegisterInternalShape()
	fmt.Println("public nested:", PublicNestedShape.A.X)
}
