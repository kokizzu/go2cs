// Go's method-set rule (the "X3" discipline) for a runtime-resolved NAMED interface
// assert: the method set of T holds only its VALUE-receiver methods, while the method
// set of *T holds both. A duck-typing shell that binds a pointer-receiver method to a
// VALUE-sourced interface value would silently widen the method set and make an
// assertion succeed where Go says it fails — so the negative below is the primary
// guard, and the positives prove the widening was not achieved by simply refusing
// everything.
//
// Two "must MISS" cases ride along: a same-named method with a DIFFERENT signature
// (the probe compares full signatures for closed types), and a Go GENERIC receiver
// whose signature can only be matched by name — the fail-soft case, which must answer
// ok=false rather than throw.
package main

import (
	"fmt"

	"NamedInterfacePointerMethodSet/pmslib"
)

// PtrOnly's Speak has a POINTER receiver: *PtrOnly is a Speaker, PtrOnly is NOT.
type PtrOnly struct{ name string }

func (p *PtrOnly) Speak() string { return "ptr:" + p.name }

// ValOnly's Speak has a VALUE receiver: both ValOnly and *ValOnly are Speakers.
type ValOnly struct{ name string }

func (v ValOnly) Speak() string { return "val:" + v.name }

// Mixed carries one method of each receiver kind; only Speak is in Mixed's own set.
type Mixed struct{ n int }

func (m Mixed) Speak() string { return fmt.Sprintf("mixed:%d", m.n) }

func (m *Mixed) Bump() { m.n++ }

// WrongSig has a Speak, but not Speaker's Speak.
type WrongSig struct{}

func (w WrongSig) Speak(times int) string { return "wrong" }

// gbox is generic: Get's result type is the type parameter, which no concrete
// interface signature can be structurally compared against.
type gbox[T any] struct{ v T }

func (b gbox[T]) Get() T { return b.v }

func report(label string, s string, ok bool) {
	fmt.Println(label, ok, s)
}

func main() {
	// X3 NEGATIVE: a VALUE of a pointer-receiver-only type is not a Speaker.
	s, ok := pmslib.TrySpeak(PtrOnly{name: "a"})
	report("PtrOnly-value  ", s, ok)

	// POSITIVE: the pointer is.
	s, ok = pmslib.TrySpeak(&PtrOnly{name: "a"})
	report("PtrOnly-pointer", s, ok)

	// POSITIVE: a value-receiver method is in BOTH method sets.
	s, ok = pmslib.TrySpeak(ValOnly{name: "b"})
	report("ValOnly-value  ", s, ok)

	s, ok = pmslib.TrySpeak(&ValOnly{name: "b"})
	report("ValOnly-pointer", s, ok)

	// POSITIVE: a mixed-receiver type's VALUE still satisfies through Speak alone.
	s, ok = pmslib.TrySpeak(Mixed{n: 41})
	report("Mixed-value    ", s, ok)

	// POSITIVE through the box, with the pointee mutated first — the shell must
	// dispatch the value-receiver method on the CURRENT pointee, not a stale copy.
	pm := &Mixed{n: 41}
	pm.Bump()
	s, ok = pmslib.TrySpeak(pm)
	report("Mixed-pointer  ", s, ok)

	// NEGATIVE: right name, wrong signature.
	s, ok = pmslib.TrySpeak(WrongSig{})
	report("WrongSig       ", s, ok)

	// NEGATIVE: no such method at all.
	s, ok = pmslib.TrySpeak(42)
	report("int            ", s, ok)

	// FAIL-SOFT NEGATIVE: name-only match against a generic receiver.
	s, ok = pmslib.TryGet(gbox[int]{v: 7})
	report("gbox-int       ", s, ok)

	// POSITIVE: the same generic type closed over the RIGHT element does satisfy.
	s, ok = pmslib.TryGet(gbox[string]{v: "g"})
	report("gbox-string    ", s, ok)
}
