package main

import (
	"fmt"
	"time"
)

// Fiat-shaped ж-bound hot loop (DESIGN-zh-box-reduction §7 item 1): pointer parameters,
// address-taken locals, and field addresses — exactly the shapes the ref-lowering arc changes,
// which the rest of the perf suite never exercises (12 of 13 transpiled benchmarks contain zero
// ж/Ꮡ/heap( occurrences). Before the lowering every iteration minted field-ref boxes and
// heap-boxed locals; after it the loop allocates nothing. Deterministic: pure integer mixing,
// fixed rounds, one checksum line.

type felem struct {
	x [4]uint64
}

// cmov is the class-2 sink: address-taken locals feed its pointer parameter.
func cmov(out *uint64, cond, a, b uint64) {
	m := uint64(0) - (cond & 1)
	*out = (m & b) | (^m & a)
}

// norm is the row-1 target: field addresses (&e.x) feed its array-pointer parameter.
func norm(out *[4]uint64, k uint64) {
	var c0, c1 uint64
	cmov(&c0, k, out[0]^k, out[0]+k)
	cmov(&c1, out[1]&1, out[1]+c0, out[1]^c0)
	out[0] = c0
	out[1] += c1
	out[2] ^= c0
	out[3] += c1 ^ c0
}

// mix forwards derived field addresses onward (D1') and feeds the sink from its own locals.
func mix(e, t *felem, k uint64) {
	norm(&e.x, k)
	norm(&t.x, k+1)
	for i := 0; i < 4; i++ {
		var s uint64
		cmov(&s, t.x[i]&1, e.x[i]+t.x[i], e.x[i]^t.x[i])
		e.x[i] = s
	}
}

func main() {
	start := time.Now().UnixNano()

	var e, t felem
	e.x = [4]uint64{0x9e3779b97f4a7c15, 0xbf58476d1ce4e5b9, 0x94d049bb133111eb, 0x2545f4914f6cdd1d}
	t.x = [4]uint64{1, 2, 3, 4}
	for r := 0; r < 20000000; r++ {
		mix(&e, &t, uint64(r))
	}
	sum := e.x[0] ^ e.x[1] ^ e.x[2] ^ e.x[3] ^ t.x[0] ^ t.x[3]

	elapsed := time.Now().UnixNano() - start
	fmt.Println("checksum:", sum)
	fmt.Println("elapsed_ns:", elapsed)
}
