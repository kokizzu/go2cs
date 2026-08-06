// Guards the C# member-vs-enclosing-type name collision (CS0542) for an EMBEDDED field.
//
// An embedded field's name is the unqualified type name (Go spec), so `type Buffer struct {
// inner.Buffer }` derives the member `Buffer` inside the struct `Buffer` — which C# forbids. The
// converter already renamed such a member in the NAMED-field path (and every ACCESS site already
// emitted the renamed form, `rb.of(Buffer.ᏑΔBuffer)`), but the embedded-field DECLARATION path
// emitted the bare name, so the declaration and its accesses disagreed. Real shape: io's own
// io_test.go declares `type Buffer struct { bytes.Buffer; ReaderFrom; WriterTo }`.
//
// Both halves must keep working after the rename: the EXPLICIT field access (`b.Buffer`) and the
// PROMOTED members reached through it (the generated field accessors and their `Ꮡ` references,
// and the pointer-receiver box descent), plus a composite literal keyed by the embedded field.
//
// A value-receiver method is called through the explicit selector (`b.Buffer.Describe()`) rather
// than by promotion: a cross-package VALUE embed's value-receiver method gets no promoted
// forwarder today (the TypeGenerator can only harvest same-compilation struct declarations,
// CS1929) — an unrelated pre-existing gap, reproduced identically with the names made distinct, so
// this guard deliberately stays clear of it.
package main

import (
	"fmt"

	"EmbeddedTypeNameCollision/inner"
)

// Buffer embeds a foreign type of the same name.
type Buffer struct {
	inner.Buffer
	Tag string
}

// Renamed reads the embed through the EXPLICIT selector in a signature position.
func Renamed(b Buffer) string {
	return fmt.Sprintf("%s/%s/%d", b.Tag, b.Buffer.Data, b.Buffer.N)
}

func main() {
	// Composite literal keyed by the embedded field.
	b := Buffer{Buffer: inner.NewBuffer("alpha", 3), Tag: "tagged"}

	// Explicit field access, then the promoted FIELDS, then a value-receiver method through
	// the explicit selector.
	fmt.Println(b.Buffer.Data, b.Buffer.N)
	fmt.Println(b.Data, b.N)
	fmt.Println(b.Buffer.Describe())
	fmt.Println(Renamed(b))

	// Promoted POINTER-receiver method mutating through the embed; read back through the SAME
	// pointer via both the explicit selector and the promotion.
	p := &b
	p.Bump()
	p.Bump()
	fmt.Println(p.Buffer.N, p.N, p.Buffer.Describe())

	// Write through the explicit selector, read back through the promotion.
	p.Buffer.Data = "beta"
	p.Append("!")
	fmt.Println(p.Data, p.Buffer.N, p.Buffer.Describe())

	// A zero value's promoted box must still resolve.
	z := new(Buffer)
	z.Append("gamma")
	z.Tag = "fresh"
	fmt.Println(z.Buffer.Describe(), z.Buffer.N, Renamed(*z))
}
