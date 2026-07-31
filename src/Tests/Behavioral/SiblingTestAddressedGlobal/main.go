package main

import "fmt"

// Package-level vars whose address is taken ONLY by this package's own in-package test file
// (export_test.go — path/filepath's `var LstatP = &lstat` over path.go's `var lstat`). A Go
// pointer to a global must alias the global's real storage, so the PRODUCTION emission has to
// back each of these with a heap box; go/packages excludes `_test.go` from the production
// package, so without the sibling scan the box is never declared and the test half's `Ꮡlookup`
// names nothing (CS0103). The boxing must be invisible here: reads, writes, calls and field
// access all still behave exactly as an unboxed global.
var lookup = func(key string) string { return "prod:" + key }

var counter int

// Never addressed by anything. export_test.go DOES take the address of a LOCAL of the same name,
// which must not reach this declaration: the golden pins `untouched` as a plain field, so an
// over-eager sibling scan that ignored local bindings would show up here as a spurious heap box.
var untouched = 5

type entry struct {
	name string
}

var head entry

func main() {
	fmt.Println(lookup("a"))

	lookup = func(key string) string { return "swapped:" + key }
	fmt.Println(lookup("b"))

	counter += 3
	counter++
	fmt.Println(counter)

	head.name = "first"
	fmt.Println(head.name)

	fmt.Println(untouched)
}
