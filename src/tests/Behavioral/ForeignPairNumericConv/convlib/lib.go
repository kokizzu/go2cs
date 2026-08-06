// convlib plays syscall in the foreign-pair conversion guard: it declares TWO named numeric types
// and never converts between them itself, so no GoImplicitConv record exists in its own metadata.
package convlib

type Handle uintptr

type Token uintptr

func NewToken(v uintptr) Token { return Token(v) }

func (h Handle) String() string { return "handle" }

func (t Token) Raw() uintptr { return uintptr(t) }
