package main

import (
	"fmt"
	"io"
)

// A pointer-interface adapter class is named "<struct>ж<ifaceSimple>". The STRUCT side is
// package-qualified when foreign, but the INTERFACE side composed from its bare last-dot segment —
// so ONE struct cast to TWO interfaces sharing a simple name produced ONE class name TWICE
// (CS0102 + CS0111 per member + CS8646). This is that shape: a package-local `Reader` that embeds
// `io.Reader`, with *src cast to BOTH. compress/flate is the real case — its own `Reader`
// (io.Reader + io.ByteReader) plus io.Reader, reached from the same *bufio.Reader and *bytes.Reader
// in its tests, which is what kept the package from building at all.
//
// The whole production corpus has ZERO such collisions; only a test closure's extra casts produce
// one, which is why this survived the Phase-3 clean-compile milestone. The rule is
// COLLISION-CONDITIONAL — a colliding group qualifies its FOREIGN members (srcжio_Reader) and
// leaves the LOCAL one bare (srcжReader) — because qualifying unconditionally would rename 644
// adapters across 3,688 construction sites. Converter and generator must agree on every name, so
// both read the same authority: the emitted GoImplement records.
// Remaining (rather than flate's ReadByte) is the distinguishing member on purpose: an interface
// of io.Reader + ReadByte is structurally io.ByteReader, which the converter then emits as an
// embed — and the BASELINE core/io stub this suite builds against has no ByteReader. The collision
// under test needs only the shared simple name, not flate's exact method set.
type Reader interface {
	io.Reader
	Remaining() int
}

type src struct {
	data []byte
	pos  int
}

func (s *src) Read(p []byte) (int, error) {
	if s.pos >= len(s.data) {
		return 0, io.EOF
	}
	n := copy(p, s.data[s.pos:])
	s.pos += n
	return n, nil
}

func (s *src) Remaining() int {
	return len(s.data) - s.pos
}

// viaForeign takes the FOREIGN interface — its adapter is the one that must be qualified.
func viaForeign(r io.Reader) string {
	buf := make([]byte, 4)
	n, err := r.Read(buf)
	if err != nil {
		return "err"
	}
	return string(buf[:n])
}

// viaLocal takes the package-LOCAL interface of the same simple name, and exercises the member
// that distinguishes it, so the two adapters cannot be conflated at runtime either.
func viaLocal(r Reader) string {
	before := r.Remaining()
	buf := make([]byte, 3)
	n, err := r.Read(buf)
	if err != nil {
		return "err"
	}
	return fmt.Sprintf("%d|%s|%d", before, string(buf[:n]), r.Remaining())
}

func main() {
	// Same struct, both interfaces — the pair that collided.
	fmt.Println(viaLocal(&src{data: []byte("abcd")}))
	fmt.Println(viaForeign(&src{data: []byte("wxyz")}))

	// Interleaved through one value, so a wrong adapter binding would show as wrong bytes
	// rather than merely failing to compile.
	s := &src{data: []byte("0123456789")}
	fmt.Println(viaLocal(s))
	fmt.Println(viaForeign(s))
	fmt.Println(viaLocal(s))
}
