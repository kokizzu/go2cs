package main

import "fmt"

// Go's m[string(b)] map-lookup special case: the compiler skips the []byte→string copy for a
// map-INDEX key because the key provably does not outlive the lookup
// (runtime.slicebytetostringtmp). go2cs emits golib's tmpstring(b) — a transient @string
// aliasing b's live bytes — for exactly that shape (net/textproto's canonicalMIMEHeaderKey
// common-header probe, a want-zero AllocsPerRun path). The semantics this program pins:
// (1) the probe sees the slice's CURRENT bytes, (2) a miss still yields the zero value,
// (3) the comma-ok read takes the same transient path, and (4) a map STORE key is COPIED —
// mutating the slice after m[string(b)] = v must not disturb the stored entry.

func main() {
	interned := map[string]string{
		"Content-Length": "len",
		"Host":           "host",
	}

	b := []byte("Content-Length")

	// (1) plain read — hit
	fmt.Println(interned[string(b)])

	// (2) mutate the slice; the same expression must now MISS (zero value)
	b[0] = 'X'
	fmt.Println(interned[string(b)] == "")

	// (3) comma-ok on both states
	b[0] = 'C'
	v, ok := interned[string(b)]
	fmt.Println(v, ok)
	b[0] = 'X'
	v, ok = interned[string(b)]
	fmt.Println(v == "", ok)
	b[0] = 'C'

	// (4) a sub-slice operand reaches the same lookup path
	fmt.Println(interned[string(b[:4])] == "")
	fmt.Println(interned[string(b[0:])])

	// (5) STORE key must COPY: write through string(k), then mutate k — the entry keyed by the
	// original bytes must survive, and the mutated key must miss.
	w := map[string]int{}
	k := []byte("alpha")
	w[string(k)] = 42
	k[0] = 'Z'
	fmt.Println(w["alpha"], len(w))
	_, hit := w[string(k)] // "Zlpha" — must miss
	fmt.Println(hit)

	// (6) delete with a []byte-derived key (a different builtin path that must keep copying)
	delete(w, string([]byte("alpha")))
	fmt.Println(len(w))
}
