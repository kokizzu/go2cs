package main

import (
	"fmt"
	"unsafe"
)

// The two arms of the pointer-word read `*(*unsafe.Pointer)(unsafe.Pointer(&x))`:
//
//   - a uintptr source's word IS the number, and must round-trip exactly;
//   - a reference-kinded source (a channel here) yields a NON-NIL word for a live value —
//     the nil-bit is the contract time.syncTimer's consumer reads.
//
// The un-fixed emission punned the source's first reference-sized bits into an
// unsafe.Pointer REFERENCE: junk dispatch on a quiet heap, an AccessViolationException
// when the bits landed unmapped (the asynctimerchan=2 witness, 2026-08-24).
func main() {
	u := uintptr(0xC0FFEE)
	q := *(*unsafe.Pointer)(unsafe.Pointer(&u))
	fmt.Println("uintptr word round-trips:", uintptr(q) == 0xC0FFEE)

	c := make(chan int, 1)
	p := *(*unsafe.Pointer)(unsafe.Pointer(&c))
	fmt.Println("live channel word is non-nil:", p != nil)

	// The word is STABLE for the same storage: two reads agree.
	p2 := *(*unsafe.Pointer)(unsafe.Pointer(&c))
	fmt.Println("re-read agrees:", uintptr(p) == uintptr(p2))
}
