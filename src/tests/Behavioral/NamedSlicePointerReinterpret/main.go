package main

import "fmt"

// A named slice type over []byte — the log/slog/internal/buffer `type Buffer []byte` shape.
type Buf []byte

// fillVia reinterprets an EXISTING pointer parameter (not `&x`): cryptobyte's ReadASN1Bytes does
// `(*String)(out)` with `out *[]byte`, and crypto/tls's readUint{8,16,24}LengthPrefixed do the same
// with an out-parameter whose whole purpose is the write-back. The derived pointer ALIASES the
// caller's slice, so what is appended through it has to be visible through `out` afterwards.
func fillVia(out *[]byte) string {
	p := (*Buf)(out)
	*p = append(*p, 'A', 'B', 'C')
	return string(*p)
}

// handler is log/slog's commonHandler shape: a struct FIELD reinterpreted so that everything
// pre-formatted through the derived pointer lands in the field itself. Against a copy, WithAttrs
// silently dropped every attribute while still advancing the handler's group state.
type handler struct {
	preformatted []byte
	groups       int
}

func (h *handler) preformat(s string) {
	buf := (*Buf)(&h.preformatted)
	*buf = append(*buf, s...)
	h.groups++
}

func main() {
	// Pointer reinterpret from *[]byte (the underlying) to *Buf (the named wrapper). A bare
	// (ж<Buf>)(Ꮡ(b)) cast is CS0030 (unrelated ж instantiations), so the converter emits golib's
	// storage reinterpret — `Ꮡb.Reinterpret<slice<byte>, Buf>()`, which re-views the SAME slot as
	// the single-field wrapper. The pointer therefore ALIASES `b`: reading `b` back is the point.
	b := make([]byte, 0, 8)
	p := (*Buf)(&b)
	*p = append(*p, 'h', 'i')
	*p = append(*p, '!')
	fmt.Println(string(*p)) // hi!
	fmt.Println(len(*p))    // 3
	fmt.Println(string(b))  // hi!  — the write reaches the addressed local
	fmt.Println(len(b))     // 3

	// The slog/buffer shape: the reinterpret inside a closure that returns the *Buf (its
	// sync.Pool New func does `return (*Buffer)(&b)`).
	makeBuf := func() *Buf {
		s := make([]byte, 0, 4)
		return (*Buf)(&s)
	}
	q := makeBuf()
	*q = append(*q, 'x', 'y')
	fmt.Println(string(*q)) // xy

	// The pointer-PARAMETER reinterpret (cryptobyte / crypto/tls out-parameter shape).
	src := make([]byte, 0, 4)
	fmt.Println(fillVia(&src)) // ABC
	fmt.Println(string(src))   // ABC — the out-parameter really received the bytes
	fmt.Println(len(src))      // 3

	// The struct-FIELD reinterpret (log/slog commonHandler.withAttrs shape), across an append
	// that outgrows the original backing store.
	h := &handler{}
	h.preformat("alpha=1")
	h.preformat(" beta=2")
	h.preformat(" gamma=3")
	fmt.Println(string(h.preformatted), len(h.preformatted), h.groups) // alpha=1 beta=2 gamma=3 22 3

	// Truncation through a freshly derived pointer is visible in the field too.
	t := (*Buf)(&h.preformatted)
	*t = (*t)[:7]
	fmt.Println(string(h.preformatted), len(h.preformatted)) // alpha=1 7
}
