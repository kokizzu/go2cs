// colorlike plays image/color: it declares an EXPORTED interface plus VALUE-receiver implementers,
// but never converts one to the other itself — no `var _ Color = NRGBA{}` witness, no in-package
// cast. Go satisfies an interface structurally, so this assembly does implement both pairs; it
// simply had no cast for the converter to record from, and the CONSUMING assembly used to mint a
// value adapter for each. The declaring side now records the pairs it satisfies, so the two
// `[assembly: GoImplement]` lines in this package's metadata are the whole fix, and the consumer
// hands over the bare value.
package colorlike

// Color plays color.Color.
type Color interface {
	RGBA() (r, g, b, a uint32)
}

// NRGBA plays color.NRGBA — a comparable struct with a VALUE receiver.
type NRGBA struct {
	R, G, B, A uint8
}

func (c NRGBA) RGBA() (uint32, uint32, uint32, uint32) {
	return uint32(c.R) * 0x101, uint32(c.G) * 0x101, uint32(c.B) * 0x101, uint32(c.A) * 0x101
}

// Gray is a second implementer, so a type switch has a live alternative to pick between.
type Gray struct {
	Y uint8
}

func (c Gray) RGBA() (uint32, uint32, uint32, uint32) {
	y := uint32(c.Y) * 0x101
	return y, y, y, 0xffff
}
