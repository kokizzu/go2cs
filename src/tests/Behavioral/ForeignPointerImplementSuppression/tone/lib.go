// tone plays text/template/parse and image: it declares an interface, POINTER-receiver
// implementers, and it converts them to that interface ITSELF. Those self-conversions are what
// write the `[assembly: GoImplement<T, Iface>(Pointer = true)]` records into this package's
// metadata, and each such record means one thing: this assembly already generated the public
// `TжIface` adapter class. An importer converting the same `*T` must reference THAT class rather
// than mint a second one.
package tone

// Level plays parse.Node / image.Image — satisfied on the POINTER, so the interface value has to
// hold the receiver box itself if a write through it is to be visible on the original.
type Level interface {
	Tone() int
	Name() string
	Set(d int)
}

// Tone plays image.RGBA: a type whose METHOD shares its name, so go2cs collision-renames the
// emitted type to ΔTone. That makes the record name ΔTone while the Go type is Tone — and it also
// makes a cross-package reference to the type resolve through a whole-TYPE `global using` alias,
// which is an identifier and not a path. The adapter must not be composed onto that alias.
type Tone struct{ D int }

func (t *Tone) Tone() int    { return t.D }
func (t *Tone) Name() string { return "tone" }
func (t *Tone) Set(d int)    { t.D = d }

// Plain is the ordinary, non-renamed implementer — the parse.ListNode shape, where only the
// INTERFACE side of the record key ever diverged.
type Plain struct{ D int }

func (p *Plain) Tone() int    { return p.D * 10 }
func (p *Plain) Name() string { return "plain" }
func (p *Plain) Set(d int)    { p.D = d }

// Lone is the NEGATIVE that keeps the fix from over-widening: it satisfies Level exactly as the
// two above do, but this package never converts one, so no record is written and an importer has
// nothing to reference — it must still mint its own local adapter. (encoding/binary's ByteOrder is
// the corpus instance of a pair a package satisfies but never records.)
type Lone struct{ D int }

func (l *Lone) Tone() int    { return l.D * 100 }
func (l *Lone) Name() string { return "lone" }
func (l *Lone) Set(d int)    { l.D = d }

// The self-conversion sites that write the records.
var (
	Default      Level = &Tone{D: 3}
	DefaultPlain Level = &Plain{D: 4}
)

// Describe converts inside the DECLARING package — the shape image's `func (p *NRGBA) At(...)
// color.Color` takes. An interface value it produces and one an importer produces must be the
// same value, which is only true if both name one adapter over one box.
func Describe(t *Tone) Level { return t }
