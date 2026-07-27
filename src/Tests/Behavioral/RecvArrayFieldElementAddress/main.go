package main

import "fmt"

// Taking the address of an element of an ARRAY FIELD OF THE RECEIVER — `&h.head[i]` inside a
// pointer-receiver method — must ALIAS the array slot. The defect emitted the one-argument
// `Ꮡ(h.head[i])` form, whose `Ꮡ(in T)` overload heap-boxes a COPY of the element, so every
// write through the returned pointer was silently dropped. `refRecv` is computed from the ROOT
// identifier of the index base, so an array FIELD of the receiver matched the "receiver IS the
// array" copy-form; only a bare receiver ident may use it. The correct emission is the
// element-aliasing two-argument `Ꮡ(h.head, (int)i)`, which shares the array's backing storage
// (`array<T>` is a readonly struct wrapping an eagerly allocated `T[]`, so copying the wrapper
// still shares elements).
//
// compress/flate's deflate() is exactly this shape — `hh := &d.hashHead[hash&hashMask]; …
// *hh = uint32(d.index + d.hashOffset)`. With the writes dropped the hash heads stayed all-zero,
// findMatch was never reached, and levels 2-9 emitted LITERALS ONLY: valid deflate streams ~2.3x
// larger than Go's (a 256x256 PNG went 36,760 -> 134,644 bytes). Sibling guards cover the same
// element address reached through a pointer PARAM/LOCAL (PointerFieldArrayElementAddress), a
// receiver SCALAR field (ReceiverFieldAddress), and wide indexes on a LOCAL array
// (ArrayWideIndexAddress); the receiver's own array field was the uncovered case.

const mask = 7

type hasher struct {
	head  [mask + 1]uint32
	prev  [16]uint32
	pairs [2][3]int
}

// storeHead mirrors flate's chained-hash update: read the current head THROUGH the pointer,
// chain it into prev, then overwrite the head through the SAME pointer. The index is unsigned
// (uint32), which also exercises the (int) narrowing the two-argument overload requires.
func (h *hasher) storeHead(hash uint32, index uint32) uint32 {
	hh := &h.head[hash&mask]
	previous := *hh
	h.prev[index&15] = previous
	*hh = index + 1
	return previous
}

// bumpPair takes the element address of a NESTED array field (`&h.pairs[i][j]`), the shape
// image/jpeg uses for `&d.huff[tc][th]`.
func (h *hasher) bumpPair(i, j int) {
	p := &h.pairs[i][j]
	*p += 10
}

// headAt reads the array back through the receiver — NOT through the pointer — which is what
// the copy-form silently broke.
func (h *hasher) headAt(i int) uint32 { return h.head[i] }

func main() {
	// Spell the nested fixed-size array field out: a zero-value composite does not allocate a
	// nested fixed-size array field (the limitation PointerFieldArrayElementAddress documents).
	h := &hasher{pairs: [2][3]int{{0, 0, 0}, {0, 0, 0}}}

	// Writes through &h.head[...] must be visible when the ARRAY is read back.
	fmt.Println(h.storeHead(3, 100)) // 0   - nothing chained yet
	fmt.Println(h.storeHead(3, 200)) // 101 - previous head, read back out of the array
	fmt.Println(h.headAt(3))         // 201
	fmt.Println(h.prev[200&15])      // 101

	// A different bucket must be independent, and 12&7 == 4 exercises the masked index.
	fmt.Println(h.storeHead(12, 7)) // 0
	fmt.Println(h.headAt(4))        // 8
	fmt.Println(h.headAt(3))        // 201 - untouched

	// Read-modify-write through a nested array field element.
	h.bumpPair(1, 2)
	h.bumpPair(1, 2)
	h.bumpPair(0, 0)
	fmt.Println(h.pairs[1][2], h.pairs[0][0], h.pairs[0][1]) // 20 10 0

	// Every head slot the program never touched must still be zero.
	total := uint32(0)
	for i := 0; i < len(h.head); i++ {
		total += h.head[i]
	}
	fmt.Println("head sum:", total) // 209
}
