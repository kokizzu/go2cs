// Same-package VALUE-implement guard: the pair a package SATISFIES but never WITNESSES.
//
// Every [assembly: GoImplement<T, Iface>] record the converter writes comes from a CAST it
// converted, so a package that satisfies its own exported interface structurally and never
// casts — encoding/binary's `var BigEndian bigEndian` against ByteOrder — recorded nothing,
// and each consumer minted a local `<pkg>_<T>ᴠ<Iface>` adapter instead. That adapter is a
// SECOND IDENTITY for one Go value: fmt and reflect see the wrapper where the value's own
// type belongs, and a directly-boxed value compares unequal to a wrapped one. Measured corpus
// footprint of the constructions it removes: 89, as encoding/binary bigEndian/littleEndian →
// ByteOrder (79), image/color Palette → Model (5) and crypto Hash → SignerOpts (5).
//
// The declaring side is `ledger`. Post-fix its metadata carries Counter → Metric and Gauge →
// Metric, THIS package's metadata carries neither (it no longer needs a local record), and
// the conversions below hand over the bare value. The four negatives — a named FUNC type, a
// pointer-only implementer, an unexported interface and a generic — must all still take the
// path they took before, which is what keeps the rule from over-recording.
package main

import (
	"fmt"

	"SamePackageImplementNoWitness/ledger"
)

// Marker and Stamp are the SAME-ASSEMBLY shape of the same rule: a local exported interface
// that a local value type satisfies with no cast anywhere. Nothing converts one to the other,
// so the only evidence is this package's own metadata — the golden pins it.
type Marker interface {
	Mark() string
}

type Stamp struct {
	S string
}

func (s Stamp) Mark() string { return s.S }

func main() {
	// A struct whose VALUE method set satisfies the foreign interface with no witness in the
	// declaring package: the bare value must land in the interface slot.
	var m ledger.Metric = ledger.Counter{N: 7}
	want := ledger.Counter{N: 7}

	// %v must render the STRUCT's fields and %T its Go dynamic type; a wrapper in the slot
	// makes fmt reflect over the WRAPPER instead.
	fmt.Printf("counter: %v %T\n", m, m)

	// Identity in both directions: an interface value and a concrete value compare on dynamic
	// type plus value, so a wrapped one and a directly-boxed one must still be equal.
	fmt.Println("eq:", m == want, want == m)
	fmt.Println("ne:", m == ledger.Counter{N: 8})

	// A named NUMERIC implementer, so no fix can be a one-shape special case.
	var g ledger.Metric = ledger.Gauge(4)
	fmt.Printf("gauge: %v %T %d\n", g, g, g.Value())

	// Assert, type switch and map key all resolve on the DYNAMIC type.
	c, ok := m.(ledger.Counter)
	fmt.Println("assert:", ok, c.N)
	_, badOK := m.(ledger.Gauge)
	fmt.Println("assert-miss:", badOK)

	switch t := g.(type) {
	case ledger.Counter:
		fmt.Println("switch:", "Counter", t.N)
	case ledger.Gauge:
		fmt.Println("switch:", "Gauge", int(t))
	default:
		fmt.Println("switch:", "default")
	}

	seen := map[ledger.Metric]string{}
	seen[m] = "from-iface"
	fmt.Println("map:", seen[want], len(seen))

	// The method still dispatches through the interface.
	fmt.Println("call:", m.Value())

	// An EXPLICIT interface conversion of a foreign value — the crypto/tls
	// `crypto.SignerOpts(sigHash)` shape, five of the corpus constructions.
	sc := ledger.Metric(ledger.Gauge(9))
	fmt.Println("explicit:", sc.Value())

	// NEGATIVE — a named FUNC type. Its pair must NOT be recorded, so this conversion keeps a
	// local adapter and has to keep working.
	var f ledger.Metric = ledger.Meter(func() int { return 11 })
	fmt.Println("func:", f.Value())

	// NEGATIVE — POINTER method set only, a separate increment: the pointer path is unchanged.
	var p ledger.Metric = &ledger.Tally{N: 5}
	fmt.Println("ptr:", p.Value())

	// NEGATIVE — an UNEXPORTED interface, satisfied and used entirely inside ledger.
	fmt.Println("hidden:", ledger.Depth(3))

	// NEGATIVE — a GENERIC implementer, never converted to the interface.
	b := ledger.Box[int]{V: 9}
	fmt.Println("generic:", b.Value(), b.V)

	// The same rule inside ONE assembly.
	fmt.Println("local:", Stamp{S: "s"}.Mark())
}
