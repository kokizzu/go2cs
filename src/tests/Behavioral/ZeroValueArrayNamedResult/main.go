package main

import "fmt"

// Guards the zero value of a NAMED RESULT whose default(T) is broken — the declaration site
// `Type name = default!;` the converter emits at function entry for `func f() (name T)`. Go's zero
// value of a `[N]T` is N zeroed elements; golib's array<T> carries its length in the constructed
// instance, so C# `default` (which runs NO field initializer and NO constructor) yields length 0
// with a null backing. Both measured stdlib shapes are here:
//
//	shape 1 — net/netip's `func (ip Addr) As16() (a16 [16]byte)`, where `a16[:8]` threw
//	          ArgumentException out of slice<T>'s constructor;
//	shape 2 — crypto/tls's `func (c *Config) ticketKeyFromBytes(b [32]byte) (key ticketKey)`,
//	          whose ticketKey carries `aesKey [16]byte` — copy() into it moved 0 bytes and
//	          aes.NewCipher was handed a 0-length key.
//
// The `var z T` twin of this is ZeroValueStructVar (which already constructs); the
// composite-literal-omission twin is ZeroValueArrayField. This test is the DECLARATION-site
// member of that family: the same ladder (fixed array -> new(N), promoted embed -> new(nil),
// array-bearing struct -> new()) now runs for named results, func-literal named results and the
// deferred-named-return lowering, and a scalar-only result still stays default!.

type ticket struct {
	aesKey  [16]byte
	hmacKey [16]byte
	seq     int
}

type box struct {
	id int
	t  ticket
}

// Shape 1: a named result of unnamed fixed-array type, written through a SLICE of itself — the
// exact netip.As16 reach (`byteorder.BePutUint64(a16[:8], …)`).
//
//go:noinline
func as16(hi, lo uint64) (a16 [16]byte) {
	putUint64(a16[:8], hi)
	putUint64(a16[8:], lo)
	return a16
}

func putUint64(b []byte, v uint64) {
	for i := 0; i < 8; i++ {
		b[i] = byte(v >> (56 - 8*i))
	}
}

// Shape 2: a named result whose STRUCT type carries fixed arrays, filled by copy() — the
// ticketKeyFromBytes reach.
//
//go:noinline
func ticketFromBytes(b [32]byte) (key ticket) {
	copy(key.aesKey[:], b[:16])
	copy(key.hmacKey[:], b[16:])
	key.seq = 7
	return key
}

// Nested: the array sits one value-struct deeper, so the ladder must recurse.
//
//go:noinline
func makeBox() (bx box) {
	bx.id = 3
	bx.t.aesKey[0] = 9
	bx.t.hmacKey[15] = 4
	return bx
}

// The deferred-named-return lowering declares its result slots OUTSIDE the wrapper, a separate
// emission site from the plain prologue above.
//
//go:noinline
func withDefer() (a4 [4]byte, err error) {
	defer func() {
		a4[0]++
	}()
	a4[1] = 2
	return a4, nil
}

// A function LITERAL's named results take the iife lowering's own declaration path.
var literalAs3 = func() (a3 [3]byte) {
	a3[2] = 5
	return a3
}

// Control: a scalar-only named result must NOT construct — its zero value stays default!, so the
// golden proves the ladder does not over-fire across the corpus.
//
//go:noinline
func scalars() (n int, s string) {
	n = 5
	return n, s
}

func main() {
	a := as16(0x0102030405060708, 0x090a0b0c0d0e0f10)
	fmt.Println(len(a), a[0], a[7], a[8], a[15])

	var raw [32]byte
	for i := range raw {
		raw[i] = byte(i + 1)
	}
	k := ticketFromBytes(raw)
	fmt.Println(len(k.aesKey), len(k.hmacKey), k.aesKey[0], k.aesKey[15], k.hmacKey[0], k.hmacKey[15], k.seq)

	bx := makeBox()
	fmt.Println(bx.id, len(bx.t.aesKey), bx.t.aesKey[0], bx.t.hmacKey[15])

	d, err := withDefer()
	fmt.Println(len(d), d[0], d[1], err == nil)

	l := literalAs3()
	fmt.Println(len(l), l[2])

	n, s := scalars()
	fmt.Println(n, len(s))
}
