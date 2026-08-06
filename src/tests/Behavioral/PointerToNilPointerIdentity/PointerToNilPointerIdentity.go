package main

import "fmt"

// Guards STRUCTURAL nil-pointer identity in golib ж equality: a heap box holding a nil
// pointer VALUE (a ж<ж<T>> captured local) is a non-nil pointer holding nil — two distinct
// such addresses are unequal, and neither equals nil. The old value-peeking equality
// reported &p1 == &p2 (both true) whenever *p1 and *p2 were both nil.
//
// Extended for the whole predicate class: a pointer's identity is its STORAGE, never anything
// about the value it points at. ж equality used to derive identity from the pointee for a
// reference-typed pointee, so two distinct `*int` variables holding the same pointer reported
// &c == &d, a map[**int]V collapsed those two distinct keys into one entry, and the hash of a
// pointer key MUTATED when its pointee was assigned — a key inserted while the pointee was nil
// could never be found again.

type holder struct {
	p *int
	s []int
}

func main() {
	p1 := (*int)(nil)
	p2 := (*int)(nil)

	pp1 := &p1
	pp2 := &p2

	// Distinct addresses of nil-holding pointer variables are DISTINCT.
	fmt.Println("pp1==pp2", pp1 == pp2)

	// An address is never nil, no matter what it holds.
	fmt.Println("pp1==nil", pp1 == nil)

	// The held values ARE nil.
	fmt.Println("*pp1==nil", *pp1 == nil)
	fmt.Println("*pp1==*pp2", *pp1 == *pp2)

	// Same address compares equal to itself through another alias.
	alias := pp1
	fmt.Println("alias==pp1", alias == pp1)

	// Writing through one alias is visible through the other (same storage).
	n := 42
	*alias = &n
	fmt.Println("p1 set", *pp1 != nil, **pp1)

	// Two DISTINCT variables holding the SAME pointer still have distinct addresses.
	v := 5
	c := &v
	d := &v
	fmt.Println("c==d", c == d, "&c==&d", &c == &d)

	// ... so they are two separate keys of a map[**int]string.
	keys := map[**int]string{&c: "c", &d: "d"}
	fmt.Println("keys", len(keys), keys[&c], keys[&d])

	// A pointer key's hash must not move when its POINTEE is assigned: insert while the
	// pointee is nil, then assign it, then look the same key up again.
	var late *int
	lp := &late
	stable := map[**int]string{lp: "stable"}
	fmt.Println("before", stable[lp])
	late = &v
	fmt.Println("after", stable[lp], len(stable), **lp)

	// The address of a reference-typed FIELD is a real address too: it dereferences to the
	// field's own value (nil while unset), and keys a map by the field it names.
	var h holder
	fp := &h.p
	fs := &h.s
	fmt.Println("field nil", *fp == nil, *fs == nil, len(*fs))
	fields := map[**int]string{fp: "h.p"}
	fmt.Println("field key", fields[fp])
	h.p = &v
	fmt.Println("field set", fields[fp], **fp, *fp == c)
}
