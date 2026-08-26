// ConstraintProxyEmbeddedInterface guards the self-referential constraint proxy's
// EMBEDDED-interface walk.
//
// A Go constraint interface may embed other interfaces:
//
//	type Constrained[T any] interface { Middle; Clone() T }
//	type Middle             interface { Base;   Size() int }
//	type Base               interface { Name() string }
//
// go2cs-gen mints a proxy type (`ImplжConstrained`) that satisfies such a constraint by
// wrapping the box and implementing the interface over ITSELF. It emitted one forwarder per
// DIRECTLY DECLARED member only, so every member reached through an embedded interface was
// missing and the proxy did not implement its own interface (CS0535, one per inherited
// member). net/http's whole test suite sits behind this: `clientserver_test.go` declares
// `type TBRun[T any] interface { testing.TB; Run(string, func(T)) bool }`, and the proxies for
// *testing.T and *testing.B were each missing all 18 members of the embedded testing.TB.
//
// Two levels of embedding are deliberate — Base through Middle through Constrained — so the
// walk is pinned as TRANSITIVE, not merely one-deep.
package main

import "fmt"

type Base interface {
	Name() string
}

// Middle embeds Base and adds its own member.
type Middle interface {
	Base
	Size() int
}

// Constrained is the self-referential constraint: T appears in Clone's result, which is what
// forces the proxy (a bare box cannot satisfy a bound that names the type itself).
type Constrained[T any] interface {
	Middle
	Clone() T
}

type Impl struct {
	n string
	s int
}

func (p *Impl) Name() string { return p.n }
func (p *Impl) Size() int    { return p.s }
func (p *Impl) Clone() *Impl { return &Impl{p.n, p.s + 1} }

// use is instantiated at T = *Impl, which is the site that records the constraint proxy.
func use[T Constrained[T]](v T) {
	c := v.Clone()
	// Name() arrives through TWO levels of embedding, Size() through one, Clone() directly.
	fmt.Println(c.Name(), c.Size(), v.Name(), v.Size())
}

// second records the SAME (element, open-interface) pair at a second instantiation site, so the
// emitter's per-pair de-duplication is exercised: exactly one proxy must still be emitted. Its
// result is built inside the generic context deliberately — a T returned out to concrete code
// arrives as the proxy TYPE, whose forwarders are explicit interface implementations and so are
// not reachable by member lookup there. That is a separate surface from the embedded walk and is
// kept out of this guard on purpose.
func second[T Constrained[T]](v T) string {
	c := v.Clone()
	return fmt.Sprint(c.Name(), "/", c.Size(), "/", v.Name(), "/", v.Size())
}

func main() {
	use(&Impl{"alpha", 1})
	use(&Impl{"beta", 10})

	fmt.Println(second(&Impl{"gamma", 100}))
}
