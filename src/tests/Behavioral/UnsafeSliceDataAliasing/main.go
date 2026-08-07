// unsafe.SliceData is DEFINED as &slice[:1][0] -- an interior pointer into the slice's own
// backing store -- so a faithful conversion has to model it as the array-element reference the
// converter already emits for &s[0], not as a pinned buffer over the whole backing array.
//
// The pin was wrong three ways, and this program exercises all three:
//
//  1. It PINNED. GCHandle refuses to pin storage whose element type carries a managed reference,
//     so SliceData over any such slice threw "Object contains references". log/slog's GroupValue
//     is the corpus witness -- groupptr(unsafe.SliceData(as)) over []Attr, rebuilt by
//     unsafe.Slice in Value.group(), which is pure interior-pointer identity and never an
//     address. It took down every grouping path in the package.
//  2. It ignored the slice's LOW bound, so SliceData(s[1:]) addressed element 0 of the backing
//     array rather than element 1.
//  3. It was undereferenceable for any element type but byte.
package main

import (
	"fmt"
	"unsafe"
)

type item struct {
	key string
	val int
}

// group models log/slog's Value: a group of attributes is stored as the data pointer of its
// []item plus the length, and rebuilt on demand. The element type carries a string, which is
// exactly the reference-bearing storage the managed model cannot pin.
type group struct {
	ptr *item
	n   int
}

func newGroup(items []item) group {
	return group{ptr: unsafe.SliceData(items), n: len(items)}
}

func (g group) items() []item {
	return unsafe.Slice(g.ptr, g.n)
}

func show(label string, items []item) {
	fmt.Print(label, ":")
	for _, it := range items {
		fmt.Print(" ", it.key, "=", it.val)
	}
	fmt.Println()
}

func main() {
	items := []item{{"a", 1}, {"b", 2}, {"c", 3}}

	// 1. Round trip through a reference-bearing element type.
	g := newGroup(items)
	show("round trip", g.items())
	fmt.Println("len:", len(g.items()))

	// The rebuilt window ALIASES the original backing, so a write through it is visible in the
	// source slice -- Go's unsafe.Slice semantics, not a snapshot.
	g.items()[1].val = 20
	show("after write through", items)

	// 2. SliceData IS &s[0]: pointer identity, and a re-sliced source starts at its own low bound.
	fmt.Println("identity:", unsafe.SliceData(items) == &items[0])
	tail := items[1:]
	fmt.Println("tail identity:", unsafe.SliceData(tail) == &items[1])
	fmt.Println("tail head:", unsafe.SliceData(tail).key)

	// 3. Dereferenceable for a non-byte element type.
	nums := []int64{10, 20, 30}
	fmt.Println("int64 head:", *unsafe.SliceData(nums))
	fmt.Println("int64 tail head:", *unsafe.SliceData(nums[2:]))

	// A slice of pointers is another reference-bearing element type.
	ptrs := []*item{&items[0], &items[2]}
	fmt.Println("ptr elem:", unsafe.Slice(unsafe.SliceData(ptrs), len(ptrs))[1].key)

	// The plain byte case that always worked, kept as the control.
	b := []byte("hello")
	fmt.Println("bytes:", string(unsafe.Slice(unsafe.SliceData(b), len(b))))
	fmt.Println("bytes tail:", string(unsafe.Slice(unsafe.SliceData(b[2:]), 3)))
}
