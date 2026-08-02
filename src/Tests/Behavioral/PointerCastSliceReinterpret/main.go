package main

import (
	"fmt"
	"unsafe"
)

// Guards the NON-IDENTITY pointer-cast slice: `(*[N]U)(unsafe.Pointer(&b[0]))[:n:n]` where the target
// element type DIFFERS from the source pointer's, so the conversion is a genuine REINTERPRET rather
// than the identity collapse PointerCastSliceRange covers (its comment defers this shape to "the
// stdlib exercises that shape" -- which is precisely how the regression below reached the corpus with
// no behavioral test to catch it).
//
// It must lower to a golib `slice<U>` built from a `ReadOnlySpan<U>` over the pointed-to memory --
// convSliceExpr's isPointerCast fusion, which matches on the emitted `(ж<…>)(uintptr)(…)` TEXT. That
// text is therefore load-bearing: routing the inner conversion through golib's managed
// `Reinterpret<T, array<U>>` instead silently defeats the match and leaves `(~box).slice(…)` over an
// `array<U>` whose backing-store REFERENCE was punned out of the pointee's DATA. That fabricates a
// managed reference -- an AccessViolationException that kills the process, not a contained wrong read.
// A pointer-to-ARRAY target can never take golib's managed-alias arm anyway (`array<U>` is an 8-byte
// struct holding a reference, so it fails both the size and the reference gate), so intercepting it is
// a pure loss.
//
// Live consequence when it regressed: internal/syscall/windows/registry.GetStringValue --
// the read behind time.initLocalFromTZI and mime.initMimeWindows, i.e. essentially every Windows
// program that formats a local time -- returned "Windows 10 Pro" before and hard-faulted after.
// See docs/Phase4/FINDING-managed-box-uintptr-lifetime.md.
func main() {
	// registry.GetStringValue: a []byte read out of the registry, reinterpreted as UTF-16 units.
	data := []byte{0x68, 0, 0x69, 0, 0x21, 0, 0, 0}
	u := (*[1 << 20]uint16)(unsafe.Pointer(&data[0]))[: len(data)/2 : len(data)/2]
	fmt.Println(len(u), u[0], u[1], u[2], u[3])

	// registry.SetStringValue: the reverse -- a []uint16 reinterpreted as its raw bytes.
	v := []uint16{0x4241, 0x4443}
	buf := (*[1 << 20]byte)(unsafe.Pointer(&v[0]))[: len(v)*2 : len(v)*2]
	fmt.Println(len(buf), buf[0], buf[1], buf[2], buf[3])

	// A FIXED-array element address rather than a slice element (the syscall reparse-buffer shape).
	var arr [4]uint32
	arr[0], arr[1] = 0x04030201, 0x08070605
	b := (*[16]byte)(unsafe.Pointer(&arr[0]))[:8]
	fmt.Println(len(b), b[0], b[3], b[4], b[7])

	// A NON-ZERO low bound. The result must start at element lo and run hi-lo, not 0..hi: this is
	// internal/syscall/windows's (*symbolicLinkReparseBuffer).path(), which slices [n1:n2:n2] to skip
	// the print name and return the substitute name, and reflect's gcSlice [begin:end:end].
	//
	// This arm alone is SAME-element-type (uint16 -> uint16), so it is not a reinterpret at all: it
	// takes array<uint16>.AliasPointer -- a real window over rb's own storage -- rather than the span
	// fusion the four reinterprets above keep. Both lowerings must honor the low bound identically,
	// which is what this case pins, and the window's own low is why the array slice extension has to
	// resolve through m_low. See ConversionStrategies-Reference, "An element pointer reinterpreted as
	// an array pointer ALIASES the element's storage", and the ArrayPointerElementAlias guard.
	var rb [8]uint16
	for i := range rb {
		rb[i] = uint16('a' + i)
	}
	n1, n2 := 2, 5
	sub := (*[64]uint16)(unsafe.Pointer(&rb[0]))[n1:n2:n2]
	fmt.Println(len(sub), sub[0], sub[1], sub[2])

	// The same, with the low bound on a byte reinterpret of a wider element (registry's
	// getValue-style offset reads).
	words := []uint32{0x04030201, 0x08070605}
	tail := (*[8]byte)(unsafe.Pointer(&words[0]))[3:7]
	fmt.Println(len(tail), tail[0], tail[1], tail[2], tail[3])
}
