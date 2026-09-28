package main

import "fmt"

// A DEFINED type over a struct type that holds fixed-size arrays (runtime's `type TimeHistogram
// timeHistogram`). Its zero value must hold zeroed arrays of their full length, exactly as the base
// struct's does, however the value comes to exist: a zero-valued variable, new(), a composite literal,
// a copy, or a package-level global.

type counts struct {
	vals [4]int
	n    int
}

type Counts counts

type grid struct {
	cells [2][3]int
}

type Grid grid

type pt struct{ X, Y int }

type path struct {
	pts [3]pt
}

type Path path

var global Counts

func main() {
	var c Counts
	c.vals[1] = 5
	fmt.Println("zero:", len(c.vals), c.vals, c.n)

	p := new(Counts)
	p.vals[3] = 7
	fmt.Println("new:", len(p.vals), p.vals)

	l := Counts{n: 1}
	l.vals[2] = 9
	fmt.Println("literal:", len(l.vals), l.vals, l.n)

	d := c
	d.vals[0] = 11
	fmt.Println("copy:", c.vals, d.vals)

	global.vals[0] = 3
	fmt.Println("global:", len(global.vals), global.vals)

	var g Grid
	g.cells[1][2] = 6
	fmt.Println("nested:", len(g.cells), len(g.cells[1]), g.cells)

	var pa Path
	pa.pts[2].Y = 8
	fmt.Println("structs:", len(pa.pts), pa.pts)

	// Zero values that exist without a `var`, new() or literal of the defined type itself: an array
	// element, a field inside another struct's zero value or keyed literal, and a slice element.
	var arr [3]Counts
	arr[1].vals[1] = 5
	fmt.Println("array:", len(arr[1].vals), arr[1].vals, arr[2].vals)

	var h holder
	h.c.vals[2] = 4
	fmt.Println("field:", len(h.c.vals), h.c.vals, h.id)

	hl := holder{id: 1}
	hl.c.vals[3] = 6
	fmt.Println("field literal:", len(hl.c.vals), hl.c.vals, hl.id)

	s := make([]Counts, 2)
	s[1].vals[3] = 2
	fmt.Println("slice:", len(s[1].vals), s[1].vals, s[0].vals)
}

type holder struct {
	id int
	c  Counts
}
