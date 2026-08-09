// Guards the two go2cs-gen defects that kept debug/dwarf at 30 of 40 (r56g).
//
// ROOT 1 - a value embed's POINTER-receiver methods belong to the outer type's POINTER
// method set, whatever the embed's exportedness. TypeGenerator promoted them only for an
// UNEXPORTED embed, which is not a Go rule; and because golib reconstructs a Go method set
// at run time by reading the EMITTED extension methods, an unemitted promotion is an ABSENT
// Go method. So *Uint (value-embedding the EXPORTED Base, whose Basic() takes a pointer
// receiver) did not satisfy the anonymous interface{ Basic() *Base } that dwarf's readType
// asserts to - the exact shape below, down to holding the value as a DIFFERENT named
// interface at the assertion site so no compile-time witness can exist.
//
// ROOT 2 - ImplementGenerator detected an embedded INTERFACE field by NAME (field name
// equals its interface type's simple name), which cannot tell a Go embedded interface from
// an ordinary named field whose name merely equals its type's. dwarf has both in one struct
// (type PtrType struct { CommonType; Type Type }), so Common() forwarded through the FIELD
// and returned the REFERENCED node's CommonType instead of the receiver's own - a silent
// wrong answer when the field was non-nil, a nil dereference when it was not. Ptr below
// reproduces that shape and checks the VALUE, not merely the absence of a crash.
package main

import "fmt"

// CommonType is the exported embed every node type carries, with a POINTER-receiver method.
// Named as dwarf names it: the embedded FIELD name must differ from the promoted METHOD
// name, or Go's own selector rules shadow the method with the field.
type CommonType struct {
	ByteSize int64
	Label    string
}

func (c *CommonType) Common() *CommonType { return c }

func (c *CommonType) String() string { return c.Label }

// Node is the named interface the values are HELD as at every assertion site below. The
// anonymous interfaces are asserted FROM a Node, never from a concrete type, so the
// converter's recorders cannot mint a witness for the pair and the run-time tier must answer.
type Node interface {
	Common() *CommonType
	String() string
}

// Base is an EXPORTED struct embedded BY VALUE, whose Basic() takes a POINTER receiver.
type Base struct {
	CommonType
	BitSize int64
}

func (b *Base) Basic() *Base { return b }

// Uint and Int reach Basic() only through the promoted *Base method, and reach
// Common()/String() only through the two-level promotion Base -> CommonType.
type Uint struct {
	Base
}

type Int struct {
	Base
}

// Ptr reproduces dwarf's PtrType: a real depth-1 struct embed AND a NAMED field whose name
// equals its interface type's simple name. Only the embed promotes Common() in Go; the
// `Node` field is an ordinary field and promotes nothing.
type Ptr struct {
	CommonType
	Node Node
}

func main() {
	// --- ROOT 1: anonymous interface satisfied by a method promoted from an EXPORTED value embed.
	u := &Uint{}
	u.Label = "uint"
	var n Node = u

	basic, ok := n.(interface{ Basic() *Base })
	fmt.Println("uint asserts to anon Basic:", ok)

	if ok {
		// The promoted method must ALIAS the embedded storage, not hand back a copy: dwarf's
		// readType writes the name and bit size through exactly this returned pointer and then
		// reads them back through the interface value.
		b := basic.Basic()
		b.BitSize = 64
		b.Label = "uint64"
	}

	fmt.Println("uint after write through promoted pointer:", u.BitSize, u.Label, n.String())

	// A second concrete type through the same lifted interface, to prove the promotion is not
	// one-off - dwarf reaches this path for IntType, UintType, CharType and UcharType alike.
	i := &Int{}
	i.Label = "int"
	var ni Node = i
	if bi, okI := ni.(interface{ Basic() *Base }); okI {
		bi.Basic().BitSize = 32
	}
	fmt.Println("int after write through promoted pointer:", i.BitSize, i.Label)

	// A type switch over the same lifted shape takes the identical run-time path.
	switch t := n.(type) {
	case interface{ Basic() *Base }:
		fmt.Println("type switch matched anon Basic, bit size:", t.Basic().BitSize)
	default:
		fmt.Println("type switch matched default")
	}

	// --- Method-set NARROWING must survive the fix: a pointer-receiver method promoted from a
	// value embed enters the method set of *Uint ONLY. A Uint VALUE must still miss, or the fix
	// would have widened a method set Go keeps narrow.
	var anyValue any = Uint{}
	_, valueOK := anyValue.(interface{ Basic() *Base })
	fmt.Println("uint VALUE asserts to anon Basic:", valueOK)

	// --- ROOT 2: the named field must not be mistaken for an embed.
	inner := &Uint{}
	inner.Label = "inner"
	inner.ByteSize = 8

	p := &Ptr{Node: inner}
	p.Label = "ptr"
	p.ByteSize = 4

	var np Node = p
	// Common() must come from the embedded CommonType, NOT from the `Node` field: "ptr"/4,
	// never "inner"/8.
	fmt.Println("ptr Common through embed:", np.Common().Label, np.Common().ByteSize)
	fmt.Println("ptr String through embed:", np.String())

	// With a nil field the promoted Common() must still answer from the embed - this is the
	// case that used to nil-dereference.
	empty := &Ptr{}
	empty.Label = "empty"
	empty.ByteSize = 2
	var ne Node = empty
	fmt.Println("ptr Common with nil field:", ne.Common().Label, ne.Common().ByteSize)

	// Writing through the pointer Common() returns must reach the receiver's own storage.
	ne.Common().ByteSize = 16
	fmt.Println("ptr after write through Common:", empty.ByteSize)

	// The Ptr still routes to its own field explicitly, proving the field is reachable as a
	// plain field even though it promotes nothing.
	fmt.Println("ptr field node:", p.Node.Common().Label, p.Node.String())
}
