// GlobalCapturedInClosure guards references to a package GLOBAL from inside a closure.
// A global is a C# static, accessed LIVE — it must never be snapshot-captured: a value
// snapshot (`var gʗ1 = g`) copies the struct (so `&gʗ1` has no box → CS0103, and writes
// through the global are lost) and is semantically wrong (Go reads/writes the live
// global). For an address-taken (heap-boxed) global, the closure references the static
// box `Ꮡmheap` directly. Mirrors runtime's `systemstack(func(){ span = mheap_.alloc() })`.
package main

import "fmt"

type heap struct {
	count int
}

func (h *heap) alloc() int { h.count++; return h.count }

var mheap heap

//go:noinline
func keep(h *heap) { _ = h } // takes &mheap so it is heap-boxed

//go:noinline
func run(f func()) { f() }

// boxedLocal is a NEGATIVE control this file happens to be the natural home for. The type declared
// above is named `heap`, which is also go2cs's own boxing intrinsic (golib's
// `heap(value, out var Ꮡname)`, in scope through `using static go.builtin`), and an address-taken
// local below needs that intrinsic in the same package. The two must NOT be treated as colliding.
//
// A local or parameter named `heap` genuinely does shadow the intrinsic, and BuiltinShadowLocal
// guards that (the emission qualifies to `builtin.heap(...)` there). A package-level TYPE does not:
// C#'s invocable-member rule ignores non-invocable type members when a simple name is the target of
// an invocation, so `heap(new heap(), out var Ꮡh)` still binds the `using static` method group. The
// first version of the shadowing check did not make that distinction and qualified here too —
// harmless output, but a golden change no defect required, which is exactly what this control now
// prevents. The emission below must stay the bare `heap(...)`.
//
//go:noinline
func boxedLocal() int {
	var h heap
	p := &h // address-taken struct local: emitted through the boxing intrinsic
	p.count += 7

	return h.count
}

func main() {
	keep(&mheap)
	var got int
	run(func() {
		got = mheap.alloc() // pointer-receiver method on the boxed global, inside the closure
		p := &mheap.count   // address of a field of the boxed global, inside the closure
		*p += 10
	})
	fmt.Println(got, mheap.count) // 1 11
	fmt.Println(boxedLocal())     // 7
}
