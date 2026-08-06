// Guard: a numeric conversion whose BOTH operands live in another package must record NO
// GoImplicitConv here. ImplicitConvGenerator realizes a record as a `partial struct <name>` inside
// THIS package's class, so with neither operand local it has nothing to extend and declares a
// PHANTOM local type of that simple name — whose `.Value` does not exist (CS1061). The aliased-
// numeric arm's local-anchor swap exists to put the record on the local operand; with both foreign
// it merely picked the other foreign one. os reaches this through os_windows_test.go's
// `syscall.CloseHandle(syscall.Handle(t))` over a `syscall.Token`.
//
// Nothing is lost by declining: the call site already emits the explicit cast chain.
// The local<->foreign conversions below are the controls — those records are still needed.
package main

import (
	"fmt"

	"ForeignPairNumericConv/convlib"
)

// Local named numerics, so the one-sided (still recorded) conversions have somewhere to anchor.
type LocalID uintptr

func main() {
	tok := convlib.NewToken(0x2A)

	// BOTH operands foreign — the defect.
	h := convlib.Handle(tok)
	fmt.Println("foreign pair:", uintptr(h), h.String())

	// Control: foreign -> LOCAL named numeric (record anchors on the local type).
	id := LocalID(tok)
	fmt.Println("foreign to local:", uintptr(id))

	// Control: LOCAL -> foreign named numeric.
	back := convlib.Token(id)
	fmt.Println("local to foreign:", back.Raw())
}
