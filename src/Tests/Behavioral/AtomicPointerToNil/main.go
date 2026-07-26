// Regression test for atomic.Pointer[T] and the POINTER-TO-NIL distinction.
//
// In Go a pointer is nil or it is not; whether the value it POINTS AT happens to be nil is a
// separate question. `&i` is a real address even when `i == nil`, and `new(any)` is a real
// address holding a nil interface — which is exactly how sync.Map's `expunged` sentinel is
// built (`expunged = new(any)`) and how it stores a nil map value (`e.p.Store(&i)` where
// `i` is a nil `any`).
//
// go2cs's core/sync/atomic holds the ж<T> box directly (a managed pointer cannot survive a GC
// move as a number), and canonicalized "the nil pointer" to a null slot so that a reference
// CompareAndSwap treats all nil pointers as equal. That canonicalization asked `ж<T>.IsNull`,
// which is true both for the nil pointer AND for a real box whose held reference-typed value is
// null — so every `Store(&i)` of a nil interface, and the `expunged` sentinel itself, collapsed
// to nil. sync.Map then lost every nil value it stored (Range skipped the entry, CompareAndSwap
// failed against it) and could not tell a deleted entry from an expunged one. The predicate is
// now the STRUCTURAL one, ж<T>.IsNilPointer.
package main

import (
	"fmt"
	"sync/atomic"
)

func main() {
	var p atomic.Pointer[any]

	// The zero value is a nil *any.
	fmt.Println("zero:", p.Load() == nil)

	// A pointer to a NIL interface is not the nil pointer, and it round-trips by identity.
	var i any
	pi := &i
	p.Store(pi)
	fmt.Println("store &nil:", p.Load() == nil, p.Load() == pi, *p.Load() == nil)

	// Storing through the pointer is visible through the loaded one (same address).
	*pi = "value"
	fmt.Println("through pointer:", *p.Load())

	// Swap hands back the previous pointer, not nil.
	old := p.Swap(nil)
	fmt.Println("swap out:", old == pi, p.Load() == nil)

	// Swapping a pointer-to-nil back in returns the nil pointer it replaced.
	var j any
	pj := &j
	old = p.Swap(pj)
	fmt.Println("swap in:", old == nil, p.Load() == pj)

	// Two distinct `new(any)` allocations are distinct non-nil pointers, both to a nil interface
	// — the sentinel shape sync.Map's `expunged` relies on.
	a := new(any)
	b := new(any)
	fmt.Println("sentinels:", a == nil, b == nil, a == b, *a == nil)

	// CompareAndSwap must distinguish the nil pointer from a pointer-to-nil, and one
	// pointer-to-nil from another.
	p.Store(a)
	fmt.Println("cas nil:", p.CompareAndSwap(nil, b))
	fmt.Println("cas wrong sentinel:", p.CompareAndSwap(b, nil))
	fmt.Println("cas right sentinel:", p.CompareAndSwap(a, b), p.Load() == b)

	// A nil old value still matches a genuinely nil slot.
	p.Store(nil)
	fmt.Println("cas from nil:", p.CompareAndSwap(nil, a), p.Load() == a)

	// The same distinction for a pointer to a nil POINTER (**T): new(*int) is non-nil.
	var q atomic.Pointer[*int]
	pp := new(*int)
	q.Store(pp)
	fmt.Println("ptr-to-nil-ptr:", q.Load() == nil, q.Load() == pp, *q.Load() == nil)
}
