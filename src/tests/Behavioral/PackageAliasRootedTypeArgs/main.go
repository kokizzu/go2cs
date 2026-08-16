// Regression test: a PACKAGE-LEVEL type alias whose target is anything but a single already-
// qualified name — every type ARGUMENT it nests has to be namespace-rooted in the emitted
// `global using`.
//
// go2cs emits `type X = T` as `global using X = <T>;`, and C# resolves a using alias's target
// "as if the immediately containing compilation unit had no using directives". That is
// COMPILATION scope: outside the file's `namespace go;`, outside the emitted `<pkg>_package`
// class, and with none of the csproj-level golib aliases in effect. Only the OUTERMOST name was
// rooted, so every argument under it named nothing at all — `global using names =
// go.slice<@string>;` is CS0246 on `@string`, and the same for `slice`, `error`, `complex64`, a
// same-package `Header` (which is `go.main_package.Header` there), a cross-package
// `io_package.Reader` (`go.io_package.Reader`), and the BCL `Func`/`Action` of a func-type alias
// (`System.Func`, since `System` is not in scope either). Nine aliases produced 17 CS0246.
//
// This is user-code-facing rather than corpus-facing: the whole standard library declares only
// four package-level aliases with type arguments, and all four are `[N]uint64` whose argument is
// a C# keyword. An end-user package that aliases a slice, map, channel or func type — the common
// `type Handlers = map[string]Handler` shape — hit it immediately.
//
// The aliases below cover each arm: golib element types, keyword element types (which must NOT
// be rooted — they are csproj aliases standing for C# keywords, so they map to the keyword
// instead), same-package named types, a lifted anonymous struct and interface, a cross-package
// interface, both directional channel forms, the two delegate spellings, nesting two deep, and
// an alias whose target is ITSELF an alias (which must resolve through, since a using alias may
// not name another using alias).
package main

import (
	"fmt"
	"io"
)

// A same-package named struct and interface, so an alias can nest one. At compilation scope
// these are `go.main_package.Header` / `go.main_package.Stringish`, never a bare name.
type Header struct {
	Name string
	Size int64
}

func (h Header) String() string { return fmt.Sprint(h.Name, "/", h.Size) }

type Stringish interface{ String() string }

type (
	// golib element types: @string, error, complex64, and the slice/map/array/channel
	// constructors themselves when they nest.
	names = []string
	grid  = [][]string
	errs  = []error
	cplx  = []complex64
	chans = chan string
	arr   = [3]string

	// csproj-alias element types, which resolve to a C# keyword or BCL type and so must be
	// substituted rather than rooted (uint8 → byte, int64 → long, complex128 →
	// System.Numerics.Complex, any → object).
	sizes = map[string]int64
	u8s   = []uint8
	ifs   = []any
	cplx2 = []complex128

	// Same-package named types, at one and two levels of nesting.
	hdrs   = []Header
	hmap   = map[string]Header
	nested = map[string][]Header
	ptrs   = []*Header
	iface  = []Stringish
	sends  = chan<- Header
	recvs  = <-chan Header

	// A cross-package interface: already package-qualified, but the ROOT is still missing.
	rdrs = []io.Reader

	// Func-type aliases: the BCL delegates live in System, which the alias RHS cannot see.
	fn  = func(string) int
	fn2 = func(Header) (string, error)
	fn0 = func()

	// A lifted anonymous struct / interface target, and an alias to a same-package named type,
	// all of which live inside the package class.
	anon   = struct{ A int }
	anonI  = interface{ Zed() int }
	direct = Header

	// An alias whose target is another alias. A C# using alias may not reference another using
	// alias, so this has to render what `hdrs` RESOLVES to, not the name `hdrs`.
	aliasOfAlias = hdrs
)

// zed satisfies anonI so the lifted anonymous interface is actually used.
type zed struct{ v int }

func (z zed) Zed() int { return z.v }

func main() {
	var a names = names{"x", "y"}
	var b sizes = sizes{"k": 7}
	var c grid = grid{{"g"}}
	var d errs = errs{nil}
	var e cplx = cplx{complex(1, 2)}
	var e2 cplx2 = cplx2{complex(3, 4)}
	var f chans = make(chans, 1)
	var g arr = arr{"a", "b", "c"}
	var h hdrs = hdrs{{Name: "n", Size: 1}}
	var i rdrs = rdrs{nil}
	var j ptrs = ptrs{nil}
	var k fn = func(s string) int { return len(s) }
	var k2 fn2 = func(x Header) (string, error) { return x.Name, nil }
	var k0 fn0 = func() { fmt.Println("fn0 ran") }
	var l nested = nested{"z": {{Name: "q", Size: 2}}}
	var m hmap = hmap{"z": {Name: "hm", Size: 3}}
	var n u8s = u8s{7, 8}
	var o ifs = ifs{1, "two"}
	var p direct = direct{Name: "dir", Size: 4}
	var q aliasOfAlias = aliasOfAlias{{Name: "aoa", Size: 5}}
	var r anon = anon{A: 9}
	var s iface = iface{Header{Name: "if", Size: 6}}
	var t anonI = zed{v: 11}

	// A directional alias narrows the channel it is assigned from, so the readable end stays
	// the bidirectional variable — the alias is what has to render, not what has to be read.
	sendCh := make(chan Header, 1)
	var send sends = sendCh
	recvCh := make(chan Header, 1)
	recvCh <- Header{Name: "recv", Size: 12}
	var recv recvs = recvCh

	f <- "ch"
	send <- Header{Name: "send", Size: 13}

	k0()
	name, err := k2(Header{Name: "k2", Size: 14})

	fmt.Println("names:", a, "sizes:", b, "grid:", c, "errs:", d)
	fmt.Println("cplx:", e, "cplx2:", e2, "chans:", <-f, "arr:", g)
	fmt.Println("hdrs:", h, "rdrs:", i, "ptrs:", j, "fn:", k("abcd"))
	fmt.Println("fn2:", name, err, "nested:", l, "hmap:", m)
	fmt.Println("u8s:", n, "ifs:", o, "direct:", p, "aliasOfAlias:", q)
	fmt.Println("anon:", r, "iface:", s, "anonI:", t.Zed())
	fmt.Println("sends:", <-sendCh, "recvs:", <-recv)
}
