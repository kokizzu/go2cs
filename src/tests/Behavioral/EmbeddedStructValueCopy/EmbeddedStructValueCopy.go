package main

import "fmt"

// inner is the embedded struct. Go copies an embedded struct INLINE at every
// value transfer, exactly like any other field, so a copy's writes must never
// reach the source. The emitted C# holds a promoted embed as a field of the
// enclosing struct; while it was held in a shared ж<T> box instead, a plain C#
// struct copy aliased it and every write through the copy hit the original —
// which is what made go/types judge a type parameter not identical to itself
// (substVar's `copy := *v` mutated the ORIGIN's field type in place).
type inner struct {
	n   int
	tag string
}

type mid struct {
	inner
	extra int
}

// deep embeds a struct that itself embeds one: promotion is transitive, and so
// is the copy.
type deep struct {
	mid
	label string
}

// ptrHolder embeds a POINTER. Go copies the pointer, so the pointee stays
// shared, but the copy's own pointer slot is its own.
type ptrHolder struct {
	*inner
	name string
}

func byValue(m mid) mid {
	m.n = 99
	m.tag = "byValue"
	m.extra = 90
	return m
}

func derefCopy(p *deep) deep {
	c := *p
	c.n = 55
	c.tag = "deref"
	c.label = "copy"
	return c
}

func main() {
	// 1. plain assignment copy
	a := mid{inner: inner{n: 1, tag: "a"}, extra: 10}
	b := a
	b.n = 2
	b.tag = "b"
	b.extra = 20
	fmt.Println("assign a:", a.n, a.tag, a.extra)
	fmt.Println("assign b:", b.n, b.tag, b.extra)

	// 2. pass by value
	src := mid{inner: inner{n: 3, tag: "src"}, extra: 30}
	got := byValue(src)
	fmt.Println("call src:", src.n, src.tag, src.extra)
	fmt.Println("call got:", got.n, got.tag, got.extra)

	// 3. pointer deref copy, two embed levels deep
	d := &deep{mid: mid{inner: inner{n: 4, tag: "d"}, extra: 40}, label: "orig"}
	c := derefCopy(d)
	fmt.Println("deref d:", d.n, d.tag, d.label)
	fmt.Println("deref c:", c.n, c.tag, c.label)

	// 4. a POINTER embed: reassigning the copy's embedded pointer must not move
	//    the source's.
	shared := &inner{n: 7, tag: "shared"}
	h1 := ptrHolder{inner: shared, name: "h1"}
	h2 := h1
	h2.inner = &inner{n: 8, tag: "other"}
	fmt.Println("ptr h1:", h1.n, h1.tag, h1.name)
	fmt.Println("ptr h2:", h2.n, h2.tag, h2.name)

	// 5. ... while the POINTEE is genuinely shared when the pointer is not
	//    reassigned.
	h3 := h1
	h3.n = 70
	fmt.Println("ptr shared h1:", h1.n)
	fmt.Println("ptr shared h3:", h3.n)

	// 6. element read out of a slice is a copy
	arr := []mid{{inner: inner{n: 11, tag: "e0"}, extra: 1}, {inner: inner{n: 12, tag: "e1"}, extra: 2}}
	e := arr[0]
	e.n = 111
	e.tag = "elem"
	fmt.Println("slice arr[0]:", arr[0].n, arr[0].tag)
	fmt.Println("slice copy:", e.n, e.tag)
}
