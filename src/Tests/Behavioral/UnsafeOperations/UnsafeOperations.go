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

	i2 := Float64bits(9.5)
	f2 := Float64frombits(i2)
	fmt.Println(i2)
	fmt.Println(f2)
}
