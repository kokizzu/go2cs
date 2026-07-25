package main

import (
	"fmt"
	"unsafe"
)

// This test locks in the two contracts a pointer-type reinterpret between two MANAGED-referent
// pointees must honor -- `(*view)(unsafe.Pointer(h))` where both `header` and `view` are ordinary
// Go structs (as opposed to the raw-address/native-buffer reinterprets guarded by
// UnsafePointerReinterpret, whose pointees are memory the managed runtime does not own):
//
//  1. ALIASING -- the derived pointer addresses the SAME storage, so a write through it is
//     observed through the original pointer. (A reinterpret that boxed a COPY fails here.)
//  2. LIFETIME -- the derived pointer KEEPS THE POINTEE ALIVE and stays valid across garbage
//     collection, exactly as Go's does. Go's GC tracks a *T obtained through unsafe.Pointer as a
//     real reference; a derived pointer that held only a raw ADDRESS would not, and after enough
//     allocation churn the address is recycled and reads return whatever now occupies it.
//
// Contract 2 is what `reflect.toRType` -- `(*rtype)(unsafe.Pointer(t))`, whose result reflect
// caches for process lifetime in canonType -- depends on. When it was violated, TypeOf(x).Kind()
// began reading recycled memory mid-process and fmt.Sprint's "space only between two non-strings"
// rule silently inverted corpus-wide (go/doc/comment TestWrap: 9498/10000 subtest names diverged).
// See docs/Phase4/FINDING-managed-box-uintptr-lifetime.md.

type header struct {
	kind int32
	size uint32
	name string
}

type view struct {
	kind int32
	size uint32
	name string
}

//go:noinline
func asView(h *header) *view {
	return (*view)(unsafe.Pointer(h))
}

// churn allocates enough short-lived garbage to force several collections and to REUSE the
// memory a dead object occupied -- a forced collection alone does not reproduce the failure,
// the address has to actually be handed to another allocation.
//
//go:noinline
func churn() {
	keep := make([][]byte, 64)
	for i := 0; i < 400000; i++ {
		keep[i&63] = make([]byte, 24)
	}
}

func main() {
	// 1. Aliasing: write through the reinterpret, read through the original.
	h := &header{kind: 24, size: 16, name: "string"}
	v := asView(h)
	v.kind = 25
	fmt.Println("alias:", h.kind == 25, v.size == 16, v.name == "string")

	// 2. Lifetime: the reinterpreted pointer is the ONLY reference to the pointee that survives
	// asView -- the source pointer is a temporary. Go keeps it alive; so must the conversion.
	retained := asView(&header{kind: 24, size: 16, name: "retained"})
	churn()
	fmt.Println("lifetime:", retained.kind == 24, retained.size == 16, retained.name == "retained")
}
