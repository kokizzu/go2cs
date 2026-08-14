// AssignThroughTypeAssert guards the shadow-rename walk over an ASSIGNMENT TARGET whose
// navigation chain contains a TYPE ASSERTION.
//
// The rename analysis descends an assignment's LHS to reach the identifiers it must rewrite
// (the chain root, and any index sub-expression along the way). Its descent knew selectors,
// indexes, stars, parens and calls -- but not `x.(T)`, so it stopped dead at an assertion and
// everything below it kept its RAW name. Where the raw name still resolves to an enclosing
// variable, that is a silent wrong bind; where it does not, it is a compile error.
//
// The witness is go/types' own generate_test.go (fixGlobalTypVarDecl):
//
//	ast.Inspect(f, func(n ast.Node) bool {
//	    switch n := n.(type) {
//	    case *ast.ValueSpec:
//	        if len(n.Names) == 1 && ... {
//	            n.Values[0].(*ast.CompositeLit).Type.(*ast.ArrayType).Len = nil
//
// The case variable collides with the func-literal parameter and is Δ-renamed, the `if`
// condition follows the rename -- and the body assignment did not, emitting `(~n)` against the
// outer `ast.Node` parameter (CS0023).
package main

import "fmt"

type Node interface{ node() }

type Lit struct {
	Len  int
	Name string
}

func (*Lit) node() {}

type ValueSpec struct {
	Names  []string
	Values []Node
}

func (*ValueSpec) node() {}

func walk(root Node, f func(n Node) bool) {
	f(root)
}

// fixSpec is the go/types witness: the case variable shadows the func-literal parameter `n`,
// and the body assignment navigates through a type assertion.
func fixSpec(root Node) {
	walk(root, func(n Node) bool {
		switch n := n.(type) {
		case *ValueSpec:
			if len(n.Names) == 1 && n.Names[0] == "Typ" && len(n.Values) == 1 {
				n.Values[0].(*Lit).Len = 42
				return false
			}
		}
		return true
	})
}

// renumber puts a SECOND shadow-renamed identifier -- the loop index -- BELOW the assertion in
// the same chain, so the fix must visit the whole operand subtree, not merely its root.
func renumber(root Node, i int) int {
	walk(root, func(n Node) bool {
		switch n := n.(type) {
		case *ValueSpec:
			for i := range n.Values {
				n.Values[i].(*Lit).Len = (i + 1) * 10
			}
		}
		return true
	})

	return i
}

// bump is the COMPOUND-assignment form of the same chain.
func bump(root Node) {
	walk(root, func(n Node) bool {
		switch n := n.(type) {
		case *ValueSpec:
			n.Values[0].(*Lit).Len += 5
		}
		return true
	})
}

// tag puts the assertion at the very ROOT of the target (the default arm, where the case
// variable is re-bound at the guard's interface type).
func tag(root Node) {
	walk(root, func(n Node) bool {
		switch n := n.(type) {
		case *ValueSpec:
			n.Values[0].(*Lit).Name = "spec"
		default:
			n.(*Lit).Name = "lit"
		}
		return true
	})
}

// read is the CONTROL: the same chain as a READ rather than an assignment. It already followed
// the rename before the fix and must keep doing so.
func read(root Node) string {
	got := ""

	walk(root, func(n Node) bool {
		switch n := n.(type) {
		case *ValueSpec:
			got = n.Values[0].(*Lit).Name
		}
		return true
	})

	return got
}

func newSpec() *ValueSpec {
	return &ValueSpec{
		Names:  []string{"Typ"},
		Values: []Node{&Lit{Len: 1, Name: "a"}},
	}
}

func main() {
	spec := newSpec()
	fixSpec(spec)
	fmt.Println("fixSpec:", spec.Values[0].(*Lit).Len)

	multi := &ValueSpec{
		Names:  []string{"Typ", "Other"},
		Values: []Node{&Lit{Len: 1, Name: "a"}, &Lit{Len: 2, Name: "b"}, &Lit{Len: 3, Name: "c"}},
	}
	outer := renumber(multi, 7)
	fmt.Println("renumber outer:", outer)

	for k := range multi.Values {
		fmt.Println("renumber:", k, multi.Values[k].(*Lit).Len)
	}

	bumped := newSpec()
	bump(bumped)
	fmt.Println("bump:", bumped.Values[0].(*Lit).Len)

	tagged := newSpec()
	tag(tagged)
	fmt.Println("tag spec:", tagged.Values[0].(*Lit).Name)

	lone := &Lit{Len: 9, Name: "z"}
	tag(lone)
	fmt.Println("tag default:", lone.Name)

	fmt.Println("read:", read(tagged))
}
