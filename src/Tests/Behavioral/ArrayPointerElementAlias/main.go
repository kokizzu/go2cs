// Regression test: Go's element-pointer-to-array reinterpret `(*[N]T)(unsafe.Pointer(p))`, where
// `p` is a `*T`, must ALIAS the storage `p` is an element of — a write through the array pointer
// has to land in the caller's buffer.
//
// This is the POINTER-sourced sibling of SliceToArrayPointerAlias's `(*[N]T)(s)`. go2cs routed it
// through the raw-address reinterpret, which cannot express it: `(ж<array<T>>)(uintptr)(p)` derefs
// an `array<T>` STRUCT — a backing-store reference plus bounds — out of the pointed-at DATA, and
// where the `[:n]` fusion caught it first the result was a `slice<T>` COPY of the memory, whose
// writes went nowhere.
//
// The corpus witness is os's TestReadStdin (462 subtests). Its poll.ReadConsole fake fills
// internal/poll's read buffer with
//
//	n = copy((*[10000]uint16)(unsafe.Pointer(buf))[:n:n], s16)
//
// so against a copy every read returned zeros ("have [0 0 0…] want [abc…]"). syscall.Readlink's
// reparse-buffer decode carries the same shape over `&data.PathBuffer[0]`.
//
// Two properties matter and both are exercised below: the window must start at the POINTED-AT
// element (not at element 0 of the backing store), and Go's `N` is an upper bound in this idiom,
// not a real extent — it is spelled 10000 / 1<<16 / 0xffff over buffers a few elements long, and
// the result is always immediately re-sliced to the real count.
package main

import (
	"fmt"
	"unsafe"
)

// readInto is the TestReadStdin shape: a *uint16 into a caller-owned buffer, reinterpreted as a
// huge array pointer, re-sliced to the real count, and written through.
func readInto(buf *uint16, toread int, src []uint16) int {
	return copy((*[10000]uint16)(unsafe.Pointer(buf))[:toread:toread], src)
}

// bumpAll walks a byte buffer through a 4-element array pointer taken at an offset element.
func bumpAll(p *byte) {
	a := (*[4]byte)(unsafe.Pointer(p))
	for i := range a {
		a[i]++
	}
}

func main() {
	// 1. Write-through at element 0 — the copy lands in the caller's buffer.
	dst := make([]uint16, 8)
	n := readInto(&dst[0], 4, []uint16{1, 2, 3, 4})
	fmt.Println("copied:", n, "dst:", dst)

	// 2. Write-through at an OFFSET element — the window starts there, not at the backing's start.
	n = readInto(&dst[4], 3, []uint16{7, 8, 9})
	fmt.Println("copied:", n, "dst:", dst)

	// 3. Direct indexed write through the array pointer.
	p := (*[4]uint16)(unsafe.Pointer(&dst[2]))
	p[0] = 0x55
	p[3] = 0x66
	fmt.Println("indexed write:", dst)

	// 4. Reads through the pointer see writes made through the SLICE (the other direction).
	dst[3] = 0xab
	fmt.Println("read back:", p[0], p[1], p[2], p[3])

	// 5. Dereferencing yields an array VALUE; mutating that copy is not a write-through.
	deref := *p
	deref[1] = 0x00
	fmt.Println("deref copy:", deref, "still through pointer:", p[1])

	// 6. A full slice of the pointed-to array aliases the same storage.
	view := p[:]
	view[2] = 0x7f
	fmt.Println("p[:] aliases:", dst[4], "len/cap:", len(view), cap(view))

	// 7. An EXPLICIT-BOUNDS slice of the window is relative to the ARRAY, not to the backing.
	inner := p[1:3]
	inner[0] = 0x21
	fmt.Println("p[1:3] aliases:", dst[3], "len/cap:", len(inner), cap(inner))

	// 8. Pointer identity: the array's elements ARE the slice's elements.
	fmt.Println("element identity:", &p[0] == &dst[2], "windowed identity:", &p[2] == &dst[4])

	// 9. len of the array pointer is Go's N.
	fmt.Println("array length:", len(p), len(*p))

	// 10. A pointer into a Go FIXED ARRAY, taken at an offset element.
	var raw [8]byte
	for i := range raw {
		raw[i] = byte(i)
	}
	bumpAll(&raw[3])
	fmt.Println("fixed array:", raw)

	// 11. The same shape over a re-sliced view: the absolute element is what the window addresses.
	buf := []byte{0, 1, 2, 3, 4, 5, 6, 7}
	tail := buf[4:]
	bumpAll(&tail[0])
	fmt.Println("resliced view:", buf)
}
