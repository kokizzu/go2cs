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

}
