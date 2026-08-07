package main

import "fmt"

// The unique.Handle shape: the only field is *T, whose pointer-identity == is
// valid for every T — equality must not depend on T's own constraints.
type handle[T comparable] struct {
	p *T
}

// A plain type-parameter field: no == is available for T itself, so equality
// falls back to the field type's own equality.
type box[T any] struct {
	v T
}

// No field mentions T at all (the internal/weak.Pointer / Cache[T] shape).
type meta[T any] struct {
	hits int
}

// Mixed: a type-parameter field beside a concrete field (the database/sql.Null shape).
type optval[T any] struct {
	v     T
	valid bool
}

// A generic struct field inside another generic struct.
type outer[T comparable] struct {
	inner box[T]
	n     int
}

func main() {
	x, y := 5, 5
	h1, h2, h3 := handle[int]{&x}, handle[int]{&x}, handle[int]{&y}
	fmt.Println(h1 == h2, h1 == h3, h1 == handle[int]{})

	b1 := box[string]{"go"}
	fmt.Println(b1 == box[string]{"go"}, b1 == box[string]{"cs"})
	fmt.Println(box[int]{7} == box[int]{7}, box[int]{7} == box[int]{8})

	m1, m2 := meta[string]{3}, meta[float64]{3}
	fmt.Println(m1 == meta[string]{3}, m2 == meta[float64]{4})

	n1, n2 := optval[string]{"a", true}, optval[string]{"a", false}
	fmt.Println(n1 == optval[string]{"a", true}, n1 == n2)

	o1, o2 := outer[string]{box[string]{"k"}, 1}, outer[string]{box[string]{"k"}, 1}
	fmt.Println(o1 == o2, o1 == outer[string]{box[string]{"k"}, 2})

	counts := map[box[string]]int{}
	counts[box[string]{"k"}]++
	counts[box[string]{"k"}]++
	counts[box[string]{"j"}]++
	fmt.Println(counts[box[string]{"k"}], counts[box[string]{"j"}], len(counts))
}
