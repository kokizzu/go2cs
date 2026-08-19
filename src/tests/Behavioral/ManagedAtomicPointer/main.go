package main

import (
	"fmt"
	"sync/atomic"
	"unsafe"
)

type proc struct{ addr int }

type lazyProc struct {
	p *proc
}

// find mirrors x/sys/windows's LazyProc.Find: a lock-free managed-pointer-field cache.
func (l *lazyProc) find() bool {
	if atomic.LoadPointer((*unsafe.Pointer)(unsafe.Pointer(&l.p))) == nil {
		atomic.StorePointer((*unsafe.Pointer)(unsafe.Pointer(&l.p)), unsafe.Pointer(&proc{addr: 42}))
		return true
	}
	return false
}

// identity exercises Go's rule that two unsafe.Pointers compare by ADDRESS. golib's
// `Pointer : ж<uintptr>` already answers that through its overridden Equals, but the operands
// have to reach the operator as BOXES: rendering each side as `x.Value` (the raw uintptr) is
// right by accident for two non-nil pointers and throws on a nil one, because a nil
// unsafe.Pointer local is a C# null reference. sync/atomic's TestLoadPointer /
// TestStorePointer / TestSwapPointer walk a table whose first element is nil and died there.
func identity() {
	var a, b int32
	pa := unsafe.Pointer(&a)
	pb := unsafe.Pointer(&b)
	var pn unsafe.Pointer

	// Same address, different addresses, and the nil operand on each side.
	fmt.Println(pa == pa, pa == pb, pa != pb)
	fmt.Println(pn == pn, pn == pa, pa != pn)

	// A fresh conversion of the SAME address is a distinct value in C# and one pointer in Go.
	fmt.Println(unsafe.Pointer(&a) == pa)

	// The table walk itself, nil first — sync/atomic's testPointers() shape.
	table := []unsafe.Pointer{nil, pa, pb}
	same, diff := 0, 0
	for _, p := range table {
		k := p
		if k == p {
			same++
		}
		if k != table[0] {
			diff++
		}
	}
	fmt.Println(same, diff)

	// Through a struct field, so the selector operand pairs with an ident operand.
	var l lazyProc
	fp := unsafe.Pointer(&l.p)
	fmt.Println(fp == unsafe.Pointer(&l.p), fp == pa)
}

func main() {
	var l lazyProc
	fmt.Println(l.find()) // true — field was nil
	fmt.Println(l.find()) // false — already set
	fmt.Println(l.p.addr) // 42 — the stored pointer is usable
	identity()
}
