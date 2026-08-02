// UnsafeStringEmpty guards zero-length string construction through unsafe.String, and the Windows
// idiom that reaches it: syscall.UTF16ToString over a fixed WCHAR buffer that is entirely NUL.
//
// UTF16ToString truncates its argument at the first NUL, decodes that prefix into a byte slice, and
// returns unsafe.String(unsafe.SliceData(buf), len(buf)). For an all-NUL buffer the prefix is empty,
// so buf is a NON-nil slice of capacity zero -- and SliceData documents exactly that case as a
// non-nil pointer to an unspecified address. The converted unsafe.String dereferenced that pointer
// in order to pin it, which for a zero-length managed backing array is element 0 of nothing:
// IndexOutOfRangeException where Go returns "". Go reads no bytes at all for a zero length, and
// neither does the conversion now.
//
// The idiom is not exotic. Every unset WCHAR[N] field of a Win32 record decodes this way --
// WIN32_FIND_DATAW.cAlternateFileName on a volume with 8.3 name generation disabled,
// MIB_IFROW.wszName, PROCESSENTRY32.szExeFile, STARTUPINFO.lpDesktop.
package main

import (
	"fmt"
	"syscall"
	"unsafe"
)

func main() {
	utf16()
	unsafeStrings()
}

// utf16 covers the motivating path. The all-NUL buffers are the regression itself; the terminated
// and unterminated buffers pin that truncation still happens where a NUL actually is, so nothing
// can pass this by returning "" unconditionally.
func utf16() {
	fmt.Println("-- UTF16ToString --")

	var alt [14]uint16   // WIN32_FIND_DATAW.cAlternateFileName, never written by the kernel
	var name [260]uint16 // WIN32_FIND_DATAW.cFileName, not yet written here

	show("all-NUL [14]", syscall.UTF16ToString(alt[:]))
	show("all-NUL [260]", syscall.UTF16ToString(name[:]))

	// A NUL-terminated prefix inside a larger buffer: name is still zeroed past the copy, so the
	// unit right after the text is the terminator and the rest of the buffer is ignored.
	copy(name[:], encode("README.TXT"))
	show("terminated", syscall.UTF16ToString(name[:]))

	// A buffer with no terminator at all decodes to its full length.
	show("unterminated", syscall.UTF16ToString(encode("NoTerminator")))

	// A NUL in the very first slot terminates immediately, whatever follows it.
	poisoned := [4]uint16{0, 'x', 'y', 'z'}
	show("leading NUL", syscall.UTF16ToString(poisoned[:]))

	show("empty slice", syscall.UTF16ToString([]uint16{}))
	show("nil slice", syscall.UTF16ToString(nil))
}

func unsafeStrings() {
	fmt.Println("-- unsafe.String --")

	// The direct control for the defect: a non-nil pointer with length 0, over a slice whose
	// capacity is zero, so there is no element 0 to reach.
	empty := make([]byte, 0)
	show("make len 0", unsafe.String(unsafe.SliceData(empty), 0))
	show("literal {}", unsafe.String(unsafe.SliceData([]byte{}), 0))

	// Zero length again, but over real storage -- this path always worked and has to keep working.
	show("make cap 8", unsafe.String(unsafe.SliceData(make([]byte, 0, 8)), 0))

	body := []byte("hello")
	show("resliced [:0]", unsafe.String(unsafe.SliceData(body[:0]), 0))

	// A nil pointer with length 0 is the case the conversion already handled.
	var nilSlice []byte
	show("nil slice", unsafe.String(unsafe.SliceData(nilSlice), 0))
	show("empty string", unsafe.String(unsafe.StringData(""), 0))

	// Non-zero lengths: a guard on the empty case must not swallow the ordinary one.
	show("full", unsafe.String(unsafe.SliceData(body), len(body)))
	show("prefix", unsafe.String(unsafe.SliceData(body), 3))

	// unsafe.Slice over the same zero-capacity pointer. Only the count is compared: Go leaves the
	// address unspecified, so nothing else about the result is portable.
	fmt.Println("zero-length Slice len =", len(unsafe.Slice(unsafe.SliceData(empty), 0)))
}

// encode renders s as UTF-16 with NO terminating NUL; syscall.UTF16FromString always appends one.
// Every input here is ASCII, so a unit per rune is exact.
func encode(s string) []uint16 {
	out := make([]uint16, 0, len(s))
	for _, r := range s {
		out = append(out, uint16(r))
	}
	return out
}

func show(what, got string) {
	fmt.Printf("%-14s %q len=%d\n", what, got, len(got))
}
