package main

import "fmt"

// Behavioral guard for the ж-box ref-lowering NIL DOCTRINE (ruling §10.4,
// docs/phase4/DESIGN-zh-box-reduction.md §3.3, §8 guard 2), differential against `go run`:
//
//   - the EAGER field-address panic — `&e.x` with nil e panics at the address formation, in the
//     FRAME that forms it, so the callee it would feed is never entered ("consume entered" must
//     not print — the panel's S-F1 third-behavior refutation, where a silently-passed null byref
//     lets the callee run side effects Go never runs);
//   - the plain-variable DEFERRED fault — a nil pointer argument still ENTERS the callee and
//     faults at first use, after the callee's earlier statements ran;
//   - a nil-GUARDING callee (X-excluded from lowering) still tolerating nil — the classification
//     boundary, not just the mechanism.
//
// Sibling-argument ordering note, measured against gc: Go evaluates function CALLS among the
// arguments in lexical order BEFORE non-call operands like `&e.x`, so a side-effecting call
// placed before OR after the faulting address in the argument list runs first under gc. The
// shapes below put observable calls lexically FIRST, where gc's order and C#'s strict
// left-to-right order agree byte-for-byte; the doctrine's load-bearing half — no callee entry,
// Go's message, recoverability — is what this guard pins.
//
// The nil flows through three spellings: a zero-valued pointer variable, the literal nil at a
// lowered position (§3.3 row 7), and a pointer round-tripped through an interface (the canonical
// typed-nil instance on the C# side). All panics are recovered and printed, so stdout carries
// the ordering AND the message and the comparison against Go is exact.

type elem struct {
	x uint64
}

// consume is ref-lowerable (deref-only use of p). With a nil base at the call site it must
// NEVER be entered — the address formation panics first, in the caller.
func consume(p *uint64, marker uint64) {
	fmt.Println("consume entered")
	*p += marker
}

// eagerFrom is ref-lowerable (its only use of e is the derived field address feeding consume —
// D1′). The observable call evaluates lexically first (gc and C# agree); with nil e the address
// formation then panics before consume runs.
func eagerFrom(e *elem) {
	m := side("side effect ran")
	consume(&e.x, m)
	fmt.Println("eagerFrom completed")
}

func side(msg string) uint64 {
	fmt.Println(msg)
	return 1
}

// passThrough is ref-lowerable; a nil argument must still ENTER it (Go semantics for a nil
// pointer argument), print the marker, and fault only at the first dereference.
func passThrough(p *uint64) {
	fmt.Println("entered callee")
	fmt.Println("value:", *p)
}

// nilGuard compares with nil (X1 — excluded from lowering) and must keep tolerating nil.
func nilGuard(p *elem) string {
	if p == nil {
		return "nil ok"
	}
	return "non-nil"
}

// nilElem launders a nil through a call so the caller-side spelling is a plain pointer variable.
func nilElem() *elem {
	return nil
}

// tryEager is ref-lowerable (e forwards to eagerFrom — D2) and recovers whatever the shape under
// test raises, printing the ordering and the message.
func tryEager(label string, e *elem) {
	defer func() { fmt.Println(label, "recovered:", recover()) }()
	eagerFrom(e)
	fmt.Println(label, "no panic")
}

func deferredFault() {
	defer func() { fmt.Println("deferred recovered:", recover()) }()
	var q *uint64
	passThrough(q)
	fmt.Println("not reached")
}

func main() {
	// Spelling 1: a zero-valued pointer variable.
	var e1 *elem
	tryEager("zerovar", e1)

	// Spelling 2: the literal nil at the lowered position (§3.3 row 7).
	tryEager("literal", nil)

	// Spelling 3: nil round-tripped through an interface (the canonical typed-nil instance).
	var i any = nilElem()
	e3 := i.(*elem)
	tryEager("boxed", e3)

	// A NON-nil pointer through the same chain runs to completion.
	good := elem{x: 40}
	tryEager("live", &good)
	fmt.Println("good.x:", good.x)

	// The deferred-fault half of the doctrine.
	deferredFault()

	// The classification boundary: the guarding callee still takes nil.
	fmt.Println("guard:", nilGuard(nil))
	fmt.Println("guard:", nilGuard(&good))
}
