// Package ptlike supplies the FOREIGN struct the parent package defines a type over.
// Its shape mirrors index/suffixarray's `Index`: a struct with an exported plain field and an
// exported field whose own type is a named struct carrying both value- and pointer-receiver
// methods — the combination that makes the parent's `type alias ptlike.Outer` need real
// variable (addressable) semantics on the forwarded selections.
package ptlike

// Inner is the nested named struct. Len has a VALUE receiver (the converter emits `this ref`),
// Push a POINTER receiver — so a selection of an Outer field of this type must be a variable.
type Inner struct {
	Vals []int
}

func (i Inner) Len() int {
	return len(i.Vals)
}

func (i *Inner) Push(v int) {
	i.Vals = append(i.Vals, v)
}

// Outer is the struct the parent package defines a type over.
type Outer struct {
	Name string
	In   Inner
}

func New(name string, vals ...int) Outer {
	return Outer{Name: name, In: Inner{Vals: vals}}
}
