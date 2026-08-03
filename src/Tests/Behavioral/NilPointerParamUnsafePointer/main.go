// Guards the rule that a pointer PARAMETER a Go body never dereferences is never dereferenced at
// function ENTRY either — so passing a legitimately nil pointer cannot panic before the body runs.
//
// A pointer parameter is emitted as the box `ж<T> Ꮡp` plus a deref'd VALUE alias
// (`ref var p = ref Ꮡp.Value`), and that alias is dropped when the converted body never names the
// value. Two independent mechanisms can wrongly keep it alive; each has its own group below.
//
// 1. `unsafe.Pointer(p)` RENDERING. Taking the pointer's address through the alias
// (`@unsafe.Pointer.FromRef(ref p)`) forces the alias to be materialized, so the entry-time deref
// throws a nil-pointer panic for a nil argument — even though Go never touches the pointee and
// `uintptr(unsafe.Pointer(nil))` is simply 0. Rendering the BOX instead
// (`new @unsafe.Pointer(Ꮡp)`) is exact, nil-safe, and additionally leaves the value alias unused so
// it is dropped as dead.
//
// This is the syscall wrapper shape: DuplicateHandle's `lpTargetHandle *Handle` is passed nil (with
// DUPLICATE_CLOSE_SOURCE) to close a handle without receiving a duplicate, and
// syscall.StartProcess does exactly that in a deferred call. Before the fix, spawning any child
// process panicked there. Pointer parameters whose pointee is a basic or struct type already took
// the box form; a NAMED-numeric pointee (`*Handle`) and a pointer-to-pointer (`**uint16`) did not.
//
// 2. The alias-liveness SCAN. It is a whole-word text match over the converted body, and a Go
// composite literal's field key renders as a C# NAMED ARGUMENT (`parent: Ꮡparent`). Go's own idiom
// names the field after the value it is initialized from, so the label alone matched and kept the
// alias alive; the dead alias then dereferenced the box at entry. internal/concurrent's
// `newIndirectNode(parent *indirect)` has exactly this shape and is called with a nil parent for the
// root node, so `NewHashTrieMap` threw inside `unique`'s package initializer and took `net/netip` —
// and every dependent, encoding/gob's TestNetIP among them — down with it.
package main

import (
	"fmt"
	"unsafe"
)

// Handle is a named type over uintptr, matching syscall.Handle — the pointee shape that used to
// take the deref-alias route.
type Handle uintptr

// addrOfNamed reports the address of a *Handle without ever dereferencing it. A nil argument must
// yield 0, not panic.
//
//go:noinline
func addrOfNamed(h *Handle) uintptr {
	return uintptr(unsafe.Pointer(h))
}

// addrOfPtrPtr is the pointer-to-pointer sibling (syscall's `stringSid **uint16`), which took the
// same deref-alias route.
//
//go:noinline
func addrOfPtrPtr(pp **uint16) uintptr {
	return uintptr(unsafe.Pointer(pp))
}

// addrOfBasic is the control: a basic pointee already used the box form, so its behavior must be
// unchanged.
//
//go:noinline
func addrOfBasic(b *uint16) uintptr {
	return uintptr(unsafe.Pointer(b))
}

// liveAlias keeps the deref'd VALUE genuinely live alongside the unsafe.Pointer use, so the value
// alias must still be emitted and must still observe writes through the pointer. Guards against the
// box rendering wrongly dropping an alias that is actually read.
//
//go:noinline
func liveAlias(h *Handle) (uintptr, Handle) {
	*h = *h + 7
	return uintptr(unsafe.Pointer(h)), *h
}

// node is the composite-literal shape of mechanism 2: the field key and the value it is
// initialized from spell the same name, so the emitted `parent: Ꮡparent` named argument is the
// only occurrence of `parent` in the converted body.
type node struct {
	id     int
	parent *node
}

// newNode never dereferences parent, so its value alias is dead and must not be emitted — a nil
// parent (the root node) must not fault at entry.
//
//go:noinline
func newNode(id int, parent *node) *node {
	return &node{id: id, parent: parent}
}

// depth is the positive control for mechanism 2: it DOES dereference its pointer parameter, so the
// value alias must still be emitted (dropping it would be CS0103) and must still read real storage.
//
//go:noinline
func depth(n *node) int {
	d := 0
	for p := n.parent; p != nil; p = p.parent {
		d++
	}
	return d
}

func main() {
	// A nil pointer parameter converts to the 0 address rather than panicking.
	fmt.Println("named nil  :", addrOfNamed(nil))
	fmt.Println("ptrptr nil :", addrOfPtrPtr(nil))
	fmt.Println("basic nil  :", addrOfBasic(nil))

	// A non-nil pointer yields a non-zero address. The address VALUE is not stable across
	// runtimes, so only its non-nilness is compared.
	var h Handle = 3
	fmt.Println("named nonnil nonzero :", addrOfNamed(&h) != 0)

	var u uint16 = 9
	pu := &u
	fmt.Println("ptrptr nonnil nonzero:", addrOfPtrPtr(&pu) != 0)
	fmt.Println("basic nonnil nonzero :", addrOfBasic(&u) != 0)

	// The value alias stays live and the write through the pointer is observed.
	addr, val := liveAlias(&h)
	fmt.Println("live alias nonzero:", addr != 0, "value:", val, "readback:", h)

	// A composite-literal field key named after its value must not keep the alias alive: the
	// root node is built with a nil parent.
	root := newNode(1, nil)
	child := newNode(2, root)
	grand := newNode(3, child)
	fmt.Println("named-arg label:", root.id, child.id, grand.id)
	fmt.Println("depths         :", depth(root), depth(child), depth(grand))
	fmt.Println("links          :", root.parent == nil, child.parent == root, grand.parent == child)
}
