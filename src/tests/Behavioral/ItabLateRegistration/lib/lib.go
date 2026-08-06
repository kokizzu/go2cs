// Package lib declares the interfaces, the dynamic types, and the assert helpers — but never a
// conversion between them. Every helper takes `any`, so the assert site has no statically known
// dynamic type and the converter can record nothing: resolution is the runtime duck-typing tier
// in every case, which is what the guard needs.
package lib

import "fmt"

// Named and Sized are two interfaces over the SAME dynamic types. golib memoizes per closed
// generic, so the two caches must stay separate and must not answer for each other.
type Named interface {
	Name() string
}

type Sized interface {
	Size() int
}

type Circle struct{ R int }

func (c Circle) Name() string { return fmt.Sprintf("circle(%d)", c.R) }

func (c Circle) Size() int { return c.R * 2 }

type Square struct{ S int }

func (s Square) Name() string { return fmt.Sprintf("square(%d)", s.S) }

func (s Square) Size() int { return s.S * 4 }

// Blank satisfies neither interface. Its MISS is memoized exactly like a hit, so it has to stay
// a miss — including after a registration invalidates the cache.
type Blank struct{ N int }

func (b Blank) Tag() string { return "blank" }

func TryName(v any) (string, bool) {
	n, ok := v.(Named)

	if !ok {
		return "-", false
	}

	return n.Name(), true
}

func TrySize(v any) (int, bool) {
	s, ok := v.(Sized)

	if !ok {
		return -1, false
	}

	return s.Size(), true
}
