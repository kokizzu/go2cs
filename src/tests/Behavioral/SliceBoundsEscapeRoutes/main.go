package main

import (
	"fmt"
	"sync/atomic"
)

// Out-of-range indices and bounds must panic with Go's runtime error, recoverably, and never address or slice the
// wrong elements: an element address (&x[i]) at a wide index or past the slice's length, a 3-index slice bound past
// int32, a foreign pointer method on an element (Ꮡ(x, i).M()), a string literal indexed out of range, and a string
// sliced past its end.

func arm(name string, f func()) {
	defer func() {
		if r := recover(); r != nil {
			fmt.Println(name, "recovered:", r)
		}
	}()
	f()
	fmt.Println(name, "no panic")
}

func main() {
	var wideU uint64 = 1<<32 + 5
	var wideI int64 = 1<<32 + 5
	wideInt := 1<<32 + 5

	arm("address uint64:", func() {
		a := make([]int, 10)
		p := &a[wideU]
		*p = 7
		fmt.Println("a[5] =", a[5])
	})
	arm("address int64:", func() {
		a := make([]int, 10)
		p := &a[wideI]
		*p = 7
		fmt.Println("a[5] =", a[5])
	})
	arm("address int:", func() {
		a := make([]int, 10)
		p := &a[wideInt]
		*p = 7
		fmt.Println("a[5] =", a[5])
	})
	arm("address past length:", func() {
		s := make([]int, 3, 10)
		i := 5
		p := &s[i]
		*p = 7
		fmt.Println("s[:6][5] =", s[:6][5])
	})
	arm("3-index bound:", func() {
		s := make([]int, 10)
		t := s[0:wideU:wideU]
		fmt.Println("len", len(t), "cap", cap(t))
	})
	arm("element method:", func() {
		a := make([]atomic.Uint64, 10)
		a[wideU].Add(1)
		fmt.Println("a[5] =", a[5].Load())
	})
	past := 5
	arm("literal index:", func() {
		fmt.Println("byte", "abc"[past])
	})
	neg := -1
	arm("literal negative index:", func() {
		fmt.Println("byte", "abc"[neg])
	})
	arm("string past end:", func() {
		s := "abc"
		fmt.Println(s[1:past])
	})
	low := 4
	arm("string low past end:", func() {
		s := "abc"
		fmt.Println(s[low:])
	})
}
