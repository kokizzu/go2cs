// Guards interface satisfaction that comes ONLY from methods promoted through SEVERAL embedded
// POINTER fields — net/rpc/jsonrpc's `type pipe struct { *io.PipeReader; *io.PipeWriter }`, whose
// Read/Write are promoted and whose Close is declared on *pipe itself.
//
// The interface-implementation generator resolved a promoted member through an embedded pointer
// only when the struct had EXACTLY ONE such embed; with two, every promoted member fell back to
// the bare `m_box.Read(p)` / `this.Read(p)` receiver, which binds nothing on the struct and lets
// C# overload resolution reach an unrelated same-named extension elsewhere in scope — CS1929
// naming `io_package.Read(ref io_package.LimitedReader, slice<byte>)` from a jsonrpc test. Each
// promoted member must instead route to the UNIQUE embed declaring it, exactly as Go's depth-1
// promotion-ambiguity rule resolves it.
//
// Covered here: LOCAL embeds (direct-ж primary AND ref-receiver forms), FOREIGN embeds resolved
// from another assembly's metadata (*strings.Reader / *strings.Builder), a member both embeds
// declare that the struct itself overrides (Close), the POINTER-sourced interface cast (adapter
// class) and the VALUE-sourced one (partial-struct impl), plus write-through aliasing to prove
// the forwarders reach the ORIGINAL embedded objects rather than a copy.
package main

import (
	"fmt"
	"strings"
)

type reader struct {
	src string
	pos int
}

// The receiver ESCAPES (the address of a receiver field is taken), so the converter emits this
// as a direct-ж primary — a promoted forwarder must bind the embedded ж field ITSELF, since
// deref'ing to the value first strands the extension receiver.
func (r *reader) Read(p []byte) (int, error) {
	q := &r.pos
	if *q >= len(r.src) {
		return 0, nil
	}
	n := copy(p, r.src[*q:])
	*q += n
	return n, nil
}

func (r *reader) Close() error {
	r.pos = len(r.src)
	return nil
}

type writer struct {
	out   []byte
	flush int
}

// A plain pointer receiver — a [GoRecv] ref extension whose ж-twin the receiver generator mints.
func (w *writer) Write(p []byte) (int, error) {
	w.out = append(w.out, p...)
	return len(p), nil
}

func (w *writer) Close() error {
	w.flush++
	return nil
}

func (w *writer) String() string {
	return string(w.out)
}

// duplex satisfies readWriteCloser with NO method of its own except Close: Read is promoted from
// *reader, Write from *writer. Close is declared by BOTH embeds — ambiguous at depth 1, so Go
// promotes neither — and the method declared here is what satisfies the interface.
type duplex struct {
	*reader
	*writer
}

func (d *duplex) Close() error {
	if err := d.reader.Close(); err != nil {
		return err
	}
	return d.writer.Close()
}

type readWriteCloser interface {
	Read(p []byte) (int, error)
	Write(p []byte) (int, error)
	Close() error
}

// A pointer embed's method set promotes into the STRUCT's method set as well, so the VALUE form
// satisfies this interface too — a distinct emission path (the partial-struct implementation
// rather than the pointer adapter) with the same promotion to resolve.
type readWriter interface {
	Read(p []byte) (int, error)
	Write(p []byte) (int, error)
}

// The same shape with FOREIGN embeds: Read lives on *strings.Reader and WriteString on
// *strings.Builder, both in another assembly, so the promoted member's receiver form is
// recoverable only from that assembly's METADATA. (Len and Reset are declared by both embeds,
// hence promoted by neither — they are never named unqualified below.)
type foreign struct {
	*strings.Reader
	*strings.Builder
}

type readStringWriter interface {
	Read(p []byte) (int, error)
	WriteString(s string) (int, error)
}

func main() {
	r := &reader{src: "hello world"}
	w := &writer{}
	d := &duplex{r, w}

	// POINTER-sourced cast: the interface value aliases *duplex, so every promoted call must
	// land on the ORIGINAL reader/writer.
	var rwc readWriteCloser = d

	buf := make([]byte, 5)
	n, err := rwc.Read(buf)
	fmt.Println("read:", n, string(buf[:n]), err == nil)

	n, err = rwc.Write(buf[:n])
	fmt.Println("write:", n, err == nil)

	// Promotion reached the embedded objects themselves, not copies of them.
	fmt.Println("aliased:", r.pos, w.String())

	// Close is the struct's own method, overriding a promotion both embeds declare.
	fmt.Println("close:", rwc.Close() == nil, r.pos, w.flush)

	// VALUE-sourced cast of the very same struct: the embedded POINTERS are copied, so writes
	// still reach the original reader/writer.
	var rw readWriter = *d
	n, _ = rw.Write([]byte("!"))
	fmt.Println("value form:", n, w.String())

	// FOREIGN embeds, resolved from metadata.
	f := &foreign{strings.NewReader("abc"), &strings.Builder{}}
	var rsw readStringWriter = f

	fbuf := make([]byte, 3)
	n, _ = rsw.Read(fbuf)
	n2, _ := rsw.WriteString(string(fbuf[:n]) + "def")
	fmt.Println("foreign:", n, n2, f.Builder.String())

	// The VALUE form of that same foreign shape — a distinct emission path (the partial-struct
	// implementation) resolving the identical promotion.
	var frsw readStringWriter = *f
	n3, _ := frsw.WriteString("gh")
	fmt.Println("foreign value form:", n3, f.Builder.String())
}
