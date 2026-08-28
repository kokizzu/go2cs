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
	// A member whose parameter is a DELEGATE over the SELF type — net/http's own constraint is
	// `Run(string, func(T)) bool`. The proxy declares it as `Action<ImplжConstrained>` while the box
	// extension its body forwards to declares `Action<ж<Impl>>`, and the two implicit conversions
	// that marshal a bare T argument for free cannot lift through a delegate: C# variance requires a
	// REFERENCE conversion and does not apply a user-defined one inside `Action<>`/`Func<>`. The
	// forwarder then binds nothing and reports CS1929 against the ref-receiver overload — an error
	// naming the RECEIVER when it is the argument that does not fit.
	Each(label string, f func(T)) bool
	// The self type in a delegate's RESULT position — the same boundary crossed the other way, so a
	// remedy that marshalled only the inbound direction cannot pass this guard.
	Pick(gen func() T) T
}

type Impl struct {
	n string
	s int
}

func (p *Impl) Name() string { return p.n }
func (p *Impl) Size() int    { return p.s }
func (p *Impl) Clone() *Impl { return &Impl{p.n, p.s + 1} }

func (p *Impl) Each(label string, f func(*Impl)) bool {
	f(&Impl{p.n + "/" + label, p.s})
	return true
}

func (p *Impl) Pick(gen func() *Impl) *Impl { return gen() }

// use is instantiated at T = *Impl, which is the site that records the constraint proxy.
func use[T Constrained[T]](v T) {
	c := v.Clone()
	// Name() arrives through TWO levels of embedding, Size() through one, Clone() directly.
	fmt.Println(c.Name(), c.Size(), v.Name(), v.Size())

	// The delegate-typed self boundary, both directions, called THROUGH the constraint.
	ok := v.Each("leaf", func(child T) {
		fmt.Println("each", child.Name(), child.Size())
	})

	picked := v.Pick(func() T { return v.Clone() })

	fmt.Println(ok, picked.Name(), picked.Size())
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

// callback exercises the T-boundary at a LAMBDA PARAMETER — the position net/http's
// `run[T TBRun[T]](t T, f func(t T, mode testMode), opts ...any)` uses everywhere. At
// T = ImplжConstrained the C# delegate is `Action<ImplжConstrained, nint>`, and a lambda PARAMETER
// declaration must match the delegate exactly (C# applies no user-defined conversion there), so
// rendering the natural box `ж<Impl>` is CS1678 + CS1661 — one pair per call site, 48 of net/http's
// 81. The callback BODY then calls constraint members on that parameter, which is the second half
// of the same boundary: the parameter is a concrete proxy there, not a type parameter, so the
// members have to be reachable by ordinary lookup.
func callback[T Constrained[T]](v T, f func(t T, mode int)) {
	f(v, 7)
	f(v.Clone(), 8)
}

func main() {
	use(&Impl{"alpha", 1})
	use(&Impl{"beta", 10})

	fmt.Println(second(&Impl{"gamma", 100}))

	callback(&Impl{"delta", 1000}, func(t *Impl, mode int) {
		fmt.Println(t.Name(), t.Size(), mode)
	})

	// The SHADOWED spelling, which is the one net/http actually has: its
	// `run(t, func(t *testing.T, mode testMode){…})` inner `t` shadows the outer `t` and so
	// shadow-renames to `tΔ1`. A literal's signature is generated from synthesized vars carrying
	// the RENDERED name, so a proxy map keyed by the Go name misses every renamed parameter — and
	// misses it ASYMMETRICALLY, since the body prologue keys off the same map: the first cut
	// emitted the prologue while leaving the declaration at the natural type, producing a
	// same-named local beside the parameter. Both halves must key on the rendered name.
	t := "outer"

	callback(&Impl{"epsilon", 2000}, func(t *Impl, mode int) {
		fmt.Println(t.Name(), t.Size(), mode)
	})

	fmt.Println(t)
}
