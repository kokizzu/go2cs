package main

import (
	"errors"
	"fmt"
)

// How the runtime RENDERS an unrecovered panic value. Go's preprintpanics (runtime/panic.go)
// substitutes before anything is printed: an `error` panic value becomes its Error(), a Stringer
// its String(). Nothing a Go program can print to stdout observes that — the substitution happens
// on the way out of the process — so this test is driven by the panic report itself, which the
// behavioral runner compares by FIRST STDERR LINE (a full compare could never match: Go appends a
// machine-specific goroutine dump). The stdout half below establishes that the recovered path and
// fmt's own formatting are unaffected, and the process then dies on the panic under test.

type openErr struct {
	path string
	code int
}

// A POINTER receiver, which is the shape that used to print an address: the panic value is a
// pointer-held error, and the managed rendering reached the box rather than the method.
func (e *openErr) Error() string {
	return fmt.Sprintf("open %s: code %d", e.path, e.code)
}

func mustOpen(path string) error {
	return &openErr{path: path, code: 13}
}

func main() {
	// The panic value is an `error` on both sides of a recover, and recover hands back the value
	// itself — the substitution is a PRINTING rule, not a value rewrite, so this must be unchanged.
	fmt.Println("recovered:", recoverText(func() { panic(mustOpen("a.txt")) }))

	// A wrapped error, to show the substitution reads through whatever Error() is bound.
	wrapped := fmt.Errorf("load config: %w", mustOpen("cfg.yaml"))
	fmt.Println("recovered:", recoverText(func() { panic(wrapped) }))

	// A plain string panic value, the commonest shape, must be untouched by the rule.
	fmt.Println("recovered:", recoverText(func() { panic("plain string") }))

	// errors.New, so the arm is proven for a value-shaped error too.
	fmt.Println("recovered:", recoverText(func() { panic(errors.New("simple")) }))

	// ...and now unrecovered: the report on stderr is what is actually under test. Go prints
	// `panic: open final.txt: code 13`, never the value's address.
	panic(mustOpen("final.txt"))
}

func recoverText(f func()) (msg string) {
	defer func() {
		if r := recover(); r != nil {
			msg = fmt.Sprintf("%v", r)
		}
	}()
	f()
	return "<no panic>"
}
