package main

import "fmt"

// A generic call that infers a type parameter from a CONSTANT argument drives C# type
// inference from the literal's OWN C# type: a bare `0` is C# `int` (System.Int32), while
// go/types resolved the parameter to Go `int` (go2cs `nint`). C# repairs that wherever an
// implicit conversion bridges it, so most such calls are fine and stay bare — but C#
// generics are INVARIANT, so wherever the type parameter reaches a CONSTRUCTED type
// (`Action<int, bool>` is not `Action<nint, bool>`, `slice<int>` is not `slice<nint>`)
// nothing can repair it and the call fails to bind (CS1503).
//
// internal/concurrent's own test suite ships the control pair this file is built around:
// `expectMissing[K, V comparable](t, key K, want V) func(got V, ok bool)` called as
// `expectMissing(t, s, 0)` mis-inferred V and its returned delegate then rejected the
// map's nint (CS1503 x16), while `expectDeleted(..., 15) func(deleted bool)` — the same
// untyped literal, V absent from the result — compiled untouched.

// ---- the defect: the type parameter reaches a FUNC result ----------------------------

// wantValue mirrors expectMissing: T reaches the RETURNED func's signature, so the
// delegate's type argument is materialized at the call site and must be nint.
func wantValue[T comparable](want T) func(T, bool) {
	return func(got T, ok bool) {
		if ok && got == want {
			fmt.Println("match", got)
		} else {
			fmt.Println("nomatch", got, want, ok)
		}
	}
}

// ---- the controls: nothing to mis-infer, so these must stay BARE ---------------------

// wantPresent mirrors expectDeleted: T never reaches the returned func's signature.
func wantPresent[T comparable](want T) func(bool) {
	return func(ok bool) {
		fmt.Println("present", want, ok)
	}
}

// takeOnly has no result at all — the instantiation is self-consistent.
func takeOnly[T comparable](want T) {
	fmt.Println("took", want)
}

// bareResult returns the type parameter ITSELF. C#'s implicit int->nint conversion repairs
// this one at the use site, so it keeps its bare literal (the no-churn arm).
func bareResult[T any](v T) T {
	return v
}

// ---- other invariant result shapes the same rule covers ------------------------------

func sliceOf[T any](v T) []T {
	return []T{v}
}

func mapTo[K comparable, V any](k K, v V) map[K]V {
	return map[K]V{k: v}
}

func chanOf[T any](v T) chan T {
	ch := make(chan T, 1)
	ch <- v
	return ch
}

// pairValue infers TWO parameters from constants; only the one reaching the result needs
// retyping, and the string argument must be left exactly as it already was.
func pairValue[K comparable, V comparable](k K, v V) func(V, bool) {
	return func(got V, ok bool) {
		fmt.Println("pair", k, got, ok)
	}
}

// ---- non-int widths: the cast type comes from the RESOLVED type ----------------------

// wantLike takes T twice, so a typed first argument fixes T and the CONSTANT second one
// resolves to that width. C# is not saved by the sibling: it infers from every argument
// and picks the type they all convert to, so a bare `3` alongside a `byte` still yields
// `int`. The cast must therefore follow the RESOLVED type, whatever it is.
func wantLike[T comparable](proto T, want T) func(T, bool) {
	return func(got T, ok bool) {
		fmt.Println("like", got, want, ok)
	}
}

func wantInt64[T int64](want T) func(T, bool) {
	return func(got T, ok bool) {
		fmt.Println("i64", got, want, ok)
	}
}

func main() {
	var i int = 7

	// The defect shapes: T reaches the returned func, so each constant must be retyped.
	wantValue(0)(i, false)
	wantValue(7)(i, true)

	// Controls: T absent from the result, or no result at all.
	wantPresent(15)(true)
	takeOnly(15)

	// Control: explicitly instantiated by the Go source — nothing to infer.
	wantValue[int](0)(i, false)

	// Control: a NON-constant argument already carries the mapped Go type in C#.
	wantValue(i)(i, true)

	// Control: a bare type-parameter result is repaired by implicit conversion.
	fmt.Println("bare", bareResult(42))

	// Other constant kinds already carry their C# type and must not gain noise.
	var f float64 = 2.5
	wantValue(0.0)(f, false)
	wantValue(2.5)(f, true)

	var r rune = 'q'
	wantValue('a')(r, false)
	wantValue('q')(r, true)

	var s string = "hi"
	wantValue("")(s, false)
	wantValue("hi")(s, true)

	var b bool = true
	wantValue(true)(b, true)

	// Two inferred parameters; only V reaches the returned delegate.
	pairValue("k", 0)(i, false)

	// Folded constant EXPRESSIONS mis-infer exactly as a bare literal does.
	wantValue(3 + 4)(i, true)
	wantValue(1 << 10)(1<<10, true)

	// Other invariant result shapes.
	fmt.Println("slice", sliceOf(9))
	fmt.Println("map", mapTo("one", 1)["one"])
	fmt.Println("chan", <-chanOf(11))

	// Narrower/wider resolved widths pick their own cast type.
	wantLike(byte(0), 3)(3, true)
	wantInt64(1234567890123)(1234567890123, true)
}
