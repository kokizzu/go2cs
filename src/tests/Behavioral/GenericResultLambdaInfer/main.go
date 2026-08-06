// GenericResultLambdaInfer guards a func LITERAL passed to a GENERIC function whose corresponding
// parameter type returns a TYPE PARAMETER — `sync.OnceValue[T any](f func() T)`,
// `OnceValues[T1, T2 any](f func() (T1, T2))`.
//
// Such a literal sits in C# type-argument INFERENCE position: C# derives the type argument from the
// lambda's own return expressions and ignores the Go result type go/types already resolved. Two
// failure shapes follow. A body that yields no C# type at all — one terminated by `panic`, or one
// whose only arm is an untyped `nil` (emitted `default!`) — infers nothing (CS0411, sync
// oncefunc_test ×4). A body whose NATURAL C# type merely differs from the declared Go result —
// `func() int { return 42 }` is naturally `Func<int>` where Go's `int` is `nint` — infers the wrong
// delegate (CS0029). The converter now states the declared result type on the lambda
// (`Once(nint () => 42)`, `Once(any () => …)`, `Once2((any, any) () => …)`), fixing the type
// argument to exactly what Go declared.
//
// The negative control is load-bearing: a type parameter appearing only in the callee's PARAMETER
// list is inferred from the lambda's own typed parameters, so those literals must stay unprefixed
// (a blanket rule would churn every `slices.SortFunc`-shaped call site in the corpus).
package main

import "fmt"

// Result-position type parameter: T is inferable ONLY from the lambda's return type.
func Once[T any](f func() T) func() T {
	var v T
	var done bool
	return func() T {
		if !done {
			v = f()
			done = true
		}
		return v
	}
}

// Two result-position type parameters.
func Once2[A, B any](f func() (A, B)) func() (A, B) {
	var a A
	var b B
	var done bool
	return func() (A, B) {
		if !done {
			a, b = f()
			done = true
		}
		return a, b
	}
}

// A result-position type parameter in a MULTI-parameter callee still states its type.
func reduce[E any](xs []E, combine func(a, b E) E) E {
	acc := xs[0]
	for _, x := range xs[1:] {
		acc = combine(acc, x)
	}
	return acc
}

// NEGATIVE CONTROL — the slices.SortFunc shape: E appears only in the comparator's PARAMETER list,
// its result is a plain `bool`. C# infers E from the lambda's own typed parameters, so these
// literals must keep the plain inferred form (a blanket rule would churn every such call site).
func pick[E any](xs []E, better func(a, b E) bool) E {
	best := xs[0]
	for _, x := range xs[1:] {
		if better(x, best) {
			best = x
		}
	}
	return best
}

// A package-level var whose declared C# type comes from the Go result: `func() int` → Func<nint>.
var fortyTwo = Once(func() int { return 42 })

func describe(v any) string {
	if v == nil {
		return "<nil>"
	}
	return fmt.Sprintf("%v", v)
}

func main() {
	// int result: the arm's natural C# type (int) differs from Go's int (nint).
	fmt.Println(fortyTwo(), fortyTwo())

	// any result with an untyped-nil arm: no arm contributes a type.
	nilOnce := Once(func() any { return nil })
	fmt.Println(describe(nilOnce()))

	// any result with a panic-terminated body: no return arm at all.
	panicOnce := Once(func() any { panic("boom") })
	func() {
		defer func() { fmt.Println("recovered:", describe(recover())) }()
		panicOnce()
	}()

	// two any results, both untyped nil.
	pairOnce := Once2(func() (any, any) { return nil, nil })
	p, q := pairOnce()
	fmt.Println(describe(p), describe(q))

	// A concrete multi-result instantiation still works.
	both := Once2(func() (int, string) { return 7, "seven" })
	n, s := both()
	fmt.Println(n, s)

	// Result-position type parameter in a multi-parameter callee.
	fmt.Println(reduce([]int{1, 2, 3, 4}, func(a, b int) int { return a + b }))
	fmt.Println(reduce([]string{"a", "b", "c"}, func(a, b string) string { return a + b }))

	// NEGATIVE CONTROL: parameter-position type parameter only, inferred from the lambda's params.
	fmt.Println(pick([]int{3, 9, 4}, func(a, b int) bool { return a > b }))
	fmt.Println(pick([]string{"pear", "fig", "plum"}, func(a, b string) bool { return len(a) < len(b) }))
}
