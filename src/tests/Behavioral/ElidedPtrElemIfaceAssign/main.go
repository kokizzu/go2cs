// Guard: a composite literal ASSIGNED to an existing interface-typed variable must not push that
// interface down onto its own ELEMENTS. The composite converts to the interface as a WHOLE; a
// keyed element's target is the composite's own value slot. Before the fix, `d = Reg{"a": {…}}`
// (with d of interface type) ran every `*Item` element through a *Item -> Describer cast, whose
// deref-copy fallback collapses the elided `Ꮡ(new Item(…))` to the bare struct — a
// map[string]*Item slot holding a VALUE (CS0029; os TestCopyFS's fstest.MapFS literal).
//
// The same literal in a `var` declaration, a call argument, or assigned to its own concrete type
// was always correct, so those arms are the controls; the interface-VALUED map is the control for
// the behavior the fix must PRESERVE (there the slot really is the interface).
package main

import "fmt"

type Item struct {
	Name string
	N    int
}

type Describer interface{ Describe() string }

// Reg's element type is a POINTER, so `{Name: …}` elides `&Item{…}`.
type Reg map[string]*Item

func (r Reg) Describe() string {
	out := ""
	for _, k := range []string{"a", "b"} {
		if it, ok := r[k]; ok {
			out += fmt.Sprintf("%s=%s/%d ", k, it.Name, it.N)
		}
	}
	return out
}

// Bag's element type IS the interface — the slot the fix must keep converting.
type Bag map[string]Describer

func (b Bag) Describe() string {
	out := ""
	for _, k := range []string{"r", "s"} {
		if d, ok := b[k]; ok {
			out += k + "{" + d.Describe() + "} "
		}
	}
	return out
}

// List exercises the same elision through a named SLICE of pointers.
type List []*Item

func (l List) Describe() string {
	out := ""
	for _, it := range l {
		out += fmt.Sprintf("%s/%d ", it.Name, it.N)
	}
	return out
}

func take(d Describer) string { return d.Describe() }

func main() {
	// 1. declaration with an explicit interface type (always worked)
	var d Describer = Reg{"a": {Name: "alpha", N: 1}}
	fmt.Println("decl:", d.Describe())

	// 2. ASSIGNMENT to an existing interface variable — the defect
	d = Reg{
		"a": {Name: "beta", N: 2},
		"b": {Name: "gamma", N: 3},
	}
	fmt.Println("assign:", d.Describe())

	// 3. assignment to a CONCRETE variable (control)
	var c Reg
	c = Reg{"a": {Name: "delta", N: 4}}
	fmt.Println("concrete:", c.Describe())

	// 4. the element is the SAME object, not a copy: a write through the stored pointer shows up
	c["a"].N = 40
	fmt.Println("mutated:", c.Describe())

	// 5. call argument in an interface slot (control)
	fmt.Println("arg:", take(Reg{"b": {Name: "epsilon", N: 5}}))

	// 6. named SLICE of pointers, assigned to an interface variable
	d = List{{Name: "zeta", N: 6}, {Name: "eta", N: 7}}
	fmt.Println("slice:", d.Describe())

	// 7. PRESERVED behavior: an interface-VALUED map still converts each element to the slot's
	//    interface, whether declared or assigned
	var b Describer = Bag{"r": Reg{"a": {Name: "theta", N: 8}}}
	fmt.Println("bagdecl:", b.Describe())
	b = Bag{
		"r": Reg{"a": {Name: "iota", N: 9}},
		"s": List{{Name: "kappa", N: 10}},
	}
	fmt.Println("bagassign:", b.Describe())
}
