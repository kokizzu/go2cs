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
}
