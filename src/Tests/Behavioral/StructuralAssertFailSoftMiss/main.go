// StructuralAssertFailSoftMiss guards golib TryTypeAssert's duck-typing wrapper construction as
// FAIL-SOFT: a MISS is normal control flow at every emitted assertion and type-switch site, so a
// wrapper that cannot be built must answer ok=false, never escape as a crash.
//
// The forcing case is a genuine, deliberate imprecision in golib's structural probe: for an
// OPEN-GENERIC receiver method the probe matches by NAME ONLY, because the candidate's signature
// carries the receiver's type parameters and cannot be compared against the interface's concrete
// signature. So `Get() T` on a Go generic type matches `interface{ Get() string }` for EVERY
// instantiation -- `box[int]` included, where Go plainly says no. The probe says yes, the generated
// wrapper's static initializer then finds no bindable extension overload and throws
// NotImplementedException, and reflection re-wraps that as TargetInvocationException:
//
//     go run .   ->  not a string getter
//     C# before  ->  Exception has been thrown by the target of an invocation.   (exit 2)
//
// Both construction paths are covered: the by-value close-over (box, value receiver) and the
// by-pointer close-over over a receiver box (keeper, pointer receiver). Each is exercised with a
// genuine match FIRST, so the guard proves the fail-soft catch narrows nothing -- a real duck-typed
// assertion still resolves and dispatches.
package main

import "fmt"

type box[T any] struct{ v T }

func (b box[T]) Get() T { return b.v }

type keeper[T any] struct{ v T }

func (k *keeper[T]) Get() T { return k.v }

func describe(x any) string {
	if g, ok := x.(interface{ Get() string }); ok {
		return "string getter: " + g.Get()
	}

	return "not a string getter"
}

func main() {
	// value close-over: genuine match, then the false-positive probe
	fmt.Println(describe(box[string]{v: "hello"}))
	fmt.Println(describe(box[int]{v: 5}))

	// pointer close-over: genuine match, then the false-positive probe
	fmt.Println(describe(&keeper[string]{v: "kept"}))
	fmt.Println(describe(&keeper[int]{v: 7}))
}
