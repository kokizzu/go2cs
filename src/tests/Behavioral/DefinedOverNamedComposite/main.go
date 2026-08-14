// Regression test: a defined type written DIRECTLY over another NAMED type whose underlying
// is composite.
//
// testing/fstest's own suite has `type shuffledFS MapFS`, where `MapFS` is
// `map[string]*MapFile`. go/types resolves BOTH underlyings past the name to the raw map, so
// the converter's "two different named types sharing a composite underlying" rule fired and
// hopped through it: `((MapFS)(map<@string, ж<MapFile>>)fsys)`. That hop is right for the
// net/mail shape it was written for (Header and MIMEHeader, both written over the RAW
// `map[string][]string`), and wrong here — the wrapper for a non-basic underlying keeps the
// NAMED base, so `shuffledFS` declares exactly ONE conversion operator and it targets `MapFS`.
// The first leg of the hop is then the very two-operator chain the hop exists to prevent
// (shuffledFS -> MapFS -> map): CS0030, and the whole 7-verdict package behind it.
//
// Both directions are guarded, all three arms of the composite switch (map, slice, array),
// and the net/mail control is kept in the same program so the hop is proven to still fire.
package main

import (
	"fmt"

	"DefinedOverNamedComposite/fslike"
)

// The fstest shape, over another package's named MAP.
type shuffledFS fslike.MapFS

func (f shuffledFS) get(k string) int { return fslike.MapFS(f).Get(k) }

// The same over a named SLICE and a named ARRAY.
type shuffledList fslike.List

type shuffledBuf fslike.Buf

// SAME-package sibling: the named base does not have to be foreign for the wrapper to keep it.
type localMap fslike.MapFS

// CONTROL -- the net/mail shape the hop exists for: two defined types written over the RAW
// map, neither over the other. These must still hop through the shared underlying.
type headerA map[string]int

type headerB map[string]int

// Every measurement below goes through the BASE type rather than the wrapper. That is on
// purpose and is not part of what is being guarded: a defined type over a named COMPOSITE
// gets an inherited wrapper that does not expose the golib sequence surface, so `len(s)` on
// one is a separate, pre-existing gap (CS0315 against `builtin.len<TSeq>`) — the same shape
// testing/fstest's `shuffledFS` never asks for, since it only converts back to MapFS.
func main() {
	m := fslike.MapFS{"a": 1, "b": 2}

	// Forward: the arg's written base IS the target.
	s := shuffledFS(m)
	fmt.Println(s.get("a"), s.get("b"))

	// Reverse: the target's written base IS the arg's type.
	back := fslike.MapFS(s)
	fmt.Println(back.Get("b"), back.Size())

	// Same-package declaration over the same foreign named type.
	lm := localMap(m)
	fmt.Println(fslike.MapFS(lm).Get("a"), fslike.MapFS(lm).Size())

	// Named slice arm, both directions.
	l := fslike.List{3, 1, 2}
	sl := shuffledList(l)
	fmt.Println(fslike.List(sl).Sum(), fslike.List(sl)[0], len(fslike.List(sl)))

	// Named array arm, both directions.
	b := fslike.Buf{7, 8}
	sb := shuffledBuf(b)
	fmt.Println(fslike.Buf(sb).First(), fslike.Buf(sb)[1])

	// Control: two raw-map siblings still convert through the shared underlying.
	ha := headerA{"x": 9}
	hb := headerB(ha)
	fmt.Println(hb["x"], len(hb), len(headerA(hb)))
}
