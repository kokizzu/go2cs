package main

import "fmt"

// A Go tagged switch may use a POINTER case label — `case &sentinel:` — which is a runtime value,
// not a constant. C# has no constant pattern for that: the lowered chain must compare with `==`
// (pointer identity on the emitted box) and never with `is`, which is a CONSTANT pattern and fails
// to compile (CS9135 at the pattern operand).
//
// The shape is 1.24 runtime/type.go's GC-mask sentinel, reproduced faithfully: a package-level byte
// whose ADDRESS is the sentinel, a LEADING default clause, `case &sentinel:` and `case nil:`, all
// inside a loop whose case bodies `continue`. The leading default and the loop are part of the
// shape because they are what the real site has; a simpler switch lowers through the same path but
// exercises less of it.
//
// This asserts SEMANTICS, not merely that the emission compiles: each classification is printed and
// compared against `go run`, so a lowering that compiled but matched the wrong arm still fails.
var sentinel byte

var other byte

// classify returns 1 for the sentinel's address, 2 for nil, 0 for anything else. The loop and the
// continues mirror the real site; every path leaves through the default arm's return.
func classify(p *byte) int {
	result := 0

	for {
		switch p {
		default:
			return result
		case &sentinel:
			result = 1
			p = nil
			continue
		case nil:
			if result == 0 {
				result = 2
			}
			p = &other
			continue
		}
	}
}

// mu and schedt mirror 1.24 runtime/lock_spinbit.go's mutexPreferLowLatency, whose case label is
// the address of a FIELD of a package-level var. That is the SAME lowering as `case &sentinel:`
// above, but C# parses the emitted operand differently and so reports a DIFFERENT diagnostic: a
// bare identifier emits `Ꮡsentinel` and reads as a CONSTANT pattern (CS9135), while a field
// address emits a member CALL and reads as a POSITIONAL pattern whose type cannot be found
// (CS0246). One defect, two diagnostics -- measured on this file's own emission, base against fix.
//
// Both shapes are guarded because a fix that screened only the constant-pattern form would leave
// this one emitting `is` while the guard above still passed. This is the shape the real source has.
type mu struct{ key uintptr }

type schedt struct{ lock mu }

var theSched schedt

// preferLowLatency returns true only for the address of theSched's own lock field.
func preferLowLatency(p *mu) bool {
	switch p {
	default:
		return false
	case &theSched.lock:
		return true
	}
}

func main() {
	// The sentinel's own address takes the sentinel arm.
	fmt.Println(classify(&sentinel))

	// nil takes the nil arm.
	fmt.Println(classify(nil))

	// A DIFFERENT variable's address takes neither: pointer identity, not byte equality. Both
	// bytes hold zero, so a comparison that dereferenced would wrongly match the sentinel.
	fmt.Println(classify(&other))

	// The identity rule's own case: a SECOND pointer to the same variable must match the sentinel
	// arm. Under the emitted model these are two boxes over one target, and the rule that keeps
	// them equal (reference identity OR equal order tokens) is what makes this print 1 rather
	// than 0 — so this line fails if `==` ever degrades to reference identity alone.
	second := &sentinel
	fmt.Println(classify(second))

	// And the sentinel compared directly, outside a switch, for the same reason.
	fmt.Println(second == &sentinel, &other == &sentinel)

	// The FIELD-address shape: true only for the lock's own address, false for nil and for an
	// unrelated mu. The middle value is what a lowering that matched on the wrong arm would flip.
	var elsewhere mu
	fmt.Println(preferLowLatency(&theSched.lock), preferLowLatency(&elsewhere), preferLowLatency(nil))
}
