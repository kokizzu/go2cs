// SliceBoundsShapes: every 2-index slice-bound failure shape R1-A must reproduce, one case per process (a CLR exception
// that escapes recover() ends the process, so the C# side runs each case alone: `SliceBoundsShapes N`). With no argument,
// every case runs in order (the Go reading).
package main

import (
	"fmt"
	"os"
	"strconv"
)

type named []int

type namedStr string

type namedArr [3]int

var sink any

func try(name string, f func()) {
	defer func() {
		r := recover()
		_, isErr := r.(interface{ RuntimeError() })
		fmt.Printf("%-24s %v | runtime.Error=%v\n", name, r, isErr)
	}()
	f()
}

func main() {
	s := make([]int, 3, 10)
	var a [3]int
	p := &a
	n := named(s)
	var na namedArr
	str := "abc"
	ns := namedStr("abc")
	neg, big, lo4, hi2, hi5, hi11 := -1, 1<<32+5, 4, 2, 5, 11
	var ubig uint64 = 1<<64 - 1
	var u32 uint32 = 11
	var i64 int64 = 1<<32 + 5

	cases := []struct {
		name string
		f    func()
	}{
		{"slice [:hi>cap]", func() { sink = s[:hi11] }},
		{"slice [:len<hi<=cap]", func() { sink = len(s[:hi5]) }},
		{"slice [:-1]", func() { sink = s[:neg] }},
		{"slice [-1:]", func() { sink = s[neg:] }},
		{"slice [-1:2]", func() { sink = s[neg:hi2] }},
		{"slice [4:2]", func() { sink = s[lo4:hi2] }},
		{"slice [4:]", func() { sink = s[lo4:] }},
		{"slice [:big]", func() { sink = s[:big] }},
		{"slice [big:]", func() { sink = s[big:] }},
		{"slice [:i64]", func() { sink = s[:i64] }},
		{"slice [:ubig]", func() { sink = s[:ubig] }},
		{"slice [:u32]", func() { sink = s[:u32] }},
		{"named [:-1]", func() { sink = n[:neg] }},
		{"named [4:2]", func() { sink = n[lo4:hi2] }},
		{"array [:5]", func() { sink = a[:hi5] }},
		{"array [:-1]", func() { sink = a[:neg] }},
		{"array [4:2]", func() { sink = a[lo4:hi2] }},
		{"array [4:]", func() { sink = a[lo4:] }},
		{"ptrarray [:5]", func() { sink = p[:hi5] }},
		{"namedarr [:5]", func() { sink = na[:hi5] }},
		{"string [:5]", func() { sink = str[:hi5] }},
		{"string [:-1]", func() { sink = str[:neg] }},
		{"string [-1:]", func() { sink = str[neg:] }},
		{"string [4:2]", func() { sink = str[lo4:hi2] }},
		{"string [4:]", func() { sink = str[lo4:] }},
		{"string [:big]", func() { sink = str[:big] }},
		{"namedstr [:5]", func() { sink = ns[:hi5] }},
		{"strlit [:5]", func() { var t string = "abc"[:hi5]; sink = t }},
		{"strlit [-1:]", func() { var t string = "abc"[neg:]; sink = t }},
	}

	if len(os.Args) > 1 {
		i, _ := strconv.Atoi(os.Args[1])
		try(cases[i].name, cases[i].f)
		return
	}

	for _, c := range cases {
		try(c.name, c.f)
	}
}
