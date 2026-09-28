package main

import "fmt"

// An UNSIGNED index keeps its full value through a string, slice or array index, as Go's does: a
// value past the length panics with the UNSIGNED value and the length (goPanicIndexU). The emission
// narrowed it first -- `(int)(x)` for a string and `(nint)(x)` for a slice or array -- so a string
// index past int's range TRUNCATED to a valid position and read the wrong byte where Go panics, and
// a slice or array index at or above 2^63 read negative and reported `[-N]`. A signed int64 string
// index truncated the same way. Every case recovers its panic and prints it; the in-range reads,
// the write, the pointer-to-array base and the byte index are the guards that the route still reads.

type bytes []byte
type path string
type block [4]byte

func try(name string, f func() byte) {
	defer func() {
		if r := recover(); r != nil {
			fmt.Printf("%s: %v\n", name, r)
		}
	}()
	fmt.Printf("%s: read %d\n", name, f())
}

func main() {
	s := "0123456789"
	b := []byte("abcdefghij")
	var a [10]byte
	copy(a[:], b)
	nb := bytes(b)
	np := path(s)
	nk := block{1, 2, 3, 4}
	pa := &a

	var u64 uint64 = 1<<32 + 5
	var u32 uint32 = 1<<32 - 1
	var u uint = 1<<63 + 2
	var up uintptr = 1<<63 + 3
	var i64 int64 = 1<<32 + 5
	var in32 uint32 = 7
	var in64 uint64 = 2
	var b8 uint8 = 250

	// A string: the index narrowed to int and read a valid byte.
	try("string uint64", func() byte { return s[u64] })
	try("string uint", func() byte { return s[u] })
	try("string uintptr", func() byte { return s[up] })
	try("string int64", func() byte { return s[i64] })
	try("string uint32", func() byte { return s[u32] })
	try("named string uint64", func() byte { return np[u64] })

	// A slice or an array: an index at or above 2^63 read negative.
	try("slice uint", func() byte { return b[u] })
	try("slice uintptr", func() byte { return b[up] })
	try("slice uint32", func() byte { return b[u32] })
	try("array uint", func() byte { return a[u] })
	try("array uintptr", func() byte { return a[up] })
	try("named slice uint", func() byte { return nb[u] })
	try("named array uint", func() byte { return nk[u] })
	try("slice write uint", func() byte { b[u] = 1; return 0 })
	try("pointer to array uint", func() byte { return pa[u] })

	// Guards: in range, and a byte index (unchanged).
	try("string in range", func() byte { return s[in32] })
	try("slice in range", func() byte { return b[in64] })
	try("array in range", func() byte { return a[in32] })
	try("named slice in range", func() byte { return nb[in64] })
	try("named string in range", func() byte { return np[in64] })
	try("named array in range", func() byte { return nk[in64] })
	try("slice byte index", func() byte { return b[b8] })
}
