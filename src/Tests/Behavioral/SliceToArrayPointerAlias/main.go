// Regression test: Go's slice-to-array-POINTER conversion `(*[N]T)(s)` must ALIAS the slice's
// storage, while the slice-to-array VALUE conversion `[N]T(s)` must COPY it.
//
// The Go spec (Conversions from slice to array or array pointer) says of the pointer form that
// "the slice and array share their underlying array"; the value form yields an ordinary array
// value. go2cs emitted BOTH through golib's copying `array<T>(slice<T>, nint)` constructor, so
// every write through the pointer form was discarded silently — a valid-but-wrong-answer defect,
// not a compile error.
//
// The corpus witness was image/png's encoder. Its cbTCA8 row loop walks the destination row in
// 4-byte windows,
//
//	d := (*[4]byte)(dst)
//	s := (*[4]byte)(src)
//	... d[0] = ...; d[1] = ...; d[2] = ...; d[3] = s[3]
//
// writing each un-premultiplied pixel through `d`. Against a copy every pixel write went nowhere,
// so any RGBA image that was not fully opaque encoded as an all-zero image (TestWriteRGBA's
// "50/50 Transparent/Opaque RGBA" and "RGBA with variable alpha" subtests).
//
// The window matters as much as the aliasing: the png loop converts `cr[0][1:]` and then re-slices
// it forward, so the pointer's first element is NOT element 0 of the backing store.
package main

import "fmt"

// writeThrough is the png shape: convert a 4-byte window of an offset slice and write through it.
func writeThrough(dst []byte) {
	d := (*[4]byte)(dst)
	d[0] = 0x11
	d[1] = 0x22
	d[2] = 0x33
	d[3] = 0x44
}

// valueCopy takes the VALUE conversion and mutates it — the original must be untouched.
func valueCopy(src []byte) [4]byte {
	a := [4]byte(src)
	a[0] = 0xff
	return a
}

// premultiplyLike walks a buffer in 4-byte windows exactly as png's encoder does.
func premultiplyLike(dst, src []byte) {
	for ; len(src) >= 4; dst, src = dst[4:], src[4:] {
		d := (*[4]byte)(dst)
		s := (*[4]byte)(src)
		if s[3] == 0x00 {
			d[0], d[1], d[2], d[3] = 0, 0, 0, 0
		} else if s[3] == 0xff {
			copy(d[:], s[:])
		} else {
			d[0] = s[0] / 2
			d[1] = s[1] / 2
			d[2] = s[2] / 2
			d[3] = s[3]
		}
	}
}

func main() {
	// 1. Aliasing at a NON-ZERO offset — the png case: the write must land in the backing array.
	buf := make([]byte, 9)
	buf[0] = 0x99
	writeThrough(buf[1:])
	fmt.Println("aliased write:", buf)

	// 2. Reads through the pointer see writes made through the SLICE (the other direction).
	p := (*[4]byte)(buf[1:])
	buf[2] = 0xab
	fmt.Println("read back through pointer:", p[0], p[1], p[2], p[3])

	// 3. The array VALUE conversion is a copy — mutating it leaves the slice alone.
	got := valueCopy(buf[1:])
	fmt.Println("value conversion:", got, "slice unchanged:", buf[1:5])

	// 4. Dereferencing the pointer yields an array VALUE; mutating that copy is not a write-through.
	deref := *p
	deref[1] = 0x00
	fmt.Println("deref copy:", deref, "still through pointer:", p[1])

	// 5. A slice of the pointed-to array aliases the same storage.
	view := p[:]
	view[3] = 0x7f
	fmt.Println("slice of array pointer:", buf[4], "len/cap:", len(view), cap(view))

	// 6. Pointer identity: the array's first element IS the slice's element.
	fmt.Println("element identity:", &p[0] == &buf[1], "windowed identity:", &p[2] == &buf[3])

	// 7. The png-shaped windowed loop over a whole buffer.
	src := []byte{
		0x10, 0x20, 0x30, 0x00,
		0x40, 0x50, 0x60, 0xff,
		0x80, 0x90, 0xa0, 0x40,
	}
	dst := make([]byte, len(src))
	premultiplyLike(dst, src)
	fmt.Println("windowed loop:", dst)

	// 8. len/cap of an aliasing array pointer report the ARRAY's length, not the slice's.
	fmt.Println("array length:", len(p), len(*p))
}
