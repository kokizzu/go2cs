// Go allows ANY number of `init` functions per package, in any number of files, and runs them in
// the order the files are presented to the compiler (sorted by filename) and, within a file, in
// declaration order. C# has one name per method, so the converter uniquifies every init after the
// first as `initΔN` from a PACKAGE-scoped counter — package-scoped, not per-file, which is what
// this test pins: `Solitaire` already covers two inits in ONE file, nothing covered two FILES.
package main

var order []string

func init() {
	order = append(order, "a_first#1")
}

func init() {
	order = append(order, "a_first#2")
}
