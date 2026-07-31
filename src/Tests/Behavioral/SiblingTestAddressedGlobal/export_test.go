package main

// The in-package test half. It is NEVER converted (a production conversion excludes `_test.go`)
// and `go build`/`go run` ignore it too — it exists purely so the converter's sibling scan sees
// the address-taking and the production `.cs` golden pins the heap-box backing it forces.

// Positives: a bare global, a global used through a FIELD selector, and one reached from a
// function body rather than a package-level initializer.
var LookupP = &lookup
var HeadNameP = &head.name

func CounterP() *int { return &counter }

// Negatives. `own` is declared by this file, so it is not a production global at all; `untouched`
// below is a LOCAL that shadows a package-level var of the same name, and taking ITS address must
// not box the global (main.go's `untouched` stays a plain field in the golden). Neither name may
// appear in the collected candidate set.
var own int
var OwnP = &own

func localAddress() *int {
	untouched := 7
	return &untouched
}
