// DerivedInterfaceStructuralProbe guards golib's structural method-set probe
// (TypeExtensions.GetInterfaceMethods, consumed by StructurallyImplements) against the
// BASE-INTERFACE STATIC LEAK: the base-interface walk collected members with reflection's
// DEFAULT BindingFlags, which include STATICS. golib's hand-written core interfaces expose
// static duck-typing conversion helpers (error.As<T>, fmt.Stringer.As<T>), so any interface
// that EMBEDS one had its structural probe demand those statics from the dynamic value's Go
// method set -- which no Go type can ever satisfy -- flipping every derived-interface probe
// from match to miss.
//
// Section 1 is the decisive case: an ANONYMOUS interface embedding the builtin `error` plus one
// extra method. It reaches golib's structural fallback (no compile-time GoImplement record pairs
// *tempErr with a runtime-only anonymous interface), and because the interface is anonymous the
// converter emits the [GoType("dyn")] runtime wrapper -- so the probe's answer is directly
// observable: match -> the wrapper is built and Temporary()/Error() dispatch; miss -> the
// assertion silently fails. With the static leak this prints "not temporary" for a value that
// Go accepts. *plainErr is the negative control (no Temporary method -> a genuine miss).
//
// Section 2 PINS transitive method collection across a 3-DEEP embedding chain
// (anonymous{named{stringer}} + Depth) -- collection across base interfaces already works and
// must not silently regress when the member filter is tightened. *node satisfies all three
// levels; *shallow is the deepest-level negative control.
package main

import "fmt"

// ---- Section 1: derived-interface structural probe (base-interface static leak) ----

type tempErr struct{ msg string }

func (e *tempErr) Error() string   { return e.msg }
func (e *tempErr) Temporary() bool { return true }

type plainErr struct{ msg string }

func (e *plainErr) Error() string { return e.msg }

// classify asserts to an anonymous interface that EMBEDS the builtin error interface.
func classify(err error) string {
	if te, ok := err.(interface {
		error
		Temporary() bool
	}); ok {
		return fmt.Sprintf("temporary=%v msg=%v", te.Temporary(), te.Error())
	}

	return "not temporary"
}

// ---- Section 2: 3-deep embedding transitive-collection pin ----

type stringish interface {
	String() string
}

type named interface {
	stringish
	Name() string
}

type node struct{ id string }

func (n *node) String() string { return "node:" + n.id }
func (n *node) Name() string   { return n.id }
func (n *node) Depth() int     { return 3 }

type shallow struct{ id string }

func (s *shallow) String() string { return "shallow:" + s.id }
func (s *shallow) Name() string   { return s.id }

// describe asserts to an anonymous interface embedding `named`, which itself embeds
// `stringish` -- three levels of method contribution collected transitively.
func describe(v any) string {
	if d, ok := v.(interface {
		named
		Depth() int
	}); ok {
		return fmt.Sprintf("%v %v %v", d.String(), d.Name(), d.Depth())
	}

	return "not deep"
}

func main() {
	fmt.Println(classify(&tempErr{msg: "boom"}))
	fmt.Println(classify(&plainErr{msg: "plain"}))
	fmt.Println(describe(&node{id: "n1"}))
	fmt.Println(describe(&shallow{id: "s1"}))
}
