package main

import "fmt"

// Exercises Go's nil-vs-empty slice distinction: `s == nil` is REPRESENTATION nilness
// (a nil backing pointer), not emptiness. A zero-length zero-capacity view over a real
// array ([]byte{}, x[len(x):], []byte("")) is observably NON-nil, while nil stays nil
// through reslicing (nil[0:0]) and no-op appends (append(nil)). bytes TestTrim/TestClone/
// TestTrimFunc observe exactly these identities.
func probe(name string, isNil bool, length, capacity int) {
	fmt.Println(name, isNil, length, capacity)
}

func main() {
	var zero []byte
	probe("zeroValue", zero == nil, len(zero), cap(zero))

	emptyLit := []byte{}
	probe("emptyLiteral", emptyLit == nil, len(emptyLit), cap(emptyLit))

	emptyStr := []byte("")
	probe("emptyStringConv", emptyStr == nil, len(emptyStr), cap(emptyStr))

	nilConv := []byte(nil)
	probe("nilConversion", nilConv == nil, len(nilConv), cap(nilConv))

	nilReslice := zero[0:0]
	probe("nilReslice", nilReslice == nil, len(nilReslice), cap(nilReslice))

	made := make([]byte, 0)
	probe("makeZero", made == nil, len(made), cap(made))

	x := []byte{1, 2}
	probe("resliceToEmpty", x[:0] == nil, len(x[:0]), cap(x[:0]))

	// KEY discriminator: len 0 AND cap 0, yet non-nil — the backing pointer is real.
	tail := x[2:2]
	probe("resliceTailCapZero", tail == nil, len(tail), cap(tail))

	appendNilNothing := append([]byte(nil))
	probe("appendNilNothing", appendNilNothing == nil, len(appendNilNothing), cap(appendNilNothing))

	appendEmptyNothing := append([]byte{})
	probe("appendEmptyNothing", appendEmptyNothing == nil, len(appendEmptyNothing), cap(appendEmptyNothing))

	// bytes.Clone shape: append onto an empty literal from an empty source must stay non-nil.
	cloneShape := append([]byte{}, x[:0]...)
	probe("cloneShape", cloneShape == nil, len(cloneShape), cap(cloneShape))

	// bytes.TrimRight shape: slicing a non-nil slice down to zero length stays non-nil.
	trim := x
	for len(trim) > 0 {
		trim = trim[:len(trim)-1]
	}
	probe("trimShape", trim == nil, len(trim), cap(trim))

	// The distinction survives a non-byte element type the same way.
	var zs []string
	probe("stringSliceZero", zs == nil, len(zs), cap(zs))
	probe("stringSliceEmpty", []string{} == nil, len([]string{}), cap([]string{}))

	// THE VARIADIC PACK carries nil-ness too, and it is the shape that reaches the callee through a
	// C# `params` boundary rather than through a slice header. A zero-argument call materializes NIL
	// — not an empty slice — while a spread passes exactly the slice it was given, nil or not.
	// unique/clone_test reads this back through reflect.DeepEqual, which separates the two.
	variadic("packZeroArgs")
	variadic("packOneArg", 1)
	variadic("packTwoArgs", 1, 2)

	var nilPack []byte
	variadic("spreadNil", nilPack...)
	variadic("spreadEmptyLiteral", []byte{}...)
	variadic("spreadMakeZero", make([]byte, 0)...)
	variadic("spreadResliceTailCapZero", x[2:2]...)
	variadic("spreadNonEmpty", x...)

	// A nil pack stays nil through the same no-op reslice and append the header cases use above.
	variadicDerived("packZeroArgsDerived")
	variadicDerived("packOneArgDerived", 9)
}

// variadic reports the pack's own identity, exactly as probe does for a slice header.
func variadic(name string, pack ...byte) {
	probe(name, pack == nil, len(pack), cap(pack))
}

// variadicDerived checks that nil-ness survives the operations a callee typically performs on its
// pack before observing it.
func variadicDerived(name string, pack ...byte) {
	probe(name+"/reslice", pack[0:0] == nil, len(pack[0:0]), cap(pack[0:0]))
	appended := append(pack)
	probe(name+"/appendNothing", appended == nil, len(appended), cap(appended))
	fmt.Println(name+"/reSpread", spreadThrough(pack...) == nil)
}

// spreadThrough forwards its pack to another variadic function, the `f(a...)` idiom fmt uses
// throughout (Println -> Fprintln(w, a...)): the forwarded pack must keep the original's nil-ness.
func spreadThrough(pack ...byte) []byte {
	return pack
}
