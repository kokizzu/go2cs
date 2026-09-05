// A channel's DIRECTION is per-LEVEL, and a scalar can only describe the outermost one. `chan
// (<-chan int)` and `chan chan int` are different Go types that a one-slot cargo renders
// identically, so the direction became a CHAIN (increment 2b): this frame consumes the head and
// hands the tail to the element, exactly as an array's dims chain already did.
//
// Go's OWN five spellings are the rows, because the rendering rule is Go's and not ours: Go
// parenthesises the element only when its rendering begins with `<` -- a RECEIVE element, since
// `chan <-chan int` would re-parse as `chan<- chan int`. A SEND element is unambiguous and prints
// bare. A parenthesise-ANY rule passes rows 1-3 and fails row 4; a no-chain rule passes rows 1
// and 5 and fails 2-4. Both control arms are named here so neither can be "fixed" into the other.
package main

import (
	"fmt"
	"reflect"
)

func main() {
	intT := reflect.TypeOf(0)

	// CONSTRUCTED route -- ChanOf prepends its own direction to the element's chain.
	both := reflect.ChanOf(reflect.BothDir, intT)
	recv := reflect.ChanOf(reflect.RecvDir, intT)
	send := reflect.ChanOf(reflect.SendDir, intT)

	// Go's five. Row 3 is the only one that parenthesises.
	fmt.Println("1 chan chan int    :", reflect.ChanOf(reflect.BothDir, both).String())
	fmt.Println("2 chan<- chan int  :", reflect.ChanOf(reflect.SendDir, both).String())
	fmt.Println("3 chan (<-chan int):", reflect.ChanOf(reflect.BothDir, recv).String())
	fmt.Println("4 chan chan<- int  :", reflect.ChanOf(reflect.BothDir, send).String())
	fmt.Println("5 <-chan chan int  :", reflect.ChanOf(reflect.RecvDir, both).String())
	// Rows 6 and 7 are reflect's own typeTests spellings that a DIRECTIONAL head puts over a receive
	// element. They exist because the parenthesisation rule's first cut keyed on the element alone
	// (wrap when it begins with `<`) and passed rows 1-5, then rendered `chan<- (<-chan int)` for a
	// struct field. Go wraps ONLY under the bare bidirectional `chan`; under `chan<-` or `<-chan` the
	// arrow is already bound and the element prints bare. Both routes now pin it.
	fmt.Println("6 chan<- <-chan int:", reflect.ChanOf(reflect.SendDir, recv).String())
	fmt.Println("7 <-chan <-chan int:", reflect.ChanOf(reflect.RecvDir, recv).String())

	// Elem() must hand the TAIL down, or a nested direction is unreachable from the type side.
	fmt.Println("elem of chan (<-chan int):", reflect.ChanOf(reflect.BothDir, recv).Elem().String())
	fmt.Println("elem dir                 :", reflect.ChanOf(reflect.BothDir, recv).Elem().ChanDir())

	// The IDENTITY control the trailing trim exists for: an all-Both chain must normalize to
	// absent, or ChanOf(BothDir, T) and a value-derived `chan T` key differently and split.
	var nilChan chan int
	fmt.Println("identity ChanOf(Both,int) == TypeOf((chan int)(nil)):",
		reflect.ChanOf(reflect.BothDir, intT) == reflect.TypeOf(nilChan))

	// And one level up: `chan chan int` is all-Both too, so it must ALSO collapse to the
	// value-derived type rather than keying on an explicit [Both, Both].
	var nilNested chan chan int
	fmt.Println("identity ChanOf(Both,chan int) == TypeOf((chan chan int)(nil)):",
		reflect.ChanOf(reflect.BothDir, both) == reflect.TypeOf(nilNested))

	// VALUE route (increment D). A nested direction is not measurable from a channel value (its
	// element is not a present value), so the chain rides the VALUE from the position that knows it
	// statically: a zero var, a make, a nil conversion, a struct field, the pointee of new. These are
	// reflect's own consumers -- TestChanOf's arrow-association rows and typeTests' field rows -- and
	// the identity rows are the point: the value-derived type must BE the constructed one.
	var vLeft chan<- chan int
	var vRight chan (<-chan int)
	fmt.Println("value zero-var chan<- chan int   :", reflect.TypeOf(vLeft).String())
	fmt.Println("value zero-var chan (<-chan int) :", reflect.TypeOf(vRight).String())
	fmt.Println("value == constructed, left       :", reflect.TypeOf(vLeft) == reflect.ChanOf(reflect.SendDir, both))
	fmt.Println("value == constructed, right      :", reflect.TypeOf(vRight) == reflect.ChanOf(reflect.BothDir, recv))
	mk := make(chan chan<- int)
	fmt.Println("value make chan chan<- int       :", reflect.TypeOf(mk).String())
	nc := (chan (<-chan int))(nil)
	fmt.Println("value nil-conv chan (<-chan int) :", reflect.TypeOf(nc).String())
	fmt.Println("value field chan<- <-chan int    :", reflect.TypeOf(struct{ x chan<- <-chan int }{}).Field(0).Type.String())
	p := new(chan (<-chan int))
	fmt.Println("value new(chan (<-chan int)).Elem:", reflect.TypeOf(p).Elem().String())

	// The other half of the same cargo: a channel's ELEMENT ARRAY LENGTH, the ChanElemDims row, on the
	// value route -- and both halves at once on one value.
	ca := make(chan [3]int)
	fmt.Println("value make chan [3]int           :", reflect.TypeOf(ca).String(), reflect.TypeOf(ca).Elem().Len())
	var cb chan<- [2][4]byte
	fmt.Println("value zero-var chan<- [2][4]byte :", reflect.TypeOf(cb).String(), reflect.TypeOf(cb).Elem().Len(), reflect.TypeOf(cb).Elem().Elem().Len())
	na := (chan [5]int)(nil)
	fmt.Println("value nil-conv chan [5]int       :", reflect.TypeOf(na).String(), reflect.TypeOf(na).Elem().Len())

	// abi.Elem() rows -- the measured defect behind moving abi.Elem to KindCarriesElementCargo.
	// reflect's AssignableTo/ConvertibleTo walk haveIdenticalUnderlyingType, whose channel arm compares
	// abi.Elem(T) against abi.Elem(V); abi.Elem named pointer and map alone as the kinds whose dims pass
	// unshifted, so a CHANNEL's element dims were shifted off there and `chan [3]int` and `chan [4]int`
	// compared their elements as two dimension-less arrays: identical, hence assignable. Go says false.
	// The positive control is the fix reverted: BOTH [3]->[4] rows flip to true -- assignable AND
	// convertible, because ConvertibleTo walks the same channel arm -- and the [3]->[3] row does not
	// move. (The first prediction named one row; the control was sharper than the prediction.)
	c3 := reflect.TypeOf(make(chan [3]int))
	fmt.Println("assignable chan [3]int -> chan [4]int:", c3.AssignableTo(reflect.ChanOf(reflect.BothDir, reflect.ArrayOf(4, intT))))
	fmt.Println("assignable chan [3]int -> chan [3]int:", c3.AssignableTo(reflect.ChanOf(reflect.BothDir, reflect.ArrayOf(3, intT))))
	fmt.Println("convertible chan [3]int -> chan [4]int:", c3.ConvertibleTo(reflect.ChanOf(reflect.BothDir, reflect.ArrayOf(4, intT))))
}
