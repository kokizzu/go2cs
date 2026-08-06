package main

import (
	"fmt"
	"unsafe"
)

type T1 struct {
	a int32
}

type T2 struct {
	a int32
}

type Inner struct {
	p int32
	q int64
}

type Outer struct {
	head byte
	in   Inner
}

var gOuter Outer

// Padded is shaped so that GO's layout rules — and only Go's — produce the numbers below: a
// leading bool forces 7 bytes of padding before the int64, a byte forces another 7 before the
// string, and a trailing int32 forces tail padding up to the struct's 8-byte alignment. It also
// holds a string and a slice, so the CONVERTED C# struct is non-blittable: this is exactly the
// shape a reflection-based `Sizeof` (Marshal.SizeOf<T>) throws on rather than answering.
type Padded struct {
	flag  bool
	count int64
	tag   byte
	name  string
	data  []byte
	code  int32
}

// Embedded promotes Padded's fields. Go measures Offsetof against the IMMEDIATELY enclosing
// struct, so `e.count` is its offset within Padded, not within Embedded.
type Embedded struct {
	lead byte
	Padded
	trail int16
}

// Arrays exercises array element/whole-array operands, whose alignment is the element's.
type Arrays struct {
	head  int16
	cells [5]int32
	tail  byte
}

func Float64bits(f float64) uint64 {
	return *(*uint64)(unsafe.Pointer(&f))
}

func Float64frombits(b uint64) float64 {
	return *(*float64)(unsafe.Pointer(&b))
}

func main() {
	b := []byte{}

	for ch := 32; ch < 80; ch++ {
		b = append(b, string(rune(ch))...)
	}

	str := unsafe.String(&b[0], len(b))
	fmt.Println(str)

	strptr := unsafe.StringData(str)
	fmt.Println(unsafe.String(strptr, len(str)))

	arr := [4]int{1, 2, 3, 4}
	arrptr := &arr[0]

	// Move the pointer to the next element in the array
	nextPtr := unsafe.Pointer(uintptr(unsafe.Pointer(arrptr)) + unsafe.Sizeof(arr[0]))
	fmt.Println("Value of the next element:", *(*int)(nextPtr))

	var t1 T1
	t1.a = 42

	// Convert t1 to type T2
	t2 := *(*T2)(unsafe.Pointer(&t1))
	fmt.Println("Value of t2.a:", t2.a)

	var i int8 = -1
	var j = int16(i)
	fmt.Println(i, j)
	var k uint8 = *(*uint8)(unsafe.Pointer(&i))
	fmt.Println(k)

	var x struct {
		a int64
		b bool
		c string
	}
	const M, N = unsafe.Sizeof(x.c), unsafe.Sizeof(x)
	fmt.Println(M, N)

	fmt.Println(unsafe.Alignof(x.a))
	fmt.Println(unsafe.Alignof(x.b))
	fmt.Println(unsafe.Alignof(x.c))

	fmt.Println(unsafe.Offsetof(x.a))
	fmt.Println(unsafe.Offsetof(x.b))
	fmt.Println(unsafe.Offsetof(x.c))

	// Operand shapes that are not a bare identifier or a one-level `ident.field`. Sizeof/Alignof/
	// Offsetof are defined against the operand's STATIC type, so each must resolve through the type
	// system rather than through the text of the converted expression.
	//
	// A CONVERSION operand renders with a leading cast, which a text-derived reshape turns into
	// `(uint32)0.GetType()` — parsed by C# as `(uint32)(0.GetType())`, CS0030. This is crypto/md5's
	// benchmarkSize alignment probe.
	fmt.Println(unsafe.Alignof(uint32(0)))
	fmt.Println(unsafe.Alignof(float64(0)))

	// An INDEX operand, and a selector reached THROUGH A POINTER.
	fmt.Println(unsafe.Alignof(arr[0]))
	op := &gOuter
	fmt.Println(unsafe.Alignof(op.in.q))

	// A TWO-level selector: Offsetof is relative to the immediately enclosing struct, so `q`'s
	// offset is measured within Inner, not within Outer.
	fmt.Println(unsafe.Offsetof(gOuter.in.q))

	// A field whose name is a C# keyword: the emitted identifier escapes to `@in`, but reflection
	// names it `in`.
	fmt.Println(unsafe.Offsetof(op.in))

	// Sizeof/Alignof/Offsetof at EXPRESSION sites. Go computes all three at COMPILE TIME from the
	// operand's static type under Go's layout rules, so every number here is Go's — not a
	// measurement of the converted C# struct. The operand is never evaluated.
	var p Padded
	var e Embedded
	var a Arrays

	// Call-argument position.
	fmt.Println(unsafe.Sizeof(p), unsafe.Alignof(p), unsafe.Offsetof(p.name))

	// Arithmetic position: sums, differences and the classic element-count division.
	fmt.Println(unsafe.Sizeof(p)+unsafe.Sizeof(a), unsafe.Offsetof(p.code)-unsafe.Offsetof(p.count))
	fmt.Println(unsafe.Sizeof(a.cells) / unsafe.Sizeof(a.cells[0]))
	fmt.Println(unsafe.Sizeof(p.name)*2 + unsafe.Alignof(p.count))

	// Comparison position.
	fmt.Println(unsafe.Sizeof(p) > unsafe.Sizeof(a), unsafe.Alignof(p.flag) == unsafe.Alignof(p.tag))

	// Assignment position, including compound assignment against a uintptr variable.
	sz := unsafe.Sizeof(e)
	sz += unsafe.Offsetof(e.trail)
	fmt.Println(sz)

	// Sizing a make() — the `debug/elf` idiom of reading a fixed-layout header.
	buf := make([]byte, unsafe.Sizeof(p))
	fmt.Println(len(buf), cap(buf))

	// Embedded struct: a PROMOTED field reached through an embedded VALUE is measured relative to
	// the operand struct — `e.count` is 16 (8 for `lead` + its 8 inside Padded), not the 8 it sits
	// at within Padded. Only an explicit selector (`gOuter.in.q` above) measures inside the inner
	// struct. go/types folds the whole path; the reflection form could only see one hop.
	fmt.Println(unsafe.Sizeof(e), unsafe.Offsetof(e.Padded), unsafe.Offsetof(e.trail), unsafe.Offsetof(e.count))

	// Array element, whole array, and the field that follows one.
	fmt.Println(unsafe.Sizeof(a), unsafe.Alignof(a.cells), unsafe.Offsetof(a.cells), unsafe.Offsetof(a.tail))

	i2 := Float64bits(9.5)
	f2 := Float64frombits(i2)
	fmt.Println(i2)
	fmt.Println(f2)
}
