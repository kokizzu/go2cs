// The interfaces live in a SEPARATE package on purpose. When the asserted interface is declared
// in the same package as the type, the converter records the pair from the package's own
// satisfaction scan and the assertion works; the defect this test guards only appears when the
// interface is FOREIGN, which is the shape the resolver hit (a user type embedding net.Conn,
// asserted to net.PacketConn inside net).
package iolike

// Reader is the interface the wrapper EMBEDS.
type Reader interface {
	Read() string
}

// ReadWriter's method set spans both sources: Read arrives by promotion from the embedded
// interface, Write is declared directly on the asserting type.
type ReadWriter interface {
	Read() string
	Write(s string) string
}

// Base gives the embedded interface something to hold.
type Base struct{ Tag string }

func (b Base) Read() string { return "read:" + b.Tag }
