// ChanElemDims is increment D's row: a channel VALUE's element array length. A channel's element is
// not a present value that can be measured (its buffer is not peekable), so the length must ride on
// the channel value the way its direction already does, stamped where the channel is made. Parked
// red after increment B by design; ChanOf's CONSTRUCTED route is covered in CanonicalTypeIdentity.
//
// CORRECTED 2026-09-04. This header used to name increment C as the fix and that was wrong: C
// landed SCOPED TO SLICES (its own commit message says so -- 12 slice-of-array creation sites) after
// the coordinator reversed it from a +8 B per-box field to a zero-byte side table keyed on a slice's
// BACKING ARRAY. That remedy cannot transfer to a channel, because two of the three stamp positions
// (a field's initializer-borne zero, a zero var) produce a NIL channel with no core to key on. The
// sentence survived the scope change unamended, so the row read as "waiting on C" when C was never
// going to reach it. The owner of this row is now increment D -- the unified channel-value cargo,
// where the direction CHAIN (increment 2b's value half) and this element length ride ONE change to
// channel<T>'s field rather than paying the layout cost twice.
//
// DELIBERATELY NOT [GoTestMatchingConsoleOutput], and this is the reason rather than an oversight:
// with the attribute on, the runner diffs this program's stdout against `go run`, and the VALUE row
// below still diverges (Go `chan [3]int` / Elem().Len()=3; C# `chan []int` / 0). Turning the arm on
// today seats a RED guard. It goes on in D's cut, with a negative arm, as the acceptance test that
// the value row actually closed -- which is also the first thing that makes this guard measure the
// dimension it is named for. Until then the Target arm compares the EMITTED text, which cannot see
// a golib type-string change at all.
package main

import (
	"fmt"
	"reflect"
)

func main() {
	c := make(chan [3]int)

	// VALUE row -- RED BY BOUNDARY, and the reason travels with the assertion: a channel value's
	// element length is not measurable (its buffer is not peekable), so increment B does not seed it.
	// Increment D carries it on the channel value the way direction already rides
	// (channel<T>.m_direction, stamped where the channel is made) -- in the SAME field change that
	// makes that direction a per-level chain, since both are value-position cargo on one struct and
	// two increments would pay the layout cost and walk the stamp sites twice. The cost D must state
	// is that field's: channel<T> measures 16 B today (one core reference plus a one-byte enum), and
	// whether a reference-sized cargo rides in the existing padding is a MEASUREMENT D owes, not the
	// arithmetic that predicts it will. A stated expected-fail, not a mysterious one.
	fmt.Printf("value row [red by boundary until increment C: not measurable from a channel value] %%T=%T String()=%s Elem().Len()=%d\n",
		c, reflect.TypeOf(c).String(), reflect.TypeOf(c).Elem().Len())

	// CONSTRUCTED row -- fixed by increment B: ChanOf passes the element's cargo unshifted, so the
	// constructed descriptor's OWN properties hold. Its IDENTITY with TypeOf(c) does not, and cannot until
	// C seeds the value side: an identity row against a boundary side is a boundary row (section 12.4's
	// prediction was wrong on exactly this, and says so). That identity is C's row.
	ct := reflect.ChanOf(reflect.BothDir, reflect.ArrayOf(3, reflect.TypeOf(0)))
	name := ct.String()
	n := ct.Elem().Len()
	fmt.Printf("constructed row: ChanOf(BothDir, ArrayOf(3,int)) String()=%s Elem().Len()=%d\n", name, n)
}
