package main

import "fmt"

// Go pointers to the SAME element of the same backing storage are equal, no matter which
// slice header (or the array itself) they were taken through: `&s[0] == &s[0]`, a re-slice
// `&s[1:][0] == &s[1]`, an in-capacity `append` result, and `&a[i] == &a[:][i]`. This test
// pins that element-pointer identity, including its use as a map key.

type node struct {
	id int
}

func main() {
	s := make([]int, 4, 8)
	for i := range s {
		s[i] = i * 10
	}

	// --- self identity and distinctness ---
	fmt.Println("self       :", &s[0] == &s[0])
	fmt.Println("self mid   :", &s[2] == &s[2])
	fmt.Println("distinct   :", &s[0] == &s[1])

	// --- re-slice: a shifted window over the same backing ---
	t := s[1:]
	fmt.Println("reslice    :", &t[0] == &s[1])
	fmt.Println("reslice x  :", &t[0] == &s[0])
	fmt.Println("reslice 2  :", &t[2] == &s[3])

	// --- re-slice of a re-slice ---
	u := t[1:]
	fmt.Println("reslice^2  :", &u[0] == &s[2])

	// --- append inside capacity keeps the backing array ---
	w := append(s[:0:1], 99)
	fmt.Println("append same:", &w[0] == &s[0])

	// --- array-backed slice vs the array itself ---
	var a [4]int
	v := a[:]
	fmt.Println("arr self   :", &a[1] == &a[1])
	fmt.Println("arr vs slc :", &v[2] == &a[2])
	fmt.Println("arr vs slc2:", &v[0] == &a[1])

	// --- struct elements ---
	ns := make([]node, 3)
	fmt.Println("struct elem:", &ns[1] == &ns[1], &ns[1] == &ns[2])

	// --- writes through an element pointer are visible through every view ---
	p := &s[1]
	*p = 42
	fmt.Println("write thru :", s[1], t[0])

	// --- element pointers as map keys (hash must agree with equality) ---
	m := map[*int]string{}
	m[&s[2]] = "two"
	m[&s[3]] = "three"
	fmt.Println("map lookup :", m[&s[2]], m[&s[3]])
	fmt.Println("map len    :", len(m))
	m[&s[2]] = "TWO"
	fmt.Println("map len 2  :", len(m))
	fmt.Println("map reslice:", m[&t[1]], m[&t[2]])
	_, miss := m[&s[0]]
	fmt.Println("map miss   :", miss)

	// --- a distinct backing array is NOT equal ---
	z := make([]int, 4)
	fmt.Println("other back :", &z[0] == &s[0])
}
