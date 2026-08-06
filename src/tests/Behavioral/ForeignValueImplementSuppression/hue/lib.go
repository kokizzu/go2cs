// hue plays image/color: it declares an interface, VALUE-receiver implementers, AND — unlike
// ValueAdapterDynamicType's colorlike — it converts them to that interface ITSELF. That self
// conversion is what writes the VALUE-form `[assembly: GoImplement<T, Iface>]` records into this
// package's metadata, so its own assembly really does implement the pairs and an importer needs no
// local ᴠ value adapter for them.
//
// Shade deliberately carries a METHOD of its own name (image/color's `func (c RGBA) RGBA()`), which
// is what makes go2cs collision-rename the emitted type — the record then names ΔShade while the Go
// type is Shade, one of the two spellings the suppression key has to reconcile.
package hue

// Tint plays color.Color.
type Tint interface {
	Shade() (l, a uint32)
}

// Shade plays color.RGBA: a comparable struct whose method name collides with its type name.
type Shade struct {
	L, A uint8
}

func (s Shade) Shade() (uint32, uint32) {
	return uint32(s.L) * 0x101, uint32(s.A) * 0x101
}

// Gray is a second implementer, so no fix can be a one-type special case.
type Gray struct {
	Y uint8
}

func (g Gray) Shade() (uint32, uint32) {
	y := uint32(g.Y) * 0x101
	return y, 0xffff
}

// Tinter plays net/http's HandlerFunc: a named FUNC type with a method. The pair is recorded here
// exactly like the struct pairs above, but a C# delegate cannot be a partial struct — go2cs-gen
// realizes THIS one as an adapter class inside this assembly. An importer that trusted the record
// would hand a bare delegate to an interface slot (CS0029), so it must keep its own adapter.
type Tinter func() (l, a uint32)

func (f Tinter) Shade() (uint32, uint32) { return f() }

// These package-level conversions are the self-conversion sites that produce the records.
var (
	Default     Tint = Shade{L: 1, A: 2}
	DefaultGray Tint = Gray{Y: 4}
	DefaultFunc Tint = Tinter(func() (uint32, uint32) { return 8, 9 })
)

// Describe converts through the interface inside the declaring package, the same shape image's
// `func (p *NRGBA) At(x, y int) color.Color` takes.
func Describe(s Shade) Tint { return s }
