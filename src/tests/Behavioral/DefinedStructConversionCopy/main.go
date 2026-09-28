package main

import "fmt"

// Converting between a defined type and the struct it is defined over COPIES the value, as any Go conversion of a
// struct does: a write through the result must not reach the source's arrays, in either direction and at any
// depth.

type counts struct {
	vals [4]int
	n    int
}

type Counts counts

type grid struct {
	cells [2][3]int
	tags  [2]counts
}

type Grid grid

func main() {
	var c Counts
	c.vals[1] = 5
	b := counts(c)
	b.vals[1] = 99
	fmt.Println("to base:", c.vals[1], b.vals[1])

	var x counts
	x.vals[2] = 7
	w := Counts(x)
	w.vals[2] = 88
	fmt.Println("to wrapper:", x.vals[2], w.vals[2])

	var g Grid
	g.cells[1][2] = 3
	g.tags[1].vals[0] = 4
	h := grid(g)
	h.cells[1][2] = 30
	h.tags[1].vals[0] = 40
	fmt.Println("nested:", g.cells[1][2], g.tags[1].vals[0], h.cells[1][2], h.tags[1].vals[0])

	// A plain assignment was already a copy; it stays one.
	d := c
	d.vals[1] = 11
	fmt.Println("assignment:", c.vals[1], d.vals[1])
}
