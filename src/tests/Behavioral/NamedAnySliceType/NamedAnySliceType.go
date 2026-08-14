// Regression test: a NAMED SLICE TYPE whose element is `any`.
//
// The generated wrapper for a named slice declares both halves of the golib slice
// surface — the typed `ISlice<T>.Append(T[])` and the non-generic `ISlice.Append(object[])`.
// Both were emitted PUBLIC, which is fine while T differs from object and is CS0111 the
// moment it does not: with T = any the two carry the same parameter list. That is one
// duplicate-member emission, and it held two whole test suites — fmt's `type SE []any`
// (63 verdicts) and archive/tar's `type fileOps []any` (97). The non-generic overload is
// now explicit, exactly as golib's own slice<T> declares it, so the public surface is the
// typed one and the interface member is the boxing path a consumer must ask for.
package main

import "fmt"

// fmt_test.go's shape, comment and all: "slice of empty; notational compactness."
type SE []any

// archive/tar's tar_test.go shape: the same named []any used as a table field.
type fileOps []any

type row struct {
	format string
	val    SE
}

func (s SE) describe() string { return fmt.Sprint(len(s), ":", s) }

func main() {
	// The table-driven use both suites are built around: the named []any spread
	// into a variadic ...any parameter.
	rows := []row{
		{"%d", SE{1}},
		{"%d %s", SE{2, "two"}},
		{"%6.2f", SE{12.0}},
	}
	for _, r := range rows {
		fmt.Printf(r.format+"\n", r.val...)
	}

	var s SE
	s = append(s, 1, "two", 3.5)
	fmt.Println(len(s), cap(s) >= 3, s)
	fmt.Println(s[0], s[1], s[2])
	fmt.Println(s[1:], s.describe())

	for i, v := range s {
		fmt.Print(i, "=", v, ";")
	}
	fmt.Println()

	// Spreading one named []any into another, and the second named type in the
	// same package so the wrapper is generated more than once.
	t := SE{}
	t = append(t, s...)
	fmt.Println(len(t), t)

	ops := fileOps{"ab", int64(3), "cde"}
	fmt.Println(len(ops), ops, ops[1:2])

	// The zero value and an empty literal are distinct in Go's printing.
	var zero SE
	fmt.Println(zero == nil, len(zero), zero, SE{} == nil)
}
