// Guards golib's unified itab cache — one entry per (dynamic type, interface) whichever tier
// produced it, a monomorphic slot in front of the dictionary, and a registration EPOCH so that a
// nominal adapter registered by a lazily-loaded assembly is never shadowed by an already-memoized
// runtime shell (the hazard the two separate caches used to avoid by construction).
//
// What each section pins:
//
//  1. RESOLUTION — lib's helpers take `any`, so no conversion site exists for the converter to
//     record and every pair below is decided by the runtime duck-typing tier.
//  2. THRASH — alternating dynamic types through ONE interface displaces the monomorphic slot on
//     every call. The slot holds (type, resolver) as a single immutable pair precisely so it can
//     never be observed torn; were it ever to pair type A with type B's resolver, the wrong
//     implementation would be constructed and these lines would print the wrong name or a miss.
//  3. MISS — Blank satisfies neither interface. Its decided miss is memoized like a hit and must
//     stay a miss, including across the invalidation below.
//  4. SEPARATION — the same dynamic types through a SECOND interface; the per-closed-generic
//     caches must not answer for each other.
//  5. EPOCH — primeLate is the only reference to the latelib assembly, so latelib's module
//     initializer — which registers the generated nominal pointer adapters for two of the pairs
//     sections 1–4 have already decided as shells — runs late and bumps the epoch. Every assert
//     is then repeated and must answer identically after each pair is re-formed.
//
// Section 5 does fire: an instrumented run records all six pairs at epoch 0, both REGISTERs at
// primeLate, and all six re-formed at epoch 2 — two of them now nominal, four re-decided as
// before, the misses still misses.
//
// What this file can and cannot pin. The epoch exists to keep TIER PRECEDENCE (a nominal adapter
// must win over an already-memoized shell), and that is deliberately NOT observable from Go: both
// tiers forward to the same receiver methods and unwrap to the same dynamic value, so a shell
// answering where an adapter should would print exactly these lines. It could only become visible
// as a Go↔C# divergence, which the suite forbids by construction. What this guard therefore proves
// is that the invalidation is complete and harmless — that re-forming every entry cannot lose one,
// mispair the monomorphic slot, turn a miss into a hit, or cross the two interfaces' caches.
package main

import (
	"fmt"

	"ItabLateRegistration/latelib"
	"ItabLateRegistration/lib"
)

// round is one full pass over every pair the cache has to answer. It runs twice — once before the
// late assembly is loaded and once after — and both passes must print identically.
func round(tag string) {
	c := lib.Circle{R: 3}
	q := lib.Square{S: 4}
	b := lib.Blank{N: 5}

	for i := 0; i < 3; i++ {
		n1, ok1 := lib.TryName(&c)
		n2, ok2 := lib.TryName(&q)
		fmt.Println(tag, "name", i, n1, ok1, n2, ok2)
	}

	n3, ok3 := lib.TryName(&b)
	fmt.Println(tag, "miss", n3, ok3)

	s1, ok4 := lib.TrySize(&c)
	s2, ok5 := lib.TrySize(&q)
	s3, ok6 := lib.TrySize(&b)
	fmt.Println(tag, "size", s1, ok4, s2, ok5, s3, ok6)
}

// primeLate isolates the ONLY reference to latelib in its own function, so the module is not
// touched while main's body is being prepared.
func primeLate() (string, int) {
	return latelib.Prime()
}

func main() {
	round("before")

	pn, ps := primeLate()
	fmt.Println("primed", pn, ps)

	round("after ")
}
