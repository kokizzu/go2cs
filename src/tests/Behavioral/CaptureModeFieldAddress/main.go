package main

import (
	"fmt"
	"sync/atomic"
)

// Guards the write-visibility of a CAPTURE-MODE pointer-receiver method called on a value-field
// chain rooted at a plain local or value parameter: `x.i.Add(delta)` implicitly takes &x.i, and
// the callee (whose receiver is direct-ж — its body takes &receiver.field) mutates through that
// pointer, so the local must be heap-boxed exactly as it is for the direct form `i.Add(delta)`.
// Unboxed, the emission falls to the `Ꮡ(x).of(…)` copy-box and every atomic write is dropped —
// sync/atomic's entire 43-divergence Phase-4 residual was this one shape.

type holder struct {
	before int32
	i      atomic.Int32
	after  int32
}

type ctr struct {
	n int32
}

// inc is pointer-receiver but NOT capture-mode (no &receiver.field in its body): it binds the
// `this ref` extension on the field directly and needs no box — the negative control.
func (c *ctr) inc() {
	c.n++
}

type wrap struct {
	c ctr
}

func localStruct() {
	var x holder
	v := x.i.Add(5)
	x.i.Add(2)
	fmt.Println("local:", v, x.i.Load(), x.before, x.after)
}

func valueParam(x holder) {
	x.i.Store(3)
	x.i.Add(4)
	fmt.Println("param:", x.i.Load())
}

func typeSwitchCase(v any) {
	switch t := v.(type) {
	case holder:
		t.i.Add(9) // composition: type-switch binding + capture-mode field call
		fmt.Println("switch:", t.i.Load())
	}
}

func anonStruct() {
	var x struct {
		i atomic.Int32
	}
	x.i.Store(7)
	fmt.Println("anon:", x.i.Add(1))
}

func nonCaptureControl() {
	var w wrap
	w.c.inc()
	w.c.inc()
	fmt.Println("control:", w.c.n)
}

func main() {
	localStruct()
	valueParam(holder{})
	typeSwitchCase(holder{})
	anonStruct()
	nonCaptureControl()
}
