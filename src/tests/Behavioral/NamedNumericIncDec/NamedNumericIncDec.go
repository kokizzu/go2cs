package main

import "fmt"

// idx is a defined type over uint (→ C# nuint). Go allows ++/-- on a named integer; the C# named-numeric
// wrapper must generate operator ++/-- returning the named type (the runtime uses this for chunkIdx /
// arenaIdx / statDep loop counters; without it, c++ promoted through the implicit conversion → CS0266).
type idx uint
type sidx int // signed named numeric (→ C# nint)

// cx is a defined type over complex128. Go defines ==/!=, +/-/*//, unary -, and ++/-- on complex
// kinds but NO ordered comparisons and NO % — the generated C# wrapper must emit exactly that
// operator set (emitting </<=/>/>=/% for a named complex was CS0019 ×10, testing/quick's
// TestComplex64Alias/TestComplex128Alias).
type cx complex128

func main() {
	// ++ on an unsigned named numeric, observed via a plain iteration counter (avoids int(named)).
	n := 0
	for c := idx(0); c < idx(5); c++ {
		n++
	}
	fmt.Println(n) // 5

	// -- on the same
	m := 0
	for c := idx(4); c > idx(0); c-- {
		m++
	}
	fmt.Println(m) // 4

	// ++/-- on a signed named numeric, observed by comparing to a named value.
	var d sidx = 3
	d++
	d++
	d--
	fmt.Println(d == sidx(4)) // true

	// The full Go operator set for a named complex: ++/--, arithmetic, equality. (Asserted by
	// comparison, not printed — the baseline stub fmt renders a named complex via ToString, a
	// documented proxy gap; the operator-set emission is what this guards.)
	c := cx(complex(1, 2))
	c++
	c--
	e := c*cx(complex(2, 0)) + cx(complex(0, 1))
	fmt.Println(c == cx(complex(1, 2)))                // true
	fmt.Println(e/cx(complex(2, 0)) == cx(complex(1, 2.5))) // true
}
