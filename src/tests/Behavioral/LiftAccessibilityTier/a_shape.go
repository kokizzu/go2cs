package main

import "fmt"

// RegisterInternalShape passes a function-local anonymous struct literal directly as a call
// argument (matching reflect's own TestTypeFieldOutOfRangePanic: `typ := TypeOf(struct{ X int
// }{10})`) — the call-boundary shape that publishes it into the package-wide dynamic-type
// registry under a name that inherits THIS function's own capitalization
// (RegisterInternalShape_type) even though the type itself is function-local and therefore
// always internal (localTypeAccess forces it, regardless of the enclosing function's export
// status).
func RegisterInternalShape() {
	fmt.Println(struct{ X int }{X: 10})
}
