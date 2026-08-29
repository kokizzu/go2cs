// addrlib reproduces syscall.SockaddrUnix's shape: a MIXED-VISIBILITY struct whose exported
// field is what a cross-package composite literal sets, and whose UNEXPORTED field needs
// construction in C# (its own type carries a fixed array, or a promoted embed box).
//
// The C# emission gives such a struct two field-wise constructors -- a PUBLIC one over the
// exported members only, and an INTERNAL one over all of them. A same-package literal binds the
// internal one (the public subset is deprioritized precisely so it does), so the defect this
// guards is invisible from inside the package: only a literal in ANOTHER package selects the
// public subset ctor, which named the unexported members nowhere and therefore left them at
// `default(T)` -- a zero-length `array<T>` backing rather than Go's fixed `[N]T`.
//
// syscall.SockaddrUnix is the shipped case: `&SockaddrUnix{Name: path}` from net left `raw`
// default, so `len(sa.raw.Path)` answered 0 and syscall's own `if n > len(sa.raw.Path)` guard
// returned EINVAL before bind ever reached the kernel -- every AF_UNIX listen/dial on Windows
// failed with "invalid argument".
package addrlib

// PathMax mirrors UNIX_PATH_MAX's role: the nested array's fixed length is the value the
// enclosing struct's guard compares against.
const PathMax = 12

// rawAddr is the needy type -- a fixed array is what `default(rawAddr)` leaves unusable.
type rawAddr struct {
	Family uint16
	Path   [PathMax]int8
}

// Addr is syscall.SockaddrUnix's shape exactly: an exported field plus an unexported needy one.
type Addr struct {
	Name string
	raw  rawAddr
}

// Capacity reports the nested fixed array's length. Go always answers PathMax; the defect
// answered 0.
func (a *Addr) Capacity() int {
	return len(a.raw.Path)
}

// Encode is syscall's sockaddr() in miniature -- it VALIDATES against the nested array's length
// before filling it, which is how a zero-length backing turns into a bogus "too long" rejection
// rather than a crash.
func (a *Addr) Encode() (int, bool) {
	n := len(a.Name)
	if n > len(a.raw.Path) {
		return 0, false
	}
	a.raw.Family = 1
	for i := 0; i < n; i++ {
		a.raw.Path[i] = int8(a.Name[i])
	}
	return 2 + n + 1, true
}

// PathByte reads back through the nested array, proving the fill landed in real storage.
func (a *Addr) PathByte(i int) int8 {
	return a.raw.Path[i]
}

// slots is the second needy kind: reached through an unexported EMBED, whose C# promoted-box
// field is allocated by a constructor rather than by a field initializer.
type slots struct {
	Cells [4]int32
}

// Embedder pairs an exported field with an unexported embedded struct, so the public subset
// constructor omits the promoted box.
type Embedder struct {
	Name string
	slots
}

// Slots reads the promoted array's length through the embed.
func (e *Embedder) Slots() int {
	return len(e.Cells)
}

// Put writes and reads back through the promoted embed.
func (e *Embedder) Put(i int, v int32) int32 {
	e.Cells[i] = v
	return e.Cells[i]
}
