package main

import (
	"fmt"
	"unsafe"
)

// This test locks in the LIFETIME contract of the reinterpret that CANNOT alias managed storage --
// the address-route fallback, as opposed to the aliasing route ReinterpretPointerLifetime guards.
//
// `(*hdr)(unsafe.Pointer(&b[0]))` is a perfectly ordinary Go reinterpret: a []byte scratch buffer
// addressed as the fixed-layout record it holds. It is NOT representable as a managed alias --
// `hdr` is eight bytes and the source pointee is one, so a C# `Unsafe.As` would read seven bytes
// past the element -- so golib falls back to handing out the source's raw ADDRESS. That address is
// interior to a managed array, and it was pinned only for the duration of the statement that took
// it. Every write through the derived pointer afterward lands wherever that memory now is:
// silently in the wrong place once a collection has moved the array, and in memory another object
// now owns once it has been recycled. Both the write and the read below fail without a pin whose
// lifetime is the DERIVED POINTER's, not the address-taking statement's.
//
// The damage is not confined to this buffer: the stray writes land in whatever object now owns
// the recycled memory, so they are silent here and surface later, somewhere unrelated.
// os_windows_test.go's createMountPoint has exactly this shape -- four uint16 field stores through
// a windows.MountPointReparseBuffer laid over a []byte scratch buffer -- and was the suspected
// source of an ExecutionEngineException whose crash site MOVED between `os` runs. That particular
// attribution did NOT survive a pre-fix control run (see
// docs/Phase4/BOARD-next-validation-candidates.md, the `os` section); the defect below is real and
// deterministic either way, which is the whole reason it is guarded here rather than there.

type hdr struct {
	A uint16
	B uint16
	C uint16
	D uint16
}

//go:noinline
func asHdr(b []byte) *hdr {
	return (*hdr)(unsafe.Pointer(&b[0]))
}

// churn allocates enough short-lived garbage to force several collections and to REUSE the memory
// a dead object occupied -- a forced collection alone does not reproduce the failure, the source
// buffer has to actually be moved and its old address handed to another allocation.
//
//go:noinline
func churn() {
	keep := make([][]byte, 64)
	for i := 0; i < 400000; i++ {
		keep[i&63] = make([]byte, 24)
	}
}

func main() {
	b := make([]byte, 8)
	h := asHdr(b)

	// Written BEFORE the churn, read back through the derived pointer AFTER it: the pointee must
	// still be the same storage, not whatever now occupies the address it once had.
	h.A = 0x0201
	churn()
	fmt.Println("read:", h.A == 0x0201, b[0] == 1, b[1] == 2)

	// Written AFTER the churn, read back through the ORIGINAL slice: the derived pointer must
	// still address the buffer's real storage.
	h.B = 0x0403
	h.C = 0x0605
	h.D = 0x0807
	fmt.Println("write:", b[2] == 3, b[3] == 4, b[4] == 5, b[5] == 6, b[6] == 7, b[7] == 8)

	// A second cycle, with the derived pointer as the ONLY surviving reference to the buffer over
	// the collection: Go's collector tracks a pointer obtained through unsafe.Pointer as a real
	// reference, so the storage must be kept alive as well as kept still.
	r := asHdr(make([]byte, 8))
	r.A = 0x0a09
	r.D = 0x100f
	churn()
	fmt.Println("retained:", r.A == 0x0a09, r.B == 0, r.C == 0, r.D == 0x100f)
}
