package main

import (
	"fmt"
	"time"
)

// PURE interface method dispatch: interface values of statically-known types, built once,
// called in a hot loop -- no assertions, no type switches, no conversions. The simplest and
// most common interface operation there is; the row that answers "what does calling an
// interface method cost?" (Iface adds asserts and type switches; IfaceShell is the
// structural duck-typing exception.) Rotating six values over three concrete types keeps
// the call sites megamorphic so neither runtime can devirtualize the loop away.

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
		total += s.Area()
		total += s.Perimeter()
	}

	return total
}

func main() {
	start := time.Now().UnixNano()

	total := run(50000000)

	elapsed := time.Now().UnixNano() - start
	fmt.Println("checksum:", total)
	fmt.Println("elapsed_ns:", elapsed)
}
