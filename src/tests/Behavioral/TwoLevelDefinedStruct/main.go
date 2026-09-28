package main

import "fmt"

// A defined type over a defined type over a struct (`type Tally Counts`, `type Counts counts`) has the
// struct's fields in Go, however many definitions sit between them.

type counts struct {
	vals [4]int
	n    int
}

type Counts counts

type Tally Counts

type Score Tally

func main() {
	var t Tally
	t.vals[1] = 5
	t.n = 2
	fmt.Println("zero:", len(t.vals), t.vals, t.n)

	p := new(Tally)
	p.vals[3] = 7
	fmt.Println("new:", len(p.vals), p.vals)

	l := Tally{n: 1}
	l.vals[2] = 9
	fmt.Println("literal:", len(l.vals), l.vals, l.n)

	c := t
	c.vals[0] = 11
	fmt.Println("copy:", t.vals, c.vals)

	var s Score
	s.vals[0] = 3
	s.n = 4
	fmt.Println("three levels:", len(s.vals), s.vals, s.n)
}
