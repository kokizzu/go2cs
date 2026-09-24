package main

import "fmt"

// Go's compiler grows x in place for `append(x, make([]T, n)...)` (extendslice) and never allocates
// the make. The conversion emits `appendꓸꓸꓸ(x, makeꓸꓸꓸ<T>(n))` for exactly that shape (REC-C,
// docs/phase4/DESIGN-slice-idiom-allocations.md §B); every observable result below must equal what
// the ordinary make-then-append gives, which is what Go's own rewrite guarantees too.

func extend(s []int, n int) []int {
	return append(s, make([]int, n)...)
}

// Grow is slices.Grow's own body: re-slice to capacity, extend, re-slice back.
func Grow(s []int, n int) []int {
	if n -= cap(s) - len(s); n > 0 {
		s = append(s[:cap(s)], make([]int, n)...)[:len(s)]
	}
	return s
}

func growGeneric[S ~[]E, E any](s S, n int) S {
	return append(s, make(S, n)...)
}

type point struct{ X, Y int }

// Refused shapes keep the ordinary emission: a capacity argument, a make bound to a name, and an
// element whose zero value must be constructed (a fixed-size array).
func withCap(s []int, n int) []int { return append(s, make([]int, n, n+1)...) }

func named(s []int, n int) []int {
	m := make([]int, n)
	return append(s, m...)
}

func arrays(s [][2]int, n int) [][2]int { return append(s, make([][2]int, n)...) }

func main() {
	s := make([]int, 2, 4)
	s[0], s[1] = 1, 2
	stale := s[:4]
	stale[2], stale[3] = 99, 99

	e := extend(s, 2)
	fmt.Println("in place:", len(e), cap(e), e, &e[0] == &s[0])

	f := extend(s, 3)
	fmt.Println("grows:", len(f), cap(f), f, &f[0] == &s[0])

	var none []int
	n := extend(none, 3)
	fmt.Println("nil:", len(n), cap(n), n)
	fmt.Println("zero:", len(extend(s, 0)), extend(nil, 0) == nil)

	g := Grow([]int{1, 2}, 5)
	fmt.Println("Grow:", len(g), cap(g) >= 7, g)

	b := growGeneric([]byte("ab"), 2)
	fmt.Println("generic:", len(b), b)
	p := growGeneric([]point{{1, 2}}, 1)
	fmt.Println("generic struct:", p)

	fmt.Println("refused:", withCap([]int{1}, 2), named([]int{1}, 1), arrays(nil, 2))

	func() {
		defer func() { fmt.Println("negative:", recover() != nil) }()
		k := -1
		_ = extend(s, k)
	}()
}
