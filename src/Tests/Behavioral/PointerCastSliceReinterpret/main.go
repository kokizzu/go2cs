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
}
