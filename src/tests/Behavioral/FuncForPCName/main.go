package main

import (
	"fmt"
	"reflect"
	"runtime"
)

// Guards runtime.FuncForPC(v.Pointer()).Name() -- recovering a Go function NAME from a function
// VALUE. reflect's own abi_test.go names every subtest this way
// (t.Run(runtime.FuncForPC(fn.Pointer()).Name(), ...)), so when it answers "" Go's testing package
// silently renumbers the subtests #00, #01, ... and 83 comparison rows become orphans that look
// like 83 defects instead of one.
//
// The managed host has no pclntab, so the converted FuncForPC once documented a *Func as having no
// referent at all. That stopped being true when ManagedPointerTokens landed: Value.Pointer() mints
// an identity token AND registers the delegate behind it, so the name is recoverable.

func passInt(x int) int { return x }

func passString(s string) string { return s }

type receiver struct{}

func (receiver) method() {}

func nameOf(fn any) string {
	p := reflect.ValueOf(fn).Pointer()
	f := runtime.FuncForPC(p)
	if f == nil {
		return "<nil Func>"
	}
	name := f.Name()
	if name == "" {
		return "<empty name>"
	}
	return name
}

func main() {
	// Package-level functions: Go spells these `main.passInt`.
	fmt.Println("func:  ", nameOf(passInt))
	fmt.Println("func:  ", nameOf(passString))

	// A method value carries its receiver type in Go's spelling.
	fmt.Println("method:", nameOf(receiver.method))

	// A function literal is named by its enclosing function plus an ordinal.
	literal := func() {}
	fmt.Println("literal present:", nameOf(literal) != "<empty name>" && nameOf(literal) != "<nil Func>")
}
