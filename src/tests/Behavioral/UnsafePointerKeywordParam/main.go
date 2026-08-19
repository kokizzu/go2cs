// Regression test: an `unsafe.Pointer` parameter whose name is a C# keyword.
//
// An `unsafe.Pointer` identifier is emitted through several different arms depending on what
// the surrounding expression wants — the box itself, a `(uintptr)` cast, a `ж<T>` deref, a
// plain argument — and each builds its text from the Go name. A parameter named `new` (a
// reserved C# keyword) must be escaped to `@new` by every one of them, or the output parses as
// the `new` operator (CS1526). This is exactly how internal/runtime/atomic's
// `CompareAndSwap(old, new unsafe.Pointer)` failed to compile.
//
// The original trigger was the comparison operand's box-value deref form `name.Value`.
// Comparisons no longer take that form — two unsafe.Pointers compare as BOXES, since golib's
// `Pointer` overrides Equals to compare the address and is nil-safe where `.Value` throws — so
// the shapes below cover the arms that remain, and the comparison arm stays as the control
// that it renders `@new` rather than `new`.
package main

import (
	"fmt"
	"unsafe"
)

type holder struct{ p unsafe.Pointer }

// `new` is a C# keyword and a valid Go identifier. Each function below reaches a different
// emission arm with it.
func sameAs(old, new unsafe.Pointer) bool { return new == old } // box comparison

func notNil(new unsafe.Pointer) bool { return new != nil } // the nil arm

func asUintptr(new unsafe.Pointer) uintptr { return uintptr(new) } // (uintptr) cast

func deref(new unsafe.Pointer) int32 { return *(*int32)(new) } // ж<T> deref

func store(h *holder, new unsafe.Pointer) { h.p = new } // plain assignment

func offset(new unsafe.Pointer, d uintptr) unsafe.Pointer { // cast, arithmetic, cast back
	return unsafe.Pointer(uintptr(new) + d)
}

func pass(new unsafe.Pointer) uintptr { return asUintptr(new) } // call argument

func main() {
	var x int32 = 7
	a, b := 1, 2
	pa := unsafe.Pointer(&a)
	pb := unsafe.Pointer(&b)
	px := unsafe.Pointer(&x)

	fmt.Println(sameAs(pa, pa)) // true
	fmt.Println(sameAs(pa, pb)) // false
	fmt.Println(notNil(pa))     // true
	fmt.Println(notNil(nil))    // false

	fmt.Println(asUintptr(px) != 0) // true
	fmt.Println(deref(px))          // 7

	var h holder
	store(&h, px)
	fmt.Println(h.p == px)           // true
	fmt.Println(offset(px, 0) == px) // true — zero offset round-trips to the same address
	fmt.Println(pass(px) == asUintptr(px))
}
