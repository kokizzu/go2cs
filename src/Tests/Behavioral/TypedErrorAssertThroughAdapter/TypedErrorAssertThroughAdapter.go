// Guards the CONCRETE-type half of a typed-error assert made on a statically-`error` operand.
//
// A pointer-sourced error interface value is a generated IжAdapter standing in for the *T it
// wraps, and a value-sourced one is the struct itself — neither is golib's reflective `error<T>`
// carrier. golib's error-specific assert used to cast the carrier straight to `error<T>`, so
// `err.(*T)` through an interface boundary threw an InvalidCastException that Go's `recover`
// could not even see (io/fs TestGlob / TestReadDirPath / TestReadFilePath died on os's dirFS
// path-fixup, `err.(*fs.PathError)`).

package main

import "fmt"

type myErr struct {
	code int
}

func (e *myErr) Error() string {
	return fmt.Sprintf("myErr %d", e.code)
}

type valErr struct {
	tag string
}

func (e valErr) Error() string {
	return "valErr " + e.tag
}

// The interface boundary: the concrete type is erased to `error` here, so every assert below
// runs against a dynamic value the call site cannot see statically.
func pointerSourced() error {
	return &myErr{code: 42}
}

func valueSourced() error {
	return valErr{tag: "v"}
}

func main() {
	// Single-value assert back to the POINTER type: must yield the ORIGINAL box, so a write
	// through it is observable on the interface value (Go's interface holds the *T).
	err := pointerSourced()
	p := err.(*myErr)
	p.code = 7
	fmt.Println("ptr-assert", p.code, err.Error())

	// Single-value assert back to a VALUE type.
	v := valueSourced().(valErr)
	fmt.Println("val-assert", v.tag, v.Error())

	// Comma-ok forms must MISS without panicking, in both directions.
	if _, ok := valueSourced().(*myErr); ok {
		fmt.Println("ptr-miss-WRONG")
	} else {
		fmt.Println("ptr-miss-ok")
	}
	if _, ok := pointerSourced().(valErr); ok {
		fmt.Println("val-miss-WRONG")
	} else {
		fmt.Println("val-miss-ok")
	}

	// A failed single-value assert is a RECOVERABLE Go panic, not a hard runtime fault.
	func() {
		defer func() {
			if r := recover(); r != nil {
				fmt.Println("recovered")
			}
		}()
		_ = pointerSourced().(valErr)
		fmt.Println("unreachable")
	}()

	// The interface half of the same machinery (guarded separately by
	// AnonIfaceThroughPointerAdapter) must keep working from an `error` operand too.
	if s, ok := pointerSourced().(interface{ Error() string }); ok {
		fmt.Println("iface-assert", s.Error())
	} else {
		fmt.Println("iface-assert-missed")
	}
}
