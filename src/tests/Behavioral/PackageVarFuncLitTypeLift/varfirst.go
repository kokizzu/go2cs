// The SAME root as main.go's `makers`, in the arrangement that produced the other symptom.
//
// Which symptom appeared depended only on declaration order within the FILE: with no function
// declaration visited before the var, currentFuncPrefix was not merely stale but nil, and writing
// the lifted declaration into it panicked (nil receiver inside strings.Builder.copyCheck). The
// converter recovers that panic per file, so the whole FILE was skipped with only a
// "visit file error" warning -- a silent, total loss of this file's conversion.
//
// This file therefore declares the var FIRST, before any function declaration.
package main

var varFirst = func(s string) Greeter {
	return struct{ Greeter }{namedGreeter{s}}
}("package-scope")

var varFirstLabel = "varfirst"
