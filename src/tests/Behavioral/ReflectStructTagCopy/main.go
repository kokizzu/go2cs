package main

import (
	"fmt"
	"reflect"
)

// Two reflection-bridge descriptor reads that the standard library's tag-driven decoders depend on.
//
// StructField.Tag: the converter has always emitted a tagged field's tag as `[GoTag(...)]` at the
// declaration, and nothing read it — so every converted struct reported as UNTAGGED. encoding/asn1
// omits an `optional` field whose value equals its zero, so with the tag invisible crypto/x509
// marshalled a nil OID instead of omitting it ("asn1: structure error: invalid object identifier").
//
// reflect.Copy: the auto conversion reinterprets both operands' data words as flat slice headers
// and hands them to typedslicecopy — no managed form, and on the bridge it dereferenced a nil box.
// asn1's parseField copies every parsed []byte through it. The copy must also ALIAS correctly: a
// slice value windows the backing store it shares with its parent, so a write is a write the
// parent sees.

type record struct {
	Version  int
	Name     string `json:"name" asn1:"optional,explicit,tag:0"`
	Data     []byte `json:"data,omitempty"`
	Untagged bool
}

func main() {
	t := reflect.TypeOf(record{})

	for i := 0; i < t.NumField(); i++ {
		f := t.Field(i)
		fmt.Printf("%s raw=%q json=%q asn1=%q\n", f.Name, string(f.Tag), f.Tag.Get("json"), f.Tag.Get("asn1"))
	}

	// Lookup distinguishes an absent key from one explicitly set to the empty string.
	f := t.Field(1)
	v, ok := f.Tag.Lookup("json")
	fmt.Println("lookup json:", v, ok)
	v, ok = f.Tag.Lookup("missing")
	fmt.Println("lookup missing:", v, ok)

	// Copy: slice <- slice, truncating at the shorter side.
	dst := make([]byte, 4)
	n := reflect.Copy(reflect.ValueOf(dst), reflect.ValueOf([]byte{1, 2, 3, 4, 5, 6}))
	fmt.Println("copy slice:", n, dst)

	short := make([]byte, 8)
	n = reflect.Copy(reflect.ValueOf(short), reflect.ValueOf([]byte{9, 8, 7}))
	fmt.Println("copy short src:", n, short)

	// Copy: slice <- string (the documented special case for a byte element type).
	sdst := make([]byte, 3)
	n = reflect.Copy(reflect.ValueOf(sdst), reflect.ValueOf("hello"))
	fmt.Println("copy string:", n, sdst)

	// Copy writes through to the ORIGINAL backing store, not a detached copy.
	backing := []int{0, 0, 0, 0}
	window := backing[1:3]
	n = reflect.Copy(reflect.ValueOf(window), reflect.ValueOf([]int{5, 6, 7}))
	fmt.Println("copy window:", n, backing)

	// Copy into an addressable ARRAY reached through a pointer's Elem.
	var arr [3]int
	n = reflect.Copy(reflect.ValueOf(&arr).Elem(), reflect.ValueOf([]int{11, 12, 13, 14}))
	fmt.Println("copy array:", n, arr)

	// A nil destination copies nothing.
	var nilDst []byte
	n = reflect.Copy(reflect.ValueOf(nilDst), reflect.ValueOf([]byte{1, 2}))
	fmt.Println("copy nil dst:", n)
}
