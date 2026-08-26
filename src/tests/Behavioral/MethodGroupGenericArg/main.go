package main

import "fmt"

func addInt(a, b int) int { return a + b }

// foldSlice mirrors slices.SortFunc's shape: the element type E appears only in the `~[]E`
// CONSTRAINT and in the func-typed parameter. C# cannot infer E from the constraint, and a
// METHOD-GROUP argument (`addInt`, a bare function reference) gives it nothing either, so the
// converter must spell out the type arguments — bare `foldSlice(nums, addInt)` is CS0411
// (encoding/asn1's `slices.SortFunc(l, bytes.Compare)`).
func foldSlice[S ~[]E, E any](s S, combine func(E, E) E) E {
	var acc E
	for _, v := range s {
		acc = combine(acc, v)
	}
	return acc
}

// equalPair is a GENERIC comparator, so a call site names it as an EXPLICIT instantiation
// (`equalPair[int]`). That is still a C# method group — writing the type arguments fixes the
// group's shape, not its inference status — and the enclosing generic call must therefore spell
// ITS type arguments out exactly as the un-instantiated `addInt` form does.
func equalPair[T comparable](a, b T) bool { return a == b }

// pairEqual mirrors slices.EqualFunc: FOUR type parameters, two of them (E1, E2) reachable only
// through the `~[]E` constraints and the comparator's own parameter list. Called with an
// explicitly-instantiated comparator this was CS0411 — the converter's method-group predicate met
// an index expression and reported "not a method group", so the call kept its uninferable form.
func pairEqual[S1 ~[]E1, S2 ~[]E2, E1, E2 any](s1 S1, s2 S2, eq func(E1, E2) bool) bool {
	if len(s1) != len(s2) {
		return false
	}

	for i := range s1 {
		if !eq(s1[i], s2[i]) {
			return false
		}
	}

	return true
}

// insertAt mirrors slices.Insert: E lives in NO parameter but the variadic tail, so a call that
// supplies no variadic values leaves C# nothing to infer it from (`insertAt(s, 1)` — CS0411, then
// CS1503 as the wrong overload binds). Go infers E through S's core type.
func insertAt[S ~[]E, E any](s S, i int, v ...E) S {
	out := make(S, 0, len(s)+len(v))
	out = append(out, s[:i]...)
	out = append(out, v...)
	out = append(out, s[i:]...)

	return out
}

// reverse is referenced as a BARE-IDENT VALUE (`applyTo(s, reverse)`) rather than called. C# infers
// a method group's type arguments only from the target delegate's own parameter types, and E
// appears in none of them — the qualified `pkg.Func` form had spelled them out for a while; the
// same-package bare form had no equivalent.
func reverse[S ~[]E, E any](s S) {
	for i, j := 0, len(s)-1; i < j; i, j = i+1, j-1 {
		s[i], s[j] = s[j], s[i]
	}
}

func applyTo[S any](v S, f func(S)) { f(v) }

// namedInts makes `reverse`'s instantiation a NAMED slice type, so the emitted type-argument list
// is not the plain `[]int` spelling.
type namedInts []int

// sliceEq is instantiated PARTIALLY at the call site below (`sliceEq[row]` — Go writes the prefix
// and infers E). C# has no partial instantiation, so the written prefix alone is CS0305,
// "requires 2 type arguments"; the resolved list has to complete it.
func sliceEq[S ~[]E, E comparable](a, b S) bool {
	if len(a) != len(b) {
		return false
	}

	for i := range a {
		if a[i] != b[i] {
			return false
		}
	}

	return true
}

type row []int

// rowsEqual is slices' own iter_test chunkEqual shape: a partial instantiation used as a VALUE,
// handed to a four-type-parameter generic as a method-group argument.
func rowsEqual(a, b []row) bool {
	return pairEqual(a, b, sliceEq[row])
}

func main() {
	nums := []int{1, 2, 3, 4}
	sum := foldSlice(nums, addInt) // method-group comparator; E inferable only via S ~[]E
	fmt.Println(sum)               // 10

	// An EXPLICITLY INSTANTIATED generic comparator is still a method group.
	fmt.Println(pairEqual(nums, []int{1, 2, 3, 4}, equalPair[int]))
	fmt.Println(pairEqual(nums, []int{1, 2, 9, 4}, equalPair[int]))

	// A variadic type parameter with NO variadic argument supplied.
	fmt.Println(insertAt(nums, 1))
	fmt.Println(insertAt(nums, 1, 7, 8))

	// A generic function referenced as a bare-ident VALUE, at two different instantiations.
	plain := []int{1, 2, 3}
	applyTo(plain, reverse)
	fmt.Println(plain)

	named := namedInts{4, 5, 6}
	applyTo(named, reverse)
	fmt.Println(named)

	// A PARTIAL explicit instantiation used as a value.
	fmt.Println(rowsEqual([]row{{1, 2}, {3}}, []row{{1, 2}, {3}}))
	fmt.Println(rowsEqual([]row{{1, 2}, {3}}, []row{{1, 2}, {4}}))
}
