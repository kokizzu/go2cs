package main

import "fmt"

// Guards the write-visibility of a TYPE-SWITCH BINDING whose address is taken: the per-case
// binding (a go/types Implicits object, invisible to Defs) must be heap-boxed when its storage
// address escapes to a position that cannot ref-lower, so writes through the pointer land in the
// binding the body then reads back. The consumers here are METHODS deliberately: a method's
// pointer parameters never ref-lower (Phase A §10.1, DESIGN-zh-box-reduction), so the call sites
// must hand over a real aliasing ж pointer — the encoding/xml Decoder.translate shape, where the
// copy-box `Ꮡ(t1).of(…)` silently dropped the namespace translation.

type name struct {
	space string
	local string
}

type start struct {
	n    name
	attr []name
}

type end struct {
	n name
}

type resolver struct {
	ns map[string]string
}

// fix is a METHOD, so its pointer parameter keeps the ж<name> form — the call site must alias.
func (r *resolver) fix(n *name) {
	if v, ok := r.ns[n.space]; ok {
		n.space = v
	}
}

// reset is the direct-&binding consumer: a METHOD taking the case type itself by pointer.
func (r *resolver) reset(e *end) {
	e.n.space = "reset"
}

func process(tok any, r *resolver) any {
	switch t1 := tok.(type) {
	case start:
		r.fix(&t1.n) // write through &t1.field must land in t1, not a boxed copy
		for i := range t1.attr {
			r.fix(&t1.attr[i]) // slice-element form aliases the shared backing (control)
		}
		return t1 // the xml `t = t1` read-back: re-boxes the (mutated) binding
	case end:
		p := &t1.n // a pointer INTO the binding held in a local
		p.space = "held"
		r.reset(&t1) // direct address of the binding itself
		return t1
	}
	return tok
}

func main() {
	r := &resolver{ns: map[string]string{"a": "urn:a"}}

	s := process(start{n: name{space: "a", local: "x"}, attr: []name{{space: "a", local: "y"}}}, r).(start)
	fmt.Println(s.n.space, s.n.local)
	fmt.Println(s.attr[0].space, s.attr[0].local)

	e := process(end{n: name{space: "z", local: "w"}}, r).(end)
	fmt.Println(e.n.space, e.n.local)
}
