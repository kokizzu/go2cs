package main

// The `net` writev shape, reduced. An UNEXPORTED method reaches an exported struct only by
// PROMOTION through a value embed, and the only thing that ever asks for it is a dynamic type
// assertion to an UNEXPORTED same-package interface.
//
// net.Buffers.WriteTo is the original: `if wv, ok := w.(buffersWriter); ok` over an `io.Writer`
// whose dynamic value is a `*net.TCPConn` — an exported struct that promotes the unexported
// `writeBuffers` from its embedded unexported `conn`. Go takes the writev fast path; the converted
// C# took the per-chunk fallback, which is a silent behavioral divergence, not a compile error.
//
// The receiver form is load-bearing, which is why `conn`'s methods are written to need the real
// receiver box (`c != nil`, then `c.ok()`): that is what makes the converter emit them as
// direct-ж primaries (`M(this ж<conn>)`) rather than the `[GoRecv] this ref conn` form, and only
// the direct-ж promotion path carried the exported-name gate this test guards.

import (
	"fmt"
	"io"
)

// buffersWriter mirrors net's own: unexported interface, unexported method.
type buffersWriter interface {
	writeBuffers(*Buffers) (int64, error)
}

// Buffers mirrors net.Buffers — a named slice-of-slice with a pointer-receiver WriteTo.
type Buffers [][]byte

func (v *Buffers) WriteTo(w io.Writer) (int64, error) {
	if wv, ok := w.(buffersWriter); ok {
		return wv.writeBuffers(v)
	}
	var n int64
	for _, b := range *v {
		nb, err := w.Write(b)
		n += int64(nb)
		if err != nil {
			return n, err
		}
	}
	return n, nil
}

// sink stands in for net's *netFD — a pointer field, so conn's `ok` has something to test.
type sink struct {
	bytes []byte
	mode  string
}

// conn mirrors net.conn: unexported, and the type that actually carries the unexported method.
type conn struct {
	fd *sink
}

// ok mirrors net.conn.ok — comparing the RECEIVER against nil is what forces the direct-ж primary.
func (c *conn) ok() bool { return c != nil && c.fd != nil }

func (c *conn) Write(b []byte) (int, error) {
	if !c.ok() {
		return 0, fmt.Errorf("invalid conn")
	}
	c.fd.bytes = append(c.fd.bytes, b...)
	c.fd.mode = "write"
	return len(b), nil
}

func (c *conn) writeBuffers(v *Buffers) (int64, error) {
	if !c.ok() {
		return 0, fmt.Errorf("invalid conn")
	}
	var n int64
	for _, b := range *v {
		c.fd.bytes = append(c.fd.bytes, b...)
		n += int64(len(b))
	}
	c.fd.mode = "writeBuffers"
	return n, nil
}

// TCPConn mirrors net.TCPConn: EXPORTED, and it PROMOTES writeBuffers through the embedded conn.
type TCPConn struct {
	conn
}

// plainConn implements only io.Writer, so the assertion must MISS and the fallback must run.
type plainConn struct {
	fd *sink
}

func (c *plainConn) ok() bool { return c != nil && c.fd != nil }

func (c *plainConn) Write(b []byte) (int, error) {
	if !c.ok() {
		return 0, fmt.Errorf("invalid conn")
	}
	c.fd.bytes = append(c.fd.bytes, b...)
	c.fd.mode = "write"
	return len(b), nil
}

// PlainConn promotes ONLY Write, so the exported struct must miss the assertion too.
type PlainConn struct {
	plainConn
}

// valueSink satisfies the unexported interface on its VALUE method set — the control that already
// worked, since a value-receiver method is promoted by the other (ungated) path.
type valueSink struct {
	fd *sink
}

func (s valueSink) Write(b []byte) (int, error) {
	s.fd.bytes = append(s.fd.bytes, b...)
	s.fd.mode = "write"
	return len(b), nil
}

func (s valueSink) writeBuffers(v *Buffers) (int64, error) {
	var n int64
	for _, b := range *v {
		s.fd.bytes = append(s.fd.bytes, b...)
		n += int64(len(b))
	}
	s.fd.mode = "writeBuffers"
	return n, nil
}

// ValueSink promotes the value method set through a value embed.
type ValueSink struct {
	valueSink
}

func report(label string, s *sink, n int64, err error) {
	fmt.Println(label, n, err, s.mode, string(s.bytes))
}

func main() {
	bufs := Buffers{[]byte("go"), []byte("2"), []byte("cs")}

	// Promoted through one value embed, exported target type — net's own shape.
	tcpSink := &sink{}
	tcp := &TCPConn{conn{fd: tcpSink}}
	var w io.Writer = tcp
	n, err := bufs.WriteTo(w)
	report("TCPConn", tcpSink, n, err)

	// The unexported type declaring the method directly — no promotion involved.
	connSink := &sink{}
	c := &conn{fd: connSink}
	n, err = bufs.WriteTo(c)
	report("conn", connSink, n, err)

	// Value method set, promoted through a value embed.
	vsSink := &sink{}
	vs := ValueSink{valueSink{fd: vsSink}}
	n, err = bufs.WriteTo(vs)
	report("ValueSink", vsSink, n, err)

	// No writeBuffers anywhere: the assertion must MISS and the per-chunk fallback must run.
	plainSink := &sink{}
	p := &PlainConn{plainConn{fd: plainSink}}
	n, err = bufs.WriteTo(p)
	report("PlainConn", plainSink, n, err)

	// The asserted value is usable as the interface it asserted to.
	if wv, ok := w.(buffersWriter); ok {
		more := Buffers{[]byte("!")}
		n, _ = wv.writeBuffers(&more)
		fmt.Println("direct", n, string(tcpSink.bytes))
	} else {
		fmt.Println("direct", "miss")
	}
}
