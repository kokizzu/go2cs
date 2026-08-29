// `(*NamedArray)(new([N]T))` — a conversion to a pointer-to-NAMED-ARRAY type whose source is a
// FRESH allocation — must CONSTRUCT the wrapper and take its address, exactly as the sibling
// composite-literal spelling `&NamedArray{…}` already does.
//
// Before the fix it emitted a bare `(ж<Named>)Ꮡ(new array<T>(N))` cast, which is CS0030: ж<> is
// not variant, so ж<array<byte>> and ж<MyBytesArray0> are unrelated instantiations. Guards
// reflect's all_test.go:4501, `V((*MyBytesArray0)(new([0]byte)))`.
//
// SCOPE, measured rather than assumed. The `(*Named)(&existing)` spelling is NOT covered here and
// deliberately still fails to compile. The reinterpret arm that serves named SLICES cannot be
// widened to arrays: a named-ARRAY wrapper's generated field is `array<T>?` — a Nullable, both
// larger than the `array<T>` it wraps and a different shape — so golib's alias gate refuses it on
// the size test, the emission falls to a raw-address box, and its first indexed read dies with an
// AccessViolationException inside `array<byte>.get_Item`. (A named SLICE wrapper's field is a bare
// `slice<T>`, identical in size and layout, which is why that arm is correct and banked.) A loud
// CS0030 is the honest answer there; constructing instead would silently write through a copy on
// whole-value assignment, which is the log/slog `WithAttrs` bug this area already paid for once.
//
// The named-slice arms below are byte-identical CONTROLS: the fix must not disturb the path it
// sits beside.
package main

import "fmt"

type MyArray [4]byte
type MyEmptyArray [0]byte
type MyInts [3]int
type MyBytes []byte

func main() {
	// ---- The fixed arm: (*Named)(new(T)), a fresh allocation ----

	q := (*MyArray)(new([4]byte))
	q[0] = 7
	q[3] = 8
	fmt.Println("B1 fresh:", *q, "len:", len(*q))

	// The zero-length shape reflect's table uses.
	z := (*MyEmptyArray)(new([0]byte))
	fmt.Println("B2 zero-length:", *z, "len:", len(*z))

	// A non-byte element type.
	n := (*MyInts)(new([3]int))
	n[1] = 5
	fmt.Println("B3 ints:", *n, "len:", len(*n))

	// The fresh wrapper is genuinely independent storage: two of them do not share.
	a := (*MyArray)(new([4]byte))
	b := (*MyArray)(new([4]byte))
	a[0] = 1
	fmt.Println("B4 independent:", *a, *b)

	// ---- Arms that already compiled: controls ----

	// nil converts to any pointer.
	var nilp *MyEmptyArray = (*MyEmptyArray)(nil)
	fmt.Println("C1 nil:", nilp == nil)

	// The composite-literal address form — the emission this fix mirrors.
	c := &MyArray{1, 2, 3, 4}
	c[1] = 9
	fmt.Println("C2 composite:", *c)

	// Named-SLICE pointer conversions: the reinterpret arm, which must be untouched.
	// Both directions alias, so the appends below land on the original.
	sraw := []byte{1, 2, 3}
	sp := (*MyBytes)(&sraw)
	*sp = append(*sp, 4)
	fmt.Println("C3 named-slice ptr aliases original:", sraw)

	var mb MyBytes = MyBytes{9, 8}
	bp := (*[]byte)(&mb)
	*bp = append(*bp, 7)
	fmt.Println("C4 underlying-slice ptr aliases named:", mb)
}
