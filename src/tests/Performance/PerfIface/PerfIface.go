package main

import (
	"fmt"
	"time"
)

// The COMMON interface cases: method dispatch through interface values built from
// statically-known types, concrete type assertions with comma-ok, and type switches
// over a closed set -- the paths nearly all Go interface code uses, all resolved
// through the nominal (compile-time) machinery. Contrast row: IfaceShell measures
// the structural duck-typing EXCEPTION, where no compile-time answer exists.
// Integer-only method surface so no string costs bleed into this row.

type Shape interface {
	Area() int
	Perimeter() int
}

type Rect struct{ w, h int }
type Circle struct{ r int }
type Tri struct{ a, b, c int }

func (r Rect) Area() int        { return r.w * r.h }
func (r Rect) Perimeter() int   { return 2 * (r.w + r.h) }
func (c Circle) Area() int      { return 3 * c.r * c.r }
func (c Circle) Perimeter() int { return 6 * c.r }
func (t Tri) Area() int         { return t.a * t.b / 2 }
func (t Tri) Perimeter() int    { return t.a + t.b + t.c }

func run(n int) int {
	shapes := []Shape{Rect{3, 4}, Circle{2}, Tri{6, 3, 7}, Rect{1, 9}, Circle{5}, Tri{4, 4, 6}}
	total := 0

	for i := 0; i < n; i++ {
		s := shapes[i%len(shapes)]

		// dispatch through the interface value
		total += s.Area()
		total += s.Perimeter()

		// concrete type assertion, comma-ok
		if c, ok := s.(Circle); ok {
			total += c.r
		}

		// type switch over the closed set
		switch v := s.(type) {
		case Rect:
			total += v.w
		case Circle:
			total += v.r
		case Tri:
			total += v.c
		}
	}

	return total
}

func main() {
	start := time.Now().UnixNano()

	total := run(20000000)

	elapsed := time.Now().UnixNano() - start
	fmt.Println("checksum:", total)
	fmt.Println("elapsed_ns:", elapsed)
}
