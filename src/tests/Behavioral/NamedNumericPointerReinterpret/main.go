package main

import "fmt"

// Locks in the go2cs conversion of a pointer reinterpret between two types that share an underlying:
// `(*uint64)(head)` where head is a *lfstack (type lfstack uint64). This is the runtime pattern
// `atomic.Load64((*uint64)(head))` / `atomic.Cas64((*uint64)(head), …)` on the named atomic types
// lfstack / sweepClass / profAtomic / sysMemStat.
//
// C# has no `ж<lfstack> → ж<uint64>` conversion (distinct generic instantiations). The converter used to
// box a COPY of the [GoType] value conversion — `atomic.Load64(Ꮡ((uint64)(head)))` — which reads
// faithfully but silently discards every WRITE through the derived pointer. Go's `(*U)(p)` names p's OWN
// storage, so both paths are asserted here against `go run`: the derived pointer now routes through
// golib's aliasing reinterpret, `Ꮡhead.Reinterpret<lfstack, uint64>()`.

// load64 / load32 stand in for internal/runtime/atomic.Load64 / Load (which read *p).
//
//go:noinline
func load64(p *uint64) uint64 { return *p }

//go:noinline
func load32(p *uint32) uint32 { return *p }

type lfstack uint64   // runtime/lfstack.go
type sweepClass uint32 // runtime/mgcsweep.go

func (head *lfstack) peek() uint64 { return load64((*uint64)(head)) }
func (sc *sweepClass) peek() uint32 { return load32((*uint32)(sc)) }

// A non-receiver pointer variable (not a deref-aliased receiver) also reinterprets — the box-deref arm.
func peekVia(p *lfstack) uint64 { return load64((*uint64)(p)) }

// Converting a NAMED numeric to a DIFFERENT named type over the IDENTICAL underlying —
// runtime mgc.go's `hex(work.full)` (`type lfstack uint64` → `type hex uint64` print
// diagnostics) — needs the underlying hop `((Δhex)(uint64)full)`: the direct cast is a two-op
// user-defined chain C# won't build (CS0030). The emission skip that trusts the exact [GoType]
// operator is right only for a BASIC-typed operand, so a named operand always hops.
type hexval uint64

//go:noinline
func describe(v hexval) uint64 { return uint64(v) }

// ---------------------------------------------------------------------------------------------
// The WRITE path — the property the copy-box could not express.
//
// encoding/gob is the witness: `Gobber.GobDecode` decodes straight back through a reinterpret used
// as a CALL ARGUMENT — `fmt.Sscanf(string(data), "VALUE=%d", (*int)(g))` — which is neither a deref
// context nor a raw-address source, so it took the value-convert-and-re-box route and Sscanf's write
// landed in a throwaway box. The decoder returned 0 where Go returns 23 (five gob tests).

//go:noinline
func storeInt(p *int, v int) { *p = v }

type gobber int

// Argument position: the reinterpret is never dereferenced here, it is handed to a callee that
// writes through it.
func (g *gobber) set(v int) { storeInt((*int)(g), v) }

// named → named over a struct underlying, with the derived pointer held in a LOCAL and written
// through a field selector.
type coord struct{ X, Y int }
type point coord

//go:noinline
func bump(c *coord) {
	p := (*point)(c)
	p.X, p.Y = 3, 4
}

// basic → named: the address-of form `(*stringReader)(&str)`, fmt's Sscan family.
type namedString string

//go:noinline
func setStr(s *namedString, v string) { *s = namedString(v) }

func main() {
	var a lfstack = 0xDEADBEEF01
	var b sweepClass = 1234
	fmt.Println(a.peek(), b.peek()) // 956397711105 1234
	fmt.Println(peekVia(&a))        // 956397711105

	var c lfstack = 42
	fmt.Println(c.peek()) // 42

	// named → named, identical underlying: value-exact round trip
	h := hexval(a)
	fmt.Println(describe(h), uint64(hexval(c))) // 956397711105 42

	// write-back through an argument-position reinterpret (the gob shape)
	var g gobber
	g.set(23)
	fmt.Println(g) // 23

	// write-back through a named→named struct reinterpret held in a local
	xy := coord{1, 2}
	bump(&xy)
	fmt.Println(xy.X, xy.Y) // 3 4

	// write-back through a basic→named reinterpret of an address-of
	s := "before"
	setStr((*namedString)(&s), "after")
	fmt.Println(s) // after

	// the read path still reads the ORIGINAL storage after a write through the alias
	var d lfstack = 7
	storeInt64((*uint64)(&d), 99)
	fmt.Println(d.peek(), d) // 99 99
}

//go:noinline
func storeInt64(p *uint64, v uint64) { *p = v }
